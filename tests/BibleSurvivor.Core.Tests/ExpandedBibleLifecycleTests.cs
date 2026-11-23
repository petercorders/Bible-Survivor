using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Json;

namespace BibleSurvivor.Core.Tests;

internal static class ExpandedBibleLifecycleTests
{
    public static void Register(Action<string, Action> test)
    {
        test("Bible deflection sorts actual contacts, restores exactly one roll per continued book overlap and gates successful removal", () =>
        {
            var state = new ExpandedBibleLifecycleState();
            var ranks = Ranks("b.03");
            var prior = new List<uint>();
            var rolls = new Queue<uint>(new[] { uint.MaxValue, 1u, 2u });
            var erased = new List<long>();
            uint Roll(uint previous) { prior.Add(previous); return rolls.Dequeue(); }
            bool Erase(long id) { erased.Add(id); return true; }
            var hits = new[] { new ExpandedBibleDeflectionContact(2, 0, .5, true), new ExpandedBibleDeflectionContact(3, 0, 0, true) };
            Check.True(state.TryDeflect(ranks, .2, hits, true, Roll, Erase));
            Check.True(erased.SequenceEqual(new long[] { 2 }));
            Check.True(prior.SequenceEqual(new[] { 0u, uint.MaxValue }));
            Check.Near(2.2, state.NextDeflectionAt!.Value);
            Check.Equal(3L, state.DeflectionAttempts!.Single().ProjectileId);

            var resumed = Restore(state);
            resumed.Validate(.2, 2);
            var clone = state.Copy();
            clone.DeflectionAttempts!.Single().BooksMask = 4;
            Check.Throws<ArgumentException>(() => clone.Validate(.2, 2));
            Check.Equal(1, state.DeflectionAttempts!.Single().BooksMask);
            var sameShot = new[] { new ExpandedBibleDeflectionContact(3, 0, 0, true), new ExpandedBibleDeflectionContact(3, 1, 0, true) };
            Check.True(!resumed.TryDeflect(ranks, 2.19, sameShot, true, Roll, Erase));
            Check.True(!resumed.TryDeflect(ranks, 2.2, sameShot, false, Roll, Erase));
            Check.Equal(2, prior.Count);
            Check.True(resumed.TryDeflect(ranks, 2.2, sameShot, true, Roll, Erase));
            Check.True(erased.SequenceEqual(new long[] { 2, 3 }));
            Check.True(prior.SequenceEqual(new[] { 0u, uint.MaxValue, 1u }));
            Check.True(resumed.DeflectionAttempts == null);
        });

        test("Bible failed book deflection can retry after a real separation but sustains no retry during overlap", () =>
        {
            var state = new ExpandedBibleLifecycleState();
            var ranks = Ranks("b.03");
            var rolled = 0;
            bool Step(double now, bool continues) =>
                state.TryDeflect(ranks, now, new[] { new ExpandedBibleDeflectionContact(9, 1, 0, continues) },
                    true, previous => ++rolled == 1 ? uint.MaxValue : 1, _ => true);

            Check.True(!Step(0, true));
            Check.True(!Step(.1, true));
            Check.Equal(1, rolled);
            Check.True(!Step(.2, false));
            Check.Equal(1, rolled);
            Check.True(state.DeflectionAttempts == null);
            Check.True(Step(.3, false));
            Check.Equal(2, rolled);
            Check.True(state.DeflectionAttempts == null);
        });

        test("Bible low-health cover requires a crossing and the received-hit barrier reserves and cools independently", () =>
        {
            var state = new ExpandedBibleLifecycleState();
            var ranks = Ranks("b.04", "b.05");
            var reserves = 0;
            bool Reserve() => ++reserves >= 2;
            Check.True(!state.ObserveDamage(ranks, 0, .32, .30, 0, Reserve));
            Check.Equal(0, reserves);
            Check.True(!state.ObserveDamage(ranks, 1, .31, .30, 2, Reserve));
            Check.Near(.15, state.CollisionRadiusBonus(1));
            Check.True(state.ObserveDamage(ranks, 2, .30, .20, 4, Reserve));
            Check.Near(4, state.CriticalCoverUntil!.Value);
            Check.Near(7, state.NextDamageBarrierAt!.Value);

            var resumed = Restore(state);
            resumed.Validate(2, 1);
            Check.Near(.15, resumed.CollisionRadiusBonus(3.5));
            Check.Near(0, resumed.CollisionRadiusBonus(4));
            Check.True(!resumed.ObserveDamage(ranks, 6.9, .8, .22, 1, Reserve));
            Check.Equal(2, reserves);
            Check.True(resumed.ObserveDamage(ranks, 7, .29, .28, 1, Reserve));
            Check.Near(0, resumed.CollisionRadiusBonus(7));
            Check.True(!resumed.ObserveDamage(ranks, 9, .25, .23, 1, Reserve));
            Check.Near(0, resumed.CollisionRadiusBonus(9));
            Check.True(!resumed.ObserveDamage(ranks, 9.1, .31, .30, 1, Reserve));
            Check.Near(.15, resumed.CollisionRadiusBonus(9.1));
            resumed.Validate(9.1, 1);
            var invalid = resumed.Copy();
            invalid.CriticalCoverUntil = 11;
            Check.Throws<ArgumentException>(() => invalid.Validate(9, 1));
        });

        test("Bible extends after one specific book reaches six normal kills, once per cohort and under the native cap", () =>
        {
            var ranks = Ranks("d.01", "d.02", "d.05");
            Check.Near(.10, ranks.DurationBonus);
            Check.Near(0, default(ExpandedBibleLifecycleRules).DurationBonus);
            var state = new ExpandedBibleLifecycleState();
            double Record(ExpandedBibleLifecycleState current, int index, double duration, bool elite = false) =>
                current.RecordAcceptedBookKill(ranks, 1, index, 2, elite, 2, duration);

            for (var i = 0; i < 3; i++)
            {
                Check.Near(2, Record(state, 0, 2));
                Check.Near(2, Record(state, 1, 2));
            }
            Check.True(state.BookKillCounts!.SequenceEqual(new[] { 3, 3 }));
            Check.Near(2, Record(state, 0, 2));
            Check.Near(2, Record(state, 0, 2, elite: true));
            Check.Near(2, Record(state, 0, 2));
            var resumed = Restore(state);
            resumed.Validate(1, 2);

            var bad = resumed.Copy();
            bad.BookKillCounts![1] = 7;
            Check.Throws<ArgumentException>(() => bad.Validate(1, 2));
            Check.True(resumed.BookKillCounts!.SequenceEqual(new[] { 5, 3 }));
            Check.Near(2.25, Record(resumed, 0, 2));
            for (var i = 0; i < 4; i++) Check.Near(2.25, Record(resumed, 1, 2.25));
            Check.True(resumed.BookKillCounts!.SequenceEqual(new[] { 6, 3 }));

            resumed.BeginBooks();
            Check.True(resumed.BookKillCounts == null);
            Check.True(resumed.NextWaitReductionArmed);
            for (var i = 0; i < 5; i++) Check.Near(3.4, Record(resumed, 1, 3.4));
            Check.Near(3.5, Record(resumed, 1, 3.4));
            resumed.Validate(1, 2);
        });

        test("Bible saves an armed future wait separately from its elite cooldown and consumes it exactly once", () =>
        {
            var state = new ExpandedBibleLifecycleState();
            var ranks = Ranks("d.05");
            void Kill(ExpandedBibleLifecycleState current, double at) =>
                current.RecordAcceptedBookKill(ranks, at, 1, 2, true, 2, 2);
            Kill(state, 1);
            Kill(state, 3);
            Check.Near(7, state.NextEliteWaitAt!.Value);
            var saved = Restore(state);
            saved.Validate(3, 2);
            Check.Near(.10, saved.ConsumeWaitReduction());
            Check.Near(0, saved.ConsumeWaitReduction());
            Kill(saved, 6.9);
            Check.True(!saved.NextWaitReductionArmed);
            Kill(saved, 7);
            Check.Near(13, saved.NextEliteWaitAt!.Value);
            Check.Near(.10, saved.ConsumeWaitReduction());
            Check.True(saved.BookKillCounts == null);
        });
    }

    private static ExpandedBibleLifecycleRules Ranks(params string[] ids) =>
        ExpandedBibleLifecycleRules.FromRanks(ids.ToDictionary(id => "weapon.bible." + id, _ => 1));

    private static ExpandedBibleLifecycleState Restore(ExpandedBibleLifecycleState state)
    {
        using var bytes = new MemoryStream();
        var serializer = new DataContractJsonSerializer(typeof(ExpandedBibleLifecycleState));
        serializer.WriteObject(bytes, state);
        bytes.Position = 0;
        return (ExpandedBibleLifecycleState)serializer.ReadObject(bytes)!;
    }
}
