#!/usr/bin/env python3
"""Mutation check for the iteration-02 checker fixes (QA A03-01 / A03-02). Standard library only.

Usage:  python3 -I -B tests/checker_mutations_iter02.py [--old-checker PATH]
Copies scripts/, tests/, export/, src/, third_party/ (and the 02 legacy file the legacy_v2 case reads) into a temporary
folder, breaks ONE key line of scripts/check_art03.py there, runs tests/test_check_art03.py inside that copy and lists
the negative cases the broken checker let through. Every mutant must make the negative test fail (exit 1) and let at
least its named cases through; otherwise those cases do not really test that line.
--old-checker PATH also runs the same with a whole older checker file (the iteration-01 checker = the regression itself).
Writes  tests/results/checker_mutations_iter02.txt, or checker_mutations_iter02_with_iter01.txt when --old-checker is given
        (verify_all runs it without --old-checker, so the two files do not overwrite each other).
        Exit 0 only if every mutant is caught."""
import argparse, json, os, shutil, subprocess, sys, tempfile

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.dirname(HERE)
LEGACY = os.path.join(os.path.dirname(ROOT), "art-concept-02", "out", "pixel-source_art-b02.json")

# (name, exact text in check_art03.py, replacement, cases that must now be let through)
MUTANTS = [
    ("no top-level check (A03-01)", "    check_top(M, C, R)\n", "",
     ["top_ppu48", "top_pivotspace_topleft", "top_layerorder_reversed", "top_framelayout_vertical", "top_all_deleted"]),
    ("no per-layer layerOrder check (A03-01)", "            if type(e.get(\"layerOrder\")) is not int or e[\"layerOrder\"] != want_order:",
     "            if False:", ["asset_layerorder_swap"]),
    ("font file existence not checked (A03-02)", "        if os.path.islink(p) or not os.path.isfile(p):",
     "        if False:", ["font_file_missing", "font_license_missing", "font_dir_missing"]),
    ("no safe-path rule (A03-02)", "    if any(part in (\"\", \".\", \"..\") for part in path.split(\"/\")):\n        return False\n",
     "", ["font_extra_unsafe"]),
    ("no font check at all (A03-02)", "    check_fonts(data.get(\"ui\"), M, C, root, R)\n", "",
     ["font_ref_missing", "font_role_missing", "font_source_bad", "font_license_missing", "font_dir_missing",
      "manifest_font_notice_bad"]),
]


def run(name, checker_text, must_escape):
    with tempfile.TemporaryDirectory(prefix="art03-mut-") as t:
        pkg = os.path.join(t, "art-concept-03")
        for part in ("scripts", "tests", "export", "src", "third_party"):
            shutil.copytree(os.path.join(ROOT, part), os.path.join(pkg, part),
                            ignore=shutil.ignore_patterns("__pycache__", "results"))
        if os.path.exists(LEGACY):
            os.makedirs(os.path.join(t, "art-concept-02", "out"))
            shutil.copy(LEGACY, os.path.join(t, "art-concept-02", "out"))
        with open(os.path.join(pkg, "scripts", "check_art03.py"), "w", encoding="utf-8") as f:
            f.write(checker_text)
        r = subprocess.run([sys.executable, "-I", "-B", os.path.join(pkg, "tests", "test_check_art03.py")],
                           capture_output=True, text=True)
        res = json.load(open(os.path.join(pkg, "tests", "results", "check_negative_results.json"), encoding="utf-8"))
        bad = [x for x in res["results"] if x.get("pass") is False]
        escaped = [x["case"] for x in bad]
        how = {x["case"]: ("accepted" if x["exit"] == 0 else
                           "crashed" if any("Traceback" in o for o in x["output"]) else f"exit {x['exit']}, other code")
               for x in bad}
        positive = res["results"][0]["exit"]
    missing = [c for c in must_escape if c not in escaped]
    caught = r.returncode == 1 and not missing
    return {"mutant": name, "test_exit": r.returncode, "positive_exit": positive, "escaped": escaped, "how": how,
            "named_not_escaped": missing, "caught": caught}


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--old-checker")
    a = ap.parse_args()
    src = open(os.path.join(ROOT, "scripts", "check_art03.py"), encoding="utf-8").read()
    rows = []
    for name, old, new, must in MUTANTS:
        assert src.count(old) == 1, f"mutant '{name}': text not found exactly once"
        rows.append(run(name, src.replace(old, new), must))
        print(f"{'CAUGHT' if rows[-1]['caught'] else 'MISSED'}  {name}: not cleanly rejected {len(rows[-1]['escaped'])}", flush=True)
    if a.old_checker:
        old_txt = open(a.old_checker, encoding="utf-8").read()
        new_cases = [c for c in json.load(open(os.path.join(HERE, "results", "check_negative_results.json"),
                                               encoding="utf-8"))["results"][1:] if c["case"].startswith(("top_", "font_", "manifest_font_", "asset_layerorder"))]
        rows.append(run("iteration-01 checker file (whole regression)", old_txt, [c["case"] for c in new_cases]))
        print(f"{'CAUGHT' if rows[-1]['caught'] else 'MISSED'}  iteration-01 checker: not cleanly rejected {len(rows[-1]['escaped'])} "
              f"cases, named-but-rejected {rows[-1]['named_not_escaped']}", flush=True)
    ok = all(r["caught"] for r in rows)
    os.makedirs(os.path.join(HERE, "results"), exist_ok=True)
    out = "checker_mutations_iter02_with_iter01.txt" if a.old_checker else "checker_mutations_iter02.txt"
    with open(os.path.join(HERE, "results", out), "w", encoding="utf-8") as f:
        f.write("checker mutation check (iteration 02): each mutant breaks one key line of scripts/check_art03.py in a temp copy\n"
                "and runs tests/test_check_art03.py there. caught = negative test exit 1 AND every named case not cleanly rejected\n"
                "(accepted = checker exit 0; crashed = exit 1 with a Python traceback, which the negative test does not count as\n"
                "a rejection; other code = exit 1 without the expected [TOP]/[LAYER]/[FONT] line).\n\n")
        for r in rows:
            f.write(f"{'CAUGHT' if r['caught'] else 'MISSED'}  {r['mutant']}\n    negative test exit={r['test_exit']}  "
                    f"positive copy exit={r['positive_exit']}\n    not cleanly rejected ({len(r['escaped'])}): "
                    f"{', '.join(c + ' (' + r['how'][c] + ')' for c in r['escaped'])}\n")
            if r["named_not_escaped"]:
                f.write(f"    named but still rejected: {', '.join(r['named_not_escaped'])}\n")
        f.write(f"\nMUTATIONS: {'PASS' if ok else 'FAIL'}\n")
    print("MUTATIONS:", "PASS" if ok else "FAIL")
    return 0 if ok else 1


if __name__ == "__main__":
    sys.exit(main())
