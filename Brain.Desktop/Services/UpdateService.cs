using System.Diagnostics;
using System.Net.Http;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Brain.Desktop.Services;

public class UpdateService
{
    private readonly HttpClient _http;
    private const string RepoUrl = "https://api.github.com/repos/gostreetogle-create/brain/releases/latest";

    public string? DownloadUrl { get; private set; }
    public string? Changelog { get; private set; }
    public string? PublishedAt { get; private set; }

    public UpdateService()
    {
        _http = new HttpClient();
        _http.DefaultRequestHeaders.Add("User-Agent", "BRAIN-UpdateChecker/1.0");
    }

    public async Task<bool> CheckForUpdatesAsync()
    {
        try
        {
            var response = await _http.GetStringAsync(RepoUrl);
            var release = JsonSerializer.Deserialize<GitHubRelease>(response);
            if (release?.Assets == null || release.Assets.Count == 0) return false;

            DownloadUrl = release.Assets[0].BrowserDownloadUrl;
            Changelog = release.Body ?? "";
            PublishedAt = release.PublishedAt ?? "";

            // Если есть ссылка на скачивание — обновление доступно
            return !string.IsNullOrEmpty(DownloadUrl);
        }
        catch
        {
            return false;
        }
    }

    public async Task DownloadAndInstallAsync(string installerPath)
    {
        if (string.IsNullOrEmpty(DownloadUrl))
            throw new Exception("Нет ссылки для скачивания");

        var dir = Path.GetDirectoryName(installerPath) ?? Path.GetTempPath();
        Directory.CreateDirectory(dir);

        using var response = await _http.GetAsync(DownloadUrl, HttpCompletionOption.ResponseHeadersRead);
        response.EnsureSuccessStatusCode();

        using var fileStream = File.Create(installerPath);
        await response.Content.CopyToAsync(fileStream);

        Process.Start(new ProcessStartInfo
        {
            FileName = installerPath,
            UseShellExecute = true,
            Arguments = "/SILENT /CLOSEAPPLICATIONS /RESTARTAPPLICATIONS"
        });

        Environment.Exit(0);
    }

    private class GitHubRelease
    {
        [JsonPropertyName("tag_name")] public string? TagName { get; set; }
        [JsonPropertyName("body")] public string? Body { get; set; }
        [JsonPropertyName("published_at")] public string? PublishedAt { get; set; }
        [JsonPropertyName("assets")] public List<GitHubAsset>? Assets { get; set; }
    }

    private class GitHubAsset
    {
        [JsonPropertyName("browser_download_url")] public string? BrowserDownloadUrl { get; set; }
    }
}
