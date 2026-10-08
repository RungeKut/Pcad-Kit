"""pkit — работа со схемами P-CAD 2002 (.SCH) через DBX API.

    import os, sys
    sys.path.insert(0, os.environ.get("PKIT_HOME",
        r"C:\\Users\\bolshakov-sp\\Desktop\\Pcad-Kit"))
    import pkit as pk

    pk.utf8_console()
    s = pk.dump(r"D:\\проект\\схема.SCH")
"""
from .env import (utf8_console, pcad_root, sch_exe, kit_root, is_safe_path,
                  work_root)
from .dbx import Session, Host, DbxError, open_design, open_live
from .sch import Schematic, Component, Net, dump, dump_live
from .verify import Report

__version__ = "0.1.0"

__all__ = [
    "utf8_console", "pcad_root", "sch_exe", "kit_root", "is_safe_path",
    "work_root", "Session", "Host", "DbxError", "open_design", "open_live",
    "Schematic", "Component", "Net", "dump", "dump_live", "Report",
]
