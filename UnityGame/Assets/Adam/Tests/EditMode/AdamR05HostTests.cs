using System;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Json;
using BibleSurvivor.Core;
using BibleSurvivor.Unity;
using NUnit.Framework;
public sealed class AdamR05HostTests
{
    string dir,path;
    [SetUp]public void Setup(){dir=Path.Combine(Path.GetTempPath(),"adam-r05-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(dir);path=Path.Combine(dir,"save.json");}
    [TearDown]public void Cleanup(){Directory.Delete(dir,true);}
    static byte[] Bytes(object value){using(var m=new MemoryStream()){new DataContractJsonSerializer(value.GetType()).WriteObject(m,value);return m.ToArray();}}
    public static AdamHost Start(string path)
    {
        var h=AdamR04HostTests.Start(path);AdamR04HostTests.Charger(h,path);h.Session.Advance(180);Assert.IsTrue(h.Checkpoint(),h.Error);Assert.IsTrue(h.AwaitingR05);Assert.IsTrue(h.Reload());Assert.IsTrue(h.AwaitingR05);h.SetPaused(false);Assert.AreEqual(5,h.Session.RoundNumber);return h;
    }
    [Test]public void R04CompletionEntersR05WithReceiptsEvolutionAndNoChargeState()
    {
        var h=Start(path);Assert.AreEqual(4,h.State.Run.PreviousNormalRound);Assert.IsTrue(h.State.Run.CompletedBoss.CompletionApplied);Assert.AreEqual(EvolutionRules.Sling,h.Session.WeaponForm("sling"));Assert.IsFalse(h.State.Run.RoundCompletionApplied);Assert.AreEqual(0,h.World.ChargeStates.Count);Assert.IsTrue(h.Session.Targets.Values.All(x=>x.MonsterId.StartsWith("e")));
        for(var i=0;i<10;i++)h.Tick(.1);h.SetPaused(true);var before=Bytes(h.World.Capture());Assert.IsTrue(h.Reload(),h.Error);CollectionAssert.AreEqual(before,Bytes(h.World.Capture()));Assert.AreEqual(0,h.World.ChargeStates.Count);Assert.IsTrue(h.World.Capture().Charges.All(x=>false));
    }
    [Test]public void GrowthDefers80GoldUnlockHealAndBetween6LoadIsExactlyOnce()
    {
        var h=Start(path);h.Session.Advance(10);h.Session.Damage(20);foreach(var id in h.Session.Targets.Keys.Take(5).ToArray())h.Session.Kill(id);var gold=h.State.Profile.Gold;var hp=h.Session.CurrentHealth;h.Session.Advance(180);Assert.IsTrue(h.Checkpoint(),h.Error);Assert.AreEqual("round_settlement",h.Session.Stage);Assert.AreEqual(gold,h.State.Profile.Gold);Assert.AreEqual(hp,h.Session.CurrentHealth);Assert.IsTrue(h.Reload());while(h.Session.PendingChoices.Count>0)Assert.IsTrue(h.Choose(h.Session.PendingChoices[0]));Assert.IsTrue(h.AwaitingR06);Assert.AreEqual(6,h.State.Run.RoundNumber);Assert.AreEqual(gold+80,h.State.Profile.Gold);Assert.IsTrue(h.State.Profile.UnlockedWeapons.Contains("daniel_lion"));Assert.AreEqual(h.State.Run.MaxHealth,h.Session.CurrentHealth);Assert.IsFalse(h.Session.Inventory.TryChoose("daniel_lion",false));Assert.IsFalse(h.CompleteNormalSettlement());var run=Bytes(h.State.Run);var profile=Bytes(h.State.Profile);Assert.IsTrue(h.Reload());CollectionAssert.AreEqual(run,Bytes(h.State.Run));CollectionAssert.AreEqual(profile,Bytes(h.State.Profile));Assert.Throws<InvalidDataException>(()=>AdamCatalog.Load().ForRound(13));
    }
    [Test]public void CompletionWriteFailureFreezesPayloadAndRetryDoesNotGrantTwice()
    {
        var h=Start(path);Assert.IsTrue(h.Checkpoint());var fail=true;h=new AdamHost(AdamCatalog.Load(),new SaveStore(path,()=>{if(fail)throw new IOException("R05 completion gate");}));h.SetPaused(false);var gold=h.State.Profile.Gold;h.Session.Advance(180);Assert.IsFalse(h.Checkpoint());var frozen=Bytes(h.State);h.Tick(50);Assert.IsFalse(h.CompleteNormalSettlement());CollectionAssert.AreEqual(frozen,Bytes(h.State));Assert.AreEqual(gold+80,h.State.Profile.Gold);fail=false;Assert.IsTrue(h.RetrySave(),h.Error);Assert.AreEqual(gold+80,h.State.Profile.Gold);Assert.IsTrue(h.AwaitingR06);Assert.IsTrue(h.Reload());Assert.AreEqual(gold+80,h.State.Profile.Gold);
    }
    [Test]public void ApprovedEActorsHaveExactMappingFramesSpeedAndExistingShadows()
    {
        var art=new AdamR05Art();Assert.AreEqual(3,art.Index.assets.Length);var ids=new[]{"e1","e2","e3"};var clips=new[]{"enm_e1-greed-maw_move_32","enm_e2-lingering-hand_move_32","enm_e3-smoke-chaser_move_32"};var fps=new[]{8,6,12};for(var i=0;i<3;i++){Assert.IsTrue(AdamRoundPresentation.R05Enemy(ids[i]));Assert.IsFalse(AdamRoundPresentation.R04Enemy(ids[i]));Assert.AreEqual(clips[i],AdamArtWorld.EnemyClip(ids[i]));Assert.AreEqual(4,art.Clip(clips[i]).frames);Assert.AreEqual(fps[i],art.Clip(clips[i]).fps);Assert.AreEqual(i==1?"fx_shadow-wide_static_24x6":"fx_shadow-small_static_12x4",AdamRoundPresentation.Shadow(ids[i]));}Assert.Throws<InvalidOperationException>(()=>AdamR05Art.EnemyClip("d3"));var definition=AdamCatalog.Load().ForRound(5);Assert.AreEqual(690,definition.Waves.Sum(x=>x.Groups.Sum(g=>g.Count)));Assert.AreEqual(1900,definition.RawExperience);
    }
}
