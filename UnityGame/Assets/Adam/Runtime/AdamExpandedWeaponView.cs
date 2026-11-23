using System;
using System.Collections.Generic;
using System.Linq;
using BibleSurvivor.Core;
using UnityEngine;

namespace BibleSurvivor.Unity
{
    // Technical outlines drawn from the combat checkpoint. Enemy telegraphs remain above these layers.
    internal sealed class AdamExpandedWeaponView : IDisposable
    {
        enum Stroke { Solid, Dash, Dot }

        const double MarkerLifetime = .14;
        const double AttackArtLifetime = .15 + 2d / 15;
        static readonly int FirstFadeFrame = 3;
        static readonly Color Rim = new Color32(29, 26, 20, 255);
        static readonly Color Voice = new Color32(255, 242, 178, 255);
        static readonly Color Lightning = new Color32(255, 252, 225, 255);
        static readonly Color Staff = new Color32(196, 229, 171, 255);
        static readonly Color Pillar = new Color32(250, 221, 134, 255);
        static readonly Color Lion = new Color32(224, 195, 144, 255);
        static readonly Color Cross = new Color32(245, 232, 200, 255);
        static readonly Color Chariot = new Color32(253, 226, 136, 255);

        readonly Texture2D[] rings = new Texture2D[3];
        readonly List<BattleAreaEffect> actualAreas = new List<BattleAreaEffect>();
        readonly List<(double At, Vector2 Center, float Radius, Stroke Stroke)> echoes =
            new List<(double, Vector2, float, Stroke)>();
        readonly List<(double At, Vector2 Center, float Radius, bool Guardian)> roars =
            new List<(double, Vector2, float, bool)>();
        readonly List<(double At, Vector2 Center, string Clip)> ended = new List<(double, Vector2, string)>();
        readonly Dictionary<long, Vector2> movingFire = new Dictionary<long, Vector2>();

        public void Clear()
        {
            actualAreas.Clear();
            echoes.Clear();
            roars.Clear();
            ended.Clear();
            movingFire.Clear();
        }

        public void Observe(BattleWorld source, BattleSnapshot previous, BattleSnapshot next, float packRoarRadius, float guardianRoarRadius)
        {
            source.ReadRecentAreaEffects(actualAreas);
            if (previous == null || next.Time <= previous.Time) return;
            ObserveEnds(previous, next);

            for (var i = echoes.Count - 1; i >= 0; i--)
                if (next.Time >= echoes[i].At + AttackArtLifetime) echoes.RemoveAt(i);
            if (previous.WildernessVoice?.Evolved == true && next.WildernessVoice?.Evolved == true)
                foreach (var cast in previous.WildernessVoice.Active)
                {
                    CaptureEcho(next, cast.StartedAt + .5, (float)cast.FirstEchoRadius, Stroke.Dash);
                    CaptureEcho(next, cast.StartedAt + 1, (float)cast.SecondEchoRadius, Stroke.Dot);
                    if (cast.HasExtraEcho) CaptureEcho(next, cast.StartedAt + 1.5, (float)cast.ExtraEchoRadius, Stroke.Solid);
                }

            var oldLion = previous.DanielLion;
            var newLion = next.DanielLion;
            roars.RemoveAll(roar => next.Time >= roar.At + MarkerLifetime);
            if (oldLion != null && newLion != null && oldLion.Evolved == newLion.Evolved)
            {
                if (newLion.Evolved && newLion.Lions.Count > 0 && newLion.NextRoarAt > oldLion.NextRoarAt &&
                    newLion.LastStepAt - oldLion.NextRoarAt < MarkerLifetime)
                    Roar(newLion.Lions[0], guardianRoarRadius, true);
                var representedLions = Math.Min(oldLion.Lions.Count, newLion.Lions.Count);
                for (var index = 0; index < representedLions; index++)
                {
                    var lion = newLion.Lions[index];
                    if (oldLion.Lions[index].NextPackRoarAt is double prior && lion.NextPackRoarAt > prior &&
                        newLion.LastStepAt - prior < MarkerLifetime)
                        Roar(lion, packRoarRadius, false);
                }
            }

            void Roar(ExpandedDanielLionActorState lion, float radius, bool guardian) =>
                roars.Add((next.Time, new Vector2(lion.X, lion.Y), radius, guardian));

            void CaptureEcho(BattleSnapshot snap, double due, float radius, Stroke stroke)
            {
                // The current anchor is valid when this fixed step resolved the due pulse.
                if (previous.Time < due && due <= snap.Time && snap.Time - due < BattleWorld.FixedStep)
                    echoes.Add((snap.Time, new Vector2(snap.PlayerX, snap.PlayerY), radius, stroke));
            }
        }

