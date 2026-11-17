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
internal static class RoundSixTests
{
    private const string Catalog="content/genesis/catalog.json";
    private static readonly IReadOnlyDictionary<string,double> Speeds=CatalogReader.MonsterSpeeds(Catalog);
    internal static (GameSnapshot State,AdamSession Game) Start()
    {var (state,game)=RoundFiveTests.Start();game.Advance(180);return(state,game.StartNextRound(CatalogReader.Round(Catalog,6)));}
    private static (GameSnapshot State,AdamSession Game,BattleWorld World) Boss()
    {var (state,game)=Start();game.Advance(180);var world=new BattleWorld(game,Speeds);world.Advance(2,Vector2.Zero);return(state,game,world);}
    private static BattleWorld Place(AdamSession game,BattleWorld world,Vector2 actor)
    {
        var snapshot=world.Capture();var boss=snapshot.Enemies.Single(x=>x.Id==781);boss.X=actor.X;boss.Y=actor.Y;
        foreach(var id in game.Inventory.Weapons.Keys)if(!snapshot.Weapons.Any(x=>x.WeaponId==id))snapshot.Weapons.Add(new BattleWeaponState{WeaponId=id,WeaponForm=game.Inventory.Forms.TryGetValue(id,out var form)?form:null,NextCycle=1000});
        return BattleWorld.Restore(game,Speeds,snapshot);
    }
    private static void Store(Action<SaveStore> action)
    {var folder=Path.Combine(Path.GetTempPath(),"r06-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(folder);try{action(new SaveStore(Path.Combine(folder,"save.json")));}finally{Directory.Delete(folder,true);}}
    public static void Register(Action<string,Action> test)
    {
        test("R06 catalog intro preserves prior R03 receipt no early heal gold or unlock and distinct boss rules",()=>{
            var definition=CatalogReader.Round(Catalog,6);Check.Equal(780,definition.Waves.Sum(w=>w.Groups.Sum(g=>g.Count)));Check.Equal(2400,definition.RawExperience);Check.Equal(90,definition.ConcurrencyCap);Check.Equal(100,definition.CompletionGold);Check.Equal("cross",definition.UnlockWeaponId);
            var (state,game)=Start();var history=JsonSerializer.Serialize(state.Run!.CompletedBoss);game.Advance(10);game.Damage(20);var hp=game.CurrentHealth;var gold=state.Profile.Gold;game.Advance(180);
            Check.Equal("boss_intro",game.Stage);Check.Near(hp,game.CurrentHealth);Check.Equal(gold,state.Profile.Gold);Check.True(!state.Profile.UnlockedWeapons.Contains("cross"));
            Check.Equal(6,game.Boss!.EncounterRound);Check.Equal(6500d,game.Boss.Health);Check.Equal(781,game.BossRules!.BossActorId);Check.Equal(7d,game.Boss.NextAttackAt);Check.Equal(5,state.Run.PreviousNormalRound);
            Check.Equal(history,JsonSerializer.Serialize(state.Run.CompletedBoss));game.Damage(10000);game.Hit(781,10000);Check.Near(hp,game.CurrentHealth);Check.Near(6500,game.Boss.Health);
            var world=new BattleWorld(game,Speeds);world.Advance(2,Vector2.Zero);Check.Equal("boss",game.Stage);Check.Equal(0,world.ShotsFired);Check.Near(182,world.SimulationTime);state.Validate();
            Check.Equal(BossDefinition.EncounterId(state.Run.RunId),state.Run.CompletedBoss!.EncounterId);Check.Equal(BossDefinition.Policy,state.Run.CompletedBoss.Reward!.PolicyId);Check.Equal(0,state.Run.CompletedBoss.RoundNumber);Check.Equal(0,state.Run.CompletedBoss.SummonMask);
        });
        test("R06 seven-second fixed point attack stops tracking target never chases and strikes once",()=>{
            var (state,game,world)=Boss();world=Place(game,world,new Vector2(30,0));world.Advance(5.8,Vector2.Zero);Check.True(game.Boss!.Telegraphing);
            var actor=world.EnemyPositions[781];var hp=game.CurrentHealth;world.Advance(1,Vector2.UnitY);Check.Equal(actor,world.EnemyPositions[781]);Check.Near(0,game.Boss.TargetY);
            world.Advance(.2,Vector2.Zero);Check.Near(hp,game.CurrentHealth);Check.Near(14,game.Boss.NextAttackAt);Check.True(!game.Boss.Telegraphing);
            var snapshot=world.Capture();snapshot.PlayerX=snapshot.PlayerY=0;world=BattleWorld.Restore(game,Speeds,snapshot);world.Advance(7,Vector2.Zero);Check.Near(hp-18,game.CurrentHealth);
            world.Advance(.2,Vector2.Zero);Check.Near(hp-18,game.CurrentHealth);Check.Equal(0,world.Capture().Projectiles.Count);
        });
        test("R06 both thresholds summon F1 six each once cap12 restore mask and lethal hit skips summons",()=>Store(store=>{
            var actual=Boss();actual.Game.Hit(781,1949);var geometry=actual.World.Capture();var actor=geometry.Enemies.Single(x=>x.Id==781);actor.X=.8f;actor.Y=0;
            actual.World=BattleWorld.Restore(actual.Game,Speeds,geometry);actual.World.Advance(BattleWorld.FixedStep,Vector2.Zero);
            Check.Equal(1,actual.Game.Boss!.SummonMask);Check.Equal(7,actual.World.EnemyPositions.Count);actual.State.Run!.World=actual.World.Capture();actual.State.Validate();
            var (state,game,world)=Boss();game.Hit(781,2000);Check.Equal(1,game.Boss!.SummonMask);Check.Equal(6,game.Boss.Supports.Count);Check.Equal("f1",game.Boss.Supports[0].MonsterId);
            world= new BattleWorld(game,Speeds);game.Capture();state.Run!.World=world.Capture();store.Commit(state,0,"threshold70");var loaded=store.Load()!;game=AdamSession.Resume(loaded,CatalogReader.Round(Catalog,6));game.Hit(781,2300);
            Check.Equal(3,game.Boss!.SummonMask);Check.Equal(12,game.Boss.Summoned);Check.Equal(12,game.Boss.Supports.Count);game.Hit(781,1);Check.Equal(12,game.Boss.Summoned);Check.Equal(794,game.Boss.NextActorId);
            var skip=Boss();skip.Game.Hit(781,4500);Check.Equal(3,skip.Game.Boss!.SummonMask);Check.Equal(12,skip.Game.Boss.Summoned);
            var lethal=Boss();lethal.Game.Hit(781,6500);Check.Equal(0,lethal.Game.Boss!.SummonMask);Check.Equal(0,lethal.Game.Boss.Summoned);Check.Equal("boss_reward",lethal.Game.Stage);
        }));
        test("R06 threshold telegraph checkpoint replays exact World and prior receipt without boss timeout",()=>Store(store=>{
            var (state,game,world)=Boss();game.Hit(781,2000);world=new BattleWorld(game,Speeds);world=Place(game,world,new Vector2(30,0));world.Advance(6,Vector2.UnitY);
            var old=JsonSerializer.Serialize(state.Run!.CompletedBoss);game.Capture();state.Run.World=world.Capture();store.Commit(state,0,"boss-mid");var loaded=store.Load()!;
            var resumed=AdamSession.Resume(loaded,CatalogReader.Round(Catalog,6));var replay=BattleWorld.Restore(resumed,Speeds,loaded.Run!.World!);
            world.Advance(1.5,Vector2.UnitX);replay.Advance(1.5,Vector2.UnitX);Check.Equal(JsonSerializer.Serialize(world.Capture()),JsonSerializer.Serialize(replay.Capture()));Check.Equal(old,JsonSerializer.Serialize(loaded.Run.CompletedBoss));
            // A distant moving technical fixture survives past the normal clock without completion.
            for(var i=0;i<6;i++)world.Advance(30,Vector2.UnitX);Check.Equal("boss",game.Stage);Check.True(game.Boss!.BossElapsed>180);
        }));
        test("R06 fixed200 and reward evolution settlement100 once coexist with immutable R03 history",()=>Store(store=>{
            var (state,game)=Start();game.Inventory.TryChoose("bible",false);while(game.Inventory.TryChoose("bible",false)){}game.Inventory.TryChoose("scroll",true);game.Capture();var old=JsonSerializer.Serialize(state.Run!.CompletedBoss);
            game.Advance(180);game.Advance(2);game.Hit(781,2000);foreach(var id in game.Boss!.Supports.Take(4).Select(x=>x.Id).ToArray())game.Kill(id);
            var gold=state.Profile.Gold;var remainder=state.Run.KillRemainder;game.Hit(781,6500);Check.Equal(gold+200,state.Profile.Gold);Check.Equal(remainder,state.Run.KillRemainder);Check.Equal(BossDefinition.SitnahPolicy,game.Boss!.Reward!.PolicyId);Check.True(!game.NeedsRewardImplementation);
            store.Commit(state,0,"reward");var loaded=store.Load()!;game=AdamSession.Resume(loaded,CatalogReader.Round(Catalog,6));var candidate=game.BossChoices.Single(x=>x.Kind=="evolution");Check.Equal(EvolutionRules.Bible,candidate.ItemId);
            loaded.Run!.PendingLevels=1;loaded.Run.Choices=game.Inventory.Choices(loaded.Profile.UnlockedWeapons,new[]{"hair"},3,loaded.Run.ChoiceSerial).ToList();loaded.Run.Health=37;
            Check.True(game.ChooseBossReward(game.Boss!.Reward!.RewardId,candidate.Id));Check.True(!game.ChooseBossReward(game.Boss!.Reward!.RewardId,candidate.Id));Check.True(!game.CompleteBossSettlement());RoundTransitionTests.Resolve(game);Check.True(game.CompleteBossSettlement());
            Check.Equal(gold+300,loaded.Profile.Gold);Check.Equal(7,loaded.Run.RoundNumber);Check.Near(loaded.Run.MaxHealth,game.CurrentHealth);Check.True(loaded.Profile.UnlockedWeapons.Contains("cross"));Check.True(!game.Inventory.TryChoose("cross",false));Check.True(!game.CompleteBossSettlement());
            Check.Equal(old,JsonSerializer.Serialize(loaded.Run.CompletedBoss));Check.Equal(6,loaded.Run.Boss!.EncounterRound);Check.True(loaded.Run.Boss.CompletionApplied);Check.Equal(EvolutionRules.Sling,game.Inventory.Forms["sling"]);Check.Equal(EvolutionRules.Bible,game.Inventory.Forms["bible"]);
            store.Commit(loaded,1,"completed");store.Commit(loaded,1,"completed");loaded=store.Load()!;game=AdamSession.Resume(loaded,CatalogReader.Round(Catalog,6));Check.True(!game.CompleteBossSettlement());Check.Equal(old,JsonSerializer.Serialize(loaded.Run!.CompletedBoss));Check.Throws<ArgumentOutOfRangeException>(()=>CatalogReader.Round(Catalog,10));
        }));
        test("R06 regen pause shared iframe and lethal player damage precede boss credit",()=>{
            var (state,game)=Start();state.Run!.PermanentSnapshot["health"]=2;state.Run.PermanentSnapshot["armor"]=1;state.Run.PermanentSnapshot["regen"]=1;game.Advance(180);game.Advance(2);
            var world=Place(game,new BattleWorld(game,Speeds),new Vector2(30,0));game.Damage(10);var hp=game.CurrentHealth;world.Advance(1,Vector2.UnitY);Check.Near(hp+.1,game.CurrentHealth);game.Pause(true);hp=game.CurrentHealth;world.Advance(1,Vector2.Zero);Check.Near(hp,game.CurrentHealth);game.Pause(false);
            var snapshot=world.Capture();var actor=snapshot.Enemies.Single(x=>x.Id==781);actor.X=snapshot.PlayerX;actor.Y=snapshot.PlayerY;world=BattleWorld.Restore(game,Speeds,snapshot);
            state.Run.Health=1;state.Run.LastHitTime=-1;game.Hit(781,6499);var gold=state.Profile.Gold;world.Advance(BattleWorld.FixedStep,Vector2.Zero);Check.True(state.Run==null);Check.Equal(gold,state.Profile.Gold);Check.Equal(0,world.BossKills);
        });
        test("R06 future encounter policy mask field and swapped historical identity protect original files",()=>Store(store=>{
            var (state,game,world)=Boss();game.Capture();state.Run!.World=world.Capture();store.Commit(state,0,"boss");var original=File.ReadAllText(store.Path);File.WriteAllText(store.Path+".bak",original);
            foreach(var change in new Action<JsonNode>[] {n=>n["Run"]!["Boss"]!["RoundNumber"]=12,n=>n["Run"]!["Boss"]!["EncounterId"]="run:genesis_06:future_boss",n=>n["Run"]!["Boss"]!["NewMask"]=1,n=>n["Run"]!["Boss"]!["Reward"]=new JsonObject{["PolicyId"]="future"}})
            {var node=JsonNode.Parse(original)!;change(node);var future=node.ToJsonString();File.WriteAllText(store.Path,future);Check.Throws<UnsupportedSaveVersionException>(()=>store.Load());Check.Throws<UnsupportedSaveVersionException>(()=>store.Commit(GameSnapshot.New(),1,"overwrite"));Check.Equal(future,File.ReadAllText(store.Path));Check.Equal(original,File.ReadAllText(store.Path+".bak"));}
            state.Run.Boss!.SummonMask=2;Check.Throws<InvalidDataException>(()=>state.Validate());state.Run.Boss.SummonMask=0;state.Run.CompletedBoss=state.Run.Boss;Check.Throws<InvalidDataException>(()=>state.Validate());
        }));
    }
}
