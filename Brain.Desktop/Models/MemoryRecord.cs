using System.Text.Json.Serialization;

namespace Brain.Desktop.Models;

public class MemoryRecord
{
    [JsonPropertyName("id")] public string Id { get; set; } = Guid.NewGuid().ToString("N")[..12];
    [JsonPropertyName("type")] public string Type { get; set; } = "document";
    [JsonPropertyName("timestamp")] public string Timestamp { get; set; } = DateTime.UtcNow.ToString("O");
    [JsonPropertyName("schema_version")] public string SchemaVersion { get; set; } = "1.0";
    [JsonPropertyName("doc_type")] public string DocType { get; set; } = "other";
    [JsonPropertyName("source_file")] public string SourceFile { get; set; } = "";
    [JsonPropertyName("source_hash")] public string SourceHash { get; set; } = "";
    [JsonPropertyName("summary")] public string Summary { get; set; } = "";
    [JsonPropertyName("uncertainty")] public double Uncertainty { get; set; } = 0.5;
    [JsonPropertyName("entities")] public List<EntityInfo> Entities { get; set; } = new();
    [JsonPropertyName("facts")] public List<FactInfo> Facts { get; set; } = new();
    [JsonPropertyName("tags")] public List<string> Tags { get; set; } = new();
    [JsonPropertyName("embedding")] public List<double> Embedding { get; set; } = new();
    [JsonIgnore] public float[] EmbeddingArray
    {
        get => Embedding.Select(d => (float)d).ToArray();
        set => Embedding = value.Select(f => (double)f).ToList();
    }
    [JsonPropertyName("relations")] public List<RelationInfo> Relations { get; set; } = new();
    [JsonPropertyName("agent_data")] public AgentData? AgentData { get; set; }
}

public class AgentData
{
    public string? SecretaryRaw { get; set; }
    public string? AnalystRaw { get; set; }
    public string? LibrarianRaw { get; set; }
    public string? Title { get; set; }
    public string? Importance { get; set; }
    public string? Department { get; set; }
}

public class EntityInfo
{
    [JsonPropertyName("id")] public string Id { get; set; } = "";
    [JsonPropertyName("type")] public string Type { get; set; } = "";
    [JsonPropertyName("name")] public string Name { get; set; } = "";
    [JsonPropertyName("role")] public string Role { get; set; } = "";
    [JsonPropertyName("details")] public string Details { get; set; } = "";
}

public class FactInfo
{
    [JsonPropertyName("predicate")] public string Predicate { get; set; } = "";
    [JsonPropertyName("object")] public string Object { get; set; } = "";
    [JsonPropertyName("confidence")] public double Confidence { get; set; } = 1.0;
}

public class RelationInfo
{
    [JsonPropertyName("target_id")] public string TargetId { get; set; } = "";
    [JsonPropertyName("similarity")] public double Similarity { get; set; } = 0;
    [JsonPropertyName("type")] public string Type { get; set; } = "similar_to";
}
