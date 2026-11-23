using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.Serialization;

namespace BibleSurvivor.Core
{
    public enum ExpandedDanielLionHitKind
    {
        Claw,
        Leap,
        Roar,
        KillFollowupLeap,
        PackLeap,
        ReturnClaw,
    }

    public readonly struct ExpandedDanielLionRankOptions
    {
        public bool PrioritizeElite { get; }
        public bool ExtendLeash { get; }
        public bool SpreadNormalTargets { get; }
        public bool StaggerFirstBossClaw { get; }
        public bool EmpowerLeap { get; }
        public bool AddPackRoar { get; }
        public bool WidenRoar { get; }
        public bool StrengthenRoarKnockback { get; }
        public bool HastenClawsAfterRoar { get; }
        public bool StaggerPackRoar { get; }
        public bool WidenEvolvedClaw { get; }
        public bool FollowUpLeapKill { get; }
        public bool WidenCommonArea { get; }
        public bool WidenDamageRegion { get; }
        public bool PounceOnPursuit { get; }
        public bool ReacquireAfterDeath { get; }
        public bool FinishLowNormalTargets { get; }
        public bool CooperateOnClaw { get; }
        public bool MarkEliteOnClaw { get; }
        public bool AddPackLeap { get; }
        public bool PatrolNearPlayer { get; }
        public bool LeapAtPatrolCrowd { get; }
        public bool ClawWhileReturning { get; }
        public bool EvolvedHuntMark { get; }
        public bool ProtectAfterDamage { get; }
        public bool GuardAfterEliteKill { get; }
        public bool DeflectWithGuardianRoar { get; }

        public ExpandedDanielLionRankOptions(bool prioritizeElite = false, bool extendLeash = false,
            bool spreadNormalTargets = false, bool staggerFirstBossClaw = false, bool empowerLeap = false,
            bool addPackRoar = false, bool widenRoar = false, bool strengthenRoarKnockback = false,
            bool hastenClawsAfterRoar = false, bool staggerPackRoar = false, bool widenEvolvedClaw = false,
            bool followUpLeapKill = false, bool widenCommonArea = false, bool widenDamageRegion = false,
            bool pounceOnPursuit = false, bool reacquireAfterDeath = false, bool finishLowNormalTargets = false,
            bool cooperateOnClaw = false, bool markEliteOnClaw = false, bool addPackLeap = false,
            bool patrolNearPlayer = false, bool leapAtPatrolCrowd = false, bool clawWhileReturning = false,
            bool evolvedHuntMark = false, bool protectAfterDamage = false, bool guardAfterEliteKill = false,
            bool deflectWithGuardianRoar = false)
        {
            PrioritizeElite = prioritizeElite;
            ExtendLeash = extendLeash;
            SpreadNormalTargets = spreadNormalTargets;
            StaggerFirstBossClaw = staggerFirstBossClaw;
            EmpowerLeap = empowerLeap;
            AddPackRoar = addPackRoar;
            WidenRoar = widenRoar;
            StrengthenRoarKnockback = strengthenRoarKnockback;
            HastenClawsAfterRoar = hastenClawsAfterRoar;
            StaggerPackRoar = staggerPackRoar;
            WidenEvolvedClaw = widenEvolvedClaw;
            FollowUpLeapKill = followUpLeapKill;
            WidenCommonArea = widenCommonArea;
            WidenDamageRegion = widenDamageRegion;
            PounceOnPursuit = pounceOnPursuit;
            ReacquireAfterDeath = reacquireAfterDeath;
            FinishLowNormalTargets = finishLowNormalTargets;
            CooperateOnClaw = cooperateOnClaw;
            MarkEliteOnClaw = markEliteOnClaw;
            AddPackLeap = addPackLeap;
            PatrolNearPlayer = patrolNearPlayer;
            LeapAtPatrolCrowd = leapAtPatrolCrowd;
            ClawWhileReturning = clawWhileReturning;
            EvolvedHuntMark = evolvedHuntMark;
            ProtectAfterDamage = protectAfterDamage;
            GuardAfterEliteKill = guardAfterEliteKill;
            DeflectWithGuardianRoar = deflectWithGuardianRoar;
        }

        public static ExpandedDanielLionRankOptions FromRanks(IReadOnlyDictionary<string, int> ranks)
        {
            if (ranks == null) throw new ArgumentNullException(nameof(ranks));
            bool Has(string id) => ranks.TryGetValue(id, out var rank) && rank > 0;
            return new ExpandedDanielLionRankOptions(Has("weapon.daniel_lion.a.01"), Has("weapon.daniel_lion.a.02"),
                Has("weapon.daniel_lion.b.01"), Has("weapon.daniel_lion.b.02"), Has("weapon.daniel_lion.e.01"),
                Has("weapon.daniel_lion.c.01"), Has("weapon.daniel_lion.c.02"), Has("weapon.daniel_lion.c.03"),
                Has("weapon.daniel_lion.c.04"), Has("weapon.daniel_lion.c.05"), Has("weapon.daniel_lion.e.03"), Has("weapon.daniel_lion.e.02"), Has("common.area.01"), Has("common.area.09"),
                Has("weapon.daniel_lion.a.03"), Has("weapon.daniel_lion.a.04"), Has("weapon.daniel_lion.a.05"),
                Has("weapon.daniel_lion.b.03"), Has("weapon.daniel_lion.b.04"), Has("weapon.daniel_lion.b.05"),
                Has("weapon.daniel_lion.d.01"), Has("weapon.daniel_lion.d.02"), Has("weapon.daniel_lion.d.03"),
                Has("weapon.daniel_lion.e.05"), Has("weapon.daniel_lion.d.04"), Has("weapon.daniel_lion.d.05"),
                Has("weapon.daniel_lion.e.04"));
        }

        public double RoarRadius(bool guardian) =>
            (guardian ? ExpandedDanielLion.GuardianRoarRadius : ExpandedDanielLion.PackRoarRadius) *
            (1 + (WidenRoar ? .15 : 0) + (WidenCommonArea ? .04 : 0) + (WidenDamageRegion ? .06 : 0));
    }

    public readonly struct ExpandedDanielLionHit
    {
        public ExpandedDanielLionHitKind Kind { get; }
        public int LionIndex { get; }
        public int TargetId { get; }
        public Vector2 Position { get; }
        public Vector2 Center { get; }
        public double Damage { get; }
        public double OriginalBaseDamage { get; }
        public int TargetOrdinal { get; }
        public double Knockback { get; }

        internal ExpandedDanielLionHit(ExpandedDanielLionHitKind kind, int lionIndex, int targetId,
            Vector2 position, Vector2 center, double damage, double knockback = 0, double originalBaseDamage = 0)
        {
            Kind = kind;
            LionIndex = lionIndex;
            TargetId = targetId;
            Position = position;
            Center = center;
            Damage = damage;
            OriginalBaseDamage = kind == ExpandedDanielLionHitKind.Claw ? originalBaseDamage : 0;
            TargetOrdinal = kind == ExpandedDanielLionHitKind.Claw ? 1 : 0;
            Knockback = knockback;
        }
    }

    [DataContract]
    public sealed class ExpandedDanielLionPounceState
    {
        [DataMember(IsRequired = true)] public double StartedAt { get; set; }
        [DataMember(IsRequired = true)] public float StartX { get; set; }
        [DataMember(IsRequired = true)] public float StartY { get; set; }
        [DataMember(IsRequired = true)] public float EndX { get; set; }
        [DataMember(IsRequired = true)] public float EndY { get; set; }

        public Vector2 Start => new Vector2(StartX, StartY);
        public Vector2 End => new Vector2(EndX, EndY);
        public double EndsAt => StartedAt + ExpandedDanielLion.PounceDuration;
        public Vector2 PositionAt(double now) => Vector2.Lerp(Start, End,
            (float)Math.Max(0, Math.Min(1, (now - StartedAt) / ExpandedDanielLion.PounceDuration)));

