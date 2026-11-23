using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.Serialization;

namespace BibleSurvivor.Core
{
    public enum ExpandedMosesStaffHitKind
    {
        Wave,
        Lane,
    }

    public readonly struct ExpandedMosesStaffHit
    {
        public ExpandedMosesStaffHitKind Kind { get; }
        public int TargetId { get; }
        public Vector2 Position { get; }
        public Vector2 Origin { get; }
        public Vector2 Direction { get; }
        public double Damage { get; }
        public double Knockback { get; }
        public long WaveId { get; }
        public long Cycle { get; }

        internal ExpandedMosesStaffHit(ExpandedMosesStaffHitKind kind, int targetId, Vector2 position, Vector2 origin,
            ExpandedMosesStaffWaveState wave)
        {
            Kind = kind;
            TargetId = targetId;
            Position = position;
            Origin = origin;
            Direction = wave.Direction;
            Damage = kind == ExpandedMosesStaffHitKind.Wave ? wave.Damage : wave.Damage * ExpandedMosesStaff.LaneDamageFactor;
            Knockback = kind == ExpandedMosesStaffHitKind.Wave ? ExpandedMosesStaff.Profile.Knockback : 0;
            WaveId = wave.Id;
            Cycle = wave.Cycle;
        }
    }

    [DataContract]
    public sealed class ExpandedMosesStaffWaveState
    {
        [DataMember(IsRequired = true)] public long Id { get; set; }
        [DataMember(IsRequired = true)] public long Cycle { get; set; }
        [DataMember(IsRequired = true)] public double EmittedAt { get; set; }
        [DataMember(IsRequired = true)] public double LastStepAt { get; set; }
        [DataMember(IsRequired = true)] public float OriginX { get; set; }
        [DataMember(IsRequired = true)] public float OriginY { get; set; }
        [DataMember(IsRequired = true)] public float DirectionX { get; set; }
        [DataMember(IsRequired = true)] public float DirectionY { get; set; }
        [DataMember(IsRequired = true)] public double Damage { get; set; }
        [DataMember(IsRequired = true)] public double Width { get; set; }
        [DataMember(IsRequired = true)] public double Range { get; set; }
        [DataMember(IsRequired = true)] public double Speed { get; set; }
        [DataMember(IsRequired = true)] public double Distance { get; set; }
        [DataMember(IsRequired = true)] public bool MakesLane { get; set; }
        [DataMember(EmitDefaultValue = false)] public double? LaneStartedAt { get; set; }
        [DataMember(EmitDefaultValue = false)] public int LaneTicks { get; set; }
        [DataMember(IsRequired = true)] public List<int> HitIds { get; set; } = new List<int>();

        public Vector2 Origin => new Vector2(OriginX, OriginY);
        public Vector2 Direction => new Vector2(DirectionX, DirectionY);
        public Vector2 Position => Origin + Direction * (float)Distance;
        public bool IsMoving => Distance < Range;
        public bool HasLane => MakesLane && LaneTicks <= ExpandedMosesStaff.LaneTickCount;
        public double LaneLength => HasLane ? Math.Min(Distance, ExpandedMosesStaff.MaximumLaneLength) : 0;

        internal ExpandedMosesStaffWaveState Copy()
        {
            var copy = (ExpandedMosesStaffWaveState)MemberwiseClone();
            copy.HitIds = new List<int>(HitIds);
            return copy;
        }
    }

    [DataContract]
    public sealed class ExpandedMosesStaffState
    {
        [DataMember(IsRequired = true)] public bool Evolved { get; set; }
        [DataMember(IsRequired = true)] public long Cycle { get; set; }
        [DataMember(IsRequired = true)] public long NextWaveId { get; set; } = 1;
        [DataMember(IsRequired = true)] public double NextCycle { get; set; }
        [DataMember(IsRequired = true)] public List<ExpandedMosesStaffWaveState> Waves { get; set; } =
            new List<ExpandedMosesStaffWaveState>();

        public int ActiveWaveCount => Waves.Count(wave => wave.IsMoving);
        public int ActiveLaneCount => Waves.Count(wave => wave.HasLane);

