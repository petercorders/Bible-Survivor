using System;
using System.Collections.Generic;

namespace BibleSurvivor.Core.Tests;

internal static class PermanentOriginalHitRulesTests
{
    public static void Register(Action<string, Action> test)
    {
        test("Common power damage checks each event condition at its inclusive boundary", () =>
        {
            Power("02", 5, Hit(elite: true), Hit(boss: true), Hit(elite: true, boss: true));
            Power("02", 0, Hit());
            Power("03", 6, Hit(first: true));
            Power("03", 0, Hit(first: false, ordinal: 1));
            Power("04", 6, Hit(target: .2499), Hit(target: .25));
            Power("04", 0, Hit(target: .2501));
            Power("05", 5, Hit(player: .9), Hit(player: .9001));
            Power("05", 0, Hit(player: .8999));
            Power("06", 6, Hit(ordinal: 3), Hit(ordinal: 4));
            Power("06", 0, Hit(ordinal: 2));
            Power("07", 7, Hit(hurt: 0), Hit(hurt: 2.9999), Hit(hurt: 3));
            Power("07", 0, Hit(hurt: 3.0001), Hit(hurt: null));
            Power("08", 5, Hit(distance: 4), Hit(distance: 4.0001));
            Power("08", 0, Hit(distance: 3.9999));
            Power("09", 7, Hit(mark: true));
            Power("09", 0, Hit(mark: false));
            Power("10", 10, Hit(target: .2499, boss: true), Hit(target: .25, boss: true));
            Power("10", 0, Hit(target: .2501, boss: true), Hit(target: .25, elite: true));
        });

        test("Common critical probability and multiplier apply separately at condition boundaries", () =>
        {
            Critical("01", .03, 1.5, Hit());
            Critical("02", 0, 1.6, Hit());
            Critical("03", .05, 1.5, Hit(first: true));
            Critical("03", 0, 1.5, Hit(first: false));
            Critical("04", .04, 1.5, Hit(elite: true), Hit(boss: true), Hit(elite: true, boss: true));
            Critical("04", 0, 1.5, Hit());
            Critical("05", 0, 1.65, Hit(target: .2499), Hit(target: .25));
            Critical("05", 0, 1.5, Hit(target: .2501));
            Critical("06", .04, 1.5, Hit(player: .90), Hit(player: .9001));
            Critical("06", 0, 1.5, Hit(player: .8999));
            Critical("07", .04, 1.5, Hit(distance: 4), Hit(distance: 4.0001));
            Critical("07", 0, 1.5, Hit(distance: 3.9999));
            Critical("08", .05, 1.5, Hit(still: 1), Hit(still: 1.0001));
            Critical("08", 0, 1.5, Hit(still: .9999), Hit(still: 0));
            Critical("09", 0, 1.65, Hit(mark: true));
            Critical("09", 0, 1.5, Hit(mark: false));
            Critical("10", .08, 1.5, Hit(target: .2499, boss: true), Hit(target: .25, boss: true));
            Critical("10", 0, 1.5, Hit(target: .2501, boss: true), Hit(target: .25, elite: true));
        });

        test("Permanent original hit combines additive values before caps and attenuates before the damage floor", () =>
        {
            var ranks = new Dictionary<string, int>();
            for (var rank = 1; rank <= 10; rank++)
            {
                if (rank >= 2) ranks.Add($"common.power.{rank:00}", 1);
                ranks.Add($"common.crit.{rank:00}", 1);
            }
            var context = Hit(.25, .90, first: true, ordinal: 3, boss: true, hurt: 3,
                distance: 4, still: 1, mark: true);
            var combined = PermanentOriginalHitRules.Evaluate(ranks, context, 80, damageBonus: .28);
            Check.Near(148, combined.NonCriticalDamage);
            Check.Near(.33, combined.CriticalChance);
            Check.Near(1.9, combined.CriticalMultiplier);
            Check.Near(148 * (1 + .33 * .9), combined.ExpectedDamage);

            var capped = PermanentOriginalHitRules.Evaluate(ranks, context, 80, damageBonus: 1.22,
                criticalChanceBonus: .1, criticalMultiplierBonus: .2, postCapDamageFactor: .5);
            Check.Near(90, capped.NonCriticalDamage);
            Check.Near(.35, capped.CriticalChance);
            Check.Near(2, capped.CriticalMultiplier);
            Check.Near(180, capped.Resolve(.3499).Damage);
            Check.Near(90, capped.Resolve(.35).Damage);

            var floor = PermanentOriginalHitRules.Evaluate(ranks, context, .9,
                postCapDamageFactor: .25, guaranteedCritical: true);
            Check.Near(1, floor.NonCriticalDamage);
            Check.Near(1.9, floor.Resolve().Damage);
        });

        test("Permanent original hit rolls are explicit, bounded and never trigger at equal probability", () =>
        {
            var noChance = PermanentOriginalHitRules.Evaluate(new Dictionary<string, int>(), Hit(), 10);
            Check.True(!noChance.RequiresCriticalRoll);
            Check.True(!noChance.Resolve().IsCritical);
            Check.True(!noChance.Resolve(0).IsCritical);
            var random = PermanentOriginalHitRules.Evaluate(One("common.crit.01"), Hit(), 10);
            Check.True(random.RequiresCriticalRoll);
            Check.True(random.Resolve(0).IsCritical);
            Check.True(random.Resolve(.0299).IsCritical);
            Check.True(!random.Resolve(.03).IsCritical);
            Check.True(!random.Resolve(.0301).IsCritical);
            Check.Throws<ArgumentException>(() => random.Resolve());
            Check.Throws<ArgumentOutOfRangeException>(() => random.Resolve(-.01));
            Check.Throws<ArgumentOutOfRangeException>(() => random.Resolve(1));
            Check.Throws<ArgumentOutOfRangeException>(() => random.Resolve(double.NaN));

            var guaranteed = PermanentOriginalHitRules.Evaluate(new Dictionary<string, int>(), Hit(), 10,
                criticalMultiplierBonus: 1, guaranteedCritical: true);
            Check.True(!guaranteed.RequiresCriticalRoll);
            Check.True(guaranteed.Resolve().IsCritical);
            Check.True(guaranteed.Resolve(.9999).IsCritical);
            Check.Near(20, guaranteed.ExpectedDamage);
        });
    }

    private static void Power(string suffix, double extraDamage, params PermanentOriginalHitContext[] inputs)
    {
        var ranks = One("common.power." + suffix);
        foreach (var input in inputs)
            Check.Near(100 + extraDamage, PermanentOriginalHitRules.Evaluate(ranks, input, 100).NonCriticalDamage);
    }

    private static void Critical(string suffix, double probability, double multiplier,
        params PermanentOriginalHitContext[] inputs)
    {
        var ranks = One("common.crit." + suffix);
        foreach (var input in inputs)
        {
            var result = PermanentOriginalHitRules.Evaluate(ranks, input, 100);
            Check.Near(probability, result.CriticalChance);
            Check.Near(multiplier, result.CriticalMultiplier);
        }
    }

    private static Dictionary<string, int> One(string id) => new Dictionary<string, int> { [id] = 1 };

    private static PermanentOriginalHitContext Hit(double target = 1, double player = .5, bool first = false,
        int ordinal = 1, bool elite = false, bool boss = false, double? hurt = null,
        double distance = 0, double still = 0, bool mark = false) =>
        new PermanentOriginalHitContext(target, player, first, ordinal, elite, boss, hurt, distance, still, mark);
}
