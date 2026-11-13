"""Weapons, pickups and effects for art 03 (carried over from the art-02 module).
Object sprites (book, jawbone, stone, gems) are opaque with a dark outline. Effect layers (glow, smear,
trail, spark) are separate sprites using the semi-transparent 'fxa' ramp so they can be dimmed or switched
off per platform (mobile option) without touching the object sprites."""
import math
import numpy as np
import px03 as px

K = px.C("outline.0")

BOOK_KEYS = {"k": "outline.0", "B": "book.0", "b": "book.1", "d": "book.2", "y": "gold.0", "Y": "gold.1",
             "g": "gold.2", "p": "page.0", "P": "page.1", "r": "ribbon.0"}
BOOK = [
    [  # frame 0: front cover, page block to the right/bottom
        "",
        "..kkkkkkkkkkk",
        "..kyBBBBBBBYkkk",
        "..kBbbbybbbdkpPk",
        "..kBbbbYbbbdkpPk",
        "..kBbyYYYYbdkpPk",
        "..kBbbbYbbbdkpPk",
        "..kBbbbYbbbdkpPk",
        "..kbbbbgbbbdkpPk",
        "..kbbbbbbbbdkpPk",
        "..kbbbbbbbddkpPk",
        "..kYbbbbbbdYkpPk",
        "..kkkkkkkkkkkpPk",
        "...kPPPrPPPPPPPk",
        "....kkkrkkkkkkkk",
        "......kr",
    ],
    [  # frame 1: turned, spine towards the viewer
        "",
        ".....kkkkkk",
        "....kyBBBBYk",
        "....kBbbbbdkk",
        "....kyYYYYgkPk",
        "....kBbbbbdkPk",
        "....kBbbbbdkPk",
        "....kBbbbbdkPk",
        "....kbbbbbdkPk",
        "....kyYYYYgkPk",
        "....kbbbbbdkPk",
        "....kYbbbddkPk",
        "....kkkkkkkkPk",
        ".....kPPrPPPPk",
        "......kkrkkkk",
        "........r",
    ],
]


def book(frame=0):
    a = px.from_rows(BOOK[frame], BOOK_KEYS, 16, 16)
    # the book's right/bottom outline columns may be ragged in the grid; close them with the outline
    return px.outline(a)


def book_glow(frame=0):
    """Soft halo + two sparkles, separate layer (fxa)."""
    base = book(frame)
    m = base[..., 3] > 0
    g = px.new(20, 20)
    gm = np.zeros((20, 20), bool)
    gm[2:18, 2:18] = m
    halo = px.C("fxa.2")
    for y in range(20):
        for x in range(20):
            if gm[y, x]:
                continue
            for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)):
                xx, yy = x + dx, y + dy
                if 0 <= xx < 20 and 0 <= yy < 20 and gm[yy, xx]:
                    g[y, x] = halo
                    break
    for x, y in ((1, 3), (17, 15)):
        px.put(g, x, y, px.C("spark.0"))
        for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)):
            px.put(g, x + dx, y + dy, px.C("fxa.0"))
    return g


def jawbone():
    """16x16 jawbone of an ass (side view): condyle + coronoid prongs at the back, straight body with a
    serrated tooth row on top, rounded chin at the front, nerve hole."""
    a = px.new(16, 16)
    bn = px.ramp("bone")
    shade = lambda x, y, t, nx, ny, it: bn[min(3, px.level(it, (0.80, 0.56, 0.30)))]
    px.tube(a, [(4.2, 10.0), (8.0, 10.6), (11.6, 10.4), (14.0, 9.4)], lambda t: 1.75 - 0.35 * t, shade)
    px.tube(a, [(4.4, 10.4), (3.6, 7.0), (2.8, 3.6)], lambda t: 1.6 - 0.5 * t, shade)       # ramus -> condyle
    px.tube(a, [(5.2, 8.6), (6.4, 6.4), (7.2, 4.6)], lambda t: 1.2 - 0.6 * t, shade)        # coronoid prong
    px.lit_ellipse(a, 2.8, 3.2, 1.4, 1.2, bn, cuts=(0.78, 0.52, 0.28))
    for x in range(7, 14):                                                                  # tooth row
        col = [y for y in range(16) if a[y, x, 3]]
        if not col:
            continue
        top = col[0]
        if x % 2 == 1:
            px.put(a, x, top - 1, px.C("white.0"))
            px.put(a, x, top, px.C("white.1"))
        else:
            px.put(a, x, top, bn[2])
    px.put(a, 6, 11, bn[3])                                                                 # foramen
    return px.outline(a)


