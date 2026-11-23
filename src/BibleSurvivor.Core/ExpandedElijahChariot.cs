using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.Serialization;

namespace BibleSurvivor.Core
{
    public enum ExpandedElijahChariotHitKind
    {
        Body,
        Whirlwind,
        Trail,
        Spark,
        OriginFire,
        EndExplosion,
        FollowingWind,
        CrossingWhirlwind,
        EndFlame,
        ReverseChariot,
        CrossingChariot,
    }

    public enum ExpandedElijahChariotSupportKind
    {
        Reverse = 1,
        Crossing = 2,
    }

    public readonly struct ExpandedElijahChariotRankOptions
    {
        public bool ExtendTravel { get; }
        public bool WidenFormation { get; }
        public bool KnockBack { get; }
        public bool WidenWhirlwind { get; }
        public bool ExtendTrail { get; }
        public bool WidenSupportShape { get; }
        public bool WidenDamageRegion { get; }
        public bool PrioritizeCrowds { get; }
        public bool PrioritizeElite { get; }
        public bool ConvergeOnBoss { get; }
        public bool StaggerFormation { get; }
        public bool StrengthenLastChariot { get; }
        public bool EmitSpark { get; }
        public bool IgniteOrigin { get; }
        public bool ExplodeAtEnd { get; }
        public bool LeaveFireTrail { get; }
        public bool RewardBodyHits { get; }
        public bool FollowWithWind { get; }
        public bool WidenFollowingWind { get; }
        public bool DeflectProjectiles { get; }
        public bool GuaranteeFollowingWind { get; }
        public bool WhirlAtCrossing { get; }
        public bool FlameAtNaturalEnd { get; }
        public bool ExtendProcessionOnEliteKill { get; }
        public bool TurnMidway { get; }
        public bool ReverseAtNaturalEnd { get; }
        public bool CrossFormation { get; }
        public double ResidualDurationBonus { get; }
        public double BasicResidualDurationBonus { get; }

        public ExpandedElijahChariotRankOptions(bool extendTravel = false, bool widenFormation = false,
            bool knockBack = false, bool widenWhirlwind = false, bool extendTrail = false, bool widenSupportShape = false,
            bool prioritizeCrowds = false, bool prioritizeElite = false, bool convergeOnBoss = false,
            bool staggerFormation = false, bool strengthenLastChariot = false, double residualDurationBonus = 0, bool widenDamageRegion = false,
            bool emitSpark = false, bool igniteOrigin = false, bool explodeAtEnd = false, bool leaveFireTrail = false,
            bool rewardBodyHits = false, double basicResidualDurationBonus = 0, bool followWithWind = false,
            bool widenFollowingWind = false, bool deflectProjectiles = false, bool guaranteeFollowingWind = false,
            bool whirlAtCrossing = false, bool flameAtNaturalEnd = false, bool extendProcessionOnEliteKill = false,
            bool turnMidway = false, bool reverseAtNaturalEnd = false, bool crossFormation = false)
        {
            if (!ExpandedWeaponRules.ValidResidualDurationBonus(residualDurationBonus))
                throw new ArgumentOutOfRangeException(nameof(residualDurationBonus));
            if (!ExpandedWeaponRules.ValidResidualDurationBonus(basicResidualDurationBonus) || basicResidualDurationBonus > .05)
                throw new ArgumentOutOfRangeException(nameof(basicResidualDurationBonus));
            ExtendTravel = extendTravel;
            WidenFormation = widenFormation;
            KnockBack = knockBack;
            WidenWhirlwind = widenWhirlwind;
            ExtendTrail = extendTrail;
            WidenSupportShape = widenSupportShape;
            WidenDamageRegion = widenDamageRegion;
            PrioritizeCrowds = prioritizeCrowds;
            PrioritizeElite = prioritizeElite;
            ConvergeOnBoss = convergeOnBoss;
            StaggerFormation = staggerFormation;
            StrengthenLastChariot = strengthenLastChariot;
            EmitSpark = emitSpark;
            IgniteOrigin = igniteOrigin;
            ExplodeAtEnd = explodeAtEnd;
            LeaveFireTrail = leaveFireTrail;
            RewardBodyHits = rewardBodyHits;
            FollowWithWind = followWithWind;
            WidenFollowingWind = widenFollowingWind;
            DeflectProjectiles = deflectProjectiles;
            GuaranteeFollowingWind = guaranteeFollowingWind;
            WhirlAtCrossing = whirlAtCrossing;
            FlameAtNaturalEnd = flameAtNaturalEnd;
            ExtendProcessionOnEliteKill = extendProcessionOnEliteKill;
            TurnMidway = turnMidway;
            ReverseAtNaturalEnd = reverseAtNaturalEnd;
            CrossFormation = crossFormation;
            ResidualDurationBonus = residualDurationBonus;
            BasicResidualDurationBonus = basicResidualDurationBonus;
        }

        public static ExpandedElijahChariotRankOptions FromRanks(IReadOnlyDictionary<string, int> ranks)
        {
            if (ranks == null) throw new ArgumentNullException(nameof(ranks));
            bool Has(string id) => ranks.TryGetValue(id, out var rank) && rank > 0;
            return new ExpandedElijahChariotRankOptions(Has("weapon.elijah_chariot.a.01"), Has("weapon.elijah_chariot.b.01"),
                Has("weapon.elijah_chariot.d.01"), Has("weapon.elijah_chariot.e.01"), Has("weapon.elijah_chariot.e.02"),
                Has("common.area.01"), Has("weapon.elijah_chariot.a.02"), Has("weapon.elijah_chariot.a.03"),
                Has("weapon.elijah_chariot.b.02"), Has("weapon.elijah_chariot.b.03"), Has("weapon.elijah_chariot.b.04"),
                ExpandedWeaponRules.ResidualDurationBonus(ranks, true), Has("common.area.09"),
                Has("weapon.elijah_chariot.c.01"), Has("weapon.elijah_chariot.c.02"), Has("weapon.elijah_chariot.c.03"),
                Has("weapon.elijah_chariot.c.04"), Has("weapon.elijah_chariot.c.05"), ExpandedWeaponRules.ResidualDurationBonus(ranks, false),
                Has("weapon.elijah_chariot.d.02"), Has("weapon.elijah_chariot.d.03"), Has("weapon.elijah_chariot.d.04"),
                Has("weapon.elijah_chariot.d.05"), Has("weapon.elijah_chariot.e.03"),
                Has("weapon.elijah_chariot.e.04"), Has("weapon.elijah_chariot.e.05"), Has("weapon.elijah_chariot.a.04"), Has("weapon.elijah_chariot.a.05"), Has("weapon.elijah_chariot.b.05"));
        }
    }

    public readonly struct ExpandedElijahChariotHit
    {
        public ExpandedElijahChariotHitKind Kind { get; }
        public int TargetId { get; }
        public Vector2 Position { get; }
        public Vector2 Direction { get; }
        public double Damage { get; }
        public double OriginalBaseDamage { get; }
        public int TargetOrdinal { get; }
        public double Knockback { get; }
        public long PathId { get; }
        public long Cycle { get; }

        internal ExpandedElijahChariotHit(ExpandedElijahChariotSupportState support, int targetId, Vector2 position)
        {
            Kind = support.Kind == ExpandedElijahChariotSupportKind.Reverse
                ? ExpandedElijahChariotHitKind.ReverseChariot : ExpandedElijahChariotHitKind.CrossingChariot;
            TargetId = targetId;
            Position = position;
            Direction = support.Direction;
            Damage = support.Damage;
            OriginalBaseDamage = 0;
            TargetOrdinal = 0;
            Knockback = 0;
            PathId = support.SourcePathId;
            Cycle = support.Cycle;
        }

        internal ExpandedElijahChariotHit(ExpandedElijahChariotWindState wind, int targetId, Vector2 position, Vector2 direction)
        {
            Kind = ExpandedElijahChariotHitKind.FollowingWind;
            TargetId = targetId;
            Position = position;
            Direction = direction;
            Damage = wind.Damage;
            OriginalBaseDamage = 0;
            TargetOrdinal = 0;
            Knockback = 0;
            PathId = wind.SourcePathId;
            Cycle = wind.Cycle;
        }

        internal ExpandedElijahChariotHit(ExpandedElijahChariotSparkState spark, Vector2 position)
        {
            Kind = ExpandedElijahChariotHitKind.Spark;
            TargetId = spark.TargetId;
            Position = position;
            Direction = Vector2.Zero;
            Damage = spark.Damage;
            OriginalBaseDamage = 0;
            TargetOrdinal = 0;
            Knockback = 0;
            PathId = spark.SourcePathId;
            Cycle = spark.Cycle;
        }

        internal ExpandedElijahChariotHit(ExpandedElijahChariotHitKind kind, int targetId,
            Vector2 position, double damage, ExpandedElijahChariotPathState path, Vector2? direction = null)
        {
            Kind = kind;
            TargetId = targetId;
            Position = position;
            Direction = direction ?? path.CurrentDirection;
            Damage = damage;
            OriginalBaseDamage = kind == ExpandedElijahChariotHitKind.Body ? path.OriginalBaseDamage : 0;
            TargetOrdinal = kind == ExpandedElijahChariotHitKind.Body ? path.BodyHitCount + 1 : 0;
            Knockback = kind == ExpandedElijahChariotHitKind.Body && path.ForwardKnockback ? .30 : 0;
            PathId = path.Id;
            Cycle = path.Cycle;
        }
    }

    public readonly struct ExpandedElijahChariotSweep
    {
        public long PathId { get; }
        public long Cycle { get; }
        public Vector2 Start { get; }
        public Vector2 End { get; }
        public double FromAt { get; }
        public double ToAt { get; }
        public double Width { get; }

        internal ExpandedElijahChariotSweep(ExpandedElijahChariotPathState path, double from, double to)
        {
            PathId = path.Id;
            Cycle = path.Cycle;
            Start = path.PositionAtDistance(from);
            End = path.PositionAtDistance(to);
            FromAt = path.MoveAt + from / path.Speed;
            ToAt = path.MoveAt + to / path.Speed;
            Width = path.Width;
        }
    }

    internal readonly struct ChariotRouteSection
    {
        internal Vector2 Origin { get; }
        internal Vector2 Direction { get; }
        internal double Offset { get; }
        internal double Length { get; }

        internal ChariotRouteSection(Vector2 origin, Vector2 direction, double offset, double length)
        {
            Origin = origin;
            Direction = direction;
            Offset = offset;
            Length = length;
        }
    }

