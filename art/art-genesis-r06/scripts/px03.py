"""Pixel helpers for art 03 (carried over from the art-02 module): palette lookup, RGBA sprite arrays, lit shapes, outline, compositing.
Sprites are numpy uint8 arrays (h, w, 4). Every painted colour comes from src/palette_v03.json by name
("skin.1"), so the build can map each sprite back to palette keys for the pixel-source JSON.
"""
import json, math, os
import numpy as np

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.dirname(HERE)
PALJ = json.load(open(os.path.join(ROOT, "src", "palette_v03.json"), encoding="utf-8"))


def hex_rgba(h):
    h = h.lstrip("#")
    return (int(h[0:2], 16), int(h[2:4], 16), int(h[4:6], 16), int(h[6:8], 16) if len(h) == 8 else 255)


PAL = {}
for group in ("actor", "ui", "terrain"):
    for ramp, cols in PALJ[group].items():
        for i, c in enumerate(cols):
            PAL[f"{ramp}.{i}"] = hex_rgba(c)
NAME_OF = {}
for k, v in PAL.items():
    NAME_OF.setdefault(v, k)
RAMP_OF = {v: k.split(".")[0] for k, v in PAL.items()}


def C(name):
    return PAL[name]


def ramp(name):
    out, i = [], 0
    while f"{name}.{i}" in PAL:
        out.append(PAL[f"{name}.{i}"]); i += 1
    return out


def new(w, h):
    return np.zeros((h, w, 4), np.uint8)


def put(a, x, y, col):
    if 0 <= x < a.shape[1] and 0 <= y < a.shape[0]:
        a[y, x] = col


def over(dst, src, ox, oy):
    """Alpha-composite src onto dst at (ox, oy) in place (clipped)."""
    h, w = src.shape[:2]
    H, W = dst.shape[:2]
    x0, y0, x1, y1 = max(0, ox), max(0, oy), min(W, ox + w), min(H, oy + h)
    if x0 >= x1 or y0 >= y1:
        return
    s = src[y0 - oy:y1 - oy, x0 - ox:x1 - ox].astype(np.float32)
    d = dst[y0:y1, x0:x1].astype(np.float32)
    sa = s[..., 3:4] / 255.0
    da = d[..., 3:4] / 255.0
    oa = sa + da * (1 - sa)
    rgb = (s[..., :3] * sa + d[..., :3] * da * (1 - sa)) / np.maximum(oa, 1e-6)
    out = np.concatenate([rgb, oa * 255], axis=2)
    dst[y0:y1, x0:x1] = np.clip(np.round(out), 0, 255).astype(np.uint8)


def flip(a):
    return a[:, ::-1].copy()


def save(a, path, scale=1):
    from PIL import Image          # review-only helper: the export build itself needs no Pillow
    os.makedirs(os.path.dirname(path), exist_ok=True)
    im = Image.fromarray(a, "RGBA")
    if scale != 1:
        im = im.resize((a.shape[1] * scale, a.shape[0] * scale), Image.NEAREST)
    im.save(path)


def up(a, s):
    return np.repeat(np.repeat(a, s, axis=0), s, axis=1)


def from_rows(rows, keys, w, h, override=None):
    """rows: right-trimmed strings; keys: char -> palette name; override: palette name -> rgba."""
    a = new(w, h)
    override = override or {}
    for y, row in enumerate(rows):
        if len(row) > w:
            raise ValueError(f"row {y} longer than {w}: {row!r}")
        for x, ch in enumerate(row):
            if ch == ".":
                continue
            if ch not in keys:
                raise ValueError(f"row {y} col {x}: unknown key {ch!r}")
            nm = keys[ch]
            a[y, x] = override.get(nm, PAL[nm])
    return a


# ---------- lighting ----------
LIGHT = np.array([-0.52, -0.66, 0.54])
LIGHT = LIGHT / np.linalg.norm(LIGHT)


def level(intensity, cuts):
    """Map intensity (0..1) to ramp index using descending cut values."""
    for i, c in enumerate(cuts):
        if intensity >= c:
            return i
    return len(cuts)


def lit_ellipse(a, cx, cy, rx, ry, rmp, cuts=(0.80, 0.50, 0.22), rot=0.0, mask_fn=None, tex=None):
    """Fill an ellipsoid lit from LIGHT. rot in radians. tex(x, y, u, v, idx) may change the ramp index."""
    h, w = a.shape[:2]
    cr, sr = math.cos(rot), math.sin(rot)
    for y in range(h):
        for x in range(w):
            dx, dy = x + 0.5 - cx, y + 0.5 - cy
            u = (dx * cr + dy * sr) / rx
            v = (-dx * sr + dy * cr) / ry
            r2 = u * u + v * v
            if r2 > 1.0:
                continue
            if mask_fn and not mask_fn(x, y, u, v):
                continue
            nz = math.sqrt(max(0.0, 1 - r2))
            nx, ny = u * cr - v * sr, u * sr + v * cr
            n = np.array([nx, ny, nz * 1.15])
            n = n / np.linalg.norm(n)
            it = float(n @ LIGHT) * 0.5 + 0.5
            idx = min(level(it, cuts), len(rmp) - 1)
            if tex:
                idx = tex(x, y, u, v, idx)
                if idx is None:
                    continue
                idx = max(0, min(len(rmp) - 1, idx))
            a[y, x] = rmp[idx]


