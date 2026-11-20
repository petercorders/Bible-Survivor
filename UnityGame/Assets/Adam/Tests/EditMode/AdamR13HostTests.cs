using System;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Json;
using BibleSurvivor.Core;
using BibleSurvivor.Unity;
using NUnit.Framework;

public sealed class AdamR13HostTests
{
    string dir,path;
    [SetUp]public void Setup(){dir=Path.Combine(Path.GetTempPath(),"adam-r13-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(dir);path=Path.Combine(dir,"save.json");}
    [TearDown]public void Cleanup(){Directory.Delete(dir,true);}
    static byte[] Bytes(object value){if(value==null)return new byte[0];using(var m=new MemoryStream()){new DataContractJsonSerializer(value.GetType()).WriteObject(m,value);return m.ToArray();}}
    public static AdamHost CompletedTwelve(string path)
    {
        var h=AdamR12HostTests.Intro(path);AdamR08HostTests.MaxEquipment(h);foreach(var id in new[]{"bible","jawbone","sling"})h.Session.Inventory.TryEvolve(EvolutionRules.Form(id));AdamR12HostTests.ActivateAndSuppress(h,path);h.Session.Hit(h.Session.BossRules.BossActorId,h.Session.BossRules.MaxHealth);Assert.IsTrue(h.Checkpoint(),h.Error);if(h.Session.Stage=="boss_reward")Assert.IsTrue(h.ChooseBossReward(h.Session.Boss.Reward.RewardId,h.Session.BossChoices[0].Id));while(h.Session.PendingChoices.Count>0)Assert.IsTrue(h.Choose(h.Session.PendingChoices[0]));Assert.IsTrue(h.CompleteBossSettlement(),h.Error);Assert.IsTrue(h.AwaitingR13);Assert.IsFalse(h.ContentComplete);Assert.IsTrue(h.Reload(),h.Error);return h;
    }
    public static AdamHost Start(string path){var h=CompletedTwelve(path);h.SetPaused(false);Assert.AreEqual(13,h.Session.RoundNumber);Assert.IsNull(h.Error);return h;}
    [TestCase(false)][TestCase(true)]public void R12ReadOnlyBoundaryEntryPreservesFourthAndPriorReceiptsWithoutRepaying(bool inject)
    {
        var h=CompletedTwelve(path);var previous=Bytes(h.State.Run.Boss);var earlier=Bytes(new[]{h.State.Run.CompletedBoss,h.State.Run.CompletedSecondBoss,h.State.Run.CompletedThirdBoss});var forms=Bytes(h.State.Run.Forms);var profile=Bytes(h.State.Profile);var id=h.State.Run.RunId;var before=Bytes(h.State.Run);var disk=File.ReadAllBytes(path);h.Tick(30);Assert.IsTrue(h.Reload());CollectionAssert.AreEqual(disk,File.ReadAllBytes(path));CollectionAssert.AreEqual(before,Bytes(h.State.Run));Assert.IsFalse(h.CanEnterExodus);Assert.IsFalse(h.CanStartNew);
        var fail=inject;h=new AdamHost(AdamCatalog.Load(),new SaveStore(path,()=>{if(fail)throw new IOException("R13 entry");}));h.SetPaused(false);Assert.AreEqual(13,h.Session.RoundNumber);var arena=h.World;
        if(fail){Assert.NotNull(h.Error);var frozen=Bytes(h.State);h.Tick(30);h.SetPaused(false);CollectionAssert.AreEqual(frozen,Bytes(h.State));CollectionAssert.AreEqual(disk,File.ReadAllBytes(path));fail=false;Assert.IsTrue(h.RetrySave());Assert.AreSame(arena,h.World);}
        Assert.IsTrue(h.Reload(),h.Error);Assert.AreEqual(id,h.State.Run.RunId);Assert.AreEqual(12,h.State.Run.PreviousNormalRound);Assert.IsNull(h.Session.Boss);CollectionAssert.AreEqual(previous,Bytes(h.State.Run.CompletedFourthBoss));CollectionAssert.AreEqual(earlier,Bytes(new[]{h.State.Run.CompletedBoss,h.State.Run.CompletedSecondBoss,h.State.Run.CompletedThirdBoss}));CollectionAssert.AreEqual(forms,Bytes(h.State.Run.Forms));CollectionAssert.AreEqual(profile,Bytes(h.State.Profile));Assert.AreEqual(3,h.World.Capture().SchemaVersion);Assert.Zero(h.World.SimulationTime);Assert.Zero(h.World.HostileProjectiles.Count);Assert.Zero(h.World.ChargeStates.Count);Assert.IsNull(h.World.ChariotDash);Assert.AreEqual(-1,h.State.Run.LastHitTime);
    }
    [Test]public void CurrentNormalWorldAndFourHistoriesResumeExactlyWithoutR12DashLeakage()
    {
        var h=Start(path);h.Tick(.1);h.SetPaused(true);var world=Bytes(h.World.Capture());var run=Bytes(h.State.Run);var disk=File.ReadAllBytes(path);h.Tick(20);Assert.IsTrue(h.Reload(),h.Error);CollectionAssert.AreEqual(world,Bytes(h.World.Capture()));CollectionAssert.AreEqual(run,Bytes(h.State.Run));CollectionAssert.AreEqual(disk,File.ReadAllBytes(path));Assert.IsTrue(h.Paused);Assert.AreEqual(4,h.State.Run.CompletedFourthBoss.Version);Assert.IsNull(h.State.Run.Boss);Assert.Zero(h.World.ChargeStates.Count);Assert.Zero(h.World.HostileProjectiles.Count);Assert.IsNull(h.World.ChariotDash);
    }
    [TestCase(false)][TestCase(true)]public void GrowthAnd240CommitFailureFreezeSameSerialAndCannotDoublePay(bool finalChoiceFailure)
    {
        var h=Start(path);AdamR08HostTests.MaxEquipment(h);h.Session.Advance(1);h.Session.Damage(20);var target=h.Session.Targets.Keys.First();h.Session.Kill(target);h.State.Run.Drops[target]=1000;h.Session.Capture();h.State.Run.World=null;new SaveStore(path).Commit(h.State,h.State.Revision,"technical-r13-drop1000");Assert.IsTrue(h.Reload());var gold=h.State.Profile.Gold;var unlocked=h.State.Profile.UnlockedWeapons.ToArray();h.Session.Advance(180);Assert.AreEqual("round_settlement",h.Session.Stage);Assert.Greater(h.State.Run.PendingLevels,1);Assert.AreEqual(gold,h.State.Profile.Gold);Assert.IsFalse(h.CompleteNormalSettlement());Assert.IsTrue(h.Checkpoint());Assert.IsTrue(h.Reload());var total=h.State.Run.PendingLevels;var calls=0;var serial=h.State.Run.ChoiceSerial;
        if(finalChoiceFailure)while(h.State.Run.PendingLevels>1){Assert.IsTrue(h.Choose(h.Session.PendingChoices.Single()));calls++;}
        var fail=true;h=new AdamHost(AdamCatalog.Load(),new SaveStore(path,()=>{if(fail)throw new IOException("R13 growth/240");}));var choice=h.Session.PendingChoices.Single();Assert.IsTrue(choice.StartsWith(AdamSession.GrowthGoldChoicePrefix,StringComparison.Ordinal));Assert.IsTrue(h.Choose(choice));calls++;Assert.NotNull(h.Error);var frozen=Bytes(h.State);h.Tick(30);Assert.IsFalse(h.Choose(choice));Assert.IsFalse(h.CompleteNormalSettlement());CollectionAssert.AreEqual(frozen,Bytes(h.State));fail=false;Assert.IsTrue(h.RetrySave());Assert.IsTrue(h.Reload());while(h.Session.PendingChoices.Count>0){var full=h.Session.PendingChoices.Single();Assert.IsTrue(h.Choose(full));calls++;Assert.IsFalse(h.Choose(full));}
        Assert.AreEqual(total,calls);Assert.AreEqual(serial+calls,h.State.Run.ChoiceSerial);Assert.AreEqual(gold+calls*25+240,h.State.Profile.Gold);Assert.IsTrue(h.AwaitingR14);Assert.IsFalse(h.ContentComplete);Assert.IsFalse(h.Session.ChapterComplete);Assert.AreEqual(14,h.State.Run.RoundNumber);Assert.AreEqual(h.State.Run.MaxHealth,h.Session.CurrentHealth);CollectionAssert.AreEqual(unlocked,h.State.Profile.UnlockedWeapons);Assert.IsFalse(h.CompleteNormalSettlement());Assert.IsTrue(h.AwaitingNextRound);Assert.NotNull(h.State.Run.CompletedFourthBoss);
    }
    [TestCase(false)][TestCase(true)]public void CompletionNewProcessOrDeathRespectsInstalledBoundaryAndProfile(bool death)
    {
        var h=Start(path);if(death)h.Session.Damage(100000);else h.Session.Advance(180);Assert.IsTrue(h.Checkpoint(),h.Error);var profile=Bytes(h.State.Profile);var run=Bytes(h.State.Run);var disk=File.ReadAllBytes(path);h=new AdamHost(AdamCatalog.Load(),new SaveStore(path));h.Tick(30);CollectionAssert.AreEqual(run,Bytes(h.State.Run));CollectionAssert.AreEqual(disk,File.ReadAllBytes(path));Assert.IsFalse(h.TryEnterExodus());if(death){h.Start("sling");Assert.AreEqual(1,h.Session.RoundNumber);Assert.AreEqual(2,h.State.SchemaVersion);}else{Assert.IsTrue(h.AwaitingR14);Assert.IsFalse(h.ContentComplete);var fourth=Bytes(h.State.Run.CompletedFourthBoss);h.SetPaused(false);Assert.AreEqual(14,h.Session.RoundNumber);CollectionAssert.AreEqual(fourth,Bytes(h.State.Run.CompletedFourthBoss));}CollectionAssert.AreEqual(profile,Bytes(h.State.Profile));
    }
    [Test]public void CanonicalRound13SpeedsAndCheckpointKeepAllEarlierFingerprintsAndReject14Combat()
    {
        var c=AdamCatalog.Load();Assert.AreEqual(50,c.LastInstalledRound);foreach(var round in new[]{10,11,12,13})Assert.AreEqual(ExodusContent.Round(round).Fingerprint,c.ForRound(round).Fingerprint);Assert.AreEqual(1080,c.ForRound(13).Waves.Sum(w=>w.Groups.Sum(g=>g.Count)));Assert.AreEqual(4800,c.ForRound(13).RawExperience);Assert.AreEqual(240,c.ForRound(13).CompletionGold);Assert.IsNull(c.ForRound(13).UnlockWeaponId);Assert.AreEqual(3,c.SpeedsFor(13).Count);Assert.AreEqual(.35,c.SpeedsFor(13)["ex12"]);Assert.IsFalse(c.SpeedsFor(12).ContainsKey("ex12"));Assert.AreEqual(13,c.ForCheckpoint(new RunState{RoundNumber=13,Stage="round_settlement"}).Number);Assert.AreEqual(13,c.ForCheckpoint(new RunState{RoundNumber=14,Stage="between_rounds"}).Number);Assert.Throws<InvalidDataException>(()=>c.ForRound(51));Assert.Throws<UnsupportedSaveVersionException>(()=>c.ForCheckpoint(new RunState{RoundNumber=51,Stage="normal"}));Assert.Throws<InvalidDataException>(()=>c.ForCheckpoint(new RunState{RoundNumber=13,Stage="boss"}));
    }
}
