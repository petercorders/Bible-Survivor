#!/usr/bin/env python3
"""Art handoff checker (v3). Standard library only.

Usage:  python3 -I scripts/check_art03.py [--root DIR] [--src-root DIR] [--manifest PATH] [--catalog CATALOG_JSON] [--quiet]
  --root      folder that contains export/ (default: art-concept-03)
  --src-root  folder that contains src/ (palette, contract); default = --root
  --manifest  manifest path (default: <root>/export/manifest_v3.json)
  --catalog   optional: the core content catalog (Bible-Survivor content/genesis/catalog.json); weapon/monster/passive refs
              must then also exist there
Exit codes: 0 = all checks passed, 1 = at least one check failed, 2 = usage error.

Checks: schema/legacy detection; required fields; unique ids; file exists + sha256; PNG is 8-bit RGBA and its size is
cell x frames; frame rects; values against src/contract_v3.json (frames, fps, loop, cell, pivot, ground, alpha, sorting,
quality, ref); every player layer x animation present with the same cell/frames/fps/pivot; foot row of every single frame;
alpha policy and RGB=0 under alpha 0; colours in the palette, <=16 per sprite; no repeated neighbour frame in an animated
strip (contract frameChange; player accessory layers may hold); refs against the stable-id list; pixel-source
v3 grids render exactly to the PNGs; data files' hashes; tile mask table; 9-slice borders inside the piece; stale inputs.
Manifest top level (contract manifestTop): ppu, pivotSpace, frameLayout, layerOrder, flip must exist with the contract
type and value; each player layer asset's layerOrder must be its index in contract player.layers.
UI fonts (contract uiFonts): every role with the contract file/pixelSize/sha256; the path relative to --root, under the
font dir, no '.'/'..'/absolute/backslash; a real file (not a link) whose bytes match; licence notice file + hash; source.
"""
import argparse, hashlib, json, os, sys

HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE)
import pngio

REQ = ["id", "file", "sha256", "size", "cell", "frames", "frameRects", "fps", "loop", "pivot", "ground", "alpha", "sorting",
       "quality", "ref", "source", "status"]


class Report:
    def __init__(self):
        self.errors, self.checked = [], 0

    def fail(self, code, msg):
        self.errors.append(f"[{code}] {msg}")

    def ok(self):
        self.checked += 1


def sha(path):
    return hashlib.sha256(open(path, "rb").read()).hexdigest()


def load_json(path, R, code):
    try:
        return json.load(open(path, encoding="utf-8"))
    except FileNotFoundError:
        R.fail(code, f"missing file: {path}")
    except json.JSONDecodeError as e:
        R.fail(code, f"invalid JSON {path}: {e}")
    return None


def palette_colours(pal):
    cols = {}
    for group in ("actor", "ui", "terrain"):
        for ramp, lst in pal.get(group, {}).items():
            for i, h in enumerate(lst):
                h = h.lstrip("#")
                rgba = (int(h[0:2], 16), int(h[2:4], 16), int(h[4:6], 16), int(h[6:8], 16) if len(h) == 8 else 255)
                cols[f"{ramp}.{i}"] = rgba
    return cols


