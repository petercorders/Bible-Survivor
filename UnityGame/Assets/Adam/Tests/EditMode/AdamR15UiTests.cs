using System;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Json;
using BibleSurvivor.Core;
using BibleSurvivor.Unity;
using NUnit.Framework;
using UnityEngine;

public sealed class AdamR15UiTests
{
    [Test] public void BezalelUsesSixthExodusTitleAndR51RemainsUninstalled()
    {
        Assert.AreEqual("브살렐",AdamRoundPresentation.Name(15));
        Assert.AreEqual("출애굽기 6 / 브살렐 · 3분 시제품",AdamRoundPresentation.Title(15));
        Assert.AreEqual("출애굽기 5 / 아론 · 3분 시제품",AdamRoundPresentation.Title(14));
        Assert.Throws<InvalidOperationException>(()=>AdamRoundPresentation.Name(61));
        Assert.Throws<InvalidOperationException>(()=>AdamRoundPresentation.Title(61));
    }

    [Test] public void ExplicitBookCompletionIsSeparateFromInstalledEndAndGenesisEntry()
    {
        Assert.AreEqual("출애굽기 완료 · 성막 완성",AdamRoundPresentation.CompletionHeading(false,true));
        Assert.AreEqual("출애굽기 6 완료 · 진행 저장됨",AdamRoundPresentation.CompletionHeading(false,false));
        StringAssert.DoesNotContain("출애굽기 완료",AdamRoundPresentation.CompletionHeading(false,false));
        Assert.AreEqual(AdamRoundPresentation.ChapterHeading,AdamRoundPresentation.CompletionHeading(true,false));
        Assert.AreEqual(AdamRoundPresentation.ChapterDetail,AdamRoundPresentation.CompletionDetail(true,false));
        var detail=AdamRoundPresentation.CompletionDetail(false,true);
        StringAssert.Contains("완료 골드 +280",detail);
        StringAssert.Contains("레위기 미구현",detail);
        StringAssert.Contains("진행 유지",detail);
        StringAssert.Contains("새 도전 선택 시 성장·장비 초기화",detail);
        StringAssert.DoesNotContain("Enter",detail);
    }

    [TestCase("ex16","fx_shadow-small_static_12x4")]
    [TestCase("ex17","fx_shadow-wide_static_24x6")]
    [TestCase("ex18","fx_shadow-wide_static_24x6")]
    [TestCase("defilement_weaver","fx_shadow-boss_static_40x8")]
    public void R15EnemiesUseExplicitContractShadows(string id,string shadow)
    {
        Assert.IsTrue(AdamRoundPresentation.R15Enemy(id));
        Assert.AreEqual(shadow,AdamRoundPresentation.Shadow(id));
    }

    [Test] public void OtherRoundsAndUnknownIdsDoNotFallBackToR15Art()
    {
        foreach(var id in new[]{"ex13","ex14","ex15","oppression_chariot","ex19","unknown",null})
            Assert.IsFalse(AdamRoundPresentation.R15Enemy(id));
        Assert.Throws<InvalidOperationException>(()=>AdamRoundPresentation.Shadow("ex19"));
        Assert.AreEqual("fx_shadow-small_static_12x4",AdamRoundPresentation.Shadow("ex13"));
        Assert.AreEqual("fx_shadow-boss_static_40x8",AdamRoundPresentation.Shadow("oppression_chariot"));
    }

    [Test] public void WeaverNameHealthAndTwoLineHudUseSelectedRules()
    {
        var rules=BossDefinition.ForRound(15);
        Assert.AreEqual("훼손의 직조군주",AdamBossPresentation.Name(rules));
        Assert.AreEqual("훼손의 직조군주",AdamBossPresentation.Name(15));
        Assert.AreEqual(26000,rules.MaxHealth);
        Assert.AreEqual(.5,AdamBossPresentation.HealthFraction(new BossEncounterState{Health=13000},rules));
        Assert.IsTrue(AdamBossPresentation.UsesTwoLineHud(rules));
        Assert.IsTrue(AdamBossPresentation.UsesTwoLineHud(BossDefinition.ForRound(12)));
        Assert.IsFalse(AdamBossPresentation.UsesTwoLineHud(BossDefinition.ForRound(9)));
    }

