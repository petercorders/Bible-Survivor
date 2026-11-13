using System;
using System.Linq;
using BibleSurvivor.Core;

namespace BibleSurvivor.Core.Tests;

internal static class CombatTests
{
    public static void Register(Action<string,Action> test)
    {
        test("Bible cooldown reduction affects downtime only", () => {
            var stats = CombatStats.For("bible",8,new CombatBonuses {CooldownReduction=0.46});
            Check.Near(5.62,stats.Cycle); Check.Near(4,stats.Duration);
            var clock=new AttackClock(stats); Check.Equal(1,clock.Advance(0).Count);
            Check.Equal(0,clock.Advance(5.61).Count); Check.Equal(1,clock.Advance(0.01).Count);
        });
        test("Jawbone emits its second swing at 0.15 seconds", () => {
            var clock=new AttackClock(CombatStats.For("jawbone",2,new CombatBonuses()));
            Check.Equal(1,clock.Advance(0).Count); Check.Equal(0,clock.Advance(0.14).Count);
            Check.Equal(1,clock.Advance(0.01).Count); Check.Equal(1,clock.Advance(0.85).Count);
        });
        test("Sling amount and additive damage bonuses compose without double counting", () => {
            var stats=CombatStats.For("sling",8,new CombatBonuses {DamageBonus=.75,ExtraAmount=3});
            Check.Equal(7,stats.Amount); Check.Near(52.5,stats.Damage); Check.Equal(2,stats.Pierce);
            Check.Near(.2,CombatStats.For("sling",3,new CombatBonuses {CooldownReduction=.99}).Cycle);
        });
        test("Attack clock pause freezes time; reset clears pooled clock state", () => {
            var clock=new AttackClock(CombatStats.For("sling",1,new CombatBonuses()));
            clock.Advance(0); clock.Paused=true; Check.Equal(0,clock.Advance(10).Count);
            clock.Paused=false; Check.Equal(0,clock.Advance(1.1).Count); Check.Equal(1,clock.Advance(.1).Count);
            clock.Reset(); Check.Equal(1,clock.Advance(0).Count);
        });
        test("Bible same target cooldown and projectile pierce reset", () => {
            var hits=new HitLedger(1.7,int.MaxValue);
            Check.True(hits.TryHit(1,0)); Check.True(!hits.TryHit(1,1.6)); Check.True(hits.TryHit(1,1.7));
            var stone=new HitLedger(double.PositiveInfinity,2);
            Check.True(stone.TryHit(1,0)); Check.True(!stone.TryHit(1,.1)); Check.True(stone.TryHit(2,.1));
            Check.True(!stone.TryHit(3,.2)); stone.Reset(); Check.True(stone.TryHit(3,0));
        });
        test("120 degree jawbone arc excludes enemies behind or beyond range", () => {
            Check.True(HitGeometry.InArc(1,0,1,0,2,120));
            Check.True(!HitGeometry.InArc(-1,0,1,0,2,120)); Check.True(!HitGeometry.InArc(3,0,1,0,2,120));
            Check.True(!HitGeometry.InArc(0,1,1,0,2,120));
        });
        test("Attack frame partition preserves chronological emissions", () => {
            var stats=CombatStats.For("sling",8,new CombatBonuses());
            var a=new AttackClock(stats).Advance(3).Select(x=>x.At).ToArray();
            var clock=new AttackClock(stats); var b=Enumerable.Range(0,30).SelectMany(_=>clock.Advance(.1)).Select(x=>x.At).ToArray();
            Check.Equal(a.Length,b.Length); for(var i=0;i<a.Length;i++) Check.Near(a[i],b[i]);
        });
        test("Overlapping high-speed bursts stay chronological and additive area composes", () => {
            var stats=CombatStats.For("sling",8,new CombatBonuses {CooldownReduction=.8});
            var events=new AttackClock(stats).Advance(1);
            Check.True(events.Select(x=>x.At).SequenceEqual(events.Select(x=>x.At).OrderBy(x=>x)));
            Check.Near(2,CombatStats.For("bible",8,new CombatBonuses {AreaBonus=.5}).AreaMultiplier);
        });
    }
}
