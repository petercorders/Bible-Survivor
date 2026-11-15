using System;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Runtime.Serialization.Json;
using System.Text.Json;
using System.Text.Json.Nodes;
using BibleSurvivor.Core;
using BibleSurvivor.Sim;

namespace BibleSurvivor.Core.Tests;
internal static class BossTests
{
    private const string Catalog="content/genesis/catalog.json";
    internal static (GameSnapshot State,AdamSession Game) Start()
    {
        var state=GameSnapshot.New();var game=new AdamSession(state,CatalogReader.Round(Catalog,1),"sling");
        game.Advance(180);game=game.StartNextRound(CatalogReader.Round(Catalog,2));game.Advance(180);
        game=game.StartNextRound(CatalogReader.Round(Catalog,3));return(state,game);
    }
    private static (GameSnapshot State,AdamSession Game,BattleWorld World) Boss()
    {var (state,game)=Start();game.Advance(180);var world=new BattleWorld(game,CatalogReader.MonsterSpeeds(Catalog));world.Advance(2,Vector2.Zero);return(state,game,world);}
    private static BattleWorld Geometry(AdamSession game,BattleWorld world,Vector2 player,Vector2 boss)
    {
        var save=world.Capture();save.PlayerX=player.X;save.PlayerY=player.Y;
        var actor=save.Enemies.Single(x=>x.Id==481);actor.X=boss.X;actor.Y=boss.Y;
        return BattleWorld.Restore(game,CatalogReader.MonsterSpeeds(Catalog),save);
    }
    private static void WithStore(Action<SaveStore> action)
    {
        var path=Path.Combine(Path.GetTempPath(),"bible-r03-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(path);
        try{action(new SaveStore(Path.Combine(path,"save.json")));}finally{Directory.Delete(path,true);}
    }
    public static void Register(Action<string,Action> test)
    {
        test("R03 purchased regen follows actual normal and boss time and freezes outside combat",()=>{
            var state=GameSnapshot.New();state.Profile.Upgrades["health"]=2;state.Profile.Upgrades["armor"]=1;state.Profile.Upgrades["regen"]=1;
            var game=new AdamSession(state,CatalogReader.Round(Catalog,1),"sling");game.Advance(180);
            game=game.StartNextRound(CatalogReader.Round(Catalog,2));game.Advance(180);game=game.StartNextRound(CatalogReader.Round(Catalog,3));
            game.Damage(10);var hp=game.CurrentHealth;game.Advance(1);Check.Near(hp+.1,game.CurrentHealth);
            game.Advance(179);hp=game.CurrentHealth;var world=new BattleWorld(game,CatalogReader.MonsterSpeeds(Catalog));
            world.Advance(2,Vector2.UnitX);Check.Equal("boss",game.Stage);Check.Near(hp,game.CurrentHealth); // Protected intro is not combat regen or completion healing.
            game.Damage(10);hp=game.CurrentHealth;world.Advance(1,Vector2.UnitX);Check.Near(hp+.1,game.CurrentHealth);
            Check.True(game.CurrentHealth<state.Run!.MaxHealth);Check.Equal(60L,state.Profile.Gold);Check.True(!state.Profile.UnlockedWeapons.Contains("moses_staff"));
            hp=game.CurrentHealth;var clock=game.Boss!.BossElapsed;game.Pause(true);world.Advance(1,Vector2.UnitX);
            Check.Near(hp,game.CurrentHealth);Check.Near(clock,game.Boss!.BossElapsed);game.Pause(false);
            state.Run.PendingLevels=1;state.Run.Choices.Add("sling");game.Capture();world.Advance(1,Vector2.UnitX);
            Check.Near(hp,game.CurrentHealth);Check.Near(clock,game.Boss!.BossElapsed);Check.True(game.Choose("sling"));
            world=Geometry(game,world,new Vector2(50,0),new Vector2(100,0));state.Run.Health=state.Run.MaxHealth-.001;
            world.Advance(1,Vector2.Zero);Check.Near(state.Run.MaxHealth,game.CurrentHealth); // Cap, not an extra heal per tick.
            state.Run.Health=state.Run.MaxHealth-10;game.Hit(481,1800);hp=game.CurrentHealth;world.Advance(1,Vector2.Zero);
            Check.Near(hp,game.CurrentHealth);Check.True(game.Stage=="boss_reward"&&!game.Boss!.Reward!.Applied);game.Advance(1);Check.Near(hp,game.CurrentHealth);state.Validate();
        });
        test("R03 actual catalog consumes 480 1100 cap60 and isolated fingerprint tuning",()=>{
            var r=CatalogReader.Round(Catalog,3);Check.Equal(480,r.Waves.Sum(w=>w.Groups.Sum(g=>g.Count)));Check.Equal(1100,r.RawExperience);
            Check.Equal(60,r.ConcurrencyCap);Check.Equal(50,r.CompletionGold);Check.Equal("moses_staff",r.UnlockWeaponId);
            Check.True(r.Waves.SelectMany(w=>w.Groups).Select(g=>g.MonsterId).Distinct().OrderBy(x=>x).SequenceEqual(new[]{"c1","c2","c3"}));
        });
        test("R03 normal timeout clears live enemies collects only drops without healing completion gold unlock",()=>{
            var (state,game)=Start();game.Advance(10);game.Damage(13);game.Kill(game.Wave!.Active.Keys.First());
            var hp=game.CurrentHealth;var gold=state.Profile.Gold;game.Advance(180);
            Check.Equal("boss_intro",game.Stage);Check.Near(hp,game.CurrentHealth);Check.Equal(gold,state.Profile.Gold);
            Check.True(!state.Profile.UnlockedWeapons.Contains("moses_staff"));Check.Equal(3,state.Run!.RoundNumber);
            Check.Equal(0,game.DroppedExperience.Count);Check.True(game.Wave==null);Check.Near(180,game.Boss!.NormalEndedAt);state.Validate();
        });
        test("R03 protected intro has exactly 120 ticks and pauses choices reload without attacks",()=>WithStore(store=>{
            var (state,game)=Start();game.Advance(10);
            foreach(var enemy in game.Wave!.Active.Keys.Take(10).ToArray())game.Kill(enemy);
            game.Advance(180);Check.True(game.PendingChoices.Count>0);
            var world=new BattleWorld(game,CatalogReader.MonsterSpeeds(Catalog));world.Advance(1,Vector2.Zero);Check.Near(0,game.Boss!.IntroElapsed);
            RoundTransitionTests.Resolve(game);game.Pause(true);world.Advance(1,Vector2.Zero);Check.Near(0,game.Boss.IntroElapsed);game.Pause(false);
            var hp=game.CurrentHealth;game.Hit(481,99999);game.Damage(99999);Check.Near(hp,game.CurrentHealth);Check.Near(1800,game.Boss.Health);
            world.Advance(1,Vector2.Zero);state.Run!.World=world.Capture();store.Commit(state,0,"intro-mid");
            var loaded=store.Load()!;game=AdamSession.Resume(loaded,CatalogReader.Round(Catalog,3));world=BattleWorld.Restore(game,CatalogReader.MonsterSpeeds(Catalog),loaded.Run!.World!);
            for(var i=0;i<59;i++)world.Advance(BattleWorld.FixedStep,Vector2.Zero);Check.Equal("boss_intro",game.Stage);
            world.Advance(BattleWorld.FixedStep,Vector2.Zero);Check.Equal("boss",game.Stage);Check.Near(0,game.Boss!.BossElapsed);
            Check.Equal(0,world.ShotsFired);Check.Equal(0L,world.ImpactSequence);Check.Near(182,world.SimulationTime);loaded.Run!.World=world.Capture();loaded.Validate();
        }));
        test("R03 telegraph locks player target stops chase then single slam at six seconds",()=>{
            var (state,game,world)=Boss();world=Geometry(game,world,Vector2.Zero,new Vector2(30,0));
            world.Advance(4.8,Vector2.Zero);Check.True(game.Boss!.Telegraphing);Check.Near(0,game.Boss.TargetX);
            var position=world.EnemyPositions[481];var hp=game.CurrentHealth;
            world.Advance(1.1,Vector2.UnitX);Check.Equal(position,world.EnemyPositions[481]);Check.Near(0,game.Boss.TargetX);
            world.Advance(.1,Vector2.Zero);Check.Near(hp,game.CurrentHealth);Check.True(!game.Boss.Telegraphing);Check.Near(12,game.Boss.NextAttackAt);
            world=Geometry(game,world,Vector2.Zero,new Vector2(30,0));world.Advance(6,Vector2.Zero);Check.Near(hp-15,game.CurrentHealth);
            world.Advance(.2,Vector2.Zero);Check.Near(hp-15,game.CurrentHealth);state.Validate();
        });
        test("R03 threshold summons six C1 once even after disk restore and lethal hit summons none",()=>WithStore(store=>{
            var (state,game,world)=Boss();game.Hit(481,900);Check.True(game.Boss!.ThresholdUsed);Check.Equal(6,game.Boss.Supports.Count);
            world.Advance(BattleWorld.FixedStep,Vector2.Zero);game.Capture();state.Run!.World=world.Capture();store.Commit(state,0,"threshold");
            var loaded=store.Load()!;game=AdamSession.Resume(loaded,CatalogReader.Round(Catalog,3));game.Hit(481,1);
            Check.Equal(6,game.Boss!.Summoned);Check.Equal(488,game.Boss.NextActorId);
            var other=Boss();other.Game.Hit(481,1800);Check.Equal(0,other.Game.Boss!.Summoned);Check.Equal("boss_reward",other.Game.Stage);
        }));
        test("R03 kill gold excludes boss remainder reward receipt and evolution eligibility survive retry",()=>WithStore(store=>{
            var (state,game,world)=Boss();for(var i=1;i<8;i++)Check.True(game.Inventory.TryChoose("sling",false));Check.True(game.Inventory.TryChoose("stones",true));
            game.Hit(481,900);foreach(var id in game.Boss!.Supports.Take(4).Select(x=>x.Id).ToArray())game.Kill(id);
            Check.Equal(4,state.Run!.KillRemainder);var gold=state.Profile.Gold;game.Hit(481,900);
            Check.Equal(gold+100,state.Profile.Gold);Check.Equal(4,state.Run.KillRemainder);Check.True(game.Stage=="boss_reward"&&!game.Boss!.Reward!.Applied);
            Check.True(game.Boss!.Reward!.EligibleEvolutions.Contains("sling_fivefold"));Check.True(!game.Boss.Reward.Applied);
            Check.Equal(0,game.Targets.Count);store.Commit(state,0,"boss-killed");var loaded=store.Load()!;
            var game2=AdamSession.Resume(loaded,CatalogReader.Round(Catalog,3));game2.Kill(481);game2.Advance(999);
            Check.Equal(gold+100,loaded.Profile.Gold);Check.True(game2.Stage=="boss_reward"&&!game2.Boss!.Reward!.Applied);Check.Equal(3,loaded.Run!.RoundNumber);
            Check.True(!game2.Choose("sling"));Check.Equal(game.Boss.Reward.RewardId,game2.Boss!.Reward!.RewardId);
            store.Commit(state,0,"boss-killed");Check.Equal(gold+100,store.Load()!.Profile.Gold);
        }));
        test("R03 boss clock exceeds normal timeout and normal iframe does not become minutes of immunity",()=>{
            var (state,game)=Start();game.Advance(179.9);game.Damage(1);game.Advance(.1);
            var world=new BattleWorld(game,CatalogReader.MonsterSpeeds(Catalog));world.Advance(2,Vector2.Zero);
            var hp=game.CurrentHealth;game.Damage(1);Check.Near(hp-1,game.CurrentHealth);
            for(var i=0;i<7;i++){world=Geometry(game,world,new Vector2(i*100,0),new Vector2(i*100,100));world.Advance(30,Vector2.UnitX);}
            Check.Equal("boss",game.Stage);Check.True(game.Boss!.BossElapsed>180);state.Run!.World=world.Capture();state.Validate();
        });
        test("R03 player death wins over ready weapon lethal boss and same tick slam shares contact iframe",()=>{
            var (state,game,world)=Boss();world=Geometry(game,world,Vector2.Zero,Vector2.Zero);state.Run!.Health=1;game.Hit(481,1799);
            world.Advance(BattleWorld.FixedStep,Vector2.Zero);Check.True(state.Run==null);Check.Equal(60L,state.Profile.Gold);Check.Equal(0,world.BossKills);
            var other=Boss();other.World=Geometry(other.Game,other.World,Vector2.Zero,new Vector2(30,0));other.World.Advance(5.99,Vector2.Zero);
            other.World=Geometry(other.Game,other.World,Vector2.Zero,Vector2.Zero);var hp=other.Game.CurrentHealth;
            other.World.Advance(BattleWorld.FixedStep,Vector2.Zero);Check.Near(hp-12,other.Game.CurrentHealth);
        });
        test("R03 telegraph midpoint World and Run disk restore produce identical next combat",()=>WithStore(store=>{
            var (state,game,world)=Boss();world=Geometry(game,world,new Vector2(5,7),new Vector2(30,0));world.Advance(5,Vector2.Zero);
            game.Hit(481,901);world.Advance(BattleWorld.FixedStep*1.4,Vector2.UnitX);game.Capture();state.Run!.World=world.Capture();store.Commit(state,0,"boss-mid");
            var loaded=store.Load()!;var resumed=AdamSession.Resume(loaded,CatalogReader.Round(Catalog,3));var replay=BattleWorld.Restore(resumed,CatalogReader.MonsterSpeeds(Catalog),loaded.Run!.World!);
            Check.Equal(0L,replay.ImpactSequence);
            Check.Equal(JsonSerializer.Serialize(world.Capture()),JsonSerializer.Serialize(replay.Capture()));
            for(var i=0;i<120;i++){world.Advance(BattleWorld.FixedStep,Vector2.UnitX);replay.Advance(BattleWorld.FixedStep,Vector2.UnitX);}
            Check.Equal(JsonSerializer.Serialize(world.Capture()),JsonSerializer.Serialize(replay.Capture()));
            game.Capture();resumed.Capture();state.Run.World=world.Capture();loaded.Run.World=replay.Capture();
            Check.Equal(JsonSerializer.Serialize(state.Run),JsonSerializer.Serialize(loaded.Run));Check.Equal(state.Profile.Gold,loaded.Profile.Gold);
        }));
        test("R03 phase boundary discards historical impacts but preserves actual lethal boss impact",()=>{
            var (state,game)=Start();game.Advance(179.9);var speeds=CatalogReader.MonsterSpeeds(Catalog);
            var world=new BattleWorld(game,speeds);var snapshot=world.Capture();snapshot.Enemies[0].X=.1f;snapshot.Enemies[0].Y=0;
            world=BattleWorld.Restore(game,speeds,snapshot);world.Advance(BattleWorld.FixedStep,Vector2.Zero);
            var events=new System.Collections.Generic.List<BattleImpact>();world.ReadImpacts(0,events);Check.True(events.Count>0);
            var cursor=world.ImpactSequence;world.Advance(.2,Vector2.Zero);Check.Equal("boss_intro",game.Stage);
            world.ReadImpacts(cursor,events);Check.Equal(0,events.Count);world.ReadImpacts(0,events);Check.Equal(0,events.Count);
            var other=Boss();other.World=Geometry(other.Game,other.World,Vector2.Zero,new Vector2(.5f,0));other.Game.Hit(481,1799);
            other.World.Advance(BattleWorld.FixedStep,Vector2.Zero);Check.Equal("boss_reward",other.Game.Stage);Check.Equal(1,other.World.BossKills);
            other.World.ReadImpacts(0,events);Check.Equal(1,events.Count);Check.Equal(481,events[0].TargetId);Check.True(events[0].Killed);
            Check.Near(1,events[0].Damage);Check.Equal("sling",events[0].Cause);other.State.Run!.World=other.World.Capture();other.State.Validate();
        });
        test("R03 invalid clocks threshold receipts geometry and future nested boss protect originals",()=>WithStore(store=>{
            foreach(var change in new Action<GameSnapshot>[] {s=>s.Run!.Boss!.BossElapsed=double.NaN,s=>s.Run!.Boss!.NextAttackAt=0,
                s=>s.Run!.Boss!.Health=900,s=>s.Run!.Boss!.BossGoldApplied=true,s=>s.Run!.LastHitTime=999,
                s=>s.Run!.Boss!.TargetX=float.NaN,s=>s.Run!.Boss!.Summoned=6})
            {var fixture=Boss();change(fixture.State);Check.Throws<InvalidDataException>(()=>fixture.State.Validate());}
            var (state,game,world)=Boss();state.Run!.World=world.Capture();store.Commit(state,0,"before-future");
            var backup=File.ReadAllText(store.Path);File.WriteAllText(store.Path+".bak",backup);
            foreach(var change in new Action<JsonNode>[] {n=>n["Run"]!["Boss"]!["Version"]=3,n=>n["Run"]!["Boss"]!["Unknown"]=7,
                n=>n["Run"]!["World"]!["SchemaVersion"]=4,n=>n["Run"]!["Boss"]!["Reward"]=new JsonObject{["PolicyId"]="future"}})
            {
                var node=JsonNode.Parse(backup)!;change(node);var original=node.ToJsonString();File.WriteAllText(store.Path,original);
                Check.Throws<UnsupportedSaveVersionException>(()=>store.Load());Check.Throws<UnsupportedSaveVersionException>(()=>store.Commit(GameSnapshot.New(),1,"overwrite"));
                Check.Equal(original,File.ReadAllText(store.Path));Check.Equal(backup,File.ReadAllText(store.Path+".bak"));
            }
        }));
    }
}
