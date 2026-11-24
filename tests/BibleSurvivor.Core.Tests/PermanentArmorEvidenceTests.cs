using System;
using System.Linq;
using System.Numerics;
using System.Text.Json;
using BibleSurvivor.Sim;

namespace BibleSurvivor.Core.Tests;

internal static partial class PermanentSkillCombatTests
{
    private readonly record struct ContactCase(double Health = 1, double LastHitAge = 4,
        int Nearby = 1, bool Elite = false, int Damage = 14);

    private static void RegisterArmorEvidence(Action<string, Action> test)
    {
        for (var rank = 1; rank <= 3; rank++)
        {
            var purchasedRank = rank;
            test($"Armor combat and replay armor@{rank}", () => CompareArmorContacts("armor", purchasedRank,
                new ContactCase(Damage: 14), new ContactCase(Damage: 1), 1));
        }
        test("Armor combat and replay common.armor.02@1", () =>
            CheckBossDamageSource("common.armor.02", "armor", 220, projectileApplies: false));
        test("Armor combat and replay common.armor.03@1", () =>
            CheckBossDamageSource("common.armor.03", "common.armor.02", 320, projectileApplies: true));
        Add("04", new(Health: .30), new(Health: .301), 1);
        Add("05", new(LastHitAge: 8), new(LastHitAge: 7.999), 2);
        Add("06", new(Elite: true), new(Elite: false), 1);
        Add("07", new(Nearby: 6), new(Nearby: 5), 1);
        Add("08", new(Damage: 14), new(Damage: 1), 1);
        Add("09", new(Health: .90), new(Health: .899), 1);
        test("Armor combat and replay common.armor.10@1", CheckBossArmor);

        void Add(string ordinal, ContactCase active, ContactCase inactive, double gain)
        {
            var id = $"common.armor.{ordinal}";
            test($"Armor combat and replay {id}@1", () => CompareArmorContacts(id, 1, active, inactive, gain));
        }
    }

    private static void CheckBossDamageSource(string id, string prerequisite, int price, bool projectileApplies)
    {
        const string catalog = "content/genesis/catalog.json";
        WithStore(store =>
        {
            var prior = ExpandedProfile(store, prerequisite);
            var bought = Buy(store, prior, id, price);
            var definition = CatalogReader.Round(catalog, 9);
            var speeds = CatalogReader.MonsterSpeeds(catalog);
            var tuning = new BattleTuning { PlayerSpeed = .01 };
            Check.Equal(1, Damage(prior, projectileApplies) - Damage(bought, projectileApplies, replay: true));
            Check.Equal(Damage(prior, !projectileApplies), Damage(bought, !projectileApplies));

            int Damage(GameSnapshot profile, bool projectile, bool replay = false)
            {
                var state = SaveStore.PrepareNewChallenge(profile);
                var session = StartJawboneThirdRound(state, CatalogReader.Round(catalog, 3));
                // Boss reward choices depend on the run id. Keep prior equipment
                // identical across both purchase states and both contact types.
                state.Run!.RunId = "53000000000000000000000000000009";
                for (var round = 3; round < 9; round++)
                {
                    session.Advance(180);
                    if (session.Stage == "boss_intro")
                    {
                        session.Advance(2);
                        session.Hit(session.BossRules!.BossActorId, session.Boss!.Health);
                        if (session.Stage == "boss_reward")
                            Check.True(session.ChooseBossReward(session.Boss.Reward!.RewardId, session.BossChoices[0].Id));
                        RoundTransitionTests.Resolve(session);
                        Check.True(session.CompleteBossSettlement());
                    }
                    session = session.StartNextRound(CatalogReader.Round(catalog, round + 1));
                }
                session.Advance(180);
                var world = new BattleWorld(session, speeds, 7, tuning);
                world.Advance(2, Vector2.Zero);
                var setup = world.Capture();
                var actor = setup.Enemies.Single(enemy => enemy.Id == session.BossRules!.BossActorId);
                actor.Position = new Vector2(projectile ? 30 : .15f, 0);
                world = BattleWorld.Restore(session, speeds, setup, tuning);
                if (projectile)
                {
                    world.Advance(5, Vector2.Zero);
                    Check.Equal(0, world.DamageTaken);
                    setup = world.Capture();
                    Check.Equal(3, setup.HostileProjectiles!.Count);
                    setup.HostileProjectiles.RemoveRange(1, 2);
                    var chain = setup.HostileProjectiles.Single();
                    chain.StartX = chain.X = -.42f;
                    chain.StartY = chain.Y = 0;
                    chain.DirectionX = 1;
                    chain.DirectionY = 0;
                    world = BattleWorld.Restore(session, speeds, setup, tuning);
                }
                var restored = replay ? ReopenCombat(store, state, definition, world, speeds, tuning) : null;
                world.Advance(BattleWorld.FixedStep, Vector2.Zero);
                Check.True(world.DamageTaken > 0);
                if (restored != null)
                {
                    restored.Advance(BattleWorld.FixedStep, Vector2.Zero);
                    Check.Near(session.CurrentHealth, restored.Session.CurrentHealth);
                    Check.Equal(JsonSerializer.Serialize(world.Capture()), JsonSerializer.Serialize(restored.Capture()));
                }
                return world.DamageTaken;
            }
        });
    }

