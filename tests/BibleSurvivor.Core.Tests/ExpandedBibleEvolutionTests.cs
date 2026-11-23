using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Runtime.Serialization.Json;

namespace BibleSurvivor.Core.Tests;

internal static class ExpandedBibleEvolutionTests
{
    public static void Register(Action<string, Action> test)
    {
        test("Expanded Bible radius and outer knockback obey their native independent rank bounds", () =>
        {
            Check.Near(0, default(ExpandedBibleEvolutionRanks).PulseRadiusBonus);
            Check.Near(.15, Ranks("01").PulseRadiusBonus);
            var ranks = Ranks("03");
            Check.Near(0, ranks.PulseRadiusBonus);
            Check.Near(0, ranks.PulseKnockback(2.3999f * 2.3999f, 3));
            Check.Near(.30, ranks.PulseKnockback(2.4f * 2.4f, 3));
            Check.Near(0, default(ExpandedBibleEvolutionRanks).PulseKnockback(2.4f * 2.4f, 3));
        });

        test("Expanded Bible schedules each fourth primary once, freezes noncritical echo damage, and restores missed fixed tick", () =>
        {
            var ranks = Ranks("02");
            var state = new ExpandedBibleEvolutionState();
            var reserves = 0;
            var suppressions = 0;
            bool Reserve() => ++reserves != 2;
            bool Pulse(ExpandedBibleEvolutionState current, double time, double damage = 40) =>
                current.BeginPrimaryPulse(ranks, time, 3.2, damage, Reserve, () => suppressions++);
            Check.True(!Pulse(state, 0));
            Check.True(!Pulse(state, 2));
            Check.True(!Pulse(state, 4));
            Check.True(Pulse(state, 6.013));
            Check.Equal(1, state.ReservedRegions);
            Check.True(state.ConsumeEcho(6.5) == null);
            var packed = Restore(state);
            packed.Validate(6.5);
            var corrupt = packed.Copy();
            corrupt.PendingEcho!.Radius = 10;
            Check.Throws<ArgumentException>(() => corrupt.Validate(6.5));

            var echo = packed.ConsumeEcho(6.517)!;
            Check.Near(6.513, echo.DueAt);
            Check.Near(3.2, echo.Radius);
            Check.Near(14, echo.Damage);
            Check.Equal(4L, packed.PulseOrdinal);
            Check.Equal(0, packed.ReservedRegions);
            Check.True(packed.ConsumeEcho(6.52) == null);
            Check.True(!Pulse(packed, 8));
            Check.True(!Pulse(packed, 10));
            Check.True(!Pulse(packed, 12));
            Check.True(!Pulse(packed, 14));
            Check.Equal(8L, packed.PulseOrdinal);
            Check.Equal(1, suppressions);
            Check.Equal(2, reserves);
            Check.True(packed.PendingEcho == null);
        });

        test("Expanded Bible steers the actual inner book toward the nearest missed target with stable ties and angular cap", () =>
        {
            var ranks = Ranks("04");
            var baseOrbit = ExpandedBibleRanks.FromRanks(new Dictionary<string, int>
            {
                ["weapon.bible.a.03"] = 1, ["weapon.bible.a.04"] = 1,
            });
            var state = new ExpandedBibleEvolutionState();
            var player = new Vector2(2, 2);
            var targets = new Dictionary<int, Vector2>
            {
                [1] = player + .5f * Vector2.UnitX,
                [3] = player - Vector2.UnitY,
                [2] = player + Vector2.UnitY,
                [4] = player + 1.41f * Vector2.UnitX,
            };
            state.BeginPrimaryPulse(ranks, 0, 3.2, 40, FailReserve, FailSuppression);
            ExpandedBibleBookPose Book(ExpandedBibleEvolutionState current, int index, double now) =>
                baseOrbit.Book(player, index, 2, now, Math.PI / 2.5, 0, 4, 2, 1.4,
                    innerAngleOffset: current.InnerAngleOffset);
            var unsteered = Book(state, 1, 1);
            var outer = Book(state, 0, 1);
            Check.Near(0, unsteered.Angle);
            Check.True(state.TrySteerInnerBook(ranks, player, unsteered.Angle, targets, new[] { 1 }));
            Check.Near(Math.PI / 6, state.InnerAngleOffset);
            Check.True(!state.TrySteerInnerBook(ranks, player, unsteered.Angle, targets, new[] { 1 }));
            var packed = Restore(state);
            packed.Validate(1);
            var turned = Book(packed, 1, 1);
            Check.Near(Math.PI / 6, turned.Angle);
            Check.True(turned.Position.Y > unsteered.Position.Y);
            Check.Equal(outer.Position, Book(packed, 0, 1).Position);
            Check.Near(-Math.PI / 10, Book(packed, 1, 1.1).Angle - turned.Angle);

            packed.BeginPrimaryPulse(ranks, 2, 3.2, 40, FailReserve, FailSuppression);
            Check.True(!packed.TrySteerInnerBook(ranks, player, turned.Angle, targets, new[] { 1, 2, 3 }));
            Check.Equal(2L, packed.LastSteeredPulse);
            Check.Near(Math.PI / 6, packed.InnerAngleOffset);
        });

        test("Expanded Bible steers across the minus-pi seam by the shorter signed turn", () =>
        {
            var ranks = Ranks("04");
            var state = new ExpandedBibleEvolutionState();
            state.BeginPrimaryPulse(ranks, 0, 3.2, 40, FailReserve, FailSuppression);
            double theta = -Math.PI + .2;
            var targets = new Dictionary<int, Vector2> { [1] = new((float)Math.Cos(theta), (float)Math.Sin(theta)) };
            Check.True(state.TrySteerInnerBook(ranks, Vector2.Zero, Math.PI - .1, targets, Array.Empty<int>()));
            Check.Near(.3, state.InnerAngleOffset, 1e-6);
        });

        test("Expanded Bible sanctuary uses accepted original damage, a reserved placed radius, lifetime, and cooldown", () =>
        {
            var ranks = Ranks("05");
            var state = new ExpandedBibleEvolutionState();
            var denied = 0;
            var center = new Vector2(0, 2);
            Check.True(!state.OnAcceptedEliteOriginalKill(ranks, 0, center, 40, () => false, () => denied++));
            Check.Equal(1, denied);
            Check.True(state.NextSanctuaryAt == null);
            Check.True(state.OnAcceptedEliteOriginalKill(ranks, 1, center, 40, () => true, FailSuppression));
            Check.Equal(1, state.ReservedRegions);
            Check.True(!state.OnAcceptedEliteOriginalKill(ranks, 2, center, 60, FailReserve, FailSuppression));

            var targets = new Dictionary<int, Vector2>
            {
                [2] = center, [1] = center + 1.2f * Vector2.UnitX, [3] = center + 1.2001f * Vector2.UnitX,
            };
            var hits = new List<(int Id, double Damage)>();
            void Step(ExpandedBibleEvolutionState current, double now) =>
                current.StepSanctuary(now, () => targets, (id, _, damage) => hits.Add((id, damage)));

            Step(state, 1);
            var saved = Restore(state);
            saved.Validate(1);
            Step(saved, 1.49);
            Step(saved, 1.5);
            Step(saved, 2);
            Step(saved, 2.5);
            Check.True(hits.Select(hit => hit.Id).SequenceEqual(new[] { 1, 2, 1, 2, 1, 2 }));
            Check.True(hits.All(hit => hit.Damage == 10));
            Check.Equal(0, saved.ReservedRegions);
            Check.True(!saved.OnAcceptedEliteOriginalKill(ranks, 5.99, center, 40, FailReserve, FailSuppression));
            Check.True(saved.OnAcceptedEliteOriginalKill(ranks, 6, center, 20, () => true, FailSuppression));
            Check.Near(5, saved.Sanctuary!.Damage);
        });
    }

    private static ExpandedBibleEvolutionRanks Ranks(params string[] ids) =>
        ExpandedBibleEvolutionRanks.FromRanks(ids.ToDictionary(id => "weapon.bible.e." + id, _ => 1));

    private static bool FailReserve() => throw new Exception("Unexpected region reservation");

    private static void FailSuppression() => throw new Exception("Unexpected suppression");

    private static ExpandedBibleEvolutionState Restore(ExpandedBibleEvolutionState state)
    {
        using var bytes = new MemoryStream();
        var serializer = new DataContractJsonSerializer(typeof(ExpandedBibleEvolutionState));
        serializer.WriteObject(bytes, state);
        bytes.Position = 0;
        return (ExpandedBibleEvolutionState)serializer.ReadObject(bytes)!;
    }
}
