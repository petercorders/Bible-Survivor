using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using BibleSurvivor.Core;
using BibleSurvivor.Sim;

namespace BibleSurvivor.Core.Tests;

internal static class RoundTransitionTests
{
    private const string Catalog="content/genesis/catalog.json";
    private static RoundDefinition Adam()=>CatalogReader.Round(Catalog,1);
    private static RoundDefinition Cain()=>CatalogReader.Round(Catalog,2);
    internal static void Resolve(AdamSession game)
    {while(game.PendingChoices.Count>0)Check.True(game.Choose(game.PendingChoices[0]));}
    private static AdamSession Completed(GameSnapshot state)
    {var game=new AdamSession(state,Adam(),"sling");game.Advance(180);Resolve(game);return game;}

    public static void Register(Action<string,Action> test)
    {
        test("R02 catalog budgets, species, cap and reward are consumed by real reader",()=>{
            var round=Cain();Check.Equal("genesis_02",round.Id);Check.Equal("lightning",round.UnlockWeaponId);
            Check.Equal(35,round.CompletionGold);Check.Equal(50,round.ConcurrencyCap);
            Check.Equal(360,round.Waves.Sum(w=>w.Groups.Sum(g=>g.Count)));Check.Equal(600,round.RawExperience);
            Check.True(round.Waves.SelectMany(w=>w.Groups).Select(g=>g.MonsterId).Distinct().OrderBy(x=>x)
                .SequenceEqual(new[]{"b1","b2","b3"}));
            Check.Throws<ArgumentOutOfRangeException>(()=>CatalogReader.Round(Catalog,3));
        });
        test("R01 R02 injected budgets preserve total kills XP gold and unlocks through R03 waiting",()=>{
            var state=GameSnapshot.New();var game=new AdamSession(state,Adam(),"sling");var kills=0;var raw=0;
            for(var number=1;number<=2;number++)
            {
                for(var tick=0;tick<1801&&game.IsActive;tick++)
                {
                    Resolve(game);game.Advance(.1);
                    foreach(var enemy in game.Wave!.Active.Values.ToArray())
                    {Resolve(game);raw+=enemy.RawExperience;game.Hit(enemy.Id,enemy.Health);kills++;game.Collect(enemy.Id);Resolve(game);}
                }
                Resolve(game);Check.True(!game.IsActive);
                if(number==1)game=game.StartNextRound(Cain());
            }
            Check.Equal(600,kills);Check.Equal(900,raw);Check.Equal(180L,state.Profile.Gold);
            Check.Equal(3,state.Run!.RoundNumber);Check.Equal("between_rounds",state.Run.Stage);
            Check.True(state.Run.Wave==null);Check.Equal(0,state.Run.KillRemainder);
            Check.True(state.Profile.UnlockedWeapons.Contains("wilderness_voice"));Check.True(state.Profile.UnlockedWeapons.Contains("lightning"));
            var gold=state.Profile.Gold;game.Advance(180);Check.Equal(gold,state.Profile.Gold);
            var resumed=AdamSession.Resume(state,Cain());Check.True(!resumed.IsActive);
            Check.Throws<InvalidOperationException>(()=>resumed.StartNextRound(new RoundDefinition(3,60,1100,50,Cain().Waves)));
        });
        test("R02 concurrency cutoff and 180s timeout never award unspawned XP or kill gold",()=>{
            var state=GameSnapshot.New();var game=Completed(state).StartNextRound(Cain());
            game.Advance(169);Check.Equal(50,game.Wave!.Active.Count);game.Advance(2);
            foreach(var id in game.Wave.Active.Keys.ToArray())game.Kill(id);
            game.Advance(1);Check.Equal(50,game.Wave.SpawnedCount);Check.True(game.IsActive);
            game.Advance(8);Resolve(game);Check.Equal(70L,state.Profile.Gold); // 25+35+50/5
            Check.Equal(3,state.Run!.RoundNumber);Check.True(game.Growth.Level<10);
        });
        test("R02 entry refuses active dead wrong round and unresolved completion choices",()=>{
            var state=GameSnapshot.New();var game=new AdamSession(state,Adam(),"sling");
            Check.Throws<ArgumentException>(()=>new AdamSession(GameSnapshot.New(),Cain(),"sling"));
            Check.Throws<InvalidOperationException>(()=>game.StartNextRound(Cain()));
            game.Advance(10);foreach(var id in game.Wave!.Active.Keys.Take(7).ToArray())game.Kill(id);
            game.Advance(180);Check.True(game.PendingChoices.Count>0);
            Check.Throws<InvalidOperationException>(()=>game.StartNextRound(Cain()));
            Resolve(game);Check.Throws<InvalidOperationException>(()=>game.StartNextRound(Adam()));
            var second=game.StartNextRound(Cain());second.Damage(10000);Check.True(state.Run==null);
            Check.Throws<InvalidOperationException>(()=>second.StartNextRound(Cain()));
            Check.Throws<InvalidOperationException>(()=>game.StartNextRound(Cain()));
            Check.Throws<InvalidOperationException>(()=>AdamSession.Resume(state,Cain()));
        });
        test("R02 entry preserves progression and permanent snapshot while resetting arena and iframe",()=>{
            var state=GameSnapshot.New();state.Profile.Upgrades["attack"]=1;
            var game=new AdamSession(state,Adam(),"sling");game.Inventory.TryChoose("hair",true);game.Advance(10);
            foreach(var id in game.Wave!.Active.Keys.Take(7).ToArray()){game.Kill(id);game.Collect(id);Resolve(game);}
            game.Damage(1);game.Advance(180);Resolve(game);var previous=state.Run!;
            var xp=previous.Experience;var level=previous.Level;var weapons=previous.Weapons.ToArray();var passives=previous.Passives.ToArray();
            var runId=previous.RunId;var gold=state.Profile.Gold;state.Profile.Upgrades["attack"]=2;
            var next=game.StartNextRound(Cain());
            Check.Equal(runId,state.Run!.RunId);Check.Near(xp,next.Growth.Experience);Check.Equal(level,next.Growth.Level);
            Check.True(weapons.SequenceEqual(next.Inventory.Weapons));Check.True(passives.SequenceEqual(next.Inventory.Passives));
            Check.Equal(1,state.Run.PermanentSnapshot["attack"]);Check.Equal(2,state.Run.KillRemainder);
            Check.Equal(gold,state.Profile.Gold);Check.True(state.Run.World==null);Check.Equal(0,next.Wave!.Active.Count);
            Check.Equal(0,next.DroppedExperience.Count);Check.Near(0,next.Wave.Elapsed);Check.Near(-1,state.Run.LastHitTime);
            var hp=next.CurrentHealth;next.Damage(1);Check.Near(hp-1,next.CurrentHealth); // no old-clock invulnerability
            Check.Throws<InvalidOperationException>(()=>game.StartNextRound(Cain()));
            game.Advance(180);game.Capture();Check.True(!game.Choose("sling"));Check.Equal(2,state.Run.RoundNumber);
            Check.Equal(gold,state.Profile.Gold);Check.Near(0,next.Wave.Elapsed);
        });
        test("R02 new world starts at origin with no inherited attacks or projectiles and old world freezes",()=>{
            var state=GameSnapshot.New();var game=new AdamSession(state,Adam(),"sling");var speeds=CatalogReader.MonsterSpeeds(Catalog);
            var oldWorld=new BattleWorld(game,speeds,7);oldWorld.Advance(2,Vector2.UnitX);
            game.Advance(180);Resolve(game);
            var ended=new BattleWorld(game,speeds,7).Capture();ended.PlayerX=9;
            ended.Weapons.Add(new BattleWeaponState{WeaponId="sling",NextCycle=181,ActiveSince=179,ActiveUntil=179});
            state.Run!.World=ended;state.Validate();
            var next=game.StartNextRound(Cain());Check.True(state.Run.World==null);
            var world=new BattleWorld(next,speeds,7);var fresh=world.Capture();
            Check.Near(0,fresh.PlayerX);Check.Near(0,fresh.PlayerY);Check.Near(0,fresh.Time);
            Check.Equal(0,fresh.Weapons.Count);Check.Equal(0,fresh.Projectiles.Count);Check.Equal(0,fresh.Drops.Count);
            var oldTime=oldWorld.SimulationTime;oldWorld.Advance(1,Vector2.UnitX);Check.Near(oldTime,oldWorld.SimulationTime);
            Check.Near(0,next.Wave!.Elapsed);
        });
        test("R02 death retains permanent balance unlocks but new challenge starts R01 level1",()=>{
            var state=GameSnapshot.New();state.Profile.Upgrades["attack"]=1;var second=Completed(state).StartNextRound(Cain());
            second.Advance(10);foreach(var id in second.Wave!.Active.Keys.Take(6).ToArray())second.Kill(id);
            var gold=state.Profile.Gold;second.Damage(10000);Check.True(state.Run==null);
            var fresh=new AdamSession(state,Adam(),"bible");Check.Equal(1,fresh.Growth.Level);Check.Equal(1,state.Run!.RoundNumber);
            Check.Equal(gold,state.Profile.Gold);Check.True(state.Profile.UnlockedWeapons.Contains("wilderness_voice"));Check.Near(1.03,fresh.DamageMultiplier);
        });
        test("R02 unlock is distinct from implemented equipment and attack capability",()=>{
            var state=GameSnapshot.New();var second=Completed(state).StartNextRound(Cain());second.Advance(180);
            Check.True(state.Profile.UnlockedWeapons.Contains("lightning"));
            Check.True(!second.Inventory.TryChoose("lightning",false));Check.True(!second.Inventory.TryChoose("wilderness_voice",false));
            Check.Throws<ArgumentException>(()=>WeaponRules.Get("lightning",1));
            Check.Throws<ArgumentException>(()=>new AdamSession(GameSnapshot.New(),Adam(),"lightning"));
            Check.True(!second.Inventory.Choices(state.Profile.UnlockedWeapons,Array.Empty<string>(),3,1).Contains("lightning"));
        });
        test("R02 midpoint and pending selection restore exactly through SaveStore",()=>WithStore(store=>{
            var state=GameSnapshot.New();var game=Completed(state).StartNextRound(Cain());game.Advance(10);
            foreach(var enemy in game.Wave!.Active.Values.ToArray())
            {game.Hit(enemy.Id,enemy.Health);game.Collect(enemy.Id);if(game.PendingChoices.Count>0)break;}
            Check.True(game.PendingChoices.Count>0);var choices=game.PendingChoices.ToArray();
            store.Commit(state,0,"pending");var loaded=store.Load()!;var resumed=AdamSession.Resume(loaded,Cain());
            Check.True(choices.SequenceEqual(resumed.PendingChoices));var clock=resumed.Wave!.Elapsed;resumed.Advance(20);Check.Near(clock,resumed.Wave.Elapsed);
            Resolve(resumed);resumed.Advance(1);resumed.Capture();store.Commit(loaded,1,"continue");
            var reloaded=store.Load()!;Check.Near(clock+1,AdamSession.Resume(reloaded,Cain()).Wave!.Elapsed);
        }));
        test("R01 completion pending choices save then transition and R02 completion save restore",()=>WithStore(store=>{
            var state=GameSnapshot.New();var game=new AdamSession(state,Adam(),"sling");game.Advance(10);
            foreach(var id in game.Wave!.Active.Keys.Take(7).ToArray())game.Kill(id);game.Advance(180);
            store.Commit(state,0,"r01-end");var loaded=store.Load()!;var checkpoint=AdamSession.Resume(loaded,Adam());
            Check.True(checkpoint.PendingChoices.Count>0);Check.Throws<InvalidOperationException>(()=>checkpoint.StartNextRound(Cain()));
            Resolve(checkpoint);var second=checkpoint.StartNextRound(Cain());store.Commit(loaded,1,"r02-start");
            loaded=store.Load()!;second=AdamSession.Resume(loaded,Cain());second.Advance(10);
            foreach(var enemy in second.Wave!.Active.Values.ToArray())second.Kill(enemy.Id);second.Advance(180);
            Check.True(second.PendingChoices.Count>0);store.Commit(loaded,2,"r02-end");
            loaded=store.Load()!;var waiting=AdamSession.Resume(loaded,Cain());Check.True(waiting.PendingChoices.Count>0);
            Resolve(waiting);waiting.Advance(180);Check.Equal(3,loaded.Run!.RoundNumber);Check.Equal(64L,loaded.Profile.Gold);
        }));
        test("R02 transition staged failure and uncertain-result retry preserve one checkpoint and reward",()=>WithStore(store=>{
            var state=GameSnapshot.New();Completed(state);store.Commit(state,0,"r01-completed");
            var broken=new SaveStore(store.Path,()=>throw new IOException("power interruption"));
            void Enter(GameSnapshot s)=>AdamSession.Resume(s,Adam()).StartNextRound(Cain());
            Check.Throws<IOException>(()=>broken.Transact(1,"enter-r02",Enter));
            Check.Equal("between_rounds",store.Load()!.Run!.Stage);Check.Equal(25L,store.Load()!.Profile.Gold);
            var entered=store.Transact(1,"enter-r02",Enter);Check.Equal("normal",entered.Run!.Stage);
            var replay=store.Transact(1,"enter-r02",_=>throw new Exception("duplicate transition executed"));
            Check.Equal(entered.Revision,replay.Revision);Check.Equal(25L,replay.Profile.Gold);
            var second=AdamSession.Resume(replay,Cain());second.Advance(180);store.Commit(replay,2,"r02-complete");
            var end=store.Transact(2,"r02-complete",_=>throw new Exception("duplicate reward executed"));
            Check.Equal(60L,end.Profile.Gold);Check.Equal(1,end.Profile.UnlockedWeapons.Count(x=>x=="lightning"));
        }));
        test("Legacy schema1 R01 active and completion checkpoints migrate without inventing rewards",()=>WithStore(store=>{
            var state=GameSnapshot.New();var game=new AdamSession(state,Adam(),"bible");game.Advance(2);state.Run!.RoundFingerprint=null;
            store.Commit(state,0,"legacy-mid");var loaded=store.Load()!;var resumed=AdamSession.Resume(loaded,Adam());
            resumed.Advance(180);loaded.Run!.RoundFingerprint=null;store.Commit(loaded,1,"legacy-end");
            loaded=store.Load()!;resumed=AdamSession.Resume(loaded,Adam());var second=resumed.StartNextRound(Cain());
            Check.Equal(1,loaded.SchemaVersion);Check.Equal(25L,loaded.Profile.Gold);Check.True(loaded.Run!.RoundFingerprint!=null);
            store.Commit(loaded,2,"upgraded");Check.True(AdamSession.Resume(store.Load()!,Cain()).IsActive);
            loaded.Run.RoundFingerprint=null;Check.Throws<InvalidDataException>(()=>loaded.Validate());
        }));
        test("R02 restore rejects wrong definition and changed future catalog rewards or waves",()=>{
            var state=GameSnapshot.New();var game=Completed(state).StartNextRound(Cain());game.Advance(1);
            Check.Throws<InvalidOperationException>(()=>AdamSession.Resume(state,Adam()));var round=Cain();
            foreach(var changed in new[]{new RoundDefinition(2,50,600,36,round.Waves),
                new RoundDefinition(2,49,600,35,round.Waves),new RoundDefinition(2,50,601,35,round.Waves),
                new RoundDefinition(2,50,600,35,round.Waves,"different"),
                new RoundDefinition(2,50,600,35,round.Waves,null,"different_unlock"),
                new RoundDefinition(2,50,600,35,new[]{new WaveDefinition(0,170,new[]{new SpawnGroup("b1",360,12,5)})})})
                Check.Throws<ArgumentException>(()=>AdamSession.Resume(state,changed));
            game.Advance(180);Check.Throws<ArgumentException>(()=>AdamSession.Resume(state,new RoundDefinition(2,50,600,36,round.Waves)));
        });
        test("R02 save rejects bad stage round iframe numeric and world identity states",()=>{
            foreach(var alter in new Action<RunState>[] {r=>r.RoundNumber=3,r=>r.RoundNumber=0,r=>r.LastHitTime=1,
                r=>r.Experience=double.NaN,r=>r.Health=double.PositiveInfinity,r=>r.KillRemainder=5,r=>r.RoundFingerprint=""})
            {var state=GameSnapshot.New();Completed(state).StartNextRound(Cain());alter(state.Run!);Check.Throws<InvalidDataException>(()=>state.Validate());}
            var data=GameSnapshot.New();var game=Completed(data).StartNextRound(Cain());var world=new BattleWorld(game,CatalogReader.MonsterSpeeds(Catalog),7);
            world.Advance(2,Vector2.Zero);game.Capture();data.Run!.World=world.Capture();data.Validate();
            data.Run.World.Enemies[0].Id=999;Check.Throws<InvalidDataException>(()=>data.Validate());
            Check.Throws<ArgumentException>(()=>new RoundDefinition(2,50,600,35,new WaveDefinition[]{null!}));
        });
        test("R02 inherited first three weapons produce coordinate hits kills and restorable world",()=>{
            foreach(var weapon in new[]{"bible","jawbone","sling"})
            {
                var state=GameSnapshot.New();var first=new AdamSession(state,Adam(),weapon);
                // Budget-injected R01 preparation; all R02 damage below is generated by World attacks.
                for(var tick=0;tick<1801&&first.IsActive;tick++)
                {
                    first.Advance(.1);
                    foreach(var enemy in first.Wave!.Active.Values.ToArray())
                    {
                        first.Hit(enemy.Id,enemy.Health);first.Collect(enemy.Id);
                        while(first.PendingChoices.Count>0)Check.True(first.Choose(first.PendingChoices.Contains(weapon)?weapon:first.PendingChoices[0]));
                    }
                }
                Resolve(first);
                var game=first.StartNextRound(Cain());var speeds=CatalogReader.MonsterSpeeds(Catalog);var world=new BattleWorld(game,speeds,7);
                for(var frame=0;frame<1200&&game.IsActive&&world.Kills==0;frame++)
                {Resolve(game);world.Advance(BattleWorld.FixedStep,Vector2.Zero);}
                Resolve(game);
                if(world.Hits==0||world.Kills==0||world.ShotsFired==0||!game.IsActive)
                    throw new Exception($"{weapon}: time={world.SimulationTime}, hits={world.Hits}, kills={world.Kills}, shots={world.ShotsFired}, active={game.IsActive}");
                game.Capture();state.Run!.World=world.Capture();state.Validate();
                var restored=BattleWorld.Restore(AdamSession.Resume(state,Cain()),speeds,state.Run.World);
                Check.Equal(world.Kills,restored.Kills);Check.Near(world.SimulationTime,restored.SimulationTime);
                var altered=new Dictionary<string,double>(speeds){["b1"]=.56};
                Check.Throws<ArgumentException>(()=>BattleWorld.Restore(AdamSession.Resume(state,Cain()),altered,state.Run.World));
            }
        });
        test("R02 coordinate midpoint disk restore continues deterministically with attacks and pickup",()=>WithStore(store=>{
            var state=GameSnapshot.New();var first=new AdamSession(state,Adam(),"sling");first.Advance(180);
            var game=first.StartNextRound(Cain());var speeds=CatalogReader.MonsterSpeeds(Catalog);var world=new BattleWorld(game,speeds,13);
            for(var frame=0;frame<600;frame++){Resolve(game);world.Advance(BattleWorld.FixedStep,Vector2.UnitX);}
            Resolve(game);game.Capture();state.Run!.World=world.Capture();store.Commit(state,0,"world-mid");
            var loaded=store.Load()!;var resumed=AdamSession.Resume(loaded,Cain());var replay=BattleWorld.Restore(resumed,speeds,loaded.Run!.World!);
            for(var frame=0;frame<120;frame++)
            {Resolve(game);Resolve(resumed);world.Advance(BattleWorld.FixedStep,Vector2.UnitX);replay.Advance(BattleWorld.FixedStep,Vector2.UnitX);}
            Check.True(game.IsActive&&resumed.IsActive);
            Check.Equal(System.Text.Json.JsonSerializer.Serialize(world.Capture()),System.Text.Json.JsonSerializer.Serialize(replay.Capture()));
            Check.Near(game.CurrentHealth,resumed.CurrentHealth);Check.Equal(state.Profile.Gold,loaded.Profile.Gold);
        }));
    }
    private static void WithStore(Action<SaveStore> action)
    {
        var directory=Path.Combine(Path.GetTempPath(),"bible-r02-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(directory);
        try{action(new SaveStore(Path.Combine(directory,"save.json")));}finally{Directory.Delete(directory,true);}
    }
}
