using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;

namespace BibleSurvivor.Core
{
    public readonly struct ExpandedBibleLifecycleRules
    {
        public bool DeflectsHostile { get; }
        public bool CriticalCover { get; }
        public bool DamageBarrier { get; }
        public double DurationBonus { get; }
        public bool ExtendsAfterBookKills { get; }
        public bool ReducesWaitAfterEliteKill { get; }

        private ExpandedBibleLifecycleRules(IReadOnlyDictionary<string, int> ranks)
        {
            bool Has(string id) => ranks.TryGetValue("weapon.bible." + id, out var rank) && rank > 0;
            DeflectsHostile = Has("b.03");
            CriticalCover = Has("b.04");
            DamageBarrier = Has("b.05");
            DurationBonus = Has("d.01") ? .10 : 0;
            ExtendsAfterBookKills = Has("d.02");
            ReducesWaitAfterEliteKill = Has("d.05");
        }

        public static ExpandedBibleLifecycleRules FromRanks(IReadOnlyDictionary<string, int> ranks) =>
            new ExpandedBibleLifecycleRules(ranks ?? throw new ArgumentNullException(nameof(ranks)));
    }

    public readonly struct ExpandedBibleDeflectionContact
    {
        public long ProjectileId { get; }
        public int BookIndex { get; }
        public double At { get; }
        public bool OverlapsAtEnd { get; }

        public ExpandedBibleDeflectionContact(long projectileId, int bookIndex, double at, bool overlapsAtEnd)
        {
            ProjectileId = projectileId;
            BookIndex = bookIndex;
            At = at;
            OverlapsAtEnd = overlapsAtEnd;
        }
    }

    [DataContract]
    public sealed class ExpandedBibleDeflectionAttempt
    {
        [DataMember(IsRequired = true)] public long ProjectileId { get; set; }
        [DataMember(IsRequired = true)] public int BooksMask { get; set; }

        internal ExpandedBibleDeflectionAttempt Copy() => (ExpandedBibleDeflectionAttempt)MemberwiseClone();
    }

    [DataContract]
    public sealed class ExpandedBibleLifecycleState
    {
        private const double TimeEpsilon = 1e-9;
        private const int RequiredBookKills = 6;
        private const double LowHealth = .30;
        internal const double DeflectionCooldown = 2;
        internal const double CoverLifetime = 3;
        internal const double CoverCooldown = 8;
        internal const double BarrierCooldown = 5;
        internal const double EliteWaitCooldown = 6;
        public const double BarrierRadius = 1.6;
        public const double BarrierDamageFactor = .35;

        [DataMember(EmitDefaultValue = false)] public double? NextDeflectionAt { get; set; }
        [DataMember(EmitDefaultValue = false)] public uint? DeflectionRandomState { get; set; }
        [DataMember(EmitDefaultValue = false)] public List<ExpandedBibleDeflectionAttempt>? DeflectionAttempts { get; set; }
        [DataMember(EmitDefaultValue = false)] public double? CriticalCoverUntil { get; set; }
        [DataMember(EmitDefaultValue = false)] public double? NextCriticalCoverAt { get; set; }
        [DataMember(EmitDefaultValue = false)] public double? NextDamageBarrierAt { get; set; }
        [DataMember(EmitDefaultValue = false)] public List<int>? BookKillCounts { get; set; }
        [DataMember(EmitDefaultValue = false)] public bool NextWaitReductionArmed { get; set; }
        [DataMember(EmitDefaultValue = false)] public double? NextEliteWaitAt { get; set; }

        public ExpandedBibleLifecycleState Copy()
        {
            var copy = (ExpandedBibleLifecycleState)MemberwiseClone();
            copy.DeflectionAttempts = DeflectionAttempts?.Select(attempt => attempt.Copy()).ToList();
            copy.BookKillCounts = BookKillCounts?.ToList();
            return copy;
        }

        public void BeginBooks()
        {
            BookKillCounts = null;
            DeflectionAttempts = null;
        }

        public double ConsumeWaitReduction()
        {
            if (!NextWaitReductionArmed) return 0;
            NextWaitReductionArmed = false;
            return .10;
        }

        public double RecordAcceptedBookKill(ExpandedBibleLifecycleRules rules, double now,
            int bookIndex, int bookCount, bool elite, double nativeDuration, double currentDuration)
        {
            if (elite)
            {
                if (rules.ReducesWaitAfterEliteKill && !NextWaitReductionArmed && Ready(NextEliteWaitAt, now))
                {
                    NextWaitReductionArmed = true;
                    NextEliteWaitAt = now + EliteWaitCooldown;
                }
                return currentDuration;
            }
            if (!rules.ExtendsAfterBookKills || BookKillCounts?.Contains(RequiredBookKills) == true) return currentDuration;

            BookKillCounts ??= Enumerable.Repeat(0, bookCount).ToList();
            return ++BookKillCounts[bookIndex] == RequiredBookKills
                ? PermanentDurationRules.Extend(nativeDuration, currentDuration, .25) : currentDuration;
        }

