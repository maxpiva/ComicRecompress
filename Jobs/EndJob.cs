using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ComicRecompress.Models;
using Pastel;
using Quartz;
namespace ComicRecompress.Jobs
{
    public class EndJob : BaseJob, IJob
    {
        private static object _lck = new object();

        public EndJob() : base(Color.White)
        {
        }
        public Task Execute(IJobExecutionContext context)
        {
            ExecutionState state = context.GetState();
            state.DeleteTemporaryDirectories();
            lock (_lck)
            {
                int value = (int)context.Scheduler.Context["cnt"];
                context.Scheduler.Context["cnt"] = value - 1;
            }
            return Task.CompletedTask;
        }
    }
}
