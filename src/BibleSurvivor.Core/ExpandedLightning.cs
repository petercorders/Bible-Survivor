using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.Serialization;

namespace BibleSurvivor.Core
{
    public enum ExpandedLightningHitKind
    {
        Strike,
        Chain,
        Aftershock,
    }

    public readonly struct ExpandedLightningHit
    {
        public ExpandedLightningHitKind Kind { get; }
        public int TargetId { get; }
        public Vector2 Position { get; }
        public double Damage { get; }
        public long StrikeId { get; }
        public long Cycle { get; }

        internal ExpandedLightningHit(ExpandedLightningHitKind kind, int targetId, Vector2 position,
            double damage, long strikeId, long cycle)
        {
            Kind = kind;
            TargetId = targetId;
            Position = position;
            Damage = damage;
            StrikeId = strikeId;
            Cycle = cycle;
        }
    }

    public readonly struct ExpandedLightningSettings
    {
        public double RadiusBonus { get; }
        public double DelayAdvance { get; }
        public bool PrioritizeCrowds { get; }
        public bool PrioritizeThreats { get; }
        public bool DeferPreviousTargetOnReuse { get; }
        public bool RetargetOnce { get; }
        public bool LongerChain { get; }
        internal Func<int, bool>? IsBoss { get; }
        internal Func<int, bool>? IsElite { get; }

        public ExpandedLightningSettings(double radiusBonus = 0, double delayAdvance = 0,
            bool prioritizeCrowds = false, bool prioritizeThreats = false, bool deferPreviousTargetOnReuse = false,
            bool retargetOnce = false, bool longerChain = false, Func<int, bool>? isBoss = null, Func<int, bool>? isElite = null)
        {
            if (!ExpandedLightning.Finite(radiusBonus) || radiusBonus < 0 ||
                !ExpandedLightning.Finite(delayAdvance) || delayAdvance < 0)
                throw new ArgumentException("Invalid expanded lightning bonuses");
            if (prioritizeThreats && (isBoss == null || isElite == null))
                throw new ArgumentException("Lightning threat priority needs boss and elite classifiers");

            RadiusBonus = radiusBonus;
            DelayAdvance = delayAdvance;
            PrioritizeCrowds = prioritizeCrowds;
            PrioritizeThreats = prioritizeThreats;
            DeferPreviousTargetOnReuse = deferPreviousTargetOnReuse;
            RetargetOnce = retargetOnce;
            LongerChain = longerChain;
            IsBoss = isBoss;
            IsElite = isElite;
        }

        public static ExpandedLightningSettings FromRanks(IReadOnlyDictionary<string, int> ranks,
            Func<int, bool>? isBoss = null, Func<int, bool>? isElite = null)
        {
            if (ranks == null) throw new ArgumentNullException(nameof(ranks));
            bool Has(string suffix) => ranks.TryGetValue("weapon.lightning." + suffix, out var rank) && rank > 0;

            return new ExpandedLightningSettings(
                radiusBonus: Has("base.04") ? .10 : 0,
                delayAdvance: Has("base.05") ? .08 : 0,
                prioritizeCrowds: Has("a.01"),
                prioritizeThreats: Has("a.02"),
                deferPreviousTargetOnReuse: Has("a.03"),
                retargetOnce: Has("a.04"),
                longerChain: Has("e.01"),
                isBoss: isBoss,
                isElite: isElite);
        }
    }

    [DataContract]
    public sealed class ExpandedLightningEventState
    {
        [DataMember(IsRequired = true)] public long Id { get; set; }
        [DataMember(IsRequired = true)] public long Cycle { get; set; }
        [DataMember(IsRequired = true)] public double At { get; set; }
        [DataMember(IsRequired = true)] public float X { get; set; }
        [DataMember(IsRequired = true)] public float Y { get; set; }
        [DataMember(IsRequired = true)] public double Damage { get; set; }
        [DataMember(IsRequired = true)] public double Radius { get; set; }
        [DataMember(IsRequired = true)] public int AssignedTargetId { get; set; }
        [DataMember(IsRequired = true)] public bool Aftershock { get; set; }
        [DataMember(EmitDefaultValue = false)] public List<int>? VisitedIds { get; set; }
        [DataMember(EmitDefaultValue = false)] public bool CanRetarget { get; set; }
        [DataMember(EmitDefaultValue = false)] public bool LongerChain { get; set; }

