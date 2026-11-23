using System;
using System.Collections.Generic;
using BibleSurvivor.Core;
using UnityEngine;

namespace BibleSurvivor.Unity
{
    // Technical outlines drawn from the combat checkpoint. Enemy telegraphs remain above these layers.
    internal sealed class AdamExpandedWeaponView : IDisposable
    {
        enum Stroke { Solid, Dash, Dot }

        const double MarkerLifetime = .14;
        const float StaffLaneWidth = 1.2f;
        const float CrossThornRadius = .75f / 2;
        const float ChariotTrailWidth = .9f;
        const double ChariotTrailDuration = 1.5;
        const float ChariotWhirlRadius = 1.15f;
        static readonly Color Rim = new Color32(29, 26, 20, 255);
        static readonly Color Voice = new Color32(255, 242, 178, 255);
        static readonly Color Lightning = new Color32(255, 252, 225, 255);
        static readonly Color Staff = new Color32(196, 229, 171, 255);
        static readonly Color Pillar = new Color32(250, 221, 134, 255);
        static readonly Color Lion = new Color32(224, 195, 144, 255);
        static readonly Color Cross = new Color32(245, 232, 200, 255);
        static readonly Color Chariot = new Color32(253, 226, 136, 255);
        static readonly ExpandedWeaponProfile PillarProfile = ExpandedWeaponRules.Get("fire_pillar");
        static readonly ExpandedWeaponProfile CrossProfile = ExpandedWeaponRules.Get("cross");

        readonly Texture2D[] rings = new Texture2D[3];
        readonly List<ExpandedLightningEventState> resolvedBolts = new List<ExpandedLightningEventState>();
        readonly List<(double At, Vector2 Center, bool Second)> echoes = new List<(double, Vector2, bool)>();
        double roarAt = double.NegativeInfinity;
        Vector2 roarCenter;

        public void Clear()
        {
            resolvedBolts.Clear();
            echoes.Clear();
            roarAt = double.NegativeInfinity;
        }

        public void Observe(BattleSnapshot previous, BattleSnapshot next)
        {
            if (previous == null || next.Time <= previous.Time) return;
            for (var i = resolvedBolts.Count - 1; i >= 0; i--)
                if (next.Time >= resolvedBolts[i].At + MarkerLifetime) resolvedBolts.RemoveAt(i);
            if (previous.Lightning != null && next.Lightning != null && previous.Lightning.Evolved == next.Lightning.Evolved)
            {
                foreach (var bolt in previous.Lightning.Pending)
                    if (bolt.At <= next.Time && next.Time < bolt.At + MarkerLifetime) resolvedBolts.Add(bolt);
            }
            else resolvedBolts.Clear();

            for (var i = echoes.Count - 1; i >= 0; i--)
                if (next.Time >= echoes[i].At + MarkerLifetime) echoes.RemoveAt(i);
            if (previous.WildernessVoice?.Evolved == true && next.WildernessVoice?.Evolved == true)
                foreach (var cast in previous.WildernessVoice.Active)
                {
                    CaptureEcho(next, cast.StartedAt + .5, false);
                    CaptureEcho(next, cast.StartedAt + 1, true);
                }

            var oldLion = previous.DanielLion;
            var newLion = next.DanielLion;
            if (oldLion?.Evolved == true && newLion?.Evolved == true && newLion.Lions.Count > 0 &&
                newLion.NextRoarAt > oldLion.NextRoarAt && newLion.LastStepAt - oldLion.NextRoarAt < BattleWorld.FixedStep)
            {
                roarAt = newLion.LastStepAt;
                roarCenter = new Vector2(newLion.Lions[0].X, newLion.Lions[0].Y);
            }

            void CaptureEcho(BattleSnapshot snap, double due, bool second)
            {
                // The current anchor is valid when this fixed step resolved the due pulse.
                if (previous.Time < due && due <= snap.Time && snap.Time - due < BattleWorld.FixedStep)
                    echoes.Add((snap.Time, new Vector2(snap.PlayerX, snap.PlayerY), second));
            }
        }

