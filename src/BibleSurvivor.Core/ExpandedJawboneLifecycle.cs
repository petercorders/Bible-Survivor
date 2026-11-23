using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.Serialization;

namespace BibleSurvivor.Core
{
    [Flags]
    public enum ExpandedJawboneLifecycleEffects : ushort
    {
        FirstKillPower = 1 << 0,
        RapidKillWait = 1 << 1,
        EliteAdvance = 1 << 2,
        CrowdReach = 1 << 3,
        Breakthrough = 1 << 4,
        CloseKnockback = 1 << 5,
        HurtPower = 1 << 6,
        EliteRecovery = 1 << 7,
        ContactGuard = 1 << 8,
        SurroundStrike = 1 << 9,
        EvolvedReach = 1 << 10,
        PushedCollision = 1 << 11,
        EliteFinalPower = 1 << 12,
        SmashWave = 1 << 13,
        EvolvedExtraSwing = 1 << 14,
    }

    [DataContract]
    public sealed class ExpandedJawbonePush
    {
        [DataMember(IsRequired = true)] public int Id { get; set; }
        [DataMember(IsRequired = true)] public double At { get; set; }
        [DataMember(IsRequired = true)] public double PreCriticalDamage { get; set; }

        internal ExpandedJawbonePush Copy() => (ExpandedJawbonePush)MemberwiseClone();
    }

    [DataContract]
    public sealed class ExpandedJawboneLifecycle
    {
        internal const double Epsilon = 1e-9;
        public const double SmashWaveCooldown = 3;
        public const double SmashWaveFactor = .35;
        public const double BreakthroughRadius = 2.2;
        private const double PushWindow = .30;
        private const double PushCooldown = 2;
        private const double PushFactor = .25;
        private const double MinCycle = .15;
        private const double EliteAdvanceSeconds = .15;
        private const double GuardDuration = .12;
        private const double MaximumPreCriticalDamage = 90;
        private const int BreakthroughKills = 20;
        private const int MaximumPushedActors = 120;

        [DataMember(EmitDefaultValue = false)] public long FirstKillCycle { get; set; }
        [DataMember(EmitDefaultValue = false)] public double? FirstKillUntil { get; set; }
        [DataMember(EmitDefaultValue = false)] public List<double>? RapidKills { get; set; }
        [DataMember(EmitDefaultValue = false)] public double? RapidWaitUntil { get; set; }
        [DataMember(EmitDefaultValue = false)] public double? NextRapidWaitAt { get; set; }
        [DataMember(EmitDefaultValue = false)] public bool AdvanceReady { get; set; }
        [DataMember(EmitDefaultValue = false)] public double? NextAdvanceAt { get; set; }
        [DataMember(EmitDefaultValue = false)] public double? CrowdReachUntil { get; set; }
        [DataMember(EmitDefaultValue = false)] public int BreakthroughCount { get; set; }
        [DataMember(EmitDefaultValue = false)] public double? NextBreakthroughAt { get; set; }
        [DataMember(EmitDefaultValue = false)] public double? HurtPowerUntil { get; set; }
        [DataMember(EmitDefaultValue = false)] public double? NextHurtPowerAt { get; set; }
        [DataMember(EmitDefaultValue = false)] public double? NextRecoveryAt { get; set; }
        [DataMember(EmitDefaultValue = false)] public double? GuardUntil { get; set; }
        [DataMember(EmitDefaultValue = false)] public double? NextSurroundAt { get; set; }
        [DataMember(EmitDefaultValue = false)] public double? NextWaveAt { get; set; }
        [DataMember(EmitDefaultValue = false)] public double? NextPushAt { get; set; }
        [DataMember(EmitDefaultValue = false)] public double? ExtraSwingUntil { get; set; }
        [DataMember(EmitDefaultValue = false)] public double? NextExtraSwingAt { get; set; }
        [DataMember(EmitDefaultValue = false)] public List<ExpandedJawbonePush>? Pushes { get; set; }

        public ExpandedJawboneLifecycle Copy()
        {
            var copy = (ExpandedJawboneLifecycle)MemberwiseClone();
            copy.RapidKills = RapidKills?.ToList();
            copy.Pushes = Pushes?.Select(push => push.Copy()).ToList();
            return copy;
        }

