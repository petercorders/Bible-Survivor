using System;
using System.Linq;
using BibleSurvivor.Core;

namespace BibleSurvivor.Core.Tests;

internal static class CampaignSeventyTests
{
    internal static void Register(Action<string,Action> test)
    {
        test("R70 R01-R70 fingerprints match the R70 build", () =>
            CampaignSixtyTests.MatchFingerprints("r70-build-round-fingerprints.txt", 70));

        test("R70 installed books, C0 curve, and composed bosses", () =>
        {
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

        test("R70 completion save reproduces from the previous build without rewriting the R60 receipts",
            () => CampaignCheckpointTests.MatchReplay(60,70,60,63,66,69));
    }
}
