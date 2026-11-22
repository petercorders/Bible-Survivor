using System;
using System.IO;
using System.Linq;
using BibleSurvivor.Core;
using BibleSurvivor.Sim;

namespace BibleSurvivor.Core.Tests;

internal static class CampaignHundredTenTests
{
    internal static void Register(Action<string, Action> test)
    {
        test("R110 preserves the campaign fingerprint through Psalms", () =>
            CampaignSixtyTests.MatchFingerprints("r110-build-round-fingerprints.txt", 110));

        test("R110 installed rounds match the C7 waves and boss tuning", () =>
        {
            for (var round = 101; round <= 110; round++)
            {
                var installed = CampaignContent.Round(round);
                var trial = ExperimentalRounds.Create(round, "C7");
                Check.Equal(trial.Definition.ConcurrencyCap, installed.ConcurrencyCap);
                Check.Equal(trial.Definition.CompletionGold, installed.CompletionGold);
                Check.Equal(trial.Definition.RawExperience, installed.RawExperience);
                var speeds = CampaignContent.SpeedsFor(round);
                Check.Equal(trial.Definition.Waves.Count, installed.Waves.Count);
                foreach (var (actual, expected) in installed.Waves.Zip(trial.Definition.Waves))
                {
                    Check.Equal(expected.Start, actual.Start);
                    Check.Equal(expected.End, actual.End);
                    Check.Equal(expected.Groups.Count, actual.Groups.Count);
                    foreach (var (group, target) in actual.Groups.Zip(expected.Groups))
                    {
                        Check.Equal(target.Count, group.Count);
                        Check.Equal(target.Health, group.Health);
                        Check.Equal(target.ContactDamage, group.ContactDamage);
                        Check.Equal(target.SpawnTimePower, group.SpawnTimePower);
                        Check.Equal(trial.Speeds[target.MonsterId], speeds[group.MonsterId]);
                    }
                }

                var boss = BossDefinition.Find(round);
                Check.Equal(trial.Boss != null, boss != null);
                if (boss == null) continue;
                var targetBoss = trial.Boss!;
                Check.Equal(targetBoss.Actions, boss.Actions);
                Check.Equal(targetBoss.MaxHealth, boss.MaxHealth);
                Check.Equal(targetBoss.KillGold, boss.KillGold);
                Check.Equal(targetBoss.Contact, boss.Contact);
                Check.Equal(targetBoss.AttackPeriod, boss.AttackPeriod);
                Check.Equal(targetBoss.AttackDamage, boss.AttackDamage);
                Check.Equal(targetBoss.BossActorId, boss.BossActorId);
                Check.Equal(targetBoss.SupportHitPoints, boss.SupportHitPoints);
                Check.Equal(targetBoss.SupportContact, boss.SupportContact);
                Check.Equal(targetBoss.SupportRatio, boss.SupportRatio);
                Check.True(targetBoss.Thresholds.SequenceEqual(boss.Thresholds));
            }
        });

        test("R110 replays R100 with explicit Psalms entry and preserves receipts", () =>
            CampaignCheckpointTests.MatchReplay(100, 110, 99, 102, 105, 108));

        test("R110 completion blocks round 111 when loaded", () =>
        {
            Check.Equal(110, CampaignContent.LastInstalledRound);
            Check.Equal("psalms", LaterCampaignBooks.Find(103)!.Id);
            Check.True(LaterCampaignBooks.StartsBook(103));
            Check.True(LaterCampaignBooks.Find(111) == null);
            Check.Throws<ArgumentOutOfRangeException>(() => CampaignContent.Round(111));
            const string path = "tests/BibleSurvivor.Core.Tests/Fixtures/r110-build-complete-save.json";
            var before = File.ReadAllBytes(path);
            var saved = new SaveStore(path).Load()!;
            var session = AdamSession.Resume(saved, CampaignContent.Round(110));
            Check.True(session.ContentComplete && !session.CanEnterLaterBook);
            Check.Equal(111, saved.Run!.RoundNumber);
            Check.True(before.SequenceEqual(File.ReadAllBytes(path)));
        });
    }
}
