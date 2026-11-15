#!/usr/bin/env python3
"""Review boards, loops and metrics for the R02 enemy sidecar. Needs numpy + Pillow. NOT a handoff input.

Usage:  python3 -I -B scripts/review_r02.py [--art03 DIR] [--font-root DIR]
Reads   export/png/*.png (this package), art 03 PNGs/manifest (terrain, player layers, shadows, gems, hit spark, death
        burst, A1-A3 enemies; unchanged) and the Galmuri11 outline font for labels.
Writes  review/boards/board_r02_solo_x8.png        each enemy: 4 frames x8 on grass / path / grey, plus 1x and 2x
        review/boards/board_r02_lineup_x4.png      A1 A2 A3 | B1 B2 B3 with their shadows, silhouettes, on grass and path
        review/boards/board_r02_motion_x4.png      foot line, flipX, Unity hit reaction (tint x squash), hit spark / death burst overlap
        review/views/scene_r02_mixed{40,50}_x2.png same camera (640x360 game px), player + gems + path, 40 / 50 mixed B enemies
        review/views/scene_r02_mixed50_gray_x2.png value check of the 50 scene
        review/views/scene_r02_mixed50_native.png  1x
        review/anim/<id>.gif and <id>_apng.png     loops at the contract fps on grass (x4)
        review/metrics/r02_metrics.json            silhouettes, IoU, contrast, hit-tint change, frame timing
Every board here is a script composite (Pillow), NOT Unity and NOT the Claude Design product. Scenes place enemies with a
fixed seed and draw them sorted by feet y; Unity's actual draw order, camera and particles are NOT_RUN here."""
import argparse, hashlib, json, math, os, sys

import numpy as np
from PIL import Image, ImageDraw, ImageFont

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.dirname(HERE)
sys.path.insert(0, HERE)
import pngio

BG = (0x1b, 0x14, 0x24, 255)
TXT = (0xf4, 0xef, 0xe1, 255)
DIM = (0xa8, 0x9c, 0xb4, 255)
NAMES = {"a1": "A1 작은 뱀", "a2": "A2 열매 나방", "a3": "A3 가시 마물",
         "b1": "B1 시기의 눈", "b2": "B2 분노의 사냥개", "b3": "B3 갈라진 가면"}


def load(path):
    w, h, b = pngio.read_rgba(path)
    return np.frombuffer(b, np.uint8).reshape(h, w, 4).copy()


def over(dst, src, ox, oy):
    h, w = src.shape[:2]
    H, W = dst.shape[:2]
    x0, y0, x1, y1 = max(0, ox), max(0, oy), min(W, ox + w), min(H, oy + h)
    if x0 >= x1 or y0 >= y1:
        return
    s = src[y0 - oy:y1 - oy, x0 - ox:x1 - ox].astype(np.float32)
    d = dst[y0:y1, x0:x1].astype(np.float32)
    sa = s[..., 3:4] / 255.0
    out = s[..., :3] * sa + d[..., :3] * (1 - sa)
    dst[y0:y1, x0:x1, :3] = np.clip(np.round(out), 0, 255).astype(np.uint8)
    dst[y0:y1, x0:x1, 3] = np.maximum(d[..., 3], s[..., 3]).astype(np.uint8)


def up(a, s):
    return np.repeat(np.repeat(a, s, axis=0), s, axis=1)


def gray(a):
    g = np.round(a[..., 0] * 0.299 + a[..., 1] * 0.587 + a[..., 2] * 0.114).astype(np.uint8)
    out = a.copy()
    out[..., 0] = out[..., 1] = out[..., 2] = g
    return out


def canvas(w, h, col=BG):
    a = np.zeros((h, w, 4), np.uint8)
    a[...] = col
    return a


