using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.Serialization;

namespace BibleSurvivor.Core
{
    public readonly struct ExpandedBibleSupportRanks
    {
        public bool ContactPages { get; }
        public bool Ink { get; }
        public bool PiercePages { get; }
        public bool Bookmarks { get; }
        public bool ClosingPages { get; }
        public bool ReturningBooks { get; }
        public bool InactiveGlow { get; }

        private ExpandedBibleSupportRanks(IReadOnlyDictionary<string, int> ranks)
        {
            bool Has(string id) => ranks.TryGetValue("weapon.bible." + id, out var rank) && rank > 0;
            ContactPages = Has("c.01");
            Ink = Has("c.02");
            PiercePages = Has("c.03");
            Bookmarks = Has("c.04");
            ClosingPages = Has("c.05");
            ReturningBooks = Has("d.03");
            InactiveGlow = Has("d.04");
        }

        public static ExpandedBibleSupportRanks FromRanks(IReadOnlyDictionary<string, int> ranks) =>
            new ExpandedBibleSupportRanks(ranks ?? throw new ArgumentNullException(nameof(ranks)));
    }

    public enum ExpandedBibleSupportKind : byte
    {
        Page, ReturningBook, Ink, InactiveGlow,
    }

    public readonly struct ExpandedBibleSupportHit
    {
        public int TargetId { get; }
        public Vector2 Position { get; }
        public double Damage { get; }
        public ExpandedBibleSupportKind Kind { get; }

        internal ExpandedBibleSupportHit(int targetId, Vector2 position, double damage, ExpandedBibleSupportKind kind)
        {
            TargetId = targetId;
            Position = position;
            Damage = damage;
            Kind = kind;
        }
    }

    [DataContract]
    public sealed class ExpandedBibleSupportProjectile
    {
        [DataMember(IsRequired = true)] public long Id { get; set; }
        [DataMember(EmitDefaultValue = false)] public bool Returning { get; set; }
        [DataMember(IsRequired = true)] public float X { get; set; }
        [DataMember(IsRequired = true)] public float Y { get; set; }
        [DataMember(IsRequired = true)] public float DirectionX { get; set; }
        [DataMember(IsRequired = true)] public float DirectionY { get; set; }
        [DataMember(IsRequired = true)] public double LastAt { get; set; }
        [DataMember(IsRequired = true)] public double Travelled { get; set; }
        [DataMember(IsRequired = true)] public double ParentDamage { get; set; }
        [DataMember(IsRequired = true)] public int HitBudget { get; set; }
        [DataMember(EmitDefaultValue = false)] public List<int>? HitIds { get; set; }
        [DataMember(EmitDefaultValue = false)] public bool InkEligible { get; set; }
        [DataMember(EmitDefaultValue = false)] public bool InkAttempted { get; set; }
        [DataMember(EmitDefaultValue = false)] public bool BookmarkEligible { get; set; }

        public Vector2 Position { get => new Vector2(X, Y); internal set { X = value.X; Y = value.Y; } }
        internal Vector2 Direction { get => new Vector2(DirectionX, DirectionY); set { DirectionX = value.X; DirectionY = value.Y; } }
        public double Radius => Returning ? .25 : .10;
        public double Speed => Returning ? 6 : 7;
        public double Range => 3;

        internal ExpandedBibleSupportProjectile Copy()
        {
            var copy = (ExpandedBibleSupportProjectile)MemberwiseClone();
            copy.HitIds = HitIds?.ToList();
            return copy;
        }
    }

    [DataContract]
    public sealed class ExpandedBibleInk
    {
        [DataMember(IsRequired = true)] public long Id { get; set; }
        [DataMember(IsRequired = true)] public float StartX { get; set; }
        [DataMember(IsRequired = true)] public float StartY { get; set; }
        [DataMember(IsRequired = true)] public float EndX { get; set; }
        [DataMember(IsRequired = true)] public float EndY { get; set; }
        [DataMember(IsRequired = true)] public double BornAt { get; set; }
        [DataMember(IsRequired = true)] public double NextHitAt { get; set; }
        [DataMember(IsRequired = true)] public double Damage { get; set; }

        public const double Width = .35;
        public const double Lifetime = 1;
        public Vector2 Start => new Vector2(StartX, StartY);
        public Vector2 End { get => new Vector2(EndX, EndY); internal set { EndX = value.X; EndY = value.Y; } }
        internal ExpandedBibleInk Copy() => (ExpandedBibleInk)MemberwiseClone();
    }