        public ExpandedMosesStaffState Copy() => new ExpandedMosesStaffState
        {
            Evolved = Evolved,
            Cycle = Cycle,
            NextWaveId = NextWaveId,
            NextCycle = NextCycle,
            Waves = Waves.Select(wave => wave.Copy()).ToList(),
        };

        public void ResetForForm(bool evolved, double now)
        {
            if (!ExpandedMosesStaff.Finite(now) || now < 0) throw new ArgumentOutOfRangeException(nameof(now));
            Evolved = evolved;
            NextCycle = now;
            Waves.Clear();
        }

        public void Validate(double now, bool evolved)
        {
            if (!ExpandedMosesStaff.Finite(now) || now < 0 || Evolved != evolved || Cycle < 0 || NextWaveId < 1 ||
                (NextWaveId > 1 && (NextWaveId - 2) / ExpandedMosesStaff.MaximumAmount >= Cycle) ||
                !ExpandedMosesStaff.Finite(NextCycle) || NextCycle < 0 ||
                NextCycle > now + ExpandedMosesStaff.MaximumCycle + ExpandedMosesStaff.TimeEpsilon ||
                Waves == null || Waves.Count > ExpandedMosesStaff.MaximumMovingWaves + ExpandedMosesStaff.MaximumLanes)
                throw new ArgumentException("Invalid expanded staff clock");

            long lastId = 0;
            var moving = 0;
            var lanes = 0;
            foreach (var wave in Waves)
            {
                if (wave == null || wave.Id <= lastId || wave.Id >= NextWaveId || wave.Cycle < 1 || wave.Cycle > Cycle ||
                    !ExpandedMosesStaff.ValidPosition(wave.Origin) || !ExpandedMosesStaff.ValidPosition(wave.Position) ||
                    !ExpandedMosesStaff.UnitDirection(wave.Direction) ||
                    !ExpandedMosesStaff.Finite(wave.EmittedAt) || wave.EmittedAt < 0 || wave.EmittedAt > now + ExpandedMosesStaff.TimeEpsilon ||
                    !ExpandedMosesStaff.Finite(wave.LastStepAt) || wave.LastStepAt < wave.EmittedAt ||
                    wave.LastStepAt > now + ExpandedMosesStaff.TimeEpsilon ||
                    !ExpandedMosesStaff.Finite(wave.Damage) || wave.Damage < 1 || wave.Damage > ExpandedMosesStaff.MaximumDamage ||
                    !ExpandedMosesStaff.Finite(wave.Width) || wave.Width <= 0 || wave.Width > ExpandedMosesStaff.MaximumWidth ||
                    !ExpandedMosesStaff.ValidMotion(wave.Range, wave.Speed) || !ExpandedMosesStaff.Finite(wave.Distance) ||
                    wave.Distance < 0 || wave.Distance > wave.Range ||
                    Math.Abs(wave.Distance - Math.Min(wave.Range, wave.Speed * (wave.LastStepAt - wave.EmittedAt))) > ExpandedMosesStaff.DistanceEpsilon ||
                    wave.HitIds == null || wave.HitIds.Count > ExpandedMosesStaff.MaximumLiveTargets ||
                    wave.HitIds.Any(id => id < 1) || wave.HitIds.Distinct().Count() != wave.HitIds.Count ||
                    (!wave.IsMoving && !wave.HasLane))
                    throw new ArgumentException("Invalid expanded staff wave");

                if (wave.IsMoving && (++moving > ExpandedMosesStaff.MaximumMovingWaves ||
                    wave.LastStepAt < now - ExpandedMosesStaff.TimeEpsilon))
                    throw new ArgumentException("Invalid expanded staff travel");
                if (!wave.MakesLane)
                {
                    if (wave.LaneStartedAt.HasValue || wave.LaneTicks != 0)
                        throw new ArgumentException("Unexpected expanded staff lane");
                }
                else
                {
                    if (!Evolved || wave.LaneTicks < 0 || wave.LaneTicks > ExpandedMosesStaff.ExpiredLane ||
                        (wave.HasLane && ++lanes > ExpandedMosesStaff.MaximumLanes))
                        throw new ArgumentException("Invalid expanded staff lane");
                    if (!wave.LaneStartedAt.HasValue)
                    {
                        if (wave.Distance != 0 || wave.LaneTicks != 0)
                            throw new ArgumentException("Expanded staff lane has no starting tick");
                    }
                    else
                    {
                        var start = wave.LaneStartedAt.Value;
                        var elapsed = now - start + ExpandedMosesStaff.TimeEpsilon;
                        var expectedTicks = elapsed >= ExpandedMosesStaff.LaneDuration ? ExpandedMosesStaff.ExpiredLane :
                            1 + (int)Math.Floor(elapsed / ExpandedMosesStaff.LaneInterval);
                        if (!ExpandedMosesStaff.Finite(start) || start <= wave.EmittedAt || start > wave.LastStepAt ||
                            expectedTicks != wave.LaneTicks)
                            throw new ArgumentException("Invalid expanded staff lane schedule");
                    }
                }
                lastId = wave.Id;
            }
        }
    }

