using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
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
        internal BattleWeaponState Copy()
        {var copy=(BattleWeaponState)MemberwiseClone();copy.OrbitHits=OrbitHits.Select(x=>x.Copy()).ToList();copy.PendingBursts=new List<double>(PendingBursts);return copy;}
    }
    [DataContract]
    public sealed class BattleSpeedState
    {
        [DataMember] public string MonsterId {get;set;}="";
        [DataMember] public double Ratio {get;set;}
    }
    [DataContract]
    public sealed class BattleSnapshot
    {
        [DataMember] public int SchemaVersion {get;set;}=1;
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
            if(SchemaVersion!=1||Tuning==null)throw new ArgumentException("Unsupported battle snapshot");
            Tuning.Validate();
            if(!Finite(Time)||Time<0||Time>180.000001||!Finite(Remainder)||Remainder<0||Remainder>=BattleWorld.FixedStep+
                1e-9||!Position(PlayerX,PlayerY)||!Position(FacingX,FacingY)||
                Math.Abs((double)FacingX*FacingX+(double)FacingY*FacingY-1)>.0001||RandomState==0||NextProjectileId<1||
                ShotsFired<0||Hits<0||Kills<0||Collected<0||DamageTaken<0||Enemies==null||Drops==null||Projectiles==null||Weapons==null||MonsterSpeeds==null||
                Enemies.Count>120||Drops.Count>6210||Projectiles.Count>Tuning.MaxProjectiles||Weapons.Count>6)
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
                if((w.WeaponId!="bible"&&w.WeaponId!="jawbone"&&w.WeaponId!="sling")||!Finite(w.NextCycle)||w.NextCycle<0||
                    !Finite(w.ActiveUntil)||w.ActiveUntil<0||!Finite(w.ActiveSince)||w.ActiveSince< -1||
                    !Finite(w.NextBurst)||w.NextBurst<0||w.BurstsRemaining<0||w.BurstsRemaining>1007||w.OrbitHits==null||
                    w.PendingBursts==null||w.PendingBursts.Count>10000||w.PendingBursts.Any(x=>!Finite(x)||x<0)||
                    w.OrbitHits.Any(x=>x==null)||
                    w.OrbitHits.Select(x=>x.Id).Distinct().Count()!=w.OrbitHits.Count||
                    w.OrbitHits.Any(x=>x.Id<=0||!Finite(x.At)||x.At<0||x.At>Time+1e-8))
                    throw new ArgumentException("Invalid weapon clock");
            }
        }
        private static void ValidateEntities(List<BattleEntityState> entries)
        {if(entries.Any(x=>x==null||x.Id<1||!Position(x.X,x.Y))||entries.Select(x=>x.Id).Distinct().Count()!=entries.Count)
            throw new ArgumentException("Invalid entity snapshot");}
        internal static bool Finite(double n)=>!double.IsNaN(n)&&!double.IsInfinity(n);
        internal static bool Position(float x,float y)=>Finite(x)&&Finite(y)&&Math.Abs(x)<=1000000&&Math.Abs(y)<=1000000;
    }
    public sealed class BattleWorld
    {
        public const double FixedStep=1.0/60;
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
        public AdamSession Session {get;}
        public Vector2 PlayerPosition {get;private set;}
        public IReadOnlyDictionary<int,Vector2> EnemyPositions=>new ReadOnlyDictionary<int,Vector2>(new Dictionary<int,Vector2>(enemies));
        public IReadOnlyDictionary<int,Vector2> DropPositions=>new ReadOnlyDictionary<int,Vector2>(new Dictionary<int,Vector2>(drops));
        public double SimulationTime {get;private set;}
        public int ShotsFired {get;private set;}
        public int Hits {get;private set;}
        public int Kills {get;private set;}
        public int Collected {get;private set;}
        public int DamageTaken {get;private set;}
        public BattleWorld(AdamSession session,IReadOnlyDictionary<string,double> monsterSpeeds,int seed=1,BattleTuning? tuning=null)
        {
            Session=session??throw new ArgumentNullException(nameof(session));
            this.tuning=(tuning??new BattleTuning()).Copy();this.tuning.Validate();
            if(monsterSpeeds==null||monsterSpeeds.Count==0||monsterSpeeds.Any(x=>string.IsNullOrWhiteSpace(x.Key)||
                !BattleSnapshot.Finite(x.Value)||x.Value<=0||x.Value>10))throw new ArgumentException("Invalid monster speeds");
            speeds=monsterSpeeds.ToDictionary(x=>x.Key,x=>x.Value,StringComparer.Ordinal);
            random=unchecked((uint)seed);if(random==0)random=0x9e3779b9;
            SimulationTime=session.Wave?.Elapsed??0;
            Synchronize();
        }
        private bool Running=>Session.IsActive&&Session.Wave!=null&&!Session.Wave.Paused&&Session.PendingChoices.Count==0;
        public void Advance(double seconds,Vector2 movement)
        {
            Guard.NonNegative(seconds,nameof(seconds));if(seconds>30)throw new ArgumentOutOfRangeException(nameof(seconds));
            if(!BattleSnapshot.Position(movement.X,movement.Y))throw new ArgumentException("Invalid movement");
            if(!Running)return;
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
            if(movement.LengthSquared()>0)facing=Vector2.Normalize(movement);
            PlayerPosition+=movement*(float)(tuning.PlayerSpeed*Session.MoveSpeedMultiplier*FixedStep);
            if(!BattleSnapshot.Position(PlayerPosition.X,PlayerPosition.Y))throw new InvalidOperationException("World coordinate limit exceeded");
            Synchronize();
            var previous=new Dictionary<int,Vector2>(enemies);
            foreach(var pair in enemies.OrderBy(x=>x.Key).ToArray())
            {
                var monster=Session.Wave!.Active[pair.Key];
                if(!speeds.TryGetValue(monster.MonsterId,out var ratio))throw new InvalidOperationException("Missing monster speed");
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
                var before=Session.CurrentHealth;Session.Damage(contact.ContactDamage);
                DamageTaken+=(int)Math.Min(before,Math.Max(0,before-Session.CurrentHealth));
                if(!Session.IsActive){ClearEnded();return;}
                var counter=Session.LastRetaliationDamage;
                if(counter>0)foreach(var target in enemies.OrderBy(x=>x.Key).ToArray())
                    if(Vector2.DistanceSquared(target.Value,PlayerPosition)<=tuning.BibleOrbitRadius*tuning.BibleOrbitRadius)Hit(target.Key,counter);
                if(!Running)break;
            }
            if(!Running){if(!Session.IsActive)ClearEnded();return;}
            // Commit this simulation tick before a hit/pickup can open a level-up modal.
            // Otherwise the moved positions would be saved against the previous clock tick.
            Session.Advance(FixedStep);SimulationTime=Session.Wave?.Elapsed??SimulationTime;
            if(!Running){ClearEnded();return;}
            Synchronize();
            SynchronizeWeapons();
            foreach(var weapon in weapons.Values.OrderBy(x=>x.WeaponId,StringComparer.Ordinal).ToArray())
            {
                Attack(weapon);
                if(!Running)break;
            }
            if(Running)MoveProjectiles(previous);
            if(Running)CollectNearby();
            if(!Session.IsActive)ClearEnded();
        }
        private void Synchronize()
        {
            var active=Session.Wave?.Active;
            if(active==null)return;
            foreach(var id in enemies.Keys.Where(x=>!active.ContainsKey(x)).ToArray())enemies.Remove(id);
            foreach(var enemy in active.Values.OrderBy(x=>x.Id))if(!enemies.ContainsKey(enemy.Id))
            {
                if(!speeds.ContainsKey(enemy.MonsterId))throw new InvalidOperationException("Missing monster speed");
                var edge=Next(4);var value=NextUnit()*2-1;
                var x=edge<2?(edge==0?-1:1)*(tuning.CameraHalfWidth+tuning.SpawnMargin):value*tuning.CameraHalfWidth;
                var y=edge>=2?(edge==2?-1:1)*(tuning.CameraHalfHeight+tuning.SpawnMargin):value*tuning.CameraHalfHeight;
                enemies.Add(enemy.Id,PlayerPosition+new Vector2((float)x,(float)y));
            }
            foreach(var id in drops.Keys.Where(x=>!Session.DroppedExperience.ContainsKey(x)).ToArray())drops.Remove(id);
        }
        private void SynchronizeWeapons()
        {
            foreach(var id in Session.Inventory.Weapons.Keys.OrderBy(x=>x,StringComparer.Ordinal))
                if(!weapons.ContainsKey(id))weapons.Add(id,new BattleWeaponState{WeaponId=id,NextCycle=SimulationTime});
        }
        private void Attack(BattleWeaponState clock)
        {
            var profile=Session.AttackProfile(clock.WeaponId);
            if(clock.NextCycle<=SimulationTime+1e-9)
            {
                clock.ActiveSince=SimulationTime;clock.ActiveUntil=SimulationTime+profile.Duration;
                clock.NextCycle=SimulationTime+profile.Cycle;
                clock.BurstsRemaining=profile.IsOrbit?0:profile.Amount;
                clock.NextBurst=SimulationTime;
                if(profile.IsOrbit)ShotsFired+=profile.Amount;
                else for(var burst=0;burst<profile.Amount;burst++)clock.PendingBursts.Add(SimulationTime+
                    burst*WeaponRules.Get(clock.WeaponId,Session.Inventory.Weapons[clock.WeaponId]).BurstInterval);
            }
            if(profile.IsOrbit)
            {
                if(SimulationTime>=clock.ActiveUntil)return;
                foreach(var pair in enemies.OrderBy(x=>x.Key).ToArray())
                {
                    var old=clock.OrbitHits.FirstOrDefault(x=>x.Id==pair.Key);
                    if(old!=null&&SimulationTime-old.At<1.7-1e-9)continue;
                    for(var book=0;book<profile.Amount;book++)
                    {
                        var angle=SimulationTime*2.5*WeaponRules.Get("bible",Session.Inventory.Weapons["bible"]).RotationSpeedMultiplier+book*2*Math.PI/profile.Amount;
                        var position=PlayerPosition+new Vector2((float)Math.Cos(angle),(float)Math.Sin(angle))*(float)(tuning.BibleOrbitRadius*profile.AreaMultiplier);
                        if(Vector2.DistanceSquared(position,pair.Value)>(tuning.BookRadius+tuning.EnemyRadius)*(tuning.BookRadius+tuning.EnemyRadius))continue;
                        if(old==null){old=new BattleHitState{Id=pair.Key};clock.OrbitHits.Add(old);}old.At=SimulationTime;
                        Hit(pair.Key,profile.Damage);break;
                    }
                    if(!Running)break;
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
                        if(HitGeometry.InArc(relative.X,relative.Y,facing.X,facing.Y,tuning.JawboneRange*profile.AreaMultiplier+tuning.EnemyRadius,120))Hit(pair.Key,profile.Damage);
                        if(!Running)break;
                    }
                }
                else if(enemies.Count>0&&projectiles.Count<tuning.MaxProjectiles)
                {
                    var target=enemies.OrderBy(x=>Vector2.DistanceSquared(x.Value,PlayerPosition)).ThenBy(x=>x.Key).First();
                    var aim=target.Value-PlayerPosition;var direction=aim.LengthSquared()>1e-12?Vector2.Normalize(aim):facing;
                    projectiles.Add(new BattleProjectileState{Id=nextProjectile++,X=PlayerPosition.X,Y=PlayerPosition.Y,
                        VelocityX=direction.X*(float)(tuning.SlingSpeed*profile.ProjectileSpeedMultiplier),
                        VelocityY=direction.Y*(float)(tuning.SlingSpeed*profile.ProjectileSpeedMultiplier),Lifetime=tuning.SlingLifetime,
                        Damage=profile.Damage,RemainingPierce=profile.Pierce,Radius=.1*profile.AreaMultiplier});ShotsFired++;
                }
                if(!Running)break;
            }
            clock.BurstsRemaining=clock.PendingBursts.Count;
            clock.NextBurst=clock.PendingBursts.Count==0?0:clock.PendingBursts.Min();
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
                    var at=Intersection(from-old,to-endEnemy,projectile.Radius+tuning.EnemyRadius);
                    if(at.HasValue)collisions.Add((enemy.Key,at.Value));
                }
                foreach(var collision in collisions.OrderBy(x=>x.At).ThenBy(x=>x.Id))
                {
                    if(!enemies.ContainsKey(collision.Id))continue;
                    projectile.HitIds.Add(collision.Id);projectile.RemainingPierce--;Hit(collision.Id,projectile.Damage);
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
        private void Hit(int id,double damage)
        {
            if(!Running||!enemies.TryGetValue(id,out var position)||Session.Wave==null)return;
            var before=Session.Wave.Active[id].Health;Session.Hit(id,damage);
            if(Session.Wave==null||!Session.Wave.Active.TryGetValue(id,out var after)){Hits++;Kills++;enemies.Remove(id);
                if(Session.DroppedExperience.ContainsKey(id))drops[id]=position;
            }
            else if(after.Health<before)Hits++;
        }
        private void CollectNearby()
        {
            foreach(var pair in drops.OrderBy(x=>x.Key).ToArray())
            {
                if(Vector2.DistanceSquared(pair.Value,PlayerPosition)>Math.Pow(tuning.PickupRadius*Session.PickupRangeMultiplier,2))continue;
                var existed=Session.DroppedExperience.ContainsKey(pair.Key);Session.Collect(pair.Key);
                if(existed&&!Session.DroppedExperience.ContainsKey(pair.Key)){drops.Remove(pair.Key);Collected++;}
                if(!Running)break;
            }
        }
        private void ClearEnded(){enemies.Clear();drops.Clear();projectiles.Clear();}
        private uint NextUInt(){random^=random<<13;random^=random>>17;random^=random<<5;return random;}
        private int Next(int bound)=>(int)(NextUInt()%(uint)bound);
        private double NextUnit()=>NextUInt()/(double)uint.MaxValue;
        public BattleSnapshot Capture()=>new BattleSnapshot{Time=SimulationTime,Remainder=remainder,PlayerX=PlayerPosition.X,PlayerY=PlayerPosition.Y,
            FacingX=facing.X,FacingY=facing.Y,RandomState=random,NextProjectileId=nextProjectile,ShotsFired=ShotsFired,Hits=Hits,Kills=Kills,
            Collected=Collected,DamageTaken=DamageTaken,Tuning=tuning.Copy(),
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
            if(Math.Abs((session.Wave?.Elapsed??snapshot.Time)-snapshot.Time)>1e-7||
                !snapshot.Enemies.Select(x=>x.Id).OrderBy(x=>x).SequenceEqual((session.IsActive?session.Wave!.Active.Keys:Array.Empty<int>()).OrderBy(x=>x))||
                !snapshot.Drops.Select(x=>x.Id).OrderBy(x=>x).SequenceEqual(session.DroppedExperience.Keys.OrderBy(x=>x))||
                snapshot.Weapons.Any(x=>!session.Inventory.Weapons.ContainsKey(x.WeaponId)))throw new ArgumentException("Checkpoint does not match session");
            world.PlayerPosition=new Vector2(snapshot.PlayerX,snapshot.PlayerY);world.facing=new Vector2(snapshot.FacingX,snapshot.FacingY);
            world.random=snapshot.RandomState;world.remainder=snapshot.Remainder;world.SimulationTime=snapshot.Time;world.nextProjectile=snapshot.NextProjectileId;
            world.ShotsFired=snapshot.ShotsFired;world.Hits=snapshot.Hits;world.Kills=snapshot.Kills;world.Collected=snapshot.Collected;world.DamageTaken=snapshot.DamageTaken;
            world.enemies.Clear();foreach(var enemy in snapshot.Enemies)world.enemies.Add(enemy.Id,enemy.Position);
            world.drops.Clear();foreach(var drop in snapshot.Drops)world.drops.Add(drop.Id,drop.Position);
            world.projectiles.Clear();world.projectiles.AddRange(snapshot.Projectiles.Select(x=>x.Copy()));
            world.weapons.Clear();foreach(var clock in snapshot.Weapons)world.weapons.Add(clock.WeaponId,clock.Copy());
            return world;
        }
    }
}
