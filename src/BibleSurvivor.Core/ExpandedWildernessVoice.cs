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
        ExtraEcho,
    }

    [Flags]
    public enum ExpandedWildernessVoiceKillEffects : byte
    {
        None = 0,
        ShortenWait = 1,
        EliteHaste = 2,
        LongerCall = 4,
        BoostEcho = 8,
    }

    public readonly struct ExpandedWildernessVoiceRankOptions
    {
        public bool FirstPulseDamage { get; }
        public bool FinalPulseKnockback { get; }
        public bool LockFacing { get; }
        public bool FourthCycleFinalDamage { get; }
        public bool CenteredDamage { get; }
        public bool EdgeKnockback { get; }
        public bool WidenEvolution { get; }
        public bool WidenEchoes { get; }
        public bool RepeatFifthEcho { get; }
        public bool AimAtCrowd { get; }
        public bool TurnTowardRearCrowd { get; }
        public bool StrongerEvolvedCloseKnockback { get; }
        public bool MarkConsecutiveElite { get; }
        public bool CountHitsForFinish { get; }
        public bool StrengthenRepeatedTargets { get; }
        public bool ArmReachAfterDamage { get; }
        public ExpandedWildernessVoiceKillEffects KillEffects { get; }

        public ExpandedWildernessVoiceRankOptions(bool firstPulseDamage = false, bool finalPulseKnockback = false,
            bool lockFacing = false, bool fourthCycleFinalDamage = false, bool centeredDamage = false,
            bool edgeKnockback = false, bool widenEvolution = false, bool widenEchoes = false, bool repeatFifthEcho = false,
            bool aimAtCrowd = false, bool turnTowardRearCrowd = false, bool strongerEvolvedCloseKnockback = false,
            bool markConsecutiveElite = false, bool countHitsForFinish = false, bool strengthenRepeatedTargets = false,
            bool armReachAfterDamage = false, bool shortenNextWait = false, bool hurryAfterEliteKill = false,
            bool lengthenNextCast = false, bool empowerNextEcho = false)
        {
            FirstPulseDamage = firstPulseDamage;
            FinalPulseKnockback = finalPulseKnockback;
            LockFacing = lockFacing;
            FourthCycleFinalDamage = fourthCycleFinalDamage;
            CenteredDamage = centeredDamage;
            EdgeKnockback = edgeKnockback;
            WidenEvolution = widenEvolution;
            WidenEchoes = widenEchoes;
            RepeatFifthEcho = repeatFifthEcho;
            AimAtCrowd = aimAtCrowd;
            TurnTowardRearCrowd = turnTowardRearCrowd;
            StrongerEvolvedCloseKnockback = strongerEvolvedCloseKnockback;
            MarkConsecutiveElite = markConsecutiveElite;
            CountHitsForFinish = countHitsForFinish;
            StrengthenRepeatedTargets = strengthenRepeatedTargets;
            ArmReachAfterDamage = armReachAfterDamage;
            KillEffects = (shortenNextWait ? ExpandedWildernessVoiceKillEffects.ShortenWait : 0) |
                (hurryAfterEliteKill ? ExpandedWildernessVoiceKillEffects.EliteHaste : 0) |
                (lengthenNextCast ? ExpandedWildernessVoiceKillEffects.LongerCall : 0) |
                (empowerNextEcho ? ExpandedWildernessVoiceKillEffects.BoostEcho : 0);
        }

        public static ExpandedWildernessVoiceRankOptions FromRanks(IReadOnlyDictionary<string, int> ranks)
        {
            if (ranks == null) throw new ArgumentNullException(nameof(ranks));
            bool Has(string suffix) => ranks.TryGetValue("weapon.wilderness_voice." + suffix, out var rank) && rank > 0;
            return new ExpandedWildernessVoiceRankOptions(Has("a.02"), Has("a.03"), Has("a.04"), Has("a.05"),
                Has("b.02"), Has("b.03"), Has("e.01"), Has("e.02"), Has("e.03"),
                Has("b.04"), Has("b.05"), Has("e.04"), Has("c.03"), Has("c.05"), Has("d.03"),
                Has("d.04"), Has("d.01"), Has("d.02"), Has("d.05"), Has("e.05"));
        }
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
        public double OriginalBaseDamage { get; }
        public int TargetOrdinal { get; }

        internal ExpandedWildernessVoiceHit(ExpandedWildernessVoiceHitKind kind, int targetId,
            Vector2 position, Vector2 center, Vector2 facing, double damage, double knockback,
            long cycle, int pulseNumber, double originalBaseDamage, int targetOrdinal)
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
            OriginalBaseDamage = originalBaseDamage;
            TargetOrdinal = targetOrdinal;
        }
    }

    public readonly struct ExpandedWildernessVoiceOriginalOutcome
    {
        public ExpandedWildernessVoiceHit Hit { get; }
        public double PreCriticalDamage { get; }
        public bool Elite { get; }
        public bool Killed { get; }
        public bool FinalPulse { get; }
        public double At { get; }

        internal ExpandedWildernessVoiceOriginalOutcome(ExpandedWildernessVoiceHit hit, double preCriticalDamage,
            bool elite, bool killed, bool finalPulse, double at)
        {
            Hit = hit;
            PreCriticalDamage = preCriticalDamage;
            Elite = elite;
            Killed = killed;
            FinalPulse = finalPulse;
            At = at;
        }
    }

    [DataContract]
    public sealed class ExpandedWildernessVoiceTargetCount
    {
        [DataMember(IsRequired = true)] public int TargetId { get; set; }
        [DataMember(IsRequired = true)] public int Count { get; set; }
        internal ExpandedWildernessVoiceTargetCount Copy() => (ExpandedWildernessVoiceTargetCount)MemberwiseClone();
    }

    [DataContract]
    public sealed class ExpandedWildernessVoiceEliteSeries
    {
        [DataMember(IsRequired = true)] public int TargetId { get; set; }
        [DataMember(IsRequired = true)] public long LastPulse { get; set; }
        [DataMember(IsRequired = true)] public int Consecutive { get; set; }
        [DataMember(EmitDefaultValue = false)] public double? MarkUntil { get; set; }
        internal ExpandedWildernessVoiceEliteSeries Copy() => (ExpandedWildernessVoiceEliteSeries)MemberwiseClone();
    }

    [DataContract]
    public sealed class ExpandedWildernessVoiceCastState
    {
        [DataMember(IsRequired = true)] public long Id { get; set; }
        [DataMember(IsRequired = true)] public double StartedAt { get; set; }
        [DataMember(IsRequired = true)] public double Duration { get; set; }
        [DataMember(IsRequired = true)] public double Damage { get; set; }
        [DataMember(EmitDefaultValue = false)] public double OriginalBaseDamage { get; set; }
        [DataMember(IsRequired = true)] public double Reach { get; set; }
        [DataMember(IsRequired = true)] public double HalfAngle { get; set; }
        [DataMember(EmitDefaultValue = false)] public double? FirstDelay { get; set; }
        [DataMember(EmitDefaultValue = false)] public double KnockbackBonus { get; set; }
        [DataMember(EmitDefaultValue = false)] public double CloseKnockbackBonus { get; set; }
        [DataMember(EmitDefaultValue = false)] public bool ShortenedPrimaryInterval { get; set; }
        [DataMember(EmitDefaultValue = false)] public double EchoRadiusBonus { get; set; }
        [DataMember(EmitDefaultValue = false)] public bool FirstPulseDamage { get; set; }
        [DataMember(EmitDefaultValue = false)] public bool FinalPulseKnockback { get; set; }
        [DataMember(EmitDefaultValue = false)] public bool FourthCycleFinalDamage { get; set; }
        [DataMember(EmitDefaultValue = false)] public bool CenteredDamage { get; set; }
        [DataMember(EmitDefaultValue = false)] public bool EdgeKnockback { get; set; }
        [DataMember(EmitDefaultValue = false)] public float? LockedFacingX { get; set; }
        [DataMember(EmitDefaultValue = false)] public float? LockedFacingY { get; set; }
        [DataMember(EmitDefaultValue = false)] public float? InitialFacingX { get; set; }
        [DataMember(EmitDefaultValue = false)] public float? InitialFacingY { get; set; }
        [DataMember(EmitDefaultValue = false)] public bool EvolvedCloseKnockback { get; set; }
        [DataMember(EmitDefaultValue = false)] public bool TracksEliteSeries { get; set; }
        [DataMember(EmitDefaultValue = false)] public bool CountsHitsForFinish { get; set; }
        [DataMember(EmitDefaultValue = false)] public bool StrengthensRepeatedTargets { get; set; }
        [DataMember(EmitDefaultValue = false)] public List<ExpandedWildernessVoiceTargetCount>? OriginalTargetCounts { get; set; }
        [DataMember(EmitDefaultValue = false)] public double? NativeDuration { get; set; }
        [DataMember(EmitDefaultValue = false)] public PermanentDurationProgress? DurationProgress { get; set; }
        [DataMember(EmitDefaultValue = false)] public bool FinalEffectsUsed { get; set; }
        [DataMember(EmitDefaultValue = false)] public ExpandedWildernessVoiceKillEffects KillEffects { get; set; }
        [DataMember(EmitDefaultValue = false)] public int WaitNormalKills { get; set; }
        [DataMember(EmitDefaultValue = false)] public bool AddedReachBoost { get; set; }
        [DataMember(EmitDefaultValue = false)] public bool AddedCallDuration { get; set; }
        [DataMember(EmitDefaultValue = false)] public bool BoostFirstEcho { get; set; }
        [DataMember(EmitDefaultValue = false)] public int DamageLevel { get; set; }
        [DataMember(EmitDefaultValue = false)] public double? UncappedDamage { get; set; }
        [DataMember(EmitDefaultValue = false)] public bool HasExtraEcho { get; set; }
        [DataMember(IsRequired = true)] public int PrimaryPulses { get; set; }
        [DataMember(IsRequired = true)] public int EchoesFired { get; set; }

        public double FirstEchoRadius => 5 * (1 + EchoRadiusBonus);
        public double SecondEchoRadius => 5.5 * (1 + EchoRadiusBonus);
        // The repeat inherits the last base echo's footprint.
        public double ExtraEchoRadius => SecondEchoRadius;

        internal double End => StartedAt + Duration;
        internal double InitialDelay => FirstDelay ?? ExpandedWildernessVoice.Profile.FirstAttackDelay;
        internal double PrimaryAt(int pulse) =>
            StartedAt + InitialDelay + pulse * (ShortenedPrimaryInterval
                ? ExpandedWildernessVoice.ShortenedInterval : ExpandedWildernessVoice.Profile.RepeatHitInterval);
        internal double EchoAt(int echo) => StartedAt + (echo + 1) * ExpandedWildernessVoice.EchoInterval;
        internal int EchoCount(bool evolved) => evolved ? (HasExtraEcho ? 3 : 2) : 0;
        internal double ExpiresAt(bool evolved) => evolved ? Math.Max(End, EchoAt(EchoCount(true) - 1)) : End;
        public Vector2? InitialFacing => InitialFacingX is float x && InitialFacingY is float y ? new Vector2(x, y) : (Vector2?)null;

        internal Vector2 PulseFacing(Vector2 facing, bool firstPrimary = false) => LockedFacingX is float x && LockedFacingY is float y
            ? new Vector2(x, y) : firstPrimary ? InitialFacing ?? facing : facing;

        internal bool ValidRanks(bool evolved)
        {
            if ((FourthCycleFinalDamage && Id % 4 != 0) || (HasExtraEcho && (!evolved || Id % 5 != 0)) ||
                EvolvedCloseKnockback && !evolved || InitialFacingX.HasValue && LockedFacingX.HasValue ||
                !ValidFacing(LockedFacingX, LockedFacingY) || !ValidFacing(InitialFacingX, InitialFacingY) ||
                !ExpandedWildernessVoice.ValidKillEffects(KillEffects, evolved) ||
                WaitNormalKills < 0 || WaitNormalKills > ExpandedWildernessVoice.KillsPerWait ||
                WaitNormalKills != 0 && !KillEffects.HasFlag(ExpandedWildernessVoiceKillEffects.ShortenWait) ||
                BoostFirstEcho && !evolved ||
                (DurationProgress != null || AddedCallDuration) && !NativeDuration.HasValue ||
                FinalEffectsUsed && (DurationProgress == null || PrimaryPulses == 0) ||
                !ExpandedWildernessVoice.ValidNativeDuration(this, evolved))
                return false;

            DurationProgress?.Validate(Duration);
            if (DurationProgress != null && DurationProgress.NativeDuration != NativeDuration) return false;
            if (OriginalTargetCounts != null)
            {
                if (!StrengthensRepeatedTargets || OriginalTargetCounts.Count == 0 ||
                    OriginalTargetCounts.Count > ExpandedWildernessVoice.MaximumTrackedTargets)
                    return false;
                var previousId = 0;
                foreach (var target in OriginalTargetCounts)
                {
                    if (target == null || target.TargetId <= previousId || target.Count <= 0 || target.Count > PrimaryPulses)
                        return false;
                    previousId = target.TargetId;
                }
            }

            if (!FirstPulseDamage && !FourthCycleFinalDamage && !CenteredDamage && !TracksEliteSeries &&
                !CountsHitsForFinish && !StrengthensRepeatedTargets)
                return DamageLevel == 0 && !UncappedDamage.HasValue;
            return DamageLevel >= 1 && DamageLevel <= 8 && (!evolved || DamageLevel == 8) &&
                UncappedDamage is double damage && ExpandedWildernessVoice.ValidUncappedDamage(DamageLevel, damage, Damage);
        }

        private static bool ValidFacing(float? x, float? y) => x.HasValue == y.HasValue &&
            (!x.HasValue || BattleSnapshot.Position(x.Value, y!.Value) &&
                Math.Abs(new Vector2(x.Value, y.Value).LengthSquared() - 1) <= .0001);

        internal ExpandedWildernessVoiceCastState Copy()
        {
            var copy = (ExpandedWildernessVoiceCastState)MemberwiseClone();
            copy.OriginalTargetCounts = OriginalTargetCounts?.Select(target => target.Copy()).ToList();
            copy.DurationProgress = DurationProgress?.Copy();
            return copy;
        }
    }

    [DataContract]
    public sealed class ExpandedWildernessVoicePendingWait
    {
        [DataMember(IsRequired = true)] public double NativeDuration { get; set; }
        [DataMember(IsRequired = true)] public double FloorAt { get; set; }
        [DataMember(IsRequired = true)] public double UnshortenedAt { get; set; }
        [DataMember(EmitDefaultValue = false)] public bool Applied { get; set; }
        internal ExpandedWildernessVoicePendingWait Copy() => (ExpandedWildernessVoicePendingWait)MemberwiseClone();
    }

    [DataContract]
    public sealed class ExpandedWildernessVoiceState
    {
        [DataMember(IsRequired = true)] public bool Evolved { get; set; }
        [DataMember(IsRequired = true)] public long Cycle { get; set; }
        [DataMember(IsRequired = true)] public double NextCycle { get; set; }
        [DataMember(IsRequired = true)] public List<ExpandedWildernessVoiceCastState> Active { get; set; } =
            new List<ExpandedWildernessVoiceCastState>();

        [DataMember(EmitDefaultValue = false)] public long ElitePulseSequence { get; set; }
        [DataMember(EmitDefaultValue = false)] public List<ExpandedWildernessVoiceEliteSeries>? EliteSeries { get; set; }
        [DataMember(EmitDefaultValue = false)] public int FinishHits { get; set; }
        [DataMember(EmitDefaultValue = false)] public bool FinishReady { get; set; }
        [DataMember(EmitDefaultValue = false)] public double? NextFinishAt { get; set; }
        [DataMember(EmitDefaultValue = false)] public bool NextWaitReady { get; set; }
        [DataMember(EmitDefaultValue = false)] public double? NextWaitAt { get; set; }
        [DataMember(EmitDefaultValue = false)] public ExpandedWildernessVoicePendingWait? PendingWait { get; set; }
        [DataMember(EmitDefaultValue = false)] public double? HasteUntil { get; set; }
        [DataMember(EmitDefaultValue = false)] public double? NextHasteAt { get; set; }
        [DataMember(EmitDefaultValue = false)] public double? ReachBoostUntil { get; set; }
        [DataMember(EmitDefaultValue = false)] public double? NextReachAt { get; set; }
        [DataMember(EmitDefaultValue = false)] public bool LongCallReady { get; set; }
        [DataMember(EmitDefaultValue = false)] public int LongCallKills { get; set; }
        [DataMember(EmitDefaultValue = false)] public double? NextLongCallAt { get; set; }
        [DataMember(EmitDefaultValue = false)] public double? EchoBoostUntil { get; set; }
        [DataMember(EmitDefaultValue = false)] public double? NextEchoBoostAt { get; set; }

        public int ReservedRegions => Active.Count(cast => cast.HasExtraEcho && cast.EchoesFired < 3);

        public ExpandedWildernessVoiceState Copy() => new ExpandedWildernessVoiceState
        {
            Evolved = Evolved,
            Cycle = Cycle,
            NextCycle = NextCycle,
            ElitePulseSequence = ElitePulseSequence,
            EliteSeries = EliteSeries?.Select(series => series.Copy()).ToList(),
            FinishHits = FinishHits,
            FinishReady = FinishReady,
            NextFinishAt = NextFinishAt,
            NextWaitReady = NextWaitReady,
            NextWaitAt = NextWaitAt,
            PendingWait = PendingWait?.Copy(),
            HasteUntil = HasteUntil,
            NextHasteAt = NextHasteAt,
            ReachBoostUntil = ReachBoostUntil,
            NextReachAt = NextReachAt,
            LongCallReady = LongCallReady,
            LongCallKills = LongCallKills,
            NextLongCallAt = NextLongCallAt,
            EchoBoostUntil = EchoBoostUntil,
            NextEchoBoostAt = NextEchoBoostAt,
            Active = Active.Select(cast => cast.Copy()).ToList(),
        };

        // The caller owns the next executable tick and the round boundary.
        public void ResetForForm(bool evolved, double now)
        {
            if (!BattleSnapshot.Finite(now) || now < 0) throw new ArgumentOutOfRangeException(nameof(now));
            Evolved = evolved;
            NextCycle = now;
            Active.Clear();
            PendingWait = null;
            if (!evolved)
            {
                EchoBoostUntil = null;
                NextEchoBoostAt = null;
            }
        }

        public void Validate(double now, bool evolved)
        {
            const double epsilon = ExpandedWildernessVoice.TimeEpsilon;
            if (!BattleSnapshot.Finite(now) || now < 0 || evolved != Evolved || Cycle < 0 ||
                !BattleSnapshot.Finite(NextCycle) || NextCycle < 0 ||
                NextCycle > now + ExpandedWildernessVoice.MaximumCycle + epsilon ||
                Active == null || Active.Count > ExpandedWildernessVoice.MaximumActiveCasts ||
                ElitePulseSequence < 0 || ElitePulseSequence > 0 &&
                    (ElitePulseSequence - 1) / ExpandedWildernessVoice.MaximumPrimaryPulses >= Cycle ||
                FinishHits < 0 || FinishHits >= ExpandedWildernessVoice.HitsPerFinish ||
                FinishHits > 0 && (FinishReady || NextFinishAt.HasValue) ||
                !ExpandedWildernessVoice.ValidReceipt(NextFinishAt, now, ExpandedWildernessVoice.FinishCooldown) ||
                !ExpandedWildernessVoice.ValidReceipt(NextWaitAt, now, ExpandedWildernessVoice.WaitCooldown) ||
                !ExpandedWildernessVoice.ValidPendingWait(PendingWait, NextCycle, now, Cycle) ||
                !ExpandedWildernessVoice.ValidWindow(HasteUntil, NextHasteAt, now, ExpandedWildernessVoice.HasteDuration,
                    ExpandedWildernessVoice.HasteCooldown) ||
                !ExpandedWildernessVoice.ValidWindow(ReachBoostUntil, NextReachAt, now, ExpandedWildernessVoice.ReachWindow,
                    ExpandedWildernessVoice.ReachCooldown) ||
                LongCallKills < 0 || LongCallKills >= ExpandedWildernessVoice.KillsPerLongCall ||
                LongCallKills > 0 && (LongCallReady || NextLongCallAt.HasValue) ||
                !ExpandedWildernessVoice.ValidReceipt(NextLongCallAt, now, ExpandedWildernessVoice.LongCallCooldown) ||
                !ExpandedWildernessVoice.ValidWindow(EchoBoostUntil, NextEchoBoostAt, now, ExpandedWildernessVoice.EchoBoostWindow,
                    ExpandedWildernessVoice.EchoBoostCooldown) ||
                !evolved && (EchoBoostUntil.HasValue || NextEchoBoostAt.HasValue))
                throw new ArgumentException("Invalid wilderness voice clock");

            ValidateEliteSeries(now);
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
                    !ExpandedWildernessVoice.ValidEchoRadiusBonus(cast.EchoRadiusBonus) ||
                    (!evolved && cast.EchoRadiusBonus != 0) || !cast.ValidRanks(evolved) ||
                    !ExpandedWildernessVoice.ValidOriginalBaseDamage(cast, evolved) ||
                    cast.ExpiresAt(evolved) < now - epsilon ||
                    cast.PrimaryPulses < 0 || cast.PrimaryPulses > ExpandedWildernessVoice.MaximumPrimaryPulses ||
                    cast.EchoesFired < 0 || cast.EchoesFired > cast.EchoCount(evolved))
                    throw new ArgumentException("Invalid wilderness voice cast");

                var nextPrimary = cast.PrimaryAt(cast.PrimaryPulses);
                var previousPrimary = cast.PrimaryAt(cast.PrimaryPulses - 1);
                if (cast.PrimaryPulses > 0 && (previousPrimary > now + epsilon || previousPrimary >= cast.End - epsilon) ||
                    nextPrimary < cast.End - epsilon && nextPrimary < now - epsilon ||
                    cast.EchoesFired > 0 && cast.EchoAt(cast.EchoesFired - 1) > now + epsilon ||
                    cast.EchoesFired < cast.EchoCount(evolved) && cast.EchoAt(cast.EchoesFired) < now - epsilon)
                    throw new ArgumentException("Invalid wilderness voice hit schedule");

                previousId = cast.Id;
                previousStart = cast.StartedAt;
            }
        }

        private void ValidateEliteSeries(double now)
        {
            if (EliteSeries == null) return;
            if (EliteSeries.Count == 0 || EliteSeries.Count > ExpandedWildernessVoice.MaximumTrackedTargets)
                throw new ArgumentException("Invalid wilderness voice elite history count");
            var previousId = 0;
            foreach (var series in EliteSeries)
            {
                if (series == null || series.TargetId <= previousId || series.LastPulse < 1 || series.LastPulse > ElitePulseSequence ||
                    series.Consecutive < 0 || series.Consecutive >= ExpandedWildernessVoice.EliteHitsPerMark ||
                    series.Consecutive == 0 && !series.MarkUntil.HasValue ||
                    !ExpandedWildernessVoice.ValidReceipt(series.MarkUntil, now, ExpandedWildernessVoice.EliteMarkDuration))
                    throw new ArgumentException("Invalid wilderness voice elite history");
                previousId = series.TargetId;
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
            (int)Math.Ceiling((MaximumDuration - MinimumDelay) / ShortenedInterval);
        internal static double ShortenedInterval => Profile.RepeatHitInterval * .96;
        internal const double TimeEpsilon = 1e-9;
        internal const double MinimumCycle = .15;
        private const double MinimumDelay = .05;
        internal const double EchoInterval = .5;
        private const double AimRange = 5;
        private const double RearRange = 4;
        private const int RearCountLead = 3;
        private const double CrowdRadius = 1.5;
        private const double EvolutionCloseKnockback = .35;
        internal const int MaximumTrackedTargets = 120;
        internal const int EliteHitsPerMark = 3;
        internal const double EliteMarkDuration = 3;
        internal const int HitsPerFinish = 15;
        internal const double FinishCooldown = 5;
        private const int RepeatedTargetThreshold = 3;
        internal const int KillsPerWait = 5;
        internal const double WaitCooldown = 4;
        internal const double HasteDuration = 3;
        internal const double HasteCooldown = 5;
        internal const double ReachWindow = 4;
        internal const double ReachCooldown = 6;
        internal const int KillsPerLongCall = 15;
        internal const double LongCallCooldown = 5;
        internal const double EchoBoostWindow = 3;
        internal const double EchoBoostCooldown = 6;
        private const double ShortenWaitBonus = .10;
        private const double EliteHasteBonus = .06;
        private const double HurtReachBonus = .12;
        private const double LongCallSeconds = .5;
        private const double BoostFirstEchoFactor = .15;

        // Cast stats are frozen at start. Each atomic pulse samples the current
        // center and facing, then processes every eligible target once in ID order.
        public static void Step(ExpandedWildernessVoiceState state, double now, Vector2 playerPosition, Vector2 facing,
            int level, bool evolved, double damage, double cycle, double reach, double duration, double halfAngle,
            Func<IReadOnlyDictionary<int, Vector2>> targets, Action<ExpandedWildernessVoiceHit> applyHit,
            double delayAdvance = 0, double knockbackBonus = 0, double closeKnockbackBonus = 0,
            bool shortenedPrimaryInterval = false, Func<double>? nextCycleDuration = null, double echoRadiusBonus = 0,
            ExpandedWildernessVoiceRankOptions rankOptions = default, double? uncappedDamage = null,
            Func<int, bool>? isElite = null, Func<int, bool>? isBoss = null,
            Func<ExpandedWildernessVoiceHit, double?>? applyOriginalHit = null,
            Action<ExpandedWildernessVoiceOriginalOutcome>? observeOriginalHit = null,
            PermanentDurationRules durationRules = default, Func<bool>? consumeFirstSustained = null,
            Action<Vector2, Vector2, double, double>? beforeOriginalPulse = null,
            Func<bool>? reserveEchoRegion = null, Action? suppressEcho = null)
        {
            if (state == null) throw new ArgumentNullException(nameof(state));
            if (targets == null) throw new ArgumentNullException(nameof(targets));
            if (applyHit == null) throw new ArgumentNullException(nameof(applyHit));
            var countOriginalHits = rankOptions.MarkConsecutiveElite || rankOptions.CountHitsForFinish || rankOptions.StrengthenRepeatedTargets;
            var tracksKills = rankOptions.KillEffects != ExpandedWildernessVoiceKillEffects.None ||
                durationRules.Extensions != PermanentDurationExtensions.None;
            if ((rankOptions.TurnTowardRearCrowd || rankOptions.MarkConsecutiveElite || tracksKills || observeOriginalHit != null) && isElite == null)
                throw new ArgumentNullException(nameof(isElite));
            if ((countOriginalHits || tracksKills || observeOriginalHit != null) && applyOriginalHit == null)
                throw new ArgumentNullException(nameof(applyOriginalHit));
            if (rankOptions.TurnTowardRearCrowd && isBoss == null) throw new ArgumentNullException(nameof(isBoss));
            if (!BattleSnapshot.Finite(delayAdvance) || delayAdvance < 0) throw new ArgumentOutOfRangeException(nameof(delayAdvance));
            if (!ValidKnockbackBonus(knockbackBonus) || !ValidKnockbackBonus(closeKnockbackBonus))
                throw new ArgumentException("Invalid voice knockback bonus");
            if (!ValidEchoRadiusBonus(echoRadiusBonus)) throw new ArgumentOutOfRangeException(nameof(echoRadiusBonus));
            var delay = Math.Max(MinimumDelay, Profile.FirstAttackDelay - delayAdvance);
            if (!BattleSnapshot.Finite(now) || now < 0 ||
                !BattleSnapshot.Position(playerPosition.X, playerPosition.Y) || !BattleSnapshot.Position(facing.X, facing.Y) ||
                level < 1 || level > 8 || evolved && level != 8 ||
                !BattleSnapshot.Finite(cycle) || cycle < MinimumCycle || cycle > MaximumCycle ||
                !ValidCast(damage, reach, duration, halfAngle, evolved, delay))
                throw new ArgumentException("Invalid wilderness voice parameters");

            if (uncappedDamage.HasValue && !ValidUncappedDamage(level, uncappedDamage.Value, damage))
                throw new ArgumentException("Voice uncapped damage does not match its base pulse", nameof(uncappedDamage));
            if ((rankOptions.FirstPulseDamage || rankOptions.FourthCycleFinalDamage || rankOptions.CenteredDamage || countOriginalHits) &&
                !uncappedDamage.HasValue)
                throw new ArgumentException("Voice pulse bonuses require pre-cap damage", nameof(uncappedDamage));

            if (state.Evolved != evolved) state.ResetForForm(evolved, now);
            ExpireReceipts(state, now);
            PruneHistory(state, now, targets);
            var direction = facing.LengthSquared() > 1e-12f ? Vector2.Normalize(facing) : Vector2.UnitX;
            ResolveDue(state, now, playerPosition, direction, targets, applyHit, isElite, applyOriginalHit,
                observeOriginalHit, beforeOriginalPulse);
            PruneHistory(state, now, targets);

            if (state.NextCycle > now + TimeEpsilon) return;
            var start = state.NextCycle;
            state.Cycle = checked(state.Cycle + 1);
            var native = Profile.AtLevel(level);
            var firstSustained = durationRules.HasFirstSustainedBonus && consumeFirstSustained?.Invoke() == true;
            var launchDuration = durationRules.LaunchDuration(native.Duration, duration, firstSustained, evolved: evolved);
            var addedCall = state.LongCallReady && rankOptions.KillEffects.HasFlag(ExpandedWildernessVoiceKillEffects.LongerCall);
            if (addedCall)
            {
                launchDuration = PermanentDurationRules.Extend(native.Duration, launchDuration, LongCallSeconds);
                state.LongCallReady = false;
            }
            var durationProgress = durationRules.BeginKillTracking(native.Duration);
            var needsNativeDuration = addedCall || launchDuration != duration || durationProgress != null;
            var addedReach = state.ReachBoostUntil.HasValue && rankOptions.ArmReachAfterDamage;
            if (addedReach)
            {
                reach = Math.Min(MaximumReach, reach + Profile.AtLevel(1).Reach * HurtReachBonus);
                state.ReachBoostUntil = null;
            }
            var boostEcho = evolved && state.EchoBoostUntil.HasValue &&
                rankOptions.KillEffects.HasFlag(ExpandedWildernessVoiceKillEffects.BoostEcho);
            if (boostEcho) state.EchoBoostUntil = null;
            ScheduleNextWait(state, start, launchDuration, native.Cycle, nextCycleDuration?.Invoke() ?? cycle, rankOptions.KillEffects);
            var fourthCycleFinalDamage = rankOptions.FourthCycleFinalDamage && state.Cycle % 4 == 0;
            var needsDamageBasis = rankOptions.FirstPulseDamage || fourthCycleFinalDamage || rankOptions.CenteredDamage || countOriginalHits;
            if (evolved && rankOptions.WidenEvolution)
                reach = Math.Min(MaximumReach, reach + Profile.AtLevel(1).Reach * .15);
            var adjustedAim = rankOptions.AimAtCrowd || rankOptions.TurnTowardRearCrowd
                ? AimAtStart(playerPosition, direction, targets(), rankOptions, isElite, isBoss) : (Vector2?)null;
            var initialAim = rankOptions.LockFacing ? null : adjustedAim;
            var lockedAim = rankOptions.LockFacing ? adjustedAim ?? direction : (Vector2?)null;
            var extraEcho = evolved && rankOptions.RepeatFifthEcho && state.Cycle % 5 == 0;
            if (extraEcho && reserveEchoRegion?.Invoke() == false)
            {
                extraEcho = false;
                suppressEcho?.Invoke();
            }
            state.Active.Add(new ExpandedWildernessVoiceCastState
            {
                Id = state.Cycle,
                StartedAt = start,
                Damage = damage,
                OriginalBaseDamage = Profile.AtLevel(level).Damage,
                Reach = reach,
                Duration = launchDuration,
                NativeDuration = needsNativeDuration ? native.Duration : (double?)null,
                DurationProgress = durationProgress,
                KillEffects = evolved ? rankOptions.KillEffects : rankOptions.KillEffects & ~ExpandedWildernessVoiceKillEffects.BoostEcho,
                AddedReachBoost = addedReach,
                AddedCallDuration = addedCall,
                BoostFirstEcho = boostEcho,
                HalfAngle = halfAngle,
                FirstDelay = delay == Profile.FirstAttackDelay ? (double?)null : delay,
                KnockbackBonus = knockbackBonus,
                CloseKnockbackBonus = closeKnockbackBonus,
                ShortenedPrimaryInterval = shortenedPrimaryInterval,
                EchoRadiusBonus = evolved ? Math.Min(1.5, echoRadiusBonus + (rankOptions.WidenEchoes ? .12 : 0)) : 0,
                FirstPulseDamage = rankOptions.FirstPulseDamage,
                FinalPulseKnockback = rankOptions.FinalPulseKnockback,
                FourthCycleFinalDamage = fourthCycleFinalDamage,
                CenteredDamage = rankOptions.CenteredDamage,
                EdgeKnockback = rankOptions.EdgeKnockback,
                LockedFacingX = lockedAim?.X,
                LockedFacingY = lockedAim?.Y,
                InitialFacingX = initialAim?.X,
                InitialFacingY = initialAim?.Y,
                EvolvedCloseKnockback = evolved && rankOptions.StrongerEvolvedCloseKnockback,
                TracksEliteSeries = rankOptions.MarkConsecutiveElite,
                CountsHitsForFinish = rankOptions.CountHitsForFinish,
                StrengthensRepeatedTargets = rankOptions.StrengthenRepeatedTargets,
                DamageLevel = needsDamageBasis ? level : 0,
                UncappedDamage = needsDamageBasis ? uncappedDamage : null,
                HasExtraEcho = extraEcho,
            });
        }

        public static bool TryArmReachAfterDamage(ExpandedWildernessVoiceState state, double now, double receivedDamage,
            ExpandedWildernessVoiceRankOptions ranks)
        {
            if (state == null) throw new ArgumentNullException(nameof(state));
            if (!BattleSnapshot.Finite(now) || now < 0) throw new ArgumentOutOfRangeException(nameof(now));
            if (!ranks.ArmReachAfterDamage || !BattleSnapshot.Finite(receivedDamage) || receivedDamage <= 0 ||
                state.NextReachAt > now + TimeEpsilon)
                return false;
            state.ReachBoostUntil = now + ReachWindow;
            state.NextReachAt = now + ReachCooldown;
            return true;
        }

        private static void ExpireReceipts(ExpandedWildernessVoiceState state, double now)
        {
            if (state.NextFinishAt <= now + TimeEpsilon) state.NextFinishAt = null;
            if (state.NextWaitAt <= now + TimeEpsilon) state.NextWaitAt = null;
            if (state.HasteUntil < now - TimeEpsilon) state.HasteUntil = null;
            if (state.NextHasteAt <= now + TimeEpsilon) state.NextHasteAt = null;
            if (state.ReachBoostUntil < now - TimeEpsilon) state.ReachBoostUntil = null;
            if (state.NextReachAt <= now + TimeEpsilon) state.NextReachAt = null;
            if (state.NextLongCallAt <= now + TimeEpsilon) state.NextLongCallAt = null;
            if (state.EchoBoostUntil < now - TimeEpsilon) state.EchoBoostUntil = null;
            if (state.NextEchoBoostAt <= now + TimeEpsilon) state.NextEchoBoostAt = null;
        }

        private static void ScheduleNextWait(ExpandedWildernessVoiceState state, double start, double duration,
            double nativeCycle, double computedCycle, ExpandedWildernessVoiceKillEffects rankEffects)
        {
            var waitFloor = Math.Max(MinimumCycle, nativeCycle * .20);
            var haste = state.HasteUntil.HasValue && rankEffects.HasFlag(ExpandedWildernessVoiceKillEffects.EliteHaste);
            var next = start + (haste ? Math.Max(waitFloor, computedCycle - nativeCycle * EliteHasteBonus) : computedCycle);
            state.NextCycle = next;
            state.PendingWait = null;
            if (!rankEffects.HasFlag(ExpandedWildernessVoiceKillEffects.ShortenWait)) return;
            var pending = new ExpandedWildernessVoicePendingWait
            {
                NativeDuration = nativeCycle, UnshortenedAt = next,
                FloorAt = start + Math.Max(waitFloor, duration + MinimumCycle),
            };
            state.PendingWait = pending;
            if (!state.NextWaitReady) return;
            state.NextWaitReady = false;
            ShortenPendingWait(state, pending, start);
        }

        private static void ShortenPendingWait(ExpandedWildernessVoiceState state, ExpandedWildernessVoicePendingWait pending, double now)
        {
            pending.Applied = true;
            state.NextCycle = Math.Min(pending.UnshortenedAt,
                Math.Max(now, EarliestShortenedWait(pending)));
        }

        private static double EarliestShortenedWait(ExpandedWildernessVoicePendingWait pending) =>
            Math.Max(pending.FloorAt, pending.UnshortenedAt - pending.NativeDuration * ShortenWaitBonus);

        private static Vector2? AimAtStart(Vector2 center, Vector2 facing, IReadOnlyDictionary<int, Vector2> targets,
            ExpandedWildernessVoiceRankOptions ranks, Func<int, bool>? isElite, Func<int, bool>? isBoss)
        {
            var front = 0;
            var rear = 0;
            if (ranks.TurnTowardRearCrowd)
            {
                foreach (var position in targets.Values)
                {
                    var offset = position - center;
                    if (offset.LengthSquared() > RearRange * RearRange) continue;
                    if (Behind(offset)) rear++;
                    else front++;
                }
            }

            var turn = rear - front >= RearCountLead;
            if (!turn && !ranks.AimAtCrowd) return null;
            var range = turn ? RearRange : AimRange;
            var best = targets.Select(target => (target.Key, target.Value, Offset: target.Value - center))
                .Where(target => target.Offset.LengthSquared() > 1e-12f && target.Offset.LengthSquared() <= range * range &&
                    (!turn || Behind(target.Offset)))
                .OrderByDescending(target => turn ? isBoss!(target.Key) ? 2 : isElite!(target.Key) ? 1 : 0 : 0)
                .ThenByDescending(target => targets.Values.Count(position =>
                    Vector2.DistanceSquared(target.Value, position) <= CrowdRadius * CrowdRadius))
                .ThenBy(target => target.Offset.LengthSquared())
                .ThenBy(target => target.Key)
                .FirstOrDefault();
            return best.Key == 0 ? (Vector2?)null : Vector2.Normalize(best.Offset);

            bool Behind(Vector2 offset) => Vector2.Dot(offset, facing) < 0;
        }

        private static void ResolveDue(ExpandedWildernessVoiceState state, double now, Vector2 center, Vector2 direction,
            Func<IReadOnlyDictionary<int, Vector2>> targets, Action<ExpandedWildernessVoiceHit> applyHit,
            Func<int, bool>? isElite, Func<ExpandedWildernessVoiceHit, double?>? applyOriginalHit,
            Action<ExpandedWildernessVoiceOriginalOutcome>? observeOriginalHit,
            Action<Vector2, Vector2, double, double>? beforeOriginalPulse)
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
                HitPulse(state, chosen, kind, now, center, direction, targets, applyHit, isElite, applyOriginalHit,
                    observeOriginalHit, beforeOriginalPulse);
            }

            state.Active.RemoveAll(cast => cast.ExpiresAt(state.Evolved) <= now + TimeEpsilon);
        }

        private static (double At, ExpandedWildernessVoiceHitKind Kind) NextPulse(
            ExpandedWildernessVoiceCastState cast, bool evolved)
        {
            var primary = cast.PrimaryAt(cast.PrimaryPulses);
            if (primary >= cast.End - TimeEpsilon) primary = double.PositiveInfinity;
            if (cast.EchoesFired >= cast.EchoCount(evolved) || primary <= cast.EchoAt(cast.EchoesFired))
                return (primary, ExpandedWildernessVoiceHitKind.Primary);
            var kind = cast.EchoesFired switch
            {
                0 => ExpandedWildernessVoiceHitKind.FirstEcho,
                1 => ExpandedWildernessVoiceHitKind.SecondEcho,
                _ => ExpandedWildernessVoiceHitKind.ExtraEcho,
            };
            return (cast.EchoAt(cast.EchoesFired), kind);
        }

        private static void HitPulse(ExpandedWildernessVoiceState state, ExpandedWildernessVoiceCastState cast,
            ExpandedWildernessVoiceHitKind kind, double now, Vector2 center, Vector2 direction,
            Func<IReadOnlyDictionary<int, Vector2>> targets, Action<ExpandedWildernessVoiceHit> applyHit, Func<int, bool>? isElite,
            Func<ExpandedWildernessVoiceHit, double?>? applyOriginalHit, Action<ExpandedWildernessVoiceOriginalOutcome>? observeOriginalHit,
            Action<Vector2, Vector2, double, double>? beforeOriginalPulse)
        {
            var primary = kind == ExpandedWildernessVoiceHitKind.Primary;
            var tracksKills = cast.DurationProgress != null || cast.KillEffects != ExpandedWildernessVoiceKillEffects.None;
            if (primary && (cast.TracksEliteSeries || cast.CountsHitsForFinish || cast.StrengthensRepeatedTargets || tracksKills) &&
                (applyOriginalHit == null || (cast.TracksEliteSeries || tracksKills) && isElite == null))
                throw new ArgumentException("Voice trained original pulses require an accepted-hit callback and elite classification where applicable");
            if (primary && cast.TracksEliteSeries)
                state.ElitePulseSequence = checked(state.ElitePulseSequence + 1);
            var pulseNumber = primary ? ++cast.PrimaryPulses : ++cast.EchoesFired;
            var reach = kind switch
            {
                ExpandedWildernessVoiceHitKind.Primary => cast.Reach,
                ExpandedWildernessVoiceHitKind.FirstEcho => cast.FirstEchoRadius,
                ExpandedWildernessVoiceHitKind.SecondEcho => cast.SecondEchoRadius,
                _ => cast.ExtraEchoRadius,
            };
            var factor = kind switch
            {
                ExpandedWildernessVoiceHitKind.Primary => 1,
                ExpandedWildernessVoiceHitKind.FirstEcho => .45 + (cast.BoostFirstEcho ? BoostFirstEchoFactor : 0),
                ExpandedWildernessVoiceHitKind.SecondEcho => .30,
                _ => .35,
            };
            // A full-circle primary still uses its facing for directional modifiers.
            var halfAngle = primary && !state.Evolved ? cast.HalfAngle : 180;
            var aim = cast.PulseFacing(direction, primary && pulseNumber == 1);
            var final = primary && !cast.FinalEffectsUsed && cast.PrimaryAt(cast.PrimaryPulses) >= cast.End - TimeEpsilon;
            if (final && cast.DurationProgress != null) cast.FinalEffectsUsed = true;
            if (primary) beforeOriginalPulse?.Invoke(center, aim, reach, halfAngle);
            var charged = final && cast.CountsHitsForFinish && state.FinishReady;
            if (charged) state.FinishReady = false;
            var pulseBonus = (primary && pulseNumber == 1 && cast.FirstPulseDamage ? .08 : 0) +
                (final && cast.FourthCycleFinalDamage ? .12 : 0) + (charged ? .12 : 0);
            var ids = targets().Keys.OrderBy(id => id).ToArray();
            var targetOrdinal = 0;
            foreach (var id in ids)
            {
                if (!targets().TryGetValue(id, out var position)) continue;
                var offset = position - center;
                if (!HitGeometry.InArc(offset.X, offset.Y, aim.X, aim.Y, reach, 2 * halfAngle)) continue;
                var angle = primary && (cast.CenteredDamage || cast.EdgeKnockback) ? DegreesFrom(aim, offset) : 0;
                var centered = primary && cast.CenteredDamage && angle <= 15 + GeometryEpsilon;
                var elite = primary && (cast.TracksEliteSeries || tracksKills || observeOriginalHit != null) && isElite!(id);
                var series = elite && cast.TracksEliteSeries ? state.EliteSeries?.Find(entry => entry.TargetId == id) : null;
                var previous = primary && cast.StrengthensRepeatedTargets ? cast.OriginalTargetCounts?.Find(entry => entry.TargetId == id) : null;
                var damageBonus = pulseBonus + (centered ? .08 : 0) +
                    (series?.MarkUntil >= now - TimeEpsilon ? .08 : 0) +
                    (previous?.Count >= RepeatedTargetThreshold - 1 ? .08 : 0);
                var damage = damageBonus > 0
                    ? CappedDamage(cast.DamageLevel, cast.UncappedDamage!.Value, damageBonus) : cast.Damage * factor;
                var knockback = primary ? PrimaryKnockback(cast, offset, angle, halfAngle, final) : 0;
                var hit = new ExpandedWildernessVoiceHit(kind, id, position, center, aim,
                    damage, knockback, cast.Id, pulseNumber,
                    primary ? cast.OriginalBaseDamage : 0, primary ? targetOrdinal + 1 : 0);
                if (!primary || applyOriginalHit == null)
                {
                    applyHit(hit);
                    if (primary) targetOrdinal++;
                    continue;
                }

                var accepted = applyOriginalHit(hit);
                if (!accepted.HasValue || !BattleSnapshot.Finite(accepted.Value) || accepted.Value <= 0) continue;
                targetOrdinal++;
                if (cast.CountsHitsForFinish && !state.FinishReady && !state.NextFinishAt.HasValue)
                {
                    if (++state.FinishHits == HitsPerFinish)
                    {
                        state.FinishHits = 0;
                        state.FinishReady = true;
                        state.NextFinishAt = now + FinishCooldown;
                    }
                }
                var killed = !targets().ContainsKey(id);
                if (killed && tracksKills) RecordOriginalKill(state, cast, now, elite);
                if (!killed)
                {
                    if (elite && cast.TracksEliteSeries) RecordEliteHit(state, series, id, now);
                    if (cast.StrengthensRepeatedTargets) RecordTargetHit(cast, previous, id);
                }
                observeOriginalHit?.Invoke(new ExpandedWildernessVoiceOriginalOutcome(hit, accepted.Value, elite, killed, final, now));
            }
        }

        private static void RecordOriginalKill(ExpandedWildernessVoiceState state, ExpandedWildernessVoiceCastState cast,
            double now, bool elite)
        {
            if (cast.DurationProgress != null)
            {
                var oldDuration = cast.Duration;
                cast.Duration = cast.DurationProgress.RecordOriginalKill(oldDuration, elite);
                if (cast.Duration > oldDuration && cast.Id == state.Cycle && state.PendingWait is { } pending)
                {
                    pending.FloorAt = Math.Max(pending.FloorAt, cast.End + MinimumCycle);
                    if (pending.Applied && state.NextCycle > now + TimeEpsilon)
                        state.NextCycle = Math.Min(pending.UnshortenedAt, Math.Max(state.NextCycle, pending.FloorAt));
                }
            }

            var effects = cast.KillEffects;
            if (elite)
            {
                if (effects.HasFlag(ExpandedWildernessVoiceKillEffects.EliteHaste) && !state.NextHasteAt.HasValue)
                {
                    state.HasteUntil = now + HasteDuration;
                    state.NextHasteAt = now + HasteCooldown;
                }
                if (effects.HasFlag(ExpandedWildernessVoiceKillEffects.BoostEcho) && !state.NextEchoBoostAt.HasValue)
                {
                    state.EchoBoostUntil = now + EchoBoostWindow;
                    state.NextEchoBoostAt = now + EchoBoostCooldown;
                }
                return;
            }

            if (effects.HasFlag(ExpandedWildernessVoiceKillEffects.ShortenWait) && cast.WaitNormalKills < KillsPerWait &&
                ++cast.WaitNormalKills == KillsPerWait && !state.NextWaitReady && !state.NextWaitAt.HasValue)
            {
                if (state.NextCycle > now + TimeEpsilon && state.PendingWait is { Applied: false } pending)
                    ShortenPendingWait(state, pending, now);
                else state.NextWaitReady = true;
                state.NextWaitAt = now + WaitCooldown;
            }
            if (effects.HasFlag(ExpandedWildernessVoiceKillEffects.LongerCall) && !state.LongCallReady && !state.NextLongCallAt.HasValue &&
                ++state.LongCallKills == KillsPerLongCall)
            {
                state.LongCallKills = 0;
                state.LongCallReady = true;
                state.NextLongCallAt = now + LongCallCooldown;
            }
        }

        private static void RecordEliteHit(ExpandedWildernessVoiceState state, ExpandedWildernessVoiceEliteSeries? series, int id, double now)
        {
            if (series == null)
            {
                state.EliteSeries ??= new List<ExpandedWildernessVoiceEliteSeries>();
                series = new ExpandedWildernessVoiceEliteSeries { TargetId = id };
                var index = state.EliteSeries.FindIndex(entry => entry.TargetId > id);
                if (index < 0) state.EliteSeries.Add(series);
                else state.EliteSeries.Insert(index, series);
            }
            series.Consecutive = series.LastPulse == state.ElitePulseSequence - 1 ? series.Consecutive + 1 : 1;
            series.LastPulse = state.ElitePulseSequence;
            if (series.Consecutive == EliteHitsPerMark)
            {
                series.Consecutive = 0;
                series.MarkUntil = now + EliteMarkDuration;
            }
        }

        private static void RecordTargetHit(ExpandedWildernessVoiceCastState cast, ExpandedWildernessVoiceTargetCount? count, int id)
        {
            if (count != null)
            {
                count.Count++;
                return;
            }

            cast.OriginalTargetCounts ??= new List<ExpandedWildernessVoiceTargetCount>();
            count = new ExpandedWildernessVoiceTargetCount { TargetId = id, Count = 1 };
            var index = cast.OriginalTargetCounts.FindIndex(entry => entry.TargetId > id);
            if (index < 0) cast.OriginalTargetCounts.Add(count);
            else cast.OriginalTargetCounts.Insert(index, count);
        }

        private static void PruneHistory(ExpandedWildernessVoiceState state, double now,
            Func<IReadOnlyDictionary<int, Vector2>> targets)
        {
            if (state.EliteSeries == null && !state.Active.Any(cast => cast.OriginalTargetCounts != null)) return;
            var current = targets();
            if (state.EliteSeries != null)
            {
                foreach (var series in state.EliteSeries)
                {
                    if (series.MarkUntil < now - TimeEpsilon) series.MarkUntil = null;
                    if (series.LastPulse < state.ElitePulseSequence) series.Consecutive = 0;
                }
                state.EliteSeries.RemoveAll(series => !current.ContainsKey(series.TargetId) ||
                    series.Consecutive == 0 && !series.MarkUntil.HasValue);
                if (state.EliteSeries.Count == 0) state.EliteSeries = null;
            }

            foreach (var cast in state.Active)
            {
                cast.OriginalTargetCounts?.RemoveAll(entry => !current.ContainsKey(entry.TargetId));
                if (cast.OriginalTargetCounts?.Count == 0) cast.OriginalTargetCounts = null;
            }
        }

        private const double GeometryEpsilon = 1e-5;

        private static double DegreesFrom(Vector2 direction, Vector2 offset)
        {
            var cross = (double)direction.X * offset.Y - (double)direction.Y * offset.X;
            var dot = (double)direction.X * offset.X + (double)direction.Y * offset.Y;
            return Math.Atan2(Math.Abs(cross), dot) * 180 / Math.PI;
        }

        private static double PrimaryKnockback(ExpandedWildernessVoiceCastState cast, Vector2 offset,
            double angle, double halfAngle, bool final)
        {
            var close = offset.LengthSquared() <= 1.2f * 1.2f
                ? cast.CloseKnockbackBonus + (cast.EvolvedCloseKnockback ? EvolutionCloseKnockback : 0) : 0;
            var edge = cast.EdgeKnockback && angle + GeometryEpsilon >= halfAngle * .8 ? .25 : 0;
            var finish = final && cast.FinalPulseKnockback ? .30 : 0;
            return Math.Min(1, Profile.Knockback + cast.KnockbackBonus + close + edge + finish);
        }

        private static double CappedDamage(int level, double uncapped, double extra)
        {
            var basis = Profile.AtLevel(level).Damage;
            return Math.Max(1, Math.Min(basis * 2.25, uncapped + basis * extra));
        }

        internal static bool ValidKillEffects(ExpandedWildernessVoiceKillEffects effects, bool evolved)
        {
            const ExpandedWildernessVoiceKillEffects common = ExpandedWildernessVoiceKillEffects.ShortenWait |
                ExpandedWildernessVoiceKillEffects.EliteHaste | ExpandedWildernessVoiceKillEffects.LongerCall;
            var valid = evolved ? common | ExpandedWildernessVoiceKillEffects.BoostEcho : common;
            return (effects & ~valid) == 0;
        }

        internal static bool ValidNativeDuration(ExpandedWildernessVoiceCastState cast, bool evolved)
        {
            if (!cast.NativeDuration.HasValue) return true;
            var native = cast.NativeDuration.Value;
            if (!BattleSnapshot.Finite(native) || cast.Duration < native - TimeEpsilon ||
                cast.Duration > native * PermanentDurationRules.MaximumScale + TimeEpsilon) return false;
            if (evolved) return native == Profile.AtLevel(8).Duration;
            if (cast.DamageLevel != 0) return native == Profile.AtLevel(cast.DamageLevel).Duration;
            return Enumerable.Range(1, 8).Any(level => Profile.AtLevel(level).Duration == native &&
                (cast.OriginalBaseDamage == 0 || Profile.AtLevel(level).Damage == cast.OriginalBaseDamage));
        }

        internal static bool ValidPendingWait(ExpandedWildernessVoicePendingWait? wait, double nextCycle, double now, long cycle) =>
            wait == null || cycle > 0 && Enumerable.Range(1, 8).Any(level => Profile.AtLevel(level).Cycle == wait.NativeDuration) &&
            BattleSnapshot.Finite(wait.FloorAt) && wait.FloorAt >= 0 && wait.FloorAt <= now + MaximumDuration + MinimumCycle + TimeEpsilon &&
            ValidReceipt(wait.UnshortenedAt, now, MaximumCycle) && wait.UnshortenedAt >= nextCycle - TimeEpsilon &&
            (wait.Applied
                ? nextCycle >= Math.Min(wait.UnshortenedAt, EarliestShortenedWait(wait)) - TimeEpsilon
                : wait.UnshortenedAt == nextCycle);

        internal static bool ValidWindow(double? expiry, double? next, double now, double duration, double cooldown) =>
            ValidReceipt(expiry, now, duration) && ValidReceipt(next, now, cooldown) &&
            (!expiry.HasValue || next.HasValue && Math.Abs(next.Value - expiry.Value - (cooldown - duration)) <= TimeEpsilon);

        internal static bool ValidReceipt(double? at, double now, double horizon) =>
            !at.HasValue || BattleSnapshot.Finite(at.Value) && at.Value >= now - TimeEpsilon && at.Value <= now + horizon + TimeEpsilon;

        internal static bool ValidUncappedDamage(int level, double uncapped, double damage) =>
            BattleSnapshot.Finite(uncapped) && uncapped >= 0 && Math.Abs(CappedDamage(level, uncapped, 0) - damage) <= TimeEpsilon;

        internal static bool ValidCast(double damage, double reach, double duration, double halfAngle, bool evolved, double delay) =>
            BattleSnapshot.Finite(delay) && delay >= MinimumDelay && delay <= Profile.FirstAttackDelay &&
            BattleSnapshot.Finite(damage) && damage >= 1 && damage <= MaximumDamage &&
            BattleSnapshot.Finite(reach) && reach > 0 && reach <= MaximumReach &&
            BattleSnapshot.Finite(duration) && duration > delay && duration <= MaximumDuration &&
            BattleSnapshot.Finite(halfAngle) && halfAngle > 0 && halfAngle <= (evolved ? 180 : 90);

        internal static bool ValidOriginalBaseDamage(ExpandedWildernessVoiceCastState cast, bool evolved)
        {
            var damage = cast.OriginalBaseDamage;
            if (damage == 0) return true;
            if (cast.DamageLevel is >= 1 and <= 8) return damage == Profile.AtLevel(cast.DamageLevel).Damage;
            return damage == Profile.AtLevel(8).Damage || !evolved &&
                (damage == Profile.AtLevel(1).Damage || damage == Profile.AtLevel(4).Damage);
        }

        internal static bool ValidKnockbackBonus(double bonus) => BattleSnapshot.Finite(bonus) && bonus >= 0 && bonus <= 1;
        internal static bool ValidEchoRadiusBonus(double bonus) => BattleSnapshot.Finite(bonus) && bonus >= 0 && bonus <= 1.5;
    }
}
