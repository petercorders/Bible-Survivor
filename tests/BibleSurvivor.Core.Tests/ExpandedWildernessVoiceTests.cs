using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Runtime.Serialization.Json;

namespace BibleSurvivor.Core.Tests;

internal static class ExpandedWildernessVoiceTests
{
    public static void Register(Action<string, Action> test)
    {
        test("Voice b.04 uses cast-start density within five and restores its first aim without freezing later facing", () =>
        {
            var rank = ExpandedWildernessVoiceRankOptions.FromRanks(new Dictionary<string, int>
            {
                ["weapon.wilderness_voice.b.04"] = 1,
            });
            var state = new ExpandedWildernessVoiceState();
            var targets = new Dictionary<int, Vector2>
            {
                [1] = Vector2.UnitX, [2] = new Vector2(0, 2), [3] = new Vector2(0, 2.2f),
                [4] = new Vector2(0, 2.4f), [5] = new Vector2(0, -5.01f), [6] = new Vector2(.2f, -5.1f),
                [7] = new Vector2(0, -5.2f), [8] = new Vector2(.2f, -5.3f),
            };
            var hits = new List<ExpandedWildernessVoiceHit>();
            Step(state, 0, targets, hits, ranks: rank);
            state = Restore(state);
            state.Validate(0, false);
            Check.Equal(Vector2.UnitY, state.Active[0].InitialFacing!.Value);
            var invalid = state.Copy();
            invalid.Active[0].InitialFacingY = null;
            Check.Throws<ArgumentException>(() => invalid.Validate(0, false));
            targets.Clear();
            targets[9] = Vector2.UnitX;
            targets[10] = Vector2.UnitY;
            Step(state, .15, targets, hits, facing: Vector2.UnitX);
            Step(state, .65, targets, hits, facing: Vector2.UnitX);
            Check.True(hits.Select(hit => hit.TargetId).SequenceEqual(new[] { 10, 9 }));
            Check.True(hits.Select(hit => hit.Facing).SequenceEqual(new[] { Vector2.UnitY, Vector2.UnitX }));

            targets[11] = new Vector2(0, 1.2f);
            var locked = new ExpandedWildernessVoiceState();
            hits.Clear();
            Step(locked, 0, targets, hits, ranks: new ExpandedWildernessVoiceRankOptions(lockFacing: true, aimAtCrowd: true));
            locked = Restore(locked);
            locked.Validate(0, false);
            Check.True(!locked.Active[0].InitialFacing.HasValue);
            Check.Equal(1f, locked.Active[0].LockedFacingY!.Value);
            Step(locked, .15, targets, hits, facing: Vector2.UnitX);
            Step(locked, .65, targets, hits, facing: Vector2.UnitX);
            Check.True(hits.All(hit => hit.Facing == Vector2.UnitY));
            Check.True(hits.Select(hit => hit.TargetId).SequenceEqual(new[] { 10, 11, 10, 11 }));
        });

        test("Voice b.05 turns only for a rear lead of three inside four and ranks rear boss then elite", () =>
        {
            var ranks = ExpandedWildernessVoiceRankOptions.FromRanks(new Dictionary<string, int>
            {
                ["weapon.wilderness_voice.b.04"] = 1, ["weapon.wilderness_voice.b.05"] = 1,
            });
            var targets = new Dictionary<int, Vector2>
            {
                [10] = Vector2.UnitX, [11] = new Vector2(1, .1f), [12] = new Vector2(1, -.1f),
                [1] = new Vector2(-4.001f, 0), [2] = new Vector2(-1, 3.5f), [3] = new Vector2(-1, -3.5f),
                [4] = new Vector2(-1, -3.4f), [5] = new Vector2(-1, 3.4f), [6] = new Vector2(-3, 0),
            };
            var hits = new List<ExpandedWildernessVoiceHit>();
            var calls = 0;
            bool Boss(int id) { calls++; return id == 1; }
            bool Elite(int id) { calls++; return id == 2; }
            var noTurn = new ExpandedWildernessVoiceState();
            Step(noTurn, 0, targets, hits, ranks: ranks, isBoss: Boss, isElite: Elite);
            Check.Equal(0, calls);
            Check.Equal(Vector2.UnitX, noTurn.Active[0].InitialFacing!.Value);

            targets[1] = new Vector2(-4, 0);
            var reroute = new ExpandedWildernessVoiceState();
            Step(reroute, 0, targets, hits, ranks: ranks, isBoss: Boss, isElite: Elite);
            Check.True(calls > 0);
            reroute = Restore(reroute);
            reroute.Validate(0, false);
            Check.Equal(-Vector2.UnitX, reroute.Active[0].InitialFacing!.Value);
            targets[15] = new Vector2(-2, 0);
            Step(reroute, .15, targets, hits, facing: Vector2.UnitX);
            Check.True(hits.Any(hit => hit.TargetId == 15));
            Check.True(hits.All(hit => hit.Facing == -Vector2.UnitX));

            var rearElite = new ExpandedWildernessVoiceState();
            Step(rearElite, 0, targets, hits, ranks: ranks, isBoss: id => id == 10, isElite: Elite);
            Check.Equal(Vector2.Normalize(targets[2]), rearElite.Active[0].InitialFacing!.Value);
        });

        test("Voice e.04 close evolved primary adds .35 at exact 1.2 without affecting echoes and caps combined knockback", () =>
        {
            var ranks = new ExpandedWildernessVoiceRankOptions(finalPulseKnockback: true, strongerEvolvedCloseKnockback: true);
            var targets = new Dictionary<int, Vector2> { [1] = new Vector2(1.2f, 0), [2] = new Vector2(1.2001f, 0) };
            var hits = new List<ExpandedWildernessVoiceHit>();
            var evolved = new ExpandedWildernessVoiceState();
            Step(evolved, 0, targets, hits, evolved: true, duration: 1, ranks: ranks, knockback: .25, closeKnockback: .25);
            evolved = Restore(evolved);
            evolved.Validate(0, true);
            Check.True(evolved.Active[0].EvolvedCloseKnockback);
            Step(evolved, .15, targets, hits, evolved: true);
            Check.Near(.95, hits.Single(hit => hit.TargetId == 1).Knockback);
            Check.Near(.35, hits.Single(hit => hit.TargetId == 2).Knockback);
            hits.Clear();
            Step(evolved, .5, targets, hits, evolved: true);
            Check.True(hits.All(hit => hit.Kind == ExpandedWildernessVoiceHitKind.FirstEcho && hit.Knockback == 0));
            hits.Clear();
            Step(evolved, .65, targets, hits, evolved: true);
            Check.Near(1, hits.Single(hit => hit.TargetId == 1).Knockback);
            Check.Near(.65, hits.Single(hit => hit.TargetId == 2).Knockback);

            hits.Clear();
            var basic = new ExpandedWildernessVoiceState();
            Step(basic, 0, targets, hits, duration: 1, ranks: ranks, knockback: .25, closeKnockback: .25);
            Check.True(!basic.Active[0].EvolvedCloseKnockback);
            Step(basic, .15, targets, hits);
            Check.Near(.60, hits.Single(hit => hit.TargetId == 1).Knockback);
        });

        test("Wilderness Voice restores its launch base and numbers eligible targets separately on each pulse", () =>
        {
            var state = new ExpandedWildernessVoiceState();
            var targets = new Dictionary<int, Vector2>
            {
                [8] = new Vector2(1, 0), [1] = new Vector2(-1, 0),
                [2] = new Vector2(2, 0), [5] = new Vector2(2.5f, 0),
            };
            var hits = new List<ExpandedWildernessVoiceHit>();
            void Tick(ExpandedWildernessVoiceState current, double now, int level) =>
                ExpandedWildernessVoice.Step(current, now, Vector2.Zero, Vector2.UnitX, level, false, 8, 3,
                    3, 1, 45, () => targets, hits.Add);

            Tick(state, 0, 1);
            Check.Near(6, state.Active.Single().OriginalBaseDamage);
            var corrupt = state.Copy();
            corrupt.Active[0].OriginalBaseDamage = 7;
            Check.Throws<ArgumentException>(() => corrupt.Validate(0, false));
            using var save = new MemoryStream();
            var serializer = new DataContractJsonSerializer(typeof(ExpandedWildernessVoiceState));
            serializer.WriteObject(save, state);
            save.Position = 0;
            var restored = (ExpandedWildernessVoiceState)serializer.ReadObject(save)!;

            Tick(restored, .15, 4);
            Check.True(hits.Select(hit => (hit.TargetId, hit.TargetOrdinal, hit.OriginalBaseDamage))
                .SequenceEqual(new[] { (2, 1, 6d), (5, 2, 6d), (8, 3, 6d) }));
            hits.Clear();
            Tick(restored, .65, 4);
            Check.True(hits.Select(hit => (hit.TargetId, hit.TargetOrdinal, hit.OriginalBaseDamage))
                .SequenceEqual(new[] { (2, 1, 6d), (5, 2, 6d), (8, 3, 6d) }));
            restored.Validate(.65, false);
        });

        test("Trained voice freezes opening aim, uses shortened final pulse and stacks pre-cap bonuses", () =>
        {
            var state = new ExpandedWildernessVoiceState { Cycle = 3 };
            var angle = Math.PI / 5;
            var targets = new Dictionary<int, Vector2>
            {
                [1] = Vector2.UnitX,
                [2] = new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle)) * 2,
                [3] = -Vector2.UnitX,
            };
            var hits = new List<ExpandedWildernessVoiceHit>();
            var ranks = ExpandedWildernessVoiceRankOptions.FromRanks(new Dictionary<string, int>
            {
                ["weapon.wilderness_voice.a.02"] = 1,
                ["weapon.wilderness_voice.a.03"] = 1,
                ["weapon.wilderness_voice.a.04"] = 1,
                ["weapon.wilderness_voice.a.05"] = 1,
                ["weapon.wilderness_voice.b.02"] = 1,
                ["weapon.wilderness_voice.b.03"] = 1,
            });
            void Tick(ExpandedWildernessVoiceState current, double now, Vector2 facing,
                ExpandedWildernessVoiceRankOptions options = default) =>
                ExpandedWildernessVoice.Step(current, now, Vector2.Zero, facing, 8, false, 31, 2.5,
                    4.5, 1.13, 45, () => targets, hits.Add, knockbackBonus: .25, closeKnockbackBonus: .25,
                    shortenedPrimaryInterval: true, rankOptions: options, uncappedDamage: 31);

            Tick(state, 0, Vector2.UnitX, ranks);
            Tick(state, .15, -Vector2.UnitX);
            Check.True(hits.Select(hit => hit.TargetId).SequenceEqual(new[] { 1, 2 }));
            Check.Near(33.4, hits[0].Damage);
            Check.Near(32.2, hits[1].Damage);
            Check.Near(.60, hits[1].Knockback);
            Check.Equal(Vector2.UnitX, hits[0].Facing);

            using var save = new MemoryStream();
            var serializer = new DataContractJsonSerializer(typeof(ExpandedWildernessVoiceState));
            serializer.WriteObject(save, state);
            save.Position = 0;
            var restored = (ExpandedWildernessVoiceState)serializer.ReadObject(save)!;
            restored.Validate(.15, false);
            hits.Clear();
            Tick(restored, .63, -Vector2.UnitX);
            Check.Near(32.2, hits[0].Damage);
            Check.Near(.60, hits[1].Knockback);
            hits.Clear();
            Tick(restored, 1.109, -Vector2.UnitX);
            Check.Equal(0, hits.Count);
            Tick(restored, 1.11, -Vector2.UnitX);
            Check.True(hits.Select(hit => hit.TargetId).SequenceEqual(new[] { 1, 2 }));
            Check.Near(33.75, hits[0].Damage);
            Check.Near(32.8, hits[1].Damage);
            Check.Near(.90, hits[0].Knockback);
            Check.Near(.90, hits[1].Knockback);
            restored.Validate(1.11, false);

            var corrupt = restored.Copy();
            corrupt.Active.Single().LockedFacingY = null;
            Check.Throws<ArgumentException>(() => corrupt.Validate(1.11, false));
            corrupt = restored.Copy();
            corrupt.Active.Single().UncappedDamage = null;
            Check.Throws<ArgumentException>(() => corrupt.Validate(1.11, false));
        });

        test("Evolved voice adds a fifth-cycle echo with frozen common and evolution geometry", () =>
        {
            var state = new ExpandedWildernessVoiceState { Cycle = 4, Evolved = true };
            var targets = new Dictionary<int, Vector2>
            {
                [1] = new Vector2(4.94f, 0),
                [2] = new Vector2(4.96f, 0),
                [3] = new Vector2(6.37f, 0),
                [4] = new Vector2(6.39f, 0),
            };
            var hits = new List<ExpandedWildernessVoiceHit>();
            var ranks = new ExpandedWildernessVoiceRankOptions(widenEvolution: true, widenEchoes: true, repeatFifthEcho: true);
            void Tick(ExpandedWildernessVoiceState current, double now, ExpandedWildernessVoiceRankOptions options = default) =>
                ExpandedWildernessVoice.Step(current, now, Vector2.Zero, Vector2.UnitX, 8, true, 20, 2.5,
                    4.5, 1.3, 180, () => targets, hits.Add, echoRadiusBonus: .04, rankOptions: options);

            Tick(state, 0, ranks);
            var cast = state.Active.Single();
            Check.True(cast.HasExtraEcho);
            Check.Near(4.95, cast.Reach);
            Check.Near(5.8, cast.FirstEchoRadius);
            Check.Near(6.38, cast.SecondEchoRadius);
            Tick(state, .15);
            Check.Equal(1, hits.Single().TargetId);
            Tick(state, 1.4);
            var corrupt = state.Copy();
            corrupt.Active.Single().HasExtraEcho = false;
            Check.Throws<ArgumentException>(() => corrupt.Validate(1.4, true));

            using var save = new MemoryStream();
            var serializer = new DataContractJsonSerializer(typeof(ExpandedWildernessVoiceState));
            serializer.WriteObject(save, state);
            save.Position = 0;
            var restored = (ExpandedWildernessVoiceState)serializer.ReadObject(save)!;
            restored.Validate(1.4, true);
            hits.Clear();
            Tick(restored, 1.499);
            Check.Equal(0, hits.Count);
            Tick(restored, 1.5);
            Check.True(hits.Select(hit => hit.TargetId).SequenceEqual(new[] { 1, 2, 3 }));
            Check.True(hits.All(hit => hit.Kind == ExpandedWildernessVoiceHitKind.ExtraEcho &&
                hit.Damage == 7 && hit.Knockback == 0 && hit.PulseNumber == 3 &&
                hit.OriginalBaseDamage == 0 && hit.TargetOrdinal == 0));
            Check.Equal(0, restored.Active.Count);
            restored.Validate(1.5, true);

            var basic = new ExpandedWildernessVoiceState { Cycle = 4 };
            ExpandedWildernessVoice.Step(basic, 0, Vector2.Zero, Vector2.UnitX, 8, false, 20, 2.5,
                4.5, 1.3, 45, () => targets, hits.Add, echoRadiusBonus: .04, rankOptions: ranks);
            Check.Near(4.5, basic.Active.Single().Reach);
            Check.Equal(0d, basic.Active.Single().EchoRadiusBonus);
            Check.True(!basic.Active.Single().HasExtraEcho);
        });

        test("Trained voice cone and close knockback use the frozen primary cast after restore", () =>
        {
            var state = new ExpandedWildernessVoiceState();
            var targets = new Dictionary<int, Vector2>
            {
                [1] = new Vector2(1.2f, 0),
                [2] = new Vector2(1.201f, 0),
                [3] = new Vector2(.63f, .75f),
                [4] = new Vector2(.57f, .80f),
            };
            var hits = new List<ExpandedWildernessVoiceHit>();
            var cycle = ExpandedWeaponRules.Get("wilderness_voice").AtLevel(1).Cycle;
            ExpandedWildernessVoice.Step(state, 0, Vector2.Zero, Vector2.UnitX, 1, false, 15, cycle, 3, .672, 53,
                () => targets, hits.Add, knockbackBonus: .25, closeKnockbackBonus: .25);
            var restored = state.Copy();
            restored.Validate(.1, false);
            ExpandedWildernessVoice.Step(restored, .15, Vector2.Zero, Vector2.UnitX, 1, false, 15, cycle, 3, .6, 45,
                () => targets, hits.Add);
            Check.True(hits.Select(hit => hit.TargetId).SequenceEqual(new[] { 1, 2, 3 }));
            Check.Near(.60, hits[0].Knockback);
            Check.Near(.35, hits[1].Knockback);
            Check.Near(.672, restored.Active.Single().Duration);
        });
        test("Wilderness Voice starts without targets and samples current cone center and facing until the exclusive end", () =>
        {
            var state = new ExpandedWildernessVoiceState();
            var enemies = new Dictionary<int, Vector2>();
            var hits = new List<ExpandedWildernessVoiceHit>();
            Step(state, 0, enemies, hits, duration: 1.15);
            Check.Equal(1L, state.Cycle);
            Check.Equal(1, state.Active.Count);
            enemies[8] = new Vector2(3, 1);
            enemies[3] = new Vector2(2, 3);
            enemies[5] = new Vector2(2, -2);
            enemies[9] = new Vector2(2, 3.01f);
            var center = new Vector2(2, 0);
            Step(state, .149, enemies, hits, center: center, facing: Vector2.UnitY);
            Check.Equal(0, hits.Count);
            Step(state, .15, enemies, hits, center: center, facing: Vector2.UnitY);
            Check.True(hits.Select(hit => hit.TargetId).SequenceEqual(new[] { 3, 8 }));
            Check.True(hits.All(hit => hit.Center == center && hit.Facing == Vector2.UnitY &&
                hit.Kind == ExpandedWildernessVoiceHitKind.Primary && hit.Damage == 15 &&
                hit.Knockback == .1 && hit.PulseNumber == 1));
            state.Validate(.15, false);

            hits.Clear();
            Step(state, .65, enemies, hits, center: center, facing: -Vector2.UnitY);
            Check.Equal(5, hits.Single().TargetId);
            Check.Equal(2, hits.Single().PulseNumber);
            hits.Clear();
            Step(state, 1.15, enemies, hits, center: center, facing: -Vector2.UnitY);
            Check.Equal(0, hits.Count);
            Check.Equal(0, state.Active.Count);
        });

        test("Wilderness Proclamation casts a 360 degree original and two independent echoes at the fixed offsets", () =>
        {
            var state = new ExpandedWildernessVoiceState();
            var enemies = new Dictionary<int, Vector2>
            {
                [8] = new Vector2(4.5f, 0),
                [3] = new Vector2(-4.5f, 0),
                [7] = new Vector2(5, 0),
                [9] = new Vector2(5.5f, 0),
                [2] = new Vector2(5.51f, 0),
            };
            var hits = new List<ExpandedWildernessVoiceHit>();
            Step(state, 0, enemies, hits, evolved: true, reach: 4.5);
            Step(state, .15, enemies, hits, evolved: true, reach: 4.5);
            Check.True(hits.Select(hit => hit.TargetId).SequenceEqual(new[] { 3, 8 }));
            Check.True(hits.All(hit => hit.Kind == ExpandedWildernessVoiceHitKind.Primary));
            hits.Clear();

            Step(state, .499, enemies, hits, evolved: true, reach: 4.5);
            Check.Equal(0, hits.Count);
            Step(state, .5, enemies, hits, evolved: true, reach: 4.5);
            Check.True(hits.Select(hit => hit.TargetId).SequenceEqual(new[] { 3, 7, 8 }));
            Check.True(hits.All(hit => hit.Kind == ExpandedWildernessVoiceHitKind.FirstEcho &&
                hit.Damage == 6.75 && hit.Knockback == 0));
            state.Validate(.5, true);

            var copy = state.Copy();
            copy.Active[0].EchoesFired = 0;
            Check.Equal(1, state.Active[0].EchoesFired);
            Check.Throws<ArgumentException>(() => copy.Validate(.51, true));
            using var save = new MemoryStream();
            var serializer = new DataContractJsonSerializer(typeof(ExpandedWildernessVoiceState));
            serializer.WriteObject(save, state);
            save.Position = 0;
            var restored = (ExpandedWildernessVoiceState)serializer.ReadObject(save)!;
            restored.Validate(.5, true);
            var replayHits = new List<ExpandedWildernessVoiceHit>();
            Step(restored, .65, enemies, replayHits, evolved: true, reach: 4.5);
            Check.True(replayHits.Select(hit => hit.TargetId).SequenceEqual(new[] { 3, 8 }));
            Check.True(replayHits.All(hit => hit.Kind == ExpandedWildernessVoiceHitKind.Primary && hit.PulseNumber == 2));
            replayHits.Clear();
            Step(restored, 1, enemies, replayHits, evolved: true, reach: 4.5);
            Check.True(replayHits.Select(hit => hit.TargetId).SequenceEqual(new[] { 3, 7, 8, 9 }));
            Check.True(replayHits.All(hit => hit.Kind == ExpandedWildernessVoiceHitKind.SecondEcho &&
                hit.Damage == 4.5 && hit.Knockback == 0));
            replayHits.Clear();
            Step(restored, 1, enemies, replayHits, evolved: true, reach: 4.5);
            Check.Equal(0, replayHits.Count);
        });

        test("Wilderness Proclamation freezes the two trained echo radii through restore", () =>
        {
            var state = new ExpandedWildernessVoiceState();
            var enemies = new Dictionary<int, Vector2>
            {
                [1] = new Vector2(5.19f, 0),
                [2] = new Vector2(5.21f, 0),
                [3] = new Vector2(5.71f, 0),
                [4] = new Vector2(5.73f, 0),
            };
            var hits = new List<ExpandedWildernessVoiceHit>();
            Step(state, 0, enemies, hits, evolved: true, echoRadiusBonus: .04);
            Check.Near(5.2, state.Active.Single().FirstEchoRadius);
            Check.Near(5.72, state.Active.Single().SecondEchoRadius);

            using var save = new MemoryStream();
            var serializer = new DataContractJsonSerializer(typeof(ExpandedWildernessVoiceState));
            serializer.WriteObject(save, state);
            save.Position = 0;
            var restored = (ExpandedWildernessVoiceState)serializer.ReadObject(save)!;
            restored.Validate(0, true);
            Step(restored, .5, enemies, hits, evolved: true);
            Check.Equal((ExpandedWildernessVoiceHitKind.FirstEcho, 1), (hits.Single().Kind, hits.Single().TargetId));
            hits.Clear();
            Step(restored, 1, enemies, hits, evolved: true);
            Check.True(hits.All(hit => hit.Kind == ExpandedWildernessVoiceHitKind.SecondEcho));
            Check.True(hits.Select(hit => hit.TargetId).SequenceEqual(new[] { 1, 2, 3 }));
            restored.Validate(1, true);
        });

        test("Wilderness Voice exact cycle schedule supports overlapping casts and rejects invalid timers", () =>
        {
            const double cycle = .151;
            var state = new ExpandedWildernessVoiceState();
            var enemies = new Dictionary<int, Vector2> { [3] = new Vector2(1, 0) };
            var hits = new List<ExpandedWildernessVoiceHit>();
            for (var tick = 0; tick < 250; tick++)
            {
                var at = tick / 60d;
                Step(state, at, enemies, hits, cycle: cycle, duration: 3.5);
                state.Validate(at, false);
            }
            Check.Equal(28L, state.Cycle);
            Check.Equal(23, state.Active.Count);
            Check.Near(28 * cycle, state.NextCycle);
            Check.True(hits.GroupBy(hit => (hit.Cycle, hit.PulseNumber, hit.TargetId)).All(group => group.Count() == 1));
            var malformed = state.Copy();
            malformed.Active.Add(malformed.Active[0]);
            Check.Throws<ArgumentException>(() => malformed.Validate(249 / 60d, false));
            malformed.Active.RemoveAt(malformed.Active.Count - 1);
            malformed.Active[0].PrimaryPulses++;
            Check.Throws<ArgumentException>(() => malformed.Validate(249 / 60d, false));
        });

        test("Wilderness Voice form change cancels the old pulses and restarts with the next cycle ID", () =>
        {
            var state = new ExpandedWildernessVoiceState();
            var enemies = new Dictionary<int, Vector2> { [3] = -Vector2.UnitX };
            var hits = new List<ExpandedWildernessVoiceHit>();
            Step(state, 0, enemies, hits);
            state.ResetForForm(true, .1);
            Step(state, .1, enemies, hits, evolved: true);
            Check.Equal(2L, state.Cycle);
            Step(state, .15, enemies, hits, evolved: true);
            Check.Equal(0, hits.Count);
            Step(state, .25, enemies, hits, evolved: true, facing: Vector2.Zero);
            Check.Equal(2L, hits.Single().Cycle);
            Check.Equal(Vector2.UnitX, hits.Single().Facing);
            Check.Equal(3, hits.Single().TargetId);
            state.Validate(.25, true);
        });

        test("Trained wilderness voice freezes its advanced first pulse across restore", () =>
        {
            var state = new ExpandedWildernessVoiceState();
            var targets = new Dictionary<int, Vector2> { [1] = Vector2.UnitX };
            var hits = new List<ExpandedWildernessVoiceHit>();
            Step(state, 0, targets, hits, delayAdvance: .08);
            Check.Near(.07, state.Active.Single().FirstDelay ?? .15);
            Step(state, .0699, targets, hits);
            Check.Equal(0, hits.Count);
            using var save = new MemoryStream();
            var serializer = new DataContractJsonSerializer(typeof(ExpandedWildernessVoiceState));
            serializer.WriteObject(save, state);
            save.Position = 0;
            var restored = (ExpandedWildernessVoiceState)serializer.ReadObject(save)!;
            restored.Validate(.0699, false);
            Step(restored, .07, targets, hits);
            Check.Equal(1, hits.Count);
            Step(restored, .0701, targets, hits);
            Check.Equal(1, hits.Count);
            Step(restored, .57, targets, hits);
            Check.Equal(2, hits.Count);
            var capped = new ExpandedWildernessVoiceState();
            Step(capped, 0, targets, hits, delayAdvance: 1);
            Check.Near(.05, capped.Active.Single().FirstDelay ?? .15);
        });
    }

    private static void Step(ExpandedWildernessVoiceState state, double time, Dictionary<int, Vector2> enemies,
        List<ExpandedWildernessVoiceHit> hits, bool evolved = false, Vector2 center = default,
        Vector2? facing = null, double cycle = 2.5, double reach = 3, double duration = 2, double delayAdvance = 0,
        double echoRadiusBonus = 0, ExpandedWildernessVoiceRankOptions ranks = default,
        Func<int, bool>? isElite = null, Func<int, bool>? isBoss = null, double knockback = 0, double closeKnockback = 0)
    {
        ExpandedWildernessVoice.Step(state, time, center, facing ?? Vector2.UnitX, 8, evolved,
            damage: 15, cycle, reach, duration, halfAngle: 45, () => enemies, hits.Add, delayAdvance,
            echoRadiusBonus: echoRadiusBonus, rankOptions: ranks, knockbackBonus: knockback,
            closeKnockbackBonus: closeKnockback, isElite: isElite, isBoss: isBoss);
    }

    private static ExpandedWildernessVoiceState Restore(ExpandedWildernessVoiceState state)
    {
        using var save = new MemoryStream();
        var serializer = new DataContractJsonSerializer(typeof(ExpandedWildernessVoiceState));
        serializer.WriteObject(save, state);
        save.Position = 0;
        return (ExpandedWildernessVoiceState)serializer.ReadObject(save)!;
    }
}
