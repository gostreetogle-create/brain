using System.Text.Json;
using Brain.Desktop.Models;

namespace Brain.Desktop.Services;

public class MemoryService
{
    private readonly string _filePath;
    private readonly object _lock = new();

    public MemoryService(string filePath)
    {
        _filePath = filePath;
        var dir = Path.GetDirectoryName(filePath);
        if (dir != null) Directory.CreateDirectory(dir);
    }

    public List<MemoryRecord> LoadAll()
    {
        if (!File.Exists(_filePath)) return new();
        var records = new List<MemoryRecord>();
        lock (_lock)
        {
            foreach (var line in File.ReadLines(_filePath))
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                try
                {
                    var record = JsonSerializer.Deserialize<MemoryRecord>(line);
                    if (record != null) records.Add(record);
                }
                catch { }
            }
        }
        return records;
    }

    public void Append(MemoryRecord record)
    {
        var json = JsonSerializer.Serialize(record);
        lock (_lock)
        {
            File.AppendAllText(_filePath, json + "\n");
        }
    }

    public int Count()
    {
        if (!File.Exists(_filePath)) return 0;
        lock (_lock) return File.ReadLines(_filePath).Count(l => !string.IsNullOrWhiteSpace(l));
    }

    public StatsInfo GetStats()
    {
        var records = LoadAll();
        var stats = new StatsInfo
        {
            TotalDocuments = records.Count,
            ByType = records.GroupBy(r => r.DocType).ToDictionary(g => g.Key, g => g.Count()),
            TotalEntities = records.Sum(r => r.Entities.Count),
            TotalFacts = records.Sum(r => r.Facts.Count)
        };
        return stats;
    }
}
