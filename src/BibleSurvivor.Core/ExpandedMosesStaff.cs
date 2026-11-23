using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.Serialization;

namespace BibleSurvivor.Core
{
    public enum ExpandedMosesStaffHitKind
    {
        Wave,
        Lane,
        ReturnWave,
        EndPillar,
        EliteSplash,
        ResidualLane,
        ResidualEdge,
        ResidualCross,
        ResidualEnd,
        WaterScreen,
        EncirclingWave,
        EvolvedReturnWave,
        EliteKillLane,
    }

    public readonly struct ExpandedMosesStaffRankOptions
    {
        public bool WidenWave { get; }
        public bool WidenFan { get; }
        public bool PrioritizeElite { get; }
        public bool IncreaseKnockback { get; }
        public bool WidenLane { get; }
        public bool EmpowerLaneEdge { get; }
        public bool ExtendLane { get; }
        public bool WidenSupportShape { get; }
        public bool WidenDamageRegion { get; }
        public double ResidualDurationBonus { get; }
        internal byte TechniqueRanks { get; }
        internal byte ResidualLaneRanks { get; }
        internal bool WaterScreen { get; }
        internal bool EraseHostileProjectiles { get; }
        internal bool AdvanceOnHurt { get; }
        internal bool EncirclingWave { get; }
        internal bool EvolvedLaneReturn { get; }
        internal bool EliteKillLane { get; }
        internal double NormalResidualDurationBonus { get; }

        public ExpandedMosesStaffRankOptions(bool widenWave = false, bool widenFan = false, bool prioritizeElite = false,
            bool increaseKnockback = false, bool widenLane = false, bool empowerLaneEdge = false, bool extendLane = false,
            bool widenSupportShape = false, double residualDurationBonus = 0, bool widenDamageRegion = false,
            bool returningWave = false, bool bendingWave = false, bool endPillar = false,
            bool closeDamage = false, bool eliteSplash = false, bool fourthCycleFirstWave = false,
            bool residualLane = false, bool longerResidualLane = false, bool residualLaneEdge = false,
            bool residualLaneCrossing = false, bool residualLaneEnd = false, double normalResidualDurationBonus = 0,
            bool waterScreen = false, bool eraseHostileProjectiles = false, bool advanceOnHurt = false,
            bool encirclingWave = false, bool evolvedLaneReturn = false, bool eliteKillLane = false)
        {
            if (!ExpandedWeaponRules.ValidResidualDurationBonus(residualDurationBonus) ||
                !ExpandedWeaponRules.ValidResidualDurationBonus(normalResidualDurationBonus))
                throw new ArgumentOutOfRangeException(nameof(residualDurationBonus));
            WidenWave = widenWave;
            WidenFan = widenFan;
            PrioritizeElite = prioritizeElite;
            IncreaseKnockback = increaseKnockback;
            WidenLane = widenLane;
            EmpowerLaneEdge = empowerLaneEdge;
            ExtendLane = extendLane;
            WidenSupportShape = widenSupportShape;
            WidenDamageRegion = widenDamageRegion;
            ResidualDurationBonus = residualDurationBonus;
            TechniqueRanks = (byte)((returningWave ? ExpandedMosesStaff.ReturningWave : 0) |
                (bendingWave ? ExpandedMosesStaff.BendingWave : 0) |
                (endPillar ? ExpandedMosesStaff.EndPillar : 0) |
                (closeDamage ? ExpandedMosesStaff.CloseDamage : 0) |
                (eliteSplash ? ExpandedMosesStaff.EliteSplash : 0) |
                (fourthCycleFirstWave ? ExpandedMosesStaff.FourthCycleFirstWave : 0));
            ResidualLaneRanks = residualLane ? (byte)(ExpandedMosesStaff.ResidualLane |
                (longerResidualLane ? ExpandedMosesStaff.LongerResidualLane : 0) |
                (residualLaneEdge ? ExpandedMosesStaff.ResidualLaneEdge : 0) |
                (residualLaneCrossing ? ExpandedMosesStaff.ResidualLaneCrossing : 0) |
                (residualLaneEnd ? ExpandedMosesStaff.ResidualLaneEnd : 0)) : (byte)0;
            NormalResidualDurationBonus = normalResidualDurationBonus;
            WaterScreen = waterScreen;
            EraseHostileProjectiles = eraseHostileProjectiles;
            AdvanceOnHurt = advanceOnHurt;
            EncirclingWave = encirclingWave;
            EvolvedLaneReturn = evolvedLaneReturn;
            EliteKillLane = eliteKillLane;
        }

        public static ExpandedMosesStaffRankOptions FromRanks(IReadOnlyDictionary<string, int> ranks)
        {
            if (ranks == null) throw new ArgumentNullException(nameof(ranks));
            bool Has(string id) => ranks.TryGetValue(id, out var rank) && rank > 0;
            return new ExpandedMosesStaffRankOptions(
                Has("weapon.moses_staff.a.01"), Has("weapon.moses_staff.a.02"), Has("weapon.moses_staff.a.03"),
                Has("weapon.moses_staff.b.01"), Has("weapon.moses_staff.e.01"), Has("weapon.moses_staff.e.02"),
                Has("weapon.moses_staff.e.03"), Has("common.area.01"),
                ExpandedWeaponRules.ResidualDurationBonus(ranks, true), Has("common.area.09"),
                Has("weapon.moses_staff.a.04"), Has("weapon.moses_staff.a.05"),
                Has("weapon.moses_staff.b.02"), Has("weapon.moses_staff.b.03"),
                Has("weapon.moses_staff.b.04"), Has("weapon.moses_staff.b.05"),
                Has("weapon.moses_staff.c.01"), Has("weapon.moses_staff.c.02"), Has("weapon.moses_staff.c.03"),
                Has("weapon.moses_staff.c.04"), Has("weapon.moses_staff.c.05"),
                ExpandedWeaponRules.ResidualDurationBonus(ranks, false), Has("weapon.moses_staff.d.01"),
                Has("weapon.moses_staff.d.02"), Has("weapon.moses_staff.d.03"), Has("weapon.moses_staff.d.05"),
                Has("weapon.moses_staff.e.04"), Has("weapon.moses_staff.e.05"));
        }
    }

    public readonly struct ExpandedMosesStaffHostileSweep
    {
        public long Id { get; }
        public Vector2 From { get; }
        public Vector2 To { get; }
        public double Radius { get; }

        public ExpandedMosesStaffHostileSweep(long id, Vector2 from, Vector2 to, double radius)
        {
            if (id <= 0 || !ExpandedMosesStaff.ValidPosition(from) || !ExpandedMosesStaff.ValidPosition(to) ||
                !ExpandedMosesStaff.Finite(radius) || radius <= 0)
                throw new ArgumentException("Invalid staff hostile sweep");
            Id = id;
            From = from;
            To = to;
            Radius = radius;
        }
    }

    public readonly struct ExpandedMosesStaffHit
    {
        public ExpandedMosesStaffHitKind Kind { get; }
        public int TargetId { get; }
        public Vector2 Position { get; }
        public Vector2 Origin { get; }
        public Vector2 Direction { get; }
        public double Damage { get; }
        public double OriginalBaseDamage { get; }
        public int TargetOrdinal { get; }
        public double Knockback { get; }
        public long WaveId { get; }
        public long Cycle { get; }

        internal ExpandedMosesStaffHit(ExpandedMosesStaffHitKind kind, int targetId, Vector2 position, Vector2 origin,
            ExpandedMosesStaffWaveState wave, bool laneEdge = false, double? waveDamage = null)
        {
            Kind = kind;
            TargetId = targetId;
            Position = position;
            Origin = origin;
            Direction = wave.TravelDirection;
            Damage = kind == ExpandedMosesStaffHitKind.Wave ? waveDamage ?? wave.Damage : wave.LaneDamage(laneEdge);
            OriginalBaseDamage = kind == ExpandedMosesStaffHitKind.Wave ? wave.OriginalBaseDamage : 0;
            TargetOrdinal = kind == ExpandedMosesStaffHitKind.Wave ? wave.OriginalHitCount + 1 : 0;
            Knockback = kind == ExpandedMosesStaffHitKind.Wave ? wave.Knockback : 0;
            WaveId = wave.Id;
            Cycle = wave.Cycle;
        }

        internal ExpandedMosesStaffHit(ExpandedMosesStaffHitKind kind, int targetId, Vector2 position, Vector2 origin,
            Vector2 direction, double damage, long waveId, long cycle)
        {
            Kind = kind;
            TargetId = targetId;
            Position = position;
            Origin = origin;
            Direction = direction;
            Damage = damage;
            OriginalBaseDamage = 0;
            TargetOrdinal = 0;
            Knockback = 0;
            WaveId = waveId;
            Cycle = cycle;
        }
    }

    [DataContract]
    public sealed class ExpandedMosesStaffWaveState
    {
        [DataMember(IsRequired = true)] public long Id { get; set; }
        [DataMember(IsRequired = true)] public long Cycle { get; set; }
        [DataMember(IsRequired = true)] public double EmittedAt { get; set; }
        [DataMember(IsRequired = true)] public double LastStepAt { get; set; }
        [DataMember(IsRequired = true)] public float OriginX { get; set; }
        [DataMember(IsRequired = true)] public float OriginY { get; set; }
        [DataMember(IsRequired = true)] public float DirectionX { get; set; }
        [DataMember(IsRequired = true)] public float DirectionY { get; set; }
        [DataMember(IsRequired = true)] public double Damage { get; set; }
        [DataMember(EmitDefaultValue = false)] public double OriginalBaseDamage { get; set; }
        [DataMember(EmitDefaultValue = false)] public int OriginalHitCount { get; set; }
        [DataMember(EmitDefaultValue = false)] public byte TechniqueRanks { get; set; }
        [DataMember(EmitDefaultValue = false)] public double? TurnAtDistance { get; set; }
        [DataMember(EmitDefaultValue = false)] public float? TurnDirectionX { get; set; }
        [DataMember(EmitDefaultValue = false)] public float? TurnDirectionY { get; set; }
        [DataMember(IsRequired = true)] public double Width { get; set; }
        [DataMember(IsRequired = true)] public double Range { get; set; }
        [DataMember(IsRequired = true)] public double Speed { get; set; }
        [DataMember(IsRequired = true)] public double Distance { get; set; }
        [DataMember(IsRequired = true)] public bool MakesLane { get; set; }
        [DataMember(EmitDefaultValue = false)] public double? LaneStartedAt { get; set; }
        [DataMember(EmitDefaultValue = false)] public int LaneTicks { get; set; }
        [DataMember(EmitDefaultValue = false)] public bool IncreasedKnockback { get; set; }
        [DataMember(EmitDefaultValue = false)] public bool WidenedLane { get; set; }
        [DataMember(EmitDefaultValue = false)] public bool EmpoweredLaneEdge { get; set; }
        [DataMember(EmitDefaultValue = false)] public bool ExtendedLane { get; set; }
        [DataMember(EmitDefaultValue = false)] public bool WidenedSupportShape { get; set; }
        [DataMember(EmitDefaultValue = false)] public bool WiderDamageRegion { get; set; }
        [DataMember(EmitDefaultValue = false)] public double ResidualDurationBonus { get; set; }
        [DataMember(IsRequired = true)] public List<int> HitIds { get; set; } = new List<int>();

        [DataMember(EmitDefaultValue = false)] public byte ResidualLaneRanks { get; set; }
        [DataMember(EmitDefaultValue = false)] public double? ResidualLaneStartedAt { get; set; }
        [DataMember(EmitDefaultValue = false)] public int ResidualLaneTicks { get; set; }
        [DataMember(EmitDefaultValue = false)] public double ResidualLaneWidthBonus { get; set; }
        [DataMember(EmitDefaultValue = false)] public double ResidualLaneDurationBonus { get; set; }
        [DataMember(EmitDefaultValue = false)] public List<int>? ResidualLaneEdgeIds { get; set; }
        [DataMember(EmitDefaultValue = false)] public List<long>? ResidualLaneOverlapIds { get; set; }

