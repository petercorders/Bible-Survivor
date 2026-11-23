using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Globalization;
using System.Numerics;
using System.Runtime.Serialization;

namespace BibleSurvivor.Core
{
    // World distances and speeds are prototype tuning, not measured Unity balance values.
    [DataContract]
    public sealed class BattleTuning
    {
        [DataMember] public double PlayerSpeed {get;set;}=3;
        [DataMember] public double CameraHalfWidth {get;set;}=10;
        [DataMember] public double CameraHalfHeight {get;set;}=5.625;
        [DataMember] public double SpawnMargin {get;set;}=1;
        [DataMember] public double PlayerRadius {get;set;}=.25;
        [DataMember] public double EnemyRadius {get;set;}=.2;
        [DataMember] public double BibleOrbitRadius {get;set;}=1.4;
        [DataMember] public double BookRadius {get;set;}=.2;
        [DataMember] public double JawboneRange {get;set;}=1.6;
        [DataMember] public double SlingSpeed {get;set;}=8;
        [DataMember] public double SlingLifetime {get;set;}=3;
        [DataMember] public double PickupRadius {get;set;}=.7;
        [DataMember] public int MaxProjectiles {get;set;}=256;
        public void Validate()
        {
            var values=new[]{PlayerSpeed,CameraHalfWidth,CameraHalfHeight,SpawnMargin,PlayerRadius,EnemyRadius,
                BibleOrbitRadius,BookRadius,JawboneRange,SlingSpeed,SlingLifetime,PickupRadius};
            if(values.Any(x=>double.IsNaN(x)||double.IsInfinity(x)||x<=0||x>10000) || MaxProjectiles<1||MaxProjectiles>4096)
                throw new ArgumentException("Invalid battle tuning");
        }
        internal BattleTuning Copy()=>(BattleTuning)MemberwiseClone();
        internal bool Same(BattleTuning other)=>PlayerSpeed==other.PlayerSpeed&&CameraHalfWidth==other.CameraHalfWidth&&
            CameraHalfHeight==other.CameraHalfHeight&&SpawnMargin==other.SpawnMargin&&PlayerRadius==other.PlayerRadius&&
            EnemyRadius==other.EnemyRadius&&BibleOrbitRadius==other.BibleOrbitRadius&&BookRadius==other.BookRadius&&
            JawboneRange==other.JawboneRange&&SlingSpeed==other.SlingSpeed&&SlingLifetime==other.SlingLifetime&&
            PickupRadius==other.PickupRadius&&MaxProjectiles==other.MaxProjectiles;
    }
    [DataContract]
    public sealed class BattleEntityState
    {
        [DataMember] public int Id {get;set;}
        [DataMember] public float X {get;set;}
        [DataMember] public float Y {get;set;}
        internal Vector2 Position {get=>new Vector2(X,Y);set{X=value.X;Y=value.Y;}}
        internal BattleEntityState Copy()=>(BattleEntityState)MemberwiseClone();
    }
    [DataContract]
    public sealed class BattleHitState
    {
        [DataMember] public int Id {get;set;}
        [DataMember] public double At {get;set;}
        internal BattleHitState Copy()=>(BattleHitState)MemberwiseClone();
    }
    [DataContract]
    public sealed class BattleProjectileState
    {
        [DataMember] public long Id {get;set;}
        [DataMember] public float X {get;set;}
        [DataMember] public float Y {get;set;}
        [DataMember] public float VelocityX {get;set;}
        [DataMember] public float VelocityY {get;set;}
        [DataMember] public double Lifetime {get;set;}
        [DataMember] public double Damage {get;set;}
        [DataMember] public int RemainingPierce {get;set;}
        [DataMember] public double Radius {get;set;}
        [DataMember] public List<int> HitIds {get;set;}=new List<int>();
        internal Vector2 Position {get=>new Vector2(X,Y);set{X=value.X;Y=value.Y;}}
        internal Vector2 Velocity {get=>new Vector2(VelocityX,VelocityY);set{VelocityX=value.X;VelocityY=value.Y;}}
        internal BattleProjectileState Copy()
        {var copy=(BattleProjectileState)MemberwiseClone();copy.HitIds=new List<int>(HitIds);return copy;}
    }
    [DataContract]
    public sealed class BattleWeaponState
    {
        [DataMember] public string WeaponId {get;set;}="";
        [DataMember] public double NextCycle {get;set;}
        [DataMember] public double ActiveUntil {get;set;}
        [DataMember] public double ActiveSince {get;set;}=-1;
        [DataMember] public double NextBurst {get;set;}
        [DataMember] public int BurstsRemaining {get;set;}
        [DataMember] public List<double> PendingBursts {get;set;}=new List<double>();
        [DataMember] public List<BattleHitState> OrbitHits {get;set;}=new List<BattleHitState>();
        [DataMember(EmitDefaultValue=false)] public string? WeaponForm {get;set;}
        [DataMember(EmitDefaultValue=false)] public double NextPulse {get;set;}
        [DataMember(EmitDefaultValue=false)] public double PulseUntil {get;set;}
        [DataMember(EmitDefaultValue=false)] public int FocusTargetId {get;set;}
        [DataMember(EmitDefaultValue=false)] public List<int>? PulseHitIds {get;set;}
        [DataMember(EmitDefaultValue=false)] public bool StrengthenedFirstPulse { get; set; }
        [DataMember(EmitDefaultValue=false)] public double FrozenPulseRadius { get; set; }
        [DataMember(EmitDefaultValue=false)] public List<BattleHitState>? FormHits {get;set;}
        internal BattleWeaponState Copy()
        {var copy=(BattleWeaponState)MemberwiseClone();copy.OrbitHits=OrbitHits.Select(x=>x.Copy()).ToList();copy.PendingBursts=new List<double>(PendingBursts);copy.PulseHitIds=PulseHitIds?.ToList();copy.FormHits=FormHits?.Select(x=>x.Copy()).ToList();return copy;}
    }
    [DataContract]
    public sealed class BattleSpeedState
    {
        [DataMember] public string MonsterId {get;set;}="";
        [DataMember] public double Ratio {get;set;}
    }
    [DataContract]
    public sealed class EnemyChargeState
    {
        [DataMember] public int Version {get;set;}=1;
        [DataMember] public int Id {get;set;}
        [DataMember] public string Phase {get;set;}="cooldown";
        [DataMember] public double NextAt {get;set;}
        [DataMember] public double Until {get;set;}
        [DataMember] public float TargetX {get;set;}
        [DataMember] public float TargetY {get;set;}
        [DataMember] public float DirectionX {get;set;}=1;
        [DataMember] public float DirectionY {get;set;}
        internal EnemyChargeState Copy()=>(EnemyChargeState)MemberwiseClone();
        internal void Validate(double time)
        {
            if(Version>1)throw new UnsupportedSaveVersionException();
            if(Version!=1||Id<1||!BattleSnapshot.Finite(NextAt)||NextAt<0||!BattleSnapshot.Finite(Until)||Until<0||
                !BattleSnapshot.Position(TargetX,TargetY)||!BattleSnapshot.Position(DirectionX,DirectionY)||Math.Abs((double)DirectionX*DirectionX+(double)DirectionY*DirectionY-1)>.0001)
                throw new ArgumentException("Invalid charge state");
            if(Phase=="cooldown")
            {if(Until!=0||NextAt<time-1e-7||NextAt>time+BattleWorld.ChargeCooldown+1e-7)throw new ArgumentException("Invalid charge cooldown");}
            else if(Phase=="telegraph"||Phase=="charge")
            {
                var duration=Phase=="telegraph"?BattleWorld.ChargeTelegraph:BattleWorld.ChargeDuration;
                var offset=BattleWorld.ChargeTelegraph+(Phase=="charge"?BattleWorld.ChargeDuration:0);
                if(Until<time-1e-7||Until>time+duration+1e-7||Math.Abs(Until-NextAt-offset)>1e-7)throw new ArgumentException("Invalid charge timing");
            }
            else throw new UnsupportedSaveVersionException();
        }
    }
    [DataContract]
    public sealed class BattleSnapshot
    {
        internal IEnumerable<int> PermanentTargetLedger()
        {
            foreach (var projectile in Projectiles)
                foreach (var id in projectile.HitIds) yield return id;
            foreach (var weapon in Weapons)
                foreach (var hit in weapon.OrbitHits) yield return hit.Id;
            if (Lightning != null)
                foreach (var strike in Lightning.Pending)
                {
                    if (strike.AssignedTargetId != 0) yield return strike.AssignedTargetId;
                    if (strike.VisitedIds != null)
                        foreach (var id in strike.VisitedIds) yield return id;
                }
            if (FirePillar != null)
                foreach (var region in FirePillar.Regions)
                    if (region.TargetId != 0) yield return region.TargetId;
            if (MosesStaff != null)
                foreach (var wave in MosesStaff.Waves)
                    foreach (var id in wave.HitIds) yield return id;
            if (Cross != null)
                foreach (var shot in Cross.Shots)
                    foreach (var target in shot.HitTargets) yield return target.Id;
            if (ElijahChariot != null)
                foreach (var path in ElijahChariot.Paths)
                {
                    foreach (var id in path.BodyHitIds) yield return id;
                    foreach (var id in path.WhirlwindCandidateIds) yield return id;
                }
            if (DanielLion != null)
            {
                foreach (var lion in DanielLion.Lions)
                    if (lion.TargetId != 0) yield return lion.TargetId;
                if (DanielLion.Leap != null)
                    foreach (var id in DanielLion.Leap.VisitedIds) yield return id;
            }
        }
        internal const int ExpandedVersion = 6;
        internal const int ExpandedProjectileLimit = 256;
        [DataMember] public int SchemaVersion {get;set;}=1;
        [DataMember(EmitDefaultValue=false)] public List<EnemyChargeState>? Charges {get;set;}
        [DataMember(EmitDefaultValue=false)] public List<HostileProjectileState>? HostileProjectiles {get;set;}
        [DataMember(EmitDefaultValue=false)] public long NextHostileId {get;set;}
        [DataMember(EmitDefaultValue=false)] public ExpandedLightningState? Lightning { get; set; }
        [DataMember(EmitDefaultValue=false)] public ExpandedWildernessVoiceState? WildernessVoice { get; set; }
        [DataMember(EmitDefaultValue=false)] public ExpandedFirePillarState? FirePillar { get; set; }
        [DataMember(EmitDefaultValue=false)] public ExpandedMosesStaffState? MosesStaff { get; set; }
        [DataMember(EmitDefaultValue=false)] public ExpandedCrossState? Cross { get; set; }
        [DataMember(EmitDefaultValue=false)] public ExpandedElijahChariotState? ElijahChariot { get; set; }
        [DataMember(EmitDefaultValue=false)] public ExpandedDanielLionState? DanielLion { get; set; }
        [DataMember] public double Time {get;set;}
        [DataMember] public double Remainder {get;set;}
        [DataMember] public float PlayerX {get;set;}
        [DataMember] public float PlayerY {get;set;}
        [DataMember] public float FacingX {get;set;}=1;
        [DataMember] public float FacingY {get;set;}
        [DataMember] public uint RandomState {get;set;}
        [DataMember] public long NextProjectileId {get;set;}=1;
        [DataMember] public int ShotsFired {get;set;}
        [DataMember] public int Hits {get;set;}
        [DataMember] public int Kills {get;set;}
        [DataMember(EmitDefaultValue=false)] public int BossKills {get;set;}
        [DataMember] public int Collected {get;set;}
        [DataMember] public int DamageTaken {get;set;}
        [DataMember] public BattleTuning Tuning {get;set;}=new BattleTuning();
        [DataMember] public List<BattleEntityState> Enemies {get;set;}=new List<BattleEntityState>();
        [DataMember] public List<BattleEntityState> Drops {get;set;}=new List<BattleEntityState>();
        [DataMember] public List<BattleProjectileState> Projectiles {get;set;}=new List<BattleProjectileState>();
        [DataMember] public List<BattleWeaponState> Weapons {get;set;}=new List<BattleWeaponState>();
        [DataMember] public List<BattleSpeedState> MonsterSpeeds {get;set;}=new List<BattleSpeedState>();
        public void Validate()
        {
            if(SchemaVersion < 1 || SchemaVersion > ExpandedVersion || Tuning==null)
                throw new ArgumentException("Unsupported battle snapshot");
            Tuning.Validate();
            var expanded = SchemaVersion == ExpandedVersion;
            if(expanded ? Charges != null : SchemaVersion>=3)
            {
                if(Charges==null||Charges.Any(x=>x==null)||Charges.Select(x=>x.Id).Distinct().Count()!=Charges.Count)throw new ArgumentException("Invalid charge collection");
                foreach(var charge in Charges)charge.Validate(Time);
            }
            else if(Charges!=null)throw new ArgumentException("Charge state requires World3");
            if(expanded ? HostileProjectiles != null : SchemaVersion==4)
            {
                if(HostileProjectiles==null||HostileProjectiles.Count>BossDefinition.ChainCap||HostileProjectiles.Any(x=>x==null)||HostileProjectiles.Select(x=>x.Id).Distinct().Count()!=HostileProjectiles.Count||NextHostileId<1||(NextHostileId-1)%3!=0)throw new ArgumentException("Invalid hostile collection");
                foreach(var hostile in HostileProjectiles)hostile.Validate(Time,NextHostileId);
            }
            else if(HostileProjectiles!=null||NextHostileId!=0)throw new ArgumentException("Hostile state requires World4");
            if(!Finite(Time)||Time<0||(SchemaVersion==1&&Time>180.000001)||!Finite(Remainder)||Remainder<0||Remainder>=BattleWorld.FixedStep+
                1e-9||!Position(PlayerX,PlayerY)||!Position(FacingX,FacingY)||
                Math.Abs((double)FacingX*FacingX+(double)FacingY*FacingY-1)>.0001||RandomState==0||NextProjectileId<1||
                BossKills<0||BossKills>1||ShotsFired<0||Hits<0||Kills<0||Collected<0||DamageTaken<0||Enemies==null||Drops==null||Projectiles==null||Weapons==null||MonsterSpeeds==null||
                Enemies.Count>120||Drops.Count>6210||Projectiles.Count>Tuning.MaxProjectiles||
                expanded && Projectiles.Count>ExpandedProjectileLimit||Weapons.Count>6)
                throw new ArgumentException("Invalid battle snapshot");
            // JSON can explicitly contain null entries even though these public DTO lists are non-nullable in C#.
            // Reject before any selector dereferences an entry so SaveStore can use its corrupt-save fallback.
            if(Projectiles.Any(x=>x==null)||Weapons.Any(x=>x==null)||MonsterSpeeds.Any(x=>x==null)||
                Enemies.Any(x=>x==null)||Drops.Any(x=>x==null))throw new ArgumentException("Null battle entry");
            if(MonsterSpeeds.Count==0||MonsterSpeeds.Count>1000||MonsterSpeeds.Any(x=>x==null||string.IsNullOrWhiteSpace(x.MonsterId)||
                !Finite(x.Ratio)||x.Ratio<=0||x.Ratio>10)||MonsterSpeeds.Select(x=>x.MonsterId).Distinct().Count()!=MonsterSpeeds.Count)
                throw new ArgumentException("Invalid checkpoint monster speeds");
            ValidateEntities(Enemies);ValidateEntities(Drops);
            if(Enemies.Any(x=>Drops.Any(d=>d.Id==x.Id)))throw new ArgumentException("Enemy also appears as drop");
            if(Projectiles.Select(x=>x.Id).Distinct().Count()!=Projectiles.Count)throw new ArgumentException("Duplicate projectile");
            foreach(var p in Projectiles)
                if(p.Id<1||p.Id>=NextProjectileId||!Position(p.X,p.Y)||!Position(p.VelocityX,p.VelocityY)||
                    !Finite(p.Lifetime)||p.Lifetime<=0||p.Lifetime>Tuning.SlingLifetime+1e-8||!Finite(p.Damage)||p.Damage<=0||
                    p.RemainingPierce<1||p.RemainingPierce>10000||!Finite(p.Radius)||p.Radius<=0||p.HitIds==null||
                    p.HitIds.Count>6210||p.HitIds.Any(x=>x<=0)||p.HitIds.Count!=p.HitIds.Distinct().Count())
                    throw new ArgumentException("Invalid projectile snapshot");
            if(Weapons.Select(x=>x.WeaponId).Distinct().Count()!=Weapons.Count)throw new ArgumentException("Duplicate weapon clock");
            foreach(var w in Weapons)
            {
                if(w.WeaponForm!=null)
                {
                    if(SchemaVersion==1||EvolutionRules.Base(w.WeaponForm)!=w.WeaponId||!Finite(w.NextPulse)||w.NextPulse<0||w.NextPulse>Time+EvolutionRules.PulsePeriod+1e-7||!Finite(w.PulseUntil)||w.PulseUntil<0||w.PulseUntil>Time+EvolutionRules.PulseLifetime+1e-7||w.FocusTargetId<0||
                        (w.PulseHitIds!=null&&(w.PulseHitIds.Any(x=>x<1)||w.PulseHitIds.Count!=w.PulseHitIds.Distinct().Count()))||
                        (w.FormHits!=null&&(w.FormHits.Any(x=>x==null||x.Id<1||!Finite(x.At)||x.At<0||x.At>Time+1e-7)||w.FormHits.Select(x=>x.Id).Distinct().Count()!=w.FormHits.Count)))
                        throw new ArgumentException("Invalid evolution clock");
                    if(w.WeaponForm!=EvolutionRules.Bible&&(w.NextPulse!=0||w.PulseUntil!=0||w.PulseHitIds!=null||w.FormHits!=null))throw new ArgumentException("Unexpected pulse clock");
                    if(w.WeaponForm!=EvolutionRules.Sling&&w.FocusTargetId!=0)throw new ArgumentException("Unexpected volley focus");
                }
                else if(w.NextPulse!=0||w.PulseUntil!=0||w.FocusTargetId!=0||w.PulseHitIds!=null||w.FormHits!=null)throw new ArgumentException("Evolution clock without form");
                if (w.StrengthenedFirstPulse && (!expanded || w.WeaponForm != EvolutionRules.Bible || w.NextPulse <= 0))
                    throw new ArgumentException("Unexpected strengthened first pulse");
                if (!Finite(w.FrozenPulseRadius) || w.FrozenPulseRadius < 0 ||
                    w.FrozenPulseRadius > EvolutionRules.PulseRadius * 2.5 ||
                    (w.FrozenPulseRadius > 0) != (expanded && w.WeaponForm == EvolutionRules.Bible && w.NextPulse > 0))
                    throw new ArgumentException("Invalid frozen pulse radius");
                if(!EquipmentInventory.SupportsWeapon(w.WeaponId,expanded?2:1)||!Finite(w.NextCycle)||w.NextCycle<0||
                    !Finite(w.ActiveUntil)||w.ActiveUntil<0||!Finite(w.ActiveSince)||w.ActiveSince< -1||
                    !Finite(w.NextBurst)||w.NextBurst<0||w.BurstsRemaining<0||w.BurstsRemaining>1007||w.OrbitHits==null||
                    w.PendingBursts==null||w.PendingBursts.Count>10000||w.PendingBursts.Any(x=>!Finite(x)||x<0)||
                    w.OrbitHits.Any(x=>x==null)||
                    w.OrbitHits.Select(x=>x.Id).Distinct().Count()!=w.OrbitHits.Count||
                    w.OrbitHits.Any(x=>x.Id<=0||!Finite(x.At)||x.At<0||x.At>Time+1e-8))
                    throw new ArgumentException("Invalid weapon clock");
                if (expanded && (w.BurstsRemaining != w.PendingBursts.Count ||
                    w.PendingBursts.Count >= CombatStats.ExpandedAmountLimit(w.WeaponId) ||
                    (w.PendingBursts.Count == 0 ? w.NextBurst != 0 :
                        w.WeaponId == "bible" || w.NextBurst != w.PendingBursts[0] ||
                        w.PendingBursts[0] <= Time || w.PendingBursts[w.PendingBursts.Count - 1] >= w.NextCycle ||
                        w.PendingBursts.Zip(w.PendingBursts.Skip(1), (earlier, later) => later <= earlier).Any(outOfOrder => outOfOrder))))
                    throw new ArgumentException("Invalid expanded weapon volley");
                if (expanded && ExpandedWeaponRules.TryGet(w.WeaponId, out _) && !HasExpandedState(w.WeaponId))
                    throw new ArgumentException("Expanded weapon clock is missing its attack state");
            }
            if (Lightning != null)
            {
                var clock = Weapons.SingleOrDefault(weapon => weapon.WeaponId == "lightning");
                if (!expanded || clock == null) throw new ArgumentException("Lightning requires an expanded weapon clock");
                Lightning.Validate(Time, clock.WeaponForm != null);
            }
            if (WildernessVoice != null)
            {
                var clock = Weapons.SingleOrDefault(weapon => weapon.WeaponId == "wilderness_voice");
                if (!expanded || clock == null) throw new ArgumentException("Voice requires an expanded weapon clock");
                WildernessVoice.Validate(Time, clock.WeaponForm != null);
            }
            if (FirePillar != null)
            {
                var clock = Weapons.SingleOrDefault(weapon => weapon.WeaponId == "fire_pillar");
                if (!expanded || clock == null) throw new ArgumentException("Fire pillar requires an expanded weapon clock");
                FirePillar.Validate(Time, clock.WeaponForm != null);
            }
            if (MosesStaff != null)
            {
                var clock = Weapons.SingleOrDefault(weapon => weapon.WeaponId == "moses_staff");
                if (!expanded || clock == null) throw new ArgumentException("Moses staff requires an expanded weapon clock");
                MosesStaff.Validate(Time, clock.WeaponForm != null);
            }
            if (Cross != null)
            {
                var clock = Weapons.SingleOrDefault(weapon => weapon.WeaponId == "cross");
                if (!expanded || clock == null) throw new ArgumentException("Cross requires an expanded weapon clock");
                Cross.Validate(Time, clock.WeaponForm != null);
            }
            if (ElijahChariot != null)
            {
                var clock = Weapons.SingleOrDefault(weapon => weapon.WeaponId == "elijah_chariot");
                if (!expanded || clock == null) throw new ArgumentException("Chariot requires an expanded weapon clock");
                ElijahChariot.Validate(Time, clock.WeaponForm != null);
            }
            if (DanielLion != null)
            {
                var clock = Weapons.SingleOrDefault(weapon => weapon.WeaponId == "daniel_lion");
                if (!expanded || clock == null) throw new ArgumentException("Lion requires an expanded weapon clock");
                DanielLion.Validate(Time, clock.WeaponForm != null);
            }
        }

