using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Runtime.Serialization.Json;

namespace BibleSurvivor.Core.Tests;

internal static class ExpandedBibleSupportsTests
{
    public static void Register(Action<string, Action> test)
    {
        test("Bible pages freeze parent damage, marks, pierce, and the exact swept ink endpoint through a save", () =>
        {
            var state = new ExpandedBibleSupportsState();
            var ranks = Ranks("c.01", "c.02", "c.03", "c.04");
            var targets = new Dictionary<int, Vector2> { [1] = new(1.4f, 0), [2] = new(2, 0) };
            var hits = new List<ExpandedBibleSupportHit>();
            void Step(ExpandedBibleSupportsState current, double now) =>
                Advance(current, now, targets, hit => { hits.Add(hit); return true; });

            Check.True(state.OnAcceptedBookContact(ranks, 0, Vector2.Zero, new Vector2(.7f, 0), 1, targets[1],
                true, 20, _ => 1, () => true, FailSuppression));
            Check.True(!state.OnAcceptedBookContact(ranks, .05, Vector2.Zero, new Vector2(.7f, 0), 1, targets[1],
                true, 20, _ => throw new Exception("Early proc roll"), () => true, FailSuppression));
            Step(state, .1);
            var page = hits.Single(hit => hit.Kind == ExpandedBibleSupportKind.Page);
            Check.Equal(1, page.TargetId);
            Check.Near(9.4, page.Damage);
            Check.Near(3, hits.Single(hit => hit.Kind == ExpandedBibleSupportKind.Ink).Damage);
            Check.True(state.Bookmarks == null);
            Check.Equal(1, state.ActiveGroups);
            Check.True(state.Projectiles.Single().HitIds!.SequenceEqual(new[] { 1 }));
            Check.Near(.35, ExpandedBibleInk.Width);
            Check.Near(1.4, state.Inks.Single().EndX, 1e-6);

            var saved = Restore(state);
            saved.Validate(.1, 1);
            var bad = state.Copy();
            bad.Inks.Single().EndX = 2.1f;
            Check.Throws<ArgumentException>(() => bad.Validate(.1, 1));
            Step(saved, .2);
            Check.True(saved.Projectiles.Count == 0);
            Check.Near(1.9, saved.Inks.Single().EndX, 1e-6);
            Check.Near(7, hits.Last(hit => hit.Kind == ExpandedBibleSupportKind.Page).Damage);
            Check.Equal(2, hits.Last(hit => hit.Kind == ExpandedBibleSupportKind.Page).TargetId);
            saved.Validate(.2, 1);
            Check.Equal(1, saved.ActiveGroups);
        });

        test("Bible contact pages spend a scoped 20 percent roll and only successful allocation starts the one-second gate", () =>
        {
            var state = new ExpandedBibleSupportsState();
            var ranks = Ranks("c.01");
            var suppressed = 0;
            bool Contact(double now, uint draw, bool canReserve) =>
                state.OnAcceptedBookContact(ranks, now, Vector2.Zero, Vector2.UnitX, 1, Vector2.UnitX,
                    false, 20, _ => draw, () => canReserve, () => suppressed++);
            Check.True(!Contact(0, uint.MaxValue, true));
            Check.True(!Contact(.1, 1, false));
            Check.Equal(1, suppressed);
            Check.True(state.NextContactPageAt == null);
            Check.True(Contact(.2, 1, true));
            Check.Near(1.2, state.NextContactPageAt!.Value);
            Check.True(!Contact(1.19, uint.MaxValue, true));
            Check.Equal(1u, state.PageRandomState!.Value);
        });

        test("Book pages sweep against moving targets and a refused contact preserves its bookmark", () =>
        {
            var state = new ExpandedBibleSupportsState();
            var ranks = Ranks("c.01", "c.04");
            var targets = new Dictionary<int, Vector2> { [1] = new(1, 1) };
            var previous = new Dictionary<int, Vector2> { [1] = new(1, -1) };
            state.OnAcceptedBookContact(ranks, 0, Vector2.Zero, new Vector2(.7f, 0), 1, new Vector2(1, 0),
                true, 20, _ => 1, () => true, FailSuppression);
            var attempts = 0;
            Advance(state, .1, targets, hit => { Check.Near(9.4, hit.Damage); attempts++; return false; }, previous);
            Check.Equal(1, attempts);
            Check.True(state.Projectiles.Single().HitIds == null);
            Check.Equal(1, state.Bookmarks!.Single().TargetId);
            Check.Near(1.4, state.Projectiles.Single().X, 1e-6);
            state.PruneBookmarks(2, _ => true);
            Check.True(state.Bookmarks == null);
        });

        test("Bible natural close emits at most two pages and two one-hit returning books and never repeats on restore", () =>
        {
            var state = new ExpandedBibleSupportsState();
            var ranks = Ranks("c.05", "d.03");
            var books = new[] { new Vector2(1.4f, 0), new Vector2(-1.4f, 0), new Vector2(0, 1.4f) };
            var targets = new Dictionary<int, Vector2> { [1] = new(.8f, 0) };
            var hits = new List<ExpandedBibleSupportHit>();
            void Close(ExpandedBibleSupportsState current, long cycle, double time) =>
                current.OnNaturalClose(ranks, cycle, time, time, Vector2.Zero, books, 20, () => true, FailSuppression);

            state.OnBooksOpening();
            Advance(state, 3, targets, _ => false);
            Check.True(state.Projectiles.Count == 0);
            Close(state, 1, 3);
            Check.Equal(4, state.ActiveGroups);
            Check.Equal(2, state.Projectiles.Count(shot => !shot.Returning && shot.Speed == 7));
            Check.Equal(2, state.Projectiles.Count(shot => shot.Returning && shot.Speed == 6 && shot.Radius == .25));
            Check.True(state.Projectiles.All(shot => shot.Range == 3));
            var saved = Restore(state);
            saved.Validate(3, 1);
            Close(saved, 1, 3);
            Check.Equal(4, saved.ActiveGroups);

            Advance(saved, 3.1, targets, hit => { hits.Add(hit); return true; });
            Check.Equal(ExpandedBibleSupportKind.ReturningBook, hits.Single().Kind);
            Check.Near(6, hits.Single().Damage);
            Check.Equal(3, saved.ActiveGroups);
            Advance(saved, 3.5, targets, hit => { hits.Add(hit); return true; });
            Check.Equal(0, saved.ActiveGroups);
            Close(saved, 2, 6);
            Check.Equal(2, saved.ActiveGroups);
            Check.True(saved.Projectiles.All(shot => shot.Returning));
        });

        test("Bible moving ink shares the page group, counts its own region slot and arbitrates overlapping ticks by damage", () =>
        {
            var state = new ExpandedBibleSupportsState();
            var ranks = Ranks("c.01", "c.02", "c.05");
            var center = new Vector2(.7f, 0);
            state.OnAcceptedBookContact(ranks, 0, Vector2.Zero, center, 1, new Vector2(1, 0), true, 40,
                _ => 1, () => true, FailSuppression);
            state.OnNaturalClose(ranks, 1, 0, 0, Vector2.Zero, new[] { center }, 20, () => true, FailSuppression);
            var regions = 0;
            var targets = new Dictionary<int, Vector2> { [1] = new(1, .175f), [2] = new(1, .176f) };
            var inkHits = new List<ExpandedBibleSupportHit>();
            void Step(double at) => Advance(state, at, targets, hit =>
            {
                if (hit.Kind == ExpandedBibleSupportKind.Ink) inkHits.Add(hit);
                return false;
            }, reserveRegion: () => { regions++; return true; });

            Step(.1);
            Check.Equal(2, regions);
            Check.Equal(2, state.ActiveGroups);
            Check.Near(6, inkHits.Single().Damage);
            Check.Equal(1, inkHits.Single().TargetId);
            Step(.6);
            Check.Equal(2, inkHits.Count);
            Check.Equal(2, state.ActiveGroups);
            Step(1.1);
            Check.Equal(2, inkHits.Count);
            Check.Equal(0, state.ActiveGroups);

            var denied = new ExpandedBibleSupportsState();
            denied.OnNaturalClose(ranks, 1, 0, 0, Vector2.Zero, new[] { center }, 20, () => true, FailSuppression);
            var blocked = 0;
            Advance(denied, .1, targets, _ => false, reserveRegion: () => false, suppressed: () => blocked++);
            Advance(denied, .2, targets, _ => false, reserveRegion: () => true, suppressed: () => blocked++);
            Check.Equal(1, blocked);
            Check.Equal(0, denied.Inks.Count);
            Check.True(denied.Projectiles.Single().InkAttempted);
            denied.Validate(.2, 1);
        });

        test("Inactive Bible glow uses the last natural close schedule and stops when books open", () =>
        {
            var state = new ExpandedBibleSupportsState();
            var targets = new Dictionary<int, Vector2> { [2] = new(.9f, 0), [1] = new(1.2f, 0), [3] = new(1.2001f, 0) };
            var hits = new List<ExpandedBibleSupportHit>();
            state.OnNaturalClose(Ranks("d.04"), 1, 3.01, 3.02, Vector2.Zero, new[] { Vector2.UnitX }, 20,
                () => throw new Exception("Unexpected projectile"), FailSuppression);
            void Step(ExpandedBibleSupportsState current, double at) => current.ObserveInactive(at, Vector2.Zero, 20, () => targets,
                hit => { hits.Add(hit); return true; }, () => true, FailSuppression);
            Step(state, 4);
            Check.Equal(0, hits.Count);
            var restored = Restore(state);
            Step(restored, 4.02);
            Check.True(hits.Select(hit => hit.TargetId).SequenceEqual(new[] { 1, 2 }));
            Check.True(hits.All(hit => hit.Kind == ExpandedBibleSupportKind.InactiveGlow && hit.Damage == 3));
            Check.Near(5.01, restored.NextInactiveGlowAt!.Value);
            restored.OnBooksOpening();
            Step(restored, 5.03);
            Check.Equal(2, hits.Count);
        });
    }

