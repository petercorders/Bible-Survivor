using System;
using System.IO;
using System.Linq;
using BibleSurvivor.Core;

namespace BibleSurvivor.Core.Tests;

internal static class SaveTests
{
    public static void Register(Action<string, Action> test)
    {
        test("R01 death resets run but preserves earned gold and permanent purchases", () => {
            var state = GameSnapshot.New(); state.Profile.Gold = 100;
            var game = new AdamSession(state, WaveTests.Adam(), "sling");
            game.Purchase("attack", 100); // Takes effect only in the next new run.
            Check.Near(1, game.DamageMultiplier);
            game.Advance(10);
            foreach (var id in game.Wave!.Active.Keys.Take(5).ToArray()) game.Kill(id);
            game.Damage(1000); Check.True(state.Run == null); Check.Equal(1L, state.Profile.Gold);
            Check.Equal(1, state.Profile.Upgrades["attack"]);
            var restart = new AdamSession(state, WaveTests.Adam(), "sling");
            Check.Near(1.03, restart.DamageMultiplier); Check.Equal(1, state.Run!.RoundNumber);
        });
        test("R01 completion heals and preserves growth, gear and remainder without double gold", () => {
            var state = GameSnapshot.New(); var game = new AdamSession(state, WaveTests.Adam(), "bible");
            game.Advance(10); game.Damage(4);
            foreach (var id in game.Wave!.Active.Keys.Take(7).ToArray()) { game.Kill(id); game.Collect(id); Resolve(game); }
            var level = game.Growth.Level; var xp = game.Growth.Experience;
            game.Advance(180); Check.Equal("between_rounds", state.Run!.Stage); Check.Equal(2, state.Run.RoundNumber);
            Check.Near(state.Run.MaxHealth, state.Run.Health); Check.Equal(level, state.Run.Level); Check.Near(xp, state.Run.Experience);
            Check.Equal(2, state.Run.KillRemainder); Check.Equal(26L, state.Profile.Gold);
            Check.Equal(1, state.Run.Weapons["bible"]); Check.True(state.Profile.UnlockedWeapons.Contains("wilderness_voice"));
            game.Advance(180); Check.Equal(26L, state.Profile.Gold);
        });
        test("R01 duplicate kill and paused input grant no extra gold or XP", () => {
            var state = GameSnapshot.New(); var game = new AdamSession(state, WaveTests.Adam(), "jawbone"); game.Advance(5);
            var id = game.Wave!.Active.Keys.First(); game.Kill(id); game.Collect(id); var xp = game.Growth.Experience;
            game.Kill(id); Check.Near(xp, game.Growth.Experience);
            game.Pause(true); var health = state.Run!.Health; game.Damage(99);
            game.Kill(game.Wave.Active.Keys.First()); game.Advance(180);
            Check.Near(5, game.Wave.Elapsed); Check.Near(health, state.Run.Health); Check.Near(xp, game.Growth.Experience);
        });
        test("R01 damage iframe and defense use simulation time", () => {
            var state = GameSnapshot.New(); state.Profile.Upgrades["health"] = 2; state.Profile.Upgrades["armor"] = 1;
            var game = new AdamSession(state, WaveTests.Adam(), "bible");
            game.Damage(4); game.Damage(4); Check.Near(117, state.Run!.Health);
            game.Advance(0.5); game.Damage(1); Check.Near(116, state.Run.Health);
        });
        test("R01 snapshot roundtrip resumes clock, active enemies and growth", () => WithStore(store => {
            var state = GameSnapshot.New(); var game = new AdamSession(state, WaveTests.Adam(), "sling"); game.Advance(20);
            game.Kill(game.Wave!.Active.Keys.First()); game.Pause(true); game.Capture();
            var revision = store.Commit(state, 0, "checkpoint-1"); Check.Equal(1L, revision);
            var restored = store.Load()!; var resumed = AdamSession.Resume(restored, WaveTests.Adam());
            Check.Near(20, resumed.Wave!.Elapsed); Check.True(resumed.Wave.Paused);
            Check.Near(game.Growth.Experience, resumed.Growth.Experience);
            Check.Equal(game.Wave.Active.Count, resumed.Wave.Active.Count);
        }));
        test("Store rejects stale revision, preserves backup and supports idempotent retry", () => WithStore(store => {
            var state = GameSnapshot.New(); Check.Equal(1L, store.Commit(state, 0, "create"));
            Check.Equal(1L, store.Commit(state, 0, "create"));
            state.Profile.Gold = 10; Check.Throws<InvalidOperationException>(() => store.Commit(state, 0, "stale"));
            Check.Equal(2L, store.Commit(state, 1, "earn"));
            Check.Equal(10L, store.Load()!.Profile.Gold);
            Check.Equal(0L, store.LoadBackup()!.Profile.Gold);
        }));
        test("Store recovers corrupt primary from backup without overwriting it on read", () => WithStore(store => {
            var state = GameSnapshot.New(); store.Commit(state, 0, "one"); state.Profile.Gold = 10; store.Commit(state, 1, "two");
            File.WriteAllText(store.Path, "broken"); Check.Equal(0L, store.Load()!.Profile.Gold);
            Check.Equal("broken", File.ReadAllText(store.Path));
        }));
        test("Store refuses future schema even when valid older backup exists", () => WithStore(store => {
            var state = GameSnapshot.New(); store.Commit(state, 0, "one"); store.Commit(state, 1, "two");
            var json = File.ReadAllText(store.Path).Replace("\"SchemaVersion\":1", "\"SchemaVersion\":999");
            File.WriteAllText(store.Path, json); Check.Throws<UnsupportedSaveVersionException>(() => store.Load());
        }));
        test("Store failure during staging leaves committed state intact", () => WithStore(store => {
            var state = GameSnapshot.New(); store.Commit(state, 0, "one"); state.Profile.Gold = 10;
            var broken = new SaveStore(store.Path, () => throw new IOException("simulated power interruption"));
            Check.Throws<IOException>(() => broken.Commit(state, 1, "two")); Check.Equal(0L, store.Load()!.Profile.Gold);
        }));
        test("Store same operation ID with altered payload is rejected", () => WithStore(store => {
            var state = GameSnapshot.New(); store.Commit(state, 0, "one"); state.Profile.Gold = 1;
            Check.Throws<InvalidOperationException>(() => store.Commit(state, 0, "one"));
        }));
        test("Corrupt or invalid state fails without inventing a new profile", () => WithStore(store => {
            Check.True(store.Load() == null); File.WriteAllText(store.Path, "broken");
            Check.Throws<InvalidDataException>(() => store.Load());
            var invalid = GameSnapshot.New(); invalid.Profile.Gold = -1;
            Check.Throws<InvalidDataException>(() => store.Commit(invalid, 0, "bad"));
        }));
        test("Transactional command failure and uncertain-result retry do not duplicate gold", () => WithStore(store => {
            store.Transact(0,"start", _=>{});
            var broken=new SaveStore(store.Path,()=>throw new IOException("power failure"));
            Check.Throws<IOException>(()=>broken.Transact(1,"earn",s=>s.Profile.Gold++));
            Check.Equal(0L,store.Load()!.Profile.Gold);
            var earned=store.Transact(1,"earn",s=>s.Profile.Gold++); Check.Equal(1L,earned.Profile.Gold);
            var replay=store.Transact(1,"earn",s=>s.Profile.Gold++);Check.Equal(1L,replay.Profile.Gold);
        }));
        test("XP is picked up separately and level-up freezes time until a valid choice", () => {
            var state=GameSnapshot.New();var game=new AdamSession(state,WaveTests.Adam(),"sling");game.Advance(10);
            foreach(var id in game.Wave!.Active.Keys.Take(5).ToArray()) {game.Kill(id);game.Collect(id);}
            Check.Equal(2,game.Growth.Level);var time=game.Wave.Elapsed;game.Advance(10);Check.Near(time,game.Wave.Elapsed);
            Check.True(!game.Choose("not-offered"));Check.True(game.Choose(game.PendingChoices[0]));
            Check.True(!game.Choose("not-offered"));game.Advance(1);Check.Near(time+1,game.Wave.Elapsed);
        });
        test("Review HIGH1: lethal Session.Hit credits exactly one kill and XP drop", () => {
            var state=GameSnapshot.New();var game=new AdamSession(state,WaveTests.Adam(),"sling");game.Advance(2);
            var enemy=game.Wave!.Active.Values.First();game.Hit(enemy.Id,enemy.Health);
            Check.Equal(1,state.Run!.KillRemainder);Check.Equal(enemy.RawExperience,state.Run.Drops[enemy.Id]);
            game.Hit(enemy.Id,enemy.Health);Check.Equal(1,state.Run.KillRemainder);
        });
        test("Review HIGH2: completed round's pending choices survive save and resume", () => WithStore(store => {
            var state=GameSnapshot.New();var game=new AdamSession(state,WaveTests.Adam(),"bible");game.Advance(10);
            foreach(var enemy in game.Wave!.Active.Values.Take(7).ToArray()) game.Hit(enemy.Id,enemy.Health);
            game.Advance(180);Check.True(game.PendingChoices.Count>0);var choices=game.PendingChoices.ToArray();
            store.Commit(state,0,"complete");var loaded=store.Load()!;var resumed=AdamSession.Resume(loaded,WaveTests.Adam());
            Check.True(choices.SequenceEqual(resumed.PendingChoices));Resolve(resumed);Check.Equal(0,loaded.Run!.PendingLevels);
            Check.Equal(2,loaded.Run.RoundNumber);Check.Equal(26L,loaded.Profile.Gold);
            resumed.Advance(180);Check.Equal(26L,loaded.Profile.Gold);
        }));
        test("Review MEDIUM: selected passives connect to attack, movement and retaliation", () => {
            var state=GameSnapshot.New();var game=new AdamSession(state,WaveTests.Adam(),"bible");
            Check.True(game.Inventory.TryChoose("stones",true));Check.True(game.Inventory.TryChoose("cloud",true));
            Check.True(game.Inventory.TryChoose("thunder",true));Check.True(game.Inventory.TryChoose("whirlwind",true));
            Check.True(game.Inventory.TryChoose("east_wind",true));Check.True(game.Inventory.TryChoose("underwear",true));
            var profile=game.AttackProfile("bible");Check.Equal(2,profile.Amount);Check.Near(3.3,profile.Duration);
            Check.Near(6.06,profile.Cycle);Check.Near(1.1,profile.AreaMultiplier);Check.Near(1.1,profile.ProjectileSpeedMultiplier);
            Check.Near(1.1,game.MoveSpeedMultiplier);
            var other=GameSnapshot.New();var thorns=new AdamSession(other,WaveTests.Adam(),"sling");
            thorns.Inventory.TryChoose("crown",true);thorns.Inventory.TryChoose("hair",true);
            thorns.Damage(1);Check.Near(5.5,thorns.LastRetaliationDamage);thorns.Damage(1);Check.Near(0,thorns.LastRetaliationDamage);
        });
        test("Free full refund preserves unlocks, active-run snapshot and returns purchase cost once", () => {
            var state=GameSnapshot.New();state.Profile.Gold=100;PermanentRules.Purchase(state.Profile,"attack");
            var game=new AdamSession(state,WaveTests.Adam(),"bible");Check.Near(1.03,game.DamageMultiplier);
            Check.Equal(100L,PermanentRules.RefundAll(state.Profile));Check.Near(1.03,game.DamageMultiplier);
            Check.Equal(0L,PermanentRules.RefundAll(state.Profile));Check.Equal(100L,state.Profile.Gold);
            Check.Equal(3,state.Profile.UnlockedWeapons.Count);
        });
    }

    private static void Resolve(AdamSession game)
    { while (game.PendingChoices.Count > 0) Check.True(game.Choose(game.PendingChoices[0])); }

    private static void WithStore(Action<SaveStore> test)
    {
        var directory = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "bible-survivor-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try { test(new SaveStore(System.IO.Path.Combine(directory, "save.json"))); }
        finally { Directory.Delete(directory, true); }
    }
}