def jaw_smear(r_in=25.0, r_out=31.0, a0=-60, a1=60, lead=48):
    """64x64 swing smear around the body centre (32,32): thin crescent made of a solid leading
    edge and two semi-transparent trailing bands. Leaves the inner area (enemies near the player)
    and most of the band see-through."""
    g = px.new(64, 64)
    core, mid, faint = px.C("bone.0"), px.C("fxa.0"), px.C("fxa.1")
    for y in range(64):
        for x in range(64):
            dx, dy = x + 0.5 - 32, 32 - (y + 0.5)
            r, ang = math.hypot(dx, dy), math.degrees(math.atan2(dy, dx))
            if not (a0 <= ang <= a1):
                continue
            t = (ang - a0) / (a1 - a0)               # 0 = trailing end, 1 = leading edge
            thick = 1.0 + 4.0 * t
            ro = r_out - 0.5
            if ro - thick <= r < ro:
                if ang >= lead - 30 and r >= ro - 2.2:
                    g[y, x] = core                   # opaque 2px leading edge over the last ~40 degrees
                elif t > 0.35:
                    g[y, x] = mid
                elif t > 0.08 and (x + y) % 2 == 0:
                    g[y, x] = faint
    # speed ticks inside the arc
    for ang in (-35, -5, 25):
        rr = r_in + 0.5
        x = int(32 + rr * math.cos(math.radians(ang)))
        y = int(32 - rr * math.sin(math.radians(ang)))
        px.put(g, x, y, faint)
    return g


def stone():
    a = px.new(8, 8)
    px.lit_ellipse(a, 4.0, 4.1, 2.8, 2.5, px.ramp("stone"), cuts=(0.82, 0.58, 0.32))
    return px.outline(a)


def stone_trail(length=18):
    """Trail behind a stone flying to the right; tapering, dotted, semi-transparent."""
    g = px.new(length + 2, 8)
    for i in range(length):
        x = length - 1 - i
        w = 2 if i < length * 0.45 else 1
        if i > length * 0.7 and i % 2:
            continue
        col = px.C("fxa.3") if i < length * 0.5 else px.C("fxa.1")
        for dy in range(w):
            px.put(g, x, 3 + dy - (w // 2) + 1, col)
    return g


def spark():
    g = px.new(16, 16)
    c0, c1, w = px.C("spark.0"), px.C("spark.1"), px.C("white.0")
    for d in range(1, 6):
        for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)):
            px.put(g, 8 + dx * d, 8 + dy * d, c1 if d > 3 else c0)
    for d in (1, 2, 3):
        for dx, dy in ((1, 1), (-1, 1), (1, -1), (-1, -1)):
            if d < 3:
                px.put(g, 8 + dx * d, 8 + dy * d, c1)
    px.put(g, 8, 8, w)
    return g


def sling_icon():
    a = px.new(16, 16)
    lt = px.ramp("leather")
    px.line(a, 3, 2, 6, 10, lt[1]); px.line(a, 4, 2, 7, 10, lt[0])
    px.line(a, 13, 3, 10, 10, lt[1])
    px.lit_ellipse(a, 8.5, 11.2, 3.4, 2.2, lt, cuts=(0.82, 0.55, 0.3))
    px.lit_ellipse(a, 8.6, 9.9, 1.7, 1.4, px.ramp("stone"), cuts=(0.8, 0.55, 0.3))
    px.put(a, 2, 1, lt[2]); px.put(a, 3, 1, lt[2])
    return px.outline(a)


GEM_KEYS = {"k": "outline.0", "h": "{r}.0", "c": "{r}.1", "C": "{r}.2", "d": "{r}.3"}
GEM = ["...kk", "..khck", ".khhcCk", "khhccCCk", "kcccCCdk", ".kcCCdk", "..kCdk", "...kk"]


def gem(kind="gem"):
    keys = {k: v.format(r=kind) for k, v in GEM_KEYS.items()}
    return px.from_rows(GEM, keys, 8, 8)


def shadow(w=16, h=6, key="shadow.0"):
    g = px.new(w, h)
    c = px.C(key)
    for y in range(h):
        for x in range(w):
            u, v = (x + 0.5 - w / 2) / (w / 2), (y + 0.5 - h / 2) / (h / 2)
            if u * u + v * v <= 1.0:
                g[y, x] = c
    return g


# ---------------------------------------------------------------- 03: animated frames
JAW_HEADS = (-25, 15, 55, 60)       # leading-edge angle per frame (degrees, 0 = facing direction)
JAW_TAILS = (-60, -60, -35, 0)


def jaw_swing(frame, r_out=31.0):
    """4-frame 120-degree swing (20 fps, once) drawn facing right around the cell centre (32,32).
    Frames 0-2: opaque 2px leading edge + see-through trailing band; frame 3: dithered fade only.
    Returns (frame RGBA, attach_px) where attach_px is the leading-edge point for the jawbone sprite."""
    g = px.new(64, 64)
    core, mid, faint = px.C("bone.0"), px.C("fxa.0"), px.C("fxa.1")
    head, tail = JAW_HEADS[frame], JAW_TAILS[frame]
    ro = r_out - 0.5
    for y in range(64):
        for x in range(64):
            dx, dy = x + 0.5 - 32, 32 - (y + 0.5)
            r, ang = math.hypot(dx, dy), math.degrees(math.atan2(dy, dx))
            if not (tail <= ang <= head):
                continue
            t = (ang - tail) / max(1e-6, head - tail)
            thick = 1.0 + 4.0 * t
            if not (ro - thick <= r < ro):
                continue
            if frame < 3:
                if ang >= head - 25 and r >= ro - 2.2:
                    g[y, x] = core
                elif t > 0.35:
                    g[y, x] = mid
                elif t > 0.08 and (x + y) % 2 == 0:
                    g[y, x] = faint
            elif t > 0.3 and (x + y) % 2 == 0:
                g[y, x] = faint
    a = math.radians(head)
    attach = (round(32 + (ro - 1) * math.cos(a)), round(32 - (ro - 1) * math.sin(a)))
    return g, attach


