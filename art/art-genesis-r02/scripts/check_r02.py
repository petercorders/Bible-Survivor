#!/usr/bin/env python3
"""Contract checker for the R02 enemy sidecar (standard library only).

Usage:  python3 -I -S -B scripts/check_r02.py [--root DIR] [--art03 DIR] [--font-root DIR] [--catalog FILE] [--quiet]
  --art03      art 03 package (default ../art-concept-03): its manifest / contract sha and the shadow PNGs are checked
  --font-root  outline font sidecar (default ../art-unity-font-01): review-board fonts must exist with the contract sha
  --catalog    content/genesis/catalog.json of the game repo: sha and monster ids b1-b3 (optional; reported if not run)

Order (support policy r02-support-1; iterations/r02-01-fix/ITERATION.md lists every value and why):
  1 shape    src/contract_r02.json, export/manifest_r02.json and export/source/pixel-source_r02.json are read as typed
             JSON before anything else: required keys, no unknown keys (closed schema; only free-text 'note'/'why' keys
             are optional), JSON types (a boolean is never 0/1, 32.0 is not the integer 32), schema name + schemaVersion.
  2 support  the CONTRACT ITSELF must use the values this package's scripts, this checker and the Unity hand-off
             support: PPU 32 = importer.ppu, the importer settings, bottom-left pixel-edge pivot space with y up, one
             horizontal strip per animation without padding, art facing right + one flipX on the parent, binary alpha,
             the art 03 sorting layers with enemies on Actors, the art 03 palette + the R02 ext palette, and the three
             R02 sheets of docs/art/r02-design-task.md (ids, 32x32 cells, 4 frames, 8/8/6 fps, loop, pivot [16,1]).
             Rebuilding the manifest from an unsupported contract does not make it pass. A contract that fails 1 or 2
             stops the check there: nothing is compared with an unsupported contract.
  3 hand-off the manifest equals the contract where it copies it, every declared file (contract, generator,
             helpers, pixel source, palettes) is a safe relative path to a regular file at its role path with the
             declared sha256, schema / license / provenance are present with the expected meaning, and every measured
             field (size, rects, alpha values, colours, frame bbox, sha256) equals the PNG; the PNGs equal the pixel
             source; grid rules (edge margin, foot row, no repeated neighbour frame).
  4 refs     art 03 manifest + contract sha, art 03 shadow PNGs, review fonts, catalog.
Error codes (one line each, '[CODE] message'):
  CONTRACT MANIFEST MISSING EXTRA PATH FILE HASH STALE PNG SIZE RECT PIVOT SPACE VALUE LAYER ALPHA COLOUR EMPTY EDGE
  FOOT HOLD SOURCE PALETTE REF FONT
Exit  0 PASS (first line 'PASS: ...'), 1 FAIL (first line 'FAIL: n problem(s)'), 2 usage error,
      3 checker bug (traceback printed; an exit 3 is never a detected problem).
This is an offline file check: Unity import, rendering and devices are NOT_RUN here."""
import argparse, hashlib, json, os, re, sys, traceback

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.dirname(HERE)
sys.path.insert(0, HERE)
import pngio
import convert_source

# ---------------------------------------------------------------------------------------------------------------
# Support policy r02-support-1. These values live in the checker on purpose: the contract is checked AGAINST them,
# so editing the contract (and rebuilding the manifest from it) cannot make an unsupported value pass.
POLICY = "r02-support-1"
CONTRACT_SCHEMA = ("bible-survivors.art-r02-contract", 1)
MANIFEST_SCHEMA = ("bible-survivors.art-manifest-r02", 1)
SOURCE_SCHEMA = ("bible-survivors.pixel-source", 3)
PPU = 32
# make_manifest.py / this checker compute foot row = cell.h - 1 - pivot.y and normalized = pivot / cell in exactly
# this space; Unity's custom pivot (0.5, 0.03125) is the same point. No other space is implemented anywhere.
PIVOT_SPACE = "pixel-edge coordinates, origin at the bottom-left corner of one frame cell, x right, y up"
FRAME_LAYOUT = "one horizontal strip per animation; frame i occupies x = i*cell.w .. (i+1)*cell.w, full height; no padding"
FLIP = {"artFaces": "right", "faceLeft": "flipX", "applyOn": "parent"}
IMPORTER = {"filterMode": "Point", "mipmaps": False, "compression": "none", "spriteMode": "multiple, one rect per frame",
            "ppu": PPU}
