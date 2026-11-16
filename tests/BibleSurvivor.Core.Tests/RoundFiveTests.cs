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
internal static class RoundFiveTests
{
    private const string Catalog="content/genesis/catalog.json";
    private static readonly IReadOnlyDictionary<string,double> Speeds=CatalogReader.MonsterSpeeds(Catalog);
    internal static (GameSnapshot State,AdamSession Game) Start()
    {var (state,game)=RoundFourTests.Start();game.Advance(180);return(state,game.StartNextRound(CatalogReader.Round(Catalog,5)));}
    private static void Store(Action<SaveStore> action)
    {var folder=Path.Combine(Path.GetTempPath(),"r05-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(folder);try{action(new SaveStore(Path.Combine(folder,"save.json")));}finally{Directory.Delete(folder,true);}}
    public static void Register(Action<string,Action> test)
    {
        test("R05 catalog entry preserves evolved gear progression boss and previous normal receipt without R04 charge",()=>{
            var definition=CatalogReader.Round(Catalog,5);Check.Equal(690,definition.Waves.Sum(w=>w.Groups.Sum(g=>g.Count)));Check.Equal(1900,definition.RawExperience);Check.Equal(80,definition.ConcurrencyCap);Check.Equal(80,definition.CompletionGold);Check.Equal("daniel_lion",definition.UnlockWeaponId);
            var (state,game)=RoundFourTests.Start();game.Advance(180);var id=state.Run!.RunId;var gold=state.Profile.Gold;var boss=JsonSerializer.Serialize(state.Run.CompletedBoss);var gear=JsonSerializer.Serialize(game.Inventory.Forms);var level=game.Growth.Level;
            game=game.StartNextRound(definition);Check.Equal(id,state.Run.RunId);Check.Equal(gold,state.Profile.Gold);Check.Equal(level,game.Growth.Level);Check.Equal(boss,JsonSerializer.Serialize(state.Run.CompletedBoss));Check.Equal(gear,JsonSerializer.Serialize(game.Inventory.Forms));
            Check.Equal(4,state.Run.PreviousNormalRound);Check.True(!state.Run.RoundCompletionApplied);Check.Near(-1,state.Run.LastHitTime);var world=new BattleWorld(game,Speeds);Check.Equal(0,world.ChargeStates.Count);Check.Equal(3,world.Capture().SchemaVersion);state.Validate();
        });
        test("R05 E1 E2 E3 use actual chase speeds and contact with no forced movement or charges",()=>{
            var (state,game)=Start();game.Advance(169);int e1=0,e2=0,e3=0;
            for(var i=0;i<10;i++)
            {
                foreach(var actor in game.Targets.Values.OrderBy(x=>x.Id).ToArray())
                {
                    if(actor.MonsterId=="e1"&&e1==0)e1=actor.Id;else if(actor.MonsterId=="e2"&&e2==0)e2=actor.Id;else if(actor.MonsterId=="e3"&&e3==0)e3=actor.Id;
                    else if(actor.Id!=e1&&actor.Id!=e2&&actor.Id!=e3)game.Kill(actor.Id);
                }
                game.Advance(BattleWorld.FixedStep);state.Run!.Drops.Clear();
            }
            Check.True(e1>0&&e2>0&&e3>0);var world=new BattleWorld(game,Speeds);var snapshot=world.Capture();
            foreach(var actor in snapshot.Enemies){actor.X=20;actor.Y=0;}
            snapshot.Weapons.Add(new BattleWeaponState{WeaponId="sling",WeaponForm=EvolutionRules.Sling,NextCycle=1000});world=BattleWorld.Restore(game,Speeds,snapshot);
            world.Advance(1,Vector2.Zero);Check.Near(20-3*.6,world.EnemyPositions[e1].X,.0001);Check.Near(20-3*.45,world.EnemyPositions[e2].X,.0001);Check.Near(20-3*.95,world.EnemyPositions[e3].X,.0001);Check.Equal(Vector2.Zero,world.PlayerPosition);Check.Equal(0,world.ChargeStates.Count);
            snapshot=world.Capture();var hand=snapshot.Enemies.Single(x=>x.Id==e2);hand.X=.1f;hand.Y=0;world=BattleWorld.Restore(game,Speeds,snapshot);var hp=game.CurrentHealth;
            world.Advance(.1,Vector2.Zero);Check.Near(hp-9,game.CurrentHealth);Check.Equal(Vector2.Zero,world.PlayerPosition);Check.Equal(9,world.DamageTaken);
        });
        test("R05 coordinate midpoint disk restore preserves evolution attacks and exact next World",()=>Store(store=>{
            var (state,game)=Start();var world=new BattleWorld(game,Speeds,7);
            for(var i=0;i<600;i++){RoundTransitionTests.Resolve(game);world.Advance(BattleWorld.FixedStep,Vector2.UnitX);}
            Check.True(world.Hits>0);Check.Equal(0,world.ChargeStates.Count);game.Capture();state.Run!.World=world.Capture();store.Commit(state,0,"mid");
            var loaded=store.Load()!;var resumed=AdamSession.Resume(loaded,CatalogReader.Round(Catalog,5));var replay=BattleWorld.Restore(resumed,Speeds,loaded.Run!.World!);
            for(var i=0;i<120;i++){RoundTransitionTests.Resolve(game);RoundTransitionTests.Resolve(resumed);world.Advance(BattleWorld.FixedStep,Vector2.UnitX);replay.Advance(BattleWorld.FixedStep,Vector2.UnitX);}
            Check.Equal(JsonSerializer.Serialize(world.Capture()),JsonSerializer.Serialize(replay.Capture()));Check.Near(game.CurrentHealth,resumed.CurrentHealth);Check.Equal(EvolutionRules.Sling,resumed.Inventory.Forms["sling"]);Check.Equal(4,loaded.Run.PreviousNormalRound);
        }));
        test("R05 pending choices precede heal gold80 unlock and between6 settlement once with save retry",()=>Store(store=>{
            var (state,game)=Start();game.Advance(10);game.Damage(20);foreach(var id in game.Targets.Keys.Take(5).ToArray())game.Kill(id);
            var hp=game.CurrentHealth;var gold=state.Profile.Gold;game.Advance(180);Check.Equal("round_settlement",game.Stage);Check.Near(hp,game.CurrentHealth);Check.Equal(gold,state.Profile.Gold);Check.True(!game.CompleteNormalSettlement());
            store.Commit(state,0,"pending");var loaded=store.Load()!;game=AdamSession.Resume(loaded,CatalogReader.Round(Catalog,5));RoundTransitionTests.Resolve(game);
            Check.Equal("between_rounds",game.Stage);Check.Equal(6,loaded.Run!.RoundNumber);Check.Equal(gold+80,loaded.Profile.Gold);Check.Near(loaded.Run.MaxHealth,game.CurrentHealth);Check.Equal(4,loaded.Run.PreviousNormalRound);
            Check.True(loaded.Profile.UnlockedWeapons.Contains("fire_pillar"));Check.True(loaded.Profile.UnlockedWeapons.Contains("daniel_lion"));Check.True(!game.Inventory.TryChoose("daniel_lion",false));Check.True(!game.CompleteNormalSettlement());
            var broken=new SaveStore(store.Path,()=>throw new IOException("replace fault"));Check.Throws<IOException>(()=>broken.Commit(loaded,1,"finish"));Check.Equal("round_settlement",store.Load()!.Run!.Stage);
            store.Commit(loaded,1,"finish");store.Commit(loaded,1,"finish");loaded=store.Load()!;game=AdamSession.Resume(loaded,CatalogReader.Round(Catalog,5));Check.True(!game.CompleteNormalSettlement());Check.Equal(gold+80,loaded.Profile.Gold);
            Check.Throws<ArgumentOutOfRangeException>(()=>CatalogReader.Round(Catalog,8));Check.Throws<InvalidOperationException>(()=>game.StartNextRound(new RoundDefinition(7,80,1900,80,CatalogReader.Round(Catalog,5).Waves)));
        }));
        test("R05 charge leakage invalid receipt and future content preserve prior checkpoint",()=>Store(store=>{
            var (state,game)=Start();var world=new BattleWorld(game,Speeds);game.Capture();state.Run!.World=world.Capture();store.Commit(state,0,"r05");var original=File.ReadAllText(store.Path);File.WriteAllText(store.Path+".bak",original);
            foreach(var change in new Action<JsonNode>[] {n=>n["Run"]!["RoundNumber"]=8,n=>n["Run"]!["UnknownFuture"]=1,n=>n["Run"]!["World"]!["SchemaVersion"]=4})
            {
                var node=JsonNode.Parse(original)!;change(node);var future=node.ToJsonString();File.WriteAllText(store.Path,future);
                Check.Throws<UnsupportedSaveVersionException>(()=>store.Load());Check.Throws<UnsupportedSaveVersionException>(()=>store.Commit(GameSnapshot.New(),1,"overwrite"));Check.Equal(future,File.ReadAllText(store.Path));Check.Equal(original,File.ReadAllText(store.Path+".bak"));
            }
            state.Run.World.Charges!.Add(new EnemyChargeState{Id=1,NextAt=6});Check.Throws<InvalidDataException>(()=>state.Validate());state.Run.World.Charges.Clear();state.Run.PreviousNormalRound=0;Check.Throws<InvalidDataException>(()=>state.Validate());
        }));
    }
}
