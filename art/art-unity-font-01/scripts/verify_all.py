#!/usr/bin/env python3
"""Run every offline check of art-unity-font-01 in order and record the real exit codes (standard library only).

Usage:  python3 -I -B scripts/verify_all.py [--zip GALMURI_ZIP] [--bitmap-dir DIR] [--mutations] [--render]
Steps   1 regenerate the manifest in a temp folder, byte compare            expect 0
        2 regenerate the text metrics in a temp folder, byte compare        expect 0
        3 checker                                                         expect 0
        4 checker --zip (official release, byte-identical members)        expect 0   (only with --zip)
        5 negative CLI tests (26 cases; zip/bitmap cases need the inputs) expect 0
        6 inspect the packaged fonts: all 'outline'                        expect 0
        7 inspect the art 03 Bitmap fonts: must NOT pass as outline        expect 1   (skipped without them)
        8 checker mutations (8 mutants, about 3 minutes)                   expect 0   (only with --mutations)
        9 render compare with Pillow (FreeType, not Unity)                 expect 0   (only with --render)
Steps 1-8 run with `python3 -I -S -B`: no site-packages, so they cannot use anything outside the standard library.
Writes  tests/results/verify_all.txt (temp folders as <tmp>, the zip as <zip>, this folder as <pkg>).
Exit    0 only if every step returned its expected code. Unity import / rendering / devices are NOT_RUN here."""
import argparse, os, platform, subprocess, sys, tempfile

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.dirname(HERE)
STD = [sys.executable, "-I", "-S", "-B"]


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--zip")
    ap.add_argument("--bitmap-dir", default=os.path.join(os.path.dirname(ROOT), "art-concept-03", "third_party", "galmuri"))
    ap.add_argument("--mutations", action="store_true")
    ap.add_argument("--render", action="store_true")
    a = ap.parse_args()
    bitmaps = sorted(os.path.join(a.bitmap_dir, f) for f in os.listdir(a.bitmap_dir)
                     if "Bitmap" in f and f.endswith(".ttf")) if os.path.isdir(a.bitmap_dir) else []
    fonts = sorted(os.path.join(ROOT, "fonts", "galmuri-outline", f)
                   for f in os.listdir(os.path.join(ROOT, "fonts", "galmuri-outline")) if f.endswith(".ttf"))
    out, ok = [], True
    with tempfile.TemporaryDirectory(prefix="font01-verify-") as t:
        def mask(s):
            s = s.replace(t, "<tmp>").replace(ROOT, "<pkg>")
            s = s.replace(os.path.dirname(ROOT), "..")
            return s.replace(a.zip, "<zip>") if a.zip else s

        def compare(rel):
            x, y = os.path.join(ROOT, rel), os.path.join(t, rel)
            same = os.path.isfile(y) and open(x, "rb").read() == open(y, "rb").read()
            return f"{rel}: {'identical' if same else 'DIFFERENT'}", same

        tests = STD + [os.path.join(ROOT, "tests", "test_check_fonts.py"), "--bitmap-dir", a.bitmap_dir]
        if a.zip:
            tests += ["--zip", a.zip]
        steps = [("regenerate manifest", STD + [os.path.join(HERE, "build_font_package.py"), "--out", t], 0,
                  "export/font_manifest_v1.json"),
                 ("regenerate text metrics", STD + [os.path.join(HERE, "measure_text.py"), "--out", t], 0,
                  "export/text_metrics_v1.json"),
                 ("checker", STD + [os.path.join(HERE, "check_fonts.py"), "--quiet"], 0, None)]
        if a.zip:
            steps.append(("checker --zip", STD + [os.path.join(HERE, "check_fonts.py"), "--zip", a.zip, "--quiet"], 0, None))
        steps.append(("negative CLI tests", tests, 0, None))
        steps.append(("inspect packaged fonts", STD + [os.path.join(HERE, "inspect_font.py")] + fonts, 0, None))
        if bitmaps:
            steps.append(("inspect art 03 Bitmap fonts (must fail)", STD + [os.path.join(HERE, "inspect_font.py")] + bitmaps, 1, None))
        else:
            out.append("SKIP  inspect art 03 Bitmap fonts: not found")
        if a.mutations:
            mut = STD + [os.path.join(ROOT, "tests", "checker_mutations.py"), "--bitmap-dir", a.bitmap_dir]
            steps.append(("checker mutations", mut + (["--zip", a.zip] if a.zip else []), 0, None))
        if a.render:
            steps.append(("render compare (Pillow)", [sys.executable, "-I", "-B", os.path.join(HERE, "render_compare.py"),
                                                      "--bitmap-dir", a.bitmap_dir], 0, None))
        for name, cmd, expect, cmp in steps:
            r = subprocess.run(cmd, cwd=ROOT, capture_output=True, text=True)
            good = r.returncode == expect
            extra = []
            if cmp and good:
                msg, same = compare(cmp)
                good &= same
                extra.append(msg)
            ok &= good
            tail = [x for x in (r.stdout + r.stderr).strip().splitlines() if x.strip()][-2:]
            shown = " ".join(os.path.relpath(c, ROOT) if os.path.isabs(c) and c.startswith(ROOT) else c for c in cmd[4 if cmd[:4] == STD else 3:])
            flags = "-I -S -B" if cmd[:4] == STD else "-I -B"
            out.append(f"{'OK  ' if good else 'FAIL'}  {name:42s} exit={r.returncode} expect={expect}  $ python3 {flags} {mask(shown)}")
            out += ["        " + mask(x) for x in tail + extra]
            print("\n".join(out[-(len(tail) + len(extra) + 1):]), flush=True)
    head = ["art-unity-font-01 verify_all", f"python {platform.python_version()} (steps with -S use no site-packages)",
            "Unity import, TextMeshPro / dynamic font rendering and devices are NOT_RUN by this script", ""]
    res = "VERIFY_ALL: " + ("PASS" if ok else "FAIL")
    os.makedirs(os.path.join(ROOT, "tests", "results"), exist_ok=True)
    with open(os.path.join(ROOT, "tests", "results", "verify_all.txt"), "w", encoding="utf-8") as f:
        f.write("\n".join(head + out + ["", res]) + "\n")
    print(res)
    return 0 if ok else 1


if __name__ == "__main__":
    sys.exit(main())
