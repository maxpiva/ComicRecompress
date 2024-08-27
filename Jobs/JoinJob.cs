using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Quartz;
using ZstdSharp;
using System.Drawing;
using ComicRecompress.Services;
using ComicRecompress.Models;
namespace ComicRecompress.Jobs
{
    public class JoinJob : BaseJob, IJob
    {
        private readonly WebComicJoiner joiner;

        public JoinJob() : base(Color.LightPink)
        {
            joiner = new WebComicJoiner(this);
        }

        public async Task Execute(IJobExecutionContext context)
        {
            ExecutionState state = context.GetState();
            Init($"Joining WebComic {Path.GetFileName(state.ProcessState.SourceFile)}");
            ExecutionState newState = state.NewContext<JoinJob>();
            bool res = await joiner.Join(state.InputPath, newState.InputPath, state.ProcessState.WebComicMaxJoinSize).ConfigureAwait(false);
            if (res)
            {
                if (state.ProcessState.Mode == Mode.Chainner)
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
