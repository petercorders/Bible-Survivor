using System;
using System.Collections.Generic;
using System.Linq;
using BibleSurvivor.Core;
using BibleSurvivor.Unity;
using NUnit.Framework;
using UnityEngine;

public sealed class AdamR09WorldTests
{
    static BossEncounterState Warning()
    {
        var boss=new BossEncounterState{RoundNumber=9,Telegraphing=true,BossElapsed=4.5,NextAttackAt=5,ChainDirections=new List<ChainDirectionState>()};
        for(var i=-1;i<=1;i++)
        {
            var a=(67+i*BossDefinition.ChainAngleDegrees)*Math.PI/180;
            boss.ChainDirections.Add(new ChainDirectionState{X=(float)Math.Cos(a),Y=(float)Math.Sin(a)});
        }
        return boss;
    }
    [TestCase(false)][TestCase(true)]
    public void ThreeSavedDirectionsUseLiveActorAndCoreDistanceInLowEffectsAndPause(bool paused)
    {
        var boss=Warning();boss.Paused=paused;
        var snap=new BattleSnapshot{Time=186.5,PlayerX=-4,PlayerY=8,Enemies=new List<BattleEntityState>{new BattleEntityState{Id=boss.Rules.BossActorId,X=3,Y=-2}}};
        var projection=new AdamWorldProjection(new Rect(20,116,1060,526),new Vector2(-4,8));
        using(var world=new AdamArtWorld{FeedbackEnabled=false})
        {
            for(var i=0;i<3;i++)
            {
                Assert.IsTrue(world.TryChainGeometry(snap,boss,"boss",i,out var g));
                Assert.AreEqual(new Vector2(3,-2),g.Start);
                var d=boss.ChainDirections[i];var expected=g.Start+new Vector2(d.X,d.Y)*(float)(BossDefinition.ChainSpeed*BossDefinition.ChainLifetime);
                Assert.Less((g.End-expected).magnitude,.00001);Assert.AreEqual((float)BossDefinition.ChainRadius,g.HalfWidth);
                Assert.AreEqual(projection.Point(expected.x,expected.y),projection.Point(g.End.x,g.End.y));
                Assert.IsTrue(world.TryChainGeometry(snap,boss,"boss",i,out var repeated));Assert.AreEqual(g.End,repeated.End);
            }
            // Core knockback/live position changes the origin; saved directions remain unchanged.
            snap.Enemies[0].X+=2;Assert.IsTrue(world.TryChainGeometry(snap,boss,"boss",1,out var shifted));Assert.AreEqual(5,shifted.Start.x);
            boss.Telegraphing=false;Assert.IsFalse(world.TryChainGeometry(snap,boss,"boss",0,out _));
            boss.Telegraphing=true;Assert.IsFalse(world.TryChainGeometry(snap,boss,"boss_intro",0,out _));
            Assert.IsFalse(world.TryChainGeometry(snap,boss,"boss_reward",0,out _));
            snap.Enemies.Clear();Assert.IsFalse(world.TryChainGeometry(snap,boss,"boss",0,out _));
        }
        Assert.AreEqual(186.5,snap.Time);Assert.AreEqual(4.5,boss.BossElapsed);
    }
    [Test]
    public void ActualProjectilePositionAndAbsoluteClockStayFixedUntilCoreChangesAndCleanupHidesIt()
    {
        var p=new HostileProjectileState{Id=7,X=7.25f,Y=-3.125f,StartX=-40,StartY=99,DirectionX=.6f,DirectionY=.8f,BornAt=187,Lifetime=4.5};
        var snap=new BattleSnapshot{Time=188.5};var projection=new AdamWorldProjection(new Rect(20,116,620,246),new Vector2(1,2));
        Assert.IsTrue(AdamChainWarning.ProjectileVisible(p,"boss"));
        var actual=AdamChainWarning.ProjectilePoint(projection,p);Assert.AreEqual(projection.Point(p.X,p.Y),actual);
        Assert.AreEqual(1.5,AdamChainWarning.ProjectileSeconds(snap,p));
        p.DirectionX=-.6f;var anchor=actual+projection.Arena.position;var rotation=AdamChainWarning.ProjectileMatrix(anchor,p);
        Assert.Less(((Vector2)rotation.MultiplyPoint3x4(anchor)-anchor).magnitude,.0001);
        var heading=(Vector2)rotation.MultiplyVector(Vector2.right);Assert.Less((heading-new Vector2(-.6f,-.8f)).magnitude,.0001);
        Assert.AreEqual(1,rotation.determinant,.0001); // One rotation; no inherited actor reflection.
        // Presentation never extrapolates from start/direction or elapsed wall time during pause.
        Assert.AreEqual(actual,AdamChainWarning.ProjectilePoint(projection,p));Assert.AreEqual(188.5,snap.Time);
        Assert.IsFalse(AdamChainWarning.ProjectileVisible(p,"boss_reward"));p.Lifetime=0;Assert.IsFalse(AdamChainWarning.ProjectileVisible(p,"boss"));
    }
    [Test]
    public void EightReviewedSheetsAnd27FramesKeepContractFpsPivotsAndBossClock()
    {
        var art=new AdamR09Art();Assert.AreEqual(8,art.Index.assets.Length);Assert.AreEqual(27,art.Index.assets.Sum(c=>c.frames));
        foreach(var id in new[]{"i1","i2","i3","silver_cup_accuser"})Assert.AreEqual(AdamR09Art.EnemyClip(id),AdamArtWorld.EnemyClip(id));
        CollectionAssert.AreEqual(new[]{10f,8f,5f},new[]{"i1","i2","i3"}.Select(id=>art.Clip(AdamR09Art.EnemyClip(id)).fps));
        var projectile=art.Clip(AdamR09Art.Projectile);Assert.AreEqual(8,projectile.fps);Assert.AreEqual(8,projectile.pivotX);Assert.AreEqual(8,projectile.pivotY);Assert.AreEqual(1,projectile.Frame(.125));Assert.AreEqual(0,projectile.Frame(.25));
        var boss=Warning();Assert.AreEqual(AdamR09Art.BossWindup,AdamArtWorld.BossClip("boss",boss,-1,186.5,out var frame));Assert.AreEqual(1,frame);
        Assert.AreEqual(AdamR09Art.BossMove,AdamArtWorld.BossClip("boss_intro",boss,-1,182,out frame));Assert.Zero(frame);
        boss.Telegraphing=false;Assert.AreEqual(AdamR09Art.BossImpact,AdamArtWorld.BossClip("boss",boss,187,187.1,out frame));Assert.AreEqual(1,frame);
        Assert.AreEqual(6,art.Clip(AdamR09Art.BossDeath).fps);Assert.IsFalse(art.Clip(AdamR09Art.BossDeath).loop);
        Assert.IsTrue(AdamArtWorld.BossDeathVisible(190,190.6));Assert.IsFalse(AdamArtWorld.BossDeathVisible(190,190+4d/6));
        Assert.Throws<InvalidOperationException>(()=>AdamR09Art.ReadIndex("{}"));
    }
}
