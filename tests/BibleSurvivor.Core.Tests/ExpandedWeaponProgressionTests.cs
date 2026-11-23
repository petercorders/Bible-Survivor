using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text.Json;
using BibleSurvivor.Sim;

namespace BibleSurvivor.Core.Tests;

internal static class ExpandedWeaponProgressionTests
{
    private static readonly Dictionary<string, double> Speeds =
        new() { ["a1"] = .45, ["a2"] = .6, ["a3"] = .35 };

    public static void Register(Action<string, Action> test)
    {
        test("Expanded R02 freezes growth candidates across lightning unlock and reopen", () =>
        {
            var state = GameSnapshot.New();
            PermanentProfile.Upgrade(state);
            var game = new AdamSession(state, WaveTests.Adam(), "sling");
            game.Advance(180);
            var roundTwo = CatalogReader.Round("content/genesis/catalog.json", 2);
            game = game.StartNextRound(roundTwo);
            game.Advance(25);
            foreach (var target in game.Targets.Keys.ToArray()) game.Kill(target);
            game.Advance(180);
            Check.True(game.PendingChoices.Count > 0);
            var first = game.PendingChoices.ToArray();
            var serial = state.Run!.ChoiceSerial;
            Check.True(state.Profile.UnlockedWeapons.Contains("lightning"));
            Check.True(!state.Run.GrowthChoiceUnlockedWeapons!.Contains("lightning"));
            Check.Equal(2, state.Profile.HighestSettledRound!.Value);
            WithStore(store =>
            {
                store.Commit(state, 0, "post-r02-level-choice");
                var restored = store.Load()!;
                game = AdamSession.Resume(restored, roundTwo);
                Check.True(first.SequenceEqual(game.PendingChoices));
                Check.True(game.Choose(first[0]));
                Check.Equal(serial + 1, restored.Run!.ChoiceSerial);
                Check.True(restored.Run.PendingLevels > 0);
                Check.True(restored.Run.GrowthChoiceUnlockedWeapons!.Contains("lightning"));
            });
        });

        test("Expanded lightning weapon publishes a delayed world attack and resumes the same hit", () =>
        {
            var state = GameSnapshot.New();
            state.Profile.Gold = 600;
            state.Profile.UnlockedWeapons.Add("lightning");
            PermanentProfile.Upgrade(state);
            var definition = WaveTests.Adam();
            var game = new AdamSession(state, definition, "bible");
            Check.True(game.Inventory.TryChoose("lightning", false));
            Check.True(!new EquipmentInventory("bible").TryChoose("lightning", false));
            var arena = new BattleWorld(game, Speeds, 7);
            for (var tick = 0; tick < 900 && arena.Capture().Lightning?.Pending.Count is not > 0; tick++)
            {
                var nearest = arena.EnemyPositions.Values.OrderBy(position =>
                    Vector2.DistanceSquared(position, arena.PlayerPosition)).FirstOrDefault();
                var move = arena.EnemyPositions.Count == 0 ? Vector2.UnitX : nearest - arena.PlayerPosition;
                arena.Advance(BattleWorld.FixedStep, move);
            }
            game.Capture();
            state.Run!.World = arena.Capture();
            Check.True(state.Run.World.Lightning?.Pending.Count > 0);
            var strike = state.Run.World.Lightning!.Pending[0];
            Check.True(strike.At > arena.SimulationTime);
            Check.Near(20, strike.Damage);
            var savedLightning = state.Run.World.Lightning;
            state.Run.World.Lightning = null;
            Check.Throws<ArgumentException>(() => state.Run.World.Validate());
            state.Run.World.Lightning = savedLightning;
            var assignedTarget = strike.AssignedTargetId;
            strike.AssignedTargetId = state.Run.Wave!.Cursor + 1;
            Check.Throws<InvalidDataException>(() => state.Validate());
            strike.AssignedTargetId = assignedTarget;
            var motion = state.Run.PermanentActions;
            state.Run.PermanentActions = null;
            Check.Throws<InvalidDataException>(() => state.Validate());
            state.Run.PermanentActions = motion;
            WithStore(store =>
            {
                store.Commit(state, 0, "lightning-mid-telegraph");
                var loaded = store.Load()!;
                var restored = BattleWorld.Restore(AdamSession.Resume(loaded, definition), Speeds, loaded.Run!.World!);
                var hits = arena.Hits + arena.Kills;
                for (var tick = 0; tick < 20; tick++)
                {
                    arena.Advance(BattleWorld.FixedStep, Vector2.Zero);
                    restored.Advance(BattleWorld.FixedStep, Vector2.Zero);
                }
                Check.True(arena.Hits + arena.Kills > hits);
                Check.Equal(JsonSerializer.Serialize(arena.Capture()), JsonSerializer.Serialize(restored.Capture()));
            });
        });

        test("Expanded boss reward checks captured unlocks and v1 retains its evolution policy", () =>
        {
            var weapons = new Dictionary<string, int> { ["sling"] = 8, ["lightning"] = 8 };
            var passives = new Dictionary<string, int> { ["thunder"] = 1, ["stones"] = 1 };
            var unlocked = new[] { "sling", "lightning" };
            Check.True(EvolutionRules.Eligibility(weapons, passives, null).SequenceEqual(new[] { EvolutionRules.Sling }));
            Check.True(EvolutionRules.Eligibility(weapons, passives, null, 2, unlocked)
                .SequenceEqual(new[] { "thunder_judgment", EvolutionRules.Sling }));
            var reward = new BossRewardState
            {
                RulesVersion = 2, Weapons = weapons, Passives = passives, UnlockedWeapons = unlocked.ToList(),
                EligibleEvolutions = EvolutionRules.Eligibility(weapons, passives, null, 2, unlocked),
                RewardId = "expanded-boss-reward",
            };
            reward.Candidates = BossRewards.Generate(reward);
            var copy = reward.Copy();
            copy.Candidates = BossRewards.Generate(copy);
            Check.True(reward.Candidates.Zip(copy.Candidates, (first, second) => first.Same(second)).All(matches => matches));
            Check.True(reward.Candidates.Take(2).All(candidate => candidate.Kind == "evolution"));
            reward.UnlockedWeapons.Remove("lightning");
            Check.True(!BossRewards.Generate(reward).Any(candidate => candidate.ItemId == "thunder_judgment"));
        });

        test("Expanded R03 lightning evolution reward survives reopening and records its achievement", () =>
        {
            WithStore(store =>
            {
                const string catalog = "content/genesis/catalog.json";
                var state = GameSnapshot.New();
                state.Profile.UnlockedWeapons.Add("lightning");
                PermanentProfile.Upgrade(state);
                var session = new AdamSession(state, WaveTests.Adam(), "bible");
                session.Advance(180);
                session = session.StartNextRound(CatalogReader.Round(catalog, 2));
                session.Advance(180);
                var round = CatalogReader.Round(catalog, 3);
                session = session.StartNextRound(round);
                for (var rank = 0; rank < 8; rank++) Check.True(session.Inventory.TryChoose("lightning", false));
                Check.True(session.Inventory.TryChoose("thunder", true));
                session.Advance(180);
                session.Advance(2);
                session.Hit(session.BossRules!.BossActorId, session.BossRules.MaxHealth);
                Check.Equal("boss_reward", session.Stage);
                var candidate = session.BossChoices.Single(choice => choice.ItemId == "thunder_judgment");
                store.Commit(state, 0, "v2-lightning-evolution-offered");

                state = store.Load()!;
                session = AdamSession.Resume(state, round);
                Check.True(session.BossChoices.Any(choice => choice.Id == candidate.Id));
                Check.True(session.ChooseBossReward(session.Boss!.Reward!.RewardId, candidate.Id));
                store.Commit(state, state.Revision, "v2-lightning-evolution-accepted");
                var loaded = store.Load()!;
                Check.True(loaded.Profile.EvolvedWeapons!.Contains("lightning"));
                Check.Equal("thunder_judgment", loaded.Run!.Forms!["lightning"]);
            });
        });

        test("All seven expanded weapons open both attack forms and restore their actual world state", () =>
        {
            var catalog = "content/genesis/catalog.json";
            var roundThree = CatalogReader.Round(catalog, 3);
            var speeds = CatalogReader.MonsterSpeeds(catalog);
            foreach (var weapon in ExpandedWeaponRules.All)
            foreach (var evolved in new[] { false, true })
            {
                WithStore(store =>
                {
                    var state = GameSnapshot.New();
                    state.Profile.UnlockedWeapons.Add(weapon.WeaponId);
                    PermanentProfile.Upgrade(state);
                    var game = new AdamSession(state, WaveTests.Adam(), "bible");
                    game.Advance(180);
                    game = game.StartNextRound(CatalogReader.Round(catalog, 2));
                    game.Advance(180);
                    game = game.StartNextRound(roundThree);
                    for (var rank = 0; rank < (evolved ? 8 : 1); rank++)
                        Check.True(game.Inventory.TryChoose(weapon.WeaponId, false));
                    if (evolved)
                    {
                        Check.True(game.Inventory.TryChoose(weapon.PassiveId, true));
                        Check.True(game.Inventory.TryEvolve(weapon.EvolutionId));
                        state.Profile.EvolvedWeapons!.Add(weapon.WeaponId);
                    }
                    var world = new BattleWorld(game, speeds, 7);
                    for (var tick = 0; tick < 60; tick++) world.Advance(BattleWorld.FixedStep, Vector2.UnitX);
                    game.Capture();
                    state.Run!.World = world.Capture();
                    Check.True(state.Run.World.Weapons.Any(clock => clock.WeaponId == weapon.WeaponId &&
                        clock.WeaponForm == (evolved ? weapon.EvolutionId : null)));
                    store.Commit(state, 0, "expanded-attack");
                    var loaded = store.Load()!;
                    var restored = BattleWorld.Restore(AdamSession.Resume(loaded, roundThree), speeds, loaded.Run!.World!);
                    for (var tick = 0; tick < 20; tick++)
                    {
                        world.Advance(BattleWorld.FixedStep, Vector2.Zero);
                        restored.Advance(BattleWorld.FixedStep, Vector2.Zero);
                    }
                    Check.Equal(JsonSerializer.Serialize(world.Capture()), JsonSerializer.Serialize(restored.Capture()));
                });
            }
        });
    }

    private static void WithStore(Action<SaveStore> action)
    {
        var directory = Path.Combine(Path.GetTempPath(), "expanded-weapon-progress-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try { action(new SaveStore(Path.Combine(directory, "save.json"))); }
        finally { Directory.Delete(directory, true); }
    }
}
