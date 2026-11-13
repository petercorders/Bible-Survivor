#!/usr/bin/env python3
"""Run every offline check of art 03 in order and record the real exit codes (standard library only).

Usage:  python3 -I scripts/verify_all.py [--catalog CATALOG_JSON] [--review]
Steps   1 build export                      expect 0
        2 checker on the export             expect 0
        3 checker + core catalog ids        expect 0   (only with --catalog)
        4 negative CLI tests (41 cases)     expect 0   (the test itself fails if any broken copy is accepted)
        4b checker mutations (5 mutants)    expect 0   (each key line of the A03-01/02 fixes, removed in a temp copy,
                                                        must make step 4 fail)
        5 byte-identical regeneration       expect 0
        5b build with Pillow blocked        expect 0   (same bytes as export/; proves the build needs NumPy only)
        6 legacy v1 (01) -> v3, pixel check expect 0   (skipped when ../art-concept-01 is absent)
        7 legacy v2 (02) -> v3, pixel check expect 0   (skipped when ../art-concept-02 is absent)
        8 converted legacy file as manifest expect 1   (a legacy file must be REJECTED)
        9 review build                      expect 0   (only with --review; needs Pillow)
        10 review GIF/APNG metadata         expect 0   (only with --review; frames, delays, loop flag vs the contract)
Writes  tests/results/verify_all.txt  (temporary folders shown as <tmp>, this folder as <art03>, the catalog as <catalog>)
Exit    0 only if every step returned its expected code. Nothing here runs Unity or a device."""
import argparse, os, platform, subprocess, sys, tempfile, zlib

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.dirname(HERE)
DOCS = os.path.dirname(ROOT)
PY = [sys.executable, "-I", "-B"]          # -B: no __pycache__ (it would hold this machine's paths)


def versions():
    v = {"python": platform.python_version(), "zlib": zlib.ZLIB_RUNTIME_VERSION}
    for mod, name in (("numpy", "numpy"), ("PIL", "Pillow")):
        r = subprocess.run(PY + ["-c", f"import {mod}; print({mod}.__version__)"], capture_output=True, text=True)
        v[name] = r.stdout.strip() if r.returncode == 0 else "not installed"
    return v


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--catalog")
    ap.add_argument("--review", action="store_true")
    a = ap.parse_args()
    out_lines, ok = [], True
    with tempfile.TemporaryDirectory(prefix="art03-verify-") as t:
        def mask(s):
            s = s.replace(t, "<tmp>").replace(ROOT, "<art03>").replace(DOCS, "..")
            return s.replace(a.catalog, "<catalog>") if a.catalog else s

        steps = [("build export", PY + [os.path.join(HERE, "build_export03.py")], 0),
                 ("checker", PY + [os.path.join(HERE, "check_art03.py"), "--quiet"], 0)]
        if a.catalog:
            steps.append(("checker + catalog", PY + [os.path.join(HERE, "check_art03.py"), "--catalog", a.catalog, "--quiet"], 0))
        steps += [("negative CLI tests", PY + [os.path.join(ROOT, "tests", "test_check_art03.py")], 0),
                  ("checker mutations (iteration 02)", PY + [os.path.join(ROOT, "tests", "checker_mutations_iter02.py")], 0),
                  ("regeneration", PY + [os.path.join(HERE, "regen_check.py")], 0),
                  ("build with Pillow blocked", PY + [os.path.join(HERE, "build_no_pillow_check.py")], 0)]
        legacy = [("v1", os.path.join(DOCS, "art-concept-01", "out")), ("v2", os.path.join(DOCS, "art-concept-02", "out"))]
        for ver, d in legacy:
            src = os.path.join(d, f"pixel-source_art-b0{ver[1]}.json")
            if os.path.exists(src):
                steps.append((f"legacy {ver} -> v3 + pixel compare",
                              PY + [os.path.join(HERE, "convert_source.py"), src, os.path.join(t, f"{ver}.json"),
                                    "--verify-png-dir", os.path.join(d, "png")], 0))
            else:
                out_lines.append(f"SKIP  legacy {ver}: {os.path.relpath(src, ROOT)} not present")
        if any(s[0].startswith("legacy v1") for s in steps):
            steps.append(("converted legacy file must be rejected",
                          PY + [os.path.join(HERE, "check_art03.py"), "--manifest", os.path.join(t, "v1.json"), "--quiet"], 1))
        if a.review:
            steps.append(("review build", PY + [os.path.join(HERE, "build_review03.py")], 0))
            steps.append(("review animation metadata", PY + [os.path.join(HERE, "anim_meta_check.py")], 0))
        for name, cmd, expect in steps:
            r = subprocess.run(cmd, cwd=ROOT, capture_output=True, text=True)
            good = r.returncode == expect
            ok &= good
            tail = [x for x in (r.stdout + r.stderr).strip().splitlines() if x.strip()][-2:]
            shown = " ".join(os.path.relpath(c, ROOT) if os.path.isabs(c) and c.startswith(DOCS) else c for c in cmd[2:])
            line = f"{'OK  ' if good else 'FAIL'}  {name:40s} exit={r.returncode} expect={expect}  $ python3 -I {mask(shown)}"
            out_lines.append(line)
            for x in tail:
                out_lines.append("        " + mask(x))
            print(out_lines[-len(tail) - 1]); [print(out_lines[-k]) for k in range(len(tail), 0, -1)]
    v = versions()
    head = ["art 03 verify_all", "versions: " + ", ".join(f"{k} {x}" for k, x in v.items()),
            "no Unity / device step exists here: Unity import and device play are NOT verified by this run", ""]
    res = "VERIFY_ALL: " + ("PASS" if ok else "FAIL")
    os.makedirs(os.path.join(ROOT, "tests", "results"), exist_ok=True)
    with open(os.path.join(ROOT, "tests", "results", "verify_all.txt"), "w", encoding="utf-8") as f:
        f.write("\n".join(head + out_lines + ["", res]) + "\n")
    print(res)
    return 0 if ok else 1


if __name__ == "__main__":
    sys.exit(main())
