#!/usr/bin/env python3
"""Build candidate 1: art -> pixel source JSON -> 2 PNGs -> manifest (standard library only, deterministic).

Usage:  python3 -I -B scripts/build_bible_r01.py [--out DIR] [--from-source]
  default        scripts/bible_r01.py writes <out>/export/source/pixel-source_bible_r01.json, then the PNGs and manifest
  --from-source  keep the existing pixel source (after hand edits) and rebuild only the PNGs and manifest from it
Writes  <out>/export/png/<id>.png (8-bit RGBA, frames side by side, RGB 0 where alpha is 0; pngio: filter 0, zlib 9)
        <out>/export/manifest_bible_r01.json (sizes, alpha values, colours, boxes and hashes measured from the PNGs)
The Claude Design canvas is NOT an input."""
import argparse, hashlib, json, os, sys

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.dirname(HERE)
sys.path.insert(0, HERE)
import pngio

SOURCE = "export/source/pixel-source_bible_r01.json"
MANIFEST = "export/manifest_bible_r01.json"


def sha(path):
    return hashlib.sha256(open(path, "rb").read()).hexdigest()


def hex_rgba(h):
    h = h.lstrip("#")
    return (int(h[0:2], 16), int(h[2:4], 16), int(h[4:6], 16), int(h[6:8], 16) if len(h) == 8 else 255)


def load_palette(root):
    base = json.load(open(os.path.join(root, "src", "palette_v03.json"), encoding="utf-8"))
    return {f"{r}.{i}": hex_rgba(c) for g in ("actor", "ui", "terrain") for r, cs in base[g].items() for i, c in enumerate(cs)}


def contract(root):
    return json.load(open(os.path.join(root, "src", "contract_bible_r01.json"), encoding="utf-8"))


def sprite_bytes(sp, pal):
    """-> (w, h, RGBA bytes) of one pixel-source sprite; ValueError on a malformed grid."""
    cw, ch = sp["cell"]
    frames = sp["frames"]
    w, h = cw * len(frames), ch
    buf = bytearray(w * h * 4)
    for k, rows in enumerate(frames):
        if len(rows) != ch or any(len(r) != cw for r in rows):
            raise ValueError(f"{sp['name']} frame {k}: grid is not {cw}x{ch}")
        for y, row in enumerate(rows):
            for x, c in enumerate(row):
                if c == ".":
                    continue
                if c not in sp["keys"] or sp["keys"][c] not in pal:
                    raise ValueError(f"{sp['name']} frame {k} ({x},{y}): key {c!r} is not a palette entry")
                j = (y * w + k * cw + x) * 4
                buf[j:j + 4] = bytes(pal[sp["keys"][c]])
    return w, h, bytes(buf)


def write_source(out):
    import bible_r01 as art
    C = contract(ROOT)
    spr = art.sprites()
    src = {"schema": "bible-survivors.pixel-source", "schemaVersion": 3, "status": "candidate-1",
           "palette": {"base": "src/palette_v03.json (master-v0.3)"},
           "rules": {"transparent": ".", "rows": "every row has exactly cell.w characters",
                     "keys": "per sprite: character -> art 03 palette entry 'ramp.index'",
                     "edit": "hand edits go here, then build_bible_r01.py --from-source; a plain build regenerates this file from scripts/bible_r01.py"},
           "sprites": []}
    for c in C["assets"]:
        keys, frames = spr[c["id"]]
        used = sorted({ch for fr in frames for row in fr for ch in row if ch != "."})
        src["sprites"].append({"name": c["id"], "authoring": f"scripts/bible_r01.py:{'glow' if c['id'].startswith('fx_') else 'BOOK'}",
                               "cell": c["cell"], "pivot_px": c["pivot"], "fps": c["fps"], "loop": c["loop"], "alpha": c["alpha"],
                               "keys": {k: keys[k] for k in used}, "frames": frames})
    dst = os.path.join(out, *SOURCE.split("/"))
    os.makedirs(os.path.dirname(dst), exist_ok=True)
    with open(dst, "w", encoding="utf-8") as f:
        json.dump(src, f, ensure_ascii=False, indent=1)
        f.write("\n")
    print(f"source -> {SOURCE}")


def frame_bbox(buf, w, h, x0, cw):
    pts = [(x - x0, y) for y in range(h) for x in range(x0, x0 + cw) if buf[(y * w + x) * 4 + 3]]
    return [min(p[0] for p in pts), min(p[1] for p in pts), max(p[0] for p in pts), max(p[1] for p in pts)] if pts else None


