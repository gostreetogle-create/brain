using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Brain.Desktop.Services;

namespace Brain.Desktop.ViewModels;

public partial class ChatViewModel : ObservableObject
{
    private readonly AIService _ai;
    private readonly MemoryService _memory;
    private readonly EmbeddingService _embeddings;

    [ObservableProperty] private string _inputText = "";
    [ObservableProperty] private bool _isWaiting;
    public ObservableCollection<ChatItem> Messages { get; } = new();

    public ChatViewModel(AIService ai, MemoryService memory, EmbeddingService embeddings)
    {
        _ai = ai;
        _memory = memory;
        _embeddings = embeddings;
    }

    [RelayCommand]
    private async Task SendAsync()
    {
        var query = InputText?.Trim();
        if (string.IsNullOrEmpty(query)) return;

        Messages.Add(new ChatItem("Вы", query, "#89b4fa"));
        InputText = "";
        IsWaiting = true;

        try
        {
            // Векторный поиск по смыслу
            var queryEmbedding = await Task.Run(() => _embeddings.ComputeEmbedding(query));
            var allRecords = _memory.GetAll();
            var similar = _embeddings.SearchSimilar(allRecords, queryEmbedding, topK: 5, minScore: 0.25f);

            string context;
            if (similar.Count > 0)
            {
                var parts = similar.Select(x =>
                    $"[{x.Record.DocType}] {x.Record.SourceFile} (схожесть: {x.Score:P1})\n" +
                    $"Сущности: {string.Join(", ", x.Record.Entities.Select(e => e.Name))}\n" +
                    $"Сводка: {x.Record.Summary}");
                context = string.Join("\n---\n", parts);
            }
            else
            {
                context = "";
            }

            var response = await _ai.ChatAsync(query, context);
            Messages.Add(new ChatItem("Brain", response ?? "Нет ответа", "#a6e3a1"));
        }
        catch (Exception ex)
        {
            Messages.Add(new ChatItem("Brain", $"Ошибка: {ex.Message}", "#f38ba8"));
        }
        finally
        {
            IsWaiting = false;
        }
    }
}

public class ChatItem
{
    public string Sender { get; }
    public string Text { get; }
    public string Color { get; }

    public ChatItem(string sender, string text, string color)
    {
        Sender = sender;
        Text = text;
        Color = color;
    }
}
