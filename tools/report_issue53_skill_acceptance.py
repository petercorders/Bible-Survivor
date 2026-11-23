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
