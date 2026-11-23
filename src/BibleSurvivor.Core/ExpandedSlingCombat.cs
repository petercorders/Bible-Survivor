using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.Serialization;

namespace BibleSurvivor.Core
{
    internal readonly struct ExpandedSlingRanks
    {
        internal readonly bool RepeatVolleyHit, FourthFirstStrike, AlternateTargets, RetargetAfterKill, FourthCopy, StoneStorm,
            Ricochet, WiderRicochet, StrongerRicochet, RicochetBurst, ReturnRicochet,
            AlignCrowd, DeepHit, Rupture, SixthFullPierce, LockEvolvedFocus, FifthHeavy, BossVolley, EliteShards, GuaranteedCritical;

        internal ExpandedSlingRanks(IReadOnlyDictionary<string, int> ranks)
        {
            bool Has(string node) => PermanentRules.Rank(ranks, "weapon.sling." + node) > 0;
            RepeatVolleyHit = Has("a.04"); FourthFirstStrike = Has("a.05");
            AlternateTargets = Has("b.02"); RetargetAfterKill = Has("b.03"); FourthCopy = Has("b.04"); StoneStorm = Has("b.05");
            Ricochet = Has("c.01"); WiderRicochet = Has("c.02"); StrongerRicochet = Has("c.03");
            RicochetBurst = Has("c.04"); ReturnRicochet = Has("c.05");
            AlignCrowd = Has("d.02"); DeepHit = Has("d.03"); Rupture = Has("d.04"); SixthFullPierce = Has("d.05");
            LockEvolvedFocus = Has("e.01"); FifthHeavy = Has("e.02"); BossVolley = Has("e.03");
            EliteShards = Has("e.04"); GuaranteedCritical = Has("e.05");
        }

        internal bool HasSpecialization => RepeatVolleyHit || AlternateTargets || Ricochet || AlignCrowd || LockEvolvedFocus;

        internal int ChooseTarget(Vector2 origin, IReadOnlyDictionary<int, Vector2> targets,
            Func<int, bool> isElite, int preferred, int previousTarget, int slot, double reach, bool evolved, int focus,
            bool previousKilled)
        {
            if (evolved && LockEvolvedFocus && targets.ContainsKey(focus)) return focus;
            if (preferred != 0 && targets.ContainsKey(preferred)) return preferred;
            var ordered = targets.OrderBy(target => Vector2.DistanceSquared(target.Value, origin)).ThenBy(target => target.Key)
                .ToArray();
            if (ordered.Length == 0) return 0;
            if (RetargetAfterKill && previousKilled && !targets.ContainsKey(previousTarget))
                return ordered[0].Key;
            if (!ordered.Any(target => isElite(target.Key) && Vector2.DistanceSquared(target.Value, origin) <= reach * reach))
            {
                if (AlignCrowd)
                {
                    var best = 0;
                    var score = 2;
                    foreach (var target in ordered)
                    {
                        var offset = target.Value - origin;
                        if (offset.LengthSquared() <= 1e-12f) continue;
                        var direction = Vector2.Normalize(offset);
                        var count = ordered.Count(other => InAimLane(other.Value - origin, direction));
                        if (count <= score) continue;
                        best = target.Key;
                        score = count;
                    }
                    if (best != 0) return best;
                }
                if (AlternateTargets) return ordered[(slot - 1) % ordered.Length].Key;
            }
            return ordered[0].Key;
        }

        private static bool InAimLane(Vector2 offset, Vector2 direction)
        {
            var distance = Vector2.Dot(offset, direction);
            var side = (double)offset.X * direction.Y - (double)offset.Y * direction.X;
            return distance >= -1e-6 && distance <= 6 + 1e-6 && Math.Abs(side) <= .3 + 1e-6;
        }

        internal double HitDamage(double raw, double damage, long cycle, int slot, int ordinal, bool elite,
            bool previouslyHit, bool evolved, bool strengthenedVolley)
        {
            var extra = (RepeatVolleyHit && previouslyHit ? .06 : 0) +
                (FourthFirstStrike && cycle % 4 == 0 && slot == 1 && elite ? .12 : 0) +
                (DeepHit && ordinal >= 2 ? .08 : 0) +
                (evolved && FifthHeavy && slot == 5 ? .15 : 0) +
                (evolved && strengthenedVolley ? .08 : 0);
            return extra > 0 ? Math.Min(raw * 2.25, damage + raw * extra) : damage;
        }
    }

    public enum ExpandedSlingSupportKind { CycleCopy, Storm, Ricochet, Return }

    [DataContract]
    public sealed class ExpandedSlingSupportState
    {
        [DataMember(IsRequired = true)] public ExpandedSlingSupportKind Kind { get; set; }
        [DataMember(IsRequired = true)] public float X { get; set; }
        [DataMember(IsRequired = true)] public float Y { get; set; }
        [DataMember(IsRequired = true)] public float DirectionX { get; set; }
        [DataMember(IsRequired = true)] public float DirectionY { get; set; }
        [DataMember(IsRequired = true)] public double StartedAt { get; set; }
        [DataMember(IsRequired = true)] public double Remaining { get; set; }
        [DataMember(IsRequired = true)] public double Damage { get; set; }
        [DataMember(EmitDefaultValue = false)] public int TargetId { get; set; }
        [DataMember(EmitDefaultValue = false)] public int ExcludedId { get; set; }
        public Vector2 Position => new Vector2(X, Y);
        public Vector2 Direction => new Vector2(DirectionX, DirectionY);
        internal ExpandedSlingSupportState Copy() => (ExpandedSlingSupportState)MemberwiseClone();
    }

