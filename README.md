# 🧠 Brain — Цифровой Интеллектуальный Сотрудник

Локальная система автономного накопления знаний компании.
Кидаешь файлы в папку — получаешь единый файл памяти и ИИ-ассистента.

## Как это работает

```
Папка "Входящие/" → [магия ИИ] → brain.jsonl
                                → Чат: задаёшь вопросы
```

1. Кладёшь любой файл в `brain_data/inbox/`
2. Система автоматически: извлекает текст → анализирует LLM → сохраняет сущности и связи
3. Открываешь чат: *"Сколько потратили на насосы?"*, *"Покажи претензии по ООО Ромашка"*
4. Файл `brain_data/brain.jsonl` — вся память. Скопировал на флешку — перенёс интеллект.

## Быстрый старт

```bash
# 1. Установка
pip install -r requirements.txt

# 2. Настройка
copy .env.example .env
# Отредактируй .env — вставь свой OPENROUTER_API_KEY

# 3. Запуск
python app.py          # Десктоп-приложение (рекомендуется)
python cli.py          # Командная строка
```

## Режимы работы

| Команда | Что делает |
|---------|-----------|
| `python cli.py watch` | Запускает автослежение за папкой Входящие |
| `python cli.py process --file path` | Обработать один файл вручную |
| `python cli.py chat` | Открыть интерактивный чат |
| `python cli.py review` | Запустить ночной ревью (самообучение) |
| `python cli.py search --query "..."` | Поиск по памяти |
| `python cli.py stats` | Статистика системы |

## Структура проекта

```
brain/
├── README.md                  # Этот файл
├── requirements.txt           # Зависимости
├── .env.example              # Пример конфига
├── .env                      # Твой конфиг (в gitignore)
├── cli.py                    # Точка входа
├── config.py                 # Конфигурация системы
├── watcher.py                # Автоотслеживание папки Входящие
├── preprocessor.py           # Извлечение текста (PDF, OCR, Excel...)
├── extractor.py              # LLM-анализ документа
├── memory.py                 # Работа с brain.jsonl
├── linker.py                 # Семантические связи между документами
├── chat.py                   # Интерфейс вопрос-ответ
├── self_review.py            # Ночной ревью и самообучение
└── brain_data/               # Данные (создаются автоматически)
    ├── inbox/                # Сюда кидаешь файлы
    ├── processed/            # Обработанные файлы
    ├── errors/               # Файлы с ошибкой
    ├── brain.jsonl           # Единый файл памяти (главное сокровище)
    └── index.faiss           # Векторный индекс (перестраивается)
```

## Формат brain.jsonl

Каждая строка — отдельный self-contained документ в JSON.
Формат понятен любому LLM:

```jsonl
{"id":"doc_001","type":"document","timestamp":"2024-03-12T10:00:00Z","schema_version":"1.0","doc_type":"invoice","source_file":"счет_12345.pdf","source_hash":"sha256:...","entities":[{"id":"ent_001","type":"organization","name":"ООО Ромашка","role":"supplier"}],"facts":[{"predicate":"поставлен","object":"Насос 45кВт","confidence":0.95}],"tags":["закупка","насосы"],"uncertainty":0.15,"embedding":[0.12,-0.45,...],"relations":[{"target":"doc_002","type":"similar_to","weight":0.87}]}
```

## Подключение любой модели ИИ

В `.env` меняешь поле `EXTRACTOR_MODEL` и `CHAT_MODEL`:

```env
OPENROUTER_API_KEY=sk-or-v1-...
EXTRACTOR_MODEL=deepseek/deepseek-v4-flash:free   # Бесплатно, 1M контекст
CHAT_MODEL=deepseek/deepseek-v4-flash:free        # Бесплатно, 1M контекст
```

OpenRouter поддерживает 300+ моделей. Хочешь сменить — просто меняешь название.

## Принципы архитектуры

1. **Immutable log** — данные только дописываются, никогда не удаляются
2. **Self-describing JSON** — любой будущий ИИ поймёт формат
3. **Uncertainty-first** — система знает, чего не знает (поле `confidence`)
4. **No lock-in** — файл .jsonl открывается блокнотом
