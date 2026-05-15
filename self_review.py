import logging
import json
from datetime import datetime, timezone
from memory import load_all, append, count, new_id
from extractor import review_records
from config import setup_logging

setup_logging()

log = logging.getLogger(__name__)


def run_review():
    total = count()
    print(f"[BRAIN_PROGRESS:0] Всего записей: {total}", flush=True)
    records = load_all()
    if not records:
        print("[BRAIN_PROGRESS:100] Нет записей для проверки.", flush=True)
        return

    high_uncertainty = [r for r in records if r.get("uncertainty", 0) > 0.3]
    print(f"[BRAIN_PROGRESS:10] Найдено неуверенных: {len(high_uncertainty)}", flush=True)

    if not high_uncertainty:
        print("[BRAIN_PROGRESS:100] Всё хорошо, неуверенных записей нет.", flush=True)
        return

    batch_size = 10
    total_batches = (len(high_uncertainty) + batch_size - 1) // batch_size
    for i in range(0, len(high_uncertainty), batch_size):
        batch_num = i // batch_size + 1
        pct = 10 + int(80 * batch_num / total_batches)
        print(f"[BRAIN_PROGRESS:{pct}] Проверка пачки {batch_num}/{total_batches}...", flush=True)

        batch = high_uncertainty[i : i + batch_size]
        try:
            result = review_records(batch)
            for insight in result.get("insights", []):
                record = {
                    "id": new_id(),
                    "type": "insight",
                    "timestamp": datetime.now(timezone.utc).isoformat(),
                    "schema_version": "1.0",
                    "content": insight,
                    "derived_from": [r["id"] for r in batch],
                    "confidence": 0.7,
                }
                append(record)

            for corr in result.get("corrections", []):
                record = {
                    "id": new_id(),
                    "type": "correction",
                    "timestamp": datetime.now(timezone.utc).isoformat(),
                    "schema_version": "1.0",
                    "correction": corr,
                    "derived_from": [r["id"] for r in batch],
                }
                append(record)

        except Exception as e:
            print(f"[BRAIN_PROGRESS:{pct}] Ошибка пачки: {e}", flush=True)

    print(f"[BRAIN_PROGRESS:100] Самообучение завершено. Всего записей: {count()}", flush=True)


if __name__ == "__main__":
    run_review()
