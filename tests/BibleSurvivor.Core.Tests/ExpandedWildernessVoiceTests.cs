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
    }

    private static void Step(ExpandedWildernessVoiceState state, double time, Dictionary<int, Vector2> enemies,
        List<ExpandedWildernessVoiceHit> hits, bool evolved = false, Vector2 center = default,
        Vector2? facing = null, double cycle = 2.5, double reach = 3, double duration = 2)
    {
        ExpandedWildernessVoice.Step(state, time, center, facing ?? Vector2.UnitX, 8, evolved,
            damage: 15, cycle, reach, duration, halfAngle: 45, () => enemies, hits.Add);
    }
}
