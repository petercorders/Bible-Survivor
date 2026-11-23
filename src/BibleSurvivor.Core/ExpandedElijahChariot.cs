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
    }

    public readonly struct ExpandedElijahChariotRankOptions
    {
        public bool ExtendTravel { get; }
        public bool WidenFormation { get; }
        public bool KnockBack { get; }
        public bool WidenWhirlwind { get; }
        public bool ExtendTrail { get; }
        public bool WidenSupportShape { get; }
        public bool PrioritizeCrowds { get; }
        public bool PrioritizeElite { get; }
        public bool ConvergeOnBoss { get; }
        public bool StaggerFormation { get; }
        public bool StrengthenLastChariot { get; }
        public double ResidualDurationBonus { get; }

        public ExpandedElijahChariotRankOptions(bool extendTravel = false, bool widenFormation = false,
            bool knockBack = false, bool widenWhirlwind = false, bool extendTrail = false, bool widenSupportShape = false,
            bool prioritizeCrowds = false, bool prioritizeElite = false, bool convergeOnBoss = false,
            bool staggerFormation = false, bool strengthenLastChariot = false, double residualDurationBonus = 0)
        {
            if (!ExpandedWeaponRules.ValidResidualDurationBonus(residualDurationBonus))
                throw new ArgumentOutOfRangeException(nameof(residualDurationBonus));
            ExtendTravel = extendTravel;
            WidenFormation = widenFormation;
            KnockBack = knockBack;
            WidenWhirlwind = widenWhirlwind;
            ExtendTrail = extendTrail;
            WidenSupportShape = widenSupportShape;
            PrioritizeCrowds = prioritizeCrowds;
            PrioritizeElite = prioritizeElite;
            ConvergeOnBoss = convergeOnBoss;
            StaggerFormation = staggerFormation;
            StrengthenLastChariot = strengthenLastChariot;
            ResidualDurationBonus = residualDurationBonus;
        }

        public static ExpandedElijahChariotRankOptions FromRanks(IReadOnlyDictionary<string, int> ranks)
        {
            if (ranks == null) throw new ArgumentNullException(nameof(ranks));
            bool Has(string id) => ranks.TryGetValue(id, out var rank) && rank > 0;
            return new ExpandedElijahChariotRankOptions(Has("weapon.elijah_chariot.a.01"), Has("weapon.elijah_chariot.b.01"),
                Has("weapon.elijah_chariot.d.01"), Has("weapon.elijah_chariot.e.01"), Has("weapon.elijah_chariot.e.02"),
                Has("common.area.01"), Has("weapon.elijah_chariot.a.02"), Has("weapon.elijah_chariot.a.03"),
                Has("weapon.elijah_chariot.b.02"), Has("weapon.elijah_chariot.b.03"), Has("weapon.elijah_chariot.b.04"),
                ExpandedWeaponRules.ResidualDurationBonus(ranks, true));
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

        internal ExpandedElijahChariotHit(ExpandedElijahChariotHitKind kind, int targetId,
            Vector2 position, double damage, ExpandedElijahChariotPathState path)
        {
            Kind = kind;
            TargetId = targetId;
            Position = position;
            Direction = path.Direction;
            Damage = damage;
            OriginalBaseDamage = kind == ExpandedElijahChariotHitKind.Body ? path.OriginalBaseDamage : 0;
            TargetOrdinal = kind == ExpandedElijahChariotHitKind.Body ? path.BodyHitCount + 1 : 0;
            Knockback = kind == ExpandedElijahChariotHitKind.Body && path.ForwardKnockback ? .30 : 0;
            PathId = path.Id;
            Cycle = path.Cycle;
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
        [DataMember(EmitDefaultValue = false)] public double ResidualDurationBonus { get; set; }
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
        public Vector2 Position => Origin + Direction * (float)Distance;
        public double WarningAt => MoveAt - WarningDelay;
        internal double CycleStartedAt => WarningAt - StaggerIndex * ExpandedElijahChariot.StaggerPerChariot;
        private double WarningDelay => FirstDelay ?? ExpandedElijahChariot.Profile.FirstAttackDelay;
        public double EndAt => MoveAt + Range / Speed;
        public bool BodyEnded => Distance >= Range;
        public double WhirlwindRadius => ExpandedElijahChariot.BaseWhirlwindRadius *
            (1 + (WiderWhirlwind ? .15 : 0) + (WidenedSupportShape ? .04 : 0));
        public double TrailWidth => ExpandedElijahChariot.BaseTrailWidth * (WidenedSupportShape ? 1.04 : 1);
        public double TrailDuration => ExpandedElijahChariot.BaseTrailDuration *
            (1 + (ExtendedTrail ? .20 : 0) + ResidualDurationBonus);

        internal int TrailTickLimit => (int)Math.Ceiling((Range / Speed + TrailDuration - ExpandedElijahChariot.TimeEpsilon) /
            ExpandedElijahChariot.TrailInterval);
        internal double TrailAt(int tick) => MoveAt + tick * ExpandedElijahChariot.TrailInterval;
        internal double ExpiresAt(bool evolved) => EndAt + (evolved && !SuppressedTrail ? TrailDuration : 0);
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
    public sealed class ExpandedElijahChariotState
    {
        [DataMember(IsRequired = true)] public bool Evolved { get; set; }
        [DataMember(IsRequired = true)] public long Cycle { get; set; }
        [DataMember(IsRequired = true)] public long NextPathId { get; set; } = 1;
        [DataMember(IsRequired = true)] public double NextCycle { get; set; }
        [DataMember(EmitDefaultValue = false)] public int SuppressedOriginals { get; set; }
        [DataMember(IsRequired = true)] public List<ExpandedElijahChariotPathState> Paths { get; set; } =
            new List<ExpandedElijahChariotPathState>();

        public ExpandedElijahChariotState Copy() => new ExpandedElijahChariotState
        {
            Evolved = Evolved,
            Cycle = Cycle,
            NextPathId = NextPathId,
            NextCycle = NextCycle,
            SuppressedOriginals = SuppressedOriginals,
            Paths = Paths.Select(path => path.Copy()).ToList(),
        };

        public void ResetForForm(bool evolved, double now)
        {
            if (!BattleSnapshot.Finite(now) || now < 0) throw new ArgumentOutOfRangeException(nameof(now));
            Evolved = evolved;
            NextCycle = now;
            Paths.Clear();
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
                Paths == null || Paths.Count > ExpandedElijahChariot.MaximumPaths)
                throw new ArgumentException("Invalid Elijah chariot clock");

            var previousId = 0L;
            var previousCycle = 0L;
            var previousCycleStart = 0d;
            var previousStaggerIndex = 0;
            foreach (var path in Paths)
            {
                if (path == null || path.Id <= previousId || path.Id >= NextPathId || path.Cycle < 1 || path.Cycle > Cycle ||
                    !BattleSnapshot.Position(path.StartX, path.StartY) ||
                    !BattleSnapshot.Position(path.DirectionX, path.DirectionY) ||
                    Math.Abs(path.Direction.LengthSquared() - 1) > .0001 ||
                    !ExpandedElijahChariot.ValidPath(path.Damage, path.Width, path.Speed, path.Range) ||
                    !ExpandedElijahChariot.ValidOriginalBaseDamage(path.OriginalBaseDamage, evolved) ||
                    !BattleSnapshot.Finite(path.MoveAt) || !ExpandedElijahChariot.ValidDelay(path.FirstDelay) ||
                    path.StaggerIndex < 0 || path.StaggerIndex >= ExpandedElijahChariot.MaximumAmount ||
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
        }

        private static void ValidateHitState(ExpandedElijahChariotPathState path, double now, bool evolved)
        {
            const double epsilon = ExpandedElijahChariot.TimeEpsilon;
            var hasTrail = evolved && !path.SuppressedTrail;
            if (path.BodyHitIds == null || path.WhirlwindCandidateIds == null ||
                path.BodyHitIds.Count + path.WhirlwindCandidateIds.Count > ExpandedElijahChariot.MaximumLiveTargets ||
                path.BodyHitCount < path.BodyHitIds.Count || path.BodyHitCount > ExpandedElijahChariot.MaximumBodyHits ||
                path.BodyHitIds.Concat(path.WhirlwindCandidateIds).Any(id => id < 1) ||
                path.BodyHitIds.Concat(path.WhirlwindCandidateIds).Distinct().Count() !=
                    path.BodyHitIds.Count + path.WhirlwindCandidateIds.Count ||
                (path.BodyEnded || !evolved) && path.WhirlwindCandidateIds.Count != 0 ||
                now < path.MoveAt - epsilon && (path.BodyHitCount != 0 || path.WhirlwindCandidateIds.Count != 0) ||
                (path.SuppressedTrail || path.WiderWhirlwind || path.WidenedSupportShape) && !evolved ||
                !ExpandedWeaponRules.ValidResidualDurationBonus(path.ResidualDurationBonus) ||
                (path.ExtendedTrail || path.ResidualDurationBonus != 0) && !hasTrail ||
                path.TrailTicksFired < 0 || path.TrailTicksFired > (hasTrail ? path.TrailTickLimit : 0))
                throw new ArgumentException("Invalid Elijah chariot hit history");

            var previousTrailAt = path.TrailAt(path.TrailTicksFired - 1);
            var nextTrailAt = path.TrailAt(path.TrailTicksFired);
            if (path.TrailTicksFired > 0 && (previousTrailAt > now + epsilon || previousTrailAt >= path.ExpiresAt(evolved) - epsilon) ||
                hasTrail && nextTrailAt < path.ExpiresAt(true) - epsilon && nextTrailAt < now - epsilon)
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
        private const double TrailDamageFactor = .35;
        internal const double BaseWhirlwindRadius = 1.15;
        private const double WhirlwindDamageFactor = .45;

        public static void Step(ExpandedElijahChariotState state, double now, Vector2 playerPosition, Vector2 facing,
            int level, bool evolved, double damage, double cycle, int amount, double width, double speed, double range,
            Func<IReadOnlyDictionary<int, Vector2>> targets, Func<ExpandedElijahChariotHit, bool> applyHit,
            double delayAdvance = 0, Func<bool>? reserveOriginal = null, Func<bool>? reserveTrail = null,
            ExpandedElijahChariotRankOptions rankOptions = default, Func<double>? nextCycleDuration = null,
            Func<int, bool>? isElite = null, Func<int, bool>? isBoss = null,
            Action<int>? recordOriginalSuppression = null, Action? recordSupportSuppression = null)
        {
            if (state == null) throw new ArgumentNullException(nameof(state));
            if (targets == null) throw new ArgumentNullException(nameof(targets));
            if (applyHit == null) throw new ArgumentNullException(nameof(applyHit));
            if (rankOptions.PrioritizeElite && isElite == null) throw new ArgumentNullException(nameof(isElite));
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
            AdvancePaths(state, now, targets, applyHit);
            if (state.NextCycle > now + TimeEpsilon) return;

            var direction = facing.LengthSquared() > 1e-12f ? Vector2.Normalize(facing) : Vector2.UnitX;
            var canAim = rankOptions.PrioritizeCrowds || rankOptions.PrioritizeElite;
            var current = canAim || rankOptions.ConvergeOnBoss ? targets() : null;
            if (canAim)
                direction = ResolveAim(playerPosition, direction, current!, rankOptions, isElite, isBoss);
            var converge = rankOptions.ConvergeOnBoss ? SoleBossPosition(current!, isBoss!) : null;
            var perpendicular = new Vector2(-direction.Y, direction.X);
            var spacing = Profile.ParallelSpacing + (rankOptions.WidenFormation ? .30 : 0);
            var travel = range + (rankOptions.ExtendTravel ? TravelBonus : 0);
            var delay = Math.Max(MinimumDelay, Profile.FirstAttackDelay - delayAdvance);
            var moveAt = state.NextCycle + delay;
            state.Cycle = checked(state.Cycle + 1);
            state.NextCycle += nextCycleDuration?.Invoke() ?? cycle;
            var originalBaseDamage = Profile.AtLevel(level).Damage;
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
                var staggerIndex = rankOptions.StaggerFormation ? index : 0;
                var pathDamage = damage;
                if (rankOptions.StrengthenLastChariot && index == amount - 1)
                {
                    pathDamage = Math.Min(originalBaseDamage * 2.25, damage + originalBaseDamage * LastChariotDamageBonus);
                }
                var suppressedTrail = evolved && reserveTrail?.Invoke() == false;
                if (suppressedTrail) recordSupportSuppression?.Invoke();
                state.Paths.Add(new ExpandedElijahChariotPathState
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
                    SuppressedTrail = suppressedTrail,
                    ForwardKnockback = rankOptions.KnockBack,
                    WiderWhirlwind = evolved && rankOptions.WidenWhirlwind,
                    ExtendedTrail = evolved && !suppressedTrail && rankOptions.ExtendTrail,
                    WidenedSupportShape = evolved && rankOptions.WidenSupportShape,
                    ResidualDurationBonus = evolved && !suppressedTrail ? rankOptions.ResidualDurationBonus : 0,
                    Range = pathRange,
                    Speed = speed,
                    Width = width,
                    Damage = pathDamage,
                    OriginalBaseDamage = originalBaseDamage,
                });
                state.NextPathId = checked(state.NextPathId + 1);
            }
        }

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
            Func<IReadOnlyDictionary<int, Vector2>> targets, Func<ExpandedElijahChariotHit, bool> applyHit)
        {
            if (state.Paths.Count == 0) return;
            var ids = targets().Keys.OrderBy(id => id).ToArray();
            foreach (var path in state.Paths)
            {
                if (path.MoveAt > now + TimeEpsilon || path.BodyEnded) continue;
                AdvanceBody(path, now, state.Evolved, ids, targets, applyHit);
            }
            if (state.Evolved) AdvanceTrails(state.Paths, now, ids, targets, applyHit);
            state.Paths.RemoveAll(path => path.ExpiresAt(state.Evolved) <= now + TimeEpsilon);
        }

        private static void AdvanceBody(ExpandedElijahChariotPathState path, double now, bool evolved, int[] ids,
            Func<IReadOnlyDictionary<int, Vector2>> targets, Func<ExpandedElijahChariotHit, bool> applyHit)
        {
            // Actor IDs are occurrence IDs and cannot be reused before the arena resets.
            // Keep live deduplication separately from the total for this original path.
            var current = targets();
            path.BodyHitIds.RemoveAll(id => !current.ContainsKey(id));
            path.WhirlwindCandidateIds.RemoveAll(id => !current.ContainsKey(id));
            var from = path.Distance;
            var to = path.DistanceAt(now);
            var whirlwindRadius = path.WhirlwindRadius;

            foreach (var id in ids)
            {
                if (!targets().TryGetValue(id, out var position) || path.BodyHitIds.Contains(id)) continue;
                var (forward, side) = Project(path, position);
                var crossesBody = forward >= from - TimeEpsilon && forward <= to + TimeEpsilon &&
                    Math.Abs(side) <= path.Width * .5 + TimeEpsilon;
                if (crossesBody && applyHit(Hit(path, ExpandedElijahChariotHitKind.Body, id, position, path.Damage)))
                {
                    path.BodyHitIds.Add(id);
                    path.BodyHitCount++;
                    path.WhirlwindCandidateIds.Remove(id);
                }
                else if (evolved && !path.WhirlwindCandidateIds.Contains(id))
                {
                    var outside = Math.Max(0, Math.Max(from - forward, forward - to));
                    if (side * side + outside * outside <= whirlwindRadius * whirlwindRadius + TimeEpsilon)
                        path.WhirlwindCandidateIds.Add(id);
                }
            }

            path.Distance = to;
            if (!path.BodyEnded || !evolved) return;
            // Resolve the outer overlap at the end of the pass so a target that later
            // crosses into the body receives the original hit instead of both.
            foreach (var id in path.WhirlwindCandidateIds.OrderBy(id => id))
                if (targets().TryGetValue(id, out var position))
                    applyHit(Hit(path, ExpandedElijahChariotHitKind.Whirlwind, id, position, path.Damage * WhirlwindDamageFactor));
            path.WhirlwindCandidateIds.Clear();
        }

        private static void AdvanceTrails(List<ExpandedElijahChariotPathState> paths, double now, int[] ids,
            Func<IReadOnlyDictionary<int, Vector2>> targets, Func<ExpandedElijahChariotHit, bool> applyHit)
        {
            Dictionary<int, ExpandedElijahChariotPathState>? winners = null;
            foreach (var path in paths)
            {
                if (path.SuppressedTrail) continue;
                var expires = path.ExpiresAt(true);
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
                        winner.Value.Damage * TrailDamageFactor));
        }

        private static bool InLiveTrail(ExpandedElijahChariotPathState path, Vector2 target, double at)
        {
            var (forward, side) = Project(path, target);
            if (forward < -TimeEpsilon || forward > path.Range + TimeEpsilon ||
                Math.Abs(side) > path.TrailWidth * .5 + TimeEpsilon)
                return false;
            // The strip grows as the body advances. Each point expires after its own lifetime.
            var laidAt = path.MoveAt + Math.Max(0, forward) / path.Speed;
            return laidAt <= at + TimeEpsilon && at < laidAt + path.TrailDuration - TimeEpsilon;
        }

        private static (double Forward, double Side) Project(ExpandedElijahChariotPathState path, Vector2 target)
        {
            var x = (double)target.X - path.StartX;
            var y = (double)target.Y - path.StartY;
            return (x * path.DirectionX + y * path.DirectionY, -x * path.DirectionY + y * path.DirectionX);
        }

        private static ExpandedElijahChariotHit Hit(ExpandedElijahChariotPathState path, ExpandedElijahChariotHitKind kind,
            int targetId, Vector2 position, double damage) =>
            new ExpandedElijahChariotHit(kind, targetId, position, damage, path);

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