    public static class ExpandedMosesStaff
    {
        internal static readonly ExpandedWeaponProfile Profile = ExpandedWeaponRules.Get("moses_staff");
        internal static readonly int MaximumAmount = Profile.MaximumOriginalAmount;
        internal static readonly double MaximumCycle = Profile.AtLevel(1).Cycle;
        internal static readonly double MaximumDamage = Profile.AtLevel(8).Damage * 2.25;
        internal static readonly double MaximumWidth = Profile.AtLevel(1).Width * 2.5;
        internal static readonly double MaximumLaneLength = Profile.TravelDistance;
        private static readonly double MaximumRange = Profile.TravelDistance * 2.5;
        private static readonly double MinimumSpeed = Profile.AtLevel(1).Speed;
        private static readonly double MaximumSpeed = Profile.AtLevel(8).Speed * 1.75;
        private const double MinimumCycle = .15;
        internal const double TimeEpsilon = 1e-9;
        internal const double DistanceEpsilon = 1e-6;
        internal const double LaneInterval = .5;
        internal const double LaneDuration = 2;
        internal const int LaneTickCount = (int)(LaneDuration / LaneInterval);
        internal const int ExpiredLane = LaneTickCount + 1;
        internal const int MaximumLanes = 16;
        internal const int MaximumLiveTargets = 120;
        internal static readonly int MaximumMovingWaves = MaximumAmount * ((int)Math.Ceiling(MaximumRange / MinimumSpeed / MinimumCycle) + 1);
        private const double LaneWidth = 1.2;
        internal const double LaneDamageFactor = .35;

        // The caller owns live targets and damage. The result counts waves actually emitted.
        // The optional reservations let the world apply shared projectile and area caps.
        public static int Step(ExpandedMosesStaffState state, double now, Vector2 playerPosition, Vector2 facing, int level,
            bool evolved, double damage, double cycle, int amount, double widthBeforeForm, double range, double speed,
            Func<IReadOnlyDictionary<int, Vector2>> targets, IReadOnlyDictionary<int, Vector2> previousTargets,
            Func<ExpandedMosesStaffHit, bool> applyHit, Func<bool>? reserveOriginal = null, Func<bool>? reserveLane = null)
        {
            if (state == null) throw new ArgumentNullException(nameof(state));
            if (targets == null) throw new ArgumentNullException(nameof(targets));
            if (previousTargets == null) throw new ArgumentNullException(nameof(previousTargets));
            if (applyHit == null) throw new ArgumentNullException(nameof(applyHit));
            if (!Finite(now) || now < 0 || !ValidPosition(playerPosition) || !ValidPosition(facing) ||
                level < 1 || level > 8 || (evolved && level != 8) ||
                !Finite(damage) || damage < 1 || damage > MaximumDamage ||
                !Finite(cycle) || cycle < MinimumCycle || cycle > MaximumCycle ||
                amount < 1 || amount > MaximumAmount || !Finite(widthBeforeForm) || widthBeforeForm <= 0 ||
                widthBeforeForm > MaximumWidth || !ValidMotion(range, speed))
                throw new ArgumentException("Invalid expanded staff parameters");

            if (state.Evolved != evolved) state.ResetForForm(evolved, now);
            if (state.Waves.Count > 0)
            {
                foreach (var wave in state.Waves)
                    AdvanceWave(wave, now, targets, previousTargets, applyHit);
                ResolveLanes(state.Waves, now, targets, previousTargets, applyHit);
                state.Waves.RemoveAll(wave => !wave.IsMoving && !wave.HasLane);
            }
            if (state.NextCycle > now + TimeEpsilon) return 0;

            state.Cycle = checked(state.Cycle + 1);
            state.NextCycle += cycle;
            var width = Math.Min(MaximumWidth, widthBeforeForm * (evolved ? 1.2 : 1));
            var forward = facing.LengthSquared() > 0 ? Vector2.Normalize(facing) : Vector2.UnitX;
            var moving = state.ActiveWaveCount;
            var lanes = state.ActiveLaneCount;
            var emitted = 0;
            for (var index = 0; index < amount; index++)
            {
                if (moving + emitted >= MaximumMovingWaves || (reserveOriginal != null && !reserveOriginal())) break;
                var angle = (index - (amount - 1) * .5) * Profile.SpreadDegrees * Math.PI / 180;
                var direction = Rotate(forward, angle);
                var makesLane = evolved && lanes < MaximumLanes && (reserveLane?.Invoke() ?? true);
                if (makesLane) lanes++;
                state.Waves.Add(new ExpandedMosesStaffWaveState
                {
                    Id = state.NextWaveId,
                    Cycle = state.Cycle,
                    EmittedAt = now,
                    LastStepAt = now,
                    OriginX = playerPosition.X,
                    OriginY = playerPosition.Y,
                    DirectionX = direction.X,
                    DirectionY = direction.Y,
                    Damage = damage,
                    Width = width,
                    Range = range,
                    Speed = speed,
                    MakesLane = makesLane,
                });
                state.NextWaveId = checked(state.NextWaveId + 1);
                emitted++;
            }
            return emitted;
        }