class Art:
    def __init__(self, art03):
        self.art03 = art03
        m = json.load(open(os.path.join(art03, "export", "manifest_v3.json"), encoding="utf-8"))
        self.m03 = {x["id"]: x for x in m["assets"]}
        self.r02 = {x["id"]: x for x in json.load(open(os.path.join(ROOT, "export", "manifest_r02.json"), encoding="utf-8"))["assets"]}
        self.cache = {}

    def sheet(self, aid):
        if aid not in self.cache:
            e = self.r02.get(aid) or self.m03[aid]
            base = ROOT if aid in self.r02 else self.art03
            self.cache[aid] = (load(os.path.join(base, *e["file"].split("/"))), e)
        return self.cache[aid]

    def frame(self, aid, k=0):
        a, e = self.sheet(aid)
        cw, ch = e["cell"]
        return a[:, k * cw:(k + 1) * cw]

    def draw(self, dst, aid, fx, fy, k=0, flip=False):
        """place at a feet point (fx, fy) in image px using the asset's pivot (bottom-left pixel-edge coords)"""
        a, e = self.sheet(aid)
        fr = self.frame(aid, k % e["frames"])
        if flip:
            fr = fr[:, ::-1]
        px_, py_ = e["pivot"]["px"]
        cw, ch = e["cell"]
        if flip:
            px_ = cw - px_
        over(dst, fr, int(round(fx - px_)), int(round(fy - (ch - py_))))


ENEMY = {"a1": "enm_a1-small-snake_move_32", "a2": "enm_a2-fruit-moth_fly_32", "a3": "enm_a3-thorn-beast_walk_32",
         "b1": "enm_b1-envy-eye_move_32", "b2": "enm_b2-rage-hound_run_32", "b3": "enm_b3-cracked-mask_move_32"}
SHADOW = {"a1": "fx_shadow-snake_static_20x4", "a2": "fx_shadow-small_static_12x4", "a3": "fx_shadow-wide_static_24x6"}


def shadow_of(art, mid):
    if mid in SHADOW:
        return SHADOW[mid]
    return art.r02[ENEMY[mid]]["shadow"]["id"]


class Label:
    def __init__(self, font_path):
        self.font_path = font_path
        self.fonts = {}

    def put(self, img, x, y, text, px=12, col=TXT):
        if px not in self.fonts:
            self.fonts[px] = ImageFont.truetype(self.font_path, px)
        im = Image.fromarray(img, "RGBA")
        d = ImageDraw.Draw(im)
        d.fontmode = "1"
        d.text((x, y), text, font=self.fonts[px], fill=col)
        img[...] = np.asarray(im)


