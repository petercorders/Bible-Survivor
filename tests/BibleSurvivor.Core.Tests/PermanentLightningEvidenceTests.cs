using System;
using System.Linq;
using System.Numerics;
using System.Text.Json;

namespace BibleSurvivor.Core.Tests;

internal static partial class PermanentSkillCombatTests
{
    private static void RegisterLightningMasteryEvidence(Action<string, Action> test)
    {
        for (var ordinal = 1; ordinal <= 5; ordinal++)
        {
            var mastery = ordinal;
            var id = $"weapon.lightning.base.{mastery:D2}";
            test($"Lightning combat and replay {id}@1", () => CheckLightningMastery(id, mastery));
        }
        foreach (var suffix in new[] { "a.01", "a.02", "a.04", "a.05", "b.01" })
        {
            var id = $"weapon.lightning.{suffix}";
            test($"Lightning combat and replay {id}@1", () => CheckLightningTargeting(id, suffix));
        }
    }

    private static void CheckLightningTargeting(string id, string suffix)
    {
        WithStore(store =>
        {
            var node = PermanentSkillCatalog.Current.Find(id);
            var prior = ExpandedProfile(store, node.Prerequisites.Keys.ToArray());
            var bought = Buy(store, prior, id, node.Prices[0]);
            foreach (var boundary in new[] { true, false })
            {
                Run(prior, acquired: false, boundary);
                Run(bought, acquired: true, boundary);
            }

            void Run(GameSnapshot profile, bool acquired, bool boundary)
            {
                var active = acquired && boundary;
                var state = SaveStore.PrepareNewChallenge(profile);
                var definition = new RoundDefinition(1, 40, 60, 25, new[]
                {
                    new WaveDefinition(0, 60, new[] {
                        new SpawnGroup("a1", 60, 1000, 4), new SpawnGroup("a2", 60, 4000, 4) }),
                });
                var tuning = new BattleTuning { PlayerSpeed = .01 };
                var session = new AdamSession(state, definition, "bible");
                Check.True(session.Inventory.TryChoose("lightning", false));
                session.Advance(5);
                var setup = new BattleWorld(session, Speeds, 7, tuning).Capture();
                var ordinary = setup.Enemies.Where(enemy => session.Targets[enemy.Id].MonsterId == "a1").Take(3).ToArray();
                var near = ordinary[0];
                var far = suffix == "a.02"
                    ? setup.Enemies.First(enemy => session.Targets[enemy.Id].MonsterId == "a2") : ordinary[1];
                foreach (var enemy in setup.Enemies) enemy.Position = new Vector2(50, 50);
                near.Position = new Vector2(2, 0);
                far.Position = new Vector2(5, 0);
                if (suffix == "a.01") ordinary[2].Position = new Vector2(5, boundary ? 1.49f : 1.51f);
                if (suffix == "a.02" && !boundary) far.X = 8.01f;
                if (suffix == "a.05") far.Position = new Vector2(2, boundary ? 2.01f : 1.99f);
                if (suffix == "b.01") session.Hit(near.Id, boundary ? 250 : 251, null, original: false);
                var world = BattleWorld.Restore(session, Speeds, setup, tuning);
                world.Advance(BattleWorld.FixedStep, Vector2.Zero);
                setup = world.Capture();
                var strike = setup.Lightning!.Pending.Single();
                var targetFar = active && suffix is ("a.01" or "a.02" or "a.04");
                Check.Equal(targetFar && suffix != "a.04" ? far.Id : near.Id, strike.AssignedTargetId);
                var replay = active ? ReopenCombat(store, state, definition, world, Speeds, tuning) : null;

                if (suffix == "a.04" && boundary)
                {
                    session.Hit(near.Id, session.Targets[near.Id].Health, null, original: false);
                    if (replay != null)
                        replay.Session.Hit(near.Id, replay.Session.Targets[near.Id].Health, null, original: false);
                    Advance();
                    var pending = world.Capture().Lightning!.Pending.Single();
                    Check.Near(strike.At, pending.At);
                    Check.Equal(acquired ? far.Id : near.Id, pending.AssignedTargetId);
                    Check.True(!pending.CanRetarget);
                    Check.Equal(0, world.Hits);
                }
                var farHp = session.Targets[far.Id].Health;
                var nearHp = session.Targets.TryGetValue(near.Id, out var actor) ? actor.Health : 0;
                for (var tick = 0; tick < 20 && world.SimulationTime < strike.At - 1e-8; tick++) Advance();
                Check.True(world.SimulationTime >= strike.At - 1e-8);
                Check.Near(targetFar ? 22.6 : 0, farHp - session.Targets[far.Id].Health);
                if (nearHp > 0)
                {
                    var bonus = active ? suffix == "a.05" ? 2 : suffix == "b.01" ? 1.2 : 0 : 0;
                    Check.Near(targetFar ? 0 : 22.6 + bonus, nearHp - session.Targets[near.Id].Health);
                }
                if (replay != null)
                {
                    Check.Near(session.Targets[far.Id].Health, replay.Session.Targets[far.Id].Health);
                    if (nearHp > 0) Check.Near(session.Targets[near.Id].Health, replay.Session.Targets[near.Id].Health);
                    Check.Equal(JsonSerializer.Serialize(world.Capture()), JsonSerializer.Serialize(replay.Capture()));
                }
                void Advance()
                {
                    world.Advance(BattleWorld.FixedStep, Vector2.Zero);
                    replay?.Advance(BattleWorld.FixedStep, Vector2.Zero);
                }
            }
        });
    }

