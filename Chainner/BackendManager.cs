using CliWrap;
using ComicRecompress.Jobs;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Threading.Tasks;

namespace ComicRecompress.Chainner
{
    public class BackendManager
    {
        public bool Ready { get; set; } = false;
        public JsonNode Settings { get; set; }
        public int Port { get; set; } = 8001;

        
        private string _pythonPath;
        private string _backendPath;
        private string _workingDir = null;
        private CommandTask<CommandResult> _currentTask;
        private CancellationTokenSource? _source = null;
        
        private readonly BaseJob _job = new BaseJob(System.Drawing.Color.DarkMagenta);

        private void WriteLine(string str)
        {
            if (str.Contains("[INFO] Done."))
                Ready = true;
            if (str.Contains("TensorRT cache location"))
                return;
            if (str.Contains("body not consumed"))
                return;
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
                    _backendPath = Path.Combine(resources, "src\\run.py");
                    if (File.Exists(_backendPath))
                    {
                        _workingDir = dir;
                        break;
                    }
                }
            }

            if (_workingDir == null)
                return false;
            string python = Path.Combine(roamingCombine, "python\\python\\python.exe");
            if (!File.Exists(python))
                Console.WriteLine("Integrated Python not found");
            _pythonPath = python;
            string settings = Path.Combine(roamingCombine, "settings.json");
            if (!File.Exists(settings))
                Console.WriteLine("Settings not found");
            JsonNode node = JsonNode.Parse(File.ReadAllText(settings));
            Settings = node["packageSettings"];
            return true;
        }

        private CommandTask<CommandResult> GetBackend(int port, CancellationToken ctx)
        {
            if (!FindBackend())
                return null;
            Port = port;
            PipeTarget target = PipeTarget.ToDelegate(WriteLine);
            var cmd = Cli.Wrap(_pythonPath).WithWorkingDirectory(_workingDir)
                .WithArguments($"\"{_backendPath}\" {port}")
                .WithStandardOutputPipe(target).WithStandardErrorPipe(target);
            return cmd.ExecuteAsync(ctx);
        }

        public async Task<bool> StartAsync(int port)
        {
            if (_currentTask != null)
            {
                Console.WriteLine("Chainner Backend Already Running");
                return false;
            }
            _source = new CancellationTokenSource();
            _currentTask = GetBackend(port, _source.Token);
            if (_currentTask == null)
            {
                Console.WriteLine("Unable to find chaiNNer, make sure you install it from https://github.com/chaiNNer-org/chaiNNer/releases, install latest chaiNNer-windows-x64, and run it. Inside chaiNNer you need to install all the required dependencies, including integrated python, pytorch, ncnn and onnx, adjust setting if required, then quit. and run comicRecompress again.");
                return false;

            }
            Task.Run(async () =>
            {
                await _currentTask;
            });
            do
            {
                await Task.Delay(100).ConfigureAwait(false);
            } while (!Ready);
            return true;
        }

        public async Task<bool> StopAsync()
        {
            if (_source!=null)
                await _source.CancelAsync().ConfigureAwait(false);
            await Task.Delay(1000).ConfigureAwait(false);
            _currentTask = null;
            _source = null;
            Ready = false;
            return true;
        }
    }
}