def stone_spin(frame):
    """8x8 stone, 2 frames: the dark chip on the stone turns half a revolution."""
    a = px.new(8, 8)
    rx, ry = (2.8, 2.5) if frame == 0 else (2.5, 2.8)
    px.lit_ellipse(a, 4.0, 4.1, rx, ry, px.ramp("stone"), cuts=(0.82, 0.58, 0.32))
    chip = ((5, 3), (5, 4)) if frame == 0 else ((3, 5), (2, 5))
    for x, y in chip:
        px.put(a, x, y, px.C("stone.3"))
    return px.outline(a)


def hit(frame):
    """16x16 hit spark, 3 frames (15 fps, once): flash -> star -> scattered embers. Opaque pixels only."""
    g = px.new(16, 16)
    c0, c1, w = px.C("spark.0"), px.C("spark.1"), px.C("white.0")
    if frame == 0:
        for dx, dy in ((0, 0), (1, 0), (-1, 0), (0, 1), (0, -1)):
            px.put(g, 8 + dx, 8 + dy, w)
        for d in (2, 3):
            for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)):
                px.put(g, 8 + dx * d, 8 + dy * d, c0)
    elif frame == 1:
        for d in range(1, 7):
            for dx, dy in ((1, 0), (-1, 0), (0, 1), (0, -1)):
                px.put(g, 8 + dx * d, 8 + dy * d, c0 if d < 4 else c1)
        for d in (1, 2, 3):
            for dx, dy in ((1, 1), (-1, 1), (1, -1), (-1, -1)):
                px.put(g, 8 + dx * d, 8 + dy * d, c1)
        px.put(g, 8, 8, w)
    else:
        for ang in range(0, 360, 60):
            r = 6 if ang % 120 == 0 else 5
            x = int(round(8 + r * math.cos(math.radians(ang + 15))))
            y = int(round(8 - r * math.sin(math.radians(ang + 15))))
            px.put(g, x, y, c1)
            if ang % 120 == 0:
                px.put(g, x + (1 if x < 8 else -1), y, c0)
    return g


GEM1 = ["...kk", "..kWck", ".khWcCk", "khhhcCCk", "kcccCCdk", ".kcCCdk", "..kCdk", "...kk"]


def gem_frames(kind="gem"):
    """2 frames (4 fps, loop): base gem and a glint frame (white facet line)."""
    keys = {k: v.format(r=kind) for k, v in GEM_KEYS.items()}
    keys1 = dict(keys, W="white.0")
    return [px.from_rows(GEM, keys, 8, 8), px.from_rows(GEM1, keys1, 8, 8)]


def death_burst(frame):
    """Common enemy death, 32x32, 4 frames (12 fps, once), opaque pixels only so it reads the same
    with any blending. Pivot (16,1) like the enemies; the burst is centred on the body (row 20)."""
    g = px.new(32, 32)
    sm = px.ramp("smoke")
    w, c0 = px.C("white.0"), px.C("spark.0")
    cx, cy = 16.0, 19.5
    if frame == 0:
        for y in range(32):
            for x in range(32):
                r = math.hypot(x + 0.5 - cx, y + 0.5 - cy)
                if r < 3.2:
                    g[y, x] = w
                elif r < 4.6:
                    g[y, x] = c0
        return px.outline(g, px.C("smoke.2"))
    puffs = [(0, 1.0), (55, 0.8), (115, 1.1), (180, 0.9), (235, 1.0), (300, 0.85)]
    R = {1: 6.0, 2: 9.0, 3: 11.0}[frame]
    rad = {1: 3.0, 2: 2.6, 3: 1.4}[frame]
    rise = {1: 0.0, 2: 1.5, 3: 3.5}[frame]
    for ang, k in puffs:
        px_, py_ = cx + R * k * math.cos(math.radians(ang)), cy - R * k * math.sin(math.radians(ang)) - rise
        for y in range(32):
            for x in range(32):
                dx, dy = x + 0.5 - px_, y + 0.5 - py_
                d = math.hypot(dx, dy)
                if d <= rad:
                    if frame == 3 and (x + y) % 2:
                        continue
                    g[y, x] = sm[0] if (dx + dy) < -0.6 * rad else (sm[1] if d < rad * 0.7 else sm[2])
    if frame == 1:
        for y in range(32):
            for x in range(32):
                if math.hypot(x + 0.5 - cx, y + 0.5 - cy) < 2.0:
                    g[y, x] = w
    motes = {1: [], 2: [(16, 13), (11, 17), (22, 21)], 3: [(15, 8), (9, 13), (24, 15), (19, 11)]}[frame]
    for x, y in motes:
        px.put(g, x, y, c0)
    return g
