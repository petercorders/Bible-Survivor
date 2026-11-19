using System;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Json;
using BibleSurvivor.Core;
using BibleSurvivor.Unity;
using NUnit.Framework;
public sealed class AdamR08HostTests
{
    string dir,path;
    [SetUp]public void Setup(){dir=Path.Combine(Path.GetTempPath(),"adam-r08-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(dir);path=Path.Combine(dir,"save.json");}
    [TearDown]public void Cleanup(){Directory.Delete(dir,true);}
    static byte[] Bytes(object value){using(var m=new MemoryStream()){new DataContractJsonSerializer(value.GetType()).WriteObject(m,value);return m.ToArray();}}
    public static AdamHost Start(string path)
    {
        var h=AdamR07HostTests.Start(path);h.Session.Advance(180);Assert.IsTrue(h.Checkpoint(),h.Error);Assert.IsTrue(h.AwaitingR08);h.SetPaused(false);Assert.AreEqual(8,h.Session.RoundNumber);return h;
    }
    public static void MaxEquipment(AdamHost h)
    {
        foreach(var id in new[]{"bible","jawbone","sling"})while(h.Session.Inventory.TryChoose(id,false)){}
        foreach(var id in new[]{"scroll","hair","stones","east_wind","seal","cloud","thunder","crown","underwear","whirlwind"})while(h.Session.Inventory.TryChoose(id,true)){}
    }
    static void CurrencyFixture(AdamHost h,string path)
    {
        MaxEquipment(h);for(var i=0;i<170&&h.Session.PendingChoices.Count==0;i++){h.Session.Advance(1);foreach(var id in h.Session.Targets.Keys.ToArray()){h.Session.Kill(id);h.Session.Collect(id);if(h.Session.PendingChoices.Count>0)break;}}
        Assert.AreEqual(1,h.Session.PendingChoices.Count);Assert.IsTrue(h.Session.PendingChoices[0].StartsWith(AdamSession.GrowthGoldChoicePrefix,StringComparison.Ordinal));h.Session.Capture();h.State.Run.World=new BattleWorld(h.Session,AdamCatalog.Load().Speeds,7).Capture();new SaveStore(path).Commit(h.State,h.State.Revision,"Core-generated-exhaustion");Assert.IsTrue(h.Reload(),h.Error);
    }
    [Test]public void Gold25FullSerialIdLoadsUnpaidThenCommitsRetryExactlyOnce()
    {
        var h=Start(path);CurrencyFixture(h,path);var id=h.Session.PendingChoices.Single();Assert.AreEqual(AdamSession.GrowthGoldChoicePrefix+h.State.Run.ChoiceSerial,id);var gold=h.State.Profile.Gold;var level=h.State.Run.Level;var xp=h.State.Run.Experience;Assert.IsTrue(h.Reload());Assert.AreEqual(id,h.Session.PendingChoices.Single());Assert.AreEqual(gold,h.State.Profile.Gold);var fail=true;h=new AdamHost(AdamCatalog.Load(),new SaveStore(path,()=>{if(fail)throw new IOException("gold25 commit");}));Assert.IsTrue(h.Choose(id));Assert.NotNull(h.Error);Assert.AreEqual(gold+25,h.State.Profile.Gold);var frozen=Bytes(h.State);h.Tick(20);Assert.IsFalse(h.Choose(id));CollectionAssert.AreEqual(frozen,Bytes(h.State));fail=false;Assert.IsTrue(h.RetrySave(),h.Error);Assert.IsTrue(h.Reload());Assert.AreEqual(gold+25,h.State.Profile.Gold);Assert.IsFalse(h.Choose(id));Assert.AreEqual(level,h.State.Run.Level);Assert.AreEqual(xp,h.State.Run.Experience);
    }
    [Test]public void CurrencyCardHasRealEffectAndNoFakeWeaponLevelOrBossGold100()
    {
        var h=Start(path);CurrencyFixture(h,path);var id=h.Session.PendingChoices.Single();var card=AdamChoiceText.Describe(id,h.Session.Inventory,h.Session.DamageMultiplier);Assert.AreEqual(id,card.Id);Assert.AreEqual("골드 주머니",card.Name);Assert.AreEqual("재화",card.Kind);Assert.AreEqual("영구 골드 +25",card.Effect);Assert.IsTrue(card.IsCurrency);var text=card.Card(0);Assert.IsFalse(text.Contains("무기"));Assert.IsFalse(text.Contains("강화"));Assert.IsFalse(text.Contains("→"));Assert.IsFalse(text.Contains("/"));Assert.IsFalse(text.Contains("100"));Assert.IsFalse(text.Contains(id));Assert.IsTrue(AdamBossPresentation.Card(new BossRewardCandidate{Kind="gold",Gold=100},0,h.Session).Contains("+100"));
    }
    [Test]public void BothReceiptsEvolutionHistoryAndWorldRestoreThroughR08()
    {
        var h=Start(path);var first=Bytes(h.State.Run.CompletedBoss);var second=Bytes(h.State.Run.CompletedSecondBoss);Assert.AreEqual(7,h.State.Run.PreviousNormalRound);Assert.IsNull(h.Session.Boss);Assert.AreEqual(EvolutionRules.Sling,h.Session.WeaponForm("sling"));Assert.AreEqual(EvolutionRules.Bible,h.Session.WeaponForm("bible"));h.Tick(.1);h.SetPaused(true);var world=Bytes(h.World.Capture());Assert.IsNull(h.Error);Assert.IsTrue(h.Reload());CollectionAssert.AreEqual(world,Bytes(h.World.Capture()));CollectionAssert.AreEqual(first,Bytes(h.State.Run.CompletedBoss));CollectionAssert.AreEqual(second,Bytes(h.State.Run.CompletedSecondBoss));Assert.Zero(h.World.ChargeStates.Count);
    }
    [Test]public void Growth140NullUnlockHealAndCompletionRetryKeepBothReceipts()
    {
        var h=Start(path);var first=Bytes(h.State.Run.CompletedBoss);var second=Bytes(h.State.Run.CompletedSecondBoss);var unlocked=h.State.Profile.UnlockedWeapons.ToArray();Assert.IsNull(AdamCatalog.Load().ForRound(8).UnlockWeaponId);h.Session.Advance(60);h.Session.Damage(20);foreach(var id in h.Session.Targets.Keys.ToArray())h.Session.Kill(id);var gold=h.State.Profile.Gold;h.Session.Advance(180);Assert.IsTrue(h.Checkpoint(),h.Error);Assert.AreEqual("round_settlement",h.Session.Stage);Assert.AreEqual(gold,h.State.Profile.Gold);Assert.IsFalse(h.CompleteNormalSettlement());Assert.IsTrue(h.Reload());while(h.Session.PendingChoices.Count>0)Assert.IsTrue(h.Choose(h.Session.PendingChoices[0]));Assert.IsTrue(h.AwaitingR09);Assert.IsFalse(h.ContentComplete);Assert.AreEqual(gold+140,h.State.Profile.Gold);Assert.AreEqual(9,h.State.Run.RoundNumber);Assert.AreEqual(h.State.Run.MaxHealth,h.Session.CurrentHealth);CollectionAssert.AreEqual(unlocked,h.State.Profile.UnlockedWeapons);Assert.IsFalse(h.CompleteNormalSettlement());Assert.IsTrue(h.Reload());CollectionAssert.AreEqual(first,Bytes(h.State.Run.CompletedBoss));CollectionAssert.AreEqual(second,Bytes(h.State.Run.CompletedSecondBoss));Assert.Throws<InvalidDataException>(()=>AdamCatalog.Load().ForRound(31));
        path=Path.Combine(dir,"retry.json");h=Start(path);Assert.IsTrue(h.Checkpoint());var fail=true;h=new AdamHost(AdamCatalog.Load(),new SaveStore(path,()=>{if(fail)throw new IOException("R08 completion");}));h.SetPaused(false);gold=h.State.Profile.Gold;h.Session.Advance(180);Assert.IsFalse(h.Checkpoint());var frozen=Bytes(h.State);h.Tick(20);Assert.IsFalse(h.CompleteNormalSettlement());CollectionAssert.AreEqual(frozen,Bytes(h.State));fail=false;Assert.IsTrue(h.RetrySave());Assert.IsTrue(h.Reload());Assert.AreEqual(gold+140,h.State.Profile.Gold);Assert.IsTrue(h.AwaitingR09);Assert.IsFalse(h.ContentComplete);
    }
    [Test]public void HMappingUsesThreeReviewedClipsAndUnchangedBossDeathRules()
    {
        var art=new AdamR08Art();Assert.AreEqual(3,art.Index.assets.Length);Assert.AreEqual(12,art.Index.assets.Sum(x=>x.frames));var ids=new[]{"h1","h2","h3"};var fps=new[]{9,5,7};for(var i=0;i<3;i++){Assert.IsTrue(AdamRoundPresentation.R08Enemy(ids[i]));Assert.AreEqual(AdamR08Art.EnemyClip(ids[i]),AdamArtWorld.EnemyClip(ids[i]));Assert.AreEqual(fps[i],art.Clip(AdamR08Art.EnemyClip(ids[i])).fps);Assert.AreEqual(i==0?"fx_shadow-small_static_12x4":"fx_shadow-wide_static_24x6",AdamRoundPresentation.Shadow(ids[i]));}Assert.IsFalse(AdamArtWorld.BossDeathVisible(10,11));Assert.AreEqual("요셉",AdamRoundPresentation.Name(8));Assert.AreEqual(1020,AdamCatalog.Load().ForRound(8).Waves.Sum(x=>x.Groups.Sum(g=>g.Count)));Assert.AreEqual(3500,AdamCatalog.Load().ForRound(8).RawExperience);
    }
}
