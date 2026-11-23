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
    }

    public readonly struct ExpandedDanielLionRankOptions
    {
        public bool PrioritizeElite { get; }
        public bool ExtendLeash { get; }
        public bool SpreadNormalTargets { get; }
        public bool StaggerFirstBossClaw { get; }
        public bool EmpowerLeap { get; }

        public ExpandedDanielLionRankOptions(bool prioritizeElite = false, bool extendLeash = false,
            bool spreadNormalTargets = false, bool staggerFirstBossClaw = false, bool empowerLeap = false)
        {
            PrioritizeElite = prioritizeElite;
            ExtendLeash = extendLeash;
            SpreadNormalTargets = spreadNormalTargets;
            StaggerFirstBossClaw = staggerFirstBossClaw;
            EmpowerLeap = empowerLeap;
        }

        public static ExpandedDanielLionRankOptions FromRanks(IReadOnlyDictionary<string, int> ranks)
        {
            if (ranks == null) throw new ArgumentNullException(nameof(ranks));
            bool Has(string id) => ranks.TryGetValue(id, out var rank) && rank > 0;
            return new ExpandedDanielLionRankOptions(Has("weapon.daniel_lion.a.01"), Has("weapon.daniel_lion.a.02"),
                Has("weapon.daniel_lion.b.01"), Has("weapon.daniel_lion.b.02"), Has("weapon.daniel_lion.e.01"));
        }
    }

    public readonly struct ExpandedDanielLionHit
    {
        public ExpandedDanielLionHitKind Kind { get; }
        public int LionIndex { get; }
        public int TargetId { get; }
        public Vector2 Position { get; }
        public Vector2 Center { get; }
        public double Damage { get; }
        public double Knockback { get; }

        internal ExpandedDanielLionHit(ExpandedDanielLionHitKind kind, int lionIndex, int targetId,
            Vector2 position, Vector2 center, double damage, double knockback = 0)
        {
            Kind = kind;
            LionIndex = lionIndex;
            TargetId = targetId;
            Position = position;
            Center = center;
            Damage = damage;
            Knockback = knockback;
        }
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

        public Vector2 Position
        {
            get => new Vector2(X, Y);
            internal set { X = value.X; Y = value.Y; }
        }

        internal ExpandedDanielLionActorState Copy() => (ExpandedDanielLionActorState)MemberwiseClone();
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
    public sealed class ExpandedDanielLionState
    {
        [DataMember(IsRequired = true)] public bool Evolved { get; set; }
        [DataMember(IsRequired = true)] public double LastStepAt { get; set; }
        [DataMember(IsRequired = true)] public double NextRoarAt { get; set; }
        [DataMember(IsRequired = true)] public long GuardianHits { get; set; }
        [DataMember(IsRequired = true)] public List<ExpandedDanielLionActorState> Lions { get; set; } =
            new List<ExpandedDanielLionActorState>();
        [DataMember(EmitDefaultValue = false)] public ExpandedDanielLionLeapState? Leap { get; set; }

        public ExpandedDanielLionState Copy() => new ExpandedDanielLionState
        {
            Evolved = Evolved,
            LastStepAt = LastStepAt,
            NextRoarAt = NextRoarAt,
            GuardianHits = GuardianHits,
            Lions = Lions.Select(lion => lion.Copy()).ToList(),
            Leap = Leap?.Copy(),
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
            foreach (var lion in Lions)
            {
                lion.TargetId = 0;
                lion.NextClawAt = now;
                lion.WaitingForTarget = false;
            }
        }

        public void Validate(double now, bool evolved)
        {
            const double epsilon = ExpandedDanielLion.TimeEpsilon;
            if (!BattleSnapshot.Finite(now) || now < 0 || Evolved != evolved ||
                !BattleSnapshot.Finite(LastStepAt) || LastStepAt < 0 || LastStepAt > now + epsilon ||
                Lions == null || Lions.Count > ExpandedDanielLion.MaximumAmount ||
                GuardianHits < 0 || GuardianHits > 0 && Lions.Count == 0 ||
                (evolved ? !BattleSnapshot.Finite(NextRoarAt) || NextRoarAt < LastStepAt - epsilon ||
                    NextRoarAt > LastStepAt + ExpandedDanielLion.RoarInterval + epsilon :
                    NextRoarAt != 0 || GuardianHits != 0 || Leap != null))
                throw new ArgumentException("Invalid Daniel lion clock");

            for (var index = 0; index < Lions.Count; index++)
            {
                var lion = Lions[index];
                if (lion == null || lion.Index != index || !BattleSnapshot.Position(lion.X, lion.Y) ||
                    lion.TargetId < 0 || lion.Returning && lion.TargetId != 0 ||
                    !BattleSnapshot.Finite(lion.NextClawAt) || lion.NextClawAt < 0 ||
                    lion.NextClawAt > LastStepAt + ExpandedDanielLion.MaximumCycle + epsilon ||
                    lion.WaitingForTarget && lion.NextClawAt > LastStepAt + epsilon)
                    throw new ArgumentException("Invalid Daniel lion actor");
            }

            var leap = Leap;
            if (leap != null && (Lions.Count == 0 || Lions[0].Returning ||
                GuardianHits == 0 || GuardianHits % ExpandedDanielLion.ClawsPerLeap != 0 ||
                !BattleSnapshot.Position(leap.OriginX, leap.OriginY) || !BattleSnapshot.Finite(leap.At) ||
                leap.At < LastStepAt - epsilon || leap.At > LastStepAt + ExpandedDanielLion.LeapInterval + epsilon ||
                !ExpandedDanielLion.ValidDamage(leap.Damage) ||
                leap.VisitedIds == null || leap.VisitedIds.Count < 1 || leap.VisitedIds.Count > 2 ||
                leap.VisitedIds.Any(id => id < 1) || leap.VisitedIds.Distinct().Count() != leap.VisitedIds.Count))
                throw new ArgumentException("Invalid Daniel lion leap");
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
        private const double RoarRadius = 1.8;
        private const double RoarDamageFactor = .35;
        private const double RoarKnockback = .45;
        private const double MinimumCycle = .15;

        // A hit callback returns true for accepted damage, including a kill. Only accepted
        // original claws count toward the next guardian leap or the returned original count.
        public static int Step(ExpandedDanielLionState state, double now, Vector2 playerPosition, int level,
            bool evolved, double damage, double cycle, int amount, double radius, double speed,
            Func<IReadOnlyDictionary<int, Vector2>> targets, Func<ExpandedDanielLionHit, bool> applyHit,
            ExpandedDanielLionRankOptions rankOptions = default, Func<int, bool>? isElite = null, Func<int, bool>? isBoss = null,
            Func<double>? afterClaw = null)
        {
            if (state == null) throw new ArgumentNullException(nameof(state));
            if (targets == null) throw new ArgumentNullException(nameof(targets));
            if (applyHit == null) throw new ArgumentNullException(nameof(applyHit));
            if ((rankOptions.PrioritizeElite || rankOptions.SpreadNormalTargets) && isElite == null)
                throw new ArgumentNullException(nameof(isElite));
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
            var originals = 0;
            var assigned = new HashSet<int>();
            foreach (var lion in state.Lions)
            {
                var guardian = evolved && lion.Index == 0;
                if (AdvanceLion(state, lion, guardian, now, playerPosition, damage, cycle, radius,
                    distance, assigned, targets, applyHit, rankOptions, isElite, isBoss, afterClaw))
                    originals++;
                if (guardian) Roar(state, lion, now, damage, targets, applyHit);
            }
            return originals;
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

        private static bool AdvanceLion(ExpandedDanielLionState state, ExpandedDanielLionActorState lion,
            bool guardian, double now, Vector2 player, double damage, double cycle, double radius,
            double distance, HashSet<int> assigned, Func<IReadOnlyDictionary<int, Vector2>> targets,
            Func<ExpandedDanielLionHit, bool> applyHit, ExpandedDanielLionRankOptions ranks,
            Func<int, bool>? isElite, Func<int, bool>? isBoss, Func<double>? afterClaw)
        {
            var leash = (float)(Profile.LeashDistance * (lion.ExtendedLeash ? 1.15 : 1));
            if (!Within(lion.Position, player, leash)) lion.Returning = true;
            if (lion.Returning)
            {
                lion.TargetId = 0;
                MarkWaitingIfDue(lion, now);
                if (guardian) state.Leap = null;
                lion.Position = Move(lion.Position, player, distance, Profile.ReturnRadius);
                lion.Returning = !Within(lion.Position, player, Profile.ReturnRadius);
                return false;
            }

            if (guardian && state.Leap != null)
            {
                lion.TargetId = 0;
                MarkWaitingIfDue(lion, now);
                AdvanceLeap(state, lion, now, player, targets, applyHit);
                return false;
            }

            var from = lion.Position;
            var (target, candidateCount) = SelectTarget(from, player, assigned, targets(), ranks, isElite);
            lion.TargetId = target.Key;
            if (target.Key == 0)
            {
                MarkWaitingIfDue(lion, now);
                lion.Position = Move(from, player, distance, Profile.ReturnRadius);
                return false;
            }

            assigned.Add(target.Key);
            lion.Position = Move(from, target.Value, distance, radius);
            if (lion.NextClawAt > now + TimeEpsilon) return false;
            if (!Within(lion.Position, target.Value, radius))
            {
                lion.WaitingForTarget = true;
                return false;
            }
            if (ranks.StaggerFirstBossClaw && !lion.FirstClawPhaseResolved &&
                lion.Index > 0 && candidateCount == 1 && isBoss!(target.Key))
            {
                lion.FirstClawPhaseResolved = true;
                lion.NextClawAt = now + lion.Index * FirstBossClawPhasePerIndex;
                lion.WaitingForTarget = false;
                return false;
            }
            if (!applyHit(new ExpandedDanielLionHit(ExpandedDanielLionHitKind.Claw,
                    lion.Index, target.Key, target.Value, lion.Position, damage)))
            {
                lion.WaitingForTarget = true;
                return false;
            }

            var at = lion.WaitingForTarget ? now : lion.NextClawAt;
            var nextWait = afterClaw?.Invoke() ?? cycle;
            if (!BattleSnapshot.Finite(nextWait) || nextWait < MinimumCycle || nextWait > MaximumCycle)
                throw new ArgumentOutOfRangeException(nameof(afterClaw));
            lion.NextClawAt = at + nextWait;
            lion.WaitingForTarget = false;
            lion.FirstClawPhaseResolved |= ranks.StaggerFirstBossClaw;
            if (guardian && ++state.GuardianHits % ClawsPerLeap == 0)
                state.Leap = new ExpandedDanielLionLeapState
                {
                    At = at + LeapInterval,
                    Damage = damage,
                    Origin = target.Value,
                    VisitedIds = new List<int> { target.Key },
                    EmpoweredDamage = ranks.EmpowerLeap,
                };
            return true;
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
            var candidates = targets().Where(entry => !leap.VisitedIds.Contains(entry.Key) &&
                Within(entry.Value, origin, LeapRadius) && Within(entry.Value, player, Profile.TargetSearchRadius))
                .OrderBy(entry => DistanceSquared(entry.Value, origin)).ThenBy(entry => entry.Key).ToArray();
            var factor = (leap.VisitedIds.Count == 1 ? 1 : .6) + (leap.EmpoweredDamage ? .15 : 0);
            foreach (var candidate in candidates)
            {
                if (!applyHit(new ExpandedDanielLionHit(ExpandedDanielLionHitKind.Leap, lion.Index,
                    candidate.Key, candidate.Value, origin, leap.Damage * factor))) continue;
                lion.Position = candidate.Value;
                if (leap.VisitedIds.Count == 2) break;
                leap.VisitedIds.Add(candidate.Key);
                leap.Origin = candidate.Value;
                leap.At += LeapInterval;
                return;
            }
            state.Leap = null;
        }

        private static void Roar(ExpandedDanielLionState state, ExpandedDanielLionActorState lion, double now,
            double damage, Func<IReadOnlyDictionary<int, Vector2>> targets, Func<ExpandedDanielLionHit, bool> applyHit)
        {
            if (state.NextRoarAt > now + TimeEpsilon) return;
            state.NextRoarAt += RoarInterval;
            var center = lion.Position;
            var ids = targets().Where(entry => Within(entry.Value, center, RoarRadius))
                .Select(entry => entry.Key).OrderBy(id => id).ToArray();
            foreach (var id in ids)
                if (targets().TryGetValue(id, out var position) && Within(position, center, RoarRadius))
                    applyHit(new ExpandedDanielLionHit(ExpandedDanielLionHitKind.Roar, lion.Index,
                        id, position, center, damage * RoarDamageFactor, RoarKnockback));
        }

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
