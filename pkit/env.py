"""pkit.env — окружение P-CAD: пути, рабочие папки, консоль.

Порядок поиска установки P-CAD 2002: PKIT_PCADROOT -> стандартный путь
C:\\Program Files (x86)\\PCAD2002. Хелпер DbxHost.exe лежит в
<pkit>/../tools/dbx-host и при отсутствии собирается build.bat.
"""
import os
import subprocess
import sys

PCAD_DEFAULT = r"C:\Program Files (x86)\PCAD2002"


def pcad_root():
    root = os.environ.get("PKIT_PCADROOT", PCAD_DEFAULT)
    if not os.path.isdir(root):
        raise RuntimeError(
            "P-CAD 2002 не найден: %s. Задайте PKIT_PCADROOT." % root)
    return root


def sch_exe():
    exe = os.path.join(pcad_root(), "Sch.exe")
    if not os.path.isfile(exe):
        raise RuntimeError("Sch.exe не найден: %s" % exe)
    return exe


def pcb_exe():
    exe = os.path.join(pcad_root(), "Pcb.exe")
    if not os.path.isfile(exe):
        raise RuntimeError("Pcb.exe не найден: %s" % exe)
    return exe


def kit_root():
    return os.path.dirname(os.path.dirname(os.path.abspath(__file__)))


def host_exe(build_if_missing=True):
    exe = os.path.join(kit_root(), "tools", "dbx-host", "DbxHost.exe")
    if not os.path.isfile(exe):
        if not build_if_missing:
            raise RuntimeError("DbxHost.exe не собран: %s" % exe)
        bat = os.path.join(kit_root(), "tools", "dbx-host", "build.bat")
        r = subprocess.run([bat], capture_output=True, text=True)
        if r.returncode != 0 or not os.path.isfile(exe):
            raise RuntimeError("сборка DbxHost не удалась:\n%s\n%s"
                               % (r.stdout, r.stderr))
    return exe


def sch_running():
    """True, если хотя бы один Sch.exe уже запущен (чужой или наш)."""
    out = subprocess.run(["tasklist", "/FI", "IMAGENAME eq Sch.exe", "/NH"],
                         capture_output=True, text=True).stdout
    return "Sch.exe" in out


def work_root():
    """Рабочие папки заданий: только ASCII без пробелов (грабля 30-01)."""
    root = os.path.join(os.environ.get("LOCALAPPDATA", r"C:\Temp"),
                        "Temp", "pkit")
    try:
        root.encode("ascii")
        assert " " not in root
    except (UnicodeEncodeError, AssertionError):
        root = os.path.join(os.environ.get("SystemDrive", "C:") + "\\",
                            "pkit_work")
    os.makedirs(root, exist_ok=True)
    return root


def new_job_dir(kind="sch"):
    import datetime
    import itertools
    stamp = datetime.datetime.now().strftime("%Y%m%d-%H%M%S")
    for n in itertools.count():
        d = os.path.join(work_root(), "%s-%s-%d" % (kind, stamp, n))
        if not os.path.exists(d):
            os.makedirs(d)
            return d


def is_safe_path(path):
    try:
        path.encode("ascii")
    except UnicodeEncodeError:
        return False
    return " " not in path


def utf8_console():
    for stream in (sys.stdout, sys.stderr):
        try:
            stream.reconfigure(encoding="utf-8", errors="replace")
        except Exception:
            pass
