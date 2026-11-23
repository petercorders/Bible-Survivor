using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Json;

namespace BibleSurvivor.Core.Tests;

internal static class PermanentRecoveryRulesTests
{
    public static void Register(Action<string, Action> test)
    {
        test("Common regeneration applies each passive at the health, no-hit and stationary boundary", () =>
        {
            var ranks = new Dictionary<string, int>
            {
                ["regen"] = 3, ["common.regen.02"] = 1, ["common.regen.03"] = 1,
                ["common.regen.04"] = 1, ["common.regen.07"] = 1, ["common.regen.10"] = 1,
            };
            Check.Near(.6, PermanentRecoveryRules.Rate(ranks, 7.999, .3001, .999));
            Check.Near(.7, PermanentRecoveryRules.Rate(ranks, 8, .3001, .999));
            Check.Near(.7, PermanentRecoveryRules.Rate(ranks, 7.999, .30, .999));
            Check.Near(.7, PermanentRecoveryRules.Rate(ranks, 7.999, .3001, 1));
            Check.Near(.9, PermanentRecoveryRules.Rate(ranks, 8.001, .2999, 1.001));
            Check.Near(1, PermanentRecoveryRules.Rate(ranks, 8, .30, 1, otherRate: .2));
            Check.Near(.2, PermanentRecoveryRules.Rate(new Dictionary<string, int> { ["regen"] = 2 }, 0, 1, 0));
        });

        test("Tenth normal original kill heals once across resume and does not defer a full-health event", () =>
        {
            var state = new PermanentRecoveryState();
            var ranks = Ranks("05");
            for (var index = 0; index < 9; index++)
                Check.Near(0, Kill(state, ranks, index, health: 70));
            Check.Near(0, Kill(state, ranks, 9, original: false, health: 70));
            Check.Near(0, Kill(state, ranks, 9, kind: PermanentRecoveryKillKind.BossSupport, health: 70));
            Check.Near(0, Kill(state, ranks, 9, kind: PermanentRecoveryKillKind.Elite, health: 70));
            Check.Equal(9, state.NormalKills);
            var resumed = Restore(state);
            resumed.Validate(9);
            Check.Near(1, Kill(resumed, ranks, 9, health: 70));
            Check.Equal(10, resumed.NormalKills);
            Check.Near(0, Kill(resumed, ranks, 10, health: 70));
            Check.Equal(0, resumed.ImmediateHeals.Count);

            var full = state.Copy();
            Check.Near(0, Kill(full, ranks, 9, health: 100));
            Check.Equal(10, full.NormalKills);
            Check.Equal(0, full.ImmediateHeals.Count);
            Check.Near(0, Kill(full, ranks, 10, health: 60));
        });

        test("Elite and support cooldowns are separate, exact and reserve only the actual healing", () =>
        {
            var ranks = Ranks("06", "09", "08");
            var state = new PermanentRecoveryState();
            Check.Near(0, Kill(state, ranks, 0, kind: PermanentRecoveryKillKind.Elite, health: 100));
            Check.Equal(0, state.ImmediateHeals.Count);
            Check.Near(5, state.NextEliteAt);
            Check.Near(.25, Kill(state, ranks, 0, kind: PermanentRecoveryKillKind.BossSupport, health: 99.75));
            Check.Near(1.75, PermanentRecoveryRules.OnActualDamage(state, ranks, 0, 26, 25, 100));
            state.Validate(0);
            Check.Equal(2, state.ImmediateHeals.Count);
            Check.Near(0, Kill(state, ranks, Math.BitDecrement(5), kind: PermanentRecoveryKillKind.Elite, health: 70));
            Check.Near(1, Kill(state, ranks, 5, kind: PermanentRecoveryKillKind.Elite, health: 70));
            Check.Near(1, Kill(state, ranks, 5, kind: PermanentRecoveryKillKind.BossSupport, health: 71));
            Check.Near(10, state.NextBossSupportAt);
            Check.Near(10, state.NextEliteAt);
            state.Validate(5);
            Check.Throws<ArgumentOutOfRangeException>(() =>
                Kill(state, ranks, 4, kind: PermanentRecoveryKillKind.Elite, health: 60));
        });

        test("Rolling recovery budget expires exactly at one second and low health requires a surviving threshold crossing", () =>
        {
            var ranks = Ranks("06", "09", "08");
            var beforeCutoff = new PermanentRecoveryState();
            Check.Near(1, Kill(beforeCutoff, ranks, 0, kind: PermanentRecoveryKillKind.Elite, health: 30));
            Check.Near(1, Kill(beforeCutoff, ranks, 0, kind: PermanentRecoveryKillKind.BossSupport, health: 31));
            var atCutoff = Restore(beforeCutoff);
            Check.Near(0, PermanentRecoveryRules.OnActualDamage(beforeCutoff, ranks, Math.BitDecrement(1), 26, 25, 100));
            Check.Near(2, PermanentRecoveryRules.OnActualDamage(atCutoff, ranks, 1, 26, 25, 100));
            Check.Near(13, atCutoff.NextLowHealthAt);
            Check.Equal(1, atCutoff.ImmediateHeals.Count);
            Check.Throws<ArgumentException>(() =>
            {
                var corrupt = atCutoff.Copy();
                corrupt.ImmediateHeals[0].Amount = 3;
                corrupt.Validate(1);
            });
            Check.Near(2, atCutoff.ImmediateHeals[0].Amount);

            Check.Near(0, PermanentRecoveryRules.OnActualDamage(atCutoff, ranks, Math.BitDecrement(13), 26, 25, 100));
            Check.Near(0, PermanentRecoveryRules.OnActualDamage(atCutoff, ranks, 13, 25, 24, 100));
            Check.Near(0, PermanentRecoveryRules.OnActualDamage(atCutoff, ranks, 13, 26, 25.0001, 100));
            Check.Near(0, PermanentRecoveryRules.OnActualDamage(atCutoff, ranks, 13, 26, 0, 100));
            Check.Near(2, PermanentRecoveryRules.OnActualDamage(atCutoff, ranks, 13, 26, 24.9999, 100));
            atCutoff.Validate(13);
        });
    }

    private static double Kill(PermanentRecoveryState state, IReadOnlyDictionary<string, int> ranks,
        double now, PermanentRecoveryKillKind kind = PermanentRecoveryKillKind.Normal, bool original = true,
        double health = 80) =>
        PermanentRecoveryRules.OnKill(state, ranks, now, kind, original, health, 100);

    private static Dictionary<string, int> Ranks(params string[] suffixes)
    {
        var ranks = new Dictionary<string, int>();
        foreach (var suffix in suffixes) ranks.Add("common.regen." + suffix, 1);
        return ranks;
    }

    private static PermanentRecoveryState Restore(PermanentRecoveryState state)
    {
        using var stream = new MemoryStream();
        var serializer = new DataContractJsonSerializer(typeof(PermanentRecoveryState));
        serializer.WriteObject(stream, state);
        stream.Position = 0;
        return (PermanentRecoveryState)serializer.ReadObject(stream)!;
    }
}
