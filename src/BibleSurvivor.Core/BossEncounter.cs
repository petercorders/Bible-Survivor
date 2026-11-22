using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.IO;
using System.Globalization;
using System.Runtime.Serialization;

namespace BibleSurvivor.Core
{
    [Flags]
    public enum BossActions
    {
        None = 0,
        Dash = 1,
        Slam = 2,
        Chains = 4,
        Summon = 8,
        AimedShot = 16,
        DamageZone = 32
    }

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
        public const string ChariotPolicy="r12-production-v1";
        public const string WeaverPolicy="r15-production-v1";
        public const string AshVeilPolicy="r18-production-v1";
        internal const string OathPolicy="r21-production-v1",DustPolicy="r24-production-v1",
            RavinePolicy="r27-production-v1",WardenPolicy="r30-production-v1",
            OblivionPolicy="r33-production-v1",CrossroadsPolicy="r36-production-v1",RampartPolicy="r39-production-v1",
            BoundaryPolicy="r42-production-v1",TorrentPolicy="r45-production-v1",FordPolicy="r48-production-v1",
            PillarPolicy="r51-production-v1",ChaffPolicy="r54-production-v1",SwarmPolicy="r57-production-v1",ValleyPolicy="r60-production-v1",
            PalacePolicy="r63-production-v1",FieldPolicy="r66-production-v1",AltarPolicy="r69-production-v1",
            VineyardPolicy="r72-production-v1",DothanPolicy="r75-production-v1",BullPolicy="r78-production-v1",
            GateLionPolicy="r81-production-v1",RaiderPolicy="r84-production-v1",BeracahPolicy="r87-production-v1",HookPolicy="r90-production-v1",
            AmbushPolicy="r93-production-v1",OnoPolicy="r96-production-v1",PurPolicy="r99-production-v1",
            TerrorKingPolicy="r102-production-v1",RoaringWatersPolicy="r105-production-v1",NightTerrorPolicy="r108-production-v1",
            VeilingDarknessPolicy="r111-production-v1",LeechPolicy="r114-production-v1",
            TroubleCloudsPolicy="r117-production-v1",LeopardPeaksPolicy="r120-production-v1",
            RuinsBeastPolicy="r123-production-v1",LeviathanShadowPolicy="r126-production-v1",ViperWebPolicy="r129-production-v1",
            SurroundingTerrorPolicy="r132-production-v1",CisternMudPolicy="r135-production-v1",DestroyingMountainPolicy="r138-production-v1",
            WormwoodGallPolicy="r141-production-v1",SoulSnarePolicy="r144-production-v1",
            HighCedarPolicy="r147-production-v1",GogHostPolicy="r150-production-v1",
            DuraImagePolicy="r153-production-v1",FourthBeastPolicy="r156-production-v1",PersianPrincePolicy="r159-production-v1",
            DeathSheolPolicy="r162-production-v1",HighEaglePolicy="r165-production-v1",LingeringAngerPolicy="r168-production-v1",
            TorchChariotPolicy="r171-production-v1",EmptyFoldPolicy="r174-production-v1",
            AccusingShadowPolicy="r177-production-v1",DevourerPolicy="r180-production-v1";
        // Bosses from R21 onward are stored in the ordered later history and must match their own round.
        internal static bool IsExpansionPolicy(string policy) => policy is OathPolicy or DustPolicy or RavinePolicy or WardenPolicy
            or OblivionPolicy or CrossroadsPolicy or RampartPolicy or BoundaryPolicy or TorrentPolicy or FordPolicy
            or PillarPolicy or ChaffPolicy or SwarmPolicy or ValleyPolicy or PalacePolicy or FieldPolicy or AltarPolicy or VineyardPolicy or DothanPolicy or BullPolicy or GateLionPolicy or RaiderPolicy or BeracahPolicy or HookPolicy or AmbushPolicy or OnoPolicy or PurPolicy
            or TerrorKingPolicy or RoaringWatersPolicy or NightTerrorPolicy
            or VeilingDarknessPolicy or LeechPolicy or TroubleCloudsPolicy or LeopardPeaksPolicy
            or RuinsBeastPolicy or LeviathanShadowPolicy or ViperWebPolicy
            or SurroundingTerrorPolicy or CisternMudPolicy or DestroyingMountainPolicy
            or WormwoodGallPolicy or SoulSnarePolicy or HighCedarPolicy or GogHostPolicy
            or DuraImagePolicy or FourthBeastPolicy or PersianPrincePolicy
            or DeathSheolPolicy or HighEaglePolicy or LingeringAngerPolicy
            or TorchChariotPolicy or EmptyFoldPolicy or AccusingShadowPolicy or DevourerPolicy;
        internal static bool IsProductionPolicy(string policy) => policy is Policy or SitnahPolicy or JudahPolicy
            or ChariotPolicy or WeaverPolicy or AshVeilPolicy || IsExpansionPolicy(policy);
        public BossActions Actions {get;}
        public bool UsesChainProjectiles=>(Actions&BossActions.Chains)!=0;
        public bool UsesDash=>(Actions&BossActions.Dash)!=0;
        public bool UsesSlam=>(Actions&BossActions.Slam)!=0;
        public bool SummonsSupport=>(Actions&BossActions.Summon)!=0;
        public bool UsesAimedShot => (Actions & BossActions.AimedShot) != 0;
        public bool UsesDamageZone => (Actions & BossActions.DamageZone) != 0;
        public const double AimedShotSpeed = 6, AimedShotRadius = .18, AimedShotLifetime = 2;
        public const double DamageZoneRadius = 1.5, DamageZoneLifetime = 2;
        internal bool HasTimedAttack=>UsesDash||UsesSlam||UsesChainProjectiles||UsesAimedShot||UsesDamageZone;
        internal const int LatestEncounterVersion = 5;
        internal int EncounterVersion=>UsesAimedShot||UsesDamageZone?LatestEncounterVersion:UsesDash?4:UsesChainProjectiles?3:2;
        // Combined geometry fits the existing World4 layout. The dash clock lives in Boss.
        internal int WorldVersion=>UsesChainProjectiles?4:UsesDash?5:Number>=4?3:2;
        public double ProjectileSpeed=>UsesChainProjectiles?ChainSpeed:0;
        public double ProjectileRadius=>UsesChainProjectiles?ChainRadius:0;
        public double ProjectileLifetime=>UsesChainProjectiles?ChainLifetime:0;
        public int ProjectileCap=>UsesChainProjectiles?ChainCap:0;
        public double ProjectileSpreadDegrees=>UsesChainProjectiles?ChainAngleDegrees:0;
        public const double ChariotDashDuration=.6, ChariotDashSpeed=1.3;
        public bool IsChariot=>Number==12;
        public double DashDuration=>UsesDash?ChariotDashDuration:0;
        public double DashSpeedRatio=>UsesDash?ChariotDashSpeed:0;
        public const double ChainSpeed=1.35,ChainRadius=.16,ChainLifetime=6,ChainAngleDegrees=20;
        public const int ChainCap=12;
        public static string ChainContract=>"three-chain-v1:"+string.Join(":",new[]{ChainSpeed,ChainRadius,ChainLifetime,ChainAngleDegrees,(double)ChainCap}.Select(x=>x.ToString("R",CultureInfo.InvariantCulture)))+":";
        public double TelegraphDuration=>UsesChainProjectiles&&!UsesDash&&!UsesAimedShot&&!UsesDamageZone?1:TelegraphSeconds;
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
        private string? experimentalRoundId;
        private BossDefinition(int number,string id,double hp,int contact,int gold,double period,int damage,int actor,string support,int supportHp,int supportDamage,double speed,string policy,BossActions actions,params double[] thresholds)
        {
            Actions=actions;
            if ((UsesAimedShot && period - TelegraphDuration <= AimedShotLifetime) ||
                (UsesDamageZone && period - TelegraphDuration <= DamageZoneLifetime))
                throw new ArgumentOutOfRangeException(nameof(period), "Hazards must expire before the next warning.");
            Number=number;Id=id;MaxHealth=hp;Contact=contact;KillGold=gold;AttackPeriod=period;AttackDamage=damage;BossActorId=actor;SupportMonster=support;SupportHitPoints=supportHp;SupportContact=supportDamage;SupportRatio=speed;RewardPolicy=policy;Thresholds=Array.AsReadOnly(SummonsSupport?thresholds:Array.Empty<double>());}
        private static readonly BossDefinition AdamBoss=new BossDefinition(3,"nephilim_tyrant",Health,ContactDamage,Gold,Period,SlamDamage,ActorId,"c1",SupportHealth,SupportDamage,SupportSpeed,Policy,BossActions.Slam|BossActions.Summon,ThresholdRatio);
        private static readonly BossDefinition IsaacBoss=new BossDefinition(6,"sitnah_keeper",6500,16,200,7,18,781,"f1",80,8,.70,SitnahPolicy,BossActions.Slam|BossActions.Summon,.70,.35);
        private static readonly BossDefinition JudahBoss=new BossDefinition(9,"silver_cup_accuser",16000,20,350,5,15,1141,"i2",260,11,.60,JudahPolicy,BossActions.Chains|BossActions.Summon,.60,.30);
        private static readonly BossDefinition MosesBoss=new BossDefinition(12,"oppression_chariot",20000,20,400,6,0,1021,"ex8",360,14,.45,ChariotPolicy,BossActions.Dash|BossActions.Summon,.60,.30);
        private static readonly BossDefinition BezalelBoss=new BossDefinition(15,"defilement_weaver",26000,22,500,5,18,1201,"ex17",400,14,.65,WeaverPolicy,BossActions.Chains|BossActions.Summon,.60,.30);
        private static readonly BossDefinition AshVeilBoss=new BossDefinition(18,"ash_veil",31000,24,560,5,20,1381,"lv1",350,14,.60,AshVeilPolicy,BossActions.Chains|BossActions.Summon,.60,.30);
        private static readonly BossDefinition OathBoss=new BossDefinition(21,"oath_wraith",33200,25,620,5,21,1561,"lv1",425,14,.60,OathPolicy,BossActions.Chains|BossActions.Summon,.60,.30);
        private static readonly BossDefinition DustBoss=new BossDefinition(24,"dust_tyrant",35500,26,680,5,22,1681,"nm1",480,15,.63,DustPolicy,BossActions.Chains|BossActions.Summon,.65,.30);
        private static readonly BossDefinition RavineBoss=new BossDefinition(27,"ravine_stalker",38000,27,740,5,23,1861,"nm1",540,15,.63,RavinePolicy,BossActions.Chains|BossActions.Summon,.65,.35);
        private static readonly BossDefinition WardenBoss=new BossDefinition(30,"wilderness_warden",41000,29,820,5,24,2041,"nm1",600,15,.63,WardenPolicy,BossActions.Chains|BossActions.Summon,.70,.35);
        private static readonly BossDefinition OblivionBoss=new BossDefinition(33,"oblivion_mist",44000,30,880,5,25,2221,"dt1",660,16,.65,OblivionPolicy,BossActions.Chains|BossActions.Summon,.70,.35);
        private static readonly BossDefinition CrossroadsBoss=new BossDefinition(36,"crossroads_sentinel",47500,31,940,5,26,2401,"dt1",720,16,.65,CrossroadsPolicy,BossActions.Chains|BossActions.Summon,.70,.40);
        private static readonly BossDefinition RampartBoss=new BossDefinition(39,"rampart_colossus",51000,32,1000,5,27,2581,"js1",780,17,.67,RampartPolicy,BossActions.Chains|BossActions.Summon,.75,.40);
        private static readonly BossDefinition BoundaryBoss=new BossDefinition(42,"shifting_boundary",54500,33,1060,5,28,2761,"js1",840,17,.67,BoundaryPolicy,BossActions.Chains|BossActions.Summon,.75,.40);
        private static readonly BossDefinition TorrentBoss=new BossDefinition(45,"torrent_serpent",58000,34,1120,5,29,2941,"jd1",900,18,.69,TorrentPolicy,BossActions.Chains|BossActions.Summon,.75,.45);
        private static readonly BossDefinition FordBoss=new BossDefinition(48,"ford_gatekeeper",61500,35,1180,5,30,3121,"jd1",960,18,.69,FordPolicy,BossActions.Chains|BossActions.Summon,.80,.45);
        private static readonly BossDefinition PillarBoss=new BossDefinition(51,"toppling_pillar",65000,36,1240,5,31,3301,"jd1",1020,18,.69,PillarPolicy,BossActions.Chains|BossActions.Summon,.80,.50);
        private static readonly BossDefinition ChaffBoss=new BossDefinition(54,"chaff_whirl",68500,37,1300,5,32,3481,"rt1",1080,19,.71,ChaffPolicy,BossActions.Chains|BossActions.Summon,.85,.50);
        private static readonly BossDefinition SwarmBoss=new BossDefinition(57,"swarm_monarch",72000,38,1360,5,33,3661,"sm1",1140,20,.73,SwarmPolicy,BossActions.Chains|BossActions.Summon,.85,.55);
        private static readonly BossDefinition ValleyBoss=new BossDefinition(60,"valley_bronze_brute",75500,39,1420,5,34,3841,"sm1",1200,20,.73,ValleyPolicy,BossActions.Chains|BossActions.Summon,.90,.55);
        private static readonly BossDefinition PalaceBoss=new BossDefinition(63,"palace_roof_eye",79000,40,1480,5,35,4021,"",0,0,0,PalacePolicy,BossActions.Chains);
        private static readonly BossDefinition FieldBoss=new BossDefinition(66,"lentil_field_beast",82500,41,1540,5,36,4201,"ss1",1320,21,.75,FieldPolicy,BossActions.Summon,.95,.60);
        private static readonly BossDefinition AltarBoss=new BossDefinition(69,"bethel_altar_calf",86000,42,1600,5,37,4381,"",0,0,0,AltarPolicy,BossActions.Slam);
        private static readonly BossDefinition VineyardBoss=new BossDefinition(72,"vineyard_greed_vine",89500,43,1660,5,38,4561,"fk1",1440,22,.77,VineyardPolicy,BossActions.Chains|BossActions.Summon,.95,.65);
        private static readonly BossDefinition DothanBoss=new BossDefinition(75,"dothan_iron_ring",93000,44,1720,5,39,4741,"sk1",1500,23,.79,DothanPolicy,BossActions.Summon,.95,.70);
        private static readonly BossDefinition BullBoss=new BossDefinition(78,"winged_bull",96500,45,1780,6,0,4921,"",0,0,0,BullPolicy,BossActions.Dash);
        private static readonly BossDefinition GateLionBoss=new BossDefinition(81,"ashen_gate_lion",100000,46,1840,6,41,5101,"",0,0,0,GateLionPolicy,BossActions.Dash|BossActions.Chains);
        private static readonly BossDefinition RaiderBoss=new BossDefinition(84,"wilderness_raider_chief",103500,47,1900,5,42,5281,"fc1",1680,24,.81,RaiderPolicy,BossActions.Summon,.95,.75);
        private static readonly BossDefinition BeracahBoss=new BossDefinition(87,"rending_host",107000,48,1960,5,43,5461,"",0,0,0,BeracahPolicy,BossActions.Slam);
        private static readonly BossDefinition HookBoss=new BossDefinition(90,"bronze_hook_chain",110500,49,2020,5,44,5641,"",0,0,0,HookPolicy,BossActions.Chains);
        private static readonly BossDefinition AmbushBoss=new BossDefinition(93,"roadside_ambush_shadow",114000,50,2080,6,0,5821,"",0,0,0,AmbushPolicy,BossActions.Dash);
        private static readonly BossDefinition OnoBoss=new BossDefinition(96,"ono_whispering_trap",117500,51,2140,5,46,6001,"nh1",1920,27,.87,OnoPolicy,BossActions.Summon,.95,.80);
        private static readonly BossDefinition PurBoss=new BossDefinition(99,"rolling_pur_cart",121000,52,2200,6,0,6181,"",0,0,0,PurPolicy,BossActions.Dash);
        private static readonly BossDefinition TerrorKingBoss=new BossDefinition(102,"king_of_terrors",122750,53,2260,5,48,6361,"",0,0,0,TerrorKingPolicy,BossActions.DamageZone|BossActions.Chains);
        private static readonly BossDefinition RoaringWatersBoss=new BossDefinition(105,"roaring_waters",124500,54,2320,5,49,6541,"",0,0,0,RoaringWatersPolicy,BossActions.Slam|BossActions.DamageZone);
        private static readonly BossDefinition NightTerrorBoss=new BossDefinition(108,"terror_by_night",126250,55,2380,5,50,6721,"hidden_place_shadow",2080,30,.93,NightTerrorPolicy,BossActions.Summon|BossActions.AimedShot,.95,.90);
        private static readonly BossDefinition VeilingDarknessBoss=new BossDefinition(111,"veiling_darkness",128000,56,2440,5,51,6901,"ps1",2100,30,.93,VeilingDarknessPolicy,BossActions.DamageZone|BossActions.Summon,.95,.90);
        private static readonly BossDefinition LeechBoss=new BossDefinition(114,"clamoring_leech",129750,57,2500,5,52,7081,"pv1",2140,31,LaterCampaignBooks.ProverbsSteadySpeed,LeechPolicy,BossActions.Dash|BossActions.Summon,.95,.90);
        private static readonly BossDefinition TroubleCloudsBoss=new BossDefinition(117,"clouds_of_trouble",131500,58,2560,5,53,7261,"",0,0,0,TroubleCloudsPolicy,BossActions.Slam|BossActions.Chains);
        private static readonly BossDefinition LeopardPeaksBoss=new BossDefinition(120,"leopard_of_the_peaks",133250,59,2620,5,54,7441,"",0,0,0,LeopardPeaksPolicy,BossActions.Dash|BossActions.AimedShot);
        private static readonly BossDefinition RuinsBeastBoss=new BossDefinition(123,"night_beast_of_ruins",135000,60,2680,5,55,7621,"isa1",2220,34,1.01,RuinsBeastPolicy,BossActions.Summon|BossActions.Chains,.95,.90);
        private static readonly BossDefinition LeviathanShadowBoss=new BossDefinition(126,"leviathan_shadow",136750,61,2740,5,56,7801,"",0,0,0,LeviathanShadowPolicy,BossActions.Dash|BossActions.Chains|BossActions.DamageZone);
        private static readonly BossDefinition ViperWebBoss=new BossDefinition(129,"viper_eggs_and_web",138500,62,2800,5,57,7981,"",0,0,0,ViperWebPolicy,BossActions.Chains|BossActions.DamageZone);
        private static readonly BossDefinition SurroundingTerrorBoss=new BossDefinition(132,"terror_all_around",140250,63,2860,5,58,8161,"jer1",2320,35,1.03,SurroundingTerrorPolicy,BossActions.Summon|BossActions.AimedShot,.95,.90);
        private static readonly BossDefinition CisternMudBoss=new BossDefinition(135,"cistern_mud",142000,64,2920,5,59,8341,"",0,0,0,CisternMudPolicy,BossActions.Slam|BossActions.DamageZone);
        private static readonly BossDefinition DestroyingMountainBoss=new BossDefinition(138,"destroying_mountain",143750,65,2980,5,60,8521,"",0,0,0,DestroyingMountainPolicy,BossActions.Slam|BossActions.Dash|BossActions.AimedShot);
        private static readonly BossDefinition WormwoodGallBoss=new BossDefinition(141,"wormwood_and_gall",145500,66,3040,5,61,8701,"",0,0,0,WormwoodGallPolicy,BossActions.DamageZone|BossActions.Chains);
        private static readonly BossDefinition SoulSnareBoss=new BossDefinition(144,"soul_snaring_band",147250,67,3100,5,62,8881,"",0,0,0,SoulSnarePolicy,BossActions.Chains|BossActions.AimedShot);
        private static readonly BossDefinition HighCedarBoss=new BossDefinition(147,"proud_high_cedar",149000,68,3160,5,63,9061,"eze1",2460,37,1.07,HighCedarPolicy,BossActions.Slam|BossActions.Summon,.95,.90);
        private static readonly BossDefinition GogHostBoss=new BossDefinition(150,"gog_host_from_the_north",150750,69,3220,5,64,9241,"eze1",2500,37,1.07,GogHostPolicy,BossActions.Dash|BossActions.Summon|BossActions.AimedShot,.95,.90);
        private static readonly BossDefinition DuraImageBoss=new BossDefinition(153,"golden_image_of_dura",152500,70,3280,5,65,9421,"",0,0,0,DuraImagePolicy,BossActions.Slam|BossActions.DamageZone);
        private static readonly BossDefinition FourthBeastBoss=new BossDefinition(156,"ten_horned_fourth_beast",154250,71,3340,5,66,9601,"",0,0,0,FourthBeastPolicy,BossActions.Dash|BossActions.Chains|BossActions.Slam);
        private static readonly BossDefinition PersianPrinceBoss=new BossDefinition(159,"spiritual_prince_of_persia",156000,72,3400,5,67,9781,"dan1",2580,38,1.09,PersianPrincePolicy,BossActions.AimedShot|BossActions.Summon|BossActions.DamageZone,.95,.90);
        private static readonly BossDefinition DeathSheolBoss=new BossDefinition(162,"death_and_sheol",157750,73,3460,5,68,9961,"mp1",2620,39,1.11,DeathSheolPolicy,BossActions.DamageZone|BossActions.Summon,.95,.90);
        private static readonly BossDefinition HighEagleBoss=new BossDefinition(165,"eagle_of_high_crags",159500,74,3520,5,69,10141,"",0,0,0,HighEaglePolicy,BossActions.Dash|BossActions.AimedShot);
        private static readonly BossDefinition LingeringAngerBoss=new BossDefinition(168,"lingering_anger",161250,75,3580,5,70,10321,"",0,0,0,LingeringAngerPolicy,BossActions.Slam|BossActions.Chains);
        private static readonly BossDefinition TorchChariotBoss=new BossDefinition(171,"lightning_torch_chariot",163000,76,3640,5,71,10501,"",0,0,0,TorchChariotPolicy,BossActions.Dash|BossActions.DamageZone);
        private static readonly BossDefinition EmptyFoldBoss=new BossDefinition(174,"famine_of_the_empty_fold",164750,77,3700,5,72,10681,"mp1",2740,39,1.11,EmptyFoldPolicy,BossActions.Summon|BossActions.DamageZone,.95,.90);
        private static readonly BossDefinition AccusingShadowBoss=new BossDefinition(177,"rebuked_accusing_shadow",166500,78,3760,5,73,10861,"mp1",2760,39,1.11,AccusingShadowPolicy,BossActions.Chains|BossActions.AimedShot|BossActions.Summon,.95,.90);
        private static readonly BossDefinition DevourerBoss=new BossDefinition(CampaignContent.OldTestamentFinalRound,"malachi_devourer",140000,79,3820,5,74,11041,"mp1",2800,39,1.11,DevourerPolicy,
            BossActions.Dash|BossActions.Slam|BossActions.Chains|BossActions.Summon|BossActions.AimedShot|BossActions.DamageZone,.95,.90);
        internal static BossDefinition? Find(int number)=>number switch
        {
            3=>AdamBoss,6=>IsaacBoss,9=>JudahBoss,12=>MosesBoss,15=>BezalelBoss,18=>AshVeilBoss,21=>OathBoss,24=>DustBoss,27=>RavineBoss,30=>WardenBoss,
            33=>OblivionBoss,36=>CrossroadsBoss,39=>RampartBoss,42=>BoundaryBoss,45=>TorrentBoss,48=>FordBoss,
            51=>PillarBoss,54=>ChaffBoss,57=>SwarmBoss,60=>ValleyBoss,63=>PalaceBoss,66=>FieldBoss,69=>AltarBoss,72=>VineyardBoss,75=>DothanBoss,78=>BullBoss,81=>GateLionBoss,84=>RaiderBoss,87=>BeracahBoss,90=>HookBoss,93=>AmbushBoss,96=>OnoBoss,99=>PurBoss,
            102=>TerrorKingBoss,105=>RoaringWatersBoss,108=>NightTerrorBoss,
            111=>VeilingDarknessBoss,114=>LeechBoss,117=>TroubleCloudsBoss,120=>LeopardPeaksBoss,
            123=>RuinsBeastBoss,126=>LeviathanShadowBoss,129=>ViperWebBoss,
            132=>SurroundingTerrorBoss,135=>CisternMudBoss,138=>DestroyingMountainBoss,
            141=>WormwoodGallBoss,144=>SoulSnareBoss,147=>HighCedarBoss,150=>GogHostBoss,
            153=>DuraImageBoss,156=>FourthBeastBoss,159=>PersianPrinceBoss,
            162=>DeathSheolBoss,165=>HighEagleBoss,168=>LingeringAngerBoss,
            171=>TorchChariotBoss,174=>EmptyFoldBoss,177=>AccusingShadowBoss,CampaignContent.OldTestamentFinalRound=>DevourerBoss,_=>null
        };
        public static BossDefinition ForRound(int number)=>Find(number)??throw new ArgumentException("No implemented boss for round");
        internal static BossDefinition Experimental(int number,string roundId,double hp,int contact,int gold,int damage,int actor,
            string support,int supportHp,int supportContact,double supportSpeed,params double[] thresholds)
            => Experimental(number,roundId,hp,contact,gold,damage,actor,support,supportHp,supportContact,supportSpeed,
                BossActions.Chains|BossActions.Summon,thresholds);

