"""Procedural R04 (Abraham) enemies in the art 03 style: D1 wild stalker, D2 luring lamp, D3 raider steed (move, charge
windup, dash). Same method as R03 scripts/actors_r03.py (copied helpers: scripts/px03.py, byte-identical with art 03):
shapes authored on a pixel grid, lit from the upper left (px03.LIGHT), colours only from palette ramps (art 03
master-v0.3 plus src/palette_r04_ext.json), 1 px outline.0 outline, ground sprites shifted so their lowest row is the
foot row. Dust motes are added after the outline (no outline on dust). Deterministic (no randomness). Needs numpy.
Each generator takes a frame index and returns a 32x32 RGBA array.
Authored by Claude (Anthropic model) in this session as a script; it is not output of the Claude Design product."""
import json, math, os
import numpy as np
import px03 as px

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.dirname(HERE)
EXT = json.load(open(os.path.join(ROOT, "src", "palette_r04_ext.json"), encoding="utf-8"))
for _ramp, _cols in EXT["actor"].items():
    if f"{_ramp}.0" in px.PAL:
        raise SystemExit(f"palette_r04_ext ramp '{_ramp}' collides with the base palette")
    for _i, _c in enumerate(_cols):
        _v = px.hex_rgba(_c)
        px.PAL[f"{_ramp}.{_i}"] = _v
        px.NAME_OF.setdefault(_v, f"{_ramp}.{_i}")
        px.RAMP_OF.setdefault(_v, _ramp)

N = 32
FOOT = N - 2                 # foot row for pivot (16, 1)
K = px.C("outline.0")
K2 = px.C("outline.1")


class Frame:
    """Authored units are cell pixels: (x, y) with y down, feet at about y = 29.6."""
    def __init__(self):
        self.a = px.new(N, N)

    def P(self, x, y):
        return x, y


def lit(rmp, cuts=(0.84, 0.58, 0.32), shift=0):
    return lambda x, y, t, nx, ny, it: rmp[min(len(rmp) - 1, px.level(it, cuts) + shift)]


def ell(F, cx, cy, rx, ry, rot, fn):
    px.shade_ellipse(F.a, cx, cy, rx, ry, rot, fn)


def tube(F, pts, r0, r1, paint):
    px.tube(F.a, list(pts), lambda t: r0 + (r1 - r0) * t, paint)


def dot(F, x, y, col):
    px.put(F.a, int(x), int(y), col)


def ell_lit(rmp, cuts=(0.84, 0.58, 0.32), shift=0, rim=None):
    def fn(x, y, u, v, it):
        if rim is not None and u * u + v * v > 0.72 and (u + v) < -0.6:
            return rim
        return rmp[min(len(rmp) - 1, px.level(it, cuts) + shift)]
    return fn


def puff(a, cx, cy, r, cols):
    """pale dust mote added after the outline (no outline); cols = (light, dark)."""
    for y in range(int(cy - r) - 1, int(cy + r) + 2):
        for x in range(int(cx - r) - 1, int(cx + r) + 2):
            d = math.hypot(x + 0.5 - cx, y + 0.5 - cy)
            if d <= r and 1 <= x <= N - 2 and 1 <= y <= FOOT and a[y, x, 3] == 0:
                a[y, x] = cols[0] if (x + 0.5 - cx) + (y + 0.5 - cy) < 0.4 else cols[1]


def finish(a, ground, dust=()):
    a = px.outline(a)
    if ground:
        a = px.ground_to(a, FOOT)
    for cx, cy, r, cols in dust:
        puff(a, cx, cy, r, cols)
    return a


