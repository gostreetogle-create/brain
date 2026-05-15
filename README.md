# BRAIN — Цифровой Сотрудник

Локальный ИИ-ассистент для накопления знаний компании.
Кидаешь файлы в папку — получаешь единый файл памяти и умный поиск.

## Быстрый старт

```bash
cd Brain.Desktop
dotnet run
```

Или запустить `Brain.lnk` с рабочего стола (Single-file EXE).

## Что умеет

- Автоматическая обработка документов (PDF, Excel, Word, изображения)
- ИИ-анализ через OpenRouter (бесплатные модели)
- Единый файл памяти `brain.jsonl` — переносится на флешке
- Чат с базой знаний
- Просмотр данных с фильтрами
- Автообновление через GitHub

## Технологии

- C# WPF (.NET 10)
- PdfPig, ExcelDataReader, Tesseract OCR
- OpenRouter API (deepseek/deepseek-v4-flash:free)
- GitHub Actions (автосборка)
