using System;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Json;
using BibleSurvivor.Core;
using BibleSurvivor.Unity;
using NUnit.Framework;

public sealed class AdamR15HostTests
{
    string dir,path;
    [SetUp]public void Setup(){dir=Path.Combine(Path.GetTempPath(),"adam-r15-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(dir);path=Path.Combine(dir,"save.json");}
    [TearDown]public void Cleanup(){Directory.Delete(dir,true);}
    static byte[] Bytes(object value){if(value==null)return new byte[0];using(var m=new MemoryStream()){new DataContractJsonSerializer(value.GetType()).WriteObject(m,value);return m.ToArray();}}
    static byte[] History(AdamHost h)=>Bytes(new[]{h.State.Run.CompletedBoss,h.State.Run.CompletedSecondBoss,h.State.Run.CompletedThirdBoss,h.State.Run.CompletedFourthBoss});
    public static AdamHost CompletedFourteen(string path){var h=AdamR14HostTests.Start(path);h.Session.Advance(180);Assert.IsTrue(h.Checkpoint(),h.Error);Assert.IsTrue(h.AwaitingR15);Assert.IsTrue(h.Reload(),h.Error);return h;}
    public static AdamHost Start(string path){var h=CompletedFourteen(path);h.SetPaused(false);Assert.AreEqual(15,h.Session.RoundNumber);return h;}
    static AdamHost Intro(string path,bool exhausted=true)
    {
        var h=Start(path);
        AdamR08HostTests.MaxEquipment(h);
        foreach(var id in new[]{"bible","jawbone","sling"})
            h.Session.Inventory.TryEvolve(EvolutionRules.Form(id));
        h.Session.Damage(20);
        h.Session.Advance(180);
        h.Session.Capture();
        if(!exhausted)
        {
            var run=h.State.Run;
            var maxGrants=new[]{run.CompletedBoss,run.CompletedSecondBoss,run.CompletedThirdBoss,run.CompletedFourthBoss}
                .Select(b=>b.Reward).SelectMany(r=>r.Candidates.Where(c=>c.Id==r.SelectedId&&c.Kind=="item"&&c.Level==5))
                .Select(c=>c.ItemId).ToHashSet();
            // Earlier rewards can fill all six slots without cloud. Lower a held passive without invalidating a receipt.
            var passive=run.Passives.Where(p=>p.Value==5&&!maxGrants.Contains(p.Key))
                .Select(p=>p.Key).OrderBy(id=>id,StringComparer.Ordinal).First();
            run.Passives[passive]=4;
        }
        h.State.Run.World=null;
        new SaveStore(path).Commit(h.State,h.State.Revision,"technical-r15-intro");
        Assert.IsTrue(h.Reload(),h.Error);
        return h;
    }
    static void ReloadExact(AdamHost h,string path){h.SetPaused(true);Assert.IsNull(h.Error);var world=Bytes(h.World.Capture());var boss=Bytes(h.Session.Boss);var disk=File.ReadAllBytes(path);h.Tick(30);Assert.IsTrue(h.Reload(),h.Error);Assert.IsTrue(h.Paused);CollectionAssert.AreEqual(disk,File.ReadAllBytes(path));CollectionAssert.AreEqual(world,Bytes(h.World.Capture()));CollectionAssert.AreEqual(boss,Bytes(h.Session.Boss));h.SetPaused(false);}
    [TestCase(false)][TestCase(true)]public void R14ReadOnlyBoundaryEntryRetryKeepsFourReceiptsEquipmentAnd260WithoutRepaying(bool inject)
    {
        var h=CompletedFourteen(path);var history=History(h);var forms=Bytes(h.State.Run.Forms);var profile=Bytes(h.State.Profile);var id=h.State.Run.RunId;var run=Bytes(h.State.Run);var disk=File.ReadAllBytes(path);h.Tick(30);Assert.IsTrue(h.Reload());CollectionAssert.AreEqual(run,Bytes(h.State.Run));CollectionAssert.AreEqual(disk,File.ReadAllBytes(path));Assert.IsFalse(h.ExodusComplete);Assert.IsFalse(h.CanEnterExodus);
        var fail=inject;h=new AdamHost(AdamCatalog.Load(),new SaveStore(path,()=>{if(fail)throw new IOException("R15 entry");}));h.SetPaused(false);Assert.AreEqual(15,h.Session.RoundNumber);var arena=h.World;if(fail){Assert.NotNull(h.Error);var frozen=Bytes(h.State);h.Tick(30);CollectionAssert.AreEqual(frozen,Bytes(h.State));CollectionAssert.AreEqual(disk,File.ReadAllBytes(path));fail=false;Assert.IsTrue(h.RetrySave(),h.Error);Assert.AreSame(arena,h.World);}Assert.IsTrue(h.Reload(),h.Error);Assert.AreEqual(id,h.State.Run.RunId);Assert.AreEqual(14,h.State.Run.PreviousNormalRound);CollectionAssert.AreEqual(history,History(h));CollectionAssert.AreEqual(forms,Bytes(h.State.Run.Forms));CollectionAssert.AreEqual(profile,Bytes(h.State.Profile));Assert.AreEqual(4,h.World.Capture().SchemaVersion);Assert.Zero(h.World.HostileProjectiles.Count);Assert.Zero(h.World.ChargeStates.Count);Assert.IsNull(h.World.ChariotDash);Assert.IsNull(h.Session.Boss);Assert.Zero(h.World.SimulationTime);
    }
    [Test]public void RealChainWarningProjectilesThresholdsAndKillCleanupResumeExactlyFromDisk()
    {
        var h=Intro(path);Assert.AreEqual(3,h.Session.Boss.Version);Assert.AreEqual(4,h.World.Capture().SchemaVersion);Assert.AreEqual("boss_intro",h.Session.Stage);var hp=h.Session.CurrentHealth;h.Tick(2);Assert.Zero(h.Session.Boss.IntroElapsed);AdamR12HostTests.ActivateAndSuppress(h,path);Assert.AreEqual(hp,h.Session.CurrentHealth);Assert.AreEqual(18,h.Session.BossRules.AttackDamage);for(var i=0;i<42;i++)h.Tick(.1);Assert.IsTrue(h.Session.Boss.Telegraphing);Assert.AreEqual(3,h.Session.Boss.ChainDirections.Count);ReloadExact(h,path);for(var i=0;i<10;i++)h.Tick(.1);Assert.AreEqual(3,h.World.HostileProjectiles.Count);var projectile=h.World.HostileProjectiles.First();ReloadExact(h,path);h.Tick(.1);var moved=h.World.HostileProjectiles.Single(x=>x.Id==projectile.Id);Assert.AreEqual(projectile.BornAt,moved.BornAt);Assert.IsTrue(projectile.X!=moved.X||projectile.Y!=moved.Y);h.Session.Hit(h.Session.BossRules.BossActorId,10400);h.Tick(.1);Assert.AreEqual(6,h.Session.Boss.Summoned);ReloadExact(h,path);h.Session.Hit(h.Session.BossRules.BossActorId,7800);h.Tick(.1);Assert.AreEqual(12,h.Session.Boss.Summoned);ReloadExact(h,path);h.Session.Hit(h.Session.BossRules.BossActorId,26000);h.World.Advance(.1,System.Numerics.Vector2.Zero);Assert.IsFalse(h.Session.Boss.Telegraphing);Assert.Zero(h.World.HostileProjectiles.Count);Assert.Zero(h.World.EnemyPositions.Count);Assert.IsTrue(h.Checkpoint(),h.Error);Assert.IsTrue(h.Reload(),h.Error);
    }
    [TestCase(false)][TestCase(true)]public void FrozenRewardAndGrowthRetryKeep500And100And25And280OnceThenExplicitNewR01(bool exhausted)
    {
        var h=Intro(path,exhausted);
        var loweredPassive=exhausted?null:h.Session.Inventory.Passives.Single(p=>p.Value<(p.Key=="stones"?2:5)).Key;
        AdamR12HostTests.ActivateAndSuppress(h,path);var history=History(h);var forms=Bytes(h.State.Run.Forms);var gold=h.State.Profile.Gold;h.Session.Hit(h.Session.BossRules.BossActorId,26000);Assert.AreEqual(gold+500+(exhausted?100:0),h.State.Profile.Gold);Assert.IsTrue(h.Checkpoint(),h.Error);var choices=Bytes(h.Session.BossChoices.ToArray());Assert.IsTrue(h.Reload(),h.Error);CollectionAssert.AreEqual(choices,Bytes(h.Session.BossChoices.ToArray()));var selected=h.Session.BossChoices.First(x=>x.Kind=="gold");var reward=h.Session.Boss.Reward.RewardId;
        if(!exhausted){var fail=true;h=new AdamHost(AdamCatalog.Load(),new SaveStore(path,()=>{if(fail)throw new IOException("R15 reward");}));Assert.IsTrue(h.ChooseBossReward(reward,selected.Id));Assert.NotNull(h.Error);var frozen=Bytes(h.State);h.Tick(30);Assert.IsFalse(h.ChooseBossReward(reward,selected.Id));CollectionAssert.AreEqual(frozen,Bytes(h.State));fail=false;Assert.IsTrue(h.RetrySave(),h.Error);Assert.IsTrue(h.Reload(),h.Error);h.State.Run.Passives[loweredPassive]=5;}
        else Assert.IsFalse(h.ChooseBossReward(reward,selected.Id));
        h.State.Run.PendingLevels=2;h.State.Run.Choices.Clear();h.State.Run.Choices.Add(AdamSession.GrowthGoldChoicePrefix+h.State.Run.ChoiceSerial);h.Session.Capture();if(!exhausted)h.State.Run.Passives[loweredPassive]=5;h.State.Run.World=null;new SaveStore(path).Commit(h.State,h.State.Revision,"technical-r15-pending2");Assert.IsTrue(h.Reload(),h.Error);var serial=h.State.Run.ChoiceSerial;var calls=0;while(h.Session.PendingChoices.Count>0){var full=h.Session.PendingChoices.Single();Assert.IsFalse(h.CompleteBossSettlement());Assert.IsTrue(h.Choose(full));calls++;Assert.IsFalse(h.Choose(full));}Assert.AreEqual(2,calls);Assert.AreEqual(serial+2,h.State.Run.ChoiceSerial);Assert.AreEqual(gold+500+100+calls*25,h.State.Profile.Gold);
        var failed=true;h=new AdamHost(AdamCatalog.Load(),new SaveStore(path,()=>{if(failed)throw new IOException("R15 settlement");}));Assert.IsFalse(h.CompleteBossSettlement());Assert.NotNull(h.Error);var complete=Bytes(h.State);h.Tick(30);Assert.IsFalse(h.CompleteBossSettlement());CollectionAssert.AreEqual(complete,Bytes(h.State));failed=false;Assert.IsTrue(h.RetrySave(),h.Error);Assert.IsTrue(h.Reload(),h.Error);Assert.IsTrue(h.ExodusComplete);Assert.IsFalse(h.ContentComplete);Assert.IsTrue(h.CanEnterLeviticus);Assert.IsFalse(h.Session.ChapterComplete);Assert.IsFalse(h.AwaitingNextRound);Assert.AreEqual(16,h.State.Run.RoundNumber);Assert.AreEqual(gold+500+100+calls*25+280,h.State.Profile.Gold);Assert.AreEqual(h.State.Run.MaxHealth,h.Session.CurrentHealth);CollectionAssert.AreEqual(history,History(h));CollectionAssert.AreEqual(forms,Bytes(h.State.Run.Forms));Assert.AreEqual(3,h.State.Run.Boss.Version);Assert.AreEqual(4,h.State.Run.CompletedFourthBoss.Version);var profile=Bytes(h.State.Profile);var run=Bytes(h.State.Run);var disk=File.ReadAllBytes(path);h=new AdamHost(AdamCatalog.Load(),new SaveStore(path));h.SetPaused(false);h.Tick(30);Assert.IsFalse(h.TryEnterExodus());CollectionAssert.AreEqual(run,Bytes(h.State.Run));CollectionAssert.AreEqual(disk,File.ReadAllBytes(path));h.Start("sling");Assert.AreEqual(1,h.Session.RoundNumber);Assert.AreEqual(2,h.State.SchemaVersion);Assert.IsFalse(h.ExodusComplete);Assert.IsNull(h.State.Run.CompletedFourthBoss);CollectionAssert.AreEqual(profile,Bytes(h.State.Profile));
    }
    [Test]public void DynamicBoss15CatalogPreservesAllFingerprintsAndRejects21AndFalseProjectileRules()
    {
        var c=AdamCatalog.Load();Assert.AreEqual(40,c.LastInstalledRound);foreach(var n in new[]{10,11,12,13,14,15})Assert.AreEqual(ExodusContent.Round(n).Fingerprint,c.ForRound(n).Fingerprint);Assert.AreEqual(3,c.SpeedsFor(15).Count);Assert.AreEqual(.35,c.SpeedsFor(15)["ex18"]);Assert.AreEqual(1200,c.ForRound(15).Waves.Sum(w=>w.Groups.Sum(g=>g.Count)));Assert.AreEqual(280,c.ForRound(15).CompletionGold);Assert.IsNull(c.ForRound(15).UnlockWeaponId);foreach(var phase in new[]{"normal","boss_intro","boss","boss_reward","boss_settlement"})Assert.AreEqual(15,c.ForCheckpoint(new RunState{RoundNumber=15,Stage=phase}).Number);Assert.AreEqual(15,c.ForCheckpoint(new RunState{RoundNumber=16,Stage="between_rounds"}).Number);Assert.Throws<InvalidDataException>(()=>c.ForRound(41));Assert.Throws<UnsupportedSaveVersionException>(()=>c.ForCheckpoint(new RunState{RoundNumber=41,Stage="normal"}));Assert.Throws<UnsupportedSaveVersionException>(()=>c.ForCheckpoint(new RunState{RoundNumber=41,Stage="round_settlement"}));Assert.Throws<UnsupportedSaveVersionException>(()=>c.ForCheckpoint(new RunState{RoundNumber=42,Stage="between_rounds"}));Assert.Throws<InvalidDataException>(()=>c.ForCheckpoint(new RunState{RoundNumber=15,Stage="round_settlement"}));Assert.AreEqual(15,BossDefinition.ForRound(9).AttackDamage);Assert.AreEqual(18,BossDefinition.ForRound(15).AttackDamage);var g=UnityEngine.Resources.Load<UnityEngine.TextAsset>("genesis-catalog").text;var e=UnityEngine.Resources.Load<UnityEngine.TextAsset>("exodus-catalog").text;Assert.Throws<InvalidDataException>(()=>new AdamCatalog(g,e.Replace("three_fixed_direction_projectiles","telegraphed_club_slam")));Assert.Throws<InvalidDataException>(()=>new AdamCatalog(g,e.Replace("\"projectile_cap\": 12","\"projectile_cap\": 11")));
    }
}
