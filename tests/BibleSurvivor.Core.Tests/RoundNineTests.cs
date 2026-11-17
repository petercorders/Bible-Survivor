using System;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text.Json;
using System.Text.Json.Nodes;
using BibleSurvivor.Core;
using BibleSurvivor.Sim;
namespace BibleSurvivor.Core.Tests;
internal static class RoundNineTests
{
    private const string Catalog="content/genesis/catalog.json";
    private static readonly System.Collections.Generic.IReadOnlyDictionary<string,double> Speeds=CatalogReader.MonsterSpeeds(Catalog);
    private static (GameSnapshot State,AdamSession Game) Start()
    {var (state,game)=RoundEightTests.CompletedSeven();game=game.StartNextRound(CatalogReader.Round(Catalog,8));game.Advance(180);return(state,game.StartNextRound(CatalogReader.Round(Catalog,9)));}
    private static (GameSnapshot State,AdamSession Game,BattleWorld World) Boss()
    {
        var (state,game)=Start();game.Advance(180);var world=new BattleWorld(game,Speeds);world.Advance(2,Vector2.Zero);
        var snapshot=world.Capture();var actor=snapshot.Enemies.Single(x=>x.Id==1141);actor.X=30;actor.Y=0;
        foreach(var id in game.Inventory.Weapons.Keys)snapshot.Weapons.Add(new BattleWeaponState{WeaponId=id,WeaponForm=game.Inventory.Forms.TryGetValue(id,out var form)?form:null,NextCycle=1000});
        return(state,game,BattleWorld.Restore(game,Speeds,snapshot));
    }
    private static void Store(Action<SaveStore> action)
    {var folder=Path.Combine(Path.GetTempPath(),"r09-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(folder);try{action(new SaveStore(Path.Combine(folder,"save.json")));}finally{Directory.Delete(folder,true);}}
    public static void Register(Action<string,Action> test)
    {
        test("R09 catalog normal intro identity preserves two histories no early completion",()=>{
            Check.Equal("three-chain-v1:1.35:0.16:6:20:12:",BossDefinition.ChainContract);var d=CatalogReader.Round(Catalog,9);Check.Equal(1140,d.Waves.Sum(w=>w.Groups.Sum(g=>g.Count)));Check.Equal(4200,d.RawExperience);Check.Equal(120,d.ConcurrencyCap);Check.Equal(160,d.CompletionGold);Check.True(d.UnlockWeaponId==null);
            var (state,game)=Start();var first=JsonSerializer.Serialize(state.Run!.CompletedBoss);var second=JsonSerializer.Serialize(state.Run.CompletedSecondBoss);game.Advance(10);game.Damage(20);var hp=game.CurrentHealth;var gold=state.Profile.Gold;game.Advance(180);
            Check.Equal("boss_intro",game.Stage);Check.Equal(8,state.Run.PreviousNormalRound);Check.Equal(9,game.Boss!.EncounterRound);Check.Equal(16000d,game.Boss.Health);Check.Equal(5d,game.Boss.NextAttackAt);Check.Equal(3,game.Boss.Version);Check.Near(hp,game.CurrentHealth);Check.Equal(gold,state.Profile.Gold);game.Damage(1000);game.Hit(1141,20000);Check.Near(16000,game.Boss.Health);
            var world=new BattleWorld(game,Speeds);world.Advance(2,Vector2.Zero);Check.Equal(4,world.Capture().SchemaVersion);Check.Equal(0,world.HostileProjectiles.Count);Check.Equal(first,JsonSerializer.Serialize(state.Run.CompletedBoss));Check.Equal(second,JsonSerializer.Serialize(state.Run.CompletedSecondBoss));state.Validate();
        });
        test("R09 fixed three directions one second stop next tick chains low speed TTL and pause",()=>{
            var (state,game,world)=Boss();world.Advance(4,Vector2.Zero);Check.True(game.Boss!.Telegraphing);var directions=JsonSerializer.Serialize(game.Boss.ChainDirections);var origin=world.EnemyPositions[1141];Check.Equal(3,game.Boss.ChainDirections!.Count);Check.Near(-1,game.Boss.ChainDirections[1].X);Check.Near(Math.Cos(Math.PI/9),Vector2.Dot(new Vector2(game.Boss.ChainDirections[0].X,game.Boss.ChainDirections[0].Y),new Vector2(-1,0)),1e-6);
            world.Advance(1,Vector2.UnitY);Check.Equal(directions,JsonSerializer.Serialize(game.Boss.ChainDirections));Check.Equal(origin,world.EnemyPositions[1141]);Check.Equal(3,world.HostileProjectiles.Count);foreach(var p in world.HostileProjectiles){Check.Near(6,p.Lifetime);Check.Near(origin.X,p.X);Check.Near(origin.Y,p.Y);}Check.Near(10,game.Boss.NextAttackAt);
            var before=world.HostileProjectiles[1];world.Advance(BattleWorld.FixedStep,Vector2.Zero);var after=world.HostileProjectiles.Single(p=>p.Id==before.Id);Check.Near(BossDefinition.ChainSpeed*BattleWorld.FixedStep,Vector2.Distance(new(before.X,before.Y),new(after.X,after.Y)),1e-5);
            game.Pause(true);var frozen=JsonSerializer.Serialize(world.Capture());world.Advance(1,Vector2.Zero);Check.Equal(frozen,JsonSerializer.Serialize(world.Capture()));game.Pause(false);
            world.Advance(6,Vector2.UnitY);Check.True(world.HostileProjectiles.All(p=>p.Id>3));Check.True(world.HostileProjectiles.Count<=6);Check.True(BossDefinition.ChainSpeed<world.Capture().Tuning.PlayerSpeed);
        });
        test("R09 hostile swept damage armor iframe removes blocked shots death wins and kill clears",()=>{
            var (state,game,world)=Boss();world.Advance(5,Vector2.Zero);var snapshot=world.Capture();snapshot.HostileProjectiles!.RemoveRange(1,2);var p=snapshot.HostileProjectiles[0];p.StartX=p.X=-.42f;p.StartY=p.Y=0;p.DirectionX=1;p.DirectionY=0;world=BattleWorld.Restore(game,Speeds,snapshot);var hp=game.CurrentHealth;world.Advance(BattleWorld.FixedStep,Vector2.Zero);Check.Near(hp-Math.Max(1,15-(game.Inventory.Passives.TryGetValue("seal",out var armor)?armor:0)),game.CurrentHealth);Check.Equal(0,world.HostileProjectiles.Count);
            var blocked=Boss();blocked.World.Advance(5,Vector2.Zero);snapshot=blocked.World.Capture();foreach(var q in snapshot.HostileProjectiles!){q.StartX=q.X=-.42f;q.StartY=q.Y=0;q.DirectionX=1;q.DirectionY=0;}blocked.World=BattleWorld.Restore(blocked.Game,Speeds,snapshot);blocked.Game.Damage(1);hp=blocked.Game.CurrentHealth;blocked.World.Advance(BattleWorld.FixedStep,Vector2.Zero);Check.Near(hp,blocked.Game.CurrentHealth);Check.Equal(0,blocked.World.HostileProjectiles.Count);
            var lethal=Boss();lethal.World.Advance(5,Vector2.Zero);snapshot=lethal.World.Capture();foreach(var q in snapshot.HostileProjectiles!){q.StartX=q.X=-.42f;q.StartY=q.Y=0;q.DirectionX=1;q.DirectionY=0;}var actor=snapshot.Enemies.Single(x=>x.Id==1141);actor.X=.8f;actor.Y=0;snapshot.Weapons.Clear();lethal.World=BattleWorld.Restore(lethal.Game,Speeds,snapshot);lethal.Game.Hit(1141,15999);lethal.State.Run!.Health=1;lethal.State.Run.LastHitTime=-1;var gold=lethal.State.Profile.Gold;lethal.World.Advance(BattleWorld.FixedStep,Vector2.Zero);Check.True(lethal.State.Run==null);Check.Equal(gold,lethal.State.Profile.Gold);Check.Equal(0,lethal.World.BossKills);
            game.Hit(1141,16000);world.Advance(BattleWorld.FixedStep,Vector2.Zero);Check.Equal(0,world.HostileProjectiles.Count);
        });
        test("R09 thresholds sixty thirty six each once same tick support checkpoint no lethal summons",()=>{
            var (state,game,world)=Boss();game.Hit(1141,6399);Check.Equal(0,game.Boss!.SummonMask);game.Hit(1141,1);Check.Equal(1,game.Boss!.SummonMask);Check.Equal(6,game.Boss.Supports.Count);Check.Equal("i2",game.Boss.Supports[0].MonsterId);game.Hit(1141,4800);Check.Equal(3,game.Boss.SummonMask);Check.Equal(12,game.Boss.Summoned);game.Hit(1141,1);Check.Equal(12,game.Boss.Summoned);world.Advance(BattleWorld.FixedStep,Vector2.Zero);game.Capture();state.Run!.World=world.Capture();state.Validate();Check.Equal(13,world.EnemyPositions.Count);
            var lethal=Boss();lethal.Game.Hit(1141,16000);Check.Equal(0,lethal.Game.Boss!.Summoned);
        });
        test("R09 telegraph and live hostile disk replay clocks cap version future original protection",()=>Store(store=>{
            var (state,game,world)=Boss();world.Advance(4,Vector2.Zero);game.Capture();state.Run!.World=world.Capture();store.Commit(state,0,"telegraph");var loaded=store.Load()!;var resumed=AdamSession.Resume(loaded,CatalogReader.Round(Catalog,9));var replay=BattleWorld.Restore(resumed,Speeds,loaded.Run!.World!);world.Advance(1.2,Vector2.UnitY);replay.Advance(1.2,Vector2.UnitY);Check.Equal(JsonSerializer.Serialize(world.Capture()),JsonSerializer.Serialize(replay.Capture()));
            game.Capture();state.Run.World=world.Capture();store.Commit(state,1,"live");loaded=store.Load()!;resumed=AdamSession.Resume(loaded,CatalogReader.Round(Catalog,9));replay=BattleWorld.Restore(resumed,Speeds,loaded.Run!.World!);world.Advance(.5,Vector2.UnitY);replay.Advance(.5,Vector2.UnitY);Check.Equal(JsonSerializer.Serialize(world.Capture()),JsonSerializer.Serialize(replay.Capture()));
            var invalid=world.Capture();invalid.HostileProjectiles!.AddRange(Enumerable.Range(0,13).Select(i=>new HostileProjectileState()));Check.Throws<ArgumentException>(()=>invalid.Validate());
            var original=File.ReadAllText(store.Path);File.WriteAllText(store.Path+".bak",original);
            foreach(var change in new Action<JsonNode>[] {n=>n["Run"]!["World"]!["HostileProjectiles"]![0]!["Version"]=2,n=>n["Run"]!["World"]!["HostileProjectiles"]![0]!["Future"]=1,n=>n["Run"]!["World"]!["SchemaVersion"]=5,n=>n["Run"]!["Boss"]!["RoundNumber"]=12,n=>n["Run"]!["Boss"]!["ChainDirections"]![0]!["Future"]=1})
            {var node=JsonNode.Parse(original)!;change(node);var future=node.ToJsonString();File.WriteAllText(store.Path,future);Check.Throws<UnsupportedSaveVersionException>(()=>store.Load());Check.Throws<UnsupportedSaveVersionException>(()=>store.Commit(GameSnapshot.New(),2,"overwrite"));Check.Equal(future,File.ReadAllText(store.Path));Check.Equal(original,File.ReadAllText(store.Path+".bak"));}
        }));
        test("R09 reward350 growth25 boss100 completion160 once chaptercomplete load all three receipts",()=>Store(store=>{
            var (state,game,world)=Boss();var first=JsonSerializer.Serialize(state.Run!.CompletedBoss);var second=JsonSerializer.Serialize(state.Run.CompletedSecondBoss);var unlocks=string.Join(",",state.Profile.UnlockedWeapons);var gold=state.Profile.Gold;game.Hit(1141,16000);Check.Equal(gold+350,state.Profile.Gold);Check.Equal(BossDefinition.JudahPolicy,game.Boss!.Reward!.PolicyId);Check.True(game.BossChoices.Select(c=>c.Id).Distinct().Count()==game.BossChoices.Count);
            game.Capture();store.Commit(state,0,"reward");var loaded=store.Load()!;game=AdamSession.Resume(loaded,CatalogReader.Round(Catalog,9));var stable=JsonSerializer.Serialize(game.BossChoices);Check.Equal(stable,JsonSerializer.Serialize(AdamSession.Resume(store.Load()!,CatalogReader.Round(Catalog,9)).BossChoices));var choice=game.BossChoices.First();Check.True(game.ChooseBossReward(game.Boss!.Reward!.RewardId,choice.Id));Check.True(!game.ChooseBossReward(game.Boss!.Reward!.RewardId,choice.Id));RoundTransitionTests.Resolve(game);Check.True(game.CompleteBossSettlement());Check.Equal(gold+510+(choice.Kind=="gold"?100:0),loaded.Profile.Gold);Check.True(!game.CompleteBossSettlement());Check.True(game.ChapterComplete);Check.Equal(10,loaded.Run!.RoundNumber);Check.Equal(unlocks,string.Join(",",loaded.Profile.UnlockedWeapons));Check.Near(loaded.Run.MaxHealth,game.CurrentHealth);
            Check.Equal(first,JsonSerializer.Serialize(loaded.Run.CompletedBoss));Check.Equal(second,JsonSerializer.Serialize(loaded.Run.CompletedSecondBoss));Check.Equal(9,loaded.Run.Boss!.EncounterRound);store.Commit(loaded,1,"complete");store.Commit(loaded,1,"complete");loaded=store.Load()!;game=AdamSession.Resume(loaded,CatalogReader.Round(Catalog,9));Check.True(game.ChapterComplete);Check.True(!game.CompleteBossSettlement());Check.Throws<ArgumentOutOfRangeException>(()=>CatalogReader.Round(Catalog,10));Check.Throws<InvalidOperationException>(()=>game.StartNextRound(new RoundDefinition(10,120,4200,160,CatalogReader.Round(Catalog,9).Waves)));loaded.Validate();
        }));
    }
}
