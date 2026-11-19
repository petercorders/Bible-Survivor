using System;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text.Json;
using System.Text.Json.Nodes;
using BibleSurvivor.Core;

namespace BibleSurvivor.Core.Tests;

internal static class LeviticusSeriesTests
{
    public static void Register(Action<string,Action> test)
    {
        test("R20 fixed chapter data preserves R16 identity and boss cadence",() =>
        {
            Check.Equal(LeviticusContent.Round().Fingerprint,LeviticusContent.Round(16).Fingerprint);
            for(var round=16;round<=20;round++)
            {
                var definition=LeviticusContent.Round(round);
                Check.Equal("leviticus_0"+(round-15),definition.Id);
                Check.Equal(1260+(round-16)*60,definition.Waves.SelectMany(w=>w.Groups).Sum(g=>g.Count));
                Check.Equal(120,definition.ConcurrencyCap);
                Check.Equal(300+(round-16)*20,definition.CompletionGold);
                Check.True(definition.UnlockWeaponId==null);
                Check.Equal(170d,definition.Waves.Last().End);
            }
            Check.Equal("ash_veil",BossDefinition.ForRound(18).Id);
            Check.Throws<ArgumentOutOfRangeException>(()=>LeviticusContent.Round(21));
        });

        test("R20 continuous Leviticus progression preserves six receipts across disk and pays once",() =>
        {
            var dir=Path.Combine(Path.GetTempPath(),"bible-r20-"+Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(dir);
            try
            {
                var store=new SaveStore(Path.Combine(dir,"save.json"));
                var (previous,session)=RoundFifteenTests.CompletedFifteen();
                var r15=JsonSerializer.Serialize(previous.Run!.Boss);
                var gold=previous.Profile.Gold;
                var state=session.PrepareLeviticus(LeviticusContent.Round(16));
                long revision=store.Commit(state,0,"entry-16");
                state=store.Load()!;
                session=AdamSession.Resume(state,LeviticusContent.Round(16));
                string? r18=null;
                for(var round=16;round<=20;round++)
                {
                    var definition=LeviticusContent.Round(round);
                    if(round>16)session=session.StartNextRound(definition);
                    var world=new BattleWorld(session,LeviticusContent.SpeedsFor(round),7);
                    if(round==18)
                    {
                        session.Advance(180);
                        world=new BattleWorld(session,LeviticusContent.SpeedsFor(round),7);
                        world.Advance(6,Vector2.Zero);
                        Check.Equal("boss",session.Stage);
                        Check.True(session.Boss!.Telegraphing);
                        Check.Equal(3,session.Boss.ChainDirections!.Count);
                        state.Run!.World=world.Capture();
                        Check.Equal(4,state.Run.World.SchemaVersion);
                        revision=store.Commit(state,revision,"telegraph");
                        state=store.Load()!;
                        session=AdamSession.Resume(state,definition);
                        world=BattleWorld.Restore(session,LeviticusContent.SpeedsFor(round),state.Run!.World!);
                        world.Advance(1,Vector2.Zero);
                        Check.Equal(3,world.HostileProjectiles.Count);
                        state.Run.World=world.Capture();
                        revision=store.Commit(state,revision,"projectiles");
                        state=store.Load()!;
                        session=AdamSession.Resume(state,definition);
                        world=BattleWorld.Restore(session,LeviticusContent.SpeedsFor(round),state.Run!.World!);
                        Check.Equal(3,world.HostileProjectiles.Count);
                        session.Hit(session.BossRules!.BossActorId,session.BossRules.MaxHealth);
                        Check.Equal("boss_settlement",session.Stage);
                        Check.True(session.CompleteBossSettlement());
                        r18=JsonSerializer.Serialize(state.Run.Boss);
                        gold+=session.BossRules.KillGold+100;
                    }
                    else session.Advance(180);
                    gold+=definition.CompletionGold;
                    Check.Equal("between_rounds",session.Stage);
                    Check.Equal(gold,state.Profile.Gold);
                    Check.True(!session.CompleteNormalSettlement()&&!session.CompleteBossSettlement());
                    revision=store.Commit(state,revision,"finished-"+round);
                    state=store.Load()!;
                    session=AdamSession.Resume(state,definition);
                    Check.Equal(gold,state.Profile.Gold);
                    Check.Equal(r15,JsonSerializer.Serialize(state.Run!.CompletedFifthBoss));
                    if(round>18)Check.Equal(r18,JsonSerializer.Serialize(state.Run!.CompletedSixthBoss));
                    Check.Equal(round==20,session.ContentComplete);
                }
                Check.Equal(21,state.Run!.RoundNumber);
                Check.True(!session.CanEnterLeviticus&&!session.ExodusComplete);
                var raw=File.ReadAllText(store.Path);
                var backup=File.ReadAllText(store.Path+".bak");
                foreach(var change in new Action<JsonNode>[]
                {
                    n=>n["Run"]!["RoundNumber"]=22,
                    n=>n["Run"]!["CompletedSixthBoss"]!["FutureExtension"]=true
                })
                {
                    var future=JsonNode.Parse(raw)!;
                    change(future);
                    var text=future.ToJsonString();
                    File.WriteAllText(store.Path,text);
                    Check.Throws<UnsupportedSaveVersionException>(()=>store.Load());
                    Check.Throws<UnsupportedSaveVersionException>(()=>store.Commit(state,revision,"future-replace"));
                    Check.Equal(text,File.ReadAllText(store.Path));
                    Check.Equal(backup,File.ReadAllText(store.Path+".bak"));
                }
            }
            finally { Directory.Delete(dir,true); }
        });
    }
}