# ---------------- D1 wild stalker: a lean rust-brown wilderness beast, head low on the scent, dust behind ----------------
def wild_stalker(frame=0):
    F = Frame()
    ru, bn = px.ramp("rust"), px.ramp("bone")
    f = frame % 4
    ph = f * math.pi / 2
    bob = (0.0, -0.6, 0.0, -0.6)[f]

    def front(sx, sy, phase, shift):
        sw, lift = math.sin(phase), max(0.0, math.cos(phase)) * 1.8
        tube(F, [(sx, sy), (sx + 0.8 + 1.1 * sw, 25.4 - lift * 0.5), (sx + 2.4 * sw, 29.6 - lift)], 1.5, 0.8, lit(ru, shift=shift))

    def hind(hx, hy, phase, shift):
        sw, lift = math.sin(phase), max(0.0, math.cos(phase)) * 1.8
        tube(F, [(hx, hy), (hx + 1.8 + 0.6 * sw, 24.2), (hx - 0.6 + 1.6 * sw, 27.2 - lift * 0.6), (hx + 0.2 + 2.4 * sw, 29.6 - lift)],
             2.0, 0.8, lit(ru, shift=shift))

    hind(10.4, 20.8 + bob, ph + math.pi, 2)                # far legs, darker
    front(20.4, 21.6 + bob, ph, 2)
    tube(F, [(8.4, 18.6 + bob), (5.8, 16.8 + bob + 0.7 * math.sin(ph)), (3.4, 17.8 + bob + 0.9 * math.sin(ph + 1.0))],
         1.8, 0.7, lit(ru, shift=1))                        # bushy tail held low behind
    ell(F, 14.8, 19.4 + bob, 7.4, 3.9, 0.12, ell_lit(ru, rim=bn[2]))
    for i, mx in enumerate((9.8, 12.2, 14.6, 17.0)):        # ragged pale mane along the back
        my = 15.6 + 0.12 * (mx - 14.8) + bob
        tube(F, [(mx + 0.6, my + 1.0), (mx - 0.8, my - 1.6 - 0.5 * ((i + f) % 2))], 1.1, 0.4,
             lambda x, y, t, nx, ny, it: bn[1] if it > 0.55 else bn[2])
    hind(9.6, 21.2 + bob, ph, 0)                           # near legs
    front(19.8, 22.0 + bob, ph + math.pi, 0)
    tube(F, [(19.2, 18.4 + bob), (22.2, 20.0 + bob)], 2.9, 2.1, lit(ru))   # neck stretched forward and low
    tube(F, [(22.6, 19.6 + bob), (21.4, 16.2 + bob)], 1.1, 0.3, lit(ru, shift=1))   # ear laid back
    ell(F, 24.0, 21.2 + bob, 3.0, 2.3, 0.30, ell_lit(ru, rim=bn[2]))
    tube(F, [(25.4, 21.8 + bob), (28.2, 23.4 + bob)], 1.6, 0.9, lit(ru))  # long snout on the scent
    dot(F, 28.6, 23.4 + bob, K)
    dot(F, 27.0, 24.6 + bob, bn[0])                        # bared tooth
    dot(F, 24.4, 20.4 + bob, px.C("eye.0"))
    dust = []
    if f in (1, 3):                                        # dust kicked up behind the hind feet
        dust = [(6.2, 28.6, 1.4, (bn[1], bn[2])), (3.8, 27.4, 1.1, (bn[1], bn[2])), (2.6, 29.2, 0.8, (bn[1], bn[2]))]
    else:
        dust = [(4.4, 29.0, 0.9, (bn[1], bn[2]))]
    return finish(F.a, True, dust)


