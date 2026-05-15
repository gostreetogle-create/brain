import os
from PySide6.QtWidgets import (QWidget, QVBoxLayout, QHBoxLayout, QLabel,
                               QLineEdit, QPushButton, QGroupBox, QGridLayout,
                               QFileDialog, QMessageBox)
from PySide6.QtGui import QFont
from config import Config


class SettingsPanel(QWidget):
    def __init__(self):
        super().__init__()
        self.setup_ui()

    def setup_ui(self):
        layout = QVBoxLayout(self)
        layout.setSpacing(12)

        header = QLabel("Настройки")
        header.setFont(QFont("Segoe UI", 14, QFont.Bold))
        layout.addWidget(header)

        api_group = QGroupBox("API")
        api_grid = QGridLayout(api_group)

        api_grid.addWidget(QLabel("OpenRouter API Key:"), 0, 0)
        self.key_input = QLineEdit()
        self.key_input.setEchoMode(QLineEdit.Password)
        self.key_input.setText(Config.OPENROUTER_API_KEY)
        api_grid.addWidget(self.key_input, 0, 1)

        api_grid.addWidget(QLabel("Модель для анализа:"), 1, 0)
        self.ext_model = QLineEdit()
        self.ext_model.setText(Config.EXTRACTOR_MODEL)
        api_grid.addWidget(self.ext_model, 1, 1)

        api_grid.addWidget(QLabel("Модель для чата:"), 2, 0)
        self.chat_model = QLineEdit()
        self.chat_model.setText(Config.CHAT_MODEL)
        api_grid.addWidget(self.chat_model, 2, 1)

        btn_save = QPushButton("Сохранить")
        btn_save.clicked.connect(self.save_settings)
        api_grid.addWidget(btn_save, 3, 1)

        layout.addWidget(api_group)

        paths_group = QGroupBox("Пути")
        paths_layout = QGridLayout(paths_group)

        paths_layout.addWidget(QLabel("Папка Входящие:"), 0, 0)
        self.lbl_inbox = QLabel(Config.INBOX_DIR)
        self.lbl_inbox.setStyleSheet("color: #6c7086;")
        paths_layout.addWidget(self.lbl_inbox, 0, 1)

        paths_layout.addWidget(QLabel("Файл памяти:"), 1, 0)
        self.lbl_memory = QLabel(Config.MEMORY_FILE)
        self.lbl_memory.setStyleSheet("color: #6c7086;")
        paths_layout.addWidget(self.lbl_memory, 1, 1)

        btn_open_memory = QPushButton("Открыть файл памяти")
        btn_open_memory.clicked.connect(self.open_memory)
        paths_layout.addWidget(btn_open_memory, 2, 1)

        layout.addWidget(paths_group)

        stats_group = QGroupBox("О системе")
        stats_info = QLabel(
            f"BRAIN v1.0\n"
            f"Python + PySide6\n"
            f"Модель: {Config.EXTRACTOR_MODEL}\n"
            f"OpenRouter: https://openrouter.ai"
        )
        stats_info.setStyleSheet("color: #a6adc8; padding: 8px;")
        layout.addWidget(stats_group)
        stats_group.setLayout(QVBoxLayout())
        stats_group.layout().addWidget(stats_info)

        layout.addStretch()

    def save_settings(self):
        key = self.key_input.text().strip()
        ext = self.ext_model.text().strip()
        chat = self.chat_model.text().strip()

        env_path = os.path.join(Config.BASE_DIR, ".env")
        try:
            with open(env_path, "w", encoding="utf-8") as f:
                f.write(f"OPENROUTER_API_KEY={key}\n")
                f.write(f"EXTRACTOR_MODEL={ext}\n")
                f.write(f"CHAT_MODEL={chat}\n")
            QMessageBox.information(self, "Настройки", "Сохранено. Перезапустите приложение для применения.")
        except Exception as e:
            QMessageBox.warning(self, "Ошибка", f"Не удалось сохранить: {e}")

    def open_memory(self):
        if os.path.exists(Config.MEMORY_FILE):
            os.startfile(Config.MEMORY_FILE)
