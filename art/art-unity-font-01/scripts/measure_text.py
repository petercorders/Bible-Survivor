#!/usr/bin/env python3
"""Measure the level-up card text with the outline fonts (art-unity-font-01). Standard library only.

Usage:  python3 -I -B scripts/measure_text.py [--out DIR]
Writes  export/text_metrics_v1.json (or DIR/export/...): for every card line the current Unity UI can show, its width in
        game px at the role's basePx (window px = game px x UI scale s), with and without the GPOS 'kern' pair
        adjustments, and whether it fits the developer's two card widths; plus the card height the line metrics need.
Card text = a Python re-implementation of UnityGame/Assets/Adam/Runtime/AdamChoiceText.Describe / Card (2026-11-14)
        with the Core WeaponRules values (src/BibleSurvivor.Core/Growth.cs). It was compared by eye with the three cards
        in the developer's capture (crown, underwear, east_wind at level 0); it is NOT run in Unity.
Widths come from hmtx advances (+ kern). Every advance and kern value is a multiple of the 100-unit grid, so widths are
whole pixels. Nothing here renders in Unity."""
import argparse, json, os, sys

HERE = os.path.dirname(os.path.abspath(__file__))
ROOT = os.path.dirname(HERE)
sys.path.insert(0, HERE)
import sfnt

NAMES = {"bible": "회전 성경", "jawbone": "삼손의 턱뼈", "sling": "다윗의 물맷돌", "scroll": "말씀의 두루마리",
         "hair": "삼손의 머리카락", "stones": "매끄러운 다섯 돌", "east_wind": "동풍", "seal": "사자굴의 봉인석",
         "cloud": "구름기둥", "thunder": "우레", "crown": "가시 면류관", "underwear": "요한의 팬티", "whirlwind": "회오리바람"}
PASSIVE = {"scroll": ("경험치 획득 보너스", 8, "%"), "hair": ("무기 피해 보너스", 10, "%"),
           "stones": ("무기 생성·연타 추가 수", 1, "개"), "east_wind": ("돌 투사체 속도 보너스", 10, "%"),
           "seal": ("보조 방어(피해 감소)", 1, " (최소 피해 1)"), "cloud": ("회전 성경 지속시간 보너스", 10, "%"),
           "thunder": ("대기시간 감소 보너스", 8, "% (합산 최대 80%)"), "crown": ("피격 시 근처 적 반격 피해", 5, " (피해 보너스 반영)"),
           "underwear": ("이동속도 보너스", 10, "%"), "whirlwind": ("무기 범위 보너스", 10, "%")}
INT_MAX = 2147483647


def weapon(wid, lv):
    """Core WeaponRules.Get: damage, amount, cooldown, duration, area, pierce, rotation"""
    if wid == "bible":
        return (10 + (10 if lv >= 4 else 0) + (10 if lv >= 7 else 0), 1 + (lv >= 2) + (lv >= 5) + (lv >= 8), 3,
                3 + (0.5 if lv >= 4 else 0) + (0.5 if lv >= 7 else 0), 1 + (0.25 if lv >= 3 else 0) + (0.25 if lv >= 6 else 0),
                INT_MAX, 1 + (0.30 if lv >= 3 else 0) + (0.30 if lv >= 6 else 0))
    if wid == "jawbone":
        return (10 + (10 if lv >= 4 else 0) + (10 if lv >= 6 else 0) + (10 if lv >= 8 else 0), 2 if lv >= 2 else 1,
                0.9 if lv >= 5 else 1, 0, 1 + (0.2 if lv >= 3 else 0) + (0.2 if lv >= 7 else 0), INT_MAX, 1)
    return (10 + (10 if lv >= 5 else 0) + (10 if lv >= 8 else 0), 1 + (lv >= 2) + (lv >= 4) + (lv >= 6),
            1 if lv >= 3 else 1.2, 0, 1, 2 if lv >= 7 else 1, 1)


def N(v):
    """C# value.ToString("0.##", InvariantCulture)"""
    s = f"{round(float(v), 2):.2f}".rstrip("0").rstrip(".")
    return "0" if s in ("", "-0") else s


def card(item, cur, slot=0):
    w = item in ("bible", "jawbone", "sling")
    mx = 8 if w else (2 if item == "stones" else 5)
    head = f"{slot + 1}. {NAMES[item]}  |  {'무기' if w else '보조'} · {'신규' if cur == 0 else '강화'} {cur} → {cur + 1}/{mx}"
    if w:
        nxt = weapon(item, cur + 1)
        if cur == 0:
            atk = {"bible": "주변 회전", "jawbone": "전방 120° 타격", "sling": "가까운 적 자동 조준"}[item]
            eff = f"{atk} · 기본 피해 {nxt[0]} · 수 {nxt[1]} · 대기 {N(nxt[2])}초"
        else:
            bef = weapon(item, cur)
            labels = [("기본 피해", 0), ("생성·연타 수", 1), ("대기(초)", 2), ("지속(초)", 3), ("범위 배율", 4),
                      ("회전속도 배율", 6), ("관통 수", 5)]
            eff = " · ".join(f"{lb} {N(bef[i])} → {N(nxt[i])}" for lb, i in labels if bef[i] != nxt[i])
    else:
        e, per, unit = PASSIVE[item]
        eff = f"{e} {N(cur * per)} → {N((cur + 1) * per)}{unit}"
    return head, eff, mx


