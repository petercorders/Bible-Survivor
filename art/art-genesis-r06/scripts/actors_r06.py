"""Procedural R06 (Isaac) actors in the art 03 style: F1 water raider, F2 well filler, F3 strife armour (32 px move
loops) and the Sitnah well-keeper boss (64 px move / windup / impact / death).
Same method as R03-R05 (copied helpers: scripts/px03.py, byte-identical with art 03): shapes authored on the cell's
pixel grid, lit from the upper left (px03.LIGHT), colours only from palette ramps (art 03 master-v0.3 plus
src/palette_r06_ext.json), 1 px outline.0 outline, ground sprites shifted so their lowest row is the foot row.
Deterministic (no randomness). Needs numpy. Each generator takes a frame index and returns an RGBA array.
Authored by Claude (Anthropic model) in this session as a script; it is not output of the Claude Design product."""
import json, math, os
import numpy as np
import px03 as px

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.dirname(HERE)
EXT = json.load(open(os.path.join(ROOT, "src", "palette_r06_ext.json"), encoding="utf-8"))
for _ramp, _cols in EXT["actor"].items():
    if f"{_ramp}.0" in px.PAL:
        raise SystemExit(f"palette_r06_ext ramp '{_ramp}' collides with the base palette")
    for _i, _c in enumerate(_cols):
        _v = px.hex_rgba(_c)
        px.PAL[f"{_ramp}.{_i}"] = _v
        px.NAME_OF.setdefault(_v, f"{_ramp}.{_i}")
        px.RAMP_OF.setdefault(_v, _ramp)

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


def ell(a, cx, cy, rx, ry, rot, fn):
    px.shade_ellipse(a, cx, cy, rx, ry, rot, fn)


def dot(a, x, y, col):
    px.put(a, int(x), int(y), col)


def finish(a, foot):
    return px.ground_to(px.outline(a), foot)


def run_leg(a, hx, hy, phase, shift, rmp, bob, r=(1.6, 0.8), reach=1.0):
    """Two-joint running leg (same construction as the R05 smoke chaser)."""
    sw, lift = math.sin(phase), max(0.0, math.cos(phase)) * 2.4
    knee = (hx + 2.2 * reach + 1.4 * sw, 23.0 + bob * 0.5 - lift * 0.4)
    ankle = (hx - 0.6 + 2.8 * sw * reach, 26.8 - lift * 0.6)
    foot = (hx + 1.0 + 3.2 * sw * reach, 29.6 - lift)
    tube(a, [(hx, hy), knee, ankle, foot], r[0], r[1], lit(rmp, shift=shift))


