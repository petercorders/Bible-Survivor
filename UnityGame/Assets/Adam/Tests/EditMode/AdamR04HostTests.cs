using System;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Json;
using BibleSurvivor.Core;
using BibleSurvivor.Unity;
using NUnit.Framework;
using UnityEngine;
public sealed class AdamR04HostTests
{
    string dir,path;AdamCatalog catalog;
    [SetUp]public void Setup(){dir=Path.Combine(Path.GetTempPath(),"adam-r04-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(dir);path=Path.Combine(dir,"save.json");catalog=AdamCatalog.Load();}
    [TearDown]public void Cleanup(){Directory.Delete(dir,true);}
    static byte[] Bytes(object value){using(var m=new MemoryStream()){new DataContractJsonSerializer(value.GetType()).WriteObject(m,value);return m.ToArray();}}
    public static AdamHost Start(string path)
    {
        var h=AdamR03HostTests.Intro(path);h.SetPaused(false);for(var i=0;i<20;i++)h.Tick(.1);h.Session.Hit(BossDefinition.ActorId,BossDefinition.Health);h.Checkpoint();
        var choice=h.Session.BossChoices.First(x=>x.Kind=="evolution");h.ChooseBossReward(h.Session.Boss.Reward.RewardId,choice.Id);h.CompleteBossSettlement();h.SetPaused(false);Assert.AreEqual(4,h.Session.RoundNumber);return h;
    }
    public static int Charger(AdamHost h,string path)
    {
        h.Session.Advance(63);var target=0;
        for(var i=0;i<5;i++)
        {
            foreach(var enemy in h.Session.Targets.Values.OrderBy(x=>x.Id).ToArray())
            {if(enemy.MonsterId=="d3"&&target==0){target=enemy.Id;continue;}if(enemy.Id!=target)h.Session.Kill(enemy.Id);}
            h.Session.Advance(BattleWorld.FixedStep);h.State.Run.Drops.Clear();
        }
        Assert.Greater(target,0);h.Session.Capture();var snapshot=new BattleWorld(h.Session,AdamCatalog.Load().Speeds,7).Capture();
        foreach(var actor in snapshot.Enemies){actor.X=actor.Id==target?8:100;actor.Y=0;}
        foreach(var weapon in h.Session.Inventory.Weapons.Keys)snapshot.Weapons.Add(new BattleWeaponState{WeaponId=weapon,WeaponForm=h.Session.Inventory.Forms.TryGetValue(weapon,out var form)?form:null,NextCycle=1000});
        h.State.Run.World=snapshot;new SaveStore(path).Commit(h.State,h.State.Revision,"live-D3-fixture");Assert.IsTrue(h.Reload(),h.Error);return target;
    }
    [Test]public void R03EntryPreservesReceiptEvolutionAndR04CompletionWaitsForGrowthThenPaysExactlyOnce()
    {
        var h=Start(path);Assert.IsTrue(h.State.Run.CompletedBoss.CompletionApplied);Assert.AreEqual(EvolutionRules.Sling,h.Session.WeaponForm("sling"));Assert.AreEqual(600,catalog.ForRound(4).Waves.Sum(x=>x.Groups.Sum(g=>g.Count)));Assert.AreEqual(1600,catalog.ForRound(4).RawExperience);
        h.Session.Advance(10);h.Session.Damage(20);foreach(var id in h.Session.Targets.Keys.Take(5).ToArray())h.Session.Kill(id);var gold=h.State.Profile.Gold;var hp=h.Session.CurrentHealth;h.Session.Advance(180);Assert.IsTrue(h.Checkpoint(),h.Error);Assert.AreEqual("round_settlement",h.Session.Stage);Assert.AreEqual(gold,h.State.Profile.Gold);Assert.AreEqual(hp,h.Session.CurrentHealth);Assert.IsTrue(h.Reload());
        while(h.Session.PendingChoices.Count>0)Assert.IsTrue(h.Choose(h.Session.PendingChoices[0]));Assert.IsTrue(h.AwaitingR05);Assert.AreEqual(gold+65,h.State.Profile.Gold);Assert.AreEqual(h.State.Run.MaxHealth,h.Session.CurrentHealth);Assert.IsTrue(h.State.Profile.UnlockedWeapons.Contains("fire_pillar"));Assert.IsFalse(h.Session.Inventory.TryChoose("fire_pillar",false));Assert.IsFalse(h.CompleteNormalSettlement());Assert.IsTrue(h.Reload());Assert.IsTrue(h.AwaitingR05);Assert.AreEqual(5,h.State.Run.RoundNumber);Assert.Throws<InvalidDataException>(()=>catalog.ForRound(15));
    }
    [Test]public void HostTelegraphAndMidDashPauseAndDiskReloadKeepExactWorldAndGeometry()
    {
        var h=Start(path);var id=Charger(h,path);h.SetPaused(false);while(h.World.ChargeStates[id].Phase=="cooldown")h.Tick(.1);
        foreach(var phase in new[]{"telegraph","charge"})
        {
            Assert.AreEqual(phase,h.World.ChargeStates[id].Phase);h.SetPaused(true);var snapshot=h.World.Capture();Assert.IsTrue(AdamChargeLane.TryGeometry(snapshot,snapshot.Charges.Single(x=>x.Id==id),out var before));h.Tick(20);CollectionAssert.AreEqual(Bytes(snapshot),Bytes(h.World.Capture()));Assert.IsTrue(h.Reload(),h.Error);CollectionAssert.AreEqual(Bytes(snapshot),Bytes(h.World.Capture()));Assert.IsTrue(AdamChargeLane.TryGeometry(h.World.Capture(),h.World.ChargeStates[id],out var after));Assert.AreEqual(before.Start,after.Start);Assert.AreEqual(before.End,after.End);Assert.AreEqual(before.HalfWidth,after.HalfWidth);
            h.SetPaused(false);if(phase=="telegraph")while(h.World.ChargeStates[id].Phase=="telegraph")h.Tick(.1);
        }
    }
    [Test]public void GeometryFollowsLiveKnockbackAndRemainingCoreTimeWithoutBoardConstants()
    {
        var snapshot=new BattleSnapshot{Time=10,Tuning=new BattleTuning{PlayerSpeed=4,PlayerRadius=.4,EnemyRadius=.3}};snapshot.Enemies.Add(new BattleEntityState{Id=9,X=3,Y=5});var state=new EnemyChargeState{Id=9,Phase="telegraph",DirectionX=0,DirectionY=-1,Until=11};
        Assert.IsTrue(AdamChargeLane.TryGeometry(snapshot,state,out var a));Assert.AreEqual(new Vector2(3,5),a.Start);Assert.AreEqual(4*BattleWorld.ChargeSpeed*BattleWorld.ChargeDuration,(a.End-a.Start).magnitude,.00001);Assert.AreEqual(.7,a.HalfWidth,.00001);
        snapshot.Enemies[0].X+=1;Assert.IsTrue(AdamChargeLane.TryGeometry(snapshot,state,out var pushed));Assert.AreEqual(a.Start+Vector2.right,pushed.Start);Assert.AreEqual(a.End+Vector2.right,pushed.End);Assert.AreEqual(0,state.DirectionX);Assert.AreEqual(-1,state.DirectionY);
        state.Phase="charge";state.Until=10.25;Assert.IsTrue(AdamChargeLane.TryGeometry(snapshot,state,out var dash));Assert.AreEqual(4*BattleWorld.ChargeSpeed*.25,(dash.End-dash.Start).magnitude,.00001);int frame;double seconds;Assert.AreEqual(AdamR04Art.D3Dash,AdamChargeLane.Clip(state,snapshot.Time,out frame,out seconds));state.Phase="telegraph";state.Until=10.5;Assert.AreEqual(AdamR04Art.D3Windup,AdamChargeLane.Clip(state,snapshot.Time,out frame,out seconds));Assert.AreEqual(1,frame);
        var projection=new AdamWorldProjection(new Rect(20,116,620,246),Vector2.zero);Assert.AreEqual(projection.Point(pushed.Start.x,pushed.Start.y),projection.Point(snapshot.Enemies[0].X,snapshot.Enemies[0].Y));Assert.AreEqual(5,new AdamR04Art().Index.assets.Length);
    }
    [Test]public void ActualCoreJawboneKnockbackMovesLiveLaneWithoutRetargeting()
    {
        var h=Start(path);var id=Charger(h,path);for(var i=0;i<8;i++)Assert.IsTrue(h.Session.Inventory.TryChoose("jawbone",false));Assert.IsTrue(h.Session.Inventory.TryChoose("hair",true));Assert.IsTrue(h.Session.Inventory.TryEvolve(EvolutionRules.Jawbone));h.Session.Capture();
        var setup=h.World.Capture();setup.Enemies.Single(x=>x.Id==id).X=1.2f;setup.Charges.Single(x=>x.Id==id).NextAt=setup.Time;setup.FacingX=1;setup.FacingY=0;setup.Weapons.Add(new BattleWeaponState{WeaponId="jawbone",WeaponForm=EvolutionRules.Jawbone,NextCycle=setup.Time+1});
        var world=BattleWorld.Restore(h.Session,catalog.Speeds,setup);world.Advance(BattleWorld.FixedStep,System.Numerics.Vector2.Zero);var before=world.Capture();var locked=before.Charges.Single(x=>x.Id==id);Assert.AreEqual("telegraph",locked.Phase);Assert.IsTrue(AdamChargeLane.TryGeometry(before,locked,out var original));before.Weapons.Single(x=>x.WeaponId=="jawbone").NextCycle=before.Time;
        world=BattleWorld.Restore(h.Session,catalog.Speeds,before);world.Advance(BattleWorld.FixedStep,System.Numerics.Vector2.Zero);var after=world.Capture();var charge=after.Charges.Single(x=>x.Id==id);Assert.AreEqual("telegraph",charge.Phase);Assert.AreEqual(locked.DirectionX,charge.DirectionX);Assert.AreEqual(locked.DirectionY,charge.DirectionY);Assert.AreEqual(locked.TargetX,charge.TargetX);Assert.AreEqual(locked.TargetY,charge.TargetY);Assert.IsTrue(AdamChargeLane.TryGeometry(after,charge,out var moved));var shift=moved.Start-original.Start;Assert.AreEqual(EvolutionRules.Knockback,shift.magnitude,.0001);Assert.AreEqual(original.End+shift,moved.End);Assert.Greater(world.Hits,0);
    }
    [Test]public void CompletionWriteFailureFreezesReceiptAndRetryDoesNotPayTwice()
    {
        var h=Start(path);Assert.IsTrue(h.Checkpoint());var fail=true;var store=new SaveStore(path,()=>{if(fail)throw new IOException("completion gate");});h=new AdamHost(catalog,store);h.SetPaused(false);var gold=h.State.Profile.Gold;h.Session.Advance(180);Assert.IsFalse(h.Checkpoint());var frozen=Bytes(h.State);h.Tick(40);Assert.IsFalse(h.CompleteNormalSettlement());CollectionAssert.AreEqual(frozen,Bytes(h.State));Assert.AreEqual(gold+65,h.State.Profile.Gold);fail=false;Assert.IsTrue(h.RetrySave(),h.Error);Assert.AreEqual(gold+65,h.State.Profile.Gold);Assert.IsTrue(h.AwaitingR05);Assert.IsTrue(h.Reload());Assert.AreEqual(gold+65,h.State.Profile.Gold);
    }
}
