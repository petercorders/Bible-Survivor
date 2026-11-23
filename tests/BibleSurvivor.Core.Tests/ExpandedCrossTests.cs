using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Runtime.Serialization.Json;

namespace BibleSurvivor.Core.Tests;

internal static class ExpandedCrossTests
{
    public static void Register(Action<string, Action> test)
    {
        test("Expanded cross aims at the nearest target with stable ties and symmetric 12 degree spread", () =>
        {
            var state = new ExpandedCrossState();
            var targets = new Dictionary<int, Vector2>
            {
                [17] = new Vector2(0, -1), [4] = new Vector2(0, 1), [1] = new Vector2(2, 0),
            };
            var hits = new List<ExpandedCrossHit>();
            Check.Equal(3, Step(state, 0, targets, hits, count: 3, cycle: .151));
            var angles = state.Shots.Select(shot => Math.Atan2(shot.Direction.Y, shot.Direction.X) * 180 / Math.PI);
            foreach (var (angle, expected) in angles.Zip(new[] { 78d, 90d, 102d }))
                Check.Near(expected, angle, 1e-4);
            targets.Clear();
            for (var tick = 1; tick <= 9; tick++) Check.Equal(0, Step(state, tick / 60d, targets, hits, cycle: .151));
            Check.Equal(1, Step(state, 10 / 60d, targets, hits, cycle: .151, facing: Vector2.Zero));
            Check.Equal(Vector2.UnitX, state.Shots.Last().Direction);
            Check.Near(.302, state.NextCycle);
            state.Validate(10 / 60d, false);
        });

        test("Expanded cross keeps each leg's four-hit budget and rehit interval while returning", () =>
        {
            var state = new ExpandedCrossState();
            var targets = Enumerable.Range(1, 6).ToDictionary(id => id, id => new Vector2(id * .8f, 0));
            var hits = new List<ExpandedCrossHit>();
            Step(state, 0, targets, hits);
            for (var tick = 1; tick <= 100; tick++)
            {
                Step(state, tick / 60d, targets, hits);
                state.Validate(tick / 60d, false);
            }
            Check.True(HitIds(hits, ExpandedCrossHitKind.Outbound).SequenceEqual(new[] { 1, 2, 3, 4 }));
            Check.True(HitIds(hits, ExpandedCrossHitKind.Return).SequenceEqual(new[] { 6, 5, 4, 3 }));
            Check.Equal(0, state.ActiveShotCount);

            targets.Clear();
            targets.Add(1, new Vector2(4.8f, 0));
            hits.Clear();
            state = new ExpandedCrossState();
            for (var tick = 0; tick <= 100; tick++) Step(state, tick / 60d, targets, hits);
            Check.True(HitIds(hits, ExpandedCrossHitKind.Outbound).SequenceEqual(new[] { 1 }));
            Check.Equal(0, HitIds(hits, ExpandedCrossHitKind.Return).Length);
        });

        test("Expanded cross sweeps a target crossing during the tick and expires a pursuit after three seconds", () =>
        {
            var state = new ExpandedCrossState();
            var targets = new Dictionary<int, Vector2>();
            var hits = new List<ExpandedCrossHit>();
            Step(state, 0, targets, hits);
            targets.Add(1, new Vector2(.04f, 1));
            var previous = new Dictionary<int, Vector2> { [1] = new Vector2(.04f, -1) };
            Step(state, 1 / 60d, targets, hits, previous: previous);
            Check.Equal(1, hits.Count);
            targets.Clear();
            for (var tick = 2; tick <= 229; tick++)
            {
                Step(state, tick / 60d, targets, hits, player: new Vector2(0, 100 + tick), reserveOriginal: () => false);
                state.Validate(tick / 60d, false);
            }
            var returning = state.Shots.Single();
            Check.Near(5 / 6d, returning.ReturnStartedAt!.Value);
            Check.True(returning.Position.Y > 0);
            Step(state, 230 / 60d, targets, hits, player: new Vector2(0, 330), reserveOriginal: () => false);
            Check.Equal(0, state.Shots.Count);
            state.Validate(230 / 60d, false);
        });

        test("Expanded cross evolution keeps six original hits and merges overlapping light thorns", () =>
        {
            var state = new ExpandedCrossState();
            var targets = new Dictionary<int, Vector2>();
            var hits = new List<ExpandedCrossHit>();
            Step(state, 0, targets, hits, evolved: true, count: 2);
            for (var id = 1; id <= 7; id++)
                targets.Add(id, new Vector2(id * .65f, 0));
            targets.Add(8, new Vector2(.01f, 0));
            for (var tick = 1; tick <= 32; tick++) Step(state, tick / 60d, targets, hits, evolved: true);
            var initialThorns = hits.Where(hit => hit.Kind == ExpandedCrossHitKind.Thorns && hit.TargetId == 8).ToArray();
            Check.Equal(2, initialThorns.Length);
            Check.True(initialThorns.All(hit => hit.ShotId == 1 && Math.Abs(hit.Damage - 10.5) < 1e-8));
            state.Validate(32 / 60d, true);

            state = new ExpandedCrossState();
            hits.Clear();
            for (var tick = 0; tick <= 31; tick++)
                Step(state, tick / 60d, targets, hits, evolved: true, cycle: .5, damage: tick < 30 ? 30 : 60);
            var strongestThorns = hits.Where(hit => hit.Kind == ExpandedCrossHitKind.Thorns && hit.TargetId == 8).ToArray();
            Check.Equal(2, strongestThorns.Length);
            Check.Equal(2L, strongestThorns[1].ShotId);
            Check.Near(21, strongestThorns[1].Damage);

            state = new ExpandedCrossState();
            targets.Remove(8);
            hits.Clear();
            for (var tick = 0; tick <= 51; tick++) Step(state, tick / 60d, targets, hits, evolved: true);
            Check.Equal(6, HitIds(hits, ExpandedCrossHitKind.Outbound).Length);
            Check.Equal(6, state.Shots[0].OutboundHits);
            Check.True(ShotTrailCrossesBothLegs(state.Shots[0]));
            state.Validate(51 / 60d, true);
        });

        test("Expanded cross restores homing, per-leg target history and expiring trail pieces deterministically", () =>
        {
            var state = new ExpandedCrossState();
            var targets = new Dictionary<int, Vector2> { [1] = new Vector2(1, 0), [2] = new Vector2(4.8f, 0) };
            var prior = new List<ExpandedCrossHit>();
            for (var tick = 0; tick <= 56; tick++)
                Step(state, tick / 60d, targets, prior, evolved: true, player: tick < 50 ? Vector2.Zero : new Vector2(0, 2));
            Check.True(state.Shots.Single().Returning);
            state.Validate(56 / 60d, true);

            var altered = state.Copy();
            altered.Shots[0].ReturnHits = 10;
            Check.Throws<ArgumentException>(() => altered.Validate(56 / 60d, true));
            altered = state.Copy();
            altered.Shots[0].Thorns[0].BornAt = -1;
            Check.Throws<ArgumentException>(() => altered.Validate(56 / 60d, true));

            var loaded = RoundTrip(state);
            var hits = new List<ExpandedCrossHit>();
            var restored = new List<ExpandedCrossHit>();
            for (var tick = 57; tick <= 215; tick++)
            {
                var now = tick / 60d;
                var player = new Vector2(0, (float)Math.Sin(now));
                Step(state, now, targets, hits, evolved: true, player: player, reserveOriginal: () => false);
                Step(loaded, now, targets, restored, evolved: true, player: player, reserveOriginal: () => false);
                loaded.Validate(now, true);
            }
            Check.True(hits.Select(HitSignature).SequenceEqual(restored.Select(HitSignature)));
            Check.True(hits.Any(hit => hit.Kind == ExpandedCrossHitKind.Thorns));
            Check.Equal(0, loaded.Shots.Count);
            loaded.ResetForForm(false, 215 / 60d);
            loaded.Validate(215 / 60d, false);
        });

        test("Expanded cross reserves projectile and thorn capacity independently", () =>
        {
            var state = new ExpandedCrossState();
            var hits = new List<ExpandedCrossHit>();
            var targets = new Dictionary<int, Vector2>();
            var original = 2;
            var support = 1;
            Check.Equal(2, Step(state, 0, targets, hits, evolved: true, count: 3,
                reserveOriginal: () => original-- > 0, reserveTrail: () => support-- > 0));
            Check.Equal(2, state.ActiveShotCount);
            Check.Equal(1, state.ActiveTrailCount);
            Check.Equal(3L, state.NextShotId);
            state.Validate(0, true);
        });
    }

