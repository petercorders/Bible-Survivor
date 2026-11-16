"""Procedural R07 (Jacob) enemies in the art 03 style: G1 deceiving mask, G2 spotted-hide beast, G3 discord shade
(32 px move loops).
Same method as R03-R06 (copied helpers: scripts/px03.py, byte-identical with art 03): shapes authored on the cell's
pixel grid, lit from the upper left (px03.LIGHT), colours only from palette ramps (art 03 master-v0.3 plus
src/palette_r07_ext.json), 1 px outline.0 outline, ground sprites shifted so their lowest row is the foot row.
Deterministic (no randomness). Needs numpy. Each generator takes a frame index and returns a 32x32 RGBA array.
Authored by Claude (Anthropic model) in this session as a script; it is not output of the Claude Design product."""
import json, math, os
import numpy as np
import px03 as px

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.dirname(HERE)
EXT = json.load(open(os.path.join(ROOT, "src", "palette_r07_ext.json"), encoding="utf-8"))
for _ramp, _cols in EXT["actor"].items():
    if f"{_ramp}.0" in px.PAL:
        raise SystemExit(f"palette_r07_ext ramp '{_ramp}' collides with the base palette")
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


def run_leg(a, hx, hy, phase, shift, rmp, bob, r=(1.5, 0.8)):
    """Two-joint running leg (same construction as the R05 smoke chaser)."""
    sw, lift = math.sin(phase), max(0.0, math.cos(phase)) * 2.4
    knee = (hx + 2.2 + 1.4 * sw, 23.0 + bob * 0.5 - lift * 0.4)
    ankle = (hx - 0.6 + 2.8 * sw, 26.8 - lift * 0.6)
    foot = (hx + 1.0 + 3.2 * sw, 29.6 - lift)
    tube(a, [(hx, hy), knee, ankle, foot], r[0], r[1], lit(rmp, shift=shift))


# ---------------- G1 deceiving mask: a veiled scuttler whose one mask flips between a smile and a scowl ----------------
def _face(a, mx, my, mood):
    """Expression on the mask only (mood 0 smile, 1 scowl). The mask is solid and opaque in both: no fade, no ghosting."""
    for ex in (mx - 1.8, mx + 1.6):
        if mood == 0:                                      # smiling eyes: little upward arcs
            dot(a, ex - 1, my - 1.0, K)
            dot(a, ex, my - 2.0, K)
            dot(a, ex + 1, my - 1.0, K)
        else:                                              # scowl: slanted brow over a slit eye
            inner = 1 if ex < mx else -1
            dot(a, ex - inner, my - 2.6, K)
            dot(a, ex, my - 2.0, K)
            dot(a, ex - 1, my - 0.8, K)
            dot(a, ex, my - 0.8, K)
    if mood == 0:                                          # wide grin, curving up
        for dx, dy in ((-2.4, 1.4), (-1.6, 2.4), (-0.6, 2.8), (0.4, 2.8), (1.4, 2.4), (2.2, 1.4)):
            dot(a, mx + dx, my + dy, K)
        dot(a, mx - 0.1, my + 2.0, px.C("tongue.0"))
    else:                                                  # down-turned mouth
        for dx, dy in ((-2.2, 3.4), (-1.4, 2.6), (-0.4, 2.4), (0.6, 2.4), (1.6, 2.6), (2.4, 3.4)):
            dot(a, mx + dx, my + dy, K)


