namespace ComicRecompress.Models;

[Serializable]
public class ProcessState
{
    public string SourceFile { get; set; }
    public string DestinationFile { get; set; }
    public string ChnFile { get; set; }
    public int WebComicMaxJoinSize { get; set; }
    public Dictionary<string, string> TemporaryPaths { get; set; } = new Dictionary<string, string>();
    public int JPEGXLThreads { get; set; }
    public int JPEGXLQuality { get; set; }
    public Mode Mode { get; set; }
}