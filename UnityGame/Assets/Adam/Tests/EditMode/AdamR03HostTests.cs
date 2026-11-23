using System;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Runtime.Serialization.Json;
using BibleSurvivor.Core;
using BibleSurvivor.Unity;
using NUnit.Framework;
using UnityEngine;
public sealed class AdamR03HostTests
{
    string dir,path;AdamCatalog catalog;
    [SetUp]public void Setup(){dir=Path.Combine(Path.GetTempPath(),"adam-r03-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(dir);path=Path.Combine(dir,"save.json");catalog=AdamCatalog.Load();}
    [TearDown]public void Cleanup(){Directory.Delete(dir,true);}
    static byte[] Bytes(object value){using(var m=new MemoryStream()){new DataContractJsonSerializer(value.GetType()).WriteObject(m,value);return m.ToArray();}}
    public static AdamHost Intro(string path,SaveStore store=null)
    {
        var h=AdamRoundHostTests.OpenLegacyChallenge(path,store);
        h.Session.Advance(180);h.Checkpoint();h.SetPaused(false);h.Session.Advance(180);h.Checkpoint();h.SetPaused(false);
        Assert.AreEqual(3,h.Session.RoundNumber);Assert.AreEqual(SaveFormat.CurrentVersion,h.State.SchemaVersion);Assert.AreEqual(1,h.Session.CombatRulesVersion);for(var i=1;i<8;i++)h.Session.Inventory.TryChoose("sling",false);h.Session.Inventory.TryChoose("stones",true);
        h.Session.Advance(180);h.Session.Capture();h.State.Run.World=null;
        new SaveStore(path).Commit(h.State,h.State.Revision,"r03-intro-fixture");Assert.IsTrue(h.Reload(),h.Error);return h;
    }
    [Test]public void ProtectedIntroAndBossTelegraphRestoreExactWorldAndFrozenPause()
    {
        var h=Intro(path);Assert.AreEqual("boss_intro",h.Session.Stage);Assert.IsTrue(h.Paused);h.SetPaused(false);
        for(var i=0;i<20;i++)h.Tick(.1);Assert.AreEqual("boss",h.Session.Stage);
        for(var i=0;i<55&&!h.Session.Boss.Telegraphing;i++){h.SetMovement(Vector2.right);h.Tick(.1);}
        Assert.IsTrue(h.Session.Boss.Telegraphing);h.SetPaused(true);var before=Bytes(h.World.Capture());var boss=Bytes(h.Session.Boss);h.Tick(40);
        CollectionAssert.AreEqual(before,Bytes(h.World.Capture()));var loaded=new AdamHost(catalog,new SaveStore(path));Assert.IsNull(loaded.Error);CollectionAssert.AreEqual(before,Bytes(loaded.World.Capture()));CollectionAssert.AreEqual(boss,Bytes(loaded.Session.Boss));
    }
    [TestCase(false)][TestCase(true)]public void RewardThenSettlementWriteFailuresFreezeAndRetryWithoutDoubleGrant(bool settlementFailure)
    {
        var fail=false;var store=new SaveStore(path,()=>{if(fail)throw new IOException("R03 exact boundary failure");});var h=Intro(path,store);h.SetPaused(false);for(var i=0;i<20;i++)h.Tick(.1);
        h.Session.Hit(BossDefinition.ActorId,BossDefinition.Health);Assert.IsTrue(h.Checkpoint(),h.Error);Assert.AreEqual("boss_reward",h.Session.Stage);var choices=Bytes(h.Session.BossChoices.ToArray());Assert.IsTrue(h.Reload());CollectionAssert.AreEqual(choices,Bytes(h.Session.BossChoices.ToArray()));
        var reward=h.Session.Boss.Reward.RewardId;var choice=h.Session.BossChoices.Single(x=>x.Kind=="evolution");Assert.IsTrue(AdamBossPresentation.Card(choice,0,h.Session).Contains("진화"));
        fail=!settlementFailure;Assert.IsTrue(h.ChooseBossReward(reward,choice.Id));Assert.AreEqual(EvolutionRules.Sling,h.Session.WeaponForm("sling"));
        if(settlementFailure){fail=true;Assert.IsFalse(h.CompleteBossSettlement());}else Assert.NotNull(h.Error);
        var frozen=Bytes(h.State);var gold=h.State.Profile.Gold;h.Tick(20);Assert.IsFalse(h.ChooseBossReward(reward,choice.Id));Assert.IsFalse(h.CompleteBossSettlement());CollectionAssert.AreEqual(frozen,Bytes(h.State));
        fail=false;Assert.IsTrue(h.RetrySave(),h.Error);Assert.AreEqual(gold,h.State.Profile.Gold);
        if(!settlementFailure)Assert.IsTrue(h.CompleteBossSettlement());
        Assert.IsTrue(h.AwaitingR04);Assert.AreEqual(4,h.State.Run.RoundNumber);Assert.AreEqual(h.State.Run.MaxHealth,h.Session.CurrentHealth);Assert.IsFalse(h.CompleteBossSettlement());var finalGold=h.State.Profile.Gold;Assert.IsTrue(h.Reload());Assert.IsTrue(h.AwaitingR04);Assert.AreEqual(finalGold,h.State.Profile.Gold);Assert.AreEqual(EvolutionRules.Sling,h.Session.WeaponForm("sling"));
    }
    [Test]public void RemainingGrowthBlocksCompletionAfterRewardAndLastChoiceCanSettle()
    {
        var h=Intro(path);h.SetPaused(false);for(var i=0;i<20;i++)h.Tick(.1);h.Session.Hit(BossDefinition.ActorId,BossDefinition.Health);h.Checkpoint();
        var state=new SaveStore(path).Load();state.Run.PendingLevels=1;state.Run.Choices=h.Session.Inventory.Choices(state.Profile.UnlockedWeapons,new[]{"scroll","hair","stones","east_wind","seal","cloud","thunder","crown","underwear","whirlwind"},3,state.Run.ChoiceSerial).ToList();new SaveStore(path).Commit(state,state.Revision,"pending-growth-fixture");Assert.IsTrue(h.Reload(),h.Error);
        var reward=h.Session.Boss.Reward.RewardId;Assert.IsTrue(h.ChooseBossReward(reward,h.Session.BossChoices[0].Id));Assert.Greater(h.Session.PendingChoices.Count,0);Assert.IsFalse(h.CompleteBossSettlement());Assert.IsTrue(h.Choose(h.Session.PendingChoices[0]));Assert.IsTrue(h.CompleteBossSettlement());Assert.IsTrue(h.AwaitingR04);
    }
    [Test]public void ProjectionUsesLockedCoreRadiusAndClockFramesWithoutOptionalFlag()
    {
        foreach(var arena in new[]{new Rect(20,116,620,246),new Rect(20,116,1060,526)})
        {var p=new AdamWorldProjection(arena,new Vector2(4,7));var target=new Vector2(5,9);var bounds=AdamDangerRing.Bounds(p,target,(float)BossDefinition.SlamRadius);Assert.AreEqual(p.Point(target.x,target.y),bounds.center);Assert.AreEqual((float)BossDefinition.SlamRadius*32*p.Scale*2,bounds.width,.0001);}
        var boss=new BossEncounterState{Telegraphing=true,BossElapsed=5.2,NextAttackAt=6};int frame;Assert.AreEqual(AdamR03Art.BossWindup,AdamArtWorld.BossClip("boss",boss,-1,20,out frame));Assert.AreEqual(1,frame);Assert.AreEqual(AdamR03Art.BossMove,AdamArtWorld.BossClip("boss_intro",boss,-1,20,out frame));Assert.Zero(frame);
        var art=new AdamR03Art();Assert.AreEqual(8,art.Index.assets.Length);Assert.AreEqual(3,art.Clip(AdamR03Art.BossWindup).frames);
    }
}
