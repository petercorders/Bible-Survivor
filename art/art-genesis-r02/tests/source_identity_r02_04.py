#!/usr/bin/env python3
"""PRODUCER regression for R02-04: pixel source sprite identity (standard library only). Not QA's result.

R02-02 QA found that run() put the pixel-source sprites into a name map where the last one wins, so an earlier sprite
with the same name was never checked. QA's input: a copy of B1 inserted BEFORE the real B1 with frame 0 row 0 cut to
31 characters, manifest data.pixelSource.sha256 updated -> check_r02.py exit 0 PASS while convert_source.py exit 1.
Every case below runs on a temp copy of src/ scripts/ export/ with data.pixelSource.sha256 updated (so [HASH] never
hides the identity problem):
  dup_first_shortrow     QA's input: B1 copy first, its frame 0 row 0 = 31 characters   expect 1 [SOURCE]; converter 1
  dup_first_transparent  QA's first input: B1 copy first, its frame 0 all transparent    expect 1 [SOURCE]
  dup_last_shortrow      order reversed: the short-row copy right AFTER the good B1      expect 1 [SOURCE]
  dup_identical          a byte-identical second B1 (a "valid" duplicate)                expect 1 [SOURCE]
  missing_b3             B3 removed                                                      expect 1 [SOURCE]
  extra_b4               a fourth sprite whose id is not in the contract                 expect 1 [SOURCE]
  name_not_string        B2's name is the number 2                                       expect 1 [SOURCE]
  sprite_not_object      a string (B1's id) inside the sprite list                       expect 1 [SOURCE]
  reordered_valid        the three sprites in another order, each once                   expect 0 PASS (order is not identity)
Pass rule  expect-1 cases: exit 1, first line FAIL:, [SOURCE] present, no traceback (a traceback or exit 3 never counts);
           expect-0: exit 0 PASS:. With --old-checker the old checker must also accept dup_first_shortrow (exit 0),
           i.e. the regression really reproduces on the old structure.
Usage:  python3 -I -S -B tests/source_identity_r02_04.py [--catalog FILE] [--art03 DIR] [--font-root DIR]
            [--checker FILE] [--old-checker FILE] [--mutants] [--report FILE]
  --mutants  also run the suite on M0 = the --old-checker byte copy and M1 = the current checker with the identity
             check taken out (all names go into the map again); each must make the suite FAIL with the duplicate cases
             accepted (exit 0)
Exit 0 when everything behaved."""
import argparse, ast, copy, hashlib, json, os, subprocess, sys, tempfile

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.dirname(HERE)
sys.path.insert(0, HERE)
import replay_qa_r02_01 as RP                         # copy_pkg / checker_dir / run_checker / describe

SRC = "export/source/pixel-source_r02.json"
MAN = "export/manifest_r02.json"
B1, B2, B3 = "enm_b1-envy-eye_move_32", "enm_b2-rage-hound_run_32", "enm_b3-cracked-mask_move_32"
# M1 goes right AFTER the closing line of source_identity's docstring (a line put after the docstring's first line lands
# inside the string and changes nothing - that first attempt left M1 identical to the fix; m1_landed() now guards it)
M1_ANCHOR = '    Returns the names that occur exactly once; only those go into the map. Order is not identity."""\n'
M1_REPLACE = M1_ANCHOR + '    return {dig(sp, "name") for sp in sprites if type(dig(sp, "name")) is str}\n'


def m1_landed(src, mutant):
    """True when the mutant's source_identity keeps the same docstring and really starts with the inserted return."""
    def fn(text):
        return next(n for n in ast.walk(ast.parse(text)) if isinstance(n, ast.FunctionDef) and n.name == "source_identity")
    a, b = fn(src), fn(mutant)
    return ast.get_docstring(a) == ast.get_docstring(b) and isinstance(b.body[1], ast.Return) \
        and not isinstance(a.body[1], ast.Return)


def by(s, name):
    return next(i for i, sp in enumerate(s["sprites"]) if sp["name"] == name)


