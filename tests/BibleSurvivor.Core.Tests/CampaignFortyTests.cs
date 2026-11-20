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

internal static class CampaignFortyTests
{
    private const string Fixtures="tests/BibleSurvivor.Core.Tests/Fixtures/";

    public static void Register(Action<string,Action> test)
    {
        test("R40 R01-R30 fingerprints match the R30 build",() =>
        {
            var lines=File.ReadAllLines(Fixtures+"r30-build-round-fingerprints.txt");
            Check.Equal(30,lines.Length);
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

        test("R40 book cadence, waves, bosses, and installed endpoint",() =>
        {
            foreach(var round in Enumerable.Range(31,10))
            {
                var definition=CampaignContent.Round(round);
                var groups=definition.Waves.SelectMany(w=>w.Groups).ToArray();
                var deuteronomy=round<JoshuaContent.GlobalRoundNumber;
                var prefix=deuteronomy?"dt":"js";
                Check.Equal(deuteronomy?"deuteronomy_"+(round-30).ToString("D2",CultureInfo.InvariantCulture):
                    "joshua_"+(round-36).ToString("D2",CultureInfo.InvariantCulture),definition.Id);
                Check.Equal(1560+(round-22)*60,groups.Sum(x=>x.Count));
                Check.True(groups.Select(x=>x.MonsterId).Distinct().OrderBy(x=>x,StringComparer.Ordinal)
                    .SequenceEqual(new[]{prefix+"1",prefix+"2",prefix+"3"}));
                Check.True(groups.All(x=>CampaignContent.SpeedsFor(round).ContainsKey(x.MonsterId)));
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
            Check.True(new[]{33,36,39}.Select(r=>BossDefinition.ForRound(r).Id)
                .SequenceEqual(new[]{"oblivion_mist","crossroads_sentinel","rampart_colossus"}));
            Check.Throws<ArgumentOutOfRangeException>(()=>CampaignContent.Round(61));
            Check.Throws<ArgumentOutOfRangeException>(()=>CampaignContent.SpeedsFor(61));
            Check.Throws<ArgumentOutOfRangeException>(()=>DeuteronomyContent.Round(37));
            Check.Throws<ArgumentOutOfRangeException>(()=>NumbersContent.Round(31));
            Check.Throws<ArgumentException>(()=>BossDefinition.ForRound(61));
        });

        test("R40 R30 build completion save enters Deuteronomy atomically without rewriting history",() =>
        {
            var dir=Path.Combine(Path.GetTempPath(),"bible-r40-entry-"+Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(dir);
            try
            {
                var path=Path.Combine(dir,"save.json");
                File.Copy(Fixtures+"r30-build-complete-save.json",path);
                var raw=File.ReadAllBytes(path);
                var store=new SaveStore(path);
                var state=store.Load()!;
                Check.True(raw.SequenceEqual(File.ReadAllBytes(path)));
                var session=AdamSession.Resume(state,CampaignContent.Round(30));
                Check.True(session.NumbersComplete&&session.CanEnterDeuteronomy&&!session.ContentComplete&&!session.LeviticusComplete);
                Check.True(!session.CanEnterJoshua&&!session.CanEnterNumbers);
                Check.Throws<InvalidOperationException>(()=>session.StartNextRound(CampaignContent.Round(31)));
                Check.Throws<ArgumentException>(()=>session.PrepareDeuteronomy(CampaignContent.Round(32)));
                Check.Throws<InvalidOperationException>(()=>session.PrepareJoshua(CampaignContent.Round(37)));
                var gold=state.Profile.Gold;
                var olderHistory=OlderHistory(state);
                var later=state.Run!.LaterBossHistory!.Select(Json).Append(Json(state.Run.Boss)).ToArray();

                var candidate=session.PrepareDeuteronomy(CampaignContent.Round(31));
                var unavailable=new SaveStore(path,()=>throw new IOException("before replace"));
                Check.Throws<IOException>(()=>unavailable.Commit(candidate,state.Revision,"deuteronomy"));
                Check.True(raw.SequenceEqual(File.ReadAllBytes(path))&&session.NumbersComplete);
                var accepted=store.Commit(candidate,state.Revision,"deuteronomy");
                Check.Equal(accepted,store.Commit(candidate,state.Revision,"deuteronomy"));

                state=store.Load()!;
                session=AdamSession.Resume(state,CampaignContent.Round(31));
                Check.Equal(gold,state.Profile.Gold);
                Check.Equal(31,state.Run!.RoundNumber);
                Check.Equal("normal",state.Run.Stage);
                Check.True(state.Run.Boss==null&&!session.NumbersComplete);
                Check.Equal(olderHistory,OlderHistory(state));
                Check.True(later.SequenceEqual(state.Run.LaterBossHistory!.Select(Json)));
            }
            finally { Directory.Delete(dir,true); }
        });

        test("R40 progress, atomic Joshua entry, ordered boss receipts, and beyond-endpoint or future save protection",() =>
        {
            var dir=Path.Combine(Path.GetTempPath(),"bible-r40-"+Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(dir);
            try
            {
                var path=Path.Combine(dir,"save.json");
                File.Copy(Fixtures+"r30-build-complete-save.json",path);
                var store=new SaveStore(path);
                var state=store.Load()!;
                var session=AdamSession.Resume(state,CampaignContent.Round(30));
                var revision=store.Commit(session.PrepareDeuteronomy(CampaignContent.Round(31)),state.Revision,"deuteronomy");
                state=store.Load()!;
                session=AdamSession.Resume(state,CampaignContent.Round(31));
                var runId=state.Run!.RunId;
                var olderHistory=OlderHistory(state);
                var receipts=state.Run.LaterBossHistory!.Select(Json).ToList();
                for(var round=31;round<=40;round++)
                {
                    var definition=CampaignContent.Round(round);
                    if(round==JoshuaContent.GlobalRoundNumber)
                    {
                        var raw=File.ReadAllBytes(path);
                        var gold=state.Profile.Gold;
                        Check.True(session.CanEnterJoshua&&session.DeuteronomyComplete&&!session.ContentComplete&&!session.CanEnterDeuteronomy);
                        Check.Throws<InvalidOperationException>(()=>session.StartNextRound(definition));
                        Check.Throws<InvalidOperationException>(()=>session.PrepareDeuteronomy(CampaignContent.Round(31)));
                        var candidate=session.PrepareJoshua(definition);
                        var unavailable=new SaveStore(path,()=>throw new IOException("before replace"));
                        Check.Throws<IOException>(()=>unavailable.Commit(candidate,revision,"joshua"));
                        Check.True(raw.SequenceEqual(File.ReadAllBytes(path))&&session.DeuteronomyComplete);
                        var accepted=store.Commit(candidate,revision,"joshua");
                        Check.Equal(accepted,store.Commit(candidate,revision,"joshua"));
                        revision=accepted;
                        state=store.Load()!;
                        session=AdamSession.Resume(state,definition);
                        Check.Equal(gold,state.Profile.Gold);
                    }
                    else if(round>31)session=session.StartNextRound(definition);
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
                    Check.True(!session.ContentComplete);
                    Check.Equal(round==36,session.DeuteronomyComplete);
                    Check.True(!session.NumbersComplete&&!session.CanEnterDeuteronomy);
                    Check.Equal(olderHistory,OlderHistory(state));
                }
                Check.Equal(41,state.Run!.RoundNumber);
                Check.True(state.Run.Boss==null&&state.Run.RoundCompletionApplied);
                Check.True(state.Run.LaterBossHistory!.Select(r=>r.EncounterRound).SequenceEqual(new[]{21,24,27,30,33,36,39}));
                Check.True(receipts.SequenceEqual(state.Run.LaterBossHistory!.Select(Json)));
                Check.True(!session.CanEnterJoshua&&!session.CanEnterDeuteronomy);

                var checkpoint=File.ReadAllText(path);
                var backup=File.ReadAllBytes(path+".bak");
                foreach(var change in new Action<JsonNode>[]
                {
                    n=>{n["Run"]!["RoundNumber"]=61;n["Run"]!["Stage"]="normal";},
                    n=>n["Run"]!["RoundNumber"]=62,
                    n=>n["Run"]!["LaterBossHistory"]![6]!["FutureExtension"]=true,
                    n=>n["Run"]!["LaterBossHistory"]![5]!["Reward"]!["PolicyId"]="r39-production-v1",
                    n=>n["Run"]!["LaterBossHistory"]![4]!["Reward"]!["PolicyId"]="future-reward"
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
