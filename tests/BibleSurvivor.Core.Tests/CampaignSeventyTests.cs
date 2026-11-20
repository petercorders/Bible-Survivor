using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using BibleSurvivor.Core;

namespace BibleSurvivor.Core.Tests;

internal static class CampaignSeventyTests
{
    private const string Fixtures = "tests/BibleSurvivor.Core.Tests/Fixtures/";

    internal static void Register(Action<string,Action> test)
    {
        test("R70 R01-R70 fingerprints match the R70 build", () =>
            CampaignSixtyTests.MatchFingerprints("r70-build-round-fingerprints.txt", 70));

        test("R70 installed books, C0 curve, and composed bosses", () =>
        {
            Check.Equal(70, CampaignContent.LastInstalledRound);
            Check.Throws<ArgumentOutOfRangeException>(() => CampaignContent.Round(71));
            foreach (var round in Enumerable.Range(61, 10))
            {
                var book = LaterCampaignBooks.Find(round)!;
                var offset = round - 22;
                var definition = CampaignContent.Round(round);
                var groups = definition.Waves.SelectMany(wave => wave.Groups).ToArray();
                Check.Equal(round < 67 ? "second_samuel" : "first_kings", book.Id);
                Check.Equal(1560 + 60 * offset, groups.Sum(group => group.Count));
                Check.Equal(7200 + 200 * offset, definition.RawExperience);
                Check.Equal(420 + 20 * offset, definition.CompletionGold);
                Check.True(groups.All(group => CampaignContent.SpeedsFor(round).ContainsKey(group.MonsterId)));
            }
            Check.Equal("lament_ash", CampaignContent.Round(61).Waves[0].Groups[0].MonsterId);
            Check.Equal("tangled_thicket", CampaignContent.Round(64).Waves[0].Groups[0].MonsterId);
            Check.Equal("dry_land_ember", CampaignContent.Round(70).Waves[0].Groups[0].MonsterId);
            Check.Equal(BossActions.Chains, BossDefinition.ForRound(63).Actions);
            Check.Equal(BossActions.Summon, BossDefinition.ForRound(66).Actions);
            Check.Equal(BossActions.Slam, BossDefinition.ForRound(69).Actions);
        });

        test("R70 completion save reproduces from the previous build without rewriting the R60 receipts", () =>
        {
            var directory = Directory.CreateTempSubdirectory("adam-r70-test-");
            try
            {
                var path = Path.Combine(directory.FullName, "save.json");
                var r60 = File.ReadAllBytes(Fixtures + "r60-build-complete-save.json");
                File.WriteAllBytes(path, r60);
                var store = new SaveStore(path);
                var state = store.Load()!;
                Check.True(r60.SequenceEqual(File.ReadAllBytes(path)));
                var session = AdamSession.Resume(state, CampaignContent.Round(60));
                var runId = state.Run!.RunId;
                var history = state.Run.LaterBossHistory!.Append(state.Run.Boss!).Select(receipt => JsonSerializer.Serialize(receipt)).ToArray();

                for (var round = 61; round <= 70; round++)
                {
                    var definition = CampaignContent.Round(round);
                    if (round is 61 or 67)
                    {
                        Check.True(session.CanEnterLaterBook);
                        Check.Throws<InvalidOperationException>(() => session.StartNextRound(definition));
                        var before = File.ReadAllBytes(path);
                        var gold = state.Profile.Gold;
                        var candidate = session.PrepareLaterBook(definition);
                        var revision = state.Revision;
                        var operation = "book-entry-" + round;
                        var failingStore = new SaveStore(path, () => throw new IOException("before replace"));
                        Check.Throws<IOException>(() => failingStore.Commit(candidate, revision, operation));
                        Check.True(before.SequenceEqual(File.ReadAllBytes(path)));
                        Check.True(session.CanEnterLaterBook);
                        var accepted = store.Commit(candidate, revision, operation);
                        Check.Equal(accepted, store.Commit(candidate, revision, operation));
                        state = store.Load()!;
                        session = AdamSession.Resume(state, definition);
                        Check.Equal(gold, state.Profile.Gold);
                    }
                    else session = session.StartNextRound(definition);

                    Check.Equal(runId, state.Run!.RunId);
                    Check.True(history.SequenceEqual(state.Run.LaterBossHistory!.Take(history.Length)
                        .Select(receipt => JsonSerializer.Serialize(receipt))));

                    session.Advance(180);
                    if (session.BossRules is BossDefinition boss)
                    {
                        session.Advance(2);
                        session.Hit(boss.BossActorId, boss.MaxHealth);
                        if (session.Stage == "boss_reward")
                            Check.True(session.ChooseBossReward(session.Boss!.Reward!.RewardId,
                                session.BossChoices.First(choice => choice.Kind == "gold").Id));
                        while (session.PendingChoices.Count != 0)
                            Check.True(session.Choose(session.PendingChoices[0]));
                        Check.True(session.CompleteBossSettlement());
                    }
                    Check.Equal("between_rounds", session.Stage);
                    store.Commit(state, state.Revision, "finish-" + round);
                    state = store.Load()!;
                    session = AdamSession.Resume(state, definition);
                }

                Check.True(session.ContentComplete && !session.CanEnterLaterBook);
                Check.Equal(71, state.Run!.RoundNumber);
                Check.True(state.Run.LaterBossHistory!.TakeLast(4).Select(receipt => receipt.EncounterRound)
                    .SequenceEqual(new[]{60,63,66,69}));
                Check.True(File.ReadAllBytes(Fixtures + "r70-build-complete-save.json").SequenceEqual(File.ReadAllBytes(path)));
            }
            finally { directory.Delete(true); }
        });
    }
}