def short_copy(s):
    bad = copy.deepcopy(s["sprites"][by(s, B1)])
    bad["frames"][0][0] = "." * 31                    # QA's exact change
    return bad


def blank_copy(s):
    bad = copy.deepcopy(s["sprites"][by(s, B1)])
    bad["frames"][0] = ["." * 32] * 32
    return bad


def extra(s):
    x = copy.deepcopy(s["sprites"][by(s, B1)])
    x["name"] = "enm_b4-ghost_move_32"
    return x


CASES = [  # name, expected exit, mutation of the pixel source
    ("dup_first_shortrow", 1, lambda s: s["sprites"].insert(by(s, B1), short_copy(s))),
    ("dup_first_transparent", 1, lambda s: s["sprites"].insert(by(s, B1), blank_copy(s))),
    ("dup_last_shortrow", 1, lambda s: s["sprites"].insert(by(s, B1) + 1, short_copy(s))),
    ("dup_identical", 1, lambda s: s["sprites"].insert(by(s, B1) + 1, copy.deepcopy(s["sprites"][by(s, B1)]))),
    ("missing_b3", 1, lambda s: s["sprites"].pop(by(s, B3))),
    ("extra_b4", 1, lambda s: s["sprites"].append(extra(s))),
    ("name_not_string", 1, lambda s: s["sprites"][by(s, B2)].__setitem__("name", 2)),
    ("sprite_not_object", 1, lambda s: s["sprites"].insert(0, B1)),
    ("reordered_valid", 0, lambda s: s.__setitem__("sprites", [s["sprites"][by(s, n)] for n in (B3, B1, B2)])),
]
DUPLICATES = ["dup_first_shortrow", "dup_first_transparent", "dup_identical"]   # what last-write-wins hides


def make_case(r, mutate):
    RP.copy_pkg(r)
    p = os.path.join(r, *SRC.split("/"))
    s = json.load(open(p, encoding="utf-8"))
    mutate(s)
    with open(p, "w", encoding="utf-8") as f:
        f.write(json.dumps(s, ensure_ascii=False, indent=1) + "\n")
    mp = os.path.join(r, *MAN.split("/"))
    m = json.load(open(mp, encoding="utf-8"))
    m["data"]["pixelSource"]["sha256"] = hashlib.sha256(open(p, "rb").read()).hexdigest()
    with open(mp, "w", encoding="utf-8") as f:
        f.write(json.dumps(m, ensure_ascii=False, indent=1) + "\n")


def good(res, expect):
    if expect == 0:
        return res["exit"] == 0 and res["first"].startswith("PASS:")
    return RP.detected(res, ["SOURCE"])