        public void Prune(double now)
        {
            RapidKills?.RemoveAll(at => at + 2 < now - Epsilon);
            if (RapidKills?.Count == 0) RapidKills = null;
            if (FirstKillUntil < now - Epsilon) FirstKillUntil = null;
            if (RapidWaitUntil < now - Epsilon) RapidWaitUntil = null;
            if (CrowdReachUntil < now - Epsilon) CrowdReachUntil = null;
            if (HurtPowerUntil < now - Epsilon) HurtPowerUntil = null;
            if (ExtraSwingUntil < now - Epsilon) ExtraSwingUntil = null;
            if (GuardUntil <= now + Epsilon) GuardUntil = null;
            if (NextRapidWaitAt <= now + Epsilon) NextRapidWaitAt = null;
            if (NextAdvanceAt <= now + Epsilon) NextAdvanceAt = null;
            if (NextBreakthroughAt <= now + Epsilon) NextBreakthroughAt = null;
            if (NextHurtPowerAt <= now + Epsilon) NextHurtPowerAt = null;
            if (NextRecoveryAt <= now + Epsilon) NextRecoveryAt = null;
            if (NextSurroundAt <= now + Epsilon) NextSurroundAt = null;
            if (NextWaveAt <= now + Epsilon) NextWaveAt = null;
            if (NextPushAt <= now + Epsilon) NextPushAt = null;
            if (NextExtraSwingAt <= now + Epsilon) NextExtraSwingAt = null;
            Pushes?.RemoveAll(push => push.At + PushWindow < now - Epsilon);
            if (Pushes?.Count == 0) Pushes = null;
        }

        // Keep per-weapon time and counters on a form transition, but never carry
        // physical pushed actors or an evolved pending swing into the basic form.
        public void ResetForForm(bool evolved)
        {
            Pushes = null;
            if (evolved) return;
            NextWaveAt = null;
            NextPushAt = null;
            ExtraSwingUntil = null;
            NextExtraSwingAt = null;
        }

        public (int Amount, double IntervalToNext) BeginCycle(ExpandedJawboneRanks ranks, double now, bool evolved,
            int amount, double interval, double nativeCycle, double computedCycle)
        {
            Prune(now);
            if (evolved && ranks.Has(ExpandedJawboneLifecycleEffects.EvolvedExtraSwing) && ExtraSwingUntil.HasValue)
            {
                amount = Math.Min(CombatStats.ExpandedAmountLimit("jawbone"), amount + 1);
                ExtraSwingUntil = null;
            }
            var batchFloor = MinimumScheduleSpacing(amount, interval);
            var reduced = ranks.Has(ExpandedJawboneLifecycleEffects.RapidKillWait) && RapidWaitUntil.HasValue
                ? Math.Max(.20 * nativeCycle, computedCycle - .05 * nativeCycle) : computedCycle;
            if (ranks.Has(ExpandedJawboneLifecycleEffects.EliteAdvance) && AdvanceReady)
            {
                AdvanceReady = false;
                reduced -= EliteAdvanceSeconds;
            }
            return (amount, Math.Max(batchFloor, reduced));
        }

        public static double MinimumScheduleSpacing(int amount, double interval) =>
            Math.Max(MinCycle, (amount - 1) * interval + BattleWorld.FixedStep);

        public (double DamageBonus, double ReachBonus, bool FullCircle) BeginSwing(
            ExpandedJawboneRanks ranks, double now, int nearbyWithinTwo)
        {
            Prune(now);
            var damage = ranks.Has(ExpandedJawboneLifecycleEffects.FirstKillPower) && FirstKillUntil.HasValue;
            if (damage) FirstKillUntil = null;
            var reach = ranks.Has(ExpandedJawboneLifecycleEffects.CrowdReach) && CrowdReachUntil.HasValue;
            if (reach) CrowdReachUntil = null;
            var fullCircle = ranks.Has(ExpandedJawboneLifecycleEffects.SurroundStrike) &&
                nearbyWithinTwo >= 6 && !NextSurroundAt.HasValue;
            if (fullCircle) NextSurroundAt = now + 5;
            return (damage ? .08 : 0, reach ? .08 : 0, fullCircle);
        }

        public void EndSwing(ExpandedJawboneRanks ranks, double now, int acceptedOriginals)
        {
            if (ranks.Has(ExpandedJawboneLifecycleEffects.CrowdReach) && acceptedOriginals >= 4)
                CrowdReachUntil = now + 2;
        }

