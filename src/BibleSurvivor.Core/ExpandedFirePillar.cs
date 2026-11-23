using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.Serialization;

namespace BibleSurvivor.Core
{
    public readonly struct ExpandedFirePillarSettings
    {
        internal bool PreferCrowds { get; }
        internal Func<int, bool>? IsElite { get; }
        internal bool ExtendedEmbers { get; }
        internal double EmberRadiusBonus { get; }
        internal bool PredictPlacement { get; }
        internal bool PreferSeparatedCenters { get; }
        internal bool SlowInCrowds { get; }
        internal bool IncreaseCoreDamage { get; }
        internal bool IncreaseIgnitionDamage { get; }
        internal double EmberDurationBonus { get; }

        public ExpandedFirePillarSettings(bool preferCrowds, Func<int, bool>? isElite, bool extendedEmbers,
            double emberRadiusBonus = 0, bool predictPlacement = false, bool preferSeparatedCenters = false,
            bool slowInCrowds = false, bool increaseCoreDamage = false, bool increaseIgnitionDamage = false,
            double emberDurationBonus = 0)
        {
            if (!BattleSnapshot.Finite(emberRadiusBonus) || emberRadiusBonus < 0 || emberRadiusBonus > 1.5)
                throw new ArgumentOutOfRangeException(nameof(emberRadiusBonus));
            if (!ExpandedWeaponRules.ValidResidualDurationBonus(emberDurationBonus))
                throw new ArgumentOutOfRangeException(nameof(emberDurationBonus));
            PreferCrowds = preferCrowds;
            IsElite = isElite;
            ExtendedEmbers = extendedEmbers;
            EmberRadiusBonus = emberRadiusBonus;
            PredictPlacement = predictPlacement;
            PreferSeparatedCenters = preferSeparatedCenters;
            SlowInCrowds = slowInCrowds;
            IncreaseCoreDamage = increaseCoreDamage;
            IncreaseIgnitionDamage = increaseIgnitionDamage;
            EmberDurationBonus = emberDurationBonus;
        }
    }

    public readonly struct ExpandedFirePillarHit
    {
        public int TargetId { get; }
        public Vector2 Position { get; }
        public double Damage { get; }
        public bool IsEmber { get; }
        public long Cycle { get; }

        internal ExpandedFirePillarHit(int targetId, Vector2 position, double damage, bool isEmber, long cycle)
        {
            TargetId = targetId;
            Position = position;
            Damage = damage;
            IsEmber = isEmber;
            Cycle = cycle;
        }
    }

    [DataContract]
    public sealed class ExpandedFireRegion
    {
        [DataMember(IsRequired = true)] public long Id { get; set; }
        [DataMember(IsRequired = true)] public bool Ember { get; set; }
        [DataMember(EmitDefaultValue = false)] public long Cycle { get; set; }
        [DataMember(IsRequired = true)] public float X { get; set; }
        [DataMember(IsRequired = true)] public float Y { get; set; }
        [DataMember(IsRequired = true)] public double StartedAt { get; set; }
        [DataMember(IsRequired = true)] public double EndsAt { get; set; }
        [DataMember(IsRequired = true)] public double NextHit { get; set; }
        [DataMember(EmitDefaultValue = false)] public double? FirstDelay { get; set; }
        [DataMember(EmitDefaultValue = false)] public bool ShortenedPrimaryInterval { get; set; }
        [DataMember(IsRequired = true)] public double Damage { get; set; }
        [DataMember(EmitDefaultValue = false)] public double? BaseDamage { get; set; }
        [DataMember(EmitDefaultValue = false)] public bool IncreasedCoreDamage { get; set; }
        [DataMember(EmitDefaultValue = false)] public bool IncreasedIgnitionDamage { get; set; }
        [DataMember(IsRequired = true)] public double Radius { get; set; }
        [DataMember(EmitDefaultValue = false)] public int TargetId { get; set; }
        [DataMember(EmitDefaultValue = false)] public double NextTargetAt { get; set; }
        [DataMember(EmitDefaultValue = false)] public double TravelSinceEmber { get; set; }

