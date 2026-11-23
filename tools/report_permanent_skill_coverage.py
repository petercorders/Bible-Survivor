#!/usr/bin/env python3
"""Report every catalog purchase step against the runtime UI purchase gate."""
import collections
import csv
import re
import sys
from pathlib import Path

workspace = Path(__file__).resolve().parents[1]
content = workspace / "content/permanent-skills/catalog-v1.tsv"
ui = workspace / "UnityGame/Assets/Adam/Runtime/AdamGamePermanentSkills.cs"
routes = {
    "common.armor": "AdamSession.Damage → PermanentCombatRules.Armor",
    "common.health": "AdamSession start → PermanentCombatRules.StartingHealth",
    "common.move": "BattleWorld.MovePlayer → AdamSession.MoveSpeedAt → PermanentActionRules.Move",
    "common.pickup": "BattleWorld.CollectNearby → AdamSession.PickupRangeAt → PermanentActionRules.Pickup",
    "common.xp": "AdamSession.AwardExperience → PermanentActionRules.Experience → GrowthLedger.Add",
    "common.regen": "AdamSession.Regenerate / RecoverOnKill / Damage → PermanentRecoveryRules",
    "common.crit": "BattleWorld.Hit → AdamSession.RollOriginalDamage → PermanentOriginalHitRules.BaseCritical",
    "common.power": "AdamSession.AttackProfile → CombatStats damage profile",
    "common.tempo": "AdamSession.AttackProfile → CombatStats cycle",
    "common.area": "AdamSession.AttackProfile → BattleWorld collision, attack geometry and schedules",
    "common.shot": "AdamSession.AttackProfile → BattleWorld projectile launch and ExpandedCross",
}
actors = {
    "cross": "ExpandedCross",
    "daniel_lion": "ExpandedDanielLion",
    "elijah_chariot": "ExpandedElijahChariot",
    "fire_pillar": "ExpandedFirePillar",
    "lightning": "ExpandedLightning",
    "moses_staff": "ExpandedMosesStaff",
    "wilderness_voice": "ExpandedWildernessVoice",
}

def implementation_route(key, tree):
    if tree in actors:
        return "AdamSession.AttackProfile → BattleWorld.RunExpandedAttack → " + actors[tree]
    if key in {"weapon.bible.base.04", "weapon.bible.base.05"}:
        return "AdamSession.AttackProfile → BattleWorld.Attack / Pulse → BattleWeaponState"
    if key == "weapon.jawbone.base.05":
        return "BattleWorld.Attack → AdamSession.JawboneDamage → BattleWorld.Hit"
    return routes.get(tree, "AdamSession.AttackProfile → BattleWorld.Attack")

allowlist = ui.read_text().split("PlayablePermanentSkills =", 1)[1].split("};", 1)[0]
allowed = set(re.findall(r'"([a-z_0-9.]+)"', allowlist))
totals = collections.Counter()
seen = set()
with content.open(encoding="utf-8") as source:
    draw = csv.writer(sys.stdout, delimiter="\t", lineterminator="\n")
    draw.writerow(("ID", "트리", "랭크", "골드", "선행", "정산 라운드", "무기 해금", "진화 업적",
                   "효과 명세", "구매 가능", "실행 경로", "검증 상태"))
    for node in csv.reader((row for row in source if not row.startswith("#")), delimiter="\t"):
        key, tree, _, _, _, prices, requirements, round_number, unlock, evolved, effect = node
        assert key not in seen
        seen.add(key)
        connected = key in allowed
        audit = "연결, 개별 랭크 인수 미완료" if connected else "실행 경로 미연결"
        if not connected and key.startswith(("common.power.", "common.crit.", "common.tempo.")):
            audit = "계산 단위 시험 있음, 실제 전투 미연결"
        route = implementation_route(key, tree) if connected else ""
        for rank, price in enumerate(prices.split(","), 1):
            draw.writerow((key, tree, rank, price, requirements, round_number, unlock, evolved,
                           effect, "예" if connected else "아니요", route, audit))
            totals["ranks"] += 1
            totals["connected_ranks"] += connected
        totals["nodes"] += 1
        totals["connected_nodes"] += connected
assert totals["ranks"] == 442 and totals["nodes"] == 420 and allowed <= seen
print(dict(totals), file=sys.stderr)
