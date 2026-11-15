#!/usr/bin/env python3
"""Run every offline check of the R02 enemy sidecar in order and record the real exit codes.

Usage:  python3 -I -B scripts/verify_all.py [--catalog FILE] [--art03 DIR] [--font-root DIR] [--review]
Steps   1 regenerate with the generator (numpy) into a temp folder: pixel source, 3 PNGs, manifest byte-identical  expect 0
        2 stdlib: convert the COMMITTED pixel source into a temp folder: 3 PNGs byte-identical                   expect 0
        3 stdlib: rebuild the manifest from step 2's files: byte-identical                                     expect 0
        4 stdlib: contract checker (art 03 refs, review fonts, catalog when given)                              expect 0
        5 stdlib: PRODUCER self-check of the checker (16 broken inputs + positive; not QA's injection)          expect 0
        5b stdlib: PRODUCER replay of the 54 R02-01 QA inputs (tests/replay_qa_r02_01.py; not QA's result)      expect 0
        5c stdlib: PRODUCER R02-04 pixel source identity, 9 cases (tests/source_identity_r02_04.py; not QA's)    expect 0
        6 copies from art 03 (palette_v03.json, px03.py, pngio.py) byte-identical to the art 03 package         (in-process)
        7 review boards / loops / metrics (numpy + Pillow; only with --review)                                  expect 0
Steps marked stdlib run with `python3 -I -S -B` (no site-packages).
Writes  tests/results/verify_all.txt (temp folders as <tmp>, this folder as <pkg>, siblings as ..).
Exit    0 only if every step returned its expected result. Unity import, Unity rendering and devices are NOT_RUN."""
import argparse, hashlib, os, platform, shutil, subprocess, sys, tempfile

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.dirname(HERE)
STD = [sys.executable, "-I", "-S", "-B"]
NP = [sys.executable, "-I", "-B"]
PNGS = ["export/png/enm_b1-envy-eye_move_32.png", "export/png/enm_b2-rage-hound_run_32.png",
        "export/png/enm_b3-cracked-mask_move_32.png"]
SRC = "export/source/pixel-source_r02.json"
MAN = "export/manifest_r02.json"


