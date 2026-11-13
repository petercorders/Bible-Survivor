#!/usr/bin/env python3
"""Pixel comparison of the outline fonts with the art 03 bitmap strikes (art-unity-font-01). Needs Pillow (FreeType).

Usage:  python3 -I -B scripts/render_compare.py [--bitmap-dir DIR]
  --bitmap-dir  folder with art 03 Galmuri*Bitmap*.ttf (default ../art-concept-03/third_party/galmuri)
For every role and every contract sample string (plus 가-힣 every 97th syllable and printable ASCII):
  A  outline font at basePx, no anti-aliasing  ==  the art 03 bitmap font at its strike size   (same design, same pixels)
  B  outline font at 2 x basePx, no anti-aliasing  ==  A scaled x2 nearest-neighbour              (integer scale stays crisp)
Writes  tests/results/render_compare.txt and review/roles_outline_s2.png (each role at UI scale 2, drawn by this script).
Exit    0 all identical, 1 any difference, 3 Pillow or the art 03 bitmap files are missing (step skipped, NOT a pass).
This is FreeType through Pillow on this Mac, NOT Unity: Unity's TextMeshPro / dynamic-font rendering is NOT_RUN."""
import argparse, json, os, sys

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.dirname(HERE)


def main(argv=None):
    ap = argparse.ArgumentParser()
    ap.add_argument("--bitmap-dir", default=os.path.join(os.path.dirname(ROOT), "art-concept-03", "third_party", "galmuri"))
    a = ap.parse_args(argv)
    try:
        from PIL import Image, ImageDraw, ImageFont, ImageChops, __version__ as pil_version
    except ImportError:
        print("SKIP: Pillow is not installed (this step is optional; nothing was compared)")
        return 3
    C = json.load(open(os.path.join(ROOT, "src", "font_contract_v1.json"), encoding="utf-8"))
    strikes = {}
    for role, r in C["roles"].items():
        name, ppem = r["art03Bitmap"].split(" (strike ")
        strikes[role] = (os.path.join(a.bitmap_dir, name), int(ppem.split()[0]))
    if not all(os.path.isfile(p) for p, _ in strikes.values()):
        print(f"SKIP: art 03 bitmap fonts not found in {os.path.basename(a.bitmap_dir)} (nothing was compared)")
        return 3
    cv = C["coverage"]
    texts = list(cv["samples"]) + ["".join(chr(c) for c in range(cv["asciiPrintable"][0], cv["asciiPrintable"][1] + 1)),
                                   "".join(chr(c) for c in range(cv["hangulSyllables"][0], cv["hangulSyllables"][1] + 1, 97)),
                                   "".join(cv["uiSymbols"])]

    def render(font, s):
        w, h = int(font.getlength(s)) + 8, font.size * 3
        im = Image.new("L", (w, h), 0)
        d = ImageDraw.Draw(im)
        d.fontmode = "1"
        d.text((2, font.size), s, font=font, fill=255, anchor="ls")
        return im.crop(im.getbbox()) if im.getbbox() else im

    lines, bad = [], 0
    for role, r in C["roles"].items():
        base = r["basePx"]
        out1 = ImageFont.truetype(os.path.join(ROOT, *r["file"].split("/")), base)
        out2 = ImageFont.truetype(os.path.join(ROOT, *r["file"].split("/")), base * 2)
        bmp = ImageFont.truetype(strikes[role][0], strikes[role][1])
        ctl = ImageFont.truetype(os.path.join(ROOT, *r["file"].split("/")), base + 1)
        same_a = same_b = inked = ctl_diff = 0
        for s in texts:
            ia, ib = render(out1, s), render(bmp, s)
            inked += ia.getbbox() is not None and ib.getbbox() is not None and sum(ia.histogram()[255:]) > 0
            if ia.size == ib.size and ImageChops.difference(ia, ib).getbbox() is None:
                same_a += 1
            i2 = render(out2, s)
            up = ia.resize((ia.width * 2, ia.height * 2), Image.NEAREST)
            if i2.size == up.size and ImageChops.difference(i2, up).getbbox() is None:
                same_b += 1
            ic = render(ctl, s)                  # control: one pixel larger must NOT match the bitmap strike
            ctl_diff += not (ic.size == ib.size and ImageChops.difference(ic, ib).getbbox() is None)
        ok = same_a == same_b == inked == ctl_diff == len(texts)
        bad += not ok
        lines.append(f"{'OK  ' if ok else 'DIFF'}  {role:6s} {os.path.basename(r['file']):20s} "
                     f"A outline@{base} == 03 {os.path.basename(strikes[role][0])}@{strikes[role][1]}: {same_a}/{len(texts)}   "
                     f"B outline@{base * 2} == A x2 nearest: {same_b}/{len(texts)}   "
                     f"inked {inked}/{len(texts)}   control outline@{base + 1} differs: {ctl_diff}/{len(texts)}")
    # proof sheet: each role at UI scale 2 (font size basePx x 2), drawn directly at that size
    rows = [(role, r["uiScalePx"]["2"], cv["samples"][i % len(cv["samples"])]) for i, (role, r) in enumerate(C["roles"].items())]
    W, H = 760, 40 + sum(px * 2 + 16 for _, px, _ in rows)
    sheet = Image.new("RGB", (W, H), (0x1b, 0x14, 0x24))
    d = ImageDraw.Draw(sheet)
    d.fontmode = "1"
    y = 16
    for role, px, s in rows:
        f = ImageFont.truetype(os.path.join(ROOT, *C["roles"][role]["file"].split("/")), px)
        small = ImageFont.truetype(os.path.join(ROOT, *C["roles"]["small"]["file"].split("/")), 16)
        d.text((16, y), f"{role} · {os.path.basename(C['roles'][role]['file'])} · {px}px (s = 2)", font=small, fill=(0x8a, 0x7a, 0x9a))
        d.text((16, y + 20), s, font=f, fill=(0xf2, 0xe9, 0xd8))
        y += px * 2 + 16
    os.makedirs(os.path.join(ROOT, "review"), exist_ok=True)
    sheet.save(os.path.join(ROOT, "review", "roles_outline_s2.png"), optimize=True)
    head = [f"render compare (Pillow {pil_version}, FreeType, anti-aliasing off) - NOT Unity",
            f"{len(texts)} strings per role: {len(cv['samples'])} contract samples, printable ASCII, every 97th Hangul syllable, UI symbols", ""]
    res = "RENDER_COMPARE: " + ("PASS" if not bad else "FAIL")
    os.makedirs(os.path.join(ROOT, "tests", "results"), exist_ok=True)
    with open(os.path.join(ROOT, "tests", "results", "render_compare.txt"), "w", encoding="utf-8") as fh:
        fh.write("\n".join(head + lines + ["", "proof sheet: review/roles_outline_s2.png (script-drawn, not Claude Design)", res]) + "\n")
    print("\n".join(lines + [res]))
    return 0 if not bad else 1


if __name__ == "__main__":
    sys.exit(main())