        internal ExpandedDanielLionPounceState Copy() => (ExpandedDanielLionPounceState)MemberwiseClone();
    }

    [DataContract]
    public sealed class ExpandedDanielLionActorState
    {
        [DataMember(IsRequired = true)] public int Index { get; set; }
        [DataMember(IsRequired = true)] public float X { get; set; }
        [DataMember(IsRequired = true)] public float Y { get; set; }
        [DataMember(IsRequired = true)] public int TargetId { get; set; }
        [DataMember(IsRequired = true)] public bool Returning { get; set; }
        [DataMember(IsRequired = true)] public double NextClawAt { get; set; }
        [DataMember(IsRequired = true)] public bool WaitingForTarget { get; set; }
        [DataMember(EmitDefaultValue = false)] public bool ExtendedLeash { get; set; }
        [DataMember(EmitDefaultValue = false)] public bool FirstClawPhaseResolved { get; set; }
        [DataMember(EmitDefaultValue = false)] public double? NextPackRoarAt { get; set; }
        [DataMember(EmitDefaultValue = false)] public double? ClawHasteUntil { get; set; }
        [DataMember(EmitDefaultValue = false)] public double? NextPounceAt { get; set; }
        [DataMember(EmitDefaultValue = false)] public ExpandedDanielLionPounceState? Pounce { get; set; }
        [DataMember(EmitDefaultValue = false)] public double? PatrolUntil { get; set; }
        [DataMember(EmitDefaultValue = false)] public double PatrolAngle { get; set; }
        [DataMember(EmitDefaultValue = false)] public ExpandedDanielLionPounceState? PatrolLeap { get; set; }
        [DataMember(EmitDefaultValue = false)] public List<int>? ReturnHitIds { get; set; }

        public Vector2 Position
        {
            get => new Vector2(X, Y);
            internal set { X = value.X; Y = value.Y; }
        }

        internal ExpandedDanielLionActorState Copy()
        {
            var copy = (ExpandedDanielLionActorState)MemberwiseClone();
            copy.Pounce = Pounce?.Copy();
            copy.PatrolLeap = PatrolLeap?.Copy();
            copy.ReturnHitIds = ReturnHitIds?.ToList();
            return copy;
        }
    }

    [DataContract]
    public sealed class ExpandedDanielLionLeapState
    {
        [DataMember(IsRequired = true)] public double At { get; set; }
        [DataMember(IsRequired = true)] public float OriginX { get; set; }
        [DataMember(IsRequired = true)] public float OriginY { get; set; }
        [DataMember(IsRequired = true)] public double Damage { get; set; }
        [DataMember(IsRequired = true)] public List<int> VisitedIds { get; set; } = new List<int>();
        [DataMember(EmitDefaultValue = false)] public bool EmpoweredDamage { get; set; }
        [DataMember(EmitDefaultValue = false)] public bool FollowupOnKill { get; set; }

        public Vector2 Origin
        {
            get => new Vector2(OriginX, OriginY);
            internal set { OriginX = value.X; OriginY = value.Y; }
        }

        internal ExpandedDanielLionLeapState Copy()
        {
            var copy = (ExpandedDanielLionLeapState)MemberwiseClone();
            copy.VisitedIds = new List<int>(VisitedIds);
            return copy;
        }
    }

    [DataContract]
    public sealed class ExpandedDanielLionClawReceipt
    {
        [DataMember(IsRequired = true)] public int LionIndex { get; set; }
        [DataMember(IsRequired = true)] public double At { get; set; }
        internal ExpandedDanielLionClawReceipt Copy() => (ExpandedDanielLionClawReceipt)MemberwiseClone();
    }

    [DataContract]
    public sealed class ExpandedDanielLionTargetHistory
    {
        [DataMember(IsRequired = true)] public int TargetId { get; set; }
        [DataMember(EmitDefaultValue = false)] public double? MarkUntil { get; set; }
        [DataMember(EmitDefaultValue = false)] public List<ExpandedDanielLionClawReceipt>? RecentClaws { get; set; }

        internal ExpandedDanielLionTargetHistory Copy() => new ExpandedDanielLionTargetHistory
        {
            TargetId = TargetId,
            MarkUntil = MarkUntil,
            RecentClaws = RecentClaws?.Select(claw => claw.Copy()).ToList(),
        };
    }

    [DataContract]
    public sealed class ExpandedDanielLionState
    {
        [DataMember(IsRequired = true)] public bool Evolved { get; set; }
        [DataMember(IsRequired = true)] public double LastStepAt { get; set; }
        [DataMember(IsRequired = true)] public double NextRoarAt { get; set; }
        [DataMember(IsRequired = true)] public long GuardianHits { get; set; }
        [DataMember(IsRequired = true)] public List<ExpandedDanielLionActorState> Lions { get; set; } =
            new List<ExpandedDanielLionActorState>();
        [DataMember(EmitDefaultValue = false)] public ExpandedDanielLionLeapState? Leap { get; set; }
        [DataMember(EmitDefaultValue = false)] public double? KillLeapReadyAt { get; set; }
        [DataMember(EmitDefaultValue = false)] public List<ExpandedDanielLionTargetHistory>? TargetHistory { get; set; }
        [DataMember(EmitDefaultValue = false)] public long PackCycle { get; set; }
        [DataMember(EmitDefaultValue = false)] public double? NextPackLeapAt { get; set; }
        [DataMember(EmitDefaultValue = false)] public double? NextPatrolAt { get; set; }
        [DataMember(EmitDefaultValue = false)] public double? NextPatrolLeapAt { get; set; }
        [DataMember(EmitDefaultValue = false)] public double? NextProtectionAt { get; set; }
        [DataMember(EmitDefaultValue = false)] public double? NextGuardAt { get; set; }
        [DataMember(EmitDefaultValue = false)] public double? GuardUntil { get; set; }
        [DataMember(EmitDefaultValue = false)] public double? NextRoarDeflectAt { get; set; }

        public double DefenseBonusAt(double now) => GuardUntil > now + ExpandedDanielLion.TimeEpsilon ? 2 : 0;

        public ExpandedDanielLionState Copy() => new ExpandedDanielLionState
        {
            Evolved = Evolved,
            LastStepAt = LastStepAt,
            NextRoarAt = NextRoarAt,
            GuardianHits = GuardianHits,
            Lions = Lions.Select(lion => lion.Copy()).ToList(),
            Leap = Leap?.Copy(),
            KillLeapReadyAt = KillLeapReadyAt,
            TargetHistory = TargetHistory?.Select(history => history.Copy()).ToList(),
            PackCycle = PackCycle,
            NextPackLeapAt = NextPackLeapAt,
            NextPatrolAt = NextPatrolAt,
            NextPatrolLeapAt = NextPatrolLeapAt,
            NextProtectionAt = NextProtectionAt,
            NextGuardAt = NextGuardAt,
            GuardUntil = GuardUntil,
            NextRoarDeflectAt = NextRoarDeflectAt,
        };

        // Form change cancels reservations without moving the other permanent summons.
        public void ResetForForm(bool evolved, double now)
        {
            if (!BattleSnapshot.Finite(now) || now < 0) throw new ArgumentOutOfRangeException(nameof(now));
            Evolved = evolved;
            LastStepAt = now;
            NextRoarAt = evolved ? now + ExpandedDanielLion.RoarInterval : 0;
            GuardianHits = 0;
            Leap = null;
            KillLeapReadyAt = null;
            if (!evolved) NextRoarDeflectAt = null;
            foreach (var lion in Lions)
            {
                lion.TargetId = 0;
                lion.NextClawAt = now;
                lion.WaitingForTarget = false;
                lion.NextPackRoarAt = null;
                lion.ClawHasteUntil = null;
                lion.Pounce = null;
                lion.PatrolUntil = null;
                lion.PatrolAngle = 0;
                lion.PatrolLeap = null;
                lion.ReturnHitIds = null;
            }
        }

