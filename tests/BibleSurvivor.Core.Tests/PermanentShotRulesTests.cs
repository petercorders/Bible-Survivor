using System;
using System.Linq;
using System.Numerics;

namespace BibleSurvivor.Core.Tests;

internal static class PermanentShotRulesTests
{
    public static void Register(Action<string, Action> test)
    {
        test("Common projectile launch bonuses distinguish first speed and later swept size using independent caps", () =>
        {
            var rules = Ranks("01", "02", "03", "05", "06");
            Check.Near(.05, rules.FlightSpeedBonus);
            Check.Near(.04, rules.CollisionBonus);
            Check.Equal(1, rules.PierceBonus);
            Check.Near(8, rules.LaunchSpeed(5, 7.6, first: true));
            Check.Near(7.6, rules.LaunchSpeed(5, 7.6, first: false));
            Check.Near(8.75, rules.LaunchSpeed(5, 8.6, first: true));
            Check.Near(.9, rules.LaunchCollisionDimension(.8, .9, first: true));
            Check.Near(.948, rules.LaunchCollisionDimension(.8, .9, first: false));
            Check.Near(2, rules.LaunchCollisionDimension(.8, 1.99, first: false));
            Check.Near(0, default(PermanentShotRules).CollisionBonus);
        });

        test("Common projectile aim reduces signed shortest error while preserving exact or absent target axes", () =>
        {
            var rules = Ranks("04");
            Check.Near(36, Angle(rules.CorrectAim(Direction(40), Vector2.UnitX)), 1e-4);
            Check.Near(-36, Angle(rules.CorrectAim(Direction(-40), Vector2.UnitX)), 1e-4);
            Check.Near(-172, Angle(rules.CorrectAim(Direction(-170), Direction(170))), 1e-4);
            var exact = Direction(80);
            Check.Equal(exact, rules.CorrectAim(exact, exact * 2));
            Check.Equal(Vector2.UnitY, rules.CorrectAim(Vector2.UnitY, Vector2.Zero));
            Check.Equal(Vector2.UnitY, default(PermanentShotRules).CorrectAim(Vector2.UnitY, Vector2.UnitX));
        });

        test("Common projectile damage uses last accepted pierce and exact swept travel before one precritical cap", () =>
        {
            var bonuses = Ranks("07", "08").HitBonuses;
            Check.Equal(PermanentShotHitBonuses.LastPierce | PermanentShotHitBonuses.LongFlight, bonuses);
            Check.True(PermanentShotRules.ValidHitBonuses(bonuses));
            Check.True(!PermanentShotRules.ValidHitBonuses((PermanentShotHitBonuses)4));
            Check.Near(45.2, PermanentShotRules.HitDamage(bonuses, 40, 40, 1, 4));
            Check.Near(42.8, PermanentShotRules.HitDamage(bonuses, 40, 40, 1, 3.9999));
            Check.Near(42.4, PermanentShotRules.HitDamage(bonuses, 40, 40, 2, 4));
            Check.Near(42.4, PermanentShotRules.HitDamage(bonuses, 40, 40, null, 4));
            Check.Near(40, PermanentShotRules.HitDamage(bonuses, 40, 40, null, 3.9999));
            Check.Near(90, PermanentShotRules.HitDamage(bonuses, 40, 89, 1, 4));
        });

        test("Common projectile evolution increases each finite budget without recapping motion speed early", () =>
        {
            var rules = Ranks("05", "09", "10");
            Check.Equal(6, rules.EvolutionPierceBudget(6, false));
            Check.Equal(7, rules.EvolutionPierceBudget(6, true));
            Check.Equal(8, rules.EvolutionPierceBudget(8, true));
            var firstLaunch = rules.LaunchSpeed(6, 6.6, true);
            Check.Near(7.08, firstLaunch);
            Check.Near(7.68, rules.ReturnSpeed(6, firstLaunch));
            Check.Near(10.5, rules.ReturnSpeed(6, 10.4));
            Check.Near(6.6, default(PermanentShotRules).ReturnSpeed(6, 6.6));
        });
    }

    private static PermanentShotRules Ranks(params string[] numbers) =>
        PermanentShotRules.FromRanks(numbers.ToDictionary(number => "common.shot." + number, _ => 1));

    private static Vector2 Direction(double degrees) =>
        new Vector2((float)Math.Cos(degrees * Math.PI / 180), (float)Math.Sin(degrees * Math.PI / 180));

    private static double Angle(Vector2 direction) => Math.Atan2(direction.Y, direction.X) * 180 / Math.PI;
}
