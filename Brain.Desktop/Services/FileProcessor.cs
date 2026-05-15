using System.Text;
using System.Xml.Linq;
using UglyToad.PdfPig;
using ExcelDataReader;

namespace Brain.Desktop.Services;

public class FileProcessor
{
    public FileProcessor()
    {
        Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);
    }

    public string ExtractText(string filePath)
    {
        var ext = Path.GetExtension(filePath).ToLower();
        var size = new FileInfo(filePath).Length;
        if (size > 50 * 1024 * 1024)
            throw new Exception("Файл больше 50 MB");

        return ext switch
        {
            ".txt" or ".md" or ".csv" or ".json" or ".xml" or ".html" or ".htm"
                => File.ReadAllText(filePath, Encoding.UTF8),

            ".pdf" => ExtractPdf(filePath),
            ".docx" => ExtractDocx(filePath),
            ".pptx" => ExtractPptx(filePath),
            ".xlsx" => ExtractXlsx(filePath),
            ".xls" => ExtractXls(filePath),
            ".png" or ".jpg" or ".jpeg" or ".tiff" or ".bmp" => ExtractImage(filePath),
            _ => throw new Exception($"Неподдерживаемый формат: {ext}")
        };
    }

    private string ExtractPdf(string path)
    {
        using var pdf = PdfDocument.Open(path);
        var sb = new StringBuilder();
        foreach (var page in pdf.GetPages())
            sb.AppendLine(page.Text);
        var text = sb.ToString().Trim();
        if (string.IsNullOrEmpty(text))
            throw new Exception("PDF не содержит текста (возможно, это сканированный документ).");
        return text;
    }

    private string ExtractDocx(string path)
    {
        using var archive = System.IO.Compression.ZipFile.OpenRead(path);
        var entry = archive.GetEntry("word/document.xml")
            ?? throw new Exception("Неверный формат DOCX");

        using var reader = new StreamReader(entry.Open());
        var xml = reader.ReadToEnd();
        var doc = XDocument.Parse(xml);
        var texts = doc.Descendants()
            .Where(e => e.Name.LocalName == "t")
            .Select(e => e.Value);
        return string.Join("", texts);
    }

    private string ExtractPptx(string path)
    {
        using var archive = System.IO.Compression.ZipFile.OpenRead(path);
        var sb = new StringBuilder();
        foreach (var entry in archive.Entries
            .Where(e => e.FullName.StartsWith("ppt/slides/slide") && e.FullName.EndsWith(".xml")))
        {
            using var reader = new StreamReader(entry.Open());
            var xml = reader.ReadToEnd();
            var doc = XDocument.Parse(xml);
            var texts = doc.Descendants()
                .Where(e => e.Name.LocalName == "t")
                .Select(e => e.Value);
            sb.AppendLine(string.Join(" ", texts));
        }
        return sb.ToString().Trim();
    }

    private string ExtractXlsx(string path)
    {
        using var stream = File.OpenRead(path);
        using var reader = ExcelReaderFactory.CreateOpenXmlReader(stream);
        var sb = new StringBuilder();
        var result = reader.AsDataSet();
        foreach (System.Data.DataTable table in result.Tables)
        {
            foreach (System.Data.DataRow row in table.Rows)
            {
                var cells = row.ItemArray.Select(c => c?.ToString() ?? "").Where(c => !string.IsNullOrEmpty(c));
                sb.AppendLine(string.Join("\t", cells));
            }
        }
        return sb.ToString().Trim();
    }

    private string ExtractXls(string path)
    {
        using var stream = File.OpenRead(path);
        using var reader = ExcelReaderFactory.CreateBinaryReader(stream);
        var sb = new StringBuilder();
        var result = reader.AsDataSet();
        foreach (System.Data.DataTable table in result.Tables)
        {
            foreach (System.Data.DataRow row in table.Rows)
            {
                var cells = row.ItemArray.Select(c => c?.ToString() ?? "").Where(c => !string.IsNullOrEmpty(c));
                sb.AppendLine(string.Join("\t", cells));
            }
        }
        return sb.ToString().Trim();
    }

    private string _tesseractExe = "tesseract";

    private async Task EnsureTesseractAsync()
    {
        // Check if available
        try
        {
            using var proc = new System.Diagnostics.Process
            {
                StartInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = _tesseractExe,
                    Arguments = "--version",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };
            proc.Start();
            proc.WaitForExit(2000);
            if (proc.ExitCode == 0) return; // already installed
        }
        catch { }

        // Try local bundled version
        var localExe = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "tesseract", "tesseract.exe");
        if (File.Exists(localExe))
        {
            _tesseractExe = localExe;
            return;
        }

        // Download Tesseract automatically
        var tessDir = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "tesseract");
        Directory.CreateDirectory(tessDir);

        var zipUrl = "https://github.com/UB-Mannheim/tesseract/releases/download/v5.5.0.20241111/tesseract-ocr-w64-setup-5.5.0.20241111.exe";
        var installerPath = Path.Combine(tessDir, "tesseract_setup.exe");

        using (var http = new HttpClient())
        {
            http.Timeout = TimeSpan.FromMinutes(5);
            var response = await http.GetAsync(zipUrl, HttpCompletionOption.ResponseHeadersRead);
            response.EnsureSuccessStatusCode();
            using var fs = File.Create(installerPath);
            await response.Content.CopyToAsync(fs);
        }

        // Silent install to local folder
        var installArgs = $"/VERYSILENT /SUPPRESSMSGBOXES /NORESTART /DIR=\"{tessDir}\"";
        using (var proc = System.Diagnostics.Process.Start(installerPath, installArgs))
        {
            proc?.WaitForExit(120000);
        }

        var installedExe = Path.Combine(tessDir, "tesseract.exe");
        if (File.Exists(installedExe))
        {
            _tesseractExe = installedExe;
        }
        else
        {
            _tesseractExe = "tesseract";
        }

        // Cleanup installer
        try { File.Delete(installerPath); } catch { }
    }

    private string ExtractImage(string path)
    {
        EnsureTesseractAsync().GetAwaiter().GetResult();

        var outPath = Path.GetTempFileName();
        try
        {
            var psi = new System.Diagnostics.ProcessStartInfo
            {
                FileName = _tesseractExe,
                Arguments = $"\"{path}\" \"{outPath}\" -l rus+eng",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };
            using var proc = System.Diagnostics.Process.Start(psi)!;
            proc.WaitForExit(30000);

            var resultPath = outPath + ".txt";
            if (!File.Exists(resultPath))
                throw new Exception("Tesseract не смог распознать текст.");

            var text = File.ReadAllText(resultPath, Encoding.UTF8).Trim();
            return string.IsNullOrEmpty(text)
                ? throw new Exception("Tesseract не нашёл текст на изображении.")
                : text;
        }
        finally
        {
            if (File.Exists(outPath)) File.Delete(outPath);
            if (File.Exists(outPath + ".txt")) File.Delete(outPath + ".txt");
        }
    }

    public string ComputeHash(string path)
    {
        using var sha = System.Security.Cryptography.SHA256.Create();
        using var stream = File.OpenRead(path);
        return Convert.ToHexStringLower(sha.ComputeHash(stream));
    }
}
