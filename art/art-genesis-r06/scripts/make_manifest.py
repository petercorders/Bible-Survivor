#!/usr/bin/env python3
"""Write export/manifest_r06.json from the two contracts and the MEASURED PNGs / pixel source (standard library only).

Usage:  python3 -I -B scripts/make_manifest.py [--root DIR] [--out DIR]
Every size, rect, alpha value, colour count, bounding box and hash below is measured from the files under <out>;
cell / fps / loop / pivot / ground / sorting / shadow / use come from src/contract_r06.json.
Deterministic output. Status stays production-draft until the root's art review; Unity import / devices are NOT_RUN."""
import argparse, hashlib, json, os, sys

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.dirname(HERE)
sys.path.insert(0, HERE)
import pngio
import convert_source


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


def shadow_ref(C, sid):
    if sid is None:
        return None
    if sid in C["references"]["shadows"]:
        return {"id": sid, "from": "art 03 (unchanged)", "sha256": C["references"]["shadows"][sid]}
    if sid in C["references"]["r03"]["shadows"]:
        r = C["references"]["r03"]["shadows"][sid]
        return {"id": sid, "from": "R03 package (unchanged)", "sha256": r["sha256"], "pivot": r["pivot"]}
    return {"id": sid, "from": "this package (art 03 shadow method)", "file": f"export/png/{sid}.png"}


def main(argv=None):
    ap = argparse.ArgumentParser()
    ap.add_argument("--root", default=ROOT)
    ap.add_argument("--out")
    a = ap.parse_args(argv)
    out = a.out or a.root
    C = json.load(open(os.path.join(a.root, "src", "contract_r06.json"), encoding="utf-8"))
    ext = json.load(open(os.path.join(a.root, "src", "palette_r06_ext.json"), encoding="utf-8"))
    src_rel = convert_source.SOURCE
    src = json.load(open(os.path.join(out, *src_rel.split("/")), encoding="utf-8"))
    authoring = {sp["name"]: sp["authoring"] for sp in src["sprites"]}
    assets = []
    for rel_contract, c in convert_source.all_assets(a.root):
        rel = f"export/png/{c['id']}.png"
        w, h, buf = pngio.read_rgba(os.path.join(out, *rel.split("/")))
        cw, chh = c["cell"]
        n = w // cw                          # measured frame count (the checker compares it with the contract)
        alphas = sorted({buf[i] for i in range(3, len(buf), 4)})
        colours = {bytes(buf[i:i + 4]) for i in range(0, len(buf), 4) if buf[i + 3]}
        assets.append({
            "id": c["id"], "kind": c.get("kind", "actor" if c["sorting"] == "Actors" else "shadow"), "contract": rel_contract,
            "file": rel, "sha256": sha(os.path.join(out, *rel.split("/"))), "size": [w, h],
            "cell": [cw, chh], "frames": n,
            "frameRects": [[k * cw, 0, cw, chh] for k in range(n)],
            "fps": c["fps"], "loop": c["loop"],
            "pivot": {"px": c["pivot"], "normalized": [c["pivot"][0] / cw, c["pivot"][1] / chh]},
            "ground": c.get("ground", False), "facing": c.get("facing", "none"),
            "alpha": {"policy": c["alpha"], "values": alphas},
            "sorting": c["sorting"], "ref": c["ref"],
            "shadow": shadow_ref(C, c.get("shadow")),
            "colours": len(colours),
            "frameBBoxTopLeft": [frame_bbox(buf, w, h, k * cw, cw) for k in range(n)],
            "use": c.get("use", ""),
            "source": {"authoring": authoring[c["id"]],
                       "method": "procedural generator written by Claude in this session (palette ramps only), deterministic",
                       "pixelSource": f"{src_rel}#{c['id']}"},
            "status": "production-draft"})
    man = {
        "schema": "bible-survivors.art-manifest-r06", "schemaVersion": 1, "status": "production-draft",
        "statusNote": "offline R06 (Isaac) F1-F3 enemy and Sitnah well-keeper boss sidecar draft: root art review pending; not imported into Unity, not checked on devices, not final game art",
        "ppu": C["ppu"], "pivotSpace": C["pivotSpace"], "frameLayout": C["frameLayout"], "flip": C["flip"],
        "importer": C["importer"],
        "palette": {"base": {"file": "src/palette_v03.json", "name": "master-v0.3", "sha256": sha(os.path.join(a.root, "src", "palette_v03.json"))},
                    "ext": {"file": "src/palette_r06_ext.json", "name": ext["name"], "sha256": sha(os.path.join(a.root, "src", "palette_r06_ext.json"))}},
        "inputs": {"contract": {"file": "src/contract_r06.json", "sha256": sha(os.path.join(a.root, "src", "contract_r06.json"))},
                   "telegraph": {"file": "src/telegraph_r06.json", "sha256": sha(os.path.join(a.root, "src", "telegraph_r06.json")), "assets": "none (ring drawn from Core)"},
                   "generator": {"file": "scripts/actors_r06.py", "sha256": sha(os.path.join(a.root, "scripts", "actors_r06.py"))},
                   "helpers": {"scripts/px03.py": sha(os.path.join(a.root, "scripts", "px03.py")),
                               "scripts/pngio.py": sha(os.path.join(a.root, "scripts", "pngio.py"))}},
        "data": {"pixelSource": {"file": src_rel, "sha256": sha(os.path.join(out, *src_rel.split("/")))}},
        "references": C["references"],
        "notProduced": C["notProduced"],
        "build": {"command": "python3 -I -B scripts/build_r06.py", "testedWith": {"python": "3.11.6", "numpy": "2.4.3"},
                  "png": "scripts/convert_source.py (standard library) from the pixel source"},
        "provenance": {"pixels": "scripts/actors_r06.py, a procedural generator written by Claude (Anthropic model) in this session; not exported from the Claude Design product",
                       "designCanvas": "Claude Design canvas (Artifact Design type) used only to present the sheets and boards for review; see README/RESULT"},
        "license": {"status": "original", "references": [],
                    "art": "every PNG here was made for Bible Survivors by this folder's scripts from src/palette_v03.json (art 03 master palette, unchanged) and src/palette_r06_ext.json. No third-party images, traces or reference art.",
                    "motifs": C["motifs"]["source"]},
        "assets": assets}
    dst = os.path.join(out, "export", "manifest_r06.json")
    os.makedirs(os.path.dirname(dst), exist_ok=True)
    with open(dst, "w", encoding="utf-8") as f:
        json.dump(man, f, ensure_ascii=False, indent=1)
        f.write("\n")
    print(f"manifest -> export/manifest_r06.json ({len(assets)} assets)")
    return 0


if __name__ == "__main__":
    sys.exit(main())
