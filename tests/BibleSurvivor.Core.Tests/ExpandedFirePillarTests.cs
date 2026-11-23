using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Json;
using System.Numerics;
using System.Text.Json;

namespace BibleSurvivor.Core.Tests;

internal static class ExpandedFirePillarTests
{
    public static void Register(Action<string, Action> test)
    {
        test("Fire pillar freezes launch base and ranks targets after selecting each overlap winner", () =>
        {
            var state = new ExpandedFirePillarState();
            var targets = new Dictionary<int, Vector2> { [99] = Vector2.UnitX, [100] = Vector2.UnitX * 3 };
            var hits = new List<ExpandedFirePillarHit>();
            void Tick(ExpandedFirePillarState current, double now, int level) =>
                ExpandedFirePillar.Step(current, now, Vector2.Zero, false, 14, 3, 2, 1.2, 2,
                    () => targets, hits.Add, level: level);

            Tick(state, 0, 1);
            Check.True(state.Regions.All(region => region.OriginalBaseDamage == 10));
            using var save = new MemoryStream();
            var serializer = new DataContractJsonSerializer(typeof(ExpandedFirePillarState));
            serializer.WriteObject(save, state);
            save.Position = 0;
            var restored = (ExpandedFirePillarState)serializer.ReadObject(save)!;
            var corrupt = restored.Copy();
            corrupt.Regions[0].OriginalBaseDamage = 12;
            Check.Throws<ArgumentException>(() => corrupt.Validate(0, false));
            targets = new Dictionary<int, Vector2>
            {
                [9] = new Vector2(3.3f, 0), [3] = new Vector2(2, 0), [8] = new Vector2(1.3f, 0),
                [2] = new Vector2(1.2f, 0), [1] = new Vector2(3.2f, 0),
            };

            var expected = new[] { (2, 1, 10d), (3, 2, 10d), (8, 3, 10d), (1, 1, 10d), (9, 2, 10d) };
            Tick(restored, .15, 8);
            Check.True(hits.Select(hit => (hit.TargetId, hit.TargetOrdinal, hit.OriginalBaseDamage)).SequenceEqual(expected));
            hits.Clear();
            Tick(restored, .65, 8);
            Check.True(hits.Select(hit => (hit.TargetId, hit.TargetOrdinal, hit.OriginalBaseDamage)).SequenceEqual(expected));
            restored.Validate(.65, false);
        });

        test("Fire pillars reserve nearest positions, hit once on overlap, and respect the exclusive end", () =>
        {
            var state = new ExpandedFirePillarState();
            var hits = new List<ExpandedFirePillarHit>();
            var targets = new Dictionary<int, Vector2> { [1] = new(2, 0) };
            void Step(double now) => ExpandedFirePillar.Step(state, now, Vector2.Zero, false, 10, 3, 2, .9, 2,
                () => targets, hits.Add);
            Step(0);
            Check.Equal(2, state.Regions.Count);
            Check.Near(2, state.Regions[0].X);
            Check.Near(1.4, state.Regions[1].X, 1e-6);
            Step(.149);
            Check.Equal(0, hits.Count);
            Step(.15);
            Check.Near(10, hits.Single().Damage);
            for (var tick = 10; tick <= 120; tick++) Step(tick / 60d);
            Check.Equal(4, hits.Count);
            Check.Equal(0, state.Regions.Count);
        });

        test("Evolved fire pillar preserves movement and embers across a checkpoint and cancels on form reset", () =>
        {
            var state = new ExpandedFirePillarState();
            var positions = new Dictionary<int, Vector2> { [1] = new(1, 0) };
            var hits = new List<ExpandedFirePillarHit>();
            int Step(ExpandedFirePillarState fire, double now, List<ExpandedFirePillarHit> output) =>
                ExpandedFirePillar.Step(fire, now, Vector2.Zero, true, 20, 3, 1, 1.125, 3, () => positions, output.Add);
            Check.Equal(1, Step(state, 0, hits));
            positions[1] = new Vector2(7, 0);
            for (var tick = 1; tick <= 55; tick++) Step(state, tick / 60d, hits);
            Check.True(state.Regions.Any(region => region.Ember));
            Check.True(state.Regions.Single(region => !region.Ember).X > 2);
            var restored = state.Copy();
            restored.Validate(55 / 60d, true);
            for (var tick = 56; tick < 100; tick++)
            {
                Step(state, tick / 60d, hits);
                Step(restored, tick / 60d, new List<ExpandedFirePillarHit>());
            }
            Check.Equal(JsonSerializer.Serialize(state), JsonSerializer.Serialize(restored));
            restored.ResetForForm(false, 100 / 60d);
            Check.Equal(0, restored.Regions.Count);
        });

        test("Trained fire pillar restores its advanced ignition and subsequent exact pulse", () =>
        {
            var state = new ExpandedFirePillarState();
            var targets = new Dictionary<int, Vector2> { [1] = Vector2.UnitX };
            var hits = new List<ExpandedFirePillarHit>();
            void Step(ExpandedFirePillarState current, double now, double delayAdvance = 0) =>
                ExpandedFirePillar.Step(current, now, Vector2.Zero, false, 10, 3, 1, .9, 2,
                    () => targets, hits.Add, delayAdvance);

            Step(state, 0, .08);
            Check.Near(.07, state.Regions.Single().NextHit);
            Step(state, .0699);
            Check.Equal(0, hits.Count);
            using var save = new MemoryStream();
            var serializer = new DataContractJsonSerializer(typeof(ExpandedFirePillarState));
            serializer.WriteObject(save, state);
            save.Position = 0;
            var restored = (ExpandedFirePillarState)serializer.ReadObject(save)!;
            restored.Validate(.0699, false);
            Step(restored, .07);
            Check.Equal(1, hits.Count);
            Step(restored, .0701);
            Check.Equal(1, hits.Count);
            Step(restored, .5699);
            Check.Equal(1, hits.Count);
            Step(restored, .57);
            Check.Equal(2, hits.Count);
            var capped = new ExpandedFirePillarState();
            Step(capped, 0, 1);
            Check.Near(.05, capped.Regions.Single().NextHit);
        });

        test("Fire pillar placement prioritizes crowds then nearby elites and evolves longer embers", () =>
        {
            var targets = new Dictionary<int, Vector2>
            {
                [1] = new Vector2(1, 0),
                [2] = new Vector2(3, 0),
                [3] = new Vector2(4.8f, 0),
                [4] = new Vector2(5.4f, 0),
                [5] = new Vector2(6, 0),
            };
            ExpandedFirePillarState Place(ExpandedFirePillarSettings settings)
            {
                var state = new ExpandedFirePillarState();
                ExpandedFirePillar.Step(state, 0, Vector2.Zero, false, 10, 3, 1, .9, 2, () => targets, _ => { },
                    settings: settings);
                return state;
            }
            Check.Near(1, Place(default).Regions.Single().X);
            Check.Near(targets[3].X, Place(new ExpandedFirePillarSettings(true, null, false)).Regions.Single().X);
            Check.Near(3, Place(new ExpandedFirePillarSettings(true, id => id is 2 or 5, false)).Regions.Single().X);
            targets[2] = new Vector2(4.001f, 0);
            Check.Near(targets[3].X, Place(new ExpandedFirePillarSettings(true, id => id is 2 or 5, false)).Regions.Single().X);

            var evolved = new ExpandedFirePillarState();
            targets = new Dictionary<int, Vector2> { [1] = Vector2.UnitX };
            var extended = new ExpandedFirePillarSettings(false, null, true);
            ExpandedFirePillar.Step(evolved, 0, Vector2.Zero, true, 20, 3, 1, 1.125, 3, () => targets, _ => { },
                settings: extended);
            targets[1] = new Vector2(7, 0);
            for (var tick = 1; tick <= 55; tick++)
                ExpandedFirePillar.Step(evolved, tick / 60d, Vector2.Zero, true, 20, 3, 1, 1.125, 3,
                    () => targets, _ => { }, settings: extended);
            var ember = evolved.Regions.Single(region => region.Ember);
            Check.Near(1.20, ember.EndsAt - ember.StartedAt);
        });

        test("Evolved fire freezes the trained ember radius separately from the pillar radius", () =>
        {
            var state = new ExpandedFirePillarState();
            var targets = new Dictionary<int, Vector2> { [1] = Vector2.UnitX };
            var hits = new List<ExpandedFirePillarHit>();
            const double pillarRadius = 1.206;
            var settings = new ExpandedFirePillarSettings(false, null, false, emberRadiusBonus: .04);
            void Tick(ExpandedFirePillarState current, double now, ExpandedFirePillarSettings ranks) =>
                ExpandedFirePillar.Step(current, now, Vector2.Zero, true, 20, 3, 1, pillarRadius, 3,
                    () => targets, hits.Add, settings: ranks);

            Tick(state, 0, settings);
            targets[1] = new Vector2(7, 0);
            for (var tick = 1; tick <= 55; tick++) Tick(state, tick / 60d, settings);
            var ember = state.Regions.Single(region => region.Ember);
            Check.Near(pillarRadius, state.Regions.Single(region => !region.Ember).Radius);
            Check.Near(.676, ember.Radius);

            using var save = new MemoryStream();
            var serializer = new DataContractJsonSerializer(typeof(ExpandedFirePillarState));
            serializer.WriteObject(save, state);
            save.Position = 0;
            var restored = (ExpandedFirePillarState)serializer.ReadObject(save)!;
            restored.Validate(55 / 60d, true);
            targets[2] = new Vector2(ember.X, .67f);
            targets[3] = new Vector2(ember.X, .68f);
            hits.Clear();
            Tick(restored, ember.NextHit, default);
            var emberHit = hits.Single(hit => hit.IsEmber);
            Check.Equal(2, emberHit.TargetId);
            Check.Equal(0, emberHit.TargetOrdinal);
            Check.Equal(0d, emberHit.OriginalBaseDamage);
        });

        test("Evolved fire does not allocate an ember when shared support regions are full", () =>
        {
            var state = new ExpandedFirePillarState();
            var targets = new Dictionary<int, Vector2> { [1] = Vector2.UnitX };
            var supportSuppressions = 0;
            void Tick(double now) => ExpandedFirePillar.Step(state, now, Vector2.Zero, true, 20, 3, 1, 1.125, 3,
                () => targets, _ => { }, reserveEmber: () => false, recordSupportSuppression: () => supportSuppressions++);
            Tick(0);
            targets[1] = new Vector2(7, 0);
            for (var tick = 1; tick <= 55; tick++) Tick(tick / 60d);
            Check.Equal(1, supportSuppressions);
            Check.True(state.Regions.Single().X > 2);
            Check.True(!state.Regions.Single().Ember);
        });
        test("Pillar a.03 projects the chosen live target by a quarter second of fixed-step movement up to one unit", () =>
        {
            var positions = new Dictionary<int, Vector2> { [1] = new Vector2(1.02f, 0) };
            var previous = new Dictionary<int, Vector2> { [1] = Vector2.UnitX };
            var ranks = new ExpandedFirePillarSettings(false, null, false, predictPlacement: true);
            ExpandedFirePillarState Place(IReadOnlyDictionary<int, Vector2> history)
            {
                var state = new ExpandedFirePillarState();
                Tick(state, 0, positions, _ => { }, settings: ranks, previousTargets: history);
                return Restore(state);
            }

            var restored = Place(previous);
            restored.Validate(0, false);
            Check.Near(1.32, restored.Regions.Single().X, 1e-6);

            positions[1] = new Vector2(1, 1);
            previous[1] = Vector2.Zero;
            var capped = Place(previous).Regions.Single();
            Check.Near(1 + Math.Sqrt(.5), capped.X, 1e-6);
            Check.Near(1 + Math.Sqrt(.5), capped.Y, 1e-6);
            Check.Equal(positions[1], Place(new Dictionary<int, Vector2>()).Regions.Single().Center);
        });

        test("Pillar a.04 chooses the inclusive 1.2-separated candidate before nearer alternatives and falls back gracefully", () =>
        {
            var positions = new Dictionary<int, Vector2>
            {
                [1] = Vector2.Zero, [2] = new Vector2(1.19f, 0), [3] = new Vector2(1.2f, 0),
            };
            var ranks = new ExpandedFirePillarSettings(false, null, false, preferSeparatedCenters: true);
            var ranked = new ExpandedFirePillarState();
            Tick(ranked, 0, positions, _ => { }, settings: ranks, amount: 3);
            Check.True(ranked.Regions.Select(region => region.X).SequenceEqual(new[] { 0f, 1.2f, 1.19f }));
            positions.Remove(3);
            var fallback = new ExpandedFirePillarState();
            Tick(fallback, 0, positions, _ => { }, settings: ranks, amount: 3);
            Check.True(fallback.Regions.Select(region => region.X).SequenceEqual(new[] { 0f, 1.19f, -.6f }));
        });

        test("Pillar e.02 slows movement only when four enemies occupy the evolved pillar's current radius 1.5", () =>
        {
            var ranks = new ExpandedFirePillarSettings(false, null, false, slowInCrowds: true);
            double Move(bool evolved, int count, float nearest, float farthest)
            {
                var state = new ExpandedFirePillarState();
                var positions = new Dictionary<int, Vector2> { [1] = Vector2.UnitX };
                Tick(state, 0, positions, _ => { }, settings: ranks, evolved: evolved);
                positions.Clear();
                for (var id = 1; id <= count; id++)
                    positions[id] = new Vector2(nearest + (farthest - nearest) * (id - 1) / (count - 1), 0);
                var restored = Restore(state);
                Tick(restored, 1 / 60d, positions, _ => { }, settings: ranks, evolved: evolved);
                restored.Validate(1 / 60d, evolved);
                return restored.Regions.Single().X;
            }

            Check.Near(1 + 1.2 * .7 / 60, Move(true, 4, 2.2f, 2.5f), 1e-6);
            Check.Near(1 + 1.2 / 60, Move(true, 3, 2.2f, 2.5f), 1e-6);
            Check.Near(1 + 1.2 / 60, Move(true, 4, 4.2f, 4.5f), 1e-6);
            Check.Near(1, Move(false, 4, 2.2f, 2.5f));
        });

        test("Pillar b.02 adds eight original damage points at the inclusive 40-percent core through save and restore", () =>
        {
            var ranks = new ExpandedFirePillarSettings(false, null, false, increaseCoreDamage: true);
            var state = new ExpandedFirePillarState();
            var positions = new Dictionary<int, Vector2> { [1] = Vector2.Zero };
            var hits = new List<ExpandedFirePillarHit>();
            Tick(state, 0, positions, hits.Add, settings: ranks, damage: 12, level: 1);
            positions[2] = new Vector2(.36f, 0);
            positions[3] = new Vector2(.361f, 0);
            var restored = Restore(state);
            restored.Validate(0, false);
            foreach (var now in new[] { .15, .65 })
            {
                hits.Clear();
                Tick(restored, now, positions, hits.Add, damage: 12);
                var actual = hits.ToDictionary(hit => hit.TargetId);
                Check.Near(12.8, actual[2].Damage);
                Check.Near(12, actual[3].Damage);
            }
        });

        test("Pillar d.01 adds ten original damage points only on the scheduled ignition and caps combined modifiers once", () =>
        {
            var ranks = new ExpandedFirePillarSettings(false, null, false, increaseIgnitionDamage: true);
            var state = new ExpandedFirePillarState();
            var positions = new Dictionary<int, Vector2> { [1] = Vector2.Zero };
            var hits = new List<ExpandedFirePillarHit>();
            Tick(state, 0, positions, hits.Add, settings: ranks, damage: 12, level: 1);
            var restored = Restore(state);
            Tick(restored, .15, positions, hits.Add, damage: 12);
            Tick(restored, .65, positions, hits.Add, damage: 12);
            Check.True(hits.Select(hit => hit.Damage).SequenceEqual(new[] { 13d, 12d }));

            hits.Clear();
            var combined = new ExpandedFirePillarSettings(false, null, false,
                increaseCoreDamage: true, increaseIgnitionDamage: true);
            var combinedState = new ExpandedFirePillarState();
            Tick(combinedState, 0, positions, hits.Add, settings: combined, damage: 21, level: 1);
            Tick(combinedState, .15, positions, hits.Add, damage: 21);
            Check.Near(22.5, hits.Single().Damage);
            var corrupt = combinedState.Copy();
            corrupt.Regions.Single().BaseDamage = 12;
            Check.Throws<ArgumentException>(() => corrupt.Validate(.15, false));
        });

        test("Pillar overlap chooses the higher conditional original damage, with one hit per simultaneous target", () =>
        {
            var ranks = new ExpandedFirePillarSettings(false, null, false, increaseCoreDamage: true);
            var state = new ExpandedFirePillarState();
            var positions = new Dictionary<int, Vector2> { [1] = Vector2.Zero };
            var hits = new List<ExpandedFirePillarHit>();
            Tick(state, 0, positions, hits.Add, settings: ranks, damage: 11, level: 1, cycle: .5);
            Tick(state, .15, positions, hits.Add, damage: 11, cycle: .5);
            positions.Add(2, new Vector2(.5f, 0));
            Tick(state, .5, positions, hits.Add, damage: 11.5, cycle: .5, player: new Vector2(.5f, 0));
            hits.Clear();
            Tick(state, .65, positions, hits.Add, damage: 11.5, cycle: .5);
            Check.Equal(2, hits.Count);
            Check.Equal(1, hits[0].TargetId);
            Check.Near(11.8, hits[0].Damage);
            Check.Equal(2, hits[1].TargetId);
            Check.Near(11.5, hits[1].Damage);
        });

        test("Pillar closing ember and ash apply independently only after natural expiry and honor support slots", () =>
        {
            foreach (var allow in new[] { false, true })
            {
                var state = new ExpandedFirePillarState();
                var targets = new Dictionary<int, Vector2> { [1] = new Vector2(2, 0) };
                var hits = new List<ExpandedFirePillarHit>();
                var suppressed = 0;
                var ranks = new ExpandedFirePillarSettings(false, null, false, leaveClosingEmber: true, closingAshHit: true);
                void Step(ExpandedFirePillarState current, double now) =>
                    ExpandedFirePillar.Step(current, now, Vector2.Zero, false, 12, 3, 1, .9, 2,
                        () => targets, hits.Add, settings: ranks, reserveEmber: () => allow,
                        recordSupportSuppression: () => suppressed++);

                for (var tick = 0; tick < 120; tick++) Step(state, tick / 60d);
                var resumed = Restore(state);
                var cancelled = Restore(state);
                cancelled.ResetForForm(false, 2);
                Check.Equal(0, cancelled.Regions.Count);
                hits.Clear();
                Step(resumed, 2);
                Check.Equal(allow ? 2 : 0, hits.Count);
                Check.Equal(allow ? 0 : 2, suppressed);
                if (!allow) continue;
                Check.True(hits.All(hit => hit.IsEmber && hit.Damage == 3 && hit.TargetOrdinal == 0));
                var ember = resumed.Regions.Single();
                Check.Equal(ExpandedFireSupportKind.ClosingEmber, ember.SupportKind);
                Check.Near(2, ember.StartedAt);
                Check.Near(3, ember.EndsAt);
                Check.Near(.7, ember.Radius);
                Check.Near(4, resumed.NextClosingEmberAt!.Value);
                Check.Near(4, resumed.NextClosingAshAt!.Value);
                Restore(resumed).Validate(2, false);
            }
        });

        test("Pillar close-range fire on the fifth cycle uses the nearest eligible enemy instead of crowd priority", () =>
        {
            var state = new ExpandedFirePillarState();
            var ranks = new ExpandedFirePillarSettings(true, null, false, closeRangeFire: true);
            var targets = new Dictionary<int, Vector2>
            {
                [1] = new Vector2(3, 0), [2] = new Vector2(3.1f, 0),
                [3] = new Vector2(.8f, 0), [4] = new Vector2(-.6f, 0),
            };
            var hits = new List<ExpandedFirePillarHit>();
            for (var tick = 0; tick <= 12 * 60; tick++) Tick(state, tick / 60d, targets, hits.Add, settings: ranks);
            Check.Equal(5L, state.Cycle);
            var fire = Restore(state).Regions.Single(region => region.SupportKind == ExpandedFireSupportKind.CloseRange);
            Check.Equal(targets[4], fire.Center);
            Check.Near(3.5, fire.Damage);
            Check.Near(12, fire.StartedAt);
            Check.Near(16, state.NextCloseRangeAt!.Value);
            hits.Clear();
            Tick(state, 12 + BattleWorld.FixedStep, targets, hits.Add, settings: ranks);
            Check.True(hits.Select(hit => hit.TargetId).SequenceEqual(new[] { 4 }));
            Check.True(hits.Single().IsEmber);
        });

    }

    private static ExpandedFirePillarState Restore(ExpandedFirePillarState state)
    {
        using var bytes = new MemoryStream();
        var serializer = new DataContractJsonSerializer(typeof(ExpandedFirePillarState));
        serializer.WriteObject(bytes, state);
        bytes.Position = 0;
        return (ExpandedFirePillarState)serializer.ReadObject(bytes)!;
    }

    private static void Tick(ExpandedFirePillarState state, double now, IReadOnlyDictionary<int, Vector2> targets,
        Action<ExpandedFirePillarHit> hit, bool evolved = false, double damage = 10, int amount = 1, double cycle = 3,
        Vector2 player = default, ExpandedFirePillarSettings settings = default,
        IReadOnlyDictionary<int, Vector2>? previousTargets = null, int level = 0) =>
        ExpandedFirePillar.Step(state, now, player, evolved, damage, cycle, amount, .9, 2,
            () => targets, hit, settings: settings, previousTargets: previousTargets, level: level);
}
