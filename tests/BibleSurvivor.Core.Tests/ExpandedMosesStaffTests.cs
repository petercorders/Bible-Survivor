using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Runtime.Serialization.Json;

namespace BibleSurvivor.Core.Tests;

internal static class ExpandedMosesStaffTests
{
    public static void Register(Action<string, Action> test)
    {
        test("Expanded staff emits forward symmetric 15 degree waves without targets and preserves the exact cycle", () =>
        {
            var state = new ExpandedMosesStaffState();
            var targets = new Dictionary<int, Vector2>();
            var hits = new List<ExpandedMosesStaffHit>();
            Check.Equal(4, Step(state, 0, targets, hits, amount: 4, cycle: .151, facing: Vector2.UnitY));
            var angles = state.Waves.Select(wave => Math.Atan2(wave.Direction.Y, wave.Direction.X) * 180 / Math.PI).ToArray();
            foreach (var (actual, expected) in angles.Zip(new[] { 67.5, 82.5, 97.5, 112.5 }))
                Check.Near(expected, actual, 1e-4);
            for (var tick = 1; tick <= 9; tick++)
                Check.Equal(0, Step(state, tick / 60d, targets, hits, cycle: .151));
            Check.Equal(1, Step(state, 10 / 60d, targets, hits, cycle: .151, facing: Vector2.Zero));
            Check.Equal(Vector2.UnitX, state.Waves.Last().Direction);
            Check.Near(.302, state.NextCycle);
            state.Validate(10 / 60d, false);
            Check.Equal(0, hits.Count);
        });

        test("Expanded staff sweeps moving targets and pierces each target once without extending travel", () =>
        {
            var state = new ExpandedMosesStaffState();
            var targets = new Dictionary<int, Vector2>
            {
                [5] = new Vector2(.04f, .45f),
                [4] = new Vector2(.9f, 0),
                [3] = new Vector2(.04f, .46f),
                [2] = new Vector2(.04f, 1),
                [1] = new Vector2(.04f, 0),
            };
            var previous = new Dictionary<int, Vector2>(targets) { [2] = new Vector2(.04f, -1) };
            var hits = new List<ExpandedMosesStaffHit>();
            Step(state, 0, targets, hits);
            Step(state, 1 / 60d, targets, hits, previousTargets: previous);
            Check.True(hits.Select(hit => hit.TargetId).SequenceEqual(new[] { 1, 2, 5 }));
            for (var tick = 2; tick <= 90; tick++) Step(state, tick / 60d, targets, hits);
            Check.True(hits.Select(hit => hit.TargetId).SequenceEqual(new[] { 1, 2, 5, 4 }));
            Check.True(hits.All(hit => hit.Kind == ExpandedMosesStaffHitKind.Wave && hit.Damage == 40 && hit.Knockback == .2));
            Check.Equal(0, state.Waves.Count);
            state.Validate(90 / 60d, false);

            var shortWave = new ExpandedMosesStaffState();
            var crossedTooLate = new Dictionary<int, Vector2> { [1] = new Vector2(.04f, 0) };
            var before = new Dictionary<int, Vector2> { [1] = new Vector2(.04f, 2) };
            var lateHits = new List<ExpandedMosesStaffHit>();
            Step(shortWave, 0, crossedTooLate, lateHits, range: .04);
            Step(shortWave, 1 / 60d, crossedTooLate, lateHits, range: .04, previousTargets: before);
            Check.Equal(0, lateHits.Count);
            Check.Equal(0, shortWave.ActiveWaveCount);
        });

        test("Expanded staff evolution grows seven-unit lanes and merges same-tick support damage", () =>
        {
            var state = new ExpandedMosesStaffState();
            var targets = new Dictionary<int, Vector2>
            {
                [1] = new Vector2(.04f, 0),
                [2] = new Vector2(.04f, .63f),
                [3] = new Vector2(-.04f, 0),
            };
            var hits = new List<ExpandedMosesStaffHit>();
            Step(state, 0, targets, hits, evolved: true, amount: 2, width: 1.08, range: 10, speed: 6.5, cycle: .5);
            Check.True(state.Waves.All(wave => Math.Abs(wave.Width - 1.296) < 1e-8));
            for (var tick = 1; tick <= 31; tick++)
            {
                var damage = tick < 30 ? 40 : 80;
                Step(state, tick / 60d, targets, hits, evolved: true, range: 10, speed: 6.5, cycle: .5, damage: damage);
            }
            var supports = hits.Where(hit => hit.Kind == ExpandedMosesStaffHitKind.Lane).ToArray();
            Check.Equal(2, supports.Length);
            Check.True(supports.All(hit => hit.TargetId == 1 && hit.Knockback == 0));
            Check.Equal(1L, supports[0].WaveId);
            Check.Near(14, supports[0].Damage);
            Check.Equal(3L, supports[1].WaveId);
            Check.Near(28, supports[1].Damage);
            state.Validate(31 / 60d, true);

            for (var tick = 32; tick <= 91; tick++)
                Step(state, tick / 60d, targets, hits, evolved: true, range: 10, speed: 6.5);
            Check.Near(7, state.Waves[0].LaneLength);
            Check.True(state.Waves[0].HasLane);
            Check.Equal(4, state.Waves[0].LaneTicks);
            state.Validate(91 / 60d, true);
            for (var tick = 92; tick <= 121; tick++)
                Step(state, tick / 60d, targets, hits, evolved: true, range: 10, speed: 6.5);
            Check.True(state.Waves.All(wave => wave.Id != 1));
            state.Validate(121 / 60d, true);
        });

        test("Expanded staff snapshots preserve a live lane and an applied wave ledger across restore", () =>
        {
            var state = new ExpandedMosesStaffState();
            var targets = new Dictionary<int, Vector2> { [1] = new Vector2(.06f, 0), [2] = new Vector2(2.8f, 0) };
            var hits = new List<ExpandedMosesStaffHit>();
            Step(state, 0, targets, hits, evolved: true);
            for (var tick = 1; tick <= 10; tick++) Step(state, tick / 60d, targets, hits, evolved: true);
            state.Validate(10 / 60d, true);
            var copy = state.Copy();
            copy.Waves[0].HitIds.Add(copy.Waves[0].HitIds[0]);
            Check.Throws<ArgumentException>(() => copy.Validate(10 / 60d, true));
            Check.Equal(1, state.Waves[0].HitIds.Count);
            copy = state.Copy();
            copy.Waves[0].LaneTicks = 4;
            Check.Throws<ArgumentException>(() => copy.Validate(10 / 60d, true));

            var loaded = RoundTrip(state);
            var after = new List<ExpandedMosesStaffHit>();
            var restored = new List<ExpandedMosesStaffHit>();
            for (var tick = 11; tick <= 130; tick++)
            {
                Step(state, tick / 60d, targets, after, evolved: true);
                Step(loaded, tick / 60d, targets, restored, evolved: true);
            }
            Check.True(after.Select(HitSignature).SequenceEqual(restored.Select(HitSignature)));
            Check.True(after.Any(hit => hit.Kind == ExpandedMosesStaffHitKind.Wave && hit.TargetId == 2));
            Check.True(after.All(hit => hit.Kind != ExpandedMosesStaffHitKind.Wave || hit.TargetId != 1));
            loaded.Validate(130 / 60d, true);
            loaded.ResetForForm(false, 130 / 60d);
            Check.Equal(0, loaded.Waves.Count);
            loaded.Validate(130 / 60d, false);
        });

        test("Expanded staff uses original and lane reservations independently", () =>
        {
            var state = new ExpandedMosesStaffState();
            var targets = new Dictionary<int, Vector2>();
            var hits = new List<ExpandedMosesStaffHit>();
            var originalSlots = 2;
            var laneSlots = 1;
            Check.Equal(2, Step(state, 0, targets, hits, evolved: true, amount: 4,
                original: () => originalSlots-- > 0, lane: () => laneSlots-- > 0));
            Check.Equal(2, state.ActiveWaveCount);
            Check.Equal(1, state.ActiveLaneCount);
            Check.True(state.Waves[0].MakesLane && !state.Waves[1].MakesLane);
            Check.Equal(1L, state.Cycle);
            Check.Equal(3L, state.NextWaveId);
            state.Validate(0, true);
        });
    }

