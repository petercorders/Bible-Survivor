using System;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text.Json;
using System.Text.Json.Nodes;
using BibleSurvivor.Core;
using BibleSurvivor.Sim;
namespace BibleSurvivor.Core.Tests;
internal static class RoundEightTests
{
    private const string Catalog="content/genesis/catalog.json";
    internal static (GameSnapshot State,AdamSession Game) CompletedSeven()
    {var (state,game)=RoundSevenTests.CompletedSix();game=game.StartNextRound(CatalogReader.Round(Catalog,7));game.Advance(180);return(state,game);}
    private static void Store(Action<SaveStore> action)
    {var folder=Path.Combine(Path.GetTempPath(),"r08-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(folder);try{action(new SaveStore(Path.Combine(folder,"save.json")));}finally{Directory.Delete(folder,true);}}
    public static void Register(Action<string,Action> test)
    {
        test("R08 exhausted growth gold25 serial choice saves retries sequentially and boss100 stays separate",()=>Store(store=>{
            var (state,game)=CompletedSeven();game=game.StartNextRound(CatalogReader.Round(Catalog,8));
            void MaxExceptLast(AdamSession session)
            {
                foreach(var id in new[]{"bible","sling"})while(session.Inventory.TryChoose(id,false)){}
                while(!session.Inventory.Weapons.TryGetValue("jawbone",out var n)||n<7)Check.True(session.Inventory.TryChoose("jawbone",false));
                foreach(var id in new[]{"scroll","hair","stones","east_wind","seal","cloud","thunder","crown","underwear","whirlwind"})while(session.Inventory.TryChoose(id,true)){}
            }
            MaxExceptLast(game);game.Capture();state.Run!.PendingLevels=3;state.Run.Choices=new(){"jawbone"};var gold=state.Profile.Gold;var serial=state.Run.ChoiceSerial;var level=state.Run.Level;var xp=state.Run.Experience;
            state.Run.Choices=new(){"growth_gold_25:"+serial};Check.Throws<InvalidDataException>(()=>state.Validate());state.Run.Choices=new(){"jawbone"};
            store.Commit(state,0,"last-item");state=store.Load()!;game=AdamSession.Resume(state,CatalogReader.Round(Catalog,8));Check.Equal("jawbone",game.PendingChoices.Single());Check.True(game.Choose("jawbone"));Check.Equal(gold,state.Profile.Gold);
            var first=game.PendingChoices.Single();Check.Equal("growth_gold_25:"+(serial+1),first);store.Commit(state,1,"gold-choice");var original=File.ReadAllText(store.Path);state=store.Load()!;game=AdamSession.Resume(state,CatalogReader.Round(Catalog,8));Check.Equal(original,File.ReadAllText(store.Path));Check.Equal(gold,state.Profile.Gold);
            Check.True(game.Choose(first));Check.True(!game.Choose(first));Check.Equal(gold+25,state.Profile.Gold);Check.Equal(1,state.Run!.PendingLevels);Check.Equal(serial+2,state.Run.ChoiceSerial);
            store.Commit(state,2,"choose-once");store.Commit(state,2,"choose-once");state=store.Load()!;game=AdamSession.Resume(state,CatalogReader.Round(Catalog,8));Check.True(!game.Choose(first));Check.Equal(gold+25,state.Profile.Gold);var second=game.PendingChoices.Single();Check.True(first!=second);Check.True(game.Choose(second));Check.True(!game.Choose(second));Check.Equal(gold+50,state.Profile.Gold);Check.Equal(0,state.Run!.PendingLevels);Check.Equal(serial+3,state.Run.ChoiceSerial);Check.Equal(level,state.Run.Level);Check.Equal(xp,state.Run.Experience);
            // A future currency candidate must protect both original files before typed validation/fallback.
            var futureNode=JsonNode.Parse(original)!;futureNode["Run"]!["Choices"]=new JsonArray("growth_gold_50:"+(serial+1));var future=futureNode.ToJsonString();File.WriteAllText(store.Path,future);File.WriteAllText(store.Path+".bak",original);Check.Throws<UnsupportedSaveVersionException>(()=>store.Load());Check.Throws<UnsupportedSaveVersionException>(()=>store.Commit(GameSnapshot.New(),3,"overwrite"));Check.Equal(future,File.ReadAllText(store.Path));Check.Equal(original,File.ReadAllText(store.Path+".bak"));
            var boss=RoundSixTests.Start();boss.Game.Advance(180);boss.Game.Advance(2);MaxExceptLast(boss.Game);boss.Game.Capture();boss.Game.Hit(781,6500);boss.State.Run!.PendingLevels=1;boss.State.Run.Choices=new(){"jawbone"};var reward=boss.Game.Boss!.Reward!;var last= boss.Game.BossChoices.Single(c=>c.Kind=="item"&&c.ItemId=="jawbone");var before=boss.State.Profile.Gold;
            Check.True(boss.Game.BossChoices.Single(c=>c.Kind=="gold").Gold==100);Check.True(boss.Game.ChooseBossReward(reward.RewardId,last.Id));Check.Equal(before,boss.State.Profile.Gold);Check.True(boss.Game.PendingChoices.Single().StartsWith("growth_gold_25:",StringComparison.Ordinal));Check.True(boss.Game.Choose(boss.Game.PendingChoices.Single()));Check.Equal(before+25,boss.State.Profile.Gold);
        }));
        test("R08 catalog three story phases finite1020 XP3500 cap110 chase stats no unlock",()=>{
            var d=CatalogReader.Round(Catalog,8);Check.Equal(110,d.ConcurrencyCap);Check.Equal(3500,d.RawExperience);Check.Equal(140,d.CompletionGold);Check.True(d.UnlockWeaponId==null);
            Check.Equal("h1",string.Join(",",d.Waves[0].Groups.Select(g=>g.MonsterId)));Check.Equal("h1,h2",string.Join(",",d.Waves[1].Groups.Select(g=>g.MonsterId)));Check.Equal("h1,h2,h3",string.Join(",",d.Waves[2].Groups.Select(g=>g.MonsterId)));
            Check.Equal("270,340,410",string.Join(",",d.Waves.Select(w=>w.Groups.Sum(g=>g.Count))));
            var groups=d.Waves.SelectMany(w=>w.Groups).GroupBy(g=>g.MonsterId).Select(g=>g.First()).ToArray();Check.Equal("160,340,400",string.Join(",",groups.Select(g=>g.Health)));Check.Equal("10,12,13",string.Join(",",groups.Select(g=>g.ContactDamage)));
            var wave=new WaveDirector(d);wave.Advance(60);Check.Equal(110,wave.Active.Count);wave=new WaveDirector(d);var count=0;var xp=0;
            for(var t=0;t<170;t++){wave.Advance(1);foreach(var id in wave.Active.Keys.ToArray()){xp+=wave.Kill(id);count++;}}Check.Equal(1020,count);Check.Equal(3500,xp);Check.True(wave.IsNormalComplete);
        });
        test("R08 entry preserves R07 original bytes both receipts progression and permanent snapshot",()=>Store(store=>{
            var (state,game)=CompletedSeven();store.Commit(state,0,"r07");var original=File.ReadAllText(store.Path);state=store.Load()!;Check.Equal(original,File.ReadAllText(store.Path));game=AdamSession.Resume(state,CatalogReader.Round(Catalog,7));
            var first=JsonSerializer.Serialize(state.Run!.CompletedBoss);var second=JsonSerializer.Serialize(state.Run.CompletedSecondBoss);var permanent=JsonSerializer.Serialize(state.Run.PermanentSnapshot);var level=state.Run.Level;var xp=state.Run.Experience;var remainder=state.Run.KillRemainder;
            game=game.StartNextRound(CatalogReader.Round(Catalog,8));Check.Equal(first,JsonSerializer.Serialize(state.Run.CompletedBoss));Check.Equal(second,JsonSerializer.Serialize(state.Run.CompletedSecondBoss));Check.Equal(permanent,JsonSerializer.Serialize(state.Run.PermanentSnapshot));Check.Equal(level,state.Run.Level);Check.Equal(xp,state.Run.Experience);Check.Equal(remainder,state.Run.KillRemainder);Check.Equal(7,state.Run.PreviousNormalRound);Check.Equal(EvolutionRules.Sling,game.Inventory.Forms["sling"]);Check.True(state.Run.Boss==null);state.Validate();
        }));
        test("R08 H chase contact generic edges no G3 charge leakage pause and midpoint replay",()=>Store(store=>{
            var (state,game)=CompletedSeven();game=game.StartNextRound(CatalogReader.Round(Catalog,8));
            for(var t=0;t<125;t++){game.Advance(1);foreach(var id in game.Targets.Keys.ToArray()){game.Kill(id);game.Collect(id);}RoundTransitionTests.Resolve(game);}game.Advance(5);
            var speeds=CatalogReader.MonsterSpeeds(Catalog);var world=new BattleWorld(game,speeds,7);var snapshot=world.Capture();Check.True(snapshot.Enemies.Any(e=>Math.Abs(e.Y)>snapshot.Tuning.CameraHalfHeight));Check.Equal(0,world.ChargeStates.Count);
            var actor=game.Targets.Values.First(x=>x.MonsterId=="h2");var position=snapshot.Enemies.Single(x=>x.Id==actor.Id);position.X=10;position.Y=0;
            snapshot.Weapons.Add(new BattleWeaponState{WeaponId="sling",WeaponForm=EvolutionRules.Sling,NextCycle=1000});world=BattleWorld.Restore(game,speeds,snapshot);world.Advance(BattleWorld.FixedStep,Vector2.Zero);
            Check.Near(10-snapshot.Tuning.PlayerSpeed*.4*BattleWorld.FixedStep,world.EnemyPositions[actor.Id].X,1e-5);
            snapshot=world.Capture();position=snapshot.Enemies.Single(x=>x.Id==actor.Id);position.X=position.Y=0;world=BattleWorld.Restore(game,speeds,snapshot);var hp=game.CurrentHealth;world.Advance(BattleWorld.FixedStep,Vector2.Zero);Check.Near(hp-Math.Max(1,12-(game.Inventory.Passives.TryGetValue("seal",out var armor)?armor:0)),game.CurrentHealth);
            game.Capture();state.Run!.World=world.Capture();store.Commit(state,0,"mid");var loaded=store.Load()!;var resumed=AdamSession.Resume(loaded,CatalogReader.Round(Catalog,8));var replay=BattleWorld.Restore(resumed,speeds,loaded.Run!.World!);
            world.Advance(.5,Vector2.UnitY);replay.Advance(.5,Vector2.UnitY);Check.Equal(JsonSerializer.Serialize(world.Capture()),JsonSerializer.Serialize(replay.Capture()));
            game.Pause(true);var before=JsonSerializer.Serialize(world.Capture());world.Advance(1,Vector2.Zero);Check.Equal(before,JsonSerializer.Serialize(world.Capture()));
        }));
        test("R08 growth gates140 null unlock heal once retry future boundary and receipts preserved",()=>Store(store=>{
            var (state,game)=CompletedSeven();game=game.StartNextRound(CatalogReader.Round(Catalog,8));var first=JsonSerializer.Serialize(state.Run!.CompletedBoss);var second=JsonSerializer.Serialize(state.Run.CompletedSecondBoss);var unlocks=string.Join(",",state.Profile.UnlockedWeapons);game.Advance(60);game.Damage(20);
            foreach(var id in game.Targets.Keys.ToArray())game.Kill(id);var gold=state.Profile.Gold;game.Advance(180);Check.Equal("round_settlement",game.Stage);Check.Equal(gold,state.Profile.Gold);Check.True(game.CurrentHealth<state.Run.MaxHealth);Check.True(!game.CompleteNormalSettlement());
            store.Commit(state,0,"growth");state=store.Load()!;game=AdamSession.Resume(state,CatalogReader.Round(Catalog,8));RoundTransitionTests.Resolve(game);Check.Equal(9,state.Run!.RoundNumber);Check.Equal("between_rounds",game.Stage);Check.Equal(gold+140,state.Profile.Gold);Check.Near(state.Run.MaxHealth,game.CurrentHealth);Check.Equal(unlocks,string.Join(",",state.Profile.UnlockedWeapons));Check.True(!game.CompleteNormalSettlement());
            Check.Equal(first,JsonSerializer.Serialize(state.Run.CompletedBoss));Check.Equal(second,JsonSerializer.Serialize(state.Run.CompletedSecondBoss));store.Commit(state,1,"done");store.Commit(state,1,"done");state=store.Load()!;game=AdamSession.Resume(state,CatalogReader.Round(Catalog,8));Check.True(!game.CompleteNormalSettlement());Check.Throws<ArgumentOutOfRangeException>(()=>CatalogReader.Round(Catalog,10));Check.Throws<InvalidOperationException>(()=>game.StartNextRound(new RoundDefinition(10,110,3500,140,CatalogReader.Round(Catalog,8).Waves)));
            var original=File.ReadAllText(store.Path);File.WriteAllText(store.Path+".bak",original);
            foreach(var change in new Action<JsonNode>[] {n=>{n["Run"]!["Stage"]="normal";n["Run"]!["RoundNumber"]=10;},n=>n["Run"]!["CompletedSecondBoss"]!["FutureReceipt"]=1,n=>n["Run"]!["CompletedBoss"]!["Reward"]!["PolicyId"]="future"})
            {var node=JsonNode.Parse(original)!;change(node);var future=node.ToJsonString();File.WriteAllText(store.Path,future);Check.Throws<UnsupportedSaveVersionException>(()=>store.Load());Check.Throws<UnsupportedSaveVersionException>(()=>store.Commit(GameSnapshot.New(),2,"overwrite"));Check.Equal(future,File.ReadAllText(store.Path));Check.Equal(original,File.ReadAllText(store.Path+".bak"));}
        }));
    }
}