# ---------------- F1 water raider: a lean jackal-imp sprinting with a stolen, sloshing water skin ----------------
def water_raider(frame=0):
    a = px.new(32, 32)
    rd, wt, lt = px.ramp("raid"), px.ramp("water"), px.ramp("leather")
    f = frame % 4
    ph = f * math.pi / 2
    bob = (0.0, -1.0, 0.0, -1.0)[f]
    slosh = (0.0, 0.6, 0.0, -0.6)[f]
    tube(a, [(11.6, 19.0 + bob), (7.4, 17.4 + bob - 0.8 * math.sin(ph)), (3.6, 18.8 + bob + 0.6 * math.sin(ph))], 1.2, 0.5, lit(rd, shift=1))   # tail
    run_leg(a, 14.0, 19.2 + bob, ph + math.pi, 2, rd, bob)                     # far leg
    ell(a, 15.6, 17.0 + bob, 5.2, 3.4, 0.38, ell_lit(rd, rim=rd[0]))           # body leaning into the run
    run_leg(a, 13.4, 19.6 + bob, ph, 0, rd, bob)                               # near leg
    tube(a, [(14.0, 12.4 + bob), (17.8, 16.6 + bob), (19.6, 19.2 + bob)], 0.7, 0.7, lit(lt, shift=1))   # strap across the chest

    def skin(x, y, u, v, it):
        if u * u + v * v > 0.70 and (u + v) < -0.5:
            return wt[0]
        if 0.10 < v < 0.34 and u > -0.6:
            return wt[3]                                   # water line sloshing inside
        return wt[min(3, px.level(it, (0.80, 0.54, 0.30)))]

    ell(a, 11.6, 13.8 + bob + slosh * 0.5, 4.4, 3.4, -0.2 + slosh * 0.15, skin)   # stolen water skin on the back
    tube(a, [(13.6, 11.2 + bob), (15.0, 10.4 + bob)], 1.0, 0.8, lit(lt))      # tied neck of the skin
    tube(a, [(19.6, 16.0 + bob), (22.6, 18.4 + bob + 0.6 * math.sin(ph)), (24.6, 17.6 + bob)], 1.0, 0.7, lit(rd, shift=1))   # grasping arm
    ell(a, 21.8, 12.2 + bob, 3.2, 2.6, 0.18, ell_lit(rd, rim=rd[0]))           # head thrust forward
    tube(a, [(23.4, 12.4 + bob), (27.2, 13.8 + bob)], 1.6, 0.9, lit(rd))       # snout
    tube(a, [(20.6, 10.6 + bob), (19.6, 7.4 + bob)], 1.1, 0.5, lit(rd, shift=1))   # pointed ear
    dot(a, 23.0, 11.6 + bob, px.C("eye.0"))
    dot(a, 27.6, 13.4 + bob, K2)                                               # nose
    return finish(a, 30)