def ground_tile(art, kind, w, h, seed=0):
    """grass a-d tiles in a fixed pattern (kind 'grass') or the path strip's straight piece (kind 'path')"""
    out = canvas(w, h)
    for ty in range(0, h, 32):
        for tx in range(0, w, 32):
            if kind == "grass":
                v = "abcd"[((tx // 32) * 7 + (ty // 32) * 13 + seed) % 4]
                over(out, art.frame(f"til_eden_grass-{v}_static_32"), tx, ty)
            else:
                over(out, art.frame("til_eden_path-16mask_static_32", 10), tx, ty)
    return out


def board_solo(art, L, out):
    S = 8
    ids = [ENEMY[k] for k in ("b1", "b2", "b3")]
    RW = 4 * 32 * S + 8 + 4 * 32 * 2 + 8 + 4 * 32 + 8
    rows = []
    for aid in ids:
        e = art.r02[aid]
        lab = canvas(RW, 28)
        L.put(lab, 4, 6, f"{NAMES[aid[4:6]]} · {aid} · {e['fps']}fps loop · pivot {e['pivot']['px']} · 그림자 {e['shadow']['id']}", 16)
        rows.append(lab)
        for bgk in ("grass", "path"):
            row = canvas(RW, 32 * S)
            bg = ground_tile(art, bgk, 32, 32)
            for k in range(4):
                cell = bg.copy()
                art.draw(cell, shadow_of(art, aid[4:6]), 16, 31)
                art.draw(cell, aid, 16, 31, k)
                over(row, up(cell, S), k * 32 * S, 0)
            g = ground_tile(art, bgk, 32, 32)
            art.draw(g, aid, 16, 31, 0)
            over(row, up(gray(g), S // 2), 4 * 32 * S + 8, 0)
            nat = ground_tile(art, bgk, 32 * 4, 32)
            for k in range(4):
                art.draw(nat, aid, k * 32 + 16, 31, k)
            over(row, up(nat, 2), 4 * 32 * S + 8, 32 * S // 2 + 8)
            over(row, nat, 4 * 32 * S + 8 + 4 * 32 * 2 + 8, 32 * S // 2 + 8)
            rows.append(row)
    W = max(r.shape[1] for r in rows)
    img = canvas(W, 40 + sum(r.shape[0] + 6 for r in rows))
    L.put(img, 8, 10, "R02 적 3종 · 프레임 4개 ×8 (잔디 / 흙길) · 오른쪽: 흑백 ×4, 실제 크기 ×2 와 ×1 — 스크립트 합성, Unity 아님", 16)
    y = 40
    for r in rows:
        over(img, r, 0, y)
        y += r.shape[0] + 6
    Image.fromarray(img, "RGBA").save(out, optimize=True)


def silhouette(fr):
    m = fr[..., 3] > 0
    return m


def board_lineup(art, L, out):
    S = 4
    order = ["a1", "a2", "a3", "b1", "b2", "b3"]
    cw = 40
    img = canvas(len(order) * cw * S + 16, 40 + 3 * (40 * S + 30))
    L.put(img, 8, 10, "아트03 A1–A3 과 R02 B1–B3 · 프레임0 · 같은 발 기준(피벗) · 잔디 / 흙길 / 실루엣 (×4)", 16)
    y = 40
    for kind in ("grass", "path", "sil"):
        for i, mid in enumerate(order):
            if kind == "sil":
                cell = canvas(cw, 40, (0xf4, 0xef, 0xe1, 255))
                m = silhouette(art.frame(ENEMY[mid], 0))
                sil = np.zeros((32, 32, 4), np.uint8)
                sil[m] = (0x1b, 0x14, 0x24, 255)
                over(cell, sil, cw // 2 - 16, 40 - 4 - 31)
            else:
                cell = ground_tile(art, kind, cw, 40)
                art.draw(cell, shadow_of(art, mid), cw // 2, 36)
                art.draw(cell, ENEMY[mid], cw // 2, 36)
            over(img, up(cell, S), 8 + i * cw * S, y + 22)
            L.put(img, 8 + i * cw * S + 4, y + 2, NAMES[mid], 14, TXT if mid[0] == "b" else DIM)
        y += 40 * S + 30
    Image.fromarray(img, "RGBA").save(out, optimize=True)


def tint_react(fr, pivot, strength=1.0):
    """Unity AdamArtWorld hit reaction: GUI.color (1, .55, .35) multiply + scale (1+.12, 1-.15) about the feet (nearest)."""
    t = fr.astype(np.float32)
    t[..., 0] *= 1.0
    t[..., 1] *= 0.55
    t[..., 2] *= 0.35
    t = np.round(t).astype(np.uint8)
    h, w = fr.shape[:2]
    sx, sy = 1 + .12 * strength, 1 - .15 * strength
    fx, fy = pivot[0], h - pivot[1]                     # feet in top-left image coords
    out = np.zeros_like(fr)
    for y in range(h):
        for x in range(w):
            u = (x + 0.5 - fx) / sx + fx - 0.5
            v = (y + 0.5 - fy) / sy + fy - 0.5
            ui, vi = int(round(u)), int(round(v))
            if 0 <= ui < w and 0 <= vi < h:
                out[y, x] = t[vi, ui]
    return out


def board_motion(art, L, out):
    S = 4
    ids = [ENEMY[k] for k in ("b1", "b2", "b3")]
    cols = ["프레임0", "프레임1", "프레임2", "프레임3", "flipX 0", "flipX 2", "피격 반응", "피격+스파크", "사망 버스트2"]
    W = len(cols) * 34 * S + 16
    img = canvas(W, 60 + len(ids) * (34 * S + 30))
    L.put(img, 8, 10, "동작 · 발 기준선(빨강, 피벗 행) · flipX · Unity 피격 반응(색 ×(1,.55,.35)·발 기준 1.12×0.85) · 아트03 스파크/버스트 겹침 (×4)", 16)
    y = 60
    spark = art.frame("fx_hit-spark_hit_16", 1)
    burst = art.frame("fx_enemy-death_burst_32", 2)
    for aid in ids:
        e = art.r02[aid]
        L.put(img, 8, y + 2, NAMES[aid[4:6]], 14)
        for i in range(len(cols)):
            cell = ground_tile(art, "grass", 34, 34)
            foot_y = 33 - 1                                # sprite foot row lands on cell row 32; the dashed line is drawn just below it
            art.draw(cell, shadow_of(art, aid[4:6]), 17, 33)
            k = (0, 1, 2, 3, 0, 2, 1, 1, 0)[i]
            if i < 4:
                art.draw(cell, aid, 17, 33, k)
            elif i < 6:
                art.draw(cell, aid, 17, 33, k, flip=True)
            else:
                fr = tint_react(art.frame(aid, k), e["pivot"]["px"])
                over(cell, fr, 17 - 16, 33 - 31)
                if i == 7:
                    over(cell, spark, 17 - 8, 33 - 16 - 8)
                if i == 8:
                    art.draw(cell, "fx_enemy-death_burst_32", 17, 33, 2)
            if i < 6:
                for x in range(34):
                    if x % 2 == 0:
                        cell[foot_y + 1, x] = (0xe0, 0x40, 0x50, 255)
            over(img, up(cell, S), 8 + i * 34 * S, y + 22)
            if y == 60:
                L.put(img, 8 + i * 34 * S + 4, 36, cols[i], 12, DIM)
        y += 34 * S + 30
    Image.fromarray(img, "RGBA").save(out, optimize=True)


def lcg(seed):
    x = seed
    while True:
        x = (1103515245 * x + 12345) & 0x7fffffff
        yield x / 0x7fffffff


def scene(art, counts, seed, W=640, H=360):
    img = canvas(W, H)
    for ty in range(0, H, 32):
        for tx in range(0, W, 32):
            v = "abcd"[((tx // 32) * 7 + (ty // 32) * 13) % 4]
            over(img, art.frame(f"til_eden_grass-{v}_static_32"), tx, ty)
    py_row, px_col = (H // 32) // 2 + 1, (W // 32) // 2 - 4
    for tx in range(0, W, 32):
        m = 2 + 8 + (5 if tx // 32 == px_col else 0)
        over(img, art.frame("til_eden_path-16mask_static_32", m), tx, py_row * 32)
    for ty in range(0, H, 32):
        if ty // 32 != py_row:
            over(img, art.frame("til_eden_path-16mask_static_32", 5), px_col * 32, ty)
    r = lcg(seed)
    pxp, pyp = W / 2, H / 2 + 8
    for _ in range(26):
        gx, gy = 16 + next(r) * (W - 32), 16 + next(r) * (H - 32)
        art.draw(img, "pkp_xp-gem-base_idle_8" if next(r) < 0.8 else "pkp_xp-gem-bonus_idle_8", gx, gy)
    enemies = []
    for mid, n in counts.items():
        for _ in range(n):
            while True:
                ang, dist = next(r) * 2 * math.pi, 40 + next(r) * 230
                ex, ey = pxp + math.cos(ang) * dist * 1.25, pyp + math.sin(ang) * dist * 0.75
                if 18 < ex < W - 18 and 34 < ey < H - 4:
                    break
            enemies.append((ey, ex, mid, int(next(r) * 4)))
    for ey, ex, mid, k in enemies:
        art.draw(img, shadow_of(art, mid), ex, ey)
    art.draw(img, "fx_shadow_static_16x6", pxp, pyp)
    actors = [(ey, ex, mid, k) for ey, ex, mid, k in enemies] + [(pyp, pxp, "player", 0)]
    for ey, ex, mid, k in sorted(actors):
        if mid == "player":
            for layer in ("cloak-back", "base", "hair-short", "cloak-front"):
                art.draw(img, f"chr_player_layer-{layer}_idle_32", ex, ey, 0)
        else:
            art.draw(img, ENEMY[mid], ex, ey, k, flip=ex > pxp)
    return img


def anims(art, out_dir):
    timing = {}
    for mid in ("b1", "b2", "b3"):
        aid = ENEMY[mid]
        e = art.r02[aid]
        frames = []
        for k in range(e["frames"]):
            cell = ground_tile(art, "grass", 40, 40)
            art.draw(cell, shadow_of(art, mid), 20, 36)
            art.draw(cell, aid, 20, 36, k)
            frames.append(Image.fromarray(up(cell, 4), "RGBA").convert("RGB"))
        ms = 1000 / e["fps"]
        gif = os.path.join(out_dir, f"{aid}.gif")
        frames[0].save(gif, save_all=True, append_images=frames[1:], duration=round(ms), loop=0, optimize=False)
        ap = os.path.join(out_dir, f"{aid}_apng.png")
        frames[0].save(ap, save_all=True, append_images=frames[1:], duration=round(ms), loop=0)
        with Image.open(gif) as g:
            stored = []
            for i in range(g.n_frames):
                g.seek(i)
                stored.append(g.info.get("duration"))
        timing[aid] = {"fps": e["fps"], "intendedMs": round(ms, 3), "gifStoredMs": stored,
                       "apngRequestedMs": round(ms), "note": "GIF stores 10 ms units; the APNG keeps whole ms. Unity plays the manifest fps, not these files."}
    return timing


def rel_lum(rgb):
    c = rgb / 255.0
    c = np.where(c <= 0.03928, c / 12.92, ((c + 0.055) / 1.055) ** 2.4)
    return 0.2126 * c[..., 0] + 0.7152 * c[..., 1] + 0.0722 * c[..., 2]


def metrics(art):
    order = ["a1", "a2", "a3", "b1", "b2", "b3"]
    grass = ground_tile(art, "grass", 64, 64)[..., :3].reshape(-1, 3).astype(np.float64)
    path = ground_tile(art, "path", 64, 64)[..., :3].reshape(-1, 3).astype(np.float64)
    Lg, Lp = float(rel_lum(grass).mean()), float(rel_lum(path).mean())
    sil, out = {}, {"note": "script measurements on the PNGs (frame union unless said); not Unity. Contrast = (L1+0.05)/(L2+0.05) of mean relative luminance. pairIoU = silhouette overlap of every frame pair aligned at the same pivot; a1-a2 / a1-a3 / a2-a3 are the reviewed art 03 baseline.",
                    "groundMeanLuminance": {"grass": round(Lg, 4), "path": round(Lp, 4)}, "enemies": {}, "pairIoU": {}}
    for mid in order:
        a, e = art.sheet(ENEMY[mid])
        n, cw = e["frames"], e["cell"][0]
        frs = [a[:, k * cw:(k + 1) * cw] for k in range(n)]
        union = np.zeros((32, 32), bool)
        for fr in frs:
            union |= fr[..., 3] > 0
        sil[mid] = [fr[..., 3] > 0 for fr in frs]
        ys, xs = np.nonzero(union)
        body = np.concatenate([fr[fr[..., 3] > 0][:, :3] for fr in frs]).astype(np.float64)
        Lb = float(rel_lum(body).mean())
        tinted = body * np.array([1.0, 0.55, 0.35])
        Lt = float(rel_lum(tinted).mean())
        out["enemies"][mid] = {
            "id": ENEMY[mid], "unionBBox": [int(xs.min()), int(ys.min()), int(xs.max()), int(ys.max())],
            "unionW": int(xs.max() - xs.min() + 1), "unionH": int(ys.max() - ys.min() + 1),
            "meanOpaquePx": round(float(np.mean([m.sum() for m in sil[mid]])), 1),
            "meanLuminance": round(Lb, 4),
            "contrastVsGrass": round((max(Lb, Lg) + .05) / (min(Lb, Lg) + .05), 2),
            "contrastVsPath": round((max(Lb, Lp) + .05) / (min(Lb, Lp) + .05), 2),
            "hitTintLuminance": round(Lt, 4), "hitTintLuminanceDrop": round(1 - Lt / Lb, 3),
            "hitTintMeanRgbShift": round(float(np.abs(body - tinted).mean()), 1)}
    for i, x in enumerate(order):
        for y in order[i + 1:]:
            ious = []
            for fa in sil[x]:
                for fb in sil[y]:
                    inter = np.logical_and(fa, fb).sum()
                    ious.append(inter / np.logical_or(fa, fb).sum())
            out["pairIoU"][f"{x}-{y}"] = {"max": round(float(max(ious)), 3), "mean": round(float(np.mean(ious)), 3)}
    return out


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--art03", default=os.path.join(os.path.dirname(ROOT), "art-concept-03"))
    ap.add_argument("--font-root", default=os.path.join(os.path.dirname(ROOT), "art-unity-font-01"))
    a = ap.parse_args()
    art = Art(a.art03)
    L = Label(os.path.join(a.font_root, "fonts", "galmuri-outline", "Galmuri11.ttf"))
    for d in ("boards", "views", "anim", "metrics"):
        os.makedirs(os.path.join(ROOT, "review", d), exist_ok=True)
    board_solo(art, L, os.path.join(ROOT, "review", "boards", "board_r02_solo_x8.png"))
    board_lineup(art, L, os.path.join(ROOT, "review", "boards", "board_r02_lineup_x4.png"))
    board_motion(art, L, os.path.join(ROOT, "review", "boards", "board_r02_motion_x4.png"))
    counts = {"40": {"b1": 20, "b2": 12, "b3": 8}, "50": {"b1": 25, "b2": 15, "b3": 10}}
    for n, cnt in counts.items():
        img = scene(art, cnt, 7)
        Image.fromarray(up(img, 2), "RGBA").save(os.path.join(ROOT, "review", "views", f"scene_r02_mixed{n}_x2.png"), optimize=True)
        if n == "50":
            Image.fromarray(up(gray(img), 2), "RGBA").save(os.path.join(ROOT, "review", "views", "scene_r02_mixed50_gray_x2.png"), optimize=True)
            Image.fromarray(img, "RGBA").save(os.path.join(ROOT, "review", "views", "scene_r02_mixed50_native.png"), optimize=True)
    timing = anims(art, os.path.join(ROOT, "review", "anim"))
    met = metrics(art)
    met["frameTiming"] = timing
    met["scenes"] = {"camera": "640x360 game px (same for both)", "counts": counts, "seed": 7,
                     "drawOrder": "shadows, then actors sorted by feet y; enemies right of the player flipped (Unity rule enemy.X > player.X)",
                     "player": "art 03 layers cloak-back, base, hair-short, cloak-front idle frame 0"}
    with open(os.path.join(ROOT, "review", "metrics", "r02_metrics.json"), "w", encoding="utf-8") as f:
        json.dump(met, f, ensure_ascii=False, indent=1)
        f.write("\n")
    for mid, v in met["enemies"].items():
        print(mid, v["unionW"], "x", v["unionH"], "contrast grass", v["contrastVsGrass"], "path", v["contrastVsPath"],
              "tint drop", v["hitTintLuminanceDrop"])
    print("IoU max (B vs others):", {k: v["max"] for k, v in met["pairIoU"].items()})
    print("timing:", {k: (v["intendedMs"], v["gifStoredMs"]) for k, v in timing.items()})
    return 0


if __name__ == "__main__":
    sys.exit(main())
