using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Brain.Desktop.Services;
using Microsoft.Win32;

namespace Brain.Desktop.ViewModels;

public partial class DashboardViewModel : ObservableObject
{
    private readonly MemoryService _memory;
    private readonly AIService _ai;
    private readonly WatcherService _watcher;
    private readonly string _inboxDir;

    [ObservableProperty] private int _documentCount;
    [ObservableProperty] private int _inboxCount;
    [ObservableProperty] private int _archiveCount;
    [ObservableProperty] private string _aiStatus = "ИИ: проверка...";
    [ObservableProperty] private string _aiStatusColor = "Gray";
    [ObservableProperty] private string _progressText = "Ожидание...";
    [ObservableProperty] private int _progressValue;
    [ObservableProperty] private bool _isWatching;
    [ObservableProperty] private string _watchButtonText = "👀 Следить за Входящими";

    public DashboardViewModel(MemoryService memory, AIService ai, WatcherService watcher)
    {
        _memory = memory;
        _ai = ai;
        _watcher = watcher;

        _inboxDir = Path.Combine(
            Path.GetDirectoryName(Path.GetDirectoryName(AppDomain.CurrentDomain.BaseDirectory)) ?? "",
            "brain_data", "inbox");

        _watcher.OnProgress += msg => ProgressText = msg;
        _watcher.OnLog += msg => { };
        _watcher.OnFileProcessed += RefreshStats;

        RefreshStats();
        _ = CheckAiAsync();
    }

    public void RefreshStats()
    {
        DocumentCount = _memory.Count();
        InboxCount = Directory.Exists(_inboxDir) ? Directory.GetFiles(_inboxDir).Length : 0;
    }

    [RelayCommand]
    private async Task CheckAiAsync()
    {
        AiStatus = "ИИ: проверка...";
        AiStatusColor = "Yellow";
        var result = await _ai.CheckConnectionAsync();
        AiStatus = result.Status == "ok" ? $"ИИ: подключено ({result.Model})" : $"ИИ: {result.Message}";
        AiStatusColor = result.Status == "ok" ? "LightGreen" : "Salmon";
    }

    [RelayCommand]
    private async Task ProcessFileAsync()
    {
        var dialog = new OpenFileDialog
        {
            Title = "Выберите файл для обработки",
            Filter = "Все файлы (*.*)|*.*"
        };

        if (dialog.ShowDialog() == true)
        {
            ProgressValue = 0;
            ProgressText = "Обработка...";
            await _watcher.ProcessFileAsync(dialog.FileName);
            RefreshStats();
        }
    }

    [RelayCommand]
    private void ToggleWatch()
    {
        if (IsWatching)
        {
            _watcher.Stop();
            IsWatching = false;
            WatchButtonText = "👀 Следить за Входящими";
            ProgressText = "Слежение остановлено";
        }
        else
        {
            _watcher.Start();
            IsWatching = true;
            WatchButtonText = "⏹ Остановить слежение";
            ProgressText = "Слежение активно";
        }
    }

    [RelayCommand]
    private void OpenInbox()
    {
        if (Directory.Exists(_inboxDir))
            System.Diagnostics.Process.Start("explorer.exe", _inboxDir);
    }
}