    private static ExpandedBibleSupportRanks Ranks(params string[] ids) =>
        ExpandedBibleSupportRanks.FromRanks(ids.ToDictionary(id => "weapon.bible." + id, _ => 1));

    private static void FailSuppression() => throw new Exception("Unexpected support suppression");

    private static void Advance(ExpandedBibleSupportsState current, double time, IReadOnlyDictionary<int, Vector2> targets,
        Func<ExpandedBibleSupportHit, bool> apply, IReadOnlyDictionary<int, Vector2>? previous = null,
        Func<bool>? reserveRegion = null, Action? suppressed = null) => current.Step(time, Vector2.Zero,
            previous ?? targets, () => targets, _ => 0, Intersect, apply, reserveRegion ?? (() => true), suppressed ?? FailSuppression);

    private static double? Intersect(Vector2 from, Vector2 to, double radius)
    {
        var delta = to - from;
        var c = Vector2.Dot(from, from) - radius * radius;
        if (c <= 0) return 0;
        var a = Vector2.Dot(delta, delta);
        var b = 2 * Vector2.Dot(from, delta);
        var disc = b * b - 4 * a * c;
        if (a < 1e-16 || disc < 0) return null;
        var t = (-b - Math.Sqrt(disc)) / (2 * a);
        return t >= 0 && t <= 1 ? t : null;
    }

    private static ExpandedBibleSupportsState Restore(ExpandedBibleSupportsState state)
    {
        using var bytes = new MemoryStream();
        var serializer = new DataContractJsonSerializer(typeof(ExpandedBibleSupportsState));
        serializer.WriteObject(bytes, state);
        bytes.Position = 0;
        return (ExpandedBibleSupportsState)serializer.ReadObject(bytes)!;
    }
}
