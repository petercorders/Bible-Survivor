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
        EliteThunder,
    }

    public readonly struct ExpandedLightningHit
    {
        public ExpandedLightningHitKind Kind { get; }
        public int TargetId { get; }
        public Vector2 Position { get; }
        public double Damage { get; }
        public long StrikeId { get; }
        public long Cycle { get; }
        public double OriginalBaseDamage { get; }
        public int TargetOrdinal { get; }

        internal ExpandedLightningHit(ExpandedLightningHitKind kind, int targetId, Vector2 position,
            double damage, long strikeId, long cycle, double originalBaseDamage = 0, int targetOrdinal = 0)
        {
            Kind = kind;
            TargetId = targetId;
            Position = position;
            Damage = damage;
            StrikeId = strikeId;
            Cycle = cycle;
            OriginalBaseDamage = originalBaseDamage;
            TargetOrdinal = targetOrdinal;
        }
    }

    public readonly struct ExpandedLightningSettings
    {
        public double RadiusBonus { get; }
        public double AftershockRadiusBonus { get; }
        public double DelayAdvance { get; }
        public bool PrioritizeCrowds { get; }
        public bool PrioritizeThreats { get; }
        public bool DeferPreviousTargetOnReuse { get; }
        public bool RetargetOnce { get; }
        public bool LongerChain { get; }
        public bool IsolatedDamage { get; }
        public bool HighHealthDamage { get; }
        public bool RepeatStrikeDamage { get; }
        public bool WiderLastStrike { get; }
        public bool EliteThunderOnFifthCycle { get; }
        internal Func<int, bool>? IsBoss { get; }
        internal Func<int, bool>? IsElite { get; }

        public ExpandedLightningSettings(double radiusBonus = 0, double delayAdvance = 0,
            bool prioritizeCrowds = false, bool prioritizeThreats = false, bool deferPreviousTargetOnReuse = false,
            bool retargetOnce = false, bool longerChain = false, Func<int, bool>? isBoss = null, Func<int, bool>? isElite = null,
            double aftershockRadiusBonus = 0, bool isolatedDamage = false, bool highHealthDamage = false,
            bool repeatStrikeDamage = false, bool widerLastStrike = false, bool eliteThunderOnFifthCycle = false)
        {
            if (!ExpandedLightning.Finite(radiusBonus) || radiusBonus < 0 ||
                !ExpandedLightning.Finite(delayAdvance) || delayAdvance < 0 ||
                !ExpandedLightning.Finite(aftershockRadiusBonus) || aftershockRadiusBonus < 0 || aftershockRadiusBonus > 1.5)
                throw new ArgumentException("Invalid expanded lightning bonuses");
            if (prioritizeThreats && (isBoss == null || isElite == null))
                throw new ArgumentException("Lightning threat priority needs boss and elite classifiers");
            if (eliteThunderOnFifthCycle && isElite == null)
                throw new ArgumentException("Lightning elite thunder needs an elite classifier");

            RadiusBonus = radiusBonus;
            AftershockRadiusBonus = aftershockRadiusBonus;
            DelayAdvance = delayAdvance;
            PrioritizeCrowds = prioritizeCrowds;
            PrioritizeThreats = prioritizeThreats;
            DeferPreviousTargetOnReuse = deferPreviousTargetOnReuse;
            RetargetOnce = retargetOnce;
            LongerChain = longerChain;
            IsolatedDamage = isolatedDamage;
            HighHealthDamage = highHealthDamage;
            RepeatStrikeDamage = repeatStrikeDamage;
            WiderLastStrike = widerLastStrike;
            EliteThunderOnFifthCycle = eliteThunderOnFifthCycle;
            IsBoss = isBoss;
            IsElite = isElite;
        }

        public static ExpandedLightningSettings FromRanks(IReadOnlyDictionary<string, int> ranks,
            Func<int, bool>? isBoss = null, Func<int, bool>? isElite = null)
        {
            if (ranks == null) throw new ArgumentNullException(nameof(ranks));
            bool HasRank(string id) => ranks.TryGetValue(id, out var rank) && rank > 0;
            bool Has(string suffix) => HasRank("weapon.lightning." + suffix);

            return new ExpandedLightningSettings(
                radiusBonus: Has("base.04") ? .10 : 0,
                delayAdvance: Has("base.05") ? .08 : 0,
                prioritizeCrowds: Has("a.01"),
                prioritizeThreats: Has("a.02"),
                deferPreviousTargetOnReuse: Has("a.03"),
                retargetOnce: Has("a.04"),
                longerChain: Has("e.01"),
                isBoss: isBoss,
                isElite: isElite,
                aftershockRadiusBonus: (HasRank("common.area.01") ? .04 : 0) + (HasRank("common.area.04") ? .05 : 0),
                isolatedDamage: Has("a.05"), highHealthDamage: Has("b.01"), repeatStrikeDamage: Has("b.03"),
                widerLastStrike: Has("b.04"), eliteThunderOnFifthCycle: Has("b.05"));
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
        [DataMember(EmitDefaultValue = false)] public double OriginalBaseDamage { get; set; }
        [DataMember(IsRequired = true)] public double Radius { get; set; }
        [DataMember(IsRequired = true)] public int AssignedTargetId { get; set; }
        [DataMember(IsRequired = true)] public bool Aftershock { get; set; }
        [DataMember(EmitDefaultValue = false)] public List<int>? VisitedIds { get; set; }
        [DataMember(EmitDefaultValue = false)] public bool CanRetarget { get; set; }
        [DataMember(EmitDefaultValue = false)] public bool LongerChain { get; set; }
        [DataMember(EmitDefaultValue = false)] public bool UsesFarthestReuse { get; set; }
        [DataMember(EmitDefaultValue = false)] public int PredecessorTargetId { get; set; }
        [DataMember(EmitDefaultValue = false)] public float? PredecessorX { get; set; }
        [DataMember(EmitDefaultValue = false)] public float? PredecessorY { get; set; }

        [DataMember(EmitDefaultValue = false)] public bool IsolatedDamage { get; set; }
        [DataMember(EmitDefaultValue = false)] public bool HighHealthDamage { get; set; }
        [DataMember(EmitDefaultValue = false)] public bool RepeatStrikeDamage { get; set; }
        [DataMember(EmitDefaultValue = false)] public bool WiderLastStrike { get; set; }
        [DataMember(EmitDefaultValue = false)] public bool EliteThunderOnHit { get; set; }
        [DataMember(EmitDefaultValue = false)] public bool EliteThunder { get; set; }

        public bool IsPrimary => !Aftershock && !EliteThunder;
        public ExpandedLightningHitKind HitKind => EliteThunder ? ExpandedLightningHitKind.EliteThunder
            : Aftershock ? ExpandedLightningHitKind.Aftershock : ExpandedLightningHitKind.Strike;

        internal Vector2 Position => new Vector2(X, Y);
        internal Vector2 PredecessorPosition => new Vector2(PredecessorX.GetValueOrDefault(), PredecessorY.GetValueOrDefault());

        internal void SetPredecessor(int targetId, Vector2 position)
        {
            PredecessorTargetId = targetId;
            PredecessorX = targetId == 0 ? (float?)null : position.X;
            PredecessorY = targetId == 0 ? (float?)null : position.Y;
        }

        internal void ClearPrimaryModifiers()
        {
            OriginalBaseDamage = 0;
            CanRetarget = false;
            UsesFarthestReuse = false;
            SetPredecessor(0, default);
            IsolatedDamage = false;
            HighHealthDamage = false;
            RepeatStrikeDamage = false;
            WiderLastStrike = false;
            EliteThunderOnHit = false;
        }

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

        [DataMember(EmitDefaultValue = false)] public List<BattleHitState>? RecentStrikeHits { get; set; }

        public ExpandedLightningState Copy() => new ExpandedLightningState
        {
            Evolved = Evolved,
            WaitingForTarget = WaitingForTarget,
            Cycle = Cycle,
            NextStrikeId = NextStrikeId,
            NextCycle = NextCycle,
            Pending = Pending.Select(attack => attack.Copy()).ToList(),
            RecentStrikeHits = RecentStrikeHits?.Select(hit => hit.Copy()).ToList(),
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
            RecentStrikeHits = null;
        }

        public void Validate(double now, bool evolved)
        {
            if (!ExpandedLightning.Finite(now) || now < 0 || evolved != Evolved ||
                NextStrikeId < 1 || Cycle < 0 || Cycle >= NextStrikeId ||
                (Cycle == 0 ? NextStrikeId != 1 : (NextStrikeId - 2) / ExpandedLightning.MaximumAmount >= Cycle) ||
                !ExpandedLightning.Finite(NextCycle) || NextCycle < 0 ||
                NextCycle > now + ExpandedLightning.MaximumCycle + ExpandedLightning.TimeEpsilon ||
                (WaitingForTarget && NextCycle > now + ExpandedLightning.TimeEpsilon) ||
                Pending == null || Pending.Count > ExpandedLightning.MaximumAmount + ExpandedLightning.MaximumPendingAftershocks + 1 ||
                RecentStrikeHits != null && (RecentStrikeHits.Count > 120 ||
                    RecentStrikeHits.Any(hit => hit == null || hit.Id <= 0 ||
                        !ExpandedLightning.Finite(hit.At) || hit.At < 0 || hit.At > now + ExpandedLightning.TimeEpsilon) ||
                    RecentStrikeHits.Select(hit => hit.Id).Distinct().Count() != RecentStrikeHits.Count))
                throw new ArgumentException("Invalid expanded lightning clock");

            var ids = new HashSet<(long Id, bool EliteThunder)>();
            var shockCycles = new HashSet<long>();
            var thunderCycles = new HashSet<long>();
            var strikeCount = 0;
            double? strikeTime = null;
            foreach (var attack in Pending)
            {
                if (attack == null || attack.Id < 1 || attack.Id >= NextStrikeId || !ids.Add((attack.Id, attack.EliteThunder)) ||
                    attack.Cycle < 1 || attack.Cycle > Cycle || attack.AssignedTargetId < 1 || attack.LongerChain && !Evolved ||
                    !ExpandedLightning.ValidPosition(attack.Position) ||
                    !ExpandedLightning.Finite(attack.At) || attack.At < now - ExpandedLightning.TimeEpsilon ||
                    !ExpandedLightning.Finite(attack.Damage) || attack.Damage <= 0 ||
                    !ExpandedLightning.Finite(attack.Radius) || attack.Radius <= 0 ||
                    !ValidPredecessor(attack) ||
                    !ExpandedLightning.ValidOriginalBaseDamage(attack.OriginalBaseDamage, evolved, !attack.IsPrimary) ||
                    !ValidPrimaryModifiers(attack))
                    throw new ArgumentException("Invalid expanded lightning event");

                if (attack.EliteThunder)
                {
                    if (attack.Aftershock || attack.CanRetarget || attack.LongerChain || !thunderCycles.Add(attack.Cycle) ||
                        thunderCycles.Count > 1 || attack.Cycle % 5 != 0 ||
                        attack.At > now + ExpandedLightning.AftershockDelay + ExpandedLightning.TimeEpsilon ||
                        attack.Damage < ExpandedLightning.EliteThunderDamageFactor ||
                        attack.Damage > ExpandedLightning.MaximumDamage * ExpandedLightning.EliteThunderDamageFactor ||
                        attack.Radius < ExpandedLightning.EliteThunderRadius || attack.Radius > ExpandedLightning.EliteThunderRadius * 2.5 ||
                        attack.VisitedIds != null)
                        throw new ArgumentException("Invalid expanded lightning elite thunder");
                }
                else if (attack.Aftershock)
                {
                    if (!Evolved || attack.CanRetarget || !shockCycles.Add(attack.Cycle) ||
                        shockCycles.Count > ExpandedLightning.MaximumPendingAftershocks ||
                        attack.At > now + ExpandedLightning.AftershockDelay + ExpandedLightning.TimeEpsilon ||
                        attack.Damage < ExpandedLightning.AftershockDamageFactor ||
                        attack.Damage > ExpandedLightning.MaximumDamage * ExpandedLightning.AftershockDamageFactor ||
                        attack.Radius < ExpandedLightning.AftershockRadius || attack.Radius > ExpandedLightning.AftershockRadius * 2.5 ||
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
                        attack.Radius > ExpandedLightning.MaximumStrikeRadius || attack.VisitedIds != null ||
                        attack.WiderLastStrike && attack.Id != NextStrikeId - 1 ||
                        attack.EliteThunderOnHit && attack.Cycle % 5 != 0)
                        throw new ArgumentException("Invalid expanded lightning strike");
                    strikeTime = attack.At;
                }
            }
            if (strikeTime.HasValue && (WaitingForTarget || shockCycles.Contains(Cycle) || thunderCycles.Contains(Cycle)))
                throw new ArgumentException("Conflicting expanded lightning events");

            foreach (var attack in Pending.Where(attack => attack.UsesFarthestReuse))
            {
                var previous = Pending.Find(candidate => candidate.IsPrimary && candidate.Cycle == attack.Cycle &&
                    candidate.Id == attack.Id - 1);
                if (previous != null && (attack.PredecessorTargetId != previous.AssignedTargetId ||
                    attack.PredecessorPosition != previous.Position))
                    throw new ArgumentException("Invalid expanded lightning predecessor");
            }
        }

        private static bool ValidPrimaryModifiers(ExpandedLightningEventState attack)
        {
            if (attack.IsolatedDamage || attack.HighHealthDamage || attack.RepeatStrikeDamage)
                return attack.IsPrimary && attack.OriginalBaseDamage > 0;
            return attack.IsPrimary || (!attack.WiderLastStrike && !attack.EliteThunderOnHit);
        }

        private static bool ValidPredecessor(ExpandedLightningEventState attack)
        {
            if (attack.PredecessorTargetId == 0)
                return !attack.PredecessorX.HasValue && !attack.PredecessorY.HasValue &&
                    (!attack.UsesFarthestReuse || attack.IsPrimary);
            return attack.IsPrimary && attack.UsesFarthestReuse && attack.PredecessorTargetId > 0 &&
                attack.PredecessorX.HasValue && attack.PredecessorY.HasValue &&
                ExpandedLightning.ValidPosition(attack.PredecessorPosition);
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
        internal const double EliteThunderRadius = .8;
        internal const double EliteThunderDamageFactor = .35;
        private const double RepeatStrikeWindow = 1.5;
        private const double IsolationRadius = 2;
        private const double WiderLastStrikeBonus = .14;
        internal const int MaximumPendingAftershocks = 4;
        private const double ChainRadius = 3;
        private const double ChainDamageFactor = .7;

        // This method runs once per fixed combat step. An accepted hit may mutate the
        // live target map. The callback returns true for actual damage, including kills.
        public static void Step(ExpandedLightningState state, double now, Vector2 playerPosition, int level,
            bool evolved, double damage, double cycle, int amount, double radius,
            Func<IReadOnlyDictionary<int, Vector2>> targets, Func<ExpandedLightningHit, bool> applyHit,
            ExpandedLightningSettings settings = default, Func<double>? nextCycleDuration = null,
            Func<int, double>? targetHealthRatio = null)
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
            if (state.RecentStrikeHits != null)
            {
                var live = targets();
                state.RecentStrikeHits.RemoveAll(hit => now - hit.At > RepeatStrikeWindow + TimeEpsilon ||
                    !live.ContainsKey(hit.Id));
                if (state.RecentStrikeHits.Count == 0) state.RecentStrikeHits = null;
            }
            ResolveDue(state, now, playerPosition, targets, applyHit, settings, targetHealthRatio);
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
            state.NextCycle = start + (nextCycleDuration?.Invoke() ?? cycle);
            var delay = Math.Max(MinimumDelay, Profile.FirstAttackDelay - settings.DelayAdvance);
            var radiusBonus = Profile.AtLevel(1).Radius * settings.RadiusBonus;
            var previous = default(KeyValuePair<int, Vector2>);
            for (var index = 0; index < amount; index++)
            {
                var target = index >= candidates.Length && settings.DeferPreviousTargetOnReuse
                    ? FarthestReuse(candidates, previous.Key, previous.Value) : candidates[index % candidates.Length];
                var wider = settings.WiderLastStrike && index == amount - 1;
                var strikeRadius = Math.Min(MaximumStrikeRadius, radius + radiusBonus +
                    (wider ? Profile.AtLevel(1).Radius * WiderLastStrikeBonus : 0));
                var attack = new ExpandedLightningEventState
                {
                    Id = state.NextStrikeId,
                    Cycle = state.Cycle,
                    At = start + delay,
                    X = target.Value.X,
                    Y = target.Value.Y,
                    Damage = damage,
                    OriginalBaseDamage = Profile.AtLevel(level).Damage,
                    Radius = strikeRadius,
                    AssignedTargetId = target.Key,
                    CanRetarget = settings.RetargetOnce,
                    LongerChain = evolved && settings.LongerChain,
                    UsesFarthestReuse = settings.DeferPreviousTargetOnReuse,
                    IsolatedDamage = settings.IsolatedDamage,
                    HighHealthDamage = settings.HighHealthDamage,
                    RepeatStrikeDamage = settings.RepeatStrikeDamage,
                    WiderLastStrike = wider,
                    EliteThunderOnHit = settings.EliteThunderOnFifthCycle && state.Cycle % 5 == 0,
                };
                if (attack.UsesFarthestReuse) attack.SetPredecessor(previous.Key, previous.Value);
                state.Pending.Add(attack);
                previous = target;
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

        private static KeyValuePair<int, Vector2> FarthestReuse(KeyValuePair<int, Vector2>[] candidates,
            int predecessorId, Vector2 predecessorPosition)
        {
            if (predecessorId == 0 || candidates.Length < 2) return candidates[0];

            var best = candidates[0];
            var greatestDistance = -1d;
            foreach (var candidate in candidates)
            {
                if (candidate.Key == predecessorId) continue;
                var distance = DistanceSquared(candidate.Value, predecessorPosition);
                if (distance <= greatestDistance) continue;
                greatestDistance = distance;
                best = candidate;
            }
            return best;
        }

        private static void TryRetarget(ExpandedLightningState state, ExpandedLightningEventState attack,
            Vector2 playerPosition, IReadOnlyDictionary<int, Vector2> targets, ExpandedLightningSettings settings,
            HashSet<(long Cycle, int Id)>? spentAssignments = null)
        {
            if (!attack.CanRetarget || targets.ContainsKey(attack.AssignedTargetId)) return;
            attack.CanRetarget = false;
            var candidates = OrderedTargets(targets, playerPosition, settings);
            if (candidates.Length == 0) return;

            var replacement = candidates[0];
            if (attack.UsesFarthestReuse)
            {
                var others = state.Pending.Where(pending => pending.IsPrimary &&
                    pending.Cycle == attack.Cycle && pending.Id != attack.Id).ToArray();
                var fresh = candidates.FirstOrDefault(candidate =>
                    spentAssignments?.Contains((attack.Cycle, candidate.Key)) != true &&
                    !others.Any(pending => pending.AssignedTargetId == candidate.Key));
                var predecessor = targets.TryGetValue(attack.PredecessorTargetId, out var livePredecessor)
                    ? livePredecessor : attack.PredecessorPosition;
                replacement = fresh.Key != 0 ? fresh : FarthestReuse(candidates, attack.PredecessorTargetId, predecessor);
            }
            attack.AssignedTargetId = replacement.Key;
            attack.X = replacement.Value.X;
            attack.Y = replacement.Value.Y;
            var successor = state.Pending.Find(next => next.IsPrimary && next.UsesFarthestReuse &&
                next.Cycle == attack.Cycle && next.Id == attack.Id + 1);
            successor?.SetPredecessor(replacement.Key, replacement.Value);
        }

        private static void ResolveDue(ExpandedLightningState state, double now, Vector2 playerPosition,
            Func<IReadOnlyDictionary<int, Vector2>> targets, Func<ExpandedLightningHit, bool> applyHit,
            ExpandedLightningSettings settings, Func<int, double>? targetHealthRatio)
        {
            if (state.Pending.Count == 0) return;
            // Retarget on the first fixed step that observes the assigned actor's death,
            // so the replacement telegraph remains visible until the original due time.
            foreach (var attack in state.Pending.Where(attack => attack.CanRetarget && attack.At > now + TimeEpsilon)
                .OrderBy(attack => attack.Id))
                TryRetarget(state, attack, playerPosition, targets(), settings);

            var due = state.Pending.Where(attack => attack.At <= now + TimeEpsilon)
                .OrderBy(attack => attack.At).ThenBy(attack => attack.Id).ThenBy(attack => attack.EliteThunder).ToArray();
            if (due.Length == 0) return;
            var shockCycles = new HashSet<long>();
            var thunderCycles = new HashSet<long>();
            var spentAssignments = new HashSet<(long Cycle, int Id)>();
            foreach (var attack in due)
            {
                if (attack.CanRetarget) TryRetarget(state, attack, playerPosition, targets(), settings, spentAssignments);
                if (attack.UsesFarthestReuse) spentAssignments.Add((attack.Cycle, attack.AssignedTargetId));
                state.Pending.Remove(attack);
                var (nearest, firstElite) = HitArea(state, attack, now, targets, applyHit,
                    attack.EliteThunderOnHit && !thunderCycles.Contains(attack.Cycle) ? settings.IsElite : null,
                    targetHealthRatio);
                if (firstElite is { } elite && thunderCycles.Add(attack.Cycle))
                    AddEliteThunder(state, attack, elite, settings.AftershockRadiusBonus);
                if (!state.Evolved || !attack.IsPrimary || !nearest.HasValue) continue;

                var visited = Chain(attack, nearest.Value, targets, applyHit);
                // A cycle schedules one thunderwave, at the first landing that actually hit.
                if (!shockCycles.Add(attack.Cycle)) continue;
                var aftershock = attack.Copy();
                aftershock.At += AftershockDelay;
                aftershock.Aftershock = true;
                aftershock.ClearPrimaryModifiers();
                aftershock.Radius = AftershockRadius * (1 + settings.AftershockRadiusBonus);
                aftershock.Damage *= AftershockDamageFactor;
                aftershock.VisitedIds = visited;
                state.Pending.Add(aftershock);
            }
        }

        private static void AddEliteThunder(ExpandedLightningState state, ExpandedLightningEventState attack,
            (int TargetId, Vector2 Position, double Damage) elite, double areaBonus)
        {
            var thunder = attack.Copy();
            thunder.At += AftershockDelay;
            thunder.EliteThunder = true;
            thunder.LongerChain = false;
            thunder.ClearPrimaryModifiers();
            thunder.AssignedTargetId = elite.TargetId;
            thunder.X = elite.Position.X;
            thunder.Y = elite.Position.Y;
            thunder.Radius = EliteThunderRadius * (1 + areaBonus);
            thunder.Damage = elite.Damage * EliteThunderDamageFactor;
            state.Pending.Add(thunder);
        }

        private static (KeyValuePair<int, Vector2>? Nearest, (int TargetId, Vector2 Position, double Damage)? Elite) HitArea(
            ExpandedLightningState state, ExpandedLightningEventState attack, double now,
            Func<IReadOnlyDictionary<int, Vector2>> targets, Func<ExpandedLightningHit, bool> applyHit,
            Func<int, bool>? isElite, Func<int, double>? targetHealthRatio)
        {
            var ids = targets().Where(target => InRange(target.Value, attack.Position, attack.Radius))
                .Select(target => target.Key).OrderBy(id => id).ToArray();
            KeyValuePair<int, Vector2>? nearest = null;
            var nearestDistance = double.PositiveInfinity;
            (int TargetId, Vector2 Position, double Damage)? firstElite = null;
            var kind = attack.HitKind;
            var primary = attack.IsPrimary;
            var acceptedOriginalTargets = 0;
            foreach (var id in ids)
            {
                var live = targets();
                if (!live.TryGetValue(id, out var position) || !InRange(position, attack.Position, attack.Radius)) continue;
                var eligibleElite = !firstElite.HasValue && isElite?.Invoke(id) == true;
                var damage = StrikeDamage(state, attack, id, now, live, targetHealthRatio);
                if (!applyHit(new ExpandedLightningHit(kind, id, position, damage, attack.Id, attack.Cycle,
                        primary ? attack.OriginalBaseDamage : 0, primary ? acceptedOriginalTargets + 1 : 0)))
                    continue;

                if (eligibleElite) firstElite = (id, position, damage);
                if (primary)
                {
                    acceptedOriginalTargets++;
                    if (attack.RepeatStrikeDamage) RecordStrikeHit(state, id, now, targets().ContainsKey(id));
                }

                var distance = DistanceSquared(position, attack.Position);
                if (distance >= nearestDistance) continue;
                nearestDistance = distance;
                nearest = new KeyValuePair<int, Vector2>(id, position);
            }
            return (nearest, firstElite);
        }

        private static double StrikeDamage(ExpandedLightningState state, ExpandedLightningEventState attack, int id,
            double now, IReadOnlyDictionary<int, Vector2> targets, Func<int, double>? targetHealthRatio)
        {
            var bonus = 0d;
            if (attack.IsolatedDamage && !targets.Any(target => target.Key != id &&
                InRange(target.Value, attack.Position, IsolationRadius))) bonus += .10;
            if (attack.HighHealthDamage && targetHealthRatio?.Invoke(id) >= .75) bonus += .06;
            if (attack.RepeatStrikeDamage &&
                state.RecentStrikeHits?.Any(hit => hit.Id == id && now - hit.At <= RepeatStrikeWindow + TimeEpsilon) == true)
                bonus += .07;
            return bonus == 0 ? attack.Damage :
                Math.Min(2.25 * attack.OriginalBaseDamage, attack.Damage + attack.OriginalBaseDamage * bonus);
        }

        private static void RecordStrikeHit(ExpandedLightningState state, int id, double now, bool stillAlive)
        {
            if (!stillAlive)
            {
                state.RecentStrikeHits?.RemoveAll(hit => hit.Id == id);
                if (state.RecentStrikeHits?.Count == 0) state.RecentStrikeHits = null;
                return;
            }
            state.RecentStrikeHits ??= new List<BattleHitState>();
            var previous = state.RecentStrikeHits.Find(hit => hit.Id == id);
            if (previous != null) previous.At = now;
            else state.RecentStrikeHits.Add(new BattleHitState { Id = id, At = now });
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

        internal static bool ValidOriginalBaseDamage(double damage, bool evolved, bool aftershock) => damage == 0 ||
            !aftershock && (damage == Profile.AtLevel(8).Damage || !evolved &&
                (damage == Profile.AtLevel(1).Damage || damage == Profile.AtLevel(4).Damage));

        internal static bool Finite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);
        internal static bool ValidPosition(Vector2 point) => Finite(point.X) && Finite(point.Y) &&
            Math.Abs(point.X) <= 1000000 && Math.Abs(point.Y) <= 1000000;
    }
}