def suite(tmp, checker, a, old=None, label=""):
    """-> (rows, ok, results by case)"""
    rows, ok, results = [], True, {}
    for name, expect, mutate in CASES:
        r = os.path.join(tmp, f"{label}case-{name}")
        make_case(r, mutate)
        res = RP.run_checker(checker, r, a.art03, a.font_root, a.catalog)
        g = good(res, expect)
        ok &= g
        results[name] = res
        line = f"{'ok  ' if g else 'BAD '} {name:22s} {RP.describe(res):34s} expect {'exit 0 PASS' if expect == 0 else 'exit 1 [SOURCE]'}"
        if old:
            ro = RP.run_checker(old, r, a.art03, a.font_root, a.catalog)
            line += f" | old {RP.describe(ro)}"
            if name == "dup_first_shortrow":
                rep = ro["exit"] == 0 and ro["first"].startswith("PASS:")
                ok &= rep
                line += " (old accepts it: regression reproduced)" if rep else " (OLD DID NOT ACCEPT IT: regression not reproduced)"
        if name == "dup_first_shortrow":
            out = os.path.join(tmp, f"{label}convert-out")
            cv = subprocess.run(RP.STD + [os.path.join(ROOT, "scripts", "convert_source.py"), "--root", r, "--out", out],
                                capture_output=True, text=True)
            err = [x for x in cv.stderr.strip().splitlines() if x.strip()]
            cg = cv.returncode == 1 and any("31 characters != cell width 32" in x for x in err)
            ok &= cg
            line += f" | converter exit={cv.returncode} {err[-1][:90] if err else ''}"
        rows.append(line)
        if not g:
            rows.extend("        " + x for x in res["out"].strip().splitlines()[:6])
    return rows, ok, results


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--catalog")
    ap.add_argument("--art03", default=os.path.join(os.path.dirname(ROOT), "art-concept-03"))
    ap.add_argument("--font-root", default=os.path.join(os.path.dirname(ROOT), "art-unity-font-01"))
    ap.add_argument("--checker", default=os.path.join(ROOT, "scripts", "check_r02.py"))
    ap.add_argument("--old-checker")
    ap.add_argument("--mutants", action="store_true")
    ap.add_argument("--report", default=os.path.join(HERE, "results", "source_identity_r02_04.txt"))
    a = ap.parse_args()
    if a.mutants and not a.old_checker:
        ap.error("--mutants needs --old-checker (M0)")
    with tempfile.TemporaryDirectory(prefix="r02-04-") as tmp:
        chk = RP.checker_dir(a.checker, tmp)
        old = RP.checker_dir(a.old_checker, tmp) if a.old_checker else None
        base = os.path.join(tmp, "baseline")
        RP.copy_pkg(base)
        rb = RP.run_checker(chk, base, a.art03, a.font_root, a.catalog)
        ok = rb["exit"] == 0 and rb["first"].startswith("PASS:")
        rows = [f"{'ok  ' if ok else 'BAD '} {'baseline (untouched copy)':22s} {RP.describe(rb):34s} expect exit 0 PASS"]
        r2, ok2, _ = suite(tmp, chk, a, old)
        rows += r2
        ok &= ok2
        mut = []
        if a.mutants:
            src = open(a.checker, encoding="utf-8").read()
            if src.count(M1_ANCHOR) != 1:              # read -> assert the anchor -> build -> write
                raise SystemExit(f"M1 anchor found {src.count(M1_ANCHOR)} times, expected 1")
            m1src = src.replace(M1_ANCHOR, M1_REPLACE)
            if not m1_landed(src, m1src):
                raise SystemExit("M1 did not land as code (docstring changed or no return inserted)")
            m1 = os.path.join(tmp, "M1_check_r02.py")
            with open(m1, "w", encoding="utf-8") as f:
                f.write(m1src)
            for mname, what, path in (("M0", "R02-02 checker byte copy (before this fix)", a.old_checker),
                                      ("M1", "identity check taken out, every name goes into the map again", m1)):
                _, mok, res = suite(tmp, RP.checker_dir(path, tmp), a, label=mname + "-")
                accepted = [n for n in DUPLICATES if res[n]["exit"] == 0]
                caught = not mok and accepted == DUPLICATES
                ok &= caught
                mut.append(f"{'caught ' if caught else 'MISSED '} {mname} {what}: suite {'FAIL' if not mok else 'PASS'} "
                           f"(expect FAIL); duplicate cases accepted with exit 0: {accepted} (expect {DUPLICATES})")
                mut.append("        " + "; ".join(f"{n} {RP.describe(res[n])}" for n, _, _ in CASES))
    head = ["R02-04 pixel source identity - PRODUCER evidence, not QA's result; never add these numbers to QA's",
            f"checker under test: {os.path.relpath(a.checker, ROOT)}" + (f"; old checker: {os.path.relpath(a.old_checker, ROOT)}" if old else ""),
            "pass rule: expect-1 = exit 1, FAIL:, [SOURCE], no traceback; expect-0 = exit 0 PASS:", ""]
    lines = head + rows + ([""] + mut if mut else []) + ["", "SOURCE_IDENTITY: " + ("PASS" if ok else "FAIL")]
    os.makedirs(os.path.dirname(os.path.abspath(a.report)), exist_ok=True)
    with open(a.report, "w", encoding="utf-8") as f:
        f.write("\n".join(lines) + "\n")
    print("\n".join(lines))
    return 0 if ok else 1


if __name__ == "__main__":
    sys.exit(main())
