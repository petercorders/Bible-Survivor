#!/usr/bin/env python3
"""PRODUCER replay of the R02-01 independent QA inputs against scripts/check_r02.py (standard library only).

NOT QA's result. The same QA re-injects on its own and the producer's numbers are never added to QA's
(docs/art/r02-design-task.md). This replays the 54 inputs QA described in art-readiness-reviews/r02-01-review.md
(mutation definitions follow QA's driver) and shows each now ends with exit 1, a 'FAIL:' first line, the expected
[CODE] and no traceback, while an untouched copy still passes.

Usage:  python3 -I -S -B tests/replay_qa_r02_01.py [--catalog FILE] [--art03 DIR] [--font-root DIR] [--checker FILE]
            [--old-checker FILE] [--qa-cases DIR] [--canonical] [--report FILE]
  default        rebuild the 54 inputs on fresh temp copies of THIS package (src/, scripts/, export/)
  --qa-cases DIR run on QA's own case folders instead, read-only (DIR holds art-genesis-r02/, cases/<name>/,
                 art-concept-03/, art-unity-font-01/); font_file_missing uses a temp font copy without the font
  --checker      checker under test (default scripts/check_r02.py; tests/mutate_checker_r02_01.py passes variants)
  --old-checker  also run this checker on every input and record what it did (e.g. the iteration-0 checker)
  --canonical    also QA's two canonical reproductions on temp copies of this package: contract ppu 16 / pivotSpace
                 'top-left' -> scripts/build_r02.py -> scripts/verify_all.py --review must exit 1 and the checker must
                 reject the rebuilt package with [SPACE]
  --report FILE  default tests/results/replay_qa_r02_01.txt
Pass rule  exit 1, first line 'FAIL:', every expected [CODE] present, no traceback; untouched copy exit 0 'PASS:'.
Exit 0 when every input behaved."""
import argparse, json, os, re, shutil, subprocess, sys, tempfile

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.dirname(HERE)
sys.path.insert(0, os.path.join(ROOT, "scripts"))
import pngio

STD = [sys.executable, "-I", "-S", "-B"]
NP = [sys.executable, "-I", "-B"]
FONT = "fonts/galmuri-outline/Galmuri11.ttf"

# What QA observed with the iteration-0 checker (r02-01-review.md table): exit, codes, traceback.
QA_SAW = """wrong_ppu 1 SPACE|wrong_pivotSpace 1 SPACE|wrong_frameLayout 1 SPACE|wrong_flip 1 SPACE|wrong_importer 1 SPACE
layerOrder_unexpected 0 -|asset_wrong_fps 1 VALUE|asset_wrong_frames 1 SIZE|asset_wrong_frameRects 1 RECT
asset_wrong_pivot 1 PIVOT|asset_wrong_ref 1 VALUE|asset_wrong_shadow 1 REF|asset_wrong_alpha 1 ALPHA
asset_wrong_sorting 1 LAYER|delete_top_ppu 1 SPACE|delete_top_pivotSpace 1 SPACE|delete_top_frameLayout 1 SPACE
delete_top_flip 1 SPACE|delete_top_schema 0 -|delete_top_schemaVersion 0 -|delete_top_license 0 -
delete_top_provenance 0 -|delete_top_assets 1 MANIFEST|delete_top_references 1 REF|delete_asset_file 1 MANIFEST
delete_asset_fps 1 MANIFEST,VALUE|delete_asset_frames 1 MANIFEST|delete_asset_frameRects 1 MANIFEST,RECT
delete_asset_pivot 1 MANIFEST|delete_asset_alpha 1 ALPHA,MANIFEST|delete_asset_shadow 1 MANIFEST,REF
delete_asset_source 1 MANIFEST,SOURCE|delete_asset_status 1 MANIFEST|type_ppu 0 -|type_fps 1 VALUE|type_loop 0 -
type_ground 0 -|type_alpha 1 TB|type_shadow 1 TB|type_source 1 TB|type_pivot 1 PIVOT|type_frames 1 SIZE
missing_png 1 FILE|unsafe_png 1 PATH|false_bbox 0 -|missing_pixel_ref 0 -|missing_generator_ref 0 -
missing_palette_ref 0 -|coherent_bad_contract_ppu 0 -|coherent_bad_contract_pivotSpace 0 -
coherent_bad_contract_frameLayout 0 -|coherent_bad_contract_flip 0 -|pixel_alpha128 1 ALPHA,COLOUR,HASH,SOURCE
font_file_missing 1 FONT"""
QA = {}
for item in QA_SAW.replace("\n", "|").split("|"):
    name, ex, codes = item.split()
    QA[name] = (int(ex), "" if codes in ("-", "TB") else codes, codes == "TB")


