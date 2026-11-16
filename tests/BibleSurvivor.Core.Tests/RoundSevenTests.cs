using System;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text.Json;
using System.Text.Json.Nodes;
using BibleSurvivor.Core;
using BibleSurvivor.Sim;
namespace BibleSurvivor.Core.Tests;
internal static class RoundSevenTests
{
    private const string Catalog="content/genesis/catalog.json";
    private static (GameSnapshot State,AdamSession Game) CompletedSix()
    {
        var (state,game)=RoundSixTests.Start();game.Advance(180);game.Advance(2);game.Hit(781,6500);
        var choice=game.BossChoices.First();Check.True(game.ChooseBossReward(game.Boss!.Reward!.RewardId,choice.Id));RoundTransitionTests.Resolve(game);Check.True(game.CompleteBossSettlement());return(state,game);
    }
    private static void Store(Action<SaveStore> action)
    {var folder=Path.Combine(Path.GetTempPath(),"r07-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(folder);try{action(new SaveStore(Path.Combine(folder,"save.json")));}finally{Directory.Delete(folder,true);}}
    public static void Register(Action<string,Action> test)
    {
        test("R07 catalog entry preserves both boss receipts evolution and completion history",()=>Store(store=>{
            var (state,game)=CompletedSix();store.Commit(state,0,"r06");var original=File.ReadAllText(store.Path);state=store.Load()!;Check.Equal(original,File.ReadAllText(store.Path));game=AdamSession.Resume(state,CatalogReader.Round(Catalog,6));
            var first=JsonSerializer.Serialize(state.Run!.CompletedBoss);var second=JsonSerializer.Serialize(state.Run.Boss);game=game.StartNextRound(CatalogReader.Round(Catalog,7));
            Check.Equal(first,JsonSerializer.Serialize(state.Run.CompletedBoss));Check.Equal(second,JsonSerializer.Serialize(state.Run.CompletedSecondBoss));Check.True(state.Run.Boss==null);Check.Equal(5,state.Run.PreviousNormalRound);Check.Equal(EvolutionRules.Sling,game.Inventory.Forms["sling"]);state.Validate();
            var d=CatalogReader.Round(Catalog,7);Check.Equal(900,d.Waves.Sum(w=>w.Groups.Sum(g=>g.Count)));Check.Equal(3000,d.RawExperience);Check.Equal(100,d.ConcurrencyCap);Check.Equal(120,d.CompletionGold);Check.Equal("elijah_chariot",d.UnlockWeaponId);
        }));
        test("R07 G3 stable species order alternates sides independent health pause and disk replay",()=>Store(store=>{
            var (state,game)=CompletedSix();game=game.StartNextRound(CatalogReader.Round(Catalog,7));var speeds=CatalogReader.MonsterSpeeds(Catalog);
            // Clear prior waves without damage injection into the geometry under test.
            for(var t=0;t<61;t++){game.Advance(1);foreach(var id in game.Targets.Keys.ToArray()){game.Kill(id);game.Collect(id);}RoundTransitionTests.Resolve(game);}game.Advance(3);
            var world=new BattleWorld(game,speeds,7);var twins=game.Targets.Values.Where(x=>x.MonsterId=="g3").OrderBy(x=>x.Id).ToArray();Check.True(twins.Length>=2);
            Check.True(world.EnemyPositions[twins[0].Id].X<world.PlayerPosition.X);Check.True(world.EnemyPositions[twins[1].Id].X>world.PlayerPosition.X);
            game.Hit(twins[0].Id,7);Check.Near(233,game.Targets[twins[0].Id].Health);Check.Near(240,game.Targets[twins[1].Id].Health);game.Kill(twins[0].Id);game.Collect(twins[0].Id);RoundTransitionTests.Resolve(game);Check.Near(240,game.Targets[twins[1].Id].Health);
            // Rebuild to synchronize the explicit kill, then save the exact RNG and actor positions.
            world=new BattleWorld(game,speeds,7);game.Capture();state.Run!.World=world.Capture();store.Commit(state,0,"g3");var loaded=store.Load()!;var resumed=AdamSession.Resume(loaded,CatalogReader.Round(Catalog,7));var replay=BattleWorld.Restore(resumed,speeds,loaded.Run!.World!);
            world.Advance(2,Vector2.UnitY);replay.Advance(2,Vector2.UnitY);Check.Equal(JsonSerializer.Serialize(world.Capture()),JsonSerializer.Serialize(replay.Capture()));Check.Equal(0,world.ChargeStates.Count);
            game.Pause(true);var before=JsonSerializer.Serialize(world.Capture());world.Advance(1,Vector2.Zero);Check.Equal(before,JsonSerializer.Serialize(world.Capture()));
        }));
        test("R07 growth gates120 gold unlock heal once retry and future round protection",()=>Store(store=>{
            var (state,game)=CompletedSix();game=game.StartNextRound(CatalogReader.Round(Catalog,7));var first=JsonSerializer.Serialize(state.Run!.CompletedBoss);var second=JsonSerializer.Serialize(state.Run.CompletedSecondBoss);var gold=state.Profile.Gold;game.Advance(10);game.Damage(20);
            game.Advance(50);foreach(var id in game.Targets.Keys.ToArray())game.Kill(id);gold=state.Profile.Gold;game.Advance(180);Check.Equal("round_settlement",game.Stage);Check.Equal(gold,state.Profile.Gold);Check.True(game.CurrentHealth<state.Run.MaxHealth);Check.True(!game.CompleteNormalSettlement());RoundTransitionTests.Resolve(game);
            Check.Equal("between_rounds",game.Stage);Check.Equal(8,state.Run.RoundNumber);Check.Equal(gold+120,state.Profile.Gold);Check.Near(state.Run.MaxHealth,game.CurrentHealth);Check.True(state.Profile.UnlockedWeapons.Contains("elijah_chariot"));Check.True(!game.Inventory.TryChoose("elijah_chariot",false));Check.True(!game.CompleteNormalSettlement());
            Check.Equal(first,JsonSerializer.Serialize(state.Run.CompletedBoss));Check.Equal(second,JsonSerializer.Serialize(state.Run.CompletedSecondBoss));store.Commit(state,0,"done");store.Commit(state,0,"done");var loaded=store.Load()!;game=AdamSession.Resume(loaded,CatalogReader.Round(Catalog,7));Check.True(!game.CompleteNormalSettlement());Check.Throws<ArgumentOutOfRangeException>(()=>CatalogReader.Round(Catalog,8));
            var original=File.ReadAllText(store.Path);File.WriteAllText(store.Path+".bak",original);
            foreach(var change in new Action<JsonNode>[] {n=>n["Run"]!["Stage"]="normal",n=>n["Run"]!["CompletedSecondBoss"]!["FutureReceipt"]=1,n=>n["Run"]!["CompletedSecondBoss"]!["Reward"]!["PolicyId"]="future"})
            {var node=JsonNode.Parse(original)!;change(node);var future=node.ToJsonString();File.WriteAllText(store.Path,future);Check.Throws<UnsupportedSaveVersionException>(()=>store.Load());Check.Throws<UnsupportedSaveVersionException>(()=>store.Commit(GameSnapshot.New(),1,"overwrite"));Check.Equal(future,File.ReadAllText(store.Path));Check.Equal(original,File.ReadAllText(store.Path+".bak"));}
            loaded.Run!.CompletedSecondBoss=loaded.Run.CompletedBoss;Check.Throws<InvalidDataException>(()=>loaded.Validate());
        }));
    }
}