        void ObserveEnds(BattleSnapshot previous, BattleSnapshot next)
        {
            ended.RemoveAll(item => next.Time >= item.At + 2d / 15);
            movingFire.Clear();
            if (previous.FirePillar != null && next.FirePillar != null && previous.FirePillar.Evolved == next.FirePillar.Evolved)
                foreach (var prior in previous.FirePillar.Regions)
                {
                    var center = new Vector2(prior.X, prior.Y);
                    if (prior.Ember) continue;
                    var current = next.FirePillar.Regions.Find(region => region.Id == prior.Id);
                    if (current == null && previous.Time < prior.EndsAt && prior.EndsAt <= next.Time)
                        ended.Add((prior.EndsAt, center, "fx_fire-pillar_column-end_64"));
                    else if (current != null)
                    {
                        var delta = new Vector2(current.X, current.Y) - center;
                        if (delta.sqrMagnitude > 1e-10f) movingFire[prior.Id] = delta;
                    }
                }

            if (previous.MosesStaff != null && next.MosesStaff != null && previous.MosesStaff.Evolved == next.MosesStaff.Evolved)
                foreach (var wave in previous.MosesStaff.Waves)
                {
                    var endAt = wave.EmittedAt + wave.Range / wave.Speed;
                    if (previous.Time < endAt && endAt <= next.Time)
                    {
                        var distance = wave.TurnAtDistance.GetValueOrDefault();
                        var endpoint = wave.TurnAtDistance.HasValue
                            ? V(wave.TurnPosition) + V(wave.TravelDirection) * (float)(wave.Range - distance)
                            : V(wave.Origin) + V(wave.Direction) * (float)wave.Range;
                        ended.Add((endAt, endpoint, "fx_moses-staff_wave-end_32"));
                    }
                }

            if (previous.ElijahChariot != null && next.ElijahChariot != null && previous.ElijahChariot.Evolved == next.ElijahChariot.Evolved)
                foreach (var path in previous.ElijahChariot.Paths)
                    if (previous.Time < path.EndAt && path.EndAt <= next.Time)
                        ended.Add((path.EndAt, V(path.Endpoint), "fx_elijah-chariot_end_64"));
        }

