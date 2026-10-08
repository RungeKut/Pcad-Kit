#!/usr/bin/env python
# POC: поднять Sch.exe с копией схемы и выгрузить данные через DbxHost.
import json
import os
import shutil
import subprocess
import sys
import time

SCH_EXE = r"C:\Program Files (x86)\PCAD2002\Sch.exe"
HOST = os.path.join(os.path.dirname(os.path.abspath(__file__)), "DbxHost.exe")
SRC = r"Y:\#33_Radiotest\#06 Схема блока\Радиотестер схема блока_1.SCH"
WORK = os.path.join(os.environ["LOCALAPPDATA"], "Temp", "pkit_poc")


class Host:
    def __init__(self, path):
        self.proc = subprocess.Popen(
            [path], stdin=subprocess.PIPE, stdout=subprocess.PIPE,
            creationflags=0x08000000,  # CREATE_NO_WINDOW
        )

    def cmd(self, **kw):
        self.proc.stdin.write((json.dumps(kw) + "\n").encode("ascii"))
        self.proc.stdin.flush()
        line = self.proc.stdout.readline()
        if not line:
            raise RuntimeError("host died, rc=%s" % self.proc.poll())
        resp = json.loads(line.decode("utf-8"))
        if not resp.get("ok"):
            raise RuntimeError(resp.get("message"))
        return resp.get("data")


def main():
    os.makedirs(WORK, exist_ok=True)
    dst = os.path.join(WORK, "in.sch")
    shutil.copyfile(SRC, dst)
    print("копия:", dst, os.path.getsize(dst), "байт")

    sch = subprocess.Popen([SCH_EXE, dst])
    print("Sch.exe pid:", sch.pid)
    time.sleep(3)

    h = Host(HOST)
    print("ping:", h.cmd(cmd="ping"))
    t0 = time.time()
    print("open:", h.cmd(cmd="open", app="sch", wait_sec=60), f"({time.time() - t0:.1f} c)")
    print("info:", json.dumps(h.cmd(cmd="info"), ensure_ascii=False, indent=1))

    t0 = time.time()
    comps = h.cmd(cmd="components")
    print(f"components: {comps['count']} за {time.time() - t0:.1f} c")
    for c in comps["components"][:5]:
        print("  ", c["refDes"], "|", c["compType"], "|", c["value"], "| pins:", c["numberPins"])

    t0 = time.time()
    nets = h.cmd(cmd="nets")
    print(f"nets: {nets['count']} за {time.time() - t0:.1f} c")
    for n in nets["nets"][:5]:
        print("  ", n["netName"], "| nodes:", n["nodeCount"])

    one = h.cmd(cmd="comp", refdes=comps["components"][0]["refDes"])
    print("comp+pins+attrs:", json.dumps(one, ensure_ascii=False)[:600])

    net0 = nets["nets"][0]["netName"]
    nodes = h.cmd(cmd="net", name=net0)
    print("net nodes:", json.dumps(nodes, ensure_ascii=False)[:600])

    print("close:", h.cmd(cmd="close"))
    h.cmd(cmd="shutdown")
    h.proc.wait(timeout=10)
    sch.terminate()
    print("POC OK")


if __name__ == "__main__":
    try:
        main()
    except Exception as e:
        print("POC FAIL:", e)
        sys.exit(1)
