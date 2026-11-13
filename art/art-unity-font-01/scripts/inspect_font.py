#!/usr/bin/env python3
"""Say what kind of font a .ttf really is (standard library only).

Usage:  python3 -I -B scripts/inspect_font.py FONT.ttf [FONT.ttf ...]
Prints per file: SFNT version, glyf bytes, bitmap tables (EBDT/EBLC/CBDT/sbix), and how many of the 11,172 Hangul
syllables and 94 printable ASCII characters map to a glyph WITH an outline. Kind:
  outline      glyf not empty and every Hangul syllable + printable ASCII has an outline
  bitmap-only  glyf empty (or missing) and bitmap strike tables present - NOT usable as an outline font
  partial / unreadable  anything else
Exit 0 only if every file is 'outline'; 1 otherwise; 2 usage error."""
import os, sys

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
import sfnt


def kind(path):
    F = sfnt.Font(open(path, "rb").read())
    cm = F.cmap()
    glen = F.tables.get("glyf", (0, 0))[1]
    bm = [t for t in ("EBDT", "EBLC", "CBDT", "CBLC", "sbix") if t in F.tables]
    def ok(c):
        return c in cm and glen > 0 and F.glyph_contours(cm[c]) != 0
    h = sum(ok(c) for c in range(0xAC00, 0xD7A4))
    a = sum(ok(c) for c in range(0x21, 0x7F))
    k = "outline" if glen > 0 and h == 11172 and a == 94 else ("bitmap-only" if glen == 0 and bm else "partial")
    return k, f"sfnt {F.version.hex()}, glyf {glen} B, bitmap tables {bm or '-'}, outlined Hangul {h}/11172, ASCII {a}/94"


def main(argv):
    if not argv:
        print(__doc__.strip().splitlines()[2])
        return 2
    allok = True
    for p in argv:
        try:
            k, d = kind(p)
        except (OSError, sfnt.SfntError) as e:
            k, d = "unreadable", str(e)
        allok &= k == "outline"
        print(f"{k:12s} {os.path.basename(p)}: {d}")
    return 0 if allok else 1


if __name__ == "__main__":
    sys.exit(main(sys.argv[1:]))
