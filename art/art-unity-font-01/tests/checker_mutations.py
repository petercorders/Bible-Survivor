#!/usr/bin/env python3
"""Mutation check for the font checker and build (standard library only).

Usage:  python3 -I -B tests/checker_mutations.py [--zip GALMURI_ZIP] [--bitmap-dir DIR]
Copies the package into a temporary folder, breaks ONE key line of scripts/check_fonts.py (or build_font_package.py)
there, runs tests/test_check_fonts.py inside that copy and lists the negative cases that are no longer cleanly
rejected. Each mutant must make the negative test fail (exit 1) and let its named cases through; a mutant whose named
case needs --zip / the bitmap fonts is reported as SKIPPED, not as caught.
Mutant 1 is the regression this package exists for: judging "has a glyf table" as "is an outline font".
Writes  tests/results/checker_mutations.txt. Exit 0 only if every mutant that ran was caught."""
import argparse, json, os, shutil, subprocess, sys, tempfile

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.dirname(HERE)

GLYF0 = ('        if glen == 0:\n'
         '            R.fail("GLYF", f"{role}: glyf table is 0 bytes - no outlines at all (a bitmap-only font is not an outline font)")\n')
EMPTY = '        empty = [c for c in PTS if c in cm and F.glyph_contours(cm[c]) == 0]\n'
MUTANTS = [  # name, file, [(old, new)], cases that must escape, needs
    ("glyf table presence only (old structure: no 0-byte glyf / empty glyph checks)", "check_fonts.py",
     [(GLYF0, ""), (EMPTY, "        empty = []\n")], ["empty_glyf_synthetic", "sample_glyph_empty"], None),
    ("no cmap check", "check_fonts.py", [("        if unmapped:\n", "        if False:\n")], ["cmap_symbol_missing"], None),
    ("no grid check", "check_fonts.py", [("        if off:\n", "        if False:\n")], ["grid_claim_6px"], None),
    ("no licence text check", "check_fonts.py", [('    for s in L["mustContain"]:\n', "    for s in []:\n")], ["license_text"], None),
    ("no Reserved Font Name check", "check_fonts.py",
     [('    if L["headerMustNotContain"].lower() in head:\n', "    if False:\n")], ["license_rfn"], None),
    ("no unknown-role check", "check_fonts.py", [("    if extra:\n", "    if False:\n")], ["role_extra"], None),
    ("no zip byte compare", "check_fonts.py",
     [('        if not os.path.isfile(p) or open(p, "rb").read() != data:\n', "        if False:\n")],
     ["zip_member_differs"], "zip"),
    ("build overwrites a differing file", "build_font_package.py",
     [('                if open(dst, "rb").read() != data:\n', "                if False:\n")],
     ["build_refuses_overwrite"], "zip"),
]


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--zip")
    ap.add_argument("--bitmap-dir", default=os.path.join(os.path.dirname(ROOT), "art-concept-03", "third_party", "galmuri"))
    a = ap.parse_args()
    rows = []
    for name, fname, edits, must, need in MUTANTS:
        if need == "zip" and not (a.zip and os.path.isfile(a.zip)):
            rows.append({"mutant": name, "state": "SKIPPED (needs --zip)"})
            print(f"SKIP    {name}", flush=True)
            continue
        with tempfile.TemporaryDirectory(prefix="font01-mut-") as t:
            for part in ("scripts", "tests", "src", "export", "fonts"):
                shutil.copytree(os.path.join(ROOT, part), os.path.join(t, part),
                                ignore=shutil.ignore_patterns("__pycache__", "results"))
            p = os.path.join(t, "scripts", fname)
            src = open(p, encoding="utf-8").read()
            for old, new in edits:
                assert src.count(old) == 1, f"mutant '{name}': text not found exactly once in {fname}"
                src = src.replace(old, new)
            with open(p, "w", encoding="utf-8") as f:
                f.write(src)
            cmd = [sys.executable, "-I", "-B", os.path.join(t, "tests", "test_check_fonts.py"), "--bitmap-dir", a.bitmap_dir]
            if a.zip:
                cmd += ["--zip", a.zip]
            r = subprocess.run(cmd, capture_output=True, text=True)
            res = json.load(open(os.path.join(t, "tests", "results", "check_font_negative_results.json"), encoding="utf-8"))
        bad = [x for x in res["results"] if x["pass"] is False]
        how = {x["case"]: "accepted" if x["exit"] == 0 else
               ("crashed" if any("Traceback" in o for o in x["output"]) else f"exit {x['exit']}, other code") for x in bad}
        missing = [c for c in must if c not in how]
        caught = r.returncode == 1 and not missing
        rows.append({"mutant": name, "state": "CAUGHT" if caught else "MISSED", "test_exit": r.returncode,
                     "not_cleanly_rejected": how, "named_but_still_rejected": missing})
        print(f"{rows[-1]['state']:7s} {name}: {', '.join(f'{k} ({v})' for k, v in how.items())}", flush=True)
    ok = all(r["state"] != "MISSED" for r in rows)
    with open(os.path.join(HERE, "results", "checker_mutations.txt"), "w", encoding="utf-8") as f:
        f.write("font checker mutation check: each mutant breaks key lines in a temp copy and runs tests/test_check_fonts.py there.\n"
                "caught = negative test exit 1 AND every named case not cleanly rejected (accepted = exit 0).\n\n")
        for r in rows:
            f.write(f"{r['state']}  {r['mutant']}\n")
            if "test_exit" in r:
                f.write(f"    negative test exit={r['test_exit']}; not cleanly rejected: "
                        f"{', '.join(f'{k} ({v})' for k, v in r['not_cleanly_rejected'].items()) or '-'}\n")
                if r["named_but_still_rejected"]:
                    f.write(f"    named but still rejected: {', '.join(r['named_but_still_rejected'])}\n")
        f.write(f"\nMUTATIONS: {'PASS' if ok else 'FAIL'}\n")
    print("MUTATIONS:", "PASS" if ok else "FAIL")
    return 0 if ok else 1


if __name__ == "__main__":
    sys.exit(main())
