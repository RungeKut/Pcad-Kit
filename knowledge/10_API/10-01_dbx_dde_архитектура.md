---
id: 10-01
title: DBX — это DDE-клиент к запущенному редактору, а не читалка файлов
tags: [dbx, dde, архитектура, TOpenDesign]
applies_to: P-CAD 2002 V17.01.22, Dbx32.dll v16.000
status: проверено
verified: 2026-10-07 — DbxHost.exe подключился к Sch.exe и выгрузил 205 компонентов и 206 цепей
source: эксперимент
---

# Архитектура DBX: DDE к запущенному Sch.exe/Pcb.exe

## Что происходит

`Dbx32.dll` не открывает .SCH/.PCB сама. `TOpenDesign(language=0,
version=16000, pAppName="sch", ctx)` устанавливает DDE-диалог с **уже
запущенным** редактором (поля контекста: `HCONV hConv`, `HWND hWnd`,
`HANDLE hMmf` — memory-mapped файл) и оперирует дизайном, открытым в нём.
Поэтому рабочий цикл всегда: запустить `Sch.exe <файл>` → подключиться →
читать/править → `TCloseDesign`.

## Почему

P-CAD 2002 (ACCEL) проектировался в эпоху DDE; API отдали как клиентскую
DLL, а сервером выступает сам редактор. Файл без редактора через DBX не
читается вообще.

## Как правильно

`pkit.dbx.Session` делает цикл целиком (копия → запуск → коннект → сбор).
Сигнатуры и структуры — канонический `Dbx32.h` в
`C:\Program Files (x86)\PCAD2002\Dbx\` (там же `ACCELDBX.doc` и примеры
`.BAS`/`.cpp`).

Итерация: `TGetFirstXxx` → `TGetNextXxx` до кода-конца
(`DBX_NO_MORE_COMPONENTS=32168`, `DBX_NO_MORE_NETS=32162`,
`DBX_NO_MORE_ITEMS=32163`); конец итерации — не 0 и не исключение.
Выводы компонента: `TGetFirstCompPin(ctx, refDes, &pin)` →
`TGetNextCompPin`. Атрибуты: `TGetFirstCompAttribute`/`Next`. Узлы цепи:
`TGetFirstNetNode(ctx, netId, &item)` → `TGetNextNetNode`, в `TItem`
смотреть `itemType`: 39=pin (тогда `item.pin.compRefDes` +
`item.pin.compPin.pinDes`), 50=port.

Координаты — database units: 100000 на мм, 2540 на mil (`DbxUtils.h`).

## Как неправильно

* Грузить `Dbx32.dll` и ждать, что она прочитает файл с диска — такого
  вызова нет вообще.
* Итерировать `TGetNextComponent`, пока статус не станет ненулевым, и
  считать 32168 ошибкой — это штатный конец списка.
* Держать вложенные итераторы (компоненты снаружи, выводы внутри):
  курсоры на сервере перебивают друг друга. Сначала собрать список
  компонентов, потом для каждого — выводы (так сделано в DbxHost).

## Чем подтверждено

POC `tools/dbx-host/poc.py` на реальной схеме: подключение за 1.8 с,
205 компонентов / 206 цепей, выводы и атрибуты (включая кириллический
атрибут ПЭ3) читаются, узлы цепей возвращают (refDes, pinDes).