        public void DrawGround(AdamWorldProjection projection, BattleSnapshot snap, bool feedback)
        {
            if (Event.current.type != EventType.Repaint) return;
            if (snap.WildernessVoice != null)
            {
                var player = new Vector2(snap.PlayerX, snap.PlayerY);
                foreach (var cast in snap.WildernessVoice.Active)
                {
                    var age = snap.Time - cast.StartedAt;
                    var initial = cast.PrimaryPulses <= 1 ? cast.InitialFacing : null;
                    var facing = Mathf.Atan2(cast.LockedFacingY ?? initial?.Y ?? snap.FacingY,
                        cast.LockedFacingX ?? initial?.X ?? snap.FacingX);
                    var warning = cast.PrimaryPulses == 0 ? Stroke.Dash : Stroke.Solid;
                    if (age < cast.Duration)
                    {
                        var facingDirection = new Vector2(Mathf.Cos(facing), Mathf.Sin(facing));
                        var firstDelay = cast.FirstDelay ?? .15;
                        var crown = player + facingDirection * (float)Math.Min(1, cast.Reach * .45);
                        if (age < firstDelay)
                            Paint(projection, "fx_wilderness-voice_mouth_32", crown, age,
                                left: facingDirection.x < 0, frame: age < firstDelay * .5 ? 0 : 1);
                        else if (cast.PrimaryPulses > 0)
                        {
                            var lastPulse = firstDelay + (cast.PrimaryPulses - 1) * (cast.ShortenedPrimaryInterval ? .48 : .5);
                            PaintAttack(projection, "fx_wilderness-voice_crest_32", crown, age - lastPulse, facingDirection.x < 0);
                        }
                        if (cast.LockedFacingX.HasValue)
                            Paint(projection, "fx_wilderness-voice_aim-lock_32", player + facingDirection * (float)cast.Reach, 0);
                        if (snap.WildernessVoice.Evolved) Ring(projection, player, (float)cast.Reach, Voice, warning);
                        else Fan(projection, player, facing, (float)cast.HalfAngle * Mathf.Deg2Rad, (float)cast.Reach, warning);
                    }
                    // Pulse shapes use the same live player anchor as the primary cast. These echo cues lead the due tick.
                    if (!snap.WildernessVoice.Evolved) continue;
                    if (cast.EchoesFired == 0 && age >= .5 - MarkerLifetime)
                        Ring(projection, player, (float)cast.FirstEchoRadius, Voice, Stroke.Dash);
                    if (cast.EchoesFired == 1 && age >= 1 - MarkerLifetime)
                        Ring(projection, player, (float)cast.SecondEchoRadius, Voice, Stroke.Dot);
                    if (cast.HasExtraEcho && cast.EchoesFired == 2 && age >= 1.5 - MarkerLifetime)
                        Ring(projection, player, (float)cast.ExtraEchoRadius, Voice, Stroke.Solid);
                }
            }
            foreach (var echo in echoes)
            {
                var age = snap.Time - echo.At;
                if (age < MarkerLifetime)
                    Ring(projection, echo.Center, echo.Radius, Voice, echo.Stroke);
                var id = echo.Stroke == Stroke.Dash ? 1 : echo.Stroke == Stroke.Dot ? 2 : 3;
                PaintAttack(projection, $"fx_wilderness-proclamation_echo-{id}_32",
                    echo.Center + Vector2.right * echo.Radius, age);
            }

            if (snap.Lightning != null)
            {
                foreach (var bolt in snap.Lightning.Pending)
                {
                    DrawBolt(projection, bolt, Stroke.Dash);
                    if (bolt.IsPrimary)
                        Paint(projection, "fx_lightning-mark_32", new Vector2(bolt.X, bolt.Y), 0,
                            frame: bolt.At - snap.Time > .075 ? 0 : 1);
                }
                if (snap.Lightning.GroundCurrents != null)
                    foreach (var ground in snap.Lightning.GroundCurrents)
                        Ring(projection, V(ground.Center), (float)ground.Radius, Lightning, Stroke.Dot);
            }
            foreach (var area in actualAreas)
            {
                if (snap.Time >= area.At + MarkerLifetime || area.Radius <= 0) continue;
                var color = area.Kind switch
                {
                    BattleAreaEffectKind.LightningStrike or BattleAreaEffectKind.LightningAftershock or
                        BattleAreaEffectKind.LightningEliteThunder => Lightning,
                    BattleAreaEffectKind.ChariotEndExplosion or BattleAreaEffectKind.ChariotCrossingWhirlwind or
                        BattleAreaEffectKind.ChariotEndFlame => Chariot,
                    _ => Cross,
                };
                Ring(projection, V(area.Center), (float)area.Radius, color, Stroke.Solid);
                if (area.Kind == BattleAreaEffectKind.LightningAftershock || area.Kind == BattleAreaEffectKind.LightningEliteThunder)
                    Ring(projection, V(area.Center), (float)area.Radius - .1f, Lightning, Stroke.Solid);
            }

            if (snap.MosesStaff != null)
                foreach (var wave in snap.MosesStaff.Waves)
                {
                    if (wave.HasLane && wave.LaneStartedAt.HasValue)
                    {
                        DrawStaffLane(projection, wave, wave.LaneLength, wave.LaneWidth, Stroke.Dash);
                        if (feedback)
                            Paint(projection, "fx_red-sea-path_channel_32", StaffPosition(wave, wave.LaneLength * .5),
                                snap.Time - wave.LaneStartedAt.Value);
                    }
                    if (wave.HasResidualLane && wave.ResidualLaneStartedAt.HasValue)
                        DrawStaffLane(projection, wave, wave.ResidualLaneLength, wave.ResidualLaneWidth, Stroke.Dot);
                }

            if (snap.FirePillar != null)
                foreach (var region in snap.FirePillar.Regions)
                {
                    var stroke = region.Ember ? Stroke.Dot : snap.Time < region.IgnitionAt ? Stroke.Dash : Stroke.Solid;
                    var center = new Vector2(region.X, region.Y);
                    Ring(projection, center, (float)region.Radius,
                        region.Ember ? Tint(Pillar, .64f) : Pillar, stroke);
                    if (region.Ember)
                    {
                        var remaining = region.EndsAt - snap.Time;
                        var frame = remaining < 2d / 15 ? (remaining < 1d / 15 ? 5 : 4) :
                            ((int)((snap.Time - region.StartedAt) * 8) % 4);
                        Paint(projection, "fx_wilderness-guidance_ember_32", center, 0, frame: frame);
                    }
                    else if (snap.Time < region.IgnitionAt)
                        Paint(projection, "fx_fire-pillar_ignite_64", center, 0,
                            frame: snap.Time - region.StartedAt < (region.IgnitionAt - region.StartedAt) * .5 ? 0 : 1);
                }

            if (snap.Cross != null)
                foreach (var shot in snap.Cross.Shots)
                {
                    DrawTrail(projection, shot.Thorns, (float)shot.ThornRadius, Tint(Cross, .68f), Stroke.Dash);
                    if (feedback && shot.Thorns.Count > 0)
                    {
                        var piece = shot.Thorns[shot.Thorns.Count - 1];
                        Paint(projection, "fx_passion-radiance_thorn_32", new Vector2(piece.EndX, piece.EndY), snap.Time);
                    }
                    if (shot.Afterglow != null)
                        DrawTrail(projection, shot.Afterglow, (float)shot.AfterglowRadius, Tint(Cross, .62f), Stroke.Dot);
                    if (shot.IsSpinning) Ring(projection, new Vector2(shot.X, shot.Y), (float)shot.SpinRadius, Cross, Stroke.Solid);
                }

            if (snap.ElijahChariot != null)
                foreach (var path in snap.ElijahChariot.Paths)
                {
                    var start = V(path.Origin);
                    if (snap.Time < path.WarningAt) continue;
                    if (snap.Time < path.MoveAt)
                    {
                        var latter = snap.Time - path.WarningAt >= (path.MoveAt - path.WarningAt) * .5;
                        var knownEnd = V(path.TurnsMidway && !path.MidpointTurnResolved ? path.Pivot : path.Endpoint);
                        Band(projection, start, knownEnd, (float)path.Width, Chariot, latter ? Stroke.Solid : Stroke.Dash);
                        Paint(projection, "fx_elijah-chariot_lane_32", start, 0, frame: latter ? 1 : 0);
                        Paint(projection, "fx_elijah-chariot_lane-end_32", knownEnd, 0, frame: latter ? 1 : 0);
                    }
                    else if (path.MakesBasicTrail || (snap.ElijahChariot.Evolved && !path.SuppressedTrail))
                    {
                        // Core expires each point individually as the dash lays the flame strip.
                        var expired = Mathf.Clamp((float)((snap.Time - path.MoveAt - path.TrailDuration) * path.Speed), 0, (float)path.Distance);
                        var trailStart = V(path.PositionAtDistance(expired));
                        var trailEnd = V(path.Position);
                        if (path.TurnDirection.HasValue && expired < path.PivotDistance && path.Distance > path.PivotDistance)
                        {
                            var pivot = V(path.Pivot);
                            Band(projection, trailStart, pivot, (float)path.TrailWidth, Chariot, Stroke.Dot);
                            trailStart = pivot;
                        }
                        Band(projection, trailStart, trailEnd, (float)path.TrailWidth, Chariot, Stroke.Dot);
                        Paint(projection, "fx_ascending-chariot_flame_32", V(path.Position), snap.Time);
                    }
                    if (path.MakesOriginFire && snap.Time >= path.MoveAt && snap.Time < path.MoveAt + path.OriginFireDuration)
                        Ring(projection, start, (float)path.OriginFireRadius, Chariot, Stroke.Dot);
                }
        }

