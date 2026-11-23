using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

namespace BibleSurvivor.Core
{
    public enum PermanentRecoveryKillKind
    {
        Normal,
        Elite,
        BossSupport,
    }

    [DataContract]
    public sealed class PermanentRecoveryReceipt
    {
        [DataMember(IsRequired = true)] public double At { get; set; }
        [DataMember(IsRequired = true)] public double Amount { get; set; }

        internal PermanentRecoveryReceipt Copy() => (PermanentRecoveryReceipt)MemberwiseClone();
    }

    [DataContract]
    public sealed class PermanentRecoveryState
    {
        [DataMember(IsRequired = true)] public int NormalKills { get; set; }
        [DataMember(IsRequired = true)] public double NextEliteAt { get; set; }
        [DataMember(IsRequired = true)] public double NextBossSupportAt { get; set; }
        [DataMember(IsRequired = true)] public double NextLowHealthAt { get; set; }
        [DataMember(IsRequired = true)] public double LastEventAt { get; set; }
        [DataMember(IsRequired = true)] public List<PermanentRecoveryReceipt> ImmediateHeals { get; set; } =
            new List<PermanentRecoveryReceipt>();

        public PermanentRecoveryState Copy() => new PermanentRecoveryState
        {
            NormalKills = NormalKills,
            NextEliteAt = NextEliteAt,
            NextBossSupportAt = NextBossSupportAt,
            NextLowHealthAt = NextLowHealthAt,
            LastEventAt = LastEventAt,
            ImmediateHeals = ImmediateHeals.Select(heal => heal.Copy()).ToList(),
        };

        public void Validate(double now)
        {
            if (!PermanentRecoveryRules.NonNegative(now) || !PermanentRecoveryRules.NonNegative(LastEventAt) ||
                LastEventAt > now || NormalKills < 0 || NormalKills > PermanentRecoveryRules.NormalKillGoal ||
                !ValidCooldown(NextEliteAt, PermanentRecoveryRules.KillCooldown) ||
                !ValidCooldown(NextBossSupportAt, PermanentRecoveryRules.KillCooldown) ||
                !ValidCooldown(NextLowHealthAt, PermanentRecoveryRules.LowHealthCooldown) ||
                ImmediateHeals == null || ImmediateHeals.Count > PermanentRecoveryRules.MaximumImmediateSources)
                throw new ArgumentException("Invalid permanent recovery state");

            var previousAt = 0d;
            var spent = 0d;
            foreach (var heal in ImmediateHeals)
            {
                if (heal == null || !PermanentRecoveryRules.NonNegative(heal.At) || heal.At < previousAt ||
                    heal.At > LastEventAt || LastEventAt >= heal.At + PermanentRecoveryRules.BudgetPeriod ||
                    !PermanentRecoveryRules.NonNegative(heal.Amount) || heal.Amount <= 0 ||
                    heal.Amount > PermanentRecoveryRules.ImmediateBudget)
                    throw new ArgumentException("Invalid permanent recovery receipt");
                spent += heal.Amount;
                previousAt = heal.At;
            }
            if (spent > PermanentRecoveryRules.ImmediateBudget)
                throw new ArgumentException("Permanent recovery receipts exceed the shared budget");
        }

        private bool ValidCooldown(double at, double elapsed) =>
            PermanentRecoveryRules.NonNegative(at) && at <= LastEventAt + elapsed;
    }

    public static class PermanentRecoveryRules
    {
        internal const int NormalKillGoal = 10;
        internal const int MaximumImmediateSources = 4;
        internal const double KillCooldown = 5;
        internal const double LowHealthCooldown = 12;
        internal const double BudgetPeriod = 1;
        internal const double ImmediateBudget = 2;
        private const double LowHealthThreshold = .25;

        public static double Rate(IReadOnlyDictionary<string, int> ranks, double noHitSeconds,
            double healthRatio, double stationarySeconds, double otherRate = 0)
        {
            if (ranks == null) throw new ArgumentNullException(nameof(ranks));
            if (!NonNegative(noHitSeconds) || !NonNegative(healthRatio) || healthRatio > 1 ||
                !NonNegative(stationarySeconds) || !NonNegative(otherRate))
                throw new ArgumentException("Invalid permanent recovery rate inputs");

            var rate = otherRate;
            if (ranks.TryGetValue("regen", out var rank) && rank > 0) rate += .1 * rank;
            if (noHitSeconds >= 8 && Has(ranks, "common.regen.02")) rate += .1;
            if (healthRatio <= .30 && Has(ranks, "common.regen.03")) rate += .1;
            if (stationarySeconds >= 1 && Has(ranks, "common.regen.04")) rate += .1;
            if (Has(ranks, "common.regen.07")) rate += .1;
            if (Has(ranks, "common.regen.10")) rate += .2;
            return Math.Min(1, rate);
        }

