# Pcad-Kit — схемы P-CAD 2002 скриптами

Чтение и правка схем P-CAD 2002 (.SCH) из Python через штатный DBX API
редактора — без ручной работы в Sch.exe. По образцу Allegro-Kit (Cadence).

```
Pcad-Kit/
├── skill/SKILL.md          — скилл Kimi Code (junction ~/.kimi-code/skills/pcad)
├── pkit/                   — Python-пакет: env / dbx / sch / verify
├── tools/dbx-host/         — DbxHost.cs → DbxHost.exe (x86, JSON stdin/stdout)
├── knowledge/              — база проверенного опытом знания (INDEX.md)
├── tests/                  — сквозные тесты (python tests\test_*.py)
├── tools/setup.ps1         — установка: junction + PKIT_HOME + сборка + дым-тест
└── Install-skill.bat       — обёртка setup.ps1 для двойного щелчка
```

## Как это устроено

`Dbx32.dll` из поставки P-CAD 2002 — DDE-клиент к **запущенному** редактору:
файл сама DLL не читает, `TOpenDesign("sch")` подключается к Sch.exe, в
котором схема открыта. DLL 32-битная, поэтому вызовы живут в x86-процессе
`DbxHost.exe` (собирается штатным `csc.exe` .NET Framework 4), а Python
(любой разрядности) говорит с ним JSON-строками через stdin/stdout.

Два режима:

* **пакетный** — `pk.dump(path)`: копия в `Temp\pkit`, свой Sch.exe,
  выгрузка, процесс погашен; оригинал не трогается (проверено по mtime);
* **живой** — `pk.dump_live()`: чтение дизайна из уже открытого человеком
  Sch.exe; только чтение, редактор не сохраняем и не закрываем.

## Быстрый старт

```python
import os, sys
sys.path.insert(0, os.environ["PKIT_HOME"])
import pkit as pk

pk.utf8_console()
s = pk.dump(r"D:\проект\схема.SCH")
print(s)                    # 205 компонентов, 206 цепей
print(s.bom())              # перечень элементов
print(s.pin_nets())         # {(refdes, pindes): netname}
```

## Установка на новой машине

`git clone` → двойной щелчок по `Install-skill.bat` (или
`powershell -ExecutionPolicy Bypass -File tools\setup.ps1`). Требования:
P-CAD 2002, Python 3.11+, .NET Framework 4 (есть в Windows из коробки).

## Что проверено

2026-10-07, P-CAD 2002 V17.01.22, Python 3.14 x64: полная выгрузка
реальной схемы (205 компонентов, 206 цепей, выводы, атрибуты с кириллицей,
узлы цепей) в пакетном и живом режиме — `tests/test_dump.py`, 11 проверок.
Правка (TModify*/TPlace*) в pkit пока не выносилась — API разобран,
реализация по мере надобности. Записи без `verified` силы не имеют —
см. `knowledge/00_ПРАВИЛА.md`.

## Работа с репозиторием

В начале — `git pull --rebase`; после каждой правки набора — `git diff`
глазами, коммит, `git push`. Без права записи правка уходит pull
request'ом из своего форка; владелец разбирает открытые PR при каждом
pull (`gh pr list`). **В набор не попадает ничего о проектируемом
изделии** — ни в файлы, ни в текст коммита. Порядок —
`knowledge/40_СРЕДА/40-02`.

## Лицензия

MIT. P-CAD и ACCEL — торговые марки правообладателей; набор не
аффилирован с ними.
