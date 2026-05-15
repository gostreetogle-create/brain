using System.Text.Json;
using CommunityToolkit.Mvvm.ComponentModel;
using Brain.Desktop.Models;

namespace Brain.Desktop.ViewModels;

public partial class MainViewModel : ObservableObject
{
    [ObservableProperty] private int _selectedTab;

    public DashboardViewModel Dashboard { get; }
    public ChatViewModel Chat { get; }
    public DataViewerViewModel DataViewer { get; }
    public SettingsViewModel Settings { get; }
    public Services.MemoryService Memory { get; }

    // Путь к конфигу хранится рядом с EXE
    private static string ConfigPath => Path.Combine(
        AppDomain.CurrentDomain.BaseDirectory, "brain_config.json");

    public static string DefaultDataDir => Path.Combine(
        AppDomain.CurrentDomain.BaseDirectory, "brain_data");

    public MainViewModel()
    {
        // Загружаем или создаём конфиг
        var config = LoadOrCreateConfig();
        var dataDir = config.DataPath;
        Directory.CreateDirectory(dataDir);

        var memoryDb = Path.Combine(dataDir, "brain.db");
        var envPath = Path.Combine(dataDir, ".env");
        if (!File.Exists(envPath))
            envPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ".env");

        var apiKey = "";
        var extractModel = "deepseek/deepseek-v4-flash:free";
        var chatModel = "deepseek/deepseek-v4-flash:free";

        if (File.Exists(envPath))
        {
            foreach (var line in File.ReadLines(envPath))
            {
                if (line.StartsWith("OPENROUTER_API_KEY=")) apiKey = line.Split('=', 2)[1].Trim();
                if (line.StartsWith("EXTRACTOR_MODEL=")) extractModel = line.Split('=', 2)[1].Trim();
                if (line.StartsWith("CHAT_MODEL=")) chatModel = line.Split('=', 2)[1].Trim();
            }
        }

        Memory = new Services.MemoryService(memoryDb);

        // Миграция из brain.jsonl (рядом с EXE или в папке данных)
        var jsonlPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "brain.jsonl");
        if (!File.Exists(jsonlPath))
            jsonlPath = Path.Combine(dataDir, "brain.jsonl");
        if (File.Exists(jsonlPath) && Memory.Count() == 0)
        {
            MigrateFromJsonl(jsonlPath);
        }

        var ai = new Services.AIService(apiKey, extractModel, chatModel);
        var processor = new Services.FileProcessor();
        var embeddings = new Services.EmbeddingService();

        var inboxDir = Path.Combine(dataDir, "inbox");
        var archiveDir = Path.Combine(dataDir, "archive");
        var errorsDir = Path.Combine(dataDir, "errors");

        var watcher = new Services.WatcherService(inboxDir, archiveDir, errorsDir, processor, ai, Memory, embeddings);

        Dashboard = new DashboardViewModel(Memory, ai, watcher, inboxDir);
        Chat = new ChatViewModel(ai, Memory, embeddings);
        DataViewer = new DataViewerViewModel(Memory);
        Settings = new SettingsViewModel(ai, envPath, Memory, dataDir);
    }

    private void MigrateFromJsonl(string jsonlPath)
    {
        try
        {
            foreach (var line in File.ReadLines(jsonlPath))
            {
                if (string.IsNullOrWhiteSpace(line)) continue;
                try
                {
                    var record = JsonSerializer.Deserialize<MemoryRecord>(line);
                    if (record != null) Memory.Insert(record);
                }
                catch { }
            }
        }
        catch { }
    }

    private AppConfig LoadOrCreateConfig()
    {
        try
        {
            if (File.Exists(ConfigPath))
            {
                var json = File.ReadAllText(ConfigPath);
                return JsonSerializer.Deserialize<AppConfig>(json) ?? new AppConfig();
            }
        }
        catch { }
        return new AppConfig();
    }

    public static void SaveConfig(string dataPath)
    {
        var config = new AppConfig { DataPath = dataPath };
        File.WriteAllText(ConfigPath, JsonSerializer.Serialize(config));
    }

    private class AppConfig
    {
        public string DataPath { get; set; } = DefaultDataDir;
    }
}
