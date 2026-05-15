import os
import sys
import logging
from dotenv import load_dotenv

load_dotenv()

def setup_logging():
    root = logging.getLogger()
    if not root.handlers:
        handler = logging.StreamHandler(sys.stdout)
        handler.setFormatter(logging.Formatter("%(asctime)s [%(levelname)s] %(message)s"))
        root.addHandler(handler)
        root.setLevel(logging.INFO)

class Config:
    OPENROUTER_API_KEY = os.getenv("OPENROUTER_API_KEY", "")
    EXTRACTOR_MODEL = os.getenv("EXTRACTOR_MODEL", "deepseek/deepseek-v4-flash:free")
    CHAT_MODEL = os.getenv("CHAT_MODEL", "deepseek/deepseek-v4-flash:free")
    MAX_FILE_SIZE_MB = int(os.getenv("MAX_FILE_SIZE_MB", 50))
    MAX_FILE_SIZE_BYTES = MAX_FILE_SIZE_MB * 1024 * 1024

    BASE_DIR = os.path.dirname(os.path.abspath(__file__))
    INBOX_DIR = os.getenv("INBOX_DIR", os.path.join(BASE_DIR, "brain_data", "inbox"))
    PROCESSED_DIR = os.getenv("PROCESSED_DIR", os.path.join(BASE_DIR, "brain_data", "processed"))
    ERRORS_DIR = os.getenv("ERRORS_DIR", os.path.join(BASE_DIR, "brain_data", "errors"))
    ARCHIVE_DIR = os.getenv("ARCHIVE_DIR", os.path.join(BASE_DIR, "brain_data", "archive"))
    MEMORY_FILE = os.getenv("MEMORY_FILE", os.path.join(BASE_DIR, "brain_data", "brain.jsonl"))
    INDEX_FILE = os.getenv("INDEX_FILE", os.path.join(BASE_DIR, "brain_data", "index.faiss"))

    OPENROUTER_BASE = "https://openrouter.ai/api/v1"

    SYSTEM_PROMPT_EXTRACTOR = """Ты — ИИ-аналитик документов. Извлеки из текста структурированную информацию.
Ответ верни ТОЛЬКО в виде JSON, без пояснений. Поля JSON:
- doc_type: тип документа (invoice|contract|claim|note|other)
- entities: список сущностей [{id, type, name, role, details}]
  где type: organization|person|product|money|date|other
- facts: список фактов [{predicate, object, confidence}]
- tags: список тегов (2-5 ключевых слов)
- uncertainty: число от 0 до 1 (насколько ты не уверен в анализе)
- summary: краткое содержание (1-2 предложения)"""
