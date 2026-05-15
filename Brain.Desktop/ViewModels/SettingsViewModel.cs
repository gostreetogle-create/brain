using System.Collections.ObjectModel;
using System.Security.Cryptography;
using System.Text;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Brain.Desktop.Services;

namespace Brain.Desktop.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    private readonly AIService _ai;
    private readonly string _envPath;
    private readonly MemoryService _memory;
    private readonly UpdateService _updater;
    private string _currentDataDir;

    [ObservableProperty] private string _apiKey = "";
    [ObservableProperty] private string _extractModel = "deepseek/deepseek-v4-flash:free";
    [ObservableProperty] private string _chatModel = "deepseek/deepseek-v4-flash:free";
    [ObservableProperty] private string _saveStatus = "";
    [ObservableProperty] private string _dbPath = "";
    [ObservableProperty] private string _migrationStatus = "";
    [ObservableProperty] private string _updateStatus = "Проверка...";
    [ObservableProperty] private bool _updateAvailable;

    // Ollama
    [ObservableProperty] private string _ollamaUrl = "http://localhost:11434";
    [ObservableProperty] private string _backend = "openrouter";
    [ObservableProperty] private string _ollamaStatus = "Проверка...";
    [ObservableProperty] private bool _ollamaAvailable;
    public List<string> Backends { get; } = ["openrouter", "ollama"];
    public ObservableCollection<string> OllamaModels { get; } = new();

    public SettingsViewModel(AIService ai, string envPath, MemoryService memory, string dataDir)
    {
        _ai = ai;
        _envPath = envPath;
        _memory = memory;
        _currentDataDir = dataDir;
        _updater = new UpdateService();

        DbPath = Path.Combine(dataDir, "brain.db");
        ApiKey = LoadEncryptedKey() ?? "";
        LoadEnv();
        _ = CheckUpdateAsync();
        _ = CheckOllamaAsync();
    }

    private void LoadEnv()
    {
        if (!File.Exists(_envPath)) return;
        foreach (var line in File.ReadLines(_envPath))
        {
            if (line.StartsWith("EXTRACTOR_MODEL=")) ExtractModel = line.Split('=', 2)[1].Trim();
            if (line.StartsWith("CHAT_MODEL=")) ChatModel = line.Split('=', 2)[1].Trim();
            if (line.StartsWith("BACKEND=")) Backend = line.Split('=', 2)[1].Trim();
            if (line.StartsWith("OLLAMA_URL=")) OllamaUrl = line.Split('=', 2)[1].Trim();
        }
    }

    private async Task CheckOllamaAsync()
    {
        OllamaStatus = "Проверка...";
        var ok = await _ai.IsOllamaAvailableAsync();
        OllamaAvailable = ok;
        OllamaStatus = ok ? "✅ Ollama доступен" : "❌ Ollama не найден";
        if (ok)
        {
            var models = await _ai.GetOllamaModelsAsync();
            OllamaModels.Clear();
            foreach (var m in models) OllamaModels.Add(m);
        }
    }

    [RelayCommand]
    private async Task RefreshOllamaAsync()
    {
        await CheckOllamaAsync();
    }

    [RelayCommand]
    private void SelectDbPath()
    {
        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            Title = "Выберите папку для базы данных",
            FileName = "brain.db",
            Filter = "LiteDB (*.db)|*.db"
        };
        if (dialog.ShowDialog() == true)
        {
            var dir = Path.GetDirectoryName(dialog.FileName)!;
            DbPath = dialog.FileName;
            _currentDataDir = dir;
            MainViewModel.SaveConfig(dir);
            MigrationStatus = "Путь сохранён. Перезапустите приложение.";
        }
    }

    [RelayCommand]
    private void ExportDb()
    {
        var dialog = new Microsoft.Win32.SaveFileDialog
        {
            Title = "Экспортировать базу знаний",
            Filter = "LiteDB (*.db)|*.db",
            FileName = $"brain_backup_{DateTime.Now:yyyy-MM-dd}.db"
        };
        if (dialog.ShowDialog() == true)
        {
            try { File.Copy(DbPath, dialog.FileName, true); MigrationStatus = "База экспортирована."; }
            catch (Exception ex) { MigrationStatus = $"Ошибка: {ex.Message}"; }
        }
    }

    [RelayCommand]
    private void ImportJsonl()
    {
        var dialog = new Microsoft.Win32.OpenFileDialog { Title = "Импортировать brain.jsonl", Filter = "JSONL (*.jsonl)|*.jsonl" };
        if (dialog.ShowDialog() == true)
        {
            try
            {
                var count = 0;
                foreach (var line in File.ReadLines(dialog.FileName))
                {
                    if (string.IsNullOrWhiteSpace(line)) continue;
                    try
                    {
                        var record = System.Text.Json.JsonSerializer.Deserialize<Brain.Desktop.Models.MemoryRecord>(line);
                        if (record != null) { _memory.Insert(record); count++; }
                    }
                    catch { }
                }
                MigrationStatus = $"Импортировано {count} записей.";
            }
            catch (Exception ex) { MigrationStatus = $"Ошибка: {ex.Message}"; }
        }
    }

    private static string KeyFilePath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "BRAIN", "api.key");

    private static string? LoadEncryptedKey()
    {
        try
        {
            if (!File.Exists(KeyFilePath)) return null;
            var bytes = ProtectedData.Unprotect(File.ReadAllBytes(KeyFilePath), null, DataProtectionScope.CurrentUser);
            return Encoding.UTF8.GetString(bytes);
        }
        catch { return null; }
    }

    private static void SaveEncryptedKey(string key)
    {
        var dir = Path.GetDirectoryName(KeyFilePath)!;
        Directory.CreateDirectory(dir);
        File.WriteAllBytes(KeyFilePath, ProtectedData.Protect(Encoding.UTF8.GetBytes(key), null, DataProtectionScope.CurrentUser));
    }

    private async Task CheckUpdateAsync()
    {
        try
        {
            var hasUpdate = await _updater.CheckForUpdatesAsync();
            if (hasUpdate)
            {
                var date = "";
                if (DateTime.TryParse(_updater.PublishedAt, out var dt)) date = dt.ToString("dd.MM.yyyy HH:mm");
                UpdateStatus = $"Доступно обновление от {date}";
                UpdateAvailable = true;
            }
            else { UpdateStatus = "Актуальная версия"; UpdateAvailable = false; }
        }
        catch { UpdateStatus = "Не удалось проверить"; }
    }

    [RelayCommand]
    private async Task DownloadUpdateAsync()
    {
        if (!UpdateAvailable) return;
        UpdateStatus = "Скачивание...";
        try
        {
            var installerPath = Path.Combine(Path.GetTempPath(), "BRAIN_Setup.exe");
            await _updater.DownloadAndInstallAsync(installerPath);
        }
        catch (Exception ex) { UpdateStatus = $"Ошибка: {ex.Message}"; }
    }

    [RelayCommand]
    private void Save()
    {
        try
        {
            if (!string.IsNullOrEmpty(ApiKey))
                SaveEncryptedKey(ApiKey);

            _ai.SetBackend(Backend, OllamaUrl);

            File.WriteAllText(_envPath,
                $"BACKEND={Backend}\n" +
                $"OLLAMA_URL={OllamaUrl}\n" +
                $"EXTRACTOR_MODEL={ExtractModel}\n" +
                $"CHAT_MODEL={ChatModel}\n");
            _ai.UpdateSettings(ApiKey, ExtractModel, ChatModel);
            SaveStatus = "Сохранено!";
        }
        catch (Exception ex) { SaveStatus = $"Ошибка: {ex.Message}"; }
    }
}
