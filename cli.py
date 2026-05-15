#!/usr/bin/env python3
import sys
import os
import argparse
import json

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))

from config import Config
from preprocessor import extract_text, file_hash
from extractor import analyze_document
from memory import build_record, append, load_all, stats
from linker import link_new_record, find_related
from chat import interactive, search, ask
from watcher import start_watching
from self_review import run_review


def progress(percent: int, msg: str):
    print(f"[BRAIN_PROGRESS:{percent}] {msg}", flush=True)


def cmd_process(args):
    filepath = args.file
    if not os.path.exists(filepath):
        print(f"File not found: {filepath}")
        return
    progress(10, "Извлечение текста из файла...")
    text = extract_text(filepath)
    if not text.strip():
        print("Ошибка: не удалось извлечь текст")
        return
    progress(30, "Текст извлечён, вычисление хеша...")
    hsh = file_hash(filepath)
    progress(40, "Отправка на анализ ИИ...")
    analysis = analyze_document(text, os.path.basename(filepath))
    progress(65, "Сохранение в базу знаний...")
    record = build_record(
        source_file=os.path.basename(filepath),
        source_hash=hsh,
        text=text,
        analysis=analysis,
        embedding=_dummy_embedding(text),
    )
    progress(80, "Поиск связей с другими документами...")
    link_new_record(record)
    append(record)
    progress(95, "Сохранено. Завершение...")
    progress(100, "Готово!")
    print(json.dumps(record, ensure_ascii=False, indent=2))


def cmd_watch(args):
    start_watching()


def cmd_chat(args):
    interactive()


def cmd_search(args):
    results = search(args.query)
    if not results:
        print("Ничего не найдено.")
        return
    for r in results:
        print(f"[{r.get('doc_type', '?')}] {r.get('source_file', '?')} — {r.get('summary', '')[:120]}")


def cmd_ask(args):
    print(ask(args.query))


def cmd_stats(args):
    s = stats()
    print(json.dumps(s, ensure_ascii=False, indent=2))


def cmd_related(args):
    records = load_all()
    doc = None
    for r in records:
        if r["id"] == args.doc_id:
            doc = r
            break
    if not doc:
        print(f"Document not found: {args.doc_id}")
        return
    emb = doc.get("embedding", [])
    if not emb:
        print("No embedding for this document.")
        return
    related = find_related(emb)
    for r in related:
        print(f"{r['target_id']} — similarity: {r['similarity']:.3f}")


def cmd_review(args):
    run_review()


def _dummy_embedding(text: str) -> list[float]:
    import hashlib
    import numpy as np
    h = hashlib.sha256(text.encode()[:1000])
    seed = int(h.hexdigest()[:8], 16)
    rng = np.random.RandomState(seed)
    vec = rng.randn(128).tolist()
    norm = np.linalg.norm(vec)
    return [v / norm for v in vec]


def main():
    parser = argparse.ArgumentParser(description="🧠 Brain — Цифровой Интеллектуальный Сотрудник")
    sub = parser.add_subparsers(dest="command")

    p_watch = sub.add_parser("watch", help="Запустить автослежение за папкой Входящие")
    p_watch.set_defaults(func=cmd_watch)

    p_process = sub.add_parser("process", help="Обработать один файл")
    p_process.add_argument("--file", "-f", required=True)
    p_process.set_defaults(func=cmd_process)

    p_chat = sub.add_parser("chat", help="Открыть интерактивный чат")
    p_chat.set_defaults(func=cmd_chat)

    p_search = sub.add_parser("search", help="Поиск по базе знаний")
    p_search.add_argument("--query", "-q", required=True)
    p_search.set_defaults(func=cmd_search)

    p_ask = sub.add_parser("ask", help="Задать вопрос и получить ответ")
    p_ask.add_argument("--query", "-q", required=True)
    p_ask.set_defaults(func=cmd_ask)

    p_stats = sub.add_parser("stats", help="Статистика системы")
    p_stats.set_defaults(func=cmd_stats)

    p_related = sub.add_parser("related", help="Найти связанные документы")
    p_related.add_argument("--doc-id", required=True)
    p_related.set_defaults(func=cmd_related)

    p_review = sub.add_parser("review", help="Запустить ночной ревью")
    p_review.set_defaults(func=cmd_review)

    args = parser.parse_args()
    if not args.command:
        parser.print_help()
        return

    os.makedirs(Config.INBOX_DIR, exist_ok=True)
    os.makedirs(Config.PROCESSED_DIR, exist_ok=True)
    os.makedirs(Config.ERRORS_DIR, exist_ok=True)
    os.makedirs(Config.ARCHIVE_DIR, exist_ok=True)

    args.func(args)


if __name__ == "__main__":
    main()
