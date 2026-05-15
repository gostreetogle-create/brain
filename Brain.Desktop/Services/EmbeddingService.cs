using SmartComponents.LocalEmbeddings;
using Brain.Desktop.Models;

namespace Brain.Desktop.Services;

public class EmbeddingService : IDisposable
{
    private readonly LocalEmbedder _embedder;

    public EmbeddingService()
    {
        _embedder = new LocalEmbedder();
    }

    public float[] ComputeEmbedding(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return new float[384]; // all-MiniLM-L6-v2 dimension

        var chunk = text.Length > 8000 ? text[..8000] : text;
        var embed = _embedder.Embed(chunk);
        return embed!.Values.ToArray();
    }

    public float CosineSimilarity(float[] a, float[] b)
    {
        if (a.Length != b.Length || a.Length == 0) return 0;
        double dot = 0, na = 0, nb = 0;
        for (int i = 0; i < a.Length; i++)
        {
            dot += a[i] * b[i];
            na += a[i] * a[i];
            nb += b[i] * b[i];
        }
        return (float)(dot / (Math.Sqrt(na) * Math.Sqrt(nb) + 1e-10));
    }

    public List<(MemoryRecord Record, float Score)> SearchSimilar(
        List<MemoryRecord> records, float[] queryEmbedding, int topK = 5, float minScore = 0.3f)
    {
        return records
            .Select(r => (Record: r, Score: CosineSimilarity(queryEmbedding, r.EmbeddingArray)))
            .Where(x => x.Score > minScore)
            .OrderByDescending(x => x.Score)
            .Take(topK)
            .ToList();
    }

    public void Dispose()
    {
        _embedder?.Dispose();
    }
}
