# Тест подключения к OpenRouter
$envPath = Join-Path (Join-Path $PSScriptRoot "..") ".env"
if (!(Test-Path $envPath)) {
    $envPath = Join-Path (Join-Path (Join-Path $env:LOCALAPPDATA "BRAIN") "brain_data") ".env"
}

Write-Host "Файл .env: $envPath" -ForegroundColor Cyan
if (Test-Path $envPath) {
    $key = (Get-Content $envPath | Where-Object { $_ -like "OPENROUTER_API_KEY=*" }).Split("=", 2)[1]
    if ($key) {
        Write-Host "Ключ найден: $($key.Substring(0, 15))..." -ForegroundColor Green
        Write-Host "Проверка подключения..." -ForegroundColor Yellow
        try {
            $body = @{ model = "deepseek/deepseek-v4-flash:free"; messages = @(@{ role = "user"; content = "ok" }); max_tokens = 10 } | ConvertTo-Json
            $response = Invoke-RestMethod -Uri "https://openrouter.ai/api/v1/chat/completions" -Method Post -Body $body -ContentType "application/json" -Headers @{ Authorization = "Bearer $key"; "HTTP-Referer" = "https://github.com/brain"; "X-Title" = "BRAIN Test" } -TimeoutSec 30
            Write-Host "OK! Ответ: $($response.choices[0].message.content)" -ForegroundColor Green
        } catch {
            Write-Host "Ошибка:" -ForegroundColor Red
            Write-Host $_.Exception.Message -ForegroundColor Red
        }
    } else {
        Write-Host "Ключ не найден в .env" -ForegroundColor Red
    }
} else {
    Write-Host ".env не найден" -ForegroundColor Red
}