        public void Validate(double now, bool evolved)
        {
            const double epsilon = ExpandedDanielLion.TimeEpsilon;
            if (!BattleSnapshot.Finite(now) || now < 0 || Evolved != evolved ||
                !BattleSnapshot.Finite(LastStepAt) || LastStepAt < 0 || LastStepAt > now + epsilon ||
                Lions == null || Lions.Count > ExpandedDanielLion.MaximumAmount ||
                GuardianHits < 0 || GuardianHits > 0 && Lions.Count == 0 || PackCycle < 0 ||
                PackCycle > Math.Ceiling(LastStepAt / ExpandedDanielLion.MinimumCycle) + 1 ||
                NextPackLeapAt.HasValue && PackCycle < ExpandedDanielLion.ClawsPerPackLeap ||
                !ExpandedDanielLion.ValidFutureTime(NextPackLeapAt, LastStepAt, ExpandedDanielLion.PackLeapCooldown) ||
                !ExpandedDanielLion.ValidFutureTime(NextPatrolAt, LastStepAt, ExpandedDanielLion.PatrolInterval) ||
                !ExpandedDanielLion.ValidFutureTime(NextPatrolLeapAt, LastStepAt, ExpandedDanielLion.PatrolInterval) ||
                !ExpandedDanielLion.ValidFutureTime(NextProtectionAt, now, ExpandedDanielLion.ProtectionCooldown) ||
                !ExpandedDanielLion.ValidFutureTime(NextGuardAt, LastStepAt, ExpandedDanielLion.GuardCooldown) ||
                !ExpandedDanielLion.ValidFutureTime(GuardUntil, LastStepAt, ExpandedDanielLion.GuardDuration) ||
                GuardUntil.HasValue && (!NextGuardAt.HasValue ||
                    Math.Abs(NextGuardAt.Value - GuardUntil.Value - ExpandedDanielLion.GuardCooldown + ExpandedDanielLion.GuardDuration) > epsilon) ||
                !ExpandedDanielLion.ValidFutureTime(NextRoarDeflectAt, LastStepAt, ExpandedDanielLion.RoarDeflectCooldown) ||
                !evolved && NextRoarDeflectAt.HasValue ||
                (evolved ? !BattleSnapshot.Finite(NextRoarAt) || NextRoarAt < LastStepAt - epsilon ||
                    NextRoarAt > LastStepAt + ExpandedDanielLion.RoarInterval + epsilon ||
                    !ExpandedDanielLion.ValidFutureTime(KillLeapReadyAt, LastStepAt, ExpandedDanielLion.KillLeapCooldown) :
                    NextRoarAt != 0 || GuardianHits != 0 || Leap != null || KillLeapReadyAt.HasValue))
                throw new ArgumentException("Invalid Daniel lion clock");

            var patrollers = 0;
            for (var index = 0; index < Lions.Count; index++)
            {
                var lion = Lions[index];
                if (lion == null || lion.Index != index || !BattleSnapshot.Position(lion.X, lion.Y) ||
                    lion.TargetId < 0 || lion.Returning && lion.TargetId != 0 ||
                    !BattleSnapshot.Finite(lion.NextClawAt) || lion.NextClawAt < 0 ||
                    lion.NextClawAt > LastStepAt + ExpandedDanielLion.MaximumCycle + epsilon ||
                    lion.WaitingForTarget && lion.NextClawAt > LastStepAt + epsilon ||
                    !ExpandedDanielLion.ValidFutureTime(lion.NextPackRoarAt, LastStepAt,
                        ExpandedDanielLion.RoarInterval + index * ExpandedDanielLion.PackRoarPhasePerIndex) ||
                    !ExpandedDanielLion.ValidFutureTime(lion.ClawHasteUntil, LastStepAt, ExpandedDanielLion.ClawHasteDuration) ||
                    !ExpandedDanielLion.ValidFutureTime(lion.NextPounceAt, LastStepAt, ExpandedDanielLion.PounceCooldown) ||
                    !ExpandedDanielLion.ValidPounce(lion, LastStepAt) ||
                    !ExpandedDanielLion.ValidPatrol(lion, LastStepAt, NextPatrolLeapAt) ||
                    lion.PatrolUntil.HasValue && (++patrollers > 1 || !NextPatrolAt.HasValue) ||
                    lion.ReturnHitIds != null && (!lion.Returning || lion.ReturnHitIds.Count == 0 ||
                        lion.ReturnHitIds.Count > ExpandedDanielLion.MaximumTrackedTargets ||
                        lion.ReturnHitIds.Any(id => id <= 0) || lion.ReturnHitIds.Distinct().Count() != lion.ReturnHitIds.Count))
                    throw new ArgumentException("Invalid Daniel lion actor");
            }

            ValidateTargetHistory();
            var leap = Leap;
            if (leap != null && (Lions.Count == 0 || Lions[0].Returning || Lions[0].Pounce != null || Lions[0].PatrolUntil.HasValue ||
                GuardianHits == 0 || GuardianHits % ExpandedDanielLion.ClawsPerLeap != 0 ||
                !BattleSnapshot.Position(leap.OriginX, leap.OriginY) || !BattleSnapshot.Finite(leap.At) ||
                leap.At < LastStepAt - epsilon || leap.At > LastStepAt + ExpandedDanielLion.LeapInterval + epsilon ||
                !ExpandedDanielLion.ValidDamage(leap.Damage) ||
                leap.VisitedIds == null || leap.VisitedIds.Count < 1 || leap.VisitedIds.Count > 2 ||
                leap.VisitedIds.Any(id => id < 1) || leap.VisitedIds.Distinct().Count() != leap.VisitedIds.Count))
                throw new ArgumentException("Invalid Daniel lion leap");
        }

        private void ValidateTargetHistory()
        {
            if (TargetHistory == null) return;
            if (TargetHistory.Count == 0 || TargetHistory.Count > ExpandedDanielLion.MaximumTrackedTargets)
                throw new ArgumentException("Invalid Daniel lion targeting history count");
            var previousId = 0;
            foreach (var history in TargetHistory)
            {
                if (history == null || history.TargetId <= previousId || !history.MarkUntil.HasValue && history.RecentClaws == null ||
                    !ExpandedDanielLion.ValidFutureTime(history.MarkUntil, LastStepAt, ExpandedDanielLion.MarkDuration))
                    throw new ArgumentException("Invalid Daniel lion target history");
                previousId = history.TargetId;
                if (history.RecentClaws == null) continue;
                if (history.RecentClaws.Count == 0 || history.RecentClaws.Count > ExpandedDanielLion.MaximumAmount)
                    throw new ArgumentException("Invalid Daniel lion claw receipt count");
                var previousIndex = -1;
                foreach (var claw in history.RecentClaws)
                {
                    if (claw == null || claw.LionIndex <= previousIndex || claw.LionIndex >= ExpandedDanielLion.MaximumAmount ||
                        !BattleSnapshot.Finite(claw.At) || claw.At < 0 || claw.At > LastStepAt + ExpandedDanielLion.TimeEpsilon ||
                        claw.At < LastStepAt - ExpandedDanielLion.CooperationDuration - ExpandedDanielLion.TimeEpsilon)
                        throw new ArgumentException("Invalid Daniel lion claw receipt");
                    previousIndex = claw.LionIndex;
                }
            }
        }
    }