        public bool TryArmHurt(ExpandedJawboneRanks ranks, double now, double received)
        {
            if (!ranks.Has(ExpandedJawboneLifecycleEffects.HurtPower) || !BattleSnapshot.Finite(received) || received <= 0 ||
                NextHurtPowerAt > now + Epsilon) return false;
            HurtPowerUntil = now + 3;
            NextHurtPowerAt = now + 5;
            return true;
        }

        public double HurtDamageBonusAt(ExpandedJawboneRanks ranks, double now) =>
            ranks.Has(ExpandedJawboneLifecycleEffects.HurtPower) && HurtPowerUntil >= now - Epsilon ? .10 : 0;

        internal int DefenseBonusAt(ExpandedJawboneRanks ranks, double now, IncomingDamageKind kind) =>
            kind == IncomingDamageKind.Contact && ranks.Has(ExpandedJawboneLifecycleEffects.ContactGuard) &&
                GuardUntil > now + Epsilon ? 1 : 0;

        // Capture the accepted pre-critical amount returned by World and classify
        // before damaging. Auxiliary damage never calls this path.
        public (double NextCycle, double BreakthroughDamage, double Recovery) AcceptedOriginal(ExpandedJawboneRanks ranks,
            double now, long cycle, bool evolved, bool killed, bool elite, double preCriticalDamage,
            double nextCycle, double earliestNextCycle)
        {
            if (HurtDamageBonusAt(ranks, now) > 0) HurtPowerUntil = null;
            if (ranks.Has(ExpandedJawboneLifecycleEffects.ContactGuard)) GuardUntil = now + GuardDuration;
            if (!killed) return (nextCycle, 0, 0);

            if (ranks.Has(ExpandedJawboneLifecycleEffects.FirstKillPower) && cycle > FirstKillCycle)
            {
                FirstKillCycle = cycle;
                FirstKillUntil = now + 2;
            }

            if (elite)
            {
                if (ranks.Has(ExpandedJawboneLifecycleEffects.EliteAdvance) && !AdvanceReady && !NextAdvanceAt.HasValue)
                {
                    if (nextCycle > now + Epsilon)
                        nextCycle = Math.Min(nextCycle, Math.Max(Math.Max(now + BattleWorld.FixedStep, earliestNextCycle),
                            nextCycle - EliteAdvanceSeconds));
                    else
                        AdvanceReady = true;
                    NextAdvanceAt = now + 5;
                }
                if (evolved && ranks.Has(ExpandedJawboneLifecycleEffects.EvolvedExtraSwing) && !NextExtraSwingAt.HasValue)
                {
                    ExtraSwingUntil = now + 3;
                    NextExtraSwingAt = now + 6;
                }
                return (nextCycle, 0, RecoveryAfterElite(ranks, now));
            }

            RecordRapidKill(ranks, now);
            if (!ranks.Has(ExpandedJawboneLifecycleEffects.Breakthrough) || NextBreakthroughAt.HasValue ||
                ++BreakthroughCount != BreakthroughKills) return (nextCycle, 0, 0);
            BreakthroughCount = 0;
            NextBreakthroughAt = now + 5;
            return (nextCycle, preCriticalDamage * .45, 0);
        }

        private double RecoveryAfterElite(ExpandedJawboneRanks ranks, double now)
        {
            if (!ranks.Has(ExpandedJawboneLifecycleEffects.EliteRecovery) || NextRecoveryAt.HasValue) return 0;
            NextRecoveryAt = now + 6;
            return 2;
        }

        private void RecordRapidKill(ExpandedJawboneRanks ranks, double now)
        {
            if (!ranks.Has(ExpandedJawboneLifecycleEffects.RapidKillWait) || NextRapidWaitAt.HasValue) return;
            RapidKills ??= new List<double>();
            RapidKills.RemoveAll(at => at + 2 < now - Epsilon);
            RapidKills.Add(now);
            if (RapidKills.Count < 4) return;
            RapidKills = null;
            RapidWaitUntil = now + 3;
            NextRapidWaitAt = now + 6;
        }

        public bool CanEmitSmashWave(ExpandedJawboneRanks ranks, double now, bool evolved, ExpandedJawboneSwing swing) =>
            evolved && swing.IsGroundSmash && ranks.Has(ExpandedJawboneLifecycleEffects.SmashWave) &&
            NextWaveAt.GetValueOrDefault() <= now + Epsilon;

