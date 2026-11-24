using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text.Json;
using BibleSurvivor.Sim;

namespace BibleSurvivor.Core.Tests;

internal static partial class PermanentSkillCombatTests
{
    private readonly record struct TempoCase(double FirstTickAt = 12, double HurtAgo = -1,
        double EliteKillAgo = -1, double Carry = 0, bool Opening = false,
        ActionRound Round = ActionRound.FirstWave);

    private static void RegisterTempoEvidence(Action<string, Action> test)
    {
        var normal = new TempoCase(FirstTickAt: 12);
        for (var rank = 1; rank <= 3; rank++)
            Cycle("cooldown", rank, normal, null, .02);
        Cycle("common.tempo.02", 1, new TempoCase(Opening: true), normal, .15);
        Cycle("common.tempo.03", 1, new TempoCase(HurtAgo: 3), new TempoCase(HurtAgo: 3.001), .03);
        Cycle("common.tempo.04", 1, new TempoCase(FirstTickAt: 10), new TempoCase(FirstTickAt: 10.001), .04);
        Cycle("common.tempo.06", 1, new TempoCase(FirstTickAt: 60), new TempoCase(FirstTickAt: 59.999), .04);
        Cycle("common.tempo.07", 1, new TempoCase(FirstTickAt: 5, Carry: 5),
            new TempoCase(FirstTickAt: 5, Carry: 4.999), .04);
        Cycle("common.tempo.08", 1, new TempoCase(EliteKillAgo: 3), new TempoCase(EliteKillAgo: 3.001), .05);
        Cycle("common.tempo.10", 1, new TempoCase(Round: ActionRound.ActiveBoss),
            new TempoCase(Round: ActionRound.BossRoundWave), .05);
        test("Tempo combat and replay common.tempo.05@1", () => CheckTempoSlot(fixedHit: true));
        test("Tempo combat and replay common.tempo.09@1", () => CheckTempoSlot(fixedHit: false));

        void Cycle(string id, int rank, TempoCase active, TempoCase? inactive, double gain) =>
            test($"Tempo combat and replay {id}@{rank}", () => CheckTempoCycle(id, rank, active, inactive, gain));
    }

    private static void CheckTempoCycle(string id, int rank, TempoCase active, TempoCase? inactive, double gain)
    {
        WithStore(store =>
        {
            var (prior, bought) = BuyActionRank(store, id, rank);
            Check.Near(gain, Measure(prior, active, false) - Measure(bought, active, true));
            if (inactive is TempoCase off)
                Check.Near(Measure(prior, off, false), Measure(bought, off, true));

            double Measure(GameSnapshot profile, TempoCase sample, bool replay)
            {
                var (state, session, definition, speeds) = PrepareTempoWorld(profile, sample);
                var run = state.Run!;
                var tuning = new BattleTuning { PlayerSpeed = .01 };
                var now = session.CombatTime + BattleWorld.FixedStep;
                if (sample.HurtAgo >= 0) run.LastHitTime = now - sample.HurtAgo;
                if (session.HasExpandedSkill("common.tempo.07")) run.NoHitCarrySeconds = sample.Carry;
                if (session.HasExpandedSkill("common.tempo.02")) run.TempoOpeningUsed = !sample.Opening;
                var setup = new BattleWorld(session, speeds, 7, tuning).Capture();
                foreach (var drop in session.DroppedExperience.Keys)
                    setup.Drops.Add(new BattleEntityState { Id = drop, X = 10, Y = 10 });
                SetActionActors(setup, 0);
                var target = setup.Enemies.First();
                target.Position = new Vector2(1.4f, 0);
                var world = BattleWorld.Restore(session, speeds, setup, tuning);
                world.Advance(BattleWorld.FixedStep, Vector2.Zero);
                Check.Equal(1, world.Hits);
                var first = world.Capture().Weapons.Single(clock => clock.WeaponId == "jawbone");
                if (session.HasExpandedSkill("common.tempo.02")) Check.True(run.TempoOpeningUsed);
                var restored = replay ? ReopenActionCombat(store, state, definition, world, speeds, tuning) : null;
                NextAt(world, first.NextCycle);
                if (restored != null)
                {
                    NextAt(restored, first.NextCycle);
                    Check.Near(session.Targets[target.Id].Health, restored.Session.Targets[target.Id].Health);
                    Check.Equal(JsonSerializer.Serialize(world.Capture()), JsonSerializer.Serialize(restored.Capture()));
                }
                return first.NextCycle - first.ActiveSince;
            }
        });
    }

