#!/usr/bin/env python3
"""PRODUCER mutation check for the R02-01 fixes (standard library only; not QA's result).

Builds checker variants that put the iteration-0 structure back, one fix at a time, and runs
tests/replay_qa_r02_01.py against each. Every variant must make the replay FAIL (exit 1) and flip its target inputs.
  M0 iteration-0 checker: iterations/r02-01-fix/before/check_r02.py (byte copy of the checker QA reviewed)
  M1 R02-01 back: the contract's own values are not checked against the support policy (check_support passes)
  M2 R02-02 back: declared files ignored, the fixed role path is hashed instead (iteration-0 behaviour)
  M3 R02-03 back: no type-first shape check and Python equality again (True == 1, 0 == False, 32.0 == 32)
  M4 R02-02 back: frameBBoxTopLeft not compared with the measured PNG bbox
Usage:  python3 -I -S -B tests/mutate_checker_r02_01.py [--catalog FILE] [--report FILE]
Exit 0 when every variant was caught."""
import argparse, os, re, subprocess, sys, tempfile

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.dirname(HERE)
STD = [sys.executable, "-I", "-S", "-B"]
CHECKER = os.path.join(ROOT, "scripts", "check_r02.py")
BEFORE = os.path.join(ROOT, "iterations", "r02-01-fix", "before", "check_r02.py")

MUTANTS = [  # name, what, [(anchor, replacement)], target inputs that must flip to BAD
    ("M0", "iteration-0 checker (byte copy)", None,
     ["layerOrder_unexpected", "delete_top_schema", "delete_top_schemaVersion", "delete_top_license",
      "delete_top_provenance", "type_ppu", "type_loop", "type_ground", "type_alpha", "type_shadow", "type_source",
      "false_bbox", "missing_pixel_ref", "missing_generator_ref", "missing_palette_ref", "coherent_bad_contract_ppu",
      "coherent_bad_contract_pivotSpace", "coherent_bad_contract_frameLayout", "coherent_bad_contract_flip"]),
    ("M1", "contract support policy not checked",
     [("def check_support(C, R):\n    n0 = len(R.errors)\n", "def check_support(C, R):\n    return True\n")],
     ["coherent_bad_contract_ppu", "coherent_bad_contract_pivotSpace", "coherent_bad_contract_frameLayout",
      "coherent_bad_contract_flip"]),
    ("M2", "declared file ignored, role path hashed",
     [('    f, s = dig(node, "file"), dig(node, "sha256")\n', '    f, s = role_path, dig(node, "sha256")\n')],
     ["missing_pixel_ref", "missing_generator_ref", "missing_palette_ref"]),
    ("M3", "no shape check, Python equality",
     [('def same(a, b):\n    """JSON equality that keeps JSON types apart: True != 1, 0 != false, 32.0 != 32."""\n',
       'def same(a, b):\n    return a == b\n'),
      ("def check_shape(R, code, doc, spec, name):\n", "def check_shape(R, code, doc, spec, name):\n    return True\n")],
     ["type_loop", "type_ground", "type_ppu", "layerOrder_unexpected"]),
    ("M4", "frame bbox not compared",
     [('    if not same(dig(e, "frameBBoxTopLeft"), bboxes):\n', "    if False:\n")],
     ["false_bbox"]),
]


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--catalog")
    ap.add_argument("--report", default=os.path.join(HERE, "results", "mutate_checker_r02_01.txt"))
    a = ap.parse_args()
    src = open(CHECKER, encoding="utf-8").read()
    lines, ok_all = ["R02-01 checker mutation check - PRODUCER evidence, not QA's result",
                     "each variant restores one iteration-0 structure; the replay of the 54 QA inputs must FAIL on it", ""], True
    with tempfile.TemporaryDirectory(prefix="r02-mut-") as tmp:
        for name, what, edits, targets in MUTANTS:
            if edits is None:
                text = open(BEFORE, encoding="utf-8").read()
            else:
                text = src
                for anchor, rep in edits:
                    n = text.count(anchor)
                    if n != 1:                         # read -> assert the anchor -> build the string -> write
                        raise SystemExit(f"{name}: anchor found {n} times, expected 1: {anchor[:60]!r}")
                    text = text.replace(anchor, rep)
            path = os.path.join(tmp, f"{name}_check_r02.py")
            with open(path, "w", encoding="utf-8") as f:
                f.write(text)
            rep_path = os.path.join(tmp, f"{name}_replay.txt")
            cmd = STD + [os.path.join(HERE, "replay_qa_r02_01.py"), "--checker", path, "--report", rep_path] \
                + (["--catalog", a.catalog] if a.catalog else [])
            r = subprocess.run(cmd, capture_output=True, text=True)
            rows = re.findall(r"^BAD +(\S+) +(exit=\S+ codes=\S+(?: TRACEBACK)?)", r.stdout, re.M)
            bad = sorted(n for n, _ in rows)
            seen = {n: d.replace("exit=", "").replace(" codes=", " ") for n, d in rows}
            missed = [t for t in targets if t not in bad]
            caught = r.returncode == 1 and not missed and "baseline" not in bad
            ok_all &= caught
            lines.append(f"{'caught ' if caught else 'MISSED '} {name} {what}: replay exit={r.returncode} (expect 1), "
                         f"{len(bad)} inputs BAD, targets {len(targets) - len(missed)}/{len(targets)} flipped"
                         + (f", NOT flipped: {missed}" if missed else ""))
            lines.append("        BAD (exit codes): " + ", ".join(f"{n} ({seen[n]})" for n in bad))
    lines += ["", "MUTATION: " + ("PASS (every variant caught)" if ok_all else "FAIL")]
    os.makedirs(os.path.dirname(os.path.abspath(a.report)), exist_ok=True)
    with open(a.report, "w", encoding="utf-8") as f:
        f.write("\n".join(lines) + "\n")
    print("\n".join(lines))
    return 0 if ok_all else 1


if __name__ == "__main__":
    sys.exit(main())