    [DataContract]
    public sealed class ExpandedElijahChariotPathState
    {
        [DataMember(IsRequired = true)] public long Id { get; set; }
        [DataMember(IsRequired = true)] public long Cycle { get; set; }
        [DataMember(IsRequired = true)] public float StartX { get; set; }
        [DataMember(IsRequired = true)] public float StartY { get; set; }
        [DataMember(IsRequired = true)] public float DirectionX { get; set; }
        [DataMember(IsRequired = true)] public float DirectionY { get; set; }
        [DataMember(IsRequired = true)] public double MoveAt { get; set; }
        [DataMember(EmitDefaultValue = false)] public double? FirstDelay { get; set; }
        [DataMember(EmitDefaultValue = false)] public int StaggerIndex { get; set; }
        [DataMember(EmitDefaultValue = false)] public bool SuppressedTrail { get; set; }
        [DataMember(EmitDefaultValue = false)] public bool ForwardKnockback { get; set; }
        [DataMember(EmitDefaultValue = false)] public bool WiderWhirlwind { get; set; }
        [DataMember(EmitDefaultValue = false)] public bool ExtendedTrail { get; set; }
        [DataMember(EmitDefaultValue = false)] public bool WidenedSupportShape { get; set; }
        [DataMember(EmitDefaultValue = false)] public bool WiderDamageRegion { get; set; }
        [DataMember(EmitDefaultValue = false)] public double ResidualDurationBonus { get; set; }
        [DataMember(EmitDefaultValue = false)] public bool MakesOriginFire { get; set; }
        [DataMember(EmitDefaultValue = false)] public int OriginFireTicks { get; set; }
        [DataMember(EmitDefaultValue = false)] public bool MakesBasicTrail { get; set; }
        [DataMember(EmitDefaultValue = false)] public bool WidensEvolvedTrail { get; set; }
        [DataMember(EmitDefaultValue = false)] public bool ExplodesAtEnd { get; set; }
        [DataMember(EmitDefaultValue = false)] public bool RewardsBodyHits { get; set; }
        [DataMember(EmitDefaultValue = false)] public bool FollowsWithWind { get; set; }
        [DataMember(EmitDefaultValue = false)] public bool GuaranteesWind { get; set; }
        [DataMember(EmitDefaultValue = false)] public bool WindChecked { get; set; }
        [DataMember(EmitDefaultValue = false)] public bool WidensWind { get; set; }
        [DataMember(EmitDefaultValue = false)] public bool WidensWindHitShape { get; set; }
        [DataMember(EmitDefaultValue = false)] public bool TracksCrossing { get; set; }
        [DataMember(EmitDefaultValue = false)] public bool MakesEndFlame { get; set; }
        [DataMember(EmitDefaultValue = false)] public bool TurnsMidway { get; set; }
        [DataMember(EmitDefaultValue = false)] public bool ReversesAtEnd { get; set; }
        [DataMember(EmitDefaultValue = false)] public PermanentShotHitBonuses SharedShotHitBonuses { get; set; }
        [DataMember(EmitDefaultValue = false)] public bool HasCrossingChild { get; set; }
        [DataMember(EmitDefaultValue = false)] public bool MidpointTurnResolved { get; set; }
        [DataMember(EmitDefaultValue = false)] public float? TurnDirectionX { get; set; }
        [DataMember(EmitDefaultValue = false)] public float? TurnDirectionY { get; set; }
        [DataMember(IsRequired = true)] public double Range { get; set; }
        [DataMember(IsRequired = true)] public double Speed { get; set; }
        [DataMember(IsRequired = true)] public double Width { get; set; }
        [DataMember(IsRequired = true)] public double Damage { get; set; }
        [DataMember(EmitDefaultValue = false)] public double OriginalBaseDamage { get; set; }
        [DataMember(IsRequired = true)] public double Distance { get; set; }
        [DataMember(IsRequired = true)] public int TrailTicksFired { get; set; }
        [DataMember(IsRequired = true)] public int BodyHitCount { get; set; }
        [DataMember(IsRequired = true)] public List<int> BodyHitIds { get; set; } = new List<int>();
        [DataMember(IsRequired = true)] public List<int> WhirlwindCandidateIds { get; set; } = new List<int>();

        public Vector2 Origin => new Vector2(StartX, StartY);
        public Vector2 Direction => new Vector2(DirectionX, DirectionY);
        public double PivotDistance => Range * .5;
        public Vector2 Pivot => Origin + Direction * (float)PivotDistance;
        public Vector2? TurnDirection => TurnDirectionX is float x && TurnDirectionY is float y ? new Vector2(x, y) : (Vector2?)null;
        public Vector2 CurrentDirection => Distance >= PivotDistance ? TurnDirection ?? Direction : Direction;
        public Vector2 Position => PositionAtDistance(Distance);
        public Vector2 Endpoint => PositionAtDistance(Range);
        public Vector2 PositionAtDistance(double distance) => distance > PivotDistance && TurnDirection is Vector2 turn
            ? Pivot + turn * (float)(distance - PivotDistance) : Origin + Direction * (float)distance;
        internal int SectionCount => TurnDirection.HasValue ? 2 : 1;
        internal ChariotRouteSection Section(int index) => index == 0
            ? new ChariotRouteSection(Origin, Direction, 0, SectionCount == 2 ? PivotDistance : Range)
            : new ChariotRouteSection(Pivot, TurnDirection!.Value, PivotDistance, Range - PivotDistance);
        public double WarningAt => MoveAt - WarningDelay;
        internal double CycleStartedAt => WarningAt - StaggerIndex * ExpandedElijahChariot.StaggerPerChariot;
        private double WarningDelay => FirstDelay ?? ExpandedElijahChariot.Profile.FirstAttackDelay;
        public double EndAt => MoveAt + Range / Speed;
        public bool BodyEnded => Distance >= Range;
        public double WhirlwindRadius => ExpandedElijahChariot.BaseWhirlwindRadius *
            (1 + (WiderWhirlwind ? .15 : 0) + (WidenedSupportShape ? .04 : 0) + (WiderDamageRegion ? .06 : 0));
        public double TrailWidth => (MakesBasicTrail ? ExpandedElijahChariot.BasicTrailWidth : ExpandedElijahChariot.BaseTrailWidth) *
            (AreaScale + (WidensEvolvedTrail ? .15 : 0));
        public double TrailDuration => (MakesBasicTrail ? ExpandedElijahChariot.BasicTrailDuration : ExpandedElijahChariot.BaseTrailDuration) *
            (1 + (ExtendedTrail ? .20 : 0) + ResidualDurationBonus);
        public double OriginFireRadius => ExpandedElijahChariot.OriginFireBaseRadius * AreaScale;
        public double OriginFireDuration => ExpandedElijahChariot.OriginFireBaseDuration * (1 + ResidualDurationBonus);
        internal double AreaScale => 1 + (WidenedSupportShape ? .04 : 0) + (WiderDamageRegion ? .06 : 0);
        internal double OriginFireExpiresAt => MoveAt + OriginFireDuration;
        internal bool HasTrail(bool evolved) => evolved ? !SuppressedTrail : MakesBasicTrail;
        internal double TrailExpiresAt => EndAt + TrailDuration;

        internal int TrailTickLimit => (int)Math.Ceiling((Range / Speed + TrailDuration - ExpandedElijahChariot.TimeEpsilon) /
            ExpandedElijahChariot.TrailInterval);
        internal double TrailAt(int tick) => MoveAt + tick * ExpandedElijahChariot.TrailInterval;
        internal double ExpiresAt(bool evolved) => Math.Max(Math.Max(HasTrail(evolved) ? TrailExpiresAt : EndAt,
            MakesOriginFire ? OriginFireExpiresAt : EndAt), TracksCrossing ? EndAt + ExpandedElijahChariot.CrossingWindow : EndAt);
        internal int OriginFireTickLimit => (int)Math.Ceiling((OriginFireDuration - ExpandedElijahChariot.TimeEpsilon) /
            ExpandedElijahChariot.TrailInterval);
        internal double DistanceAt(double time) => time >= EndAt - ExpandedElijahChariot.TimeEpsilon
            ? Range : Math.Max(0, (time - MoveAt) * Speed);

        internal ExpandedElijahChariotPathState Copy()
        {
            var copy = (ExpandedElijahChariotPathState)MemberwiseClone();
            copy.BodyHitIds = new List<int>(BodyHitIds);
            copy.WhirlwindCandidateIds = new List<int>(WhirlwindCandidateIds);
            return copy;
        }
    }

    [DataContract]
    public sealed class ExpandedElijahChariotSparkState
    {
        [DataMember(IsRequired = true)] public long SourcePathId { get; set; }
        [DataMember(IsRequired = true)] public long Cycle { get; set; }
        [DataMember(IsRequired = true)] public int TargetId { get; set; }
        [DataMember(IsRequired = true)] public float X { get; set; }
        [DataMember(IsRequired = true)] public float Y { get; set; }
        [DataMember(IsRequired = true)] public double Damage { get; set; }
        [DataMember(IsRequired = true)] public double SpawnedAt { get; set; }
        [DataMember(IsRequired = true)] public double SteppedAt { get; set; }

        public Vector2 Position => new Vector2(X, Y);
        internal ExpandedElijahChariotSparkState Copy() => (ExpandedElijahChariotSparkState)MemberwiseClone();
    }

    [DataContract]
    public sealed class ExpandedElijahChariotWindState
    {
        [DataMember(IsRequired = true)] public long SourcePathId { get; set; }
        [DataMember(IsRequired = true)] public long Cycle { get; set; }
        [DataMember(IsRequired = true)] public float StartX { get; set; }
        [DataMember(IsRequired = true)] public float StartY { get; set; }
        [DataMember(IsRequired = true)] public float DirectionX { get; set; }
        [DataMember(IsRequired = true)] public float DirectionY { get; set; }
        [DataMember(IsRequired = true)] public double MoveAt { get; set; }
        [DataMember(IsRequired = true)] public double Range { get; set; }
        [DataMember(IsRequired = true)] public double Speed { get; set; }
        [DataMember(IsRequired = true)] public double Damage { get; set; }
        [DataMember(IsRequired = true)] public double Distance { get; set; }
        [DataMember(IsRequired = true)] public List<int> HitIds { get; set; } = new List<int>();
        [DataMember(EmitDefaultValue = false)] public bool Guaranteed { get; set; }
        [DataMember(EmitDefaultValue = false)] public bool Wider { get; set; }
        [DataMember(EmitDefaultValue = false)] public bool WiderHitShape { get; set; }
        [DataMember(EmitDefaultValue = false)] public float? TurnDirectionX { get; set; }
        [DataMember(EmitDefaultValue = false)] public float? TurnDirectionY { get; set; }

        public Vector2 Origin => new Vector2(StartX, StartY);
        public Vector2 Direction => new Vector2(DirectionX, DirectionY);
        public double PivotDistance => Range * .5;
        public Vector2 Pivot => Origin + Direction * (float)PivotDistance;
        public Vector2? TurnDirection => TurnDirectionX is float x && TurnDirectionY is float y ? new Vector2(x, y) : (Vector2?)null;
        public Vector2 CurrentDirection => Distance >= PivotDistance ? TurnDirection ?? Direction : Direction;
        public Vector2 Position => PositionAtDistance(Distance);
        public Vector2 Endpoint => PositionAtDistance(Range);
        public Vector2 PositionAtDistance(double distance) => distance > PivotDistance && TurnDirection is Vector2 turn
            ? Pivot + turn * (float)(distance - PivotDistance) : Origin + Direction * (float)distance;
        internal int SectionCount => TurnDirection.HasValue ? 2 : 1;
        internal ChariotRouteSection Section(int index) => index == 0
            ? new ChariotRouteSection(Origin, Direction, 0, SectionCount == 2 ? PivotDistance : Range)
            : new ChariotRouteSection(Pivot, TurnDirection!.Value, PivotDistance, Range - PivotDistance);
        public double Width => ExpandedElijahChariot.WindBaseWidth * (1 + (Wider ? .20 : 0) + (WiderHitShape ? .04 : 0));
        public double EndAt => MoveAt + Range / Speed;
        internal double DistanceAt(double now) => now >= EndAt - ExpandedElijahChariot.TimeEpsilon ? Range :
            Math.Max(0, (now - MoveAt) * Speed);

        internal ExpandedElijahChariotWindState Copy()
        {
            var copy = (ExpandedElijahChariotWindState)MemberwiseClone();
            copy.HitIds = new List<int>(HitIds);
            return copy;
        }
    }

