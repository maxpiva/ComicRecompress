using ComicRecompress.Chainner.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using ComicRecompress.Models;

namespace ComicRecompress.Chainner
{
    public class ChainnerClient
    {
        private readonly string _backendUrl;
        private readonly JsonNode _settings;
        public NodesSchema? NodeSchema { get; private set; } = null;

        public delegate void MessageEvent(object sender, BaseEvent message);
        public event MessageEvent OnMessage;



        public class Worker
        {
            public string executor { get; set; }
        }
        public ChainnerClient(string backendUrl, JsonNode settings)
        {
            _backendUrl = backendUrl;
            _settings = settings;
        }


        public async Task<bool> LoadNodesAsync()
        {
            if (NodeSchema != null)
                return true;
            string url = $"{_backendUrl}/nodes";
            using (HttpClient cl = new HttpClient())
            {
                string nodes = await cl.GetStringAsync(url).ConfigureAwait(false);
                try
                {
                    NodeSchema = JsonSerializer.Deserialize<NodesSchema>(nodes, SerializerGenerationContext.Options);
                    return true;
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.Message);
                }
            }

            return false;
        }
        public async Task<bool> KillAsync()
        {
            HttpClient cl = new HttpClient();
            string url = $"{_backendUrl}/kill";
            string res = await cl.GetStringAsync(url).ConfigureAwait(false);
            return true;
        }


       




        public async Task<bool> RunChnAsync(ChnFile file)
        {
            bool res=await LoadNodesAsync().ConfigureAwait(false);
            if (!res)
                Console.WriteLine("Unable to load nodes");
            string url = $"{_backendUrl}/run";
            RunRequest req = new RunRequest();
            req.data = file.ToBackendJson(NodeSchema.nodes);
            req.sendBroadcastData = true;
            req.options = _settings;
            string chn = JsonSerializer.Serialize(req, SerializerGenerationContext.Options);
            HttpRequestMessage msg = new HttpRequestMessage(HttpMethod.Post, url);
            CancellationTokenSource src = new CancellationTokenSource();
            msg.Content = new StringContent(chn, Encoding.UTF8, "application/json");
            Task.Run(async () =>
            {
                await EventHandling(src.Token).ConfigureAwait(false);
            }, src.Token);
            using (HttpClient cl = new HttpClient())
            {
                cl.Timeout = TimeSpan.FromMinutes(1000);
                HttpResponseMessage resp = await cl.SendAsync(msg).ConfigureAwait(false);
                await src.CancelAsync();
                if (resp.IsSuccessStatusCode)
                {
                    string response = await resp.Content.ReadAsStringAsync().ConfigureAwait(false);
                    Response respObj = JsonSerializer.Deserialize<Response>(response, SerializerGenerationContext.Options);
                    if (respObj.type == "success")
                        return true;
                }
            }
            return false;
        }

        private async Task EventHandling(CancellationToken token)
        {
            using (var client = new HttpClient())
            {
                StringBuilder bld = new StringBuilder();
                while (!token.IsCancellationRequested)
                {
                    try
                    {
                        using (var stream = await client.GetStreamAsync($"{_backendUrl}/sse",token).ConfigureAwait(false))
                        {
                            using (var reader = new StreamReader(stream))
                            {
                                //while(true)
                                while (!token.IsCancellationRequested)
                                {
                                    CancellationTokenSource src2 = CancellationTokenSource.CreateLinkedTokenSource(token);
                                    src2.CancelAfter(200000);
                                    string? msg = await reader.ReadLineAsync(src2.Token).ConfigureAwait(false);
                                    if (msg == null)
                                        throw new Exception("");
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
                                                                NodeStart ns = JsonSerializer.Deserialize<NodeStart>(eventdata, SerializerGenerationContext.Options);
                                                                OnMessage?.Invoke(this, ns);
                                                                break;
                                                            case "node-finish":
                                                                NodeFinish nf = JsonSerializer.Deserialize<NodeFinish>(eventdata, SerializerGenerationContext.Options);
                                                                OnMessage?.Invoke(this, nf);
                                                                break;
                                                            case "node-progress":
                                                                NodeProgress np = JsonSerializer.Deserialize<NodeProgress>(eventdata, SerializerGenerationContext.Options);
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
                        //Eat it
                    }
                }
            }
        }
    }
}
