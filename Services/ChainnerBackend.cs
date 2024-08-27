using CliWrap;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.Tracing;
using System.Dynamic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using ComicRecompress.Jobs;
using SixLabors.ImageSharp;
using static ComicRecompress.Services.Chainner;
using ComicRecompress.ChainnerModels;
using ComicRecompress.Chainner.Models;

namespace ComicRecompress.Services
{


    public class ChsssainnerBackend
    {
        public string PythonPath { get; set; }
        public string BackendPath { get; set; }
        public string WorkingDir { get; set; } = null;
        public bool Ready { get; set; } = false;
        public JsonNode Settings { get; set; }
        public int Port { get; private set; } = 5000;
        private readonly BaseJob _job = new BaseJob(System.Drawing.Color.DarkMagenta);

        private void WriteLine(string str)
        {
            if (str.Contains("[INFO] Done."))
                Ready = true;
            _job.WriteLine(str);
        }

        private bool FindBackend()
        {
            //Only Windows OS right now
            string local = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            string roaming = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
            string localCombine = Path.Combine(local, "chaiNNer");
            string roamingCombine = Path.Combine(roaming, "chaiNNer");
            if (!Directory.Exists(localCombine))
                return false;
            if (!Directory.Exists(roamingCombine))
                return false;
            string[] dirs = Directory.GetDirectories(localCombine, "App*", SearchOption.TopDirectoryOnly);
            dirs = dirs.OrderByDescending(a => a).ToArray();
            foreach (string dir in dirs)
            {
                string resources = Path.Combine(dir, "resources");
                if (Directory.Exists(resources))
                {
                    BackendPath = Path.Combine(resources, "src\\run.py");
                    if (File.Exists(BackendPath))
                    {
                        WorkingDir = dir;
                        break;
                    }
                }
            }

            if (WorkingDir == null)
                return false;
            string python = Path.Combine(roamingCombine, "python\\python\\python.exe");
            if (!File.Exists(python))
                Console.WriteLine("Integrated Python not found");
            PythonPath = python;
            string settings = Path.Combine(roamingCombine, "settings.json");
            if (!File.Exists(settings))
                Console.WriteLine("Settings not found");
            JsonNode node = JsonNode.Parse(File.ReadAllText(settings));
            Settings = node["packageSettings"];
            return true;
        }


        public CommandTask<CommandResult> GetBackend(int port, CancellationToken ctx)
        {
            if (!FindBackend())
                return null;
            Port = port;
            PipeTarget target = PipeTarget.ToDelegate(WriteLine);
            var cmd = Cli.Wrap(PythonPath).WithWorkingDirectory(WorkingDir)
                .WithArguments($"\"{BackendPath}\" {port}")
                .WithStandardOutputPipe(target).WithStandardErrorPipe(target);
            return cmd.ExecuteAsync(ctx);
        }

        private CommandTask<CommandResult> CurrentTask;
        private CancellationTokenSource _source;

        public class RunRequest
        {
            public List<BackendJsonNode> data { get; set; }
            public JsonNode options { get; set; }

            public bool sendBroadcastData { get; set; }
        }

        public class Worker
        {
            public string executor { get; set; }
        }
        public class Response
        {
            public string type { get; set; }
            public string message { get; set; }

            public bool ready { get; set; }

            public Worker worker { get; set; }
        }
        public interface BaseEvent
        {
        }


        public class NodeStart : BaseEvent
        {
            [JsonPropertyName("nodeId")]
            public string NodeId { get; set; }
        }
        public class NodeFinish : BaseEvent
        {
            [JsonPropertyName("nodeId")]
            public string NodeId { get; set; }
            [JsonPropertyName("executionTime")]
            public decimal ExecutionTime { get; set; }
        }


        public class NodeProgress : BaseEvent
        {
            [JsonPropertyName("nodeId")]
            public string NodeId { get; set; }
            [JsonPropertyName("progress")]
            public decimal Progress { get; set; }
            [JsonPropertyName("index")]
            public int Index { get; set; }
            [JsonPropertyName("total")]
            public int Total { get; set; }
            [JsonPropertyName("eta")]
            public decimal Eta { get; set; }
        }
        public delegate void MessageEvent(object sender, BaseEvent message);

        public event MessageEvent OnMessage;



