using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Net.NetworkInformation;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using ComicRecompress.Models;
using ComicRecompress.Services;
using Pastel;
using Quartz;

namespace ComicRecompress.Jobs
{
    public class CompressJob : BaseJob, IJob
    {
        private readonly Compression compression;

        public CompressJob() : base(Color.Green)
        {
            compression = new Compression(this);
        }
        public async Task Execute(IJobExecutionContext context)
        {
            ExecutionState state = context.GetState();
            Init($"Compressing {Path.GetFileName(state.ProcessState.DestinationFile)}");
            string destDir = Path.GetDirectoryName(state.ProcessState.DestinationFile);
            Directory.CreateDirectory(destDir);
            string decompressDirectory = state.GetTemporaryDirectory<DecompressJob>();
            foreach (string file in Directory.GetFiles(decompressDirectory, "*", SearchOption.AllDirectories))
            {
                if (!Extensions.IsImage(file))
                {
                    string relative = file.Substring(decompressDirectory.Length + 1);
                    string dest2 = Path.Combine(state.InputPath, relative);
                    Directory.CreateDirectory(Path.GetDirectoryName(dest2));
                    File.Copy(file, dest2, true);
                }
            }

            bool result = compression.Compress(state.InputPath, state.ProcessState.DestinationFile);
            if (!result)
                context.Scheduler.AddErrorFile(state.ProcessState.SourceFile);
            await context.Scheduler.ScheduleJob<EndJob>(state, 10).ConfigureAwait(false);
            End();
        }
    }
}
