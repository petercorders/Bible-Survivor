using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using BibleSurvivor.Core;

namespace BibleSurvivor.Core.Tests;

public static class ImpactTests
{
    public static readonly IReadOnlyDictionary<string,double> Speeds=new Dictionary<string,double>{{"a1",.0001}};
    public static BattleWorld Fixture(int count=2,int health=100,string weapon="jawbone")
    {
        var definition=new RoundDefinition(1,40,count,25,new[]{new WaveDefinition(0,1,new[]{new SpawnGroup("a1",count,health,4)})});
        var session=new AdamSession(GameSnapshot.New(),definition,weapon);session.Advance(1);
        var world=new BattleWorld(session,Speeds,7);var snapshot=world.Capture();
        foreach(var enemy in snapshot.Enemies){enemy.X=1;enemy.Y=0;}
        return BattleWorld.Restore(session,Speeds,snapshot);
    }
    public static void Register(Action<string,Action> test)
    {
        test("Impact lethal hit retains actual damage, cause, position and one fact per target",()=>{
            var world=Fixture(2,3);world.Advance(BattleWorld.FixedStep,Vector2.Zero);
            var events=new List<BattleImpact>();Check.Equal(0L,world.ReadImpacts(0,events));
            Check.Equal(2,events.Count);Check.Equal(2,world.Kills);
            foreach(var e in events){Check.True(e.Killed);Check.Equal("jawbone",e.Cause);Check.Near(3,e.Damage);Check.Near(1,e.Position.X,.001);Check.Equal(1L,e.Step);}
            Check.Equal(1L,events[0].Sequence);Check.Equal(2L,events[1].Sequence);
            world.ReadImpacts(world.ImpactSequence,events);Check.Equal(0,events.Count);
        });
        test("Impact multiple same-target hits between projections do not collapse",()=>{
            var world=Fixture(1,100);var snapshot=world.Capture();snapshot.NextProjectileId=3;
            for(var i=1;i<=2;i++)snapshot.Projectiles.Add(new BattleProjectileState{Id=i,X=.8f,Y=0,VelocityX=100,Lifetime=1,Damage=4,RemainingPierce=1,Radius=.1});
            world=BattleWorld.Restore(world.Session,Speeds,snapshot);world.Advance(BattleWorld.FixedStep,Vector2.Zero);
            var events=new List<BattleImpact>();world.ReadImpacts(0,events);Check.Equal(3,events.Count);
            Check.Equal(1,events.Select(e=>e.TargetId).Distinct().Count());Check.Near(18,events.Sum(e=>e.Damage));
            Check.Equal(2,events.Count(e=>e.Cause=="sling"));Check.Equal(1,events.Count(e=>e.Cause=="jawbone"));
            Check.Near(82,world.Session.Wave!.Active[1].Health);
        });
        test("Impact swept pierce reports each contact in collision order including lethal",()=>{
            var world=Fixture(2,3);var snapshot=world.Capture();snapshot.FacingX=-1;
            snapshot.Enemies[1].X=2;snapshot.NextProjectileId=2;
            snapshot.Projectiles.Add(new BattleProjectileState{Id=1,VelocityX=240,Lifetime=1,Damage=10,RemainingPierce=2,Radius=.1});
            world=BattleWorld.Restore(world.Session,Speeds,snapshot);world.Advance(BattleWorld.FixedStep,Vector2.Zero);
            var events=new List<BattleImpact>();world.ReadImpacts(0,events);Check.Equal(2,events.Count);
            Check.Equal(1,events[0].TargetId);Check.Equal(2,events[1].TargetId);Check.True(events.All(e=>e.Cause=="sling"&&e.Killed));
            Check.Near(.7,events[0].Position.X,.001);Check.Near(1.7,events[1].Position.X,.001);
        });
        test("Impact contact and retaliation distinguish source without invented weapon",()=>{
            var world=Fixture();world.Session.Inventory.TryChoose("crown",true);var snapshot=world.Capture();snapshot.Enemies[0].X=.1f;
            world=BattleWorld.Restore(world.Session,Speeds,snapshot);world.Advance(BattleWorld.FixedStep,Vector2.Zero);
            var events=new List<BattleImpact>();world.ReadImpacts(0,events);
            Check.Equal("contact",events[0].Cause);Check.Equal(0,events[0].TargetId);Check.Near(4,events[0].Damage);
            Check.Equal(2,events.Count(e=>e.Cause=="retaliation"));Check.True(events.Where(e=>e.Cause=="retaliation").All(e=>e.Damage==5));
        });
        test("Impact bounded overflow reports losses and restores without historical events",()=>{
            var world=Fixture(120,100000);world.Advance(25,Vector2.Zero);
            var events=new List<BattleImpact>();var lost=world.ReadImpacts(0,events);
            Check.True(lost>0);Check.Equal(BattleWorld.ImpactCapacity,events.Count);Check.Equal(world.ImpactSequence-BattleWorld.ImpactCapacity,lost);
            Check.Equal(lost+1,events[0].Sequence);world.Session.Capture();
            var restored=BattleWorld.Restore(world.Session,Speeds,world.Capture());Check.Equal(0L,restored.ImpactSequence);
            restored.ReadImpacts(0,events);Check.Equal(0,events.Count);
            Check.Throws<ArgumentOutOfRangeException>(()=>restored.ReadImpacts(1,events));
        });
    }
}