        internal Vector2 Center => new Vector2(X, Y);
        public double IgnitionAt => StartedAt + (Ember ? 0 : FirstDelay ?? ExpandedFirePillar.Profile.FirstAttackDelay);
        internal ExpandedFireRegion Copy() => (ExpandedFireRegion)MemberwiseClone();
    }

    [DataContract]
    public sealed class ExpandedFirePillarState
    {
        [DataMember(IsRequired = true)] public bool Evolved { get; set; }
        [DataMember(IsRequired = true)] public bool WaitingForTarget { get; set; }
        [DataMember(IsRequired = true)] public bool SuppressedOriginal { get; set; }
        [DataMember(IsRequired = true)] public long NextId { get; set; } = 1;
        [DataMember(EmitDefaultValue = false)] public long Cycle { get; set; }
        [DataMember(IsRequired = true)] public double NextCycle { get; set; }
        [DataMember(IsRequired = true)] public double LastStep { get; set; }
        [DataMember(IsRequired = true)] public List<ExpandedFireRegion> Regions { get; set; } = new List<ExpandedFireRegion>();

        public ExpandedFirePillarState Copy() => new ExpandedFirePillarState
        {
            Evolved = Evolved, WaitingForTarget = WaitingForTarget, SuppressedOriginal = SuppressedOriginal,
            NextId = NextId, NextCycle = NextCycle, LastStep = LastStep, Cycle = Cycle,
            Regions = Regions.Select(region => region.Copy()).ToList()
        };

        public void ResetForForm(bool evolved, double now)
        {
            Evolved = evolved;
            NextCycle = now;
            LastStep = now;
            WaitingForTarget = false;
            Regions.Clear();
        }

        public void Validate(double now, bool evolved)
        {
            const double epsilon = ExpandedFirePillar.TimeEpsilon;
            if (Evolved != evolved || NextId < 1 || Cycle < 0 || !BattleSnapshot.Finite(LastStep) || LastStep < 0 ||
                LastStep > now + epsilon || !BattleSnapshot.Finite(NextCycle) || NextCycle < 0 ||
                NextCycle > now + 3 + epsilon || Regions == null || Regions.Count > 48 ||
                Regions.Any(region => region == null) ||
                Regions.Count(region => region.Ember) > 16 || Regions.Count(region => !region.Ember) > 32 ||
                Regions.Select(region => region.Id).Distinct().Count() != Regions.Count)
                throw new ArgumentException("Invalid expanded fire pillar clock");

            foreach (var region in Regions)
            {
                if (region.Id < 1 || region.Id >= NextId || region.Cycle < 0 || region.Cycle > Cycle ||
                    (region.Ember && (!Evolved || region.Cycle != 0)) ||
                    !BattleSnapshot.Position(region.X, region.Y) ||
                    !BattleSnapshot.Finite(region.StartedAt) || region.StartedAt < 0 || region.StartedAt > now + epsilon ||
                    !BattleSnapshot.Finite(region.EndsAt) || region.EndsAt <= now - epsilon ||
                    region.EndsAt <= region.StartedAt || region.EndsAt - region.StartedAt > 5.25 + epsilon ||
                    !BattleSnapshot.Finite(region.NextHit) || region.NextHit < now - epsilon ||
                    region.NextHit < region.IgnitionAt - epsilon || !ExpandedFirePillar.ValidDelay(region.FirstDelay) ||
                    region.Ember && region.ShortenedPrimaryInterval ||
                    region.Ember && region.FirstDelay.HasValue ||
                    !BattleSnapshot.Finite(region.Radius) || region.Radius <= 0 || region.Radius > 2.25 ||
                    region.Ember && (region.Radius < ExpandedFirePillar.EmberRadius || region.Radius > ExpandedFirePillar.EmberRadius * 2.5) ||
                    !BattleSnapshot.Finite(region.Damage) || region.Damage <= 0 || region.Damage > 45 ||
                    !ExpandedFirePillar.ValidDamageBasis(region) ||
                    region.TargetId < 0 || !BattleSnapshot.Finite(region.NextTargetAt) ||
                    region.NextTargetAt < 0 || region.NextTargetAt > now + .5 + epsilon ||
                    !BattleSnapshot.Finite(region.TravelSinceEmber) || region.TravelSinceEmber < 0 || region.TravelSinceEmber >= 1)
                    throw new ArgumentException("Invalid expanded fire pillar region");
            }
        }
    }