        private bool HasExpandedState(string weaponId) => weaponId switch
        {
            "lightning" => Lightning != null,
            "wilderness_voice" => WildernessVoice != null,
            "fire_pillar" => FirePillar != null,
            "moses_staff" => MosesStaff != null,
            "cross" => Cross != null,
            "elijah_chariot" => ElijahChariot != null,
            "daniel_lion" => DanielLion != null,
            _ => false,
        };
        private static void ValidateEntities(List<BattleEntityState> entries)
        {if(entries.Any(x=>x==null||x.Id<1||!Position(x.X,x.Y))||entries.Select(x=>x.Id).Distinct().Count()!=entries.Count)
            throw new ArgumentException("Invalid entity snapshot");}
        internal static bool Finite(double n)=>!double.IsNaN(n)&&!double.IsInfinity(n);
        internal static bool Position(float x,float y)=>Finite(x)&&Finite(y)&&Math.Abs(x)<=1000000&&Math.Abs(y)<=1000000;
    }
    public sealed class ChariotDashView
    {
        public Vector2 Origin {get;}
        public Vector2 Direction {get;}
        public string Phase {get;}
        public double BossElapsed {get;}
        public double Deadline {get;}
        public double RemainingDistance {get;}
        public double BodyRadius {get;}
        public double PlayerRadius {get;}
        public double DangerHalfWidth=>BodyRadius+PlayerRadius;
        internal ChariotDashView(Vector2 origin,Vector2 direction,string phase,double elapsed,double deadline,double remaining,double body,double player)
        {Origin=origin;Direction=direction;Phase=phase;BossElapsed=elapsed;Deadline=deadline;RemainingDistance=remaining;BodyRadius=body;PlayerRadius=player;}
    }
    public sealed class BattleWorld
    {
        public const double FixedStep=1.0/60;
        public const double E1Speed=.60,E2Speed=.45,E3Speed=.95;
        public static string RoundFiveContract=>"r05-chase-v1:no-forced-movement:"+string.Join(":",new[]{E1Speed,E2Speed,E3Speed}.Select(x=>x.ToString("R",CultureInfo.InvariantCulture)));
        public const double ChargeCooldown=6, ChargeTelegraph=1.2, ChargeDuration=.6, ChargeSpeed=1.3;
        public static bool UsesNormalCharge(int round,string monster)=>round==4&&monster=="d3"||round==12&&monster=="ex9";
        // Fresh immutable geometry for rendering, including actual tuning radii and a live origin.
        public ChariotDashView? ChariotDash
        {
            get
            {
                var boss=Session.Encounter;var charge=boss?.ChariotCharge;var rules=Session.BossRules;
                if(charge==null||Session.Stage!="boss"||!enemies.TryGetValue(rules!.BossActorId,out var origin))return null;
                var seconds=charge.Phase=="dash"?Math.Max(0,charge.Until-boss!.BossElapsed):charge.Phase=="telegraph"?rules.DashDuration:0;
                return new ChariotDashView(origin,new Vector2(charge.DirectionX,charge.DirectionY),charge.Phase,boss!.BossElapsed,charge.Until,
                    seconds*tuning.PlayerSpeed*rules.DashSpeedRatio,BossDefinition.Radius,tuning.PlayerRadius);
            }
        }
        public static string ChargeContract=>"r04-charge-v1:locked:overlap1,0:end-cooldown:"+string.Join(":",new[]{ChargeCooldown,ChargeTelegraph,ChargeDuration,ChargeSpeed,.65,.5,.6}.Select(x=>x.ToString("R",CultureInfo.InvariantCulture)));
        private readonly List<HostileProjectileState> hostileProjectiles=new List<HostileProjectileState>();
        private long nextHostile=1;
        private ExpandedLightningState? lightning;
        private readonly ExpandedLightningSettings lightningSettings;
        private ExpandedWildernessVoiceState? wildernessVoice;
        private ExpandedFirePillarState? firePillar;
        private ExpandedMosesStaffState? mosesStaff;
        private ExpandedCrossState? cross;
        private ExpandedElijahChariotState? elijahChariot;
        private ExpandedDanielLionState? danielLion;
        public IReadOnlyList<HostileProjectileState> HostileProjectiles=>hostileProjectiles.OrderBy(x=>x.Id).Select(x=>x.Copy()).ToList().AsReadOnly();
        private readonly Dictionary<int,EnemyChargeState> charges=new Dictionary<int,EnemyChargeState>();
        public IReadOnlyDictionary<int,EnemyChargeState> ChargeStates=>new ReadOnlyDictionary<int,EnemyChargeState>(charges.ToDictionary(x=>x.Key,x=>x.Value.Copy()));
        private readonly BattleTuning tuning;
        private readonly Dictionary<string,double> speeds;
        private readonly Dictionary<int,Vector2> enemies=new Dictionary<int,Vector2>();
        private readonly Dictionary<int,Vector2> drops=new Dictionary<int,Vector2>();
        private readonly List<BattleProjectileState> projectiles=new List<BattleProjectileState>();
        private readonly Dictionary<string,BattleWeaponState> weapons=new Dictionary<string,BattleWeaponState>();
        private uint random;
        private double remainder;
        private Vector2 facing=Vector2.UnitX;
        private long nextProjectile=1;
        private readonly BattleImpact[] impacts=new BattleImpact[ImpactCapacity];
        private long stepOrdinal;
        private long impactFloor=1;
        public const int ImpactCapacity=256;
        public long ImpactSequence {get;private set;}
        // World-local cursor. Retain the newest events on overflow; readers learn exactly how many were lost.
        // Neither cursor nor events are checkpoint data. Restore starts with an empty history.
        public long ReadImpacts(long after,List<BattleImpact> destination)
        {
            if(destination==null)throw new ArgumentNullException(nameof(destination));
            if(after<0||after>ImpactSequence)throw new ArgumentOutOfRangeException(nameof(after));
            destination.Clear();
            var first=Math.Max(impactFloor,Math.Max(after+1,Math.Max(1,ImpactSequence-ImpactCapacity+1)));
            for(var sequence=first;sequence<=ImpactSequence;sequence++)destination.Add(impacts[(int)((sequence-1)%ImpactCapacity)]);
            return Math.Max(0,first-after-1);
        }
        private void Impact(string cause,int target,Vector2 position,double damage,bool killed)
        {
            var sequence=++ImpactSequence;
            impacts[(int)((sequence-1)%ImpactCapacity)]=new BattleImpact(sequence,stepOrdinal,SimulationTime,cause,target,position,damage,killed,Session.Inventory.Forms.TryGetValue(cause,out var form)?form:null);
        }
        public AdamSession Session {get;}
        public Vector2 PlayerPosition {get;private set;}
        public IReadOnlyDictionary<int,Vector2> EnemyPositions=>new ReadOnlyDictionary<int,Vector2>(new Dictionary<int,Vector2>(enemies));
        public IReadOnlyDictionary<int,Vector2> DropPositions=>new ReadOnlyDictionary<int,Vector2>(new Dictionary<int,Vector2>(drops));
        public double SimulationTime {get;private set;}
        public int ShotsFired {get;private set;}
        public int Hits {get;private set;}
        public int Kills {get;private set;}
        public int BossKills {get;private set;}
        public int Collected {get;private set;}
        public int DamageTaken {get;private set;}
        public BattleWorld(AdamSession session,IReadOnlyDictionary<string,double> monsterSpeeds,int seed=1,BattleTuning? tuning=null)
        {
            Session=session??throw new ArgumentNullException(nameof(session));
            if (session.CombatRulesVersion == 2) lightningSettings = session.LightningSettings();
            this.tuning=(tuning??new BattleTuning()).Copy();this.tuning.Validate();
            if(monsterSpeeds==null||monsterSpeeds.Count==0||monsterSpeeds.Any(x=>string.IsNullOrWhiteSpace(x.Key)||
                !BattleSnapshot.Finite(x.Value)||x.Value<=0||x.Value>10))throw new ArgumentException("Invalid monster speeds");
            if(session.BossRules is { SummonsSupport:true } bossRules&&(!monsterSpeeds.TryGetValue(bossRules.SupportMonster,out var supportSpeed)||supportSpeed!=bossRules.SupportRatio))
                throw new ArgumentException("Unsupported boss support speed");
            if(session.RoundNumber==4&&(!monsterSpeeds.TryGetValue("d1",out var d1)||d1!=.65||!monsterSpeeds.TryGetValue("d2",out var d2)||d2!=.5||!monsterSpeeds.TryGetValue("d3",out var d3)||d3!=.6))throw new ArgumentException("Unsupported R04 monster speed");
            if(session.RoundNumber==5&&(!monsterSpeeds.TryGetValue("e1",out var e1)||e1!=E1Speed||!monsterSpeeds.TryGetValue("e2",out var e2)||e2!=E2Speed||!monsterSpeeds.TryGetValue("e3",out var e3)||e3!=E3Speed))throw new ArgumentException("Unsupported R05 monster speed");
            if(session.RoundNumber==7&&(!monsterSpeeds.TryGetValue("g1",out var g1)||g1!=.65||!monsterSpeeds.TryGetValue("g2",out var g2)||g2!=.55||!monsterSpeeds.TryGetValue("g3",out var g3)||g3!=.65))throw new ArgumentException("Unsupported R07 monster speed");
            if(session.RoundNumber==8&&(!monsterSpeeds.TryGetValue("h1",out var h1)||h1!=.65||!monsterSpeeds.TryGetValue("h2",out var h2)||h2!=.4||!monsterSpeeds.TryGetValue("h3",out var h3)||h3!=.55))throw new ArgumentException("Unsupported R08 monster speed");
            if(session.RoundNumber==9&&(!monsterSpeeds.TryGetValue("i1",out var i1)||i1!=.85||!monsterSpeeds.TryGetValue("i2",out var i2)||i2!=.6||!monsterSpeeds.TryGetValue("i3",out var i3)||i3!=.35))throw new ArgumentException("Unsupported R09 monster speed");
            if(session.RoundNumber>=LeviticusContent.GlobalRoundNumber&&!session.IsExperimental)
                foreach(var entry in CampaignContent.SpeedsFor(session.RoundNumber))
                    if(!monsterSpeeds.TryGetValue(entry.Key,out var ratio)||ratio!=entry.Value)
                        throw new ArgumentException("Unsupported Leviticus monster speed");
            if(session.RoundNumber==10||session.RoundNumber==11||session.RoundNumber==12||session.RoundNumber==13||session.RoundNumber==14||session.RoundNumber==15)foreach(var entry in ExodusContent.SpeedsFor(session.RoundNumber))
                if(!monsterSpeeds.TryGetValue(entry.Key,out var ratio)||ratio!=entry.Value)throw new ArgumentException("Unsupported Exodus monster speed");
            speeds=monsterSpeeds.ToDictionary(x=>x.Key,x=>x.Value,StringComparer.Ordinal);
            random=unchecked((uint)seed);if(random==0)random=0x9e3779b9;
            SimulationTime=session.CombatTime;
            Synchronize();
        }
        private bool Running=>Session.SimulationRunning;
        public void Advance(double seconds,Vector2 movement)
        {
            Guard.NonNegative(seconds,nameof(seconds));if(seconds>30)throw new ArgumentOutOfRangeException(nameof(seconds));
            if(!BattleSnapshot.Position(movement.X,movement.Y))throw new ArgumentException("Invalid movement");
            if(!Running){if(!Session.IsActive)ClearEnded();return;}
            if(movement.LengthSquared()>1)movement=Vector2.Normalize(movement);
            remainder+=seconds;
            while(remainder+1e-10>=FixedStep)
            {
                remainder=Math.Max(0,remainder-FixedStep);
                Step(movement);
                if(!Running){remainder=0;break;}
            }
        }
        private void Step(Vector2 movement)
        {
            stepOrdinal++;
            if(Session.Encounter!=null){StepBoss(movement);return;}
            if(movement.LengthSquared()>0)facing=Vector2.Normalize(movement);
            MovePlayer(movement);
            if(!BattleSnapshot.Position(PlayerPosition.X,PlayerPosition.Y))throw new InvalidOperationException("World coordinate limit exceeded");
            Synchronize();
            var previous=new Dictionary<int,Vector2>(enemies);
            foreach(var pair in enemies.OrderBy(x=>x.Key).ToArray())
            {
                var monster=Session.Wave!.Active[pair.Key];
                if(!speeds.TryGetValue(monster.MonsterId,out var ratio))throw new InvalidOperationException("Missing monster speed");
                if(UsesNormalCharge(Session.RoundNumber,monster.MonsterId))
                {enemies[pair.Key]=MoveCharge(charges[pair.Key],pair.Value,SimulationTime,Math.Min(180,SimulationTime+FixedStep));continue;}
                var offset=PlayerPosition-pair.Value;
                var distance=offset.Length();
                var direction=distance>1e-6?offset/distance:Vector2.Zero;
                if(distance>1e-6 && (monster.MonsterId=="a1"||monster.MonsterId=="a2"))
                {
                    var side=new Vector2(-direction.Y,direction.X);
                    direction+=side*(float)(Math.Sin(SimulationTime*3+pair.Key)*.15);
                    direction=Vector2.Normalize(direction);
                }
                enemies[pair.Key]=pair.Value+direction*(float)Math.Min(distance,tuning.PlayerSpeed*ratio*FixedStep);
            }
            foreach(var pair in enemies.OrderBy(x=>x.Key).ToArray())
            {
                if(Vector2.DistanceSquared(pair.Value,PlayerPosition)>(tuning.PlayerRadius+tuning.EnemyRadius)*(tuning.PlayerRadius+tuning.EnemyRadius))continue;
                if(Session.Wave==null||!Session.Wave.Active.TryGetValue(pair.Key,out var contact))continue;
                var before=Session.CurrentHealth;
                Session.Damage(contact.ContactDamage, IncomingDamageKind.Contact,
                    Session.HasExpandedSkill("common.armor.06") && Session.IsEliteActor(pair.Key), ArmorSurrounded());
                var received=Session.CombatRulesVersion==2
                    ? Session.LastDamageReceived : Math.Min(before,Math.Max(0,before-Session.CurrentHealth));
                DamageTaken+=(int)received;
                if(received>0)Impact("contact",0,PlayerPosition,received,!Session.IsActive);
                if(!Session.IsActive){ClearEnded();return;}
                var counter=Session.LastRetaliationDamage;
                if(counter>0)foreach(var target in enemies.OrderBy(x=>x.Key).ToArray())
                    if(Vector2.DistanceSquared(target.Value,PlayerPosition)<=tuning.BibleOrbitRadius*tuning.BibleOrbitRadius)Hit(target.Key,counter,"retaliation",target.Value);
                if(!Running)break;
            }
            if(!Running){if(!Session.IsActive)ClearEnded();return;}
            // Commit this simulation tick before a hit/pickup can open a level-up modal.
            // Otherwise the moved positions would be saved against the previous clock tick.
            Session.Advance(FixedStep);SimulationTime=Session.CombatTime;
            if(Session.Encounter!=null){EnterBossGeometry();return;}
            if(!Running){ClearEnded();return;}
            Synchronize();
            SynchronizeWeapons();
            foreach(var weapon in weapons.Values.OrderBy(x=>x.WeaponId,StringComparer.Ordinal).ToArray())
            {
                Attack(weapon, previous);
                if(!Running||Session.Encounter!=null)break;
            }
            if(Session.Encounter!=null){EnterBossGeometry();return;}
            if(Running)MoveProjectiles(previous);
            if(Session.Encounter!=null){EnterBossGeometry();return;}
            if(Running)CollectNearby();
            if(!Session.IsActive)ClearEnded();
        }
        private void Synchronize()
        {
            var active=Session.Targets;
            if(active==null)return;
            foreach(var id in enemies.Keys.Where(x=>!active.ContainsKey(x)).ToArray())enemies.Remove(id);
            foreach(var enemy in active.Values.OrderBy(x=>x.Id))if(!enemies.ContainsKey(enemy.Id))
            {
                if(Session.Encounter!=null)
                {
                    if(enemy.Id==Session.BossRules!.BossActorId)enemies.Add(enemy.Id,PlayerPosition+new Vector2(0,(float)BossDefinition.IntroOffset));
                    else
                    {
                        var angle=(enemy.Id-Session.BossRules!.BossActorId-1)*2*Math.PI/BossDefinition.SupportCount;
                        enemies.Add(enemy.Id,enemies[Session.BossRules!.BossActorId]+new Vector2((float)Math.Cos(angle),(float)Math.Sin(angle))*(float)BossDefinition.SupportRadius);
                    }
                    continue;
                }
                if(!speeds.ContainsKey(enemy.MonsterId))throw new InvalidOperationException("Missing monster speed");
                var edge=Next(4);var value=NextUnit()*2-1;
                if(Session.RoundNumber==7&&enemy.MonsterId=="g3"||Session.RoundNumber==14&&enemy.MonsterId=="ex13")edge=(Session.SpeciesOrdinal(enemy.Id)-1)%2;
                var x=edge<2?(edge==0?-1:1)*(tuning.CameraHalfWidth+tuning.SpawnMargin):value*tuning.CameraHalfWidth;
                var y=edge>=2?(edge==2?-1:1)*(tuning.CameraHalfHeight+tuning.SpawnMargin):value*tuning.CameraHalfHeight;
                enemies.Add(enemy.Id,PlayerPosition+new Vector2((float)x,(float)y));
            }
            foreach(var id in charges.Keys.Where(x=>!active.ContainsKey(x)).ToArray())charges.Remove(id);
            foreach(var actor in active.Values.Where(x=>UsesNormalCharge(Session.RoundNumber,x.MonsterId)))
                if(!charges.ContainsKey(actor.Id))charges.Add(actor.Id,new EnemyChargeState{Id=actor.Id,NextAt=(actor.SpawnedAt??throw new InvalidOperationException("Missing D3 spawn clock"))+ChargeCooldown});
            foreach(var id in drops.Keys.Where(x=>!Session.DroppedExperience.ContainsKey(x)).ToArray())drops.Remove(id);
        }
        private Vector2 MoveCharge(EnemyChargeState state,Vector2 position,double from,double to)
        {
            var cursor=from;
            while(cursor<to-1e-10)
            {
                if(state.Phase=="cooldown")
                {
                    var end=Math.Min(to,state.NextAt);var offset=PlayerPosition-position;var distance=offset.Length();
                    if(distance>1e-6)position+=offset/distance*(float)Math.Min(distance,tuning.PlayerSpeed*speeds[Session.RoundNumber==12?"ex9":"d3"]*Math.Max(0,end-cursor));
                    cursor=Math.Max(cursor,end);
                    if(state.NextAt>to+1e-9)break;
                    state.Phase="telegraph";state.Until=state.NextAt+ChargeTelegraph;state.TargetX=PlayerPosition.X;state.TargetY=PlayerPosition.Y;
                    offset=new Vector2(state.TargetX,state.TargetY)-position;
                    var direction=offset.LengthSquared()>1e-12?Vector2.Normalize(offset):Vector2.UnitX;
                    state.DirectionX=direction.X;state.DirectionY=direction.Y;
                }
                else if(state.Phase=="telegraph")
                {
                    cursor=Math.Max(cursor,Math.Min(to,state.Until));
                    if(state.Until>to+1e-9)break;
                    state.Phase="charge";state.Until+=ChargeDuration;
                }
                else
                {
                    var end=Math.Min(to,state.Until);
                    position+=new Vector2(state.DirectionX,state.DirectionY)*(float)(tuning.PlayerSpeed*ChargeSpeed*Math.Max(0,end-cursor));cursor=Math.Max(cursor,end);
                    if(state.Until>to+1e-9)break;
                    state.NextAt=state.Until+ChargeCooldown;state.Until=0;state.Phase="cooldown";
                }
            }
            if(!BattleSnapshot.Position(position.X,position.Y))throw new InvalidOperationException("World coordinate limit exceeded");
            return position;
        }
        private double TargetRadius(int id)=>Session.Encounter!=null&&id==Session.BossRules!.BossActorId?BossDefinition.Radius:tuning.EnemyRadius;
        private void EnterBossGeometry()
        {
            impactFloor=ImpactSequence+1;
            enemies.Clear();drops.Clear();projectiles.Clear();weapons.Clear();ClearExpandedAttacks();Synchronize();
        }
        private void StepBoss(Vector2 movement)
        {
            var previousPlayer=PlayerPosition;
            if(movement.LengthSquared()>0)facing=Vector2.Normalize(movement);
            MovePlayer(movement);
            if(!BattleSnapshot.Position(PlayerPosition.X,PlayerPosition.Y))throw new InvalidOperationException("World coordinate limit exceeded");
            var boss=Session.Encounter!;
            if(Session.Stage=="boss_intro")
            {
                Synchronize();Session.Advance(FixedStep);SimulationTime=Session.CombatTime;return;
            }
            var rules=Session.BossRules!;
            var previousBossTime=boss.BossElapsed;
            var attackAt=boss.NextAttackAt;
            boss.BossElapsed+=FixedStep;SimulationTime=boss.CombatTime;
            var hazardRetaliations = MoveBossHazards(boss, rules, previousPlayer);
            if(!Session.IsActive){ClearEnded();return;}
            if(!rules.UsesDash&&rules.HasTimedAttack&&!boss.Telegraphing&&boss.BossElapsed+1e-9>=attackAt-rules.TelegraphDuration)
            {
                var origin=enemies[rules.BossActorId];
                var offset=PlayerPosition-origin;
                BeginBossTelegraph(boss,rules,origin,offset.LengthSquared()>1e-12?Vector2.Normalize(offset):Vector2.UnitX);
            }
            var retaliations=hazardRetaliations+MoveHostileProjectiles(previousPlayer);
            if(!Session.IsActive){ClearEnded();return;}
            var fireChains=false;
            Synchronize();var previous=new Dictionary<int,Vector2>(enemies);
            foreach(var pair in enemies.OrderBy(x=>x.Key).ToArray())
            {
                if(pair.Key==rules.BossActorId&&rules.UsesDash)
                {enemies[pair.Key]=MoveBossDash(boss,rules,pair.Value,previousBossTime,boss.BossElapsed);continue;}
                if(pair.Key==Session.BossRules!.BossActorId&&boss.Telegraphing)continue;
                var ratio=pair.Key==Session.BossRules!.BossActorId?BossDefinition.Speed:speeds[Session.BossRules!.SupportMonster];
                var offset=PlayerPosition-pair.Value;var distance=offset.Length();
                if(distance>1e-6)enemies[pair.Key]=pair.Value+offset/distance*(float)Math.Min(distance,tuning.PlayerSpeed*ratio*FixedStep);
            }
            foreach(var pair in enemies.OrderBy(x=>x.Key).ToArray())
            {
                var radius=tuning.PlayerRadius+TargetRadius(pair.Key);
                var contact=rules.UsesDash&&pair.Key==rules.BossActorId?
                    Intersection(previous[pair.Key]-previousPlayer,pair.Value-PlayerPosition,radius).HasValue:
                    Vector2.DistanceSquared(pair.Value,PlayerPosition)<=radius*radius;
                if(!contact)continue;
                ApplyPlayerDamage(Session.Targets[pair.Key].ContactDamage,"contact",pair.Key);
                if(!Session.IsActive){ClearEnded();return;}
                if(Session.LastRetaliationDamage>0)retaliations++;
            }
            if(rules.UsesDash?boss.NextAttackAt>attackAt:boss.BossElapsed+1e-9>=attackAt)
            {
                if(!rules.UsesDash)
                {
                    boss.Telegraphing=false;boss.NextAttackAt+=rules.AttackPeriod;
                }
                fireChains=rules.UsesChainProjectiles;
                if(rules.UsesSlam&&Vector2.DistanceSquared(PlayerPosition,new Vector2(boss.TargetX,boss.TargetY))<=Math.Pow(BossDefinition.SlamRadius+tuning.PlayerRadius,2))
                {
                    ApplyPlayerDamage(Session.BossRules!.AttackDamage,"slam");
                    if(!Session.IsActive){ClearEnded();return;}
                    if(Session.LastRetaliationDamage>0)retaliations++;
                }
            }
            Session.Regenerate(FixedStep);
            // Player death above always wins over reflection or weapon kills in this tick.
            for(var i=0;i<retaliations;i++)foreach(var pair in enemies.OrderBy(x=>x.Key).ToArray())
                if(Vector2.DistanceSquared(pair.Value,PlayerPosition)<=tuning.BibleOrbitRadius*tuning.BibleOrbitRadius)
                    Hit(pair.Key,5*(Session.Inventory.Passives.TryGetValue("crown",out var rank)?rank:0)*Session.DamageMultiplier,"retaliation",pair.Value);
            if(Running)
            {
                Synchronize();SynchronizeWeapons();
                foreach(var weapon in weapons.Values.OrderBy(x=>x.WeaponId,StringComparer.Ordinal).ToArray()){Attack(weapon,previous);if(!Running)break;}
                if(Running)MoveProjectiles(previous);
                if(Running)CollectNearby();
            }
            if(Session.IsActive)
            {
                Synchronize();
                if(fireChains&&Session.Stage=="boss")
                {
                    if(hostileProjectiles.Count+3>BossDefinition.ChainCap)throw new InvalidOperationException("Hostile projectile cap exceeded");
                    var origin=enemies[Session.BossRules!.BossActorId];
                    foreach(var direction in boss.ChainDirections!)hostileProjectiles.Add(new HostileProjectileState{Id=nextHostile++,BornAt=SimulationTime,StartX=origin.X,StartY=origin.Y,X=origin.X,Y=origin.Y,DirectionX=direction.X,DirectionY=direction.Y,Lifetime=BossDefinition.ChainLifetime});
                }
            }
            else {ClearEnded();weapons.Clear();}
            Session.Capture();
        }
        private void BeginBossTelegraph(BossEncounterState boss,BossDefinition rules,Vector2 origin,Vector2 direction)
        {
            boss.Telegraphing=true;boss.TargetX=PlayerPosition.X;boss.TargetY=PlayerPosition.Y;
            if (rules.UsesAimedShot)
                boss.AimedShot = new BossAimedShotState {
                    OriginX = origin.X, OriginY = origin.Y, DirectionX = direction.X, DirectionY = direction.Y
                };
            if(!rules.UsesChainProjectiles)return;
            boss.ChainDirections=Enumerable.Range(-1,3).Select(i=>
            {
                var angle=i*BossDefinition.ChainAngleDegrees*Math.PI/180;
                return new ChainDirectionState{X=(float)(direction.X*Math.Cos(angle)-direction.Y*Math.Sin(angle)),
                    Y=(float)(direction.X*Math.Sin(angle)+direction.Y*Math.Cos(angle))};
            }).ToList();
        }