def write_pngs_and_manifest(out):
    C = contract(ROOT)
    pal = load_palette(ROOT)
    src = json.load(open(os.path.join(out, *SOURCE.split("/")), encoding="utf-8"))
    S = {s["name"]: s for s in src["sprites"]}
    os.makedirs(os.path.join(out, "export", "png"), exist_ok=True)
    assets = []
    for c in C["assets"]:
        sp = S[c["id"]]
        w, h, data = sprite_bytes(sp, pal)
        rel = f"export/png/{c['id']}.png"
        pngio.write_rgba(os.path.join(out, *rel.split("/")), w, h, data)
        print(f"png  {c['id']}.png  {w}x{h}")
        w2, h2, buf = pngio.read_rgba(os.path.join(out, *rel.split("/")))
        cw, chh = c["cell"]
        n = w2 // cw
        assets.append({
            "id": c["id"], "file": rel, "sha256": sha(os.path.join(out, *rel.split("/"))), "size": [w2, h2],
            "cell": [cw, chh], "frames": n, "frameRects": [[k * cw, 0, cw, chh] for k in range(n)],
            "fps": c["fps"], "loop": c["loop"],
            "pivot": {"px": c["pivot"], "normalized": [c["pivot"][0] / cw, c["pivot"][1] / chh]},
            "ground": False, "alpha": {"policy": c["alpha"], "values": sorted({buf[i] for i in range(3, len(buf), 4)})},
            "sorting": c["sorting"], "quality": c["quality"], "ref": c["ref"],
            "colours": len({bytes(buf[i:i + 4]) for i in range(0, len(buf), 4) if buf[i + 3]}),
            "frameBBoxTopLeft": [frame_bbox(buf, w2, h2, k * cw, cw) for k in range(n)],
            "replaces": {"art03": c["id"], "currentSha256": C["references"]["current"][c["id"]]},
            "source": {"authoring": sp["authoring"], "pixelSource": f"{SOURCE}#{c['id']}",
                       "method": "written by Claude in this session (hand grid for the book, procedural for the glow), art 03 palette only"},
            "status": "candidate-1"})
    man = {"schema": "bible-survivors.art-manifest-bible-r01", "schemaVersion": 1, "status": "candidate-1",
           "statusNote": "visual sidecar candidate after play feedback: independent art review pending; not imported into Unity, not checked on devices",
           "ppu": C["ppu"], "pivotSpace": C["pivotSpace"], "frameLayout": C["frameLayout"], "flip": C["flip"],
           "palette": {"file": "src/palette_v03.json", "name": "master-v0.3", "sha256": sha(os.path.join(ROOT, "src", "palette_v03.json"))},
           "inputs": {"contract": {"file": "src/contract_bible_r01.json", "sha256": sha(os.path.join(ROOT, "src", "contract_bible_r01.json"))},
                      "art": {"file": "scripts/bible_r01.py", "sha256": sha(os.path.join(ROOT, "scripts", "bible_r01.py"))},
                      "helpers": {"scripts/pngio.py": sha(os.path.join(ROOT, "scripts", "pngio.py"))}},
           "data": {"pixelSource": {"file": SOURCE, "sha256": sha(os.path.join(out, *SOURCE.split("/")))}},
           "references": C["references"],
           "build": {"command": "python3 -I -B scripts/build_bible_r01.py", "testedWith": {"python": "3.11.6"}},
           "provenance": {"pixels": "scripts/bible_r01.py written by Claude (Anthropic model) in this session; not exported from the Claude Design product",
                          "designCanvas": "Claude Design canvas used only to present the boards for review; see RESULT.md"},
           "license": {"status": "original", "references": [],
                       "art": "made for Bible Survivors from the art 03 master palette (unchanged) by this folder's script; the book grid is a redraw of the art 03 book's idea (cover, spine, pages, ribbon). No third-party images, traces or reference art; no lettering."},
           "assets": assets}
    dst = os.path.join(out, *MANIFEST.split("/"))
    with open(dst, "w", encoding="utf-8") as f:
        json.dump(man, f, ensure_ascii=False, indent=1)
        f.write("\n")
    print(f"manifest -> {MANIFEST} ({len(assets)} assets)")


def main(argv=None):
    ap = argparse.ArgumentParser()
    ap.add_argument("--out")
    ap.add_argument("--from-source", action="store_true")
    a = ap.parse_args(argv)
    out = a.out or ROOT
    if not a.from_source:
        write_source(out)
    write_pngs_and_manifest(out)
    return 0


if __name__ == "__main__":
    sys.exit(main())
