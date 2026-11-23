using System;
using System.Linq;
using System.Numerics;
using System.Text.Json;
using BibleSurvivor.Sim;

namespace BibleSurvivor.Core.Tests;

internal static partial class PermanentSkillCombatTests
{
    private readonly record struct StrikeCase(bool Elite = false, double TargetHealth = 1,
        double PlayerHealth = 1, double HurtAgeAtLanding = 4, float TargetX = 4,
        int ObservedOrdinal = 1);

    private static void RegisterOriginalPowerEvidence(Action<string, Action> test)
    {
        Add("02", new(Elite: true), new(TargetHealth: 1), 1);
        Add("03", new(ObservedOrdinal: 1), new(ObservedOrdinal: 2), 1.2);
        Add("04", new(TargetHealth: .25), new(TargetHealth: .251), 1.2);
        Add("05", new(PlayerHealth: .90), new(PlayerHealth: .899), 1);
        Add("06", new(ObservedOrdinal: 3), new(ObservedOrdinal: 2), 1.2);
        Add("07", new(HurtAgeAtLanding: 3), new(HurtAgeAtLanding: 3.001), 1.4);
        Add("08", new(TargetX: 4.01f), new(TargetX: 3.99f), 1);
        test("Power combat and replay common.power.09@1", CheckPowerMark);
        test("Power combat and replay common.power.10@1", CheckBossPower);

        void Add(string ordinal, StrikeCase active, StrikeCase inactive, double gain)
        {
            var id = $"common.power.{ordinal}";
            test($"Power combat and replay {id}@1", () => ComparePowerStrikes(id, active, inactive, gain));
        }
    }

    private static void CheckPowerMark()
    {
        WithStore(store =>
        {
            var prior = ExpandedProfile(store, "common.power.08");
            var bought = Buy(store, prior, "common.power.09", 1450);
            var expected = Damage(prior, expired: false, replay: false);
            var marked = Damage(bought, expired: false, replay: true);
            Check.Near(expected.Third, marked.Third);
            Check.Near(.7, marked.Fourth - expected.Fourth);
            var old = Damage(prior, expired: true, replay: false);
            var expired = Damage(bought, expired: true, replay: false);
            Check.Near(old.Fourth, expired.Fourth);

            (double Third, double Fourth) Damage(GameSnapshot profile, bool expired, bool replay)
            {
                var definition = new RoundDefinition(1, 40, 60, 25, new[]
                {
                    new WaveDefinition(0, 60, new[] { new SpawnGroup("a1", 60, 1000, 4) }),
                });
                var state = SaveStore.PrepareNewChallenge(profile);
                var tuning = new BattleTuning { PlayerSpeed = .05 };
                var session = new AdamSession(state, definition, "jawbone");
                session.Advance(2.1);
                var setup = new BattleWorld(session, Speeds, 7, tuning).Capture();
                var targetId = setup.Enemies.First().Id;
                foreach (var enemy in setup.Enemies) enemy.Position = new Vector2(50, 50);
                Place(setup, 1);
                var world = BattleWorld.Restore(session, Speeds, setup, tuning);
                ForNextHit(world);
                ForNextHit(world);
                var secondHp = session.Targets[targetId].Health;
                ForNextHit(world);
                var thirdHp = session.Targets[targetId].Health;
                if (expired)
                {
                    var away = world.Capture();
                    Place(away, 50);
                    world = BattleWorld.Restore(session, Speeds, away, tuning);
                    world.Advance(3.01, Vector2.Zero);
                    Check.Equal(3, world.Hits);
                    var back = world.Capture();
                    Place(back, 1);
                    world = BattleWorld.Restore(session, Speeds, back, tuning);
                }
                BattleWorld? restored = null;
                if (replay)
                {
                    state.Run!.World = world.Capture();
                    store.Commit(state, state.Revision, "power-mark-before-fourth-hit");
                    var loaded = store.LoadConfirmed()!;
                    restored = BattleWorld.Restore(AdamSession.Resume(loaded, definition), Speeds, loaded.Run!.World!, tuning);
                }
                ForNextHit(world);
                var fourthHp = session.Targets[targetId].Health;
                if (restored != null)
                {
                    ForNextHit(restored);
                    Check.Near(fourthHp, restored.Session.Targets[targetId].Health);
                    Check.Equal(JsonSerializer.Serialize(world.Capture()), JsonSerializer.Serialize(restored.Capture()));
                }
                return (secondHp - thirdHp, thirdHp - fourthHp);

                void Place(BattleSnapshot snapshot, float x) =>
                    snapshot.Enemies.Single(enemy => enemy.Id == targetId).Position = new Vector2(x, 0);
            }
        });
    }

    private static void ForNextHit(BattleWorld world)
    {
        var hits = world.Hits;
        for (var tick = 0; tick < 240 && world.Hits == hits; tick++)
            world.Advance(BattleWorld.FixedStep, Vector2.Zero);
        Check.Equal(hits + 1, world.Hits);
    }