    [DataContract]
    public sealed class ExpandedBibleBookmark
    {
        [DataMember(IsRequired = true)] public int TargetId { get; set; }
        [DataMember(IsRequired = true)] public double ExpiresAt { get; set; }

        internal ExpandedBibleBookmark Copy() => (ExpandedBibleBookmark)MemberwiseClone();
    }

    [DataContract]
    public sealed class ExpandedBibleSupportsState
    {
        private const double Epsilon = 1e-9;
        internal const int GroupLimit = 4;
        private const double HitPageCooldown = 1;
        private const double ClosingPageCooldown = 4;
        private const double BookmarkLifetime = 2;
        private const double PageFactor = .35;
        private const double BookmarkFactor = .12;
        private const double InkFactor = .15;
        private const double ReturnFactor = .30;
        public const double GlowRadius = 1.2;

        [DataMember(IsRequired = true)] public long NextId { get; set; } = 1;
        [DataMember(IsRequired = true)] public List<ExpandedBibleSupportProjectile> Projectiles { get; set; } = new List<ExpandedBibleSupportProjectile>();
        [DataMember(IsRequired = true)] public List<ExpandedBibleInk> Inks { get; set; } = new List<ExpandedBibleInk>();
        [DataMember(EmitDefaultValue = false)] public List<ExpandedBibleBookmark>? Bookmarks { get; set; }
        [DataMember(EmitDefaultValue = false)] public long LastClosedCycle { get; set; }
        [DataMember(EmitDefaultValue = false)] public double? NextContactPageAt { get; set; }
        [DataMember(EmitDefaultValue = false)] public double? NextClosingPageAt { get; set; }
        [DataMember(EmitDefaultValue = false)] public double? NextInactiveGlowAt { get; set; }
        [DataMember(EmitDefaultValue = false)] public uint? PageRandomState { get; set; }

        public int ActiveGroups => Projectiles.Count + Inks.Count(ink => Projectiles.All(shot => shot.Id != ink.Id));
        public bool CanReserveGroup => ActiveGroups < GroupLimit;

        public ExpandedBibleSupportsState Copy()
        {
            var copy = (ExpandedBibleSupportsState)MemberwiseClone();
            copy.Projectiles = Projectiles.Select(shot => shot.Copy()).ToList();
            copy.Inks = Inks.Select(ink => ink.Copy()).ToList();
            copy.Bookmarks = Bookmarks?.Select(mark => mark.Copy()).ToList();
            return copy;
        }

        public bool OnAcceptedBookContact(ExpandedBibleSupportRanks ranks, double now, Vector2 player,
            Vector2 book, int targetId, Vector2 target, bool targetAlive, double preCriticalDamage,
            Func<uint, uint> nextRandom, Func<bool> reserveProjectile, Action suppressed)
        {
            if (ranks.Bookmarks && targetAlive)
            {
                Bookmarks ??= new List<ExpandedBibleBookmark>();
                var mark = Bookmarks.Find(entry => entry.TargetId == targetId);
                if (mark == null) Bookmarks.Add(new ExpandedBibleBookmark { TargetId = targetId, ExpiresAt = now + BookmarkLifetime });
                else mark.ExpiresAt = now + BookmarkLifetime;
            }
            if (!ranks.ContactPages || !Ready(NextContactPageAt, now)) return false;
            PageRandomState = nextRandom(PageRandomState ?? 0);
            if (PageRandomState.Value / 4294967296d >= .20 ||
                !Spawn(ranks, now, false, book, SafeDirection(target - book, book - player),
                    preCriticalDamage, reserveProjectile, suppressed)) return false;
            NextContactPageAt = now + HitPageCooldown;
            return true;
        }

        public void OnNaturalClose(ExpandedBibleSupportRanks ranks, long cycle, double closeAt, double now,
            Vector2 player, IReadOnlyList<Vector2> bookPositions, double bookDamage, Func<bool> reserveProjectile,
            Action suppressed)
        {
            if (cycle <= LastClosedCycle) return;
            LastClosedCycle = cycle;
            if (ranks.InactiveGlow) NextInactiveGlowAt = closeAt + 1;
            if (ranks.ClosingPages && Ready(NextClosingPageAt, now))
            {
                var created = false;
                foreach (var book in bookPositions.Take(2))
                    created |= Spawn(ranks, now, false, book, SafeDirection(book - player, Vector2.UnitX),
                        bookDamage, reserveProjectile, suppressed);
                if (created) NextClosingPageAt = now + ClosingPageCooldown;
            }
            if (!ranks.ReturningBooks) return;
            foreach (var book in bookPositions.Take(2))
                if (Vector2.DistanceSquared(player, book) > 1e-12f)
                    Spawn(ranks, now, true, book, SafeDirection(player - book, Vector2.UnitX),
                        bookDamage, reserveProjectile, suppressed);
        }

