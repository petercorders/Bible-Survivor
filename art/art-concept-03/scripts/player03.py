"""Player frames for art 03: places the hand-placed parts of src/player_rig_v3.json by its pose table.

Every layer of every frame is built with exactly the same placement numbers, so layers cannot drift apart.
Rotated death frames are re-grounded from the base layer alone and that one shift is applied to all layers.
"""
import json, math, os
import numpy as np
import px03 as px

RIG = json.load(open(os.path.join(px.ROOT, "src", "player_rig_v3.json"), encoding="utf-8"))
W, H = RIG["cell"]
KEYS = RIG["keys"]
REC = px.PALJ["recolor"]
PAD = 16                       # work canvas margin so rotations never clip
FOOT_ROW = H - 1 - RIG["pivot_px"][1]   # 30
GROUP_OF = {p: g for g, ps in RIG["groups"].items() for p in ps}
UPPER = {"head", "torso", "arm_b", "arm_f"}


def override(skin=None, hair=None, tunic=None, cloak=None):
    o = {}
    for grp, sel, table in (("skin", skin, REC["skin"]), ("hair", hair, REC["hair"]),
                            ("tunic", tunic, REC["dye"]), ("cloak", cloak, REC["dye"])):
        if sel:
            o.update({f"{grp}.{i}": px.hex_rgba(c) for i, c in enumerate(table[sel])})
    return o


def part_rows(name, fr):
    if name == "base.head" and fr.get("head"):
        return RIG["parts"]["base.head@" + fr["head"]]
    for leg in ("leg_l", "leg_r"):
        if name == "base." + leg:
            return RIG["parts"][f"base.{leg}@{fr.get(leg, 'stand')}"]
    return RIG["parts"][name]


def row_shift(name, fr):
    """per-row dx inside the part (before translation)."""
    sh = {}
    grp = GROUP_OF[name]
    if grp in ("arm_b", "arm_f") and fr.get(grp, {}).get("hand"):
        for y in range(21, 24):
            sh[y] = fr[grp]["hand"]
    if name == "cloak_back.cloak" and fr.get("cape", {}).get("hem"):
        hem = fr["cape"]["hem"]
        for y in range(19, 32):
            sh[y] = hem if y >= 22 else int(hem / 2)
    return sh


def place(name, fr):
    grp = GROUP_OF[name]
    dx = dy = 0
    if grp in UPPER:
        dx, dy = fr.get("body", [0, 0])
    if name == "cloak_back.cloak":
        dx += fr.get("cape", {}).get("dx", 0)
        dy += fr.get("cape", {}).get("dy", 0)
    return dx, dy


def render_part(name, fr, ov):
    a = px.from_rows(part_rows(name, fr), KEYS, W, H, ov)
    sh = row_shift(name, fr)
    if sh:
        b = np.zeros_like(a)
        for y in range(H):
            d = sh.get(y, 0)
            if d > 0:
                b[y, d:] = a[y, :W - d]
                assert not a[y, W - d:, 3].any()
            elif d < 0:
                b[y, :W + d] = a[y, -d:]
                assert not a[y, :-d, 3].any()
            else:
                b[y] = a[y]
        a = b
    return a


def lean_rows(work, k):
    """shear the upper body: row y moves left by round(k*(27-y)) (work coords include PAD)."""
    out = np.zeros_like(work)
    for y in range(work.shape[0]):
        d = -int(round(k * max(0, (27 + PAD) - y)))
        out[y] = np.roll(work[y], d, axis=0)
        assert d == 0 or not (work[y, :abs(d), 3].any())
    return out


def rot_shear(a, deg, cx, cy):
    """Paeth three-shear rotation: every source pixel lands on exactly one target pixel (no resampling).
    deg > 0 turns the top of the figure to the left (counter-clockwise on screen)."""
    th = math.radians(deg)
    al, be = -math.tan(th / 2), math.sin(th)
    h, w = a.shape[:2]

    def xshear(img):
        o = np.zeros_like(img)
        for y in range(h):
            o[y] = np.roll(img[y], int(round(al * (y + 0.5 - cy))) * -1, axis=0)
        return o

    def yshear(img):
        o = np.zeros_like(img)
        for x in range(w):
            o[:, x] = np.roll(img[:, x], int(round(be * (x + 0.5 - cx))) * -1, axis=0)
        return o

    return xshear(yshear(xshear(a)))


def frame_layers(anim, fi, layers, ov):
    """Return {layer: 32x32 RGBA} for one frame. Only layers in `layers` are built, but every
    placement number depends on the frame alone (never on which layers are present)."""
    fr = RIG["animations"][anim]["frames"][fi]
    N = W + 2 * PAD
    work = {}
    for ln in RIG["layer_order"]:
        if ln not in layers and ln != "base":
            continue
        acc = np.zeros((N, N, 4), np.uint8)
        for pn in RIG["layers"][ln]["parts"]:
            a = render_part(pn, fr, ov)
            dx, dy = place(pn, fr)
            piece = np.zeros((N, N, 4), np.uint8)
            px.over(piece, a, PAD + dx, PAD + dy)
            if fr.get("lean") and GROUP_OF[pn] in UPPER:
                piece = lean_rows(piece, fr["lean"])
            px.over(acc, piece, 0, 0)
        work[ln] = acc
    alpha_before = {k: int((v[..., 3] > 0).sum()) for k, v in work.items()}
    if fr.get("rotate"):
        work = {k: rot_shear(v, fr["rotate"], PAD + 16, PAD + FOOT_ROW + 1) for k, v in work.items()}
    if fr.get("rot90"):
        work = {k: np.rot90(v, 1).copy() for k, v in work.items()}
    if fr.get("rotate") or fr.get("rot90"):
        b = px.bbox(work["base"])
        sy = (PAD + FOOT_ROW) - b[3]
        sx = int(round((PAD + 16) - (b[0] + b[2] + 1) / 2))   # centre the fallen body on the pivot line
        work = {k: px.shift(v, sx, sy) for k, v in work.items()}
    out = {}
    for k, v in work.items():
        assert int((v[..., 3] > 0).sum()) == alpha_before[k], f"{anim} f{fi} {k}: pixels lost in transform"
        crop = v[PAD:PAD + H, PAD:PAD + W].copy()
        assert int((crop[..., 3] > 0).sum()) == alpha_before[k], f"{anim} f{fi} {k}: clipped by the cell"
        if k in layers:
            out[k] = crop
    return out


def anim_layers(anim, ov=None):
    """{layer: [frames]} for all six layers with the default (key) colours or an override."""
    n = len(RIG["animations"][anim]["frames"])
    res = {ln: [] for ln in RIG["layer_order"]}
    for fi in range(n):
        f = frame_layers(anim, fi, set(RIG["layer_order"]), ov or {})
        for ln in RIG["layer_order"]:
            res[ln].append(f[ln])
    return res


def compose(anim, variant, **rec):
    ov = override(**rec)
    lays = RIG["variants"][variant]
    frames = []
    for fi in range(len(RIG["animations"][anim]["frames"])):
        f = frame_layers(anim, fi, set(lays), ov)
        out = px.new(W, H)
        for ln in RIG["layer_order"]:
            if ln in lays:
                px.over(out, f[ln], 0, 0)
        frames.append(out)
    return frames
