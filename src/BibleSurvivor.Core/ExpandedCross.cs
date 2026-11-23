using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.Serialization;

namespace BibleSurvivor.Core
{
    public enum ExpandedCrossHitKind { Outbound, Return, Thorns, Afterglow, EliteThorn, EliteLight, ExtraPass, RecoveryWave, TurnSpin, IntersectionLight }

    public readonly struct ExpandedCrossRankOptions
    {
        public bool AccelerateOutbound { get; }
        public bool PrioritizeElite { get; }
        public bool AccelerateReturn { get; }
        public bool IncreaseLegBudget { get; }
        public bool WidenCollision { get; }
        public bool ExtendThorns { get; }
        public bool IncreaseSharedLegBudget { get; }
        public bool ShortenRehitInterval { get; }
        public bool WidenSupportShape { get; }
        public bool WidenSpread { get; }
        public bool FirstOutboundDamage { get; }
        public bool ReturnDamage { get; }
        public bool CurvedReturn { get; }
        public bool PreviouslyHitReturnDamage { get; }
        public bool MultipleHitDamage { get; }
        public bool ExpandFirstReturnBudget { get; }
        public bool LeaveAfterglow { get; }
        public bool ExtendAfterglow { get; }
        public bool BoostReturningAfterglow { get; }
        public bool EliteHitThorn { get; }
        public bool EliteKillLight { get; }
        public bool LaunchExtraPass { get; }
        public bool EmpowerThornOverlap { get; }
        public bool WidenThornRadius { get; }
        public bool WaveOnRecovery { get; }
        public bool WidenDamageRegion { get; }
        public bool SpinAtTurn { get; }
        public bool LightAtIntersection { get; }
        public double ResidualDurationBonus { get; }

        public ExpandedCrossRankOptions(bool accelerateOutbound = false, bool prioritizeElite = false,
            bool accelerateReturn = false, bool increaseLegBudget = false, bool widenCollision = false, bool extendThorns = false,
            bool increaseSharedLegBudget = false, bool shortenRehitInterval = false, bool widenSupportShape = false,
            bool widenSpread = false, bool firstOutboundDamage = false, bool returnDamage = false,
            bool curvedReturn = false, bool previouslyHitReturnDamage = false, bool multipleHitDamage = false,
            bool expandFirstReturnBudget = false, double residualDurationBonus = 0, bool leaveAfterglow = false,
            bool extendAfterglow = false, bool boostReturningAfterglow = false, bool eliteHitThorn = false,
            bool eliteKillLight = false, bool launchExtraPass = false, bool empowerThornOverlap = false,
            bool widenThornRadius = false, bool waveOnRecovery = false, bool widenDamageRegion = false, bool spinAtTurn = false, bool lightAtIntersection = false)
        {
            if (!ExpandedWeaponRules.ValidResidualDurationBonus(residualDurationBonus))
                throw new ArgumentOutOfRangeException(nameof(residualDurationBonus));
            AccelerateOutbound = accelerateOutbound;
            PrioritizeElite = prioritizeElite;
            AccelerateReturn = accelerateReturn;
            IncreaseLegBudget = increaseLegBudget;
            WidenCollision = widenCollision;
            ExtendThorns = extendThorns;
            IncreaseSharedLegBudget = increaseSharedLegBudget;
            ShortenRehitInterval = shortenRehitInterval;
            WidenSupportShape = widenSupportShape;
            WidenSpread = widenSpread;
            FirstOutboundDamage = firstOutboundDamage;
            ReturnDamage = returnDamage;
            CurvedReturn = curvedReturn;
            PreviouslyHitReturnDamage = previouslyHitReturnDamage;
            MultipleHitDamage = multipleHitDamage;
            ExpandFirstReturnBudget = expandFirstReturnBudget;
            LeaveAfterglow = leaveAfterglow;
            ExtendAfterglow = extendAfterglow;
            BoostReturningAfterglow = boostReturningAfterglow;
            EliteHitThorn = eliteHitThorn;
            EliteKillLight = eliteKillLight;
            LaunchExtraPass = launchExtraPass;
            EmpowerThornOverlap = empowerThornOverlap;
            WidenThornRadius = widenThornRadius;
            WaveOnRecovery = waveOnRecovery;
            WidenDamageRegion = widenDamageRegion;
            SpinAtTurn = spinAtTurn;
            LightAtIntersection = lightAtIntersection;
            ResidualDurationBonus = residualDurationBonus;
        }

        public static ExpandedCrossRankOptions FromRanks(IReadOnlyDictionary<string, int> ranks)
        {
            if (ranks == null) throw new ArgumentNullException(nameof(ranks));
            bool Has(string id) => ranks.TryGetValue(id, out var rank) && rank > 0;
            return new ExpandedCrossRankOptions(Has("weapon.cross.a.01"), Has("weapon.cross.a.02"),
                Has("weapon.cross.b.01"), Has("weapon.cross.c.01"), Has("weapon.cross.c.02"), Has("weapon.cross.e.01"),
                Has("common.shot.03"), Has("common.tempo.05"), Has("common.area.01"),
                Has("weapon.cross.a.03"), Has("weapon.cross.a.04"), Has("weapon.cross.b.02"), Has("weapon.cross.b.03"),
                Has("weapon.cross.b.04"), Has("weapon.cross.c.03"), Has("weapon.cross.c.04"),
                ExpandedWeaponRules.ResidualDurationBonus(ranks, true),
                Has("weapon.cross.d.01"), Has("weapon.cross.d.02"), Has("weapon.cross.d.03"),
                Has("weapon.cross.d.04"), Has("weapon.cross.d.05"), Has("weapon.cross.b.05"), Has("weapon.cross.e.02"),
                Has("weapon.cross.e.03"), Has("weapon.cross.e.04"), Has("common.area.09"), Has("weapon.cross.e.05"), Has("weapon.cross.c.05"));
        }
    }

    public readonly struct ExpandedCrossHit
    {
        public ExpandedCrossHitKind Kind { get; }
        public int TargetId { get; }
        public Vector2 Position { get; }
        public Vector2 Origin { get; }
        public Vector2 Direction { get; }
        public double Damage { get; }
        public double OriginalBaseDamage { get; }
        public int TargetOrdinal { get; }
        public long ShotId { get; }
        public long Cycle { get; }
        public bool IsOriginal => Kind == ExpandedCrossHitKind.Outbound || Kind == ExpandedCrossHitKind.Return;

        internal ExpandedCrossHit(ExpandedCrossHitKind kind, int targetId, Vector2 position, Vector2 origin,
            ExpandedCrossShotState shot, double? originalDamage = null)
        {
            Kind = kind;
            TargetId = targetId;
            Position = position;
            Origin = origin;
            Direction = shot.Direction;
            Damage = originalDamage ?? (kind == ExpandedCrossHitKind.Thorns
                ? shot.Damage * ExpandedCross.ThornDamageFactor : shot.Damage);
            OriginalBaseDamage = kind == ExpandedCrossHitKind.Outbound || kind == ExpandedCrossHitKind.Return
                ? shot.OriginalBaseDamage : 0;
            TargetOrdinal = kind switch
            {
                ExpandedCrossHitKind.Outbound => shot.OutboundHits + 1,
                ExpandedCrossHitKind.Return => shot.ReturnHits + 1,
                _ => 0,
            };
            ShotId = shot.Id;
            Cycle = shot.Cycle;
        }
    }

    [DataContract]
    public sealed class ExpandedCrossTargetState
    {
        [DataMember(IsRequired = true)] public int Id { get; set; }
        [DataMember(IsRequired = true)] public bool Outbound { get; set; }
        [DataMember(IsRequired = true)] public bool Returned { get; set; }
        [DataMember(IsRequired = true)] public double LastHitAt { get; set; }

        internal ExpandedCrossTargetState Copy() => (ExpandedCrossTargetState)MemberwiseClone();
    }

    [DataContract]
    public sealed class ExpandedCrossThornState
    {
        [DataMember(IsRequired = true)] public float StartX { get; set; }
        [DataMember(IsRequired = true)] public float StartY { get; set; }
        [DataMember(IsRequired = true)] public float EndX { get; set; }
        [DataMember(IsRequired = true)] public float EndY { get; set; }
        [DataMember(IsRequired = true)] public double BornAt { get; set; }
        [DataMember(IsRequired = true)] public bool Returning { get; set; }

        public Vector2 Start => new Vector2(StartX, StartY);
        public Vector2 End => new Vector2(EndX, EndY);
        internal ExpandedCrossThornState Copy() => (ExpandedCrossThornState)MemberwiseClone();
    }

