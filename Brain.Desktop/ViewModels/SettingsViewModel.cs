using System.Windows.Input;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Brain.Desktop.Services;

namespace Brain.Desktop.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    private readonly AIService _ai;
    private readonly string _envPath;
    private readonly UpdateService _updater;

    [ObservableProperty] private string _apiKey = "";
    [ObservableProperty] private string _extractModel = "deepseek/deepseek-v4-flash:free";
    [ObservableProperty] private string _chatModel = "deepseek/deepseek-v4-flash:free";
    [ObservableProperty] private string _saveStatus = "";

    // Update
    [ObservableProperty] private string _currentVersion = "";
    [ObservableProperty] private string _updateStatus = "Проверка...";
    [ObservableProperty] private bool _updateAvailable;
    [ObservableProperty] private string _changelog = "";

    public SettingsViewModel(AIService ai, string envPath)
    {
        _ai = ai;
        _envPath = envPath;
        _updater = new UpdateService();

        CurrentVersion = _updater.CurrentVersion ?? "1.0.0.0";
        _ = CheckUpdateAsync();

        if (File.Exists(envPath))
        {
            foreach (var line in File.ReadLines(envPath))
            {
                if (line.StartsWith("OPENROUTER_API_KEY=")) ApiKey = line.Split('=', 2)[1].Trim();
                if (line.StartsWith("EXTRACTOR_MODEL=")) ExtractModel = line.Split('=', 2)[1].Trim();
                if (line.StartsWith("CHAT_MODEL=")) ChatModel = line.Split('=', 2)[1].Trim();
            }
        }
    }

    private async Task CheckUpdateAsync()
    {
        try
        {
            var hasUpdate = await _updater.CheckForUpdatesAsync();
            if (hasUpdate)
            {
                UpdateStatus = $"Доступна версия {_updater.LatestVersion}";
                UpdateAvailable = true;
                Changelog = _updater.Changelog ?? "";
            }
            else
            {
                UpdateStatus = "У вас актуальная версия";
                UpdateAvailable = false;
            }
        }
        catch
        {
            UpdateStatus = "Не удалось проверить";
        }
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
        catch (Exception ex)
        {
            UpdateStatus = $"Ошибка: {ex.Message}";
        }
    }

    [RelayCommand]
    private void Save()
    {
        try
        {
            File.WriteAllText(_envPath,
                $"OPENROUTER_API_KEY={ApiKey}\n" +
                $"EXTRACTOR_MODEL={ExtractModel}\n" +
                $"CHAT_MODEL={ChatModel}\n");
            _ai.UpdateSettings(ApiKey, ExtractModel, ChatModel);
            SaveStatus = "Сохранено! Перезапуск не требуется.";
        }
        catch (Exception ex)
        {
            SaveStatus = $"Ошибка: {ex.Message}";
        }
    }
}
