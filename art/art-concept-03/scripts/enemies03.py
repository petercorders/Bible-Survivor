"""Procedural enemies for art 03 (carried over from the art-02 module).
Each generator takes a frame index (0..3) and a cell scale s (1.0 = 32px cell, 1.5 = 48px comparison).
Shapes are authored on a 32-unit grid; G() maps them into the cell with a per-enemy body size g so
the creature keeps a margin inside the cell. Colours come only from palette ramps."""
import math
import numpy as np
import px03 as px

K = px.C("outline.0")


class Frame:
    def __init__(self, s, g, cx=16.0, base=30.0):
        self.n = int(round(32 * s))
        self.S = s * g
        # keep authored point (cx, base) at the cell's (centre, foot row)
        self.ox = self.n / 2 - cx * self.S
        self.oy = (self.n - 1.5) - base * self.S
        self.a = px.new(self.n, self.n)

    def P(self, x, y):
        return x * self.S + self.ox, y * self.S + self.oy

    def R(self, r):
        return r * self.S


# ---------------- A1 small snake ----------------
def snake(frame=0, s=1.0):
    F = Frame(s, 0.80)
    a, n = F.a, F.n
    sn, bl = px.ramp("snake"), px.ramp("belly")
    ph = frame * math.pi / 2
    pts = []
    for i in range(8):
        u = i / 7
        amp = 1.2 + 1.3 * u
        pts.append((3.0 + u * 19.0, 26.2 + amp * math.sin(u * 2.4 * math.pi - ph)))
    pts += [(23.8, pts[-1][1] - 2.0), (23.6, 20.5), (24.6, 18.6)]
    pts = [F.P(x, y) for x, y in pts]

    def rad(t):
        if t < 0.18:
            r = 0.55 + (2.1 - 0.55) * (t / 0.18)
        elif t < 0.85:
            r = 2.1 + 0.25 * math.sin((t - 0.18) / 0.67 * math.pi)
        else:
            r = 2.05
        return F.R(r)

    period = F.R(5.2)
    total = [1.0]

    def paint(x, y, t, nx, ny, it):
        idx = px.level(it, (0.83, 0.58, 0.33))
        if ny > 0.42 and t > 0.10:
            return bl[0] if it > 0.55 else bl[1]
        if t > 0.12 and (t * total[0]) % period < period * 0.34 and ny < 0.25:
            idx = 2 if idx < 2 else 3
        return sn[min(idx, 3)]

    total[0] = 26.0 * F.S
    px.tube(a, pts, rad, paint)
    hx, hy = F.P(25.6, 16.4)

    def head_col(x, y, u, v, it):
        if v > 0.38:
            return bl[0] if u < 0.1 else bl[1]
        idx = px.level(it, (0.80, 0.55, 0.30))
        if -0.7 < u < 0.15 and abs(v + 0.45) < 0.22 and idx >= 1:
            idx = min(3, idx + 1)
        return sn[idx]

    px.shade_ellipse(a, hx, hy, F.R(3.7), F.R(2.7), 0.10, head_col)
    ex, ey = (int(v) for v in F.P(26.7, 15.4))
    px.put(a, ex, ey, px.C("eye.0"))
    px.put(a, ex + 1, ey, K)
    if s > 1.2:
        px.put(a, ex, ey - 1, px.C("eye.0"))
        px.put(a, ex + 1, ey - 1, px.C("white.0"))
    nx_, ny_ = (int(v) for v in F.P(28.7, 16.0))
    px.put(a, nx_, ny_, sn[3])
    x0, my = (int(v) for v in F.P(25.4, 17.5))
    x1 = int(F.P(29.0, 0)[0])
    for x in range(x0, x1 + 1):
        if a[my, x, 3]:
            a[my, x] = sn[3]
    if frame in (0, 1):
        tx, ty = (int(v) for v in F.P(29.7, 17.5))
        tg = px.C("tongue.0")
        for dx, dy in ((0, 0), (1, 0), (2, -1), (2, 1)):
            px.put(a, tx + dx, ty + dy, tg)
    a = px.outline(a)
    return px.ground_to(a, n - 2)


