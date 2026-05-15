using System.Text;
using System.Text.Json;

namespace Brain.Desktop.Services;

public class OllamaService
{
    private readonly HttpClient _http;
    private const string DefaultBase = "http://localhost:11434";

    public string BaseUrl { get; set; } = DefaultBase;

    public OllamaService()
    {
        _http = new HttpClient { Timeout = TimeSpan.FromMinutes(10) };
    }

    public async Task<bool> IsAvailableAsync()
    {
        try
        {
            var resp = await _http.GetAsync($"{BaseUrl}/api/tags");
            return resp.IsSuccessStatusCode;
        }
        catch { return false; }
    }

    public async Task<List<string>> GetModelsAsync()
    {
        try
        {
            var json = await _http.GetStringAsync($"{BaseUrl}/api/tags");
            using var doc = JsonDocument.Parse(json);
            var models = doc.RootElement.GetProperty("models");
            return models.EnumerateArray().Select(m => m.GetProperty("name").GetString() ?? "").Where(n => !string.IsNullOrEmpty(n)).ToList();
        }
        catch { return new(); }
    }

    public async Task<string?> ChatAsync(string model, string systemPrompt, string userMsg)
    {
        var request = new
        {
            model,
            messages = new[]
            {
                new { role = "system", content = systemPrompt },
                new { role = "user", content = userMsg }
            },
            stream = false
        };

        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _http.PostAsync($"{BaseUrl}/api/chat", content);
        response.EnsureSuccessStatusCode();

        var responseJson = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(responseJson);
        return doc.RootElement.GetProperty("message").GetProperty("content").GetString();
    }

    public async Task<string?> GenerateAsync(string model, string prompt)
    {
        var request = new
        {
            model,
            prompt,
            stream = false
        };

        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var response = await _http.PostAsync($"{BaseUrl}/api/generate", content);
        response.EnsureSuccessStatusCode();

        var responseJson = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(responseJson);
        return doc.RootElement.GetProperty("response").GetString();
    }
}
