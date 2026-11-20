using System;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Json;
using BibleSurvivor.Core;
using BibleSurvivor.Unity;
using NUnit.Framework;
using UnityEngine;

public sealed class AdamR50HostTests
{
    string directory, path;

    [SetUp] public void Setup()
    {
        directory=Path.Combine(Path.GetTempPath(),"adam-r50-host-"+Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        path=Path.Combine(directory,"save.json");
    }

    [TearDown] public void Cleanup()=>Directory.Delete(directory,true);

    static string Boss(int round)=>round switch
    {
        33=>"oblivion_mist",36=>"crossroads_sentinel",39=>"rampart_colossus",
        42=>"shifting_boundary",45=>"torrent_serpent",48=>"ford_gatekeeper",_=>null
    };

    static AdamCatalog Partial(bool judges)=>new AdamCatalog(Resources.Load<TextAsset>("genesis-catalog").text,
        Resources.Load<TextAsset>("exodus-catalog").text,installLeviticus:true,installNumbers:true,installDeuteronomy:true,installJoshua:true,installJudges:judges);

    // R30 completion → Deuteronomy → Joshua through the R42 boss, stopped on the Joshua completion screen.
    internal static AdamHost CompletedJoshua(string path)
    {
        var host=AdamR40HostTests.CompletedThirty(path);
        Assert.IsTrue(host.TryEnterDeuteronomy(),host.Error);
        for(var round=31;round<=42;round++)
        {
            if(round==JoshuaContent.GlobalRoundNumber)Assert.IsTrue(host.TryEnterJoshua(),host.Error);
            Assert.AreEqual(round,host.Session.RoundNumber);
            AdamR20HostTests.CompleteRound(host,Boss(round));
            if(round!=36&&round!=42)host.SetPaused(false);
        }
        Assert.IsTrue(host.JoshuaComplete,host.Error);
        return host;
    }

    [TestCase(false)] [TestCase(true)]
    public void JudgesEntryRetryKeepsR42PublishedUntilR43Commits(bool replaceBeforeFailure)
    {
        CompletedJoshua(path);
        var partial=new AdamHost(Partial(false),new SaveStore(path));
        Assert.IsNull(partial.Error);
        Assert.IsTrue(partial.JoshuaComplete);
        Assert.IsTrue(partial.CanStartNew);
        Assert.IsFalse(partial.AwaitingNextRound);
        Assert.IsFalse(partial.CanEnterJudges);
        Assert.IsFalse(partial.TryEnterJudges());
        Assert.AreEqual(0,AdamRoundPresentation.NextChapter(partial));
        Assert.AreEqual(AdamRoundPresentation.JoshuaHeading,AdamRoundPresentation.CompletionHeading(partial));
        StringAssert.Contains("여호수아 1–6 라운드",AdamRoundPresentation.CompletionDetail(partial));
        StringAssert.DoesNotContain("사사기",AdamRoundPresentation.CompletionDetail(partial));

        var callbacks=0;
        var store=new SaveStore(path,()=>
        {
            if(++callbacks!=1)return;
            if(replaceBeforeFailure)
                File.Replace(Directory.GetFiles(directory,"save.json.tmp-*").Single(),path,path+".bak");
            throw new IOException("Judges entry write or acknowledgement failure");
        });
        var host=new AdamHost(AdamCatalog.Load(),store);
        var published=host.State;
        var publishedBytes=Bytes(published);
        Assert.IsTrue(host.JoshuaComplete);
        Assert.IsTrue(host.BookComplete);
        Assert.IsFalse(host.ContentComplete);
        Assert.IsFalse(host.AwaitingNextRound);
        Assert.IsTrue(host.CanEnterJudges);
        Assert.IsFalse(host.CanEnterJoshua);
        Assert.AreEqual(JudgesContent.GlobalRoundNumber,AdamRoundPresentation.NextChapter(host));
        Assert.AreEqual(AdamRoundPresentation.JudgesStartCard,AdamRoundPresentation.StartCard(AdamRoundPresentation.NextChapter(host)));
        Assert.AreEqual(AdamRoundPresentation.JoshuaHeading,AdamRoundPresentation.CompletionHeading(host));
        StringAssert.Contains("사사기는 현재 성장·장비로 계속",AdamRoundPresentation.CompletionDetail(host));
        Assert.IsFalse(host.TryEnterJudges());
        Assert.IsTrue(host.HasJudgesEntryRetry);
        Assert.IsTrue(host.Paused);
        Assert.IsTrue(host.JoshuaComplete);
        Assert.IsFalse(host.TryEnterJudges());
        host.Start("sling");host.Tick(.1);
        Assert.AreSame(published,host.State);
        CollectionAssert.AreEqual(publishedBytes,Bytes(host.State));
        var afterFailure=store.Load();
        Assert.AreEqual(published.Revision+(replaceBeforeFailure?1:0),afterFailure.Revision);

        Assert.IsTrue(host.RetrySave(),host.Error);
        Assert.AreEqual(replaceBeforeFailure?1:2,callbacks);
        Assert.IsFalse(host.HasJudgesEntryRetry);
        Assert.IsTrue(host.Paused);
        Assert.AreEqual(43,host.Session.RoundNumber);
        Assert.AreEqual(published.Revision+1,host.State.Revision);
        if(replaceBeforeFailure)Assert.AreEqual(afterFailure.LastOperationId,host.State.LastOperationId);
        Assert.AreEqual(published.Run.RunId,host.State.Run.RunId);
        CollectionAssert.AreEqual(Bytes(published.Profile),Bytes(host.State.Profile));
        CollectionAssert.AreEqual(Bytes(published.Run.Boss),Bytes(host.State.Run.LaterBossHistory.Last()));
        CollectionAssert.AreEqual(new[]{21,24,27,30,33,36,39,42},host.State.Run.LaterBossHistory.Select(x=>x.EncounterRound).ToArray());
        Assert.Zero(host.World.SimulationTime);
        Assert.IsFalse(partial.Reload());
        Assert.IsTrue(partial.RequiresNewerGame);
    }

    [Test]
    public void JudgesKeepsTheRunAcrossReloadsThroughR50()
    {
        var catalog=AdamCatalog.Load();
        Assert.AreEqual(60,catalog.LastInstalledRound);
        var store=new SaveStore(path);
        var host=CompletedJoshua(path);
        var runId=host.State.Run.RunId;
        var gold=host.State.Profile.Gold;
        Assert.IsTrue(host.TryEnterJudges(),host.Error);
        Assert.AreEqual(gold,host.State.Profile.Gold);
        for(var round=43;round<=50;round++)
        {
            Assert.AreEqual(round,host.Session.RoundNumber);
            Assert.AreEqual(runId,host.State.Run.RunId);
            var definition=catalog.ForRound(round);
            Assert.AreEqual(CampaignContent.Round(round).Fingerprint,definition.Fingerprint);
            CollectionAssert.AreEquivalent(new[]{"jd1","jd2","jd3"},catalog.SpeedsFor(round).Keys);
            var oldGold=AdamR20HostTests.CompleteRound(host,Boss(round));
            var committed=File.ReadAllBytes(path);
            host=new AdamHost(catalog,store);
            Assert.IsNull(host.Error);
            Assert.IsTrue(host.Paused);
            Assert.AreEqual(round,host.Session.RoundNumber);
            Assert.AreEqual(round+1,host.State.Run.RoundNumber);
            Assert.AreEqual(oldGold+definition.CompletionGold,host.State.Profile.Gold);
            Assert.IsFalse(host.ContentComplete);
            Assert.IsTrue(host.AwaitingNextRound);
            Assert.IsFalse(host.JoshuaComplete);
            Assert.IsFalse(host.CanEnterJudges);
            CollectionAssert.AreEqual(committed,File.ReadAllBytes(path));
            if(round!=50)host.SetPaused(false);
        }
        // R50 is an ordinary Judges round now: no book completion, the next round is R51.
        Assert.AreEqual(50,host.Session.RoundNumber);
        Assert.AreEqual(51,host.State.Run.RoundNumber);
        Assert.IsFalse(host.BookComplete);
        Assert.IsFalse(host.CanStartNew);
        Assert.IsFalse(host.CanEnterJudges);
        Assert.IsFalse(host.CanEnterRuth);
        Assert.AreEqual(0,AdamRoundPresentation.NextChapter(host));
        CollectionAssert.AreEqual(new[]{21,24,27,30,33,36,39,42,45,48},host.State.Run.LaterBossHistory.Select(x=>x.EncounterRound).ToArray());
        host.SetPaused(false);host.Tick(.1);
        Assert.AreEqual(51,host.Session.RoundNumber);
        Assert.AreEqual(51,host.State.Run.RoundNumber);
        Assert.AreEqual("normal",host.State.Run.Stage);
        Assert.AreEqual(runId,host.State.Run.RunId);
    }

    [TestCase(41,"기브온의 해")] [TestCase(42,"세겜 언약")]
    public void LateJoshuaTitlesUseBookLocalRound(int round,string name)
    {
        Assert.AreEqual($"여호수아 {round-36} / {name} · 3분 시제품",AdamRoundPresentation.Title(round));
        Assert.AreEqual(6,AdamBookTitle.Total(round));
        Assert.AreEqual(AdamBookTitle.Boss(5,round-37),round==42);
        StringAssert.Contains("사사기는 현재 성장·장비로 계속",AdamRoundPresentation.CompletionDetail(false,false,joshuaComplete:true,judgesInstalled:true));
    }

    [TestCase(43,"옷니엘")] [TestCase(44,"에훗")] [TestCase(45,"드보라")] [TestCase(46,"기드온의 양털")]
    [TestCase(47,"삼백 용사")] [TestCase(48,"입다")] [TestCase(49,"마노아의 제단")] [TestCase(50,"삼손과 사자")]
    public void JudgesTitlesUseBookLocalRound(int round,string name)
    {
        Assert.AreEqual($"사사기 {round-42} / {name} · 3분 시제품",AdamRoundPresentation.Title(round));
        Assert.AreEqual(9,AdamBookTitle.Total(round));
        Assert.AreEqual(AdamBookTitle.Boss(6,round-43),round is 45 or 48);
    }

    [TestCase(42,"흔들리는 경계석")] [TestCase(45,"급류의 뱀")] [TestCase(48,"나루터의 문지기")]
    public void LaterBookBossHudShowsProjectileWarning(int round,string name)
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
