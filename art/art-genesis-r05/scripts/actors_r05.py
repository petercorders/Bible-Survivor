"""Procedural R05 (Lot) enemies in the art 03 style: E1 greed maw, E2 lingering hand, E3 smoke chaser (move loops).
Same method as R03 / R04 (copied helpers: scripts/px03.py, byte-identical with art 03): shapes authored on the cell's
pixel grid, lit from the upper left (px03.LIGHT), colours only from palette ramps (art 03 master-v0.3 plus
src/palette_r05_ext.json), 1 px outline.0 outline, ground sprites shifted so their lowest row is the foot row.
Deterministic (no randomness). Needs numpy. Each generator takes a frame index and returns a 32x32 RGBA array.
Authored by Claude (Anthropic model) in this session as a script; it is not output of the Claude Design product."""
import json, math, os
import numpy as np
import px03 as px

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.dirname(HERE)
EXT = json.load(open(os.path.join(ROOT, "src", "palette_r05_ext.json"), encoding="utf-8"))
for _ramp, _cols in EXT["actor"].items():
    if f"{_ramp}.0" in px.PAL:
        raise SystemExit(f"palette_r05_ext ramp '{_ramp}' collides with the base palette")
    for _i, _c in enumerate(_cols):
        _v = px.hex_rgba(_c)
        px.PAL[f"{_ramp}.{_i}"] = _v
        px.NAME_OF.setdefault(_v, f"{_ramp}.{_i}")
        px.RAMP_OF.setdefault(_v, _ramp)

N = 32
FOOT = N - 2                 # foot row for pivot (16, 1)
K = px.C("outline.0")
K2 = px.C("outline.1")


def new():
    return px.new(N, N)


def lit(rmp, cuts=(0.84, 0.58, 0.32), shift=0):
    return lambda x, y, t, nx, ny, it: rmp[min(len(rmp) - 1, px.level(it, cuts) + shift)]


def ell_lit(rmp, cuts=(0.84, 0.58, 0.32), shift=0, rim=None):
    def fn(x, y, u, v, it):
        if rim is not None and u * u + v * v > 0.72 and (u + v) < -0.6:
            return rim
        return rmp[min(len(rmp) - 1, px.level(it, cuts) + shift)]
    return fn


def tube(a, pts, r0, r1, paint):
    px.tube(a, list(pts), lambda t: r0 + (r1 - r0) * t, paint)


def dot(a, x, y, col):
    px.put(a, int(x), int(y), col)


def finish(a, ground=True):
    a = px.outline(a)
    return px.ground_to(a, FOOT) if ground else a


# ---------------- E1 greed maw: a waddling, gold-decked sack that is mostly a chomping mouth ----------------
def greed_maw(frame=0):
    a = new()
    gl, gd, bn = px.ramp("glut"), px.ramp("gold"), px.ramp("bone")
    f = frame % 4
    jaw = (0.62, 0.34, 0.06, 0.34)[f]                     # half-angle of the open mouth (radians): open -> shut -> open
    bob = (0.0, -0.8, 0.0, -0.4)[f]
    step = (1.0, 0.0, -1.0, 0.0)[f]
    for lx, ph, sh in ((12.4, step, 2), (18.0, -step, 0)):   # stubby legs, far one darker
        tube(a, [(lx, 23.6 + bob), (lx + 0.6 * ph, 29.6 - max(0.0, ph) * 1.2)], 2.0, 1.6, lit(gl, shift=sh))
    px.shade_ellipse(a, 15.0, 17.6 + bob, 9.4, 8.0, 0.0, ell_lit(gl, rim=gl[0]))
    mx, my = 15.6, 18.6 + bob                              # throat; the mouth opens to the right
    tn = math.tan(jaw)
    body = a[..., 3] > 0
    for y in range(N):
        for x in range(N):
            dx, dy = x + 0.5 - mx, y + 0.5 - my
            if dx <= 0 or not body[y, x]:
                continue
            w = dx * tn + 0.6
            if abs(dy) < w:
                if dx < 4.2:
                    a[y, x] = px.C("tongue.0") if dy > w * 0.35 else K2   # dark throat, tongue below
                else:
                    a[y, x] = 0                            # the open mouth cuts the silhouette
            elif abs(dy) < w + 1.3 and dx > 2.5 and (x + (1 if dy > 0 else 0)) % 2 == 0:
                a[y, x] = bn[0]                            # saw teeth on both lips
    tube(a, [(8.6, 12.4 + bob), (12.0, 10.0 + bob), (16.0, 9.6 + bob)], 1.3, 1.1, lit(gd))   # gold crown band
    for x, y in ((9.6, 10.8), (12.6, 8.6), (15.6, 8.4)):  # crown points
        dot(a, x, y + bob, gd[0])
    for x, y in ((8.2, 22.6), (10.4, 24.0), (12.8, 24.8)):  # coin necklace on the belly
        dot(a, x, y + bob, gd[1])
        dot(a, x + 1, y + bob, gd[0])
    dot(a, 13.4, 13.4 + bob, px.C("eye.0"))               # small greedy eyes above the mouth
    dot(a, 14.4, 13.4 + bob, K)
    dot(a, 16.8, 13.0 + bob, px.C("eye.0"))
    dot(a, 17.8, 13.0 + bob, K)
    return finish(a)


