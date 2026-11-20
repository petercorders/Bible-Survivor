using System;
using BibleSurvivor.Core;

namespace BibleSurvivor.Core.Tests;

internal static class CampaignNinetyTests
{
    internal static void Register(Action<string,Action> test)
    {
        test("R90 R01-R90 fingerprints match the R90 build",
            () => CampaignSixtyTests.MatchFingerprints("r90-build-round-fingerprints.txt",90));

        test("R90 installed Chronicles boundaries, motifs, and boss compositions", () =>
        {
            Check.Equal(90,CampaignContent.LastInstalledRound);
            Check.Throws<ArgumentOutOfRangeException>(() => CampaignContent.Round(91));
            Check.Equal("second_kings",LaterCampaignBooks.Find(81)!.Id);
            Check.Equal("first_chronicles",LaterCampaignBooks.Find(82)!.Id);
            Check.Equal("second_chronicles",LaterCampaignBooks.Find(85)!.Id);
            Check.Equal("scattered_scroll_fragments",CampaignContent.Round(82).Waves[0].Groups[0].MonsterId);
            Check.Equal("endless_procession",CampaignContent.Round(86).Waves[0].Groups[0].MonsterId);
            Check.Equal("tower_pride_shadow",CampaignContent.Round(88).Waves[0].Groups[0].MonsterId);
            Check.Equal(BossActions.Dash|BossActions.Chains,BossDefinition.ForRound(81).Actions);
            Check.Equal(BossActions.Summon,BossDefinition.ForRound(84).Actions);
            Check.Equal(BossActions.Slam,BossDefinition.ForRound(87).Actions);
            Check.Equal(BossActions.Chains,BossDefinition.ForRound(90).Actions);
        });

        test("R90 completion replays from the R80 build and stores the final boss receipt",
            () => CampaignCheckpointTests.MatchReplay(80,90,81,84,87,90));
    }
}
