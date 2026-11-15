#!/usr/bin/env python3
"""Build the R04 sidecar: generator -> pixel source JSON -> PNG sheets -> manifest.

Usage:  python3 -I -B scripts/build_r04.py [--out DIR]
Step 1 (needs numpy)        scripts/actors_r04.py draws every frame of every asset in src/contract_r04.json and
                            src/telegraph_r04.json; each sprite is written as palette-keyed rows to
                            <out>/export/source/pixel-source_r04.json (the editable grid source; '.' = transparent).
Step 2 (standard library)   scripts/convert_source.py turns that source into <out>/export/png/*.png.
Step 3 (standard library)   scripts/make_manifest.py measures the PNGs and writes <out>/export/manifest_r04.json.
Deterministic: running it again (or into a temp --out) gives byte-identical files; scripts/check_r04.py --regen compares them."""
import argparse, json, os, sys

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.dirname(HERE)
sys.path.insert(0, HERE)
import convert_source


def pixel_source(out):
    import actors_r04 as A
    import px03 as px
    ext = json.load(open(os.path.join(ROOT, "src", "palette_r04_ext.json"), encoding="utf-8"))
    sprites = []
    for rel, c in convert_source.all_assets(ROOT):
        cw, ch = c["cell"]
        gen = A.GENERATORS[c["id"]]
        frames = [gen(k) for k in range(c["frames"])]
        names = sorted({px.NAME_OF[tuple(int(v) for v in p)] for fr in frames for p in fr[fr[..., 3] > 0]})
        if len(names) > 26:
            raise SystemExit(f"{c['id']}: {len(names)} colours do not fit one-letter keys")
        key_of = {n: chr(ord("a") + i) for i, n in enumerate(names)}
        rows = []
        for fr in frames:
            assert fr.shape == (ch, cw, 4), (c["id"], fr.shape)
            rows.append(["".join(key_of[px.NAME_OF[tuple(int(v) for v in fr[y, x])]] if fr[y, x, 3] else "."
                                 for x in range(cw)) for y in range(ch)])
        sprites.append({"name": c["id"], "contract": rel, "authoring": f"scripts/actors_r04.py:{gen.__name__}",
                        "cell": [cw, ch], "pivot_px": c["pivot"], "fps": c["fps"], "loop": c["loop"],
                        "alpha": c["alpha"], "keys": {v: k for k, v in key_of.items()}, "frames": rows})
    src = {"schema": "bible-survivors.pixel-source", "schemaVersion": 3, "status": "production-draft",
           "palette": {"base": "src/palette_v03.json (master-v0.3)", "ext": f"src/palette_r04_ext.json ({ext['name']})"},
           "rules": {"transparent": ".", "rows": "every row has exactly cell.w characters (no trimming)",
                     "keys": "per sprite: character -> palette entry 'ramp.index' (base actor/ui/terrain groups or ext actor group)",
                     "edit": "hand edits go here; scripts/convert_source.py rebuilds the PNGs and scripts/make_manifest.py the manifest. Re-running build_r04.py regenerates this file from the generator and overwrites hand edits."},
           "sprites": sprites}
    dst = os.path.join(out, *convert_source.SOURCE.split("/"))
    os.makedirs(os.path.dirname(dst), exist_ok=True)
    with open(dst, "w", encoding="utf-8") as f:
        json.dump(src, f, ensure_ascii=False, indent=1)
        f.write("\n")
    print(f"source -> {convert_source.SOURCE} ({len(sprites)} sprites)")
    return dst


def main(argv=None):
    ap = argparse.ArgumentParser()
    ap.add_argument("--out")
    a = ap.parse_args(argv)
    out = a.out or ROOT
    src = pixel_source(out)
    import make_manifest
    convert_source.main(["--root", ROOT, "--source", src, "--out", out])
    make_manifest.main(["--root", ROOT, "--out", out])
    return 0


if __name__ == "__main__":
    sys.exit(main())