        internal static BossDefinition Experimental(int number,string roundId,double hp,int contact,int gold,int damage,int actor,
            string support,int supportHp,int supportContact,double supportSpeed,BossActions actions,params double[] thresholds)
        {
            if(number<LaterCampaignBooks.FirstRound)
                throw new ArgumentOutOfRangeException(nameof(number));
            return new BossDefinition(number,"trial_boss",hp,contact,gold,5,damage,actor,
                support,supportHp,supportContact,supportSpeed,ValleyPolicy,actions,thresholds)
                {experimentalRoundId=roundId};
        }
        internal string EncounterSuffix => ":"+(experimentalRoundId??(Number switch
        {
            >=NumbersContent.GlobalRoundNumber=>CampaignContent.RoundId(Number),
            21=>"leviticus_06",18=>"leviticus_03",15=>"exodus_06",12=>"exodus_03",
            _=>"genesis_"+Number.ToString("D2",CultureInfo.InvariantCulture)
        }))+":"+Id;
        public string EncounterKey(string runId)=>runId+EncounterSuffix;
        public string FingerprintContract=>Number==3?Contract:Id+(IsChariot?":absolute-clock:fixed-direction:relative-swept:contact-only:dash0.6:speed1.3:":UsesChainProjectiles?":"+ChainContract:":point-impact-v1:")+SupportMonster+":"+SupportCount.ToString(CultureInfo.InvariantCulture)+":"+RewardPolicy+":"+EvolutionRules.Contract+":"+
            string.Join(":",new[]{MaxHealth,(double)Contact,KillGold,AttackPeriod,AttackDamage,BossActorId,SupportHitPoints,SupportContact,SupportRatio,
                Speed,Radius,IntroSeconds,IntroOffset,TelegraphDuration,SlamRadius,SupportRadius,0,12}.Concat(Thresholds).Select(x=>x.ToString("R",CultureInfo.InvariantCulture)))+
            (Number>60?":actions="+((int)Actions).ToString(CultureInfo.InvariantCulture):"")+
            (Number>60&&UsesDash?":absolute-clock:fixed-direction:relative-swept:contact-only:dash"+
                DashDuration.ToString("R",CultureInfo.InvariantCulture)+":speed"+DashSpeedRatio.ToString("R",CultureInfo.InvariantCulture):"")+
            (UsesAimedShot?":aimed-v1:"+HazardContract(AimedShotSpeed,AimedShotRadius,AimedShotLifetime):"")+
            (UsesDamageZone?":zone-v1:"+HazardContract(DamageZoneRadius,DamageZoneLifetime):"");
        private static string HazardContract(params double[] values)
            => string.Join(":", values.Select(x => x.ToString("R", CultureInfo.InvariantCulture)));
        public static string EncounterId(string runId) => runId + ":genesis_03:nephilim_tyrant";
    }

    [DataContract]
    public sealed class ChariotChargeState
    {
        [DataMember] public string Phase {get;set;}="chase";
        [DataMember] public float DirectionX {get;set;}=1;
        [DataMember] public float DirectionY {get;set;}
        // Deadline is in the encounter's absolute BossElapsed clock, not world time.
        [DataMember] public double Until {get;set;}
        internal ChariotChargeState Copy()=>(ChariotChargeState)MemberwiseClone();
        internal void Validate(BossEncounterState boss,BossDefinition rules,bool combat)
        {
            if(Phase!="chase"&&Phase!="telegraph"&&Phase!="dash")throw new UnsupportedSaveVersionException();
            if(!BattleSnapshot.Position(DirectionX,DirectionY)||Math.Abs((double)DirectionX*DirectionX+(double)DirectionY*DirectionY-1)>.0001||!BattleSnapshot.Finite(Until))throw new InvalidDataException("Invalid chariot direction/deadline");
            var last=boss.NextAttackAt-rules.AttackPeriod;
            var expected=combat&&last>=rules.AttackPeriod&&boss.BossElapsed<last+BossDefinition.ChariotDashDuration-1e-9?"dash":
                combat&&boss.BossElapsed+1e-9>=boss.NextAttackAt-rules.TelegraphDuration?"telegraph":"chase";
            var deadline=expected=="dash"?last+BossDefinition.ChariotDashDuration:expected=="telegraph"?boss.NextAttackAt:0;
            if(Phase!=expected||Math.Abs(Until-deadline)>1e-7||boss.Telegraphing!=(Phase=="telegraph"))throw new InvalidDataException("Invalid absolute chariot clock");
        }
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
    public sealed class BossAimedShotState
    {
        [DataMember] public float OriginX { get; set; }
        [DataMember] public float OriginY { get; set; }
        [DataMember] public float DirectionX { get; set; }
        [DataMember] public float DirectionY { get; set; }

        public Vector2 Origin => new Vector2(OriginX, OriginY);
        public Vector2 Direction => new Vector2(DirectionX, DirectionY);
        public Vector2 PositionAt(double age) => Origin + Direction * (float)(BossDefinition.AimedShotSpeed * age);
        internal BossAimedShotState Copy() => (BossAimedShotState)MemberwiseClone();

        internal void Validate()
        {
            if (!BattleSnapshot.Position(OriginX, OriginY) || !BattleSnapshot.Position(DirectionX, DirectionY) ||
                Math.Abs(Direction.LengthSquared() - 1) > .0001)
                throw new InvalidDataException("Invalid aimed shot geometry");
        }
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
        [DataMember(EmitDefaultValue=false)] public ChariotChargeState? ChariotCharge {get;set;}
        [DataMember(EmitDefaultValue=false)] public BossAimedShotState? AimedShot { get; set; }
        public double SecondsSinceAttack(BossDefinition rules) => BossElapsed - (NextAttackAt - rules.AttackPeriod);
        public bool HasActiveDamageZone(BossDefinition rules, string stage)
            => rules.UsesDamageZone && stage == "boss" && !Telegraphing &&
                NextAttackAt > rules.AttackPeriod && SecondsSinceAttack(rules) < BossDefinition.DamageZoneLifetime - 1e-9;
        [DataMember] public bool ThresholdUsed {get;set;}
        [DataMember] public int Summoned {get;set;}
        [DataMember] public int NextActorId {get;set;}=482;
        [DataMember] public List<SpawnedEnemy> Supports {get;set;}=new List<SpawnedEnemy>();
        [DataMember] public bool BossGoldApplied {get;set;}
        [DataMember] public BossRewardState? Reward {get;set;}
        [DataMember(EmitDefaultValue=false)] public bool CompletionApplied {get;set;}
        public double CombatTime=>NormalEndedAt+IntroElapsed+BossElapsed;
        internal BossEncounterState Copy()
        {var copy=(BossEncounterState)MemberwiseClone();copy.ChariotCharge=ChariotCharge?.Copy();copy.AimedShot=AimedShot?.Copy();copy.ChainDirections=ChainDirections?.Select(x=>x.Copy()).ToList();copy.Supports=Supports.Select(x=>x.Copy()).ToList();copy.Reward=Reward?.Copy();return copy;}
        internal void Validate(RunState run)
            => Validate(run,BossDefinition.Find(EncounterRound)??throw new UnsupportedSaveVersionException());

        internal void Validate(RunState run,BossDefinition rules)
        {
            bool Finite(double n)=>BattleSnapshot.Finite(n);
            if(Version>BossDefinition.LatestEncounterVersion||EncounterRound!=rules.Number)throw new UnsupportedSaveVersionException();
            var mask=rules.Number==3?(ThresholdUsed?1:0):SummonMask;
            var used=(mask&1)+((mask>>1)&1);var maxMask=(1<<rules.Thresholds.Count)-1;
            if((Version!=rules.EncounterVersion&&!(rules.Number==3&&Version==1))||EncounterId!=rules.EncounterKey(run.RunId)||!Finite(NormalEndedAt)||NormalEndedAt<0||NormalEndedAt>180||
                !Finite(IntroElapsed)||IntroElapsed<0||IntroElapsed>2||!Finite(BossElapsed)||BossElapsed<0||
                !Finite(CombatTime)||!Finite(Health)||Health<0||Health>rules.MaxHealth||!Finite(NextAttackAt)||NextAttackAt<rules.AttackPeriod||
                Math.Abs(NextAttackAt/rules.AttackPeriod-Math.Round(NextAttackAt/rules.AttackPeriod))>1e-7||NextAttackAt<=BossElapsed||NextAttackAt>BossElapsed+rules.AttackPeriod+1e-7||
                !BattleSnapshot.Position(TargetX,TargetY)||Supports==null||Supports.Any(x=>x==null)||mask<0||mask>maxMask||(mask==2)||
                (rules.Number==3&&SummonMask!=0)||(rules.Number!=3&&ThresholdUsed!=(mask!=0))||Summoned!=used*BossDefinition.SupportCount||NextActorId!=rules.BossActorId+1+Summoned||Supports.Count>Summoned||
                Supports.Select(x=>x.Id).Distinct().Count()!=Supports.Count||Supports.Any(x=>x.Id<=rules.BossActorId||x.Id>=NextActorId||x.MonsterId!=rules.SupportMonster||
                    !Finite(x.Health)||x.Health<=0||x.Health>rules.SupportHitPoints||x.ContactDamage!=rules.SupportContact||x.RawExperience!=0)||run.LastHitTime>CombatTime)
                throw new InvalidDataException("Invalid boss encounter");
            if(rules.UsesDash){if(ChariotCharge==null)throw new InvalidDataException("Missing dash clock");ChariotCharge.Validate(this,rules,run.Stage=="boss");}
            else if(ChariotCharge!=null||Version==4)throw new UnsupportedSaveVersionException();
            if(rules.UsesChainProjectiles)
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
                if(rules.UsesDash&&ChainDirections.Count==3&&
                    (Math.Abs(ChainDirections[1].X-ChariotCharge!.DirectionX)>.0001||
                     Math.Abs(ChainDirections[1].Y-ChariotCharge.DirectionY)>.0001))
                    throw new InvalidDataException("Chain directions do not match the locked dash aim");
            }
            else if(ChainDirections!=null)throw new InvalidDataException("Chain directions outside R09");
            var needsAim = rules.UsesAimedShot && run.Stage == "boss" && Telegraphing;
            var mayHaveShot = rules.UsesAimedShot && run.Stage == "boss" &&
                NextAttackAt > rules.AttackPeriod && SecondsSinceAttack(rules) < BossDefinition.AimedShotLifetime - 1e-9;
            if (AimedShot == null ? needsAim : !needsAim && !mayHaveShot)
                throw new InvalidDataException("Aimed shot outside attack clock");
            AimedShot?.Validate();
            if (needsAim && ChariotCharge != null &&
                Vector2.DistanceSquared(AimedShot!.Direction, new Vector2(ChariotCharge.DirectionX, ChariotCharge.DirectionY)) > 1e-8)
                throw new InvalidDataException("Aimed shot does not match the locked dash aim");
            if(run.Stage=="boss_intro")
            {
                if(run.RoundNumber!=rules.Number||TargetX!=0||TargetY!=0||IntroElapsed>=2||BossElapsed!=0||Health!=rules.MaxHealth||Summoned!=0||CompletionApplied||BossGoldApplied||Reward!=null||Telegraphing||NextAttackAt!=rules.AttackPeriod||run.Drops.Count!=0)
                    throw new InvalidDataException("Invalid protected intro");
            }
            else if(run.Stage=="boss")
            {
                if(run.RoundNumber!=rules.Number||IntroElapsed!=2||Health<=0||CompletionApplied||BossGoldApplied||Reward!=null||Telegraphing!=(rules.HasTimedAttack&&BossElapsed+1e-9>=NextAttackAt-rules.TelegraphDuration)||
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
                    (Reward.PolicyId==BossDefinition.LegacyPolicy?(rules.Number!=3||Version!=1):(Version!=rules.EncounterVersion||Reward.PolicyId!=rules.RewardPolicy)))
                    throw new InvalidDataException("Invalid boss reward/settlement receipt");
                BossRewards.Validate(Reward,run,CompletionApplied);
            }
            else throw new InvalidDataException("Boss state outside boss lifecycle");
        }
    }
}
