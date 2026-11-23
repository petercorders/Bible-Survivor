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
        test("Common duration Cross persists its longer return timeout but still catches at actual fixed travel", () =>
        {
            var rules = PermanentDurationRules.FromRanks(new Dictionary<string, int> { ["common.duration.09"] = 1 });
            var state = new ExpandedCrossState();
            var targets = new Dictionary<int, Vector2>();
            void Tick(double now, Vector2 player, PermanentDurationRules selected = default) =>
                ExpandedCross.Step(state, now, player, Vector2.UnitX, 8, false, 30, 1.5, 1, .28, 5, 6,
                    () => targets, targets, _ => true, reserveOriginal: () => state.Cycle == 1, durationRules: selected);

            Tick(0, Vector2.Zero, rules);
            var distant = new Vector2(100, 0);
            Tick(.85, distant);
            state = RoundTrip(state);
            Check.True(state.Shots.Single().LongerReturnLifetime);
            Check.Near(3.24, state.Shots.Single().ReturnLifetime);
            Tick(3.84, distant);
            state.Validate(3.84, false);
            Check.Equal(1, state.Shots.Count);
            var expiry = 5 / 6d + 3.24;
            Tick(expiry - .0001, distant);
            Check.Equal(1, state.Shots.Count);
            Tick(expiry, distant);
            Check.Equal(0, state.Shots.Count);

            state = new ExpandedCrossState();
            Tick(0, Vector2.Zero, rules);
            Tick(1.62, Vector2.Zero);
            Check.Equal(0, state.Shots.Count);
        });

        test("Common projectile Cross restores separate launch speed, radius, return speed and evolution budget", () =>
        {
            var rules = SharedShots("05", "06", "09", "10");
            var state = new ExpandedCrossState { Evolved = true };
            var targets = new Dictionary<int, Vector2>();
            ExpandedCross.Step(state, 0, Vector2.Zero, Vector2.UnitX, 8, true, 30, 1.5, 2, .35, 5, 7.5,
                () => targets, targets, _ => true,
                rankOptions: ExpandedCrossRankOptions.FromRanks(new Dictionary<string,int> { ["weapon.cross.b.01"] = 1 }),
                shotRules: rules);
            state = RoundTrip(state);
            state.Validate(0, true);
            Check.Near(8.1, state.Shots[0].OutboundSpeed);
            Check.Near(7.5, state.Shots[1].OutboundSpeed);
            Check.Near(.35, state.Shots[0].Radius);
            Check.Near(.3668, state.Shots[1].Radius);
            Check.Near(9.75, state.Shots[0].ReturnSpeed);
            Check.Near(9.15, state.Shots[1].ReturnSpeed);
            Check.True(state.Shots.All(shot => shot.IncreasedEvolvedLegBudget && shot.HitBudget(true) == 7 && shot.ReturnBudget(true) == 7));

            state = new ExpandedCrossState { Evolved = true };
            var otherRanks = ExpandedCrossRankOptions.FromRanks(new Dictionary<string, int>
                { ["weapon.cross.c.01"] = 1, ["common.shot.03"] = 1 });
            ExpandedCross.Step(state, 0, Vector2.Zero, Vector2.UnitX, 8, true, 30, 1.5, 1, .35, 5, 7.5,
                () => targets, targets, _ => true, rankOptions: otherRanks, shotRules: rules);
            Check.True(!state.Shots.Single().IncreasedEvolvedLegBudget);
            Check.Equal(8, state.Shots.Single().HitBudget(true));
            RoundTrip(state).Validate(0, true);
        });

        test("Common projectile Cross bonuses use the accepted last hit and actual contact before end of step", () =>
        {
            var rules = SharedShots("07", "08");
            var state = new ExpandedCrossState();
            var targets = new Dictionary<int, Vector2>
            {
                [1] = new Vector2(1, 0), [2] = new Vector2(2, 0), [3] = new Vector2(3, 0),
                [4] = new Vector2(4.25f, 0), [5] = new Vector2(4.6f, 0),
            };
            var attempts = new List<ExpandedCrossHit>();
            void Tick(double now, PermanentShotRules selected = default) =>
                ExpandedCross.Step(state, now, Vector2.Zero, Vector2.UnitX, 8, false, 30, 1.5, 1, .28, 5, 6,
                    () => targets, targets, hit => { attempts.Add(hit); return hit.TargetId != 4; }, shotRules: selected);
            Tick(0, rules);
            state = RoundTrip(state);
            Tick(.8);
            Check.True(attempts.Select(hit => hit.TargetId).SequenceEqual(new[] { 1, 2, 3, 4, 5 }));
            Check.True(attempts.Take(3).All(hit => hit.Damage == 30));
            Check.Near(32.1, attempts[3].Damage);
            Check.Near(33.9, attempts[4].Damage);
            Check.Equal(4, state.Shots.Single().OutboundHits);
            state.Validate(.8, false);
        });

        test("Common projectile Cross return measures cumulative distance through restored turn and actual contact", () =>
        {
            var rules = SharedShots("08", "10");
            var state = new ExpandedCrossState();
            var targets = new Dictionary<int, Vector2>();
            var hits = new List<ExpandedCrossHit>();
            void Tick(double now, PermanentShotRules selected = default) =>
                ExpandedCross.Step(state, now, Vector2.Zero, Vector2.UnitX, 8, false, 30, 1.5, 1, .28, 3.5, 6,
                    () => targets, targets, hit => { hits.Add(hit); return true; }, shotRules: selected);
            Tick(0, rules);
            Tick(.59);
            Check.True(state.Shots.Single().Returning);
            Check.Near(6.75, state.Shots.Single().ReturnSpeed);
            state = RoundTrip(state);
            state.Validate(.59, false);
            targets[1] = new Vector2(3.4f, 0);
            targets[2] = new Vector2(2.6f, 0);
            Tick(.75);
            Check.True(hits.All(hit => hit.Kind == ExpandedCrossHitKind.Return));
            Check.Near(30, hits.Single(hit => hit.TargetId == 1).Damage);
            Check.Near(31.8, hits.Single(hit => hit.TargetId == 2).Damage);
        });

        test("Cross a.05 returns with a saved two-second rotation cooldown at the exact reversal", () =>
        {
            var ranks = Rank("a.05");
            var state = new ExpandedCrossState();
            var targets = new Dictionary<int, Vector2>();
            var hits = new List<ExpandedCrossHit>();
            var firings = new List<double>();
            Step(state, 0, targets, hits, cycle: 1, ranks: ranks);
            targets[1] = new Vector2(5, 1);
            targets[2] = new Vector2(5, 1.001f);
            for (var tick = 1; tick <= 50; tick++)
            {
                var now = tick / 60d;
                Step(state, now, targets, hits, cycle: 1, ranks: ranks,
                    emitReturnSpin: (center, radius) =>
                    {
                        Check.Equal(new Vector2(5, 0), center);
                        Check.Near(1, radius);
                        firings.Add(now);
                    });
                if (tick == 49) Check.Equal(0, firings.Count);
            }
            Check.Equal(1, firings.Count);
            var spin = hits.Single(value => value.Kind == ExpandedCrossHitKind.ReturnSpin);
            Check.Equal(1, spin.TargetId);
            Check.Near(9, spin.Damage);
            Check.True(!spin.IsOriginal && spin.OriginalBaseDamage == 0 && spin.TargetOrdinal == 0);
            state = RoundTrip(state);
            state.Validate(50 / 60d, false);
            Check.Near(170 / 60d, state.NextReturnSpinAt!.Value);
            Check.True(state.Shots[0].Returning);
            targets.Clear();
            for (var tick = 51; tick <= 170; tick++)
            {
                var now = tick / 60d;
                Step(state, now, targets, hits, cycle: 1, ranks: ranks,
                    emitReturnSpin: (_, _) => firings.Add(now));
                if (tick == 169) Check.Equal(1, firings.Count);
            }
            Check.Equal(2, firings.Count);
            Check.Near(170 / 60d, firings[1]);
            state.Validate(170 / 60d, false);
        });

        test("Cross a.05 and e.05 resolve one strongest overlap while retaining the return ring and the evolution hold", () =>
        {
            var ranks = ExpandedCrossRankOptions.FromRanks(new Dictionary<string, int>
            {
                ["weapon.cross.a.05"] = 1, ["weapon.cross.e.05"] = 1,
                ["common.area.01"] = 1, ["common.area.09"] = 1,
            });
            var targets = new Dictionary<int, Vector2>();
            var hits = new List<ExpandedCrossHit>();
            var state = new ExpandedCrossState { Cycle = 3 };
            Step(state, 0, targets, hits, evolved: true, ranks: ranks);
            targets[1] = new Vector2(5, .879f);
            targets[2] = new Vector2(5, 1.099f);
            targets[3] = new Vector2(5, 1.101f);
            var radii = new List<double>();
            Step(state, 50 / 60d, targets, hits, evolved: true,
                emitTurnSpin: (_, radius) => radii.Add(radius), emitReturnSpin: (_, radius) => radii.Add(radius));
            Check.Equal(2, radii.Count);
            Check.Near(.88, radii[0]);
            Check.Near(1.1, radii[1]);
            var spinningHits = hits.Where(value => value.Kind is ExpandedCrossHitKind.TurnSpin or ExpandedCrossHitKind.ReturnSpin).ToArray();
            Check.True(spinningHits.Select(hit => hit.TargetId).SequenceEqual(new[] { 1, 2 }));
            Check.Near(10.5, spinningHits[0].Damage);
            Check.Equal(ExpandedCrossHitKind.TurnSpin, spinningHits[0].Kind);
            Check.Near(9, spinningHits[1].Damage);
            Check.Equal(ExpandedCrossHitKind.ReturnSpin, spinningHits[1].Kind);
            Check.True(state.Shots[0].IsSpinning);
            state.Validate(50 / 60d, true);

            hits.Clear();
            targets.Clear();
            var capacity = new ExpandedCrossState { Cycle = 3 };
            Step(capacity, 0, targets, hits, evolved: true, ranks: ranks);
            targets[1] = new Vector2(5, .87f);
            var suppressed = 0;
            Step(capacity, 50 / 60d, targets, hits, evolved: true, reserveTrail: () => capacity.ActiveTrailCount < 2,
                supportSuppressed: () => suppressed++);
            Check.Equal(1, suppressed);
            Check.True(capacity.NextTurnSpinAt.HasValue && !capacity.NextReturnSpinAt.HasValue);
            Check.Near(10.5, hits.Single().Damage);
        });

        test("Cross c.05 emits once at the swept intersection of distinct originals and persists its cooldown", () =>
        {
            var ranks = ExpandedCrossRankOptions.FromRanks(new Dictionary<string, int>
            {
                ["weapon.cross.c.05"] = 1, ["common.area.01"] = 1, ["common.area.09"] = 1,
            });
            var state = new ExpandedCrossState();
            var targets = new Dictionary<int, Vector2>();
            var hits = new List<ExpandedCrossHit>();
            var home = new Vector2(1.2f, -.18f);
            Step(state, 0, targets, hits, cycle: .15, ranks: ranks);
            for (var tick = 1; tick <= 9; tick++)
                Step(state, tick / 60d, targets, hits, cycle: .15, player: tick == 9 ? home : Vector2.Zero,
                    facing: Vector2.UnitY, ranks: ranks);
            state = RoundTrip(state);
            state.Validate(.15, false);
            Check.True(state.RecentTrajectories?.Count > 0);
            var invalid = state.Copy();
            invalid.RecentTrajectories![0].ShotId = state.NextShotId;
            Check.Throws<ArgumentException>(() => invalid.Validate(.15, false));

            targets[1] = new Vector2(1.2f, .87f);
            targets[2] = new Vector2(1.2f, .89f);
            var emissions = new List<(Vector2 Center, double Radius)>();
            for (var tick = 10; tick <= 90; tick++)
            {
                var now = tick / 60d;
                Step(state, now, targets, hits, cycle: .15, player: home, reserveOriginal: () => false, ranks: ranks,
                    emitIntersectionLight: (center, radius) => emissions.Add((center, radius)));
                state.Validate(now, false);
            }
            Check.Equal(1, emissions.Count);
            Check.Near(1.2, emissions[0].Center.X, 1e-6);
            Check.Near(0, emissions[0].Center.Y, 1e-6);
            Check.Near(.88, emissions[0].Radius);
            var pulse = hits.Single(hit => hit.Kind == ExpandedCrossHitKind.IntersectionLight);
            Check.Equal(1, pulse.TargetId);
            Check.Near(9, pulse.Damage);
            Check.True(!pulse.IsOriginal && pulse.TargetOrdinal == 0);
            state = RoundTrip(state);
            Check.Near(3.2, state.NextIntersectionAt!.Value);

            var fan = new ExpandedCrossState();
            targets.Clear();
            Step(fan, 0, targets, hits, count: 3, ranks: ranks);
            Step(fan, 1 / 60d, targets, hits, ranks: ranks);
            Check.True(!fan.NextIntersectionAt.HasValue);
        });

        test("Cross c.05 includes exactly a quarter-second but excludes older trajectories and unsupported groups", () =>
        {
            var ranks = Rank("c.05");
            var targets = new Dictionary<int, Vector2>();
            var hits = new List<ExpandedCrossHit>();
            foreach (var (delay, expected) in new[] { (.249, true), (.25, true), (.251, false) })
            {
                var state = new ExpandedCrossState();
                var home = new Vector2(.6f, (float)(-6 * (.1 + delay - .15)));
                Step(state, 0, targets, hits, cycle: .15, ranks: ranks);
                for (var tick = 1; tick <= 25; tick++)
                {
                    var now = tick / 60d;
                    Step(state, now, targets, hits, cycle: .15, ranks: ranks, reserveOriginal: () => tick == 9,
                        player: tick >= 9 ? home : Vector2.Zero, facing: Vector2.UnitY);
                    state.Validate(now, false);
                }
                Check.Equal(expected, state.NextIntersectionAt.HasValue);
            }

            var denied = new ExpandedCrossState();
            Step(denied, 0, targets, hits, cycle: .15, ranks: ranks);
            var suppressions = 0;
            for (var tick = 1; tick <= 25; tick++)
                Step(denied, tick / 60d, targets, hits, cycle: .15, ranks: ranks,
                    reserveOriginal: () => tick == 9, reserveTrail: () => false,
                    supportSuppressed: () => suppressions++, player: tick >= 9 ? new Vector2(1.2f, -.18f) : Vector2.Zero,
                    facing: Vector2.UnitY);
            Check.True(!denied.NextIntersectionAt.HasValue);
            Check.True(suppressions > 0);
        });

        test("Cross e.05 reserves only the fourth cycle's first spin and restores the half-second turn hold", () =>
        {
            var ranks = ExpandedCrossRankOptions.FromRanks(new Dictionary<string, int>
            {
                ["weapon.cross.e.05"] = 1, ["common.area.01"] = 1, ["common.area.09"] = 1,
            });
            var state = new ExpandedCrossState { Cycle = 3 };
            var targets = new Dictionary<int, Vector2>();
            var hits = new List<ExpandedCrossHit>();
            var starts = new List<(Vector2 Center, double Radius)>();
            Step(state, 0, targets, hits, evolved: true, count: 2, ranks: ranks);
            Check.True(state.Shots[0].SpinsAtTurn && !state.Shots[1].SpinsAtTurn);
            var center = state.Shots[0].Direction * 5;
            targets[1] = new Vector2(center.X, center.Y - .87f);
            targets[2] = new Vector2(center.X, center.Y - .89f);
            for (var tick = 1; tick <= 55; tick++)
            {
                var now = tick / 60d;
                Step(state, now, targets, hits, evolved: true, emitTurnSpin: (origin, radius) => starts.Add((origin, radius)));
                state.Validate(now, true);
                if (tick == 49) Check.Equal(0, starts.Count);
                if (tick == 50) Check.Equal(1, starts.Count);
            }
            state = RoundTrip(state);
            var spinning = state.Shots[0];
            Check.True(spinning.IsSpinning && !spinning.Returning);
            Check.True(state.Shots[1].Returning);
            Check.Near(50 / 60d + .5, spinning.SpinEndsAt!.Value);
            Check.Near(50 / 60d + 1, state.NextTurnSpinAt!.Value);
            var malformed = state.Copy();
            malformed.Shots[0].SpinEndsAt += .1;
            Check.Throws<ArgumentException>(() => malformed.Validate(55 / 60d, true));
            Check.Equal(3, state.ActiveTrailCount);
            Check.Equal(1, starts.Count);
            Check.Equal(center, starts[0].Center);
            Check.Near(.88, starts[0].Radius);
            var hit = hits.Single(value => value.Kind == ExpandedCrossHitKind.TurnSpin);
            Check.Equal(1, hit.TargetId);
            Check.Near(10.5, hit.Damage);
            Check.True(!hit.IsOriginal && hit.OriginalBaseDamage == 0);

            for (var tick = 56; tick <= 79; tick++) Step(state, tick / 60d, targets, hits, evolved: true);
            Check.Equal(center, spinning.Position);
            Step(state, 80 / 60d, targets, hits, evolved: true);
            Check.True(spinning.Returning && !spinning.IsSpinning);
            Check.Near(80 / 60d, spinning.ReturnStartedAt!.Value);
            Step(state, 81 / 60d, targets, hits, evolved: true);
            Check.True(Vector2.Distance(spinning.Position, center) > .09);
            Check.Equal(1, HitIds(hits, ExpandedCrossHitKind.TurnSpin).Length);
            state.Validate(81 / 60d, true);

            var denied = new ExpandedCrossState { Cycle = 3 };
            targets.Clear();
            hits.Clear();
            var suppression = 0;
            Step(denied, 0, targets, hits, evolved: true, ranks: ranks);
            for (var tick = 1; tick <= 50; tick++)
                Step(denied, tick / 60d, targets, hits, evolved: true, reserveTrail: () => false,
                    supportSuppressed: () => suppression++);
            Check.Equal(1, suppression);
            Check.True(!denied.Shots[0].SpinEndsAt.HasValue && denied.Shots[0].Returning);
        });

        test("Cross e.05 suppresses a fast fourth-cycle spin during the shared one-second group wait", () =>
        {
            var ranks = Rank("e.05");
            var state = new ExpandedCrossState { Cycle = 3 };
            var targets = new Dictionary<int, Vector2>();
            var hits = new List<ExpandedCrossHit>();
            var spins = new List<double>();
            Step(state, 0, targets, hits, evolved: true, cycle: .15, ranks: ranks);
            for (var tick = 1; tick <= 123; tick++)
            {
                var now = tick / 60d;
                Step(state, now, targets, hits, evolved: true, cycle: .15, ranks: ranks,
                    reserveOriginal: () => tick % 36 == 0, emitTurnSpin: (_, _) => spins.Add(now));
                state.Validate(now, true);
            }
            Check.Equal(2, spins.Count);
            Check.Near(50 / 60d, spins[0]);
            Check.Near(122 / 60d, spins[1]);
        });

        test("Cross synchronous elite pulses each consult active regional capacity and leave no occupancy", () =>
        {
            var ranks = new ExpandedCrossRankOptions(eliteHitThorn: true, eliteKillLight: true);
            var state = new ExpandedCrossState();
            var targets = new Dictionary<int, Vector2>();
            var hits = new List<ExpandedCrossHit>();
            bool IsElite(int id) => id == 1;
            Step(state, 0, targets, hits, ranks: ranks, isElite: IsElite);
            targets[1] = new Vector2(.2f, 0);
            targets[2] = new Vector2(.25f, .4f);
            var attempts = 0;
            bool ReservePulse()
            {
                Check.Equal(0, state.ActiveTrailCount);
                attempts++;
                return true;
            }
            Step(state, 1 / 60d, targets, hits, ranks: ranks, isElite: IsElite,
                apply: hit => { if (hit.IsOriginal) targets.Remove(1); return true; }, reserveTrail: ReservePulse);
            Check.Equal(2, attempts);
            Check.Equal(0, state.ActiveTrailCount);
            Check.True(hits.Where(hit => !hit.IsOriginal).Select(hit => hit.Kind).SequenceEqual(
                new[] { ExpandedCrossHitKind.EliteThorn, ExpandedCrossHitKind.EliteLight }));
            Check.True(state.NextEliteThornAt.HasValue && state.NextEliteLightAt.HasValue);
        });

        test("Cross b.05 fifth cycle reserves and restores one nonrecursive extra round trip", () =>
        {
            var ranks = Rank("b.05");
            var state = new ExpandedCrossState { Cycle = 3 };
            var targets = new Dictionary<int, Vector2>();
            var hits = new List<ExpandedCrossHit>();
            var reservations = 0;
            Func<bool> reserve = () => { reservations++; return true; };
            Step(state, 0, targets, hits, cycle: .5, ranks: ranks, reserveSupportShot: reserve);
            Check.Equal(0, reservations);
            for (var tick = 1; tick < 30; tick++)
                Step(state, tick / 60d, targets, hits, cycle: .5, ranks: ranks, reserveSupportShot: reserve);
            Check.Equal(1, Step(state, .5, targets, hits, cycle: .5, ranks: ranks, reserveSupportShot: reserve));
            Check.Equal(1, reservations);
            Check.Equal(2, state.ActiveOriginalShotCount);
            Check.Equal(1, state.ActiveSupportShotCount);
            Check.Equal(5L, state.Shots.Single(shot => shot.IsExtraPass).Cycle);
            var resumed = RoundTrip(state);
            resumed.Validate(.5, false);
            Check.Near(1.5, resumed.NextExtraPassAt!.Value);
            var saved = resumed.Copy();
            saved.Shots.Last().LeavesThorns = true;
            Check.Throws<ArgumentException>(() => saved.Validate(.5, false));

            targets[1] = new Vector2(2, 0);
            for (var tick = 31; tick <= 130; tick++)
            {
                var now = tick / 60d;
                Step(resumed, now, targets, hits, cycle: .5, reserveOriginal: () => false,
                    ranks: ranks, reserveSupportShot: reserve);
                resumed.Validate(now, false);
            }
            var supportHits = hits.Where(hit => hit.Kind == ExpandedCrossHitKind.ExtraPass).ToArray();
            Check.Equal(2, supportHits.Length);
            Check.True(supportHits.All(hit => !hit.IsOriginal && hit.TargetOrdinal == 0 && hit.OriginalBaseDamage == 0));
            foreach (var hit in supportHits) Check.Near(10.5, hit.Damage);
            Check.Equal(0, resumed.ActiveSupportShotCount);
            Check.Equal(1, reservations);

            var denied = new ExpandedCrossState { Cycle = 4 };
            var deniedCount = 0;
            Check.Equal(1, Step(denied, 0, targets, hits, ranks: ranks,
                reserveSupportShot: () => false, supportSuppressed: () => deniedCount++));
            Check.Equal(1, deniedCount);
            Check.Equal(0, denied.ActiveSupportShotCount);
            Check.True(!denied.NextExtraPassAt.HasValue);
            denied.Validate(0, false);
        });

        test("Cross e.02 applies one enhanced thorn hit only in simultaneous forward and return overlap", () =>
        {
            var state = new ExpandedCrossState();
            var targets = new Dictionary<int, Vector2>();
            var hits = new List<ExpandedCrossHit>();
            Step(state, 0, targets, hits, evolved: true, ranks: Rank("e.02"));
            for (var tick = 1; tick <= 60; tick++)
            {
                if (tick == 2) targets[1] = new Vector2(4.5f, 0);
                if (tick == 27) targets[2] = new Vector2(2, .33f);
                Step(state, tick / 60d, targets, hits, evolved: true);
            }
            Check.Near(10.5, hits.Single(hit => hit.Kind == ExpandedCrossHitKind.Thorns && hit.TargetId == 2).Damage);
            var resumed = RoundTrip(state);
            resumed.Validate(1, true);
            Check.True(resumed.Shots.Single().EmpoweredThornOverlap);
            hits.Clear();
            Step(resumed, 61 / 60d, targets, hits, evolved: true);
            Check.Near(14.1, hits.Single(hit => hit.Kind == ExpandedCrossHitKind.Thorns && hit.TargetId == 1).Damage);
            Check.Near(10.5, hits.Single(hit => hit.Kind == ExpandedCrossHitKind.Thorns && hit.TargetId == 2).Damage);

            // A moving actor can cross both directions in one fixed step while
            // staying outside their simultaneous overlap.
            resumed = state.Copy();
            var shot = resumed.Shots.Single();
            shot.Thorns = new List<ExpandedCrossThornState>
            {
                new ExpandedCrossThornState { StartX = 1, EndX = 2, BornAt = .5 },
                new ExpandedCrossThornState { StartX = 3, EndX = 4, BornAt = .95, Returning = true },
            };
            hits.Clear();
            targets.Clear();
            targets[4] = new Vector2(4, 0);
            Step(resumed, 61 / 60d, targets, hits, evolved: true, player: new Vector2(40, 0),
                previous: new Dictionary<int, Vector2> { [4] = new Vector2(1, 0) });
            Check.Near(10.5, hits.Single(hit => hit.Kind == ExpandedCrossHitKind.Thorns && hit.TargetId == 4).Damage);
        });

        test("Cross e.03 and common damage region bonuses add from each base without widening moving collision", () =>
        {
            var ranks = ExpandedCrossRankOptions.FromRanks(new Dictionary<string, int>
            {
                ["weapon.cross.e.03"] = 1, ["weapon.cross.d.01"] = 1,
                ["common.area.01"] = 1, ["common.area.09"] = 1,
            });
            var state = new ExpandedCrossState();
            var targets = new Dictionary<int, Vector2>();
            var hits = new List<ExpandedCrossHit>();
            Step(state, 0, targets, hits, evolved: true, ranks: ranks);
            var resumed = RoundTrip(state);
            resumed.Validate(0, true);
            var shot = resumed.Shots.Single();
            Check.Near(.46875, shot.ThornRadius);
            Check.Near(.33, shot.AfterglowRadius);
            Check.Near(.28, shot.Radius);
            targets[1] = new Vector2(.15f, .46f);
            targets[2] = new Vector2(.15f, .32f);
            targets[3] = new Vector2(.15f, .48f);
            for (var tick = 1; tick <= 4; tick++) Step(resumed, tick / 60d, targets, hits, evolved: true);
            Check.True(HitIds(hits, ExpandedCrossHitKind.Thorns).SequenceEqual(new[] { 1, 2 }));
            Check.True(HitIds(hits, ExpandedCrossHitKind.Afterglow).SequenceEqual(new[] { 2 }));
            Check.Equal(0, HitIds(hits, ExpandedCrossHitKind.Outbound).Length);
        });

        test("Cross e.04 reserves a widened wave at actual catch even on an empty field, and never on expiry", () =>
        {
            var ranks = new ExpandedCrossRankOptions(waveOnRecovery: true, widenSupportShape: true, widenDamageRegion: true);
            var state = new ExpandedCrossState();
            var targets = new Dictionary<int, Vector2>();
            var hits = new List<ExpandedCrossHit>();
            var emissions = new List<(Vector2 Origin, double Radius)>();
            var initial = 0;
            Step(state, 0, targets, hits, evolved: true, ranks: ranks, count: 2, reserveTrail: () => ++initial <= 1);
            var resumed = RoundTrip(state);
            Check.True(resumed.Shots[1].WavesOnRecovery && !resumed.Shots[1].LeavesThorns);
            resumed.Validate(0, true);
            for (var tick = 1; tick <= 97; tick++)
                Step(resumed, tick / 60d, targets, hits, evolved: true,
                    emitRecoveryWave: (origin, radius) => emissions.Add((origin, radius)));
            Check.Equal(1, emissions.Count);
            Check.Near(1.65, emissions[0].Radius);
            Check.Near(.35, emissions[0].Origin.Length(), 1e-5);
            Check.Equal(0, HitIds(hits, ExpandedCrossHitKind.RecoveryWave).Length);
            Check.Near(97 / 60d + 3, resumed.NextRecoveryWaveAt!.Value);
            RoundTrip(resumed).Validate(97 / 60d, true);

            var expire = new ExpandedCrossState();
            Step(expire, 0, targets, hits, evolved: true, ranks: ranks);
            for (var tick = 1; tick <= 232; tick++)
            {
                var now = tick / 60d;
                Step(expire, now, targets, hits, evolved: true, player: new Vector2(0, 50 + tick),
                    reserveOriginal: () => false, emitRecoveryWave: (origin, radius) => emissions.Add((origin, radius)));
                expire.Validate(now, true);
            }
            Check.Equal(1, emissions.Count);
            Check.True(!expire.NextRecoveryWaveAt.HasValue);

            state = new ExpandedCrossState();
            Step(state, 0, targets, hits, evolved: true, ranks: ranks);
            targets[1] = new Vector2(.35f, 1.6f);
            targets[2] = new Vector2(.35f, 1.66f);
            for (var tick = 1; tick <= 97; tick++) Step(state, tick / 60d, targets, hits, evolved: true);
            var wave = hits.Single(hit => hit.Kind == ExpandedCrossHitKind.RecoveryWave);
            Check.Equal(1, wave.TargetId);
            Check.Near(10.5, wave.Damage);
            Check.True(!wave.IsOriginal && wave.OriginalBaseDamage == 0);
        });

        test("Cross original applied-damage callback is authoritative for both elite derivatives", () =>
        {
            var ranks = new ExpandedCrossRankOptions(eliteHitThorn: true, eliteKillLight: true);
            var state = new ExpandedCrossState();
            var targets = new Dictionary<int, Vector2>();
            var hits = new List<ExpandedCrossHit>();
            bool IsElite(int id) => id == 1;
            Step(state, 0, targets, hits, ranks: ranks, isElite: IsElite);
            targets[1] = new Vector2(.2f, 0);
            targets[2] = new Vector2(.25f, .4f);
            var callbacks = 0;
            double? Original(ExpandedCrossHit attack)
            {
                Check.Equal(1, attack.TargetId);
                callbacks++;
                targets.Remove(attack.TargetId);
                return 42;
            }
            Step(state, 1 / 60d, targets, hits, ranks: ranks, isElite: IsElite, applyOriginalHit: Original);
            Check.Equal(1, callbacks);
            Check.True(hits.All(hit => !hit.IsOriginal));
            Check.Near(12.6, hits.Single(hit => hit.Kind == ExpandedCrossHitKind.EliteThorn).Damage);
            Check.Near(10.5, hits.Single(hit => hit.Kind == ExpandedCrossHitKind.EliteLight).Damage);
        });

        test("Cross a.03 freezes an 18 degree gap between launches", () =>
        {
            var state = new ExpandedCrossState();
            var targets = new Dictionary<int, Vector2>();
            var hits = new List<ExpandedCrossHit>();
            Step(state, 0, targets, hits, count: 3, ranks: Rank("a.03"));
            var loaded = RoundTrip(state);
            loaded.Validate(0, false);
            foreach (var (shot, angle) in loaded.Shots.Zip(new[] { -18d, 0d, 18d }))
                Check.Near(angle, Math.Atan2(shot.Direction.Y, shot.Direction.X) * 180 / Math.PI, 1e-4);
        });

        test("Cross primary damage stacks by phase and accepted hit ordinal using the frozen base", () =>
        {
            var ranks = ExpandedCrossRankOptions.FromRanks(new Dictionary<string, int>
            {
                ["weapon.cross.a.04"] = 1,
                ["weapon.cross.b.02"] = 1,
                ["weapon.cross.b.04"] = 1,
                ["weapon.cross.c.03"] = 1,
            });
            var state = new ExpandedCrossState();
            var targets = Enumerable.Range(1, 6).ToDictionary(id => id, id => new Vector2(id * .8f, 0));
            var hits = new List<ExpandedCrossHit>();
            Step(state, 0, targets, hits, evolved: true, ranks: ranks);
            for (var tick = 1; tick <= 50; tick++) Step(state, tick / 60d, targets, hits, evolved: true);
            var outbound = hits.Where(hit => hit.Kind == ExpandedCrossHitKind.Outbound).ToArray();
            Check.Equal(6, outbound.Length);
            foreach (var (hit, damage) in outbound.Zip(new[] { 32.4, 30, 30, 32.4, 32.4, 32.4 }))
                Check.Near(damage, hit.Damage);

            var restored = RoundTrip(state);
            restored.Validate(50 / 60d, true);
            Check.Equal(8, restored.Shots.Single().DamageLevel);
            targets.Add(7, new Vector2(4.6f, 0));
            hits.Clear();
            for (var tick = 51; tick <= 100; tick++)
            {
                Step(restored, tick / 60d, targets, hits, evolved: true);
                restored.Validate(tick / 60d, true);
            }
            var returning = hits.Where(hit => hit.Kind == ExpandedCrossHitKind.Return).ToArray();
            Check.Near(34.8, returning.Single(hit => hit.TargetId == 7).Damage);
            Check.Near(36.9, returning.Single(hit => hit.TargetId == 1).Damage);
            Check.True(hits.Where(hit => hit.Kind == ExpandedCrossHitKind.Thorns).All(hit => hit.Damage == 10.5));

            var capped = new ExpandedCrossState();
            targets.Clear();
            targets[1] = new Vector2(.3f, 0);
            hits.Clear();
            Step(capped, 0, targets, hits, evolved: true, damage: 67, ranks: ranks);
            Step(capped, 1 / 60d, targets, hits, evolved: true, damage: 67);
            Check.Near(67.5, hits.Single(hit => hit.Kind == ExpandedCrossHitKind.Outbound).Damage);
        });

        test("Cross b.03 bends counterclockwise on return, unwinds on time, and catches by swept geometry", () =>
        {
            var state = new ExpandedCrossState();
            var targets = new Dictionary<int, Vector2>();
            var hits = new List<ExpandedCrossHit>();
            Step(state, 0, targets, hits, ranks: Rank("b.03"));
            for (var tick = 1; tick <= 51; tick++) Step(state, tick / 60d, targets, hits);
            var shot = state.Shots.Single();
            Check.Near(-Math.Sin(Math.PI / 9) * .1, shot.Position.Y, 1e-6);
            Check.Near(20, TurnOffsetDegrees(-Vector2.UnitX, shot.Direction), 1e-4);

            var restored = RoundTrip(state);
            restored.Validate(.85, false);
            for (var tick = 52; tick <= 75; tick++)
            {
                var prior = restored.Shots.Single().Position;
                var now = tick / 60d;
                Step(state, now, targets, hits);
                Step(restored, now, targets, hits);
                restored.Validate(now, false);
                var current = restored.Shots.Single();
                Check.Equal(state.Shots.Single().Position, current.Position);
                Check.Equal(state.Shots.Single().Direction, current.Direction);
                if (tick == 63)
                    Check.Near(10, TurnOffsetDegrees(-prior, current.Direction), 1e-4);
                if (tick == 75)
                    Check.Near(0, TurnOffsetDegrees(-prior, current.Direction), 1e-4);
            }

            var catchState = new ExpandedCrossState();
            Step(catchState, 0, targets, hits, ranks: Rank("b.03"));
            Step(catchState, 50 / 60d, targets, hits);
            var player = new Vector2(4.6f, 0);
            Step(catchState, 50.5 / 60d, targets, hits, player: player);
            Check.True(catchState.Shots.Single().IsFlying);
            Step(catchState, 51 / 60d, targets, hits, player: player);
            Check.Equal(0, catchState.Shots.Count);

            var trailState = new ExpandedCrossState();
            var trailRanks = new ExpandedCrossRankOptions(accelerateOutbound: true, curvedReturn: true,
                extendThorns: true, widenSupportShape: true);
            Step(trailState, 0, targets, hits, evolved: true, ranks: trailRanks);
            for (var tick = 1; tick <= 110; tick++)
            {
                var now = tick / 60d;
                Step(trailState, now, targets, hits, evolved: true, player: new Vector2(0, 30));
                trailState.Validate(now, true);
            }
            var trail = RoundTrip(trailState).Shots.Single();
            Check.True(ShotTrailCrossesBothLegs(trail));
            Check.Near(1.8, trail.ThornDuration);
            Check.Near(.39, trail.ThornRadius);
        });

        test("Cross c.04 gives only the first sixth-cycle projectile eight return hits in isolation", () =>
        {
            var state = new ExpandedCrossState { Cycle = 5 };
            var targets = new Dictionary<int, Vector2>();
            var hits = new List<ExpandedCrossHit>();
            Step(state, 0, targets, hits, count: 2, ranks: Rank("c.04"));
            var restored = RoundTrip(state);
            var first = restored.Shots[0];
            var second = restored.Shots[1];
            Check.Equal(4, first.HitBudget(false));
            Check.Equal(8, first.ReturnBudget(false));
            Check.Equal(4, second.ReturnBudget(false));
            var bad = restored.Copy();
            bad.Shots[0].LaunchIndex = 1;
            Check.Throws<ArgumentException>(() => bad.Validate(0, false));

            var direction = first.Direction;
            for (var tick = 1; tick <= 50; tick++) Step(restored, tick / 60d, targets, hits);
            for (var id = 1; id <= 9; id++) targets.Add(id, direction * (1.55f + (id - 1) * .35f));
            for (var tick = 51; tick <= 100; tick++)
            {
                Step(restored, tick / 60d, targets, hits);
                restored.Validate(tick / 60d, false);
            }
            Check.Equal(8, hits.Count(hit => hit.ShotId == first.Id && hit.Kind == ExpandedCrossHitKind.Return));
            Check.Equal(0, hits.Count(hit => hit.Kind == ExpandedCrossHitKind.Outbound));

            var other = new ExpandedCrossState { Cycle = 4 };
            Step(other, 0, new Dictionary<int, Vector2>(), hits, ranks: Rank("c.04"));
            Check.Equal(4, other.Shots.Single().ReturnBudget(false));
        });

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

        test("Cross a.01 applies additive outbound speed before the cap and retains the exact restored turn time", () =>
        {
            var state = new ExpandedCrossState();
            var targets = new Dictionary<int, Vector2>();
            var hits = new List<ExpandedCrossHit>();
            Step(state, 0, targets, hits, evolved: true, speed: 8.25, ranks: Rank("a.01"));
            Step(state, .5, targets, hits, evolved: true, speed: 8.25);
            Check.Near(9.15, state.Shots.Single().OutboundSpeed);
            Check.Near(4.575, state.Shots.Single().Position.X, 1e-6);
            var loaded = RoundTrip(state);
            loaded.Validate(.5, true);
            Step(loaded, .55, targets, hits, evolved: true);
            Check.Near(5 / 9.15, loaded.Shots.Single().ReturnStartedAt!.Value);
            Check.Near(8.25, loaded.Shots.Single().ReturnSpeed);
            loaded.Validate(.55, true);

            var capped = new ExpandedCrossState();
            Step(capped, 0, targets, hits, evolved: true, speed: 12.75, ranks: Rank("a.01"));
            Check.Near(7.5 * 1.75, capped.Shots.Single().OutboundSpeed);

            var corrupt = loaded.Copy();
            corrupt.Shots[0].TrainedOutboundSpeed = 14;
            Check.Throws<ArgumentException>(() => corrupt.Validate(.55, true));
        });

        test("Cross a.02 prefers elites within the inclusive five-unit distance, with deterministic ties", () =>
        {
            var targets = new Dictionary<int, Vector2>
            {
                [9] = new Vector2(0, -5), [3] = new Vector2(0, 5), [1] = new Vector2(1, 0),
            };
            var hits = new List<ExpandedCrossHit>();
            var state = new ExpandedCrossState();
            Step(state, 0, targets, hits, ranks: Rank("a.02"), isElite: id => id != 1);
            Check.Equal(Vector2.UnitY, state.Shots.Single().Direction);
            targets[9] = new Vector2(0, -5.001f);
            targets[3] = new Vector2(0, 5.001f);
            Step(state, 2, targets, hits, ranks: Rank("a.02"), isElite: id => id != 1);
            Check.Equal(Vector2.UnitX, state.Shots.Single().Direction);
        });

        test("Cross b.01 accelerates only the return leg and freezes that speed before recovery", () =>
        {
            var state = new ExpandedCrossState();
            var targets = new Dictionary<int, Vector2>();
            var hits = new List<ExpandedCrossHit>();
            Step(state, 0, targets, hits, ranks: Rank("b.01"));
            Step(state, .8, targets, hits);
            var loaded = RoundTrip(state);
            Check.Near(6, loaded.Shots.Single().OutboundSpeed);
            Check.Near(6.72, loaded.Shots.Single().ReturnSpeed);
            Step(loaded, .9, targets, hits);
            var shot = loaded.Shots.Single();
            Check.Near(5 / 6d, shot.ReturnStartedAt!.Value);
            Check.Near(5 - (.9 - 5 / 6d) * 6.72, shot.Position.X, 1e-6);
            loaded.Validate(.9, false);
        });

        test("Cross c.01 adds one successful hit to both leg budgets and preserves the rehit delay", () =>
        {
            var state = new ExpandedCrossState();
            var targets = Enumerable.Range(1, 6).ToDictionary(id => id, id => new Vector2(id * .8f, 0));
            var hits = new List<ExpandedCrossHit>();
            Step(state, 0, targets, hits, ranks: Rank("c.01"));
            for (var tick = 1; tick <= 51; tick++) Step(state, tick / 60d, targets, hits);
            var loaded = RoundTrip(state);
            Check.Equal(5, loaded.Shots.Single().HitBudget(false));
            for (var tick = 52; tick <= 100; tick++)
            {
                Step(loaded, tick / 60d, targets, hits);
                loaded.Validate(tick / 60d, false);
            }
            Check.True(HitIds(hits, ExpandedCrossHitKind.Outbound).SequenceEqual(new[] { 1, 2, 3, 4, 5 }));
            Check.True(HitIds(hits, ExpandedCrossHitKind.Return).SequenceEqual(new[] { 6, 4, 3, 2, 1 }));

            var evolved = new ExpandedCrossState();
            targets = Enumerable.Range(1, 8).ToDictionary(id => id, id => new Vector2(id * .55f, 0));
            hits.Clear();
            Step(evolved, 0, targets, hits, evolved: true, ranks: Rank("c.01"));
            for (var tick = 1; tick <= 50; tick++) Step(evolved, tick / 60d, targets, hits, evolved: true);
            Check.Equal(7, evolved.Shots.Single().HitBudget(true));
            Check.Equal(7, HitIds(hits, ExpandedCrossHitKind.Outbound).Length);
            evolved.Validate(50 / 60d, true);
        });

        test("Cross c.02 applies eight additive collision-radius points to actual swept hits", () =>
        {
            var state = new ExpandedCrossState();
            var targets = new Dictionary<int, Vector2>();
            var hits = new List<ExpandedCrossHit>();
            Step(state, 0, targets, hits, ranks: Rank("c.02"));
            Check.Near(.3024, state.Shots.Single().Radius);
            targets.Add(1, new Vector2(.04f, .3024f));
            targets.Add(2, new Vector2(.04f, .303f));
            Step(state, 1 / 60d, targets, hits);
            Check.Equal(1, hits.Single().TargetId);
        });

        test("Cross e.01 keeps evolved thorn collision active through the extra pulse and restores its exact expiry", () =>
        {
            var state = new ExpandedCrossState();
            var targets = new Dictionary<int, Vector2>();
            var hits = new List<ExpandedCrossHit>();
            Step(state, 0, targets, hits, evolved: true, ranks: Rank("e.01"));
            targets.Add(1, new Vector2(-.37f, 0));
            for (var tick = 1; tick <= 90; tick++)
                Step(state, tick / 60d, targets, hits, evolved: true, player: new Vector2(100, 0));
            var loaded = RoundTrip(state);
            var shot = loaded.Shots.Single();
            Check.Near(1.8, shot.ThornDuration);
            Check.Equal(3, HitIds(hits, ExpandedCrossHitKind.Thorns).Length);
            for (var tick = 91; tick <= 108; tick++)
            {
                Step(loaded, tick / 60d, targets, hits, evolved: true, player: new Vector2(100, 0));
                loaded.Validate(tick / 60d, true);
            }
            Check.Equal(4, HitIds(hits, ExpandedCrossHitKind.Thorns).Length);
            Check.Near(1 / 60d, shot.Thorns[0].BornAt);
            Step(loaded, 109 / 60d, targets, hits, evolved: true, player: new Vector2(100, 0));
            Check.True(shot.Thorns[0].BornAt > 1 / 60d);
            loaded.Validate(109 / 60d, true);

            var basic = new ExpandedCrossState();
            Step(basic, 0, targets, hits, ranks: Rank("e.01"));
            Check.True(!basic.Shots.Single().ExtendedThorns && !basic.Shots.Single().LeavesThorns);
        });

        test("Cross preserves the launch-level damage basis and resets one-based ordinals on each leg", () =>
        {
            var state = new ExpandedCrossState();
            var targets = new Dictionary<int, Vector2> { [1] = new Vector2(1, 0), [2] = new Vector2(2, 0) };
            var hits = new List<ExpandedCrossHit>();
            void Tick(ExpandedCrossState current, int tick, int level, List<ExpandedCrossHit> output) =>
                ExpandedCross.Step(current, tick / 60d, Vector2.Zero, Vector2.UnitX, level, false,
                    6, 2, 1, .28, 5, 6, () => targets, targets, hit => { output.Add(hit); return true; });

            for (var tick = 0; tick <= 12; tick++) Tick(state, tick, 1, hits);
            Check.Equal((5d, 1), (hits.Single().OriginalBaseDamage, hits.Single().TargetOrdinal));
            var preview = state.Copy();
            preview.Shots.Single().OriginalBaseDamage = 0;
            var old = RoundTrip(preview);
            old.Validate(.2, false);

            var loaded = RoundTrip(state);
            loaded.Validate(.2, false);
            var corrupt = loaded.Copy();
            corrupt.Shots.Single().OriginalBaseDamage = 7;
            Check.Throws<ArgumentException>(() => corrupt.Validate(.2, false));
            for (var tick = 13; tick <= 100; tick++) Tick(loaded, tick, 4, hits);
            Check.True(hits.All(hit => hit.OriginalBaseDamage == 5));
            Check.True(hits.Where(hit => hit.Kind == ExpandedCrossHitKind.Outbound)
                .Select(hit => hit.TargetOrdinal).SequenceEqual(new[] { 1, 2 }));
            Check.True(hits.Where(hit => hit.Kind == ExpandedCrossHitKind.Return)
                .Select(hit => hit.TargetOrdinal).SequenceEqual(new[] { 1, 2 }));
            var oldHits = new List<ExpandedCrossHit>();
            for (var tick = 13; tick <= 22; tick++) Tick(old, tick, 4, oldHits);
            Check.Equal((0d, 2), (oldHits.Single().OriginalBaseDamage, oldHits.Single().TargetOrdinal));

            hits.Clear();
            targets[1] = Vector2.Zero;
            var evolved = new ExpandedCrossState();
            Step(evolved, 0, targets, hits, evolved: true);
            Step(evolved, 1 / 60d, targets, hits, evolved: true);
            var thorns = hits.Single(hit => hit.Kind == ExpandedCrossHitKind.Thorns);
            Check.Equal((0d, 0), (thorns.OriginalBaseDamage, thorns.TargetOrdinal));
        });

        test("Cross d.01 leaves a width .60, one-second afterglow and reserves it separately from evolved thorns", () =>
        {
            var targets = new Dictionary<int, Vector2>();
            var hits = new List<ExpandedCrossHit>();
            var state = new ExpandedCrossState();
            Step(state, 0, targets, hits, ranks: Rank("d.01"));
            targets[1] = new Vector2(.04f, .3f);
            targets[2] = new Vector2(.04f, .301f);
            Step(state, 1 / 60d, targets, hits);
            var glow = hits.Single(hit => hit.Kind == ExpandedCrossHitKind.Afterglow);
            Check.Equal((1, 7.5d, 0), (glow.TargetId, glow.Damage, glow.TargetOrdinal));
            Check.True(!glow.IsOriginal);
            Check.Near(1, state.Shots.Single().AfterglowDuration);
            Check.Equal(1, state.ActiveTrailCount);

            var evolved = new ExpandedCrossState();
            var suppressed = 0;
            Step(evolved, 0, targets, hits, evolved: true, ranks: Rank("d.01"),
                reserveTrail: () => evolved.ActiveTrailCount < 1, supportSuppressed: () => suppressed++);
            Check.Equal(1, evolved.ActiveTrailCount);
            Check.Equal(1, suppressed);
            Check.True(evolved.Shots.Single().LeavesThorns && !evolved.Shots.Single().LeavesAfterglow);
            var doubleTrail = new ExpandedCrossState();
            Step(doubleTrail, 0, targets, hits, evolved: true, ranks: Rank("d.01"));
            Check.Equal(2, doubleTrail.ActiveTrailCount);
            RoundTrip(doubleTrail).Validate(0, true);
        });

        test("Cross d.02 restores the longer outer piece and preserves the anchored extra afterglow pulse", () =>
        {
            foreach (var trained in new[] { false, true })
            {
                var ranks = new ExpandedCrossRankOptions(leaveAfterglow: true, extendAfterglow: trained);
                var state = new ExpandedCrossState();
                var targets = new Dictionary<int, Vector2>();
                var hits = new List<ExpandedCrossHit>();
                Step(state, 0, targets, hits, ranks: ranks);
                targets[1] = new Vector2(-.29f, 0);
                for (var tick = 1; tick <= 60; tick++) Step(state, tick / 60d, targets, hits);
                var restored = RoundTrip(state);
                restored.Validate(1, false);
                Check.Near(trained ? 1.2 : 1, restored.Shots.Single().AfterglowDuration);
                Step(restored, 61 / 60d, targets, hits);
                Check.Equal(trained ? 3 : 2, HitIds(hits, ExpandedCrossHitKind.Afterglow).Length);
            }
        });

        test("Cross d.03 afterglow chooses the stronger return piece once per target when legs overlap", () =>
        {
            var state = new ExpandedCrossState();
            var targets = new Dictionary<int, Vector2>();
            var hits = new List<ExpandedCrossHit>();
            var ranks = new ExpandedCrossRankOptions(leaveAfterglow: true, boostReturningAfterglow: true);
            Step(state, 0, targets, hits, ranks: ranks);
            targets[1] = new Vector2(.04f, .29f);
            targets[2] = new Vector2(4.7f, .1f);
            Step(state, 1 / 60d, targets, hits);
            Check.Near(7.5, hits.Single(hit => hit.Kind == ExpandedCrossHitKind.Afterglow).Damage);
            for (var tick = 2; tick <= 60; tick++) Step(state, tick / 60d, targets, hits);
            var restored = RoundTrip(state);
            hits.Clear();
            Step(restored, 61 / 60d, targets, hits);
            var glow = hits.Single(hit => hit.Kind == ExpandedCrossHitKind.Afterglow && hit.TargetId == 2);
            Check.Near(10.5, glow.Damage);
        });

        test("Cross d.04 pulses from a confirmed elite original contact and persists the two-second shared cooldown", () =>
        {
            var state = new ExpandedCrossState();
            var targets = new Dictionary<int, Vector2>();
            var hits = new List<ExpandedCrossHit>();
            var ranks = Rank("d.04");
            Step(state, 0, targets, hits, ranks: ranks, isElite: id => id == 1);
            targets[1] = new Vector2(.06f, 0);
            targets[2] = new Vector2(0, .6f);
            targets[3] = new Vector2(0, .601f);
            Step(state, 1 / 60d, targets, hits, ranks: ranks, isElite: id => id == 1);
            var effects = hits.Where(hit => hit.Kind == ExpandedCrossHitKind.EliteThorn).ToArray();
            Check.True(effects.Select(hit => hit.TargetId).SequenceEqual(new[] { 1, 2 }));
            Check.True(effects.All(hit => hit.Damage == 9 && hit.OriginalBaseDamage == 0 && !hit.IsOriginal));
            var restored = RoundTrip(state);
            restored.Validate(1 / 60d, false);
            Check.Near(2 + 1 / 60d, restored.NextEliteThornAt!.Value);
            restored.ResetForForm(false, 1);
            hits.Clear();
            Step(restored, 1, targets, hits, ranks: ranks, isElite: id => id == 1);
            Step(restored, 1 + 1 / 60d, targets, hits, ranks: ranks, isElite: id => id == 1);
            Check.Equal(0, HitIds(hits, ExpandedCrossHitKind.EliteThorn).Length);
        });

        test("Cross d.05 chooses the two nearest normal targets only after confirmed elite removal", () =>
        {
            foreach (var (killed, reserved) in new[] { (true, true), (false, true), (true, false) })
            {
                var state = new ExpandedCrossState();
                var targets = new Dictionary<int, Vector2>();
                var hits = new List<ExpandedCrossHit>();
                var ranks = Rank("d.05");
                var suppressed = 0;
                Step(state, 0, targets, hits, ranks: ranks, isElite: id => id == 1 || id == 5);
                targets[1] = new Vector2(.06f, 0);
                targets[2] = new Vector2(1, 0);
                targets[3] = new Vector2(-1, 0);
                targets[4] = new Vector2(2, 0);
                targets[5] = new Vector2(.06f, .4f);
                Step(state, 1 / 60d, targets, hits, ranks: ranks, isElite: id => id == 1 || id == 5,
                    apply: hit =>
                    {
                        if (killed && hit.IsOriginal && hit.TargetId == 1) targets.Remove(1);
                        return true;
                    }, reserveTrail: () => reserved, supportSuppressed: () => suppressed++);
                var light = hits.Where(hit => hit.Kind == ExpandedCrossHitKind.EliteLight).ToArray();
                if (killed && reserved)
                {
                    Check.True(light.Select(hit => hit.TargetId).SequenceEqual(new[] { 2, 3 }));
                    Check.True(light.All(hit => hit.Damage == 7.5 && hit.TargetOrdinal == 0 && !hit.IsOriginal));
                    Check.Near(4 + 1 / 60d, RoundTrip(state).NextEliteLightAt!.Value);
                }
                else
                {
                    Check.Equal(0, light.Length);
                    Check.True(!state.NextEliteLightAt.HasValue);
                }
                Check.Equal(killed && !reserved ? 1 : 0, suppressed);
            }
        });
    }

    private static double TurnOffsetDegrees(Vector2 tangent, Vector2 direction)
    {
        var from = Vector2.Normalize(tangent);
        return Math.Atan2(from.X * direction.Y - from.Y * direction.X, Vector2.Dot(from, direction)) * 180 / Math.PI;
    }

    private static ExpandedCrossRankOptions Rank(string id) => ExpandedCrossRankOptions.FromRanks(
        new Dictionary<string, int> { ["weapon.cross." + id] = 1 });

    private static int[] HitIds(IEnumerable<ExpandedCrossHit> hits, ExpandedCrossHitKind kind) =>
        hits.Where(hit => hit.Kind == kind).Select(hit => hit.TargetId).ToArray();

    private static bool ShotTrailCrossesBothLegs(ExpandedCrossShotState shot) =>
        shot.Thorns.Any(piece => piece.Returning) && shot.Thorns.Any(piece => !piece.Returning);

    private static (ExpandedCrossHitKind, int, Vector2, long, double) HitSignature(ExpandedCrossHit hit) =>
        (hit.Kind, hit.TargetId, hit.Origin, hit.ShotId, hit.Damage);

    private static PermanentShotRules SharedShots(params string[] ids) =>
        PermanentShotRules.FromRanks(ids.ToDictionary(id => "common.shot." + id, _ => 1));

    private static int Step(ExpandedCrossState state, double now, IReadOnlyDictionary<int, Vector2> targets,
        List<ExpandedCrossHit> hits, bool evolved = false, double cycle = 2, int count = 1,
        Vector2? player = null, Vector2? facing = null, IReadOnlyDictionary<int, Vector2>? previous = null,
        Func<bool>? reserveOriginal = null, Func<bool>? reserveTrail = null, double damage = 30, double speed = 6,
        ExpandedCrossRankOptions ranks = default, Func<int, bool>? isElite = null,
        Func<ExpandedCrossHit, bool>? apply = null, Action? supportSuppressed = null,
        Func<bool>? reserveSupportShot = null, Action<Vector2, double>? emitRecoveryWave = null,
        Func<ExpandedCrossHit, double?>? applyOriginalHit = null, Action<Vector2, double>? emitTurnSpin = null,
        Action<Vector2, double>? emitIntersectionLight = null, Action<Vector2, double>? emitReturnSpin = null) =>
        ExpandedCross.Step(state, now, player ?? Vector2.Zero, facing ?? Vector2.UnitX, evolved ? 8 : 1,
            evolved, damage, cycle, count, .28, 5, speed, () => targets, previous ?? targets, hit =>
            {
                if (apply?.Invoke(hit) == false) return false;
                hits.Add(hit);
                return true;
            }, reserveOriginal, reserveTrail, ranks, isElite, recordSupportSuppression: supportSuppressed,
            reserveSupportShot: reserveSupportShot, emitRecoveryWave: emitRecoveryWave, applyOriginalHit: applyOriginalHit,
            emitTurnSpin: emitTurnSpin, emitIntersectionLight: emitIntersectionLight, emitReturnSpin: emitReturnSpin);

    private static ExpandedCrossState RoundTrip(ExpandedCrossState state)
    {
        using var stream = new MemoryStream();
        var serializer = new DataContractJsonSerializer(typeof(ExpandedCrossState));
        serializer.WriteObject(stream, state);
        stream.Position = 0;
        return (ExpandedCrossState)serializer.ReadObject(stream)!;
    }
}
