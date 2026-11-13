using System;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text.Json;
using BibleSurvivor.Core;

namespace BibleSurvivor.Sim;

internal static class BattleRunner
{
    internal static int Run(string[] args)
    {
        var weapon=args.Length>0?args[0]:"bible";
        var seed=args.Length>1?int.Parse(args[1]):7;
        var definition=CatalogReader.Adam("content/genesis/catalog.json");
        var speeds=CatalogReader.MonsterSpeeds("content/genesis/catalog.json");
        var state=GameSnapshot.New();var game=new AdamSession(state,definition,weapon);
        var world=new BattleWorld(game,speeds,seed);
        var directory=Path.Combine(".local","battle-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(directory);
        var store=new SaveStore(Path.Combine(directory,"save.json"));
        var checkpoint="NOT_RUN";long revision=0;
        for(var frame=0;frame<10860 && game.IsActive;frame++)
        {
            Resolve(game,weapon);
            var input=Movement(world);
            world.Advance(1.0/60,input);
            Resolve(game,weapon);
            if(frame==600 && state.Run!=null)
            {
                game.Capture();state.Run.World=world.Capture();
                revision=store.Commit(state,revision,"mid-battle");
                var loaded=store.Load()!;var resumed=AdamSession.Resume(loaded,definition);
                world=BattleWorld.Restore(resumed,speeds,loaded.Run!.World!);game=resumed;state=loaded;
                checkpoint="PASS";
            }
        }
        Resolve(game,weapon);game.Capture();
        if(state.Run!=null) state.Run.World=world.Capture();
        store.Commit(state,revision,"battle-end");var saved=store.Load()!;
        var outcome=state.Run==null?"dead":state.Run.Stage;
        var passed=!game.IsActive && world.ShotsFired>0 && world.Hits>0 && world.Kills>0 && checkpoint=="PASS";
        Console.WriteLine(JsonSerializer.Serialize(new {
            status=passed?"PASS":"FAIL",kind="fixed-step coordinate combat; no injected kills",starting_weapon=weapon,seed,
            input_policy="nearest XP pickup; otherwise deterministic circular movement",
            simulation_seconds=world.SimulationTime,shots=world.ShotsFired,hits=world.Hits,kills=world.Kills,
            collected_by_distance=world.Collected,damage_taken=world.DamageTaken,level=game.Growth.Level,
            health=game.CurrentHealth,gold=saved.Profile.Gold,outcome,mid_battle_restore=checkpoint,
            unity="NOT_RUN",mobile="NOT_RUN",balance="UNVALIDATED"},new JsonSerializerOptions {WriteIndented=true}));
        return passed?0:1;
    }

    internal static Vector2 Movement(BattleWorld world)
    {
        if(world.DropPositions.Count>0)
            return world.DropPositions.OrderBy(x=>Vector2.DistanceSquared(x.Value,world.PlayerPosition)).ThenBy(x=>x.Key).First().Value-world.PlayerPosition;
        var phase=(float)(world.SimulationTime*.3);
        return new Vector2((float)Math.Cos(phase),(float)Math.Sin(phase));
    }
    internal static void Resolve(AdamSession game,string preferred)
    {
        while(game.PendingChoices.Count>0)
        {
            var choice=game.PendingChoices.Contains(preferred)?preferred:game.PendingChoices.FirstOrDefault(x=>x!="scroll")??game.PendingChoices[0];
            if(!game.Choose(choice)) throw new InvalidOperationException("Pending choice could not be resolved");
        }
    }
}
