using System;
using System.Collections.Generic;

namespace BibleSurvivor.Core
{
    public readonly struct PermanentTempoContext
    {
        public bool AfterRoundFirstAttack { get; }
        public double RoundCombatSeconds { get; }
        public double? SecondsSinceRoundHurt { get; }
        public double ChallengeNoHitSeconds { get; }
        public double? SecondsSinceRoundEliteKill { get; }
        public bool BossActive { get; }

        public PermanentTempoContext(bool afterRoundFirstAttack, double roundCombatSeconds, double? secondsSinceRoundHurt,
            double challengeNoHitSeconds, double? secondsSinceRoundEliteKill, bool bossActive)
        {
            AfterRoundFirstAttack = afterRoundFirstAttack;
            RoundCombatSeconds = roundCombatSeconds;
            SecondsSinceRoundHurt = secondsSinceRoundHurt;
            ChallengeNoHitSeconds = challengeNoHitSeconds;
            SecondsSinceRoundEliteKill = secondsSinceRoundEliteKill;
            BossActive = bossActive;
        }

        internal void Validate()
        {
            if (!PermanentTempoRules.NonNegative(RoundCombatSeconds) || !PermanentTempoRules.NonNegative(ChallengeNoHitSeconds) ||
                !ValidRoundEvent(SecondsSinceRoundHurt) || !ValidRoundEvent(SecondsSinceRoundEliteKill))
                throw new ArgumentException("Invalid permanent tempo context");
        }

        private bool ValidRoundEvent(double? elapsed) =>
            !elapsed.HasValue || (PermanentTempoRules.NonNegative(elapsed.Value) && elapsed.Value <= RoundCombatSeconds);
    }

    public readonly struct PermanentTempoProfile
    {
        public double CycleReduction { get; }
        public double BurstReduction { get; }
        public double FixedHitReduction { get; }

        internal PermanentTempoProfile(double cycleReduction, double burstReduction, double fixedHitReduction)
        {
            CycleReduction = cycleReduction;
            BurstReduction = burstReduction;
            FixedHitReduction = fixedHitReduction;
        }

        public double Wait(double baseWaitingSeconds)
        {
            RequireSeconds(baseWaitingSeconds, nameof(baseWaitingSeconds));
            return baseWaitingSeconds * Math.Max(.20, 1 - CycleReduction);
        }

        // A cycle's active portion is preserved. lastBurstOffset is the scheduled
        // final emission relative to this start, which must finish before the next.
        public double Cycle(double activeSeconds, double baseWaitingSeconds, double lastBurstOffset = 0,
            double advanceSeconds = 0)
        {
            RequireSeconds(activeSeconds, nameof(activeSeconds));
            RequireSeconds(lastBurstOffset, nameof(lastBurstOffset));
            RequireSeconds(advanceSeconds, nameof(advanceSeconds));
            var duration = activeSeconds + Math.Max(0, Wait(baseWaitingSeconds) - advanceSeconds);
            RequireSeconds(duration, nameof(duration));
            return Math.Max(duration, Math.Max(.15, lastBurstOffset + 1d / 60));
        }

        public double BurstInterval(double baseSeconds)
        {
            RequireInterval(baseSeconds);
            return Math.Max(.05, baseSeconds * (1 - BurstReduction));
        }

        public double FixedHitInterval(double baseSeconds)
        {
            RequireInterval(baseSeconds);
            return Math.Max(.40, baseSeconds * (1 - FixedHitReduction));
        }

        private static void RequireInterval(double seconds)
        {
            if (seconds == 0) throw new ArgumentOutOfRangeException(nameof(seconds), "A scheduled interval must be positive");
            RequireSeconds(seconds, nameof(seconds));
        }

        private static void RequireSeconds(double seconds, string parameter)
        {
            if (!PermanentTempoRules.NonNegative(seconds)) throw new ArgumentOutOfRangeException(parameter);
        }
    }

    public static class PermanentTempoRules
    {
        public static PermanentTempoProfile Evaluate(IReadOnlyDictionary<string, int> ranks, PermanentTempoContext context,
            int thunderRank = 0, double otherCycleReduction = 0, double otherBurstReduction = 0,
            double otherFixedHitReduction = 0)
        {
            if (ranks == null) throw new ArgumentNullException(nameof(ranks));
            context.Validate();
            if (thunderRank < 0 || !NonNegative(otherCycleReduction) || !NonNegative(otherBurstReduction) ||
                !NonNegative(otherFixedHitReduction))
                throw new ArgumentException("Invalid permanent tempo modifiers");

            var cycle = otherCycleReduction + .08 * thunderRank;
            if (ranks.TryGetValue("cooldown", out var rank) && rank > 0) cycle += .02 * rank;
            if (context.AfterRoundFirstAttack && Has(ranks, "common.tempo.02")) cycle += .15;
            if (context.SecondsSinceRoundHurt is double hurt && hurt <= 3 && Has(ranks, "common.tempo.03")) cycle += .03;
            if (context.RoundCombatSeconds <= 10 && Has(ranks, "common.tempo.04")) cycle += .04;
            if (context.RoundCombatSeconds >= 60 && Has(ranks, "common.tempo.06")) cycle += .04;
            if (context.ChallengeNoHitSeconds >= 10 && Has(ranks, "common.tempo.07")) cycle += .04;
            if (context.SecondsSinceRoundEliteKill is double kill && kill <= 3 && Has(ranks, "common.tempo.08")) cycle += .05;
            if (context.BossActive && Has(ranks, "common.tempo.10")) cycle += .05;

            var burst = otherBurstReduction + (Has(ranks, "common.tempo.09") ? .08 : 0);
            var fixedHit = otherFixedHitReduction + (Has(ranks, "common.tempo.05") ? .04 : 0);
            return new PermanentTempoProfile(Math.Min(.8, cycle), Math.Min(.25, burst), Math.Min(.20, fixedHit));
        }

        private static bool Has(IReadOnlyDictionary<string, int> ranks, string id) =>
            ranks.TryGetValue(id, out var rank) && rank > 0;

        internal static bool NonNegative(double value) =>
            !double.IsNaN(value) && !double.IsInfinity(value) && value >= 0;
    }
}
