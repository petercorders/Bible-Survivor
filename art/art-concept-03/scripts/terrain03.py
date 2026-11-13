"""Ground for art 03 (carried over from the art-02 module): low-contrast base tiles, meadow patches, a dirt path, clustered decals,
late-phase thorn vines and canopy shade. Built from parts that map to a Unity setup:
  base Tilemap (4 grass + 2 meadow variants, random pick + x-flip) | meadow patch blend (dithered edge)
  | path layer (spline or rule tiles) | decal sprites (no collider) | canopy shade overlay (alpha, optional)."""
import math
import numpy as np
import px03 as px

T = {k: px.C(k) for k in px.PAL if k.split(".")[0] in ("grass", "meadow", "soil", "rock", "flower", "vine", "canopy")}


def value_noise(w, h, cell, rng):
    gw, gh = w // cell + 3, h // cell + 3
    g = rng.random((gh, gw))
    ys, xs = np.mgrid[0:h, 0:w]
    fx, fy = xs / cell, ys / cell
    x0, y0 = fx.astype(int), fy.astype(int)
    tx, ty = fx - x0, fy - y0
    sx, sy = tx * tx * (3 - 2 * tx), ty * ty * (3 - 2 * ty)
    v = (g[y0, x0] * (1 - sx) + g[y0, x0 + 1] * sx) * (1 - sy) + (g[y0 + 1, x0] * (1 - sx) + g[y0 + 1, x0 + 1] * sx) * sy
    return v


def fbm(w, h, cell, rng, octaves=3):
    v, amp, tot = np.zeros((h, w)), 1.0, 0.0
    for o in range(octaves):
        v += amp * value_noise(w, h, max(2, cell >> o), rng)
        tot += amp
        amp *= 0.5
    return v / tot


# ---------------- base tiles (seamless 32x32) ----------------
def base_tile(seed, kind="grass"):
    rng = np.random.default_rng(seed)
    if kind == "grass":
        base, lite, tip, dark = T["grass.2"], T["grass.1"], T["grass.0"], T["grass.3"]
        blades, specks = 34, 9
    else:
        base, lite, tip, dark = T["meadow.1"], T["meadow.0"], T["meadow.0"], T["grass.2"]
        blades, specks = 26, 7
    a = px.new(32, 32)
    a[...] = base
    for _ in range(specks):                         # soft darker clumps
        cx, cy = rng.integers(0, 32, 2)
        for dx, dy in ((0, 0), (1, 0), (0, 1), (-1, 0), (1, 1)):
            if rng.random() < 0.75:
                a[(cy + dy) % 32, (cx + dx) % 32] = dark
    for _ in range(blades):                         # blade strokes: 1-2px light, shadow pixel at the root
        x, y = rng.integers(0, 32, 2)
        hgt = 2 if rng.random() < 0.6 else 1
        for k in range(hgt):
            a[(y - k) % 32, x] = lite
        if rng.random() < 0.25:
            a[(y - hgt) % 32, x] = tip
        a[(y + 1) % 32, x] = dark
        if rng.random() < 0.35:                     # second blade leaning
            a[(y - 1) % 32, (x + 1) % 32] = lite
    return a


