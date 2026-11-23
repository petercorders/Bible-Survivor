using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace BibleSurvivor.Core
{
    public readonly struct ExpandedWeaponLevel
    {
        public int Damage { get; }
        // Time from one attack start to the next. Duration does not extend the cycle.
        public double Cycle { get; }
        public int Amount { get; }
        // Dimensions use world units. Width spans the full attack and Radius spans its half.
        public double Radius { get; }
        public double Width { get; }
        public double Reach { get; }
        public double Duration { get; }
        public double Speed { get; }

        internal ExpandedWeaponLevel(int damage, double cycle, int amount, double radius = 0,
            double width = 0, double reach = 0, double duration = 0, double speed = 0)
        {
            Damage = damage;
            Cycle = cycle;
            Amount = amount;
            Radius = radius;
            Width = width;
            Reach = reach;
            Duration = duration;
            Speed = speed;
        }
    }

    public sealed class ExpandedWeaponProfile
    {
        private readonly ExpandedWeaponLevel[] levels;

        public string WeaponId { get; }
        public string Name { get; }
        public string EvolutionId { get; }
        public string EvolutionName { get; }
        public string PassiveId { get; }
        public int UnlockRound { get; }
        public bool UnlockRequiresBossSettlement { get; }
        // Earliest boss reward window when the prerequisite unlock is earned in this run.
        public int FirstEvolutionBossRound { get; }
        public int MaximumOriginalAmount { get; }
        public double FirstAttackDelay { get; }
        public double RepeatHitInterval { get; }
        public double TargetSearchRadius { get; }
        public double TravelDistance { get; }
        public double HalfAngleDegrees { get; }
        public double SpreadDegrees { get; }
        public double Knockback { get; }
        public double TargetReuseSpacing { get; }
        public double LeashDistance { get; }
        public double ReturnRadius { get; }
        public double ReturnTimeout { get; }
        public int HitsPerLeg { get; }
        public double MinimumRehitInterval { get; }
        public double ParallelSpacing { get; }

        internal ExpandedWeaponProfile(string weaponId, string name, string evolutionId, string evolutionName,
            string passiveId, int unlockRound, int firstEvolutionBossRound, int maximumOriginalAmount,
            ExpandedWeaponLevel[] levels, bool unlockRequiresBossSettlement = false,
            double firstAttackDelay = 0, double repeatHitInterval = 0, double targetSearchRadius = 0,
            double travelDistance = 0, double halfAngleDegrees = 0, double spreadDegrees = 0, double knockback = 0,
            double targetReuseSpacing = 0, double leashDistance = 0, double returnRadius = 0, double returnTimeout = 0,
            int hitsPerLeg = 0, double minimumRehitInterval = 0, double parallelSpacing = 0)
        {
            if (levels.Length != 8) throw new ArgumentException("Expanded weapons require eight levels", nameof(levels));
            this.levels = (ExpandedWeaponLevel[])levels.Clone();
            WeaponId = weaponId;
            Name = name;
            EvolutionId = evolutionId;
            EvolutionName = evolutionName;
            PassiveId = passiveId;
            UnlockRound = unlockRound;
            UnlockRequiresBossSettlement = unlockRequiresBossSettlement;
            FirstEvolutionBossRound = firstEvolutionBossRound;
            MaximumOriginalAmount = maximumOriginalAmount;
            FirstAttackDelay = firstAttackDelay;
            RepeatHitInterval = repeatHitInterval;
            TargetSearchRadius = targetSearchRadius;
            TravelDistance = travelDistance;
            HalfAngleDegrees = halfAngleDegrees;
            SpreadDegrees = spreadDegrees;
            Knockback = knockback;
            TargetReuseSpacing = targetReuseSpacing;
            LeashDistance = leashDistance;
            ReturnRadius = returnRadius;
            ReturnTimeout = returnTimeout;
            HitsPerLeg = hitsPerLeg;
            MinimumRehitInterval = minimumRehitInterval;
            ParallelSpacing = parallelSpacing;
        }

        public ExpandedWeaponLevel AtLevel(int level)
        {
            if (level < 1 || level > levels.Length) throw new ArgumentOutOfRangeException(nameof(level));
            return levels[level - 1];
        }
    }

    public static class ExpandedWeaponRules
    {
        internal const double MaximumResidualDurationBonus = .11;

        internal static double ResidualDurationBonus(IReadOnlyDictionary<string, int> ranks, bool evolved) =>
            (PermanentRules.Rank(ranks, "common.duration.02") > 0 ? .05 : 0) +
            (evolved && PermanentRules.Rank(ranks, "common.duration.04") > 0 ? .06 : 0);

        internal static bool ValidResidualDurationBonus(double amount) =>
            BattleSnapshot.Finite(amount) && amount >= 0 && amount <= MaximumResidualDurationBonus + 1e-9;

        // Base values and unlock metadata from the pinned combat-weapons tables.
        public static IReadOnlyList<ExpandedWeaponProfile> All { get; } = Array.AsReadOnly(new[]
        {
            new ExpandedWeaponProfile("wilderness_voice", "광야의 외침", "wilderness_proclamation", "광야의 선포",
                "underwear", unlockRound: 1, firstEvolutionBossRound: 3, maximumOriginalAmount: 1,
                levels: new[]
                {
                    new ExpandedWeaponLevel(6, 3, 1, reach: 3, duration: 1),
                    new ExpandedWeaponLevel(6, 3, 1, reach: 3.75, duration: 1),
                    new ExpandedWeaponLevel(6, 3, 1, reach: 3.75, duration: 1.5),
                    new ExpandedWeaponLevel(10, 3, 1, reach: 3.75, duration: 1.5),
                    new ExpandedWeaponLevel(10, 2.5, 1, reach: 3.75, duration: 1.5),
                    new ExpandedWeaponLevel(10, 2.5, 1, reach: 4.5, duration: 1.5),
                    new ExpandedWeaponLevel(10, 2.5, 1, reach: 4.5, duration: 2),
                    new ExpandedWeaponLevel(15, 2.5, 1, reach: 4.5, duration: 2),
                }, firstAttackDelay: .15, repeatHitInterval: .5, halfAngleDegrees: 45, knockback: .1),

            new ExpandedWeaponProfile("lightning", "하늘의 번개", "thunder_judgment", "우레의 심판",
                "thunder", unlockRound: 2, firstEvolutionBossRound: 3, maximumOriginalAmount: 4,
                levels: new[]
                {
                    new ExpandedWeaponLevel(20, 4, 1, radius: .8),
                    new ExpandedWeaponLevel(20, 4, 2, radius: .8),
                    new ExpandedWeaponLevel(20, 3.5, 2, radius: .8),
                    new ExpandedWeaponLevel(30, 3.5, 2, radius: .8),
                    new ExpandedWeaponLevel(30, 3.5, 3, radius: .8),
                    new ExpandedWeaponLevel(30, 3.5, 3, radius: 1),
                    new ExpandedWeaponLevel(30, 3, 3, radius: 1),
                    new ExpandedWeaponLevel(40, 3, 3, radius: 1),
                }, firstAttackDelay: .15, targetSearchRadius: 8),

            new ExpandedWeaponProfile("moses_staff", "모세의 지팡이", "red_sea_path", "홍해의 길",
                "east_wind", unlockRound: 3, firstEvolutionBossRound: 6, maximumOriginalAmount: 4,
                levels: new[]
                {
                    new ExpandedWeaponLevel(20, 3, 1, width: .9, speed: 5),
                    new ExpandedWeaponLevel(20, 3, 2, width: .9, speed: 5),
                    new ExpandedWeaponLevel(20, 3, 2, width: 1.08, speed: 5),
                    new ExpandedWeaponLevel(30, 3, 2, width: 1.08, speed: 5),
                    new ExpandedWeaponLevel(30, 2.5, 2, width: 1.08, speed: 5),
                    new ExpandedWeaponLevel(30, 2.5, 3, width: 1.08, speed: 5),
                    new ExpandedWeaponLevel(30, 2.5, 3, width: 1.08, speed: 6.5),
                    new ExpandedWeaponLevel(40, 2.5, 3, width: 1.08, speed: 6.5),
                }, unlockRequiresBossSettlement: true, travelDistance: 7, spreadDegrees: 15, knockback: .2),

            new ExpandedWeaponProfile("fire_pillar", "불기둥", "wilderness_guidance", "광야의 인도",
                "cloud", unlockRound: 4, firstEvolutionBossRound: 6, maximumOriginalAmount: 3,
                levels: new[]
                {
                    new ExpandedWeaponLevel(10, 3, 1, radius: .9, duration: 2),
                    new ExpandedWeaponLevel(10, 3, 1, radius: .9, duration: 2.5),
                    new ExpandedWeaponLevel(10, 3, 1, radius: 1.125, duration: 2.5),
                    new ExpandedWeaponLevel(15, 3, 1, radius: 1.125, duration: 2.5),
                    new ExpandedWeaponLevel(15, 3, 2, radius: 1.125, duration: 2.5),
                    new ExpandedWeaponLevel(15, 2.5, 2, radius: 1.125, duration: 2.5),
                    new ExpandedWeaponLevel(15, 2.5, 2, radius: 1.125, duration: 3),
                    new ExpandedWeaponLevel(20, 2.5, 2, radius: 1.125, duration: 3),
                }, firstAttackDelay: .15, repeatHitInterval: .5, targetSearchRadius: 7, targetReuseSpacing: .6),

            new ExpandedWeaponProfile("daniel_lion", "다니엘의 사자", "den_guardian", "사자굴의 수호자",
                "seal", unlockRound: 5, firstEvolutionBossRound: 6, maximumOriginalAmount: 3,
                levels: new[]
                {
                    new ExpandedWeaponLevel(20, 1.5, 1, radius: .65, speed: 4),
                    new ExpandedWeaponLevel(25, 1.5, 1, radius: .65, speed: 4),
                    new ExpandedWeaponLevel(25, 1.3, 1, radius: .65, speed: 4),
                    new ExpandedWeaponLevel(25, 1.3, 2, radius: .65, speed: 4),
                    new ExpandedWeaponLevel(35, 1.3, 2, radius: .65, speed: 4),
                    new ExpandedWeaponLevel(35, 1.3, 2, radius: .65, speed: 5),
                    new ExpandedWeaponLevel(35, 1, 2, radius: .65, speed: 5),
                    new ExpandedWeaponLevel(50, 1, 2, radius: .65, speed: 5),
                }, targetSearchRadius: 7, leashDistance: 7, returnRadius: 1),

            new ExpandedWeaponProfile("cross", "십자가", "passion_radiance", "수난의 광휘",
                "crown", unlockRound: 6, firstEvolutionBossRound: 9, maximumOriginalAmount: 3,
                levels: new[]
                {
                    new ExpandedWeaponLevel(5, 2, 1, radius: .28, speed: 6),
                    new ExpandedWeaponLevel(10, 2, 1, radius: .28, speed: 6),
                    new ExpandedWeaponLevel(10, 2, 1, radius: .28, speed: 7.5),
                    new ExpandedWeaponLevel(10, 2, 2, radius: .28, speed: 7.5),
                    new ExpandedWeaponLevel(20, 2, 2, radius: .28, speed: 7.5),
                    new ExpandedWeaponLevel(20, 2, 2, radius: .35, speed: 7.5),
                    new ExpandedWeaponLevel(20, 1.5, 2, radius: .35, speed: 7.5),
                    new ExpandedWeaponLevel(30, 1.5, 2, radius: .35, speed: 7.5),
                }, unlockRequiresBossSettlement: true, travelDistance: 5, spreadDegrees: 12,
                returnRadius: .35, returnTimeout: 3, hitsPerLeg: 4, minimumRehitInterval: .5),

            new ExpandedWeaponProfile("elijah_chariot", "엘리야의 불수레", "ascending_chariot", "승천의 불수레",
                "whirlwind", unlockRound: 7, firstEvolutionBossRound: 9, maximumOriginalAmount: 4,
                levels: new[]
                {
                    new ExpandedWeaponLevel(40, 6, 1, width: 1.2, speed: 8),
                    new ExpandedWeaponLevel(40, 5.5, 1, width: 1.2, speed: 8),
                    new ExpandedWeaponLevel(40, 5.5, 1, width: 1.5, speed: 8),
                    new ExpandedWeaponLevel(60, 5.5, 1, width: 1.5, speed: 8),
                    new ExpandedWeaponLevel(60, 5.5, 2, width: 1.5, speed: 8),
                    new ExpandedWeaponLevel(60, 5, 2, width: 1.5, speed: 8),
                    new ExpandedWeaponLevel(60, 5, 2, width: 1.8, speed: 8),
                    new ExpandedWeaponLevel(80, 5, 2, width: 1.8, speed: 8),
                }, firstAttackDelay: .15, travelDistance: 10, parallelSpacing: .9),
        });

        private static readonly Dictionary<string, ExpandedWeaponProfile> byWeapon =
            All.ToDictionary(profile => profile.WeaponId, StringComparer.Ordinal);
        private static readonly Dictionary<string, ExpandedWeaponProfile> byEvolution =
            All.ToDictionary(profile => profile.EvolutionId, StringComparer.Ordinal);

        public static ExpandedWeaponProfile Get(string weaponId) => TryGet(weaponId, out var profile)
            ? profile
            : throw new ArgumentException("Unknown expanded weapon", nameof(weaponId));

        public static bool TryGet(string weaponId, [NotNullWhen(true)] out ExpandedWeaponProfile? profile) =>
            byWeapon.TryGetValue(weaponId, out profile);

        public static bool TryGetByEvolution(string evolutionId, [NotNullWhen(true)] out ExpandedWeaponProfile? profile) =>
            byEvolution.TryGetValue(evolutionId, out profile);
    }
}