        private Vector2 MoveBossDash(BossEncounterState boss,BossDefinition rules,Vector2 position,double from,double to)
        {
            var charge=boss.ChariotCharge!;var cursor=from;
            while(cursor<to-1e-10)
            {
                var boundary=charge.Phase=="chase"?boss.NextAttackAt-rules.TelegraphDuration:charge.Until;
                var end=Math.Min(to,boundary);var elapsed=Math.Max(0,end-cursor);
                if(charge.Phase=="dash")position+=new Vector2(charge.DirectionX,charge.DirectionY)*(float)(tuning.PlayerSpeed*rules.DashSpeedRatio*elapsed);
                else if(charge.Phase=="chase")
                {
                    var offset=PlayerPosition-position;var distance=offset.Length();
                    if(distance>1e-6)position+=offset/distance*(float)Math.Min(distance,tuning.PlayerSpeed*BossDefinition.Speed*elapsed);
                }
                cursor=end;
                if(end+1e-9<boundary)break;
                if(charge.Phase=="chase")
                {
                    var offset=PlayerPosition-position;var direction=offset.LengthSquared()>1e-12?Vector2.Normalize(offset):Vector2.UnitX;
                    charge.DirectionX=direction.X;charge.DirectionY=direction.Y;
                    charge.Phase="telegraph";charge.Until=boss.NextAttackAt;
                    BeginBossTelegraph(boss,rules,position,direction);
                }
                else if(charge.Phase=="telegraph")
                {charge.Phase="dash";charge.Until=boss.NextAttackAt+rules.DashDuration;boss.NextAttackAt+=rules.AttackPeriod;boss.Telegraphing=false;}
                else {charge.Phase="chase";charge.Until=0;}
            }
            if(!BattleSnapshot.Position(position.X,position.Y))throw new InvalidOperationException("World coordinate limit exceeded");
            return position;
        }
        private int MoveBossHazards(BossEncounterState boss, BossDefinition rules, Vector2 previousPlayer)
        {
            if ((!rules.UsesAimedShot && !rules.UsesDamageZone) || boss.Telegraphing ||
                boss.NextAttackAt <= rules.AttackPeriod) return 0;

            var age = boss.SecondsSinceAttack(rules);
            var start = Math.Max(0, age - FixedStep);
            var retaliations = 0;
            if (boss.AimedShot is { } shot)
            {
                var end = Math.Min(age, BossDefinition.AimedShotLifetime);
                var endPlayer = Vector2.Lerp(previousPlayer, PlayerPosition, (float)((end - start) / FixedStep));
                if (Intersection(shot.PositionAt(start) - previousPlayer, shot.PositionAt(end) - endPlayer,
                    BossDefinition.AimedShotRadius + tuning.PlayerRadius).HasValue)
                {
                    boss.AimedShot = null;
                    ApplyPlayerDamage(rules.AttackDamage, "aimed_shot");
                    if (!Session.IsActive) return 0;
                    if (Session.LastRetaliationDamage > 0) retaliations++;
                }
                else if (age >= BossDefinition.AimedShotLifetime - 1e-9) boss.AimedShot = null;
            }
            if (rules.UsesDamageZone && start < BossDefinition.DamageZoneLifetime - 1e-9)
            {
                var center = new Vector2(boss.TargetX, boss.TargetY);
                var end = Math.Min(age, BossDefinition.DamageZoneLifetime);
                var endPlayer = Vector2.Lerp(previousPlayer, PlayerPosition, (float)((end - start) / FixedStep));
                if (Intersection(previousPlayer - center, endPlayer - center,
                    BossDefinition.DamageZoneRadius + tuning.PlayerRadius).HasValue)
                {
                    ApplyPlayerDamage(rules.AttackDamage, "damage_zone");
                    if (Session.LastRetaliationDamage > 0) retaliations++;
                }
            }
            return retaliations;
        }
        private int MoveHostileProjectiles(Vector2 previousPlayer)
        {
            var retaliations=0;
            foreach(var projectile in hostileProjectiles.OrderBy(x=>x.Id).ToArray())
            {
                var from=new Vector2(projectile.X,projectile.Y);var duration=Math.Min(FixedStep,projectile.Lifetime);
                var age=Math.Min(BossDefinition.ChainLifetime,SimulationTime-projectile.BornAt);
                var to=new Vector2((float)(projectile.StartX+projectile.DirectionX*BossDefinition.ChainSpeed*age),(float)(projectile.StartY+projectile.DirectionY*BossDefinition.ChainSpeed*age));
                var endPlayer=Vector2.Lerp(previousPlayer,PlayerPosition,(float)(duration/FixedStep));
                if(Intersection(from-previousPlayer,to-endPlayer,BossDefinition.ChainRadius+tuning.PlayerRadius).HasValue)
                {
                    hostileProjectiles.Remove(projectile);ApplyPlayerDamage(Session.BossRules!.AttackDamage,"chain");
                    if(!Session.IsActive)return retaliations;
                    if(Session.LastRetaliationDamage>0)retaliations++;
                    continue;
                }
                projectile.X=to.X;projectile.Y=to.Y;projectile.Lifetime-=duration;
                if(projectile.Lifetime<=1e-9)hostileProjectiles.Remove(projectile);
            }
            return retaliations;
        }
        private bool Surrounded()
        {
            var count = 0;
            foreach (var position in enemies.Values)
                if (Vector2.DistanceSquared(position, PlayerPosition) <= 9 && ++count == 6) return true;
            return false;
        }

