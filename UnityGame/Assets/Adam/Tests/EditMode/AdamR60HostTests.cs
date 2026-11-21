using System;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Json;
using BibleSurvivor.Core;
using BibleSurvivor.Unity;
using NUnit.Framework;
using UnityEngine;

public sealed class AdamR60HostTests
{
    string directory, path;

    [SetUp] public void Setup()
    {
        directory=Path.Combine(Path.GetTempPath(),"adam-r60-host-"+Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        path=Path.Combine(directory,"save.json");
    }

    [TearDown] public void Cleanup()=>Directory.Delete(directory,true);

    static string Boss(int round)=>round switch
    {
        45=>"torrent_serpent",48=>"ford_gatekeeper",51=>"toppling_pillar",
        54=>"chaff_whirl",57=>"swarm_monarch",60=>"valley_bronze_brute",_=>null
    };

    static AdamCatalog Partial(bool ruth,bool firstSamuel)=>new AdamCatalog(Resources.Load<TextAsset>("genesis-catalog").text,
        Resources.Load<TextAsset>("exodus-catalog").text,installLeviticus:true,installNumbers:true,installDeuteronomy:true,installJoshua:true,
        installJudges:true,installRuth:ruth,installFirstSamuel:firstSamuel);

    // Joshua completion → Judges through the R51 boss, stopped on the Judges completion screen.
    static AdamHost CompletedJudges(string path)
    {
        var host=AdamR50HostTests.CompletedJoshua(path);
        Assert.IsTrue(host.TryEnterJudges(),host.Error);
        for(var round=43;round<=51;round++)
        {
            Assert.AreEqual(round,host.Session.RoundNumber);
            AdamR20HostTests.CompleteRound(host,Boss(round));
            if(round!=51)host.SetPaused(false);
        }
        Assert.IsTrue(host.JudgesComplete,host.Error);
        return host;
    }

    static AdamHost CompletedRuth(string path)
    {
        var host=CompletedJudges(path);
        Assert.IsTrue(host.TryEnterRuth(),host.Error);
        for(var round=52;round<=54;round++)
        {
            Assert.AreEqual(round,host.Session.RoundNumber);
            AdamR20HostTests.CompleteRound(host,Boss(round));
            if(round!=54)host.SetPaused(false);
        }
        Assert.IsTrue(host.RuthComplete,host.Error);
        return host;
    }

    [TestCase(false)] [TestCase(true)]
    public void RuthEntryRetryKeepsR51PublishedUntilR52Commits(bool replaceBeforeFailure)
    {
        CompletedJudges(path);
        var partial=new AdamHost(Partial(false,false),new SaveStore(path));
        Assert.IsNull(partial.Error);
        Assert.IsTrue(partial.JudgesComplete);
        Assert.IsTrue(partial.CanStartNew);
        Assert.IsFalse(partial.AwaitingNextRound);
        Assert.IsFalse(partial.CanEnterRuth);
        Assert.IsFalse(partial.TryEnterRuth());
        Assert.AreEqual(0,AdamRoundPresentation.NextChapter(partial));
        Assert.AreEqual(AdamRoundPresentation.JudgesHeading,AdamRoundPresentation.CompletionHeading(partial));
        StringAssert.Contains("사사기 1–9 라운드",AdamRoundPresentation.CompletionDetail(partial));
        StringAssert.DoesNotContain("룻기",AdamRoundPresentation.CompletionDetail(partial));

        var callbacks=0;
        var store=new SaveStore(path,()=>
        {
            if(++callbacks!=1)return;
            if(replaceBeforeFailure)
                File.Replace(Directory.GetFiles(directory,"save.json.tmp-*").Single(),path,path+".bak");
            throw new IOException("Ruth entry write or acknowledgement failure");
        });
        var host=new AdamHost(AdamCatalog.Load(),store);
        var published=host.State;
        var publishedBytes=Bytes(published);
        Assert.IsTrue(host.JudgesComplete);
        Assert.IsTrue(host.BookComplete);
        Assert.IsFalse(host.ContentComplete);
        Assert.IsFalse(host.AwaitingNextRound);
        Assert.IsTrue(host.CanEnterRuth);
        Assert.IsFalse(host.CanEnterJudges);
        Assert.IsFalse(host.CanEnterFirstSamuel);
        Assert.AreEqual(RuthContent.GlobalRoundNumber,AdamRoundPresentation.NextChapter(host));
        Assert.AreEqual(AdamRoundPresentation.RuthStartCard,AdamRoundPresentation.StartCard(AdamRoundPresentation.NextChapter(host)));
        Assert.AreEqual(AdamRoundPresentation.JudgesHeading,AdamRoundPresentation.CompletionHeading(host));
        StringAssert.Contains("룻기는 현재 성장·장비로 계속",AdamRoundPresentation.CompletionDetail(host));
        Assert.IsFalse(host.TryEnterRuth());
        Assert.IsTrue(host.HasRuthEntryRetry);
        Assert.IsFalse(host.HasFirstSamuelEntryRetry);
        Assert.AreEqual((AdamRoundPresentation.JudgesHeading,"룻기 진입 저장을 확인하지 못했습니다.\n같은 저장을 재시도하거나 저장 기록을 다시 불러오세요."),AdamRoundPresentation.EntryRetryModal(host));
        Assert.IsTrue(host.Paused);
        Assert.IsTrue(host.JudgesComplete);
        Assert.IsFalse(host.TryEnterRuth());
        host.Start("sling");host.Tick(.1);
        Assert.AreSame(published,host.State);
        CollectionAssert.AreEqual(publishedBytes,Bytes(host.State));
        var afterFailure=store.Load();
        Assert.AreEqual(published.Revision+(replaceBeforeFailure?1:0),afterFailure.Revision);

        Assert.IsTrue(host.RetrySave(),host.Error);
        Assert.AreEqual(replaceBeforeFailure?1:2,callbacks);
        Assert.IsFalse(host.HasRuthEntryRetry);
        Assert.IsTrue(host.Paused);
        Assert.AreEqual(52,host.Session.RoundNumber);
        Assert.AreEqual(published.Revision+1,host.State.Revision);
        if(replaceBeforeFailure)Assert.AreEqual(afterFailure.LastOperationId,host.State.LastOperationId);
        Assert.AreEqual(published.Run.RunId,host.State.Run.RunId);
        CollectionAssert.AreEqual(Bytes(published.Profile),Bytes(host.State.Profile));
        CollectionAssert.AreEqual(Bytes(published.Run.Boss),Bytes(host.State.Run.LaterBossHistory.Last()));
        CollectionAssert.AreEqual(new[]{21,24,27,30,33,36,39,42,45,48,51},host.State.Run.LaterBossHistory.Select(x=>x.EncounterRound).ToArray());
        Assert.Zero(host.World.SimulationTime);
        Assert.IsFalse(partial.Reload());
        Assert.IsTrue(partial.RequiresNewerGame);
    }

    [TestCase(false)] [TestCase(true)]
    public void FirstSamuelEntryRetryKeepsR54PublishedUntilR55Commits(bool replaceBeforeFailure)
    {
        CompletedRuth(path);
        var partial=new AdamHost(Partial(true,false),new SaveStore(path));
        Assert.IsNull(partial.Error);
        Assert.IsTrue(partial.RuthComplete);
        Assert.IsTrue(partial.CanStartNew);
        Assert.IsFalse(partial.AwaitingNextRound);
        Assert.IsFalse(partial.CanEnterFirstSamuel);
        Assert.IsFalse(partial.TryEnterFirstSamuel());
        Assert.AreEqual(0,AdamRoundPresentation.NextChapter(partial));
        Assert.AreEqual(AdamRoundPresentation.RuthHeading,AdamRoundPresentation.CompletionHeading(partial));
        StringAssert.Contains("룻기 1–3 라운드",AdamRoundPresentation.CompletionDetail(partial));
        StringAssert.DoesNotContain("사무엘상",AdamRoundPresentation.CompletionDetail(partial));

        var callbacks=0;
        var store=new SaveStore(path,()=>
        {
            if(++callbacks!=1)return;
            if(replaceBeforeFailure)
                File.Replace(Directory.GetFiles(directory,"save.json.tmp-*").Single(),path,path+".bak");
            throw new IOException("First Samuel entry write or acknowledgement failure");
        });
        var host=new AdamHost(AdamCatalog.Load(),store);
        var published=host.State;
        var publishedBytes=Bytes(published);
        Assert.IsTrue(host.RuthComplete);
        Assert.IsTrue(host.BookComplete);
        Assert.IsFalse(host.ContentComplete);
        Assert.IsFalse(host.AwaitingNextRound);
        Assert.IsTrue(host.CanEnterFirstSamuel);
        Assert.IsFalse(host.CanEnterRuth);
        Assert.AreEqual(FirstSamuelContent.GlobalRoundNumber,AdamRoundPresentation.NextChapter(host));
        Assert.AreEqual(AdamRoundPresentation.FirstSamuelStartCard,AdamRoundPresentation.StartCard(AdamRoundPresentation.NextChapter(host)));
        Assert.AreEqual(AdamRoundPresentation.RuthHeading,AdamRoundPresentation.CompletionHeading(host));
        StringAssert.Contains("사무엘상은 현재 성장·장비로 계속",AdamRoundPresentation.CompletionDetail(host));
        Assert.IsFalse(host.TryEnterFirstSamuel());
        Assert.IsTrue(host.HasFirstSamuelEntryRetry);
        Assert.IsFalse(host.HasRuthEntryRetry);
        Assert.AreEqual((AdamRoundPresentation.RuthHeading,"사무엘상 진입 저장을 확인하지 못했습니다.\n같은 저장을 재시도하거나 저장 기록을 다시 불러오세요."),AdamRoundPresentation.EntryRetryModal(host));
        Assert.IsTrue(host.Paused);
        Assert.IsTrue(host.RuthComplete);
        host.Start("sling");host.Tick(.1);
        Assert.AreSame(published,host.State);
        CollectionAssert.AreEqual(publishedBytes,Bytes(host.State));
        var afterFailure=store.Load();
        Assert.AreEqual(published.Revision+(replaceBeforeFailure?1:0),afterFailure.Revision);

        Assert.IsTrue(host.RetrySave(),host.Error);
        Assert.AreEqual(replaceBeforeFailure?1:2,callbacks);
        Assert.IsFalse(host.HasFirstSamuelEntryRetry);
        Assert.AreEqual(55,host.Session.RoundNumber);
        Assert.AreEqual(published.Revision+1,host.State.Revision);
        if(replaceBeforeFailure)Assert.AreEqual(afterFailure.LastOperationId,host.State.LastOperationId);
        Assert.AreEqual(published.Run.RunId,host.State.Run.RunId);
        CollectionAssert.AreEqual(Bytes(published.Profile),Bytes(host.State.Profile));
        CollectionAssert.AreEqual(Bytes(published.Run.Boss),Bytes(host.State.Run.LaterBossHistory.Last()));
        CollectionAssert.AreEqual(new[]{21,24,27,30,33,36,39,42,45,48,51,54},host.State.Run.LaterBossHistory.Select(x=>x.EncounterRound).ToArray());
        Assert.IsFalse(partial.Reload());
        Assert.IsTrue(partial.RequiresNewerGame);
    }

    [Test]
    public void PartialFirstSamuelInstallKeepsTheRunAcrossReloadsAndStopsAtR60()
    {
        var catalog=Partial(true,true);
        Assert.AreEqual(60,catalog.LastInstalledRound);
        var store=new SaveStore(path);
        var host=CompletedRuth(path);
        var runId=host.State.Run.RunId;
        var gold=host.State.Profile.Gold;
        Assert.IsTrue(host.TryEnterFirstSamuel(),host.Error);
        Assert.AreEqual(gold,host.State.Profile.Gold);
        for(var round=55;round<=60;round++)
        {
            Assert.AreEqual(round,host.Session.RoundNumber);
            Assert.AreEqual(runId,host.State.Run.RunId);
            var definition=catalog.ForRound(round);
            Assert.AreEqual(CampaignContent.Round(round).Fingerprint,definition.Fingerprint);
            CollectionAssert.AreEquivalent(new[]{"sm1","sm2","sm3"},catalog.SpeedsFor(round).Keys);
            var oldGold=AdamR20HostTests.CompleteRound(host,Boss(round));
            var committed=File.ReadAllBytes(path);
            host=new AdamHost(catalog,store);
            Assert.IsNull(host.Error);
            Assert.IsTrue(host.Paused);
            Assert.AreEqual(round,host.Session.RoundNumber);
            Assert.AreEqual(round+1,host.State.Run.RoundNumber);
            Assert.AreEqual(oldGold+definition.CompletionGold,host.State.Profile.Gold);
            Assert.AreEqual(round==60,host.ContentComplete);
            Assert.AreEqual(round!=60,host.AwaitingNextRound);
            Assert.IsFalse(host.RuthComplete);
            Assert.IsFalse(host.CanEnterFirstSamuel);
            CollectionAssert.AreEqual(committed,File.ReadAllBytes(path));
            if(round!=60)host.SetPaused(false);
        }
        var revision=host.State.Revision;
        host.SetPaused(false);host.Tick(.1);
        Assert.AreEqual(60,host.Session.RoundNumber);
        Assert.AreEqual(61,host.State.Run.RoundNumber);
        Assert.AreEqual(revision,host.State.Revision);
        Assert.IsTrue(host.CanStartNew);
        Assert.IsFalse(host.CanPlay);
        Assert.IsFalse(host.CanEnterRuth);
        Assert.IsFalse(host.CanEnterFirstSamuel);
        Assert.IsFalse(host.CanEnterJudges);
        Assert.AreEqual(0,AdamRoundPresentation.NextChapter(host));
        Assert.AreEqual("사무엘상 6 완료 · 진행 저장됨",AdamRoundPresentation.CompletionHeading(host));
        var detail=AdamRoundPresentation.CompletionDetail(host);
        StringAssert.Contains("완료 골드 +"+CampaignContent.Round(60).CompletionGold,detail);
        StringAssert.Contains("사무엘상 1–6 라운드",detail);
        Assert.AreEqual(60,host.State.Run.Boss.EncounterRound);
        CollectionAssert.AreEqual(new[]{21,24,27,30,33,36,39,42,45,48,51,54,57},host.State.Run.LaterBossHistory.Select(x=>x.EncounterRound).ToArray());
        Assert.Throws<InvalidDataException>(()=>catalog.ForRound(61));
        Assert.Throws<UnsupportedSaveVersionException>(()=>catalog.ForCheckpoint(new RunState{RoundNumber=61,Stage="normal"}));
        Assert.Throws<UnsupportedSaveVersionException>(()=>catalog.ForCheckpoint(new RunState{RoundNumber=62,Stage="between_rounds"}));
    }

    [TestCase(51,"사사기",9,9,"삼손의 마지막")]
    [TestCase(52,"룻기",1,3,"모압에서 돌아옴")] [TestCase(53,"룻기",2,3,"보아스의 밭")] [TestCase(54,"룻기",3,3,"타작마당")]
    [TestCase(55,"사무엘상",1,6,"한나의 기도")] [TestCase(56,"사무엘상",2,6,"실로의 부르심")] [TestCase(57,"사무엘상",3,6,"빼앗긴 언약궤")]
    [TestCase(58,"사무엘상",4,6,"사울의 기름부음")] [TestCase(59,"사무엘상",5,6,"믹마스의 요나단")] [TestCase(60,"사무엘상",6,6,"엘라 골짜기")]
    public void LaterTitlesUseBookLocalRoundAndFutureRoundsRemainUninstalled(int round,string book,int local,int total,string name)
    {
        Assert.AreEqual($"{book} {local} / {name} · 3분 시제품",AdamRoundPresentation.Title(round));
        Assert.AreEqual(total,AdamBookTitle.Total(round));
        Assert.AreEqual(AdamBookTitle.Boss(AdamBookTitle.Book(round),local-1),round is 51 or 54 or 57 or 60);
        Assert.Throws<InvalidOperationException>(()=>AdamRoundPresentation.Name(CampaignContent.LastInstalledRound+1));
        Assert.Throws<InvalidOperationException>(()=>AdamRoundPresentation.Title(CampaignContent.LastInstalledRound+1));
    }

    [Test]
    public void PartialInstallsAndCampaignEndUseTheirOwnCompletionText()
    {
        Assert.AreEqual("사사기 완료 · 진행 저장됨",AdamRoundPresentation.CompletionHeading(false,false,judgesComplete:true));
        StringAssert.Contains("완료 골드 +"+CampaignContent.Round(51).CompletionGold,AdamRoundPresentation.CompletionDetail(false,false,judgesComplete:true));
        StringAssert.Contains("룻기는 현재 성장·장비로 계속",AdamRoundPresentation.CompletionDetail(false,false,judgesComplete:true,ruthInstalled:true));
        Assert.AreEqual("룻기 완료 · 진행 저장됨",AdamRoundPresentation.CompletionHeading(false,false,ruthComplete:true));
        StringAssert.Contains("완료 골드 +"+CampaignContent.Round(54).CompletionGold,AdamRoundPresentation.CompletionDetail(false,false,ruthComplete:true));
        StringAssert.Contains("사무엘상은 현재 성장·장비로 계속",AdamRoundPresentation.CompletionDetail(false,false,ruthComplete:true,firstSamuelInstalled:true));
        Assert.AreEqual("욥기 1 완료 · 진행 저장됨",AdamRoundPresentation.CompletionHeading(false,false,contentComplete:true));
        StringAssert.Contains("욥기 1 라운드",AdamRoundPresentation.CompletionDetail(false,false,contentComplete:true));
    }

    [Test]
    public void InstalledActorsReuseTheReviewedSpriteAndShadowPair()
    {
        foreach(var round in System.Linq.Enumerable.Range(61,40))
        {
            var actorIds=CampaignContent.Round(round).Waves.SelectMany(wave=>wave.Groups)
                .Select(group=>group.MonsterId).Distinct();
            foreach(var id in actorIds)
            {
                Assert.IsNotEmpty(AdamArtWorld.EnemyClip(id));
                Assert.IsNotEmpty(AdamArtWorld.EnemyShadow(id));
            }
        }
        foreach(var round in new[]{63,66,69,72,75,78,81,84,87,90,93,96,99})
            Assert.AreEqual("fx_shadow-boss_static_40x8",AdamArtWorld.EnemyShadow(BossDefinition.ForRound(round).Id));
    }

    [Test]
    public void GateLionDisplaysBothLiveWarningsTogether()
    {
        var fixture=Path.GetFullPath(Path.Combine(Application.dataPath,"..","..","tests",
            "BibleSurvivor.Core.Tests","Fixtures","r80-build-complete-save.json"));
        var state=new SaveStore(fixture).Load();
        var session=AdamSession.Resume(state,CampaignContent.Round(80)).StartNextRound(CampaignContent.Round(81));
        session.Advance(180);
        var world=new BattleWorld(session,CampaignContent.SpeedsFor(81),7);
        world.Advance(7,System.Numerics.Vector2.Zero);
        Assert.IsTrue(session.Boss.Telegraphing);
        Assert.AreEqual("돌진 · 사슬 3방향 예고",AdamBossPresentation.Phase(session));
        Assert.IsTrue(AdamChariotWarning.TryBossGeometry(world.ChariotDash,out _));
        using var art=new AdamArtWorld();
        var snapshot=world.Capture();
        for(var direction=0;direction<3;direction++)
            Assert.IsTrue(art.TryChainGeometry(snapshot,session.Boss,session.Stage,direction,out _));
    }

    [Test]
    public void BullKeepsItsAttackPoseUntilTheDashEnds()
    {
        var rules=BossDefinition.ForRound(78);
        var boss=new BossEncounterState{RoundNumber=78,BossElapsed=6.5,NextAttackAt=12,
            ChariotCharge=new ChariotChargeState{Phase="dash",Until=6.6}};
        Assert.AreEqual("돌진 진행",AdamBossPresentation.Phase("boss",boss,rules));
        Assert.AreEqual(AdamExodusR06Art.BossAttack,AdamExodusR06Art.BossClip("boss",boss,6.5,out var frame,out _));
        Assert.AreEqual(1,frame);
        boss.ChariotCharge.Phase="chase";
        Assert.AreEqual(AdamExodusR06Art.BossMove,AdamExodusR06Art.BossClip("boss",boss,6.5,out _,out _));
    }

    [TestCase(63,"사슬 3방향 예고")]
    [TestCase(66,"지원군 · 추격")]
    [TestCase(69,"내려치기 예고")]
    public void InstalledBossesUseTheirOwnActionsInHudAndAnimation(int round,string expected)
    {
        var rules=BossDefinition.ForRound(round);
        var boss=new BossEncounterState{RoundNumber=round,BossElapsed=5,NextAttackAt=10,
            Telegraphing=rules.UsesChainProjectiles||rules.UsesSlam,ThresholdUsed=rules.SummonsSupport};
        StringAssert.Contains(expected,AdamBossPresentation.Phase("boss",boss,rules));
        boss.Telegraphing=false;
        Assert.AreEqual(rules.SummonsSupport?AdamExodusR06Art.BossMove:AdamExodusR06Art.BossAttack,
            AdamExodusR06Art.BossClip("boss",boss,5,out _,out _));
    }

    [TestCase(51,"무너지는 기둥")] [TestCase(54,"타작마당의 겨바람")] [TestCase(57,"무리의 군주")] [TestCase(60,"골짜기의 청동 짐승")]
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