    public static class ExpandedFirePillar
    {
        internal const double TimeEpsilon = 1e-9;
        private const double HitInterval = .5;
        private const double MinimumDelay = .05;
        private const float MotionSpeed = 1.2f;
        private const double PredictionLead = .25;
        private const float PredictionLimit = 1;
        private const float PreferredCenterGap = 1.2f;
        private const double CrowdRadius = 1.5;
        private const double CrowdMotionMultiplier = .70;
        private const double DamageCapMultiplier = 2.25;
        private const double InnerRadiusFraction = .40;
        private const double CoreDamageBonus = .08;
        private const double IgnitionDamageBonus = .10;
        internal const double EmberRadius = .65;
        internal const double EmberLifetime = 1;
        private const double EmberDamage = .25;
        internal static readonly ExpandedWeaponProfile Profile = ExpandedWeaponRules.Get("fire_pillar");

        public static int Step(ExpandedFirePillarState state, double now, Vector2 player, bool evolved,
            double damage, double cycle, int amount, double radius, double duration,
            Func<IReadOnlyDictionary<int, Vector2>> getTargets, Action<ExpandedFirePillarHit> hit,
            double delayAdvance = 0, ExpandedFirePillarSettings settings = default, Func<bool>? reserveEmber = null,
            bool shortenedPrimaryInterval = false, Func<double>? nextCycleDuration = null,
            IReadOnlyDictionary<int, Vector2>? previousTargets = null, int level = 0,
            Action<int>? recordOriginalSuppression = null, Action? recordSupportSuppression = null)
        {
            if (!BattleSnapshot.Finite(delayAdvance) || delayAdvance < 0) throw new ArgumentOutOfRangeException(nameof(delayAdvance));
            double? baseDamage = null;
            if (settings.IncreaseCoreDamage || settings.IncreaseIgnitionDamage)
            {
                if (level < 1 || level > 8) throw new ArgumentOutOfRangeException(nameof(level));
                baseDamage = Profile.AtLevel(level).Damage;
            }
            if (state.Evolved != evolved) state.ResetForForm(evolved, now);
            state.Regions.RemoveAll(region => now + TimeEpsilon >= region.EndsAt);
            if (evolved)
                MovePillars(state, now, getTargets(), EmberLifetime *
                    (1 + (settings.ExtendedEmbers ? .20 : 0) + settings.EmberDurationBonus),
                    EmberRadius * (1 + settings.EmberRadiusBonus), reserveEmber, settings.SlowInCrowds,
                    recordSupportSuppression);
            state.LastStep = now;
            ResolveDue(state, now, getTargets(), hit);

            if (state.NextCycle > now + TimeEpsilon) return 0;
            var targets = getTargets();
            var candidates = targets.Where(target => InRange(target.Value, player, Profile.TargetSearchRadius))
                .OrderByDescending(target => InRange(target.Value, player, 4) && settings.IsElite?.Invoke(target.Key) == true)
                .ThenByDescending(target => settings.PreferCrowds ? targets.Count(other => InRange(target.Value, other.Value, CrowdRadius)) : 0)
                .ThenBy(target => Vector2.DistanceSquared(target.Value, player)).ThenBy(target => target.Key).ToArray();
            if (candidates.Length == 0)
            {
                state.WaitingForTarget = true;
                return 0;
            }

            var start = state.WaitingForTarget ? now : state.NextCycle;
            state.WaitingForTarget = false;
            state.Cycle = checked(state.Cycle + 1);
            state.NextCycle = start + (nextCycleDuration?.Invoke() ?? cycle);
            var capacity = 32 - state.Regions.Count(region => !region.Ember);
            var accepted = Math.Min(capacity, amount);
            if (accepted < amount)
            {
                state.SuppressedOriginal = true;
                recordOriginalSuppression?.Invoke(amount - accepted);
            }
            var delay = Math.Max(MinimumDelay, Profile.FirstAttackDelay - delayAdvance);
            var rankedCenters = settings.PredictPlacement || settings.PreferSeparatedCenters
                ? ResolveCenters(candidates, settings, previousTargets)
                : null;
            for (var index = 0; index < accepted; index++)
            {
                var candidateIndex = index % candidates.Length;
                var center = rankedCenters?[candidateIndex] ?? candidates[candidateIndex].Value;
                // Reused targets alternate left and right in world coordinates.
                if (index >= candidates.Length)
                {
                    var reuse = index / candidates.Length;
                    center.X += (float)(Profile.TargetReuseSpacing * ((reuse + 1) / 2) * (reuse % 2 == 1 ? -1 : 1));
                }
                var region = Create(state, center, start, damage, radius, duration, false);
                region.Cycle = state.Cycle;
                region.FirstDelay = delay == Profile.FirstAttackDelay ? (double?)null : delay;
                region.ShortenedPrimaryInterval = shortenedPrimaryInterval;
                region.BaseDamage = baseDamage;
                region.IncreasedCoreDamage = settings.IncreaseCoreDamage;
                region.IncreasedIgnitionDamage = settings.IncreaseIgnitionDamage;
                region.NextHit = region.IgnitionAt;
            }
            return accepted;
        }

