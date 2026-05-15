#Requires -Version 5.1
Add-Type -AssemblyName System.Windows.Forms
Add-Type -AssemblyName System.Drawing
Add-Type -AssemblyName Microsoft.VisualBasic

$BrainDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$DataDir = Join-Path $BrainDir "brain_data"
$Inbox = Join-Path $DataDir "inbox"
$MemoryFile = Join-Path $DataDir "brain.jsonl"
$Archive = Join-Path $DataDir "archive"

foreach ($d in @($DataDir, $Inbox, $Archive)) {
    if (!(Test-Path $d)) { New-Item -ItemType Directory -Path $d -Force | Out-Null }
}

$form = New-Object System.Windows.Forms.Form
$form.Text = "BRAIN - Цифровой Сотрудник"
$form.Size = New-Object System.Drawing.Size(580, 680)
$form.StartPosition = "CenterScreen"
$form.FormBorderStyle = "FixedSingle"
$form.MaximizeBox = $false
$form.BackColor = [System.Drawing.Color]::FromArgb(30, 30, 45)
$form.Font = New-Object System.Drawing.Font("Segoe UI", 10)

$title = New-Object System.Windows.Forms.Label
$title.Text = "BRAIN"
$title.Font = New-Object System.Drawing.Font("Segoe UI", 22, [System.Drawing.FontStyle]::Bold)
$title.ForeColor = [System.Drawing.Color]::Cyan
$title.Size = New-Object System.Drawing.Size(540, 30)
$title.Location = New-Object System.Drawing.Point(20, 8)
$title.TextAlign = "MiddleCenter"
$form.Controls.Add($title)

$sub = New-Object System.Windows.Forms.Label
$sub.Text = "Цифровой Интеллектуальн\u044bй Сотрудник"
$sub.Font = New-Object System.Drawing.Font("Segoe UI", 11)
$sub.ForeColor = [System.Drawing.Color]::LightGray
$sub.Size = New-Object System.Drawing.Size(540, 22)
$sub.Location = New-Object System.Drawing.Point(20, 38)
$sub.TextAlign = "MiddleCenter"
$form.Controls.Add($sub)

# Top stats panel
$sp = New-Object System.Windows.Forms.Panel
$sp.Size = New-Object System.Drawing.Size(540, 40)
$sp.Location = New-Object System.Drawing.Point(20, 65)
$sp.BackColor = [System.Drawing.Color]::FromArgb(40, 40, 60)

$lblDocs = New-Object System.Windows.Forms.Label
$lblDocs.Text = "Документов: 0"
$lblDocs.Font = New-Object System.Drawing.Font("Segoe UI", 9)
$lblDocs.ForeColor = [System.Drawing.Color]::Yellow
$lblDocs.Size = New-Object System.Drawing.Size(120, 38)
$lblDocs.Location = New-Object System.Drawing.Point(8, 1)
$lblDocs.TextAlign = "MiddleLeft"
$sp.Controls.Add($lblDocs)

$lblInbox = New-Object System.Windows.Forms.Label
$lblInbox.Text = "Входящие: 0"
$lblInbox.Font = New-Object System.Drawing.Font("Segoe UI", 9)
$lblInbox.ForeColor = [System.Drawing.Color]::Yellow
$lblInbox.Size = New-Object System.Drawing.Size(100, 38)
$lblInbox.Location = New-Object System.Drawing.Point(130, 1)
$lblInbox.TextAlign = "MiddleLeft"
$sp.Controls.Add($lblInbox)

$lblArchive = New-Object System.Windows.Forms.Label
$lblArchive.Text = "Архив: 0"
$lblArchive.Font = New-Object System.Drawing.Font("Segoe UI", 9)
$lblArchive.ForeColor = [System.Drawing.Color]::Yellow
$lblArchive.Size = New-Object System.Drawing.Size(100, 38)
$lblArchive.Location = New-Object System.Drawing.Point(230, 1)
$lblArchive.TextAlign = "MiddleLeft"
$sp.Controls.Add($lblArchive)

