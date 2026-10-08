"""pkit.sch — высокоуровневая выгрузка схемы P-CAD (.SCH) в объект Schematic.

    import pkit as pk
    s = pk.dump(r"D:\\проект\\схема.SCH")
    print(s)                  # сводка
    s.bom()                   # перечень элементов
    s.pin_nets()              # {(refdes, pindes): netname}

Координаты в сырых данных — database units: 100000 на мм, 2540 на mil
(DbxUtils.h). Schematic.to_mm() переводит.
"""
from collections import OrderedDict

from . import dbx

DB_PER_MM = 100000.0
DB_PER_MIL = 2540.0


class Component(object):
    def __init__(self, raw):
        self.refdes = raw["refDes"]
        self.type = raw["compType"]
        self.value = raw["value"]
        self.pattern = raw["patternName"]
        self.library = raw["libraryName"]
        self.number_pins = raw["numberPins"]
        self.pins = raw.get("pins", [])       # [{pinDes, pinName, netId, ...}]
        self.attrs = {a["type"]: a["value"] for a in raw.get("attrs", [])}

    def __repr__(self):
        return "<Component %s %s %s>" % (self.refdes, self.type, self.value)


class Net(object):
    def __init__(self, raw):
        self.name = raw["netName"]
        self.net_id = raw["netId"]
        self.node_count = raw["nodeCount"]
        self.pins = [(n["refDes"], n["pinDes"])
                     for n in raw.get("nodes", []) if n.get("kind") == "pin"]

    def __repr__(self):
        return "<Net %s pins=%d>" % (self.name, len(self.pins))


class Schematic(object):
    """Разобранная выгрузка схемы."""

    def __init__(self, path, info, comps, nets):
        self.path = path
        self.info = info
        self.comps = OrderedDict((c.refdes, c) for c in
                                 (Component(r) for r in comps))
        self.nets = OrderedDict((n.name, n) for n in
                                (Net(r) for r in nets))

    def __str__(self):
        return "Schematic(%s): %d компонентов, %d цепей" % (
            self.path, len(self.comps), len(self.nets))

    # --- выборки --------------------------------------------------------

    def refdes(self):
        return sorted(self.comps)

    def comp(self, refdes):
        return self.comps.get(refdes)

    def net(self, name):
        return self.nets.get(name)

    def bom(self):
        """Перечень элементов: {(type, value): [refdes...]} по алфавиту."""
        groups = {}
        for c in self.comps.values():
            groups.setdefault((c.type, c.value), []).append(c.refdes)
        for k in groups:
            groups[k].sort()
        return OrderedDict(sorted(groups.items()))

    def pin_nets(self):
        """{(refdes, pindes): netname} — связность по общим выводам."""
        out = {}
        for n in self.nets.values():
            for refdes, pindes in n.pins:
                out[(refdes, pindes)] = n.name
        return out

    # --- единицы --------------------------------------------------------

    @staticmethod
    def to_mm(v):
        return v / DB_PER_MM

    @staticmethod
    def to_mil(v):
        return v / DB_PER_MIL


def dump(path, wait=90):
    """Полная выгрузка схемы с копии: компоненты с выводами и атрибутами,
    цепи с узлами. Оригинал не открывается и не меняется."""
    with dbx.open_design(path, wait=wait) as s:
        return _collect(s, path)


def dump_live(wait=30):
    """Выгрузка дизайна, открытого прямо сейчас в запущенном Sch.exe.
    Только чтение: редактор человека не сохраняем и не закрываем."""
    with dbx.open_live(wait=wait) as s:
        return _collect(s, "<live>")


def _collect(s, path):
    info = s.info()
    comps = s.components(pins=True, attrs=True)["components"]
    nets = s.nets(nodes=True)["nets"]
    return Schematic(path, info, comps, nets)