    [DataContract]
    public sealed class ExpandedCrossShotState
    {
        [DataMember(IsRequired = true)] public long Id { get; set; }
        [DataMember(IsRequired = true)] public long Cycle { get; set; }
        [DataMember(IsRequired = true)] public double EmittedAt { get; set; }
        [DataMember(IsRequired = true)] public double SimulatedAt { get; set; }
        [DataMember(IsRequired = true)] public float StartX { get; set; }
        [DataMember(IsRequired = true)] public float StartY { get; set; }
        [DataMember(IsRequired = true)] public float X { get; set; }
        [DataMember(IsRequired = true)] public float Y { get; set; }
        [DataMember(IsRequired = true)] public float DirectionX { get; set; }
        [DataMember(IsRequired = true)] public float DirectionY { get; set; }
        [DataMember(IsRequired = true)] public double Damage { get; set; }
        [DataMember(EmitDefaultValue = false)] public double OriginalBaseDamage { get; set; }
        [DataMember(IsRequired = true)] public double Radius { get; set; }
        [DataMember(IsRequired = true)] public double Range { get; set; }
        [DataMember(IsRequired = true)] public double Speed { get; set; }
        [DataMember(IsRequired = true)] public double Distance { get; set; }
        [DataMember(EmitDefaultValue = false)] public double? ReturnStartedAt { get; set; }
        [DataMember(EmitDefaultValue = false)] public double? EndedAt { get; set; }
        [DataMember(IsRequired = true)] public int OutboundHits { get; set; }
        [DataMember(IsRequired = true)] public int ReturnHits { get; set; }
        [DataMember(IsRequired = true)] public bool LeavesThorns { get; set; }
        [DataMember(EmitDefaultValue = false)] public double? NextThornAt { get; set; }
        [DataMember(IsRequired = true)] public List<ExpandedCrossTargetState> HitTargets { get; set; } = new List<ExpandedCrossTargetState>();
        [DataMember(IsRequired = true)] public List<ExpandedCrossThornState> Thorns { get; set; } = new List<ExpandedCrossThornState>();
        [DataMember(EmitDefaultValue = false)] public bool LeavesAfterglow { get; set; }
        [DataMember(EmitDefaultValue = false)] public bool ExtendedAfterglow { get; set; }
        [DataMember(EmitDefaultValue = false)] public bool BoostReturningAfterglow { get; set; }
        [DataMember(EmitDefaultValue = false)] public double? NextAfterglowAt { get; set; }
        [DataMember(EmitDefaultValue = false)] public List<ExpandedCrossThornState>? Afterglow { get; set; }

        [DataMember(EmitDefaultValue = false)] public double? TrainedOutboundSpeed { get; set; }
        [DataMember(EmitDefaultValue = false)] public double? TrainedReturnSpeed { get; set; }
        [DataMember(EmitDefaultValue = false)] public bool IncreasedLegBudget { get; set; }
        [DataMember(EmitDefaultValue = false)] public bool IncreasedSharedLegBudget { get; set; }
        [DataMember(EmitDefaultValue = false)] public bool ExtendedThorns { get; set; }
        [DataMember(EmitDefaultValue = false)] public bool ShortenedRehitInterval { get; set; }
        [DataMember(EmitDefaultValue = false)] public bool WidenedSupportShape { get; set; }
        [DataMember(EmitDefaultValue = false)] public double ResidualDurationBonus { get; set; }

        [DataMember(EmitDefaultValue = false)] public bool FirstOutboundDamage { get; set; }
        [DataMember(EmitDefaultValue = false)] public bool ReturnDamage { get; set; }
        [DataMember(EmitDefaultValue = false)] public bool CurvedReturn { get; set; }
        [DataMember(EmitDefaultValue = false)] public bool PreviouslyHitReturnDamage { get; set; }
        [DataMember(EmitDefaultValue = false)] public bool MultipleHitDamage { get; set; }
        [DataMember(EmitDefaultValue = false)] public bool ExpandedFirstReturnBudget { get; set; }
        [DataMember(EmitDefaultValue = false)] public int? LaunchIndex { get; set; }
        [DataMember(EmitDefaultValue = false)] public int DamageLevel { get; set; }

        [DataMember(EmitDefaultValue = false)] public bool IsExtraPass { get; set; }
        [DataMember(EmitDefaultValue = false)] public bool EmpoweredThornOverlap { get; set; }
        [DataMember(EmitDefaultValue = false)] public bool WiderThornRadius { get; set; }
        [DataMember(EmitDefaultValue = false)] public bool WavesOnRecovery { get; set; }
        [DataMember(EmitDefaultValue = false)] public bool WiderDamageRegion { get; set; }
        [DataMember(EmitDefaultValue = false)] public bool SpinsAtTurn { get; set; }
        [DataMember(EmitDefaultValue = false)] public double? SpinEndsAt { get; set; }

        public double OutboundSpeed => TrainedOutboundSpeed ?? Speed;
        public double ReturnSpeed => TrainedReturnSpeed ?? Speed;
        public double ThornDuration => ExpandedCross.BaseThornDuration *
            (1 + (ExtendedThorns ? .20 : 0) + ResidualDurationBonus);
        public double ThornRadius => ExpandedCross.BaseThornRadius *
            (DamageAreaScale + (WiderThornRadius ? .15 : 0));
        public double AfterglowRadius => ExpandedCross.BaseAfterglowRadius * DamageAreaScale;
        internal double DamageAreaScale => ExpandedCross.AreaScale(WidenedSupportShape, WiderDamageRegion);
        public double AfterglowDuration => 1 + (ExtendedAfterglow ? .20 : 0) + ResidualDurationBonus;
        public int HitBudget(bool evolved) => Math.Min(8, (evolved ? 6 : ExpandedCross.Profile.HitsPerLeg) +
            (IncreasedLegBudget ? 1 : 0) + (IncreasedSharedLegBudget ? 1 : 0));
        public int ReturnBudget(bool evolved) => ExpandedFirstReturnBudget ? 8 : HitBudget(evolved);

        public Vector2 Start => new Vector2(StartX, StartY);
        public Vector2 Position => new Vector2(X, Y);
        public Vector2 Direction => new Vector2(DirectionX, DirectionY);
        public bool Returning => ReturnStartedAt.HasValue;
        public bool IsFlying => !EndedAt.HasValue;
        public bool HasThorns => LeavesThorns && (IsFlying || Thorns.Count > 0);
        public bool HasAfterglow => LeavesAfterglow && (IsFlying || Afterglow?.Count > 0);
        public bool HasTrail => HasThorns || HasAfterglow;
        public bool IsSpinning => SpinEndsAt.HasValue && !Returning && IsFlying;
        public double SpinRadius => ExpandedCross.TurnSpinRadius * DamageAreaScale;
        internal int ActiveTrailCount => (HasThorns ? 1 : 0) + (HasAfterglow ? 1 : 0) + (IsSpinning ? 1 : 0);

        internal ExpandedCrossShotState Copy()
        {
            var copy = (ExpandedCrossShotState)MemberwiseClone();
            copy.HitTargets = HitTargets.Select(target => target.Copy()).ToList();
            copy.Thorns = Thorns.Select(thorn => thorn.Copy()).ToList();
            copy.Afterglow = Afterglow?.Select(piece => piece.Copy()).ToList();
            return copy;
        }

        internal bool ValidDamageLevel(bool evolved)
        {
            if (!FirstOutboundDamage && !ReturnDamage && !PreviouslyHitReturnDamage && !MultipleHitDamage)
                return DamageLevel == 0;
            return DamageLevel >= 1 && DamageLevel <= 8 && (!evolved || DamageLevel == 8) &&
                Damage <= ExpandedCross.Profile.AtLevel(DamageLevel).Damage * 2.25 + ExpandedCross.Epsilon;
        }

        internal double OriginalHitDamage(bool returning, ExpandedCrossTargetState? target)
        {
            var bonus = returning
                ? (ReturnDamage ? .08 : 0) + (PreviouslyHitReturnDamage && target?.Outbound == true ? .07 : 0)
                : FirstOutboundDamage && OutboundHits == 0 ? .08 : 0;
            if (MultipleHitDamage && OutboundHits + ReturnHits >= 3) bonus += .08;
            if (bonus <= 0) return Damage;

            var basis = ExpandedCross.Profile.AtLevel(DamageLevel).Damage;
            return Math.Min(basis * 2.25, Damage + basis * bonus);
        }

        internal void MoveTo(Vector2 position) { X = position.X; Y = position.Y; }

        internal void Face(Vector2 direction) { DirectionX = direction.X; DirectionY = direction.Y; }
    }

    [DataContract]
    public sealed class ExpandedCrossTrajectoryState
    {
        [DataMember(IsRequired = true)] public long ShotId { get; set; }
        [DataMember(IsRequired = true)] public float StartX { get; set; }
        [DataMember(IsRequired = true)] public float StartY { get; set; }
        [DataMember(IsRequired = true)] public float EndX { get; set; }
        [DataMember(IsRequired = true)] public float EndY { get; set; }
        [DataMember(IsRequired = true)] public double FromAt { get; set; }
        [DataMember(IsRequired = true)] public double ToAt { get; set; }
        [DataMember(EmitDefaultValue = false)] public bool StartsAtLaunch { get; set; }

        public Vector2 Start => new Vector2(StartX, StartY);
        public Vector2 End => new Vector2(EndX, EndY);
        internal ExpandedCrossTrajectoryState Copy() => (ExpandedCrossTrajectoryState)MemberwiseClone();
    }

    [DataContract]
    public sealed class ExpandedCrossState
    {
        [DataMember(IsRequired = true)] public bool Evolved { get; set; }
        [DataMember(IsRequired = true)] public long Cycle { get; set; }
        [DataMember(IsRequired = true)] public long NextShotId { get; set; } = 1;
        [DataMember(IsRequired = true)] public double NextCycle { get; set; }
        [DataMember(IsRequired = true)] public List<ExpandedCrossShotState> Shots { get; set; } = new List<ExpandedCrossShotState>();

        public int ActiveShotCount => Shots.Count(shot => shot.IsFlying);
        public int ActiveOriginalShotCount => Shots.Count(shot => shot.IsFlying && !shot.IsExtraPass);
        public int ActiveSupportShotCount => Shots.Count(shot => shot.IsFlying && shot.IsExtraPass);
        [DataMember(EmitDefaultValue = false)] public long ExtraPassesEmitted { get; set; }
        [DataMember(EmitDefaultValue = false)] public double? NextExtraPassAt { get; set; }
        [DataMember(EmitDefaultValue = false)] public double? NextRecoveryWaveAt { get; set; }
        [DataMember(EmitDefaultValue = false)] public double? NextIntersectionAt { get; set; }
        [DataMember(EmitDefaultValue = false)] public double? NextTurnSpinAt { get; set; }
        [DataMember(EmitDefaultValue = false)] public List<ExpandedCrossTrajectoryState>? RecentTrajectories { get; set; }
        public int ActiveTrailCount => Shots.Sum(shot => shot.ActiveTrailCount);
        [DataMember(EmitDefaultValue = false)] public double? NextEliteThornAt { get; set; }
        [DataMember(EmitDefaultValue = false)] public double? NextEliteLightAt { get; set; }

