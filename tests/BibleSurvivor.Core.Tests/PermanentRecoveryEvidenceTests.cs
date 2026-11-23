using System;
using System.Linq;
using System.Numerics;
using BibleSurvivor.Sim;

namespace BibleSurvivor.Core.Tests;

internal static partial class PermanentSkillCombatTests
{
    private readonly record struct RecoveryRateCase(double Health = .5, double HitAge = 4, double StillAge = -1);
    private enum RecoveryEvent { NormalKill, EliteKill, BossSupportKill, IncomingDamage }
    private readonly record struct RecoveryEventCase(RecoveryEvent Event, bool Original = true, int NormalKills = 0,
        bool InsideCooldown = false, bool MissThreshold = false, bool AlreadyLow = false);
    private readonly record struct RecoveryEventResult(double Restored, double Health, int DropCount);

    private static void RegisterRecoveryEvidence(Action<string, Action> test)
    {
        var ordinary = new RecoveryRateCase(Health: .5);
        var full = new RecoveryRateCase(Health: 1);
        for (var rank = 1; rank <= 3; rank++)
            Rate("regen", rank, ordinary, full, .1);
        Rate("common.regen.02", 1, new RecoveryRateCase(HitAge: 8), new RecoveryRateCase(HitAge: 7.999), .1);
        Rate("common.regen.03", 1, new RecoveryRateCase(Health: .30), new RecoveryRateCase(Health: .301), .1);
        Rate("common.regen.04", 1, new RecoveryRateCase(StillAge: 1), new RecoveryRateCase(StillAge: .999), .1);
        Rate("common.regen.07", 1, ordinary, full, .1);
        Rate("common.regen.10", 1, ordinary, full, .2);

        Event("common.regen.05", new RecoveryEventCase(RecoveryEvent.NormalKill, NormalKills: 9), 1,
            new RecoveryEventCase(RecoveryEvent.NormalKill, NormalKills: 8),
            new RecoveryEventCase(RecoveryEvent.NormalKill, Original: false, NormalKills: 9));
        Event("common.regen.06", new RecoveryEventCase(RecoveryEvent.EliteKill), 1,
            new RecoveryEventCase(RecoveryEvent.EliteKill, InsideCooldown: true),
            new RecoveryEventCase(RecoveryEvent.EliteKill, Original: false),
            new RecoveryEventCase(RecoveryEvent.NormalKill));
        Event("common.regen.08", new RecoveryEventCase(RecoveryEvent.IncomingDamage), 2,
            new RecoveryEventCase(RecoveryEvent.IncomingDamage, MissThreshold: true),
            new RecoveryEventCase(RecoveryEvent.IncomingDamage, AlreadyLow: true),
            new RecoveryEventCase(RecoveryEvent.IncomingDamage, InsideCooldown: true));
        Event("common.regen.09", new RecoveryEventCase(RecoveryEvent.BossSupportKill), 1,
            new RecoveryEventCase(RecoveryEvent.BossSupportKill, InsideCooldown: true),
            new RecoveryEventCase(RecoveryEvent.BossSupportKill, Original: false),
            new RecoveryEventCase(RecoveryEvent.NormalKill));

        void Rate(string id, int rank, RecoveryRateCase active, RecoveryRateCase inactive, double gain) =>
            test($"Recovery combat and replay {id}@{rank}", () => CheckRecoveryRate(id, rank, active, inactive, gain));

        void Event(string id, RecoveryEventCase active, double gain, params RecoveryEventCase[] inactive) =>
            test($"Recovery combat and replay {id}@1", () => CheckRecoveryEvent(id, active, inactive, gain));
    }

    private static void CheckRecoveryRate(string id, int rank, RecoveryRateCase active, RecoveryRateCase inactive,
        double gain)
    {
        WithStore(store =>
        {
            var (prior, bought) = BuyActionRank(store, id, rank);
            Check.Near(BattleWorld.FixedStep * gain, Measure(bought, active, true) - Measure(prior, active, false));
            Check.Near(0, Measure(bought, inactive, true) - Measure(prior, inactive, false));

            double Measure(GameSnapshot profile, RecoveryRateCase sample, bool replay)
            {
                var (state, session, definition, speeds) = PrepareActionWorld(profile, ActionRound.FirstWave);
                var sampleAt = session.CombatTime + BattleWorld.FixedStep;
                var run = state.Run!;
                var health = run.MaxHealth * sample.Health;
                run.Health = health;
                run.LastHitTime = sampleAt - sample.HitAge;
                var movement = sample.StillAge < 0 ? Vector2.UnitX : Vector2.Zero;
                if (sample.StillAge >= 0) run.PermanentActions!.StationarySince = sampleAt - sample.StillAge;
                var tuning = new BattleTuning();
                var setup = new BattleWorld(session, speeds, 7, tuning).Capture();
                SetActionActors(setup, 0);
                var world = BattleWorld.Restore(session, speeds, setup, tuning);
                var restored = replay ? ReopenActionCombat(store, state, definition, world, speeds, tuning) : null;
                world.Advance(BattleWorld.FixedStep, movement);
                var actual = session.CurrentHealth;
                if (restored != null)
                {
                    restored.Advance(BattleWorld.FixedStep, movement);
                    Check.Equal(actual, restored.Session.CurrentHealth);
                }
                if (sample.Health == 1) Check.Equal(run.MaxHealth, actual);
                return actual - health;
            }
        });
    }

