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
        test("Chariot a.05 gives only the first fifth-cycle natural arrival an exact reversed terminal route", () =>
        {
            var ranks = ExpandedElijahChariotRankOptions.FromRanks(new Dictionary<string, int>
            {
                ["weapon.elijah_chariot.a.04"] = 1, ["weapon.elijah_chariot.a.05"] = 1,
                ["weapon.elijah_chariot.d.01"] = 1, ["weapon.elijah_chariot.d.02"] = 1,
            });
            var targets = new Dictionary<int, Vector2> { [1] = new Vector2(5, 2), [2] = new Vector2(5.2f, 2.2f) };
            var hits = new List<ExpandedElijahChariotHit>();
            var state = new ExpandedElijahChariotState { Cycle = 4, NextPathId = 5 };
            Step(state, 0, targets, hits, amount: 2, ranks: ranks);
            Check.True(state.Paths[0].ReversesAtEnd && !state.Paths[1].ReversesAtEnd);
            Step(state, .15, targets, hits, amount: 2);
            Step(state, .8, targets, hits, amount: 2);
            state = Restore(state);
            state.Validate(.8, false);
            var original = state.Paths[0];
            Check.True(original.TurnDirection.HasValue);
            Step(state, 1.399, targets, hits);
            Check.True(state.SupportChariots == null);
            Step(state, 1.4, targets, hits);
            Check.Equal(1, state.SupportChariots!.Count);
            var reversed = state.SupportChariots[0];
            Check.Equal((ExpandedElijahChariotSupportKind.Reverse, 5L, 5L), (reversed.Kind, reversed.SourcePathId, reversed.Cycle));
            Check.Equal(original.Endpoint, reversed.Origin);
            Check.Equal(-original.CurrentDirection, reversed.Direction);
            Check.Near(1.4, reversed.MoveAt);
            Check.Near(2.4, state.NextReverseAt!.Value);
            state = Restore(state);
            state.Validate(1.4, false);
            targets.Clear();
            targets[5] = reversed.Origin + reversed.Direction * 1.8f;
            hits.Clear();
            Step(state, 1.65, targets, hits);
            Step(state, 1.75, targets, hits);
            var hit = hits.Single(value => value.Kind == ExpandedElijahChariotHitKind.ReverseChariot);
            Check.Equal(5, hit.TargetId);
            Check.Near(28, hit.Damage);
            Check.Equal((0d, 0d, 0), (hit.OriginalBaseDamage, hit.Knockback, hit.TargetOrdinal));

            var cancelled = new ExpandedElijahChariotState { Cycle = 4, NextPathId = 5 };
            Step(cancelled, 0, targets, hits, ranks: ranks);
            Step(cancelled, .8, targets, hits, ranks: ranks);
            Step(cancelled, 1.4, targets, hits, evolved: true, reserveOriginal: () => false);
            Check.True(cancelled.SupportChariots == null && !cancelled.NextReverseAt.HasValue);
        });

        test("Chariot b.05 rotates every sixth final original relative to the first converged heading without adding one", () =>
        {
            var ranks = new ExpandedElijahChariotRankOptions(convergeOnBoss: true, crossFormation: true);
            var boss = new Vector2(3, 2);
            var targets = new Dictionary<int, Vector2> { [1] = boss };
            var hits = new List<ExpandedElijahChariotHit>();
            foreach (var prior in new[] { 4L, 5L })
            {
                var state = new ExpandedElijahChariotState { Cycle = prior, NextPathId = prior + 1 };
                Step(state, 0, targets, hits, amount: 3, ranks: ranks, isBoss: _ => true);
                state = Restore(state);
                state.Validate(0, false);
                var first = state.Paths[0];
                var middle = state.Paths[1];
                var last = state.Paths[2];
                Check.True(Vector2.Distance(boss, first.Endpoint) < 1e-5);
                Check.True(Vector2.Distance(boss, middle.Endpoint) < 1e-5);
                Check.Equal(3, state.Paths.Count);
                Check.True(state.SupportChariots == null);
                if (prior == 5)
                {
                    Check.Near(0, Vector2.Dot(first.Direction, last.Direction), 1e-6);
                    Check.True(Vector2.Dot(first.Origin - last.Origin, last.Direction) > 0);
                    Check.Near(Vector2.Distance(boss, last.Origin), last.Range, 1e-5);
                }
                else Check.True(Vector2.Distance(boss, last.Endpoint) < 1e-5);
            }
        });

        test("Chariot b.05 crosses the first noncoincident original instead of pointing the last path away", () =>
        {
            var ranks = new ExpandedElijahChariotRankOptions(crossFormation: true, whirlAtCrossing: true);
            var state = new ExpandedElijahChariotState { Cycle = 5, NextPathId = 6 };
            var targets = new Dictionary<int, Vector2>();
            var hits = new List<ExpandedElijahChariotHit>();
            var centers = new List<Vector2>();
            Step(state, 0, targets, hits, evolved: true, amount: 2, ranks: ranks);
            Check.Equal(Vector2.UnitX, state.Paths[0].Direction);
            Check.Equal(-Vector2.UnitY, state.Paths[1].Direction);
            Check.True(state.Paths[0].Origin != state.Paths[1].Origin);
            for (var tick = 1; tick <= 16; tick++)
                Step(state, tick / 60d, targets, hits, evolved: true, ranks: ranks,
                    emitCrossingWhirlwind: (center, _) => centers.Add(center));
            Check.Equal(state.Paths[0].Origin, centers.Single());
        });

        test("Chariot b.05 persists one separately reserved perpendicular child for a sole sixth-cycle original", () =>
        {
            var ranks = Rank("b.05");
            var state = new ExpandedElijahChariotState { Cycle = 5, NextPathId = 6 };
            var targets = new Dictionary<int, Vector2> { [1] = new Vector2(0, .8f) };
            var hits = new List<ExpandedElijahChariotHit>();
            var reservations = 0;
            Step(state, 0, targets, hits, ranks: ranks, reserveSupportShot: () => { reservations++; return true; });
            Check.Equal(1, reservations);
            Check.Equal(1, state.Paths.Count);
            Check.Equal(1, state.ActiveSupportProjectiles);
            state = Restore(state);
            state.Validate(0, false);
            var original = state.Paths[0];
            var child = state.SupportChariots!.Single();
            Check.True(original.HasCrossingChild);
            Check.Equal(Vector2.UnitX, original.Direction);
            Check.Equal(Vector2.UnitY, child.Direction);
            Check.Equal(original.Origin, child.Origin);
            Check.Near(original.MoveAt, child.MoveAt);
            Step(state, .149, targets, hits);
            Step(state, .15, targets, hits);
            Check.Equal(0, hits.Count);
            Step(state, .26, targets, hits);
            Step(state, .38, targets, hits);
            var hit = hits.Single();
            Check.Equal(ExpandedElijahChariotHitKind.CrossingChariot, hit.Kind);
            Check.Near(28, hit.Damage);
            Check.Equal((0d, 0), (hit.OriginalBaseDamage, hit.TargetOrdinal));
            state.Validate(.38, false);

            var suppressed = new ExpandedElijahChariotState { Cycle = 5, NextPathId = 6 };
            var rejected = 0;
            Step(suppressed, 0, targets, hits, ranks: ranks, reserveSupportShot: () => false,
                supportSuppressed: () => rejected++);
            Check.Equal(1, rejected);
            Check.Equal(1, suppressed.Paths.Count);
            Check.True(!suppressed.Paths[0].HasCrossingChild && !suppressed.NextCrossChildAt.HasValue);
            suppressed.Validate(0, false);

            var partial = new ExpandedElijahChariotState { Cycle = 5, NextPathId = 6 };
            var originalSlots = 0;
            Step(partial, 0, targets, hits, amount: 2, ranks: ranks, reserveOriginal: () => ++originalSlots <= 1);
            Check.Equal(1, partial.Paths.Count);
            Check.True(partial.SupportChariots == null);
        });

        test("Chariot b.05 generation clock skips a too-soon sixth cycle without suppressing its original", () =>
        {
            var ranks = Rank("b.05");
            var state = new ExpandedElijahChariotState { Cycle = 5, NextPathId = 6 };
            var targets = new Dictionary<int, Vector2>();
            var hits = new List<ExpandedElijahChariotHit>();
            var reservations = 0;
            Step(state, 0, targets, hits, cycle: .15, ranks: ranks, reserveSupportShot: () => { reservations++; return true; });
            for (var tick = 1; tick <= 108; tick++)
            {
                Step(state, tick / 60d, targets, hits, cycle: .15, ranks: ranks,
                    reserveSupportShot: () => { reservations++; return true; });
                if (tick == 54)
                {
                    Check.Equal(1, reservations);
                    Check.True(state.Paths.Any(path => path.Cycle == 12));
                    Check.True(state.SupportChariots!.All(support => support.Cycle != 12));
                }
            }
            Check.Equal(2, reservations);
            Check.True(state.Paths.Any(path => path.Cycle == 18 && path.HasCrossingChild));
        });

        test("Chariot a.04 resolves one bounded turn at its midpoint, chooses nearest-cluster ID ties and splits the deflection sweep", () =>
        {
            var ranks = ExpandedElijahChariotRankOptions.FromRanks(new Dictionary<string, int>
            {
                ["weapon.elijah_chariot.a.04"] = 1, ["weapon.elijah_chariot.d.01"] = 1,
                ["weapon.elijah_chariot.d.04"] = 1,
            });
            var targets = new Dictionary<int, Vector2>
            {
                [1] = new Vector2(5, 2), [2] = new Vector2(5, -2), [3] = new Vector2(5.2f, 2.2f),
                [4] = new Vector2(5.2f, -2.2f), [5] = new Vector2(3.9f, .7f),
            };
            var hits = new List<ExpandedElijahChariotHit>();
            var state = new ExpandedElijahChariotState();
            Step(state, 0, targets, hits, ranks: ranks);
            Step(state, .77, targets, hits, ranks: ranks);
            state = Restore(state);
            state.Validate(.77, false);
            Check.True(!state.Paths.Single().MidpointTurnResolved);
            var sweeps = new List<ExpandedElijahChariotSweep>();
            Step(state, .79, targets, hits, ranks: ranks, tryDeflect: sweep => { sweeps.Add(sweep); return false; });
            Check.Equal(2, sweeps.Count);
            Check.Equal(new Vector2(5, 0), sweeps[0].End);
            Check.Equal(sweeps[0].End, sweeps[1].Start);
            Check.Near(.775, sweeps[0].ToAt);
            Check.Near(.775, sweeps[1].FromAt);
            Check.Near(.79, sweeps[1].ToAt);
            var path = state.Paths.Single();
            Check.True(path.MidpointTurnResolved);
            var turn = path.TurnDirection!.Value;
            Check.Near(12, Math.Atan2(turn.Y, turn.X) * 180 / Math.PI, 1e-4);
            Check.Equal(sweeps[1].End, path.Position);
            Check.Equal(turn, path.CurrentDirection);
            state = Restore(state);
            state.Validate(.79, false);
            var invalid = state.Copy();
            invalid.Paths[0].TurnDirectionY = null;
            Check.Throws<ArgumentException>(() => invalid.Validate(.79, false));

            targets.Clear();
            targets[6] = state.Paths[0].PositionAtDistance(8.5);
            targets[7] = new Vector2(8.5f, -.1f);
            Step(state, 1.25, targets, hits, ranks: ranks);
            var body = hits.Single(hit => hit.Kind == ExpandedElijahChariotHitKind.Body);
            Check.Equal(6, body.TargetId);
            Check.Equal(turn, body.Direction);
            Check.Near(.30, body.Knockback);
            Check.True(state.Paths[0].Endpoint.Y > 1);
        });

        test("Chariot a.04 leaves a singleton route straight and freezes the decision through save and later crowd changes", () =>
        {
            var ranks = Rank("a.04");
            var targets = new Dictionary<int, Vector2> { [1] = new Vector2(5, 2) };
            var hits = new List<ExpandedElijahChariotHit>();
            var state = new ExpandedElijahChariotState();
            Step(state, 0, targets, hits, ranks: ranks);
            Step(state, .775, targets, hits);
            state = Restore(state);
            state.Validate(.775, false);
            Check.True(state.Paths.Single().MidpointTurnResolved);
            Check.True(!state.Paths[0].TurnDirection.HasValue);
            targets[2] = new Vector2(5.1f, 2);
            Step(state, .85, targets, hits);
            Check.True(!state.Paths[0].TurnDirection.HasValue);
            Check.Equal(new Vector2(10, 0), state.Paths[0].Endpoint);
        });

        test("Chariot a.04 bends the timed wind, residual strip and actual endpoint together after restore", () =>
        {
            var ranks = new ExpandedElijahChariotRankOptions(turnMidway: true, followWithWind: true,
                leaveFireTrail: true, explodeAtEnd: true);
            var targets = new Dictionary<int, Vector2> { [1] = new Vector2(5, 2), [2] = new Vector2(5.2f, 2.2f) };
            var hits = new List<ExpandedElijahChariotHit>();
            var state = new ExpandedElijahChariotState();
            Step(state, 0, targets, hits, ranks: ranks);
            Step(state, .15, targets, hits);
            Step(state, .8, targets, hits);
            state = Restore(state);
            state.Validate(.8, false);
            var path = state.Paths.Single();
            var wind = state.Winds!.Single();
            var turn = path.TurnDirection!.Value;
            Check.Equal(turn, wind.TurnDirection!.Value);
            Check.Equal(Vector2.UnitX, wind.CurrentDirection);
            Check.Equal(path.Endpoint, wind.Endpoint);
            targets.Clear();
            targets[5] = path.PositionAtDistance(8.5);
            targets[6] = new Vector2(8.5f, -.1f);
            var end = Vector2.Zero;
            for (var tick = 49; tick <= 103; tick++)
                Step(state, tick / 60d, targets, hits, emitEndExplosion: (center, _) => end = center);
            Check.Equal(path.Endpoint, end);
            Check.Equal(turn, wind.CurrentDirection);
            Check.True(hits.Any(hit => hit.Kind == ExpandedElijahChariotHitKind.Trail && hit.TargetId == 5));
            Check.True(hits.Any(hit => hit.Kind == ExpandedElijahChariotHitKind.FollowingWind && hit.TargetId == 5));
            Check.True(!hits.Any(hit => hit.TargetId == 6 && hit.Kind is ExpandedElijahChariotHitKind.Trail or ExpandedElijahChariotHitKind.FollowingWind));
        });

        test("Chariot a.04 and e.03 intersect only the true timed bent second section", () =>
        {
            var ranks = new ExpandedElijahChariotRankOptions(turnMidway: true, whirlAtCrossing: true);
            var targets = new Dictionary<int, Vector2> { [1] = new Vector2(5, 2), [2] = new Vector2(5.2f, 2.2f) };
            var hits = new List<ExpandedElijahChariotHit>();
            var state = new ExpandedElijahChariotState();
            var crossings = new List<Vector2>();
            Step(state, 0, targets, hits, evolved: true, cycle: .5, ranks: ranks);
            for (var tick = 1; tick <= 75; tick++)
            {
                var now = tick / 60d;
                Step(state, now, targets, hits, evolved: true, cycle: .5, player: new Vector2(8, -3.2f),
                    facing: Vector2.UnitY, ranks: ranks, reserveOriginal: () => tick == 30,
                    emitCrossingWhirlwind: (center, _) => crossings.Add(center));
                state.Validate(now, true);
            }
            var point = crossings.Single();
            Check.Near(8, point.X, 1e-6);
            Check.Near(Math.Tan(Math.PI / 15) * 3, point.Y, 1e-6);
        });

        test("Chariot e.03 uses two timed swept originals and keeps ended geometry without reserving slots", () =>
        {
            var ranks = new ExpandedElijahChariotRankOptions(whirlAtCrossing: true,
                widenSupportShape: true, widenDamageRegion: true);
            var targets = new Dictionary<int, Vector2>();
            var hits = new List<ExpandedElijahChariotHit>();
            var centers = new List<(Vector2 Center, double Radius)>();
            var state = new ExpandedElijahChariotState();
            var second = new Vector2(1.6f, -.4f);
            Step(state, 0, targets, hits, evolved: true, cycle: .15, ranks: ranks, reserveTrail: () => false);
            for (var tick = 1; tick <= 19; tick++)
                Step(state, tick / 60d, targets, hits, evolved: true, cycle: .15, ranks: ranks,
                    player: second, facing: Vector2.UnitY, reserveOriginal: () => tick == 9, reserveTrail: () => false);
            state = Restore(state);
            state.Validate(19 / 60d, true);
            Check.Equal(2, state.Paths.Count);
            Check.Equal(0, state.ActiveSupportRegions);
            targets[1] = new Vector2(1.6f, 1.09f);
            targets[2] = new Vector2(1.6f, 1.11f);
            for (var tick = 20; tick <= 22; tick++)
                Step(state, tick / 60d, targets, hits, evolved: true, cycle: .15, ranks: ranks,
                    reserveOriginal: () => false, emitCrossingWhirlwind: (center, radius) => centers.Add((center, radius)));
            Check.Equal(1, centers.Count);
            Check.Near(1.6, centers[0].Center.X, 1e-6);
            Check.Near(0, centers[0].Center.Y, 1e-6);
            Check.Near(1.1, centers[0].Radius);
            Check.Near(3.35, state.NextCrossingAt!.Value);
            var whirl = hits.Single(hit => hit.Kind == ExpandedElijahChariotHitKind.CrossingWhirlwind);
            Check.Equal(1, whirl.TargetId);
            Check.Near(32, whirl.Damage);
            for (var tick = 23; tick <= 99; tick++)
            {
                var now = tick / 60d;
                Step(state, now, targets, hits, evolved: true, cycle: .15, ranks: ranks, reserveOriginal: () => false);
                state.Validate(now, true);
                if (tick == 84)
                {
                    Check.True(state.Paths[0].BodyEnded && state.Paths[0].TracksCrossing);
                    Check.Equal(1, BattleWorld.CountOriginalProjectiles(0, null, null, state));
                    Check.Equal(0, state.ActiveSupportRegionsAt(now));
                }
            }
            Check.True(state.Paths.All(path => path.Id != 1));
        });

        test("Chariot e.03 can strike against a completed trajectory without a living trail reservation", () =>
        {
            var ranks = Rank("e.03");
            var targets = new Dictionary<int, Vector2>();
            var hits = new List<ExpandedElijahChariotHit>();
            var state = new ExpandedElijahChariotState();
            var centers = new List<Vector2>();
            Step(state, 0, targets, hits, evolved: true, cycle: .5, ranks: ranks, reserveTrail: () => false);
            for (var tick = 1; tick <= 93; tick++)
            {
                var now = tick / 60d;
                Step(state, now, targets, hits, evolved: true, cycle: .5, ranks: ranks,
                    reserveOriginal: () => tick == 30, reserveTrail: () => tick > 30,
                    player: new Vector2(10, -7.2f), facing: Vector2.UnitY,
                    emitCrossingWhirlwind: (center, _) => centers.Add(center));
                if (tick == 90)
                {
                    state = Restore(state);
                    state.Validate(now, true);
                    Check.True(state.Paths[0].BodyEnded);
                    Check.Equal(0, state.ActiveSupportRegionsAt(now));
                    Check.Equal(0, centers.Count);
                }
            }
            Check.Equal(new Vector2(10, 0), centers.Single());
            Check.Near(4.55, state.NextCrossingAt!.Value);
        });

        test("Chariot e.03 includes the exact quarter-second crossing boundary", () =>
        {
            var ranks = Rank("e.03");
            var targets = new Dictionary<int, Vector2>();
            var hits = new List<ExpandedElijahChariotHit>();
            foreach (var (side, expected) in new[] { (-.999f, true), (-1f, true), (-1.001f, false) })
            {
                var state = new ExpandedElijahChariotState();
                Step(state, 0, targets, hits, evolved: true, cycle: .25, ranks: ranks);
                for (var tick = 1; tick <= 36; tick++)
                    Step(state, tick / 60d, targets, hits, evolved: true, cycle: .25, ranks: ranks,
                        reserveOriginal: () => tick == 15, player: new Vector2(1, side), facing: Vector2.UnitY);
                Check.Equal(expected, state.NextCrossingAt.HasValue);
            }
        });

        test("Chariot e.04 matches the natural endpoint and resolves overlapping c.05 inner and e.04 outer once", () =>
        {
            var ranks = new ExpandedElijahChariotRankOptions(explodeAtEnd: true, rewardBodyHits: true,
                flameAtNaturalEnd: true, widenSupportShape: true, widenDamageRegion: true);
            var targets = Enumerable.Range(1, 6).ToDictionary(id => id, id => new Vector2(id * .5f, 0));
            targets[50] = new Vector2(10, 1.05f);
            targets[60] = new Vector2(10, 1.35f);
            targets[70] = new Vector2(10, 1.44f);
            var hits = new List<ExpandedElijahChariotHit>();
            var state = new ExpandedElijahChariotState();
            Step(state, 0, targets, hits, evolved: true, ranks: ranks);
            Step(state, 1.399, targets, hits, evolved: true);
            Check.True(!state.NextEndExplosionAt.HasValue && !state.NextEndFlameAt.HasValue);
            var centers = new List<(Vector2 Center, double Radius)>();
            Step(state, 1.4, targets, hits, evolved: true,
                emitEndExplosion: (center, radius) => centers.Add((center, radius)),
                emitEndFlame: (center, radius) => centers.Add((center, radius)));
            Check.Equal(2, centers.Count);
            Check.True(centers.All(shape => shape.Center == new Vector2(10, 0)));
            Check.Near(1.1, centers[0].Radius);
            Check.Near(1.43, centers[1].Radius);
            Check.Equal(2, hits.Count(hit => hit.Kind is ExpandedElijahChariotHitKind.EndExplosion or ExpandedElijahChariotHitKind.EndFlame));
            var explosion = hits.Single(hit => hit.Kind == ExpandedElijahChariotHitKind.EndExplosion);
            Check.Equal(50, explosion.TargetId);
            Check.Near(40, explosion.Damage);
            var flame = hits.Single(hit => hit.Kind == ExpandedElijahChariotHitKind.EndFlame);
            Check.Equal(60, flame.TargetId);
            Check.Near(32, flame.Damage);
            state = Restore(state);
            state.Validate(1.4, true);
            Check.Near(4.4, state.NextEndFlameAt!.Value);
        });

        test("Chariot e.05 saves one extra formation after an accepted elite original kill through the inclusive six-second deadline", () =>
        {
            var ranks = Rank("e.05");
            bool IsElite(int id) => id == 1;
            ExpandedElijahChariotState Latch(bool kill, bool evolved = true)
            {
                var newState = new ExpandedElijahChariotState();
                var actors = new Dictionary<int, Vector2> { [1] = new Vector2(.2f, -.45f) };
                var recorded = new List<ExpandedElijahChariotHit>();
                Step(newState, 0, actors, recorded, evolved: evolved, amount: 2, ranks: ranks, isElite: IsElite);
                Step(newState, .2, actors, recorded, evolved: evolved, amount: 2, ranks: ranks, isElite: IsElite,
                    applyOriginalHit: hit =>
                    {
                        if (kill) actors.Remove(hit.TargetId);
                        return 80;
                    });
                return Restore(newState);
            }

            Check.True(!Latch(false).ProcessionUntil.HasValue);
            Check.True(!Latch(true, false).ProcessionUntil.HasValue);
            var before = Latch(true);
            before.Validate(.2, true);
            Check.Near(6.2, before.ProcessionUntil!.Value);
            Check.Near(6.2, before.NextProcessionAt!.Value);
            var noTargets = new Dictionary<int, Vector2>();
            var hits = new List<ExpandedElijahChariotHit>();
            var early = before.Copy();
            early.NextCycle = 6.2;
            Step(early, 6.2, noTargets, hits, evolved: true, amount: 2, ranks: ranks, isElite: IsElite);
            Check.Equal(3, early.Paths.Count);
            Check.True(!early.ProcessionUntil.HasValue);
            Step(early, 11.2, noTargets, hits, evolved: true, amount: 2, ranks: ranks, isElite: IsElite);
            Check.Equal(2, early.Paths.Count);

            var late = before.Copy();
            late.NextCycle = 6.2;
            Step(late, 6.201, noTargets, hits, evolved: true, amount: 2, ranks: ranks, isElite: IsElite);
            Check.Equal(2, late.Paths.Count);
            var denied = before.Copy();
            Step(denied, 5, noTargets, hits, evolved: true, amount: 2, ranks: ranks, isElite: IsElite,
                reserveOriginal: () => false);
            Check.Equal(0, denied.Paths.Count);
            Check.Near(6.2, denied.ProcessionUntil!.Value);
            var partial = before.Copy();
            var reservations = 0;
            Step(partial, 5, noTargets, hits, evolved: true, amount: 2, ranks: ranks, isElite: IsElite,
                reserveOriginal: () => ++reservations <= 2);
            Check.Equal(2, partial.Paths.Count);
            Check.Equal(1, partial.SuppressedOriginals);
            Check.True(!partial.ProcessionUntil.HasValue);
            var capped = before.Copy();
            Step(capped, 5, noTargets, hits, evolved: true, amount: 4, ranks: ranks, isElite: IsElite);
            Check.Equal(4, capped.Paths.Count);
            Check.True(!capped.ProcessionUntil.HasValue);
        });

        test("Chariot d.02 and d.03 follow the frozen path after half a second and cap each target to one contact", () =>
        {
            foreach (var widened in new[] { false, true })
            {
                var ranks = new ExpandedElijahChariotRankOptions(followWithWind: true, widenFollowingWind: widened,
                    widenSupportShape: widened, widenDamageRegion: widened);
                var targets = new Dictionary<int, Vector2>
                {
                    [1] = new Vector2(0, .29f), [2] = new Vector2(0, .371f), [3] = new Vector2(0, .373f),
                    [4] = new Vector2(9.9f, .29f),
                };
                var hits = new List<ExpandedElijahChariotHit>();
                var state = new ExpandedElijahChariotState();
                Step(state, 0, targets, hits, ranks: ranks);
                state = Restore(state);
                Step(state, .149, targets, hits, player: new Vector2(100, 100));
                Check.Equal(0, state.ActiveSupportProjectiles);
                Step(state, .15, targets, hits);
                Check.Equal(1, state.ActiveSupportProjectiles);
                var wind = state.Winds!.Single();
                Check.Equal(Vector2.Zero, wind.Origin);
                Check.Equal(Vector2.UnitX, wind.Direction);
                Check.Near(.65, wind.MoveAt);
                Check.Near(widened ? .744 : .6, wind.Width);
                state = Restore(state);
                state.Validate(.15, false);
                Step(state, .649, targets, hits);
                Check.Equal(0, hits.Count(hit => hit.Kind == ExpandedElijahChariotHitKind.FollowingWind));
                Step(state, .65, targets, hits);
                Step(state, .7, targets, hits);
                state = Restore(state);
                state.Validate(.7, false);
                Check.True(hits.Where(hit => hit.Kind == ExpandedElijahChariotHitKind.FollowingWind).Select(hit => hit.TargetId)
                    .SequenceEqual(widened ? new[] { 1, 2 } : new[] { 1 }));
                Step(state, 1.4, targets, hits);
                Check.Equal(0, state.Paths.Count);
                Check.Equal(1, state.ActiveSupportProjectiles);
                Step(state, 1.9, targets, hits);
                Check.True(hits.Any(hit => hit.Kind == ExpandedElijahChariotHitKind.FollowingWind && hit.TargetId == 4));
                Check.True(hits.Where(hit => hit.Kind == ExpandedElijahChariotHitKind.FollowingWind).All(hit =>
                    hit.Damage == 20 && hit.OriginalBaseDamage == 0 && hit.TargetOrdinal == 0));
                Check.Equal(0, state.ActiveSupportProjectiles);
            }
        });

        test("Chariot d.05 guarantees one enhanced fifth-cycle wind despite the ordinary two-second clock", () =>
        {
            var ranks = new ExpandedElijahChariotRankOptions(followWithWind: true, guaranteeFollowingWind: true);
            var state = new ExpandedElijahChariotState();
            var targets = new Dictionary<int, Vector2>();
            var hits = new List<ExpandedElijahChariotHit>();
            Step(state, 0, targets, hits, cycle: .15, amount: 2, ranks: ranks);
            for (var tick = 1; tick <= 45; tick++)
                Step(state, tick / 60d, targets, hits, cycle: .15, amount: 2, ranks: ranks);
            state = Restore(state);
            state.Validate(.75, false);
            Check.Equal(2, state.ActiveSupportProjectiles);
            Check.Equal(1, state.Winds!.Count(wind => wind.Guaranteed));
            var guaranteed = state.Winds!.Single(wind => wind.Guaranteed);
            Check.Equal(5L, guaranteed.Cycle);
            Check.Equal(9L, guaranteed.SourcePathId);
            Check.Near(32, guaranteed.Damage);
            Check.Near(1.75, state.NextGuaranteedWindAt!.Value);
            Check.Near(2.75, state.NextWindAt!.Value);
            for (var tick = 46; tick <= 92; tick++)
                Step(state, tick / 60d, targets, hits, cycle: .15, amount: 2, ranks: ranks);
            Check.Equal(2, state.ActiveSupportProjectiles);
            Check.True(state.Winds!.All(wind => wind.Cycle < 10));

            var denied = new ExpandedElijahChariotState();
            var suppressions = 0;
            Step(denied, 0, targets, hits, ranks: ranks);
            Step(denied, .15, targets, hits, reserveSupportShot: () => false, supportSuppressed: () => suppressions++);
            Step(denied, .3, targets, hits, reserveSupportShot: () => true, supportSuppressed: () => suppressions++);
            Check.Equal(1, suppressions);
            Check.Equal(0, denied.ActiveSupportProjectiles);
            Check.True(!denied.NextWindAt.HasValue);
        });

        test("Chariot d.04 offers real moving body sweeps and spends its two-second receipt only on positive removal", () =>
        {
            var ranks = Rank("d.04");
            var state = new ExpandedElijahChariotState();
            var targets = new Dictionary<int, Vector2>();
            var hits = new List<ExpandedElijahChariotHit>();
            var candidates = new List<ExpandedElijahChariotSweep>();
            bool TryDeflect(ExpandedElijahChariotSweep sweep)
            {
                candidates.Add(sweep);
                return candidates.Count == 2;
            }
            Step(state, 0, targets, hits, cycle: .5, ranks: ranks, tryDeflect: TryDeflect);
            Step(state, .15, targets, hits, cycle: .5, ranks: ranks, tryDeflect: TryDeflect);
            Check.Equal(0, candidates.Count);
            Step(state, .2, targets, hits, cycle: .5, ranks: ranks, tryDeflect: TryDeflect);
            Check.Equal(1, candidates.Count);
            Check.True(!state.NextDeflectAt.HasValue);
            Check.Equal(Vector2.Zero, candidates[0].Start);
            Check.Near(.4, candidates[0].End.X, 1e-6);
            Check.Near(.15, candidates[0].FromAt);
            Check.Near(.2, candidates[0].ToAt);
            Check.Near(1.2, candidates[0].Width);
            Step(state, .3, targets, hits, cycle: .5, ranks: ranks, tryDeflect: TryDeflect);
            Check.Near(2.3, state.NextDeflectAt!.Value);
            Check.Equal((1L, 1L), (candidates[1].PathId, candidates[1].Cycle));
            state = Restore(state);
            state.Validate(.3, false);
            for (var tick = 19; tick <= 137; tick++)
                Step(state, tick / 60d, targets, hits, cycle: .5, ranks: ranks, tryDeflect: TryDeflect);
            Check.Equal(2, candidates.Count);
            Step(state, 2.3, targets, hits, cycle: .5, ranks: ranks, tryDeflect: TryDeflect);
            Check.True(candidates.Count > 2);
            Check.True(!state.NextDeflectAt.HasValue);
        });

        test("Chariot c.01 restores a speed-six spark targeted at the closest different actor and uses pre-critical damage", () =>
        {
            var ranks = Rank("c.01");
            var targets = new Dictionary<int, Vector2>
            {
                [1] = new Vector2(.2f, 0), [3] = new Vector2(.2f, -2.5f),
                [2] = new Vector2(.2f, 2.5f), [4] = new Vector2(.2f, 2.5001f),
            };
            var hits = new List<ExpandedElijahChariotHit>();
            var state = new ExpandedElijahChariotState();
            var originals = 0;
            double? Resolve(ExpandedElijahChariotHit hit)
            {
                Check.Equal(1, hit.TargetId);
                originals++;
                return 100;
            }
            Step(state, 0, targets, hits, ranks: ranks);
            Step(state, .2, targets, hits, ranks: ranks, applyOriginalHit: Resolve);
            Check.Equal(1, originals);
            Check.Equal(1, state.ActiveSupportProjectiles);
            Check.Equal(2, state.Spark!.TargetId);
            Check.Equal(new Vector2(.2f, 0), state.Spark.Position);
            Check.Near(1.2, state.NextSparkAt!.Value);
            for (var tick = 13; tick <= 18; tick++)
                Step(state, tick / 60d, targets, hits, ranks: ranks, applyOriginalHit: Resolve);
            state = Restore(state);
            state.Validate(.3, false);
            Check.Near(.6, state.Spark!.Position.Y, 1e-6);
            for (var tick = 19; tick <= 36; tick++)
                Step(state, tick / 60d, targets, hits, ranks: ranks, applyOriginalHit: Resolve);
            Check.Equal(0, hits.Count);
            Step(state, 37 / 60d, targets, hits, ranks: ranks, applyOriginalHit: Resolve);
            var spark = hits.Single();
            Check.Equal(ExpandedElijahChariotHitKind.Spark, spark.Kind);
            Check.Equal(2, spark.TargetId);
            Check.Near(25, spark.Damage);
            Check.Equal((0d, 0), (spark.OriginalBaseDamage, spark.TargetOrdinal));
            Check.Equal(0, state.ActiveSupportProjectiles);
            state.Validate(37 / 60d, false);

            var denied = new ExpandedElijahChariotState();
            var suppressed = 0;
            Step(denied, 0, targets, hits, ranks: ranks);
            Step(denied, .2, targets, hits, ranks: ranks, applyOriginalHit: Resolve, reserveSupportShot: () => false,
                supportSuppressed: () => suppressed++);
            Check.Equal(1, suppressed);
            Check.Equal(0, denied.ActiveSupportProjectiles);
            Check.True(!denied.NextSparkAt.HasValue);
            var rejected = new ExpandedElijahChariotState();
            Step(rejected, 0, targets, hits, ranks: ranks);
            Step(rejected, .2, targets, hits, ranks: ranks, applyOriginalHit: _ => null);
            Check.Equal(0, rejected.Paths.Single().BodyHitCount);
            Check.True(rejected.Spark == null);
        });

        test("Chariot c.02 anchors a widened departure fire to each reserved origin and ticks the overlap once", () =>
        {
            var ranks = ExpandedElijahChariotRankOptions.FromRanks(new Dictionary<string, int>
            {
                ["weapon.elijah_chariot.c.02"] = 1,
                ["common.area.01"] = 1, ["common.area.09"] = 1,
            });
            var targets = new Dictionary<int, Vector2>
            {
                [1] = new Vector2(-.72f, 0), [2] = new Vector2(-.8f, 0),
            };
            var hits = new List<ExpandedElijahChariotHit>();
            var state = new ExpandedElijahChariotState();
            Step(state, 0, targets, hits, ranks: ranks, amount: 2);
            Check.Equal(2, state.ActiveSupportRegions);
            Check.True(state.Paths.All(path => path.MakesOriginFire));
            Step(state, .149, targets, hits, amount: 2);
            Check.Equal(0, hits.Count);
            Step(state, .15, targets, hits, amount: 2, player: new Vector2(30, 30));
            var first = hits.Single();
            Check.Equal((ExpandedElijahChariotHitKind.OriginFire, 1, 1L), (first.Kind, first.TargetId, first.PathId));
            Check.Near(20, first.Damage);
            state = Restore(state);
            state.Validate(.15, false);
            Check.Equal(1, state.Paths[0].OriginFireTicks);
            Check.Near(.88, state.Paths[0].OriginFireRadius);
            Step(state, .649, targets, hits);
            Check.Equal(1, hits.Count);
            Step(state, .65, targets, hits);
            Step(state, 1.15, targets, hits);
            Check.Equal(3, hits.Count);
            Step(state, 1.65, targets, hits);
            Check.Equal(3, hits.Count);
            Check.Equal(0, state.ActiveSupportRegions);

            var capacity = new ExpandedElijahChariotState();
            var denied = 0;
            Step(capacity, 0, targets, hits, ranks: ranks, amount: 2,
                reserveTrail: () => capacity.ActiveSupportRegions < 1, supportSuppressed: () => denied++);
            Check.Equal(1, denied);
            Check.True(capacity.Paths[0].MakesOriginFire && !capacity.Paths[1].MakesOriginFire);

            var evolvedCapacity = new ExpandedElijahChariotState();
            Step(evolvedCapacity, 0, targets, hits, evolved: true, ranks: ranks,
                reserveTrail: () => evolvedCapacity.ActiveSupportRegions < 1, supportSuppressed: () => denied++);
            Check.Equal(2, denied);
            var firstPath = evolvedCapacity.Paths.Single();
            Check.True(!firstPath.SuppressedTrail && !firstPath.MakesOriginFire);
            Check.Equal(1, evolvedCapacity.ActiveSupportRegions);
            evolvedCapacity.Validate(0, true);
        });

        test("Chariot c.03 only explodes on natural arrival, and c.05 rewards the sixth accepted body contact", () =>
        {
            var options = ExpandedElijahChariotRankOptions.FromRanks(new Dictionary<string, int>
            {
                ["weapon.elijah_chariot.c.03"] = 1,
                ["weapon.elijah_chariot.c.05"] = 1,
                ["common.area.01"] = 1, ["common.area.09"] = 1,
            });
            foreach (var count in new[] { 5, 6 })
            {
                var targets = Enumerable.Range(1, count).ToDictionary(id => id, id => new Vector2(id * .5f, 0));
                targets[50] = new Vector2(10, 1.09f);
                targets[60] = new Vector2(10, 1.11f);
                var hits = new List<ExpandedElijahChariotHit>();
                var state = new ExpandedElijahChariotState();
                Step(state, 0, targets, hits, cycle: .5, ranks: options);
                for (var tick = 1; tick <= 34; tick++)
                    Step(state, tick / 60d, targets, hits, cycle: .5, ranks: options);
                Check.Equal(count, state.Paths[0].BodyHitCount);
                foreach (var id in Enumerable.Range(1, count)) targets.Remove(id);
                state = Restore(state);
                state.Validate(34 / 60d, false);
                var centers = new List<(Vector2 Center, double Radius)>();
                for (var tick = 35; tick <= 115; tick++)
                    Step(state, tick / 60d, targets, hits, cycle: .5, ranks: options, reserveOriginal: () => false,
                        emitEndExplosion: (center, radius) => centers.Add((center, radius)));
                Check.Equal(1, centers.Count);
                Check.Equal(new Vector2(10, 0), centers[0].Center);
                Check.Near(1.1, centers[0].Radius);
                var burst = hits.Single(hit => hit.Kind == ExpandedElijahChariotHitKind.EndExplosion);
                Check.Equal(50, burst.TargetId);
                Check.Near(count == 6 ? 40 : 28, burst.Damage);
                Check.Near(3.4, state.NextEndExplosionAt!.Value);
            }

            var reset = new ExpandedElijahChariotState();
            var empty = new Dictionary<int, Vector2>();
            var resetHits = new List<ExpandedElijahChariotHit>();
            Step(reset, 0, empty, resetHits, ranks: options);
            Step(reset, .2, empty, resetHits, ranks: options);
            var unexpected = 0;
            Step(reset, 1.4, empty, resetHits, evolved: true, reserveOriginal: () => false,
                emitEndExplosion: (_, _) => unexpected++);
            Check.Equal(0, unexpected);
            Check.True(!reset.NextEndExplosionAt.HasValue);
        });

        test("Chariot c.04 leaves a narrow basic strip and widens the existing evolved trail additively", () =>
        {
            var ranks = ExpandedElijahChariotRankOptions.FromRanks(new Dictionary<string, int>
            {
                ["weapon.elijah_chariot.c.04"] = 1,
                ["common.area.01"] = 1, ["common.area.09"] = 1,
                ["common.duration.02"] = 1, ["common.duration.04"] = 1,
            });
            foreach (var evolved in new[] { false, true })
            {
                var targets = new Dictionary<int, Vector2>
                {
                    [1] = new Vector2(2, evolved ? .561f : .384f),
                    [2] = new Vector2(2, evolved ? .564f : .386f),
                };
                var state = new ExpandedElijahChariotState();
                var hits = new List<ExpandedElijahChariotHit>();
                Step(state, 0, targets, hits, evolved: evolved, ranks: ranks);
                state = Restore(state);
                state.Validate(0, evolved);
                Check.Equal(1, state.ActiveSupportRegions);
                Check.Near(evolved ? 1.125 : .77, state.Paths[0].TrailWidth);
                Check.Near(evolved ? 1.665 : 1.05, state.Paths[0].TrailDuration);
                Step(state, .15, targets, hits, evolved: evolved);
                Step(state, .65, targets, hits, evolved: evolved);
                var trail = hits.Single(hit => hit.Kind == ExpandedElijahChariotHitKind.Trail);
                Check.Equal(1, trail.TargetId);
                Check.Near(evolved ? 28 : 16, trail.Damage);
            }
        });

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

        test("Chariot preserves body hit order and level damage through saved path while support carries no power metadata", () =>
        {
            var state = new ExpandedElijahChariotState();
            var targets = new Dictionary<int, Vector2> { [1] = new Vector2(.2f, 0), [2] = new Vector2(1, 0) };
            var hits = new List<ExpandedElijahChariotHit>();
            Step(state, 0, targets, hits, level: 1, damage: 44);
            Step(state, .2, targets, hits, level: 1, damage: 44);
            Check.Equal((40d, 1), (hits.Single().OriginalBaseDamage, hits.Single().TargetOrdinal));
            targets.Remove(1);
            var restored = Restore(state);
            restored.Validate(.2, false);
            var corrupt = restored.Copy();
            corrupt.Paths.Single().OriginalBaseDamage = 50;
            Check.Throws<ArgumentException>(() => corrupt.Validate(.2, false));
            Step(restored, .3, targets, hits, level: 8);
            Check.Equal((2, 40d, 2), (hits.Last().TargetId, hits.Last().OriginalBaseDamage, hits.Last().TargetOrdinal));

            targets[1] = Vector2.Zero;
            targets[3] = new Vector2(2, 1);
            hits.Clear();
            var evolved = new ExpandedElijahChariotState();
            Step(evolved, 0, targets, hits, evolved: true);
            Step(evolved, .15, targets, hits, evolved: true);
            Step(evolved, 1.4, targets, hits, evolved: true);
            var support = hits.Where(hit => hit.Kind != ExpandedElijahChariotHitKind.Body).ToArray();
            Check.True(support.Any(hit => hit.Kind == ExpandedElijahChariotHitKind.Trail));
            Check.True(support.Any(hit => hit.Kind == ExpandedElijahChariotHitKind.Whirlwind));
            Check.True(support.All(hit => hit.OriginalBaseDamage == 0 && hit.TargetOrdinal == 0));
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
        int level = 8, Func<bool>? reserveOriginal = null, Func<bool>? reserveTrail = null,
        Func<bool>? reserveSupportShot = null, Action? supportSuppressed = null,
        Action<Vector2, double>? emitEndExplosion = null, Func<ExpandedElijahChariotHit, double?>? applyOriginalHit = null,
        Func<ExpandedElijahChariotSweep, bool>? tryDeflect = null, Action<Vector2, double>? emitCrossingWhirlwind = null,
        Action<Vector2, double>? emitEndFlame = null)
    {
        ExpandedElijahChariot.Step(state, time, player, facing ?? Vector2.UnitX, level, evolved,
            damage, cycle, amount, width: 1.2, speed: 8, range: 10, () => targets,
            hit => { hits.Add(hit); return true; }, delayAdvance, reserveOriginal: reserveOriginal,
            rankOptions: ranks, isElite: isElite, isBoss: isBoss, reserveTrail: reserveTrail,
            reserveSupportShot: reserveSupportShot, recordSupportSuppression: supportSuppressed,
            emitEndExplosion: emitEndExplosion, applyOriginalHit: applyOriginalHit, tryDeflect: tryDeflect,
            emitCrossingWhirlwind: emitCrossingWhirlwind, emitEndFlame: emitEndFlame);
    }
}
