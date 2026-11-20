using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using BibleSurvivor.Core;

namespace BibleSurvivor.Sim;

internal static class LaterCampaignRunner
{
    internal static int Run(AdamSession session,SaveStore store,long revision,string weapon,int seed,int lastRound)
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
        var laterReceipts=new List<string>();
        for(var round=16;round<=lastRound;round++)
        {
            var definition=CampaignContent.Round(round);
            var speeds=CampaignContent.SpeedsFor(round);
            if(round is NumbersContent.GlobalRoundNumber or DeuteronomyContent.GlobalRoundNumber or JoshuaContent.GlobalRoundNumber or JudgesContent.GlobalRoundNumber or
                RuthContent.GlobalRoundNumber or FirstSamuelContent.GlobalRoundNumber || LaterCampaignBooks.StartsBook(round))
            {
                var before=state.Profile.Gold;
                var (completed,entering)=round switch
                {
                    NumbersContent.GlobalRoundNumber=>(session.LeviticusComplete,session.PrepareNumbers(definition)),
                    DeuteronomyContent.GlobalRoundNumber=>(session.NumbersComplete,session.PrepareDeuteronomy(definition)),
                    JoshuaContent.GlobalRoundNumber=>(session.DeuteronomyComplete,session.PrepareJoshua(definition)),
                    JudgesContent.GlobalRoundNumber=>(session.JoshuaComplete,session.PrepareJudges(definition)),
                    RuthContent.GlobalRoundNumber=>(session.JudgesComplete,session.PrepareRuth(definition)),
                    FirstSamuelContent.GlobalRoundNumber=>(session.RuthComplete,session.PrepareFirstSamuel(definition)),
                    _=>(session.CanEnterLaterBook,session.PrepareLaterBook(definition))
                };
                passed&=completed&&entering.Profile.Gold==before;
                revision=store.Commit(entering,revision,"book-entry-"+round);
                state=store.Load()!;
                session=AdamSession.Resume(state,definition);
            }
            else if(round>16)session=session.StartNextRound(definition);
            var boss=session.BossRules;
            var hasBoss=boss!=null;
            var world=new BattleWorld(session,speeds,seed);
            var phases=new HashSet<string>();
            var midpoint=false;
            var waveHits=0;
            // Allow late high-health bosses enough fixed steps for the technical equipment fixture.
            var maxTicks=hasBoss?(round>70?120000:48000):10860;
            for(var tick=0;tick<maxTicks&&session.IsActive;tick++)
            {
                BattleRunner.Resolve(session,weapon);
                world.Advance(BattleWorld.FixedStep,BattleRunner.Movement(world));
                if(session.Stage=="normal")waveHits=world.Hits;
                BattleRunner.Resolve(session,weapon);
                if(!session.IsActive)break;
                var phase=!midpoint&&world.SimulationTime>=60?"midpoint":
                    session.Stage=="boss"&&session.Boss?.Telegraphing==true?"telegraph":
                    session.Stage=="boss"&&session.Boss?.ChariotCharge?.Phase=="dash"?"dash":
                    session.Stage=="boss"&&world.HostileProjectiles.Count>0&& !phases.Contains("projectiles")?"projectiles":
                    round>=LaterCampaignBooks.FirstRound&&session.Stage=="boss"&&session.Boss?.ThresholdUsed==true?"support":null;
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
            if(hasBoss&&world.BossKills==1)
            {
                if(session.Stage=="boss_reward")
                {
                    var choice=session.BossChoices.FirstOrDefault(x=>x.Kind=="evolution")??session.BossChoices.First();
                    passed&=session.ChooseBossReward(session.Boss!.Reward!.RewardId,choice.Id);
                }
                BattleRunner.Resolve(session,weapon);
                passed&=session.CompleteBossSettlement();
                if(round==18)sixthReceipt=JsonSerializer.Serialize(state.Run!.Boss);
                else laterReceipts.Add(JsonSerializer.Serialize(state.Run!.Boss));
            }

            records.Add(new {round,world.Hits,world.Kills,world.BossKills,world.SimulationTime,phases,waveHits});
            // Late waves may be cleared by survival. Require actual hits during the wave, then prove the boss separately.
            var waveEngaged=round<=80?world.Hits>0&&world.Kills>0:waveHits>0;
            passed&=midpoint&&waveEngaged&&session.Stage=="between_rounds"&&
                (boss==null||world.BossKills==1&&
                    (!(boss.UsesDash||boss.UsesSlam||boss.UsesChainProjectiles)||phases.Contains("telegraph"))&&
                    (!boss.UsesChainProjectiles||phases.Contains("projectiles"))&&
                    (!boss.UsesDash||phases.Contains("dash"))&&
                    (round<LaterCampaignBooks.FirstRound||!boss.SummonsSupport||phases.Contains("support")));
            if(!passed)break;
            session.Capture();
            revision=store.Commit(state,revision,"leviticus-complete-"+round);
            state=store.Load()!;
            session=AdamSession.Resume(state,definition);
            passed&=state.Run!.RoundNumber==round+1&&previousReceipt==JsonSerializer.Serialize(state.Run.CompletedFifthBoss)&&
                (round<=18||sixthReceipt==JsonSerializer.Serialize(state.Run.CompletedSixthBoss))&&
                session.ContentComplete==(round==CampaignContent.LastInstalledRound)&&
                session.NumbersComplete==(round==NumbersContent.LastRound)&&session.DeuteronomyComplete==(round==DeuteronomyContent.LastRound)&&
                session.JoshuaComplete==(round==JoshuaContent.LastRound)&&session.JudgesComplete==(round==JudgesContent.LastRound)&&
                session.RuthComplete==(round==RuthContent.LastRound);
            if(round>=NumbersContent.GlobalRoundNumber)
            {
                // The current boss stays in Run.Boss until the next round starts.
                var expected=round%3==0?laterReceipts.Count-1:laterReceipts.Count;
                passed&=state.Run.LaterBossHistory!=null&&
                    laterReceipts.Take(expected).SequenceEqual(state.Run.LaterBossHistory.Select(r=>JsonSerializer.Serialize(r)));
            }
            if(!passed)break;
        }
        System.Console.WriteLine(JsonSerializer.Serialize(new
        {
            status=passed?"PASS":"FAIL", kind=$"Campaign R16–R{lastRound} coordinate combat, technical equipment/health fixture",
            records, content_complete=session.ContentComplete, next_round=state.Run?.RoundNumber
        }));
        return passed?0:1;
    }
}