def catmull(pts, n=24):
    out = []
    P = [pts[0]] + list(pts) + [pts[-1]]
    for i in range(1, len(P) - 2):
        p0, p1, p2, p3 = (np.array(P[j], float) for j in (i - 1, i, i + 1, i + 2))
        for k in range(n):
            t = k / n
            out.append(0.5 * ((2 * p1) + (-p0 + p2) * t + (2 * p0 - 5 * p1 + 4 * p2 - p3) * t * t
                              + (-p0 + 3 * p1 - 3 * p2 + p3) * t * t * t))
    out.append(np.array(pts[-1], float))
    return np.array(out)


def tube(a, pts, radius_fn, paint, samples=24):
    """Cylinder along a Catmull-Rom curve. radius_fn(t)->r; paint(x, y, t, nx, ny, it) -> rgba or None."""
    curve = catmull(pts, samples)
    seg = np.linalg.norm(np.diff(curve, axis=0), axis=1)
    arclen = np.concatenate([[0], np.cumsum(seg)])
    total = arclen[-1]
    h, w = a.shape[:2]
    for y in range(h):
        for x in range(w):
            p = np.array([x + 0.5, y + 0.5])
            d = np.linalg.norm(curve - p, axis=1)
            i = int(np.argmin(d))
            t = arclen[i] / total
            r = radius_fn(t)
            if d[i] > r:
                continue
            v = (p - curve[i]) / max(r, 1e-6)
            nz = math.sqrt(max(0.0, 1 - min(1.0, float(v @ v))))
            n = np.array([v[0], v[1], nz * 1.1]); n /= np.linalg.norm(n)
            it = float(n @ LIGHT) * 0.5 + 0.5
            col = paint(x, y, t, v[0], v[1], it)
            if col is not None:
                a[y, x] = col
    return total


def line(a, x0, y0, x1, y1, col):
    n = int(max(abs(x1 - x0), abs(y1 - y0))) + 1
    for i in range(n):
        t = i / max(1, n - 1)
        put(a, int(round(x0 + (x1 - x0) * t)), int(round(y0 + (y1 - y0) * t)), col)


def outline(a, col=None, diag=False, lit_col=None):
    """Add a 1px outside outline. lit_col(neighbour_rgba) may return a softer colour for the
    top/left (lit) side; bottom/right always use col."""
    col = col or C("outline.0")
    m = a[..., 3] > 0
    h, w = m.shape
    out = a.copy()
    for y in range(h):
        for x in range(w):
            if m[y, x]:
                continue
            nb = []
            for dx, dy in ((1, 0), (0, 1), (-1, 0), (0, -1)) + (((1, 1), (-1, 1), (1, -1), (-1, -1)) if diag else ()):
                xx, yy = x + dx, y + dy
                if 0 <= xx < w and 0 <= yy < h and m[yy, xx] and a[yy, xx, 3] == 255:
                    nb.append((dx, dy))
            if not nb:
                continue
            c = col
            if lit_col and all(d in ((1, 0), (0, 1), (1, 1)) for d in nb):
                xx, yy = x + nb[0][0], y + nb[0][1]
                c = lit_col(tuple(int(v) for v in a[yy, xx])) or col
            out[y, x] = c
    return out


def bbox(a):
    ys, xs = np.nonzero(a[..., 3])
    if len(ys) == 0:
        return None
    return int(xs.min()), int(ys.min()), int(xs.max()), int(ys.max())


def shift(a, dx, dy):
    out = np.zeros_like(a)
    h, w = a.shape[:2]
    for y in range(h):
        for x in range(w):
            xx, yy = x - dx, y - dy
            if 0 <= xx < w and 0 <= yy < h:
                out[y, x] = a[yy, xx]
    return out


def ground_to(a, foot_row):
    """Shift sprite vertically so its lowest opaque row is foot_row."""
    b = bbox(a)
    return shift(a, 0, foot_row - b[3]) if b else a


def colors(a):
    m = a[..., 3] > 0
    return {tuple(int(v) for v in p) for p in a[m]}


def gray(a):
    g = (a[..., 0] * 0.299 + a[..., 1] * 0.587 + a[..., 2] * 0.114).astype(np.uint8)
    out = a.copy()
    out[..., 0] = out[..., 1] = out[..., 2] = g
    return out


BAYER4 = np.array([[0, 8, 2, 10], [12, 4, 14, 6], [3, 11, 1, 9], [15, 7, 13, 5]]) / 16.0


def shade_ellipse(a, cx, cy, rx, ry, rot, color_fn):
    """Generic lit ellipsoid: color_fn(x, y, u, v, it) -> rgba or None (skip)."""
    h, w = a.shape[:2]
    cr, sr = math.cos(rot), math.sin(rot)
    for y in range(h):
        for x in range(w):
            dx, dy = x + 0.5 - cx, y + 0.5 - cy
            u = (dx * cr + dy * sr) / rx
            v = (-dx * sr + dy * cr) / ry
            r2 = u * u + v * v
            if r2 > 1.0:
                continue
            nz = math.sqrt(max(0.0, 1 - r2))
            nx, ny = u * cr - v * sr, u * sr + v * cr
            n = np.array([nx, ny, nz * 1.15]); n /= np.linalg.norm(n)
            it = float(n @ LIGHT) * 0.5 + 0.5
            c = color_fn(x, y, u, v, it)
            if c is not None:
                a[y, x] = c
