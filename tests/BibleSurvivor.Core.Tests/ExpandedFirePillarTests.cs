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
            Check.Equal(2, hits.Single(hit => hit.IsEmber).TargetId);
        });

        test("Evolved fire does not allocate an ember when shared support regions are full", () =>
        {
            var state = new ExpandedFirePillarState();
            var targets = new Dictionary<int, Vector2> { [1] = Vector2.UnitX };
            void Tick(double now) => ExpandedFirePillar.Step(state, now, Vector2.Zero, true, 20, 3, 1, 1.125, 3,
                () => targets, _ => { }, reserveEmber: () => false);
            Tick(0);
            targets[1] = new Vector2(7, 0);
            for (var tick = 1; tick <= 55; tick++) Tick(tick / 60d);
            Check.True(state.Regions.Single().X > 2);
            Check.True(!state.Regions.Single().Ember);
        });
    }
}
