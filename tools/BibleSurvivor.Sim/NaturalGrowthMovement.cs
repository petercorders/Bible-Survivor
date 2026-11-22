using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using BibleSurvivor.Core;

namespace BibleSurvivor.Sim;

internal sealed class NaturalGrowthMovement(BattleWorld world, IReadOnlyDictionary<string, double> speeds, bool lethalEscape)
{
    private const float ContactHorizon = .30f;
    private const float EscapeHorizon = .85f;
    private const float LowCrowdCost = .25f;
    private const float AttackRangeWeight = 2f;
    private static readonly BattleTuning Tuning = new();
    private static readonly Vector2[] Directions = Enumerable.Range(0, 16)
        .Select(index => Unit(index * Math.PI / 8)).Append(Vector2.Zero).ToArray();

    private Vector2 direction;
    private Vector2 heading = Vector2.UnitX;

    internal Vector2 Next(int tick)
    {
        var session = world.Session;
        var normalWave = session.Stage == "normal";
        if (tick % (normalWave ? 3 : 6) != 0)
            return direction;

        var player = world.PlayerPosition;
        var positions = world.EnemyPositions;
        var targets = session.Targets;
        var boss = session.BossRules;
        const float horizon = .45f;
        var threats = new List<Threat>();
        var evaluateEscape = lethalEscape && normalWave && session.RoundNumber >= 121;
        var lethalContact = false;
        foreach (var pair in positions)
        {
            if (!targets.TryGetValue(pair.Key, out var enemy))
                continue;

            var lethal = evaluateEscape && session.CurrentHealth <= session.EffectiveContactDamage(enemy.ContactDamage);
            lethalContact |= lethal;
            var relative = pair.Value - player;
            var isBoss = pair.Key == boss?.BossActorId && session.Stage is "boss" or "boss_intro";
            var speed = isBoss ? BossDefinition.Speed : speeds[enemy.MonsterId];
            var radius = isBoss ? BossDefinition.Radius : Tuning.EnemyRadius;
            var chaseSpeed = (float)(Tuning.PlayerSpeed * speed);
            threats.Add(new Threat(relative, -Normalize(relative) * chaseSpeed,
                (float)(Tuning.PlayerRadius + radius + .25), chaseSpeed, lethal));
        }

        foreach (var projectile in world.HostileProjectiles)
        {
            var velocity = new Vector2(projectile.DirectionX, projectile.DirectionY) * (float)(BossDefinition.ChainSpeed * Tuning.PlayerSpeed);
            threats.Add(new Threat(new Vector2(projectile.X, projectile.Y) - player, velocity,
                (float)(Tuning.PlayerRadius + BossDefinition.ChainRadius + .25)));
        }

        if (world.ChariotDash is { Phase: "telegraph" or "dash" } charge)
        {
            // Keep the telegraphed corridor clear while choosing a direction.
            for (var offset = .5f; offset < charge.RemainingDistance; offset += .5f)
                threats.Add(new Threat(charge.Origin + charge.Direction * offset - player, Vector2.Zero, (float)charge.DangerHalfWidth + .25f));
        }

        var extendedWave = normalWave && session.RoundNumber > ExperimentalRounds.CalibrationRound;
        var preferred = extendedWave ? heading : Unit(world.SimulationTime * .3);
        var pickup = world.DropPositions.OrderBy(pair => Vector2.DistanceSquared(pair.Value, player))
            .ThenBy(pair => pair.Key).Select(pair => pair.Value).FirstOrDefault(player);
        if (pickup != player)
        {
            preferred = Normalize(pickup - player);
        }
        else if (boss != null && session.Stage is "boss" or "boss_intro" && positions.TryGetValue(boss.BossActorId, out var bossPosition))
        {
            var radial = Normalize(bossPosition - player);
            var orbit = new Vector2(-radial.Y, radial.X);
            preferred = Normalize(orbit + Math.Clamp((Vector2.Distance(player, bossPosition) - 5) / 2, -1, 1) * radial);
        }

        var encounter = boss is { UsesSlam: true } or { UsesAimedShot: true } or { UsesDamageZone: true }
            ? session.Boss : null;
        var warnSlam = boss?.UsesSlam == true && encounter is { Telegraphing: true };
        if (encounter?.AimedShot is { } shot)
        {
            var padding = (float)(Tuning.PlayerRadius + BossDefinition.AimedShotRadius + .25);
            if (encounter.Telegraphing)
            {
                for (var age = .1; age <= BossDefinition.AimedShotLifetime; age += .1)
                    threats.Add(new Threat(shot.PositionAt(age) - player, Vector2.Zero, padding));
            }
            else
            {
                var age = encounter.SecondsSinceAttack(boss!);
                threats.Add(new Threat(shot.PositionAt(age) - player,
                    shot.Direction * (float)BossDefinition.AimedShotSpeed, padding));
            }
        }
        var dangerZone = boss?.UsesDamageZone == true && encounter != null &&
            (encounter.Telegraphing || encounter.HasActiveDamageZone(boss, session.Stage));
        var moveSpeed = (float)(Tuning.PlayerSpeed * session.MoveSpeedMultiplier);
        var seekRange = extendedWave && pickup == player && threats.Count > 0 &&
            session.WeaponForm("bible") == EvolutionRules.Bible;
        var nearest = seekRange ? threats.MinBy(threat => threat.Position.LengthSquared()).Position : Vector2.Zero;
        var pulseReach = (float)(EvolutionRules.PulseRadius + Tuning.EnemyRadius);
        var currentGap = MathF.Max(0, nearest.Length() - pulseReach);
        // At attack range, reduce the distance penalty so escape heading has more influence.
        var rangeWeight = currentGap == 0 ? AttackRangeWeight * .5f : AttackRangeWeight;
        var bestScore = float.NegativeInfinity;
        foreach (var candidate in Directions)
        {
            var velocity = candidate * moveSpeed;
            var score = Vector2.Dot(candidate, preferred);
            if (normalWave)
            {
                var cost = CrowdCost(velocity, threats);
                score -= cost;
                // Preserve attack opportunities when a route has room and no pickup is pulling us away.
                if (seekRange && cost <= LowCrowdCost)
                {
                    var nextGap = MathF.Max(0, (nearest - velocity * horizon).Length() - pulseReach);
                    score += rangeWeight * (currentGap - nextGap);
                }
            }
            else foreach (var threat in threats)
            {
                var relativeVelocity = threat.Velocity - velocity;
                var speedSquared = relativeVelocity.LengthSquared();
                var time = speedSquared == 0 ? 0 : Math.Clamp(-Vector2.Dot(threat.Position, relativeVelocity) / speedSquared, 0, horizon);
                var distance = (threat.Position + relativeVelocity * time).Length();
                var endDistance = (threat.Position + relativeVelocity * horizon).Length();
                score -= 12 * MathF.Max(0, threat.Radius - distance) + 3 * MathF.Max(0, threat.Radius + .4f - endDistance);
            }

            if (warnSlam || dangerZone)
            {
                var landing = player + velocity * horizon;
                var offset = landing - new Vector2(encounter!.TargetX, encounter.TargetY);
                var radius = dangerZone ? BossDefinition.DamageZoneRadius : BossDefinition.SlamRadius;
                score -= 12 * MathF.Max(0, (float)(radius + Tuning.PlayerRadius) - offset.Length());
            }

            if (score > bestScore)
            {
                bestScore = score;
                direction = candidate;
            }
        }
        if (lethalContact)
        {
            var intendedVelocity = direction * moveSpeed;
            if (threats.Any(threat => threat.Lethal && PredictContact(threat, intendedVelocity)))
                return MoveToClearance(threats, moveSpeed);
        }
        if (direction.LengthSquared() > 0) heading = direction;
        return direction;
    }

