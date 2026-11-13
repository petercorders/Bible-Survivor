using System;
using System.Collections.Generic;
using System.Linq;
using BibleSurvivor.Core;

namespace BibleSurvivor.Core.Tests
{
    public static class GrowthTests
    {
        public static void Register(Action<string, Action> test)
        {
            test("XP requirements preserve the 20/40 barriers", () =>
            {
                Check.Equal(5, ExperienceRules.Required(1));
                Check.Equal(185, ExperienceRules.Required(19));
                Check.Equal(795, ExperienceRules.Required(20));
                Check.Equal(208, ExperienceRules.Required(21));
                Check.Equal(2855, ExperienceRules.Required(40));
                Check.Equal(471, ExperienceRules.Required(41));
                Check.Throws<ArgumentOutOfRangeException>(() => ExperienceRules.Required(0));
                Check.Throws<OverflowException>(() => ExperienceRules.Required(int.MaxValue));
            });
            test("XP bonus is additive and ends inside a large pickup", () =>
            {
                var growth = new GrowthLedger(20);
                Check.Equal(1, growth.Add(400));
                Check.Equal(21, growth.Level);
                Check.Near(2.5, growth.Experience);
                Check.Near(2.49, ExperienceRules.GainMultiplier(20, 0.09, 0.40));
                Check.Near(1.49, ExperienceRules.GainMultiplier(21, 0.09, 0.40));
            });
            test("XP level-up batch retains fractional experience", () =>
            {
                var growth = new GrowthLedger();
                Check.Equal(7, growth.Add(300));
                Check.Equal(8, growth.Level);
                Check.Near(55, growth.Experience);
                var split = new GrowthLedger();
                for (var i = 0; i < 100; i++) split.Add(0.01, 0.03);
                Check.Near(1.03, split.Experience);
                var full = new GrowthLedger();
                full.Add(18600);
                Check.Equal(56, full.Level);
                Check.Near(50, full.Experience);
            });
            test("XP invalid and overflow inputs do not mutate the ledger", () =>
            {
                var growth = new GrowthLedger(3, 2);
                Check.Throws<ArgumentOutOfRangeException>(() => growth.Add(double.NaN));
                Check.Throws<ArgumentOutOfRangeException>(() => growth.Add(double.PositiveInfinity));
                Check.Throws<ArgumentOutOfRangeException>(() => growth.Add(-1));
                Check.Throws<ArgumentOutOfRangeException>(() => growth.Add(1, -0.01));
                Check.Throws<OverflowException>(() => growth.Add(double.MaxValue));
                Check.Equal(3, growth.Level);
                Check.Near(2, growth.Experience);
                Check.Throws<ArgumentOutOfRangeException>(() => new GrowthLedger(1, 5));
            });
            test("first three weapons have the documented cumulative levels", () =>
            {
                var bible = WeaponRules.Get("bible", 8);
                Check.Equal(30, bible.Damage);
                Check.Equal(4, bible.Amount);
                Check.Near(3, bible.Cooldown);
                Check.Near(4, bible.Duration);
                Check.Near(1.5, bible.AreaMultiplier);
                Check.Near(1.6, bible.RotationSpeedMultiplier);
                var jawbone = WeaponRules.Get("jawbone", 8);
                Check.Equal(40, jawbone.Damage);
                Check.Equal(2, jawbone.Amount);
                Check.Near(0.9, jawbone.Cooldown);
                Check.Near(1.4, jawbone.AreaMultiplier);
                Check.Near(0.15, jawbone.BurstInterval);
                Check.Near(120, jawbone.ArcDegrees);
                var sling = WeaponRules.Get("sling", 8);
                Check.Equal(30, sling.Damage);
                Check.Equal(4, sling.Amount);
                Check.Near(1, sling.Cooldown);
                Check.Equal(2, sling.Pierce);
                Check.Throws<ArgumentOutOfRangeException>(() => WeaponRules.Get("bible", 9));
                Check.Throws<ArgumentException>(() => WeaponRules.Get("voice", 1));
            });
            test("inventory offers unique deterministic legal choices", () =>
            {
                var gear = new EquipmentInventory("sling");
                var choices = gear.Choices(new[] { "bible", "jawbone", "sling", "sling" }, new[] { "scroll", "stones", "scroll" }, 3, 71);
                Check.Equal(3, choices.Count);
                Check.Equal(3, choices.Distinct().Count());
                Check.True(choices.SequenceEqual(gear.Choices(new[] { "sling", "jawbone", "bible" }, new[] { "stones", "scroll" }, 3, 71)));
                Check.True(!gear.TryChoose("missing", false));
                Check.Throws<ArgumentOutOfRangeException>(() => gear.Choices(new[] { "sling" }, Array.Empty<string>(), 4, 0));
            });
            test("maxed gear and full passive slots are excluded without blocking upgrades", () =>
            {
                var gear = new EquipmentInventory("sling");
                for (var i = 1; i < 8; i++) Check.True(gear.TryChoose("sling", false));
                Check.True(!gear.TryChoose("sling", false));
                Check.True(gear.TryChoose("stones", true));
                Check.True(gear.TryChoose("stones", true));
                Check.True(!gear.TryChoose("stones", true));
                foreach (var id in new[] { "scroll", "hair", "east_wind", "seal", "cloud" }) Check.True(gear.TryChoose(id, true));
                Check.True(!gear.TryChoose("thunder", true));
                Check.True(gear.TryChoose("scroll", true));
                var options = gear.Choices(new[] { "sling" }, new[] { "stones", "thunder" }, 3, 0);
                Check.Equal(0, options.Count);
            });
            test("snapshot restore validates caps and cannot expose mutable dictionaries", () =>
            {
                var weapons = new Dictionary<string, int> { ["sling"] = 4 };
                var passives = new Dictionary<string, int> { ["stones"] = 1 };
                var gear = EquipmentInventory.Restore(weapons, passives);
                weapons["sling"] = 8;
                passives.Clear();
                Check.Equal(4, gear.Weapons["sling"]);
                Check.Equal(1, gear.Passives["stones"]);
                Check.Throws<NotSupportedException>(() => ((IDictionary<string, int>)gear.Weapons).Add("bible", 1));
                Check.Throws<ArgumentException>(() => EquipmentInventory.Restore(new Dictionary<string, int>(), passives));
                Check.Throws<ArgumentException>(() => EquipmentInventory.Restore(weapons, new Dictionary<string, int> { ["stones"] = 3 }));
            });
            test("inventory can acquire only the implemented starting weapon set", () =>
            {
                var gear = new EquipmentInventory("bible");
                Check.True(!gear.TryChoose("voice", false));
                var choices = gear.Choices(new[] { "bible", "voice" }, Array.Empty<string>(), 3, 1);
                Check.Equal(1, choices.Count);
                Check.Equal("bible", choices[0]);
                Check.Throws<ArgumentException>(() => new EquipmentInventory("voice"));
            });
        }
    }
}
