using System;
using BibleSurvivor.Core;

namespace BibleSurvivor.Core.Tests;

internal static class CampaignHundredFiftyTests
{
    internal static void Register(Action<string, Action> test)
    {
        test("R150 preserves the campaign fingerprint through Ezekiel", () =>
            CampaignSixtyTests.MatchFingerprints("r150-build-round-fingerprints.txt", 150));
        test("R150 matches the C7 tuning for R141 through R150", () =>
            CampaignHundredTenTests.MatchTuning(141, 150));
        test("R150 replays R140 with explicit Ezekiel entry and preserves receipts", () =>
            CampaignCheckpointTests.MatchReplay(140, 150, 138, 141, 144, 147, 150));
        test("R150 completion blocks round 151 when loaded", () =>
        {
            Check.Equal(150, CampaignContent.LastInstalledRound);
            Check.True(LaterCampaignBooks.StartsBook(142));
            Check.Throws<ArgumentOutOfRangeException>(() => CampaignContent.Round(151));
            const string path = "tests/BibleSurvivor.Core.Tests/Fixtures/r150-build-complete-save.json";
            var saved = new SaveStore(path).Load()!;
            var session = AdamSession.Resume(saved, CampaignContent.Round(150));
            Check.True(session.ContentComplete && !session.CanEnterLaterBook);
            Check.Equal(151, saved.Run!.RoundNumber);
            Check.True(saved.Run.Boss!.CompletionApplied);
        });
    }
}
