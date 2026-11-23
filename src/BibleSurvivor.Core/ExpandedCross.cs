using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.Serialization;

namespace BibleSurvivor.Core
{
    public enum ExpandedCrossHitKind { Outbound, Return, Thorns }

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

        public ExpandedCrossRankOptions(bool accelerateOutbound = false, bool prioritizeElite = false,
            bool accelerateReturn = false, bool increaseLegBudget = false, bool widenCollision = false, bool extendThorns = false,
            bool increaseSharedLegBudget = false, bool shortenRehitInterval = false)
        {
            AccelerateOutbound = accelerateOutbound;
            PrioritizeElite = prioritizeElite;
            AccelerateReturn = accelerateReturn;
            IncreaseLegBudget = increaseLegBudget;
            WidenCollision = widenCollision;
            ExtendThorns = extendThorns;
            IncreaseSharedLegBudget = increaseSharedLegBudget;
            ShortenRehitInterval = shortenRehitInterval;
        }

        public static ExpandedCrossRankOptions FromRanks(IReadOnlyDictionary<string, int> ranks)
        {
            if (ranks == null) throw new ArgumentNullException(nameof(ranks));
            bool Has(string id) => ranks.TryGetValue(id, out var rank) && rank > 0;
            return new ExpandedCrossRankOptions(Has("weapon.cross.a.01"), Has("weapon.cross.a.02"),
                Has("weapon.cross.b.01"), Has("weapon.cross.c.01"), Has("weapon.cross.c.02"), Has("weapon.cross.e.01"),
                Has("common.shot.03"), Has("common.tempo.05"));
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
        public long ShotId { get; }
        public long Cycle { get; }

        internal ExpandedCrossHit(ExpandedCrossHitKind kind, int targetId, Vector2 position, Vector2 origin,
            ExpandedCrossShotState shot)
        {
            Kind = kind;
            TargetId = targetId;
            Position = position;
            Origin = origin;
            Direction = shot.Direction;
            Damage = kind == ExpandedCrossHitKind.Thorns ? shot.Damage * ExpandedCross.ThornDamageFactor : shot.Damage;
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

        [DataMember(EmitDefaultValue = false)] public double? TrainedOutboundSpeed { get; set; }
        [DataMember(EmitDefaultValue = false)] public double? TrainedReturnSpeed { get; set; }
        [DataMember(EmitDefaultValue = false)] public bool IncreasedLegBudget { get; set; }
        [DataMember(EmitDefaultValue = false)] public bool IncreasedSharedLegBudget { get; set; }
        [DataMember(EmitDefaultValue = false)] public bool ExtendedThorns { get; set; }
        [DataMember(EmitDefaultValue = false)] public bool ShortenedRehitInterval { get; set; }

        public double OutboundSpeed => TrainedOutboundSpeed ?? Speed;
        public double ReturnSpeed => TrainedReturnSpeed ?? Speed;
        public double ThornDuration => ExpandedCross.BaseThornDuration * (ExtendedThorns ? 1.20 : 1);
        public int HitBudget(bool evolved) => Math.Min(8, (evolved ? 6 : ExpandedCross.Profile.HitsPerLeg) +
            (IncreasedLegBudget ? 1 : 0) + (IncreasedSharedLegBudget ? 1 : 0));

        public Vector2 Start => new Vector2(StartX, StartY);
        public Vector2 Position => new Vector2(X, Y);
        public Vector2 Direction => new Vector2(DirectionX, DirectionY);
        public bool Returning => ReturnStartedAt.HasValue;
        public bool IsFlying => !EndedAt.HasValue;
        public bool HasTrail => LeavesThorns && (IsFlying || Thorns.Count > 0);

        internal ExpandedCrossShotState Copy()
        {
            var copy = (ExpandedCrossShotState)MemberwiseClone();
            copy.HitTargets = HitTargets.Select(target => target.Copy()).ToList();
            copy.Thorns = Thorns.Select(thorn => thorn.Copy()).ToList();
            return copy;
        }

        internal void MoveTo(Vector2 position) { X = position.X; Y = position.Y; }

        internal void Face(Vector2 direction) { DirectionX = direction.X; DirectionY = direction.Y; }
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
        public int ActiveTrailCount => Shots.Count(shot => shot.HasTrail);

        public ExpandedCrossState Copy() => new ExpandedCrossState
        {
            Evolved = Evolved,
            Cycle = Cycle,
            NextShotId = NextShotId,
            NextCycle = NextCycle,
            Shots = Shots.Select(shot => shot.Copy()).ToList(),
        };

        public void ResetForForm(bool evolved, double now)
        {
            if (!ExpandedCross.Finite(now) || now < 0) throw new ArgumentOutOfRangeException(nameof(now));
            Evolved = evolved;
            NextCycle = now;
            Shots.Clear();
        }

        public void Validate(double now, bool evolved)
        {
            if (!ExpandedCross.Finite(now) || now < 0 || Evolved != evolved || Cycle < 0 || NextShotId < 1 ||
                (NextShotId > 1 && (NextShotId - 2) / ExpandedCross.MaximumAmount >= Cycle) ||
                !ExpandedCross.Finite(NextCycle) || NextCycle < 0 ||
                NextCycle > now + ExpandedCross.MaximumCycle + ExpandedCross.Epsilon ||
                Shots == null || Shots.Count > ExpandedCross.MaximumShots + ExpandedCross.MaximumTrails)
                throw new ArgumentException("Invalid expanded cross clock");

            long previousId = 0;
            var flying = 0;
            var trails = 0;
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
                    shot.HitTargets == null || shot.Thorns == null || (shot.LeavesThorns && !evolved))
                    throw new ArgumentException("Invalid expanded cross shot");

                if (shot.Returning)
                {
                    var start = shot.ReturnStartedAt!.Value;
                    if (!ExpandedCross.Within(start, shot.EmittedAt, shot.SimulatedAt) || shot.Distance != shot.Range ||
                        Math.Abs(start - (shot.EmittedAt + shot.Range / shot.OutboundSpeed)) > ExpandedCross.Epsilon ||
                        (shot.IsFlying && shot.SimulatedAt >= start + ExpandedCross.Profile.ReturnTimeout))
                        throw new ArgumentException("Invalid expanded cross return");
                }
                else if (!shot.IsFlying || shot.Distance >= shot.Range ||
                    Math.Abs(shot.Distance - (shot.SimulatedAt - shot.EmittedAt) * shot.OutboundSpeed) > ExpandedCross.Epsilon)
                    throw new ArgumentException("Invalid expanded cross outward flight");

                if (shot.IsFlying)
                {
                    if (++flying > ExpandedCross.MaximumShots || shot.SimulatedAt < now - ExpandedCross.Epsilon)
                        throw new ArgumentException("Invalid expanded cross flight clock");
                }
                else if (!shot.HasTrail || !ExpandedCross.Within(shot.EndedAt!.Value, shot.ReturnStartedAt!.Value,
                    Math.Min(shot.SimulatedAt, shot.ReturnStartedAt.Value + ExpandedCross.Profile.ReturnTimeout)))
                    throw new ArgumentException("Invalid expanded cross expiry");

                ValidateHits(shot, shot.HitBudget(evolved));
                ValidateTrail(shot, now);
                if (shot.HasTrail && ++trails > ExpandedCross.MaximumTrails)
                    throw new ArgumentException("Invalid expanded cross trail count");
                previousId = shot.Id;
            }
        }

