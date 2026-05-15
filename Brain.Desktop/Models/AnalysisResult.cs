using System.Text.Json.Serialization;

namespace Brain.Desktop.Models;

public class AnalysisResult
{
    [JsonPropertyName("doc_type")] public string DocType { get; set; } = "other";
    [JsonPropertyName("entities")] public List<EntityInfo> Entities { get; set; } = new();
    [JsonPropertyName("facts")] public List<FactInfo> Facts { get; set; } = new();
    [JsonPropertyName("tags")] public List<string> Tags { get; set; } = new();
    [JsonPropertyName("uncertainty")] public double Uncertainty { get; set; } = 0.5;
    [JsonPropertyName("summary")] public string Summary { get; set; } = "";
}

public class AiHealthResult
{
    public string Status { get; set; } = "error";
    public string Model { get; set; } = "";
    public string Message { get; set; } = "";
}
