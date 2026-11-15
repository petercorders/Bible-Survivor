#!/usr/bin/env python3
"""PRODUCER self-check of scripts/check_r02.py (standard library only).

NOT the QA error injection: docs/art/r02-design-task.md says QA injects errors independently and the producer's
results are not added to QA's. This only shows the checker is not blind to the categories the task names
(grid / sheet / manifest mismatch, missing font / reference, wrong coordinate system / layer / frame layout).
Usage:  python3 -I -B tests/selftest_r02.py [--catalog FILE]
Each case copies the package to a temp folder, breaks ONE thing (regenerating PNG + manifest from the pixel source
where the break is in the pixels, so only the intended rule fails) and runs the checker there.
A case counts only with exit 1, first line 'FAIL:', no traceback and the expected [CODE].
Writes  tests/results/selftest_r02.txt. Exit 0 if every case behaved."""
import argparse, json, os, shutil, subprocess, sys, tempfile

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.dirname(HERE)
PY = [sys.executable, "-I", "-S", "-B"]


def jedit(path, fn):
    d = json.load(open(path, encoding="utf-8"))
    fn(d)
    with open(path, "w", encoding="utf-8") as f:
        json.dump(d, f, ensure_ascii=False, indent=1)
        f.write("\n")


def regen(t):
    for script in ("convert_source.py", "make_manifest.py"):
        r = subprocess.run(PY + [os.path.join(t, "scripts", script), "--root", t], capture_output=True, text=True)
        assert r.returncode == 0, r.stdout + r.stderr


def src_edit(fn):
    def go(t):
        jedit(os.path.join(t, "export", "source", "pixel-source_r02.json"), fn)
        regen(t)
    return go


def man_edit(fn):
    return lambda t: jedit(os.path.join(t, "export", "manifest_r02.json"), fn)


def sprite(d, i):
    return d["sprites"][i]


def hold(d):
    sprite(d, 0)["frames"][1] = list(sprite(d, 0)["frames"][0])


def lift(d):
    f = sprite(d, 1)["frames"][2]
    sprite(d, 1)["frames"][2] = f[1:] + ["." * 32]


def to_edge(d):
    f = sprite(d, 0)["frames"][0]
    sprite(d, 0)["frames"][0] = [r[3:] + "..." for r in f]


def short_row(d):
    sprite(d, 2)["frames"][0][5] = sprite(d, 2)["frames"][0][5][:-1]


def unknown_key(d):
    r = sprite(d, 2)["frames"][3][20]
    i = next(k for k, c in enumerate(r) if c != ".")
    sprite(d, 2)["frames"][3][20] = r[:i] + "Z" + r[i + 1:]


def three_frames(d):
    sprite(d, 1)["frames"] = sprite(d, 1)["frames"][:3]


def asset(d, i):
    return d["assets"][i]