        private void MovePlayer(Vector2 movement)
        {
            Session.ObserveMotion(movement);
            var surrounded = Session.HasExpandedSkill("common.move.05") && Surrounded();
            PlayerPosition += movement * (float)(tuning.PlayerSpeed * Session.MoveSpeedAt(surrounded) * FixedStep);
        }

        private bool ArmorSurrounded() => Session.HasExpandedSkill("common.armor.07") && Surrounded();

        private void ApplyPlayerDamage(int amount,string cause,int contactActor=0)
        {
            var before=Session.CurrentHealth;
            var kind = cause == "contact" ? IncomingDamageKind.Contact :
                cause == "chain" || cause == "aimed_shot" ? IncomingDamageKind.Projectile : IncomingDamageKind.Other;
            Session.Damage(amount, kind,
                Session.HasExpandedSkill("common.armor.06") && (contactActor==0 || Session.IsEliteActor(contactActor)),
                ArmorSurrounded());
            var received=Session.CombatRulesVersion==2
                ? Session.LastDamageReceived : Math.Min(before,Math.Max(0,before-Session.CurrentHealth));
            DamageTaken+=(int)received;
            if(received>0)Impact(cause,0,PlayerPosition,received,!Session.IsActive);
        }
        private void SynchronizeWeapons()
        {
            foreach(var id in Session.Inventory.Weapons.Keys.OrderBy(x=>x,StringComparer.Ordinal))
            {
                var form=Session.Inventory.Forms.TryGetValue(id,out var evolved)?evolved:null;
                if(!weapons.TryGetValue(id,out var clock)||clock.WeaponForm!=form)
                {
                    weapons[id]=new BattleWeaponState{WeaponId=id,WeaponForm=form,NextCycle=SimulationTime};
                    if (id == "lightning")
                    {
                        lightning ??= new ExpandedLightningState();
                        lightning.ResetForForm(form!=null, SimulationTime);
                    }
                    if (id == "wilderness_voice")
                    {
                        wildernessVoice ??= new ExpandedWildernessVoiceState();
                        wildernessVoice.ResetForForm(form!=null, SimulationTime);
                    }
                    if (id == "fire_pillar")
                    {
                        firePillar ??= new ExpandedFirePillarState();
                        firePillar.ResetForForm(form!=null, SimulationTime);
                    }
                    if (id == "moses_staff")
                    {
                        mosesStaff ??= new ExpandedMosesStaffState();
                        mosesStaff.ResetForForm(form!=null, SimulationTime);
                    }
                    if (id == "cross")
                    {
                        cross ??= new ExpandedCrossState();
                        cross.ResetForForm(form!=null, SimulationTime);
                    }
                    if (id == "elijah_chariot")
                    {
                        elijahChariot ??= new ExpandedElijahChariotState();
                        elijahChariot.ResetForForm(form!=null, SimulationTime);
                    }
                    if (id == "daniel_lion")
                    {
                        danielLion ??= new ExpandedDanielLionState();
                        danielLion.ResetForForm(form!=null, SimulationTime);
                    }
                }
            }
        }
        private int StrongestTarget()=>enemies.Count==0?0:enemies.Keys.OrderByDescending(Session.TargetMaxHealth).ThenBy(x=>x).First();
        private bool FormHitReady(BattleWeaponState clock,int id)
            =>clock.FormHits?.Any(x=>x.Id==id&&SimulationTime-x.At<EvolutionRules.MinimumHitInterval-1e-9)!=true;
        private void RecordFormHit(BattleWeaponState clock,int id)
        {
            if(clock.FormHits==null)clock.FormHits=new List<BattleHitState>();
            var hit=clock.FormHits.FirstOrDefault(x=>x.Id==id);
            if(hit==null)clock.FormHits.Add(new BattleHitState{Id=id,At=SimulationTime});else hit.At=SimulationTime;
            clock.FormHits.RemoveAll(x=>!enemies.ContainsKey(x.Id));
        }
        private void Pulse(BattleWeaponState clock,CombatStats profile)
        {
            if(SimulationTime+1e-9>=clock.NextPulse)
            {
                clock.NextPulse=SimulationTime+EvolutionRules.PulsePeriod;clock.PulseUntil=SimulationTime+EvolutionRules.PulseLifetime;
                clock.PulseHitIds=new List<int>();ShotsFired++;
                clock.StrengthenedFirstPulse = Session.ConsumeFirstBibleAttack();
                if (Session.CombatRulesVersion == 2)
                    clock.FrozenPulseRadius = EvolutionRules.PulseRadius * Math.Min(2.5,
                        profile.PulseRadiusMultiplier + (clock.StrengthenedFirstPulse ? .10 : 0));
            }
            var expanded = Session.CombatRulesVersion == 2;
            if(SimulationTime + (expanded ? 1e-9 : 0) >= clock.PulseUntil)return;
            var radius = expanded ? clock.FrozenPulseRadius : EvolutionRules.PulseRadius;
            foreach(var pair in enemies.OrderBy(x=>x.Key).ToArray())
            {
                if(clock.PulseHitIds!.Contains(pair.Key)||!FormHitReady(clock,pair.Key)||Vector2.DistanceSquared(pair.Value,PlayerPosition)>Math.Pow(radius+TargetRadius(pair.Key),2))continue;
                clock.PulseHitIds.Add(pair.Key);Hit(pair.Key,profile.Damage,"bible",pair.Value, original: false);RecordFormHit(clock,pair.Key);
                if(!Session.CanAttack)break;
            }
        }
        private void Attack(BattleWeaponState clock, IReadOnlyDictionary<int,Vector2> previous)
        {
            if(!Session.CanAttack)return;
            var profile=Session.AttackProfile(clock.WeaponId);
            if (RunExpandedAttack(clock, profile, previous)) return;
            var expanded = Session.CombatRulesVersion == 2;
            var projectileLimit = expanded ? Math.Min(tuning.MaxProjectiles, BattleSnapshot.ExpandedProjectileLimit) : tuning.MaxProjectiles;
            if(clock.NextCycle<=SimulationTime+1e-9)
            {
                var scheduled = expanded ? clock.NextCycle : SimulationTime;
                clock.ActiveSince=scheduled;clock.ActiveUntil=scheduled+profile.Duration;
                var cycle = clock.WeaponId == "bible" && clock.WeaponForm == null && Session.ConsumeFirstBibleAttack()
                    ? Session.AttackProfile("bible", true).Cycle : profile.Cycle;
                clock.NextCycle=scheduled+cycle;
                clock.BurstsRemaining=profile.IsOrbit?0:profile.Amount;
                clock.NextBurst=expanded&&profile.IsOrbit?0:scheduled;
                if(clock.WeaponForm==EvolutionRules.Sling)clock.FocusTargetId=StrongestTarget();
                if(profile.IsOrbit)ShotsFired+=profile.Amount;
                else for(var burst=0;burst<profile.Amount;burst++)
                    clock.PendingBursts.Add(scheduled + burst*profile.BurstInterval);
            }
            if(profile.IsOrbit)
            {
                if(clock.WeaponForm==EvolutionRules.Bible)Pulse(clock,profile);
                if(!Session.CanAttack||SimulationTime+(expanded?1e-9:0)>=clock.ActiveUntil)return;
                foreach(var pair in enemies.OrderBy(x=>x.Key).ToArray())
                {
                    var old=clock.OrbitHits.FirstOrDefault(x=>x.Id==pair.Key);
                    if(old!=null&&SimulationTime-old.At<1.7-1e-9)continue;
                    if(clock.WeaponForm==EvolutionRules.Bible&&!FormHitReady(clock,pair.Key))continue;
                    for(var book=0;book<profile.Amount;book++)
                    {
                        var angle=SimulationTime*2.5*WeaponRules.Get("bible",Session.Inventory.Weapons["bible"]).RotationSpeedMultiplier+book*2*Math.PI/profile.Amount;
                        var position=PlayerPosition+new Vector2((float)Math.Cos(angle),(float)Math.Sin(angle))*(float)(tuning.BibleOrbitRadius*profile.AreaMultiplier);
                        var strikeRadius = tuning.BookRadius * profile.OrbitCollisionMultiplier + TargetRadius(pair.Key);
                        if (Vector2.DistanceSquared(position,pair.Value)>strikeRadius*strikeRadius) continue;
                        if(old==null){old=new BattleHitState{Id=pair.Key};clock.OrbitHits.Add(old);}old.At=SimulationTime;
                        Hit(pair.Key,profile.Damage,"bible",position);
                        if(clock.WeaponForm==EvolutionRules.Bible)RecordFormHit(clock,pair.Key);break;
                    }
                    if(!Session.CanAttack)break;
                }
                clock.OrbitHits.RemoveAll(x=>!enemies.ContainsKey(x.Id));return;
            }
            foreach(var at in clock.PendingBursts.Where(x=>x<=SimulationTime+1e-9).OrderBy(x=>x).ToArray())
            {
                clock.PendingBursts.Remove(at);
                if(clock.WeaponId=="jawbone")
                {
                    ShotsFired++;
                    foreach(var pair in enemies.OrderBy(x=>x.Key).ToArray())
                    {
                        var relative=pair.Value-PlayerPosition;
                        var reach = tuning.JawboneRange * profile.AreaMultiplier;
                        if(HitGeometry.InArc(relative.X,relative.Y,facing.X,facing.Y,reach+TargetRadius(pair.Key),120))
                            Hit(pair.Key,Session.JawboneDamage(profile.Damage,relative.Length(),reach),"jawbone",pair.Value);
                        if(!Session.CanAttack)break;
                    }
                }
                else if(enemies.Count>0&&(expanded?OriginalProjectileCount:projectiles.Count)<projectileLimit)
                {
                    if(clock.WeaponForm==EvolutionRules.Sling&&!enemies.ContainsKey(clock.FocusTargetId))clock.FocusTargetId=StrongestTarget();
                    var target=clock.WeaponForm==EvolutionRules.Sling?new KeyValuePair<int,Vector2>(clock.FocusTargetId,enemies[clock.FocusTargetId]):
                        enemies.OrderBy(x=>Vector2.DistanceSquared(x.Value,PlayerPosition)).ThenBy(x=>x.Key).First();
                    var aim=target.Value-PlayerPosition;var direction=aim.LengthSquared()>1e-12?Vector2.Normalize(aim):facing;
                    projectiles.Add(new BattleProjectileState{Id=nextProjectile++,X=PlayerPosition.X,Y=PlayerPosition.Y,
                        VelocityX=direction.X*(float)(tuning.SlingSpeed*profile.ProjectileSpeedMultiplier),
                        VelocityY=direction.Y*(float)(tuning.SlingSpeed*profile.ProjectileSpeedMultiplier),Lifetime=tuning.SlingLifetime,
                        Damage=profile.Damage,RemainingPierce=profile.Pierce,Radius=.1*profile.AreaMultiplier});ShotsFired++;
                }
                if(!Session.CanAttack)break;
            }
            clock.BurstsRemaining=clock.PendingBursts.Count;
            clock.NextBurst=clock.PendingBursts.Count==0?0:clock.PendingBursts.Min();
        }
        private bool RunExpandedAttack(BattleWeaponState clock, CombatStats profile, IReadOnlyDictionary<int,Vector2> previous)
        {
            var weapon = clock.WeaponId;
            var evolved = clock.WeaponForm != null;
            switch (weapon)
            {
                case "lightning":
                    lightning ??= new ExpandedLightningState();
                    var lightningCycle = lightning.Cycle;
                    ExpandedLightning.Step(lightning, SimulationTime, PlayerPosition, Session.Inventory.Weapons[weapon],
                        evolved, profile.Damage, profile.Cycle, profile.Amount, profile.AttackRadius, () => enemies,
                        hit => ApplyExpandedHit(hit.TargetId, hit.Damage, weapon, hit.Position, Vector2.Zero, 0,
                            hit.Kind == ExpandedLightningHitKind.Strike), lightningSettings);
                    if (lightning.Cycle != lightningCycle) ShotsFired += profile.Amount;
                    return true;
                case "wilderness_voice":
                    wildernessVoice ??= new ExpandedWildernessVoiceState();
                    var voiceCycle = wildernessVoice.Cycle;
                    ExpandedWildernessVoice.Step(wildernessVoice, SimulationTime, PlayerPosition, facing,
                        Session.Inventory.Weapons[weapon], evolved, profile.Damage, profile.Cycle,
                        profile.AttackReach, profile.Duration, evolved?180:45, () => enemies,
                        hit => ApplyExpandedHit(hit.TargetId, hit.Damage, weapon, hit.Position,
                            hit.Position - hit.Center, hit.Knockback, hit.Kind == ExpandedWildernessVoiceHitKind.Primary));
                    if (wildernessVoice.Cycle != voiceCycle) ShotsFired++;
                    return true;
                case "fire_pillar":
                    firePillar ??= new ExpandedFirePillarState();
                    ShotsFired += ExpandedFirePillar.Step(firePillar, SimulationTime, PlayerPosition, evolved,
                        profile.Damage, profile.Cycle, profile.Amount, profile.AttackRadius, profile.Duration, () => enemies,
                        hit => ApplyExpandedHit(hit.TargetId, hit.Damage, weapon, hit.Position, Vector2.Zero, 0, !hit.IsEmber));
                    return true;
                case "moses_staff":
                    mosesStaff ??= new ExpandedMosesStaffState();
                    ShotsFired += ExpandedMosesStaff.Step(mosesStaff, SimulationTime, PlayerPosition, facing,
                        Session.Inventory.Weapons[weapon], evolved, profile.Damage, profile.Cycle, profile.Amount,
                        profile.AttackWidth, ExpandedWeaponRules.Get(weapon).TravelDistance, profile.AttackSpeed, () => enemies, previous,
                        hit => ApplyExpandedHit(hit.TargetId, hit.Damage, weapon, hit.Position, hit.Direction, hit.Knockback,
                            hit.Kind == ExpandedMosesStaffHitKind.Wave),
                        reserveOriginal: () => OriginalProjectileCount < 192,
                        reserveLane: () => SupportRegionCount < 16);
                    return true;
                case "cross":
                    cross ??= new ExpandedCrossState();
                    ShotsFired += ExpandedCross.Step(cross, SimulationTime, PlayerPosition, facing,
                        Session.Inventory.Weapons[weapon], evolved, profile.Damage, profile.Cycle, profile.Amount,
                        profile.AttackRadius, ExpandedWeaponRules.Get(weapon).TravelDistance, profile.AttackSpeed, () => enemies, previous,
                        hit => ApplyExpandedHit(hit.TargetId, hit.Damage, weapon, hit.Position, Vector2.Zero, 0,
                            hit.Kind != ExpandedCrossHitKind.Thorns),
                        reserveOriginal: () => OriginalProjectileCount < 192,
                        reserveTrail: () => SupportRegionCount < 16);
                    return true;
                case "elijah_chariot":
                    elijahChariot ??= new ExpandedElijahChariotState();
                    var chariotCycle = elijahChariot.Cycle;
                    ExpandedElijahChariot.Step(elijahChariot, SimulationTime, PlayerPosition, facing,
                        Session.Inventory.Weapons[weapon], evolved, profile.Damage, profile.Cycle, profile.Amount,
                        profile.AttackWidth, profile.AttackSpeed, ExpandedWeaponRules.Get(weapon).TravelDistance, () => enemies,
                        hit => ApplyExpandedHit(hit.TargetId, hit.Damage, weapon, hit.Position, Vector2.Zero, 0,
                            hit.Kind == ExpandedElijahChariotHitKind.Body));
                    if (elijahChariot.Cycle != chariotCycle) ShotsFired += profile.Amount;
                    return true;
                case "daniel_lion":
                    danielLion ??= new ExpandedDanielLionState();
                    ShotsFired += ExpandedDanielLion.Step(danielLion, SimulationTime, PlayerPosition,
                        Session.Inventory.Weapons[weapon], evolved, profile.Damage, profile.Cycle,
                        profile.Amount, profile.AttackRadius, profile.AttackSpeed, () => enemies,
                        hit => ApplyExpandedHit(hit.TargetId, hit.Damage, weapon, hit.Position,
                            hit.Position-hit.Center, hit.Knockback, hit.Kind == ExpandedDanielLionHitKind.Claw));
                    return true;
                default:
                    return false;
            }
        }

