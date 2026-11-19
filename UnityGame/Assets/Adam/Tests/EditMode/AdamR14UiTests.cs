using System;
using BibleSurvivor.Core;
using BibleSurvivor.Unity;
using NUnit.Framework;

public sealed class AdamR14UiTests
{
    [Test] public void AaronUsesFifthExodusTitleAndR21RemainsUninstalled()
    {
        Assert.AreEqual("아론",AdamRoundPresentation.Name(14));
        Assert.AreEqual("출애굽기 5 / 아론 · 3분 시제품",AdamRoundPresentation.Title(14));
        Assert.AreEqual("출애굽기 4 / 미리암 · 3분 시제품",AdamRoundPresentation.Title(13));
        Assert.AreEqual("출애굽기 3 / 모세 · 3분 시제품",AdamRoundPresentation.Title(12));
        Assert.AreEqual("창세기 / 유다 · 3분 시제품",AdamRoundPresentation.Title(9));
        Assert.Throws<InvalidOperationException>(()=>AdamRoundPresentation.Name(21));
        Assert.Throws<InvalidOperationException>(()=>AdamRoundPresentation.Title(21));
        Assert.Throws<ArgumentException>(()=>AdamBossPresentation.Name(14));
    }

    [Test] public void InstalledEndShowsCompletion280WithoutWholeBookClaim()
    {
        Assert.AreEqual("출애굽기 6 완료 · 진행 저장됨",AdamRoundPresentation.ContentHeading);
        Assert.AreEqual(240,ExodusContent.Round(13).CompletionGold);
        Assert.AreEqual(260,ExodusContent.Round(14).CompletionGold);
        StringAssert.Contains("완료 골드 +280",AdamRoundPresentation.ContentDetail);
        StringAssert.Contains("레위기 미구현",AdamRoundPresentation.ContentDetail);
        StringAssert.Contains("다음 책 준비 중",AdamRoundPresentation.ContentDetail);
        StringAssert.Contains("진행 유지",AdamRoundPresentation.ContentDetail);
        StringAssert.Contains("새 도전 선택 시 성장·장비 초기화",AdamRoundPresentation.ContentDetail);
        StringAssert.DoesNotContain("출애굽기 완료",AdamRoundPresentation.ContentHeading);
        StringAssert.Contains("레위기 미구현",AdamRoundPresentation.ContentDetail);
    }

    [TestCase("ex13","fx_shadow-small_static_12x4")]
    [TestCase("ex14","fx_shadow-wide_static_24x6")]
    [TestCase("ex15","fx_shadow-wide_static_24x6")]
    public void R14EnemiesUseContractShadows(string id,string shadow)
    {
        Assert.IsTrue(AdamRoundPresentation.R14Enemy(id));
        Assert.AreEqual(shadow,AdamRoundPresentation.Shadow(id));
    }

    [Test] public void R14MappingRejectsOtherRoundsBossesAndUnknownIds()
    {
        foreach(var id in new[]{"ex10","ex11","ex12","oppression_chariot","ex16","unknown",null})
            Assert.IsFalse(AdamRoundPresentation.R14Enemy(id));
        Assert.Throws<InvalidOperationException>(()=>AdamRoundPresentation.Shadow("ex19"));
        Assert.Throws<InvalidOperationException>(()=>AdamRoundPresentation.Shadow("unknown"));
        Assert.AreEqual("fx_shadow-small_static_12x4",AdamRoundPresentation.Shadow("ex10"));
        Assert.AreEqual("fx_shadow-wide_static_24x6",AdamRoundPresentation.Shadow("ex11"));
        Assert.AreEqual("fx_shadow-wide_static_24x6",AdamRoundPresentation.Shadow("ex12"));
        Assert.AreEqual("fx_shadow-boss_static_40x8",AdamRoundPresentation.Shadow("oppression_chariot"));
    }

    [Test] public void Growth25AndEarlierBoss400AndFallback100RemainDistinct()
    {
        var growth=AdamChoiceText.Describe(AdamSession.GrowthGoldChoicePrefix+"1",new EquipmentInventory("sling"));
        Assert.AreEqual("영구 골드 +25",growth.Effect);
        StringAssert.DoesNotContain("강화",growth.Card(0));
        StringAssert.DoesNotContain("→",growth.Card(0));
        StringAssert.Contains("고정 골드 +400 적용됨",AdamBossPresentation.RewardDetail(BossDefinition.ForRound(12)));
        var fallback=AdamBossPresentation.Card(new BossRewardCandidate{Kind="gold",Gold=100},0,null);
        StringAssert.Contains("+100",fallback);
        StringAssert.DoesNotContain("+400",fallback);
    }

    [Test] public void ExplicitNewChallengeAndGenesisEntryKeepTheirDescriptions()
    {
        Assert.AreEqual("출애굽기 시작 (Enter)",AdamChoiceText.ExodusStartCard);
        StringAssert.Contains("현재 성장·장비로 계속",AdamRoundPresentation.ChapterDetail);
        Assert.AreEqual("1. 새 도전 · 회전 성경",AdamChoiceText.StartCard("bible",0,true));
        Assert.AreEqual("2. 새 도전 · 삼손의 턱뼈",AdamChoiceText.StartCard("jawbone",1,true));
        Assert.AreEqual("3. 새 도전 · 다윗의 물맷돌",AdamChoiceText.StartCard("sling",2,true));
    }
}