# ---------------- A2 fruit moth ----------------
def moth(frame=0, s=1.0):
    F = Frame(s, 0.86, base=24.0)
    a, n = F.a, F.n
    wg, fr, lf, gold = px.ramp("wing"), px.ramp("fruit"), px.ramp("leaf"), px.ramp("gold")
    flap = (1.0, 0.3, -1.0, -0.3)[frame % 4]     # 03: down-stroke and up-stroke mid poses differ
    bob = (-0.6, -0.2, 0.6, 0.2)[frame % 4]
    cx, root_y = 16.0, 11.2 + bob

    def wing(wx, wy, rx, ry, rot, spot, upper):
        cwx, cwy = F.P(wx, wy)
        rootx, rooty = F.P(cx, root_y)
        RX, RY = F.R(rx), F.R(ry)
        cr, sr = math.cos(rot), math.sin(rot)
        for y in range(n):
            for x in range(n):
                dx, dy = x + 0.5 - cwx, y + 0.5 - cwy
                u = (dx * cr + dy * sr) / RX
                v = (-dx * sr + dy * cr) / RY
                ang = math.atan2(v, u)
                lim = 1.0 - 0.10 * abs(math.sin(ang * 3.0 + 0.6))
                r = math.hypot(u, v)
                if r > lim:
                    continue
                rho = min(1.0, math.hypot(x + 0.5 - rootx, (y + 0.5 - rooty) * 1.15) / (RX * 1.9))
                if r > lim - 0.17:
                    col = wg[2]                       # dark margin
                elif rho < 0.30:
                    col = wg[3]                       # furry base near the body
                elif rho < 0.47:
                    col = wg[2]
                elif r > lim - 0.38:
                    col = wg[0] if upper else wg[1]   # pale submarginal band
                else:
                    col = wg[1]
                if 0.34 < rho < 0.9 and r < lim - 0.17 and r > 0.25:
                    for k in (-0.55, 0.05, 0.6):      # three veins fanning from the root
                        if abs(v - k * u) < 0.07 / max(0.3, r):
                            col = wg[2]
                ex, ey, er = spot
                d = math.hypot(u - ex, v - ey)
                if er and d < er:
                    col = fr[3] if d < er * 0.5 else gold[1]
                    if d < er * 0.2:
                        col = px.C("white.0")
                a[y, x] = col

    for side in (-1, 1):
        wing(cx + side * 5.0, root_y + 3.6 - 0.6 * flap, 4.4, 3.0 - 0.5 * abs(flap),
             side * (0.35 + 0.15 * flap), (0, 0, 0), False)
        wing(cx + side * (6.6 + 0.4 * (1 - abs(flap))), root_y - 1.6 - 1.6 * flap, 6.8, 4.3 - 0.9 * abs(flap),
             side * (-0.42 - 0.32 * flap), (0.30 * side, 0.05, 0.36), True)
    tx, ty = F.P(cx, root_y - 0.2)
    px.lit_ellipse(a, tx, ty, F.R(2.3), F.R(2.0), [wg[2], wg[3], px.C("outline.1"), px.C("outline.1")],
                   cuts=(0.75, 0.45, 0.2))
    bx, by = F.P(cx, root_y + 4.6)

    def fruit_col(x, y, u, v, it):
        idx = px.level(it, (0.84, 0.58, 0.30))
        if idx == 1 and v > -0.2 and (x * 7 + y * 13) % 11 == 0:
            idx = 0
        return fr[idx]

    px.shade_ellipse(a, bx, by, F.R(3.3), F.R(4.1), 0.0, fruit_col)
    hl = F.P(cx - 1.4, root_y + 2.4)
    px.put(a, int(hl[0]), int(hl[1]), px.C("white.0"))
    tip = int(round(by + F.R(4.1)))
    for dx, dy, c in ((0, 0, lf[2]), (-1, -1, lf[1]), (1, -1, lf[1]), (0, -1, fr[3])):
        px.put(a, int(bx) + dx, tip + dy, c)
    lx, ly = (int(v) for v in F.P(cx + 2.0, root_y - 2.4))
    for dx, dy, c in ((0, 0, lf[1]), (1, 0, lf[0]), (1, -1, lf[0]), (2, -1, lf[1])):
        px.put(a, lx + dx, ly + dy, c)
    hx, hy = F.P(cx, root_y - 2.9)
    px.lit_ellipse(a, hx, hy, F.R(1.7), F.R(1.4), [wg[3], px.C("outline.1"), px.C("outline.1"), K], cuts=(0.8, 0.5, 0.2))
    px.put(a, int(hx - F.R(1.0)), int(hy), px.C("eye.1"))
    px.put(a, int(hx + F.R(0.6)), int(hy), px.C("eye.1"))
    for side in (-1, 1):
        x0, y0 = hx + side * F.R(0.6), hy - F.R(1.2)
        x1, y1 = hx + side * F.R(4.0), hy - F.R(4.6)
        px.line(a, int(x0), int(y0), int(x1), int(y1), px.C("outline.1"))
        px.put(a, int(x1) + side, int(y1), px.C("outline.1"))
    return px.outline(a)


