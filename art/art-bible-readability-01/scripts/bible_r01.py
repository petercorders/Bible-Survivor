"""Candidate 1 pixels for the art 03 Bible book and its glow (standard library only, deterministic).

wpn_bible-book_spin_16: hand grid, two 16x16 frames (keys below -> art 03 palette entries, '.' transparent).
fx_bible-glow_static_20: procedural 20x20 halo computed from the union silhouette of both book frames.
Authored by Claude (Anthropic model) in this session as a script; it is not output of the Claude Design product."""
import math

BOOK_KEYS = {"k": "outline.0", "B": "book.0", "b": "book.1", "d": "book.2", "y": "gold.0", "Y": "gold.1",
             "g": "gold.2", "p": "page.0", "P": "page.1", "r": "ribbon.0"}
BOOK = [
    [  # frame 0: front cover lit from the upper left, spine strip with studs on the left, page block right + bottom, ribbon
        "..kkkkkkkkkkk...",
        ".kdBBBBBBBBYkk..",
        ".kgBbbbbbbbbkpk.",
        ".kdBbbbybbbdkPPk",
        ".kdBbbbybbbdkppk",
        ".kgBbyyyyybdkPPk",
        ".kdBbbbybbbdkppk",
        ".kdBbbbybbbdkPPk",
        ".kgBbbbYbbbdkppk",
        ".kdbbbbbbbbdkPPk",
        ".kdbbbbbbbbdkppk",
        ".kdYbbbbbbddkPPk",
        ".kkkkkkkkkkkkppk",
        "..kpppppprppPPPk",
        "...kkkkkkrkkkkkk",
        ".........r......",
    ],
    [  # frame 1: turned, rounded spine towards the viewer with three raised gold bands, page block to the right
        "....kkkkkkk.....",
        "...kdBBBBBdkk...",
        "...kgyyyyygkpk..",
        "...kdBbbbbdkPPk.",
        "...kdBbbbbdkppk.",
        "...kgyyyyygkPPk.",
        "...kdBbbbbdkppk.",
        "...kdBbYbbdkPPk.",
        "...kdBbbbbdkppk.",
        "...kgyyyyygkPPk.",
        "...kdbbbbbdkppk.",
        "...kdbbbbbdkPPk.",
        "...kkkkkkkkkppk.",
        "....kpprpppPPPk.",
        ".....kkrkkkkkkk.",
        ".......r........",
    ],
]
GLOW_KEYS = {"o": "fxa.2", "O": "fxa.0", "*": "spark.0"}


def _hash(x, y):
    """deterministic per-pixel value in [0, 1) (integer hash, no randomness)"""
    h = (x * 374761393 + y * 668265263) & 0xFFFFFFFF
    h = ((h ^ (h >> 13)) * 1274126177) & 0xFFFFFFFF
    return ((h ^ (h >> 16)) & 0xFFFF) / 65536.0


def glow():
    """20x20 static halo behind both book frames: no closed outline and no corners. Warm light rises off the book's
    upper edge and fades down the sides by mid-height (none under the book). Pixel density falls with distance
    (irregular, not a regular dither), so no edge is a solid 1 px line; within 3 px of the book, inside a circle of
    radius 10.8; two motes, none at a corner."""
    n, off = 20, 2                                     # the 16 px book sits at (2, 2) in the 20 px glow (both centre pivots)
    sil = set()
    for fr in BOOK:                                    # union silhouette of both frames in glow coordinates
        for y, row in enumerate(fr):
            for x, c in enumerate(row):
                if c != ".":
                    sil.add((x + off, y + off))
    rows = []
    for y in range(n):
        row = []
        for x in range(n):
            ch = "."
            if (x, y) not in sil:
                d = min(max(abs(x - sx), abs(y - sy)) for sx, sy in sil)          # chessboard distance to the book
                r = math.hypot(x + 0.5 - 10, y + 0.5 - 10)
                th = math.atan2(y + 0.5 - 10, x + 0.5 - 10)
                ang = max(0.0, math.cos(th + math.pi / 2 + 0.3)) ** 1.3             # upper half only, peak slightly left of top
                h = _hash(x, y)
                if r <= 10.8 and ang > 0.0:
                    if d == 1 and h < 0.72 * ang:
                        ch = "O" if h < 0.45 * ang else "o"                        # brighter right at the book's edge
                    elif d == 2 and h < 0.55 * ang:
                        ch = "o"
                    elif d == 3 and h < 0.22 * ang:
                        ch = "o"
            row.append(ch)
        rows.append(row)
    for x, y in ((1, 9), (18, 4)):                     # motes beside the book, never at a corner
        rows[y][x] = "*"
    return ["".join(r) for r in rows]


def sprites():
    """-> {id: (keys, frames)}; frames are lists of row strings."""
    for fr in BOOK:
        assert len(fr) == 16 and all(len(r) == 16 for r in fr)
    return {"fx_bible-glow_static_20": (GLOW_KEYS, [glow()]),
            "wpn_bible-book_spin_16": (BOOK_KEYS, BOOK)}
