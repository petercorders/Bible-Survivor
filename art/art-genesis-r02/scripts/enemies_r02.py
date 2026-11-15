"""Procedural R02 (Cain round) enemies in the art 03 style: B1 envy eye, B2 rage hound, B3 cracked mask.
Same method as art 03 scripts/enemies03.py (copied helpers: scripts/px03.py, byte-identical): shapes authored on a
32-unit grid, lit from the upper left (px03.LIGHT), colours only from palette ramps (art 03 master-v0.3 plus
src/palette_r02_ext.json), 1 px outline.0 outline, ground sprites shifted so their lowest row is the foot row.
Deterministic (no randomness). Needs numpy. Each generator takes a frame index 0..3 and returns a 32x32 RGBA array.
Authored by Claude (Anthropic model) in this session as a script; it is not output of the Claude Design product."""
import json, math, os
import numpy as np
import px03 as px

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.dirname(HERE)
EXT = json.load(open(os.path.join(ROOT, "src", "palette_r02_ext.json"), encoding="utf-8"))
for _ramp, _cols in EXT["actor"].items():
    if f"{_ramp}.0" in px.PAL:
        raise SystemExit(f"palette_r02_ext ramp '{_ramp}' collides with the base palette")
    for _i, _c in enumerate(_cols):
        _v = px.hex_rgba(_c)
        px.PAL[f"{_ramp}.{_i}"] = _v
        px.NAME_OF.setdefault(_v, f"{_ramp}.{_i}")
        px.RAMP_OF.setdefault(_v, _ramp)

K = px.C("outline.0")


class Frame:
    """Same mapping as art 03: authored point (cx, base) lands on the cell's (centre, foot row)."""
    def __init__(self, s, g, cx=16.0, base=30.0):
        self.n = int(round(32 * s))
        self.S = s * g
        self.ox = self.n / 2 - cx * self.S
        self.oy = (self.n - 1.5) - base * self.S
        self.a = px.new(self.n, self.n)

    def P(self, x, y):
        return x * self.S + self.ox, y * self.S + self.oy

    def R(self, r):
        return r * self.S

    def U(self, x, y):
        """pixel centre -> authored units"""
        return (x + 0.5 - self.ox) / self.S, (y + 0.5 - self.oy) / self.S


# ---------------- B1 envy eye: a floating cluster of eyes whose slit pupils look along the travel direction ----------------
def envy_eye(frame=0, s=1.0):
    F = Frame(s, 0.90, base=27.0)
    a = F.a
    bn, fl, ev = px.ramp("bone"), px.ramp("fruit"), px.ramp("envy")
    ph = frame * math.pi / 2
    bob = (0.0, -0.7, -1.1, -0.5)[frame % 4]
    look = (0.0, 0.5, 0.0, -0.5)[frame % 4]          # pupils dart forward and back

    # fleshy stalks hanging below and behind the eyes (waving)
    for k, (x0, ln, amp) in enumerate(((12.0, 6.0, 1.3), (16.5, 7.5, 1.7), (21.0, 5.5, 1.1))):
        pts = [(x0 - 2.0 * t + amp * t * math.sin(ph + k * 2.1 + t * 2.6), 19.0 + bob + t * ln)
               for t in (i / 4 for i in range(5))]
        px.tube(a, [F.P(*p) for p in pts], lambda t: F.R(1.6 - 1.0 * t),
                lambda x, y, t, nx, ny, it: fl[min(3, px.level(it, (0.86, 0.60, 0.34)) + 1)])

    def eyeball(cx, cy, rx, ry, iris_r, closed=False, veins=()):
        X, Y = F.P(cx, cy)
        ix, iy = X + F.R(rx * 0.36 + look), Y + F.R(0.4)
        IR = F.R(iris_r)

        def col(x, y, u, v, it):
            lid = -0.64 + 0.08 * math.sin(u * 3.0 + 0.5)
            fidx = min(3, px.level(it, (0.86, 0.62, 0.36)) + 1)
            if closed:
                if abs(v - 0.15) < 0.16:
                    return fl[3]                          # shut lid seam
                return fl[fidx]
            if v < lid:
                return fl[fidx]                           # heavy red upper lid, lit upper left
            if v < lid + 0.14:
                return fl[3]                              # lid crease
            if v > 0.84:
                return fl[2]                              # lower rim
            dx, dy = x + 0.5 - ix, y + 0.5 - iy
            d = math.hypot(dx, dy)
            if d < IR:
                if abs(dx) < max(0.5, IR * 0.18) and abs(dy) < IR * 0.8:
                    return K                              # slit pupil
                if d > IR - 0.9:
                    return ev[3]                          # dark iris ring
                return ev[1] if dx + dy < 0 else ev[2]
            return bn[min(3, px.level(it, (0.80, 0.54, 0.28)))]

        px.shade_ellipse(a, X, Y, F.R(rx), F.R(ry), 0.0, col)
        if not closed:
            hx, hy = int(ix - IR * 0.5), int(iy - IR * 0.55)
            if a[hy, hx, 3] and tuple(a[hy, hx]) in (tuple(ev[1]), tuple(ev[2])):
                px.put(a, hx, hy, px.C("white.0"))        # wet highlight on the iris
            for vx, vy in veins:                          # bloodshot veins on the sclera only
                x, y = (int(v) for v in F.P(cx + vx, cy + vy))
                if a[y, x, 3] and px.RAMP_OF.get(tuple(int(c) for c in a[y, x])) == "bone":
                    px.put(a, x, y, fl[1])

    eyeball(7.6, 8.6 + bob, 4.2, 3.8, 1.8, closed=(frame % 4 == 2))
    eyeball(6.2, 17.6 + bob, 3.8, 3.4, 1.6, closed=(frame % 4 == 0))
    eyeball(18.0, 14.0 + bob, 8.2, 7.8, 3.4, veins=((-6.0, 1.2), (-5.2, 2.2), (-6.2, 3.4), (-4.4, 4.4)))
    return px.outline(a)


