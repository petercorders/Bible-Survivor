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
        Current,
        CurrentShock,
        GroundCurrent,
        GroundLink,
        GroundBurst,
        EvolutionFork,
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
        internal byte CurrentRanks { get; }
        internal byte GroundRanks { get; }
        internal byte EvolutionRanks { get; }
        internal double GroundDurationBonus { get; }
        internal double EvolvedGroundDurationBonus { get; }
        internal Func<int, bool>? IsBoss { get; }
        internal Func<int, bool>? IsElite { get; }

        public ExpandedLightningSettings(double radiusBonus = 0, double delayAdvance = 0,
            bool prioritizeCrowds = false, bool prioritizeThreats = false, bool deferPreviousTargetOnReuse = false,
            bool retargetOnce = false, bool longerChain = false, Func<int, bool>? isBoss = null, Func<int, bool>? isElite = null,
            double aftershockRadiusBonus = 0, bool isolatedDamage = false, bool highHealthDamage = false,
            bool repeatStrikeDamage = false, bool widerLastStrike = false, bool eliteThunderOnFifthCycle = false,
            bool smallCurrent = false, bool fartherCurrent = false, bool strongerCurrent = false,
            bool markOnCurrent = false, bool fallbackShock = false, bool createsGroundCurrent = false,
            bool widerGroundCurrent = false, bool longerGroundCurrent = false, bool linksGroundCurrents = false,
            bool eighthStrikeGroundBurst = false, double groundDurationBonus = 0, double evolvedGroundDurationBonus = 0,
            bool evolutionFork = false, bool widerEvolutionAftershock = false, bool eliteChainMark = false,
            bool fifthEvolutionStorm = false)
        {
            if (!ExpandedLightning.Finite(radiusBonus) || radiusBonus < 0 ||
                !ExpandedLightning.Finite(delayAdvance) || delayAdvance < 0 ||
                !ExpandedLightning.Finite(aftershockRadiusBonus) || aftershockRadiusBonus < 0 || aftershockRadiusBonus > 1.5 ||
                !ExpandedLightning.Finite(groundDurationBonus) || groundDurationBonus < 0 ||
                !ExpandedLightning.Finite(evolvedGroundDurationBonus) || evolvedGroundDurationBonus < 0)
                throw new ArgumentException("Invalid expanded lightning bonuses");
            if (prioritizeThreats && (isBoss == null || isElite == null))
                throw new ArgumentException("Lightning threat priority needs boss and elite classifiers");
            if ((eliteThunderOnFifthCycle || eliteChainMark) && isElite == null)
                throw new ArgumentException("Lightning elite effects need an elite classifier");
            if (fifthEvolutionStorm && isBoss == null)
                throw new ArgumentException("Lightning evolution storm needs a boss classifier");

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
            CurrentRanks = smallCurrent ? (byte)(ExpandedLightning.SmallCurrent |
                (fartherCurrent ? ExpandedLightning.FartherCurrent : 0) |
                (strongerCurrent ? ExpandedLightning.StrongerCurrent : 0) |
                (markOnCurrent ? ExpandedLightning.MarkOnCurrent : 0) |
                (fallbackShock ? ExpandedLightning.FallbackShock : 0)) : (byte)0;
            GroundRanks = createsGroundCurrent ? (byte)(ExpandedLightning.CreatesGroundCurrent |
                (widerGroundCurrent ? ExpandedLightning.WiderGroundCurrent : 0) |
                (longerGroundCurrent ? ExpandedLightning.LongerGroundCurrent : 0) |
                (linksGroundCurrents ? ExpandedLightning.LinksGroundCurrents : 0) |
                (eighthStrikeGroundBurst ? ExpandedLightning.EighthStrikeGroundBurst : 0)) : (byte)0;
            GroundDurationBonus = groundDurationBonus;
            EvolvedGroundDurationBonus = evolvedGroundDurationBonus;
            EvolutionRanks = (byte)((evolutionFork ? ExpandedLightning.EvolutionFork : 0) |
                (widerEvolutionAftershock ? ExpandedLightning.WiderEvolutionAftershock : 0) |
                (eliteChainMark ? ExpandedLightning.EliteChainMark : 0) |
                (fifthEvolutionStorm ? ExpandedLightning.FifthEvolutionStorm : 0));
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
                aftershockRadiusBonus: (HasRank("common.area.01") ? .04 : 0) + (HasRank("common.area.04") ? .05 : 0) +
                    (HasRank("common.area.09") ? .06 : 0),
                isolatedDamage: Has("a.05"), highHealthDamage: Has("b.01"), repeatStrikeDamage: Has("b.03"),
                widerLastStrike: Has("b.04"), eliteThunderOnFifthCycle: Has("b.05"), smallCurrent: Has("c.01"),
                fartherCurrent: Has("c.02"), strongerCurrent: Has("c.03"), markOnCurrent: Has("c.04"),
                fallbackShock: Has("c.05"), createsGroundCurrent: Has("d.01"), widerGroundCurrent: Has("d.02"),
                longerGroundCurrent: Has("d.03"), linksGroundCurrents: Has("d.04"), eighthStrikeGroundBurst: Has("d.05"),
                groundDurationBonus: HasRank("common.duration.02") ? .05 : 0,
                evolvedGroundDurationBonus: HasRank("common.duration.04") ? .06 : 0,
                evolutionFork: Has("e.02"), widerEvolutionAftershock: Has("e.03"), eliteChainMark: Has("e.04"),
                fifthEvolutionStorm: Has("e.05"));
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

        [DataMember(EmitDefaultValue = false)] public byte CurrentRanks { get; set; }
        [DataMember(EmitDefaultValue = false)] public byte GroundRanks { get; set; }
        [DataMember(EmitDefaultValue = false)] public byte EvolutionRanks { get; set; }

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
            CurrentRanks = 0;
            GroundRanks = 0;
            EvolutionRanks = 0;
        }

        internal ExpandedLightningEventState Copy()
        {
            var copy = (ExpandedLightningEventState)MemberwiseClone();
            copy.VisitedIds = VisitedIds == null ? null : new List<int>(VisitedIds);
            return copy;
        }
    }

    [DataContract]
    public sealed class ExpandedLightningGroundState
    {
        [DataMember(IsRequired = true)] public long Id { get; set; }
        [DataMember(IsRequired = true)] public long Cycle { get; set; }
        [DataMember(IsRequired = true)] public float X { get; set; }
        [DataMember(IsRequired = true)] public float Y { get; set; }
        [DataMember(IsRequired = true)] public double StartedAt { get; set; }
        [DataMember(IsRequired = true)] public double EndsAt { get; set; }
        [DataMember(IsRequired = true)] public double NextPulse { get; set; }
        [DataMember(IsRequired = true)] public double Radius { get; set; }
        [DataMember(IsRequired = true)] public double Damage { get; set; }

        public Vector2 Center => new Vector2(X, Y);

        internal ExpandedLightningGroundState Copy() => (ExpandedLightningGroundState)MemberwiseClone();
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

        [DataMember(EmitDefaultValue = false)] public double? NextCurrentAt { get; set; }
        [DataMember(EmitDefaultValue = false)] public List<BattleHitState>? CurrentMarks { get; set; }

        [DataMember(EmitDefaultValue = false)] public List<ExpandedLightningGroundState>? GroundCurrents { get; set; }
        [DataMember(EmitDefaultValue = false)] public double? NextGroundLinkAt { get; set; }
        [DataMember(EmitDefaultValue = false)] public double? NextGroundBurstAt { get; set; }

        [DataMember(EmitDefaultValue = false)] public double? NextEvolutionForkAt { get; set; }
        [DataMember(EmitDefaultValue = false)] public double? NextEliteChainMarkAt { get; set; }
        [DataMember(EmitDefaultValue = false)] public double? EliteChainDamageUntil { get; set; }

        public int ActiveSupportGroups => Pending.Count(attack => !attack.IsPrimary) + (GroundCurrents?.Count ?? 0);

        public ExpandedLightningState Copy() => new ExpandedLightningState
        {
            Evolved = Evolved,
            WaitingForTarget = WaitingForTarget,
            Cycle = Cycle,
            NextStrikeId = NextStrikeId,
            NextCycle = NextCycle,
            Pending = Pending.Select(attack => attack.Copy()).ToList(),
            RecentStrikeHits = RecentStrikeHits?.Select(hit => hit.Copy()).ToList(),
            NextCurrentAt = NextCurrentAt,
            CurrentMarks = CurrentMarks?.Select(hit => hit.Copy()).ToList(),
            GroundCurrents = GroundCurrents?.Select(region => region.Copy()).ToList(),
            NextGroundLinkAt = NextGroundLinkAt,
            NextGroundBurstAt = NextGroundBurstAt,
            NextEvolutionForkAt = NextEvolutionForkAt,
            NextEliteChainMarkAt = NextEliteChainMarkAt,
            EliteChainDamageUntil = EliteChainDamageUntil,
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
            NextCurrentAt = null;
            CurrentMarks = null;
            GroundCurrents = null;
            NextGroundLinkAt = null;
            NextGroundBurstAt = null;
            NextEvolutionForkAt = null;
            NextEliteChainMarkAt = null;
            EliteChainDamageUntil = null;
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
                NextCurrentAt.HasValue && (!ExpandedLightning.Finite(NextCurrentAt.Value) || NextCurrentAt.Value < 0 ||
                    NextCurrentAt.Value > now + ExpandedLightning.CurrentCooldown + ExpandedLightning.TimeEpsilon) ||
                CurrentMarks != null && (CurrentMarks.Count == 0 || CurrentMarks.Count > 120 || !NextCurrentAt.HasValue ||
                    CurrentMarks.Any(mark => mark == null || mark.Id <= 0 || !ExpandedLightning.Finite(mark.At) ||
                        mark.At < now - ExpandedLightning.TimeEpsilon ||
                        mark.At > now + ExpandedLightning.CurrentMarkDuration + ExpandedLightning.TimeEpsilon) ||
                    CurrentMarks.Select(mark => mark.Id).Distinct().Count() != CurrentMarks.Count) ||
                RecentStrikeHits != null && (RecentStrikeHits.Count > 120 ||
                    RecentStrikeHits.Any(hit => hit == null || hit.Id <= 0 ||
                        !ExpandedLightning.Finite(hit.At) || hit.At < 0 || hit.At > now + ExpandedLightning.TimeEpsilon) ||
                    RecentStrikeHits.Select(hit => hit.Id).Distinct().Count() != RecentStrikeHits.Count))
                throw new ArgumentException("Invalid expanded lightning clock");

            ValidateGround(now);
            if (!ValidCooldown(NextEvolutionForkAt, now, ExpandedLightning.CurrentCooldown) ||
                !ValidCooldown(NextEliteChainMarkAt, now, ExpandedLightning.EliteChainCooldown) ||
                !ValidCooldown(EliteChainDamageUntil, now, ExpandedLightning.EliteChainMarkDuration) ||
                !Evolved && (NextEvolutionForkAt.HasValue || NextEliteChainMarkAt.HasValue || EliteChainDamageUntil.HasValue) ||
                EliteChainDamageUntil.HasValue && (!NextEliteChainMarkAt.HasValue ||
                    EliteChainDamageUntil.Value < now - ExpandedLightning.TimeEpsilon ||
                    Math.Abs(EliteChainDamageUntil.Value - NextEliteChainMarkAt.Value -
                        (ExpandedLightning.EliteChainMarkDuration - ExpandedLightning.EliteChainCooldown)) >
                        ExpandedLightning.TimeEpsilon))
                throw new ArgumentException("Invalid expanded lightning evolution clock");

            var ids = new HashSet<(long Id, bool EliteThunder)>();
            var shockCycles = new HashSet<long>();
            var thunderCycles = new HashSet<long>();
            var strikeCount = 0;
            double? strikeTime = null;
            var firstStrikeId = long.MaxValue;
            long fifthEvolutionStrikeId = 0;
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
                    !ValidPrimaryModifiers(attack, evolved))
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
                    firstStrikeId = Math.Min(firstStrikeId, attack.Id);
                    if ((attack.EvolutionRanks & ExpandedLightning.FifthEvolutionStorm) != 0)
                    {
                        if (fifthEvolutionStrikeId != 0 || attack.Cycle % 5 != 0)
                            throw new ArgumentException("Invalid expanded lightning evolution storm");
                        fifthEvolutionStrikeId = attack.Id;
                    }
                }
            }
            if (fifthEvolutionStrikeId != 0 && fifthEvolutionStrikeId != firstStrikeId)
                throw new ArgumentException("Invalid expanded lightning evolution storm");
            if (ActiveSupportGroups > ExpandedLightning.MaximumSupportGroups)
                throw new ArgumentException("Too many expanded lightning support groups");
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

        private void ValidateGround(double now)
        {
            if (!ValidCooldown(NextGroundLinkAt, now, ExpandedLightning.GroundSupportCooldown) ||
                !ValidCooldown(NextGroundBurstAt, now, ExpandedLightning.GroundSupportCooldown))
                throw new ArgumentException("Invalid expanded lightning ground clock");
            if (GroundCurrents == null) return;
            if (GroundCurrents.Count == 0 || GroundCurrents.Count > ExpandedLightning.MaximumSupportGroups)
                throw new ArgumentException("Invalid expanded lightning ground regions");
            var previousId = 0L;
            foreach (var region in GroundCurrents)
            {
                if (region == null || region.Id <= previousId || region.Id >= NextStrikeId ||
                    region.Cycle < 1 || region.Cycle > Cycle ||
                    !ExpandedLightning.ValidPosition(region.Center) ||
                    !ExpandedLightning.Finite(region.StartedAt) || region.StartedAt < 0 ||
                    region.StartedAt > now + ExpandedLightning.TimeEpsilon ||
                    !ExpandedLightning.Finite(region.EndsAt) || region.EndsAt <= now + ExpandedLightning.TimeEpsilon ||
                    region.EndsAt - region.StartedAt < ExpandedLightning.GroundDuration - ExpandedLightning.TimeEpsilon ||
                    region.EndsAt - region.StartedAt > ExpandedLightning.GroundDuration * 1.75 + ExpandedLightning.TimeEpsilon ||
                    !ExpandedLightning.Finite(region.Radius) || region.Radius < ExpandedLightning.GroundRadius ||
                    region.Radius > ExpandedLightning.GroundRadius * 2.5 ||
                    !ExpandedLightning.Finite(region.Damage) || region.Damage < ExpandedLightning.GroundDamageFactor ||
                    region.Damage > ExpandedLightning.MaximumDamage * ExpandedLightning.GroundDamageFactor ||
                    !ExpandedLightning.Finite(region.NextPulse) || region.NextPulse <= now + ExpandedLightning.TimeEpsilon ||
                    region.NextPulse > now + ExpandedLightning.GroundPulseInterval + ExpandedLightning.TimeEpsilon)
                    throw new ArgumentException("Invalid expanded lightning ground region");
                var pulse = (region.NextPulse - region.StartedAt) / ExpandedLightning.GroundPulseInterval;
                if (Math.Abs(pulse - Math.Round(pulse)) > ExpandedLightning.TimeEpsilon)
                    throw new ArgumentException("Invalid expanded lightning ground pulse");
                previousId = region.Id;
            }
        }

        private static bool ValidCooldown(double? at, double now, double duration) => !at.HasValue ||
            ExpandedLightning.Finite(at.Value) && at.Value >= 0 &&
                at.Value <= now + duration + ExpandedLightning.TimeEpsilon;

        private static bool ValidPrimaryModifiers(ExpandedLightningEventState attack, bool evolved)
        {
            if (attack.CurrentRanks != 0 && ((attack.CurrentRanks & ExpandedLightning.SmallCurrent) == 0 ||
                (attack.CurrentRanks & ~ExpandedLightning.AllCurrentRanks) != 0))
                return false;
            if (attack.GroundRanks != 0 && ((attack.GroundRanks & ExpandedLightning.CreatesGroundCurrent) == 0 ||
                (attack.GroundRanks & ~ExpandedLightning.AllGroundRanks) != 0))
                return false;
            if (attack.EvolutionRanks != 0 && (!evolved || (attack.EvolutionRanks & ~ExpandedLightning.AllEvolutionRanks) != 0))
                return false;
            if (attack.IsolatedDamage || attack.HighHealthDamage || attack.RepeatStrikeDamage ||
                attack.CurrentRanks != 0 || attack.GroundRanks != 0 || attack.EvolutionRanks != 0)
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
        internal const byte SmallCurrent = 1 << 0;
        internal const byte FartherCurrent = 1 << 1;
        internal const byte StrongerCurrent = 1 << 2;
        internal const byte MarkOnCurrent = 1 << 3;
        internal const byte FallbackShock = 1 << 4;
        internal const byte AllCurrentRanks = SmallCurrent | FartherCurrent | StrongerCurrent | MarkOnCurrent | FallbackShock;
        internal const double CurrentCooldown = 1;
        internal const double CurrentMarkDuration = 3;
        private const double CurrentRadius = 2.5;
        internal const byte CreatesGroundCurrent = 1 << 0;
        internal const byte WiderGroundCurrent = 1 << 1;
        internal const byte LongerGroundCurrent = 1 << 2;
        internal const byte LinksGroundCurrents = 1 << 3;
        internal const byte EighthStrikeGroundBurst = 1 << 4;
        internal const byte AllGroundRanks = CreatesGroundCurrent | WiderGroundCurrent | LongerGroundCurrent |
            LinksGroundCurrents | EighthStrikeGroundBurst;
        internal const int MaximumSupportGroups = 4;
        internal const double GroundRadius = .7;
        internal const double GroundDuration = 1.5;
        internal const double GroundPulseInterval = .5;
        internal const double GroundDamageFactor = .25;
        internal const double GroundSupportCooldown = 3;
        private const double GroundLinkDistance = 2.5;
        private const double GroundLinkWidth = .5;
        private const double GroundBurstRadius = 1.6;
        internal const byte EvolutionFork = 1 << 0;
        internal const byte WiderEvolutionAftershock = 1 << 1;
        internal const byte EliteChainMark = 1 << 2;
        internal const byte FifthEvolutionStorm = 1 << 3;
        internal const byte AllEvolutionRanks = EvolutionFork | WiderEvolutionAftershock | EliteChainMark | FifthEvolutionStorm;
        internal const double EliteChainCooldown = 4;
        internal const double EliteChainMarkDuration = 5;
        private const double EvolutionForkRadius = 2.5;

        // This method runs once per fixed combat step. An accepted hit may mutate the
        // live target map. The callback returns true for actual damage, including kills.
        public static void Step(ExpandedLightningState state, double now, Vector2 playerPosition, int level,
            bool evolved, double damage, double cycle, int amount, double radius,
            Func<IReadOnlyDictionary<int, Vector2>> targets, Func<ExpandedLightningHit, bool> applyHit,
            ExpandedLightningSettings settings = default, Func<double>? nextCycleDuration = null,
            Func<int, double>? targetHealthRatio = null,
            Func<bool>? reserveSupportRegion = null, Action? recordSupportSuppression = null,
            Func<ExpandedLightningHit, double?>? applyOriginalHit = null, double groundShapeBonus = 0,
            Action<ExpandedLightningEventState>? emitVisualLanding = null)
        {
            if (state == null) throw new ArgumentNullException(nameof(state));
            if (targets == null) throw new ArgumentNullException(nameof(targets));
            if (applyHit == null) throw new ArgumentNullException(nameof(applyHit));
            if (!Finite(now) || now < 0 || !ValidPosition(playerPosition) ||
                level < 1 || level > 8 || (evolved && level != 8) ||
                !Finite(damage) || damage < 1 || damage > MaximumDamage ||
                !Finite(cycle) || cycle < MinimumCycle || cycle > MaximumCycle ||
                amount < 1 || amount > MaximumAmount || !Finite(radius) || radius <= 0 || radius > MaximumStrikeRadius ||
                !Finite(groundShapeBonus) || groundShapeBonus < 0 || groundShapeBonus > 1.5)
                throw new ArgumentException("Invalid expanded lightning parameters");

            if (state.Evolved != evolved) state.ResetForForm(evolved, now);
            if (state.RecentStrikeHits != null)
            {
                var live = targets();
                state.RecentStrikeHits.RemoveAll(hit => now - hit.At > RepeatStrikeWindow + TimeEpsilon ||
                    !live.ContainsKey(hit.Id));
                if (state.RecentStrikeHits.Count == 0) state.RecentStrikeHits = null;
            }
            PruneCurrentMarks(state, now, targets);
            if (state.EliteChainDamageUntil < now - TimeEpsilon) state.EliteChainDamageUntil = null;
            RemoveExpiredGround(state, now);
            ResolveDue(state, now, playerPosition, targets, applyHit, settings, targetHealthRatio,
                reserveSupportRegion, recordSupportSuppression, applyOriginalHit, groundShapeBonus, emitVisualLanding);
            ResolveGroundPulses(state, now, targets, applyHit);
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
            var evolutionRanks = evolved ? settings.EvolutionRanks : (byte)0;
            var fifthStorm = (evolutionRanks & FifthEvolutionStorm) != 0 && state.Cycle % 5 == 0;
            var assignments = candidates;
            if (fifthStorm)
            {
                var first = StormTarget(candidates, targets(), playerPosition, settings);
                assignments = candidates.Where(candidate => candidate.Key != first.Key).Prepend(first).ToArray();
            }
            var previous = default(KeyValuePair<int, Vector2>);
            for (var index = 0; index < amount; index++)
            {
                var target = index >= candidates.Length && settings.DeferPreviousTargetOnReuse
                    ? FarthestReuse(candidates, previous.Key, previous.Value) : assignments[index % assignments.Length];
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
                    CurrentRanks = settings.CurrentRanks,
                    GroundRanks = settings.GroundRanks,
                    EvolutionRanks = fifthStorm && index == 0 ? evolutionRanks : (byte)(evolutionRanks & ~FifthEvolutionStorm),
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

        private static KeyValuePair<int, Vector2> StormTarget(KeyValuePair<int, Vector2>[] candidates,
            IReadOnlyDictionary<int, Vector2> targets, Vector2 playerPosition, ExpandedLightningSettings settings)
        {
            var bosses = candidates.Where(candidate => settings.IsBoss?.Invoke(candidate.Key) == true).ToArray();
            if (bosses.Length == 1) return bosses[0];
            return candidates.OrderByDescending(candidate => settings.PrioritizeThreats &&
                    settings.IsElite?.Invoke(candidate.Key) == true)
                .ThenByDescending(candidate => targets.Values.Count(other => InRange(other, candidate.Value, CrowdRadius)))
                .ThenBy(candidate => DistanceSquared(candidate.Value, playerPosition))
                .ThenBy(candidate => candidate.Key).First();
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

            if ((attack.EvolutionRanks & FifthEvolutionStorm) != 0)
            {
                var first = StormTarget(candidates, targets, playerPosition, settings);
                candidates = candidates.Where(candidate => candidate.Key != first.Key).Prepend(first).ToArray();
            }
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
            ExpandedLightningSettings settings, Func<int, double>? targetHealthRatio,
            Func<bool>? reserveSupportRegion, Action? recordSupportSuppression,
            Func<ExpandedLightningHit, double?>? applyOriginalHit, double groundShapeBonus,
            Action<ExpandedLightningEventState>? emitVisualLanding)
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
                emitVisualLanding?.Invoke(attack.Copy());
                var canProcCurrent = attack.CurrentRanks != 0 && (state.NextCurrentAt ?? 0) <= now + TimeEpsilon;
                var canProcFork = (attack.EvolutionRanks & EvolutionFork) != 0 &&
                    (state.NextEvolutionForkAt ?? 0) <= now + TimeEpsilon;
                var (nearest, firstElite, first, originalVisited) = HitArea(state, attack, now, targets, applyHit,
                    attack.EliteThunderOnHit && !thunderCycles.Contains(attack.Cycle) ? settings.IsElite : null,
                    targetHealthRatio, canProcCurrent || canProcFork, applyOriginalHit);
                if (state.Evolved && attack.IsPrimary && nearest.HasValue)
                {
                    var canMarkElite = (attack.EvolutionRanks & EliteChainMark) != 0 &&
                        (state.NextEliteChainMarkAt ?? 0) <= now + TimeEpsilon;
                    var (visited, firstTransfer, hitElite) = Chain(attack, nearest.Value, targets, applyHit,
                        canMarkElite ? settings.IsElite : null);
                    originalVisited?.UnionWith(visited);
                    if (hitElite)
                    {
                        state.NextEliteChainMarkAt = now + EliteChainCooldown;
                        state.EliteChainDamageUntil = now + EliteChainMarkDuration;
                    }
                    if (canProcFork && firstTransfer.HasValue && originalVisited != null)
                        TryEvolutionFork(state, attack, firstTransfer.Value, nearest.Value.Damage, originalVisited,
                            now, targets, applyHit);
                    // Preserve the base thunderwave's priority when only one region slot is available.
                    if (shockCycles.Add(attack.Cycle) && ReserveSupport(state, reserveSupportRegion, recordSupportSuppression))
                        AddAftershock(state, attack, visited, nearest.Value.Damage, settings.AftershockRadiusBonus);
                }
                if (firstElite is { } elite && thunderCycles.Add(attack.Cycle) &&
                    ReserveSupport(state, reserveSupportRegion, recordSupportSuppression))
                    AddEliteThunder(state, attack, elite, settings.AftershockRadiusBonus);
                if (canProcCurrent && first is { } current && originalVisited != null)
                    TryCurrent(state, attack, current, originalVisited, now, targets, applyHit);
                if (attack.GroundRanks != 0)
                    ResolveGroundImpact(state, attack, nearest?.Damage ?? attack.Damage, now, settings,
                        groundShapeBonus, targets, applyHit, reserveSupportRegion, recordSupportSuppression);
            }
            PruneCurrentMarks(state, now, targets);
        }

        private static bool ReserveSupport(ExpandedLightningState state, Func<bool>? reserve, Action? recordSuppression)
        {
            if (state.ActiveSupportGroups < MaximumSupportGroups && reserve?.Invoke() != false) return true;
            recordSuppression?.Invoke();
            return false;
        }

        private static void RemoveExpiredGround(ExpandedLightningState state, double now)
        {
            if (state.GroundCurrents == null) return;
            state.GroundCurrents.RemoveAll(region => region.EndsAt <= now + TimeEpsilon);
            if (state.GroundCurrents.Count == 0) state.GroundCurrents = null;
        }

        private static void ResolveGroundImpact(ExpandedLightningState state, ExpandedLightningEventState attack,
            double damage, double now, ExpandedLightningSettings settings, double groundShapeBonus,
            Func<IReadOnlyDictionary<int, Vector2>> targets, Func<ExpandedLightningHit, bool> applyHit,
            Func<bool>? reserveSupportRegion, Action? recordSupportSuppression)
        {
            var ranks = attack.GroundRanks;
            var areaBonus = settings.AftershockRadiusBonus + groundShapeBonus;
            if (ReserveSupport(state, reserveSupportRegion, recordSupportSuppression))
            {
                var radius = GroundRadius * Math.Min(2.5, 1 + areaBonus +
                    ((ranks & WiderGroundCurrent) != 0 ? .15 : 0));
                var duration = GroundDuration * Math.Min(1.75, 1 + settings.GroundDurationBonus +
                    (state.Evolved ? settings.EvolvedGroundDurationBonus : 0) +
                    ((ranks & LongerGroundCurrent) != 0 ? .20 : 0));
                var previous = (ranks & LinksGroundCurrents) != 0 && (state.NextGroundLinkAt ?? 0) <= now + TimeEpsilon
                    ? state.GroundCurrents?.Find(region => InRange(region.Center, attack.Position, GroundLinkDistance)) : null;
                state.GroundCurrents ??= new List<ExpandedLightningGroundState>();
                state.GroundCurrents.Add(new ExpandedLightningGroundState
                {
                    Id = attack.Id, Cycle = attack.Cycle, X = attack.X, Y = attack.Y,
                    StartedAt = attack.At, EndsAt = attack.At + duration, NextPulse = attack.At,
                    Radius = radius, Damage = damage * GroundDamageFactor,
                });
                if (previous != null && ReserveSupport(state, reserveSupportRegion, recordSupportSuppression))
                {
                    var halfWidth = GroundLinkWidth * Math.Min(2.5, 1 + areaBonus) / 2;
                    ApplyGroundImpact(attack, ExpandedLightningHitKind.GroundLink, damage * .30,
                        position => DistanceSquaredToSegment(position, previous.Center, attack.Position) <= halfWidth * halfWidth,
                        targets, applyHit);
                    state.NextGroundLinkAt = now + GroundSupportCooldown;
                }
            }
            if ((ranks & EighthStrikeGroundBurst) == 0 || attack.Id % 8 != 0 ||
                (state.NextGroundBurstAt ?? 0) > now + TimeEpsilon ||
                !ReserveSupport(state, reserveSupportRegion, recordSupportSuppression)) return;
            var burstRadius = GroundBurstRadius * Math.Min(2.5, 1 + areaBonus);
            ApplyGroundImpact(attack, ExpandedLightningHitKind.GroundBurst, damage * .35,
                position => InRange(position, attack.Position, burstRadius), targets, applyHit);
            state.NextGroundBurstAt = now + GroundSupportCooldown;
        }

        private static void ApplyGroundImpact(ExpandedLightningEventState attack, ExpandedLightningHitKind kind,
            double damage, Func<Vector2, bool> inShape, Func<IReadOnlyDictionary<int, Vector2>> targets,
            Func<ExpandedLightningHit, bool> applyHit)
        {
            var hitIds = targets().Where(target => inShape(target.Value)).Select(target => target.Key).OrderBy(id => id).ToArray();
            foreach (var id in hitIds)
            {
                if (!targets().TryGetValue(id, out var position) || !inShape(position)) continue;
                applyHit(new ExpandedLightningHit(kind, id, position, damage, attack.Id, attack.Cycle));
            }
        }

        private static void ResolveGroundPulses(ExpandedLightningState state, double now,
            Func<IReadOnlyDictionary<int, Vector2>> targets, Func<ExpandedLightningHit, bool> applyHit)
        {
            if (state.GroundCurrents == null) return;
            var due = state.GroundCurrents.Where(region => region.NextPulse <= now + TimeEpsilon).ToArray();
            if (due.Length == 0) return;
            foreach (var region in due)
                do { region.NextPulse += GroundPulseInterval; } while (region.NextPulse <= now + TimeEpsilon);

            var hits = new List<(ExpandedLightningGroundState Region, int TargetId)>();
            foreach (var target in targets())
            {
                var winner = due.Where(region => InRange(target.Value, region.Center, region.Radius))
                    .OrderByDescending(region => region.Damage).ThenBy(region => region.Id).FirstOrDefault();
                if (winner != null) hits.Add((winner, target.Key));
            }
            foreach (var (region, id) in hits.OrderBy(hit => hit.Region.Id).ThenBy(hit => hit.TargetId))
            {
                if (!targets().TryGetValue(id, out var position) || !InRange(position, region.Center, region.Radius)) continue;
                applyHit(new ExpandedLightningHit(ExpandedLightningHitKind.GroundCurrent, id, position,
                    region.Damage, region.Id, region.Cycle));
            }
        }

        private static double DistanceSquaredToSegment(Vector2 target, Vector2 from, Vector2 to)
        {
            var segment = to - from;
            var lengthSquared = (double)segment.X * segment.X + (double)segment.Y * segment.Y;
            if (lengthSquared == 0) return DistanceSquared(target, from);
            var offset = target - from;
            var fraction = Math.Max(0, Math.Min(1, ((double)offset.X * segment.X + (double)offset.Y * segment.Y) / lengthSquared));
            var x = offset.X - fraction * segment.X;
            var y = offset.Y - fraction * segment.Y;
            return x * x + y * y;
        }

        private static void AddAftershock(ExpandedLightningState state, ExpandedLightningEventState attack,
            List<int> visited, double parentDamage, double areaBonus)
        {
            var aftershock = attack.Copy();
            aftershock.At += AftershockDelay;
            aftershock.Aftershock = true;
            aftershock.ClearPrimaryModifiers();
            var rankBonus = (attack.EvolutionRanks & WiderEvolutionAftershock) != 0 ? .15 : 0;
            aftershock.Radius = AftershockRadius * Math.Min(2.5, 1 + areaBonus + rankBonus);
            aftershock.Damage = parentDamage * AftershockDamageFactor;
            aftershock.VisitedIds = visited;
            state.Pending.Add(aftershock);
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

        private static ((int TargetId, Vector2 Position, double Damage)? Nearest,
            (int TargetId, Vector2 Position, double Damage)? Elite,
            (int TargetId, Vector2 Position, double Damage)? First, HashSet<int>? CurrentVisited) HitArea(
            ExpandedLightningState state, ExpandedLightningEventState attack, double now,
            Func<IReadOnlyDictionary<int, Vector2>> targets, Func<ExpandedLightningHit, bool> applyHit,
            Func<int, bool>? isElite, Func<int, double>? targetHealthRatio, bool canProcCurrent,
            Func<ExpandedLightningHit, double?>? applyOriginalHit)
        {
            var snapshot = targets();
            var ids = snapshot.Where(target => InRange(target.Value, attack.Position, attack.Radius))
                .Select(target => target.Key).OrderBy(id => id).ToArray();
            var surrounding = attack.IsolatedDamage ? snapshot.Where(target =>
                InRange(target.Value, attack.Position, IsolationRadius)).Select(target => target.Key).Take(2).ToArray() : null;
            (int TargetId, Vector2 Position, double Damage)? nearest = null;
            var nearestDistance = double.PositiveInfinity;
            (int TargetId, Vector2 Position, double Damage)? firstElite = null;
            (int TargetId, Vector2 Position, double Damage)? first = null;
            var currentVisited = canProcCurrent ? new HashSet<int>() : null;
            var kind = attack.HitKind;
            var primary = attack.IsPrimary;
            var acceptedOriginalTargets = 0;
            foreach (var id in ids)
            {
                var live = targets();
                if (!live.TryGetValue(id, out var position) || !InRange(position, attack.Position, attack.Radius)) continue;
                var eligibleElite = !firstElite.HasValue && isElite?.Invoke(id) == true;
                var marked = (attack.CurrentRanks & MarkOnCurrent) != 0 && state.CurrentMarks?.Any(mark => mark.Id == id) == true;
                var isolated = surrounding?.Length == 1 && surrounding[0] == id;
                var eliteChainMarked = (attack.EvolutionRanks & EliteChainMark) != 0 && state.EliteChainDamageUntil.HasValue;
                var damage = StrikeDamage(state, attack, id, now, isolated, marked, eliteChainMarked, targetHealthRatio);
                var hit = new ExpandedLightningHit(kind, id, position, damage, attack.Id, attack.Cycle,
                    primary ? attack.OriginalBaseDamage : 0, primary ? acceptedOriginalTargets + 1 : 0);
                // The game returns the final pre-critical damage for this actual original.
                // Never call both callbacks for the same hit or reroll its critical result.
                var acceptedDamage = primary && applyOriginalHit != null
                    ? applyOriginalHit(hit) : applyHit(hit) ? damage : (double?)null;
                if (!acceptedDamage.HasValue) continue;

                if (eligibleElite) firstElite = (id, position, acceptedDamage.Value);
                if (primary)
                {
                    acceptedOriginalTargets++;
                    if (eliteChainMarked) state.EliteChainDamageUntil = null;
                    if (currentVisited != null)
                    {
                        first ??= (id, position, acceptedDamage.Value);
                        currentVisited.Add(id);
                    }
                    if (marked)
                    {
                        state.CurrentMarks!.RemoveAll(mark => mark.Id == id);
                        if (state.CurrentMarks.Count == 0) state.CurrentMarks = null;
                    }
                    if (attack.RepeatStrikeDamage) RecordStrikeHit(state, id, now, targets().ContainsKey(id));
                }

                var distance = DistanceSquared(position, attack.Position);
                if (distance >= nearestDistance) continue;
                nearestDistance = distance;
                nearest = (id, position, acceptedDamage.Value);
            }
            return (nearest, firstElite, first, currentVisited);
        }

        private static void TryEvolutionFork(ExpandedLightningState state, ExpandedLightningEventState attack,
            Vector2 firstTransfer, double parentDamage, HashSet<int> visited, double now,
            Func<IReadOnlyDictionary<int, Vector2>> targets, Func<ExpandedLightningHit, bool> applyHit)
        {
            var target = targets().Where(candidate => !visited.Contains(candidate.Key) &&
                    InRange(candidate.Value, firstTransfer, EvolutionForkRadius))
                .OrderBy(candidate => DistanceSquared(candidate.Value, firstTransfer))
                .ThenBy(candidate => candidate.Key).FirstOrDefault();
            if (target.Key == 0) return;
            state.NextEvolutionForkAt = now + CurrentCooldown;
            applyHit(new ExpandedLightningHit(ExpandedLightningHitKind.EvolutionFork, target.Key, target.Value,
                parentDamage * .30, attack.Id, attack.Cycle));
        }

        private static void TryCurrent(ExpandedLightningState state, ExpandedLightningEventState attack,
            (int TargetId, Vector2 Position, double Damage) first, HashSet<int> visited, double now,
            Func<IReadOnlyDictionary<int, Vector2>> targets, Func<ExpandedLightningHit, bool> applyHit)
        {
            var ranks = attack.CurrentRanks;
            var live = targets();
            var radius = CurrentRadius + ((ranks & FartherCurrent) != 0 ? 1 : 0);
            // Resolve after the base evolved chain so its accepted targets are also ineligible.
            var target = live.Where(candidate => !visited.Contains(candidate.Key) &&
                    InRange(candidate.Value, first.Position, radius))
                .OrderBy(candidate => DistanceSquared(candidate.Value, first.Position))
                .ThenBy(candidate => candidate.Key).FirstOrDefault();
            if (target.Key == 0)
            {
                if ((ranks & FallbackShock) == 0) return;
                state.NextCurrentAt = now + CurrentCooldown;
                if (live.TryGetValue(first.TargetId, out var position))
                    applyHit(new ExpandedLightningHit(ExpandedLightningHitKind.CurrentShock, first.TargetId, position,
                        first.Damage * .20, attack.Id, attack.Cycle));
                return;
            }
            state.NextCurrentAt = now + CurrentCooldown;
            var factor = .35 + ((ranks & StrongerCurrent) != 0 ? .15 : 0);
            if (!applyHit(new ExpandedLightningHit(ExpandedLightningHitKind.Current, target.Key, target.Value,
                first.Damage * factor, attack.Id, attack.Cycle)) || (ranks & MarkOnCurrent) == 0 ||
                !targets().ContainsKey(target.Key)) return;
            state.CurrentMarks ??= new List<BattleHitState>();
            var mark = state.CurrentMarks.Find(candidate => candidate.Id == target.Key);
            if (mark == null)
                state.CurrentMarks.Add(new BattleHitState { Id = target.Key, At = now + CurrentMarkDuration });
            else
                mark.At = now + CurrentMarkDuration;
        }

        private static void PruneCurrentMarks(ExpandedLightningState state, double now,
            Func<IReadOnlyDictionary<int, Vector2>> targets)
        {
            if (state.CurrentMarks == null) return;
            var live = targets();
            state.CurrentMarks.RemoveAll(mark => mark.At < now - TimeEpsilon || !live.ContainsKey(mark.Id));
            if (state.CurrentMarks.Count == 0) state.CurrentMarks = null;
        }

        private static double StrikeDamage(ExpandedLightningState state, ExpandedLightningEventState attack, int id,
            double now, bool isolated, bool marked, bool eliteChainMarked, Func<int, double>? targetHealthRatio)
        {
            var bonus = isolated ? .10 : 0;
            if (marked) bonus += .08;
            if (eliteChainMarked) bonus += .10;
            if ((attack.EvolutionRanks & FifthEvolutionStorm) != 0) bonus += .12;
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

        private static (List<int> Visited, Vector2? FirstTransfer, bool HitElite) Chain(ExpandedLightningEventState attack,
            (int TargetId, Vector2 Position, double Damage) anchor, Func<IReadOnlyDictionary<int, Vector2>> targets,
            Func<ExpandedLightningHit, bool> applyHit, Func<int, bool>? isElite)
        {
            var visited = new List<int> { anchor.TargetId };
            var attempted = new HashSet<int> { anchor.TargetId };
            var origin = anchor.Position;
            Vector2? firstTransfer = null;
            var hitElite = false;
            var limit = ChainLimit + (attack.LongerChain ? 1 : 0);
            while (visited.Count <= limit)
            {
                var next = targets().Where(target => !attempted.Contains(target.Key) && InRange(target.Value, origin, ChainRadius))
                    .OrderBy(target => DistanceSquared(target.Value, origin)).ThenBy(target => target.Key).FirstOrDefault();
                if (next.Key == 0) break;
                attempted.Add(next.Key);
                var elite = !hitElite && isElite?.Invoke(next.Key) == true;
                if (!applyHit(new ExpandedLightningHit(ExpandedLightningHitKind.Chain, next.Key, next.Value,
                    anchor.Damage * ChainDamageFactor, attack.Id, attack.Cycle)))
                    continue;
                visited.Add(next.Key);
                origin = next.Value;
                firstTransfer ??= next.Value;
                hitElite |= elite;
            }
            return (visited, firstTransfer, hitElite);
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
