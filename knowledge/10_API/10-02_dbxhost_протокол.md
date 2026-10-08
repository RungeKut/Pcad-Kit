---
id: 10-02
title: DbxHost — x86-процесс с JSON-протоколом stdin/stdout между python и Dbx32.dll
tags: [dbxhost, протокол, json, x86]
applies_to: P-CAD 2002 V17.01.22, DbxHost.exe (pkit 0.1.0)
status: проверено
verified: 2026-10-07 — tests/test_dump.py, 11 проверок зелёные
source: эксперимент
---

# Протокол DbxHost

## Что происходит

64-битный Python не может загрузить 32-битную `Dbx32.dll` (30-01), поэтому
вызовы DBX живут в отдельном x86-процессе `tools/dbx-host/DbxHost.exe`.
Протокол — REPL: на stdin по одной JSON-команде на строку, на stdout по
одному JSON-ответу: `{"ok":true,"data":...}` или
`{"ok":false,"error":N,"message":"вызов: [N] DBX_ИМЯ"}`.

## Команды

| Команда | Параметры | Ответ data |
|---|---|---|
| `ping` | — | версия хоста и путь к dll |
| `open` | `app` ("sch"), `wait_sec` | подключение с ретраями до таймаута |
| `close` | — | `TCloseDesign` |
| `save` | — | `TSaveDesign` (сохраняет открытый в редакторе файл) |
| `info` | — | `TGetDesignInfo`: units, workspace, флаг isModified |
| `components` | `pins`, `attrs` | все компоненты (+выводы, +атрибуты) |
| `comp` | `refdes`, `pins`, `attrs` | один компонент |
| `nets` | `nodes` | все цепи (+узлы: pin/port) |
| `net` | `name`, `nodes` | одна цепь |
| `shutdown` | — | закрыть дизайн и завершить хост |

## Как правильно

Пользоваться через `pkit.dbx.Host`/`Session` — они следят за жизненным
циклом процесса. Сборка после правки `DbxHost.cs`: `tools\dbx-host\build.bat`
(штатный `csc.exe` .NET Framework 4, `/platform:x86`, синтаксис C# 4).

## Как неправильно

* Запускать несколько хостов на один Sch.exe — DDE-диалог один, второй
  хост получит `DBX_ALREADY_CONNECTED`/мусор.
* Читать stdout хоста посимвольно без буфера — ответ с 205 компонентами и
  выводами это сотни КБ одной строкой; читать целой строкой.

## Чем подтверждено

`tests/test_dump.py`: полная выгрузка с выводами, атрибутами и узлами цепей
проходит в пакетном и живом режимах.