# ---------------- B2 rage hound: low, long running body, ears swept back, open jaw, flaring hackles ----------------
def rage_hound(frame=0, s=1.0):
    F = Frame(s, 0.92, base=30.0)
    a, n = F.a, F.n
    rg, bn, sp = px.ramp("rage"), px.ramp("bone"), px.ramp("spark")
    f = frame % 4
    by = 20.0 + (0.0, -0.6, -1.2, -0.5)[f]
    # (near foot, far foot) per frame: 0 stretched, 1 front planted, 2 gathered, 3 hind push / front reach.
    # Every frame keeps at least one paw on the foot row (y = 30).
    FRONT = [((28.4, 30.0), (26.2, 29.0)), ((21.8, 30.0), (19.6, 29.2)),
             ((18.0, 28.2), (19.8, 29.0)), ((26.2, 27.0), (24.4, 27.6))][f]
    HIND = [((3.6, 30.0), (5.6, 29.2)), ((9.2, 27.6), (7.6, 28.2)),
            ((14.6, 30.0), (12.6, 29.6)), ((7.6, 30.0), (5.6, 29.4))][f]
    shoulder, hip = (21.2, by + 1.6), (9.6, by + 1.2)

    def leg(anchor, foot, near, front):
        mx, my = (anchor[0] + foot[0]) / 2, (anchor[1] + foot[1]) / 2
        knee = (mx + (-0.9 if front else 1.1), my)
        shade = 1 if near else 2

        def paint(x, y, t, nx, ny, it):
            return rg[min(3, px.level(it, (0.86, 0.62, 0.36)) + shade)]
        px.tube(a, [F.P(*anchor), F.P(*knee), F.P(*foot)], lambda t: F.R(1.7 - 0.9 * t), paint)
        fx, fy = (int(v) for v in F.P(*foot))
        px.put(a, fx, fy, rg[3])
        px.put(a, fx + 1, fy, rg[3])

    leg(shoulder, FRONT[1], False, True)
    leg(hip, HIND[1], False, False)
    # tail, whipping behind
    wag = (0.0, -0.9, -0.3, 0.6)[f]
    px.tube(a, [F.P(6.4, by - 1.0), F.P(3.8, by - 2.6 + wag * 0.5), F.P(1.8, by - 4.4 + wag)],
            lambda t: F.R(1.3 - 0.8 * t),
            lambda x, y, t, nx, ny, it: sp[1] if t > 0.86 else rg[min(3, px.level(it, (0.84, 0.58, 0.32)) + 1)])

    def body_col(x, y, u, v, it):
        idx = px.level(it, (0.84, 0.58, 0.32))
        if v > 0.55:
            return rg[3]                                  # dark belly
        if u * u + v * v > 0.70 and (u + v) < -0.45:
            return rg[0]                                  # orange rim light upper left
        return rg[min(3, idx + (1 if idx == 0 else 0))]

    for cx, cy, rx, ry, rot in ((15.4, by, 8.8, 3.8, -0.06), (9.2, by - 0.2, 4.4, 4.0, 0.0), (20.8, by + 0.4, 4.2, 4.4, 0.0)):
        X, Y = F.P(cx, cy)
        px.shade_ellipse(a, X, Y, F.R(rx), F.R(ry), rot, body_col)
    # hackles: a jagged ridge of raised fur along the back that flares frame by frame
    flare = (0.0, 0.7, 1.2, 0.4)[f]
    for i, bx in enumerate((8.6, 11.0, 13.4, 15.8, 18.2)):
        L = 2.0 + 0.8 * ((i + f) % 2) + flare * (0.5 if i % 2 else 0.25)
        x0, y0 = F.P(bx, by - 3.3)
        x1, y1 = F.P(bx - 1.6, by - 3.3 - L)
        steps = int(F.R(L) * 2) + 2
        for k in range(steps):
            t = k / (steps - 1)
            px.put(a, int(x0 + (x1 - x0) * t), int(y0 + (y1 - y0) * t), rg[1] if t < 0.45 else (rg[0] if t < 0.8 else sp[1]))
    # neck and head
    px.tube(a, [F.P(20.6, by - 1.4), F.P(23.0, by - 3.4), F.P(24.8, by - 4.6)], lambda t: F.R(2.6 - 0.5 * t),
            lambda x, y, t, nx, ny, it: rg[0] if (nx + ny) < -0.9 else rg[min(3, px.level(it, (0.84, 0.58, 0.32)) + 1)])
    for cx, cy, rx, ry, rot in ((25.6, by - 5.4, 3.2, 2.5, 0.15), (28.8, by - 4.4, 2.4, 1.4, 0.10)):
        X, Y = F.P(cx, cy)
        px.shade_ellipse(a, X, Y, F.R(rx), F.R(ry), rot, body_col)
    # ears swept back (far ear first, darker)
    for base_, tip, shade in (((25.4, by - 7.2), (23.6, by - 10.2), 2), ((24.2, by - 7.0), (21.6, by - 10.8), 1)):
        px.tube(a, [F.P(*base_), F.P(*tip)], lambda t: F.R(1.3 - 1.0 * t),
                lambda x, y, t, nx, ny, it, shade=shade: rg[min(3, px.level(it, (0.84, 0.58, 0.32)) + shade)])
    # open jaw: dark mouth, pale fangs, lower jaw
    jaw = (0.0, 0.4, 0.7, 0.2)[f]
    mx0, my0 = (int(v) for v in F.P(26.4, by - 3.6))
    mx1 = int(F.P(30.6, 0)[0])
    for x in range(mx0, mx1 + 1):
        px.put(a, x, my0, K)
    px.line(a, *(int(v) for v in F.P(26.2, by - 2.6)), *(int(v) for v in F.P(30.2, by - 2.4 + jaw)), rg[2])
    for tx in (27.6, 29.6):
        x, y = (int(v) for v in F.P(tx, by - 3.6))
        px.put(a, x, y, bn[0])
    ex, ey = (int(v) for v in F.P(26.6, by - 6.0))
    px.put(a, ex, ey, px.C("eye.0"))
    px.put(a, ex - 1, ey - 1, K)
    px.put(a, ex, ey - 1, K)
    # near legs over the body
    leg(shoulder, FRONT[0], True, True)
    leg(hip, HIND[0], True, False)
    a = px.outline(a)
    return px.ground_to(a, n - 2)