def deceiving_mask(frame=0):
    a = px.new(N, N)
    vl, bn, gd = px.ramp("veil"), px.ramp("bone"), px.ramp("gold")
    f = frame % 4
    bob = (0.0, -0.6, 0.0, -0.6)[f]
    step = (1.0, 0.0, -1.0, 0.0)[f]
    mood = (0, 0, 1, 1)[f]                                 # smile for half the loop, scowl for the other half
    for lx, s, sh in ((13.4, step, 1), (18.0, -step, 0)):  # small clawed feet scuttling under the hem
        tube(a, [(lx, 26.0 + bob), (lx + 1.0 + s, 29.6 - max(0.0, s) * 1.0)], 1.3, 0.9, lit(vl, shift=sh + 1))

    def veil(x, y, u, v, it):
        if v > 0.72 and (x + f) % 3 == 0:
            return None                                    # ragged hem
        if u * u + v * v > 0.74 and (u + v) < -0.6:
            return vl[0]
        lv = px.level(it, (0.86, 0.60, 0.34))
        if v > 0.2 and (x + 2 * f) % 4 == 0:
            lv = min(3, lv + 1)                            # hanging folds
        return vl[min(3, lv)]

    px.shade_ellipse(a, 15.4, 19.0 + bob, 7.0, 9.0, -0.06, veil)          # opaque veil body
    px.shade_ellipse(a, 16.0, 10.6 + bob, 5.4, 4.8, 0.0, ell_lit(vl, rim=vl[0]))   # hood
    tube(a, [(9.4, 19.0 + bob), (15.0, 21.0 + bob), (21.4, 19.2 + bob)], 0.7, 0.7, lit(gd))   # gold sash
    tube(a, [(20.4, 17.4 + bob), (23.6, 20.0 + bob), (24.4, 22.4 + bob)], 1.0, 0.7, lit(vl, shift=1))   # thin arm
    dot(a, 25.0, 23.0 + bob, bn[1])                                        # claw tip

    def mask(x, y, u, v, it):
        if u * u + v * v > 0.70 and (u + v) < -0.5:
            return bn[0]
        return bn[min(2, px.level(it, (0.80, 0.52)))]

    mx, my = 20.4, 12.0 + bob
    px.shade_ellipse(a, mx, my, 4.4, 5.0, 0.12, mask)                     # the one mask, worn forward
    _face(a, mx, my, mood)
    return finish(a)


# ---------------- G2 spotted-hide beast: a crouching predator wearing a speckled hide like a fleece (a disguise) ----------------
def spotted_hide(frame=0):
    a = px.new(N, N)
    hd, sp, bn = px.ramp("hide"), px.ramp("spot"), px.ramp("bone")
    f = frame % 4
    bob = (0.0, -0.6, 0.0, -0.6)[f]
    step = (1.0, 0.0, -1.0, 0.0)[f]

    def leg(hx, s, shift, r0):                             # short, bent, clawed predator legs (the beast's own)
        tube(a, [(hx, 21.6 + bob), (hx - 1.2 + 0.6 * s, 25.2 + bob * 0.5), (hx + 0.4 + 1.4 * s, 28.0), (hx + 1.8 + 1.4 * s, 29.6 - max(0.0, s))],
             r0, 0.9, lit(sp, shift=shift))

    tube(a, [(7.0, 19.2 + bob), (4.2, 17.6 + bob), (2.6, 19.4 + bob)], 1.0, 0.8, lit(sp, shift=1))   # tail
    leg(10.4, step, 2, 1.4)                                # far legs
    leg(19.4, -step, 2, 1.4)

    def fleece(x, y, u, v, it):
        if v < -0.78 and (x + f) % 2:
            return None                                    # tufted top edge
        if v > 0.62 and (x + f) % 3 == 0:
            return None                                    # ragged hem of the worn hide
        lv = min(2, px.level(it, (0.80, 0.50)))
        if (x * 7 + y * 13) % 19 < 3 or (x * 5 + y * 3) % 23 == 0:
            return sp[1] if lv < 2 else sp[2]              # speckles and spots
        return hd[lv]

    px.shade_ellipse(a, 14.4, 19.2 + bob, 8.6, 5.4, -0.04, fleece)       # speckled hide draped over the back
    leg(12.4, -step, 0, 1.6)                               # near legs
    leg(21.6, step, 0, 1.6)
    px.shade_ellipse(a, 23.8, 17.0 + bob, 3.4, 2.8, 0.1, ell_lit(sp, rim=sp[0]))   # low predator head (not a goat or sheep)
    tube(a, [(25.6, 16.8 + bob), (28.6, 17.6 + bob)], 1.5, 1.0, lit(sp))  # upper jaw
    tube(a, [(25.4, 19.0 + bob), (27.8, 19.8 + bob)], 0.9, 0.7, lit(sp, shift=1))   # open lower jaw
    for x in (26.4, 27.6):
        dot(a, x, 18.6 + bob, bn[0])                       # fangs in the open mouth
    dot(a, 29.0, 17.0 + bob, K2)                           # nose
    px.shade_ellipse(a, 21.6, 14.6 + bob, 3.6, 2.8, -0.2,
                     lambda x, y, u, v, it: None if (v > 0.6 and x % 2) else (hd[0] if (u + v) < -0.3 else hd[1]))   # hide pulled over the head like a hood
    tube(a, [(21.4, 12.6 + bob), (19.6, 11.2 + bob), (18.6, 12.8 + bob), (19.8, 13.8 + bob)], 0.9, 0.6, lambda x, y, t, nx, ny, it: bn[1] if t < 0.6 else bn[2])   # the fake curled horn of the hood
    dot(a, 24.6, 16.0 + bob, px.C("eye.0"))                # eye glinting under the hood
    return finish(a)


