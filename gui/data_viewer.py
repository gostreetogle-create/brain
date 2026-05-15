import json
import os
from PySide6.QtWidgets import (QWidget, QVBoxLayout, QHBoxLayout, QLabel,
                               QTableWidget, QTableWidgetItem, QLineEdit,
                               QComboBox, QHeaderView, QPushButton, QGroupBox,
                               QAbstractItemView)
from PySide6.QtCore import Qt, QTimer
from PySide6.QtGui import QFont
from memory import load_all
from config import Config


class DataViewer(QWidget):
    def __init__(self):
        super().__init__()
        self.all_records = []
        self.setup_ui()
        self.load_data()

    def setup_ui(self):
        layout = QVBoxLayout(self)
        layout.setSpacing(8)

        header = QLabel("Просмотр данных")
        header.setFont(QFont("Segoe UI", 14, QFont.Bold))
        layout.addWidget(header)

        filter_group = QGroupBox("Фильтры")
        filter_layout = QHBoxLayout(filter_group)

        filter_layout.addWidget(QLabel("Тип:"))
        self.cmb_type = QComboBox()
        self.cmb_type.addItems(["Все", "invoice", "contract", "claim", "note", "other"])
        self.cmb_type.currentTextChanged.connect(self.apply_filters)
        filter_layout.addWidget(self.cmb_type)

        filter_layout.addWidget(QLabel("Поиск:"))
        self.txt_search = QLineEdit()
        self.txt_search.setPlaceholderText("Текст в документе...")
        self.txt_search.textChanged.connect(self.apply_filters)
        filter_layout.addWidget(self.txt_search)

        filter_layout.addWidget(QLabel("Теги:"))
        self.txt_tags = QLineEdit()
        self.txt_tags.setPlaceholderText("Фильтр по тегам...")
        self.txt_tags.textChanged.connect(self.apply_filters)
        filter_layout.addWidget(self.txt_tags)

        self.lbl_count = QLabel("Записей: 0")
        self.lbl_count.setStyleSheet("color: #f9e2af; font-weight: bold;")
        filter_layout.addWidget(self.lbl_count)

        layout.addWidget(filter_group)

        self.table = QTableWidget()
        self.table.setColumnCount(5)
        self.table.setHorizontalHeaderLabels(["Тип", "Источник", "Сущности", "Теги", "Содержание"])
        self.table.horizontalHeader().setSectionResizeMode(0, QHeaderView.ResizeToContents)
        self.table.horizontalHeader().setSectionResizeMode(1, QHeaderView.Stretch)
        self.table.horizontalHeader().setSectionResizeMode(2, QHeaderView.Stretch)
        self.table.horizontalHeader().setSectionResizeMode(3, QHeaderView.Stretch)
        self.table.horizontalHeader().setSectionResizeMode(4, QHeaderView.Stretch)
        self.table.setSelectionBehavior(QAbstractItemView.SelectRows)
        self.table.setAlternatingRowColors(True)
        self.table.verticalHeader().setVisible(False)
        self.table.setSortingEnabled(True)
        layout.addWidget(self.table)

        self.detail_label = QLabel("Выберите запись для просмотра")
        self.detail_label.setStyleSheet("color: #6c7086; padding: 4px;")
        layout.addWidget(self.detail_label)

        self.table.itemSelectionChanged.connect(self.show_detail)

    def load_data(self):
        self.all_records = load_all()
        self.apply_filters()

    def apply_filters(self):
        filter_type = self.cmb_type.currentText()
        search_text = self.txt_search.text().lower()
        tag_text = self.txt_tags.text().lower()

        filtered = []
        for r in self.all_records:
            if filter_type != "Все" and r.get("doc_type") != filter_type:
                continue
            raw = json.dumps(r, ensure_ascii=False).lower()
            if search_text and search_text not in raw:
                continue
            if tag_text:
                tags = " ".join(r.get("tags", [])).lower()
                if tag_text not in tags:
                    continue
            filtered.append(r)

        self.table.setRowCount(len(filtered))
        for i, r in enumerate(filtered):
            entities = ", ".join(e.get("name", "") for e in r.get("entities", []))
            tags = ", ".join(r.get("tags", []))
            summary = r.get("summary", "")[:150]
            self.table.setItem(i, 0, QTableWidgetItem(r.get("doc_type", "")))
            self.table.setItem(i, 1, QTableWidgetItem(r.get("source_file", "")))
            self.table.setItem(i, 2, QTableWidgetItem(entities[:100]))
            self.table.setItem(i, 3, QTableWidgetItem(tags))
            self.table.setItem(i, 4, QTableWidgetItem(summary))

        self.lbl_count.setText(f"Записей: {len(filtered)}")

    def show_detail(self):
        rows = self.table.selectedItems()
        if rows:
            row = rows[0].row()
            info = []
            for col in range(self.table.columnCount()):
                item = self.table.item(row, col)
                if item and item.text():
                    info.append(f"{self.table.horizontalHeaderItem(col).text()}: {item.text()}")
            self.detail_label.setText(" | ".join(info[:3]))