        public void RecordSmashWave(double now) => NextWaveAt = now + SmashWaveCooldown;

        public void RecordPush(ExpandedJawboneRanks ranks, double now, int id, double preCriticalDamage,
            Vector2 from, Vector2 to, IReadOnlyDictionary<int, Vector2> positions, Func<int, double> radius,
            Func<ExpandedJawbonePushHit, bool> applySupport)
        {
            if (!ranks.Has(ExpandedJawboneLifecycleEffects.PushedCollision) || NextPushAt > now + Epsilon ||
                !positions.ContainsKey(id) || Vector2.DistanceSquared(from, to) < 1e-12f) return;
            Prune(now);
            Pushes ??= new List<ExpandedJawbonePush>();
            var push = Pushes.Find(item => item.Id == id);
            if (push == null)
            {
                if (Pushes.Count >= MaximumPushedActors) return;
                push = new ExpandedJawbonePush { Id = id, At = now, PreCriticalDamage = preCriticalDamage };
                var index = Pushes.FindIndex(item => item.Id > id);
                if (index < 0) Pushes.Add(push);
                else Pushes.Insert(index, push);
            }
            var contact = FindPushContact(push, from, to, positions, null, radius);
            if (contact.HasValue) SettlePush(now, contact.Value, applySupport);
        }

        public void ResolvePushes(double now, IReadOnlyDictionary<int, Vector2> before,
            IReadOnlyDictionary<int, Vector2> positions, Func<int, double> radius,
            Func<ExpandedJawbonePushHit, bool> applySupport)
        {
            Prune(now);
            if (Pushes == null) return;
            Pushes.RemoveAll(push => !positions.ContainsKey(push.Id));
            var contacts = new List<ExpandedJawbonePushHit>();
            foreach (var push in Pushes)
            {
                var to = positions[push.Id];
                var from = before.TryGetValue(push.Id, out var start) ? start : to;
                var contact = FindPushContact(push, from, to, positions, before, radius);
                if (contact.HasValue) contacts.Add(contact.Value);
            }
            foreach (var contact in contacts.OrderBy(hit => hit.SweepTime).ThenBy(hit => hit.DistanceSquared)
                .ThenBy(hit => hit.SourceId).ThenBy(hit => hit.TargetId))
            {
                if (Pushes == null) break;
                if (positions.ContainsKey(contact.SourceId) && positions.ContainsKey(contact.TargetId))
                    SettlePush(now, contact, applySupport);
            }
            if (Pushes?.Count == 0) Pushes = null;
        }

        private void SettlePush(double now, ExpandedJawbonePushHit contact, Func<ExpandedJawbonePushHit, bool> applySupport)
        {
            Pushes!.RemoveAll(push => push.Id == contact.SourceId);
            if (Pushes.Count == 0) Pushes = null;
            if (NextPushAt > now + Epsilon || !applySupport(contact)) return;
            NextPushAt = now + PushCooldown;
            Pushes = null; // Its cooldown outlives every current .30-second window.
        }

        private static ExpandedJawbonePushHit? FindPushContact(ExpandedJawbonePush push,
            Vector2 from, Vector2 to, IReadOnlyDictionary<int, Vector2> positions,
            IReadOnlyDictionary<int, Vector2>? before, Func<int, double> radius)
        {
            ExpandedJawbonePushHit? first = null;
            var sourceRadius = radius(push.Id);
            foreach (var target in positions)
            {
                if (target.Key == push.Id) continue;
                var old = before != null && before.TryGetValue(target.Key, out var previous) ? previous : target.Value;
                var time = SweptContact(from - old, to - target.Value, sourceRadius + radius(target.Key));
                if (!time.HasValue) continue;
                var distance = Vector2.DistanceSquared(from, old);
                if (first is { } found && (time.Value > found.SweepTime + Epsilon ||
                    Math.Abs(time.Value - found.SweepTime) <= Epsilon &&
                    (distance > found.DistanceSquared || distance == found.DistanceSquared && target.Key > found.TargetId)))
                    continue;
                first = new ExpandedJawbonePushHit(push.Id, target.Key,
                    Vector2.Lerp(old, target.Value, (float)time.Value), push.PreCriticalDamage * PushFactor, time.Value, distance);
            }
            return first;
        }