    [DataContract]
    public sealed class ExpandedElijahChariotSupportState
    {
        [DataMember(IsRequired = true)] public ExpandedElijahChariotSupportKind Kind { get; set; }
        [DataMember(IsRequired = true)] public long SourcePathId { get; set; }
        [DataMember(IsRequired = true)] public long Cycle { get; set; }
        [DataMember(IsRequired = true)] public float StartX { get; set; }
        [DataMember(IsRequired = true)] public float StartY { get; set; }
        [DataMember(IsRequired = true)] public float DirectionX { get; set; }
        [DataMember(IsRequired = true)] public float DirectionY { get; set; }
        [DataMember(IsRequired = true)] public double MoveAt { get; set; }
        [DataMember(IsRequired = true)] public double Range { get; set; }
        [DataMember(IsRequired = true)] public double Speed { get; set; }
        [DataMember(IsRequired = true)] public double Width { get; set; }
        [DataMember(IsRequired = true)] public double Damage { get; set; }
        [DataMember(IsRequired = true)] public double Distance { get; set; }
        [DataMember(IsRequired = true)] public List<int> HitIds { get; set; } = new List<int>();

        public Vector2 Origin => new Vector2(StartX, StartY);
        public Vector2 Direction => new Vector2(DirectionX, DirectionY);
        public Vector2 Position => Origin + Direction * (float)Distance;
        public double EndAt => MoveAt + Range / Speed;
        internal double DistanceAt(double now) => now >= EndAt - ExpandedElijahChariot.TimeEpsilon ? Range :
            Math.Max(0, (now - MoveAt) * Speed);

        internal ExpandedElijahChariotSupportState Copy()
        {
            var copy = (ExpandedElijahChariotSupportState)MemberwiseClone();
            copy.HitIds = new List<int>(HitIds);
            return copy;
        }
    }

    [DataContract]
    public sealed class ExpandedElijahChariotState
    {
        [DataMember(IsRequired = true)] public bool Evolved { get; set; }
        [DataMember(IsRequired = true)] public long Cycle { get; set; }
        [DataMember(IsRequired = true)] public long NextPathId { get; set; } = 1;
        [DataMember(IsRequired = true)] public double NextCycle { get; set; }
        [DataMember(EmitDefaultValue = false)] public int SuppressedOriginals { get; set; }
        [DataMember(IsRequired = true)] public List<ExpandedElijahChariotPathState> Paths { get; set; } =
            new List<ExpandedElijahChariotPathState>();
        [DataMember(EmitDefaultValue = false)] public double? NextSparkAt { get; set; }
        [DataMember(EmitDefaultValue = false)] public double? NextEndExplosionAt { get; set; }
        [DataMember(EmitDefaultValue = false)] public ExpandedElijahChariotSparkState? Spark { get; set; }

        [DataMember(EmitDefaultValue = false)] public double? NextWindAt { get; set; }
        [DataMember(EmitDefaultValue = false)] public double? NextGuaranteedWindAt { get; set; }
        [DataMember(EmitDefaultValue = false)] public double? NextDeflectAt { get; set; }
        [DataMember(EmitDefaultValue = false)] public List<ExpandedElijahChariotWindState>? Winds { get; set; }
        [DataMember(EmitDefaultValue = false)] public double? IntersectionCheckedAt { get; set; }
        [DataMember(EmitDefaultValue = false)] public double? NextCrossingAt { get; set; }
        [DataMember(EmitDefaultValue = false)] public double? NextEndFlameAt { get; set; }
        [DataMember(EmitDefaultValue = false)] public double? NextProcessionAt { get; set; }
        [DataMember(EmitDefaultValue = false)] public double? ProcessionUntil { get; set; }
        [DataMember(EmitDefaultValue = false)] public double? NextReverseAt { get; set; }
        [DataMember(EmitDefaultValue = false)] public double? NextCrossChildAt { get; set; }
        [DataMember(EmitDefaultValue = false)] public List<ExpandedElijahChariotSupportState>? SupportChariots { get; set; }

        public int ActiveSupportProjectiles => (Spark == null ? 0 : 1) + (Winds?.Count ?? 0) + (SupportChariots?.Count ?? 0);
        public int ActiveSupportRegions => Paths.Sum(path => (path.HasTrail(Evolved) ? 1 : 0) + (path.MakesOriginFire ? 1 : 0));
        public int ActiveSupportRegionsAt(double now) => Paths.Sum(path =>
            (path.HasTrail(Evolved) && path.TrailExpiresAt > now + ExpandedElijahChariot.TimeEpsilon ? 1 : 0) +
            (path.MakesOriginFire && path.OriginFireExpiresAt > now + ExpandedElijahChariot.TimeEpsilon ? 1 : 0));

        public ExpandedElijahChariotState Copy() => new ExpandedElijahChariotState
        {
            Evolved = Evolved,
            Cycle = Cycle,
            NextPathId = NextPathId,
            NextCycle = NextCycle,
            SuppressedOriginals = SuppressedOriginals,
            NextSparkAt = NextSparkAt,
            NextEndExplosionAt = NextEndExplosionAt,
            Spark = Spark?.Copy(),
            NextWindAt = NextWindAt,
            NextGuaranteedWindAt = NextGuaranteedWindAt,
            NextDeflectAt = NextDeflectAt,
            Winds = Winds?.Select(wind => wind.Copy()).ToList(),
            IntersectionCheckedAt = IntersectionCheckedAt,
            NextCrossingAt = NextCrossingAt,
            NextEndFlameAt = NextEndFlameAt,
            NextProcessionAt = NextProcessionAt,
            ProcessionUntil = ProcessionUntil,
            NextReverseAt = NextReverseAt,
            NextCrossChildAt = NextCrossChildAt,
            SupportChariots = SupportChariots?.Select(support => support.Copy()).ToList(),
            Paths = Paths.Select(path => path.Copy()).ToList(),
        };

        public void ResetForForm(bool evolved, double now)
        {
            if (!BattleSnapshot.Finite(now) || now < 0) throw new ArgumentOutOfRangeException(nameof(now));
            Evolved = evolved;
            NextCycle = now;
            Paths.Clear();
            Spark = null;
            Winds = null;
            SupportChariots = null;
            IntersectionCheckedAt = null;
            if (!evolved)
            {
                NextCrossingAt = null;
                NextEndFlameAt = null;
                NextProcessionAt = null;
                ProcessionUntil = null;
            }
        }

        public void Validate(double now, bool evolved)
        {
            const double epsilon = ExpandedElijahChariot.TimeEpsilon;
            if (!BattleSnapshot.Finite(now) || now < 0 || evolved != Evolved || NextPathId < 1 ||
                SuppressedOriginals < 0 || NextPathId > long.MaxValue - SuppressedOriginals ||
                Cycle < 0 || Cycle >= NextPathId + SuppressedOriginals ||
                (Cycle == 0 ? NextPathId != 1 || SuppressedOriginals != 0 :
                    (NextPathId + SuppressedOriginals - 2) / ExpandedElijahChariot.MaximumAmount >= Cycle) ||
                !BattleSnapshot.Finite(NextCycle) || NextCycle < 0 ||
                NextCycle > now + ExpandedElijahChariot.MaximumCycle + epsilon ||
                !ExpandedElijahChariot.ValidCooldown(NextSparkAt, now, ExpandedElijahChariot.SparkCooldown) ||
                !ExpandedElijahChariot.ValidCooldown(NextEndExplosionAt, now, ExpandedElijahChariot.EndExplosionCooldown) ||
                !ExpandedElijahChariot.ValidCooldown(NextWindAt, now, ExpandedElijahChariot.WindCooldown) ||
                !ExpandedElijahChariot.ValidCooldown(NextGuaranteedWindAt, now, ExpandedElijahChariot.GuaranteedWindCooldown) ||
                !ExpandedElijahChariot.ValidCooldown(NextDeflectAt, now, ExpandedElijahChariot.DeflectCooldown) ||
                !ExpandedElijahChariot.ValidCooldown(NextCrossingAt, now, ExpandedElijahChariot.CrossingCooldown) ||
                !ExpandedElijahChariot.ValidCooldown(NextEndFlameAt, now, ExpandedElijahChariot.EndFlameCooldown) ||
                !ExpandedElijahChariot.ValidCooldown(NextProcessionAt, now, ExpandedElijahChariot.ProcessionLifetime) ||
                !ExpandedElijahChariot.ValidCooldown(ProcessionUntil, now, ExpandedElijahChariot.ProcessionLifetime) ||
                !ExpandedElijahChariot.ValidCooldown(NextReverseAt, now, ExpandedElijahChariot.SupportChariotCooldown) ||
                !ExpandedElijahChariot.ValidCooldown(NextCrossChildAt, now, ExpandedElijahChariot.SupportChariotCooldown) ||
                ActiveSupportProjectiles > ExpandedElijahChariot.MaximumExtraGroups ||
                ProcessionUntil.HasValue && (NextProcessionAt.HasValue ? ProcessionUntil != NextProcessionAt :
                    Math.Abs(ProcessionUntil.Value - now) > epsilon) ||
                IntersectionCheckedAt.HasValue && IntersectionCheckedAt.Value != now ||
                !evolved && (NextCrossingAt.HasValue || IntersectionCheckedAt.HasValue || NextEndFlameAt.HasValue ||
                    NextProcessionAt.HasValue || ProcessionUntil.HasValue) ||
                Paths == null || Paths.Count > ExpandedElijahChariot.MaximumPaths)
                throw new ArgumentException("Invalid Elijah chariot clock");

            if (Spark is { } spark && (!NextSparkAt.HasValue || spark.SourcePathId <= 0 || spark.SourcePathId >= NextPathId ||
                spark.Cycle <= 0 || spark.Cycle > Cycle || spark.TargetId <= 0 || !BattleSnapshot.Position(spark.X, spark.Y) ||
                !BattleSnapshot.Finite(spark.Damage) || spark.Damage <= 0 || spark.Damage > ExpandedElijahChariot.MaximumSparkDamage ||
                !BattleSnapshot.Finite(spark.SpawnedAt) || spark.SpawnedAt < 0 ||
                spark.SteppedAt != now || spark.SteppedAt < spark.SpawnedAt ||
                spark.SteppedAt >= spark.SpawnedAt + ExpandedElijahChariot.SparkLifetime ||
                Math.Abs(NextSparkAt.Value - (spark.SpawnedAt + ExpandedElijahChariot.SparkCooldown)) > epsilon))
                throw new ArgumentException("Invalid Elijah chariot spark");

            var previousId = 0L;
            var previousCycle = 0L;
            var previousCycleStart = 0d;
            var previousStaggerIndex = 0;
            var reversingCycles = new HashSet<long>();
            var crossingChildCycles = new HashSet<long>();
            foreach (var path in Paths)
            {
                if (path == null || path.Id <= previousId || path.Id >= NextPathId || path.Cycle < 1 || path.Cycle > Cycle ||
                    !BattleSnapshot.Position(path.StartX, path.StartY) ||
                    !BattleSnapshot.Position(path.DirectionX, path.DirectionY) ||
                    Math.Abs(path.Direction.LengthSquared() - 1) > .0001 ||
                    !ExpandedElijahChariot.ValidTurn(path.Direction, path.TurnDirectionX, path.TurnDirectionY) ||
                    !path.TurnsMidway && path.MidpointTurnResolved || !path.MidpointTurnResolved && path.TurnDirection.HasValue ||
                    path.TurnsMidway && path.MidpointTurnResolved != (path.Distance + epsilon >= path.PivotDistance) ||
                    !ExpandedElijahChariot.ValidPath(path.Damage, path.Width, path.Speed, path.Range) ||
                    !ExpandedElijahChariot.ValidOriginalBaseDamage(path.OriginalBaseDamage, evolved) ||
                    (path.SharedShotHitBonuses & ~PermanentShotHitBonuses.LongFlight) != 0 ||
                    path.SharedShotHitBonuses != PermanentShotHitBonuses.None && path.OriginalBaseDamage <= 0 ||
                    !BattleSnapshot.Finite(path.MoveAt) || !ExpandedElijahChariot.ValidDelay(path.FirstDelay) ||
                    path.StaggerIndex < 0 || path.StaggerIndex >= ExpandedElijahChariot.MaximumAmount ||
                    path.GuaranteesWind && (path.Cycle % 5 != 0 || path.StaggerIndex > 0) ||
                    path.ReversesAtEnd && (path.Cycle % 5 != 0 || path.StaggerIndex > 0 ||
                        previousCycle == path.Cycle || !reversingCycles.Add(path.Cycle)) ||
                    path.HasCrossingChild && (path.Cycle % 6 != 0 || path.StaggerIndex > 0 || !crossingChildCycles.Add(path.Cycle)) ||
                    !path.FollowsWithWind && !path.GuaranteesWind && (path.WidensWind || path.WidensWindHitShape) ||
                    path.WindChecked && (!path.FollowsWithWind && !path.GuaranteesWind || now < path.MoveAt - epsilon) ||
                    !path.WindChecked && (path.FollowsWithWind || path.GuaranteesWind) && now > path.MoveAt + epsilon ||
                    path.Cycle < previousCycle || path.Cycle == previousCycle &&
                        (Math.Abs(path.CycleStartedAt - previousCycleStart) > epsilon ||
                            previousStaggerIndex > 0 && path.StaggerIndex <= previousStaggerIndex) ||
                    path.CycleStartedAt < -epsilon || path.CycleStartedAt > now + epsilon ||
                    path.CycleStartedAt + ExpandedElijahChariot.MinimumCycle > NextCycle + epsilon ||
                    path.ExpiresAt(evolved) < now - epsilon ||
                    !BattleSnapshot.Finite(path.Distance) || path.Distance < 0 || path.Distance > path.Range ||
                    Math.Abs(path.Distance - path.DistanceAt(now)) > .000001)
                    throw new ArgumentException("Invalid Elijah chariot path");

                ValidateHitState(path, now, evolved);
                previousId = path.Id;
                previousCycle = path.Cycle;
                previousCycleStart = path.CycleStartedAt;
                previousStaggerIndex = path.StaggerIndex;
            }
            ValidateWinds(now);
            foreach (var cycle in crossingChildCycles)
                if (Paths.Count(path => path.Cycle == cycle) != 1)
                    throw new ArgumentException("A crossing support chariot requires a lone original");
            ValidateSupportChariots(now);
        }

