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
        internal bool CloseRangeFire { get; }
        internal bool LeaveClosingEmber { get; }
        internal bool ClosingAshHit { get; }
        internal bool RepeatedHeat { get; }
        internal bool ExtendOnEliteKill { get; }
        internal bool NormalKillBurst { get; }
        internal bool EliteReignition { get; }
        internal bool CyclePulse { get; }
        internal double RegionShapeBonus { get; }

        public ExpandedFirePillarSettings(bool preferCrowds, Func<int, bool>? isElite, bool extendedEmbers,
            double emberRadiusBonus = 0, bool predictPlacement = false, bool preferSeparatedCenters = false,
            bool slowInCrowds = false, bool increaseCoreDamage = false, bool increaseIgnitionDamage = false,
            double emberDurationBonus = 0, bool closeRangeFire = false, bool leaveClosingEmber = false,
            bool closingAshHit = false, double regionShapeBonus = 0, bool repeatedHeat = false,
            bool extendOnEliteKill = false, bool normalKillBurst = false, bool eliteReignition = false, bool cyclePulse = false)
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
            CloseRangeFire = closeRangeFire;
            LeaveClosingEmber = leaveClosingEmber;
            ClosingAshHit = closingAshHit;
            RepeatedHeat = repeatedHeat;
            ExtendOnEliteKill = extendOnEliteKill;
            NormalKillBurst = normalKillBurst;
            EliteReignition = eliteReignition;
            CyclePulse = cyclePulse;
            if (!BattleSnapshot.Finite(regionShapeBonus) || regionShapeBonus < 0 || regionShapeBonus > 1.5)
                throw new ArgumentOutOfRangeException(nameof(regionShapeBonus));
            RegionShapeBonus = regionShapeBonus;
        }
    }

    public readonly struct ExpandedFirePillarHit
    {
        public int TargetId { get; }
        public Vector2 Position { get; }
        public double Damage { get; }
        public bool IsEmber { get; }
        public long Cycle { get; }
        public double OriginalBaseDamage { get; }
        public int TargetOrdinal { get; }

        internal ExpandedFirePillarHit(int targetId, Vector2 position, double damage, bool isEmber, long cycle,
            double originalBaseDamage, int targetOrdinal)
        {
            TargetId = targetId;
            Position = position;
            Damage = damage;
            IsEmber = isEmber;
            Cycle = cycle;
            OriginalBaseDamage = originalBaseDamage;
            TargetOrdinal = targetOrdinal;
        }
    }

    public enum ExpandedFireSupportKind { EvolvedEmber, ClosingEmber, CloseRange, EliteReignition }

    [DataContract]
    public sealed class ExpandedFireContact
    {
        [DataMember(IsRequired = true)] public int TargetId { get; set; }
        [DataMember(IsRequired = true)] public double LastDueAt { get; set; }
        [DataMember(IsRequired = true)] public int ConsecutiveHits { get; set; }

        internal ExpandedFireContact Copy() => (ExpandedFireContact)MemberwiseClone();
    }

    [DataContract]
    public sealed class ExpandedFireRegion
    {
        [DataMember(IsRequired = true)] public long Id { get; set; }
        [DataMember(IsRequired = true)] public bool Ember { get; set; }
        [DataMember(EmitDefaultValue = false)] public ExpandedFireSupportKind SupportKind { get; set; }
        [DataMember(EmitDefaultValue = false)] public long Cycle { get; set; }
        [DataMember(IsRequired = true)] public float X { get; set; }
        [DataMember(IsRequired = true)] public float Y { get; set; }
        [DataMember(IsRequired = true)] public double StartedAt { get; set; }
        [DataMember(IsRequired = true)] public double EndsAt { get; set; }
        [DataMember(IsRequired = true)] public double NextHit { get; set; }
        [DataMember(EmitDefaultValue = false)] public double? FirstDelay { get; set; }
        [DataMember(EmitDefaultValue = false)] public bool ShortenedPrimaryInterval { get; set; }
        [DataMember(IsRequired = true)] public double Damage { get; set; }
        [DataMember(EmitDefaultValue = false)] public double OriginalBaseDamage { get; set; }
        [DataMember(EmitDefaultValue = false)] public double? BaseDamage { get; set; }
        [DataMember(EmitDefaultValue = false)] public bool IncreasedCoreDamage { get; set; }
        [DataMember(EmitDefaultValue = false)] public bool IncreasedIgnitionDamage { get; set; }
        [DataMember(EmitDefaultValue = false)] public bool RepeatedHeat { get; set; }
        [DataMember(EmitDefaultValue = false)] public List<ExpandedFireContact>? Contacts { get; set; }
        [DataMember(EmitDefaultValue = false)] public double? NativeDuration { get; set; }
        [DataMember(EmitDefaultValue = false)] public bool EliteExtensionUsed { get; set; }
        [DataMember(IsRequired = true)] public double Radius { get; set; }
        [DataMember(EmitDefaultValue = false)] public int TargetId { get; set; }
        [DataMember(EmitDefaultValue = false)] public double NextTargetAt { get; set; }
        [DataMember(EmitDefaultValue = false)] public double TravelSinceEmber { get; set; }

        internal Vector2 Center => new Vector2(X, Y);
        public double IgnitionAt => StartedAt + (Ember ? 0 : FirstDelay ?? ExpandedFirePillar.Profile.FirstAttackDelay);
        internal double Interval => ShortenedPrimaryInterval ? ExpandedFirePillar.HitInterval * .96 : ExpandedFirePillar.HitInterval;
        internal ExpandedFireRegion Copy()
        {
            var copy = (ExpandedFireRegion)MemberwiseClone();
            copy.Contacts = Contacts?.Select(contact => contact.Copy()).ToList();
            return copy;
        }
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
        [DataMember(EmitDefaultValue = false)] public double? NextClosingEmberAt { get; set; }
        [DataMember(EmitDefaultValue = false)] public double? NextClosingAshAt { get; set; }
        [DataMember(EmitDefaultValue = false)] public double? NextCloseRangeAt { get; set; }
        [DataMember(EmitDefaultValue = false)] public double? NextEliteExtensionAt { get; set; }
        [DataMember(EmitDefaultValue = false)] public int NormalBurstKills { get; set; }
        [DataMember(EmitDefaultValue = false)] public double? NextNormalBurstAt { get; set; }
        [DataMember(EmitDefaultValue = false)] public double? NextEliteReignitionAt { get; set; }
        [DataMember(EmitDefaultValue = false)] public double? NextCyclePulseAt { get; set; }
        [DataMember(IsRequired = true)] public List<ExpandedFireRegion> Regions { get; set; } = new List<ExpandedFireRegion>();

        public ExpandedFirePillarState Copy() => new ExpandedFirePillarState
        {
            Evolved = Evolved, WaitingForTarget = WaitingForTarget, SuppressedOriginal = SuppressedOriginal,
            NextId = NextId, NextCycle = NextCycle, LastStep = LastStep, Cycle = Cycle,
            NextClosingEmberAt = NextClosingEmberAt, NextClosingAshAt = NextClosingAshAt,
            NextCloseRangeAt = NextCloseRangeAt, NextEliteExtensionAt = NextEliteExtensionAt,
            NormalBurstKills = NormalBurstKills, NextNormalBurstAt = NextNormalBurstAt,
            NextEliteReignitionAt = NextEliteReignitionAt, NextCyclePulseAt = NextCyclePulseAt,
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
                !ExpandedFirePillar.ValidCooldown(NextClosingEmberAt, now, 2) ||
                !ExpandedFirePillar.ValidCooldown(NextClosingAshAt, now, 2) ||
                !ExpandedFirePillar.ValidCooldown(NextCloseRangeAt, now, 4) ||
                !ExpandedFirePillar.ValidCooldown(NextEliteExtensionAt, now, 4) ||
                !ExpandedFirePillar.ValidCooldown(NextNormalBurstAt, now, 2) ||
                !ExpandedFirePillar.ValidCooldown(NextEliteReignitionAt, now, 5) ||
                !ExpandedFirePillar.ValidCooldown(NextCyclePulseAt, now, 4) ||
                NormalBurstKills < 0 || NormalBurstKills >= 8 || NextNormalBurstAt.HasValue && NormalBurstKills != 0 ||
                Regions.Any(region => region == null) ||
                Regions.Count(region => region.Ember) > 16 || Regions.Count(region => !region.Ember) > 32 ||
                Regions.Select(region => region.Id).Distinct().Count() != Regions.Count)
                throw new ArgumentException("Invalid expanded fire pillar clock");

            foreach (var region in Regions)
            {
                if (region.Id < 1 || region.Id >= NextId || region.Cycle < 0 || region.Cycle > Cycle ||
                    !ExpandedFirePillar.ValidRegionKind(region, evolved) ||
                    !BattleSnapshot.Position(region.X, region.Y) ||
                    !BattleSnapshot.Finite(region.StartedAt) || region.StartedAt < 0 || region.StartedAt > now + epsilon ||
                    !BattleSnapshot.Finite(region.EndsAt) || region.EndsAt <= now - epsilon ||
                    region.EndsAt <= region.StartedAt || region.EndsAt - region.StartedAt > 5.25 + epsilon ||
                    !BattleSnapshot.Finite(region.NextHit) || region.NextHit < now - epsilon ||
                    region.NextHit < region.IgnitionAt - epsilon || !ExpandedFirePillar.ValidDelay(region.FirstDelay) ||
                    region.Ember && region.ShortenedPrimaryInterval ||
                    region.Ember && region.FirstDelay.HasValue ||
                    !BattleSnapshot.Finite(region.Radius) || region.Radius <= 0 || region.Radius > 2.25 ||
                    region.Ember && (region.Radius < ExpandedFirePillar.SupportRadius(region.SupportKind) ||
                        region.Radius > ExpandedFirePillar.SupportRadius(region.SupportKind) * 2.5) ||
                    !BattleSnapshot.Finite(region.Damage) || region.Damage <= 0 || region.Damage > 45 ||
                    !ExpandedFirePillar.ValidDamageBasis(region) ||
                    !ExpandedFirePillar.ValidContacts(region, now) || !ExpandedFirePillar.ValidDuration(region) ||
                    !ExpandedFirePillar.ValidOriginalBaseDamage(region, evolved) ||
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
        internal const double HitInterval = .5;
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
            Action<int>? recordOriginalSuppression = null, Action? recordSupportSuppression = null,
            double conditionalRegionBonus = 0, Func<int, bool>? isElite = null,
            Func<ExpandedFirePillarHit, double?>? applyOriginalHit = null)
        {
            if (!BattleSnapshot.Finite(delayAdvance) || delayAdvance < 0) throw new ArgumentOutOfRangeException(nameof(delayAdvance));
            if (level < 0 || level > 8 || evolved && level != 0 && level != 8)
                throw new ArgumentOutOfRangeException(nameof(level));
            if (!BattleSnapshot.Finite(conditionalRegionBonus) || conditionalRegionBonus < 0 ||
                settings.RegionShapeBonus + conditionalRegionBonus > 1.5 || settings.EmberRadiusBonus + conditionalRegionBonus > 1.5)
                throw new ArgumentOutOfRangeException(nameof(conditionalRegionBonus));
            var originalBaseDamage = level == 0 ? 0 : Profile.AtLevel(level).Damage;
            double? baseDamage = null;
            if (settings.IncreaseCoreDamage || settings.IncreaseIgnitionDamage || settings.RepeatedHeat || settings.ExtendOnEliteKill)
            {
                if (level < 1 || level > 8) throw new ArgumentOutOfRangeException(nameof(level));
                baseDamage = originalBaseDamage;
            }
            if (state.Evolved != evolved) state.ResetForForm(evolved, now);
            if (state.NextClosingEmberAt <= now + TimeEpsilon) state.NextClosingEmberAt = null;
            if (state.NextClosingAshAt <= now + TimeEpsilon) state.NextClosingAshAt = null;
            if (state.NextCloseRangeAt <= now + TimeEpsilon) state.NextCloseRangeAt = null;
            if (state.NextEliteExtensionAt <= now + TimeEpsilon) state.NextEliteExtensionAt = null;
            if (state.NextNormalBurstAt <= now + TimeEpsilon) state.NextNormalBurstAt = null;
            if (state.NextEliteReignitionAt <= now + TimeEpsilon) state.NextEliteReignitionAt = null;
            if (state.NextCyclePulseAt <= now + TimeEpsilon) state.NextCyclePulseAt = null;
            var regionShapeBonus = settings.RegionShapeBonus + conditionalRegionBonus;
            EndRegions(state, now, settings, regionShapeBonus, getTargets, hit, reserveEmber, recordSupportSuppression);
            if (evolved)
                MovePillars(state, now, getTargets(), EmberLifetime *
                    (1 + (settings.ExtendedEmbers ? .20 : 0) + settings.EmberDurationBonus),
                    EmberRadius * (1 + settings.EmberRadiusBonus + conditionalRegionBonus), reserveEmber, settings.SlowInCrowds,
                    recordSupportSuppression);
            state.LastStep = now;
            ResolveDue(state, now, getTargets, hit, isElite, settings, regionShapeBonus,
                reserveEmber, recordSupportSuppression, applyOriginalHit);

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
                region.OriginalBaseDamage = originalBaseDamage;
                region.FirstDelay = delay == Profile.FirstAttackDelay ? (double?)null : delay;
                region.ShortenedPrimaryInterval = shortenedPrimaryInterval;
                region.BaseDamage = baseDamage;
                region.IncreasedCoreDamage = settings.IncreaseCoreDamage;
                region.IncreasedIgnitionDamage = settings.IncreaseIgnitionDamage;
                region.RepeatedHeat = settings.RepeatedHeat;
                region.NativeDuration = settings.ExtendOnEliteKill ? Profile.AtLevel(level).Duration : (double?)null;
                region.NextHit = region.IgnitionAt;
            }
            if (settings.CloseRangeFire && state.Cycle % 5 == 0 && !state.NextCloseRangeAt.HasValue)
            {
                var nearby = candidates.Where(candidate => InRange(candidate.Value, player, 1))
                    .OrderBy(candidate => Vector2.DistanceSquared(candidate.Value, player))
                    .ThenBy(candidate => candidate.Key).Take(1).ToArray();
                if (nearby.Length > 0 && ReserveSupport(state, reserveEmber, recordSupportSuppression))
                {
                    SpawnSupport(state, nearby[0].Value, start, damage * .35,
                        ExpandedFireSupportKind.CloseRange, regionShapeBonus, settings.EmberDurationBonus);
                    state.NextCloseRangeAt = start + 4;
                }
            }
            if (settings.CyclePulse && state.Cycle % 5 == 0 && !state.NextCyclePulseAt.HasValue)
            {
                var active = state.Regions.Where(region => !region.Ember).OrderBy(region => region.Id).Take(3).ToArray();
                if (active.Length > 0 && ReserveSupport(state, reserveEmber, recordSupportSuppression))
                {
                    var used = new HashSet<int>();
                    foreach (var region in active)
                        HitSupportCircle(region.Center, region.Radius, region.Damage * .25, getTargets, hit, used);
                    state.NextCyclePulseAt = now + 4;
                }
            }
            return accepted;
        }

        private static void EndRegions(ExpandedFirePillarState state, double now, ExpandedFirePillarSettings settings,
            double regionShapeBonus,
            Func<IReadOnlyDictionary<int, Vector2>> getTargets, Action<ExpandedFirePillarHit> hit,
            Func<bool>? reserveEmber, Action? recordSupportSuppression)
        {
            if (!settings.LeaveClosingEmber && !settings.ClosingAshHit)
            {
                state.Regions.RemoveAll(region => now + TimeEpsilon >= region.EndsAt);
                return;
            }
            var ended = state.Regions.Where(region => now + TimeEpsilon >= region.EndsAt)
                .OrderBy(region => region.EndsAt).ThenBy(region => region.Id).ToArray();
            foreach (var region in ended) state.Regions.Remove(region);
            foreach (var region in ended)
            {
                if (region.Ember) continue;
                if (settings.LeaveClosingEmber && !state.NextClosingEmberAt.HasValue &&
                    ReserveSupport(state, reserveEmber, recordSupportSuppression))
                {
                    SpawnSupport(state, region.Center, region.EndsAt, region.Damage * .25,
                        ExpandedFireSupportKind.ClosingEmber, regionShapeBonus, settings.EmberDurationBonus);
                    state.NextClosingEmberAt = region.EndsAt + 2;
                }
                if (!settings.ClosingAshHit || state.NextClosingAshAt.HasValue) continue;
                var radius = 1 + regionShapeBonus;
                var targets = getTargets().Where(target => InRange(target.Value, region.Center, radius))
                    .OrderBy(target => target.Key).ToArray();
                if (targets.Length == 0 || !ReserveSupport(state, reserveEmber, recordSupportSuppression)) continue;
                foreach (var target in targets)
                {
                    if (!getTargets().ContainsKey(target.Key)) continue;
                    hit(new ExpandedFirePillarHit(target.Key, target.Value, region.Damage * .25, true, 0, 0, 0));
                }
                state.NextClosingAshAt = region.EndsAt + 2;
            }
        }

        private static void SpawnSupport(ExpandedFirePillarState state, Vector2 center, double at,
            double damage, ExpandedFireSupportKind kind, double radiusBonus, double durationBonus)
        {
            var region = Create(state, center, at, damage, SupportRadius(kind) * (1 + radiusBonus), 1 + durationBonus, true);
            region.SupportKind = kind;
        }

        private static bool ReserveSupport(ExpandedFirePillarState state, Func<bool>? reserve, Action? suppressed)
        {
            if (state.Regions.Count(region => region.Ember) < 16 && reserve?.Invoke() != false) return true;
            suppressed?.Invoke();
            return false;
        }

        internal static bool ValidCooldown(double? at, double now, double duration) => !at.HasValue ||
            BattleSnapshot.Finite(at.Value) && at.Value >= now - TimeEpsilon && at.Value <= now + duration + TimeEpsilon;

        internal static bool ValidRegionKind(ExpandedFireRegion region, bool evolved)
        {
            if (!region.Ember) return region.SupportKind == ExpandedFireSupportKind.EvolvedEmber;
            if (region.Cycle != 0) return false;
            return region.SupportKind switch
            {
                ExpandedFireSupportKind.EvolvedEmber => evolved,
                ExpandedFireSupportKind.ClosingEmber or ExpandedFireSupportKind.CloseRange or
                    ExpandedFireSupportKind.EliteReignition => region.TargetId == 0 &&
                    region.NextTargetAt == 0 && region.TravelSinceEmber == 0,
                _ => false,
            };
        }

        internal static double SupportRadius(ExpandedFireSupportKind kind) =>
            kind == ExpandedFireSupportKind.EvolvedEmber ? EmberRadius : .7;

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
            var hasBonus = region.IncreasedCoreDamage || region.IncreasedIgnitionDamage || region.RepeatedHeat || region.NativeDuration.HasValue;
            if (!hasBonus) return !region.BaseDamage.HasValue;
            return !region.Ember && region.BaseDamage is double basis &&
                (basis == Profile.AtLevel(1).Damage || basis == Profile.AtLevel(4).Damage || basis == Profile.AtLevel(8).Damage) &&
                region.Damage <= basis * DamageCapMultiplier;
        }

        internal static bool ValidOriginalBaseDamage(ExpandedFireRegion region, bool evolved)
        {
            var damage = region.OriginalBaseDamage;
            if (damage == 0) return true;
            return !region.Ember && (!region.BaseDamage.HasValue || damage == region.BaseDamage.Value) &&
                (damage == Profile.AtLevel(8).Damage || !evolved &&
                    (damage == Profile.AtLevel(1).Damage || damage == Profile.AtLevel(4).Damage));
        }

        internal static bool ValidContacts(ExpandedFireRegion region, double now)
        {
            return region.Contacts == null || region.RepeatedHeat && region.Contacts.Count > 0 && region.Contacts.Count <= 120 &&
                region.Contacts.All(contact => contact != null && contact.TargetId > 0 && contact.ConsecutiveHits >= 1 &&
                    contact.ConsecutiveHits <= 3 && BattleSnapshot.Finite(contact.LastDueAt) &&
                    contact.LastDueAt >= region.IgnitionAt - TimeEpsilon && contact.LastDueAt <= now + TimeEpsilon &&
                    contact.LastDueAt + 2 * region.Interval >= region.NextHit - TimeEpsilon) &&
                region.Contacts.Select(contact => contact.TargetId).Distinct().Count() == region.Contacts.Count;
        }

        internal static bool ValidDuration(ExpandedFireRegion region)
        {
            if (region.NativeDuration is not double basis) return !region.EliteExtensionUsed;
            return !region.Ember && (basis == Profile.AtLevel(1).Duration || basis == Profile.AtLevel(2).Duration || basis == Profile.AtLevel(8).Duration) &&
                region.EndsAt <= region.StartedAt + basis * 1.75 + TimeEpsilon;
        }

        private static void ResolveDue(ExpandedFirePillarState state, double now,
            Func<IReadOnlyDictionary<int, Vector2>> targets, Action<ExpandedFirePillarHit> hit, Func<int, bool>? isElite,
            ExpandedFirePillarSettings settings, double regionShapeBonus, Func<bool>? reserve, Action? suppressed,
            Func<ExpandedFirePillarHit, double?>? applyOriginalHit)
        {
            var due = state.Regions.Where(region => region.NextHit <= now + TimeEpsilon && region.NextHit < region.EndsAt)
                .OrderBy(region => region.Id)
                .Select(region => (Region: region, At: region.NextHit))
                .Select(item => (item.Region, item.At, First: Math.Abs(item.Region.IgnitionAt - item.At) <= TimeEpsilon))
                .ToArray();
            if (due.Length == 0) return;
            foreach (var (region, _, _) in due) region.NextHit += region.Interval;
            var groups = due.Select(item => (item.Region.Ember, item.Region.SupportKind)).Distinct().ToArray();
            var hits = new List<(ExpandedFireRegion Region, int Id, Vector2 Position, double Damage, double At, int Streak)>();
            foreach (var target in targets())
            {
                foreach (var group in groups)
                {
                    ExpandedFireRegion? winner = null;
                    var damage = 0d;
                    var dueAt = 0d;
                    var streak = 1;
                    foreach (var (region, at, first) in due)
                    {
                        if ((region.Ember, region.SupportKind) != group ||
                            !InRange(region.Center, target.Value, region.Radius)) continue;
                        var contact = region.Contacts?.Find(entry => entry.TargetId == target.Key);
                        var currentStreak = contact != null && Math.Abs(contact.LastDueAt + region.Interval - at) <= TimeEpsilon
                            ? Math.Min(3, contact.ConsecutiveHits + 1) : 1;
                        var candidate = HitDamage(region, target.Value, first, currentStreak);
                        if (winner != null && candidate <= damage) continue;
                        winner = region;
                        damage = candidate;
                        dueAt = at;
                        streak = currentStreak;
                    }
                    if (winner != null) hits.Add((winner, target.Key, target.Value, damage, dueAt, streak));
                }
            }
            var regionId = 0L;
            var targetOrdinal = 0;
            foreach (var candidate in hits.OrderBy(entry => entry.Region.Id).ThenBy(entry => entry.Id))
            {
                if (!targets().ContainsKey(candidate.Id)) continue;
                var region = candidate.Region;
                if (region.Id != regionId)
                {
                    regionId = region.Id;
                    targetOrdinal = 0;
                }
                var trackKills = !region.Ember && (region.NativeDuration.HasValue || settings.NormalKillBurst || settings.EliteReignition);
                var elite = trackKills && isElite?.Invoke(candidate.Id) == true;
                var contact = new ExpandedFirePillarHit(candidate.Id, candidate.Position, candidate.Damage, region.Ember,
                    region.Cycle, region.Ember ? 0 : region.OriginalBaseDamage, region.Ember ? 0 : targetOrdinal + 1);
                double? acceptedDamage = contact.Damage;
                if (!region.Ember && applyOriginalHit != null) acceptedDamage = applyOriginalHit(contact);
                else hit(contact);
                if (!acceptedDamage.HasValue) continue;
                if (!region.Ember) targetOrdinal++;
                if (targets().ContainsKey(candidate.Id))
                {
                    if (region.RepeatedHeat) RecordContact(region, candidate.Id, candidate.At, candidate.Streak);
                }
                else if (trackKills)
                    OnOriginalKill(state, region, candidate.Position, now, elite, acceptedDamage.Value, settings,
                        regionShapeBonus, targets, hit, reserve, suppressed);
            }
            foreach (var (region, at, _) in due)
            {
                region.Contacts?.RemoveAll(contact => contact.LastDueAt < at - TimeEpsilon ||
                    !targets().ContainsKey(contact.TargetId));
                if (region.Contacts?.Count == 0) region.Contacts = null;
            }
        }

        private static void OnOriginalKill(ExpandedFirePillarState state, ExpandedFireRegion region,
            Vector2 position, double now, bool elite, double preCriticalDamage, ExpandedFirePillarSettings settings,
            double regionShapeBonus, Func<IReadOnlyDictionary<int, Vector2>> targets, Action<ExpandedFirePillarHit> hit,
            Func<bool>? reserve, Action? suppressed)
        {
            if (elite)
            {
                if (region.NativeDuration.HasValue && !region.EliteExtensionUsed && !state.NextEliteExtensionAt.HasValue)
                    ExtendForElite(state, region, now);
                if (settings.EliteReignition && !state.NextEliteReignitionAt.HasValue && ReserveSupport(state, reserve, suppressed))
                {
                    SpawnSupport(state, position, now, preCriticalDamage * .35,
                        ExpandedFireSupportKind.EliteReignition, regionShapeBonus, settings.EmberDurationBonus);
                    state.NextEliteReignitionAt = now + 5;
                }
                return;
            }
            if (!settings.NormalKillBurst || state.NextNormalBurstAt.HasValue || ++state.NormalBurstKills < 8) return;
            state.NormalBurstKills = 0;
            if (!ReserveSupport(state, reserve, suppressed)) return;
            HitSupportCircle(position, 1 + regionShapeBonus, preCriticalDamage * .25, targets, hit);
            state.NextNormalBurstAt = now + 2;
        }

        private static void HitSupportCircle(Vector2 origin, double radius, double damage,
            Func<IReadOnlyDictionary<int, Vector2>> targets, Action<ExpandedFirePillarHit> hit, HashSet<int>? used = null)
        {
            var inside = targets().Where(target => InRange(target.Value, origin, radius)).OrderBy(target => target.Key).ToArray();
            foreach (var target in inside)
                if (targets().ContainsKey(target.Key) && (used == null || used.Add(target.Key)))
                    hit(new ExpandedFirePillarHit(target.Key, target.Value, damage, true, 0, 0, 0));
        }

        private static void RecordContact(ExpandedFireRegion region, int id, double at, int consecutive)
        {
            region.Contacts ??= new List<ExpandedFireContact>();
            var contact = region.Contacts.Find(entry => entry.TargetId == id);
            if (contact == null)
                region.Contacts.Add(new ExpandedFireContact { TargetId = id, LastDueAt = at, ConsecutiveHits = consecutive });
            else
            {
                contact.LastDueAt = at;
                contact.ConsecutiveHits = consecutive;
            }
        }

        private static void ExtendForElite(ExpandedFirePillarState state, ExpandedFireRegion region, double now)
        {
            var end = Math.Min(region.EndsAt + .35, region.StartedAt + region.NativeDuration!.Value * 1.75);
            if (end <= region.EndsAt + TimeEpsilon) return;
            region.EndsAt = end;
            region.EliteExtensionUsed = true;
            state.NextEliteExtensionAt = now + 4;
        }

        private static double HitDamage(ExpandedFireRegion region, Vector2 position, bool first, int streak)
        {
            if (region.BaseDamage is not double basis) return region.Damage;
            var bonus = first && region.IncreasedIgnitionDamage ? IgnitionDamageBonus : 0;
            if (region.RepeatedHeat && streak >= 3) bonus += .08;
            var innerRadius = (float)(region.Radius * InnerRadiusFraction);
            if (region.IncreasedCoreDamage && Vector2.DistanceSquared(region.Center, position) <= innerRadius * innerRadius)
                bonus += CoreDamageBonus;
            return Math.Min(basis * DamageCapMultiplier, region.Damage + basis * bonus);
        }

        private static bool InRange(Vector2 left, Vector2 right, double radius) =>
            Vector2.DistanceSquared(left, right) <= radius * radius + TimeEpsilon;
    }
}