        public ExpandedCrossState Copy() => new ExpandedCrossState
        {
            Evolved = Evolved,
            Cycle = Cycle,
            NextShotId = NextShotId,
            NextCycle = NextCycle,
            ExtraPassesEmitted = ExtraPassesEmitted,
            NextExtraPassAt = NextExtraPassAt,
            NextRecoveryWaveAt = NextRecoveryWaveAt,
            NextIntersectionAt = NextIntersectionAt,
            NextTurnSpinAt = NextTurnSpinAt,
            RecentTrajectories = RecentTrajectories?.Select(piece => piece.Copy()).ToList(),
            NextEliteThornAt = NextEliteThornAt,
            NextEliteLightAt = NextEliteLightAt,
            Shots = Shots.Select(shot => shot.Copy()).ToList(),
        };

        public void ResetForForm(bool evolved, double now)
        {
            if (!ExpandedCross.Finite(now) || now < 0) throw new ArgumentOutOfRangeException(nameof(now));
            Evolved = evolved;
            NextCycle = now;
            if (!evolved)
            {
                NextRecoveryWaveAt = null;
                NextTurnSpinAt = null;
            }
            Shots.Clear();
            RecentTrajectories = null;
        }

        public void Validate(double now, bool evolved)
        {
            if (!ExpandedCross.Finite(now) || now < 0 || Evolved != evolved || Cycle < 0 || NextShotId < 1 ||
                ExtraPassesEmitted < 0 || ExtraPassesEmitted > Cycle / 5 || ExtraPassesEmitted >= NextShotId ||
                (NextShotId - ExtraPassesEmitted > 1 &&
                    (NextShotId - ExtraPassesEmitted - 2) / ExpandedCross.MaximumAmount >= Cycle) ||
                NextExtraPassAt.HasValue && ExtraPassesEmitted == 0 ||
                !ExpandedCross.ValidCooldown(NextExtraPassAt, now, ExpandedCross.ExtraPassCooldown) ||
                !ExpandedCross.ValidCooldown(NextRecoveryWaveAt, now, ExpandedCross.RecoveryWaveCooldown) ||
                !ExpandedCross.ValidCooldown(NextIntersectionAt, now, ExpandedCross.IntersectionCooldown) ||
                !ExpandedCross.ValidCooldown(NextTurnSpinAt, now, ExpandedCross.TurnSpinCooldown) ||
                !evolved && (NextRecoveryWaveAt.HasValue || NextTurnSpinAt.HasValue) ||
                !ExpandedCross.Finite(NextCycle) || NextCycle < 0 ||
                NextCycle > now + ExpandedCross.MaximumCycle + ExpandedCross.Epsilon ||
                !ExpandedCross.ValidCooldown(NextEliteThornAt, now, ExpandedCross.EliteThornCooldown) ||
                !ExpandedCross.ValidCooldown(NextEliteLightAt, now, ExpandedCross.EliteLightCooldown) ||
                Shots == null || Shots.Count > ExpandedCross.MaximumShots + ExpandedCross.MaximumTrails + ExpandedCross.MaximumExtraPasses)
                throw new ArgumentException("Invalid expanded cross clock");

            long previousId = 0;
            var flying = 0;
            var trails = 0;
            var extraCycles = new HashSet<long>();
            var spinCycles = new HashSet<long>();
            var launchSlots = new HashSet<(long Cycle, int Index)>();
            foreach (var shot in Shots)
            {
                if (shot == null || shot.Id <= previousId || shot.Id >= NextShotId || shot.Cycle < 1 || shot.Cycle > Cycle ||
                    !ExpandedCross.ValidPosition(shot.Start) || !ExpandedCross.ValidPosition(shot.Position) ||
                    !ExpandedCross.UnitDirection(shot.Direction) ||
                    !ExpandedCross.Within(shot.EmittedAt, 0, now) ||
                    !ExpandedCross.Within(shot.SimulatedAt, shot.EmittedAt, now) ||
                    !ExpandedCross.Within(shot.Damage, 1, ExpandedCross.MaximumDamage) ||
                    !ExpandedCross.ValidGeometry(shot.Radius, shot.Range, shot.Speed) ||
                    !ExpandedCross.ValidTrainedSpeed(shot.Speed, shot.TrainedOutboundSpeed) ||
                    !ExpandedCross.ValidTrainedSpeed(shot.Speed, shot.TrainedReturnSpeed) ||
                    !ExpandedCross.Within(shot.Distance, 0, shot.Range) ||
                    shot.HitTargets == null || shot.Thorns == null || (shot.LeavesThorns && !evolved) ||
                    !shot.ValidDamageLevel(evolved) ||
                    !ExpandedCross.ValidOriginalBaseDamage(shot.OriginalBaseDamage, evolved) ||
                    shot.OriginalBaseDamage != 0 && shot.DamageLevel != 0 &&
                    shot.OriginalBaseDamage != ExpandedCross.Profile.AtLevel(shot.DamageLevel).Damage ||
                    (shot.LaunchIndex is int index && (index < 0 || index >= ExpandedCross.MaximumAmount || !launchSlots.Add((shot.Cycle, index)))) ||
                    shot.WavesOnRecovery && !evolved ||
                    shot.SpinsAtTurn && (!evolved || shot.Cycle % 4 != 0 || !spinCycles.Add(shot.Cycle) ||
                        shot.LaunchIndex is > 0) || !shot.SpinsAtTurn && shot.SpinEndsAt.HasValue ||
                    shot.IsExtraPass && (!shot.IsFlying || shot.Cycle % 5 != 0 || !extraCycles.Add(shot.Cycle) ||
                        shot.LeavesThorns || shot.LeavesAfterglow || shot.WavesOnRecovery || shot.SpinsAtTurn || shot.LaunchIndex.HasValue ||
                        shot.DamageLevel != 0 || shot.OriginalBaseDamage != 0) ||
                    shot.ExpandedFirstReturnBudget != (shot.LaunchIndex == 0 && shot.Cycle % 6 == 0))
                    throw new ArgumentException("Invalid expanded cross shot");

                var turnAt = shot.EmittedAt + shot.Range / shot.OutboundSpeed;
                if (shot.SpinEndsAt is double spinEnd && (!ExpandedCross.Within(spinEnd, turnAt + ExpandedCross.TurnSpinDuration,
                    turnAt + ExpandedCross.TurnSpinDuration) || shot.SimulatedAt < turnAt - ExpandedCross.Epsilon))
                    throw new ArgumentException("Invalid expanded cross spin chronology");
                if (shot.Returning)
                {
                    var start = shot.ReturnStartedAt!.Value;
                    if (!ExpandedCross.Within(start, shot.EmittedAt, shot.SimulatedAt) || shot.Distance != shot.Range ||
                        Math.Abs(start - (shot.SpinEndsAt ?? turnAt)) > ExpandedCross.Epsilon ||
                        (shot.IsFlying && shot.SimulatedAt >= start + ExpandedCross.Profile.ReturnTimeout))
                        throw new ArgumentException("Invalid expanded cross return");
                }
                else if (shot.IsSpinning)
                {
                    if (shot.Distance != shot.Range || shot.SimulatedAt >= shot.SpinEndsAt!.Value + ExpandedCross.Epsilon ||
                        Vector2.DistanceSquared(shot.Position, shot.Start + shot.Direction * (float)shot.Range) > 1e-6)
                        throw new ArgumentException("Invalid expanded cross spin position");
                }
                else if (!shot.IsFlying || shot.Distance >= shot.Range ||
                    Math.Abs(shot.Distance - (shot.SimulatedAt - shot.EmittedAt) * shot.OutboundSpeed) > ExpandedCross.Epsilon)
                    throw new ArgumentException("Invalid expanded cross outward flight");

                if (shot.IsFlying)
                {
                    if ((!shot.IsExtraPass && ++flying > ExpandedCross.MaximumShots) || shot.SimulatedAt < now - ExpandedCross.Epsilon)
                        throw new ArgumentException("Invalid expanded cross flight clock");
                }
                else if (!shot.HasTrail || !ExpandedCross.Within(shot.EndedAt!.Value, shot.ReturnStartedAt!.Value,
                    Math.Min(shot.SimulatedAt, shot.ReturnStartedAt.Value + ExpandedCross.Profile.ReturnTimeout)))
                    throw new ArgumentException("Invalid expanded cross expiry");

                ValidateHits(shot, shot.HitBudget(evolved), shot.ReturnBudget(evolved));
                ValidateTrail(shot, now);
                trails += shot.ActiveTrailCount;
                if (trails > ExpandedCross.MaximumTrails)
                    throw new ArgumentException("Invalid expanded cross trail count");
                previousId = shot.Id;
            }
            if (extraCycles.Count > ExpandedCross.MaximumExtraPasses || extraCycles.Count > ExtraPassesEmitted)
                throw new ArgumentException("Invalid expanded cross extra-pass count");
            ValidateTrajectories(now);
        }

