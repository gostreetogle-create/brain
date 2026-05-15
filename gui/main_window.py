from PySide6.QtWidgets import QMainWindow, QTabWidget, QMessageBox
from PySide6.QtCore import QTimer
from gui.dashboard import DashboardWidget
from gui.chat_panel import ChatPanel
from gui.data_viewer import DataViewer
from gui.settings_panel import SettingsPanel
from memory import count as memory_count
from config import Config


class MainWindow(QMainWindow):
    def __init__(self):
        super().__init__()
        self.setWindowTitle("BRAIN — Цифровой Сотрудник")
        self.resize(1100, 720)
        self.setMinimumSize(800, 500)

        self.tabs = QTabWidget()
        self.setCentralWidget(self.tabs)

        self.dashboard = DashboardWidget(self)
        self.chat = ChatPanel()
        self.viewer = DataViewer()
        self.settings = SettingsPanel()

        self.tabs.addTab(self.dashboard, "Главная")
        self.tabs.addTab(self.chat, "Чат")
        self.tabs.addTab(self.viewer, "Данные")
        self.tabs.addTab(self.settings, "Настройки")

        self.timer = QTimer()
        self.timer.setInterval(3000)
        self.timer.timeout.connect(self._on_timer)
        self.timer.start()

    def _on_timer(self):
        self.dashboard.refresh_stats()