    private static void ComparePowerStrikes(string id, StrikeCase active, StrikeCase inactive, double gain)
    {
        WithStore(store =>
        {
            var node = PermanentSkillCatalog.Current.Find(id);
            var prior = ExpandedProfile(store, node.Prerequisites.Keys.Append("weapon.lightning.base.01").ToArray());
            var bought = Buy(store, prior, id, node.Prices[0]);
            Check.Near(gain, Damage(bought, active, true) - Damage(prior, active, false));
            Check.Near(0, Damage(bought, inactive, false) - Damage(prior, inactive, false));

            double Damage(GameSnapshot profile, StrikeCase sample, bool replay)
            {
                var definition = new RoundDefinition(1, 40, 60, 25, new[]
                {
                    new WaveDefinition(0, 60, new[] {
                        new SpawnGroup("a1", 60, 1000, 4), new SpawnGroup("a2", 60, 4000, 4) }),
                });
                var state = SaveStore.PrepareNewChallenge(profile);
                var tuning = new BattleTuning { PlayerSpeed = .01 };
                var session = new AdamSession(state, definition, "bible");
                Check.True(session.Inventory.TryChoose("lightning", false));
                session.Advance(5);
                var setup = new BattleWorld(session, Speeds, 7, tuning).Capture();
                var group = sample.Elite ? "a2" : "a1";
                var targets = setup.Enemies.Where(enemy => session.Targets[enemy.Id].MonsterId == group)
                    .OrderBy(enemy => enemy.Id).Take(3).ToArray();
                Check.Equal(3, targets.Length);
                foreach (var enemy in setup.Enemies) enemy.Position = new Vector2(50, 50);
                for (var index = 0; index < targets.Length; index++)
                    targets[index].Position = new Vector2(sample.TargetX, index * .05f);
                var observed = targets[sample.ObservedOrdinal - 1].Id;
                if (sample.TargetHealth < 1)
                    session.Hit(observed, session.Targets[observed].Health * (1 - sample.TargetHealth), null, original: false);
                state.Run!.Health = state.Run.MaxHealth * sample.PlayerHealth;
                state.Run.LastHitTime = Math.Round(session.CombatTime + BattleWorld.FixedStep + .15 - sample.HurtAgeAtLanding, 9);
                var world = BattleWorld.Restore(session, Speeds, setup, tuning);
                world.Advance(BattleWorld.FixedStep, Vector2.Zero);
                return ObserveStrike(store, state, definition, Speeds, tuning, world, observed, replay);
            }
        });
    }

    private static void CheckBossPower()
    {
        const string catalog = "content/genesis/catalog.json";
        ComparePowerStrikes("common.power.10", new(TargetHealth: .25), new(TargetHealth: .251), 0);
        WithStore(store =>
        {
            var prior = ExpandedProfile(store, "common.power.09", "weapon.lightning.base.01");
            var bought = Buy(store, prior, "common.power.10", 1750);
            var definition = CatalogReader.Round(catalog, 3);
            var speeds = CatalogReader.MonsterSpeeds(catalog);
            var tuning = new BattleTuning { PlayerSpeed = .01 };
            Check.Near(2, Damage(bought, .25, true) - Damage(prior, .25, false));
            Check.Near(0, Damage(bought, .251, false) - Damage(prior, .251, false));

            double Damage(GameSnapshot profile, double healthRatio, bool replay)
            {
                var state = SaveStore.PrepareNewChallenge(profile);
                var session = StartJawboneThirdRound(state, definition);
                Check.True(session.Inventory.TryChoose("lightning", false));
                session.Advance(180);
                var world = new BattleWorld(session, speeds, 7, tuning);
                world.Advance(2, Vector2.Zero);
                Check.Equal("boss", session.Stage);
                var bossId = session.BossRules!.BossActorId;
                var hp = session.Targets[bossId].Health;
                session.Hit(bossId, hp * (1 - healthRatio), null, original: false);
                var setup = new BattleWorld(session, speeds, 7, tuning).Capture();
                foreach (var enemy in setup.Enemies) enemy.Position = new Vector2(50, 50);
                setup.Enemies.Single(enemy => enemy.Id == bossId).Position = new Vector2(4, 0);
                world = BattleWorld.Restore(session, speeds, setup, tuning);
                world.Advance(BattleWorld.FixedStep, Vector2.Zero);
                return ObserveStrike(store, state, definition, speeds, tuning, world, bossId, replay);
            }
        });
    }

    private static double ObserveStrike(SaveStore store, GameSnapshot state, RoundDefinition definition,
        System.Collections.Generic.IReadOnlyDictionary<string, double> speeds, BattleTuning tuning,
        BattleWorld world, int observed, bool replay)
    {
        var snapshot = world.Capture();
        var landingAt = snapshot.Lightning!.Pending.Single().At;
        var hp = world.Session.Targets[observed].Health;
        BattleWorld? restored = null;
        if (replay)
        {
            state.Run!.World = snapshot;
            store.Commit(state, state.Revision, "power-before-strike");
            var loaded = store.LoadConfirmed()!;
            restored = BattleWorld.Restore(AdamSession.Resume(loaded, definition), speeds, loaded.Run!.World!, tuning);
        }
        for (var tick = 0; tick < 120 && world.SimulationTime < landingAt - 1e-8; tick++)
        {
            world.Advance(BattleWorld.FixedStep, Vector2.Zero);
            restored?.Advance(BattleWorld.FixedStep, Vector2.Zero);
        }
        Check.True(world.SimulationTime >= landingAt - 1e-8);
        var after = world.Session.Targets[observed].Health;
        Check.True(after < hp);
        if (restored != null)
        {
            Check.Near(after, restored.Session.Targets[observed].Health);
            Check.Equal(JsonSerializer.Serialize(world.Capture()), JsonSerializer.Serialize(restored.Capture()));
        }
        return hp - after;
    }
}