    private static bool PredictContact(Threat threat, Vector2 velocity)
    {
        if (threat.Position.LengthSquared() <= threat.Radius * threat.Radius)
            return true;
        var relativeVelocity = threat.Velocity - velocity;
        var speedSquared = relativeVelocity.LengthSquared();
        var closest = speedSquared == 0 ? 0 : Math.Clamp(-Vector2.Dot(threat.Position, relativeVelocity) / speedSquared,
            0, ContactHorizon);
        return (threat.Position + relativeVelocity * closest).LengthSquared() <= threat.Radius * threat.Radius;
    }

    private Vector2 MoveToClearance(List<Threat> threats, float moveSpeed)
    {
        var bestClearance = float.NegativeInfinity;
        var bestHeading = float.NegativeInfinity;
        foreach (var candidate in Directions)
        {
            var destination = (candidate * moveSpeed) * EscapeHorizon;
            var clearance = threats.Min(threat => Vector2.Distance(threat.Position, destination) -
                threat.ChaseSpeed * EscapeHorizon - threat.Radius);
            var forward = Vector2.Dot(candidate, heading);
            if (clearance > bestClearance || clearance == bestClearance && forward > bestHeading)
            {
                bestClearance = clearance;
                bestHeading = forward;
                direction = candidate;
            }
        }
        if (direction.LengthSquared() > 0) heading = direction;
        return direction;
    }

    // Enemies can turn as the player moves. Reserve room for their possible approach,
    // especially where several enemies can converge on the end of the escape route.
    private static float CrowdCost(Vector2 velocity, List<Threat> threats)
    {
        const float nearTime = .25f;
        var near = velocity * nearTime;
        var exit = velocity * EscapeHorizon;
        var cost = 0f;
        foreach (var threat in threats)
        {
            var nearClearance = Vector2.Distance(threat.Position, near) - threat.ChaseSpeed * nearTime - threat.Radius;
            var exitClearance = Vector2.Distance(threat.Position, exit) - threat.ChaseSpeed * EscapeHorizon - threat.Radius;
            cost += 8 * MathF.Max(0, .25f - nearClearance) + 2 * MathF.Max(0, .9f - exitClearance);
        }
        return cost;
    }

    private static Vector2 Unit(double angle) => new((float)Math.Cos(angle), (float)Math.Sin(angle));
    private static Vector2 Normalize(Vector2 value) => value.LengthSquared() == 0 ? Vector2.Zero : Vector2.Normalize(value);
    private readonly record struct Threat(Vector2 Position, Vector2 Velocity, float Radius, float ChaseSpeed = 0, bool Lethal = false);
}