        [DataMember(EmitDefaultValue = false)] public bool ErasesHostileProjectiles { get; set; }
        [DataMember(EmitDefaultValue = false)] public List<long>? HostileEraseAttemptIds { get; set; }
        [DataMember(EmitDefaultValue = false)] public bool ReturnsEvolvedLane { get; set; }
        [DataMember(EmitDefaultValue = false)] public bool LeavesEliteKillLane { get; set; }
        [DataMember(EmitDefaultValue = false)] public PermanentShotHitBonuses SharedShotHitBonuses { get; set; }

        public Vector2 Origin => new Vector2(OriginX, OriginY);
        public Vector2 Direction => new Vector2(DirectionX, DirectionY);
        public Vector2 TurnPosition => Origin + Direction * (float)TurnAtDistance.GetValueOrDefault();
        public Vector2 TravelDirection => TurnAtDistance.HasValue
            ? new Vector2(TurnDirectionX.GetValueOrDefault(), TurnDirectionY.GetValueOrDefault()) : Direction;
        public Vector2 Position => PositionAt(Distance);

        internal Vector2 PositionAt(double distance) => TurnAtDistance is double turn && distance > turn
            ? TurnPosition + TravelDirection * (float)(distance - turn) : Origin + Direction * (float)distance;
        public bool IsMoving => Distance < Range;
        public bool HasLane => MakesLane && LaneTicks <= LaneTickCount;
        public bool HasResidualLane => ResidualLaneRanks != 0 && ResidualLaneTicks <= ResidualLaneTickCount;
        public double ResidualLaneLength => HasResidualLane ? Distance : 0;
        public double ResidualLaneWidth => ExpandedMosesStaff.BaseResidualWidth * (1 + ResidualLaneWidthBonus);
        public double ResidualLaneDuration => ExpandedMosesStaff.BaseResidualDuration * Math.Min(1.75,
            1 + ((ResidualLaneRanks & ExpandedMosesStaff.LongerResidualLane) != 0 ? .15 : 0) + ResidualLaneDurationBonus);
        internal int ResidualLaneTickCount => (int)Math.Ceiling(ResidualLaneDuration / ExpandedMosesStaff.LaneInterval);
        internal int ExpiredResidualLane => ResidualLaneTickCount + 1;
        public double LaneLength => HasLane ? Math.Min(Distance, ExpandedMosesStaff.MaximumLaneLength) : 0;
        public double LaneWidth => ExpandedMosesStaff.BaseLaneWidth *
            (1 + (WidenedLane ? .15 : 0) + (WidenedSupportShape ? .04 : 0) + (WiderDamageRegion ? .06 : 0));
        public double LaneDuration => ExpandedMosesStaff.BaseLaneDuration *
            (1 + (ExtendedLane ? .20 : 0) + ResidualDurationBonus);
        internal int LaneTickCount => (int)Math.Ceiling(LaneDuration / ExpandedMosesStaff.LaneInterval);
        internal int ExpiredLane => LaneTickCount + 1;
        internal double Knockback => Math.Min(1, ExpandedMosesStaff.Profile.Knockback + (IncreasedKnockback ? .25 : 0));
        internal double LaneDamage(bool outer) =>
            Damage * (ExpandedMosesStaff.LaneDamageFactor + (EmpoweredLaneEdge && outer ? .12 : 0));

        internal ExpandedMosesStaffWaveState Copy()
        {
            var copy = (ExpandedMosesStaffWaveState)MemberwiseClone();
            copy.HitIds = new List<int>(HitIds);
            copy.ResidualLaneEdgeIds = ResidualLaneEdgeIds == null ? null : new List<int>(ResidualLaneEdgeIds);
            copy.ResidualLaneOverlapIds = ResidualLaneOverlapIds == null ? null : new List<long>(ResidualLaneOverlapIds);
            copy.HostileEraseAttemptIds = HostileEraseAttemptIds == null ? null : new List<long>(HostileEraseAttemptIds);
            return copy;
        }
    }

    [DataContract]
    public sealed class ExpandedMosesStaffReturnState
    {
        [DataMember(IsRequired = true)] public long SourceWaveId { get; set; }
        [DataMember(IsRequired = true)] public long Cycle { get; set; }
        [DataMember(IsRequired = true)] public double StartedAt { get; set; }
        [DataMember(IsRequired = true)] public double LastStepAt { get; set; }
        [DataMember(IsRequired = true)] public float X { get; set; }
        [DataMember(IsRequired = true)] public float Y { get; set; }
        [DataMember(IsRequired = true)] public float DirectionX { get; set; }
        [DataMember(IsRequired = true)] public float DirectionY { get; set; }
        [DataMember(IsRequired = true)] public double Width { get; set; }
        [DataMember(IsRequired = true)] public double Damage { get; set; }
        [DataMember(IsRequired = true)] public double Distance { get; set; }
        [DataMember(IsRequired = true)] public List<int> HitIds { get; set; } = new List<int>();
        [DataMember(EmitDefaultValue = false)] public bool FromEvolvedLane { get; set; }
        [DataMember(EmitDefaultValue = false)] public double? TravelRange { get; set; }
        [DataMember(EmitDefaultValue = false)] public double? TravelSpeed { get; set; }

        public double Range => TravelRange ?? ExpandedMosesStaff.ReturnRange;
        public double Speed => TravelSpeed ?? ExpandedMosesStaff.ReturnSpeed;
        public Vector2 Origin => new Vector2(X, Y);
        public Vector2 Direction => new Vector2(DirectionX, DirectionY);
        public Vector2 Position => Origin + Direction * (float)Distance;

        internal ExpandedMosesStaffReturnState Copy()
        {
            var copy = (ExpandedMosesStaffReturnState)MemberwiseClone();
            copy.HitIds = new List<int>(HitIds);
            return copy;
        }
    }

    [DataContract]
    public sealed class ExpandedMosesStaffEliteKillLaneState
    {
        [DataMember(IsRequired = true)] public long SourceWaveId { get; set; }
        [DataMember(IsRequired = true)] public long Cycle { get; set; }
        [DataMember(IsRequired = true)] public int VictimId { get; set; }
        [DataMember(IsRequired = true)] public double StartedAt { get; set; }
        [DataMember(IsRequired = true)] public double EndsAt { get; set; }
        [DataMember(IsRequired = true)] public double NextPulseAt { get; set; }
        [DataMember(IsRequired = true)] public float X { get; set; }
        [DataMember(IsRequired = true)] public float Y { get; set; }
        [DataMember(IsRequired = true)] public float DirectionX { get; set; }
        [DataMember(IsRequired = true)] public float DirectionY { get; set; }
        [DataMember(IsRequired = true)] public double Width { get; set; }
        [DataMember(IsRequired = true)] public double Damage { get; set; }

        public Vector2 Origin => new Vector2(X, Y);
        public Vector2 Direction => new Vector2(DirectionX, DirectionY);
        public double Length => ExpandedMosesStaff.EliteKillLaneLength;

        internal ExpandedMosesStaffEliteKillLaneState Copy() => (ExpandedMosesStaffEliteKillLaneState)MemberwiseClone();
    }

    [DataContract]
    public sealed class ExpandedMosesStaffState
    {
        [DataMember(IsRequired = true)] public bool Evolved { get; set; }
        [DataMember(IsRequired = true)] public long Cycle { get; set; }
        [DataMember(IsRequired = true)] public long NextWaveId { get; set; } = 1;
        [DataMember(IsRequired = true)] public double NextCycle { get; set; }
        [DataMember(IsRequired = true)] public List<ExpandedMosesStaffWaveState> Waves { get; set; } =
            new List<ExpandedMosesStaffWaveState>();

        [DataMember(EmitDefaultValue = false)] public List<ExpandedMosesStaffReturnState>? ReturnWaves { get; set; }
        [DataMember(EmitDefaultValue = false)] public double? NextReturnAt { get; set; }
        [DataMember(EmitDefaultValue = false)] public double? NextEndPillarAt { get; set; }
        [DataMember(EmitDefaultValue = false)] public double? NextEliteSplashAt { get; set; }

        public int ActiveReturnCount => ReturnWaves?.Count ?? 0;
        public int ActiveWaveCount => Waves.Count(wave => wave.IsMoving);
        [DataMember(EmitDefaultValue = false)] public double? NextResidualCrossAt { get; set; }
        [DataMember(EmitDefaultValue = false)] public double? NextResidualEndAt { get; set; }
        [DataMember(EmitDefaultValue = false)] public double? NextWaterScreenAt { get; set; }
        [DataMember(EmitDefaultValue = false)] public double? NextEncirclingWaveAt { get; set; }
        [DataMember(EmitDefaultValue = false)] public double? NextHostileEraseAt { get; set; }
        [DataMember(EmitDefaultValue = false)] public uint? HostileEraseRandomState { get; set; }
        [DataMember(EmitDefaultValue = false)] public double? LastCycleStartedAt { get; set; }
        [DataMember(EmitDefaultValue = false)] public double? LastPlayerHurtObservedAt { get; set; }
        [DataMember(EmitDefaultValue = false)] public double? NextHurtAdvanceAt { get; set; }
        [DataMember(EmitDefaultValue = false)] public double? NextEvolvedReturnAt { get; set; }
        [DataMember(EmitDefaultValue = false)] public double? NextEliteKillLaneAt { get; set; }
        [DataMember(EmitDefaultValue = false)] public ExpandedMosesStaffEliteKillLaneState? EliteKillLane { get; set; }

        public int ActiveResidualLaneCount => Waves.Count(wave => wave.HasResidualLane);
        public int ActiveLaneCount => Waves.Count(wave => wave.HasLane) + ActiveResidualLaneCount + (EliteKillLane != null ? 1 : 0);
        public int ActiveExtraGroups => ActiveResidualLaneCount + ActiveReturnCount + (EliteKillLane != null ? 1 : 0);

        public ExpandedMosesStaffState Copy() => new ExpandedMosesStaffState
        {
            Evolved = Evolved,
            Cycle = Cycle,
            NextWaveId = NextWaveId,
            NextCycle = NextCycle,
            Waves = Waves.Select(wave => wave.Copy()).ToList(),
            ReturnWaves = ReturnWaves?.Select(wave => wave.Copy()).ToList(),
            NextReturnAt = NextReturnAt,
            NextEndPillarAt = NextEndPillarAt,
            NextEliteSplashAt = NextEliteSplashAt,
            NextResidualCrossAt = NextResidualCrossAt,
            NextResidualEndAt = NextResidualEndAt,
            NextWaterScreenAt = NextWaterScreenAt, NextEncirclingWaveAt = NextEncirclingWaveAt,
            NextHostileEraseAt = NextHostileEraseAt, HostileEraseRandomState = HostileEraseRandomState,
            LastCycleStartedAt = LastCycleStartedAt, LastPlayerHurtObservedAt = LastPlayerHurtObservedAt,
            NextHurtAdvanceAt = NextHurtAdvanceAt,
            NextEvolvedReturnAt = NextEvolvedReturnAt, NextEliteKillLaneAt = NextEliteKillLaneAt,
            EliteKillLane = EliteKillLane?.Copy(),
        };

        public void ResetForForm(bool evolved, double now)
        {
            if (!ExpandedMosesStaff.Finite(now) || now < 0) throw new ArgumentOutOfRangeException(nameof(now));
            Evolved = evolved;
            NextCycle = now;
            Waves.Clear();
            ReturnWaves = null;
            NextReturnAt = null;
            NextEndPillarAt = null;
            NextEliteSplashAt = null;
            NextResidualCrossAt = null;
            NextResidualEndAt = null;
            NextWaterScreenAt = null;
            NextEncirclingWaveAt = null;
            NextHostileEraseAt = null;
            LastCycleStartedAt = null;
            LastPlayerHurtObservedAt = null;
            NextHurtAdvanceAt = null;
            NextEvolvedReturnAt = null;
            NextEliteKillLaneAt = null;
            EliteKillLane = null;
        }

