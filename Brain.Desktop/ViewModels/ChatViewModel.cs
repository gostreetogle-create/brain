using System.Collections.ObjectModel;
using System.Windows.Data;
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
    [ObservableProperty] private string _typingIndicator = "";
    [ObservableProperty] private int _messageCount;

    public ObservableCollection<ChatItem> Messages { get; } = new();

    private readonly object _chatLock = new();

    public ChatViewModel(AIService ai, MemoryService memory, EmbeddingService embeddings)
    {
        BindingOperations.EnableCollectionSynchronization(Messages, _chatLock);
        _ai = ai;
        _memory = memory;
        _embeddings = embeddings;
        Messages.CollectionChanged += (_, _) => MessageCount = Messages.Count;
    }

    [RelayCommand]
    private void CopyMessage(ChatItem item)
    {
        try
        {
            System.Windows.Clipboard.SetText(item.Text);
        }
        catch { }
    }

    [RelayCommand]
    private void ClearChat()
    {
        Messages.Clear();
    }

    [RelayCommand]
    private async Task SendAsync()
    {
        var query = InputText?.Trim();
        if (string.IsNullOrEmpty(query)) return;

        var time = DateTime.Now.ToString("HH:mm");
        Messages.Add(new ChatItem("Вы", query, "#89b4fa", time, true));
        InputText = "";
        IsWaiting = true;
        TypingIndicator = "Печатает...";

        try
        {
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

            TypingIndicator = "Жду ответ ИИ...";
            var response = await _ai.ChatAsync(query, context);
            var responseTime = DateTime.Now.ToString("HH:mm");
            Messages.Add(new ChatItem("Brain", response ?? "Нет ответа", "#a6e3a1", responseTime, false));
        }
        catch (Exception ex)
        {
            Messages.Add(new ChatItem("Brain", $"Ошибка: {ex.Message}", "#f38ba8", DateTime.Now.ToString("HH:mm"), false));
        }
        finally
        {
            IsWaiting = false;
            TypingIndicator = "";
        }
    }
}

public class ChatItem
{
    public string Sender { get; }
    public string Text { get; }
    public string Color { get; }
    public string Time { get; }
    public bool IsUser { get; }
    public string Initials { get; }

    public ChatItem(string sender, string text, string color, string time, bool isUser)
    {
        Sender = sender;
        Text = text;
        Color = color;
        Time = time;
        IsUser = isUser;
        Initials = isUser ? "U" : "AI";
    }
}
