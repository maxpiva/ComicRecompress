using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using CliWrap;
using CliWrap.Buffered;
using ComicRecompress.Jobs;
using ComicRecompress.Models;
using Pastel;


namespace ComicRecompress.Services
{
    public class JpegXL
    {
        public string Exe { get; } = "cjxl.exe";

        private readonly BaseJob _job;

        public JpegXL(BaseJob job)
        {
            _job = job;
        }
        public async Task<bool> IsAvailableAsync()
        {
            try
            {
                var result = await Cli.Wrap(Exe).WithArguments("--version").ExecuteBufferedAsync().ConfigureAwait(false);
                if (result.IsSuccess && result.StandardOutput.Contains("JPEG XL"))
                    return true;
            }
            catch (Exception e)
            {
            }
            return false;
        }

        public async Task<bool> CompressAsync(string input, string output, ProcessState state)
        {
            try
            {
                PipeTarget target = PipeTarget.ToDelegate(_job.WriteLine);
                int modular = 1;
                int lossjpeg = 0;
                if (input.EndsWith(".jpg") && state.Mode == Mode.Reconstruct)
                {
                    modular = 0;
                    lossjpeg = 1;
                }

                var cmd = Cli.Wrap(Exe).WithArguments($"-q {state.JPEGXLQuality} -j {lossjpeg} -e 10 -m {modular} --brotli_effort 11 \"{input}\" \"{output}\"")
                    .WithStandardOutputPipe(target).WithStandardErrorPipe(target);
                var result = await cmd.ExecuteAsync().ConfigureAwait(false);
                if (result.IsSuccess)
                    return true;
                _job.WriteError($"ERROR: executing JPEG XL Compression: Exit Code: {result.ExitCode}");
                return false;

            }
            catch (Exception e)
            {
                _job.WriteError($"ERROR:executing JPEG XL Compression: {e.Message}");
            }
            return false;
        }
    }
}
