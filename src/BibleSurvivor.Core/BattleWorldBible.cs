using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace BibleSurvivor.Core
{
    public sealed partial class BattleWorld
    {
        private ExpandedBibleLifecycleState? GetBibleLifecycle(BattleWeaponState clock)
        {
            if (bibleLifecycleRules.DeflectsHostile || bibleLifecycleRules.CriticalCover ||
                bibleLifecycleRules.DamageBarrier || bibleLifecycleRules.ExtendsAfterBookKills ||
                bibleLifecycleRules.ReducesWaitAfterEliteKill)
                clock.BibleLifecycle ??= new ExpandedBibleLifecycleState();
            return clock.BibleLifecycle;
        }

        private void ObserveBibleDamage(double before, double received)
        {
            if (!weapons.TryGetValue("bible", out var clock)) return;
            var state = GetBibleLifecycle(clock);
            if (state == null || !state.ObserveDamage(bibleLifecycleRules, SimulationTime,
                before / Session.PlayerMaximumHealth, Math.Max(0, before - received) / Session.PlayerMaximumHealth,
                received, ReserveRegion)) return;
            const double radius = ExpandedBibleLifecycleState.BarrierRadius;
            var center = PlayerPosition;
            RecordAreaEffect(BattleAreaEffectKind.BibleBarrier, center, radius, SimulationTime);
            var damage = (clock.FrozenOrbitDamage > 0 ? clock.FrozenOrbitDamage :
                Session.AttackProfile("bible").Damage) * .35;
            foreach (var target in enemies.OrderBy(target => target.Key).ToArray())
                if (Vector2.DistanceSquared(target.Value, center) <= radius * radius)
                    Hit(target.Key, damage, "bible", target.Value, original: false);

            bool ReserveRegion()
            {
                if (SupportRegionCount < BattleSnapshot.ExpandedSupportRegionLimit) return true;
                RecordSupportSuppression();
                return false;
            }
        }

        private void ResolveBibleDeflection(BattleWeaponState clock, int books, double orbitRadius, double hitRadius,
            Vector2 pastCrowd, Vector2 crowd)
        {
            if (!bibleLifecycleRules.DeflectsHostile || clock.BibleLifecycle == null || hostileProjectiles.Count == 0)
                return;
            var contacts = new List<ExpandedBibleDeflectionContact>();
            var tickStart = SimulationTime - FixedStep;
            var born = Math.Max(tickStart, clock.ActiveSince);
            var launchPhase = (float)Math.Max(0, Math.Min(1, (born - tickStart) / FixedStep));
            var centerAtBirth = Vector2.Lerp(previousPlayerForDeflection, PlayerPosition, launchPhase);
            var strikeRadius = hitRadius + BossDefinition.ChainRadius;
            for (var index = 0; index < books; index++)
            {
                var previous = BiblePose(clock, index, books, born, centerAtBirth, orbitRadius, pastCrowd).Position;
                var current = BiblePose(clock, index, books, SimulationTime, PlayerPosition, orbitRadius, crowd).Position;
                foreach (var hostile in hostileProjectiles)
                {
                    var end = new Vector2(hostile.X, hostile.Y);
                    var start = hostileStepStarts.TryGetValue(hostile.Id, out var from)
                        ? Vector2.Lerp(from, end, launchPhase) : end;
                    var at = Intersection(start - previous, end - current, strikeRadius);
                    if (at.HasValue)
                        contacts.Add(new ExpandedBibleDeflectionContact(hostile.Id, index, at.Value,
                            Vector2.DistanceSquared(end, current) <= strikeRadius * strikeRadius));
                }
            }
            clock.BibleLifecycle.TryDeflect(bibleLifecycleRules, SimulationTime, contacts, CanEraseHostile(),
                previous => Session.NextWeaponProcState("bible.b.03", previous), EraseHostile);
        }

        private ExpandedBibleBookPose BiblePose(BattleWeaponState clock, int index, int count, double now,
            Vector2 center, double orbitRadius, Vector2 crowd) =>
            bibleRanks.Book(center, index, count, now, Session.BibleRotationMultiplier,
                clock.ActiveSince, clock.BibleMotionCycle, orbitRadius, tuning.BibleOrbitRadius, crowd,
                clock.BibleEvolution?.InnerAngleOffset ?? 0);

        private void RecordBibleKill(BattleWeaponState clock, int index, int books, bool elite)
        {
            var before = clock.ActiveUntil - clock.ActiveSince;
            var nativeDuration = clock.BibleDuration?.NativeDuration ?? BibleNativeDuration(clock);
            var after = clock.BibleLifecycle?.RecordAcceptedBookKill(
                bibleLifecycleRules, SimulationTime, index, books, elite, nativeDuration, before) ?? before;
            after = clock.BibleDuration?.RecordOriginalKill(after, elite) ?? after;
            clock.ActiveUntil += after - before;
            clock.NextCycle += after - before;
            var reduction = clock.BibleLifecycle?.ConsumeWaitReduction() ?? 0;
            if (clock.WeaponForm == null) ReduceBibleWait(clock, reduction);
        }

        private double BibleNativeDuration(BattleWeaponState clock)
        {
            if (clock.FrozenOrbitBaseDamage >= 30) return WeaponRules.Get("bible", 7).Duration;
            if (clock.FrozenOrbitBaseDamage >= 20) return WeaponRules.Get("bible", 4).Duration;
            return WeaponRules.Get("bible", 1).Duration;
        }

        private static void ReduceBibleWait(BattleWeaponState clock, double reduction)
        {
            if (reduction <= 0) return;
            var wait = clock.NextCycle - clock.ActiveUntil;
            var native = WeaponRules.Get("bible", 1).Cooldown;
            clock.NextCycle = clock.ActiveUntil + Math.Min(wait, Math.Max(.20 * native, wait - native * reduction));
        }

        private void StepBibleEvolution(BattleWeaponState clock)
        {
            if (bibleEvolutionRanks.ExtraPulse || bibleEvolutionRanks.SteerInnerBook || bibleEvolutionRanks.EliteSanctuary)
                clock.BibleEvolution ??= new ExpandedBibleEvolutionState();
            var state = clock.BibleEvolution;
            if (state == null) return;
            state.StepSanctuary(SimulationTime, () => enemies, ApplyBibleEvolutionSupport);
            if (!Session.CanAttack) return;
            if (state.ConsumeEcho(SimulationTime) is ExpandedBibleEcho echo)
            {
                var center = PlayerPosition;
                RecordAreaEffect(BattleAreaEffectKind.BibleBarrier, center, echo.Radius, SimulationTime);
                foreach (var target in enemies.OrderBy(target => target.Key).ToArray())
                {
                    if (!Session.CanAttack) break;
                    var radius = echo.Radius + TargetRadius(target.Key);
                    if (Vector2.DistanceSquared(target.Value, center) <= radius * radius)
                        ApplyBibleEvolutionSupport(target.Key, target.Value, echo.Damage);
                }
            }
            if (!Session.CanAttack || clock.PulseHitIds == null || clock.FrozenOrbitCount == 0 ||
                SimulationTime + 1e-9 < clock.PulseUntil || state.LastSteeredPulse >= state.PulseOrdinal) return;
            var crowd = new Vector2(clock.BibleCrowdX, clock.BibleCrowdY);
            var inner = BiblePose(clock, clock.FrozenOrbitCount - 1, clock.FrozenOrbitCount, SimulationTime, PlayerPosition,
                clock.FrozenOrbitRadius, crowd);
            state.TrySteerInnerBook(bibleEvolutionRanks, PlayerPosition, inner.Angle, enemies, clock.PulseHitIds);
        }

        private void ApplyBibleEvolutionSupport(int target, Vector2 position, double damage)
        {
            if (Session.CanAttack) Hit(target, damage, "bible", position, original: false);
        }

        private void RecordBibleEliteSanctuary(BattleWeaponState clock, Vector2 position, double damage)
        {
            var state = clock.BibleEvolution;
            if (state?.OnAcceptedEliteOriginalKill(bibleEvolutionRanks, SimulationTime, position, damage,
                CanReserveBibleRegion, RecordSupportSuppression) != true) return;
            state.StepSanctuary(SimulationTime, () => enemies, ApplyBibleEvolutionSupport);
        }

        private void StepBibleSupports(BattleWeaponState clock, IReadOnlyDictionary<int, Vector2> previous)
        {
            if (bibleSupportRanks.ContactPages || bibleSupportRanks.Bookmarks || bibleSupportRanks.ClosingPages ||
                bibleSupportRanks.ReturningBooks || bibleSupportRanks.InactiveGlow)
                clock.BibleSupports ??= new ExpandedBibleSupportsState();
            var state = clock.BibleSupports;
            if (state == null) return;
            state.Step(SimulationTime, PlayerPosition, previous, () => enemies, TargetRadius, Intersection, ApplyBibleSupport,
                () => SupportRegionCount < BattleSnapshot.ExpandedSupportRegionLimit, RecordSupportSuppression);
            if (!Session.CanAttack || clock.StarterCycle <= state.LastClosedCycle || clock.FrozenOrbitCount == 0 ||
                SimulationTime + 1e-9 < clock.ActiveUntil) return;
            var crowd = new Vector2(clock.BibleCrowdX, clock.BibleCrowdY);
            var clockFraction = (float)Math.Max(0, Math.Min(1,
                (clock.ActiveUntil - (SimulationTime - FixedStep)) / FixedStep));
            var centerAtClose = Vector2.Lerp(previousPlayerForDeflection, PlayerPosition, clockFraction);
            var positions = Enumerable.Range(0, Math.Min(clock.FrozenOrbitCount, 2))
                .Select(index => BiblePose(clock, index, clock.FrozenOrbitCount, clock.ActiveUntil,
                    centerAtClose, clock.FrozenOrbitRadius, crowd).Position).ToArray();
            state.OnNaturalClose(bibleSupportRanks, clock.StarterCycle, clock.ActiveUntil, SimulationTime,
                centerAtClose, positions, clock.FrozenOrbitDamage, CanReserveBibleProjectile, RecordSupportSuppression);
        }

        private void ObserveInactiveBible(BattleWeaponState clock) =>
            clock.BibleSupports?.ObserveInactive(SimulationTime, PlayerPosition, clock.FrozenOrbitDamage,
                () => enemies, ApplyBibleSupport, CanReserveBibleRegion, RecordSupportSuppression);

        private void RecordBibleContact(BattleWeaponState clock, Vector2 book, int target, Vector2 position, double damage) =>
            clock.BibleSupports?.OnAcceptedBookContact(bibleSupportRanks, SimulationTime, PlayerPosition, book, target,
                position, enemies.ContainsKey(target), damage, seed => Session.NextWeaponProcState("bible.c.01", seed),
                CanReserveBibleProjectile, RecordSupportSuppression);

        private bool ApplyBibleSupport(ExpandedBibleSupportHit hit)
        {
            var applied = ApplyExpandedHit(hit.TargetId, hit.Damage, "bible", hit.Position, Vector2.Zero, 0, original: false);
            if (applied && hit.Kind == ExpandedBibleSupportKind.InactiveGlow)
                RecordAreaEffect(BattleAreaEffectKind.BibleBarrier, PlayerPosition, ExpandedBibleSupportsState.GlowRadius, SimulationTime);
            return applied;
        }

        private bool CanReserveBibleProjectile() => SupportProjectileCount < BattleSnapshot.ExpandedSupportProjectileLimit &&
            HasBibleSupportCapacity();

        private bool CanReserveBibleRegion() => SupportRegionCount < BattleSnapshot.ExpandedSupportRegionLimit &&
            HasBibleSupportCapacity();

        private bool HasBibleSupportCapacity()
        {
            var pending = weapons.TryGetValue("bible", out var clock) ? clock.BibleEvolution?.ReservedRegions ?? 0 : 0;
            return (BibleActiveSupports?.ActiveGroups ?? 0) + pending < ExpandedBibleSupportsState.GroupLimit;
        }

        private BattleWeaponState CaptureWeapon(BattleWeaponState weapon)
        {
            var copy = weapon.Copy();
            copy.BibleLifecycle?.PruneDeflectionAttempts(id => hostileProjectiles.Any(shot => shot.Id == id));
            copy.BibleSupports?.PruneBookmarks(SimulationTime, enemies.ContainsKey);
            if (copy.JawboneLifecycle?.Pushes is { } pushes)
            {
                pushes.RemoveAll(push => !enemies.ContainsKey(push.Id));
                if (pushes.Count == 0) copy.JawboneLifecycle.Pushes = null;
            }
            return copy;
        }
    }
}
