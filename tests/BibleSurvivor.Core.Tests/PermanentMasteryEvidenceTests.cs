using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text.Json;
using BibleSurvivor.Sim;

namespace BibleSurvivor.Core.Tests;

internal static partial class PermanentSkillCombatTests
{
    private static void RegisterLiveMasteryEvidence(Action<string, Action> test)
    {
        var starters = new[] { "bible", "jawbone", "sling" };
        foreach (var weapon in starters.Concat(ExpandedWeaponRules.All.Select(profile => profile.WeaponId)))
        {
            if (weapon == "lightning") continue;
            test($"Mastery damage combat and replay weapon.{weapon}.base.01@1", () =>
                CheckMasteryDamageBoundary(weapon));
            test($"Mastery cadence combat and replay weapon.{weapon}.base.02@1", () =>
                CheckMasteryCadenceBoundary(weapon));
            if (weapon != "sling")
                test($"Mastery amount combat and replay weapon.{weapon}.base.03@1", () =>
                    CheckMasteryAmountBoundary(weapon));
        }
    }

    private static void CheckMasteryDamageBoundary(string weapon)
    {
        WithStore(store =>
        {
            var (prior, bought) = BuyMasteryRank(store, weapon, "01");
            var before = Strike(prior, 1000, false);
            var after = Strike(bought, 1000, true);
            var baseDamage = ExpandedWeaponRules.TryGet(weapon, out var scheduled)
                ? scheduled.AtLevel(1).Damage : WeaponRules.Get(weapon, 1).Damage;
            Check.Near(baseDamage * .1, after.Damage - before.Damage);
            Check.Near(before.Time, after.Time);
            var between = (before.Damage + after.Damage) / 2;
            Check.True(!Strike(prior, between, false).Killed);
            Check.True(Strike(bought, between, true).Killed);
            Check.True(!Strike(bought, after.Damage + .01, true).Killed);

            BattleImpact Strike(GameSnapshot profile, double health, bool replay)
            {
                var (state, session, definition, speeds) = PrepareTempoWorld(
                    profile, new TempoCase(FirstTickAt: 12), weapon);
                var tuning = new BattleTuning { PlayerSpeed = .01 };
                var setup = new BattleWorld(session, speeds, 7, tuning).Capture();
                SetActionActors(setup, 0);
                var target = setup.Enemies.First(enemy => session.Targets[enemy.Id].MonsterId == "a1");
                target.Position = weapon == "bible" ? new Vector2(0, -1.4f) : new Vector2(1.4f, 0);
                if (health < session.Targets[target.Id].Health)
                    session.Hit(target.Id, session.Targets[target.Id].Health - health, null, original: false);
                var world = BattleWorld.Restore(session, speeds, setup, tuning);
                var restored = replay ? ReopenActionCombat(store, state, definition, world, speeds, tuning) : null;
                var hit = FirstImpact(world, target.Id, weapon);
                if (restored != null)
                {
                    var again = FirstImpact(restored, target.Id, weapon);
                    Check.Equal(hit.Killed, again.Killed);
                    Check.Equal(hit.Time, again.Time);
                    Check.Equal(hit.Damage, again.Damage);
                    Check.Equal(JsonSerializer.Serialize(world.Capture()), JsonSerializer.Serialize(restored.Capture()));
                }
                return hit;
            }
        });
    }

    private static void CheckMasteryCadenceBoundary(string weapon)
    {
        WithStore(store =>
        {
            var (prior, bought) = BuyMasteryRank(store, weapon, "02");
            var oldGap = Measure(prior, false);
            var newGap = Measure(bought, true);
            var waitingBase = ExpandedWeaponRules.TryGet(weapon, out var scheduled)
                ? scheduled.AtLevel(1).Cycle : WeaponRules.Get(weapon, 1).Cooldown;
            Check.True(newGap < oldGap);
            Check.Near(waitingBase * .05, oldGap - newGap, BattleWorld.FixedStep + 1e-7);

            double Measure(GameSnapshot profile, bool replay)
            {
                var (state, session, definition, speeds) = PrepareTempoWorld(
                    profile, new TempoCase(FirstTickAt: 12), weapon);
                var tuning = new BattleTuning { PlayerSpeed = .01 };
                var setup = new BattleWorld(session, speeds, 7, tuning).Capture();
                SetActionActors(setup, 0);
                var target = setup.Enemies.First();
                target.Position = weapon == "bible" ? new Vector2(0, -1.4f) : new Vector2(1.4f, 0);
                var world = BattleWorld.Restore(session, speeds, setup, tuning);
                var first = ObserveLaunch(world);
                var restored = replay ? ReopenActionCombat(store, state, definition, world, speeds, tuning) : null;
                var second = ObserveLaunch(world);
                if (restored != null)
                {
                    Check.Near(second, ObserveLaunch(restored));
                    Check.Equal(JsonSerializer.Serialize(world.Capture()), JsonSerializer.Serialize(restored.Capture()));
                }
                return second - first;
            }
        });
    }