        private void ValidateTrajectories(double now)
        {
            if (RecentTrajectories == null) return;
            if (RecentTrajectories.Count == 0 || RecentTrajectories.Count > ExpandedCross.MaximumIntersectionSegments)
                throw new ArgumentException("Invalid expanded cross trajectory count");
            var lastById = new Dictionary<long, (int Count, double EndedAt, Vector2 Position)>();
            var activeById = Shots.ToDictionary(shot => shot.Id);
            foreach (var piece in RecentTrajectories)
            {
                if (piece == null || piece.ShotId <= 0 || piece.ShotId >= NextShotId ||
                    !ExpandedCross.Within(piece.FromAt, 0, piece.ToAt) || piece.FromAt >= piece.ToAt ||
                    !ExpandedCross.Within(piece.ToAt, Math.Max(0, now - ExpandedCross.IntersectionWindow), now) ||
                    !ExpandedCross.ValidPosition(piece.Start) || !ExpandedCross.ValidPosition(piece.End) || piece.Start == piece.End)
                    throw new ArgumentException("Invalid expanded cross trajectory piece");
                if (activeById.TryGetValue(piece.ShotId, out var shot) && (shot.IsExtraPass ||
                    !ExpandedCross.Within(piece.FromAt, shot.EmittedAt, shot.EndedAt ?? shot.SimulatedAt) ||
                    !ExpandedCross.Within(piece.ToAt, shot.EmittedAt, shot.EndedAt ?? shot.SimulatedAt) ||
                    piece.StartsAtLaunch != (piece.FromAt == shot.EmittedAt) || piece.StartsAtLaunch && piece.Start != shot.Start))
                    throw new ArgumentException("Invalid expanded cross trajectory source");
                if (lastById.TryGetValue(piece.ShotId, out var previous) &&
                    (previous.Count >= ExpandedCross.MaximumIntersectionSegmentsPerShot || piece.Start != previous.Position ||
                        piece.StartsAtLaunch || piece.FromAt < previous.EndedAt - ExpandedCross.Epsilon))
                    throw new ArgumentException("Invalid expanded cross trajectory sequence");
                lastById[piece.ShotId] = (previous.Count + 1, piece.ToAt, piece.End);
            }
        }

        private static void ValidateHits(ExpandedCrossShotState shot, int outboundBudget, int returnBudget)
        {
            var seen = new HashSet<int>();
            var outward = 0;
            var returning = 0;
            if (shot.HitTargets.Count > outboundBudget + returnBudget) throw new ArgumentException("Invalid expanded cross hit count");
            foreach (var target in shot.HitTargets)
            {
                if (target == null || target.Id < 1 || !seen.Add(target.Id) || (!target.Outbound && !target.Returned) ||
                    (target.Returned && !shot.Returning) ||
                    !ExpandedCross.Within(target.LastHitAt, target.Returned ? shot.ReturnStartedAt!.Value : shot.EmittedAt,
                        target.Returned ? shot.EndedAt ?? shot.SimulatedAt : shot.ReturnStartedAt ?? shot.SimulatedAt))
                    throw new ArgumentException("Invalid expanded cross hit record");
                if (target.Outbound) outward++;
                if (target.Returned) returning++;
            }
            if (outward != shot.OutboundHits || returning != shot.ReturnHits || outward > outboundBudget || returning > returnBudget)
                throw new ArgumentException("Invalid expanded cross hit budgets");
        }

        private static void ValidateTrail(ExpandedCrossShotState shot, double now)
        {
            if (!ExpandedWeaponRules.ValidResidualDurationBonus(shot.ResidualDurationBonus) ||
                !shot.LeavesThorns && (shot.ExtendedThorns || shot.EmpoweredThornOverlap || shot.WiderThornRadius) ||
                !shot.LeavesAfterglow && (shot.ExtendedAfterglow || shot.BoostReturningAfterglow) ||
                !shot.LeavesThorns && !shot.LeavesAfterglow && shot.ResidualDurationBonus != 0 ||
                !shot.LeavesThorns && !shot.LeavesAfterglow && !shot.WavesOnRecovery && !shot.SpinsAtTurn &&
                    (shot.WidenedSupportShape || shot.WiderDamageRegion) ||
                shot.LeavesAfterglow != (shot.Afterglow != null))
                throw new ArgumentException("Invalid expanded cross trail");
            ValidateTrailPieces(shot, now, shot.LeavesThorns, shot.Thorns, shot.NextThornAt, shot.ThornDuration);
            ValidateTrailPieces(shot, now, shot.LeavesAfterglow, shot.Afterglow, shot.NextAfterglowAt, shot.AfterglowDuration);
        }

        private static void ValidateTrailPieces(ExpandedCrossShotState shot, double now, bool enabled,
            List<ExpandedCrossThornState>? pieces, double? next, double duration)
        {
            if (!enabled && (next.HasValue || pieces?.Count > 0) ||
                pieces?.Count > ExpandedCross.ThornPieceLimit(duration) ||
                pieces?.Count > 0 && !next.HasValue ||
                next.HasValue && !ExpandedCross.Within(next.Value, now, now + ExpandedCross.ThornInterval))
                throw new ArgumentException("Invalid expanded cross trail schedule");
            if (pieces == null) return;
            double last = double.NegativeInfinity;
            foreach (var piece in pieces)
            {
                if (piece == null || piece.BornAt < last ||
                    !ExpandedCross.Within(piece.BornAt, shot.EmittedAt, shot.EndedAt ?? shot.SimulatedAt) ||
                    piece.BornAt + duration <= now + ExpandedCross.Epsilon ||
                    !ExpandedCross.ValidPosition(piece.Start) || !ExpandedCross.ValidPosition(piece.End) ||
                    (piece.Returning && (!shot.Returning || piece.BornAt < shot.ReturnStartedAt!.Value)) ||
                    (!piece.Returning && piece.BornAt > shot.ReturnStartedAt))
                    throw new ArgumentException("Invalid expanded cross trail piece");
                last = piece.BornAt;
            }
        }
    }

    public static class ExpandedCross
    {
        internal static readonly ExpandedWeaponProfile Profile = ExpandedWeaponRules.Get("cross");
        internal static readonly int MaximumAmount = Profile.MaximumOriginalAmount;
        internal static readonly double MaximumCycle = Profile.AtLevel(1).Cycle;
        internal static readonly double MaximumDamage = Profile.AtLevel(8).Damage * 2.25;
        private static readonly double[] OriginalDamageByLevel = Enumerable.Range(1, 8)
            .Select(level => (double)Profile.AtLevel(level).Damage).ToArray();
        private static readonly double MinimumSpeed = Profile.AtLevel(1).Speed;
        private static readonly double MaximumSpeed = Profile.AtLevel(8).Speed * 1.75;
        private static readonly double MaximumRange = Profile.TravelDistance * 2.5;
        private static readonly double MaximumRadius = Profile.AtLevel(1).Radius * 2.5;
        private const double MinimumCycle = .15;
        private const double ReturnCurveDuration = .40;
        private const double ReturnCurveDegrees = 20;
        internal static readonly int MaximumShots =
            MaximumAmount * ((int)Math.Ceiling((MaximumRange / MinimumSpeed + Profile.ReturnTimeout) / MinimumCycle) + 1) + 1;
        internal const double IntersectionWindow = .25;
        internal const double IntersectionCooldown = 3;
        internal const int MaximumIntersectionSegmentsPerShot = 18;
        // A just-ended original remains in the window even after its flying slot is released.
        internal static readonly int MaximumIntersectionSegments =
            (MaximumShots + MaximumAmount * ((int)Math.Ceiling(IntersectionWindow / MinimumCycle) + 1)) *
            MaximumIntersectionSegmentsPerShot;
        private const double IntersectionRadius = .8;
        private const double IntersectionFactor = .30;
        internal const int MaximumTrails = 16;
        internal const int MaximumExtraPasses = 4;
        internal const double ExtraPassCooldown = 1;
        internal const double RecoveryWaveCooldown = 3;
        private const double RecoveryWaveRadius = 1.5;
        private const double RecoveryWaveFactor = .35;
        internal const double TurnSpinRadius = .8;
        internal const double TurnSpinDuration = .5;
        internal const double TurnSpinCooldown = 1;
        private const double TurnSpinFactor = .35;
        private const double ExtraPassFactor = .35;
        private const double ThornOverlapFactor = .12;
        internal const double BaseThornDuration = 1.5;
        internal const double ThornInterval = .5;
        internal const double ThornDamageFactor = .35;
        internal const double BaseThornRadius = .75 / 2;
        internal const double BaseAfterglowRadius = .60 / 2;
        internal const double EliteThornCooldown = 2;
        internal const double EliteLightCooldown = 4;
        private const double BaseAfterglowDamageFactor = .25;
        private const double ReturningAfterglowBonus = .10;
        private const double EliteThornRadius = .6;
        private const double EliteThornDamageFactor = .30;
        private const double EliteLightRadius = 3;
        private const double EliteLightDamageFactor = .25;
        internal const double Epsilon = 1e-8;

