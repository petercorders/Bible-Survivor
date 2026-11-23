using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text.Json;
using BibleSurvivor.Sim;

namespace BibleSurvivor.Core.Tests;

internal static partial class PermanentSkillCombatTests
{
    private const string ActionCatalog = "content/genesis/catalog.json";
    private enum ActionRound { FirstWave, BossRoundWave, ActiveBoss }

    private readonly record struct MoveCase(bool Input = true, double Health = 1,
        double HitAge = 4, int Crowd = 0, double HeadingAge = 0,
        double ContactAge = -1, ActionRound Round = ActionRound.FirstWave);

    private readonly record struct PickupCase(bool Moving = false, double StillAge = 0,
        double Health = 1, int Gems = 1, double GemAge = 4,
        int Crowd = 0, ActionRound Round = ActionRound.FirstWave);

    private static void RegisterActionEvidence(Action<string, Action> test)
    {
        for (var rank = 1; rank <= 3; rank++)
            Move("move", rank, new MoveCase(Health: 1), new MoveCase(Input: false), .05);
        Move("common.move.02", 1, new MoveCase(Health: 1), new MoveCase(Input: false), .02);
        Move("common.move.03", 1, new MoveCase(HitAge: 8), new MoveCase(HitAge: 7.999), .03);
        Move("common.move.04", 1, new MoveCase(HitAge: 2), new MoveCase(HitAge: 2.001), .04);
        Move("common.move.05", 1, new MoveCase(Crowd: 6), new MoveCase(Crowd: 5), .04);
        Move("common.move.06", 1, new MoveCase(Health: .30), new MoveCase(Health: .301), .04);
        Move("common.move.07", 1, new MoveCase(Health: 1), new MoveCase(Input: false), .02);
        Move("common.move.08", 1, new MoveCase(HeadingAge: 1), new MoveCase(HeadingAge: .999), .03);
        Move("common.move.09", 1, new MoveCase(ContactAge: 2), new MoveCase(ContactAge: 2.001), .05);
        Move("common.move.10", 1, new MoveCase(Round: ActionRound.ActiveBoss),
            new MoveCase(Round: ActionRound.BossRoundWave), .04);

        for (var rank = 2; rank <= 5; rank++)
            Pickup("pickup", rank, new PickupCase(Gems: 1), null, .10);
        Pickup("common.pickup.02", 1, new PickupCase(Gems: 1), null, .05);
        Pickup("common.pickup.03", 1, new PickupCase(Moving: true), new PickupCase(Gems: 1), .05);
        Pickup("common.pickup.04", 1, new PickupCase(StillAge: 1.01), new PickupCase(StillAge: .98), .07);
        Pickup("common.pickup.05", 1, new PickupCase(Health: .30), new PickupCase(Health: .301), .08);
        Pickup("common.pickup.06", 1, new PickupCase(Gems: 0), new PickupCase(Gems: 1), .08);
        Pickup("common.pickup.07", 1, new PickupCase(GemAge: 1.98), new PickupCase(GemAge: 2.01), .05);
        Pickup("common.pickup.08", 1, new PickupCase(Gems: 1), null, .06);
        Pickup("common.pickup.09", 1, new PickupCase(Crowd: 6), new PickupCase(Crowd: 5), .08);
        Pickup("common.pickup.10", 1, new PickupCase(Round: ActionRound.BossRoundWave),
            new PickupCase(Round: ActionRound.ActiveBoss), .10);

        void Move(string id, int rank, MoveCase active, MoveCase inactive, double gain) =>
            test($"Move combat and replay {id}@{rank}", () => CheckDisplacement(id, rank, active, inactive, gain));

        void Pickup(string id, int rank, PickupCase active, PickupCase? inactive, double gain) =>
            test($"Pickup combat and replay {id}@{rank}", () => CheckPickup(id, rank, active, inactive, gain));
    }

