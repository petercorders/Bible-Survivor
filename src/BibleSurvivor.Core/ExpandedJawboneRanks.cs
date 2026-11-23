using System;
using System.Collections.Generic;
using System.Numerics;
using System.Linq;
using System.Runtime.Serialization;

namespace BibleSurvivor.Core
{
    public readonly struct ExpandedJawboneRanks
    {
        private readonly bool widerArc;
        private readonly bool reverseSecond;
        private readonly bool rearFourthCycle;
        private readonly bool smashLast;
        private readonly bool fourWaySixthCycle;
        private readonly bool strongWrist;
        private readonly bool highHealthDamage;
        private readonly bool groundShock;
        private readonly bool crowdKnockback;

        public const double GroundShockCooldown = 2;
        public bool TracksEliteMark { get; }
        public ExpandedJawboneLifecycleEffects Lifecycle { get; }

        public bool Has(ExpandedJawboneLifecycleEffects effect) => (Lifecycle & effect) != 0;

        private ExpandedJawboneRanks(bool widerArc, bool reverseSecond, bool rearFourthCycle, bool smashLast,
            bool fourWaySixthCycle, bool strongWrist, bool highHealthDamage, bool eliteMark, bool groundShock,
            bool crowdKnockback, ExpandedJawboneLifecycleEffects lifecycle)
        {
            this.widerArc = widerArc;
            this.reverseSecond = reverseSecond;
            this.rearFourthCycle = rearFourthCycle;
            this.smashLast = smashLast;
            this.fourWaySixthCycle = fourWaySixthCycle;
            this.strongWrist = strongWrist;
            this.highHealthDamage = highHealthDamage;
            this.groundShock = groundShock;
            this.crowdKnockback = crowdKnockback;
            TracksEliteMark = eliteMark;
            Lifecycle = lifecycle;
        }

        public static ExpandedJawboneRanks FromRanks(IReadOnlyDictionary<string, int> ranks)
        {
            if (ranks == null) throw new ArgumentNullException(nameof(ranks));
            bool Has(string id) => ranks.TryGetValue("weapon.jawbone." + id, out var rank) && rank > 0;
            ExpandedJawboneLifecycleEffects Effect(string id, ExpandedJawboneLifecycleEffects effect) =>
                Has(id) ? effect : 0;
            var lifecycle = Effect("c.01", ExpandedJawboneLifecycleEffects.FirstKillPower) |
                Effect("c.02", ExpandedJawboneLifecycleEffects.RapidKillWait) |
                Effect("c.03", ExpandedJawboneLifecycleEffects.EliteAdvance) |
                Effect("c.04", ExpandedJawboneLifecycleEffects.CrowdReach) |
                Effect("c.05", ExpandedJawboneLifecycleEffects.Breakthrough) |
                Effect("d.01", ExpandedJawboneLifecycleEffects.CloseKnockback) |
                Effect("d.02", ExpandedJawboneLifecycleEffects.HurtPower) |
                Effect("d.03", ExpandedJawboneLifecycleEffects.EliteRecovery) |
                Effect("d.04", ExpandedJawboneLifecycleEffects.ContactGuard) |
                Effect("d.05", ExpandedJawboneLifecycleEffects.SurroundStrike) |
                Effect("e.01", ExpandedJawboneLifecycleEffects.EvolvedReach) |
                Effect("e.02", ExpandedJawboneLifecycleEffects.PushedCollision) |
                Effect("e.03", ExpandedJawboneLifecycleEffects.EliteFinalPower) |
                Effect("e.04", ExpandedJawboneLifecycleEffects.SmashWave) |
                Effect("e.05", ExpandedJawboneLifecycleEffects.EvolvedExtraSwing);
            return new ExpandedJawboneRanks(Has("a.01"), Has("a.02"), Has("a.03"), Has("a.04"), Has("a.05"),
                Has("b.01"), Has("b.02"), Has("b.03"), Has("b.04"), Has("b.05"), lifecycle);
        }

        public ExpandedJawboneSwing ForSwing(long cycle, int slot, int amount, Vector2 facing,
            double extraHalfAngle = 0, bool fullCircle = false)
        {
            if (cycle < 1) throw new ArgumentOutOfRangeException(nameof(cycle));
            if (amount < 1 || amount > CombatStats.ExpandedAmountLimit("jawbone"))
                throw new ArgumentOutOfRangeException(nameof(amount));
            if (slot < 1 || slot > amount) throw new ArgumentOutOfRangeException(nameof(slot));
            if (!BattleSnapshot.Position(facing.X, facing.Y)) throw new ArgumentException("Invalid facing", nameof(facing));
            if (!BattleSnapshot.Finite(extraHalfAngle) || extraHalfAngle < 0)
                throw new ArgumentOutOfRangeException(nameof(extraHalfAngle));

            const double baseHalfAngle = 60;
            var direction = facing.LengthSquared() > 1e-12f ? Vector2.Normalize(facing) : Vector2.UnitX;
            var bonusAngle = (widerArc ? 10 : 0) + extraHalfAngle;
            var groundSmash = smashLast && slot == amount;
            var halfAngle = Math.Min(90, baseHalfAngle * (groundSmash ? .60 : 1) + bonusAngle);
            var rearHalfAngle = Math.Min(90, baseHalfAngle + bonusAngle);
            return new ExpandedJawboneSwing(direction, reverseSecond && slot == 2, fullCircle ? 180 : halfAngle, rearHalfAngle,
                groundSmash, rearFourthCycle && cycle % 4 == 0 && slot == 1,
                fourWaySixthCycle && cycle % 6 == 0 && slot == 1);
        }

        public double ApplyOriginalDamage(ExpandedJawboneSwing swing, double damage, double originalBaseDamage,
            double targetHealthRatio, bool marked, double additionalDamageBonus = 0)
        {
            var bonus = swing.DamageBonus + additionalDamageBonus + (highHealthDamage && targetHealthRatio >= .75 ? .06 : 0) +
                (TracksEliteMark && marked ? .08 : 0);
            return bonus > 0 ? Math.Min(2.25 * originalBaseDamage, damage + originalBaseDamage * bonus) : damage;
        }

        public double Knockback(double baseKnockback, int originalTargetCount, double targetDistance = double.PositiveInfinity)
        {
            return Math.Min(1, baseKnockback + (strongWrist ? .25 : 0) +
                (crowdKnockback && originalTargetCount >= 4 ? .20 : 0) +
                (Has(ExpandedJawboneLifecycleEffects.CloseKnockback) && targetDistance <= 1 + 1e-6 ? .25 : 0));
        }

        public double OriginalReach(double nativeReach, double computedReach, bool evolved, double crowdBonus = 0) =>
            Math.Min(2.5 * nativeReach, computedReach + nativeReach *
                (crowdBonus + (evolved && Has(ExpandedJawboneLifecycleEffects.EvolvedReach) ? .15 : 0)));

        public double GroundShockLength(bool evolved) =>
            evolved && Has(ExpandedJawboneLifecycleEffects.EvolvedReach) ? 2 * 1.15 : 2;

        public double EliteFinalDamageBonus(bool evolved, bool isFinal, bool elite) =>
            evolved && isFinal && elite && Has(ExpandedJawboneLifecycleEffects.EliteFinalPower) ? .10 : 0;

        public bool CanEmitGroundShock(ExpandedJawboneSwing swing, double now, double? nextAt) =>
            groundShock && swing.IsGroundSmash && (!nextAt.HasValue || nextAt.Value <= now + 1e-9);
    }

