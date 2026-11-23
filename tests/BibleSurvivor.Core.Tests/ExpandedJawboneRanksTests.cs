using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Runtime.Serialization.Json;
using System.Numerics;

namespace BibleSurvivor.Core.Tests;

internal static class ExpandedJawboneRanksTests
{
    public static void Register(Action<string, Action> test)
    {
        test("Jawbone c.01 and c.04 arm from accepted event boundaries and are consumed by one following swing", () =>
        {
            var ranks = FromIds("c.01", "c.04");
            var state = new ExpandedJawboneLifecycle();
            Check.Equal((0d, 0d, false), state.BeginSwing(ranks, 0, 0));
            state.AcceptedOriginal(ranks, 0, 1, false, false, false, 40, .9, .15);
            state.EndSwing(ranks, 0, 3);
            Check.Equal((0d, 0d, false), state.BeginSwing(ranks, .15, 0));

            state.AcceptedOriginal(ranks, .15, 1, false, true, false, 40, .9, .15);
            state.EndSwing(ranks, .15, 4);
            state = RestoreLifecycle(state);
            state.Validate(.15, 1, false);
            var late = state.Copy();
            Check.Equal((.08, .08, false), state.BeginSwing(ranks, 2.15, 0));
            Check.Equal((0d, 0d, false), late.BeginSwing(ranks, 2.1501, 0));
            state.AcceptedOriginal(ranks, 2.15, 1, false, true, false, 40, 3, 2.5);
            Check.Equal((0d, 0d, false), state.BeginSwing(ranks, 2.16, 0));
            state.AcceptedOriginal(ranks, 2.16, 2, false, true, true, 40, 3, 2.5);
            Check.Equal((.08, 0d, false), state.BeginSwing(ranks, 2.17, 0));
        });

        test("Jawbone c.02 uses a true two-second rolling normal-kill boundary and shortens new waits for three seconds", () =>
        {
            var ranks = FromIds("c.02");
            var state = new ExpandedJawboneLifecycle();
            foreach (var time in new[] { 0d, .1, 1 })
                state.AcceptedOriginal(ranks, time, 1, false, true, false, 40, 2.9, .15);
            state.AcceptedOriginal(ranks, 1.9, 1, false, true, true, 40, 2.9, .15);
            Check.True(!state.RapidWaitUntil.HasValue);
            var late = state.Copy();
            state.AcceptedOriginal(ranks, 2, 1, false, true, false, 40, 2.9, .15);
            late.AcceptedOriginal(ranks, 2.0001, 1, false, true, false, 40, 2.9, .15);
            Check.True(!late.RapidWaitUntil.HasValue);
            Check.Near(5, state.RapidWaitUntil!.Value);
            Check.Near(8, state.NextRapidWaitAt!.Value);
            state = RestoreLifecycle(state);
            state.Validate(2, 1, false);
            Check.Near(.855, state.BeginCycle(ranks, 5, false, 2, .15, .9, .9).IntervalToNext);
            Check.Near(.9, state.BeginCycle(ranks, 5.0001, false, 2, .15, .9, .9).IntervalToNext);
            Check.True(state.NextRapidWaitAt.HasValue);
            state.Prune(8);
            Check.True(!state.NextRapidWaitAt.HasValue);
        });

        test("Jawbone c.03 moves one pending cycle earlier while preserving the latest original burst", () =>
        {
            var ranks = FromIds("c.03");
            var state = new ExpandedJawboneLifecycle();
            var advanced = state.AcceptedOriginal(ranks, .15, 1, false, true, true, 40, .9, .3166666666667);
            Check.Near(.75, advanced.NextCycle);
            Check.Near(5.15, state.NextAdvanceAt!.Value);
            Check.Near(.75, state.AcceptedOriginal(ranks, .30, 1, false, true, true, 40, .75, .3166666666667).NextCycle);
            var floor = ExpandedJawboneLifecycle.MinimumScheduleSpacing(6, .15);
            state = new ExpandedJawboneLifecycle();
            var clamped = state.AcceptedOriginal(ranks, .6, 1, false, true, true, 40, .9, floor);
            Check.Near(.75 + BattleWorld.FixedStep, clamped.NextCycle);
            state = new ExpandedJawboneLifecycle();
            state.AcceptedOriginal(ranks, .9, 1, false, true, true, 40, .9, .1666666667);
            Check.True(state.AdvanceReady);
            state = RestoreLifecycle(state);
            Check.Near(.75, state.BeginCycle(ranks, .9, false, 2, .15, .9, .9).IntervalToNext);
            Check.True(!state.AdvanceReady);
        });

        test("Jawbone c.05 emits one breakthrough from the twentieth original normal's effective damage", () =>
        {
            var ranks = FromIds("c.05");
            var state = new ExpandedJawboneLifecycle();
            for (var i = 0; i < 19; i++)
                Check.Near(0, state.AcceptedOriginal(ranks, 0, 1, false, true, false, 40, .9, .15).BreakthroughDamage);
            state.AcceptedOriginal(ranks, 0, 1, false, true, true, 40, .9, .15);
            state.AcceptedOriginal(ranks, 0, 1, false, false, false, 40, .9, .15);
            state = RestoreLifecycle(state);
            state.Validate(0, 1, false);
            Check.Equal(19, state.BreakthroughCount);
            Check.Near(27, state.AcceptedOriginal(ranks, 0, 1, false, true, false, 60, .9, .15).BreakthroughDamage);
            Check.Near(2.2, ExpandedJawboneLifecycle.BreakthroughRadius);
            Check.Near(5, state.NextBreakthroughAt!.Value);
            Check.Equal(0, state.BreakthroughCount);
            state.AcceptedOriginal(ranks, 0, 1, false, true, false, 60, .9, .15);
            Check.Equal(0, state.BreakthroughCount);
        });

        test("Jawbone d.01–d.05 use real close range, surviving damage, actual original defense and the six-target circle", () =>
        {
            var ranks = FromIds("b.01", "d.01", "d.02", "d.03", "d.04", "d.05");
            var state = new ExpandedJawboneLifecycle();
            Check.Near(.5, ranks.Knockback(0, 1, 1));
            Check.Near(.25, ranks.Knockback(0, 1, 1.0001));
            Check.Near(1, ranks.Knockback(.8, 1, 1));
            Check.True(!state.TryArmHurt(ranks, 0, 0));
            Check.True(state.TryArmHurt(ranks, 0, 1));
            Check.True(!state.TryArmHurt(ranks, 2, 1));
            Check.Near(.10, state.HurtDamageBonusAt(ranks, 3));
            Check.Near(0, state.HurtDamageBonusAt(ranks, 3.0001));
            Check.True(!state.BeginSwing(ranks, 3, 5).FullCircle);
            var circle = state.BeginSwing(ranks, 3, 6);
            var swing = ranks.ForSwing(1, 1, 2, Vector2.UnitX, fullCircle: circle.FullCircle);
            Check.Near(180, swing.HalfAngle);
            Check.True(swing.HitsOriginal(-Vector2.UnitX, 1));
            Check.True(!state.BeginSwing(ranks, 3, 6).FullCircle);
            Check.Near(2, state.AcceptedOriginal(ranks, 3, 1, false, true, true, 40, 4, 3.15).Recovery);
            Check.Near(0, state.HurtDamageBonusAt(ranks, 3));
            Check.Near(0, state.AcceptedOriginal(ranks, 3, 1, false, true, true, 40, 4, 3.15).Recovery);
            state = RestoreLifecycle(state);
            state.Validate(3, 1, false);
            Check.Equal(1, state.DefenseBonusAt(ranks, 3.1199, IncomingDamageKind.Contact));
            Check.Equal(0, state.DefenseBonusAt(ranks, 3.12, IncomingDamageKind.Contact));
            Check.Equal(0, state.DefenseBonusAt(ranks, 3.1199, IncomingDamageKind.Projectile));
            Check.True(state.BeginSwing(ranks, 8, 6).FullCircle);
        });

        test("Jawbone e.01 independently widens evolved main impact and e.03/e.04 respect actual final geometry", () =>
        {
            var range = FromIds("e.01");
            var normal = range.ForSwing(1, 1, 1, Vector2.UnitX);
            Check.Near(2.24, range.OriginalReach(1.6, 2, true));
            Check.Near(2, range.OriginalReach(1.6, 2, false));
            Check.True(normal.HitsOriginal(new Vector2(2.23f, 0), range.OriginalReach(1.6, 2, true)));
            Check.True(!normal.HitsOriginal(new Vector2(2.23f, 0), 2));
            Check.Near(4, range.OriginalReach(1.6, 3.99, true, .08));
            Check.Near(2.3, range.GroundShockLength(true));

            var ranks = FromIds("a.02", "a.04", "e.03", "e.04");
            var first = ranks.ForSwing(1, 1, 2, Vector2.UnitX);
            var smash = ranks.ForSwing(1, 2, 2, Vector2.UnitX);
            Check.Near(.10, ranks.EliteFinalDamageBonus(true, true, true));
            Check.Near(0, ranks.EliteFinalDamageBonus(false, true, true));
            Check.Near(0, ranks.EliteFinalDamageBonus(true, false, true));
            Check.Near(0, ranks.EliteFinalDamageBonus(true, true, false));
            Check.Near(48.8, ranks.ApplyOriginalDamage(smash, 40, 40, 1, false, .10));
            var state = new ExpandedJawboneLifecycle();
            Check.True(state.CanEmitSmashWave(ranks, 0, true, smash));
            Check.True(!state.CanEmitSmashWave(ranks, 0, false, smash));
            Check.True(!state.CanEmitSmashWave(ranks, 0, true, first));
            Check.True(smash.HitsSmashWave(new Vector2(-1.6970563f, 1.6970563f)));
            Check.True(!smash.HitsSmashWave(new Vector2(-1, 1.01f)));
            Check.True(!smash.HitsSmashWave(new Vector2(-2.4001f, 0)));
            Check.True(!smash.HitsSmashWave(Vector2.UnitX));
            state.RecordSmashWave(0);
            Check.True(!state.CanEmitSmashWave(ranks, 2.9999, true, smash));
            Check.True(state.CanEmitSmashWave(ranks, 3, true, smash));
        });

        test("Jawbone e.02 detects real swept collision before and after restore, ordered nearest then ID with one receipt", () =>
        {
            var ranks = FromIds("e.02");
            var state = new ExpandedJawboneLifecycle();
            var positions = new Dictionary<int, Vector2>
            {
                [1] = new Vector2(1.5f, 0), [3] = new Vector2(.7f, 0), [2] = new Vector2(.7f, 0), [4] = Vector2.UnitX,
            };
            var hits = new List<ExpandedJawbonePushHit>();
            bool Hit(ExpandedJawbonePushHit hit) { hits.Add(hit); return true; }
            double Radius(int _) => .1;
            state.RecordPush(ranks, 0, 1, 40, Vector2.Zero, positions[1], positions, Radius, Hit);
            Check.Equal((1, 2), (hits.Single().SourceId, hits.Single().TargetId));
            Check.Near(10, hits.Single().Damage);
            Check.Near(2, state.NextPushAt!.Value);
            Check.True(state.Pushes == null);
            state.RecordPush(ranks, 1.9999, 1, 40, Vector2.Zero, positions[1], positions, Radius, Hit);
            Check.Equal(1, hits.Count);

            positions[1] = new Vector2(.15f, 0);
            state.RecordPush(ranks, 2, 1, 60, Vector2.Zero, positions[1], positions, Radius, Hit);
            state = RestoreLifecycle(state);
            state.Validate(2, 1, true);
            var expired = state.Copy();
            var before = new Dictionary<int, Vector2>(positions);
            positions[1] = new Vector2(.85f, 0);
            state.ResolvePushes(2.3, before, positions, Radius, Hit);
            Check.Equal(2, hits.Count);
            Check.Equal(2, hits.Last().TargetId);
            Check.Near(15, hits.Last().Damage);
            expired.ResolvePushes(2.3001, before, positions, Radius, Hit);
            Check.Equal(2, hits.Count);
            Check.True(expired.Pushes == null);
        });

        test("Jawbone e.05 adds one reserved evolved slot and preserves the last-hit spacing cap", () =>
        {
            var ranks = FromIds("a.04", "e.05");
            var state = new ExpandedJawboneLifecycle();
            state.AcceptedOriginal(ranks, 1, 1, true, true, true, 40, 2, 1.15);
            state = RestoreLifecycle(state);
            state.Validate(1, 1, true);
            var expired = state.Copy();
            Check.Equal(2, expired.BeginCycle(ranks, 4.0001, true, 2, .15, .9, .9).Amount);
            var boosted = state.BeginCycle(ranks, 4, true, 5, .15, .9, .25);
            Check.Equal(6, boosted.Amount);
            Check.Near(.75 + BattleWorld.FixedStep, boosted.IntervalToNext);
            Check.True(ranks.ForSwing(2, 6, boosted.Amount, Vector2.UnitX).IsGroundSmash);
            Check.True(!ranks.ForSwing(2, 5, boosted.Amount, Vector2.UnitX).IsGroundSmash);
            state.AcceptedOriginal(ranks, 4.5, 2, true, true, true, 40, 5, 4.5);
            Check.Equal(5, state.BeginCycle(ranks, 5, true, 5, .15, .9, .9).Amount);
        });

        test("Jawbone force ranks add exact health and marked damage under one precritical cap", () =>
        {
            var ranks = FromIds("a.04", "b.02", "b.03");
            var strike = ranks.ForSwing(1, 1, 1, Vector2.UnitX);
            Check.True(ranks.TracksEliteMark);
            Check.Near(58.32, ranks.ApplyOriginalDamage(strike, 50, 32, .75, marked: true));
            Check.Near(56.4, ranks.ApplyOriginalDamage(strike, 50, 32, .74999, marked: true));
            Check.Near(55.76, ranks.ApplyOriginalDamage(strike, 50, 32, .75, marked: false));
            Check.Near(72, ranks.ApplyOriginalDamage(strike, 70, 32, 1, marked: true));
            var plain = default(ExpandedJawboneRanks);
            var ordinary = plain.ForSwing(1, 1, 1, Vector2.UnitX);
            Check.True(!plain.TracksEliteMark);
            Check.Near(50, plain.ApplyOriginalDamage(ordinary, 50, 32, .75, marked: true));
        });

        test("Jawbone elite marks need three consecutive qualifying originals and restore the inclusive expiry", () =>
        {
            var ledger = new ExpandedJawboneEliteMarks();
            ledger.RecordOriginalHit(7, elite: true, survived: true, .1);
            ledger.RecordOriginalHit(7, elite: true, survived: true, .2);
            Check.True(!ledger.IsMarked(7, .2));
            ledger.RecordOriginalHit(8, elite: false, survived: true, .3);
            Check.Equal(0, ledger.LastEliteId);
            Check.Equal(0, ledger.ConsecutiveHits);
            ledger.RecordOriginalHit(7, elite: true, survived: true, .4);
            ledger.RecordOriginalHit(7, elite: true, survived: true, .5);
            Check.True(!ledger.IsMarked(7, .5));
            ledger = Restore(ledger);
            ledger.Validate(.5);
            ledger.RecordOriginalHit(7, elite: true, survived: true, .6);
            Check.Equal(7, ledger.Marks!.Single().Id);
            Check.Near(.6, ledger.Marks!.Single().At);
            Check.True(!ledger.IsMarked(8, .6));
            Check.True(ledger.IsMarked(7, 3.6));
            Check.True(!ledger.IsMarked(7, 3.6001));
            var detached = ledger.Copy();
            detached.RemoveTarget(7);
            Check.True(detached.Marks == null && ledger.Marks != null);
            var pruned = ledger.Copy();
            pruned.Prune(.7, id => id != 7);
            Check.Equal(0, pruned.LastEliteId);
            Check.True(pruned.Marks == null);

            ledger.RecordOriginalHit(7, elite: true, survived: false, .7);
            Check.True(ledger.Marks == null);
            Check.Equal(0, ledger.LastEliteId);
            ledger.Validate(.7);
        });

        test("Jawbone knockback bonuses use one cap and ground shock faces the actual reversed final strike", () =>
        {
            var ranks = FromIds("a.02", "a.04", "b.01", "b.04", "b.05");
            Check.Near(.25, ranks.Knockback(0, 3));
            Check.Near(.45, ranks.Knockback(0, 4));
            Check.Near(1, ranks.Knockback(.8, 4));
            Check.Near(.8, default(ExpandedJawboneRanks).Knockback(.8, 4));

            var first = ranks.ForSwing(1, 1, 2, Vector2.UnitY);
            var last = ranks.ForSwing(1, 2, 2, Vector2.UnitY);
            Check.True(!ranks.CanEmitGroundShock(first, 1, null));
            Check.True(ranks.CanEmitGroundShock(last, 1, null));
            Check.True(!ranks.CanEmitGroundShock(last, 1.999, 2));
            Check.True(ranks.CanEmitGroundShock(last, 2, 2));
            Check.Near(2, ExpandedJawboneRanks.GroundShockCooldown);
            Check.True(last.HitsGroundShock(new Vector2(.3f, -2)));
            Check.True(last.HitsGroundShock(new Vector2(-.3f, -2)));
            Check.True(!last.HitsGroundShock(new Vector2(.301f, -1)));
            Check.True(!last.HitsGroundShock(new Vector2(0, -2.001f)));
            Check.True(!last.HitsGroundShock(new Vector2(0, .001f)));
            Check.True(!first.HitsGroundShock(Vector2.UnitY));
            Check.True(!default(ExpandedJawboneRanks).CanEmitGroundShock(last, 1, null));
        });

        test("Jawbone second original reverses and the final original reduces its base half angle before bonuses", () =>
        {
            var ranks = Ranks("01", "02", "04");
            var forward = new Vector2(.6f, .8f);
            var first = ranks.ForSwing(1, 1, 2, forward, 4);
            var last = ranks.ForSwing(1, 2, 2, forward, 4);
            Check.Equal(forward, first.Direction);
            Check.Equal(-forward, last.Direction);
            Check.Near(74, first.HalfAngle);
            Check.Near(50, last.HalfAngle);
            Check.True(!first.IsGroundSmash && last.IsGroundSmash);
            Check.Near(0, first.DamageBonus);
            Check.Near(.12, last.DamageBonus);
            Check.Near(53.84, last.OriginalDamage(50, 32));
            Check.Near(72, last.OriginalDamage(70, 32));
            Check.Near(50, first.OriginalDamage(50, 32));
            Check.True(last.HitsOriginal(-forward, 1));
            Check.True(!last.HitsOriginal(forward, 1));
            Check.True(last.HitsOriginal(new Vector2(-.9f, -.2f), 1));

            var alone = ranks.ForSwing(1, 1, 1, Vector2.Zero);
            Check.Equal(Vector2.UnitX, alone.Direction);
            Check.True(alone.IsGroundSmash);
            Check.Near(46, alone.HalfAngle);
            Check.Throws<ArgumentOutOfRangeException>(() => ranks.ForSwing(1, 2, 1, Vector2.UnitX));
            Check.Throws<ArgumentOutOfRangeException>(() => ranks.ForSwing(1, 1, 7, Vector2.UnitX));
        });

        test("Jawbone fourth and sixth cycle supports occur once at the opening and combine at cycle twelve", () =>
        {
            var ranks = Ranks("01", "02", "03", "04", "05");
            var rear = ranks.ForSwing(4, 1, 3, Vector2.UnitX);
            Check.True(rear.RearStrike && !rear.FourWayStrike);
            Check.True(rear.HitsRear(-Vector2.UnitX, 1));
            Check.True(!rear.HitsRear(Vector2.UnitX, 1));
            Check.True(!ranks.ForSwing(4, 2, 3, Vector2.UnitX).RearStrike);
            Check.True(!ranks.ForSwing(4, 3, 3, Vector2.UnitX).RearStrike);
            var fourWay = ranks.ForSwing(6, 1, 3, Vector2.UnitX);
            Check.True(fourWay.FourWayStrike && !fourWay.RearStrike);
            Check.True(!ranks.ForSwing(6, 2, 3, Vector2.UnitX).FourWayStrike);

            var both = ranks.ForSwing(12, 1, 1, Vector2.UnitX);
            Check.True(both.RearStrike && both.FourWayStrike && both.IsGroundSmash);
            Check.Near(70, both.RearHalfAngle);
            Check.True(both.HitsRear(new Vector2(-.5f, .8660254f), 1));
            Check.Near(15, ExpandedJawboneSwing.SupportDamage(50));

            var none = ranks.ForSwing(5, 1, 1, Vector2.UnitX);
            Check.True(!none.RearStrike && !none.FourWayStrike);
            var plain = default(ExpandedJawboneRanks).ForSwing(12, 1, 1, Vector2.UnitY);
            Check.True(!plain.RearStrike && !plain.FourWayStrike && !plain.IsGroundSmash);
            Check.Near(60, plain.HalfAngle);
        });

        test("Jawbone four-way collision rotates with facing and applies one inclusive target predicate", () =>
        {
            var ranks = Ranks("05");
            var swing = ranks.ForSwing(6, 1, 1, new Vector2(1, 1));
            Check.True(swing.HitsFourWay(Vector2.Zero, 1));
            Check.True(swing.HitsFourWay(new Vector2(1, 1), 2));
            Check.True(swing.HitsFourWay(new Vector2(-1, 1), 2));
            Check.True(swing.HitsFourWay(new Vector2(-1, -1), 2));
            Check.True(swing.HitsFourWay(new Vector2(1, -1), 2));
            Check.True(!swing.HitsFourWay(Vector2.UnitX, 2));
            Check.True(!swing.HitsFourWay(Vector2.UnitY, 2));

            swing = ranks.ForSwing(6, 1, 1, Vector2.UnitX);
            static Vector2 AtAngle(double degrees) => new Vector2(
                (float)Math.Cos(degrees * Math.PI / 180), (float)Math.Sin(degrees * Math.PI / 180));
            Check.True(swing.HitsFourWay(AtAngle(25), 1));
            Check.True(!swing.HitsFourWay(AtAngle(25.01), 1));
            Check.True(!swing.HitsFourWay(Vector2.UnitY * 1.001f, 1));
        });
    }

    private static ExpandedJawboneLifecycle RestoreLifecycle(ExpandedJawboneLifecycle state)
    {
        using var save = new MemoryStream();
        var serializer = new DataContractJsonSerializer(typeof(ExpandedJawboneLifecycle));
        serializer.WriteObject(save, state);
        save.Position = 0;
        return (ExpandedJawboneLifecycle)serializer.ReadObject(save)!;
    }

    private static ExpandedJawboneRanks FromIds(params string[] ids) =>
        ExpandedJawboneRanks.FromRanks(ids.ToDictionary(id => "weapon.jawbone." + id, _ => 1));

    private static ExpandedJawboneEliteMarks Restore(ExpandedJawboneEliteMarks ledger)
    {
        using var stream = new MemoryStream();
        var serializer = new DataContractJsonSerializer(typeof(ExpandedJawboneEliteMarks));
        serializer.WriteObject(stream, ledger);
        stream.Position = 0;
        return (ExpandedJawboneEliteMarks)serializer.ReadObject(stream)!;
    }

    private static ExpandedJawboneRanks Ranks(params string[] numbers) =>
        ExpandedJawboneRanks.FromRanks(numbers.ToDictionary(number => "weapon.jawbone.a." + number, _ => 1));
}
