using System;
using System.IO;
using System.Text.Json.Nodes;
using BibleSurvivor.Core;

namespace BibleSurvivor.Core.Tests;

internal static class CampaignHundredTests
{
    internal static void Register(Action<string,Action> test)
    {
        test("R100 R01-R100 fingerprints match the complete campaign", () =>
            CampaignSixtyTests.MatchFingerprints("r100-build-round-fingerprints.txt",100));

        test("R100 Job follows the completed historical books", () =>
        {
            Check.Equal("ezra",LaterCampaignBooks.Find(91)!.Id);
            Check.Equal("nehemiah",LaterCampaignBooks.Find(94)!.Id);
            Check.Equal("esther",LaterCampaignBooks.Find(99)!.Id);
            Check.Equal("job",LaterCampaignBooks.Find(100)!.Id);
            Check.True(LaterCampaignBooks.StartsBook(100));
            Check.Equal(102,LaterCampaignBooks.Find(100)!.LastRound);
            Check.Equal("accusation_cloud",CampaignContent.Round(92).Waves[0].Groups[0].MonsterId);
            Check.Equal("accusing_shadow",CampaignContent.Round(100).Waves[0].Groups[0].MonsterId);
            Check.Equal(BossActions.Dash,BossDefinition.ForRound(93).Actions);
            Check.Equal(BossActions.Summon,BossDefinition.ForRound(96).Actions);
            Check.Equal(BossActions.Dash,BossDefinition.ForRound(99).Actions);
            Check.Throws<ArgumentException>(() => BossDefinition.ForRound(100));
            Check.Equal("job_02",CampaignContent.Round(101).Id);
        });

        test("R100 completion replays from the R90 build and preserves the last historical receipts",
            () => CampaignCheckpointTests.MatchReplay(90,100,90,93,96,99));

        test("R100 checkpoint loads as completed; an invalid active R101 save is protected",
            () =>
        {
            var directory=Directory.CreateTempSubdirectory("adam-r100-end-");
            try
            {
                var path=Path.Combine(directory.FullName,"save.json");
                File.Copy("tests/BibleSurvivor.Core.Tests/Fixtures/r100-build-complete-save.json",path);
                var store=new SaveStore(path);
                var saved=store.Load()!;
                var session=AdamSession.Resume(saved,CampaignContent.Round(100));
                Check.True(!session.ContentComplete && !session.CanEnterLaterBook);
                Check.Equal(101,saved.Run!.RoundNumber);
                Check.Equal("between_rounds",saved.Run.Stage);
                var future=JsonNode.Parse(File.ReadAllText(path))!;
                future["Run"]!["Stage"]="normal";
                var serialized=future.ToJsonString();
                File.WriteAllText(path,serialized);
                Check.Throws<UnsupportedSaveVersionException>(()=>store.Load());
                Check.Throws<UnsupportedSaveVersionException>(()=>store.Commit(saved,saved.Revision,"reject-active-r101"));
                Check.Equal(serialized,File.ReadAllText(path));
            }
            finally { directory.Delete(true); }
        });
    }
}