    public static class ExpandedDanielLion
    {
        private static readonly ExpandedWeaponProfile Profile = ExpandedWeaponRules.Get("daniel_lion");
        internal static readonly int MaximumAmount = Profile.MaximumOriginalAmount;
        internal static readonly double MaximumCycle = Profile.AtLevel(1).Cycle;
        private static readonly double MaximumDamage = Profile.AtLevel(8).Damage * 2.25;
        private static readonly double MaximumRadius = Profile.AtLevel(1).Radius * 2.5;
        private static readonly double MinimumSpeed = Profile.AtLevel(1).Speed;
        private static readonly double MaximumSpeed = Profile.AtLevel(8).Speed * 1.75;
        internal const double TimeEpsilon = 1e-9;
        private const double DistanceEpsilon = 1e-7;
        internal const int ClawsPerLeap = 3;
        internal const double LeapInterval = .5;
        internal const double RoarInterval = 4;
        private const double FirstBossClawPhasePerIndex = .12;
        private const double LeapRadius = 2.5;
        internal const double GuardianRoarRadius = 1.8;
        internal const double PackRoarRadius = 1.4;
        internal const double PackRoarPhasePerIndex = .18;
        internal const double ClawHasteDuration = 2;
        internal const double KillLeapCooldown = 5;
        private const double KillLeapDamageFactor = .35;
        private const double GuardianRoarDamageFactor = .35;
        private const double PackRoarDamageFactor = .25;
        private const double GuardianRoarKnockback = .45;
        internal const double MinimumCycle = .15;
        internal const double PounceDuration = .25;
        internal const double PounceCooldown = 3;
        internal const double PounceMinimumDistance = 2;
        internal const double PounceMaximumDistance = 2.5;
        internal const double CooperationDuration = 1;
        internal const double MarkDuration = 2;
        internal const int MaximumTrackedTargets = 120;
        internal const int ClawsPerPackLeap = 6;
        internal const double PackLeapCooldown = 4;
        private const double PackLeapFactor = .35;
        internal const double PatrolInterval = 4;
        internal const double PatrolDuration = 1;
        internal const double PatrolRadius = 1.5;
        internal const double PatrolLeapRadius = 2;
        private const int PatrolCrowdCount = 4;
        private const double ReturnClawWidth = .5;
        private const double ReturnClawFactor = .30;
        private const double FullCircle = 2 * Math.PI;
        internal const double ProtectionCooldown = 6;
        internal const double GuardCooldown = 6;
        internal const double GuardDuration = 2;
        internal const double RoarDeflectCooldown = 2;
        private const double ProtectionRadius = 1.6;
        private const double ProtectionPush = .6;
        private const double GuardKillRadius = 2;

        // A hit callback returns true for accepted damage, including a kill. Only accepted
        // original claws count toward the next guardian leap or the returned original count.
        public static int Step(ExpandedDanielLionState state, double now, Vector2 playerPosition, int level,
            bool evolved, double damage, double cycle, int amount, double radius, double speed,
            Func<IReadOnlyDictionary<int, Vector2>> targets, Func<ExpandedDanielLionHit, bool> applyHit,
            ExpandedDanielLionRankOptions rankOptions = default, Func<int, bool>? isElite = null, Func<int, bool>? isBoss = null,
            Func<double>? afterClaw = null, Func<int, double>? targetHealthRatio = null,
            Func<ExpandedDanielLionHit, double?>? applyOriginalHit = null, Func<bool>? reserveSupportLeap = null,
            Action? recordSupportSuppression = null, Func<bool>? reserveSupportRegion = null,
            Func<Vector2, double, bool>? tryGuardianRoarDeflect = null)
        {
            if (state == null) throw new ArgumentNullException(nameof(state));
            if (targets == null) throw new ArgumentNullException(nameof(targets));
            if (applyHit == null) throw new ArgumentNullException(nameof(applyHit));
            if ((rankOptions.PrioritizeElite || rankOptions.SpreadNormalTargets || rankOptions.FinishLowNormalTargets ||
                rankOptions.MarkEliteOnClaw || rankOptions.GuardAfterEliteKill ||
                evolved && rankOptions.EvolvedHuntMark) && isElite == null)
                throw new ArgumentNullException(nameof(isElite));
            if (rankOptions.FinishLowNormalTargets && targetHealthRatio == null)
                throw new ArgumentNullException(nameof(targetHealthRatio));
            if (rankOptions.StaggerFirstBossClaw && isBoss == null) throw new ArgumentNullException(nameof(isBoss));
            if (!BattleSnapshot.Finite(now) || now < state.LastStepAt - TimeEpsilon || now < 0 ||
                !BattleSnapshot.Position(playerPosition.X, playerPosition.Y) ||
                level < 1 || level > 8 || evolved && level != 8 || amount < 1 || amount > MaximumAmount ||
                !ValidDamage(damage) || !BattleSnapshot.Finite(cycle) || cycle < MinimumCycle || cycle > MaximumCycle ||
                !BattleSnapshot.Finite(radius) || radius <= 0 || radius > MaximumRadius ||
                !BattleSnapshot.Finite(speed) || speed < MinimumSpeed || speed > MaximumSpeed)
                throw new ArgumentException("Invalid Daniel lion parameters");

            if (state.Evolved != evolved) state.ResetForForm(evolved, now);
            SynchronizePack(state, amount, playerPosition, now, rankOptions.ExtendLeash);
            var distance = Math.Max(0, Math.Min(BattleWorld.FixedStep, now - state.LastStepAt)) * speed;
            state.LastStepAt = now;
            if (state.KillLeapReadyAt <= now + TimeEpsilon) state.KillLeapReadyAt = null;
            if (state.NextPackLeapAt <= now + TimeEpsilon) state.NextPackLeapAt = null;
            if (state.NextPatrolLeapAt <= now + TimeEpsilon) state.NextPatrolLeapAt = null;
            if (state.NextProtectionAt <= now + TimeEpsilon) state.NextProtectionAt = null;
            if (state.NextGuardAt <= now + TimeEpsilon) state.NextGuardAt = null;
            if (state.GuardUntil <= now + TimeEpsilon) state.GuardUntil = null;
            if (state.NextRoarDeflectAt <= now + TimeEpsilon) state.NextRoarDeflectAt = null;
            if (rankOptions.PatrolNearPlayer) SchedulePatrol(state, now, playerPosition);
            PruneTargetHistory(state, now, targets);
            if (evolved && rankOptions.WidenEvolvedClaw)
                radius = Math.Min(MaximumRadius, radius + Profile.AtLevel(1).Radius * .12);
            var levelProfile = Profile.AtLevel(level);
            var originals = 0;
            double packLeapDamage = 0;
            var assigned = new HashSet<int>();
            Func<ExpandedDanielLionHit, double?> resolveOriginal = applyOriginalHit ??
                (hit => applyHit(hit) ? hit.Damage : (double?)null);
            foreach (var lion in state.Lions)
            {
                var guardian = evolved && lion.Index == 0;
                if (lion.ClawHasteUntil <= now + TimeEpsilon) lion.ClawHasteUntil = null;
                if (lion.Pounce == null && lion.NextPounceAt <= now + TimeEpsilon) lion.NextPounceAt = null;
                var applied = AdvanceLion(state, lion, guardian, now, playerPosition, damage, cycle, radius,
                    distance, assigned, targets, applyHit, resolveOriginal, rankOptions, isElite, isBoss, targetHealthRatio,
                    afterClaw, levelProfile, reserveSupportRegion, recordSupportSuppression);
                if (applied > 0)
                {
                    originals++;
                    if (lion.Index == 0 && rankOptions.AddPackLeap)
                    {
                        state.PackCycle = checked(state.PackCycle + 1);
                        if (state.PackCycle % ClawsPerPackLeap == 0 && !state.NextPackLeapAt.HasValue)
                            packLeapDamage = applied * PackLeapFactor;
                    }
                }
                if (guardian && state.NextRoarAt <= now + TimeEpsilon)
                {
                    state.NextRoarAt += RoarInterval;
                    var center = lion.Position;
                    Roar(lion, now, damage, true, rankOptions, targets, applyHit);
                    if (rankOptions.DeflectWithGuardianRoar && !state.NextRoarDeflectAt.HasValue &&
                        tryGuardianRoarDeflect?.Invoke(center, rankOptions.RoarRadius(true)) == true)
                        state.NextRoarDeflectAt = now + RoarDeflectCooldown;
                }
                if (rankOptions.AddPackRoar)
                    AdvancePackRoar(lion, now, damage, rankOptions, targets, applyHit);
            }
            if (packLeapDamage > 0)
                ApplyPackLeap(state, now, playerPosition, packLeapDamage, targets, applyHit, reserveSupportLeap, recordSupportSuppression);
            PruneTargetHistory(state, now, targets);
            return originals;
        }

