"""Procedural R08 (Joseph) enemies in the art 03 style: H1 envy shadow, H2 prison chain wraith, H3 famine beast
(32 px move loops).
Same method as R03-R07 (copied helpers: scripts/px03.py, byte-identical with art 03): shapes authored on the cell's
pixel grid, lit from the upper left (px03.LIGHT), colours only from palette ramps (art 03 master-v0.3 plus
src/palette_r08_ext.json), 1 px outline.0 outline, ground sprites shifted so their lowest row is the foot row.
Deterministic (no randomness). Needs numpy. Each generator takes a frame index and returns a 32x32 RGBA array.
Authored by Claude (Anthropic model) in this session as a script; it is not output of the Claude Design product."""
import json, math, os
import numpy as np
import px03 as px

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.dirname(HERE)
EXT = json.load(open(os.path.join(ROOT, "src", "palette_r08_ext.json"), encoding="utf-8"))
for _ramp, _cols in EXT["actor"].items():
    if f"{_ramp}.0" in px.PAL:
        raise SystemExit(f"palette_r08_ext ramp '{_ramp}' collides with the base palette")
    for _i, _c in enumerate(_cols):
        _v = px.hex_rgba(_c)
        px.PAL[f"{_ramp}.{_i}"] = _v
        px.NAME_OF.setdefault(_v, f"{_ramp}.{_i}")
        px.RAMP_OF.setdefault(_v, _ramp)

N = 32
FOOT = N - 2                 # foot row for pivot (16, 1)
K = px.C("outline.0")
K2 = px.C("outline.1")


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
    px.put(a, int(round(x)), int(round(y)), col)


def finish(a):
    return px.ground_to(px.outline(a), FOOT)


# ---------------- H1 envy shadow: a crouching shadow imp staring with huge green eyes, claws clutched to its chest ----------------
def envy_shadow(frame=0):
    a = px.new(N, N)
    gl, ev = px.ramp("gloom"), px.ramp("envy")
    f = frame % 4
    ph = f * math.pi / 2
    bob = (0.0, -1.0, 0.0, -1.0)[f]

    def wisp(x, y, t, nx, ny, it):
        if t > 0.5 and (x + y + f) % 2:
            return None                                    # the shadow frays behind it
        return gl[2] if t < 0.45 else gl[3]

    tube(a, [(11.0, 18.0 + bob), (7.0, 16.6 + bob + 0.8 * math.sin(ph)), (3.6, 18.4 + bob - 0.6 * math.sin(ph))], 2.6, 0.7, wisp)
    for hx, p, sh in ((13.0, ph + math.pi, 2), (17.0, ph, 0)):   # short scuttling legs
        sw, lift = math.sin(p), max(0.0, math.cos(p)) * 1.8
        tube(a, [(hx, 21.6 + bob), (hx + 1.6 + 1.2 * sw, 25.4 - lift * 0.4), (hx + 0.4 + 2.4 * sw, 29.6 - lift)], 1.6, 1.0, lit(gl, shift=sh))
    px.shade_ellipse(a, 15.0, 18.0 + bob, 6.4, 5.2, 0.2, ell_lit(gl, rim=gl[0]))   # hunched body
    px.shade_ellipse(a, 19.6, 11.2 + bob, 5.6, 4.6, 0.0, ell_lit(gl, rim=gl[0]))   # wide head thrust forward
    for ex, sz in ((18.0, 1), (22.0, 1)):                  # two huge staring green eyes
        for dx in range(-1, 2):
            for dy in range(-1, 1):
                dot(a, ex + dx, 10.6 + bob + dy, ev[1])
        dot(a, ex, 10.0 + bob, ev[0])
        dot(a, ex + 1, 10.6 + bob, K)                      # pupil looking ahead
    for i, x in enumerate(range(18, 24)):                  # thin jagged grin, glowing like the eyes
        dot(a, x, 13.6 + bob + (i % 2), ev[1])
    for i, (bx, tx, ty) in enumerate(((16.2, 13.6, 6.2), (18.8, 17.8, 4.8), (21.4, 22.2, 5.6))):   # jagged shadow spikes, not ears
        tube(a, [(bx, 8.0 + bob), (tx, ty + bob)], 1.1, 0.3, lit(gl, shift=1 if i == 0 else 0))
    tube(a, [(17.8, 16.6 + bob), (21.2, 18.4 + bob), (19.6, 20.2 + bob)], 1.1, 0.8, lit(gl, shift=1))   # clawed hands clutched in
    for dx in (0.0, 1.2):
        dot(a, 20.0 + dx, 21.0 + bob, ev[1])               # a green glint between the claws (what it covets)
    return finish(a)