# ---------------- B3 cracked mask: a wide porcelain mask split by a glowing crack over a heavy dragging shroud ----------------
def cracked_mask(frame=0, s=1.0):
    F = Frame(s, 0.92, base=30.0)
    a, n = F.a, F.n
    sh, mk, rg, sp = px.ramp("shroud"), px.ramp("mask"), px.ramp("rage"), px.ramp("spark")
    f = frame % 4
    bob = (0.0, 0.8, 0.2, 1.0)[f]                       # sinks on the two heavy steps
    sway = (-0.8, 0.0, 0.8, 0.0)[f]
    glow = (1, 0, 1, 2)[f]
    cx, top, hem = 16.0, 13.0 + bob, 30.0
    for y in range(n):
        for x in range(n):
            ux, uy = F.U(x, y)
            if uy < top or uy > hem + 0.5:
                continue
            t = (uy - top) / (hem - top)
            half = 6.2 + 6.0 * t ** 0.8
            c = cx + sway * t
            if abs(ux - c) > half:
                continue
            tooth = 0.9 * (1 + math.sin((ux - c) * 1.7 + sway * 2.2))
            if uy > hem + 0.5 - tooth:
                continue
            side = (ux - c) / half
            idx = 1 if side < -0.40 else (2 if side < 0.35 else 3)
            if math.sin((ux - c) * 1.3 + 0.4 + sway) > 0.86 and idx < 3:
                idx += 1                                  # heavy vertical folds
            if side < -0.78 and t < 0.65:
                idx = 0                                   # lit rim on the left
            a[y, x] = sh[idx]
    mxc, myc = 16.0 + sway * 0.25, 11.0 + bob
    notch = [(F.P(18.0, 1.0 + bob)), (F.P(21.5, 2.0 + bob)), (F.P(18.2, 4.8 + bob))]

    def in_notch(x, y):
        (x0, y0), (x1, y1), (x2, y2) = notch
        px_, py_ = x + 0.5, y + 0.5
        d1 = (px_ - x1) * (y0 - y1) - (x0 - x1) * (py_ - y1)
        d2 = (px_ - x2) * (y1 - y2) - (x1 - x2) * (py_ - y2)
        d3 = (px_ - x0) * (y2 - y0) - (x2 - x0) * (py_ - y0)
        neg, pos = d1 < 0 or d2 < 0 or d3 < 0, d1 > 0 or d2 > 0 or d3 > 0
        return not (neg and pos)

    def mask_col(x, y, u, v, it):
        if v > 0.30 and abs(u) > 1.0 - (v - 0.30) * 0.85:
            return None                                   # tapered chin
        if in_notch(x, y):
            return None                                   # a chip broken off the brow
        idx = px.level(it, (0.80, 0.55, 0.30))
        return mk[min(3, idx)]

    X, Y = F.P(mxc, myc)
    px.shade_ellipse(a, X, Y, F.R(10.4), F.R(8.8), 0.0, mask_col)
    # brow ridges and eye holes
    for ex_, ey_ in ((12.0, 10.0), (20.2, 10.6)):
        EX, EY = F.P(mxc - 16.0 + ex_, ey_ + bob)
        px.line(a, int(EX - F.R(2.4)), int(EY - F.R(2.0)), int(EX + F.R(2.0)), int(EY - F.R(1.8)), mk[2])
        px.shade_ellipse(a, EX, EY, F.R(2.3), F.R(1.4), 0.0, lambda x, y, u, v, it: K)
        px.put(a, int(EX + F.R(0.4)), int(EY), sp[1] if glow else rg[1])
    # down-turned mouth slit
    pts = [(12.0, 16.8), (13.4, 16.0), (16.0, 16.0), (18.6, 16.2), (20.0, 17.0)]
    for (x0, y0), (x1, y1) in zip(pts, pts[1:]):
        px.line(a, *(int(v) for v in F.P(mxc - 16.0 + x0, y0 + bob)), *(int(v) for v in F.P(mxc - 16.0 + x1, y1 + bob)), K)
    # the crack: zigzag from the broken brow to the chin, dark with an ember glow that pulses
    crack = [(18.0, 3.6), (15.8, 6.6), (17.6, 9.2), (15.4, 12.8), (16.8, 15.6), (15.0, 19.6)]
    glow_col = (rg[1], rg[0], sp[1])[glow]
    for (x0, y0), (x1, y1) in zip(crack, crack[1:]):
        X0, Y0 = F.P(mxc - 16.0 + x0, y0 + bob)
        X1, Y1 = F.P(mxc - 16.0 + x1, y1 + bob)
        steps = int(max(abs(X1 - X0), abs(Y1 - Y0))) + 1
        for k in range(steps + 1):
            t = k / steps
            x, y = int(X0 + (X1 - X0) * t), int(Y0 + (Y1 - Y0) * t)
            if a[y, x, 3]:
                px.put(a, x, y, K)
                if a[y, x + 1, 3] and px.RAMP_OF.get(tuple(int(c) for c in a[y, x + 1])) == "mask" and (x + y + glow) % 2 == 0:
                    px.put(a, x + 1, y, glow_col)
    a = px.outline(a)
    return px.ground_to(a, n - 2)


GENERATORS = {
    "enm_b1-envy-eye_move_32": envy_eye,
    "enm_b2-rage-hound_run_32": rage_hound,
    "enm_b3-cracked-mask_move_32": cracked_mask,
}


def sheet(asset_id, frames=4):
    """Horizontal strip, frame i at x = i*32 (no padding)."""
    return np.concatenate([GENERATORS[asset_id](k) for k in range(frames)], axis=1)
