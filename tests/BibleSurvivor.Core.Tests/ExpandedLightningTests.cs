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
        test("Lightning landing observer receives one detached copy of a final-tick retarget or empty impact", () =>
        {
            var ranks = ExpandedLightningSettings.FromRanks(Ranks("a.04"));
            var targets = new Dictionary<int, Vector2> { [1] = Vector2.UnitX };
            var hits = new List<ExpandedLightningHit>();
            var observed = new List<(long Id, int Target, Vector2 Position, double Time)>();
            var state = new ExpandedLightningState();
            void Tick(double now) => ExpandedLightning.Step(state, now, Vector2.Zero, 8, false, 40, 3,
                1, 1, () => targets, hit => { hits.Add(hit); return true; }, ranks, emitVisualLanding: attack =>
                {
                    Check.True(state.Pending.All(pending => pending.Id != attack.Id));
                    observed.Add((attack.Id, attack.AssignedTargetId, new Vector2(attack.X, attack.Y), attack.At));
                    attack.X = 500;
                    attack.AssignedTargetId = 500;
                });
            Tick(0);
            Check.Equal(0, observed.Count);
            state = Restore(state);
            targets.Clear();
            targets.Add(2, new Vector2(3, 2));
            Tick(.15);
            Check.Equal((1L, 2, new Vector2(3, 2), .15), observed.Single());
            Check.Equal(2, hits.Single().TargetId);

            Tick(3);
            targets.Clear();
            Tick(3.15);
            Check.Equal((2L, 2, new Vector2(3, 2), 3.15), observed[1]);
            Check.Equal(1, hits.Count);
        });

        test("Lightning evolved fork uses the first base transfer, excludes original and chain accepts, and reopens its cooldown", () =>
        {
            var nodes = Ranks("e.01", "e.02", "e.03");
            nodes.Add("common.area.09", 1);
            var ranks = ExpandedLightningSettings.FromRanks(nodes);
            var targets = new Dictionary<int, Vector2>
            {
                [1] = Vector2.UnitX, [2] = new Vector2(2.1f, 0), [3] = new Vector2(2.4f, 0),
                [4] = new Vector2(2.7f, 0), [5] = new Vector2(3.1f, 0),
                [8] = new Vector2(0, 1.2f), [9] = new Vector2(0, -1.2f),
            };
            var state = new ExpandedLightningState { Evolved = true };
            var hits = new List<ExpandedLightningHit>();
            void Tick(double now, ExpandedLightningSettings settings) =>
                ExpandedLightning.Step(state, now, Vector2.Zero, 8, true, 40, .5, 1, 1, () => targets,
                    hit => { hits.Add(hit); return true; }, settings, applyOriginalHit: _ => 50);

            Tick(0, ranks);
            Check.Equal((byte)3, state.Pending.Single().EvolutionRanks);
            state = Restore(state);
            Tick(.15, ranks);
            var fork = hits.Single(hit => hit.Kind == ExpandedLightningHitKind.EvolutionFork);
            Check.Equal(8, fork.TargetId);
            Check.Near(15, fork.Damage);
            Check.Equal(0, fork.TargetOrdinal);
            Check.True(hits.Where(hit => hit.Kind == ExpandedLightningHitKind.Chain).Select(hit => hit.TargetId)
                .SequenceEqual(new[] { 2, 3, 4, 5 }));
            Check.Near(1.694, state.Pending.Single(attack => attack.Aftershock).Radius);
            Check.Near(1.15, state.NextEvolutionForkAt!.Value);
            state = Restore(state);
            state.Validate(.15, true);

            // Keep the same original center. Only the unhit fork candidate could be selected from the first hop.
            targets[8] = new Vector2(0, 2);
            targets[9] = new Vector2(0, -1.2f);
            hits.Clear();
            Tick(.5, ranks);
            Tick(.65, ranks);
            Check.True(hits.All(hit => hit.Kind != ExpandedLightningHitKind.EvolutionFork));
            Tick(1, ranks);
            Tick(1.15, ranks);
            Check.Equal(9, hits.Single(hit => hit.Kind == ExpandedLightningHitKind.EvolutionFork).TargetId);
        });

        test("Lightning accepted elite transition marks the next weapon original for five seconds and gates refresh for four", () =>
        {
            var ranks = ExpandedLightningSettings.FromRanks(Ranks("e.04"), isElite: id => id == 2);
            var targets = new Dictionary<int, Vector2> { [1] = Vector2.UnitX, [2] = new Vector2(2.4f, 0) };
            var state = new ExpandedLightningState { Evolved = true };
            var hits = new List<ExpandedLightningHit>();
            void Tick(double now, Func<ExpandedLightningHit, bool>? accept = null) =>
                Step(state, now, targets, hits, evolved: true, settings: ranks, accept: accept);
            Tick(0);
            Tick(.15);
            Check.Near(4.15, state.NextEliteChainMarkAt!.Value);
            Check.Near(5.15, state.EliteChainDamageUntil!.Value);
            state.Validate(.15, true);
            state = Restore(state);
            var expires = Restore(state);
            hits.Clear();
            Tick(3);
            Tick(3.15);
            Check.Near(44, hits.Single(hit => hit.Kind == ExpandedLightningHitKind.Strike).Damage);
            Check.True(state.EliteChainDamageUntil == null);
            Check.Near(4.15, state.NextEliteChainMarkAt!.Value);

            state = Restore(expires);
            targets.Clear();
            Tick(3);
            Check.True(state.WaitingForTarget);
            targets.Add(9, Vector2.UnitX);
            hits.Clear();
            Tick(5);
            state = Restore(state);
            Tick(5.15);
            Check.Near(44, hits.Single(hit => hit.Kind == ExpandedLightningHitKind.Strike).Damage);
            Check.True(state.EliteChainDamageUntil == null);

            state = Restore(expires);
            targets.Clear();
            Tick(5.151);
            Check.True(state.EliteChainDamageUntil == null);

            state = new ExpandedLightningState { Evolved = true };
            targets[1] = Vector2.UnitX;
            targets[2] = new Vector2(2.4f, 0);
            Tick(0);
            Tick(.15, hit => hit.TargetId != 2);
            Check.True(state.EliteChainDamageUntil == null);
        });

        test("Lightning evolved fifth-cycle storm targets crowds on only the first bolt while preserving a lone boss", () =>
        {
            var ranks = ExpandedLightningSettings.FromRanks(Ranks("a.04", "e.05"), isBoss: id => id == 9);
            var targets = new Dictionary<int, Vector2>
            {
                [1] = Vector2.UnitX, [2] = new Vector2(4, 0), [3] = new Vector2(4.3f, 0),
                [4] = new Vector2(4.5f, 0),
            };
            var state = new ExpandedLightningState { Evolved = true, Cycle = 4, NextStrikeId = 5 };
            var hits = new List<ExpandedLightningHit>();
            Step(state, 0, targets, hits, evolved: true, amount: 2, settings: ranks);
            Check.True(state.Pending.Select(hit => hit.AssignedTargetId).SequenceEqual(new[] { 2, 1 }));
            Check.True(state.Pending.Select(hit => hit.EvolutionRanks).SequenceEqual(new byte[] { 8, 0 }));
            state = Restore(state);
            state.Validate(0, true);
            Step(state, .15, targets, hits, evolved: true, amount: 2, settings: ranks);
            Check.True(hits.Where(hit => hit.Kind == ExpandedLightningHitKind.Strike && hit.StrikeId == 5)
                .All(hit => Math.Abs(hit.Damage - 44.8) < 1e-9));
            Check.Near(40, hits.Single(hit => hit.Kind == ExpandedLightningHitKind.Strike && hit.StrikeId == 6).Damage);

            targets.Add(9, new Vector2(7, 0));
            state = new ExpandedLightningState { Evolved = true, Cycle = 4, NextStrikeId = 5 };
            Step(state, 0, targets, hits, evolved: true, amount: 2, settings: ranks);
            Check.True(state.Pending.Select(hit => hit.AssignedTargetId).SequenceEqual(new[] { 9, 1 }));
            targets.Remove(9);
            state = Restore(state);
            Step(state, .05, targets, hits, evolved: true, amount: 2, settings: ranks);
            Check.True(state.Pending.Select(hit => hit.AssignedTargetId).SequenceEqual(new[] { 2, 1 }));
            Check.Equal((byte)8, state.Pending[0].EvolutionRanks);

            state = new ExpandedLightningState { Cycle = 4, NextStrikeId = 5 };
            Step(state, 0, targets, hits, amount: 2, settings: ranks);
            Check.True(state.Pending.Select(hit => hit.AssignedTargetId).SequenceEqual(new[] { 1, 2 }));
            Check.True(state.Pending.All(hit => hit.EvolutionRanks == 0));
        });

        test("Lightning ground current persists its impact geometry, pulse phase, and exclusive lifetime", () =>
        {
            var nodes = Ranks("d.01", "d.02", "d.03");
            nodes.Add("common.area.01", 1);
            nodes.Add("common.area.04", 1);
            nodes.Add("common.area.09", 1);
            nodes.Add("common.duration.02", 1);
            var settings = ExpandedLightningSettings.FromRanks(nodes);
            var targets = new Dictionary<int, Vector2> { [1] = Vector2.UnitX, [2] = new Vector2(1, .95f) };
            var hits = new List<ExpandedLightningHit>();
            var state = new ExpandedLightningState();
            void Tick(double now, ExpandedLightningSettings ranks = default, double conditional = 0) =>
                ExpandedLightning.Step(state, now, Vector2.Zero, 8, false, 40, 3, 1, 1, () => targets,
                    hit => { hits.Add(hit); return true; }, ranks, groundShapeBonus: conditional);

            Tick(0, settings);
            Check.Equal((byte)7, state.Pending.Single().GroundRanks);
            state = Restore(state);
            Tick(.15, settings, .06);
            var ground = state.GroundCurrents!.Single();
            Check.Near(.952, ground.Radius);
            Check.Near(.15, ground.StartedAt);
            Check.Near(2.025, ground.EndsAt);
            Check.Near(.65, ground.NextPulse);
            Check.Near(10, ground.Damage);
            Check.True(hits.Where(hit => hit.Kind == ExpandedLightningHitKind.GroundCurrent)
                .Select(hit => hit.TargetId).SequenceEqual(new[] { 1, 2 }));
            state = Restore(state);
            state.Validate(.15, false);
            hits.Clear();
            Tick(.649);
            Check.Equal(0, hits.Count);
            Tick(.65);
            Tick(1.15);
            Tick(1.65);
            Check.Equal(6, hits.Count);
            Check.True(hits.All(hit => hit.Kind == ExpandedLightningHitKind.GroundCurrent &&
                hit.TargetOrdinal == 0 && hit.OriginalBaseDamage == 0));
            Tick(2.025);
            Check.Equal(6, hits.Count);
            Check.True(state.GroundCurrents == null);
            Check.Equal(0, state.ActiveSupportGroups);

            state = new ExpandedLightningState();
            hits.Clear();
            var baseRanks = ExpandedLightningSettings.FromRanks(Ranks("d.01"));
            Tick(0, baseRanks);
            Tick(.15, baseRanks);
            Tick(.65);
            Tick(1.15);
            Check.Equal(3, hits.Count(hit => hit.Kind == ExpandedLightningHitKind.GroundCurrent));
            Tick(1.65);
            Check.Equal(3, hits.Count(hit => hit.Kind == ExpandedLightningHitKind.GroundCurrent));
            Check.True(state.GroundCurrents == null);
        });

        test("Lightning overlapping ground pulses choose the larger parent amount and then the lower original ID", () =>
        {
            var settings = ExpandedLightningSettings.FromRanks(Ranks("d.01"));
            var targets = new Dictionary<int, Vector2> { [1] = Vector2.UnitX };
            var hits = new List<ExpandedLightningHit>();
            foreach (var tie in new[] { false, true })
            {
                var state = new ExpandedLightningState();
                void Tick(double now, double cycle) => ExpandedLightning.Step(state, now, Vector2.Zero, 8, false,
                    40, cycle, 1, 1, () => targets, hit => { hits.Add(hit); return true; }, settings,
                    applyOriginalHit: hit => hit.StrikeId == 1 || tie ? 40 : 60);
                Tick(0, .5);
                Tick(.15, .5);
                Tick(.5, 3);
                state = Restore(state);
                hits.Clear();
                Tick(.65, 3);
                state.Validate(.65, false);
                Check.Equal(2, state.ActiveSupportGroups);
                var pulse = hits.Single();
                Check.Equal(ExpandedLightningHitKind.GroundCurrent, pulse.Kind);
                Check.Equal(tie ? 1L : 2L, pulse.StrikeId);
                Check.Near(tie ? 10 : 15, pulse.Damage);
            }
        });

        test("Lightning first ground link accepts distance 2.5 and width .5 across restore", () =>
        {
            var settings = ExpandedLightningSettings.FromRanks(Ranks("d.01", "d.02", "d.03", "d.04"));
            var targets = new Dictionary<int, Vector2>
            {
                [1] = Vector2.UnitX, [2] = new Vector2(3.5f, 0), [3] = new Vector2(2.25f, .25f),
                [4] = new Vector2(2.25f, .2501f),
            };
            var hits = new List<ExpandedLightningHit>();
            var state = new ExpandedLightningState();
            void Tick(double now, Vector2 player, double cycle = .5) => ExpandedLightning.Step(state, now, player,
                8, false, 40, cycle, 1, 1, () => targets, hit => { hits.Add(hit); return true; }, settings);

            Tick(0, Vector2.Zero);
            Tick(.15, Vector2.Zero);
            Tick(.5, targets[2]);
            state = Restore(state);
            hits.Clear();
            Tick(.65, targets[2]);
            var links = hits.Where(hit => hit.Kind == ExpandedLightningHitKind.GroundLink).ToArray();
            Check.True(links.Select(hit => hit.TargetId).SequenceEqual(new[] { 1, 2, 3 }));
            Check.True(links.All(hit => hit.Damage == 12 && hit.TargetOrdinal == 0 && hit.OriginalBaseDamage == 0));
            Check.Near(3.65, state.NextGroundLinkAt!.Value);
            Check.Equal(2, state.GroundCurrents!.Count);
            state = Restore(state);
            state.Validate(.65, false);
            hits.Clear();
            Tick(1, targets[3]);
            Tick(1.15, targets[3]);
            Check.True(hits.All(hit => hit.Kind != ExpandedLightningHitKind.GroundLink));

            state = new ExpandedLightningState();
            targets[2] = new Vector2(3.5001f, 0);
            hits.Clear();
            Tick(0, Vector2.Zero);
            Tick(.15, Vector2.Zero);
            Tick(.5, targets[2]);
            Tick(.65, targets[2]);
            Check.True(hits.All(hit => hit.Kind != ExpandedLightningHitKind.GroundLink));
        });

        test("Lightning ground burst uses the eighth actual bolt and persists its independent gate", () =>
        {
            var settings = ExpandedLightningSettings.FromRanks(Ranks("d.01", "d.05"));
            var targets = new Dictionary<int, Vector2> { [1] = Vector2.UnitX, [2] = new Vector2(1, 1.59f), [3] = new Vector2(1, 1.61f) };
            var hits = new List<ExpandedLightningHit>();
            var state = new ExpandedLightningState { Cycle = 7, NextStrikeId = 8 };
            void Tick(double now) => ExpandedLightning.Step(state, now, Vector2.Zero, 8, false, 40, 3,
                1, 1, () => targets, hit => { hits.Add(hit); return true; }, settings, applyOriginalHit: _ => 50);

            Tick(0);
            state = Restore(state);
            Tick(.15);
            var bursts = hits.Where(hit => hit.Kind == ExpandedLightningHitKind.GroundBurst).ToArray();
            Check.True(bursts.Select(hit => hit.TargetId).SequenceEqual(new[] { 1, 2 }));
            Check.True(bursts.All(hit => hit.StrikeId == 8 && hit.Damage == 17.5 && hit.TargetOrdinal == 0));
            Check.Near(3.15, state.NextGroundBurstAt!.Value);
            Check.Equal(12.5, hits.Single(hit => hit.Kind == ExpandedLightningHitKind.GroundCurrent).Damage);
            state = Restore(state);
            state.Validate(.15, false);

            state = new ExpandedLightningState { Cycle = 15, NextStrikeId = 16, NextGroundBurstAt = 2 };
            hits.Clear();
            Tick(0);
            Tick(.15);
            Check.True(hits.All(hit => hit.Kind != ExpandedLightningHitKind.GroundBurst));
            Check.Near(2, state.NextGroundBurstAt!.Value);

            state = new ExpandedLightningState { Cycle = 6, NextStrikeId = 7 };
            hits.Clear();
            Tick(0);
            Tick(.15);
            Check.True(hits.All(hit => hit.Kind != ExpandedLightningHitKind.GroundBurst));
        });

        test("Lightning ground budgets four occupied groups and suppresses new support without replacing old regions", () =>
        {
            var settings = ExpandedLightningSettings.FromRanks(Ranks("d.01", "d.02", "d.03", "d.04", "d.05"));
            var targets = new Dictionary<int, Vector2> { [1] = Vector2.UnitX };
            var hits = new List<ExpandedLightningHit>();
            var state = new ExpandedLightningState { Cycle = 4, NextStrikeId = 5 };
            var suppressed = 0;
            var allowRegion = true;
            void Tick(double now, int amount = 4) => ExpandedLightning.Step(state, now, Vector2.Zero, 8, false, 40, .5,
                amount, 1, () => targets, hit => { hits.Add(hit); return true; }, settings,
                reserveSupportRegion: () => allowRegion, recordSupportSuppression: () => suppressed++);
            Tick(0);
            Tick(.15);
            Check.Equal(4, state.ActiveSupportGroups);
            Check.Equal(1, suppressed); // Eighth strike burst cannot occupy a fifth group.
            Check.Equal(4, hits.Count(hit => hit.Kind == ExpandedLightningHitKind.Strike));
            Check.Equal(1, hits.Count(hit => hit.Kind == ExpandedLightningHitKind.GroundCurrent));
            Check.Equal(0, hits.Count(hit => hit.Kind == ExpandedLightningHitKind.GroundBurst));
            Check.True(state.GroundCurrents!.Select(region => region.Id).SequenceEqual(new[] { 5L, 6L, 7L, 8L }));
            state = Restore(state);
            state.Validate(.15, false);
            Tick(.5, 1);
            hits.Clear();
            Tick(.65, 1);
            Check.Equal(2, suppressed);
            Check.Equal(1, hits.Count(hit => hit.Kind == ExpandedLightningHitKind.Strike));
            Check.Equal(1, hits.Count(hit => hit.Kind == ExpandedLightningHitKind.GroundCurrent));
            Check.True(state.GroundCurrents!.Select(region => region.Id).SequenceEqual(new[] { 5L, 6L, 7L, 8L }));

            state = new ExpandedLightningState();
            hits.Clear();
            allowRegion = false;
            Tick(0, 1);
            Tick(.15, 1);
            Check.Equal(3, suppressed);
            Check.Equal(0, state.ActiveSupportGroups);
            Check.Equal(ExpandedLightningHitKind.Strike, hits.Single().Kind);
        });

        test("Lightning derives its four support branches from the correct accepted pre-critical original", () =>
        {
            var settings = ExpandedLightningSettings.FromRanks(Ranks("b.05", "c.01"), isElite: id => id == 7);
            var state = new ExpandedLightningState { Evolved = true, Cycle = 4, NextStrikeId = 5 };
            var targets = new Dictionary<int, Vector2>
            {
                [6] = Vector2.UnitX, [2] = new Vector2(1.4f, 0), [7] = new Vector2(1.6f, 0),
                [10] = new Vector2(2.1f, 0), [9] = new Vector2(1.4f, 2.4f),
            };
            var originals = new List<ExpandedLightningHit>();
            var supports = new List<ExpandedLightningHit>();
            void Tick(double now) => ExpandedLightning.Step(state, now, Vector2.Zero, 8, true, 40, 3, 1, 1,
                () => targets, hit => { supports.Add(hit); return true; }, settings, applyOriginalHit: hit =>
                {
                    originals.Add(hit);
                    return hit.TargetId switch { 2 => 50, 6 => 44, 7 => 56, _ => null };
                });
            Tick(0);
            state = Restore(state);
            Tick(.15);
            Check.True(originals.Select(hit => hit.TargetId).SequenceEqual(new[] { 2, 6, 7 }));
            Check.True(originals.All(hit => hit.Kind == ExpandedLightningHitKind.Strike && hit.Damage == 40));
            Check.True(supports.Where(hit => hit.Kind == ExpandedLightningHitKind.Chain).All(hit => Math.Abs(hit.Damage - 30.8) < 1e-9));
            Check.Equal(3, supports.Count(hit => hit.Kind == ExpandedLightningHitKind.Chain));
            Check.Equal(9, supports.Single(hit => hit.Kind == ExpandedLightningHitKind.Current).TargetId);
            Check.Near(17.5, supports.Single(hit => hit.Kind == ExpandedLightningHitKind.Current).Damage);
            state = Restore(state);
            state.Validate(.15, true);
            Check.Near(17.6, state.Pending.Single(hit => hit.HitKind == ExpandedLightningHitKind.Aftershock).Damage);
            Check.Near(19.6, state.Pending.Single(hit => hit.HitKind == ExpandedLightningHitKind.EliteThunder).Damage);

            Check.Equal(3, originals.Count);
            Tick(.65);
            Check.Equal(3, originals.Count);
            Check.True(supports.All(hit => hit.Kind != ExpandedLightningHitKind.Strike));
        });

        test("Lightning current saves a longer stronger bolt and consumes its accepted mark at the inclusive deadline", () =>
        {
            var ranks = ExpandedLightningSettings.FromRanks(Ranks("c.01", "c.02", "c.03", "c.04"));
            var targets = new Dictionary<int, Vector2> { [1] = Vector2.UnitX, [9] = new Vector2(4.49f, 0) };
            var hits = new List<ExpandedLightningHit>();
            var state = new ExpandedLightningState();
            Step(state, 0, targets, hits, settings: ranks);
            Check.Equal((byte)15, state.Pending.Single().CurrentRanks);
            state = Restore(state);
            Step(state, .15, targets, hits);
            var current = hits.Single(hit => hit.Kind == ExpandedLightningHitKind.Current);
            Check.Equal(9, current.TargetId);
            Check.Near(20, current.Damage);
            Check.Equal(0, current.TargetOrdinal);
            Check.Near(0, current.OriginalBaseDamage);
            Check.Near(1.15, state.NextCurrentAt!.Value);
            Check.Near(3.15, state.CurrentMarks!.Single().At);
            state.Validate(.15, false);

            var expired = Restore(state);
            var noProc = new ExpandedLightningState();
            var shortRanks = ExpandedLightningSettings.FromRanks(Ranks("c.01", "c.03"));
            hits.Clear();
            Step(noProc, 0, targets, hits, settings: shortRanks);
            Step(noProc, .15, targets, hits, settings: shortRanks);
            Check.True(hits.All(hit => hit.Kind == ExpandedLightningHitKind.Strike));
            Check.True(!noProc.NextCurrentAt.HasValue);

            targets.Remove(1);
            hits.Clear();
            Step(state, 3, targets, hits, amount: 2, settings: ranks);
            state = Restore(state);
            var rejectedFirst = state.Pending.Min(hit => hit.Id);
            Step(state, 3.15, targets, hits, amount: 2, settings: ranks, accept: hit => hit.StrikeId != rejectedFirst);
            Check.Near(43.2, hits.Single().Damage);
            Check.True(state.CurrentMarks == null);

            hits.Clear();
            targets.Clear();
            Step(expired, 3.151, targets, hits, settings: ranks);
            Check.True(expired.CurrentMarks == null);
            Check.True(expired.WaitingForTarget);
            expired.Validate(3.151, false);
        });

        test("Lightning current skips every original and base chain accept and breaks equal range by ID", () =>
        {
            var ranks = ExpandedLightningSettings.FromRanks(Ranks("c.01"));
            var targets = new Dictionary<int, Vector2>
            {
                [1] = Vector2.UnitX, [2] = new Vector2(2.03f, 0), [3] = new Vector2(2.35f, 0),
                [4] = new Vector2(2.65f, 0), [7] = new Vector2(-1.1f, 1), [8] = new Vector2(-1.1f, -1),
                [9] = new Vector2(1.9f, 0),
            };
            var hits = new List<ExpandedLightningHit>();
            var state = new ExpandedLightningState { Evolved = true };
            Step(state, 0, targets, hits, evolved: true, settings: ranks);
            Step(state, .15, targets, hits, evolved: true, settings: ranks);
            Check.True(hits.Where(hit => hit.Kind == ExpandedLightningHitKind.Strike)
                .Select(hit => hit.TargetId).SequenceEqual(new[] { 1, 9 }));
            Check.True(hits.Where(hit => hit.Kind == ExpandedLightningHitKind.Chain)
                .Select(hit => hit.TargetId).SequenceEqual(new[] { 9, 2, 3 }));
            Check.Equal(4, hits.Single(hit => hit.Kind == ExpandedLightningHitKind.Current).TargetId);
            Check.Near(14, hits.Single(hit => hit.Kind == ExpandedLightningHitKind.Current).Damage);
            state = Restore(state);
            state.Validate(.15, true);

            targets.Remove(4);
            state = new ExpandedLightningState { Evolved = true };
            hits.Clear();
            Step(state, 0, targets, hits, evolved: true, settings: ranks);
            Step(state, .15, targets, hits, evolved: true, settings: ranks);
            Check.Equal(7, hits.Single(hit => hit.Kind == ExpandedLightningHitKind.Current).TargetId);
        });

        test("Lightning current fallback consumes one shared cooldown and cannot mark the first target", () =>
        {
            var ranks = ExpandedLightningSettings.FromRanks(Ranks("c.01", "c.02", "c.03", "c.04", "c.05"));
            var targets = new Dictionary<int, Vector2> { [4] = Vector2.UnitX, [2] = new Vector2(1.4f, 0) };
            var hits = new List<ExpandedLightningHit>();
            var state = new ExpandedLightningState();
            Step(state, 0, targets, hits, amount: 2, cycle: .5, settings: ranks);
            Step(state, .15, targets, hits, amount: 2, cycle: .5, settings: ranks);
            var shock = hits.Single(hit => hit.Kind == ExpandedLightningHitKind.CurrentShock);
            Check.Equal(2, shock.TargetId);
            Check.Equal(1L, shock.StrikeId);
            Check.Near(8, shock.Damage);
            Check.True(state.CurrentMarks == null);
            Check.Near(1.15, state.NextCurrentAt!.Value);
            state = Restore(state);

            hits.Clear();
            Step(state, .5, targets, hits, amount: 2, cycle: .5, settings: ranks);
            Step(state, .65, targets, hits, amount: 2, cycle: .5, settings: ranks);
            Check.True(hits.All(hit => hit.Kind == ExpandedLightningHitKind.Strike));
            Step(state, 1, targets, hits, amount: 2, cycle: .5, settings: ranks);
            Step(state, 1.15, targets, hits, amount: 2, cycle: .5, settings: ranks);
            Check.Equal(1, hits.Count(hit => hit.Kind == ExpandedLightningHitKind.CurrentShock));

            state = new ExpandedLightningState();
            targets.Remove(4);
            hits.Clear();
            Step(state, 0, targets, hits, settings: ranks);
            Step(state, .15, targets, hits, settings: ranks, accept: hit => targets.Remove(hit.TargetId));
            Check.Equal(ExpandedLightningHitKind.Strike, hits.Single().Kind);
            Check.Near(1.15, state.NextCurrentAt!.Value);
        });

        test("Lightning keeps original and chain damage when a reserved support region is denied", () =>
        {
            var nodes = Ranks("b.05", "c.01");
            nodes.Add("common.area.09", 1);
            var ranks = ExpandedLightningSettings.FromRanks(nodes, isElite: id => id == 1);
            var targets = new Dictionary<int, Vector2> { [1] = Vector2.UnitX, [2] = new Vector2(2.5f, 0) };
            var state = new ExpandedLightningState { Evolved = true, Cycle = 4, NextStrikeId = 5 };
            var hits = new List<ExpandedLightningHit>();
            var suppression = 0;
            var slots = 1;
            void Tick(double now) => ExpandedLightning.Step(state, now, Vector2.Zero, 8, true, 40, 3, 1, 1,
                () => targets, hit => { hits.Add(hit); return true; }, ranks,
                reserveSupportRegion: () => slots-- > 0, recordSupportSuppression: () => suppression++);
            Tick(0);
            Tick(.15);
            Check.Equal(1, suppression);
            Check.Equal(ExpandedLightningHitKind.Aftershock, state.Pending.Single().HitKind);
            Check.Near(1.484, state.Pending.Single().Radius);
            Check.True(hits.Select(hit => hit.Kind).SequenceEqual(new[]
                { ExpandedLightningHitKind.Strike, ExpandedLightningHitKind.Chain }));
            Check.Near(40, hits[0].Damage);
            Check.Near(28, hits[1].Damage);
            state = Restore(state);
            state.Validate(.15, true);
            Tick(.65);
            Check.Equal(1, hits.Count(hit => hit.Kind == ExpandedLightningHitKind.Aftershock));
            Check.Equal(1, suppression);

            state = new ExpandedLightningState { Evolved = true, Cycle = 4, NextStrikeId = 5 };
            slots = 0;
            hits.Clear();
            Tick(0);
            Tick(.15);
            Check.Equal(3, suppression);
            Check.Equal(0, state.Pending.Count);
            Check.Equal(2, hits.Count);
        });

        test("Lightning isolation counts every enemy present at that impact even when the first hit kills", () =>
        {
            var settings = ExpandedLightningSettings.FromRanks(Ranks("a.05"));
            var targets = new Dictionary<int, Vector2> { [1] = Vector2.UnitX, [2] = new Vector2(1.6f, 0) };
            var state = new ExpandedLightningState();
            var hits = new List<ExpandedLightningHit>();
            Step(state, 0, targets, hits, settings: settings);
            Step(state, .15, targets, hits, settings: settings, accept: hit =>
            {
                if (hit.TargetId == 1) targets.Remove(1);
                return true;
            });
            Check.Equal(2, hits.Count);
            Check.True(hits.All(hit => hit.Damage == 40));
        });

        test("Lightning stacks frozen solo, high-health, and repeat damage at their inclusive boundaries", () =>
        {
            var settings = ExpandedLightningSettings.FromRanks(Ranks("a.05", "b.01", "b.03"));
            var targets = new Dictionary<int, Vector2> { [9] = Vector2.UnitX };
            var hits = new List<ExpandedLightningHit>();
            var ratio = .75;
            void Tick(ExpandedLightningState current, double now, ExpandedLightningSettings ranks = default,
                double period = 1.5) =>
                ExpandedLightning.Step(current, now, Vector2.Zero, 8, false, 40, period, 1, 1,
                    () => targets, hit => { hits.Add(hit); return true; }, ranks, targetHealthRatio: _ => ratio);

            var state = new ExpandedLightningState();
            Tick(state, 0, settings);
            var restored = Restore(state);
            Tick(restored, .15);
            Check.Near(46.4, hits.Single().Damage);
            Check.Equal(9, restored.RecentStrikeHits!.Single().Id);
            Check.Near(.15, restored.RecentStrikeHits!.Single().At);
            restored.Validate(.15, false);
            hits.Clear();

            targets[2] = Vector2.UnitX * 3; // Exactly two units from the frozen impact.
            Tick(restored, 1.5, settings);
            restored = Restore(restored);
            Tick(restored, 1.65);
            Check.Near(45.2, hits.Single().Damage);
            restored.Validate(1.65, false);

            var late = new ExpandedLightningState();
            Tick(late, 0, settings, period: 1.5001);
            Tick(late, .15);
            Tick(late, 1.5001, settings, period: 1.5001);
            ratio = .749;
            hits.Clear();
            Tick(late, 1.6501);
            Check.Near(40, hits.Single().Damage);
        });

        test("Lightning widened last strike schedules elite thunder at the first accepted elite hit", () =>
        {
            var ranks = Ranks("b.01", "b.04", "b.05");
            ranks.Add("common.area.01", 1);
            ranks.Add("common.area.04", 1);
            var settings = ExpandedLightningSettings.FromRanks(ranks, isElite: id => id == 4);
            var state = new ExpandedLightningState { Evolved = true, Cycle = 4, NextStrikeId = 5 };
            var targets = new Dictionary<int, Vector2>
            {
                [1] = new Vector2(.4f, 0), [8] = new Vector2(4, 0), [4] = new Vector2(5.08f, 0),
            };
            var hits = new List<ExpandedLightningHit>();
            void Tick(ExpandedLightningState current, double now) =>
                ExpandedLightning.Step(current, now, Vector2.Zero, 8, true, 40, 3, 2, 1,
                    () => targets, hit => { hits.Add(hit); return true; }, settings,
                    targetHealthRatio: id => id == 4 ? .75 : .74);

            Tick(state, 0);
            var pending = state.Pending.OrderBy(attack => attack.Id).ToArray();
            Check.Near(1, pending[0].Radius);
            Check.Near(1.112, pending[1].Radius);
            Check.True(!pending[0].WiderLastStrike && pending[1].WiderLastStrike);
            Check.True(pending.All(attack => attack.EliteThunderOnHit));
            state = Restore(state);
            state.Validate(0, true);
            Tick(state, .15);
            var elite = hits.Single(hit => hit.Kind == ExpandedLightningHitKind.Strike && hit.TargetId == 4);
            Check.Equal(6L, elite.StrikeId);
            Check.Near(42.4, elite.Damage);
            var thunder = state.Pending.Single(attack => attack.EliteThunder);
            Check.Equal(6L, thunder.Id);
            Check.Near(5.08, thunder.X, 1e-6);
            Check.Near(.65, thunder.At);
            Check.Near(.872, thunder.Radius);
            Check.Near(14.84, thunder.Damage);
            Check.True(!thunder.IsPrimary && !thunder.CanRetarget && !thunder.EliteThunderOnHit);
            state = Restore(state);
            state.Validate(.15, true);

            hits.Clear();
            Tick(state, .649);
            Check.Equal(0, hits.Count);
            Tick(state, .65);
            Check.True(hits.Select(hit => (hit.Kind, hit.TargetId)).SequenceEqual(
                new[] { (ExpandedLightningHitKind.Aftershock, 1), (ExpandedLightningHitKind.EliteThunder, 4) }));
            Check.True(hits.All(hit => hit.OriginalBaseDamage == 0 && hit.TargetOrdinal == 0));
            Check.Equal(0, state.Pending.Count);

            var other = new ExpandedLightningState { Evolved = true, Cycle = 3, NextStrikeId = 4 };
            Tick(other, 0);
            Tick(other, .15);
            Check.True(!other.Pending.Any(attack => attack.EliteThunder));
        });

        test("Lightning can reopen two support events from one fifth-cycle strike in fixed order", () =>
        {
            var settings = ExpandedLightningSettings.FromRanks(Ranks("b.05"), isElite: _ => true);
            var targets = new Dictionary<int, Vector2> { [1] = Vector2.UnitX };
            var state = new ExpandedLightningState { Evolved = true, Cycle = 4, NextStrikeId = 5 };
            var hits = new List<ExpandedLightningHit>();
            Step(state, 0, targets, hits, evolved: true, settings: settings);
            Step(state, .15, targets, hits, evolved: true, settings: settings);
            Check.True(state.Pending.All(attack => attack.Id == 5));
            state = Restore(state);
            state.Validate(.15, true);
            hits.Clear();
            Step(state, .65, targets, hits, evolved: true, settings: settings);
            Check.True(hits.Select(hit => hit.Kind).SequenceEqual(
                new[] { ExpandedLightningHitKind.Aftershock, ExpandedLightningHitKind.EliteThunder }));
        });

        test("Lightning restores launch base and restarts accepted target ordinals for every strike", () =>
        {
            var state = new ExpandedLightningState();
            var targets = new Dictionary<int, Vector2>
            {
                [9] = new Vector2(1.2f, 0), [2] = new Vector2(1, 0), [7] = new Vector2(1.1f, 0),
            };
            var accepted = new List<ExpandedLightningHit>();
            void Tick(ExpandedLightningState current, double now, int level) =>
                ExpandedLightning.Step(current, now, Vector2.Zero, level, false, 32, 3, 2, 1, () => targets, hit =>
                {
                    if (hit.TargetId == 2) return false;
                    accepted.Add(hit);
                    return true;
                });

            Tick(state, 0, 4);
            Check.True(state.Pending.All(strike => strike.OriginalBaseDamage == 30));
            var corrupt = state.Copy();
            corrupt.Pending[0].OriginalBaseDamage = 31;
            Check.Throws<ArgumentException>(() => corrupt.Validate(0, false));

            var restored = Restore(state);
            restored.Validate(0, false);
            Tick(restored, .15, 8);
            Check.True(accepted.Select(hit => (hit.StrikeId, hit.TargetId, hit.TargetOrdinal, hit.OriginalBaseDamage))
                .SequenceEqual(new[] { (1L, 7, 1, 30d), (1L, 9, 2, 30d),
                    (2L, 7, 1, 30d), (2L, 9, 2, 30d) }));
        });

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
            Check.True(firstStrike.Skip(1).All(hit => hit.Kind == ExpandedLightningHitKind.Chain && hit.Damage == 28 &&
                hit.OriginalBaseDamage == 0 && hit.TargetOrdinal == 0));
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
            Check.Equal(0d, replayHits[0].OriginalBaseDamage);
            Check.Equal(0, replayHits[0].TargetOrdinal);
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
