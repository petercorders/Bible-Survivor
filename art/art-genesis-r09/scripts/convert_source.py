#!/usr/bin/env python3
"""Pixel source -> game PNG sheets for the R09 sidecar (standard library only).

Usage:  python3 -I -B scripts/convert_source.py [--root DIR] [--source FILE] [--out DIR]
Reads   --source (default <root>/export/source/pixel-source_r09.json) and the two palettes (src/palette_v03.json + src/palette_r09_ext.json)
Writes  <out>/export/png/<sprite name>.png: 8-bit RGBA, frames side by side (frame i at x = i*cell.w), RGB 0 where alpha is 0.
        Alpha comes from the palette entry (255 for actor colours; the shadow.0 entry carries its own alpha).
        Same pixels -> same bytes (scripts/pngio.py: filter 0, zlib level 9).
This is the conversion path from the editable grid source to the game PNG; the Claude Design canvas is NOT an input.
Also holds all_assets(): the one list of produced assets (src/contract_r09.json)."""
import argparse, json, os, sys

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.dirname(HERE)
sys.path.insert(0, HERE)
import pngio

SOURCE = "export/source/pixel-source_r09.json"


def hex_rgba(h):
    h = h.lstrip("#")
    return (int(h[0:2], 16), int(h[2:4], 16), int(h[4:6], 16), int(h[6:8], 16) if len(h) == 8 else 255)


def load_palette(root):
    """name 'ramp.i' -> rgba, from the base palette groups actor/ui/terrain and the ext palette actor group."""
    base = json.load(open(os.path.join(root, "src", "palette_v03.json"), encoding="utf-8"))
    ext = json.load(open(os.path.join(root, "src", "palette_r09_ext.json"), encoding="utf-8"))
    pal = {}
    for group in ("actor", "ui", "terrain"):
        for ramp, cols in base[group].items():
            for i, c in enumerate(cols):
                pal[f"{ramp}.{i}"] = hex_rgba(c)
    for ramp, cols in ext["actor"].items():
        if f"{ramp}.0" in pal:
            raise ValueError(f"ext ramp '{ramp}' collides with the base palette")
        for i, c in enumerate(cols):
            pal[f"{ramp}.{i}"] = hex_rgba(c)
    return pal


def all_assets(root):
    """-> list of (contract file, asset dict) for every produced PNG, from the actor contract."""
    out = []
    for rel in ("src/contract_r09.json",):
        doc = json.load(open(os.path.join(root, *rel.split("/")), encoding="utf-8"))
        for a in doc["assets"]:
            out.append((rel, a))
    return out


def sprite_bytes(sp, pal):
    """-> (width, height, RGBA bytes) for one sprite of the pixel source; raises ValueError on a malformed grid."""
    cw, ch = sp["cell"]
    frames = sp["frames"]
    w, h = cw * len(frames), ch
    buf = bytearray(w * h * 4)
    for k, rows in enumerate(frames):
        if len(rows) != ch:
            raise ValueError(f"{sp['name']} frame {k}: {len(rows)} rows != cell height {ch}")
        for y, row in enumerate(rows):
            if len(row) != cw:
                raise ValueError(f"{sp['name']} frame {k} row {y}: {len(row)} characters != cell width {cw}")
            for x, c in enumerate(row):
                if c == ".":
                    continue
                if c not in sp["keys"]:
                    raise ValueError(f"{sp['name']} frame {k} row {y} col {x}: key {c!r} not in keys")
                name = sp["keys"][c]
                if name not in pal:
                    raise ValueError(f"{sp['name']}: key {c!r} -> '{name}' is not a palette entry")
                j = (y * w + k * cw + x) * 4
                buf[j:j + 4] = bytes(pal[name])
    return w, h, bytes(buf)


def main(argv=None):
    ap = argparse.ArgumentParser()
    ap.add_argument("--root", default=ROOT)
    ap.add_argument("--source")
    ap.add_argument("--out")
    a = ap.parse_args(argv)
    out = a.out or a.root
    pal = load_palette(a.root)
    src = json.load(open(a.source or os.path.join(a.root, *SOURCE.split("/")), encoding="utf-8"))
    os.makedirs(os.path.join(out, "export", "png"), exist_ok=True)
    for sp in src["sprites"]:
        w, h, data = sprite_bytes(sp, pal)
        pngio.write_rgba(os.path.join(out, "export", "png", sp["name"] + ".png"), w, h, data)
        print(f"png  {sp['name']}.png  {w}x{h}")
    return 0


if __name__ == "__main__":
    sys.exit(main())
