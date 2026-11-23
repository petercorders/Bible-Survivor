#!/usr/bin/env python3
"""Report every purchase step, its runtime route, and audited effect evidence."""
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
    "common.power": "BattleWorld.Hit → AdamSession.ResolveOriginalDamage → PermanentOriginalHitRules.ApplyPower",
    "common.tempo": "AdamSession.AttackProfile → CombatStats cycle",
    "common.area": "AdamSession.AttackProfile → BattleWorld collision, attack geometry and schedules",
    "common.shot": "BattleWorld.Attack / RunExpandedAttack → PermanentShotRules → launch or late contact",
    "common.duration": "BattleWorld.Attack / RunExpandedAttack → PermanentDurationRules → cohort clock or Cross return",
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

# Evidence is recorded only for the individual rank and dimension the assertion exercises.
rank_evidence = {
    ("pickup", 1): ("P01 새 도전 수집", "P01 고정 위치 보석", "P01 환불 뒤 기존 도전 재개"),
    ("weapon.sling.base.01", 1): ("P02 무기별 피해 계산", "", ""),
    ("weapon.sling.base.03", 1): ("P02 추가 투석", "P02 7·8발 수량 상한", "P02 발사 중 저장과 재생"),
    ("common.shot.04", 1): ("Cross·Staff 부채꼴 실타격", "Cross·Staff 가장자리 표적", "Cross·Staff 비행 중 저장과 재생"),
    ("weapon.jawbone.e.02", 1): ("진화형 월드 밀침 충돌", "월드 기본형 제외·단위 시험 2초 재사용", "밀침 충돌 후 저장과 재생"),
    ("weapon.jawbone.c.02", 1): ("라이프사이클 단위 시험의 원본 처치", "단위 시험 2초 연속 처치·3초 대기 감소", "보스 진입 상태 만료 뒤 재개"),
    ("weapon.wilderness_voice.d.01", 1): ("", "", "보스 진입 상태 만료 뒤 재개"),
}
for ordinal in range(2, 11):
    rank_evidence[(f"common.health.{ordinal:02}", 1)] = ("시작 체력 누적 단계별 단정", "", "")


def implementation_route(key, tree):
    if tree in actors:
        return "AdamSession.AttackProfile → BattleWorld.RunExpandedAttack → " + actors[tree]
    if tree == "bible":
        branch = key.split(".")[2]
        if branch == "c" or key in {"weapon.bible.d.03", "weapon.bible.d.04"}:
            return "BattleWorld.Attack → BattleWorldBible → ExpandedBibleSupportsState"
        if branch == "e":
            return "BattleWorld.Pulse / RecordBibleEliteSanctuary → ExpandedBibleEvolutionState"
        if branch == "d" or key in {"weapon.bible.b.03", "weapon.bible.b.04", "weapon.bible.b.05"}:
            return "BattleWorld.Attack / ApplyPlayerDamage → ExpandedBibleLifecycleState"
        return "AdamSession.AttackProfile → BattleWorld.Attack / Pulse → ExpandedBibleRanks"
    if tree == "jawbone":
        return "BattleWorld.Attack / ApplyPlayerDamage → ResolveExpandedJawbone → ExpandedJawboneRanks / Lifecycle"
    if tree == "sling":
        return "BattleWorld.Attack / MoveProjectiles → BattleWorldSling → ExpandedSlingRanks / CombatState"
    return routes.get(tree, "AdamSession.AttackProfile → BattleWorld.Attack")

allowlist = ui.read_text().split("PlayablePermanentSkills =", 1)[1].split("};", 1)[0]
allowed = set(re.findall(r'"([a-z_0-9.]+)"', allowlist))
totals = collections.Counter()
seen = set()
with content.open(encoding="utf-8") as source:
    draw = csv.writer(sys.stdout, delimiter="\t", lineterminator="\n")
    draw.writerow(("ID", "트리", "랭크", "골드", "선행", "정산 라운드", "무기 해금", "진화 업적",
                   "효과 명세", "구매 가능", "실행 경로", "검증 상태",
                   "발동 근거", "경계 근거", "저장 재생 근거"))
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
            evidence = rank_evidence.get((key, rank), ("", "", ""))
            draw.writerow((key, tree, rank, price, requirements, round_number, unlock, evolved,
                           effect, "예" if connected else "아니요", route, audit,
                           *(entry or "미확인" for entry in evidence)))
            totals["ranks"] += 1
            totals["connected_ranks"] += connected
            totals["ranks_with_audited_evidence"] += any(evidence)
        totals["nodes"] += 1
        totals["connected_nodes"] += connected
assert totals["ranks"] == 442 and totals["nodes"] == 420 and allowed <= seen
print(dict(totals), file=sys.stderr)
