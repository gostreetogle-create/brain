# BRAIN — Установщик для Windows
# Запускать с правами администратора

$AppName = "BRAIN"
$ExeName = "BRAIN.exe"
$ScriptDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$SourceExe = Join-Path $ScriptDir "Brain.Desktop" "publish" $ExeName
$InstallDir = Join-Path $env:ProgramFiles $AppName
$Desktop = [Environment]::GetFolderPath("Desktop")
$StartMenu = Join-Path ([Environment]::GetFolderPath("StartMenu")) "Programs" $AppName
$UninstallKey = "HKLM:\SOFTWARE\Microsoft\Windows\CurrentVersion\Uninstall\$AppName"

# Проверка прав
if (-NOT ([Security.Principal.WindowsPrincipal] [Security.Principal.WindowsIdentity]::GetCurrent()).IsInRole([Security.Principal.WindowsBuiltInRole] "Administrator")) {
    Write-Host "Запустите от имени администратора!" -ForegroundColor Red
    pause
    exit 1
}

# Проверка наличия EXE
if (!(Test-Path $SourceExe)) {
    Write-Host "Ошибка: не найден $SourceExe" -ForegroundColor Red
    Write-Host "Сначала соберите проект: cd Brain.Desktop && dotnet publish -c Release"
    pause
    exit 1
}

Write-Host "Установка $AppName..." -ForegroundColor Cyan

# 1. Создать папку в Program Files
New-Item -ItemType Directory -Path $InstallDir -Force | Out-Null

# 2. Копировать EXE
Copy-Item $SourceExe (Join-Path $InstallDir $ExeName) -Force

# 3. Копировать brain_data (если есть)
$DataDir = Join-Path $InstallDir "brain_data"
if (Test-Path (Join-Path $ScriptDir "brain_data")) {
    Copy-Item (Join-Path $ScriptDir "brain_data") $InstallDir -Recurse -Force
}

# 4. Копировать .env (если есть)
if (Test-Path (Join-Path $ScriptDir ".env")) {
    Copy-Item (Join-Path $ScriptDir ".env") $InstallDir -Force
}

# 5. Ярлык на рабочем столе
$ws = New-Object -ComObject WScript.Shell
$lnk = $ws.CreateShortcut((Join-Path $Desktop "$AppName.lnk"))
$lnk.TargetPath = Join-Path $InstallDir $ExeName
$lnk.WorkingDirectory = $InstallDir
$lnk.Description = "BRAIN - Cifrovoj Sotrudnik"
$lnk.Save()

# 6. Ярлык в Пуск
New-Item -ItemType Directory -Path $StartMenu -Force | Out-Null
$lnk = $ws.CreateShortcut((Join-Path $StartMenu "$AppName.lnk"))
$lnk.TargetPath = Join-Path $InstallDir $ExeName
$lnk.WorkingDirectory = $InstallDir
$lnk.Description = "BRAIN - Cifrovoj Sotrudnik"
$lnk.Save()

# 7. Запись в Установку/Удаление программ
$uninstallString = "cmd /c `"$InstallDir\uninstall.ps1`""
$uninstallContent = @'
# Uninstall BRAIN
$dir = "___INSTALL_DIR___"
$name = "___APP_NAME___"
$desktop = "___DESKTOP___"
$startMenu = "___START_MENU___"
$regKey = "___REG_KEY___"

Remove-Item $dir -Recurse -Force -ErrorAction SilentlyContinue
Remove-Item (Join-Path $desktop "$name.lnk") -Force -ErrorAction SilentlyContinue
Remove-Item $startMenu -Recurse -Force -ErrorAction SilentlyContinue
Remove-Item $regKey -Force -ErrorAction SilentlyContinue
Write-Host "$name deleted." -ForegroundColor Green
'@

$uninstallContent = $uninstallContent.Replace("___INSTALL_DIR___", $InstallDir)
$uninstallContent = $uninstallContent.Replace("___APP_NAME___", $AppName)
$uninstallContent = $uninstallContent.Replace("___DESKTOP___", $Desktop)
$uninstallContent = $uninstallContent.Replace("___START_MENU___", $StartMenu)
$uninstallContent = $uninstallContent.Replace("___REG_KEY___", $UninstallKey)
Set-Content -Path (Join-Path $InstallDir "uninstall.ps1") -Value $uninstallContent -Encoding UTF8

New-Item -Path $UninstallKey -Force | Out-Null
Set-ItemProperty -Path $UninstallKey -Name "DisplayName" -Value $AppName
Set-ItemProperty -Path $UninstallKey -Name "DisplayVersion" -Value "1.0.0"
Set-ItemProperty -Path $UninstallKey -Name "Publisher" -Value "BRAIN"
Set-ItemProperty -Path $UninstallKey -Name "InstallLocation" -Value $InstallDir
Set-ItemProperty -Path $UninstallKey -Name "UninstallString" -Value "powershell -ExecutionPolicy Bypass -File `"$InstallDir\uninstall.ps1`""
Set-ItemProperty -Path $UninstallKey -Name "DisplayIcon" -Value (Join-Path $InstallDir $ExeName)
Set-ItemProperty -Path $UninstallKey -Name "NoModify" -Value 1
Set-ItemProperty -Path $UninstallKey -Name "NoRepair" -Value 1

Write-Host ""
Write-Host "Установка завершена!" -ForegroundColor Green
Write-Host "BRAIN установлен в: $InstallDir" -ForegroundColor Yellow
Write-Host "Ярлык на рабочем столе: $AppName.lnk" -ForegroundColor Yellow
Write-Host "Для удаления: Параметры → Приложения → BRAIN → Удалить" -ForegroundColor Yellow
Write-Host ""
pause