        public void Validate(double now, bool evolved)
        {
            if (!ExpandedMosesStaff.Finite(now) || now < 0 || Evolved != evolved || Cycle < 0 || NextWaveId < 1 ||
                (NextWaveId > 1 && (NextWaveId - 2) / ExpandedMosesStaff.MaximumAmount >= Cycle) ||
                !ExpandedMosesStaff.Finite(NextCycle) || NextCycle < 0 ||
                NextCycle > now + ExpandedMosesStaff.MaximumCycle + ExpandedMosesStaff.TimeEpsilon ||
                Waves == null || Waves.Count > ExpandedMosesStaff.MaximumMovingWaves + ExpandedMosesStaff.MaximumLanes ||
                !ExpandedMosesStaff.ValidTechniqueCooldown(NextReturnAt, now) ||
                !ExpandedMosesStaff.ValidTechniqueCooldown(NextEndPillarAt, now) ||
                !ExpandedMosesStaff.ValidTechniqueCooldown(NextEliteSplashAt, now) ||
                !ExpandedMosesStaff.ValidTechniqueCooldown(NextResidualCrossAt, now, ExpandedMosesStaff.ResidualCooldown) ||
                !ExpandedMosesStaff.ValidTechniqueCooldown(NextResidualEndAt, now, ExpandedMosesStaff.ResidualCooldown) ||
                !ExpandedMosesStaff.ValidTechniqueCooldown(NextWaterScreenAt, now) ||
                !ExpandedMosesStaff.ValidTechniqueCooldown(NextEncirclingWaveAt, now, ExpandedMosesStaff.EncirclingCooldown) ||
                !ExpandedMosesStaff.ValidTechniqueCooldown(NextHostileEraseAt, now) ||
                NextHostileEraseAt.HasValue && !HostileEraseRandomState.HasValue || HostileEraseRandomState == 0 ||
                !ExpandedMosesStaff.ValidTechniqueCooldown(NextHurtAdvanceAt, now, ExpandedMosesStaff.HurtAdvanceCooldown) ||
                !ExpandedMosesStaff.ValidTechniqueCooldown(NextEvolvedReturnAt, now, ExpandedMosesStaff.ResidualCooldown) ||
                !ExpandedMosesStaff.ValidTechniqueCooldown(NextEliteKillLaneAt, now, ExpandedMosesStaff.EliteKillLaneCooldown) ||
                !Evolved && (NextEvolvedReturnAt.HasValue || NextEliteKillLaneAt.HasValue || EliteKillLane != null) ||
                !ExpandedMosesStaff.ValidTechniqueCooldown(LastPlayerHurtObservedAt, now, 0) ||
                !ExpandedMosesStaff.ValidTechniqueCooldown(LastCycleStartedAt, now, 0) ||
                NextHurtAdvanceAt.HasValue && !LastPlayerHurtObservedAt.HasValue ||
                LastCycleStartedAt.HasValue && (Cycle < 1 || NextCycle < LastCycleStartedAt.Value + .15 - ExpandedMosesStaff.TimeEpsilon))
                throw new ArgumentException("Invalid expanded staff clock");

            ValidateReturns(now);
            ValidateEliteKillLane(now);
            long lastId = 0;
            var moving = 0;
            var lanes = 0;
            var fourthWaves = new HashSet<long>();
            foreach (var wave in Waves)
            {
                if (wave == null || wave.Id <= lastId || wave.Id >= NextWaveId || wave.Cycle < 1 || wave.Cycle > Cycle ||
                    !ExpandedMosesStaff.ValidPosition(wave.Origin) || !ExpandedMosesStaff.ValidPosition(wave.Position) ||
                    !ExpandedMosesStaff.UnitDirection(wave.Direction) ||
                    !ExpandedMosesStaff.Finite(wave.EmittedAt) || wave.EmittedAt < 0 || wave.EmittedAt > now + ExpandedMosesStaff.TimeEpsilon ||
                    !ExpandedMosesStaff.Finite(wave.LastStepAt) || wave.LastStepAt < wave.EmittedAt ||
                    wave.LastStepAt > now + ExpandedMosesStaff.TimeEpsilon ||
                    !ExpandedMosesStaff.Finite(wave.Damage) || wave.Damage < 1 || wave.Damage > ExpandedMosesStaff.MaximumDamage ||
                    !ExpandedMosesStaff.ValidOriginalBaseDamage(wave.OriginalBaseDamage, evolved) ||
                    !ExpandedMosesStaff.ValidTechniques(wave) ||
                    (wave.SharedShotHitBonuses & ~PermanentShotHitBonuses.LongFlight) != 0 ||
                    wave.SharedShotHitBonuses != PermanentShotHitBonuses.None && wave.OriginalBaseDamage <= 0 ||
                    (wave.TechniqueRanks & ExpandedMosesStaff.FourthCycleFirstWave) != 0 && !fourthWaves.Add(wave.Cycle) ||
                    wave.OriginalHitCount < 0 || wave.OriginalHitCount > ExpandedMosesStaff.MaximumOriginalHits ||
                    !ExpandedMosesStaff.Finite(wave.Width) || wave.Width <= 0 || wave.Width > ExpandedMosesStaff.MaximumWidth ||
                    !ExpandedMosesStaff.ValidMotion(wave.Range, wave.Speed) || !ExpandedMosesStaff.Finite(wave.Distance) ||
                    wave.Distance < 0 || wave.Distance > wave.Range ||
                    Math.Abs(wave.Distance - Math.Min(wave.Range, wave.Speed * (wave.LastStepAt - wave.EmittedAt))) > ExpandedMosesStaff.DistanceEpsilon ||
                    wave.HitIds == null || wave.HitIds.Count > ExpandedMosesStaff.MaximumLiveTargets ||
                    (wave.OriginalBaseDamage != 0 || wave.OriginalHitCount != 0) && wave.OriginalHitCount < wave.HitIds.Count ||
                    wave.HitIds.Any(id => id < 1) || wave.HitIds.Distinct().Count() != wave.HitIds.Count ||
                    (!wave.IsMoving && !wave.HasLane && !wave.HasResidualLane) ||
                    !ExpandedMosesStaff.ValidResidualLane(wave, now) ||
                    wave.ReturnsEvolvedLane && !wave.MakesLane || wave.LeavesEliteKillLane && !Evolved ||
                    wave.HostileEraseAttemptIds != null && (!wave.ErasesHostileProjectiles || !HostileEraseRandomState.HasValue ||
                        !wave.IsMoving || wave.HostileEraseAttemptIds.Count == 0 ||
                        wave.HostileEraseAttemptIds.Count > ExpandedMosesStaff.MaximumTrackedHostiles ||
                        wave.HostileEraseAttemptIds.Any(id => id <= 0) ||
                        wave.HostileEraseAttemptIds.Distinct().Count() != wave.HostileEraseAttemptIds.Count))
                    throw new ArgumentException("Invalid expanded staff wave");

                if (wave.IsMoving && (++moving > ExpandedMosesStaff.MaximumMovingWaves ||
                    wave.LastStepAt < now - ExpandedMosesStaff.TimeEpsilon))
                    throw new ArgumentException("Invalid expanded staff travel");
                if (!wave.MakesLane)
                {
                    if (wave.LaneStartedAt.HasValue || wave.LaneTicks != 0 || wave.WidenedLane ||
                        wave.EmpoweredLaneEdge || wave.ExtendedLane || wave.WidenedSupportShape || wave.WiderDamageRegion || wave.ResidualDurationBonus != 0)
                        throw new ArgumentException("Unexpected expanded staff lane");
                }
                else
                {
                    if (!Evolved || !ExpandedWeaponRules.ValidResidualDurationBonus(wave.ResidualDurationBonus) ||
                        wave.LaneTicks < 0 || wave.LaneTicks > wave.ExpiredLane ||
                        (wave.HasLane && ++lanes > ExpandedMosesStaff.MaximumLanes))
                        throw new ArgumentException("Invalid expanded staff lane");
                    if (!wave.LaneStartedAt.HasValue)
                    {
                        if (wave.Distance != 0 || wave.LaneTicks != 0)
                            throw new ArgumentException("Expanded staff lane has no starting tick");
                    }
                    else
                    {
                        var start = wave.LaneStartedAt.Value;
                        var elapsed = now - start + ExpandedMosesStaff.TimeEpsilon;
                        var expectedTicks = elapsed >= wave.LaneDuration ? wave.ExpiredLane :
                            1 + (int)Math.Floor(elapsed / ExpandedMosesStaff.LaneInterval);
                        if (!ExpandedMosesStaff.Finite(start) || start <= wave.EmittedAt || start > wave.LastStepAt ||
                            expectedTicks != wave.LaneTicks)
                            throw new ArgumentException("Invalid expanded staff lane schedule");
                    }
                }
                lastId = wave.Id;
            }
            if (ActiveLaneCount > ExpandedMosesStaff.MaximumLanes || ActiveExtraGroups > ExpandedMosesStaff.MaximumExtraGroups)
                throw new ArgumentException("Too many expanded staff lane groups");
            if (Waves.Any(wave => wave.ResidualLaneOverlapIds != null))
            {
                var liveIds = Waves.Where(wave => wave.HasResidualLane && wave.ResidualLaneStartedAt.HasValue)
                    .Select(wave => wave.Id).ToHashSet();
                if (Waves.Any(wave => wave.ResidualLaneOverlapIds?.Any(id => !liveIds.Contains(id)) == true))
                    throw new ArgumentException("Expanded staff residual overlap refers to a missing lane");
            }
        }

        private void ValidateReturns(double now)
        {
            if (ReturnWaves == null) return;
            if (ReturnWaves.Count == 0 || ReturnWaves.Count > ExpandedMosesStaff.MaximumExtraGroups)
                throw new ArgumentException("Invalid expanded staff return waves");
            var sourceIds = new HashSet<(long WaveId, bool FromEvolvedLane)>();
            foreach (var wave in ReturnWaves)
            {
                if (wave == null || wave.SourceWaveId < 1 || wave.SourceWaveId >= NextWaveId ||
                    !sourceIds.Add((wave.SourceWaveId, wave.FromEvolvedLane)) || wave.Cycle < 1 || wave.Cycle > Cycle ||
                    !ExpandedMosesStaff.ValidReturnShape(wave, Evolved) ||
                    (wave.FromEvolvedLane ? !NextEvolvedReturnAt.HasValue : !NextReturnAt.HasValue) ||
                    !ExpandedMosesStaff.ValidPosition(wave.Origin) || !ExpandedMosesStaff.ValidPosition(wave.Position) ||
                    !ExpandedMosesStaff.UnitDirection(wave.Direction) ||
                    !ExpandedMosesStaff.Finite(wave.StartedAt) || wave.StartedAt < 0 ||
                    !ExpandedMosesStaff.Finite(wave.LastStepAt) || wave.LastStepAt < wave.StartedAt ||
                    Math.Abs(wave.LastStepAt - now) > ExpandedMosesStaff.TimeEpsilon ||
                    !ExpandedMosesStaff.Finite(wave.Distance) || wave.Distance < 0 || wave.Distance >= wave.Range ||
                    Math.Abs(wave.Distance - (now - wave.StartedAt) * wave.Speed) > ExpandedMosesStaff.DistanceEpsilon ||
                    wave.HitIds == null || wave.HitIds.Count > ExpandedMosesStaff.MaximumLiveTargets ||
                    wave.HitIds.Any(id => id < 1) || wave.HitIds.Distinct().Count() != wave.HitIds.Count)
                    throw new ArgumentException("Invalid expanded staff returning wave");
            }
        }

