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

        test("Lightning aftershock freezes its additive common radius through restore", () =>
        {
            var state = new ExpandedLightningState();
            var targets = new Dictionary<int, Vector2> { [1] = Vector2.Zero };
            var hits = new List<ExpandedLightningHit>();
            var ranks = new Dictionary<string, int> { ["common.area.01"] = 1 };
            Check.Near(.04, ExpandedLightningSettings.FromRanks(ranks).AftershockRadiusBonus);
            ranks.Add("common.area.04", 1);
            var settings = ExpandedLightningSettings.FromRanks(ranks);
            Step(state, 0, targets, hits, evolved: true, settings: settings);
            Check.Near(1, state.Pending.Single().Radius);
            Step(state, .15, targets, hits, evolved: true, settings: settings);
            Check.Near(1.526, state.Pending.Single().Radius);

            using var save = new MemoryStream();
            var serializer = new DataContractJsonSerializer(typeof(ExpandedLightningState));
            serializer.WriteObject(save, state);
            save.Position = 0;
            var restored = (ExpandedLightningState)serializer.ReadObject(save)!;
            restored.Validate(.15, true);
            targets.Clear();
            targets[2] = new Vector2(1.52f, 0);
            targets[3] = new Vector2(1.53f, 0);
            hits.Clear();
            Step(restored, .65, targets, hits, evolved: true);
            Check.Equal((ExpandedLightningHitKind.Aftershock, 2), (hits.Single().Kind, hits.Single().TargetId));
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

        test("Lightning rank radius adds from level one and delay retains the .05 floor", () =>
        {
            var state = new ExpandedLightningState();
            var targets = new Dictionary<int, Vector2>
            {
                [1] = Vector2.Zero, [4] = new Vector2(1.07f, 0), [6] = new Vector2(1.09f, 0),
            };
            var hits = new List<ExpandedLightningHit>();
            var settings = ExpandedLightningSettings.FromRanks(Ranks("base.04", "base.05"));
            Step(state, 0, targets, hits, settings: settings);
            Check.Near(.07, state.Pending.Single().At);
            Check.Near(1.08, state.Pending.Single().Radius);
            Step(state, .0699, targets, hits, settings: settings);
            Check.Equal(0, hits.Count);
            Step(state, .07, targets, hits, settings: settings);
            Check.True(hits.Select(hit => hit.TargetId).SequenceEqual(new[] { 1, 4 }));
            state.Validate(.07, false);

            var overCap = new ExpandedLightningState();
            Step(overCap, 0, targets, hits, settings: new ExpandedLightningSettings(radiusBonus: 3, delayAdvance: 1));
            Check.Near(.05, overCap.Pending.Single().At);
            Check.Near(2, overCap.Pending.Single().Radius);
        });

        test("Lightning rank targeting puts boss and elite before crowd, and crowd before distance", () =>
        {
            var targets = new Dictionary<int, Vector2>
            {
                [1] = new Vector2(1, 0), [4] = new Vector2(4.5f, 0), [5] = new Vector2(3, 0),
                [6] = new Vector2(6, 0), [10] = new Vector2(0, 7.4f), [12] = new Vector2(0, -7.9f),
            };
            var hits = new List<ExpandedLightningHit>();
            var normal = new ExpandedLightningState();
            Step(normal, 0, targets, hits, amount: 3);
            Check.Equal(1, normal.Pending[0].AssignedTargetId);
            var crowd = new ExpandedLightningState();
            var crowdSettings = ExpandedLightningSettings.FromRanks(Ranks("a.01"));
            Step(crowd, 0, targets, hits, amount: 3, settings: crowdSettings);
            Check.Equal(4, crowd.Pending[0].AssignedTargetId);

            var threats = new ExpandedLightningState();
            var threatSettings = ExpandedLightningSettings.FromRanks(Ranks("a.01", "a.02"),
                isBoss: id => id == 12, isElite: id => id == 10);
            Step(threats, 0, targets, hits, amount: 3, settings: threatSettings);
            Check.True(threats.Pending.Select(attack => attack.AssignedTargetId).SequenceEqual(new[] { 12, 10, 4 }));
        });

        test("Lightning rank retarget saves its sole attempt without moving the impact time", () =>
        {
            var state = new ExpandedLightningState();
            var targets = new Dictionary<int, Vector2>
            {
                [1] = new Vector2(1, 0), [2] = new Vector2(4, 0), [3] = new Vector2(6, 0),
            };
            var hits = new List<ExpandedLightningHit>();
            var settings = ExpandedLightningSettings.FromRanks(Ranks("a.04"));
            Step(state, 0, targets, hits, settings: settings);
            Check.True(state.Pending.Single().CanRetarget);
            targets.Remove(1);
            Step(state, .05, targets, hits, settings: settings);
            var strike = state.Pending.Single();
            Check.Equal(2, strike.AssignedTargetId);
            Check.Near(4, strike.X);
            Check.Near(.15, strike.At);
            Check.True(!strike.CanRetarget);

            using var save = new MemoryStream();
            var serializer = new DataContractJsonSerializer(typeof(ExpandedLightningState));
            serializer.WriteObject(save, state);
            save.Position = 0;
            state = (ExpandedLightningState)serializer.ReadObject(save)!;
            state.Validate(.05, false);
            targets.Remove(2);
            Step(state, .1, targets, hits, settings: settings);
            Check.Equal(2, state.Pending.Single().AssignedTargetId);
            targets[5] = new Vector2(4, 0);
            Step(state, .15, targets, hits, settings: settings);
            Check.Equal(5, hits.Single().TargetId);
        });

        test("Lightning reuse preference reserves an unassigned replacement when that bolt can retarget", () =>
        {
            foreach (var reserveFreshTarget in new[] { false, true })
            {
                var state = new ExpandedLightningState();
                var targets = new Dictionary<int, Vector2>
                {
                    [1] = new Vector2(1, 0), [2] = new Vector2(2, 0), [3] = new Vector2(3, 0),
                };
                var hits = new List<ExpandedLightningHit>();
                var settings = ExpandedLightningSettings.FromRanks(
                    reserveFreshTarget ? Ranks("a.03", "a.04") : Ranks("a.04"));
                Step(state, 0, targets, hits, amount: 2, settings: settings);
                Check.True(state.Pending.Select(attack => attack.AssignedTargetId).SequenceEqual(new[] { 1, 2 }));
                targets.Remove(1);
                Step(state, .05, targets, hits, amount: 2, settings: settings);
                Check.Equal(reserveFreshTarget ? 3 : 2, state.Pending[0].AssignedTargetId);
                Check.Equal(2, state.Pending[1].AssignedTargetId);
                Check.Equal(reserveFreshTarget ? 3 : 0, state.Pending[1].PredecessorTargetId);
                state.Validate(.05, false);
            }
        });

        test("Lightning reuses the farthest actor only after the unique targets and retains priority for ties", () =>
        {
            var settings = ExpandedLightningSettings.FromRanks(Ranks("a.03"));
            var hits = new List<ExpandedLightningHit>();
            var targets = new Dictionary<int, Vector2>
            {
                [1] = new Vector2(.5f, 0), [2] = new Vector2(3, 0), [3] = new Vector2(-4, 0),
            };
            int[] Assign(ExpandedLightningSettings ranks)
            {
                var state = new ExpandedLightningState();
                Step(state, 0, targets, hits, amount: 4, settings: ranks);
                state.Validate(0, false);
                return state.Pending.Select(attack => attack.AssignedTargetId).ToArray();
            }
            Check.True(Assign(default).SequenceEqual(new[] { 1, 2, 3, 1 }));
            Check.True(Assign(settings).SequenceEqual(new[] { 1, 2, 3, 2 }));

            targets[1] = new Vector2(1, 0);
            targets[2] = new Vector2(-1, 0);
            targets[3] = new Vector2(0, 4);
            Check.True(Assign(settings).SequenceEqual(new[] { 1, 2, 3, 1 }));
            targets.Remove(3);
            targets[2] = targets[1];
            Check.True(Assign(settings).SequenceEqual(new[] { 1, 2, 1, 2 }));
            targets.Remove(2);
            Check.True(Assign(settings).SequenceEqual(new[] { 1, 1, 1, 1 }));
        });

        test("Lightning restored retarget uses the live predecessor or current priority when there is none", () =>
        {
            var settings = ExpandedLightningSettings.FromRanks(Ranks("a.03", "a.04"));
            var targets = RetargetTargets();
            var hits = new List<ExpandedLightningHit>();
            var before = new ExpandedLightningState();
            Step(before, 0, targets, hits, amount: 4, settings: settings);
            var state = Restore(before);
            Check.Equal(2, state.Pending[2].PredecessorTargetId);
            Check.Near(2, state.Pending[2].PredecessorX!.Value);
            targets[2] = new Vector2(7, 0);
            targets.Remove(3);
            Step(state, .05, targets, hits, amount: 4, settings: settings);
            Check.Equal(1, state.Pending[2].AssignedTargetId);
            Check.Equal(1, state.Pending[3].PredecessorTargetId);
            Check.Near(-1, state.Pending[3].PredecessorX!.Value);
            Check.Near(.15, state.Pending[2].At);
            Check.Near(40, state.Pending[2].Damage);
            Check.Near(1, state.Pending[2].Radius);
            state.Validate(.05, false);
            var corrupt = state.Copy();
            corrupt.Pending[3].PredecessorY = null;
            Check.Throws<ArgumentException>(() => corrupt.Validate(.05, false));

            targets = RetargetTargets();
            state = Restore(before);
            targets.Remove(1);
            Step(state, .05, targets, hits, amount: 4, settings: settings);
            Check.Equal(0, state.Pending[0].PredecessorTargetId);
            Check.Equal(2, state.Pending[0].AssignedTargetId);
        });

        test("Lightning same-step retarget uses the saved anchor and counts earlier strikes as already assigned", () =>
        {
            var settings = ExpandedLightningSettings.FromRanks(Ranks("a.03", "a.04"));
            foreach (var moveSurvivor in new[] { false, true })
            {
                var targets = RetargetTargets();
                var hits = new List<ExpandedLightningHit>();
                var state = new ExpandedLightningState();
                Step(state, 0, targets, hits, amount: 4, settings: settings);
                state = Restore(state);
                Step(state, .149, targets, hits, amount: 4, settings: settings);
                Check.Equal(0, hits.Count);
                Step(state, .15, targets, hits, amount: 4, settings: settings, accept: hit =>
                {
                    if (hit.StrikeId == 1) targets.Remove(3);
                    if (hit.StrikeId == 2)
                    {
                        targets.Remove(2);
                        if (moveSurvivor) targets[1] = new Vector2(-5, 0);
                    }
                    return true;
                });
                var reassigned = hits.Single(hit => hit.StrikeId == 3);
                Check.Equal(moveSurvivor ? 1 : 4, reassigned.TargetId);
                Check.Near(40, reassigned.Damage);
                Check.Equal(0, state.Pending.Count);
                state.Validate(.15, false);
            }

            var evolved = new ExpandedLightningState();
            var fixedTargets = RetargetTargets();
            var evolvedHits = new List<ExpandedLightningHit>();
            Step(evolved, 0, fixedTargets, evolvedHits, evolved: true, amount: 4, settings: settings);
            Step(evolved, .15, fixedTargets, evolvedHits, evolved: true, amount: 4, settings: settings);
            var aftershock = Restore(evolved).Pending.Single();
            Check.True(aftershock.Aftershock && !aftershock.UsesFarthestReuse);
            Check.Equal(0, aftershock.PredecessorTargetId);
        });

        test("Lightning evolution rank adds a fourth accepted chain transfer only to its evolved form", () =>
        {
            var targets = new Dictionary<int, Vector2>();
            for (var index = 0; index < 6; index++) targets.Add(index + 1, new Vector2(index * 2.5f, 0));
            var hits = new List<ExpandedLightningHit>();
            var settings = ExpandedLightningSettings.FromRanks(Ranks("e.01"));
            var untrained = new ExpandedLightningState();
            Step(untrained, 0, targets, hits, evolved: true);
            Step(untrained, .15, targets, hits, evolved: true);
            Check.Equal(3, hits.Count(hit => hit.Kind == ExpandedLightningHitKind.Chain));

            hits.Clear();
            var evolved = new ExpandedLightningState();
            Step(evolved, 0, targets, hits, evolved: true, settings: settings);
            Step(evolved, .15, targets, hits, evolved: true, settings: settings);
            Check.True(hits.Where(hit => hit.Kind == ExpandedLightningHitKind.Chain).Select(hit => hit.TargetId)
                .SequenceEqual(new[] { 2, 3, 4, 5 }));
            Check.Equal(5, evolved.Pending.Single().VisitedIds!.Count);
            evolved.Validate(.15, true);

            hits.Clear();
            var basic = new ExpandedLightningState();
            Step(basic, 0, targets, hits, settings: settings);
            Step(basic, .15, targets, hits, settings: settings);
            Check.Equal(ExpandedLightningHitKind.Strike, hits.Single().Kind);
            Check.Equal(0, basic.Pending.Count);
        });
    }

    private static Dictionary<int, Vector2> RetargetTargets() => new()
    {
        [1] = new Vector2(-1, 0), [2] = new Vector2(2, 0),
        [3] = new Vector2(4, 0), [4] = new Vector2(6, 0),
    };

    private static ExpandedLightningState Restore(ExpandedLightningState state)
    {
        using var save = new MemoryStream();
        var serializer = new DataContractJsonSerializer(typeof(ExpandedLightningState));
        serializer.WriteObject(save, state);
        save.Position = 0;
        return (ExpandedLightningState)serializer.ReadObject(save)!;
    }

    private static void Step(ExpandedLightningState state, double time, Dictionary<int, Vector2> targets,
        List<ExpandedLightningHit> hits, bool evolved = false, int amount = 1, double cycle = 3,
        Func<ExpandedLightningHit, bool>? accept = null, ExpandedLightningSettings settings = default)
    {
        ExpandedLightning.Step(state, time, Vector2.Zero, 8, evolved, 40, cycle, amount, 1,
            () => targets, hit =>
            {
                if (accept != null && !accept(hit)) return false;
                hits.Add(hit);
                return true;
            }, settings);
    }

    private static Dictionary<string, int> Ranks(params string[] suffixes)
    {
        var ranks = new Dictionary<string, int>();
        foreach (var suffix in suffixes) ranks.Add("weapon.lightning." + suffix, 1);
        return ranks;
    }
}
