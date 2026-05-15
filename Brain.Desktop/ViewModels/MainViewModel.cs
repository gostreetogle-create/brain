using CommunityToolkit.Mvvm.ComponentModel;

namespace Brain.Desktop.ViewModels;

public partial class MainViewModel : ObservableObject
{
    [ObservableProperty] private int _selectedTab;

    public DashboardViewModel Dashboard { get; }
    public ChatViewModel Chat { get; }
    public DataViewerViewModel DataViewer { get; }
    public SettingsViewModel Settings { get; }

    public MainViewModel()
    {
        var baseDir = AppDomain.CurrentDomain.BaseDirectory;
        var brainDir = Path.GetFullPath(Path.Combine(baseDir, "..", "..", "..", ".."));
        if (!Directory.Exists(brainDir))
            brainDir = Path.GetFullPath(Path.Combine(baseDir, "..", "..", "..", "..", ".."));

        var dataDir = Path.Combine(brainDir, "brain_data");
        Directory.CreateDirectory(dataDir);

        var memoryDb = Path.Combine(dataDir, "brain.db");
        var envPath = Path.Combine(brainDir, ".env");

        // Читаем ключ
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

        // Инициализация сервисов
        var memory = new Services.MemoryService(memoryDb);
        var ai = new Services.AIService(apiKey, extractModel, chatModel);
        var processor = new Services.FileProcessor();
        var embeddings = new Services.EmbeddingService();

        var inboxDir = Path.Combine(dataDir, "inbox");
        var archiveDir = Path.Combine(dataDir, "archive");
        var errorsDir = Path.Combine(dataDir, "errors");

        var watcher = new Services.WatcherService(inboxDir, archiveDir, errorsDir, processor, ai, memory, embeddings);

        Dashboard = new DashboardViewModel(memory, ai, watcher, inboxDir);
        Chat = new ChatViewModel(ai, memory, embeddings);
        DataViewer = new DataViewerViewModel(memory);
        Settings = new SettingsViewModel(ai, envPath);
    }
}
