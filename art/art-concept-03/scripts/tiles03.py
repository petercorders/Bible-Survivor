"""Dirt-path tile set for art 03: 16 tiles chosen by a 4-direction neighbour mask.

mask = N*1 + E*2 + S*4 + W*8, where a bit is set when the neighbouring cell in that direction is also path.
Diagonal neighbours are ignored. Tile index in the strip = mask. Every connected edge has the same cross-section
(x or y from 7 to 24 plus a 32-periodic wobble), and all texture noise is a function of tile-local coordinates on a
32x32 torus, so any two connected tiles meet without a seam. Unsupported: paths two or more cells wide (the inner
corner of a 2x2 block shows a grass notch) and diagonal-only contact (drawn as two separate ends).
"""
import math
import numpy as np
import px03 as px
import terrain03 as tr

N, E, S, W = 1, 2, 4, 8
NAMES = {0: "isolated", 1: "end-n", 2: "end-e", 4: "end-s", 8: "end-w", 5: "straight-ns", 10: "straight-ew",
         3: "corner-ne", 6: "corner-es", 12: "corner-sw", 9: "corner-wn", 7: "tee-nes", 11: "tee-new",
         13: "tee-nsw", 14: "tee-esw", 15: "cross"}
LO, HI = 7.0, 25.0          # path band [7, 25) in cell pixels: 18 px wide
ROUND = 5.0
C = {k: px.C(k) for k in ("path.0", "path.1", "path.2", "path.3", "path.4", "grass.1", "grass.0", "grass.3")}


def _hash(x, y, k=0):
    x, y = x % 32, y % 32
    v = math.sin(x * 12.9898 + y * 78.233 + k * 37.719) * 43758.5453
    return v - math.floor(v)


def _wob(t, phase):
    """32-periodic edge wobble in pixels (-1, 0, +1)."""
    return round(1.1 * math.sin(2 * math.pi * (t % 32) / 32 * 2 + phase))


def inside(mask, x, y):
    """Path membership for tile-local pixel (x, y); valid for x, y in [-4, 36) so edges can see neighbours."""
    cx, cy = x + 0.5, y + 0.5
    lo_x, hi_x = LO + _wob(y, 0.3), HI + _wob(y, 2.1)
    lo_y, hi_y = LO + _wob(x, 1.2), HI + _wob(x, 4.0)
    if mask & N and cy < 16 and lo_x <= cx < hi_x:
        return True
    if mask & S and cy >= 16 and lo_x <= cx < hi_x:
        return True
    if mask & W and cx < 16 and lo_y <= cy < hi_y:
        return True
    if mask & E and cx >= 16 and lo_y <= cy < hi_y:
        return True
    # rounded core: corners round only where no arm meets them
    if not (LO <= cx < HI and LO <= cy < HI):
        return False
    for (kx, ky, bits) in ((LO + ROUND, LO + ROUND, N | W), (HI - ROUND, LO + ROUND, N | E),
                           (LO + ROUND, HI - ROUND, S | W), (HI - ROUND, HI - ROUND, S | E)):
        if (cx - kx) * (1 if kx > 16 else -1) > 0 and (cy - ky) * (1 if ky > 16 else -1) > 0:
            if mask & bits:
                continue
            if math.hypot(cx - kx, cy - ky) > ROUND:
                return False
    return True


def path_tile(mask, grass_seed=11):
    a = tr.base_tile(grass_seed, "grass").copy()
    ins = np.array([[inside(mask, x, y) for x in range(-4, 36)] for y in range(-4, 36)])
    I = lambda x, y: ins[y + 4, x + 4]
    for y in range(32):
        for x in range(32):
            if I(x, y):
                n_up = not I(x, y - 1)
                edge = n_up or not I(x + 1, y) or not I(x - 1, y) or not I(x, y + 1)
                lip2 = (not I(x, y - 2)) and not n_up
                h = _hash(x, y)
                if edge:
                    c = C["path.4"] if n_up else C["path.3"]
                elif lip2:
                    c = C["path.3"]
                else:
                    deep = all(I(x + dx, y + dy) for dx in (-3, 0, 3) for dy in (-3, 0, 3))
                    c = C["path.0"] if (deep and h > 0.55) else C["path.1"]
                    if h > 0.965:
                        c = C["path.3"]                      # pebble
                    elif h < 0.05 and deep:
                        c = C["path.2"]
                a[y, x] = c
            else:
                near = I(x, y + 1) or I(x + 1, y) or I(x - 1, y)
                if near and _hash(x, y, 3) < 0.45:
                    a[y, x] = C["grass.1"]               # grass lip leaning over the path edge
    # pebble highlights (above a pebble pixel, still inside the path)
    for y in range(1, 32):
        for x in range(32):
            if I(x, y) and I(x, y - 1) and _hash(x, y) > 0.965 and not (not I(x, y - 1)):
                if tuple(a[y - 1, x]) in (C["path.1"], C["path.0"]):
                    a[y - 1, x] = C["path.0"]
    return a, ins.copy()          # 40x40 membership incl. 4 px of each neighbour as this tile expects it


