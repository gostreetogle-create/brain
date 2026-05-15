# Собрать single-file EXE
Write-Host "Publishing BRAIN single-file EXE..." -ForegroundColor Cyan

$project = Join-Path $PSScriptRoot "Brain.Desktop.csproj"
$output = Join-Path $PSScriptRoot "publish"

dotnet publish $project `
    -c Release `
    -r win-x64 `
    --self-contained true `
    -p:PublishSingleFile=true `
    -p:IncludeNativeLibrariesForSelfExtract=true `
    -p:DebugType=embedded `
    -o $output

if ($LASTEXITCODE -eq 0) {
    Write-Host "OK! EXE at: $output\BRAIN.exe" -ForegroundColor Green
    Write-Host "Size: $((Get-Item "$output\BRAIN.exe").Length / 1MB) MB" -ForegroundColor Yellow
} else {
    Write-Host "Publish failed" -ForegroundColor Red
}
