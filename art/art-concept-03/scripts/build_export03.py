"""Build the runtime handoff of art 03 (production-draft, not final game art).

Usage:  python3 -I scripts/build_export03.py [--out DIR]
Reads   src/palette_v03.json, src/player_rig_v3.json, src/contract_v3.json (ids/refs, manifest top-level ppu/pivotSpace/
        frameLayout/flip, font source), third_party/galmuri (font files are only hashed into ui-pieces fonts)
Writes  <DIR or art-concept-03>/export/png/*.png            one horizontal strip per asset (stdlib PNG writer)
        <...>/export/source/pixel-source_v3.json             explicit frame grids for every exported PNG
        <...>/export/ui/ui-pieces_v3.json                    9-slice borders, states, fonts, colour tokens, layouts
        <...>/export/tiles/path-16mask_v3.json               neighbour-mask rule, mask -> frame/rect, test map
        <...>/export/manifest_v3.json                        everything above with sizes, pivots, timing, hashes
Depends on Python 3 + NumPy only for building (PNG bytes are written with zlib). No 01/02 files and no system fonts."""
import argparse, hashlib, json, os, sys
HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE)
import numpy as np
import px03 as px, player03 as pl, enemies03 as en, fx03 as fx, terrain03 as tr, tiles03 as tl, ui03 as ui, pngio

ROOT = px.ROOT
ap = argparse.ArgumentParser()
ap.add_argument("--out", default=ROOT, help="root folder that receives export/ (default: art-concept-03)")
args = ap.parse_args()
OUTROOT = os.path.abspath(args.out)
EXP = os.path.join(OUTROOT, "export")
for d in ("png", "source", "ui", "tiles"):
    os.makedirs(os.path.join(EXP, d), exist_ok=True)


def rel(p):
    return os.path.relpath(p, ROOT).replace(os.sep, "/")


def sha(path):
    return hashlib.sha256(open(path, "rb").read()).hexdigest()


CONTRACT = json.load(open(os.path.join(ROOT, "src", "contract_v3.json"), encoding="utf-8"))
CA = {a["id"]: a for a in CONTRACT["assets"]}

# ---------------------------------------------------------------- registry
SPR = {}


def add(name, frames, pivot, *, fps=0, loop=False, ground=False, alpha="binary", sorting, quality, ref, source, extra=None):
    frames = frames if isinstance(frames, list) else [frames]
    assert name not in SPR, name
    SPR[name] = dict(frames=frames, pivot=list(pivot), fps=fps, loop=loop, ground=ground, alpha=alpha, sorting=sorting,
                     quality=quality, ref=ref, source=source, extra=extra or {})


def from_contract(name, frames, source, extra=None):
    c = CA[name]
    add(name, frames, c["pivot"], fps=c["fps"], loop=c["loop"], ground=c["ground"], alpha=c["alpha"], sorting=c["sorting"],
        quality=c["quality"], ref=c["ref"], source=source, extra=extra)


# player: 6 layers x 3 animations, default (key) colours
PC = CONTRACT["player"]
rig_src = {"authoring": "src/player_rig_v3.json", "method": "hand-placed parts (cut from the hand-drawn 02 layers and edited) placed per frame by the pose table in the rig; scripts/player03.py"}
for anim, spec in PC["animations"].items():
    lay = pl.anim_layers(anim)
    for ln in PC["layers"]:
        add(f"chr_player_layer-{PC['fileLayerNames'][ln]}_{anim}_32", lay[ln], PC["pivot"], fps=spec["fps"], loop=spec["loop"],
            ground="layer", alpha="binary", sorting="Actors", quality="core", ref="player:player", source=rig_src,
            extra={"layer": ln, "animation": anim, "layerOrder": pl.RIG["layer_order"].index(ln),
                   "recolorGroups": pl.RIG["recolor_groups"], "keysFromRig": True})

