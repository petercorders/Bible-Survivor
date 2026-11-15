"""Procedural R03 (Noah) actors in the art 03 style: C1 violence shadow, C2 timber devourer, C3 wreckage giant,
the Nephilim tyrant boss (64 px clips) and its shadow, plus the slam-warning reference ring and impact decal.
Same method as art 03 scripts/enemies03.py and R02 scripts/enemies_r02.py (copied helpers: scripts/px03.py,
byte-identical): shapes authored on a unit grid, lit from the upper left (px03.LIGHT), colours only from palette
ramps (art 03 master-v0.3 plus src/palette_r03_ext.json), 1 px outline.0 outline, ground sprites shifted so their
lowest row is the foot row. Deterministic (no randomness). Needs numpy. Each generator takes a frame index and
returns an RGBA array.
Authored by Claude (Anthropic model) in this session as a script; it is not output of the Claude Design product."""
import json, math, os
import numpy as np
import px03 as px

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.dirname(HERE)
EXT = json.load(open(os.path.join(ROOT, "src", "palette_r03_ext.json"), encoding="utf-8"))
for _ramp, _cols in EXT["actor"].items():
    if f"{_ramp}.0" in px.PAL:
        raise SystemExit(f"palette_r03_ext ramp '{_ramp}' collides with the base palette")
    for _i, _c in enumerate(_cols):
        _v = px.hex_rgba(_c)
        px.PAL[f"{_ramp}.{_i}"] = _v
        px.NAME_OF.setdefault(_v, f"{_ramp}.{_i}")
        px.RAMP_OF.setdefault(_v, _ramp)

K = px.C("outline.0")
W = px.C("white.0")
TAU = math.pi * 2


class Frame:
    """Authored point (cx, base) lands on the cell's (centre, foot row); g = cell px per authored unit."""
    def __init__(self, n, g, cx, base):
        self.n = n
        self.S = g
        self.ox = n / 2 - cx * g
        self.oy = (n - 1.5) - base * g
        self.a = px.new(n, n)

    def P(self, x, y):
        return x * self.S + self.ox, y * self.S + self.oy

    def R(self, r):
        return r * self.S


def lit(rmp, cuts=(0.84, 0.58, 0.32), shift=0):
    """paint for px.tube: ramp index from the light level, darkened by `shift`."""
    return lambda x, y, t, nx, ny, it: rmp[min(len(rmp) - 1, px.level(it, cuts) + shift)]


def ell(F, cx, cy, rx, ry, rot, fn):
    X, Y = F.P(cx, cy)
    px.shade_ellipse(F.a, X, Y, F.R(rx), F.R(ry), rot, fn)


def tube(F, pts, r0, r1, paint):
    px.tube(F.a, [F.P(*p) for p in pts], lambda t: F.R(r0 + (r1 - r0) * t), paint)


def dot(F, x, y, col):
    X, Y = F.P(x, y)
    px.put(F.a, int(X), int(Y), col)


def finish(a, ground, n):
    a = px.outline(a)
    return px.ground_to(a, n - 2) if ground else a