        public void DrawActors(AdamWorldProjection projection, BattleSnapshot snap, bool feedback)
        {
            if (Event.current.type != EventType.Repaint) return;
            foreach (var area in actualAreas)
            {
                var age = snap.Time - area.At;
                var center = V(area.Center);
                if (area.Kind == BattleAreaEffectKind.LightningStrike)
                {
                    PaintAttack(projection, "fx_lightning-bolt_64", center, age);
                    if (feedback && age < .05) Paint(projection, "fx_lightning-flash_64", center, 0);
                }
                else if (area.Kind is BattleAreaEffectKind.LightningAftershock or BattleAreaEffectKind.LightningEliteThunder)
                    PaintAttack(projection, "fx_thunder-judgment_wave-spark_32", center + Vector2.right * (float)area.Radius, age);
                else if (area.Kind is BattleAreaEffectKind.CrossRecovery or BattleAreaEffectKind.CrossReturnSpin)
                {
                    if (age < 2d / 15) Paint(projection, "fx_cross_recover_32", center, age, frame: age < 1d / 15 ? 0 : 1);
                }
                else if (area.Kind == BattleAreaEffectKind.LightningChain)
                    PaintAttack(projection, "fx_thunder-judgment_chain-node_32", center, age);
                else if (area.Kind == BattleAreaEffectKind.LionClaw)
                    PaintAttack(projection, "fx_daniel-lion_claw_32", center, age);
                else if (age >= 0 && age < .15)
                {
                    if (area.Kind == BattleAreaEffectKind.StaffHit) Paint(projection, "fx_moses-staff_hit_32", center, age);
                    if (area.Kind == BattleAreaEffectKind.CrossHit) Paint(projection, "fx_cross_hit_32", center, age);
                }
            }
            foreach (var item in ended)
                Paint(projection, item.Clip, item.Center, snap.Time - item.At, frame: snap.Time - item.At < 1d / 15 ? 0 : 1);

            if (snap.MosesStaff != null)
                foreach (var wave in snap.MosesStaff.Waves)
                {
                    if (!wave.IsMoving) continue;
                    var center = V(wave.Position);
                    var direction = V(wave.TravelDirection);
                    Paint(projection, "wpn_moses-staff_wave_32", center, snap.Time - wave.EmittedAt, left: direction.x < 0);
                    var side = Perpendicular(direction) * (float)(wave.Width * .5);
                    Line(projection, center - side, center + side, Staff);
                    Line(projection, center - side, center - side - direction * .18f, Staff);
                    Line(projection, center + side, center + side - direction * .18f, Staff);
                }

            if (snap.FirePillar != null)
                foreach (var region in snap.FirePillar.Regions)
                {
                    if (region.Ember || snap.Time < region.IgnitionAt) continue;
                    var center = new Vector2(region.X, region.Y);
                    var move = movingFire.TryGetValue(region.Id, out var displacement);
                    Paint(projection, move ? "fx_wilderness-guidance_column-move_64" : "fx_fire-pillar_column_64",
                        center, snap.Time - region.IgnitionAt, left: move && displacement.x < 0);
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
                var frontGuardian = snap.DanielLion.Evolved ? snap.DanielLion.Lions.FirstOrDefault(actor => actor.Index == 0) : null;
                var player = new Vector2(snap.PlayerX, snap.PlayerY);
                foreach (var lion in snap.DanielLion.Lions.OrderByDescending(actor => actor.Y).ThenByDescending(actor => actor.Index))
                {
                    var guardian = snap.DanielLion.Evolved && lion.Index == 0;
                    var center = new Vector2(lion.X, lion.Y);
                    var radius = guardian ? .28f : .20f;
                    var target = player;
                    if (!lion.Returning && EnemyPosition(snap, lion.TargetId, out var chosen)) target = chosen;
                    var pose = guardian ? (lion.Returning ? "wpn_den-guardian_return_48" : "wpn_den-guardian_walk_48")
                        : (lion.Returning ? "wpn_daniel-lion_return_32" : "wpn_daniel-lion_walk_32");
                    var age = snap.Time;
                    foreach (var roar in roars)
                    {
                        if (roar.Guardian != guardian || (roar.Center - center).sqrMagnitude > .05f) continue;
                        pose = guardian ? "wpn_den-guardian_roar_48" : "wpn_daniel-lion_roar_32";
                        age = snap.Time - roar.At;
                        break;
                    }
                    if (guardian && snap.DanielLion.Leap is { } leap && leap.At - snap.Time < .15)
                    {
                        pose = "wpn_den-guardian_leap_48";
                        age = Math.Max(0, snap.Time - (leap.At - .15));
                    }
                    var display = !guardian && frontGuardian != null
                        ? LionDisplayFoot(center, new Vector2(frontGuardian.X, frontGuardian.Y), player) : center;
                    if (display != center)
                        Line(projection, center, display, Tint(Lion, .65f), Stroke.Dot);
                    if (!Paint(projection, pose, display, age, left: target.x < center.x))
                        Ring(projection, center, radius, Lion, lion.Returning ? Stroke.Dash : Stroke.Solid);
                    Line(projection, center - Vector2.right * radius * .55f, center + Vector2.right * radius * .55f, Lion);
                }
                if (snap.DanielLion.Leap != null)
                    Ring(projection, new Vector2(snap.DanielLion.Leap.OriginX, snap.DanielLion.Leap.OriginY), 2.5f, Lion, Stroke.Dot);
                foreach (var roar in roars)
                {
                    Ring(projection, roar.Center, roar.Radius, Lion, roar.Guardian ? Stroke.Solid : Stroke.Dash);
                    Paint(projection, "fx_daniel-lion_roar-piece_32", roar.Center + Vector2.right * roar.Radius,
                        snap.Time - roar.At);
                }
            }

            if (snap.Cross != null)
                foreach (var shot in snap.Cross.Shots)
                {
                    if (!shot.IsFlying) continue;
                    var center = new Vector2(shot.X, shot.Y);
                    var returning = shot.ReturnStartedAt.HasValue;
                    var direction = returning && !shot.CurvedReturn
                        ? (new Vector2(snap.PlayerX, snap.PlayerY) - center).normalized
                        : new Vector2(shot.DirectionX, shot.DirectionY);
                    Paint(projection, returning ? "wpn_cross_fly-back_32" : "wpn_cross_fly-out_32", center,
                        snap.Time - (shot.ReturnStartedAt ?? shot.EmittedAt), left: direction.x < 0);
                    var outer = Mathf.Max(.10f, (float)shot.Radius);
                    var horizontal = Vector2.right * outer;
                    var vertical = Vector2.up * outer;
                    Line(projection, center - horizontal, center + horizontal, Cross);
                    Line(projection, center - vertical, center + vertical, Cross);
                    var hits = returning ? shot.ReturnHits : shot.OutboundHits;
                    var budget = returning ? shot.ReturnBudget(snap.Cross.Evolved) : shot.HitBudget(snap.Cross.Evolved);
                    var streaks = hits >= budget ? 1 : 3;
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
                    var direction = V(path.CurrentDirection);
                    Paint(projection, "wpn_elijah-chariot_body_64", center, snap.Time - path.MoveAt, left: direction.x < 0);
                    var side = Perpendicular(direction) * (float)(path.Width * .5);
                    Line(projection, center - side, center + side, Chariot);
                    Line(projection, center - side - direction * .32f, center - side, Chariot);
                    Line(projection, center + side - direction * .32f, center + side, Chariot);
                    if (snap.ElijahChariot.Evolved)
                    {
                        Ring(projection, center, (float)path.WhirlwindRadius, Chariot, Stroke.Solid);
                        Paint(projection, "fx_ascending-chariot_whirl-piece_32",
                            center + Vector2.right * (float)path.WhirlwindRadius, snap.Time - path.MoveAt);
                    }
                }
        }