    private static void CheckRecoveryEvent(string id, RecoveryEventCase active,
        RecoveryEventCase[] inactive, double gain)
    {
        WithStore(store =>
        {
            var (prior, bought) = BuyActionRank(store, id, 1);
            Check.Near(gain, Measure(bought, active, true).Restored - Measure(prior, active, false).Restored);
            foreach (var sample in inactive)
                Check.Near(0, Measure(bought, sample, true).Restored - Measure(prior, sample, false).Restored);

            RecoveryEventResult Measure(GameSnapshot profile, RecoveryEventCase sample, bool replay)
            {
                var boss = sample.Event == RecoveryEvent.BossSupportKill;
                var elite = sample.Event == RecoveryEvent.EliteKill;
                var fixture = elite
                    ? PrepareEliteRecoveryWorld(profile)
                    : PrepareActionWorld(profile, boss ? ActionRound.ActiveBoss : ActionRound.FirstWave);
                var (state, session, definition, speeds) = fixture;
                var run = state.Run!;
                run.Health = run.MaxHealth / 2;
                var actionTarget = 0;
                if (boss)
                {
                    var bossId = session.BossRules!.BossActorId;
                    session.Hit(bossId, session.Targets[bossId].Health * .75, null, original: false);
                    actionTarget = session.Targets.Keys.First(id => id != bossId);
                }
                else if (sample.Event == RecoveryEvent.IncomingDamage)
                {
                    const int incoming = 10;
                    var effective = session.EffectiveContactDamage(incoming);
                    var threshold = run.MaxHealth / 4;
                    run.Health = sample.AlreadyLow ? threshold : threshold + effective + (sample.MissThreshold ? .01 : 0);
                }
                else
                {
                    for (var index = 0; index < sample.NormalKills; index++)
                        session.Kill(session.Targets.Keys.Min());
                    actionTarget = session.Targets.Keys.First(id => session.IsEliteActor(id) == elite);
                }

                var recovery = run.PermanentRecovery!;
                var now = session.CombatTime;
                var cooldown = sample.Event == RecoveryEvent.IncomingDamage ? 12 : 5;
                if (sample.Event != RecoveryEvent.NormalKill)
                {
                    recovery.LastEventAt = now - cooldown + (sample.InsideCooldown ? .001 : 0);
                    var ready = recovery.LastEventAt + cooldown;
                    if (boss) recovery.NextBossSupportAt = ready;
                    else if (elite) recovery.NextEliteAt = ready;
                    else recovery.NextLowHealthAt = ready;
                }

                var tuning = new BattleTuning();
                var setup = new BattleWorld(session, speeds, 7, tuning).Capture();
                SetActionActors(setup, 0);
                foreach (var drop in session.DroppedExperience.Keys)
                    setup.Drops.Add(new BattleEntityState { Id = drop, X = 10, Y = 10 });
                var world = BattleWorld.Restore(session, speeds, setup, tuning);
                var restored = replay ? ReopenActionCombat(store, state, definition, world, speeds, tuning) : null;
                var result = Apply(session);
                if (restored != null) Check.Equal(result, Apply(restored.Session));
                return result;

                RecoveryEventResult Apply(AdamSession live)
                {
                    var hp = live.CurrentHealth;
                    if (sample.Event == RecoveryEvent.IncomingDamage)
                    {
                        live.Damage(10);
                        Check.True(live.LastDamageReceived > 0);
                    }
                    else
                    {
                        live.Hit(actionTarget, live.Targets[actionTarget].Health, null, sample.Original);
                        Check.True(!live.Targets.ContainsKey(actionTarget));
                        Check.True(live.DroppedExperience.ContainsKey(actionTarget));
                    }
                    return new RecoveryEventResult(live.CurrentHealth - hp + live.LastDamageReceived,
                        live.CurrentHealth, live.DroppedExperience.Count);
                }
            }
        });
    }

    private static (GameSnapshot, AdamSession, RoundDefinition,
        System.Collections.Generic.IReadOnlyDictionary<string, double>) PrepareEliteRecoveryWorld(GameSnapshot profile)
    {
        var state = SaveStore.PrepareNewChallenge(profile);
        var wave = new RoundDefinition(1, 40, 60, 25, new[]
        {
            new WaveDefinition(0, 60, new[] { new SpawnGroup("a1", 60, 30, 4), new SpawnGroup("a3", 30, 90, 4) }),
        });
        var normal = new AdamSession(state, wave, "bible");
        normal.Advance(15);
        return (state, normal, wave, Speeds);
    }
}
