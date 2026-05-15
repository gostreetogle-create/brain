using LiteDB;
using Brain.Desktop.Models;

namespace Brain.Desktop.Services;

public class MemoryService : IDisposable
{
    private readonly LiteDatabase _db;
    private readonly ILiteCollection<MemoryRecord> _records;

    public MemoryService(string dbPath)
    {
        var dir = Path.GetDirectoryName(dbPath);
        if (dir != null) Directory.CreateDirectory(dir);

        _db = new LiteDatabase($"Filename={dbPath};Connection=shared;Timeout=5");
        _records = _db.GetCollection<MemoryRecord>("records");
        _records.EnsureIndex(r => r.DocType);
        _records.EnsureIndex(r => r.SourceFile);
        _records.EnsureIndex(r => r.Timestamp);
    }

    public void Insert(MemoryRecord record)
    {
        _records.Insert(record);
    }

    public MemoryRecord? FindById(string id)
    {
        return _records.FindById(id);
    }

    public List<MemoryRecord> GetAll()
    {
        return _records.FindAll().ToList();
    }

    public int Count()
    {
        return _records.Count();
    }

    public List<MemoryRecord> Search(string? docType = null, string? textFilter = null, string? tagFilter = null)
    {
        var query = _records.Query();

        if (!string.IsNullOrEmpty(docType) && docType != "Все")
            query = query.Where(r => r.DocType == docType);

        if (!string.IsNullOrEmpty(tagFilter))
            query = query.Where(r => r.Tags.Any(t => t.Contains(tagFilter, StringComparison.OrdinalIgnoreCase)));

        var results = query.ToList();

        if (!string.IsNullOrEmpty(textFilter))
        {
            var f = textFilter.ToLower();
            results = results.Where(r =>
                (r.Summary?.ToLower().Contains(f) == true) ||
                r.Entities.Any(e => e.Name?.ToLower().Contains(f) == true) ||
                r.Tags.Any(t => t.ToLower().Contains(f))
            ).ToList();
        }

        return results;
    }

    public List<MemoryRecord> GetRecent(int count = 10)
    {
        return _records.Query().OrderByDescending(r => r.Timestamp).Limit(count).ToList();
    }

    public StatsInfo GetStats()
    {
        var all = GetAll();
        return new StatsInfo
        {
            TotalDocuments = all.Count,
            ByType = all.GroupBy(r => r.DocType).ToDictionary(g => g.Key, g => g.Count()),
            TotalEntities = all.Sum(r => r.Entities.Count),
            TotalFacts = all.Sum(r => r.Facts.Count)
        };
    }

    public void Dispose()
    {
        _db?.Dispose();
    }
}