        public void DrawGround(AdamWorldProjection projection, BattleSnapshot snap)
        {
            if (Event.current.type != EventType.Repaint) return;
            if (snap.WildernessVoice != null)
            {
                var player = new Vector2(snap.PlayerX, snap.PlayerY);
                var facing = Mathf.Atan2(snap.FacingY, snap.FacingX);
                foreach (var cast in snap.WildernessVoice.Active)
                {
                    var age = snap.Time - cast.StartedAt;
                    var warning = cast.PrimaryPulses == 0 ? Stroke.Dash : Stroke.Solid;
                    if (age < cast.Duration)
                    {
                        if (snap.WildernessVoice.Evolved) Ring(projection, player, (float)cast.Reach, Voice, warning);
                        else Fan(projection, player, facing, (float)cast.HalfAngle * Mathf.Deg2Rad, (float)cast.Reach, warning);
                    }
                    // Pulse shapes use the same live player anchor as the primary cast. These echo cues lead the due tick.
                    if (!snap.WildernessVoice.Evolved) continue;
                    if (cast.EchoesFired == 0 && age >= .5 - MarkerLifetime) Ring(projection, player, 5, Voice, Stroke.Dash);
                    if (cast.EchoesFired == 1 && age >= 1 - MarkerLifetime) Ring(projection, player, 5.5f, Voice, Stroke.Dot);
                }
            }
            foreach (var echo in echoes)
                Ring(projection, echo.Center, echo.Second ? 5.5f : 5, Voice, echo.Second ? Stroke.Dot : Stroke.Dash);

            if (snap.Lightning != null)
                foreach (var bolt in snap.Lightning.Pending) DrawBolt(projection, bolt, Stroke.Dash);
            foreach (var bolt in resolvedBolts) DrawBolt(projection, bolt, Stroke.Solid);

            if (snap.MosesStaff != null)
                foreach (var wave in snap.MosesStaff.Waves)
                    if (wave.HasLane && wave.LaneStartedAt.HasValue)
                        Band(projection, V(wave.Origin), V(wave.Origin) + V(wave.Direction) * (float)wave.LaneLength,
                            StaffLaneWidth, Staff, Stroke.Dash);

            if (snap.FirePillar != null)
                foreach (var region in snap.FirePillar.Regions)
                {
                    var stroke = region.Ember ? Stroke.Dot : snap.Time < region.StartedAt + PillarProfile.FirstAttackDelay ? Stroke.Dash : Stroke.Solid;
                    Ring(projection, new Vector2(region.X, region.Y), (float)region.Radius, Pillar, stroke);
                }

            if (snap.Cross != null)
                foreach (var shot in snap.Cross.Shots) DrawThorns(projection, shot.Thorns);

            if (snap.ElijahChariot != null)
                foreach (var path in snap.ElijahChariot.Paths)
                {
                    var start = V(path.Origin);
                    var direction = V(path.Direction);
                    if (snap.Time < path.MoveAt)
                        Band(projection, start, start + direction * (float)path.Range, (float)path.Width, Chariot, Stroke.Dash);
                    else if (snap.ElijahChariot.Evolved)
                    {
                        // Core expires each point individually as the dash lays the flame strip.
                        var expired = Mathf.Clamp((float)((snap.Time - path.MoveAt - ChariotTrailDuration) * path.Speed), 0, (float)path.Distance);
                        Band(projection, start + direction * expired, V(path.Position), ChariotTrailWidth, Chariot, Stroke.Dot);
                    }
                }
        }

