using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using ComicRecompress.Chainner;
using ComicRecompress.Models;
using ComicRecompress.Services;
using Pastel;
using Quartz;

namespace ComicRecompress.Jobs
{

    public class ChainnerJob : BaseJob, IJob
    {
        private readonly Services.Chainner chainner;

        public ChainnerJob() : base(Color.Magenta)
        {
            chainner = new Services.Chainner(this);
        }
        public async Task Execute(IJobExecutionContext context)
        {
            ExecutionState state = context.GetState();
            string name = Path.GetFileNameWithoutExtension(state.ProcessState.ChnFile);

            Init($"chaiNNer '{name}' - {Path.GetFileName(state.ProcessState.SourceFile)}");
            ExecutionState newState = state.NewContext<ChainnerJob>();

            bool result = await chainner.ExecuteAsync(state.InputPath, newState.InputPath, state.ProcessState.ChnFile);
            if (result)
            {
                if (VerifyFiles(state.InputPath, newState.InputPath))
                {
                    await context.Scheduler.ScheduleJob<JPegXLJob>(newState, 7, true).ConfigureAwait(false);
                    End();
                    await Program.MayRespawn().ConfigureAwait(false);
                    return;
                }
            }
            context.Scheduler.AddErrorFile(state.ProcessState.SourceFile);
            await Program.MayRespawn().ConfigureAwait(false);
            End();
        }
        private bool VerifyFiles(string srcdir, string dstdir)
        {
            HashSet<string> infiles = new HashSet<string>(Directory.GetFiles(srcdir, "*", SearchOption.AllDirectories).
                    Where(Extensions.IsImage).Select(a=>Path.ChangeExtension(a.Substring(srcdir.Length+1), "png")),StringComparer.InvariantCultureIgnoreCase);
            HashSet<string> outfiles = new HashSet<string>(Directory.GetFiles(dstdir, "*", SearchOption.AllDirectories).
                Where(Extensions.IsImage).Select(a => Path.ChangeExtension(a.Substring(dstdir.Length + 1), "png")), StringComparer.InvariantCultureIgnoreCase);
            bool miss = false;
            foreach(string str in infiles)
            {
                if (!outfiles.Contains(str))
                {
                    miss = true;
                    Console.WriteLine($"Error:Missing file {str} in Chainner Destination directory".Pastel(Color.Red));
                }
            }

            return !miss;
        }
    }
}