        public void OnBooksOpening() => NextInactiveGlowAt = null;

        public void ObserveInactive(double now, Vector2 player, double bookDamage,
            Func<IReadOnlyDictionary<int, Vector2>> targets, Func<ExpandedBibleSupportHit, bool> apply,
            Func<bool> reserveRegion, Action suppressed)
        {
            if (!NextInactiveGlowAt.HasValue || !Ready(NextInactiveGlowAt, now)) return;
            NextInactiveGlowAt += 1;
            var nearby = targets().Where(target =>
                    Vector2.DistanceSquared(player, target.Value) <= (float)(GlowRadius * GlowRadius))
                .OrderBy(target => target.Key).ToArray();
            if (nearby.Length == 0) return;
            if (!CanReserveGroup || !reserveRegion()) { suppressed(); return; }
            foreach (var target in nearby)
                if (targets().ContainsKey(target.Key))
                    apply(new ExpandedBibleSupportHit(target.Key, target.Value, bookDamage * .15, ExpandedBibleSupportKind.InactiveGlow));
        }

        public void Step(double now, Vector2 player, IReadOnlyDictionary<int, Vector2> previous,
            Func<IReadOnlyDictionary<int, Vector2>> targets, Func<int, double> targetRadius,
            Func<Vector2, Vector2, double, double?> intersect, Func<ExpandedBibleSupportHit, bool> apply,
            Func<bool> reserveRegion, Action suppressed)
        {
            PruneBookmarks(now, id => targets().ContainsKey(id));
            Inks.RemoveAll(ink => now + Epsilon >= ink.BornAt + ExpandedBibleInk.Lifetime);
            foreach (var shot in Projectiles.OrderBy(shot => shot.Id).ToArray())
            {
                if (now <= shot.LastAt + Epsilon) continue;
                var from = shot.Position;
                var toPlayer = player - from;
                var toPlayerDistance = toPlayer.Length();
                var remaining = shot.Range - shot.Travelled;
                if (shot.Returning) remaining = Math.Min(remaining, toPlayerDistance);
                if (remaining <= Epsilon) { Projectiles.Remove(shot); continue; }
                var stepDistance = (now - shot.LastAt) * shot.Speed;
                var distance = Math.Min(remaining, stepDistance);
                var direction = shot.Returning ? Vector2.Normalize(toPlayer) : shot.Direction;
                var to = from + direction * (float)distance;
                var collisions = new List<(int Id, Vector2 Position, double At)>();
                foreach (var target in targets())
                {
                    if (shot.HitIds?.Contains(target.Key) == true) continue;
                    var old = previous.TryGetValue(target.Key, out var other) ? other : target.Value;
                    var current = Vector2.Lerp(old, target.Value, (float)(distance / stepDistance));
                    var at = intersect(from - old, to - current, shot.Radius + targetRadius(target.Key));
                    if (at.HasValue) collisions.Add((target.Key, Vector2.Lerp(old, current, (float)at.Value), at.Value));
                }
                var spent = false;
                foreach (var collision in collisions.OrderBy(value => value.At).ThenBy(value => value.Id))
                {
                    if (!targets().ContainsKey(collision.Id)) continue;
                    var marker = shot.BookmarkEligible ? Bookmarks?.Find(mark => mark.TargetId == collision.Id) : null;
                    var factor = shot.Returning ? ReturnFactor : PageFactor + (marker == null ? 0 : BookmarkFactor);
                    var kind = shot.Returning ? ExpandedBibleSupportKind.ReturningBook : ExpandedBibleSupportKind.Page;
                    if (!apply(new ExpandedBibleSupportHit(collision.Id, collision.Position, shot.ParentDamage * factor, kind))) continue;
                    if (marker != null) Bookmarks!.Remove(marker);
                    shot.HitIds ??= new List<int>();
                    shot.HitIds.Add(collision.Id);
                    if (shot.HitIds.Count < shot.HitBudget) continue;
                    to = Vector2.Lerp(from, to, (float)collision.At);
                    spent = true;
                    break;
                }
                if (Vector2.DistanceSquared(from, to) > 1e-12f)
                    TraceInk(shot, now, from, to, reserveRegion, suppressed);
                if (spent || distance >= remaining - Epsilon)
                    Projectiles.Remove(shot);
                else
                {
                    shot.LastAt = now;
                    shot.Direction = direction;
                    shot.Position = to;
                    shot.Travelled += distance;
                }
            }
            ApplyInk(now, targets, apply);
            PruneBookmarks(now, id => targets().ContainsKey(id));
        }

