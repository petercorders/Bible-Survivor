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
    public void LeviticusKeepsTheRunAcrossReloadsAndCompletesAtR21()
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
        for(var round=16;round<=21;round++)
        {
            Assert.AreEqual(round,host.Session.RoundNumber);
            Assert.AreEqual(runId,host.State.Run.RunId);
            Assert.IsFalse(host.CanEnterLeviticus);
            Assert.IsFalse(host.CanStartNew);
            if(round>=19)CollectionAssert.AreEqual(sixthBoss,Bytes(host.State.Run.CompletedSixthBoss));
            var definition=catalog.ForRound(round);
            Assert.AreEqual(LeviticusContent.Round(round).Fingerprint,definition.Fingerprint);
            CollectionAssert.AreEquivalent(new[]{"lv1","lv2","lv3"},catalog.SpeedsFor(round).Keys);
            oldGold=CompleteRound(host,round==18?"ash_veil":round==21?"oath_wraith":null);
            if(round==18)sixthBoss=Bytes(host.State.Run.Boss);
            var committed=File.ReadAllBytes(path);
            host=new AdamHost(catalog,store);
            Assert.IsNull(host.Error);
            Assert.IsTrue(host.Paused);
            Assert.AreEqual(round,host.Session.RoundNumber);
            Assert.AreEqual(round+1,host.State.Run.RoundNumber);
            Assert.AreEqual(oldGold+definition.CompletionGold,host.State.Profile.Gold);
            Assert.IsFalse(host.ContentComplete);
            Assert.AreEqual(round==21,host.LeviticusComplete);
            Assert.AreEqual(round==21,host.CanEnterNumbers);
            Assert.AreEqual(round<21,host.AwaitingNextRound);
            CollectionAssert.AreEqual(committed,File.ReadAllBytes(path));
            if(round<21)host.SetPaused(false);
        }
        var revision=host.State.Revision;
        host.SetPaused(false);host.Tick(.1);
        Assert.AreEqual(21,host.Session.RoundNumber);
        Assert.AreEqual(revision,host.State.Revision);
        Assert.IsTrue(host.CanStartNew);
        Assert.IsFalse(host.CanPlay);
        Assert.IsTrue(host.CanEnterNumbers);
    }

    [TestCase(false)] [TestCase(true)]
    public void NumbersEntryRetryKeepsR21PublishedUntilR22Commits(bool replaceBeforeFailure)
    {
        var completed=AdamR15UiTests.CompletedFifteen(path);
        Assert.IsTrue(completed.TryEnterLeviticus(),completed.Error);
        for(var round=16;round<=21;round++)
        {
            Assert.AreEqual(round,completed.Session.RoundNumber);
            CompleteRound(completed,round==18?"ash_veil":round==21?"oath_wraith":null);
            if(round<21)completed.SetPaused(false);
        }

        var genesis=Resources.Load<TextAsset>("genesis-catalog").text;
        var exodus=Resources.Load<TextAsset>("exodus-catalog").text;
        var partialCatalog=new AdamCatalog(genesis,exodus,installLeviticus:true);
        var partial=new AdamHost(partialCatalog,new SaveStore(path));
        Assert.IsNull(partial.Error);
        Assert.IsTrue(partial.LeviticusComplete);
        Assert.IsFalse(partial.CanEnterNumbers);
        Assert.IsFalse(partial.TryEnterNumbers());

        var callbacks=0;
        var store=new SaveStore(path,()=>
        {
            if(++callbacks!=1)return;
            if(replaceBeforeFailure)
                File.Replace(Directory.GetFiles(directory,"save.json.tmp-*").Single(),path,path+".bak");
            throw new IOException("Numbers entry write or acknowledgement failure");
        });
        var host=new AdamHost(AdamCatalog.Load(),store);
        var published=host.State;
        var publishedBytes=Bytes(published);
        Assert.IsTrue(host.CanEnterNumbers);
        Assert.IsFalse(host.TryEnterNumbers());
        Assert.IsTrue(host.HasNumbersEntryRetry);
        Assert.IsTrue(host.Paused);
        Assert.IsTrue(host.LeviticusComplete);
        Assert.IsFalse(host.ContentComplete);
        Assert.IsFalse(host.TryEnterNumbers());
        host.Start("sling");host.Tick(.1);
        Assert.AreSame(published,host.State);
        CollectionAssert.AreEqual(publishedBytes,Bytes(host.State));
        var afterFailure=store.Load();
        Assert.AreEqual(published.Revision+(replaceBeforeFailure?1:0),afterFailure.Revision);

        Assert.IsTrue(host.RetrySave(),host.Error);
        Assert.AreEqual(replaceBeforeFailure?1:2,callbacks);
        Assert.IsFalse(host.HasNumbersEntryRetry);
        Assert.IsTrue(host.Paused);
        Assert.AreEqual(22,host.Session.RoundNumber);
        Assert.AreEqual(published.Revision+1,host.State.Revision);
        if(replaceBeforeFailure)Assert.AreEqual(afterFailure.LastOperationId,host.State.LastOperationId);
        Assert.AreEqual(published.Run.RunId,host.State.Run.RunId);
        CollectionAssert.AreEqual(Bytes(published.Profile),Bytes(host.State.Profile));
        CollectionAssert.AreEquivalent(published.Run.Weapons,host.State.Run.Weapons);
        CollectionAssert.AreEquivalent(published.Run.Passives,host.State.Run.Passives);
        Assert.Zero(host.World.SimulationTime);
        Assert.IsFalse(partial.Reload());
        Assert.IsTrue(partial.RequiresNewerGame);
    }

    internal static long CompleteRound(AdamHost host,string bossId)
    {
        var gold=host.State.Profile.Gold;
        host.Session.Advance(180);
        if(bossId!=null)
        {
            host.Session.Advance(2);
            Assert.AreEqual(bossId,host.Session.BossRules.Id);
            host.Session.Hit(host.Session.BossRules.BossActorId,host.Session.BossRules.MaxHealth);
            Assert.IsTrue(host.Checkpoint(),host.Error);
            if(host.Session.Stage=="boss_reward")
                Assert.IsTrue(host.ChooseBossReward(host.Session.Boss.Reward.RewardId,host.Session.BossChoices[0].Id));
            while(host.Session.PendingChoices.Count>0)Assert.IsTrue(host.Choose(host.Session.PendingChoices[0]));
            gold=host.State.Profile.Gold;
            Assert.IsTrue(host.CompleteBossSettlement(),host.Error);
            Assert.IsFalse(host.CompleteBossSettlement());
        }
        else Assert.IsTrue(host.Checkpoint(),host.Error);
        return gold;
    }

    [TestCase(16,"아론")] [TestCase(17,"정결의 길")] [TestCase(18,"속죄일")]
    [TestCase(19,"이웃 돌봄")] [TestCase(20,"희년")] [TestCase(21,"서원의 끝")]
    public void LeviticusTitlesUseBookLocalRoundAndShowInstalledRange(int round,string name)
    {
        Assert.AreEqual($"레위기 {round-15} / {name} · 3분 시제품",AdamRoundPresentation.Title(round));
        Assert.AreEqual("레위기 완료 · 진행 저장됨",AdamRoundPresentation.CompletionHeading(false,false,true));
        StringAssert.Contains("레위기 1–6 라운드",AdamRoundPresentation.CompletionDetail(false,false,true,true));
        StringAssert.Contains("민수기는 현재 성장·장비로 계속",AdamRoundPresentation.CompletionDetail(false,false,true,true,true));
        StringAssert.Contains("성장·장비로 계속",AdamRoundPresentation.CompletionDetail(false,true,true));
    }

    [TestCase(22,"인구 조사")] [TestCase(23,"광야 행진")] [TestCase(24,"불평의 진영")]
    [TestCase(25,"정탐꾼")] [TestCase(26,"긴 광야")] [TestCase(27,"바위의 물")]
    [TestCase(28,"놋뱀")] [TestCase(29,"모압 평야")] [TestCase(30,"요단 앞")]
    public void NumbersTitlesUseBookLocalRound(int round,string name)
    {
        Assert.AreEqual($"민수기 {round-21} / {name} · 3분 시제품",AdamRoundPresentation.Title(round));
        Assert.AreEqual("민수기 완료 · 진행 저장됨",AdamRoundPresentation.CompletionHeading(false,false,numbersComplete:true));
        StringAssert.Contains("민수기 1–9 라운드",AdamRoundPresentation.CompletionDetail(false,false,numbersComplete:true));
        StringAssert.Contains("신명기는 현재 성장·장비로 계속",AdamRoundPresentation.CompletionDetail(false,false,numbersComplete:true,deuteronomyInstalled:true));
    }

    [TestCase(21,"서원의 망령")] [TestCase(24,"먼지의 폭군")]
    [TestCase(27,"협곡의 추적자")] [TestCase(30,"광야의 수문장")]
    public void NewBossHudShowsProjectileWarning(int round,string name)
    {
        var rules=BossDefinition.ForRound(round);
        Assert.AreEqual(name,AdamBossPresentation.Name(rules));
        Assert.IsTrue(AdamBossPresentation.UsesTwoLineHud(rules));
        StringAssert.Contains("파편 3방향 예고",AdamBossPresentation.Phase("boss",new BossEncounterState{Telegraphing=true},rules));
    }

    static byte[] Bytes(object value)
    {
        using var stream=new MemoryStream();
        new DataContractJsonSerializer(value.GetType()).WriteObject(stream,value);
        return stream.ToArray();
    }
}