    private static (GameSnapshot Prior, GameSnapshot Bought) BuyActionRank(SaveStore store, string id, int rank)
    {
        var node = PermanentSkillCatalog.Current.Find(id);
        var prior = ExpandedProfile(store, node.Prerequisites.Keys.ToArray());
        if (id == "move") prior = Purchase(prior, "pickup", 1, 120);
        for (var owned = 0; owned < rank - 1; owned++) prior = Purchase(prior, id, owned, node.Prices[owned]);
        return (prior, Purchase(prior, id, rank - 1, node.Prices[rank - 1]));

        GameSnapshot Purchase(GameSnapshot profile, string skill, int owned, int price) => store.TransactPermanent(
            PermanentSkillCommand.Purchase(PermanentSkillCommand.NewOperationId(profile.Revision),
                profile.Revision, skill, owned, price));
    }

    private static void CheckDisplacement(string id, int rank, MoveCase active, MoveCase inactive, double gain)
    {
        WithStore(store =>
        {
            var (prior, bought) = BuyActionRank(store, id, rank);
            var unit = new BattleTuning().PlayerSpeed * BattleWorld.FixedStep;
            Check.Near(unit * gain, Measure(bought, active, true) - Measure(prior, active, false), 2e-7);
            Check.Near(0, Measure(bought, inactive, true) - Measure(prior, inactive, false), 2e-7);

            double Measure(GameSnapshot profile, MoveCase sample, bool replay)
            {
                var (state, session, definition, speeds) = PrepareActionWorld(profile, sample.Round);
                var now = session.CombatTime;
                state.Run!.Health = state.Run.MaxHealth * sample.Health;
                state.Run.LastHitTime = now - sample.HitAge;
                var actions = state.Run.PermanentActions!;
                if (sample.ContactAge >= 0) actions.LastContactAt = now - sample.ContactAge;
                if (sample.HeadingAge > 0) actions.ObserveMotion(now - sample.HeadingAge, Vector2.UnitX);
                var tuning = new BattleTuning();
                var setup = new BattleWorld(session, speeds, 7, tuning).Capture();
                SetActionActors(setup, sample.Crowd);
                var world = BattleWorld.Restore(session, speeds, setup, tuning);
                var restored = replay ? ReopenActionCombat(store, state, definition, world, speeds, tuning) : null;
                var input = sample.Input ? Vector2.UnitX : Vector2.Zero;
                world.Advance(BattleWorld.FixedStep, input);
                if (restored != null)
                {
                    restored.Advance(BattleWorld.FixedStep, input);
                    Check.Equal(world.PlayerPosition, restored.PlayerPosition);
                    Check.Equal(JsonSerializer.Serialize(world.Capture()), JsonSerializer.Serialize(restored.Capture()));
                }
                var distance = world.PlayerPosition.X - setup.PlayerX;
                if (sample.Input) Check.True(distance > 0);
                else Check.Near(0, distance);
                return distance;
            }
        });
    }

    private static void CheckPickup(string id, int rank, PickupCase active, PickupCase? inactive, double gain)
    {
        WithStore(store =>
        {
            var (prior, bought) = BuyActionRank(store, id, rank);
            var baseRadius = new BattleTuning().PickupRadius;
            var oldRadius = Calibrate(prior, active);
            var newRadius = oldRadius + baseRadius * gain;
            var between = oldRadius + baseRadius * gain / 2;
            Check.Equal(0, CollectAt(prior, active, between, false));
            Check.Equal(1, CollectAt(bought, active, between, true));
            Check.Equal(1, CollectAt(bought, active, newRadius - .005, false));
            Check.Equal(0, CollectAt(bought, active, newRadius + .005, true));
            if (inactive is PickupCase off)
            {
                var outside = Calibrate(prior, off) + baseRadius * gain / 2;
                Check.Equal(0, CollectAt(prior, off, outside, false));
                Check.Equal(0, CollectAt(bought, off, outside, true));
            }

            double Calibrate(GameSnapshot profile, PickupCase sample)
            {
                var (_, session, _, _) = PreparePickup(profile, sample);
                return baseRadius * session.PickupRangeAt(sample.Crowd >= 6);
            }

            int CollectAt(GameSnapshot profile, PickupCase sample, double distance, bool replay)
            {
                var (state, session, definition, speeds) = PreparePickup(profile, sample);
                var tuning = new BattleTuning { PlayerSpeed = .01 };
                var enemy = PrepareActionDrop(session);
                var setup = new BattleWorld(session, speeds, 7, tuning).Capture();
                SetActionActors(setup, sample.Crowd);
                Check.True(session.DroppedExperience.ContainsKey(enemy));
                setup.Drops.Add(new BattleEntityState { Id = enemy, X = (float)distance, Y = 0 });
                var world = BattleWorld.Restore(session, speeds, setup, tuning);
                var restored = replay ? ReopenActionCombat(store, state, definition, world, speeds, tuning) : null;
                var input = sample.Moving ? Vector2.UnitY : Vector2.Zero;
                world.Advance(BattleWorld.FixedStep, input);
                if (restored != null)
                {
                    restored.Advance(BattleWorld.FixedStep, input);
                    Check.Equal(world.Collected, restored.Collected);
                    Check.Equal(session.DroppedExperience.Count, restored.Session.DroppedExperience.Count);
                    Check.Equal(JsonSerializer.Serialize(world.Capture()), JsonSerializer.Serialize(restored.Capture()));
                }
                Check.Equal(world.Collected == 1, !session.DroppedExperience.ContainsKey(enemy));
                return world.Collected;
            }
        });
    }

