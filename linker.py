import numpy as np
from memory import load_all, get_embeddings_matrix


def cosine_similarity(a: np.ndarray, b: np.ndarray) -> float:
    if a.size == 0 or b.size == 0:
        return 0.0
    return float(np.dot(a, b) / (np.linalg.norm(a) * np.linalg.norm(b) + 1e-10))


def find_related(
    embedding: list[float],
    threshold: float = 0.7,
    top_k: int = 5,
) -> list[dict]:
    records = load_all()
    if not records:
        return []

    query_vec = np.array(embedding)
    if query_vec.size == 0:
        return []

    mat, ids = get_embeddings_matrix(records)
    if mat.size == 0:
        return []

    sims = np.array([cosine_similarity(query_vec, mat[i]) for i in range(len(ids))])
    top_indices = np.argsort(sims)[::-1][:top_k]

    results = []
    for idx in top_indices:
        if sims[idx] >= threshold:
            results.append({
                "target_id": ids[idx],
                "similarity": float(sims[idx]),
                "type": "similar_to",
            })
    return results


def link_new_record(record: dict):
    emb = record.get("embedding", [])
    if not emb:
        return

    related = find_related(emb)
    record["relations"] = related
