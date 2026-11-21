using System;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Json;
using BibleSurvivor.Core;
using BibleSurvivor.Unity;
using NUnit.Framework;
using UnityEngine;

public sealed class AdamR40HostTests
{
    string directory, path;

    [SetUp] public void Setup()
    {
        directory=Path.Combine(Path.GetTempPath(),"adam-r40-host-"+Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        path=Path.Combine(directory,"save.json");
    }

    [TearDown] public void Cleanup()=>Directory.Delete(directory,true);

    static string Boss(int round)=>round switch
    {
        18=>"ash_veil",21=>"oath_wraith",24=>"dust_tyrant",27=>"ravine_stalker",30=>"wilderness_warden",
        33=>"oblivion_mist",36=>"crossroads_sentinel",39=>"rampart_colossus",_=>null
    };

    static AdamCatalog Partial(bool deuteronomy)=>new AdamCatalog(Resources.Load<TextAsset>("genesis-catalog").text,
        Resources.Load<TextAsset>("exodus-catalog").text,installLeviticus:true,installNumbers:true,installDeuteronomy:deuteronomy);

    internal static AdamHost CompletedThirty(string path)
    {
        var host=AdamR15UiTests.CompletedFifteen(path);
        Assert.IsTrue(host.TryEnterLeviticus(),host.Error);
        for(var round=16;round<=30;round++)
        {
            if(round==NumbersContent.GlobalRoundNumber)Assert.IsTrue(host.TryEnterNumbers(),host.Error);
            Assert.AreEqual(round,host.Session.RoundNumber);
            AdamR20HostTests.CompleteRound(host,Boss(round));
            if(round!=21&&round!=30)host.SetPaused(false);
        }
        Assert.IsTrue(host.NumbersComplete,host.Error);
        return host;
    }

    [TestCase(false)] [TestCase(true)]
    public void DeuteronomyEntryRetryKeepsR30PublishedUntilR31Commits(bool replaceBeforeFailure)
    {
        CompletedThirty(path);
        var partial=new AdamHost(Partial(false),new SaveStore(path));
        Assert.IsNull(partial.Error);
        Assert.IsTrue(partial.NumbersComplete);
        Assert.IsTrue(partial.CanStartNew);
        Assert.IsFalse(partial.AwaitingNextRound);
        Assert.IsFalse(partial.CanEnterDeuteronomy);
        Assert.IsFalse(partial.TryEnterDeuteronomy());
        StringAssert.Contains("민수기 1–9 라운드",AdamRoundPresentation.CompletionDetail(false,false,numbersComplete:true,
            deuteronomyInstalled:partial.DeuteronomyInstalled));

        var callbacks=0;
        var store=new SaveStore(path,()=>
        {
            if(++callbacks!=1)return;
            if(replaceBeforeFailure)
                File.Replace(Directory.GetFiles(directory,"save.json.tmp-*").Single(),path,path+".bak");
            throw new IOException("Deuteronomy entry write or acknowledgement failure");
        });
        var host=new AdamHost(AdamCatalog.Load(),store);
        var published=host.State;
        var publishedBytes=Bytes(published);
        Assert.IsTrue(host.NumbersComplete);
        Assert.IsFalse(host.ContentComplete);
        Assert.IsFalse(host.AwaitingNextRound);
        Assert.IsTrue(host.CanEnterDeuteronomy);
        Assert.IsFalse(host.CanEnterJoshua);
        Assert.IsFalse(host.TryEnterDeuteronomy());
        Assert.IsTrue(host.HasDeuteronomyEntryRetry);
        Assert.IsTrue(host.Paused);
        Assert.IsTrue(host.NumbersComplete);
        Assert.IsFalse(host.TryEnterDeuteronomy());
        host.Start("sling");host.Tick(.1);
        Assert.AreSame(published,host.State);
        CollectionAssert.AreEqual(publishedBytes,Bytes(host.State));
        var afterFailure=store.Load();
        Assert.AreEqual(published.Revision+(replaceBeforeFailure?1:0),afterFailure.Revision);

        Assert.IsTrue(host.RetrySave(),host.Error);
        Assert.AreEqual(replaceBeforeFailure?1:2,callbacks);
        Assert.IsFalse(host.HasDeuteronomyEntryRetry);
        Assert.IsTrue(host.Paused);
        Assert.AreEqual(31,host.Session.RoundNumber);
        Assert.AreEqual(published.Revision+1,host.State.Revision);
        if(replaceBeforeFailure)Assert.AreEqual(afterFailure.LastOperationId,host.State.LastOperationId);
        Assert.AreEqual(published.Run.RunId,host.State.Run.RunId);
        CollectionAssert.AreEqual(Bytes(published.Profile),Bytes(host.State.Profile));
        CollectionAssert.AreEqual(Bytes(published.Run.Boss),Bytes(host.State.Run.LaterBossHistory.Last()));
        CollectionAssert.AreEqual(new[]{21,24,27,30},host.State.Run.LaterBossHistory.Select(x=>x.EncounterRound).ToArray());
        Assert.Zero(host.World.SimulationTime);
        Assert.IsFalse(partial.Reload());
        Assert.IsTrue(partial.RequiresNewerGame);
    }

    [Test]
    public void DeuteronomyAndJoshuaKeepTheRunAcrossReloadsAndContinueIntoR41()
    {
        var catalog=AdamCatalog.Load();
        var store=new SaveStore(path);
        CompletedThirty(path);
        var host=new AdamHost(catalog,store);
        var runId=host.State.Run.RunId;
        var gold=host.State.Profile.Gold;
        Assert.IsTrue(host.TryEnterDeuteronomy(),host.Error);
        Assert.AreEqual(gold,host.State.Profile.Gold);
        for(var round=31;round<=40;round++)
        {
            if(round==JoshuaContent.GlobalRoundNumber)
            {
                var partial=new AdamHost(Partial(true),new SaveStore(path));
                Assert.IsNull(partial.Error);
                Assert.IsTrue(partial.DeuteronomyComplete);
                Assert.IsFalse(partial.CanEnterJoshua);
                Assert.IsFalse(partial.AwaitingNextRound);
                Assert.IsTrue(partial.CanStartNew);
                gold=host.State.Profile.Gold;
                Assert.IsTrue(host.DeuteronomyComplete);
                Assert.IsFalse(host.ContentComplete);
                Assert.IsTrue(host.TryEnterJoshua(),host.Error);
                Assert.AreEqual(gold,host.State.Profile.Gold);
                Assert.IsFalse(partial.Reload());
                Assert.IsTrue(partial.RequiresNewerGame);
            }
            Assert.AreEqual(round,host.Session.RoundNumber);
            Assert.AreEqual(runId,host.State.Run.RunId);
            var definition=catalog.ForRound(round);
            Assert.AreEqual(CampaignContent.Round(round).Fingerprint,definition.Fingerprint);
            CollectionAssert.AreEquivalent(round<JoshuaContent.GlobalRoundNumber?new[]{"dt1","dt2","dt3"}:new[]{"js1","js2","js3"},
                catalog.SpeedsFor(round).Keys);
            var oldGold=AdamR20HostTests.CompleteRound(host,Boss(round));
            var committed=File.ReadAllBytes(path);
            host=new AdamHost(catalog,store);
            Assert.IsNull(host.Error);
            Assert.IsTrue(host.Paused);
            Assert.AreEqual(round,host.Session.RoundNumber);
            Assert.AreEqual(round+1,host.State.Run.RoundNumber);
            Assert.AreEqual(oldGold+definition.CompletionGold,host.State.Profile.Gold);
            Assert.AreEqual(round==36,host.DeuteronomyComplete);
            Assert.AreEqual(round==36,host.CanEnterJoshua);
            Assert.IsFalse(host.ContentComplete);
            Assert.AreEqual(round!=36,host.AwaitingNextRound);
            Assert.IsFalse(host.NumbersComplete);
            CollectionAssert.AreEqual(committed,File.ReadAllBytes(path));
            if(round!=36&&round!=40)host.SetPaused(false);
        }
        Assert.IsFalse(host.CanStartNew);
        host.SetPaused(false);host.Tick(.1);
        Assert.AreEqual(41,host.Session.RoundNumber);
        Assert.AreEqual(41,host.State.Run.RoundNumber);
        Assert.AreEqual("normal",host.State.Run.Stage);
        Assert.AreEqual(runId,host.State.Run.RunId);
        Assert.IsFalse(host.CanEnterJoshua);
        Assert.IsFalse(host.CanEnterDeuteronomy);
        CollectionAssert.AreEqual(new[]{21,24,27,30,33,36,39},host.State.Run.LaterBossHistory.Select(x=>x.EncounterRound).ToArray());
        Assert.Throws<InvalidDataException>(()=>catalog.ForRound(CampaignContent.LastInstalledRound+1));
        Assert.Throws<UnsupportedSaveVersionException>(()=>catalog.ForCheckpoint(new RunState{RoundNumber=CampaignContent.LastInstalledRound+1,Stage="normal"}));
    }

    [TestCase(31,"모세의 회고")] [TestCase(32,"두 돌판")] [TestCase(33,"기억하라")]
    [TestCase(34,"도피성")] [TestCase(35,"축복과 저주")] [TestCase(36,"느보산")]
    public void DeuteronomyTitlesUseBookLocalRound(int round,string name)
    {
        Assert.AreEqual($"신명기 {round-30} / {name} · 3분 시제품",AdamRoundPresentation.Title(round));
        Assert.AreEqual("신명기 완료 · 진행 저장됨",AdamRoundPresentation.CompletionHeading(false,false,deuteronomyComplete:true));
        StringAssert.Contains("신명기 1–6 라운드",AdamRoundPresentation.CompletionDetail(false,false,deuteronomyComplete:true));
        StringAssert.Contains("여호수아는 현재 성장·장비로 계속",AdamRoundPresentation.CompletionDetail(false,false,deuteronomyComplete:true,joshuaInstalled:true));
    }

    [TestCase(37,"요단 도하")] [TestCase(38,"길갈의 돌")] [TestCase(39,"여리고 성벽")] [TestCase(40,"아이 성")]
    public void JoshuaTitlesUseBookLocalRoundAndR61RemainsUninstalled(int round,string name)
    {
        Assert.AreEqual($"여호수아 {round-36} / {name} · 3분 시제품",AdamRoundPresentation.Title(round));
        Assert.AreEqual("여호수아 완료 · 진행 저장됨",AdamRoundPresentation.CompletionHeading(false,false,joshuaComplete:true));
        var detail=AdamRoundPresentation.CompletionDetail(false,false,joshuaComplete:true);
        StringAssert.Contains("완료 골드 +"+CampaignContent.Round(42).CompletionGold,detail);
        StringAssert.Contains("여호수아 1–6 라운드",detail);
        Assert.Throws<InvalidOperationException>(()=>AdamRoundPresentation.Name(CampaignContent.LastInstalledRound+1));
        Assert.Throws<InvalidOperationException>(()=>AdamRoundPresentation.Title(CampaignContent.LastInstalledRound+1));
    }

    [TestCase(33,"망각의 안개")] [TestCase(36,"갈림길의 파수꾼")] [TestCase(39,"성벽의 거상")]
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
