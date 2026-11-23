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

        public ExpandedWildernessVoiceRankOptions(bool firstPulseDamage = false, bool finalPulseKnockback = false,
            bool lockFacing = false, bool fourthCycleFinalDamage = false, bool centeredDamage = false,
            bool edgeKnockback = false, bool widenEvolution = false, bool widenEchoes = false, bool repeatFifthEcho = false,
            bool aimAtCrowd = false, bool turnTowardRearCrowd = false, bool strongerEvolvedCloseKnockback = false,
            bool markConsecutiveElite = false, bool countHitsForFinish = false, bool strengthenRepeatedTargets = false)
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
        }

        public static ExpandedWildernessVoiceRankOptions FromRanks(IReadOnlyDictionary<string, int> ranks)
        {
            if (ranks == null) throw new ArgumentNullException(nameof(ranks));
            bool Has(string suffix) => ranks.TryGetValue("weapon.wilderness_voice." + suffix, out var rank) && rank > 0;
            return new ExpandedWildernessVoiceRankOptions(Has("a.02"), Has("a.03"), Has("a.04"), Has("a.05"),
                Has("b.02"), Has("b.03"), Has("e.01"), Has("e.02"), Has("e.03"),
                Has("b.04"), Has("b.05"), Has("e.04"), Has("c.03"), Has("c.05"), Has("d.03"));
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
                !ValidFacing(LockedFacingX, LockedFacingY) || !ValidFacing(InitialFacingX, InitialFacingY))
                return false;

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
            return copy;
        }
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
                Active == null || Active.Count > ExpandedWildernessVoice.MaximumActiveCasts ||
                ElitePulseSequence < 0 || ElitePulseSequence > 0 &&
                    (ElitePulseSequence - 1) / ExpandedWildernessVoice.MaximumPrimaryPulses >= Cycle ||
                FinishHits < 0 || FinishHits >= ExpandedWildernessVoice.HitsPerFinish ||
                FinishHits > 0 && (FinishReady || NextFinishAt.HasValue) ||
                !ExpandedWildernessVoice.ValidReceipt(NextFinishAt, now, ExpandedWildernessVoice.FinishCooldown))
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
            Action<ExpandedWildernessVoiceOriginalOutcome>? observeOriginalHit = null)
        {
            if (state == null) throw new ArgumentNullException(nameof(state));
            if (targets == null) throw new ArgumentNullException(nameof(targets));
            if (applyHit == null) throw new ArgumentNullException(nameof(applyHit));
            var countOriginalHits = rankOptions.MarkConsecutiveElite || rankOptions.CountHitsForFinish || rankOptions.StrengthenRepeatedTargets;
            if ((rankOptions.TurnTowardRearCrowd || rankOptions.MarkConsecutiveElite || observeOriginalHit != null) && isElite == null)
                throw new ArgumentNullException(nameof(isElite));
            if ((countOriginalHits || observeOriginalHit != null) && applyOriginalHit == null)
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
            if (state.NextFinishAt <= now + TimeEpsilon) state.NextFinishAt = null;
            PruneHistory(state, now, targets);
            var direction = facing.LengthSquared() > 1e-12f ? Vector2.Normalize(facing) : Vector2.UnitX;
            ResolveDue(state, now, playerPosition, direction, targets, applyHit, isElite, applyOriginalHit, observeOriginalHit);
            PruneHistory(state, now, targets);

            if (state.NextCycle > now + TimeEpsilon) return;
            var start = state.NextCycle;
            state.Cycle = checked(state.Cycle + 1);
            state.NextCycle = start + (nextCycleDuration?.Invoke() ?? cycle);
            var fourthCycleFinalDamage = rankOptions.FourthCycleFinalDamage && state.Cycle % 4 == 0;
            var needsDamageBasis = rankOptions.FirstPulseDamage || fourthCycleFinalDamage || rankOptions.CenteredDamage || countOriginalHits;
            if (evolved && rankOptions.WidenEvolution)
                reach = Math.Min(MaximumReach, reach + Profile.AtLevel(1).Reach * .15);
            var adjustedAim = rankOptions.AimAtCrowd || rankOptions.TurnTowardRearCrowd
                ? AimAtStart(playerPosition, direction, targets(), rankOptions, isElite, isBoss) : (Vector2?)null;
            var initialAim = rankOptions.LockFacing ? null : adjustedAim;
            var lockedAim = rankOptions.LockFacing ? adjustedAim ?? direction : (Vector2?)null;
            state.Active.Add(new ExpandedWildernessVoiceCastState
            {
                Id = state.Cycle,
                StartedAt = start,
                Damage = damage,
                OriginalBaseDamage = Profile.AtLevel(level).Damage,
                Reach = reach,
                Duration = duration,
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
                HasExtraEcho = evolved && rankOptions.RepeatFifthEcho && state.Cycle % 5 == 0,
            });
        }

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
            Action<ExpandedWildernessVoiceOriginalOutcome>? observeOriginalHit)
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
                HitPulse(state, chosen, kind, now, center, direction, targets, applyHit, isElite, applyOriginalHit, observeOriginalHit);
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
            Func<ExpandedWildernessVoiceHit, double?>? applyOriginalHit, Action<ExpandedWildernessVoiceOriginalOutcome>? observeOriginalHit)
        {
            var primary = kind == ExpandedWildernessVoiceHitKind.Primary;
            if (primary && (cast.TracksEliteSeries || cast.CountsHitsForFinish || cast.StrengthensRepeatedTargets) &&
                (applyOriginalHit == null || cast.TracksEliteSeries && isElite == null))
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
                ExpandedWildernessVoiceHitKind.FirstEcho => .45,
                ExpandedWildernessVoiceHitKind.SecondEcho => .30,
                _ => .35,
            };
            // A full-circle primary still uses its facing for directional modifiers.
            var halfAngle = primary && !state.Evolved ? cast.HalfAngle : 180;
            var aim = cast.PulseFacing(direction, primary && pulseNumber == 1);
            var final = primary && cast.PrimaryAt(cast.PrimaryPulses) >= cast.End - TimeEpsilon;
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
                var elite = primary && (cast.TracksEliteSeries || observeOriginalHit != null) && isElite!(id);
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
                if (!killed)
                {
                    if (elite && cast.TracksEliteSeries) RecordEliteHit(state, series, id, now);
                    if (cast.StrengthensRepeatedTargets) RecordTargetHit(cast, previous, id);
                }
                observeOriginalHit?.Invoke(new ExpandedWildernessVoiceOriginalOutcome(hit, accepted.Value, elite, killed, final, now));
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
