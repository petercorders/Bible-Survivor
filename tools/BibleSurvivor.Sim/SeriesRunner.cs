using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using BibleSurvivor.Core;

namespace BibleSurvivor.Sim;

internal static class SeriesRunner
{
    internal static int Run(string[] args,bool combat)
    {
        var weapon=args.Length>0?args[0]:"sling";var seed=args.Length>1?int.Parse(args[1]):7;
        const string catalog="content/genesis/catalog.json";
        var first=CatalogReader.Round(catalog,1);var second=CatalogReader.Round(catalog,2);
        var speeds=CatalogReader.MonsterSpeeds(catalog);
        var state=GameSnapshot.New();var game=new AdamSession(state,first,weapon);
        var folder=Path.Combine(".local","series-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(folder);
        var store=new SaveStore(Path.Combine(folder,"save.json"));long revision=0;
        var records=new List<object>();var killed=0;var rawXp=0;var passed=true;
        foreach(var definition in new[]{first,second})
        {
            var world=combat?new BattleWorld(game,speeds,seed):null;
            var midpoint=false;var roundKills=0;var roundRaw=0;
            for(var tick=0;tick<(combat?10860:1801)&&game.IsActive;tick++)
            {
                BattleRunner.Resolve(game,weapon);
                if(world!=null)world.Advance(BattleWorld.FixedStep,BattleRunner.Movement(world));
                else
                {
                    game.Advance(.1);
                    foreach(var enemy in game.Wave!.Active.Values.ToArray())
                    {
                        BattleRunner.Resolve(game,weapon);game.Hit(enemy.Id,enemy.Health);roundKills++;roundRaw+=enemy.RawExperience;
                        game.Collect(enemy.Id);BattleRunner.Resolve(game,weapon);
                    }
                }
                BattleRunner.Resolve(game,weapon);
                if(tick==(combat?600:100)&&state.Run!=null)
                {
                    game.Capture();state.Run.World=world?.Capture();
                    revision=store.Commit(state,revision,"r"+definition.Number+"-midpoint");state=store.Load()!;
                    game=AdamSession.Resume(state,definition);
                    if(world!=null)world=BattleWorld.Restore(game,speeds,state.Run!.World!);
                    midpoint=true;
                }
            }
            BattleRunner.Resolve(game,weapon);game.Capture();if(state.Run!=null)state.Run.World=world?.Capture();
            revision=store.Commit(state,revision,"r"+definition.Number+"-end");state=store.Load()!;
            var completed=state.Run?.Stage=="between_rounds"&&state.Run.RoundNumber==definition.Number+1;
            // A normal round can legitimately complete by timeout, even with no kills.
            // Attack/kill correctness is checked separately by the controlled coordinate tests.
            passed&=completed&&midpoint;
            records.Add(new{round=definition.Id,outcome=state.Run?.Stage??"dead",midpoint_restore=midpoint,
                hits=world?.Hits,kills=world?.Kills??roundKills,raw_xp=world==null?(int?)roundRaw:null,
                seconds=world?.SimulationTime,gold=state.Profile.Gold});
            killed+=roundKills;rawXp+=roundRaw;
            if(!completed)break;
            game=AdamSession.Resume(state,definition);
            if(definition.Number==1)
            {
                game=game.StartNextRound(second);
                revision=store.Commit(state,revision,"r02-enter");state=store.Load()!;game=AdamSession.Resume(state,second);
            }
        }
        if(!combat)passed&=killed==600&&rawXp==900&&state.Profile.Gold==180;
        passed&=state.Run?.RoundNumber==3&&state.Run.Stage=="between_rounds";
        Console.WriteLine(JsonSerializer.Serialize(new{status=passed?"PASS":"FAIL",
            kind=combat?"R01 to R02 fixed-step coordinate combat; no injected kills":"R01 to R02 injected-kill budget/growth simulation; not gameplay",
            starting_weapon=weapon,seed,rounds=records,next_round=state.Run?.RoundNumber,
            boss_round="R03 WAITING; not implemented",unlocked=state.Profile.UnlockedWeapons,
            equippable_attacks=new[]{"bible","jawbone","sling"},save=store.Path,
            unity="NOT_RUN",mobile="NOT_RUN",balance="UNVALIDATED"},new JsonSerializerOptions{WriteIndented=true}));
        return passed?0:1;
    }
}