# ---------------- A3 thorn beast ----------------
def beast(frame=0, s=1.0):
    """A3 thorn beast, 4-frame walk (6 fps). 03: darker plum 'beast' ramp with a lit rim so the body
    separates from grass AND the dirt path; body bobs 1 unit on the passing frames; diagonal leg pairs
    lift 2 units and swing forward/back."""
    F = Frame(s, 0.84, base=30.0)
    a, n = F.a, F.n
    bk, ms, th = px.ramp("beast"), px.ramp("moss"), px.ramp("thorn")
    swing = (1, 0, -1, 0)[frame % 4]            # contact A, passing, contact B, passing
    lifted = (None, 0, None, 1)[frame % 4]       # which diagonal pair (i % 2) is in the air
    bob = -1.2 if frame % 2 else 0.0
    roots = [((9.0, 21.0 + bob), (7.0, 25.5), (5.5 + 1.6 * swing, 30.0)),
             ((13.0, 22.0 + bob), (12.5, 26.5), (11.8 - 1.6 * swing, 30.0)),
             ((19.0, 22.0 + bob), (20.0, 26.5), (20.6 + 1.6 * swing, 30.0)),
             ((23.0, 20.5 + bob), (25.0, 25.0), (26.8 - 1.6 * swing, 30.0))]
    for i, (p0, p1, p2) in enumerate(roots):
        lift = 2.0 if lifted is not None and i % 2 == lifted else 0.0
        pts = [F.P(*p0), F.P(p1[0], p1[1] - lift * 0.6), F.P(p2[0], p2[1] - lift)]
        px.tube(a, pts, lambda t: F.R(1.9 - 1.2 * t),
                lambda x, y, t, nx, ny, it: bk[min(3, px.level(it, (0.86, 0.62, 0.36)) + 1)])
        tx, ty = F.P(p2[0], p2[1] - lift)
        px.put(a, int(tx) - 1, int(ty) - 1, bk[3])
        px.put(a, int(tx) + 1, int(ty) - 1, bk[2])

    lobes = [(9.0, 17.5, 5.6, 5.2, 0.5, 0.9), (15.5, 17.0, 8.6, 6.4, -0.1, -0.7),
             (12.0, 11.5, 6.4, 5.0, -0.45, 1.0), (21.5, 14.5, 5.6, 5.0, 0.2, -0.6)]
    for li, (lx, ly, rx, ry, rot, tw) in enumerate(lobes):
        cx_, cy_ = F.P(lx, ly + bob)

        def col(x, y, u, v, it, tw=tw, li=li):
            idx = px.level(it, (0.80, 0.60, 0.34))
            if u * u + v * v > 0.80 and (u + v) > 0.25:
                return bk[3]                                   # contour between stem bundles
            if u * u + v * v > 0.62 and (u + v) < -0.55:
                return bk[0]                                   # lit rim on the upper-left edge
            if li == 2 and v < -0.45 and u < 0.35:
                return ms[0] if it > 0.62 else ms[1]           # moss cap on the hump
            fibre = math.sin((u * math.cos(tw) + v * math.sin(tw)) * 7.0 + li * 1.3)
            if fibre > 0.82 and idx < 3:
                idx += 1                                       # twisted stem fibres (thin lines)
            return bk[idx]

        px.shade_ellipse(a, cx_, cy_, F.R(rx), F.R(ry), rot, col)

    thorns = [(6.0, 13.0, 200, 4), (8.0, 8.5, 235, 4), (12.5, 6.8, 265, 5), (16.5, 8.0, 300, 4),
              (20.5, 10.2, 290, 4), (25.5, 12.5, 335, 3), (4.5, 18.5, 185, 4), (11.0, 21.5, 120, 2),
              (24.5, 18.5, 20, 3), (15.0, 11.5, 250, 3), (18.5, 14.0, 300, 2)]
    for bxp, byp, ang, ln in thorns:
        ang = math.radians(ang)
        ux, uy = math.cos(ang), math.sin(ang)
        L = F.R(ln)
        bx0, by0 = F.P(bxp, byp + bob)
        steps = int(L * 2) + 1
        for i in range(steps):
            t = i / max(1, steps - 1)
            x, y = bx0 + ux * L * t, by0 + uy * L * t
            px.put(a, int(x), int(y), bk[2] if t < 0.35 else (th[1] if t < 0.75 else th[0]))
            if t < 0.3:
                px.put(a, int(x - uy * 0.9), int(y + ux * 0.9), bk[3])
    fx, fy = F.P(22.6, 15.6 + bob)
    for y in range(n):
        for x in range(n):
            u, v = (x + 0.5 - fx) / F.R(3.0), (y + 0.5 - fy) / F.R(2.2)
            r2 = u * u + v * v
            if r2 <= 1.0 and a[y, x, 3]:
                a[y, x] = K if r2 < 0.62 else bk[3]
    e = px.C("eye.1")
    for ex in (-1.4, 1.2):
        x, y = int(fx + F.R(ex)), int(fy - F.R(0.4))
        px.put(a, x, y, e)
        px.put(a, x + (1 if ex > 0 else -1), y - 1, e)
    for tx in (-1.0, 0.6, 2.0):
        px.put(a, int(fx + F.R(tx)), int(fy + F.R(1.3)), th[0])
    for bxp, byp in ((10.5, 9.0), (14.0, 8.0)):
        x, y = (int(v) for v in F.P(bxp, byp + bob))
        px.put(a, x, y, px.C("fruit.2"))
        px.put(a, x - 1, y, px.C("fruit.1"))
    a = px.outline(a)
    return px.ground_to(a, n - 2)