        private void ValidateWinds(double now)
        {
            if (Winds == null) return;
            if (Winds.Count == 0 || ActiveSupportProjectiles > ExpandedElijahChariot.MaximumExtraGroups)
                throw new ArgumentException("Invalid Elijah chariot wind count");
            var sources = new HashSet<long>();
            foreach (var wind in Winds)
            {
                if (wind == null || wind.SourcePathId <= 0 || wind.SourcePathId >= NextPathId || !sources.Add(wind.SourcePathId) ||
                    wind.Cycle <= 0 || wind.Cycle > Cycle || wind.Guaranteed && wind.Cycle % 5 != 0 ||
                    !ExpandedElijahChariot.ValidWind(wind, now))
                    throw new ArgumentException("Invalid Elijah chariot wind");
                var path = Paths.Find(candidate => candidate?.Id == wind.SourcePathId);
                if (path != null && (path.Cycle != wind.Cycle || !path.WindChecked ||
                    wind.Guaranteed && !path.GuaranteesWind || !wind.Guaranteed && !path.FollowsWithWind ||
                    Math.Abs(wind.MoveAt - (path.MoveAt + ExpandedElijahChariot.WindDelay)) > ExpandedElijahChariot.TimeEpsilon ||
                    wind.Origin != path.Origin || wind.Direction != path.Direction || wind.TurnDirection != path.TurnDirection ||
                    wind.Speed != path.Speed ||
                    wind.Range != path.Range || wind.Wider != path.WidensWind || wind.WiderHitShape != path.WidensWindHitShape ||
                    wind.Damage != path.Damage * ExpandedElijahChariot.WindFactor(wind.Guaranteed)))
                    throw new ArgumentException("Invalid Elijah chariot wind source");
            }
        }

        private void ValidateSupportChariots(double now)
        {
            if (SupportChariots == null) return;
            if (SupportChariots.Count == 0) throw new ArgumentException("Invalid Elijah support chariots");
            var sources = new HashSet<(long Path, ExpandedElijahChariotSupportKind Kind)>();
            foreach (var support in SupportChariots)
            {
                if (support == null || support.SourcePathId <= 0 || support.SourcePathId >= NextPathId ||
                    support.Cycle <= 0 || support.Cycle > Cycle || !sources.Add((support.SourcePathId, support.Kind)) ||
                    !ExpandedElijahChariot.ValidSupportChariot(support, now))
                    throw new ArgumentException("Invalid Elijah support chariot");
                var path = Paths.Find(candidate => candidate.Id == support.SourcePathId);
                if (path == null) continue;

                var reverse = support.Kind == ExpandedElijahChariotSupportKind.Reverse;
                if (support.Cycle != path.Cycle || support.Range != path.Range || support.Speed != path.Speed ||
                    support.Width != path.Width || support.Damage != path.Damage * ExpandedElijahChariot.SupportChariotFactor ||
                    (reverse ? !path.ReversesAtEnd || !path.BodyEnded || support.Origin != path.Endpoint ||
                        support.Direction != -path.CurrentDirection || support.MoveAt != path.EndAt :
                        !path.HasCrossingChild || support.Origin != path.Origin ||
                        support.Direction != ExpandedElijahChariot.Perpendicular(path.Direction) || support.MoveAt != path.MoveAt))
                    throw new ArgumentException("Invalid Elijah support chariot source");
            }
        }

        private static void ValidateHitState(ExpandedElijahChariotPathState path, double now, bool evolved)
        {
            const double epsilon = ExpandedElijahChariot.TimeEpsilon;
            var hasTrail = path.HasTrail(evolved);
            if (path.BodyHitIds == null || path.WhirlwindCandidateIds == null ||
                path.BodyHitIds.Count + path.WhirlwindCandidateIds.Count > ExpandedElijahChariot.MaximumLiveTargets ||
                path.BodyHitCount < path.BodyHitIds.Count || path.BodyHitCount > ExpandedElijahChariot.MaximumBodyHits ||
                path.BodyHitIds.Concat(path.WhirlwindCandidateIds).Any(id => id < 1) ||
                path.BodyHitIds.Concat(path.WhirlwindCandidateIds).Distinct().Count() !=
                    path.BodyHitIds.Count + path.WhirlwindCandidateIds.Count ||
                (path.BodyEnded || !evolved) && path.WhirlwindCandidateIds.Count != 0 ||
                now < path.MoveAt - epsilon && (path.BodyHitCount != 0 || path.WhirlwindCandidateIds.Count != 0) ||
                !evolved && (path.SuppressedTrail || path.WiderWhirlwind || path.WidensEvolvedTrail || path.ExtendedTrail ||
                    path.ResidualDurationBonus > .05 || path.TracksCrossing || path.MakesEndFlame ||
                    !hasTrail && !path.MakesOriginFire && !path.ExplodesAtEnd && (path.WidenedSupportShape || path.WiderDamageRegion)) ||
                evolved && path.MakesBasicTrail || path.WidensEvolvedTrail && !hasTrail ||
                path.RewardsBodyHits && !path.ExplodesAtEnd ||
                !ExpandedWeaponRules.ValidResidualDurationBonus(path.ResidualDurationBonus) ||
                path.ExtendedTrail && !hasTrail || path.ResidualDurationBonus != 0 && !hasTrail && !path.MakesOriginFire ||
                path.TrailTicksFired < 0 || path.TrailTicksFired > (hasTrail ? path.TrailTickLimit : 0) ||
                path.OriginFireTicks < 0 || path.OriginFireTicks > (path.MakesOriginFire ? path.OriginFireTickLimit : 0))
                throw new ArgumentException("Invalid Elijah chariot hit history");

            var previousTrailAt = path.TrailAt(path.TrailTicksFired - 1);
            var nextTrailAt = path.TrailAt(path.TrailTicksFired);
            if (path.TrailTicksFired > 0 && (previousTrailAt > now + epsilon || previousTrailAt >= path.TrailExpiresAt - epsilon) ||
                hasTrail && nextTrailAt < path.TrailExpiresAt - epsilon && nextTrailAt < now - epsilon ||
                path.MakesOriginFire && (path.OriginFireTicks > 0 && path.TrailAt(path.OriginFireTicks - 1) > now + epsilon ||
                    path.OriginFireTicks < path.OriginFireTickLimit && path.TrailAt(path.OriginFireTicks) < now - epsilon))
                throw new ArgumentException("Invalid Elijah chariot trail schedule");
        }
    }

    public static class ExpandedElijahChariot
    {
        internal static readonly ExpandedWeaponProfile Profile = ExpandedWeaponRules.Get("elijah_chariot");
        internal static readonly int MaximumAmount = Profile.MaximumOriginalAmount;
        internal static readonly double MaximumCycle = Profile.AtLevel(1).Cycle;
        private static readonly double MaximumDamage = Profile.AtLevel(8).Damage * 2.25;
        private static readonly double[] OriginalDamageByLevel = Enumerable.Range(1, 8)
            .Select(level => (double)Profile.AtLevel(level).Damage).ToArray();
        private static readonly double MaximumWidth = Profile.AtLevel(1).Width * 2.5;
        private static readonly double MinimumSpeed = Profile.AtLevel(1).Speed;
        private static readonly double MaximumSpeed = MinimumSpeed * 1.75;
        internal static readonly double MaximumBaseRange = Profile.TravelDistance * 1.05;
        internal static readonly double MaximumUntrainedRange = Profile.TravelDistance;
        internal static readonly double TravelBonus = Profile.TravelDistance * .12;
        private static readonly double MaximumRange = MaximumBaseRange + TravelBonus;
        internal static readonly int MaximumPaths = MaximumAmount *
            (int)Math.Ceiling((Profile.FirstAttackDelay + (MaximumAmount - 1) * StaggerPerChariot +
                MaximumRange / MinimumSpeed + BaseTrailDuration * (1.20 + ExpandedWeaponRules.MaximumResidualDurationBonus)) / MinimumCycle);
        internal const double TimeEpsilon = 1e-9;
        internal const double StaggerPerChariot = .12;
        private const double MinimumConvergedRange = .000001;
        private const double AimRadius = 8;
        private const double CrowdRadius = 1.5;
        private const double LastChariotDamageBonus = .10;
        internal const double MinimumCycle = .15;
        private const double MinimumDelay = .05;
        internal const double TrailInterval = .5;
        internal const double BaseTrailDuration = 1.5;
        internal const int MaximumLiveTargets = 120;
        internal const int MaximumBodyHits = 6210;
        internal const double BaseTrailWidth = .9;
        internal const double BasicTrailWidth = .7;
        internal const double BasicTrailDuration = 1;
        internal const double OriginFireBaseRadius = .8;
        internal const double OriginFireBaseDuration = 1.5;
        internal const double SparkCooldown = 1;
        internal const double EndExplosionCooldown = 2;
        internal const double WindDelay = .5;
        internal const double WindBaseWidth = .6;
        internal const double WindCooldown = 2;
        internal const double GuaranteedWindCooldown = 1;
        internal const double DeflectCooldown = 2;
        internal const double CrossingWindow = .25;
        internal const double CrossingCooldown = 3;
        internal const double EndFlameCooldown = 3;
        internal const double ProcessionLifetime = 6;
        private const double CrossingRadius = 1;
        private const double CrossingFactor = .40;
        private const double EndFlameRadius = 1.3;
        private const double EndFlameFactor = .40;
        private const double CrossingGeometryEpsilon = .00001;
        private const double MaxTurnRadians = Math.PI / 15;
        // Origins and directions have Unity float precision, while crossing times are doubles.
        private const double CrossingTimeEpsilon = .0000001;
        internal const int MaximumExtraGroups = 4;
        internal const double SupportChariotCooldown = 1;
        internal const double SupportChariotFactor = .35;
        private const double SparkSpeed = 6;
        private const double SparkGeometryEpsilon = 1e-5;
        private const double SparkSearchRadius = 2.5;
        internal const double SparkLifetime = SparkSearchRadius / SparkSpeed;
        private const double SparkFactor = .25;
        private const double OriginFireFactor = .25;
        private const double BasicTrailFactor = .20;
        private const double EndExplosionFactor = .35;
        private const double EndExplosionRadius = 1;
        private const double EndExplosionBodyBonus = .15;
        internal static double MaximumSparkDamage => MaximumDamage * SparkFactor;
        private const double TrailDamageFactor = .35;
        internal const double BaseWhirlwindRadius = 1.15;
        private const double WhirlwindDamageFactor = .45;