        private static void AdvanceWave(ExpandedMosesStaffWaveState wave, double now,
            Func<IReadOnlyDictionary<int, Vector2>> targets, IReadOnlyDictionary<int, Vector2> previousTargets,
            Func<ExpandedMosesStaffHit, bool> applyHit)
        {
            var duration = now - wave.LastStepAt;
            if (!wave.IsMoving || duration <= TimeEpsilon) return;
            wave.LastStepAt = now;
            var from = wave.Position;
            var previousDistance = wave.Distance;
            wave.Distance = Math.Min(wave.Range, (now - wave.EmittedAt) * wave.Speed);
            var to = wave.Position;
            var fractionOfStep = Math.Min(1, (wave.Distance - previousDistance) / (wave.Speed * duration));
            var live = targets();
            wave.HitIds.RemoveAll(id => !live.ContainsKey(id));
            foreach (var id in live.Keys.OrderBy(id => id).ToArray())
            {
                if (wave.HitIds.Contains(id) || !targets().TryGetValue(id, out var current)) continue;
                var previous = previousTargets.TryGetValue(id, out var old) ? old : current;
                var endTarget = Vector2.Lerp(previous, current, (float)fractionOfStep);
                if (!SweptContact(from - previous, to - endTarget, wave.Width / 2, out var contact)) continue;
                if (applyHit(new ExpandedMosesStaffHit(ExpandedMosesStaffHitKind.Wave, id, current, Vector2.Lerp(from, to, contact), wave)))
                    wave.HitIds.Add(id);
            }
            // One lane starts on the first tick that leaves a trace. Its lifetime belongs
            // to that lane, while its length grows behind the wave up to seven units.
            if (wave.MakesLane && !wave.LaneStartedAt.HasValue)
                wave.LaneStartedAt = now;
        }

