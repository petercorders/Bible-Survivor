using System;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Json;
using BibleSurvivor.Core;
using BibleSurvivor.Unity;
using NUnit.Framework;

public sealed class AdamR11HostTests
{
    string dir,path;
    [SetUp]public void Setup(){dir=Path.Combine(Path.GetTempPath(),"adam-r11-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(dir);path=Path.Combine(dir,"save.json");}
    [TearDown]public void Cleanup(){Directory.Delete(dir,true);}
    static byte[] Bytes(object value){using(var m=new MemoryStream()){new DataContractJsonSerializer(value.GetType()).WriteObject(m,value);return m.ToArray();}}
    public static AdamHost CompletedTen(string path)
    {
        var h=AdamR10HostTests.CompletedGenesis(path);Assert.IsTrue(h.TryEnterExodus(),h.Error);h.Session.Advance(180);Assert.IsTrue(h.Checkpoint(),h.Error);Assert.IsTrue(h.AwaitingR11);Assert.IsFalse(h.ContentComplete);Assert.IsTrue(h.Reload());return h;
    }
    public static AdamHost Start(string path)
    {var h=CompletedTen(path);h.SetPaused(false);Assert.AreEqual(11,h.Session.RoundNumber);Assert.IsNull(h.Error);return h;}
    [Test]public void R10BoundaryLoadsWithoutWritingAndResumeEntersR11WithCorrectSpeedSet()
    {
        var h=CompletedTen(path);var disk=File.ReadAllBytes(path);var gold=h.State.Profile.Gold;var run=Bytes(h.State.Run);Assert.IsTrue(h.Paused);h.Tick(60);Assert.IsTrue(h.Reload());CollectionAssert.AreEqual(disk,File.ReadAllBytes(path));CollectionAssert.AreEqual(run,Bytes(h.State.Run));Assert.IsTrue(h.AwaitingNextRound);Assert.IsFalse(h.CanStartNew);Assert.IsFalse(h.CanEnterExodus);Assert.IsFalse(h.TryEnterExodus());
        var first=Bytes(h.State.Run.CompletedBoss);var second=Bytes(h.State.Run.CompletedSecondBoss);var third=Bytes(h.State.Run.CompletedThirdBoss);var forms=Bytes(h.State.Run.Forms);var id=h.State.Run.RunId;h.SetPaused(false);Assert.AreEqual(11,h.Session.RoundNumber);Assert.AreEqual(10,h.State.Run.PreviousNormalRound);Assert.AreEqual(gold,h.State.Profile.Gold);Assert.AreEqual(id,h.State.Run.RunId);Assert.Zero(h.World.SimulationTime);Assert.AreEqual(-1,h.State.Run.LastHitTime);Assert.Zero(h.World.HostileProjectiles.Count);Assert.Zero(h.World.ChargeStates.Count);CollectionAssert.AreEqual(first,Bytes(h.State.Run.CompletedBoss));CollectionAssert.AreEqual(second,Bytes(h.State.Run.CompletedSecondBoss));CollectionAssert.AreEqual(third,Bytes(h.State.Run.CompletedThirdBoss));CollectionAssert.AreEqual(forms,Bytes(h.State.Run.Forms));
        h.Tick(.1);h.SetPaused(true);var world=Bytes(h.World.Capture());var bytes=File.ReadAllBytes(path);Assert.IsTrue(h.Reload(),h.Error);CollectionAssert.AreEqual(world,Bytes(h.World.Capture()));CollectionAssert.AreEqual(bytes,File.ReadAllBytes(path));Assert.IsTrue(h.Paused);
    }
    [Test]public void R11EntrySaveFailureFreezesOneArenaAndRetryDoesNotRepay180()
    {
        var h=CompletedTen(path);var gold=h.State.Profile.Gold;var id=h.State.Run.RunId;var fail=true;h=new AdamHost(AdamCatalog.Load(),new SaveStore(path,()=>{if(fail)throw new IOException("R11 entry");}));h.SetPaused(false);Assert.NotNull(h.Error);Assert.AreEqual(11,h.Session.RoundNumber);var arena=h.World;var frozen=Bytes(h.State);h.Tick(30);h.SetPaused(false);Assert.IsFalse(h.TryEnterExodus());CollectionAssert.AreEqual(frozen,Bytes(h.State));Assert.AreSame(arena,h.World);fail=false;Assert.IsTrue(h.RetrySave(),h.Error);Assert.AreSame(arena,h.World);Assert.IsTrue(h.Reload());Assert.AreEqual(11,h.Session.RoundNumber);Assert.AreEqual(id,h.State.Run.RunId);Assert.AreEqual(gold,h.State.Profile.Gold);Assert.IsFalse(h.AwaitingR11);
    }
    [Test]public void GrowthMustResolveBefore200AndFinalChoiceRetryCannotDoubleGrant()
    {
        var h=Start(path);AdamR08HostTests.MaxEquipment(h);h.Session.Advance(1);h.Session.Damage(20);var enemy=h.Session.Targets.Keys.First();h.Session.Kill(enemy);h.State.Run.Drops[enemy]=1000;h.Session.Capture();h.State.Run.World=null;new SaveStore(path).Commit(h.State,h.State.Revision,"technical-growth-drop1000");Assert.IsTrue(h.Reload());var gold=h.State.Profile.Gold;var unlocks=h.State.Profile.UnlockedWeapons.ToArray();h.Session.Advance(180);Assert.AreEqual("round_settlement",h.Session.Stage);Assert.Greater(h.State.Run.PendingLevels,0);Assert.AreEqual(gold,h.State.Profile.Gold);Assert.IsFalse(h.CompleteNormalSettlement());Assert.IsTrue(h.Checkpoint());Assert.IsTrue(h.Reload());var pending=h.State.Run.PendingLevels;var serial=h.State.Run.ChoiceSerial;var calls=0;var first=h.Session.PendingChoices.Single();Assert.AreEqual(AdamSession.GrowthGoldChoicePrefix+serial,first);
        while(h.State.Run.PendingLevels>1){Assert.IsTrue(h.Choose(h.Session.PendingChoices.Single()));calls++;}var fail=true;h=new AdamHost(AdamCatalog.Load(),new SaveStore(path,()=>{if(fail)throw new IOException("R11 final growth/200 commit");}));var last=h.Session.PendingChoices.Single();Assert.IsTrue(h.Choose(last));calls++;Assert.NotNull(h.Error);Assert.AreEqual(pending,calls);Assert.AreEqual(gold+pending*25+200,h.State.Profile.Gold);var frozen=Bytes(h.State);h.Tick(30);Assert.IsFalse(h.Choose(last));Assert.IsFalse(h.CompleteNormalSettlement());CollectionAssert.AreEqual(frozen,Bytes(h.State));fail=false;Assert.IsTrue(h.RetrySave());Assert.IsTrue(h.Reload());Assert.IsTrue(h.AwaitingR12);Assert.IsFalse(h.ContentComplete);Assert.AreEqual(12,h.State.Run.RoundNumber);Assert.IsFalse(h.Session.ChapterComplete);Assert.IsTrue(h.AwaitingNextRound);Assert.IsFalse(h.CanEnterExodus);Assert.AreEqual(serial+pending,h.State.Run.ChoiceSerial);Assert.AreEqual(gold+pending*25+200,h.State.Profile.Gold);Assert.AreEqual(h.State.Run.MaxHealth,h.Session.CurrentHealth);CollectionAssert.AreEqual(unlocks,h.State.Profile.UnlockedWeapons);Assert.IsFalse(h.Choose(first));Assert.IsFalse(h.Choose(last));Assert.IsFalse(h.CompleteNormalSettlement());
    }
    [Test]public void CompletedR11DiskLoadKeepsThreeReceiptsAndExplicitResumeStartsR12()
    {
        var h=Start(path);var receipts=Bytes(new[]{h.State.Run.CompletedBoss,h.State.Run.CompletedSecondBoss,h.State.Run.CompletedThirdBoss});var forms=Bytes(h.State.Run.Forms);var id=h.State.Run.RunId;h.Session.Advance(180);Assert.IsTrue(h.Checkpoint());Assert.IsTrue(h.AwaitingR12);Assert.IsFalse(h.ContentComplete);var run=Bytes(h.State.Run);var disk=File.ReadAllBytes(path);h=new AdamHost(AdamCatalog.Load(),new SaveStore(path));Assert.IsTrue(h.Paused);h.Tick(30);CollectionAssert.AreEqual(run,Bytes(h.State.Run));CollectionAssert.AreEqual(disk,File.ReadAllBytes(path));h.SetPaused(false);Assert.AreEqual(12,h.Session.RoundNumber);Assert.AreEqual(id,h.State.Run.RunId);CollectionAssert.AreEqual(receipts,Bytes(new[]{h.State.Run.CompletedBoss,h.State.Run.CompletedSecondBoss,h.State.Run.CompletedThirdBoss}));CollectionAssert.AreEqual(forms,Bytes(h.State.Run.Forms));Assert.IsFalse(h.TryEnterExodus());
    }
    [Test]public void R11DeathStillRestartsR01AndCanonicalR10FingerprintAndSpeedsStayIndependent()
    {
        var c=AdamCatalog.Load();Assert.AreEqual(14,c.LastInstalledRound);Assert.AreEqual("IunkZmlC50vfCA3mEtdpX5iiLjkDjk2gkFVsEXpZF9A=",c.ForRound(10).Fingerprint);Assert.AreEqual(960,c.ForRound(11).Waves.Sum(w=>w.Groups.Sum(g=>g.Count)));Assert.AreEqual(4000,c.ForRound(11).RawExperience);Assert.AreEqual(200,c.ForRound(11).CompletionGold);Assert.IsNull(c.ForRound(11).UnlockWeaponId);Assert.AreEqual(3,c.SpeedsFor(10).Count);Assert.AreEqual(3,c.SpeedsFor(11).Count);Assert.IsFalse(c.SpeedsFor(10).ContainsKey("ex4"));Assert.AreEqual(.9,c.SpeedsFor(11)["ex6"]);Assert.AreEqual(11,c.ForCheckpoint(new RunState{RoundNumber=12,Stage="between_rounds"}).Number);Assert.Throws<InvalidDataException>(()=>c.ForRound(15));Assert.Throws<InvalidDataException>(()=>c.ForCheckpoint(new RunState{RoundNumber=15,Stage="normal"}));
        var h=Start(path);var profile=Bytes(h.State.Profile);h.Session.Damage(100000);Assert.IsTrue(h.Checkpoint());Assert.IsNull(h.State.Run);h.Start("sling");Assert.AreEqual(1,h.Session.RoundNumber);Assert.AreEqual(2,h.State.SchemaVersion);CollectionAssert.AreEqual(profile,Bytes(h.State.Profile));
    }
}
