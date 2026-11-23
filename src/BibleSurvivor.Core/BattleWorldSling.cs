using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace BibleSurvivor.Core
{
    public sealed partial class BattleWorld
    {
        private void ApplySlingFollowups(ExpandedSlingCombatState state, BattleProjectileState projectile,
            int id, Vector2 contact, double precrit, bool elite, bool boss)
        {
            var killed = !enemies.ContainsKey(id);
            if (slingRanks.RetargetAfterKill && id == state.LastTargetId && projectile.Id == state.LastOriginalId)
                state.LastTargetKilled = killed;
            if (slingRanks.RepeatVolleyHit && state.VolleyHits?.Any(hit =>
                hit.Cycle == projectile.Cycle && hit.TargetId == id) != true)
            {
                state.VolleyHits ??= new List<ExpandedSlingVolleyHit>();
                state.VolleyHits.Add(new ExpandedSlingVolleyHit { Cycle = projectile.Cycle, TargetId = id });
            }
            if (slingRanks.Ricochet && projectile.HitIds.Count == 1)
            {
                var limit = slingRanks.WiderRicochet ? 3.5 : 2.5;
                var next = enemies.Where(target => !projectile.HitIds.Contains(target.Key) &&
                        Vector2.DistanceSquared(target.Value, contact) <= limit * limit)
                    .OrderBy(target => Vector2.DistanceSquared(target.Value, contact)).ThenBy(target => target.Key).FirstOrDefault();
                if (next.Key != 0)
                    SpawnSlingSupport(state, ExpandedSlingSupportKind.Ricochet, contact, next.Value - contact,
                        precrit * (slingRanks.StrongerRicochet ? .60 : .45), limit, next.Key, id);
                else if (slingRanks.ReturnRicochet && state.NextReturnAt.GetValueOrDefault() <= SimulationTime &&
                    Vector2.DistanceSquared(contact, PlayerPosition) > 1e-8f &&
                    SpawnSlingSupport(state, ExpandedSlingSupportKind.Return, contact, PlayerPosition - contact, precrit * .25, 3, excluded: id))
                    state.NextReturnAt = SimulationTime + 2;
            }
            if (slingRanks.Rupture && projectile.RemainingPierce == 0 && state.NextRuptureAt.GetValueOrDefault() <= SimulationTime &&
                SlingSupportCircle(contact, 1, precrit * .25))
                state.NextRuptureAt = SimulationTime + 1;
            if (!elite && slingRanks.StoneStorm && state.NextStormAt.GetValueOrDefault() <= SimulationTime)
            {
                state.NormalHits = Math.Min(15, state.NormalHits + 1);
                if (state.NormalHits == 15)
                {
                    state.NormalHits = 0;
                    if (SupportProjectileCount <= BattleSnapshot.ExpandedSupportProjectileLimit - 2 &&
                        SpawnSlingSupport(state, ExpandedSlingSupportKind.Storm, PlayerPosition, contact - PlayerPosition, precrit * .30, 3))
                    {
                        state.NextStormAt = SimulationTime + 5;
                        state.StormSecondAt = SimulationTime + .15;
                        state.StormSecondDamage = precrit * .30;
                    }
                    else RecordSupportSuppression();
                }
            }
            if (projectile.SlingEvolved && boss && slingRanks.BossVolley && !state.BossBoostPending &&
                state.NextBossBoostAt.GetValueOrDefault() <= SimulationTime && ++state.BossHits == 12)
            {
                state.BossHits = 0;
                state.BossBoostPending = true;
                state.NextBossBoostAt = SimulationTime + 5;
            }
            if (projectile.SlingEvolved && elite && killed && slingRanks.EliteShards &&
                state.NextShardsAt.GetValueOrDefault() <= SimulationTime)
            {
                var targets = enemies.Where(target => !Session.IsEliteActor(target.Key) &&
                    Vector2.DistanceSquared(target.Value, contact) <= 9).OrderBy(target =>
                    Vector2.DistanceSquared(target.Value, contact)).ThenBy(target => target.Key).Take(2).ToArray();
                if (targets.Length > 0 && SupportRegionCount < BattleSnapshot.ExpandedSupportRegionLimit)
                {
                    foreach (var target in targets) Hit(target.Key, precrit * .25, "sling", target.Value, original: false);
                    state.NextShardsAt = SimulationTime + 3;
                }
                else if (targets.Length > 0) RecordSupportSuppression();
            }
        }

        private bool SpawnSlingSupport(ExpandedSlingCombatState state, ExpandedSlingSupportKind kind, Vector2 origin,
            Vector2 aim, double damage, double distance, int target = 0, int excluded = 0)
        {
            if (SupportProjectileCount >= BattleSnapshot.ExpandedSupportProjectileLimit)
            {
                RecordSupportSuppression();
                return false;
            }
            var direction = aim.LengthSquared() > 1e-12f ? Vector2.Normalize(aim) : facing;
            state.Support ??= new List<ExpandedSlingSupportState>();
            state.Support.Add(new ExpandedSlingSupportState
            {
                Kind = kind, X = origin.X, Y = origin.Y, DirectionX = direction.X, DirectionY = direction.Y,
                StartedAt = SimulationTime, Remaining = distance, Damage = damage, TargetId = target, ExcludedId = excluded,
            });
            return true;
        }

        private bool SlingSupportCircle(Vector2 center, double radius, double damage)
        {
            if (SupportRegionCount >= BattleSnapshot.ExpandedSupportRegionLimit)
            {
                RecordSupportSuppression();
                return false;
            }
            foreach (var target in enemies.OrderBy(target => target.Key).ToArray())
                if (Vector2.DistanceSquared(center, target.Value) <= radius * radius)
                    Hit(target.Key, damage, "sling", target.Value, original: false);
            return true;
        }

        private void MoveSlingSupports(ExpandedSlingCombatState state, IReadOnlyDictionary<int, Vector2> previous)
        {
            if (state.StormSecondAt <= SimulationTime + 1e-9)
            {
                var damage = state.StormSecondDamage;
                state.StormSecondAt = null;
                state.StormSecondDamage = 0;
                if (enemies.Count > 0)
                {
                    var nearest = enemies.OrderBy(target => Vector2.DistanceSquared(PlayerPosition, target.Value))
                        .ThenBy(target => target.Key).First();
                    SpawnSlingSupport(state, ExpandedSlingSupportKind.Storm, PlayerPosition, nearest.Value - PlayerPosition, damage, 3);
                }
            }
            if (state.Support == null) return;
            foreach (var shot in state.Support.ToArray())
            {
                if (shot.StartedAt >= SimulationTime - 1e-9) continue;
                var from = shot.Position;
                var direction = shot.Direction;
                var distance = Math.Min(shot.Remaining, tuning.SlingSpeed * FixedStep);
                if (shot.Kind == ExpandedSlingSupportKind.Return)
                {
                    var towardPlayer = PlayerPosition - from;
                    var catchDistance = towardPlayer.Length();
                    if (catchDistance <= 1e-6f) { state.Support.Remove(shot); continue; }
                    direction = towardPlayer / catchDistance;
                    distance = Math.Min(distance, catchDistance);
                }
                var to = from + direction * (float)distance;
                var earnedAt = 2d;
                var struck = 0;
                foreach (var target in enemies.OrderBy(target => target.Key))
                {
                    if (target.Key == shot.ExcludedId || shot.TargetId > 0 && target.Key != shot.TargetId) continue;
                    var old = previous.TryGetValue(target.Key, out var position) ? position : target.Value;
                    var at = Intersection(from - old, to - target.Value, .10 + TargetRadius(target.Key));
                    if (!at.HasValue || at.Value >= earnedAt) continue;
                    earnedAt = at.Value;
                    struck = target.Key;
                }
                if (struck > 0)
                {
                    var contact = from + (to - from) * (float)earnedAt;
                    if (Hit(struck, shot.Damage, "sling", contact, original: false) > 0 &&
                        shot.Kind == ExpandedSlingSupportKind.Ricochet && slingRanks.RicochetBurst &&
                        state.NextRicochetBurstAt.GetValueOrDefault() <= SimulationTime &&
                        SlingSupportCircle(contact, 1, shot.Damage / (slingRanks.StrongerRicochet ? .60 : .45) * .20))
                        state.NextRicochetBurstAt = SimulationTime + 2;
                    state.Support.Remove(shot);
                }
                else
                {
                    shot.X = to.X;
                    shot.Y = to.Y;
                    shot.Remaining -= distance;
                    if (shot.Remaining <= 1e-8) state.Support.Remove(shot);
                }
                if (!Session.CanAttack) break;
            }
            if (state.Support.Count == 0) state.Support = null;
        }
    }
}