def write(path, doc):                                 # QA's writer: indent 2 + newline
    with open(path, "w", encoding="utf-8") as f:
        f.write(json.dumps(doc, ensure_ascii=False, indent=2) + "\n")


def contract_bad(k, v):
    def go(m, c, s, r):
        import hashlib
        c[k] = v
        write(os.path.join(r, "src", "contract_r02.json"), c)
        m[k] = v
        m["inputs"]["contract"]["sha256"] = hashlib.sha256(open(os.path.join(r, "src", "contract_r02.json"), "rb").read()).hexdigest()
    return go


def alpha128(m, c, s, r):                             # QA used Pillow; same pixel change with the stdlib PNG codec
    p = os.path.join(r, *m["assets"][0]["file"].split("/"))
    w, h, buf = pngio.read_rgba(p)
    buf = bytearray(buf)
    j = (15 * w + 10) * 4
    buf[j:j + 4] = bytes((255, 20, 20, 128))
    pngio.write_rgba(p, w, h, bytes(buf))


def a0(k, v):
    return lambda m, c, s, r: m["assets"][0].update({k: v})


def cases():
    L = []
    for k, v in [("ppu", 16), ("pivotSpace", "top-left y down"), ("frameLayout", "vertical"),
                 ("flip", {"artFaces": "left", "faceLeft": "none", "applyOn": "layer"}), ("importer", {"ppu": 16})]:
        L.append(("wrong_" + k, ["SPACE"], lambda m, c, s, r, k=k, v=v: m.update({k: v})))
    L.append(("layerOrder_unexpected", ["MANIFEST"], lambda m, c, s, r: m.update(layerOrder=["UI", "Actors"])))
    for k, v, code in [("fps", 9, "VALUE"), ("frames", 3, "SIZE"), ("frameRects", [[0, 0, 32, 32]], "RECT"),
                       ("pivot", {"px": [16, 4], "normalized": [.5, .125]}, "PIVOT"), ("ref", "monster:none", "VALUE"),
                       ("shadow", {"id": "missing"}, "REF"), ("alpha", {"policy": "blend", "values": [0, 128, 255]}, "ALPHA"),
                       ("sorting", "UI", "LAYER")]:
        L.append(("asset_wrong_" + k, [code], a0(k, v)))
    for k in ["ppu", "pivotSpace", "frameLayout", "flip", "schema", "schemaVersion", "license", "provenance", "assets",
              "references"]:
        code = {"ppu": "SPACE", "pivotSpace": "SPACE", "frameLayout": "SPACE", "flip": "SPACE", "references": "REF"}.get(k, "MANIFEST")
        L.append(("delete_top_" + k, [code], lambda m, c, s, r, k=k: m.pop(k, None)))
    for k in ["file", "fps", "frames", "frameRects", "pivot", "alpha", "shadow", "source", "status"]:
        L.append(("delete_asset_" + k, ["MANIFEST"], lambda m, c, s, r, k=k: m["assets"][0].pop(k, None)))
    for k, v, code in [("ppu", 32.0, "SPACE"), ("fps", "8", "VALUE"), ("loop", 1, "VALUE"), ("ground", 0, "VALUE"),
                       ("alpha", "binary", "ALPHA"), ("shadow", "missing", "REF"), ("source", "missing", "SOURCE"),
                       ("pivot", [16, 1], "PIVOT"), ("frames", "4", "SIZE")]:
        fn = (lambda m, c, s, r, v=v: m.update(ppu=v)) if k == "ppu" else a0(k, v)
        L.append(("type_" + k, ["MANIFEST", code], fn))
    L.append(("missing_png", ["FILE"], lambda m, c, s, r: os.unlink(os.path.join(r, *m["assets"][0]["file"].split("/")))))
    L.append(("unsafe_png", ["PATH"], a0("file", "../outside.png")))
    L.append(("false_bbox", ["VALUE"], a0("frameBBoxTopLeft", [[0, 0, 1, 1]] * 4)))
    L.append(("missing_pixel_ref", ["PATH", "FILE"], lambda m, c, s, r: m["data"]["pixelSource"].update(file="missing.json")))
    L.append(("missing_generator_ref", ["PATH", "FILE"], lambda m, c, s, r: m["inputs"]["generator"].update(file="missing.py")))
    L.append(("missing_palette_ref", ["PATH", "FILE"], lambda m, c, s, r: m["palette"]["ext"].update(file="missing.json")))
    for k, v in [("ppu", 16), ("pivotSpace", "top-left"), ("frameLayout", "vertical"),
                 ("flip", {"artFaces": "left", "faceLeft": "none", "applyOn": "layer"})]:
        L.append(("coherent_bad_contract_" + k, ["SPACE"], contract_bad(k, v)))
    L.append(("pixel_alpha128", ["ALPHA"], alpha128))
    L.append(("font_file_missing", ["FONT"], None))   # same package; the font root lacks the font
    assert len(L) == 54 and set(n for n, _, _ in L) == set(QA), "replay must cover QA's 54 inputs"
    return L


