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

        public ExpandedFirePillarSettings(bool preferCrowds, Func<int, bool>? isElite, bool extendedEmbers)
        {
            PreferCrowds = preferCrowds;
            IsElite = isElite;
            ExtendedEmbers = extendedEmbers;
        }
    }

    public readonly struct ExpandedFirePillarHit
    {
        public int TargetId { get; }
        public Vector2 Position { get; }
        public double Damage { get; }
        public bool IsEmber { get; }

        internal ExpandedFirePillarHit(int targetId, Vector2 position, double damage, bool isEmber)
        {
            TargetId = targetId;
            Position = position;
            Damage = damage;
            IsEmber = isEmber;
        }
    }

    [DataContract]
    public sealed class ExpandedFireRegion
    {
        [DataMember(IsRequired = true)] public long Id { get; set; }
        [DataMember(IsRequired = true)] public bool Ember { get; set; }
        [DataMember(IsRequired = true)] public float X { get; set; }
        [DataMember(IsRequired = true)] public float Y { get; set; }
        [DataMember(IsRequired = true)] public double StartedAt { get; set; }
        [DataMember(IsRequired = true)] public double EndsAt { get; set; }
        [DataMember(IsRequired = true)] public double NextHit { get; set; }
        [DataMember(EmitDefaultValue = false)] public double? FirstDelay { get; set; }
        [DataMember(EmitDefaultValue = false)] public bool ShortenedPrimaryInterval { get; set; }
        [DataMember(IsRequired = true)] public double Damage { get; set; }
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
        [DataMember(IsRequired = true)] public double NextCycle { get; set; }
        [DataMember(IsRequired = true)] public double LastStep { get; set; }
        [DataMember(IsRequired = true)] public List<ExpandedFireRegion> Regions { get; set; } = new List<ExpandedFireRegion>();

        public ExpandedFirePillarState Copy() => new ExpandedFirePillarState
        {
            Evolved = Evolved, WaitingForTarget = WaitingForTarget, SuppressedOriginal = SuppressedOriginal,
            NextId = NextId, NextCycle = NextCycle, LastStep = LastStep,
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
            if (Evolved != evolved || NextId < 1 || !BattleSnapshot.Finite(LastStep) || LastStep < 0 ||
                LastStep > now + epsilon || !BattleSnapshot.Finite(NextCycle) || NextCycle < 0 ||
                NextCycle > now + 3 + epsilon || Regions == null || Regions.Count > 48 ||
                Regions.Any(region => region == null) ||
                Regions.Count(region => region.Ember) > 16 || Regions.Count(region => !region.Ember) > 32 ||
                Regions.Select(region => region.Id).Distinct().Count() != Regions.Count)
                throw new ArgumentException("Invalid expanded fire pillar clock");

            foreach (var region in Regions)
            {
                if (region.Id < 1 || region.Id >= NextId || (region.Ember && !Evolved) ||
                    !BattleSnapshot.Position(region.X, region.Y) ||
                    !BattleSnapshot.Finite(region.StartedAt) || region.StartedAt < 0 || region.StartedAt > now + epsilon ||
                    !BattleSnapshot.Finite(region.EndsAt) || region.EndsAt <= now - epsilon ||
                    region.EndsAt <= region.StartedAt || region.EndsAt - region.StartedAt > 5.25 + epsilon ||
                    !BattleSnapshot.Finite(region.NextHit) || region.NextHit < now - epsilon ||
                    region.NextHit < region.IgnitionAt - epsilon || !ExpandedFirePillar.ValidDelay(region.FirstDelay) ||
                    region.Ember && region.ShortenedPrimaryInterval ||
                    region.Ember && region.FirstDelay.HasValue ||
                    !BattleSnapshot.Finite(region.Radius) || region.Radius <= 0 || region.Radius > 2.25 ||
                    !BattleSnapshot.Finite(region.Damage) || region.Damage <= 0 || region.Damage > 45 ||
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
        private const double EmberRadius = .65;
        private const double EmberLifetime = 1;
        private const double EmberDamage = .25;
        internal static readonly ExpandedWeaponProfile Profile = ExpandedWeaponRules.Get("fire_pillar");

        public static int Step(ExpandedFirePillarState state, double now, Vector2 player, bool evolved,
            double damage, double cycle, int amount, double radius, double duration,
            Func<IReadOnlyDictionary<int, Vector2>> getTargets, Action<ExpandedFirePillarHit> hit,
            double delayAdvance = 0, ExpandedFirePillarSettings settings = default, Func<bool>? reserveEmber = null,
            bool shortenedPrimaryInterval = false, Func<double>? nextCycleDuration = null)
        {
            if (!BattleSnapshot.Finite(delayAdvance) || delayAdvance < 0) throw new ArgumentOutOfRangeException(nameof(delayAdvance));
            if (state.Evolved != evolved) state.ResetForForm(evolved, now);
            state.Regions.RemoveAll(region => now + TimeEpsilon >= region.EndsAt);
            if (evolved) MovePillars(state, now, getTargets(), settings.ExtendedEmbers ? 1.20 * EmberLifetime : EmberLifetime,
                reserveEmber);
            state.LastStep = now;
            ResolveDue(state, now, getTargets(), hit);

            if (state.NextCycle > now + TimeEpsilon) return 0;
            var targets = getTargets();
            var candidates = targets.Where(target => InRange(target.Value, player, Profile.TargetSearchRadius))
                .OrderByDescending(target => InRange(target.Value, player, 4) && settings.IsElite?.Invoke(target.Key) == true)
                .ThenByDescending(target => settings.PreferCrowds ? targets.Count(other => InRange(target.Value, other.Value, 1.5)) : 0)
                .ThenBy(target => Vector2.DistanceSquared(target.Value, player)).ThenBy(target => target.Key).ToArray();
            if (candidates.Length == 0)
            {
                state.WaitingForTarget = true;
                return 0;
            }

            var start = state.WaitingForTarget ? now : state.NextCycle;
            state.WaitingForTarget = false;
            state.NextCycle = start + (nextCycleDuration?.Invoke() ?? cycle);
            var capacity = 32 - state.Regions.Count(region => !region.Ember);
            var accepted = Math.Min(capacity, amount);
            state.SuppressedOriginal |= accepted != amount;
            var delay = Math.Max(MinimumDelay, Profile.FirstAttackDelay - delayAdvance);
            for (var index = 0; index < accepted; index++)
            {
                var center = candidates[index % candidates.Length].Value;
                // Reused targets alternate left and right in world coordinates.
                if (index >= candidates.Length)
                {
                    var reuse = index / candidates.Length;
                    center.X += (float)(Profile.TargetReuseSpacing * ((reuse + 1) / 2) * (reuse % 2 == 1 ? -1 : 1));
                }
                var region = Create(state, center, start, damage, radius, duration, false);
                region.FirstDelay = delay == Profile.FirstAttackDelay ? (double?)null : delay;
                region.ShortenedPrimaryInterval = shortenedPrimaryInterval;
                region.NextHit = region.IgnitionAt;
            }
            return accepted;
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
            IReadOnlyDictionary<int, Vector2> targets, double emberDuration, Func<bool>? reserveEmber)
        {
            foreach (var pillar in state.Regions.Where(region => !region.Ember).OrderBy(region => region.Id).ToArray())
            {
                if (pillar.NextTargetAt <= now + TimeEpsilon)
                {
                    var due = pillar.NextTargetAt == 0 ? Math.Max(state.LastStep, pillar.StartedAt) : pillar.NextTargetAt;
                    pillar.NextTargetAt = due + HitInterval;
                    pillar.TargetId = targets
                        .OrderByDescending(candidate => targets.Count(other => InRange(candidate.Value, other.Value, 1.5)))
                        .ThenBy(candidate => Vector2.DistanceSquared(candidate.Value, pillar.Center))
                        .ThenBy(candidate => candidate.Key).Select(candidate => candidate.Key).FirstOrDefault();
                }
                if (!targets.TryGetValue(pillar.TargetId, out var target)) continue;
                var from = pillar.Center;
                var delta = target - from;
                var distance = delta.Length();
                if (distance <= 1e-7f) continue;
                var movement = Math.Min(distance, MotionSpeed * (now - Math.Max(state.LastStep, pillar.StartedAt)));
                var direction = delta / distance;
                var center = from + direction * (float)movement;
                pillar.X = center.X;
                pillar.Y = center.Y;
                var firstEmber = 1 - pillar.TravelSinceEmber;
                for (var offset = firstEmber; offset <= movement; offset++)
                {
                    if (state.Regions.Count(region => region.Ember) < 16 && reserveEmber?.Invoke() != false)
                        Create(state, from + direction * (float)offset, now, pillar.Damage * EmberDamage,
                            EmberRadius, emberDuration, true);
                }
                pillar.TravelSinceEmber = (pillar.TravelSinceEmber + movement) % 1;
            }
        }

        internal static bool ValidDelay(double? delay) => !delay.HasValue ||
            BattleSnapshot.Finite(delay.Value) && delay.Value >= MinimumDelay && delay.Value <= Profile.FirstAttackDelay;

        private static void ResolveDue(ExpandedFirePillarState state, double now,
            IReadOnlyDictionary<int, Vector2> targets, Action<ExpandedFirePillarHit> hit)
        {
            var due = state.Regions.Where(region => region.NextHit <= now + TimeEpsilon && region.NextHit < region.EndsAt)
                .OrderBy(region => region.Id).ToArray();
            if (due.Length == 0) return;
            foreach (var region in due) region.NextHit += region.ShortenedPrimaryInterval ? HitInterval * .96 : HitInterval;
            var hits = new List<(ExpandedFireRegion Region, int Id, Vector2 Position)>();
            foreach (var target in targets.OrderBy(target => target.Key))
                foreach (var category in new[] { false, true })
                {
                    var chosen = due.Where(region => region.Ember == category && InRange(region.Center, target.Value, region.Radius))
                        .OrderByDescending(region => region.Damage).ThenBy(region => region.Id).FirstOrDefault();
                    if (chosen != null) hits.Add((chosen, target.Key, target.Value));
                }
            foreach (var candidate in hits.OrderBy(entry => entry.Region.Id).ThenBy(entry => entry.Id))
                hit(new ExpandedFirePillarHit(candidate.Id, candidate.Position, candidate.Region.Damage, candidate.Region.Ember));
        }

        private static bool InRange(Vector2 left, Vector2 right, double radius) =>
            Vector2.DistanceSquared(left, right) <= radius * radius + TimeEpsilon;
    }
}
