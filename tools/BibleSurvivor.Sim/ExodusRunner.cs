using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using BibleSurvivor.Core;
namespace BibleSurvivor.Sim;
internal static class ExodusRunner
{
    internal static int Run(GameSnapshot state,AdamSession game,SaveStore store,long revision,string weapon,int seed)
    {
        var definition=CatalogReader.Exodus("content/exodus/catalog.json");var speeds=CatalogReader.MonsterSpeeds("content/exodus/catalog.json");
        var before=File.ReadAllBytes(store.Path);var gold=state.Profile.Gold;var health=game.CurrentHealth;var runId=state.Run!.RunId;
        var first=JsonSerializer.Serialize(state.Run.CompletedBoss);var second=JsonSerializer.Serialize(state.Run.CompletedSecondBoss);var third=JsonSerializer.Serialize(state.Run.Boss);
        var candidate=game.PrepareExodus(definition);var passed=game.CanEnterExodus&&before.SequenceEqual(File.ReadAllBytes(store.Path))&&candidate.Profile.Gold==gold&&candidate.Run!.Health==health;
        revision=store.Commit(candidate,revision,"exodus-entry");state=store.Load()!;game=AdamSession.Resume(state,definition);var world=new BattleWorld(game,speeds,seed);var midpoint=false;
        for(var tick=0;tick<10860&&game.IsActive;tick++)
        {
            BattleRunner.Resolve(game,weapon);world.Advance(BattleWorld.FixedStep,BattleRunner.Movement(world));BattleRunner.Resolve(game,weapon);
            if(!midpoint&&world.SimulationTime>=60&&game.IsActive)
            {
                game.Capture();state.Run!.World=world.Capture();var geometry=JsonSerializer.Serialize(world.Capture());revision=store.Commit(state,revision,"exodus-midpoint");state=store.Load()!;
                game=AdamSession.Resume(state,definition);world=BattleWorld.Restore(game,speeds,state.Run!.World!);midpoint=geometry==JsonSerializer.Serialize(world.Capture());
            }
        }
        BattleRunner.Resolve(game,weapon);game.Capture();if(state.Run!=null)state.Run.World=world.Capture();
        passed&=midpoint&&game.ContentComplete&&!game.ChapterComplete&&!game.CanEnterExodus&&state.Run?.RoundNumber==11&&world.Hits>0&&world.Kills>0;
        if(passed)
        {
            var paid=state.Profile.Gold;passed&=!game.CompleteNormalSettlement()&&state.Profile.Gold==paid;
            revision=store.Commit(state,revision,"exodus-complete");state=store.Load()!;game=AdamSession.Resume(state,definition);
            passed&=game.ContentComplete&&state.Run!.RunId==runId&&first==JsonSerializer.Serialize(state.Run.CompletedBoss)&&second==JsonSerializer.Serialize(state.Run.CompletedSecondBoss)&&third==JsonSerializer.Serialize(state.Run.CompletedThirdBoss);
        }
        Console.WriteLine(JsonSerializer.Serialize(new {status=passed?"PASS":"FAIL",kind="R01 through R10 actual coordinate combat; existing equipment8/passive1 HP10000 technical fixture; no injected hits or kills; not natural completion",weapon,seed,r09_completion_checkpoint=true,exodus_entry_no_payment_or_heal=candidate.Profile.Gold==gold&&candidate.Run!.Health==health,midpoint_disk_restore=midpoint,three_boss_receipts_preserved=passed,content_complete=game.ContentComplete,chapter_complete=game.ChapterComplete,next_round=state.Run?.RoundNumber,hits=world.Hits,kills=world.Kills,seconds=world.SimulationTime,gold=state.Profile.Gold,save=store.Path,unity="NOT_RUN",mobile="NOT_RUN",balance="UNVALIDATED"},new JsonSerializerOptions{WriteIndented=true}));
        return passed?0:1;
    }
}
