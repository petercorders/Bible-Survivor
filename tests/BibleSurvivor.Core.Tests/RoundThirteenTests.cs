using System;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text.Json;
using System.Text.Json.Nodes;
using BibleSurvivor.Core;
using BibleSurvivor.Sim;
namespace BibleSurvivor.Core.Tests;
internal static class RoundThirteenTests
{
    private static RoundDefinition Definition()=>CatalogReader.Exodus("content/exodus/catalog.json",13);
    private static string Json(object? value)=>JsonSerializer.Serialize(value);
    private static readonly System.Collections.Generic.IReadOnlyDictionary<string,double> Speeds=CatalogReader.MonsterSpeeds("content/exodus/catalog.json");
    // Direct timeouts, full equipment and direct boss hit isolate transition/economy, not natural play.
    internal static (GameSnapshot State,AdamSession Game) CompletedTwelve()
    {
        var (state,game)=RoundElevenTests.CompletedTen();game=game.StartNextRound(ExodusContent.Round(11));game.Advance(180);
        foreach(var id in new[]{"bible","jawbone","sling"})while(game.Inventory.TryChoose(id,false)){}
        foreach(var id in new[]{"scroll","hair","stones","east_wind","seal","cloud","thunder","crown","underwear","whirlwind"})while(game.Inventory.TryChoose(id,true)){}
        foreach(var id in new[]{"bible","jawbone","sling"})game.Inventory.TryEvolve(EvolutionRules.Form(id));game.Capture();
        game=game.StartNextRound(ExodusContent.Round(12));var gold=state.Profile.Gold;
        game.Advance(180);var world=new BattleWorld(game,Speeds,7);world.Advance(2,Vector2.Zero);game.Hit(1021,20000);
        Check.Equal("boss_settlement",game.Stage);Check.Equal(gold+500,state.Profile.Gold);Check.True(game.CompleteBossSettlement());Check.Equal(gold+720,state.Profile.Gold);
        state.Validate();return(state,game);
    }
    private static (GameSnapshot State,AdamSession Game) Start()
    {var (state,game)=CompletedTwelve();return(state,game.StartNextRound(Definition()));}
    private static void Store(Action<SaveStore> action)
    {var folder=Path.Combine(Path.GetTempPath(),"r13-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(folder);try{action(new SaveStore(Path.Combine(folder,"save.json")));}finally{Directory.Delete(folder,true);}}
    private static string Identity(GameSnapshot state)=>Json(new{state.Run!.RunId,state.Run.CompletedBoss,state.Run.CompletedSecondBoss,state.Run.CompletedThirdBoss,state.Run.Weapons,state.Run.Passives,state.Run.Forms,state.Run.PermanentSnapshot,state.Run.Level,state.Run.Experience,state.Run.ChoiceSerial,state.Run.KillRemainder,state.Profile});
    public static void Register(Action<string,Action> test)
    {
        test("R13 catalog1080 XP4800 cap110 exact waves stats chase no boss and historical fingerprints",()=>{
            var d=Definition();Check.Equal("exodus_04",d.Id);Check.Equal(13,d.Number);Check.Equal(110,d.ConcurrencyCap);Check.Equal(4800,d.RawExperience);Check.Equal(240,d.CompletionGold);Check.True(d.UnlockWeaponId==null);
            Check.Equal("280,360,440",string.Join(",",d.Waves.Select(w=>w.Groups.Sum(g=>g.Count))));Check.Equal("224,56,216,144,220,132,88",string.Join(",",d.Waves.SelectMany(w=>w.Groups).Select(g=>g.Count)));
            var groups=d.Waves.SelectMany(w=>w.Groups).GroupBy(g=>g.MonsterId).Select(g=>g.First()).ToArray();Check.Equal("150,280,600",string.Join(",",groups.Select(g=>g.Health)));Check.Equal("10,12,15",string.Join(",",groups.Select(g=>g.ContactDamage)));Check.Equal("0.6,0.6,0.35",string.Join(",",ExodusContent.SpeedsFor(13).Values.Select(v=>v.ToString(System.Globalization.CultureInfo.InvariantCulture))));
            Check.True(groups.All(g=>!BattleWorld.UsesNormalCharge(13,g.MonsterId)));var wave=new WaveDirector(d);wave.Advance(120);Check.Equal(110,wave.Active.Count);wave=new WaveDirector(d);var kills=0;var xp=0;
            for(var t=0;t<170;t++){wave.Advance(1);foreach(var id in wave.Active.Keys.ToArray()){xp+=wave.Kill(id);kills++;}}Check.Equal(1080,kills);Check.Equal(4800,xp);Check.True(wave.IsNormalComplete);
            Check.Equal("IunkZmlC50vfCA3mEtdpX5iiLjkDjk2gkFVsEXpZF9A=",ExodusContent.Round(10).Fingerprint);Check.Equal("GLT9uoWhUnPu8fFrWqeQKDYBrDWEiNHqazqM+/7eHFg=",ExodusContent.Round(11).Fingerprint);Check.Equal(ExodusContent.Round(12).Fingerprint,CatalogReader.Exodus("content/exodus/catalog.json",12).Fingerprint);
        });
        test("R13 R12 disk entry preserves four receipts identity growth profile and no720 repayment fresh clocks",()=>Store(store=>{
            var (state,game)=CompletedTwelve();var run=state.Run!;var identity=Identity(state);var fourth=Json(run.Boss);var gold=state.Profile.Gold;
            Check.Equal(4,run.Boss!.Version);Check.Equal(12,run.Boss.EncounterRound);store.Commit(state,0,"r12-complete");var bytes=File.ReadAllBytes(store.Path);state=store.Load()!;game=AdamSession.Resume(state,ExodusContent.Round(12));Check.True(bytes.SequenceEqual(File.ReadAllBytes(store.Path)));Check.Equal(identity,Identity(state));Check.True(!game.ContentComplete);
            var original=Json(state);Check.Throws<ArgumentException>(()=>game.StartNextRound(new RoundDefinition(13,110,4800,999,Definition().Waves,"exodus_04")));Check.Equal(original,Json(state));
            game=game.StartNextRound(Definition());run=state.Run!;Check.Equal(identity,Identity(state));Check.Equal(fourth,Json(run.CompletedFourthBoss));Check.Equal(gold,state.Profile.Gold);Check.Equal(3,state.SchemaVersion);Check.Equal(12,run.PreviousNormalRound);Check.True(run.Boss==null&&run.World==null&&run.LastHitTime==-1&&run.Wave!.Elapsed==0);Check.True(!run.RoundCompletionApplied&&run.NormalEndedAt==null);Check.True(game.BossRules==null&&game.BossChoices.Count==0&&!game.CanEnterExodus);
            Check.Throws<InvalidOperationException>(()=>game.StartNextRound(Definition()));store.Commit(state,1,"r13-entry");state=store.Load()!;game=AdamSession.Resume(state,Definition());Check.Equal(identity,Identity(state));Check.Equal(fourth,Json(state.Run!.CompletedFourthBoss));Check.Equal(gold,state.Profile.Gold);
            var world=new BattleWorld(game,Speeds,7);var fresh=world.Capture();Check.Near(0,fresh.Time);Check.Equal(3,fresh.SchemaVersion);Check.Equal(0,fresh.Charges!.Count);Check.True(fresh.HostileProjectiles==null&&fresh.NextHostileId==0&&fresh.Projectiles.Count==0&&world.ChariotDash==null);state.Run.World=fresh;state.Validate();
        }));
        test("R13 World3 chase pause midpoint disk Load deterministic continuation rejects inherited dash",()=>Store(store=>{
            var (state,game)=Start();Check.Throws<ArgumentException>(()=>new BattleWorld(game,ExodusContent.SpeedsFor(12)));var world=new BattleWorld(game,Speeds,7);world.Advance(2,Vector2.UnitX);game.Capture();state.Run!.World=world.Capture();state.Validate();store.Commit(state,0,"mid");var loaded=store.Load()!;var replayGame=AdamSession.Resume(loaded,Definition());var replay=BattleWorld.Restore(replayGame,Speeds,loaded.Run!.World!);
            world.Advance(.5,Vector2.UnitY);replay.Advance(.5,Vector2.UnitY);Check.Equal(Json(world.Capture()),Json(replay.Capture()));Check.Equal(0,world.ChargeStates.Count);Check.True(game.Targets.Values.All(x=>!x.SpawnedAt.HasValue));
            game.Pause(true);var paused=Json(world.Capture());world.Advance(1,Vector2.Zero);Check.Equal(paused,Json(world.Capture()));game.Pause(false);
            var bad=world.Capture();bad.Charges!.Add(new EnemyChargeState{Id=game.Targets.Keys.First(),Phase="cooldown",NextAt=8});Check.Throws<ArgumentException>(()=>BattleWorld.Restore(game,Speeds,bad));
            game.Capture();state.Run.World=bad;Check.Throws<InvalidDataException>(()=>state.Validate());
        }));
        test("R13 settlement pending versus candidates choices SaveLoad failure retry growth25 completion240 once",()=>Store(store=>{
            var (state,game)=Start();game.Advance(1);game.Damage(20);var enemy=game.Targets.Values.First();game.Kill(enemy.Id);state.Run!.Drops[enemy.Id]=100;game=AdamSession.Resume(state,Definition());var gold=state.Profile.Gold;var unlocks=Json(state.Profile.UnlockedWeapons);var receipts=Json(new{state.Run.CompletedBoss,state.Run.CompletedSecondBoss,state.Run.CompletedThirdBoss,state.Run.CompletedFourthBoss});
            game.Advance(180);Check.Equal("round_settlement",game.Stage);Check.True(game.Boss==null);var pending=state.Run.PendingLevels;var serial=state.Run.ChoiceSerial;Check.True(pending>1);Check.Equal(1,game.PendingChoices.Count);Check.Equal(gold,state.Profile.Gold);Check.True(!game.CompleteNormalSettlement());store.Commit(state,0,"settle");state=store.Load()!;game=AdamSession.Resume(state,Definition());Check.Equal(pending,state.Run!.PendingLevels);Check.Equal(1,game.PendingChoices.Count);
            var chosen=0;var first=game.PendingChoices.Single();Check.True(game.Choose(first));chosen++;Check.True(!game.Choose(first));var failing=new SaveStore(store.Path,()=>throw new IOException("injected before replace"));Check.Throws<IOException>(()=>failing.Commit(state,1,"first-choice"));Check.Equal(gold,store.Load()!.Profile.Gold);Check.Equal(2L,store.Commit(state,1,"first-choice"));Check.Equal(2L,store.Commit(state,1,"first-choice"));state=store.Load()!;game=AdamSession.Resume(state,Definition());Check.True(!game.Choose(first));
            while(game.PendingChoices.Count>0){Check.True(game.Choose(game.PendingChoices.Single()));chosen++;}
            Check.Equal(pending,chosen);Check.Equal(serial+chosen,state.Run!.ChoiceSerial);Check.Equal(0,state.Run.PendingLevels);Check.Equal(0,game.PendingChoices.Count);Check.Equal(gold+25*chosen+240,state.Profile.Gold);Check.Equal(14,state.Run.RoundNumber);Check.Equal("between_rounds",game.Stage);Check.True(game.ContentComplete&&!game.ChapterComplete&&!game.CanEnterExodus);Check.Equal(state.Run.MaxHealth,state.Run.Health);Check.Equal(unlocks,Json(state.Profile.UnlockedWeapons));Check.Equal(receipts,Json(new{state.Run.CompletedBoss,state.Run.CompletedSecondBoss,state.Run.CompletedThirdBoss,state.Run.CompletedFourthBoss}));Check.True(!game.CompleteNormalSettlement());
            var paid=state.Profile.Gold;Check.Throws<IOException>(()=>failing.Commit(state,2,"complete"));Check.Equal(3L,store.Commit(state,2,"complete"));Check.Equal(3L,store.Commit(state,2,"complete"));state=store.Load()!;game=AdamSession.Resume(state,Definition());Check.Equal(paid,state.Profile.Gold);Check.True(game.ContentComplete&&!game.CompleteNormalSettlement());Check.Throws<InvalidOperationException>(()=>game.StartNextRound(new RoundDefinition(14,110,4800,240,Definition().Waves,"exodus_05")));state.Validate();
            Console.WriteLine($"R13 settlement evidence pending={pending} candidates=1 successful_Choose={chosen} growth_gold={25*chosen} completion_gold=240");
        }));
        test("R13 future R14 World5 boss5 receipt unknown members preserve primary backup",()=>Store(store=>{
            var (state,game)=Start();var world=new BattleWorld(game,Speeds,7);world.Advance(1,Vector2.Zero);game.Capture();state.Run!.World=world.Capture();store.Commit(state,0,"supported");var original=File.ReadAllText(store.Path);File.WriteAllText(store.Path+".bak",original);
            foreach(var change in new Action<JsonNode>[] {n=>n["Run"]!["RoundNumber"]=14,n=>n["Run"]!["Stage"]="boss",n=>n["Run"]!["World"]!["SchemaVersion"]=5,n=>n["Run"]!["FutureR14"]=true,n=>n["Run"]!["CompletedFourthBoss"]!["Version"]=5,n=>n["Run"]!["CompletedFourthBoss"]!["ChariotCharge"]!["Phase"]="future",n=>n["Run"]!["CompletedFourthBoss"]!["Future"]=true,n=>n["SchemaVersion"]=4})
            {var node=JsonNode.Parse(original)!;change(node);var future=node.ToJsonString();File.WriteAllText(store.Path,future);Check.Throws<UnsupportedSaveVersionException>(()=>store.Load());Check.Throws<UnsupportedSaveVersionException>(()=>store.Commit(GameSnapshot.New(),1,"overwrite"));Check.Equal(future,File.ReadAllText(store.Path));Check.Equal(original,File.ReadAllText(store.Path+".bak"));}
        }));
        test("R13 four history required death and explicit new challenge preserve profile R01 schema2",()=>{
            var (state,game)=Start();var receipt=state.Run!.CompletedFourthBoss;state.Run.CompletedFourthBoss=null;Check.Throws<InvalidDataException>(()=>state.Validate());state.Run.CompletedFourthBoss=receipt;
            receipt!.CompletionApplied=false;Check.Throws<InvalidDataException>(()=>state.Validate());receipt.CompletionApplied=true;state.Validate();
            var profile=Json(state.Profile);game.Damage(100000);Check.True(state.Run==null);Check.Equal(profile,Json(state.Profile));var fresh=new AdamSession(state,CatalogReader.Round("content/genesis/catalog.json",1),"bible");Check.Equal(2,state.SchemaVersion);Check.Equal(1,fresh.RoundNumber);Check.Equal(profile,Json(state.Profile));state.Validate();
            (state,game)=Start();profile=Json(state.Profile);state.Run=null;fresh=new AdamSession(state,CatalogReader.Round("content/genesis/catalog.json",1),"sling");Check.Equal(2,state.SchemaVersion);Check.Equal(1,fresh.RoundNumber);Check.Equal(profile,Json(state.Profile));Check.True(state.Run!.CompletedFourthBoss==null);state.Validate();
        });
    }
}
