#!/usr/bin/env python3
"""Validate Genesis design data against the documented initial budgets.

No game engine or third-party packages are required. Exit 0 means design-data
validation passed; it says nothing about implemented gameplay or difficulty.
"""

import argparse
import json
import math
import re
import sys
from pathlib import Path


class ContentError(ValueError):
    pass


def require(condition, message):
    if not condition:
        raise ContentError(message)


def integer(value, label, minimum=0):
    require(type(value) is int and value >= minimum, f"{label}: expected integer >= {minimum}")


def finite_positive(value, label):
    require(type(value) in (int, float) and math.isfinite(value) and value > 0,
            f"{label}: expected finite positive number")


def unique(items, key, label):
    require(isinstance(items, list), f"{label}: expected array")
    values = [item[key] for item in items]
    require(len(values) == len(set(values)), f"{label}: duplicate {key}")
    return {item[key]: item for item in items}


def no_duplicate_keys(pairs):
    result = {}
    for key, value in pairs:
        require(key not in result, f"duplicate JSON property: {key}")
        result[key] = value
    return result


# Direct transcription of docs/genesis-balance.md's 27 monster rows.
EXPECTED_MONSTER_STATS = [
    (1, 4, .45), (6, 4, .60), (12, 5, .35),
    (12, 5, .55), (18, 6, .90), (30, 6, .45),
    (30, 6, .55), (50, 7, .50), (130, 9, .30),
    (45, 7, .65), (60, 7, .50), (100, 10, .60),
    (60, 8, .60), (80, 9, .45), (90, 9, .95),
    (80, 8, .70), (120, 10, .50), (270, 12, .35),
    (120, 9, .65), (180, 10, .55), (240, 11, .65),
    (160, 10, .65), (340, 12, .40), (400, 13, .55),
    (160, 10, .85), (260, 11, .60), (600, 14, .35),
]
EXPECTED_PERSONS = ["아담", "가인", "노아", "아브라함", "롯", "이삭", "야곱", "요셉", "유다"]
EXPECTED_SPAWNS = [240, 360, 480, 600, 690, 780, 900, 1020, 1140]
EXPECTED_XP = [300, 600, 1100, 1600, 1900, 2400, 3000, 3500, 4200]
EXPECTED_COMPLETION_GOLD = [25, 35, 50, 65, 80, 100, 120, 140, 160]
EXPECTED_WAVE_COUNTS = [
    [60, 80, 100], [90, 120, 150], [120, 160, 200],
    [160, 200, 240], [180, 230, 280], [200, 260, 320],
    [240, 300, 360], [270, 340, 410], [300, 380, 460],
]
# Percentages of the three local monster IDs (a1/a2/a3, etc.).
EXPECTED_PERCENTAGES = [
    [[100, 0, 0], [70, 30, 0], [60, 30, 10]],
    [[80, 0, 20], [70, 30, 0], [50, 30, 20]],
    [[80, 20, 0], [70, 30, 0], [60, 30, 10]],
    [[80, 20, 0], [70, 20, 10], [60, 30, 10]],
    [[70, 30, 0], [60, 20, 20], [50, 20, 30]],
    [[80, 20, 0], [70, 20, 10], [50, 30, 20]],
    [[70, 30, 0], [60, 30, 10], [50, 20, 30]],
    [[100, 0, 0], [70, 30, 0], [50, 20, 30]],
    [[80, 20, 0], [70, 30, 0], [50, 30, 20]],
]
EXPECTED_UNLOCKS = {
    "bible": (0, False), "jawbone": (0, False), "sling": (0, False),
    "wilderness_voice": (1, False), "lightning": (2, False),
    "moses_staff": (3, True), "fire_pillar": (4, False),
    "daniel_lion": (5, False), "cross": (6, True), "elijah_chariot": (7, False),
}
EXPECTED_EVOLUTIONS = {
    "bible": ("scroll", "sanctuary"),
    "jawbone": ("hair", "thousand_strength"),
    "sling": ("stones", "five_stone_barrage"),
    "moses_staff": ("east_wind", "red_sea_path"),
    "daniel_lion": ("seal", "den_guardian"),
    "fire_pillar": ("cloud", "wilderness_guidance"),
    "lightning": ("thunder", "thunder_judgment"),
    "cross": ("crown", "passion_radiance"),
    "wilderness_voice": ("underwear", "wilderness_proclamation"),
    "elijah_chariot": ("whirlwind", "ascending_chariot"),
}
EXPECTED_BOSSES = {
    "nephilim_tyrant": (3, 1800, 12, 100, 6, 1.2, 15, 0, [(0.5, "c1", 6)]),
    "sitnah_keeper": (6, 6500, 16, 200, 7, 1.2, 18, 0, [(0.7, "f1", 6), (0.35, "f1", 6)]),
    "silver_cup_accuser": (9, 16000, 20, 350, 5, 1, 15, 3, [(0.6, "i2", 6), (0.3, "i2", 6)]),
}