        private void ValidateEliteKillLane(double now)
        {
            if (EliteKillLane is not { } lane) return;
            if (!NextEliteKillLaneAt.HasValue || lane.SourceWaveId < 1 || lane.SourceWaveId >= NextWaveId ||
                lane.Cycle < 1 || lane.Cycle > Cycle || lane.VictimId < 1 ||
                !ExpandedMosesStaff.ValidPosition(lane.Origin) || !ExpandedMosesStaff.UnitDirection(lane.Direction) ||
                !ExpandedMosesStaff.ValidPosition(lane.Origin + lane.Direction * (float)lane.Length) ||
                !ExpandedMosesStaff.Finite(lane.StartedAt) || lane.StartedAt < 0 || lane.StartedAt > now + ExpandedMosesStaff.TimeEpsilon ||
                !ExpandedMosesStaff.Finite(lane.EndsAt) || lane.EndsAt <= now + ExpandedMosesStaff.TimeEpsilon ||
                lane.EndsAt - lane.StartedAt < ExpandedMosesStaff.EliteKillLaneDuration - ExpandedMosesStaff.TimeEpsilon ||
                lane.EndsAt - lane.StartedAt > ExpandedMosesStaff.EliteKillLaneDuration * 1.75 + ExpandedMosesStaff.TimeEpsilon ||
                !ExpandedMosesStaff.Finite(lane.NextPulseAt) || lane.NextPulseAt <= now + ExpandedMosesStaff.TimeEpsilon ||
                lane.NextPulseAt > now + ExpandedMosesStaff.LaneInterval + ExpandedMosesStaff.TimeEpsilon ||
                Math.Abs((lane.NextPulseAt - lane.StartedAt) / ExpandedMosesStaff.LaneInterval -
                    Math.Round((lane.NextPulseAt - lane.StartedAt) / ExpandedMosesStaff.LaneInterval)) > ExpandedMosesStaff.TimeEpsilon ||
                !ExpandedMosesStaff.Finite(lane.Width) || lane.Width < ExpandedMosesStaff.EliteKillLaneWidth ||
                lane.Width > ExpandedMosesStaff.EliteKillLaneWidth * 2.5 ||
                !ExpandedMosesStaff.Finite(lane.Damage) || lane.Damage < ExpandedMosesStaff.EliteKillLaneDamageFactor ||
                lane.Damage > ExpandedMosesStaff.MaximumDamage * ExpandedMosesStaff.EliteKillLaneDamageFactor)
                throw new ArgumentException("Invalid expanded staff elite kill lane");
        }
    }

    public static class ExpandedMosesStaff
    {
        internal static readonly ExpandedWeaponProfile Profile = ExpandedWeaponRules.Get("moses_staff");
        internal static readonly int MaximumAmount = Profile.MaximumOriginalAmount;
        internal static readonly double MaximumCycle = Profile.AtLevel(1).Cycle;
        internal static readonly double MaximumDamage = Profile.AtLevel(8).Damage * 2.25;
        private static readonly double[] OriginalDamageByLevel = Enumerable.Range(1, 8)
            .Select(level => (double)Profile.AtLevel(level).Damage).ToArray();
        internal static readonly double MaximumWidth = Profile.AtLevel(1).Width * 2.5;
        internal static readonly double MaximumLaneLength = Profile.TravelDistance;
        private static readonly double MaximumRange = Profile.TravelDistance * 2.5;
        private static readonly double MinimumSpeed = Profile.AtLevel(1).Speed;
        private static readonly double MaximumSpeed = Profile.AtLevel(8).Speed * 1.75;
        private const double MinimumCycle = .15;
        internal const double TimeEpsilon = 1e-9;
        internal const double DistanceEpsilon = 1e-6;
        internal const double LaneInterval = .5;
        internal const double BaseLaneDuration = 2;
        internal const int MaximumLanes = 16;
        internal const int MaximumLiveTargets = 120;
        internal const int MaximumOriginalHits = 6210;
        internal static readonly int MaximumMovingWaves = MaximumAmount * ((int)Math.Ceiling(MaximumRange / MinimumSpeed / MinimumCycle) + 1);
        internal const double BaseLaneWidth = 1.2;
        internal const double LaneDamageFactor = .35;
        internal const byte ReturningWave = 1 << 0;
        internal const byte BendingWave = 1 << 1;
        internal const byte EndPillar = 1 << 2;
        internal const byte CloseDamage = 1 << 3;
        internal const byte EliteSplash = 1 << 4;
        internal const byte FourthCycleFirstWave = 1 << 5;
        internal const byte AllTechniques = ReturningWave | BendingWave | EndPillar | CloseDamage | EliteSplash | FourthCycleFirstWave;
        internal const int MaximumExtraGroups = 4;
        internal const byte ResidualLane = 1 << 0;
        internal const byte LongerResidualLane = 1 << 1;
        internal const byte ResidualLaneEdge = 1 << 2;
        internal const byte ResidualLaneCrossing = 1 << 3;
        internal const byte ResidualLaneEnd = 1 << 4;
        internal const byte AllResidualRanks = ResidualLane | LongerResidualLane | ResidualLaneEdge | ResidualLaneCrossing | ResidualLaneEnd;
        internal const double BaseResidualWidth = .7;
        internal const double BaseResidualDuration = 1.5;
        internal const double ResidualCooldown = 3;
        internal const double ReturnRange = 3;
        internal const double ReturnSpeed = 6;
        internal const double ReturnWidth = .6;
        internal const double ReturnDamageFactor = .35;
        internal const double TechniqueCooldown = 2;
        private const double TurnLimit = 12 * Math.PI / 180;
        internal const double EncirclingCooldown = 4;
        internal const double HurtAdvanceCooldown = 5;
        internal const int MaximumTrackedHostiles = BossDefinition.ChainCap;
        private const double HurtAdvanceSeconds = .25;
        internal const double EvolvedReturnDamageFactor = .50;
        internal const double EliteKillLaneLength = 2;
        internal const double EliteKillLaneWidth = .8;
        internal const double EliteKillLaneDuration = 1;
        internal const double EliteKillLaneDamageFactor = .30;
        internal const double EliteKillLaneCooldown = 5;

        // The caller owns live targets and damage. The result counts waves actually emitted.
        // The optional reservations let the world apply shared projectile and area caps.
        public static int Step(ExpandedMosesStaffState state, double now, Vector2 playerPosition, Vector2 facing, int level,
            bool evolved, double damage, double cycle, int amount, double widthBeforeForm, double range, double speed,
            Func<IReadOnlyDictionary<int, Vector2>> targets, IReadOnlyDictionary<int, Vector2> previousTargets,
            Func<ExpandedMosesStaffHit, bool> applyHit, Func<bool>? reserveOriginal = null, Func<bool>? reserveLane = null,
            ExpandedMosesStaffRankOptions rankOptions = default, Func<int, bool>? isElite = null,
            Func<double>? nextCycleDuration = null, Action<int>? recordOriginalSuppression = null,
            Action? recordSupportSuppression = null, Func<bool>? reserveSupportProjectile = null,
            Func<ExpandedMosesStaffHit, double?>? applyOriginalHit = null, double conditionalSupportShapeBonus = 0,
            double placedCircleBonus = 0, double? lastPlayerHurtAt = null,
            Func<IReadOnlyList<ExpandedMosesStaffHostileSweep>>? hostileSweeps = null,
            Func<bool>? canEraseHostile = null, Func<long, bool>? eraseHostile = null,
            Func<uint?, uint>? nextHostileEraseRandom = null, PermanentShotRules shotRules = default)
        {
            if (state == null) throw new ArgumentNullException(nameof(state));
            if (targets == null) throw new ArgumentNullException(nameof(targets));
            if (previousTargets == null) throw new ArgumentNullException(nameof(previousTargets));
            if (applyHit == null) throw new ArgumentNullException(nameof(applyHit));
            if ((rankOptions.PrioritizeElite || evolved && rankOptions.EliteKillLane ||
                (rankOptions.TechniqueRanks & (BendingWave | EliteSplash)) != 0) &&
                isElite == null) throw new ArgumentNullException(nameof(isElite));
            if (!Finite(now) || now < 0 || !ValidPosition(playerPosition) || !ValidPosition(facing) ||
                level < 1 || level > 8 || (evolved && level != 8) ||
                !Finite(damage) || damage < 1 || damage > MaximumDamage ||
                !Finite(cycle) || cycle < MinimumCycle || cycle > MaximumCycle ||
                amount < 1 || amount > MaximumAmount || !Finite(widthBeforeForm) || widthBeforeForm <= 0 ||
                widthBeforeForm > MaximumWidth || !ValidMotion(range, speed) ||
                !Finite(conditionalSupportShapeBonus) || conditionalSupportShapeBonus < 0 || conditionalSupportShapeBonus > 1.5 ||
                !Finite(placedCircleBonus) || placedCircleBonus < 0 || placedCircleBonus > 1.5 ||
                !ValidTechniqueCooldown(lastPlayerHurtAt, now, 0))
                throw new ArgumentException("Invalid expanded staff parameters");

            if (state.Evolved != evolved) state.ResetForForm(evolved, now);
            if (rankOptions.AdvanceOnHurt && lastPlayerHurtAt.HasValue)
                AdvanceOnPlayerHurt(state, lastPlayerHurtAt.Value, now);
            var supportShapeBonus = (rankOptions.WidenSupportShape ? .04 : 0) + conditionalSupportShapeBonus;
            var circleShapeBonus = supportShapeBonus + placedCircleBonus;
            if (state.EliteKillLane is { } endedKillLane && endedKillLane.EndsAt <= now + TimeEpsilon)
                state.EliteKillLane = null;
            AdvanceReturns(state, now, targets, previousTargets, applyHit);
            if (state.Waves.Count > 0)
            {
                foreach (var wave in state.Waves)
                {
                    if (!wave.IsMoving) continue;
                    AdvanceWave(state, wave, now, targets, previousTargets, applyHit, applyOriginalHit,
                        isElite, reserveLane, recordSupportSuppression, circleShapeBonus,
                        hostileSweeps, canEraseHostile, eraseHostile, nextHostileEraseRandom,
                        supportShapeBonus + (rankOptions.WidenDamageRegion ? .06 : 0), rankOptions.ResidualDurationBonus);
                    if (!wave.IsMoving)
                    {
                        wave.HostileEraseAttemptIds = null;
                        CompleteWave(state, wave, now, targets, applyHit, reserveLane, reserveSupportProjectile,
                            recordSupportSuppression, supportShapeBonus, circleShapeBonus);
                    }
                }
                PruneHostileAttempts(state.Waves, hostileSweeps);
                ResolveLanes(state.Waves, now, targets, previousTargets, applyHit,
                    wave => LaunchEvolvedReturn(state, wave, now, reserveSupportProjectile, recordSupportSuppression));
                ResolveResidualLanes(state, now, targets, previousTargets, applyHit,
                    reserveLane, recordSupportSuppression, circleShapeBonus);
                state.Waves.RemoveAll(wave => !wave.IsMoving && !wave.HasLane && !wave.HasResidualLane);
            }
            // New returns begin at the exact endpoint schedule and travel only any remaining part of this tick.
            AdvanceReturns(state, now, targets, previousTargets, applyHit);
            ResolveEliteKillLane(state, now, targets, previousTargets, applyHit);
            if (state.NextCycle > now + TimeEpsilon) return 0;

            state.Cycle = checked(state.Cycle + 1);
            if (rankOptions.AdvanceOnHurt) state.LastCycleStartedAt = state.NextCycle;
            state.NextCycle += nextCycleDuration?.Invoke() ?? cycle;
            var baseWidth = Profile.AtLevel(1).Width;
            var widthBonus = baseWidth * ((rankOptions.WidenWave ? .10 : 0) + shotRules.CollisionBonus);
            var formationWidth = widthBeforeForm + widthBonus;
            var originalBaseDamage = Profile.AtLevel(level).Damage;
            var forward = facing.LengthSquared() > 0 ? Vector2.Normalize(facing) : Vector2.UnitX;
            var spread = rankOptions.WidenFan ? 20 : Profile.SpreadDegrees;
            if (rankOptions.PrioritizeElite && TryFindEliteDirection(playerPosition, targets(), isElite!, out var aim))
            {
                if (aim != Vector2.Zero) forward = Vector2.Normalize(aim);
                spread = Math.Min(spread, 5);
            }
            var moving = state.ActiveWaveCount;
            var lanes = state.ActiveLaneCount;
            var emitted = 0;
            for (var index = 0; index < amount; index++)
            {
                if (moving + emitted >= MaximumMovingWaves || reserveOriginal?.Invoke() == false)
                {
                    recordOriginalSuppression?.Invoke(amount - index);
                    break;
                }
                var angle = (index - (amount - 1) * .5) * spread * Math.PI / 180;
                var direction = Rotate(forward, angle);
                var techniques = rankOptions.TechniqueRanks;
                var fourthFirst = (techniques & FourthCycleFirstWave) != 0 && index == 0 && state.Cycle % 4 == 0;
                if (!fourthFirst) techniques = (byte)(techniques & ~FourthCycleFirstWave);
                var swingWidth = formationWidth + (fourthFirst ? baseWidth * .15 : 0);
                var width = Math.Min(MaximumWidth, shotRules.LaunchCollisionDimension(baseWidth, swingWidth, emitted == 0) *
                    (evolved ? 1.2 : 1));
                var waveDamage = fourthFirst ? Math.Min(originalBaseDamage * 2.25, damage + originalBaseDamage * .10) : damage;
                var makesLane = evolved && lanes < MaximumLanes && (reserveLane?.Invoke() ?? true);
                if (makesLane) lanes++;
                else if (evolved) recordSupportSuppression?.Invoke();
                var wave = new ExpandedMosesStaffWaveState
                {
                    Id = state.NextWaveId,
                    Cycle = state.Cycle,
                    EmittedAt = now,
                    LastStepAt = now,
                    OriginX = playerPosition.X,
                    OriginY = playerPosition.Y,
                    DirectionX = direction.X,
                    DirectionY = direction.Y,
                    Damage = waveDamage,
                    OriginalBaseDamage = originalBaseDamage,
                    TechniqueRanks = techniques,
                    ErasesHostileProjectiles = rankOptions.EraseHostileProjectiles,
                    Width = width,
                    Range = range,
                    Speed = shotRules.LaunchSpeed(Profile.AtLevel(level).Speed, speed, emitted == 0),
                    SharedShotHitBonuses = shotRules.HitBonuses & PermanentShotHitBonuses.LongFlight,
                    MakesLane = makesLane,
                    ReturnsEvolvedLane = makesLane && rankOptions.EvolvedLaneReturn,
                    LeavesEliteKillLane = evolved && rankOptions.EliteKillLane,
                    IncreasedKnockback = rankOptions.IncreaseKnockback,
                    WidenedLane = makesLane && rankOptions.WidenLane,
                    EmpoweredLaneEdge = makesLane && rankOptions.EmpowerLaneEdge,
                    ExtendedLane = makesLane && rankOptions.ExtendLane,
                    WidenedSupportShape = makesLane && rankOptions.WidenSupportShape,
                    WiderDamageRegion = makesLane && rankOptions.WidenDamageRegion,
                    ResidualDurationBonus = makesLane ? rankOptions.ResidualDurationBonus : 0,
                };
                state.Waves.Add(wave);
                if (rankOptions.ResidualLaneRanks != 0)
                {
                    if (lanes < MaximumLanes && ReserveExtraGroup(state, reserveLane, recordSupportSuppression))
                    {
                        wave.ResidualLaneRanks = rankOptions.ResidualLaneRanks;
                        wave.ResidualLaneWidthBonus = Math.Min(1.5, supportShapeBonus + (rankOptions.WidenDamageRegion ? .06 : 0));
                        wave.ResidualLaneDurationBonus = evolved ? rankOptions.ResidualDurationBonus : rankOptions.NormalResidualDurationBonus;
                        lanes++;
                    }
                    else if (lanes >= MaximumLanes) recordSupportSuppression?.Invoke();
                }
                state.NextWaveId = checked(state.NextWaveId + 1);
                if (emitted == 0)
                    ApplyLaunchDefenses(state, wave, now, playerPosition, forward, rankOptions, targets, applyHit,
                        reserveLane, recordSupportSuppression, supportShapeBonus);
                emitted++;
            }
            return emitted;
        }