        private static Vector2[] ResolveCenters(KeyValuePair<int, Vector2>[] candidates, ExpandedFirePillarSettings settings,
            IReadOnlyDictionary<int, Vector2>? previousTargets)
        {
            var centers = candidates.Select(candidate => settings.PredictPlacement &&
                previousTargets != null && previousTargets.TryGetValue(candidate.Key, out var previous)
                ? PredictCenter(previous, candidate.Value)
                : candidate.Value).ToArray();
            if (!settings.PreferSeparatedCenters || centers.Length == 1) return centers;

            var first = centers[0];
            return centers.Skip(1)
                .OrderByDescending(center => Vector2.DistanceSquared(first, center) >= PreferredCenterGap * PreferredCenterGap)
                .Prepend(first).ToArray();
        }

        private static Vector2 PredictCenter(Vector2 previous, Vector2 current)
        {
            var delta = (current - previous) * (float)(PredictionLead / BattleWorld.FixedStep);
            var length = delta.Length();
            if (length > PredictionLimit) delta *= PredictionLimit / length;
            var predicted = current + delta;
            return BattleSnapshot.Position(predicted.X, predicted.Y) ? predicted : current;
        }

        private static ExpandedFireRegion Create(ExpandedFirePillarState state, Vector2 center,
            double start, double damage, double radius, double duration, bool ember)
        {
            var region = new ExpandedFireRegion
            {
                Id = state.NextId++, Ember = ember, X = center.X, Y = center.Y, Damage = damage, Radius = radius,
                StartedAt = start, EndsAt = start + duration, NextHit = start,
            };
            state.Regions.Add(region);
            return region;
        }

        private static void MovePillars(ExpandedFirePillarState state, double now,
            IReadOnlyDictionary<int, Vector2> targets, double emberDuration, double emberRadius, Func<bool>? reserveEmber,
            bool slowInCrowds, Action? recordSupportSuppression)
        {
            foreach (var pillar in state.Regions.Where(region => !region.Ember).OrderBy(region => region.Id).ToArray())
            {
                if (pillar.NextTargetAt <= now + TimeEpsilon)
                {
                    var due = pillar.NextTargetAt == 0 ? Math.Max(state.LastStep, pillar.StartedAt) : pillar.NextTargetAt;
                    pillar.NextTargetAt = due + HitInterval;
                    pillar.TargetId = targets
                        .OrderByDescending(candidate => targets.Count(other => InRange(candidate.Value, other.Value, CrowdRadius)))
                        .ThenBy(candidate => Vector2.DistanceSquared(candidate.Value, pillar.Center))
                        .ThenBy(candidate => candidate.Key).Select(candidate => candidate.Key).FirstOrDefault();
                }
                if (!targets.TryGetValue(pillar.TargetId, out var target)) continue;
                var from = pillar.Center;
                var delta = target - from;
                var distance = delta.Length();
                if (distance <= 1e-7f) continue;
                var speed = slowInCrowds && targets.Values.Count(position => InRange(from, position, CrowdRadius)) >= 4
                    ? MotionSpeed * CrowdMotionMultiplier : MotionSpeed;
                var movement = Math.Min(distance, speed * (now - Math.Max(state.LastStep, pillar.StartedAt)));
                var direction = delta / distance;
                var center = from + direction * (float)movement;
                pillar.X = center.X;
                pillar.Y = center.Y;
                var firstEmber = 1 - pillar.TravelSinceEmber;
                for (var offset = firstEmber; offset <= movement; offset++)
                {
                    if (state.Regions.Count(region => region.Ember) < 16 && reserveEmber?.Invoke() != false)
                        Create(state, from + direction * (float)offset, now, pillar.Damage * EmberDamage,
                            emberRadius, emberDuration, true);
                    else recordSupportSuppression?.Invoke();
                }
                pillar.TravelSinceEmber = (pillar.TravelSinceEmber + movement) % 1;
            }
        }