        public static void Step(ExpandedElijahChariotState state, double now, Vector2 playerPosition, Vector2 facing,
            int level, bool evolved, double damage, double cycle, int amount, double width, double speed, double range,
            Func<IReadOnlyDictionary<int, Vector2>> targets, Func<ExpandedElijahChariotHit, bool> applyHit,
            double delayAdvance = 0, Func<bool>? reserveOriginal = null, Func<bool>? reserveTrail = null,
            ExpandedElijahChariotRankOptions rankOptions = default, Func<double>? nextCycleDuration = null,
            Func<int, bool>? isElite = null, Func<int, bool>? isBoss = null,
            Action<int>? recordOriginalSuppression = null, Action? recordSupportSuppression = null,
            Func<bool>? reserveSupportShot = null, Action<Vector2, double>? emitEndExplosion = null,
            Func<ExpandedElijahChariotHit, double?>? applyOriginalHit = null,
            Func<ExpandedElijahChariotSweep, bool>? tryDeflect = null, Action<Vector2, double>? emitCrossingWhirlwind = null,
            Action<Vector2, double>? emitEndFlame = null, PermanentShotRules shotRules = default)
        {
            if (state == null) throw new ArgumentNullException(nameof(state));
            if (targets == null) throw new ArgumentNullException(nameof(targets));
            if (applyHit == null) throw new ArgumentNullException(nameof(applyHit));
            if ((rankOptions.PrioritizeElite || evolved && rankOptions.ExtendProcessionOnEliteKill) && isElite == null) throw new ArgumentNullException(nameof(isElite));
            if ((rankOptions.PrioritizeElite || rankOptions.ConvergeOnBoss) && isBoss == null)
                throw new ArgumentNullException(nameof(isBoss));
            if (!BattleSnapshot.Finite(delayAdvance) || delayAdvance < 0) throw new ArgumentOutOfRangeException(nameof(delayAdvance));
            if (!BattleSnapshot.Finite(now) || now < 0 ||
                !BattleSnapshot.Position(playerPosition.X, playerPosition.Y) || !BattleSnapshot.Position(facing.X, facing.Y) ||
                level < 1 || level > 8 || evolved && level != 8 || amount < 1 || amount > MaximumAmount ||
                !BattleSnapshot.Finite(cycle) || cycle < MinimumCycle || cycle > MaximumCycle ||
                !ValidPath(damage, width, speed, range) || range > MaximumBaseRange)
                throw new ArgumentException("Invalid Elijah chariot parameters");

            if (state.Evolved != evolved) state.ResetForForm(evolved, now);
            if (state.NextSparkAt <= now + TimeEpsilon) state.NextSparkAt = null;
            if (state.NextEndExplosionAt <= now + TimeEpsilon) state.NextEndExplosionAt = null;
            if (state.NextWindAt <= now + TimeEpsilon) state.NextWindAt = null;
            if (state.NextGuaranteedWindAt <= now + TimeEpsilon) state.NextGuaranteedWindAt = null;
            if (state.NextDeflectAt <= now + TimeEpsilon) state.NextDeflectAt = null;
            if (state.NextCrossingAt <= now + TimeEpsilon) state.NextCrossingAt = null;
            if (state.NextEndFlameAt <= now + TimeEpsilon) state.NextEndFlameAt = null;
            if (state.NextProcessionAt <= now + TimeEpsilon) state.NextProcessionAt = null;
            if (state.ProcessionUntil < now - TimeEpsilon) state.ProcessionUntil = null;
            if (state.NextReverseAt <= now + TimeEpsilon) state.NextReverseAt = null;
            if (state.NextCrossChildAt <= now + TimeEpsilon) state.NextCrossChildAt = null;
            AdvanceSpark(state, now, targets, applyHit);
            AdvanceWinds(state, now, targets, applyHit);
            var canSpark = rankOptions.EmitSpark && !state.NextSparkAt.HasValue;
            Func<ExpandedElijahChariotHit, double?> resolveOriginal = applyOriginalHit ??
                (hit => applyHit(hit) ? hit.Damage : (double?)null);
            var canProcProcession = evolved && rankOptions.ExtendProcessionOnEliteKill && !state.NextProcessionAt.HasValue;
            var tracksCrossing = evolved && rankOptions.WhirlAtCrossing;
            AdvancePaths(state, now, targets, applyHit, resolveOriginal, canSpark ? SpawnSpark : (Action<ExpandedElijahChariotHit, double>?)null,
                ResolveNaturalEnd, ScheduleWind, rankOptions.DeflectProjectiles ? tryDeflect : null,
                canProcProcession ? isElite : null, LatchProcession, tracksCrossing ? ResolveCrossing : (Action?)null);
            AdvanceSupportChariots(state, now, targets, applyHit);
            if (state.NextCycle > now + TimeEpsilon) return;

            var direction = facing.LengthSquared() > 1e-12f ? Vector2.Normalize(facing) : Vector2.UnitX;
            var canAim = rankOptions.PrioritizeCrowds || rankOptions.PrioritizeElite;
            var current = canAim || rankOptions.ConvergeOnBoss ? targets() : null;
            if (canAim)
                direction = ResolveAim(playerPosition, direction, current!, rankOptions, isElite, isBoss);
            var converge = rankOptions.ConvergeOnBoss ? SoleBossPosition(current!, isBoss!) : null;
            var perpendicular = Perpendicular(direction);
            var spacing = Profile.ParallelSpacing + (rankOptions.WidenFormation ? .30 : 0);
            var travel = range + (rankOptions.ExtendTravel ? TravelBonus : 0);
            var delay = Math.Max(MinimumDelay, Profile.FirstAttackDelay - delayAdvance);
            var moveAt = state.NextCycle + delay;
            state.Cycle = checked(state.Cycle + 1);
            state.NextCycle += nextCycleDuration?.Invoke() ?? cycle;
            var originalBaseDamage = Profile.AtLevel(level).Damage;
            var processionBoost = evolved && state.ProcessionUntil.HasValue;
            if (processionBoost) amount = Math.Min(MaximumAmount, amount + 1);
            var crossFormation = rankOptions.CrossFormation && state.Cycle % 6 == 0;
            var firstDirection = direction;
            var firstStart = playerPosition;
            for (var index = 0; index < amount; index++)
            {
                if (reserveOriginal?.Invoke() == false)
                {
                    state.SuppressedOriginals = checked(state.SuppressedOriginals + amount - index);
                    recordOriginalSuppression?.Invoke(amount - index);
                    break;
                }
                var offset = (index - (amount - 1) * .5) * spacing;
                var start = playerPosition + perpendicular * (float)offset;
                var pathDirection = direction;
                var pathRange = travel;
                if (converge.HasValue)
                {
                    var x = (double)converge.Value.X - start.X;
                    var y = (double)converge.Value.Y - start.Y;
                    var distance = Math.Sqrt(x * x + y * y);
                    if (distance > 0)
                        pathDirection = new Vector2((float)(x / distance), (float)(y / distance));
                    // Use the same projection as collision so the quantized direction includes the boss at the endpoint.
                    pathRange = Math.Min(travel, Math.Max(MinimumConvergedRange, x * pathDirection.X + y * pathDirection.Y));
                }
                if (index == 0)
                {
                    firstDirection = pathDirection;
                    firstStart = start;
                }
                else if (crossFormation && index == amount - 1)
                {
                    var across = Perpendicular(firstDirection);
                    pathDirection = Vector2.Dot(firstStart - start, across) < 0 ? -across : across;
                }
                var staggerIndex = rankOptions.StaggerFormation ? index : 0;
                var pathDamage = damage;
                if (rankOptions.StrengthenLastChariot && index == amount - 1)
                {
                    pathDamage = Math.Min(originalBaseDamage * 2.25, damage + originalBaseDamage * LastChariotDamageBonus);
                }
                var path = new ExpandedElijahChariotPathState
                {
                    Id = state.NextPathId,
                    Cycle = state.Cycle,
                    StartX = start.X,
                    StartY = start.Y,
                    DirectionX = pathDirection.X,
                    DirectionY = pathDirection.Y,
                    MoveAt = moveAt + staggerIndex * StaggerPerChariot,
                    FirstDelay = delay == Profile.FirstAttackDelay ? (double?)null : delay,
                    StaggerIndex = staggerIndex,
                    SuppressedTrail = evolved,
                    ForwardKnockback = rankOptions.KnockBack,
                    WiderWhirlwind = evolved && rankOptions.WidenWhirlwind,
                    ExplodesAtEnd = rankOptions.ExplodeAtEnd,
                    RewardsBodyHits = rankOptions.ExplodeAtEnd && rankOptions.RewardBodyHits,
                    FollowsWithWind = rankOptions.FollowWithWind,
                    GuaranteesWind = rankOptions.GuaranteeFollowingWind && index == 0 && state.Cycle % 5 == 0,
                    TracksCrossing = tracksCrossing,
                    MakesEndFlame = evolved && rankOptions.FlameAtNaturalEnd,
                    TurnsMidway = rankOptions.TurnMidway,
                    ReversesAtEnd = rankOptions.ReverseAtNaturalEnd && index == 0 && state.Cycle % 5 == 0,
                    Range = pathRange,
                    Speed = shotRules.LaunchSpeed(Profile.AtLevel(level).Speed, speed, index == 0),
                    Width = shotRules.LaunchCollisionDimension(Profile.AtLevel(1).Width,
                        Math.Min(MaximumWidth, width + Profile.AtLevel(1).Width * shotRules.CollisionBonus), index == 0),
                    Damage = pathDamage,
                    OriginalBaseDamage = originalBaseDamage,
                    SharedShotHitBonuses = shotRules.HitBonuses & PermanentShotHitBonuses.LongFlight,
                };
                if (path.FollowsWithWind || path.GuaranteesWind)
                {
                    path.WidensWind = rankOptions.WidenFollowingWind;
                    path.WidensWindHitShape = rankOptions.WidenSupportShape;
                }
                state.Paths.Add(path);
                state.NextPathId = checked(state.NextPathId + 1);
                if (processionBoost) state.ProcessionUntil = null;
                if (evolved || rankOptions.LeaveFireTrail)
                {
                    if (reserveTrail?.Invoke() != false)
                    {
                        path.SuppressedTrail = false;
                        path.MakesBasicTrail = !evolved;
                        path.ExtendedTrail = evolved && rankOptions.ExtendTrail;
                        path.WidensEvolvedTrail = evolved && rankOptions.LeaveFireTrail;
                    }
                    else recordSupportSuppression?.Invoke();
                }
                if (rankOptions.IgniteOrigin)
                {
                    if (reserveTrail?.Invoke() != false) path.MakesOriginFire = true;
                    else recordSupportSuppression?.Invoke();
                }
                var hasArea = evolved || path.MakesBasicTrail || path.MakesOriginFire || path.ExplodesAtEnd;
                path.WidenedSupportShape = hasArea && rankOptions.WidenSupportShape;
                path.WiderDamageRegion = hasArea && rankOptions.WidenDamageRegion;
                if (path.HasTrail(evolved) || path.MakesOriginFire)
                    path.ResidualDurationBonus = evolved ? rankOptions.ResidualDurationBonus : rankOptions.BasicResidualDurationBonus;
                if (crossFormation && amount == 1 && !state.NextCrossChildAt.HasValue &&
                    AddSupportChariot(path, ExpandedElijahChariotSupportKind.Crossing, path.Origin,
                        Perpendicular(path.Direction), path.MoveAt))
                {
                    path.HasCrossingChild = true;
                    state.NextCrossChildAt = now + SupportChariotCooldown;
                }
            }

            void ResolveNaturalEnd(ExpandedElijahChariotPathState path)
            {
                EmitNaturalEndEffects(path);
                if (path.ReversesAtEnd && !state.NextReverseAt.HasValue &&
                    AddSupportChariot(path, ExpandedElijahChariotSupportKind.Reverse, path.Endpoint, -path.CurrentDirection, path.EndAt))
                    state.NextReverseAt = now + SupportChariotCooldown;
            }

            bool AddSupportChariot(ExpandedElijahChariotPathState path, ExpandedElijahChariotSupportKind kind,
                Vector2 origin, Vector2 direction, double moveAt)
            {
                if (state.ActiveSupportProjectiles >= MaximumExtraGroups || reserveSupportShot?.Invoke() == false)
                {
                    recordSupportSuppression?.Invoke();
                    return false;
                }
                state.SupportChariots ??= new List<ExpandedElijahChariotSupportState>();
                state.SupportChariots.Add(new ExpandedElijahChariotSupportState
                {
                    Kind = kind, SourcePathId = path.Id, Cycle = path.Cycle, StartX = origin.X, StartY = origin.Y,
                    DirectionX = direction.X, DirectionY = direction.Y, MoveAt = moveAt, Range = path.Range, Speed = path.Speed,
                    Width = path.Width, Damage = path.Damage * SupportChariotFactor,
                });
                return true;
            }

            void ScheduleWind(ExpandedElijahChariotPathState path)
            {
                if (path.WindChecked || !path.FollowsWithWind && !path.GuaranteesWind) return;
                path.WindChecked = true;
                var guaranteed = path.GuaranteesWind && !state.NextGuaranteedWindAt.HasValue;
                if (!guaranteed && (!path.FollowsWithWind || state.NextWindAt.HasValue)) return;
                if (state.ActiveSupportProjectiles >= MaximumExtraGroups || reserveSupportShot?.Invoke() == false)
                {
                    recordSupportSuppression?.Invoke();
                    return;
                }
                state.Winds ??= new List<ExpandedElijahChariotWindState>();
                state.Winds.Add(new ExpandedElijahChariotWindState
                {
                    SourcePathId = path.Id, Cycle = path.Cycle, StartX = path.StartX, StartY = path.StartY,
                    DirectionX = path.DirectionX, DirectionY = path.DirectionY, MoveAt = path.MoveAt + WindDelay,
                    Range = path.Range, Speed = path.Speed, Damage = path.Damage * WindFactor(guaranteed),
                    Guaranteed = guaranteed, Wider = path.WidensWind, WiderHitShape = path.WidensWindHitShape,
                    TurnDirectionX = path.TurnDirectionX, TurnDirectionY = path.TurnDirectionY,
                });
                state.NextWindAt = now + WindCooldown;
                if (guaranteed) state.NextGuaranteedWindAt = now + GuaranteedWindCooldown;
            }

            void SpawnSpark(ExpandedElijahChariotHit parent, double originalDamage)
            {
                if (state.NextSparkAt.HasValue || state.Spark != null) return;
                var targetId = targets()
                    .Where(target => target.Key != parent.TargetId)
                    .Select(target => (target.Key, Distance: Vector2.DistanceSquared(target.Value, parent.Position)))
                    .Where(target => target.Distance <= SparkSearchRadius * SparkSearchRadius + TimeEpsilon)
                    .OrderBy(target => target.Distance).ThenBy(target => target.Key)
                    .Select(target => target.Key).FirstOrDefault();
                if (targetId == 0) return;
                if (state.ActiveSupportProjectiles >= MaximumExtraGroups || reserveSupportShot?.Invoke() == false)
                {
                    recordSupportSuppression?.Invoke();
                    return;
                }
                state.Spark = new ExpandedElijahChariotSparkState
                {
                    SourcePathId = parent.PathId, Cycle = parent.Cycle, TargetId = targetId,
                    X = parent.Position.X, Y = parent.Position.Y, Damage = originalDamage * SparkFactor,
                    SpawnedAt = now, SteppedAt = now,
                };
                state.NextSparkAt = now + SparkCooldown;
            }

            void LatchProcession()
            {
                if (state.NextProcessionAt.HasValue) return;
                var expires = now + ProcessionLifetime;
                state.NextProcessionAt = expires;
                state.ProcessionUntil = expires;
            }

            void ResolveCrossing()
            {
                var since = state.IntersectionCheckedAt ?? -1;
                state.IntersectionCheckedAt = now;
                if (state.NextCrossingAt.HasValue ||
                    FindCrossing(state.Paths, since, now) is not { } crossing || !ReserveRegion())
                    return;
                state.NextCrossingAt = now + CrossingCooldown;
                var radius = CrossingRadius * crossing.Source.AreaScale;
                emitCrossingWhirlwind?.Invoke(crossing.Point, radius);
                var damage = crossing.Source.Damage * CrossingFactor;
                foreach (var target in targets().Keys.OrderBy(id => id).ToArray())
                    if (targets().TryGetValue(target, out var position) && WithinRadius(crossing.Point, position, radius))
                        applyHit(Hit(crossing.Source, ExpandedElijahChariotHitKind.CrossingWhirlwind, target, position, damage));
            }

            void EmitNaturalEndEffects(ExpandedElijahChariotPathState path)
            {
                var explosion = path.ExplodesAtEnd && !state.NextEndExplosionAt.HasValue && ReserveRegion();
                var endFlame = path.MakesEndFlame && !state.NextEndFlameAt.HasValue && ReserveRegion();
                if (!explosion && !endFlame) return;

                var center = path.Position;
                var burstRadius = EndExplosionRadius * path.AreaScale;
                var flameRadius = EndFlameRadius * path.AreaScale;
                if (explosion)
                {
                    state.NextEndExplosionAt = now + EndExplosionCooldown;
                    emitEndExplosion?.Invoke(center, burstRadius);
                }
                if (endFlame)
                {
                    state.NextEndFlameAt = now + EndFlameCooldown;
                    emitEndFlame?.Invoke(center, flameRadius);
                }
                var burstFactor = EndExplosionFactor + (path.RewardsBodyHits && path.BodyHitCount >= 6 ? EndExplosionBodyBonus : 0);
                foreach (var target in targets().Keys.OrderBy(id => id).ToArray())
                {
                    if (!targets().TryGetValue(target, out var position)) continue;
                    var receivesBurst = explosion && WithinRadius(center, position, burstRadius);
                    var receivesFlame = endFlame && WithinRadius(center, position, flameRadius);
                    if (receivesFlame && (!receivesBurst || EndFlameFactor >= burstFactor))
                        applyHit(Hit(path, ExpandedElijahChariotHitKind.EndFlame, target, position, path.Damage * EndFlameFactor));
                    else if (receivesBurst)
                        applyHit(Hit(path, ExpandedElijahChariotHitKind.EndExplosion, target, position, path.Damage * burstFactor));
                }
            }

            bool ReserveRegion()
            {
                if (reserveTrail?.Invoke() != false) return true;
                recordSupportSuppression?.Invoke();
                return false;
            }
        }