        private static bool TryFindEliteDirection(Vector2 origin, IReadOnlyDictionary<int, Vector2> targets,
            Func<int, bool> isElite, out Vector2 aim)
        {
            const double radiusSquared = 4 * 4;
            var bestDistance = radiusSquared;
            var bestId = int.MaxValue;
            var found = false;
            aim = Vector2.Zero;
            foreach (var (id, position) in targets)
            {
                var direction = position - origin;
                var distance = (double)direction.X * direction.X + (double)direction.Y * direction.Y;
                if (distance > bestDistance || (found && distance == bestDistance && id >= bestId) || !isElite(id))
                    continue;
                found = true;
                bestDistance = distance;
                bestId = id;
                aim = direction;
            }
            return found;
        }

        private static void AdvanceWave(ExpandedMosesStaffState state, ExpandedMosesStaffWaveState wave, double now,
            Func<IReadOnlyDictionary<int, Vector2>> targets, IReadOnlyDictionary<int, Vector2> previousTargets,
            Func<ExpandedMosesStaffHit, bool> applyHit, Func<ExpandedMosesStaffHit, double?>? applyOriginalHit,
            Func<int, bool>? isElite, Func<bool>? reserveLane, Action? recordSupportSuppression, double areaBonus,
            Func<IReadOnlyList<ExpandedMosesStaffHostileSweep>>? hostileSweeps, Func<bool>? canEraseHostile,
            Func<long, bool>? eraseHostile, Func<uint?, uint>? nextHostileEraseRandom,
            double killLaneWidthBonus, double killLaneDurationBonus)
        {
            var duration = now - wave.LastStepAt;
            if (!wave.IsMoving || duration <= TimeEpsilon) return;
            wave.LastStepAt = now;
            var from = wave.Position;
            if ((wave.TechniqueRanks & BendingWave) != 0 && !wave.TurnAtDistance.HasValue && isElite != null)
                TurnWave(wave, from, targets(), isElite);
            var previousDistance = wave.Distance;
            wave.Distance = Math.Min(wave.Range, (now - wave.EmittedAt) * wave.Speed);
            var to = wave.Position;
            var fractionOfStep = Math.Min(1, (wave.Distance - previousDistance) / (wave.Speed * duration));
            if (wave.ErasesHostileProjectiles && hostileSweeps != null && eraseHostile != null && nextHostileEraseRandom != null)
                TryEraseHostile(state, wave, now, from, to, fractionOfStep, hostileSweeps, canEraseHostile,
                    eraseHostile, nextHostileEraseRandom);
            var live = targets();
            // Preview checkpoints lacked a total. Recover their available ledger before pruning dead actors.
            wave.OriginalHitCount = Math.Max(wave.OriginalHitCount, wave.HitIds.Count);
            wave.HitIds.RemoveAll(id => !live.ContainsKey(id));
            foreach (var id in live.Keys.OrderBy(id => id).ToArray())
            {
                if (wave.HitIds.Contains(id) || !targets().TryGetValue(id, out var current)) continue;
                var previous = previousTargets.TryGetValue(id, out var old) ? old : current;
                var endTarget = Vector2.Lerp(previous, current, (float)fractionOfStep);
                if (!SweptContact(from - previous, to - endTarget, wave.Width / 2, out var contact)) continue;
                var close = (wave.TechniqueRanks & CloseDamage) != 0 &&
                    Vector2.DistanceSquared(current, wave.Origin) <= 4;
                var damage = close ? Math.Min(wave.OriginalBaseDamage * 2.25, wave.Damage + wave.OriginalBaseDamage * .08) : wave.Damage;
                var travelledAtContact = previousDistance + (wave.Distance - previousDistance) * contact;
                damage = PermanentShotRules.HitDamage(wave.SharedShotHitBonuses, wave.OriginalBaseDamage,
                    damage, null, travelledAtContact);
                var canSplash = (wave.TechniqueRanks & EliteSplash) != 0 && IsReady(state.NextEliteSplashAt, now);
                var canLeaveLane = wave.LeavesEliteKillLane && IsReady(state.NextEliteKillLaneAt, now);
                var elite = (canSplash || canLeaveLane) && isElite?.Invoke(id) == true;
                var hit = new ExpandedMosesStaffHit(ExpandedMosesStaffHitKind.Wave, id, current,
                    Vector2.Lerp(from, to, contact), wave, waveDamage: damage);
                var accepted = applyOriginalHit != null ? applyOriginalHit(hit) : applyHit(hit) ? damage : (double?)null;
                if (!accepted.HasValue) continue;
                wave.HitIds.Add(id);
                wave.OriginalHitCount++;
                if (canLeaveLane && elite && !targets().ContainsKey(id))
                    SpawnEliteKillLane(state, wave, now, id, current, accepted.Value, killLaneWidthBonus,
                        killLaneDurationBonus, reserveLane, recordSupportSuppression);
                if (canSplash && elite && ReserveTechnique(reserveLane, recordSupportSuppression))
                {
                    HitCircle(wave, current, 1.2 * Math.Min(2.5, 1 + areaBonus), accepted.Value * .25,
                        ExpandedMosesStaffHitKind.EliteSplash, targets, applyHit);
                    state.NextEliteSplashAt = now + TechniqueCooldown;
                }
            }
            // One lane starts on the first tick that leaves a trace. Its lifetime belongs
            // to that lane, while its length grows behind the wave up to seven units.
            if (wave.MakesLane && !wave.LaneStartedAt.HasValue)
                wave.LaneStartedAt = now;
            if (wave.ResidualLaneRanks != 0 && !wave.ResidualLaneStartedAt.HasValue)
                wave.ResidualLaneStartedAt = now;
        }

        public static bool IsPlayerInLane(ExpandedMosesStaffState? state, Vector2 position)
        {
            if (state == null) return false;
            foreach (var wave in state.Waves)
            {
                if (wave.HasLane && wave.LaneStartedAt.HasValue &&
                    CrossesPath(position, position, wave, wave.LaneLength, wave.LaneWidth / 2, 0, out _))
                    return true;
                if (wave.HasResidualLane && wave.ResidualLaneStartedAt.HasValue &&
                    CrossesPath(position, position, wave, wave.ResidualLaneLength, wave.ResidualLaneWidth / 2, 0, out _))
                    return true;
            }
            return false;
        }

        private static void AdvanceOnPlayerHurt(ExpandedMosesStaffState state, double hurt, double now)
        {
            if (state.LastPlayerHurtObservedAt.HasValue && hurt <= state.LastPlayerHurtObservedAt.Value + TimeEpsilon) return;
            state.LastPlayerHurtObservedAt = hurt;
            if (!IsReady(state.NextHurtAdvanceAt, now) || state.NextCycle <= now + TimeEpsilon || state.Cycle == 0) return;
            var previousStart = state.LastCycleStartedAt ?? state.Waves.LastOrDefault()?.EmittedAt;
            if (!previousStart.HasValue) return;
            var advanced = Math.Max(now, Math.Max(previousStart.Value + MinimumCycle, state.NextCycle - HurtAdvanceSeconds));
            if (advanced >= state.NextCycle - TimeEpsilon) return;
            state.NextCycle = advanced;
            state.NextHurtAdvanceAt = now + HurtAdvanceCooldown;
        }

