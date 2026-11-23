using System;
using System.Collections.Generic;
using System.Linq;
using BibleSurvivor.Core;

namespace BibleSurvivor.Core.Tests;

internal static class ExpandedWeaponRulesTests
{
    public static void Register(Action<string, Action> test)
    {
        test("Expanded weapons identify seven unique base forms, unlocks, and evolution windows", () =>
        {
            var expected = new[]
            {
                (Weapon: "wilderness_voice", Passive: "underwear", Evolution: "wilderness_proclamation", Unlock: 1, Boss: 3, Cap: 1, Settlement: false),
                (Weapon: "lightning", Passive: "thunder", Evolution: "thunder_judgment", Unlock: 2, Boss: 3, Cap: 4, Settlement: false),
                (Weapon: "moses_staff", Passive: "east_wind", Evolution: "red_sea_path", Unlock: 3, Boss: 6, Cap: 4, Settlement: true),
                (Weapon: "fire_pillar", Passive: "cloud", Evolution: "wilderness_guidance", Unlock: 4, Boss: 6, Cap: 3, Settlement: false),
                (Weapon: "daniel_lion", Passive: "seal", Evolution: "den_guardian", Unlock: 5, Boss: 6, Cap: 3, Settlement: false),
                (Weapon: "cross", Passive: "crown", Evolution: "passion_radiance", Unlock: 6, Boss: 9, Cap: 3, Settlement: true),
                (Weapon: "elijah_chariot", Passive: "whirlwind", Evolution: "ascending_chariot", Unlock: 7, Boss: 9, Cap: 4, Settlement: false),
            };
            Check.Equal(7, ExpandedWeaponRules.All.Count);
            Check.True(expected.Select(row => row.Weapon).SequenceEqual(ExpandedWeaponRules.All.Select(profile => profile.WeaponId)));
            foreach (var row in expected)
            {
                var profile = ExpandedWeaponRules.Get(row.Weapon);
                Check.Equal(row.Passive, profile.PassiveId);
                Check.Equal(row.Evolution, profile.EvolutionId);
                Check.Equal(row.Unlock, profile.UnlockRound);
                Check.Equal(row.Boss, profile.FirstEvolutionBossRound);
                Check.Equal(row.Cap, profile.MaximumOriginalAmount);
                Check.Equal(row.Settlement, profile.UnlockRequiresBossSettlement);
                Check.True(ExpandedWeaponRules.TryGetByEvolution(row.Evolution, out var evolved) && profile == evolved);
                Check.True(!ExpandedWeaponRules.TryGetByEvolution(row.Weapon, out _));
            }
            Check.True(!ExpandedWeaponRules.TryGet("sling", out _));
            Check.True(!ExpandedWeaponRules.TryGet("Cross", out _));
            Check.Throws<ArgumentException>(() => ExpandedWeaponRules.Get("unknown"));
            Check.Throws<ArgumentOutOfRangeException>(() => ExpandedWeaponRules.Get("cross").AtLevel(0));
            Check.Throws<ArgumentOutOfRangeException>(() => ExpandedWeaponRules.Get("cross").AtLevel(9));
            Check.Throws<NotSupportedException>(() => ((IList<ExpandedWeaponProfile>)ExpandedWeaponRules.All).Clear());
        });

        test("Expanded weapon damage, cycles, quantity, geometry and speed match all eight levels", () =>
        {
            Match("wilderness_voice", new[] { 6, 6, 6, 10, 10, 10, 10, 15 }, new[] { 3, 3, 3, 3, 2.5, 2.5, 2.5, 2.5 },
                new[] { 1, 1, 1, 1, 1, 1, 1, 1 }, reach: new[] { 3, 3.75, 3.75, 3.75, 3.75, 4.5, 4.5, 4.5 },
                duration: new[] { 1, 1, 1.5, 1.5, 1.5, 1.5, 2, 2 });
            Match("lightning", new[] { 20, 20, 20, 30, 30, 30, 30, 40 }, new[] { 4, 4, 3.5, 3.5, 3.5, 3.5, 3, 3 },
                new[] { 1, 2, 2, 2, 3, 3, 3, 3 }, radius: new[] { .8, .8, .8, .8, .8, 1, 1, 1 });
            Match("moses_staff", new[] { 20, 20, 20, 30, 30, 30, 30, 40 }, new[] { 3, 3, 3, 3, 2.5, 2.5, 2.5, 2.5 },
                new[] { 1, 2, 2, 2, 2, 3, 3, 3 }, width: new[] { .9, .9, 1.08, 1.08, 1.08, 1.08, 1.08, 1.08 },
                speed: new[] { 5, 5, 5, 5, 5, 5, 6.5, 6.5 });
            Match("fire_pillar", new[] { 10, 10, 10, 15, 15, 15, 15, 20 }, new[] { 3, 3, 3, 3, 3, 2.5, 2.5, 2.5 },
                new[] { 1, 1, 1, 1, 2, 2, 2, 2 }, radius: new[] { .9, .9, 1.125, 1.125, 1.125, 1.125, 1.125, 1.125 },
                duration: new[] { 2, 2.5, 2.5, 2.5, 2.5, 2.5, 3, 3 });
            Match("daniel_lion", new[] { 20, 25, 25, 25, 35, 35, 35, 50 }, new[] { 1.5, 1.5, 1.3, 1.3, 1.3, 1.3, 1, 1 },
                new[] { 1, 1, 1, 2, 2, 2, 2, 2 }, radius: new[] { .65, .65, .65, .65, .65, .65, .65, .65 },
                speed: new double[] { 4, 4, 4, 4, 4, 5, 5, 5 });
            Match("cross", new[] { 5, 10, 10, 10, 20, 20, 20, 30 }, new[] { 2, 2, 2, 2, 2, 2, 1.5, 1.5 },
                new[] { 1, 1, 1, 2, 2, 2, 2, 2 }, radius: new[] { .28, .28, .28, .28, .28, .35, .35, .35 },
                speed: new[] { 6, 6, 7.5, 7.5, 7.5, 7.5, 7.5, 7.5 });
            Match("elijah_chariot", new[] { 40, 40, 40, 60, 60, 60, 60, 80 }, new[] { 6, 5.5, 5.5, 5.5, 5.5, 5, 5, 5 },
                new[] { 1, 1, 1, 1, 2, 2, 2, 2 }, width: new[] { 1.2, 1.2, 1.5, 1.5, 1.5, 1.5, 1.8, 1.8 },
                speed: new double[] { 8, 8, 8, 8, 8, 8, 8, 8 });
        });

        test("Expanded weapon first attacks, repeat timings and fixed collision geometry match the profiles", () =>
        {
            var voice = ExpandedWeaponRules.Get("wilderness_voice");
            Check.Near(.15, voice.FirstAttackDelay);
            Check.Near(.5, voice.RepeatHitInterval);
            Check.Near(45, voice.HalfAngleDegrees);
            Check.Near(.1, voice.Knockback);

            var lightning = ExpandedWeaponRules.Get("lightning");
            Check.Near(.15, lightning.FirstAttackDelay);
            Check.Near(8, lightning.TargetSearchRadius);

            var staff = ExpandedWeaponRules.Get("moses_staff");
            Check.Near(0, staff.FirstAttackDelay);
            Check.Near(7, staff.TravelDistance);
            Check.Near(15, staff.SpreadDegrees);
            Check.Near(.2, staff.Knockback);

            var fire = ExpandedWeaponRules.Get("fire_pillar");
            Check.Near(.15, fire.FirstAttackDelay);
            Check.Near(.5, fire.RepeatHitInterval);
            Check.Near(7, fire.TargetSearchRadius);
            Check.Near(.6, fire.TargetReuseSpacing);

            var lion = ExpandedWeaponRules.Get("daniel_lion");
            Check.Near(0, lion.FirstAttackDelay);
            Check.Near(7, lion.TargetSearchRadius);
            Check.Near(7, lion.LeashDistance);
            Check.Near(1, lion.ReturnRadius);

            var cross = ExpandedWeaponRules.Get("cross");
            Check.Near(0, cross.FirstAttackDelay);
            Check.Near(12, cross.SpreadDegrees);
            Check.Near(5, cross.TravelDistance);
            Check.Near(.35, cross.ReturnRadius);
            Check.Near(3, cross.ReturnTimeout);
            Check.Equal(4, cross.HitsPerLeg);
            Check.Near(.5, cross.MinimumRehitInterval);

            var chariot = ExpandedWeaponRules.Get("elijah_chariot");
            Check.Near(.15, chariot.FirstAttackDelay);
            Check.Near(10, chariot.TravelDistance);
            Check.Near(.9, chariot.ParallelSpacing);
        });
    }

    private static void Match(string weaponId, int[] damage, double[] cycles, int[] amounts,
        double[]? radius = null, double[]? width = null, double[]? reach = null,
        double[]? duration = null, double[]? speed = null)
    {
        var profile = ExpandedWeaponRules.Get(weaponId);
        for (var index = 0; index < 8; index++)
        {
            var stats = profile.AtLevel(index + 1);
            Check.Equal(damage[index], stats.Damage);
            Check.Near(cycles[index], stats.Cycle);
            Check.Equal(amounts[index], stats.Amount);
            Check.Near(radius?[index] ?? 0, stats.Radius);
            Check.Near(width?[index] ?? 0, stats.Width);
            Check.Near(reach?[index] ?? 0, stats.Reach);
            Check.Near(duration?[index] ?? 0, stats.Duration);
            Check.Near(speed?[index] ?? 0, stats.Speed);
        }
    }
}
