using System;
using System.Collections.Generic;
using System.Linq;
using BibleSurvivor.Core;
using BibleSurvivor.Unity;
using NUnit.Framework;
using UnityEngine;

public sealed class AdamR20WorldTests
{
    [Test]
    public void LeviticusActorsKeepDistinctReviewedRoleClipsAndShadows()
    {
        var roles = new[] { "lv1", "lv2", "lv3", "ash_veil" };
        var reviewedActors = new[] { "ex17", "ex16", "ex18", "defilement_weaver" };
        var shadows = new[] { "fx_shadow-wide_static_24x6", "fx_shadow-small_static_12x4", "fx_shadow-wide_static_24x6", "fx_shadow-boss_static_40x8" };

        using (var rig = new AdamExodusR06Art())
        {
            var art = new AdamArt();
            var bossArt = new AdamR03Art();
            for (var i = 0; i < roles.Length; i++)
            {
                var clip = rig.Clip(AdamArtWorld.EnemyClip(roles[i]));
                Assert.AreEqual(AdamExodusR06Art.EnemyClip(reviewedActors[i]), clip.id);
                Assert.AreEqual(4, clip.frames);
                Assert.AreEqual(shadows[i], AdamRoundPresentation.Shadow(roles[i]));
                if (i == 3) Assert.NotNull(bossArt.Clip(shadows[i]));
                else Assert.NotNull(art.Sprite(shadows[i], 0));
            }
            Assert.Greater(rig.Clip(AdamArtWorld.EnemyClip("lv2")).fps, rig.Clip(AdamArtWorld.EnemyClip("lv1")).fps);
            Assert.Greater(rig.Clip(AdamArtWorld.EnemyClip("lv1")).fps, rig.Clip(AdamArtWorld.EnemyClip("lv3")).fps);
            Assert.NotNull(rig.Sprite(AdamExodusR06Art.Projectile, 0));
        }

        for (var round = LeviticusContent.GlobalRoundNumber; round <= LeviticusContent.LastInstalledRound; round++)
        {
            var groups = LeviticusContent.Round(round).Waves.SelectMany(wave => wave.Groups);
            CollectionAssert.AreEquivalent(roles.Take(3), groups.Select(group => group.MonsterId).Distinct());
        }
        Assert.Throws<InvalidOperationException>(() => AdamArtWorld.EnemyClip("lv4"));
    }

    [Test]
    public void AshVeilShowsThreeSavedDirectionsWithLowEffectsAndUsesReviewedBossClock()
    {
        var rules = BossDefinition.ForRound(18);
        var boss = new BossEncounterState
        {
            RoundNumber = 18,
            Paused = true,
            Telegraphing = true,
            NextAttackAt = rules.AttackPeriod,
            BossElapsed = rules.AttackPeriod - rules.TelegraphDuration / 2,
            ChainDirections = new List<ChainDirectionState>()
        };
        for (var offset = -1; offset <= 1; offset++)
        {
            var angle = (37 + offset * rules.ProjectileSpreadDegrees) * Math.PI / 180;
            boss.ChainDirections.Add(new ChainDirectionState { X = (float)Math.Cos(angle), Y = (float)Math.Sin(angle) });
        }
        var actor = new BattleEntityState { Id = rules.BossActorId, X = 4, Y = -3 };
        var snapshot = new BattleSnapshot { Time = 1000, Enemies = new List<BattleEntityState> { actor } };
        Assert.AreEqual("ash_veil", rules.Id);
        using (var world = new AdamArtWorld { FeedbackEnabled = false })
        {
            for (var lane = 0; lane < 3; lane++)
            {
                Assert.IsTrue(world.TryChainGeometry(snapshot, boss, "boss", lane, out var geometry));
                Assert.AreEqual(new Vector2(actor.X, actor.Y), geometry.Start);
                var direction = boss.ChainDirections[lane];
                var end = geometry.Start + new Vector2(direction.X, direction.Y) * (float)(rules.ProjectileSpeed * rules.ProjectileLifetime);
                Assert.Less((geometry.End - end).magnitude, .00001);
                Assert.AreEqual((float)rules.ProjectileRadius, geometry.HalfWidth);
            }
            Assert.IsFalse(world.TryChainGeometry(snapshot, boss, "boss_reward", 0, out _));
        }
        Assert.AreEqual(AdamExodusR06Art.BossWindup, AdamExodusR06Art.BossClip("boss", boss, snapshot.Time, out var frame, out _));
        Assert.AreEqual(1, frame);
        boss.Telegraphing = false;
        boss.BossElapsed = rules.AttackPeriod;
        boss.NextAttackAt = 2 * rules.AttackPeriod;
        Assert.AreEqual(AdamExodusR06Art.BossAttack, AdamExodusR06Art.BossClip("boss", boss, snapshot.Time, out frame, out var seconds));
        Assert.Zero(frame);
        Assert.Zero(seconds);
        Assert.IsTrue(AdamArtWorld.BossDeathVisible(10, 10.49, boss));
        Assert.IsFalse(AdamArtWorld.BossDeathVisible(10, 10.50, boss));
    }
}