    [TestCase(false)][TestCase(true)]
    public void WeaverDisplaysThreeShardDirectionsAndRuleClockWithoutSlamDashOrChains(bool warning)
    {
        var rules=BossDefinition.ForRound(15);
        var boss=new BossEncounterState{Telegraphing=warning,ThresholdUsed=true};
        var text=AdamBossPresentation.Phase("boss",boss,rules);
        StringAssert.Contains("쇳조각 3방향",text);
        StringAssert.Contains("5초 주기",text);
        StringAssert.Contains("지원군",text);
        if(warning){StringAssert.Contains("예고",text);StringAssert.Contains("1초",text);}
        foreach(var word in new[]{"내려치기","돌진","사슬"})StringAssert.DoesNotContain(word,text);
        Assert.AreEqual("등장 · 피해 보호",AdamBossPresentation.Phase("boss_intro",boss,rules));
        Assert.AreEqual("사슬 3방향 예고",AdamBossPresentation.AttackPhase(9,true,false,5));
    }

    [Test] public void Fixed500Fallback100Growth25AndCompletion280StayDistinct()
    {
        StringAssert.Contains("고정 골드 +500 적용됨",AdamBossPresentation.RewardDetail(BossDefinition.ForRound(15)));
        var fallback=AdamBossPresentation.Card(new BossRewardCandidate{Kind="gold",Gold=100},0,null);
        StringAssert.Contains("+100",fallback);StringAssert.DoesNotContain("+500",fallback);
        var growth=AdamChoiceText.Describe(AdamSession.GrowthGoldChoicePrefix+"1",new EquipmentInventory("sling"));
        Assert.AreEqual("영구 골드 +25",growth.Effect);
        StringAssert.DoesNotContain("강화",growth.Card(0));
        Assert.AreEqual(260,ExodusContent.Round(14).CompletionGold);
        StringAssert.Contains("완료 골드 +280",AdamRoundPresentation.ContentDetail);
    }

    // Technical timeout/direct boss damage fixture; no user save or natural-play claim.
    internal static AdamHost CompletedFifteen(string path)
    {
        var h=AdamR14HostTests.Start(path);
        h.Session.Advance(180);Assert.IsTrue(h.Checkpoint(),h.Error);
        h.SetPaused(false);Assert.AreEqual(15,h.Session.RoundNumber);
        h.Session.Advance(180);h.Session.Capture();h.State.Run.World=null;
        new SaveStore(path).Commit(h.State,h.State.Revision,"technical-r15-ui-intro");
        Assert.IsTrue(h.Reload(),h.Error);h.SetPaused(false);
        for(var i=0;i<20;i++)h.Tick(.1);
        Assert.AreEqual("boss",h.Session.Stage);
        h.Session.Hit(h.Session.BossRules.BossActorId,h.Session.BossRules.MaxHealth);
        Assert.IsTrue(h.Checkpoint(),h.Error);
        if(h.Session.Stage=="boss_reward")Assert.IsTrue(h.ChooseBossReward(h.Session.Boss.Reward.RewardId,h.Session.BossChoices.First().Id));
        while(h.Session.PendingChoices.Count>0)Assert.IsTrue(h.Choose(h.Session.PendingChoices[0]));
        Assert.IsTrue(h.CompleteBossSettlement(),h.Error);
        Assert.IsTrue(h.Reload(),h.Error);return h;
    }
    static byte[] Bytes(object value)
    {using(var m=new MemoryStream()){new DataContractJsonSerializer(value.GetType()).WriteObject(m,value);return m.ToArray();}}