# ---------------- C1 violence shadow: a hunched smoke figure surging forward with a raised fist ----------------
def violence_shadow(frame=0):
    F = Frame(32, 0.92, cx=16.0, base=30.2)
    sh, em = px.ramp("shade"), px.ramp("ember")
    f = frame % 4
    ph = f * math.pi / 2
    bob = (0.0, -0.6, -1.0, -0.4)[f]
    pump = (0.0, -1.0, -0.4, 0.5)[f]

    def smoke(x, y, t, nx, ny, it):
        if t > 0.70 and (x + y + f) % 2:
            return None                                   # wisps fray into dither
        return sh[min(3, px.level(it, (0.9, 0.66, 0.40)) + 1)]

    tube(F, [(13.6, 21.0 + bob), (10.8, 25.0 + bob * 0.5), (7.4 + 0.9 * math.sin(ph), 27.4), (4.0 + 1.2 * math.sin(ph + 1.3), 28.2)],
         4.2, 0.7, smoke)                                  # trailing smoke instead of legs
    tube(F, [(16.4, 22.6 + bob), (16.0, 26.2), (14.2 + 0.8 * math.sin(ph + 2.0), 28.8)], 2.4, 0.6, smoke)
    tube(F, [(10.4, 12.8 + bob), (8.6 + 0.6 * math.sin(ph), 10.4 + bob), (7.0 + 0.9 * math.sin(ph + 1.0), 8.2 + bob)],
         1.4, 0.4, smoke)                                  # smoke rising off the back
    tube(F, [(17.8, 14.6 + bob), (21.6, 17.6 + bob), (24.4, 18.4 + bob)], 1.5, 1.1, lit(sh, shift=2))   # far arm, claw forward

    def body(x, y, u, v, it):
        if u * u + v * v > 0.70 and (u + v) < -0.55:
            return sh[0]                                  # cold rim light, upper left
        return sh[min(3, px.level(it, (0.86, 0.60, 0.36)) + 1)]

    ell(F, 14.0, 17.6 + bob, 5.2, 6.6, -0.28, body)
    ell(F, 15.0, 12.6 + bob, 5.6, 2.9, -0.12, body)       # hunched shoulders

    def head(x, y, u, v, it):
        if u * u + v * v > 0.62 and (u + v) < -0.45:
            return sh[0]
        if v > 0.35 and u > -0.2:
            return sh[3]                                  # shadowed face under the hood
        return sh[min(3, px.level(it, (0.84, 0.60, 0.36)) + 1)]

    ell(F, 16.8, 8.4 + bob, 3.3, 3.3, 0.0, head)
    for ex in (17.9, 19.4):                               # two ember eyes
        dot(F, ex, 9.0 + bob, em[0])
    tube(F, [(19.0, 12.2 + bob), (22.2, 9.4 + bob + pump * 0.5), (23.6, 5.6 + bob + pump)], 1.7, 1.4, lit(sh, shift=1))

    def fist(x, y, u, v, it):
        if v < -0.1 and u > -0.3:
            return em[1] if u > 0.3 else em[2]            # hot knuckles on top
        return sh[min(3, px.level(it, (0.84, 0.58, 0.34)))]

    ell(F, 24.0, 4.4 + bob + pump, 2.3, 2.2, 0.0, fist)
    return finish(F.a, False, 32)


# ---------------- C2 timber devourer: an arching wood grub with an armoured head and working pincers ----------------
def timber_devourer(frame=0):
    F = Frame(32, 0.95, cx=16.0, base=30.0)
    ch, bn, bk = px.ramp("chitin"), px.ramp("bone"), px.ramp("bark")
    f = frame % 4
    jaw = (1.0, 0.5, 0.0, 0.5)[f]                          # open -> half -> snapped shut -> half
    arch = (5.4, 4.2, 3.0, 4.2)[f]                         # inchworm gait: the hump rises and flattens
    reach = (0.0, 0.6, 1.2, 0.6)[f]
    n_seg = 6
    centres = []
    for i in range(n_seg):
        t = i / (n_seg - 1)
        x = 3.8 + (17.0 + reach) * t
        y = 27.2 - (arch + 3.4) * math.sin(math.pi * min(1.0, t * 1.05)) - 2.6 * t * t
        r = 2.4 + 1.3 * math.sin(math.pi * t) + 0.3 * t
        centres.append((x, y, r))
    for x, y, r in (centres[0], centres[1], centres[-2], centres[-1]):   # stubby prolegs that grip the ground
        tube(F, [(x, y + r * 0.5), (x + 0.4, 30.0)], 0.9, 0.7, lambda *_: ch[3])

    def seg(x, y, u, v, it):
        if u * u + v * v > 0.72:
            return bn[3]                                  # ring between body segments
        if v > 0.5:
            return bn[2]
        return bn[min(3, px.level(it, (0.86, 0.62, 0.36)))]

    for x, y, r in centres:
        ell(F, x, y, r * 1.05, r, 0.0, seg)
    hx, hy = centres[-1][0] + 2.6, centres[-1][1] - 1.8

    def mandible(base_pt, tip, bend):
        mid = ((base_pt[0] + tip[0]) / 2 + 0.4, (base_pt[1] + tip[1]) / 2 + bend)
        tube(F, [base_pt, mid, tip], 1.4, 0.5,
             lambda x, y, t, nx, ny, it: bn[1] if t > 0.80 else ch[min(3, px.level(it, (0.80, 0.52, 0.30)) + 2)])

    mandible((hx + 1.8, hy - 1.4), (hx + 5.4, hy - 0.6 - 4.0 * jaw), -1.6 - 0.6 * jaw)
    mandible((hx + 1.8, hy + 1.6), (hx + 5.4, hy + 0.8 + 3.6 * jaw), 1.4 + 0.6 * jaw)

    def head(x, y, u, v, it):
        if u * u + v * v > 0.66 and (u + v) < -0.5:
            return ch[0]
        if v > 0.5:
            return ch[3]
        return ch[min(3, px.level(it, (0.84, 0.60, 0.34)))]

    ell(F, hx, hy, 3.7, 3.4, -0.2, head)
    dot(F, hx + 1.3, hy - 0.8, K)                         # small dark eye
    if f == 2:                                            # sawdust bursts from the snapped pincers
        for sx, sy in ((hx + 5.6, hy - 2.6), (hx + 5.8, hy + 2.2), (hx + 4.2, hy + 3.8)):
            dot(F, sx, sy, bk[0])
    return finish(F.a, True, 32)


