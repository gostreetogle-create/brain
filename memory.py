import json
import os
import uuid
import numpy as np
from datetime import datetime, timezone
from config import Config


def new_id() -> str:
    return f"doc_{uuid.uuid4().hex[:12]}"


def load_all(limit: int | None = None) -> list[dict]:
    records = []
    if not os.path.exists(Config.MEMORY_FILE):
        return records
    with open(Config.MEMORY_FILE, "r", encoding="utf-8") as f:
        for i, line in enumerate(f):
            if limit and i >= limit:
                break
            line = line.strip()
            if line:
                records.append(json.loads(line))
    return records


def append(record: dict):
    os.makedirs(os.path.dirname(Config.MEMORY_FILE), exist_ok=True)
    with open(Config.MEMORY_FILE, "a", encoding="utf-8") as f:
        f.write(json.dumps(record, ensure_ascii=False) + "\n")


def count() -> int:
    if not os.path.exists(Config.MEMORY_FILE):
        return 0
    with open(Config.MEMORY_FILE, "r", encoding="utf-8") as f:
        return sum(1 for _ in f)


def build_record(
    source_file: str,
    source_hash: str,
    text: str,
    analysis: dict,
    embedding: list[float] | None = None,
) -> dict:
    return {
        "id": new_id(),
        "type": "document",
        "timestamp": datetime.now(timezone.utc).isoformat(),
        "schema_version": "1.0",
        "doc_type": analysis.get("doc_type", "other"),
        "source_file": source_file,
        "source_hash": source_hash,
        "entities": analysis.get("entities", []),
        "facts": analysis.get("facts", []),
        "tags": analysis.get("tags", []),
        "summary": analysis.get("summary", ""),
        "uncertainty": analysis.get("uncertainty", 0.5),
        "embedding": embedding or [],
        "relations": [],
    }


def get_embeddings_matrix(records: list[dict]) -> tuple[np.ndarray, list[str]]:
    ids = []
    embs = []
    for r in records:
        e = r.get("embedding", [])
        if e:
            ids.append(r["id"])
            embs.append(e)
    if not embs:
        return np.array([]), []
    return np.array(embs), ids


def stats() -> dict:
    records = load_all()
    doc_types = {}
    total_entities = 0
    total_facts = 0
    for r in records:
        dt = r.get("doc_type", "unknown")
        doc_types[dt] = doc_types.get(dt, 0) + 1
        total_entities += len(r.get("entities", []))
        total_facts += len(r.get("facts", []))
    return {
        "total_documents": len(records),
        "by_type": doc_types,
        "total_entities": total_entities,
        "total_facts": total_facts,
    }
