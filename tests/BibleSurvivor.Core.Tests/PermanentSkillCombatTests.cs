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

        test("The remaining six expanded weapon masteries reach real persisted attack shapes", () =>
        {
            WithStore(store =>
            {
                var weapons = ExpandedWeaponRules.All.Where(weapon => weapon.WeaponId != "lightning").ToArray();
                var state = ExpandedProfile(store, weapons.Select(weapon => $"weapon.{weapon.WeaponId}.base.05").ToArray());
                var definition = WaveTests.Adam();
                foreach (var weapon in weapons)
                {
                    state = SaveStore.PrepareNewChallenge(state);
                    var id = weapon.WeaponId;
                    var session = new AdamSession(state, definition, "sling");
                    Check.True(session.Inventory.TryChoose(id, false));
                    session.Advance(1.5);
                    var setup = new BattleWorld(session, Speeds, 7).Capture();
                    foreach (var enemy in setup.Enemies)
                    {
                        enemy.X = 2 + enemy.Id;
                        enemy.Y = 0;
                    }
                    var world = BattleWorld.Restore(session, Speeds, setup);
                    world.Advance(BattleWorld.FixedStep, Vector2.Zero);
                    var captured = world.Capture();
                    state.Run!.World = captured;
                    store.Commit(state, state.Revision, "expanded-mastery-" + id);
                    state = store.Load()!;
                    var restored = state.Run!.World!;
                    var basis = weapon.AtLevel(1);
                    var stats = session.AttackProfile(id);
                    switch (id)
                    {
                        case "wilderness_voice":
                            var voice = restored.WildernessVoice!.Active.Single();
                            Check.Near(basis.Reach * 1.30, voice.Reach);
                            Check.Near(.07, voice.FirstDelay!.Value);
                            break;
                        case "fire_pillar":
                            var pillar = restored.FirePillar!.Regions.First();
                            Check.Near(basis.Radius * 1.10, pillar.Radius);
                            Check.Near(.07, pillar.IgnitionAt - pillar.StartedAt);
                            break;
                        case "moses_staff":
                            var wave = restored.MosesStaff!.Waves.First();
                            Check.Near(weapon.TravelDistance * 1.12, wave.Range);
                            Check.Near(basis.Speed * 1.12, wave.Speed);
                            break;
                        case "cross":
                            var shot = restored.Cross!.Shots.First();
                            Check.Near(weapon.TravelDistance * 1.12, shot.Range);
                            Check.Near(basis.Radius * 1.10, shot.Radius);
                            break;
                        case "daniel_lion":
                            Check.True(restored.DanielLion!.Lions.Count > 0);
                            Check.Near(basis.Speed * 1.12, stats.AttackSpeed);
                            Check.Near(basis.Radius * 1.10, stats.AttackRadius);
                            break;
                        case "elijah_chariot":
                            var path = restored.ElijahChariot!.Paths.First();
                            Check.Near(basis.Width * 1.10, path.Width);
                            Check.Near(.07, path.MoveAt - path.WarningAt);
                            break;
                    }
                }
            });
        });

        test("Starter orbit speed and jawbone angle change actual contact without affecting the baseline", () =>
        {
            WithStore(store =>
            {
                var profile = ExpandedProfile(store, "weapon.bible.a.01", "weapon.jawbone.a.01");
                var definition = new RoundDefinition(1, 40, 60, 25, new[]
                {
                    new WaveDefinition(0, 60, new[] { new SpawnGroup("a1", 60, 100, 4) }),
                });
                foreach (var weapon in new[] { "bible", "jawbone" })
                {
                    var owned = SaveStore.PrepareNewChallenge(profile);
                    var session = new AdamSession(owned, definition, weapon);
                    session.Advance(15);
                    var snapshot = new BattleWorld(session, Speeds, 7).Capture();
                    var target = snapshot.Enemies[0];
                    var angle = weapon == "bible"
                        ? (snapshot.Time + BattleWorld.FixedStep) * 2.5 * 1.12 : 65 * Math.PI / 180;
                    var radius = weapon == "bible" ? snapshot.Tuning.BibleOrbitRadius : 1.1;
                    target.X = (float)(Math.Cos(angle) * radius);
                    target.Y = (float)(Math.Sin(angle) * radius);
                    foreach (var outside in snapshot.Enemies.Skip(1))
                    {
                        outside.X = 100;
                        outside.Y = 100;
                    }
                    var beforeAcquisition = SaveStore.Copy(owned);
                    beforeAcquisition.Run!.PermanentSnapshot.Remove($"weapon.{weapon}.a.01");
                    var baseline = AdamSession.Resume(beforeAcquisition, definition);
                    var baselineWorld = BattleWorld.Restore(baseline, Speeds, snapshot);
                    var upgradedWorld = BattleWorld.Restore(session, Speeds, snapshot);
                    baselineWorld.Advance(BattleWorld.FixedStep, Vector2.Zero);
                    upgradedWorld.Advance(BattleWorld.FixedStep, Vector2.Zero);
                    Check.Near(100, baseline.Targets[target.Id].Health);
                    Check.True(session.Targets[target.Id].Health < 100);
                }
            });
        });

        test("Purchased staff lane choices survive an evolved World6 checkpoint", () =>
        {
            WithStore(store =>
            {
                var state = ExpandedProfile(store, "weapon.moses_staff.e.03");
                const string catalog = "content/genesis/catalog.json";
                var first = CatalogReader.Round(catalog, 1);
                var session = new AdamSession(state, first, "sling");
                session.Advance(180);
                session = session.StartNextRound(CatalogReader.Round(catalog, 2));
                session.Advance(180);
                var third = CatalogReader.Round(catalog, 3);
                session = session.StartNextRound(third);
                for (var rank = 0; rank < 8; rank++) Check.True(session.Inventory.TryChoose("moses_staff", false));
                var staff = ExpandedWeaponRules.Get("moses_staff");
                Check.True(session.Inventory.TryChoose(staff.PassiveId, true));
                Check.True(session.Inventory.TryEvolve(staff.EvolutionId));
                var arena = new BattleWorld(session, CatalogReader.MonsterSpeeds(catalog), 7);
                arena.Advance(.25, Vector2.Zero);
                state.Run!.World = arena.Capture();
                store.Commit(state, state.Revision, "evolved-staff-lane");
                var saved = store.Load()!;
                var wave = saved.Run!.World!.MosesStaff!.Waves.First();
                Check.True(wave.WidenedLane && wave.EmpoweredLaneEdge && wave.ExtendedLane);
                Check.Near(1.38, wave.LaneWidth);
                Check.Near(2.4, wave.LaneDuration);
            });
        });

        test("Sling aims at an in-range elite and rolls its weapon-only critical on impact", () =>
        {
            WithStore(store =>
            {
                var state = ExpandedProfile(store, "weapon.sling.a.02");
                var definition = new RoundDefinition(1, 40, 60, 25, new[]
                {
                    new WaveDefinition(0, 60, new[] { new SpawnGroup("a1", 30, 10, 4), new SpawnGroup("a2", 30, 100, 4) }),
                });
                var session = new AdamSession(state, definition, "sling");
                session.Advance(8);
                var setup = new BattleWorld(session, Speeds, 7).Capture();
                var ordinary = session.Targets.Values.First(enemy => enemy.MonsterId == "a1").Id;
                var elite = session.Targets.Values.First(enemy => enemy.MonsterId == "a2").Id;
                foreach (var enemy in setup.Enemies)
                {
                    enemy.X = enemy.Id == ordinary ? 1.5f : enemy.Id == elite ? 0 : 50;
                    enemy.Y = enemy.Id == ordinary ? 0 : enemy.Id == elite ? 2.5f : 50;
                }
                state.Run!.CriticalRandomStates!["sling"] = 600;
                var roll = PermanentOriginalHitRules.NextCriticalState(state.Run.RunId, 1, "sling", 600) / 4294967296d;
                Check.True(roll > .03 && roll < .06);
                var world = BattleWorld.Restore(session, Speeds, setup);
                world.Advance(BattleWorld.FixedStep, Vector2.Zero);
                var launched = world.Capture().Projectiles.First();
                Check.True(launched.VelocityY > Math.Abs(launched.VelocityX));
                state.Run.World = world.Capture();
                store.Commit(state, state.Revision, "elite-sling-release");
                state = store.Load()!;
                session = AdamSession.Resume(state, definition);
                world = BattleWorld.Restore(session, Speeds, state.Run!.World!);
                for (var tick = 0; tick < 60 && world.Hits == 0; tick++)
                    world.Advance(BattleWorld.FixedStep, Vector2.Zero);
                Check.Near(10, session.Targets[ordinary].Health);
                Check.Near(100 - session.AttackProfile("sling").Damage * 1.5, session.Targets[elite].Health);
            });
        });

        test("Sling burst timing, pierce and critical multiplier stack only for its equipped shots", () =>
        {
            WithStore(store =>
            {
                var state = ExpandedProfile(store, "weapon.sling.a.03", "weapon.sling.b.01", "weapon.sling.d.01",
                    "weapon.sling.base.03", "common.crit.02", "common.tempo.09", "common.shot.03");
                var definition = WaveTests.Adam();
                var session = new AdamSession(state, definition, "sling");
                Check.True(session.Inventory.TryChoose("jawbone", false));
                Check.Near(.08, session.AttackProfile("sling").BurstInterval);
                Check.Near(.15 * .92, session.AttackProfile("jawbone").BurstInterval);
                var ranks = state.Run!.PermanentSnapshot;
                Check.Near(1.8, PermanentOriginalHitRules.BaseCritical(ranks, "sling", 10).CriticalMultiplier);
                Check.Near(1.6, PermanentOriginalHitRules.BaseCritical(ranks, "jawbone", 10).CriticalMultiplier);

                session.Advance(2);
                var battle = new BattleWorld(session, Speeds, 7).Capture();
                foreach (var enemy in battle.Enemies) enemy.X = 4;
                var world = BattleWorld.Restore(session, Speeds, battle);
                world.Advance(BattleWorld.FixedStep, Vector2.Zero);
                var frozen = world.Capture();
                var sling = frozen.Weapons.Single(weapon => weapon.WeaponId == "sling");
                Check.Near(.08, sling.PendingBursts.Single() - sling.ActiveSince);
                Check.Equal(3, frozen.Projectiles.Single().RemainingPierce);
                state.Run.World = frozen;
                store.Commit(state, state.Revision, "sling-burst-bonus");
                Check.Equal(3, store.Load()!.Run!.World!.Projectiles.Single().RemainingPierce);
            });
        });

        test("Common motion and duration ranks change their own dimensions without increasing sling travel", () =>
        {
            WithStore(store =>
            {
                var state = ExpandedProfile(store, "common.shot.03", "common.duration.01", "weapon.cross.c.01");
                var definition = WaveTests.Adam();
                var session = new AdamSession(state, definition, "sling");
                Check.True(session.Inventory.TryChoose("bible", false));
                Check.True(session.Inventory.TryChoose("cross", false));
                session.Advance(1.5);
                var snapshot = new BattleWorld(session, Speeds, 7).Capture();
                foreach (var enemy in snapshot.Enemies)
                {
                    enemy.X = 3 + enemy.Id;
                    enemy.Y = 0;
                }
                var world = BattleWorld.Restore(session, Speeds, snapshot);
                world.Advance(BattleWorld.FixedStep, Vector2.Zero);
                var frozen = world.Capture();
                var sling = frozen.Projectiles.First();
                var cross = frozen.Cross!.Shots.First();
                var bible = frozen.Weapons.Single(weapon => weapon.WeaponId == "bible");
                Check.Near(3.15, bible.ActiveUntil - bible.ActiveSince);
                Check.Near(.104, sling.Radius);
                Check.Equal(2, sling.RemainingPierce);
                Check.Near(snapshot.Tuning.SlingSpeed * 1.05, sling.Velocity.Length(), 1e-5);
                Check.Near(snapshot.Tuning.SlingSpeed * snapshot.Tuning.SlingLifetime,
                    sling.Velocity.Length() * (sling.Lifetime + BattleWorld.FixedStep), 1e-5);
                Check.Near(.28 * 1.04, cross.Radius);
                Check.Near(6 * 1.05, cross.Speed);
                Check.Near(5, cross.Range);
                Check.Equal(6, cross.HitBudget(false));
                Check.Equal(8, cross.HitBudget(true));
                state.Run!.World = frozen;
                store.Commit(state, state.Revision, "expanded-shared-projectile-pierce");
                Check.Equal(6, store.Load()!.Run!.World!.Cross!.Shots.First().HitBudget(false));
                sling.RemainingPierce = 8;
                sling.HitIds.Add(snapshot.Enemies.First().Id);
                Check.Throws<ArgumentException>(() => frozen.Validate());
            });
        });

        test("Common tempo opening occurs once, contextual wait follows actual events, and no-hit carries into the next round", () =>
        {
            WithStore(store =>
            {
                var state = ExpandedProfile(store, "common.tempo.10");
                var definition = new RoundDefinition(1, 40, 180, 25, new[]
                {
                    new WaveDefinition(0, 170, new[] { new SpawnGroup("a1", 20, 10, 4), new SpawnGroup("a2", 20, 100, 4) }),
                });
                var session = new AdamSession(state, definition, "bible");
                Check.True(session.Inventory.TryChoose("jawbone", false));
                var world = new BattleWorld(session, Speeds, 7);
                world.Advance(BattleWorld.FixedStep, Vector2.Zero);
                var frozen = world.Capture();
                var bible = frozen.Weapons.Single(weapon => weapon.WeaponId == "bible");
                var jawbone = frozen.Weapons.Single(weapon => weapon.WeaponId == "jawbone");
                var bibleBase = WeaponRules.Get("bible", 1);
                var jawboneBase = WeaponRules.Get("jawbone", 1);
                Check.Near(bibleBase.Duration + bibleBase.Cooldown * .79, bible.NextCycle - bible.ActiveSince);
                Check.Near(jawboneBase.Cooldown * .94, jawbone.NextCycle - jawbone.ActiveSince);
                Check.Near(jawboneBase.BurstInterval * .92, session.AttackProfile("jawbone").BurstInterval);
                Check.True(state.Run!.TempoOpeningUsed);
                state.Run.World = frozen;
                store.Commit(state, state.Revision, "tempo-opening");
                state = store.Load()!;
                session = AdamSession.Resume(state, definition);
                Check.True(state.Run!.TempoOpeningUsed);
                session.Advance(10.1);
                Check.Near(jawboneBase.Cooldown * .94, session.AttackProfile("jawbone").Cycle);
                session.Damage(10);
                Check.Near(jawboneBase.Cooldown * .95, session.AttackProfile("jawbone").Cycle);
                session.Advance(4);
                Check.Near(jawboneBase.Cooldown * .98, session.AttackProfile("jawbone").Cycle);
                session.Advance(60);
                var elite = session.Targets.Keys.First(session.IsEliteActor);
                Check.Near(jawboneBase.Cooldown * .90, session.AttackProfile("jawbone").Cycle);
                session.Kill(elite);
                Check.Near(jawboneBase.Cooldown * .85, session.AttackProfile("jawbone").Cycle);
                state.Run.World = null;
                store.Commit(state, state.Revision, "tempo-elite");
                state = store.Load()!;
                session = AdamSession.Resume(state, definition);
                Check.Near(jawboneBase.Cooldown * .85, session.AttackProfile("jawbone").Cycle);
                session.Advance(180);
                while (session.PendingChoices.Count != 0) Check.True(session.Choose(session.PendingChoices[0]));
                Check.Equal("between_rounds", session.Stage);
                Check.Near(10, state.Run!.NoHitCarrySeconds);
                var second = CatalogReader.Round("content/genesis/catalog.json", 2);
                session = session.StartNextRound(second);
                Check.True(!state.Run!.TempoOpeningUsed);
                Check.True(state.Run.LastTempoEliteKillAt == null);
                var level = session.Inventory.Weapons["jawbone"];
                var next = WeaponRules.Get("jawbone", level);
                Check.Near(next.Cooldown * .90, session.AttackProfile("jawbone").Cycle);
                session.Advance(180);
                while (session.PendingChoices.Count != 0) Check.True(session.Choose(session.PendingChoices[0]));
                var third = CatalogReader.Round("content/genesis/catalog.json", 3);
                session = session.StartNextRound(third);
                session.Advance(180);
                Check.Equal("boss_intro", session.Stage);
                var enteringBossCycle = session.AttackProfile("jawbone").Cycle;
                session.Advance(2);
                Check.Equal("boss", session.Stage);
                Check.Near(enteringBossCycle - WeaponRules.Get("jawbone", session.Inventory.Weapons["jawbone"]).Cooldown * .05,
                    session.AttackProfile("jawbone").Cycle);
            });
        });

        test("Chariot branch geometry and knockback survive a schema 4 world checkpoint", () =>
        {
            WithStore(store =>
            {
                var state = ExpandedProfile(store, "weapon.elijah_chariot.e.02", "weapon.elijah_chariot.a.01",
                    "weapon.elijah_chariot.b.03", "weapon.elijah_chariot.d.01", "common.duration.04");
                var session = new AdamSession(state, WaveTests.Adam(), "bible");
                for (var round = 2; round <= 3; round++)
                {
                    session.Advance(180);
                    while (session.PendingChoices.Count != 0) Check.True(session.Choose(session.PendingChoices[0]));
                    session = session.StartNextRound(CatalogReader.Round("content/genesis/catalog.json", round));
                }
                var definition = CatalogReader.Round("content/genesis/catalog.json", 3);
                for (var level = 0; level < 8; level++) Check.True(session.Inventory.TryChoose("elijah_chariot", false));
                var weapon = ExpandedWeaponRules.Get("elijah_chariot");
                Check.True(session.Inventory.TryChoose(weapon.PassiveId, true));
                Check.True(session.Inventory.TryEvolve(weapon.EvolutionId));
                var speeds = CatalogReader.MonsterSpeeds("content/genesis/catalog.json");
                var world = new BattleWorld(session, speeds, 7);
                world.Advance(BattleWorld.FixedStep, Vector2.Zero);
                state.Run!.World = world.Capture();
                store.Commit(state, state.Revision, "chariot-ranks-warning");
                state = store.Load()!;
                session = AdamSession.Resume(state, definition);
                world = BattleWorld.Restore(session, speeds, state.Run!.World!);
                var paths = world.Capture().ElijahChariot!.Paths;
                Check.Equal(2, paths.Count);
                Check.Equal(1, paths[1].StaggerIndex);
                Check.Near(.12, paths[1].WarningAt - paths[0].WarningAt);
                Check.Near(1.2, Vector2.Distance(paths[0].Origin, paths[1].Origin), 1e-6);
                foreach (var path in paths)
                {
                    Check.Near(11.2, path.Range);
                    Check.Near(1.3225, path.WhirlwindRadius);
                    Check.Near(1.965, path.TrailDuration);
                    Check.True(path.ForwardKnockback);
                }
            });
        });

        test("Common fixed hit tempo changes primary pulse schedules without accelerating echoes or embers", () =>
        {
            var positions = new Dictionary<int, Vector2> { [1] = Vector2.UnitX };
            var voice = new ExpandedWildernessVoiceState();
            var voiceHits = new List<ExpandedWildernessVoiceHit>();
            ExpandedWildernessVoice.Step(voice, 0, Vector2.Zero, Vector2.UnitX, 8, true, 15, 2.5, 4.5, 2, 180,
                () => positions, voiceHits.Add, shortenedPrimaryInterval: true);
            var restored = voice.Copy();
            Check.Near(.63, restored.Active.Single().PrimaryAt(1));
            Check.Near(.5, restored.Active.Single().EchoAt(0));
            ExpandedWildernessVoice.Step(restored, .63, Vector2.Zero, Vector2.UnitX, 8, true, 15, 2.5, 4.5, 2, 180,
                () => positions, voiceHits.Add);
            Check.Equal(2, voiceHits.Count(hit => hit.Kind == ExpandedWildernessVoiceHitKind.Primary));
            Check.Equal(1, voiceHits.Count(hit => hit.Kind != ExpandedWildernessVoiceHitKind.Primary));

            var fire = new ExpandedFirePillarState();
            var fireHits = new List<ExpandedFirePillarHit>();
            ExpandedFirePillar.Step(fire, 0, Vector2.Zero, false, 10, 3, 1, .9, 2,
                () => positions, fireHits.Add, shortenedPrimaryInterval: true);
            fire = fire.Copy();
            foreach (var time in new[] { .15, .63 })
                ExpandedFirePillar.Step(fire, time, Vector2.Zero, false, 10, 3, 1, .9, 2,
                    () => positions, fireHits.Add);
            Check.Equal(2, fireHits.Count);
            Check.Near(1.11, fire.Regions.Single().NextHit);
        });

        test("Common duration rank adds a low-health original window and additive evolved residual time", () =>
        {
            WithStore(store =>
            {
                var state = ExpandedProfile(store, "common.duration.04");
                var session = new AdamSession(state, WaveTests.Adam(), "bible");
                Check.Near(3.15, session.AttackProfile("bible").Duration);
                session.Damage(70);
                var world = new BattleWorld(session, Speeds, 7);
                world.Advance(BattleWorld.FixedStep, Vector2.Zero);
                var book = world.Capture().Weapons.Single(weapon => weapon.WeaponId == "bible");
                Check.Near(3.33, book.ActiveUntil - book.ActiveSince);
                var ranks = state.Run!.PermanentSnapshot;
                Check.Near(.05, ExpandedWeaponRules.ResidualDurationBonus(ranks, false));
                Check.Near(.11, ExpandedWeaponRules.ResidualDurationBonus(ranks, true));
            });
        });

        test("Common area ranks freeze separate hit, orbit, projectile and placed widths in a restored world", () =>
        {
            WithStore(store =>
            {
                var state = ExpandedProfile(store, "common.area.06", "weapon.wilderness_voice.b.01",
                    "weapon.cross.b.04", "weapon.cross.c.04", "weapon.elijah_chariot.a.01", "weapon.jawbone.a.01",
                    "weapon.lightning.base.01");
                var definition = WaveTests.Adam();
                var session = new AdamSession(state, definition, "bible");
                foreach (var weapon in new[] { "sling", "wilderness_voice", "lightning", "cross", "elijah_chariot" })
                    Check.True(session.Inventory.TryChoose(weapon, false));
                session.Advance(2);
                var setup = new BattleWorld(session, Speeds, 7).Capture();
                foreach (var entry in setup.Enemies)
                {
                    entry.X = 5;
                    entry.Y = 0;
                }
                var world = BattleWorld.Restore(session, Speeds, setup);
                world.Advance(BattleWorld.FixedStep, Vector2.Zero);
                var frozen = world.Capture();
                var book = frozen.Weapons.First(weapon => weapon.WeaponId == "bible");
                Check.Near(setup.Tuning.BibleOrbitRadius * 1.05, book.FrozenOrbitRadius);
                Check.Near(setup.Tuning.BookRadius * 1.04, book.FrozenOrbitHitRadius);
                Check.Equal(1, book.FrozenOrbitCount);
                Check.Near(148, session.JawboneArcDegrees);
                var stone = frozen.Projectiles.First();
                Check.Near(.105, stone.Radius);
                Check.Near(setup.Tuning.SlingLifetime * 1.05, stone.Lifetime + BattleWorld.FixedStep);
                var shot = frozen.Cross!.Shots.First();
                Check.Near(.28 * 1.13, shot.Radius);
                Check.Near(5.25, shot.Range);
                Check.True(shot.CurvedReturn && shot.PreviouslyHitReturnDamage && shot.LaunchIndex == 0);
                var cast = frozen.WildernessVoice!.Active.First();
                Check.Near(57, cast.HalfAngle);
                Check.Near(3.12, cast.Reach);
                Check.Near(.8 * 1.09, frozen.Lightning!.Pending.First().Radius);
                var chariot = frozen.ElijahChariot!.Paths.First();
                Check.Near(11.7, chariot.Range);
                Check.Near(1.2 * 1.04, chariot.Width);
                state.Run!.World = frozen;
                store.Commit(state, state.Revision, "frozen-area-geometry");
                state = store.Load()!;
                session = AdamSession.Resume(state, definition);
                var restored = BattleWorld.Restore(session, Speeds, state.Run!.World!).Capture();
                Check.Near(book.FrozenOrbitRadius, restored.Weapons.First(weapon => weapon.WeaponId == "bible").FrozenOrbitRadius);
                Check.Near(shot.Range, restored.Cross!.Shots.First().Range);
                Check.Near(cast.HalfAngle, restored.WildernessVoice!.Active.First().HalfAngle);
            });
        });

        test("Lion pack roar schedules and the expanded visual radius survive a schema 4 checkpoint", () =>
        {
            WithStore(store =>
            {
                var state = ExpandedProfile(store, "weapon.daniel_lion.c.05", "weapon.daniel_lion.base.03", "common.area.01");
                var definition = WaveTests.Adam();
                var session = new AdamSession(state, definition, "bible");
                Check.True(session.Inventory.TryChoose("daniel_lion", false));
                var world = new BattleWorld(session, Speeds, 7);
                world.Advance(BattleWorld.FixedStep, Vector2.Zero);
                var frozen = world.Capture();
                var lions = frozen.DanielLion!.Lions;
                Check.Equal(2, lions.Count);
                Check.Near(.18, lions[1].NextPackRoarAt!.Value - lions[0].NextPackRoarAt!.Value);
                Check.Near(1.4 * 1.19, session.LionRoarRadius(false));
                state.Run!.World = frozen;
                store.Commit(state, state.Revision, "lion-pack-roar");
                state = store.Load()!;
                session = AdamSession.Resume(state, definition);
                world = BattleWorld.Restore(session, Speeds, state.Run!.World!);
                var restoredLions = world.Capture().DanielLion!.Lions;
                Check.Near(lions[1].NextPackRoarAt!.Value, restoredLions[1].NextPackRoarAt!.Value);
                Check.Near(1.8 * 1.19, session.LionRoarRadius(true));
            });
        });

        test("Voice rank cadence survives a boss-intro checkpoint and old casts are canceled", () =>
        {
            WithStore(store =>
            {
                var state = ExpandedProfile(store, "weapon.wilderness_voice.a.05", "weapon.wilderness_voice.b.03",
                    "weapon.wilderness_voice.e.03", "common.area.01");
                var session = new AdamSession(state, WaveTests.Adam(), "bible");
                for (var round = 2; round <= 3; round++)
                {
                    session.Advance(180);
                    while (session.PendingChoices.Count != 0) Check.True(session.Choose(session.PendingChoices[0]));
                    session = session.StartNextRound(CatalogReader.Round("content/genesis/catalog.json", round));
                }
                var definition = CatalogReader.Round("content/genesis/catalog.json", 3);
                for (var level = 0; level < 8; level++) Check.True(session.Inventory.TryChoose("wilderness_voice", false));
                var weapon = ExpandedWeaponRules.Get("wilderness_voice");
                Check.True(session.Inventory.TryChoose(weapon.PassiveId, true));
                Check.True(session.Inventory.TryEvolve(weapon.EvolutionId));
                var speeds = CatalogReader.MonsterSpeeds("content/genesis/catalog.json");
                session.Advance(175);
                var prepared = new BattleWorld(session, speeds, 7).Capture();
                foreach (var enemy in prepared.Enemies) enemy.X = 100;
                var world = BattleWorld.Restore(session, speeds, prepared);
                world.Advance(4.6, Vector2.UnitY);
                var beforeBoundary = world.Capture().WildernessVoice!;
                Check.True(beforeBoundary.Cycle > 0);
                var cast = beforeBoundary.Active.Last();
                Check.Near(1, cast.LockedFacingY!.Value);
                Check.Near(.16, cast.EchoRadiusBonus);
                Check.True(cast.CenteredDamage && cast.EdgeKnockback && cast.UncappedDamage.HasValue);
                state.Run!.World = world.Capture();
                store.Commit(state, state.Revision, "voice-cast-locked");
                state = store.Load()!;
                session = AdamSession.Resume(state, definition);
                world = BattleWorld.Restore(session, speeds, state.Run!.World!);
                world.Advance(.5, Vector2.UnitX);
                Check.Equal("boss_intro", session.Stage);
                Check.True(world.Capture().WildernessVoice == null);
                Check.Equal(beforeBoundary.Cycle, state.Run.BossEntryVoiceCycle);
                state.Run.World = world.Capture();
                store.Commit(state, state.Revision, "voice-boss-intro");
                state = store.Load()!;
                session = AdamSession.Resume(state, definition);
                world = BattleWorld.Restore(session, speeds, state.Run!.World!);
                world.Advance(2.1, Vector2.UnitX);
                Check.Equal("boss", session.Stage);
                var bossVoice = world.Capture().WildernessVoice!;
                Check.Equal(beforeBoundary.Cycle + 1, bossVoice.Cycle);
                Check.Near(1, bossVoice.Active.Single().LockedFacingX!.Value);
            });
        });

        test("Common critical first-hit bonus applies once per real originating volley", () =>
        {
            WithStore(store =>
            {
                var state = ExpandedProfile(store, "common.crit.03");
                var definition = new RoundDefinition(1, 40, 60, 25, new[]
                {
                    new WaveDefinition(0, 60, new[] { new SpawnGroup("a1", 60, 100, 4) }),
                });
                var session = new AdamSession(state, definition, "jawbone");
                session.Advance(2.1);
                var setup = new BattleWorld(session, Speeds, 7).Capture();
                var candidates = setup.Enemies.Take(2).ToArray();
                Check.Equal(2, candidates.Length);
                foreach (var enemy in setup.Enemies) enemy.X = 100;
                candidates[0].X = 1;
                candidates[0].Y = .2f;
                candidates[1].X = 1;
                candidates[1].Y = -.2f;
                var run = state.Run!;
                const double denominator = 4294967296d;
                uint Seeded(uint value) => PermanentOriginalHitRules.NextCriticalState(run.RunId, 1, "jawbone", value);
                bool OnlyOpeningRoll(uint value) => value / denominator > .03 && value / denominator < .08;
                var seed = (uint)Enumerable.Range(1, 120000).First(value =>
                    OnlyOpeningRoll(Seeded((uint)value)) && OnlyOpeningRoll(Seeded(Seeded((uint)value))));
                run.CriticalRandomStates!["jawbone"] = seed;
                var world = BattleWorld.Restore(session, Speeds, setup);
                world.Advance(BattleWorld.FixedStep, Vector2.Zero);
                var damage = session.AttackProfile("jawbone").Damage;
                Check.Near(100 - damage * 1.6, session.Targets[candidates[0].Id].Health);
                Check.Near(100 - damage, session.Targets[candidates[1].Id].Health);
                run.World = world.Capture();
                store.Commit(state, state.Revision, "critical-original-volley");
                var restored = store.Load()!;
                var ledger = restored.Run!.World!.Weapons.Single().OriginalHitLedger!;
                Check.Equal(1L, ledger.LatestCycle);
                Check.True(!ledger.IsFirst(1) && ledger.IsFirst(2));
                Check.Equal(Seeded(Seeded(seed)), restored.Run.CriticalRandomStates!["jawbone"]);
            });
        });

        test("Common critical same-target mark requires three original hits and restores its expiry", () =>
        {
            WithStore(store =>
            {
                var state = ExpandedProfile(store, "common.crit.09");
                var definition = new RoundDefinition(1, 40, 60, 25, new[]
                {
                    new WaveDefinition(0, 60, new[] { new SpawnGroup("a1", 60, 1000, 4) }),
                });
                var session = new AdamSession(state, definition, "jawbone");
                session.Advance(2.1);
                var setup = new BattleWorld(session, Speeds, 7, new BattleTuning { PlayerSpeed = .05 }).Capture();
                var target = setup.Enemies.First();
                foreach (var enemy in setup.Enemies) enemy.X = 100;
                target.X = 1;
                target.Y = 0;
                var world = BattleWorld.Restore(session, Speeds, setup);
                world.Advance(1.2, Vector2.Zero);
                var secondHit = world.Capture().Weapons.Single().OriginalHitLedger!;
                Check.Equal(2, secondHit.ConsecutiveHits);
                Check.True(!secondHit.IsMarked(target.Id, world.SimulationTime));
                world.Advance(1, Vector2.Zero);
                state.Run!.World = world.Capture();
                store.Commit(state, state.Revision, "critical-three-hit");
                var saved = store.Load()!;
                var receipt = saved.Run!.World!.Weapons.Single().OriginalHitLedger!;
                var recorded = receipt.Marks!.Single();
                Check.Equal(target.Id, recorded.Id);
                Check.True(receipt.IsMarked(target.Id, recorded.At + 3));
                Check.True(!receipt.IsMarked(target.Id, recorded.At + 3.001));
            });
        });

        test("Suppressed original shots are counted once and remain recorded after reloading", () =>
        {
            WithStore(store =>
            {
                var state = ExpandedProfile(store, "weapon.sling.base.03");
                var definition = WaveTests.Adam();
                var session = new AdamSession(state, definition, "sling");
                session.Advance(2);
                var variant = new BattleTuning { MaxProjectiles = 1 };
                var setup = new BattleWorld(session, Speeds, 7, variant).Capture();
                foreach (var enemy in setup.Enemies) enemy.X = 12;
                var world = BattleWorld.Restore(session, Speeds, setup, variant);
                world.Advance(.2, Vector2.Zero);
                Check.Equal(1, world.ShotsFired);
                Check.Equal(1, world.OriginalSuppressions);
                Check.Equal(0, world.SupportSuppressions);
                state.Run!.World = world.Capture();
                store.Commit(state, state.Revision, "count-original-overflow");
                var saved = store.Load()!;
                session = AdamSession.Resume(saved, definition);
                world = BattleWorld.Restore(session, Speeds, saved.Run!.World!, variant);
                Check.Equal(1, world.OriginalSuppressions);
                world.Advance(BattleWorld.FixedStep, Vector2.Zero);
                Check.Equal(1, world.OriginalSuppressions);
            });
        });

        test("Approved lightning reuse changes the fourth strike with three targets and preserves a two-target rotation", () =>
        {
            foreach (var trained in new[] { false, true })
                foreach (var targetCount in new[] { 2, 3 })
                    WithStore(store =>
                    {
                        var state = ExpandedProfile(store, "weapon.lightning.base.03",
                            trained ? "weapon.lightning.a.03" : "weapon.lightning.a.02");
                        var definition = WaveTests.Adam();
                        var session = new AdamSession(state, definition, "bible");
                        for (var rank = 0; rank < 8; rank++) Check.True(session.Inventory.TryChoose("lightning", false));
                        session.Advance(3.5);
                        var setup = new BattleWorld(session, Speeds, 7).Capture();
                        var targets = setup.Enemies.OrderBy(enemy => enemy.Id).Take(targetCount).ToArray();
                        Check.Equal(targetCount, targets.Length);
                        foreach (var enemy in setup.Enemies)
                        {
                            enemy.X = 100;
                            enemy.Y = 0;
                        }
                        targets[0].X = 1.5f;
                        targets[1].X = -2.5f;
                        if (targets.Length == 3) targets[2].X = 5.5f;
                        var world = BattleWorld.Restore(session, Speeds, setup);
                        world.Advance(BattleWorld.FixedStep, Vector2.Zero);
                        var frozen = world.Capture();
                        var slots = frozen.Lightning!.Pending.OrderBy(strike => strike.Id).Select(strike => strike.AssignedTargetId);
                        var indices = targetCount == 2 ? new[] { 0, 1, 0, 1 } :
                            new[] { 0, 1, 2, trained ? 1 : 0 };
                        Check.True(slots.SequenceEqual(indices.Select(index => targets[index].Id)));
                        state.Run!.World = frozen;
                        store.Commit(state, state.Revision, "approved-lightning-reuse");
                        var saved = store.Load()!;
                        var restoredSession = AdamSession.Resume(saved, definition);
                        var restored = BattleWorld.Restore(restoredSession, Speeds, saved.Run!.World!);
                        restored.Advance(.3, Vector2.Zero);
                        world.Advance(.3, Vector2.Zero);
                        Check.Equal(JsonSerializer.Serialize(world.Capture()), JsonSerializer.Serialize(restored.Capture()));
                    });
        });

        test("Common power adds first, elite, low health and third-target damage to real originals", () =>
        {
            WithStore(store =>
            {
                var state = ExpandedProfile(store, "common.power.09");
                var definition = new RoundDefinition(1, 40, 60, 25, new[]
                {
                    new WaveDefinition(0, 60, new[]
                    {
                        new SpawnGroup("a1", 60, 100, 4), new SpawnGroup("a2", 60, 100, 4),
                        new SpawnGroup("a3", 60, 400, 4),
                    }),
                });
                var session = new AdamSession(state, definition, "jawbone");
                session.Advance(2.1);
                var setup = new BattleWorld(session, Speeds, 7).Capture();
                var chosen = new[] { "a1", "a2", "a3" }
                    .Select(species => setup.Enemies.First(actor => session.Targets[actor.Id].MonsterId == species))
                    .OrderBy(actor => actor.Id).ToArray();
                foreach (var actor in setup.Enemies) actor.X = 100;
                for (var index = 0; index < chosen.Length; index++)
                {
                    var actor = chosen[index];
                    actor.X = 1.2f;
                    actor.Y = (index - 1) * .2f;
                }
                var lowTarget = chosen[1].Id;
                session.Hit(lowTarget, session.Targets[lowTarget].Health * .75, null, original: false);
                var healthBefore = chosen.Select(actor => session.Targets[actor.Id].Health).ToArray();
                var world = BattleWorld.Restore(session, Speeds, setup);
                world.Advance(BattleWorld.FixedStep, Vector2.Zero);
                for (var index = 0; index < chosen.Length; index++)
                {
                    var id = chosen[index].Id;
                    var expected = 10 * (1 + .03 + .05 + (index == 0 ? .06 : 0) +
                        (index == 2 ? .06 : 0) + (id == lowTarget ? .06 : 0) +
                        (session.Targets[id].MonsterId == "a3" ? .05 : 0));
                    Check.Near(expected, healthBefore[index] - session.Targets[id].Health);
                }
                state.Run!.World = world.Capture();
                store.Commit(state, state.Revision, "power-original-order");
                var saved = store.Load()!;
                Check.Equal(1L, saved.Run!.World!.Weapons.Single().OriginalHitLedger!.LatestCycle);
                Check.True(saved.Run.CriticalRandomStates == null);
            });
        });

        test("A queued lightning strike restores its damage basis and applies power by accepted target order", () =>
        {
            WithStore(store =>
            {
                var state = ExpandedProfile(store, "common.power.06", "weapon.lightning.base.01");
                var definition = new RoundDefinition(1, 40, 60, 25, new[]
                {
                    new WaveDefinition(0, 60, new[] { new SpawnGroup("a1", 60, 100, 4) }),
                });
                var session = new AdamSession(state, definition, "bible");
                Check.True(session.Inventory.TryChoose("lightning", false));
                session.Advance(3.5);
                var setup = new BattleWorld(session, Speeds, 7).Capture();
                var chosen = setup.Enemies.OrderBy(actor => actor.Id).Take(3).ToArray();
                Check.Equal(3, chosen.Length);
                foreach (var actor in setup.Enemies) actor.X = 100;
                for (var index = 0; index < chosen.Length; index++)
                {
                    chosen[index].X = 4.5f + index * .05f;
                    chosen[index].Y = 0;
                }

                var world = BattleWorld.Restore(session, Speeds, setup);
                world.Advance(BattleWorld.FixedStep, Vector2.Zero);
                var frozen = world.Capture();
                var strike = frozen.Lightning!.Pending.Single();
                Check.Near(20, strike.OriginalBaseDamage);
                state.Run!.World = frozen;
                store.Commit(state, state.Revision, "lightning-original-power");
                var saved = store.Load()!;
                var restoredSession = AdamSession.Resume(saved, definition);
                var restored = BattleWorld.Restore(restoredSession, Speeds, saved.Run!.World!);
                world.Advance(.3, Vector2.Zero);
                restored.Advance(.3, Vector2.Zero);
                Check.Equal(JsonSerializer.Serialize(world.Capture()), JsonSerializer.Serialize(restored.Capture()));
                for (var index = 0; index < chosen.Length; index++)
                {
                    var damage = 20 * (1 + .03 + .10 + .05 + (index == 0 || index == 2 ? .06 : 0));
                    Check.Near(damage, 100 - restoredSession.Targets[chosen[index].Id].Health);
                }
            });
        });

        test("Purchased area conditions reach actual jawbone swings and persist the last drawn reach", () =>
        {
            foreach (var (lowHealth, targets, evolved) in new[]
            {
                (false, 5, false), (true, 5, false), (false, 6, false), (true, 6, true),
            })
                WithStore(store =>
                {
                    var state = ExpandedProfile(store, "common.area.10");
                    var definition = new RoundDefinition(1, 40, 60, 25, new[]
                    {
                        new WaveDefinition(0, 60, new[] { new SpawnGroup("a1", 60, 100, 4) }),
                    });
                    const string path = "content/genesis/catalog.json";
                    var speeds = evolved ? CatalogReader.MonsterSpeeds(path) : Speeds;
                    var session = new AdamSession(state, definition, "jawbone");
                    if (evolved)
                    {
                        session.Advance(180);
                        session = session.StartNextRound(CatalogReader.Round(path, 2));
                        session.Advance(180);
                        definition = CatalogReader.Round(path, 3);
                        session = session.StartNextRound(definition);
                        for (var rank = 1; rank < 8; rank++) Check.True(session.Inventory.TryChoose("jawbone", false));
                        Check.True(session.Inventory.TryChoose("hair", true));
                        Check.True(session.Inventory.TryEvolve(EvolutionRules.Jawbone));
                    }
                    session.Advance(7.5);
                    if (lowHealth) session.Damage((int)session.CurrentHealth - 30);
                    var setup = new BattleWorld(session, speeds, 7).Capture();
                    var nearby = setup.Enemies.OrderBy(enemy => enemy.Id).Take(targets).ToArray();
                    Check.Equal(targets, nearby.Length);
                    foreach (var enemy in setup.Enemies) { enemy.X = 100; enemy.Y = 0; }
                    for (var index = 0; index < nearby.Length; index++)
                    {
                        nearby[index].X = 2.9f;
                        nearby[index].Y = (index - 2) * .10f;
                    }
                    var world = BattleWorld.Restore(session, speeds, setup);
                    world.Advance(BattleWorld.FixedStep, Vector2.Zero);
                    var snapshot = world.Capture();
                    var expected = WeaponRules.Get("jawbone", evolved ? 8 : 1).AreaMultiplier + .04
                        + (lowHealth ? .06 : 0) + (targets == 6 ? .06 : 0)
                        + (evolved ? .07 + EvolutionRules.JawboneArea : 0);
                    Check.Near(snapshot.Tuning.JawboneRange * expected, snapshot.Weapons.Single().LastJawboneReach);
                    state.Run!.World = snapshot;
                    store.Commit(state, state.Revision, "jawbone-area-conditions");
                    var saved = store.Load()!;
                    Check.Near(snapshot.Weapons.Single().LastJawboneReach, saved.Run!.World!.Weapons.Single().LastJawboneReach);
                });
        });

        test("Purchased fire expiry keeps the current shape through resume and freezes the new region radius", () =>
        {
            WithStore(store =>
            {
                var state = ExpandedProfile(store, "weapon.fire_pillar.b.03", "weapon.fire_pillar.d.02", "common.area.07");
                var session = new AdamSession(state, WaveTests.Adam(), "bible");
                Check.True(session.Inventory.TryChoose("fire_pillar", false));
                session.Advance(3.5);
                var setup = new BattleWorld(session, Speeds, 7).Capture();
                var target = setup.Enemies.OrderBy(actor => actor.Id).First();
                foreach (var enemy in setup.Enemies) { enemy.X = 100; enemy.Y = 0; }
                target.X = 4;
                var world = BattleWorld.Restore(session, Speeds, setup);
                world.Advance(BattleWorld.FixedStep, Vector2.Zero);
                var before = world.Capture();
                Check.Near(.9 * 1.09, before.FirePillar!.Regions.Single().Radius);
                session.Damage((int)session.CurrentHealth - 30);
                state.Run!.World = before;
                store.Commit(state, state.Revision, "before-trained-fire-expiry");
                var saved = store.Load()!;
                var restoredSession = AdamSession.Resume(saved, WaveTests.Adam());
                var restored = BattleWorld.Restore(restoredSession, Speeds, saved.Run!.World!);
                world.Advance(2.3, Vector2.Zero);
                restored.Advance(2.3, Vector2.Zero);
                var snapshot = world.Capture();
                var ember = snapshot.FirePillar!.Regions.Single(region => region.SupportKind == ExpandedFireSupportKind.ClosingEmber);
                Check.Near(.7 * 1.10, ember.Radius);
                Check.Equal(JsonSerializer.Serialize(snapshot), JsonSerializer.Serialize(restored.Capture()));
                saved.Run.World = restored.Capture();
                store.Commit(saved, saved.Revision, "trained-fire-expiry");
                Check.Near(ember.Radius, store.Load()!.Run!.World!.FirePillar!.Regions.Single().Radius);
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
        foreach (var expanded in ExpandedWeaponRules.All)
            if (desired.Any(id => id.StartsWith($"weapon.{expanded.WeaponId}.", StringComparison.Ordinal)))
                state.Profile.UnlockedWeapons.Add(expanded.WeaponId);
        foreach (var id in desired)
            if (PermanentSkillCatalog.Current.Find(id).RequiredEvolution is string evolution &&
                !state.Profile.EvolvedWeapons!.Contains(evolution)) state.Profile.EvolvedWeapons.Add(evolution);
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
