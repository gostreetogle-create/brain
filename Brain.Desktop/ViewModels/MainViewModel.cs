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

    // Данные хранятся в %LocalAppData%/BRAIN/
    public static string AppDataDir => Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "BRAIN");

    private static string ConfigPath => Path.Combine(AppDataDir, "brain_config.json");

    public MainViewModel()
    {
        // Загружаем конфиг или используем AppData
        var config = LoadOrCreateConfig();
        var dataDir = config.DataPath;
        Directory.CreateDirectory(dataDir);

        var memoryDb = Path.Combine(dataDir, "brain.db");
        var envPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, ".env");
        if (!File.Exists(envPath))
            envPath = Path.Combine(dataDir, ".env");

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

        // Миграция из brain.jsonl
        MigrateFromJsonlIfNeeded(dataDir);

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

    private void MigrateFromJsonlIfNeeded(string dataDir)
    {
        if (Memory.Count() > 0) return;
        var jsonl = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "brain.jsonl");
        if (!File.Exists(jsonl))
            jsonl = Path.Combine(dataDir, "brain.jsonl");
        if (!File.Exists(jsonl)) return;

        try
        {
            foreach (var line in File.ReadLines(jsonl))
                if (!string.IsNullOrWhiteSpace(line))
                    try { Memory.Insert(JsonSerializer.Deserialize<MemoryRecord>(line)!); } catch { }
        }
        catch { }
    }

    private AppConfig LoadOrCreateConfig()
    {
        try
        {
            if (File.Exists(ConfigPath))
                return JsonSerializer.Deserialize<AppConfig>(File.ReadAllText(ConfigPath)) ?? new AppConfig();
        }
        catch { }
        return new AppConfig();
    }

    public static void SaveConfig(string dataPath)
    {
        Directory.CreateDirectory(AppDataDir);
        File.WriteAllText(ConfigPath, JsonSerializer.Serialize(new AppConfig { DataPath = dataPath }));
    }

    private class AppConfig
    {
        public string DataPath { get; set; } = Path.Combine(AppDataDir, "brain_data");
    }
}