    private static void CompareArmorContacts(string id, int rank, ContactCase active, ContactCase inactive, double gain)
    {
        WithStore(store =>
        {
            var node = PermanentSkillCatalog.Current.Find(id);
            var prior = ExpandedProfile(store, node.Prerequisites.Keys.ToArray());
            if (id == "armor")
                prior = store.TransactPermanent(PermanentSkillCommand.Purchase(
                    PermanentSkillCommand.NewOperationId(prior.Revision), prior.Revision, "health", 1, 160));
            for (var owned = 0; owned < rank - 1; owned++) prior = PurchaseArmor(prior, owned);
            var bought = PurchaseArmor(prior, rank - 1);
            Check.Near(gain, Damage(prior, active, false) - Damage(bought, active, true));
            Check.Near(0, Damage(prior, inactive, false) - Damage(bought, inactive, false));

            GameSnapshot PurchaseArmor(GameSnapshot profile, int owned) => store.TransactPermanent(
                PermanentSkillCommand.Purchase(PermanentSkillCommand.NewOperationId(profile.Revision),
                    profile.Revision, id, owned, node.Prices[owned]));

            double Damage(GameSnapshot profile, ContactCase sample, bool replay)
            {
                var branches = new[]
                {
                    new SpawnGroup("a1", 60, 100, sample.Damage),
                    new SpawnGroup("a2", 60, 400, sample.Damage),
                };
                var definition = new RoundDefinition(1, 40, 60, 25, new[] { new WaveDefinition(0, 60, branches) });
                var state = SaveStore.PrepareNewChallenge(profile);
                var tuning = new BattleTuning { PlayerSpeed = .01 };
                var session = new AdamSession(state, definition, "bible");
                session.Advance(12);
                state.Run!.Health = state.Run.MaxHealth * sample.Health;
                state.Run.LastHitTime = session.CombatTime - sample.LastHitAge;
                var setup = new BattleWorld(session, Speeds, 7, tuning).Capture();
                var contact = setup.Enemies.First(enemy =>
                    session.Targets[enemy.Id].MonsterId == (sample.Elite ? "a2" : "a1"));
                foreach (var enemy in setup.Enemies) enemy.Position = new Vector2(50, 50);
                contact.Position = new Vector2(.15f, 0);
                foreach (var enemy in setup.Enemies.Where(enemy => enemy.Id != contact.Id).Take(sample.Nearby - 1))
                    enemy.Position = new Vector2(2.99f, 0);
                var world = BattleWorld.Restore(session, Speeds, setup, tuning);
                var restored = replay ? ReopenCombat(store, state, definition, world, Speeds, tuning) : null;
                world.Advance(BattleWorld.FixedStep, Vector2.Zero);
                if (restored != null)
                {
                    restored.Advance(BattleWorld.FixedStep, Vector2.Zero);
                    Check.Equal(world.DamageTaken, restored.DamageTaken);
                    Check.Near(session.CurrentHealth, restored.Session.CurrentHealth);
                    Check.Equal(JsonSerializer.Serialize(world.Capture()), JsonSerializer.Serialize(restored.Capture()));
                }
                Check.True(world.DamageTaken > 0);
                if (sample.Damage == 1) Check.Equal(1, world.DamageTaken);
                return world.DamageTaken;
            }
        });
    }

    private static void CheckBossArmor()
    {
        const string catalog = "content/genesis/catalog.json";
        CompareArmorContacts("common.armor.10", 1, new ContactCase(Damage: 14), new ContactCase(Damage: 1), 0);
        WithStore(store =>
        {
            var before = ExpandedProfile(store, "common.armor.09");
            var after = Buy(store, before, "common.armor.10", 1750);
            var definition = CatalogReader.Round(catalog, 3);
            var speeds = CatalogReader.MonsterSpeeds(catalog);
            var tuning = new BattleTuning { PlayerSpeed = .01 };
            Check.Equal(2, Damage(before, false) - Damage(after, true));

            int Damage(GameSnapshot profile, bool replay)
            {
                var session = StartJawboneThirdRound(profile, definition);
                session.Advance(180);
                Check.Equal("boss_intro", session.Stage);
                var world = new BattleWorld(session, speeds, 7, tuning);
                world.Advance(2 - BattleWorld.FixedStep, Vector2.Zero);
                Check.Equal("boss_intro", session.Stage);
                Check.Equal(0, world.DamageTaken);
                world.Advance(BattleWorld.FixedStep, Vector2.Zero);
                Check.Equal("boss", session.Stage);
                var setup = world.Capture();
                setup.Enemies.Single(enemy => enemy.Id == BossDefinition.ActorId).Position = new Vector2(.15f, 0);
                world = BattleWorld.Restore(session, speeds, setup, tuning);
                var restored = replay ? ReopenCombat(store, profile, definition, world, speeds, tuning) : null;
                world.Advance(BattleWorld.FixedStep, Vector2.Zero);
                Check.True(world.DamageTaken > 0);
                if (restored != null)
                {
                    restored.Advance(BattleWorld.FixedStep, Vector2.Zero);
                    Check.Equal(world.DamageTaken, restored.DamageTaken);
                    Check.Near(session.CurrentHealth, restored.Session.CurrentHealth);
                }
                return world.DamageTaken;
            }
        });
    }

    private static BattleWorld ReopenCombat(SaveStore store, GameSnapshot state, RoundDefinition definition,
        BattleWorld world, System.Collections.Generic.IReadOnlyDictionary<string, double> speeds, BattleTuning tuning)
    {
        state.Run!.World = world.Capture();
        store.Commit(state, state.Revision, "permanent-combat-before-impact");
        var loaded = store.LoadConfirmed()!;
        return BattleWorld.Restore(AdamSession.Resume(loaded, definition), speeds, loaded.Run!.World!, tuning);
    }
}
