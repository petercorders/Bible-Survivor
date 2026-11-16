using System;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text.Json;
using BibleSurvivor.Core;

namespace BibleSurvivor.Sim;
internal static class BossRunner
{
    internal static int Run(string[] args,bool settle=false,bool fourth=false,bool fifth=false,bool sixth=false,bool seventh=false)
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
        var r04Restore=false;var observedCharge=false;var r04Kills=0;string? restoredChargePhase=null;
        if(fourth&&passed)
        {
            game=game.StartNextRound(CatalogReader.Round(catalog,4));world=new BattleWorld(game,speeds,seed);
            revision=store.Commit(state,revision,"r04-enter");state=store.Load()!;game=AdamSession.Resume(state,CatalogReader.Round(catalog,4));world=new BattleWorld(game,speeds,seed);
            for(var tick=0;tick<10860&&game.IsActive;tick++)
            {
                BattleRunner.Resolve(game,weapon);world.Advance(BattleWorld.FixedStep,BattleRunner.Movement(world));BattleRunner.Resolve(game,weapon);
                observedCharge|=world.ChargeStates.Values.Any(x=>x.Phase=="telegraph"||x.Phase=="charge");
                if(!r04Restore&&world.ChargeStates.Count>0)
                {
                    restoredChargePhase=world.ChargeStates.Values.OrderBy(x=>x.Id).First().Phase;
                    game.Capture();state.Run!.World=world.Capture();revision=store.Commit(state,revision,"r04-charge-state");state=store.Load()!;
                    game=AdamSession.Resume(state,CatalogReader.Round(catalog,4));world=BattleWorld.Restore(game,speeds,state.Run!.World!);r04Restore=true;
                }
            }
            BattleRunner.Resolve(game,weapon);game.Capture();state.Run!.World=world.Capture();r04Kills=world.Kills;
            revision=store.Commit(state,revision,"r04-completed");state=store.Load()!;game=AdamSession.Resume(state,CatalogReader.Round(catalog,4));
            passed&=r04Restore&&world.Hits>0&&world.Kills>0&&state.Run!.RoundNumber==5&&state.Run.CompletedBoss?.CompletionApplied==true&&state.Run.RoundCompletionApplied;
        }
        var r05Restore=false;var r05Kills=0;
        if(fifth&&passed)
        {
            game=game.StartNextRound(CatalogReader.Round(catalog,5));world=new BattleWorld(game,speeds,seed);
            revision=store.Commit(state,revision,"r05-enter");state=store.Load()!;game=AdamSession.Resume(state,CatalogReader.Round(catalog,5));world=new BattleWorld(game,speeds,seed);
            for(var tick=0;tick<10860&&game.IsActive;tick++)
            {
                BattleRunner.Resolve(game,weapon);world.Advance(BattleWorld.FixedStep,BattleRunner.Movement(world));BattleRunner.Resolve(game,weapon);
                passed&=world.ChargeStates.Count==0;
                if(tick==600)
                {
                    game.Capture();state.Run!.World=world.Capture();revision=store.Commit(state,revision,"r05-mid");state=store.Load()!;
                    game=AdamSession.Resume(state,CatalogReader.Round(catalog,5));world=BattleWorld.Restore(game,speeds,state.Run!.World!);r05Restore=true;
                }
            }
            BattleRunner.Resolve(game,weapon);game.Capture();state.Run!.World=world.Capture();r05Kills=world.Kills;
            revision=store.Commit(state,revision,"r05-completed");state=store.Load()!;game=AdamSession.Resume(state,CatalogReader.Round(catalog,5));
            passed&=r05Restore&&world.Hits>0&&world.Kills>0&&state.Run!.RoundNumber==6&&state.Run.PreviousNormalRound==4&&state.Run.RoundCompletionApplied&&game.Inventory.Forms.ContainsKey(weapon);
        }
        var r06Restore=false;var r06RewardRestore=false;var preservedR03=false;
        if(sixth&&passed)
        {
            var history=JsonSerializer.Serialize(state.Run!.CompletedBoss);game=game.StartNextRound(CatalogReader.Round(catalog,6));world=new BattleWorld(game,speeds,seed);
            revision=store.Commit(state,revision,"r06-enter");state=store.Load()!;game=AdamSession.Resume(state,CatalogReader.Round(catalog,6));world=new BattleWorld(game,speeds,seed);
            for(var tick=0;tick<72000&&game.IsActive;tick++)
            {
                BattleRunner.Resolve(game,weapon);Vector2 input;
                if(game.Stage=="normal")input=BattleRunner.Movement(world);
                else if(game.Stage=="boss_intro")input=Vector2.Zero;
                else {var actor=world.EnemyPositions[game.BossRules!.BossActorId];input=Vector2.Distance(actor,world.PlayerPosition)>4?actor-world.PlayerPosition:Vector2.Zero;}
                world.Advance(BattleWorld.FixedStep,input);if(game.Stage!="boss_reward")BattleRunner.Resolve(game,weapon);
                if(!r06Restore&&game.Stage=="boss"&&game.Boss!.SummonMask!=0)
                {
                    game.Capture();state.Run!.World=world.Capture();revision=store.Commit(state,revision,"r06-threshold");state=store.Load()!;
                    game=AdamSession.Resume(state,CatalogReader.Round(catalog,6));world=BattleWorld.Restore(game,speeds,state.Run!.World!);r06Restore=true;
                }
            }
            game.Capture();state.Run!.World=world.Capture();revision=store.Commit(state,revision,"r06-reward");state=store.Load()!;game=AdamSession.Resume(state,CatalogReader.Round(catalog,6));
            passed&=r06Restore&&world.BossKills==1&&game.Stage=="boss_reward"&&game.Boss!.SummonMask==3&&game.Boss.Summoned==12;
            if(passed)
            {
                var candidates=JsonSerializer.Serialize(game.BossChoices);state=store.Load()!;game=AdamSession.Resume(state,CatalogReader.Round(catalog,6));r06RewardRestore=candidates==JsonSerializer.Serialize(game.BossChoices);
                var choice=game.BossChoices.FirstOrDefault(x=>x.Kind=="evolution")??game.BossChoices.First();passed&=game.ChooseBossReward(game.Boss!.Reward!.RewardId,choice.Id);
                BattleRunner.Resolve(game,weapon);passed&=game.CompleteBossSettlement();revision=store.Commit(state,revision,"r06-completed");state=store.Load()!;game=AdamSession.Resume(state,CatalogReader.Round(catalog,6));
                preservedR03=history==JsonSerializer.Serialize(state.Run!.CompletedBoss);
                passed&=r06RewardRestore&&preservedR03&&state.Run.RoundNumber==7&&game.Boss!.CompletionApplied&&!game.CompleteBossSettlement();
            }
        }
        var r07Restore=false;var preservedBoth=false;var r07Sides=false;
        if(seventh&&passed)
        {
            var first=JsonSerializer.Serialize(state.Run!.CompletedBoss);var second=JsonSerializer.Serialize(state.Run.Boss);
            game=game.StartNextRound(CatalogReader.Round(catalog,7));world=new BattleWorld(game,speeds,seed);
            for(var tick=0;tick<10860&&game.IsActive;tick++)
            {
                BattleRunner.Resolve(game,weapon);world.Advance(BattleWorld.FixedStep,BattleRunner.Movement(world));BattleRunner.Resolve(game,weapon);
                passed&=world.ChargeStates.Count==0;
                if(!r07Restore&&game.Targets.Values.Any(x=>x.MonsterId=="g3"))
                {
                    r07Sides=true;game.Capture();state.Run!.World=world.Capture();revision=store.Commit(state,revision,"r07-g3");state=store.Load()!;
                    game=AdamSession.Resume(state,CatalogReader.Round(catalog,7));world=BattleWorld.Restore(game,speeds,state.Run!.World!);r07Restore=true;
                }
            }
            BattleRunner.Resolve(game,weapon);game.Capture();state.Run!.World=world.Capture();revision=store.Commit(state,revision,"r07-completed");state=store.Load()!;game=AdamSession.Resume(state,CatalogReader.Round(catalog,7));
            preservedBoth=first==JsonSerializer.Serialize(state.Run!.CompletedBoss)&&second==JsonSerializer.Serialize(state.Run.CompletedSecondBoss);
            passed&=r07Restore&&r07Sides&&preservedBoth&&world.Kills>0&&world.Hits>0&&state.Run.RoundNumber==8&&state.Run.RoundCompletionApplied&&state.Profile.UnlockedWeapons.Contains("elijah_chariot")&&!game.CompleteNormalSettlement();
        }
        Console.WriteLine(JsonSerializer.Serialize(new{status=passed?"PASS":"FAIL",kind=seventh?"R03 to R07 coordinate combat; R1/R2 timeout + equipment8 HP10000 fixture":sixth?"R03 to R06 coordinate combat; R1/R2 timeout + equipment8 HP10000 fixture":fifth?"R03 to R05 coordinate combat; R1/R2 timeout + equipment8 HP10000 fixture":fourth?"R03 to R04 coordinate combat; R1/R2 timeout + equipment8 HP10000 fixture":"R03 coordinate combat; isolated R1/R2 timeout + equipment8 HP10000 fixture; no injected R03 damage",
            weapon,seed,normal_hits=normalHits,hits=world.Hits,boss_kills=world.BossKills,normal_and_support_kills=world.Kills,seconds=world.SimulationTime,
            phase=game.Stage,boss_seconds=game.Boss?.BossElapsed,summoned=game.Boss?.Summoned,health=game.CurrentHealth,gold=state.Profile.Gold,
            r07_midpoint_restore=r07Restore,r07_g3_observed=r07Sides,both_boss_receipts_preserved=preservedBoth,r06_threshold_restore=r06Restore,r06_reward_restore=r06RewardRestore,r03_receipt_preserved=preservedR03,summon_mask=game.Boss?.SummonMask,r05_midpoint_restore=r05Restore,r05_kills=r05Kills,previous_normal_completion=state.Run?.PreviousNormalRound,r04_charge_state_restore=r04Restore,restored_charge_phase=restoredChargePhase,observed_charge=observedCharge,r04_kills=r04Kills,next_round=state.Run?.RoundNumber,midpoint_disk_restore=mid,reward_reload=rewardReload,reward=game.Boss?.Reward?.PolicyId,forms=game.Inventory.Forms,completion_receipt=game.Boss?.CompletionApplied??state.Run?.CompletedBoss?.CompletionApplied,save=store.Path,
            unity="NOT_RUN",mobile="NOT_RUN",balance="UNVALIDATED"},new JsonSerializerOptions{WriteIndented=true}));
        return passed?0:1;
    }
}