        public void PruneBookmarks(double now, Func<int, bool> targetStillLive)
        {
            if (Bookmarks == null) return;
            Bookmarks.RemoveAll(mark => mark.ExpiresAt <= now + Epsilon || !targetStillLive(mark.TargetId));
            if (Bookmarks.Count == 0) Bookmarks = null;
        }

        public void Validate(double now, long cycle)
        {
            var maximumDamage = WeaponRules.Get("bible", 8).Damage * 2.25;
            if (NextId < 1 || LastClosedCycle < 0 || LastClosedCycle > cycle ||
                !ValidClock(NextContactPageAt, now, HitPageCooldown) ||
                !ValidClock(NextClosingPageAt, now, ClosingPageCooldown) ||
                !ValidClock(NextInactiveGlowAt, now, 1) ||
                PageRandomState == 0 || NextContactPageAt.HasValue && !PageRandomState.HasValue ||
                NextInactiveGlowAt.HasValue && LastClosedCycle == 0 ||
                Projectiles == null || Inks == null || Projectiles.Count > GroupLimit || Inks.Count > GroupLimit ||
                Projectiles.Any(shot => shot == null) || Inks.Any(ink => ink == null) || ActiveGroups > GroupLimit ||
                Projectiles.Select(shot => shot.Id).Distinct().Count() != Projectiles.Count ||
                Inks.Select(ink => ink.Id).Distinct().Count() != Inks.Count)
                throw new ArgumentException("Invalid expanded Bible supports");

            foreach (var shot in Projectiles)
            {
                if (shot.Id < 1 || shot.Id >= NextId || !BattleSnapshot.Position(shot.X, shot.Y) ||
                    !BattleSnapshot.Finite(shot.LastAt) || shot.LastAt < 0 || shot.LastAt > now + Epsilon ||
                    !BattleSnapshot.Finite(shot.Travelled) || shot.Travelled < 0 || shot.Travelled >= shot.Range ||
                    !BattleSnapshot.Finite(shot.ParentDamage) || shot.ParentDamage <= 0 || shot.ParentDamage > maximumDamage ||
                    !BattleSnapshot.Finite(shot.DirectionX) || !BattleSnapshot.Finite(shot.DirectionY) ||
                    Math.Abs(shot.Direction.LengthSquared() - 1) > 1e-5 ||
                    shot.HitBudget < 1 || shot.HitBudget > 2 ||
                    shot.HitIds != null && (shot.HitIds.Count == 0 || shot.HitIds.Count >= shot.HitBudget ||
                        shot.HitIds.Any(id => id <= 0) || shot.HitIds.Distinct().Count() != shot.HitIds.Count) ||
                    shot.InkAttempted && (!shot.InkEligible || shot.Travelled <= 0) ||
                    shot.Returning && (shot.HitBudget != 1 || shot.InkEligible || shot.BookmarkEligible))
                    throw new ArgumentException("Invalid expanded Bible support projectile");
            }
            foreach (var ink in Inks)
            {
                var path = Vector2.DistanceSquared(ink.Start, ink.End);
                var parent = Projectiles.Find(shot => shot.Id == ink.Id);
                if (ink.Id < 1 || ink.Id >= NextId || !BattleSnapshot.Position(ink.StartX, ink.StartY) ||
                    !BattleSnapshot.Position(ink.EndX, ink.EndY) || path <= 1e-12f || path > 9 + 1e-4 ||
                    !BattleSnapshot.Finite(ink.BornAt) || ink.BornAt < 0 || ink.BornAt > now + Epsilon ||
                    now + Epsilon >= ink.BornAt + ExpandedBibleInk.Lifetime ||
                    !BattleSnapshot.Finite(ink.NextHitAt) || ink.NextHitAt < now - Epsilon ||
                    Math.Abs(ink.NextHitAt - ink.BornAt - .5) > Epsilon &&
                        Math.Abs(ink.NextHitAt - ink.BornAt - ExpandedBibleInk.Lifetime) > Epsilon ||
                    !BattleSnapshot.Finite(ink.Damage) || ink.Damage <= 0 || ink.Damage > maximumDamage * InkFactor ||
                    parent != null && (!parent.InkAttempted || parent.Position != ink.End || parent.ParentDamage * InkFactor != ink.Damage))
                    throw new ArgumentException("Invalid expanded Bible ink");
            }

            if (Bookmarks != null && (Bookmarks.Count == 0 || Bookmarks.Count > 120 ||
                Bookmarks.Any(mark => mark == null || mark.TargetId <= 0 || !BattleSnapshot.Finite(mark.ExpiresAt) ||
                    mark.ExpiresAt <= now + Epsilon || mark.ExpiresAt > now + BookmarkLifetime + Epsilon) ||
                Bookmarks.Select(mark => mark.TargetId).Distinct().Count() != Bookmarks.Count))
                throw new ArgumentException("Invalid expanded Bible bookmarks");
        }

