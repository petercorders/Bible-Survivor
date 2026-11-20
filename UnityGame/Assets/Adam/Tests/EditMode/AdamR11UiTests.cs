using System;
using BibleSurvivor.Core;
using BibleSurvivor.Unity;
using NUnit.Framework;

public sealed class AdamR11UiTests
{
    [Test] public void JochebedUsesSecondExodusTitleAndNextUninstalledRoundIsRejected()
    {
        Assert.AreEqual("요게벳",AdamRoundPresentation.Name(11));
        Assert.AreEqual("출애굽기 2 / 요게벳 · 3분 시제품",AdamRoundPresentation.Title(11));
        Assert.AreEqual("출애굽기 1 / 십브라·부아 · 3분 시제품",AdamRoundPresentation.Title(10));
        Assert.AreEqual("창세기 / 유다 · 3분 시제품",AdamRoundPresentation.Title(9));
        Assert.AreEqual("창세기 / 아담 · 3분 시제품",AdamRoundPresentation.Title(1));
        Assert.Throws<InvalidOperationException>(()=>AdamRoundPresentation.Name(61));
        Assert.Throws<InvalidOperationException>(()=>AdamRoundPresentation.Title(61));
        Assert.Throws<InvalidOperationException>(()=>AdamRoundPresentation.Name(0));
    }

    [Test] public void InstalledEndDoesNotAssumeWholeBookCompletion()
    {
        Assert.AreEqual("출애굽기 6 완료 · 진행 저장됨",AdamRoundPresentation.ContentHeading);
        StringAssert.Contains("레위기 미구현",AdamRoundPresentation.ContentDetail);
        StringAssert.Contains("진행 유지",AdamRoundPresentation.ContentDetail);
        StringAssert.Contains("새 도전 선택 시 성장·장비 초기화",AdamRoundPresentation.ContentDetail);
        StringAssert.DoesNotContain("R11 대기",AdamRoundPresentation.ContentDetail);
        StringAssert.DoesNotContain("출애굽기 완료",AdamRoundPresentation.ContentHeading);
        StringAssert.Contains("레위기 미구현",AdamRoundPresentation.ContentDetail);
    }

    [TestCase("ex4","fx_shadow-wide_static_24x6")]
    [TestCase("ex5","fx_shadow-small_static_12x4")]
    [TestCase("ex6","fx_shadow-wide_static_24x6")]
    public void R11EnemiesUseExplicitReviewedShadows(string id,string shadow)
    {
        Assert.IsTrue(AdamRoundPresentation.R11Enemy(id));
        Assert.AreEqual(shadow,AdamRoundPresentation.Shadow(id));
    }

    [Test] public void R11MappingRejectsOtherRoundsAndUnknownEnemiesWithoutFallback()
    {
        foreach(var id in new[]{"ex1","ex2","ex3","i1","silver_cup_accuser","ex7","unknown",null})
            Assert.IsFalse(AdamRoundPresentation.R11Enemy(id));
        Assert.Throws<InvalidOperationException>(()=>AdamRoundPresentation.Shadow("ex19"));
        Assert.Throws<InvalidOperationException>(()=>AdamRoundPresentation.Shadow("unknown"));
        Assert.AreEqual("fx_shadow-wide_static_24x6",AdamRoundPresentation.Shadow("ex1"));
        Assert.AreEqual("fx_shadow-small_static_12x4",AdamRoundPresentation.Shadow("ex2"));
        Assert.AreEqual("fx_shadow-wide_static_24x6",AdamRoundPresentation.Shadow("ex3"));
        Assert.AreEqual("fx_shadow-boss_static_40x8",AdamRoundPresentation.Shadow("silver_cup_accuser"));
        Assert.Throws<ArgumentException>(()=>AdamBossPresentation.Name(11));
    }

    [Test] public void ExhaustedGrowth25AndBossGold100KeepSeparateCards()
    {
        var choice=AdamChoiceText.Describe(AdamSession.GrowthGoldChoicePrefix+"1",new EquipmentInventory("sling"));
        Assert.IsTrue(choice.IsCurrency);
        Assert.AreEqual("영구 골드 +25",choice.Effect);
        StringAssert.DoesNotContain("강화",choice.Card(0));
        StringAssert.DoesNotContain("→",choice.Card(0));
        var boss=AdamBossPresentation.Card(new BossRewardCandidate{Kind="gold",Gold=100},0,null);
        StringAssert.Contains("+100",boss);
        StringAssert.DoesNotContain("+25",boss);
    }

    [Test] public void GenesisEntryAndExplicitNewChallengeDescriptionsRemainAvailable()
    {
        Assert.AreEqual("출애굽기 시작 (Enter)",AdamChoiceText.ExodusStartCard);
        StringAssert.Contains("창세기 완료",AdamRoundPresentation.ChapterHeading);
        StringAssert.Contains("현재 성장·장비로 계속",AdamRoundPresentation.ChapterDetail);
        Assert.AreEqual("1. 새 도전 · 회전 성경",AdamChoiceText.StartCard("bible",0,true));
        Assert.AreEqual("2. 새 도전 · 삼손의 턱뼈",AdamChoiceText.StartCard("jawbone",1,true));
        Assert.AreEqual("3. 새 도전 · 다윗의 물맷돌",AdamChoiceText.StartCard("sling",2,true));
    }
}
