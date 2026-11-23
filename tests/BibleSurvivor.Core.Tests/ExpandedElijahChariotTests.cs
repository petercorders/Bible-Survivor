using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Runtime.Serialization.Json;

namespace BibleSurvivor.Core.Tests;

internal static class ExpandedElijahChariotTests
{
    public static void Register(Action<string, Action> test)
    {
        test("Elijah chariot locks a symmetric warning and sweeps the traveled strip exactly once per original", () =>
        {
            var state = new ExpandedElijahChariotState();
            var targets = new Dictionary<int, Vector2>
            {
                [11] = new Vector2(.2f, -.45f),
                [3] = new Vector2(.2f, .45f),
                [5] = new Vector2(10, -.45f),
                [1] = new Vector2(10.01f, -.45f),
                [7] = new Vector2(.2f, 1.06f),
            };
            var hits = new List<ExpandedElijahChariotHit>();
            Step(state, 0, targets, hits, amount: 2);
            Check.True(state.Paths.Select(path => path.Origin).SequenceEqual(
                new[] { new Vector2(0, -.45f), new Vector2(0, .45f) }));
            Check.True(state.Paths.All(path => path.WarningAt == 0 && path.MoveAt == .15));
            state.Validate(0, false);
            Step(state, .149, targets, hits, amount: 2);
            Check.Equal(0, hits.Count);
            Step(state, .2, targets, hits, amount: 2, player: new Vector2(4, 2), facing: -Vector2.UnitX);
            Check.True(hits.Select(hit => (hit.PathId, hit.TargetId)).SequenceEqual(new[] { (1L, 11), (2L, 3) }));
            Check.True(state.Paths.All(path => path.Direction == Vector2.UnitX));
            Check.Near(.4, state.Paths[0].Distance);
            state.Validate(.2, false);
            Step(state, .2, targets, hits, amount: 2);
            Check.Equal(2, hits.Count);
            Step(state, 1.4, targets, hits, amount: 2);
            Check.True(hits.Select(hit => hit.TargetId).SequenceEqual(new[] { 11, 3, 5 }));
            Check.True(hits.All(hit => hit.Kind == ExpandedElijahChariotHitKind.Body && hit.Damage == 80));
            Check.Equal(0, state.Paths.Count);
        });

        test("Ascending Chariot restores swept whirl candidates and prioritizes a later body hit", () =>
        {
            var state = new ExpandedElijahChariotState();
            var targets = new Dictionary<int, Vector2>
            {
                [6] = new Vector2(2, 1),
                [4] = new Vector2(2, -1),
                [1] = new Vector2(2, 0),
                [9] = new Vector2(2, 1.16f),
            };
            var history = new List<ExpandedElijahChariotHit>();
            Step(state, 0, targets, history, evolved: true);
            Step(state, .4, targets, history, evolved: true);
            Check.True(state.Paths[0].WhirlwindCandidateIds.SequenceEqual(new[] { 4, 6 }));
            Check.True(state.Paths[0].BodyHitIds.SequenceEqual(new[] { 1 }));
            Check.True(history.All(hit => hit.Kind != ExpandedElijahChariotHitKind.Whirlwind));
            state.Validate(.4, true);

            var copied = state.Copy();
            copied.Paths[0].WhirlwindCandidateIds.Clear();
            Check.Equal(2, state.Paths[0].WhirlwindCandidateIds.Count);
            var restored = Restore(state);
            restored.Validate(.4, true);
            targets[6] = new Vector2(4, 0);
            var hits = new List<ExpandedElijahChariotHit>();
            Step(restored, .65, targets, hits, evolved: true);
            Check.True(restored.Paths[0].BodyHitIds.SequenceEqual(new[] { 1, 6 }));
            Check.True(restored.Paths[0].WhirlwindCandidateIds.SequenceEqual(new[] { 4 }));
            Step(restored, 1.4, targets, hits, evolved: true);
            var whirlwind = hits.Single(hit => hit.Kind == ExpandedElijahChariotHitKind.Whirlwind);
            Check.Equal((4, 36d), (whirlwind.TargetId, whirlwind.Damage));
            Check.True(restored.Paths[0].BodyEnded);
            Check.Equal(0, restored.Paths[0].WhirlwindCandidateIds.Count);
            Check.Equal(new Vector2(10, 0), restored.Paths[0].Position);
            restored.Validate(1.4, true);
            Step(restored, 1.4, targets, hits, evolved: true);
            Check.Equal(1, hits.Count(hit => hit.Kind == ExpandedElijahChariotHitKind.Whirlwind));
        });

        test("Ascending Chariot overlap emits one trail hit per target and fixed tick using damage then path order", () =>
        {
            foreach (var (firstDamage, secondDamage, expectedPath, expectedDamage) in new[]
            {
                (80d, 100d, 2L, 35d), (100d, 80d, 1L, 35d), (80d, 80d, 1L, 28d),
            })
            {
                var state = new ExpandedElijahChariotState();
                var targets = new Dictionary<int, Vector2>
                {
                    [7] = Vector2.Zero,
                    [1] = new Vector2(0, .46f),
                };
                var hits = new List<ExpandedElijahChariotHit>();
                for (var tick = 0; tick < 39; tick++)
                    Step(state, tick / 60d, targets, hits, evolved: true, cycle: .5,
                        damage: tick < 30 ? firstDamage : secondDamage);
                hits.Clear();
                Step(state, .65, targets, hits, evolved: true, cycle: .5, damage: secondDamage);
                var trail = hits.Single(hit => hit.Kind == ExpandedElijahChariotHitKind.Trail);
                Check.Equal(7, trail.TargetId);
                Check.Equal(expectedPath, trail.PathId);
                Check.Near(expectedDamage, trail.Damage);
                state.Validate(.65, true);
            }
        });

        test("Ascending Chariot trail persists by point lifetime and form reset cancels all prior path events", () =>
        {
            var state = new ExpandedElijahChariotState();
            var targets = new Dictionary<int, Vector2>
            {
                [7] = Vector2.Zero,
                [4] = new Vector2(4, 0),
                [9] = new Vector2(10, 0),
            };
            var hits = new List<ExpandedElijahChariotHit>();
            for (var tick = 0; tick <= 98; tick++) Step(state, tick / 60d, targets, hits, evolved: true);
            var restored = Restore(state);
            restored.Validate(98 / 60d, true);
            var duplicate = state.Copy();
            duplicate.Paths.Add(duplicate.Paths[0]);
            Check.Throws<ArgumentException>(() => duplicate.Validate(98 / 60d, true));

            hits.Clear();
            Step(restored, 1.65, targets, hits, evolved: true);
            Check.True(hits.Where(hit => hit.Kind == ExpandedElijahChariotHitKind.Trail)
                .Select(hit => hit.TargetId).SequenceEqual(new[] { 4, 9 }));
            Check.Equal(4, restored.Paths[0].TrailTicksFired);
            restored.Validate(1.65, true);

            restored.ResetForForm(false, 1.7);
            Check.Equal(0, restored.Paths.Count);
            Step(restored, 1.7, targets, hits, facing: Vector2.Zero);
            var newPath = restored.Paths.Single();
            Check.Equal((2L, 2L, Vector2.UnitX), (newPath.Id, newPath.Cycle, newPath.Direction));
            hits.Clear();
            Step(restored, 2.15, targets, hits);
            Check.True(hits.All(hit => hit.Kind == ExpandedElijahChariotHitKind.Body && hit.PathId == 2));
            restored.Validate(2.15, false);
        });
    }

    private static ExpandedElijahChariotState Restore(ExpandedElijahChariotState state)
    {
        using var bytes = new MemoryStream();
        var serializer = new DataContractJsonSerializer(typeof(ExpandedElijahChariotState));
        serializer.WriteObject(bytes, state);
        bytes.Position = 0;
        return (ExpandedElijahChariotState)serializer.ReadObject(bytes)!;
    }

    private static void Step(ExpandedElijahChariotState state, double time, Dictionary<int, Vector2> targets,
        List<ExpandedElijahChariotHit> hits, bool evolved = false, int amount = 1, double cycle = 5,
        double damage = 80, Vector2 player = default, Vector2? facing = null)
    {
        ExpandedElijahChariot.Step(state, time, player, facing ?? Vector2.UnitX, 8, evolved,
            damage, cycle, amount, width: 1.2, speed: 8, range: 10, () => targets,
            hit => { hits.Add(hit); return true; });
    }
}
