using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Runtime.Serialization.Json;

namespace BibleSurvivor.Core.Tests;

internal static class ExpandedLightningTests
{
    public static void Register(Action<string, Action> test)
    {
        test("Expanded lightning targets nearest distinct centers before reusing and strikes their fixed positions", () =>
        {
            var state = new ExpandedLightningState();
            var targets = new Dictionary<int, Vector2>
            {
                [17] = new Vector2(-8, 0),
                [3] = new Vector2(8, 0),
                [1] = new Vector2(8.01f, 0),
            };
            var hits = new List<ExpandedLightningHit>();
            Step(state, 0, targets, hits, amount: 4);
            Check.True(state.Pending.Select(attack => attack.AssignedTargetId).SequenceEqual(new[] { 3, 17, 3, 17 }));
            Check.True(state.Pending.Select(attack => attack.Id).SequenceEqual(new long[] { 1, 2, 3, 4 }));
            state.Validate(0, false);
            targets[3] = new Vector2(12, 0);
            targets.Remove(1);
            targets.Add(44, new Vector2(9, 0));
            targets.Add(45, new Vector2(9.01f, 0));
            Step(state, .149, targets, hits, amount: 4);
            Check.Equal(0, hits.Count);
            Step(state, .15, targets, hits, amount: 4);
            Check.True(hits.Select(hit => hit.TargetId).SequenceEqual(new[] { 44, 17, 44, 17 }));
            Check.True(hits.All(hit => hit.Kind == ExpandedLightningHitKind.Strike && hit.Damage == 40));
            Check.Equal(0, state.Pending.Count);
        });

        test("Expanded lightning waits for targets and keeps the exact cycle schedule through tick rounding", () =>
        {
            const double cycle = .151;
            var state = new ExpandedLightningState();
            var targets = new Dictionary<int, Vector2> { [1] = new Vector2(8.01f, 0) };
            var hits = new List<ExpandedLightningHit>();
            for (var tick = 0; tick < 3; tick++) Step(state, tick / 60d, targets, hits, cycle: cycle);
            Check.Equal(0L, state.Cycle);
            Check.True(state.WaitingForTarget);
            Check.Equal(0, state.Pending.Count);

            targets[1] = Vector2.Zero;
            const double enteredAt = 3 / 60d;
            Step(state, enteredAt, targets, hits, cycle: cycle);
            Check.Equal(1L, state.Cycle);
            Check.True(!state.WaitingForTarget);
            Check.Near(enteredAt + .15, state.Pending.Single().At);
            Check.Near(enteredAt + cycle, state.NextCycle);
            for (var tick = 4; tick <= 13; tick++) Step(state, tick / 60d, targets, hits, cycle: cycle);
            Check.Equal(2L, state.Cycle);
            Check.Equal(1, hits.Count);
            Check.Near(enteredAt + cycle + .15, state.Pending.Single().At);
            Check.Near(enteredAt + 2 * cycle, state.NextCycle);
            state.Validate(13 / 60d, false);
        });

        test("Expanded lightning evolution chains from effective hits and restores one delayed aftershock", () =>
        {
            var state = new ExpandedLightningState();
            var targets = new Dictionary<int, Vector2>
            {
                [1] = Vector2.Zero,
                [2] = new Vector2(3, 0),
                [3] = new Vector2(6, 0),
                [4] = new Vector2(9, 0),
                [5] = new Vector2(12, 0),
            };
            var hits = new List<ExpandedLightningHit>();
            Step(state, 0, targets, hits, evolved: true, amount: 2);
            Step(state, .15, targets, hits, evolved: true, amount: 2, accept: hit =>
            {
                if (hit.Kind == ExpandedLightningHitKind.Strike && hit.TargetId == 1) targets.Remove(1);
                return true;
            });
            var firstStrike = hits.Where(hit => hit.StrikeId == 1).ToArray();
            Check.True(firstStrike.Select(hit => hit.TargetId).SequenceEqual(new[] { 1, 2, 3, 4 }));
            Check.Equal(ExpandedLightningHitKind.Strike, firstStrike[0].Kind);
            Check.True(firstStrike.Skip(1).All(hit => hit.Kind == ExpandedLightningHitKind.Chain && hit.Damage == 28));
            Check.Equal(1, state.Pending.Count);
            var pending = state.Pending.Single();
            Check.True(pending.Aftershock && pending.VisitedIds!.SequenceEqual(new[] { 1, 2, 3, 4 }));
            Check.Near(.65, pending.At);
            Check.Near(16, pending.Damage);
            Check.Near(1.4, pending.Radius);
            Check.Equal(Vector2.Zero, new Vector2(pending.X, pending.Y));
            state.Validate(.15, true);

            var copy = state.Copy();
            copy.Pending[0].VisitedIds!.Clear();
            Check.Equal(4, pending.VisitedIds!.Count);
            Check.Throws<ArgumentException>(() => copy.Validate(.15, true));
            using var save = new MemoryStream();
            var serializer = new DataContractJsonSerializer(typeof(ExpandedLightningState));
            serializer.WriteObject(save, state);
            save.Position = 0;
            var restored = (ExpandedLightningState)serializer.ReadObject(save)!;
            restored.Validate(.15, true);
            targets[30] = new Vector2(1.4f, 0);
            targets[31] = new Vector2(1.401f, 0);
            var replayHits = new List<ExpandedLightningHit>();
            Step(restored, .649, targets, replayHits, evolved: true, amount: 2);
            Check.Equal(0, replayHits.Count);
            Step(restored, .65, targets, replayHits, evolved: true, amount: 2);
            Check.Equal(1, replayHits.Count);
            Check.Equal((ExpandedLightningHitKind.Aftershock, 30, 16d),
                (replayHits[0].Kind, replayHits[0].TargetId, replayHits[0].Damage));
            Check.Equal(0, restored.Pending.Count);
        });

        test("Expanded lightning does not chain on rejected hits and a form change cancels pending strikes", () =>
        {
            var state = new ExpandedLightningState();
            var targets = new Dictionary<int, Vector2> { [1] = Vector2.Zero, [2] = new Vector2(2, 0) };
            var hits = new List<ExpandedLightningHit>();
            Step(state, 0, targets, hits, evolved: true);
            Step(state, .15, targets, hits, evolved: true, accept: _ => false);
            Check.Equal(0, hits.Count);
            Check.Equal(0, state.Pending.Count);

            Step(state, 3, targets, hits, evolved: true);
            var pending = state.Pending.Single();
            Check.Equal(2L, pending.Id);
            Check.Equal(2L, pending.Cycle);
            var duplicate = state.Copy();
            duplicate.Pending.Add(duplicate.Pending[0]);
            Check.Throws<ArgumentException>(() => duplicate.Validate(3, true));

            state.ResetForForm(false, 3.05);
            Check.Equal(0, state.Pending.Count);
            Step(state, 3.05, targets, hits);
            Check.Equal(3L, state.Pending.Single().Id);
            Step(state, 3.15, targets, hits);
            Check.Equal(0, hits.Count);
            Step(state, 3.2, targets, hits);
            Check.Equal(1, hits.Count);
            Check.Equal(3L, hits[0].StrikeId);
            Check.Equal(ExpandedLightningHitKind.Strike, hits[0].Kind);
            state.Validate(3.2, false);
        });
    }

    private static void Step(ExpandedLightningState state, double time, Dictionary<int, Vector2> targets,
        List<ExpandedLightningHit> hits, bool evolved = false, int amount = 1, double cycle = 3,
        Func<ExpandedLightningHit, bool>? accept = null)
    {
        ExpandedLightning.Step(state, time, Vector2.Zero, 8, evolved, 40, cycle, amount, 1,
            () => targets, hit =>
            {
                if (accept != null && !accept(hit)) return false;
                hits.Add(hit);
                return true;
            });
    }
}