CASES = [  # name, expected code, mutation, extra checker args
    ("grid_hold_frame", "HOLD", src_edit(hold), []),
    ("grid_foot_lifted", "FOOT", src_edit(lift), []),
    ("grid_touches_edge", "EDGE", src_edit(to_edge), []),
    ("grid_row_short", "SOURCE", lambda t: jedit(os.path.join(t, "export", "source", "pixel-source_r02.json"), short_row), []),
    ("grid_unknown_key", "SOURCE", lambda t: jedit(os.path.join(t, "export", "source", "pixel-source_r02.json"), unknown_key), []),
    ("sheet_three_frames", "SIZE", src_edit(three_frames), []),
    ("manifest_rect_vertical", "RECT", man_edit(lambda d: asset(d, 0).__setitem__("frameRects", [[0, k * 32, 32, 32] for k in range(4)])), []),
    ("manifest_pivot_16_4", "PIVOT", man_edit(lambda d: asset(d, 1)["pivot"].__setitem__("px", [16, 4])), []),
    ("manifest_y_down_space", "SPACE", man_edit(lambda d: d.__setitem__("pivotSpace", "origin top-left, y down")), []),
    ("manifest_layer_fx", "LAYER", man_edit(lambda d: asset(d, 2).__setitem__("sorting", "FX")), []),
    ("manifest_extra_asset", "EXTRA", man_edit(lambda d: d["assets"].append(dict(d["assets"][0], id="enm_b4-ghost_move_32"))), []),
    ("png_changed_after_build", "HASH", lambda t: open(os.path.join(t, "export", "png", "enm_b2-rage-hound_run_32.png"), "ab").write(b"\0"), []),
    ("contract_stale", "STALE", lambda t: jedit(os.path.join(t, "src", "contract_r02.json"), lambda d: d.__setitem__("note", d["note"] + " edited")), []),
    ("review_font_missing", "FONT", lambda t: None, ["--font-root", "<empty>"]),
    ("art03_missing", "REF", lambda t: None, ["--art03", "<empty>"]),
    ("shadow_ref_wrong", "REF", man_edit(lambda d: asset(d, 0)["shadow"].__setitem__("id", "fx_shadow-snake_static_20x4")), []),
]


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--catalog")
    a = ap.parse_args()
    lines, ok_all = [], True
    for name, code, mutate, extra in CASES:
        with tempfile.TemporaryDirectory(prefix="r02-self-") as t, tempfile.TemporaryDirectory(prefix="r02-empty-") as empty:
            for part in ("src", "scripts", "export"):
                shutil.copytree(os.path.join(ROOT, part), os.path.join(t, part), ignore=shutil.ignore_patterns("__pycache__"))
            mutate(t)
            args = [x.replace("<empty>", empty) for x in extra]
            parent = os.path.dirname(ROOT)
            if "--art03" not in args:
                args += ["--art03", os.path.join(parent, "art-concept-03")]
            if "--font-root" not in args:
                args += ["--font-root", os.path.join(parent, "art-unity-font-01")]
            r = subprocess.run(PY + [os.path.join(t, "scripts", "check_r02.py"), "--root", t] + args, capture_output=True, text=True)
        out = (r.stdout + r.stderr).strip().splitlines()
        codes = sorted({x.split("]")[0] + "]" for x in out if x.startswith("[")})
        good = r.returncode == 1 and out and out[0].startswith("FAIL:") and not any("Traceback" in x for x in out) \
            and f"[{code}]" in codes
        ok_all &= good
        first = next((x for x in out if x.startswith(f"[{code}]")), out[1] if len(out) > 1 else "")
        lines.append(f"{'ok  ' if good else 'BAD '} {name:26s} exit={r.returncode} expect [{code}]  codes={','.join(codes)}  {first[:150]}")
        print(lines[-1], flush=True)
    # positive control on an untouched copy
    with tempfile.TemporaryDirectory(prefix="r02-self-") as t:
        for part in ("src", "scripts", "export"):
            shutil.copytree(os.path.join(ROOT, part), os.path.join(t, part), ignore=shutil.ignore_patterns("__pycache__"))
        parent = os.path.dirname(ROOT)
        cmd = PY + [os.path.join(t, "scripts", "check_r02.py"), "--root", t, "--art03", os.path.join(parent, "art-concept-03"),
                    "--font-root", os.path.join(parent, "art-unity-font-01")] + (["--catalog", a.catalog] if a.catalog else [])
        r = subprocess.run(cmd, capture_output=True, text=True)
    pos = r.returncode == 0 and r.stdout.startswith("PASS:")
    ok_all &= pos
    lines.insert(0, f"{'ok  ' if pos else 'BAD '} positive (untouched copy)  exit={r.returncode} expect 0  {r.stdout.strip()[:150]}")
    print(lines[0])
    os.makedirs(os.path.join(HERE, "results"), exist_ok=True)
    with open(os.path.join(HERE, "results", "selftest_r02.txt"), "w", encoding="utf-8") as f:
        f.write("PRODUCER self-check of scripts/check_r02.py - not QA's independent error injection; do not add to QA results.\n"
                "A case counts only with exit 1, first line FAIL:, no traceback and the expected [CODE].\n\n")
        f.write("\n".join(lines) + f"\n\nSELFTEST: {'PASS' if ok_all else 'FAIL'} ({len(CASES)} cases + positive)\n")
    print("SELFTEST:", "PASS" if ok_all else "FAIL")
    return 0 if ok_all else 1


if __name__ == "__main__":
    sys.exit(main())