        private static void ResolveLanes(IReadOnlyList<ExpandedMosesStaffWaveState> waves, double now,
            Func<IReadOnlyDictionary<int, Vector2>> targets, IReadOnlyDictionary<int, Vector2> previousTargets,
            Func<ExpandedMosesStaffHit, bool> applyHit)
        {
            var due = new List<ExpandedMosesStaffWaveState>();
            foreach (var wave in waves)
            {
                if (!wave.HasLane || !wave.LaneStartedAt.HasValue) continue;
                var start = wave.LaneStartedAt.Value;
                if (start + LaneDuration <= now + TimeEpsilon)
                {
                    wave.LaneTicks = ExpiredLane;
                    continue;
                }
                var next = start + wave.LaneTicks * LaneInterval;
                if (wave.LaneTicks == LaneTickCount || next > now + TimeEpsilon) continue;
                do { wave.LaneTicks++; }
                while (wave.LaneTicks < LaneTickCount && start + wave.LaneTicks * LaneInterval <= now + TimeEpsilon);
                due.Add(wave);
            }
            if (due.Count == 0) return;

            // Physical contacts use the live target map before support damage. Resolve
            // same-tick lane overlaps once per target, then apply winners in wave/target order.
            var winners = new Dictionary<int, ExpandedMosesStaffWaveState>();
            var live = targets();
            foreach (var wave in due.OrderByDescending(wave => wave.Damage).ThenBy(wave => wave.Id))
            {
                var length = Math.Min(wave.Distance, MaximumLaneLength);
                foreach (var pair in live)
                {
                    if (winners.ContainsKey(pair.Key)) continue;
                    var previous = previousTargets.TryGetValue(pair.Key, out var old) ? old : pair.Value;
                    if (CrossesLane(previous, pair.Value, wave, length)) winners.Add(pair.Key, wave);
                }
            }
            foreach (var hit in winners.OrderBy(entry => entry.Value.Id).ThenBy(entry => entry.Key))
                if (targets().TryGetValue(hit.Key, out var position))
                    applyHit(new ExpandedMosesStaffHit(ExpandedMosesStaffHitKind.Lane, hit.Key, position, hit.Value.Origin, hit.Value));
        }

        private static bool SweptContact(Vector2 from, Vector2 to, double radius, out float contact)
        {
            var dx = (double)to.X - from.X;
            var dy = (double)to.Y - from.Y;
            var c = (double)from.X * from.X + (double)from.Y * from.Y - radius * radius;
            contact = 0;
            if (c <= DistanceEpsilon) return true;
            var a = dx * dx + dy * dy;
            if (a <= 0) return false;
            var b = from.X * dx + from.Y * dy;
            var discriminant = b * b - a * c;
            if (discriminant < 0) return false;
            var fraction = (-b - Math.Sqrt(discriminant)) / a;
            if (fraction < 0 || fraction > 1) return false;
            contact = (float)fraction;
            return true;
        }

        private static bool CrossesLane(Vector2 previous, Vector2 current, ExpandedMosesStaffWaveState wave, double length)
        {
            var from = previous - wave.Origin;
            var to = current - wave.Origin;
            var direction = wave.Direction;
            var perpendicular = new Vector2(-direction.Y, direction.X);
            double first = 0, last = 1;
            return ClipAxis(Vector2.Dot(from, direction), Vector2.Dot(to, direction), 0, length, ref first, ref last) &&
                ClipAxis(Vector2.Dot(from, perpendicular), Vector2.Dot(to, perpendicular), -LaneWidth / 2, LaneWidth / 2, ref first, ref last);
        }

        private static bool ClipAxis(double from, double to, double minimum, double maximum, ref double first, ref double last)
        {
            minimum -= DistanceEpsilon;
            maximum += DistanceEpsilon;
            var delta = to - from;
            if (delta == 0) return from >= minimum && from <= maximum;
            var a = (minimum - from) / delta;
            var b = (maximum - from) / delta;
            first = Math.Max(first, Math.Min(a, b));
            last = Math.Min(last, Math.Max(a, b));
            return first <= last;
        }

        private static Vector2 Rotate(Vector2 direction, double angle) => new Vector2(
            (float)(direction.X * Math.Cos(angle) - direction.Y * Math.Sin(angle)),
            (float)(direction.X * Math.Sin(angle) + direction.Y * Math.Cos(angle)));

        internal static bool Finite(double number) => !double.IsNaN(number) && !double.IsInfinity(number);
        internal static bool ValidPosition(Vector2 value) =>
            Finite(value.X) && Finite(value.Y) && Math.Abs(value.X) <= 1e6 && Math.Abs(value.Y) <= 1e6;
        internal static bool UnitDirection(Vector2 value) => ValidPosition(value) && Math.Abs(value.LengthSquared() - 1) <= 1e-5;
        internal static bool ValidMotion(double range, double speed) =>
            Finite(range) && range > 0 && range <= MaximumRange &&
            Finite(speed) && speed >= MinimumSpeed && speed <= MaximumSpeed;
    }
}