# ---------------- C3 wreckage giant: a slow hulk of broken, pitch-stained planks lashed with rope ----------------
def wreckage_giant(frame=0):
    F = Frame(32, 0.94, cx=16.0, base=30.0)
    dr, bk, pt, st, lt, em = (px.ramp(r) for r in ("drift", "bark", "pitch", "stone", "leather", "ember"))
    f = frame % 4
    sway = (0.0, 0.5, 0.0, -0.5)[f]
    lift = ((0.0, 0.0), (1.4, 0.0), (0.0, 0.0), (0.0, 1.4))[f]
    swing = (0.0, 0.8, 0.0, -0.8)[f]

    def plank_col(rmp, shift=0):
        return lambda x, y, t, nx, ny, it: pt[1] if abs(nx) < 0.18 else rmp[min(3, px.level(it, (0.80, 0.54, 0.30)) + shift)]

    tube(F, [(12.8 + sway, 21.0), (12.0, 25.5), (11.4, 30.0 - lift[0])], 2.2, 2.0, plank_col(bk, 1))
    tube(F, [(7.2 + sway, 9.4), (5.8 + sway * 0.4, 5.4), (4.4 + sway * 0.4, 1.8)], 1.3, 1.0,
         lambda x, y, t, nx, ny, it: (None if (x + y) % 2 else bk[1]) if t > 0.9 else bk[min(3, px.level(it, (0.8, 0.5, 0.3)) + 1)])
    tube(F, [(6.6 + sway, 10.0), (5.8 - swing * 0.5, 15.0), (5.4 - swing, 20.4)], 1.8, 1.7, plank_col(dr, 2))  # far arm

    def planks(x, y, u, v, it):
        k = (u + 1) * 4.0
        if v < -0.82 + 0.24 * (int(k) % 2):
            return None                                   # broken, uneven plank tops
        if k - int(k) < 0.20:
            return pt[1]                                  # tar-filled seam between planks
        if abs(v - 0.05 - 0.55 * u) < 0.10 or abs(v + 0.25 + 0.55 * u) < 0.09:
            return lt[1] if (x + y) % 2 else lt[2]        # crossed rope lashing
        if (int(k) * 7 + int((v + 1) * 3)) % 5 == 0 and v > 0.25:
            return pt[0]                                  # pitch patch
        rmp = dr if int(k) % 2 == 0 else bk
        return rmp[min(3, px.level(it, (0.84, 0.60, 0.34)) + (0 if rmp is dr else 1))]

    ell(F, 16.0 + sway, 15.8, 8.4, 8.6, 0.0, planks)
    for nx_, ny_ in ((11.2, 13.0), (20.6, 18.6), (14.8, 21.0)):
        dot(F, nx_ + sway, ny_, st[0])                    # nail heads
    tube(F, [(19.6 + sway, 21.0), (20.2, 25.5), (20.6, 30.0 - lift[1])], 2.2, 2.0, plank_col(dr))

    def beam(x, y, t, nx, ny, it):
        if t < 0.05 or t > 0.95:
            return None if (x + y) % 2 else dr[2]         # splintered ends
        return dr[min(3, px.level(it, (0.80, 0.52, 0.30)))]

    tube(F, [(5.2, 8.8 + sway * 0.2), (16.0 + sway, 7.8), (27.2, 8.2 - sway * 0.2)], 1.7, 1.6, beam)
    for nx_ in (9.0, 23.0):
        dot(F, nx_, 7.6, st[0])                           # iron nails in the crossbeam
    X0, Y0 = F.P(13.4 + sway, 2.6)
    X1, Y1 = F.P(19.4 + sway, 6.6)
    for y in range(int(Y0), int(Y1) + 1):                 # crate head
        for x in range(int(X0), int(X1) + 1):
            v = (y - Y0) / max(1, Y1 - Y0)
            col = bk[1] if x < (X0 + X1) / 2 else bk[2]
            if 0.35 < v < 0.72:
                col = pt[1]                               # dark slit
            px.put(F.a, x, y, col)
    for ex in (16.4, 18.2):
        dot(F, ex + sway, 4.7, em[1])
    tube(F, [(25.6 + sway, 9.2), (26.8 + swing * 0.5, 14.6), (27.4 + swing, 20.4)], 2.0, 1.9, plank_col(dr))   # near arm
    dot(F, 27.4 + swing, 17.2, st[0])
    for dx_ in (11.2, 15.4, 21.4):                        # pitch drips under the hull
        dot(F, dx_ + sway, 24.8, pt[1])
    return finish(F.a, True, 32)