        public async Task<bool> RunChn(ChnFile file)
        {
            if (CurrentTask == null)
                Console.WriteLine("Chainner Backend Not Running");
            HttpClient cl = new HttpClient();

            string url = $"http://localhost:{Port}/run";
            RunRequest req = new RunRequest();
            req.data = Converter.ToBackendJson(file.content.nodes, file.content.edges, NodeSchema.nodes);
            req.sendBroadcastData = true;
            req.options = Settings;
            string chn = JsonSerializer.Serialize(req);
            HttpRequestMessage msg = new HttpRequestMessage(HttpMethod.Post, url);
            msg.Content = new StringContent(chn, Encoding.UTF8, "application/json");
            Task.Run(async () =>
            {
                await EventHandling().ConfigureAwait(false);
            }, _source.Token);
            cl.Timeout = TimeSpan.FromMinutes(1000);
            HttpResponseMessage resp = await cl.SendAsync(msg).ConfigureAwait(false);
            if (resp.IsSuccessStatusCode)
            {
                string response = await resp.Content.ReadAsStringAsync().ConfigureAwait(false);
                Response respObj = JsonSerializer.Deserialize<Response>(response);
                if (respObj.type == "success")
                    return true;
            }
            return false;
        }

        private async Task EventHandling()
        {
            using (var client = new HttpClient())
            {
                StringBuilder bld = new StringBuilder();
                while (true)
                {
                    try
                    {
                        using (var stream = await client.GetStreamAsync($"http://localhost:{Port}/sse")
                                   .ConfigureAwait(false))
                        {
                            using (var reader = new StreamReader(stream))
                            {
                                //while(true)
                                while (!_source.Token.IsCancellationRequested)
                                {
                                    string msg = reader.ReadLine();
                                    if (string.IsNullOrEmpty(msg))
                                    {
                                        if (bld.Length > 0)
                                        {
                                            List<string> lines = bld.ToString().Split('\n').ToList();
                                            string eventname = null;
                                            string eventdata = null;
                                            foreach (string k in lines)
                                            {
                                                if (k.StartsWith("event:"))
                                                    eventname = k.Substring(6).Trim();
                                                if (k.StartsWith("data:"))
                                                {
                                                    eventdata = k.Substring(5).Trim();
                                                    if (eventname != null && eventdata != null)
                                                    {
                                                        switch (eventname)
                                                        {
                                                            case "node-start":
                                                                NodeStart ns = JsonSerializer.Deserialize<NodeStart>(eventdata);
                                                                OnMessage?.Invoke(this, ns);
                                                                break;
                                                            case "node-finish":
                                                                NodeFinish nf = JsonSerializer.Deserialize<NodeFinish>(eventdata);
                                                                OnMessage?.Invoke(this, nf);
                                                                break;
                                                            case "node-progress":
                                                                NodeProgress np = JsonSerializer.Deserialize<NodeProgress>(eventdata);
                                                                OnMessage?.Invoke(this, np);
                                                                break;
                                                        }
                                                    }

                                                }
                                            }

                                        }
                                        bld.Clear();
                                    }
                                    else
                                    {
                                        bld.AppendLine(msg);
                                    }
                                }
                            }
                        }
                    }
                    catch (Exception e)
                    {
                    }
                }
            }
        }

        public async Task<bool> Kill()
        {
            HttpClient cl = new HttpClient();
            string url = $"http://localhost:{Port}/shutdown";
            return true;
        }
        public async Task<bool> StartBackend(int port)
        {
            if (CurrentTask != null)
            {
                Console.WriteLine("Chainner Backend Already Running");
                return false;
            }
            _source = new CancellationTokenSource();
            CurrentTask = GetBackend(port, _source.Token);
            if (CurrentTask == null)
                return false;
            Task.Run(async () =>
            {
                await CurrentTask;
            });
            do
            {
                await Task.Delay(100).ConfigureAwait(false);
            } while (!Ready);
            HttpClient cl = new HttpClient();
            string url = $"http://localhost:{Port}/nodes";
            string nodes = await cl.GetStringAsync(url);
            try
            {
                NodeSchema = JsonSerializer.Deserialize<NodesSchema>(nodes);
            }
            catch (Exception e)
            {
                int a = 1;
            }

            return true;
        }

        public async Task<bool> StopBackend()
        {
            HttpClient cl = new HttpClient();
            string url = $"http://localhost:{Port}/kill";
            string res = await cl.GetStringAsync(url);
            await _source.CancelAsync().ConfigureAwait(false);
            await Task.Delay(1000).ConfigureAwait(false);
            CurrentTask = null;
            Ready = false;
            return true;
        }

    }
}
