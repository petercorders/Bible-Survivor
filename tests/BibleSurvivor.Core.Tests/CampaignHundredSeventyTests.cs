using System;
using BibleSurvivor.Core;

namespace BibleSurvivor.Core.Tests;

internal static class CampaignHundredSeventyTests
{
    internal static void Register(Action<string, Action> test)
    {
        test("R170 preserves the campaign fingerprint through Micah", () =>
            CampaignSixtyTests.MatchFingerprints("r170-build-round-fingerprints.txt", 170));
        test("R170 matches the C7 tuning for R161 through R170", () =>
            CampaignHundredTenTests.MatchTuning(161, 170));
        test("R170 replays R160 and preserves receipts", () =>
            CampaignCheckpointTests.MatchReplay(160, 170, 159, 162, 165, 168));
        test("R170 stays in the same book and continues to Nahum", () =>
        {
            var book = LaterCampaignBooks.Find(160)!;
            for (var round = 161; round <= 170; round++)
            {
                Check.True(ReferenceEquals(book, LaterCampaignBooks.Find(round)));
                Check.True(!LaterCampaignBooks.StartsBook(round));
            }
            Check.Equal("minor_prophets_12", CampaignContent.Round(171).Id);
            const string path = "tests/BibleSurvivor.Core.Tests/Fixtures/r170-build-complete-save.json";
            var saved = new SaveStore(path).Load()!;
            var session = AdamSession.Resume(saved, CampaignContent.Round(170));
            Check.True(!session.ContentComplete && !session.CanEnterLaterBook);
            Check.Equal(171, saved.Run!.RoundNumber);
        });
    }
}