# ---------------- Boss: the Nephilim tyrant, a created giant with a broken timber club (64 px clips) ----------------
# Authored on a 1 px grid (Frame(64, 1.0, cx=32, base=62)): x right, y down, feet on row 62 (= 64 - 2).
GI, BK, ST, GD, LT, EM, BN, PT, SM = "giant", "bark", "stone", "gold", "leather", "ember", "bone", "pitch", "smoke"


def _club(F, pommel, grip, head, broken=True, split=None):
    """Broken ark-timber beam: thin at the grip, thick and splintered at the head, two iron bands and nails."""
    bk, st = px.ramp(BK), px.ramp(ST)
    gx, gy = grip
    hx, hy = head
    L = math.hypot(hx - gx, hy - gy)
    ux, uy = (hx - gx) / L, (hy - gy) / L

    def wood(x, y, t, nx, ny, it):
        if 0.62 < t < 0.70 or 0.84 < t < 0.92:
            return st[min(2, px.level(it, (0.76, 0.46)))]
        lv = px.level(it, (0.82, 0.58, 0.34))
        if t > 0.3 and abs(nx * uy - ny * ux) < 0.18 and (int(t * 40) % 3 == 0):
            lv = min(3, lv + 1)                              # wood grain
        return bk[lv]

    pts = [pommel, grip, ((gx + hx) / 2, (gy + hy) / 2), head]
    tube(F, pts, 1.6, 3.4, wood)
    for s, off in ((0.0, 0.0), (0.9, -1.0), (-1.1, 1.2)):  # splinters at the broken end
        sx, sy = -uy * s * 2.2, ux * s * 2.2
        tube(F, [(hx + sx, hy + sy), (hx + sx + ux * (3.0 + off), hy + sy + uy * (3.0 + off))], 1.0, 0.4, lit(bk, shift=0))
    for t in (0.66, 0.88):                                # nail heads on the bands
        dot(F, gx + ux * L * t - uy * 1.4, gy + uy * L * t + ux * 1.4, st[0])


def _limb(F, pts, r0, r1, band=None, bracer=None, shift=0):
    gi, gd = px.ramp(GI), px.ramp(GD)

    def paint(x, y, t, nx, ny, it):
        if band and band[0] < t < band[1] or bracer and bracer[0] < t < bracer[1]:
            return gd[min(2, px.level(it, (0.80, 0.50)))]
        return gi[min(3, px.level(it, (0.82, 0.58, 0.34)) + shift)]

    tube(F, pts, r0, r1, paint)


def _fist(F, x, y, r=2.7, shift=0):
    gi = px.ramp(GI)
    ell(F, x, y, r, r * 0.92, 0.0, lambda X, Y, u, v, it: gi[min(3, px.level(it, (0.82, 0.56, 0.32)) + shift)])


def _foot(F, x, y, shift=0):
    gi = px.ramp(GI)
    ell(F, x + 1.0, y - 1.6, 4.6, 2.2, 0.0, lambda X, Y, u, v, it: gi[min(3, px.level(it, (0.80, 0.55, 0.30)) + shift)])


def _torso(F, chest, belly, rot):
    gi = px.ramp(GI)

    def skin(x, y, u, v, it):
        if u * u + v * v > 0.74 and (u + v) < -0.6:
            return gi[0]                                  # rim light upper left
        lv = px.level(it, (0.86, 0.62, 0.38))
        if -0.05 < u < 0.12 and v > -0.35:
            lv = min(3, lv + 1)                           # breastbone shadow
        if 0.05 < v < 0.18 and abs(u) < 0.7:
            lv = min(3, lv + 1)                           # under the pecs
        return gi[min(3, lv)]

    ell(F, belly[0], belly[1], 9.0, 6.4, rot, lambda x, y, u, v, it: gi[min(3, px.level(it, (0.86, 0.60, 0.36)) + 1)])
    ell(F, chest[0], chest[1], 12.0, 10.4, rot, skin)


def _belt(F, hip, swing=0.0):
    lt, gd = px.ramp(LT), px.ramp(GD)
    x, y = hip
    tube(F, [(x - 9.0, y - 0.6), (x, y + 0.4), (x + 9.0, y - 0.6)], 2.0, 2.0, lit(lt))
    tube(F, [(x + 2.6, y + 1.0), (x + 3.2 + swing * 0.4, y + 5.0), (x + 3.0 + swing, y + 8.6)], 3.6, 2.6, lit(lt, shift=1))
    ell(F, x + 2.6, y + 0.2, 1.6, 1.5, 0.0, lambda X, Y, u, v, it: gd[min(2, px.level(it, (0.75, 0.45)))])