def run_checker(checker, root, art03, font_root, catalog):
    cmd = STD + [checker, "--root", root, "--art03", art03, "--font-root", font_root] + (["--catalog", catalog] if catalog else [])
    r = subprocess.run(cmd, capture_output=True, text=True)
    out = r.stdout + r.stderr
    lines = [x for x in r.stdout.splitlines() if x.strip()]
    return {"exit": r.returncode, "first": lines[0] if lines else "", "codes": sorted(set(re.findall(r"^\[([A-Z]+)\]", r.stdout, re.M))),
            "tb": "Traceback" in out, "out": out}


def detected(res, expect):
    return res["exit"] == 1 and res["first"].startswith("FAIL:") and not res["tb"] and set(expect) <= set(res["codes"])


def describe(res):
    return f"exit={res['exit']} codes={','.join(res['codes']) or '-'}{' TRACEBACK' if res['tb'] else ''}"


def copy_pkg(dst, full=False):
    ign = shutil.ignore_patterns("__pycache__", ".DS_Store", "iterations")
    if full:
        shutil.copytree(ROOT, dst, ignore=ign)
    else:
        for part in ("src", "scripts", "export"):
            shutil.copytree(os.path.join(ROOT, part), os.path.join(dst, part), ignore=ign)


def checker_dir(checker_file, tmp):
    """a scripts/ folder where checker_file runs as check_r02.py next to the package's pngio / convert_source"""
    d = os.path.join(tmp, "checker-" + str(len(os.listdir(tmp))))
    os.makedirs(d)
    for f in ("pngio.py", "convert_source.py"):
        shutil.copyfile(os.path.join(ROOT, "scripts", f), os.path.join(d, f))
    shutil.copyfile(checker_file, os.path.join(d, "check_r02.py"))
    return os.path.join(d, "check_r02.py")


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--catalog")
    ap.add_argument("--art03", default=os.path.join(os.path.dirname(ROOT), "art-concept-03"))
    ap.add_argument("--font-root", default=os.path.join(os.path.dirname(ROOT), "art-unity-font-01"))
    ap.add_argument("--checker", default=os.path.join(ROOT, "scripts", "check_r02.py"))
    ap.add_argument("--old-checker")
    ap.add_argument("--qa-cases")
    ap.add_argument("--canonical", action="store_true")
    ap.add_argument("--report", default=os.path.join(HERE, "results", "replay_qa_r02_01.txt"))
    a = ap.parse_args()
    rows, ok_all = [], True
    tally = {"new": [0, 0, 0], "old": [0, 0, 0]}       # explicit exit-1 detections, exit 0, traceback/crash
    fidelity = [0, 0]
    with tempfile.TemporaryDirectory(prefix="r02-replay-") as tmp:
        chk = checker_dir(a.checker, tmp)
        old = checker_dir(a.old_checker, tmp) if a.old_checker else None
        if a.qa_cases:
            art03, font_root = os.path.join(a.qa_cases, "art-concept-03"), os.path.join(a.qa_cases, "art-unity-font-01")
        else:
            art03, font_root = a.art03, a.font_root
        nofont = os.path.join(tmp, "font-without-ttf")
        shutil.copytree(font_root, nofont)
        os.unlink(os.path.join(nofont, *FONT.split("/")))

        def mask(s):
            for p, rep in ((tmp, "<tmp>"), (os.path.realpath(tmp), "<tmp>"), (a.qa_cases or "\0", "<qa>"),
                           (ROOT, "<pkg>"), (os.path.dirname(ROOT), ".."), (a.catalog or "\0", "<catalog>")):
                s = s.replace(p, rep)
            return s

        # untouched copy
        if a.qa_cases:
            base = os.path.join(a.qa_cases, "art-genesis-r02")
        else:
            base = os.path.join(tmp, "baseline")
            copy_pkg(base)
        res = run_checker(chk, base, art03, font_root, a.catalog)
        good = res["exit"] == 0 and res["first"].startswith("PASS:")
        ok_all &= good
        rows.append(f"{'ok  ' if good else 'BAD '} {'baseline (untouched copy)':34s} {describe(res)} expect exit 0 PASS")
        if not good:
            rows.extend("        " + mask(x) for x in res["out"].strip().splitlines()[:6])
        for name, expect, mutate in cases():
            fr = nofont if name == "font_file_missing" else font_root
            if a.qa_cases:
                r = os.path.join(a.qa_cases, "cases", name)
            else:
                r = os.path.join(tmp, "case-" + name)
                copy_pkg(r)
                mp = os.path.join(r, "export", "manifest_r02.json")
                m = json.load(open(mp, encoding="utf-8"))
                c = json.load(open(os.path.join(r, "src", "contract_r02.json"), encoding="utf-8"))
                s = json.load(open(os.path.join(r, "export", "source", "pixel-source_r02.json"), encoding="utf-8"))
                if mutate:
                    mutate(m, c, s, r)
                write(mp, m)
            res = run_checker(chk, r, art03, fr, a.catalog)
            good = detected(res, expect)
            ok_all &= good
            t = tally["new"]
            t[2 if res["tb"] or res["exit"] not in (0, 1) else 1 if res["exit"] == 0 else 0] += 1
            line = f"{'ok  ' if good else 'BAD '} {name:34s} {describe(res):44s} expect {','.join(expect)}"
            qa = QA[name]
            if old:
                ro = run_checker(old, r, art03, fr, a.catalog)
                to = tally["old"]
                to[2 if ro["tb"] else 1 if ro["exit"] == 0 else 0] += 1
                match = ro["exit"] == qa[0] and ro["tb"] == qa[2] and ",".join(ro["codes"]) == qa[1]
                fidelity[0 if match else 1] += 1
                line += f" | old {describe(ro)}{'' if match else ' (QA saw ' + str(qa) + ')'}"
            else:
                line += f" | QA saw exit={qa[0]} codes={qa[1] or '-'}{' TRACEBACK' if qa[2] else ''}"
            rows.append(line)
            if not good:
                rows.extend("        " + mask(x) for x in res["out"].strip().splitlines()[:8])
        canon = []
        if a.canonical:
            for k, v in (("ppu", 16), ("pivotSpace", "top-left")):
                pkg = os.path.join(tmp, f"canonical-{k}", "art-genesis-r02")
                copy_pkg(pkg, full=True)
                cp = os.path.join(pkg, "src", "contract_r02.json")
                c = json.load(open(cp, encoding="utf-8"))
                c[k] = v
                write(cp, c)
                b = subprocess.run(NP + [os.path.join(pkg, "scripts", "build_r02.py")], cwd=pkg, capture_output=True, text=True)
                vcmd = NP + [os.path.join(pkg, "scripts", "verify_all.py"), "--art03", art03, "--font-root", font_root, "--review"] \
                    + (["--catalog", a.catalog] if a.catalog else [])
                v_ = subprocess.run(vcmd, cwd=pkg, capture_output=True, text=True)
                step4 = next((x for x in v_.stdout.splitlines() if "4 stdlib contract checker" in x), "")
                res = run_checker(chk, pkg, art03, font_root, a.catalog)
                good = b.returncode == 0 and v_.returncode == 1 and step4.startswith("FAIL") and "VERIFY_ALL: FAIL" in v_.stdout \
                    and detected(res, ["SPACE"])
                ok_all &= good
                line = (f"{'ok  ' if good else 'BAD '} canonical contract {k}={json.dumps(v)}: build_r02 exit={b.returncode} "
                        f"verify_all --review exit={v_.returncode} (expect 1); checker {describe(res)} expect SPACE")
                if old:
                    ro = run_checker(old, pkg, art03, font_root, a.catalog)
                    line += f" | old checker on the same rebuilt package {describe(ro)} (QA saw exit=0 PASS)"
                canon.append(line)
                canon.append("        checker: " + " / ".join(mask(x) for x in res["out"].strip().splitlines()[:4]))
                canon.append("        verify_all: " + " / ".join(mask(x).strip() for x in v_.stdout.splitlines()
                                                          if x.startswith(("OK", "FAIL", "VERIFY_ALL", "SKIP"))))
        n = len(cases())
        head = ["R02-01 QA input replay - PRODUCER evidence, not QA's result; never add these numbers to QA's",
                f"inputs: {'QA case folders (read-only) <qa>' if a.qa_cases else 'rebuilt on temp copies of <pkg> from QA driver definitions'}",
                f"checker under test: {mask(a.checker)}" + (f"; old checker: {mask(a.old_checker)}" if old else ""),
                "pass rule: exit 1, first line FAIL:, expected [CODE] present, no traceback; untouched copy exit 0 PASS",
                ""]
        tn = tally["new"]
        hit = sum(1 for x in rows if x.startswith("ok  ") and "baseline" not in x)
        summ = [f"checker under test: {hit}/{n} inputs detected with the expected code; explicit exit1 {tn[0]} / exit0 "
                f"accepted {tn[1]} / traceback or crash {tn[2]}; baseline {'PASS' if rows[0].startswith('ok') else 'FAIL'}"]
        if old:
            to = tally["old"]
            summ.append(f"old checker on the same inputs: explicit exit1 {to[0]} / exit0 accepted {to[1]} / traceback {to[2]} "
                        f"(QA saw 35 / 16 / 3); same exit+codes+traceback as QA saw: {fidelity[0]}/{n}")
        lines = head + rows + ([""] + canon if canon else []) + [""] + summ + ["", "REPLAY: " + ("PASS" if ok_all else "FAIL")]
    os.makedirs(os.path.dirname(os.path.abspath(a.report)), exist_ok=True)
    with open(a.report, "w", encoding="utf-8") as f:
        f.write("\n".join(lines) + "\n")
    print("\n".join(lines))
    return 0 if ok_all else 1


if __name__ == "__main__":
    sys.exit(main())