        public bool ObserveDamage(ExpandedBibleLifecycleRules rules, double now, double healthBeforeRatio,
            double healthAfterRatio, double received, Func<bool> reserveBarrier)
        {
            if (received <= 0) return false;
            if (rules.CriticalCover && healthBeforeRatio > LowHealth && healthAfterRatio <= LowHealth && Ready(NextCriticalCoverAt, now))
            {
                CriticalCoverUntil = now + CoverLifetime;
                NextCriticalCoverAt = now + CoverCooldown;
            }
            if (!rules.DamageBarrier || !Ready(NextDamageBarrierAt, now) || !reserveBarrier()) return false;
            NextDamageBarrierAt = now + BarrierCooldown;
            return true;
        }

        public double CollisionRadiusBonus(double now) => now + TimeEpsilon < CriticalCoverUntil ? .15 : 0;

        public bool TryDeflect(ExpandedBibleLifecycleRules rules, double now, IEnumerable<ExpandedBibleDeflectionContact> contacts,
            bool sharedReady, Func<uint, uint> nextRandom, Func<long, bool> erase)
        {
            if (!rules.DeflectsHostile) return false;
            var prior = DeflectionAttempts?.ToDictionary(attempt => attempt.ProjectileId, attempt => attempt.BooksMask);
            var ongoing = new Dictionary<long, int>();
            long erased = 0;
            var ready = sharedReady && Ready(NextDeflectionAt, now);
            foreach (var contact in contacts.OrderBy(contact => contact.At).ThenBy(contact => contact.ProjectileId).ThenBy(contact => contact.BookIndex))
            {
                if (contact.ProjectileId == erased) continue;
                var mask = 1 << contact.BookIndex;
                var attempted = prior?.TryGetValue(contact.ProjectileId, out var previous) == true && (previous & mask) != 0;
                ongoing.TryGetValue(contact.ProjectileId, out var current);
                if (!attempted && (current & mask) == 0 && ready && erased == 0)
                {
                    attempted = true;
                    DeflectionRandomState = nextRandom(DeflectionRandomState ?? 0);
                    if (DeflectionRandomState.Value / 4294967296d < .20 && erase(contact.ProjectileId))
                    {
                        erased = contact.ProjectileId;
                        ongoing.Remove(erased);
                        NextDeflectionAt = now + DeflectionCooldown;
                        continue;
                    }
                }
                if (attempted && contact.OverlapsAtEnd) ongoing[contact.ProjectileId] = current | mask;
            }
            DeflectionAttempts = ongoing.Count == 0 ? null : ongoing.OrderBy(entry => entry.Key)
                .Select(entry => new ExpandedBibleDeflectionAttempt { ProjectileId = entry.Key, BooksMask = entry.Value }).ToList();
            return erased != 0;
        }

        public void PruneDeflectionAttempts(Func<long, bool> survives)
        {
            if (DeflectionAttempts == null) return;
            DeflectionAttempts.RemoveAll(attempt => !survives(attempt.ProjectileId));
            if (DeflectionAttempts.Count == 0) DeflectionAttempts = null;
        }

        public void Validate(double now, int bookCount)
        {
            if (bookCount < 0 || bookCount > CombatStats.ExpandedAmountLimit("bible") ||
                !ValidTime(NextDeflectionAt, now, DeflectionCooldown) ||
                !ValidTime(CriticalCoverUntil, now, CoverLifetime) ||
                !ValidTime(NextCriticalCoverAt, now, CoverCooldown) ||
                !ValidTime(NextDamageBarrierAt, now, BarrierCooldown) ||
                !ValidTime(NextEliteWaitAt, now, EliteWaitCooldown) ||
                DeflectionRandomState == 0 || NextDeflectionAt.HasValue && !DeflectionRandomState.HasValue ||
                NextWaitReductionArmed && !NextEliteWaitAt.HasValue ||
                CriticalCoverUntil.HasValue != NextCriticalCoverAt.HasValue ||
                CriticalCoverUntil.HasValue && Math.Abs(NextCriticalCoverAt!.Value - CriticalCoverUntil.Value -
                    (CoverCooldown - CoverLifetime)) > TimeEpsilon ||
                BookKillCounts != null && (bookCount == 0 || BookKillCounts.Count != bookCount ||
                    BookKillCounts.Any(count => count < 0 || count > RequiredBookKills) ||
                    BookKillCounts.All(count => count == 0) || BookKillCounts.Count(count => count == RequiredBookKills) > 1) ||
                DeflectionAttempts != null && (bookCount == 0 || !DeflectionRandomState.HasValue ||
                    DeflectionAttempts.Count == 0 || DeflectionAttempts.Count > BossDefinition.ChainCap ||
                    DeflectionAttempts.Any(attempt => attempt == null || attempt.ProjectileId <= 0 ||
                        attempt.BooksMask <= 0 || attempt.BooksMask >= 1 << bookCount) ||
                    DeflectionAttempts.Select(attempt => attempt.ProjectileId).Distinct().Count() != DeflectionAttempts.Count))
                throw new ArgumentException("Invalid expanded Bible lifecycle");
        }

        private static bool Ready(double? at, double now) => !at.HasValue || at.Value <= now + TimeEpsilon;

        private static bool ValidTime(double? at, double now, double duration) =>
            !at.HasValue || BattleSnapshot.Finite(at.Value) && at.Value >= duration - TimeEpsilon && at.Value <= now + duration + TimeEpsilon;
    }
}