        public void DrawActors(AdamWorldProjection projection, BattleSnapshot snap, bool feedback)
        {
            if (Event.current.type != EventType.Repaint) return;
            foreach (var bolt in resolvedBolts)
            {
                if (bolt.Aftershock) continue;
                var center = new Vector2(bolt.X, bolt.Y);
                var elbow = center + new Vector2(-.12f, .34f);
                Line(projection, center, elbow, Lightning);
                Line(projection, elbow, center + new Vector2(.13f, .68f), Lightning);
            }

            if (snap.MosesStaff != null)
                foreach (var wave in snap.MosesStaff.Waves)
                {
                    if (!wave.IsMoving) continue;
                    var center = V(wave.Position);
                    var direction = V(wave.Direction);
                    var side = Perpendicular(direction) * (float)(wave.Width * .5);
                    Line(projection, center - side, center + side, Staff);
                    Line(projection, center - side, center - side - direction * .18f, Staff);
                    Line(projection, center + side, center + side - direction * .18f, Staff);
                }

            if (snap.FirePillar != null)
                foreach (var region in snap.FirePillar.Regions)
                {
                    if (region.Ember || snap.Time < region.StartedAt + PillarProfile.FirstAttackDelay) continue;
                    var center = new Vector2(region.X, region.Y);
                    Line(projection, center, center + Vector2.up * .68f, Pillar);
                    if (feedback && EnemyPosition(snap, region.TargetId, out var target))
                    {
                        var direction = (target - center).normalized;
                        var tip = center + direction * Mathf.Min(.8f, (target - center).magnitude);
                        var tail = tip - direction * .18f;
                        var side = Perpendicular(direction) * .11f;
                        Line(projection, center, tip, Pillar);
                        Line(projection, tip, tail - side, Pillar);
                        Line(projection, tip, tail + side, Pillar);
                    }
                }

            if (snap.DanielLion != null)
            {
                foreach (var lion in snap.DanielLion.Lions)
                {
                    var guardian = snap.DanielLion.Evolved && lion.Index == 0;
                    var center = new Vector2(lion.X, lion.Y);
                    var radius = guardian ? .28f : .20f;
                    Ring(projection, center, radius, Lion, lion.Returning ? Stroke.Dash : Stroke.Solid);
                    Line(projection, center - Vector2.right * radius * .55f, center + Vector2.right * radius * .55f, Lion);
                }
                if (snap.DanielLion.Leap != null)
                    Ring(projection, new Vector2(snap.DanielLion.Leap.OriginX, snap.DanielLion.Leap.OriginY), 2.5f, Lion, Stroke.Dot);
                if (snap.Time < roarAt + MarkerLifetime) Ring(projection, roarCenter, 1.8f, Lion, Stroke.Solid);
            }

            if (snap.Cross != null)
                foreach (var shot in snap.Cross.Shots)
                {
                    if (!shot.IsFlying) continue;
                    var center = new Vector2(shot.X, shot.Y);
                    var returning = shot.ReturnStartedAt.HasValue;
                    var direction = returning ? (new Vector2(snap.PlayerX, snap.PlayerY) - center).normalized : new Vector2(shot.DirectionX, shot.DirectionY);
                    var outer = Mathf.Max(.10f, (float)shot.Radius);
                    var horizontal = Vector2.right * outer;
                    var vertical = Vector2.up * outer;
                    Line(projection, center - horizontal, center + horizontal, Cross);
                    Line(projection, center - vertical, center + vertical, Cross);
                    var hits = returning ? shot.ReturnHits : shot.OutboundHits;
                    var streaks = hits >= (snap.Cross.Evolved ? 6 : CrossProfile.HitsPerLeg) ? 1 : 3;
                    for (var i = 0; i < streaks; i++)
                    {
                        var tail = center - direction * (outer + .06f) + Perpendicular(direction) * ((i - (streaks - 1) * .5f) * .11f);
                        Line(projection, tail, tail - direction * .30f, Cross, returning ? Stroke.Dot : Stroke.Solid);
                    }
                }

            if (snap.ElijahChariot != null)
                foreach (var path in snap.ElijahChariot.Paths)
                {
                    if (snap.Time < path.MoveAt || path.BodyEnded) continue;
                    var center = V(path.Position);
                    var direction = V(path.Direction);
                    var side = Perpendicular(direction) * (float)(path.Width * .5);
                    Line(projection, center - side, center + side, Chariot);
                    Line(projection, center - side - direction * .32f, center - side, Chariot);
                    Line(projection, center + side - direction * .32f, center + side, Chariot);
                    if (snap.ElijahChariot.Evolved) Ring(projection, center, ChariotWhirlRadius, Chariot, Stroke.Solid);
                }
        }