        private static void ValidateHits(ExpandedCrossShotState shot, int budget)
        {
            var seen = new HashSet<int>();
            var outward = 0;
            var returning = 0;
            if (shot.HitTargets.Count > 2 * budget) throw new ArgumentException("Invalid expanded cross hit count");
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
            if (outward != shot.OutboundHits || returning != shot.ReturnHits || outward > budget || returning > budget)
                throw new ArgumentException("Invalid expanded cross hit budgets");
        }

        private static void ValidateTrail(ExpandedCrossShotState shot, double now)
        {
            if ((!shot.LeavesThorns && (shot.NextThornAt.HasValue || shot.Thorns.Count != 0 || shot.ExtendedThorns)) ||
                shot.Thorns.Count > ExpandedCross.ThornPieceLimit(shot.ThornDuration) ||
                (shot.Thorns.Count > 0 && !shot.NextThornAt.HasValue) ||
                (shot.NextThornAt.HasValue && !ExpandedCross.Within(shot.NextThornAt.Value, now, now + ExpandedCross.ThornInterval)))
                throw new ArgumentException("Invalid expanded cross trail");

            double last = double.NegativeInfinity;
            foreach (var piece in shot.Thorns)
            {
                if (piece == null || piece.BornAt < last ||
                    !ExpandedCross.Within(piece.BornAt, shot.EmittedAt, shot.EndedAt ?? shot.SimulatedAt) ||
                    piece.BornAt + shot.ThornDuration <= now + ExpandedCross.Epsilon ||
                    !ExpandedCross.ValidPosition(piece.Start) || !ExpandedCross.ValidPosition(piece.End) ||
                    (piece.Returning && (!shot.Returning || piece.BornAt < shot.ReturnStartedAt!.Value)) ||
                    (!piece.Returning && piece.BornAt > shot.ReturnStartedAt))
                    throw new ArgumentException("Invalid expanded cross thorn piece");
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
        private static readonly double MinimumSpeed = Profile.AtLevel(1).Speed;
        private static readonly double MaximumSpeed = Profile.AtLevel(8).Speed * 1.75;
        private static readonly double MaximumRange = Profile.TravelDistance * 2.5;
        private static readonly double MaximumRadius = Profile.AtLevel(1).Radius * 2.5;
        private const double MinimumCycle = .15;
        internal static readonly int MaximumShots =
            MaximumAmount * ((int)Math.Ceiling((MaximumRange / MinimumSpeed + Profile.ReturnTimeout) / MinimumCycle) + 1);
        internal const int MaximumTrails = 16;
        internal const double BaseThornDuration = 1.5;
        internal const double ThornInterval = .5;
        internal const double ThornDamageFactor = .35;
        private const double ThornRadius = .75 / 2;
        internal const double Epsilon = 1e-8;

        public static int Step(ExpandedCrossState state, double now, Vector2 playerPosition, Vector2 facing, int level,
            bool evolved, double damage, double cycle, int amount, double radius, double range, double speed,
            Func<IReadOnlyDictionary<int, Vector2>> targets, IReadOnlyDictionary<int, Vector2> previousTargets,
            Func<ExpandedCrossHit, bool> applyHit, Func<bool>? reserveOriginal = null, Func<bool>? reserveTrail = null,
            ExpandedCrossRankOptions rankOptions = default, Func<int, bool>? isElite = null,
            Func<double>? nextCycleDuration = null)
        {
            if (state == null) throw new ArgumentNullException(nameof(state));
            if (targets == null) throw new ArgumentNullException(nameof(targets));
            if (previousTargets == null) throw new ArgumentNullException(nameof(previousTargets));
            if (applyHit == null) throw new ArgumentNullException(nameof(applyHit));
            if (rankOptions.PrioritizeElite && isElite == null) throw new ArgumentNullException(nameof(isElite));
            if (!Finite(now) || now < 0 || !ValidPosition(playerPosition) || !ValidPosition(facing) ||
                level < 1 || level > 8 || (evolved && level != 8) || !Within(damage, 1, MaximumDamage) ||
                !Within(cycle, MinimumCycle, MaximumCycle) || amount < 1 || amount > MaximumAmount ||
                !ValidGeometry(radius, range, speed))
                throw new ArgumentException("Invalid expanded cross parameters");

            if (state.Evolved != evolved) state.ResetForForm(evolved, now);
            foreach (var shot in state.Shots)
            {
                if (shot.IsFlying) Move(shot, now, playerPosition, shot.HitBudget(evolved), targets, previousTargets, applyHit);
                shot.Thorns.RemoveAll(piece => piece.BornAt + shot.ThornDuration <= now + Epsilon);
            }
            if (evolved) ResolveThorns(state.Shots, now, targets, previousTargets, applyHit);
            state.Shots.RemoveAll(shot => !shot.IsFlying && !shot.HasTrail);
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
            var active = state.ActiveShotCount;
            var trails = state.ActiveTrailCount;
            var emitted = 0;
            for (var index = 0; index < amount; index++)
            {
                if (active + emitted >= MaximumShots || (reserveOriginal != null && !reserveOriginal())) break;
                var angle = (index - (amount - 1) * .5) * Profile.SpreadDegrees * Math.PI / 180;
                var direction = new Vector2((float)(aim.X * Math.Cos(angle) - aim.Y * Math.Sin(angle)),
                    (float)(aim.X * Math.Sin(angle) + aim.Y * Math.Cos(angle)));
                var leavesThorns = evolved && trails < MaximumTrails && (reserveTrail?.Invoke() ?? true);
                if (leavesThorns) trails++;
                state.Shots.Add(new ExpandedCrossShotState
                {
                    Id = state.NextShotId, Cycle = state.Cycle, EmittedAt = now, SimulatedAt = now,
                    StartX = playerPosition.X, StartY = playerPosition.Y, X = playerPosition.X, Y = playerPosition.Y,
                    DirectionX = direction.X, DirectionY = direction.Y, Damage = damage, Radius = radius,
                    Range = range, Speed = speed, LeavesThorns = leavesThorns,
                    TrainedOutboundSpeed = rankOptions.AccelerateOutbound ? trainedSpeed : null,
                    TrainedReturnSpeed = rankOptions.AccelerateReturn ? trainedSpeed : null,
                    IncreasedLegBudget = rankOptions.IncreaseLegBudget,
                    IncreasedSharedLegBudget = rankOptions.IncreaseSharedLegBudget,
                    ExtendedThorns = leavesThorns && rankOptions.ExtendThorns,
                    ShortenedRehitInterval = rankOptions.ShortenRehitInterval,
                });
                state.NextShotId = checked(state.NextShotId + 1);
                emitted++;
            }
            return emitted;
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

        private static void Move(ExpandedCrossShotState shot, double now, Vector2 player, int budget,
            Func<IReadOnlyDictionary<int, Vector2>> targets, IReadOnlyDictionary<int, Vector2> previous,
            Func<ExpandedCrossHit, bool> applyHit)
        {
            var start = shot.SimulatedAt;
            if (now <= start + Epsilon) return;
            var segmentStart = start;
            shot.SimulatedAt = now;
            if (!shot.Returning)
            {
                var turnAt = shot.EmittedAt + shot.Range / shot.OutboundSpeed;
                var until = Math.Min(now, turnAt);
                shot.Distance = until >= turnAt ? shot.Range : (until - shot.EmittedAt) * shot.OutboundSpeed;
                var destination = shot.Start + shot.Direction * (float)shot.Distance;
                Advance(
                    shot, destination, start, until, start, now, budget, targets, previous, applyHit);
                if (now < turnAt) return;
                shot.ReturnStartedAt = turnAt;
                segmentStart = turnAt;
            }

            var returning = player - shot.Position;
            var length = returning.Length();
            var required = Math.Max(0, length - Profile.ReturnRadius);
            var expiry = shot.ReturnStartedAt!.Value + Profile.ReturnTimeout;
            var catchAt = segmentStart + required / shot.ReturnSpeed;
            var end = Math.Min(Math.Min(expiry, catchAt), now);
            if (required > 0 && end > segmentStart + Epsilon)
            {
                shot.Face(Vector2.Normalize(returning));
                var destination = shot.Position + shot.Direction * (float)((end - segmentStart) * shot.ReturnSpeed);
                Advance(shot, destination, segmentStart, end, start, now, budget, targets, previous, applyHit);
            }
            if (end >= Math.Min(expiry, catchAt)) shot.EndedAt = end;
        }

        private static void Advance(ExpandedCrossShotState shot, Vector2 end, double fromAt, double toAt, double tickStart,
            double tickEnd, int budget, Func<IReadOnlyDictionary<int, Vector2>> targets,
            IReadOnlyDictionary<int, Vector2> previous, Func<ExpandedCrossHit, bool> applyHit)
        {
            var start = shot.Position;
            shot.MoveTo(end);
            var duration = toAt - fromAt;
            if (start == end || duration <= Epsilon) return;
            var returning = shot.Returning;
            var kind = returning ? ExpandedCrossHitKind.Return : ExpandedCrossHitKind.Outbound;
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
                    !applyHit(new ExpandedCrossHit(kind, id, current, Vector2.Lerp(partial, end, (float)contact.Value), shot)))
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
            if (!shot.LeavesThorns) return;
            shot.NextThornAt ??= tickEnd;
            shot.Thorns.Add(new ExpandedCrossThornState
            {
                StartX = start.X, StartY = start.Y, EndX = end.X, EndY = end.Y, BornAt = toAt, Returning = returning,
            });
        }

        private static void ResolveThorns(IReadOnlyList<ExpandedCrossShotState> shots, double now,
            Func<IReadOnlyDictionary<int, Vector2>> targets, IReadOnlyDictionary<int, Vector2> previous,
            Func<ExpandedCrossHit, bool> applyHit)
        {
            var due = shots.Where(shot => shot.HasTrail && shot.NextThornAt <= now + Epsilon).ToArray();
            if (due.Length == 0) return;

            // Each trail is one support area for overlap. Its recent path pieces expire
            // individually, so the moving return path never refreshes older outgoing ground.
            var winners = new Dictionary<int, (ExpandedCrossShotState shot, Vector2 origin)>();
            var live = targets();
            foreach (var shot in due.OrderByDescending(shot => shot.Damage).ThenBy(shot => shot.Id))
            {
                var next = shot.NextThornAt!.Value;
                do { next += ThornInterval; } while (next <= now + Epsilon);
                shot.NextThornAt = next;
                foreach (var target in live)
                {
                    if (winners.ContainsKey(target.Key)) continue;
                    var old = previous.TryGetValue(target.Key, out var before) ? before : target.Value;
                    foreach (var piece in shot.Thorns)
                    {
                        if (!CrossesThorn(old, target.Value, piece)) continue;
                        winners.Add(target.Key, (shot, piece.Start));
                        break;
                    }
                }
            }
            foreach (var hit in winners.OrderBy(entry => entry.Value.shot.Id).ThenBy(entry => entry.Key))
                if (targets().TryGetValue(hit.Key, out var position))
                    applyHit(new ExpandedCrossHit(ExpandedCrossHitKind.Thorns, hit.Key, position, hit.Value.origin, hit.Value.shot));
        }

        private static bool CrossesThorn(Vector2 from, Vector2 to, ExpandedCrossThornState piece)
        {
            var start = piece.Start;
            var end = piece.End;
            if (Math.Max(from.X, to.X) < Math.Min(start.X, end.X) - ThornRadius - Epsilon ||
                Math.Min(from.X, to.X) > Math.Max(start.X, end.X) + ThornRadius + Epsilon ||
                Math.Max(from.Y, to.Y) < Math.Min(start.Y, end.Y) - ThornRadius - Epsilon ||
                Math.Min(from.Y, to.Y) > Math.Max(start.Y, end.Y) + ThornRadius + Epsilon)
                return false;
            if (CircleContact(from - start, to - start, ThornRadius).HasValue ||
                CircleContact(from - end, to - end, ThornRadius).HasValue)
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
                Clip(Vector2.Dot(from, vertical), Vector2.Dot(to, vertical), -ThornRadius, ThornRadius, ref lower, ref upper);
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

        // A 60 Hz strip can also acquire one extra piece when a step straddles the turn.
        internal static int ThornPieceLimit(double duration) => (int)Math.Ceiling(duration * 60) + 3;

        private static double? ResolveTrainedSpeed(double speed, double basis)
        {
            var trained = Math.Min(basis * 1.75, speed + basis * .12);
            return trained > speed ? trained : (double?)null;
        }

        internal static bool ValidTrainedSpeed(double untrained, double? trained) => !trained.HasValue ||
            (trained.Value > untrained && Within(trained.Value, MinimumSpeed,
                Math.Min(MaximumSpeed, untrained + Profile.AtLevel(8).Speed * .12)));

        internal static bool Finite(double number) => !double.IsNaN(number) && !double.IsInfinity(number);
        internal static bool Within(double value, double minimum, double maximum) =>
            Finite(value) && value >= minimum - Epsilon && value <= maximum + Epsilon;
        internal static bool ValidPosition(Vector2 value) => Within(value.X, -1e6, 1e6) && Within(value.Y, -1e6, 1e6);
        internal static bool UnitDirection(Vector2 value) => ValidPosition(value) && Math.Abs(value.LengthSquared() - 1) <= 1e-5;
        internal static bool ValidGeometry(double radius, double range, double speed) => radius > 0 && range > 0 &&
            Within(radius, 0, MaximumRadius) && Within(range, 0, MaximumRange) && Within(speed, MinimumSpeed, MaximumSpeed);
    }
}
