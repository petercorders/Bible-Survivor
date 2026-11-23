using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Runtime.Serialization.Json;

namespace BibleSurvivor.Core.Tests;

internal static class ExpandedDanielLionTests
{
    public static void Register(Action<string, Action> test)
    {
        test("Daniel lions prioritize nearer targets and split equidistant targets in stable index order", () =>
        {
            var state = new ExpandedDanielLionState();
            var enemies = new Dictionary<int, Vector2>
            {
                [3] = new Vector2(.5f, 0),
                [1] = new Vector2(-.5f, 0),
            };
            var hits = new List<ExpandedDanielLionHit>();
            Check.Equal(2, Step(state, 0, enemies, hits, amount: 2));
            Check.True(hits.Select(hit => (hit.LionIndex, hit.TargetId)).SequenceEqual(new[] { (0, 1), (1, 3) }));
            Check.True(hits.All(hit => hit.Kind == ExpandedDanielLionHitKind.Claw && hit.Damage == 50));
            Check.Equal(0, Step(state, .999, enemies, hits, amount: 2));
            enemies[3] = new Vector2(.6f, 0);
            hits.Clear();
            Check.Equal(2, Step(state, 1, enemies, hits, amount: 2));
            Check.True(hits.Select(hit => (hit.LionIndex, hit.TargetId)).SequenceEqual(new[] { (0, 1), (1, 1) }));
            state.Validate(1, false);
        });

        test("Daniel lions enter a return state past the leash until within the player return radius", () =>
        {
            var state = new ExpandedDanielLionState();
            var enemies = new Dictionary<int, Vector2>
            {
                [2] = new Vector2(7.01f, 0),
                [7] = new Vector2(-7, 0),
            };
            var hits = new List<ExpandedDanielLionHit>();
            Step(state, 0, enemies, hits);
            Step(state, 1 / 60d, enemies, hits);
            Check.Equal(7, state.Lions[0].TargetId);
            Check.Near(-5d / 60d, state.Lions[0].Position.X);

            var movedPlayer = new Vector2(10, 0);
            enemies[2] = movedPlayer;
            Step(state, 2 / 60d, enemies, hits, player: movedPlayer);
            Check.True(state.Lions[0].Returning);
            Check.Equal(0, state.Lions[0].TargetId);
            for (var tick = 3; tick <= 63; tick++) Step(state, tick / 60d, enemies, hits, player: movedPlayer);
            Check.True(state.Lions[0].Returning);
            Check.True(Vector2.Distance(state.Lions[0].Position, movedPlayer) < 7);
            Check.Equal(0, hits.Count);

            var nextTick = 64;
            while (state.Lions[0].Returning && nextTick < 130)
                Step(state, nextTick++ / 60d, enemies, hits, player: movedPlayer);
            Check.True(!state.Lions[0].Returning);
            Check.True(Vector2.Distance(state.Lions[0].Position, movedPlayer) <= 1);
            Check.Equal(0, hits.Count);
            Step(state, nextTick / 60d, enemies, hits, player: movedPlayer);
            Check.Equal(2, state.Lions[0].TargetId);
            state.Validate(nextTick / 60d, false);
        });

        test("Den Guardian saves each effective leap origin, skips visited targets and roars on the four second clock", () =>
        {
            var state = new ExpandedDanielLionState();
            var enemies = new Dictionary<int, Vector2>
            {
                [1] = Vector2.Zero,
                [4] = new Vector2(2.5f, 0),
                [6] = new Vector2(5, 0),
                [8] = new Vector2(6.75f, 0),
                [9] = new Vector2(6.81f, 0),
            };
            var hits = new List<ExpandedDanielLionHit>();
            Step(state, 0, enemies, hits, evolved: true);
            Step(state, 1, enemies, hits, evolved: true);
            Check.True(state.Leap == null);
            Step(state, 2, enemies, hits, evolved: true, accept: hit =>
            {
                if (hit.Kind == ExpandedDanielLionHitKind.Claw) enemies.Remove(hit.TargetId);
                return true;
            });
            Check.Equal(3L, state.GuardianHits);
            Check.Near(2.5, state.Leap!.At);
            Check.True(state.Leap.VisitedIds.SequenceEqual(new[] { 1 }));
            state.Validate(2, true);

            var copy = state.Copy();
            copy.Leap!.VisitedIds.Clear();
            Check.Equal(1, state.Leap.VisitedIds.Count);
            Check.Throws<ArgumentException>(() => copy.Validate(2, true));
            state = Restore(state);
            hits.Clear();
            Step(state, 2.499, enemies, hits, evolved: true);
            Check.Equal(0, hits.Count);
            Step(state, 2.5, enemies, hits, evolved: true);
            Check.Equal((ExpandedDanielLionHitKind.Leap, 4, 50d),
                (hits[0].Kind, hits[0].TargetId, hits[0].Damage));
            Check.Near(3, state.Leap!.At);
            Check.Equal(new Vector2(2.5f, 0), state.Leap.Origin);
            Check.True(state.Leap.VisitedIds.SequenceEqual(new[] { 1, 4 }));
            state = Restore(state);
            state.Validate(2.5, true);
            Step(state, 3, enemies, hits, evolved: true);
            Check.Equal((ExpandedDanielLionHitKind.Leap, 6, 30d),
                (hits[1].Kind, hits[1].TargetId, hits[1].Damage));
            Check.True(state.Leap == null);
            Check.Equal(3L, state.GuardianHits);
            Check.Equal(new Vector2(5, 0), state.Lions[0].Position);

            Step(state, 3.999, enemies, hits, evolved: true);
            Check.True(hits.All(hit => hit.Kind != ExpandedDanielLionHitKind.Roar));
            Step(state, 4, enemies, hits, evolved: true);
            var roar = hits.Where(hit => hit.Kind == ExpandedDanielLionHitKind.Roar).ToArray();
            Check.True(roar.Select(hit => hit.TargetId).SequenceEqual(new[] { 6, 8 }));
            Check.True(roar.All(hit => hit.Damage == 17.5 && hit.Knockback == .45 && hit.Center == new Vector2(5, 0)));
            Check.Near(8, state.NextRoarAt);
            state.Validate(4, true);
        });

        test("Den Guardian ignores rejected claws and cancels leap and roar reservations on form change", () =>
        {
            var state = new ExpandedDanielLionState();
            var enemies = new Dictionary<int, Vector2> { [1] = Vector2.Zero, [2] = new Vector2(1.5f, 0) };
            var hits = new List<ExpandedDanielLionHit>();
            Step(state, 0, enemies, hits, evolved: true, amount: 2, accept: _ => false);
            Check.Equal(0L, state.GuardianHits);
            Check.Equal(0, hits.Count);
            Step(state, .1, enemies, hits, evolved: true, amount: 2);
            Step(state, 1.1, enemies, hits, evolved: true, amount: 2);
            Step(state, 2.1, enemies, hits, evolved: true, amount: 2);
            Check.Near(2.6, state.Leap!.At);
            var pack = state.Lions.Select(lion => lion.Position).ToArray();

            state.ResetForForm(false, 2.2);
            Check.True(state.Lions.Select(lion => lion.Position).SequenceEqual(pack));
            hits.Clear();
            Step(state, 2.6, enemies, hits, amount: 2);
            Step(state, 4, enemies, hits, amount: 2);
            Check.True(hits.All(hit => hit.Kind == ExpandedDanielLionHitKind.Claw));
            Check.Equal(0L, state.GuardianHits);
            Check.Equal(0d, state.NextRoarAt);
            state.Validate(4, false);
        });
    }

    private static ExpandedDanielLionState Restore(ExpandedDanielLionState state)
    {
        using var bytes = new MemoryStream();
        var serializer = new DataContractJsonSerializer(typeof(ExpandedDanielLionState));
        serializer.WriteObject(bytes, state);
        bytes.Position = 0;
        return (ExpandedDanielLionState)serializer.ReadObject(bytes)!;
    }

    private static int Step(ExpandedDanielLionState state, double time, Dictionary<int, Vector2> enemies,
        List<ExpandedDanielLionHit> hits, bool evolved = false, int amount = 1, Vector2 player = default,
        Func<ExpandedDanielLionHit, bool>? accept = null) =>
        ExpandedDanielLion.Step(state, time, player, 8, evolved, damage: 50, cycle: 1, amount,
            radius: .65, speed: 5, () => enemies, hit =>
            {
                if (accept != null && !accept(hit)) return false;
                hits.Add(hit);
                return true;
            });
}
