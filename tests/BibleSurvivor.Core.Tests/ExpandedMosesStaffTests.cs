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
        test("Staff evolved closure launches one real endpoint-to-origin return with frozen parent geometry", () =>
        {
            var ranks = ExpandedMosesStaffRankOptions.FromRanks(StaffRanks("a.05", "e.04"));
            var targets = new Dictionary<int, Vector2>();
            var hits = new List<ExpandedMosesStaffHit>();
            var state = new ExpandedMosesStaffState { Evolved = true };
            var suppressed = 0;
            var allowReturn = true;
            void Tick(double now) => ExpandedMosesStaff.Step(state, now, Vector2.Zero, Vector2.UnitX,
                8, true, 40, 3, 1, .9, 7, 5, () => targets, targets, hit => { hits.Add(hit); return true; },
                rankOptions: ranks, isElite: _ => false, reserveSupportProjectile: () => allowReturn,
                recordSupportSuppression: () => suppressed++);

            Tick(0);
            Tick(.1);
            targets[2] = new Vector2(1, 3);
            Tick(.2);
            Tick(1.4);
            var wave = state.Waves.Single();
            var terminal = wave.Position;
            var width = wave.Width;
            Check.True(terminal.Y > 1);
            var blocked = RoundTrip(state);
            state = RoundTrip(state);
            state.Validate(1.4, true);
            targets.Clear();
            Tick(2.099);
            Check.True(state.ReturnWaves == null);
            Tick(2.1);
            var child = state.ReturnWaves!.Single();
            Check.True(child.FromEvolvedLane);
            Check.Equal(terminal, child.Origin);
            Check.Near(terminal.Length(), child.Range);
            Check.Near(width, child.Width);
            Check.Near(5, child.Speed);
            Check.Near(20, child.Damage);
            Check.Near(2.1, child.StartedAt);
            Check.Near(5.1, state.NextEvolvedReturnAt!.Value);
            Check.True(!state.NextReturnAt.HasValue);
            Check.True(Vector2.Distance(child.Origin + child.Direction * (float)child.Range, Vector2.Zero) < 1e-6f);
            targets[3] = terminal + child.Direction * .5f;
            state = RoundTrip(state);
            state.Validate(2.1, true);
            hits.Clear();
            Tick(2.3);
            var childHit = hits.Single(hit => hit.Kind == ExpandedMosesStaffHitKind.EvolvedReturnWave);
            Check.Equal(3, childHit.TargetId);
            Check.Near(20, childHit.Damage);
            Check.Equal(0, childHit.TargetOrdinal);
            Check.Near(1, state.ReturnWaves!.Single().Distance);

            state = blocked;
            allowReturn = false;
            Tick(2.1);
            Check.Equal(1, suppressed);
            Check.True(state.ReturnWaves == null && !state.NextEvolvedReturnAt.HasValue);
            Check.True(state.Waves.Count == 0);
        });

        test("Staff accepted elite original kill leaves one directional lane with saved precritical amount and expiry", () =>
        {
            var ranks = ExpandedMosesStaffRankOptions.FromRanks(StaffRanks("e.05"));
            var targets = new Dictionary<int, Vector2>
            {
                [1] = Vector2.UnitX, [2] = new Vector2(2.99f, .4f), [3] = new Vector2(2.99f, .401f),
                [4] = new Vector2(.95f, 0), [5] = new Vector2(3.001f, 0),
            };
            var hits = new List<ExpandedMosesStaffHit>();
            var state = new ExpandedMosesStaffState { Evolved = true };
            var emittedSuppression = 0;
            var allowArea = true;
            void Tick(double now, bool evolved = true) => ExpandedMosesStaff.Step(state, now, Vector2.Zero,
                Vector2.UnitX, 8, evolved, 40, 3, 1, .9, 7, 5, () => targets, targets,
                hit => { hits.Add(hit); return true; }, rankOptions: ranks, isElite: id => id == 1 || id == 4,
                reserveLane: () => allowArea, recordSupportSuppression: () => emittedSuppression++,
                applyOriginalHit: hit =>
                {
                    if (hit.TargetId != 1 && hit.TargetId != 4) return hit.Damage;
                    targets.Remove(hit.TargetId);
                    return 50;
                });

            Tick(0);
            state = RoundTrip(state);
            Tick(.2);
            var lane = state.EliteKillLane!;
            Check.Equal(1, lane.VictimId);
            Check.Equal(Vector2.UnitX, lane.Origin);
            Check.Equal(Vector2.UnitX, lane.Direction);
            Check.Near(.8, lane.Width);
            Check.Near(2, lane.Length);
            Check.Near(15, lane.Damage);
            Check.Near(1.2, lane.EndsAt);
            Check.Near(.7, lane.NextPulseAt);
            Check.Near(5.2, state.NextEliteKillLaneAt!.Value);
            Check.Equal(2, state.ActiveLaneCount);
            Check.Equal(1, state.ActiveExtraGroups);
            Check.Equal(2, hits.Single(hit => hit.Kind == ExpandedMosesStaffHitKind.EliteKillLane).TargetId);
            Check.True(!targets.ContainsKey(4));
            state = RoundTrip(state);
            state.Validate(.2, true);
            hits.Clear();
            Tick(.7);
            Check.Equal(2, hits.Single(hit => hit.Kind == ExpandedMosesStaffHitKind.EliteKillLane).TargetId);
            Tick(1.2);
            Check.Equal(1, hits.Count(hit => hit.Kind == ExpandedMosesStaffHitKind.EliteKillLane));
            Check.True(state.EliteKillLane == null);

            allowArea = true;
            state = new ExpandedMosesStaffState();
            targets[1] = Vector2.UnitX;
            Tick(0, false);
            Tick(.2, false);
            Check.True(state.EliteKillLane == null && !state.NextEliteKillLaneAt.HasValue);

            state = new ExpandedMosesStaffState { Evolved = true };
            targets[1] = Vector2.UnitX;
            Tick(0);
            allowArea = false;
            Tick(.2);
            Check.Equal(1, emittedSuppression);
            Check.True(state.EliteKillLane == null && !state.NextEliteKillLaneAt.HasValue);
        });

        test("Staff evolved kill lane cannot displace the four already reserved extra groups", () =>
        {
            var ranks = ExpandedMosesStaffRankOptions.FromRanks(StaffRanks("c.01", "e.05"));
            var targets = new Dictionary<int, Vector2> { [1] = Vector2.UnitX };
            var state = new ExpandedMosesStaffState { Evolved = true };
            var suppressions = 0;
            void Tick(double now) => ExpandedMosesStaff.Step(state, now, Vector2.Zero, Vector2.UnitX, 8,
                true, 40, 3, 4, .9, 7, 5, () => targets, targets, _ => true, rankOptions: ranks,
                isElite: _ => true, reserveLane: () => true, recordSupportSuppression: () => suppressions++,
                applyOriginalHit: hit => targets.Remove(hit.TargetId) ? 40 : null);
            Tick(0);
            Check.Equal(4, state.ActiveExtraGroups);
            Check.Equal(8, state.ActiveLaneCount);
            state = RoundTrip(state);
            Tick(.2);
            Check.True(!targets.ContainsKey(1));
            Check.Equal(1, suppressions);
            Check.Equal(4, state.ActiveExtraGroups);
            Check.Equal(8, state.ActiveLaneCount);
            Check.True(state.EliteKillLane == null);
            state.Validate(.2, true);
        });

        test("Staff defensive wave attempts the earliest surviving crossing before a later lower ID", () =>
        {
            var ranks = ExpandedMosesStaffRankOptions.FromRanks(StaffRanks("d.02"));
            var state = new ExpandedMosesStaffState();
            var targets = new Dictionary<int, Vector2>();
            var hostiles = new List<ExpandedMosesStaffHostileSweep>
            {
                new ExpandedMosesStaffHostileSweep(1, Vector2.UnitX, Vector2.UnitX, .1),
                new ExpandedMosesStaffHostileSweep(2, Vector2.UnitX * .2f, Vector2.UnitX * .2f, .1),
            };
            var erased = new List<long>();
            void Tick(double now) => ExpandedMosesStaff.Step(state, now, Vector2.Zero, Vector2.UnitX,
                8, false, 40, 3, 1, .9, 7, 5, () => targets, targets, _ => true,
                rankOptions: ranks, hostileSweeps: () => hostiles, nextHostileEraseRandom: _ => 1, eraseHostile: id =>
                {
                    erased.Add(id);
                    return hostiles.RemoveAll(shot => shot.Id == id) == 1;
                });
            Tick(0);
            Tick(.2);
            Check.True(erased.SequenceEqual(new[] { 2L }));
        });

        test("Staff launch defenses separate the forward water screen from the surrounded circle", () =>
        {
            var ranks = ExpandedMosesStaffRankOptions.FromRanks(StaffRanks("d.01", "d.05"));
            var targets = new Dictionary<int, Vector2>
            {
                [1] = new Vector2(1.2f, 0), [2] = new Vector2((float)Math.Cos(Math.PI / 6), .5f),
                [3] = new Vector2(0, 1), [4] = new Vector2(-1, 0), [5] = new Vector2(0, -1),
                [6] = new Vector2(2, 0), [7] = new Vector2(2.001f, 0),
                [8] = new Vector2(.86f, .51f),
            };
            var hits = new List<ExpandedMosesStaffHit>();
            var state = new ExpandedMosesStaffState();
            Step(state, 0, targets, hits, amount: 2, ranks: ranks);
            Check.True(hits.Where(hit => hit.Kind == ExpandedMosesStaffHitKind.WaterScreen)
                .Select(hit => hit.TargetId).SequenceEqual(new[] { 1, 2 }));
            Check.True(hits.Where(hit => hit.Kind == ExpandedMosesStaffHitKind.EncirclingWave)
                .Select(hit => hit.TargetId).SequenceEqual(new[] { 1, 2, 3, 4, 5, 6, 8 }));
            Check.True(hits.All(hit => hit.WaveId == 1 && hit.TargetOrdinal == 0 && hit.OriginalBaseDamage == 0));
            Check.True(hits.Where(hit => hit.Kind == ExpandedMosesStaffHitKind.WaterScreen).All(hit => hit.Damage == 10));
            Check.True(hits.Where(hit => hit.Kind == ExpandedMosesStaffHitKind.EncirclingWave).All(hit => hit.Damage == 12));
            Check.Near(2, state.NextWaterScreenAt!.Value);
            Check.Near(4, state.NextEncirclingWaveAt!.Value);
            state = RoundTrip(state);
            state.Validate(0, false);

            hits.Clear();
            Step(state, 3, targets, hits, ranks: ranks);
            Check.True(hits.Any(hit => hit.Kind == ExpandedMosesStaffHitKind.WaterScreen));
            Check.True(hits.All(hit => hit.Kind != ExpandedMosesStaffHitKind.EncirclingWave));

            state = new ExpandedMosesStaffState();
            targets.Remove(6);
            targets.Remove(8);
            hits.Clear();
            Step(state, 0, targets, hits, ranks: ranks);
            Check.True(hits.All(hit => hit.Kind != ExpandedMosesStaffHitKind.EncirclingWave));
            Check.True(!state.NextEncirclingWaveAt.HasValue);

            state = new ExpandedMosesStaffState();
            hits.Clear();
            Step(state, 0, targets, hits, original: () => false, ranks: ranks);
            Check.Equal(0, hits.Count);
            Check.True(!state.NextWaterScreenAt.HasValue);
        });

        test("Staff defensive contact rolls once per wave and hostile sweep and saves the independent RNG", () =>
        {
            var ranks = ExpandedMosesStaffRankOptions.FromRanks(StaffRanks("d.02"));
            var state = new ExpandedMosesStaffState();
            var actors = new Dictionary<int, Vector2>();
            var hostiles = new Dictionary<long, ExpandedMosesStaffHostileSweep>
            {
                [1] = new ExpandedMosesStaffHostileSweep(1, new Vector2(.4f, 0), new Vector2(.4f, 0), .1),
                [9] = new ExpandedMosesStaffHostileSweep(9, new Vector2(.4f, 2), new Vector2(.4f, 2), .1),
            };
            var rolls = 0;
            var successes = new List<long>();
            var allowed = false;
            void Tick(double now) => ExpandedMosesStaff.Step(state, now, Vector2.Zero, Vector2.UnitX, 8,
                false, 40, 3, 1, .9, 7, 5, () => actors, actors, _ => true, rankOptions: ranks,
                hostileSweeps: () => hostiles.Values.ToArray(), canEraseHostile: () => allowed, eraseHostile: id =>
                {
                    successes.Add(id);
                    return hostiles.Remove(id);
                }, nextHostileEraseRandom: previous =>
                {
                    rolls++;
                    if (rolls == 1) { Check.True(!previous.HasValue); return 858993460u; }
                    Check.Equal(858993460u, previous!.Value);
                    return 858993459u;
                });

            Tick(0);
            Tick(.02);
            Check.Equal(0, rolls);
            allowed = true;
            Tick(.1);
            Check.Equal(1, rolls);
            Check.True(!state.NextHostileEraseAt.HasValue);
            Check.True(state.Waves.Single().HostileEraseAttemptIds!.SequenceEqual(new[] { 1L }));
            state = RoundTrip(state);
            state.Validate(.1, false);
            var exceedsChainCapacity = RoundTrip(state);
            exceedsChainCapacity.Waves.Single().HostileEraseAttemptIds = Enumerable
                .Range(1, BossDefinition.ChainCap + 1).Select(id => (long)id).ToList();
            Check.Throws<ArgumentException>(() => exceedsChainCapacity.Validate(.1, false));
            hostiles[1] = new ExpandedMosesStaffHostileSweep(1, new Vector2(.65f, 0), new Vector2(.9f, 0), .1);
            Tick(.14);
            Check.Equal(1, rolls);
            hostiles[2] = new ExpandedMosesStaffHostileSweep(2, new Vector2(.85f, 1), new Vector2(.85f, -1), .1);
            Tick(.2);
            Check.Equal(2, rolls);
            Check.True(successes.SequenceEqual(new[] { 2L }));
            Check.Near(2.2, state.NextHostileEraseAt!.Value);
            Check.Equal(858993459u, state.HostileEraseRandomState!.Value);
            Check.True(state.Waves.Single().HostileEraseAttemptIds!.SequenceEqual(new[] { 1L }));
            state = RoundTrip(state);
            state.Validate(.2, false);
            hostiles.Remove(1);
            hostiles[3] = new ExpandedMosesStaffHostileSweep(3, new Vector2(1.2f, 0), new Vector2(1.2f, 0), .1);
            Tick(.25);
            Check.Equal(2, rolls);
            Check.True(state.Waves.Single().HostileEraseAttemptIds == null);
        });

        test("Staff hurt advance consumes one accepted hurt, honors cooldown across restore, and preserves the cycle floor", () =>
        {
            var ranks = ExpandedMosesStaffRankOptions.FromRanks(StaffRanks("d.03"));
            var state = new ExpandedMosesStaffState();
            var targets = new Dictionary<int, Vector2>();
            int Tick(double now, double? hurt = null, double period = 3) =>
                ExpandedMosesStaff.Step(state, now, Vector2.Zero, Vector2.UnitX, 8, false, 40,
                    period, 1, .9, 7, 5, () => targets, targets, _ => true,
                    rankOptions: ranks, lastPlayerHurtAt: hurt);

            Check.Equal(1, Tick(0));
            Check.Near(3, state.NextCycle);
            Check.Equal(0, Tick(1, 1));
            Check.Near(2.75, state.NextCycle);
            Check.Near(6, state.NextHurtAdvanceAt!.Value);
            state = RoundTrip(state);
            state.Validate(1, false);
            Check.Equal(0, Tick(2, 1));
            Check.Equal(0, Tick(2.1, 2.1));
            Check.Near(2.75, state.NextCycle);
            Check.Near(2.1, state.LastPlayerHurtObservedAt!.Value);
            Check.Equal(1, Tick(2.75, 2.1));
            Check.Equal(1, Tick(5.75, 2.1));
            Check.Near(8.75, state.NextCycle);
            Check.Equal(0, Tick(6, 6));
            Check.Near(8.5, state.NextCycle);
            Check.Near(11, state.NextHurtAdvanceAt!.Value);

            state = new ExpandedMosesStaffState();
            Tick(0, period: .2);
            Check.Equal(0, Tick(.05, .05, .2));
            Check.Near(.15, state.NextCycle);
            Check.Equal(0, Tick(.149, .05, .2));
            Check.Equal(1, Tick(.15, .05, .2));
            Check.Near(.15, state.LastCycleStartedAt!.Value);
        });

        test("Staff lane movement membership tests live residual or evolved geometry at inclusive edges", () =>
        {
            var ranks = ExpandedMosesStaffRankOptions.FromRanks(StaffRanks("c.01"));
            var targets = new Dictionary<int, Vector2>();
            var hits = new List<ExpandedMosesStaffHit>();
            var state = new ExpandedMosesStaffState();
            Step(state, 0, targets, hits, ranks: ranks);
            Check.True(!ExpandedMosesStaff.IsPlayerInLane(state, Vector2.Zero));
            Step(state, .2, targets, hits, ranks: ranks);
            Check.True(ExpandedMosesStaff.IsPlayerInLane(state, new Vector2(1, .35f)));
            Check.True(!ExpandedMosesStaff.IsPlayerInLane(state, new Vector2(1, .351f)));
            state = RoundTrip(state);
            Step(state, 1.7, targets, hits, ranks: ranks);
            Check.True(!ExpandedMosesStaff.IsPlayerInLane(state, new Vector2(1, .35f)));

            state = new ExpandedMosesStaffState { Evolved = true };
            Step(state, 0, targets, hits, evolved: true, ranks: ranks);
            Step(state, .2, targets, hits, evolved: true, ranks: ranks);
            Check.True(ExpandedMosesStaff.IsPlayerInLane(state, new Vector2(1, .35f)));
            Check.True(ExpandedMosesStaff.IsPlayerInLane(state, new Vector2(1, .6f)));
            Check.True(!ExpandedMosesStaff.IsPlayerInLane(state, new Vector2(1, .601f)));
        });

        test("Staff residual lane saves its independent width, longer lifetime, outer-entry ledger, and pulse phase", () =>
        {
            var nodes = StaffRanks("c.01", "c.02", "c.03");
            nodes.Add("common.duration.02", 1);
            var ranks = ExpandedMosesStaffRankOptions.FromRanks(nodes);
            var state = new ExpandedMosesStaffState();
            var targets = new Dictionary<int, Vector2>
            {
                [2] = new Vector2(.3f, .28f), [3] = new Vector2(.3f, .279f), [4] = new Vector2(.3f, .351f),
            };
            var hits = new List<ExpandedMosesStaffHit>();
            Step(state, 0, targets, hits, ranks: ranks);
            state = RoundTrip(state);
            Step(state, .1, targets, hits);
            var wave = state.Waves.Single();
            Check.Equal((byte)7, wave.ResidualLaneRanks);
            Check.True(wave.HasResidualLane && !wave.HasLane);
            Check.Near(.1, wave.ResidualLaneStartedAt!.Value);
            Check.Near(.7, wave.ResidualLaneWidth);
            Check.Near(1.8, wave.ResidualLaneDuration);
            Check.True(wave.ResidualLaneEdgeIds!.SequenceEqual(new[] { 2 }));
            var extra = hits.Single(hit => hit.Kind == ExpandedMosesStaffHitKind.ResidualEdge);
            Check.Equal(2, extra.TargetId);
            Check.Near(8, extra.Damage);
            Check.True(hits.Where(hit => hit.Kind == ExpandedMosesStaffHitKind.ResidualLane)
                .Select(hit => hit.TargetId).SequenceEqual(new[] { 2, 3 }));
            Check.True(hits.Where(hit => hit.Kind == ExpandedMosesStaffHitKind.ResidualLane)
                .All(hit => hit.Damage == 10 && hit.TargetOrdinal == 0 && hit.OriginalBaseDamage == 0));
            state = RoundTrip(state);
            state.Validate(.1, false);
            hits.Clear();
            targets[5] = new Vector2(.35f, 0);
            var previous = targets.ToDictionary(pair => pair.Key, pair => pair.Value);
            previous[5] = new Vector2(.35f, .5f);
            Step(state, .2, targets, hits, previousTargets: previous);
            Check.Equal(5, hits.Single(hit => hit.Kind == ExpandedMosesStaffHitKind.ResidualEdge).TargetId);
            Check.True(hits.All(hit => hit.Kind != ExpandedMosesStaffHitKind.ResidualLane));
            hits.Clear();
            Step(state, .6, targets, hits);
            Step(state, 1.1, targets, hits);
            Step(state, 1.6, targets, hits);
            Check.Equal(9, hits.Count(hit => hit.Kind == ExpandedMosesStaffHitKind.ResidualLane));
            Step(state, 1.9, targets, hits);
            Check.Equal(9, hits.Count(hit => hit.Kind == ExpandedMosesStaffHitKind.ResidualLane));
            Check.Equal(0, state.ActiveResidualLaneCount);

            state = new ExpandedMosesStaffState { Evolved = true };
            hits.Clear();
            Step(state, 0, targets, hits, evolved: true, ranks: ranks);
            Step(state, .1, targets, hits, evolved: true, ranks: ranks);
            Check.Equal(2, state.ActiveLaneCount);
            Check.True(state.Waves.Single().HasLane && state.Waves.Single().HasResidualLane);
            Check.Near(14, hits.Single(hit => hit.Kind == ExpandedMosesStaffHitKind.Lane && hit.TargetId == 3).Damage);
            Check.Near(10, hits.Single(hit => hit.Kind == ExpandedMosesStaffHitKind.ResidualLane && hit.TargetId == 3).Damage);
        });

        test("Staff residual crossing finds overlap after the pivot and records each first pair across restore", () =>
        {
            var oldRanks = ExpandedMosesStaffRankOptions.FromRanks(StaffRanks("a.05", "c.01", "c.04"));
            var newRanks = ExpandedMosesStaffRankOptions.FromRanks(StaffRanks("c.01", "c.04"));
            var state = new ExpandedMosesStaffState();
            var targets = new Dictionary<int, Vector2>();
            var hits = new List<ExpandedMosesStaffHit>();
            void Tick(double now, Vector2 position = default, Vector2 direction = default,
                double wait = 3, ExpandedMosesStaffRankOptions ranks = default) =>
                ExpandedMosesStaff.Step(state, now, position, direction, 8, false, 40, wait,
                    1, .9, 7, 5, () => targets, targets, hit => { hits.Add(hit); return true; },
                    rankOptions: ranks, isElite: _ => false);

            Tick(0, wait: .5, ranks: oldRanks);
            Tick(.1);
            targets[99] = new Vector2(1, 3);
            Tick(.2);
            Check.Near(.5, state.Waves[0].TurnAtDistance!.Value);
            targets.Clear();
            Tick(.5, new Vector2(3, -2), Vector2.UnitY, ranks: newRanks);
            targets[1] = new Vector2(3, .4f);
            targets[2] = new Vector2(3, -2);
            Tick(.84);
            Check.True(hits.All(hit => hit.Kind != ExpandedMosesStaffHitKind.ResidualCross));
            state = RoundTrip(state);
            hits.Clear();
            Tick(.98);
            var cross = hits.Single(hit => hit.Kind == ExpandedMosesStaffHitKind.ResidualCross);
            Check.Equal(1, cross.TargetId);
            Check.Equal(2L, cross.WaveId);
            Check.Near(12, cross.Damage);
            Check.Near(3.98, state.NextResidualCrossAt!.Value);
            Check.True(state.Waves[1].ResidualLaneOverlapIds!.SequenceEqual(new[] { 1L }));
            state = RoundTrip(state);
            state.Validate(.98, false);
            hits.Clear();
            Tick(1);
            Check.True(hits.All(hit => hit.Kind != ExpandedMosesStaffHitKind.ResidualCross));
        });

        test("Staff residual natural ending uses the bent midpoint and can be cancelled by changing form", () =>
        {
            var ranks = ExpandedMosesStaffRankOptions.FromRanks(StaffRanks("a.05", "c.01", "c.05"));
            var state = new ExpandedMosesStaffState();
            var targets = new Dictionary<int, Vector2>();
            var hits = new List<ExpandedMosesStaffHit>();
            void Tick(double now, bool evolved = false) => Step(state, now, targets, hits,
                evolved: evolved, ranks: ranks, speed: 6, isElite: _ => false);
            Tick(0);
            Tick(.15);
            targets[90] = new Vector2(1, 3);
            Tick(.2);
            Tick(1.2);
            var cancelled = RoundTrip(state);
            targets.Clear();
            targets[2] = new Vector2(3.45f, 1.65f);
            targets[3] = new Vector2(3.45f, -1.1f);
            hits.Clear();
            state = RoundTrip(state);
            Tick(1.649);
            Check.True(hits.All(hit => hit.Kind != ExpandedMosesStaffHitKind.ResidualEnd));
            Tick(1.65);
            var end = hits.Single(hit => hit.Kind == ExpandedMosesStaffHitKind.ResidualEnd);
            Check.Equal(2, end.TargetId);
            Check.Near(10, end.Damage);
            Check.Near(4.65, state.NextResidualEndAt!.Value);
            Check.Equal(0, state.ActiveResidualLaneCount);

            state = cancelled;
            state.ResetForForm(true, 1.64);
            hits.Clear();
            Tick(1.65, true);
            Check.True(hits.All(hit => hit.Kind != ExpandedMosesStaffHitKind.ResidualEnd));
            Check.True(!state.NextResidualEndAt.HasValue);
        });

        test("Staff extra residual lanes honor the four-group limit without suppressing original waves", () =>
        {
            var ranks = ExpandedMosesStaffRankOptions.FromRanks(StaffRanks("c.01"));
            var empty = new Dictionary<int, Vector2>();
            var state = new ExpandedMosesStaffState();
            var suppression = 0;
            int Tick(double now, int amount) => ExpandedMosesStaff.Step(state, now, Vector2.Zero,
                Vector2.UnitX, 8, false, 40, .5, amount, .9, 7, 5, () => empty, empty, _ => true,
                rankOptions: ranks, recordSupportSuppression: () => suppression++);
            Check.Equal(4, Tick(0, 4));
            Check.Equal(4, state.ActiveResidualLaneCount);
            Check.Equal(1, Tick(.5, 1));
            Check.Equal(1, suppression);
            Check.Equal(5, state.ActiveWaveCount);
            Check.Equal(4, state.ActiveResidualLaneCount);
            Check.True(!state.Waves[^1].HasResidualLane && state.Waves[^1].ResidualLaneRanks == 0);
            state = RoundTrip(state);
            state.Validate(.5, false);
        });

        test("Staff endpoint return and pillar share the parent endpoint but retain distinct reservations and cooldowns", () =>
        {
            var ranks = ExpandedMosesStaffRankOptions.FromRanks(StaffRanks("a.04", "b.02"));
            var targets = new Dictionary<int, Vector2> { [1] = new Vector2(3, .99f), [2] = new Vector2(3, 1.01f) };
            var hits = new List<ExpandedMosesStaffHit>();
            var state = new ExpandedMosesStaffState();
            var suppression = 0;
            void Tick(double now, bool allowReturn = true, bool allowPillar = true, double wait = .5) =>
                ExpandedMosesStaff.Step(state, now, Vector2.Zero, Vector2.UnitX, 8, false, 40, wait, 1,
                    .9, 3, 6, () => targets, targets, hit => { hits.Add(hit); return true; },
                    reserveLane: () => allowPillar, rankOptions: ranks, reserveSupportProjectile: () => allowReturn,
                    recordSupportSuppression: () => suppression++);

            Tick(0);
            Check.Equal((byte)5, state.Waves.Single().TechniqueRanks);
            state = RoundTrip(state);
            Tick(.5);
            Check.Equal(ExpandedMosesStaffHitKind.EndPillar, hits.Single().Kind);
            Check.Equal(1, hits.Single().TargetId);
            Check.Near(14, hits.Single().Damage);
            Check.Near(2.5, state.NextEndPillarAt!.Value);
            Check.Near(2.5, state.NextReturnAt!.Value);
            var child = state.ReturnWaves!.Single();
            Check.Equal(1L, child.SourceWaveId);
            Check.Equal(new Vector2(3, 0), child.Origin);
            Check.Equal(-Vector2.UnitX, child.Direction);
            Check.Near(.6, child.Width);
            Check.Near(.5, child.StartedAt);
            state.Validate(.5, false);
            state = RoundTrip(state);
            targets[3] = new Vector2(2, .29f);
            targets[4] = new Vector2(2, .31f);
            hits.Clear();
            Tick(.75);
            var returnHit = hits.Single(hit => hit.Kind == ExpandedMosesStaffHitKind.ReturnWave);
            Check.Equal(3, returnHit.TargetId);
            Check.Near(14, returnHit.Damage);
            Check.Equal(0, returnHit.TargetOrdinal);
            Check.Near(0, returnHit.OriginalBaseDamage);
            Check.Near(1.5, state.ReturnWaves!.Single().Distance);
            state = RoundTrip(state);
            state.Validate(.75, false);
            Tick(1);
            Check.True(state.ReturnWaves == null);
            Check.Equal(1, hits.Count(hit => hit.Kind == ExpandedMosesStaffHitKind.ReturnWave));
            Check.Equal(0, suppression);

            state = new ExpandedMosesStaffState();
            hits.Clear();
            Tick(0);
            Tick(.5, allowReturn: false, allowPillar: false);
            Check.Equal(2, suppression);
            Check.True(state.ReturnWaves == null && !state.NextReturnAt.HasValue && !state.NextEndPillarAt.HasValue);
            Check.True(hits.All(hit => hit.Kind == ExpandedMosesStaffHitKind.Wave));
        });

        test("Staff first ordinary target bends once by at most twelve degrees and freezes both lane segments", () =>
        {
            var ranks = ExpandedMosesStaffRankOptions.FromRanks(StaffRanks("a.05"));
            var targets = new Dictionary<int, Vector2> { [1] = new Vector2(5, 2) };
            var state = new ExpandedMosesStaffState { Evolved = true };
            var hits = new List<ExpandedMosesStaffHit>();
            void Tick(double now, ExpandedMosesStaffRankOptions activeRanks = default) =>
                Step(state, now, targets, hits, evolved: true, speed: 5, ranks: activeRanks, isElite: id => id == 1);

            Tick(0, ranks);
            Tick(.2);
            var wave = state.Waves.Single();
            Check.True(!wave.TurnAtDistance.HasValue);
            Check.Equal(new Vector2(1, 0), wave.Position);
            targets.Add(2, new Vector2(1.5f, 3));
            Tick(.3);
            wave = state.Waves.Single();
            Check.Near(1, wave.TurnAtDistance!.Value);
            Check.Near(12, Math.Atan2(wave.TravelDirection.Y, wave.TravelDirection.X) * 180 / Math.PI, 1e-4);
            Check.Equal(Vector2.UnitX, wave.TurnPosition);
            state.Validate(.3, true);
            state = RoundTrip(state);
            targets.Clear();
            targets[20] = new Vector2(1.5f, -.5f);
            targets[30] = new Vector2(5.5f, .95f);
            targets[31] = new Vector2(5.5f, -.05f);
            hits.Clear();
            Tick(1.2);
            wave = state.Waves.Single();
            Check.Near(12, Math.Atan2(wave.TravelDirection.Y, wave.TravelDirection.X) * 180 / Math.PI, 1e-4);
            Check.True(hits.Any(hit => hit.Kind == ExpandedMosesStaffHitKind.Wave && hit.TargetId == 30));
            Check.True(hits.Any(hit => hit.Kind == ExpandedMosesStaffHitKind.Lane && hit.TargetId == 30));
            Check.True(hits.All(hit => hit.TargetId != 31));
            state.Validate(1.2, true);
        });

        test("Staff near damage uses the inclusive two-unit origin and elite splash consumes actual precritical damage", () =>
        {
            var ranks = ExpandedMosesStaffRankOptions.FromRanks(StaffRanks("b.03", "b.04"));
            var targets = new Dictionary<int, Vector2>
            {
                [2] = new Vector2(2, 0), [3] = new Vector2(2.001f, .02f),
                [4] = new Vector2(2, 1.2f), [5] = new Vector2(2, 1.2001f),
            };
            var state = new ExpandedMosesStaffState();
            var originals = new List<ExpandedMosesStaffHit>();
            var supports = new List<ExpandedMosesStaffHit>();
            void Tick(double now, bool rejectElite = false) => ExpandedMosesStaff.Step(state, now, Vector2.Zero,
                Vector2.UnitX, 8, false, 40, 3, 1, .9, 7, 6, () => targets, targets,
                hit => { supports.Add(hit); return true; }, rankOptions: ranks, isElite: id => id == 2,
                applyOriginalHit: hit =>
                {
                    originals.Add(hit);
                    return hit.TargetId == 2 ? rejectElite ? null : 54 : hit.Damage;
                });

            Tick(0);
            state = RoundTrip(state);
            Tick(.4);
            Check.Near(43.2, originals.Single(hit => hit.TargetId == 2).Damage);
            Check.Near(40, originals.Single(hit => hit.TargetId == 3).Damage);
            Check.True(supports.Select(hit => hit.TargetId).SequenceEqual(new[] { 2, 3, 4 }));
            Check.True(supports.All(hit => hit.Kind == ExpandedMosesStaffHitKind.EliteSplash &&
                hit.Damage == 13.5 && hit.TargetOrdinal == 0 && hit.OriginalBaseDamage == 0));
            Check.Near(2.4, state.NextEliteSplashAt!.Value);
            state = RoundTrip(state);
            state.Validate(.4, false);

            state = new ExpandedMosesStaffState();
            supports.Clear();
            Tick(0);
            Tick(.4, rejectElite: true);
            Check.True(supports.Count == 0 && !state.NextEliteSplashAt.HasValue);
        });

        test("Staff fourth cycle enhances only its first emitted original with level-one width and additive damage", () =>
        {
            var ranks = ExpandedMosesStaffRankOptions.FromRanks(StaffRanks("b.05"));
            var state = new ExpandedMosesStaffState { Cycle = 3, NextWaveId = 4 };
            var hits = new List<ExpandedMosesStaffHit>();
            var targets = new Dictionary<int, Vector2>();
            Step(state, 0, targets, hits, amount: 2, width: 1.08, ranks: ranks);
            Check.True(state.Waves.Select(wave => wave.TechniqueRanks).SequenceEqual(new byte[] { 32, 0 }));
            Check.Near(1.215, state.Waves[0].Width);
            Check.Near(44, state.Waves[0].Damage);
            Check.Near(1.08, state.Waves[1].Width);
            Check.Near(40, state.Waves[1].Damage);
            state = RoundTrip(state);
            state.Validate(0, false);

            state = new ExpandedMosesStaffState { Evolved = true, Cycle = 3, NextWaveId = 4 };
            Step(state, 0, targets, hits, evolved: true, amount: 2, width: 1.08, ranks: ranks);
            Check.Near(1.458, state.Waves[0].Width);
            Check.Near(44, state.Waves[0].Damage);
            Check.Near(1.296, state.Waves[1].Width);
            Check.Near(40, state.Waves[1].Damage);

            state = new ExpandedMosesStaffState { Cycle = 4, NextWaveId = 5 };
            Step(state, 0, targets, hits, amount: 2, ranks: ranks);
            Check.True(state.Waves.All(wave => wave.TechniqueRanks == 0));
        });

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

    private static Dictionary<string, int> StaffRanks(params string[] nodes) =>
        nodes.ToDictionary(node => "weapon.moses_staff." + node, _ => 1);

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