        private static bool WithinRadius(Vector2 center, Vector2 target, double radius)
        {
            var range = (float)radius;
            return Vector2.DistanceSquared(center, target) <= range * range + TimeEpsilon;
        }

        private static (ExpandedElijahChariotPathState Source, Vector2 Point)? FindCrossing(
            IReadOnlyList<ExpandedElijahChariotPathState> paths, double since, double now)
        {
            (ExpandedElijahChariotPathState Source, Vector2 Point)? earliest = null;
            var earliestAt = double.PositiveInfinity;
            for (var a = 0; a < paths.Count; a++)
            {
                var first = paths[a];
                if (!first.TracksCrossing || first.MoveAt > now + TimeEpsilon ||
                    first.EndAt < since - CrossingWindow - TimeEpsilon) continue;
                for (var b = a + 1; b < paths.Count; b++)
                {
                    var second = paths[b];
                    if (!second.TracksCrossing || second.MoveAt > now + TimeEpsilon ||
                        second.EndAt < since - CrossingWindow - TimeEpsilon ||
                        first.EndAt + CrossingWindow < second.MoveAt - TimeEpsilon ||
                        second.EndAt + CrossingWindow < first.MoveAt - TimeEpsilon)
                        continue;

                    for (var firstIndex = 0; firstIndex < first.SectionCount; firstIndex++)
                    {
                        var firstPiece = first.Section(firstIndex);
                        for (var secondIndex = 0; secondIndex < second.SectionCount; secondIndex++)
                        {
                            var secondPiece = second.Section(secondIndex);
                            var angle = Cross(firstPiece.Direction, secondPiece.Direction);
                            if (Math.Abs(angle) <= 1e-12) continue;
                            var offset = secondPiece.Origin - firstPiece.Origin;
                            var firstDistance = Cross(offset, secondPiece.Direction) / angle;
                            var secondDistance = Cross(offset, firstPiece.Direction) / angle;
                            if (!OnPath(firstDistance, firstPiece.Length) || !OnPath(secondDistance, secondPiece.Length) ||
                                firstPiece.Offset == 0 && secondPiece.Offset == 0 &&
                                firstDistance <= CrossingGeometryEpsilon && secondDistance <= CrossingGeometryEpsilon)
                                continue;
                            firstDistance = Math.Max(0, Math.Min(firstPiece.Length, firstDistance));
                            secondDistance = Math.Max(0, Math.Min(secondPiece.Length, secondDistance));
                            var firstAt = first.MoveAt + (firstPiece.Offset + firstDistance) / first.Speed;
                            var secondAt = second.MoveAt + (secondPiece.Offset + secondDistance) / second.Speed;
                            var at = Math.Max(firstAt, secondAt);
                            if (Math.Abs(firstAt - secondAt) > CrossingWindow + CrossingTimeEpsilon ||
                                at <= since + CrossingTimeEpsilon || at > now + CrossingTimeEpsilon || at >= earliestAt)
                                continue;
                            earliestAt = at;
                            earliest = (firstAt >= secondAt ? first : second,
                                firstPiece.Origin + firstPiece.Direction * (float)firstDistance);
                        }
                    }
                }
            }
            return earliest;

            static double Cross(Vector2 a, Vector2 b) => (double)a.X * b.Y - (double)a.Y * b.X;
            static bool OnPath(double distance, double range) =>
                distance >= -CrossingGeometryEpsilon && distance <= range + CrossingGeometryEpsilon;
        }

        private static void AdvanceSpark(ExpandedElijahChariotState state, double now,
            Func<IReadOnlyDictionary<int, Vector2>> targets, Func<ExpandedElijahChariotHit, bool> applyHit)
        {
            if (state.Spark is not { } spark || now <= spark.SteppedAt + TimeEpsilon) return;
            if (!targets().TryGetValue(spark.TargetId, out var target))
            {
                state.Spark = null;
                return;
            }

            var endAt = spark.SpawnedAt + SparkLifetime;
            var travel = (Math.Min(now, endAt) - spark.SteppedAt) * SparkSpeed;
            var offset = target - spark.Position;
            var length = offset.Length();
            if (length <= travel + SparkGeometryEpsilon)
            {
                state.Spark = null;
                applyHit(new ExpandedElijahChariotHit(spark, target));
            }
            else if (now >= endAt - TimeEpsilon) state.Spark = null;
            else
            {
                var position = spark.Position + offset * (float)(travel / length);
                spark.X = position.X;
                spark.Y = position.Y;
                spark.SteppedAt = now;
            }
        }

