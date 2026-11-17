using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Serialization.Json;
using BibleSurvivor.Core;
using BibleSurvivor.Unity;
using NUnit.Framework;
using UnityEngine;

public sealed class AdamR10HostTests
{
    string dir,path;
    [SetUp]public void Setup(){dir=Path.Combine(Path.GetTempPath(),"adam-r10-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(dir);path=Path.Combine(dir,"save.json");}
    [TearDown]public void Cleanup(){Directory.Delete(dir,true);}
    static byte[] Bytes(object value){using(var m=new MemoryStream()){new DataContractJsonSerializer(value.GetType()).WriteObject(m,value);return m.ToArray();}}
    static object Field(AdamHost h,string name)=>typeof(AdamHost).GetField(name,BindingFlags.Instance|BindingFlags.NonPublic).GetValue(h);
    public static AdamHost CompletedGenesis(string path)
    {
        // Technical earlier-round progression/boss damage fixture; never a natural playthrough claim.
        var h=AdamR09HostTests.Intro(path);h.SetPaused(false);for(var i=0;i<20;i++)h.Tick(.1);
        h.Session.Hit(h.Session.BossRules.BossActorId,h.Session.BossRules.MaxHealth);Assert.IsTrue(h.Checkpoint(),h.Error);
        if(h.Session.Stage=="boss_reward")Assert.IsTrue(h.ChooseBossReward(h.Session.Boss.Reward.RewardId,h.Session.BossChoices.First().Id));
        while(h.Session.PendingChoices.Count>0)Assert.IsTrue(h.Choose(h.Session.PendingChoices[0]));
        Assert.IsTrue(h.CompleteBossSettlement(),h.Error);Assert.IsTrue(h.Reload(),h.Error);Assert.IsTrue(h.Session.ChapterComplete);return h;
    }
    [Test]public void CompletedGenesisLoadsWithoutWritingOrEnteringAndInstallationIsRequired()
    {
        var h=CompletedGenesis(path);var disk=File.ReadAllBytes(path);var run=Bytes(h.State.Run);Assert.IsTrue(h.CanEnterExodus);Assert.IsFalse(h.ContentComplete);Assert.IsTrue(h.CanStartNew);h.Tick(60);Assert.IsTrue(h.Reload());CollectionAssert.AreEqual(disk,File.ReadAllBytes(path));CollectionAssert.AreEqual(run,Bytes(h.State.Run));Assert.AreEqual(9,h.Session.RoundNumber);Assert.IsNull(h.World);
        var genesisOnly=new AdamCatalog(Resources.Load<TextAsset>("genesis-catalog").text);h=new AdamHost(genesisOnly,new SaveStore(path));Assert.IsNull(h.Error);Assert.IsFalse(h.CanEnterExodus);Assert.IsFalse(h.TryEnterExodus());CollectionAssert.AreEqual(disk,File.ReadAllBytes(path));
    }
    [TestCase(false)][TestCase(true)]
    public void EntryFailureKeepsOldMemoryAndFixedCandidateUntilRetryOrExplicitReload(bool uncertainPublish)
    {
        var h=CompletedGenesis(path);var before=File.ReadAllBytes(path);var callbacks=0;var fail=true;
        var store=new SaveStore(path,()=>
        {
            callbacks++;if(!fail)return;
            // Own SaveStore staged bytes are published then acknowledgement is lost, exercising real operation replay.
            if(uncertainPublish)File.Replace(Directory.GetFiles(Path.GetDirectoryName(path),Path.GetFileName(path)+".tmp-*").Single(),path,path+".bak");
            throw new IOException("R10 entry replacement/ack failure");
        });
        h=new AdamHost(AdamCatalog.Load(),store);var oldState=h.State;var oldSession=h.Session;var oldRun=Bytes(h.State.Run);var oldRevision=h.State.Revision;
        Assert.IsFalse(h.TryEnterExodus());Assert.NotNull(h.Error);Assert.IsTrue(h.HasSaveRetry);Assert.IsTrue(h.Paused);Assert.AreSame(oldState,h.State);Assert.AreSame(oldSession,h.Session);Assert.IsNull(h.World);Assert.AreEqual(oldRevision,h.State.Revision);Assert.IsFalse(h.CanEnterExodus);
        var operation=(string)Field(h,"operation");var candidate=Bytes(Field(h,"exodusCandidate"));h.Tick(60);h.Start("bible");Assert.IsFalse(h.TryEnterExodus());Assert.IsFalse(h.Checkpoint());CollectionAssert.AreEqual(oldRun,Bytes(h.State.Run));CollectionAssert.AreEqual(candidate,Bytes(Field(h,"exodusCandidate")));Assert.AreEqual(operation,Field(h,"operation"));
        if(!uncertainPublish)CollectionAssert.AreEqual(before,File.ReadAllBytes(path));else Assert.AreEqual(10,AdamSession.Resume(store.Load(),AdamCatalog.Load().ForRound(10)).RoundNumber);
        fail=false;Assert.IsTrue(h.RetrySave(),h.Error);Assert.AreEqual(10,h.Session.RoundNumber);Assert.AreEqual(oldRevision+1,h.State.Revision);Assert.AreEqual(operation,h.State.LastOperationId);Assert.AreEqual(uncertainPublish?1:2,callbacks);Assert.IsTrue(h.Paused);Assert.IsFalse(h.HasSaveRetry);Assert.IsFalse(h.TryEnterExodus());Assert.IsTrue(h.Reload());Assert.AreEqual(10,h.Session.RoundNumber);
    }
    [Test]public void ExplicitReloadCancelsOnlyUncommittedEntryAndReconcilesPublishedEntryWithoutRollback()
    {
        var h=CompletedGenesis(path);var old=File.ReadAllBytes(path);h=new AdamHost(AdamCatalog.Load(),new SaveStore(path,()=>{throw new IOException("cancel before commit");}));Assert.IsFalse(h.TryEnterExodus());Assert.IsTrue(h.Reload());Assert.IsTrue(h.CanEnterExodus);Assert.IsTrue(h.Session.ChapterComplete);Assert.IsFalse(h.HasSaveRetry);CollectionAssert.AreEqual(old,File.ReadAllBytes(path));
        h=new AdamHost(AdamCatalog.Load(),new SaveStore(path,()=>{File.Replace(Directory.GetFiles(dir,"save.json.tmp-*").Single(),path,path+".bak");throw new IOException("ack lost");}));Assert.IsFalse(h.TryEnterExodus());Assert.IsTrue(h.Session.ChapterComplete);var committed=File.ReadAllBytes(path);Assert.IsTrue(h.Reload());Assert.AreEqual(10,h.Session.RoundNumber);Assert.IsTrue(h.Paused);Assert.IsFalse(h.CanEnterExodus);CollectionAssert.AreEqual(committed,File.ReadAllBytes(path));
    }
    [Test]public void SuccessfulEntryPreservesThreeReceiptsProfileGrowthAndResetsWorldOnly()
    {
        var h=CompletedGenesis(path);var first=Bytes(h.State.Run.CompletedBoss);var second=Bytes(h.State.Run.CompletedSecondBoss);var third=Bytes(h.State.Run.Boss);var profile=Bytes(h.State.Profile);var forms=Bytes(h.State.Run.Forms);var equipment=Bytes(h.State.Run.Weapons);var passives=Bytes(h.State.Run.Passives);var permanent=Bytes(h.State.Run.PermanentSnapshot);var id=h.State.Run.RunId;var level=h.State.Run.Level;var xp=h.State.Run.Experience;var remainder=h.State.Run.KillRemainder;var hp=h.Session.CurrentHealth;var rev=h.State.Revision;
        Assert.IsTrue(h.TryEnterExodus(),h.Error);Assert.IsFalse(h.Paused);Assert.IsTrue(h.CanPlay);Assert.AreEqual(rev+1,h.State.Revision);Assert.AreEqual(3,h.State.SchemaVersion);Assert.AreEqual(10,h.Session.RoundNumber);Assert.IsFalse(h.Session.ChapterComplete);Assert.IsFalse(h.ContentComplete);Assert.IsFalse(h.CanStartNew);Assert.IsFalse(h.TryEnterExodus());Assert.AreEqual(id,h.State.Run.RunId);
        CollectionAssert.AreEqual(first,Bytes(h.State.Run.CompletedBoss));CollectionAssert.AreEqual(second,Bytes(h.State.Run.CompletedSecondBoss));CollectionAssert.AreEqual(third,Bytes(h.State.Run.CompletedThirdBoss));Assert.IsNull(h.State.Run.Boss);CollectionAssert.AreEqual(profile,Bytes(h.State.Profile));CollectionAssert.AreEqual(forms,Bytes(h.State.Run.Forms));CollectionAssert.AreEqual(equipment,Bytes(h.State.Run.Weapons));CollectionAssert.AreEqual(passives,Bytes(h.State.Run.Passives));CollectionAssert.AreEqual(permanent,Bytes(h.State.Run.PermanentSnapshot));Assert.AreEqual(level,h.State.Run.Level);Assert.AreEqual(xp,h.State.Run.Experience);Assert.AreEqual(remainder,h.State.Run.KillRemainder);Assert.AreEqual(hp,h.Session.CurrentHealth);Assert.AreEqual(-1,h.State.Run.LastHitTime);Assert.Zero(h.World.SimulationTime);Assert.Zero(h.World.ChargeStates.Count);Assert.Zero(h.World.HostileProjectiles.Count);Assert.Zero(h.World.Capture().Projectiles.Count);
        h.Tick(.1);h.SetPaused(true);var world=Bytes(h.World.Capture());Assert.IsTrue(h.Reload(),h.Error);CollectionAssert.AreEqual(world,Bytes(h.World.Capture()));Assert.IsTrue(h.Paused);Assert.AreEqual(10,h.Session.RoundNumber);
    }
    [Test]public void R10SettlementRetryPays180OncePreservesNullUnlockAndWaitsAt11ThenDeathRestartsR01()
    {
        var h=CompletedGenesis(path);Assert.IsTrue(h.TryEnterExodus());h.Tick(.1);var first=Bytes(h.State.Run.CompletedBoss);var second=Bytes(h.State.Run.CompletedSecondBoss);var third=Bytes(h.State.Run.CompletedThirdBoss);var gold=h.State.Profile.Gold;var unlocks=h.State.Profile.UnlockedWeapons.ToArray();Assert.IsNull(AdamCatalog.Load().ForRound(10).UnlockWeaponId);Assert.IsTrue(h.Checkpoint());
        var fail=true;h=new AdamHost(AdamCatalog.Load(),new SaveStore(path,()=>{if(fail)throw new IOException("completion");}));h.SetPaused(false);h.Session.Advance(180);Assert.IsFalse(h.Checkpoint());var frozen=Bytes(h.State);h.Tick(60);Assert.IsFalse(h.CompleteNormalSettlement());CollectionAssert.AreEqual(frozen,Bytes(h.State));fail=false;Assert.IsTrue(h.RetrySave());Assert.IsTrue(h.Reload());Assert.IsFalse(h.ContentComplete);Assert.IsTrue(h.AwaitingR11);Assert.IsFalse(h.Session.ChapterComplete);Assert.AreEqual(11,h.State.Run.RoundNumber);Assert.IsFalse(h.CanEnterExodus);Assert.IsFalse(h.TryEnterExodus());Assert.IsTrue(h.AwaitingNextRound);Assert.AreEqual(gold+180,h.State.Profile.Gold);Assert.AreEqual(h.State.Run.MaxHealth,h.Session.CurrentHealth);CollectionAssert.AreEqual(unlocks,h.State.Profile.UnlockedWeapons);CollectionAssert.AreEqual(first,Bytes(h.State.Run.CompletedBoss));CollectionAssert.AreEqual(second,Bytes(h.State.Run.CompletedSecondBoss));CollectionAssert.AreEqual(third,Bytes(h.State.Run.CompletedThirdBoss));Assert.IsFalse(h.CompleteNormalSettlement());Assert.Throws<InvalidDataException>(()=>AdamCatalog.Load().ForRound(15));
        var profile=Bytes(h.State.Profile);h.Start("bible");Assert.AreEqual(10,h.Session.RoundNumber);Assert.IsTrue(h.AwaitingR11);h.SetPaused(false);Assert.AreEqual(11,h.Session.RoundNumber);CollectionAssert.AreEqual(profile,Bytes(h.State.Profile));
        path=Path.Combine(dir,"death.json");h=CompletedGenesis(path);Assert.IsTrue(h.TryEnterExodus());h.Session.Damage(100000);Assert.IsTrue(h.Checkpoint());Assert.IsNull(h.State.Run);Assert.IsTrue(h.CanStartNew);profile=Bytes(h.State.Profile);h.Start("sling");Assert.AreEqual(1,h.Session.RoundNumber);Assert.AreEqual(2,h.State.SchemaVersion);CollectionAssert.AreEqual(profile,Bytes(h.State.Profile));
    }
    [Test]public void R10Gold25FullCandidateLoadsUnpaidThenRetriesOnce()
    {
        var h=CompletedGenesis(path);Assert.IsTrue(h.TryEnterExodus());AdamR08HostTests.MaxEquipment(h);
        for(var i=0;i<170 && h.Session.PendingChoices.Count==0;i++)
        {h.Session.Advance(1);foreach(var id in h.Session.Targets.Keys.ToArray()){h.Session.Kill(id);h.Session.Collect(id);if(h.Session.PendingChoices.Count>0)break;}}
        Assert.AreEqual(1,h.Session.PendingChoices.Count);var choice=h.Session.PendingChoices.Single();Assert.IsTrue(choice.StartsWith(AdamSession.GrowthGoldChoicePrefix,StringComparison.Ordinal));
        h.Session.Capture();h.State.Run.World=new BattleWorld(h.Session,AdamCatalog.Load().SpeedsFor(10),7).Capture();new SaveStore(path).Commit(h.State,h.State.Revision,"R10-real-XP-gold25-fixture");Assert.IsTrue(h.Reload());var gold=h.State.Profile.Gold;var pending=h.State.Run.PendingLevels;Assert.Greater(pending,0);Assert.AreEqual(choice,h.Session.PendingChoices.Single());Assert.IsTrue(h.Reload());Assert.AreEqual(gold,h.State.Profile.Gold);
        var fail=true;h=new AdamHost(AdamCatalog.Load(),new SaveStore(path,()=>{if(fail)throw new IOException("gold25");}));Assert.IsTrue(h.Choose(choice));Assert.NotNull(h.Error);Assert.AreEqual(gold+25,h.State.Profile.Gold);var frozen=Bytes(h.State);h.Tick(20);Assert.IsFalse(h.Choose(choice));CollectionAssert.AreEqual(frozen,Bytes(h.State));fail=false;Assert.IsTrue(h.RetrySave());Assert.IsTrue(h.Reload());Assert.AreEqual(gold+25,h.State.Profile.Gold);Assert.AreEqual(pending-1,h.State.Run.PendingLevels);Assert.IsFalse(h.Choose(choice));
    }
    [TestCase(true)][TestCase(false)]public void SharedGameHandlersConsumeOnlyOneEntryOrNewChallengePerFrame(bool entryFirst)
    {
        var h=CompletedGenesis(path);var old=h.State.Run.RunId;var profile=Bytes(h.State.Profile);var go=new GameObject("R10 handlers");
        try
        {
            var game=go.AddComponent<AdamGame>();game.AutoInitialize=false;game.enabled=false;game.Initialize(h);game.SetLifecycle(true,false);
            if(entryFirst){game.EnterExodus();game.EnterExodus();game.SelectSlot(0);Assert.AreEqual(10,h.Session.RoundNumber);Assert.AreEqual(old,h.State.Run.RunId);}
            else{game.SelectSlot(0);game.EnterExodus();Assert.AreEqual(1,h.Session.RoundNumber);Assert.AreNotEqual(old,h.State.Run.RunId);}
            CollectionAssert.AreEqual(profile,Bytes(h.State.Profile));Assert.IsFalse(h.HasSaveRetry);
        }
        finally{UnityEngine.Object.DestroyImmediate(go);}
    }
    [Test]public void InstalledCatalogUsesIndependentExodusSpeedSetAndRejectsTunedOrUnknownContent()
    {
        var c=AdamCatalog.Load();Assert.IsTrue(c.ExodusInstalled);Assert.AreEqual(900,c.ForRound(10).Waves.Sum(x=>x.Groups.Sum(g=>g.Count)));Assert.AreEqual(3600,c.ForRound(10).RawExperience);Assert.AreEqual(3,c.SpeedsFor(10).Count);Assert.IsFalse(c.Speeds.ContainsKey("ex1"));Assert.AreEqual(.45,c.SpeedsFor(10)["ex1"]);Assert.AreEqual(10,c.ForCheckpoint(new RunState{Stage="between_rounds",RoundNumber=11}).Number);
        var genesis=Resources.Load<TextAsset>("genesis-catalog").text;var data=JsonUtility.FromJson<AdamCatalog.Root>(Resources.Load<TextAsset>("exodus-catalog").text);data.monsters[0].speed_ratio=.99;Assert.Throws<InvalidDataException>(()=>new AdamCatalog(genesis,JsonUtility.ToJson(data)));
    }
}
