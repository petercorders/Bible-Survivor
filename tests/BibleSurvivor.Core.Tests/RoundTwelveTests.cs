using System;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text.Json;
using System.Text.Json.Nodes;
using BibleSurvivor.Core;
using BibleSurvivor.Sim;
namespace BibleSurvivor.Core.Tests;
internal static class RoundTwelveTests
{
    private static RoundDefinition Definition()=>CatalogReader.Exodus("content/exodus/catalog.json",12);
    private static readonly System.Collections.Generic.IReadOnlyDictionary<string,double> Speeds=CatalogReader.MonsterSpeeds("content/exodus/catalog.json");
    private static string Json(object? value)=>JsonSerializer.Serialize(value);
    private static (GameSnapshot State,AdamSession Game) Start()
    {
        var (state,game)=RoundElevenTests.CompletedTen();game=game.StartNextRound(ExodusContent.Round(11));game.Advance(180);
        return(state,game.StartNextRound(Definition()));
    }
    // Direct normal timeout and disabled weapon clocks isolate boss mechanics; not natural combat.
    private static (GameSnapshot State,AdamSession Game,BattleWorld World) Boss()
    {
        var (state,game)=Start();game.Advance(180);var world=new BattleWorld(game,Speeds,7);world.Advance(2,Vector2.Zero);
        var snapshot=world.Capture();var actor=snapshot.Enemies.Single(x=>x.Id==1021);actor.X=30;actor.Y=0;
        foreach(var id in game.Inventory.Weapons.Keys)snapshot.Weapons.Add(new BattleWeaponState{WeaponId=id,WeaponForm=game.Inventory.Forms.TryGetValue(id,out var form)?form:null,NextCycle=1000});
        return(state,game,BattleWorld.Restore(game,Speeds,snapshot));
    }
    private static void Store(Action<SaveStore> action)
    {var folder=Path.Combine(Path.GetTempPath(),"r12-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(folder);try{action(new SaveStore(Path.Combine(folder,"save.json")));}finally{Directory.Delete(folder,true);}}
    private static void Checkpoint(GameSnapshot state,AdamSession game,BattleWorld world)
    {game.Capture();state.Run!.World=world.Capture();state.Validate();}
    public static void Register(Action<string,Action> test)
    {
        test("R12 catalog1020 XP4500 cap120 exact phases stats boss and preserved R10 R11 fingerprints",()=>{
            var d=Definition();Check.Equal("exodus_03",d.Id);Check.Equal(120,d.ConcurrencyCap);Check.Equal(4500,d.RawExperience);Check.Equal(220,d.CompletionGold);Check.True(d.UnlockWeaponId==null);
            Check.Equal("270,340,410",string.Join(",",d.Waves.Select(w=>w.Groups.Sum(g=>g.Count))));Check.Equal("216,54,238,68,34,246,123,41",string.Join(",",d.Waves.SelectMany(w=>w.Groups).Select(g=>g.Count)));
            var species=d.Waves.SelectMany(w=>w.Groups).GroupBy(g=>g.MonsterId).Select(g=>g.First());Check.Equal("200,360,300",string.Join(",",species.Select(g=>g.Health)));Check.Equal("12,14,13",string.Join(",",species.Select(g=>g.ContactDamage)));
            var wave=new WaveDirector(d);var count=0;var xp=0;for(var t=0;t<170;t++){wave.Advance(1);foreach(var id in wave.Active.Keys.ToArray()){xp+=wave.Kill(id);count++;}}Check.Equal(1020,count);Check.Equal(4500,xp);
            Check.Equal("IunkZmlC50vfCA3mEtdpX5iiLjkDjk2gkFVsEXpZF9A=",ExodusContent.Round().Fingerprint);Check.Equal(ExodusContent.Round(11).Fingerprint,CatalogReader.Exodus("content/exodus/catalog.json",11).Fingerprint);
            Check.Equal("GLT9uoWhUnPu8fFrWqeQKDYBrDWEiNHqazqM+/7eHFg=",ExodusContent.Round(11).Fingerprint);
            var rules=BossDefinition.ForRound(12);Check.Near(20000,rules.MaxHealth);Check.Equal(20,rules.Contact);Check.Equal(400,rules.KillGold);Check.Near(6,rules.AttackPeriod);Check.Near(.6,rules.DashDuration);Check.Near(1.3,rules.DashSpeedRatio);Check.Equal(0,rules.AttackDamage);
        });
        test("R12 ordinary entry three receipts and protected intro no early heal gold or clock",()=>{
            var (state,game)=Start();var receipts=Json(new{state.Run!.CompletedBoss,state.Run.CompletedSecondBoss,state.Run.CompletedThirdBoss});var gold=state.Profile.Gold;
            game.Advance(1);game.Damage(20);var hp=game.CurrentHealth;game.Advance(180);Check.Equal("boss_intro",game.Stage);Check.Equal(4,game.Boss!.Version);Check.Equal(11,state.Run.PreviousNormalRound);Check.Near(hp,game.CurrentHealth);Check.Equal(gold,state.Profile.Gold);
            var world=new BattleWorld(game,Speeds);game.Pause(true);world.Advance(3,Vector2.Zero);Check.Near(0,game.Boss.IntroElapsed);game.Pause(false);world.Advance(2,Vector2.Zero);Check.Equal("boss",game.Stage);Check.Near(0,game.Boss.BossElapsed);Check.Equal(5,world.Capture().SchemaVersion);Check.Equal(receipts,Json(new{state.Run.CompletedBoss,state.Run.CompletedSecondBoss,state.Run.CompletedThirdBoss}));Checkpoint(state,game,world);
        });
        test("R12 ex9 reuses D3 end cooldown fixed direction and normal checkpoint",()=>Store(store=>{
            var (state,game)=Start();for(var t=0;t<60;t++){game.Advance(1);foreach(var id in game.Targets.Keys.ToArray())game.Kill(id);}game.Advance(3);state.Run!.Drops.Clear();var world=new BattleWorld(game,Speeds,7);var initial=world.ChargeStates.First();var actor=game.Targets[initial.Key];Check.Equal("ex9",actor.MonsterId);Check.Near(actor.SpawnedAt!.Value+6,initial.Value.NextAt);
            var snapshot=world.Capture();snapshot.Enemies.ForEach(x=>{x.X=30+x.Id%10;x.Y=20;});foreach(var id in game.Inventory.Weapons.Keys)snapshot.Weapons.Add(new BattleWeaponState{WeaponId=id,WeaponForm=game.Inventory.Forms.TryGetValue(id,out var form)?form:null,NextCycle=1000});world=BattleWorld.Restore(game,Speeds,snapshot);
            world.Advance(6,Vector2.Zero);Check.Equal("telegraph",world.ChargeStates[initial.Key].Phase);var direction=Json(world.ChargeStates[initial.Key]);Checkpoint(state,game,world);store.Commit(state,0,"ex9");var loaded=store.Load()!;var replayGame=AdamSession.Resume(loaded,Definition());var replay=BattleWorld.Restore(replayGame,Speeds,loaded.Run!.World!);
            world.Advance(1.5,Vector2.UnitY);replay.Advance(1.5,Vector2.UnitY);Check.Equal(Json(world.Capture()),Json(replay.Capture()));Check.Equal("charge",world.ChargeStates[initial.Key].Phase);Check.True(direction.Contains("telegraph"));world.Advance(.3,Vector2.Zero);var charge=world.ChargeStates[initial.Key];Check.Equal("cooldown",charge.Phase);Check.Near(actor.SpawnedAt!.Value+6+1.2+.6+6,charge.NextAt,1e-6);
        }));
        test("R12 boss absolute4.8 warn6 dash6.6 chase10.8 nextwarn fixed live geometry pause copy",()=>{
            var (state,game,world)=Boss();world.Advance(4.8,Vector2.Zero);Check.True(game.Boss!.Telegraphing);var origin=world.ChariotDash!.Origin;var direction=world.ChariotDash.Direction;Check.Near(-1,direction.X);Check.Near(.85,world.ChariotDash.DangerHalfWidth);Check.Near(2.34,world.ChariotDash.RemainingDistance);
            var copy=game.Boss;copy.ChariotCharge!.DirectionX=1;Check.Near(-1,game.Boss.ChariotCharge!.DirectionX);game.Pause(true);var frozen=Json(world.Capture());world.Advance(1,Vector2.UnitY);Check.Equal(frozen,Json(world.Capture()));game.Pause(false);
            world.Advance(1.2,Vector2.UnitY);Check.Equal(origin,world.EnemyPositions[1021]);Check.Equal("dash",game.Boss.ChariotCharge!.Phase);Check.Near(12,game.Boss.NextAttackAt);Check.Equal(direction,world.ChariotDash!.Direction);
            world.Advance(.3,Vector2.UnitY);Check.Near(1.17,Vector2.Distance(origin,world.EnemyPositions[1021]),1e-4);Check.Equal(world.EnemyPositions[1021],world.ChariotDash!.Origin);Check.Near(1.17,world.ChariotDash.RemainingDistance,1e-6);
            world.Advance(.3,Vector2.UnitY);Check.Equal("chase",game.Boss.ChariotCharge!.Phase);Check.Near(2.34,Vector2.Distance(origin,world.EnemyPositions[1021]),1e-4);world.Advance(4.2,Vector2.UnitY);Check.True(game.Boss.Telegraphing);Check.Near(10.8,game.Boss.BossElapsed,1e-7);Checkpoint(state,game,world);
            state.Run!.PendingLevels=1;var clock=game.Boss.BossElapsed;world.Advance(1,Vector2.UnitY);Check.Near(clock,game.Boss.BossElapsed);state.Run.PendingLevels=0;
        });
        test("R12 boss telegraph dash and chase disk replay exact phase direction deadline position",()=>Store(store=>{
            var (state,game,world)=Boss();long revision=0;
            foreach(var duration in new[]{5.1,1.2,.5})
            {
                world.Advance(duration,Vector2.UnitY);Checkpoint(state,game,world);var expected=Json(game.Boss);revision=store.Commit(state,revision,"phase"+revision);var loaded=store.Load()!;var replayGame=AdamSession.Resume(loaded,Definition());var replay=BattleWorld.Restore(replayGame,Speeds,loaded.Run!.World!);Check.Equal(expected,Json(replayGame.Boss));Check.Equal(Json(world.Capture()),Json(replay.Capture()));
                world.Advance(.1,Vector2.UnitX);replay.Advance(.1,Vector2.UnitX);Check.Equal(Json(world.Capture()),Json(replay.Capture()));Check.Equal(Json(game.Boss),Json(replayGame.Boss));
            }
        }));
        test("R12 relative swept body contact20 armor iframe moving player no slam packet and overlap direction",()=>{
            var (state,game,world)=Boss();world.Advance(6,Vector2.Zero);var snapshot=world.Capture();snapshot.Tuning.PlayerSpeed=300;var actor=snapshot.Enemies.Single(x=>x.Id==1021);actor.X=-3;actor.Y=0;state.Run!.Boss!.ChariotCharge!.DirectionX=1;state.Run.Boss.ChariotCharge.DirectionY=0;
            world=BattleWorld.Restore(game,Speeds,snapshot);var hp=game.CurrentHealth;var damage=20-(game.Inventory.Passives.TryGetValue("seal",out var armor)?armor:0);world.Advance(BattleWorld.FixedStep,-Vector2.UnitX);Check.Near(hp-damage,game.CurrentHealth);Check.True(Vector2.Distance(world.PlayerPosition,world.EnemyPositions[1021])>.85);world.Advance(BattleWorld.FixedStep,Vector2.Zero);Check.Near(hp-damage,game.CurrentHealth);Check.Equal(0,world.HostileProjectiles.Count);
            var overlap=Boss();overlap.World.Advance(4.8-BattleWorld.FixedStep,Vector2.Zero);snapshot=overlap.World.Capture();actor=snapshot.Enemies.Single(x=>x.Id==1021);actor.X=snapshot.PlayerX;actor.Y=snapshot.PlayerY;overlap.World=BattleWorld.Restore(overlap.Game,Speeds,snapshot);overlap.World.Advance(BattleWorld.FixedStep,Vector2.Zero);Check.Near(1,overlap.Game.Boss!.ChariotCharge!.DirectionX);Check.Near(0,overlap.Game.Boss.ChariotCharge.DirectionY);
        });
        test("R12 threshold supports6 then12 once lethal skips kill cleans dash and player death wins",()=>{
            var (state,game,world)=Boss();game.Hit(1021,8000);Check.Equal(6,game.Boss!.Supports.Count);game.Hit(1021,6000);Check.Equal(12,game.Boss.Summoned);game.Hit(1021,1);Check.Equal(12,game.Boss.Summoned);world.Advance(BattleWorld.FixedStep,Vector2.Zero);Checkpoint(state,game,world);
            var lethal=Boss();lethal.World.Advance(6.1,Vector2.Zero);lethal.Game.Hit(1021,20000);Check.Equal(0,lethal.Game.Boss!.Summoned);Check.Equal("chase",lethal.Game.Boss.ChariotCharge!.Phase);Check.True(!lethal.Game.Boss.Telegraphing);Check.True(lethal.World.ChariotDash==null);lethal.World.Advance(1,Vector2.Zero);Check.Equal(0,lethal.World.EnemyPositions.Count);Check.Equal(0,lethal.World.ChargeStates.Count);
            var death=Boss();var snapshot=death.World.Capture();var actor=snapshot.Enemies.Single(x=>x.Id==1021);actor.X=.8f;actor.Y=0;snapshot.Weapons.Clear();death.World=BattleWorld.Restore(death.Game,Speeds,snapshot);death.Game.Hit(1021,19999);death.State.Run!.Health=1;death.State.Run.LastHitTime=-1;var gold=death.State.Profile.Gold;death.World.Advance(BattleWorld.FixedStep,Vector2.Zero);Check.True(death.State.Run==null);Check.Equal(gold,death.State.Profile.Gold);Check.Equal(0,death.World.BossKills);
        });
        test("R12 reward frozen candidates load no grant once400 choice remaining growth220 heal three receipts",()=>Store(store=>{
            var (state,game,world)=Boss();var receipts=Json(new{state.Run!.CompletedBoss,state.Run.CompletedSecondBoss,state.Run.CompletedThirdBoss});var gold=state.Profile.Gold;game.Hit(1021,20000);Check.Equal(gold+400,state.Profile.Gold);var candidates=Json(game.BossChoices);store.Commit(state,0,"reward");state=store.Load()!;game=AdamSession.Resume(state,Definition());Check.Equal(gold+400,state.Profile.Gold);Check.Equal(candidates,Json(game.BossChoices));var choice=game.BossChoices.First();var reward=game.Boss!.Reward!.RewardId;Check.True(game.ChooseBossReward(reward,choice.Id));Check.True(!game.ChooseBossReward(reward,choice.Id));RoundTransitionTests.Resolve(game);Check.True(game.CompleteBossSettlement());Check.True(!game.CompleteBossSettlement());Check.Equal(gold+620+(choice.Kind=="gold"?100:0),state.Profile.Gold);Check.True(game.ContentComplete&&!game.ChapterComplete);Check.Equal(13,state.Run!.RoundNumber);Check.Near(state.Run.MaxHealth,game.CurrentHealth);Check.Equal(receipts,Json(new{state.Run.CompletedBoss,state.Run.CompletedSecondBoss,state.Run.CompletedThirdBoss}));store.Commit(state,1,"complete");state=store.Load()!;game=AdamSession.Resume(state,Definition());Check.True(game.ContentComplete);Check.True(!game.CompleteBossSettlement());Check.Throws<InvalidOperationException>(()=>game.StartNextRound(new RoundDefinition(13,120,4500,220,Definition().Waves,"exodus_04")));state.Validate();
        }));
        test("R12 exhausted100 pending growth25 completion220 once and selection freezes boss clock",()=>Store(store=>{
            var (state,game)=Start();foreach(var id in new[]{"bible","jawbone","sling"})while(game.Inventory.TryChoose(id,false)){}foreach(var id in new[]{"scroll","hair","stones","east_wind","seal","cloud","thunder","crown","underwear","whirlwind"})while(game.Inventory.TryChoose(id,true)){}foreach(var id in new[]{"bible","jawbone","sling"})game.Inventory.TryEvolve(EvolutionRules.Form(id));game.Capture();game.Advance(1);var enemy=game.Targets.Values.First();game.Kill(enemy.Id);state.Run!.Drops[enemy.Id]=100;game=AdamSession.Resume(state,Definition());game.Advance(180);var pending=state.Run.PendingLevels;Check.True(pending>0);var world=new BattleWorld(game,Speeds);world.Advance(3,Vector2.Zero);Check.Near(0,game.Boss!.IntroElapsed);RoundTransitionTests.Resolve(game);world.Advance(2,Vector2.Zero);var gold=state.Profile.Gold;game.Hit(1021,20000);Check.Equal("boss_settlement",game.Stage);Check.Equal(gold+500,state.Profile.Gold);state.Run!.PendingLevels=2;state.Run.Choices.Add(AdamSession.GrowthGoldChoicePrefix+state.Run.ChoiceSerial);store.Commit(state,0,"exhausted");state=store.Load()!;game=AdamSession.Resume(state,Definition());Check.Equal(gold+500,state.Profile.Gold);Check.True(!game.CompleteBossSettlement());var first=game.PendingChoices.Single();Check.True(game.Choose(first));Check.True(!game.Choose(first));store.Commit(state,1,"growth-one");state=store.Load()!;game=AdamSession.Resume(state,Definition());Check.Equal(1,state.Run!.PendingLevels);RoundTransitionTests.Resolve(game);Check.True(game.CompleteBossSettlement());Check.Equal(gold+770,state.Profile.Gold);Check.True(!game.CompleteBossSettlement());state.Validate();
        }));
        test("R12 future R13 boss5 world6 phase unknown original protection death R01 schema2",()=>Store(store=>{
            var (state,game,world)=Boss();world.Advance(5,Vector2.Zero);Checkpoint(state,game,world);store.Commit(state,0,"supported");var original=File.ReadAllText(store.Path);File.WriteAllText(store.Path+".bak",original);
            foreach(var change in new Action<JsonNode>[] {n=>n["Run"]!["RoundNumber"]=13,n=>n["Run"]!["Boss"]!["Version"]=5,n=>n["Run"]!["World"]!["SchemaVersion"]=6,n=>n["Run"]!["Boss"]!["ChariotCharge"]!["Phase"]="future",n=>n["Run"]!["Boss"]!["ChariotCharge"]!["Future"]=true})
            {var node=JsonNode.Parse(original)!;change(node);var future=node.ToJsonString();File.WriteAllText(store.Path,future);Check.Throws<UnsupportedSaveVersionException>(()=>store.Load());Check.Throws<UnsupportedSaveVersionException>(()=>store.Commit(GameSnapshot.New(),1,"overwrite"));Check.Equal(future,File.ReadAllText(store.Path));Check.Equal(original,File.ReadAllText(store.Path+".bak"));}
            var profile=Json(state.Profile);game.Damage(100000);Check.True(state.Run==null);Check.Equal(profile,Json(state.Profile));var fresh=new AdamSession(state,CatalogReader.Round("content/genesis/catalog.json",1),"bible");Check.Equal(2,state.SchemaVersion);Check.Equal(1,fresh.RoundNumber);Check.Equal(profile,Json(state.Profile));
        }));
    }
}
