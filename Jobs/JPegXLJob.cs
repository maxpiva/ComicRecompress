﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Channels;
using System.Threading.Tasks;
using ComicRecompress.Models;
using ComicRecompress.Services;
using Quartz;

namespace ComicRecompress.Jobs
{
    public class JPegXLJob : BaseJob, IJob
    {
        private readonly JpegXL jpeg;
        public JPegXLJob() : base(Color.Yellow)
        {
            jpeg =new JpegXL(this);
        }

        public async Task Execute(IJobExecutionContext context)
        {
            ExecutionState state = context.GetState();
            Init($"Converting to JPG XL {state.ProcessState.SourceFile}");
            ExecutionState newState = state.NewContext<JPegXLJob>();
            bool breaking=false;
            await Parallel.ForEachAsync<string>(Directory.GetFiles(state.InputPath, "*.png", SearchOption.AllDirectories), new ParallelOptions { MaxDegreeOfParallelism = 4 }, 
                async (file, token) =>
            {
                if (breaking)
                    return;
                string relative = file.Substring(state.InputPath.Length + 1);
                WriteLine($"Converting {relative}");
                string dest2 = Path.Combine(newState.InputPath, relative);
                dest2 = Path.ChangeExtension(dest2, "jxl");
                Directory.CreateDirectory(Path.GetDirectoryName(dest2));
                bool res = await jpeg.CompressAsync(file, dest2, state.ProcessState).ConfigureAwait(false);
                if (!res)
                {
                    breaking = true;
                }
            }).ConfigureAwait(false);
            if (breaking)
            {
                context.Scheduler.AddErrorFile(state.ProcessState.SourceFile);
            }
            else
            {
                await context.Scheduler.ScheduleJob<CompressJob>(newState, 8).ConfigureAwait(false);
            }
            End();
        }
    }
}