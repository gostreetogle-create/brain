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
    private readonly UpdateService _updater;

    [ObservableProperty] private string _apiKey = "";
    [ObservableProperty] private string _extractModel = "deepseek/deepseek-v4-flash:free";
    [ObservableProperty] private string _chatModel = "deepseek/deepseek-v4-flash:free";
    [ObservableProperty] private string _saveStatus = "";
    [ObservableProperty] private string _updateStatus = "Проверка...";
    [ObservableProperty] private bool _updateAvailable;

    public SettingsViewModel(AIService ai, string envPath)
    {
        _ai = ai;
        _envPath = envPath;
        _updater = new UpdateService();

        // Загрузка ключа через DPAPI
        ApiKey = LoadEncryptedKey() ?? "";
        LoadEnvModels();
        _ = CheckUpdateAsync();
    }

    private void LoadEnvModels()
    {
        if (!File.Exists(_envPath)) return;
        foreach (var line in File.ReadLines(_envPath))
        {
            if (line.StartsWith("EXTRACTOR_MODEL=")) ExtractModel = line.Split('=', 2)[1].Trim();
            if (line.StartsWith("CHAT_MODEL=")) ChatModel = line.Split('=', 2)[1].Trim();
        }
    }

    // DPAPI шифрование
    private static string KeyFilePath => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "BRAIN", "api.key");

    private static string? LoadEncryptedKey()
    {
        try
        {
            var path = KeyFilePath;
            if (!File.Exists(path)) return null;
            var encrypted = File.ReadAllBytes(path);
            var bytes = ProtectedData.Unprotect(encrypted, null, DataProtectionScope.CurrentUser);
            return Encoding.UTF8.GetString(bytes);
        }
        catch { return null; }
    }

    private static void SaveEncryptedKey(string key)
    {
        var dir = Path.GetDirectoryName(KeyFilePath)!;
        Directory.CreateDirectory(dir);
        var bytes = ProtectedData.Protect(Encoding.UTF8.GetBytes(key), null, DataProtectionScope.CurrentUser);
        File.WriteAllBytes(KeyFilePath, bytes);
    }

    private async Task CheckUpdateAsync()
    {
        try
        {
            var hasUpdate = await _updater.CheckForUpdatesAsync();
            if (hasUpdate)
            {
                var date = "";
                if (DateTime.TryParse(_updater.PublishedAt, out var dt))
                    date = dt.ToString("dd.MM.yyyy HH:mm");
                UpdateStatus = $"Доступно обновление от {date}";
                UpdateAvailable = true;
            }
            else
            {
                UpdateStatus = "Актуальная версия";
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
            // Сохраняем ключ через DPAPI (безопасно)
            if (!string.IsNullOrEmpty(ApiKey))
                SaveEncryptedKey(ApiKey);

            // Модели и пути — в .env (не чувствительные данные)
            File.WriteAllText(_envPath,
                $"EXTRACTOR_MODEL={ExtractModel}\n" +
                $"CHAT_MODEL={ChatModel}\n");
            _ai.UpdateSettings(ApiKey, ExtractModel, ChatModel);
            SaveStatus = "Сохранено!";
        }
        catch (Exception ex)
        {
            SaveStatus = $"Ошибка: {ex.Message}";
        }
    }
}