        static bool EnemyPosition(BattleSnapshot snap, int id, out Vector2 point)
        {
            if (id > 0)
                foreach (var enemy in snap.Enemies)
                    if (enemy.Id == id) { point = new Vector2(enemy.X, enemy.Y); return true; }
            point = default;
            return false;
        }

        void DrawBolt(AdamWorldProjection projection, ExpandedLightningEventState bolt, Stroke stroke)
        {
            var center = new Vector2(bolt.X, bolt.Y);
            var radius = (float)bolt.Radius;
            Ring(projection, center, radius, Lightning, stroke);
            if (bolt.Aftershock) Ring(projection, center, radius - .10f, Lightning, stroke);
        }

        void DrawThorns(AdamWorldProjection projection, List<ExpandedCrossThornState> pieces)
        {
            for (var i = 0; i < pieces.Count;)
            {
                var first = i;
                var piece = pieces[i++];
                var start = new Vector2(piece.StartX, piece.StartY);
                var end = new Vector2(piece.EndX, piece.EndY);
                var direction = (end - start).normalized;
                var slack = .5f / (32 * projection.Scale);
                // Join only contiguous collinear fixed-tick pieces. This keeps long straight trails cheap to draw.
                while (i < pieces.Count && pieces[i].Returning == piece.Returning)
                {
                    var following = pieces[i];
                    var next = new Vector2(following.EndX, following.EndY);
                    if ((new Vector2(following.StartX, following.StartY) - end).sqrMagnitude > 1e-9f ||
                        Mathf.Abs(Vector2.Dot(next - start, Perpendicular(direction))) > slack ||
                        Vector2.Dot(next - end, direction) <= 0) break;
                    end = next;
                    i++;
                }
                Rail(projection, start, end, CrossThornRadius, Cross, Stroke.Dash);
                // Endcaps close the capsule without drawing inner boundaries along a contiguous leg.
                if (i == pieces.Count || (new Vector2(pieces[i].StartX, pieces[i].StartY) - end).sqrMagnitude > 1e-9f)
                    Arc(projection, end, Mathf.Atan2(direction.y, direction.x), Mathf.PI / 2, CrossThornRadius, Cross, Stroke.Dash);
                if (first == 0 || (new Vector2(pieces[first - 1].EndX, pieces[first - 1].EndY) - start).sqrMagnitude > 1e-9f)
                    Arc(projection, start, Mathf.Atan2(-direction.y, -direction.x), Mathf.PI / 2, CrossThornRadius, Cross, Stroke.Dash);
            }
        }