    private static void CheckLightningMastery(string id, int mastery)
    {
        WithStore(store =>
        {
            var node = PermanentSkillCatalog.Current.Find(id);
            var before = ExpandedProfile(store, node.Prerequisites.Keys.ToArray());
            if (!before.Profile.UnlockedWeapons.Contains("lightning"))
            {
                before.Profile.UnlockedWeapons.Add("lightning");
                store.Commit(before, before.Revision, "lightning-unlocked");
                before = store.Load()!;
            }
            var after = Buy(store, before, id, node.Prices[0]);
            var definition = new RoundDefinition(1, 40, 60, 25, new[]
            {
                new WaveDefinition(0, 60, new[] { new SpawnGroup("a1", 60, 1000, 4) }),
            });
            var tuning = new BattleTuning { PlayerSpeed = .01 };

            Run(before, acquired: false);
            Run(after, acquired: true);

            void Run(GameSnapshot profile, bool acquired)
            {
                var session = new AdamSession(profile, definition, "bible");
                Check.True(session.Inventory.TryChoose("lightning", false));
                session.Advance(5.5);
                var fixture = new BattleWorld(session, Speeds, 7, tuning).Capture();
                var targets = fixture.Enemies.OrderBy(enemy => enemy.Id).Take(5).ToArray();
                Check.Equal(5, targets.Length);
                foreach (var enemy in fixture.Enemies) enemy.Position = new Vector2(50, 50);
                targets[0].Position = new Vector2(4, 0);
                if (mastery == 4)
                {
                    targets[1].Position = new Vector2(4, .76f);
                    targets[2].Position = new Vector2(4, .84f);
                    targets[3].Position = new Vector2(4, .92f);
                    targets[4].Position = new Vector2(-4.01f, 0);
                }
                else targets[1].Position = new Vector2(4, 2);

                var world = BattleWorld.Restore(session, Speeds, fixture, tuning);
                world.Advance(BattleWorld.FixedStep, Vector2.Zero);
                var frozen = world.Capture();
                var lightning = frozen.Lightning!;
                var count = mastery > 3 || mastery == 3 && acquired ? 2 : 1;
                var wait = mastery == 2 && acquired ? 3.8 : 4;
                var delay = mastery == 5 && acquired ? .07 : .15;
                var radius = mastery > 4 || mastery == 4 && acquired ? .88 : .80;
                var damage = mastery > 1 || acquired ? 22.6 : 20.6;
                Check.Equal(count, lightning.Pending.Count);
                Check.Near(wait - delay, lightning.NextCycle - lightning.Pending[0].At);
                Check.Near(delay, lightning.Pending[0].At - frozen.Time);
                Check.Near(radius, lightning.Pending[0].Radius);
                Check.Equal(0, world.Hits);

                BattleWorld? replay = null;
                AdamSession? resumed = null;
                if (acquired)
                {
                    profile.Run!.World = frozen;
                    store.Commit(profile, profile.Revision, "lightning-before-impact");
                    var saved = store.LoadConfirmed()!;
                    resumed = AdamSession.Resume(saved, definition);
                    replay = BattleWorld.Restore(resumed, Speeds, saved.Run!.World!, tuning);
                }

                StepUntil(lightning.Pending[0].At, () =>
                {
                    Check.Equal(0, world.Hits);
                    Check.Near(1000, session.Targets[targets[0].Id].Health);
                });
                Check.Near(1000 - damage, session.Targets[targets[0].Id].Health);
                Check.Near(1000 - (mastery == 4 || count == 2 ? damage : 0), session.Targets[targets[1].Id].Health);
                Check.Near(1000 - (mastery == 4 && acquired ? damage : 0), session.Targets[targets[2].Id].Health);
                Check.Near(1000, session.Targets[targets[3].Id].Health);
                Check.Near(1000 - (mastery == 4 ? damage : 0), session.Targets[targets[4].Id].Health);
                CheckReplay();

                StepUntil(lightning.NextCycle, () => Check.Equal(1L, world.Capture().Lightning!.Cycle));
                Check.Equal(2L, world.Capture().Lightning!.Cycle);
                CheckReplay();

                void StepUntil(double at, Action beforeBoundary)
                {
                    for (var tick = 0; tick < 600 && world.SimulationTime + BattleWorld.FixedStep < at - 1e-8; tick++)
                        Step();
                    Check.True(world.SimulationTime < at && world.SimulationTime + BattleWorld.FixedStep >= at - 1e-8);
                    beforeBoundary();
                    Step();
                }

                void Step()
                {
                    world.Advance(BattleWorld.FixedStep, Vector2.Zero);
                    replay?.Advance(BattleWorld.FixedStep, Vector2.Zero);
                }

                void CheckReplay()
                {
                    if (replay == null) return;
                    Check.Equal(JsonSerializer.Serialize(world.Capture()), JsonSerializer.Serialize(replay.Capture()));
                    foreach (var target in targets)
                        Check.Near(session.Targets[target.Id].Health, resumed!.Targets[target.Id].Health);
                }
            }
        });
    }
}
