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

internal static class CampaignFiftyTests
{
    private const string Fixtures="tests/BibleSurvivor.Core.Tests/Fixtures/";

    public static void Register(Action<string,Action> test)
    {
        test("R50 R01-R40 fingerprints match the R40 build",() =>
        {
            var lines=File.ReadAllLines(Fixtures+"r40-build-round-fingerprints.txt");
            Check.Equal(40,lines.Length);
            foreach(var line in lines)
            {
                var parts=line.Split(' ');
                var round=int.Parse(parts[0],CultureInfo.InvariantCulture);
                var definition=round<=9?CatalogReader.Round("content/genesis/catalog.json",round):
                    round<=15?CatalogReader.Exodus("content/exodus/catalog.json",round):CampaignContent.Round(round);
                Check.Equal(parts[1],definition.Id);
                Check.Equal(parts[2],definition.Fingerprint);
            }
        });

        test("R50 book cadence, waves, bosses, and installed endpoint",() =>
        {
            foreach(var round in Enumerable.Range(41,10))
            {
                var definition=CampaignContent.Round(round);
                var groups=definition.Waves.SelectMany(w=>w.Groups).ToArray();
                var joshua=round<JudgesContent.GlobalRoundNumber;
                var prefix=joshua?"js":"jd";
                Check.Equal(joshua?"joshua_"+(round-36).ToString("D2",CultureInfo.InvariantCulture):
                    "judges_"+(round-42).ToString("D2",CultureInfo.InvariantCulture),definition.Id);
                Check.Equal(1560+(round-22)*60,groups.Sum(x=>x.Count));
                Check.Equal(7200+(round-22)*200,definition.RawExperience);
                Check.Equal(420+(round-22)*20,definition.CompletionGold);
                Check.True(groups.Select(x=>x.MonsterId).Distinct().OrderBy(x=>x,StringComparer.Ordinal)
                    .SequenceEqual(new[]{prefix+"1",prefix+"2",prefix+"3"}));
                Check.True(groups.All(x=>CampaignContent.SpeedsFor(round).ContainsKey(x.MonsterId)));
                Check.Equal(joshua?17:18,groups.First(x=>x.MonsterId==prefix+"1").ContactDamage);
                Check.Equal(170d,definition.Waves.Last().End);
                if(round%3==0)
                {
                    var boss=BossDefinition.ForRound(round);
                    Check.True(boss.UsesChainProjectiles);
                    Check.Equal(groups.Sum(x=>x.Count)+1,boss.BossActorId);
                    Check.Equal(prefix+"1",boss.SupportMonster);
                    Check.Equal(groups.First(x=>x.MonsterId==prefix+"1").Health,boss.SupportHitPoints);
                    Check.True(boss.EncounterKey("run").StartsWith("run:"+definition.Id+":",StringComparison.Ordinal));
                }
                else Check.Throws<ArgumentException>(()=>BossDefinition.ForRound(round));
            }
            Check.True(new[]{42,45,48}.Select(r=>BossDefinition.ForRound(r).Id)
                .SequenceEqual(new[]{"shifting_boundary","torrent_serpent","ford_gatekeeper"}));
            Check.Equal(.69,JudgesContent.SpeedsFor(43)["jd1"]);
            Check.Equal(.67,JoshuaContent.SpeedsFor(42)["js1"]);
            Check.Throws<ArgumentOutOfRangeException>(()=>CampaignContent.Round(51));
            Check.Throws<ArgumentOutOfRangeException>(()=>CampaignContent.SpeedsFor(51));
            Check.Throws<ArgumentOutOfRangeException>(()=>JoshuaContent.Round(43));
            Check.Throws<ArgumentOutOfRangeException>(()=>JudgesContent.Round(42));
            Check.Throws<ArgumentException>(()=>BossDefinition.ForRound(51));
        });

        test("R50 R40 build completion save continues Joshua without rewriting history",() =>
        {
            var dir=Path.Combine(Path.GetTempPath(),"bible-r50-entry-"+Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(dir);
            try
            {
                var path=Path.Combine(dir,"save.json");
                File.Copy(Fixtures+"r40-build-complete-save.json",path);
                var raw=File.ReadAllBytes(path);
                var store=new SaveStore(path);
                var state=store.Load()!;
                Check.True(raw.SequenceEqual(File.ReadAllBytes(path)));
                Check.Equal(41,state.Run!.RoundNumber);
                Check.Equal("between_rounds",state.Run.Stage);
                var session=AdamSession.Resume(state,CampaignContent.Round(40));
                Check.True(!session.ContentComplete&&!session.JoshuaComplete&&!session.CanEnterJudges&&!session.DeuteronomyComplete);
                Check.Throws<InvalidOperationException>(()=>session.PrepareJudges(CampaignContent.Round(43)));
                Check.Throws<InvalidOperationException>(()=>session.PrepareJoshua(CampaignContent.Round(37)));
                var gold=state.Profile.Gold;
                var olderHistory=OlderHistory(state);
                var later=state.Run.LaterBossHistory!.Select(Json).ToArray();
                Check.True(state.Run.LaterBossHistory!.Select(r=>r.EncounterRound).SequenceEqual(new[]{21,24,27,30,33,36,39}));

                session=session.StartNextRound(CampaignContent.Round(41));
                var revision=store.Commit(state,state.Revision,"r41");
                state=store.Load()!;
                session=AdamSession.Resume(state,CampaignContent.Round(41));
                Check.Equal(gold,state.Profile.Gold);
                Check.Equal(41,state.Run!.RoundNumber);
                Check.Equal("normal",state.Run.Stage);
                Check.Equal(CampaignContent.Round(41).Fingerprint,state.Run.RoundFingerprint);
                Check.Equal(olderHistory,OlderHistory(state));
                Check.True(later.SequenceEqual(state.Run.LaterBossHistory!.Select(Json)));
                Check.True(revision>0);
            }
            finally { Directory.Delete(dir,true); }
        });

        test("R50 progress, atomic Judges entry, ordered boss receipts, and R51 or future save protection",() =>
        {
            var dir=Path.Combine(Path.GetTempPath(),"bible-r50-"+Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(dir);
            try
            {
                var path=Path.Combine(dir,"save.json");
                File.Copy(Fixtures+"r40-build-complete-save.json",path);
                var store=new SaveStore(path);
                var state=store.Load()!;
                var session=AdamSession.Resume(state,CampaignContent.Round(40));
                var revision=state.Revision;
                var runId=state.Run!.RunId;
                var olderHistory=OlderHistory(state);
                var receipts=state.Run.LaterBossHistory!.Select(Json).ToList();
                for(var round=41;round<=50;round++)
                {
                    var definition=CampaignContent.Round(round);
                    if(round==JudgesContent.GlobalRoundNumber)
                    {
                        var raw=File.ReadAllBytes(path);
                        var gold=state.Profile.Gold;
                        Check.True(session.CanEnterJudges&&session.JoshuaComplete&&!session.ContentComplete&&!session.CanEnterJoshua);
                        Check.Throws<InvalidOperationException>(()=>session.StartNextRound(definition));
                        Check.Throws<ArgumentException>(()=>session.PrepareJudges(CampaignContent.Round(44)));
                        Check.Throws<InvalidOperationException>(()=>session.PrepareJoshua(CampaignContent.Round(37)));
                        var candidate=session.PrepareJudges(definition);
                        var unavailable=new SaveStore(path,()=>throw new IOException("before replace"));
                        Check.Throws<IOException>(()=>unavailable.Commit(candidate,revision,"judges"));
                        Check.True(raw.SequenceEqual(File.ReadAllBytes(path))&&session.JoshuaComplete);
                        var accepted=store.Commit(candidate,revision,"judges");
                        Check.Equal(accepted,store.Commit(candidate,revision,"judges"));
                        revision=accepted;
                        state=store.Load()!;
                        session=AdamSession.Resume(state,definition);
                        Check.Equal(gold,state.Profile.Gold);
                        Check.True(!session.JoshuaComplete&&!session.CanEnterJudges);
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
                        receipts.Add(Json(state.Run!.Boss));
                    }
                    Check.Equal(before+definition.CompletionGold,state.Profile.Gold);
                    Check.Equal("between_rounds",session.Stage);
                    Check.True(!session.CompleteNormalSettlement()&&!session.CompleteBossSettlement());
                    revision=store.Commit(state,revision,"finish-"+round);
                    state=store.Load()!;
                    session=AdamSession.Resume(state,definition);
                    Check.Equal(runId,state.Run!.RunId);
                    Check.Equal(round==50,session.ContentComplete);
                    Check.Equal(round==42,session.JoshuaComplete);
                    Check.True(!session.DeuteronomyComplete&&!session.CanEnterJoshua);
                    Check.Equal(olderHistory,OlderHistory(state));
                }
                Check.Equal(51,state.Run!.RoundNumber);
                Check.True(state.Run.Boss==null&&state.Run.RoundCompletionApplied);
                Check.True(state.Run.LaterBossHistory!.Select(r=>r.EncounterRound).SequenceEqual(new[]{21,24,27,30,33,36,39,42,45,48}));
                Check.True(receipts.SequenceEqual(state.Run.LaterBossHistory!.Select(Json)));
                Check.True(!session.CanEnterJudges&&!session.JoshuaComplete);

                var checkpoint=File.ReadAllText(path);
                var backup=File.ReadAllBytes(path+".bak");
                foreach(var change in new Action<JsonNode>[]
                {
                    n=>{n["Run"]!["RoundNumber"]=51;n["Run"]!["Stage"]="normal";},
                    n=>n["Run"]!["RoundNumber"]=52,
                    n=>n["Run"]!["LaterBossHistory"]![9]!["FutureExtension"]=true,
                    n=>n["Run"]!["LaterBossHistory"]![9]!["RoundNumber"]=51,
                    n=>n["Run"]!["LaterBossHistory"]![8]!["Reward"]!["PolicyId"]="r48-production-v1",
                    n=>n["Run"]!["LaterBossHistory"]![7]!["Reward"]!["PolicyId"]="future-reward"
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

    private static string Json(object? value)=>JsonSerializer.Serialize(value);

    private static string OlderHistory(GameSnapshot state)=>Json(new{state.Run!.CompletedBoss,state.Run.CompletedSecondBoss,
        state.Run.CompletedThirdBoss,state.Run.CompletedFourthBoss,state.Run.CompletedFifthBoss,state.Run.CompletedSixthBoss});
}
