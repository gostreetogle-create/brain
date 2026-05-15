using System.Text.Json;
using Brain.Desktop.Models;

namespace Brain.Desktop.Services;

public class MultiAgentService
{
    private readonly AIService _ai;

    public MultiAgentService(AIService ai)
    {
        _ai = ai;
    }

    public async Task<MultiAgentResult> AnalyzeAsync(string text, string filename)
    {
        var chunk = text.Length > 30000 ? text[..30000] : text;

        // Запускаем трёх агентов параллельно
        var taskSecretary = AskAgent(AgentRoles.Secretary, chunk, filename);
        var taskAnalyst = AskAgent(AgentRoles.Analyst, chunk, filename);
        var taskLibrarian = AskAgent(AgentRoles.Librarian, chunk, filename);

        await Task.WhenAll(taskSecretary, taskAnalyst, taskLibrarian);

        var result = new MultiAgentResult
        {
            SecretaryResult = taskSecretary.Result,
            AnalystResult = taskAnalyst.Result,
            LibrarianResult = taskLibrarian.Result,
        };

        // Собираем общий результат
        result.MergedDocType = result.LibrarianResult?.DocType ?? result.SecretaryResult?.DocType ?? "other";
        result.MergedTags = MergeTags(
            result.LibrarianResult?.Tags,
            result.SecretaryResult?.Tags
        );
        result.MergedEntities = result.SecretaryResult?.Entities ?? new();

        return result;
    }

    private async Task<AgentResponse?> AskAgent(AgentRoles role, string text, string filename)
    {
        var prompt = GetPrompt(role);
        var fullPrompt = $"{prompt}\n\nФайл: {filename}\n\nТекст:\n{text}";

        try
        {
            var response = await _ai.SendChatRequestRaw(_ai.ExtractModel,
            [
                new ChatMessage("system", prompt),
                new ChatMessage("user", fullPrompt)
            ], maxTokens: 2000);

            if (response == null) return null;

            // Пробуем распарсить JSON из ответа
            try
            {
                return JsonSerializer.Deserialize<AgentResponse>(response);
            }
            catch
            {
                return new AgentResponse { RawOutput = response };
            }
        }
        catch
        {
            return null;
        }
    }

    private static string GetPrompt(AgentRoles role) => role switch
    {
        AgentRoles.Secretary => 
            "Ты — ИИ-Секретарь. Извлеки из документа строгие факты в формате JSON.\n" +
            "Поля: doc_type (invoice|contract|claim|note|other), \n" +
            "entities: [{type, name, role}], \n" +
            "dates: [даты], \n" +
            "amounts: [{value, currency}], \n" +
            "tags: [теги].\n" +
            "Ответь ТОЛЬКО JSON, без пояснений.",

        AgentRoles.Analyst => 
            "Ты — ИИ-Аналитик. Проанализируй документ и напиши:\n" +
            "1. О чём этот документ (1 предложение)\n" +
            "2. К какому проекту/отделу относится\n" +
            "3. Какие риски или важные даты (сроки, штрафы, окончания)\n" +
            "4. Какие логические связи с другими документами могут быть\n\n" +
            "Формат JSON: {summary, department, risks:[], connections:[]}",

        AgentRoles.Librarian => 
            "Ты — ИИ-Библиотекарь. Организуй документ:\n" +
            "1. Присвой тип: invoice|contract|claim|note|other\n" +
            "2. Придумай короткое понятное название (до 80 символов)\n" +
            "3. Поставь 3-5 тегов\n" +
            "4. Определи важность: critical|normal|archive\n\n" +
            "Формат JSON: {doc_type, title, tags:[], importance, category}",
        _ => ""
    };

    private static List<string> MergeTags(params List<string>?[] tagLists)
    {
        var merged = new HashSet<string>();
        foreach (var tl in tagLists)
            if (tl != null)
                foreach (var t in tl)
                    merged.Add(t);
        return merged.ToList();
    }
}

public enum AgentRoles
{
    Secretary,   // Фактолог — даты, суммы, ИНН
    Analyst,     // Аналитик — смысл, риски, связи
    Librarian    // Библиотекарь — теги, название, категория
}

public class MultiAgentResult
{
    public AgentResponse? SecretaryResult { get; set; }
    public AgentResponse? AnalystResult { get; set; }
    public AgentResponse? LibrarianResult { get; set; }

    public string MergedDocType { get; set; } = "other";
    public List<string> MergedTags { get; set; } = new();
    public List<EntityInfo> MergedEntities { get; set; } = new();
}

public class AgentResponse
{
    public string? DocType { get; set; }
    public string? Title { get; set; }
    public string? Summary { get; set; }
    public string? Department { get; set; }
    public string? Importance { get; set; }
    public string? Category { get; set; }
    public string? RawOutput { get; set; }
    public List<string>? Tags { get; set; }
    public List<string>? Dates { get; set; }
    public List<string>? Risks { get; set; }
    public List<string>? Connections { get; set; }
    public List<EntityInfo>? Entities { get; set; }
    public List<AmountInfo>? Amounts { get; set; }
}

public class AmountInfo
{
    public double Value { get; set; }
    public string Currency { get; set; } = "";
}
