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
internal static class RoundFourTests
{
    private const string Catalog="content/genesis/catalog.json";
    private static readonly IReadOnlyDictionary<string,double> Speeds=CatalogReader.MonsterSpeeds(Catalog);
    internal static (GameSnapshot State,AdamSession Game) CompletedThree()
    {
        var (state,game)=BossTests.Start();while(game.Inventory.TryChoose("sling",false)){}game.Inventory.TryChoose("stones",true);game.Capture();
        game.Advance(180);game.Advance(2);game.Hit(481,1800);var choice=game.BossChoices.First(x=>x.Kind=="evolution");
        Check.True(game.ChooseBossReward(game.Boss!.Reward!.RewardId,choice.Id));Check.True(game.CompleteBossSettlement());return(state,game);
    }
    internal static (GameSnapshot State,AdamSession Game) Start()
    {var (state,game)=CompletedThree();return(state,game.StartNextRound(CatalogReader.Round(Catalog,4)));}
    private static (GameSnapshot State,AdamSession Game,BattleWorld World,int Id) Charger()
    {
        var (state,game)=Start();game.Advance(63);var id=0;
        // Injected setup clears the first wave backlog, not evidence of coordinate kills.
        for(var i=0;i<5;i++)
        {
            foreach(var enemy in game.Targets.Values.OrderBy(x=>x.Id).ToArray())
            {if(enemy.MonsterId=="d3"&&id==0){id=enemy.Id;continue;}if(enemy.Id!=id)game.Kill(enemy.Id);}
            game.Advance(BattleWorld.FixedStep);state.Run!.Drops.Clear();
        }
        Check.True(id>0);var world=new BattleWorld(game,Speeds);var snapshot=world.Capture();
        foreach(var enemy in snapshot.Enemies){enemy.X=enemy.Id==id?30:100;enemy.Y=0;}
        foreach(var weapon in game.Inventory.Weapons.Keys)snapshot.Weapons.Add(new BattleWeaponState{WeaponId=weapon,WeaponForm=game.Inventory.Forms.TryGetValue(weapon,out var form)?form:null,NextCycle=1000});
        world=BattleWorld.Restore(game,Speeds,snapshot);return(state,game,world,id);
    }
    private static void Store(Action<SaveStore> action)
    {var directory=Path.Combine(Path.GetTempPath(),"r04-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(directory);try{action(new SaveStore(Path.Combine(directory,"save.json")));}finally{Directory.Delete(directory,true);}}
    public static void Register(Action<string,Action> test)
    {
        test("R04 catalog budgets and entry preserve evolved gear permanent run and R03 receipts",()=>{
            var definition=CatalogReader.Round(Catalog,4);Check.Equal(600,definition.Waves.Sum(x=>x.Groups.Sum(g=>g.Count)));Check.Equal(1600,definition.RawExperience);
            Check.Equal(70,definition.ConcurrencyCap);Check.Equal(65,definition.CompletionGold);Check.Equal("fire_pillar",definition.UnlockWeaponId);
            var (state,game)=CompletedThree();var gold=state.Profile.Gold;var id=state.Run!.RunId;var receipt=game.Boss!.Reward!.SelectedId;
            game=game.StartNextRound(definition);Check.True(game.Boss==null);Check.Equal(receipt,state.Run.CompletedBoss!.Reward!.SelectedId);Check.True(state.Run.CompletedBoss.CompletionApplied);
            Check.Equal(EvolutionRules.Sling,game.Inventory.Forms["sling"]);Check.Equal(id,state.Run.RunId);Check.Equal(gold,state.Profile.Gold);Check.Near(-1,state.Run.LastHitTime);Check.Near(0,game.CombatTime);state.Validate();
        });
        test("R04 D3 first six-second telegraph locks target stops then charges 2.34 units and cooldown starts at end",()=>{
            var (state,game,world,id)=Charger();var start=world.ChargeStates[id].NextAt;world.Advance(start-world.SimulationTime,Vector2.Zero);
            Check.Equal("telegraph",world.ChargeStates[id].Phase);var position=world.EnemyPositions[id];var target=world.ChargeStates[id];Check.Near(0,target.TargetX);Check.Near(0,target.TargetY);
            world.Advance(1.2,Vector2.UnitY);Check.Equal(position,world.EnemyPositions[id]);Check.Near(0,world.ChargeStates[id].TargetY);Check.Equal("charge",world.ChargeStates[id].Phase);
            world.Advance(.6,Vector2.UnitY);Check.Near(position.X-2.34,world.EnemyPositions[id].X,.00002);Check.Near(position.Y,world.EnemyPositions[id].Y,.00002);
            Check.Equal("cooldown",world.ChargeStates[id].Phase);Check.Near(world.SimulationTime+6,world.ChargeStates[id].NextAt,1e-7);
            game.Capture();state.Run!.World=world.Capture();state.Validate();
        });
        test("R04 D3 pause selection death cleanup and fresh actor clocks do not leak",()=>{
            var (state,game,world,id)=Charger();world.Advance(world.ChargeStates[id].NextAt-world.SimulationTime,Vector2.Zero);
            var clock=world.SimulationTime;var position=world.EnemyPositions[id];game.Pause(true);world.Advance(1,Vector2.UnitX);Check.Near(clock,world.SimulationTime);Check.Equal(position,world.EnemyPositions[id]);game.Pause(false);
            state.Run!.PendingLevels=1;state.Run.Choices=game.Inventory.Choices(state.Profile.UnlockedWeapons,new[]{"hair"},3,state.Run.ChoiceSerial).ToList();game.Capture();world.Advance(1,Vector2.UnitX);Check.Near(clock,world.SimulationTime);Check.True(game.Choose(game.PendingChoices[0]));
            game.Kill(id);state.Run.Drops.Clear();world.Advance(BattleWorld.FixedStep,Vector2.Zero);Check.True(!world.ChargeStates.ContainsKey(id));
            foreach(var pair in world.ChargeStates)Check.Near(game.Targets[pair.Key].SpawnedAt!.Value+6,pair.Value.NextAt,1e-7);
            state.Run.LastHitTime=-1;game.Damage(10000);world.Advance(1,Vector2.Zero);Check.True(state.Run==null);Check.Equal(0,world.ChargeStates.Count);
            // A dead host never advances a retained charge timer.
            var after=world.SimulationTime;world.Advance(1,Vector2.Zero);Check.Near(after,world.SimulationTime);
        });
        test("R04 D3 telegraph and midcharge disk restore preserve locked direction remaining cooldown and World",()=>Store(store=>{
            var (state,game,world,id)=Charger();world.Advance(world.ChargeStates[id].NextAt-world.SimulationTime+.4,Vector2.UnitY);
            long revision=0;
            foreach(var phase in new[]{"telegraph","charge"})
            {
                Check.Equal(phase,world.ChargeStates[id].Phase);game.Capture();state.Run!.World=world.Capture();revision=store.Commit(state,revision,phase);
                var loaded=store.Load()!;var resumed=AdamSession.Resume(loaded,CatalogReader.Round(Catalog,4));var replay=BattleWorld.Restore(resumed,Speeds,loaded.Run!.World!);
                Check.Equal(JsonSerializer.Serialize(world.Capture()),JsonSerializer.Serialize(replay.Capture()));
                world.Advance(phase=="telegraph"?1:.8,Vector2.UnitX);replay.Advance(phase=="telegraph"?1:.8,Vector2.UnitX);
                Check.Equal(JsonSerializer.Serialize(world.Capture()),JsonSerializer.Serialize(replay.Capture()));
            }
        }));
        test("R04 completion waits for level choices then heals pays65 unlocks only once and resumes between5",()=>Store(store=>{
            var (state,game)=Start();game.Advance(10);game.Damage(20);foreach(var enemy in game.Targets.Keys.Take(5).ToArray())game.Kill(enemy);
            var hp=game.CurrentHealth;var gold=state.Profile.Gold;game.Advance(180);Check.Equal("round_settlement",game.Stage);Check.True(game.PendingChoices.Count>0);Check.Near(hp,game.CurrentHealth);Check.Equal(gold,state.Profile.Gold);
            store.Commit(state,0,"pending");var loaded=store.Load()!;game=AdamSession.Resume(loaded,CatalogReader.Round(Catalog,4));RoundTransitionTests.Resolve(game);
            Check.Equal("between_rounds",game.Stage);Check.Equal(5,loaded.Run!.RoundNumber);Check.Near(loaded.Run.MaxHealth,game.CurrentHealth);Check.Equal(gold+65,loaded.Profile.Gold);
            Check.True(loaded.Profile.UnlockedWeapons.Contains("fire_pillar"));Check.True(!game.Inventory.TryChoose("fire_pillar",false));Check.True(!game.CompleteNormalSettlement());game.Advance(180);Check.Equal(gold+65,loaded.Profile.Gold);
            store.Commit(loaded,1,"complete");store.Commit(loaded,1,"complete");game=AdamSession.Resume(store.Load()!,CatalogReader.Round(Catalog,4));Check.True(!game.IsActive);
            Check.Throws<ArgumentOutOfRangeException>(()=>CatalogReader.Round(Catalog,9));
        }));
        test("R04 D3 uses only contact damage and shared iframe with stable overlap direction",()=>{
            var (state,game,world,id)=Charger();var snapshot=world.Capture();var actor=snapshot.Enemies.Single(x=>x.Id==id);actor.X=actor.Y=0;
            var charge=snapshot.Charges!.Single(x=>x.Id==id);charge.NextAt=snapshot.Time;world=BattleWorld.Restore(game,Speeds,snapshot);var hp=game.CurrentHealth;
            world.Advance(.1,Vector2.Zero);Check.Near(hp-10,game.CurrentHealth);Check.Near(1,world.ChargeStates[id].DirectionX);Check.Near(0,world.ChargeStates[id].DirectionY);Check.Equal(10,world.DamageTaken);
        });
        test("R04 future World charge and completed receipt preserve originals while invalid clocks are rejected",()=>Store(store=>{
            var (state,game,world,id)=Charger();game.Capture();state.Run!.World=world.Capture();store.Commit(state,0,"charge");var original=File.ReadAllText(store.Path);File.WriteAllText(store.Path+".bak",original);
            foreach(var change in new Action<JsonNode>[] {n=>n["Run"]!["World"]!["Charges"]![0]!["Version"]=2,n=>n["Run"]!["World"]!["Charges"]![0]!["Phase"]="future",
                n=>n["Run"]!["World"]!["Charges"]![0]!["NewField"]=1,n=>n["Run"]!["World"]!["SchemaVersion"]=4,n=>n["Run"]!["CompletedBoss"]!["Version"]=3})
            {var node=JsonNode.Parse(original)!;change(node);var future=node.ToJsonString();File.WriteAllText(store.Path,future);Check.Throws<UnsupportedSaveVersionException>(()=>store.Load());Check.Throws<UnsupportedSaveVersionException>(()=>store.Commit(GameSnapshot.New(),1,"overwrite"));Check.Equal(future,File.ReadAllText(store.Path));Check.Equal(original,File.ReadAllText(store.Path+".bak"));}
            state.Run.World.Charges![0].NextAt=double.NaN;Check.Throws<ArgumentException>(()=>state.Validate());
        }));
    }
}