def _head(F, hx, hy, eye=0, tilt=0.0):
    gi, gd, em, bn, pt = px.ramp(GI), px.ramp(GD), px.ramp(EM), px.ramp(BN), px.ramp(PT)
    tube(F, [(hx - 2.0, hy - 3.6), (hx - 5.6, hy - 1.0), (hx - 7.4, hy + 3.4)], 3.4, 1.4, lit(pt))   # matted mane

    def face(x, y, u, v, it):
        if u * u + v * v > 0.70 and (u + v) < -0.5:
            return gi[0]
        lv = px.level(it, (0.84, 0.60, 0.36))
        if -0.42 < v < -0.12 and u > -0.2:
            lv = 3                                         # heavy brow shadow band
        return gi[min(3, lv)]

    ell(F, hx, hy, 5.2, 5.6, tilt, face)
    ell(F, hx + 2.6, hy + 3.6, 3.4, 2.2, tilt, lambda x, y, u, v, it: gi[min(3, px.level(it, (0.80, 0.55, 0.32)) + 1)])  # jaw
    for sx in (0.6, 3.2):                                  # tusks pointing up from the jaw
        tube(F, [(hx + sx, hy + 4.4), (hx + sx + 0.6, hy + 2.2)], 0.9, 0.5, lambda x, y, t, nx, ny, it: bn[0])
    for i, k in enumerate((-4.0, -1.0, 2.0)):              # spiked bronze circlet
        tube(F, [(hx + k * 0.9, hy - 4.4), (hx + k * 0.9 + 0.4, hy - 7.6 + abs(k) * 0.3)], 1.0, 0.4, lit(gd))
    tube(F, [(hx - 4.8, hy - 3.4), (hx, hy - 4.6), (hx + 4.8, hy - 3.4)], 1.1, 1.1, lit(gd))
    for ex in (hx + 2.4, hx + 4.4):
        dot(F, ex, hy - 0.2, em[eye])


def _collar(F, x, y):
    tube(F, [(x - 5.0, y), (x, y + 1.6), (x + 5.0, y - 0.2)], 1.5, 1.5, lit(px.ramp(GD)))


def _leg(F, hip, knee, ankle, shift=0):
    gi = px.ramp(GI)
    tube(F, [hip, knee, ankle], 4.8, 3.2, lambda x, y, t, nx, ny, it: gi[min(3, px.level(it, (0.82, 0.56, 0.32)) + shift)])
    _foot(F, ankle[0], ankle[1] + 2.0, shift)


def _body(F, P, eye=0):
    """Draws one pose. P: hip, chest, rot, head, legs (near, far), arms (near, far), club, clubFront."""
    nl, fl = P["legs"]
    na, fa = P["arms"]
    _leg(F, *fl, shift=1)
    _limb(F, fa, 3.6, 2.6, shift=1)
    _fist(F, *fa[-1], shift=1)
    if P.get("club") and not P.get("clubFront"):
        _club(F, *P["club"])
    _torso(F, P["chest"], P["belly"], P["rot"])
    _leg(F, *nl)
    _belt(F, P["hip"], P.get("swing", 0.0))
    _head(F, *P["head"], eye=eye, tilt=P.get("tilt", 0.0))
    _collar(F, P["chest"][0] + 4.0, P["chest"][1] - 9.0)
    if P.get("club") and P.get("clubFront"):
        _club(F, *P["club"])
    _limb(F, na, 4.0, 3.0, band=(0.22, 0.32), bracer=(0.72, 0.86))
    _fist(F, *na[-1])


def _walk_pose(f):
    bob = (0.0, -1.0, 0.0, -1.0)[f]
    step = (1.0, 0.0, -1.0, 0.0)[f]
    lift_n = (0.0, 0.0, 0.0, 2.0)[f]
    lift_f = (0.0, 2.0, 0.0, 0.0)[f]
    hip = (31.0, 41.0 + bob)
    nh, fh = (35.0, 43.0 + bob), (27.5, 43.0 + bob)
    na_ = (36.0 + 6.0 * step, 59.0 - lift_n)
    fa_ = (27.0 - 6.0 * step, 59.0 - lift_f)
    return {
        "hip": hip, "chest": (32.0, 28.0 + bob), "belly": (31.0, 37.0 + bob), "rot": -0.12,
        "head": (40.0, 13.0 + bob),
        "legs": ((nh, ((nh[0] + na_[0]) / 2 + 1.6, 51.0 + bob - lift_n * 0.5), na_),
                 (fh, ((fh[0] + fa_[0]) / 2 + 1.6, 51.0 + bob - lift_f * 0.5), fa_)),
        "arms": (((40.0, 21.0 + bob), (48.0, 28.0 + bob), (45.0, 23.0 + bob)),
                 ((23.0, 22.0 + bob), (19.0 + step, 31.0 + bob), (20.0 + 2.0 * step, 39.0 + bob))),
        "club": ((48.0, 25.4 + bob), (45.0, 23.5 + bob), (12.0, 8.0 + bob)), "clubFront": True,
        "swing": -step,
    }