$aiDot = New-Object System.Windows.Forms.Label
$aiDot.Text = "o"
$aiDot.Font = New-Object System.Drawing.Font("Segoe UI", 14)
$aiDot.ForeColor = [System.Drawing.Color]::Gray
$aiDot.Size = New-Object System.Drawing.Size(20, 38)
$aiDot.Location = New-Object System.Drawing.Point(340, 1)
$aiDot.TextAlign = "MiddleLeft"
$sp.Controls.Add($aiDot)

$aiLabel = New-Object System.Windows.Forms.Label
$aiLabel.Text = "ИИ: проверка..."
$aiLabel.Font = New-Object System.Drawing.Font("Segoe UI", 9)
$aiLabel.ForeColor = [System.Drawing.Color]::Gray
$aiLabel.Size = New-Object System.Drawing.Size(190, 38)
$aiLabel.Location = New-Object System.Drawing.Point(358, 1)
$aiLabel.TextAlign = "MiddleLeft"
$sp.Controls.Add($aiLabel)
$form.Controls.Add($sp)

# Progress section
$pp = New-Object System.Windows.Forms.Panel
$pp.Size = New-Object System.Drawing.Size(540, 45)
$pp.Location = New-Object System.Drawing.Point(20, 108)
$pp.BackColor = [System.Drawing.Color]::FromArgb(35, 35, 55)

$lblProgress = New-Object System.Windows.Forms.Label
$lblProgress.Text = "Ожидание..."
$lblProgress.Font = New-Object System.Drawing.Font("Segoe UI", 9)
$lblProgress.ForeColor = [System.Drawing.Color]::LightGray
$lblProgress.Size = New-Object System.Drawing.Size(530, 20)
$lblProgress.Location = New-Object System.Drawing.Point(5, 2)
$pp.Controls.Add($lblProgress)

$pb = New-Object System.Windows.Forms.ProgressBar
$pb.Size = New-Object System.Drawing.Size(530, 18)
$pb.Location = New-Object System.Drawing.Point(5, 23)
$pb.Minimum = 0
$pb.Maximum = 100
$pb.Value = 0
$pb.Style = "Continuous"
$pp.Controls.Add($pb)
$form.Controls.Add($pp)

# Output box
$outputBox = New-Object System.Windows.Forms.TextBox
$outputBox.Multiline = $true
$outputBox.ReadOnly = $true
$outputBox.ScrollBars = "Vertical"
$outputBox.Size = New-Object System.Drawing.Size(540, 70)
$outputBox.Location = New-Object System.Drawing.Point(20, 158)
$outputBox.BackColor = [System.Drawing.Color]::FromArgb(20, 20, 35)
$outputBox.ForeColor = [System.Drawing.Color]::LightGray
$outputBox.Font = New-Object System.Drawing.Font("Consolas", 9)
$outputBox.Text = "Добро пожаловать! В\u044bберите дей\u0441твие..."
$form.Controls.Add($outputBox)