        internal Vector2 Position => new Vector2(X, Y);

        internal ExpandedLightningEventState Copy()
        {
            var copy = (ExpandedLightningEventState)MemberwiseClone();
            copy.VisitedIds = VisitedIds == null ? null : new List<int>(VisitedIds);
            return copy;
        }
    }

    [DataContract]
    public sealed class ExpandedLightningState
    {
        [DataMember(IsRequired = true)] public bool Evolved { get; set; }
        [DataMember(IsRequired = true)] public bool WaitingForTarget { get; set; }
        [DataMember(IsRequired = true)] public long Cycle { get; set; }
        [DataMember(IsRequired = true)] public long NextStrikeId { get; set; } = 1;
        [DataMember(IsRequired = true)] public double NextCycle { get; set; }
        [DataMember(IsRequired = true)] public List<ExpandedLightningEventState> Pending { get; set; } =
            new List<ExpandedLightningEventState>();

        public ExpandedLightningState Copy() => new ExpandedLightningState
        {
            Evolved = Evolved,
            WaitingForTarget = WaitingForTarget,
            Cycle = Cycle,
            NextStrikeId = NextStrikeId,
            NextCycle = NextCycle,
            Pending = Pending.Select(attack => attack.Copy()).ToList(),
        };

        // Call at a form change or when the encounter cancels held attacks.
        // The surrounding world owns the next executable tick and the round's lifetime.
        public void ResetForForm(bool evolved, double now)
        {
            if (!ExpandedLightning.Finite(now) || now < 0) throw new ArgumentOutOfRangeException(nameof(now));
            Evolved = evolved;
            WaitingForTarget = false;
            NextCycle = now;
            Pending.Clear();
        }

        public void Validate(double now, bool evolved)
        {
            if (!ExpandedLightning.Finite(now) || now < 0 || evolved != Evolved ||
                NextStrikeId < 1 || Cycle < 0 || Cycle >= NextStrikeId ||
                (Cycle == 0 ? NextStrikeId != 1 : (NextStrikeId - 2) / ExpandedLightning.MaximumAmount >= Cycle) ||
                !ExpandedLightning.Finite(NextCycle) || NextCycle < 0 ||
                NextCycle > now + ExpandedLightning.MaximumCycle + ExpandedLightning.TimeEpsilon ||
                (WaitingForTarget && NextCycle > now + ExpandedLightning.TimeEpsilon) ||
                Pending == null || Pending.Count > ExpandedLightning.MaximumAmount + ExpandedLightning.MaximumPendingAftershocks)
                throw new ArgumentException("Invalid expanded lightning clock");

            var ids = new HashSet<long>();
            var shockCycles = new HashSet<long>();
            var strikeCount = 0;
            double? strikeTime = null;
            foreach (var attack in Pending)
            {
                if (attack == null || attack.Id < 1 || attack.Id >= NextStrikeId || !ids.Add(attack.Id) ||
                    attack.Cycle < 1 || attack.Cycle > Cycle || attack.AssignedTargetId < 1 || attack.LongerChain && !Evolved ||
                    !ExpandedLightning.ValidPosition(attack.Position) ||
                    !ExpandedLightning.Finite(attack.At) || attack.At < now - ExpandedLightning.TimeEpsilon ||
                    !ExpandedLightning.Finite(attack.Damage) || attack.Damage <= 0 ||
                    !ExpandedLightning.Finite(attack.Radius) || attack.Radius <= 0)
                    throw new ArgumentException("Invalid expanded lightning event");

                if (attack.Aftershock)
                {
                    if (!Evolved || attack.CanRetarget || !shockCycles.Add(attack.Cycle) ||
                        shockCycles.Count > ExpandedLightning.MaximumPendingAftershocks ||
                        attack.At > now + ExpandedLightning.AftershockDelay + ExpandedLightning.TimeEpsilon ||
                        attack.Damage < ExpandedLightning.AftershockDamageFactor ||
                        attack.Damage > ExpandedLightning.MaximumDamage * ExpandedLightning.AftershockDamageFactor ||
                        attack.Radius != ExpandedLightning.AftershockRadius ||
                        attack.VisitedIds == null || attack.VisitedIds.Count == 0 ||
                        attack.VisitedIds.Count > ExpandedLightning.ChainLimit + (attack.LongerChain ? 2 : 1) ||
                        attack.VisitedIds.Any(id => id < 1) ||
                        attack.VisitedIds.Distinct().Count() != attack.VisitedIds.Count)
                        throw new ArgumentException("Invalid expanded lightning aftershock");
                }
                else
                {
                    if (++strikeCount > ExpandedLightning.MaximumAmount || attack.Cycle != Cycle ||
                        attack.At > now + ExpandedLightning.Profile.FirstAttackDelay + ExpandedLightning.TimeEpsilon ||
                        attack.At > NextCycle + ExpandedLightning.TimeEpsilon ||
                        (strikeTime.HasValue && attack.At != strikeTime.Value) ||
                        attack.Damage < 1 || attack.Damage > ExpandedLightning.MaximumDamage ||
                        attack.Radius > ExpandedLightning.MaximumStrikeRadius || attack.VisitedIds != null)
                        throw new ArgumentException("Invalid expanded lightning strike");
                    strikeTime = attack.At;
                }
            }
            if (strikeTime.HasValue && (WaitingForTarget || shockCycles.Contains(Cycle)))
                throw new ArgumentException("Conflicting expanded lightning events");
        }
    }

