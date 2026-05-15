import json
import os
from PySide6.QtWidgets import (QWidget, QVBoxLayout, QHBoxLayout, QLabel,
                               QPushButton, QGroupBox, QGridLayout, QFileDialog,
                               QMessageBox, QProgressBar, QInputDialog, QLineEdit)
from PySide6.QtCore import QThread, Signal, Qt
from PySide6.QtGui import QFont
from config import Config
from memory import count, stats
from health import check_connection
from preprocessor import extract_text, file_hash
from extractor import analyze_document
from linker import link_new_record
from watcher import start_watching, process_file


class ProcessWorker(QThread):
    progress = Signal(int, str)
    finished = Signal(bool, str)

    def __init__(self, filepath):
        super().__init__()
        self.filepath = filepath

    def run(self):
        try:
            self.progress.emit(10, "Извлечение текста...")
            text = extract_text(self.filepath)
            if not text.strip():
                self.finished.emit(False, "Пустой текст")
                return

            self.progress.emit(30, "Хеширование...")
            hsh = file_hash(self.filepath)

            self.progress.emit(40, "Анализ ИИ...")
            analysis = analyze_document(text, os.path.basename(self.filepath))

            self.progress.emit(65, "Сохранение в память...")
            from memory import build_record, append
            record = build_record(
                source_file=os.path.basename(self.filepath),
                source_hash=hsh,
                text=text,
                analysis=analysis,
            )

            self.progress.emit(80, "Поиск связей...")
            link_new_record(record)
            append(record)

            self.progress.emit(100, "Готово!")
            self.finished.emit(True, record.get("id", ""))
        except Exception as e:
            self.finished.emit(False, str(e))


class ReviewWorker(QThread):
    progress = Signal(int, str)
    finished = Signal(str)

    def run(self):
        from self_review import run_review
        import io, sys
        old = sys.stdout
        sys.stdout = buf = io.StringIO()
        try:
            run_review()
        finally:
            sys.stdout = old
        self.finished.emit(buf.getvalue())


