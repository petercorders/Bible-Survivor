using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BibleSurvivor.Core;
using UnityEngine;

namespace BibleSurvivor.Unity
{
    // Adapter reads the unchanged canonical catalog for the nine implemented rounds.
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
            if(!definitions.TryGetValue(number,out var value))throw new InvalidDataException("Only Genesis R01 through R09 are implemented; the next chapter is pending");
            return value;
        }
        public RoundDefinition ForCheckpoint(RunState run)
        {
            var boss=run.Stage=="boss_intro"||run.Stage=="boss"||run.Stage=="boss_reward"||run.Stage=="boss_settlement";
            var number=run.Stage=="normal"||run.Stage=="round_settlement"||boss?run.RoundNumber:run.Stage=="between_rounds"?run.RoundNumber-1:0;
            if((boss&&number!=3&&number!=6&&number!=9)||number<1||number>9 || (run.Stage=="round_settlement"&&number!=4&&number!=5&&number!=7&&number!=8))throw new InvalidDataException("Checkpoint is outside implemented content");
            return ForRound(number);
        }
        public AdamCatalog(string json)
        {
            var root=JsonUtility.FromJson<Root>(json);
            if(root?.rules==null || root.monsters==null || root.rounds==null || root.schema_version!=1 || root.chapter_id!="genesis" || root.rules.normal_duration_seconds!=180 || root.rules.spawn_cutoff_seconds!=170)
                throw new InvalidDataException("Unsupported Genesis catalog");
            var monsters=root.monsters.ToDictionary(x=>x.id);Speeds=root.monsters.ToDictionary(x=>x.id,x=>x.speed_ratio);
            foreach(var bossRound in new[]{3,6,9})
            {
                var rules=BossDefinition.ForRound(bossRound);var boss=root.bosses?.SingleOrDefault(x=>x.id==rules.Id);var attack=boss?.attack;var support=boss?.support_summons;
                if(boss==null||attack==null||support==null||boss.max_health!=rules.MaxHealth||boss.contact_damage!=rules.Contact||boss.gold!=rules.KillGold||
                    attack.cooldown_seconds!=rules.AttackPeriod||attack.telegraph_seconds!=rules.TelegraphDuration||attack.damage!=rules.AttackDamage||attack.projectile_count!=(bossRound==9?3:0)||attack.pattern_id!=(bossRound==3?"telegraphed_club_slam":bossRound==6?"telegraphed_dirt_throw":"three_way_chain_projectiles")||
                    support.Length!=rules.Thresholds.Count||support.Where((x,i)=>x.health_ratio!=rules.Thresholds[i]||x.monster_id!=rules.SupportMonster||x.count!=BossDefinition.SupportCount).Any()||root.rules.boss_intro_seconds!=BossDefinition.IntroSeconds||root.rules.boss_support_cap!=12||
                    monsters[rules.SupportMonster].max_health!=rules.SupportHitPoints||monsters[rules.SupportMonster].contact_damage!=rules.SupportContact||monsters[rules.SupportMonster].speed_ratio!=rules.SupportRatio)
                    throw new InvalidDataException("Unsupported boss catalog contract: "+rules.Id);
            }
            foreach(var number in new[]{1,2,3,4,5,6,7,8,9})
            {
                var round=root.rounds.Single(x=>x.number==number);
                if(round.id!="genesis_0"+number || ((number==3||number==6||number==9)?round.boss_id!=BossDefinition.ForRound(number).Id:!string.IsNullOrEmpty(round.boss_id)) || round.waves==null)throw new InvalidDataException("Invalid implemented round identity");
                var waves=round.waves.Select(w=>new WaveDefinition(w.start_seconds,w.end_seconds,w.composition.Select(g=>
                {
                    if(!monsters.TryGetValue(g.monster_id,out var monster))throw new InvalidDataException("Unknown round monster "+g.monster_id);
                    return new SpawnGroup(g.monster_id,g.count,monster.max_health,monster.contact_damage);
                }))).ToArray();
                if(waves.SelectMany(w=>w.Groups).Sum(g=>g.Count)!=round.spawn_budget)throw new InvalidDataException("Budget mismatch");
                definitions.Add(number,new RoundDefinition(number,round.concurrency_cap,round.raw_xp_budget,round.completion_gold,waves,round.id,string.IsNullOrEmpty(round.unlock_weapon_id)?null:round.unlock_weapon_id));
            }
        }
        public static AdamCatalog Load()=>new AdamCatalog(Resources.Load<TextAsset>("genesis-catalog").text);
    }
}