    private static double ObserveLaunch(BattleWorld world)
    {
        var previous = world.ShotsFired;
        for (var tick = 0; tick < 550 && world.ShotsFired == previous; tick++)
            world.Advance(BattleWorld.FixedStep, Vector2.Zero);
        Check.Equal(previous + 1, world.ShotsFired);
        return world.SimulationTime;
    }

    private static void CheckMasteryAmountBoundary(string weapon)
    {
        if (weapon == "wilderness_voice")
        {
            CheckVoiceReachBoundary();
            return;
        }

        WithStore(store =>
        {
            var (prior, bought) = BuyMasteryRank(store, weapon, "03");
            Check.Equal(1, Resolve(prior, false));
            Check.Equal(2, Resolve(bought, true));

            int Resolve(GameSnapshot profile, bool replay)
            {
                var (state, session, definition, speeds) = PrepareTempoWorld(
                    profile, new TempoCase(FirstTickAt: 12), weapon);
                var tuning = new BattleTuning { PlayerSpeed = .01 };
                var setup = new BattleWorld(session, speeds, 7, tuning).Capture();
                SetActionActors(setup, 0);
                setup.Enemies[0].Position = new Vector2(1.4f, 0);
                setup.Enemies[1].Position = new Vector2(1.9f, .75f);
                var world = BattleWorld.Restore(session, speeds, setup, tuning);
                var restored = replay ? ReopenActionCombat(store, state, definition, world, speeds, tuning) : null;
                world.Advance(.75, Vector2.Zero);
                if (restored != null)
                {
                    restored.Advance(.75, Vector2.Zero);
                    Check.Equal(world.ShotsFired, restored.ShotsFired);
                    Check.Equal(JsonSerializer.Serialize(world.Capture()), JsonSerializer.Serialize(restored.Capture()));
                }
                return world.ShotsFired;
            }
        });
    }

    private static void CheckVoiceReachBoundary()
    {
        const string weapon = "wilderness_voice";
        WithStore(store =>
        {
            var (prior, bought) = BuyMasteryRank(store, weapon, "03");
            Check.True(Hits(prior, 2.99f, false));
            Check.True(!Hits(prior, 3.3f, false));
            Check.True(Hits(bought, 3.3f, true));
            Check.True(Hits(bought, 3.59f, true));
            Check.True(!Hits(bought, 3.61f, true));

            bool Hits(GameSnapshot profile, float distance, bool replay)
            {
                var (state, session, definition, speeds) = PrepareTempoWorld(
                    profile, new TempoCase(FirstTickAt: 12), weapon);
                var tuning = new BattleTuning { PlayerSpeed = .01 };
                var setup = new BattleWorld(session, speeds, 7, tuning).Capture();
                SetActionActors(setup, 0);
                var target = setup.Enemies.First();
                target.Position = new Vector2(distance, 0);
                var world = BattleWorld.Restore(session, speeds, setup, tuning);
                var restored = replay ? ReopenActionCombat(store, state, definition, world, speeds, tuning) : null;
                world.Advance(.3, Vector2.Zero);
                Check.Equal(1, world.ShotsFired);
                if (restored != null)
                {
                    restored.Advance(.3, Vector2.Zero);
                    Check.Equal(world.Hits, restored.Hits);
                    Check.Equal(JsonSerializer.Serialize(world.Capture()), JsonSerializer.Serialize(restored.Capture()));
                }
                return world.Hits > 0;
            }
        });
    }

    private static (GameSnapshot Prior, GameSnapshot Bought) BuyMasteryRank(SaveStore store, string weapon, string suffix)
    {
        var id = $"weapon.{weapon}.base.{suffix}";
        var node = PermanentSkillCatalog.Current.Find(id);
        var prior = ExpandedProfile(store, node.Prerequisites.Keys.ToArray());
        if (!prior.Profile.UnlockedWeapons.Contains(weapon))
        {
            prior.Profile.UnlockedWeapons.Add(weapon);
            store.Commit(prior, prior.Revision, "mastery-acceptance-unlock");
            prior = store.LoadConfirmed()!;
        }
        return (prior, Buy(store, prior, id, node.Prices[0]));
    }

    private static BattleImpact FirstImpact(BattleWorld world, int target, string weapon)
    {
        var impacts = new List<BattleImpact>();
        var cursor = world.ImpactSequence;
        var initialHealth = world.Session.Targets[target].Health;
        for (var ticks = 0; ticks < 620; ticks++)
        {
            world.Advance(BattleWorld.FixedStep, Vector2.Zero);
            Check.Equal(0L, world.ReadImpacts(cursor, impacts));
            cursor = world.ImpactSequence;
            var hit = impacts.FirstOrDefault(item => item.TargetId == target && item.Cause == weapon);
            if (hit.Sequence == 0) continue;
            if (hit.Killed) Check.True(!world.Session.Targets.ContainsKey(target));
            else Check.Near(initialHealth - world.Session.Targets[target].Health, hit.Damage);
            return hit;
        }
        throw new InvalidOperationException($"No {weapon} impact against {target} at time {world.SimulationTime}");
    }
}