        private static void AdvanceWinds(ExpandedElijahChariotState state, double now,
            Func<IReadOnlyDictionary<int, Vector2>> targets, Func<ExpandedElijahChariotHit, bool> applyHit)
        {
            if (state.Winds == null) return;
            var ids = targets().Keys.OrderBy(id => id).ToArray();
            foreach (var wind in state.Winds)
            {
                if (now < wind.MoveAt - TimeEpsilon) continue;
                var from = wind.Distance;
                var to = wind.DistanceAt(now);
                wind.Distance = to;
                var current = targets();
                wind.HitIds.RemoveAll(id => !current.ContainsKey(id));
                for (var index = 0; index < wind.SectionCount; index++)
                {
                    var section = wind.Section(index);
                    if (!SweptBounds(section, from, to, out var start, out var end)) continue;
                    ApplySupportSweep(section, start, end, wind.Width, ids, targets, wind.HitIds,
                        (id, position) => applyHit(new ExpandedElijahChariotHit(wind, id, position, section.Direction)));
                }
            }
            state.Winds.RemoveAll(wind => wind.Distance >= wind.Range);
            if (state.Winds.Count == 0) state.Winds = null;
        }

        private static void AdvanceSupportChariots(ExpandedElijahChariotState state, double now,
            Func<IReadOnlyDictionary<int, Vector2>> targets, Func<ExpandedElijahChariotHit, bool> applyHit)
        {
            if (state.SupportChariots == null) return;
            var ids = targets().Keys.OrderBy(id => id).ToArray();
            foreach (var support in state.SupportChariots)
            {
                if (now < support.MoveAt - TimeEpsilon) continue;
                var from = support.Distance;
                support.Distance = support.DistanceAt(now);
                var current = targets();
                support.HitIds.RemoveAll(id => !current.ContainsKey(id));
                ApplySupportSweep(new ChariotRouteSection(support.Origin, support.Direction, 0, support.Range), from,
                    support.Distance, support.Width, ids, targets, support.HitIds,
                    (id, position) => applyHit(new ExpandedElijahChariotHit(support, id, position)));
            }
            state.SupportChariots.RemoveAll(support => support.Distance >= support.Range);
            if (state.SupportChariots.Count == 0) state.SupportChariots = null;
        }

        private static void ApplySupportSweep(ChariotRouteSection section, double start, double end, double width,
            int[] ids, Func<IReadOnlyDictionary<int, Vector2>> targets, List<int> hitIds, Func<int, Vector2, bool> applyHit)
        {
            foreach (var id in ids)
            {
                if (!targets().TryGetValue(id, out var position) || hitIds.Contains(id)) continue;
                var (forward, side) = Project(section.Origin, section.Direction, position);
                if (forward >= start - TimeEpsilon && forward <= end + TimeEpsilon &&
                    Math.Abs(side) <= width * .5 + TimeEpsilon && applyHit(id, position))
                    hitIds.Add(id);
            }
        }

        internal static Vector2 Perpendicular(Vector2 direction) => new Vector2(-direction.Y, direction.X);

        private static Vector2 ResolveAim(Vector2 player, Vector2 facing, IReadOnlyDictionary<int, Vector2> targets,
            ExpandedElijahChariotRankOptions ranks, Func<int, bool>? isElite, Func<int, bool>? isBoss)
        {
            var candidate = targets.Select(target =>
                {
                    var offset = target.Value - player;
                    var distance = offset.LengthSquared();
                    return (Target: target, Offset: offset, Distance: distance);
                })
                .Where(candidate => candidate.Distance <= AimRadius * AimRadius)
                .Select(candidate => (candidate.Target, candidate.Offset, candidate.Distance,
                    Priority: !ranks.PrioritizeElite ? 0 :
                        isBoss!(candidate.Target.Key) ? 2 : isElite!(candidate.Target.Key) ? 1 : 0))
                .Where(candidate => ranks.PrioritizeCrowds || candidate.Priority > 0)
                .OrderByDescending(candidate => candidate.Priority)
                .ThenByDescending(candidate => ranks.PrioritizeCrowds
                    ? targets.Values.Count(position => Vector2.DistanceSquared(candidate.Target.Value, position) <= CrowdRadius * CrowdRadius)
                    : 0)
                .ThenBy(candidate => candidate.Distance)
                .ThenBy(candidate => candidate.Target.Key)
                .FirstOrDefault();
            return candidate.Offset.LengthSquared() > 1e-12f ? Vector2.Normalize(candidate.Offset) : facing;
        }

        private static Vector2? SoleBossPosition(IReadOnlyDictionary<int, Vector2> targets, Func<int, bool> isBoss)
        {
            if (targets.Count != 1) return null;
            var target = targets.First();
            return isBoss(target.Key) ? target.Value : (Vector2?)null;
        }

        private static void AdvancePaths(ExpandedElijahChariotState state, double now,
            Func<IReadOnlyDictionary<int, Vector2>> targets, Func<ExpandedElijahChariotHit, bool> applyHit,
            Func<ExpandedElijahChariotHit, double?> applyOriginalHit, Action<ExpandedElijahChariotHit, double>? spawnSpark,
            Action<ExpandedElijahChariotPathState> emitEndExplosion, Action<ExpandedElijahChariotPathState> scheduleWind,
            Func<ExpandedElijahChariotSweep, bool>? tryDeflect, Func<int, bool>? isEliteForProcession,
            Action latchProcession, Action? resolveCrossing)
        {
            if (state.Paths.Count == 0)
            {
                resolveCrossing?.Invoke();
                return;
            }
            var ids = targets().Keys.OrderBy(id => id).ToArray();
            foreach (var path in state.Paths)
            {
                if (path.MoveAt > now + TimeEpsilon || path.BodyEnded) continue;
                scheduleWind(path);
                var from = path.Distance;
                AdvanceBody(state, path, now, ids, targets, applyHit, applyOriginalHit, spawnSpark,
                    isEliteForProcession, latchProcession);
                if (tryDeflect != null && !state.NextDeflectAt.HasValue && path.Distance > from + TimeEpsilon)
                {
                    var bend = path.TurnDirection.HasValue && from < path.PivotDistance - TimeEpsilon &&
                        path.Distance > path.PivotDistance + TimeEpsilon;
                    var firstEnd = bend ? path.PivotDistance : path.Distance;
                    if (tryDeflect(new ExpandedElijahChariotSweep(path, from, firstEnd)) ||
                        bend && tryDeflect(new ExpandedElijahChariotSweep(path, firstEnd, path.Distance)))
                        state.NextDeflectAt = now + DeflectCooldown;
                }
                if (path.BodyEnded) emitEndExplosion(path);
            }
            resolveCrossing?.Invoke();
            AdvanceTrails(state.Paths, state.Evolved, now, ids, targets, applyHit);
            AdvanceOriginFires(state.Paths, now, ids, targets, applyHit);
            foreach (var path in state.Paths)
            {
                if (path.MakesOriginFire && path.OriginFireExpiresAt <= now + TimeEpsilon)
                {
                    path.MakesOriginFire = false;
                    path.OriginFireTicks = 0;
                }
                if (path.MakesBasicTrail && path.TrailExpiresAt <= now + TimeEpsilon)
                {
                    path.MakesBasicTrail = false;
                    path.TrailTicksFired = 0;
                }
                if (!path.HasTrail(state.Evolved) && !path.MakesOriginFire)
                    path.ResidualDurationBonus = 0;
                if (!state.Evolved && !path.MakesBasicTrail && !path.MakesOriginFire && !path.ExplodesAtEnd)
                {
                    path.WidenedSupportShape = false;
                    path.WiderDamageRegion = false;
                }
            }
            state.Paths.RemoveAll(path => path.ExpiresAt(state.Evolved) <= now + TimeEpsilon);
        }

        private static void AdvanceBody(ExpandedElijahChariotState state, ExpandedElijahChariotPathState path, double now, int[] ids,
            Func<IReadOnlyDictionary<int, Vector2>> targets, Func<ExpandedElijahChariotHit, bool> applyHit,
            Func<ExpandedElijahChariotHit, double?> applyOriginalHit, Action<ExpandedElijahChariotHit, double>? spawnSpark,
            Func<int, bool>? isEliteForProcession, Action latchProcession)
        {
            // Actor IDs are occurrence IDs and cannot be reused before the arena resets.
            // Keep live deduplication separately from the total for this original path.
            var current = targets();
            path.BodyHitIds.RemoveAll(id => !current.ContainsKey(id));
            path.WhirlwindCandidateIds.RemoveAll(id => !current.ContainsKey(id));
            var from = path.Distance;
            var to = path.DistanceAt(now);
            var whirlwindRadius = path.WhirlwindRadius;
            if (path.TurnsMidway && !path.MidpointTurnResolved && to + TimeEpsilon >= path.PivotDistance)
            {
                Sweep(path.Section(0), from, Math.Min(to, path.PivotDistance));
                ResolveMidpoint(state, path, targets());
                if (to > path.PivotDistance + TimeEpsilon)
                    Sweep(path.Section(path.SectionCount - 1), path.PivotDistance, to);
            }
            else
            {
                for (var index = 0; index < path.SectionCount; index++)
                    Sweep(path.Section(index), from, to);
            }

            path.Distance = to;
            if (!path.BodyEnded || !state.Evolved) return;
            // A later original body contact suppresses this path's outer overlap.
            foreach (var id in path.WhirlwindCandidateIds.OrderBy(id => id))
                if (targets().TryGetValue(id, out var position))
                    applyHit(Hit(path, ExpandedElijahChariotHitKind.Whirlwind, id, position, path.Damage * WhirlwindDamageFactor));
            path.WhirlwindCandidateIds.Clear();

            void Sweep(ChariotRouteSection section, double startAt, double endAt)
            {
                if (!SweptBounds(section, startAt, endAt, out var start, out var end)) return;
                foreach (var id in ids)
                {
                    if (!targets().TryGetValue(id, out var position) || path.BodyHitIds.Contains(id)) continue;
                    var (forward, side) = Project(section.Origin, section.Direction, position);
                    var crossesBody = forward >= start - TimeEpsilon && forward <= end + TimeEpsilon &&
                        Math.Abs(side) <= path.Width * .5 + TimeEpsilon;
                    if (crossesBody)
                    {
                        var damageAtContact = PermanentShotRules.HitDamage(path.SharedShotHitBonuses, path.OriginalBaseDamage,
                            path.Damage, null, section.Offset + Math.Max(start, Math.Min(end, forward)));
                        var hit = new ExpandedElijahChariotHit(ExpandedElijahChariotHitKind.Body, id, position,
                            damageAtContact, path, section.Direction);
                        var elite = isEliteForProcession?.Invoke(id) == true;
                        var damage = applyOriginalHit(hit);
                        if (damage.HasValue)
                        {
                            path.BodyHitIds.Add(id);
                            path.BodyHitCount++;
                            path.WhirlwindCandidateIds.Remove(id);
                            if (elite && !targets().ContainsKey(id)) latchProcession();
                            spawnSpark?.Invoke(hit, damage.Value);
                            continue;
                        }
                    }
                    if (state.Evolved && !path.WhirlwindCandidateIds.Contains(id))
                    {
                        var outside = Math.Max(0, Math.Max(start - forward, forward - end));
                        if (side * side + outside * outside <= whirlwindRadius * whirlwindRadius + TimeEpsilon)
                            path.WhirlwindCandidateIds.Add(id);
                    }
                }
            }
        }

