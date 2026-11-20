using System;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text.Json;
using System.Text.Json.Nodes;
using BibleSurvivor.Core;
using BibleSurvivor.Sim;

namespace BibleSurvivor.Core.Tests;

internal static class CampaignSixtyTests
{
    private const string Fixtures="tests/BibleSurvivor.Core.Tests/Fixtures/";

    public static void Register(Action<string,Action> test)
    {
        test("R60 R01-R60 fingerprints match the R60 build",() =>MatchFingerprints("r60-build-round-fingerprints.txt",60));

        test("R60 book cadence, waves, bosses, and installed endpoint",() =>
        {
            foreach(var round in Enumerable.Range(51,10))
            {
                var definition=CampaignContent.Round(round);
                var groups=definition.Waves.SelectMany(w=>w.Groups).ToArray();
                var (prefix,id,contact,speed)=round switch
                {
                    < RuthContent.GlobalRoundNumber=>("jd","judges_"+(round-42).ToString("D2",CultureInfo.InvariantCulture),18,.69),
                    < FirstSamuelContent.GlobalRoundNumber=>("rt","ruth_"+(round-51).ToString("D2",CultureInfo.InvariantCulture),19,.71),
                    _=>("sm","first_samuel_"+(round-54).ToString("D2",CultureInfo.InvariantCulture),20,.73)
                };
                Check.Equal(id,definition.Id);
                Check.Equal(1560+(round-22)*60,groups.Sum(x=>x.Count));
                Check.Equal(7200+(round-22)*200,definition.RawExperience);
                Check.Equal(420+(round-22)*20,definition.CompletionGold);
                Check.True(groups.Select(x=>x.MonsterId).Distinct().OrderBy(x=>x,StringComparer.Ordinal)
                    .SequenceEqual(new[]{prefix+"1",prefix+"2",prefix+"3"}));
                Check.True(groups.All(x=>CampaignContent.SpeedsFor(round).ContainsKey(x.MonsterId)));
                Check.Equal(contact,groups.First(x=>x.MonsterId==prefix+"1").ContactDamage);
                Check.Equal(speed,CampaignContent.SpeedsFor(round)[prefix+"1"]);
                Check.Equal(170d,definition.Waves.Last().End);
                if(round%3==0)
                {
                    var boss=BossDefinition.ForRound(round);
                    Check.True(boss.UsesChainProjectiles);
                    Check.Equal(groups.Sum(x=>x.Count)+1,boss.BossActorId);
                    Check.Equal(prefix+"1",boss.SupportMonster);
                    Check.Equal(groups.First(x=>x.MonsterId==prefix+"1").Health,boss.SupportHitPoints);
                    Check.Equal(contact,boss.SupportContact);
                    Check.Equal(speed,boss.SupportRatio);
                    Check.True(boss.EncounterKey("run").StartsWith("run:"+definition.Id+":",StringComparison.Ordinal));
                }
                else Check.Throws<ArgumentException>(()=>BossDefinition.ForRound(round));
            }
            Check.True(new[]{51,54,57,60}.Select(r=>BossDefinition.ForRound(r).Id)
                .SequenceEqual(new[]{"toppling_pillar","chaff_whirl","swarm_monarch","valley_bronze_brute"}));
            Check.Equal(60,CampaignContent.LastInstalledRound);
            Check.Equal(FirstSamuelContent.LastRound,CampaignContent.LastInstalledRound);
            // Judges R43-R50 keep their R50-build IDs once Judges ends at its own book endpoint.
            Check.Equal("judges_08",CampaignContent.Round(50).Id);
            Check.Throws<ArgumentOutOfRangeException>(()=>CampaignContent.Round(61));
            Check.Throws<ArgumentOutOfRangeException>(()=>CampaignContent.SpeedsFor(61));
            Check.Throws<ArgumentOutOfRangeException>(()=>JudgesContent.Round(52));
            Check.Throws<ArgumentOutOfRangeException>(()=>RuthContent.Round(51));
            Check.Throws<ArgumentOutOfRangeException>(()=>RuthContent.Round(55));
            Check.Throws<ArgumentOutOfRangeException>(()=>FirstSamuelContent.Round(54));
            Check.Throws<ArgumentOutOfRangeException>(()=>FirstSamuelContent.Round(61));
            Check.Throws<ArgumentException>(()=>BossDefinition.ForRound(61));
            Check.Throws<ArgumentException>(()=>BossDefinition.ForRound(63));
        });

        test("R60 R50 build completion save continues Judges without rewriting history",() =>
        {
            var dir=Path.Combine(Path.GetTempPath(),"bible-r60-entry-"+Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(dir);
            try
            {
                var path=Path.Combine(dir,"save.json");
                File.Copy(Fixtures+"r50-build-complete-save.json",path);
                var raw=File.ReadAllBytes(path);
                var store=new SaveStore(path);
                var state=store.Load()!;
                Check.True(raw.SequenceEqual(File.ReadAllBytes(path)));
                Check.Equal(51,state.Run!.RoundNumber);
                Check.Equal("between_rounds",state.Run.Stage);
                var session=AdamSession.Resume(state,CampaignContent.Round(50));
                Check.True(!session.ContentComplete&&!session.JudgesComplete&&!session.CanEnterRuth&&!session.JoshuaComplete);
                Check.Throws<InvalidOperationException>(()=>session.PrepareRuth(CampaignContent.Round(52)));
                Check.Throws<InvalidOperationException>(()=>session.PrepareFirstSamuel(CampaignContent.Round(55)));
                Check.Throws<InvalidOperationException>(()=>session.PrepareJudges(CampaignContent.Round(43)));
                var gold=state.Profile.Gold;
                var olderHistory=OlderHistory(state);
                var later=state.Run.LaterBossHistory!.Select(Json).ToArray();
                Check.True(state.Run.LaterBossHistory!.Select(r=>r.EncounterRound).SequenceEqual(new[]{21,24,27,30,33,36,39,42,45,48}));

                session=session.StartNextRound(CampaignContent.Round(51));
                var revision=store.Commit(state,state.Revision,"r51");
                state=store.Load()!;
                session=AdamSession.Resume(state,CampaignContent.Round(51));
                Check.Equal(gold,state.Profile.Gold);
                Check.Equal(51,state.Run!.RoundNumber);
                Check.Equal("normal",state.Run.Stage);
                Check.Equal(CampaignContent.Round(51).Fingerprint,state.Run.RoundFingerprint);
                Check.Equal(olderHistory,OlderHistory(state));
                Check.True(later.SequenceEqual(state.Run.LaterBossHistory!.Select(Json)));
                Check.True(revision>0);
            }
            finally { Directory.Delete(dir,true); }
        });

        test("R60 build complete save loads unchanged as the final checkpoint",() =>
        {
            var dir=Path.Combine(Path.GetTempPath(),"bible-r60-final-"+Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(dir);
            try
            {
                // Byte copy of the save the R60 QA app wrote after completing R60.
                var path=Path.Combine(dir,"save.json");
                File.Copy(Fixtures+"r60-build-complete-save.json",path);
                var raw=File.ReadAllBytes(path);
                var store=new SaveStore(path);
                var state=store.Load()!;
                Check.True(raw.SequenceEqual(File.ReadAllBytes(path)));
                Check.Equal(61,state.Run!.RoundNumber);
                Check.Equal("between_rounds",state.Run.Stage);
                Check.Equal(0,state.Run.PendingLevels);
                Check.True(state.Run.Boss!.EncounterRound==60&&state.Run.Boss.CompletionApplied);
                Check.True(state.Run.LaterBossHistory!.Select(r=>r.EncounterRound).SequenceEqual(new[]{21,24,27,30,33,36,39,42,45,48,51,54,57}));
                var session=AdamSession.Resume(state,CampaignContent.Round(60));
                Check.True(session.ContentComplete);
                Check.True(!session.JudgesComplete&&!session.RuthComplete&&!session.JoshuaComplete);
                Check.True(!session.CanEnterJudges&&!session.CanEnterRuth&&!session.CanEnterFirstSamuel);
                Check.Throws<InvalidOperationException>(()=>session.PrepareRuth(CampaignContent.Round(52)));
                Check.Throws<InvalidOperationException>(()=>session.PrepareFirstSamuel(CampaignContent.Round(55)));
                Check.Throws<ArgumentOutOfRangeException>(()=>CampaignContent.Round(61));
                var gold=state.Profile.Gold;
                var history=state.Run.LaterBossHistory!.Select(Json).ToArray();
                var boss=Json(state.Run.Boss);
                Check.True(!session.CompleteNormalSettlement()&&!session.CompleteBossSettlement());
                Check.Equal(gold,state.Profile.Gold);

                // Saving the checkpoint again keeps it the same terminal state.
                var expected=state.Revision;
                var revision=store.Commit(state,expected,"r60-resave");
                Check.Equal(revision,store.Commit(state,expected,"r60-resave"));
                state=store.Load()!;
                session=AdamSession.Resume(state,CampaignContent.Round(60));
                Check.True(session.ContentComplete&&!session.CanEnterFirstSamuel);
                Check.Equal(61,state.Run!.RoundNumber);
                Check.Equal(gold,state.Profile.Gold);
                Check.Equal(boss,Json(state.Run.Boss));
                Check.True(history.SequenceEqual(state.Run.LaterBossHistory!.Select(Json)));
            }
            finally { Directory.Delete(dir,true); }
        });

        test("R60 progress, atomic Ruth and First Samuel entries, ordered boss receipts, and R61 or future save protection",() =>
        {
            var dir=Path.Combine(Path.GetTempPath(),"bible-r60-"+Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(dir);
            try
            {
                var path=Path.Combine(dir,"save.json");
                File.Copy(Fixtures+"r50-build-complete-save.json",path);
                var store=new SaveStore(path);
                var state=store.Load()!;
                var session=AdamSession.Resume(state,CampaignContent.Round(50));
                var revision=state.Revision;
                var runId=state.Run!.RunId;
                var olderHistory=OlderHistory(state);
                var receipts=state.Run.LaterBossHistory!.Select(Json).ToList();
                for(var round=51;round<=60;round++)
                {
                    var definition=CampaignContent.Round(round);
                    if(round is RuthContent.GlobalRoundNumber or FirstSamuelContent.GlobalRoundNumber)
                    {
                        var ruth=round==RuthContent.GlobalRoundNumber;
                        var raw=File.ReadAllBytes(path);
                        var gold=state.Profile.Gold;
                        Check.True(ruth?session.CanEnterRuth&&session.JudgesComplete&&!session.CanEnterFirstSamuel:
                            session.CanEnterFirstSamuel&&session.RuthComplete&&!session.CanEnterRuth);
                        Check.True(!session.ContentComplete&&!session.CanEnterJudges);
                        Check.Throws<InvalidOperationException>(()=>session.StartNextRound(definition));
                        // Growth choices still pending: the previous book is complete but its entry stays closed.
                        state.Run!.PendingLevels=1;
                        Check.True(ruth?session.JudgesComplete&&!session.CanEnterRuth:session.RuthComplete&&!session.CanEnterFirstSamuel);
                        Check.Throws<InvalidOperationException>(()=>_=ruth?session.PrepareRuth(definition):session.PrepareFirstSamuel(definition));
                        state.Run.PendingLevels=0;
                        Check.Throws<ArgumentException>(()=>_=ruth?session.PrepareRuth(CampaignContent.Round(53)):session.PrepareFirstSamuel(CampaignContent.Round(56)));
                        Check.Throws<InvalidOperationException>(()=>_=ruth?session.PrepareFirstSamuel(CampaignContent.Round(55)):session.PrepareRuth(CampaignContent.Round(52)));
                        var candidate=ruth?session.PrepareRuth(definition):session.PrepareFirstSamuel(definition);
                        var unavailable=new SaveStore(path,()=>throw new IOException("before replace"));
                        Check.Throws<IOException>(()=>unavailable.Commit(candidate,revision,"book-"+round));
                        Check.True(raw.SequenceEqual(File.ReadAllBytes(path))&&(ruth?session.JudgesComplete:session.RuthComplete));
                        var accepted=store.Commit(candidate,revision,"book-"+round);
                        Check.Equal(accepted,store.Commit(candidate,revision,"book-"+round));
                        revision=accepted;
                        state=store.Load()!;
                        session=AdamSession.Resume(state,definition);
                        Check.Equal(gold,state.Profile.Gold);
                        Check.True(!session.JudgesComplete&&!session.RuthComplete&&!session.CanEnterRuth&&!session.CanEnterFirstSamuel);
                    }
                    else session=session.StartNextRound(definition);
                    Check.True(receipts.SequenceEqual(state.Run!.LaterBossHistory!.Select(Json)));

                    var before=state.Profile.Gold;
                    session.Advance(180);
                    if(session.BossRules is BossDefinition boss)
                    {
                        var world=new BattleWorld(session,CampaignContent.SpeedsFor(round),7);
                        world.Advance(6,Vector2.Zero);
                        Check.True(session.Boss!.Telegraphing);
                        state.Run!.World=world.Capture();
                        revision=store.Commit(state,revision,"warning-"+round);
                        state=store.Load()!;
                        session=AdamSession.Resume(state,definition);
                        world=BattleWorld.Restore(session,CampaignContent.SpeedsFor(round),state.Run!.World!);
                        world.Advance(1,Vector2.Zero);
                        Check.Equal(3,world.HostileProjectiles.Count);
                        session.Hit(boss.BossActorId,boss.MaxHealth);
                        Check.True(session.CompleteBossSettlement());
                        before+=boss.KillGold+100;
                        if(round<60)receipts.Add(Json(state.Run!.Boss));
                    }
                    Check.Equal(before+definition.CompletionGold,state.Profile.Gold);
                    Check.Equal("between_rounds",session.Stage);
                    Check.True(!session.CompleteNormalSettlement()&&!session.CompleteBossSettlement());
                    revision=store.Commit(state,revision,"finish-"+round);
                    state=store.Load()!;
                    session=AdamSession.Resume(state,definition);
                    Check.Equal(runId,state.Run!.RunId);
                    Check.Equal(round==60,session.ContentComplete);
                    Check.Equal(round==51,session.JudgesComplete);
                    Check.Equal(round==54,session.RuthComplete);
                    Check.True(!session.JoshuaComplete&&!session.CanEnterJudges);
                    Check.Equal(olderHistory,OlderHistory(state));
                }
                Check.Equal(61,state.Run!.RoundNumber);
                // R60 is a boss round: its receipt stays in Run.Boss because no later round starts.
                Check.True(state.Run.Boss!.EncounterRound==60&&state.Run.Boss.CompletionApplied);
                Check.True(state.Run.LaterBossHistory!.Select(r=>r.EncounterRound).SequenceEqual(new[]{21,24,27,30,33,36,39,42,45,48,51,54,57}));
                Check.True(receipts.SequenceEqual(state.Run.LaterBossHistory!.Select(Json)));
                Check.True(!session.CanEnterRuth&&!session.CanEnterFirstSamuel&&!session.RuthComplete);
                // R61 stays closed because CampaignContent.Round(61) throws (endpoint test) and the saves below are rejected as future saves.

                var checkpoint=File.ReadAllText(path);
                var backup=File.ReadAllBytes(path+".bak");
                foreach(var change in new Action<JsonNode>[]
                {
                    n=>{n["Run"]!["RoundNumber"]=61;n["Run"]!["Stage"]="normal";},
                    n=>n["Run"]!["RoundNumber"]=62,
                    n=>n["Run"]!["LaterBossHistory"]![12]!["FutureExtension"]=true,
                    n=>n["Run"]!["LaterBossHistory"]![12]!["RoundNumber"]=63,
                    n=>n["Run"]!["LaterBossHistory"]![11]!["Reward"]!["PolicyId"]="r57-production-v1",
                    n=>n["Run"]!["LaterBossHistory"]![10]!["Reward"]!["PolicyId"]="future-reward",
                    n=>n["Run"]!["Boss"]!["Reward"]!["PolicyId"]="r57-production-v1"
                })
                {
                    var document=JsonNode.Parse(checkpoint)!;
                    change(document);
                    var future=document.ToJsonString();
                    File.WriteAllText(path,future);
                    Check.Throws<UnsupportedSaveVersionException>(()=>store.Load());
                    Check.Throws<UnsupportedSaveVersionException>(()=>store.Commit(state,revision,"replace"));
                    Check.Equal(future,File.ReadAllText(path));
                    Check.True(backup.SequenceEqual(File.ReadAllBytes(path+".bak")));
                }
            }
            finally { Directory.Delete(dir,true); }
        });
    }

    private static void MatchFingerprints(string fixture,int rounds)
    {
        var lines=File.ReadAllLines(Fixtures+fixture);
        Check.Equal(rounds,lines.Length);
        foreach(var line in lines)
        {
            var parts=line.Split(' ');
            var round=int.Parse(parts[0],CultureInfo.InvariantCulture);
            var definition=round<=9?CatalogReader.Round("content/genesis/catalog.json",round):
                round<=15?CatalogReader.Exodus("content/exodus/catalog.json",round):CampaignContent.Round(round);
            Check.Equal(parts[1],definition.Id);
            Check.Equal(parts[2],definition.Fingerprint);
        }
    }

    private static string Json(object? value)=>JsonSerializer.Serialize(value);

    private static string OlderHistory(GameSnapshot state)=>Json(new{state.Run!.CompletedBoss,state.Run.CompletedSecondBoss,
        state.Run.CompletedThirdBoss,state.Run.CompletedFourthBoss,state.Run.CompletedFifthBoss,state.Run.CompletedSixthBoss});
}
