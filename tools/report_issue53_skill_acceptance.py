#!/usr/bin/env python3
"""Join the release ledger to individual purchase tests and Core lookup sites."""

import collections
import csv
import re
import subprocess
import sys
from pathlib import Path


ROOT = Path(__file__).resolve().parents[1]
CORE = Path("src/BibleSurvivor.Core")
SOURCE_FILES = {
    path.relative_to(ROOT): path.read_text(encoding="utf-8").splitlines()
    for path in sorted((ROOT / CORE).glob("*.cs"))
    if not path.name.startswith(("Save", "PermanentSkill", "PermanentProfile"))
}

# Each entry points to assertions in PermanentSkillCombatTests.cs. The three
# columns are independent. Snapshot fields alone do not prove the next hit.
AUDITED_TESTS = {
    "weapon.lightning.a.03": (
        "Approved lightning reuse: fourth pending strike changes target",
        "Approved lightning reuse: same two-target rotation; different three-target rotation",
        "Approved lightning reuse: same world after advancing the restored strike",
    ),
    "weapon.sling.c.01": (
        "A purchased Sling ricochet: original contact creates one child for a different target",
        "",
        "A purchased Sling ricochet: restored moving child hits; ensuing world matches",
    ),
    "weapon.jawbone.a.02": (
        "A purchased Jawbone reverse strike: pending reverse connects behind the player",
        "",
        "A purchased Jawbone reverse strike: restored pending slot connects; ensuing world matches",
    ),
}

# These cases create their individual ranks through the shop transaction and
# compare live world events across the boundary and after reopening the save.
COMBAT_EVIDENCE = {
    **{f"weapon.lightning.base.{index:02}@1": ("Lightning", boundary)
       for index, boundary in enumerate((
           "원본 피해 +10%p, 착탄 전 무피해",
           "4초 → 3.8초, 다음 주기 직전/개시 틱",
           "1 → 2회 낙뢰, 두 번째 표적 실제 피해",
           "반경 0.80 → 0.88, 안쪽·사이·바깥 실제 피해",
           "지연 0.15 → 0.07초, 이전 틱 무피해",
       ), 1)},
    **{f"weapon.lightning.{suffix}@1": ("Lightning", boundary) for suffix, boundary in (
        ("a.01", "군집 반경 1.5 안/밖과 다른 표적의 실제 피해"),
        ("a.02", "탐색거리 안/밖의 정예와 일반 표적"),
        ("a.04", "착탄 전 표적 사망/생존, 1회 재조준 소비와 원래 낙뢰 시각"),
        ("a.05", "고립 반경 2의 양쪽 1.99/2.01 배치"),
        ("b.01", "대상 HP 75%/74.9%"),
    )},
    **{f"armor@{rank}": ("Armor", "접촉 피해 감소와 최소 피해 1")
       for rank in range(1, 4)},
    **{f"common.armor.{index:02}@1": ("Armor", boundary)
       for index, boundary in enumerate((
           "R09 본체 접촉/사슬 투사체", "R09 사슬 투사체/본체 접촉",
           "피격 전 HP 30%/30.1%", "피격 간격 8초/7.999초",
           "정예/일반 적 접촉", "반경 3 이내 6/5체",
           "접촉 피해 감소와 최소 피해 1", "피격 전 HP 90%/89.9%",
           "일반 웨이브/R03 활성 보스 본체 접촉, 인트로 무피해",
       ), 2)},
    **{f"common.power.{index:02}@1": ("Power", boundary)
       for index, boundary in enumerate((
           "정예/일반 표적", "주기 첫/두 번째 타격",
           "대상 HP 25%/25.1%", "내 HP 90%/89.9%",
           "동일 낙뢰 두 번째/세 번째 표적", "착탄 시 피격 후 3초/3.001초",
           "거리 경계 4의 양쪽 3.99/4.01 배치", "연속 세 번째/네 번째·표식 만료",
           "R03 보스 HP 25%/25.1%와 일반 적 HP 25% 대조",
       ), 2)},
}


