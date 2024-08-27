using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ComicRecompress.Models;
using Quartz;

namespace ComicRecompress.Jobs
{
    public class RecompressJob : IJob
    {

        public Task Execute(IJobExecutionContext context)
        {
            ExecutionState state = context.GetState();
            return context.Scheduler.ScheduleJob<DecompressJob>(state,5);
        }
    }
}