        internal static bool ValidDelay(double? delay) => !delay.HasValue ||
            BattleSnapshot.Finite(delay.Value) && delay.Value >= MinimumDelay && delay.Value <= Profile.FirstAttackDelay;

        internal static bool ValidDamageBasis(ExpandedFireRegion region)
        {
            var hasBonus = region.IncreasedCoreDamage || region.IncreasedIgnitionDamage;
            if (!hasBonus) return !region.BaseDamage.HasValue;
            return !region.Ember && region.BaseDamage is double basis &&
                (basis == Profile.AtLevel(1).Damage || basis == Profile.AtLevel(4).Damage || basis == Profile.AtLevel(8).Damage) &&
                region.Damage <= basis * DamageCapMultiplier;
        }

        private static void ResolveDue(ExpandedFirePillarState state, double now,
            IReadOnlyDictionary<int, Vector2> targets, Action<ExpandedFirePillarHit> hit)
        {
            var due = state.Regions.Where(region => region.NextHit <= now + TimeEpsilon && region.NextHit < region.EndsAt)
                .OrderBy(region => region.Id)
                .Select(region => (Region: region, First: Math.Abs(region.IgnitionAt - region.NextHit) <= TimeEpsilon))
                .ToArray();
            if (due.Length == 0) return;
            foreach (var (region, _) in due) region.NextHit += region.ShortenedPrimaryInterval ? HitInterval * .96 : HitInterval;
            var hits = new List<(ExpandedFireRegion Region, int Id, Vector2 Position, double Damage)>();
            foreach (var target in targets)
            {
                foreach (var ember in new[] { false, true })
                {
                    ExpandedFireRegion? winner = null;
                    var damage = 0d;
                    foreach (var (region, first) in due)
                    {
                        if (region.Ember != ember || !InRange(region.Center, target.Value, region.Radius)) continue;
                        var candidate = HitDamage(region, target.Value, first);
                        if (winner != null && candidate <= damage) continue;
                        winner = region;
                        damage = candidate;
                    }
                    if (winner != null) hits.Add((winner, target.Key, target.Value, damage));
                }
            }
            foreach (var candidate in hits.OrderBy(entry => entry.Region.Id).ThenBy(entry => entry.Id))
                hit(new ExpandedFirePillarHit(candidate.Id, candidate.Position, candidate.Damage, candidate.Region.Ember, candidate.Region.Cycle));
        }

        private static double HitDamage(ExpandedFireRegion region, Vector2 position, bool first)
        {
            if (region.BaseDamage is not double basis) return region.Damage;
            var bonus = first && region.IncreasedIgnitionDamage ? IgnitionDamageBonus : 0;
            var innerRadius = (float)(region.Radius * InnerRadiusFraction);
            if (region.IncreasedCoreDamage && Vector2.DistanceSquared(region.Center, position) <= innerRadius * innerRadius)
                bonus += CoreDamageBonus;
            return Math.Min(basis * DamageCapMultiplier, region.Damage + basis * bonus);
        }

        private static bool InRange(Vector2 left, Vector2 right, double radius) =>
            Vector2.DistanceSquared(left, right) <= radius * radius + TimeEpsilon;
    }
}
