using System;
using System.Linq;
using System.Numerics;
using System.Text.Json;
using BibleSurvivor.Sim;

namespace BibleSurvivor.Core.Tests;

internal static partial class PermanentSkillCombatTests
{
    private static void RegisterBasicCombatEvidence(Action<string, Action> test)
    {
        for (var rank = 1; rank <= 5; rank++)
        {
            var stage = rank;
            test($"Attack combat and replay attack@{stage}", () => CheckAttackBoundary(stage));
            Health("health", stage, 10);
        }
        var increments = new[] { 4, 4, 5, 5, 5, 6, 6, 7, 8 };
        for (var index = 0; index < increments.Length; index++)
            Health($"common.health.{index + 2:00}", 1, increments[index]);

        void Health(string id, int rank, int gain) =>
            test($"Survival combat and replay {id}@{rank}", () => CheckSurvivalBoundary(id, rank, gain));
    }

    private static void CheckSurvivalBoundary(string id, int rank, int gain)
    {
        WithStore(store =>
        {
            var (prior, bought) = BuyActionRank(store, id, rank);
            var (_, baseline, _, _) = PrepareActionWorld(prior, ActionRound.FirstWave);
            var oldLimit = (int)baseline.CurrentHealth;
            var newLimit = oldLimit + gain;
            Check.Equal(oldLimit, baseline.CurrentHealth);
            Check.True(!Survives(prior, oldLimit, oldLimit, false));
            Check.True(Survives(bought, newLimit, oldLimit, true));
            Check.True(Survives(bought, newLimit, newLimit - 1, true));
            Check.True(!Survives(bought, newLimit, newLimit, true));

            bool Survives(GameSnapshot profile, int fullHealth, int incoming, bool replay)
            {
                var state = SaveStore.PrepareNewChallenge(profile);
                var definition = new RoundDefinition(1, 40, 60, 25, new[]
                {
                    new WaveDefinition(0, 60, new[] { new SpawnGroup("a1", 60, 1000, incoming) }),
                });
                var session = new AdamSession(state, definition, "bible");
                session.Advance(12);
                var tuning = new BattleTuning { PlayerSpeed = .01 };
                Check.Equal(fullHealth, session.CurrentHealth);
                var setup = new BattleWorld(session, Speeds, 7, tuning).Capture();
                SetActionActors(setup, 0);
                setup.Enemies.First().Position = new Vector2(.15f, 0);
                var world = BattleWorld.Restore(session, Speeds, setup, tuning);
                var restored = replay ? ReopenActionCombat(store, state, definition, world, Speeds, tuning) : null;
                world.Advance(BattleWorld.FixedStep, Vector2.Zero);
                Check.Equal(incoming, world.DamageTaken);
                if (session.IsActive) Check.Equal(fullHealth - incoming, session.CurrentHealth);
                else Check.True(state.Run == null);
                if (restored != null)
                {
                    restored.Advance(BattleWorld.FixedStep, Vector2.Zero);
                    Check.Equal(world.DamageTaken, restored.DamageTaken);
                    Check.Equal(session.IsActive, restored.Session.IsActive);
                    if (session.IsActive) Check.Equal(session.CurrentHealth, restored.Session.CurrentHealth);
                }
                return session.IsActive;
            }
        });
    }

    private static void CheckAttackBoundary(int rank)
    {
        WithStore(store =>
        {
            var (prior, bought) = BuyActionRank(store, "attack", rank);
            var before = Strike(prior, 1000, false);
            var after = Strike(bought, 1000, true);
            Check.Near(.3, after.Damage - before.Damage);
            var between = before.Damage + .15;
            Check.True(!Strike(prior, between, false).Killed);
            Check.True(Strike(bought, between, true).Killed);
            Check.True(!Strike(bought, after.Damage + .01, true).Killed);

            (double Damage, bool Killed) Strike(GameSnapshot profile, double health, bool replay)
            {
                var (state, session, definition, speeds) = PrepareTempoWorld(profile, new TempoCase(FirstTickAt: 12));
                var tuning = new BattleTuning { PlayerSpeed = .01 };
                var setup = new BattleWorld(session, speeds, 7, tuning).Capture();
                SetActionActors(setup, 0);
                var target = setup.Enemies.First(enemy => session.Targets[enemy.Id].MonsterId == "a1");
                target.Position = new Vector2(1.4f, 0);
                if (health < session.Targets[target.Id].Health)
                    session.Hit(target.Id, session.Targets[target.Id].Health - health, null, original: false);
                var world = BattleWorld.Restore(session, speeds, setup, tuning);
                var restored = replay ? ReopenActionCombat(store, state, definition, world, speeds, tuning) : null;
                var result = Apply(world);
                if (restored != null)
                {
                    Check.Equal(result, Apply(restored));
                    Check.Equal(JsonSerializer.Serialize(world.Capture()), JsonSerializer.Serialize(restored.Capture()));
                }
                return result;

                (double Damage, bool Killed) Apply(BattleWorld field)
                {
                    field.Advance(BattleWorld.FixedStep, Vector2.Zero);
                    Check.Equal(1, field.Hits);
                    if (field.Session.Targets.TryGetValue(target.Id, out var alive))
                        return (health - alive.Health, false);
                    Check.True(field.Session.DroppedExperience.ContainsKey(target.Id));
                    return (health, true);
                }
            }
        });
    }
}
