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

    private static System.Windows.Forms.NotifyIcon? _balloon;
    private static void NotifyTray(string title, string text)
    {
        try
        {
            if (_balloon == null)
            {
                _balloon = new System.Windows.Forms.NotifyIcon
                {
                    Icon = System.Drawing.SystemIcons.Information,
                    Visible = true
                };
            }
            _balloon.BalloonTipTitle = title;
            _balloon.BalloonTipText = text.Length > 200 ? text[..200] : text;
            _balloon.ShowBalloonTip(3000);
        }
        catch { }
    }

    public DashboardViewModel(MemoryService memory, AIService ai, WatcherService watcher, string inboxDir)
    {
        _memory = memory;
        _ai = ai;
        _watcher = watcher;
        _inboxDir = inboxDir;

        _watcher.OnProgress += msg => ProgressText = msg;
        _watcher.OnLog += msg => NotifyTray("BRAIN", msg);
        _watcher.OnFileProcessed += () =>
        {
            RefreshStats();
            NotifyTray("BRAIN", "Файл обработан");
        };

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
            Filter = "Все файлы (*.*)|*.*",
            InitialDirectory = Directory.Exists(_inboxDir) ? _inboxDir : Environment.GetFolderPath(Environment.SpecialFolder.Desktop)
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

    [RelayCommand]
    private void OpenDataFolder()
    {
        var dir = Path.GetDirectoryName(_inboxDir);
        if (dir != null && Directory.Exists(dir))
            System.Diagnostics.Process.Start("explorer.exe", dir);
    }
}
