#Requires -Version 5.1
Add-Type -AssemblyName System.Windows.Forms
Add-Type -AssemblyName System.Drawing

$BrainDir = Split-Path -Parent $MyInvocation.MyCommand.Path
$MemoryFile = Join-Path (Join-Path $BrainDir "brain_data") "brain.jsonl"

$form = New-Object System.Windows.Forms.Form
$form.Text = "BRAIN - Data Viewer"
$form.Size = New-Object System.Drawing.Size(950, 650)
$form.StartPosition = "CenterScreen"
$form.BackColor = [System.Drawing.Color]::FromArgb(30, 30, 45)
$form.Font = New-Object System.Drawing.Font("Segoe UI", 10)

# Title
$title = New-Object System.Windows.Forms.Label
$title.Text = "BRAIN - Data Viewer"
$title.Font = New-Object System.Drawing.Font("Segoe UI", 16, [System.Drawing.FontStyle]::Bold)
$title.ForeColor = [System.Drawing.Color]::Cyan
$title.Size = New-Object System.Drawing.Size(930, 30)
$title.Location = New-Object System.Drawing.Point(10, 8)
$form.Controls.Add($title)

# Toolbar
$toolbar = New-Object System.Windows.Forms.Panel
$toolbar.Size = New-Object System.Drawing.Size(950, 40)
$toolbar.Location = New-Object System.Drawing.Point(0, 42)
$toolbar.BackColor = [System.Drawing.Color]::FromArgb(40, 40, 60)

$lblType = New-Object System.Windows.Forms.Label
$lblType.Text = "Type:"
$lblType.ForeColor = "LightGray"
$lblType.Location = New-Object System.Drawing.Point(10, 10)
$lblType.Size = New-Object System.Drawing.Size(40, 22)
$toolbar.Controls.Add($lblType)

$cmbType = New-Object System.Windows.Forms.ComboBox
$cmbType.Items.AddRange(@("All", "invoice", "contract", "claim", "note", "other"))
$cmbType.SelectedIndex = 0
$cmbType.Location = New-Object System.Drawing.Point(50, 8)
$cmbType.Size = New-Object System.Drawing.Size(110, 25)
$cmbType.BackColor = [System.Drawing.Color]::FromArgb(50, 50, 70)
$cmbType.ForeColor = "White"
$toolbar.Controls.Add($cmbType)

$lblSearch = New-Object System.Windows.Forms.Label
$lblSearch.Text = "Search:"
$lblSearch.ForeColor = "LightGray"
$lblSearch.Location = New-Object System.Drawing.Point(175, 10)
$lblSearch.Size = New-Object System.Drawing.Size(50, 22)
$toolbar.Controls.Add($lblSearch)

$txtSearch = New-Object System.Windows.Forms.TextBox
$txtSearch.Location = New-Object System.Drawing.Point(225, 8)
$txtSearch.Size = New-Object System.Drawing.Size(250, 25)
$txtSearch.BackColor = [System.Drawing.Color]::FromArgb(50, 50, 70)
$txtSearch.ForeColor = "White"
$toolbar.Controls.Add($txtSearch)

$lblTagFilter = New-Object System.Windows.Forms.Label
$lblTagFilter.Text = "Tags:"
$lblTagFilter.ForeColor = "LightGray"
$lblTagFilter.Location = New-Object System.Drawing.Point(490, 10)
$lblTagFilter.Size = New-Object System.Drawing.Size(40, 22)
$toolbar.Controls.Add($lblTagFilter)

$txtTagFilter = New-Object System.Windows.Forms.TextBox
$txtTagFilter.Location = New-Object System.Drawing.Point(525, 8)
$txtTagFilter.Size = New-Object System.Drawing.Size(150, 25)
$txtTagFilter.BackColor = [System.Drawing.Color]::FromArgb(50, 50, 70)
$txtTagFilter.ForeColor = "White"
$toolbar.Controls.Add($txtTagFilter)

