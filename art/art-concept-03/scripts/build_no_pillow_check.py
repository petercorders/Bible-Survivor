#!/usr/bin/env python3
"""Show that the export build needs no Pillow (standard library only).

Usage:  python3 -I scripts/build_no_pillow_check.py
Runs scripts/build_export03.py in a child Python in which every import of PIL raises ImportError, writing into a fresh
temporary folder, then compares every exported file byte-for-byte (sha256) with export/.
Writes tests/results/build_no_pillow.txt. Exit 0 = the build ran with Pillow blocked and every file is identical,
1 = the build failed or a file differs."""
import hashlib, os, subprocess, sys, tempfile

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.dirname(HERE)

CHILD = r'''
import sys, runpy
class NoPIL:
    def find_spec(self, name, path=None, target=None):
        if name == "PIL" or name.startswith("PIL."):
            raise ImportError("Pillow is blocked for this run")
        return None
sys.meta_path.insert(0, NoPIL())
try:
    import PIL
    print("PIL blocked: no"); sys.exit(3)
except ImportError:
    print("PIL blocked: yes")
build = sys.argv[1]
sys.argv = [build] + sys.argv[2:]
runpy.run_path(build, run_name="__main__")
'''


def tree(d):
    out = {}
    for dp, _, fs in os.walk(d):
        for f in fs:
            p = os.path.join(dp, f)
            out[os.path.relpath(p, d).replace(os.sep, "/")] = hashlib.sha256(open(p, "rb").read()).hexdigest()
    return out


def main():
    log = []
    with tempfile.TemporaryDirectory(prefix="art03-nopil-") as t:
        r = subprocess.run([sys.executable, "-I", "-B", "-c", CHILD, os.path.join(HERE, "build_export03.py"), "--out", t],
                           cwd=ROOT, capture_output=True, text=True)
        out = [x.replace(t, "<tmp>") for x in (r.stdout + r.stderr).strip().splitlines()]
        log += ["build with Pillow blocked: exit %d" % r.returncode] + ["  " + x for x in out[-3:]]
        code = 1
        if r.returncode == 0:
            a, b = tree(os.path.join(ROOT, "export")), tree(os.path.join(t, "export"))
            diff = sorted(k for k in set(a) | set(b) if a.get(k) != b.get(k))
            log.append(f"files: export {len(a)}, rebuilt without Pillow {len(b)}; identical {len(set(a) & set(b)) - len(diff)}")
            log += ["  differs: " + k for k in diff[:10]]
            code = 0 if not diff else 1
    log.append("NO_PILLOW_BUILD: " + ("PASS (byte-identical)" if code == 0 else "FAIL"))
    os.makedirs(os.path.join(ROOT, "tests", "results"), exist_ok=True)
    with open(os.path.join(ROOT, "tests", "results", "build_no_pillow.txt"), "w", encoding="utf-8") as f:
        f.write("\n".join(log) + "\n")
    print("\n".join(log))
    return code


if __name__ == "__main__":
    sys.exit(main())
