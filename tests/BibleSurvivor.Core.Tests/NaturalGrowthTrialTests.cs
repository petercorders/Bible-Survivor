using System;
using System.IO;
using System.Linq;
using BibleSurvivor.Core;
using BibleSurvivor.Sim;

namespace BibleSurvivor.Core.Tests;

internal static class NaturalGrowthTrialTests
{
    internal static void Register(Action<string, Action> test)
    {
        test("R180 trials branch experimentally from the installed R100 checkpoint", () =>
        {
            const string path = "tests/BibleSurvivor.Core.Tests/Fixtures/r100-build-complete-save.json";
            var originalBytes = File.ReadAllBytes(path);
            var state = new SaveStore(path).Load()!;
            var game = AdamSession.Resume(state, CampaignContent.Round(100));
            var trial = ExperimentalRounds.Create(101, "C0");
            Check.Equal("trial_job_02", trial.Definition.Id);
            Check.Equal("job_02", CampaignContent.Round(101).Id);
            Check.Throws<ArgumentOutOfRangeException>(() => ExperimentalRounds.Create(100, "C0"));
            Check.Throws<ArgumentOutOfRangeException>(() => ExperimentalRounds.Create(181, "C0"));

            game = game.StartExperimentalRound(trial.Definition, trial.Boss);
            Check.Equal(101, game.RoundNumber);
            var world = new BattleWorld(game, trial.Speeds, 7);
            world.Advance(BattleWorld.FixedStep, System.Numerics.Vector2.Zero);
            Check.Near(BattleWorld.FixedStep, world.SimulationTime);
            Check.True(originalBytes.AsSpan().SequenceEqual(File.ReadAllBytes(path)));
        });

        test("R180 trial curves use mid-book recovery and R99-anchored boss increases", () =>
        {
            static int SteadyHealth(int round, string curve) =>
                ExperimentalRounds.Create(round, curve).Definition.Waves[0].Groups[0].Health;

            Check.Equal(2020, SteadyHealth(101, "C0"));
            Check.Equal(2020, SteadyHealth(103, "C3"));
            Check.Equal(2040, SteadyHealth(104, "C3"));
            Check.Equal(2040, SteadyHealth(103, "C1"));
            Check.Equal(SteadyHealth(104, "C0"), SteadyHealth(104, "C1"));
            Check.Near(215500, ExperimentalRounds.Create(180, "C0").Boss!.MaxHealth);
            Check.Near(215500, ExperimentalRounds.Create(180, "C1").Boss!.MaxHealth);
            Check.Near(168250, ExperimentalRounds.Create(180, "C3").Boss!.MaxHealth);
        });

        test("C4 delays only the first fast group and preserves the C3 encounter budget", () =>
        {
            var regular = ExperimentalRounds.Create(122, "C3");
            var paced = ExperimentalRounds.Create(122, "C4");
            var baseline = new WaveDirector(regular.Definition);
            var counterfactual = new WaveDirector(paced.Definition);
            baseline.Advance(11.133333333333);
            counterfactual.Advance(11.133333333333);
            Check.Equal(55, baseline.Active.Values.Count(enemy => enemy.MonsterId == "trial_steady"));
            Check.Equal(55, counterfactual.Active.Values.Count(enemy => enemy.MonsterId == "trial_steady"));
            Check.Equal(29, baseline.Active.Values.Count(enemy => enemy.MonsterId == "trial_fast"));
            Check.Equal(21, counterfactual.Active.Values.Count(enemy => enemy.MonsterId == "trial_fast"));
            Check.True(regular.Definition.Fingerprint != paced.Definition.Fingerprint);
            Check.Equal(ExperimentalRounds.Create(138, "C3").Definition.Fingerprint,
                ExperimentalRounds.Create(138, "C4").Definition.Fingerprint);
            Check.Equal(regular.Definition.RawExperience, paced.Definition.RawExperience);
            Check.Equal(regular.Definition.CompletionGold, paced.Definition.CompletionGold);
            for (var wave = 0; wave < regular.Definition.Waves.Count; wave++)
            {
                var expected = regular.Definition.Waves[wave];
                var actual = paced.Definition.Waves[wave];
                Check.Near(expected.Start, actual.Start);
                Check.Near(expected.End, actual.End);
                Check.True(expected.Groups.Select(group => (group.MonsterId, group.Count, group.Health, group.ContactDamage))
                    .SequenceEqual(actual.Groups.Select(group => (group.MonsterId, group.Count, group.Health, group.ContactDamage))));
                if (wave > 0) Check.True(actual.Groups.All(group => group.SpawnTimePower == 1));
            }
            Check.Near(ExperimentalRounds.Create(180, "C3").Boss!.MaxHealth,
                ExperimentalRounds.Create(180, "C4").Boss!.MaxHealth);
        });

        test("C5 caps ordinary contact from R121 and preserves C4 boss and pacing", () =>
        {
            Check.Equal(ExperimentalRounds.Create(120, "C4").Definition.Fingerprint,
                ExperimentalRounds.Create(120, "C5").Definition.Fingerprint);
            for (var round = 121; round <= 180; round++)
            {
                var baseline = ExperimentalRounds.Create(round, "C4");
                var capped = ExperimentalRounds.Create(round, "C5");
                Check.Equal(baseline.Boss?.FingerprintContract, capped.Boss?.FingerprintContract);
                var before = baseline.Definition.Waves.SelectMany(wave => wave.Groups).ToArray();
                var after = capped.Definition.Waves.SelectMany(wave => wave.Groups).ToArray();
                Check.True(before.Select(group => (group.MonsterId, group.Count, group.Health, group.SpawnTimePower))
                    .SequenceEqual(after.Select(group => (group.MonsterId, group.Count, group.Health, group.SpawnTimePower))));
                Check.True(before.Select(group => Math.Min(group.ContactDamage, 33))
                    .SequenceEqual(after.Select(group => group.ContactDamage)));
            }
            Check.Equal(34, ExperimentalRounds.Create(123, "C5").Boss!.SupportContact);
        });

        test("C6 extends C5 fast-group pacing only from R160", () =>
        {
            for (var round = 101; round <= 180; round++)
            {
                var baseline = ExperimentalRounds.Create(round, "C5");
                var adjusted = ExperimentalRounds.Create(round, "C6");
                Check.Equal(baseline.Boss?.FingerprintContract, adjusted.Boss?.FingerprintContract);
                if (round < 160)
                    Check.Equal(baseline.Definition.Fingerprint, adjusted.Definition.Fingerprint);
                else
                {
                    var before = baseline.Definition.Waves.SelectMany(wave => wave.Groups).ToArray();
                    var after = adjusted.Definition.Waves.SelectMany(wave => wave.Groups).ToArray();
                    Check.True(before.Select(group => (group.MonsterId, group.Count, group.Health, group.ContactDamage))
                        .SequenceEqual(after.Select(group => (group.MonsterId, group.Count, group.Health, group.ContactDamage))));
                    Check.Equal(.85, after[1].SpawnTimePower);
                    Check.Equal(before[0].SpawnTimePower, after[0].SpawnTimePower);
                    Check.True(before.Skip(2).Select(group => group.SpawnTimePower)
                        .SequenceEqual(after.Skip(2).Select(group => group.SpawnTimePower)));
                }
            }
            Check.True(ExperimentalRounds.Create(160, "C5").Definition.Fingerprint !=
                ExperimentalRounds.Create(160, "C6").Definition.Fingerprint);
        });

        test("C7 changes late ordinary health and first fast pace plus the final boss health", () =>
        {
            for (var round = 101; round <= 180; round++)
            {
                var baseline = ExperimentalRounds.Create(round, "C6");
                var adjusted = ExperimentalRounds.Create(round, "C7");
                if (round < 160)
                    Check.Equal(baseline.Definition.Fingerprint, adjusted.Definition.Fingerprint);
                else
                {
                    var before = baseline.Definition.Waves.SelectMany(wave => wave.Groups).ToArray();
                    var after = adjusted.Definition.Waves.SelectMany(wave => wave.Groups).ToArray();
                    Check.True(before.Select(group => (group.MonsterId, group.Count, group.ContactDamage,
                            Health: group.Health * 80 / 100))
                        .SequenceEqual(after.Select(group => (group.MonsterId, group.Count, group.ContactDamage, group.Health))));
                    Check.Equal(.65, after[1].SpawnTimePower);
                    Check.Equal(before[0].SpawnTimePower, after[0].SpawnTimePower);
                    Check.True(before.Skip(2).Select(group => group.SpawnTimePower)
                        .SequenceEqual(after.Skip(2).Select(group => group.SpawnTimePower)));
                }
                if (round < 180)
                    Check.Equal(baseline.Boss?.FingerprintContract, adjusted.Boss?.FingerprintContract);
                else
                {
                    Check.Near(140000, adjusted.Boss!.MaxHealth);
                    Check.Equal(baseline.Boss!.Actions, adjusted.Boss.Actions);
                    Check.Equal(baseline.Boss.SupportContact, adjusted.Boss.SupportContact);
                }
            }
        });

        test("R180 trials use the approved book boundaries and new action combinations", () =>
        {
            Check.Equal("trial_psalms_01", ExperimentalRounds.Create(103, "C0").Definition.Id);
            Check.Equal("trial_proverbs_01", ExperimentalRounds.Create(112, "C0").Definition.Id);
            Check.Equal("trial_ecclesiastes_01", ExperimentalRounds.Create(115, "C0").Definition.Id);
            Check.Equal("trial_song_of_songs_01", ExperimentalRounds.Create(118, "C0").Definition.Id);
            Check.Equal("trial_isaiah_01", ExperimentalRounds.Create(121, "C0").Definition.Id);
            Check.Equal("trial_jeremiah_01", ExperimentalRounds.Create(130, "C0").Definition.Id);
            Check.Equal("trial_lamentations_01", ExperimentalRounds.Create(139, "C0").Definition.Id);
            Check.Equal("trial_ezekiel_01", ExperimentalRounds.Create(142, "C0").Definition.Id);
            Check.Equal("trial_daniel_01", ExperimentalRounds.Create(151, "C0").Definition.Id);
            Check.Equal("trial_minor_prophets_21", ExperimentalRounds.Create(180, "C0").Definition.Id);
            Check.Equal(BossActions.DamageZone | BossActions.Chains, ExperimentalRounds.Create(102, "C0").Boss!.Actions);
            Check.Equal(BossActions.Dash | BossActions.AimedShot, ExperimentalRounds.Create(120, "C0").Boss!.Actions);
            Check.Equal(BossActions.Dash | BossActions.Slam | BossActions.Chains | BossActions.Summon |
                BossActions.AimedShot | BossActions.DamageZone, ExperimentalRounds.Create(180, "C0").Boss!.Actions);
            for (var round = 101; round <= 180; round++)
                Check.Equal(round % 3 == 0, ExperimentalRounds.Create(round, "C0").Boss != null);
        });
    }
}