        static Vector2 StaffPosition(ExpandedMosesStaffWaveState wave, double distance)
        {
            if (wave.TurnAtDistance is double turn && distance > turn)
                return V(wave.TurnPosition) + V(wave.TravelDirection) * (float)(distance - turn);
            return V(wave.Origin) + V(wave.Direction) * (float)distance;
        }

        static Vector2 LionDisplayFoot(Vector2 lion, Vector2 guardian, Vector2 player)
        {
            var delta = lion - guardian;
            const float width = .88f;
            const float height = .70f;
            var x = Mathf.Abs(delta.x);
            var y = Mathf.Abs(delta.y);
            if (x >= width || y >= height) return lion;
            var side = x > .12f ? Mathf.Sign(delta.x) : guardian.x >= player.x ? 1f : -1f;
            var offset = (width - x) * (1 - y / height);
            return lion + Vector2.right * (side * offset);
        }

        static Color Tint(Color color, float alpha)
        {
            color.a *= alpha;
            return color;
        }

        void DrawStaffLane(AdamWorldProjection projection, ExpandedMosesStaffWaveState wave, double length,
            double width, Stroke stroke)
        {
            var origin = V(wave.Origin);
            var end = StaffPosition(wave, length);
            var color = Tint(Staff, stroke == Stroke.Dot ? .60f : .76f);
            if (wave.TurnAtDistance is double turn && turn < length)
            {
                var pivot = V(wave.TurnPosition);
                Band(projection, origin, pivot, (float)width, color, stroke);
                Band(projection, pivot, end, (float)width, color, stroke);
            }
            else Band(projection, origin, end, (float)width, color, stroke);
        }

