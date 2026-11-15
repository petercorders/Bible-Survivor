using System;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text.Json;
using BibleSurvivor.Core;

namespace BibleSurvivor.Sim;
internal static class BossRunner
{
    internal static int Run(string[] args,bool settle=false)
    {
        var weapon=args.Length>0?args[0]:"sling";var seed=args.Length>1?int.Parse(args[1]):7;
        const string catalog="content/genesis/catalog.json";var speeds=CatalogReader.MonsterSpeeds(catalog);
        var state=GameSnapshot.New();var game=new AdamSession(state,CatalogReader.Round(catalog,1),weapon);
        // Explicit isolated setup: timeout R1/R2, equipment8 and max technical HP fixture.
        game.Advance(180);game=game.StartNextRound(CatalogReader.Round(catalog,2));game.Advance(180);
        game=game.StartNextRound(CatalogReader.Round(catalog,3));
        for(var i=1;i<8;i++)game.Inventory.TryChoose(weapon,false);
        if(settle)game.Inventory.TryChoose(weapon=="bible"?"scroll":weapon=="jawbone"?"hair":"stones",true);
        state.Run!.MaxHealth=state.Run.Health=10000;game.Capture();
        var world=new BattleWorld(game,speeds,seed);var normalHits=0;var mid=false;var phase="normal";
        var folder=Path.Combine(".local","boss-combat-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(folder);
        var store=new SaveStore(Path.Combine(folder,"save.json"));long revision=0;
        for(var frame=0;frame<72000&&game.IsActive;frame++)
        {
            BattleRunner.Resolve(game,weapon);
            Vector2 input;
            if(game.Stage=="normal")input=BattleRunner.Movement(world);
            else if(game.Stage=="boss_intro")input=Vector2.Zero;
            else
            {
                var boss=world.EnemyPositions[481];var distance=Vector2.Distance(boss,world.PlayerPosition);
                // Remain inside sling range, approach for orbit/melee; no injected damage.
                input=distance>(weapon=="sling"?4:1)?boss-world.PlayerPosition:Vector2.Zero;
            }
            world.Advance(BattleWorld.FixedStep,input);
            if(phase=="normal"&&game.Stage!="normal"){normalHits=world.Hits;phase=game.Stage;}
            if(game.Stage!="boss_reward")BattleRunner.Resolve(game,weapon);
            if(!mid&&game.Stage=="boss"&&game.Boss!.Telegraphing)
            {
                game.Capture();state.Run!.World=world.Capture();revision=store.Commit(state,revision,"boss-telegraph");state=store.Load()!;
                game=AdamSession.Resume(state,CatalogReader.Round(catalog,3));world=BattleWorld.Restore(game,speeds,state.Run!.World!);mid=true;
            }
        }
        game.Capture();if(state.Run!=null)state.Run.World=world.Capture();
        revision=store.Commit(state,revision,"boss-end");state=store.Load()!;game=AdamSession.Resume(state,CatalogReader.Round(catalog,3));
        var passed=mid&&game.Stage=="boss_reward"&&game.BossChoices.Count>0&&world.BossKills==1&&world.Hits>normalHits&&game.Boss!.ThresholdUsed&&state.Run!.RoundNumber==3;
        var rewardReload=false;
        if(settle&&passed)
        {
            var choice=game.BossChoices.First(x=>x.Kind=="evolution");
            var stable=JsonSerializer.Serialize(game.BossChoices);state=store.Load()!;game=AdamSession.Resume(state,CatalogReader.Round(catalog,3));
            rewardReload=stable==JsonSerializer.Serialize(game.BossChoices);
            passed&=game.ChooseBossReward(game.Boss!.Reward!.RewardId,choice.Id);
            BattleRunner.Resolve(game,weapon);passed&=game.CompleteBossSettlement();
            revision=store.Commit(state,revision,"boss-completed");state=store.Load()!;game=AdamSession.Resume(state,CatalogReader.Round(catalog,3));
            passed&=rewardReload&&state.Run!.RoundNumber==4&&state.Run.Stage=="between_rounds"&&game.Inventory.Forms.ContainsKey(weapon)&&!game.CompleteBossSettlement();
        }
        Console.WriteLine(JsonSerializer.Serialize(new{status=passed?"PASS":"FAIL",kind="R03 coordinate combat; isolated R1/R2 timeout + equipment8 HP10000 fixture; no injected R03 damage",
            weapon,seed,normal_hits=normalHits,hits=world.Hits,boss_kills=world.BossKills,normal_and_support_kills=world.Kills,seconds=world.SimulationTime,
            phase=game.Stage,boss_seconds=game.Boss?.BossElapsed,summoned=game.Boss?.Summoned,health=game.CurrentHealth,gold=state.Profile.Gold,
            midpoint_disk_restore=mid,reward_reload=rewardReload,reward=game.Boss?.Reward?.PolicyId,forms=game.Inventory.Forms,completion_receipt=game.Boss?.CompletionApplied,save=store.Path,
            unity="NOT_RUN",mobile="NOT_RUN",balance="UNVALIDATED"},new JsonSerializerOptions{WriteIndented=true}));
        return passed?0:1;
    }
}
