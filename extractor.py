import json
import time
from openai import OpenAI
from config import Config


def _build_client() -> OpenAI:
    return OpenAI(
        base_url=Config.OPENROUTER_BASE,
        api_key=Config.OPENROUTER_API_KEY,
        default_headers={
            "HTTP-Referer": "https://github.com/brain",
            "X-Title": "Brain Digital Employee",
        },
    )


def analyze_document(text: str, filename: str) -> dict:
    client = _build_client()
    prompt = f"""Проанализируй документ.

Имя файла: {filename}

Текст документа:
{text[:50000]}"""

    response = client.chat.completions.create(
        model=Config.EXTRACTOR_MODEL,
        messages=[
            {"role": "system", "content": Config.SYSTEM_PROMPT_EXTRACTOR},
            {"role": "user", "content": prompt},
        ],
        temperature=0.1,
        max_tokens=4000,
        response_format={"type": "json_object"},
    )

    raw = response.choices[0].message.content
    try:
        result = json.loads(raw)
    except json.JSONDecodeError:
        result = {"doc_type": "other", "entities": [], "facts": [], "tags": [], "uncertainty": 1.0, "summary": raw[:500]}
    return result


def chat_query(query: str, context: str) -> str:
    client = _build_client()
    if context:
        system_prompt = "Ты — ИИ-ассистент, отвечающий на вопросы на основе базы знаний компании. Отвечай кратко, по делу, ссылаясь на конкретные документы."
        user_msg = f"Контекст из базы знаний:\n{context}\n\nВопрос: {query}"
    else:
        system_prompt = "Ты — полезный ИИ-ассистент. База знаний компании пока пуста, но ты можешь отвечать на общие вопросы."
        user_msg = query

    response = client.chat.completions.create(
        model=Config.CHAT_MODEL,
        messages=[
            {"role": "system", "content": system_prompt},
            {"role": "user", "content": user_msg},
        ],
        temperature=0.3,
        max_tokens=2000,
    )
    return response.choices[0].message.content


def review_records(records: list[dict]) -> dict:
    client = _build_client()
    prompt = f"""Проверь следующие записи из базы знаний. Для каждой определи:
1. Корректна ли классификация (doc_type, tags)
2. Можно ли объединить похожие записи
3. Какие инсайты можно извлечь

Записи:
{json.dumps(records, ensure_ascii=False, indent=2)[:8000]}

Ответь JSON: {{"corrections": [...], "insights": [...], "merges": [...]}}"""

    response = client.chat.completions.create(
        model=Config.CHAT_MODEL,
        messages=[
            {"role": "system", "content": "Ты — ревьюер базы знаний. Анализируй записи и предлагай улучшения."},
            {"role": "user", "content": prompt},
        ],
        temperature=0.2,
        max_tokens=4000,
        response_format={"type": "json_object"},
    )
    try:
        return json.loads(response.choices[0].message.content)
    except json.JSONDecodeError:
        return {"corrections": [], "insights": [], "merges": []}