    public static class ExpandedLightning
    {
        internal static readonly ExpandedWeaponProfile Profile = ExpandedWeaponRules.Get("lightning");
        internal static readonly int MaximumAmount = Profile.MaximumOriginalAmount;
        internal static readonly double MaximumCycle = Profile.AtLevel(1).Cycle;
        internal static readonly double MaximumDamage = Profile.AtLevel(8).Damage * 2.25;
        internal static readonly double MaximumStrikeRadius = Profile.AtLevel(1).Radius * 2.5;
        internal const double TimeEpsilon = 1e-9;
        internal const double MinimumCycle = .15;
        private const double MinimumDelay = .05;
        private const double CrowdRadius = 1.5;
        internal const int ChainLimit = 3;
        internal const double AftershockDelay = .5;
        internal const double AftershockRadius = 1.4;
        internal const double AftershockDamageFactor = .4;
        internal const int MaximumPendingAftershocks = 4;
        private const double ChainRadius = 3;
        private const double ChainDamageFactor = .7;

        // This method runs once per fixed combat step. An accepted hit may mutate the
        // live target map. The callback returns true for actual damage, including kills.
        public static void Step(ExpandedLightningState state, double now, Vector2 playerPosition, int level,
            bool evolved, double damage, double cycle, int amount, double radius,
            Func<IReadOnlyDictionary<int, Vector2>> targets, Func<ExpandedLightningHit, bool> applyHit,
            ExpandedLightningSettings settings = default)
        {
            if (state == null) throw new ArgumentNullException(nameof(state));
            if (targets == null) throw new ArgumentNullException(nameof(targets));
            if (applyHit == null) throw new ArgumentNullException(nameof(applyHit));
            if (!Finite(now) || now < 0 || !ValidPosition(playerPosition) ||
                level < 1 || level > 8 || (evolved && level != 8) ||
                !Finite(damage) || damage < 1 || damage > MaximumDamage ||
                !Finite(cycle) || cycle < MinimumCycle || cycle > MaximumCycle ||
                amount < 1 || amount > MaximumAmount || !Finite(radius) || radius <= 0 || radius > MaximumStrikeRadius)
                throw new ArgumentException("Invalid expanded lightning parameters");

            if (state.Evolved != evolved) state.ResetForForm(evolved, now);
            ResolveDue(state, now, playerPosition, targets, applyHit, settings);
            if (state.NextCycle > now + TimeEpsilon) return;

            var candidates = OrderedTargets(targets(), playerPosition, settings);
            if (candidates.Length == 0)
            {
                state.WaitingForTarget = true;
                return;
            }

            // A cycle waiting for a target starts on the first eligible tick.
            // Other cycles keep their exact previous schedule across tick rounding.
            var start = state.WaitingForTarget ? now : state.NextCycle;
            state.WaitingForTarget = false;
            state.Cycle = checked(state.Cycle + 1);
            state.NextCycle = start + cycle;
            var delay = Math.Max(MinimumDelay, Profile.FirstAttackDelay - settings.DelayAdvance);
            var strikeRadius = Math.Min(MaximumStrikeRadius, radius + Profile.AtLevel(1).Radius * settings.RadiusBonus);
            var previous = 0;
            for (var index = 0; index < amount; index++)
            {
                var target = index >= candidates.Length && settings.DeferPreviousTargetOnReuse
                    ? PreferredReuse(candidates, previous) : candidates[index % candidates.Length];
                state.Pending.Add(new ExpandedLightningEventState
                {
                    Id = state.NextStrikeId,
                    Cycle = state.Cycle,
                    At = start + delay,
                    X = target.Value.X,
                    Y = target.Value.Y,
                    Damage = damage,
                    Radius = strikeRadius,
                    AssignedTargetId = target.Key,
                    CanRetarget = settings.RetargetOnce,
                    LongerChain = evolved && settings.LongerChain,
                });
                previous = target.Key;
                state.NextStrikeId = checked(state.NextStrikeId + 1);
            }
        }