        static Vector2 V(System.Numerics.Vector2 point) => new Vector2(point.X, point.Y);
        static Vector2 Perpendicular(Vector2 direction) => new Vector2(-direction.y, direction.x);
        static Vector2 Unit(double angle) => new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle));

        static void Fan(AdamWorldProjection projection, Vector2 center, float direction, float halfAngle, float radius, Stroke stroke)
        {
            Line(projection, center, center + Unit(direction - halfAngle) * radius, Voice, stroke);
            Arc(projection, center, direction, halfAngle, radius, Voice, stroke);
            Line(projection, center, center + Unit(direction + halfAngle) * radius, Voice, stroke);
        }

        static void Arc(AdamWorldProjection projection, Vector2 center, float angle, float halfAngle, float radius, Color color, Stroke stroke)
        {
            var count = Mathf.CeilToInt(halfAngle / (Mathf.PI / 24));
            var start = center + Unit(angle - halfAngle) * radius;
            for (var i = 1; i <= count; i++)
            {
                var end = center + Unit(angle - halfAngle + 2 * halfAngle * i / count) * radius;
                if (stroke == Stroke.Solid || i % 2 != 0) Line(projection, start, end, color);
                start = end;
            }
        }

        static void Rail(AdamWorldProjection projection, Vector2 start, Vector2 end, float radius, Color color, Stroke stroke)
        {
            var side = Perpendicular((end - start).normalized) * radius;
            Line(projection, start + side, end + side, color, stroke);
            Line(projection, start - side, end - side, color, stroke);
        }

        static void Band(AdamWorldProjection projection, Vector2 start, Vector2 end, float width, Color color, Stroke stroke)
        {
            if ((end - start).sqrMagnitude <= 1e-9f) return;
            Rail(projection, start, end, width * .5f, color, stroke);
            var side = Perpendicular((end - start).normalized) * (width * .5f);
            Line(projection, start - side, start + side, color, stroke);
            Line(projection, end - side, end + side, color, stroke);
        }

        static void Line(AdamWorldProjection projection, Vector2 start, Vector2 end, Color color, Stroke stroke = Stroke.Solid)
        {
            var a = projection.Point(start.x, start.y);
            var b = projection.Point(end.x, end.y);
            var scale = projection.Scale;
            if (a == b || Mathf.Max(a.x, b.x) < -scale || Mathf.Max(a.y, b.y) < -scale ||
                Mathf.Min(a.x, b.x) > projection.Arena.width + scale || Mathf.Min(a.y, b.y) > projection.Arena.height + scale) return;

            var length = (b - a).magnitude;
            var dash = (stroke == Stroke.Dot ? 3 : 9) * scale;
            if (stroke == Stroke.Solid || length <= dash) Segment(a, b);
            else
            {
                var direction = (b - a) / length;
                for (var i = 0; i < length; i += dash + 4 * scale)
                    Segment(a + direction * i, a + direction * Mathf.Min(i + dash, length));
            }

            void Segment(Vector2 from, Vector2 to)
            {
                AdamVisualQualityWorld.Line(projection.Arena, from, to, 3 * scale, Rim);
                AdamVisualQualityWorld.Line(projection.Arena, from, to, scale, color);
            }
        }

        void Ring(AdamWorldProjection projection, Vector2 center, float radius, Color color, Stroke stroke)
        {
            var bounds = AdamDangerRing.Bounds(projection, center, radius);
            if (bounds.xMax < 0 || bounds.yMax < 0 || bounds.xMin > projection.Arena.width || bounds.yMin > projection.Arena.height) return;
            var index = (int)stroke;
            if (rings[index] == null) rings[index] = CreateRing(stroke);

            var previous = GUI.color;
            try
            {
                GUI.color = color;
                GUI.DrawTexture(bounds, rings[index], ScaleMode.StretchToFill, true);
            }
            finally { GUI.color = previous; }
        }

        static Texture2D CreateRing(Stroke stroke)
        {
            const int size = 192;
            const float radius = size / 2f;
            var image = new Texture2D(size, size, TextureFormat.RGBA32, false)
            {
                filterMode = FilterMode.Bilinear,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.HideAndDontSave,
            };
            var pixels = new Color32[size * size];
            for (var y = 0; y < size; y++)
                for (var x = 0; x < size; x++)
                {
                    var offset = new Vector2(x + .5f - radius, y + .5f - radius);
                    var inset = radius - offset.magnitude;
                    if (inset < 0 || inset > 3) continue;
                    if (stroke != Stroke.Solid)
                    {
                        var angle = Mathf.Atan2(offset.y, offset.x) + Mathf.PI;
                        var division = stroke == Stroke.Dash ? 24 : 32;
                        var fraction = angle / (2 * Mathf.PI) * division % 1;
                        if (fraction > (stroke == Stroke.Dash ? .64f : .25f)) continue;
                    }
                    pixels[y * size + x] = inset >= 1 && inset <= 2 ? new Color32(255, 255, 255, 255) : Rim;
                }
            image.SetPixels32(pixels);
            image.Apply(false, true);
            return image;
        }

        public void Dispose()
        {
            Clear();
            for (var i = 0; i < rings.Length; i++)
            {
                if (rings[i] == null) continue;
                if (Application.isPlaying) UnityEngine.Object.Destroy(rings[i]);
                else UnityEngine.Object.DestroyImmediate(rings[i]);
                rings[i] = null;
            }
        }
    }
}