    [DataContract]
    public sealed class ExpandedSlingVolleyHit
    {
        [DataMember(IsRequired = true)] public long Cycle { get; set; }
        [DataMember(IsRequired = true)] public int TargetId { get; set; }
        internal ExpandedSlingVolleyHit Copy() => (ExpandedSlingVolleyHit)MemberwiseClone();
    }

    [DataContract]
    public sealed class ExpandedSlingCombatState
    {
        [DataMember(EmitDefaultValue = false)] public int Slot { get; set; }
        [DataMember(EmitDefaultValue = false)] public int LastTargetId { get; set; }
        [DataMember(EmitDefaultValue = false)] public long LastOriginalId { get; set; }
        [DataMember(EmitDefaultValue = false)] public bool LastTargetKilled { get; set; }
        [DataMember(EmitDefaultValue = false)] public bool StrengthenedVolley { get; set; }
        [DataMember(EmitDefaultValue = false)] public List<ExpandedSlingVolleyHit>? VolleyHits { get; set; }
        [DataMember(EmitDefaultValue = false)] public List<ExpandedSlingSupportState>? Support { get; set; }
        [DataMember(EmitDefaultValue = false)] public double? NextRicochetBurstAt { get; set; }
        [DataMember(EmitDefaultValue = false)] public double? NextReturnAt { get; set; }
        [DataMember(EmitDefaultValue = false)] public double? NextRuptureAt { get; set; }
        [DataMember(EmitDefaultValue = false)] public int NormalHits { get; set; }
        [DataMember(EmitDefaultValue = false)] public double? NextStormAt { get; set; }
        [DataMember(EmitDefaultValue = false)] public double? StormSecondAt { get; set; }
        [DataMember(EmitDefaultValue = false)] public double StormSecondDamage { get; set; }
        [DataMember(EmitDefaultValue = false)] public int BossHits { get; set; }
        [DataMember(EmitDefaultValue = false)] public double? NextBossBoostAt { get; set; }
        [DataMember(EmitDefaultValue = false)] public bool BossBoostPending { get; set; }
        [DataMember(EmitDefaultValue = false)] public double? NextShardsAt { get; set; }

        public int SupportCount => (Support?.Count ?? 0) + (StormSecondAt.HasValue ? 1 : 0);

        internal ExpandedSlingCombatState Copy()
        {
            var copy = (ExpandedSlingCombatState)MemberwiseClone();
            copy.VolleyHits = VolleyHits?.Select(hit => hit.Copy()).ToList();
            copy.Support = Support?.Select(shot => shot.Copy()).ToList();
            return copy;
        }

        internal void Validate(double now, long cycle)
        {
            if (Slot < 0 || Slot > CombatStats.ExpandedAmountLimit("sling") || LastTargetId < 0 || LastOriginalId < 0 ||
                (LastTargetId == 0) != (LastOriginalId == 0) || LastTargetKilled && LastTargetId == 0 ||
                NormalHits < 0 || NormalHits > 14 || BossHits < 0 || BossHits > 11 ||
                !ValidAt(NextRicochetBurstAt, now, 2) || !ValidAt(NextReturnAt, now, 2) || !ValidAt(NextRuptureAt, now, 1) ||
                !ValidAt(NextStormAt, now, 5) || !ValidAt(StormSecondAt, now, .15) ||
                !ValidAt(NextBossBoostAt, now, 5) || !ValidAt(NextShardsAt, now, 3) ||
                StormSecondAt.HasValue != (StormSecondDamage > 0) ||
                !BattleSnapshot.Finite(StormSecondDamage) || StormSecondDamage < 0 ||
                SupportCount > BattleSnapshot.ExpandedSupportProjectileLimit)
                throw new ArgumentException("Invalid sling combat state");

            if (VolleyHits != null && (VolleyHits.Count < 1 || VolleyHits.Count > 600 ||
                VolleyHits.Any(hit => hit == null || hit.Cycle < 1 || hit.Cycle > cycle || hit.TargetId <= 0) ||
                VolleyHits.Select(hit => (hit.Cycle, hit.TargetId)).Distinct().Count() != VolleyHits.Count))
                throw new ArgumentException("Invalid sling volley hits");

            if (Support != null && (Support.Count == 0 || Support.Any(shot => shot == null ||
                !Enum.IsDefined(typeof(ExpandedSlingSupportKind), shot.Kind) ||
                !BattleSnapshot.Position(shot.X, shot.Y) || !BattleSnapshot.Position(shot.DirectionX, shot.DirectionY) ||
                Math.Abs(shot.Direction.LengthSquared() - 1) > .0001 ||
                !BattleSnapshot.Finite(shot.StartedAt) || shot.StartedAt < 0 || shot.StartedAt > now + 1e-9 ||
                !BattleSnapshot.Finite(shot.Remaining) || shot.Remaining <= 0 || shot.Remaining > 4 ||
                !BattleSnapshot.Finite(shot.Damage) || shot.Damage <= 0 || shot.TargetId < 0 || shot.ExcludedId < 0 ||
                shot.TargetId != 0 && shot.TargetId == shot.ExcludedId)))
                throw new ArgumentException("Invalid sling support projectiles");
        }

        private static bool ValidAt(double? at, double now, double duration) =>
            !at.HasValue || BattleSnapshot.Finite(at.Value) && at.Value >= 0 && at.Value <= now + duration + 1e-9;
    }
}
