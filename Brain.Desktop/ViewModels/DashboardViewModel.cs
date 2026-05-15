using System.Windows.Input;
using System.Windows.Data;
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
    private readonly MainViewModel? _mainVm;

    [ObservableProperty] private int _documentCount;
    [ObservableProperty] private int _inboxCount;
    [ObservableProperty] private int _archiveCount;
    [ObservableProperty] private string _aiStatus = "ИИ: проверка...";
    [ObservableProperty] private string _aiStatusColor = "Gray";
    [ObservableProperty] private string _progressText = "Ожидание...";
    [ObservableProperty] private int _progressValue;
    [ObservableProperty] private bool _isWatching;
    [ObservableProperty] private string _watchToggleText = "▶ Автоматика: выкл";
    [ObservableProperty] private string _inboxButtonText = "📂 Открыть Входящие";
    [ObservableProperty] private string _aiShortStatus = "";
    private readonly object _logLock = new();
    public ObservableCollection<string> LogEntries { get; } = new();

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

    public DashboardViewModel(MemoryService memory, AIService ai, WatcherService watcher, string inboxDir, MainViewModel? mainVm = null)
    {
        BindingOperations.EnableCollectionSynchronization(LogEntries, _logLock);

        _memory = memory;
        _ai = ai;
        _watcher = watcher;
        _inboxDir = inboxDir;
        _mainVm = mainVm;

        _watcher.OnProgress += msg =>
        {
            ProgressText = msg;
            AddLog($"🔄 {msg}");
        };
        _watcher.OnLog += msg =>
        {
            NotifyTray("BRAIN", msg);
            AddLog(msg);
        };
        _watcher.OnFileProcessed += () =>
        {
            RefreshStats();
            NotifyTray("BRAIN", "Файл обработан");
            AddLog("✅ Файл обработан");
        };

        RefreshStats();
        _ = CheckAiAsync();
    }

    public void RefreshStats()
    {
        DocumentCount = _memory.Count();
        InboxCount = Directory.Exists(_inboxDir) ? Directory.GetFiles(_inboxDir).Length : 0;
        InboxButtonText = InboxCount > 0
            ? $"📂 Открыть Входящие ({InboxCount})"
            : "📂 Открыть Входящие";
    }

    [RelayCommand]
    private async Task CheckAiAsync()
    {
        AiStatus = "ИИ: проверка...";
        AiStatusColor = "Yellow";
        AiShortStatus = "🟡 проверка...";
        var result = await _ai.CheckConnectionAsync();
        AiStatus = result.Status == "ok" ? $"ИИ: подключено ({result.Model})" : $"ИИ: {result.Message}";
        AiStatusColor = result.Status == "ok" ? "LightGreen" : "Salmon";
        AiShortStatus = result.Status == "ok" ? "🟢 ИИ готов" : "🔴 ИИ недоступен";
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
            WatchToggleText = "▶ Автоматика: выкл";
            ProgressText = "Слежение остановлено";
        }
        else
        {
            _watcher.Start();
            IsWatching = true;
            WatchToggleText = "⏹ Автоматика: вкл";
            ProgressText = "Слежение активно";
        }
        AddLog($"ℹ️ Слежение: {(IsWatching ? "включено" : "выключено")}");
    }

    [RelayCommand]
    private void OpenChat()
    {
        if (_mainVm != null)
            _mainVm.SelectedTab = 1; // вкладка чата
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

    [RelayCommand]
    private void ClearLog() => LogEntries.Clear();

    private void AddLog(string msg)
    {
        var time = DateTime.Now.ToString("HH:mm:ss");
        LogEntries.Add($"[{time}] {msg}");
        if (LogEntries.Count > 1000)
            LogEntries.RemoveAt(0);
    }
}
