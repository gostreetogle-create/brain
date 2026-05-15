import json
from memory import load_all
from extractor import chat_query


def search(query: str, top_k: int = 10) -> list[dict]:
    records = load_all()
    query_lower = query.lower()

    scored = []
    for r in records:
        score = 0
        text = json.dumps(r, ensure_ascii=False).lower()

        if query_lower in text:
            score += 1

        for tag in r.get("tags", []):
            if query_lower in tag.lower():
                score += 3

        for fact in r.get("facts", []):
            if query_lower in fact.get("predicate", "").lower():
                score += 2
            if query_lower in fact.get("object", "").lower():
                score += 2

        if score > 0:
            scored.append((score, r))

    scored.sort(key=lambda x: x[0], reverse=True)
    return [r for _, r in scored[:top_k]]


def ask(query: str) -> str:
    results = search(query, top_k=10)

    if results:
        context_parts = []
        for r in results:
            context_parts.append(
                f"[{r.get('doc_type', 'doc')}] {r.get('source_file', 'unknown')}\n"
                f"Сущности: {[e.get('name', '') for e in r.get('entities', [])]}\n"
                f"Факты: {r.get('facts', [])}\n"
                f"Сводка: {r.get('summary', '')}"
            )
        context = "\n---\n".join(context_parts[:5])
    else:
        context = ""

    return chat_query(query, context)


def interactive():
    print("🧠 Brain Chat. Введи вопрос (exit для выхода).")
    while True:
        query = input("\n> ").strip()
        if query.lower() in ("exit", "quit", "выход"):
            break
        if not query:
            continue
        response = ask(query)
        print(response)