    public readonly struct ExpandedJawboneSwing
    {
        private const double FourWayHalfAngle = 25;
        private const double SupportDamageFactor = .30;
        private readonly Vector2 facing;

        public Vector2 Direction { get; }
        public Vector2 RearDirection => -facing;
        public double HalfAngle { get; }
        public double RearHalfAngle { get; }
        public double DamageBonus => IsGroundSmash ? .12 : 0;
        public bool IsGroundSmash { get; }
        public bool RearStrike { get; }
        public bool FourWayStrike { get; }

        internal ExpandedJawboneSwing(Vector2 facing, bool reverse, double halfAngle, double rearHalfAngle,
            bool groundSmash, bool rearStrike, bool fourWayStrike)
        {
            this.facing = facing;
            Direction = reverse ? -facing : facing;
            HalfAngle = halfAngle;
            RearHalfAngle = rearHalfAngle;
            IsGroundSmash = groundSmash;
            RearStrike = rearStrike;
            FourWayStrike = fourWayStrike;
        }

        public double OriginalDamage(double damage, double originalBaseDamage) =>
            IsGroundSmash ? Math.Min(2.25 * originalBaseDamage, damage + originalBaseDamage * DamageBonus) : damage;

        public static double SupportDamage(double preCriticalOriginalDamage) => preCriticalOriginalDamage * SupportDamageFactor;

        public bool HitsOriginal(Vector2 relative, double reach) => InArc(relative, Direction, reach, HalfAngle);

        public bool HitsRear(Vector2 relative, double reach) => RearStrike &&
            InArc(relative, RearDirection, reach, RearHalfAngle);

        public bool HitsFourWay(Vector2 relative, double reach)
        {
            if (!FourWayStrike) return false;
            var perpendicular = new Vector2(-facing.Y, facing.X);
            return InArc(relative, facing, reach, FourWayHalfAngle) ||
                InArc(relative, perpendicular, reach, FourWayHalfAngle) ||
                InArc(relative, -facing, reach, FourWayHalfAngle) ||
                InArc(relative, -perpendicular, reach, FourWayHalfAngle);
        }

        public bool HitsGroundShock(Vector2 relative, double length = 2, double width = .6)
        {
            if (!IsGroundSmash) return false;
            var along = (double)relative.X * Direction.X + (double)relative.Y * Direction.Y;
            var side = (double)relative.X * Direction.Y - (double)relative.Y * Direction.X;
            return along >= -1e-6 && along <= length + 1e-6 && Math.Abs(side) <= width / 2 + 1e-6;
        }

        public bool HitsSmashWave(Vector2 relative, double reach = 2.4) =>
            IsGroundSmash && InArc(relative, Direction, reach, 45);

        private static bool InArc(Vector2 relative, Vector2 direction, double reach, double halfAngle)
        {
            // Input positions are floats. Keep repository arc limits while allowing
            // their rounding at an inclusive geometric boundary.
            return HitGeometry.InArc(relative.X, relative.Y, direction.X, direction.Y,
                reach + 1e-6, Math.Min(360, 2 * halfAngle + 1e-5));
        }
    }

