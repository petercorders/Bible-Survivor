using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Runtime.Serialization.Json;
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

        test("Aimed shot warns for 1.2 seconds and keeps its aim through fire and checkpoint", () =>
        {
            var (state, world) = Encounter(BossActions.AimedShot);
            Advance(world, 347);
            Check.True(world.Session.Boss!.AimedShot == null);
            Advance(world, 1);
            var warning = world.Session.Boss!;
            Check.True(warning.Telegraphing);
            Check.Near(1.2, warning.NextAttackAt - warning.BossElapsed);
            var aim = warning.AimedShot!;
            Check.Near(0, Vector2.Dot(aim.Direction, Vector2.UnitY));
            Check.Equal(5, warning.Version);
            var missingAim = warning.Copy();
            missingAim.AimedShot = null;
            Check.Throws<InvalidDataException>(() => missingAim.Validate(state.Run!, world.Session.BossRules!));

            var (restoredState, matching) = Encounter(BossActions.AimedShot);
            Advance(matching, 348);
            using var serialized = new MemoryStream();
            var serializer = new DataContractJsonSerializer(typeof(BossEncounterState));
            serializer.WriteObject(serialized, warning);
            serialized.Position = 0;
            restoredState.Run!.Boss = (BossEncounterState)serializer.ReadObject(serialized)!;
            restoredState.Run.Boss.Validate(restoredState.Run, matching.Session.BossRules!);
            var restored = BattleWorld.Restore(matching.Session, Speeds, world.Capture());

            Advance(world, 24, Vector2.UnitY);
            Advance(restored, 24, Vector2.UnitY);
            Advance(world, 48);
            Advance(restored, 48);
            var fired = world.Session.Boss!;
            Check.True(!fired.Telegraphing);
            Check.Near(0, fired.SecondsSinceAttack(world.Session.BossRules!));
            Check.Equal(aim.Direction, fired.AimedShot!.Direction);
            Check.Equal(aim.Origin, fired.AimedShot.Origin);
            Check.True(world.PlayerPosition.Y > .18 + .25);
            Advance(world, 120);
            Advance(restored, 120);
            Check.Equal(0, world.DamageTaken);
            Check.True(world.Session.Boss!.AimedShot == null);
            Check.Equal(JsonSerializer.Serialize(world.Capture()), JsonSerializer.Serialize(restored.Capture()));

            var (_, standing) = Encounter(BossActions.AimedShot);
            standing.Advance(9, Vector2.Zero);
            Check.Equal(2, standing.DamageTaken);
        });

        test("Damage zone has a locked circle, time to leave it and a two-second active window", () =>
        {
            var (_, world) = Encounter(BossActions.DamageZone);
            Advance(world, 348);
            var warning = world.Session.Boss!;
            var rules = world.Session.BossRules!;
            Check.True(warning.Telegraphing && !warning.HasActiveDamageZone(rules, world.Session.Stage));
            Check.Near(1.2, warning.NextAttackAt - warning.BossElapsed);
            Advance(world, 36, Vector2.UnitY);
            Advance(world, 36);
            Check.True(world.PlayerPosition.Y > 1.5 + .25);
            var active = world.Session.Boss!;
            Check.True(active.HasActiveDamageZone(rules, world.Session.Stage));
            Check.Equal(warning.TargetY, active.TargetY);
            Advance(world, 60);
            Check.Equal(0, world.DamageTaken);
            Advance(world, 25, -Vector2.UnitY);
            Check.Equal(2, world.DamageTaken);
            Advance(world, 35);
            Check.True(!world.Session.Boss!.HasActiveDamageZone(rules, world.Session.Stage));
            int damage = world.DamageTaken;
            Advance(world, 31);
            Check.Equal(damage, world.DamageTaken);

            var (_, standing) = Encounter(BossActions.DamageZone);
            Advance(standing, 539);
            Check.Equal(8, standing.DamageTaken);
        });

        test("Aimed shot and damage zone compose with all four existing boss actions", () =>
        {
            var (state, world) = Encounter(AllActions | BossActions.AimedShot | BossActions.DamageZone);
            Advance(world, 360);
            var warning = world.Session.Boss!;
            Check.True(warning.Telegraphing && warning.AimedShot != null);
            Check.Equal(world.ChariotDash!.Direction, warning.AimedShot!.Direction);
            Check.Equal(warning.AimedShot.Direction.X, warning.ChainDirections![1].X);
            warning.Validate(state.Run!, world.Session.BossRules!);
            Advance(world, 60);
            Check.Equal("dash", world.ChariotDash!.Phase);
            Check.Equal(3, world.HostileProjectiles.Count);
            Check.True(world.Session.Boss!.HasActiveDamageZone(world.Session.BossRules!, world.Session.Stage));
            world.Session.Hit(world.Session.BossRules!.BossActorId, 3000);
            Check.Equal(6, world.Session.Boss!.Summoned);
            world.Session.Hit(world.Session.BossRules!.BossActorId, 2000);
            Check.True(world.Session.Boss!.AimedShot == null);
            Check.True(!world.Session.Boss.HasActiveDamageZone(world.Session.BossRules, world.Session.Stage));
            world.Session.Boss.Validate(state.Run!, world.Session.BossRules);
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
        if ((actions & (BossActions.AimedShot | BossActions.DamageZone)) != 0)
        {
            state.Run!.Passives.Remove("underwear");
            state.Run.PermanentSnapshot.Remove("move");
        }
        var supports = (actions & BossActions.Summon) != 0;
        var boss = BossDefinition.Experimental(61, definition.Id, 5000, 0, 0, 7, 2,
            supports ? "trial" : "", 500, 0, supports ? .73 : 0, actions, .50);
        session = session.StartExperimentalRound(definition, boss);
        if (boss.UsesAimedShot || boss.UsesDamageZone)
            Check.Near(1, session.MoveSpeedMultiplier);
        session.Advance(180);
        return (state, new BattleWorld(session, Speeds, 7));
    }

    private static void Advance(BattleWorld world, int steps, Vector2 movement = default)
    {
        for (var step = 0; step < steps; step++)
            world.Advance(BattleWorld.FixedStep, movement);
    }
}