def tyrant_move(frame=0):
    F = Frame(64, 1.0, cx=32.0, base=62.0)
    _body(F, _walk_pose(frame % 4))
    return finish(F.a, True, 64)


def _windup_pose(k):
    base = _walk_pose(0)
    P = dict(base)
    if k == 0:      # crouch, club comes off the shoulder and up in front
        P.update(chest=(32.0, 30.0), belly=(31.0, 38.5), hip=(31.0, 42.0), head=(40.0, 15.5), rot=-0.05,
                 arms=(((40.0, 23.0), (47.0, 31.0), (45.0, 25.0)), ((24.0, 24.0), (32.0, 30.0), (42.0, 26.0))),
                 club=((46.0, 29.5), (45.0, 25.5), (50.5, 9.5)), clubFront=True)
    elif k == 1:    # club over the head, both hands high
        P.update(chest=(30.5, 29.0), belly=(30.5, 38.0), hip=(31.0, 41.5), head=(37.5, 14.0), rot=0.06,
                 arms=(((38.0, 22.0), (45.0, 15.0), (40.0, 7.0)), ((23.0, 22.0), (27.0, 13.0), (35.0, 7.0))),
                 club=((44.0, 6.0), (39.0, 6.5), (8.0, 8.0)), clubFront=False)
    else:           # held: arms at full stretch, club dropped behind the back, body arched
        P.update(chest=(29.5, 29.5), belly=(30.0, 38.5), hip=(31.0, 42.0), head=(35.5, 15.0), rot=0.18, tilt=-0.15,
                 arms=(((37.0, 22.5), (41.0, 13.0), (35.0, 6.5)), ((22.5, 23.0), (24.0, 13.0), (31.0, 6.5))),
                 club=((36.5, 4.6), (33.0, 6.6), (9.0, 27.0)), clubFront=False)
    nh, fh = (35.0, 44.0), (27.5, 44.0)
    P["legs"] = ((nh, (40.0, 51.5), (40.0, 59.0)), (fh, (24.0, 51.5), (24.0, 59.0)))
    return P


def tyrant_windup(frame=0):
    F = Frame(64, 1.0, cx=32.0, base=62.0)
    _body(F, _windup_pose(frame % 3), eye=0)
    return finish(F.a, True, 64)


def _debris(F, pts, cols):
    for (x, y), c in zip(pts, cols):
        dot(F, x, y, c)
        dot(F, x + 1, y, c)


def tyrant_impact(frame=0):
    F = Frame(64, 1.0, cx=32.0, base=62.0)
    k = frame % 2
    bk, st = px.ramp(BK), px.ramp(ST)
    rise = (0.0, -1.5)[k]
    P = {
        "hip": (30.0, 42.0), "chest": (35.0, 32.0 + rise), "belly": (32.0, 39.0), "rot": -0.35,
        "head": (45.0, 24.0 + rise),
        "legs": (((34.0, 44.0), (42.0, 51.0), (42.0, 59.0)), ((27.0, 44.0), (22.0, 52.0), (20.0, 59.0))),
        "arms": (((42.0, 26.0 + rise), (48.0, 33.0), (46.0, 40.0)), ((28.0, 26.0 + rise), (36.0, 34.0), (44.0, 39.0))),
        "club": ((43.5, 37.0), (46.0, 40.0), (56.5, 54.5)), "clubFront": True,
    }
    _body(F, P)
    if k == 0:   # chips burst from the club head; dust low at its base
        _debris(F, [(50.0, 50.0), (60.0, 49.0), (53.0, 46.5), (59.5, 45.0)], [bk[0], bk[1], st[0], bk[0]])
        for x in (49.0, 51.0, 61.0):
            dot(F, x, 59.6, st[0])
    else:        # chips higher and wider, dust spreading
        _debris(F, [(49.0, 46.0), (60.5, 42.0), (54.0, 39.0), (47.5, 52.0)], [bk[1], bk[0], st[1], st[0]])
        for x in (46.0, 48.0, 50.0, 60.0, 61.0):
            dot(F, x, 59.6, st[1])
    return finish(F.a, True, 64)


