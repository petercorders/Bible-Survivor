using System;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text.Json;
using System.Text.Json.Nodes;
using BibleSurvivor.Core;
using BibleSurvivor.Sim;
namespace BibleSurvivor.Core.Tests;
internal static class RoundElevenTests
{
    private const string Exodus="content/exodus/catalog.json";
    private static RoundDefinition Definition()=>CatalogReader.Round(Exodus,11);
    private static string Json(object? value)=>JsonSerializer.Serialize(value);
    internal static (GameSnapshot State,AdamSession Game) CompletedTen()
    {
        var (state,game)=RoundTenTests.CompletedNine();state=game.PrepareExodus(ExodusContent.Round());
        game=AdamSession.Resume(state,ExodusContent.Round());game.Advance(180);return(state,game);
    }
    private static (GameSnapshot State,AdamSession Game) Start()
    {var (state,game)=CompletedTen();return(state,game.StartNextRound(Definition()));}
    private static void Store(Action<SaveStore> action)
    {var folder=Path.Combine(Path.GetTempPath(),"r11-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(folder);try{action(new SaveStore(Path.Combine(folder,"save.json")));}finally{Directory.Delete(folder,true);}}
    public static void Register(Action<string,Action> test)
    {
        test("R11 catalog finite960 XP4000 phases HP contact speed and R10 fingerprint unchanged",()=>{
            var d=Definition();Check.Equal("exodus_02",d.Id);Check.Equal(110,d.ConcurrencyCap);Check.Equal(4000,d.RawExperience);Check.Equal(200,d.CompletionGold);Check.True(d.UnlockWeaponId==null);
            Check.Equal("240,320,400",string.Join(",",d.Waves.Select(w=>w.Groups.Sum(g=>g.Count))));
            Check.Equal("192,48,192,128,200,120,80",string.Join(",",d.Waves.SelectMany(w=>w.Groups).Select(g=>g.Count)));
            var groups=d.Waves.SelectMany(w=>w.Groups).GroupBy(g=>g.MonsterId).Select(g=>g.First()).ToArray();Check.Equal("180,140,240",string.Join(",",groups.Select(g=>g.Health)));Check.Equal("10,10,12",string.Join(",",groups.Select(g=>g.ContactDamage)));Check.Equal("0.6,0.75,0.9",string.Join(",",ExodusContent.SpeedsFor(11).Values.Select(v=>v.ToString(System.Globalization.CultureInfo.InvariantCulture))));
            var wave=new WaveDirector(d);wave.Advance(120);Check.Equal(110,wave.Active.Count);wave=new WaveDirector(d);var count=0;var xp=0;
            for(var t=0;t<170;t++){wave.Advance(1);foreach(var id in wave.Active.Keys.ToArray()){xp+=wave.Kill(id);count++;}}Check.Equal(960,count);Check.Equal(4000,xp);Check.True(wave.IsNormalComplete);
            Check.Equal("IunkZmlC50vfCA3mEtdpX5iiLjkDjk2gkFVsEXpZF9A=",ExodusContent.Round().Fingerprint);Check.Equal(ExodusContent.Round().Fingerprint,CatalogReader.Round(Exodus,10).Fingerprint);
        });
        test("R11 ordinary entry preserves RunId profile three receipts three evolutions growth with no R10 repayment",()=>Store(store=>{
            var (state,game)=CompletedTen();
            foreach(var id in new[]{"bible","jawbone","sling"})while(game.Inventory.TryChoose(id,false)){}
            foreach(var id in new[]{"scroll","hair","stones"})while(game.Inventory.TryChoose(id,true)){}
            foreach(var id in new[]{"bible","jawbone","sling"})game.Inventory.TryEvolve(EvolutionRules.Form(id));game.Capture();state.Validate();
            store.Commit(state,0,"r10");var bytes=File.ReadAllBytes(store.Path);state=store.Load()!;game=AdamSession.Resume(state,ExodusContent.Round());Check.True(!game.ContentComplete&&!game.CanEnterExodus);Check.True(bytes.SequenceEqual(File.ReadAllBytes(store.Path)));
            var run=state.Run!;var identity=Json(new{run.RunId,run.CompletedBoss,run.CompletedSecondBoss,run.CompletedThirdBoss,run.Weapons,run.Passives,run.Forms,run.PermanentSnapshot,run.Level,run.Experience,run.ChoiceSerial,run.KillRemainder,state.Profile});var gold=state.Profile.Gold;var hp=run.Health;
            var invalid=new RoundDefinition(11,110,4000,999,Definition().Waves,"exodus_02");var original=Json(state);Check.Throws<ArgumentException>(()=>game.StartNextRound(invalid));Check.Equal(original,Json(state));
            game=game.StartNextRound(Definition());Check.True(game.IsActive);Check.Equal(3,state.SchemaVersion);Check.Equal(10,run.PreviousNormalRound);Check.Equal(3,run.Forms!.Count);Check.Equal(gold,state.Profile.Gold);Check.Equal(hp,run.Health);Check.Equal(identity,Json(new{run.RunId,run.CompletedBoss,run.CompletedSecondBoss,run.CompletedThirdBoss,run.Weapons,run.Passives,run.Forms,run.PermanentSnapshot,run.Level,run.Experience,run.ChoiceSerial,run.KillRemainder,state.Profile}));Check.True(run.World==null&&run.Boss==null&&run.Wave!.Elapsed==0&&run.LastHitTime==-1);Check.True(!run.RoundCompletionApplied&&run.NormalEndedAt==null);state.Validate();
            Check.Throws<InvalidOperationException>(()=>game.StartNextRound(Definition()));Check.Equal(gold,state.Profile.Gold);
        }));
        test("R11 chase geometry pause midpoint disk Load and deterministic continuation",()=>Store(store=>{
            var (state,game)=Start();var speeds=ExodusContent.SpeedsFor(11);Check.Throws<ArgumentException>(()=>new BattleWorld(game,ExodusContent.MonsterSpeeds));
            var world=new BattleWorld(game,speeds,7);world.Advance(2,Vector2.UnitX);game.Capture();state.Run!.World=world.Capture();Check.Equal(3,state.Run.World.SchemaVersion);Check.Equal(0,world.ChargeStates.Count);Check.True(state.Run.World.HostileProjectiles==null);store.Commit(state,0,"mid");var bytes=File.ReadAllBytes(store.Path);var loaded=store.Load()!;Check.True(bytes.SequenceEqual(File.ReadAllBytes(store.Path)));var replayGame=AdamSession.Resume(loaded,Definition());var replay=BattleWorld.Restore(replayGame,speeds,loaded.Run!.World!);
            world.Advance(.5,Vector2.UnitY);replay.Advance(.5,Vector2.UnitY);Check.Equal(Json(world.Capture()),Json(replay.Capture()));game.Pause(true);var paused=Json(world.Capture());world.Advance(1,Vector2.Zero);Check.Equal(paused,Json(world.Capture()));
        }));
        test("R11 settlement SaveLoad exhausted gold25 sequential choices completion200 once heal null unlock retry",()=>Store(store=>{
            var (state,game)=Start();foreach(var id in new[]{"bible","jawbone","sling"})while(game.Inventory.TryChoose(id,false)){}
            foreach(var id in new[]{"scroll","hair","stones","east_wind","seal","cloud","thunder","crown","underwear","whirlwind"})while(game.Inventory.TryChoose(id,true)){}
            game.Capture();game.Advance(1);game.Damage(20);var enemy=game.Targets.Values.First();game.Kill(enemy.Id);state.Run!.Drops[enemy.Id]=100;game=AdamSession.Resume(state,Definition());var gold=state.Profile.Gold;var unlocks=Json(state.Profile.UnlockedWeapons);game.Advance(180);Check.Equal("round_settlement",game.Stage);Check.True(state.Run.PendingLevels>0);var pending=state.Run.PendingLevels;var serial=state.Run.ChoiceSerial;Check.Equal(gold,state.Profile.Gold);Check.True(!game.CompleteNormalSettlement());store.Commit(state,0,"settle");state=store.Load()!;game=AdamSession.Resume(state,Definition());
            var first=game.PendingChoices.Single();Check.Equal("growth_gold_25:"+serial,first);Check.True(game.Choose(first));Check.True(!game.Choose(first));store.Commit(state,1,"first-choice");Check.Equal(2L,store.Commit(state,1,"first-choice"));state=store.Load()!;game=AdamSession.Resume(state,Definition());Check.True(!game.Choose(first));RoundTransitionTests.Resolve(game);
            Check.Equal(gold+25*pending+200,state.Profile.Gold);Check.Equal(serial+pending,state.Run!.ChoiceSerial);Check.Equal(0,state.Run.PendingLevels);Check.Equal(12,state.Run.RoundNumber);Check.True(!game.ContentComplete&&!game.ChapterComplete&&!game.CanEnterExodus);Check.Equal(state.Run.MaxHealth,state.Run.Health);Check.Equal(unlocks,Json(state.Profile.UnlockedWeapons));Check.True(!game.CompleteNormalSettlement());state.Validate();var paid=state.Profile.Gold;
            var failing=new SaveStore(store.Path,()=>throw new IOException("injected before replace"));Check.Throws<IOException>(()=>failing.Commit(state,2,"done"));store.Commit(state,2,"done");Check.Equal(3L,store.Commit(state,2,"done"));state=store.Load()!;game=AdamSession.Resume(state,Definition());Check.True(!game.ContentComplete);Check.True(!game.CompleteNormalSettlement());Check.Equal(paid,state.Profile.Gold);Check.Throws<InvalidOperationException>(()=>game.StartNextRound(new RoundDefinition(13,110,4000,200,Definition().Waves,"exodus_04")));
        }));
        test("R11 unsupported R13 combat preserves primary backup and death new challenge uses R01 schema2",()=>Store(store=>{
            var (state,game)=Start();store.Commit(state,0,"start");var original=File.ReadAllText(store.Path);File.WriteAllText(store.Path+".bak",original);
            foreach(var change in new Action<JsonNode>[] {n=>n["Run"]!["RoundNumber"]=13,n=>n["Run"]!["RoundFingerprint"]=ExodusContent.Round().Fingerprint,n=>n["Run"]!["FutureR13"]=true,n=>n["SchemaVersion"]=4})
            {var node=JsonNode.Parse(original)!;change(node);var future=node.ToJsonString();File.WriteAllText(store.Path,future);Check.Throws<UnsupportedSaveVersionException>(()=>store.Load());Check.Throws<UnsupportedSaveVersionException>(()=>store.Commit(GameSnapshot.New(),1,"overwrite"));Check.Equal(future,File.ReadAllText(store.Path));Check.Equal(original,File.ReadAllText(store.Path+".bak"));}
            var profile=Json(state.Profile);game.Damage(100000);Check.True(state.Run==null);Check.Equal(profile,Json(state.Profile));state.Validate();var fresh=new AdamSession(state,CatalogReader.Round("content/genesis/catalog.json",1),"bible");Check.Equal(1,fresh.RoundNumber);Check.Equal(2,state.SchemaVersion);Check.Equal(profile,Json(state.Profile));state.Validate();
        }));
    }
}
