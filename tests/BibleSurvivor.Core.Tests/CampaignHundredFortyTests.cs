using System;
using BibleSurvivor.Core;

namespace BibleSurvivor.Core.Tests;

internal static class CampaignHundredFortyTests
{
    internal static void Register(Action<string, Action> test)
    {
        test("R140 preserves the campaign fingerprint through the Lamentations opening", () =>
            CampaignSixtyTests.MatchFingerprints("r140-build-round-fingerprints.txt", 140));
        test("R140 matches the C7 tuning for R131 through R140", () =>
            CampaignHundredTenTests.MatchTuning(131, 140));
        test("R140 replays R130 with explicit Lamentations entry and preserves receipts", () =>
            CampaignCheckpointTests.MatchReplay(130, 140, 129, 132, 135, 138));
        test("R140 completion blocks round 141 when loaded", () =>
        {
            Check.Equal(140, CampaignContent.LastInstalledRound);
            Check.True(LaterCampaignBooks.StartsBook(139));
            Check.Throws<ArgumentOutOfRangeException>(() => CampaignContent.Round(141));
            const string path = "tests/BibleSurvivor.Core.Tests/Fixtures/r140-build-complete-save.json";
            var saved = new SaveStore(path).Load()!;
            var session = AdamSession.Resume(saved, CampaignContent.Round(140));
            Check.True(session.ContentComplete && !session.CanEnterLaterBook);
            Check.Equal(141, saved.Run!.RoundNumber);
        });
    }
}