def tyrant_death(frame=0):
    F = Frame(64, 1.0, cx=32.0, base=62.0)
    k = frame % 4
    gi, sm, gd, bk = px.ramp(GI), px.ramp(SM), px.ramp(GD), px.ramp(BK)
    if k == 0:   # stagger back, the club slips from the hand
        P = {"hip": (30.0, 42.0), "chest": (29.0, 29.5), "belly": (29.5, 38.0), "rot": 0.25, "head": (33.0, 15.5), "tilt": -0.3,
             "legs": (((34.0, 44.0), (38.0, 51.0), (40.0, 59.0)), ((27.0, 44.0), (22.0, 51.0), (21.0, 59.0))),
             "arms": (((37.0, 22.5), (45.0, 18.0), (50.0, 13.0)), ((22.0, 23.0), (16.0, 30.0), (13.0, 36.0))),
             "club": ((54.0, 40.0), (51.0, 43.0), (56.0, 58.0)), "clubFront": True}
        _body(F, P, eye=2)
    elif k == 1:  # down on one knee, club on the ground
        _club(F, (20.0, 59.5), (23.0, 59.5), (54.5, 58.5))
        P = {"hip": (30.0, 48.0), "chest": (33.0, 36.0), "belly": (31.0, 44.0), "rot": -0.3, "head": (41.0, 23.5), "tilt": 0.3,
             "legs": (((34.0, 50.0), (42.0, 52.5), (40.0, 59.0)), ((26.0, 50.0), (22.0, 58.0), (14.0, 59.0))),
             "arms": (((40.0, 30.0), (45.0, 39.0), (46.0, 47.0)), ((25.0, 30.0), (23.0, 40.0), (24.0, 48.0)))}
        _body(F, P, eye=2)
    elif k == 2:  # fallen face down, the club broken in two
        _club(F, (6.0, 59.0), (8.0, 59.0), (24.0, 58.5))
        tube(F, [(50.0, 59.0), (58.0, 57.5)], 2.6, 3.2, lit(bk))
        ell(F, 20.0, 57.0, 6.0, 3.6, 0.0, lambda x, y, u, v, it: gi[min(3, px.level(it, (0.80, 0.55, 0.32)) + 1)])   # legs
        ell(F, 33.0, 55.0, 12.0, 5.6, 0.0, lambda x, y, u, v, it: gi[min(3, px.level(it, (0.84, 0.60, 0.36)))])     # back
        tube(F, [(40.0, 54.0), (47.0, 58.0), (54.0, 58.5)], 3.0, 2.4, lit(gi))                                    # arm
        _belt(F, (24.0, 54.0))
        ell(F, 46.0, 55.0, 4.6, 4.0, 0.0, lambda x, y, u, v, it: gi[min(3, px.level(it, (0.84, 0.60, 0.36)))])    # head
        tube(F, [(44.0, 51.0), (40.0, 52.0)], 2.4, 1.6, lit(px.ramp(PT)))
        tube(F, [(43.0, 51.6), (48.0, 51.0), (50.0, 53.0)], 1.0, 1.0, lit(gd))
    else:         # ash heap: smoke mound, broken club pieces, the circlet left on top
        def ash(x, y, u, v, it):
            if v < -0.55 and (x + y) % 2:
                return None
            return sm[1 + min(1, px.level(it, (0.62,)))]
        ell(F, 32.0, 58.0, 18.0, 4.6, 0.0, ash)
        ell(F, 30.0, 55.0, 10.0, 4.0, 0.0, ash)
        _club(F, (6.0, 59.0), (8.0, 59.0), (22.0, 58.5))
        tube(F, [(46.0, 59.0), (56.0, 58.0)], 2.4, 3.0, lit(bk))
        tube(F, [(27.0, 52.0), (31.0, 51.0), (35.0, 52.0)], 1.0, 1.0, lit(gd))
        for i, (x, y) in enumerate(((26.0, 46.0), (33.0, 43.0), (38.0, 47.0))):
            tube(F, [(x, y + 4.0), (x - 1.0, y + 2.0), (x + 0.6, y)], 1.4, 0.6,
                 lambda X, Y, t, nx, ny, it: None if (X + Y + i) % 2 and t > 0.4 else sm[1])
    return finish(F.a, True, 64)


def boss_shadow(frame=0):
    """Art 03 shadow method (art-concept-03 scripts/fx03.py: shadow, key shadow.0), at 40 x 8."""
    w, h = 40, 8
    g = px.new(w, h)
    c = px.C("shadow.0")
    for y in range(h):
        for x in range(w):
            u, v = (x + 0.5 - w / 2) / (w / 2), (y + 0.5 - h / 2) / (h / 2)
            if u * u + v * v <= 1.0:
                g[y, x] = c
    return g


# ---------------- Slam warning reference ring and impact decal (separate overlay contract: src/telegraph_r03.json) ----------------
RING_R = 1.2 * 32          # Core radius 1.2 world units x PPU 32, at SOURCE density only (not a screen-pixel radius)
RING_W = 2.0               # source px per line


