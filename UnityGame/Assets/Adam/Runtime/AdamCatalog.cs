using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BibleSurvivor.Core;
using UnityEngine;

namespace BibleSurvivor.Unity
{
    // Adapter reads the unchanged canonical catalog for the three implemented rounds.
    public sealed class AdamCatalog
    {
        [Serializable] public class Root { public int schema_version; public string chapter_id; public Rules rules; public Monster[] monsters; public Round[] rounds; public Boss[] bosses; }
        [Serializable] public class Rules { public int normal_duration_seconds; public int spawn_cutoff_seconds; public int boss_intro_seconds,boss_support_cap; }
        [Serializable] public class Boss { public string id; public int max_health,contact_damage,gold; public Attack attack; public Support[] support_summons; }
        [Serializable] public class Attack { public string pattern_id; public double cooldown_seconds,telegraph_seconds; public int damage,projectile_count; }
        [Serializable] public class Support { public double health_ratio; public string monster_id; public int count; }
        [Serializable] public class Monster { public string id; public int max_health; public int contact_damage; public double speed_ratio; }
        [Serializable] public class Round { public int number; public string id; public string boss_id; public int concurrency_cap; public int raw_xp_budget; public int completion_gold; public int spawn_budget; public string unlock_weapon_id; public Wave[] waves; }
        [Serializable] public class Wave { public double start_seconds; public double end_seconds; public Group[] composition; }
        [Serializable] public class Group { public string monster_id; public int count; }
        readonly Dictionary<int,RoundDefinition> definitions=new Dictionary<int,RoundDefinition>();
        public RoundDefinition Definition => ForRound(1); // Existing R01 callers retain their contract.
        public IReadOnlyDictionary<string, double> Speeds { get; }
        public RoundDefinition ForRound(int number)
        {
            if(!definitions.TryGetValue(number,out var value))throw new InvalidDataException("Only R01/R02/R03 are implemented; R04 is pending");
            return value;
        }
        public RoundDefinition ForCheckpoint(RunState run)
        {
            var boss=run.Stage=="boss_intro"||run.Stage=="boss"||run.Stage=="boss_reward"||run.Stage=="boss_settlement";
            var number=run.Stage=="normal"||boss?run.RoundNumber:run.Stage=="between_rounds"?run.RoundNumber-1:0;
            if((boss&&number!=3)||number<1||number>3)throw new InvalidDataException("Checkpoint is outside implemented content");
            return ForRound(number);
        }
        public AdamCatalog(string json)
        {
            var root=JsonUtility.FromJson<Root>(json);
            if(root?.rules==null || root.monsters==null || root.rounds==null || root.schema_version!=1 || root.chapter_id!="genesis" || root.rules.normal_duration_seconds!=180 || root.rules.spawn_cutoff_seconds!=170)
                throw new InvalidDataException("Unsupported Genesis catalog");
            var monsters=root.monsters.ToDictionary(x=>x.id);Speeds=root.monsters.ToDictionary(x=>x.id,x=>x.speed_ratio);
            var boss=root.bosses?.SingleOrDefault(x=>x.id=="nephilim_tyrant");var attack=boss?.attack;var support=boss?.support_summons?.SingleOrDefault();
            if(boss==null||attack==null||support==null||boss.max_health!=BossDefinition.Health||boss.contact_damage!=BossDefinition.ContactDamage||boss.gold!=BossDefinition.Gold||
                attack.cooldown_seconds!=BossDefinition.Period||attack.telegraph_seconds!=BossDefinition.TelegraphSeconds||attack.damage!=BossDefinition.SlamDamage||attack.projectile_count!=0||attack.pattern_id!="telegraphed_club_slam"||
                support.health_ratio!=BossDefinition.ThresholdRatio||support.monster_id!="c1"||support.count!=BossDefinition.SupportCount||root.rules.boss_intro_seconds!=BossDefinition.IntroSeconds||root.rules.boss_support_cap!=12||
                monsters["c1"].max_health!=BossDefinition.SupportHealth||monsters["c1"].contact_damage!=BossDefinition.SupportDamage||monsters["c1"].speed_ratio!=BossDefinition.SupportSpeed)
                throw new InvalidDataException("Unsupported first boss catalog contract");
            foreach(var number in new[]{1,2,3})
            {
                var round=root.rounds.Single(x=>x.number==number);
                if(round.id!="genesis_0"+number || (number==3?round.boss_id!="nephilim_tyrant":!string.IsNullOrEmpty(round.boss_id)) || round.waves==null)throw new InvalidDataException("Invalid implemented round identity");
                var waves=round.waves.Select(w=>new WaveDefinition(w.start_seconds,w.end_seconds,w.composition.Select(g=>
                {
                    if(!monsters.TryGetValue(g.monster_id,out var monster))throw new InvalidDataException("Unknown round monster "+g.monster_id);
                    return new SpawnGroup(g.monster_id,g.count,monster.max_health,monster.contact_damage);
                }))).ToArray();
                if(waves.SelectMany(w=>w.Groups).Sum(g=>g.Count)!=round.spawn_budget)throw new InvalidDataException("Budget mismatch");
                definitions.Add(number,new RoundDefinition(number,round.concurrency_cap,round.raw_xp_budget,round.completion_gold,waves,round.id,round.unlock_weapon_id));
            }
        }
        public static AdamCatalog Load()=>new AdamCatalog(Resources.Load<TextAsset>("genesis-catalog").text);
    }
}