        public static bool TryProtectPlayer(ExpandedDanielLionState state, double now, double receivedDamage,
            ExpandedDanielLionRankOptions ranks, Func<IReadOnlyDictionary<int, Vector2>> targets,
            Func<int, Vector2, double, bool> applyKnockback, Func<bool>? reserveSupportRegion = null,
            Action? recordSupportSuppression = null, Action<Vector2, double>? emitProtection = null)
        {
            if (state == null) throw new ArgumentNullException(nameof(state));
            if (targets == null) throw new ArgumentNullException(nameof(targets));
            if (applyKnockback == null) throw new ArgumentNullException(nameof(applyKnockback));
            if (!BattleSnapshot.Finite(now) || now < 0 || now < state.LastStepAt - TimeEpsilon)
                throw new ArgumentOutOfRangeException(nameof(now));
            if (!ranks.ProtectAfterDamage || !BattleSnapshot.Finite(receivedDamage) || receivedDamage <= 0 ||
                state.NextProtectionAt > now + TimeEpsilon) return false;

            var lion = state.Lions.Find(candidate => candidate.PatrolUntil > now + TimeEpsilon);
            if (lion == null) return false;
            var center = lion.Position;
            var radius = ProtectionRadius * (1 + (ranks.WidenCommonArea ? .04 : 0) + (ranks.WidenDamageRegion ? .06 : 0));
            var ids = targets().Where(target => Within(target.Value, center, radius)).Select(target => target.Key).OrderBy(id => id).ToArray();
            if (ids.Length == 0) return false;
            if (reserveSupportRegion?.Invoke() == false)
            {
                recordSupportSuppression?.Invoke();
                return false;
            }
            var applied = false;
            foreach (var id in ids)
                if (targets().TryGetValue(id, out var position) && Within(position, center, radius))
                    applied |= applyKnockback(id, position - center, ProtectionPush);
            if (!applied) return false;
            state.NextProtectionAt = now + ProtectionCooldown;
            emitProtection?.Invoke(center, radius);
            return true;
        }

        private static void SynchronizePack(ExpandedDanielLionState state, int amount, Vector2 player, double now,
            bool extendLeash)
        {
            if (amount < state.Lions.Count) state.Lions.RemoveRange(amount, state.Lions.Count - amount);
            while (state.Lions.Count < amount)
                state.Lions.Add(new ExpandedDanielLionActorState
                {
                    Index = state.Lions.Count,
                    Position = player,
                    NextClawAt = now,
                    ExtendedLeash = extendLeash,
                });
        }

        private static double AdvanceLion(ExpandedDanielLionState state, ExpandedDanielLionActorState lion,
            bool guardian, double now, Vector2 player, double damage, double cycle, double radius,
            double distance, HashSet<int> assigned, Func<IReadOnlyDictionary<int, Vector2>> targets,
            Func<ExpandedDanielLionHit, bool> applyHit, Func<ExpandedDanielLionHit, double?> applyOriginalHit,
            ExpandedDanielLionRankOptions ranks, Func<int, bool>? isElite, Func<int, bool>? isBoss,
            Func<int, double>? targetHealthRatio, Func<double>? afterClaw, ExpandedWeaponLevel levelProfile,
            Func<bool>? reserveSupportRegion, Action? recordSupportSuppression)
        {
            var leash = (float)(Profile.LeashDistance * (lion.ExtendedLeash ? 1.15 : 1));
            if (!Within(lion.Position, player, leash)) lion.Returning = true;
            if (!lion.Returning && lion.PatrolUntil.HasValue)
            {
                if (AdvancePatrol(state, lion, now, player, leash, distance, ranks, targets))
                {
                    MarkWaitingIfDue(lion, now);
                    return 0;
                }
                EndPatrol(lion);
                lion.Returning = !Within(lion.Position, player, Profile.ReturnRadius);
                MarkWaitingIfDue(lion, now);
                return 0;
            }

            if (lion.Returning)
            {
                lion.TargetId = 0;
                lion.Pounce = null;
                EndPatrol(lion);
                MarkWaitingIfDue(lion, now);
                if (guardian) state.Leap = null;
                var fromReturn = lion.Position;
                lion.Position = Move(fromReturn, player, distance, Profile.ReturnRadius);
                if (ranks.ClawWhileReturning)
                    ApplyReturnClaw(lion, fromReturn, damage, ranks.WidenCommonArea, targets, applyHit,
                        reserveSupportRegion, recordSupportSuppression);
                lion.Returning = !Within(lion.Position, player, Profile.ReturnRadius);
                if (!lion.Returning) lion.ReturnHitIds = null;
                return 0;
            }

            if (guardian && state.Leap != null)
            {
                lion.TargetId = 0;
                MarkWaitingIfDue(lion, now);
                AdvanceLeap(state, lion, now, player, targets, applyHit);
                return 0;
            }

            if (lion.Pounce is { } pounce)
            {
                if (ranks.ReacquireAfterDeath && !targets().ContainsKey(lion.TargetId))
                    lion.Pounce = null;
                else
                {
                    var position = pounce.PositionAt(now);
                    if (!Within(position, player, leash))
                    {
                        lion.Returning = true;
                        lion.TargetId = 0;
                        lion.Pounce = null;
                        MarkWaitingIfDue(lion, now);
                        return 0;
                    }
                    lion.Position = position;
                    if (now < pounce.EndsAt - TimeEpsilon)
                    {
                        assigned.Add(lion.TargetId);
                        MarkWaitingIfDue(lion, now);
                        return 0;
                    }
                    lion.Pounce = null;
                    distance *= Math.Max(0, Math.Min(1, (now - pounce.EndsAt) / BattleWorld.FixedStep));
                }
            }

            var from = lion.Position;
            var (target, candidateCount) = SelectTarget(from, player, assigned, targets(), ranks, isElite);
            lion.TargetId = target.Key;
            if (target.Key == 0)
            {
                MarkWaitingIfDue(lion, now);
                lion.Position = Move(from, player, distance, Profile.ReturnRadius);
                return 0;
            }

            assigned.Add(target.Key);
            if (ranks.PounceOnPursuit && !lion.NextPounceAt.HasValue &&
                DistanceSquared(from, target.Value) >= PounceMinimumDistance * PounceMinimumDistance - DistanceEpsilon)
            {
                var end = Move(from, target.Value, PounceMaximumDistance, 0);
                lion.Pounce = new ExpandedDanielLionPounceState
                {
                    StartedAt = now, StartX = from.X, StartY = from.Y, EndX = end.X, EndY = end.Y,
                };
                lion.NextPounceAt = now + PounceCooldown;
                MarkWaitingIfDue(lion, now);
                return 0;
            }
            lion.Position = Move(from, target.Value, distance, radius);
            if (lion.NextClawAt > now + TimeEpsilon) return 0;
            if (!Within(lion.Position, target.Value, radius))
            {
                lion.WaitingForTarget = true;
                return 0;
            }
            if (ranks.StaggerFirstBossClaw && !lion.FirstClawPhaseResolved &&
                lion.Index > 0 && candidateCount == 1 && isBoss!(target.Key))
            {
                lion.FirstClawPhaseResolved = true;
                lion.NextClawAt = now + lion.Index * FirstBossClawPhasePerIndex;
                lion.WaitingForTarget = false;
                return 0;
            }
            var markFactor = ranks.MarkEliteOnClaw ? state.Evolved && ranks.EvolvedHuntMark ? .10 : .06 :
                state.Evolved && ranks.EvolvedHuntMark ? .04 : 0;
            var canGuard = ranks.GuardAfterEliteKill && !state.NextGuardAt.HasValue;
            var elite = (ranks.FinishLowNormalTargets || markFactor > 0 || canGuard) && isElite!(target.Key);
            var history = state.TargetHistory?.Find(entry => entry.TargetId == target.Key);
            var bonus = ranks.CooperateOnClaw && history?.RecentClaws?.Any(entry => entry.LionIndex != lion.Index) == true ? .08 : 0;
            if (history?.MarkUntil >= now - TimeEpsilon) bonus += markFactor;
            if (ranks.FinishLowNormalTargets && !elite)
            {
                var health = targetHealthRatio!(target.Key);
                if (!BattleSnapshot.Finite(health) || health < 0 || health > 1)
                    throw new ArgumentOutOfRangeException(nameof(targetHealthRatio));
                if (health <= .25) bonus += .10;
            }
            var clawDamage = bonus > 0 ? Math.Min(levelProfile.Damage * 2.25, damage + levelProfile.Damage * bonus) : damage;
            var acceptedDamage = applyOriginalHit(new ExpandedDanielLionHit(ExpandedDanielLionHitKind.Claw,
                lion.Index, target.Key, target.Value, lion.Position, clawDamage, originalBaseDamage: levelProfile.Damage));
            if (!acceptedDamage.HasValue || acceptedDamage.Value <= 0)
            {
                lion.WaitingForTarget = true;
                return 0;
            }

            var mark = markFactor > 0 && elite;
            if (ranks.CooperateOnClaw || mark || canGuard && elite)
            {
                var survived = targets().ContainsKey(target.Key);
                if ((ranks.CooperateOnClaw || mark) && survived)
                    RecordTargetHistory(state, history, target.Key, lion.Index, now, ranks.CooperateOnClaw, mark);
                if (canGuard && elite && !survived && Within(target.Value, player, GuardKillRadius))
                {
                    state.GuardUntil = now + GuardDuration;
                    state.NextGuardAt = now + GuardCooldown;
                }
            }

            var at = lion.WaitingForTarget ? now : lion.NextClawAt;
            var nextWait = afterClaw?.Invoke() ?? cycle;
            if (!BattleSnapshot.Finite(nextWait) || nextWait < MinimumCycle || nextWait > MaximumCycle)
                throw new ArgumentOutOfRangeException(nameof(afterClaw));
            if (lion.ClawHasteUntil > now + TimeEpsilon)
            {
                var floor = Math.Max(MinimumCycle, levelProfile.Cycle * .20);
                nextWait = Math.Min(nextWait, Math.Max(floor, nextWait - levelProfile.Cycle * .06));
            }
            lion.NextClawAt = at + nextWait;
            lion.WaitingForTarget = false;
            lion.FirstClawPhaseResolved |= ranks.StaggerFirstBossClaw;
            if (guardian && ++state.GuardianHits % ClawsPerLeap == 0)
                state.Leap = new ExpandedDanielLionLeapState
                {
                    At = at + LeapInterval,
                    Damage = acceptedDamage.Value,
                    Origin = target.Value,
                    VisitedIds = new List<int> { target.Key },
                    EmpoweredDamage = ranks.EmpowerLeap,
                    FollowupOnKill = ranks.FollowUpLeapKill,
                };
            return acceptedDamage.Value;
        }

