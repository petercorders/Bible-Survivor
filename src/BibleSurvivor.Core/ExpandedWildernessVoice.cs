using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.Serialization;

namespace BibleSurvivor.Core
{
    public enum ExpandedWildernessVoiceHitKind
    {
        Primary,
        FirstEcho,
        SecondEcho,
    }

    public readonly struct ExpandedWildernessVoiceHit
    {
        public ExpandedWildernessVoiceHitKind Kind { get; }
        public int TargetId { get; }
        public Vector2 Position { get; }
        public Vector2 Center { get; }
        public Vector2 Facing { get; }
        public double Damage { get; }
        public double Knockback { get; }
        public long Cycle { get; }
        public int PulseNumber { get; }

        internal ExpandedWildernessVoiceHit(ExpandedWildernessVoiceHitKind kind, int targetId,
            Vector2 position, Vector2 center, Vector2 facing, double damage, double knockback,
            long cycle, int pulseNumber)
        {
            Kind = kind;
            TargetId = targetId;
            Position = position;
            Center = center;
            Facing = facing;
            Damage = damage;
            Knockback = knockback;
            Cycle = cycle;
            PulseNumber = pulseNumber;
        }
    }

    [DataContract]
    public sealed class ExpandedWildernessVoiceCastState
    {
        [DataMember(IsRequired = true)] public long Id { get; set; }
        [DataMember(IsRequired = true)] public double StartedAt { get; set; }
        [DataMember(IsRequired = true)] public double Duration { get; set; }
        [DataMember(IsRequired = true)] public double Damage { get; set; }
        [DataMember(IsRequired = true)] public double Reach { get; set; }
        [DataMember(IsRequired = true)] public double HalfAngle { get; set; }
        [DataMember(EmitDefaultValue = false)] public double? FirstDelay { get; set; }
        [DataMember(EmitDefaultValue = false)] public double KnockbackBonus { get; set; }
        [DataMember(EmitDefaultValue = false)] public double CloseKnockbackBonus { get; set; }
        [DataMember(IsRequired = true)] public int PrimaryPulses { get; set; }
        [DataMember(IsRequired = true)] public int EchoesFired { get; set; }

        internal double End => StartedAt + Duration;
        internal double InitialDelay => FirstDelay ?? ExpandedWildernessVoice.Profile.FirstAttackDelay;
        internal double PrimaryAt(int pulse) =>
            StartedAt + InitialDelay + pulse * ExpandedWildernessVoice.Profile.RepeatHitInterval;
        internal double EchoAt(int echo) => StartedAt + (echo + 1) * ExpandedWildernessVoice.EchoInterval;
        internal double ExpiresAt(bool evolved) => evolved ? Math.Max(End, EchoAt(1)) : End;

        internal ExpandedWildernessVoiceCastState Copy() => (ExpandedWildernessVoiceCastState)MemberwiseClone();
    }

    [DataContract]
    public sealed class ExpandedWildernessVoiceState
    {
        [DataMember(IsRequired = true)] public bool Evolved { get; set; }
        [DataMember(IsRequired = true)] public long Cycle { get; set; }
        [DataMember(IsRequired = true)] public double NextCycle { get; set; }
        [DataMember(IsRequired = true)] public List<ExpandedWildernessVoiceCastState> Active { get; set; } =
            new List<ExpandedWildernessVoiceCastState>();

        public ExpandedWildernessVoiceState Copy() => new ExpandedWildernessVoiceState
        {
            Evolved = Evolved,
            Cycle = Cycle,
            NextCycle = NextCycle,
            Active = Active.Select(cast => cast.Copy()).ToList(),
        };

        // The caller owns the next executable tick and the round boundary.
        public void ResetForForm(bool evolved, double now)
        {
            if (!BattleSnapshot.Finite(now) || now < 0) throw new ArgumentOutOfRangeException(nameof(now));
            Evolved = evolved;
            NextCycle = now;
            Active.Clear();
        }

