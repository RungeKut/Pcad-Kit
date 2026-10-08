# tests/_common.py — общее для тестов pkit.
import os
import sys

sys.path.insert(0, os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
import pkit as pk  # noqa: E402

# Тестовая схема пользователя (P-CAD 2002 Binary Rev 5). Не коммитить пути
# изделия в публичный репозиторий — см. knowledge/00_ПРАВИЛА.md.
TEST_SCH = os.environ.get(
    "PKIT_TEST_SCH",
    r"Y:\#33_Radiotest\#06 Схема блока\Радиотестер схема блока_1.SCH")

# Ожидания сняты с этой схемы 2026-10-07 (первый удачный dump).
EXPECT_COMPONENTS = 205
EXPECT_NETS = 206

OUT_DIR = os.path.join(os.path.dirname(os.path.abspath(__file__)), "vyvod")


def start(name):
    pk.utf8_console()
    print("=== %s ===" % name)
    if not os.path.isfile(TEST_SCH):
        print("нет тестовой схемы:", TEST_SCH)
        sys.exit(2)
    os.makedirs(OUT_DIR, exist_ok=True)
    print("pkit", pk.__version__, "| схема:", TEST_SCH)
    return pk.Report(name)