def main(argv=None):
    ap = argparse.ArgumentParser()
    ap.add_argument("--out")
    a = ap.parse_args(argv)
    C = json.load(open(os.path.join(ROOT, "src", "font_contract_v1.json"), encoding="utf-8"))
    fonts = {}
    for role, r in C["roles"].items():
        F = sfnt.Font(open(os.path.join(ROOT, *r["file"].split("/")), "rb").read())
        fonts[role] = (F, F.cmap(), r["gridUnits"], r["basePx"])

    def width(role, s):
        F, cm, g, _ = fonts[role]
        gids = [cm.get(ord(ch), 0) for ch in s]
        adv = sum(F.advance(x) for x in gids)
        kern = sum(F.kern(x, y) for x, y in zip(gids, gids[1:]))
        assert adv % g == 0 and kern % g == 0, "advance/kern off the pixel grid"
        return {"px": adv // g, "pxKerned": (adv + kern) // g, "missingGlyphs": sum(1 for x in gids if x == 0)}

    # developer layout (AdamChoiceText.ChoicePanel/ChoiceCard, window px) at UI scale 2, and the art 03 card piece
    border, pad, mark = 6, 2, 10                   # ui_card_9slice_24 border; inner padding; focus marker column
    cards_w = {"660x440 window": 612 // 2, "1100x720 window": 736 // 2}
    inner = {k: v - 2 * (border + pad) - mark for k, v in cards_w.items()}
    lm = {}
    for role, (F, cm, g, base) in fonts.items():
        hh = F.table("hhea")[0]
        asc, desc, gap = F._i16(hh + 4) // g, F._i16(hh + 6) // g, F._i16(hh + 8) // g
        lm[role] = {"basePx": base, "ascender": asc, "descender": desc, "lineGap": gap, "box": asc - desc,
                    "lineHeight": asc - desc + gap}
    head_box, eff_box, gap = lm["label"]["box"], lm["body"]["box"], 2

    def card_h(eff_lines):
        return 2 * (border + pad) + head_box + gap + eff_lines * eff_box + (eff_lines - 1) * gap

    rows = []
    for item in NAMES:
        mx = card(item, 0)[2]
        for cur in range(mx):
            head, eff, _ = card(item, cur)
            hw, ew = width("label", head), width("body", eff)
            rows.append({"item": item, "level": f"{cur}->{cur + 1}", "header": head, "effect": eff,
                         "headerPx": hw["pxKerned"], "effectPx": ew["pxKerned"],
                         "headerPxNoKern": hw["px"], "effectPxNoKern": ew["px"],
                         "missingGlyphs": hw["missingGlyphs"] + ew["missingGlyphs"],
                         "effectLinesAt": {k: 1 if ew["pxKerned"] <= v else 2 for k, v in inner.items()},
                         "headerFitsAt": {k: hw["pxKerned"] <= v for k, v in inner.items()}})
    ui = [("title", "레벨 상승 · 하나를 선택하세요"), ("body", "전투 정지 중 · 숫자 1–3 / 클릭 · 선택 즉시 저장"),
          ("body", "무기 수치는 기본값 · 보조/영구 강화는 별도 합산 · 진화 미구현"), ("title", "시작 무기를 선택하세요"),
          ("label", "재개 (Enter)"), ("body", "저장 기록을 불러오는 중…")]
    ui_rows = [{"role": r, "text": s, **width(r, s)} for r, s in ui]
    over = {k: [f"{x['item']} {x['level']}" for x in rows if x["effectLinesAt"][k] > 1] for k in inner}
    head_over = {k: [f"{x['item']} {x['level']}" for x in rows if not x["headerFitsAt"][k]] for k in inner}
    out = {"schema": "bible-survivors.font-text-metrics", "schemaVersion": 1,
           "note": "game px at the role basePx (UI scale s: window px = game px x s; font size = basePx x s). Widths "
                   "include the GPOS kern pair adjustments (*NoKern without). Card text re-implements "
                   "AdamChoiceText.Describe/Card in Python (crown with damageMultiplier 1); not run in Unity.",
           "lineMetrics": lm,
           "cardLayout": {"cardWidthGamePx": cards_w, "border": border, "padding": pad, "focusMarkerColumn": mark,
                          "textWidthGamePx": inner, "headerBox": head_box, "effectLineBox": eff_box, "lineGap": gap,
                          "cardHeightGamePx": {"1 effect line": card_h(1), "2 effect lines": card_h(2)},
                          "developerCardHeightGamePx": 88 // 2},
           "summary": {"cards": len(rows), "maxHeaderPx": max(r["headerPx"] for r in rows),
                       "maxEffectPx": max(r["effectPx"] for r in rows), "effectNeedsTwoLines": over,
                       "headerTooWide": head_over, "missingGlyphs": sum(r["missingGlyphs"] for r in rows)},
           "uiLines": ui_rows, "cards": rows}
    dst = os.path.join(a.out or ROOT, "export", "text_metrics_v1.json")
    os.makedirs(os.path.dirname(dst), exist_ok=True)
    with open(dst, "w", encoding="utf-8") as f:
        json.dump(out, f, ensure_ascii=False, indent=1)
        f.write("\n")
    s = out["summary"]
    print(f"cards {s['cards']}: max header {s['maxHeaderPx']} px, max effect {s['maxEffectPx']} px (game px, kerned); "
          f"text width {inner}; missing glyphs {s['missingGlyphs']}")
    for k in inner:
        print(f"  {k}: effect needs 2 lines: {len(over[k])} {over[k][:6]}; header too wide: {len(head_over[k])} {head_over[k][:4]}")
    print(f"  card height: 1 effect line {card_h(1)} game px, 2 lines {card_h(2)} (developer card 44)")
    return 0


if __name__ == "__main__":
    sys.exit(main())
