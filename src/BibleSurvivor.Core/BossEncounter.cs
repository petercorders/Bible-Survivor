using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Globalization;
using System.Runtime.Serialization;

namespace BibleSurvivor.Core
{
    // Approved technical tuning v1; not a claim of measured gameplay balance.
    public sealed class BossDefinition
    {
        public const string LegacyPolicy = "r03-production-pending-v1";
        public const string Policy = "r03-production-v2";
        public static string LegacyContract => "nephilim-v1:stopped:fixed:c1:" + string.Join(":",new double[]{Health,ContactDamage,Gold,Period,
            TelegraphSeconds,SlamDamage,0,IntroSeconds,ThresholdRatio,SupportCount,12,SupportHealth,SupportDamage,SupportSpeed,0,
            Speed,Radius,0,IntroOffset,SlamRadius,SupportRadius}.Select(x=>x.ToString("R",CultureInfo.InvariantCulture))) + ":" + LegacyPolicy;
        public static string Contract=>LegacyContract+":"+Policy+":"+EvolutionRules.Contract;
        public const int ActorId = 481;
        public const double Health = 1800, Speed = .30, Radius = .60, IntroSeconds = 2, Period = 6, TelegraphSeconds = 1.2, SlamRadius = 1.20;
        public const int ContactDamage = 12, SlamDamage = 15, Gold = 100, SupportCount = 6, SupportHealth = 30, SupportDamage = 6;
        public const double SupportSpeed = .55, SupportRadius = 1.5, IntroOffset = 3, ThresholdRatio = .5;
        public static string EncounterId(string runId) => runId + ":genesis_03:nephilim_tyrant";
    }

    [DataContract]
    public sealed class BossRewardState
    {
        [DataMember] public string RewardId {get;set;}="";
        [DataMember] public string PolicyId {get;set;}=BossDefinition.Policy;
        // Frozen eligibility inputs and stable candidates survive retry/reload.
        [DataMember] public Dictionary<string,int> Weapons {get;set;}=new Dictionary<string,int>();
        [DataMember] public Dictionary<string,int> Passives {get;set;}=new Dictionary<string,int>();
        [DataMember] public List<string> EligibleEvolutions {get;set;}=new List<string>();
        [DataMember] public bool Applied {get;set;}
        [DataMember(EmitDefaultValue=false)] public Dictionary<string,string>? Forms {get;set;}
        [DataMember(EmitDefaultValue=false)] public List<string>? UnlockedWeapons {get;set;}
        [DataMember(EmitDefaultValue=false)] public List<BossRewardCandidate>? Candidates {get;set;}
        [DataMember(EmitDefaultValue=false)] public string? SelectedId {get;set;}
        [DataMember(EmitDefaultValue=false)] public string? PromotedFrom {get;set;}
        [DataMember(EmitDefaultValue=false)] public bool GoldApplied {get;set;}
        internal BossRewardState Copy()=>new BossRewardState{RewardId=RewardId,PolicyId=PolicyId,Weapons=new Dictionary<string,int>(Weapons),
            Passives=new Dictionary<string,int>(Passives),EligibleEvolutions=new List<string>(EligibleEvolutions),Applied=Applied,
            Forms=Forms==null?null:new Dictionary<string,string>(Forms),UnlockedWeapons=UnlockedWeapons?.ToList(),Candidates=Candidates?.Select(x=>x.Copy()).ToList(),
            SelectedId=SelectedId,PromotedFrom=PromotedFrom,GoldApplied=GoldApplied};
        internal static List<string> Eligibility(IReadOnlyDictionary<string,int> weapons,IReadOnlyDictionary<string,int> passives)
            =>EvolutionRules.Eligibility(weapons,passives,null);
    }

