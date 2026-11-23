using System;
using System.Linq;
using System.Numerics;

namespace BibleSurvivor.Core
{
    public sealed partial class BattleWorld
    {
        private uint voiceDeflectRandom;
        private double? nextVoiceDeflectAt;

        private void TryVoicePulseDeflect(Vector2 center, Vector2 facing, double reach, double halfAngle)
        {
            if (!CanEraseHostile() || nextVoiceDeflectAt > SimulationTime + 1e-9) return;
            foreach (var shot in hostileProjectiles.OrderBy(shot => shot.Id).ToArray())
            {
                var end = new Vector2(shot.X, shot.Y);
                var start = hostileStepStarts.TryGetValue(shot.Id, out var previous) ? previous : end;
                if (!VoicePulseTouches(center, facing, reach, halfAngle, start, end, BossDefinition.ChainRadius))
                    continue;
                voiceDeflectRandom = Session.NextWeaponProcState("wilderness_voice.c.04", voiceDeflectRandom);
                if (voiceDeflectRandom / 4294967296d >= .20 || !EraseHostile(shot.Id)) continue;
                nextVoiceDeflectAt = SimulationTime + 2;
                return;
            }
        }

        internal static bool VoicePulseTouches(Vector2 center, Vector2 facing, double reach, double halfAngle,
            Vector2 from, Vector2 to, double shotRadius)
        {
            var start = from - center;
            var end = to - center;
            var outer = reach + shotRadius;
            var enter = Intersection(start, end, outer);
            if (!enter.HasValue) return false;
            if (halfAngle >= 180) return true;
            var step = end - start;
            if (Contains(start + step * (float)enter.Value) || Contains(end)) return true;

            // A straight sweep can enter the angular sector through either radial edge.
            var radians = halfAngle * Math.PI / 180;
            var forward = facing * (float)(reach * Math.Cos(radians));
            var lateral = new Vector2(-facing.Y, facing.X) * (float)(reach * Math.Sin(radians));
            return TouchesEdge(forward + lateral) || TouchesEdge(forward - lateral);

            bool Contains(Vector2 point) => HitGeometry.InArc(point.X, point.Y, facing.X, facing.Y, outer, 2 * halfAngle);

            bool TouchesEdge(Vector2 tip)
            {
                var cross = CrossProduct(step, tip);
                if (Math.Abs(cross) > 1e-12)
                {
                    var alongSweep = CrossProduct(-start, tip) / cross;
                    var alongEdge = CrossProduct(-start, step) / cross;
                    if (alongSweep >= 0 && alongSweep <= 1 && alongEdge >= 0 && alongEdge <= 1) return true;
                }
                var radiusSquared = shotRadius * shotRadius;
                return SegmentDistanceSquared(start, Vector2.Zero, tip) <= radiusSquared ||
                    SegmentDistanceSquared(end, Vector2.Zero, tip) <= radiusSquared ||
                    SegmentDistanceSquared(Vector2.Zero, start, end) <= radiusSquared ||
                    SegmentDistanceSquared(tip, start, end) <= radiusSquared;
            }
        }

        private static double CrossProduct(Vector2 first, Vector2 second) =>
            (double)first.X * second.Y - (double)first.Y * second.X;

        private static double SegmentDistanceSquared(Vector2 point, Vector2 start, Vector2 end)
        {
            var delta = end - start;
            var distance = delta.LengthSquared();
            var t = distance <= 0 ? 0 : Math.Max(0, Math.Min(1, Vector2.Dot(point - start, delta) / distance));
            return Vector2.DistanceSquared(point, start + delta * t);
        }
    }
}
