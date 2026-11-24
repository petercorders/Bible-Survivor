using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text.Json;
using BibleSurvivor.Sim;

namespace BibleSurvivor.Core.Tests;

internal static partial class PermanentSkillCombatTests
{
    private readonly record struct CriticalCase(
        bool Elite = false, bool Boss = false, double TargetHealth = 1, double PlayerHealth = .8,
        float Distance = 3, double StationaryAtLanding = .25, int Ordinal = 1);

    private static void RegisterCriticalEvidence(Action<string, Action> test)
    {
        var ordinary = new CriticalCase(TargetHealth: 1);
        Chance("01", ordinary, null, 0, .03, 1.5);
        Multiplier("02", ordinary, null, .10);
        Chance("03", ordinary, new CriticalCase(Ordinal: 2), .03, .05, 1.6);
        Chance("04", new CriticalCase(Elite: true), ordinary, .08, .04, 1.6);
        Multiplier("05", new CriticalCase(TargetHealth: .25), new CriticalCase(TargetHealth: .251), .15);
        Chance("06", new CriticalCase(PlayerHealth: .90), new CriticalCase(PlayerHealth: .899), .08, .04, 1.6);
        Chance("07", new CriticalCase(Distance: 4.01f), new CriticalCase(Distance: 3.99f), .08, .04, 1.6);
        Chance("08", new CriticalCase(StationaryAtLanding: 1),
            new CriticalCase(StationaryAtLanding: .999), .08, .05, 1.6);
        test("Critical combat and replay common.crit.09@1", CheckCriticalMark);
        Chance("10", new CriticalCase(Boss: true, TargetHealth: .25),
            new CriticalCase(Boss: true, TargetHealth: .251), .12, .08, 1.75,
            new CriticalCase(TargetHealth: .25));

        void Chance(string suffix, CriticalCase active, CriticalCase? inactive, double threshold,
            double gain, double multiplier, CriticalCase? extraControl = null)
        {
            var id = $"common.crit.{suffix}";
            test($"Critical combat and replay {id}@1", () =>
            {
                WithStore(store =>
                {
                    var (prior, bought) = BuyCriticalRank(store, id);
                    var roll = threshold + gain / 2;
                    var before = MeasureCriticalStrike(store, prior, active, roll);
                    var after = MeasureCriticalStrike(store, bought, active, roll, replay: true);
                    Check.Near(before * multiplier, after);
                    Check.Near(before, MeasureCriticalStrike(store, bought, active, threshold + gain + .001));
                    if (threshold > 0)
                        Check.Near(after, MeasureCriticalStrike(store, prior, active, threshold - .001));
                    if (inactive is CriticalCase off) CompareControl(off, roll);
                    if (extraControl is CriticalCase extra) CompareControl(extra, threshold + gain / 8);

                    void CompareControl(CriticalCase sample, double controlRoll) =>
                        Check.Near(MeasureCriticalStrike(store, prior, sample, controlRoll),
                            MeasureCriticalStrike(store, bought, sample, controlRoll, replay: true));
                });
            });
        }

        void Multiplier(string suffix, CriticalCase active, CriticalCase? inactive, double gain)
        {
            var id = $"common.crit.{suffix}";
            test($"Critical combat and replay {id}@1", () =>
            {
                WithStore(store =>
                {
                    var (prior, bought) = BuyCriticalRank(store, id);
                    var ordinaryHit = MeasureCriticalStrike(store, prior, active, .98);
                    var before = MeasureCriticalStrike(store, prior, active, .01);
                    var after = MeasureCriticalStrike(store, bought, active, .01, replay: true);
                    Check.Near(ordinaryHit * gain, after - before);
                    Check.Near(ordinaryHit, MeasureCriticalStrike(store, bought, active, .98, replay: true));
                    if (inactive is CriticalCase off)
                        Check.Near(MeasureCriticalStrike(store, prior, off, .01),
                            MeasureCriticalStrike(store, bought, off, .01, replay: true));
                });
            });
        }
    }

    private static void CheckCriticalMark()
    {
        WithStore(store =>
        {
            var (prior, bought) = BuyActionRank(store, "common.crit.09", 1);
            var baseline = Measure(prior, false, false);
            var marked = Measure(bought, false, true);
            Check.Near(baseline.Third, marked.Third);
            var ordinary = Measure(prior, false, false, criticalFourth: false).Fourth;
            Check.Near(ordinary * .15, marked.Fourth - baseline.Fourth);
            Check.Near(ordinary, Measure(bought, false, true, criticalFourth: false).Fourth);
            Check.Near(Measure(prior, true, false).Fourth, Measure(bought, true, true).Fourth);

            (double Third, double Fourth) Measure(GameSnapshot profile, bool expired, bool replay, bool criticalFourth = true)
            {
                var (state, session, definition, speeds) = PrepareCriticalWave(profile, "jawbone");
                var tuning = new BattleTuning { PlayerSpeed = .005 };
                var setup = new BattleWorld(session, speeds, 7, tuning).Capture();
                var target = setup.Enemies.First().Id;
                SetActionActors(setup, 0);
                Put(setup, 1);
                var world = BattleWorld.Restore(session, speeds, setup, tuning);
                var critical = PrepareCriticalRoll(session, "jawbone", 1, .01);
                var streams = state.Run!.CriticalRandomStates!;
                double Contact(uint seed)
                {
                    streams["jawbone"] = seed;
                    var health = session.Targets[target].Health;
                    ForNextHit(world);
                    return health - session.Targets[target].Health;
                }

                Contact(critical);
                Contact(critical);
                var third = Contact(critical);
                if (expired)
                {
                    var away = world.Capture();
                    Put(away, 50);
                    world = BattleWorld.Restore(session, speeds, away, tuning);
                    world.Advance(3.01, Vector2.Zero);
                    Check.Equal(3, world.Hits);
                    var back = world.Capture();
                    Put(back, 1);
                    world = BattleWorld.Restore(session, speeds, back, tuning);
                }

                var fourthSeed = criticalFourth ? critical : PrepareCriticalRoll(session, "jawbone", 1, .98);
                streams["jawbone"] = fourthSeed;
                var restored = replay ? ReopenActionCombat(store, state, definition, world, speeds, tuning) : null;
                var fourth = Contact(fourthSeed);
                if (restored != null)
                {
                    ForNextHit(restored);
                    Check.Near(session.Targets[target].Health, restored.Session.Targets[target].Health);
                    Check.Equal(JsonSerializer.Serialize(world.Capture()), JsonSerializer.Serialize(restored.Capture()));
                }
                return (third, fourth);

                void Put(BattleSnapshot snapshot, float x) =>
                    snapshot.Enemies.Single(enemy => enemy.Id == target).Position = new Vector2(x, 0);
            }
        });
    }