        private static void SchedulePatrol(ExpandedDanielLionState state, double now, Vector2 player)
        {
            var due = state.NextPatrolAt ?? (now + PatrolInterval);
            state.NextPatrolAt = due;
            if (due > now + TimeEpsilon) return;
            do { state.NextPatrolAt += PatrolInterval; } while (state.NextPatrolAt <= now + TimeEpsilon);
            if (due + PatrolDuration <= now + TimeEpsilon || state.Lions.Any(lion => lion.PatrolUntil.HasValue)) return;

            var lion = state.Lions.FirstOrDefault(candidate => !candidate.Returning && candidate.Pounce == null &&
                (!state.Evolved || candidate.Index != 0 || state.Leap == null));
            if (lion == null) return;
            var relative = lion.Position - player;
            var angle = Math.Atan2(relative.Y, relative.X);
            lion.PatrolAngle = angle < 0 ? angle + FullCircle : angle;
            lion.PatrolUntil = due + PatrolDuration;
            lion.TargetId = 0;
        }

        private static bool AdvancePatrol(ExpandedDanielLionState state, ExpandedDanielLionActorState lion, double now,
            Vector2 player, double leash, double distance, ExpandedDanielLionRankOptions ranks,
            Func<IReadOnlyDictionary<int, Vector2>> targets)
        {
            if (lion.PatrolLeap is { } leap)
            {
                var position = leap.PositionAt(now);
                if (!Within(position, player, leash)) return false;
                lion.Position = position;
                return now < leap.EndsAt - TimeEpsilon;
            }
            if (lion.PatrolUntil <= now + TimeEpsilon) return false;

            var center = lion.Position;
            var length = Vector2.Distance(center, player);
            if (Math.Abs(length - PatrolRadius) <= distance + DistanceEpsilon)
                lion.PatrolAngle = (lion.PatrolAngle + distance / PatrolRadius) % FullCircle;
            var angle = lion.PatrolAngle;
            var waypoint = player + new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle)) * (float)PatrolRadius;
            lion.Position = Move(center, waypoint, distance, 0);
            if (!ranks.LeapAtPatrolCrowd || state.NextPatrolLeapAt.HasValue) return true;

