using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using BibleSurvivor.Core;

namespace BibleSurvivor.Sim;

internal static class LeviticusRunner
{
    internal static int Run(AdamSession session,SaveStore store,long revision,string weapon,int seed)
    {
        var previous=store.Load()!;
        var previousReceipt=JsonSerializer.Serialize(previous.Run!.Boss);
        var candidate=session.PrepareLeviticus(LeviticusContent.Round(16));
        var passed=candidate.Profile.Gold==previous.Profile.Gold && candidate.Run!.RunId==previous.Run.RunId;
        revision=store.Commit(candidate,revision,"leviticus-entry");
        var state=store.Load()!;
        session=AdamSession.Resume(state,LeviticusContent.Round(16));
        var records=new List<object>();
        string? sixthReceipt=null;
        for(var round=16;round<=LeviticusContent.LastInstalledRound;round++)
        {
            var definition=LeviticusContent.Round(round);
            var speeds=LeviticusContent.SpeedsFor(round);
            if(round>16)session=session.StartNextRound(definition);
            var world=new BattleWorld(session,speeds,seed);
            var phases=new HashSet<string>();
            var midpoint=false;
            for(var tick=0;tick<(round==18?36000:10860)&&session.IsActive;tick++)
            {
                BattleRunner.Resolve(session,weapon);
                world.Advance(BattleWorld.FixedStep,BattleRunner.Movement(world));
                BattleRunner.Resolve(session,weapon);
                if(!session.IsActive)break;
                var phase=!midpoint&&world.SimulationTime>=60?"midpoint":
                    session.Stage=="boss"&&session.Boss?.Telegraphing==true?"telegraph":
                    session.Stage=="boss"&&world.HostileProjectiles.Count>0?"projectiles":null;
                if(phase==null||!phases.Add(phase))continue;
                session.Capture();
                state.Run!.World=world.Capture();
                var geometry=JsonSerializer.Serialize(state.Run.World);
                revision=store.Commit(state,revision,$"leviticus-{round}-{phase}");
                state=store.Load()!;
                session=AdamSession.Resume(state,definition);
                world=BattleWorld.Restore(session,speeds,state.Run!.World!);
                passed&=geometry==JsonSerializer.Serialize(world.Capture());
                midpoint|=phase=="midpoint";
            }
            if(round==18&&world.BossKills==1)
            {
                if(session.Stage=="boss_reward")
                {
                    var choice=session.BossChoices.FirstOrDefault(x=>x.Kind=="evolution")??session.BossChoices.First();
                    passed&=session.ChooseBossReward(session.Boss!.Reward!.RewardId,choice.Id);
                }
                BattleRunner.Resolve(session,weapon);
                passed&=session.CompleteBossSettlement();
                sixthReceipt=JsonSerializer.Serialize(state.Run!.Boss);
            }

            records.Add(new {round,world.Hits,world.Kills,world.BossKills,world.SimulationTime,phases});
            passed&=midpoint&&world.Hits>0&&world.Kills>0&&session.Stage=="between_rounds"&&
                (round!=18||world.BossKills==1&&phases.Contains("telegraph")&&phases.Contains("projectiles"));
            if(!passed)break;
            session.Capture();
            revision=store.Commit(state,revision,"leviticus-complete-"+round);
            state=store.Load()!;
            session=AdamSession.Resume(state,definition);
            passed&=state.Run!.RoundNumber==round+1&&previousReceipt==JsonSerializer.Serialize(state.Run.CompletedFifthBoss)&&
                (round<=18||sixthReceipt==JsonSerializer.Serialize(state.Run.CompletedSixthBoss))&&
                session.ContentComplete==(round==LeviticusContent.LastInstalledRound);
            if(!passed)break;
        }
        System.Console.WriteLine(JsonSerializer.Serialize(new
        {
            status=passed?"PASS":"FAIL", kind="Leviticus R16–R20 coordinate combat, technical equipment/health fixture",
            records, content_complete=session.ContentComplete, next_round=state.Run?.RoundNumber
        }));
        return passed?0:1;
    }
}