    [DataContract]
    public sealed class ExpandedJawboneEliteMarks
    {
        public const double Duration = 3;
        private const double TimeEpsilon = 1e-9;

        [DataMember(EmitDefaultValue = false)] public int LastEliteId { get; set; }
        [DataMember(EmitDefaultValue = false)] public int ConsecutiveHits { get; set; }
        [DataMember(EmitDefaultValue = false)] public List<BattleHitState>? Marks { get; set; }

        public bool IsMarked(int targetId, double now) => Marks?.Any(mark => mark.Id == targetId &&
            now - mark.At <= Duration + TimeEpsilon) == true;

        // Call only after an accepted original hit, with the elite classification
        // captured before damage and the survival result from that damage.
        public void RecordOriginalHit(int targetId, bool elite, bool survived, double now)
        {
            Prune(now);
            if (!survived) RemoveTarget(targetId);
            if (!elite || !survived)
            {
                LastEliteId = 0;
                ConsecutiveHits = 0;
                return;
            }
            if (LastEliteId != targetId)
            {
                LastEliteId = targetId;
                ConsecutiveHits = 0;
            }
            if (++ConsecutiveHits < 3) return;
            ConsecutiveHits = 0;
            Marks ??= new List<BattleHitState>();
            var existing = Marks.Find(mark => mark.Id == targetId);
            if (existing == null)
                Marks.Add(new BattleHitState { Id = targetId, At = now });
            else
                existing.At = now;
        }

        public void RemoveTarget(int targetId)
        {
            Marks?.RemoveAll(mark => mark.Id == targetId);
            if (Marks?.Count == 0) Marks = null;
            if (LastEliteId != targetId) return;
            LastEliteId = 0;
            ConsecutiveHits = 0;
        }

        public void Prune(double now, Func<int, bool>? isLive = null)
        {
            Marks?.RemoveAll(mark => now - mark.At > Duration + TimeEpsilon ||
                isLive?.Invoke(mark.Id) == false);
            if (Marks?.Count == 0) Marks = null;
            if (LastEliteId != 0 && isLive?.Invoke(LastEliteId) == false)
            {
                LastEliteId = 0;
                ConsecutiveHits = 0;
            }
        }

        public ExpandedJawboneEliteMarks Copy() => new ExpandedJawboneEliteMarks
        {
            LastEliteId = LastEliteId,
            ConsecutiveHits = ConsecutiveHits,
            Marks = Marks?.Select(mark => mark.Copy()).ToList(),
        };

        public void Validate(double now)
        {
            if (!BattleSnapshot.Finite(now) || now < 0 || LastEliteId < 0 ||
                ConsecutiveHits < 0 || ConsecutiveHits > 2 || LastEliteId == 0 && ConsecutiveHits != 0 ||
                Marks != null && (Marks.Count == 0 || Marks.Count > 120 ||
                    Marks.Any(mark => mark == null || mark.Id <= 0 || !BattleSnapshot.Finite(mark.At) ||
                        mark.At < 0 || mark.At > now + TimeEpsilon) ||
                    Marks.Select(mark => mark.Id).Distinct().Count() != Marks.Count))
                throw new ArgumentException("Invalid jawbone elite mark history");
        }
    }
}
