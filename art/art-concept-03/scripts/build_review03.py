"""Review material for art 03 (not handoff input).

Usage:  python3 -I scripts/build_review03.py          (run build_export03.py first)
Reads   export/ (PNG strips + manifest; frames are taken from the exported PNGs, so what is reviewed is what is handed off),
        src/, third_party/galmuri (labels use Galmuri, no system fonts),
        ../art-concept-02/out/views/view_pc_native.png and ../art-concept-02/src (read-only, only for the 02 -> 03 comparison)
Writes  review/boards/*.png, review/views/*.png, review/mock/*.png, review/anim/*.gif|*_apng.png, review/index.html,
        review/metrics/*.json
Needs   Python 3, NumPy, Pillow (GIF/APNG and font rasterising)."""
import base64, io, json, math, os, random, sys
HERE = os.path.dirname(os.path.abspath(__file__))
sys.path.insert(0, HERE)
import numpy as np
from PIL import Image
import px03 as px, player03 as pl, terrain03 as tr, tiles03 as tl, ui03 as ui, hud03 as hud, metrics03 as mt, pngio

ROOT = px.ROOT
EXP = os.path.join(ROOT, "export")
RV = os.path.join(ROOT, "review")
for d in ("boards", "views", "mock", "anim", "metrics"):
    os.makedirs(os.path.join(RV, d), exist_ok=True)
MAN = json.load(open(os.path.join(EXP, "manifest_v3.json"), encoding="utf-8"))
A = {a["id"]: a for a in MAN["assets"]}
BG = (24, 20, 31, 255)
F11B, F11, F9, F7 = hud.F11B, hud.F11, hud.F9, hud.F7
REC = px.PALJ["recolor"]


def frames_of(aid):
    a = A[aid]
    w, h, buf = pngio.read_rgba(os.path.join(ROOT, a["file"]))
    arr = np.frombuffer(buf, np.uint8).reshape(h, w, 4)
    cw = a["cell"][0]
    return [arr[:, i * cw:(i + 1) * cw].copy() for i in range(a["frames"])]


# UI mock-ups are drawn from the HANDOFF data: exported piece PNG frames + borders from export/ui/ui-pieces_v3.json
ui.build()
_UID = json.load(open(os.path.join(EXP, "ui", "ui-pieces_v3.json"), encoding="utf-8"))["pieces"]
for _pid, _pc in _UID.items():
    ui.PIECES[_pid]["frames"] = frames_of(_pid)
    ui.PIECES[_pid]["border"] = tuple(_pc["border_LBRT"]) if _pc["border_LBRT"] else None


def board(w, h):
    b = px.new(w, h); b[...] = BG
    return b


def label(img, s, x, y, font=None, col=None, anchor="l"):
    return hud.text(img, s, font or F9, x, y, col or px.C("text.0"), outline=False, anchor=anchor)


def on_bg(a, key="grass.2"):
    b = px.new(a.shape[1], a.shape[0]); b[...] = px.C(key) if isinstance(key, str) else key
    px.over(b, a, 0, 0)
    return b


def save(a, *parts):
    p = os.path.join(RV, *parts)
    Image.fromarray(a, "RGBA").save(p, optimize=True)
    return p


# ---------------------------------------------------------------- player recolour from exported layer frames
KEYMAP = {}
for ch, nm in pl.RIG["keys"].items():
    KEYMAP[px.C(nm)] = nm


def recolour(fr, skin=None, hair=None, tunic=None, cloak=None):
    ov = pl.override(skin=skin, hair=hair, tunic=tunic, cloak=cloak)
    if not ov:
        return fr
    out = fr.copy()
    m = fr[..., 3] > 0
    ys, xs = np.nonzero(m)
    for y, x in zip(ys, xs):
        nm = KEYMAP.get(tuple(int(v) for v in fr[y, x]))
        if nm in ov:
            out[y, x] = ov[nm]
    return out


LAYER_FRAMES = {(ln, an): frames_of(f"chr_player_layer-{MAN_name}_{an}_32")
                for ln, MAN_name in json.load(open(os.path.join(ROOT, "src", "contract_v3.json")))["player"]["fileLayerNames"].items()
                for an in ("idle", "walk", "death")}


def player(anim, variant, **rec):
    lays = pl.RIG["variants"][variant]
    out = []
    for i in range(len(LAYER_FRAMES[("base", anim)])):
        f = px.new(32, 32)
        for ln in pl.RIG["layer_order"]:
            if ln in lays:
                px.over(f, recolour(LAYER_FRAMES[(ln, anim)][i], **rec), 0, 0)
        out.append(f)
    return out


# ---------------------------------------------------------------- scene
SCENE_PATH = ["." * 25 for _ in range(25)]
def _cells():
    g = [list(r) for r in SCENE_PATH]
    for x in range(0, 13):
        g[13][x] = "#"
    for y in range(13, 17):
        g[y][12] = "#"
    for x in range(12, 25):
        g[16][x] = "#"
    for y in range(6, 13):
        g[y][7] = "#"
    for x in range(17, 21):
        g[9][x] = "#"
    g[10][20] = "#"; g[11][20] = "#"
    return ["".join(r) for r in g]
SCENE_PATH = _cells()
PT = [frames_of("til_eden_path-16mask_static_32")[m] for m in range(16)]
_, PMASK = tl.build()


