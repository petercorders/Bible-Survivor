using System;
using System.IO;
using System.Linq;
using BibleSurvivor.Core;
using BibleSurvivor.Unity;
using NUnit.Framework;
using UnityEngine;
using CoreVector=System.Numerics.Vector2;

public sealed class AdamR15WorldTests
{
    [Test]
    public void EightReviewedClips27FramesKeepRegistrationMaskAndProjectileContracts()
    {
        using(var art=new AdamExodusR06Art())
        {
            Assert.AreEqual(8,art.Index.assets.Length);Assert.AreEqual(27,art.Index.assets.Sum(c=>c.frames));
            var ids=new[]{"ex16","ex17","ex18","defilement_weaver"};var fps=new[]{10,8,6,6};
            for(var i=0;i<ids.Length;i++)
            {
                var clip=art.Clip(AdamArtWorld.EnemyClip(ids[i]));Assert.AreEqual(AdamExodusR06Art.EnemyClip(ids[i]),clip.id);Assert.AreEqual(fps[i],clip.fps);
                Assert.AreEqual(i==3?"fx_shadow-boss_static_40x8":i==0?"fx_shadow-small_static_12x4":"fx_shadow-wide_static_24x6",AdamRoundPresentation.Shadow(ids[i]));
            }
            art.PrepareHitMasks();
            foreach(var clip in art.Index.assets)
            {
                Assert.AreEqual(clip.id+"_"+(clip.frames-1).ToString("D2"),art.Sprite(clip.id,999).name);
                var source=art.Sprite(clip.id,0).texture;var original=source.GetPixels32();var mask=art.HitMask(clip.id);var pixels=mask.GetPixels32();
                var n=Array.FindIndex(original,p=>p.a!=0);Assert.GreaterOrEqual(n,0);Assert.AreEqual(new Color32(255,255,255,original[n].a),pixels[n]);
                art.PrepareHitMasks();Assert.AreSame(mask,art.HitMask(clip.id));CollectionAssert.AreEqual(original,source.GetPixels32());
            }
            var projectile=art.Clip(AdamExodusR06Art.Projectile);Assert.AreEqual(16,projectile.width);Assert.AreEqual(8,projectile.pivotX);Assert.AreEqual(8,projectile.pivotY);Assert.AreEqual(8,projectile.fps);Assert.AreEqual(1,projectile.Frame(.125));Assert.AreEqual(0,projectile.Frame(.25));
            Assert.AreEqual(12,art.Clip(AdamExodusR06Art.BossAttack).fps);Assert.AreEqual(8,art.Clip(AdamExodusR06Art.BossDeath).fps);
            var derived=art.HitMask(AdamExodusR06Art.BossWindup);art.Dispose();Assert.IsTrue(derived==null);
        }
        Assert.Throws<InvalidOperationException>(()=>AdamExodusR06Art.ReadIndex("{}"));
    }
    [TestCase(0,0)][TestCase(1d/3,1)][TestCase(2d/3,2)]
    public void SavedBossClockSelectsThirdsAndShortAttackOnlyAfterFirstEmission(double elapsed,int expected)
    {
        var boss=new BossEncounterState{RoundNumber=15,Telegraphing=true,BossElapsed=4+elapsed,NextAttackAt=5};
        Assert.AreEqual(AdamExodusR06Art.BossWindup,AdamExodusR06Art.BossClip("boss",boss,9999,out var frame,out var seconds));Assert.AreEqual(expected,frame);Assert.AreEqual(elapsed,seconds,.000001);
        boss.Telegraphing=false;boss.BossElapsed=.1;Assert.AreEqual(AdamExodusR06Art.BossMove,AdamExodusR06Art.BossClip("boss",boss,9999,out frame,out seconds));
        boss.BossElapsed=5;boss.NextAttackAt=10;Assert.AreEqual(AdamExodusR06Art.BossAttack,AdamExodusR06Art.BossClip("boss",boss,9999,out frame,out seconds));Assert.Zero(frame);Assert.Zero(seconds);
        boss.BossElapsed=5+1d/12;Assert.AreEqual(AdamExodusR06Art.BossAttack,AdamExodusR06Art.BossClip("boss",boss,9999,out frame,out seconds));Assert.AreEqual(1,frame);
        boss.Paused=true;Assert.AreEqual(AdamExodusR06Art.BossAttack,AdamExodusR06Art.BossClip("boss",boss,19999,out var pausedFrame,out var pausedSeconds));Assert.AreEqual(frame,pausedFrame);Assert.AreEqual(seconds,pausedSeconds);
        boss.BossElapsed=5+1d/6;Assert.AreEqual(AdamExodusR06Art.BossMove,AdamExodusR06Art.BossClip("boss",boss,9999,out frame,out seconds));
        Assert.AreEqual(AdamExodusR06Art.BossMove,AdamExodusR06Art.BossClip("boss_intro",boss,9999,out frame,out seconds));Assert.Zero(frame);
        Assert.IsTrue(AdamArtWorld.BossDeathVisible(20,20.49,boss));Assert.IsFalse(AdamArtWorld.BossDeathVisible(20,20.5,boss));Assert.IsTrue(AdamArtWorld.BossDeathVisible(20,20.6));
    }
    [Test]
    public void ActualCoreLiveThreeDirectionsProjectilesAbsoluteClockPauseAndCleanupUseRules()
    {
        var dir=Path.Combine(Path.GetTempPath(),"adam-r15-world-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(dir);
        try
        {
            var host=AdamR14HostTests.Start(Path.Combine(dir,"save.json"));host.Session.Advance(180);
            var game=host.Session.StartNextRound(ExodusContent.Round(15));game.Advance(180);var speeds=ExodusContent.SpeedsFor(15);
            var world=new BattleWorld(game,speeds,7);world.Advance(2,CoreVector.Zero);var snapshot=world.Capture();snapshot.Enemies.Single(e=>e.Id==game.BossRules.BossActorId).X=30;
            foreach(var id in game.Inventory.Weapons.Keys)snapshot.Weapons.Add(new BattleWeaponState{WeaponId=id,WeaponForm=game.Inventory.Forms.TryGetValue(id,out var form)?form:null,NextCycle=1000});
            world=BattleWorld.Restore(game,speeds,snapshot);world.Advance(4,CoreVector.Zero);snapshot=world.Capture();var boss=game.Boss;var rules=game.BossRules;
            Assert.IsTrue(rules.UsesChainProjectiles);Assert.AreEqual(18,rules.AttackDamage);Assert.AreEqual(15,BossDefinition.ForRound(9).AttackDamage);Assert.AreEqual(3,boss.ChainDirections.Count);
            var live=snapshot.Enemies.Single(e=>e.Id==rules.BossActorId);live.X+=2;world=BattleWorld.Restore(game,speeds,snapshot);snapshot=world.Capture();
            var projection=new AdamWorldProjection(new Rect(20,116,620,246),new Vector2(snapshot.PlayerX,snapshot.PlayerY));
            using(var low=new AdamArtWorld{FeedbackEnabled=false})
            {
                for(var i=0;i<3;i++)
                {
                    Assert.IsTrue(low.TryChainGeometry(snapshot,boss,"boss",i,out var g));Assert.AreEqual(new Vector2(live.X,live.Y),g.Start);
                    var saved=boss.ChainDirections[i];var end=g.Start+new Vector2(saved.X,saved.Y)*(float)(rules.ProjectileSpeed*rules.ProjectileLifetime);
                    Assert.Less((g.End-end).magnitude,.00001);Assert.AreEqual(rules.ProjectileRadius,g.HalfWidth,.000001);Assert.AreEqual(projection.Point(end.x,end.y),projection.Point(g.End.x,g.End.y));
                }
                game.Pause(true);world.Advance(10,CoreVector.UnitY);Assert.AreEqual(snapshot.Time,world.Capture().Time);Assert.AreEqual(boss.BossElapsed,game.Boss.BossElapsed);game.Pause(false);
                world.Advance(1,CoreVector.UnitY);snapshot=world.Capture();Assert.AreEqual(3,snapshot.HostileProjectiles.Count);Assert.IsFalse(low.TryChainGeometry(snapshot,game.Boss,"boss",0,out _));
                world.Advance(.125,CoreVector.Zero);snapshot=world.Capture();var projectile=snapshot.HostileProjectiles[0];var seconds=AdamChainWarning.ProjectileSeconds(snapshot,projectile);
                Assert.AreEqual(snapshot.Time-projectile.BornAt,seconds,.000001);Assert.AreEqual(projection.Point(projectile.X,projectile.Y),AdamChainWarning.ProjectilePoint(projection,projectile));Assert.Greater(projectile.Id,0);
                var before=AdamChainWarning.ProjectilePoint(projection,projectile);game.Pause(true);world.Advance(10,CoreVector.UnitX);var frozen=world.Capture();Assert.AreEqual(snapshot.Time,frozen.Time);Assert.AreEqual(before,AdamChainWarning.ProjectilePoint(projection,frozen.HostileProjectiles[0]));game.Pause(false);
                world=BattleWorld.Restore(game,speeds,world.Capture());Assert.AreEqual(projectile.Id,world.Capture().HostileProjectiles[0].Id);
                game.Hit(rules.BossActorId,rules.MaxHealth);world.Advance(0,CoreVector.Zero);Assert.Zero(world.Capture().HostileProjectiles.Count);Assert.IsFalse(low.TryChainGeometry(world.Capture(),game.Boss,game.Stage,0,out _));Assert.IsFalse(AdamChainWarning.ProjectileVisible(projectile,game.Stage));
            }
        }
        finally{Directory.Delete(dir,true);}
    }
}
