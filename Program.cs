using System.Collections.Specialized;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Drawing;
using System.Globalization;
using System.Reflection.Metadata;
using ComicRecompress.Chainner;
using ComicRecompress.Chainner.Models;
using ComicRecompress.Jobs;
using ComicRecompress.Models;
using ComicRecompress.Services;
using CommandLine;
using Pastel;
using Quartz;
using Quartz.Impl;
using Quartz.Impl.Matchers;

namespace ComicRecompress
{
    internal class Program
    {

        public class Options
        {
            [Option('i', "input", Required = true, HelpText = "Input File or Directory")]
            public string Input { get; set; }

            [Option('o', "output", Required = true, HelpText = "Output Directory")]
            public string Output { get; set; }

            [Option('c', "chain", Required = false, HelpText = "ChaiNNer chain file", Default = "default-onnx.chn")]
            public string Chain { get; set; }

            [Option('m', "mode", Required = false, HelpText = "Mode of Execution [chaiNNer|Reconstruct|Modular]",
                Default = "chaiNNer")]
            public string Mode { get; set; }

            [Option('w', "webcomic", Required = false,
                HelpText =
                    "WebComic/Manhwa, Join same width images, into a bigger images before processing, specifying the maximum height of the final images [0 - Disabled]",
                Default = 0)]
            public int WebComicMaxHeight { get; set; }

            [Option('q', "quality", Required = false, HelpText = "JPEG XL Quality for Chainner/Modular Mode",
                Default = 95)]
            public int Quality { get; set; }

            [Option('p', "port", Required = false, HelpText = "Chainner Backend Port", Default = 8000)]
            public int Port { get; set; }

            [Option('t', "tasks", Required = false,
                HelpText = "Number of Comics that can be processed at the same time doing different tasks",
                Default = 2)]
            public int Tasks { get; set; }

            [Option('j', "jpegXLThreads", Required = false,
                HelpText = "Number of JPEG XL Threads running at the same time", Default = 4)]
            public int JPEGThreads { get; set; }

            [Option('k', "tasksRestartBackend", Required = false,
                HelpText = "Number of tasks before restarting the backend [0 = Never]", Default = 5)]
            public int RestartTasks { get; set; }

        }


        private static string defaultChn = "default-onnx.chn";

        public static object _lck = new object();
        public static BackendManager Backend { get; set; }

        public static int Port { get; } = 8000;

        public static int ChainnerRespawnCount { get; set; } = 5;

        public static int RespawnCount = 0;
        [DynamicDependency(DynamicallyAccessedMemberTypes.All, typeof(Options))]
        public static async Task Main(string[] args)
        {

            await Parser.Default.ParseArguments<Options>(args).WithNotParsed(HandleParseError).WithParsedAsync(RunOptions).ConfigureAwait(false);
        }

        static async Task RunOptions(Options opts)
        {

            if (!File.Exists(opts.Input))
            {
                if (!Directory.Exists(opts.Input))
                {
                    Console.WriteLine("Source File/Directory does not exist");
                    return;
                }
            }
            if (!Directory.Exists(opts.Output))
                Directory.CreateDirectory(opts.Output);
            JpegXL jpegXL = new JpegXL(new BaseJob(Color.Yellow));
            if (!(await jpegXL.IsAvailableAsync().ConfigureAwait(false)))
            {
                Console.WriteLine($"Jpeg XL Compressor ({jpegXL.Exe}) not found. Download it from https://github.com/libjxl/libjxl/releases and download latest jxl-x86-windows-static.zip and put '{jpegXL.Exe}' in this app directory.");
                return;
            }
            Mode mode = Mode.Chainner;
            Enum.TryParse<Mode>(opts.Mode, true, out mode);
            if (mode == Mode.Chainner)
            {
                Services.Chainner chainner = new Services.Chainner(new BaseJob(Color.Magenta));
                if (!chainner.VerifyChainnerFile(opts.Chain))
                    return;
                Backend = new BackendManager();
                Program.Backend.Port = opts.Port;
                bool res = await Backend.StartAsync(Port).ConfigureAwait(false);
                if (!res)
                {
                    Console.WriteLine("Unable to start Chainner backend");
                    return;
                }
            }
            Console.CancelKeyPress += (a, b) =>
            {
                Backend?.StopAsync().GetAwaiter().GetResult();
                b.Cancel = false;
            };

            DirectSchedulerFactory.Instance.CreateVolatileScheduler(5);
            ISchedulerFactory schFac = DirectSchedulerFactory.Instance;
            IScheduler scheduler = await schFac.GetScheduler().ConfigureAwait(false);
            await scheduler.Start().ConfigureAwait(false);
            try
            {
                scheduler.Context["cnt"] = 0;
                scheduler.Context["ErrorFiles"] = new List<string>();
                string[] files = null;
                bool fileMode = false;
                if (File.Exists(opts.Input))
                {
                    files = new[] { opts.Input };
                    fileMode = true;
                }
                else
                    files = Directory.GetFiles(opts.Input, "*", SearchOption.AllDirectories);
                ChainnerRespawnCount = opts.RestartTasks;
                foreach (string file in files.OrderBy(a => a))
                {
                    string relative = fileMode ? Path.GetFileName(opts.Input) : file.Substring(opts.Input.Length + 1);
                    string dest = Path.Combine(opts.Output, relative);
                    dest = Path.ChangeExtension(dest, ".cbz");
                    if (File.Exists(dest))
                        continue; //Already Processed
                    ProcessState state = new ProcessState();
                    state.Mode = mode;
                    state.ChnFile = opts.Chain;
                    state.DestinationFile = dest;
                    state.SourceFile = file;
                    state.WebComicMaxJoinSize = opts.WebComicMaxHeight;
                    state.JPEGXLQuality = opts.Quality;
                    state.JPEGXLThreads = opts.JPEGThreads;
                    lock (_lck)
                    {
                        int value = (int)scheduler.Context["cnt"];
                        scheduler.Context["cnt"] = value + 1;
                    }
                    await scheduler.ScheduleJob<RecompressJob>(new ExecutionState { ProcessState = state }, 5).ConfigureAwait(false);
                    while ((int)scheduler.Context["cnt"] >= opts.Tasks)
                        await Task.Delay(100).ConfigureAwait(false);
                }

                do
                {
                    var groupMatcher = GroupMatcher<JobKey>.GroupContains("comicrecompress");
                    var jobKeys = await scheduler.GetJobKeys(groupMatcher);
                    if (jobKeys.Count == 0)
                        break;
                    await Task.Delay(100);

                } while (true);
                List<string> errorfiles = (List<string>)scheduler.Context["ErrorFiles"];
                await scheduler.Shutdown(true).ConfigureAwait(false);
                if (errorfiles.Count > 0)
                {
                    Console.WriteLine($"*** ERROR FILES ***".Pastel(Color.Red));
                    Console.WriteLine($"The following files could not be recompressed:".Pastel(Color.Red));

                }
                foreach (string file in errorfiles)
                {
                    Console.WriteLine($"{file}".Pastel(Color.Red));
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
            await Backend.StopAsync().ConfigureAwait(false);
        }


        static void HandleParseError(IEnumerable<Error> errs)
        {

        }
        public static async Task MayRespawn()
        {
            if (ChainnerRespawnCount == 0)
                return;
            RespawnCount++;
            if (RespawnCount == ChainnerRespawnCount)
            {
                RespawnCount = 0;
                await Backend.StopAsync().ConfigureAwait(false);
                await Backend.StartAsync(Port).ConfigureAwait(false);
            }
        }

    }

}