def build():
    tiles, masks = [], []
    for m in range(16):
        t, ins = path_tile(m)
        tiles.append(t); masks.append(ins)
    return tiles, masks


def mask_grid(cells):
    """cells: list of strings ('#' = path). Returns int mask per cell (-1 = not path)."""
    h, w = len(cells), max(len(r) for r in cells)
    P = [[(x < len(r) and r[x] == "#") for x in range(w)] for r in cells]
    out = [[-1] * w for _ in range(h)]
    for y in range(h):
        for x in range(w):
            if not P[y][x]:
                continue
            m = 0
            if y > 0 and P[y - 1][x]: m |= N
            if x < w - 1 and P[y][x + 1]: m |= E
            if y < h - 1 and P[y + 1][x]: m |= S
            if x > 0 and P[y][x - 1]: m |= W
            out[y][x] = m
    return out


def render_map(cells, tiles, grass_tiles, seed=5):
    rng = np.random.default_rng(seed)
    M = mask_grid(cells)
    h, w = len(M), len(M[0])
    img = px.new(w * 32, h * 32)
    for y in range(h):
        for x in range(w):
            if M[y][x] >= 0:
                t = tiles[M[y][x]]
            else:
                t = grass_tiles[int(rng.integers(0, len(grass_tiles)))]
                if rng.random() < 0.5:
                    t = t[:, ::-1]
            img[y * 32:(y + 1) * 32, x * 32:(x + 1) * 32] = t
    return img, M


def seam_check(M, masks):
    """masks: 40x40 membership per tile (4 px margin = what the tile expects of its neighbours).
    For every pair of neighbouring cells, each tile's border pixels must equal what the neighbour tile's own shape
    predicts for them (so the outline continues across the border), and a tile must not put path on a border whose
    neighbour is grass. Returns a list of problems (empty = pass)."""
    P = 4
    probs = []
    h, w = len(M), len(M[0])
    own = lambda e, x, y: bool(e[y + P, x + P])
    for y in range(h):
        for x in range(w):
            m = M[y][x]
            if m < 0:
                continue
            a = masks[m]
            for name, dx, dy in (("E", 1, 0), ("S", 0, 1), ("N", 0, -1), ("W", -1, 0)):
                xx, yy = x + dx, y + dy
                nb = M[yy][xx] if 0 <= xx < w and 0 <= yy < h else -1
                for i in range(32):
                    ax, ay = {"E": (31, i), "W": (0, i), "S": (i, 31), "N": (i, 0)}[name]
                    mine = own(a, ax, ay)
                    if nb < 0:
                        if mine:
                            probs.append(f"cell ({x},{y}) mask {m}: path on the {name} border next to grass")
                            break
                        continue
                    b = masks[nb]
                    expect = own(b, ax - 32 * dx, ay - 32 * dy)
                    if mine != expect:
                        probs.append(f"cells ({x},{y})->({xx},{yy}) masks {m}/{nb}: {name} border pixel {i} differs")
                        break
    return probs


TEST_MAP = [
    "..............",
    "...#......#...",
    "...#......#...",
    ".#####....#...",
    "...#......###.",
    "...#........#.",
    "..............",
    ".###...#......",
    "..#...###.....",
    "..........#...",
    ".#....#..###..",
    ".##..##.......",
    ".#....#.......",
    "..............",
    ".###......#...",
    ".#.#..........",
    ".###..........",
    "..............",
]
UNSUPPORTED_MAP = [
    ".........",
    ".##...#..",
    ".##....#.",
    ".........",
]
SCENE_PATH = None   # set by build script (25x25 cell grid for the 800px scene)
