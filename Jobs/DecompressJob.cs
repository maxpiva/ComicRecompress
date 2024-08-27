using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ComicRecompress.Models;
using ComicRecompress.Services;
using Pastel;
using Quartz;

namespace ComicRecompress.Jobs
{
    public class DecompressJob : BaseJob, IJob
    {
        private readonly Compression compression;

        public DecompressJob() : base(Color.Blue)
        {
            compression = new Compression(this);
        }
        public async Task Execute(IJobExecutionContext context)
        {
            ExecutionState state = context.GetState();
            Init($"Decompressing {Path.GetFileName(state.ProcessState.SourceFile)}");
            ExecutionState newState = state.NewContext<DecompressJob>();
            bool res=compression.Decompress(newState.InputPath, state.ProcessState.SourceFile);
            if (res)
            {
                if (state.ProcessState.WebComicMaxJoinSize > 0)
                    await context.Scheduler.ScheduleJob<JoinJob>(newState, 6).ConfigureAwait(false);
                else if (state.ProcessState.Mode == Mode.Chainner)
                    await context.Scheduler.ScheduleJob<ChainnerJob>(newState, 6, true).ConfigureAwait(false);
                else
                    await context.Scheduler.ScheduleJob<JPegXLJob>(newState, 7, true).ConfigureAwait(false);
            }
            else
                context.Scheduler.AddErrorFile(state.ProcessState.SourceFile);
            End();
        }
    }
}
