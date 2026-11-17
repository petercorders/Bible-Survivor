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
        public const string SitnahPolicy="r06-production-v1";
        public const string JudahPolicy="r09-production-v1";
        public const double ChainSpeed=1.35,ChainRadius=.16,ChainLifetime=6,ChainAngleDegrees=20;
        public const int ChainCap=12;
        public static string ChainContract=>"three-chain-v1:"+string.Join(":",new[]{ChainSpeed,ChainRadius,ChainLifetime,ChainAngleDegrees,(double)ChainCap}.Select(x=>x.ToString("R",CultureInfo.InvariantCulture)))+":";
        public double TelegraphDuration=>Number==9?1:TelegraphSeconds;
        public int Number {get;}
        public string Id {get;}
        public double MaxHealth {get;}
        public int Contact {get;}
        public int KillGold {get;}
        public double AttackPeriod {get;}
        public int AttackDamage {get;}
        public int BossActorId {get;}
        public string SupportMonster {get;}
        public int SupportHitPoints {get;}
        public int SupportContact {get;}
        public double SupportRatio {get;}
        public string RewardPolicy {get;}
        public IReadOnlyList<double> Thresholds {get;}
        private BossDefinition(int number,string id,double hp,int contact,int gold,double period,int damage,int actor,string support,int supportHp,int supportDamage,double speed,string policy,params double[] thresholds)
        {Number=number;Id=id;MaxHealth=hp;Contact=contact;KillGold=gold;AttackPeriod=period;AttackDamage=damage;BossActorId=actor;SupportMonster=support;SupportHitPoints=supportHp;SupportContact=supportDamage;SupportRatio=speed;RewardPolicy=policy;Thresholds=Array.AsReadOnly(thresholds);}
        private static readonly BossDefinition AdamBoss=new BossDefinition(3,"nephilim_tyrant",Health,ContactDamage,Gold,Period,SlamDamage,ActorId,"c1",SupportHealth,SupportDamage,SupportSpeed,Policy,ThresholdRatio);
        private static readonly BossDefinition IsaacBoss=new BossDefinition(6,"sitnah_keeper",6500,16,200,7,18,781,"f1",80,8,.70,SitnahPolicy,.70,.35);
        private static readonly BossDefinition JudahBoss=new BossDefinition(9,"silver_cup_accuser",16000,20,350,5,15,1141,"i2",260,11,.60,JudahPolicy,.60,.30);
        public static BossDefinition ForRound(int number)=>number==3?AdamBoss:number==6?IsaacBoss:number==9?JudahBoss:throw new ArgumentException("No implemented boss for round");
        public string EncounterKey(string runId)=>runId+":genesis_"+Number.ToString("D2",CultureInfo.InvariantCulture)+":"+Id;
        public string FingerprintContract=>Number==3?Contract:Id+(Number==9?":"+ChainContract:":point-impact-v1:")+SupportMonster+":"+SupportCount.ToString(CultureInfo.InvariantCulture)+":"+RewardPolicy+":"+EvolutionRules.Contract+":"+
            string.Join(":",new[]{MaxHealth,(double)Contact,KillGold,AttackPeriod,AttackDamage,BossActorId,SupportHitPoints,SupportContact,SupportRatio,
                Speed,Radius,IntroSeconds,IntroOffset,TelegraphDuration,SlamRadius,SupportRadius,0,12}.Concat(Thresholds).Select(x=>x.ToString("R",CultureInfo.InvariantCulture)));
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
        [DataMember(EmitDefaultValue=false)] public int RoundNumber {get;set;}
        [DataMember(EmitDefaultValue=false)] public int SummonMask {get;set;}
        public int EncounterRound=>RoundNumber==0?3:RoundNumber;
        public BossDefinition Rules=>BossDefinition.ForRound(EncounterRound);
        [DataMember] public double NormalEndedAt {get;set;}
        [DataMember] public double IntroElapsed {get;set;}
        [DataMember] public double BossElapsed {get;set;}
        [DataMember] public double Health {get;set;}=BossDefinition.Health;
        [DataMember] public bool Paused {get;set;}
        [DataMember] public double NextAttackAt {get;set;}=BossDefinition.Period;
        [DataMember] public bool Telegraphing {get;set;}
        [DataMember] public float TargetX {get;set;}
        [DataMember] public float TargetY {get;set;}
        [DataMember(EmitDefaultValue=false)] public List<ChainDirectionState>? ChainDirections {get;set;}
        [DataMember] public bool ThresholdUsed {get;set;}
        [DataMember] public int Summoned {get;set;}
        [DataMember] public int NextActorId {get;set;}=482;
        [DataMember] public List<SpawnedEnemy> Supports {get;set;}=new List<SpawnedEnemy>();
        [DataMember] public bool BossGoldApplied {get;set;}
        [DataMember] public BossRewardState? Reward {get;set;}
        [DataMember(EmitDefaultValue=false)] public bool CompletionApplied {get;set;}
        public double CombatTime=>NormalEndedAt+IntroElapsed+BossElapsed;
        internal BossEncounterState Copy()
        {var copy=(BossEncounterState)MemberwiseClone();copy.ChainDirections=ChainDirections?.Select(x=>x.Copy()).ToList();copy.Supports=Supports.Select(x=>x.Copy()).ToList();copy.Reward=Reward?.Copy();return copy;}
        internal void Validate(RunState run)
        {
            bool Finite(double n)=>BattleSnapshot.Finite(n);
            if(Version>3||EncounterRound!=3&&EncounterRound!=6&&EncounterRound!=9)throw new UnsupportedSaveVersionException();
            var rules=Rules;var mask=rules.Number==3?(ThresholdUsed?1:0):SummonMask;
            var used=(mask&1)+((mask>>1)&1);var maxMask=(1<<rules.Thresholds.Count)-1;
            if((Version!=1&&Version!=2&&Version!=3)||(rules.Number==6&&Version!=2)||(rules.Number==9&&Version!=3)||(rules.Number!=9&&Version==3)||EncounterId!=rules.EncounterKey(run.RunId)||!Finite(NormalEndedAt)||NormalEndedAt<0||NormalEndedAt>180||
                !Finite(IntroElapsed)||IntroElapsed<0||IntroElapsed>2||!Finite(BossElapsed)||BossElapsed<0||
                !Finite(CombatTime)||!Finite(Health)||Health<0||Health>rules.MaxHealth||!Finite(NextAttackAt)||NextAttackAt<rules.AttackPeriod||
                Math.Abs(NextAttackAt/rules.AttackPeriod-Math.Round(NextAttackAt/rules.AttackPeriod))>1e-7||NextAttackAt<=BossElapsed||NextAttackAt>BossElapsed+rules.AttackPeriod+1e-7||
                !BattleSnapshot.Position(TargetX,TargetY)||Supports==null||Supports.Any(x=>x==null)||mask<0||mask>maxMask||(mask==2)||
                (rules.Number==3&&SummonMask!=0)||(rules.Number!=3&&ThresholdUsed!=(mask!=0))||Summoned!=used*BossDefinition.SupportCount||NextActorId!=rules.BossActorId+1+Summoned||Supports.Count>Summoned||
                Supports.Select(x=>x.Id).Distinct().Count()!=Supports.Count||Supports.Any(x=>x.Id<=rules.BossActorId||x.Id>=NextActorId||x.MonsterId!=rules.SupportMonster||
                    !Finite(x.Health)||x.Health<=0||x.Health>rules.SupportHitPoints||x.ContactDamage!=rules.SupportContact||x.RawExperience!=0)||run.LastHitTime>CombatTime)
                throw new InvalidDataException("Invalid boss encounter");
            if(rules.Number==9)
            {
                if(ChainDirections==null||ChainDirections.Count!=0&&ChainDirections.Count!=3||ChainDirections.Any(x=>x==null))throw new InvalidDataException("Invalid chain telegraph directions");
                foreach(var direction in ChainDirections)direction.Validate();
                if(ChainDirections.Count==3)
                    for(var i=0;i<3;i++)
                    {
                        var angle=(i-1)*BossDefinition.ChainAngleDegrees*Math.PI/180;var center=ChainDirections[1];
                        if(Math.Abs(ChainDirections[i].X-(center.X*Math.Cos(angle)-center.Y*Math.Sin(angle)))>.0001||Math.Abs(ChainDirections[i].Y-(center.X*Math.Sin(angle)+center.Y*Math.Cos(angle)))>.0001)throw new InvalidDataException("Invalid chain spread");
                    }
                if(ChainDirections.Count!=(BossElapsed+1e-9>=rules.AttackPeriod-rules.TelegraphDuration?3:0))throw new InvalidDataException("Chain directions outside attack clock");
            }
            else if(ChainDirections!=null)throw new InvalidDataException("Chain directions outside R09");
            if(run.Stage=="boss_intro")
            {
                if(run.RoundNumber!=rules.Number||TargetX!=0||TargetY!=0||IntroElapsed>=2||BossElapsed!=0||Health!=rules.MaxHealth||Summoned!=0||CompletionApplied||BossGoldApplied||Reward!=null||Telegraphing||NextAttackAt!=rules.AttackPeriod||run.Drops.Count!=0)
                    throw new InvalidDataException("Invalid protected intro");
            }
            else if(run.Stage=="boss")
            {
                if(run.RoundNumber!=rules.Number||IntroElapsed!=2||Health<=0||CompletionApplied||BossGoldApplied||Reward!=null||Telegraphing!=(BossElapsed+1e-9>=NextAttackAt-rules.TelegraphDuration)||
                    rules.Thresholds.Where((ratio,index)=>((mask>>index)&1)!=(Health<=rules.MaxHealth*ratio?1:0)).Any()||
                    run.Drops.Any(x=>x.Key<=rules.BossActorId||x.Key>=NextActorId||x.Value!=0||Supports.Any(s=>s.Id==x.Key)))throw new InvalidDataException("Invalid boss combat lifecycle");
            }
            else if(run.Stage=="boss_reward"||run.Stage=="boss_settlement"||run.Stage=="between_rounds")
            {
                if(IntroElapsed!=2||Health!=0||!BossGoldApplied||Reward==null||Supports.Count!=0||run.Drops.Count!=0||Telegraphing||
                    Reward.RewardId!=EncounterId+":reward"||Reward.Weapons==null||Reward.Passives==null||Reward.EligibleEvolutions==null||
                    !Reward.EligibleEvolutions.SequenceEqual(EvolutionRules.Eligibility(Reward.Weapons,Reward.Passives,Reward.Forms))||
                    (run.Stage=="boss_reward")==Reward.Applied||CompletionApplied!=(run.Stage=="between_rounds")||
                    run.RoundNumber!=(run.Stage=="between_rounds"?rules.Number+1:rules.Number)||
                    (run.Stage=="between_rounds"&&(run.PendingLevels!=0||run.Health!=run.MaxHealth))||
                    (Reward.PolicyId==BossDefinition.LegacyPolicy?(rules.Number!=3||Version!=1):(Version!=(rules.Number==9?3:2)||Reward.PolicyId!=rules.RewardPolicy)))
                    throw new InvalidDataException("Invalid boss reward/settlement receipt");
                BossRewards.Validate(Reward,run,CompletionApplied);
            }
            else throw new InvalidDataException("Boss state outside boss lifecycle");
        }
    }
}