def main(argv=None):
    here_root = os.path.dirname(HERE)
    ap = argparse.ArgumentParser(add_help=True)
    ap.add_argument("--root", default=here_root)
    ap.add_argument("--src-root", default=None)
    ap.add_argument("--manifest", default=None)
    ap.add_argument("--catalog", default=None)
    ap.add_argument("--quiet", action="store_true")
    try:
        a = ap.parse_args(argv)
    except SystemExit as e:
        return 2 if e.code else 0
    root = os.path.abspath(a.root)
    src_root = os.path.abspath(a.src_root or a.root)
    man_path = a.manifest or os.path.join(root, "export", "manifest_v3.json")
    R = Report()

    M = load_json(man_path, R, "MANIFEST")
    if M is None:
        return finish(R, a.quiet)
    if M.get("schemaVersion") != 3 or M.get("schema") != "bible-survivors.art-manifest":
        if "sprites" in M and "schemaVersion" not in M:
            kind = "v2" if isinstance(M.get("palette"), dict) and "actor" in M.get("palette", {}) else "v1"
            R.fail("LEGACY", f"{man_path} is a legacy {kind} pixel-source file, not a v3 manifest "
                             f"(convert with scripts/convert_source.py; legacy files are not handoff input)")
        else:
            R.fail("SCHEMA", f"schema/schemaVersion must be bible-survivors.art-manifest / 3, got {M.get('schema')} / {M.get('schemaVersion')}")
        return finish(R, a.quiet)

    C = load_json(os.path.join(src_root, "src", "contract_v3.json"), R, "CONTRACT")
    pal_file = os.path.join(src_root, M.get("palette", {}).get("file", "src/palette_v03.json"))
    PAL = load_json(pal_file, R, "PALETTE")
    if C is None or PAL is None:
        return finish(R, a.quiet)
    if os.path.exists(pal_file) and sha(pal_file) != M["palette"].get("sha256"):
        R.fail("STALE", f"palette changed after export: {M['palette']['file']}")
    for k, v in M.get("inputs", {}).items():
        p = os.path.join(src_root, v["file"])
        if not os.path.exists(p):
            R.fail("INPUT", f"input missing: {v['file']}")
        elif sha(p) != v["sha256"]:
            R.fail("STALE", f"{k} input changed after export ({v['file']}); rebuild with scripts/build_export03.py")
    COL = palette_colours(PAL)
    COLSET = set(COL.values())
    check_top(M, C, R)

    # ---- data files
    data = {}
    for k, v in M.get("data", {}).items():
        p = os.path.join(root, v["file"])
        if not os.path.exists(p):
            R.fail("DATA", f"data file missing: {v['file']}")
            continue
        if sha(p) != v["sha256"]:
            R.fail("HASH", f"data file hash differs: {v['file']}")
        data[k] = load_json(p, R, "DATA")

    # ---- assets
    assets = M.get("assets")
    if not isinstance(assets, list) or not assets:
        R.fail("MANIFEST", "assets list missing or empty")
        return finish(R, a.quiet)
    by_id = {}
    pix = {}
    for i, e in enumerate(assets):
        miss = [f for f in REQ if f not in e]
        if miss:
            R.fail("FIELD", f"asset #{i} {e.get('id')}: missing fields {miss}")
            continue
        if e["id"] in by_id:
            R.fail("DUP", f"duplicate id {e['id']}")
        by_id[e["id"]] = e
        try:
            cw, ch = e["cell"]; n = e["frames"]
            px_, py_ = e["pivot"]["px"]
        except Exception:
            R.fail("FIELD", f"{e['id']}: cell/frames/pivot malformed")
            continue
        if not (isinstance(n, int) and n >= 1):
            R.fail("FRAMES", f"{e['id']}: frames must be a positive integer, got {n}")
            continue
        if e["size"] != [cw * n, ch]:
            R.fail("SIZE", f"{e['id']}: size {e['size']} != cell x frames {[cw * n, ch]}")
        if e["frameRects"] != [[k * cw, 0, cw, ch] for k in range(n)]:
            R.fail("RECT", f"{e['id']}: frameRects are not the horizontal strip layout")
        if not (0 <= px_ <= cw and 0 <= py_ <= ch):
            R.fail("PIVOT", f"{e['id']}: pivot {e['pivot']['px']} outside the {cw}x{ch} cell")
        nz = e["pivot"].get("normalized")
        if nz is None or abs(nz[0] - px_ / cw) > 1e-4 or abs(nz[1] - py_ / ch) > 1e-4:
            R.fail("PIVOT", f"{e['id']}: normalized pivot {nz} != px/cell")
        if e["fps"] < 0 or (n > 1 and e["fps"] <= 0 and "path-16mask" not in e["id"] and not e.get("states")):
            R.fail("FPS", f"{e['id']}: animated asset needs fps > 0")
        if e["sorting"] not in C["sortingLayers"]:
            R.fail("SORT", f"{e['id']}: unknown sorting layer {e['sorting']}")
        if e["quality"] not in C["qualityGroups"]:
            R.fail("QUALITY", f"{e['id']}: unknown quality group {e['quality']}")
        if e["alpha"].get("policy") not in C["alphaPolicies"]:
            R.fail("ALPHA", f"{e['id']}: unknown alpha policy {e['alpha'].get('policy')}")
        check_ref(e, C, R)
        # file + png
        p = os.path.join(root, e["file"])
        if not os.path.exists(p):
            R.fail("FILE", f"{e['id']}: file missing {e['file']}")
            continue
        if sha(p) != e["sha256"]:
            R.fail("HASH", f"{e['id']}: sha256 differs from manifest ({e['file']})")
        try:
            w, h, buf = pngio.read_rgba(p)
        except ValueError as ex:
            R.fail("PNG", f"{e['id']}: {ex}")
            continue
        if [w, h] != [cw * n, ch]:
            R.fail("SIZE", f"{e['id']}: PNG is {w}x{h}, expected {cw * n}x{ch} ({n} frames of {cw}x{ch})")
            continue
        pix[e["id"]] = (w, h, buf)
        check_pixels(e, w, h, buf, COLSET, COL, R)
        check_frame_change(e, w, h, buf, C, R)
        R.ok()

    check_contract(by_id, C, R)
    check_player(by_id, pix, C, R)
    check_source(by_id, pix, data.get("pixelSource"), COL, R)
    check_tiles(by_id, data.get("tiles"), R)
    check_ui(by_id, data.get("ui"), R)
    check_fonts(data.get("ui"), M, C, root, R)
    if a.catalog:
        check_catalog(by_id, a.catalog, R)
    return finish(R, a.quiet, len(assets))


