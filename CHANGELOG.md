# Журнал изменений

Записи ведутся по правилам `knowledge/00_ПРАВИЛА.md`. Формат строки:
дата — что изменилось — почему.

## 2026-10-07 — первый выпуск: выгрузка схем P-CAD 2002

* **Скелет набора** по образцу Allegro-Kit: `skill/SKILL.md`, `pkit/`,
  `knowledge/`, `tests/`, `tools/setup.ps1`, `Install-skill.bat`.
* **DbxHost.exe** (`tools/dbx-host/`): x86-хост DBX API с JSON-протоколом
  stdin/stdout; собирается штатным csc.exe .NET Framework 4. Команды:
  ping/open/close/save/info/components/comp/nets/net/shutdown.
  Структуры и константы перенесены из канонического
  `PCAD2002/Dbx/Dbx32.h`.
* **pkit**: `pk.dump` (пакетный режим на копии), `pk.dump_live` (живой
  режим, только чтение), `Schematic` (comps/nets/bom/pin_nets),
  `pk.Report` для проверок.
* **Проверено на реальной схеме**: 205 компонентов, 206 цепей, выводы,
  атрибуты (включая кириллицу), узлы цепей; оригинал не тронут
  (`tests/test_dump.py`, 11 проверок).
* База знаний: 10-01, 10-02, 20-01, 30-01…30-05, 40-01 (30-05 — ps1 с
  кириллицей требует BOM; поймано на первом же запуске setup.ps1).
