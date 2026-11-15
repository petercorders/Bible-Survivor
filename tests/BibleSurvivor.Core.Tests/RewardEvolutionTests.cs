using System;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Nodes;
using BibleSurvivor.Core;
using BibleSurvivor.Sim;

namespace BibleSurvivor.Core.Tests;
internal static class RewardEvolutionTests
{
    private const string Catalog="content/genesis/catalog.json";
    private static readonly IReadOnlyDictionary<string,double> Speeds=CatalogReader.MonsterSpeeds(Catalog);
    private static (GameSnapshot State,AdamSession Game) Prepared(string weapon="sling",bool evolve=true)
    {
        var (state,game)=BossTests.Start();
        if(!game.Inventory.Weapons.ContainsKey(weapon))Check.True(game.Inventory.TryChoose(weapon,false));
        while(game.Inventory.Weapons[weapon]<8)Check.True(game.Inventory.TryChoose(weapon,false));
        if(evolve)Check.True(game.Inventory.TryChoose(weapon=="bible"?"scroll":weapon=="jawbone"?"hair":"stones",true));
        game.Capture();return(state,game);
    }
    private static (GameSnapshot State,AdamSession Game) Reward(string weapon="sling",bool evolve=true)
    {
        var (state,game)=Prepared(weapon,evolve);game.Advance(180);game.Advance(2);game.Hit(481,1800);return(state,game);
    }
    private static void Store(Action<SaveStore> action)
    {
        var path=Path.Combine(Path.GetTempPath(),"r03-reward-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(path);
        try{action(new SaveStore(Path.Combine(path,"save.json")));}finally{Directory.Delete(path,true);}
    }
    private static BattleWorld Place(AdamSession game,BattleWorld world,Func<int,Vector2> position)
    {
        var s=world.Capture();foreach(var e in s.Enemies){var p=position(e.Id);e.X=p.X;e.Y=p.Y;}
        return BattleWorld.Restore(game,Speeds,s);
    }
    public static void Register(Action<string,Action> test)
    {
        test("R03 production reward guarantees evolution stable unique candidates save and single apply",()=>Store(store=>{
            var (state,game)=Reward();var candidates=game.BossChoices;Check.True(candidates.Count<=3&&candidates.Count>0);
            Check.Equal(candidates.Count,candidates.Select(x=>x.Id).Distinct().Count());var evo=candidates.Single(x=>x.Kind=="evolution");
            Check.Equal(EvolutionRules.Sling,evo.ItemId);Check.True(!game.NeedsRewardImplementation);var serial=state.Run!.ChoiceSerial;
            store.Commit(state,0,"reward");var loaded=store.Load()!;var resumed=AdamSession.Resume(loaded,CatalogReader.Round(Catalog,3));
            Check.Equal(JsonSerializer.Serialize(candidates),JsonSerializer.Serialize(resumed.BossChoices));
            Check.True(!resumed.ChooseBossReward("stale",evo.Id));Check.True(resumed.ChooseBossReward(resumed.Boss!.Reward!.RewardId,evo.Id));
            Check.True(!resumed.ChooseBossReward(resumed.Boss.Reward.RewardId,evo.Id));Check.Equal("boss_settlement",resumed.Stage);
            Check.Equal(8,resumed.Inventory.Weapons["sling"]);Check.Equal(EvolutionRules.Sling,resumed.Inventory.Forms["sling"]);Check.Equal(1,resumed.Inventory.Passives["stones"]);
            Check.Equal(serial,loaded.Run!.ChoiceSerial);Check.True(!resumed.Inventory.TryChoose("sling",false));Check.True(!resumed.Inventory.TryEvolve(EvolutionRules.Sling));
            Check.True(!resumed.Inventory.Choices(loaded.Profile.UnlockedWeapons,Array.Empty<string>(),3,1).Contains("sling"));
            store.Commit(loaded,1,"evolve");loaded=store.Load()!;resumed=AdamSession.Resume(loaded,CatalogReader.Round(Catalog,3));
            Check.Equal(EvolutionRules.Sling,resumed.Inventory.Forms["sling"]);loaded.Validate();
        }));
        test("R03 explicit pending-v1 promotion preserves eligibility and receipt without silent reroll",()=>Store(store=>{
            var (state,game)=Reward();var reward=state.Run!.Boss!.Reward!;reward.PolicyId=BossDefinition.LegacyPolicy;reward.Candidates=null;reward.UnlockedWeapons=null;
            state.Run.Boss.Version=1;state.Run.RoundFingerprint=CatalogReader.Round(Catalog,3).PreviousBossFingerprint;
            var fingerprint=state.Run.RoundFingerprint;store.Commit(state,0,"old-pending");var loaded=store.Load()!;game=AdamSession.Resume(loaded,CatalogReader.Round(Catalog,3));
            game.Capture();Check.Equal(fingerprint,loaded.Run!.RoundFingerprint);Check.Equal(0,game.BossChoices.Count);Check.True(game.NeedsRewardImplementation);
            var rewardId=game.Boss!.Reward!.RewardId;Check.True(game.PromoteBossReward());Check.True(!game.PromoteBossReward());
            Check.Equal(BossDefinition.LegacyPolicy,game.Boss!.Reward!.PromotedFrom);Check.Equal(rewardId,game.Boss.Reward.RewardId);
            Check.True(game.BossChoices.Any(x=>x.Kind=="evolution"));Check.Equal(CatalogReader.Round(Catalog,3).Fingerprint,loaded.Run.RoundFingerprint);
            store.Commit(loaded,1,"promote");var choices=JsonSerializer.Serialize(game.BossChoices);loaded=store.Load()!;
            game=AdamSession.Resume(loaded,CatalogReader.Round(Catalog,3));Check.Equal(choices,JsonSerializer.Serialize(game.BossChoices));Check.True(!game.PromoteBossReward());
        }));
        test("R03 missing passive or weapon8 never invents evolution and stale inventory choice is refused",()=>{
            var (state,game)=Reward("sling",false);Check.True(game.BossChoices.All(x=>x.Kind!="evolution"));
            var candidate=game.BossChoices.First();Check.True(game.Inventory.TryChoose("hair",true));
            Check.True(!game.ChooseBossReward(game.Boss!.Reward!.RewardId,candidate.Id));
            var other=BossTests.Start();other.Game.Inventory.TryChoose("stones",true);other.Game.Advance(180);other.Game.Advance(2);other.Game.Hit(481,1800);
            Check.True(other.Game.BossChoices.All(x=>x.Kind!="evolution"));
        });
        test("R03 item reward grants exactly one level and scarce growth adds only one gold fallback",()=>{
            var (state,game)=BossTests.Start();game.Advance(180);game.Advance(2);game.Hit(481,1800);
            var item=game.BossChoices.First(x=>x.Kind=="item");Check.True(game.ChooseBossReward(game.Boss!.Reward!.RewardId,item.Id));
            var actual=game.Inventory.Weapons.TryGetValue(item.ItemId,out var level)?level:game.Inventory.Passives[item.ItemId];Check.Equal(item.Level,actual);
            Check.True(!game.ChooseBossReward(game.Boss.Reward.RewardId,item.Id));state.Validate();
            var other=Prepared("sling",false);
            foreach(var weapon in new[]{"bible","jawbone"}){other.Game.Inventory.TryChoose(weapon,false);while(other.Game.Inventory.TryChoose(weapon,false)){} }
            foreach(var passive in new[]{"east_wind","seal","cloud","thunder","crown","underwear"})while(other.Game.Inventory.TryChoose(passive,true)){}
            other.Game.Capture();other.State.Run!.Weapons["sling"]=7;other.Game=AdamSession.Resume(other.State,CatalogReader.Round(Catalog,3));
            other.Game.Advance(180);other.Game.Advance(2);other.Game.Hit(481,1800);
            Check.Equal(2,other.Game.BossChoices.Count);Check.Equal(1,other.Game.BossChoices.Count(x=>x.Kind=="gold"));
            Check.Equal(8,other.Game.BossChoices.Single(x=>x.Kind=="item").Level);
        });
        test("R03 gold fallback is one candidate and gold-only auto applies once separately from completion",()=>Store(store=>{
            var (state,game)=Prepared();
            foreach(var weapon in new[]{"bible","jawbone"}){game.Inventory.TryChoose(weapon,false);while(game.Inventory.Weapons[weapon]<8)game.Inventory.TryChoose(weapon,false);}
            foreach(var passive in new[]{"stones","east_wind","seal","cloud","thunder","crown"})
            {if(!game.Inventory.Passives.ContainsKey(passive))game.Inventory.TryChoose(passive,true);while(game.Inventory.TryChoose(passive,true)){} }
            Check.True(game.Inventory.TryEvolve(EvolutionRules.Sling));game.Capture();game.Advance(180);game.Advance(2);var gold=state.Profile.Gold;game.Hit(481,1800);
            Check.Equal("boss_settlement",game.Stage);Check.Equal(gold+200,state.Profile.Gold);Check.Equal(1,game.BossChoices.Count);Check.Equal("gold",game.BossChoices[0].Kind);
            Check.True(game.Boss!.Reward!.GoldApplied);Check.True(!game.ChooseBossReward(game.Boss.Reward.RewardId,game.BossChoices[0].Id));
            store.Commit(state,0,"gold-only");var loaded=store.Load()!;game=AdamSession.Resume(loaded,CatalogReader.Round(Catalog,3));Check.True(game.CompleteBossSettlement());
            Check.Equal(gold+250,loaded.Profile.Gold);Check.True(!game.CompleteBossSettlement());
        }));
        test("R03 settlement waits for levels heals unlocks and pays fifty once through failed and uncertain commits",()=>Store(store=>{
            var (state,game)=Reward();state.Run!.PendingLevels=1;state.Run.Choices=game.Inventory.Choices(state.Profile.UnlockedWeapons,new[]{"hair"},3,state.Run.ChoiceSerial).ToList();
            state.Run.Health=37;var gold=state.Profile.Gold;var candidate=game.BossChoices.First(x=>x.Kind=="evolution");
            Check.True(game.ChooseBossReward(game.Boss!.Reward!.RewardId,candidate.Id));Check.True(!game.CompleteBossSettlement());Check.Near(37,game.CurrentHealth);
            Check.True(!state.Profile.UnlockedWeapons.Contains("moses_staff"));Check.True(game.PendingChoices.Count>0);store.Commit(state,0,"settlement-pending");
            var loaded=store.Load()!;game=AdamSession.Resume(loaded,CatalogReader.Round(Catalog,3));Check.True(game.Choose(game.PendingChoices[0]));
            Check.True(game.CompleteBossSettlement());Check.Equal(4,loaded.Run!.RoundNumber);Check.Near(loaded.Run.MaxHealth,game.CurrentHealth);Check.Equal(gold+50,loaded.Profile.Gold);
            Check.Equal(1,loaded.Profile.UnlockedWeapons.Count(x=>x=="moses_staff"));Check.True(!game.CompleteBossSettlement());
            var broken=new SaveStore(store.Path,()=>throw new IOException("replace fault"));Check.Throws<IOException>(()=>broken.Commit(loaded,1,"complete"));
            Check.Equal("boss_settlement",store.Load()!.Run!.Stage);store.Commit(loaded,1,"complete");store.Commit(loaded,1,"complete");
            loaded=store.Load()!;game=AdamSession.Resume(loaded,CatalogReader.Round(Catalog,3));Check.True(!game.IsActive);Check.True(!game.CompleteBossSettlement());
            Check.Equal(gold+50,loaded.Profile.Gold);Check.Throws<InvalidOperationException>(()=>game.StartNextRound(new RoundDefinition(5,60,1100,50,CatalogReader.Round(Catalog,3).Waves)));
        }));
        test("R03 Bible sanctuary preserves max base stats continuous orbit pulse hit ledger and disk replay",()=>Store(store=>{
            var (state,game)=Prepared("bible");Check.True(game.Inventory.TryEvolve(EvolutionRules.Bible));game.Capture();game.Advance(180);game.Advance(2);
            var basic=CombatStats.For("bible",8,new CombatBonuses());var profile=game.AttackProfile("bible");Check.Near(basic.Damage,profile.Damage);Check.Equal(basic.Amount,profile.Amount);
            Check.Near(basic.AreaMultiplier,profile.AreaMultiplier);Check.Near(profile.Duration,profile.Cycle);
            var world=Place(game,new BattleWorld(game,Speeds),_=>new Vector2(2.7f,0));world.Advance(.1,Vector2.Zero);Check.True(world.Hits>0);
            var impacts=new List<BattleImpact>();world.ReadImpacts(0,impacts);Check.True(impacts.Any(x=>x.Cause=="bible"&&x.WeaponForm==EvolutionRules.Bible));
            var clock=world.Capture().Weapons.Single(x=>x.WeaponId=="bible");Check.True(clock.PulseHitIds!.Contains(481));var hp=game.Boss!.Health;
            world.Advance(.1,Vector2.Zero);Check.Near(hp,game.Boss.Health);game.Capture();state.Run!.World=world.Capture();store.Commit(state,0,"pulse-mid");
            var loaded=store.Load()!;var resumed=AdamSession.Resume(loaded,CatalogReader.Round(Catalog,3));var replay=BattleWorld.Restore(resumed,Speeds,loaded.Run!.World!);
            world.Advance(.5,Vector2.UnitX);replay.Advance(.5,Vector2.UnitX);Check.Equal(JsonSerializer.Serialize(world.Capture()),JsonSerializer.Serialize(replay.Capture()));
        }));
        test("R03 jawbone thousand keeps bursts and applies actual one-unit knockback to boss",()=>Store(store=>{
            var (state,game)=Prepared("jawbone");Check.True(game.Inventory.TryEvolve(EvolutionRules.Jawbone));game.Capture();game.Advance(180);game.Advance(2);
            var basic=CombatStats.For("jawbone",8,new CombatBonuses{DamageBonus=.1});var profile=game.AttackProfile("jawbone");Check.Near(basic.Damage,profile.Damage);
            Check.Equal(basic.Amount,profile.Amount);Check.Near(.15,profile.BurstInterval);Check.Near(basic.AreaMultiplier+.25,profile.AreaMultiplier);
            var world=Place(game,new BattleWorld(game,Speeds),_=>new Vector2(1.8f,0));world.Advance(BattleWorld.FixedStep,Vector2.Zero);
            Check.True(game.Boss!.Health<1800);Check.True(world.EnemyPositions[481].X>2.7);Check.Equal(1,world.Hits);
            game.Capture();state.Run!.World=world.Capture();store.Commit(state,0,"knockback");var loaded=store.Load()!;
            var restored=BattleWorld.Restore(AdamSession.Resume(loaded,CatalogReader.Round(Catalog,3)),Speeds,loaded.Run!.World!);Check.Equal(world.EnemyPositions[481],restored.EnemyPositions[481]);
            world.Advance(.15,Vector2.Zero);restored.Advance(.15,Vector2.Zero);Check.True(world.Hits>=2);Check.True(world.EnemyPositions[481].X>3.5);
            Check.Equal(JsonSerializer.Serialize(world.Capture()),JsonSerializer.Serialize(restored.Capture()));
        }));
        test("R03 sling fivefold preserves six shots pierce strongest max-health focus and retarget replay",()=>Store(store=>{
            var (state,game)=Prepared("sling");game.Inventory.TryChoose("stones",true);Check.True(game.Inventory.TryEvolve(EvolutionRules.Sling));game.Capture();game.Advance(180);game.Advance(2);
            game.Hit(481,901);var world=new BattleWorld(game,Speeds);world=Place(game,world,id=>id==481?new Vector2(10,0):new Vector2(0,3));
            var profile=game.AttackProfile("sling");Check.Equal(6,profile.Amount);Check.Equal(2,profile.Pierce);Check.Near(.1,profile.BurstInterval);
            world.Advance(.15,Vector2.Zero);var clock=world.Capture().Weapons.Single(x=>x.WeaponId=="sling");Check.Equal(481,clock.FocusTargetId);Check.Equal(4,clock.PendingBursts.Count);
            game.Capture();state.Run!.World=world.Capture();store.Commit(state,0,"volley-mid");var loaded=store.Load()!;var resumed=AdamSession.Resume(loaded,CatalogReader.Round(Catalog,3));
            var replay=BattleWorld.Restore(resumed,Speeds,loaded.Run!.World!);
            Check.Equal(JsonSerializer.Serialize(world.Capture()),JsonSerializer.Serialize(replay.Capture()));world.Advance(.45,Vector2.Zero);replay.Advance(.45,Vector2.Zero);
            Check.Equal(JsonSerializer.Serialize(world.Capture()),JsonSerializer.Serialize(replay.Capture()));Check.Equal(6,world.ShotsFired);
            world.Advance(1,Vector2.Zero);replay.Advance(1,Vector2.Zero);Check.True(world.Hits>0);Check.True(game.Boss!.Health<899);
            Check.Equal(JsonSerializer.Serialize(world.Capture()),JsonSerializer.Serialize(replay.Capture()));
            // Separate normal geometry fixture: C3 maxHP130 wins even after it has less current HP than C1.
            var other=Prepared("sling");other.Game.Inventory.TryEvolve(EvolutionRules.Sling);other.Game.Capture();other.Game.Advance(169);
            int c1=0,c3=0;
            for(var i=0;i<10;i++)
            {
                foreach(var target in other.Game.Targets.Values.OrderBy(x=>x.Id).ToArray())
                {
                    if(target.MonsterId=="c1"&&c1==0){c1=target.Id;continue;}
                    if(target.MonsterId=="c3"&&c3==0){c3=target.Id;continue;}
                    if(target.Id!=c1&&target.Id!=c3)other.Game.Kill(target.Id);
                }
                other.Game.Advance(.01);
            }
            Check.True(c1>0&&c3>0);other.Game.Hit(c3,129);
            // Discard injected setup drops; the subsequent aim/shot/retarget is actual World combat.
            other.State.Run!.Drops.Clear();other.Game.Capture();
            var arena=Place(other.Game,new BattleWorld(other.Game,Speeds),id=>id==c3?new Vector2(10,0):new Vector2(0,3));
            arena.Advance(.15,Vector2.Zero);Check.Equal(c3,arena.Capture().Weapons.Single(x=>x.WeaponId=="sling").FocusTargetId);
            other.Game.Kill(c3);arena.Advance(.1,Vector2.Zero);Check.Equal(c1,arena.Capture().Weapons.Single(x=>x.WeaponId=="sling").FocusTargetId);
        }));
        test("R03 reward receipt form lineage and issued target clocks reject invalid combinations",()=>{
            foreach(var change in new Action<GameSnapshot>[] {s=>s.Run!.Boss!.CompletionApplied=true,
                s=>s.Run!.Boss!.Reward!.Applied=true,s=>s.Run!.Boss!.Reward!.Candidates![0].Gold=999,
                s=>s.Run!.Forms=new Dictionary<string,string>{{"sling",EvolutionRules.Bible}}})
            {var fixture=Reward();change(fixture.State);Check.Throws<InvalidDataException>(()=>fixture.State.Validate());}
            var data=Prepared();Check.True(data.Game.Inventory.TryEvolve(EvolutionRules.Sling));data.Game.Capture();data.Game.Advance(180);data.Game.Advance(2);
            var world=new BattleWorld(data.Game,Speeds);world.Advance(.15,Vector2.UnitX);data.State.Run!.World=world.Capture();data.State.Validate();
            data.State.Run.World.Weapons.Single(x=>x.WeaponId=="sling").FocusTargetId=999;Check.Throws<InvalidDataException>(()=>data.State.Validate());
            var multi=Prepared();foreach(var weapon in new[]{"bible","jawbone"}){multi.Game.Inventory.TryChoose(weapon,false);while(multi.Game.Inventory.TryChoose(weapon,false)){} }
            multi.Game.Inventory.TryChoose("scroll",true);multi.Game.Inventory.TryChoose("hair",true);multi.Game.Capture();multi.Game.Advance(180);multi.Game.Advance(2);multi.Game.Hit(481,1800);
            Check.Equal(3,multi.Game.BossChoices.Count);Check.True(multi.Game.BossChoices.All(x=>x.Kind=="evolution"));multi.State.Validate();
        });
        test("R03 future form policy and nested candidate content preserve original primary and backup",()=>Store(store=>{
            var (state,game)=Reward();store.Commit(state,0,"reward");var original=File.ReadAllText(store.Path);File.WriteAllText(store.Path+".bak",original);
            foreach(var change in new Action<JsonNode>[] {n=>n["Run"]!["Forms"]=new JsonArray(new JsonObject{["Key"]="sling",["Value"]="future_form"}),
                n=>n["Run"]!["Boss"]!["Reward"]!["PolicyId"]="future",n=>n["Run"]!["Boss"]!["Reward"]!["Candidates"]![0]!["Kind"]="future_kind",n=>n["Run"]!["Boss"]!["Reward"]!["Candidates"]![0]!["Future"]=1})
            {var node=JsonNode.Parse(original)!;change(node);var future=node.ToJsonString();File.WriteAllText(store.Path,future);
                Check.Throws<UnsupportedSaveVersionException>(()=>store.Load());Check.Throws<UnsupportedSaveVersionException>(()=>store.Commit(GameSnapshot.New(),1,"overwrite"));
                Check.Equal(future,File.ReadAllText(store.Path));Check.Equal(original,File.ReadAllText(store.Path+".bak"));}
        }));
    }
}
