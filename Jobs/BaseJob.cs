using System.Drawing;
using Pastel;

namespace ComicRecompress.Jobs;

public class BaseJob 
{
    private static object _lck = new object();
    public Color Color { get; }
    public int ThreadNumber { get; private set; }
    public string ThreadString => ThreadNumber.ToString().PadLeft(2, '0');
    public BaseJob(Color color)
    {
        Color = color;
    }
    private void AddThread()
    {
        lock (_lck)
        {
            int tsk = 1;
            do
            {
                if (concurrentThreads.All(a => a != tsk))
                {
                    ThreadNumber = tsk;
                    break;
                }
                tsk++;
            } while (true);
            concurrentThreads.Add(ThreadNumber);
        }
    }
    private void RemoveThread()
    {
        lock (_lck)
        {
            concurrentThreads.Remove(ThreadNumber);
        }
    }
    public void Init(string title)
    {
        AddThread();
        string length = new string('\u2550', title.Length+6);
        Console.WriteLine($"\u2554{length}\u2557".Pastel(Color));
        Console.WriteLine($"\u2551{ThreadString}".Pastel(Color)+" " +title+"   \u2551".Pastel(Color));
        Console.WriteLine($"\u255A{length}\u255D".Pastel(Color));
    }

    public void End()
    {
        RemoveThread();
    }
    public void WriteLine(string msg)
    {
        Console.WriteLine($"[{ThreadString}]{msg}".Pastel(Color));
    }
    public void WriteError(string msg)
    {
        Console.WriteLine($"[{ThreadString}]{msg}".Pastel(Color.Red));
    }
    public static List<int> concurrentThreads = new List<int>();
}