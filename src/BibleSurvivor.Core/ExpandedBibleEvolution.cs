using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.Serialization;

namespace BibleSurvivor.Core
{
    public readonly struct ExpandedBibleEvolutionRanks
    {
        public double PulseRadiusBonus { get; }
        public bool ExtraPulse { get; }
        public bool OuterKnockback { get; }
        public bool SteerInnerBook { get; }
        public bool EliteSanctuary { get; }

        private ExpandedBibleEvolutionRanks(IReadOnlyDictionary<string, int> ranks)
        {
            bool Has(string id) => ranks.TryGetValue("weapon.bible.e." + id, out var rank) && rank > 0;
            PulseRadiusBonus = Has("01") ? .15 : 0;
            ExtraPulse = Has("02");
            OuterKnockback = Has("03");
            SteerInnerBook = Has("04");
            EliteSanctuary = Has("05");
        }

        public static ExpandedBibleEvolutionRanks FromRanks(IReadOnlyDictionary<string, int> ranks) =>
            new ExpandedBibleEvolutionRanks(ranks ?? throw new ArgumentNullException(nameof(ranks)));

        public double PulseKnockback(double distanceSquared, double radius)
        {
            var outerEdge = radius * .8;
            return OuterKnockback && distanceSquared >= (float)(outerEdge * outerEdge) ? .30 : 0;
        }
    }

    [DataContract]
    public sealed class ExpandedBibleEcho
    {
        [DataMember(IsRequired = true)] public double DueAt { get; set; }
        [DataMember(IsRequired = true)] public double Radius { get; set; }
        [DataMember(IsRequired = true)] public double Damage { get; set; }

        internal ExpandedBibleEcho Copy() => (ExpandedBibleEcho)MemberwiseClone();
    }

    [DataContract]
    public sealed class ExpandedBibleSanctuary
    {
        public const double Radius = 1.2;
        public const double Lifetime = 1.5;
        internal const double HitInterval = .5;
        internal const double DamageFactor = .25;

        [DataMember(IsRequired = true)] public float X { get; set; }
        [DataMember(IsRequired = true)] public float Y { get; set; }
        [DataMember(IsRequired = true)] public double StartedAt { get; set; }
        [DataMember(IsRequired = true)] public double NextHitAt { get; set; }
        [DataMember(IsRequired = true)] public double Damage { get; set; }

        public Vector2 Position => new Vector2(X, Y);
        public double EndsAt => StartedAt + Lifetime;
        internal ExpandedBibleSanctuary Copy() => (ExpandedBibleSanctuary)MemberwiseClone();
    }

    [DataContract]
    public sealed class ExpandedBibleEvolutionState
    {
        private const double Epsilon = 1e-9;
        private const double EchoDelay = .5;
        private const double EchoDamageFactor = .35;
        private const double SteeringRange = 1.4;
        private const double MaxSteeringAngle = Math.PI / 6;
        private const double SanctuaryCooldown = 5;

        [DataMember(EmitDefaultValue = false)] public long PulseOrdinal { get; set; }
        [DataMember(EmitDefaultValue = false)] public long LastSteeredPulse { get; set; }
        [DataMember(EmitDefaultValue = false)] public double InnerAngleOffset { get; set; }
        [DataMember(EmitDefaultValue = false)] public ExpandedBibleEcho? PendingEcho { get; set; }
        [DataMember(EmitDefaultValue = false)] public ExpandedBibleSanctuary? Sanctuary { get; set; }
        [DataMember(EmitDefaultValue = false)] public double? NextSanctuaryAt { get; set; }

        public int ReservedRegions => (PendingEcho == null ? 0 : 1) + (Sanctuary == null ? 0 : 1);

        public ExpandedBibleEvolutionState Copy()
        {
            var copy = (ExpandedBibleEvolutionState)MemberwiseClone();
            copy.PendingEcho = PendingEcho?.Copy();
            copy.Sanctuary = Sanctuary?.Copy();
            return copy;
        }

        public bool BeginPrimaryPulse(ExpandedBibleEvolutionRanks ranks, double at, double radius,
            double preCriticalPulseDamage, Func<bool> reserveRegion, Action suppressed)
        {
            PulseOrdinal = checked(PulseOrdinal + 1);
            if (!ranks.ExtraPulse || PulseOrdinal % 4 != 0) return false;
            if (PendingEcho != null || !reserveRegion()) { suppressed(); return false; }
            PendingEcho = new ExpandedBibleEcho { DueAt = at + EchoDelay, Radius = radius, Damage = preCriticalPulseDamage * EchoDamageFactor };
            return true;
        }

        public ExpandedBibleEcho? ConsumeEcho(double now)
        {
            if (PendingEcho == null || now + Epsilon < PendingEcho.DueAt) return null;
            var echo = PendingEcho;
            PendingEcho = null;
            return echo;
        }