    [TestCase(0)][TestCase(1)][TestCase(2)]
    public void ExodusOnlyInstallKeepsProgressUntilExplicitNewChallengeAndDuplicateIsConsumed(int slot)
    {
        var dir=Path.Combine(Path.GetTempPath(),"adam-r15-ui-"+Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);var path=Path.Combine(dir,"save.json");
        var go=new GameObject("R15 UI commands");go.SetActive(false);
        try
        {
            CompletedFifteen(path);
            var genesis=Resources.Load<TextAsset>("genesis-catalog").text;
            var exodus=Resources.Load<TextAsset>("exodus-catalog").text;
            var h=new AdamHost(new AdamCatalog(genesis,exodus),new SaveStore(path));
            var saved=File.ReadAllBytes(path);
            var run=Bytes(h.State.Run);var profile=Bytes(h.State.Profile);var id=h.State.Run.RunId;
            Assert.IsTrue(h.ExodusComplete);Assert.IsTrue(h.ContentComplete);
            Assert.IsFalse(h.Session.ChapterComplete);Assert.IsFalse(h.CanEnterExodus);
            Assert.IsFalse(h.AwaitingNextRound);Assert.IsTrue(h.CanStartNew);
            Assert.AreEqual("출애굽기 완료 · 성막 완성",AdamRoundPresentation.CompletionHeading(h.Session.ChapterComplete,h.ExodusComplete));
            var game=go.AddComponent<AdamGame>();game.AutoInitialize=false;game.enabled=false;
            game.Initialize(h);game.SetLifecycle(true,false);
            game.EnterCommand();game.EnterCommand();game.EnterExodus();h.Tick(30);
            Assert.IsFalse(h.TryEnterExodus());CollectionAssert.AreEqual(run,Bytes(h.State.Run));
            CollectionAssert.AreEqual(saved,File.ReadAllBytes(path));
            game.SetLifecycle(false,true);game.SelectSlot(slot);game.EnterCommand();
            CollectionAssert.AreEqual(run,Bytes(h.State.Run));
            game.SetLifecycle(true,false);game.SelectSlot(slot);var newId=h.State.Run.RunId;
            Assert.AreEqual(1,h.Session.RoundNumber);Assert.AreEqual(2,h.State.SchemaVersion);
            Assert.AreNotEqual(id,newId);Assert.IsTrue(h.Session.Inventory.Weapons.ContainsKey(AdamGame.Weapons[slot]));
            CollectionAssert.AreEqual(profile,Bytes(h.State.Profile));Assert.IsFalse(h.ExodusComplete);
            game.SelectSlot((slot+1)%3);game.EnterCommand();Assert.AreEqual(newId,h.State.Run.RunId);
        }
        finally{UnityEngine.Object.DestroyImmediate(go);Directory.Delete(dir,true);}
    }

    [TestCase(false)][TestCase(true)]
    public void GenesisEnterAndNewChallengeStillShareOneCommandPerFrame(bool newChallengeFirst)
    {
        var dir=Path.Combine(Path.GetTempPath(),"adam-r15-genesis-ui-"+Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(dir);var go=new GameObject("Genesis explicit entry");go.SetActive(false);
        try
        {
            var h=AdamR10HostTests.CompletedGenesis(Path.Combine(dir,"save.json"));var id=h.State.Run.RunId;
            var game=go.AddComponent<AdamGame>();game.AutoInitialize=false;game.enabled=false;
            game.Initialize(h);game.SetLifecycle(true,false);
            if(newChallengeFirst)
            {
                game.SelectSlot(0);game.EnterCommand();
                Assert.AreEqual(1,h.Session.RoundNumber);Assert.AreNotEqual(id,h.State.Run.RunId);
            }
            else
            {
                game.EnterCommand();game.EnterCommand();game.SelectSlot(0);
                Assert.AreEqual(10,h.Session.RoundNumber);Assert.AreEqual(id,h.State.Run.RunId);
            }
            Assert.IsFalse(h.ExodusComplete);Assert.IsNull(h.Error);
        }
        finally{UnityEngine.Object.DestroyImmediate(go);Directory.Delete(dir,true);}
    }

    [TestCase(660,440)][TestCase(1100,720)]
    public void AllThreeExplicitNewChallengeTargetsFitTheCompletionPanel(float width,float height)
    {
        var panel=AdamChoiceText.ChoicePanel(width,height);
        for(var i=0;i<3;i++)
        {
            StringAssert.Contains("새 도전",AdamChoiceText.StartCard(AdamGame.Weapons[i],i,true));
            var target=AdamChoiceText.ChapterAction(panel,i);
            Assert.GreaterOrEqual(target.yMin,panel.y+142);Assert.LessOrEqual(target.yMax,height);
            Assert.GreaterOrEqual(target.xMin,0);Assert.LessOrEqual(target.xMax,width);
            for(var j=0;j<i;j++)Assert.IsFalse(target.Overlaps(AdamChoiceText.ChapterAction(panel,j)));
        }
    }
}