# ---------------- decal sprites ----------------
def tuft(seed, w=11, h=8, dry=False):
    rng = np.random.default_rng(seed)
    a = px.new(w, h)
    lite = T["meadow.0"] if dry else T["grass.0"]
    mid = T["meadow.1"] if dry else T["grass.1"]
    dk = T["grass.3"]
    cx = w // 2
    for i in range(int(rng.integers(6, 9))):
        x = cx + int(rng.integers(-3, 4))
        hgt = int(rng.integers(3, h))
        lean = int(rng.choice([-1, 0, 0, 1]))
        for k in range(hgt):
            xx = x + (lean if k > hgt // 2 else 0)
            px.put(a, xx, h - 2 - k, lite if k >= hgt - 2 else mid)
    for x in range(cx - 3, cx + 4):                 # ground contact shade
        if a[h - 2, x, 3] or abs(x - cx) < 2:
            px.put(a, x, h - 1, dk)
    return a


def flowers(seed, w=16, h=10, kind=None):
    """A small bed: leafy clump with 5-8 blooms of one colour (2px blooms, yellow eye on pink/white)."""
    rng = np.random.default_rng(seed)
    a = px.new(w, h)
    k = int(rng.integers(0, 3)) if kind is None else kind
    petal = [T["flower.0"], T["flower.1"], T["flower.2"]][k]
    eye = T["soil.0"] if k == 0 else T["flower.0"]
    for _ in range(int(rng.integers(9, 14))):         # leaves first
        x, y = int(rng.integers(1, w - 2)), int(rng.integers(3, h - 1))
        px.put(a, x, y, T["grass.1"]); px.put(a, x + 1, y, T["grass.0"] if rng.random() < 0.3 else T["grass.1"])
        px.put(a, x, y + 1, T["grass.3"])
    for _ in range(int(rng.integers(5, 9))):
        x, y = int(rng.integers(1, w - 2)), int(rng.integers(1, h - 3))
        px.put(a, x, y, petal); px.put(a, x + 1, y, petal); px.put(a, x, y + 1, eye)
    return a


def clover(seed, w=14, h=8):
    rng = np.random.default_rng(seed)
    a = px.new(w, h)
    for _ in range(int(rng.integers(6, 10))):
        x, y = int(rng.integers(1, w - 2)), int(rng.integers(1, h - 2))
        px.put(a, x, y, T["grass.0"]); px.put(a, x + 1, y, T["grass.1"])
        px.put(a, x, y + 1, T["grass.1"]); px.put(a, x + 1, y + 1, T["grass.3"])
    return a


def pebbles(seed, w=10, h=6):
    rng = np.random.default_rng(seed)
    a = px.new(w, h)
    for _ in range(int(rng.integers(3, 6))):
        x, y = int(rng.integers(0, w - 1)), int(rng.integers(0, h - 2))
        px.put(a, x, y, T["rock.1"])
        if rng.random() < 0.5:
            px.put(a, x + 1, y, T["rock.2"])
        px.put(a, x, y + 1, T["rock.3"])
    return a


def rock(seed, w=14, h=9):
    rng = np.random.default_rng(seed)
    a = px.new(w, h)
    rr = [T["rock.0"], T["rock.1"], T["rock.2"], T["rock.3"]]
    px.lit_ellipse(a, w / 2, h / 2 + 0.5, w / 2 - 1.2, h / 2 - 1.2, rr, cuts=(0.84, 0.62, 0.40),
                   tex=lambda x, y, u, v, i: min(3, i + 1) if (x * 5 + y * 3 + seed) % 9 == 0 and i < 3 else i)
    # sit it in the grass: darker contact line and a couple of blades over the base
    for x in range(1, w - 1):
        col = [y for y in range(h) if a[y, x, 3]]
        if col:
            px.put(a, x, col[-1] + 1 if col[-1] + 1 < h else col[-1], T["grass.4"])
    for x in (2, w - 3):
        px.put(a, x, h - 2, T["grass.1"])
        px.put(a, x, h - 3, T["grass.1"])
    return a


def edge_stone(seed, w=7, h=5):
    a = px.new(w, h)
    px.lit_ellipse(a, w / 2, h / 2, w / 2 - 0.4, h / 2 - 0.6, [T["rock.1"], T["rock.2"], T["rock.2"], T["rock.3"]],
                   cuts=(0.82, 0.6, 0.38))
    for x in range(w):
        col = [y for y in range(h) if a[y, x, 3]]
        if col:
            px.put(a, x, col[-1], T["rock.3"])
    return a


def root(seed, w=46, h=18, flip=False):
    rng = np.random.default_rng(seed)
    a = px.new(w, h)
    for k in range(3):
        y0 = 3 + k * 4 + rng.random() * 2
        pts = [(0, y0), (w * 0.35, y0 + rng.random() * 3), (w * 0.7, y0 + 2 + rng.random() * 3), (w - 2, y0 + 4)]
        px.tube(a, pts, lambda t: 2.2 * (1 - t) + 0.6,
                lambda x, y, t, nx, ny, it: T["soil.0"] if it > 0.80 else (T["vine.0"] if it > 0.50 else T["vine.1"]))
    for x in range(w):                                   # contact shadow under the roots
        col = [y for y in range(h) if a[y, x, 3]]
        if col and col[-1] + 1 < h:
            px.put(a, x, col[-1] + 1, T["grass.4"])
    return a[:, ::-1].copy() if flip else a


def leaves(seed, w=12, h=7):
    rng = np.random.default_rng(seed)
    a = px.new(w, h)
    for _ in range(int(rng.integers(4, 7))):
        x, y = int(rng.integers(1, w - 2)), int(rng.integers(1, h - 1))
        c = [T["meadow.0"], T["soil.1"], T["soil.0"]][int(rng.integers(0, 3))]
        px.put(a, x, y, c)
        px.put(a, x + 1, y, c)
        px.put(a, x + 1, y + 1, T["soil.2"])
    return a


def thorn_vine(seed, w=44, h=22):
    rng = np.random.default_rng(seed)
    a = px.new(w, h)
    for k in range(2):
        y = h * (0.35 + 0.3 * k)
        pts = [(1, y)]
        for i in range(1, 6):
            pts.append((w * i / 5 - 1, h * 0.2 + rng.random() * h * 0.6))
        px.tube(a, pts, lambda t: 1.0 if t < 0.8 else 0.6,
                lambda x, y, t, nx, ny, it: T["vine.0"] if it > 0.6 else T["vine.1"], samples=10)
    ys, xs = np.nonzero(a[..., 3])
    for i in rng.choice(len(xs), size=min(len(xs), 14), replace=False):  # small thorns
        x, y = xs[i], ys[i]
        dx, dy = (1, -1) if rng.random() < 0.5 else (-1, -1)
        if 0 <= y + dy < h and 0 <= x + dx < w and not a[y + dy, x + dx, 3]:
            a[y + dy, x + dx] = T["rock.1"]
    for x in range(w):
        col = [y for y in range(h) if a[y, x, 3]]
        if col and col[-1] + 1 < h:
            px.put(a, x, col[-1] + 1, T["grass.4"])
    # withered leaves on the vine
    for _ in range(4):
        i = int(rng.integers(0, len(xs)))
        px.put(a, xs[i], ys[i], T["meadow.0"])
    return a


DECALS = {}


def build_decals():
    DECALS.clear()
    for i in range(3):
        DECALS[f"dec_eden_tuft-{i + 1}_11x8"] = tuft(100 + i)
    DECALS["dec_eden_tuft-dry_11x8"] = tuft(104, dry=True)
    for i in range(3):
        DECALS[f"dec_eden_flowers-{i + 1}_16x10"] = flowers(200 + i, kind=i)   # 1 yellow, 2 pink, 3 pale blue
    DECALS["dec_eden_pebbles_10x6"] = pebbles(300)
    for i in range(2):
        DECALS[f"dec_eden_clover-{i + 1}_14x8"] = clover(250 + i)
    DECALS["dec_eden_rock-m_14x9"] = rock(401)
    DECALS["dec_eden_rock-s_10x7"] = rock(402, 10, 7)
    for i in range(2):
        DECALS[f"dec_eden_edge-stone-{i + 1}_7x5"] = edge_stone(500 + i)
    DECALS["dec_eden_root_46x18"] = root(600)
    DECALS["dec_eden_leaves_12x7"] = leaves(700)
    for i in range(2):
        DECALS[f"dec_eden_thorn-vine-{i + 1}_44x22"] = thorn_vine(800 + i)
    return DECALS


TILES = {}


def build_tiles():
    TILES.clear()
    for i, sd in enumerate((11, 23, 37, 41)):
        TILES[f"til_eden_grass-{'abcd'[i]}_static_32"] = base_tile(sd, "grass")
    for i, sd in enumerate((53, 67)):
        TILES[f"til_eden_meadow-{'ab'[i]}_static_32"] = base_tile(sd, "meadow")
    return TILES


# ---------------- world ----------------
PATH = [(-20, 170), (120, 205), (250, 268), (330, 352), (372, 470), (455, 560), (590, 600), (700, 652), (830, 700)]
GARDEN_ARCS = [((248, 330), 46, 200, 330), ((560, 300), 34, 150, 320)]   # centre, radius, a0, a1
CANOPY = [(70, 250, 120), (760, 560, 110), (520, 20, 90)]               # shade blobs (cx, cy, r)
RUIN = (640, 470)


def world(size=800, seed=702, late=True, with_shade=True, path_cells=None, path_tiles=None, path_masks=None):
    """03: when path_cells (list of strings, '#' = path, one char per 32 px cell) and the 16 path tiles are given,
    the dirt path is built from those tiles (the same tiles that are exported) instead of the 02 spline."""
    rng = np.random.default_rng(seed)
    build_tiles(); build_decals()
    gr = [TILES[f"til_eden_grass-{c}_static_32"] for c in "abcd"]
    md = [TILES[f"til_eden_meadow-{c}_static_32"] for c in "ab"]
    G = np.zeros((size, size, 4), np.uint8)
    M = np.zeros((size, size, 4), np.uint8)
    n = size // 32
    for ty in range(n):
        for tx in range(n):
            t = gr[int(rng.integers(0, 4))]
            m = md[int(rng.integers(0, 2))]
            if rng.random() < 0.5:
                t = t[:, ::-1]
            if rng.random() < 0.5:
                m = m[:, ::-1]
            G[ty * 32:(ty + 1) * 32, tx * 32:(tx + 1) * 32] = t
            M[ty * 32:(ty + 1) * 32, tx * 32:(tx + 1) * 32] = m
    # meadow patches: low-frequency noise with a 2-step ordered-dither edge
    f = fbm(size, size, 160, rng)
    bay = np.tile(px.BAYER4, (size // 4, size // 4))
    meadow = (f + (bay - 0.5) * 0.05) > 0.56
    G[meadow] = M[meadow]
    # darker lush patches
    f2 = fbm(size, size, 70, rng, octaves=4)
    lush = ((f2 + (bay - 0.5) * 0.05) < 0.33) & ~meadow
    sel = lush & np.all(G == T["grass.2"], axis=2)
    G[sel] = T["grass.3"]

    ys, xs = np.mgrid[0:size, 0:size]
    placed = []

    def stamp(name, x, y, flipx=False):
        spr = DECALS[name][:, ::-1] if flipx else DECALS[name]
        px.over(G, spr, int(x - spr.shape[1] / 2), int(y - spr.shape[0]))
        placed.append((name, int(x), int(y), bool(flipx)))

    path = np.zeros((size, size), bool)
    if path_cells is not None:
        import tiles03 as tl
        M = tl.mask_grid(path_cells)
        for cy, row in enumerate(M):
            for cx, m in enumerate(row):
                if m < 0:
                    continue
                G[cy * 32:(cy + 1) * 32, cx * 32:(cx + 1) * 32] = path_tiles[m]
                path[cy * 32:(cy + 1) * 32, cx * 32:(cx + 1) * 32] = path_masks[m][4:36, 4:36]
        # tufts along the path where it meets grass
        for cy, row in enumerate(M):
            for cx, m in enumerate(row):
                if m < 0:
                    continue
                for bit, ox, oy in ((1, 0, -1), (4, 0, 1)):
                    if not (m & bit) and rng.random() < 0.6:
                        x = cx * 32 + 16 + rng.normal(0, 7)
                        y = cy * 32 + (2 if oy < 0 else 32) + 2
                        stamp(f"dec_eden_tuft-{int(rng.integers(1, 4))}_11x8", x, y, rng.random() < 0.5)

    # garden traces: low stone borders along arcs with flower beds inside
    for (cx, cy), r, a0, a1 in GARDEN_ARCS:
        for ang in range(a0, a1, 9):
            x = cx + r * math.cos(math.radians(ang)); y = cy + r * 0.55 * math.sin(math.radians(ang))
            if rng.random() < 0.85:
                stamp(f"dec_eden_edge-stone-{int(rng.integers(1, 3))}_7x5", x, y + 3)
        for _ in range(5):
            stamp(f"dec_eden_flowers-{int(rng.integers(1, 4))}_16x10", cx + rng.normal(0, r * 0.35), cy + rng.normal(0, r * 0.18) + 4)
    # scattered clusters: tufts + flowers + rocks
    for _ in range(48):
        cx, cy = rng.integers(0, size, 2)
        kind = rng.random()
        for _ in range(int(rng.integers(2, 6))):
            x, y = cx + rng.normal(0, 9), cy + rng.normal(0, 6)
            if kind < 0.45:
                stamp("dec_eden_tuft-dry_11x8" if meadow[int(np.clip(y, 0, size - 1)), int(np.clip(x, 0, size - 1))]
                      else f"dec_eden_tuft-{int(rng.integers(1, 4))}_11x8", x, y, rng.random() < 0.5)
            elif kind < 0.7:
                stamp(f"dec_eden_clover-{int(rng.integers(1, 3))}_14x8", x, y, rng.random() < 0.5)
            else:
                stamp(f"dec_eden_flowers-{int(rng.integers(1, 4))}_16x10", x, y)
    for x, y, nm in ((150, 430, "dec_eden_rock-m_14x9"), (470, 260, "dec_eden_rock-s_10x7"), (690, 330, "dec_eden_rock-m_14x9"),
                     (300, 540, "dec_eden_rock-s_10x7"), (600, 520, "dec_eden_rock-s_10x7"), (95, 330, "dec_eden_rock-s_10x7")):
        stamp(nm, x, y)
        stamp("dec_eden_tuft-1_11x8", x - 8, y + 2)
    # ruin: broken stone edging of an old garden bed + thorn vines (late phase)
    rx, ry = RUIN
    for k in range(7):
        stamp(f"dec_eden_edge-stone-{1 + k % 2}_7x5", rx - 24 + k * 8, ry + (k % 3 == 1))
    for k in range(4):
        stamp(f"dec_eden_edge-stone-{1 + k % 2}_7x5", rx - 26, ry - 8 - k * 6)
    # canopy corners: roots + leaf litter
    stamp("dec_eden_root_46x18", 40, 300)
    stamp("dec_eden_root_46x18", 770, 610, True)
    for cx, cy, r in CANOPY:
        for _ in range(7):
            stamp("dec_eden_leaves_12x7", cx + rng.normal(0, r * 0.4), cy + rng.normal(0, r * 0.3))
    if late:
        for x, y, k, fl in ((rx + 18, ry + 26, 1, False), (rx - 40, ry + 18, 2, True), (120, 600, 1, False),
                            (740, 250, 2, True), (230, 150, 1, False), (520, 700, 2, False), (80, 520, 2, True)):
            stamp(f"dec_eden_thorn-vine-{k}_44x22", x, y, fl)

    shade = np.zeros((size, size), bool)
    if with_shade:
        fsh = fbm(size, size, 18, rng)
        for cx, cy, r in CANOPY:
            dist = np.hypot(xs - cx, (ys - cy) * 1.25) / r
            shade |= (dist + (fsh - 0.5) * 0.55 + (bay - 0.5) * 0.08) < 1.0
    return G, shade, placed, meadow, path


def apply_shade(img, shade, alpha=0.22):
    """Canopy shade overlay (separate, dimmable layer)."""
    out = img.copy()
    c = np.array(px.C("canopy.0")[:3], np.float32)
    f = out[shade, :3].astype(np.float32)
    out[shade, :3] = np.clip(np.round(f * (1 - alpha) + c * alpha), 0, 255).astype(np.uint8)
    return out
