using System;
using System.Linq;
using System.Numerics;
using BibleSurvivor.Sim;

namespace BibleSurvivor.Core.Tests;

internal static partial class PermanentSkillCombatTests
{
    private readonly record struct ExperienceCase(int Level = 4, double Health = 1, int Gems = 1,
        double GemAge = 4, int Crowd = 0, ActionRound Round = ActionRound.FirstWave);

    private readonly record struct ExperienceResult(double Raw, double Progress, int Level, bool Pending, string Stage);

    private static void RegisterExperienceEvidence(Action<string, Action> test)
    {
        var ordinary = new ExperienceCase(Level: 4);
        for (var rank = 1; rank <= 3; rank++)
            Experience("growth", rank, ordinary, null, .03);
        Experience("common.xp.02", 1, new ExperienceCase(Gems: 4), new ExperienceCase(Gems: 5), .05);
        Experience("common.xp.03", 1, new ExperienceCase(Level: 5), new ExperienceCase(Level: 6), .05);
        Experience("common.xp.04", 1, ordinary, null, .03);
        Experience("common.xp.05", 1, new ExperienceCase(Health: .90), new ExperienceCase(Health: .899), .03);
        Experience("common.xp.06", 1, new ExperienceCase(Level: 20), new ExperienceCase(Level: 19), .04);
        Experience("common.xp.07", 1, new ExperienceCase(GemAge: 1.98), new ExperienceCase(GemAge: 2.01), .04);
        Experience("common.xp.08", 1, new ExperienceCase(Crowd: 6), new ExperienceCase(Crowd: 5), .05);
        Experience("common.xp.09", 1, ordinary, null, .04);
        Experience("common.xp.10", 1, new ExperienceCase(Round: ActionRound.BossRoundWave), ordinary, .06);

        void Experience(string id, int rank, ExperienceCase active, ExperienceCase? inactive, double gain) =>
            test($"Experience combat and replay {id}@{rank}", () => CheckExperience(id, rank, active, inactive, gain));
    }

    private static void CheckExperience(string id, int rank, ExperienceCase active, ExperienceCase? inactive, double gain)
    {
        WithStore(store =>
        {
            var (prior, bought) = BuyActionRank(store, id, rank);
            var before = Measure(prior, active);
            var after = Measure(bought, active, replay: true);
            Check.Equal(before.Raw, after.Raw);
            Check.Near(before.Raw * gain, after.Progress - before.Progress);
            if (inactive is ExperienceCase off)
            {
                var offBefore = Measure(prior, off);
                var offAfter = Measure(bought, off, replay: true);
                Check.Equal(offBefore.Raw, offAfter.Raw);
                Check.Near(offBefore.Progress, offAfter.Progress);
            }
            else
            {
                var startXp = ExperienceRules.Required(active.Level) - (before.Progress + before.Raw * gain / 2);
                var lower = Measure(prior, active, startXp);
                var upper = Measure(bought, active, startXp, replay: true);
                Check.Equal(active.Level, lower.Level);
                Check.True(!lower.Pending);
                Check.Equal(active.Level + 1, upper.Level);
                Check.True(upper.Pending);
            }

            if (id == "common.xp.10")
            {
                var atFinishBefore = Measure(prior, active, finishWave: true);
                var atFinishAfter = Measure(bought, active, replay: true, finishWave: true);
                Check.Equal("boss_intro", atFinishBefore.Stage);
                Check.Equal("boss_intro", atFinishAfter.Stage);
                Check.Equal(atFinishBefore.Raw, atFinishAfter.Raw);
                Check.Near(atFinishBefore.Raw * gain, atFinishAfter.Progress - atFinishBefore.Progress);
            }

            ExperienceResult Measure(GameSnapshot profile, ExperienceCase sample, double xp = 0,
                bool replay = false, bool finishWave = false)
            {
                var (state, initial, definition, speeds) = PrepareActionWorld(profile, sample.Round);
                var run = state.Run!;
                run.Level = sample.Level;
                run.Experience = xp;
                run.Health = run.MaxHealth * sample.Health;
                var actions = run.PermanentActions!;
                actions.GemsCollected = sample.Gems;
                actions.LastGemAt = initial.CombatTime - sample.GemAge;
                var session = AdamSession.Resume(state, definition);
                var target = PrepareActionDrop(session);
                var raw = session.DroppedExperience[target];
                Check.True(raw > 0);
                var tuning = new BattleTuning();
                var setup = new BattleWorld(session, speeds, 7, tuning).Capture();
                SetActionActors(setup, sample.Crowd);
                setup.Drops.Add(new BattleEntityState { Id = target, X = .1f, Y = 0 });
                var world = BattleWorld.Restore(session, speeds, setup, tuning);
                var restored = replay ? ReopenActionCombat(store, state, definition, world, speeds, tuning) : null;
                var result = Advance(world);
                if (restored != null)
                    Check.Equal(result, Advance(restored));
                return result;

                ExperienceResult Advance(BattleWorld battlefield)
                {
                    var live = battlefield.Session;
                    if (finishWave) live.Advance(180 - live.CombatTime);
                    else
                    {
                        battlefield.Advance(BattleWorld.FixedStep, Vector2.Zero);
                        Check.Equal(1, battlefield.Collected);
                    }
                    Check.True(!live.DroppedExperience.ContainsKey(target));
                    var progress = live.Growth.Experience - xp;
                    for (var level = sample.Level; level < live.Growth.Level; level++)
                        progress += ExperienceRules.Required(level);
                    return new ExperienceResult(raw, progress, live.Growth.Level, live.PendingChoices.Count > 0, live.Stage);
                }
            }
        });
    }
}
