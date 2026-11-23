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

        test("Staff a.01 adds ten percent points of level-one width before the evolution multiplier", () =>
        {
            var targets = new Dictionary<int, Vector2> { [1] = new Vector2(.04f, .70f) };
            var hits = new List<ExpandedMosesStaffHit>();
            var state = new ExpandedMosesStaffState();
            Step(state, 0, targets, hits, evolved: true, width: 1.08, ranks: Rank("a.01"));
            Check.Near((1.08 + .09) * 1.2, state.Waves[0].Width);
            Step(state, 1 / 60d, targets, hits, evolved: true);
            Check.True(hits.Any(hit => hit.TargetId == 1 && hit.Kind == ExpandedMosesStaffHitKind.Wave));
        });

        test("Staff a.02 changes the fan to twenty degrees around facing", () =>
        {
            var state = new ExpandedMosesStaffState();
            Step(state, 0, new Dictionary<int, Vector2>(), new List<ExpandedMosesStaffHit>(),
                facing: Vector2.UnitY, amount: 3, ranks: Rank("a.02"));
            CheckAngles(state, 70, 90, 110);
        });

        test("Staff a.03 locks on the nearest elite within four and narrows to five degrees", () =>
        {
            var targets = new Dictionary<int, Vector2>
            {
                [10] = new Vector2(4, 0),
                [3] = new Vector2(0, 4),
                [1] = new Vector2(1, 0),
            };
            var ranks = ExpandedMosesStaffRankOptions.FromRanks(new Dictionary<string, int>
            {
                ["weapon.moses_staff.a.02"] = 1,
                ["weapon.moses_staff.a.03"] = 1,
            });
            var state = new ExpandedMosesStaffState();
            var hits = new List<ExpandedMosesStaffHit>();
            Step(state, 0, targets, hits, amount: 3, ranks: ranks, isElite: id => id != 1);
            CheckAngles(state, 85, 90, 95);
            targets[3] = new Vector2(0, 4.001f);
            targets[10] = new Vector2(4.001f, 0);
            Step(state, 3, targets, hits, amount: 3, ranks: ranks, isElite: id => id != 1);
            CheckAngles(state, -20, 0, 20);
        });

        test("Staff b.01 preserves original knockback across restore and applies none to lane hits", () =>
        {
            var state = new ExpandedMosesStaffState();
            var hits = new List<ExpandedMosesStaffHit>();
            var targets = new Dictionary<int, Vector2> { [1] = new Vector2(.04f, 0) };
            Step(state, 0, targets, hits, evolved: true, ranks: Rank("b.01"));
            var loaded = RoundTrip(state);
            Step(loaded, 1 / 60d, targets, hits, evolved: true);
            Check.Near(.45, hits.Single(hit => hit.Kind == ExpandedMosesStaffHitKind.Wave).Knockback);
            Check.Near(0, hits.Single(hit => hit.Kind == ExpandedMosesStaffHitKind.Lane).Knockback);
        });

        test("Staff e.01 extends actual lane width to 1.38 only in the evolved form", () =>
        {
            var state = new ExpandedMosesStaffState();
            var hits = new List<ExpandedMosesStaffHit>();
            var targets = new Dictionary<int, Vector2>
            {
                [1] = new Vector2(.04f, .69f),
                [2] = new Vector2(.04f, .691f),
            };
            Step(state, 0, targets, hits, ranks: Rank("e.01"));
            Check.True(!state.Waves[0].WidenedLane);
            Step(state, 0, targets, hits, evolved: true, ranks: Rank("e.01"));
            Check.Near(1.38, state.Waves[0].LaneWidth);
            Step(state, 1 / 60d, targets, hits, evolved: true);
            Check.Equal(1, hits.Single(hit => hit.Kind == ExpandedMosesStaffHitKind.Lane).TargetId);
        });

        test("Staff e.02 increases swept outer contact damage at the inclusive 75 percent edge", () =>
        {
            var state = new ExpandedMosesStaffState();
            var hits = new List<ExpandedMosesStaffHit>();
            var targets = new Dictionary<int, Vector2>
            {
                [1] = new Vector2(.04f, .45f),
                [2] = new Vector2(.04f, .449f),
                [3] = new Vector2(.04f, -.46f),
                [4] = new Vector2(.04f, 0),
            };
            var previous = new Dictionary<int, Vector2>(targets) { [4] = new Vector2(.04f, .8f) };
            Step(state, 0, targets, hits, evolved: true, ranks: Rank("e.02"));
            var loaded = RoundTrip(state);
            Step(loaded, 1 / 60d, targets, hits, evolved: true, previousTargets: previous);
            var support = hits.Where(hit => hit.Kind == ExpandedMosesStaffHitKind.Lane).ToDictionary(hit => hit.TargetId);
            Check.Near(18.8, support[1].Damage);
            Check.Near(14, support[2].Damage);
            Check.Near(18.8, support[3].Damage);
            Check.Near(18.8, support[4].Damage);
        });

        test("Staff overlap chooses the strongest qualified lane once even when a lower-ID lane also hits", () =>
        {
            var state = new ExpandedMosesStaffState();
            var hits = new List<ExpandedMosesStaffHit>();
            var targets = new Dictionary<int, Vector2> { [1] = new Vector2(2, .2f) };
            var ranks = new ExpandedMosesStaffRankOptions(widenFan: true, empowerLaneEdge: true);
            Step(state, 0, targets, hits, evolved: true, amount: 3, ranks: ranks);
            for (var tick = 1; tick <= 31; tick++) Step(state, tick / 60d, targets, hits, evolved: true);
            var support = hits.Single(hit => hit.Kind == ExpandedMosesStaffHitKind.Lane);
            Check.Equal(3L, support.WaveId);
            Check.Near(18.8, support.Damage);
        });

        test("Staff e.03 adds a fifth pulse at two seconds and ends on the exact 2.4-second boundary after restore", () =>
        {
            var state = new ExpandedMosesStaffState();
            var hits = new List<ExpandedMosesStaffHit>();
            var targets = new Dictionary<int, Vector2> { [1] = new Vector2(.04f, 0) };
            Step(state, 0, targets, hits, evolved: true, ranks: Rank("e.03"));
            for (var tick = 1; tick <= 120; tick++) Step(state, tick / 60d, targets, hits, evolved: true);
            Check.Equal(4, hits.Count(hit => hit.Kind == ExpandedMosesStaffHitKind.Lane));
            var loaded = RoundTrip(state);
            Step(loaded, 121 / 60d, targets, hits, evolved: true);
            Check.Equal(5, hits.Count(hit => hit.Kind == ExpandedMosesStaffHitKind.Lane));
            var active = loaded.Waves.Single();
            Check.Near(2.4, active.LaneDuration);
            for (var tick = 122; tick < 145; tick++) Step(loaded, tick / 60d, targets, hits, evolved: true);
            Check.True(active.HasLane);
            loaded.Validate(144 / 60d, true);
            Step(loaded, 145 / 60d, targets, hits, evolved: true);
            Check.Equal(0, loaded.ActiveLaneCount);
            Check.Equal(5, hits.Count(hit => hit.Kind == ExpandedMosesStaffHitKind.Lane));
            loaded.Validate(145 / 60d, true);
        });

        test("Staff total original ordinal survives a rejected contact, dead-ledger pruning and checkpoint restore", () =>
        {
            var state = new ExpandedMosesStaffState();
            var targets = new Dictionary<int, Vector2> { [1] = new Vector2(.18f, 0), [2] = new Vector2(.20f, 0) };
            var hits = new List<ExpandedMosesStaffHit>();
            void Tick(ExpandedMosesStaffState current, int tick, int level, Func<ExpandedMosesStaffHit, bool> apply) =>
                ExpandedMosesStaff.Step(current, tick / 60d, Vector2.Zero, Vector2.UnitX, level, false, 24, 3,
                    1, .9, 7, 5, () => targets, targets, apply);

            Tick(state, 0, 1, _ => true);
            Tick(state, 1, 1, hit =>
            {
                hits.Add(hit);
                if (hit.TargetId == 1) return false;
                targets.Remove(2);
                return true;
            });
            Check.True(hits.Select(hit => (hit.TargetId, hit.OriginalBaseDamage, hit.TargetOrdinal)).SequenceEqual(
                new[] { (1, 20d, 1), (2, 20d, 1) }));
            var preview = state.Copy();
            preview.Waves.Single().OriginalBaseDamage = 0;
            preview.Waves.Single().OriginalHitCount = 0;
            var old = RoundTrip(preview);
            old.Validate(1 / 60d, false);

            targets.Clear();
            Tick(state, 2, 8, _ => true);
            Check.Equal(0, state.Waves.Single().HitIds.Count);
            var loaded = RoundTrip(state);
            loaded.Validate(2 / 60d, false);
            var corrupt = loaded.Copy();
            corrupt.Waves.Single().OriginalHitCount = 6211;
            Check.Throws<ArgumentException>(() => corrupt.Validate(2 / 60d, false));
            targets[3] = new Vector2(.40f, 0);
            Tick(loaded, 3, 8, hit => { hits.Add(hit); return true; });
            Check.Equal((3, 20d, 2), (hits.Last().TargetId, hits.Last().OriginalBaseDamage, hits.Last().TargetOrdinal));
            Tick(old, 3, 8, hit => { hits.Add(hit); return true; });
            Check.Equal((3, 0d, 2), (hits.Last().TargetId, hits.Last().OriginalBaseDamage, hits.Last().TargetOrdinal));

            var evolved = new ExpandedMosesStaffState();
            targets[1] = new Vector2(.04f, 0);
            Step(evolved, 0, targets, hits, evolved: true);
            Step(evolved, 1 / 60d, targets, hits, evolved: true);
            var lane = hits.Single(hit => hit.Kind == ExpandedMosesStaffHitKind.Lane);
            Check.Equal((0d, 0), (lane.OriginalBaseDamage, lane.TargetOrdinal));
        });
    }

    private static ExpandedMosesStaffRankOptions Rank(string id) => ExpandedMosesStaffRankOptions.FromRanks(
        new Dictionary<string, int> { ["weapon.moses_staff." + id] = 1 });

    private static void CheckAngles(ExpandedMosesStaffState state, params double[] expected)
    {
        Check.Equal(expected.Length, state.Waves.Count);
        foreach (var (wave, angle) in state.Waves.Zip(expected))
            Check.Near(angle, Math.Atan2(wave.Direction.Y, wave.Direction.X) * 180 / Math.PI, 1e-4);
    }

    private static int Step(ExpandedMosesStaffState state, double now, IReadOnlyDictionary<int, Vector2> targets,
        List<ExpandedMosesStaffHit> hits, bool evolved = false, double damage = 40, double cycle = 3,
        int amount = 1, double width = .9, double range = 7, double speed = 5, Vector2? facing = null,
        IReadOnlyDictionary<int, Vector2>? previousTargets = null, Func<bool>? original = null, Func<bool>? lane = null,
        ExpandedMosesStaffRankOptions ranks = default, Func<int, bool>? isElite = null) =>
        ExpandedMosesStaff.Step(state, now, Vector2.Zero, facing ?? Vector2.UnitX, 8, evolved, damage, cycle,
            amount, width, range, speed, () => targets, previousTargets ?? targets, hit =>
            {
                hits.Add(hit);
                return true;
            }, original, lane, ranks, isElite);

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