def validate(data):
    require(type(data["schema_version"]) is int and data["schema_version"] == 1,
            "unsupported schema_version")
    require(data["chapter_id"] == "genesis", "chapter_id must be genesis")
    require(data["status"] == "design_data", "status must not imply gameplay completion")
    rules = data["rules"]
    for key, expected in {
        "normal_duration_seconds": 180, "spawn_cutoff_seconds": 170,
        "boss_interval_rounds": 3, "boss_intro_seconds": 2,
        "boss_support_cap": 12, "kills_per_gold": 5, "boss_fallback_gold": 100,
        "weapon_slots": 6, "passive_slots": 6, "level_up_choices": 3,
    }.items():
        integer(rules[key], f"rules.{key}", 1)
        require(rules[key] == expected, f"rules.{key}: differs from design")
    for key, expected in {
        "normal_round_completion": "all_scheduled_enemies_defeated_or_time_expired",
        "boss_round_completion": "normal_condition_and_boss_defeated",
        "xp_allocation": "floor_budget_per_spawn_plus_one_to_highest_health_then_spawn_order",
        "spawn_placement": "offscreen",
    }.items():
        require(rules[key] == expected, f"rules.{key}: differs from design")
    monsters = unique(data["monsters"], "id", "monsters")
    expected_monster_ids = [f"{letter}{i}" for letter in "abcdefghi" for i in (1, 2, 3)]
    require(set(monsters) == set(expected_monster_ids), "expected exactly 27 stable monster IDs")
    for monster_id, stats in zip(expected_monster_ids, EXPECTED_MONSTER_STATS):
        monster = monsters[monster_id]
        integer(monster["max_health"], f"{monster_id}.max_health", 1)
        integer(monster["contact_damage"], f"{monster_id}.contact_damage", 1)
        finite_positive(monster["speed_ratio"], f"{monster_id}.speed_ratio")
        require((monster["max_health"], monster["contact_damage"], monster["speed_ratio"]) == stats,
                f"{monster_id}: combat values differ from design")
        require(monster["source_code"] == monster_id.upper(), f"{monster_id}: source code mismatch")
        require(isinstance(monster["name_ko"], str) and monster["name_ko"], f"{monster_id}: missing name")
        require(re.fullmatch(r"[a-z][a-z0-9_]*", monster["behavior_id"]), f"{monster_id}: invalid behavior ID")
        require("dash" not in monster or monster_id == "d3", "dash is only allowed for d3")
    require(monsters["d3"]["dash"] == {
        "telegraph_seconds": 1.2, "speed_ratio": 1.3,
        "duration_seconds": .6, "cooldown_seconds": 6,
    }, "d3: dash values differ from design")

    bosses = unique(data["bosses"], "id", "bosses")
    require(set(bosses) == set(EXPECTED_BOSSES), "expected exactly three bosses")
    for boss_id, expected in EXPECTED_BOSSES.items():
        boss = bosses[boss_id]
        attack = boss["attack"]
        supports = [(s["health_ratio"], s["monster_id"], s["count"]) for s in boss["support_summons"]]
        actual = (boss["round_number"], boss["max_health"], boss["contact_damage"], boss["gold"],
                  attack["cooldown_seconds"], attack["telegraph_seconds"], attack["damage"],
                  attack["projectile_count"], supports)
        require(actual == expected, f"{boss_id}: boss values differ from design")
        require(attack["pattern_id"] == {
            "nephilim_tyrant": "telegraphed_club_slam",
            "sitnah_keeper": "telegraphed_dirt_throw",
            "silver_cup_accuser": "three_way_chain_projectiles",
        }[boss_id], f"{boss_id}: invalid attack pattern")
        for field in ("round_number", "max_health", "contact_damage", "gold"):
            integer(boss[field], f"{boss_id}.{field}", 1)
        for field in ("cooldown_seconds", "telegraph_seconds", "damage"):
            finite_positive(attack[field], f"{boss_id}.attack.{field}")
        integer(attack["projectile_count"], f"{boss_id}.projectile_count")
        require(sum(s[2] for s in supports) <= rules["boss_support_cap"], f"{boss_id}: support cap exceeded")
        require(len({s[0] for s in supports}) == len(supports), f"{boss_id}: repeated threshold")
        for health_ratio, monster_id, count in supports:
            finite_positive(health_ratio, f"{boss_id}.health_ratio")
            require(health_ratio < 1 and monster_id in monsters, f"{boss_id}: invalid support trigger/reference")
            integer(count, f"{boss_id}.support.count", 1)

    unlocks = unique(data["weapon_unlocks"], "weapon_id", "weapon_unlocks")
    require(set(unlocks) == set(EXPECTED_UNLOCKS), "expected exactly 10 weapons")
    for weapon_id, expected in EXPECTED_UNLOCKS.items():
        unlock = unlocks[weapon_id]
        integer(unlock["round_number"], f"{weapon_id}.round_number")
        require(type(unlock["requires_boss_reward"]) is bool, f"{weapon_id}: invalid boss requirement")
        require((unlock["round_number"], unlock["requires_boss_reward"]) == expected,
                f"{weapon_id}: unlock differs from design")
    evolutions = unique(data["evolution_pairs"], "weapon_id", "evolution_pairs")
    require(set(evolutions) == set(unlocks), "every weapon requires one evolution pair")
    require(len({e["passive_id"] for e in evolutions.values()}) == 10, "passive pairs must be unique")
    require(len({e["evolved_weapon_id"] for e in evolutions.values()}) == 10, "evolved IDs must be unique")
    for weapon_id, expected in EXPECTED_EVOLUTIONS.items():
        evolution = evolutions[weapon_id]
        require((evolution["passive_id"], evolution["evolved_weapon_id"]) == expected,
                f"{weapon_id}: evolution pairing differs from design")
        integer(evolution["required_weapon_level"], f"{weapon_id}.required_weapon_level", 1)
        integer(evolution["required_passive_level"], f"{weapon_id}.required_passive_level", 1)
        require(evolution["required_weapon_level"] == 8 and evolution["required_passive_level"] == 1,
                f"{weapon_id}: invalid evolution levels")
        require(evolution["consumes_passive"] is False and evolution["replaces_weapon_slot"] is True,
                f"{weapon_id}: invalid evolution consumption/slot rule")

    rounds = unique(data["rounds"], "id", "rounds")
    require(set(rounds) == {f"genesis_{n:02}" for n in range(1, 10)}, "expected exactly nine rounds")
    all_wave_ids = set()
    for i in range(9):
        number = i + 1
        round_id = f"genesis_{number:02}"
        row = rounds[round_id]
        for field in ("number", "concurrency_cap", "spawn_budget", "raw_xp_budget", "completion_gold"):
            integer(row[field], f"{round_id}.{field}", 1)
        require(row["number"] == number and row["person_ko"] == EXPECTED_PERSONS[i], f"{round_id}: person/order mismatch")
        require(row["concurrency_cap"] == 40 + 10 * i, f"{round_id}: invalid concurrency cap")
        require(row["spawn_budget"] == EXPECTED_SPAWNS[i], f"{round_id}: spawn budget mismatch")
        require(row["raw_xp_budget"] == EXPECTED_XP[i], f"{round_id}: XP budget mismatch")
        require(row["completion_gold"] == EXPECTED_COMPLETION_GOLD[i], f"{round_id}: completion gold mismatch")
        expected_boss = next((b["id"] for b in bosses.values() if b["round_number"] == number), None)
        require(row["boss_id"] == expected_boss, f"{round_id}: boss cadence/reference mismatch")
        expected_unlock = next((key for key, value in EXPECTED_UNLOCKS.items() if value[0] == number), None)
        require(row["unlock_weapon_id"] == expected_unlock, f"{round_id}: unlock reference mismatch")
        require(len(row["waves"]) == 3, f"{round_id}: expected three waves")
        require(sum(w["spawn_budget"] for w in row["waves"]) == row["spawn_budget"], f"{round_id}: wave budgets do not sum")
        for j, wave in enumerate(row["waves"]):
            expected_id = f"{round_id}_w{j + 1}"
            require(wave["id"] == expected_id and expected_id not in all_wave_ids, f"{round_id}: duplicate/invalid wave ID")
            all_wave_ids.add(expected_id)
            require((wave["start_seconds"], wave["end_seconds"]) == [(0, 60), (60, 120), (120, 170)][j],
                    f"{expected_id}: invalid spawning window")
            integer(wave["start_seconds"], f"{expected_id}.start_seconds")
            integer(wave["end_seconds"], f"{expected_id}.end_seconds", 1)
            integer(wave["spawn_budget"], f"{expected_id}.spawn_budget", 1)
            require(wave["spawn_budget"] == EXPECTED_WAVE_COUNTS[i][j], f"{expected_id}: wrong wave budget")
            composition = unique(wave["composition"], "monster_id", f"{expected_id}.composition")
            for monster_id, entry in composition.items():
                require(monster_id in monsters, f"{expected_id}: unknown monster {monster_id}")
                integer(entry["count"], f"{expected_id}.{monster_id}.count", 1)
            require(sum(c["count"] for c in composition.values()) == wave["spawn_budget"],
                    f"{expected_id}: composition does not sum to budget")
            expected_composition = {
                f"{'abcdefghi'[i]}{k + 1}": wave["spawn_budget"] * percent // 100
                for k, percent in enumerate(EXPECTED_PERCENTAGES[i][j]) if percent
            }
            require(all(wave["spawn_budget"] * p % 100 == 0 for p in EXPECTED_PERCENTAGES[i][j]),
                    f"{expected_id}: non-integral source percentage")
            require({key: value["count"] for key, value in composition.items()} == expected_composition,
                    f"{expected_id}: composition differs from design")

    total_spawns = sum(r["spawn_budget"] for r in rounds.values())
    total_xp = sum(r["raw_xp_budget"] for r in rounds.values())
    total_gold = total_spawns // rules["kills_per_gold"] + sum(r["completion_gold"] for r in rounds.values()) + sum(b["gold"] for b in bosses.values())
    require((total_spawns, total_xp, total_gold) == (6210, 18600, 2667), "Genesis totals differ from design")
    return {"rounds": len(rounds), "monsters": len(monsters), "waves": len(all_wave_ids),
            "bosses": len(bosses), "spawn_budget": total_spawns, "raw_xp_budget": total_xp,
            "all_kills_base_gold": total_gold, "gameplay_verified": False}


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("path", nargs="?", type=Path,
                        default=Path(__file__).resolve().parents[1] / "content/genesis/catalog.json")
    args = parser.parse_args()
    try:
        data = json.loads(args.path.read_text(encoding="utf-8"), object_pairs_hook=no_duplicate_keys)
        summary = validate(data)
    except (OSError, ValueError, TypeError, KeyError, IndexError) as error:
        print(f"FAIL: {error}", file=sys.stderr)
        return 1
    print("PASS: Genesis design-data validation " + json.dumps(summary, ensure_ascii=False))
    return 0


if __name__ == "__main__":
    sys.exit(main())
