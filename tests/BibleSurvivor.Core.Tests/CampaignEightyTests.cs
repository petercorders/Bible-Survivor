using System;
using System.Linq;
using BibleSurvivor.Core;

namespace BibleSurvivor.Core.Tests;

internal static class CampaignEightyTests
{
    internal static void Register(Action<string,Action> test)
    {
        test("R80 R01-R80 fingerprints match the R80 build",
            () => CampaignSixtyTests.MatchFingerprints("r80-build-round-fingerprints.txt",80));

        test("R80 installed book boundary, new motifs, and boss compositions", () =>
        {
            Check.Equal(80,CampaignContent.LastInstalledRound);
            Check.Throws<ArgumentOutOfRangeException>(() => CampaignContent.Round(81));
            Check.Equal("first_kings",LaterCampaignBooks.Find(72)!.Id);
            Check.Equal("second_kings",LaterCampaignBooks.Find(73)!.Id);
            Check.Equal("cave_hunter",CampaignContent.Round(71).Waves[0].Groups[0].MonsterId);
            Check.Equal("starving_siege_beast",CampaignContent.Round(76).Waves[0].Groups[0].MonsterId);
            Check.Equal(BossActions.Chains|BossActions.Summon,BossDefinition.ForRound(72).Actions);
            Check.Equal(BossActions.Summon,BossDefinition.ForRound(75).Actions);
            var bull=BossDefinition.ForRound(78);
            Check.Equal(BossActions.Dash,bull.Actions);
            Check.True(bull.FingerprintContract.Contains("fixed-direction:relative-swept:contact-only:dash0.6:speed1.3"));
            Check.True(CampaignContent.Round(73).Waves.SelectMany(wave=>wave.Groups)
                .All(group=>CampaignContent.SpeedsFor(73).ContainsKey(group.MonsterId)));
        });

        test("R80 completion replays from the R70 build through both books",
            () => CampaignCheckpointTests.MatchReplay(70,80,69,72,75,78));
    }
}
