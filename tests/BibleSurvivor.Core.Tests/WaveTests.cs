using System;
using System.Linq;
using BibleSurvivor.Core;

namespace BibleSurvivor.Core.Tests;

internal static class WaveTests
{
    internal static RoundDefinition Adam() => new RoundDefinition(1, 40, 300, 25, new[] {
        new WaveDefinition(0, 60, new[] { new SpawnGroup("a1", 60, 1, 4) }),
        new WaveDefinition(60, 120, new[] { new SpawnGroup("a1", 56, 1, 4), new SpawnGroup("a2", 24, 6, 4) }),
        new WaveDefinition(120, 170, new[] { new SpawnGroup("a1", 60, 1, 4), new SpawnGroup("a2", 30, 6, 4), new SpawnGroup("a3", 10, 12, 5) }) });

    public static void Register(Action<string, Action> test)
    {
        test("R01 momentarily empty arena does not clear future waves", () => {
            var wave = new WaveDirector(Adam());
            Check.True(!wave.IsNormalComplete);
            wave.Advance(1); foreach (var id in wave.Active.Keys.ToArray()) wave.Kill(id);
            Check.True(!wave.IsNormalComplete);
        });
        test("R01 all scheduled enemies yield exactly 240 spawns and raw XP 300", () => {
            var wave = new WaveDirector(Adam()); var xp = 0; var killed = 0;
            for (var i = 0; i < 1700; i++) {
                wave.Advance(0.1);
                foreach (var id in wave.Active.Keys.ToArray()) { xp += wave.Kill(id); killed++; }
            }
            Check.Equal(240, killed); Check.Equal(300, xp); Check.True(wave.IsNormalComplete);
        });
        test("R01 concurrency is capped and unspawned budget expires at cutoff", () => {
            var wave = new WaveDirector(Adam()); wave.Advance(169);
            Check.Equal(40, wave.Active.Count);
            wave.Advance(2); foreach (var id in wave.Active.Keys.ToArray()) wave.Kill(id);
            wave.Advance(1); Check.Equal(40, wave.SpawnedCount); Check.True(!wave.IsNormalComplete);
            wave.Advance(8); Check.True(wave.IsNormalComplete);
        });
        test("R01 time limit clears live enemies at 180 seconds", () => {
            var wave = new WaveDirector(Adam()); wave.Advance(180);
            Check.True(wave.IsNormalComplete); Check.Equal(0, wave.Active.Count); Check.Near(180, wave.Elapsed);
        });
        test("R01 paused clock freezes spawns and elapsed time", () => {
            var wave = new WaveDirector(Adam()); wave.Paused = true; wave.Advance(180);
            Check.Near(0, wave.Elapsed); Check.Equal(0, wave.SpawnedCount);
        });
        test("R01 frame partition with sufficient kill capacity is deterministic", () => {
            var a = new WaveDirector(Adam()); var b = new WaveDirector(Adam());
            for (var i = 0; i < 600; i++) a.Advance(0.1);
            for (var i = 0; i < 60; i++) b.Advance(1);
            Check.Equal(a.SpawnedCount, b.SpawnedCount); Check.Equal(a.Active.Count, b.Active.Count);
        });
        test("R01 saved wave restores exact cursor, active IDs, XP and pause", () => {
            var a = new WaveDirector(Adam()); a.Advance(30); a.Paused = true;
            var b = WaveDirector.Restore(Adam(), a.Snapshot());
            Check.Equal(a.SpawnedCount, b.SpawnedCount); Check.Near(a.Elapsed, b.Elapsed);
            Check.True(b.Paused); Check.Equal(a.Active.Keys.First(), b.Active.Keys.First());
            Check.Equal(a.Kill(a.Active.Keys.First()), b.Kill(b.Active.Keys.First()));
        });
        test("R01 kill replay grants no duplicate XP", () => {
            var wave = new WaveDirector(Adam()); wave.Advance(1); var id = wave.Active.Keys.First();
            Check.True(wave.Kill(id) > 0); Check.Equal(0, wave.Kill(id));
        });
        test("Clock rejects negative and nonfinite input", () => {
            var wave = new WaveDirector(Adam()); Check.Throws<ArgumentOutOfRangeException>(() => wave.Advance(-1));
            Check.Throws<ArgumentOutOfRangeException>(() => wave.Advance(double.NaN));
        });
        test("Fractional weapon damage is retained between hits", () => {
            var wave=new WaveDirector(Adam());wave.Advance(1);var id=wave.Active.Keys.First();
            Check.Equal(0,wave.Hit(id,.4));Check.Equal(0,wave.Hit(id,.4));
            Check.True(wave.Active.ContainsKey(id));Check.True(wave.Hit(id,.4)>0);Check.True(!wave.Active.ContainsKey(id));
        });
        test("Review MEDIUM: restored NaN and infinity enemy health are rejected", () => {
            var wave=new WaveDirector(Adam());wave.Advance(2);var state=wave.Snapshot();
            state.Active[0].Health=double.NaN;Check.Throws<ArgumentException>(()=>WaveDirector.Restore(Adam(),state));
            state.Active[0].Health=double.PositiveInfinity;Check.Throws<ArgumentException>(()=>WaveDirector.Restore(Adam(),state));
        });
    }
}
