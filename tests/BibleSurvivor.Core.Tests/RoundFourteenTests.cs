using System;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text.Json;
using System.Text.Json.Nodes;
using BibleSurvivor.Core;
using BibleSurvivor.Sim;
namespace BibleSurvivor.Core.Tests;
internal static class RoundFourteenTests
{
    private static RoundDefinition Definition()=>CatalogReader.Exodus("content/exodus/catalog.json",14);
    private static string Json(object? value)=>JsonSerializer.Serialize(value);
    private static readonly System.Collections.Generic.IReadOnlyDictionary<string,double> Speeds=CatalogReader.MonsterSpeeds("content/exodus/catalog.json");
    // Reuse the existing full-equipment/timeout/direct-boss-hit technical fixture.
    internal static (GameSnapshot State,AdamSession Game) CompletedThirteen()
    {var (state,game)=RoundThirteenTests.CompletedTwelve();game=game.StartNextRound(ExodusContent.Round(13));game.Advance(180);state.Validate();return(state,game);}
    private static (GameSnapshot State,AdamSession Game) Start()
    {var (state,game)=CompletedThirteen();return(state,game.StartNextRound(Definition()));}
    private static void Store(Action<SaveStore> action)
    {var folder=Path.Combine(Path.GetTempPath(),"r14-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(folder);try{action(new SaveStore(Path.Combine(folder,"save.json")));}finally{Directory.Delete(folder,true);}}
    private static string Identity(GameSnapshot state)=>Json(new{state.Run!.RunId,state.Run.CompletedBoss,state.Run.CompletedSecondBoss,state.Run.CompletedThirdBoss,state.Run.CompletedFourthBoss,state.Run.Weapons,state.Run.Passives,state.Run.Forms,state.Run.PermanentSnapshot,state.Run.Level,state.Run.Experience,state.Run.ChoiceSerial,state.Run.KillRemainder,state.Profile});
    public static void Register(Action<string,Action> test)
    {
        test("R14 catalog1140 XP5200 cap120 exact waves stats and preserved R13 fingerprint",()=>{
            var d=Definition();Check.Equal("exodus_05",d.Id);Check.Equal(14,d.Number);Check.Equal(120,d.ConcurrencyCap);Check.Equal(5200,d.RawExperience);Check.Equal(260,d.CompletionGold);Check.True(d.UnlockWeaponId==null);
            Check.Equal("300,380,460",string.Join(",",d.Waves.Select(w=>w.Groups.Sum(g=>g.Count))));Check.Equal("240,60,266,114,230,138,92",string.Join(",",d.Waves.SelectMany(w=>w.Groups).Select(g=>g.Count)));
            var groups=d.Waves.SelectMany(w=>w.Groups).GroupBy(g=>g.MonsterId).Select(g=>g.First()).ToArray();Check.Equal("220,700,360",string.Join(",",groups.Select(g=>g.Health)));Check.Equal("12,15,14",string.Join(",",groups.Select(g=>g.ContactDamage)));Check.Equal("0.8,0.35,0.6",string.Join(",",ExodusContent.SpeedsFor(14).Values.Select(v=>v.ToString(System.Globalization.CultureInfo.InvariantCulture))));
            Check.True(groups.All(g=>!BattleWorld.UsesNormalCharge(14,g.MonsterId)));var wave=new WaveDirector(d);wave.Advance(120);Check.Equal(120,wave.Active.Count);wave=new WaveDirector(d);var kills=0;var xp=0;
            for(var t=0;t<170;t++){wave.Advance(1);foreach(var id in wave.Active.Keys.ToArray()){xp+=wave.Kill(id);kills++;}}Check.Equal(1140,kills);Check.Equal(5200,xp);Check.True(wave.IsNormalComplete);
            Check.Equal("IunkZmlC50vfCA3mEtdpX5iiLjkDjk2gkFVsEXpZF9A=",ExodusContent.Round(10).Fingerprint);Check.Equal("GLT9uoWhUnPu8fFrWqeQKDYBrDWEiNHqazqM+/7eHFg=",ExodusContent.Round(11).Fingerprint);Check.Equal("IYXFTf1Ay0QB/3B5vLnYziBZsV/32jIp1/5LTFiPdnk=",ExodusContent.Round(13).Fingerprint);
        });
        test("R14 R13 disk entry preserves four receipts identity no240 repayment fresh World3",()=>Store(store=>{
            var (state,game)=CompletedThirteen();var identity=Identity(state);var gold=state.Profile.Gold;store.Commit(state,0,"r13-complete");var bytes=File.ReadAllBytes(store.Path);state=store.Load()!;game=AdamSession.Resume(state,ExodusContent.Round(13));Check.True(bytes.SequenceEqual(File.ReadAllBytes(store.Path)));Check.Equal(identity,Identity(state));Check.True(!game.ContentComplete);
            var original=Json(state);Check.Throws<ArgumentException>(()=>game.StartNextRound(new RoundDefinition(14,120,5200,999,Definition().Waves,"exodus_05")));Check.Equal(original,Json(state));
            game=game.StartNextRound(Definition());var run=state.Run!;Check.Equal(identity,Identity(state));Check.Equal(gold,state.Profile.Gold);Check.Equal(3,state.SchemaVersion);Check.Equal(13,run.PreviousNormalRound);Check.True(run.Boss==null&&run.World==null&&run.LastHitTime==-1&&run.Wave!.Elapsed==0);Check.True(!run.RoundCompletionApplied&&run.NormalEndedAt==null);Check.True(game.BossRules==null&&game.BossChoices.Count==0&&!game.CanEnterExodus);
            Check.Throws<InvalidOperationException>(()=>game.StartNextRound(Definition()));store.Commit(state,1,"r14-entry");state=store.Load()!;game=AdamSession.Resume(state,Definition());Check.Equal(identity,Identity(state));Check.Equal(gold,state.Profile.Gold);
            var world=new BattleWorld(game,Speeds,7);var fresh=world.Capture();Check.Near(0,fresh.Time);Check.Equal(3,fresh.SchemaVersion);Check.Equal(0,fresh.Charges!.Count);Check.True(fresh.HostileProjectiles==null&&fresh.NextHostileId==0&&fresh.Projectiles.Count==0&&world.ChariotDash==null);state.Run!.World=fresh;state.Validate();
        }));
        test("R14 ex13 actual alternating species sides independent HP surviving prior wave pause disk replay",()=>Store(store=>{
            var (state,game)=Start();game.Advance(3);var world=new BattleWorld(game,Speeds,7);var shadows=game.Targets.Values.Where(x=>x.MonsterId=="ex13").OrderBy(x=>x.Id).ToArray();Check.True(shadows.Length>=8);
            var tuning=world.Capture().Tuning;var boundary=tuning.CameraHalfWidth+tuning.SpawnMargin;
            for(var i=0;i<shadows.Length;i++){var pos=world.EnemyPositions[shadows[i].Id];Check.Near(world.PlayerPosition.X+(i%2==0?-boundary:boundary),pos.X,1e-6);Check.True(Math.Abs(pos.Y-world.PlayerPosition.Y)<=tuning.CameraHalfHeight);}
            game.Hit(shadows[0].Id,7);Check.Near(213,game.Targets[shadows[0].Id].Health);Check.Near(220,game.Targets[shadows[1].Id].Health);game.Kill(shadows[0].Id);game.Collect(shadows[0].Id);RoundTransitionTests.Resolve(game);Check.Near(220,game.Targets[shadows[1].Id].Health);
            // Surviving issued actors persist across the wave boundary; rebuild only the technical world.
            var survivor=shadows[1].Id;game.Advance(59);Check.Near(220,game.Targets[survivor].Health);world=new BattleWorld(game,Speeds,7);game.Capture();state.Run!.World=world.Capture();state.Validate();store.Commit(state,0,"sides-midpoint");var loaded=store.Load()!;var replayGame=AdamSession.Resume(loaded,Definition());var replay=BattleWorld.Restore(replayGame,Speeds,loaded.Run!.World!);
            Check.Near(220,replayGame.Targets[survivor].Health);world.Advance(1,Vector2.UnitY);replay.Advance(1,Vector2.UnitY);Check.Equal(Json(world.Capture()),Json(replay.Capture()));Check.Equal(0,world.ChargeStates.Count);Check.True(world.Capture().HostileProjectiles==null);
            game.Pause(true);var paused=Json(world.Capture());world.Advance(1,Vector2.Zero);Check.Equal(paused,Json(world.Capture()));game.Pause(false);
            var bad=world.Capture();bad.Charges!.Add(new EnemyChargeState{Id=survivor,Phase="cooldown",NextAt=69});Check.Throws<ArgumentException>(()=>BattleWorld.Restore(game,Speeds,bad));game.Capture();state.Run.World=bad;Check.Throws<InvalidDataException>(()=>state.Validate());
        }));
        test("R14 settlement pending candidates actual choices failure retry growth25 completion260 once",()=>Store(store=>{
            var (state,game)=Start();game.Advance(1);game.Damage(20);var enemy=game.Targets.Values.First();game.Kill(enemy.Id);state.Run!.Drops[enemy.Id]=100;game=AdamSession.Resume(state,Definition());var gold=state.Profile.Gold;var unlocks=Json(state.Profile.UnlockedWeapons);var receipts=Json(new{state.Run.CompletedBoss,state.Run.CompletedSecondBoss,state.Run.CompletedThirdBoss,state.Run.CompletedFourthBoss});
            game.Advance(180);Check.Equal("round_settlement",game.Stage);Check.True(game.Boss==null);var pending=state.Run.PendingLevels;var serial=state.Run.ChoiceSerial;Check.True(pending>1);Check.Equal(1,game.PendingChoices.Count);Check.Equal(gold,state.Profile.Gold);Check.True(game.CurrentHealth<state.Run.MaxHealth);Check.True(!game.CompleteNormalSettlement());store.Commit(state,0,"settle");state=store.Load()!;game=AdamSession.Resume(state,Definition());Check.Equal(pending,state.Run!.PendingLevels);Check.Equal(1,game.PendingChoices.Count);
            var chosen=0;var first=game.PendingChoices.Single();Check.True(game.Choose(first));chosen++;Check.True(!game.Choose(first));var failing=new SaveStore(store.Path,()=>throw new IOException("injected before replace"));Check.Throws<IOException>(()=>failing.Commit(state,1,"first-choice"));Check.Equal(gold,store.Load()!.Profile.Gold);Check.Equal(2L,store.Commit(state,1,"first-choice"));Check.Equal(2L,store.Commit(state,1,"first-choice"));state=store.Load()!;game=AdamSession.Resume(state,Definition());Check.True(!game.Choose(first));
            while(game.PendingChoices.Count>0){Check.True(game.Choose(game.PendingChoices.Single()));chosen++;}
            Check.Equal(pending,chosen);Check.Equal(serial+chosen,state.Run!.ChoiceSerial);Check.Equal(0,state.Run.PendingLevels);Check.Equal(0,game.PendingChoices.Count);Check.Equal(gold+25*chosen+260,state.Profile.Gold);Check.Equal(15,state.Run.RoundNumber);Check.Equal("between_rounds",game.Stage);Check.True(game.ContentComplete&&!game.ChapterComplete&&!game.CanEnterExodus);Check.Equal(state.Run.MaxHealth,state.Run.Health);Check.Equal(unlocks,Json(state.Profile.UnlockedWeapons));Check.Equal(receipts,Json(new{state.Run.CompletedBoss,state.Run.CompletedSecondBoss,state.Run.CompletedThirdBoss,state.Run.CompletedFourthBoss}));Check.True(!game.CompleteNormalSettlement());
            var paid=state.Profile.Gold;Check.Throws<IOException>(()=>failing.Commit(state,2,"complete"));Check.Equal(3L,store.Commit(state,2,"complete"));Check.Equal(3L,store.Commit(state,2,"complete"));state=store.Load()!;game=AdamSession.Resume(state,Definition());Check.Equal(paid,state.Profile.Gold);Check.True(game.ContentComplete&&!game.CompleteNormalSettlement());Check.Throws<InvalidOperationException>(()=>game.StartNextRound(new RoundDefinition(15,120,5200,260,Definition().Waves,"exodus_06")));state.Validate();
            Console.WriteLine($"R14 settlement evidence pending={pending} candidates=1 successful_Choose={chosen} growth_gold={25*chosen} completion_gold=260");
        }));
        test("R14 future R15 World5 boss5 receipt unknown preserves primary backup",()=>Store(store=>{
            var (state,game)=Start();var world=new BattleWorld(game,Speeds,7);world.Advance(1,Vector2.Zero);game.Capture();state.Run!.World=world.Capture();store.Commit(state,0,"supported");var original=File.ReadAllText(store.Path);File.WriteAllText(store.Path+".bak",original);
            foreach(var change in new Action<JsonNode>[] {n=>n["Run"]!["RoundNumber"]=15,n=>n["Run"]!["Stage"]="boss",n=>n["Run"]!["World"]!["SchemaVersion"]=5,n=>n["Run"]!["FutureR15"]=true,n=>n["Run"]!["CompletedFourthBoss"]!["Version"]=5,n=>n["Run"]!["CompletedFourthBoss"]!["Future"]=true,n=>n["SchemaVersion"]=4})
            {var node=JsonNode.Parse(original)!;change(node);var future=node.ToJsonString();File.WriteAllText(store.Path,future);Check.Throws<UnsupportedSaveVersionException>(()=>store.Load());Check.Throws<UnsupportedSaveVersionException>(()=>store.Commit(GameSnapshot.New(),1,"overwrite"));Check.Equal(future,File.ReadAllText(store.Path));Check.Equal(original,File.ReadAllText(store.Path+".bak"));}
        }));
        test("R14 four history required death explicit new challenge preserve profile R01 schema2",()=>{
            var (state,game)=Start();var receipt=state.Run!.CompletedFourthBoss;state.Run.CompletedFourthBoss=null;Check.Throws<InvalidDataException>(()=>state.Validate());state.Run.CompletedFourthBoss=receipt;state.Validate();
            var profile=Json(state.Profile);game.Damage(100000);Check.True(state.Run==null);Check.Equal(profile,Json(state.Profile));var fresh=new AdamSession(state,CatalogReader.Round("content/genesis/catalog.json",1),"bible");Check.Equal(2,state.SchemaVersion);Check.Equal(1,fresh.RoundNumber);Check.Equal(profile,Json(state.Profile));state.Validate();
            (state,game)=Start();profile=Json(state.Profile);state.Run=null;fresh=new AdamSession(state,CatalogReader.Round("content/genesis/catalog.json",1),"sling");Check.Equal(2,state.SchemaVersion);Check.Equal(1,fresh.RoundNumber);Check.Equal(profile,Json(state.Profile));Check.True(state.Run!.CompletedFourthBoss==null);state.Validate();
        });
    }
}