def lookup_sites(skill, tree):
    keys = {path: [skill] for path in SOURCE_FILES}
    if tree in {"common.shot", "common.duration"}:
        options = "PermanentShotRules.cs" if tree == "common.shot" else "PermanentDurationRules.cs"
        keys[CORE / options].append(skill.rsplit(".", 1)[1])
    if skill.startswith("weapon."):
        _, _, branch, number = skill.split(".")
        suffix = f"{branch}.{number}"
        for path, lines in SOURCE_FILES.items():
            if any(f'"weapon.{tree}." +' in line for line in lines):
                keys[path].append(suffix)
        if tree == "bible" and branch == "e":
            keys[CORE / "ExpandedBibleEvolution.cs"].append(number)
        if branch == "base":
            # Restrict the dynamic lookup to AttackProfile's per-weapon prefix.
            path = CORE / "AdamSession.cs"
            keys[path].append(f'skillPrefix + "{number}"')
            if (tree, number) in {("bible", "04"), ("jawbone", "05"), ("lightning", "04"), ("lightning", "05")}:
                keys[path].pop()

    sites = []
    for path, tokens in keys.items():
        for line_number, line in enumerate(SOURCE_FILES[path], 1):
            for token in tokens:
                if token.startswith("skillPrefix"):
                    starter = {"bible": "collisionBonus = .12", "jawbone": "bonuses.AreaBonus += .10",
                               "sling": ("collisionBonus += .10" if skill.endswith("04")
                                         else "bonuses.ProjectileSpeedBonus += .12")}
                    matched = token in line and (
                        int(skill[-2:]) <= 3 or
                        (tree in starter and starter[tree] in line) or
                        (tree not in starter and "collisionBonus);" in line)
                    )
                elif token != skill:
                    matched = bool(re.search(rf'\b(?:Has|Effect)\("{re.escape(token)}"[,)]', line))
                else:
                    matched = f'"{token}"' in line
                if matched:
                    sites.append(f"{path.name}:{line_number} {line.strip()}")
                    break
    return " | ".join(sites)


def main():
    result = subprocess.run(
        [sys.executable, str(ROOT / "tools/report_permanent_skill_coverage.py")],
        check=True, capture_output=True, text=True,
    )
    rows = csv.DictReader(result.stdout.splitlines(), delimiter="\t")
    extra = ("Core 효과 조회 위치", "구매 경계 시험", "부분 효과 증거")
    output = csv.DictWriter(sys.stdout, fieldnames=[*rows.fieldnames, *extra], delimiter="\t", lineterminator="\n")
    output.writeheader()
    counts = collections.Counter()
    for row in rows:
        skill, tree, rank = row["ID"], row["트리"], row["랭크"]
        stage = f"{skill}@{rank}"
        sites = lookup_sites(skill, tree)
        if not sites or row["구매 가능"] != "예":
            raise SystemExit(f"Missing Core lookup or shop connection: {stage}")
        supplemental = ""
        dimensions = ("발동 근거", "경계 근거", "저장 재생 근거")
        for column, assertion in zip(dimensions, AUDITED_TESTS.get(skill, ())):
            if assertion:
                row[column] = assertion
        if skill == "health":
            row["발동 근거"] = f"Health rank {stage}: 구매 후 시작·최대 HP"
            row["저장 재생 근거"] = f"Health rank {stage}: HP 보존과 재실행 후 동일 피해"
            supplemental = "가격은 구매 경계 시험. 100% 보너스 상한의 별도 변이는 미실행"
        if tree == "common.health" and skill != "health":
            row["발동 근거"] = f"Health upgrade {stage}: 구매 전후 시작·최대 HP 차이"
            row["저장 재생 근거"] = f"Health upgrade {stage}: HP 보존과 재실행 후 동일 피해"
        if re.fullmatch(r"weapon\.[a-z_]+\.base\.0[123]", skill):
            supplemental = f"Mastery profile {stage}: 구매 전후 착용 무기의 수치·비착용 무기 대조·재개 수치"
        if stage in COMBAT_EVIDENCE:
            family, boundary = COMBAT_EVIDENCE[stage]
            test = f"{family} combat and replay {stage}"
            row["발동 근거"] = f"{test}: 구매 전후 실제 전투 결과 대조"
            row["경계 근거"] = f"{test}: {boundary}"
            row["저장 재생 근거"] = f"{test}: 전투 스냅숏 저장·복원 후 결과 일치"
        row.update(zip(extra, (sites, f"Purchase boundary {stage}", supplemental)))
        output.writerow(row)
        counts["ranks"] += 1
        for column in dimensions:
            counts[column] += row[column] == "미확인"
    if counts["ranks"] != 442:
        raise SystemExit(f"Unexpected ledger length: {counts['ranks']}")
    print(dict(counts), file=sys.stderr)


if __name__ == "__main__":
    main()
