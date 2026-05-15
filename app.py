import sys
import os
sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))

from PySide6.QtWidgets import QApplication
from PySide6.QtCore import Qt
from gui.main_window import MainWindow


def main():
    app = QApplication(sys.argv)
    app.setStyle("Fusion")

    dark_palette = """
    QMainWindow, QWidget { background-color: #1e1e2e; color: #cdd6f4; }
    QTabWidget::pane { background-color: #1e1e2e; border: 1px solid #313244; }
    QTabBar::tab { background-color: #313244; color: #cdd6f4; padding: 10px 20px; margin-right: 2px; }
    QTabBar::tab:selected { background-color: #45475a; border-bottom: 2px solid #89b4fa; }
    QPushButton { background-color: #45475a; color: #cdd6f4; border: none; padding: 8px 16px; border-radius: 6px; }
    QPushButton:hover { background-color: #585b70; }
    QPushButton:pressed { background-color: #313244; }
    QPushButton:disabled { background-color: #313244; color: #6c7086; }
    QTableWidget { background-color: #181825; color: #cdd6f4; gridline-color: #313244; border: 1px solid #313244; }
    QTableWidget::item:selected { background-color: #45475a; }
    QHeaderView::section { background-color: #313244; color: #cdd6f4; border: 1px solid #45475a; padding: 4px; }
    QTextEdit, QLineEdit, QComboBox { background-color: #313244; color: #cdd6f4; border: 1px solid #45475a; border-radius: 4px; padding: 4px; }
    QComboBox::drop-down { border: none; }
    QComboBox QAbstractItemView { background-color: #313244; color: #cdd6f4; selection-background-color: #45475a; }
    QLabel { color: #cdd6f4; }
    QGroupBox { border: 1px solid #45475a; border-radius: 6px; margin-top: 10px; padding-top: 10px; color: #cdd6f4; }
    QGroupBox::title { subcontrol-origin: margin; left: 10px; padding: 0 5px; }
    QScrollBar:vertical { background-color: #1e1e2e; width: 10px; }
    QScrollBar::handle:vertical { background-color: #585b70; border-radius: 5px; min-height: 20px; }
    QScrollBar::add-line:vertical, QScrollBar::sub-line:vertical { height: 0; }
    QProgressBar { background-color: #313244; border: none; border-radius: 4px; text-align: center; color: #cdd6f4; }
    QProgressBar::chunk { background-color: #89b4fa; border-radius: 4px; }
    QSplitter::handle { background-color: #45475a; }
    """

    app.setStyleSheet(dark_palette)

    window = MainWindow()
    window.show()
    sys.exit(app.exec())


if __name__ == "__main__":
    main()