        private static KeyValuePair<int, Vector2>[] OrderedTargets(IReadOnlyDictionary<int, Vector2> targets,
            Vector2 playerPosition, ExpandedLightningSettings settings)
        {
            return targets.Where(target => InRange(target.Value, playerPosition, Profile.TargetSearchRadius))
                .OrderByDescending(target => settings.PrioritizeThreats && settings.IsBoss!(target.Key))
                .ThenByDescending(target => settings.PrioritizeThreats && settings.IsElite!(target.Key))
                .ThenByDescending(target => settings.PrioritizeCrowds ?
                    targets.Values.Count(other => InRange(other, target.Value, CrowdRadius)) : 0)
                .ThenBy(target => DistanceSquared(target.Value, playerPosition))
                .ThenBy(target => target.Key).ToArray();
        }

        private static KeyValuePair<int, Vector2> PreferredReuse(KeyValuePair<int, Vector2>[] candidates, int previous)
        {
            var alternative = candidates.FirstOrDefault(candidate => candidate.Key != previous);
            return alternative.Key != 0 ? alternative : candidates[0];
        }

        private static void TryRetarget(ExpandedLightningState state, ExpandedLightningEventState attack,
            Vector2 playerPosition, IReadOnlyDictionary<int, Vector2> targets, ExpandedLightningSettings settings)
        {
            if (!attack.CanRetarget || targets.ContainsKey(attack.AssignedTargetId)) return;
            attack.CanRetarget = false;
            var candidates = OrderedTargets(targets, playerPosition, settings);
            if (candidates.Length == 0) return;

            var replacement = candidates[0];
            if (settings.DeferPreviousTargetOnReuse)
            {
                var others = state.Pending.Where(pending => !pending.Aftershock &&
                    pending.Cycle == attack.Cycle && pending.Id != attack.Id).ToArray();
                var fresh = candidates.FirstOrDefault(candidate => !others.Any(pending => pending.AssignedTargetId == candidate.Key));
                var previous = others.LastOrDefault(pending => pending.Id < attack.Id)?.AssignedTargetId ?? 0;
                replacement = fresh.Key != 0 ? fresh : PreferredReuse(candidates, previous);
            }
            attack.AssignedTargetId = replacement.Key;
            attack.X = replacement.Value.X;
            attack.Y = replacement.Value.Y;
        }

