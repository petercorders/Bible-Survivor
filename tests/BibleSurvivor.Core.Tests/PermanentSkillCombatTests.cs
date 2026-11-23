using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text.Json;
using BibleSurvivor.Sim;

namespace BibleSurvivor.Core.Tests;

internal static class PermanentSkillCombatTests
{
    private static readonly Dictionary<string, double> Speeds = new() { ["a1"] = .45, ["a2"] = .6, ["a3"] = .35 };

    public static void Register(Action<string, Action> test)
    {
        test("Expanded armor applies per-source, health, time, crowd and boss conditions before capping", () =>
        {
            var ranks = new Dictionary<string, int> { ["armor"] = 3 };
            for (var ordinal = 2; ordinal <= 10; ordinal++) ranks.Add($"common.armor.{ordinal:D2}", 1);
            Check.Equal(5, PermanentCombatRules.Armor(ranks, 0, IncomingDamageKind.Contact, .5, 7.999,
                false, false, false));
            Check.Equal(5, PermanentCombatRules.Armor(ranks, 0, IncomingDamageKind.Projectile, .5, 7.999,
                false, false, false));
            Check.Equal(8, PermanentCombatRules.Armor(ranks, 0, IncomingDamageKind.Other, .30, 8,
                true, false, false));
            Check.Equal(8, PermanentCombatRules.Armor(ranks, 0, IncomingDamageKind.Other, .90, 0,
                false, true, true));
            Check.Equal(12, PermanentCombatRules.Armor(ranks, 5, IncomingDamageKind.Projectile, .30, 8,
                true, true, true));
        });

        test("Expanded health tree applies every rank contribution to new challenge health", () =>
        {
            var profile = GameSnapshot.New();
            PermanentProfile.Upgrade(profile);
            profile.Profile.HighestSettledRound = 180;
            var modifiers = new[] { 4, 4, 5, 5, 5, 6, 6, 7, 8 };
            var expected = 150;
            profile.Profile.Upgrades.Add("health", 5);
            profile.Profile.PaidRanks!.Add("health", PermanentRules.LegacyRankPrices("health", 5));
            Check.Near(expected, new AdamSession(profile, WaveTests.Adam(), "bible").CurrentHealth);
            for (var index = 0; index < modifiers.Length; index++)
            {
                profile.Run = null;
                var id = $"common.health.{index + 2:D2}";
                var node = PermanentSkillCatalog.Current.Find(id);
                profile.Profile.Upgrades.Add(id, 1);
                profile.Profile.PaidRanks.Add(id, node.Prices.ToList());
                expected += modifiers[index];
                Check.Near(expected, new AdamSession(profile, WaveTests.Adam(), "bible").CurrentHealth);
                profile.Validate();
            }
            Check.Equal(200, expected);
        });

        test("Expanded motion and collection context survive a live checkpoint and reset for the next round", () =>
        {
            WithStore(store =>
            {
                var state = ExpandedProfile(store, "common.move.08", "common.pickup.07", "common.xp.07");
                var session = new AdamSession(state, WaveTests.Adam(), "sling");
                var world = new BattleWorld(session, Speeds, 7);
                world.Advance(1.1, Vector2.UnitX);
                Check.True(state.Run!.PermanentActions!.Moving);
                Check.Near(0, state.Run.PermanentActions.HeadingSince);
                Check.Near(1.12, session.MoveSpeedMultiplier);
                Check.Near(1.38, session.PickupRangeMultiplier);
                state.Run.World = world.Capture();
                store.Commit(state, state.Revision, "moving-checkpoint");

                var restored = store.Load()!;
                session = AdamSession.Resume(restored, WaveTests.Adam());
                Check.Near(1.12, session.MoveSpeedMultiplier);
                var loadedWorld = BattleWorld.Restore(session, Speeds, restored.Run!.World!);
                loadedWorld.Advance(BattleWorld.FixedStep, Vector2.UnitY);
                Check.Near(1.09, session.MoveSpeedMultiplier);
                var clock = restored.Run.PermanentActions!;
                Check.True(clock.HeadingSince >= 1);
                var target = session.Targets.Keys.First();
                session.Kill(target);
                session.Collect(target);
                Check.Equal(1, clock.GemsCollected);
                Check.Near(session.CombatTime, clock.LastGemAt);
                Check.Near(1.35, session.PickupRangeMultiplier);
                while (session.PendingChoices.Count > 0) Check.True(session.Choose(session.PendingChoices[0]));
                session.Advance(180);
                while (session.PendingChoices.Count > 0) Check.True(session.Choose(session.PendingChoices[0]));
                session.StartNextRound(CatalogReader.Round("content/genesis/catalog.json", 2));
                Check.Equal(0, restored.Run.PermanentActions!.GemsCollected);
                Check.True(!restored.Run.PermanentActions.Moving);
            });
        });

        test("Expanded immediate recovery persists normal-kill count and ignores non-original kills", () =>
        {
            WithStore(store =>
            {
                var state = ExpandedProfile(store, "common.regen.09");
                var session = new AdamSession(state, WaveTests.Adam(), "sling");
                session.Advance(15);
                session.Damage(55);
                var targets = session.Targets.Keys.ToArray();
                Check.True(targets.Length >= 11);
                foreach (var id in targets.Take(9)) session.Hit(id, 1_000);
                Check.Equal(9, state.Run!.PermanentRecovery!.NormalKills);
                store.Commit(state, state.Revision, "before-tenth-kill");
                state = store.Load()!;
                session = AdamSession.Resume(state, WaveTests.Adam());
                var health = session.CurrentHealth;
                session.Hit(targets[9], 1_000, null, original: false);
                Check.Near(health, session.CurrentHealth);
                session.Hit(targets[10], 1_000);
                Check.Near(health + 1, session.CurrentHealth);
                Check.Equal(10, state.Run!.PermanentRecovery!.NormalKills);
                Check.Equal(1, state.Run.PermanentRecovery.ImmediateHeals.Count);
            });
        });

        test("Expanded base critical ranks roll on a real jawbone hit and restore an independent stream", () =>
        {
            WithStore(store =>
            {
                var state = ExpandedProfile(store, "common.crit.02");
                var definition = new RoundDefinition(1, 40, 60, 25, new[]
                {
                    new WaveDefinition(0, 60, new[] { new SpawnGroup("a1", 60, 100, 4) }),
                });
                var session = new AdamSession(state, definition, "jawbone");
                session.Advance(1.1);
                var snapshot = new BattleWorld(session, Speeds, 7).Capture();
                var target = snapshot.Enemies.OrderBy(enemy => enemy.Id).First();
                target.X = 1;
                target.Y = 0;
                snapshot.FacingX = 1;
                snapshot.FacingY = 0;
                state.Run!.CriticalRandomStates = new Dictionary<string, uint> { ["jawbone"] = 1 };
                state.Run.World = snapshot;
                store.Commit(state, state.Revision, "before-original-critical");
                var loaded = store.Load()!;
                var streams = loaded.Run!.CriticalRandomStates;
                loaded.Run.CriticalRandomStates = null;
                Check.Throws<InvalidDataException>(loaded.Validate);
                loaded.Run.CriticalRandomStates = streams;
                var replay = AdamSession.Resume(loaded, definition);
                var world = BattleWorld.Restore(session, Speeds, snapshot);
                var replayWorld = BattleWorld.Restore(replay, Speeds, loaded.Run!.World!);
                var baseDamage = session.AttackProfile("jawbone").Damage;
                world.Advance(BattleWorld.FixedStep, Vector2.Zero);
                replayWorld.Advance(BattleWorld.FixedStep, Vector2.Zero);
                Check.Near(100 - baseDamage * 1.6, session.Targets[target.Id].Health);
                Check.Near(session.Targets[target.Id].Health, replay.Targets[target.Id].Health);
                Check.Equal(state.Run.CriticalRandomStates["jawbone"], loaded.Run.CriticalRandomStates!["jawbone"]);
                Check.True(state.Run.CriticalRandomStates["jawbone"] != 1);
                Check.Equal(snapshot.RandomState, world.Capture().RandomState);
            });
        });

        test("Starter masteries reach real book clocks, outer jawbone hits and saved sling projectiles", () =>
        {
            WithStore(store =>
            {
                var state = ExpandedProfile(store, "weapon.bible.base.05", "weapon.sling.base.05", "weapon.jawbone.base.05");
                var definition = WaveTests.Adam();
                var session = new AdamSession(state, definition, "bible");
                var world = new BattleWorld(session, Speeds, 7);
                world.Advance(BattleWorld.FixedStep, Vector2.Zero);
                var bible = session.AttackProfile("bible");
                var clock = world.Capture().Weapons.Single();
                Check.Near(bible.Cycle - .75, clock.NextCycle - clock.ActiveSince);
                Check.Near(1.12, bible.OrbitCollisionMultiplier);
                Check.Near(1, bible.PulseRadiusMultiplier);
                Check.True(state.Run!.FirstBibleAttackUsed);
                state.Run.World = world.Capture();
                store.Commit(state, state.Revision, "first-bible-wait");
                state = store.Load()!;
                Check.True(state.Run!.FirstBibleAttackUsed);

                state = SaveStore.PrepareNewChallenge(state);
                session = new AdamSession(state, definition, "bible");
                const string catalog = "content/genesis/catalog.json";
                session.Advance(180);
                session = session.StartNextRound(CatalogReader.Round(catalog, 2));
                session.Advance(180);
                session = session.StartNextRound(CatalogReader.Round(catalog, 3));
                for (var rank = 1; rank < 8; rank++) Check.True(session.Inventory.TryChoose("bible", false));
                Check.True(session.Inventory.TryChoose("scroll", true));
                Check.True(session.Inventory.TryEvolve(EvolutionRules.Bible));
                world = new BattleWorld(session, CatalogReader.MonsterSpeeds(catalog), 7);
                world.Advance(BattleWorld.FixedStep, Vector2.Zero);
                clock = world.Capture().Weapons.Single();
                Check.True(clock.StrengthenedFirstPulse);
                Check.Near(session.AttackProfile("bible").Duration, clock.NextCycle - clock.ActiveSince);
                Check.Near(1.10 * EvolutionRules.PulseRadius, clock.FrozenPulseRadius);
                Check.True(session.Inventory.TryChoose("whirlwind", true));
                world.Advance(BattleWorld.FixedStep, Vector2.Zero);
                Check.Near(clock.FrozenPulseRadius, world.Capture().Weapons.Single().FrozenPulseRadius);
                state.Run!.World = world.Capture();
                store.Commit(state, state.Revision, "first-evolved-bible-pulse");
                var savedPulse = store.Load()!.Run!.World!.Weapons.Single();
                Check.True(savedPulse.StrengthenedFirstPulse);
                Check.Near(clock.FrozenPulseRadius, savedPulse.FrozenPulseRadius);

                state = SaveStore.PrepareNewChallenge(store.Load()!);
                session = new AdamSession(state, definition, "sling");
                Check.True(session.Inventory.TryChoose("jawbone", false));
                var jawbone = session.AttackProfile("jawbone");
                Check.Near(1.1, jawbone.AreaMultiplier);
                Check.Near(jawbone.Damage, session.JawboneDamage(jawbone.Damage, .799, 1));
                Check.Near(jawbone.Damage + .8, session.JawboneDamage(jawbone.Damage, .8, 1));
                session.Advance(1.2);
                var snapshot = new BattleWorld(session, Speeds, 7).Capture();
                foreach (var enemy in snapshot.Enemies)
                {
                    enemy.X = 4 + enemy.Id;
                    enemy.Y = 0;
                }
                world = BattleWorld.Restore(session, Speeds, snapshot);
                world.Advance(BattleWorld.FixedStep, Vector2.Zero);
                var projectile = world.Capture().Projectiles.First();
                Check.Near(.11, projectile.Radius);
                Check.Near(snapshot.Tuning.SlingSpeed * 1.12, projectile.Velocity.Length(), 1e-5);
                state.Run!.World = world.Capture();
                store.Commit(state, state.Revision, "expanded-sling-projectile");
                var loaded = store.Load()!.Run!.World!.Projectiles.First();
                Check.Near(projectile.Radius, loaded.Radius);
                Check.Equal(projectile.VelocityX, loaded.VelocityX);
            });
        });

        test("Purchased lightning radius and delay reach the actual frozen strike and restore it", () =>
        {
            WithStore(store =>
            {
                var state = ExpandedProfile(store, "weapon.lightning.base.05");
                var definition = WaveTests.Adam();
                var session = new AdamSession(state, definition, "sling");
                Check.True(session.Inventory.TryChoose("lightning", false));
                session.Advance(1.5);
                var initial = new BattleWorld(session, Speeds, 7).Capture();
                foreach (var enemy in initial.Enemies)
                {
                    enemy.X = 3 + enemy.Id;
                    enemy.Y = 0;
                }
                var world = BattleWorld.Restore(session, Speeds, initial);
                world.Advance(BattleWorld.FixedStep, Vector2.Zero);
                var snapshot = world.Capture();
                var strike = snapshot.Lightning!.Pending.First();
                Check.Near(.88, strike.Radius);
                Check.Near(.07, strike.At - snapshot.Time);
                state.Run!.World = snapshot;
                store.Commit(state, state.Revision, "expanded-lightning-geometry");
                var saved = store.Load()!;
                var restored = BattleWorld.Restore(AdamSession.Resume(saved, definition), Speeds, saved.Run!.World!);
                Check.Equal(JsonSerializer.Serialize(snapshot.Lightning), JsonSerializer.Serialize(restored.Capture().Lightning));
            });
        });

        test("P01 pickup purchase and refund affect only new challenges and cross the same drop boundary", () =>
        {
            WithStore(store =>
            {
                var state = GameSnapshot.New();
                state.Profile.Gold = 60;
                var active = new AdamSession(state, WaveTests.Adam(), "bible");
                PutDropAtPickupBoundary(state, active);
                store.Commit(state, 0, "legacy-paused-checkpoint");
                var originalRunId = state.Run!.RunId;
                state = Buy(store, store.Load()!, "pickup", 60);
                var resumed = AdamSession.Resume(state, WaveTests.Adam());
                Check.Equal(1, resumed.CombatRulesVersion);
                Check.Equal(originalRunId, state.Run!.RunId);
                Check.Near(1, resumed.PickupRangeMultiplier);
                Check.Equal(0, CollectFixtureDrop(resumed, state));

                state.Run = null;
                var upgraded = new AdamSession(state, WaveTests.Adam(), "bible");
                PutDropAtPickupBoundary(state, upgraded);
                Check.Equal(2, upgraded.CombatRulesVersion);
                Check.Equal(1, state.Run!.PermanentCatalogVersion!.Value);
                Check.Near(1.10, upgraded.PickupRangeMultiplier);
                store.Commit(state, state.Revision, "new-challenge-before-refund");

                state = store.Load()!;
                var refund = PermanentSkillCommand.QuoteRefund(state.Profile);
                var refunded = store.TransactPermanent(PermanentSkillCommand.RefundAll(
                    PermanentSkillCommand.NewOperationId(state.Revision), state.Revision, refund.Gold, refund.Digest));
                Check.Equal(60L, refunded.Profile.Gold);
                Check.Equal(state.Run!.RunId, refunded.Run!.RunId);
                Check.Equal(1, refunded.Run.PermanentSnapshot["pickup"]);
                var afterRefund = AdamSession.Resume(refunded, WaveTests.Adam());
                Check.Equal(1, CollectFixtureDrop(afterRefund, refunded));
                refunded.Run = null;
                Check.Near(1, new AdamSession(refunded, WaveTests.Adam(), "bible").PickupRangeMultiplier);
            });
        });

        test("P02 sling ranks are scoped to the equipped weapon and a v2 volley survives checkpoint restoration", () =>
        {
            WithStore(store =>
            {
                var state = GameSnapshot.New();
                state.Profile.Gold = 600;
                store.Commit(state, 0, "fund");
                state = Buy(store, store.Load()!, "attack", 100);
                state = Buy(store, state, "weapon.sling.base.01", 150);
                state = Buy(store, state, "weapon.sling.base.03", 350);
                Check.Equal(0L, state.Profile.Gold);
                var game = new AdamSession(state, WaveTests.Adam(), "bible");
                Check.Equal(1, game.AttackProfile("bible").Amount);
                Check.Near(10.30, game.AttackProfile("bible").Damage);
                Check.True(game.Inventory.TryChoose("sling", false));
                Check.Equal(2, game.AttackProfile("sling").Amount);
                Check.Near(11.30, game.AttackProfile("sling").Damage);
                Check.True(game.Inventory.TryChoose("sling", false));
                Check.Equal(3, game.AttackProfile("sling").Amount);
                for (var rank = 2; rank < 8; rank++) Check.True(game.Inventory.TryChoose("sling", false));
                for (var rank = 0; rank < 2; rank++) Check.True(game.Inventory.TryChoose("stones", true));
                Check.Equal(7, game.AttackProfile("sling").Amount);

                var original = new BattleWorld(game, Speeds, 7);
                for (var tick = 0; tick < 360 && !original.Capture().Weapons.Any(
                    clock => clock.WeaponId == "sling" && clock.PendingBursts.Count >= 2); tick++)
                    original.Advance(BattleWorld.FixedStep, Vector2.Zero);
                game.Capture();
                state.Run!.World = original.Capture();
                Check.Equal(6, state.Run.World.SchemaVersion);
                var volley = state.Run.World.Weapons.Single(clock => clock.WeaponId == "sling");
                Check.True(volley.PendingBursts.Count >= 2);
                Check.Near(.10, volley.PendingBursts[1] - volley.PendingBursts[0]);
                store.Commit(state, state.Revision, "mid-volley");
                var loaded = store.Load()!;
                var resumedGame = AdamSession.Resume(loaded, WaveTests.Adam());
                var resumed = BattleWorld.Restore(resumedGame, Speeds, loaded.Run!.World!);
                for (var tick = 0; tick < 45; tick++)
                {
                    original.Advance(BattleWorld.FixedStep, Vector2.Zero);
                    resumed.Advance(BattleWorld.FixedStep, Vector2.Zero);
                }
                Check.Equal(JsonSerializer.Serialize(original.Capture()), JsonSerializer.Serialize(resumed.Capture()));
                Check.True(resumedGame.Inventory.TryEvolve(EvolutionRules.Sling));
                Check.Equal(8, resumedGame.AttackProfile("sling").Amount);
                Check.Equal(6, CombatStats.For("sling", 8, new CombatBonuses { ExtraAmount = 2 })
                    .Evolved(EvolutionRules.Sling).Amount);
            });
        });
    }

