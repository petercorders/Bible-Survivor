using System;
using System.Collections.Generic;

namespace BibleSurvivor.Core
{
    public readonly struct PermanentOriginalHitContext
    {
        public double TargetHealthRatio { get; }
        public double PlayerHealthRatio { get; }
        public bool FirstHitOfCycle { get; }
        public int TargetOrdinal { get; }
        public bool IsElite { get; }
        public bool IsBoss { get; }
        public double? SecondsSinceHurt { get; }
        public double Distance { get; }
        public double StationarySeconds { get; }
        public bool HasConsecutiveMark { get; }

        public PermanentOriginalHitContext(double targetHealthRatio, double playerHealthRatio, bool firstHitOfCycle,
            int targetOrdinal, bool isElite, bool isBoss, double? secondsSinceHurt, double distance,
            double stationarySeconds, bool hasConsecutiveMark)
        {
            TargetHealthRatio = targetHealthRatio;
            PlayerHealthRatio = playerHealthRatio;
            FirstHitOfCycle = firstHitOfCycle;
            TargetOrdinal = targetOrdinal;
            IsElite = isElite;
            IsBoss = isBoss;
            SecondsSinceHurt = secondsSinceHurt;
            Distance = distance;
            StationarySeconds = stationarySeconds;
            HasConsecutiveMark = hasConsecutiveMark;
        }

        internal void Validate()
        {
            if (!PermanentOriginalHitRules.NonNegative(TargetHealthRatio) || TargetHealthRatio > 1 ||
                !PermanentOriginalHitRules.NonNegative(PlayerHealthRatio) || PlayerHealthRatio > 1 ||
                TargetOrdinal < 1 || (SecondsSinceHurt.HasValue && !PermanentOriginalHitRules.NonNegative(SecondsSinceHurt.Value)) ||
                !PermanentOriginalHitRules.NonNegative(Distance) || !PermanentOriginalHitRules.NonNegative(StationarySeconds))
                throw new ArgumentException("Invalid permanent original-hit context");
        }
    }

    public readonly struct PermanentOriginalHitResult
    {
        public double Damage { get; }
        public bool IsCritical { get; }

        internal PermanentOriginalHitResult(double damage, bool isCritical)
        {
            Damage = damage;
            IsCritical = isCritical;
        }
    }

    public readonly struct PermanentOriginalHitProfile
    {
        public double NonCriticalDamage { get; }
        public double CriticalChance { get; }
        public double CriticalMultiplier { get; }
        public bool GuaranteedCritical { get; }
        public bool RequiresCriticalRoll => !GuaranteedCritical && CriticalChance > 0;
        public double ExpectedDamage => GuaranteedCritical ? NonCriticalDamage * CriticalMultiplier :
            NonCriticalDamage * (1 + CriticalChance * (CriticalMultiplier - 1));

        internal PermanentOriginalHitProfile(double nonCriticalDamage, double criticalChance,
            double criticalMultiplier, bool guaranteedCritical)
        {
            NonCriticalDamage = nonCriticalDamage;
            CriticalChance = criticalChance;
            CriticalMultiplier = criticalMultiplier;
            GuaranteedCritical = guaranteedCritical;
        }

        // Critical randomness is owned by the caller. Zero-chance and guaranteed
        // outcomes can resolve without consuming a roll.
        public PermanentOriginalHitResult Resolve(double? roll = null)
        {
            if (roll.HasValue && (!PermanentOriginalHitRules.NonNegative(roll.Value) || roll.Value >= 1))
                throw new ArgumentOutOfRangeException(nameof(roll));
            if (RequiresCriticalRoll && !roll.HasValue)
                throw new ArgumentException("This original hit needs a critical roll", nameof(roll));

            var critical = GuaranteedCritical || (roll.HasValue && roll.Value < CriticalChance);
            return new PermanentOriginalHitResult(
                critical ? NonCriticalDamage * CriticalMultiplier : NonCriticalDamage, critical);
        }
    }