        public bool TrySteerInnerBook(ExpandedBibleEvolutionRanks ranks, Vector2 player, double actualInnerAngle,
            IReadOnlyDictionary<int, Vector2> targets, IReadOnlyCollection<int> primaryPulseHitIds)
        {
            if (!ranks.SteerInnerBook || LastSteeredPulse >= PulseOrdinal) return false;
            LastSteeredPulse = PulseOrdinal;
            var relative = targets.Where(target => !primaryPulseHitIds.Contains(target.Key))
                .Select(target => (target.Key, Offset: target.Value - player))
                .Where(target => target.Offset.LengthSquared() <= (float)(SteeringRange * SteeringRange))
                .OrderBy(target => target.Offset.LengthSquared()).ThenBy(target => target.Key)
                .Select(target => target.Offset).FirstOrDefault();
            if (relative.LengthSquared() <= 1e-12f) return false;

            var delta = Normalize(Math.Atan2(relative.Y, relative.X) - actualInnerAngle);
            var turn = Math.Max(-MaxSteeringAngle, Math.Min(MaxSteeringAngle, delta));
            InnerAngleOffset = Normalize(InnerAngleOffset + turn);
            return true;
        }

        public bool OnAcceptedEliteOriginalKill(ExpandedBibleEvolutionRanks ranks, double now, Vector2 position,
            double preCriticalDamage, Func<bool> reserveRegion, Action suppressed)
        {
            if (!ranks.EliteSanctuary || NextSanctuaryAt > now + Epsilon) return false;
            if (!reserveRegion()) { suppressed(); return false; }
            Sanctuary = new ExpandedBibleSanctuary
            {
                X = position.X, Y = position.Y, StartedAt = now, NextHitAt = now,
                Damage = preCriticalDamage * ExpandedBibleSanctuary.DamageFactor,
            };
            NextSanctuaryAt = now + SanctuaryCooldown;
            return true;
        }

        public void StepSanctuary(double now, Func<IReadOnlyDictionary<int, Vector2>> targets, Action<int, Vector2, double> apply)
        {
            if (Sanctuary is not ExpandedBibleSanctuary region) return;
            if (now + Epsilon >= region.EndsAt) { Sanctuary = null; return; }
            if (now + Epsilon < region.NextHitAt) return;
            region.NextHitAt += ExpandedBibleSanctuary.HitInterval;
            var radius = ExpandedBibleSanctuary.Radius;
            foreach (var target in targets().Where(target =>
                Vector2.DistanceSquared(region.Position, target.Value) <= (float)(radius * radius))
                .OrderBy(target => target.Key).ToArray())
                if (targets().ContainsKey(target.Key)) apply(target.Key, target.Value, region.Damage);
        }

        public void Validate(double now)
        {
            var maximumDamage = WeaponRules.Get("bible", 8).Damage * 2.25;
            if (PulseOrdinal < 0 || LastSteeredPulse < 0 || LastSteeredPulse > PulseOrdinal ||
                !BattleSnapshot.Finite(InnerAngleOffset) || Math.Abs(InnerAngleOffset) > Math.PI ||
                InnerAngleOffset != 0 && LastSteeredPulse == 0 ||
                PendingEcho is ExpandedBibleEcho echo && (PulseOrdinal == 0 || PulseOrdinal % 4 != 0 ||
                    !BattleSnapshot.Finite(echo.DueAt) || echo.DueAt < now - Epsilon || echo.DueAt > now + EchoDelay + Epsilon ||
                    !BattleSnapshot.Finite(echo.Damage) || echo.Damage <= 0 || echo.Damage > maximumDamage * EchoDamageFactor ||
                    !BattleSnapshot.Finite(echo.Radius) || echo.Radius < EvolutionRules.PulseRadius || echo.Radius > EvolutionRules.PulseRadius * 2.5) ||
                NextSanctuaryAt is double next && (!BattleSnapshot.Finite(next) || next < SanctuaryCooldown ||
                    next > now + SanctuaryCooldown + Epsilon))
                throw new ArgumentException("Invalid expanded Bible evolution");

            if (Sanctuary is ExpandedBibleSanctuary region && (!BattleSnapshot.Position(region.X, region.Y) ||
                !BattleSnapshot.Finite(region.StartedAt) || region.StartedAt < 0 || region.StartedAt > now + Epsilon ||
                region.EndsAt <= now + Epsilon || NextSanctuaryAt != region.StartedAt + SanctuaryCooldown ||
                !BattleSnapshot.Finite(region.NextHitAt) || region.NextHitAt < now - Epsilon || region.NextHitAt > region.EndsAt + Epsilon ||
                !ValidSanctuaryTick(region.NextHitAt - region.StartedAt) ||
                !BattleSnapshot.Finite(region.Damage) || region.Damage <= 0 || region.Damage > maximumDamage * ExpandedBibleSanctuary.DamageFactor))
                throw new ArgumentException("Invalid expanded Bible sanctuary");
        }

        private static bool ValidSanctuaryTick(double elapsed)
        {
            var ticks = elapsed / ExpandedBibleSanctuary.HitInterval;
            return elapsed >= -Epsilon && Math.Abs(ticks - Math.Round(ticks)) <= Epsilon;
        }

        private static double Normalize(double angle) => Math.Atan2(Math.Sin(angle), Math.Cos(angle));
    }
}
