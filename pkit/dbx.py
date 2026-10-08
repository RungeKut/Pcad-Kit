"""pkit.dbx — сессия DBX: копия схемы + Sch.exe + DbxHost (JSON-протокол).

DBX — DDE-клиент к ЗАПУЩЕННОМУ редактору (knowledge/10_API/10-01): файл
сам по себе не читается, Dbx32.dll подключается к Sch.exe, в котором схема
уже открыта. Session делает это целиком: копирует исходник в ASCII-папку
задания, запускает Sch.exe, соединяется через DbxHost.exe (x86), а при
закрытии гасит только свой Sch.exe.

Оригинал не изменяется никогда: save() пишет копию в папке задания,
результат забирается через export_saved().
"""
import json
import os
import shutil
import subprocess
import time

from . import env

CREATE_NO_WINDOW = 0x08000000


class DbxError(RuntimeError):
    """Ошибка вызова DBX: message уже содержит код и его имя."""


class Host(object):
    """REPL-процесс DbxHost.exe. Одна JSON-команда — один JSON-ответ."""

    def __init__(self):
        self.proc = subprocess.Popen(
            [env.host_exe()], stdin=subprocess.PIPE, stdout=subprocess.PIPE,
            creationflags=CREATE_NO_WINDOW)

    def cmd(self, **kw):
        self.proc.stdin.write((json.dumps(kw) + "\n").encode("ascii"))
        self.proc.stdin.flush()
        line = self.proc.stdout.readline()
        if not line:
            raise DbxError("DbxHost умер, rc=%s" % self.proc.poll())
        resp = json.loads(line.decode("utf-8"))
        if not resp.get("ok"):
            raise DbxError(resp.get("message", "unknown host error"))
        return resp.get("data")

    def shutdown(self):
        try:
            self.cmd(cmd="shutdown")
            self.proc.wait(timeout=10)
        except Exception:
            self.proc.kill()


class Session(object):
    """Открытая в Sch.exe копия дизайна. Контекстный менеджер."""

    def __init__(self, path=None, app="sch", wait=90, live=False):
        self.app = app
        self.live = live
        self._saved = False
        self.sch_proc = None
        if live:
            # Живой режим: подключаемся к уже запущенному Sch.exe.
            # Ничего не запускаем и при close() НЕ гасим чужой редактор.
            if not env.sch_running():
                raise DbxError("живой режим: Sch.exe не запущен")
            self.src = None
            self.job_dir = None
            self.work_file = None
        else:
            src = os.path.abspath(path)
            if not os.path.isfile(src):
                raise DbxError("файл не найден: %s" % src)
            if env.sch_running():
                raise DbxError(
                    "Sch.exe уже запущен — пакетный режим отказал: DDE "
                    "подключится к непредсказуемому экземпляру "
                    "(knowledge/30_ГРАБЛИ/30-03). Закройте P-CAD SCH или "
                    "используйте live=True (только чтение).")
            self.src = src
            self.job_dir = env.new_job_dir(app)
            self.work_file = os.path.join(self.job_dir, "in.sch")
            shutil.copyfile(src, self.work_file)
            self.sch_proc = subprocess.Popen(
                [env.sch_exe(), self.work_file], cwd=self.job_dir)
        self.host = Host()
        try:
            self.host.cmd(cmd="open", app=app, wait_sec=wait)
        except Exception:
            self.close()
            raise

    def __enter__(self):
        return self

    def __exit__(self, *exc):
        self.close()
        return False

    # --- данные ---------------------------------------------------------

    def info(self):
        return self.host.cmd(cmd="info")

    def components(self, pins=False, attrs=False):
        return self.host.cmd(cmd="components", pins=pins, attrs=attrs)

    def comp(self, refdes, pins=True, attrs=True):
        return self.host.cmd(cmd="comp", refdes=refdes, pins=pins, attrs=attrs)

    def nets(self, nodes=False):
        return self.host.cmd(cmd="nets", nodes=nodes)

    def net(self, name, nodes=True):
        return self.host.cmd(cmd="net", name=name, nodes=nodes)

    # --- сохранение -----------------------------------------------------

    def save(self):
        """Сохранить копию в папке задания (не оригинал!). В живом режиме
        запрещено: сохранение чужого редактора — за человеком."""
        if self.live:
            raise DbxError("save() в живом режиме запрещён (knowledge/30_ГРАБЛИ/30-04)")
        self.host.cmd(cmd="save")
        self._saved = True

    def export_saved(self, dst):
        """Забрать сохранённую копию из папки задания новым файлом."""
        if not self._saved:
            raise DbxError("сначала save(); оригинал не перезаписывается")
        if os.path.abspath(dst) == self.src:
            raise DbxError("export_saved в исходный файл запрещён")
        shutil.copyfile(self.work_file, dst)
        return dst

    # --- завершение -----------------------------------------------------

    def close(self):
        try:
            if self.host is not None:
                self.host.shutdown()
        finally:
            self.host = None
            # Свой Sch.exe гасим, чужой (живой режим) — ни в коем случае.
            if (not self.live and self.sch_proc is not None
                    and self.sch_proc.poll() is None):
                self.sch_proc.terminate()
                try:
                    self.sch_proc.wait(timeout=10)
                except Exception:
                    self.sch_proc.kill()
            self.sch_proc = None


def open_design(path, app="sch", wait=90):
    """Открыть копию дизайна: with open_design(p) as s: s.components()"""
    return Session(path, app=app, wait=wait)


def open_live(app="sch", wait=30):
    """Подключиться к уже запущенному редактору (только чтение)."""
    return Session(app=app, wait=wait, live=True)
