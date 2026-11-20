using System;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Json;
using BibleSurvivor.Core;
using BibleSurvivor.Unity;
using NUnit.Framework;

public sealed class AdamR12HostTests
{
    string dir,path;
    [SetUp]public void Setup(){dir=Path.Combine(Path.GetTempPath(),"adam-r12-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(dir);path=Path.Combine(dir,"save.json");}
    [TearDown]public void Cleanup(){Directory.Delete(dir,true);}
    static byte[] Bytes(object value){if(value==null)return new byte[0];using(var m=new MemoryStream()){new DataContractJsonSerializer(value.GetType()).WriteObject(m,value);return m.ToArray();}}
    public static AdamHost CompletedEleven(string path)
    {var h=AdamR11HostTests.Start(path);h.Session.Advance(180);Assert.IsTrue(h.Checkpoint(),h.Error);Assert.IsTrue(h.AwaitingR12);Assert.IsTrue(h.Reload());return h;}
    public static AdamHost Intro(string path)
    {
        var h=CompletedEleven(path);h.SetPaused(false);Assert.AreEqual(12,h.Session.RoundNumber);h.Session.Damage(20);h.Session.Advance(180);h.Session.Capture();h.State.Run.World=null;new SaveStore(path).Commit(h.State,h.State.Revision,"technical-r12-intro");Assert.IsTrue(h.Reload(),h.Error);return h;
    }
    public static void ActivateAndSuppress(AdamHost h,string path)
    {
        h.SetPaused(false);for(var i=0;i<20;i++)h.Tick(.1);Assert.AreEqual("boss",h.Session.Stage);var s=h.World.Capture();var actor=s.Enemies.Single(x=>x.Id==h.Session.BossRules.BossActorId);actor.X=30;actor.Y=0;
        foreach(var id in h.Session.Inventory.Weapons.Keys){var w=s.Weapons.SingleOrDefault(x=>x.WeaponId==id);if(w==null){w=new BattleWeaponState{WeaponId=id,WeaponForm=h.Session.Inventory.Forms.TryGetValue(id,out var form)?form:null};s.Weapons.Add(w);}w.NextCycle=1000;w.BurstsRemaining=0;w.PendingBursts.Clear();w.NextBurst=0;}
        h.Session.Capture();h.State.Run.World=s;new SaveStore(path).Commit(h.State,h.State.Revision,"technical-r12-no-weapons");Assert.IsTrue(h.Reload(),h.Error);h.SetPaused(false);
    }
    static void ReloadExact(AdamHost h,string path)
    {
        h.SetPaused(true);var world=Bytes(h.World.Capture());var boss=Bytes(h.Session.Boss);var disk=File.ReadAllBytes(path);h.Tick(30);Assert.IsTrue(h.Reload(),h.Error);CollectionAssert.AreEqual(disk,File.ReadAllBytes(path));CollectionAssert.AreEqual(world,Bytes(h.World.Capture()));CollectionAssert.AreEqual(boss,Bytes(h.Session.Boss));Assert.IsTrue(h.Paused);h.SetPaused(false);
    }
    [TestCase(false)][TestCase(true)]public void R11BoundaryReadOnlyLoadAndR12EntryRetryPreserveIdentityAndThreeReceipts(bool injectFailure)
    {
        var h=CompletedEleven(path);var disk=File.ReadAllBytes(path);var run=Bytes(h.State.Run);var gold=h.State.Profile.Gold;var id=h.State.Run.RunId;var receipts=Bytes(new[]{h.State.Run.CompletedBoss,h.State.Run.CompletedSecondBoss,h.State.Run.CompletedThirdBoss});var forms=Bytes(h.State.Run.Forms);h.Tick(30);Assert.IsTrue(h.Reload());CollectionAssert.AreEqual(disk,File.ReadAllBytes(path));CollectionAssert.AreEqual(run,Bytes(h.State.Run));Assert.IsFalse(h.CanEnterExodus);Assert.IsFalse(h.ContentComplete);
        var fail=injectFailure;h=new AdamHost(AdamCatalog.Load(),new SaveStore(path,()=>{if(fail)throw new IOException("R12 entry");}));h.SetPaused(false);Assert.AreEqual(12,h.Session.RoundNumber);var arena=h.World;
        if(fail){Assert.NotNull(h.Error);var frozen=Bytes(h.State);h.Tick(30);CollectionAssert.AreEqual(frozen,Bytes(h.State));fail=false;Assert.IsTrue(h.RetrySave(),h.Error);Assert.AreSame(arena,h.World);}
        Assert.IsTrue(h.Reload());Assert.AreEqual(id,h.State.Run.RunId);Assert.AreEqual(11,h.State.Run.PreviousNormalRound);Assert.AreEqual(gold,h.State.Profile.Gold);CollectionAssert.AreEqual(receipts,Bytes(new[]{h.State.Run.CompletedBoss,h.State.Run.CompletedSecondBoss,h.State.Run.CompletedThirdBoss}));CollectionAssert.AreEqual(forms,Bytes(h.State.Run.Forms));Assert.Zero(h.World.SimulationTime);
    }
    [Test]public void IntroBoss4World5TelegraphDashThresholdsAndKillCleanupResumeFromDisk()
    {
        var h=Intro(path);var injured=h.Session.CurrentHealth;Assert.AreEqual("boss_intro",h.Session.Stage);Assert.AreEqual(4,h.Session.Boss.Version);Assert.AreEqual(5,h.World.Capture().SchemaVersion);h.Tick(2);Assert.Zero(h.Session.Boss.IntroElapsed);Assert.AreEqual(injured,h.Session.CurrentHealth);ActivateAndSuppress(h,path);Assert.AreEqual(injured,h.Session.CurrentHealth);Assert.AreEqual(20000,h.Session.BossRules.MaxHealth);Assert.AreEqual(400,h.Session.BossRules.KillGold);
        for(var i=0;i<51;i++)h.Tick(.1);Assert.IsNull(h.Error);Assert.AreEqual("boss",h.Session.Stage);Assert.NotNull(h.World.ChariotDash,"isolated living boss must expose warning at5.1");Assert.AreEqual("telegraph",h.World.ChariotDash.Phase);ReloadExact(h,path);var direction=h.World.ChariotDash.Direction;var origin=h.World.ChariotDash.Origin;
        for(var i=0;i<12;i++)h.Tick(.1);Assert.AreEqual("dash",h.World.ChariotDash.Phase);Assert.AreEqual(direction,h.World.ChariotDash.Direction);Assert.AreNotEqual(origin,h.World.ChariotDash.Origin);ReloadExact(h,path);h.Session.Hit(h.Session.BossRules.BossActorId,8000);h.Tick(.1);Assert.AreEqual(6,h.Session.Boss.Summoned);Assert.AreEqual(1,h.Session.Boss.SummonMask);ReloadExact(h,path);h.Session.Hit(h.Session.BossRules.BossActorId,6000);h.Tick(.1);Assert.AreEqual(12,h.Session.Boss.Summoned);Assert.AreEqual(3,h.Session.Boss.SummonMask);ReloadExact(h,path);h.Session.Hit(h.Session.BossRules.BossActorId,20000);Assert.IsNull(h.World.ChariotDash);Assert.IsFalse(h.Session.Boss.Telegraphing);h.World.Advance(.01,System.Numerics.Vector2.Zero);Assert.Zero(h.World.EnemyPositions.Count);Assert.Zero(h.World.ChargeStates.Count);Assert.IsTrue(h.Checkpoint(),h.Error);Assert.IsTrue(h.Reload(),h.Error);
    }
    [TestCase(false)][TestCase(true)]public void FrozenRewardRetryAndPendingGrowthKeep400Candidate100AndCompletion220Distinct(bool exhausted)
    {
        var h=Intro(path);if(exhausted){AdamR08HostTests.MaxEquipment(h);foreach(var id in new[]{"bible","jawbone","sling"})h.Session.Inventory.TryEvolve(EvolutionRules.Form(id));}ActivateAndSuppress(h,path);var gold=h.State.Profile.Gold;h.Session.Hit(h.Session.BossRules.BossActorId,20000);Assert.AreEqual(gold+400+(exhausted?100:0),h.State.Profile.Gold);Assert.IsTrue(h.Checkpoint(),h.Error);var choices=Bytes(h.Session.BossChoices.ToArray());var selected=h.Session.BossChoices.First();var reward=h.Session.Boss.Reward.RewardId;Assert.IsTrue(h.Reload());CollectionAssert.AreEqual(choices,Bytes(h.Session.BossChoices.ToArray()));Assert.AreEqual(gold+400+(exhausted?100:0),h.State.Profile.Gold);
        if(exhausted){Assert.AreEqual("gold",selected.Kind);Assert.AreEqual(100,selected.Gold);h.State.Run.PendingLevels=2;h.State.Run.Choices.Clear();h.State.Run.Choices.Add(AdamSession.GrowthGoldChoicePrefix+h.State.Run.ChoiceSerial);h.Session.Capture();h.State.Run.World=null;new SaveStore(path).Commit(h.State,h.State.Revision,"technical-r12-pending2");Assert.IsTrue(h.Reload(),h.Error);}
        var fail=true;h=new AdamHost(AdamCatalog.Load(),new SaveStore(path,()=>{if(fail)throw new IOException("R12 reward/growth selection");}));var firstGrowth=exhausted?h.Session.PendingChoices.Single():null;
        if(exhausted){Assert.IsFalse(h.ChooseBossReward(reward,selected.Id));Assert.IsTrue(h.Choose(firstGrowth));}else Assert.IsTrue(h.ChooseBossReward(reward,selected.Id));
        Assert.NotNull(h.Error);var frozen=Bytes(h.State);h.Tick(20);Assert.IsFalse(h.ChooseBossReward(reward,selected.Id));if(exhausted)Assert.IsFalse(h.Choose(firstGrowth));CollectionAssert.AreEqual(frozen,Bytes(h.State));fail=false;Assert.IsTrue(h.RetrySave(),h.Error);Assert.IsTrue(h.Reload(),h.Error);Assert.IsFalse(h.ChooseBossReward(reward,selected.Id));var pending=h.State.Run.PendingLevels;var actual=exhausted?1:0;
        while(h.Session.PendingChoices.Count>0){Assert.IsFalse(h.CompleteBossSettlement());var fullId=h.Session.PendingChoices.Single();Assert.IsTrue(fullId.StartsWith(AdamSession.GrowthGoldChoicePrefix,StringComparison.Ordinal));Assert.IsTrue(h.Choose(fullId));actual++;Assert.IsFalse(h.Choose(fullId));}Assert.AreEqual(pending+(exhausted?1:0),actual);Assert.IsTrue(h.CompleteBossSettlement(),h.Error);Assert.IsFalse(h.CompleteBossSettlement());Assert.AreEqual(gold+400+(selected.Kind=="gold"?100:0)+actual*25+220,h.State.Profile.Gold);Assert.IsTrue(h.AwaitingR13);Assert.IsFalse(h.ContentComplete);Assert.IsFalse(h.Session.ChapterComplete);Assert.AreEqual(13,h.State.Run.RoundNumber);Assert.AreEqual(h.State.Run.MaxHealth,h.Session.CurrentHealth);Assert.IsTrue(h.AwaitingNextRound);var complete=Bytes(h.State.Run);var profile=Bytes(h.State.Profile);var bytes=File.ReadAllBytes(path);h=new AdamHost(AdamCatalog.Load(),new SaveStore(path));CollectionAssert.AreEqual(complete,Bytes(h.State.Run));CollectionAssert.AreEqual(bytes,File.ReadAllBytes(path));Assert.AreEqual(4,h.State.Run.Boss.Version);Assert.NotNull(h.State.Run.CompletedBoss);Assert.NotNull(h.State.Run.CompletedSecondBoss);Assert.NotNull(h.State.Run.CompletedThirdBoss);h.Tick(30);Assert.IsFalse(h.TryEnterExodus());CollectionAssert.AreEqual(complete,Bytes(h.State.Run));var receipt=Bytes(h.State.Run.Boss);h.SetPaused(false);Assert.AreEqual(13,h.Session.RoundNumber);CollectionAssert.AreEqual(receipt,Bytes(h.State.Run.CompletedFourthBoss));CollectionAssert.AreEqual(profile,Bytes(h.State.Profile));
    }
    [Test]public void CatalogValidatesMosesBossAndChargeWithoutChangingR10R11AndRejectsFuture13()
    {
        var c=AdamCatalog.Load();Assert.AreEqual(60,c.LastInstalledRound);Assert.AreEqual("IunkZmlC50vfCA3mEtdpX5iiLjkDjk2gkFVsEXpZF9A=",c.ForRound(10).Fingerprint);Assert.AreEqual("GLT9uoWhUnPu8fFrWqeQKDYBrDWEiNHqazqM+/7eHFg=",c.ForRound(11).Fingerprint);Assert.AreEqual(ExodusContent.Round(12).Fingerprint,c.ForRound(12).Fingerprint);Assert.AreEqual(.6,c.SpeedsFor(12)["ex9"]);Assert.IsTrue(BattleWorld.UsesNormalCharge(12,"ex9"));Assert.IsFalse(BattleWorld.UsesNormalCharge(11,"ex6"));foreach(var phase in new[]{"normal","boss_intro","boss","boss_reward","boss_settlement"})Assert.AreEqual(12,c.ForCheckpoint(new RunState{RoundNumber=12,Stage=phase}).Number);Assert.AreEqual(12,c.ForCheckpoint(new RunState{RoundNumber=13,Stage="between_rounds"}).Number);Assert.Throws<InvalidDataException>(()=>c.ForRound(61));Assert.Throws<InvalidDataException>(()=>c.ForCheckpoint(new RunState{RoundNumber=13,Stage="boss"}));var genesis=UnityEngine.Resources.Load<UnityEngine.TextAsset>("genesis-catalog").text;var exodus=UnityEngine.Resources.Load<UnityEngine.TextAsset>("exodus-catalog").text;
        Assert.Throws<InvalidDataException>(()=>new AdamCatalog(genesis,exodus.Replace("absolute_fixed_direction_contact_dash","telegraphed_club_slam")));Assert.Throws<InvalidDataException>(()=>new AdamCatalog(genesis,exodus.Replace("telegraphed_fixed_dash","chase")));
    }
}
