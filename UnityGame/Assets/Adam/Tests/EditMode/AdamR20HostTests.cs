using System;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Json;
using BibleSurvivor.Core;
using BibleSurvivor.Unity;
using NUnit.Framework;
using UnityEngine;

public sealed class AdamR20HostTests
{
    string directory, path;

    [SetUp] public void Setup()
    {
        directory=Path.Combine(Path.GetTempPath(),"adam-r20-host-"+Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        path=Path.Combine(directory,"save.json");
    }

    [TearDown] public void Cleanup()=>Directory.Delete(directory,true);

    [TestCase(false,false)] [TestCase(true,false)] [TestCase(false,true)] [TestCase(true,true)]
    public void FailedLeviticusEntryKeepsR15UntilRetryOrLoad(bool replaceBeforeFailure,bool reload)
    {
        AdamR15UiTests.CompletedFifteen(path);
        var oldFile=File.ReadAllBytes(path);
        var callbacks=0;
        var fail=true;
        var store=new SaveStore(path,()=>
        {
            callbacks++;
            if(!fail)return;
            if(replaceBeforeFailure)
                File.Replace(Directory.GetFiles(directory,"save.json.tmp-*").Single(),path,path+".bak");
            throw new IOException("Leviticus entry write or acknowledgement failure");
        });
        var host=new AdamHost(AdamCatalog.Load(),store);
        var previous=host.State;
        var previousSession=host.Session;
        var oldRun=Bytes(previous.Run);
        var previousRevision=previous.Revision;
        Assert.IsTrue(host.CanEnterLeviticus);
        Assert.IsFalse(host.TryEnterLeviticus());
        Assert.IsTrue(host.HasLeviticusEntryRetry);
        Assert.IsTrue(host.ExodusComplete);
        Assert.IsFalse(host.ContentComplete);
        Assert.IsTrue(host.Paused);
        Assert.AreSame(previous,host.State);
        Assert.AreSame(previousSession,host.Session);
        Assert.IsFalse(host.TryEnterLeviticus());
        host.Start("sling");host.Tick(5);
        CollectionAssert.AreEqual(oldRun,Bytes(host.State.Run));

        var committed=store.Load();
        Assert.AreEqual(previousRevision+(replaceBeforeFailure?1:0),committed.Revision);
        fail=false;
        Assert.IsTrue(reload?host.Reload():host.RetrySave(),host.Error);
        Assert.IsFalse(host.HasSaveRetry);
        Assert.IsTrue(host.Paused);
        if(reload&&!replaceBeforeFailure)
        {
            CollectionAssert.AreEqual(oldFile,File.ReadAllBytes(path));
            Assert.IsTrue(host.CanEnterLeviticus);
            Assert.AreEqual(15,host.Session.RoundNumber);
            return;
        }
        Assert.AreEqual(16,host.Session.RoundNumber);
        Assert.AreEqual(previousRevision+1,host.State.Revision);
        Assert.AreEqual(replaceBeforeFailure||reload?1:2,callbacks);
        if(replaceBeforeFailure)Assert.AreEqual(committed.LastOperationId,host.State.LastOperationId);
        Assert.AreEqual(previous.Run.RunId,host.State.Run.RunId);
        CollectionAssert.AreEqual(Bytes(previous.Run.Boss),Bytes(host.State.Run.CompletedFifthBoss));
        CollectionAssert.AreEqual(Bytes(previous.Profile),Bytes(host.State.Profile));
        Assert.Zero(host.World.SimulationTime);
    }

    [Test]
    public void ChaptersAndInstalledEndUseDistinctCheckpointsAcrossProcessReloads()
    {
        var catalog=AdamCatalog.Load();
        var store=new SaveStore(path);
        var host=AdamR15UiTests.CompletedFifteen(path);
        var runId=host.State.Run.RunId;
        var oldGold=host.State.Profile.Gold;
        host.SetPaused(false);host.Tick(.1);
        Assert.AreEqual(15,host.Session.RoundNumber);
        Assert.IsTrue(host.TryEnterLeviticus(),host.Error);
        Assert.AreEqual(oldGold,host.State.Profile.Gold);
        byte[] sixthBoss=null;
        for(var round=16;round<=20;round++)
        {
            Assert.AreEqual(round,host.Session.RoundNumber);
            Assert.AreEqual(runId,host.State.Run.RunId);
            Assert.IsFalse(host.CanEnterLeviticus);
            Assert.IsFalse(host.CanStartNew);
            if(round>=19)CollectionAssert.AreEqual(sixthBoss,Bytes(host.State.Run.CompletedSixthBoss));
            var definition=catalog.ForRound(round);
            Assert.AreEqual(LeviticusContent.Round(round).Fingerprint,definition.Fingerprint);
            CollectionAssert.AreEquivalent(new[]{"lv1","lv2","lv3"},catalog.SpeedsFor(round).Keys);
            oldGold=host.State.Profile.Gold;
            host.Session.Advance(180);
            if(round==18)
            {
                host.Session.Advance(2);
                Assert.AreEqual("ash_veil",host.Session.BossRules.Id);
                host.Session.Hit(host.Session.BossRules.BossActorId,host.Session.BossRules.MaxHealth);
                Assert.IsTrue(host.Checkpoint(),host.Error);
                if(host.Session.Stage=="boss_reward")
                    Assert.IsTrue(host.ChooseBossReward(host.Session.Boss.Reward.RewardId,host.Session.BossChoices[0].Id));
                while(host.Session.PendingChoices.Count>0)Assert.IsTrue(host.Choose(host.Session.PendingChoices[0]));
                oldGold=host.State.Profile.Gold;
                Assert.IsTrue(host.CompleteBossSettlement(),host.Error);
                Assert.IsFalse(host.CompleteBossSettlement());
                sixthBoss=Bytes(host.State.Run.Boss);
            }
            Assert.IsTrue(host.Checkpoint(),host.Error);
            var committed=File.ReadAllBytes(path);
            host=new AdamHost(catalog,store);
            Assert.IsNull(host.Error);
            Assert.IsTrue(host.Paused);
            Assert.AreEqual(round,host.Session.RoundNumber);
            Assert.AreEqual(round+1,host.State.Run.RoundNumber);
            Assert.AreEqual(oldGold+definition.CompletionGold,host.State.Profile.Gold);
            Assert.AreEqual(round==20,host.ContentComplete);
            Assert.AreEqual(round<20,host.AwaitingNextRound);
            CollectionAssert.AreEqual(committed,File.ReadAllBytes(path));
            if(round<20)host.SetPaused(false);
        }
        var revision=host.State.Revision;
        host.SetPaused(false);host.Tick(.1);
        Assert.AreEqual(20,host.Session.RoundNumber);
        Assert.AreEqual(revision,host.State.Revision);
        Assert.IsTrue(host.CanStartNew);
        Assert.IsFalse(host.CanPlay);
        Assert.Throws<InvalidDataException>(()=>catalog.ForRound(21));
    }

    [TestCase(16,"아론")] [TestCase(17,"정결의 길")] [TestCase(18,"속죄일")]
    [TestCase(19,"이웃 돌봄")] [TestCase(20,"희년")]
    public void LeviticusTitlesUseBookLocalRoundAndShowInstalledRange(int round,string name)
    {
        Assert.AreEqual($"레위기 {round-15} / {name} · 3분 시제품",AdamRoundPresentation.Title(round));
        Assert.AreEqual("희년 완료 · 진행 저장됨",AdamRoundPresentation.CompletionHeading(false,false,true));
        StringAssert.Contains("레위기 1–5 라운드",AdamRoundPresentation.CompletionDetail(false,false,true,true));
        StringAssert.Contains("성장·장비로 계속",AdamRoundPresentation.CompletionDetail(false,true,true));
    }

    static byte[] Bytes(object value)
    {
        using var stream=new MemoryStream();
        new DataContractJsonSerializer(value.GetType()).WriteObject(stream,value);
        return stream.ToArray();
    }
}