        public static int Step(ExpandedCrossState state, double now, Vector2 playerPosition, Vector2 facing, int level,
            bool evolved, double damage, double cycle, int amount, double radius, double range, double speed,
            Func<IReadOnlyDictionary<int, Vector2>> targets, IReadOnlyDictionary<int, Vector2> previousTargets,
            Func<ExpandedCrossHit, bool> applyHit, Func<bool>? reserveOriginal = null, Func<bool>? reserveTrail = null,
            ExpandedCrossRankOptions rankOptions = default, Func<int, bool>? isElite = null,
            Func<double>? nextCycleDuration = null, Action<int>? recordOriginalSuppression = null,
            Action? recordSupportSuppression = null, Func<bool>? reserveSupportShot = null,
            Action<Vector2, double>? emitRecoveryWave = null, Func<ExpandedCrossHit, double?>? applyOriginalHit = null,
            Action<Vector2, double>? emitTurnSpin = null, Action<Vector2, double>? emitIntersectionLight = null)
        {
            if (state == null) throw new ArgumentNullException(nameof(state));
            if (targets == null) throw new ArgumentNullException(nameof(targets));
            if (previousTargets == null) throw new ArgumentNullException(nameof(previousTargets));
            if (applyHit == null) throw new ArgumentNullException(nameof(applyHit));
            if ((rankOptions.PrioritizeElite || rankOptions.EliteHitThorn || rankOptions.EliteKillLight) && isElite == null) throw new ArgumentNullException(nameof(isElite));
            if (!Finite(now) || now < 0 || !ValidPosition(playerPosition) || !ValidPosition(facing) ||
                level < 1 || level > 8 || (evolved && level != 8) || !Within(damage, 1, MaximumDamage) ||
                !Within(cycle, MinimumCycle, MaximumCycle) || amount < 1 || amount > MaximumAmount ||
                !ValidGeometry(radius, range, speed))
                throw new ArgumentException("Invalid expanded cross parameters");

            var damageLevel = rankOptions.FirstOutboundDamage || rankOptions.ReturnDamage ||
                rankOptions.PreviouslyHitReturnDamage || rankOptions.MultipleHitDamage ? level : 0;
            if (damageLevel != 0 && damage > Profile.AtLevel(damageLevel).Damage * 2.25 + Epsilon)
                throw new ArgumentOutOfRangeException(nameof(damage));

            if (state.Evolved != evolved) state.ResetForForm(evolved, now);
            if (state.NextEliteThornAt <= now + Epsilon) state.NextEliteThornAt = null;
            if (state.NextEliteLightAt <= now + Epsilon) state.NextEliteLightAt = null;
            if (state.NextExtraPassAt <= now + Epsilon) state.NextExtraPassAt = null;
            if (state.NextRecoveryWaveAt <= now + Epsilon) state.NextRecoveryWaveAt = null;
            if (state.NextIntersectionAt <= now + Epsilon) state.NextIntersectionAt = null;
            if (state.NextTurnSpinAt <= now + Epsilon) state.NextTurnSpinAt = null;
            var consideredIntersection = false;
            Action<ExpandedCrossShotState, Vector2, Vector2, double, double>? noteMovement = rankOptions.LightAtIntersection
                ? NoteIntersection : (Action<ExpandedCrossShotState, Vector2, Vector2, double, double>?)null;
            foreach (var shot in state.Shots)
            {
                shot.Thorns.RemoveAll(piece => piece.BornAt + shot.ThornDuration <= now + Epsilon);
                shot.Afterglow?.RemoveAll(piece => piece.BornAt + shot.AfterglowDuration <= now + Epsilon);
                if (!shot.IsFlying) continue;
                Func<ExpandedCrossHit, bool> original = applyHit;
                if (!shot.IsExtraPass && (applyOriginalHit != null || rankOptions.EliteHitThorn || rankOptions.EliteKillLight))
                    original = attack => ApplyOriginalWithEliteBursts(state, shot, now, attack, rankOptions, isElite,
                        targets, applyHit, reserveTrail, recordSupportSuppression, applyOriginalHit);
                Func<bool>? beginSpin = shot.SpinsAtTurn && !shot.Returning && !shot.SpinEndsAt.HasValue &&
                    !state.NextTurnSpinAt.HasValue ? () => BeginTurnSpin(shot) : (Func<bool>?)null;
                var caught = Move(shot, now, playerPosition, shot.HitBudget(evolved), shot.ReturnBudget(evolved),
                    targets, previousTargets, original, beginSpin, shot.IsExtraPass ? null : noteMovement);
                if (caught && shot.WavesOnRecovery && !state.NextRecoveryWaveAt.HasValue &&
                    EmitBurst(state, shot, ExpandedCrossHitKind.RecoveryWave, shot.Position,
                        RecoveryWaveRadius * shot.DamageAreaScale, shot.Damage * RecoveryWaveFactor,
                        targets, applyHit, reserveTrail, recordSupportSuppression, emit: emitRecoveryWave))
                    state.NextRecoveryWaveAt = now + RecoveryWaveCooldown;
                shot.Thorns.RemoveAll(piece => piece.BornAt + shot.ThornDuration <= now + Epsilon);
                shot.Afterglow?.RemoveAll(piece => piece.BornAt + shot.AfterglowDuration <= now + Epsilon);
            }
            ResolveTrails(state.Shots, now, targets, previousTargets, applyHit);
            state.Shots.RemoveAll(shot => !shot.IsFlying && !shot.HasTrail);
            state.RecentTrajectories?.RemoveAll(piece => piece.ToAt < now - IntersectionWindow - Epsilon);
            if (state.RecentTrajectories?.Count == 0) state.RecentTrajectories = null;
            if (state.NextCycle > now + Epsilon) return 0;

            state.Cycle = checked(state.Cycle + 1);
            state.NextCycle += nextCycleDuration?.Invoke() ?? cycle;
            var aimFrom = AimOffset(playerPosition, targets(), rankOptions.PrioritizeElite ? isElite : null);
            var aim = DirectionOr(aimFrom, DirectionOr(facing, Vector2.UnitX));
            if (rankOptions.WidenCollision)
                radius = Math.Min(MaximumRadius, radius + Profile.AtLevel(1).Radius * .08);
            var trainedSpeed = rankOptions.AccelerateOutbound || rankOptions.AccelerateReturn
                ? ResolveTrainedSpeed(speed, Profile.AtLevel(level).Speed)
                : null;
            var spread = Profile.SpreadDegrees + (rankOptions.WidenSpread ? 6 : 0);
            var active = state.ActiveOriginalShotCount;
            var trails = state.ActiveTrailCount;
            var emitted = 0;
            for (var index = 0; index < amount; index++)
            {
                if (active + emitted >= MaximumShots || reserveOriginal?.Invoke() == false)
                {
                    recordOriginalSuppression?.Invoke(amount - index);
                    break;
                }
                var angle = (index - (amount - 1) * .5) * spread;
                var direction = Rotate(aim, angle);
                var leavesThorns = evolved && trails < MaximumTrails && (reserveTrail?.Invoke() ?? true);
                if (leavesThorns) trails++;
                else if (evolved) recordSupportSuppression?.Invoke();
                var spinsAtTurn = evolved && rankOptions.SpinAtTurn && index == 0 && state.Cycle % 4 == 0;
                var wavesOnRecovery = evolved && rankOptions.WaveOnRecovery;
                var hasDamageArea = leavesThorns || wavesOnRecovery || spinsAtTurn;
                var shot = new ExpandedCrossShotState
                {
                    Id = state.NextShotId, Cycle = state.Cycle, EmittedAt = now, SimulatedAt = now,
                    StartX = playerPosition.X, StartY = playerPosition.Y, X = playerPosition.X, Y = playerPosition.Y,
                    DirectionX = direction.X, DirectionY = direction.Y, Damage = damage, Radius = radius,
                    OriginalBaseDamage = Profile.AtLevel(level).Damage,
                    Range = range, Speed = speed, LeavesThorns = leavesThorns,
                    TrainedOutboundSpeed = rankOptions.AccelerateOutbound ? trainedSpeed : null,
                    TrainedReturnSpeed = rankOptions.AccelerateReturn ? trainedSpeed : null,
                    IncreasedLegBudget = rankOptions.IncreaseLegBudget,
                    IncreasedSharedLegBudget = rankOptions.IncreaseSharedLegBudget,
                    ExtendedThorns = leavesThorns && rankOptions.ExtendThorns,
                    EmpoweredThornOverlap = leavesThorns && rankOptions.EmpowerThornOverlap,
                    WiderThornRadius = leavesThorns && rankOptions.WidenThornRadius,
                    WavesOnRecovery = wavesOnRecovery,
                    SpinsAtTurn = spinsAtTurn,
                    ShortenedRehitInterval = rankOptions.ShortenRehitInterval,
                    WidenedSupportShape = hasDamageArea && rankOptions.WidenSupportShape,
                    WiderDamageRegion = hasDamageArea && rankOptions.WidenDamageRegion,
                    ResidualDurationBonus = leavesThorns ? rankOptions.ResidualDurationBonus : 0,
                    FirstOutboundDamage = rankOptions.FirstOutboundDamage,
                    ReturnDamage = rankOptions.ReturnDamage,
                    CurvedReturn = rankOptions.CurvedReturn,
                    PreviouslyHitReturnDamage = rankOptions.PreviouslyHitReturnDamage,
                    MultipleHitDamage = rankOptions.MultipleHitDamage,
                    ExpandedFirstReturnBudget = rankOptions.ExpandFirstReturnBudget && index == 0 && state.Cycle % 6 == 0,
                    LaunchIndex = rankOptions.ExpandFirstReturnBudget ? index : (int?)null,
                    DamageLevel = damageLevel,
                };
                state.Shots.Add(shot);
                if (rankOptions.LeaveAfterglow)
                {
                    if (trails < MaximumTrails && (reserveTrail?.Invoke() ?? true))
                    {
                        trails++;
                        shot.LeavesAfterglow = true;
                        shot.ExtendedAfterglow = rankOptions.ExtendAfterglow;
                        shot.BoostReturningAfterglow = rankOptions.BoostReturningAfterglow;
                        shot.Afterglow = new List<ExpandedCrossThornState>();
                        shot.WidenedSupportShape = rankOptions.WidenSupportShape;
                        shot.WiderDamageRegion = rankOptions.WidenDamageRegion;
                        shot.ResidualDurationBonus = rankOptions.ResidualDurationBonus;
                    }
                    else recordSupportSuppression?.Invoke();
                }
                state.NextShotId = checked(state.NextShotId + 1);
                emitted++;
            }
            if (emitted > 0 && rankOptions.LaunchExtraPass && state.Cycle % 5 == 0 && !state.NextExtraPassAt.HasValue)
            {
                if (state.ActiveSupportShotCount >= MaximumExtraPasses || reserveSupportShot?.Invoke() == false)
                    recordSupportSuppression?.Invoke();
                else
                {
                    state.Shots.Add(new ExpandedCrossShotState
                    {
                        Id = state.NextShotId, Cycle = state.Cycle, EmittedAt = now, SimulatedAt = now,
                        StartX = playerPosition.X, StartY = playerPosition.Y, X = playerPosition.X, Y = playerPosition.Y,
                        DirectionX = aim.X, DirectionY = aim.Y, Damage = damage, Radius = radius, Range = range, Speed = speed,
                        TrainedOutboundSpeed = rankOptions.AccelerateOutbound ? trainedSpeed : null,
                        TrainedReturnSpeed = rankOptions.AccelerateReturn ? trainedSpeed : null,
                        CurvedReturn = rankOptions.CurvedReturn,
                        IsExtraPass = true,
                    });
                    state.NextShotId = checked(state.NextShotId + 1);
                    state.ExtraPassesEmitted = checked(state.ExtraPassesEmitted + 1);
                    state.NextExtraPassAt = now + ExtraPassCooldown;
                }
            }
            return emitted;

            bool BeginTurnSpin(ExpandedCrossShotState shot)
            {
                if (!EmitBurst(state, shot, ExpandedCrossHitKind.TurnSpin, shot.Position, shot.SpinRadius,
                    shot.Damage * TurnSpinFactor, targets, applyHit, reserveTrail, recordSupportSuppression, emit: emitTurnSpin))
                    return false;
                state.NextTurnSpinAt = now + TurnSpinCooldown;
                return true;
            }

            void NoteIntersection(ExpandedCrossShotState shot, Vector2 from, Vector2 to, double fromAt, double toAt)
            {
                if (toAt < now - IntersectionWindow - Epsilon) return;
                var piece = new ExpandedCrossTrajectoryState
                {
                    ShotId = shot.Id, StartX = from.X, StartY = from.Y, EndX = to.X, EndY = to.Y,
                    FromAt = fromAt, ToAt = toAt, StartsAtLaunch = fromAt == shot.EmittedAt,
                };
                state.RecentTrajectories ??= new List<ExpandedCrossTrajectoryState>();
                if (!consideredIntersection && !state.NextIntersectionAt.HasValue)
                {
                    var crossing = FirstIntersection(piece, state.RecentTrajectories);
                    if (crossing.HasValue)
                    {
                        consideredIntersection = true;
                        if (EmitBurst(state, shot, ExpandedCrossHitKind.IntersectionLight, crossing.Value,
                            IntersectionRadius * AreaScale(rankOptions.WidenSupportShape, rankOptions.WidenDamageRegion),
                            shot.Damage * IntersectionFactor, targets, applyHit, reserveTrail, recordSupportSuppression,
                            emit: emitIntersectionLight))
                            state.NextIntersectionAt = now + IntersectionCooldown;
                    }
                }
                state.RecentTrajectories.Add(piece);
            }
        }

