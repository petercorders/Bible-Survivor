using System;
using BibleSurvivor.Core;
using BibleSurvivor.Unity;
using NUnit.Framework;

public sealed class AdamR12UiTests
{
    [Test] public void MosesUsesThirdExodusTitleAndR14RemainsUninstalled()
    {
        Assert.AreEqual("모세",AdamRoundPresentation.Name(12));
        Assert.AreEqual("출애굽기 3 / 모세 · 3분 시제품",AdamRoundPresentation.Title(12));
        Assert.AreEqual("출애굽기 2 / 요게벳 · 3분 시제품",AdamRoundPresentation.Title(11));
        Assert.AreEqual("출애굽기 1 / 십브라·부아 · 3분 시제품",AdamRoundPresentation.Title(10));
        Assert.AreEqual("창세기 / 유다 · 3분 시제품",AdamRoundPresentation.Title(9));
        Assert.Throws<InvalidOperationException>(()=>AdamRoundPresentation.Name(14));
        Assert.Throws<InvalidOperationException>(()=>AdamRoundPresentation.Title(14));
    }

    [Test] public void InstalledEndWaitsForR14AndCompletion240WithoutWholeBookClaim()
    {
        Assert.AreEqual("출애굽기 4 완료 · 진행 저장됨",AdamRoundPresentation.ContentHeading);
        Assert.AreEqual(220,ExodusContent.Round(12).CompletionGold);
        StringAssert.Contains("R14 대기",AdamRoundPresentation.ContentDetail);
        StringAssert.Contains("완료 골드 +240",AdamRoundPresentation.ContentDetail);
        StringAssert.Contains("진행 유지",AdamRoundPresentation.ContentDetail);
        StringAssert.Contains("새 도전 선택 시 성장·장비 초기화",AdamRoundPresentation.ContentDetail);
        StringAssert.DoesNotContain("출애굽기 완료",AdamRoundPresentation.ContentHeading);
        StringAssert.DoesNotContain("레위기",AdamRoundPresentation.ContentDetail);
    }

    [TestCase("ex7","fx_shadow-wide_static_24x6")]
    [TestCase("ex8","fx_shadow-wide_static_24x6")]
    [TestCase("ex9","fx_shadow-wide_static_24x6")]
    [TestCase("oppression_chariot","fx_shadow-boss_static_40x8")]
    public void ExplicitR12EnemiesUseReviewedShadows(string id,string shadow)
    {
        Assert.IsTrue(AdamRoundPresentation.R12Enemy(id));
        Assert.AreEqual(shadow,AdamRoundPresentation.Shadow(id));
    }

    [Test] public void R12MappingDoesNotCaptureEarlierOrUnknownEnemies()
    {
        foreach(var id in new[]{"ex4","ex5","ex6","silver_cup_accuser","ex10","unknown",null})
            Assert.IsFalse(AdamRoundPresentation.R12Enemy(id));
        Assert.Throws<InvalidOperationException>(()=>AdamRoundPresentation.Shadow("ex13"));
        Assert.AreEqual("fx_shadow-wide_static_24x6",AdamRoundPresentation.Shadow("ex4"));
        Assert.AreEqual("fx_shadow-small_static_12x4",AdamRoundPresentation.Shadow("ex5"));
        Assert.AreEqual("fx_shadow-wide_static_24x6",AdamRoundPresentation.Shadow("ex6"));
    }

    [Test] public void ChariotNameAndHealthUseTheSelectedBossRules()
    {
        var rules=BossDefinition.ForRound(12);
        Assert.AreEqual("oppression_chariot",rules.Id);
        Assert.AreEqual("압제의 전차장",AdamBossPresentation.Name(rules));
        Assert.AreEqual("압제의 전차장",AdamBossPresentation.Name(12));
        Assert.AreEqual(20000,rules.MaxHealth);
        Assert.AreEqual(.5,AdamBossPresentation.HealthFraction(new BossEncounterState{Health=10000},rules));
        Assert.AreEqual("네피림 폭군",AdamBossPresentation.Name(3));
        Assert.AreEqual("싯나의 우물지기",AdamBossPresentation.Name(6));
        Assert.AreEqual("은잔의 참소자",AdamBossPresentation.Name(9));
        Assert.Throws<ArgumentException>(()=>AdamBossPresentation.Name(13));
    }

    [TestCase("chase","추격")]
    [TestCase("telegraph","돌진 예고 1.2초 · 고정 방향")]
    [TestCase("dash","돌진 진행 · 고정 방향")]
    public void ChariotUsesStoredPhaseAndActualPeriodWithoutSlamOrChains(string phase,string label)
    {
        var rules=BossDefinition.ForRound(12);
        var boss=new BossEncounterState{ChariotCharge=new ChariotChargeState{Phase=phase}};
        var text=AdamBossPresentation.Phase("boss",boss,rules);
        StringAssert.Contains(label,text);
        StringAssert.Contains("6초 주기",text);
        foreach(var word in new[]{"내려치기","사슬","낙하"})StringAssert.DoesNotContain(word,text);
        Assert.AreEqual("등장 · 피해 보호",AdamBossPresentation.Phase("boss_intro",boss,rules));
        boss.ThresholdUsed=true;
        StringAssert.Contains("지원군",AdamBossPresentation.Phase("boss",boss,rules));
    }

    [Test] public void EarlierBossPhasesKeepTheirOwnAttackLabels()
    {
        Assert.AreEqual("내려치기 예고",AdamBossPresentation.AttackPhase(3,true,false,6));
        Assert.AreEqual("흙더미 낙하 예고",AdamBossPresentation.AttackPhase(6,true,false,7));
        Assert.AreEqual("사슬 3방향 예고",AdamBossPresentation.AttackPhase(9,true,false,5));
        StringAssert.Contains("돌진 예고",AdamBossPresentation.AttackPhase(12,true,false,6));
    }

    [Test] public void FixedBoss400Fallback100Growth25AndInstalledCompletion240StayDistinct()
    {
        var fixedReward=AdamBossPresentation.RewardDetail(BossDefinition.ForRound(12));
        StringAssert.Contains("압제의 전차장",fixedReward);
        StringAssert.Contains("고정 골드 +400 적용됨",fixedReward);
        StringAssert.Contains("선택 즉시 저장",fixedReward);
        var fallback=AdamBossPresentation.Card(new BossRewardCandidate{Kind="gold",Gold=100},0,null);
        StringAssert.Contains("+100",fallback);
        StringAssert.DoesNotContain("+400",fallback);
        var growth=AdamChoiceText.Describe(AdamSession.GrowthGoldChoicePrefix+"1",new EquipmentInventory("sling"));
        Assert.AreEqual("영구 골드 +25",growth.Effect);
        StringAssert.DoesNotContain("강화",growth.Card(0));
        StringAssert.DoesNotContain("→",growth.Card(0));
        Assert.AreEqual(220,ExodusContent.Round(12).CompletionGold);
        StringAssert.Contains("완료 골드 +240",AdamRoundPresentation.ContentDetail);
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
