using System;
using System.Collections.Generic;
using System.Linq;
using BibleSurvivor.Core;
using BibleSurvivor.Unity;
using NUnit.Framework;
using UnityEngine;

public sealed class AdamR50WorldTests
{
    [Test]
    public void InstalledWavesHaveReviewedRoleClipsAndShadows()
    {
        var roles = new[] { "js1", "js2", "js3", "jd1", "jd2", "jd3" };
        var reviewedActors = new[] { "ex17", "ex16", "ex18", "ex17", "ex16", "ex18" };
        var shadows = new[] { "fx_shadow-wide_static_24x6", "fx_shadow-small_static_12x4", "fx_shadow-wide_static_24x6",
            "fx_shadow-wide_static_24x6", "fx_shadow-small_static_12x4", "fx_shadow-wide_static_24x6" };
        var art = new AdamArt();
        using (var rig = new AdamExodusR06Art())
        {
            for (var i = 0; i < roles.Length; i++)
            {
                var clip = rig.Clip(AdamArtWorld.EnemyClip(roles[i]));
                Assert.AreEqual(AdamExodusR06Art.EnemyClip(reviewedActors[i]), clip.id);
                Assert.AreEqual(4, clip.frames);
                Assert.AreEqual(shadows[i], AdamRoundPresentation.Shadow(roles[i]));
                Assert.NotNull(art.Sprite(shadows[i], 0));
            }
        }

        for (var round = 41; round <= 50; round++)
        {
            var groups = CampaignContent.Round(round).Waves.SelectMany(wave => wave.Groups);
            var expected = round < JudgesContent.GlobalRoundNumber ? roles.Take(3) : roles.Skip(3);
            CollectionAssert.AreEquivalent(expected, groups.Select(group => group.MonsterId).Distinct(), $"Round {round}");
        }
    }

    [TestCase(42, "shifting_boundary")]
    [TestCase(45, "torrent_serpent")]
    [TestCase(48, "ford_gatekeeper")]
    public void BossUsesReviewedArtAndProjectsEverySavedWarningWithLowEffects(int round, string id)
    {
        var rules = BossDefinition.ForRound(round);
        Assert.AreEqual(id, rules.Id);
        Assert.AreEqual(AdamExodusR06Art.BossMove, AdamArtWorld.EnemyClip(id));
        Assert.AreEqual("fx_shadow-boss_static_40x8", AdamRoundPresentation.Shadow(id));
        Assert.AreEqual(AdamExodusR06Art.DeathSeconds, AdamArtWorld.BossDeathDuration(new BossEncounterState { RoundNumber = round }));

        var boss = new BossEncounterState
        {
            RoundNumber = round,
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
        Assert.AreEqual(AdamExodusR06Art.BossAttack, AdamExodusR06Art.BossClip("boss", boss, snapshot.Time, out frame, out _));
        Assert.Zero(frame);
    }
}
