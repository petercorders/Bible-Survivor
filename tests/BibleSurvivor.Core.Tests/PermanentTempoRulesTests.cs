using System;
using System.Collections.Generic;

namespace BibleSurvivor.Core.Tests;

internal static class PermanentTempoRulesTests
{
    public static void Register(Action<string, Action> test)
    {
        test("Common tempo opening, hurt, elite and boss effects apply at their event boundaries", () =>
        {
            CycleReduction("02", .15, Window(afterFirst: true));
            CycleReduction("02", 0, Window(afterFirst: false));
            Check.Near(2.55, PermanentTempoRules.Evaluate(One("common.tempo.02"), Window(afterFirst: true)).Wait(3));
            Check.Near(3, PermanentTempoRules.Evaluate(One("common.tempo.02"), Window()).Wait(3));
            CycleReduction("03", .03, Window(hurt: 0), Window(hurt: Math.BitDecrement(3)), Window(hurt: 3));
            CycleReduction("03", 0, Window(hurt: Math.BitIncrement(3)), Window(hurt: null));
            CycleReduction("04", .04, Window(round: 0), Window(round: Math.BitDecrement(10)), Window(round: 10));
            CycleReduction("04", 0, Window(round: Math.BitIncrement(10)));
            CycleReduction("08", .05, Window(eliteKill: 0), Window(eliteKill: Math.BitDecrement(3)), Window(eliteKill: 3));
            CycleReduction("08", 0, Window(eliteKill: Math.BitIncrement(3)), Window(eliteKill: null));
            CycleReduction("10", .05, Window(boss: true));
            CycleReduction("10", 0, Window(boss: false));
        });

        test("Common tempo separates same-challenge no-hit time from the current round", () =>
        {
            CycleReduction("06", 0, Window(round: 1, challengeNoHit: 100), Window(round: Math.BitDecrement(60)));
            CycleReduction("06", .04, Window(round: 60), Window(round: Math.BitIncrement(60)));
            CycleReduction("07", 0, Window(round: 70, challengeNoHit: Math.BitDecrement(10)));
            CycleReduction("07", .04, Window(round: 1, challengeNoHit: 10), Window(round: 1, challengeNoHit: Math.BitIncrement(10)));
            Check.Throws<ArgumentException>(() => PermanentTempoRules.Evaluate(One("common.tempo.03"), Window(round: 1, hurt: 2)));
            Check.Throws<ArgumentException>(() => PermanentTempoRules.Evaluate(One("common.tempo.08"), Window(round: 1, eliteKill: 2)));
        });

        test("Common tempo adds rank, thunder and external reductions before preserving cycle floors", () =>
        {
            var ranks = new Dictionary<string, int> { ["cooldown"] = 3 };
            for (var number = 2; number <= 10; number++) ranks.Add($"common.tempo.{number:00}", 1);
            var context = Window(afterFirst: true, round: 10, hurt: 3, challengeNoHit: 12, eliteKill: 3, boss: true);
            var profile = PermanentTempoRules.Evaluate(ranks, context, thunderRank: 2, otherCycleReduction: .02);
            Check.Near(.60, profile.CycleReduction);
            Check.Near(1.2, profile.Wait(3));
            Check.Near(3.2, profile.Cycle(2, 3));
            Check.Near(2, profile.Cycle(2, 0));
            Check.Near(2, profile.Cycle(2, 1, advanceSeconds: .55));

            var over = PermanentTempoRules.Evaluate(ranks, context, thunderRank: 2, otherCycleReduction: .5);
            Check.Near(.8, over.CycleReduction);
            Check.Near(.6, over.Wait(3));
            Check.Near(.15, over.Cycle(0, .1, advanceSeconds: .1));
            Check.Near(.3 + 1d / 60, over.Cycle(0, 1, lastBurstOffset: .3));
            Check.Near(.3 + 1d / 60, over.Cycle(0, 1, lastBurstOffset: .3, advanceSeconds: 1));
        });

        test("Common tempo distinguishes burst and fixed-hit reductions and interval floors", () =>
        {
            var fixedOnly = PermanentTempoRules.Evaluate(One("common.tempo.05"), Window());
            Check.Near(.96, fixedOnly.FixedHitInterval(1));
            Check.Near(1, fixedOnly.BurstInterval(1));
            var burstOnly = PermanentTempoRules.Evaluate(One("common.tempo.09"), Window());
            Check.Near(.92, burstOnly.BurstInterval(1));
            Check.Near(1, burstOnly.FixedHitInterval(1));
            var ranks = new Dictionary<string, int> { ["common.tempo.05"] = 1, ["common.tempo.09"] = 1 };
            var capped = PermanentTempoRules.Evaluate(ranks, Window(), otherBurstReduction: .30, otherFixedHitReduction: .40);
            Check.Near(.25, capped.BurstReduction);
            Check.Near(.20, capped.FixedHitReduction);
            Check.Near(.075, capped.BurstInterval(.1));
            Check.Near(.05, capped.BurstInterval(.05));
            Check.Near(.4, capped.FixedHitInterval(.5));
            Check.Near(.4, capped.FixedHitInterval(.45));
        });
    }

    private static void CycleReduction(string suffix, double reduction, params PermanentTempoContext[] contexts)
    {
        var ranks = One("common.tempo." + suffix);
        foreach (var context in contexts)
            Check.Near(reduction, PermanentTempoRules.Evaluate(ranks, context).CycleReduction);
    }

    private static Dictionary<string, int> One(string id) => new Dictionary<string, int> { [id] = 1 };

    private static PermanentTempoContext Window(bool afterFirst = false, double round = 30, double? hurt = null,
        double challengeNoHit = 0, double? eliteKill = null, bool boss = false) =>
        new PermanentTempoContext(afterFirst, round, hurt, challengeNoHit, eliteKill, boss);
}
