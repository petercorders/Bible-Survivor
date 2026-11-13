using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.IO;
using System.Runtime.Serialization.Json;
using BibleSurvivor.Core;

namespace BibleSurvivor.Core.Tests;

public static class BattleWorldTests
{
    private static readonly IReadOnlyDictionary<string,double> Speeds=new Dictionary<string,double>{{"a1",.45},{"a2",.6},{"a3",.35}};
    private static BattleWorld Make(string weapon="sling",BattleTuning? tuning=null) => new BattleWorld(
        new AdamSession(GameSnapshot.New(),WaveTests.Adam(),weapon),Speeds,7,tuning);
    private static void Resolve(BattleWorld world)
    { while(world.Session.PendingChoices.Count>0) world.Session.Choose(world.Session.PendingChoices.First(x=>x!="scroll")); }
    private static void Run(BattleWorld world,int ticks,Vector2 input)
    {for(var i=0;i<ticks&&world.Session.IsActive;i++){Resolve(world);world.Advance(1.0/60,input);}}
    private static BattleWorld Controlled(int count=1,int health=100,string weapon="jawbone")
    {
        var definition=new RoundDefinition(1,40,count,25,new[]{new WaveDefinition(0,1,new[]{new SpawnGroup("a1",count,health,4)})});
        var session=new AdamSession(GameSnapshot.New(),definition,weapon);session.Advance(1);
        return new BattleWorld(session,new Dictionary<string,double>{{"a1",.0001}},7);
    }
    public static void Register(Action<string,Action> test)
    {
        test("Battle input normalizes diagonals and preserves facing when released",()=>{
            var a=Make();var b=Make();a.Advance(1,new Vector2(1,0));b.Advance(1,new Vector2(1,1));
            Check.Near(a.PlayerPosition.Length(),b.PlayerPosition.Length(),1e-5);
            var facing=b.Capture().FacingX;b.Advance(.1,Vector2.Zero);Check.Near(facing,b.Capture().FacingX);
        });
        test("Battle spawns are seeded and outside camera rectangle",()=>{
            var a=Make();var b=Make();a.Advance(1,Vector2.Zero);b.Advance(1,Vector2.Zero);
            Check.Equal(a.EnemyPositions.Count,b.EnemyPositions.Count);
            foreach(var pair in a.EnemyPositions){Check.Equal(pair.Value,b.EnemyPositions[pair.Key]);
                Check.True(Math.Abs(pair.Value.X)>9.9 || Math.Abs(pair.Value.Y)>5.5);}
        });
        test("Battle enemies pursue the player without injected kills",()=>{
            var world=Make("jawbone");world.Advance(1,Vector2.Zero);var first=world.EnemyPositions.First();
            world.Advance(1,Vector2.Zero);Check.True(world.EnemyPositions[first.Key].Length()<first.Value.Length());
            Check.Equal(0,world.Kills);
        });
        foreach(var weapon in new[]{"bible","jawbone","sling"})
            test("Battle "+weapon+" auto attacks, kills and collects real drops",()=>{
                var world=Make(weapon,new BattleTuning{CameraHalfWidth=2,CameraHalfHeight=1.5,SpawnMargin=.5,PickupRadius=2});
                Run(world,1200,Vector2.Zero);
                Check.True(world.ShotsFired>0);Check.True(world.Hits>0);Check.True(world.Kills>0);Check.True(world.Collected>0);
            });
        test("Battle pause and level choices freeze movement, enemies and clocks",()=>{
            var world=Make();world.Advance(1,Vector2.Zero);var before=world.Capture();world.Session.Pause(true);
            world.Advance(10,Vector2.One);Check.Near(before.Time,world.SimulationTime);Check.Equal(Vector2.Zero,world.PlayerPosition);
            Check.Equal(before.RandomState,world.Capture().RandomState);world.Session.Pause(false);
            world.Session.Inventory.TryChoose("scroll",true); // Independent inventory operation does not reset the world clock.
            world.Advance(.1,Vector2.Zero);Check.Near(before.Time+.1,world.SimulationTime);
            var leveling=Make("sling",new BattleTuning{CameraHalfWidth=2,CameraHalfHeight=1.5,SpawnMargin=.5,PickupRadius=2});
            for(var i=0;i<3000&&leveling.Session.IsActive&&leveling.Session.PendingChoices.Count==0;i++)leveling.Advance(BattleWorld.FixedStep,Vector2.Zero);
            Check.True(leveling.Session.PendingChoices.Count>0);var pending=leveling.Capture();
            leveling.Advance(10,Vector2.One);Check.Near(pending.Time,leveling.SimulationTime);
            Check.Equal(pending.PlayerX,leveling.PlayerPosition.X);Check.Equal(pending.PlayerY,leveling.PlayerPosition.Y);
            Check.Equal(pending.ShotsFired,leveling.ShotsFired);Check.Equal(pending.RandomState,leveling.Capture().RandomState);
        });
        test("Battle fixed step gives the same result across frame partitions",()=>{
            var a=Make();var b=Make();a.Advance(3,Vector2.UnitX);
            for(var i=0;i<300;i++)b.Advance(.01,Vector2.UnitX);
            Check.Near(a.SimulationTime,b.SimulationTime);Check.Equal(a.PlayerPosition,b.PlayerPosition);
            Check.Equal(a.ShotsFired,b.ShotsFired);Check.Equal(a.Capture().RandomState,b.Capture().RandomState);
            foreach(var enemy in a.EnemyPositions)Check.Equal(enemy.Value,b.EnemyPositions[enemy.Key]);
        });
        test("Battle snapshot restores exact positions, projectiles, timers and seeded future",()=>{
            var state=GameSnapshot.New();var session=new AdamSession(state,WaveTests.Adam(),"sling");
            var a=new BattleWorld(session,Speeds,7);a.Advance(2.007,Vector2.UnitX);session.Capture();
            using var bytes=new MemoryStream();var codec=new DataContractJsonSerializer(typeof(GameSnapshot));
            codec.WriteObject(bytes,state);bytes.Position=0;var independent=(GameSnapshot)codec.ReadObject(bytes)!;
            var restored=AdamSession.Resume(independent,WaveTests.Adam());var b=BattleWorld.Restore(restored,Speeds,a.Capture());
            Run(a,300,Vector2.UnitY);Run(b,300,Vector2.UnitY);
            Check.Equal(a.PlayerPosition,b.PlayerPosition);Check.Equal(a.Kills,b.Kills);Check.Equal(a.ShotsFired,b.ShotsFired);
            Check.Equal(a.Capture().RandomState,b.Capture().RandomState);
            foreach(var enemy in a.EnemyPositions)Check.Equal(enemy.Value,b.EnemyPositions[enemy.Key]);
        });
        test("Battle checkpoint rejects mismatched session enemies and invalid coordinates",()=>{
            var world=Make();world.Advance(2,Vector2.Zero);var snap=world.Capture();snap.PlayerX=float.NaN;
            Check.Throws<ArgumentException>(()=>BattleWorld.Restore(world.Session,Speeds,snap));
            snap=world.Capture();snap.Enemies.Clear();Check.Throws<ArgumentException>(()=>BattleWorld.Restore(world.Session,Speeds,snap));
            var changed=new Dictionary<string,double>{{"a1",.5},{"a2",.6},{"a3",.35}};
            Check.Throws<ArgumentException>(()=>BattleWorld.Restore(world.Session,changed,world.Capture()));
        });
        test("Battle projectiles obey cap and expire without a target",()=>{
            var world=Make("sling",new BattleTuning{MaxProjectiles=1,SlingLifetime=.05});
            Run(world,180,Vector2.Zero);Check.True(world.Capture().Projectiles.Count<=1);
            Check.True(world.Capture().Projectiles.All(x=>x.Lifetime<=.05));
        });
        test("Battle validates time, movement and tuning",()=>{
            var world=Make();Check.Throws<ArgumentOutOfRangeException>(()=>world.Advance(double.NaN,Vector2.Zero));
            Check.Throws<ArgumentOutOfRangeException>(()=>world.Advance(31,Vector2.Zero));
            Check.Throws<ArgumentException>(()=>world.Advance(.1,new Vector2(float.PositiveInfinity,0)));
            Check.Throws<ArgumentException>(()=>Make(tuning:new BattleTuning{PlayerSpeed=-1}));
        });
        test("Battle snapshot is independent and pending projectiles cannot hit the same enemy twice",()=>{
            var world=Make();world.Advance(3,Vector2.Zero);var snap=world.Capture();
            var count=world.EnemyPositions.Count;snap.Enemies.Clear();Check.Equal(count,world.EnemyPositions.Count);
            foreach(var projectile in world.Capture().Projectiles) Check.Equal(projectile.HitIds.Count,projectile.HitIds.Distinct().Count());
            var controlled=Controlled();var checkpoint=controlled.Capture();checkpoint.FacingX=-1;
            checkpoint.Enemies[0].X=1;checkpoint.Enemies[0].Y=0;checkpoint.NextProjectileId=2;
            checkpoint.Projectiles.Add(new BattleProjectileState{Id=1,X=1,Y=0,Lifetime=.5,Damage=10,RemainingPierce=2,Radius=.1});
            var restored=BattleWorld.Restore(controlled.Session,new Dictionary<string,double>{{"a1",.0001}},checkpoint);
            restored.Advance(2*BattleWorld.FixedStep,Vector2.Zero);
            Check.Near(90,restored.Session.Wave!.Active.Values.Single().Health);
        });
        test("Battle swept projectile hits across a frame and stops at its pierce limit",()=>{
            var world=Controlled(2);var checkpoint=world.Capture();checkpoint.FacingX=-1;
            checkpoint.Enemies[0].X=1;checkpoint.Enemies[0].Y=0;checkpoint.Enemies[1].X=2;checkpoint.Enemies[1].Y=0;
            checkpoint.NextProjectileId=2;checkpoint.Projectiles.Add(new BattleProjectileState{Id=1,VelocityX=240,
                Lifetime=1,Damage=10,RemainingPierce=1,Radius=.1});
            var restored=BattleWorld.Restore(world.Session,new Dictionary<string,double>{{"a1",.0001}},checkpoint);
            restored.Advance(BattleWorld.FixedStep,Vector2.Zero);
            Check.Near(90,restored.Session.Wave!.Active[1].Health);Check.Near(100,restored.Session.Wave.Active[2].Health);
            Check.Equal(0,restored.Capture().Projectiles.Count);
        });
        test("Battle jawbone arc excludes behind enemies and remembers the last facing",()=>{
            var world=Controlled(2);var checkpoint=world.Capture();
            checkpoint.Enemies[0].X=1;checkpoint.Enemies[0].Y=0;checkpoint.Enemies[1].X=-1;checkpoint.Enemies[1].Y=0;
            var restored=BattleWorld.Restore(world.Session,new Dictionary<string,double>{{"a1",.0001}},checkpoint);
            restored.Advance(BattleWorld.FixedStep,Vector2.Zero);
            Check.Near(90,restored.Session.Wave!.Active[1].Health);Check.Near(100,restored.Session.Wave.Active[2].Health);
        });
        test("Battle bullet expires and cannot hit targets beyond its remaining lifetime",()=>{
            var world=Controlled();var checkpoint=world.Capture();checkpoint.FacingX=-1;
            checkpoint.Enemies[0].X=2;checkpoint.Enemies[0].Y=0;checkpoint.NextProjectileId=2;
            checkpoint.Projectiles.Add(new BattleProjectileState{Id=1,VelocityX=240,Lifetime=.001,Damage=10,RemainingPierce=1,Radius=.1});
            var restored=BattleWorld.Restore(world.Session,new Dictionary<string,double>{{"a1",.0001}},checkpoint);
            restored.Advance(BattleWorld.FixedStep,Vector2.Zero);
            Check.Near(100,restored.Session.Wave!.Active[1].Health);Check.Equal(0,restored.Capture().Projectiles.Count);
        });
        test("Battle death and round completion discard later wall time and all combat activity",()=>{
            var world=Make();world.Session.Damage(1000);var time=world.SimulationTime;world.Advance(30,Vector2.One);
            Check.Near(time,world.SimulationTime);Check.Equal(Vector2.Zero,world.PlayerPosition);Check.Equal(0,world.ShotsFired);
            var complete=Controlled();complete.Session.Advance(179);var shots=complete.ShotsFired;
            complete.Advance(20,Vector2.One);Check.Equal(shots,complete.ShotsFired);Check.Equal(Vector2.Zero,complete.PlayerPosition);
        });
        test("Battle upgrading a weapon preserves its pending cycle deadline",()=>{
            var world=Controlled(1,1000,"sling");var snapshot=world.Capture();snapshot.Enemies[0].X=4;snapshot.Enemies[0].Y=0;
            var restored=BattleWorld.Restore(world.Session,new Dictionary<string,double>{{"a1",.0001}},snapshot);
            restored.Advance(BattleWorld.FixedStep,Vector2.Zero);Check.Equal(1,restored.ShotsFired);
            for(var i=0;i<7;i++)Check.True(restored.Session.Inventory.TryChoose("sling",false));
            for(var i=0;i<5;i++)Check.True(restored.Session.Inventory.TryChoose("thunder",true));
            restored.Advance(.5,Vector2.Zero);Check.Equal(1,restored.ShotsFired);
            restored.Advance(1,Vector2.Zero);Check.True(restored.ShotsFired>1);
        });
        test("Review: JSON null nested battle entries are rejected as corrupt snapshots",()=>{
            var mutations=new Action<BattleSnapshot>[] {
                s=>s.Enemies.Add(null!),s=>s.Drops.Add(null!),s=>s.Projectiles.Add(null!),
                s=>s.Weapons.Add(null!),s=>s.MonsterSpeeds.Add(null!),
                s=>s.Weapons.Add(new BattleWeaponState{WeaponId="bible",OrbitHits=new List<BattleHitState>{null!}})
            };
            foreach(var mutate in mutations)
            {
                var snapshot=Make().Capture();mutate(snapshot);
                using var bytes=new MemoryStream();var serializer=new DataContractJsonSerializer(typeof(BattleSnapshot));
                serializer.WriteObject(bytes,snapshot);bytes.Position=0;
                var decoded=(BattleSnapshot)serializer.ReadObject(bytes)!;
                Check.Throws<ArgumentException>(()=>decoded.Validate());
            }
            Check.Throws<ArgumentNullException>(()=>BattleWorld.Restore(Make().Session,Speeds,null!));
        });
    }
}