        private static Vector2? FirstIntersection(ExpandedCrossTrajectoryState piece,
            IReadOnlyList<ExpandedCrossTrajectoryState> history)
        {
            var first = double.PositiveInfinity;
            var start = piece.Start;
            var end = piece.End;
            var direction = end - start;
            foreach (var other in history)
            {
                if (other.ShotId == piece.ShotId ||
                    piece.FromAt > other.ToAt + IntersectionWindow + Epsilon ||
                    other.FromAt > piece.ToAt + IntersectionWindow + Epsilon ||
                    !Overlap(start.X, end.X, other.StartX, other.EndX) ||
                    !Overlap(start.Y, end.Y, other.StartY, other.EndY))
                    continue;

                var opposite = other.End - other.Start;
                var angle = Cross(direction, opposite);
                // Collinear strips have no single crossing point.
                if (Math.Abs(angle) <= 1e-12) continue;
                var separation = other.Start - start;
                var part = Cross(separation, opposite) / angle;
                var otherPart = Cross(separation, direction) / angle;
                if (!Within(part, 0, 1) || !Within(otherPart, 0, 1) || part > first ||
                    (piece.StartsAtLaunch && other.StartsAtLaunch && part <= Epsilon && otherPart <= Epsilon))
                    continue;

                var crossingAt = piece.FromAt + (piece.ToAt - piece.FromAt) * part;
                var otherAt = other.FromAt + (other.ToAt - other.FromAt) * otherPart;
                if (Math.Abs(crossingAt - otherAt) <= IntersectionWindow + Epsilon)
                    first = part;
            }
            return double.IsPositiveInfinity(first) ? (Vector2?)null : Vector2.Lerp(start, end, (float)Math.Max(0, Math.Min(1, first)));

            static double Cross(Vector2 a, Vector2 b) => (double)a.X * b.Y - (double)a.Y * b.X;

            static bool Overlap(float a, float b, float c, float d) =>
                Math.Min(a, b) <= Math.Max(c, d) + Epsilon && Math.Min(c, d) <= Math.Max(a, b) + Epsilon;
        }

        private static Vector2 AimOffset(Vector2 origin, IReadOnlyDictionary<int, Vector2> targets, Func<int, bool>? isElite)
        {
            var best = Vector2.Zero;
            var bestDistance = float.PositiveInfinity;
            var bestId = int.MaxValue;
            var bestIsElite = false;
            foreach (var (id, position) in targets)
            {
                var distance = Vector2.DistanceSquared(origin, position);
                var elite = isElite != null && distance <= 25 && isElite(id);
                if ((!elite && bestIsElite) || (elite == bestIsElite &&
                    (distance > bestDistance || (distance == bestDistance && id >= bestId))))
                    continue;
                best = position - origin;
                bestDistance = distance;
                bestId = id;
                bestIsElite = elite;
            }
            return best;
        }

        private static bool Move(ExpandedCrossShotState shot, double now, Vector2 player, int outboundBudget, int returnBudget,
            Func<IReadOnlyDictionary<int, Vector2>> targets, IReadOnlyDictionary<int, Vector2> previous,
            Func<ExpandedCrossHit, bool> applyHit, Func<bool>? beginSpin,
            Action<ExpandedCrossShotState, Vector2, Vector2, double, double>? noteMovement)
        {
            var start = shot.SimulatedAt;
            if (now <= start + Epsilon) return false;
            var segmentStart = Math.Max(start, shot.ReturnStartedAt ?? start);
            shot.SimulatedAt = now;
            if (!shot.Returning)
            {
                var turnAt = shot.EmittedAt + shot.Range / shot.OutboundSpeed;
                if (shot.Distance < shot.Range)
                {
                    var reachedTurn = now + Epsilon >= turnAt;
                    var until = Math.Min(now, turnAt);
                    shot.Distance = reachedTurn ? shot.Range : (until - shot.EmittedAt) * shot.OutboundSpeed;
                    var destination = shot.Start + shot.Direction * (float)shot.Distance;
                    Advance(shot, destination, start, until, start, now, outboundBudget, targets, previous, applyHit, noteMovement);
                    if (!reachedTurn) return false;
                    if (beginSpin?.Invoke() == true) shot.SpinEndsAt = turnAt + TurnSpinDuration;
                }
                var departAt = shot.SpinEndsAt ?? turnAt;
                if (now + Epsilon < departAt) return false;
                shot.ReturnStartedAt = departAt;
                segmentStart = departAt;
            }

            var expiry = shot.ReturnStartedAt!.Value + Profile.ReturnTimeout;
            var curveEnd = shot.ReturnStartedAt.Value + ReturnCurveDuration;
            if (shot.CurvedReturn && segmentStart < curveEnd - Epsilon)
            {
                var curvedUntil = Math.Min(Math.Min(now, curveEnd), expiry);
                AdvanceCurvedReturn(shot, player, segmentStart, curvedUntil, start, now, returnBudget, targets, previous, applyHit, noteMovement);
                if (shot.EndedAt.HasValue) return true;
                if (curvedUntil >= now - Epsilon && curvedUntil < expiry) return false;
                segmentStart = curvedUntil;
            }

            var returning = player - shot.Position;
            var length = returning.Length();
            var required = Math.Max(0, length - Profile.ReturnRadius);
            var catchAt = segmentStart + required / shot.ReturnSpeed;
            var end = Math.Min(Math.Min(expiry, catchAt), now);
            if (required > 0 && end > segmentStart + Epsilon)
            {
                shot.Face(Vector2.Normalize(returning));
                var destination = shot.Position + shot.Direction * (float)((end - segmentStart) * shot.ReturnSpeed);
                Advance(shot, destination, segmentStart, end, start, now, returnBudget, targets, previous, applyHit, noteMovement);
            }
            if (end < Math.Min(expiry, catchAt)) return false;
            shot.EndedAt = end;
            return catchAt <= expiry;
        }

        private static void AdvanceCurvedReturn(ExpandedCrossShotState shot, Vector2 player,
            double fromAt, double toAt, double tickStart, double tickEnd, int budget,
            Func<IReadOnlyDictionary<int, Vector2>> targets, IReadOnlyDictionary<int, Vector2> previous,
            Func<ExpandedCrossHit, bool> applyHit, Action<ExpandedCrossShotState, Vector2, Vector2, double, double>? noteMovement)
        {
            var home = player - shot.Position;
            var returnRadiusSquared = Profile.ReturnRadius * Profile.ReturnRadius;
            if (home.LengthSquared() <= returnRadiusSquared)
            {
                shot.EndedAt = fromAt;
                return;
            }

            // Use one counterclockwise sign for all shots. Re-evaluate the live
            // homing tangent on each fixed step as the offset returns to zero.
            while (fromAt < toAt - Epsilon)
            {
                var end = Math.Min(toAt, fromAt + BattleWorld.FixedStep);
                var degrees = ReturnCurveDegrees * Math.Max(0, 1 - (fromAt - shot.ReturnStartedAt!.Value) / ReturnCurveDuration);
                shot.Face(Rotate(Vector2.Normalize(home), degrees));
                var destination = shot.Position + shot.Direction * (float)((end - fromAt) * shot.ReturnSpeed);
                var capture = CircleContact(-home, destination - player, Profile.ReturnRadius);
                if (capture.HasValue)
                {
                    destination = Vector2.Lerp(shot.Position, destination, (float)capture.Value);
                    end = fromAt + (end - fromAt) * capture.Value;
                }
                Advance(shot, destination, fromAt, end, tickStart, tickEnd, budget, targets, previous, applyHit, noteMovement);
                if (capture.HasValue)
                {
                    shot.EndedAt = end;
                    return;
                }
                fromAt = end;
                home = player - shot.Position;
            }
        }