        private static void ApplyLaunchDefenses(ExpandedMosesStaffState state, ExpandedMosesStaffWaveState wave,
            double now, Vector2 origin, Vector2 forward, ExpandedMosesStaffRankOptions ranks,
            Func<IReadOnlyDictionary<int, Vector2>> targets, Func<ExpandedMosesStaffHit, bool> applyHit,
            Func<bool>? reserveArea, Action? recordSuppression, double supportShapeBonus)
        {
            var encircle = ranks.EncirclingWave && IsReady(state.NextEncirclingWaveAt, now) &&
                targets().Count(target => Vector2.DistanceSquared(target.Value, origin) <= 4 + DistanceEpsilon) >= 6;
            var shape = Math.Min(2.5, 1 + supportShapeBonus);
            if (ranks.WaterScreen && IsReady(state.NextWaterScreenAt, now) && ReserveExtraGroup(state, reserveArea, recordSuppression))
            {
                var prospective = targets().Keys.OrderBy(id => id).ToArray();
                foreach (var id in prospective)
                {
                    if (!targets().TryGetValue(id, out var position)) continue;
                    var offset = position - origin;
                    // World coordinates are floats. Allow their rounding at the inclusive 30-degree boundary.
                    if (!HitGeometry.InArc(offset.X, offset.Y, forward.X, forward.Y, 1.2 * shape + DistanceEpsilon, 60.00001))
                        continue;
                    applyHit(new ExpandedMosesStaffHit(ExpandedMosesStaffHitKind.WaterScreen, id, position, origin,
                        forward, wave.Damage * .25, wave.Id, wave.Cycle));
                }
                state.NextWaterScreenAt = now + TechniqueCooldown;
            }
            if (!encircle || !ReserveExtraGroup(state, reserveArea, recordSuppression)) return;
            HitCircle(wave, origin, 2 * shape, wave.Damage * .30, ExpandedMosesStaffHitKind.EncirclingWave, targets, applyHit);
            state.NextEncirclingWaveAt = now + EncirclingCooldown;
        }

        private static void TryEraseHostile(ExpandedMosesStaffState state, ExpandedMosesStaffWaveState wave,
            double now, Vector2 from, Vector2 to, double advanceFraction,
            Func<IReadOnlyList<ExpandedMosesStaffHostileSweep>> hostileSweeps, Func<bool>? canErase,
            Func<long, bool> erase, Func<uint?, uint> nextRandom)
        {
            if (!IsReady(state.NextHostileEraseAt, now) || canErase?.Invoke() == false) return;
            var contacts = new List<(long Id, float Time)>();
            foreach (var hostile in hostileSweeps())
            {
                if (wave.HostileEraseAttemptIds?.Contains(hostile.Id) == true ||
                    !SweptContact(from - hostile.From, to - Vector2.Lerp(hostile.From, hostile.To, (float)advanceFraction),
                        wave.Width / 2 + hostile.Radius, out var time)) continue;
                contacts.Add((hostile.Id, time));
            }
            foreach (var hostile in contacts.OrderBy(contact => contact.Time).ThenBy(contact => contact.Id))
            {
                wave.HostileEraseAttemptIds ??= new List<long>();
                wave.HostileEraseAttemptIds.Add(hostile.Id);
                var current = nextRandom(state.HostileEraseRandomState);
                if (current == 0) throw new InvalidOperationException("Invalid staff hostile erase RNG state");
                state.HostileEraseRandomState = current;
                if (current / 4294967296d >= .20 || !erase(hostile.Id)) continue;
                state.NextHostileEraseAt = now + TechniqueCooldown;
                break;
            }
        }

        private static void PruneHostileAttempts(IEnumerable<ExpandedMosesStaffWaveState> waves,
            Func<IReadOnlyList<ExpandedMosesStaffHostileSweep>>? hostileSweeps)
        {
            if (hostileSweeps == null || !waves.Any(wave => wave.HostileEraseAttemptIds != null)) return;
            var live = hostileSweeps().Select(sweep => sweep.Id).ToHashSet();
            foreach (var wave in waves)
            {
                wave.HostileEraseAttemptIds?.RemoveAll(id => !live.Contains(id));
                if (wave.HostileEraseAttemptIds?.Count == 0) wave.HostileEraseAttemptIds = null;
            }
        }

        private static void TurnWave(ExpandedMosesStaffWaveState wave, Vector2 from,
            IReadOnlyDictionary<int, Vector2> targets, Func<int, bool> isElite)
        {
            var range = wave.Range - wave.Distance;
            var target = targets.Select(candidate => (candidate.Key, Offset: candidate.Value - from))
                .Where(candidate => candidate.Offset.LengthSquared() > 0 &&
                    candidate.Offset.LengthSquared() <= range * range && !isElite(candidate.Key))
                .OrderBy(candidate => candidate.Offset.LengthSquared()).ThenBy(candidate => candidate.Key).FirstOrDefault();
            if (target.Key == 0) return;
            var direction = wave.Direction;
            var angle = Math.Atan2((double)direction.X * target.Offset.Y - (double)direction.Y * target.Offset.X,
                Vector2.Dot(direction, target.Offset));
            var turned = Rotate(direction, Math.Max(-TurnLimit, Math.Min(TurnLimit, angle)));
            wave.TurnAtDistance = wave.Distance;
            wave.TurnDirectionX = turned.X;
            wave.TurnDirectionY = turned.Y;
        }

        private static void CompleteWave(ExpandedMosesStaffState state, ExpandedMosesStaffWaveState wave, double now,
            Func<IReadOnlyDictionary<int, Vector2>> targets, Func<ExpandedMosesStaffHit, bool> applyHit,
            Func<bool>? reserveLane, Func<bool>? reserveSupportProjectile, Action? recordSupportSuppression,
            double supportShapeBonus, double circleShapeBonus)
        {
            if ((wave.TechniqueRanks & ReturningWave) != 0 && IsReady(state.NextReturnAt, now) &&
                ReserveExtraGroup(state, reserveSupportProjectile, recordSupportSuppression))
            {
                var position = wave.Position;
                var direction = -wave.TravelDirection;
                var startedAt = wave.EmittedAt + wave.Range / wave.Speed;
                SpawnReturn(state, wave, position, direction, startedAt,
                    ReturnWidth * Math.Min(2.5, 1 + supportShapeBonus), wave.Damage * ReturnDamageFactor);
                state.NextReturnAt = now + TechniqueCooldown;
            }
            if ((wave.TechniqueRanks & EndPillar) == 0 || !IsReady(state.NextEndPillarAt, now) ||
                !ReserveTechnique(reserveLane, recordSupportSuppression)) return;
            HitCircle(wave, wave.Position, Math.Min(2.5, 1 + circleShapeBonus), wave.Damage * .35,
                ExpandedMosesStaffHitKind.EndPillar, targets, applyHit);
            state.NextEndPillarAt = now + TechniqueCooldown;
        }

        private static void LaunchEvolvedReturn(ExpandedMosesStaffState state, ExpandedMosesStaffWaveState wave,
            double now, Func<bool>? reserveSupportProjectile, Action? recordSuppression)
        {
            if (!wave.ReturnsEvolvedLane || !IsReady(state.NextEvolvedReturnAt, now)) return;
            var end = wave.PositionAt(Math.Min(wave.Distance, MaximumLaneLength));
            var offset = wave.Origin - end;
            var length = offset.Length();
            if (length <= DistanceEpsilon || !ReserveExtraGroup(state, reserveSupportProjectile, recordSuppression)) return;
            SpawnReturn(state, wave, end, offset / length, wave.LaneStartedAt!.Value + wave.LaneDuration,
                wave.Width, wave.Damage * EvolvedReturnDamageFactor, length, wave.Speed);
            state.NextEvolvedReturnAt = now + ResidualCooldown;
        }

        private static void SpawnReturn(ExpandedMosesStaffState state, ExpandedMosesStaffWaveState wave,
            Vector2 position, Vector2 direction, double startedAt, double width, double damage,
            double? range = null, double? speed = null)
        {
            state.ReturnWaves ??= new List<ExpandedMosesStaffReturnState>();
            state.ReturnWaves.Add(new ExpandedMosesStaffReturnState
            {
                SourceWaveId = wave.Id, Cycle = wave.Cycle, X = position.X, Y = position.Y,
                DirectionX = direction.X, DirectionY = direction.Y, StartedAt = startedAt, LastStepAt = startedAt,
                Width = width, Damage = damage, FromEvolvedLane = range.HasValue, TravelRange = range, TravelSpeed = speed,
            });
        }

        internal static bool ValidReturnShape(ExpandedMosesStaffReturnState wave, bool evolved)
        {
            if (!Finite(wave.Damage) || !Finite(wave.Width)) return false;
            if (wave.FromEvolvedLane)
                return evolved && wave.TravelRange.HasValue && wave.TravelSpeed.HasValue && ValidMotion(wave.Range, wave.Speed) &&
                    wave.Range <= MaximumLaneLength + DistanceEpsilon && wave.Width > 0 && wave.Width <= MaximumWidth &&
                    wave.Damage >= EvolvedReturnDamageFactor && wave.Damage <= MaximumDamage * EvolvedReturnDamageFactor;
            return !wave.TravelRange.HasValue && !wave.TravelSpeed.HasValue && wave.Width >= ReturnWidth &&
                wave.Width <= ReturnWidth * 2.5 && wave.Damage >= ReturnDamageFactor &&
                wave.Damage <= MaximumDamage * ReturnDamageFactor;
        }

        private static void SpawnEliteKillLane(ExpandedMosesStaffState state, ExpandedMosesStaffWaveState wave, double now,
            int victim, Vector2 position, double damage, double widthBonus, double durationBonus, Func<bool>? reserveArea,
            Action? recordSuppression)
        {
            if (state.ActiveLaneCount >= MaximumLanes || !ValidPosition(position + wave.TravelDirection * (float)EliteKillLaneLength))
            {
                recordSuppression?.Invoke();
                return;
            }
            if (!ReserveExtraGroup(state, reserveArea, recordSuppression)) return;
            state.EliteKillLane = new ExpandedMosesStaffEliteKillLaneState
            {
                SourceWaveId = wave.Id, Cycle = wave.Cycle, VictimId = victim, StartedAt = now,
                EndsAt = now + EliteKillLaneDuration * Math.Min(1.75, 1 + durationBonus), NextPulseAt = now,
                X = position.X, Y = position.Y, DirectionX = wave.TravelDirection.X, DirectionY = wave.TravelDirection.Y,
                Width = EliteKillLaneWidth * Math.Min(2.5, 1 + widthBonus), Damage = damage * EliteKillLaneDamageFactor,
            };
            state.NextEliteKillLaneAt = now + EliteKillLaneCooldown;
        }

        private static void ResolveEliteKillLane(ExpandedMosesStaffState state, double now,
            Func<IReadOnlyDictionary<int, Vector2>> targets, IReadOnlyDictionary<int, Vector2> previousTargets,
            Func<ExpandedMosesStaffHit, bool> applyHit)
        {
            if (state.EliteKillLane is not { } lane || lane.NextPulseAt > now + TimeEpsilon) return;
            do { lane.NextPulseAt += LaneInterval; } while (lane.NextPulseAt <= now + TimeEpsilon);
            foreach (var id in targets().Keys.OrderBy(id => id).ToArray())
            {
                if (!targets().TryGetValue(id, out var position)) continue;
                var previous = previousTargets.TryGetValue(id, out var old) ? old : position;
                if (!CrossesLaneSegment(previous, position, lane.Origin, lane.Direction, lane.Length, lane.Width / 2, 0, out _)) continue;
                applyHit(new ExpandedMosesStaffHit(ExpandedMosesStaffHitKind.EliteKillLane, id, position, lane.Origin,
                    lane.Direction, lane.Damage, lane.SourceWaveId, lane.Cycle));
            }
        }

