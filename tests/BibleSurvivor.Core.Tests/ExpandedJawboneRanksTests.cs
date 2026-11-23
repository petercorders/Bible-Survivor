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
