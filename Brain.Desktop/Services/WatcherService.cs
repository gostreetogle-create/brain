using System.Collections.ObjectModel;
using System.IO;

namespace Brain.Desktop.Services;

public class WatcherService : IDisposable
{
    private FileSystemWatcher? _watcher;
    private readonly string _inboxDir;
    private readonly string _archiveDir;
    private readonly string _errorsDir;
    private readonly FileProcessor _processor;
    private readonly AIService _ai;
    private readonly MemoryService _memory;

    public event Action<string>? OnProgress;
    public event Action<string>? OnLog;
    public event Action? OnFileProcessed;

    public bool IsRunning { get; private set; }

    public WatcherService(string inboxDir, string archiveDir, string errorsDir,
                          FileProcessor processor, AIService ai, MemoryService memory)
    {
        _inboxDir = inboxDir;
        _archiveDir = archiveDir;
        _errorsDir = errorsDir;
        _processor = processor;
        _ai = ai;
        _memory = memory;

        Directory.CreateDirectory(_inboxDir);
        Directory.CreateDirectory(_archiveDir);
        Directory.CreateDirectory(_errorsDir);
    }

    public void Start()
    {
        if (IsRunning) return;
        _watcher = new FileSystemWatcher(_inboxDir)
        {
            EnableRaisingEvents = true,
            NotifyFilter = NotifyFilters.FileName | NotifyFilters.CreationTime
        };
        _watcher.Created += OnCreated;
        IsRunning = true;
        OnLog?.Invoke($"Слежение запущено: {_inboxDir}");
    }

    public void Stop()
    {
        if (_watcher != null)
        {
            _watcher.EnableRaisingEvents = false;
            _watcher.Created -= OnCreated;
            _watcher.Dispose();
            _watcher = null;
        }
        IsRunning = false;
        OnLog?.Invoke("Слежение остановлено");
    }

    private async void OnCreated(object sender, FileSystemEventArgs e)
    {
        // Wait for file to finish copying
        await Task.Delay(1000);

        var ext = Path.GetExtension(e.FullPath).ToLower();
        if (ext is ".tmp" or ".part" or ".crdownload") return;

        await ProcessFileAsync(e.FullPath);
    }

    public async Task ProcessFileAsync(string filePath)
    {
        var filename = Path.GetFileName(filePath);
        OnLog?.Invoke($"Обработка: {filename}");

        try
        {
            OnProgress?.Invoke("Извлечение текста...");
            var text = await Task.Run(() => _processor.ExtractText(filePath));
            if (string.IsNullOrWhiteSpace(text))
                throw new Exception("Пустой текст");

            OnProgress?.Invoke("Анализ ИИ...");
            var hash = _processor.ComputeHash(filePath);
            var analysis = await _ai.AnalyzeDocumentAsync(text, filename);
            if (analysis == null)
                throw new Exception("Ошибка анализа ИИ");

            OnProgress?.Invoke("Сохранение в память...");
            var record = new Models.MemoryRecord
            {
                Id = $"doc_{Guid.NewGuid().ToString("N")[..12]}",
                DocType = analysis.DocType,
                SourceFile = filename,
                SourceHash = hash,
                Summary = analysis.Summary,
                Uncertainty = analysis.Uncertainty,
                Entities = analysis.Entities,
                Facts = analysis.Facts,
                Tags = analysis.Tags
            };
            _memory.Append(record);

            // Move to archive
            var month = DateTime.Now.ToString("yyyy-MM");
            var destDir = Path.Combine(_archiveDir, month);
            Directory.CreateDirectory(destDir);
            var destPath = Path.Combine(destDir, filename);
            if (File.Exists(destPath)) File.Delete(destPath);
            File.Move(filePath, destPath);

            OnLog?.Invoke($"Готово: {filename} → {record.Id}");
            OnProgress?.Invoke("Готово");
            OnFileProcessed?.Invoke();
        }
        catch (Exception ex)
        {
            OnLog?.Invoke($"Ошибка: {filename} — {ex.Message}");
            OnProgress?.Invoke($"Ошибка: {ex.Message}");

            Directory.CreateDirectory(_errorsDir);
            var errPath = Path.Combine(_errorsDir, filename);
            if (File.Exists(filePath))
            {
                if (File.Exists(errPath)) File.Delete(errPath);
                File.Move(filePath, errPath);
            }
        }
    }

    public void Dispose()
    {
        Stop();
    }
}