    public static class PermanentOriginalHitRules
    {
        public static PermanentOriginalHitProfile Evaluate(IReadOnlyDictionary<string, int> ranks,
            PermanentOriginalHitContext context, double baseDamage, double damageBonus = 0,
            double criticalChanceBonus = 0, double criticalMultiplierBonus = 0, double postCapDamageFactor = 1,
            bool guaranteedCritical = false)
        {
            if (ranks == null) throw new ArgumentNullException(nameof(ranks));
            context.Validate();
            if (!NonNegative(baseDamage) || !NonNegative(damageBonus) || !NonNegative(criticalChanceBonus) ||
                !NonNegative(criticalMultiplierBonus) || !NonNegative(postCapDamageFactor))
                throw new ArgumentException("Invalid permanent original-hit modifiers");

            // DamageBonus already includes common attack ranks, weapon mastery, Hair and other
            // additives known by the caller. The explicit original attenuation follows the cap.
            var nonCriticalDamage = Math.Max(1, baseDamage * Math.Min(2.25, 1 + damageBonus + PowerBonus(ranks, context))
                * postCapDamageFactor);
            var chance = Math.Min(.35, criticalChanceBonus + ChanceBonus(ranks, context));
            var multiplier = Math.Min(2, 1.5 + criticalMultiplierBonus + MultiplierBonus(ranks, context));
            if (!NonNegative(nonCriticalDamage * multiplier))
                throw new ArgumentException("Permanent original-hit damage exceeds numeric bounds");

            return new PermanentOriginalHitProfile(nonCriticalDamage, chance, multiplier, guaranteedCritical);
        }

        private static double PowerBonus(IReadOnlyDictionary<string, int> ranks, PermanentOriginalHitContext context)
        {
            var bonus = 0d;
            if ((context.IsElite || context.IsBoss) && Has(ranks, "common.power.02")) bonus += .05;
            if (context.FirstHitOfCycle && Has(ranks, "common.power.03")) bonus += .06;
            if (context.TargetHealthRatio <= .25 && Has(ranks, "common.power.04")) bonus += .06;
            if (context.PlayerHealthRatio >= .90 && Has(ranks, "common.power.05")) bonus += .05;
            if (context.TargetOrdinal >= 3 && Has(ranks, "common.power.06")) bonus += .06;
            if (context.SecondsSinceHurt is double hurt && hurt <= 3 && Has(ranks, "common.power.07")) bonus += .07;
            if (context.Distance >= 4 && Has(ranks, "common.power.08")) bonus += .05;
            if (context.HasConsecutiveMark && Has(ranks, "common.power.09")) bonus += .07;
            if (context.IsBoss && context.TargetHealthRatio <= .25 && Has(ranks, "common.power.10")) bonus += .10;
            return bonus;
        }

        private static double ChanceBonus(IReadOnlyDictionary<string, int> ranks, PermanentOriginalHitContext context)
        {
            var bonus = 0d;
            if (Has(ranks, "common.crit.01")) bonus += .03;
            if (context.FirstHitOfCycle && Has(ranks, "common.crit.03")) bonus += .05;
            if ((context.IsElite || context.IsBoss) && Has(ranks, "common.crit.04")) bonus += .04;
            if (context.PlayerHealthRatio >= .90 && Has(ranks, "common.crit.06")) bonus += .04;
            if (context.Distance >= 4 && Has(ranks, "common.crit.07")) bonus += .04;
            if (context.StationarySeconds >= 1 && Has(ranks, "common.crit.08")) bonus += .05;
            if (context.IsBoss && context.TargetHealthRatio <= .25 && Has(ranks, "common.crit.10")) bonus += .08;
            return bonus;
        }

        private static double MultiplierBonus(IReadOnlyDictionary<string, int> ranks, PermanentOriginalHitContext context)
        {
            var bonus = 0d;
            if (Has(ranks, "common.crit.02")) bonus += .10;
            if (context.TargetHealthRatio <= .25 && Has(ranks, "common.crit.05")) bonus += .15;
            if (context.HasConsecutiveMark && Has(ranks, "common.crit.09")) bonus += .15;
            return bonus;
        }

        private static bool Has(IReadOnlyDictionary<string, int> ranks, string id) =>
            ranks.TryGetValue(id, out var rank) && rank > 0;

        internal static bool NonNegative(double value) =>
            !double.IsNaN(value) && !double.IsInfinity(value) && value >= 0;
    }
}
