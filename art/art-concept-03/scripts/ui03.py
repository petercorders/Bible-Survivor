"""UI pieces for art 03: reusable pixel pieces with 9-slice borders and state frames, plus a 9-slice renderer.
Screens (HUD, level-up, boss reward, customisation) are assembled ONLY from these pieces + Galmuri text, so the
mock-ups double as a test that the pieces are enough. Pieces are 1x native pixels (1 px = 1 game pixel)."""
import math
import numpy as np
import px03 as px
import fx03 as fx

K = px.C("outline.0")
P = {k: px.C(k) for k in px.PAL if k.split(".")[0] in ("panel", "hp", "xp", "text", "gold", "dim", "white")}
CLEAR = (0, 0, 0, 0)

PIECES = {}   # id -> dict(frames=[RGBA], states=[...], border=(L,B,R,T) or None, content=(x,y,w,h) or None, note)


def piece(pid, frames, states=None, border=None, content=None, note=""):
    PIECES[pid] = dict(frames=frames if isinstance(frames, list) else [frames], states=states or ["default"],
                       border=border, content=content, note=note)


def _box(w, h, outline, rim_tl, rim_br, fill, round_corners=True):
    a = px.new(w, h)
    a[...] = outline
    a[1:h - 1, 1:w - 1] = fill
    if rim_tl is not None:
        a[1, 1:w - 1] = rim_tl
        a[1:h - 1, 1] = rim_tl
    if rim_br is not None:
        a[h - 2, 2:w - 1] = rim_br
        a[2:h - 1, w - 2] = rim_br
    if round_corners:
        for cx, cy in ((0, 0), (w - 1, 0), (0, h - 1), (w - 1, h - 1)):
            a[cy, cx] = CLEAR
    return a


