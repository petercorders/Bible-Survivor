using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace BibleSurvivor.Core.Tests;

internal static class ExpandedBibleRanksTests
{
    public static void Register(Action<string, Action> test)
    {
        test("Bible expands only the dense half-plane when six enemy centers are within radius three", () =>
        {
            var ranks = Ranks("a.02");
            var player = new Vector2(10, 10);
            var enemies = Enumerable.Range(1, 5).Select(value => player + new Vector2(value * .4f, 0))
                .Concat(new[] { player + 3 * Vector2.UnitX, player - 3.001f * Vector2.UnitX }).ToArray();
            Check.Equal(Vector2.Zero, default(ExpandedBibleRanks).FindCrowdedDirection(player, enemies));
            var crowd = ranks.FindCrowdedDirection(player, enemies);
            Check.Equal(Vector2.UnitX, crowd);
            Check.Near(1.81, ranks.Book(player, 0, 4, 0, 1, 0, 3, 1.6, 1.4, crowd).Radius);
            Check.Near(1.6, ranks.Book(player, 1, 4, 0, 1, 0, 3, 1.6, 1.4, crowd).Radius);
            Check.Near(1.6, ranks.Book(player, 2, 4, 0, 1, 0, 3, 1.6, 1.4, crowd).Radius);
            Check.Near(1.6, ranks.Book(player, 3, 4, 0, 1, 0, 3, 1.6, 1.4, crowd).Radius);

            enemies[5] = player + 3.001f * Vector2.UnitX;
            Check.Equal(Vector2.Zero, ranks.FindCrowdedDirection(player, enemies));
            var symmetric = new[] { Vector2.UnitX, -Vector2.UnitX, Vector2.UnitY, -Vector2.UnitY, 2 * Vector2.UnitX, -2 * Vector2.UnitX };
            Check.Equal(Vector2.Zero, ranks.FindCrowdedDirection(player, symmetric.Select(offset => player + offset)));
        });

        test("Bible specializes only the added last book and reverses only that rotation from the same phase slot", () =>
        {
            var ranks = Ranks("a.03", "a.04");
            var player = new Vector2(5, -2);
            ExpandedBibleBookPose Book(int index, double time) => ranks.Book(player, index, 3, time, 1.12, 0, 3, 2, 1.4);
            var outerStart = Book(1, 0);
            var innerStart = Book(2, 0);
            var outerNext = Book(1, .2);
            var innerNext = Book(2, .2);
            Check.Near(2 * Math.PI / 3, outerStart.Angle);
            Check.Near(4 * Math.PI / 3, innerStart.Angle);
            Check.Near(2, outerNext.Radius);
            Check.Near(1.3, innerNext.Radius);
            Check.Near(1.3, Vector2.Distance(innerNext.Position, player), 1e-6);
            Check.Equal(1, outerNext.RotationSign);
            Check.Equal(-1, innerNext.RotationSign);
            Check.Near(.56, outerNext.Angle - outerStart.Angle);
            Check.Near(-.56, innerNext.Angle - innerStart.Angle);
            Check.Equal(1, Ranks("a.03").Book(player, 2, 3, .2, 1, 0, 3, 2, 1.4).RotationSign);
        });

        test("Bible excursion increments are triangular over the frozen native cycle and honor the shared shape cap", () =>
        {
            var ranks = Ranks("a.02", "a.03", "a.05");
            double OrbitAt(double time, int index = 0) => ranks.Book(Vector2.Zero, index, 2, time, 1, 10, 4, 2, 1.4).Radius;
            Check.Near(2, OrbitAt(10));
            Check.Near(2.1, OrbitAt(11));
            Check.Near(2.2, OrbitAt(12));
            Check.Near(2.1, OrbitAt(13));
            Check.Near(2, OrbitAt(14));
            Check.Near(2, OrbitAt(15));
            Check.Near(2.2 * .65, OrbitAt(12, index: 1));
            Check.Near(3.5, ranks.Book(Vector2.Zero, 0, 2, 0, 1, -2, 4, 3.45, 1.4, Vector2.UnitX).Radius);
            Check.Near(3.5 * .65, ranks.Book(Vector2.Zero, 1, 2, 0, 1, -2, 4, 3.45, 1.4, -Vector2.UnitX).Radius);
        });

        test("Bible book contact adds hard cover and applies approach knockback at the player range boundary", () =>
        {
            var ranks = Ranks("b.01", "b.02");
            Check.Near(0, default(ExpandedBibleRanks).BookKnockback(Vector2.UnitX));
            Check.Near(.20, Ranks("b.01").BookKnockback(Vector2.UnitX));
            Check.Near(.45, ranks.BookKnockback(new Vector2(1.2f, 0)));
            Check.Near(.20, ranks.BookKnockback(new Vector2(1.2001f, 0)));
            Check.Near(.45, ranks.BookKnockback(new Vector2(-.6f, .6f)));
        });
    }

    private static ExpandedBibleRanks Ranks(params string[] ids) =>
        ExpandedBibleRanks.FromRanks(ids.ToDictionary(id => "weapon.bible." + id, _ => 1));
}
