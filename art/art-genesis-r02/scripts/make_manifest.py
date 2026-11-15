#!/usr/bin/env python3
"""Write export/manifest_r02.json from the contract and the MEASURED PNGs / pixel source (standard library only).

Usage:  python3 -I -B scripts/make_manifest.py [--root DIR] [--out DIR]
Every size, rect, alpha value, colour count, bounding box and hash below is measured from the files under <root>;
cell / fps / loop / pivot / ground / sorting / shadow come from src/contract_r02.json. Deterministic output.
Status stays production-draft until the root's independent art review; Unity import / devices are NOT_RUN."""
import argparse, hashlib, json, os, sys

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.dirname(HERE)
sys.path.insert(0, HERE)
import pngio

AUTHORING = {"enm_b1-envy-eye_move_32": "envy_eye", "enm_b2-rage-hound_run_32": "rage_hound",
             "enm_b3-cracked-mask_move_32": "cracked_mask"}


def sha(path):
    return hashlib.sha256(open(path, "rb").read()).hexdigest()


def frame_bbox(buf, w, h, x0, cw):
    xs, ys = [], []
    for y in range(h):
        for x in range(x0, x0 + cw):
            if buf[(y * w + x) * 4 + 3]:
                xs.append(x - x0)
                ys.append(y)
    return [min(xs), min(ys), max(xs), max(ys)] if xs else None


def main(argv=None):
    ap = argparse.ArgumentParser()
    ap.add_argument("--root", default=ROOT)
    ap.add_argument("--out")
    a = ap.parse_args(argv)
    out = a.out or a.root
    C = json.load(open(os.path.join(a.root, "src", "contract_r02.json"), encoding="utf-8"))
    ext = json.load(open(os.path.join(a.root, "src", "palette_r02_ext.json"), encoding="utf-8"))
    src_rel = "export/source/pixel-source_r02.json"
    assets = []
    for c in C["assets"]:
        rel = f"export/png/{c['id']}.png"
        w, h, buf = pngio.read_rgba(os.path.join(out, *rel.split("/")))
        cw, chh = c["cell"]
        n = w // cw                          # measured frame count (the checker compares it with the contract)
        alphas = sorted({buf[i] for i in range(3, len(buf), 4)})
        colours = {bytes(buf[i:i + 4]) for i in range(0, len(buf), 4) if buf[i + 3]}
        assets.append({
            "id": c["id"], "file": rel, "sha256": sha(os.path.join(out, *rel.split("/"))), "size": [w, h],
            "cell": [cw, chh], "frames": n,
            "frameRects": [[k * cw, 0, cw, chh] for k in range(n)],
            "fps": c["fps"], "loop": c["loop"],
            "pivot": {"px": c["pivot"], "normalized": [c["pivot"][0] / cw, c["pivot"][1] / chh]},
            "ground": c["ground"], "facing": c["facing"],
            "alpha": {"policy": c["alpha"], "values": alphas},
            "sorting": c["sorting"], "quality": c["quality"], "ref": c["ref"],
            "shadow": {"id": c["shadow"], "from": "art 03 (unchanged)", "sha256": C["references"]["shadows"][c["shadow"]]},
            "colours": len(colours),
            "frameBBoxTopLeft": [frame_bbox(buf, w, h, k * cw, cw) for k in range(n)],
            "source": {"authoring": f"scripts/enemies_r02.py:{AUTHORING[c['id']]}",
                       "method": "procedural generator written by Claude in this session (palette ramps only), deterministic",
                       "pixelSource": f"{src_rel}#{c['id']}"},
            "status": "production-draft"})
    man = {
        "schema": "bible-survivors.art-manifest-r02", "schemaVersion": 1, "status": "production-draft",
        "statusNote": "offline R02 enemy sidecar draft: root independent art review pending; not imported into Unity, not checked on devices, not final game art",
        "ppu": C["ppu"], "pivotSpace": C["pivotSpace"], "frameLayout": C["frameLayout"], "flip": C["flip"],
        "importer": C["importer"],
        "palette": {"base": {"file": "src/palette_v03.json", "name": "master-v0.3", "sha256": sha(os.path.join(a.root, "src", "palette_v03.json"))},
                    "ext": {"file": "src/palette_r02_ext.json", "name": ext["name"], "sha256": sha(os.path.join(a.root, "src", "palette_r02_ext.json"))}},
        "inputs": {"contract": {"file": "src/contract_r02.json", "sha256": sha(os.path.join(a.root, "src", "contract_r02.json"))},
                   "generator": {"file": "scripts/enemies_r02.py", "sha256": sha(os.path.join(a.root, "scripts", "enemies_r02.py"))},
                   "helpers": {"scripts/px03.py": sha(os.path.join(a.root, "scripts", "px03.py")),
                               "scripts/pngio.py": sha(os.path.join(a.root, "scripts", "pngio.py"))}},
        "data": {"pixelSource": {"file": src_rel, "sha256": sha(os.path.join(out, *src_rel.split("/")))}},
        "references": C["references"],
        "build": {"command": "python3 -I -B scripts/build_r02.py", "testedWith": {"python": "3.11.6", "numpy": "2.4.3"},
                  "png": "scripts/convert_source.py (standard library) from the pixel source"},
        "provenance": {"pixels": "scripts/enemies_r02.py, a procedural generator written by Claude (Anthropic model) in this session; not exported from the Claude Design product",
                       "designCanvas": "Claude Design canvas (Artifact Design type) used to present the sheets and comparisons for review; see README/RESULT"},
        "license": {"status": "original", "references": [],
                    "art": "every PNG here was made for Bible Survivors by this folder's scripts from src/palette_v03.json (art 03 master palette, unchanged) and src/palette_r02_ext.json. No third-party images, traces or reference art.",
                    "reviewFonts": "review boards only: Galmuri 2.40.4 (SIL OFL 1.1) from the art-unity-font-01 sidecar; no font is inside a sprite"},
        "assets": assets}
    dst = os.path.join(out, "export", "manifest_r02.json")
    os.makedirs(os.path.dirname(dst), exist_ok=True)
    with open(dst, "w", encoding="utf-8") as f:
        json.dump(man, f, ensure_ascii=False, indent=1)
        f.write("\n")
    print(f"manifest -> {os.path.relpath(dst, out)} ({len(assets)} assets)")
    return 0


if __name__ == "__main__":
    sys.exit(main())
