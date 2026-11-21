using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Text.Json;

namespace BibleSurvivor.Core.Tests;

internal static class BossActionTests
{
    private static readonly Dictionary<string, double> Speeds = new() { ["trial"] = .73 };
    private const BossActions AllActions = BossActions.Dash | BossActions.Slam | BossActions.Chains | BossActions.Summon;

    public static void Register(Action<string, Action> test)
    {
        test("Boss actions dash and chains share their locked aim and survive geometry restore", () =>
        {
            var (state, first) = Encounter(AllActions);
            var (_, second) = Encounter(AllActions);
            Advance(first, 360);
            Advance(second, 360);
            Check.Equal("telegraph", first.ChariotDash!.Phase);
            var directions = first.Session.Boss!.ChainDirections!;
            Check.Equal(3, directions.Count);
            Check.Equal(first.ChariotDash.Direction.X, directions[1].X);
            Check.Equal(first.ChariotDash.Direction.Y, directions[1].Y);

            first.Session.Encounter!.Validate(state.Run!, first.Session.BossRules!);
            var mismatchedAim = first.Session.Boss!;
            foreach (var direction in mismatchedAim.ChainDirections!)
                (direction.X, direction.Y) = (-direction.Y, direction.X);
            Check.Throws<InvalidDataException>(() => mismatchedAim.Validate(state.Run!, first.Session.BossRules!));
            var snapshot = first.Capture();
            Check.Equal(4, snapshot.SchemaVersion);
            snapshot.Validate();
            var restored = BattleWorld.Restore(second.Session, Speeds, snapshot);
            Advance(first, 60);
            Advance(restored, 60);
            Check.Equal("dash", first.ChariotDash!.Phase);
            Check.Equal(3, first.HostileProjectiles.Count);
            Check.Equal(2, first.DamageTaken); // R60 fixture carries five points of armor.
            first.Session.Encounter!.Validate(state.Run!, first.Session.BossRules!);
            Check.Equal(JsonSerializer.Serialize(first.Capture()), JsonSerializer.Serialize(restored.Capture()));

            first.Session.Hit(first.Session.BossRules!.BossActorId, 3000);
            Check.Equal(6, first.Session.Boss!.Supports.Count);
        });

        test("Boss actions summon-only has thresholds without a timed strike", () =>
        {
            var (state, world) = Encounter(BossActions.Summon);
            Advance(world, 480);
            var boss = world.Session.Boss!;
            Check.True(!boss.Telegraphing && world.ChariotDash == null && world.HostileProjectiles.Count == 0);
            Check.Equal(0, world.DamageTaken);
            world.Session.Hit(world.Session.BossRules!.BossActorId, 3000);
            Check.Equal(6, world.Session.Boss!.Summoned);
            world.Session.Encounter!.Validate(state.Run!, world.Session.BossRules!);
        });

        test("Boss actions dash-only does not inherit the R12 summons or chain payload", () =>
        {
            var (_, world) = Encounter(BossActions.Dash);
            Advance(world, 425);
            Check.Equal("dash", world.ChariotDash!.Phase);
            Check.Equal(5, world.Capture().SchemaVersion);
            Check.True(world.Session.Boss!.ChainDirections == null);
            world.Session.Hit(world.Session.BossRules!.BossActorId, 3000);
            Check.Equal(0, world.Session.Boss!.Summoned);
        });
    }

    private static (GameSnapshot State, BattleWorld World) Encounter(BossActions actions)
    {
        var fixture = Path.Combine("tests", "BibleSurvivor.Core.Tests", "Fixtures", "r60-build-complete-save.json");
        var state = new SaveStore(fixture).Load()!;
        var session = AdamSession.Resume(state, CampaignContent.Round(60));
        var definition = new RoundDefinition(61, 12, 100, 0,
            new[] { new WaveDefinition(0, 170, new[] { new SpawnGroup("trial", 1, 10, 1) }) }, "trial_61");
        var supports = (actions & BossActions.Summon) != 0;
        var boss = BossDefinition.Experimental(61, definition.Id, 5000, 0, 0, 7, 2,
            supports ? "trial" : "", 500, 0, supports ? .73 : 0, actions, .50);
        session = session.StartExperimentalRound(definition, boss);
        session.Advance(180);
        return (state, new BattleWorld(session, Speeds, 7));
    }

    private static void Advance(BattleWorld world, int steps)
    {
        for (var step = 0; step < steps; step++)
            world.Advance(BattleWorld.FixedStep, Vector2.Zero);
    }
}