        private static void AdvanceReturns(ExpandedMosesStaffState state, double now,
            Func<IReadOnlyDictionary<int, Vector2>> targets, IReadOnlyDictionary<int, Vector2> previousTargets,
            Func<ExpandedMosesStaffHit, bool> applyHit)
        {
            if (state.ReturnWaves == null) return;
            foreach (var wave in state.ReturnWaves.OrderBy(wave => wave.SourceWaveId).ThenBy(wave => wave.FromEvolvedLane))
            {
                if (now <= wave.LastStepAt + TimeEpsilon) continue;
                var from = wave.Position;
                var available = now - wave.LastStepAt;
                var moved = Math.Min(wave.Range - wave.Distance, available * wave.Speed);
                var fractionOfStep = moved / (available * wave.Speed);
                wave.Distance = Math.Min(wave.Range, (now - wave.StartedAt) * wave.Speed);
                wave.LastStepAt = now;
                var to = wave.Position;
                var live = targets();
                wave.HitIds.RemoveAll(id => !live.ContainsKey(id));
                foreach (var id in live.Keys.OrderBy(id => id).ToArray())
                {
                    if (wave.HitIds.Contains(id) || !targets().TryGetValue(id, out var current)) continue;
                    var previous = previousTargets.TryGetValue(id, out var old) ? old : current;
                    var elapsedFraction = Math.Min(1, available / BattleWorld.FixedStep);
                    var startTarget = Vector2.Lerp(previous, current, 1 - (float)elapsedFraction);
                    var endTarget = Vector2.Lerp(startTarget, current, (float)fractionOfStep);
                    if (!SweptContact(from - startTarget, to - endTarget, wave.Width / 2, out var contact)) continue;
                    var kind = wave.FromEvolvedLane ? ExpandedMosesStaffHitKind.EvolvedReturnWave : ExpandedMosesStaffHitKind.ReturnWave;
                    if (applyHit(new ExpandedMosesStaffHit(kind, id, current,
                        Vector2.Lerp(from, to, contact), wave.Direction, wave.Damage, wave.SourceWaveId, wave.Cycle)))
                        wave.HitIds.Add(id);
                }
            }
            state.ReturnWaves.RemoveAll(wave => wave.Distance >= wave.Range);
            if (state.ReturnWaves.Count == 0) state.ReturnWaves = null;
        }

        private static void HitCircle(ExpandedMosesStaffWaveState wave, Vector2 center, double radius, double damage,
            ExpandedMosesStaffHitKind kind, Func<IReadOnlyDictionary<int, Vector2>> targets,
            Func<ExpandedMosesStaffHit, bool> applyHit)
        {
            foreach (var id in targets().Where(target => Vector2.DistanceSquared(target.Value, center) <= radius * radius + DistanceEpsilon)
                .Select(target => target.Key).OrderBy(id => id).ToArray())
            {
                if (!targets().TryGetValue(id, out var position) ||
                    Vector2.DistanceSquared(position, center) > radius * radius + DistanceEpsilon) continue;
                applyHit(new ExpandedMosesStaffHit(kind, id, position, center, wave.TravelDirection, damage, wave.Id, wave.Cycle));
            }
        }

        private static bool ReserveTechnique(Func<bool>? reserve, Action? suppressed)
        {
            if (reserve?.Invoke() != false) return true;
            suppressed?.Invoke();
            return false;
        }

        private static bool IsReady(double? cooldown, double now) => (cooldown ?? 0) <= now + TimeEpsilon;
        internal static bool ValidTechniqueCooldown(double? cooldown, double now, double duration = TechniqueCooldown) =>
            !cooldown.HasValue || Finite(cooldown.Value) && cooldown.Value >= 0 &&
            cooldown.Value <= now + duration + TimeEpsilon;
        internal static bool ValidTechniques(ExpandedMosesStaffWaveState wave)
        {
            if ((wave.TechniqueRanks & ~AllTechniques) != 0 || wave.TechniqueRanks != 0 && wave.OriginalBaseDamage <= 0 ||
                (wave.TechniqueRanks & FourthCycleFirstWave) != 0 && wave.Cycle % 4 != 0)
                return false;
            if (!wave.TurnAtDistance.HasValue) return !wave.TurnDirectionX.HasValue && !wave.TurnDirectionY.HasValue;
            return (wave.TechniqueRanks & BendingWave) != 0 && Finite(wave.TurnAtDistance.Value) &&
                wave.TurnAtDistance.Value >= 0 && wave.TurnAtDistance.Value < wave.Distance &&
                wave.TurnDirectionX.HasValue && wave.TurnDirectionY.HasValue && UnitDirection(wave.TravelDirection) &&
                Vector2.Dot(wave.Direction, wave.TravelDirection) >= Math.Cos(TurnLimit) - DistanceEpsilon;
        }

        internal static bool ValidResidualLane(ExpandedMosesStaffWaveState wave, double now)
        {
            var ranks = wave.ResidualLaneRanks;
            if (ranks == 0)
                return !wave.ResidualLaneStartedAt.HasValue && wave.ResidualLaneTicks == 0 &&
                    wave.ResidualLaneWidthBonus == 0 && wave.ResidualLaneDurationBonus == 0 &&
                    wave.ResidualLaneEdgeIds == null && wave.ResidualLaneOverlapIds == null;
            if ((ranks & ResidualLane) == 0 || (ranks & ~AllResidualRanks) != 0 ||
                !Finite(wave.ResidualLaneWidthBonus) || wave.ResidualLaneWidthBonus < 0 || wave.ResidualLaneWidthBonus > 1.5 ||
                !ExpandedWeaponRules.ValidResidualDurationBonus(wave.ResidualLaneDurationBonus) ||
                wave.ResidualLaneTicks < 0 || wave.ResidualLaneTicks > wave.ExpiredResidualLane ||
                wave.ResidualLaneEdgeIds != null && ((ranks & ResidualLaneEdge) == 0 ||
                    !wave.HasResidualLane || wave.ResidualLaneEdgeIds.Count == 0 || wave.ResidualLaneEdgeIds.Count > MaximumLiveTargets ||
                    wave.ResidualLaneEdgeIds.Any(id => id <= 0) ||
                    wave.ResidualLaneEdgeIds.Distinct().Count() != wave.ResidualLaneEdgeIds.Count) ||
                wave.ResidualLaneOverlapIds != null && ((ranks & ResidualLaneCrossing) == 0 ||
                    !wave.HasResidualLane || wave.ResidualLaneOverlapIds.Count == 0 ||
                    wave.ResidualLaneOverlapIds.Count >= MaximumExtraGroups ||
                    wave.ResidualLaneOverlapIds.Any(id => id <= 0 || id >= wave.Id) ||
                    wave.ResidualLaneOverlapIds.Distinct().Count() != wave.ResidualLaneOverlapIds.Count))
                return false;
            if (wave.ResidualLaneStartedAt is not double start) return wave.Distance == 0 && wave.ResidualLaneTicks == 0 &&
                wave.ResidualLaneEdgeIds == null && wave.ResidualLaneOverlapIds == null;
            var elapsed = now - start + TimeEpsilon;
            var expectedTicks = elapsed >= wave.ResidualLaneDuration ? wave.ExpiredResidualLane :
                1 + (int)Math.Floor(elapsed / LaneInterval);
            return Finite(start) && start > wave.EmittedAt && start <= wave.LastStepAt && expectedTicks == wave.ResidualLaneTicks;
        }

        private static void ResolveResidualLanes(ExpandedMosesStaffState state, double now,
            Func<IReadOnlyDictionary<int, Vector2>> targets, IReadOnlyDictionary<int, Vector2> previousTargets,
            Func<ExpandedMosesStaffHit, bool> applyHit, Func<bool>? reserveArea, Action? recordSuppression,
            double circleShapeBonus)
        {
            var active = state.Waves.Where(wave => wave.HasResidualLane && wave.ResidualLaneStartedAt.HasValue).ToArray();
            if (active.Length == 0) return;
            var due = new List<ExpandedMosesStaffWaveState>();
            var ended = new List<ExpandedMosesStaffWaveState>();
            foreach (var wave in active)
            {
                var start = wave.ResidualLaneStartedAt!.Value;
                if (start + wave.ResidualLaneDuration <= now + TimeEpsilon)
                {
                    wave.ResidualLaneTicks = wave.ExpiredResidualLane;
                    wave.ResidualLaneEdgeIds = null;
                    wave.ResidualLaneOverlapIds = null;
                    ended.Add(wave);
                    continue;
                }
                var next = start + wave.ResidualLaneTicks * LaneInterval;
                if (wave.ResidualLaneTicks == wave.ResidualLaneTickCount || next > now + TimeEpsilon) continue;
                do { wave.ResidualLaneTicks++; }
                while (wave.ResidualLaneTicks < wave.ResidualLaneTickCount && start + wave.ResidualLaneTicks * LaneInterval <= now + TimeEpsilon);
                due.Add(wave);
            }
            foreach (var wave in ended)
            {
                if ((wave.ResidualLaneRanks & ResidualLaneEnd) == 0 || !IsReady(state.NextResidualEndAt, now) ||
                    !ReserveExtraGroup(state, reserveArea, recordSuppression)) continue;
                HitCircle(wave, wave.PositionAt(wave.Distance / 2), 1.2 * Math.Min(2.5, 1 + circleShapeBonus),
                    wave.Damage * .25, ExpandedMosesStaffHitKind.ResidualEnd, targets, applyHit);
                state.NextResidualEndAt = now + ResidualCooldown;
            }
            active = active.Where(wave => wave.HasResidualLane).ToArray();
            if (active.Length == 0) return;
            ApplyResidualEdges(active, targets, previousTargets, applyHit);
            ApplyFirstResidualCrosses(state, active, now, targets, applyHit, reserveArea, recordSuppression, circleShapeBonus);
            ApplyResidualPulses(due, targets, previousTargets, applyHit);
        }

        private static void ApplyResidualEdges(IReadOnlyList<ExpandedMosesStaffWaveState> lanes,
            Func<IReadOnlyDictionary<int, Vector2>> targets, IReadOnlyDictionary<int, Vector2> previousTargets,
            Func<ExpandedMosesStaffHit, bool> applyHit)
        {
            foreach (var wave in lanes)
            {
                if ((wave.ResidualLaneRanks & ResidualLaneEdge) == 0) continue;
                var live = targets();
                wave.ResidualLaneEdgeIds?.RemoveAll(id => !live.ContainsKey(id));
                foreach (var id in live.Keys.OrderBy(id => id).ToArray())
                {
                    if (wave.ResidualLaneEdgeIds?.Contains(id) == true || !targets().TryGetValue(id, out var position)) continue;
                    var previous = previousTargets.TryGetValue(id, out var old) ? old : position;
                    if (!CrossesPath(previous, position, wave, wave.ResidualLaneLength, wave.ResidualLaneWidth / 2, .80, out var outer) ||
                        !outer || !applyHit(new ExpandedMosesStaffHit(ExpandedMosesStaffHitKind.ResidualEdge, id, position,
                            wave.Origin, wave.TravelDirection, wave.Damage * .20, wave.Id, wave.Cycle))) continue;
                    wave.ResidualLaneEdgeIds ??= new List<int>();
                    wave.ResidualLaneEdgeIds.Add(id);
                }
                if (wave.ResidualLaneEdgeIds?.Count == 0) wave.ResidualLaneEdgeIds = null;
            }
        }

        private static void ApplyFirstResidualCrosses(ExpandedMosesStaffState state, ExpandedMosesStaffWaveState[] lanes,
            double now, Func<IReadOnlyDictionary<int, Vector2>> targets, Func<ExpandedMosesStaffHit, bool> applyHit,
            Func<bool>? reserveArea, Action? recordSuppression, double circleShapeBonus)
        {
            for (var index = 0; index < lanes.Length; index++)
            {
                var newer = lanes[index];
                if ((newer.ResidualLaneRanks & ResidualLaneCrossing) == 0) continue;
                newer.ResidualLaneOverlapIds?.RemoveAll(id => !lanes.Any(lane => lane.Id == id));
                for (var previous = 0; previous < index; previous++)
                {
                    var older = lanes[previous];
                    if (newer.ResidualLaneOverlapIds?.Contains(older.Id) == true ||
                        !TryResidualCrossing(older, newer, out var intersection)) continue;
                    newer.ResidualLaneOverlapIds ??= new List<long>();
                    newer.ResidualLaneOverlapIds.Add(older.Id);
                    if (!IsReady(state.NextResidualCrossAt, now) || !ReserveExtraGroup(state, reserveArea, recordSuppression)) continue;
                    HitCircle(newer, intersection, Math.Min(2.5, 1 + circleShapeBonus), newer.Damage * .30,
                        ExpandedMosesStaffHitKind.ResidualCross, targets, applyHit);
                    state.NextResidualCrossAt = now + ResidualCooldown;
                }
                if (newer.ResidualLaneOverlapIds?.Count == 0) newer.ResidualLaneOverlapIds = null;
            }
        }