# ---------------- G3 discord shade: ONE lean shadow runner with a single horn on one side (one actor, one body) ----------------
def discord_shade(frame=0):
    a = px.new(N, N)
    sh, dc = px.ramp("shade"), px.ramp("discord")
    f = frame % 4
    ph = f * math.pi / 2
    bob = (0.0, -1.0, 0.0, -1.0)[f]

    def cloth(x, y, t, nx, ny, it):
        if t > 0.55 and (x + y + f) % 2:
            return None                                    # torn shadow cloth frays behind
        return sh[2] if (t < 0.4 and it > 0.5) else sh[3]

    tube(a, [(13.6, 12.6 + bob), (9.4, 13.4 + bob + 0.8 * math.sin(ph)), (5.4, 12.4 + bob - 0.8 * math.sin(ph)), (2.6, 13.6 + bob)], 2.4, 0.6, cloth)
    run_leg(a, 14.2, 18.4 + bob, ph + math.pi, 2, sh, bob)                # far leg
    px.shade_ellipse(a, 16.2, 15.4 + bob, 4.6, 5.8, 0.36, ell_lit(sh, rim=sh[0]))   # lean torso
    run_leg(a, 13.6, 18.8 + bob, ph, 0, sh, bob)                           # near leg
    tube(a, [(19.0, 13.4 + bob), (23.0, 15.6 + bob + 0.6 * math.sin(ph)), (26.2, 14.6 + bob)], 1.1, 0.7, lit(sh, shift=1))   # reaching arm
    for fy in (13.6, 15.6):                                                # two claws
        tube(a, [(26.0, 14.6 + bob), (27.8, fy + bob)], 0.6, 0.5, lambda x, y, t, nx, ny, it: sh[0])
    px.shade_ellipse(a, 21.0, 9.6 + bob, 3.2, 3.0, 0.0, ell_lit(sh, rim=sh[0]))   # head
    tube(a, [(19.8, 7.6 + bob), (17.6, 5.8 + bob), (14.4, 5.4 + bob), (12.0, 6.6 + bob)], 1.2, 0.4, lambda x, y, t, nx, ny, it: dc[1] if t > 0.85 else (sh[0] if ny < 0.2 else sh[1]))   # the single swept-back horn
    dot(a, 22.6, 9.4 + bob, dc[0])                                         # one red eye
    dot(a, 23.6, 9.6 + bob, dc[1])
    dot(a, 15.4, 15.0 + bob, dc[1])                                        # ember of discord in the chest
    dot(a, 16.4, 15.6 + bob, dc[0])
    return finish(a)


GENERATORS = {
    "enm_g1-deceiving-mask_move_32": deceiving_mask,
    "enm_g2-spotted-hide_move_32": spotted_hide,
    "enm_g3-discord-shade_move_32": discord_shade,
}
