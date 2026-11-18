using System;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Json;
using BibleSurvivor.Core;
using BibleSurvivor.Unity;
using NUnit.Framework;
using UnityEngine;

public sealed class AdamR09HostTests
{
    string dir,path;
    [SetUp]public void Setup(){dir=Path.Combine(Path.GetTempPath(),"adam-r09-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(dir);path=Path.Combine(dir,"save.json");}
    [TearDown]public void Cleanup(){Directory.Delete(dir,true);}
    static byte[] Bytes(object value){using(var m=new MemoryStream()){new DataContractJsonSerializer(value.GetType()).WriteObject(m,value);return m.ToArray();}}
    public static AdamHost Start(string path)
    {
        // Technical progression fixture: earlier-round helpers inject boss damage; not a natural nine-round playthrough.
        var h=AdamR08HostTests.Start(path);h.Session.Advance(180);Assert.IsTrue(h.Checkpoint(),h.Error);
        Assert.IsTrue(h.AwaitingR09);Assert.IsFalse(h.ContentComplete);Assert.IsFalse(h.CanStartNew);
        var run=h.State.Run.RunId;h.Start("bible");Assert.AreEqual(run,h.State.Run.RunId);
        h.SetPaused(false);Assert.AreEqual(9,h.Session.RoundNumber);Assert.IsFalse(h.AwaitingNextRound);return h;
    }
    public static AdamHost Intro(string path)
    {
        var h=Start(path);h.Session.Advance(180);h.Session.Capture();
        // Direct Session timeout left the old normal World behind; rebuild from the valid intro state on Reload.
        h.State.Run.World=null;new SaveStore(path).Commit(h.State,h.State.Revision,"R09-intro-technical-fixture");
        Assert.IsTrue(h.Reload(),h.Error);Assert.AreEqual("boss_intro",h.Session.Stage);return h;
    }
    static void Active(AdamHost h){h.SetPaused(false);for(var i=0;i<20;i++)h.Tick(.1);Assert.AreEqual("boss",h.Session.Stage);}
    void SuppressWeapons(AdamHost h)
    {
        // Preparation only after intro ends: retain valid Core actor/clock/slots; no weakened validation.
        var w=h.World.Capture();var actor=w.Enemies.Single(x=>x.Id==h.Session.BossRules.BossActorId);actor.X=30;actor.Y=0;
        foreach(var id in h.Session.Inventory.Weapons.Keys)
        {
            var weapon=w.Weapons.SingleOrDefault(x=>x.WeaponId==id);
            if(weapon==null){weapon=new BattleWeaponState{WeaponId=id,WeaponForm=h.Session.Inventory.Forms.TryGetValue(id,out var form)?form:null};w.Weapons.Add(weapon);}
            weapon.NextCycle=1000;weapon.NextBurst=0;weapon.BurstsRemaining=0;weapon.PendingBursts.Clear();
        }
        h.Session.Capture();h.State.Run.World=w;new SaveStore(path).Commit(h.State,h.State.Revision,"R09-distant-boss-weapon-clock-fixture");Assert.IsTrue(h.Reload(),h.Error);h.SetPaused(false);
    }
    static readonly string[] Passives={"scroll","hair","stones","east_wind","seal","cloud","thunder","crown","underwear","whirlwind"};
    [Test]public void CatalogLoadsNineRoundsAndAllThreeDynamicBossContracts()
    {
        var c=AdamCatalog.Load();var d=c.ForRound(9);Assert.AreEqual(1140,d.Waves.Sum(w=>w.Groups.Sum(g=>g.Count)));Assert.AreEqual(4200,d.RawExperience);Assert.AreEqual(120,d.ConcurrencyCap);Assert.AreEqual(160,d.CompletionGold);Assert.IsNull(d.UnlockWeaponId);
        foreach(var round in new[]{3,6,9}){var rule=BossDefinition.ForRound(round);Assert.AreEqual(round,c.ForRound(round).Number);Assert.Greater(rule.MaxHealth,0);}
        Assert.AreEqual(1,BossDefinition.ForRound(9).TelegraphDuration);Assert.Throws<InvalidDataException>(()=>c.ForRound(16));
        Assert.Throws<UnsupportedSaveVersionException>(()=>c.ForCheckpoint(new RunState{RoundNumber=16,Stage="normal"}));
    }
    [TestCase("clock")][TestCase("count")][TestCase("pattern")]
    public void CatalogRejectsR09SlamContractInsteadOfAcceptingOldBossDefaults(string field)
    {
        var data=JsonUtility.FromJson<AdamCatalog.Root>(Resources.Load<TextAsset>("genesis-catalog").text);var attack=data.bosses.Single(x=>x.id=="silver_cup_accuser").attack;
        if(field=="clock")attack.telegraph_seconds=BossDefinition.TelegraphSeconds;
        if(field=="count")attack.projectile_count=0;
        if(field=="pattern")attack.pattern_id="telegraphed_club_slam";
        Assert.Throws<InvalidDataException>(()=>new AdamCatalog(JsonUtility.ToJson(data)));
    }
    [Test]public void R09IntroNormalAndLiveHostileDiskReplayKeepTwoReceiptsAndPause()
    {
        var h=Start(path);var first=Bytes(h.State.Run.CompletedBoss);var second=Bytes(h.State.Run.CompletedSecondBoss);
        h.Tick(.1);h.SetPaused(true);var normal=Bytes(h.World.Capture());Assert.IsTrue(h.Reload(),h.Error);CollectionAssert.AreEqual(normal,Bytes(h.World.Capture()));Assert.AreEqual(9,h.Session.RoundNumber);
        h.Session.Advance(180);h.Session.Capture();h.State.Run.World=null;new SaveStore(path).Commit(h.State,h.State.Revision,"R09-normal-timeout-fixture");Assert.IsTrue(h.Reload(),h.Error);
        Assert.AreEqual(16000,h.Session.Boss.Health);Assert.AreEqual(1141,h.Session.BossRules.BossActorId);Assert.AreEqual(5,h.Session.Boss.NextAttackAt);Assert.IsFalse(h.ContentComplete);
        Active(h);SuppressWeapons(h);for(var i=0;i<40;i++)h.Tick(.1);
        Assert.IsTrue(h.Session.Boss.Telegraphing);Assert.AreEqual(3,h.Session.Boss.ChainDirections.Count);Assert.Zero(h.World.HostileProjectiles.Count);
        h.SetPaused(true);var boss=Bytes(h.Session.Boss);var warning=Bytes(h.World.Capture());Assert.IsTrue(h.Reload(),h.Error);Assert.IsTrue(h.Paused);h.Tick(60);CollectionAssert.AreEqual(warning,Bytes(h.World.Capture()));CollectionAssert.AreEqual(boss,Bytes(h.Session.Boss));
        h.SetPaused(false);for(var i=0;i<12;i++)h.Tick(.1);Assert.AreEqual(3,h.World.HostileProjectiles.Count);
        h.SetPaused(true);var live=Bytes(h.World.Capture());Assert.IsTrue(h.Reload(),h.Error);CollectionAssert.AreEqual(live,Bytes(h.World.Capture()));h.Tick(60);CollectionAssert.AreEqual(live,Bytes(h.World.Capture()));
        CollectionAssert.AreEqual(first,Bytes(h.State.Run.CompletedBoss));CollectionAssert.AreEqual(second,Bytes(h.State.Run.CompletedSecondBoss));Assert.Zero(h.World.ChargeStates.Count);
    }
    [TestCase(false)][TestCase(true)]
    public void RewardAndChapterCompletionRetryPreserveThreeReceiptsGrowthAndExplicitNewChallenge(bool failSettlement)
    {
        var h=Intro(path);Active(h);var first=Bytes(h.State.Run.CompletedBoss);var second=Bytes(h.State.Run.CompletedSecondBoss);var unlocks=h.State.Profile.UnlockedWeapons.ToArray();var gold=h.State.Profile.Gold;
        h.Session.Hit(h.Session.BossRules.BossActorId,h.Session.BossRules.MaxHealth);Assert.IsTrue(h.Checkpoint(),h.Error);Assert.AreEqual(gold+350,h.State.Profile.Gold);Assert.AreEqual(BossDefinition.JudahPolicy,h.Session.Boss.Reward.PolicyId);
        var candidates=Bytes(h.Session.BossChoices.ToArray());Assert.IsTrue(h.Reload(),h.Error);CollectionAssert.AreEqual(candidates,Bytes(h.Session.BossChoices.ToArray()));
        // Explicit single pending-growth fixture; candidate count is not the number of Choose calls.
        var state=new SaveStore(path).Load();state.Run.PendingLevels=1;state.Run.Choices=h.Session.Inventory.Choices(state.Profile.UnlockedWeapons,Passives,3,state.Run.ChoiceSerial).ToList();Assert.Greater(state.Run.Choices.Count,0);new SaveStore(path).Commit(state,state.Revision,"R09-one-pending-growth-fixture");
        var fail=false;h=new AdamHost(AdamCatalog.Load(),new SaveStore(path,()=>{if(fail)throw new IOException("R09 exact commit boundary");}));Assert.IsNull(h.Error);
        var reward=h.Session.Boss.Reward.RewardId;var candidate=h.Session.BossChoices.First();fail=!failSettlement;Assert.IsTrue(h.ChooseBossReward(reward,candidate.Id));
        if(fail)
        {
            Assert.NotNull(h.Error);var frozen=Bytes(h.State);h.Tick(60);Assert.IsFalse(h.ChooseBossReward(reward,candidate.Id));CollectionAssert.AreEqual(frozen,Bytes(h.State));fail=false;Assert.IsTrue(h.RetrySave(),h.Error);Assert.IsTrue(h.Reload(),h.Error);
        }
        Assert.AreEqual(1,h.State.Run.PendingLevels);Assert.IsFalse(h.CompleteBossSettlement());var chooseCalls=0;
        while(h.Session.PendingChoices.Count>0){Assert.IsTrue(h.Choose(h.Session.PendingChoices[0]));chooseCalls++;}Assert.AreEqual(1,chooseCalls);Assert.Zero(h.State.Run.PendingLevels);
        fail=failSettlement;var completed=h.CompleteBossSettlement();Assert.AreEqual(!failSettlement,completed);
        if(fail)
        {
            Assert.NotNull(h.Error);var frozen=Bytes(h.State);h.Tick(60);h.Start("bible");Assert.IsFalse(h.CompleteBossSettlement());CollectionAssert.AreEqual(frozen,Bytes(h.State));fail=false;Assert.IsTrue(h.RetrySave(),h.Error);
        }
        Assert.IsTrue(h.Reload(),h.Error);Assert.IsTrue(h.Paused);Assert.IsTrue(h.Session.ChapterComplete);Assert.IsFalse(h.ContentComplete);Assert.IsTrue(h.CanEnterExodus);Assert.IsFalse(h.AwaitingNextRound);Assert.AreEqual(10,h.State.Run.RoundNumber);Assert.IsNull(h.World);Assert.IsTrue(h.CanStartNew);
        Assert.AreEqual(gold+350+160+(candidate.Kind=="gold"?candidate.Gold:0),h.State.Profile.Gold);Assert.AreEqual(h.State.Run.MaxHealth,h.Session.CurrentHealth);CollectionAssert.AreEqual(unlocks,h.State.Profile.UnlockedWeapons);
        CollectionAssert.AreEqual(first,Bytes(h.State.Run.CompletedBoss));CollectionAssert.AreEqual(second,Bytes(h.State.Run.CompletedSecondBoss));Assert.AreEqual(9,h.State.Run.Boss.EncounterRound);Assert.IsTrue(h.State.Run.Boss.CompletionApplied);Assert.AreEqual(EvolutionRules.Sling,h.Session.WeaponForm("sling"));Assert.AreEqual(EvolutionRules.Bible,h.Session.WeaponForm("bible"));
        Assert.IsFalse(h.ChooseBossReward(reward,candidate.Id));Assert.IsFalse(h.CompleteBossSettlement());var run=Bytes(h.State.Run);var profile=Bytes(h.State.Profile);h.SetPaused(false);h.Tick(60);CollectionAssert.AreEqual(run,Bytes(h.State.Run));CollectionAssert.AreEqual(profile,Bytes(h.State.Profile));var disk=File.ReadAllBytes(path);Assert.IsTrue(h.Reload(),h.Error);CollectionAssert.AreEqual(disk,File.ReadAllBytes(path));CollectionAssert.AreEqual(run,Bytes(h.State.Run));
        var oldRun=h.State.Run.RunId;h.Start("bible");Assert.AreNotEqual(oldRun,h.State.Run.RunId);Assert.AreEqual(1,h.Session.RoundNumber);Assert.IsFalse(h.ContentComplete);CollectionAssert.AreEqual(profile,Bytes(h.State.Profile));
    }
}