    private static (GameSnapshot State, AdamSession Session, RoundDefinition Definition,
        IReadOnlyDictionary<string, double> Speeds) PreparePickup(GameSnapshot profile, PickupCase sample)
    {
        var fixture = PrepareActionWorld(profile, sample.Round);
        var (state, session, _, _) = fixture;
        var now = session.CombatTime;
        state.Run!.Health = state.Run.MaxHealth * sample.Health;
        var actions = state.Run.PermanentActions!;
        actions.GemsCollected = sample.Gems;
        actions.LastGemAt = sample.Gems > 0 ? now - sample.GemAge : -1;
        if (sample.Moving) actions.ObserveMotion(now, Vector2.UnitY);
        else actions.StationarySince = now - sample.StillAge;
        return fixture;
    }

    private static (GameSnapshot State, AdamSession Session, RoundDefinition Definition,
        IReadOnlyDictionary<string, double> Speeds) PrepareActionWorld(GameSnapshot profile, ActionRound round)
    {
        var state = SaveStore.PrepareNewChallenge(profile);
        if (round != ActionRound.FirstWave)
        {
            var definition = CatalogReader.Round(ActionCatalog, 3);
            var session = StartJawboneThirdRound(state, definition);
            if (round == ActionRound.ActiveBoss)
            {
                session.Advance(180);
                session.Advance(2);
                Check.Equal("boss", session.Stage);
            }
            else
            {
                session.Advance(12);
                Check.Equal("normal", session.Stage);
            }
            return (state, session, definition, CatalogReader.MonsterSpeeds(ActionCatalog));
        }
        var wave = new RoundDefinition(1, 40, 60, 25, new[]
        {
            new WaveDefinition(0, 60, new[] { new SpawnGroup("a1", 60, 1000, 4) }),
        });
        var normal = new AdamSession(state, wave, "bible");
        normal.Advance(12);
        return (state, normal, wave, Speeds);
    }

    private static int PrepareActionDrop(AdamSession session)
    {
        if (session.Stage == "boss")
        {
            var boss = session.BossRules!.BossActorId;
            session.Hit(boss, session.Targets[boss].Health * .75, null, original: false);
            var support = session.Targets.Keys.Where(id => id != boss).Min();
            session.Kill(support);
            Check.Equal(0, session.DroppedExperience[support]);
            return support;
        }
        var enemy = session.Targets.Keys.Min();
        session.Kill(enemy);
        return enemy;
    }

    private static void SetActionActors(BattleSnapshot setup, int nearby)
    {
        Check.True(setup.Enemies.Count >= nearby);
        for (var index = 0; index < setup.Enemies.Count; index++)
            setup.Enemies[index].Position = index < nearby ? new Vector2(2.9f, 0) : new Vector2(50, 50);
    }

    private static BattleWorld ReopenActionCombat(SaveStore purchased, GameSnapshot state,
        RoundDefinition definition, BattleWorld world, IReadOnlyDictionary<string, double> speeds, BattleTuning tuning)
    {
        // Start each boundary case from the same committed purchase revision.
        BattleWorld? restored = null;
        WithStore(store =>
        {
            File.Copy(purchased.Path, store.Path);
            restored = ReopenCombat(store, state, definition, world, speeds, tuning);
        });
        return restored!;
    }
}