# ---------------- E2 lingering hand: a hooded creeper whose long arm drags behind, reaching back ----------------
def lingering_hand(frame=0):
    a = new()
    lg, bn = px.ramp("linger"), px.ramp("bone")
    f = frame % 4
    bob = (0.0, -0.5, 0.0, -0.5)[f]
    sway = (0.0, 0.4, 0.8, 0.4)[f]                        # the hand lags further behind, then catches up
    curl = (0, 1, 2, 1)[f]

    def arm(x, y, t, nx, ny, it):
        if t < 0.30:
            return lg[min(3, px.level(it, (0.84, 0.58, 0.32)) + 1)]   # sleeve
        return bn[1] if it > 0.6 else (bn[2] if it > 0.35 else bn[3])  # thin bony forearm

    tube(a, [(16.8, 15.0 + bob), (11.8, 19.8 + bob * 0.5), (8.6 - 0.3 * sway, 22.4), (7.0 - 0.6 * sway, 25.4)], 1.8, 1.0, arm)   # bent at the elbow
    hx, hy = 6.0 - 0.6 * sway, 26.8
    for i, (fx, fy) in enumerate(((3.4, 26.4), (3.0, 28.2), (3.8, 29.6), (5.6, 29.6))):   # four spread fingers clawing the ground
        tube(a, [(hx, hy + 0.4), (fx - sway + 0.3 * ((i + curl) % 2), fy)], 0.75, 0.5, lambda x, y, t, nx, ny, it: bn[1] if t < 0.5 else bn[2])
    px.shade_ellipse(a, hx, hy, 2.3, 1.7, 0.5, lambda x, y, u, v, it: bn[0] if it > 0.7 else (bn[1] if it > 0.45 else bn[2]))   # palm

    def robe(x, y, u, v, it):
        if v > 0.55 and (x + y + f) % 2:
            return None                                    # ragged hem
        if u * u + v * v > 0.72 and (u + v) < -0.6:
            return lg[0]
        return lg[min(3, px.level(it, (0.86, 0.60, 0.34)))]

    px.shade_ellipse(a, 19.2, 21.0 + bob, 5.4, 8.6, -0.22, robe)
    tube(a, [(20.6, 28.0 + bob), (22.6, 29.6)], 1.3, 1.0, lit(lg, shift=2))   # a shuffling foot under the hem
    px.shade_ellipse(a, 21.8, 12.0 + bob, 3.8, 3.6, 0.0, ell_lit(lg, rim=lg[0]))   # hood

    def face(x, y, u, v, it):
        return lg[3] if u > -0.3 else None

    px.shade_ellipse(a, 23.4, 12.8 + bob, 2.4, 2.4, 0.0, face)   # shadowed face inside the hood
    dot(a, 23.6, 12.6 + bob, px.C("white.1"))
    dot(a, 25.0, 12.4 + bob, px.C("white.1"))
    tube(a, [(21.4, 17.2 + bob), (23.8, 19.6 + bob), (24.2, 21.6 + bob)], 1.0, 0.7, lit(lg, shift=1))   # near arm folded in
    return finish(a)