        private static bool ReserveExtraGroup(ExpandedMosesStaffState state, Func<bool>? reserve, Action? suppressed) =>
            ReserveTechnique(() => state.ActiveExtraGroups < MaximumExtraGroups && reserve?.Invoke() != false, suppressed);

        private static bool TryResidualCrossing(ExpandedMosesStaffWaveState older, ExpandedMosesStaffWaveState newer,
            out Vector2 intersection)
        {
            var olderHalfWidth = older.ResidualLaneWidth / 2;
            var newerHalfWidth = newer.ResidualLaneWidth / 2;
            foreach (var (from, to) in PathSegments(older))
                foreach (var (otherFrom, otherTo) in PathSegments(newer))
                {
                    if (TryCrossSegments(from, to, otherFrom, otherTo, out intersection)) return true;
                    var firstLength = Vector2.Distance(from, to);
                    var secondLength = Vector2.Distance(otherFrom, otherTo);
                    if (firstLength <= DistanceEpsilon || secondLength <= DistanceEpsilon) continue;
                    var firstDirection = (to - from) / firstLength;
                    var secondDirection = (otherTo - otherFrom) / secondLength;
                    var first = StripCorners(from, to, firstDirection, olderHalfWidth);
                    var second = StripCorners(otherFrom, otherTo, secondDirection, newerHalfWidth);
                    foreach (var point in first)
                        if (CrossesLaneSegment(point, point, otherFrom, secondDirection, secondLength, newerHalfWidth, 0, out _))
                        {
                            intersection = point;
                            return true;
                        }
                    foreach (var point in second)
                        if (CrossesLaneSegment(point, point, from, firstDirection, firstLength, olderHalfWidth, 0, out _))
                        {
                            intersection = point;
                            return true;
                        }
                    for (var i = 0; i < first.Length; i++)
                        for (var j = 0; j < second.Length; j++)
                            if (TryCrossSegments(first[i], first[(i + 1) % first.Length],
                                second[j], second[(j + 1) % second.Length], out intersection)) return true;
                }
            intersection = default;
            return false;
        }

        private static Vector2[] StripCorners(Vector2 from, Vector2 to, Vector2 direction, double halfWidth)
        {
            var normal = new Vector2(-direction.Y, direction.X) * (float)halfWidth;
            return new[] { from + normal, to + normal, to - normal, from - normal };
        }

        private static IEnumerable<(Vector2 From, Vector2 To)> PathSegments(ExpandedMosesStaffWaveState wave)
        {
            var length = wave.ResidualLaneLength;
            if (wave.TurnAtDistance is double turn && turn > 0 && turn < length)
            {
                yield return (wave.Origin, wave.TurnPosition);
                yield return (wave.TurnPosition, wave.PositionAt(length));
            }
            else yield return (wave.Origin, wave.PositionAt(length));
        }

        private static bool TryCrossSegments(Vector2 from, Vector2 to, Vector2 otherFrom, Vector2 otherTo,
            out Vector2 intersection)
        {
            intersection = default;
            var direction = to - from;
            var otherDirection = otherTo - otherFrom;
            var denominator = Cross(direction, otherDirection);
            if (Math.Abs(denominator) <= DistanceEpsilon) return false;
            var offset = otherFrom - from;
            var first = Cross(offset, otherDirection) / denominator;
            var second = Cross(offset, direction) / denominator;
            if (first < 0 || first > 1 || second < 0 || second > 1) return false;
            intersection = from + direction * (float)first;
            return true;
        }

        private static double Cross(Vector2 first, Vector2 second) =>
            (double)first.X * second.Y - (double)first.Y * second.X;
        private static void ApplyResidualPulses(IEnumerable<ExpandedMosesStaffWaveState> lanes,
            Func<IReadOnlyDictionary<int, Vector2>> targets, IReadOnlyDictionary<int, Vector2> previousTargets,
            Func<ExpandedMosesStaffHit, bool> applyHit)
        {
            var winners = new Dictionary<int, ExpandedMosesStaffWaveState>();
            var live = targets();
            foreach (var wave in lanes)
                foreach (var target in live)
                {
                    var previous = previousTargets.TryGetValue(target.Key, out var old) ? old : target.Value;
                    if (!CrossesPath(previous, target.Value, wave, wave.ResidualLaneLength, wave.ResidualLaneWidth / 2, 0, out _))
                        continue;
                    if (!winners.TryGetValue(target.Key, out var current) || wave.Damage > current.Damage)
                        winners[target.Key] = wave;
                }
            foreach (var (id, wave) in winners.OrderBy(entry => entry.Value.Id).ThenBy(entry => entry.Key))
            {
                if (!targets().TryGetValue(id, out var position)) continue;
                applyHit(new ExpandedMosesStaffHit(ExpandedMosesStaffHitKind.ResidualLane, id, position, wave.Origin,
                    wave.TravelDirection, wave.Damage * .25, wave.Id, wave.Cycle));
            }
        }

        private static void ResolveLanes(IReadOnlyList<ExpandedMosesStaffWaveState> waves, double now,
            Func<IReadOnlyDictionary<int, Vector2>> targets, IReadOnlyDictionary<int, Vector2> previousTargets,
            Func<ExpandedMosesStaffHit, bool> applyHit, Action<ExpandedMosesStaffWaveState> onNaturalEnd)
        {
            var due = new List<ExpandedMosesStaffWaveState>();
            foreach (var wave in waves)
            {
                if (!wave.HasLane || !wave.LaneStartedAt.HasValue) continue;
                var start = wave.LaneStartedAt.Value;
                if (start + wave.LaneDuration <= now + TimeEpsilon)
                {
                    wave.LaneTicks = wave.ExpiredLane;
                    onNaturalEnd(wave);
                    continue;
                }
                var next = start + wave.LaneTicks * LaneInterval;
                if (wave.LaneTicks == wave.LaneTickCount || next > now + TimeEpsilon) continue;
                do { wave.LaneTicks++; }
                while (wave.LaneTicks < wave.LaneTickCount && start + wave.LaneTicks * LaneInterval <= now + TimeEpsilon);
                due.Add(wave);
            }
            if (due.Count == 0) return;

            // Physical contacts use the live target map before support damage. Resolve
            // same-tick lane overlaps once per target, then apply winners in wave/target order.
            var winners = new Dictionary<int, (ExpandedMosesStaffWaveState Wave, bool Outer)>();
            var live = targets();
            foreach (var wave in due)
            {
                var length = Math.Min(wave.Distance, MaximumLaneLength);
                foreach (var pair in live)
                {
                    var previous = previousTargets.TryGetValue(pair.Key, out var old) ? old : pair.Value;
                    if (!CrossesLane(previous, pair.Value, wave, length, out var outer)) continue;
                    if (!winners.TryGetValue(pair.Key, out var current))
                    {
                        winners.Add(pair.Key, (wave, outer));
                        continue;
                    }
                    var damage = wave.LaneDamage(outer);
                    var currentDamage = current.Wave.LaneDamage(current.Outer);
                    if (damage > currentDamage || (damage == currentDamage && wave.Id < current.Wave.Id))
                        winners[pair.Key] = (wave, outer);
                }
            }
            foreach (var hit in winners.OrderBy(entry => entry.Value.Wave.Id).ThenBy(entry => entry.Key))
                if (targets().TryGetValue(hit.Key, out var position))
                    applyHit(new ExpandedMosesStaffHit(ExpandedMosesStaffHitKind.Lane, hit.Key, position,
                        hit.Value.Wave.Origin, hit.Value.Wave, hit.Value.Outer));
        }

        private static bool SweptContact(Vector2 from, Vector2 to, double radius, out float contact)
        {
            var dx = (double)to.X - from.X;
            var dy = (double)to.Y - from.Y;
            var c = (double)from.X * from.X + (double)from.Y * from.Y - radius * radius;
            contact = 0;
            if (c <= DistanceEpsilon) return true;
            var a = dx * dx + dy * dy;
            if (a <= 0) return false;
            var b = from.X * dx + from.Y * dy;
            var discriminant = b * b - a * c;
            if (discriminant < 0) return false;
            var fraction = (-b - Math.Sqrt(discriminant)) / a;
            if (fraction < 0 || fraction > 1) return false;
            contact = (float)fraction;
            return true;
        }

        private static bool CrossesLane(Vector2 previous, Vector2 current, ExpandedMosesStaffWaveState wave, double length,
            out bool outer)
        {
            return CrossesPath(previous, current, wave, length, wave.LaneWidth / 2,
                wave.EmpoweredLaneEdge ? .75 : 0, out outer);
        }

        private static bool CrossesPath(Vector2 previous, Vector2 current, ExpandedMosesStaffWaveState wave,
            double length, double halfWidth, double outerFraction, out bool outer)
        {
            var turn = wave.TurnAtDistance ?? length;
            var first = CrossesLaneSegment(previous, current, wave.Origin, wave.Direction, Math.Min(length, turn),
                halfWidth, outerFraction, out outer);
            if (length <= turn) return first;
            var second = CrossesLaneSegment(previous, current, wave.TurnPosition, wave.TravelDirection, length - turn,
                halfWidth, outerFraction, out var secondOuter);
            outer |= secondOuter;
            return first || second;
        }

        private static bool CrossesLaneSegment(Vector2 previous, Vector2 current, Vector2 origin, Vector2 direction,
            double length, double halfWidth, double outerFraction, out bool outer)
        {
            var from = previous - origin;
            var to = current - origin;
            var perpendicular = new Vector2(-direction.Y, direction.X);
            var fromSide = Vector2.Dot(from, perpendicular);
            var toSide = Vector2.Dot(to, perpendicular);
            double first = 0, last = 1;
            outer = false;
            if (!ClipAxis(Vector2.Dot(from, direction), Vector2.Dot(to, direction), 0, length, ref first, ref last) ||
                !ClipAxis(fromSide, toSide, -halfWidth, halfWidth, ref first, ref last))
                return false;
            if (outerFraction > 0)
            {
                var step = (double)toSide - fromSide;
                var edge = Math.Max(Math.Abs(fromSide + step * first), Math.Abs(fromSide + step * last));
                outer = edge + DistanceEpsilon >= halfWidth * outerFraction;
            }
            return true;
        }

        private static bool ClipAxis(double from, double to, double minimum, double maximum, ref double first, ref double last)
        {
            minimum -= DistanceEpsilon;
            maximum += DistanceEpsilon;
            var delta = to - from;
            if (delta == 0) return from >= minimum && from <= maximum;
            var a = (minimum - from) / delta;
            var b = (maximum - from) / delta;
            first = Math.Max(first, Math.Min(a, b));
            last = Math.Min(last, Math.Max(a, b));
            return first <= last;
        }

        private static Vector2 Rotate(Vector2 direction, double angle) => new Vector2(
            (float)(direction.X * Math.Cos(angle) - direction.Y * Math.Sin(angle)),
            (float)(direction.X * Math.Sin(angle) + direction.Y * Math.Cos(angle)));

        internal static bool ValidOriginalBaseDamage(double value, bool evolved) => value == 0 ||
            (evolved ? value == Profile.AtLevel(8).Damage : OriginalDamageByLevel.Contains(value));

        internal static bool Finite(double number) => !double.IsNaN(number) && !double.IsInfinity(number);
        internal static bool ValidPosition(Vector2 value) =>
            Finite(value.X) && Finite(value.Y) && Math.Abs(value.X) <= 1e6 && Math.Abs(value.Y) <= 1e6;
        internal static bool UnitDirection(Vector2 value) => ValidPosition(value) && Math.Abs(value.LengthSquared() - 1) <= 1e-5;
        internal static bool ValidMotion(double range, double speed) =>
            Finite(range) && range > 0 && range <= MaximumRange &&
            Finite(speed) && speed >= MinimumSpeed && speed <= MaximumSpeed;
    }
}
