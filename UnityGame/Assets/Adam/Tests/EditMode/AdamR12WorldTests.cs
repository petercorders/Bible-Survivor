using System;
using System.IO;
using System.Linq;
using BibleSurvivor.Core;
using BibleSurvivor.Unity;
using NUnit.Framework;
using UnityEngine;
using CoreVector=System.Numerics.Vector2;

public sealed class AdamR12WorldTests
{
    [Test]
    public void NineClips29FramesRespectShortPoseClampsMaskRegistrationAndSharedShadows()
    {
        using(var art=new AdamExodusR03Art())
        {
            Assert.AreEqual(9,art.Index.assets.Length);Assert.AreEqual(29,art.Index.assets.Sum(c=>c.frames));art.PrepareHitMasks();
            foreach(var clip in art.Index.assets)
            {
                var last=art.Sprite(clip.id,999);Assert.AreEqual(clip.id+"_"+(clip.frames-1).ToString("D2"),last.name);
                Assert.Less(clip.Frame(999),clip.frames);
                var source=last.texture;var before=source.GetPixels32();var mask=art.HitMask(clip.id);
                Assert.AreNotSame(source,mask);Assert.AreEqual(source.width,mask.width);Assert.AreEqual(source.height,mask.height);
                var pixels=mask.GetPixels32();var n=Array.FindIndex(before,p=>p.a!=0);Assert.GreaterOrEqual(n,0);Assert.AreEqual(new Color32(255,255,255,before[n].a),pixels[n]);
                art.PrepareHitMasks();Assert.AreSame(mask,art.HitMask(clip.id));CollectionAssert.AreEqual(before,source.GetPixels32());
                Assert.AreEqual(clip.width==64?32:16,clip.pivotX);Assert.AreEqual(1,clip.pivotY);
            }
            foreach(var id in new[]{"ex7","ex8","ex9","oppression_chariot"})
            {Assert.AreEqual(AdamExodusR03Art.EnemyClip(id),AdamArtWorld.EnemyClip(id));Assert.AreEqual(id=="oppression_chariot"?"fx_shadow-boss_static_40x8":"fx_shadow-wide_static_24x6",AdamRoundPresentation.Shadow(id));}
            Assert.AreEqual(12,art.Clip(AdamExodusR03Art.Ex9Dash).fps);Assert.AreEqual(12,art.Clip(AdamExodusR03Art.BossDash).fps);
            Assert.AreEqual(0,art.Clip(AdamExodusR03Art.Ex9Windup).fps);Assert.AreEqual(0,art.Clip(AdamExodusR03Art.BossWindup).fps);
            var texture=art.HitMask(AdamExodusR03Art.BossWindup);art.Dispose();Assert.IsTrue(texture==null);Assert.IsFalse(art.Sprite(AdamExodusR03Art.BossWindup,0).texture==null);
        }
        Assert.IsFalse(AdamArtWorld.BossDeathVisible(20,20+4d/6));
    }
    [TestCase(.599,0)][TestCase(.6,1)][TestCase(1.199,1)]
    public void NormalWindupReadsWorldUntilAndGeometryUsesLivePositionAndActualTuning(double elapsed,int expected)
    {
        var charge=new EnemyChargeState{Id=9,Phase="telegraph",Until=71.2,DirectionX=-.6f,DirectionY=.8f};
        var snap=new BattleSnapshot{Time=70+elapsed,Tuning=new BattleTuning{PlayerSpeed=4,PlayerRadius=.4,EnemyRadius=.3}};
        snap.Enemies.Add(new BattleEntityState{Id=9,X=7,Y=-4});
        Assert.AreEqual(AdamExodusR03Art.Ex9Windup,AdamChariotWarning.NormalClip(charge,snap.Time,out var frame,out var seconds));Assert.AreEqual(expected,frame);Assert.AreEqual(elapsed,seconds,.000001);
        using(var low=new AdamArtWorld{FeedbackEnabled=false})
        {
            Assert.IsTrue(AdamChariotWarning.TryNormalGeometry(snap,charge,out var g));Assert.AreEqual(new Vector2(7,-4),g.Start);
            var distance=snap.Tuning.PlayerSpeed*BattleWorld.ChargeSpeed*BattleWorld.ChargeDuration;Assert.AreEqual(distance,(g.End-g.Start).magnitude,.00001);Assert.AreEqual(.7,g.HalfWidth,.000001);
            var projection=new AdamWorldProjection(new Rect(20,116,620,246),new Vector2(2,3));Assert.AreEqual(projection.Point(7,-4),projection.Point(g.Start.x,g.Start.y));
            Assert.IsTrue(AdamChariotWarning.TryNormalGeometry(snap,charge,out var frozen));Assert.AreEqual(g.End,frozen.End);
            charge.Phase="charge";snap.Time=71.5;charge.Until=71.8;Assert.IsTrue(AdamChariotWarning.TryNormalGeometry(snap,charge,out g));Assert.AreEqual(distance/2,(g.End-g.Start).magnitude,.00001);
            Assert.AreEqual(AdamExodusR03Art.Ex9Dash,AdamChariotWarning.NormalClip(charge,snap.Time,out frame,out seconds));Assert.AreEqual(.3,seconds,.000001);
            snap.Enemies.Clear();Assert.IsFalse(AdamChariotWarning.TryNormalGeometry(snap,charge,out _));
        }
    }
    [Test]
    public void ActualCoreBossViewUsesItsOwnClockAndRemainingDistanceDuringPauseRestoreAndKillCleanup()
    {
        var dir=Path.Combine(Path.GetTempPath(),"adam-r12-world-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(dir);
        try
        {
            // Reuse existing completed-R10/R11 fixture; no new gameplay/save implementation in the view.
            var host=AdamR11HostTests.Start(Path.Combine(dir,"save.json"));host.Session.Advance(180);
            var game=host.Session.StartNextRound(ExodusContent.Round(12));game.Advance(180);
            var speeds=ExodusContent.SpeedsFor(12);var world=new BattleWorld(game,speeds,7,new BattleTuning{PlayerSpeed=4,PlayerRadius=.4});world.Advance(2,CoreVector.Zero);
            // Place the actor outside weapons/contact so only the real Core attack clock changes the view.
            var snapshot=world.Capture();snapshot.Enemies.Single(e=>e.Id==game.BossRules.BossActorId).X=30;
            foreach(var id in game.Inventory.Weapons.Keys)snapshot.Weapons.Add(new BattleWeaponState{WeaponId=id,WeaponForm=game.Inventory.Forms.TryGetValue(id,out var form)?form:null,NextCycle=1000});
            world=BattleWorld.Restore(game,speeds,snapshot);world.Advance(4.8,CoreVector.Zero);
            var view=world.ChariotDash;Assert.IsNotNull(view);Assert.AreEqual("telegraph",view.Phase);
            Assert.IsTrue(AdamChariotWarning.TryBossGeometry(view,out var g));Assert.AreEqual(view.RemainingDistance,(g.End-g.Start).magnitude,.00001);Assert.AreEqual(1,g.HalfWidth,.000001);
            var locked=view.Direction;var origin=g.Start;
            var expectedFrame=0;
            foreach(var advance in new[]{0d,.4,.4})
            {
                world.Advance(advance,CoreVector.Zero);view=world.ChariotDash;
                Assert.AreEqual(AdamExodusR03Art.BossWindup,AdamChariotWarning.BossClip("boss",view,world.SimulationTime,out var frame,out _));
                Assert.AreEqual(expectedFrame++,frame);
            }
            game.Pause(true);var frozen=world.ChariotDash;world.Advance(10,CoreVector.UnitY);Assert.AreEqual(frozen.BossElapsed,world.ChariotDash.BossElapsed);Assert.AreEqual(frozen.Deadline,world.ChariotDash.Deadline);game.Pause(false);
            world=BattleWorld.Restore(game,speeds,world.Capture());Assert.AreEqual(locked,world.ChariotDash.Direction);world.Advance(.4,CoreVector.UnitY);
            Assert.AreEqual(AdamExodusR03Art.BossDash,AdamChariotWarning.BossClip("boss",world.ChariotDash,world.SimulationTime,out _,out var seconds));Assert.AreEqual(0,seconds,.00001);
            world.Advance(.3,CoreVector.UnitY);view=world.ChariotDash;Assert.AreEqual(locked,view.Direction);Assert.IsTrue(AdamChariotWarning.TryBossGeometry(view,out g));Assert.AreEqual(view.RemainingDistance,(g.End-g.Start).magnitude,.00001);Assert.AreNotEqual(origin,g.Start);
            Assert.AreEqual(new Vector2(view.Origin.X,view.Origin.Y),g.Start);
            game.Hit(game.BossRules.BossActorId,game.BossRules.MaxHealth);Assert.IsNull(world.ChariotDash);Assert.IsFalse(AdamChariotWarning.TryBossGeometry(world.ChariotDash,out _));
        }
        finally{Directory.Delete(dir,true);}
        Assert.AreEqual(0,AdamChariotWarning.WindupFrame(.399,1.2,3));Assert.AreEqual(1,AdamChariotWarning.WindupFrame(.4,1.2,3));Assert.AreEqual(1,AdamChariotWarning.WindupFrame(.799,1.2,3));Assert.AreEqual(2,AdamChariotWarning.WindupFrame(.8,1.2,3));
    }
}
