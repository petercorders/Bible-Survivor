using System;
using BibleSurvivor.Core;

namespace BibleSurvivor.Core.Tests;

internal static class CampaignHundredTwentyTests
{
    internal static void Register(Action<string, Action> test)
    {
        test("R120 preserves the campaign fingerprint through Song of Songs", () =>
            CampaignSixtyTests.MatchFingerprints("r120-build-round-fingerprints.txt", 120));
        test("R120 matches the C7 tuning for R111 through R120", () =>
            CampaignHundredTenTests.MatchTuning(111, 120));
        test("R120 replays R110 with explicit Wisdom book entries and preserves receipts", () =>
            CampaignCheckpointTests.MatchReplay(110, 120, 108, 111, 114, 117, 120));
        test("R120 completion blocks round 121 when loaded", () =>
        {
            Check.Equal(120, CampaignContent.LastInstalledRound);
            Check.True(LaterCampaignBooks.StartsBook(112));
            Check.True(LaterCampaignBooks.StartsBook(115));
            Check.True(LaterCampaignBooks.StartsBook(118));
            Check.Throws<ArgumentOutOfRangeException>(() => CampaignContent.Round(121));
            const string path = "tests/BibleSurvivor.Core.Tests/Fixtures/r120-build-complete-save.json";
            var saved = new SaveStore(path).Load()!;
            var session = AdamSession.Resume(saved, CampaignContent.Round(120));
            Check.True(session.ContentComplete && !session.CanEnterLaterBook);
            Check.Equal(121, saved.Run!.RoundNumber);
            Check.True(saved.Run.Boss!.CompletionApplied);
        });
    }
}