# ---------------- D2 luring lamp: a floating cage lamp with a sly face and a false pink-violet light ----------------
def luring_lamp(frame=0):
    F = Frame()
    lu, st = px.ramp("lure"), px.ramp("stone")
    f = frame % 4
    bob = (0.0, -1.0, -2.0, -1.0)[f]
    sway = (0.0, 0.9, 0.0, -0.9)[f]

    def wisp(x, y, t, nx, ny, it):
        if t > 0.55 and (x + y + f) % 2:
            return None                                    # the false light frays into dither
        return lu[2] if t < 0.45 else lu[3]

    tube(F, [(13.2, 20.0 + bob), (9.8, 22.0 + bob + sway), (6.6, 20.8 + bob - sway), (3.6, 22.8 + bob + sway)], 2.3, 0.5, wisp)

    def flame(x, y, u, v, it):
        r2 = u * u + (v + 0.15) * (v + 0.15)
        if r2 < 0.20:
            return lu[0]
        if r2 < 0.58:
            return lu[1]
        return lu[2] if v < 0.45 else lu[3]

    ell(F, 16.0, 15.6 + bob, 4.7, 5.3, 0.0, flame)
    tube(F, [(16.0, 12.0 + bob), (16.4 + 0.6 * sway, 9.6 + bob)], 1.6, 0.5, lambda *_: lu[1])   # flame tongue
    for bx in (11.6, 20.4):                                # side bars of the cage
        tube(F, [(bx, 11.0 + bob), (bx - 0.2 * (1 if bx < 16 else -1), 20.2 + bob)], 0.8, 0.8, lit(st, shift=1))
    ell(F, 16.0, 10.2 + bob, 5.2, 1.8, 0.0, ell_lit(st, shift=1))   # cap
    ell(F, 16.0, 8.6 + bob, 2.4, 1.3, 0.0, ell_lit(st, shift=1))    # roof
    tube(F, [(14.2, 8.0 + bob), (14.6, 5.4 + bob), (17.4, 5.4 + bob), (17.8, 8.0 + bob)], 0.6, 0.6, lambda *_: st[3])   # ring
    ell(F, 16.0, 21.0 + bob, 4.8, 1.6, 0.0, ell_lit(st, shift=1))   # base
    tube(F, [(16.0, 22.2 + bob), (16.0, 24.8 + bob)], 1.2, 0.4, lit(st, shift=1))   # finial
    for x, y in ((13.6, 14.4), (14.6, 15.0), (18.4, 14.4), (17.4, 15.0)):   # slanted sly eyes
        dot(F, x, y + bob, K)
    for x, y in ((14.0, 17.6), (15.0, 18.2), (16.0, 18.4), (17.0, 18.2), (18.0, 17.6)):   # crooked smile
        dot(F, x, y + bob, K2)
    a = finish(F.a, False)
    for x, y in (((7, 13), (24, 8)), ((6, 11), (25, 10)), ((8, 9), (23, 12)), ((7, 11), (26, 9)))[f]:   # drifting motes
        if a[y, x, 3] == 0:
            a[y, x] = lu[1]
    return a


# ---------------- D3 raider steed: a riderless war-horse with a bone mask, red raid blanket and plunder bags ----------------
def _steed(P):
    F = Frame()
    st, bn = px.ramp("steed"), px.ramp("bone")
    ln, le, gd = px.ramp("lining"), px.ramp("leather")[1:], px.C("gold.0")
    hoof = lambda x, y, t, nx, ny, it: K2 if t > 0.86 else st[min(3, px.level(it, (0.84, 0.58, 0.32)) + P.get("legshift", 0))]
    hoof_far = lambda x, y, t, nx, ny, it: K2 if t > 0.86 else st[min(3, px.level(it, (0.84, 0.58, 0.32)) + 2)]
    for leg in P["far"]:
        tube(F, leg, 1.6, 0.9, hoof_far)
    tube(F, P["tail"], 1.7, 0.6, lambda *_: st[3])
    bx, by, rot = P["body"]
    ell(F, bx - 5.4 * math.cos(rot), by - 5.4 * math.sin(rot) - 0.3, 3.9, 4.0, rot, ell_lit(st))   # rump
    ell(F, bx, by, 8.0, 4.0, rot, ell_lit(st))
    ell(F, bx + 5.8 * math.cos(rot), by + 5.8 * math.sin(rot) - 0.4, 3.6, 4.1, rot, ell_lit(st))   # chest
    sx, sy = P["blanket"]
    ell(F, sx, sy, 4.4, 1.9, rot, lambda x, y, u, v, it: ln[0] if it > 0.6 else ln[1])
    dot(F, sx + 2.6 * math.cos(rot), sy + 2.6 * math.sin(rot) + 1.2, gd)            # blanket stud
    gx, gy = P["bag"]
    ell(F, gx, gy, 2.3, 2.5, 0.0, lambda x, y, u, v, it: le[0] if it > 0.6 else le[1])
    dot(F, gx - 0.4, gy - 2.0, gd)                                                  # plunder peeking out
    dot(F, gx + 0.8, gy - 1.8, gd)
    tube(F, P["neck"], 3.0, 2.2, lit(st))
    for strand in P["mane"]:
        tube(F, strand, 1.0, 0.4, lambda *_: K2)
    mask = lambda x, y, t, nx, ny, it: (bn[1] if it > 0.55 else bn[2]) if t > 0.30 else st[min(3, px.level(it, (0.84, 0.58, 0.32)))]
    tube(F, P["head"], 2.3, 1.5, mask)
    tube(F, P["ear"], 0.9, 0.3, lit(st, shift=1))
    tube(F, P["horn"], 0.9, 0.3, lambda *_: bn[1])
    ex, ey = P["eye"]
    dot(F, ex, ey, K2)
    dot(F, ex + 1, ey, px.C("ribbon.0"))
    for leg in P["near"]:
        tube(F, leg, 1.7, 0.9, hoof)
    return finish(F.a, True, [(x, y, r, (bn[1], bn[2])) for x, y, r in P.get("dust", ())])