# ---------------- E3 smoke chaser: a lean, fast runner wrapped in smoke, ember-eyed ----------------
def smoke_chaser(frame=0):
    a = new()
    ash, cn = px.ramp("ash"), px.ramp("cinder")
    f = frame % 4
    ph = f * math.pi / 2
    bob = (0.0, -1.0, 0.0, -1.0)[f]

    def smoke(x, y, t, nx, ny, it):
        if t > 0.5 and (x + y + f) % 2:
            return None                                    # smoke frays into dither behind
        return ash[1] if (t < 0.4 and it > 0.5) else ash[2]

    tube(a, [(14.0, 13.6 + bob), (9.6, 12.4 + bob + 0.8 * math.sin(ph)), (5.4, 13.6 + bob - 0.8 * math.sin(ph)), (2.4, 12.6 + bob)], 2.6, 0.6, smoke)
    tube(a, [(13.0, 18.6 + bob), (8.6, 19.8 + bob - 0.7 * math.sin(ph + 1)), (4.6, 18.6 + bob + 0.7 * math.sin(ph + 1)), (2.2, 20.2 + bob)], 2.0, 0.5, smoke)

    def leg(hx, hy, phase, shift):
        sw, lift = math.sin(phase), max(0.0, math.cos(phase)) * 2.6
        knee = (hx + 2.4 + 1.6 * sw, 22.6 + bob * 0.5 - lift * 0.4)
        ankle = (hx - 0.6 + 3.2 * sw, 26.6 - lift * 0.6)
        foot = (hx + 1.0 + 3.6 * sw, 29.6 - lift)
        tube(a, [(hx, hy), knee, ankle, foot], 1.6, 0.8, lit(ash, shift=shift))

    leg(14.2, 18.4 + bob, ph + math.pi, 2)                 # far leg
    px.shade_ellipse(a, 16.4, 15.6 + bob, 5.4, 3.6, 0.42, ell_lit(ash, rim=ash[0]))   # body leaning into the run
    for x, y in ((15.4, 15.6), (16.6, 16.2), (17.6, 15.4)):   # ember cracks in the chest
        dot(a, x, y + bob, cn[1])
    dot(a, 16.6, 15.2 + bob, cn[0])
    leg(13.6, 18.8 + bob, ph, 0)                           # near leg
    tube(a, [(19.4, 16.4 + bob), (22.4, 18.6 + bob + 0.6 * math.sin(ph)), (24.6, 18.0 + bob)], 1.1, 0.7, lit(ash, shift=1))   # arm forward
    px.shade_ellipse(a, 22.8, 11.8 + bob, 3.4, 2.5, 0.22, ell_lit(ash, rim=ash[0]))   # head thrust forward
    tube(a, [(19.2, 11.0 + bob), (15.6, 9.0 + bob), (12.0, 9.4 + bob + 0.6 * math.sin(ph))], 1.4, 0.5, smoke)   # smoke hood trailing
    dot(a, 23.8, 11.4 + bob, cn[0])                        # ember eyes
    dot(a, 25.0, 11.6 + bob, cn[1])
    dot(a, 24.4, 13.0 + bob, cn[2])                        # glowing slit mouth
    dot(a, 25.4, 13.0 + bob, cn[2])
    return finish(a)


GENERATORS = {
    "enm_e1-greed-maw_move_32": greed_maw,
    "enm_e2-lingering-hand_move_32": lingering_hand,
    "enm_e3-smoke-chaser_move_32": smoke_chaser,
}