    private static int Step(ExpandedMosesStaffState state, double now, IReadOnlyDictionary<int, Vector2> targets,
        List<ExpandedMosesStaffHit> hits, bool evolved = false, double damage = 40, double cycle = 3,
        int amount = 1, double width = .9, double range = 7, double speed = 5, Vector2? facing = null,
        IReadOnlyDictionary<int, Vector2>? previousTargets = null, Func<bool>? original = null, Func<bool>? lane = null) =>
        ExpandedMosesStaff.Step(state, now, Vector2.Zero, facing ?? Vector2.UnitX, 8, evolved, damage, cycle,
            amount, width, range, speed, () => targets, previousTargets ?? targets, hit =>
            {
                hits.Add(hit);
                return true;
            }, original, lane);

    private static (ExpandedMosesStaffHitKind, int, long, double, Vector2) HitSignature(ExpandedMosesStaffHit hit) =>
        (hit.Kind, hit.TargetId, hit.WaveId, hit.Damage, hit.Origin);

    private static ExpandedMosesStaffState RoundTrip(ExpandedMosesStaffState state)
    {
        using var stream = new MemoryStream();
        var serializer = new DataContractJsonSerializer(typeof(ExpandedMosesStaffState));
        serializer.WriteObject(stream, state);
        stream.Position = 0;
        return (ExpandedMosesStaffState)serializer.ReadObject(stream)!;
    }
}
