using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using BibleSurvivor.Core;
namespace BibleSurvivor.Sim;
internal static class ExodusRunner
{
    internal static int Run(GameSnapshot state,AdamSession game,SaveStore store,long revision,string weapon,int seed,bool eleventh=false,bool twelfth=false)
    {
        var definition=CatalogReader.Exodus("content/exodus/catalog.json");var speeds=CatalogReader.MonsterSpeeds("content/exodus/catalog.json");
        var before=File.ReadAllBytes(store.Path);var gold=state.Profile.Gold;var health=game.CurrentHealth;var runId=state.Run!.RunId;
        var first=JsonSerializer.Serialize(state.Run.CompletedBoss);var second=JsonSerializer.Serialize(state.Run.CompletedSecondBoss);var third=JsonSerializer.Serialize(state.Run.Boss);
        var candidate=game.PrepareExodus(definition);var passed=game.CanEnterExodus&&before.SequenceEqual(File.ReadAllBytes(store.Path))&&candidate.Profile.Gold==gold&&candidate.Run!.Health==health;
        revision=store.Commit(candidate,revision,"exodus-entry");state=store.Load()!;game=AdamSession.Resume(state,definition);var world=new BattleWorld(game,speeds,seed);var records=new List<object>();
        for(var round=10;round<=(twelfth?12:eleventh?11:10);round++)
        {
            definition=CatalogReader.Exodus("content/exodus/catalog.json",round);
            if(round>=11){var entryGold=state.Profile.Gold;var entryHp=game.CurrentHealth;game=game.StartNextRound(definition);passed&=entryGold==state.Profile.Gold&&entryHp==game.CurrentHealth;world=new BattleWorld(game,speeds,seed);}
            var equipment=JsonSerializer.Serialize(new{weapons=game.Inventory.Weapons,passives=game.Inventory.Passives,forms=game.Inventory.Forms,health=game.CurrentHealth});var midpoint=false;
            var phaseRestores=new HashSet<string>();var coordinates=new List<object>();
            for(var tick=0;tick<(round==12?30000:10860)&&game.IsActive;tick++)
            {
                BattleRunner.Resolve(game,weapon);world.Advance(BattleWorld.FixedStep,BattleRunner.Movement(world));BattleRunner.Resolve(game,weapon);
                if(round==12&&tick%120==0)coordinates.Add(new{time=world.SimulationTime,player=world.PlayerPosition.ToString(),boss=world.ChariotDash?.Origin.ToString(),phase=world.ChariotDash?.Phase,boss_clock=game.Boss?.BossElapsed});
                var phase=game.Boss?.ChariotCharge?.Phase;
                if(round==12&&game.Stage=="boss"&&(phase=="telegraph"||phase=="dash")&&!phaseRestores.Contains(phase))
                {
                    game.Capture();state.Run!.World=world.Capture();var geometry=JsonSerializer.Serialize(world.Capture());var encounter=JsonSerializer.Serialize(game.Boss);revision=store.Commit(state,revision,"r12-"+phase);state=store.Load()!;
                    game=AdamSession.Resume(state,definition);world=BattleWorld.Restore(game,speeds,state.Run!.World!);passed&=geometry==JsonSerializer.Serialize(world.Capture())&&encounter==JsonSerializer.Serialize(game.Boss);phaseRestores.Add(phase);
                }
                if(!midpoint&&world.SimulationTime>=60&&game.IsActive)
                {
                    game.Capture();state.Run!.World=world.Capture();var geometry=JsonSerializer.Serialize(world.Capture());revision=store.Commit(state,revision,"exodus-midpoint-"+round);state=store.Load()!;
                    game=AdamSession.Resume(state,definition);world=BattleWorld.Restore(game,speeds,state.Run!.World!);midpoint=geometry==JsonSerializer.Serialize(world.Capture());
                }
            }
            if(round==12)
            {
                passed&=phaseRestores.Count==2&&world.BossKills==1;
                if(game.Stage=="boss_reward")
                {
                    var candidates=JsonSerializer.Serialize(game.BossChoices);game.Capture();revision=store.Commit(state,revision,"r12-reward");state=store.Load()!;game=AdamSession.Resume(state,definition);passed&=candidates==JsonSerializer.Serialize(game.BossChoices);
                    var choice=game.BossChoices.FirstOrDefault(x=>x.Kind=="evolution")??game.BossChoices.First();passed&=game.ChooseBossReward(game.Boss!.Reward!.RewardId,choice.Id);
                }
                BattleRunner.Resolve(game,weapon);passed&=game.CompleteBossSettlement();
            }
            BattleRunner.Resolve(game,weapon);game.Capture();if(state.Run!=null)state.Run.World=world.Capture();
            passed&=midpoint&&game.ContentComplete==(round==ExodusContent.LastInstalledRound)&&!game.ChapterComplete&&!game.CanEnterExodus&&state.Run?.RoundNumber==round+1&&world.Hits>0&&world.Kills>0;
            if(passed)
            {
                var paid=state.Profile.Gold;passed&=!game.CompleteNormalSettlement()&&state.Profile.Gold==paid;
                revision=store.Commit(state,revision,"exodus-complete-"+round);state=store.Load()!;game=AdamSession.Resume(state,definition);
                passed&=game.ContentComplete==(round==ExodusContent.LastInstalledRound)&&state.Run!.RunId==runId&&first==JsonSerializer.Serialize(state.Run.CompletedBoss)&&second==JsonSerializer.Serialize(state.Run.CompletedSecondBoss)&&third==JsonSerializer.Serialize(state.Run.CompletedThirdBoss);
            }
            records.Add(new{round,midpoint_disk_restore=midpoint,boss_phase_restores=phaseRestores,coordinates,hits=world.Hits,kills=world.Kills,boss_kills=world.BossKills,seconds=world.SimulationTime,equipment});
        }
        Console.WriteLine(JsonSerializer.Serialize(new {status=passed?"PASS":"FAIL",kind="R01 through R"+(twelfth?"12":eleventh?"11":"10")+" actual coordinate combat; existing equipment8/passive1 HP10000 technical fixture; no injected hits or kills; not natural completion",weapon,seed,r09_completion_checkpoint=true,exodus_entry_no_payment_or_heal=candidate.Profile.Gold==gold&&candidate.Run!.Health==health,rounds=records,three_boss_receipts_preserved=passed,content_complete=game.ContentComplete,chapter_complete=game.ChapterComplete,next_round=state.Run?.RoundNumber,hits=world.Hits,kills=world.Kills,seconds=world.SimulationTime,gold=state.Profile.Gold,save=store.Path,unity="NOT_RUN",mobile="NOT_RUN",balance="UNVALIDATED"},new JsonSerializerOptions{WriteIndented=true}));
        return passed?0:1;
    }
}