def build():
    PIECES.clear()
    # panel (dialog background), 16x16, 9-slice border 4 on every side (L,B,R,T in pixels)
    pn = _box(16, 16, P["panel.0"], P["panel.2"], P["panel.0"], P["panel.1"])
    for x, y in ((2, 2), (13, 2), (2, 13), (13, 13)):
        pn[y, x] = P["gold.1"]
    piece("ui_panel_9slice_16", pn, border=(4, 4, 4, 4), note="dialog/panel background; centre and edges tile")

    # card (level-up / reward option), 24x24, 4 states, border 6
    def card(state):
        if state == "normal":
            a = _box(24, 24, P["panel.0"], P["panel.3"], P["panel.0"], P["panel.1"])
        elif state == "focus":
            a = _box(24, 24, P["panel.0"], P["gold.0"], P["gold.2"], P["panel.2"])
            a[2, 2:22] = P["gold.1"]; a[2:22, 2] = P["gold.1"]
            for x, y in ((3, 3), (20, 3), (3, 20), (20, 20)):
                a[y, x] = P["gold.0"]
        elif state == "pressed":
            a = _box(24, 24, P["panel.0"], P["panel.0"], P["panel.2"], P["panel.1"])
            a[2, 2:22] = P["panel.0"]
        else:   # disabled
            a = _box(24, 24, P["panel.0"], P["panel.2"], P["panel.0"], P["panel.0"])
            for x in range(2, 22, 2):
                a[1, x] = P["panel.1"]
        return a
    piece("ui_card_9slice_24", [card(s) for s in ("normal", "focus", "pressed", "disabled")],
          states=["normal", "focus", "pressed", "disabled"], border=(6, 6, 6, 6),
          note="selectable card; content moves down 1 px when pressed; disabled uses text.2 for labels")

    # button, 16x16, 4 states, border 4
    def button(state):
        if state == "normal":
            return _box(16, 16, P["panel.0"], P["panel.3"], P["panel.0"], P["panel.2"])
        if state == "focus":
            a = _box(16, 16, P["panel.0"], P["gold.0"], P["gold.2"], P["panel.2"])
            return a
        if state == "pressed":
            return _box(16, 16, P["panel.0"], P["panel.0"], P["panel.3"], P["panel.1"])
        return _box(16, 16, P["panel.0"], P["panel.1"], P["panel.0"], P["panel.1"])
    piece("ui_button_9slice_16", [button(s) for s in ("normal", "focus", "pressed", "disabled")],
          states=["normal", "focus", "pressed", "disabled"], border=(4, 4, 4, 4), note="text button / selector arrow button")

    # weapon / passive slot, 18x18 (fixed size, not 9-slice), 4 states
    def slot(state):
        if state == "empty":
            return _box(18, 18, P["panel.0"], P["panel.1"], P["panel.0"], P["panel.0"])
        a = _box(18, 18, P["panel.0"], P["panel.2"], P["panel.0"], P["panel.1"])
        if state == "new":
            a[0, 2:16] = P["gold.1"]; a[17, 2:16] = P["gold.1"]; a[2:16, 0] = P["gold.1"]; a[2:16, 17] = P["gold.1"]
        if state == "max":
            a[1, 1:17] = P["gold.0"]; a[1:17, 1] = P["gold.0"]; a[16, 2:17] = P["gold.2"]; a[2:17, 16] = P["gold.2"]
        return a
    piece("ui_slot_18", [slot(s) for s in ("empty", "filled", "new", "max")], states=["empty", "filled", "new", "max"],
          content=(1, 1, 16, 16), note="16x16 icon centred in content rect; pips drawn on row 15")
    pip_off = px.new(2, 2); pip_off[...] = P["panel.2"]
    pip_on = px.new(2, 2); pip_on[...] = P["gold.0"]; pip_on[1, :] = P["gold.1"]
    piece("ui_pip_2", [pip_off, pip_on], states=["off", "on"], note="weapon level pip, 1 px gap")

    # bar frame (9-slice) + tileable back/fill strips (1 px wide columns, repeat horizontally)
    bf = _box(8, 6, P["panel.0"], None, None, CLEAR, round_corners=False)
    piece("ui_bar-frame_9slice_8x6", bf, border=(1, 1, 1, 1), content=(1, 1, 6, 4),
          note="outline only; centre is transparent, fill and back are drawn inside the content rect")
    def column(colors):
        a = px.new(1, len(colors))
        for i, c in enumerate(colors):
            a[i, 0] = c
        return a
    piece("ui_bar-xp_tile_1x4", [column([P["xp.3"]] * 4), column([P["xp.0"], P["xp.1"], P["xp.1"], P["xp.2"]])],
          states=["back", "fill"], note="repeat horizontally; fill width = round(content_w * value)")
    piece("ui_bar-hp_tile_1x2", [column([P["hp.2"]] * 2), column([P["hp.0"], P["hp.1"]])], states=["back", "fill"],
          note="player HP under the feet; repeat horizontally")

    # colour swatch frame 12x12 (centre 8x8 filled by code with ramp index 1 of the option), 2 states
    def swatch(sel):
        a = px.new(12, 12)
        a[...] = P["gold.0"] if sel else P["panel.0"]
        a[1:11, 1:11] = P["panel.0"]
        a[2:10, 2:10] = CLEAR
        if sel:
            a[0, 0] = a[0, 11] = a[11, 0] = a[11, 11] = CLEAR
        return a
    piece("ui_swatch_12", [swatch(False), swatch(True)], states=["normal", "selected"], content=(2, 2, 8, 8),
          note="content rect is filled with the option colour (ramp index 1) at runtime")

    arrow = px.from_rows(["k.....", "kk....", "kak...", "kaak..", "kaaak.", "kaak..", "kak...", "kk....", "k....."],
                         {"k": "outline.0", "a": "text.1"}, 6, 9)
    piece("ui_arrow_6x9", arrow, note="points right; flipX for left")
    badge = px.from_rows(["...kk...", "..kyyk..", ".kyyyyk.", "kyyyyyyk", "kkkyykkk", "..kyyk..", "..kyyk..", "..kkkk.."],
                         {"k": "outline.0", "y": "gold.0"}, 8, 8)
    piece("ui_badge-evolve_8", badge, note="evolution marker on the top-right of an icon")
    return PIECES


# ---------------------------------------------------------------- icons (16x16)
def _icon_scroll():
    a = px.new(16, 16)
    pg, gd = px.ramp("page"), px.ramp("gold")
    a[4:12, 3:13] = pg[0]
    a[11, 3:13] = pg[1]
    for x in (3, 12):
        a[3:13, x] = gd[1]
    a[3, 3] = a[12, 3] = a[3, 12] = a[12, 12] = gd[2]
    for y in (6, 8, 10):
        a[y, 5:11] = pg[1]
    a[6, 9:11] = px.C("book.1")
    return px.outline(a)


