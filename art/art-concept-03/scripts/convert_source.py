#!/usr/bin/env python3
"""Convert a legacy pixel-source (v1 = art-concept-01, v2 = art-concept-02) to the v3 layout. Standard library only.

Usage:  python3 -I scripts/convert_source.py LEGACY.json OUT.json [--verify-png-dir DIR]
  --verify-png-dir  render every converted sprite and compare it pixel-for-pixel with DIR/<name>.png (exit 1 on mismatch)

Differences handled:
  v1  top-level "palette" maps ONE global set of single characters to hex colours ("." = null); rows are full width;
      no per-sprite keys. -> v3 embeds the v1 colours as an embedded palette ramp "legacy_v1" and gives each sprite keys.
  v2  top-level "palette" is the whole master-v0.2 object (named ramps); sprites carry "keys" (char -> "ramp.i");
      rows are RIGHT-TRIMMED (missing characters = transparent). -> v3 pads every row to the cell width and keeps keys;
      the v0.2 palette is embedded so the file stays self-contained.
  v3  "schema"/"schemaVersion": 3, palette by file+sha256 or embedded, rows exactly cell.w, keys per sprite.
Converted files are review/legacy material, NOT handoff input: their status is "legacy-converted"."""
import argparse, json, os, sys

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE)
import pngio


def detect(d):
    if d.get("schemaVersion") == 3:
        return "v3"
    pal = d.get("palette")
    if isinstance(pal, dict) and "actor" in pal:
        return "v2"
    if isinstance(pal, dict) and all(len(k) == 1 for k in pal):
        return "v1"
    raise ValueError("unknown pixel-source layout")


def hex_rgba(h):
    h = h.lstrip("#")
    return (int(h[0:2], 16), int(h[2:4], 16), int(h[4:6], 16), int(h[6:8], 16) if len(h) == 8 else 255)


def convert(d):
    ver = detect(d)
    if ver == "v3":
        return d, ver
    sprites = []
    if ver == "v1":
        chars = [k for k, v in d["palette"].items() if v is not None]
        ramp = [d["palette"][k] for k in chars]
        idx = {k: i for i, k in enumerate(chars)}
        embedded = {"actor": {"legacy_v1": ramp}}
        for s in d["sprites"]:
            w, h = s["cell"]
            used = sorted({ch for fr in s["frames"] for row in fr for ch in row if ch != "."})
            frames = [[row.ljust(w, ".") for row in fr] for fr in s["frames"]]
            sprites.append({"name": s["name"], "cell": s["cell"], "pivot_px": s["pivot_px"], "fps": s.get("fps", 0),
                            "loop": s.get("loop", False), "alpha": "binary",
                            "keys": {ch: f"legacy_v1.{idx[ch]}" for ch in used}, "frames": frames,
                            "legacy": {k: s[k] for k in s if k not in ("frames", "cell", "pivot_px", "fps", "loop", "name")}})
        pal = {"embedded": embedded, "name": d.get("palette_name", "master-v0.1")}
    else:
        pal = {"embedded": {g: d["palette"][g] for g in ("actor", "ui", "terrain")}, "name": d["palette"].get("name", "master-v0.2")}
        for s in d["sprites"]:
            w, h = s["cell"]
            frames = [[row.ljust(w, ".") for row in (fr + [""] * h)[:h]] for fr in s["frames"]]
            sprites.append({"name": s["name"], "cell": s["cell"], "pivot_px": s["pivot_px"], "fps": s.get("fps", 0),
                            "loop": s.get("loop", False), "alpha": "blend" if s.get("alpha_ok") else "binary",
                            "keys": s["keys"], "frames": frames,
                            "legacy": {k: s[k] for k in s if k not in ("frames", "cell", "pivot_px", "fps", "loop", "name", "keys")}})
    return {"schema": "bible-survivors.pixel-source", "schemaVersion": 3, "status": "legacy-converted",
            "convertedFrom": ver, "palette": pal, "rules": {"transparent": ".", "rows": "exactly cell.w characters"},
            "sprites": sprites}, ver


def render(v3, s):
    cols = {}
    for g, ramps in v3["palette"]["embedded"].items():
        for r, lst in ramps.items():
            if isinstance(lst, list):
                for i, hx in enumerate(lst):
                    cols[f"{r}.{i}"] = hex_rgba(hx)
    w, h = s["cell"]
    n = len(s["frames"])
    buf = bytearray(w * n * h * 4)
    for k, fr in enumerate(s["frames"]):
        assert len(fr) == h and all(len(r) == w for r in fr), s["name"]
        for y, row in enumerate(fr):
            for x, ch in enumerate(row):
                if ch != ".":
                    j = (y * w * n + k * w + x) * 4
                    buf[j:j + 4] = bytes(cols[s["keys"][ch]])
    return w * n, h, bytes(buf)


def main(argv=None):
    ap = argparse.ArgumentParser()
    ap.add_argument("legacy"); ap.add_argument("out"); ap.add_argument("--verify-png-dir")
    a = ap.parse_args(argv)
    d = json.load(open(a.legacy, encoding="utf-8"))
    v3, ver = convert(d)
    json.dump(v3, open(a.out, "w", encoding="utf-8"), ensure_ascii=False, separators=(",", ":"))
    print(f"converted {ver} -> v3: {len(v3['sprites'])} sprites")
    if a.verify_png_dir:
        same = diff = missing = 0
        for s in v3["sprites"]:
            p = os.path.join(a.verify_png_dir, s["name"] + ".png")
            if not os.path.exists(p):
                missing += 1
                continue
            w, h, buf = render(v3, s)
            pw, ph, pb = pngio.read_rgba(p)
            pb = bytes(b if (i % 4 == 3 or pb[i - i % 4 + 3]) else 0 for i, b in enumerate(pb))  # RGB under alpha 0 ignored
            if (pw, ph) == (w, h) and pb == buf:
                same += 1
            else:
                diff += 1
                print(f"  differs: {s['name']}")
        print(f"verify: identical {same}, different {diff}, no PNG {missing}")
        return 0 if diff == 0 and same > 0 else 1
    return 0


if __name__ == "__main__":
    sys.exit(main())
