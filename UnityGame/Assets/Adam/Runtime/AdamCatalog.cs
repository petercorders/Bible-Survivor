using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BibleSurvivor.Core;
using UnityEngine;

namespace BibleSurvivor.Unity
{
    // Genesis and Exodus use JSON catalogs. The later books use the canonical Core definitions.
    public sealed class AdamCatalog
    {
        [Serializable] public class Root { public int schema_version; public string chapter_id; public Rules rules; public Monster[] monsters; public Round[] rounds; public Boss[] bosses; }
        [Serializable] public class Rules { public int normal_duration_seconds; public int spawn_cutoff_seconds; public int boss_intro_seconds,boss_support_cap; }
        [Serializable] public class Boss { public string id; public int round_number,max_health,contact_damage,gold; public double speed_ratio,radius,intro_seconds; public double[] intro_offset; public Attack attack; public Support[] support_summons; }
        [Serializable] public class Attack { public string pattern_id; public double cooldown_seconds,telegraph_seconds; public double period_seconds,duration_seconds,speed_ratio; public int damage,projectile_count,damage_packets,projectile_cap; public double projectile_speed,projectile_radius,projectile_lifetime,spread_degrees; }
        [Serializable] public class Support { public double health_ratio; public string monster_id; public int count; }
        [Serializable] public class Monster { public string id; public int max_health; public int contact_damage; public double speed_ratio; public string behavior_id; }
        [Serializable] public class Round { public int number; public int local_number; public string id; public string boss_id; public int concurrency_cap; public int raw_xp_budget; public int completion_gold; public int spawn_budget; public string unlock_weapon_id; public Wave[] waves; }
        [Serializable] public class Wave { public double start_seconds; public double end_seconds; public Group[] composition; }
        [Serializable] public class Group { public string monster_id; public int count; }
        readonly Dictionary<int,RoundDefinition> definitions=new Dictionary<int,RoundDefinition>();
        public RoundDefinition Definition => ForRound(1); // Existing R01 callers retain their contract.
        public IReadOnlyDictionary<string, double> Speeds { get; }
        readonly Dictionary<int,IReadOnlyDictionary<string,double>> exodusSpeeds=new Dictionary<int,IReadOnlyDictionary<string,double>>();
        public int LastInstalledRound { get; }
        public bool ExodusInstalled => definitions.ContainsKey(ExodusContent.GlobalRoundNumber);
        public bool LeviticusInstalled => definitions.ContainsKey(LeviticusContent.GlobalRoundNumber);
        public bool NumbersInstalled => definitions.ContainsKey(NumbersContent.GlobalRoundNumber);
        public IReadOnlyDictionary<string,double> SpeedsFor(int round)
        {
            ForRound(round);
            if(round>=LeviticusContent.GlobalRoundNumber)return CampaignContent.SpeedsFor(round);
            return round>=ExodusContent.GlobalRoundNumber?exodusSpeeds[round]:Speeds;
        }
        public RoundDefinition ForRound(int number)
        {
            if(!definitions.TryGetValue(number,out var value))throw new InvalidDataException("Round is outside installed content");
            return value;
        }
        public RoundDefinition ForCheckpoint(RunState run)
        {
            var playedRound=run.Stage=="between_rounds"?run.RoundNumber-1:run.RoundNumber;
            if(playedRound>=LeviticusContent.GlobalRoundNumber&&!definitions.ContainsKey(playedRound))
                throw new UnsupportedSaveVersionException();
            var supportedStage=run.Stage switch
            {
                "normal" or "between_rounds" => true,
                "round_settlement" => playedRound is 4 or 5 or 7 or 8 or 10 or 11 or 13 or 14 or 16 or 17 or 19 or 20 or 22 or 23 or 25 or 26 or 28 or 29,
                "boss_intro" or "boss" or "boss_reward" or "boss_settlement" => playedRound is 3 or 6 or 9 or 12 or 15 or 18 or 21 or 24 or 27 or 30,
                _ => false
            };
            if(!supportedStage)throw new InvalidDataException("Checkpoint is outside implemented content");
            return ForRound(playedRound);
        }
        public AdamCatalog(string json,string exodusJson=null,bool installLeviticus=false,bool installNumbers=false)
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
            if(exodusJson!=null)LoadExodus(exodusJson);
            if(installLeviticus && definitions.ContainsKey(ExodusContent.LastInstalledRound))
            {
                var last=installNumbers?CampaignContent.LastInstalledRound:LeviticusContent.LastInstalledRound;
                for(var round=LeviticusContent.GlobalRoundNumber;round<=last;round++)
                    definitions.Add(round,CampaignContent.Round(round));
            }
            LastInstalledRound=definitions.Keys.Max();
        }
        void LoadExodus(string json)
        {
            var root=JsonUtility.FromJson<Root>(json);
            if(root?.rules==null || root.schema_version!=1 || root.chapter_id!=ExodusContent.ChapterId || root.rules.normal_duration_seconds!=180 || root.rules.spawn_cutoff_seconds!=170 || root.monsters==null || root.rounds==null || root.rounds.Length<1 || root.rounds.Length>ExodusContent.LastInstalledRound-9)
                throw new InvalidDataException("Unsupported Exodus catalog");
            var monsters=root.monsters.ToDictionary(x=>x.id);
            var rounds=root.rounds.OrderBy(x=>x.number).ToArray();
            if(!rounds.Select(x=>x.number).SequenceEqual(Enumerable.Range(10,rounds.Length)) || root.monsters.Length!=rounds.Sum(x=>ExodusContent.SpeedsFor(x.number).Count))
                throw new InvalidDataException("Noncontiguous Exodus catalog or unexpected monsters");
            var installedBosses=rounds.Where(x=>x.number==12||x.number==15).Select(x=>x.number).ToArray();
            if((root.bosses?.Length??0)!=installedBosses.Length || root.bosses!=null&&!root.bosses.Select(x=>x.round_number).OrderBy(x=>x).SequenceEqual(installedBosses))throw new InvalidDataException("Unexpected Exodus boss set");
            foreach(var bossRound in installedBosses)
            {
                var rules=BossDefinition.ForRound(bossRound);var boss=root.bosses.Single(x=>x.round_number==bossRound);var attack=boss.attack;var support=boss.support_summons;
                if(boss.id!=rules.Id || boss.max_health!=rules.MaxHealth || boss.contact_damage!=rules.Contact || boss.gold!=rules.KillGold ||
                    boss.speed_ratio!=BossDefinition.Speed || boss.radius!=BossDefinition.Radius || boss.intro_seconds!=BossDefinition.IntroSeconds || boss.intro_offset==null || !boss.intro_offset.SequenceEqual(new[]{0d,BossDefinition.IntroOffset}) ||
                    attack==null || attack.period_seconds!=rules.AttackPeriod || attack.telegraph_seconds!=rules.TelegraphDuration ||
                    (rules.IsChariot?(attack.pattern_id!="absolute_fixed_direction_contact_dash" || attack.duration_seconds!=rules.DashDuration || attack.speed_ratio!=rules.DashSpeedRatio || attack.damage_packets!=0):
                    (attack.pattern_id!="three_fixed_direction_projectiles" || !rules.UsesChainProjectiles || attack.damage!=rules.AttackDamage || attack.projectile_speed!=rules.ProjectileSpeed || attack.projectile_radius!=rules.ProjectileRadius || attack.projectile_lifetime!=rules.ProjectileLifetime || attack.projectile_cap!=rules.ProjectileCap || attack.spread_degrees!=rules.ProjectileSpreadDegrees)) ||
                    support==null || support.Length!=rules.Thresholds.Count || support.Where((x,i)=>x.health_ratio!=rules.Thresholds[i] || x.monster_id!=rules.SupportMonster || x.count!=BossDefinition.SupportCount).Any() ||
                    !monsters.TryGetValue(rules.SupportMonster,out var helper) || helper.max_health!=rules.SupportHitPoints || helper.contact_damage!=rules.SupportContact || helper.speed_ratio!=rules.SupportRatio)
                    throw new InvalidDataException("Unsupported Exodus boss contract");
            }
            foreach(var round in rounds)
            {
                var expectedSpeeds=ExodusContent.SpeedsFor(round.number);
                foreach(var expected in expectedSpeeds)
                    if(!monsters.TryGetValue(expected.Key,out var monster) || monster.speed_ratio!=expected.Value || monster.behavior_id!=(BattleWorld.UsesNormalCharge(round.number,expected.Key)?"telegraphed_fixed_dash":"chase"))
                        throw new InvalidDataException("Unsupported Exodus monster speed/behavior");
                if(round.id!=ExodusContent.Round(round.number).Id || round.local_number!=round.number-9 || (round.number==12||round.number==15?round.boss_id!=BossDefinition.ForRound(round.number).Id:!string.IsNullOrEmpty(round.boss_id)) || !string.IsNullOrEmpty(round.unlock_weapon_id) || round.waves==null)
                    throw new InvalidDataException("Unsupported Exodus round identity");
                var waves=round.waves.Select(w=>new WaveDefinition(w.start_seconds,w.end_seconds,w.composition.Select(g=>
                {
                    if(!expectedSpeeds.ContainsKey(g.monster_id) || !monsters.TryGetValue(g.monster_id,out var monster))throw new InvalidDataException("Unknown Exodus round monster");
                    return new SpawnGroup(g.monster_id,g.count,monster.max_health,monster.contact_damage);
                }))).ToArray();
                if(waves.SelectMany(w=>w.Groups).Sum(g=>g.Count)!=round.spawn_budget)throw new InvalidDataException("Exodus budget mismatch");
                var definition=new RoundDefinition(round.number,round.concurrency_cap,round.raw_xp_budget,round.completion_gold,waves,round.id);
                ExodusContent.Validate(definition);definitions.Add(definition.Number,definition);
                exodusSpeeds.Add(round.number,expectedSpeeds.ToDictionary(x=>x.Key,x=>monsters[x.Key].speed_ratio));
            }
        }
        public static AdamCatalog Load()=>new AdamCatalog(Resources.Load<TextAsset>("genesis-catalog").text,Resources.Load<TextAsset>("exodus-catalog")?.text,true,true);
    }
}
