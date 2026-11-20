using System;
using BibleSurvivor.Core;
using BibleSurvivor.Unity;
using NUnit.Framework;
public sealed class AdamR09UiTests
{
    [Test] public void JudahNamesAndExplicitShadowMappingsKeepEarlierRounds()
    {
        Assert.AreEqual("유다",AdamRoundPresentation.Name(9));Assert.AreEqual("요셉",AdamRoundPresentation.Name(8));Assert.Throws<InvalidOperationException>(()=>AdamRoundPresentation.Name(51));
        foreach(var id in new[]{"i1","i2","i3","silver_cup_accuser"})Assert.IsTrue(AdamRoundPresentation.R09Enemy(id));
        Assert.IsFalse(AdamRoundPresentation.R09Enemy("h1"));Assert.IsFalse(AdamRoundPresentation.R09Enemy("unknown"));
        Assert.AreEqual("fx_shadow-small_static_12x4",AdamRoundPresentation.Shadow("i1"));Assert.AreEqual("fx_shadow-small_static_12x4",AdamRoundPresentation.Shadow("i2"));Assert.AreEqual("fx_shadow-wide_static_24x6",AdamRoundPresentation.Shadow("i3"));Assert.AreEqual("fx_shadow-boss_static_40x8",AdamRoundPresentation.Shadow("silver_cup_accuser"));
        Assert.AreEqual("fx_shadow-boss_static_40x8",AdamRoundPresentation.Shadow("sitnah_keeper"));Assert.Throws<InvalidOperationException>(()=>AdamRoundPresentation.Shadow("unknown"));
    }
    [Test] public void ChainBossUsesItsActualHealthAndPeriodWithoutSlamLabels()
    {
        var rules=BossDefinition.ForRound(9);Assert.AreEqual("은잔의 참소자",AdamBossPresentation.Name(rules.Number));Assert.AreEqual(16000,rules.MaxHealth);Assert.AreEqual(5,rules.AttackPeriod);
        Assert.AreEqual(.5,AdamBossPresentation.HealthFraction(new BossEncounterState{Health=8000},rules));
        Assert.AreEqual("사슬 3방향 예고",AdamBossPresentation.AttackPhase(9,true,false,rules.AttackPeriod));Assert.AreEqual("사슬 5초 주기",AdamBossPresentation.AttackPhase(9,false,false,rules.AttackPeriod));
        Assert.IsTrue(AdamBossPresentation.AttackPhase(9,false,true,rules.AttackPeriod).Contains("지원군"));
        Assert.AreEqual("네피림 폭군",AdamBossPresentation.Name(3));Assert.AreEqual("싯나의 우물지기",AdamBossPresentation.Name(6));Assert.AreEqual("내려치기 예고",AdamBossPresentation.AttackPhase(3,true,false,6));Assert.AreEqual("흙더미 낙하 예고",AdamBossPresentation.AttackPhase(6,true,false,7));Assert.Throws<ArgumentException>(()=>AdamBossPresentation.Name(10));
    }
    [Test] public void ChapterCompletionExplainsPreservedProgressAndExplicitNewChallenge()
    {
        Assert.IsTrue(AdamRoundPresentation.ChapterHeading.Contains("창세기 완료"));Assert.IsTrue(AdamRoundPresentation.ChapterHeading.Contains("저장"));Assert.IsTrue(AdamRoundPresentation.ChapterDetail.Contains("출애굽기"));Assert.IsTrue(AdamRoundPresentation.ChapterDetail.Contains("현재 성장·장비로 계속"));Assert.IsTrue(AdamRoundPresentation.ContentDetail.Contains("레위기 미구현"));Assert.IsTrue(AdamRoundPresentation.ChapterDetail.Contains("진행 유지"));
        Assert.AreEqual("1. 새 도전 · 회전 성경",AdamChoiceText.StartCard("bible",0,true));Assert.AreEqual("2. 삼손의 턱뼈",AdamChoiceText.StartCard("jawbone",1));Assert.AreEqual("3. 새 도전 · 다윗의 물맷돌",AdamChoiceText.StartCard("sling",2,true));
    }
    [Test] public void ExhaustedGrowth25AndBossGold100RemainDistinct()
    {
        var card=AdamChoiceText.Describe(AdamSession.GrowthGoldChoicePrefix+"1",new EquipmentInventory("bible"));Assert.IsTrue(card.IsCurrency);Assert.AreEqual("영구 골드 +25",card.Effect);Assert.IsFalse(card.Card(0).Contains("강화"));Assert.IsFalse(card.Card(0).Contains("→"));
        var reward=AdamBossPresentation.Card(new BossRewardCandidate{Kind="gold",Gold=100},0,null);Assert.IsTrue(reward.Contains("+100"));Assert.IsFalse(reward.Contains("+25"));
    }
}