def check_ref(e, C, R):
    ref = e.get("ref", "")
    if ":" not in ref:
        R.fail("REF", f"{e['id']}: ref '{ref}' is not kind:id")
        return
    kind, rid = ref.split(":", 1)
    S = C["stableIds"]
    if kind in ("weapon", "passive", "monster"):
        if rid not in S[kind]:
            R.fail("REF", f"{e['id']}: {kind} id '{rid}' is not a core stable id")
    elif kind in S["artOnly"]:
        if rid not in S["artOnly"][kind]:
            R.fail("REF", f"{e['id']}: art-only {kind} id '{rid}' is not listed in the contract")
    else:
        R.fail("REF", f"{e['id']}: unknown ref kind '{kind}'")


def check_pixels(e, w, h, buf, COLSET, COL, R):
    policy = e["alpha"]["policy"]
    cw, ch = e["cell"]
    used, alphas = set(), set()
    bad_rgb0 = bad_col = 0
    first_bad = None
    for i in range(0, len(buf), 4):
        r, g, b, al = buf[i], buf[i + 1], buf[i + 2], buf[i + 3]
        alphas.add(al)
        if al == 0:
            if r or g or b:
                bad_rgb0 += 1
            continue
        c = (r, g, b, al)
        used.add(c)
        if c not in COLSET:
            bad_col += 1
            if first_bad is None:
                first_bad = ((i // 4) % w, (i // 4) // w, c)
    if policy == "binary" and not alphas <= {0, 255}:
        R.fail("ALPHA", f"{e['id']}: alpha values {sorted(alphas - {0, 255})} in a binary-alpha sprite")
    if policy == "blend":
        allowed = {0} | {c[3] for c in COLSET}
        if not alphas <= allowed:
            R.fail("ALPHA", f"{e['id']}: alpha values {sorted(alphas - allowed)} not used by any palette entry")
    if sorted(alphas) != sorted(e["alpha"].get("values", [])):
        R.fail("ALPHA", f"{e['id']}: manifest alpha values {e['alpha'].get('values')} != PNG {sorted(alphas)}")
    if bad_rgb0:
        R.fail("ALPHA", f"{e['id']}: {bad_rgb0} fully transparent pixels with non-zero RGB")
    if bad_col:
        R.fail("COLOUR", f"{e['id']}: {bad_col} pixels not in the palette, first at {first_bad}")
    limit = 16
    if len(used) > limit and not e["id"].startswith("til_eden_path-16mask"):
        R.fail("COLOUR", f"{e['id']}: {len(used)} colours > {limit}")
    if e["id"].startswith("til_eden_path-16mask"):
        # per frame limit for the 16-tile strip
        for k in range(e["frames"]):
            fr = set()
            for y in range(h):
                for x in range(k * cw, (k + 1) * cw):
                    j = (y * w + x) * 4
                    if buf[j + 3]:
                        fr.add(bytes(buf[j:j + 4]))
            if len(fr) > limit:
                R.fail("COLOUR", f"{e['id']} frame {k}: {len(fr)} colours > {limit}")
    # foot row of every single frame for ground sprites (player layers are checked as a set in check_player)
    if e["ground"] is True:
        foot = ch - 1 - e["pivot"]["px"][1]
        for k in range(e["frames"]):
            low = lowest_row(buf, w, h, k * cw, cw)
            if low != foot:
                R.fail("FOOT", f"{e['id']} frame {k}: lowest opaque row {low} != foot row {foot}")


def check_frame_change(e, w, h, buf, C, R):
    """HOLD: an animated strip must not repeat the previous frame (loop: last vs first too). Rule in contract frameChange."""
    rule = C.get("frameChange")
    if rule is None:
        R.fail("CONTRACT", "contract has no frameChange rule")
        return
    n, cw = e["frames"], e["cell"][0]
    if e["fps"] <= 0 or n < 2 or e.get("layer") in rule["holdAllowedPlayerLayers"]:
        return
    frames = [b"".join(bytes(buf[(y * w + k * cw) * 4:(y * w + (k + 1) * cw) * 4]) for y in range(h)) for k in range(n)]
    pairs = [(k, k + 1) for k in range(n - 1)] + ([(n - 1, 0)] if e["loop"] else [])
    for a, b in pairs:
        if frames[a] == frames[b]:
            R.fail("HOLD", f"{e['id']}: frame {b} repeats frame {a} (animated strip must change every frame)")


def lowest_row(buf, w, h, x0, cw):
    for y in range(h - 1, -1, -1):
        for x in range(x0, x0 + cw):
            if buf[(y * w + x) * 4 + 3]:
                return y
    return None


def check_contract(by_id, C, R):
    for c in C["assets"]:
        e = by_id.get(c["id"])
        if e is None:
            R.fail("MISSING", f"contract asset {c['id']} is not in the manifest")
            continue
        for k, mk in (("cell", "cell"), ("frames", "frames"), ("fps", "fps"), ("loop", "loop"), ("ground", "ground"),
                      ("sorting", "sorting"), ("quality", "quality"), ("ref", "ref")):
            if e.get(mk) != c[k]:
                R.fail("CONTRACT", f"{c['id']}: {k} is {e.get(mk)!r}, contract says {c[k]!r}")
        if e["pivot"]["px"] != c["pivot"]:
            R.fail("CONTRACT", f"{c['id']}: pivot {e['pivot']['px']} != contract {c['pivot']}")
        if e["alpha"]["policy"] != c["alpha"]:
            R.fail("CONTRACT", f"{c['id']}: alpha policy {e['alpha']['policy']} != contract {c['alpha']}")
    for gname, g in C["assetGroups"].items():
        if gname == "note":
            continue
        members = [e for i, e in by_id.items() if i.startswith(g["prefix"]) and i not in g.get("exclude", [])]
        if len(members) < g["min"]:
            R.fail("MISSING", f"group {gname}: {len(members)} assets with prefix {g['prefix']}, need >= {g['min']}")
        for e in members:
            if e["alpha"]["policy"] != g["alpha"] or e["sorting"] != g["sorting"] or e["quality"] != g["quality"] or e["ref"] != g["ref"]:
                R.fail("CONTRACT", f"{e['id']}: alpha/sorting/quality/ref differ from group {gname}")
            if "cell" in g and e["cell"] != g["cell"]:
                R.fail("CONTRACT", f"{e['id']}: cell {e['cell']} != group {g['cell']}")
            if g.get("frames") and e["frames"] != g["frames"]:
                R.fail("CONTRACT", f"{e['id']}: frames {e['frames']} != group {g['frames']}")
            if isinstance(g.get("pivot"), list) and e["pivot"]["px"] != g["pivot"]:
                R.fail("CONTRACT", f"{e['id']}: pivot {e['pivot']['px']} != group {g['pivot']}")
            if g.get("pivot") == "bottom-centre: [w/2, 0]" and e["pivot"]["px"] != [e["cell"][0] // 2, 0]:
                R.fail("CONTRACT", f"{e['id']}: decal pivot must be bottom-centre")


def check_player(by_id, pix, C, R):
    P = C["player"]
    for anim, spec in P["animations"].items():
        lows = {}
        for ln in P["layers"]:
            pid = f"chr_player_layer-{P['fileLayerNames'][ln]}_{anim}_32"
            e = by_id.get(pid)
            if e is None:
                R.fail("LAYER", f"player layer missing: {pid}")
                continue
            for k in ("frames", "fps", "loop"):
                if e[k] != spec[k]:
                    R.fail("LAYER", f"{pid}: {k} {e[k]!r} != {anim} timeline {spec[k]!r}")
            if e["cell"] != P["cell"] or e["pivot"]["px"] != P["pivot"]:
                R.fail("LAYER", f"{pid}: cell/pivot differ from the player contract")
            if e["ref"] != "player:player" or e.get("layer") != ln or e.get("animation") != anim:
                R.fail("LAYER", f"{pid}: ref/layer/animation fields wrong")
            want_order = P["layers"].index(ln)
            if type(e.get("layerOrder")) is not int or e["layerOrder"] != want_order:
                R.fail("LAYER", f"{pid}: layerOrder {e.get('layerOrder')!r} != {want_order} (index of {ln} in contract player.layers)")
            if pid in pix:
                w, h, buf = pix[pid]
                cw = P["cell"][0]
                lows[ln] = [lowest_row(buf, w, h, k * cw, cw) for k in range(e["frames"])]
        foot = P["cell"][1] - 1 - P["pivot"][1]
        if "base" in lows:
            for k, low in enumerate(lows["base"]):
                if low != foot:
                    R.fail("FOOT", f"player base {anim} frame {k}: lowest row {low} != foot row {foot}")
            for ln, ls in lows.items():
                for k, low in enumerate(ls):
                    if low is not None and low > foot:
                        R.fail("FOOT", f"player {ln} {anim} frame {k}: row {low} below the foot row {foot}")


def check_source(by_id, pix, S, COL, R):
    if S is None:
        R.fail("SOURCE", "pixel-source data missing")
        return
    if S.get("schemaVersion") != 3 or S.get("schema") != "bible-survivors.pixel-source":
        R.fail("SOURCE", "pixel-source is not schemaVersion 3")
        return
    sp = {s["name"]: s for s in S.get("sprites", [])}
    for aid, e in by_id.items():
        s = sp.get(aid)
        if s is None:
            R.fail("SOURCE", f"{aid}: no pixel-source sprite")
            continue
        cw, ch = e["cell"]
        if s["cell"] != [cw, ch] or len(s["frames"]) != e["frames"] or s["pivot_px"] != e["pivot"]["px"]:
            R.fail("SOURCE", f"{aid}: pixel-source cell/frames/pivot differ from the manifest")
            continue
        if aid not in pix:
            continue
        w, h, buf = pix[aid]
        bad = 0
        for k, rows in enumerate(s["frames"]):
            if len(rows) != ch or any(len(r) != cw for r in rows):
                R.fail("SOURCE", f"{aid} frame {k}: grid is not {cw}x{ch}")
                bad = -1
                break
            for y, row in enumerate(rows):
                for x, chx in enumerate(row):
                    j = (y * w + k * cw + x) * 4
                    if chx == ".":
                        want = (0, 0, 0, 0)
                    else:
                        nm = s["keys"].get(chx)
                        if nm not in COL:
                            R.fail("SOURCE", f"{aid}: key {chx!r} -> {nm!r} is not a palette entry")
                            return
                        want = COL[nm]
                    if tuple(buf[j:j + 4]) != want:
                        bad += 1
        if bad > 0:
            R.fail("SOURCE", f"{aid}: {bad} PNG pixels differ from the pixel-source grids")


def check_tiles(by_id, T, R):
    e = by_id.get("til_eden_path-16mask_static_32")
    if T is None or e is None:
        R.fail("TILES", "path tile mapping or tileset missing")
        return
    if T.get("bits") != {"N": 1, "E": 2, "S": 4, "W": 8}:
        R.fail("TILES", "bit definition must be N1 E2 S4 W8")
    masks = [f["mask"] for f in T.get("frames", [])]
    if sorted(masks) != list(range(16)) or e["frames"] != 16:
        R.fail("TILES", "tile mapping must list masks 0..15 once each and the strip must have 16 frames")
    for f in T.get("frames", []):
        if f["rectTopLeft"] != [f["mask"] * 32, 0, 32, 32]:
            R.fail("TILES", f"mask {f['mask']}: rect {f['rectTopLeft']} != frame index layout")
    tm = T.get("testMap", {})
    used = {m for row in tm.get("masks", []) for m in row if m >= 0}
    if used != set(range(16)):
        R.fail("TILES", f"test map does not use every mask (missing {sorted(set(range(16)) - used)})")
    # masks recomputed from the cells must equal the stored masks
    cells = tm.get("cells", [])
    hgt, wid = len(cells), max((len(r) for r in cells), default=0)
    Pm = [[x < len(r) and r[x] == "#" for x in range(wid)] for r in cells]
    for y in range(hgt):
        for x in range(wid):
            if not Pm[y][x]:
                continue
            m = (1 if y > 0 and Pm[y - 1][x] else 0) | (2 if x < wid - 1 and Pm[y][x + 1] else 0) | \
                (4 if y < hgt - 1 and Pm[y + 1][x] else 0) | (8 if x > 0 and Pm[y][x - 1] else 0)
            if tm["masks"][y][x] != m:
                R.fail("TILES", f"test map cell ({x},{y}): stored mask {tm['masks'][y][x]} != {m}")


def check_ui(by_id, U, R):
    if U is None:
        R.fail("UI", "ui data missing")
        return
    for pid, pc in U.get("pieces", {}).items():
        e = by_id.get(pid)
        if e is None:
            R.fail("UI", f"ui piece {pid} has no manifest asset")
            continue
        fw, fh = pc["frameSize"]
        if e["cell"] != [fw, fh] or len(pc["states"]) != e["frames"]:
            R.fail("UI", f"{pid}: frame size/states differ from the manifest")
        b = pc.get("border_LBRT")
        if b is not None:
            L, B, Rr, T = b
            if min(b) < 0 or L + Rr >= fw or B + T >= fh:
                R.fail("UI", f"{pid}: 9-slice border {b} does not leave a centre inside {fw}x{fh}")
        c = pc.get("content_xywh_topLeft")
        if c is not None and (c[0] < 0 or c[1] < 0 or c[0] + c[2] > fw or c[1] + c[3] > fh):
            R.fail("UI", f"{pid}: content rect {c} outside {fw}x{fh}")
    for aid in by_id:
        if aid.startswith("ui_") and aid not in U.get("pieces", {}):
            R.fail("UI", f"{aid}: no entry in ui-pieces_v3.json")


TOP_FIELDS = ("ppu", "pivotSpace", "frameLayout", "layerOrder", "flip")
JSON_TYPES = {"int": lambda v: type(v) is int, "str": lambda v: isinstance(v, str),
              "list": lambda v: isinstance(v, list), "object": lambda v: isinstance(v, dict)}


def check_top(M, C, R):
    """QA A03-01: the top-level fields an importer reads must exist, have the contract type and equal the contract."""
    spec = C.get("manifestTop", {}).get("types", {})
    want = {"ppu": C.get("ppu"), "pivotSpace": C.get("pivotSpace"), "frameLayout": C.get("frameLayout"),
            "layerOrder": C.get("player", {}).get("layers"), "flip": C.get("flip")}
    for k in TOP_FIELDS:
        typ = spec.get(k)
        if typ not in JSON_TYPES or want[k] is None or not JSON_TYPES[typ](want[k]):
            R.fail("CONTRACT", f"contract has no usable type/value for manifest top-level '{k}'")
            continue
        if k not in M:
            R.fail("TOP", f"manifest top-level '{k}' is missing")
        elif not JSON_TYPES[typ](M[k]):
            R.fail("TOP", f"manifest top-level '{k}' must be {typ}, got {type(M[k]).__name__}")
        elif M[k] != want[k]:
            src = "contract player.layers" if k == "layerOrder" else f"contract {k}"
            R.fail("TOP", f"manifest top-level '{k}' is {M[k]!r}, {src} says {want[k]!r}")


def safe_rel(path, prefix):
    """relative, '/'-separated, no empty / '.' / '..' part, starts with prefix"""
    if not isinstance(path, str) or not path or path.startswith("/") or "\\" in path or ":" in path:
        return False
    if any(part in ("", ".", "..") for part in path.split("/")):
        return False
    return path.startswith(prefix)


def check_fonts(U, M, C, root, R):
    """QA A03-02: every font role and the licence notice must be the contract file, safe, really present and unchanged."""
    F = C.get("uiFonts")
    if not isinstance(F, dict) or not all(k in F for k in ("dir", "roles", "license", "source")):
        R.fail("CONTRACT", "contract has no uiFonts dir/roles/license/source")
        return
    if U is None:
        return                                   # check_ui already reported the missing ui data
    fonts = U.get("fonts")
    if not isinstance(fonts, dict):
        R.fail("FONT", "ui-pieces has no fonts object")
        return

    def real_file(label, path, want_sha=None):
        if not safe_rel(path, F["dir"]):
            R.fail("FONT", f"{label}: path {path!r} is not a safe relative path under {F['dir']}")
            return
        p = os.path.join(root, *path.split("/"))
        if os.path.islink(p) or not os.path.isfile(p):
            R.fail("FONT", f"{label}: file missing: {path}")
        elif want_sha is not None and sha(p) != want_sha:
            R.fail("FONT", f"{label}: {path} bytes differ from the contract sha256")

    for role, want in F["roles"].items():
        f = fonts.get(role)
        if not isinstance(f, dict):
            R.fail("FONT", f"font role '{role}' missing from ui-pieces fonts")
            continue
        for k in ("file", "pixelSize", "sha256"):
            if f.get(k) != want[k]:
                R.fail("FONT", f"font {role}: {k} is {f.get(k)!r}, contract says {want[k]!r}")
        real_file(f"font {role}", f.get("file"), want["sha256"])
    for k, f in fonts.items():                   # any other entry that names a file must also be safe and present
        if k not in F["roles"] and isinstance(f, dict) and "file" in f and k != "license":
            real_file(f"font {k}", f["file"])
    wl, lic = F["license"], fonts.get("license")
    if not isinstance(lic, dict):
        R.fail("FONT", f"fonts.license must be an object with file/name, got {type(lic).__name__}")
    else:
        for k in ("file", "name"):
            if lic.get(k) != wl[k]:
                R.fail("FONT", f"fonts.license {k} is {lic.get(k)!r}, contract says {wl[k]!r}")
        real_file("font licence notice", lic.get("file"), wl["sha256"])
    if fonts.get("source") != F["source"]:
        R.fail("FONT", f"fonts.source {fonts.get('source')!r} differs from contract uiFonts.source")
    ml = M.get("license", {}).get("fonts") if isinstance(M.get("license"), dict) else None
    if not isinstance(ml, dict) or ml.get("notice") != wl["file"] or ml.get("license") != wl["name"] \
            or ml.get("source") != F["source"]:
        R.fail("FONT", "manifest license.fonts notice/license/source differ from contract uiFonts")


def check_catalog(by_id, path, R):
    cat = load_json(path, R, "CATALOG")
    if cat is None:
        return
    live = {"weapon": {w["weapon_id"] for w in cat.get("weapon_unlocks", [])},
            "monster": {m["id"] for m in cat.get("monsters", [])},
            "passive": {p["passive_id"] for p in cat.get("evolution_pairs", [])}}
    for aid, e in by_id.items():
        kind, _, rid = e["ref"].partition(":")
        if kind in live and rid not in live[kind]:
            R.fail("CATALOG", f"{aid}: {kind} '{rid}' not found in {path}")


def finish(R, quiet, n=0):
    if R.errors:
        print(f"FAIL: {len(R.errors)} problem(s) ({R.checked}/{n} assets passed file-level checks)")
        for line in R.errors[: (40 if quiet else 400)]:
            print("  " + line)
        return 1
    print(f"PASS: {R.checked}/{n} assets, manifest top level, contract, player layers, pixel-source, tiles, ui, fonts")
    return 0


if __name__ == "__main__":
    sys.exit(main())
