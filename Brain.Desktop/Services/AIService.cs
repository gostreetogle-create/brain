using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Brain.Desktop.Models;

namespace Brain.Desktop.Services;

public class AIService
{
    private readonly HttpClient _http;
    private readonly OllamaService _ollama;
    private string _apiKey;
    private string _extractModel;
    public string ExtractModel => _extractModel;
    private string _chatModel;
    private string _backend = "openrouter"; // "openrouter" | "ollama"
    private string _ollamaUrl = "http://localhost:11434";

    private const string BaseUrl = "https://openrouter.ai/api/v1";

    public AIService(string apiKey, string extractModel, string chatModel)
    {
        _apiKey = apiKey;
        _extractModel = extractModel;
        _chatModel = chatModel;
        _http = new HttpClient { Timeout = TimeSpan.FromMinutes(5) };
        _http.DefaultRequestHeaders.Add("HTTP-Referer", "https://github.com/brain");
        _http.DefaultRequestHeaders.Add("X-Title", "Brain Desktop");
        _ollama = new OllamaService();
    }

    public void UpdateSettings(string apiKey, string extractModel, string chatModel)
    {
        _apiKey = apiKey;
        _extractModel = extractModel;
        _chatModel = chatModel;
    }

    public void SetBackend(string backend, string ollamaUrl = "http://localhost:11434")
    {
        _backend = backend;
        _ollamaUrl = ollamaUrl;
        _ollama.BaseUrl = ollamaUrl;
    }

    public async Task<AiHealthResult> CheckConnectionAsync()
    {
        // Если Ollama — проверяем локальный сервер
        if (_backend == "ollama")
        {
            var ok = await _ollama.IsAvailableAsync();
            if (ok) return new AiHealthResult { Status = "ok", Model = _extractModel, Message = "Ollama подключён" };
            return new AiHealthResult { Status = "error", Message = "Ollama не отвечает. Запустите Ollama." };
        }

        // OpenRouter
        if (string.IsNullOrEmpty(_apiKey))
            return new AiHealthResult { Status = "error", Message = "Нет API ключа" };

        try
        {
            var response = await SendChatRequestAsync(_extractModel,
                [new ChatMessage("user", "Ответь одним словом: ok")], maxTokens: 10);
            if (response != null)
                return new AiHealthResult { Status = "ok", Model = _extractModel, Message = "Подключено" };
            return new AiHealthResult { Status = "error", Message = "Нет ответа от API" };
        }
        catch (HttpRequestException)
        {
            return new AiHealthResult { Status = "error", Message = "Нет доступа к openrouter.ai" };
        }
        catch (Exception ex)
        {
            return new AiHealthResult { Status = "error", Message = ex.Message[..100] };
        }
    }

    public async Task<AnalysisResult?> AnalyzeDocumentAsync(string text, string filename)
    {
        var systemPrompt = "Ты — ИИ-аналитик документов. Извлеки структурированную информацию. Ответь ТОЛЬКО JSON.";
        var userMsg = $"Файл: {filename}\n\nТекст:\n{text[..Math.Min(text.Length, 50000)]}";

        try
        {
            string? response;

            if (_backend == "ollama")
            {
                response = await _ollama.ChatAsync(_extractModel, systemPrompt, userMsg);
            }
            else
            {
                response = await SendChatRequestAsync(_extractModel,
                [
                    new ChatMessage("system", systemPrompt),
                    new ChatMessage("user", userMsg)
                ], maxTokens: 4000);
            }

            if (response == null) return null;

            try
            {
                return JsonSerializer.Deserialize<AnalysisResult>(response);
            }
            catch
            {
                return new AnalysisResult { Summary = response[..Math.Min(response.Length, 500)], Uncertainty = 1.0 };
            }
        }
        catch (Exception ex)
        {
            throw new Exception($"Ошибка ИИ: {ex.Message}");
        }
    }

    public async Task<string?> ChatAsync(string query, string context)
    {
        string systemPrompt;
        string userMsg;

        if (string.IsNullOrEmpty(context))
        {
            systemPrompt = "Ты полезный ИИ-ассистент. База знаний пока пуста, отвечай на общие вопросы.";
            userMsg = query;
        }
        else
        {
            systemPrompt = "Ты — ИИ-ассистент, отвечающий на основе базы знаний компании. Отвечай кратко, ссылаясь на документы.";
            userMsg = $"Контекст:\n{context}\n\nВопрос: {query}";
        }

        if (_backend == "ollama")
        {
            return await _ollama.ChatAsync(_chatModel, systemPrompt, userMsg);
        }

        return await SendChatRequestAsync(_chatModel,
        [
            new ChatMessage("system", systemPrompt),
            new ChatMessage("user", userMsg)
        ], maxTokens: 2000);
    }

    public async Task<string?> SendChatRequestRaw(string model, List<ChatMessage> messages, int maxTokens)
    {
        return await SendChatRequestAsync(model, messages, maxTokens);
    }

    public async Task<List<string>> GetOllamaModelsAsync()
    {
        return await _ollama.GetModelsAsync();
    }

    public async Task<bool> IsOllamaAvailableAsync()
    {
        return await _ollama.IsAvailableAsync();
    }

    private async Task<string?> SendChatRequestAsync(string model, List<ChatMessage> messages, int maxTokens)
    {
        var request = new { model, messages, max_tokens = maxTokens, temperature = 0.1 };
        var json = JsonSerializer.Serialize(request);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
        _http.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);

        const int maxRetries = 3;
        for (int attempt = 1; attempt <= maxRetries; attempt++)
        {
            try
            {
                using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(5));
                var response = await _http.PostAsync($"{BaseUrl}/chat/completions", content, cts.Token);
                response.EnsureSuccessStatusCode();

                var responseJson = await response.Content.ReadAsStringAsync(cts.Token);
                using var doc = JsonDocument.Parse(responseJson);
                return doc.RootElement.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString();
            }
            catch (HttpRequestException) when (attempt < maxRetries)
            {
                await Task.Delay(TimeSpan.FromSeconds(attempt * 3));
            }
            catch (TaskCanceledException) when (attempt < maxRetries)
            {
                await Task.Delay(TimeSpan.FromSeconds(attempt * 3));
            }
        }
        throw new Exception("Сервер ИИ не ответил после 3 попыток. Попробуйте позже.");
    }
}

public class ChatMessage
{
    public string role { get; set; }
    public string content { get; set; }

    public ChatMessage(string role, string content)
    {
        this.role = role;
        this.content = content;
    }
}
