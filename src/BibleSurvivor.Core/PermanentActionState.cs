using System;
using System.Collections.Generic;
using System.Numerics;
using System.Runtime.Serialization;

namespace BibleSurvivor.Core
{
    [DataContract]
    public sealed class PermanentActionState
    {
        [DataMember(IsRequired = true)] public int GemsCollected { get; set; }
        [DataMember(IsRequired = true)] public double LastGemAt { get; set; } = -1;
        [DataMember(IsRequired = true)] public double LastContactAt { get; set; } = -1;
        [DataMember(IsRequired = true)] public double StationarySince { get; set; }
        [DataMember(IsRequired = true)] public double HeadingSince { get; set; } = -1;
        [DataMember(IsRequired = true)] public float HeadingX { get; set; }
        [DataMember(IsRequired = true)] public float HeadingY { get; set; }

        internal bool Moving => StationarySince < 0;

        internal void ObserveMotion(double now, Vector2 input)
        {
            if (input == Vector2.Zero)
            {
                if (Moving) StationarySince = now;
                HeadingSince = -1;
                HeadingX = 0;
                HeadingY = 0;
                return;
            }

            var length = Math.Sqrt((double)input.X * input.X + (double)input.Y * input.Y);
            var direction = new Vector2((float)(input.X / length), (float)(input.Y / length));
            StationarySince = -1;
            const double cosFifteenDegrees = .9659258262890683;
            if (HeadingSince >= 0 &&
                Vector2.Dot(new Vector2(HeadingX, HeadingY), direction) >= cosFifteenDegrees - 1e-7) return;
            HeadingSince = now;
            HeadingX = direction.X;
            HeadingY = direction.Y;
        }

        internal void Collect(double now)
        {
            GemsCollected = checked(GemsCollected + 1);
            LastGemAt = now;
        }

        internal void Validate(double now)
        {
            if (GemsCollected < 0 || GemsCollected > 6210 ||
                (GemsCollected == 0) != (LastGemAt < 0) ||
                !ValidTime(LastGemAt, now) || !ValidTime(LastContactAt, now) ||
                !ValidTime(StationarySince, now) || !ValidTime(HeadingSince, now) ||
                !BattleSnapshot.Position(HeadingX, HeadingY) ||
                (HeadingSince >= 0) != Moving ||
                (Moving ? Math.Abs((double)HeadingX * HeadingX + (double)HeadingY * HeadingY - 1) > 1e-5 :
                    HeadingX != 0 || HeadingY != 0))
                throw new ArgumentException("Invalid permanent action state");
        }

        private static bool ValidTime(double time, double now) =>
            time == -1 || BattleSnapshot.Finite(time) && time >= 0 && time <= now + 1e-8;
    }

    internal static class PermanentActionRules
    {
        internal static double Move(IReadOnlyDictionary<string, int> ranks, PermanentActionState actions,
            double now, double lastHitAt, double healthRatio, bool surrounded, bool bossActive, int underwear)
        {
            var bonus = .05 * PermanentRules.Rank(ranks, "move") + .10 * underwear +
                .02 * PermanentRules.Rank(ranks, "common.move.02") + .02 * PermanentRules.Rank(ranks, "common.move.07");
            if (now - Math.Max(0, lastHitAt) >= 8) bonus += .03 * PermanentRules.Rank(ranks, "common.move.03");
            if (Recent(now, lastHitAt, 2)) bonus += .04 * PermanentRules.Rank(ranks, "common.move.04");
            if (surrounded) bonus += .04 * PermanentRules.Rank(ranks, "common.move.05");
            if (healthRatio <= .30) bonus += .04 * PermanentRules.Rank(ranks, "common.move.06");
            if (actions.Moving && now - actions.HeadingSince >= 1) bonus += .03 * PermanentRules.Rank(ranks, "common.move.08");
            if (Recent(now, actions.LastContactAt, 2)) bonus += .05 * PermanentRules.Rank(ranks, "common.move.09");
            if (bossActive) bonus += .04 * PermanentRules.Rank(ranks, "common.move.10");
            return Math.Min(.80, bonus);
        }

        internal static double Pickup(IReadOnlyDictionary<string, int> ranks, PermanentActionState actions,
            double now, double healthRatio, bool surrounded, bool bossNormalWave)
        {
            var bonus = .10 * PermanentRules.Rank(ranks, "pickup") +
                .05 * PermanentRules.Rank(ranks, "common.pickup.02") + .06 * PermanentRules.Rank(ranks, "common.pickup.08");
            if (actions.Moving) bonus += .05 * PermanentRules.Rank(ranks, "common.pickup.03");
            if (!actions.Moving && now - actions.StationarySince >= 1) bonus += .07 * PermanentRules.Rank(ranks, "common.pickup.04");
            if (healthRatio <= .30) bonus += .08 * PermanentRules.Rank(ranks, "common.pickup.05");
            if (actions.GemsCollected == 0) bonus += .08 * PermanentRules.Rank(ranks, "common.pickup.06");
            if (Recent(now, actions.LastGemAt, 2)) bonus += .05 * PermanentRules.Rank(ranks, "common.pickup.07");
            if (surrounded) bonus += .08 * PermanentRules.Rank(ranks, "common.pickup.09");
            if (bossNormalWave) bonus += .10 * PermanentRules.Rank(ranks, "common.pickup.10");
            return Math.Min(1.20, bonus);
        }

        internal static double Experience(IReadOnlyDictionary<string, int> ranks, PermanentActionState actions,
            double now, int level, double healthRatio, bool surrounded, bool bossNormalOrSettlement, int scroll)
        {
            var bonus = .03 * PermanentRules.Rank(ranks, "growth") + .08 * scroll +
                .03 * PermanentRules.Rank(ranks, "common.xp.04") + .04 * PermanentRules.Rank(ranks, "common.xp.09");
            if (actions.GemsCollected < 5) bonus += .05 * PermanentRules.Rank(ranks, "common.xp.02");
            if (level <= 5) bonus += .05 * PermanentRules.Rank(ranks, "common.xp.03");
            if (healthRatio >= .90) bonus += .03 * PermanentRules.Rank(ranks, "common.xp.05");
            if (level >= 20) bonus += .04 * PermanentRules.Rank(ranks, "common.xp.06");
            if (Recent(now, actions.LastGemAt, 2)) bonus += .04 * PermanentRules.Rank(ranks, "common.xp.07");
            if (surrounded) bonus += .05 * PermanentRules.Rank(ranks, "common.xp.08");
            if (bossNormalOrSettlement) bonus += .06 * PermanentRules.Rank(ranks, "common.xp.10");
            return Math.Min(.75, bonus);
        }

        private static bool Recent(double now, double at, double limit) => at >= 0 && now - at <= limit;
    }
}