def slam_warning(frame=0):
    """Reference rendering: black outer line whose OUTER edge is the hit boundary, white line just inside, no fill.
    The runtime ring is drawn procedurally from Core's target/radius; this picture is for review and sanity only."""
    n, c = 78, 39.0
    a = px.new(n, n)
    for y in range(n):
        for x in range(n):
            d = math.hypot(x + 0.5 - c, y + 0.5 - c)
            if RING_R - RING_W < d <= RING_R:
                a[y, x] = K
            elif RING_R - 2 * RING_W < d <= RING_R - RING_W:
                a[y, x] = W
    return a


def slam_impact(frame=0):
    """Ground cracks, a dent and flying chips at the locked target (1 px cracks, no outline pass so they stay thin).
    All content within 34 px of the centre (< 38.4 nominal ring radius)."""
    n, c = 72, 36.0
    k = frame % 2
    a = px.new(n, n)
    bk, st, dr = px.ramp(BK), px.ramp(ST), px.ramp("drift")
    for y in range(n):
        for x in range(n):
            dx, dy = x + 0.5 - c, y + 0.5 - c
            r = math.hypot(dx, dy)
            edge = 6.0 + 0.9 * math.sin(math.atan2(dy, dx) * 3.0 + 0.7)
            if r <= edge:
                if r > edge - 1.0:
                    a[y, x] = K                                  # dent rim
                elif dx + dy < -2.0:
                    a[y, x] = bk[3]                              # far wall of the dent in shade
                else:
                    a[y, x] = bk[2]

    def crack(ang, length, seed):
        x0, y0 = c + math.cos(ang) * 5.0, c + math.sin(ang) * 5.0
        steps = max(3, int(length / 4.0))
        for s_ in range(1, steps + 1):
            rr = 5.0 + (length - 5.0) * s_ / steps
            wob = 1.3 * math.sin(seed * 2.1 + s_ * 1.9)
            x1 = c + math.cos(ang) * rr - math.sin(ang) * wob
            y1 = c + math.sin(ang) * rr + math.cos(ang) * wob
            px.line(a, int(x0), int(y0), int(x1), int(y1), K)
            if s_ == steps // 2 and length > 16:                 # one branch on the long cracks
                bx = x1 + math.cos(ang + 0.7) * length * 0.3
                by = y1 + math.sin(ang + 0.7) * length * 0.3
                px.line(a, int(x1), int(y1), int(bx), int(by), K)
            x0, y0 = x1, y1

    for i, (ang, length) in enumerate(((0.2, 22.0), (1.15, 14.0), (2.05, 25.0), (2.9, 12.0), (3.75, 20.0), (4.6, 15.0), (5.45, 24.0))):
        crack(ang, length, i)
    chips = (((0.9, 12.5), (2.0, 15.0), (3.8, 12.0), (5.0, 14.0), (0.0, 26.0), (2.9, 25.0)),
             ((0.9, 21.0), (2.0, 24.0), (3.8, 20.0), (5.0, 23.0), (0.0, 31.0), (2.9, 30.5)))[k]
    for j, (ang, rr) in enumerate(chips):
        x, y = int(c + math.cos(ang) * rr), int(c + math.sin(ang) * rr)
        col = (bk[0], st[0], dr[0])[j % 3]
        px.put(a, x, y, col)
        px.put(a, x + 1, y, col)
        px.put(a, x, y + 1, col)
        px.put(a, x + 1, y + 1, K)                                # chip's dark underside
    ring_r, gap = ((8.6, 2), (12.0, 3))[k]                       # dust puffs around the dent
    for t in range(28):
        if t % gap:
            continue
        ang = t * TAU / 28
        x, y = int(c + math.cos(ang) * ring_r), int(c + math.sin(ang) * ring_r)
        px.put(a, x, y, dr[0])
        px.put(a, x + 1, y, dr[1])
    return a


GENERATORS = {
    "enm_c1-violence-shadow_move_32": violence_shadow,
    "enm_c2-timber-devourer_move_32": timber_devourer,
    "enm_c3-wreckage-giant_move_32": wreckage_giant,
    "bos_nephilim-tyrant_move_64": tyrant_move,
    "bos_nephilim-tyrant_windup_64": tyrant_windup,
    "bos_nephilim-tyrant_impact_64": tyrant_impact,
    "bos_nephilim-tyrant_death_64": tyrant_death,
    "fx_shadow-boss_static_40x8": boss_shadow,
    "fx_boss-slam-warning_static_78": slam_warning,
    "fx_boss-slam-impact_hit_72": slam_impact,
}