# ---------------- F2 well filler: a squat burrowing beast hauling a heaped basket of dirt ----------------
def well_filler(frame=0):
    a = px.new(32, 32)
    ml, dt, bk, bn = px.ramp("mole"), px.ramp("dirt"), px.ramp("bark"), px.ramp("bone")
    f = frame % 4
    bob = (0.0, -0.6, 0.0, -0.6)[f]
    step = (1.0, 0.0, -1.0, 0.0)[f]
    for lx, s, sh in ((10.4, -step, 2), (19.4, step, 2)):                     # far legs
        tube(a, [(lx, 23.0 + bob), (lx + 0.8 * s, 29.4 - max(0.0, s) * 1.0)], 1.8, 1.5, lit(ml, shift=sh))
    ell(a, 15.4, 21.0 + bob, 9.0, 5.6, 0.0, ell_lit(ml, rim=ml[0]))           # heavy body

    def basket(x, y, u, v, it):
        if v < -0.30:
            return None                                    # open top: the dirt sits in it
        lv = px.level(it, (0.70, 0.40))
        if (x + (y // 2)) % 3 == 0:
            lv = min(3, lv + 1)                            # woven slats
        return bk[min(3, lv + 1)]

    ell(a, 13.6, 13.8 + bob, 7.4, 4.0, 0.0, basket)                            # basket strapped on the back

    def heap(x, y, u, v, it):
        if v > 0.35:
            return None
        lv = px.level(it, (0.82, 0.58, 0.32))
        if (x * 3 + y * 5 + f // 2) % 11 == 0:
            lv = min(3, lv + 2)                            # clods
        return dt[lv]

    ell(a, 13.4, 11.4 + bob, 6.4, 4.6, 0.0, heap)                              # heaped dirt
    for x, y in ((9.0, 8.6), (17.4, 9.0)):
        dot(a, x, y + bob, dt[2])
    tube(a, [(7.0, 15.6 + bob), (14.0, 17.8 + bob), (20.4, 15.4 + bob)], 0.7, 0.7, lit(px.ramp("leather"), shift=1))   # basket strap
    ell(a, 24.2, 21.4 + bob, 3.8, 3.2, 0.0, ell_lit(ml, rim=ml[0]))           # low head
    tube(a, [(26.0, 22.0 + bob), (28.6, 23.2 + bob)], 1.4, 0.8, lit(ml, shift=1))   # long snout
    dot(a, 25.0, 20.2 + bob, px.C("eye.0"))
    for lx, s in ((13.2, step), (22.6, -step)):                                # near legs
        tube(a, [(lx, 24.0 + bob), (lx + 0.8 * s, 29.4 - max(0.0, s) * 1.0)], 2.0, 1.6, lit(ml))
    for i in range(3):                                                         # shovel claws on the near fore foot
        tube(a, [(23.4 - step * 0.8 + i * 0.9, 28.8 - max(0.0, -step)), (24.8 - step * 0.8 + i * 0.9, 29.6 - max(0.0, -step))], 0.6, 0.5,
             lambda x, y, t, nx, ny, it: bn[1])
    return finish(a, 30)


# ---------------- F3 strife armour: a hollow, horned suit of plates that stomps forward (no shield, open front) ----------------
def strife_armour(frame=0):
    a = px.new(32, 32)
    pl, sf, ln = px.ramp("plate"), px.ramp("strife"), px.ramp("lining")
    f = frame % 4
    bob = (0.0, -0.6, 0.0, -0.6)[f]
    step = (1.0, 0.0, -1.0, 0.0)[f]
    swing = (-1.0, 0.0, 1.0, 0.0)[f]
    tube(a, [(12.6, 21.4 + bob), (12.0 - step * 1.2, 25.4 + bob * 0.5), (12.4 - step * 1.6, 29.0 - max(0.0, -step))], 2.2, 1.9, lit(pl, shift=2))   # far leg
    tube(a, [(10.6, 13.0 + bob), (8.6 - swing, 17.0 + bob), (9.0 - swing, 20.4 + bob)], 1.4, 1.2, lit(pl, shift=2))   # far arm
    ell(a, 9.2 - swing, 21.0 + bob, 1.9, 1.7, 0.0, ell_lit(pl, shift=1))

    def cuirass(x, y, u, v, it):
        if u * u + v * v > 0.74 and (u + v) < -0.6:
            return pl[0]
        lv = px.level(it, (0.84, 0.60, 0.36))
        if 0.18 < u < 0.34 and v < 0.4:
            lv = min(3, lv + 2)                            # dented ridge down the breastplate
        if v > 0.42 and (y % 2 == 0):
            lv = min(3, lv + 1)                            # overlapping lames over the belly
        return pl[min(3, lv)]

    for i, tx in enumerate((12.8, 15.4, 18.0)):                                # ragged red tassets
        tube(a, [(tx, 21.6 + bob), (tx + 0.4, 24.4 + bob + (i + f) % 2 * 0.6)], 1.2, 0.8, lit(ln, cuts=(0.6,)))
    ell(a, 15.4, 16.8 + bob, 6.4, 6.6, 0.0, cuirass)                           # barrel cuirass, hollow inside
    tube(a, [(17.8, 21.6 + bob), (18.6 + step * 1.2, 25.4 + bob * 0.5), (18.6 + step * 1.6, 29.0 - max(0.0, step))], 2.4, 2.0, lit(pl))   # near leg
    ell(a, 10.8, 11.6 + bob, 3.4, 2.6, -0.2, ell_lit(pl, shift=1))             # far pauldron
    ell(a, 20.2, 12.0 + bob, 3.6, 2.8, 0.2, ell_lit(pl, rim=pl[0]))            # near pauldron
    tube(a, [(20.4, 9.6 + bob), (21.6, 7.6 + bob)], 0.9, 0.4, lambda x, y, t, nx, ny, it: pl[1])   # pauldron spike

    def helm(x, y, u, v, it):
        if u * u + v * v > 0.70 and (u + v) < -0.5:
            return pl[0]
        if -0.05 < v < 0.30 and u > -0.1:
            return K2                                      # dark visor band
        return pl[min(3, px.level(it, (0.84, 0.60, 0.36)))]

    ell(a, 16.8, 8.8 + bob, 3.8, 3.6, 0.0, helm)
    tube(a, [(14.4, 6.4 + bob), (12.8, 5.0 + bob), (13.4, 3.8 + bob)], 1.0, 0.5, lambda x, y, t, nx, ny, it: px.C("bone.1"))   # back horn
    tube(a, [(19.2, 6.6 + bob), (21.2, 5.2 + bob), (20.6, 4.0 + bob)], 1.0, 0.5, lambda x, y, t, nx, ny, it: px.C("bone.0"))   # front horn, both curling in to clash
    for x in (17.6, 18.8, 20.0):
        dot(a, x, 9.2 + bob, sf[0] if x < 19 else sf[1])                       # angry glow in the empty helm
    tube(a, [(20.8, 14.0 + bob), (22.6 + swing, 17.6 + bob), (22.8 + swing, 20.6 + bob)], 1.5, 1.3, lit(pl, shift=1))   # near arm
    ell(a, 23.0 + swing, 21.2 + bob, 2.0, 1.8, 0.0, ell_lit(pl, rim=pl[0]))   # gauntlet fist
    return finish(a, 30)


# ---------------- Boss: the Sitnah well-keeper, a created monster of well stones, choking dirt and a bucket chain (64 px) ----------------
WS, DT, CH = "wellstone", "dirt", "stone"


def _well(a, cx, top, bot, rx, ryc, eye=0, mouth="dirt", crack=False):
    """Stone well shaft seen from the front and a little above: curved courses of stone, a rim and its mouth."""
    ws, dt, wt = px.ramp(WS), px.ramp(DT), px.ramp("water")
    h, w = a.shape[:2]
    for y in range(h):
        for x in range(w):
            u = (x + 0.5 - cx) / rx
            if abs(u) > 1.0:
                continue
            arc = ryc * math.sqrt(max(0.0, 1 - u * u))
            if not (top - arc <= y + 0.5 <= bot + arc):
                continue
            n = np.array([u, -0.15, math.sqrt(max(0.0, 1 - u * u))]); n /= np.linalg.norm(n)
            it = float(n @ px.LIGHT) * 0.5 + 0.5
            lv = px.level(it, (0.80, 0.60, 0.40))
            yy = y + 0.5 - top - arc
            band = int(yy // 5)
            sarc = math.asin(max(-1.0, min(1.0, u))) * rx
            if yy % 5 < 1 or int(sarc + band * 3.5 + 50) % 7 == 0:
                lv = min(3, lv + 2)                        # mortar joints between the courses
            if crack and 0.10 < u < 0.42 and abs(yy - 9 - 6 * u) < 0.8:
                a[y, x] = wt[1]                            # water seeping through a crack
                continue
            a[y, x] = ws[min(3, lv)]
    rim = lambda x, y, u, v, it: ws[0] if (u + v) < -0.2 else ws[1]
    ell(a, cx, top, rx + 0.6, ryc + 1.2, 0.0, rim)                             # rim stones
    inner = {"dirt": lambda x, y, u, v, it: dt[1] if v < -0.2 else dt[2],
             "dark": lambda x, y, u, v, it: K2,
             "water": lambda x, y, u, v, it: wt[0] if (u < -0.3 and v < -0.2) else (wt[1] if v < 0.3 else wt[2])}[mouth]
    ell(a, cx, top, rx - 2.2, ryc - 0.8, 0.0, inner)
    for ex in (cx + 4.0, cx + 9.0) if eye < 3 else ():                         # eyes glowing between the stones (none once freed)
        for dx in (-1, 0, 1):
            dot(a, ex + dx, top + 9.0, K2)
            dot(a, ex + dx, top + 10.0, K2)
        if eye < 2:
            dot(a, ex, top + 9.0, px.C("eye.0") if eye == 0 else wt[0])
            dot(a, ex + 1, top + 9.0, px.C("eye.0") if eye == 0 else wt[0])
    for i, x in enumerate(range(int(cx + 2), int(cx + 12)) if eye < 3 else ()):  # jagged mouth crack
        dot(a, x, top + 15.0 + (i % 3 == 1), K2)


def _rock(a, x, y, r, shift=0):
    ws = px.ramp(WS)
    ell(a, x, y, r, r * 0.86, 0.0, lambda X, Y, u, v, it: ws[min(3, px.level(it, (0.82, 0.56, 0.32)) + shift)])


def _arm(a, pts, shift=0, fist=2.8):
    tube(a, pts, 3.2, 2.6, lit(px.ramp(WS), shift=shift))
    _rock(a, pts[-1][0], pts[-1][1], fist, shift)


def _legs(a, near, far):
    for (hx, hy, fx, fy), sh in ((far, 1), (near, 0)):
        tube(a, [(hx, hy), (fx, fy)], 4.2, 3.6, lit(px.ramp(WS), shift=sh))
        ell(a, fx + 1.2, fy + 1.8, 4.8, 2.2, 0.0, lambda X, Y, u, v, it, s=sh: px.ramp(WS)[min(3, px.level(it, (0.80, 0.55)) + 1 + s)])


def _bucket(a, top_x, top_y, end_x, end_y, tilt=0.0):
    """Chain from the rim to a wooden bucket (the bucket and chain are the boss's own parts, never a weapon or projectile)."""
    ch, bk = px.ramp(CH), px.ramp("bark")
    n = max(2, int(math.hypot(end_x - top_x, end_y - top_y)))
    for i in range(n + 1):
        t = i / n
        dot(a, top_x + (end_x - top_x) * t, top_y + (end_y - top_y) * t, ch[1] if i % 2 else ch[3])
    for y in range(6):
        half = 2.4 + y * 0.25
        for x in range(-4, 5):
            if abs(x - tilt * y) <= half:
                X, Y = int(end_x + x), int(end_y + 1 + y)
                col = bk[1] if x - tilt * y < -0.5 else bk[2]
                if y in (1, 4):
                    col = ch[2]                            # iron hoops
                px.put(a, X, Y, col)


def _heap(a, cx, cy, rx, ry, f=0):
    dt = px.ramp(DT)

    def fn(x, y, u, v, it):
        if v > 0.55:
            return None
        lv = px.level(it, (0.82, 0.58, 0.32))
        if (x * 3 + y * 5 + f) % 13 == 0:
            lv = min(3, lv + 2)                            # clods in the heap
        return dt[lv]

    ell(a, cx, cy, rx, ry, 0.0, fn)


def keeper_move(frame=0):
    a = px.new(64, 64)
    f = frame % 4
    bob = (0.0, -1.0, 0.0, -1.0)[f]
    step = (1.0, 0.0, -1.0, 0.0)[f]
    top = 22.0 + bob
    _arm(a, [(20.0, top + 6.0), (14.0, top + 13.0), (14.0 - step * 2.0, top + 19.0)], shift=1)
    _legs(a, (37.0, 46.0 + bob, 38.0 + step * 3.0, 58.0 - max(0.0, step) * 2.0),
             (25.0, 46.0 + bob, 25.0 - step * 3.0, 58.0 - max(0.0, -step) * 2.0))
    _well(a, 31.0, top, 45.0 + bob, 15.0, 4.6)
    _heap(a, 31.0, top - 1.0, 9.0, 3.6, f)                                     # the dirt choking the well mouth
    _arm(a, [(42.0, top + 6.0), (47.0, top + 12.0), (45.0 + step * 2.0, top + 19.0)])
    _bucket(a, 44.0, top + 2.0, 53.0 + step * 1.5, top + 12.0, tilt=step * 0.2)   # in front of the arm so it reads
    return finish(a, 62)


def _stand(a, top, bob=0.0):
    _legs(a, (37.0, 46.0 + bob, 38.0, 58.0), (25.0, 46.0 + bob, 24.0, 58.0))


def keeper_windup(frame=0):
    """The 1.2 s throw warning, driven by the Core telegraph clock: frame k from elapsed k*0.4 s, frame 2 held."""
    a = px.new(64, 64)
    k = frame % 3
    if k == 0:      # crouch, both hands dig into the choked mouth; the dirt bulges up
        top = 24.0
        _arm(a, [(20.0, top + 6.0), (17.0, top + 2.0), (22.0, top - 1.0)], shift=1)
        _stand(a, top, 2.0)
        _well(a, 31.0, top, 47.0, 15.0, 4.6)
        _heap(a, 31.0, top - 2.0, 10.0, 5.2, 1)
        _bucket(a, 44.0, top + 2.0, 50.0, top + 13.0)
        _arm(a, [(42.0, top + 6.0), (46.0, top + 2.0), (40.0, top - 1.0)])
    elif k == 1:    # the heap is torn out and lifted to the chest; the mouth shows dark
        top = 22.0
        _arm(a, [(20.0, top + 6.0), (16.0, top - 2.0), (21.0, top - 9.0)], shift=1)
        _stand(a, top)
        _well(a, 31.0, top, 45.0, 15.0, 4.6, mouth="dark")
        _heap(a, 31.0, top - 9.0, 11.0, 6.0, 2)
        _bucket(a, 44.0, top + 2.0, 49.0, top + 13.0)
        _arm(a, [(42.0, top + 6.0), (46.0, top - 2.0), (41.0, top - 9.0)])
    else:           # held: the heap high overhead and pulled back, arms at full stretch, eyes bright
        top = 22.0
        _arm(a, [(20.0, top + 6.0), (15.0, top - 3.0), (18.0, top - 11.0)], shift=1)
        _stand(a, top)
        _well(a, 31.0, top, 45.0, 15.0, 4.6, mouth="dark")
        _heap(a, 28.0, top - 12.5, 12.5, 5.6, 3)
        _bucket(a, 44.0, top + 2.0, 48.0, top + 13.0, tilt=-0.2)
        _arm(a, [(42.0, top + 6.0), (44.0, top - 3.0), (38.0, top - 11.0)])
    return finish(a, 62)


def keeper_impact(frame=0):
    """Starts on the Core impact tick: the heap leaves the hands toward the facing side. The flight and the fallen
    dirt are not in this clip (Unity visual only, decoration, never terrain)."""
    a = px.new(64, 64)
    k = frame % 2
    dt = px.ramp(DT)
    top = 22.0
    if k == 0:      # release: both arms flung forward and up, clods leaving the hands
        _arm(a, [(20.0, top + 6.0), (28.0, top - 2.0), (38.0, top - 7.0)], shift=1)
        _stand(a, top)
        _well(a, 31.0, top, 45.0, 15.0, 4.6, mouth="dark")
        _bucket(a, 44.0, top + 2.0, 52.0, top + 11.0, tilt=0.4)
        _arm(a, [(42.0, top + 6.0), (48.0, top - 1.0), (54.0, top - 6.0)])
        for x, y, r in ((56.0, 9.0, 2.0), (51.0, 7.0, 1.5), (58.0, 14.5, 1.3), (46.0, 10.0, 1.3)):
            _heap(a, x, y, r, r * 0.9, int(x))
    else:           # follow-through: arms low and forward, a last spray of crumbs
        _arm(a, [(20.0, top + 6.0), (30.0, top + 4.0), (40.0, top + 6.0)], shift=1)
        _stand(a, top)
        _well(a, 31.0, top, 45.0, 15.0, 4.6, mouth="dark")
        _bucket(a, 44.0, top + 2.0, 51.0, top + 12.0, tilt=0.2)
        _arm(a, [(42.0, top + 6.0), (50.0, top + 6.0), (56.0, top + 9.0)])
        for x, y in ((56.0, 6.0), (58.0, 9.0), (53.0, 4.0), (59.0, 12.0)):
            dot(a, x, y, dt[1])
    return finish(a, 62)


def _spring(a, cx, base, height, f):
    """Water rising out of the freed well (death only; not a hazard and not left on the ground)."""
    wt = px.ramp("water")
    tube(a, [(cx, base), (cx - 0.5, base - height * 0.5), (cx + 0.5, base - height)], 2.6, 1.2,
         lambda x, y, t, nx, ny, it: wt[0] if nx < -0.2 else (wt[1] if t < 0.7 else wt[0]))
    for i, (dx, dy) in enumerate(((-5.0, 2.0), (5.0, 1.0), (-3.0, -3.0), (4.0, -4.0), (-7.0, 5.0), (7.0, 4.0))):
        if (i + f) % 3:
            dot(a, cx + dx, base - height + 6.0 + dy, wt[1] if i % 2 else wt[0])


def keeper_death(frame=0):
    """Plays once on the boss kill: the well-keeper breaks and the filled well flows again."""
    a = px.new(64, 64)
    k = frame % 4
    ws = px.ramp(WS)
    if k == 0:      # stagger back, eyes fading, water seeping through a crack
        top = 23.0
        _arm(a, [(20.0, top + 6.0), (14.0, top + 14.0), (12.0, top + 21.0)], shift=1)
        _stand(a, top, 1.0)
        _well(a, 30.0, top, 46.0, 15.0, 4.6, eye=1, mouth="dark", crack=True)
        _bucket(a, 43.0, top + 2.0, 47.0, top + 15.0)
        _arm(a, [(41.0, top + 6.0), (47.0, top + 14.0), (49.0, top + 21.0)])
    elif k == 1:    # the top courses tumble off, water bursts up out of the mouth
        top = 30.0
        for x, y, r in ((12.0, 54.0, 3.4), (50.0, 55.0, 3.0), (16.0, 44.0, 2.6), (47.0, 42.0, 2.4)):
            _rock(a, x, y, r, 1)
        _legs(a, (37.0, 50.0, 38.0, 58.0), (25.0, 50.0, 24.0, 58.0))
        _well(a, 31.0, top, 49.0, 15.0, 4.6, eye=2, mouth="water", crack=True)
        _spring(a, 31.0, top, 18.0, k)
        _bucket(a, 44.0, top + 2.0, 49.0, top + 13.0, tilt=0.5)
    elif k == 2:    # collapsed into a low ring of stones, the spring falling back
        top = 42.0
        for x, y, r in ((10.0, 57.0, 3.4), (53.0, 57.0, 3.2), (18.0, 58.0, 2.6), (46.0, 58.0, 2.8)):
            _rock(a, x, y, r, 1)
        _well(a, 31.0, top, 54.0, 15.0, 4.6, eye=3, mouth="water")
        _spring(a, 31.0, top, 11.0, k)
        _bucket(a, 47.0, top + 5.0, 52.0, top + 10.0, tilt=0.9)
    else:           # still: an open stone well with clear water, the stones it shed lying around
        top = 46.0
        for x, y, r in ((9.0, 58.0, 3.2), (55.0, 58.0, 3.0), (17.0, 59.0, 2.4), (47.0, 59.0, 2.4)):
            _rock(a, x, y, r, 1)
        _well(a, 31.0, top, 55.0, 15.0, 4.6, eye=3, mouth="water")
        for x in (27.0, 29.0, 34.0):
            dot(a, x, top - 1.0, px.C("white.0"))                             # calm water glint
        _bucket(a, 48.0, top + 6.0, 52.0, top + 10.0, tilt=1.0)
    return finish(a, 62)


GENERATORS = {
    "enm_f1-water-raider_move_32": water_raider,
    "enm_f2-well-filler_move_32": well_filler,
    "enm_f3-strife-armour_move_32": strife_armour,
    "bos_sitnah-keeper_move_64": keeper_move,
    "bos_sitnah-keeper_windup_64": keeper_windup,
    "bos_sitnah-keeper_impact_64": keeper_impact,
    "bos_sitnah-keeper_death_64": keeper_death,
}
