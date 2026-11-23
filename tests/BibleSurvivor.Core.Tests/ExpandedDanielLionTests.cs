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

        test("Lion a.01 ranks elites within five of the player even if another target is closer to the lion", () =>
        {
            var state = new ExpandedDanielLionState();
            var enemies = new Dictionary<int, Vector2>
            {
                [1] = new Vector2(.1f, 0),
                [2] = new Vector2(0, 5),
                [3] = new Vector2(-5.001f, 0),
            };
            var hits = new List<ExpandedDanielLionHit>();
            Step(state, 0, enemies, hits, ranks: Rank("a.01"), isElite: id => id != 1);
            Check.Equal(2, state.Lions[0].TargetId);
            enemies[2] = new Vector2(0, 5.001f);
            Step(state, 1 / 60d, enemies, hits, ranks: Rank("a.01"), isElite: id => id != 1);
            Check.Equal(1, state.Lions[0].TargetId);
            Check.Equal(1, hits.Single().TargetId);
        });

        test("Lion a.02 restores an 8.05-unit leash and leaves target search at seven", () =>
        {
            var state = new ExpandedDanielLionState();
            var enemies = new Dictionary<int, Vector2>();
            var hits = new List<ExpandedDanielLionHit>();
            Step(state, 0, enemies, hits, ranks: Rank("a.02"));
            var loaded = Restore(state);
            enemies.Add(1, Vector2.Zero);
            enemies.Add(2, new Vector2(14, 0));
            Step(loaded, 1 / 60d, enemies, hits, player: new Vector2(8.05f, 0));
            Check.True(!loaded.Lions[0].Returning);
            Check.Equal(2, loaded.Lions[0].TargetId);
            Check.Equal(0, hits.Count);

            var beyond = Restore(state);
            Step(beyond, 1 / 60d, enemies, hits, player: new Vector2(8.051f, 0));
            Check.True(beyond.Lions[0].Returning);
            Check.Equal(0, beyond.Lions[0].TargetId);
            beyond.Validate(1 / 60d, false);
        });

        test("Lion b.01 spreads lions to an unassigned ordinary target on the inclusive nearest-plus-one boundary", () =>
        {
            var enemies = new Dictionary<int, Vector2>
            {
                [1] = new Vector2(.25f, 0),
                [2] = new Vector2(0, 1.25f),
                [3] = new Vector2(.5f, 0),
            };
            var state = new ExpandedDanielLionState();
            var hits = new List<ExpandedDanielLionHit>();
            Step(state, 0, enemies, hits, amount: 2, ranks: Rank("b.01"), isElite: id => id == 3);
            Check.True(state.Lions.Select(lion => lion.TargetId).SequenceEqual(new[] { 1, 2 }));
            Check.Equal(0, hits.Single().LionIndex);

            enemies[2] = new Vector2(0, 1.251f);
            var outside = new ExpandedDanielLionState();
            hits.Clear();
            Step(outside, 0, enemies, hits, amount: 2, ranks: Rank("b.01"), isElite: id => id == 3);
            Check.True(outside.Lions.Select(lion => lion.TargetId).SequenceEqual(new[] { 1, 1 }));
            Check.Equal(2, hits.Count);
        });

        test("Lion b.02 staggers only first claws against a sole searchable boss and restores the exact deadlines", () =>
        {
            var enemies = new Dictionary<int, Vector2> { [1] = Vector2.Zero, [2] = new Vector2(7.01f, 0) };
            var state = new ExpandedDanielLionState();
            var hits = new List<ExpandedDanielLionHit>();
            var ranks = Rank("b.02");
            int Tick(ExpandedDanielLionState pack, double at) =>
                Step(pack, at, enemies, hits, amount: 3, ranks: ranks, isBoss: id => id == 1);
            Check.Equal(1, Tick(state, 0));
            Check.Near(.12, state.Lions[1].NextClawAt);
            Check.Near(.24, state.Lions[2].NextClawAt);
            var loaded = Restore(state);
            loaded.Validate(0, false);
            Check.Equal(0, Tick(loaded, .119));
            Check.Equal(1, Tick(loaded, .12));
            Check.Equal(1, hits.Last().LionIndex);
            Check.Equal(0, Tick(loaded, .239));
            Check.Equal(1, Tick(loaded, .24));
            Check.Equal(2, hits.Last().LionIndex);
            Check.Equal(1, Tick(loaded, 1));
            Check.Equal(1, Tick(loaded, 1.12));
            Check.Equal(1, hits.Last().LionIndex);
            Check.Equal(1, Tick(loaded, 1.24));
            Check.Equal(2, hits.Last().LionIndex);
            loaded.Validate(1.24, false);

            var multiple = new ExpandedDanielLionState();
            enemies[2] = new Vector2(7, 0);
            Check.Equal(3, Tick(multiple, 0));
            enemies.Remove(2);
            Check.Equal(3, Tick(multiple, 1));

            var normal = new ExpandedDanielLionState();
            enemies.Clear();
            enemies.Add(2, Vector2.Zero);
            Check.Equal(3, Tick(normal, 0));
        });

        test("Lion e.01 freezes the extra leap coefficient on the evolved reservation through both restored hops", () =>
        {
            var enemies = new Dictionary<int, Vector2>
            {
                [1] = Vector2.Zero, [2] = new Vector2(2.5f, 0), [3] = new Vector2(5, 0),
            };
            var state = new ExpandedDanielLionState();
            var hits = new List<ExpandedDanielLionHit>();
            for (var tick = 0; tick <= 2; tick++)
                Step(state, tick, enemies, hits, evolved: true, ranks: Rank("e.01"));
            var loaded = Restore(state);
            loaded.Validate(2, true);
            Step(loaded, 2.5, enemies, hits, evolved: true);
            var resumed = Restore(loaded);
            resumed.Validate(2.5, true);
            Step(resumed, 3, enemies, hits, evolved: true);
            var leaps = hits.Where(hit => hit.Kind == ExpandedDanielLionHitKind.Leap).ToArray();
            Check.Equal(2, leaps.Length);
            Check.Near(57.5, leaps[0].Damage);
            Check.Near(37.5, leaps[1].Damage);
            Check.True(hits.Where(hit => hit.Kind == ExpandedDanielLionHitKind.Claw).All(hit => hit.Damage == 50));
        });

        test("Lion c.01 gives each lion its own radius-1.4 support roar on the four-second schedule", () =>
        {
            var state = new ExpandedDanielLionState();
            var enemies = new Dictionary<int, Vector2>
            {
                [1] = Vector2.Zero, [2] = new Vector2(1.4f, 0), [3] = new Vector2(1.401f, 0),
            };
            var hits = new List<ExpandedDanielLionHit>();
            var ranks = Rank("c.01");
            Step(state, 0, enemies, hits, amount: 2, ranks: ranks);
            var loaded = Restore(state);
            loaded.Validate(0, false);
            Step(loaded, 3.999, enemies, hits, amount: 2, ranks: ranks);
            Check.Equal(0, hits.Count(hit => hit.Kind == ExpandedDanielLionHitKind.Roar));
            Step(loaded, 4, enemies, hits, amount: 2, ranks: ranks);
            var roars = hits.Where(hit => hit.Kind == ExpandedDanielLionHitKind.Roar).ToArray();
            Check.True(roars.Select(hit => (hit.LionIndex, hit.TargetId)).SequenceEqual(
                new[] { (0, 1), (0, 2), (1, 1), (1, 2) }));
            Check.True(roars.All(hit => hit.Damage == 12.5 && hit.Knockback == 0));
            Check.True(loaded.Lions.All(lion => lion.NextPackRoarAt == 8));
        });

        test("Lion c.02 widens support and the evolved guardian roar by fifteen percent", () =>
        {
            var state = new ExpandedDanielLionState();
            var enemies = new Dictionary<int, Vector2>
            {
                [1] = Vector2.Zero, [2] = new Vector2(1.61f, 0), [3] = new Vector2(1.611f, 0), [4] = new Vector2(2.07f, 0),
            };
            var hits = new List<ExpandedDanielLionHit>();
            var ranks = new ExpandedDanielLionRankOptions(addPackRoar: true, widenRoar: true);
            Step(state, 0, enemies, hits, evolved: true, ranks: ranks);
            Step(state, 4, enemies, hits, evolved: true, ranks: ranks);
            var roars = hits.Where(hit => hit.Kind == ExpandedDanielLionHitKind.Roar).ToArray();
            Check.True(roars.Where(hit => hit.Damage < 15).Select(hit => hit.TargetId).SequenceEqual(new[] { 1, 2 }));
            Check.True(roars.Where(hit => hit.Damage > 15).Select(hit => hit.TargetId).SequenceEqual(new[] { 1, 2, 3, 4 }));
            Check.Near(1.61, ranks.RoarRadius(false));
            Check.Near(2.07, ranks.RoarRadius(true));
        });

        test("Lion c.03 adds .30 knockback to support and guardian roar hits while claws stay unchanged", () =>
        {
            var state = new ExpandedDanielLionState();
            var enemies = new Dictionary<int, Vector2> { [1] = Vector2.Zero };
            var hits = new List<ExpandedDanielLionHit>();
            var ranks = new ExpandedDanielLionRankOptions(addPackRoar: true, strengthenRoarKnockback: true);
            Step(state, 0, enemies, hits, evolved: true, ranks: ranks);
            Step(state, 4, enemies, hits, evolved: true, ranks: ranks);
            var roars = hits.Where(hit => hit.Kind == ExpandedDanielLionHitKind.Roar).ToArray();
            Check.Near(.30, roars.Single(hit => hit.Damage < 15).Knockback);
            Check.Near(.75, roars.Single(hit => hit.Damage > 15).Knockback);
            Check.True(hits.Where(hit => hit.Kind == ExpandedDanielLionHitKind.Claw).All(hit => hit.Knockback == 0));
        });

        test("Lion c.04 restores its two-second window and adds six points to the composed claw wait reduction", () =>
        {
            var state = new ExpandedDanielLionState();
            var enemies = new Dictionary<int, Vector2> { [1] = Vector2.Zero };
            var hits = new List<ExpandedDanielLionHit>();
            var ranks = new ExpandedDanielLionRankOptions(addPackRoar: true, hastenClawsAfterRoar: true);
            for (var time = 0; time <= 4; time++)
                Step(state, time, enemies, hits, ranks: ranks);
            Check.Near(5, state.Lions[0].NextClawAt);
            Check.Near(6, state.Lions[0].ClawHasteUntil!.Value);
            var restored = Restore(state);
            restored.Validate(4, false);
            Step(restored, 5, enemies, hits, ranks: ranks, afterClaw: () => 1.06);
            Check.Near(6, restored.Lions[0].NextClawAt);
            Step(restored, 6, enemies, hits, ranks: ranks, afterClaw: () => .8);
            Check.Near(6.8, restored.Lions[0].NextClawAt);
            Check.True(!restored.Lions[0].ClawHasteUntil.HasValue);

            var floored = Restore(state);
            Step(floored, 5, enemies, hits, ranks: ranks, afterClaw: () => .22);
            Check.Near(5.2, floored.Lions[0].NextClawAt);
        });

        test("Lion c.05 restores index-based roar deadlines and recurs without shifting their phase", () =>
        {
            var state = new ExpandedDanielLionState();
            var enemies = new Dictionary<int, Vector2> { [1] = Vector2.Zero };
            var hits = new List<ExpandedDanielLionHit>();
            var ranks = new ExpandedDanielLionRankOptions(addPackRoar: true, staggerPackRoar: true);
            Step(state, 0, enemies, hits, amount: 3, ranks: ranks);
            var loaded = Restore(state);
            loaded.Validate(0, false);
            Check.Near(4, loaded.Lions[0].NextPackRoarAt!.Value);
            Check.Near(4.18, loaded.Lions[1].NextPackRoarAt!.Value);
            Check.Near(4.36, loaded.Lions[2].NextPackRoarAt!.Value);
            foreach (var time in new[] { 4, 4.179, 4.18, 4.359, 4.36 })
            {
                var prior = hits.Count(hit => hit.Kind == ExpandedDanielLionHitKind.Roar);
                Step(loaded, time, enemies, hits, amount: 3, ranks: ranks);
                var count = hits.Count(hit => hit.Kind == ExpandedDanielLionHitKind.Roar);
                Check.Equal(time == 4.179 || time == 4.359 ? prior : prior + 1, count);
                loaded.Validate(time, false);
            }
            Check.True(hits.Where(hit => hit.Kind == ExpandedDanielLionHitKind.Roar)
                .Select(hit => hit.LionIndex).SequenceEqual(new[] { 0, 1, 2 }));
            Check.Near(8.18, loaded.Lions[1].NextPackRoarAt!.Value);
        });

        test("Lion roar common area adds four percentage points before the shared push without multiplying c.02", () =>
        {
            var ranks = ExpandedDanielLionRankOptions.FromRanks(new Dictionary<string, int>
            {
                ["common.area.01"] = 1, ["weapon.daniel_lion.c.01"] = 1, ["weapon.daniel_lion.c.02"] = 1,
            });
            Check.Near(1.666, ranks.RoarRadius(false));
            Check.Near(2.142, ranks.RoarRadius(true));
            var state = new ExpandedDanielLionState();
            var enemies = new Dictionary<int, Vector2>
            {
                [1] = Vector2.Zero, [2] = new Vector2(1.665f, 0), [3] = new Vector2(1.667f, 0),
            };
            var hits = new List<ExpandedDanielLionHit>();
            Step(state, 0, enemies, hits, ranks: ranks);
            Step(state, 4, enemies, hits, ranks: ranks);
            Check.True(hits.Where(hit => hit.Kind == ExpandedDanielLionHitKind.Roar)
                .Select(hit => hit.TargetId).SequenceEqual(new[] { 1, 2 }));
        });

        test("Lion e.02 uses only the last natural leap kill for a nonrecursive unvisited 35 percent follow-up", () =>
        {
            foreach (var (killFinal, canFollow, readyAt) in new[]
            {
                (true, true, (double?)null), (false, true, (double?)null), (true, false, (double?)null),
                (true, true, (double?)3), (true, true, (double?)3.001),
            })
            {
                var state = new ExpandedDanielLionState();
                var enemies = new Dictionary<int, Vector2>
                {
                    [1] = Vector2.Zero, [2] = new Vector2(2, 0), [3] = new Vector2(4, 0),
                };
                if (canFollow)
                {
                    enemies.Add(4, new Vector2(6, 0));
                    enemies.Add(5, new Vector2(6.3f, 0));
                }
                var hits = new List<ExpandedDanielLionHit>();
                for (var time = 0; time <= 2; time++)
                    Step(state, time, enemies, hits, evolved: true, ranks: Rank("e.02"));
                state.KillLeapReadyAt = readyAt;
                var loaded = Restore(state);
                loaded.Validate(2, true);
                Step(loaded, 2.5, enemies, hits, evolved: true, accept: hit => enemies.Remove(hit.TargetId));
                Check.Equal(0, hits.Count(hit => hit.Kind == ExpandedDanielLionHitKind.KillFollowupLeap));
                loaded = Restore(loaded);
                Step(loaded, 3, enemies, hits, evolved: true, accept: hit =>
                {
                    if (hit.Kind == ExpandedDanielLionHitKind.KillFollowupLeap || killFinal)
                        enemies.Remove(hit.TargetId);
                    return true;
                });
                Check.True(hits.Where(hit => hit.Kind == ExpandedDanielLionHitKind.Leap)
                    .Select(hit => hit.TargetId).SequenceEqual(new[] { 2, 3 }));
                var bonus = hits.Where(hit => hit.Kind == ExpandedDanielLionHitKind.KillFollowupLeap).ToArray();
                if (killFinal && canFollow && (!readyAt.HasValue || readyAt <= 3))
                {
                    Check.Equal(4, bonus.Single().TargetId);
                    Check.Near(17.5, bonus[0].Damage);
                    Check.True(enemies.ContainsKey(5));
                    var saved = Restore(loaded);
                    Check.Near(8, saved.KillLeapReadyAt!.Value);
                    saved.Validate(3, true);
                }
                else
                {
                    Check.Equal(0, bonus.Length);
                    Check.Equal(readyAt, loaded.KillLeapReadyAt);
                }
            }
        });

        test("Lion e.03 adds twelve radius points only in the evolved stance and extends actual claw contact", () =>
        {
            var enemies = new Dictionary<int, Vector2> { [1] = new Vector2(.728f, 0) };
            var hits = new List<ExpandedDanielLionHit>();
            var ranks = Rank("e.03");
            Check.Equal(0, Step(new ExpandedDanielLionState(), 0, enemies, hits, ranks: ranks));
            Check.Equal(0, Step(new ExpandedDanielLionState(), 0, enemies, hits, evolved: true));
            Check.Equal(1, Step(new ExpandedDanielLionState(), 0, enemies, hits, evolved: true, ranks: ranks));
            Check.Equal(ExpandedDanielLionHitKind.Claw, hits.Single().Kind);
            enemies[1] = new Vector2(.729f, 0);
            Check.Equal(0, Step(new ExpandedDanielLionState(), 0, enemies, hits, evolved: true, ranks: ranks));
        });

        test("Lion each claw starts an independent ordinal and leap or roar never carries original metadata", () =>
        {
            var state = new ExpandedDanielLionState();
            var targets = new Dictionary<int, Vector2> { [1] = Vector2.Zero, [2] = new Vector2(2, 0) };
            var hits = new List<ExpandedDanielLionHit>();
            Step(state, 0, targets, hits, amount: 2, level: 1);
            Check.True(hits.Select(hit => (hit.OriginalBaseDamage, hit.TargetOrdinal))
                .SequenceEqual(new[] { (20d, 1), (20d, 1) }));
            hits.Clear();
            Step(state, 1, targets, hits, amount: 2);
            Check.True(hits.Select(hit => (hit.OriginalBaseDamage, hit.TargetOrdinal))
                .SequenceEqual(new[] { (50d, 1), (50d, 1) }));

            hits.Clear();
            var evolved = new ExpandedDanielLionState();
            for (var time = 0; time <= 2; time++) Step(evolved, time, targets, hits, evolved: true);
            Step(evolved, 2.5, targets, hits, evolved: true);
            Step(evolved, 3, targets, hits, evolved: true);
            Step(evolved, 4, targets, hits, evolved: true);
            var support = hits.Where(hit => hit.Kind != ExpandedDanielLionHitKind.Claw).ToArray();
            Check.True(support.Any(hit => hit.Kind == ExpandedDanielLionHitKind.Leap));
            Check.True(support.Any(hit => hit.Kind == ExpandedDanielLionHitKind.Roar));
            Check.True(support.All(hit => hit.OriginalBaseDamage == 0 && hit.TargetOrdinal == 0));
        });
    }

    private static ExpandedDanielLionRankOptions Rank(string id) => ExpandedDanielLionRankOptions.FromRanks(
        new Dictionary<string, int> { ["weapon.daniel_lion." + id] = 1 });

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
        Func<ExpandedDanielLionHit, bool>? accept = null, ExpandedDanielLionRankOptions ranks = default,
        Func<int, bool>? isElite = null, Func<int, bool>? isBoss = null, Func<double>? afterClaw = null,
        int level = 8) =>
        ExpandedDanielLion.Step(state, time, player, level, evolved, damage: 50, cycle: 1, amount,
            radius: .65, speed: 5, () => enemies, hit =>
            {
                if (accept != null && !accept(hit)) return false;
                hits.Add(hit);
                return true;
            }, ranks, isElite, isBoss, afterClaw);
}