        public void Validate(double now, bool evolved)
        {
            const double epsilon = ExpandedWildernessVoice.TimeEpsilon;
            if (!BattleSnapshot.Finite(now) || now < 0 || evolved != Evolved || Cycle < 0 ||
                !BattleSnapshot.Finite(NextCycle) || NextCycle < 0 ||
                NextCycle > now + ExpandedWildernessVoice.MaximumCycle + epsilon ||
                Active == null || Active.Count > ExpandedWildernessVoice.MaximumActiveCasts)
                throw new ArgumentException("Invalid wilderness voice clock");

            var previousId = 0L;
            var previousStart = double.NegativeInfinity;
            foreach (var cast in Active)
            {
                if (cast == null || cast.Id <= previousId || cast.Id > Cycle ||
                    !BattleSnapshot.Finite(cast.StartedAt) || cast.StartedAt < 0 || cast.StartedAt > now + epsilon ||
                    cast.StartedAt < previousStart + ExpandedWildernessVoice.MinimumCycle - epsilon ||
                    cast.StartedAt + ExpandedWildernessVoice.MinimumCycle > NextCycle + epsilon ||
                    !ExpandedWildernessVoice.ValidCast(cast.Damage, cast.Reach, cast.Duration, cast.HalfAngle, evolved, cast.InitialDelay) ||
                    !ExpandedWildernessVoice.ValidKnockbackBonus(cast.KnockbackBonus) ||
                    !ExpandedWildernessVoice.ValidKnockbackBonus(cast.CloseKnockbackBonus) ||
                    cast.ExpiresAt(evolved) < now - epsilon ||
                    cast.PrimaryPulses < 0 || cast.PrimaryPulses > ExpandedWildernessVoice.MaximumPrimaryPulses ||
                    cast.EchoesFired < 0 || cast.EchoesFired > (evolved ? 2 : 0))
                    throw new ArgumentException("Invalid wilderness voice cast");

                var nextPrimary = cast.PrimaryAt(cast.PrimaryPulses);
                var previousPrimary = cast.PrimaryAt(cast.PrimaryPulses - 1);
                if (cast.PrimaryPulses > 0 && (previousPrimary > now + epsilon || previousPrimary >= cast.End - epsilon) ||
                    nextPrimary < cast.End - epsilon && nextPrimary < now - epsilon ||
                    cast.EchoesFired > 0 && cast.EchoAt(cast.EchoesFired - 1) > now + epsilon ||
                    evolved && cast.EchoesFired < 2 && cast.EchoAt(cast.EchoesFired) < now - epsilon)
                    throw new ArgumentException("Invalid wilderness voice hit schedule");

                previousId = cast.Id;
                previousStart = cast.StartedAt;
            }
        }
    }

    public static class ExpandedWildernessVoice
    {
        internal static readonly ExpandedWeaponProfile Profile = ExpandedWeaponRules.Get("wilderness_voice");
        internal static readonly double MaximumCycle = Profile.AtLevel(1).Cycle;
        internal static readonly double MaximumDamage = Profile.AtLevel(8).Damage * 2.25;
        internal static readonly double MaximumReach = Profile.AtLevel(1).Reach * 2.5;
        internal static readonly double MaximumDuration = Profile.AtLevel(8).Duration * 1.75;
        internal static readonly int MaximumActiveCasts = (int)Math.Ceiling(MaximumDuration / MinimumCycle);
        internal static readonly int MaximumPrimaryPulses =
            (int)Math.Ceiling((MaximumDuration - MinimumDelay) / Profile.RepeatHitInterval);
        internal const double TimeEpsilon = 1e-9;
        internal const double MinimumCycle = .15;
        private const double MinimumDelay = .05;
        internal const double EchoInterval = .5;

        // Cast stats are frozen at start. Each atomic pulse samples the current
        // center and facing, then processes every eligible target once in ID order.
        public static void Step(ExpandedWildernessVoiceState state, double now, Vector2 playerPosition, Vector2 facing,
            int level, bool evolved, double damage, double cycle, double reach, double duration, double halfAngle,
            Func<IReadOnlyDictionary<int, Vector2>> targets, Action<ExpandedWildernessVoiceHit> applyHit,
            double delayAdvance = 0, double knockbackBonus = 0, double closeKnockbackBonus = 0)
        {
            if (state == null) throw new ArgumentNullException(nameof(state));
            if (targets == null) throw new ArgumentNullException(nameof(targets));
            if (applyHit == null) throw new ArgumentNullException(nameof(applyHit));
            if (!BattleSnapshot.Finite(delayAdvance) || delayAdvance < 0) throw new ArgumentOutOfRangeException(nameof(delayAdvance));
            if (!ValidKnockbackBonus(knockbackBonus) || !ValidKnockbackBonus(closeKnockbackBonus))
                throw new ArgumentException("Invalid voice knockback bonus");
            var delay = Math.Max(MinimumDelay, Profile.FirstAttackDelay - delayAdvance);
            if (!BattleSnapshot.Finite(now) || now < 0 ||
                !BattleSnapshot.Position(playerPosition.X, playerPosition.Y) || !BattleSnapshot.Position(facing.X, facing.Y) ||
                level < 1 || level > 8 || evolved && level != 8 ||
                !BattleSnapshot.Finite(cycle) || cycle < MinimumCycle || cycle > MaximumCycle ||
                !ValidCast(damage, reach, duration, halfAngle, evolved, delay))
                throw new ArgumentException("Invalid wilderness voice parameters");

            if (state.Evolved != evolved) state.ResetForForm(evolved, now);
            var direction = facing.LengthSquared() > 1e-12f ? Vector2.Normalize(facing) : Vector2.UnitX;
            ResolveDue(state, now, playerPosition, direction, targets, applyHit);

            if (state.NextCycle > now + TimeEpsilon) return;
            var start = state.NextCycle;
            state.Cycle = checked(state.Cycle + 1);
            state.NextCycle = start + cycle;
            state.Active.Add(new ExpandedWildernessVoiceCastState
            {
                Id = state.Cycle,
                StartedAt = start,
                Damage = damage,
                Reach = reach,
                Duration = duration,
                HalfAngle = halfAngle,
                FirstDelay = delay == Profile.FirstAttackDelay ? (double?)null : delay,
                KnockbackBonus = knockbackBonus,
                CloseKnockbackBonus = closeKnockbackBonus,
            });
        }

