using System;
using System.Collections.Generic;
using System.Numerics;

namespace BibleSurvivor.Core
{
    public readonly struct ExpandedBibleBookPose
    {
        public Vector2 Position { get; }
        public double Angle { get; }
        public double Radius { get; }
        public int RotationSign { get; }

        internal ExpandedBibleBookPose(Vector2 position, double angle, double radius, int rotationSign)
        {
            Position = position;
            Angle = angle;
            Radius = radius;
            RotationSign = rotationSign;
        }
    }

    public readonly struct ExpandedBibleRanks
    {
        private const double CrowdRadiusSquared = 3 * 3;
        private const double CloseKnockbackRadius = 1.2;
        private readonly bool innerBook;
        private readonly bool reverseInnerBook;
        private readonly bool hardCover;
        private readonly bool blockApproach;

        public bool ExpandsCrowdedSide { get; }
        public bool PulsesOrbitRadius { get; }

        private ExpandedBibleRanks(IReadOnlyDictionary<string, int> ranks)
        {
            bool Has(string id) => ranks.TryGetValue("weapon.bible." + id, out var rank) && rank > 0;
            ExpandsCrowdedSide = Has("a.02");
            innerBook = Has("a.03");
            reverseInnerBook = Has("a.04");
            PulsesOrbitRadius = Has("a.05");
            hardCover = Has("b.01");
            blockApproach = Has("b.02");
        }

        public static ExpandedBibleRanks FromRanks(IReadOnlyDictionary<string, int> ranks) =>
            new ExpandedBibleRanks(ranks ?? throw new ArgumentNullException(nameof(ranks)));

        public Vector2 FindCrowdedDirection(Vector2 player, IEnumerable<Vector2> enemyCenters)
        {
            if (!ExpandsCrowdedSide) return Vector2.Zero;
            var count = 0;
            double x = 0, y = 0;
            foreach (var center in enemyCenters)
            {
                var relative = center - player;
                if (relative.LengthSquared() > CrowdRadiusSquared) continue;
                count++;
                x += relative.X;
                y += relative.Y;
            }
            if (count < 6) return Vector2.Zero;
            var centroid = new Vector2((float)(x / count), (float)(y / count));
            return centroid.LengthSquared() > 1e-12f ? Vector2.Normalize(centroid) : Vector2.Zero;
        }

        public ExpandedBibleBookPose Book(Vector2 player, int index, int count, double simulationTime, double rotationMultiplier,
            double cycleStart, double motionCycle, double orbitRadius, double nativeOrbitRadius, Vector2 crowdedDirection = default,
            double innerAngleOffset = 0)
        {
            // The added specialization book is the last phase slot.
            var inner = innerBook && index == count - 1;
            var rotationSign = inner && reverseInnerBook ? -1 : 1;
            var angle = rotationSign * simulationTime * 2.5 * rotationMultiplier + index * 2 * Math.PI / count;
            if (inner) angle += innerAngleOffset;
            var direction = new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle));
            if (ExpandsCrowdedSide && Vector2.Dot(direction, crowdedDirection) > 1e-6f)
                orbitRadius += nativeOrbitRadius * .15;
            if (PulsesOrbitRadius && motionCycle > 0)
            {
                var phase = Math.Max(0, Math.Min(1, (simulationTime - cycleStart) / motionCycle));
                orbitRadius += .4 * Math.Min(phase, 1 - phase);
            }
            orbitRadius = Math.Min(nativeOrbitRadius * 2.5, orbitRadius);
            if (inner) orbitRadius *= .65;
            return new ExpandedBibleBookPose(player + direction * (float)orbitRadius, angle, orbitRadius, rotationSign);
        }

        public double BookKnockback(Vector2 playerToTarget)
        {
            var knockback = hardCover ? .20 : 0;
            if (blockApproach && playerToTarget.LengthSquared() <= (float)(CloseKnockbackRadius * CloseKnockbackRadius))
                knockback += .25;
            return knockback;
        }
    }
}