        private int OriginalProjectileCount => projectiles.Count + (mosesStaff?.ActiveWaveCount ?? 0) +
            (cross?.ActiveShotCount ?? 0) + (elijahChariot?.Paths.Count(path => !path.BodyEnded) ?? 0);

        private int SupportRegionCount => (mosesStaff?.ActiveLaneCount ?? 0) + (cross?.ActiveTrailCount ?? 0) +
            (firePillar?.Regions.Count(region => region.Ember) ?? 0) +
            (elijahChariot?.Evolved == true ? elijahChariot.Paths.Count : 0);

        private bool ApplyExpandedHit(int targetId, double damage, string weapon, Vector2 position,
            Vector2 impulse, double knockback, bool original)
        {
            if (!Session.CanAttack || !Session.Targets.TryGetValue(targetId, out var before)) return false;
            var health = before.Health;
            Hit(targetId, damage, weapon, position, original);
            if (!Session.Targets.TryGetValue(targetId, out var after)) return true;
            if (after.Health >= health) return false;
            if (knockback > 0 && targetId != Session.BossRules?.BossActorId &&
                enemies.TryGetValue(targetId, out var targetPosition))
            {
                var direction = impulse.LengthSquared() > 1e-12f ? Vector2.Normalize(impulse) : facing;
                var displaced = targetPosition + direction * (float)Math.Min(1, knockback);
                if (BattleSnapshot.Position(displaced.X, displaced.Y)) enemies[targetId] = displaced;
            }
            return true;
        }
        private void MoveProjectiles(Dictionary<int,Vector2> previous)
        {
            foreach(var projectile in projectiles.OrderBy(x=>x.Id).ToArray())
            {
                var from=projectile.Position;var duration=Math.Min(FixedStep,projectile.Lifetime);
                var to=from+projectile.Velocity*(float)duration;
                var collisions=new List<(int Id,double At)>();
                foreach(var enemy in enemies)
                {
                    if(projectile.HitIds.Contains(enemy.Key))continue;
                    var old=previous.TryGetValue(enemy.Key,out var oldPosition)?oldPosition:enemy.Value;
                    var endEnemy=old+(enemy.Value-old)*(float)(duration/FixedStep);
                    var at=Intersection(from-old,to-endEnemy,projectile.Radius+TargetRadius(enemy.Key));
                    if(at.HasValue)collisions.Add((enemy.Key,at.Value));
                }
                foreach(var collision in collisions.OrderBy(x=>x.At).ThenBy(x=>x.Id))
                {
                    if(!enemies.ContainsKey(collision.Id))continue;
                    projectile.HitIds.Add(collision.Id);projectile.RemainingPierce--;Hit(collision.Id,projectile.Damage,"sling",from+(to-from)*(float)collision.At);
                    if(projectile.RemainingPierce==0||!Running)break;
                }
                projectile.Position=to;projectile.Lifetime-=duration;
                if(projectile.RemainingPierce==0||projectile.Lifetime<=1e-9)projectiles.Remove(projectile);
                if(!Running)break;
            }
        }
        // Earliest swept-circle contact in relative coordinates (enemy motion is included).
        private static double? Intersection(Vector2 from,Vector2 to,double radius)
        {
            var delta=to-from;var c=Vector2.Dot(from,from)-radius*radius;if(c<=0)return 0;
            var a=Vector2.Dot(delta,delta);if(a<1e-16)return null;
            var b=2*Vector2.Dot(from,delta);var disc=b*b-4*a*c;if(disc<0)return null;
            var time=(-b-Math.Sqrt(disc))/(2*a);return time>=0&&time<=1?time:(double?)null;
        }
        private void Hit(int id,double damage,string cause,Vector2 contact,bool original = true)
        {
            if(!Session.CanAttack||!enemies.TryGetValue(id,out var position)||!Session.Targets.ContainsKey(id))return;
            var before=Session.Targets[id].Health;
            original = original && cause != "retaliation";
            if (original) damage = Session.RollOriginalDamage(cause, damage);
            Session.Hit(id,damage,null,original);
            if(!Session.Targets.TryGetValue(id,out var after)){Hits++;if(Session.Encounter!=null&&id==Session.BossRules!.BossActorId)BossKills++;else Kills++;enemies.Remove(id);charges.Remove(id);
                if(Session.DroppedExperience.ContainsKey(id))drops[id]=position;
                if(Session.Stage=="boss_reward"||Session.Stage=="boss_settlement")impactFloor=ImpactSequence+1;
                Impact(cause,id,contact,before,true);
            }
            else if(after.Health<before)
            {
                Hits++;Impact(cause,id,contact,before-after.Health,false);
                if(cause=="jawbone"&&Session.WeaponForm("jawbone")==EvolutionRules.Jawbone)
                {
                    var offset=position-PlayerPosition;var direction=offset.LengthSquared()>1e-12?Vector2.Normalize(offset):facing;
                    var pushed=position+direction*(float)EvolutionRules.Knockback;
                    if(!BattleSnapshot.Position(pushed.X,pushed.Y))throw new InvalidOperationException("World coordinate limit exceeded");
                    enemies[id]=pushed;
                }
            }
        }
        private void CollectNearby()
        {
            if (drops.Count == 0) return;
            var surrounded = (Session.HasExpandedSkill("common.pickup.09") || Session.HasExpandedSkill("common.xp.08")) && Surrounded();
            foreach(var pair in drops.OrderBy(x=>x.Key).ToArray())
            {
                if(Vector2.DistanceSquared(pair.Value,PlayerPosition)>Math.Pow(tuning.PickupRadius*Session.PickupRangeAt(surrounded),2))continue;
                var existed=Session.DroppedExperience.ContainsKey(pair.Key);Session.Collect(pair.Key, surrounded);
                if(existed&&!Session.DroppedExperience.ContainsKey(pair.Key)){drops.Remove(pair.Key);Collected++;}
                if(!Running)break;
            }
        }
        private void ClearExpandedAttacks()
        {
            lightning=null; wildernessVoice=null; firePillar=null; mosesStaff=null; cross=null; elijahChariot=null; danielLion=null;
        }
        private void ClearEnded(){hostileProjectiles.Clear();enemies.Clear();drops.Clear();projectiles.Clear();charges.Clear();ClearExpandedAttacks();}
        private uint NextUInt(){random^=random<<13;random^=random>>17;random^=random<<5;return random;}
        private int Next(int bound)=>(int)(NextUInt()%(uint)bound);
        private double NextUnit()=>NextUInt()/(double)uint.MaxValue;
        public BattleSnapshot Capture()=>new BattleSnapshot{Time=SimulationTime,Remainder=remainder,PlayerX=PlayerPosition.X,PlayerY=PlayerPosition.Y,
            FacingX=facing.X,FacingY=facing.Y,RandomState=random,NextProjectileId=nextProjectile,ShotsFired=ShotsFired,Hits=Hits,Kills=Kills,
            Collected=Collected,DamageTaken=DamageTaken,BossKills=BossKills,SchemaVersion=Session.CombatRulesVersion==2
                ? BattleSnapshot.ExpandedVersion
                : Session.RoundNumber>=4?Session.BossRules?.WorldVersion??3:Session.Encounter!=null||Session.Inventory.Forms.Count>0?2:1,
            HostileProjectiles=Session.BossRules?.UsesChainProjectiles==true?hostileProjectiles.OrderBy(x=>x.Id).Select(x=>x.Copy()).ToList():null,NextHostileId=Session.BossRules?.UsesChainProjectiles==true?nextHostile:0,
            Lightning=lightning?.Copy(),
            WildernessVoice=wildernessVoice?.Copy(), FirePillar=firePillar?.Copy(),
            MosesStaff=mosesStaff?.Copy(),
            Cross=cross?.Copy(), ElijahChariot=elijahChariot?.Copy(),
            DanielLion=danielLion?.Copy(),
            Charges=Session.RoundNumber>=4?charges.Values.OrderBy(x=>x.Id).Select(x=>x.Copy()).ToList():null,Tuning=tuning.Copy(),
            Enemies=enemies.OrderBy(x=>x.Key).Select(x=>new BattleEntityState{Id=x.Key,X=x.Value.X,Y=x.Value.Y}).ToList(),
            Drops=drops.OrderBy(x=>x.Key).Select(x=>new BattleEntityState{Id=x.Key,X=x.Value.X,Y=x.Value.Y}).ToList(),
            Projectiles=projectiles.Select(x=>x.Copy()).ToList(),Weapons=weapons.Values.OrderBy(x=>x.WeaponId,StringComparer.Ordinal).Select(x=>x.Copy()).ToList(),
            MonsterSpeeds=speeds.OrderBy(x=>x.Key,StringComparer.Ordinal).Select(x=>new BattleSpeedState{MonsterId=x.Key,Ratio=x.Value}).ToList()};
        public static BattleWorld Restore(AdamSession session,IReadOnlyDictionary<string,double> speeds,BattleSnapshot snapshot,BattleTuning? tuning=null)
        {
            if(snapshot==null)throw new ArgumentNullException(nameof(snapshot));snapshot.Validate();
            if(tuning!=null&&!snapshot.Tuning.Same(tuning))throw new ArgumentException("Checkpoint tuning mismatch");
            var world=new BattleWorld(session,speeds,1,snapshot.Tuning);
            if(snapshot.MonsterSpeeds.Count!=speeds.Count||snapshot.MonsterSpeeds.Any(x=>!speeds.TryGetValue(x.MonsterId,out var ratio)||ratio!=x.Ratio))
                throw new ArgumentException("Checkpoint monster catalog mismatch");
            if(Math.Abs((session.Encounter!=null?session.CombatTime:session.Wave?.Elapsed??snapshot.Time)-snapshot.Time)>1e-7||
                !snapshot.Enemies.Select(x=>x.Id).OrderBy(x=>x).SequenceEqual(session.Targets.Keys.OrderBy(x=>x))||
                !snapshot.Drops.Select(x=>x.Id).OrderBy(x=>x).SequenceEqual(session.DroppedExperience.Keys.OrderBy(x=>x))||
                snapshot.Weapons.Any(x=>!session.Inventory.Weapons.ContainsKey(x.WeaponId)||x.WeaponForm!=(session.Inventory.Forms.TryGetValue(x.WeaponId,out var f)?f:null)))throw new ArgumentException("Checkpoint does not match session");
            var expanded = session.CombatRulesVersion == 2;
            if ((snapshot.SchemaVersion == BattleSnapshot.ExpandedVersion) != expanded)
                throw new ArgumentException("Checkpoint combat rules do not match session");
            if(session.RoundNumber>=4&&(!expanded&&snapshot.SchemaVersion!=(session.BossRules?.WorldVersion??3)||snapshot.Charges==null||!snapshot.Charges.Select(x=>x.Id).OrderBy(x=>x).SequenceEqual(session.Targets.Values.Where(x=>UsesNormalCharge(session.RoundNumber,x.MonsterId)).Select(x=>x.Id).OrderBy(x=>x))))throw new ArgumentException("Charge checkpoint does not match actors");
            world.PlayerPosition=new Vector2(snapshot.PlayerX,snapshot.PlayerY);world.facing=new Vector2(snapshot.FacingX,snapshot.FacingY);
            world.random=snapshot.RandomState;world.remainder=snapshot.Remainder;world.SimulationTime=snapshot.Time;world.nextProjectile=snapshot.NextProjectileId;
            world.BossKills=snapshot.BossKills;world.ShotsFired=snapshot.ShotsFired;world.Hits=snapshot.Hits;world.Kills=snapshot.Kills;world.Collected=snapshot.Collected;world.DamageTaken=snapshot.DamageTaken;
            if(snapshot.HostileProjectiles!=null)world.hostileProjectiles.AddRange(snapshot.HostileProjectiles.Select(x=>x.Copy()));world.nextHostile=snapshot.NextHostileId==0?1:snapshot.NextHostileId;
            world.enemies.Clear();foreach(var enemy in snapshot.Enemies)world.enemies.Add(enemy.Id,enemy.Position);
            world.charges.Clear();if(snapshot.Charges!=null)foreach(var charge in snapshot.Charges)world.charges.Add(charge.Id,charge.Copy());
            world.drops.Clear();foreach(var drop in snapshot.Drops)world.drops.Add(drop.Id,drop.Position);
            world.projectiles.Clear();world.projectiles.AddRange(snapshot.Projectiles.Select(x=>x.Copy()));
            world.weapons.Clear();foreach(var clock in snapshot.Weapons)world.weapons.Add(clock.WeaponId,clock.Copy());
            world.lightning = snapshot.Lightning?.Copy();
            world.wildernessVoice = snapshot.WildernessVoice?.Copy();
            world.firePillar = snapshot.FirePillar?.Copy();
            world.mosesStaff = snapshot.MosesStaff?.Copy();
            world.cross = snapshot.Cross?.Copy();
            world.elijahChariot = snapshot.ElijahChariot?.Copy();
            world.danielLion = snapshot.DanielLion?.Copy();
            return world;
        }
    }
}