ALPHA_POLICY = "binary"
SORTING_LAYERS = ["Ground", "GroundDecal", "Pickups", "Actors", "WeaponFX", "FX", "UI"]   # art 03 order
ENEMY_LAYER = "Actors"
FACING = "right"
CELL = [PPU, PPU]
PIVOT = [CELL[0] // 2, 1]          # centred, foot row 30: every art 03 enemy; R02-01 QA measured it on B2/B3
COLOUR_LIMIT_MAX = 16
EDGE_MARGIN = 1
STATUS = ("production-draft",)
QUALITY = ("core",)
PALETTE_BASE = {"file": "src/palette_v03.json", "name": "master-v0.3", "groups": ["actor", "ui", "terrain"],
                "sha256": "636c0ba15ceafaf1de1339792c3fb44fb6ba72a349e6bf67a8848e0cfc9364f3"}
PALETTE_EXT = {"file": "src/palette_r02_ext.json", "groups": ["actor"]}   # what convert_source.load_palette reads
MANIFEST_FILE = "export/manifest_r02.json"
ROLE = {"contract": "src/contract_r02.json", "generator": "scripts/enemies_r02.py",
        "pixelSource": "export/source/pixel-source_r02.json", "helpers": ["scripts/px03.py", "scripts/pngio.py"]}
REFS = {"catalog": "content/genesis/catalog.json", "art03Manifest": "export/manifest_v3.json",
        "art03Contract": "src/contract_v3.json", "fontPackage": "art-unity-font-01"}
TASK = {  # docs/art/r02-design-task.md: transparent 32x32 x 4 horizontal sheets
    "enm_b1-envy-eye_move_32": {"ref": "monster:b1", "frames": 4, "fps": 8, "loop": True},
    "enm_b2-rage-hound_run_32": {"ref": "monster:b2", "frames": 4, "fps": 8, "loop": True},
    "enm_b3-cracked-mask_move_32": {"ref": "monster:b3", "frames": 4, "fps": 6, "loop": True}}
PROVENANCE_MUST_SAY = "not exported from the Claude Design product"

# ---------------------------------------------------------------------------------------------------------------
# JSON shapes. "str" = non-empty string, "text" = any string, "int" = JSON integer (not boolean, not 32.0),
# "bool" = JSON boolean, "num" = integer or fraction (not boolean), "sha" = 64 lowercase hex.
INT2 = ("list", "int", 2)
FILE_REF = ("obj", {"file": "str", "sha256": "sha"})
REFERENCES_S = ("obj", {
    "catalog": ("obj", {"repoFile": "str", "sha256": "sha", "monsters": ("map", "str")}),
    "art03": ("obj", {"manifest": "str", "sha256": "sha", "contract": "str", "contractSha256": "sha"}),
    "shadows": ("map", "sha")})
FLIP_S = ("obj", {"artFaces": "str", "faceLeft": "str", "applyOn": "str", "note": ("opt", "str")})
IMPORTER_S = ("obj", {"filterMode": "str", "mipmaps": "bool", "compression": "str", "spriteMode": "str", "ppu": "int"})
CONTRACT_S = ("obj", {
    "schema": "str", "schemaVersion": "int", "note": ("opt", "str"),
    "ppu": "int", "pivotSpace": "str", "frameLayout": "str", "flip": FLIP_S, "importer": IMPORTER_S,
    "alphaPolicies": ("map", "str"), "sortingLayers": ("list", "str", None),
    "frameChange": ("opt", ("obj", {"rule": "str"})), "colourLimit": "int",
    "edgeMargin": ("obj", {"px": "int", "why": ("opt", "str")}),
    "palette": ("obj", {
        "base": ("obj", {"file": "str", "name": "str", "sha256": "sha", "groups": ("list", "str", None)}),
        "ext": ("obj", {"file": "str", "name": "str", "groups": ("list", "str", None), "rampsMustNotCollide": "bool"})}),
    "references": REFERENCES_S,
    "reviewFonts": ("obj", {"note": ("opt", "str"), "package": "str", "files": ("map", "sha")}),
    "assets": ("list", ("obj", {"id": "str", "ref": "str", "cell": INT2, "frames": "int", "fps": "int", "loop": "bool",
                                 "pivot": INT2, "ground": "bool", "alpha": "str", "sorting": "str", "quality": "str",
                                 "shadow": "str", "facing": "str"}), None)})
ASSET_S = ("obj", {
    "id": "str", "file": "str", "sha256": "sha", "size": INT2, "cell": INT2, "frames": "int",
    "frameRects": ("list", ("list", "int", 4), None), "fps": "int", "loop": "bool",
    "pivot": ("obj", {"px": INT2, "normalized": ("list", "num", 2)}), "ground": "bool", "facing": "str",
    "alpha": ("obj", {"policy": "str", "values": ("list", "int", None)}), "sorting": "str", "quality": "str",
    "ref": "str", "shadow": ("obj", {"id": "str", "from": "str", "sha256": "sha"}), "colours": "int",
    "frameBBoxTopLeft": ("list", ("list", "int", 4), None),
    "source": ("obj", {"authoring": "str", "method": "str", "pixelSource": "str"}), "status": "str"})
MANIFEST_S = ("obj", {
    "schema": "str", "schemaVersion": "int", "status": "str", "statusNote": "str",
    "ppu": "int", "pivotSpace": "str", "frameLayout": "str", "flip": FLIP_S, "importer": IMPORTER_S,
    "palette": ("obj", {"base": ("obj", {"file": "str", "name": "str", "sha256": "sha"}),
                        "ext": ("obj", {"file": "str", "name": "str", "sha256": "sha"})}),
    "inputs": ("obj", {"contract": FILE_REF, "generator": FILE_REF, "helpers": ("map", "sha")}),
    "data": ("obj", {"pixelSource": FILE_REF}),
    "references": REFERENCES_S,
    "build": ("obj", {"command": "str", "testedWith": ("map", "str"), "png": "str"}),
    "provenance": ("obj", {"pixels": "str", "designCanvas": "str"}),
    "license": ("obj", {"status": "str", "references": ("list", "str", None), "art": "str", "reviewFonts": "str"}),
    "assets": ("list", ASSET_S, None)})
SOURCE_S = ("obj", {
    "schema": "str", "schemaVersion": "int", "status": "str",
    "palette": ("obj", {"base": "str", "ext": "str"}), "rules": ("map", "str"),
    "sprites": ("list", ("obj", {"name": "str", "cell": INT2, "pivot_px": INT2, "fps": "int", "loop": "bool",
                                  "alpha": "str", "keys": ("map", "str"),
                                  "frames": ("list", ("list", "text", None), None)}), None)})


class _Missing:
    def __repr__(self):
        return "<missing>"


MISSING = _Missing()


class Report:
    def __init__(self):
        self.errors = []

    def fail(self, code, msg):
        self.errors.append(f"[{code}] {msg}")


def sha_bytes(b):
    return hashlib.sha256(b).hexdigest()


def kind(v):
    return {type(None): "null", bool: "boolean", int: "integer", float: "number", str: "string", list: "array",
            dict: "object"}.get(type(v), type(v).__name__)


def short(v):
    return json.dumps(v, ensure_ascii=False, default=repr)[:70]     # MISSING (also nested) prints as <missing>


def same(a, b):
    """JSON equality that keeps JSON types apart: True != 1, 0 != false, 32.0 != 32."""
    if type(a) is not type(b):
        return False
    if type(a) is dict:
        return a.keys() == b.keys() and all(same(a[k], b[k]) for k in a)
    if type(a) is list:
        return len(a) == len(b) and all(same(x, y) for x, y in zip(a, b))
    return a == b


def dig(o, *keys):
    """o[k1][k2]... or MISSING when any step is absent or of the wrong type (never raises)."""
    for k in keys:
        if type(o) is dict and type(k) is str and k in o:
            o = o[k]
        elif type(o) is list and type(k) is int and 0 <= k < len(o):
            o = o[k]
        else:
            return MISSING
    return o


SCALARS = {"str": (lambda v: type(v) is str and v.strip() != "", "a non-empty string"),
           "text": (lambda v: type(v) is str, "a string"),
           "int": (lambda v: type(v) is int, "a JSON integer (not a boolean, not a fraction like 32.0)"),
           "bool": (lambda v: type(v) is bool, "a JSON boolean (not 0/1)"),
           "num": (lambda v: type(v) in (int, float), "a number"),
           "sha": (lambda v: type(v) is str and re.fullmatch(r"[0-9a-f]{64}", v) is not None, "a sha256 (64 lowercase hex)")}


def shape(v, spec, path, out):
    """Append 'path: problem' lines to out; True when v has the JSON shape spec."""
    if type(spec) is str:
        test, want = SCALARS[spec]
        if not test(v):
            out.append(f"{path} must be {want}, got {kind(v)} {short(v)}")
            return False
        return True
    tag = spec[0]
    if tag == "list":
        item, n = spec[1], spec[2]
        if type(v) is not list:
            out.append(f"{path} must be an array, got {kind(v)} {short(v)}")
            return False
        ok = True
        if n is not None and len(v) != n:
            out.append(f"{path} must have {n} items, has {len(v)}")
            ok = False
        for i, x in enumerate(v):
            ok = shape(x, item, f"{path}[{i}]", out) and ok
        return ok
    if tag == "map":
        if type(v) is not dict:
            out.append(f"{path} must be an object, got {kind(v)} {short(v)}")
            return False
        ok = True
        for k, x in v.items():
            ok = shape(x, spec[1], f"{path}.{k}", out) and ok
        return ok
    if tag == "obj":
        fields = spec[1]
        if type(v) is not dict:
            out.append(f"{path} must be an object, got {kind(v)} {short(v)}")
            return False
        ok = True
        for k, s in fields.items():
            optional = type(s) is tuple and s[0] == "opt"
            if k not in v:
                if not optional:
                    out.append(f"{path} has no '{k}'")
                    ok = False
                continue
            ok = shape(v[k], s[1] if optional else s, f"{path}.{k}", out) and ok
        for k in v:
            if k not in fields:
                out.append(f"{path} has unknown key '{k}' (closed schema {POLICY})")
                ok = False
        return ok
    raise ValueError(f"bad shape spec {spec!r}")


def check_shape(R, code, doc, spec, name):
    out = []
    ok = shape(doc, spec, name, out)
    for x in out:
        R.fail(code, x)
    return ok


def safe_rel(rel, prefix=""):
    """relative, '/' separated, under prefix, no '.', '..' or empty part, no backslash or drive"""
    if type(rel) is not str or not rel or rel.startswith("/") or "\\" in rel or ":" in rel:
        return False
    parts = rel.split("/")
    if any(p in ("", ".", "..") for p in parts):
        return False
    return rel.startswith(prefix)


def read_regular(root, rel, R, what):
    p = os.path.join(root, *rel.split("/"))
    real_root = os.path.realpath(root)
    if os.path.islink(p) or (os.path.exists(p) and not os.path.isfile(p)):
        R.fail("PATH", f"{what}: {rel} is not a regular file")
        return None
    if not os.path.isfile(p):
        R.fail("FILE", f"{what}: file missing: {rel}")
        return None
    if not os.path.realpath(p).startswith(real_root + os.sep):
        R.fail("PATH", f"{what}: {rel} resolves outside the package")
        return None
    with open(p, "rb") as f:
        return f.read()


def load_json(root, rel, R, code):
    data = read_regular(root, rel, R, code.lower())
    if data is None:
        return None, None
    try:
        return json.loads(data.decode("utf-8")), data
    except (UnicodeDecodeError, ValueError) as e:
        R.fail(code, f"{rel}: not readable JSON ({e})")
        return None, data


def load_ext_json(path):
    """JSON from another package (art 03, catalog): (value, bytes) or (None, bytes/None); never raises."""
    if not os.path.isfile(path):
        return None, None
    with open(path, "rb") as f:
        b = f.read()
    try:
        return json.loads(b.decode("utf-8")), b
    except (UnicodeDecodeError, ValueError):
        return None, b


def frames_of(buf, w, h, cw, n):
    return [b"".join(bytes(buf[(y * w + k * cw) * 4:(y * w + (k + 1) * cw) * 4]) for y in range(h)) for k in range(n)]


# --- 2. support policy for the contract itself -------------------------------------------------------------------
def check_support(C, R):
    n0 = len(R.errors)
    if not same(dig(C, "schema"), CONTRACT_SCHEMA[0]) or not same(dig(C, "schemaVersion"), CONTRACT_SCHEMA[1]):
        R.fail("CONTRACT", f"contract schema {short(dig(C, 'schema'))} v{short(dig(C, 'schemaVersion'))} is not "
                           f"{CONTRACT_SCHEMA[0]} v{CONTRACT_SCHEMA[1]}, the only contract this checker reads")
    if not same(dig(C, "ppu"), PPU):
        R.fail("SPACE", f"contract ppu {short(dig(C, 'ppu'))} is not supported: the package, its 32 px cells and the "
                        f"Unity hand-off use PPU {PPU} (one 32 px cell = one world unit)")
    if not same(dig(C, "ppu"), dig(C, "importer", "ppu")):
        R.fail("SPACE", f"contract ppu {short(dig(C, 'ppu'))} != importer.ppu {short(dig(C, 'importer', 'ppu'))}")
    if not same(dig(C, "importer"), IMPORTER):
        R.fail("SPACE", f"contract importer {short(dig(C, 'importer'))} is not the supported importer {short(IMPORTER)}")
    if not same(dig(C, "pivotSpace"), PIVOT_SPACE):
        R.fail("SPACE", f"contract pivotSpace {short(dig(C, 'pivotSpace'))} is not supported: foot row, normalized "
                        f"pivot and the Unity custom pivot are computed only in '{PIVOT_SPACE}'")
    if not same(dig(C, "frameLayout"), FRAME_LAYOUT):
        R.fail("SPACE", f"contract frameLayout {short(dig(C, 'frameLayout'))} is not supported: the converter, the "
                        f"manifest rects and this checker read only '{FRAME_LAYOUT}'")
    if not same({k: dig(C, "flip", k) for k in FLIP}, FLIP):
        R.fail("SPACE", f"contract flip {short(dig(C, 'flip'))} is not supported: art faces right and the enemy's "
                        f"parent applies flipX once ({short(FLIP)})")
    ap = dig(C, "alphaPolicies")
    if type(ap) is not dict or list(ap) != [ALPHA_POLICY]:
        R.fail("ALPHA", f"contract alphaPolicies {short(ap)}: only '{ALPHA_POLICY}' is supported")
    if not same(dig(C, "sortingLayers"), SORTING_LAYERS):
        R.fail("LAYER", f"contract sortingLayers {short(dig(C, 'sortingLayers'))} != art 03 layers {SORTING_LAYERS}")
    cl = dig(C, "colourLimit")
    if type(cl) is not int or not 1 <= cl <= COLOUR_LIMIT_MAX:
        R.fail("COLOUR", f"contract colourLimit {short(cl)} must be an integer 1..{COLOUR_LIMIT_MAX}")
    if not same(dig(C, "edgeMargin", "px"), EDGE_MARGIN):
        R.fail("EDGE", f"contract edgeMargin.px {short(dig(C, 'edgeMargin', 'px'))} != supported {EDGE_MARGIN}")
    base = {k: dig(C, "palette", "base", k) for k in PALETTE_BASE}
    if not same(base, PALETTE_BASE):
        R.fail("PALETTE", f"contract palette.base {short(base)} is not the art 03 master palette {short(PALETTE_BASE)}")
    ext = {k: dig(C, "palette", "ext", k) for k in PALETTE_EXT}
    if not same(ext, PALETTE_EXT) or not same(dig(C, "palette", "ext", "rampsMustNotCollide"), True):
        R.fail("PALETTE", f"contract palette.ext {short(dig(C, 'palette', 'ext'))} is not {short(PALETTE_EXT)} with "
                          f"rampsMustNotCollide true (what scripts/convert_source.py loads)")
    if not same(dig(C, "references", "catalog", "repoFile"), REFS["catalog"]):
        R.fail("REF", f"contract references.catalog.repoFile must be {REFS['catalog']}")
    monsters = dig(C, "references", "catalog", "monsters")
    want_m = sorted(t["ref"].split(":", 1)[1] for t in TASK.values())
    if type(monsters) is not dict or sorted(monsters) != want_m:
        R.fail("REF", f"contract catalog monsters {short(monsters)} must be exactly {want_m}")
    for k, want in (("manifest", REFS["art03Manifest"]), ("contract", REFS["art03Contract"])):
        if not same(dig(C, "references", "art03", k), want):
            R.fail("REF", f"contract references.art03.{k} must be {want}")
    if not same(dig(C, "reviewFonts", "package"), REFS["fontPackage"]):
        R.fail("FONT", f"contract reviewFonts.package must be {REFS['fontPackage']}")
    files = dig(C, "reviewFonts", "files")
    if type(files) is not dict or not files:
        R.fail("FONT", "contract reviewFonts.files must list at least one font")
    assets = dig(C, "assets")
    ids = [x for x in (dig(c, "id") for c in assets) if type(x) is str] if type(assets) is list else []
    for aid in sorted(set(TASK) - set(ids)):
        R.fail("CONTRACT", f"contract has no asset {aid} (docs/art/r02-design-task.md)")
    for aid in sorted(set(ids) - set(TASK)):
        R.fail("CONTRACT", f"contract asset {aid} is not one of the R02 task sheets {sorted(TASK)}")
    for aid in sorted({x for x in ids if ids.count(x) > 1}):
        R.fail("CONTRACT", f"contract lists asset {aid} more than once")
    shadows = dig(C, "references", "shadows")
    for c in assets if type(assets) is list else []:
        aid = dig(c, "id")
        t = TASK.get(aid) if type(aid) is str else None
        if t is None:
            continue
        for k, code in (("ref", "VALUE"), ("frames", "SIZE"), ("fps", "VALUE"), ("loop", "VALUE")):
            if not same(dig(c, k), t[k]):
                R.fail(code, f"contract {aid}.{k} {short(dig(c, k))} != task {short(t[k])} (docs/art/r02-design-task.md)")
        for k, want, code in (("cell", CELL, "SIZE"), ("pivot", PIVOT, "PIVOT"), ("facing", FACING, "SPACE"),
                              ("alpha", ALPHA_POLICY, "ALPHA"), ("sorting", ENEMY_LAYER, "LAYER")):
            if not same(dig(c, k), want):
                R.fail(code, f"contract {aid}.{k} {short(dig(c, k))} is not supported (must be {short(want)})")
        if dig(c, "quality") not in QUALITY:
            R.fail("VALUE", f"contract {aid}.quality {short(dig(c, 'quality'))} must be one of {list(QUALITY)}")
        if type(shadows) is not dict or dig(c, "shadow") not in shadows:
            R.fail("REF", f"contract {aid}.shadow {short(dig(c, 'shadow'))} is not one of the referenced art 03 shadows")
    return len(R.errors) == n0


# --- 3. declared files --------------------------------------------------------------------------------------------
def check_declared(root, R, label, node, role_path, mismatch_code, what):
    """manifest node {file, sha256}: safe relative path, the role's path, a regular file, the declared sha256."""
    f, s = dig(node, "file"), dig(node, "sha256")
    if type(f) is not str:                            # also a shape error; never return silently
        R.fail("MANIFEST", f"{label}.file {short(f)} is not a path, so the {what} is not checked")
        return None
    if not safe_rel(f):
        R.fail("PATH", f"{label}.file '{f}' is not a safe relative path inside the package")
        return None
    if f != role_path:
        R.fail("PATH", f"{label}.file '{f}' is not the {what} ({role_path})")
    b = read_regular(root, f, R, f"{label}.file")
    if b is not None and type(s) is str and sha_bytes(b) != s:
        R.fail(mismatch_code, f"{label}.sha256 does not match {f}" +
               ("; run scripts/build_r02.py" if mismatch_code == "STALE" else ""))
    return b


def check_manifest_top(root, C, M, ext_name, R):
    if not same(dig(M, "schema"), MANIFEST_SCHEMA[0]) or not same(dig(M, "schemaVersion"), MANIFEST_SCHEMA[1]):
        R.fail("MANIFEST", f"manifest schema {short(dig(M, 'schema'))} v{short(dig(M, 'schemaVersion'))} is not "
                           f"{MANIFEST_SCHEMA[0]} v{MANIFEST_SCHEMA[1]}")
    if dig(M, "status") not in STATUS:
        R.fail("VALUE", f"manifest status {short(dig(M, 'status'))} must be one of {list(STATUS)}")
    for k in ("ppu", "pivotSpace", "frameLayout", "flip", "importer"):
        if not same(dig(M, k), C[k]):
            R.fail("SPACE", f"manifest {k} {short(dig(M, k))} != contract")
    if not same(dig(M, "references"), C["references"]):
        R.fail("REF", "manifest references differ from the contract references")
    if not same(dig(M, "license", "status"), "original") or not same(dig(M, "license", "references"), []):
        R.fail("VALUE", f"manifest license {short(dig(M, 'license'))}: status must be 'original' with no references "
                        "(every PNG is made by this folder's scripts)")
    pix = dig(M, "provenance", "pixels")
    if type(pix) is not str or ROLE["generator"] not in pix or PROVENANCE_MUST_SAY not in pix:
        R.fail("VALUE", f"manifest provenance.pixels must name {ROLE['generator']} and say '{PROVENANCE_MUST_SAY}'")
    check_declared(root, R, "manifest inputs.contract", dig(M, "inputs", "contract"), ROLE["contract"], "STALE", "contract")
    gen = check_declared(root, R, "manifest inputs.generator", dig(M, "inputs", "generator"), ROLE["generator"],
                         "STALE", "generator")
    helpers = dig(M, "inputs", "helpers")
    if type(helpers) is dict:
        if sorted(helpers) != sorted(ROLE["helpers"]):
            R.fail("PATH", f"manifest inputs.helpers {sorted(helpers)} must be exactly {ROLE['helpers']}")
        for rel in ROLE["helpers"]:
            b = read_regular(root, rel, R, "helper")
            if b is not None and helpers.get(rel) != sha_bytes(b):
                R.fail("STALE", f"{rel} changed after the manifest was built")
    src = check_declared(root, R, "manifest data.pixelSource", dig(M, "data", "pixelSource"), ROLE["pixelSource"],
                         "HASH", "pixel source")
    for side, role in (("base", C["palette"]["base"]["file"]), ("ext", C["palette"]["ext"]["file"])):
        check_declared(root, R, f"manifest palette.{side}", dig(M, "palette", side), role, "HASH", f"{side} palette")
    if not same(dig(M, "palette", "base", "name"), C["palette"]["base"]["name"]):
        R.fail("PALETTE", f"manifest palette.base.name {short(dig(M, 'palette', 'base', 'name'))} != contract")
    if not same(dig(M, "palette", "ext", "name"), C["palette"]["ext"]["name"]) or ext_name != C["palette"]["ext"]["name"]:
        R.fail("PALETTE", f"palette.ext name: manifest {short(dig(M, 'palette', 'ext', 'name'))}, contract "
                          f"{short(C['palette']['ext']['name'])}, file {short(ext_name)} must agree")
    return gen, src


def check_palette(root, C, R):
    base_b = read_regular(root, C["palette"]["base"]["file"], R, "palette base")
    if base_b is not None and sha_bytes(base_b) != C["palette"]["base"]["sha256"]:
        R.fail("PALETTE", f"{C['palette']['base']['file']} is not the art 03 master palette (sha256 differs)")
    ext, _ = load_json(root, C["palette"]["ext"]["file"], R, "PALETTE")
    try:
        pal = convert_source.load_palette(root)
    except (OSError, ValueError, KeyError, TypeError, AttributeError, IndexError) as e:
        R.fail("PALETTE", f"palette not usable: {e}")
        return None, dig(ext, "name")
    return pal, dig(ext, "name")


def source_identity(sprites, R):
    """R02-04: every R02 sheet exactly once in the pixel source, checked BEFORE any name map is built.
    A map keeps only the last of two same-named sprites, so the earlier one would never be compared with the PNG
    (QA: a duplicate B1 with a 31-character row in front of the good B1 passed while convert_source.py failed).
    Returns the names that occur exactly once; only those go into the map. Order is not identity."""
    seen = {}
    for i, sp in enumerate(sprites):
        name = dig(sp, "name")
        if type(name) is str:                          # other types are shape errors, reported there
            seen.setdefault(name, []).append(i)
    for name in sorted(TASK):
        idx = seen.get(name, [])
        if not idx:
            R.fail("SOURCE", f"pixel source has no sprite {name}; each R02 sheet must appear exactly once")
        elif len(idx) > 1:
            R.fail("SOURCE", f"pixel source lists sprite {name} {len(idx)} times (sprites{idx}); each R02 sheet must "
                             "appear exactly once")
    for name in sorted(set(seen) - set(TASK)):
        R.fail("SOURCE", f"pixel source sprite {name} is not in the contract (sprites{seen[name]})")
    return {name for name, idx in seen.items() if len(idx) == 1}


def check_source_top(S, R):
    if not same(dig(S, "schema"), SOURCE_SCHEMA[0]) or not same(dig(S, "schemaVersion"), SOURCE_SCHEMA[1]):
        R.fail("SOURCE", f"pixel source schema {short(dig(S, 'schema'))} v{short(dig(S, 'schemaVersion'))} is not "
                         f"{SOURCE_SCHEMA[0]} v{SOURCE_SCHEMA[1]}")
    if dig(S, "status") not in STATUS:
        R.fail("SOURCE", f"pixel source status {short(dig(S, 'status'))} must be one of {list(STATUS)}")
    for side, role in (("base", PALETTE_BASE["file"]), ("ext", PALETTE_EXT["file"])):
        v = dig(S, "palette", side)
        if type(v) is not str or not v.startswith(role):
            R.fail("SOURCE", f"pixel source palette.{side} {short(v)} must name {role}")


def check_asset(root, c, e, C, M, gen_text, pal, src_by, R):
    aid = c["id"]
    cw, ch = c["cell"]
    n = c["frames"]
    if not same(dig(e, "cell"), c["cell"]) or not same(dig(e, "frames"), n):
        R.fail("SIZE", f"{aid}: manifest cell/frames {short(dig(e, 'cell'))}/{short(dig(e, 'frames'))} != contract {c['cell']}/{n}")
    for k in ("fps", "loop", "ground", "facing", "quality", "ref"):
        if not same(dig(e, k), c[k]):
            R.fail("VALUE", f"{aid}: {k} {short(dig(e, k))} != contract {short(c[k])}")
    if not same(dig(e, "pivot", "px"), c["pivot"]):
        R.fail("PIVOT", f"{aid}: pivot {short(dig(e, 'pivot'))} != contract px {c['pivot']} ({PIVOT_SPACE})")
    elif not same(dig(e, "pivot", "normalized"), [c["pivot"][0] / cw, c["pivot"][1] / ch]):
        R.fail("PIVOT", f"{aid}: normalized pivot {short(dig(e, 'pivot', 'normalized'))} != px / cell")
    if not same(dig(e, "sorting"), c["sorting"]):
        R.fail("LAYER", f"{aid}: sorting layer {short(dig(e, 'sorting'))} != contract {c['sorting']!r}")
    if not (same(dig(e, "shadow", "id"), c["shadow"])
            and same(dig(e, "shadow", "sha256"), C["references"]["shadows"].get(c["shadow"]))):
        R.fail("REF", f"{aid}: shadow {short(dig(e, 'shadow'))} != contract {c['shadow']} with its art 03 sha256")
    if not same(dig(e, "status"), dig(M, "status")):
        R.fail("VALUE", f"{aid}: status {short(dig(e, 'status'))} != manifest status {short(dig(M, 'status'))}")
    if not same(dig(e, "source", "pixelSource"), f"{ROLE['pixelSource']}#{aid}"):
        R.fail("SOURCE", f"{aid}: source.pixelSource {short(dig(e, 'source', 'pixelSource'))} does not point at this "
                         f"sprite in {ROLE['pixelSource']}")
    auth = dig(e, "source", "authoring")
    fn = auth[len(ROLE["generator"]) + 1:] if type(auth) is str and auth.startswith(ROLE["generator"] + ":") else ""
    if not re.fullmatch(r"[A-Za-z_][A-Za-z0-9_]*", fn) or (gen_text is not None and f"\ndef {fn}(" not in gen_text):
        R.fail("SOURCE", f"{aid}: source.authoring {short(auth)} must be {ROLE['generator']}:<function defined there>")
    rel = dig(e, "file")
    if type(rel) is not str:                           # also a shape error; never return silently
        R.fail("MANIFEST", f"{aid}: file {short(rel)} is not a path, so the PNG is not checked")
        return
    if not safe_rel(rel, "export/png/") or not rel.endswith(".png"):
        R.fail("PATH", f"{aid}: file '{rel}' is not a safe relative path under export/png/")
        return
    if rel != f"export/png/{aid}.png":
        R.fail("PATH", f"{aid}: file '{rel}' does not match the stable id")
    data = read_regular(root, rel, R, aid)
    if data is None:
        return
    if not same(dig(e, "sha256"), sha_bytes(data)):
        R.fail("HASH", f"{aid}: {rel} bytes differ from the manifest sha256")
    try:
        w, h, buf = pngio.read_rgba(os.path.join(root, *rel.split("/")))
    except (ValueError, OSError) as ex:
        R.fail("PNG", f"{aid}: {ex}")
        return
    if [w, h] != [cw * n, ch]:
        R.fail("SIZE", f"{aid}: PNG is {w}x{h}, frame layout needs {cw * n}x{ch} ({n} frames of {cw}x{ch} side by side)")
        return
    if not same(dig(e, "size"), [w, h]):
        R.fail("SIZE", f"{aid}: manifest size {short(dig(e, 'size'))} != PNG {[w, h]}")
    if not same(dig(e, "frameRects"), [[k * cw, 0, cw, ch] for k in range(n)]):
        R.fail("RECT", f"{aid}: frameRects {short(dig(e, 'frameRects'))} != horizontal strip layout of {n} x {cw}x{ch}")
    if not (0 <= c["pivot"][0] <= cw and 0 <= c["pivot"][1] <= ch):
        R.fail("PIVOT", f"{aid}: contract pivot {c['pivot']} lies outside the cell")
    # pixels
    alphas, used, bad_rgb0, bad_col = set(), set(), 0, 0
    palset = set(pal.values()) if pal else None
    for i in range(0, len(buf), 4):
        px_ = tuple(buf[i:i + 4])
        alphas.add(px_[3])
        if px_[3] == 0:
            if px_[:3] != (0, 0, 0):
                bad_rgb0 += 1
            continue
        used.add(px_)
        if palset is not None and px_ not in palset:
            bad_col += 1
    if alphas - {0, 255}:
        R.fail("ALPHA", f"{aid}: alpha values {sorted(alphas - {0, 255})} break the binary policy")
    if bad_rgb0:
        R.fail("ALPHA", f"{aid}: {bad_rgb0} fully transparent pixels with non-zero RGB")
    if not same(dig(e, "alpha", "policy"), c["alpha"]) or not same(dig(e, "alpha", "values"), sorted(alphas)):
        R.fail("ALPHA", f"{aid}: manifest alpha {short(dig(e, 'alpha'))} != policy {c['alpha']} / PNG values {sorted(alphas)}")
    if bad_col:
        R.fail("COLOUR", f"{aid}: {bad_col} pixels use colours that are not palette entries")
    if len(used) > C["colourLimit"]:
        R.fail("COLOUR", f"{aid}: {len(used)} colours > {C['colourLimit']}")
    if not same(dig(e, "colours"), len(used)):
        R.fail("COLOUR", f"{aid}: manifest colours {short(dig(e, 'colours'))} != PNG {len(used)}")
    margin = C["edgeMargin"]["px"]
    foot = ch - 1 - c["pivot"][1]                      # bottom-left pixel-edge pivot -> top-left row index
    bboxes = []
    for k in range(n):
        rows = [y for y in range(h) for x in range(k * cw, (k + 1) * cw) if buf[(y * w + x) * 4 + 3]]
        cols = [x - k * cw for y in range(h) for x in range(k * cw, (k + 1) * cw) if buf[(y * w + x) * 4 + 3]]
        if not rows:
            R.fail("EMPTY", f"{aid} frame {k}: no opaque pixel")
            bboxes.append(None)
            continue
        bboxes.append([min(cols), min(rows), max(cols), max(rows)])
        if min(rows) < margin or max(rows) > ch - 1 - margin or min(cols) < margin or max(cols) > cw - 1 - margin:
            R.fail("EDGE", f"{aid} frame {k}: opaque pixel within {margin} px of the cell edge (rows {min(rows)}-{max(rows)}, cols {min(cols)}-{max(cols)})")
        if c["ground"] and max(rows) != foot:
            R.fail("FOOT", f"{aid} frame {k}: lowest opaque row {max(rows)} != foot row {foot}")
    if not same(dig(e, "frameBBoxTopLeft"), bboxes):
        R.fail("VALUE", f"{aid}: frameBBoxTopLeft {short(dig(e, 'frameBBoxTopLeft'))} != measured opaque bbox {bboxes} "
                        "(x0, y0, x1, y1 inclusive, top-left origin)")
    if c["fps"] > 0 and n > 1:
        fr = frames_of(buf, w, h, cw, n)
        for x0, x1 in [(k, k + 1) for k in range(n - 1)] + ([(n - 1, 0)] if c["loop"] else []):
            if fr[x0] == fr[x1]:
                R.fail("HOLD", f"{aid}: frame {x1} repeats frame {x0}")
    # pixel source <-> PNG
    sp = src_by.get(aid)
    if sp is None:
        R.fail("SOURCE", f"{aid}: no single well-formed pixel source sprite to compare the PNG with "
                         "(missing, listed more than once or malformed; see the pixel source lines)")
    elif pal is not None:
        if not (same(sp["cell"], c["cell"]) and len(sp["frames"]) == n and same(sp["pivot_px"], c["pivot"])
                and same(sp["fps"], c["fps"]) and same(sp["loop"], c["loop"]) and same(sp["alpha"], c["alpha"])):
            R.fail("SOURCE", f"{aid}: pixel source cell/frames/pivot/fps/loop/alpha differ from the contract")
        try:
            sw, shh, sbuf = convert_source.sprite_bytes(sp, pal)
            if (sw, shh) != (w, h) or sbuf != buf:
                diff = sum(1 for i in range(0, min(len(sbuf), len(buf)), 4) if sbuf[i:i + 4] != buf[i:i + 4])
                R.fail("SOURCE", f"{aid}: PNG pixels differ from the pixel source ({diff} pixels)")
        except (ValueError, KeyError, TypeError) as ex:
            R.fail("SOURCE", f"{aid}: pixel source grid is malformed: {ex}")


def check_refs(C, art03, font_root, catalog, R):
    ran = []
    ref03 = C["references"]["art03"]
    m03, b = load_ext_json(os.path.join(art03, *ref03["manifest"].split("/")))
    if b is None:
        R.fail("REF", f"art 03 manifest not found under {os.path.basename(art03)} (use --art03)")
    else:
        if sha_bytes(b) != ref03["sha256"]:
            R.fail("REF", "art 03 manifest sha256 differs from the reviewed one in the contract")
        _, cb = load_ext_json(os.path.join(art03, *ref03["contract"].split("/")))
        if cb is None or sha_bytes(cb) != ref03["contractSha256"]:
            R.fail("REF", f"art 03 contract {ref03['contract']} missing or sha256 differs from the contract")
        a03 = dig(m03, "assets")
        by03 = {x["id"]: x for x in a03 if type(x) is dict and type(x.get("id")) is str} if type(a03) is list else {}
        for sid, ssha in C["references"]["shadows"].items():
            x = by03.get(sid)
            f = dig(x, "file")
            if x is None or not safe_rel(f, "export/png/"):
                R.fail("REF", f"shadow {sid} is not an art 03 asset with a safe file path")
                continue
            p = os.path.join(art03, *f.split("/"))
            if not os.path.isfile(p) or sha_bytes(open(p, "rb").read()) != ssha or dig(x, "sha256") != ssha:
                R.fail("REF", f"shadow {sid}: art 03 file missing or sha256 differs")
        ran.append("art03 refs")
    RF = C["reviewFonts"]
    for rel, fsha in RF["files"].items():
        if not safe_rel(rel, "fonts/"):
            R.fail("FONT", f"review font path '{rel}' is not a safe relative path")
            continue
        p = os.path.join(font_root, *rel.split("/"))
        if not os.path.isfile(p):
            R.fail("FONT", f"review font missing: {RF['package']}/{rel}")
        elif sha_bytes(open(p, "rb").read()) != fsha:
            R.fail("FONT", f"review font {RF['package']}/{rel}: sha256 differs from the contract")
    ran.append("review fonts")
    if catalog:
        cat = C["references"]["catalog"]
        doc, b = load_ext_json(catalog)
        if b is None:
            R.fail("REF", "catalog file not found")
        else:
            if sha_bytes(b) != cat["sha256"]:
                R.fail("REF", "catalog.json sha256 differs from the contract")
            mons = dig(doc, "monsters")
            ids = {m.get("id"): m.get("name_ko") for m in mons if type(m) is dict} if type(mons) is list else {}
            for mid, name in cat["monsters"].items():
                if ids.get(mid) != name:
                    R.fail("REF", f"catalog monster {mid} is {ids.get(mid)!r}, contract says {name!r}")
            ran.append("catalog")
    return ran


def run(a):
    root = os.path.abspath(a.root)
    parent = os.path.dirname(root)
    art03 = a.art03 or os.path.join(parent, "art-concept-03")
    font_root = a.font_root or os.path.join(parent, "art-unity-font-01")
    R = Report()
    C, c_bytes = load_json(root, ROLE["contract"], R, "CONTRACT")
    M, _ = load_json(root, MANIFEST_FILE, R, "MANIFEST")
    S, _ = load_json(root, ROLE["pixelSource"], R, "SOURCE")
    # 1 shape (all three files, so one run reports every malformed field). A partly malformed manifest is still
    #   compared field by field below (dig/same never raise); the shape errors already make the run FAIL.
    c_ok = C is not None and check_shape(R, "CONTRACT", C, CONTRACT_S, "contract")
    if M is not None:
        check_shape(R, "MANIFEST", M, MANIFEST_S, "manifest")
    src_by = {}
    if S is not None:
        sprites = dig(S, "sprites")
        if type(sprites) is not list:
            check_shape(R, "SOURCE", S, SOURCE_S, "pixel source")
        else:
            check_shape(R, "SOURCE", {k: v for k, v in S.items() if k != "sprites"} | {"sprites": []}, SOURCE_S, "pixel source")
            once = source_identity(sprites, R)
            for i, sp in enumerate(sprites):
                out = []
                if shape(sp, SOURCE_S[1]["sprites"][1], f"pixel source.sprites[{i}]", out) and sp["name"] in once:
                    src_by[sp["name"]] = sp            # only names that occur exactly once: nothing is overwritten
                for x in out:
                    R.fail("SOURCE", x)
            check_source_top(S, R)
    # 2 support policy of the contract itself; nothing is compared with a malformed or unsupported contract
    if C is None:
        return finish(R, "")
    supported = check_support(C, R)
    if M is not None and type(M) is not dict:          # also a shape error; never return silently
        R.fail("MANIFEST", "manifest is not a JSON object, so nothing is compared")
    if not c_ok or not supported or type(M) is not dict:
        return finish(R, "")
    # 3 hand-off: manifest top level, declared files, palette
    pal, ext_name = check_palette(root, C, R)
    gen_b, _ = check_manifest_top(root, C, M, ext_name, R)
    gen_text = gen_b.decode("utf-8", "replace") if gen_b is not None else None
    assets = dig(M, "assets")
    if type(assets) is not list:                       # also a shape error; never return silently
        R.fail("MANIFEST", f"manifest assets {short(assets)} is not a list, so no sheet is checked")
        return finish(R, "")
    by_id = {}
    for e in assets:
        aid = dig(e, "id")
        if type(aid) is not str:
            R.fail("MANIFEST", "an asset entry has no id")
            continue
        if aid in by_id:
            R.fail("MANIFEST", f"duplicate asset id {aid}")
        by_id[aid] = e
    for extra in sorted(set(by_id) - set(TASK)):
        R.fail("EXTRA", f"manifest asset {extra} is not in the contract")
    for c in C["assets"]:
        if c["id"] not in by_id:
            R.fail("MISSING", f"contract asset {c['id']} is not in the manifest")
            continue
        check_asset(root, c, by_id[c["id"]], C, M, gen_text, pal, src_by, R)
    # 4 references
    ran = check_refs(C, art03, font_root, a.catalog, R)
    note = (f"checked: contract shape + supported values ({POLICY}), manifest shape, declared files, pixel source, "
            f"PNG pixels, palette, " + ", ".join(ran))
    if not a.catalog:
        note += "; catalog NOT checked (no --catalog)"
    return finish(R, note)


def finish(R, note):
    if not R.errors and not note:                      # an early stop must come with a finding
        print("CHECKER BUG: the check stopped early without reporting why; exit 3 is not a contract result")
        return 3
    if R.errors:
        print(f"FAIL: {len(R.errors)} problem(s)")
        for x in R.errors:
            print(x)
        return 1
    print(f"PASS: R02 enemy sheets match the contract - {note}")
    return 0


def main(argv=None):
    ap = argparse.ArgumentParser(add_help=True)
    ap.add_argument("--root", default=ROOT)
    ap.add_argument("--art03")
    ap.add_argument("--font-root")
    ap.add_argument("--catalog")
    ap.add_argument("--quiet", action="store_true")
    try:
        a = ap.parse_args(argv)
    except SystemExit as ex:
        return 2 if ex.code else 0
    try:
        return run(a)
    except Exception:                                  # a checker bug must never look like a detected problem
        traceback.print_exc()
        print("CHECKER BUG: unexpected exception above; exit 3 is not a contract result")
        return 3


if __name__ == "__main__":
    sys.exit(main())
