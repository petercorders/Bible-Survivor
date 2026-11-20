using System;
using BibleSurvivor.Core;
using BibleSurvivor.Unity;
using NUnit.Framework;

public sealed class AdamR13UiTests
{
    [Test] public void MiriamUsesFourthExodusTitleAndR51RemainsUninstalled()
    {
        Assert.AreEqual("미리암",AdamRoundPresentation.Name(13));
        Assert.AreEqual("출애굽기 4 / 미리암 · 3분 시제품",AdamRoundPresentation.Title(13));
        Assert.AreEqual("출애굽기 3 / 모세 · 3분 시제품",AdamRoundPresentation.Title(12));
        Assert.AreEqual("출애굽기 2 / 요게벳 · 3분 시제품",AdamRoundPresentation.Title(11));
        Assert.AreEqual("창세기 / 유다 · 3분 시제품",AdamRoundPresentation.Title(9));
        Assert.Throws<InvalidOperationException>(()=>AdamRoundPresentation.Name(51));
        Assert.Throws<InvalidOperationException>(()=>AdamRoundPresentation.Title(51));
        Assert.Throws<ArgumentException>(()=>AdamBossPresentation.Name(13));
    }

    [Test] public void InstalledEndShowsCompletion280WithoutWholeBookClaim()
    {
        Assert.AreEqual("출애굽기 6 완료 · 진행 저장됨",AdamRoundPresentation.ContentHeading);
        Assert.AreEqual(240,ExodusContent.Round(13).CompletionGold);
        StringAssert.Contains("완료 골드 +280",AdamRoundPresentation.ContentDetail);
        StringAssert.Contains("레위기 미구현",AdamRoundPresentation.ContentDetail);
        StringAssert.Contains("다음 책 준비 중",AdamRoundPresentation.ContentDetail);
        StringAssert.Contains("진행 유지",AdamRoundPresentation.ContentDetail);
        StringAssert.Contains("새 도전 선택 시 성장·장비 초기화",AdamRoundPresentation.ContentDetail);
        StringAssert.DoesNotContain("출애굽기 완료",AdamRoundPresentation.ContentHeading);
        StringAssert.DoesNotContain("홍해 건넘",AdamRoundPresentation.ContentDetail);
        StringAssert.Contains("레위기 미구현",AdamRoundPresentation.ContentDetail);
    }

    [TestCase("ex10","fx_shadow-small_static_12x4")]
    [TestCase("ex11","fx_shadow-wide_static_24x6")]
    [TestCase("ex12","fx_shadow-wide_static_24x6")]
    public void R13EnemiesUseContractShadows(string id,string shadow)
    {
        Assert.IsTrue(AdamRoundPresentation.R13Enemy(id));
        Assert.AreEqual(shadow,AdamRoundPresentation.Shadow(id));
    }

    [Test] public void R13MappingRejectsOtherRoundsBossesAndUnknownIds()
    {
        foreach(var id in new[]{"ex7","ex8","ex9","oppression_chariot","ex13","unknown",null})
            Assert.IsFalse(AdamRoundPresentation.R13Enemy(id));
        Assert.Throws<InvalidOperationException>(()=>AdamRoundPresentation.Shadow("ex19"));
        Assert.Throws<InvalidOperationException>(()=>AdamRoundPresentation.Shadow("unknown"));
        Assert.AreEqual("fx_shadow-wide_static_24x6",AdamRoundPresentation.Shadow("ex9"));
        Assert.AreEqual("fx_shadow-boss_static_40x8",AdamRoundPresentation.Shadow("oppression_chariot"));
    }

    [Test] public void ExhaustedGrowth25AndEarlierBoss400AndFallback100RemainDistinct()
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