def _icon_hair():
    a = px.new(16, 16)
    hr = px.ramp("hair")
    for i in range(11):
        x = 6 + int(round(1.6 * math.sin(i * 0.9)))
        y = 2 + i
        a[y, x:x + 3] = hr[0] if i % 2 == 0 else hr[1]
        a[y, x + 2] = hr[2]
    a[13, 6:10] = px.C("ribbon.0")
    return px.outline(a)


def _icon_stones():
    a = px.new(16, 16)
    st = px.ramp("stone")
    for cx, cy, r in ((5.0, 11.0, 2.4), (10.5, 11.5, 2.6), (8.0, 7.5, 2.2), (4.5, 6.0, 1.8), (11.5, 6.0, 1.9)):
        px.lit_ellipse(a, cx, cy, r, r * 0.85, st, cuts=(0.8, 0.55, 0.3))
    return px.outline(a)


def _icon_gold():
    a = px.new(16, 16)
    px.lit_ellipse(a, 8.0, 8.0, 5.5, 5.5, px.ramp("gold"), cuts=(0.78, 0.5, 0.2))
    for y in range(5, 11):
        a[y, 7] = px.C("gold.2")
    a[5, 7:10] = px.C("gold.2")
    return px.outline(a)


def icons():
    """stable icon ids -> 16x16 RGBA. Weapon icons reuse the in-game objects."""
    out = {
        "ico_wpn_bible_static_16": fx.book(0),
        "ico_wpn_jawbone_static_16": fx.jawbone(),
        "ico_wpn_sling_static_16": fx.sling_icon(),
        "ico_psv_scroll_static_16": _icon_scroll(),
        "ico_psv_hair_static_16": _icon_hair(),
        "ico_psv_stones_static_16": _icon_stones(),
        "ico_res_gold_static_16": _icon_gold(),
    }
    return out


# ---------------------------------------------------------------- renderer
def nine(dst, pid, x, y, w, h, state=0):
    """Draw piece `pid` (state index) as a 9-slice into dst at (x,y) with size (w,h). Edges and centre repeat."""
    pc = PIECES[pid]
    src = pc["frames"][state]
    L, B, R, T = pc["border"]
    sh, sw = src.shape[:2]
    assert w >= L + R and h >= T + B, (pid, w, h)
    out = px.new(w, h)
    cw, ch = sw - L - R, sh - T - B
    xs = [(0, 0, L)] + [(L + i, L + (i % cw), 1) for i in range(w - L - R)] + [(w - R, sw - R, R)]
    ys = [(0, 0, T)] + [(T + j, T + (j % ch), 1) for j in range(h - T - B)] + [(h - B, sh - B, B)]
    for dx, sx, ww in xs:
        for dy, sy, hh in ys:
            if ww and hh:
                out[dy:dy + hh, dx:dx + ww] = src[sy:sy + hh, sx:sx + ww]
    px.over(dst, out, x, y)


def stamp(dst, pid, x, y, state=0, flip=False):
    a = PIECES[pid]["frames"][state]
    px.over(dst, a[:, ::-1] if flip else a, x, y)


def bar(dst, x, y, w, value, kind="xp"):
    """frame 9-slice + tiled back + tiled fill; kind xp (h=6) or hp (h=4)."""
    h = 6 if kind == "xp" else 4
    tile = "ui_bar-xp_tile_1x4" if kind == "xp" else "ui_bar-hp_tile_1x2"
    nine(dst, "ui_bar-frame_9slice_8x6", x, y, w, h)
    inner = w - 2
    back, fill = PIECES[tile]["frames"]
    for i in range(inner):
        col = fill if i < int(round(inner * value)) else back
        px.over(dst, col, x + 1 + i, y + 1)


def slot(dst, x, y, icon=None, state=1, pips=0, max_pips=0):
    stamp(dst, "ui_slot_18", x, y, state)
    if icon is not None:
        px.over(dst, icon, x + 1 + (16 - icon.shape[1]) // 2, y + 1 + (16 - icon.shape[0]) // 2 - (1 if max_pips else 0))
    for p in range(max_pips):
        stamp(dst, "ui_pip_2", x + 2 + p * 2 if max_pips > 4 else x + 3 + p * 3, y + 14, 1 if p < pips else 0)
