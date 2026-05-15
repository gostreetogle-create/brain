namespace Brain.Desktop.Models;

public class StatsInfo
{
    public int TotalDocuments { get; set; }
    public int InboxCount { get; set; }
    public int ArchiveCount { get; set; }
    public Dictionary<string, int> ByType { get; set; } = new();
    public int TotalEntities { get; set; }
    public int TotalFacts { get; set; }
}
