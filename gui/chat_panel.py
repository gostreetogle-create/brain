from PySide6.QtWidgets import (QWidget, QVBoxLayout, QHBoxLayout,
                               QTextEdit, QLineEdit, QPushButton, QLabel)
from PySide6.QtCore import QThread, Signal, Qt
from PySide6.QtGui import QFont, QTextCursor
from chat import ask


class ChatWorker(QThread):
    response_ready = Signal(str)

    def __init__(self, query):
        super().__init__()
        self.query = query

    def run(self):
        try:
            result = ask(self.query)
            self.response_ready.emit(result)
        except Exception as e:
            self.response_ready.emit(f"Ошибка: {e}")


class ChatPanel(QWidget):
    def __init__(self):
        super().__init__()
        self.typing_pos = None
        self.setup_ui()

    def setup_ui(self):
        layout = QVBoxLayout(self)
        layout.setSpacing(8)

        header = QLabel("Чат с базой знаний")
        header.setFont(QFont("Segoe UI", 14, QFont.Bold))
        layout.addWidget(header)

        self.chat_history = QTextEdit()
        self.chat_history.setReadOnly(True)
        self.chat_history.setFont(QFont("Segoe UI", 10))
        layout.addWidget(self.chat_history)

        input_layout = QHBoxLayout()
        self.input_field = QLineEdit()
        self.input_field.setPlaceholderText("Введите вопрос...")
        self.input_field.setFont(QFont("Segoe UI", 10))
        self.input_field.returnPressed.connect(self.send_message)
        input_layout.addWidget(self.input_field)

        self.send_btn = QPushButton("Отправить")
        self.send_btn.clicked.connect(self.send_message)
        input_layout.addWidget(self.send_btn)

        layout.addLayout(input_layout)

    def send_message(self):
        query = self.input_field.text().strip()
        if not query:
            return

        self.chat_history.append(f"<b style='color:#89b4fa'>Вы:</b> {query}")
        self.input_field.clear()
        self.input_field.setEnabled(False)
        self.send_btn.setEnabled(False)

        cursor = self.chat_history.textCursor()
        cursor.movePosition(QTextCursor.End)
        self.typing_pos = cursor.position()
        cursor.insertHtml("<i>Печатает...</i>")

        self.worker = ChatWorker(query)
        self.worker.response_ready.connect(self.on_response)
        self.worker.start()

    def on_response(self, response):
        if self.typing_pos is not None:
            cursor = self.chat_history.textCursor()
            cursor.setPosition(self.typing_pos)
            cursor.movePosition(QTextCursor.End, QTextCursor.KeepAnchor)
            cursor.removeSelectedText()
            self.typing_pos = None

        self.chat_history.append(f"<b style='color:#a6e3a1'>Brain:</b> {response}")
        self.input_field.setEnabled(True)
        self.send_btn.setEnabled(True)
        self.input_field.setFocus()
