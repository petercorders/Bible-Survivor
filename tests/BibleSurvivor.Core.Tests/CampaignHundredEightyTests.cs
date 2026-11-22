using System;
using System.IO;
using System.Linq;
using BibleSurvivor.Core;
using BibleSurvivor.Sim;

namespace BibleSurvivor.Core.Tests;

internal static class CampaignHundredEightyTests
{
    private const string Fixtures = "tests/BibleSurvivor.Core.Tests/Fixtures/";

    internal static void Register(Action<string, Action> test)
    {
        test("R180 preserves the campaign fingerprint through Malachi", () =>
            CampaignSixtyTests.MatchFingerprints("r180-build-round-fingerprints.txt", 180));
        test("R180 matches the C7 tuning through the final boss", () =>
            CampaignHundredTenTests.MatchTuning(171, 180));
        test("R180 replays R170 and writes a single stable defeat receipt across retries", () =>
            CampaignCheckpointTests.MatchReplay(170, 180, 168, 171, 174, 177, 180));
        test("R180 blocks round 181 and retains the previous clear on a new challenge", () =>
        {
            Check.Equal(180, CampaignContent.LastInstalledRound);
            Check.Throws<ArgumentOutOfRangeException>(() => CampaignContent.Round(181));
            var directory = Directory.CreateTempSubdirectory("adam-r180-new-run-");
            try
            {
                var path = Path.Combine(directory.FullName, "save.json");
                File.Copy(Fixtures + "r180-build-complete-save.json", path);
                var store = new SaveStore(path);
                var saved = store.Load()!;
                var session = AdamSession.Resume(saved, CampaignContent.Round(180));
                Check.True(session.ContentComplete && !session.CanEnterLaterBook);
                Check.Equal(181, saved.Run!.RoundNumber);
                var clear = saved.OldTestamentClear!;
                Check.Equal(saved.Run.RunId, clear.RunId);
                Check.Equal(CampaignCheckpointTests.ClearFixtureTime.ToUnixTimeMilliseconds(), clear.CompletedAtUnixMilliseconds);

                saved.Run = null;
                _ = new AdamSession(saved, CatalogReader.Round("content/genesis/catalog.json", 1), "sling");
                store.Commit(saved, saved.Revision, "new-challenge");
                var restarted = store.Load()!;
                Check.True(restarted.Run!.RunId != clear.RunId);
                Check.Equal(1, restarted.Run.RoundNumber);
                Check.Equal(clear.RunId, restarted.OldTestamentClear!.RunId);
                Check.Equal(clear.CompletedAtUnixMilliseconds, restarted.OldTestamentClear.CompletedAtUnixMilliseconds);
            }
            finally { directory.Delete(true); }
        });
        test("R180 keeps pre-clear saves readable and rejects malformed final receipts", () =>
        {
            var earlierPath = Fixtures + "r170-build-complete-save.json";
            var legacyBytes = File.ReadAllBytes(earlierPath);
            Check.True(new SaveStore(earlierPath).Load()!.OldTestamentClear == null);
            Check.True(legacyBytes.SequenceEqual(File.ReadAllBytes(earlierPath)));
            var final = new SaveStore(Fixtures + "r180-build-complete-save.json").Load()!;
            var clear = final.OldTestamentClear!;
            final.OldTestamentClear = null;
            Check.Throws<InvalidDataException>(final.Validate);
            final.OldTestamentClear = clear;
            clear.RunId = "wrong-challenge";
            Check.Throws<InvalidDataException>(final.Validate);
            clear.RunId = final.Run!.RunId;
            clear.CompletedAtUnixMilliseconds = 0;
            Check.Throws<InvalidDataException>(final.Validate);
        });
    }
}