    private static void PutDropAtPickupBoundary(GameSnapshot state, AdamSession session)
    {
        var world = new BattleWorld(session, Speeds, 7);
        world.Advance(2, Vector2.Zero);
        session.Capture();
        var worldState = world.Capture();
        var target = worldState.Enemies.First();
        session.Kill(target.Id);
        Check.True(session.DroppedExperience.ContainsKey(target.Id));
        session.Capture();
        worldState.Enemies.Remove(target);
        worldState.Drops.Add(new BattleEntityState { Id = target.Id, X = worldState.PlayerX + .74f, Y = worldState.PlayerY });
        state.Run!.World = worldState;
    }

    private static int CollectFixtureDrop(AdamSession session, GameSnapshot state)
    {
        session.Pause(false);
        var arena = BattleWorld.Restore(session, Speeds, state.Run!.World!);
        arena.Advance(BattleWorld.FixedStep, Vector2.Zero);
        return arena.Collected;
    }

    private static GameSnapshot ExpandedProfile(SaveStore store, params string[] desired)
    {
        var state = SaveStore.PrepareNewChallenge(GameSnapshot.New());
        state.Profile.HighestSettledRound = 180;
        state.Profile.Gold = 100_000;
        if (desired.Any(id => id.StartsWith("weapon.lightning.", StringComparison.Ordinal)))
            state.Profile.UnlockedWeapons.Add("lightning");
        store.Commit(state, state.Revision, "seed-settled-profile");
        state = store.Load()!;
        foreach (var id in desired) BuyPrerequisites(id, 1);
        return state;

        void BuyPrerequisites(string id, int rank)
        {
            if (PermanentRules.Rank(state.Profile.Upgrades, id) >= rank) return;
            var node = PermanentSkillCatalog.Current.Find(id);
            foreach (var requirement in node.Prerequisites) BuyPrerequisites(requirement.Key, requirement.Value);
            var currentRank = PermanentRules.Rank(state.Profile.Upgrades, id);
            for (; currentRank < rank; currentRank++)
                state = store.TransactPermanent(PermanentSkillCommand.Purchase(
                    PermanentSkillCommand.NewOperationId(state.Revision), state.Revision, id,
                    currentRank, node.Prices[currentRank]));
        }
    }

    private static GameSnapshot Buy(SaveStore store, GameSnapshot state, string id, int price) =>
        store.TransactPermanent(PermanentSkillCommand.Purchase(
            PermanentSkillCommand.NewOperationId(state.Revision), state.Revision, id, 0, price));

    private static void WithStore(Action<SaveStore> action)
    {
        var directory = Path.Combine(Path.GetTempPath(), "skill-combat-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try { action(new SaveStore(Path.Combine(directory, "profile.json"))); }
        finally { Directory.Delete(directory, true); }
    }
}