def layout01():
    """identical to 01/02 (seed 7): 40 enemies inside the PC camera + 16 outside, 34 gems."""
    rng = random.Random(7)
    for _ in range((800 // 32) ** 2):
        rng.random()
    P = (400, 400)
    placed = []

    def pick(region, avoid_rect=None, min_r=56, min_d=18):
        for _ in range(2000):
            x = rng.randint(region[0], region[2]); y = rng.randint(region[1], region[3])
            if math.hypot(x - P[0], y - P[1]) < min_r:
                continue
            if avoid_rect and avoid_rect[0] <= x <= avoid_rect[2] and avoid_rect[1] <= y <= avoid_rect[3]:
                continue
            if any(math.hypot(x - a, y - b) < min_d for a, b in placed):
                continue
            placed.append((x, y)); return x, y
        raise SystemExit("placement failed")

    PC = (100, 250, 700, 574); OUTER = (14, 14, 786, 786); PCR = (80, 220, 720, 580)
    order = [("a1", PC, None)] * 24 + [("a2", PC, None)] * 12 + [("a3", PC, None)] * 4 + \
            [("a1", OUTER, PCR)] * 10 + [("a2", OUTER, PCR)] * 4 + [("a3", OUTER, PCR)] * 2
    ents = [[n, *pick(reg, av)] for n, reg, av in order]
    gems = [("base", *pick(PC, None, 26, 10)) for _ in range(22)] + [("bonus", *pick(PC, None, 26, 10)) for _ in range(4)] + \
           [("base", *pick(OUTER, PCR, 26, 10)) for _ in range(8)]
    return P, ents, gems


EN = {"a1": frames_of("enm_a1-small-snake_move_32"), "a2": frames_of("enm_a2-fruit-moth_fly_32"), "a3": frames_of("enm_a3-thorn-beast_walk_32")}
SHADOW = {"a1": frames_of("fx_shadow-snake_static_20x4")[0], "a2": frames_of("fx_shadow-small_static_12x4")[0],
          "a3": frames_of("fx_shadow-wide_static_24x6")[0], "player": frames_of("fx_shadow_static_16x6")[0]}
GEM = {"base": frames_of("pkp_xp-gem-base_idle_8"), "bonus": frames_of("pkp_xp-gem-bonus_idle_8")}
BOOK = frames_of("wpn_bible-book_spin_16"); GLOW = frames_of("fx_bible-glow_static_20")[0]
JAW = frames_of("wpn_jawbone_held_16")[0]
EDGE, TRAIL = frames_of("fx_jawbone-swing-edge_hit_64"), frames_of("fx_jawbone-swing-trail_hit_64")
STONE = frames_of("wpn_sling-stone_spin_8"); STRAIL = frames_of("fx_sling-trail_static_20x8")[0]
HIT = frames_of("fx_hit-spark_hit_16"); DEATH = frames_of("fx_enemy-death_burst_32")
CLOSE = [("a1", 432, 392), ("a1", 438, 412), ("a2", 424, 372), ("a3", 446, 430), ("a1", 368, 418), ("a2", 372, 366),
         ("a1", 412, 434), ("a3", 330, 452)]


def scene(fx_level="full"):
    G, shade, placed, meadow, path = tr.world(path_cells=SCENE_PATH, path_tiles=PT, path_masks=PMASK)
    img = tr.apply_shade(G, shade)
    P, ents, gems = layout01()
    # close quarters: move the 8 enemies nearest to the player into contact range (count in the PC view stays 40)
    inview = [e for e in ents if 80 <= e[1] < 720 and 220 <= e[2] < 580]
    inview.sort(key=lambda e: math.hypot(e[1] - P[0], e[2] - P[1]))
    for e, (k, x, y) in zip(inview[:8], CLOSE):
        e[0], e[1], e[2] = k, x, y
    # one A3 in canopy shade at the left edge of the view
    a3s = [e for e in inview[8:] if e[0] == "a3"]
    if a3s:
        a3s[0][1], a3s[0][2] = 128, 300
    rng = random.Random(77)
    dying = (462, 414)                             # a 41st enemy that was just killed: only its death burst is drawn
    sprites = []
    for k, x, y in ents:
        fr = EN[k][rng.randrange(4)]
        if x > P[0]:
            fr = fr[:, ::-1]                       # enemies face the player
        sprites.append((y, "enemy", k, x, y, fr))
    pfr = player("walk", "b-cloak-beard")[1]
    sprites.append((P[1], "player", "player", P[0], P[1], pfr))
    for k, x, y in gems:
        px.over(img, GEM[k][rng.randrange(2)], x - 4, y - 4)
    for g, (x, y) in zip(("base", "base", "bonus", "base"), ((392, 420), (418, 446), (360, 398), (456, 402))):
        px.over(img, GEM[g][0], x - 4, y - 4)
    for _, kind, k, x, y, fr in sprites:
        sh = SHADOW[k]
        px.over(img, sh, x - sh.shape[1] // 2, y - sh.shape[0] // 2 - (8 if k == "a2" else 0) + (0 if k == "a2" else -1) + (6 if k == "a2" else 0))
    for _, kind, k, x, y, fr in sorted(sprites, key=lambda s: s[0]):
        px.over(img, fr, x - 16, y - 30)
    # weapon FX layer
    cx, cy = P[0], P[1] - 14                       # body centre
    if fx_level == "full":
        px.over(img, TRAIL[2], cx - 32, cy - 32)
    px.over(img, EDGE[2], cx - 32, cy - 32)
    ax, ay = A["fx_jawbone-swing-edge_hit_64"]["attachPx"][2]
    px.over(img, JAW, cx - 32 + ax - 8, cy - 32 + ay - 8)
    for i, ang in enumerate((200, 320, 80)):
        bx = cx + 42 * math.cos(math.radians(ang)); by = cy + 30 * math.sin(math.radians(ang))
        if fx_level == "full":
            px.over(img, GLOW, int(bx) - 10, int(by) - 10)
        px.over(img, BOOK[i % 2], int(bx) - 8, int(by) - 8)
    for (sx, sy), (tx, ty) in (((470, 352), (506, 330)), ((330, 380), (296, 372))):
        if fx_level == "full":
            tr_ = STRAIL if tx > sx else STRAIL[:, ::-1]
            px.over(img, tr_, sx - (19 if tx > sx else 1), sy - 4)
        px.over(img, STONE[0], sx - 4, sy - 4)
    for (x, y), f in (((432, 378), 1), ((438, 398), 0), ((424, 358), 2)):
        px.over(img, HIT[f], x - 8, y - 8)
    px.over(img, DEATH[1], dying[0] - 16, dying[1] - 30)
    return img, P, ents, shade, path


def hud_layer(W, H, safe=(0, 0, 0, 0), portrait=False):
    lay = px.new(W, H)
    L, T, R, B = safe
    ui.bar(lay, L, T, W - L - R, 0.62, "xp")
    icons = [A and frames_of(f"ico_wpn_{w}_static_16")[0] for w in ("jawbone", "bible", "sling")]
    psv = [frames_of(f"ico_psv_{p}_static_16")[0] for p in ("hair", "scroll")]
    size = 18
    for i in range(6):
        ui.slot(lay, L + 6 + i * (size + 2), T + 9, icons[i] if i < 3 else None, 1 if i < 3 else 0, [3, 2, 1][i] if i < 3 else 0, 8 if i < 3 else 0)
    for i in range(6):
        ui.slot(lay, L + 6 + i * (size + 2), T + 9 + size + 2, psv[i] if i < 2 else None, 1 if i < 2 else 0)
    if portrait:
        hud.text(lay, "Lv 7", F11B, W - R - 4, T + 9, px.C("text.1"), anchor="r")
        hud.text(lay, "02:15", F11B, W - R - 4, T + 24, anchor="r")
        hud.text(lay, "1 · 아담", F9, W - R - 4, T + 39, px.C("text.2"), anchor="r")
    else:
        hud.text(lay, "Lv 7", F11B, W - R - 6, T + 10, px.C("text.1"), anchor="r")
        hud.text(lay, "02:15", F11B, L + (W - L - R) // 2, T + 10, anchor="c")
        hud.text(lay, "1 · 아담", F9, L + (W - L - R) // 2, T + 24, px.C("text.2"), anchor="c")
    return lay


def view(img, P, rect, safe=(0, 0, 0, 0), portrait=False):
    x0, y0, x1, y1 = rect
    v = img[y0:y1, x0:x1].copy()
    ui.bar(v, P[0] - x0 - 10, P[1] - y0 + 3, 20, 0.8, "hp")
    px.over(v, hud_layer(x1 - x0, y1 - y0, safe, portrait), 0, 0)
    return v


VIEWS = {"pc": ((80, 220, 720, 580), (0, 0, 0, 0), False),
         "phone-l": ((10, 220, 790, 580), (48, 0, 48, 21), False),
         "phone-p": ((220, 10, 580, 790), (3, 47, 3, 34), True)}


def dashed(img, x0, y0, x1, y1, col, step=4):
    for x in range(x0, x1):
        if (x // step) % 2 == 0:
            px.put(img, x, y0, col); px.put(img, x, y1 - 1, col)
    for y in range(y0, y1):
        if (y // step) % 2 == 0:
            px.put(img, x0, y, col); px.put(img, x1 - 1, y, col)


def build_scene():
    out = {}
    for lvl in ("full", "low"):
        img, P, ents, shade, path = scene(lvl)
        out[lvl] = (img, P)
        if lvl == "full":
            alive = [e for e in ents if 80 <= e[1] < 720 and 220 <= e[2] < 580]
            near = sum(1 for e in alive if math.hypot(e[1] - P[0], e[2] - P[1]) < 64)
        for name, (rect, safe, portrait) in VIEWS.items():
            v = view(img, P, rect, safe, portrait)
            if name != "pc":
                L, T, R, B = safe
                dashed(v, L, T, v.shape[1] - R, v.shape[0] - B, (255, 255, 255, 170))
            suffix = "" if lvl == "full" else "_fx-low"
            save(px.up(v, 2), "views", f"view_{name}{suffix}_x2.png")
            if name == "pc":
                save(v, "views", f"view_pc{suffix}_native.png")
                save(px.up(px.gray(v), 2), "views", f"view_pc{suffix}_gray_x2.png")
    img, P = out["full"]
    inview = {"alive_in_pc_view": len(alive), "by_kind": {k: sum(1 for e in alive if e[0] == k) for k in ("a1", "a2", "a3")},
              "within_64px_of_player": near, "outside_view": len(ents) - len(alive), "death_bursts": 1}
    json.dump(inview, open(os.path.join(RV, "metrics", "scene_counts.json"), "w", encoding="utf-8"), indent=1)
    zoom = img[P[1] - 70:P[1] + 50, P[0] - 110:P[0] + 110]
    lowz = out["low"][0][P[1] - 70:P[1] + 50, P[0] - 110:P[0] + 110]
    b = board(220 * 4 * 2 + 30, 120 * 4 * 2 + 70)
    label(b, "가까운 전투 ×4 — FX 기본 (위 왼쪽) · FX 낮춤 (위 오른쪽) · 흑백 (아래)", 10, 8, F11)
    px.over(b, px.up(zoom, 4), 10, 30); px.over(b, px.up(lowz, 4), 20 + 880, 30)
    px.over(b, px.up(px.gray(zoom), 4), 10, 40 + 480); px.over(b, px.up(px.gray(lowz), 4), 20 + 880, 40 + 480)
    save(b, "boards", "board_close-quarters_x4.png")
    return out, inview


# ---------------------------------------------------------------- boards
def grid_board(rows, scale, title, cell_gap=6, bgkey="grass.2", row_labels=None, label_w=150):
    """rows: list of lists of RGBA frames; each row is as tall as its tallest frame."""
    hs = [max(max(f.shape[0] for f in r) * scale, 16) for r in rows]
    fw = max(sum(f.shape[1] * scale + cell_gap for f in r) for r in rows)
    W = max(label_w + fw + 20, hud.text_mask(title, F11).shape[1] + 30)
    H = 36 + sum(h + cell_gap + 4 for h in hs) + 10
    b = board(W, H)
    label(b, title, 10, 10, F11)
    y = 36
    for ri, r in enumerate(rows):
        if row_labels:
            label(b, row_labels[ri], 10, y + 4, F9)
        x = label_w
        for f in r:
            px.over(b, px.up(on_bg(f, bgkey) if bgkey else f, scale), x, y)
            x += f.shape[1] * scale + cell_gap
        y += hs[ri] + cell_gap + 4
    return b


COMBOS = [("s6", "black", "linen", "indigo", "b-cloak-beard", "가장 어두운 피부 × 머리 6색 (경계 위험 최대, 1/6)"),
          ("s6", "darkbrown", "madder", "olive", "b-cloak-beard", "s6 × darkbrown: 02에서 약했던 조합"),
          ("s6", "brown", "ochre", "purple", "b-cloak-beard", "s6 × brown: 02에서 가장 약했던 조합"),
          ("s6", "auburn", "olive", "madder", "a-tunic", "s6 × auburn: 02 약함 · 수염 없음 얼굴"),
          ("s6", "gray", "indigo", "ochre", "a-tunic", "s6 × gray: 밝은 머리 대비"),
          ("s6", "white", "purple", "linen", "a-tunic", "s6 × white: 가장 밝은 머리 (6/6)"),
          ("s6", "brown", "linen", "indigo", "c-hood", "두건이 머리를 가림 · 어두운 피부"),
          ("s6", "darkbrown", "indigo", "madder", "d-hood-cloak-beard", "가림 최대 (두건+망토+수염)"),
          ("s1", "black", "madder", "indigo", "b-cloak-beard", "가장 밝은 피부 × 가장 어두운 머리 (반대 극단)"),
          ("s1", "white", "olive", "purple", "a-tunic", "밝은 피부 × 흰 머리 (낮은 명도 차)"),
          ("s3", "brown", "linen", "indigo", "b-cloak-beard", "중간 피부 · 기본 색"),
          ("s5", "darkbrown", "ochre", "olive", "d-hood-cloak-beard", "어두운 중간 피부 · 가림 최대")]


def combo_frames(sk, hr, tu, cl, v):
    return sum((player(an, v, skin=sk, hair=hr, tunic=tu, cloak=cl) for an in ("idle", "walk", "death")), [])


def _shade_rgba(key="grass.2"):
    return tuple(int(c) for c in tr.apply_shade(np.full((1, 1, 4), px.C(key), np.uint8), np.ones((1, 1), bool))[0, 0])


BGS = [("잔디", px.C("grass.2")), ("흙길", px.C("path.1")), ("그늘", _shade_rgba())]


def combo_panel(bc, g, scale):
    """12 rows x 14 frames on one flat background, 2 px gap, then optional grayscale and integer zoom."""
    pan = px.new(14 * 34, 12 * 34); pan[...] = bc
    for r, (sk, hr, tu, cl, v, _) in enumerate(COMBOS):
        for i, f in enumerate(combo_frames(sk, hr, tu, cl, v)):
            px.over(pan, f, i * 34 + 1, r * 34 + 1)
    if g:
        pan = px.gray(pan)
    return px.up(pan, scale) if scale > 1 else pan


def build_player_boards():
    order = pl.RIG["layer_order"]
    rows, labs = [], []
    for an in ("idle", "walk", "death"):
        for ln in order:
            rows.append(LAYER_FRAMES[(ln, an)]); labs.append(f"{an} · {ln}")
    save(grid_board(rows, 3, "플레이어 레이어 6종 × idle4 / walk4 / death6 (실제 export PNG 프레임, ×3, 같은 셀 · 피벗 (16,1))",
                    row_labels=labs, bgkey=(60, 84, 50, 255)), "boards", "board_player_layers_x3.png")
    rows, labs = [], []
    for v in ("a-tunic", "b-cloak-beard", "c-hood", "d-hood-cloak-beard"):
        for an in ("idle", "walk", "death"):
            fr = player(an, v)
            rows.append(fr + [f[:, ::-1] for f in fr]); labs.append(f"{v} {an} 오른쪽 | flipX 왼쪽")
    save(grid_board(rows, 3, "레이어 합성 4종 × 3동작 — 오른쪽 원본 뒤에 같은 프레임의 flipX", row_labels=labs, label_w=230),
         "boards", "board_player_variants_x3.png")
    # 12 representative combinations: an exact, reasoned sample (NOT the full product). The full skin x hair product
    # (36 x 14 frames) is covered separately by the boundary metric + board_skin-hair-36.
    combos = COMBOS
    json.dump({"count": len(combos), "scope": "representative sample, not all combinations",
               "coverage": {"skins": sorted({c[0] for c in combos}), "hairs": sorted({c[1] for c in combos}),
                            "tunic_dyes": sorted({c[2] for c in combos}), "cloak_dyes": sorted({c[3] for c in combos}),
                            "variants": sorted({c[4] for c in combos})},
               "combos": [{"no": i + 1, "skin": c[0], "hair": c[1], "tunic": c[2], "cloak": c[3], "variant": c[4], "why": c[5]}
                          for i, c in enumerate(combos)],
               "full_product_checked_elsewhere": "skin6 x hair6 = 36 combinations x 14 frames: metrics/hair_boundary_36.json"},
              open(os.path.join(RV, "metrics", "combos_12.json"), "w", encoding="utf-8"), ensure_ascii=False, indent=1)
    rows, labs = [], []
    for i, (sk, hr, tu, cl, v, why) in enumerate(combos):
        rows.append(combo_frames(sk, hr, tu, cl, v)); labs.append(f"{i + 1}. {sk} {hr} / {tu} {cl} / {v}")
    b = grid_board(rows, 3, f"대표 조합 {len(combos)}개 (전체 조합 아님) × 14프레임 idle4 · walk4 · death6, 잔디 ×3 — 고른 이유는 오른쪽",
                   row_labels=labs, label_w=320)
    nb = board(b.shape[1] + 330, b.shape[0]); px.over(nb, b, 0, 0)
    for i, c in enumerate(combos):
        label(nb, c[5], b.shape[1] + 6, 36 + i * (32 * 3 + 10) + 4, F9, px.C("text.2"))
    save(nb, "boards", "board_player_combos-12_x3.png")
    # the same 12 x 14 frames on grass / path / shade, colour and grayscale, at 1x and at x2
    for scale in (1, 2):
        pans = [(f"{bn}{' 흑백' if g else ''}", combo_panel(bc, g, scale)) for g in (False, True) for bn, bc in BGS]
        pw, ph = pans[0][1].shape[1], pans[0][1].shape[0]
        cols = 3 if scale == 1 else 2
        nrow = (len(pans) + cols - 1) // cols
        b = board(cols * (pw + 34) + 20, 40 + nrow * (ph + 26))
        label(b, f"대표 조합 12개 × 14프레임 — 잔디 · 흙길 · 그늘, 컬러와 흑백, {'1배 (원래 크기)' if scale == 1 else '×2'}. 줄 번호 = combos_12.json no", 10, 8, F11 if scale == 2 else F9)
        for k, (nm, pan) in enumerate(pans):
            x = 30 + (k % cols) * (pw + 34); y = 34 + (k // cols) * (ph + 26)
            label(b, nm, x, y, F9)
            for r in range(12):
                label(b, str(r + 1), x - 22, y + 16 + r * 34 * scale + 10 * scale, F7, px.C("text.2"))
            px.over(b, pan, x, y + 16)
        save(b, "boards", f"board_player_combos-12_backgrounds_{'1x' if scale == 1 else 'x2'}.png")
    # full 36 skin x hair head check + measured boundary values (all 14 frames)
    res = hair_metrics()
    b = board(6 * 120 + 150, 6 * 98 + 80)
    label(b, "피부 6 × 머리 6 = 36조합 전체 (머리 부분 ×4, idle f0). 숫자 = 14프레임 중 가장 약한 프레임의 '약한 경계 비율' 02 → 03", 10, 8, F9)
    for i, sk in enumerate(REC["skin"]):
        label(b, sk, 10, 40 + i * 98 + 30, F11)
        for j, hr in enumerate(REC["hair"]):
            f = player("idle", "a-tunic", skin=sk, hair=hr)[0][2:22, 8:28]
            px.over(b, px.up(on_bg(f), 4), 60 + j * 120, 34 + i * 98)
            r = res[f"{sk}/{hr}"]
            col = px.C("hp.0") if r["v02"]["weak_share"] > 0.10 else px.C("text.2")
            label(b, f"{r['v02']['weak_share']:.2f}→{r['v03']['weak_share']:.2f}", 60 + j * 120, 34 + i * 98 + 82, F7, col)
    for j, hr in enumerate(REC["hair"]):
        label(b, hr, 60 + j * 120, 24, F7)
    save(b, "boards", "board_skin-hair-36_x4.png")


def hair_metrics():
    src02 = json.load(open(os.path.join(os.path.dirname(ROOT), "art-concept-02", "src", "player_layers_32.json"), encoding="utf-8"))
    res = {}
    for sk in REC["skin"]:
        for hr in REC["hair"]:
            skc = {px.hex_rgba(c) for c in REC["skin"][sk]}
            ov = pl.override(skin=sk, hair=hr)

            def meas(base, hl):
                img = px.new(32, 32); px.over(img, base, 0, 0); px.over(img, hl, 0, 0)
                hm = hl[..., 3] > 0
                sm = (hl[..., 3] == 0) & (base[..., 3] > 0) & np.array([[tuple(p) in skc for p in row] for row in base])
                return mt.hair_skin_boundary(img, hm, sm)
            r02 = meas(px.from_rows(src02["layers"]["base"], src02["keys"], 32, 32, ov),
                       px.from_rows(src02["layers"]["hair_short"], src02["keys"], 32, 32, ov))
            rs = []
            for an, n in (("idle", 4), ("walk", 4), ("death", 6)):
                for i in range(n):
                    rs.append(meas(recolour(LAYER_FRAMES[("base", an)][i], skin=sk, hair=hr),
                                   recolour(LAYER_FRAMES[("hair_short", an)][i], skin=sk, hair=hr)))
            res[f"{sk}/{hr}"] = {"v02": r02, "v03": max(rs, key=lambda r: r["weak_share"]), "frames": len(rs)}
    json.dump({"definition": "CIE76 colour difference across every 4-neighbour boundary between an opaque hair-layer pixel and a visible "
                             "skin pixel; weak = dE < 12. v03 = the worst of all 14 frames (idle4+walk4+death6); v02 = the single 02 frame. "
                             "Evidence for the boundary fix only, not a quality score.",
               "combos": res, "weak_share_over_0.10": {"v02": sum(r["v02"]["weak_share"] > 0.10 for r in res.values()),
                                                      "v03": sum(r["v03"]["weak_share"] > 0.10 for r in res.values())}},
              open(os.path.join(RV, "metrics", "hair_boundary_36.json"), "w", encoding="utf-8"), ensure_ascii=False, indent=1)
    return res


def build_enemy_fx_boards():
    shade_c = tuple(int(v) for v in tr.apply_shade(np.full((1, 1, 4), px.C("grass.2"), np.uint8), np.ones((1, 1), bool))[0, 0])
    rows, labs = [], []
    for k, nm in (("a1", "A1 작은 뱀 8fps"), ("a2", "A2 열매 나방 12fps"), ("a3", "A3 가시 마물 6fps")):
        for bname, bc in (("잔디", px.C("grass.2")), ("흙길", px.C("path.1")), ("그늘", shade_c)):
            fr = [on_bg(f, bc) for f in EN[k]]
            rows.append(fr + [px.gray(f) for f in fr]); labs.append(f"{nm} · {bname} | 흑백")
    save(grid_board(rows, 4, "적 3종 4프레임 — 잔디 · 흙길 · 그늘, 컬러 4칸 뒤 흑백 4칸 (×4)", row_labels=labs, bgkey=None, label_w=200),
         "boards", "board_enemies_x4.png")
    # 02 vs 03 A3 (02 read-only)
    old = Image.open(os.path.join(os.path.dirname(ROOT), "art-concept-02", "out", "png", "enm_a3-thorn-beast_walk_32.png")).convert("RGBA")
    old = np.array(old); oldf = [old[:, i * 32:(i + 1) * 32] for i in range(4)]
    rows = [[on_bg(f, c) for f in oldf] + [on_bg(f, c) for f in EN["a3"]] for c in (px.C("grass.2"), px.C("path.1"), shade_c)]
    rows += [[px.gray(f) for f in r] for r in rows]
    save(grid_board(rows, 4, "A3 02 (왼쪽 4칸) → 03 (오른쪽 4칸): 잔디 · 흙길 · 그늘, 아래 3줄은 흑백", bgkey=None, label_w=20),
         "boards", "board_a3_02-vs-03_x4.png")
    # FX
    rows = [EDGE, TRAIL, [on_bg(e) for e in EDGE], DEATH, HIT + BOOK + STONE + GEM["base"] + GEM["bonus"]]
    labs = ["턱뼈 앞날 (core) 4", "턱뼈 잔상 (optional, 반투명) 4", "앞날만 = FX 낮춤", "공통 사망 4 · 12fps 1회",
            "명중3 · 성경2 · 돌2 · 보석2+2"]
    save(grid_board(rows, 3, "효과 프레임 (×3) — 실제 export PNG", row_labels=labs, label_w=210), "boards", "board_fx_x3.png")
    # swing attach demo: player + edge + trail + jawbone at attachPx per frame (canvas wider than the 64 cell so the
    # separately drawn bone is not cut; dashed box = the 64x64 FX cell)
    save(grid_board([swing_frames(True)], 4, "턱뼈 휘두르기 4프레임 + attachPx 위치의 턱뼈 + 플레이어 (×4, 20fps 1회, 점선 = 64 셀)",
                    bgkey=None, label_w=10), "boards", "board_jawbone-swing_x4.png")


def swing_frames(box=False, bg=True):
    out = []
    pf = player("idle", "b-cloak-beard")[0]
    for i in range(4):
        c = px.new(80, 72)
        if bg:
            c[...] = px.C("grass.2")
        px.over(c, pf, 16, 4 + 16)              # body centre (32, 4+32) = cell centre
        px.over(c, TRAIL[i], 0, 4); px.over(c, EDGE[i], 0, 4)
        ax, ay = A["fx_jawbone-swing-edge_hit_64"]["attachPx"][i]
        px.over(c, JAW, ax - 8, 4 + ay - 8)
        if box:
            dashed(c, 0, 4, 64, 68, (255, 255, 255, 120), 2)
        out.append(c)
    return out


def build_tile_boards():
    grass = [frames_of(f"til_eden_grass-{c}_static_32")[0] for c in "abcd"]
    b = board(16 * 34 * 3 + 40, 160)
    label(b, "흙길 16마스크 (프레임 번호 = N1+E2+S4+W8), ×3", 10, 8, F11)
    for m in range(16):
        px.over(b, px.up(PT[m], 3), 10 + m * 102, 34)
        label(b, f"{m} {tl.NAMES[m]}", 10 + m * 102, 34 + 100, F7)
    save(b, "boards", "board_path-16mask_x3.png")
    img, M = tl.render_map(tl.TEST_MAP, PT, grass)
    img2, M2 = tl.render_map(tl.UNSUPPORTED_MAP, PT, grass)
    probs = tl.seam_check(M, PMASK); probs2 = tl.seam_check(M2, PMASK)
    bad = [r[:] for r in M]; bad[3][2] = 2
    probs_bad = tl.seam_check(bad, PMASK)
    json.dump({"testMap": {"masks_used": sorted({m for r in M for m in r if m >= 0}), "seam_problems": probs},
               "unsupportedMap": {"seam_problems": probs2, "note": "passes the border check but shows the inner-corner notch / two separate ends: unsupported by rule"},
               "negative_wrong_tile": {"change": "cell (2,3) straight-ew replaced by end-e", "seam_problems": probs_bad}},
              open(os.path.join(RV, "metrics", "path_seam_check.json"), "w", encoding="utf-8"), ensure_ascii=False, indent=1)
    W = img.shape[1] * 2 + 40
    b = board(max(W, img2.shape[1] * 2 + 40), img.shape[0] * 2 + img2.shape[0] * 2 + 120)
    label(b, f"검사 지도 — 16마스크 모두 사용, 경계 문제 {len(probs)}건 (×2, 마스크 번호 표시)", 10, 8, F11)
    v = px.up(img, 2)
    for y, r in enumerate(M):
        for x, m in enumerate(r):
            if m >= 0:
                hud.text(v, str(m), F7, x * 64 + 3, y * 64 + 3, px.C("text.0"))
    px.over(b, v, 10, 30)
    y2 = 40 + v.shape[0]
    label(b, "미지원 예: 2×2 넓은 길 → 가운데 풀 구멍 · 대각선으로만 닿은 칸 → 끝 두 개", 10, y2, F11)
    px.over(b, px.up(img2, 2), 10, y2 + 22)
    save(b, "boards", "board_path-testmap_x2.png")
    save(img, "boards", "path-testmap_native.png")


NINE_DEMO = {"ui_panel_9slice_16": [(40, 24), (72, 32)], "ui_card_9slice_24": [(40, 48), (90, 40)],
             "ui_button_9slice_16": [(48, 20), (90, 28)], "ui_bar-frame_9slice_8x6": [(40, 6), (90, 6)]}


def build_ui():
    """One row per exported piece: state frames x4 (from the PNG), a border guide drawn from ui-pieces_v3.json, and
    the piece stretched by the 9-slice renderer to two other sizes (x2)."""
    S = 4
    data = json.load(open(os.path.join(EXP, "ui", "ui-pieces_v3.json"), encoding="utf-8"))
    rows = []
    for pid, pc in data["pieces"].items():
        fr = frames_of(pid)
        rows.append((pid, pc, fr, max(max(f.shape[0] for f in fr) * S, 44)))
    H = 40 + sum(h + 16 for *_, h in rows) + 110
    b = board(1060, H)
    label(b, "UI 조각 (×4, export PNG) — 상태 프레임 · 하늘색 선 = 9-slice 테두리 (ui-pieces_v3.json 값) · 오른쪽 = 9-slice 로 늘린 예 (×2)", 10, 8, F11)
    y = 40
    guide = (120, 220, 255, 255)
    for pid, pc, fr, h in rows:
        label(b, pid, 10, y, F9)
        label(b, "상태: " + " / ".join(pc["states"]), 10, y + 14, F7, px.C("text.2"))
        if pc["border_LBRT"]:
            label(b, f"9-slice L,B,R,T = {pc['border_LBRT']}", 10, y + 26, F7, px.C("text.2"))
        if pc["content_xywh_topLeft"]:
            label(b, f"내용 칸 x,y,w,h = {pc['content_xywh_topLeft']}", 10, y + 38, F7, px.C("text.2"))
        x = 230
        for f in fr:
            px.over(b, px.up(on_bg(f), S), x, y)          # grass backdrop: the HUD sits over the game
            x += f.shape[1] * S + 8
        if pc["border_LBRT"]:
            L, B, R, T = pc["border_LBRT"]
            g = px.up(on_bg(fr[0]), S)
            gh, gw = g.shape[:2]
            for xx in (L * S, gw - R * S - 1):
                g[:, xx] = guide
            for yy in (T * S, gh - B * S - 1):
                g[yy, :] = guide
            x += 8
            px.over(b, g, x, y)
            x += gw + 16
            for w, hh in NINE_DEMO.get(pid, []):
                tmp = px.new(w, hh); tmp[...] = px.C("grass.2")
                ui.nine(tmp, pid, 0, 0, w, hh, 0)
                px.over(b, px.up(tmp, 2), x, y)
                label(b, f"{w}×{hh}", x, y + hh * 2 + 2, F7, px.C("text.2"))
                x += w * 2 + 12
        y += h + 16
    label(b, "아이콘 7종 (×3)", 10, y, F9)
    for i, k in enumerate(ui.icons()):
        px.over(b, px.up(frames_of(k)[0], 3), 10 + i * 110, y + 18)
        label(b, k.replace("_static_16", ""), 10 + i * 110, y + 70, F7)
    save(b, "boards", "board_ui-pieces_x4.png")
    return data


def mock_bg(view_img):
    v = view_img.copy()
    dim = np.array(px.C("dim.0"), np.float32)
    a = dim[3] / 255
    v[..., :3] = np.clip(v[..., :3] * (1 - a) + dim[:3] * a, 0, 255).astype(np.uint8)
    return v


def card(img, x, y, w, h, icon, title, lines, state=0, tag=None, badge=False):
    ui.nine(img, "ui_card_9slice_24", x, y, w, h, state)
    off = 1 if state == 2 else 0
    dim = state == 3
    if icon is not None:
        px.over(img, px.up(icon, 2), x + (w - 32) // 2, y + 12 + off)
        if badge:
            ui.stamp(img, "ui_badge-evolve_8", x + (w + 32) // 2 - 6, y + 8 + off)
    if tag:
        hud.text(img, tag, F7, x + w // 2, y + 50 + off, px.C("text.1"), outline=False, anchor="c")
    hud.text(img, title, F11, x + w // 2, y + 62 + off, px.C("text.2") if dim else px.C("text.0"), outline=False, anchor="c")
    for i, s in enumerate(lines):
        hud.text(img, s, F9, x + w // 2, y + 82 + off + i * 14, px.C("text.2"), outline=False, anchor="c")


def ico(k):
    return frames_of(k)[0]


def screen_levelup(bgimg, layout):
    v = mock_bg(bgimg)
    H, W = v.shape[:2]
    hits = []
    if layout == "portrait":
        pw, ph = W - 24, 3 * 96 + 60
        x0, y0 = 12, (H - ph) // 2
        ui.nine(v, "ui_panel_9slice_16", x0, y0, pw, ph)
        hud.text(v, "레벨 업!", F11B, W // 2, y0 + 10, px.C("text.1"), anchor="c")
        opts = [("ico_wpn_jawbone_static_16", "삼손의 턱뼈", ["Lv 3 → 4"], 1, None, False),
                ("ico_psv_scroll_static_16", "말씀의 두루마리", ["새 보조"], 0, "NEW", False),
                ("ico_wpn_sling_static_16", "다윗의 물맷돌", ["Lv 1 → 2"], 0, None, False)]
        for i, (ic, t, ls, st, tag, bd) in enumerate(opts):
            cx, cy, cw, chh = x0 + 10, y0 + 36 + i * 96, pw - 20, 88
            ui.nine(v, "ui_card_9slice_24", cx, cy, cw, chh, st)
            px.over(v, px.up(ico(ic), 2), cx + 12, cy + (chh - 32) // 2)
            hud.text(v, t, F11, cx + 56, cy + 24, outline=False)
            hud.text(v, ls[0], F9, cx + 56, cy + 44, px.C("text.2"), outline=False)
            if tag:
                hud.text(v, tag, F7, cx + cw - 10, cy + 10, px.C("text.1"), outline=False, anchor="r")
            hits.append((cx, cy, cw, chh))
        return v, hits
    pw, ph = 3 * 126 + 40, 220
    x0, y0 = (W - pw) // 2, (H - ph) // 2
    ui.nine(v, "ui_panel_9slice_16", x0, y0, pw, ph)
    hud.text(v, "레벨 업!", F11B, W // 2, y0 + 10, px.C("text.1"), anchor="c")
    hud.text(v, "하나를 고르세요", F9, W // 2, y0 + 28, px.C("text.2"), anchor="c")
    opts = [("ico_wpn_jawbone_static_16", "삼손의 턱뼈", ["Lv 3 → 4", "효과 문구 자리"], 1, None),
            ("ico_psv_scroll_static_16", "말씀의 두루마리", ["새 보조", "효과 문구 자리"], 0, "NEW"),
            ("ico_wpn_sling_static_16", "다윗의 물맷돌", ["Lv 1 → 2", "효과 문구 자리"], 0, None)]
    for i, (ic, t, ls, st, tag) in enumerate(opts):
        cx, cy = x0 + 20 + i * 126, y0 + 46
        card(v, cx, cy, 118, 150, ico(ic), t, ls, st, tag)
        hits.append((cx, cy, 118, 150))
    return v, hits


def screen_reward(bgimg, layout):
    v = mock_bg(bgimg)
    H, W = v.shape[:2]
    hits = []
    opts = [("ico_wpn_bible_static_16", "말씀의 성역", ["회전 성경 Lv 8", "+ 말씀의 두루마리"], 1, "진화", True),
            ("ico_wpn_jawbone_static_16", "삼손의 턱뼈", ["Lv 5 → 6"], 0, "강화", False),
            ("ico_res_gold_static_16", "골드 100", ["성장 보상이 부족할 때", "한 번만 나오는 칸"], 0, "재화", False)]
    if layout == "portrait":
        pw, ph = W - 24, 3 * 96 + 100
        x0, y0 = 12, (H - ph) // 2
        ui.nine(v, "ui_panel_9slice_16", x0, y0, pw, ph)
        hud.text(v, "보스 보상", F11B, W // 2, y0 + 10, px.C("text.1"), anchor="c")
        for i, (ic, t, ls, st, tag, bd) in enumerate(opts):
            cx, cy, cw, chh = x0 + 10, y0 + 36 + i * 96, pw - 20, 88
            ui.nine(v, "ui_card_9slice_24", cx, cy, cw, chh, st)
            px.over(v, px.up(ico(ic), 2), cx + 12, cy + (chh - 32) // 2)
            if bd:
                ui.stamp(v, "ui_badge-evolve_8", cx + 38, cy + (chh - 32) // 2 - 4)
            hud.text(v, t, F11, cx + 56, cy + 20, outline=False)
            for k, s in enumerate(ls):
                hud.text(v, s, F9, cx + 56, cy + 40 + k * 16, px.C("text.2"), outline=False)
            hud.text(v, tag, F7, cx + cw - 10, cy + 10, px.C("text.1"), outline=False, anchor="r")
            hits.append((cx, cy, cw, chh))
        bx, by = x0 + (pw - 120) // 2, y0 + ph - 54
        ui.nine(v, "ui_button_9slice_16", bx, by, 120, 44, 0)
        hud.text(v, "확인", F11, bx + 60, by + 15, outline=False, anchor="c")
        hits.append((bx, by, 120, 44))
        return v, hits
    phone = W != 640
    bw, bh = (120, 44) if phone else (80, 30)
    pw, ph = 3 * 126 + 40, 214 + bh + 8
    x0, y0 = (W - pw) // 2, (H - ph) // 2 - (8 if phone else 0)
    ui.nine(v, "ui_panel_9slice_16", x0, y0, pw, ph)
    hud.text(v, "보스 보상 — 하나를 고르세요", F11B, W // 2, y0 + 10, px.C("text.1"), anchor="c")
    for i, (ic, t, ls, st, tag, bd) in enumerate(opts):
        cx, cy = x0 + 20 + i * 126, y0 + 34
        card(v, cx, cy, 118, 160, ico(ic), t, ls, st, tag, bd)
        hits.append((cx, cy, 118, 160))
    bx, by = W // 2 - bw // 2, y0 + ph - bh - 10
    ui.nine(v, "ui_button_9slice_16", bx, by, bw, bh, 1)
    hud.text(v, "확인", F11, bx + bw // 2, by + (bh - 12) // 2, outline=False, anchor="c")
    hits.append((bx, by, bw, bh))
    return v, hits


def screen_custom(layout):
    """Customisation candidate. Geometry is fixed per layout (PC = mouse sizes; phones = 44 px targets inside the
    assumed safe area). Face has ONE option in the art, so its selector is drawn disabled."""
    W, H = {"pc": (640, 360), "phone-l": (780, 360), "phone-p": (360, 780)}[layout]
    v = px.new(W, H); v[...] = px.C("panel.1")
    hits = []
    preview = player("idle", "b-cloak-beard", skin="s5", hair="darkbrown", tunic="madder", cloak="indigo")[0]
    rows = [("피부", [REC["skin"][k][1] for k in REC["skin"]], 4), ("머리 색", [REC["hair"][k][1] for k in REC["hair"]], 1),
            ("튜닉 색", [REC["dye"][k][1] for k in REC["dye"]], 1), ("망토 색", [REC["dye"][k][1] for k in REC["dye"]], 2)]
    toggles = [("머리 모양", "짧은 머리", 0), ("수염", "있음", 0), ("망토", "있음", 0), ("두건", "없음", 0), ("얼굴", "기본 (1종)", 3)]
    dimc, txt = px.C("text.2"), px.C("text.0")

    def swatches(x, y, cols, sel, sw):
        for i, c in enumerate(cols):
            sx = x + i * (sw + 4)
            if sw > 12:
                ui.nine(v, "ui_button_9slice_16", sx, y, sw, sw, 1 if i == sel else 0)
                sxi, syi = sx + (sw - 12) // 2, y + (sw - 12) // 2
            else:
                sxi, syi = sx, y
            ui.stamp(v, "ui_swatch_12", sxi, syi, 1 if i == sel else 0)
            v[syi + 2:syi + 10, sxi + 2:sxi + 10] = px.hex_rgba(c)
            hits.append((sx, y, sw, sw))

    def toggle(x, y, val, bw, bh, state):
        ui.nine(v, "ui_button_9slice_16", x, y, bw, bh, state)
        if state != 3:
            ui.stamp(v, "ui_arrow_6x9", x + 5, y + (bh - 9) // 2, flip=True)
            ui.stamp(v, "ui_arrow_6x9", x + bw - 11, y + (bh - 9) // 2)
            hits.append((x, y, bw, bh))
        hud.text(v, val, F9, x + bw // 2, y + (bh - 10) // 2, dimc if state == 3 else txt, outline=False, anchor="c")

    def button(x, y, bw, bh, s, state=1):
        ui.nine(v, "ui_button_9slice_16", x, y, bw, bh, state)
        hud.text(v, s, F11, x + bw // 2, y + (bh - 12) // 2, outline=False, anchor="c")
        hits.append((x, y, bw, bh))

    def name_field(x, y, w, h):
        hud.text(v, "이름", F9, x + 2, y - 14, dimc, outline=False)
        ui.nine(v, "ui_panel_9slice_16", x, y, w, h)
        hud.text(v, "아담", F11, x + 10, y + (h - 12) // 2, outline=False)
        hits.append((x, y, w, h))

    if layout == "pc":
        hud.text(v, "꾸미기", F11B, W // 2, 10, px.C("text.1"), anchor="c")
        ui.nine(v, "ui_panel_9slice_16", 16, 36, 150, 170)
        px.over(v, px.up(preview, 4), 16 + 11, 36 + 20)
        name_field(16, 230, 150, 26)
        button(16, 300, 150, 30, "시작")
        x0, y = 190, 40
        for name, cols, sel in rows:
            hud.text(v, name, F9, x0, y + 1, txt, outline=False)
            swatches(x0 + 64, y, cols, sel, 12)
            y += 22
        y = 140
        for name, val, st in toggles:
            hud.text(v, name, F9, x0, y + 6, txt, outline=False)
            toggle(x0 + 64, y, val, 120, 22, st)
            y += 28
    elif layout == "phone-l":                 # usable x 48..732, y 0..339
        hud.text(v, "꾸미기", F11B, W // 2, 8, px.C("text.1"), anchor="c")
        ui.nine(v, "ui_panel_9slice_16", 56, 30, 150, 160)
        px.over(v, px.up(preview, 4), 56 + 11, 30 + 16)
        name_field(56, 216, 150, 44)
        button(56, 280, 150, 44, "시작")
        y = 28
        for name, cols, sel in rows:
            hud.text(v, name, F9, 222, y, txt, outline=False)
            swatches(222, y + 14, cols, sel, 44)
            y += 64
        y = 28
        for name, val, st in toggles:
            hud.text(v, name, F9, 522, y, txt, outline=False)
            toggle(522, y + 14, val, 196, 44, st)
            y += 62
    else:                                     # phone-p, usable y 47..746
        hud.text(v, "꾸미기", F11B, W // 2, 54, px.C("text.1"), anchor="c")
        ui.nine(v, "ui_panel_9slice_16", 12, 76, 336, 136)
        px.over(v, px.up(preview, 4), W // 2 - 64, 80)
        name_field(12, 238, 336, 44)
        y = 290
        for name, cols, sel in rows:
            hud.text(v, name, F9, 38, y, txt, outline=False)
            swatches(38, y + 14, cols, sel, 44)
            y += 62
        for i, (name, val, st) in enumerate(toggles):
            x = 16 if i % 2 == 0 else 184
            yy = y + (i // 2) * 62
            hud.text(v, name, F9, x, yy, txt, outline=False)
            toggle(x, yy + 14, val, 160, 44, st)
        button(184, y + 2 * 62 + 14, 160, 44, "시작")
    return v, hits


def touch_overlay(v, hits, min_px):
    o = v.copy()
    for x, y, w, h in hits:
        col = (120, 230, 140, 255) if min(w, h) >= min_px else (255, 90, 90, 255)
        dashed(o, x - 1, y - 1, x + w + 1, y + h + 1, col, 2)
    return o


def build_mocks(views_full):
    img, P = views_full
    rects = {k: v[0] for k, v in VIEWS.items()}
    made = []
    MIN = 44
    for scr in ("levelup", "reward", "custom"):
        for layout in ("pc", "phone-l", "phone-p"):
            x0, y0, x1, y1 = rects[layout]
            bgimg = img[y0:y1, x0:x1]
            lay = "portrait" if layout == "phone-p" else "landscape"
            if scr == "levelup":
                v, hits = screen_levelup(bgimg, lay)
            elif scr == "reward":
                v, hits = screen_reward(bgimg, lay)
            else:
                v, hits = screen_custom(layout)
            if layout != "pc":
                L, T, R, B = VIEWS[layout][1]
                dashed(v, L, T, v.shape[1] - R, v.shape[0] - B, (255, 255, 255, 170))
                v = touch_overlay(v, hits, MIN)
            save(px.up(v, 2), "mock", f"mock_{scr}_{layout}_x2.png")
            made.append((scr, layout, len(hits), sum(1 for h in hits if min(h[2], h[3]) >= MIN)))
    json.dump({"assumption": "CANDIDATE ONLY. Phone layouts aim for touch targets >= 44 game px (Apple HIG 44 pt / Material 48 dp would "
                             "hold only if 1 game px >= ~1.09 dp, i.e. a 360-px-tall landscape view on a >= ~390 dp tall screen). "
                             "Orientation, device scale and safe-area values are NOT decided; safe areas here are example insets. "
                             "PC layouts are mouse/keyboard: touch sizes do not apply. Nothing here is implemented UI.",
               "screens": [{"screen": s, "layout": l, "targets": n,
                            "meeting_44px": (ok if l != "pc" else "n/a (mouse)"),
                            "all_meet": (ok == n if l != "pc" else None)} for s, l, n, ok in made]},
              open(os.path.join(RV, "metrics", "touch_targets.json"), "w", encoding="utf-8"), ensure_ascii=False, indent=1)
    return made


def build_compare(views_full):
    img, P = views_full
    p02 = os.path.join(os.path.dirname(ROOT), "art-concept-02", "out", "views", "view_pc_native.png")
    old = np.array(Image.open(p02).convert("RGBA"))
    new = np.array(Image.open(os.path.join(RV, "views", "view_pc_native.png")).convert("RGBA"))
    b = board(1280 + 20, 720 * 2 + 70)
    label(b, "02 PC 화면 (위) → 03 PC 화면 (아래) · 같은 카메라 · 적 40마리 · 03은 가까운 전투 8마리를 플레이어 곁으로 옮김 (×2)", 10, 8, F11)
    px.over(b, px.up(old, 2), 10, 30); px.over(b, px.up(new, 2), 10, 50 + 720)
    save(b, "boards", "compare_02-vs-03_pc_x2.png")


# ---------------------------------------------------------------- animations
def save_anim(name, frames, fps, loop, scale=4, bgkey="grass.2"):
    ims = [Image.fromarray(px.up(on_bg(f, bgkey), scale), "RGBA").convert("RGB") for f in frames]
    dur = 1000.0 / fps
    gif = os.path.join(RV, "anim", name + ".gif")
    kw = {"loop": 0} if loop else {}
    ims[0].save(gif, save_all=True, append_images=ims[1:], duration=int(round(dur / 10.0) * 10), disposal=1, **kw)
    ap = os.path.join(RV, "anim", name + "_apng.png")
    ims[0].save(ap, save_all=True, append_images=ims[1:], duration=dur, loop=0 if loop else 1, format="PNG")
    return gif, ap


def build_anims():
    out = []
    for v in ("a-tunic", "b-cloak-beard", "c-hood", "d-hood-cloak-beard"):
        for an, sp in pl.RIG["animations"].items():
            fr = player(an, v)
            out.append((f"player_{v}_{an}", sp["fps"], sp["loop"], len(fr)))
            save_anim(f"player_{v}_{an}", fr, sp["fps"], sp["loop"])
    fr = player("walk", "b-cloak-beard", skin="s6", hair="brown", tunic="ochre", cloak="purple")
    save_anim("player_s6-brown_walk_flipx", [f[:, ::-1] for f in fr], 8, True)
    out.append(("player_s6-brown_walk_flipx", 8, True, 4))
    for aid in ("enm_a1-small-snake_move_32", "enm_a2-fruit-moth_fly_32", "enm_a3-thorn-beast_walk_32", "fx_enemy-death_burst_32",
                "fx_hit-spark_hit_16", "wpn_bible-book_spin_16", "wpn_sling-stone_spin_8", "pkp_xp-gem-base_idle_8", "pkp_xp-gem-bonus_idle_8"):
        a = A[aid]
        save_anim(aid.replace("_32", "").replace("_16", "").replace("_8", ""), frames_of(aid), a["fps"], a["loop"],
                  scale=4 if a["cell"][0] >= 16 else 8)
        out.append((aid, a["fps"], a["loop"], a["frames"]))
    save_anim("fx_jawbone-swing_with-player", swing_frames(bg=False), 20, False, scale=3)
    out.append(("fx_jawbone-swing (edge+trail+bone)", 20, False, 4))
    return out


def build_html(anims):
    """Local review page: plays the exported PNG strips at manifest fps (embedded as data URIs so file:// works)."""
    want = [a for a in MAN["assets"] if a["frames"] > 1 and a["fps"] > 0]
    data = {}
    for a in want:
        data[a["id"]] = {"src": "data:image/png;base64," + base64.b64encode(open(os.path.join(ROOT, a["file"]), "rb").read()).decode(),
                         "cell": a["cell"], "frames": a["frames"], "fps": a["fps"], "loop": a["loop"]}
    recol = {"keys": {nm: "#%02x%02x%02x" % px.C(nm)[:3] for nm in pl.RIG["keys"].values()},
             "skin": REC["skin"], "hair": REC["hair"], "dye": REC["dye"]}
    html = HTML.replace("__DATA__", json.dumps(data)).replace("__RECOL__", json.dumps(recol)) \
               .replace("__LAYERS__", json.dumps(pl.RIG["layer_order"])).replace("__VARIANTS__", json.dumps(pl.RIG["variants"]))
    open(os.path.join(RV, "index.html"), "w", encoding="utf-8").write(html)


HTML = r"""<!doctype html><html lang="ko"><head><meta charset="utf-8"><meta name="viewport" content="width=device-width,initial-scale=1">
<title>Art 03 local review</title>
<style>
:root{--bg:#18141f;--fg:#f4efe1;--dim:#a89cb4;--line:#4a3c56;--grass:#4f7039}
body{margin:0;background:var(--bg);color:var(--fg);font:14px/1.5 system-ui,sans-serif}
main{max-width:1100px;margin:0 auto;padding:16px}
h1{font-size:20px;margin:0 0 4px}h2{font-size:16px;margin:24px 0 8px}
p{color:var(--dim);margin:4px 0}
.grid{display:flex;flex-wrap:wrap;gap:12px;align-items:flex-start}
#layers{margin:6px 0}
.cell{border:1px solid var(--line);padding:8px;border-radius:6px}
canvas{image-rendering:pixelated;background:var(--grass);display:block}
label{margin-right:10px;color:var(--dim)} select,button{background:#2c2236;color:var(--fg);border:1px solid var(--line);border-radius:4px;padding:2px 6px}
.small{font-size:12px;color:var(--dim)}
</style></head><body><main>
<h1>Art 03 — local playback review</h1>
<p>Plays the exported PNG strips (export/png) at the fps and loop flags from export/manifest_v3.json. Static files, no build step. This page is a review aid, not game UI.</p>
<p><button id="play">일시정지</button> <button id="step">한 프레임</button> <button id="replay">다시 재생 (1회 동작)</button> <label><input type="checkbox" id="flip"> flipX (왼쪽)</label> <label>배율 <select id="scale"><option>2</option><option>3</option><option selected>4</option><option>6</option></select></label></p>
<h2>플레이어 — 레이어 합성</h2>
<div class="cell"><div id="pctl"></div><div class="grid" id="pgrid"></div></div>
<h2>적 · 효과 · 픽업 (각각 manifest fps)</h2>
<div class="grid" id="agrid"></div>
<p class="small">브라우저 타이머로 재생하므로 프레임 간격은 화면 주사율에 맞춰 약간 흔들릴 수 있다. 정확한 값은 manifest 의 fps.</p>
</main><script>
const DATA=__DATA__, RECOL=__RECOL__, LAYERS=__LAYERS__, VARIANTS=__VARIANTS__;
const FILE={cloak_back:"cloak-back",base:"base",hair_short:"hair-short",beard:"beard",hood:"hood",cloak_front:"cloak-front"};
const imgs={};let playing=true,t0=performance.now(),stepCount=0;
function load(id){return new Promise(r=>{const i=new Image();i.onload=()=>{imgs[id]=i;r()};i.src=DATA[id].src})}
function hex(h){h=h.replace('#','');return[parseInt(h.slice(0,2),16),parseInt(h.slice(2,4),16),parseInt(h.slice(4,6),16)]}
const opt={variant:"b-cloak-beard",skin:"s3",hair:"brown",tunic:"linen",cloak:"indigo"};
const recCache={};
function recoloured(id){const k=id+JSON.stringify(opt);if(recCache[k])return recCache[k];
 const im=imgs[id],c=document.createElement('canvas');c.width=im.width;c.height=im.height;const x=c.getContext('2d');x.drawImage(im,0,0);
 const d=x.getImageData(0,0,c.width,c.height),p=d.data,map={};
 const put=(ramp,cols)=>cols.forEach((h,i)=>{map[RECOL.keys[ramp+"."+i]]=hex(h)});
 put("skin",RECOL.skin[opt.skin]);put("hair",RECOL.hair[opt.hair]);put("tunic",RECOL.dye[opt.tunic]);put("cloak",RECOL.dye[opt.cloak]);
 const m={};for(const[k,v]of Object.entries(map))m[k]=v;
 for(let i=0;i<p.length;i+=4){if(!p[i+3])continue;const h='#'+[p[i],p[i+1],p[i+2]].map(v=>v.toString(16).padStart(2,'0')).join('');if(m[h]){p[i]=m[h][0];p[i+1]=m[h][1];p[i+2]=m[h][2]}}
 x.putImageData(d,0,0);recCache[k]=c;return c}
function frameAt(meta,ms){const n=meta.frames,f=Math.floor(ms*meta.fps/1000)+stepCount;return meta.loop?f%n:Math.min(f,n-1)}
const cells=[];
function addCell(parent,title,draw,w,h){const d=document.createElement('div');d.className='cell';const c=document.createElement('canvas');d.appendChild(c);
 const s=document.createElement('div');s.className='small';s.textContent=title;d.appendChild(s);parent.appendChild(d);cells.push({c,draw,w,h,s,title})}
const on=new Set(VARIANTS[opt.variant]);
function setup(){
 const pctl=document.getElementById('pctl');
 const lw=document.createElement('div');lw.id='layers';
 const syncBoxes=()=>{lw.textContent='레이어 (그리는 순서): ';LAYERS.forEach(ln=>{const l=document.createElement('label');const c=document.createElement('input');c.type='checkbox';c.checked=on.has(ln);c.onchange=()=>{c.checked?on.add(ln):on.delete(ln)};l.appendChild(c);l.append(' '+ln);lw.appendChild(l)})};
 const sel=(name,vals)=>{const l=document.createElement('label');l.textContent=name+' ';const s=document.createElement('select');vals.forEach(v=>{const o=document.createElement('option');o.textContent=v;s.appendChild(o)});s.value=opt[name];s.onchange=()=>{opt[name]=s.value;if(name==='variant'){on.clear();VARIANTS[s.value].forEach(x=>on.add(x));syncBoxes()}};l.appendChild(s);pctl.appendChild(l)};
 sel('variant',Object.keys(VARIANTS));sel('skin',Object.keys(RECOL.skin));sel('hair',Object.keys(RECOL.hair));sel('tunic',Object.keys(RECOL.dye));sel('cloak',Object.keys(RECOL.dye));
 pctl.appendChild(lw);syncBoxes();
 const pg=document.getElementById('pgrid');
 for(const an of["idle","walk","death"]){addCell(pg,an,(x,ms)=>{const meta=DATA[`chr_player_layer-base_${an}_32`];const f=frameAt(meta,ms);
   for(const ln of LAYERS){if(!on.has(ln))continue;const id=`chr_player_layer-${FILE[ln]}_${an}_32`;x.drawImage(recoloured(id),f*32,0,32,32,0,0,32,32)}
   return `${an} ${meta.fps}fps ${meta.loop?'반복':'1회'} · 프레임 ${f+1}/${meta.frames}`},32,32)}
 const ag=document.getElementById('agrid');
 for(const id of Object.keys(DATA)){if(id.startsWith('chr_'))continue;const meta=DATA[id];
  addCell(ag,id,(x,ms)=>{const f=frameAt(meta,ms);x.drawImage(imgs[id],f*meta.cell[0],0,meta.cell[0],meta.cell[1],0,0,meta.cell[0],meta.cell[1]);
   return `${id} · ${meta.fps}fps ${meta.loop?'반복':'1회'} · ${f+1}/${meta.frames}`},meta.cell[0],meta.cell[1])}
}
function tick(now){const sc=+document.getElementById('scale').value,flip=document.getElementById('flip').checked;const ms=playing?now-t0:pausedAt;
 for(const k of cells){const c=k.c;if(c.width!=k.w*sc){c.width=k.w*sc;c.height=k.h*sc}const x=c.getContext('2d');x.imageSmoothingEnabled=false;x.setTransform(1,0,0,1,0,0);x.clearRect(0,0,c.width,c.height);
  if(flip){x.setTransform(-sc,0,0,sc,c.width,0)}else{x.setTransform(sc,0,0,sc,0,0)}const t=k.draw(x,ms);k.s.textContent=t}
 requestAnimationFrame(tick)}
let pausedAt=0;
document.getElementById('play').onclick=e=>{if(playing){pausedAt=performance.now()-t0;playing=false;e.target.textContent='재생'}else{t0=performance.now()-pausedAt;playing=true;e.target.textContent='일시정지'}};
document.getElementById('step').onclick=()=>{stepCount++};
document.getElementById('replay').onclick=()=>{t0=performance.now();pausedAt=0;stepCount=0};
Promise.all(Object.keys(DATA).map(load)).then(()=>{setup();requestAnimationFrame(tick)});
</script></body></html>
"""


if __name__ == "__main__":
    views_full, inview = build_scene()
    print("scene: enemies inside the PC view =", inview)
    build_player_boards(); print("player boards")
    build_enemy_fx_boards(); print("enemy/fx boards")
    build_tile_boards(); print("tile boards")
    build_ui(); print("ui board")
    made = build_mocks(views_full["full"]); print("mock-ups", made)
    build_compare(views_full["full"]); print("compare")
    anims = build_anims(); print("animations", len(anims))
    build_html(anims); print("review/index.html")