        private static bool ValidClock(double? at, double now, double offset) =>
            !at.HasValue || BattleSnapshot.Finite(at.Value) && at.Value >= offset - Epsilon &&
                at.Value <= now + offset + Epsilon;

        private bool Spawn(ExpandedBibleSupportRanks ranks, double now, bool returning, Vector2 at, Vector2 direction,
            double parentDamage, Func<bool> reserveProjectile, Action suppressed)
        {
            if (!CanReserveGroup || !reserveProjectile()) { suppressed(); return false; }
            Projectiles.Add(new ExpandedBibleSupportProjectile
            {
                Id = NextId++, Returning = returning, Position = at, Direction = direction, LastAt = now,
                ParentDamage = parentDamage, HitBudget = !returning && ranks.PiercePages ? 2 : 1,
                InkEligible = !returning && ranks.Ink, BookmarkEligible = !returning && ranks.Bookmarks,
            });
            return true;
        }

        private void TraceInk(ExpandedBibleSupportProjectile shot, double now, Vector2 from, Vector2 to,
            Func<bool> reserve, Action suppressed)
        {
            if (!shot.InkEligible) return;
            if (shot.InkAttempted)
            {
                var active = Inks.Find(ink => ink.Id == shot.Id);
                if (active != null) active.End = to;
                return;
            }
            shot.InkAttempted = true;
            if (!reserve()) { suppressed(); return; }
            Inks.Add(new ExpandedBibleInk
            {
                Id = shot.Id, BornAt = now, NextHitAt = now, StartX = from.X, StartY = from.Y, End = to,
                Damage = shot.ParentDamage * InkFactor,
            });
        }

        private void ApplyInk(double now, Func<IReadOnlyDictionary<int, Vector2>> targets,
            Func<ExpandedBibleSupportHit, bool> apply)
        {
            var due = Inks.Where(ink => now + Epsilon >= ink.NextHitAt).ToArray();
            if (due.Length == 0) return;
            foreach (var ink in due) ink.NextHitAt += .5;
            var hits = new List<(ExpandedBibleInk Ink, int Id, Vector2 Position)>();
            foreach (var target in targets())
            {
                var winner = due.Where(ink => WithinInk(ink, target.Value))
                    .OrderByDescending(ink => ink.Damage).ThenBy(ink => ink.Id).FirstOrDefault();
                if (winner != null) hits.Add((winner, target.Key, target.Value));
            }
            foreach (var hit in hits.OrderBy(hit => hit.Ink.Id).ThenBy(hit => hit.Id))
                if (targets().ContainsKey(hit.Id))
                    apply(new ExpandedBibleSupportHit(hit.Id, hit.Position, hit.Ink.Damage, ExpandedBibleSupportKind.Ink));
        }

        private static bool WithinInk(ExpandedBibleInk ink, Vector2 point)
        {
            var segment = ink.End - ink.Start;
            var rel = point - ink.Start;
            var t = Math.Max(0, Math.Min(1, Vector2.Dot(rel, segment) / segment.LengthSquared()));
            var radius = ExpandedBibleInk.Width / 2;
            return (rel - segment * t).LengthSquared() <= (float)(radius * radius);
        }

        private static Vector2 SafeDirection(Vector2 value, Vector2 fallback)
        {
            if (value.LengthSquared() > 1e-12f) return Vector2.Normalize(value);
            return fallback.LengthSquared() > 1e-12f ? Vector2.Normalize(fallback) : Vector2.UnitX;
        }

        private static bool Ready(double? scheduled, double now) => !scheduled.HasValue || now + Epsilon >= scheduled.Value;
    }
}