# ---------------- H2 prison chain wraith: a hooded spirit whose body is a small iron cage, swinging one chain around itself ----------------
def chain_wraith(frame=0):
    """The chain swings round the wraith in a flat circle at chest height; it stays inside the 32 px clip with a
    margin and never reaches out, hooks or binds (no restraint)."""
    a = px.new(N, N)
    gw, ir = px.ramp("gaol"), px.ramp("iron")
    f = frame % 4
    bob = (0.0, -0.5, 0.0, -0.5)[f]
    ang = f * math.pi / 2                                  # 90 degrees per frame: right, front, left, back
    hand = (19.6, 13.4 + bob)
    cx, cy, rx, ry = 16.0, 15.4 + bob, 10.6, 5.0             # tilted circle: the front pass shows on the dark robe
    end = (cx + rx * math.cos(ang), cy + ry * math.sin(ang))
    behind = math.sin(ang) < -0.5                          # the back quarter passes behind the body

    def draw_chain():
        n = 7
        for i in range(1, n + 1):
            t = i / n
            mx = hand[0] + (end[0] - hand[0]) * t
            my = hand[1] + (end[1] - hand[1]) * t + math.sin(t * math.pi) * 1.2
            dot(a, mx, my, ir[0] if i % 2 else ir[2])
        px.shade_ellipse(a, end[0], end[1], 1.9, 1.7, 0.0, ell_lit(ir, cuts=(0.72, 0.42)))   # iron weight on the end

    if behind:
        draw_chain()

    def robe(x, y, u, v, it):
        if v > 0.70 and (x + f) % 3 == 0:
            return None                                    # tattered hem
        lv = px.level(it, (0.86, 0.60, 0.34))
        if v > 0.2 and (x + f // 2) % 4 == 0:
            lv = min(3, lv + 1)
        return gw[min(3, lv)]

    px.shade_ellipse(a, 15.6, 23.4 + bob, 5.8, 6.4, 0.0, robe)                      # long robe, hem on the ground
    tube(a, [(15.4, 12.0 + bob), (15.6, 19.0 + bob)], 4.6, 5.4, lit(gw, shift=1))   # robe shoulders tapering into the skirt
    for y in range(int(12.0 + bob), int(18.6 + bob) + 1):
        for x in range(12, 19):
            px.put(a, x, y, K2)                                                     # dark inside of the cage
    for bx in (12.6, 15.0, 17.4):                                                   # the iron cage that is its chest
        tube(a, [(bx, 11.6 + bob), (bx + 0.2, 18.4 + bob)], 0.7, 0.7, lit(ir))
    tube(a, [(11.6, 11.4 + bob), (18.6, 11.4 + bob)], 0.8, 0.8, lit(ir))
    tube(a, [(11.6, 18.6 + bob), (18.6, 18.6 + bob)], 0.8, 0.8, lit(ir))
    for x, y in ((13.8, 14.6), (16.2, 15.4)):
        dot(a, x, y + bob, px.C("eye.0"))                  # dim light caught inside the cage
    px.shade_ellipse(a, 15.6, 7.6 + bob, 4.2, 4.0, 0.0, ell_lit(gw, rim=gw[0]))   # hood
    px.shade_ellipse(a, 16.8, 8.4 + bob, 2.6, 2.4, 0.0, lambda x, y, u, v, it: K2 if u > -0.4 else None)   # empty hood
    dot(a, 16.6, 8.2 + bob, px.C("white.1"))
    dot(a, 18.2, 8.2 + bob, px.C("white.1"))
    tube(a, [(18.4, 11.8 + bob), (19.8, 12.6 + bob), hand], 1.1, 0.9, lit(gw, shift=1))   # arm to the chain
    if not behind:
        draw_chain()
    return finish(a)


# ---------------- H3 famine beast: a gaunt, rib-showing beast chewing a sheaf of grain (it stays thin) ----------------
def famine_beast(frame=0):
    a = px.new(N, N)
    gt, gd, lf = px.ramp("gaunt"), px.ramp("gold"), px.ramp("leaf")
    f = frame % 4
    bob = (0.0, -0.6, 0.0, -0.6)[f]
    step = (1.0, 0.0, -1.0, 0.0)[f]
    jaw = (0.0, 1.4, 0.4, 1.4)[f]                          # chewing: shut, open, half, open

    def leg(hx, hy, s, shift):                             # long bony legs with knobby joints
        knee = (hx + 0.6 * s, 24.6 + bob * 0.5)
        foot = (hx + 1.6 * s, 29.6 - max(0.0, s) * 1.2)
        tube(a, [(hx, hy), knee, foot], 1.2, 0.8, lit(gt, shift=shift))
        dot(a, knee[0], knee[1], gt[0] if shift == 0 else gt[2])

    leg(9.6, 19.6 + bob, -step, 2)                         # far legs
    leg(19.8, 19.4 + bob, step, 2)
    tube(a, [(7.4, 16.6 + bob), (4.6, 18.2 + bob), (3.4, 21.0 + bob)], 0.8, 0.5, lit(gt, shift=1))   # thin tail

    def ribs(x, y, u, v, it):
        lv = px.level(it, (0.84, 0.58, 0.34))
        if -0.1 < u < 0.8 and v > -0.6 and (x % 2 == 0):
            lv = min(3, lv + 2)                            # ribs showing through
        if v > 0.45:
            lv = min(3, lv + 1)                            # sunken belly
        return gt[min(3, lv)]

    px.shade_ellipse(a, 14.4, 17.4 + bob, 7.6, 3.8, -0.08, ribs)                   # narrow ribcage
    for x in (9.6, 12.0, 14.4, 16.8):
        dot(a, x, 13.8 + bob, gt[0])                       # spine knobs
    leg(12.0, 19.8 + bob, step, 0)                         # near legs
    leg(21.8, 19.6 + bob, -step, 0)
    tube(a, [(19.6, 15.4 + bob), (22.0, 12.6 + bob), (23.6, 11.6 + bob)], 1.6, 1.2, lit(gt))   # long bony neck
    px.shade_ellipse(a, 25.0, 11.4 + bob, 3.0, 2.4, 0.15, ell_lit(gt, rim=gt[0]))  # long skull
    tube(a, [(26.0, 11.0 + bob), (29.0, 12.0 + bob)], 1.3, 0.9, lit(gt))           # upper jaw
    tube(a, [(25.6, 13.0 + bob + jaw * 0.4), (28.4, 13.6 + bob + jaw)], 0.8, 0.6, lit(gt, shift=1))   # chewing lower jaw
    dot(a, 24.6, 10.6 + bob, px.C("tongue.0"))             # hollow red eye
    for i, (sx, sy) in enumerate(((27.0, 12.6), (27.6, 14.2), (26.6, 15.4))):   # a sheaf of grain held in the jaws
        tube(a, [(sx + 0.4, sy + bob + jaw * 0.3), (sx - 2.0 - i * 0.6, sy + 4.0 + bob)], 0.5, 0.5, lambda x, y, t, nx, ny, it: lf[1] if t > 0.5 else gd[1])
        dot(a, sx + 0.8, sy - 0.2 + bob + jaw * 0.3, gd[0])  # grain heads
    return finish(a)


GENERATORS = {
    "enm_h1-envy-shadow_move_32": envy_shadow,
    "enm_h2-chain-wraith_move_32": chain_wraith,
    "enm_h3-famine-beast_move_32": famine_beast,
}
