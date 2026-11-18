using System;
using System.IO;
using BibleSurvivor.Core;
using BibleSurvivor.Unity;
using NUnit.Framework;
using UnityEngine;

public sealed class AdamVisualQuality02UiTests
{
    [TestCase(660,440)][TestCase(1100,720)]
    public void StageCardsFitAndVisualMotionDoesNotMoveImmediateHitTargets(float width,float height)
    {
        var arena=AdamGame.Arena(width,height);
        for(var i=0;i<3;i++)
        {
            var hit=AdamStartStage.Card(width,height,i);
            Assert.GreaterOrEqual(hit.xMin,arena.xMin);Assert.LessOrEqual(hit.xMax,arena.xMax);
            Assert.GreaterOrEqual(hit.yMin,arena.yMin);Assert.LessOrEqual(hit.yMax,arena.yMax);
            Assert.IsTrue(hit.Contains(hit.center));
            Assert.AreEqual(hit,AdamStartStage.Card(width,height,i));
            Assert.AreEqual(hit.y-4,AdamStartStage.Visual(hit,true,100,false,i).y);
            Assert.AreEqual(hit.y+4,AdamStartStage.Visual(hit,true,0,true,i).y);
            for(var j=0;j<i;j++)Assert.IsFalse(hit.Overlaps(AdamStartStage.Card(width,height,j)));
        }
    }
    [Test] public void FocusWrapsAndOnlyActualPointerMovementOverridesKeyboardSelection()
    {
        var stage=new AdamStartStage();stage.Move(-1);Assert.AreEqual(2,stage.Focus);
        stage.Move(1);Assert.AreEqual(0,stage.Focus);
        var point=AdamStartStage.Card(660,440,1).center;stage.Hover(point,660,440);Assert.AreEqual(1,stage.Focus);
        stage.Move(1);stage.Hover(point,660,440);Assert.AreEqual(2,stage.Focus);
        stage.Hover(point+Vector2.one,660,440);Assert.AreEqual(1,stage.Focus);
        stage.Reset();Assert.AreEqual(0,stage.Focus);
    }
    [TestCase(660,440)][TestCase(1100,720)]
    public void LargeHealthValueHasItsOwnLaneAndSettingsDoNotCoverResume(float width,float height)
    {
        var value=AdamHud.HealthValue(width,true);var hp=AdamHud.Health(width,true);
        Assert.IsFalse(value.Overlaps(AdamHud.TimerRect(width)));
        Assert.LessOrEqual(value.xMax,hp.xMax-6);Assert.AreEqual(hp.y,value.y);
        Assert.GreaterOrEqual(value.width,198); // 660 v6 approved lane for the real 10000 / 10000 fixture.
        Assert.IsFalse(AdamHud.Experience(width,true).Overlaps(AdamHud.ExperienceValue(width)));
        var panel=AdamHud.PausePanel(width,height);var resume=AdamHud.PauseResume(width,height,false,true);
        for(var i=0;i<4;i++)
        {
            var row=AdamHud.PauseSetting(panel,i);Assert.IsFalse(row.Overlaps(resume));
            Assert.LessOrEqual(row.xMax,panel.xMax);Assert.LessOrEqual(row.yMax,panel.yMax);
            for(var j=0;j<i;j++)Assert.IsFalse(row.Overlaps(AdamHud.PauseSetting(panel,j)));
        }
        Assert.AreEqual(42,AdamHud.BossBacking(width,true).height);
    }
    [Test] public void MissingOptionalBundleKeepsBaselineGeometry()
    {
        var hp=AdamHud.Health(660,false);Assert.AreEqual(40,hp.y);Assert.AreEqual(18,hp.height);
        Assert.AreEqual(new Rect(94,86,546,16),AdamHud.Experience(660,false));
        Assert.AreEqual(new Rect(205,240,250,56),AdamHud.PauseResume(660,440,false,false));
        Assert.AreEqual(64,AdamHud.BossBacking(660,false).height);
    }
    [Test] public void ReducedEffectsHaveNoPresentationDelayPulseOrCountup()
    {
        foreach(var t in new[]{0d,.033,.1,.6,1})
        {
            Assert.Zero(AdamUiMotion.EnterOffset(t,false,2));Assert.Zero(AdamUiMotion.BracketFrame(t,false));
            Assert.AreEqual(.55f,AdamUiMotion.Dim(t,false,true));Assert.AreEqual(500,AdamUiMotion.Gold(500,t,false));
            Assert.AreEqual(1,AdamUiMotion.CompletionRule(t,false));Assert.IsTrue(AdamUiMotion.CautionBright(t,false));
        }
    }
    [Test] public void MotionUsesSteppedUnscaledPresentationTimeAndNeverChangesAppliedGold()
    {
        var motion=new AdamUiMotion();motion.Advance("reward:one",2);Assert.Zero(motion.Elapsed);
        motion.Advance("reward:one",.11);Assert.AreEqual(.11,motion.Elapsed,1e-9);
        Assert.AreEqual(8,AdamUiMotion.EnterOffset(0,true));Assert.AreEqual(4,AdamUiMotion.EnterOffset(1d/30,true));
        Assert.AreEqual(2,AdamUiMotion.EnterOffset(2d/30,true));Assert.Zero(AdamUiMotion.EnterOffset(.1,true));
        Assert.Zero(AdamUiMotion.Gold(500,.1,true));Assert.AreEqual(500,AdamUiMotion.Gold(500,1,true));
        motion.Advance("complete:15",.5);Assert.Zero(motion.Elapsed);
    }
    [TestCase(0)][TestCase(1)][TestCase(2)]
    public void FocusConfirmUsesSelectSlotImmediatelyAndDuplicateNumbersCannotRestart(int slot)
    {
        var folder=Path.Combine(Path.GetTempPath(),"visual02-ui-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(folder);
        var owner=new GameObject("start focus input");owner.SetActive(false);
        try
        {
            var host=new AdamHost(AdamCatalog.Load(),new SaveStore(Path.Combine(folder,"save.json")));
            var game=owner.AddComponent<AdamGame>();game.AutoInitialize=false;game.enabled=false;game.Initialize(host);
            game.SetLifecycle(true,false);Assert.AreEqual(AdamHud.PolishEnabled,game.StartStageAvailable);
            if(!AdamHud.PolishEnabled)
            {
                game.MoveStartFocus(1);game.EnterCommand();Assert.IsNull(host.Session);
                game.SelectSlot(slot);Assert.IsTrue(host.Session.Inventory.Weapons.ContainsKey(AdamGame.Weapons[slot]));return;
            }
            for(var i=0;i<slot;i++)game.MoveStartFocus(1);
            Assert.AreEqual(slot,game.StartFocus);game.EnterCommand();var id=host.State.Run.RunId;
            Assert.IsTrue(host.Session.Inventory.Weapons.ContainsKey(AdamGame.Weapons[slot]));
            game.SelectSlot((slot+1)%3);game.EnterCommand();Assert.AreEqual(id,host.State.Run.RunId);
            Assert.IsFalse(game.StartStageAvailable);
        }
        finally{UnityEngine.Object.DestroyImmediate(owner);Directory.Delete(folder,true);}
    }
}
