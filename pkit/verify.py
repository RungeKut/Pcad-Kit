"""pkit.verify — накопитель проверок (как ak.Report в Allegro-Kit).

    r = pk.Report("схема блока")
    r.equal(len(s.comps), 205, "число компонентов")
    r.truth("GND" in s.nets, "цепь GND на месте")
    r.done()   # печатает итог и падает, если были провалы
"""


class Report(object):
    def __init__(self, name=""):
        self.name = name
        self._fails = []
        self._count = 0

    def _check(self, ok, label, detail=""):
        self._count += 1
        if not ok:
            self._fails.append("%s %s" % (label, detail))
        return ok

    def equal(self, got, expect, label):
        return self._check(got == expect, label, "(got %r, expect %r)" % (got, expect))

    def truth(self, cond, label):
        return self._check(bool(cond), label)

    def same_set(self, got, expect, label):
        g, e = set(got), set(expect)
        return self._check(g == e, label,
                           "(лишние %s, недостают %s)"
                           % (sorted(g - e)[:5], sorted(e - g)[:5]))

    def note(self, text):
        print("  # %s" % text)

    def done(self, verbose=True):
        if verbose:
            print("Report[%s]: %d проверок, %d провалов"
                  % (self.name, self._count, len(self._fails)))
            for f in self._fails:
                print("  FAIL:", f)
        if self._fails:
            raise AssertionError("%d проверок провалено" % len(self._fails))
        return True