# Progress helper
function Invoke-WithProgress {
    param([scriptblock]$Command)
    $outputBox.Clear()
    $pb.Value = 0
    $pb.Style = "Continuous"
    $job = Start-Job -ScriptBlock $Command
    while ($job.HasMoreData -or $job.State -eq "Running") {
        $msgs = $job | Receive-Job -ErrorAction SilentlyContinue
        foreach ($m in $msgs) {
            $line = "$m"
            if ($line -match '\[BRAIN_PROGRESS:(\d+)\] (.*)') {
                $pct = [int]$Matches[1]
                $pb.Value = $pct
                $lblProgress.Text = $Matches[2]
            } elseif ($line -match '\[BRAIN_PROGRESS:(\d+)\](.*)') {
                $pct = [int]$Matches[1]
                $pb.Value = $pct
                $lblProgress.Text = $Matches[2]
            }
            $outputBox.AppendText($line + [Environment]::NewLine)
            $outputBox.SelectionStart = $outputBox.TextLength
            $outputBox.ScrollToCaret()
        }
        Start-Sleep -Milliseconds 100
    }
    $msgs = $job | Receive-Job -ErrorAction SilentlyContinue
    foreach ($m in $msgs) {
        $line = "$m"
        if ($line -match '\[BRAIN_PROGRESS:(\d+)\](.*)') {
            $pb.Value = [int]$Matches[1]
            $lblProgress.Text = $Matches[2].Trim()
        }
        $outputBox.AppendText($line + [Environment]::NewLine)
    }
    Remove-Job $job -Force -ErrorAction SilentlyContinue
}

# Stats
function Update-Stats {
    $docs = if (Test-Path $MemoryFile) { (Get-Content $MemoryFile -ErrorAction SilentlyContinue | Measure-Object -Line).Lines } else { 0 }
    $ic = (Get-ChildItem $Inbox -File -ErrorAction SilentlyContinue).Count
    $ac = @(Get-ChildItem $Archive -Recurse -File -ErrorAction SilentlyContinue).Count
    $lblDocs.Text = "Документов: $docs"
    $lblInbox.Text = "Входящие: $ic"
    $lblArchive.Text = "Архив: $ac"
}

function Update-AIStatus {
    $result = py -3 "$BrainDir\health.py" 2>&1 | Out-String
    try {
        $data = $result | ConvertFrom-Json
        if ($data.status -eq "ok") {
            $aiDot.ForeColor = "Lime"
            $aiLabel.Text = "ИИ: $($data.model)"
            $aiLabel.ForeColor = "LightGreen"
        } else {
            $aiDot.ForeColor = "Red"
            $aiLabel.Text = "ИИ: $($data.message)"
            $aiLabel.ForeColor = "Salmon"
        }
    } catch {
        $aiDot.ForeColor = "Orange"
        $aiLabel.Text = "ИИ: ошибка проверки"
        $aiLabel.ForeColor = "Orange"
    }
}

function Add-Btn($t, $x, $y, $w, $h, $c, $a) {
    $b = New-Object System.Windows.Forms.Button
    $b.Text = $t
    $b.Size = New-Object System.Drawing.Size($w, $h)
    $b.Location = New-Object System.Drawing.Point($x, $y)
    $b.FlatStyle = "Flat"
    $b.FlatAppearance.BorderSize = 0
    $b.BackColor = $c
    $b.ForeColor = "White"
    $b.Font = New-Object System.Drawing.Font("Segoe UI", 10)
    $b.Cursor = "Hand"
    $b.Add_Click($a)
    return $b
}

$c1 = 20; $c2 = 285; $bw = 260; $bh = 35; $gap = 8
$r = 240

$form.Controls.Add((Add-Btn "Следить \u0437а Входящими" $c1 $r $bw $bh ([System.Drawing.Color]::FromArgb(0,120,200)) {
    $outputBox.Text = "Слежение \u0437апущено."
    $lblProgress.Text = "Слежение активно..."
    $pb.Style = "Marquee"
    Start-Job -ScriptBlock { param($d) Set-Location $d; py -3 "cli.py" watch } -ArgumentList $BrainDir | Out-Null
}))

$form.Controls.Add((Add-Btn "Откр\u044bть чат" $c2 $r $bw $bh ([System.Drawing.Color]::FromArgb(0,150,100)) {
    Start-Process py -ArgumentList "-3", "cli.py", "chat" -WorkingDirectory $BrainDir
}))

$r += $bh + $gap