        // Event sources are mutually exclusive. The ordinary count and cooldowns are
        // consumed by the triggering event, even if the restoration is discarded.
        public static double OnKill(PermanentRecoveryState state, IReadOnlyDictionary<string, int> ranks,
            double now, PermanentRecoveryKillKind kind, bool original, double health, double maxHealth)
        {
            if ((uint)kind > (uint)PermanentRecoveryKillKind.BossSupport)
                throw new ArgumentOutOfRangeException(nameof(kind));
            ValidateHealth(health, maxHealth);
            AdvanceBudget(state, ranks, now);
            if (!original || health <= 0) return 0;

            switch (kind)
            {
                case PermanentRecoveryKillKind.Normal:
                    if (state.NormalKills >= NormalKillGoal || !Has(ranks, "common.regen.05")) return 0;
                    if (++state.NormalKills != NormalKillGoal) return 0;
                    break;
                case PermanentRecoveryKillKind.Elite:
                    if (now < state.NextEliteAt || !Has(ranks, "common.regen.06")) return 0;
                    state.NextEliteAt = now + KillCooldown;
                    break;
                case PermanentRecoveryKillKind.BossSupport:
                    if (now < state.NextBossSupportAt || !Has(ranks, "common.regen.09")) return 0;
                    state.NextBossSupportAt = now + KillCooldown;
                    break;
            }

            return Restore(state, now, 1, health, maxHealth);
        }

        public static double OnActualDamage(PermanentRecoveryState state, IReadOnlyDictionary<string, int> ranks,
            double now, double healthBefore, double healthAfter, double maxHealth)
        {
            ValidateHealth(healthBefore, maxHealth);
            ValidateHealth(healthAfter, maxHealth);
            AdvanceBudget(state, ranks, now);
            var threshold = maxHealth * LowHealthThreshold;
            if (healthAfter <= 0 || healthBefore <= threshold || healthAfter > threshold ||
                now < state.NextLowHealthAt || !Has(ranks, "common.regen.08"))
                return 0;

            state.NextLowHealthAt = now + LowHealthCooldown;
            return Restore(state, now, 2, healthAfter, maxHealth);
        }

        internal static double OnWeaponRecovery(PermanentRecoveryState state, IReadOnlyDictionary<string, int> ranks,
            double now, double requested, double health, double maxHealth)
        {
            ValidateHealth(health, maxHealth);
            if (!NonNegative(requested)) throw new ArgumentOutOfRangeException(nameof(requested));
            AdvanceBudget(state, ranks, now);
            return Restore(state, now, requested, health, maxHealth);
        }

        private static void ValidateHealth(double health, double maxHealth)
        {
            if (!NonNegative(health) || !NonNegative(maxHealth) || maxHealth <= 0 || health > maxHealth)
                throw new ArgumentException("Invalid health for permanent recovery");
        }

        private static void AdvanceBudget(PermanentRecoveryState state, IReadOnlyDictionary<string, int> ranks, double now)
        {
            if (state == null) throw new ArgumentNullException(nameof(state));
            if (ranks == null) throw new ArgumentNullException(nameof(ranks));
            if (!NonNegative(now) || now < state.LastEventAt)
                throw new ArgumentOutOfRangeException(nameof(now));
            state.ImmediateHeals.RemoveAll(heal => now >= heal.At + BudgetPeriod);
            state.LastEventAt = now;
        }

        private static double Restore(PermanentRecoveryState state, double now, double requested, double health, double maxHealth)
        {
            var available = Math.Max(0, ImmediateBudget - state.ImmediateHeals.Sum(heal => heal.Amount));
            var actual = Math.Min(requested, Math.Min(maxHealth - health, available));
            if (actual <= 0) return 0;
            state.ImmediateHeals.Add(new PermanentRecoveryReceipt { At = now, Amount = actual });
            return actual;
        }

        private static bool Has(IReadOnlyDictionary<string, int> ranks, string id) =>
            ranks.TryGetValue(id, out var rank) && rank > 0;

        internal static bool NonNegative(double value) =>
            !double.IsNaN(value) && !double.IsInfinity(value) && value >= 0;
    }
}