$lblCount = New-Object System.Windows.Forms.Label
$lblCount.Text = "Records: 0"
$lblCount.ForeColor = "Yellow"
$lblCount.Location = New-Object System.Drawing.Point(750, 10)
$lblCount.Size = New-Object System.Drawing.Size(180, 22)
$lblCount.TextAlign = "MiddleRight"
$toolbar.Controls.Add($lblCount)

$form.Controls.Add($toolbar)

# Grid
$grid = New-Object System.Windows.Forms.DataGridView
$grid.Location = New-Object System.Drawing.Point(0, 82)
$grid.Size = New-Object System.Drawing.Size(950, 480)
$grid.BackgroundColor = [System.Drawing.Color]::FromArgb(25, 25, 40)
$grid.ForeColor = "White"
$grid.GridColor = [System.Drawing.Color]::FromArgb(60, 60, 80)
$grid.DefaultCellStyle.BackColor = [System.Drawing.Color]::FromArgb(30, 30, 45)
$grid.DefaultCellStyle.ForeColor = "LightGray"
$grid.DefaultCellStyle.Font = New-Object System.Drawing.Font("Consolas", 9)
$grid.ColumnHeadersDefaultCellStyle.BackColor = [System.Drawing.Color]::FromArgb(50, 50, 70)
$grid.ColumnHeadersDefaultCellStyle.ForeColor = "White"
$grid.ColumnHeadersHeight = 30
$grid.AutoSizeColumnsMode = "Fill"
$grid.AllowUserToAddRows = $false
$grid.AllowUserToDeleteRows = $false
$grid.ReadOnly = $true
$grid.RowHeadersVisible = $false
$grid.SelectionMode = "FullRowSelect"
$grid.MultiSelect = $false
$grid.Anchor = "Top, Bottom, Left, Right"

$col1 = New-Object System.Windows.Forms.DataGridViewTextBoxColumn
$col1.HeaderText = "Type"; $col1.FillWeight = 70
$col2 = New-Object System.Windows.Forms.DataGridViewTextBoxColumn
$col2.HeaderText = "Source"; $col2.FillWeight = 150
$col3 = New-Object System.Windows.Forms.DataGridViewTextBoxColumn
$col3.HeaderText = "Entities"; $col3.FillWeight = 180
$col4 = New-Object System.Windows.Forms.DataGridViewTextBoxColumn
$col4.HeaderText = "Tags"; $col4.FillWeight = 130
$col5 = New-Object System.Windows.Forms.DataGridViewTextBoxColumn
$col5.HeaderText = "Summary"; $col5.FillWeight = 420
$grid.Columns.AddRange($col1, $col2, $col3, $col4, $col5)

# Detail bar
$detailBar = New-Object System.Windows.Forms.Panel
$detailBar.Size = New-Object System.Drawing.Size(950, 45)
$detailBar.Location = New-Object System.Drawing.Point(0, 562)
$detailBar.BackColor = [System.Drawing.Color]::FromArgb(35, 35, 55)
$detailBar.Anchor = "Bottom, Left, Right"

$lblDetail = New-Object System.Windows.Forms.Label
$lblDetail.Text = "Select a record to view details"
$lblDetail.ForeColor = "Gray"
$lblDetail.Location = New-Object System.Drawing.Point(10, 5)
$lblDetail.Size = New-Object System.Drawing.Size(700, 18)
$detailBar.Controls.Add($lblDetail)

$lblDetail2 = New-Object System.Windows.Forms.Label
$lblDetail2.Text = ""
$lblDetail2.ForeColor = "DarkGray"
$lblDetail2.Location = New-Object System.Drawing.Point(10, 25)
$lblDetail2.Size = New-Object System.Drawing.Size(920, 18)
$detailBar.Controls.Add($lblDetail2)

$form.Controls.Add($detailBar)
$form.Controls.Add($grid)

# Bottom info bar
$infoBar = New-Object System.Windows.Forms.Panel
$infoBar.Size = New-Object System.Drawing.Size(950, 43)
$infoBar.Location = New-Object System.Drawing.Point(0, 607)
$infoBar.BackColor = [System.Drawing.Color]::FromArgb(40, 40, 60)
$infoBar.Anchor = "Bottom, Left, Right"

