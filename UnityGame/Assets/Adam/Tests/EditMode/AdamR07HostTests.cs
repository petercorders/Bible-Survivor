using System;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Json;
using BibleSurvivor.Core;
using BibleSurvivor.Unity;
using NUnit.Framework;
public sealed class AdamR07HostTests
{
    string dir,path;
    [SetUp]public void Setup(){dir=Path.Combine(Path.GetTempPath(),"adam-r07-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(dir);path=Path.Combine(dir,"save.json");}
    [TearDown]public void Cleanup(){Directory.Delete(dir,true);}
    static byte[] Bytes(object value){using(var m=new MemoryStream()){new DataContractJsonSerializer(value.GetType()).WriteObject(m,value);return m.ToArray();}}
    public static AdamHost Start(string path)
    {
        var h=AdamR06HostTests.Intro(path);h.SetPaused(false);for(var i=0;i<20;i++)h.Tick(.1);h.Session.Hit(h.Session.BossRules.BossActorId,h.Session.BossRules.MaxHealth);Assert.IsTrue(h.Checkpoint(),h.Error);Assert.IsTrue(h.ChooseBossReward(h.Session.Boss.Reward.RewardId,h.Session.BossChoices.Single(x=>x.Kind=="evolution").Id));Assert.IsTrue(h.CompleteBossSettlement());Assert.IsTrue(h.AwaitingR07);var first=Bytes(h.State.Run.CompletedBoss);var second=Bytes(h.State.Run.Boss);h.SetPaused(false);Assert.AreEqual(7,h.Session.RoundNumber);CollectionAssert.AreEqual(first,Bytes(h.State.Run.CompletedBoss));CollectionAssert.AreEqual(second,Bytes(h.State.Run.CompletedSecondBoss));Assert.IsNull(h.State.Run.Boss);return h;
    }
    public static void SpawnFixture(AdamHost h,string path)
    {
        // Geometry preparation only: clear earlier waves without collecting XP, retain real Core spawn positions/RNG.
        for(var i=0;i<61;i++){h.Session.Advance(1);foreach(var id in h.Session.Targets.Keys.ToArray())h.Session.Kill(id);h.State.Run.Drops.Clear();}h.Session.Advance(3);h.Session.Capture();var world=new BattleWorld(h.Session,AdamCatalog.Load().Speeds,7).Capture();foreach(var id in h.Session.Inventory.Weapons.Keys)world.Weapons.Add(new BattleWeaponState{WeaponId=id,WeaponForm=h.Session.Inventory.Forms.TryGetValue(id,out var form)?form:null,NextCycle=1000});h.State.Run.World=world;new SaveStore(path).Commit(h.State,h.State.Revision,"G3-Core-spawn-fixture");Assert.IsTrue(h.Reload(),h.Error);
    }
    [Test]public void TwoBossReceiptsFormsAndHistorySurviveR07MidWorldDiskLoad()
    {
        var h=Start(path);Assert.AreEqual(5,h.State.Run.PreviousNormalRound);Assert.AreEqual(3,h.State.Run.CompletedBoss.EncounterRound);Assert.AreEqual(6,h.State.Run.CompletedSecondBoss.EncounterRound);Assert.AreEqual(EvolutionRules.Sling,h.Session.WeaponForm("sling"));Assert.AreEqual(EvolutionRules.Bible,h.Session.WeaponForm("bible"));var first=Bytes(h.State.Run.CompletedBoss);var second=Bytes(h.State.Run.CompletedSecondBoss);h.Tick(.1);h.SetPaused(true);var world=Bytes(h.World.Capture());Assert.IsTrue(h.Reload(),h.Error);CollectionAssert.AreEqual(world,Bytes(h.World.Capture()));CollectionAssert.AreEqual(first,Bytes(h.State.Run.CompletedBoss));CollectionAssert.AreEqual(second,Bytes(h.State.Run.CompletedSecondBoss));Assert.Zero(h.World.ChargeStates.Count);
    }
    [Test]public void RealCoreG3SpawnAlternatesSidesWithIndependentHealthAndPauseReplay()
    {
        var h=Start(path);SpawnFixture(h,path);var twins=h.Session.Targets.Values.Where(x=>x.MonsterId=="g3").OrderBy(x=>x.Id).ToArray();Assert.GreaterOrEqual(twins.Length,2);Assert.Less(h.World.EnemyPositions[twins[0].Id].X,h.World.PlayerPosition.X);Assert.Greater(h.World.EnemyPositions[twins[1].Id].X,h.World.PlayerPosition.X);Assert.AreEqual(240,twins[1].Health);h.Session.Hit(twins[0].Id,7);Assert.AreEqual(233,h.Session.Targets[twins[0].Id].Health);Assert.AreEqual(240,h.Session.Targets[twins[1].Id].Health);h.Session.Kill(twins[0].Id);h.State.Run.Drops.Remove(twins[0].Id);h.SetPaused(false);h.Tick(.1);Assert.AreEqual(240,h.Session.Targets[twins[1].Id].Health);Assert.IsFalse(h.World.EnemyPositions.ContainsKey(twins[0].Id));h.SetPaused(true);Assert.IsNull(h.Error,"geometry checkpoint: "+h.Error);var before=Bytes(h.World.Capture());h.Tick(20);CollectionAssert.AreEqual(before,Bytes(h.World.Capture()));Assert.IsTrue(h.Reload(),h.Error);CollectionAssert.AreEqual(before,Bytes(h.World.Capture()));
    }
    [Test]public void GrowthThen120GoldUnlockHealAndBothReceiptsAreExactlyOnce()
    {
        var h=Start(path);var first=Bytes(h.State.Run.CompletedBoss);var second=Bytes(h.State.Run.CompletedSecondBoss);h.Session.Advance(10);h.Session.Damage(20);foreach(var id in h.Session.Targets.Keys.Take(5).ToArray())h.Session.Kill(id);var gold=h.State.Profile.Gold;var hp=h.Session.CurrentHealth;h.Session.Advance(180);Assert.IsTrue(h.Checkpoint(),h.Error);Assert.AreEqual("round_settlement",h.Session.Stage);Assert.AreEqual(gold,h.State.Profile.Gold);Assert.AreEqual(hp,h.Session.CurrentHealth);Assert.IsFalse(h.CompleteNormalSettlement());Assert.IsTrue(h.Reload());while(h.Session.PendingChoices.Count>0)Assert.IsTrue(h.Choose(h.Session.PendingChoices[0]));Assert.IsTrue(h.AwaitingR08);Assert.AreEqual(8,h.State.Run.RoundNumber);Assert.AreEqual(gold+120,h.State.Profile.Gold);Assert.IsTrue(h.State.Profile.UnlockedWeapons.Contains("elijah_chariot"));Assert.IsFalse(h.Session.Inventory.TryChoose("elijah_chariot",false));Assert.AreEqual(h.State.Run.MaxHealth,h.Session.CurrentHealth);Assert.IsFalse(h.CompleteNormalSettlement());Assert.IsTrue(h.Reload());CollectionAssert.AreEqual(first,Bytes(h.State.Run.CompletedBoss));CollectionAssert.AreEqual(second,Bytes(h.State.Run.CompletedSecondBoss));Assert.Throws<InvalidDataException>(()=>AdamCatalog.Load().ForRound(41));
    }
    [Test]public void CompletionCommitFailureFreezesPayloadAndRetryCannotDuplicate120Gold()
    {
        var h=Start(path);Assert.IsTrue(h.Checkpoint());var fail=true;h=new AdamHost(AdamCatalog.Load(),new SaveStore(path,()=>{if(fail)throw new IOException("R07 completion");}));h.SetPaused(false);var gold=h.State.Profile.Gold;h.Session.Advance(180);Assert.IsFalse(h.Checkpoint());var frozen=Bytes(h.State);h.Tick(60);Assert.IsFalse(h.CompleteNormalSettlement());CollectionAssert.AreEqual(frozen,Bytes(h.State));Assert.AreEqual(gold+120,h.State.Profile.Gold);fail=false;Assert.IsTrue(h.RetrySave(),h.Error);Assert.IsTrue(h.AwaitingR08);Assert.IsTrue(h.Reload());Assert.AreEqual(gold+120,h.State.Profile.Gold);
    }
    [Test]public void ReviewedThreeClipMappingKeepsBossNamesAndShortDeathContract()
    {
        var art=new AdamR07Art();Assert.AreEqual(3,art.Index.assets.Length);Assert.AreEqual(12,art.Index.assets.Sum(x=>x.frames));var ids=new[]{"g1","g2","g3"};var clips=new[]{"enm_g1-deceiving-mask_move_32","enm_g2-spotted-hide_move_32","enm_g3-discord-shade_move_32"};for(var i=0;i<3;i++){Assert.IsTrue(AdamRoundPresentation.R07Enemy(ids[i]));Assert.AreEqual(clips[i],AdamArtWorld.EnemyClip(ids[i]));Assert.AreEqual(i==1?7:9,art.Clip(clips[i]).fps);Assert.AreEqual(i==2?"fx_shadow-small_static_12x4":"fx_shadow-wide_static_24x6",AdamRoundPresentation.Shadow(ids[i]));}Assert.IsFalse(AdamArtWorld.BossDeathVisible(10,11));Assert.AreEqual("야곱",AdamRoundPresentation.Name(7));var definition=AdamCatalog.Load().ForRound(7);Assert.AreEqual(900,definition.Waves.Sum(x=>x.Groups.Sum(g=>g.Count)));Assert.AreEqual(3000,definition.RawExperience);
    }
}