            center = lion.Position;
            var close = targets().Where(target => Within(target.Value, center, PatrolLeapRadius))
                .OrderBy(target => DistanceSquared(target.Value, center)).ThenBy(target => target.Key).ToArray();
            if (close.Length < PatrolCrowdCount) return true;
            var target = close.FirstOrDefault(candidate => DistanceSquared(candidate.Value, center) > DistanceEpsilon);
            if (target.Key == 0) return true;
            var end = Move(center, target.Value, PatrolLeapRadius, 0);
            if (!Within(end, player, leash)) return true;
            lion.PatrolLeap = new ExpandedDanielLionPounceState
            {
                StartedAt = now, StartX = center.X, StartY = center.Y, EndX = end.X, EndY = end.Y,
            };
            state.NextPatrolLeapAt = now + PatrolInterval;
            return true;
        }

        private static void EndPatrol(ExpandedDanielLionActorState lion)
        {
            lion.PatrolUntil = null;
            lion.PatrolAngle = 0;
            lion.PatrolLeap = null;
        }

        private static void ApplyReturnClaw(ExpandedDanielLionActorState lion, Vector2 origin, double damage,
            bool wider, Func<IReadOnlyDictionary<int, Vector2>> targets, Func<ExpandedDanielLionHit, bool> applyHit,
            Func<bool>? reserveSupportRegion, Action? recordSupportSuppression)
        {
            var current = targets();
            lion.ReturnHitIds?.RemoveAll(id => !current.ContainsKey(id));
            if (lion.ReturnHitIds?.Count == 0) lion.ReturnHitIds = null;
            var offset = lion.Position - origin;
            var lengthSquared = DistanceSquared(lion.Position, origin);
            if (lengthSquared <= DistanceEpsilon) return;
            var half = ReturnClawWidth * .5 * (wider ? 1.04 : 1);
            var length = Math.Sqrt(lengthSquared);
            var ids = current.Where(target => lion.ReturnHitIds?.Contains(target.Key) != true && Swept(target.Value))
                .Select(target => target.Key).OrderBy(id => id).ToArray();
            if (ids.Length == 0) return;
            if (reserveSupportRegion?.Invoke() == false)
            {
                recordSupportSuppression?.Invoke();
                return;
            }
            foreach (var id in ids)
            {
                if (!targets().TryGetValue(id, out var position) || !Swept(position) ||
                    !applyHit(new ExpandedDanielLionHit(ExpandedDanielLionHitKind.ReturnClaw, lion.Index, id, position,
                        origin, damage * ReturnClawFactor))) continue;
                lion.ReturnHitIds ??= new List<int>();
                lion.ReturnHitIds.Add(id);
            }

            bool Swept(Vector2 position)
            {
                var delta = position - origin;
                var forward = Vector2.Dot(delta, offset);
                if (forward < -DistanceEpsilon || forward > lengthSquared + DistanceEpsilon) return false;
                return Math.Abs((double)delta.X * offset.Y - (double)delta.Y * offset.X) <= half * length + DistanceEpsilon;
            }
        }

        private static void PruneTargetHistory(ExpandedDanielLionState state, double now, Func<IReadOnlyDictionary<int, Vector2>> targets)
        {
            if (state.TargetHistory == null) return;
            var current = targets();
            foreach (var history in state.TargetHistory)
            {
                if (history.MarkUntil < now - TimeEpsilon) history.MarkUntil = null;
                history.RecentClaws?.RemoveAll(claw => claw.At < now - CooperationDuration - TimeEpsilon);
                if (history.RecentClaws?.Count == 0) history.RecentClaws = null;
            }
            state.TargetHistory.RemoveAll(history => !current.ContainsKey(history.TargetId) ||
                !history.MarkUntil.HasValue && history.RecentClaws == null);
            if (state.TargetHistory.Count == 0) state.TargetHistory = null;
        }

        private static void RecordTargetHistory(ExpandedDanielLionState state, ExpandedDanielLionTargetHistory? history,
            int targetId, int lionIndex, double now, bool cooperation, bool mark)
        {
            if (history == null)
            {
                state.TargetHistory ??= new List<ExpandedDanielLionTargetHistory>();
                history = new ExpandedDanielLionTargetHistory { TargetId = targetId };
                var insert = state.TargetHistory.FindIndex(entry => entry.TargetId > targetId);
                if (insert < 0) state.TargetHistory.Add(history);
                else state.TargetHistory.Insert(insert, history);
            }
            if (mark) history.MarkUntil = now + MarkDuration;
            if (!cooperation) return;
            history.RecentClaws ??= new List<ExpandedDanielLionClawReceipt>();
            var claw = history.RecentClaws.Find(entry => entry.LionIndex == lionIndex);
            if (claw != null) claw.At = now;
            else
            {
                claw = new ExpandedDanielLionClawReceipt { LionIndex = lionIndex, At = now };
                var insert = history.RecentClaws.FindIndex(entry => entry.LionIndex > lionIndex);
                if (insert < 0) history.RecentClaws.Add(claw);
                else history.RecentClaws.Insert(insert, claw);
            }
        }

        private static void ApplyPackLeap(ExpandedDanielLionState state, double now, Vector2 player, double damage,
            Func<IReadOnlyDictionary<int, Vector2>> targets, Func<ExpandedDanielLionHit, bool> applyHit,
            Func<bool>? reserveSupportLeap, Action? recordSupportSuppression)
        {
            var accepted = 0;
            var candidatesUsed = 0;
            foreach (var lion in state.Lions)
            {
                if (lion.Returning || lion.Pounce != null || lion.PatrolUntil.HasValue) continue;
                var origin = lion.Position;
                var candidates = targets().Where(entry => Within(entry.Value, origin, LeapRadius) &&
                    Within(entry.Value, player, Profile.TargetSearchRadius))
                    .OrderBy(entry => DistanceSquared(entry.Value, origin)).ThenBy(entry => entry.Key).ToArray();
                if (candidates.Length == 0) continue;
                if (candidatesUsed++ == 2) break;
                if (reserveSupportLeap?.Invoke() == false)
                {
                    recordSupportSuppression?.Invoke();
                    continue;
                }
                foreach (var candidate in candidates)
                {
                    if (!applyHit(new ExpandedDanielLionHit(ExpandedDanielLionHitKind.PackLeap, lion.Index,
                        candidate.Key, candidate.Value, origin, damage))) continue;
                    lion.Position = candidate.Value;
                    lion.TargetId = 0;
                    accepted++;
                    break;
                }
            }
            if (accepted > 0) state.NextPackLeapAt = now + PackLeapCooldown;
        }

        private static (KeyValuePair<int, Vector2> Target, int CandidateCount) SelectTarget(Vector2 lion,
            Vector2 player, HashSet<int> assigned, IReadOnlyDictionary<int, Vector2> targets,
            ExpandedDanielLionRankOptions ranks, Func<int, bool>? isElite)
        {
            var nearest = default(KeyValuePair<int, Vector2>);
            var unassignedNormal = default(KeyValuePair<int, Vector2>);
            var nearestDistance = double.PositiveInfinity;
            var unassignedDistance = double.PositiveInfinity;
            var nearestAssigned = false;
            var nearestPriority = false;
            var nearestElite = false;
            var candidateCount = 0;
            foreach (var entry in targets)
            {
                if (!Within(entry.Value, player, Profile.TargetSearchRadius)) continue;
                candidateCount++;
                var distance = DistanceSquared(entry.Value, lion);
                var alreadyAssigned = assigned.Contains(entry.Key);
                var elite = (ranks.PrioritizeElite || ranks.SpreadNormalTargets) && isElite!(entry.Key);
                var priority = ranks.PrioritizeElite && elite && Within(entry.Value, player, 5);
                if ((priority && !nearestPriority) || (priority == nearestPriority &&
                    (distance < nearestDistance || (distance == nearestDistance &&
                    (alreadyAssigned == nearestAssigned ? entry.Key < nearest.Key : !alreadyAssigned)))))
                {
                    nearest = entry;
                    nearestDistance = distance;
                    nearestAssigned = alreadyAssigned;
                    nearestPriority = priority;
                    nearestElite = elite;
                }
                if (ranks.SpreadNormalTargets && !alreadyAssigned && !elite &&
                    (distance < unassignedDistance || distance == unassignedDistance && entry.Key < unassignedNormal.Key))
                {
                    unassignedNormal = entry;
                    unassignedDistance = distance;
                }
            }
            if (nearestAssigned && !nearestElite && unassignedNormal.Key != 0 &&
                Math.Sqrt(unassignedDistance) <= Math.Sqrt(nearestDistance) + 1 + DistanceEpsilon)
                nearest = unassignedNormal;
            return (nearest, candidateCount);
        }

        private static void AdvanceLeap(ExpandedDanielLionState state, ExpandedDanielLionActorState lion,
            double now, Vector2 player, Func<IReadOnlyDictionary<int, Vector2>> targets,
            Func<ExpandedDanielLionHit, bool> applyHit)
        {
            var leap = state.Leap!;
            if (leap.At > now + TimeEpsilon) return;
            var origin = leap.Origin;
            var candidates = LeapCandidates(origin, player, leap.VisitedIds, targets());
            var factor = (leap.VisitedIds.Count == 1 ? 1 : .6) + (leap.EmpoweredDamage ? .15 : 0);
            foreach (var candidate in candidates)
            {
                if (!applyHit(new ExpandedDanielLionHit(ExpandedDanielLionHitKind.Leap, lion.Index,
                    candidate.Key, candidate.Value, origin, leap.Damage * factor))) continue;
                lion.Position = candidate.Value;
                if (leap.VisitedIds.Count == 2)
                {
                    // The last natural hop can branch once. If no second natural hop can hit,
                    // the same search has no eligible unvisited target for a bonus hop.
                    if (leap.FollowupOnKill && !state.KillLeapReadyAt.HasValue && !targets().ContainsKey(candidate.Key))
                    {
                        leap.VisitedIds.Add(candidate.Key);
                        ApplyKillFollowupLeap(state, lion, now, player, leap, targets, applyHit);
                    }
                    break;
                }
                leap.VisitedIds.Add(candidate.Key);
                leap.Origin = candidate.Value;
                leap.At += LeapInterval;
                return;
            }
            state.Leap = null;
        }

        private static KeyValuePair<int, Vector2>[] LeapCandidates(Vector2 origin, Vector2 player, List<int> visited,
            IReadOnlyDictionary<int, Vector2> targets) => targets.Where(entry => !visited.Contains(entry.Key) &&
                Within(entry.Value, origin, LeapRadius) && Within(entry.Value, player, Profile.TargetSearchRadius))
            .OrderBy(entry => DistanceSquared(entry.Value, origin)).ThenBy(entry => entry.Key).ToArray();

        private static void ApplyKillFollowupLeap(ExpandedDanielLionState state, ExpandedDanielLionActorState lion,
            double now, Vector2 player, ExpandedDanielLionLeapState leap,
            Func<IReadOnlyDictionary<int, Vector2>> targets, Func<ExpandedDanielLionHit, bool> applyHit)
        {
            var origin = lion.Position;
            foreach (var candidate in LeapCandidates(origin, player, leap.VisitedIds, targets()))
            {
                if (!applyHit(new ExpandedDanielLionHit(ExpandedDanielLionHitKind.KillFollowupLeap, lion.Index,
                    candidate.Key, candidate.Value, origin, leap.Damage * KillLeapDamageFactor))) continue;
                lion.Position = candidate.Value;
                state.KillLeapReadyAt = now + KillLeapCooldown;
                return;
            }
        }

        private static void AdvancePackRoar(ExpandedDanielLionActorState lion, double now, double damage,
            ExpandedDanielLionRankOptions ranks, Func<IReadOnlyDictionary<int, Vector2>> targets,
            Func<ExpandedDanielLionHit, bool> applyHit)
        {
            var stagger = ranks.StaggerPackRoar ? lion.Index * PackRoarPhasePerIndex : 0;
            var due = lion.NextPackRoarAt ?? (now + RoarInterval + stagger);
            lion.NextPackRoarAt = due;
            if (due > now + TimeEpsilon) return;
            lion.NextPackRoarAt = due + RoarInterval;
            Roar(lion, now, damage, false, ranks, targets, applyHit);
        }

        private static void Roar(ExpandedDanielLionActorState lion, double now, double damage, bool guardian,
            ExpandedDanielLionRankOptions ranks, Func<IReadOnlyDictionary<int, Vector2>> targets,
            Func<ExpandedDanielLionHit, bool> applyHit)
        {
            var radius = ranks.RoarRadius(guardian);
            var factor = guardian ? GuardianRoarDamageFactor : PackRoarDamageFactor;
            var knockback = (guardian ? GuardianRoarKnockback : 0) + (ranks.StrengthenRoarKnockback ? .30 : 0);
            var center = lion.Position;
            var ids = targets().Where(entry => Within(entry.Value, center, radius))
                .Select(entry => entry.Key).OrderBy(id => id).ToArray();
            foreach (var id in ids)
                if (targets().TryGetValue(id, out var position) && Within(position, center, radius))
                    applyHit(new ExpandedDanielLionHit(ExpandedDanielLionHitKind.Roar, lion.Index,
                        id, position, center, damage * factor, knockback));
            if (ranks.HastenClawsAfterRoar) lion.ClawHasteUntil = now + ClawHasteDuration;
        }

        internal static bool ValidPounce(ExpandedDanielLionActorState lion, double now)
        {
            if (lion.Pounce is not { } pounce) return true;
            return !lion.Returning && lion.TargetId > 0 && lion.NextPounceAt.HasValue &&
                BattleSnapshot.Finite(pounce.StartedAt) && pounce.StartedAt >= 0 && pounce.StartedAt <= now + TimeEpsilon &&
                pounce.EndsAt > now + TimeEpsilon &&
                Math.Abs(lion.NextPounceAt.Value - (pounce.StartedAt + PounceCooldown)) <= TimeEpsilon &&
                BattleSnapshot.Position(pounce.StartX, pounce.StartY) && BattleSnapshot.Position(pounce.EndX, pounce.EndY) &&
                DistanceSquared(pounce.Start, pounce.End) >= PounceMinimumDistance * PounceMinimumDistance - 1e-5 &&
                DistanceSquared(pounce.Start, pounce.End) <= PounceMaximumDistance * PounceMaximumDistance + 1e-5 &&
                lion.Position == pounce.PositionAt(now);
        }

        internal static bool ValidPatrol(ExpandedDanielLionActorState lion, double now, double? nextLeapAt)
        {
            if (!lion.PatrolUntil.HasValue) return lion.PatrolAngle == 0 && lion.PatrolLeap == null;
            var until = lion.PatrolUntil.Value;
            if (lion.Pounce != null || lion.Returning || lion.TargetId != 0 ||
                !BattleSnapshot.Finite(lion.PatrolAngle) || lion.PatrolAngle < 0 || lion.PatrolAngle >= FullCircle ||
                !BattleSnapshot.Finite(until) || until > now + PatrolDuration + TimeEpsilon)
                return false;
            if (lion.PatrolLeap is not { } route) return until > now + TimeEpsilon;
            return nextLeapAt.HasValue && Math.Abs(nextLeapAt.Value - (route.StartedAt + PatrolInterval)) <= TimeEpsilon &&
                BattleSnapshot.Finite(route.StartedAt) && route.StartedAt >= 0 && route.StartedAt >= until - PatrolDuration - TimeEpsilon &&
                route.StartedAt <= now + TimeEpsilon && route.StartedAt < until && route.EndsAt > now + TimeEpsilon &&
                BattleSnapshot.Position(route.StartX, route.StartY) && BattleSnapshot.Position(route.EndX, route.EndY) &&
                DistanceSquared(route.Start, route.End) > DistanceEpsilon &&
                DistanceSquared(route.Start, route.End) <= PatrolLeapRadius * PatrolLeapRadius + 1e-5 &&
                lion.Position == route.PositionAt(now);
        }

        internal static bool ValidFutureTime(double? value, double now, double horizon) => !value.HasValue ||
            (BattleSnapshot.Finite(value.Value) && value.Value >= now - TimeEpsilon &&
                value.Value <= now + horizon + TimeEpsilon);

        private static void MarkWaitingIfDue(ExpandedDanielLionActorState lion, double now)
        {
            if (lion.NextClawAt <= now + TimeEpsilon) lion.WaitingForTarget = true;
        }

        private static Vector2 Move(Vector2 from, Vector2 to, double budget, double stopDistance)
        {
            var squared = DistanceSquared(from, to);
            if (squared <= stopDistance * stopDistance + DistanceEpsilon || budget <= 0) return from;
            var distance = Math.Sqrt(squared);
            var movement = Math.Min(distance, budget);
            return from + (to - from) * (float)(movement / distance);
        }

        private static bool Within(Vector2 point, Vector2 center, double distance) =>
            DistanceSquared(point, center) <= distance * distance + DistanceEpsilon;

        private static double DistanceSquared(Vector2 a, Vector2 b)
        {
            var x = (double)a.X - b.X;
            var y = (double)a.Y - b.Y;
            return x * x + y * y;
        }

        internal static bool ValidDamage(double damage) =>
            BattleSnapshot.Finite(damage) && damage >= 1 && damage <= MaximumDamage;
    }
}