        private static Vector2 Rotate(Vector2 direction, double degrees)
        {
            var radians = degrees * Math.PI / 180;
            var cosine = Math.Cos(radians);
            var sine = Math.Sin(radians);
            return new Vector2((float)(direction.X * cosine - direction.Y * sine),
                (float)(direction.X * sine + direction.Y * cosine));
        }

        private static void Advance(ExpandedCrossShotState shot, Vector2 end, double fromAt, double toAt, double tickStart,
            double tickEnd, int budget, Func<IReadOnlyDictionary<int, Vector2>> targets,
            IReadOnlyDictionary<int, Vector2> previous, Func<ExpandedCrossHit, bool> applyHit,
            Action<ExpandedCrossShotState, Vector2, Vector2, double, double>? noteMovement)
        {
            var start = shot.Position;
            shot.MoveTo(end);
            var duration = toAt - fromAt;
            if (start == end || duration <= Epsilon) return;
            var returning = shot.Returning;
            var kind = shot.IsExtraPass ? ExpandedCrossHitKind.ExtraPass :
                returning ? ExpandedCrossHitKind.Return : ExpandedCrossHitKind.Outbound;
            foreach (var id in targets().Keys.OrderBy(id => id).ToArray())
            {
                if ((returning ? shot.ReturnHits : shot.OutboundHits) >= budget) break;
                var record = shot.HitTargets.Find(item => item.Id == id);
                if (record != null && (returning ? record.Returned : record.Outbound)) continue;
                var interval = Profile.MinimumRehitInterval * (shot.ShortenedRehitInterval ? .96 : 1);
                var firstAllowed = record == null ? fromAt : Math.Max(fromAt, record.LastHitAt + interval);
                if (firstAllowed > toAt + Epsilon || !targets().TryGetValue(id, out var current)) continue;
                var old = previous.TryGetValue(id, out var oldPosition) ? oldPosition : current;
                var fraction = (float)Math.Min(1, (firstAllowed - fromAt) / duration);
                var partial = Vector2.Lerp(start, end, fraction);
                var atFirst = Vector2.Lerp(old, current, (float)((firstAllowed - tickStart) / (tickEnd - tickStart)));
                var atEnd = Vector2.Lerp(old, current, (float)((toAt - tickStart) / (tickEnd - tickStart)));
                var contact = CircleContact(partial - atFirst, end - atEnd, shot.Radius);
                if (!contact.HasValue ||
                    !applyHit(new ExpandedCrossHit(kind, id, current, Vector2.Lerp(partial, end, (float)contact.Value), shot,
                        shot.IsExtraPass ? shot.Damage * ExtraPassFactor : shot.OriginalHitDamage(returning, record))))
                    continue;

                if (record == null)
                {
                    record = new ExpandedCrossTargetState { Id = id };
                    shot.HitTargets.Add(record);
                }
                record.LastHitAt = firstAllowed + (toAt - firstAllowed) * contact.Value;
                if (returning) { record.Returned = true; shot.ReturnHits++; }
                else { record.Outbound = true; shot.OutboundHits++; }
            }
            noteMovement?.Invoke(shot, start, end, fromAt, toAt);
            if (!shot.LeavesThorns && !shot.LeavesAfterglow) return;
            var piece = new ExpandedCrossThornState
            {
                StartX = start.X, StartY = start.Y, EndX = end.X, EndY = end.Y, BornAt = toAt, Returning = returning,
            };
            if (shot.LeavesThorns)
            {
                shot.NextThornAt ??= tickEnd;
                shot.Thorns.Add(piece);
            }
            if (shot.LeavesAfterglow)
            {
                shot.NextAfterglowAt ??= tickEnd;
                shot.Afterglow!.Add(shot.LeavesThorns ? piece.Copy() : piece);
            }
        }

        private static void ResolveTrails(IReadOnlyList<ExpandedCrossShotState> shots, double now,
            Func<IReadOnlyDictionary<int, Vector2>> targets, IReadOnlyDictionary<int, Vector2> previous,
            Func<ExpandedCrossHit, bool> applyHit)
        {
            List<(ExpandedCrossShotState Shot, Vector2 Origin, int Id, double Damage, ExpandedCrossHitKind Kind)>? hits = null;
            Collect(false, ExpandedCrossHitKind.Thorns);
            Collect(true, ExpandedCrossHitKind.Afterglow);
            if (hits == null) return;
            foreach (var hit in hits.OrderBy(entry => entry.Shot.Id).ThenBy(entry => entry.Id).ThenBy(entry => entry.Kind))
                if (targets().TryGetValue(hit.Id, out var position))
                    applyHit(new ExpandedCrossHit(hit.Kind, hit.Id, position, hit.Origin, hit.Shot, hit.Damage));

            void Collect(bool afterglow, ExpandedCrossHitKind kind)
            {
                Dictionary<int, (ExpandedCrossShotState Shot, Vector2 Origin, double Damage)>? winners = null;
                Dictionary<int, List<(double Start, double End, bool Returning)>>? thornContacts = null;
                var canOverlap = !afterglow && shots.Any(shot => shot.Returning && shot.Thorns.Count != 0);
                foreach (var shot in shots)
                {
                    var timer = afterglow ? shot.NextAfterglowAt : shot.NextThornAt;
                    if (!timer.HasValue || timer.Value > now + Epsilon) continue;
                    var next = timer.Value;
                    do { next += ThornInterval; } while (next <= now + Epsilon);
                    if (afterglow) shot.NextAfterglowAt = next;
                    else shot.NextThornAt = next;
                    var pieces = afterglow ? shot.Afterglow! : shot.Thorns;
                    var radius = afterglow ? (float)shot.AfterglowRadius : shot.ThornRadius;
                    var checkOverlap = canOverlap && shot.EmpoweredThornOverlap;
                    foreach (var target in targets())
                    {
                        var old = previous.TryGetValue(target.Key, out var before) ? before : target.Value;
                        foreach (var piece in pieces)
                        {
                            if (!CrossesThorn(old, target.Value, piece, radius)) continue;
                            var overlap = checkOverlap && HasOverlap(target.Key, old, target.Value, piece, radius);
                            var factor = afterglow ? BaseAfterglowDamageFactor +
                                (piece.Returning && shot.BoostReturningAfterglow ? ReturningAfterglowBonus : 0) :
                                ThornDamageFactor + (overlap ? ThornOverlapFactor : 0);
                            var damage = shot.Damage * factor;
                            winners ??= new Dictionary<int, (ExpandedCrossShotState Shot, Vector2 Origin, double Damage)>();
                            if (!winners.TryGetValue(target.Key, out var winner) || damage > winner.Damage)
                                winners[target.Key] = (shot, piece.Start, damage);
                            if (afterglow ? !shot.BoostReturningAfterglow || piece.Returning : !checkOverlap || overlap)
                                break;
                        }
                    }
                }
                if (winners == null) return;
                hits ??= new List<(ExpandedCrossShotState Shot, Vector2 Origin, int Id, double Damage, ExpandedCrossHitKind Kind)>();
                foreach (var hit in winners)
                    hits.Add((hit.Value.Shot, hit.Value.Origin, hit.Key, hit.Value.Damage, kind));

                bool HasOverlap(int id, Vector2 from, Vector2 to, ExpandedCrossThornState piece, double radius)
                {
                    var contact = ThornContact(from, to, piece, radius);
                    if (!contact.HasValue) return false;
                    thornContacts ??= new Dictionary<int, List<(double, double, bool)>>();
                    if (!thornContacts.TryGetValue(id, out var intervals))
                    {
                        intervals = new List<(double, double, bool)>();
                        foreach (var other in shots)
                        {
                            foreach (var candidate in other.Thorns)
                            {
                                var interval = ThornContact(from, to, candidate, other.ThornRadius);
                                if (interval.HasValue) intervals.Add((interval.Value.Start, interval.Value.End, candidate.Returning));
                            }
                        }
                        thornContacts.Add(id, intervals);
                    }

                    foreach (var interval in intervals)
                        if (interval.Returning != piece.Returning &&
                            interval.Start <= contact.Value.End + Epsilon && contact.Value.Start <= interval.End + Epsilon)
                            return true;
                    return false;
                }
            }
        }

        private static bool ApplyOriginalWithEliteBursts(ExpandedCrossState state, ExpandedCrossShotState shot, double now,
            ExpandedCrossHit original, ExpandedCrossRankOptions ranks, Func<int, bool>? isElite,
            Func<IReadOnlyDictionary<int, Vector2>> targets, Func<ExpandedCrossHit, bool> applyHit,
            Func<bool>? reserveTrail, Action? recordSupportSuppression, Func<ExpandedCrossHit, double?>? applyOriginalHit)
        {
            var elite = (ranks.EliteHitThorn || ranks.EliteKillLight) && isElite!(original.TargetId);
            var parentDamage = applyOriginalHit != null ? applyOriginalHit(original) :
                applyHit(original) ? original.Damage : (double?)null;
            if (!parentDamage.HasValue) return false;
            if (!elite) return true;

            var killed = !targets().ContainsKey(original.TargetId);
            if (ranks.EliteHitThorn && !state.NextEliteThornAt.HasValue &&
                EmitBurst(state, shot, ExpandedCrossHitKind.EliteThorn, original.Origin,
                    EliteThornRadius * AreaScale(ranks.WidenSupportShape, ranks.WidenDamageRegion), parentDamage.Value * EliteThornDamageFactor,
                    targets, applyHit, reserveTrail, recordSupportSuppression))
                state.NextEliteThornAt = now + EliteThornCooldown;
            if (ranks.EliteKillLight && killed && !state.NextEliteLightAt.HasValue &&
                EmitBurst(state, shot, ExpandedCrossHitKind.EliteLight, original.Position, EliteLightRadius, parentDamage.Value * EliteLightDamageFactor,
                    targets, applyHit, reserveTrail, recordSupportSuppression, id => !isElite!(id), 2))
                state.NextEliteLightAt = now + EliteLightCooldown;
            return true;
        }

