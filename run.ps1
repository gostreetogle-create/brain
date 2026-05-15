#Requires -Version 5.1
Add-Type -AssemblyName System.Windows.Forms
Add-Type -AssemblyName System.Drawing

$BrainDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$DataDir = Join-Path $BrainDir "brain_data"
$Inbox = Join-Path $DataDir "inbox"
$MemoryFile = Join-Path $DataDir "brain.jsonl"

if (!(Test-Path $DataDir)) { New-Item -ItemType Directory -Path $DataDir -Force | Out-Null }
if (!(Test-Path $Inbox)) { New-Item -ItemType Directory -Path $Inbox -Force | Out-Null }

function Show-Header {
    Clear-Host
    $docs = if (Test-Path $MemoryFile) { (Get-Content $MemoryFile -ErrorAction SilentlyContinue | Measure-Object -Line).Lines } else { 0 }
    $inboxCount = (Get-ChildItem $Inbox -File -ErrorAction SilentlyContinue).Count
    Write-Host "============================================" -ForegroundColor Cyan
    Write-Host "        BRAIN - Цифровой Сотрудник" -ForegroundColor Cyan
    Write-Host "--------------------------------------------" -ForegroundColor Cyan
    Write-Host "  Документов в памяти: $docs" -ForegroundColor Yellow
    Write-Host "  Файлов во Входящих:  $inboxCount" -ForegroundColor Yellow
    Write-Host "============================================" -ForegroundColor Cyan
}

function Show-Menu {
    Show-Header
    Write-Host ""
    Write-Host "  1 - Следить за папкой Входящие (watch)" -ForegroundColor Yellow
    Write-Host "  2 - Открыть чат" -ForegroundColor Yellow
    Write-Host "  3 - Обработать файл вручную" -ForegroundColor Yellow
    Write-Host "  4 - Поиск по базе знаний" -ForegroundColor Yellow
    Write-Host "  5 - Статистика системы" -ForegroundColor Yellow
    Write-Host "  6 - Найти связанные документы" -ForegroundColor Yellow
    Write-Host "  7 - Ночной ревью (самообучение)" -ForegroundColor Yellow
    Write-Host "  8 - Открыть папку Входящие" -ForegroundColor Yellow
    Write-Host "  0 - Выход" -ForegroundColor Red
    Write-Host ""
}

function Start-WatchMode {
    Show-Header
    Write-Host "Слежение запущено. Кидай файлы в папку:" -ForegroundColor Green
    Write-Host $Inbox -ForegroundColor Yellow
    Write-Host "Ctrl+C для остановки." -ForegroundColor DarkGray
    py -3 "$BrainDir\cli.py" watch
    $null = Read-Host "Нажми Enter..."
}

function Start-Chat {
    Show-Header
    Write-Host "Задай вопрос (exit - выход)" -ForegroundColor Green
    py -3 "$BrainDir\cli.py" chat
}

function Process-File {
    Show-Header
    $dialog = New-Object System.Windows.Forms.OpenFileDialog
    $dialog.Title = "Выберите файл для обработки"
    $dialog.Filter = "Все файлы (*.*)|*.*|PDF (*.pdf)|*.pdf|Excel (*.xlsx;*.xls)|*.xlsx;*.xls|Word (*.docx)|*.docx|Изображения (*.png;*.jpg;*.jpeg;*.tiff)|*.png;*.jpg;*.jpeg;*.tiff|Текст (*.txt;*.csv;*.json)|*.txt;*.csv;*.json"
    $dialog.InitialDirectory = [Environment]::GetFolderPath("Desktop")
    if ($dialog.ShowDialog() -eq "OK") {
        $file = $dialog.FileName
        Write-Host "Обработка: $file..." -ForegroundColor Yellow
        py -3 "$BrainDir\cli.py" process --file "$file"
        Write-Host "Готово!" -ForegroundColor Green
    }
    $null = Read-Host "Нажми Enter..."
}

function Search-Knowledge {
    Show-Header
    $query = Read-Host "Введи поисковый запрос"
    if ($query) {
        Clear-Host
        py -3 "$BrainDir\cli.py" search --query "$query"
    }
    $null = Read-Host "Нажми Enter..."
}

function Show-Stats {
    Show-Header
    py -3 "$BrainDir\cli.py" stats
    $null = Read-Host "Нажми Enter..."
}

function Show-Related {
    Show-Header
    $id = Read-Host "Введи ID документа"
    if ($id) {
        Clear-Host
        py -3 "$BrainDir\cli.py" related --doc-id "$id"
    }
    $null = Read-Host "Нажми Enter..."
}

function Open-Inbox {
    if (Test-Path $Inbox) { Invoke-Item $Inbox }
}

do {
    Show-Menu
    $choice = Read-Host "Выбери действие"
    switch ($choice) {
        "1" { Start-WatchMode }
        "2" { Start-Chat }
        "3" { Process-File }
        "4" { Search-Knowledge }
        "5" { Show-Stats }
        "6" { Show-Related }
        "7" {
            Show-Header
            Write-Host "Запуск ночного ревью..." -ForegroundColor Green
            py -3 "$BrainDir\cli.py" review
            Write-Host "Готово!" -ForegroundColor Green
            $null = Read-Host "Нажми Enter..."
        }
        "8" { Open-Inbox }
        "0" { Write-Host "До свидания!" -ForegroundColor Cyan; break }
        default { Write-Host "Неверный ввод!" -ForegroundColor Red; Start-Sleep 1 }
    }
} while ($choice -ne "0")