        private static double? SweptContact(Vector2 from, Vector2 to, double radius)
        {
            var distanceSquared = (double)Vector2.Dot(from, from);
            var outside = distanceSquared - radius * radius;
            if (outside <= 1e-8) return 0;
            var delta = to - from;
            var speedSquared = (double)Vector2.Dot(delta, delta);
            if (speedSquared <= 1e-16) return null;
            var along = (double)Vector2.Dot(from, delta);
            if (along >= 0) return null;
            var discriminant = along * along - speedSquared * outside;
            if (discriminant < 0) return null;
            var entry = (-along - Math.Sqrt(discriminant)) / speedSquared;
            return entry <= 1 + 1e-7 ? Math.Min(1, entry) : (double?)null;
        }

        public void Validate(double now, long cycle, bool evolved)
        {
            if (!BattleSnapshot.Finite(now) || now < 0 || FirstKillCycle < 0 || FirstKillCycle > cycle ||
                FirstKillCycle == 0 && FirstKillUntil.HasValue || !Receipt(FirstKillUntil, now, 2) ||
                !Window(RapidWaitUntil, NextRapidWaitAt, now, 3, 6) ||
                RapidKills is { } rapid && (rapid.Count < 1 || rapid.Count > 3 || NextRapidWaitAt.HasValue ||
                    rapid.Where((at, i) => !Past(at, now, 2) || i > 0 && at < rapid[i - 1]).Any()) ||
                !Receipt(NextAdvanceAt, now, 5) || !Receipt(CrowdReachUntil, now, 2) ||
                BreakthroughCount < 0 || BreakthroughCount >= BreakthroughKills ||
                BreakthroughCount != 0 && NextBreakthroughAt.HasValue || !Receipt(NextBreakthroughAt, now, 5) ||
                !Window(HurtPowerUntil, NextHurtPowerAt, now, 3, 5) ||
                !Receipt(NextRecoveryAt, now, 6) || !Receipt(GuardUntil, now, GuardDuration) ||
                !Receipt(NextSurroundAt, now, 5) || !Receipt(NextWaveAt, now, SmashWaveCooldown) ||
                !Receipt(NextPushAt, now, PushCooldown) || !Window(ExtraSwingUntil, NextExtraSwingAt, now, 3, 6) ||
                !evolved && (NextWaveAt.HasValue || NextPushAt.HasValue || ExtraSwingUntil.HasValue ||
                    NextExtraSwingAt.HasValue || Pushes != null))
                throw new ArgumentException("Invalid jawbone lifecycle");

            if (Pushes == null) return;
            if (Pushes.Count == 0 || Pushes.Count > MaximumPushedActors || NextPushAt.HasValue)
                throw new ArgumentException("Invalid jawbone push count");
            var previous = 0;
            foreach (var push in Pushes)
            {
                if (push == null || push.Id <= previous || !Past(push.At, now, PushWindow) ||
                    !BattleSnapshot.Finite(push.PreCriticalDamage) || push.PreCriticalDamage < 1 ||
                    push.PreCriticalDamage > MaximumPreCriticalDamage)
                    throw new ArgumentException("Invalid jawbone push");
                previous = push.Id;
            }
        }

        private static bool Receipt(double? at, double now, double horizon) =>
            !at.HasValue || BattleSnapshot.Finite(at.Value) && at >= now - Epsilon && at <= now + horizon + Epsilon;

        private static bool Past(double at, double now, double horizon) =>
            BattleSnapshot.Finite(at) && at >= 0 && at <= now + Epsilon && at >= now - horizon - Epsilon;

        private static bool Window(double? end, double? cooldown, double now, double duration, double cooldownDuration) =>
            Receipt(end, now, duration) && Receipt(cooldown, now, cooldownDuration) &&
            (!end.HasValue || cooldown.HasValue && Math.Abs(cooldown.Value - end.Value - (cooldownDuration - duration)) <= Epsilon);
    }

    public readonly struct ExpandedJawbonePushHit
    {
        public int SourceId { get; }
        public int TargetId { get; }
        public Vector2 Position { get; }
        public double Damage { get; }
        internal double SweepTime { get; }
        internal double DistanceSquared { get; }

        internal ExpandedJawbonePushHit(int sourceId, int targetId, Vector2 position, double damage,
            double sweepTime, double distanceSquared)
        {
            SourceId = sourceId;
            TargetId = targetId;
            Position = position;
            Damage = damage;
            SweepTime = sweepTime;
            DistanceSquared = distanceSquared;
        }
    }
}
