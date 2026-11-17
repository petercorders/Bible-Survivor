using System;
using BibleSurvivor.Core;
using BibleSurvivor.Unity;
using NUnit.Framework;

public sealed class AdamR10UiTests
{
    [Test] public void ExodusTitleUsesLocalRoundAndKeepsGenesisNames()
    {
        Assert.AreEqual("출애굽기 1 / 십브라·부아 · 3분 시제품",AdamRoundPresentation.Title(10));
        Assert.AreEqual("십브라·부아",AdamRoundPresentation.Name(10));
        Assert.AreEqual("창세기 / 유다 · 3분 시제품",AdamRoundPresentation.Title(9));
        Assert.AreEqual("창세기 / 아담 · 3분 시제품",AdamRoundPresentation.Title(1));
        Assert.Throws<InvalidOperationException>(()=>AdamRoundPresentation.Title(16));
        Assert.Throws<InvalidOperationException>(()=>AdamRoundPresentation.Name(0));
        Assert.Throws<ArgumentException>(()=>AdamBossPresentation.Name(10));
    }

    [Test] public void ContinueAndNewChallengeExplainDifferentProgressEffects()
    {
        Assert.AreEqual("출애굽기 시작 (Enter)",AdamChoiceText.ExodusStartCard);
        StringAssert.Contains("진행 유지",AdamRoundPresentation.ChapterDetail);
        StringAssert.Contains("현재 성장·장비로 계속",AdamRoundPresentation.ChapterDetail);
        StringAssert.Contains("새 도전 선택 시 성장·장비 초기화",AdamRoundPresentation.ChapterDetail);
        Assert.AreEqual("1. 새 도전 · 회전 성경",AdamChoiceText.StartCard("bible",0,true));
        Assert.AreEqual("2. 새 도전 · 삼손의 턱뼈",AdamChoiceText.StartCard("jawbone",1,true));
        Assert.AreEqual("3. 새 도전 · 다윗의 물맷돌",AdamChoiceText.StartCard("sling",2,true));
    }

    [Test] public void InstalledRoundEndKeepsBookCompletionSeparate()
    {
        Assert.AreEqual("출애굽기 6 완료 · 진행 저장됨",AdamRoundPresentation.ContentHeading);
        StringAssert.Contains("레위기 미구현",AdamRoundPresentation.ContentDetail);
        StringAssert.Contains("진행 유지",AdamRoundPresentation.ContentDetail);
        StringAssert.DoesNotContain("출애굽기 완료",AdamRoundPresentation.ContentHeading);
        StringAssert.Contains("레위기 미구현",AdamRoundPresentation.ContentDetail);
        var choice=AdamChoiceText.Describe(AdamSession.GrowthGoldChoicePrefix+"1",new EquipmentInventory("sling"));
        Assert.AreEqual("영구 골드 +25",choice.Effect);
    }

    [TestCase(660,440)]
    [TestCase(1100,720)]
    public void ContinueAndAllThreeNewChallengeTargetsFitWithoutOverlap(float width,float height)
    {
        var panel=AdamChoiceText.ChoicePanel(width,height);
        Assert.GreaterOrEqual(panel.xMin,0);Assert.LessOrEqual(panel.xMax,width);
        Assert.GreaterOrEqual(panel.yMin,0);Assert.LessOrEqual(panel.yMax,height);
        for(var i=0;i<4;i++)
        {
            var action=AdamChoiceText.ChapterAction(panel,i);
            Assert.GreaterOrEqual(action.height,44);
            Assert.GreaterOrEqual(action.xMin,panel.xMin);Assert.LessOrEqual(action.xMax,panel.xMax);
            Assert.GreaterOrEqual(action.yMin,panel.y+142);Assert.LessOrEqual(action.yMax,panel.yMax);
            for(var j=0;j<i;j++)Assert.IsFalse(action.Overlaps(AdamChoiceText.ChapterAction(panel,j)));
        }
        Assert.Throws<ArgumentOutOfRangeException>(()=>AdamChoiceText.ChapterAction(panel,4));
    }
}