proc = lambda fn: {"authoring": f"scripts/{fn}", "method": "procedural generator (palette ramps only), deterministic"}
from_contract("enm_a1-small-snake_move_32", [en.snake(i) for i in range(4)], proc("enemies03.py:snake"))
from_contract("enm_a2-fruit-moth_fly_32", [en.moth(i) for i in range(4)], proc("enemies03.py:moth"))
from_contract("enm_a3-thorn-beast_walk_32", [en.beast(i) for i in range(4)], proc("enemies03.py:beast"))
from_contract("fx_enemy-death_burst_32", [fx.death_burst(i) for i in range(4)], proc("fx03.py:death_burst"))
from_contract("wpn_bible-book_spin_16", [fx.book(0), fx.book(1)], {"authoring": "scripts/fx03.py:BOOK", "method": "hand grid"})
from_contract("fx_bible-glow_static_20", [fx.book_glow(0)], proc("fx03.py:book_glow"))
from_contract("wpn_jawbone_held_16", [fx.jawbone()], proc("fx03.py:jawbone"))
swing = [fx.jaw_swing(i) for i in range(4)]
edge, trail = [], []
for fr, _ in swing:
    e = fr.copy(); t = fr.copy()
    opaque = fr[..., 3] == 255
    e[~opaque] = 0
    t[opaque] = 0
    edge.append(e); trail.append(t)
attach = [list(a) for _, a in swing]
jaw_extra = {"attachPx": attach, "attachNote": "jawbone sprite centre per frame, pixel coordinates from the TOP-LEFT of the 64x64 frame; art faces right (0 deg); rotate the whole effect to the swing direction about the pivot; flipX for left",
             "pairedWith": None}
from_contract("fx_jawbone-swing-edge_hit_64", edge, proc("fx03.py:jaw_swing (opaque pixels)"), dict(jaw_extra, pairedWith="fx_jawbone-swing-trail_hit_64"))
from_contract("fx_jawbone-swing-trail_hit_64", trail, proc("fx03.py:jaw_swing (see-through pixels)"), dict(jaw_extra, pairedWith="fx_jawbone-swing-edge_hit_64"))
from_contract("wpn_sling-stone_spin_8", [fx.stone_spin(0), fx.stone_spin(1)], proc("fx03.py:stone_spin"))
from_contract("fx_sling-trail_static_20x8", [fx.stone_trail()], proc("fx03.py:stone_trail"),
              {"note": "points left from the stone (pivot at the stone end); rotate with the shot direction"})
from_contract("fx_hit-spark_hit_16", [fx.hit(i) for i in range(3)], proc("fx03.py:hit"))
gb, gn = fx.gem_frames("gem"), fx.gem_frames("gem2")
from_contract("pkp_xp-gem-base_idle_8", gb, {"authoring": "scripts/fx03.py:GEM/GEM1", "method": "hand grid"})
from_contract("pkp_xp-gem-bonus_idle_8", gn, {"authoring": "scripts/fx03.py:GEM/GEM1", "method": "hand grid"})
for nm, (w, h, key) in {"fx_shadow_static_16x6": (16, 6, "shadow.0"), "fx_shadow-snake_static_20x4": (20, 4, "shadow.1"),
                        "fx_shadow-wide_static_24x6": (24, 6, "shadow.0"), "fx_shadow-small_static_12x4": (12, 4, "shadow.1")}.items():
    from_contract(nm, [fx.shadow(w, h, key)], proc("fx03.py:shadow"))
path_tiles, path_masks = tl.build()
from_contract("til_eden_path-16mask_static_32", path_tiles, proc("tiles03.py:path_tile"),
              {"tileRule": "frame index = mask = N*1 + E*2 + S*4 + W*8 (neighbour is path); diagonals ignored",
               "mapping": "export/tiles/path-16mask_v3.json"})
ground = tr.build_tiles()
for nm, a in ground.items():
    g = CONTRACT["assetGroups"]["groundTiles"]
    add(nm, [a], g["pivot"], alpha="binary", sorting=g["sorting"], quality=g["quality"], ref=g["ref"], source=proc("terrain03.py:base_tile"),
        extra={"placement": "random pick among grass-a..d (meadow-a..b inside meadow patches) + random flipX"})
