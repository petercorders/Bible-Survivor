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

        public ExpandedElijahChariotRankOptions(bool extendTravel = false, bool widenFormation = false,
            bool knockBack = false, bool widenWhirlwind = false, bool extendTrail = false)
        {
            ExtendTravel = extendTravel;
            WidenFormation = widenFormation;
            KnockBack = knockBack;
            WidenWhirlwind = widenWhirlwind;
            ExtendTrail = extendTrail;
        }

        public static ExpandedElijahChariotRankOptions FromRanks(IReadOnlyDictionary<string, int> ranks)
        {
            if (ranks == null) throw new ArgumentNullException(nameof(ranks));
            bool Has(string id) => ranks.TryGetValue(id, out var rank) && rank > 0;
            return new ExpandedElijahChariotRankOptions(Has("weapon.elijah_chariot.a.01"), Has("weapon.elijah_chariot.b.01"),
                Has("weapon.elijah_chariot.d.01"), Has("weapon.elijah_chariot.e.01"), Has("weapon.elijah_chariot.e.02"));
        }
    }

    public readonly struct ExpandedElijahChariotHit
    {
        public ExpandedElijahChariotHitKind Kind { get; }
        public int TargetId { get; }
        public Vector2 Position { get; }
        public Vector2 Direction { get; }
        public double Damage { get; }
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
        [DataMember(EmitDefaultValue = false)] public bool SuppressedTrail { get; set; }
        [DataMember(EmitDefaultValue = false)] public bool ForwardKnockback { get; set; }
        [DataMember(EmitDefaultValue = false)] public bool WiderWhirlwind { get; set; }
        [DataMember(EmitDefaultValue = false)] public bool ExtendedTrail { get; set; }
        [DataMember(IsRequired = true)] public double Range { get; set; }
        [DataMember(IsRequired = true)] public double Speed { get; set; }
        [DataMember(IsRequired = true)] public double Width { get; set; }
        [DataMember(IsRequired = true)] public double Damage { get; set; }
        [DataMember(IsRequired = true)] public double Distance { get; set; }
        [DataMember(IsRequired = true)] public int TrailTicksFired { get; set; }
        [DataMember(IsRequired = true)] public int BodyHitCount { get; set; }
        [DataMember(IsRequired = true)] public List<int> BodyHitIds { get; set; } = new List<int>();
        [DataMember(IsRequired = true)] public List<int> WhirlwindCandidateIds { get; set; } = new List<int>();

        public Vector2 Origin => new Vector2(StartX, StartY);
        public Vector2 Direction => new Vector2(DirectionX, DirectionY);
        public Vector2 Position => Origin + Direction * (float)Distance;
        public double WarningAt => MoveAt - WarningDelay;
        private double WarningDelay => FirstDelay ?? ExpandedElijahChariot.Profile.FirstAttackDelay;
        public double EndAt => MoveAt + Range / Speed;
        public bool BodyEnded => Distance >= Range;
        public double WhirlwindRadius => ExpandedElijahChariot.BaseWhirlwindRadius * (WiderWhirlwind ? 1.15 : 1);
        public double TrailDuration => ExpandedElijahChariot.BaseTrailDuration * (ExtendedTrail ? 1.20 : 1);

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
            foreach (var path in Paths)
            {
                if (path == null || path.Id <= previousId || path.Id >= NextPathId || path.Cycle < 1 || path.Cycle > Cycle ||
                    !BattleSnapshot.Position(path.StartX, path.StartY) ||
                    !BattleSnapshot.Position(path.DirectionX, path.DirectionY) ||
                    Math.Abs(path.Direction.LengthSquared() - 1) > .0001 ||
                    !ExpandedElijahChariot.ValidPath(path.Damage, path.Width, path.Speed, path.Range) ||
                    !BattleSnapshot.Finite(path.MoveAt) || !ExpandedElijahChariot.ValidDelay(path.FirstDelay) || path.WarningAt < -epsilon || path.WarningAt > now + epsilon ||
                    path.WarningAt + ExpandedElijahChariot.MinimumCycle > NextCycle + epsilon ||
                    path.ExpiresAt(evolved) < now - epsilon ||
                    !BattleSnapshot.Finite(path.Distance) || path.Distance < 0 || path.Distance > path.Range ||
                    Math.Abs(path.Distance - path.DistanceAt(now)) > .000001)
                    throw new ArgumentException("Invalid Elijah chariot path");

                ValidateHitState(path, now, evolved);
                previousId = path.Id;
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
                (path.SuppressedTrail || path.WiderWhirlwind) && !evolved || path.ExtendedTrail && !hasTrail ||
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
        private static readonly double MaximumWidth = Profile.AtLevel(1).Width * 2.5;
        private static readonly double MinimumSpeed = Profile.AtLevel(1).Speed;
        private static readonly double MaximumSpeed = MinimumSpeed * 1.75;
        private static readonly double TravelBonus = Profile.TravelDistance * .12;
        private static readonly double MaximumRange = Profile.TravelDistance + TravelBonus;
        internal static readonly int MaximumPaths = MaximumAmount *
            (int)Math.Ceiling((Profile.FirstAttackDelay + MaximumRange / MinimumSpeed + BaseTrailDuration * 1.20) / MinimumCycle);
        internal const double TimeEpsilon = 1e-9;
        internal const double MinimumCycle = .15;
        private const double MinimumDelay = .05;
        internal const double TrailInterval = .5;
        internal const double BaseTrailDuration = 1.5;
        internal const int MaximumLiveTargets = 120;
        internal const int MaximumBodyHits = 6210;
        private const double TrailWidth = .9;
        private const double TrailDamageFactor = .35;
        internal const double BaseWhirlwindRadius = 1.15;
        private const double WhirlwindDamageFactor = .45;

        public static void Step(ExpandedElijahChariotState state, double now, Vector2 playerPosition, Vector2 facing,
            int level, bool evolved, double damage, double cycle, int amount, double width, double speed, double range,
            Func<IReadOnlyDictionary<int, Vector2>> targets, Func<ExpandedElijahChariotHit, bool> applyHit,
            double delayAdvance = 0, Func<bool>? reserveOriginal = null, Func<bool>? reserveTrail = null,
            ExpandedElijahChariotRankOptions rankOptions = default, Func<double>? nextCycleDuration = null)
        {
            if (state == null) throw new ArgumentNullException(nameof(state));
            if (targets == null) throw new ArgumentNullException(nameof(targets));
            if (applyHit == null) throw new ArgumentNullException(nameof(applyHit));
            if (!BattleSnapshot.Finite(delayAdvance) || delayAdvance < 0) throw new ArgumentOutOfRangeException(nameof(delayAdvance));
            if (!BattleSnapshot.Finite(now) || now < 0 ||
                !BattleSnapshot.Position(playerPosition.X, playerPosition.Y) || !BattleSnapshot.Position(facing.X, facing.Y) ||
                level < 1 || level > 8 || evolved && level != 8 || amount < 1 || amount > MaximumAmount ||
                !BattleSnapshot.Finite(cycle) || cycle < MinimumCycle || cycle > MaximumCycle ||
                !ValidPath(damage, width, speed, range) || range > Profile.TravelDistance)
                throw new ArgumentException("Invalid Elijah chariot parameters");

            if (state.Evolved != evolved) state.ResetForForm(evolved, now);
            AdvancePaths(state, now, targets, applyHit);
            if (state.NextCycle > now + TimeEpsilon) return;

            var direction = facing.LengthSquared() > 1e-12f ? Vector2.Normalize(facing) : Vector2.UnitX;
            var perpendicular = new Vector2(-direction.Y, direction.X);
            var spacing = Profile.ParallelSpacing + (rankOptions.WidenFormation ? .30 : 0);
            var travel = range + (rankOptions.ExtendTravel ? TravelBonus : 0);
            var delay = Math.Max(MinimumDelay, Profile.FirstAttackDelay - delayAdvance);
            var moveAt = state.NextCycle + delay;
            state.Cycle = checked(state.Cycle + 1);
            state.NextCycle += nextCycleDuration?.Invoke() ?? cycle;
            for (var index = 0; index < amount; index++)
            {
                if (reserveOriginal?.Invoke() == false)
                {
                    state.SuppressedOriginals = checked(state.SuppressedOriginals + amount - index);
                    break;
                }
                var offset = (index - (amount - 1) * .5) * spacing;
                var start = playerPosition + perpendicular * (float)offset;
                var suppressedTrail = evolved && reserveTrail?.Invoke() == false;
                state.Paths.Add(new ExpandedElijahChariotPathState
                {
                    Id = state.NextPathId,
                    Cycle = state.Cycle,
                    StartX = start.X,
                    StartY = start.Y,
                    DirectionX = direction.X,
                    DirectionY = direction.Y,
                    MoveAt = moveAt,
                    FirstDelay = delay == Profile.FirstAttackDelay ? (double?)null : delay,
                    SuppressedTrail = suppressedTrail,
                    ForwardKnockback = rankOptions.KnockBack,
                    WiderWhirlwind = evolved && rankOptions.WidenWhirlwind,
                    ExtendedTrail = evolved && !suppressedTrail && rankOptions.ExtendTrail,
                    Range = travel,
                    Speed = speed,
                    Width = width,
                    Damage = damage,
                });
                state.NextPathId = checked(state.NextPathId + 1);
            }
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
                Math.Abs(side) > TrailWidth * .5 + TimeEpsilon)
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

        internal static bool ValidPath(double damage, double width, double speed, double range) =>
            BattleSnapshot.Finite(damage) && damage >= 1 && damage <= MaximumDamage &&
            BattleSnapshot.Finite(width) && width > 0 && width <= MaximumWidth &&
            BattleSnapshot.Finite(speed) && speed >= MinimumSpeed && speed <= MaximumSpeed &&
            BattleSnapshot.Finite(range) && range > 0 && range <= MaximumRange;
    }
}
