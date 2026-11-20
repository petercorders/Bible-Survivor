using System;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Json;
using BibleSurvivor.Core;
using BibleSurvivor.Unity;
using NUnit.Framework;

public sealed class AdamR14HostTests
{
    string dir,path;
    [SetUp]public void Setup(){dir=Path.Combine(Path.GetTempPath(),"adam-r14-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(dir);path=Path.Combine(dir,"save.json");}
    [TearDown]public void Cleanup(){Directory.Delete(dir,true);}
    static byte[] Bytes(object value){if(value==null)return new byte[0];using(var m=new MemoryStream()){new DataContractJsonSerializer(value.GetType()).WriteObject(m,value);return m.ToArray();}}
    public static AdamHost CompletedThirteen(string path)
    {var h=AdamR13HostTests.Start(path);h.Session.Advance(180);Assert.IsTrue(h.Checkpoint(),h.Error);Assert.IsTrue(h.AwaitingR14);Assert.IsFalse(h.ContentComplete);Assert.IsTrue(h.Reload(),h.Error);return h;}
    public static AdamHost Start(string path){var h=CompletedThirteen(path);h.SetPaused(false);Assert.AreEqual(14,h.Session.RoundNumber);Assert.IsNull(h.Error);return h;}
    [TestCase(false)][TestCase(true)]public void R13ReadOnlyBoundaryEntryPreservesFourthAndPriorReceiptsWithoutRepaying(bool inject)
    {
        var h=CompletedThirteen(path);var previous=Bytes(h.State.Run.CompletedFourthBoss);var earlier=Bytes(new[]{h.State.Run.CompletedBoss,h.State.Run.CompletedSecondBoss,h.State.Run.CompletedThirdBoss});var forms=Bytes(h.State.Run.Forms);var profile=Bytes(h.State.Profile);var id=h.State.Run.RunId;var before=Bytes(h.State.Run);var disk=File.ReadAllBytes(path);h.Tick(30);Assert.IsTrue(h.Reload());CollectionAssert.AreEqual(disk,File.ReadAllBytes(path));CollectionAssert.AreEqual(before,Bytes(h.State.Run));Assert.IsFalse(h.CanEnterExodus);Assert.IsFalse(h.CanStartNew);
        var fail=inject;h=new AdamHost(AdamCatalog.Load(),new SaveStore(path,()=>{if(fail)throw new IOException("R14 entry");}));h.SetPaused(false);Assert.AreEqual(14,h.Session.RoundNumber);var arena=h.World;
        if(fail){Assert.NotNull(h.Error);var frozen=Bytes(h.State);h.Tick(30);h.SetPaused(false);CollectionAssert.AreEqual(frozen,Bytes(h.State));CollectionAssert.AreEqual(disk,File.ReadAllBytes(path));fail=false;Assert.IsTrue(h.RetrySave());Assert.AreSame(arena,h.World);}
        Assert.IsTrue(h.Reload(),h.Error);Assert.AreEqual(id,h.State.Run.RunId);Assert.AreEqual(13,h.State.Run.PreviousNormalRound);Assert.IsNull(h.Session.Boss);CollectionAssert.AreEqual(previous,Bytes(h.State.Run.CompletedFourthBoss));CollectionAssert.AreEqual(earlier,Bytes(new[]{h.State.Run.CompletedBoss,h.State.Run.CompletedSecondBoss,h.State.Run.CompletedThirdBoss}));CollectionAssert.AreEqual(forms,Bytes(h.State.Run.Forms));CollectionAssert.AreEqual(profile,Bytes(h.State.Profile));Assert.AreEqual(3,h.World.Capture().SchemaVersion);Assert.Zero(h.World.SimulationTime);Assert.Zero(h.World.HostileProjectiles.Count);Assert.Zero(h.World.ChargeStates.Count);Assert.IsNull(h.World.ChariotDash);Assert.AreEqual(-1,h.State.Run.LastHitTime);
    }
    [Test]public void ActualCoreEx13LeftAndRightGroupsKeepIndependentHitPointsBeforeConverging()
    {
        var h=Start(path);var snapshot=h.World.Capture();foreach(var id in h.Session.Inventory.Weapons.Keys)snapshot.Weapons.Add(new BattleWeaponState{WeaponId=id,WeaponForm=h.Session.Inventory.Forms.TryGetValue(id,out var form)?form:null,NextCycle=1000});h.Session.Capture();h.State.Run.World=snapshot;new SaveStore(path).Commit(h.State,h.State.Revision,"technical-r14-delayed-weapons");Assert.IsTrue(h.Reload(),h.Error);h.SetPaused(false);for(var i=0;i<30;i++)h.Tick(.1);var actors=h.Session.Targets.Values.Where(x=>x.MonsterId=="ex13").OrderBy(x=>x.Id).ToArray();Assert.GreaterOrEqual(actors.Length,2);Assert.IsTrue(actors.Any(x=>h.World.EnemyPositions[x.Id].X<h.World.PlayerPosition.X));Assert.IsTrue(actors.Any(x=>h.World.EnemyPositions[x.Id].X>h.World.PlayerPosition.X));var first=actors[0].Id;var other=actors[1].Id;Assert.AreEqual(220,h.Session.Targets[other].Health);h.Session.Hit(first,7);Assert.AreEqual(213,h.Session.Targets[first].Health);Assert.AreEqual(220,h.Session.Targets[other].Health);h.Session.Kill(first);h.State.Run.Drops.Clear(); /* Direct session kill has no World drop geometry: discard this technical drop. */ h.World.Advance(.1,System.Numerics.Vector2.Zero);Assert.AreEqual(220,h.Session.Targets[other].Health);h.SetPaused(true);Assert.IsNull(h.Error);var bytes=Bytes(h.World.Capture());Assert.IsTrue(h.Reload(),h.Error);CollectionAssert.AreEqual(bytes,Bytes(h.World.Capture()));
    }
    [Test]public void CurrentNormalWorldAndFourHistoriesResumeExactlyWithoutR12DashLeakage()
    {
        var h=Start(path);h.Tick(.1);h.SetPaused(true);var world=Bytes(h.World.Capture());var run=Bytes(h.State.Run);var disk=File.ReadAllBytes(path);h.Tick(20);Assert.IsTrue(h.Reload(),h.Error);CollectionAssert.AreEqual(world,Bytes(h.World.Capture()));CollectionAssert.AreEqual(run,Bytes(h.State.Run));CollectionAssert.AreEqual(disk,File.ReadAllBytes(path));Assert.IsTrue(h.Paused);Assert.AreEqual(4,h.State.Run.CompletedFourthBoss.Version);Assert.IsNull(h.State.Run.Boss);Assert.Zero(h.World.ChargeStates.Count);Assert.Zero(h.World.HostileProjectiles.Count);Assert.IsNull(h.World.ChariotDash);
    }
    [TestCase(false)][TestCase(true)]public void GrowthAnd260CommitFailureFreezeSameSerialAndCannotDoublePay(bool finalChoiceFailure)
    {
        var h=Start(path);AdamR08HostTests.MaxEquipment(h);h.Session.Advance(1);h.Session.Damage(20);var target=h.Session.Targets.Keys.First();h.Session.Kill(target);h.State.Run.Drops[target]=1000;h.Session.Capture();h.State.Run.World=null;new SaveStore(path).Commit(h.State,h.State.Revision,"technical-r14-drop1000");Assert.IsTrue(h.Reload());var gold=h.State.Profile.Gold;var unlocked=h.State.Profile.UnlockedWeapons.ToArray();h.Session.Advance(180);Assert.AreEqual("round_settlement",h.Session.Stage);Assert.Greater(h.State.Run.PendingLevels,1);Assert.AreEqual(gold,h.State.Profile.Gold);Assert.IsFalse(h.CompleteNormalSettlement());Assert.IsTrue(h.Checkpoint());Assert.IsTrue(h.Reload());var total=h.State.Run.PendingLevels;var calls=0;var serial=h.State.Run.ChoiceSerial;
        if(finalChoiceFailure)while(h.State.Run.PendingLevels>1){Assert.IsTrue(h.Choose(h.Session.PendingChoices.Single()));calls++;}
        var fail=true;h=new AdamHost(AdamCatalog.Load(),new SaveStore(path,()=>{if(fail)throw new IOException("R14 growth/260");}));var choice=h.Session.PendingChoices.Single();Assert.IsTrue(choice.StartsWith(AdamSession.GrowthGoldChoicePrefix,StringComparison.Ordinal));Assert.IsTrue(h.Choose(choice));calls++;Assert.NotNull(h.Error);var frozen=Bytes(h.State);h.Tick(30);Assert.IsFalse(h.Choose(choice));Assert.IsFalse(h.CompleteNormalSettlement());CollectionAssert.AreEqual(frozen,Bytes(h.State));fail=false;Assert.IsTrue(h.RetrySave());Assert.IsTrue(h.Reload());while(h.Session.PendingChoices.Count>0){var full=h.Session.PendingChoices.Single();Assert.IsTrue(h.Choose(full));calls++;Assert.IsFalse(h.Choose(full));}
        Assert.AreEqual(total,calls);Assert.AreEqual(serial+calls,h.State.Run.ChoiceSerial);Assert.AreEqual(gold+calls*25+260,h.State.Profile.Gold);Assert.IsFalse(h.ContentComplete);Assert.IsTrue(h.AwaitingR15);Assert.IsFalse(h.Session.ChapterComplete);Assert.AreEqual(15,h.State.Run.RoundNumber);Assert.AreEqual(h.State.Run.MaxHealth,h.Session.CurrentHealth);CollectionAssert.AreEqual(unlocked,h.State.Profile.UnlockedWeapons);Assert.IsFalse(h.CompleteNormalSettlement());Assert.IsTrue(h.AwaitingNextRound);Assert.NotNull(h.State.Run.CompletedFourthBoss);
    }
    [TestCase(false)][TestCase(true)]public void CompletionNewProcessOrDeathOnlyAllowsExplicitR01WithProfileRetained(bool death)
    {
        var h=Start(path);if(death)h.Session.Damage(100000);else h.Session.Advance(180);Assert.IsTrue(h.Checkpoint(),h.Error);var profile=Bytes(h.State.Profile);var id=h.State.Run?.RunId;var run=Bytes(h.State.Run);var disk=File.ReadAllBytes(path);h=new AdamHost(AdamCatalog.Load(),new SaveStore(path));h.Tick(30);CollectionAssert.AreEqual(run,Bytes(h.State.Run));CollectionAssert.AreEqual(disk,File.ReadAllBytes(path));Assert.IsFalse(h.TryEnterExodus());
        if(!death){Assert.IsFalse(h.ContentComplete);Assert.IsTrue(h.AwaitingR15);Assert.AreEqual(15,h.State.Run.RoundNumber);var fourth=Bytes(h.State.Run.CompletedFourthBoss);h.SetPaused(false);Assert.AreEqual(15,h.Session.RoundNumber);CollectionAssert.AreEqual(fourth,Bytes(h.State.Run.CompletedFourthBoss));CollectionAssert.AreEqual(profile,Bytes(h.State.Profile));return;}
        h.Start("sling");Assert.AreEqual(1,h.Session.RoundNumber);Assert.AreEqual(2,h.State.SchemaVersion);Assert.IsNull(h.State.Run.CompletedFourthBoss);Assert.AreNotEqual(id,h.State.Run.RunId);CollectionAssert.AreEqual(profile,Bytes(h.State.Profile));
    }
    [Test]public void CanonicalRound14SpeedsAndCheckpointKeepAllEarlierFingerprintsAndReject15Combat()
    {
        var c=AdamCatalog.Load();Assert.AreEqual(50,c.LastInstalledRound);foreach(var round in new[]{10,11,12,13,14})Assert.AreEqual(ExodusContent.Round(round).Fingerprint,c.ForRound(round).Fingerprint);Assert.AreEqual(1140,c.ForRound(14).Waves.Sum(w=>w.Groups.Sum(g=>g.Count)));Assert.AreEqual(5200,c.ForRound(14).RawExperience);Assert.AreEqual(260,c.ForRound(14).CompletionGold);Assert.IsNull(c.ForRound(14).UnlockWeaponId);Assert.AreEqual(3,c.SpeedsFor(14).Count);Assert.AreEqual(.8,c.SpeedsFor(14)["ex13"]);Assert.IsFalse(c.SpeedsFor(13).ContainsKey("ex13"));Assert.AreEqual(14,c.ForCheckpoint(new RunState{RoundNumber=14,Stage="round_settlement"}).Number);Assert.AreEqual(14,c.ForCheckpoint(new RunState{RoundNumber=15,Stage="between_rounds"}).Number);Assert.Throws<InvalidDataException>(()=>c.ForRound(51));Assert.Throws<UnsupportedSaveVersionException>(()=>c.ForCheckpoint(new RunState{RoundNumber=51,Stage="normal"}));Assert.Throws<InvalidDataException>(()=>c.ForCheckpoint(new RunState{RoundNumber=14,Stage="boss"}));
    }
}