        private static void ResolveDue(ExpandedLightningState state, double now, Vector2 playerPosition,
            Func<IReadOnlyDictionary<int, Vector2>> targets, Func<ExpandedLightningHit, bool> applyHit,
            ExpandedLightningSettings settings)
        {
            if (state.Pending.Count == 0) return;
            // Retarget on the first fixed step that observes the assigned actor's death,
            // so the replacement telegraph remains visible until the original due time.
            foreach (var attack in state.Pending)
                if (attack.CanRetarget && attack.At > now + TimeEpsilon)
                    TryRetarget(state, attack, playerPosition, targets(), settings);

            var due = state.Pending.Where(attack => attack.At <= now + TimeEpsilon)
                .OrderBy(attack => attack.At).ThenBy(attack => attack.Id).ToArray();
            if (due.Length == 0) return;
            var shockCycles = new HashSet<long>();
            foreach (var attack in due)
            {
                if (attack.CanRetarget) TryRetarget(state, attack, playerPosition, targets(), settings);
                state.Pending.Remove(attack);
                var kind = attack.Aftershock ? ExpandedLightningHitKind.Aftershock : ExpandedLightningHitKind.Strike;
                var nearest = HitArea(attack, kind, targets, applyHit);
                if (!state.Evolved || attack.Aftershock || !nearest.HasValue) continue;

                var visited = Chain(attack, nearest.Value, targets, applyHit);
                // A cycle schedules one thunderwave, at the first landing that actually hit.
                if (!shockCycles.Add(attack.Cycle)) continue;
                var aftershock = attack.Copy();
                aftershock.At += AftershockDelay;
                aftershock.Aftershock = true;
                aftershock.CanRetarget = false;
                aftershock.Radius = AftershockRadius;
                aftershock.Damage *= AftershockDamageFactor;
                aftershock.VisitedIds = visited;
                state.Pending.Add(aftershock);
            }
        }

        private static KeyValuePair<int, Vector2>? HitArea(ExpandedLightningEventState attack,
            ExpandedLightningHitKind kind, Func<IReadOnlyDictionary<int, Vector2>> targets,
            Func<ExpandedLightningHit, bool> applyHit)
        {
            var ids = targets().Where(target => InRange(target.Value, attack.Position, attack.Radius))
                .Select(target => target.Key).OrderBy(id => id).ToArray();
            KeyValuePair<int, Vector2>? nearest = null;
            var nearestDistance = double.PositiveInfinity;
            foreach (var id in ids)
            {
                if (!targets().TryGetValue(id, out var position) || !InRange(position, attack.Position, attack.Radius) ||
                    !applyHit(new ExpandedLightningHit(kind, id, position, attack.Damage, attack.Id, attack.Cycle)))
                    continue;

                var distance = DistanceSquared(position, attack.Position);
                if (distance >= nearestDistance) continue;
                nearestDistance = distance;
                nearest = new KeyValuePair<int, Vector2>(id, position);
            }
            return nearest;
        }

        private static List<int> Chain(ExpandedLightningEventState attack, KeyValuePair<int, Vector2> anchor,
            Func<IReadOnlyDictionary<int, Vector2>> targets, Func<ExpandedLightningHit, bool> applyHit)
        {
            var visited = new List<int> { anchor.Key };
            var attempted = new HashSet<int> { anchor.Key };
            var origin = anchor.Value;
            var limit = ChainLimit + (attack.LongerChain ? 1 : 0);
            while (visited.Count <= limit)
            {
                var next = targets().Where(target => !attempted.Contains(target.Key) && InRange(target.Value, origin, ChainRadius))
                    .OrderBy(target => DistanceSquared(target.Value, origin)).ThenBy(target => target.Key).FirstOrDefault();
                if (next.Key == 0) break;
                attempted.Add(next.Key);
                if (!applyHit(new ExpandedLightningHit(ExpandedLightningHitKind.Chain, next.Key, next.Value,
                    attack.Damage * ChainDamageFactor, attack.Id, attack.Cycle)))
                    continue;
                visited.Add(next.Key);
                origin = next.Value;
            }
            return visited;
        }

        private static bool InRange(Vector2 position, Vector2 center, double radius) =>
            DistanceSquared(position, center) <= radius * radius;

        private static double DistanceSquared(Vector2 a, Vector2 b)
        {
            var x = (double)a.X - b.X;
            var y = (double)a.Y - b.Y;
            return x * x + y * y;
        }

        internal static bool Finite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);
        internal static bool ValidPosition(Vector2 point) => Finite(point.X) && Finite(point.Y) &&
            Math.Abs(point.X) <= 1000000 && Math.Abs(point.Y) <= 1000000;
    }
}