    private static (GameSnapshot Prior, GameSnapshot Bought) BuyCriticalRank(SaveStore store, string id)
    {
        var node = PermanentSkillCatalog.Current.Find(id);
        var prior = ExpandedProfile(store, node.Prerequisites.Keys.Append("weapon.lightning.base.01").ToArray());
        return (prior, Buy(store, prior, id, node.Prices[0]));
    }

    private static double MeasureCriticalStrike(SaveStore store, GameSnapshot profile, CriticalCase sample,
        double roll, bool replay = false)
    {
        var (state, session, definition, speeds) = sample.Boss
            ? PrepareActionWorld(profile, ActionRound.ActiveBoss)
            : PrepareCriticalWave(profile);
        var tuning = new BattleTuning { PlayerSpeed = .001 };
        Check.True(session.Inventory.TryChoose("lightning", false));
        var setup = new BattleWorld(session, speeds, 7, tuning).Capture();
        foreach (var enemy in setup.Enemies) enemy.Position = new Vector2(50, 50);
        var candidates = setup.Enemies.Where(enemy => sample.Boss
                ? enemy.Id == session.BossRules!.BossActorId : session.IsEliteActor(enemy.Id) == sample.Elite)
            .OrderBy(enemy => enemy.Id).Take(sample.Ordinal).ToArray();
        Check.Equal(sample.Ordinal, candidates.Length);
        for (var i = 0; i < candidates.Length; i++)
            candidates[i].Position = new Vector2(sample.Distance, i * .05f);
        var target = candidates[^1].Id;
        if (sample.TargetHealth < 1)
            session.Hit(target, session.Targets[target].Health * (1 - sample.TargetHealth), null, original: false);
        if (sample.Boss)
        {
            // At low boss health the deterministic phase change declares support actors.
            var positions = setup.Enemies.ToDictionary(enemy => enemy.Id, enemy => enemy.Position);
            setup = new BattleWorld(session, speeds, 7, tuning).Capture();
            foreach (var enemy in setup.Enemies)
                enemy.Position = positions.GetValueOrDefault(enemy.Id, new Vector2(50, 50));
        }
        var run = state.Run!;
        run.Health = run.MaxHealth * sample.PlayerHealth;
        if (run.CriticalRandomStates != null)
            run.CriticalRandomStates["lightning"] = PrepareCriticalRoll(session, "lightning", sample.Ordinal, roll);
        var world = BattleWorld.Restore(session, speeds, setup, tuning);
        world.Advance(BattleWorld.FixedStep, Vector2.Zero);
        var strikeAt = world.Capture().Lightning!.Pending.Single().At;
        run.PermanentActions!.StationarySince = strikeAt - sample.StationaryAtLanding;
        var restored = replay ? ReopenActionCombat(store, state, definition, world, speeds, tuning) : null;
        var health = session.Targets[target].Health;
        for (var tick = 0; tick < 120 && world.SimulationTime < strikeAt - 1e-8; tick++)
        {
            world.Advance(BattleWorld.FixedStep, Vector2.Zero);
            restored?.Advance(BattleWorld.FixedStep, Vector2.Zero);
        }
        Check.True(world.SimulationTime >= strikeAt - 1e-8);
        var remaining = session.Targets[target].Health;
        Check.True(remaining < health);
        if (restored != null)
        {
            Check.Near(remaining, restored.Session.Targets[target].Health);
            Check.Equal(JsonSerializer.Serialize(world.Capture()), JsonSerializer.Serialize(restored.Capture()));
        }
        return health - remaining;
    }

    private static (GameSnapshot, AdamSession, RoundDefinition, IReadOnlyDictionary<string, double>)
        PrepareCriticalWave(GameSnapshot profile, string weapon = "bible")
    {
        var state = SaveStore.PrepareNewChallenge(profile);
        var wave = new RoundDefinition(1, 40, 60, 25, new[]
        {
            new WaveDefinition(0, 60, new[]
            {
                new SpawnGroup("a1", 60, 1000, 4), new SpawnGroup("a2", 60, 4000, 4),
            }),
        });
        var normal = new AdamSession(state, wave, weapon);
        normal.Advance(5);
        return (state, normal, wave, Speeds);
    }

    private static uint PrepareCriticalRoll(AdamSession session, string weapon, int ordinal, double target)
    {
        for (uint state = 1; state < 100_000; state++)
        {
            var current = state;
            for (var hit = 0; hit < ordinal; hit++)
                current = session.NextWeaponProcState(weapon, current);
            var roll = current / 4294967296d;
            if (Math.Abs(roll - target) < .0005) return state;
        }
        throw new InvalidOperationException($"Could not prepare critical roll at {target} for hit {ordinal}");
    }
}
