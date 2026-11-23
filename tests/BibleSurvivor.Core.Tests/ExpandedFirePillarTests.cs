using System;
using System.Collections.Generic;
using System.Linq;
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
    }
}