    private static void CheckTempoSlot(bool fixedHit)
    {
        WithStore(store =>
        {
            var id = fixedHit ? "common.tempo.05" : "common.tempo.09";
            var (prior, bought) = BuyActionRank(store, id, 1);
            Verify(prior, false, fixedHit ? 1.7 : .15);
            Verify(bought, true, fixedHit ? 1.7 * .96 : .15 * .92);

            void Verify(GameSnapshot profile, bool replay, double interval)
            {
                var weapon = fixedHit ? "bible" : "jawbone";
                var (state, session, definition, speeds) = PrepareTempoWorld(profile, new TempoCase(FirstTickAt: 12), weapon);
                if (!fixedHit) Check.True(session.Inventory.TryChoose("jawbone", false));
                var tuning = new BattleTuning { PlayerSpeed = .01, BookRadius = 4 };
                var setup = new BattleWorld(session, speeds, 7, tuning).Capture();
                SetActionActors(setup, 0);
                var target = setup.Enemies.First();
                target.Position = new Vector2(1.4f, 0);
                var world = BattleWorld.Restore(session, speeds, setup, tuning);
                world.Advance(BattleWorld.FixedStep, Vector2.Zero);
                Check.Equal(1, world.Hits);
                var clock = world.Capture().Weapons.Single();
                var next = fixedHit ? world.SimulationTime + interval : clock.ActiveSince + interval;
                if (!fixedHit) Check.Near(next, clock.PendingBursts.Single());
                var restored = replay ? ReopenActionCombat(store, state, definition, world, speeds, tuning) : null;
                NextAt(world, next);
                if (restored != null)
                {
                    NextAt(restored, next);
                    Check.Near(session.Targets[target.Id].Health, restored.Session.Targets[target.Id].Health);
                    Check.Equal(JsonSerializer.Serialize(world.Capture()), JsonSerializer.Serialize(restored.Capture()));
                }
            }
        });
    }

    private static void NextAt(BattleWorld world, double scheduled)
    {
        var hits = world.Hits;
        for (var tick = 0; tick < 200 && world.SimulationTime + BattleWorld.FixedStep < scheduled - 1e-9; tick++)
            world.Advance(BattleWorld.FixedStep, Vector2.Zero);
        Check.Equal(hits, world.Hits);
        Check.True(world.SimulationTime + BattleWorld.FixedStep >= scheduled - 1e-9);
        world.Advance(BattleWorld.FixedStep, Vector2.Zero);
        Check.Equal(hits + 1, world.Hits);
    }

    private static (GameSnapshot, AdamSession, RoundDefinition, IReadOnlyDictionary<string, double>)
        PrepareTempoWorld(GameSnapshot profile, TempoCase sample, string weapon = "jawbone")
    {
        if (sample.Round != ActionRound.FirstWave) return PrepareActionWorld(profile, sample.Round);
        var state = SaveStore.PrepareNewChallenge(profile);
        var wave = new RoundDefinition(1, 40, 100, 25, new[]
        {
            new WaveDefinition(0, 120, new[] { new SpawnGroup("a1", 120, 1000, 4),
                new SpawnGroup("a2", 60, 4000, 4) }),
        });
        var normal = new AdamSession(state, wave, weapon);
        if (sample.EliteKillAgo >= 0)
        {
            normal.Advance(sample.FirstTickAt - sample.EliteKillAgo);
            var elite = normal.Targets.Keys.First(normal.IsEliteActor);
            normal.Hit(elite, normal.Targets[elite].Health);
            normal.Advance(sample.EliteKillAgo - BattleWorld.FixedStep);
        }
        else normal.Advance(sample.FirstTickAt - BattleWorld.FixedStep);
        return (state, normal, wave, Speeds);
    }
}
