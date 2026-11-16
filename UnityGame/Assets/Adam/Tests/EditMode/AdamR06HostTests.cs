using System;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Json;
using BibleSurvivor.Core;
using BibleSurvivor.Unity;
using NUnit.Framework;
using UnityEngine;
public sealed class AdamR06HostTests
{
    string dir,path;AdamCatalog catalog;
    [SetUp]public void Setup(){dir=Path.Combine(Path.GetTempPath(),"adam-r06-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(dir);path=Path.Combine(dir,"save.json");catalog=AdamCatalog.Load();}
    [TearDown]public void Cleanup(){Directory.Delete(dir,true);}
    static byte[] Bytes(object value){using(var m=new MemoryStream()){new DataContractJsonSerializer(value.GetType()).WriteObject(m,value);return m.ToArray();}}
    public static AdamHost Intro(string path)
    {
        var h=AdamR05HostTests.Start(path);h.Session.Advance(180);Assert.IsTrue(h.Checkpoint(),h.Error);Assert.IsTrue(h.AwaitingR06);h.SetPaused(false);Assert.AreEqual(6,h.Session.RoundNumber);while(h.Session.Inventory.TryChoose("bible",false)){}h.Session.Inventory.TryChoose("scroll",true);h.Session.Advance(180);h.Session.Capture();h.State.Run.World=null;new SaveStore(path).Commit(h.State,h.State.Revision,"R06-intro-fixture");Assert.IsTrue(h.Reload(),h.Error);return h;
    }
    static void Active(AdamHost h){h.SetPaused(false);for(var i=0;i<20;i++)h.Tick(.1);Assert.AreEqual("boss",h.Session.Stage);}
    void SuppressWeapons(AdamHost h)
    {
        var s=h.World.Capture();s.Enemies.Single(x=>x.Id==h.Session.BossRules.BossActorId).X=30;foreach(var id in h.Session.Inventory.Weapons.Keys){var w=s.Weapons.SingleOrDefault(x=>x.WeaponId==id);if(w==null){w=new BattleWeaponState{WeaponId=id,WeaponForm=h.Session.Inventory.Forms.TryGetValue(id,out var form)?form:null};s.Weapons.Add(w);}w.NextCycle=1000;w.BurstsRemaining=0;w.PendingBursts.Clear();w.NextBurst=0;}
        h.Session.Capture();h.State.Run.World=s;new SaveStore(path).Commit(h.State,h.State.Revision,"distant-boss-clock-fixture");Assert.IsTrue(h.Reload(),h.Error);h.SetPaused(false);
    }
    void ExactReload(AdamHost h){h.SetPaused(true);var world=Bytes(h.World.Capture());var boss=Bytes(h.Session.Boss);h.Tick(30);CollectionAssert.AreEqual(world,Bytes(h.World.Capture()));Assert.IsTrue(h.Reload(),h.Error);CollectionAssert.AreEqual(world,Bytes(h.World.Capture()));CollectionAssert.AreEqual(boss,Bytes(h.Session.Boss));h.SetPaused(false);}
    [Test]public void R06DynamicIntroTwoThresholdsAndTelegraphDiskReplayPreserveR03History()
    {
        var h=Intro(path);var old=Bytes(h.State.Run.CompletedBoss);Assert.AreEqual(5,h.State.Run.PreviousNormalRound);Assert.AreEqual(6500,h.Session.Boss.Health);Assert.AreEqual(781,h.Session.BossRules.BossActorId);Assert.AreEqual(7,h.Session.Boss.NextAttackAt);Assert.AreEqual(16,h.Session.BossRules.Contact);Assert.AreEqual(18,h.Session.BossRules.AttackDamage);Assert.AreEqual(200,h.Session.BossRules.KillGold);Assert.AreEqual("싯나의 우물지기",AdamBossPresentation.Name(h.Session));Assert.AreEqual(1,AdamBossPresentation.HealthFraction(h.Session));Active(h);SuppressWeapons(h);
        h.Session.Hit(781,2000);h.Tick(.1);Assert.AreEqual(1,h.Session.Boss.SummonMask);Assert.AreEqual(7,h.World.EnemyPositions.Count);ExactReload(h);h.Session.Hit(781,2300);h.Tick(.1);Assert.AreEqual(3,h.Session.Boss.SummonMask);Assert.AreEqual(12,h.Session.Boss.Summoned);Assert.AreEqual(13,h.World.EnemyPositions.Count);ExactReload(h);while(!h.Session.Boss.Telegraphing)h.Tick(.1);Assert.Less(h.Session.Boss.BossElapsed,7);Assert.GreaterOrEqual(h.Session.Boss.BossElapsed,5.8-1e-9);ExactReload(h);Assert.AreEqual("흙더미 낙하 예고",AdamBossPresentation.Phase(h.Session));CollectionAssert.AreEqual(old,Bytes(h.State.Run.CompletedBoss));Assert.Zero(h.World.ChargeStates.Count);
    }
    [TestCase(false)][TestCase(true)]public void RewardAndSettlementFailuresFreezeSamePayloadAndRetryWithoutDoubleGrants(bool settlementFailure)
    {
        var h=Intro(path);Active(h);h.Session.Hit(h.Session.BossRules.BossActorId,h.Session.BossRules.MaxHealth);Assert.IsTrue(h.Checkpoint(),h.Error);var old=Bytes(h.State.Run.CompletedBoss);var choices=Bytes(h.Session.BossChoices.ToArray());Assert.IsTrue(h.Reload());CollectionAssert.AreEqual(choices,Bytes(h.Session.BossChoices.ToArray()));Assert.IsFalse(h.Session.BossChoices.Any(x=>x.Kind=="evolution"&&x.ItemId==EvolutionRules.Sling));Assert.AreEqual(BossDefinition.SitnahPolicy,h.Session.Boss.Reward.PolicyId);var choice=h.Session.BossChoices.Single(x=>x.Kind=="evolution");Assert.AreEqual(EvolutionRules.Bible,choice.ItemId);
        var fail=false;h=new AdamHost(catalog,new SaveStore(path,()=>{if(fail)throw new IOException("R06 exact boundary");}));var reward=h.Session.Boss.Reward.RewardId;fail=!settlementFailure;Assert.IsTrue(h.ChooseBossReward(reward,choice.Id));if(settlementFailure){fail=true;Assert.IsFalse(h.CompleteBossSettlement());}else Assert.NotNull(h.Error);var frozen=Bytes(h.State);var gold=h.State.Profile.Gold;h.Tick(20);Assert.IsFalse(h.ChooseBossReward(reward,choice.Id));Assert.IsFalse(h.CompleteBossSettlement());CollectionAssert.AreEqual(frozen,Bytes(h.State));fail=false;Assert.IsTrue(h.RetrySave(),h.Error);Assert.AreEqual(gold,h.State.Profile.Gold);if(!settlementFailure)Assert.IsTrue(h.CompleteBossSettlement());Assert.IsTrue(h.ContentComplete);Assert.AreEqual(7,h.State.Run.RoundNumber);Assert.IsTrue(h.State.Profile.UnlockedWeapons.Contains("cross"));Assert.IsFalse(h.Session.Inventory.TryChoose("cross",false));Assert.AreEqual(h.State.Run.MaxHealth,h.Session.CurrentHealth);Assert.IsFalse(h.CompleteBossSettlement());Assert.IsTrue(h.Reload());CollectionAssert.AreEqual(old,Bytes(h.State.Run.CompletedBoss));Assert.AreEqual(EvolutionRules.Sling,h.Session.WeaponForm("sling"));Assert.AreEqual(EvolutionRules.Bible,h.Session.WeaponForm("bible"));Assert.Throws<InvalidDataException>(()=>catalog.ForRound(7));
    }
    [Test]public void AppliedRewardWaitsForRemainingGrowthBefore100GoldCompletion()
    {
        var h=Intro(path);Active(h);h.Session.Hit(781,6500);Assert.IsTrue(h.Checkpoint());var state=new SaveStore(path).Load();state.Run.PendingLevels=1;state.Run.Choices=h.Session.Inventory.Choices(state.Profile.UnlockedWeapons,new[]{"scroll","hair","stones","east_wind","seal","cloud","thunder","crown","underwear","whirlwind"},3,state.Run.ChoiceSerial).ToList();new SaveStore(path).Commit(state,state.Revision,"pending-growth-fixture");Assert.IsTrue(h.Reload(),h.Error);var gold=h.State.Profile.Gold;Assert.IsTrue(h.ChooseBossReward(h.Session.Boss.Reward.RewardId,h.Session.BossChoices.Single(x=>x.Kind=="evolution").Id));Assert.Greater(h.Session.PendingChoices.Count,0);Assert.IsFalse(h.CompleteBossSettlement());Assert.AreEqual(gold,h.State.Profile.Gold);Assert.IsTrue(h.Choose(h.Session.PendingChoices[0]));Assert.IsTrue(h.CompleteBossSettlement());Assert.AreEqual(gold+100,h.State.Profile.Gold);Assert.IsTrue(h.ContentComplete);
    }
    [Test]public void SevenReviewedClipsDynamicClockRadiusAndShortDeathKeepR03Mapping()
    {
        var art=new AdamR06Art();Assert.AreEqual(7,art.Index.assets.Length);Assert.AreEqual(25,art.Index.assets.Sum(x=>x.frames));foreach(var id in new[]{"f1","f2","f3","sitnah_keeper"})Assert.AreEqual(AdamR06Art.EnemyClip(id),AdamArtWorld.EnemyClip(id));Assert.AreEqual("fx_shadow-boss_static_40x8",AdamRoundPresentation.Shadow("sitnah_keeper"));var boss=new BossEncounterState{RoundNumber=6,Telegraphing=true,BossElapsed=6.4,NextAttackAt=7};int frame;Assert.AreEqual(AdamR06Art.BossWindup,AdamArtWorld.BossClip("boss",boss,-1,190,out frame));Assert.AreEqual(1,frame);Assert.AreEqual(AdamR06Art.BossImpact,AdamArtWorld.BossClip("boss",new BossEncounterState{RoundNumber=6},190,190.1,out frame));Assert.IsTrue(AdamArtWorld.BossDeathVisible(190,190.6));Assert.IsFalse(AdamArtWorld.BossDeathVisible(190,190+4d/6));Assert.IsFalse(AdamArtWorld.BossDeathVisible(190,200));var p=new AdamWorldProjection(new Rect(20,116,620,246),new Vector2(2,3));var target=new Vector2(5,7);var bounds=AdamDangerRing.Bounds(p,target,(float)BossDefinition.SlamRadius);Assert.AreEqual(p.Point(5,7),bounds.center);Assert.AreEqual(2*BossDefinition.SlamRadius*32*p.Scale,bounds.width,.0001);Assert.AreEqual(780,catalog.ForRound(6).Waves.Sum(w=>w.Groups.Sum(g=>g.Count)));Assert.AreEqual(2400,catalog.ForRound(6).RawExperience);
    }
}
