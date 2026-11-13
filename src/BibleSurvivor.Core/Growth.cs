using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace BibleSurvivor.Core
{
    public static class ExperienceRules
    {
        public static int Required(int level)
        {
            if (level < 1) throw new ArgumentOutOfRangeException(nameof(level));
            long value = level < 20 ? 5L + 10L * (level - 1)
                : level <= 40 ? 195L + 13L * (level - 20)
                : 455L + 16L * (level - 40);
            if (level == 20) value += 600;
            if (level == 40) value += 2400;
            if (value > int.MaxValue) throw new OverflowException("Experience requirement exceeds the supported integer range.");
            return (int)value;
        }

        public static double GainMultiplier(int level, double permanentGrowth, double passiveGrowth)
        {
            Required(level);
            ValidateBonus(permanentGrowth, nameof(permanentGrowth));
            ValidateBonus(passiveGrowth, nameof(passiveGrowth));
            var multiplier = 1 + permanentGrowth + passiveGrowth + (level == 20 || level == 40 ? 1 : 0);
            if (double.IsInfinity(multiplier)) throw new OverflowException("Growth bonuses overflow.");
            return multiplier;
        }

        private static void ValidateBonus(double value, string name)
        {
            if (double.IsNaN(value) || double.IsInfinity(value) || value < 0)
                throw new ArgumentOutOfRangeException(name);
        }
    }

    public sealed class GrowthLedger
    {
        public int Level { get; private set; }
        public double Experience { get; private set; }

        public GrowthLedger(int level = 1, double experience = 0)
        {
            var requirement = ExperienceRules.Required(level);
            if (double.IsNaN(experience) || double.IsInfinity(experience) || experience < 0 || experience >= requirement)
                throw new ArgumentOutOfRangeException(nameof(experience));
            Level = level;
            Experience = experience;
        }

        public int Add(double rawExperience, double permanentGrowth = 0, double passiveGrowth = 0)
        {
            if (double.IsNaN(rawExperience) || double.IsInfinity(rawExperience) || rawExperience < 0)
                throw new ArgumentOutOfRangeException(nameof(rawExperience));
            ExperienceRules.GainMultiplier(Level, permanentGrowth, passiveGrowth);
            var level = Level;
            var experience = Experience;
            var remaining = rawExperience;

            // Bonuses apply while at a level, not to an entire arbitrarily sized pickup.
            // Consume only the raw XP needed to leave levels 20/40, then re-evaluate.
            // This makes one large pickup equivalent to splitting the same raw XP.
            while (remaining > 0)
            {
                var multiplier = ExperienceRules.GainMultiplier(level, permanentGrowth, passiveGrowth);
                var available = remaining * multiplier + experience;
                if (double.IsInfinity(available)) throw new OverflowException("Experience gain overflows.");
                var required = ExperienceRules.Required(level);
                if (available < required)
                {
                    experience = available;
                    break;
                }

                // Search a linear-cost segment rather than iterating millions of levels
                // for an invalid or unusually large external input. All state stays local
                // until the operation is known to succeed.
                var end = level < 20 ? 19 : level == 20 ? 20 : level < 40 ? 39
                    : level == 40 ? 40 : (int.MaxValue - 455) / 16 + 40;
                var slope = level < 20 ? 10 : level <= 40 ? 13 : 16;
                if (level == 20 || level == 40) slope = 0;
                var low = 1;
                var high = checked(end - level + 1);
                while (low < high)
                {
                    var middle = low + (high - low + 1) / 2;
                    var cost = Cost(required, slope, middle);
                    if (cost <= available) low = middle;
                    else high = middle - 1;
                }
                var consumed = Cost(required, slope, low);
                remaining = Math.Max(0, (available - consumed) / multiplier);
                level = checked(level + low);
                ExperienceRules.Required(level);
                experience = 0;
            }

            var gained = level - Level;
            Level = level;
            Experience = experience;
            return gained;
        }

        private static double Cost(int first, int slope, int levels)
            => (double)levels * first + (double)slope * levels * (levels - 1) / 2;
    }

    public sealed class WeaponStats
    {
        public int Damage { get; }
        public int Amount { get; }
        public double Cooldown { get; }
        public double Duration { get; }
        public double AreaMultiplier { get; }
        public int Pierce { get; }
        public double RotationSpeedMultiplier { get; }
        public double HitInterval { get; }
        public double BurstInterval { get; }
        public double ArcDegrees { get; }

        internal WeaponStats(int damage, int amount, double cooldown, double duration,
            double areaMultiplier, int pierce, double rotationSpeedMultiplier,
            double hitInterval, double burstInterval, double arcDegrees)
        {
            Damage = damage;
            Amount = amount;
            Cooldown = cooldown;
            Duration = duration;
            AreaMultiplier = areaMultiplier;
            Pierce = pierce;
            RotationSpeedMultiplier = rotationSpeedMultiplier;
            HitInterval = hitInterval;
            BurstInterval = burstInterval;
            ArcDegrees = arcDegrees;
        }
    }

    public static class WeaponRules
    {
        public static WeaponStats Get(string weaponId, int level)
        {
            if (level < 1 || level > 8) throw new ArgumentOutOfRangeException(nameof(level));
            switch (weaponId)
            {
                case "bible":
                    return new WeaponStats(10 + (level >= 4 ? 10 : 0) + (level >= 7 ? 10 : 0),
                        1 + (level >= 2 ? 1 : 0) + (level >= 5 ? 1 : 0) + (level >= 8 ? 1 : 0),
                        3, 3 + (level >= 4 ? 0.5 : 0) + (level >= 7 ? 0.5 : 0),
                        1 + (level >= 3 ? 0.25 : 0) + (level >= 6 ? 0.25 : 0), int.MaxValue,
                        1 + (level >= 3 ? 0.30 : 0) + (level >= 6 ? 0.30 : 0), 1.7, 0, 360);
                case "jawbone":
                    return new WeaponStats(10 + (level >= 4 ? 10 : 0) + (level >= 6 ? 10 : 0) + (level >= 8 ? 10 : 0),
                        level >= 2 ? 2 : 1, level >= 5 ? 0.9 : 1, 0,
                        1 + (level >= 3 ? 0.2 : 0) + (level >= 7 ? 0.2 : 0), int.MaxValue,
                        1, 0, 0.15, 120);
                case "sling":
                    return new WeaponStats(10 + (level >= 5 ? 10 : 0) + (level >= 8 ? 10 : 0),
                        1 + (level >= 2 ? 1 : 0) + (level >= 4 ? 1 : 0) + (level >= 6 ? 1 : 0),
                        level >= 3 ? 1 : 1.2, 0, 1, level >= 7 ? 2 : 1,
                        1, 0, 0.1, 0);
                default:
                    throw new ArgumentException("Weapon has no implemented R01 attack profile.", nameof(weaponId));
            }
        }
    }

    public sealed class EquipmentInventory
    {
        private static readonly HashSet<string> ImplementedWeapons = new HashSet<string>(StringComparer.Ordinal)
        {
            "bible", "jawbone", "sling"
        };
        private static readonly HashSet<string> KnownPassives = new HashSet<string>(StringComparer.Ordinal)
        {
            "scroll", "hair", "stones", "east_wind", "seal", "cloud", "thunder", "crown", "underwear", "whirlwind"
        };
        private readonly Dictionary<string, int> weapons;
        private readonly Dictionary<string, int> passives;
        public IReadOnlyDictionary<string, int> Weapons { get; }
        public IReadOnlyDictionary<string, int> Passives { get; }

        public EquipmentInventory(string startingWeapon)
            : this(new Dictionary<string, int>(StringComparer.Ordinal) { [startingWeapon ?? ""] = 1 },
                new Dictionary<string, int>(StringComparer.Ordinal)) { }

        private EquipmentInventory(IReadOnlyDictionary<string, int> weaponLevels, IReadOnlyDictionary<string, int> passiveLevels)
        {
            if (weaponLevels == null) throw new ArgumentNullException(nameof(weaponLevels));
            if (passiveLevels == null) throw new ArgumentNullException(nameof(passiveLevels));
            if (weaponLevels.Count == 0 || weaponLevels.Count > 6 || passiveLevels.Count > 6)
                throw new ArgumentException("Inventory must have 1..6 weapons and 0..6 passives.");
            weapons = new Dictionary<string, int>(StringComparer.Ordinal);
            passives = new Dictionary<string, int>(StringComparer.Ordinal);
            foreach (var pair in weaponLevels)
            {
                if (!ImplementedWeapons.Contains(pair.Key) || pair.Value < 1 || pair.Value > 8)
                    throw new ArgumentException("Invalid or unimplemented weapon in inventory.", nameof(weaponLevels));
                weapons.Add(pair.Key, pair.Value);
            }
            foreach (var pair in passiveLevels)
            {
                if (!KnownPassives.Contains(pair.Key) || pair.Value < 1 || pair.Value > PassiveMaximum(pair.Key))
                    throw new ArgumentException("Invalid passive in inventory.", nameof(passiveLevels));
                passives.Add(pair.Key, pair.Value);
            }
            Weapons = new ReadOnlyDictionary<string, int>(weapons);
            Passives = new ReadOnlyDictionary<string, int>(passives);
        }

        public static EquipmentInventory Restore(IReadOnlyDictionary<string, int> weapons, IReadOnlyDictionary<string, int> passives)
            => new EquipmentInventory(weapons, passives);

        public bool TryChoose(string id, bool passive)
        {
            if (id == null || !(passive ? KnownPassives : ImplementedWeapons).Contains(id)) return false;
            var items = passive ? passives : weapons;
            var maximum = passive ? PassiveMaximum(id) : 8;
            if (items.TryGetValue(id, out var current))
            {
                if (current >= maximum) return false;
                items[id] = current + 1;
                return true;
            }
            if (items.Count >= 6) return false;
            items.Add(id, 1);
            return true;
        }

        public IReadOnlyList<string> Choices(IEnumerable<string> unlockedWeapons, IEnumerable<string> availablePassives, int count, int seed)
        {
            if (unlockedWeapons == null) throw new ArgumentNullException(nameof(unlockedWeapons));
            if (availablePassives == null) throw new ArgumentNullException(nameof(availablePassives));
            if (count < 0 || count > 3) throw new ArgumentOutOfRangeException(nameof(count));
            var choices = new HashSet<string>(StringComparer.Ordinal);
            foreach (var id in unlockedWeapons)
                if (id != null && ImplementedWeapons.Contains(id) && IsEligible(weapons, id, 8)) choices.Add(id);
            foreach (var id in availablePassives)
                if (id != null && KnownPassives.Contains(id) && IsEligible(passives, id, PassiveMaximum(id))) choices.Add(id);
            // Sorting makes enumeration order irrelevant; xorshift32 is stable across
            // .NET/Unity versions, unlike relying on a runtime's Random implementation.
            var sorted = choices.OrderBy(id => id, StringComparer.Ordinal).ToArray();
            var random = new ChoiceRandom(seed);
            for (var i = sorted.Length - 1; i > 0; i--)
            {
                var j = random.Next(i + 1);
                var swap = sorted[i];
                sorted[i] = sorted[j];
                sorted[j] = swap;
            }
            return Array.AsReadOnly(sorted.Take(count).ToArray());
        }

        private static int PassiveMaximum(string id) => id == "stones" ? 2 : 5;
        private static bool IsEligible(Dictionary<string, int> items, string id, int maximum)
            => items.TryGetValue(id, out var level) ? level < maximum : items.Count < 6;

        private sealed class ChoiceRandom
        {
            private uint state;
            public ChoiceRandom(int seed) { state = unchecked((uint)seed); if (state == 0) state = 0x9E3779B9; }
            private uint NextUInt()
            {
                state ^= state << 13;
                state ^= state >> 17;
                state ^= state << 5;
                return state;
            }
            public int Next(int bound)
            {
                var range = (uint)bound;
                var threshold = unchecked(0u - range) % range;
                uint value;
                do { value = NextUInt(); } while (value < threshold);
                return (int)(value % range);
            }
        }
    }
}
