using System.Drawing;
using System.Linq.Expressions;
using System.Runtime.Intrinsics.Arm;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
using ComicRecompress.Models;
using Quartz;
using static Quartz.Logging.OperationName;

namespace ComicRecompress.Jobs;

public static class Extensions
{

    public class WaitJob
    {
        public Type JobType { get; set; }
        public ExecutionState State { get; set; }
        public int Priority { get; set; }
    }

    public static List<WaitJob> WaitingJobs { get; set; } = new List<WaitJob>();

    private static Task ScheduleJob(this IScheduler scheduler, Type t, string id, ExecutionState state, int priority)
    {
        IJobDetail job = JobBuilder.Create()
            .WithIdentity(id, "comicrecompress")
            .OfType(t)
            .UsingJobData("state", JsonSerializer.Serialize(state, SerializerGenerationContext.Options))
            .Build();

        ITrigger trigger = TriggerBuilder.Create()
            .WithIdentity(id, "comicrecompress")
            .WithPriority(priority)
            .StartNow()
            .Build();
        return scheduler.ScheduleJob(job, trigger);
    }
    private static readonly Regex Regex = new Regex(@"\d+", RegexOptions.Compiled);


    public static IEnumerable<(string, T)> OrderByNatural<T>(this IEnumerable<T> items, Func<T, string> selector, StringComparer? stringComparer = null)
    {
        var list = items.ToList();
        var maxDigits = list
            .SelectMany(i => Regex.Matches(selector(i))
                .Select(digitChunk => (int?)digitChunk.Value.Length))
            .Max() ?? 0;

        return list.OrderBy(i => Regex.Replace(selector(i), match => match.Value.PadLeft(maxDigits, '0')),
                stringComparer ?? StringComparer.CurrentCulture)
            .Select(a => (Regex.Matches(selector(a)).Select(a => a.Value).Last(), a)).ToList();

    }
    public static async Task ScheduleJob<T>(this IScheduler scheduler, ExecutionState state, int priority, bool disallow=false) where T : IJob
    {



        string id = disallow ? typeof(T).Name : typeof(T).Name+"_"+Guid.NewGuid().ToString();
        IJobDetail? j=await scheduler.GetJobDetail(new JobKey(id, "comicrecompress")).ConfigureAwait(false);
        if (j != null)
        {
            WaitingJobs.Add(new WaitJob { JobType = typeof(T), State = state, Priority = priority });
            return;
        }
        await scheduler.ScheduleJob(typeof(T), id, state, priority).ConfigureAwait(false);
        Task.Run(async () =>
        {
            await Task.Delay(200).ConfigureAwait(false);
            foreach (WaitJob wj in WaitingJobs.ToList())
            {
                IJobDetail? jd = await scheduler.GetJobDetail(new JobKey(wj.JobType.Name, "comicrecompress")).ConfigureAwait(false);
                if (jd == null)
                {
                    await scheduler.ScheduleJob(wj.JobType, wj.JobType.Name, wj.State, wj.Priority).ConfigureAwait(false);
                    WaitingJobs.Remove(wj);
                }
            }
        });

    }
    private static string[] imageextensions = new[] { ".png", ".jpeg", ".jpg", ".webp", ".gif", ".avif", ".jxl" };


    public static void AddErrorFile(this IScheduler scheduler, string file)
    {
        List<string> files = (List<string>)scheduler.Context["ErrorFiles"];
        lock (files)
        {
            files.Add(file);
        }
    }

    public static bool IsImage(string fileName)
    {
        string ext = Path.GetExtension(fileName).ToLowerInvariant();
        if (imageextensions.Contains(ext))
            return true;
        return false;
    }
    public static ExecutionState GetState(this IJobExecutionContext context)
    {
        return JsonSerializer.Deserialize<ExecutionState>(context.JobDetail.JobDataMap.GetString("state"), SerializerGenerationContext.Options);
    }
    public static ExecutionState NewContext<T>(this ExecutionState oldState)
    {
        ExecutionState context = new ExecutionState();
        context.InputPath = CreateTemporaryDirectory(oldState, typeof(T).Name, oldState.ProcessState.SourceFile);
        context.ProcessState = oldState.ProcessState;
        return context;
    }
    public static ExecutionState NewContext(this ExecutionState oldState, string name)
    {
        ExecutionState context = new ExecutionState();
        context.InputPath = CreateTemporaryDirectory(oldState, name, oldState.ProcessState.SourceFile);
        context.ProcessState = oldState.ProcessState;
        return context;
    }

    private static string MD5toHex(string input)
    {
        using MD5 md5 = MD5.Create();
        byte[] inputBytes = Encoding.ASCII.GetBytes(input);
        byte[] hashBytes = md5.ComputeHash(inputBytes);
        StringBuilder sb = new StringBuilder();
        foreach (byte t in hashBytes)
            sb.Append(t.ToString("X2"));
        return sb.ToString();

    }
    public static string CreateTemporaryDirectory(this ExecutionState state, string name, string originalfilename)
    {

        string hash = name + originalfilename;
        string tempPath = Path.GetTempPath();
        string directory = Path.Combine(tempPath, "rec_" + MD5toHex(hash));
        Directory.CreateDirectory(directory);
        state.ProcessState.TemporaryPaths.Add(name, directory);
        return directory;
    }
    public static void DeleteTemporaryDirectories(this ExecutionState state)
    {
        foreach (string directory in state.ProcessState.TemporaryPaths.Values)
            Directory.Delete(directory, true);
    }
    public static string GetTemporaryDirectory(this ExecutionState state, string name)
    {
        state.ProcessState.TemporaryPaths.TryGetValue(name, out string? directory);
        if (directory == null)
            throw new Exception("Temporary directory not found");
        return directory;
    }
    public static string GetTemporaryDirectory<T>(this ExecutionState state)
    {
        state.ProcessState.TemporaryPaths.TryGetValue(typeof(T).Name, out string? directory);
        if (directory == null)
            throw new Exception("Temporary directory not found");
        return directory;
    }

}