$lblInfo = New-Object System.Windows.Forms.Label
$lblInfo.Text = "Double-click a row to copy JSON | Filters: Type, Text, Tags"
$lblInfo.ForeColor = "Gray"
$lblInfo.Location = New-Object System.Drawing.Point(10, 12)
$lblInfo.Size = New-Object System.Drawing.Size(550, 22)
$infoBar.Controls.Add($lblInfo)

$btnRefresh = New-Object System.Windows.Forms.Button
$btnRefresh.Text = "Refresh"
$btnRefresh.Location = New-Object System.Drawing.Point(830, 8)
$btnRefresh.Size = New-Object System.Drawing.Size(100, 28)
$btnRefresh.FlatStyle = "Flat"
$btnRefresh.BackColor = [System.Drawing.Color]::FromArgb(60, 60, 80)
$btnRefresh.ForeColor = "White"
$btnRefresh.Cursor = "Hand"
$btnRefresh.Add_Click({ Load-Data })
$infoBar.Controls.Add($btnRefresh)

$form.Controls.Add($infoBar)

# Functions
function Load-Data {
    if (!(Test-Path $MemoryFile)) {
        $grid.Rows.Clear()
        $lblCount.Text = "Records: 0 (no file)"
        return
    }
    $filterType = $cmbType.SelectedItem
    $searchText = $txtSearch.Text.ToLower()
    $tagFilter = $txtTagFilter.Text.ToLower()
    $grid.Rows.Clear()
    $count = 0

    Get-Content $MemoryFile -Encoding UTF8 | ForEach-Object {
        try {
            $row = $_ | ConvertFrom-Json
            if ($filterType -ne "All" -and $row.doc_type -ne $filterType) { return }
            $jsonLine = $_ | Out-String
            $jsonLower = $jsonLine.ToLower()
            if ($searchText -and $jsonLower -notmatch $searchText) { return }
            if ($tagFilter) {
                $tags = $row.tags -join " "
                if ($tags.ToLower() -notmatch $tagFilter) { return }
            }
            $entities = if ($row.entities) { ($row.entities | % { $_.name }) -join ", " } else { "" }
            $tags = if ($row.tags) { ($row.tags -join ", ") } else { "" }
            $summary = if ($row.summary) { $row.summary.Substring(0, [Math]::Min($row.summary.Length, 120)) } else { "" }
            $grid.Rows.Add($row.doc_type, $row.source_file, $entities, $tags, $summary)
            $count++
        } catch { }
    }
    $lblCount.Text = "Records: $count"
}

$grid.Add_CellMouseClick({
    if ($grid.SelectedRows.Count -gt 0) {
        $r = $grid.SelectedRows[0]
        $lblDetail.Text = "Type: $($r.Cells[0].Value) | Source: $($r.Cells[1].Value)"
        $tagsVal = $r.Cells[3].Value
        if ($tagsVal) { $lblDetail2.Text = "Tags: $tagsVal" } else { $lblDetail2.Text = "" }
    }
})

$grid.Add_CellDoubleClick({
    if ($grid.SelectedRows.Count -gt 0) {
        $r = $grid.SelectedRows[0]
        $type = $r.Cells[0].Value
        $source = $r.Cells[1].Value
        Get-Content $MemoryFile -Encoding UTF8 | ForEach-Object {
            try {
                $row = $_ | ConvertFrom-Json
                if ($row.doc_type -eq $type -and $row.source_file -eq $source) {
                    Set-Clipboard -Value $_
                    $lblDetail.Text = "JSON copied to clipboard!"
                    break
                }
            } catch { }
        }
    }
})

$cmbType.Add_SelectedIndexChanged({ Load-Data })
$txtSearch.Add_TextChanged({ Load-Data })
$txtTagFilter.Add_TextChanged({ Load-Data })

Load-Data
$form.Add_Shown({ Load-Data })
[System.Windows.Forms.Application]::Run($form)
