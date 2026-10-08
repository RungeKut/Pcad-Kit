# tests/test_dump.py — сквозная выгрузка схемы: копия -> Sch.exe -> DBX -> объекты.
import os

from _common import (TEST_SCH, EXPECT_COMPONENTS, EXPECT_NETS, start)
import pkit as pk


def main():
    r = start("test_dump")
    live = pk.env.sch_running()
    mtime_before = os.path.getmtime(TEST_SCH)
    if live:
        r.note("Sch.exe уже запущен — выгрузка из живого редактора (только чтение)")
        s = pk.dump_live()
    else:
        s = pk.dump(TEST_SCH)
    print(s)

    r.equal(len(s.comps), EXPECT_COMPONENTS, "число компонентов")
    r.equal(len(s.nets), EXPECT_NETS, "число цепей")
    r.truth("A1" in s.comps, "компонент A1 на месте")
    r.truth("MCX-BNC" in s.nets, "именованная цепь MCX-BNC на месте")

    a1 = s.comp("A1")
    r.equal(a1.type, "MODUL", "тип A1")
    r.equal(a1.attrs.get("RefDes"), "A1", "атрибут RefDes у A1")

    net0 = s.net("NET00000")
    r.truth(net0 is not None, "NET00000 есть")
    if net0 is not None:
        r.truth(("A7_XW1", "5") in net0.pins, "узел A7_XW1:5 в NET00000")

    pn = s.pin_nets()
    r.truth(len(pn) > 0, "карта выводов непуста")

    bom = s.bom()
    r.truth(len(bom) > 0, "перечень элементов непуст")
    r.note("групп в перечне: %d" % len(bom))

    r.equal(os.path.getmtime(TEST_SCH), mtime_before,
            "оригинал не тронут (mtime)")
    r.done()
    print("test_dump OK")


if __name__ == "__main__":
    main()