def same(a, b):
    return os.path.isfile(a) and os.path.isfile(b) and open(a, "rb").read() == open(b, "rb").read()


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--catalog")
    ap.add_argument("--art03", default=os.path.join(os.path.dirname(ROOT), "art-concept-03"))
    ap.add_argument("--font-root", default=os.path.join(os.path.dirname(ROOT), "art-unity-font-01"))
    ap.add_argument("--review", action="store_true")
    a = ap.parse_args()
    out, ok = [], True
    with tempfile.TemporaryDirectory(prefix="r02-verify-") as t1, tempfile.TemporaryDirectory(prefix="r02-verify-") as t2:
        def mask(s):
            for p, r in ((t1, "<tmp1>"), (t2, "<tmp2>"), (ROOT, "<pkg>"), (os.path.dirname(ROOT), "..")):
                s = s.replace(p, r)
            return s.replace(a.catalog, "<catalog>") if a.catalog else s

        def run(name, cmd, expect, compares=()):
            nonlocal ok
            r = subprocess.run(cmd, cwd=ROOT, capture_output=True, text=True)
            good = r.returncode == expect
            extra = []
            if good:
                for x, y in compares:
                    s_ = same(x, y)
                    good &= s_
                    extra.append(f"{os.path.relpath(x, ROOT) if x.startswith(ROOT) else mask(x)}: {'identical' if s_ else 'DIFFERENT'}")
            ok &= good
            tail = [l for l in (r.stdout + r.stderr).strip().splitlines() if l.strip()][-2:]
            flags = "-I -S -B" if cmd[:4] == STD else "-I -B"
            shown = " ".join(os.path.relpath(c, ROOT) if os.path.isabs(c) and c.startswith(ROOT) else c
                             for c in cmd[(4 if cmd[:4] == STD else 3):])
            out.append(f"{'OK  ' if good else 'FAIL'}  {name:44s} exit={r.returncode} expect={expect}  $ python3 {flags} {mask(shown)}")
            out.extend("        " + mask(l) for l in tail + extra)
            print("\n".join(out[-(1 + len(tail) + len(extra)):]), flush=True)

        run("1 regenerate (generator, numpy)", NP + [os.path.join(HERE, "build_r02.py"), "--out", t1], 0,
            [(os.path.join(ROOT, *p.split("/")), os.path.join(t1, *p.split("/"))) for p in [SRC] + PNGS + [MAN]])
        run("2 stdlib convert committed pixel source", STD + [os.path.join(HERE, "convert_source.py"), "--out", t2], 0,
            [(os.path.join(ROOT, *p.split("/")), os.path.join(t2, *p.split("/"))) for p in PNGS])
        os.makedirs(os.path.join(t2, "export", "source"), exist_ok=True)
        shutil.copyfile(os.path.join(ROOT, *SRC.split("/")), os.path.join(t2, *SRC.split("/")))
        run("3 stdlib rebuild manifest", STD + [os.path.join(HERE, "make_manifest.py"), "--out", t2], 0,
            [(os.path.join(ROOT, *MAN.split("/")), os.path.join(t2, *MAN.split("/")))])
        chk = STD + [os.path.join(HERE, "check_r02.py"), "--art03", a.art03, "--font-root", a.font_root]
        run("4 stdlib contract checker", chk + (["--catalog", a.catalog] if a.catalog else []), 0)
        run("5 stdlib producer self-check (not QA)", STD + [os.path.join(ROOT, "tests", "selftest_r02.py")]
            + (["--catalog", a.catalog] if a.catalog else []), 0)
        run("5b stdlib R02-01 QA input replay (producer)", STD + [os.path.join(ROOT, "tests", "replay_qa_r02_01.py"),
            "--art03", a.art03, "--font-root", a.font_root] + (["--catalog", a.catalog] if a.catalog else []), 0)
        run("5c stdlib R02-04 pixel source identity (producer)", STD + [os.path.join(ROOT, "tests", "source_identity_r02_04.py"),
            "--art03", a.art03, "--font-root", a.font_root] + (["--catalog", a.catalog] if a.catalog else []), 0)
    copies =[("src/palette_v03.json", "src/palette_v03.json"), ("scripts/px03.py", "scripts/px03.py"),
              ("scripts/pngio.py", "scripts/pngio.py")]
    res = []
    for mine, theirs in copies:
        s_ = same(os.path.join(ROOT, *mine.split("/")), os.path.join(a.art03, *theirs.split("/")))
        res.append(f"{mine} == art-concept-03/{theirs}: {'identical' if s_ else 'DIFFERENT'}")
        ok &= s_
    out.append(f"{'OK  ' if all('identical' in x for x in res) else 'FAIL'}  6 copies from art 03 byte-identical")
    out.extend("        " + x for x in res)
    print("\n".join(out[-4:]))
    if a.review:
        run_review = subprocess.run(NP + [os.path.join(HERE, "review_r02.py"), "--art03", a.art03, "--font-root", a.font_root],
                                    cwd=ROOT, capture_output=True, text=True)
        good = run_review.returncode == 0
        ok &= good
        out.append(f"{'OK  ' if good else 'FAIL'}  {'7 review boards (numpy + Pillow)':44s} exit={run_review.returncode} expect=0  $ python3 -I -B scripts/review_r02.py")
        out.extend("        " + l for l in run_review.stdout.strip().splitlines()[-2:])
    else:
        out.append("SKIP  7 review boards (no --review)")
    print("\n".join(out[-3:]))
    head = ["art-genesis-r02 verify_all", f"python {platform.python_version()} (stdlib steps use -S: no site-packages)",
            "Unity import, Unity rendering, device checks and QA's independent error injection are NOT part of this run", ""]
    resl = "VERIFY_ALL: " + ("PASS" if ok else "FAIL")
    os.makedirs(os.path.join(ROOT, "tests", "results"), exist_ok=True)
    with open(os.path.join(ROOT, "tests", "results", "verify_all.txt"), "w", encoding="utf-8") as f:
        f.write("\n".join(head + out + ["", resl]) + "\n")
    print(resl)
    return 0 if ok else 1


if __name__ == "__main__":
    sys.exit(main())