def _front(sx, sy, sw, lift):
    return [(sx, sy), (sx + 0.6 + 1.0 * sw, 25.0 - lift * 0.6), (sx + 2.4 * sw, 29.6 - lift)]


def _hind(hx, hy, sw, lift):
    return [(hx, hy), (hx + 1.4 + 0.6 * sw, 23.4), (hx - 0.6 + 1.4 * sw, 26.4 - lift * 0.6), (hx + 0.4 + 2.4 * sw, 29.6 - lift)]


def steed_move(frame=0):
    f = frame % 4
    ph = f * math.pi / 2
    b = (0.0, -0.6, 0.0, -0.6)[f]
    s = lambda p: (math.sin(p), max(0.0, math.cos(p)) * 1.8)
    return _steed({
        "body": (14.4, 18.0 + b, -0.04), "blanket": (13.0, 14.2 + b), "bag": (11.2, 18.6 + b),
        "far": [_hind(9.8, 20.0 + b, *s(ph + math.pi)), _front(19.8, 20.4 + b, *s(ph))],
        "near": [_hind(9.2, 20.4 + b, *s(ph)), _front(19.2, 20.8 + b, *s(ph + math.pi))],
        "tail": [(6.4, 15.6 + b), (4.2, 17.0 + b + 0.6 * math.sin(ph)), (3.0, 21.0 + b + 0.8 * math.sin(ph + 1.0))],
        "neck": [(19.8, 16.0 + b), (22.2, 12.4 + b), (23.4, 9.8 + b)],
        "mane": [[(19.0, 13.6 + b), (17.4, 12.6 + b + 0.6 * math.sin(ph))], [(20.6, 11.2 + b), (19.2, 9.8 + b + 0.6 * math.sin(ph + 1))],
                 [(22.0, 9.2 + b), (21.0, 7.4 + b)]],
        "head": [(23.6, 9.2 + b), (26.0, 10.8 + b), (27.8, 12.8 + b)],
        "ear": [(22.8, 8.4 + b), (22.2, 6.0 + b)], "horn": [(24.4, 8.2 + b), (25.2, 5.8 + b)], "eye": (24.6, 9.8 + b)})


