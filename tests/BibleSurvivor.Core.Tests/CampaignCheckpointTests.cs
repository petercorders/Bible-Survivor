using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using BibleSurvivor.Core;

namespace BibleSurvivor.Core.Tests;

internal static class CampaignCheckpointTests
{
    private const string Fixtures = "tests/BibleSurvivor.Core.Tests/Fixtures/";

    internal static void MatchReplay(int startRound,int endRound,params int[] expectedFinalBosses)
    {
        var directory = Directory.CreateTempSubdirectory("adam-campaign-test-");
        try
        {
            var path = Path.Combine(directory.FullName, "save.json");
            var original = File.ReadAllBytes(Fixtures + $"r{startRound}-build-complete-save.json");
            File.WriteAllBytes(path, original);
            var store = new SaveStore(path);
            var state = store.Load()!;
            Check.True(original.SequenceEqual(File.ReadAllBytes(path)));
            var session = AdamSession.Resume(state, CampaignContent.Round(startRound));
            var runId = state.Run!.RunId;
            var history = state.Run.LaterBossHistory!.Select(receipt => JsonSerializer.Serialize(receipt)).ToList();
            if (state.Run.Boss is BossEncounterState endpointBoss) history.Add(JsonSerializer.Serialize(endpointBoss));

            for (var round = startRound + 1; round <= endRound; round++)
            {
                var definition = CampaignContent.Round(round);
                if (LaterCampaignBooks.StartsBook(round))
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
                Check.True(history.SequenceEqual(state.Run.LaterBossHistory!.Take(history.Count)
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

            Check.Equal(endRound == CampaignContent.LastInstalledRound, session.ContentComplete);
            Check.Equal(endRound + 1, state.Run!.RoundNumber);
            Check.True(state.Run.LaterBossHistory!.TakeLast(expectedFinalBosses.Length).Select(receipt => receipt.EncounterRound)
                .SequenceEqual(expectedFinalBosses));
            Check.True(File.ReadAllBytes(Fixtures + $"r{endRound}-build-complete-save.json").SequenceEqual(File.ReadAllBytes(path)));
        }
        finally { directory.Delete(true); }
    }
}
