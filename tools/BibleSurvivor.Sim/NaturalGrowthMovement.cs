using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using BibleSurvivor.Core;

namespace BibleSurvivor.Sim;

internal sealed class NaturalGrowthMovement(BattleWorld world, IReadOnlyDictionary<string, double> speeds)
{
    private static readonly BattleTuning Tuning = new();
    private static readonly Vector2[] Directions = Enumerable.Range(0, 16)
        .Select(index => Unit(index * Math.PI / 8)).Append(Vector2.Zero).ToArray();

    private Vector2 direction;

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
        foreach (var pair in positions)
        {
            if (!targets.TryGetValue(pair.Key, out var enemy))
                continue;

            var relative = pair.Value - player;
            var isBoss = pair.Key == boss?.BossActorId && session.Stage is "boss" or "boss_intro";
            var speed = isBoss ? BossDefinition.Speed : speeds[enemy.MonsterId];
            var radius = isBoss ? BossDefinition.Radius : Tuning.EnemyRadius;
            var chaseSpeed = (float)(Tuning.PlayerSpeed * speed);
            threats.Add(new Threat(relative, -Normalize(relative) * chaseSpeed,
                (float)(Tuning.PlayerRadius + radius + .25), chaseSpeed));
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

        var preferred = Unit(world.SimulationTime * .3);
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

        var slam = boss is { UsesSlam: true } ? session.Boss : null;
        var moveSpeed = (float)(Tuning.PlayerSpeed * session.MoveSpeedMultiplier);
        var bestScore = float.NegativeInfinity;
        foreach (var candidate in Directions)
        {
            var velocity = candidate * moveSpeed;
            var score = Vector2.Dot(candidate, preferred);
            if (normalWave)
                score -= CrowdCost(velocity, threats);
            else foreach (var threat in threats)
            {
                var relativeVelocity = threat.Velocity - velocity;
                var speedSquared = relativeVelocity.LengthSquared();
                var time = speedSquared == 0 ? 0 : Math.Clamp(-Vector2.Dot(threat.Position, relativeVelocity) / speedSquared, 0, horizon);
                var distance = (threat.Position + relativeVelocity * time).Length();
                var endDistance = (threat.Position + relativeVelocity * horizon).Length();
                score -= 12 * MathF.Max(0, threat.Radius - distance) + 3 * MathF.Max(0, threat.Radius + .4f - endDistance);
            }

            if (slam is { Telegraphing: true })
            {
                var landing = player + velocity * horizon;
                var offset = landing - new Vector2(slam.TargetX, slam.TargetY);
                score -= 12 * MathF.Max(0, (float)(BossDefinition.SlamRadius + Tuning.PlayerRadius) - offset.Length());
            }

            if (score > bestScore)
            {
                bestScore = score;
                direction = candidate;
            }
        }
        return direction;
    }

    // Enemies can turn as the player moves. Reserve room for their possible approach,
    // especially where several enemies can converge on the end of the escape route.
    private static float CrowdCost(Vector2 velocity, List<Threat> threats)
    {
        const float nearTime = .25f, escapeTime = .85f;
        var near = velocity * nearTime;
        var exit = velocity * escapeTime;
        var cost = 0f;
        foreach (var threat in threats)
        {
            var nearClearance = Vector2.Distance(threat.Position, near) - threat.ChaseSpeed * nearTime - threat.Radius;
            var exitClearance = Vector2.Distance(threat.Position, exit) - threat.ChaseSpeed * escapeTime - threat.Radius;
            cost += 8 * MathF.Max(0, .25f - nearClearance) + 2 * MathF.Max(0, .9f - exitClearance);
        }
        return cost;
    }

    private static Vector2 Unit(double angle) => new((float)Math.Cos(angle), (float)Math.Sin(angle));
    private static Vector2 Normalize(Vector2 value) => value.LengthSquared() == 0 ? Vector2.Zero : Vector2.Normalize(value);
    private readonly record struct Threat(Vector2 Position, Vector2 Velocity, float Radius, float ChaseSpeed = 0);
}
