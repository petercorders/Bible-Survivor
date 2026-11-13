using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BibleSurvivor.Core;
using UnityEngine;

namespace BibleSurvivor.Unity
{
    // JsonUtility field DTOs read the generated copy of the single canonical catalog.
    public sealed class AdamCatalog
    {
        [Serializable] public class Root { public int schema_version; public string chapter_id; public Rules rules; public Monster[] monsters; public Round[] rounds; }
        [Serializable] public class Rules { public int normal_duration_seconds; public int spawn_cutoff_seconds; }
        [Serializable] public class Monster { public string id; public int max_health; public int contact_damage; public double speed_ratio; }
        [Serializable] public class Round { public int number; public string id; public string boss_id; public int concurrency_cap; public int raw_xp_budget; public int completion_gold; public int spawn_budget; public string unlock_weapon_id; public Wave[] waves; }
        [Serializable] public class Wave { public double start_seconds; public double end_seconds; public Group[] composition; }
        [Serializable] public class Group { public string monster_id; public int count; }
        public RoundDefinition Definition { get; }
        public IReadOnlyDictionary<string, double> Speeds { get; }
        public AdamCatalog(string json)
        {
            var root = JsonUtility.FromJson<Root>(json);
            if (root.schema_version != 1 || root.chapter_id != "genesis" || root.rules.normal_duration_seconds != 180 || root.rules.spawn_cutoff_seconds != 170)
                throw new InvalidDataException("Unsupported Genesis catalog");
            var monsters = root.monsters.ToDictionary(x => x.id);
            Speeds = root.monsters.ToDictionary(x => x.id, x => x.speed_ratio);
            var round = root.rounds.Single(x => x.number == 1);
            if (round.id != "genesis_01" || !string.IsNullOrEmpty(round.boss_id)) throw new InvalidDataException("Only Adam is supported");
            var waves = round.waves.Select(w => new WaveDefinition(w.start_seconds, w.end_seconds,
                w.composition.Select(g => new SpawnGroup(g.monster_id, g.count, monsters[g.monster_id].max_health, monsters[g.monster_id].contact_damage)))).ToArray();
            if (waves.SelectMany(w => w.Groups).Sum(g => g.Count) != round.spawn_budget) throw new InvalidDataException("Budget mismatch");
            Definition = new RoundDefinition(1, round.concurrency_cap, round.raw_xp_budget, round.completion_gold, waves, round.id, round.unlock_weapon_id);
        }
        public static AdamCatalog Load() => new AdamCatalog(Resources.Load<TextAsset>("genesis-catalog").text);
    }
}