        private static bool SweptBounds(ChariotRouteSection section, double from, double to, out double start, out double end)
        {
            start = Math.Max(0, from - section.Offset);
            end = Math.Min(section.Length, to - section.Offset);
            return start <= end + TimeEpsilon;
        }

        private static void ResolveMidpoint(ExpandedElijahChariotState state, ExpandedElijahChariotPathState path,
            IReadOnlyDictionary<int, Vector2> targets)
        {
            path.MidpointTurnResolved = true;
            var pivot = path.Pivot;
            var nearest = targets.Select(target => (target.Key, target.Value, Distance: Vector2.DistanceSquared(pivot, target.Value)))
                .Where(candidate => candidate.Distance > 1e-12f)
                .OrderBy(candidate => candidate.Distance)
                .ThenBy(candidate => candidate.Key)
                .FirstOrDefault(candidate => targets.Any(peer => peer.Key != candidate.Key &&
                    Vector2.DistanceSquared(peer.Value, candidate.Value) <= CrowdRadius * CrowdRadius));
            if (nearest.Key == 0) return;

            var heading = path.Direction;
            var toward = Vector2.Normalize(nearest.Value - pivot);
            var angle = Math.Atan2(heading.X * toward.Y - heading.Y * toward.X, Vector2.Dot(heading, toward));
            var turn = Math.Max(-MaxTurnRadians, Math.Min(MaxTurnRadians, angle));
            if (Math.Abs(turn) <= TimeEpsilon) return;
            var cos = Math.Cos(turn);
            var sin = Math.Sin(turn);
            path.TurnDirectionX = (float)(heading.X * cos - heading.Y * sin);
            path.TurnDirectionY = (float)(heading.X * sin + heading.Y * cos);
            var wind = state.Winds?.Find(candidate => candidate.SourcePathId == path.Id);
            if (wind != null)
            {
                wind.TurnDirectionX = path.TurnDirectionX;
                wind.TurnDirectionY = path.TurnDirectionY;
            }
        }

        private static void AdvanceTrails(List<ExpandedElijahChariotPathState> paths, bool evolved, double now, int[] ids,
            Func<IReadOnlyDictionary<int, Vector2>> targets, Func<ExpandedElijahChariotHit, bool> applyHit)
        {
            Dictionary<int, ExpandedElijahChariotPathState>? winners = null;
            foreach (var path in paths)
            {
                if (!path.HasTrail(evolved)) continue;
                var expires = path.TrailExpiresAt;
                var at = path.TrailAt(path.TrailTicksFired);
                while (at <= now + TimeEpsilon && at < expires - TimeEpsilon)
                {
                    foreach (var id in ids)
                    {
                        if (!targets().TryGetValue(id, out var position) || !InLiveTrail(path, position, at)) continue;
                        winners ??= new Dictionary<int, ExpandedElijahChariotPathState>();
                        if (!winners.TryGetValue(id, out var previous) ||
                            path.Damage > previous.Damage || path.Damage == previous.Damage && path.Id < previous.Id)
                            winners[id] = path;
                    }
                    at = path.TrailAt(++path.TrailTicksFired);
                }
            }
            if (winners == null) return;
            // Chariot trails share one residual-hit group for this fixed tick.
            foreach (var winner in winners.OrderBy(entry => entry.Value.Id).ThenBy(entry => entry.Key))
                if (targets().TryGetValue(winner.Key, out var position))
                    applyHit(Hit(winner.Value, ExpandedElijahChariotHitKind.Trail, winner.Key, position,
                        winner.Value.Damage * (evolved ? TrailDamageFactor : BasicTrailFactor)));
        }

        private static void AdvanceOriginFires(List<ExpandedElijahChariotPathState> paths, double now, int[] ids,
            Func<IReadOnlyDictionary<int, Vector2>> targets, Func<ExpandedElijahChariotHit, bool> applyHit)
        {
            Dictionary<int, ExpandedElijahChariotPathState>? winners = null;
            foreach (var path in paths)
            {
                if (!path.MakesOriginFire) continue;
                var radius = (float)path.OriginFireRadius;
                var sampled = false;
                while (path.OriginFireTicks < path.OriginFireTickLimit && path.TrailAt(path.OriginFireTicks) <= now + TimeEpsilon)
                {
                    path.OriginFireTicks++;
                    sampled = true;
                }
                if (!sampled) continue;
                foreach (var id in ids)
                {
                    if (!targets().TryGetValue(id, out var position) ||
                        Vector2.DistanceSquared(path.Origin, position) > radius * radius + TimeEpsilon)
                        continue;
                    winners ??= new Dictionary<int, ExpandedElijahChariotPathState>();
                    if (!winners.TryGetValue(id, out var previous) ||
                        path.Damage > previous.Damage || path.Damage == previous.Damage && path.Id < previous.Id)
                        winners[id] = path;
                }
            }
            if (winners == null) return;
            foreach (var winner in winners.OrderBy(entry => entry.Value.Id).ThenBy(entry => entry.Key))
                if (targets().TryGetValue(winner.Key, out var position))
                    applyHit(Hit(winner.Value, ExpandedElijahChariotHitKind.OriginFire, winner.Key, position,
                        winner.Value.Damage * OriginFireFactor));
        }

        private static bool InLiveTrail(ExpandedElijahChariotPathState path, Vector2 target, double at)
        {
            for (var index = 0; index < path.SectionCount; index++)
            {
                var section = path.Section(index);
                var (forward, side) = Project(section.Origin, section.Direction, target);
                if (forward < -TimeEpsilon || forward > section.Length + TimeEpsilon ||
                    Math.Abs(side) > path.TrailWidth * .5 + TimeEpsilon)
                    continue;
                var laidAt = path.MoveAt + (section.Offset + Math.Max(0, forward)) / path.Speed;
                if (laidAt <= at + TimeEpsilon && at < laidAt + path.TrailDuration - TimeEpsilon)
                    return true;
            }
            return false;
        }

        private static (double Forward, double Side) Project(Vector2 origin, Vector2 direction, Vector2 target)
        {
            var x = (double)target.X - origin.X;
            var y = (double)target.Y - origin.Y;
            return (x * direction.X + y * direction.Y, -x * direction.Y + y * direction.X);
        }

        internal static bool ValidTurn(Vector2 initial, float? x, float? y)
        {
            if (x.HasValue != y.HasValue) return false;
            if (!x.HasValue) return true;
            var turn = new Vector2(x.Value, y!.Value);
            return BattleSnapshot.Position(turn.X, turn.Y) && Math.Abs(turn.LengthSquared() - 1) <= .0001 &&
                Vector2.Dot(initial, turn) >= Math.Cos(MaxTurnRadians) - .000001;
        }

        internal static double WindFactor(bool guaranteed) => guaranteed ? .40 : .25;

        internal static bool ValidSupportChariot(ExpandedElijahChariotSupportState support, double now) =>
            (support.Kind == ExpandedElijahChariotSupportKind.Reverse ? support.Cycle % 5 == 0 && support.MoveAt <= now + TimeEpsilon :
                support.Kind == ExpandedElijahChariotSupportKind.Crossing && support.Cycle % 6 == 0 &&
                support.MoveAt <= now + Profile.FirstAttackDelay + TimeEpsilon) &&
            BattleSnapshot.Position(support.StartX, support.StartY) &&
            BattleSnapshot.Position(support.DirectionX, support.DirectionY) &&
            Math.Abs(support.Direction.LengthSquared() - 1) <= .0001 &&
            BattleSnapshot.Finite(support.MoveAt) && support.MoveAt >= 0 &&
            BattleSnapshot.Finite(support.Range) && support.Range > 0 && support.Range <= MaximumRange &&
            BattleSnapshot.Finite(support.Speed) && support.Speed >= MinimumSpeed && support.Speed <= MaximumSpeed &&
            BattleSnapshot.Finite(support.Width) && support.Width > 0 && support.Width <= MaximumWidth &&
            BattleSnapshot.Finite(support.Damage) && support.Damage >= SupportChariotFactor &&
            support.Damage <= MaximumDamage * SupportChariotFactor &&
            BattleSnapshot.Finite(support.Distance) && support.Distance >= 0 && support.Distance < support.Range &&
            Math.Abs(support.Distance - support.DistanceAt(now)) <= .000001 && support.EndAt > now + TimeEpsilon &&
            support.HitIds != null && support.HitIds.Count <= MaximumLiveTargets &&
            support.HitIds.All(id => id > 0) && support.HitIds.Distinct().Count() == support.HitIds.Count &&
            (support.MoveAt <= now + TimeEpsilon || support.HitIds.Count == 0);

        internal static bool ValidWind(ExpandedElijahChariotWindState wind, double now) =>
            BattleSnapshot.Position(wind.StartX, wind.StartY) && BattleSnapshot.Position(wind.DirectionX, wind.DirectionY) &&
            Math.Abs(wind.Direction.LengthSquared() - 1) <= .0001 &&
            ValidTurn(wind.Direction, wind.TurnDirectionX, wind.TurnDirectionY) &&
            BattleSnapshot.Finite(wind.MoveAt) && wind.MoveAt >= WindDelay && wind.MoveAt <= now + WindDelay + TimeEpsilon &&
            BattleSnapshot.Finite(wind.Range) && wind.Range > 0 && wind.Range <= MaximumRange &&
            BattleSnapshot.Finite(wind.Speed) && wind.Speed >= MinimumSpeed && wind.Speed <= MaximumSpeed &&
            BattleSnapshot.Finite(wind.Damage) && wind.Damage >= WindFactor(wind.Guaranteed) &&
            wind.Damage <= MaximumDamage * WindFactor(wind.Guaranteed) &&
            BattleSnapshot.Finite(wind.Distance) && wind.Distance >= 0 && wind.Distance < wind.Range &&
            Math.Abs(wind.Distance - wind.DistanceAt(now)) <= .000001 && wind.EndAt > now + TimeEpsilon &&
            wind.HitIds != null && wind.HitIds.Count <= MaximumLiveTargets &&
            wind.HitIds.All(id => id > 0) && wind.HitIds.Distinct().Count() == wind.HitIds.Count &&
            (wind.MoveAt <= now + TimeEpsilon || wind.HitIds.Count == 0);

        private static ExpandedElijahChariotHit Hit(ExpandedElijahChariotPathState path, ExpandedElijahChariotHitKind kind,
            int targetId, Vector2 position, double damage) =>
            new ExpandedElijahChariotHit(kind, targetId, position, damage, path);

        internal static bool ValidCooldown(double? at, double now, double cooldown) =>
            !at.HasValue || BattleSnapshot.Finite(at.Value) && at.Value >= now - TimeEpsilon && at.Value <= now + cooldown + TimeEpsilon;

        internal static bool ValidDelay(double? seconds) => !seconds.HasValue ||
            (BattleSnapshot.Finite(seconds.Value) && seconds.Value >= MinimumDelay && seconds.Value <= Profile.FirstAttackDelay);

        internal static bool ValidOriginalBaseDamage(double value, bool evolved) => value == 0 ||
            (evolved ? value == Profile.AtLevel(8).Damage : OriginalDamageByLevel.Contains(value));

        internal static bool ValidPath(double damage, double width, double speed, double range) =>
            BattleSnapshot.Finite(damage) && damage >= 1 && damage <= MaximumDamage &&
            BattleSnapshot.Finite(width) && width > 0 && width <= MaximumWidth &&
            BattleSnapshot.Finite(speed) && speed >= MinimumSpeed && speed <= MaximumSpeed &&
            BattleSnapshot.Finite(range) && range > 0 && range <= MaximumRange;
    }
}
