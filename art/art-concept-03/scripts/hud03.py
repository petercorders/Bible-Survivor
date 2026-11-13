"""Minimal pixel HUD for art 03 review scenes (carried over from the art-02 module), drawn at native resolution (1 px = 1 game pixel).
Text uses Galmuri bitmap fonts (SIL OFL 1.1, third_party/galmuri) rendered without antialiasing."""
import os
import numpy as np
from PIL import Image, ImageDraw, ImageFont
import px03 as px

FD = os.path.join(px.ROOT, "third_party", "galmuri")
F11B = ImageFont.truetype(os.path.join(FD, "Galmuri11Bitmap-Bold-2.40.4.ttf"), 16)
F11 = ImageFont.truetype(os.path.join(FD, "Galmuri11Bitmap-Regular-2.40.4.ttf"), 16)
F9 = ImageFont.truetype(os.path.join(FD, "Galmuri9Bitmap-Regular-2.40.4.ttf"), 12)
F7 = ImageFont.truetype(os.path.join(FD, "Galmuri7Bitmap-Regular-2.40.4.ttf"), 10)

P = {k: px.C(k) for k in px.PAL if k.split(".")[0] in ("panel", "hp", "xp", "text", "dim")}


def text_mask(s, font):
    l, t, r, b = font.getbbox(s)
    im = Image.new("L", (r + 2, b + 2), 0)
    d = ImageDraw.Draw(im)
    d.fontmode = "1"
    d.text((0, 0), s, font=font, fill=255)
    m = np.array(im) > 127
    ys, xs = np.nonzero(m)
    return m[ys.min():ys.max() + 1, xs.min():xs.max() + 1]


def text(img, s, font, x, y, col=None, outline=True, anchor="l"):
    """Draw pixel text with a 1px dark outline. anchor: l / c / r on x."""
    col = col or P["text.0"]
    m = text_mask(s, font)
    h, w = m.shape
    if anchor == "c":
        x -= w // 2
    elif anchor == "r":
        x -= w
    if outline:
        o = np.zeros((h + 2, w + 2), bool)
        for dy in range(3):
            for dx in range(3):
                o[dy:dy + h, dx:dx + w] |= m
        lay = px.new(w + 2, h + 2)
        lay[o] = P["panel.0"]
        lay[1:1 + h, 1:1 + w][m] = col
        px.over(img, lay, x - 1, y - 1)
    else:
        lay = px.new(w, h)
        lay[m] = col
        px.over(img, lay, x, y)
    return w, h


def frame(img, x, y, w, h, fill=None, bright=True):
    """9-slice style slot frame: dark border, bevel highlight top-left, shade bottom-right."""
    lay = px.new(w, h)
    lay[...] = P["panel.0"]
    lay[1:h - 1, 1:w - 1] = fill if fill is not None else (P["panel.1"] if bright else P["dim.0"])
    if bright:
        lay[1, 1:w - 1] = P["panel.2"]
        lay[1:h - 1, 1] = P["panel.2"]
        lay[h - 2, 2:w - 1] = P["panel.0"]
    for cx, cy in ((0, 0), (w - 1, 0), (0, h - 1), (w - 1, h - 1)):
        lay[cy, cx] = (0, 0, 0, 0)
    px.over(img, lay, x, y)


def bar(img, x, y, w, h, frac, ramp="xp"):
    lay = px.new(w, h)
    lay[...] = P["panel.0"]
    inner_w = w - 2
    fill = int(round(inner_w * frac))
    lay[1:h - 1, 1:1 + inner_w] = P[f"{ramp}.{3 if ramp == 'xp' else 2}"]
    lay[1:h - 1, 1:1 + fill] = P[f"{ramp}.1"]
    lay[1, 1:1 + fill] = P[f"{ramp}.0"]
    px.over(img, lay, x, y)


def slots(img, x, y, icons, pips, size=18, gap=2, n=6, bright=True):
    for i in range(n):
        sx = x + i * (size + gap)
        frame(img, sx, y, size, size, bright=bright)
        if i < len(icons) and icons[i] is not None:
            ic = icons[i]
            px.over(img, ic, sx + (size - ic.shape[1]) // 2, y + (size - ic.shape[0]) // 2 - (1 if pips else 0))
            for p in range(pips[i] if i < len(pips) else 0):
                px.put(img, sx + 3 + p * 2, y + size - 3, P["text.1"])


def hud(img, icons, pips, layout="pc", safe=(0, 0, 0, 0), time="02:15", round_label="1 · 아담", level=7, xp=0.62,
        passive_icons=None):
    """safe = (left, top, right, bottom) insets in native px."""
    H, W = img.shape[:2]
    L, Tp, R, B = safe
    if layout == "portrait":
        bar(img, L, Tp, W - L - R, 6, xp)
        text(img, f"Lv {level}", F11B, W - R - 4, Tp + 9, P["text.1"], anchor="r")
        slots(img, L + 4, Tp + 9, icons, pips, size=16, gap=2)
        slots(img, L + 4, Tp + 27, passive_icons or [], [], size=12, gap=6, bright=False)
        text(img, time, F11B, W // 2 + 20, Tp + 9, anchor="c")
        text(img, round_label, F9, W // 2 + 20, Tp + 23, P["text.2"], anchor="c")
        return
    bar(img, L, Tp, W - L - R, 6, xp)
    text(img, f"Lv {level}", F11B, W - R - 6, Tp + 10, P["text.1"], anchor="r")
    slots(img, L + 6, Tp + 10, icons, pips)
    slots(img, L + 6, Tp + 31, passive_icons or [], [], size=14, gap=6, bright=False)
    text(img, time, F11B, L + (W - L - R) // 2, Tp + 10, anchor="c")
    text(img, round_label, F9, L + (W - L - R) // 2, Tp + 24, P["text.2"], anchor="c")


def hp_bar(img, cx, y, frac=0.8, w=20):
    bar(img, cx - w // 2, y, w, 4, frac, ramp="hp")
