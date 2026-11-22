using System;
using BibleSurvivor.Core;

namespace BibleSurvivor.Core.Tests;

internal static class CampaignHundredThirtyTests
{
    internal static void Register(Action<string, Action> test)
    {
        test("R130 preserves the campaign fingerprint through the Jeremiah opening", () =>
            CampaignSixtyTests.MatchFingerprints("r130-build-round-fingerprints.txt", 130));
        test("R130 matches the C7 tuning for R121 through R130", () =>
            CampaignHundredTenTests.MatchTuning(121, 130));
        test("R130 replays R120 with explicit Isaiah and Jeremiah entries and preserves receipts", () =>
            CampaignCheckpointTests.MatchReplay(120, 130, 120, 123, 126, 129));
        test("R130 completion loads and continues Jeremiah", () =>
        {
            Check.True(LaterCampaignBooks.StartsBook(121));
            Check.True(LaterCampaignBooks.StartsBook(130));
            Check.Equal("jeremiah_02", CampaignContent.Round(131).Id);
            const string path = "tests/BibleSurvivor.Core.Tests/Fixtures/r130-build-complete-save.json";
            var saved = new SaveStore(path).Load()!;
            var session = AdamSession.Resume(saved, CampaignContent.Round(130));
            Check.True(!session.ContentComplete && !session.CanEnterLaterBook);
            Check.Equal(131, saved.Run!.RoundNumber);
        });
    }
}
