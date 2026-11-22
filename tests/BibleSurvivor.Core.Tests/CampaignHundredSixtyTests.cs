using System;
using BibleSurvivor.Core;

namespace BibleSurvivor.Core.Tests;

internal static class CampaignHundredSixtyTests
{
    internal static void Register(Action<string, Action> test)
    {
        test("R160 preserves the campaign fingerprint through the Minor Prophets opening", () =>
            CampaignSixtyTests.MatchFingerprints("r160-build-round-fingerprints.txt", 160));
        test("R160 matches the C7 tuning through the first reduced wave health", () =>
            CampaignHundredTenTests.MatchTuning(151, 160));
        test("R160 replays R150 with explicit Daniel and Minor Prophets entries and preserves receipts", () =>
            CampaignCheckpointTests.MatchReplay(150, 160, 150, 153, 156, 159));
        test("R160 registers all 21 Minor Prophets rounds and blocks uninstalled 161", () =>
        {
            Check.Equal(160, CampaignContent.LastInstalledRound);
            Check.True(LaterCampaignBooks.StartsBook(151));
            Check.True(LaterCampaignBooks.StartsBook(160));
            var book = LaterCampaignBooks.Find(160)!;
            Check.Equal("minor_prophets", book.Id);
            Check.Equal(21, book.LastRound - book.FirstRound + 1);
            Check.Throws<ArgumentOutOfRangeException>(() => CampaignContent.Round(161));
            const string path = "tests/BibleSurvivor.Core.Tests/Fixtures/r160-build-complete-save.json";
            var saved = new SaveStore(path).Load()!;
            var session = AdamSession.Resume(saved, CampaignContent.Round(160));
            Check.True(session.ContentComplete && !session.CanEnterLaterBook);
            Check.Equal(161, saved.Run!.RoundNumber);
        });
    }
}
