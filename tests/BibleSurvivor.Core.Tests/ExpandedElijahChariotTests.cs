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
        test("Chariot shares projectile and support reservations without losing its original body", () =>
        {
            var state = new ExpandedElijahChariotState();
            var targets = new Dictionary<int, Vector2> { [1] = new Vector2(.2f, -.9f) };
            var originalSuppression = 0;
            var supportSuppression = 0;
            var hits = new List<ExpandedElijahChariotHit>();
            void Tick(ExpandedElijahChariotState current, double now) =>
                ExpandedElijahChariot.Step(current, now, Vector2.Zero, Vector2.UnitX, 8, true, 80, 1.9,
                    3, 1.2, 8, 10, () => targets, hit => { hits.Add(hit); return true; },
                    reserveOriginal: () => BattleWorld.CountOriginalProjectiles(191, null, null, current) < 192,
                    reserveTrail: () => false, recordOriginalSuppression: count => originalSuppression += count,
                    recordSupportSuppression: () => supportSuppression++);
            Tick(state, 0);
            Check.Equal(2, state.SuppressedOriginals);
            Check.Equal(2, originalSuppression);
            Check.Equal(1, supportSuppression);
            Check.Equal(1, state.Paths.Count);
            Check.True(state.Paths[0].SuppressedTrail);
            Check.Equal(0, BattleWorld.CountSupportRegions(null, null, null, state));
            state = Restore(state);
            state.Validate(0, true);
            Tick(state, .2);
            Check.Equal(ExpandedElijahChariotHitKind.Body, hits.Single().Kind);
            Tick(state, 1.4);
            Check.Equal(0, state.Paths.Count);
            state.Validate(1.4, true);
        });
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

        test("Trained chariot persists its shortened warning and reaches the first sweep exactly once", () =>
        {
            var state = new ExpandedElijahChariotState();
            var targets = new Dictionary<int, Vector2> { [1] = Vector2.Zero };
            var hits = new List<ExpandedElijahChariotHit>();
            Step(state, 0, targets, hits, delayAdvance: .08);
            Check.Near(0, state.Paths.Single().WarningAt);
            Check.Near(.07, state.Paths.Single().MoveAt);
            Step(state, .0699, targets, hits);
            Check.Equal(0, hits.Count);
            var restored = Restore(state);
            restored.Validate(.0699, false);
            Check.Near(0, restored.Paths.Single().WarningAt);
            Step(restored, .07, targets, hits);
            Check.Equal(1, hits.Count);
            Step(restored, .0701, targets, hits);
            Check.Equal(1, hits.Count);
            var capped = new ExpandedElijahChariotState();
            Step(capped, 0, targets, hits, delayAdvance: 1);
            Check.Near(.05, capped.Paths.Single().MoveAt);
        });

        test("Chariot a.01 restores the 11.2-unit warning and extends actual body collision to its inclusive endpoint", () =>
        {
            var state = new ExpandedElijahChariotState();
            var targets = new Dictionary<int, Vector2>
            {
                [1] = new Vector2(11.2f, 0), [2] = new Vector2(11.201f, 0),
            };
            var hits = new List<ExpandedElijahChariotHit>();
            Step(state, 0, targets, hits, ranks: Rank("a.01"));
            var restored = Restore(state);
            restored.Validate(0, false);
            Check.Near(11.2, restored.Paths.Single().Range);
            Step(restored, 1.549, targets, hits);
            Check.Equal(0, hits.Count);
            Step(restored, 1.55, targets, hits);
            Check.Equal(1, hits.Single().TargetId);
            Check.Equal(0, restored.Paths.Count);
        });

        test("Chariot b.01 widens parallel centers by .3 and lets the restored outer paths reach farther sideways", () =>
        {
            var state = new ExpandedElijahChariotState();
            var targets = new Dictionary<int, Vector2>
            {
                [1] = new Vector2(1.19f, .2f), [2] = new Vector2(-1.19f, .2f),
            };
            var hits = new List<ExpandedElijahChariotHit>();
            Step(state, 0, targets, hits, facing: Vector2.UnitY, amount: 2, ranks: Rank("b.01"));
            Check.True(state.Paths.Select(path => path.Origin).SequenceEqual(
                new[] { new Vector2(.6f, 0), new Vector2(-.6f, 0) }));
            var restored = Restore(state);
            Step(restored, .2, targets, hits, amount: 2);
            Check.True(hits.Select(hit => (hit.PathId, hit.TargetId)).SequenceEqual(new[] { (1L, 1), (2L, 2) }));
        });

        test("Chariot d.01 applies .30 knockback along the saved original travel direction only", () =>
        {
            var state = new ExpandedElijahChariotState();
            var targets = new Dictionary<int, Vector2> { [1] = Vector2.Zero, [2] = new Vector2(1, .2f) };
            var hits = new List<ExpandedElijahChariotHit>();
            Step(state, 0, targets, hits, evolved: true, facing: Vector2.UnitY, ranks: Rank("d.01"));
            var restored = Restore(state);
            Step(restored, .15, targets, hits, evolved: true, facing: -Vector2.UnitY);
            var body = hits.Single(hit => hit.Kind == ExpandedElijahChariotHitKind.Body);
            Check.Near(.3, body.Knockback);
            Check.Equal(Vector2.UnitY, body.Direction);
            Step(restored, 1.4, targets, hits, evolved: true);
            Check.True(hits.Any(hit => hit.Kind == ExpandedElijahChariotHitKind.Trail));
            Check.True(hits.Any(hit => hit.Kind == ExpandedElijahChariotHitKind.Whirlwind));
            Check.True(hits.Where(hit => hit.Kind != ExpandedElijahChariotHitKind.Body).All(hit => hit.Knockback == 0));
        });

        test("Chariot e.01 restores a 1.3225 whirlwind and keeps it distinct from the original body", () =>
        {
            var state = new ExpandedElijahChariotState();
            var targets = new Dictionary<int, Vector2>
            {
                [1] = new Vector2(2, 1.3225f), [2] = new Vector2(2, 1.323f), [3] = new Vector2(2, 0),
            };
            var hits = new List<ExpandedElijahChariotHit>();
            Step(state, 0, targets, hits, ranks: Rank("e.01"));
            Check.True(!state.Paths.Single().WiderWhirlwind);
            Step(state, 0, targets, hits, evolved: true, ranks: Rank("e.01"));
            var restored = Restore(state);
            restored.Validate(0, true);
            Check.Near(1.3225, restored.Paths.Single().WhirlwindRadius);
            Step(restored, 1.4, targets, hits, evolved: true);
            Check.Equal(3, hits.Single(hit => hit.Kind == ExpandedElijahChariotHitKind.Body).TargetId);
            Check.Equal(1, hits.Single(hit => hit.Kind == ExpandedElijahChariotHitKind.Whirlwind).TargetId);
        });

        test("Chariot e.02 preserves its extra flame pulse through restore and expires the last point after 1.8 seconds", () =>
        {
            var state = new ExpandedElijahChariotState();
            var targets = new Dictionary<int, Vector2> { [1] = Vector2.Zero };
            var hits = new List<ExpandedElijahChariotHit>();
            Step(state, 0, targets, hits, evolved: true, ranks: Rank("e.02"));
            for (var tick = 1; tick <= 98; tick++) Step(state, tick / 60d, targets, hits, evolved: true);
            var restored = Restore(state);
            Check.Near(1.8, restored.Paths.Single().TrailDuration);
            Check.Equal(3, hits.Count(hit => hit.Kind == ExpandedElijahChariotHitKind.Trail));
            Step(restored, 1.65, targets, hits, evolved: true);
            Check.Equal(4, hits.Count(hit => hit.Kind == ExpandedElijahChariotHitKind.Trail));
            Step(restored, 3.199, targets, hits, evolved: true);
            restored.Validate(3.199, true);
            Check.Equal(1, restored.Paths.Count);
            Step(restored, 3.2, targets, hits, evolved: true);
            Check.Equal(0, restored.Paths.Count);
            Check.Equal(4, hits.Count(hit => hit.Kind == ExpandedElijahChariotHitKind.Trail));

            var capped = new ExpandedElijahChariotState();
            ExpandedElijahChariot.Step(capped, 0, Vector2.Zero, Vector2.UnitX, 8, true, 80, 5,
                1, 1.2, 8, 10, () => targets, _ => true, reserveTrail: () => false, rankOptions: Rank("e.02"));
            Check.True(capped.Paths.Single().SuppressedTrail && !capped.Paths.Single().ExtendedTrail);
            capped.Validate(0, true);
        });

        test("Chariot a.02 aims its real body at the densest eligible center within eight", () =>
        {
            var targets = new Dictionary<int, Vector2>
            {
                [1] = Vector2.UnitX, [2] = new Vector2(0, 8), [3] = new Vector2(.8f, 8), [4] = new Vector2(0, 8.6f),
            };
            var hits = new List<ExpandedElijahChariotHit>();
            var state = new ExpandedElijahChariotState();
            Step(state, 0, targets, hits, ranks: Rank("a.02"));
            Check.Equal(Vector2.UnitY, state.Paths.Single().Direction);
            Step(state, 1.2, targets, hits);
            Check.Equal(2, hits.Single().TargetId);

            targets[2] = new Vector2(0, 8.001f);
            var noneWithin = new ExpandedElijahChariotState();
            Step(noneWithin, 0, targets, hits, ranks: Rank("a.02"));
            Check.Equal(Vector2.UnitX, noneWithin.Paths.Single().Direction);
        });

        test("Chariot a.03 orders boss then elite before crowds inside the inclusive eight-unit search", () =>
        {
            var targets = new Dictionary<int, Vector2>
            {
                [1] = new Vector2(2, 0), [2] = new Vector2(2.1f, 0), [3] = new Vector2(2.2f, 0),
                [11] = new Vector2(-8, 0), [10] = new Vector2(0, 4),
            };
            var ranks = ExpandedElijahChariotRankOptions.FromRanks(new Dictionary<string, int>
            {
                ["weapon.elijah_chariot.a.02"] = 1, ["weapon.elijah_chariot.a.03"] = 1,
            });
            Vector2 Aim()
            {
                var state = new ExpandedElijahChariotState();
                Step(state, 0, targets, new List<ExpandedElijahChariotHit>(), ranks: ranks,
                    isBoss: id => id == 11, isElite: id => id == 10 || id == 11);
                return state.Paths.Single().Direction;
            }
            Check.Equal(-Vector2.UnitX, Aim());
            targets[11] = new Vector2(-8.001f, 0);
            Check.Equal(Vector2.UnitY, Aim());
            targets[10] = new Vector2(0, 8.001f);
            Check.Equal(Vector2.UnitX, Aim());
        });

        test("Chariot b.02 freezes separate paths that converge on the sole boss and respects the travel cap", () =>
        {
            var boss = new Vector2(5, 3);
            var targets = new Dictionary<int, Vector2> { [1] = boss };
            var state = new ExpandedElijahChariotState();
            var ranks = Rank("b.02");
            var hits = new List<ExpandedElijahChariotHit>();
            Step(state, 0, targets, hits, amount: 3, ranks: ranks, isBoss: id => id == 1);
            Check.Equal(3, state.Paths.Select(path => path.Origin).Distinct().Count());
            var restored = Restore(state);
            restored.Validate(0, false);
            foreach (var path in restored.Paths)
                Check.Near(0, Vector2.Distance(boss, path.Origin + path.Direction * (float)path.Range), 2e-6);
            Step(restored, 1.2, targets, hits, amount: 3, facing: -Vector2.UnitX);
            Check.True(hits.Select(hit => hit.TargetId).SequenceEqual(new[] { 1, 1, 1 }));
            Check.Equal(0, restored.Paths.Count);

            targets[1] = new Vector2(13, 0);
            var far = new ExpandedElijahChariotState();
            Step(far, 0, targets, hits, amount: 3, ranks: ranks, isBoss: id => id == 1);
            Check.True(far.Paths.All(path => path.Range == 10));
            targets[2] = Vector2.UnitY;
            var two = new ExpandedElijahChariotState();
            Step(two, 0, targets, hits, amount: 3, ranks: ranks, isBoss: id => id == 1);
            Check.True(two.Paths.All(path => path.Range == 10 && path.Direction == Vector2.UnitX));

            targets = new Dictionary<int, Vector2> { [1] = Vector2.Zero };
            var coincident = new ExpandedElijahChariotState();
            hits.Clear();
            Step(coincident, 0, targets, hits, ranks: ranks, isBoss: id => id == 1);
            Step(coincident, .15, targets, hits);
            Check.Equal(1, hits.Single().TargetId);
        });

        test("Chariot b.03 restores each 0.12-second warning and movement offset with no early body hit", () =>
        {
            var targets = new Dictionary<int, Vector2>
            {
                [1] = new Vector2(0, -.9f), [2] = Vector2.Zero, [3] = new Vector2(0, .9f),
            };
            var state = new ExpandedElijahChariotState();
            var hits = new List<ExpandedElijahChariotHit>();
            Step(state, 0, targets, hits, amount: 3, ranks: Rank("b.03"));
            var loaded = Restore(state);
            loaded.Validate(0, false);
            foreach (var (path, index) in loaded.Paths.Select((path, index) => (path, index)))
            {
                Check.Equal(index, path.StaggerIndex);
                Check.Near(index * .12, path.WarningAt);
                Check.Near(index * .12 + .15, path.MoveAt);
            }
            Step(loaded, .149, targets, hits);
            Check.Equal(0, hits.Count);
            Step(loaded, .15, targets, hits);
            Check.True(hits.Select(hit => hit.TargetId).SequenceEqual(new[] { 1 }));
            Step(loaded, .269, targets, hits);
            Check.Equal(1, hits.Count);
            Step(loaded, .27, targets, hits);
            Check.True(hits.Select(hit => hit.TargetId).SequenceEqual(new[] { 1, 2 }));
            Step(loaded, .39, targets, hits);
            Check.True(hits.Select(hit => hit.TargetId).SequenceEqual(new[] { 1, 2, 3 }));
            loaded.Validate(.39, false);

            var corrupt = loaded.Copy();
            corrupt.Paths[2].StaggerIndex = 4;
            Check.Throws<ArgumentException>(() => corrupt.Validate(.39, false));
        });

        test("Chariot b.04 spends ten damage points only on the last planned original and caps on its level basis", () =>
        {
            var targets = new Dictionary<int, Vector2>
            {
                [1] = new Vector2(0, -.9f), [2] = Vector2.Zero, [3] = new Vector2(0, .9f),
            };
            var ranks = Rank("b.04");
            var hits = new List<ExpandedElijahChariotHit>();
            var state = new ExpandedElijahChariotState();
            Step(state, 0, targets, hits, amount: 3, damage: 80, level: 1, ranks: ranks);
            var loaded = Restore(state);
            Step(loaded, .15, targets, hits);
            Check.True(hits.Select(hit => hit.Damage).SequenceEqual(new[] { 80d, 80d, 84d }));

            var cap = new ExpandedElijahChariotState();
            Step(cap, 0, targets, hits, amount: 3, damage: 89, level: 1, ranks: ranks);
            Check.Near(90, cap.Paths.Last().Damage);

            var reserved = new ExpandedElijahChariotState();
            var seats = 2;
            Step(reserved, 0, targets, hits, amount: 3, damage: 80, level: 1, ranks: ranks,
                reserveOriginal: () => seats-- > 0);
            Check.Equal(1, reserved.SuppressedOriginals);
            Check.True(reserved.Paths.Select(path => path.Damage).SequenceEqual(new[] { 80d, 80d }));
        });
    }

    private static ExpandedElijahChariotRankOptions Rank(string id) => ExpandedElijahChariotRankOptions.FromRanks(
        new Dictionary<string, int> { ["weapon.elijah_chariot." + id] = 1 });

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
        double damage = 80, Vector2 player = default, Vector2? facing = null, double delayAdvance = 0,
        ExpandedElijahChariotRankOptions ranks = default, Func<int, bool>? isElite = null, Func<int, bool>? isBoss = null,
        int level = 8, Func<bool>? reserveOriginal = null)
    {
        ExpandedElijahChariot.Step(state, time, player, facing ?? Vector2.UnitX, level, evolved,
            damage, cycle, amount, width: 1.2, speed: 8, range: 10, () => targets,
            hit => { hits.Add(hit); return true; }, delayAdvance, reserveOriginal: reserveOriginal,
            rankOptions: ranks, isElite: isElite, isBoss: isBoss);
    }
}