$form.Controls.Add((Add-Btn "Обработать файл" $c1 $r $bw $bh ([System.Drawing.Color]::FromArgb(180,100,20)) {
    $dlg = New-Object System.Windows.Forms.OpenFileDialog
    $dlg.Title = "В\u044bберите файл"
    $dlg.Filter = "В\u0441е файл\u044b (*.*)|*.*"
    if ($dlg.ShowDialog() -eq "OK") {
        Invoke-WithProgress -Command { param($d,$f) Set-Location $d; py -3 "cli.py" process --file "$f" } -ArgumentList $BrainDir, $dlg.FileName
        Update-Stats
    }
}))

$form.Controls.Add((Add-Btn "Пои\u0441к по ба\u0437е" $c2 $r $bw $bh ([System.Drawing.Color]::FromArgb(100,60,180)) {
    $q = [Microsoft.VisualBasic.Interaction]::InputBox("Введите \u0437апро\u0441:", "Пои\u0441к", "")
    if ($q) {
        $outputBox.Clear()
        $r2 = py -3 "$BrainDir\cli.py" search --query "$q" 2>&1 | Out-String
        $outputBox.Text = $r2
    }
}))

$r += $bh + $gap

$form.Controls.Add((Add-Btn "Стати\u0441тика" $c1 $r $bw $bh ([System.Drawing.Color]::FromArgb(60,100,160)) {
    $outputBox.Clear()
    $outputBox.Text = py -3 "$BrainDir\cli.py" stats 2>&1 | Out-String
}))

$form.Controls.Add((Add-Btn "Самообучение" $c2 $r $bw $bh ([System.Drawing.Color]::FromArgb(80,60,120)) {
    Invoke-WithProgress -Command { param($d) Set-Location $d; py -3 "cli.py" review } -ArgumentList $BrainDir
}))

$r += $bh + $gap

$form.Controls.Add((Add-Btn "Откр\u044bть Входящие" $c1 $r 170 $bh ([System.Drawing.Color]::FromArgb(70,70,90)) {
    if (Test-Path $Inbox) { Invoke-Item $Inbox }
}))

$form.Controls.Add((Add-Btn "Откр\u044bть Архив" ($c1+180) $r 170 $bh ([System.Drawing.Color]::FromArgb(70,70,90)) {
    if (Test-Path $Archive) { Invoke-Item $Archive } else { $outputBox.Text = "Архив пу\u0441т." }
}))

$form.Controls.Add((Add-Btn "Следить и \u0441вернуть" $c2 $r $bw $bh ([System.Drawing.Color]::FromArgb(0,140,60)) {
    $outputBox.Text = "Слежение в фоне."
    $lblProgress.Text = "Слежение активно..."
    $pb.Style = "Marquee"
    $form.WindowState = "Minimized"
    Start-Job -ScriptBlock { param($d) Set-Location $d; py -3 "cli.py" watch } -ArgumentList $BrainDir | Out-Null
}))

$r += $bh + $gap

$form.Controls.Add((Add-Btn "Обновить" $c1 $r 170 $bh ([System.Drawing.Color]::FromArgb(70,70,90)) {
    Update-Stats; Update-AIStatus
    $outputBox.Clear()
    $outputBox.Text = "Обновлено."
}))

$form.Controls.Add((Add-Btn "Просмотр" ($c1+180) $r 170 $bh ([System.Drawing.Color]::FromArgb(40,100,140)) {
    Start-Process powershell -ArgumentList "-ExecutionPolicy Bypass", "-File", "$BrainDir\brain_viewer.ps1" -WorkingDirectory $BrainDir
}))

$form.Controls.Add((Add-Btn "В\u044bход" $c2 $r $bw $bh ([System.Drawing.Color]::FromArgb(120,40,40)) {
    $form.Close()
}))

$timer = New-Object System.Windows.Forms.Timer
$timer.Interval = 5000
$timer.Add_Tick({ Update-Stats })
$timer.Start()

$form.Add_Shown({ Update-Stats; Update-AIStatus })
[System.Windows.Forms.Application]::Run($form)