for nm, a in tr.build_decals().items():
    g = CONTRACT["assetGroups"]["decals"]
    add(nm, [a], (a.shape[1] // 2, 0), alpha="binary", sorting=g["sorting"], quality=g["quality"], ref=g["ref"],
        source=proc("terrain03.py:build_decals"), extra={"collider": False, "late": "thorn-vine" in nm})
ICONS = ui.icons()
for nm, a in ICONS.items():
    from_contract(nm, [a], {"authoring": f"scripts/ui03.py:icons ({nm})", "method": "procedural/hand"})
PIECES = ui.build()
for pid, pc in PIECES.items():
    g = CONTRACT["assetGroups"]["uiPieces"]
    h, w = pc["frames"][0].shape[:2]
    add(pid, pc["frames"], (w // 2, h // 2), alpha="binary", sorting=g["sorting"], quality=g["quality"], ref=g["ref"],
        source={"authoring": "scripts/ui03.py:build", "method": "procedural pixel boxes"},
        extra={"states": pc["states"], "border_LBRT": list(pc["border"]) if pc["border"] else None,
               "content_xywh_topLeft": list(pc["content"]) if pc["content"] else None, "note": pc["note"]})

# ---------------------------------------------------------------- pixel source v3 (explicit grids)
ALPHABET = "abcdefghijmnopqstuvxzACDEFGHIJLMNPQSUVXZ!#$%&*+-=?@^~"
RIG_REV = {}
for ch, nm in pl.RIG["keys"].items():
    c = px.C(nm)
    assert c not in RIG_REV, f"rig keys {RIG_REV.get(c)} and {ch} share colour {c}"
    RIG_REV[c] = ch


def grids_for(name, s):
    frames = s["frames"]
    h, w = frames[0].shape[:2]
    used = []
    for fr in frames:
        assert fr.shape[:2] == (h, w), name
        for c in sorted(px.colors(fr)):
            if c not in used:
                used.append(c)
    if s["extra"].get("keysFromRig"):
        cmap = {c: RIG_REV[c] for c in used}
        keys = {RIG_REV[c]: pl.RIG["keys"][RIG_REV[c]] for c in used}
    else:
        assert len(used) <= len(ALPHABET), name
        cmap, keys = {}, {}
        for i, c in enumerate(sorted(used, key=lambda c: px.NAME_OF[c])):
            cmap[c] = ALPHABET[i]
            keys[ALPHABET[i]] = px.NAME_OF[c]
    out = []
    for fr in frames:
        rows = []
        for y in range(h):
            rows.append("".join(cmap[tuple(int(v) for v in fr[y, x])] if fr[y, x, 3] else "." for x in range(w)))
        out.append(rows)
    return keys, out


src_sprites = []
for name, s in SPR.items():
    keys, grids = grids_for(name, s)
    src_sprites.append({"name": name, "cell": [s["frames"][0].shape[1], s["frames"][0].shape[0]], "pivot_px": s["pivot"],
                        "fps": s["fps"], "loop": s["loop"], "alpha": s["alpha"], "keys": keys, "frames": grids})
pal_path = os.path.join(ROOT, "src", "palette_v03.json")
PIXSRC = {"schema": "bible-survivors.pixel-source", "schemaVersion": 3,
          "status": "production-draft",
          "palette": {"file": "src/palette_v03.json", "name": px.PALJ["name"], "sha256": sha(pal_path)},
          "rules": {"transparent": ".", "rows": "every row has exactly cell.w characters (no trimming)",
                    "keys": "per sprite: character -> palette entry 'ramp.index' (actor/ui/terrain groups); player layers use the rig keys so recolor groups apply",
                    "recolor": "player layers: replace ramps skin/hair/tunic/cloak with a palette 'recolor' set (skin s1-s6, hair x6, dye x6 for tunic and cloak)"},
          "sprites": src_sprites}
src_file = os.path.join(EXP, "source", "pixel-source_v3.json")
json.dump(PIXSRC, open(src_file, "w", encoding="utf-8"), ensure_ascii=False, separators=(",", ":"))

# ---------------------------------------------------------------- PNG strips
files = {}
for name, s in SPR.items():
    strip = np.ascontiguousarray(np.concatenate(s["frames"], axis=1))
    strip[strip[..., 3] == 0] = 0
    p = os.path.join(EXP, "png", name + ".png")
    pngio.write_rgba(p, strip.shape[1], strip.shape[0], strip.tobytes())
    files[name] = p

# ---------------------------------------------------------------- tiles mapping
tile_map = {"schema": "bible-survivors.tile-rule", "schemaVersion": 3, "tileset": "til_eden_path-16mask_static_32",
            "bits": {"N": 1, "E": 2, "S": 4, "W": 8}, "rule": "a bit is set when the orthogonal neighbour cell is path; frame = mask",
            "diagonals": "not used. Cells that touch only diagonally are drawn as separate ends; a solid 2x2 (or wider) path shows a grass notch at the shared inner corner, so the path layer must stay one cell wide (a 47-tile blob set would be needed for wide paths)",
            "flip": "tiles are not flipped or rotated (lighting comes from the top-left); each mask has its own drawing",
            "cell": [32, 32],
            "frames": [{"mask": m, "name": tl.NAMES[m], "rectTopLeft": [m * 32, 0, 32, 32], "rectUnityBottomLeft": [m * 32, 0, 32, 32]}
                       for m in range(16)],
            "testMap": {"cells": tl.TEST_MAP, "masks": tl.mask_grid(tl.TEST_MAP)},
            "unsupportedMap": {"cells": tl.UNSUPPORTED_MAP, "masks": tl.mask_grid(tl.UNSUPPORTED_MAP)}}
tile_file = os.path.join(EXP, "tiles", "path-16mask_v3.json")
json.dump(tile_map, open(tile_file, "w", encoding="utf-8"), ensure_ascii=False, indent=1)

# ---------------------------------------------------------------- UI data
def hexof(name):
    return "#%02x%02x%02x%02x" % px.C(name)


ui_data = {"schema": "bible-survivors.ui-pieces", "schemaVersion": 3, "status": "production-draft (art data, not implemented UI)",
           "pixelScale": "pieces are 1x game pixels; the game view is integer-scaled",
           "nineSlice": "border_LBRT = left, bottom, right, top in pixels (Unity Sprite Editor order). Edges and centre repeat (tile), corners fixed",
           "pieces": {pid: {"file": f"export/png/{pid}.png", "frameSize": list(pc["frames"][0].shape[1::-1]), "states": pc["states"],
                            "border_LBRT": list(pc["border"]) if pc["border"] else None,
                            "content_xywh_topLeft": list(pc["content"]) if pc["content"] else None, "note": pc["note"]}
                      for pid, pc in PIECES.items()},
           "stateRules": {"focus": "keyboard/gamepad focus or touch-down highlight", "pressed": "content moves down 1 px",
                          "disabled": "labels use text.2; not selectable", "new": "slot just gained an item this level-up",
                          "max": "item reached its max level"},
           "fonts": {**{role: {"file": f, "pixelSize": px, "sha256": sha(os.path.join(ROOT, *f.split("/"))), "use": use}
                           for role, f, px, use in (
                               ("title", "third_party/galmuri/Galmuri11Bitmap-Bold-2.40.4.ttf", 16, "titles, numbers"),
                               ("label", "third_party/galmuri/Galmuri11Bitmap-Regular-2.40.4.ttf", 16, "card names"),
                               ("body", "third_party/galmuri/Galmuri9Bitmap-Regular-2.40.4.ttf", 12, "descriptions"),
                               ("small", "third_party/galmuri/Galmuri7Bitmap-Regular-2.40.4.ttf", 10, "level numbers, tags"))},
                     "render": "no anti-aliasing (bitmap strikes at exactly these sizes); 1 px outline in outline.0 on the game view",
                     "license": {"file": "third_party/galmuri/LICENSE.txt", "name": "SIL OFL 1.1",
                                 "copyright": "Copyright 2019-2025 Lee Minseo"},
                     "source": CONTRACT["uiFonts"]["source"]},
           "colors": {n: hexof(n) for n in ("text.0", "text.1", "text.2", "panel.0", "panel.1", "panel.2", "panel.3", "gold.0", "gold.1",
                                            "hp.0", "hp.1", "hp.2", "xp.0", "xp.1", "xp.2", "xp.3", "dim.0", "dim.1")},
           "layoutsNote": "layouts in this file are mock-up candidates for review; orientation, safe areas and touch sizes are NOT decided"}
ui_file = os.path.join(EXP, "ui", "ui-pieces_v3.json")
json.dump(ui_data, open(ui_file, "w", encoding="utf-8"), ensure_ascii=False, indent=1)

# ---------------------------------------------------------------- manifest
alpha_vals = {}
for name, s in SPR.items():
    vals = sorted({int(v) for fr in s["frames"] for v in np.unique(fr[..., 3])})
    alpha_vals[name] = vals
assets = []
for name, s in SPR.items():
    h, w = s["frames"][0].shape[:2]
    n = len(s["frames"])
    a = {"id": name, "file": rel(files[name]) if OUTROOT == ROOT else "export/png/" + name + ".png",
         "sha256": sha(files[name]), "size": [w * n, h], "cell": [w, h], "frames": n,
         "frameRects": [[i * w, 0, w, h] for i in range(n)], "fps": s["fps"], "loop": s["loop"],
         "pivot": {"px": s["pivot"], "normalized": [round(s["pivot"][0] / w, 6), round(s["pivot"][1] / h, 6)]},
         "ground": s["ground"], "alpha": {"policy": s["alpha"], "values": alpha_vals[name]},
         "sorting": s["sorting"], "quality": s["quality"], "ref": s["ref"],
         "source": dict(s["source"], pixelSource=f"export/source/pixel-source_v3.json#{name}"),
         "status": "production-draft"}
    a.update(s["extra"])
    assets.append(a)
tool_versions = {"python": sys.version.split()[0], "numpy": np.__version__, "zlib": __import__("zlib").ZLIB_RUNTIME_VERSION}
manifest = {"schema": "bible-survivors.art-manifest", "schemaVersion": 3, "status": "production-draft",
            "statusNote": "offline handoff draft: not imported into Unity, not checked on devices, not final game art",
            "ppu": CONTRACT["ppu"], "pivotSpace": CONTRACT["pivotSpace"], "frameLayout": CONTRACT["frameLayout"],
            "layerOrder": pl.RIG["layer_order"], "flip": CONTRACT["flip"],
            "palette": PIXSRC["palette"],
            "inputs": {"contract": {"file": "src/contract_v3.json", "sha256": sha(os.path.join(ROOT, "src", "contract_v3.json"))},
                       "rig": {"file": "src/player_rig_v3.json", "sha256": sha(os.path.join(ROOT, "src", "player_rig_v3.json"))}},
            "data": {"pixelSource": {"file": "export/source/pixel-source_v3.json", "sha256": sha(src_file)},
                     "tiles": {"file": "export/tiles/path-16mask_v3.json", "sha256": sha(tile_file)},
                     "ui": {"file": "export/ui/ui-pieces_v3.json", "sha256": sha(ui_file)}},
            "build": {"command": "python3 -I scripts/build_export03.py", "testedWith": tool_versions},
            "license": {"status": "original", "references": [],
                        "art": "every PNG here was made for Bible Survivors: player parts are cut from the project's own 02 drafts "
                               "and edited by hand; everything else is drawn by this folder's scripts from src/palette_v03.json. "
                               "No third-party images, traces or reference art.",
                        "fonts": {"files": "third_party/galmuri/*.ttf (Galmuri 2.40.4)", "license": "SIL OFL 1.1",
                                  "notice": "third_party/galmuri/LICENSE.txt", "source": CONTRACT["uiFonts"]["source"],
                                  "use": "review boards and UI mock-up text only; no export PNG contains font glyphs"}},
            "assets": assets}
man_file = os.path.join(EXP, "manifest_v3.json")
json.dump(manifest, open(man_file, "w", encoding="utf-8"), ensure_ascii=False, indent=1)
print(f"export: {len(assets)} assets, {sum(a['frames'] for a in assets)} frames -> {rel(man_file) if OUTROOT == ROOT else man_file}")
