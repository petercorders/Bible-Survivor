using System;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text.Json;
using System.Text.Json.Nodes;
using BibleSurvivor.Core;

namespace BibleSurvivor.Core.Tests;

internal static class CampaignThirtyTests
{
    public static void Register(Action<string,Action> test)
    {
        test("R30 chapter cadence, waves, actor ids, and installed endpoint",() =>
        {
            foreach(var round in Enumerable.Range(21,10))
            {
                var definition=CampaignContent.Round(round);
                var monsters=definition.Waves.SelectMany(w=>w.Groups).ToArray();
                Check.Equal(round==21?1560:1560+(round-22)*60,monsters.Sum(x=>x.Count));
                Check.True(monsters.All(x=>CampaignContent.SpeedsFor(round).ContainsKey(x.MonsterId)));
                Check.Equal(170d,definition.Waves.Last().End);
                if(round%3==0)
                {
                    var boss=BossDefinition.ForRound(round);
                    Check.True(boss.UsesChainProjectiles);
                    Check.Equal(monsters.Sum(x=>x.Count)+1,boss.BossActorId);
                    Check.True(boss.EncounterKey("run").StartsWith("run:"+definition.Id+":",StringComparison.Ordinal));
                }
                else Check.Throws<ArgumentException>(()=>BossDefinition.ForRound(round));
            }
            Check.Throws<ArgumentOutOfRangeException>(()=>NumbersContent.Round(31));
        });

        test("R30 later boss content is never interpreted as a corrupt legacy primary",() =>
        {
            var dir=Path.Combine(Path.GetTempPath(),"bible-future-boss-"+Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(dir);
            try
            {
                var path=Path.Combine(dir,"save.json");
                var store=new SaveStore(path);
                var (state,_)=RoundTenTests.CompletedNine();
                var revision=store.Commit(state,0,"legacy");
                var original=File.ReadAllText(path);
                File.WriteAllText(path+".bak",original);
                foreach(var schema in new[]{1,2})
                {
                    var newer=JsonNode.Parse(original)!;
                    var boss=newer["Run"]!["Boss"]!;
                    newer["SchemaVersion"]=schema;
                    boss["RoundNumber"]=21;
                    boss["EncounterId"]=BossDefinition.ForRound(21).EncounterKey(state.Run!.RunId);
                    var future=newer.ToJsonString();
                    File.WriteAllText(path,future);
                    Check.Throws<UnsupportedSaveVersionException>(()=>store.Load());
                    Check.Throws<UnsupportedSaveVersionException>(()=>store.Commit(state,revision,"replace-legacy"));
                    Check.Equal(future,File.ReadAllText(path));
                    Check.Equal(original,File.ReadAllText(path+".bak"));
                }
                var foreignPolicy=JsonNode.Parse(original)!;
                foreignPolicy["Run"]!["Boss"]!["Reward"]!["PolicyId"]="r21-production-v1";
                var foreign=foreignPolicy.ToJsonString();
                File.WriteAllText(path,foreign);
                Check.Throws<UnsupportedSaveVersionException>(()=>store.Load());
                Check.Equal(foreign,File.ReadAllText(path));
                Check.Equal(original,File.ReadAllText(path+".bak"));
            }
            finally { Directory.Delete(dir,true); }
        });

        test("R30 progress, atomic Numbers entry, boss receipts, rewards, and future save protection",() =>
        {
            var dir=Path.Combine(Path.GetTempPath(),"bible-r30-"+Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(dir);
            try
            {
                var path=Path.Combine(dir,"save.json");
                var store=new SaveStore(path);
                var (_,r15)=RoundFifteenTests.CompletedFifteen();
                var state=r15.PrepareLeviticus(CampaignContent.Round(16));
                var session=AdamSession.Resume(state,CampaignContent.Round(16));
                var runId=state.Run!.RunId;
                long revision=0;
                for(var round=16;round<=30;round++)
                {
                    var definition=CampaignContent.Round(round);
                    if(round==22)
                    {
                        var raw=File.ReadAllBytes(path);
                        var gold=state.Profile.Gold;
                        var previous=JsonSerializer.Serialize(state.Run!.Boss);
                        Check.True(session.CanEnterNumbers&&session.LeviticusComplete&&!session.ContentComplete);
                        Check.Throws<InvalidOperationException>(()=>session.StartNextRound(definition));
                        var candidate=session.PrepareNumbers(definition);
                        var unavailable=new SaveStore(path,()=>throw new IOException("before replace"));
                        Check.Throws<IOException>(()=>unavailable.Commit(candidate,revision,"numbers"));
                        Check.True(raw.SequenceEqual(File.ReadAllBytes(path))&&session.LeviticusComplete);
                        var accepted=store.Commit(candidate,revision,"numbers");
                        Check.Equal(accepted,store.Commit(candidate,revision,"numbers"));
                        revision=accepted;
                        state=store.Load()!;
                        session=AdamSession.Resume(state,definition);
                        Check.Equal(gold,state.Profile.Gold);
                        Check.Equal(previous,JsonSerializer.Serialize(state.Run!.LaterBossHistory!.Single()));
                    }
                    else if(round>16)session=session.StartNextRound(definition);

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
                    }
                    Check.Equal(before+definition.CompletionGold,state.Profile.Gold);
                    Check.Equal("between_rounds",session.Stage);
                    Check.True(!session.CompleteNormalSettlement()&&!session.CompleteBossSettlement());
                    revision=store.Commit(state,revision,"finish-"+round);
                    state=store.Load()!;
                    session=AdamSession.Resume(state,definition);
                    Check.Equal(runId,state.Run!.RunId);
                    Check.True(!session.ContentComplete);
                    Check.Equal(round==30,session.NumbersComplete);
                    Check.Equal(round==21,session.LeviticusComplete);
                }
                Check.Equal(31,state.Run!.RoundNumber);
                Check.True(state.Run.LaterBossHistory!.Select(r=>r.EncounterRound).SequenceEqual(new[]{21,24,27}));
                Check.Equal(30,state.Run.Boss!.EncounterRound);
                var checkpoint=File.ReadAllText(path);
                var backup=File.ReadAllBytes(path+".bak");
                foreach(var change in new Action<JsonNode>[]
                {
                    n=>n["Run"]!["RoundNumber"]=52,
                    n=>n["Run"]!["LaterBossHistory"]![0]!["FutureExtension"]=true,
                    n=>n["Run"]!["LaterBossHistory"]![0]!["Reward"]!["PolicyId"]="future-reward"
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
}
