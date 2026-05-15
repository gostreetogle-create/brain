import os
import shutil
import time
import datetime
import logging
from watchdog.observers import Observer
from watchdog.events import FileSystemEventHandler
from config import Config, setup_logging
from preprocessor import extract_text, file_hash
from extractor import analyze_document
from memory import build_record, append

setup_logging()
log = logging.getLogger(__name__)


def process_file(filepath: str, progress_fn=None) -> bool:
    filename = os.path.basename(filepath)
    log.info(f"Processing: {filename}")

    def pct(n, msg):
        if progress_fn:
            progress_fn(n, msg)
        log.info(f"[{n}%] {msg}")

    try:
        pct(10, "Извлечение текста...")
        text = extract_text(filepath)
        if not text.strip():
            raise ValueError("Empty text extracted")

        pct(30, "Вычисление хеша...")
        hsh = file_hash(filepath)
        pct(40, "Анализ ИИ...")
        analysis = analyze_document(text, filename)
        pct(65, "Сохранение в память...")
        record = build_record(
            source_file=filename,
            source_hash=hsh,
            text=text,
            analysis=analysis,
            embedding=calc_embedding(text),
        )
        pct(80, "Поиск связей...")
        link_new_record(record)
        append(record)
        pct(100, f"Готово: {filename}")
        log.info(f"Done: {filename} → {record['id']} (type={analysis.get('doc_type')})")
        return True

    except Exception as e:
        log.error(f"Error processing {filename}: {e}")
        return False


def calc_embedding(text: str) -> list[float]:
    dummy_dim = 128
    import hashlib
    h = hashlib.sha256(text.encode()[:1000])
    seed = int(h.hexdigest()[:8], 16)
    rng = __import__("numpy").random.RandomState(seed)
    vec = rng.randn(dummy_dim).tolist()
    norm = __import__("numpy").linalg.norm(vec)
    return [v / norm for v in vec]


class InboxHandler(FileSystemEventHandler):
    def on_created(self, event):
        if event.is_directory:
            return
        time.sleep(1)
        ext = os.path.splitext(event.src_path)[1].lower()
        if ext in (".tmp", ".part", ".crdownload"):
            return
        def on_progress(n, msg):
            print(f"[BRAIN_PROGRESS:{n}] {msg}", flush=True)
        success = process_file(event.src_path, progress_fn=on_progress)
        if success:
            today = datetime.date.today().strftime("%Y-%m")
            dest_dir = os.path.join(Config.ARCHIVE_DIR, today)
        else:
            dest_dir = Config.ERRORS_DIR
        os.makedirs(dest_dir, exist_ok=True)
        dest = os.path.join(dest_dir, os.path.basename(event.src_path))
        try:
            shutil.move(event.src_path, dest)
        except Exception:
            pass


def start_watching():
    os.makedirs(Config.INBOX_DIR, exist_ok=True)
    os.makedirs(Config.PROCESSED_DIR, exist_ok=True)
    os.makedirs(Config.ERRORS_DIR, exist_ok=True)
    os.makedirs(Config.ARCHIVE_DIR, exist_ok=True)

    log.info(f"Watching: {Config.INBOX_DIR}")
    log.info(f"Archive → {Config.ARCHIVE_DIR}")
    log.info(f"Errors → {Config.ERRORS_DIR}")

    event_handler = InboxHandler()
    observer = Observer()
    observer.schedule(event_handler, Config.INBOX_DIR, recursive=False)
    observer.start()
    try:
        while True:
            time.sleep(1)
    except KeyboardInterrupt:
        observer.stop()
    observer.join()
