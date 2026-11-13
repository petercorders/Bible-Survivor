#!/usr/bin/env python3
"""Regeneration check: rebuild the runtime export into a fresh temporary folder from the permanent sources
(src/, scripts/, third_party/) and compare every exported file byte-for-byte (sha256) with export/.

Usage:  python3 -I scripts/regen_check.py
Writes tests/results/regen_results.txt. Exit 0 = identical, 1 = differences, 2 = build failed."""
import hashlib, os, subprocess, sys, tempfile

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.dirname(HERE)


def tree(base):
    out = {}
    for d, _, fs in os.walk(base):
        for f in fs:
            p = os.path.join(d, f)
            out[os.path.relpath(p, base).replace(os.sep, "/")] = hashlib.sha256(open(p, "rb").read()).hexdigest()
    return out


def main():
    with tempfile.TemporaryDirectory(prefix="art03-regen-") as t:
        r = subprocess.run([sys.executable, "-I", os.path.join(HERE, "build_export03.py"), "--out", t], capture_output=True, text=True)
        log = [f"build exit={r.returncode}: {r.stdout.strip().replace(t, '<tmp>')} {r.stderr.strip()[-400:]}"]
        if r.returncode:
            code = 2
        else:
            a, b = tree(os.path.join(ROOT, "export")), tree(os.path.join(t, "export"))
            only_a, only_b = sorted(set(a) - set(b)), sorted(set(b) - set(a))
            diff = sorted(k for k in set(a) & set(b) if a[k] != b[k])
            log.append(f"files: committed {len(a)}, regenerated {len(b)}; identical {len(set(a) & set(b)) - len(diff)}")
            log += [f"  only in export/: {k}" for k in only_a] + [f"  only in regenerated: {k}" for k in only_b] + [f"  differs: {k}" for k in diff]
            code = 0 if not (only_a or only_b or diff) else 1
            log.append("REGEN: " + ("PASS (byte-identical)" if code == 0 else "FAIL"))
    os.makedirs(os.path.join(ROOT, "tests", "results"), exist_ok=True)
    open(os.path.join(ROOT, "tests", "results", "regen_results.txt"), "w", encoding="utf-8").write("\n".join(log) + "\n")
    print("\n".join(log))
    return code


if __name__ == "__main__":
    sys.exit(main())