def steed_windup(frame=0):
    if frame % 2 == 0:                                      # head down, near forefoot pawing, weight back
        return _steed({
            "body": (14.0, 18.2, 0.06), "blanket": (12.6, 14.2), "bag": (10.8, 18.8),
            "far": [_hind(9.2, 20.2, -0.5, 0.0), _front(19.8, 21.2, 0.3, 0.0)],
            "near": [_hind(8.6, 20.6, 0.5, 0.0), [(19.4, 21.4), (22.4, 22.8), (22.2, 25.8)]],
            "tail": [(6.0, 15.4), (3.6, 14.6), (2.6, 17.2)],
            "neck": [(19.8, 17.2), (22.6, 16.8), (24.6, 17.6)],
            "mane": [[(19.4, 14.8), (18.0, 13.2)], [(21.4, 14.4), (20.6, 12.6)], [(23.4, 15.0), (23.2, 13.0)]],
            "head": [(24.8, 17.0), (26.4, 19.6), (27.4, 22.4)],
            "ear": [(24.2, 15.4), (23.0, 13.6)], "horn": [(25.6, 15.6), (26.0, 13.2)], "eye": (25.2, 17.8),
            "dust": [(23.6, 29.0, 1.3), (25.8, 28.2, 0.9), (21.4, 29.4, 0.8)]})
    return _steed({                                         # reared and gathered: forefeet up, hind legs coiled
        "body": (13.6, 18.0, -0.42), "blanket": (12.4, 13.8), "bag": (10.6, 18.2),
        "far": [[(9.2, 21.0), (11.4, 24.2), (8.6, 26.8), (10.4, 29.6)], [(18.6, 14.8), (21.4, 14.0), (21.6, 17.4)]],
        "near": [[(8.6, 21.4), (10.8, 24.6), (7.8, 27.0), (9.0, 29.6)], [(18.0, 15.6), (21.2, 15.8), (20.4, 19.2)]],
        "tail": [(6.2, 20.2), (3.8, 22.4), (2.8, 26.0)],
        "neck": [(18.6, 12.6), (20.4, 8.6), (21.6, 6.8)],
        "mane": [[(17.8, 10.6), (15.8, 9.6)], [(19.0, 8.0), (17.2, 6.8)], [(20.4, 6.0), (19.0, 4.6)]],
        "head": [(21.8, 6.6), (24.2, 6.6), (26.2, 7.8)],
        "ear": [(21.0, 5.6), (20.0, 3.8)], "horn": [(22.6, 5.0), (22.4, 2.8)], "eye": (22.8, 6.6),
        "dust": [(4.6, 28.8, 1.5), (2.8, 27.4, 1.0), (6.8, 29.4, 0.9)]})


def steed_dash(frame=0):
    if frame % 2 == 0:                                      # stretched: forelegs reaching, hind legs pushing off
        far = [[(9.4, 20.0), (7.2, 23.0), (5.0, 26.4), (4.0, 29.6)], [(20.4, 20.0), (23.2, 22.6), (24.6, 26.6)]]
        near = [[(8.8, 20.4), (6.6, 23.6), (4.0, 26.0), (2.6, 28.2)], [(19.8, 20.4), (23.4, 22.0), (26.0, 25.0)]]
        dust = [(2.6, 29.4, 0.9), (5.8, 29.0, 1.1)]
    else:                                                   # gathered: all four hooves under the belly
        far = [[(9.6, 20.0), (12.0, 23.0), (11.2, 26.4), (12.8, 29.6)], [(20.2, 20.0), (19.0, 23.8), (20.6, 27.2)]]
        near = [[(9.0, 20.4), (11.6, 23.6), (10.4, 26.8), (11.4, 29.6)], [(19.6, 20.4), (18.2, 24.2), (19.4, 28.0)]]
        dust = [(4.2, 29.0, 1.4), (2.6, 27.8, 1.0), (6.6, 29.4, 0.8)]
    return _steed({
        "body": (14.4, 18.4, 0.0), "blanket": (12.8, 14.6), "bag": (10.8, 19.0), "legshift": 0,
        "far": far, "near": near,
        "tail": [(6.2, 16.4), (3.8, 15.2), (2.4, 15.8)],
        "neck": [(20.4, 16.6), (23.2, 14.0), (24.6, 12.8)],
        "mane": [[(19.8, 14.0), (16.6, 12.8)], [(21.6, 12.6), (18.4, 11.0)], [(23.2, 11.6), (20.6, 9.8)]],
        "head": [(24.6, 12.4), (26.6, 13.2), (28.0, 14.6)],
        "ear": [(23.6, 11.2), (21.8, 10.0)], "horn": [(25.0, 11.2), (23.8, 9.2)], "eye": (25.4, 12.6),
        "dust": dust})


GENERATORS = {
    "enm_d1-wild-stalker_move_32": wild_stalker,
    "enm_d2-luring-lamp_move_32": luring_lamp,
    "enm_d3-raider-steed_move_32": steed_move,
    "enm_d3-raider-steed_windup_32": steed_windup,
    "enm_d3-raider-steed_dash_32": steed_dash,
}