class DashboardWidget(QWidget):
    def __init__(self, parent=None):
        super().__init__(parent)
        self.main_win = parent
        self.setup_ui()
        self.refresh_stats()
        self.check_ai()

    def setup_ui(self):
        layout = QVBoxLayout(self)
        layout.setSpacing(12)

        title = QLabel("BRAIN — Цифровой Сотрудник")
        title.setFont(QFont("Segoe UI", 18, QFont.Bold))
        title.setStyleSheet("color: #89b4fa;")
        layout.addWidget(title)

        stats_group = QGroupBox("Статистика")
        stats_grid = QGridLayout(stats_group)

        self.lbl_docs = QLabel("Документов: —")
        self.lbl_docs.setFont(QFont("Segoe UI", 12))
        stats_grid.addWidget(self.lbl_docs, 0, 0)

        self.lbl_inbox = QLabel("Во Входящих: —")
        self.lbl_inbox.setFont(QFont("Segoe UI", 12))
        stats_grid.addWidget(self.lbl_inbox, 0, 1)

        self.lbl_archive = QLabel("В архиве: —")
        self.lbl_archive.setFont(QFont("Segoe UI", 12))
        stats_grid.addWidget(self.lbl_archive, 0, 2)

        self.lbl_ai = QLabel("ИИ: проверка...")
        self.lbl_ai.setFont(QFont("Segoe UI", 12))
        stats_grid.addWidget(self.lbl_ai, 1, 0, 1, 3)

        layout.addWidget(stats_group)

        progress_group = QGroupBox("Прогресс")
        prog_layout = QVBoxLayout(progress_group)
        self.lbl_progress = QLabel("Ожидание...")
        self.lbl_progress.setFont(QFont("Segoe UI", 10))
        prog_layout.addWidget(self.lbl_progress)

        self.progress_bar = QProgressBar()
        self.progress_bar.setMaximum(100)
        prog_layout.addWidget(self.progress_bar)
        layout.addWidget(progress_group)

        actions_group = QGroupBox("Действия")
        actions_grid = QGridLayout(actions_group)

        btn_watch = QPushButton("Следить за Входящими")
        btn_watch.clicked.connect(self.start_watch)
        actions_grid.addWidget(btn_watch, 0, 0)

        btn_file = QPushButton("Обработать файл")
        btn_file.clicked.connect(self.process_file)
        actions_grid.addWidget(btn_file, 0, 1)

        btn_chat = QPushButton("Чат")
        btn_chat.clicked.connect(lambda: self.main_win.tabs.setCurrentIndex(1))
        actions_grid.addWidget(btn_chat, 0, 2)

        btn_review = QPushButton("Самообучение")
        btn_review.clicked.connect(self.run_review)
        actions_grid.addWidget(btn_review, 1, 0)

        btn_ai = QPushButton("Проверить ИИ")
        btn_ai.clicked.connect(self.check_ai)
        actions_grid.addWidget(btn_ai, 1, 1)

        btn_open_inbox = QPushButton("Открыть Входящие")
        btn_open_inbox.clicked.connect(self.open_inbox)
        actions_grid.addWidget(btn_open_inbox, 1, 2)

        layout.addWidget(actions_group)
        layout.addStretch()

    def refresh_stats(self):
        try:
            docs = count()
            inbox = len(os.listdir(Config.INBOX_DIR)) if os.path.isdir(Config.INBOX_DIR) else 0
            archive = 0
            if os.path.isdir(Config.ARCHIVE_DIR):
                for root, dirs, files in os.walk(Config.ARCHIVE_DIR):
                    archive += len(files)
            self.lbl_docs.setText(f"Документов: {docs}")
            self.lbl_inbox.setText(f"Во Входящих: {inbox}")
            self.lbl_archive.setText(f"В архиве: {archive}")
        except Exception:
            pass

    def check_ai(self):
        self.lbl_ai.setText("ИИ: проверка...")
        self.lbl_ai.setStyleSheet("color: yellow;")
        try:
            result = check_connection()
            if result["status"] == "ok":
                self.lbl_ai.setText(f"ИИ: подключено ({result['model']})")
                self.lbl_ai.setStyleSheet("color: #a6e3a1;")
            else:
                self.lbl_ai.setText(f"ИИ: {result['message']}")
                self.lbl_ai.setStyleSheet("color: #f38ba8;")
        except Exception as e:
            self.lbl_ai.setText(f"ИИ: ошибка ({str(e)[:50]})")
            self.lbl_ai.setStyleSheet("color: #f38ba8;")

    def start_watch(self):
        QMessageBox.information(self, "Слежение",
            "Слежение запущено. Кидайте файлы в папку Входящие.\n"
            "Закройте это окно, чтобы остановить.")
        self.worker = ProcessWorker.__new__(ProcessWorker)
        import threading
        t = threading.Thread(target=start_watching, daemon=True)
        t.start()

    def process_file(self):
        path, _ = QFileDialog.getOpenFileName(self, "Выберите файл")
        if not path:
            return

        self.progress_bar.setValue(0)
        self.lbl_progress.setText("Начинаю обработку...")

        self.worker = ProcessWorker(path)
        self.worker.progress.connect(lambda p, m: (
            self.progress_bar.setValue(p),
            self.lbl_progress.setText(m)
        ))
        self.worker.finished.connect(lambda ok, msg: (
            self.lbl_progress.setText(f"Готово!" if ok else f"Ошибка: {msg}"),
            self.progress_bar.setValue(100 if ok else 0),
            self.refresh_stats()
        ))
        self.worker.start()

    def run_review(self):
        self.progress_bar.setValue(0)
        self.lbl_progress.setText("Самообучение...")
        self.worker = ReviewWorker()
        self.worker.finished.connect(lambda out: (
            self.lbl_progress.setText("Самообучение завершено"),
            self.progress_bar.setValue(100)
        ))
        self.worker.start()

    def open_inbox(self):
        if os.path.isdir(Config.INBOX_DIR):
            os.startfile(Config.INBOX_DIR)
