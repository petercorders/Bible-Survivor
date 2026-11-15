#!/usr/bin/env python3
"""Minimal hand-off check for candidate 1 (standard library only).

Usage:  python3 -I -B scripts/check_bible_r01.py [--art03 DIR] [--regen]
  ids        the two contract ids appear once each in the manifest, the pixel source and export/png (no extra, none missing)
  basics     PNG size = cell x frames; manifest sha / size / frames / pivot / fps / loop / quality / sorting equal file and contract
  source     the pixel source converts to exactly the PNG pixels
  alpha      only the contract's alpha values; binary book = 0/255; RGB 0 wherever alpha is 0; every colour in the palette
  footprint  each book frame's opaque box stays inside the current art 03 frame's box (shown size does not grow)
  refs       palette sha; art 03 manifest and both current PNGs unchanged (--art03, default ../art-concept-03)
  regen      (--regen) rebuild into a temp folder: pixel source, both PNGs and the manifest byte-identical
Exit 0 only if all pass. Unity import, Unity rendering and devices are NOT checked."""
import argparse, hashlib, json, os, shutil, subprocess, sys, tempfile

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.dirname(HERE)
sys.path.insert(0, HERE)
import pngio
import build_bible_r01 as B

FAIL = []


def bad(msg):
    FAIL.append(msg)
    print("FAIL", msg)


def main(argv=None):
    ap = argparse.ArgumentParser()
    ap.add_argument("--art03", default=os.path.join(os.path.dirname(ROOT), "art-concept-03"))
    ap.add_argument("--regen", action="store_true")
    a = ap.parse_args(argv)
    C = B.contract(ROOT)
    man = json.load(open(os.path.join(ROOT, *B.MANIFEST.split("/")), encoding="utf-8"))
    src = json.load(open(os.path.join(ROOT, *B.SOURCE.split("/")), encoding="utf-8"))
    pal = B.load_palette(ROOT)
    ids = [c["id"] for c in C["assets"]]
    for label, got in (("contract", ids), ("manifest", [m["id"] for m in man["assets"]]),
                       ("pixel source", [s["name"] for s in src["sprites"]]),
                       ("export/png", sorted(f[:-4] for f in os.listdir(os.path.join(ROOT, "export", "png")) if f.endswith(".png")))):
        if len(got) != len(set(got)) or sorted(got) != sorted(set(ids)):
            bad(f"{label} ids {sorted(got)} != {sorted(set(ids))}")
    M = {m["id"]: m for m in man["assets"]}
    S = {s["name"]: s for s in src["sprites"]}
    for c in C["assets"]:
        i = c["id"]
        path = os.path.join(ROOT, "export", "png", i + ".png")
        if i not in M or i not in S or not os.path.isfile(path):
            continue
        cw, ch = c["cell"]
        w, h, buf = pngio.read_rgba(path)
        if (w, h) != (cw * c["frames"], ch):
            bad(f"{i}: {w}x{h} != {cw}x{ch} x {c['frames']}")
            continue
        m = M[i]
        for k, v in (("sha256", B.sha(path)), ("size", [w, h]), ("frames", c["frames"]), ("fps", c["fps"]), ("loop", c["loop"]),
                     ("quality", c["quality"]), ("sorting", c["sorting"])):
            if m[k] != v:
                bad(f"{i}: manifest {k} {m[k]} != {v}")
        if m["pivot"]["px"] != c["pivot"]:
            bad(f"{i}: pivot {m['pivot']['px']} != {c['pivot']}")
        try:
            if B.sprite_bytes(S[i], pal) != (w, h, buf):
                bad(f"{i}: pixel source does not convert to the PNG")
        except ValueError as e:
            bad(f"{i}: {e}")
        cols = {tuple(buf[p:p + 4]) for p in range(0, len(buf), 4) if buf[p + 3]}
        alphas = {0} | {q[3] for q in cols}
        if alphas - set(c["alphaValues"]):
            bad(f"{i}: alpha {sorted(alphas - set(c['alphaValues']))} outside {c['alphaValues']}")
        if any(buf[p + 3] == 0 and buf[p:p + 3] != b"\0\0\0" for p in range(0, len(buf), 4)):
            bad(f"{i}: RGB not 0 under alpha 0")
        if cols - set(pal.values()):
            bad(f"{i}: colours outside the art 03 palette")
        if "footprint" in c:
            for k, (x0, y0, x1, y1) in enumerate(c["footprint"]["currentTopLeftBBox"]):
                bb = B.frame_bbox(buf, w, h, k * cw, cw)
                if bb is None or bb[0] < x0 or bb[1] < y0 or bb[2] > x1 or bb[3] > y1:
                    bad(f"{i} frame {k}: box {bb} grows past the current {[x0, y0, x1, y1]}")
        print(f"ok   {i}  {w}x{h}  frames {c['frames']}  pivot {c['pivot']}  alpha {sorted(alphas)}  colours {len(cols)}")
    if B.sha(os.path.join(ROOT, "src", "palette_v03.json")) != C["palette"]["sha256"]:
        bad("palette sha differs")
    if os.path.isdir(a.art03):
        r = C["references"]
        if B.sha(os.path.join(a.art03, *r["art03"]["manifest"].split("/"))) != r["art03"]["sha256"]:
            bad("art 03 manifest changed")
        for i, h_ in r["current"].items():
            if B.sha(os.path.join(a.art03, "export", "png", i + ".png")) != h_:
                bad(f"art 03 {i}.png changed")
        if B.sha(os.path.join(a.art03, "src", "palette_v03.json")) != C["palette"]["sha256"]:
            bad("art 03 palette changed")
        cur = {x["id"]: x for x in json.load(open(os.path.join(a.art03, "export", "manifest_v3.json"), encoding="utf-8"))["assets"]}
        for c in C["assets"]:
            o = cur[c["id"]]
            for k in ("cell", "frames", "fps", "loop", "sorting", "quality", "ref"):
                if o[k] != c[k]:
                    bad(f"{c['id']}: contract {k} {c[k]} != art 03 {o[k]}")
            if o["pivot"]["px"] != c["pivot"] or o["alpha"]["policy"] != c["alpha"]:
                bad(f"{c['id']}: contract pivot/alpha policy differs from art 03")
        print("ok   art 03 references unchanged, contract values equal the art 03 manifest")
    else:
        print("SKIP art 03 references (folder not found)")
    if a.regen:
        tmp = tempfile.mkdtemp(prefix="bible-regen-")
        try:
            rc = subprocess.run([sys.executable, "-I", "-S", "-B", os.path.join(HERE, "build_bible_r01.py"), "--out", tmp],
                                stdout=subprocess.DEVNULL).returncode
            files = [B.SOURCE, B.MANIFEST] + [f"export/png/{i}.png" for i in ids]
            diff = [f for f in files if not os.path.isfile(os.path.join(tmp, *f.split("/")))
                    or open(os.path.join(tmp, *f.split("/")), "rb").read() != open(os.path.join(ROOT, *f.split("/")), "rb").read()]
            if rc or diff:
                bad(f"regen exit {rc}, differs: {diff}")
            else:
                print(f"ok   regen byte-identical ({len(files)} files)")
        finally:
            shutil.rmtree(tmp)
    print(f"\n{'PASS' if not FAIL else 'FAIL'}  {len(FAIL)} problem(s)")
    return 0 if not FAIL else 1


if __name__ == "__main__":
    sys.exit(main())