        static void PaintAttack(AdamWorldProjection projection, string id, Vector2 anchor, double age, bool left = false)
        {
            if (age < 0 || age >= AttackArtLifetime) return;
            var frame = age < .15 ? Math.Min(2, (int)(age * 20)) : FirstFadeFrame + Math.Min(1, (int)((age - .15) * 15));
            Paint(projection, id, anchor, age, left, frame);
        }

        static bool Paint(AdamWorldProjection projection, string id, Vector2 anchor, double age, bool left = false, int frame = -1)
        {
            if (!AdamVisualQualityArt.TryGetClip(id, out var clip)) return false;
            if (age < 0) return true;
            var sprite = AdamVisualQualityArt.Sprite(id, frame < 0 ? clip.Frame(age) : frame);
            var target = AdamArt.PlacedRect(clip, projection.Point(anchor.x, anchor.y), projection.Scale);
            AdamArt.Blit(sprite, target, left);
            return true;
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
            if (!bolt.IsPrimary) Ring(projection, center, radius - .10f, Lightning, stroke);
        }

        void DrawTrail(AdamWorldProjection projection, List<ExpandedCrossThornState> pieces, float radius, Color color, Stroke stroke)
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
                Rail(projection, start, end, radius, color, stroke);
                // Endcaps close the capsule without drawing inner boundaries along a contiguous leg.
                if (i == pieces.Count || (new Vector2(pieces[i].StartX, pieces[i].StartY) - end).sqrMagnitude > 1e-9f)
                    Arc(projection, end, Mathf.Atan2(direction.y, direction.x), Mathf.PI / 2, radius, color, stroke);
                if (first == 0 || (new Vector2(pieces[first - 1].EndX, pieces[first - 1].EndY) - start).sqrMagnitude > 1e-9f)
                    Arc(projection, start, Mathf.Atan2(-direction.y, -direction.x), Mathf.PI / 2, radius, color, stroke);
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
                AdamVisualQualityWorld.Line(projection.Arena, from, to, 3 * scale, Tint(Rim, color.a));
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