        private static void ResolveDue(ExpandedWildernessVoiceState state, double now, Vector2 center, Vector2 direction,
            Func<IReadOnlyDictionary<int, Vector2>> targets, Action<ExpandedWildernessVoiceHit> applyHit)
        {
            if (state.Active.Count == 0) return;
            while (true)
            {
                ExpandedWildernessVoiceCastState? chosen = null;
                var chosenAt = double.PositiveInfinity;
                var kind = ExpandedWildernessVoiceHitKind.Primary;
                foreach (var cast in state.Active)
                {
                    var next = NextPulse(cast, state.Evolved);
                    if (next.At > now + TimeEpsilon ||
                        chosen != null && (next.At > chosenAt || next.At == chosenAt && cast.Id > chosen.Id))
                        continue;
                    chosen = cast;
                    chosenAt = next.At;
                    kind = next.Kind;
                }
                if (chosen == null) break;
                HitPulse(chosen, kind, state.Evolved, center, direction, targets, applyHit);
            }

            state.Active.RemoveAll(cast => cast.ExpiresAt(state.Evolved) <= now + TimeEpsilon);
        }

        private static (double At, ExpandedWildernessVoiceHitKind Kind) NextPulse(
            ExpandedWildernessVoiceCastState cast, bool evolved)
        {
            var primary = cast.PrimaryAt(cast.PrimaryPulses);
            if (primary >= cast.End - TimeEpsilon) primary = double.PositiveInfinity;
            if (!evolved || cast.EchoesFired >= 2 || primary <= cast.EchoAt(cast.EchoesFired))
                return (primary, ExpandedWildernessVoiceHitKind.Primary);
            return (cast.EchoAt(cast.EchoesFired),
                cast.EchoesFired == 0 ? ExpandedWildernessVoiceHitKind.FirstEcho : ExpandedWildernessVoiceHitKind.SecondEcho);
        }

        private static void HitPulse(ExpandedWildernessVoiceCastState cast, ExpandedWildernessVoiceHitKind kind,
            bool evolved, Vector2 center, Vector2 direction, Func<IReadOnlyDictionary<int, Vector2>> targets,
            Action<ExpandedWildernessVoiceHit> applyHit)
        {
            var primary = kind == ExpandedWildernessVoiceHitKind.Primary;
            var pulseNumber = primary ? ++cast.PrimaryPulses : ++cast.EchoesFired;
            var reach = primary ? cast.Reach : kind == ExpandedWildernessVoiceHitKind.FirstEcho ? 5.0 : 5.5;
            var damage = cast.Damage * (primary ? 1 : kind == ExpandedWildernessVoiceHitKind.FirstEcho ? .45 : .30);
            var degrees = primary && !evolved ? 2 * cast.HalfAngle : 360;
            var ids = targets().Keys.OrderBy(id => id).ToArray();
            foreach (var id in ids)
            {
                if (!targets().TryGetValue(id, out var position)) continue;
                var offset = position - center;
                if (HitGeometry.InArc(offset.X, offset.Y, direction.X, direction.Y, reach, degrees))
                {
                    var closeBonus = offset.LengthSquared() <= 1.2f * 1.2f ? cast.CloseKnockbackBonus : 0;
                    var knockback = primary ? Math.Min(1, Profile.Knockback + cast.KnockbackBonus + closeBonus) : 0;
                    applyHit(new ExpandedWildernessVoiceHit(kind, id, position, center, direction,
                        damage, knockback, cast.Id, pulseNumber));
                }
            }
        }

        internal static bool ValidCast(double damage, double reach, double duration, double halfAngle, bool evolved, double delay) =>
            BattleSnapshot.Finite(delay) && delay >= MinimumDelay && delay <= Profile.FirstAttackDelay &&
            BattleSnapshot.Finite(damage) && damage >= 1 && damage <= MaximumDamage &&
            BattleSnapshot.Finite(reach) && reach > 0 && reach <= MaximumReach &&
            BattleSnapshot.Finite(duration) && duration > delay && duration <= MaximumDuration &&
            BattleSnapshot.Finite(halfAngle) && halfAngle > 0 && halfAngle <= (evolved ? 180 : 90);

        internal static bool ValidKnockbackBonus(double bonus) => BattleSnapshot.Finite(bonus) && bonus >= 0 && bonus <= 1;
    }
}
