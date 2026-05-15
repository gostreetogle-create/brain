using System.Collections.ObjectModel;
using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Brain.Desktop.Services;

namespace Brain.Desktop.ViewModels;

public partial class ChatViewModel : ObservableObject
{
    private readonly AIService _ai;
    private readonly MemoryService _memory;

    [ObservableProperty] private string _inputText = "";
    [ObservableProperty] private bool _isWaiting;
    public ObservableCollection<ChatItem> Messages { get; } = new();

    public ChatViewModel(AIService ai, MemoryService memory)
    {
        _ai = ai;
        _memory = memory;
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
            // Search context
            var records = _memory.LoadAll();
            var context = "";
            var matches = records.Where(r =>
                r.Summary?.Contains(query, StringComparison.OrdinalIgnoreCase) == true ||
                r.Tags?.Any(t => t.Contains(query, StringComparison.OrdinalIgnoreCase)) == true ||
                r.Entities?.Any(e => e.Name?.Contains(query, StringComparison.OrdinalIgnoreCase) == true) == true
            ).Take(5).ToList();

            if (matches.Count > 0)
            {
                var parts = matches.Select(r =>
                    $"[{r.DocType}] {r.SourceFile}\nСущности: {string.Join(", ", r.Entities.Select(e => e.Name))}\nСводка: {r.Summary}");
                context = string.Join("\n---\n", parts);
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