    private static int[] HitIds(IEnumerable<ExpandedCrossHit> hits, ExpandedCrossHitKind kind) =>
        hits.Where(hit => hit.Kind == kind).Select(hit => hit.TargetId).ToArray();

    private static bool ShotTrailCrossesBothLegs(ExpandedCrossShotState shot) =>
        shot.Thorns.Any(piece => piece.Returning) && shot.Thorns.Any(piece => !piece.Returning);

    private static (ExpandedCrossHitKind, int, Vector2, long, double) HitSignature(ExpandedCrossHit hit) =>
        (hit.Kind, hit.TargetId, hit.Origin, hit.ShotId, hit.Damage);

    private static int Step(ExpandedCrossState state, double now, IReadOnlyDictionary<int, Vector2> targets,
        List<ExpandedCrossHit> hits, bool evolved = false, double cycle = 2, int count = 1,
        Vector2? player = null, Vector2? facing = null, IReadOnlyDictionary<int, Vector2>? previous = null,
        Func<bool>? reserveOriginal = null, Func<bool>? reserveTrail = null, double damage = 30) =>
        ExpandedCross.Step(state, now, player ?? Vector2.Zero, facing ?? Vector2.UnitX, evolved ? 8 : 1,
            evolved, damage, cycle, count, .28, 5, 6, () => targets, previous ?? targets, hit => { hits.Add(hit); return true; },
            reserveOriginal, reserveTrail);

    private static ExpandedCrossState RoundTrip(ExpandedCrossState state)
    {
        using var stream = new MemoryStream();
        var serializer = new DataContractJsonSerializer(typeof(ExpandedCrossState));
        serializer.WriteObject(stream, state);
        stream.Position = 0;
        return (ExpandedCrossState)serializer.ReadObject(stream)!;
    }
}