    [DataContract]
    public sealed class BossEncounterState
    {
        [DataMember] public int Version {get;set;}=2;
        [DataMember] public string EncounterId {get;set;}="";
        [DataMember] public double NormalEndedAt {get;set;}
        [DataMember] public double IntroElapsed {get;set;}
        [DataMember] public double BossElapsed {get;set;}
        [DataMember] public double Health {get;set;}=BossDefinition.Health;
        [DataMember] public bool Paused {get;set;}
        [DataMember] public double NextAttackAt {get;set;}=BossDefinition.Period;
        [DataMember] public bool Telegraphing {get;set;}
        [DataMember] public float TargetX {get;set;}
        [DataMember] public float TargetY {get;set;}
        [DataMember] public bool ThresholdUsed {get;set;}
        [DataMember] public int Summoned {get;set;}
        [DataMember] public int NextActorId {get;set;}=482;
        [DataMember] public List<SpawnedEnemy> Supports {get;set;}=new List<SpawnedEnemy>();
        [DataMember] public bool BossGoldApplied {get;set;}
        [DataMember] public BossRewardState? Reward {get;set;}
        [DataMember(EmitDefaultValue=false)] public bool CompletionApplied {get;set;}
        public double CombatTime=>NormalEndedAt+IntroElapsed+BossElapsed;
        internal BossEncounterState Copy()
        {var copy=(BossEncounterState)MemberwiseClone();copy.Supports=Supports.Select(x=>x.Copy()).ToList();copy.Reward=Reward?.Copy();return copy;}
        internal void Validate(RunState run)
        {
            bool Finite(double n)=>BattleSnapshot.Finite(n);
            if(Version>2)throw new UnsupportedSaveVersionException();
            if((Version!=1&&Version!=2)||EncounterId!=BossDefinition.EncounterId(run.RunId)||!Finite(NormalEndedAt)||NormalEndedAt<0||NormalEndedAt>180||
                !Finite(IntroElapsed)||IntroElapsed<0||IntroElapsed>2||!Finite(BossElapsed)||BossElapsed<0||
                !Finite(CombatTime)||!Finite(Health)||Health<0||Health>1800||!Finite(NextAttackAt)||NextAttackAt<6||
                Math.Abs(NextAttackAt/6-Math.Round(NextAttackAt/6))>1e-7||NextAttackAt<=BossElapsed||NextAttackAt>BossElapsed+6+1e-7||
                !BattleSnapshot.Position(TargetX,TargetY)||Supports==null||Supports.Any(x=>x==null)||
                (Summoned!=0&&Summoned!=6)||ThresholdUsed!=(Summoned==6)||NextActorId!=482+Summoned||Supports.Count>Summoned||
                Supports.Select(x=>x.Id).Distinct().Count()!=Supports.Count||Supports.Any(x=>x.Id<482||x.Id>=NextActorId||x.MonsterId!="c1"||
                    !Finite(x.Health)||x.Health<=0||x.Health>30||x.ContactDamage!=6||x.RawExperience!=0)||run.LastHitTime>CombatTime)
                throw new InvalidDataException("Invalid boss encounter");
            if(run.Stage=="boss_intro")
            {
                if(TargetX!=0||TargetY!=0||IntroElapsed>=2||BossElapsed!=0||Health!=1800||Summoned!=0||CompletionApplied||BossGoldApplied||Reward!=null||Telegraphing||NextAttackAt!=6||run.Drops.Count!=0)
                    throw new InvalidDataException("Invalid protected intro");
            }
            else if(run.Stage=="boss")
            {
                if(IntroElapsed!=2||Health<=0||CompletionApplied||BossGoldApplied||Reward!=null||Telegraphing!=(BossElapsed+1e-9>=NextAttackAt-1.2)||
                    (!ThresholdUsed&&Health<=900)||(ThresholdUsed&&Health>900)||run.Drops.Any(x=>x.Key<482||x.Key>=NextActorId||x.Value!=0||Supports.Any(s=>s.Id==x.Key)))
                    throw new InvalidDataException("Invalid boss combat lifecycle");
            }
            else if(run.Stage=="boss_reward"||run.Stage=="boss_settlement"||run.Stage=="between_rounds")
            {
                if(IntroElapsed!=2||Health!=0||!BossGoldApplied||Reward==null||Supports.Count!=0||run.Drops.Count!=0||Telegraphing||
                    Reward.RewardId!=EncounterId+":reward"||Reward.Weapons==null||Reward.Passives==null||Reward.EligibleEvolutions==null||
                    !Reward.EligibleEvolutions.SequenceEqual(EvolutionRules.Eligibility(Reward.Weapons,Reward.Passives,Reward.Forms))||
                    (run.Stage=="boss_reward")==Reward.Applied||CompletionApplied!=(run.Stage=="between_rounds")||
                    (run.Stage=="between_rounds"&&(run.RoundNumber!=4||run.PendingLevels!=0||run.Health!=run.MaxHealth))||
                    (Reward.PolicyId==BossDefinition.LegacyPolicy?Version!=1:Version!=2))
                    throw new InvalidDataException("Invalid boss reward/settlement receipt");
                BossRewards.Validate(Reward,run,CompletionApplied);
            }
            else throw new InvalidDataException("Boss state outside boss lifecycle");
        }
    }
}