        private static bool EmitBurst(ExpandedCrossState state, ExpandedCrossShotState shot, ExpandedCrossHitKind kind,
            Vector2 center, double radius, double damage, Func<IReadOnlyDictionary<int, Vector2>> targets,
            Func<ExpandedCrossHit, bool> applyHit, Func<bool>? reserveTrail, Action? recordSupportSuppression,
            Func<int, bool>? isEligible = null, int limit = int.MaxValue, Action<Vector2, double>? emit = null)
        {
            var range = (float)radius;
            var eligible = targets().Where(target => Vector2.DistanceSquared(center, target.Value) <= range * range + Epsilon &&
                (isEligible == null || isEligible(target.Key)));
            var ids = (isEligible != null ? eligible.OrderBy(target => Vector2.DistanceSquared(center, target.Value)).ThenBy(target => target.Key) :
                eligible.OrderBy(target => target.Key)).Select(target => target.Key).ToArray();
            var activeWithoutTarget = kind is ExpandedCrossHitKind.RecoveryWave or ExpandedCrossHitKind.TurnSpin or ExpandedCrossHitKind.IntersectionLight;
            if (ids.Length == 0 && !activeWithoutTarget) return false;
            if (state.ActiveTrailCount >= MaximumTrails || reserveTrail?.Invoke() == false)
            {
                recordSupportSuppression?.Invoke();
                return false;
            }
            emit?.Invoke(center, radius);
            var accepted = 0;
            foreach (var id in ids)
            {
                if (accepted >= limit) break;
                if (targets().TryGetValue(id, out var position) &&
                    applyHit(new ExpandedCrossHit(kind, id, position, center, shot, damage)))
                    accepted++;
            }
            return activeWithoutTarget || accepted > 0;
        }

        internal static double AreaScale(bool widenSupportShape, bool widenDamageRegion) =>
            1 + (widenSupportShape ? .04 : 0) + (widenDamageRegion ? .06 : 0);

        private static bool CrossesThorn(Vector2 from, Vector2 to, ExpandedCrossThornState piece, double radius)
        {
            var start = piece.Start;
            var end = piece.End;
            if (Math.Max(from.X, to.X) < Math.Min(start.X, end.X) - radius - Epsilon ||
                Math.Min(from.X, to.X) > Math.Max(start.X, end.X) + radius + Epsilon ||
                Math.Max(from.Y, to.Y) < Math.Min(start.Y, end.Y) - radius - Epsilon ||
                Math.Min(from.Y, to.Y) > Math.Max(start.Y, end.Y) + radius + Epsilon)
                return false;
            if (CircleContact(from - start, to - start, radius).HasValue ||
                CircleContact(from - end, to - end, radius).HasValue)
                return true;
            var offset = end - start;
            var length = offset.Length();
            if (length <= Epsilon) return false;
            var horizontal = offset / length;
            var vertical = new Vector2(-horizontal.Y, horizontal.X);
            from -= start;
            to -= start;
            double lower = 0, upper = 1;
            return Clip(Vector2.Dot(from, horizontal), Vector2.Dot(to, horizontal), 0, length, ref lower, ref upper) &&
                Clip(Vector2.Dot(from, vertical), Vector2.Dot(to, vertical), -radius, radius, ref lower, ref upper);
        }

        private static (double Start, double End)? ThornContact(Vector2 from, Vector2 to, ExpandedCrossThornState piece, double radius)
        {
            var start = piece.Start;
            var end = piece.End;
            if (Math.Max(from.X, to.X) < Math.Min(start.X, end.X) - radius - Epsilon ||
                Math.Min(from.X, to.X) > Math.Max(start.X, end.X) + radius + Epsilon ||
                Math.Max(from.Y, to.Y) < Math.Min(start.Y, end.Y) - radius - Epsilon ||
                Math.Min(from.Y, to.Y) > Math.Max(start.Y, end.Y) + radius + Epsilon)
                return null;

            var offset = end - start;
            var length = offset.Length();
            var result = CircleInterval(from - start, to - start, radius);
            if (length <= Epsilon) return result;
            var horizontal = offset / length;
            var vertical = new Vector2(-horizontal.Y, horizontal.X);
            double lower = 0, upper = 1;
            if (Clip(Vector2.Dot(from - start, horizontal), Vector2.Dot(to - start, horizontal), 0, length, ref lower, ref upper) &&
                Clip(Vector2.Dot(from - start, vertical), Vector2.Dot(to - start, vertical), -radius, radius, ref lower, ref upper))
                result = Merge(result, (lower, upper));
            return Merge(result, CircleInterval(from - end, to - end, radius));

            // A capsule is convex, so its intersection along the target's motion is one interval.
            static (double Start, double End)? Merge((double Start, double End)? first, (double Start, double End)? second)
                => !first.HasValue ? second : !second.HasValue ? first :
                    (Math.Min(first.Value.Start, second.Value.Start), Math.Max(first.Value.End, second.Value.End));
        }

        private static (double Start, double End)? CircleInterval(Vector2 from, Vector2 to, double radius)
        {
            var dx = (double)to.X - from.X;
            var dy = (double)to.Y - from.Y;
            var c = (double)from.X * from.X + (double)from.Y * from.Y - radius * radius;
            var a = dx * dx + dy * dy;
            if (a == 0) return c <= Epsilon ? (0d, 1d) : ((double, double)?)null;
            var b = from.X * dx + from.Y * dy;
            var discriminant = b * b - a * c;
            if (discriminant < 0) return c <= Epsilon ? (0d, 0d) : ((double, double)?)null;
            var root = Math.Sqrt(discriminant);
            var lower = Math.Max(0, (-b - root) / a);
            var upper = Math.Min(1, (-b + root) / a);
            if (lower <= upper + Epsilon) return (Math.Min(1, lower), Math.Max(0, upper));
            return c <= Epsilon ? (0d, 0d) : ((double, double)?)null;
        }

        private static bool Clip(double from, double to, double min, double max, ref double lower, ref double upper)
        {
            var delta = to - from;
            if (delta == 0) return from >= min - Epsilon && from <= max + Epsilon;
            var first = (min - from) / delta;
            var last = (max - from) / delta;
            lower = Math.Max(lower, Math.Min(first, last));
            upper = Math.Min(upper, Math.Max(first, last));
            return lower <= upper + Epsilon;
        }

        private static double? CircleContact(Vector2 from, Vector2 to, double radius)
        {
            var dx = (double)to.X - from.X;
            var dy = (double)to.Y - from.Y;
            var c = (double)from.X * from.X + (double)from.Y * from.Y - radius * radius;
            if (c <= Epsilon) return 0;
            var a = dx * dx + dy * dy;
            if (a <= 0) return null;
            var b = from.X * dx + from.Y * dy;
            var discriminant = b * b - a * c;
            if (discriminant < 0) return null;
            var fraction = (-b - Math.Sqrt(discriminant)) / a;
            return fraction >= 0 && fraction <= 1 ? fraction : (double?)null;
        }

        private static Vector2 DirectionOr(Vector2 value, Vector2 fallback) =>
            value.LengthSquared() > Epsilon ? Vector2.Normalize(value) : fallback;

        // A strip can split when a step straddles the turn or the end of curved return.
        internal static int ThornPieceLimit(double duration) => (int)Math.Ceiling(duration * 60) + 3;

        private static double? ResolveTrainedSpeed(double speed, double basis)
        {
            var trained = Math.Min(basis * 1.75, speed + basis * .12);
            return trained > speed ? trained : (double?)null;
        }

        internal static bool ValidTrainedSpeed(double untrained, double? trained) => !trained.HasValue ||
            (trained.Value > untrained && Within(trained.Value, MinimumSpeed,
                Math.Min(MaximumSpeed, untrained + Profile.AtLevel(8).Speed * .12)));

        internal static bool ValidCooldown(double? at, double now, double duration) =>
            !at.HasValue || Within(at.Value, now, now + duration);

        internal static bool ValidOriginalBaseDamage(double value, bool evolved) => value == 0 ||
            (evolved ? value == Profile.AtLevel(8).Damage : OriginalDamageByLevel.Contains(value));

        internal static bool Finite(double number) => !double.IsNaN(number) && !double.IsInfinity(number);
        internal static bool Within(double value, double minimum, double maximum) =>
            Finite(value) && value >= minimum - Epsilon && value <= maximum + Epsilon;
        internal static bool ValidPosition(Vector2 value) => Within(value.X, -1e6, 1e6) && Within(value.Y, -1e6, 1e6);
        internal static bool UnitDirection(Vector2 value) => ValidPosition(value) && Math.Abs(value.LengthSquared() - 1) <= 1e-5;
        internal static bool ValidGeometry(double radius, double range, double speed) => radius > 0 && range > 0 &&
            Within(radius, 0, MaximumRadius) && Within(range, 0, MaximumRange) && Within(speed, MinimumSpeed, MaximumSpeed);
    }
}
