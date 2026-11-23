using System;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text.Json;
using System.Text.Json.Nodes;
using BibleSurvivor.Core;
using BibleSurvivor.Sim;
namespace BibleSurvivor.Core.Tests;
internal static class RoundTenTests
{
    private const string Genesis="content/genesis/catalog.json";
    private const string Exodus="content/exodus/catalog.json";
    private static RoundDefinition Definition()=>CatalogReader.Round(Exodus,10);
    internal static (GameSnapshot State,AdamSession Game) CompletedNine()
    {
        var (state,game)=RoundEightTests.CompletedSeven();
        game=game.StartNextRound(CatalogReader.Round(Genesis,8));game.Advance(180);
        game=game.StartNextRound(CatalogReader.Round(Genesis,9));game.Advance(180);
        var world=new BattleWorld(game,CatalogReader.MonsterSpeeds(Genesis));world.Advance(2,Vector2.UnitX);
        game.Hit(1141,16000);if(game.Stage=="boss_reward")Check.True(game.ChooseBossReward(game.Boss!.Reward!.RewardId,game.BossChoices.First().Id));
        RoundTransitionTests.Resolve(game);Check.True(game.CompleteBossSettlement());state.Validate();return(state,game);
    }
    private static void Store(Action<SaveStore> action)
    {var folder=Path.Combine(Path.GetTempPath(),"r10-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(folder);try{action(new SaveStore(Path.Combine(folder,"save.json")));}finally{Directory.Delete(folder,true);}}
    private static string Json(object? value)=>JsonSerializer.Serialize(value);
    public static void Register(Action<string,Action> test)
    {
        test("R10 legacy schema1 and schema2 golden geometry fingerprints and Load bytes preserved",()=>Store(store=>{
            foreach(var version in new[]{1,2})
            {
                var fixture=File.ReadAllBytes("tests/BibleSurvivor.Core.Tests/Fixtures/genesis-schema"+version+"-sling-r2-t120.json");File.WriteAllBytes(store.Path,fixture);
                var state=store.Load()!;Check.Equal(2,state.SchemaVersion);Check.True(fixture.SequenceEqual(File.ReadAllBytes(store.Path)));var expected=JsonNode.Parse(fixture)!;expected["SchemaVersion"]=2;
                using var stream=new MemoryStream();new System.Runtime.Serialization.Json.DataContractJsonSerializer(typeof(GameSnapshot)).WriteObject(stream,state);Check.True(JsonNode.DeepEquals(expected,JsonNode.Parse(stream.ToArray())));
                var game=AdamSession.Resume(state,CatalogReader.Round(Genesis,2));var world=BattleWorld.Restore(game,CatalogReader.MonsterSpeeds(Genesis),state.Run!.World!);Check.Near(2,world.SimulationTime,1e-6);Check.True(!game.CanEnterExodus);
            }
        }));
        test("R10 explicit candidate preserves original schema2 and three receipts progression with no entry payment",()=>Store(store=>{
            var (state,game)=CompletedNine();store.Commit(state,0,"r09");var bytes=File.ReadAllBytes(store.Path);state=store.Load()!;game=AdamSession.Resume(state,CatalogReader.Round(Genesis,9));Check.Equal(2,state.SchemaVersion);Check.True(game.ChapterComplete&&game.CanEnterExodus&&!game.ContentComplete);Check.True(bytes.SequenceEqual(File.ReadAllBytes(store.Path)));
            var original=Json(state);var run=state.Run!;var first=Json(run.CompletedBoss);var second=Json(run.CompletedSecondBoss);var third=Json(run.Boss);var gold=state.Profile.Gold;
            Check.Throws<ArgumentException>(()=>game.PrepareExodus(new RoundDefinition(10,110,3600,999,Definition().Waves,"exodus_01")));Check.Equal(original,Json(state));
            var candidate=game.PrepareExodus(Definition());var entered=candidate.Run!;Check.Equal(original,Json(state));Check.Equal(3,candidate.SchemaVersion);Check.Equal(2,state.SchemaVersion);Check.Equal(first,Json(entered.CompletedBoss));Check.Equal(second,Json(entered.CompletedSecondBoss));Check.Equal(third,Json(entered.CompletedThirdBoss));Check.True(entered.Boss==null&&entered.World==null);Check.Equal(run.RunId,entered.RunId);Check.Equal(Json(run.Weapons),Json(entered.Weapons));Check.Equal(Json(run.Passives),Json(entered.Passives));Check.Equal(Json(run.Forms),Json(entered.Forms));Check.Equal(Json(run.PermanentSnapshot),Json(entered.PermanentSnapshot));Check.Equal(run.Level,entered.Level);Check.Equal(run.Experience,entered.Experience);Check.Equal(run.ChoiceSerial,entered.ChoiceSerial);Check.Equal(run.KillRemainder,entered.KillRemainder);Check.Equal(gold,candidate.Profile.Gold);Check.Equal(run.Health,entered.Health);Check.Equal(-1d,entered.LastHitTime);Check.Equal(0d,entered.Wave!.Elapsed);
            var failing=new SaveStore(store.Path,()=>throw new IOException("injected before replace"));Check.Throws<IOException>(()=>failing.Commit(candidate,state.Revision,"enter"));Check.True(bytes.SequenceEqual(File.ReadAllBytes(store.Path)));Check.Equal(original,Json(state));
            var revision=store.Commit(candidate,state.Revision,"enter");Check.Equal(revision,store.Commit(game.PrepareExodus(Definition()),state.Revision,"enter"));var loaded=store.Load()!;var active=AdamSession.Resume(loaded,Definition());Check.True(active.IsActive&&!active.CanEnterExodus&&!active.ChapterComplete&&!active.ContentComplete);Check.Throws<InvalidOperationException>(()=>active.PrepareExodus(Definition()));Check.Equal(gold,loaded.Profile.Gold);Check.Equal(run.RunId,loaded.Run!.RunId);
        }));
        test("R10 catalog finite budget cap XP contact speed and coordinate SaveLoad completion once",()=>Store(store=>{
            var d=Definition();Check.Equal("exodus_01",d.Id);Check.Equal(900,d.Waves.Sum(w=>w.Groups.Sum(g=>g.Count)));Check.Equal(3600,d.RawExperience);Check.Equal(180,d.CompletionGold);Check.Equal(110,d.ConcurrencyCap);Check.True(d.UnlockWeaponId==null);var wave=new WaveDirector(d);wave.Advance(120);Check.Equal(110,wave.Active.Count);Check.True(wave.Active.Values.All(x=>x.RawExperience==4));
            var (state,game)=CompletedNine();state=game.PrepareExodus(d);game=AdamSession.Resume(state,d);var world=new BattleWorld(game,ExodusContent.MonsterSpeeds,10);world.Advance(2,Vector2.UnitX);game.Capture();state.Run!.World=world.Capture();Check.Equal(3,state.Run.World.SchemaVersion);Check.Equal(0,state.Run.World.Charges!.Count);Check.True(state.Run.World.HostileProjectiles==null);Check.True(state.Run.World.PlayerX>0);store.Commit(state,0,"mid");
            var loaded=store.Load()!;var resumed=AdamSession.Resume(loaded,d);var replay=BattleWorld.Restore(resumed,ExodusContent.MonsterSpeeds,loaded.Run!.World!);world.Advance(.5,Vector2.UnitY);replay.Advance(.5,Vector2.UnitY);Check.Equal(Json(world.Capture()),Json(replay.Capture()));
            // Technical geometry fixture: large health is injected solely to inspect the full timer path.
            loaded.Run.Health=loaded.Run.MaxHealth=100000;var gold=loaded.Profile.Gold;for(var tick=0;tick<1801&&resumed.IsActive;tick++){RoundTransitionTests.Resolve(resumed);replay.Advance(.1,Vector2.UnitY);}RoundTransitionTests.Resolve(resumed);Check.True(!resumed.ContentComplete&&!resumed.ChapterComplete&&!resumed.CanEnterExodus);Check.Equal(11,loaded.Run.RoundNumber);Check.True(loaded.Run.RoundCompletionApplied);Check.True(!resumed.CompleteNormalSettlement());Check.True(loaded.Profile.Gold>=gold+180);Check.Equal(loaded.Run.MaxHealth,loaded.Run.Health);resumed.Capture();loaded.Run.World=null;loaded.Validate();store.Commit(loaded,1,"done");var done=store.Load()!;Check.True(!AdamSession.Resume(done,d).ContentComplete);var paid=done.Profile.Gold;Check.True(!AdamSession.Resume(done,d).CompleteNormalSettlement());Check.Equal(paid,done.Profile.Gold);Check.Throws<InvalidOperationException>(()=>AdamSession.Resume(done,d).StartNextRound(new RoundDefinition(12,110,3600,180,d.Waves,"exodus_03")));
        }));
        test("R10 settlement growth pending SaveLoad final180 once unlock unchanged",()=>Store(store=>{
            var (state,game)=CompletedNine();state=game.PrepareExodus(Definition());game=AdamSession.Resume(state,Definition());game.Advance(1);var enemy=game.Targets.Values.First();game.Kill(enemy.Id);state.Run!.Drops[enemy.Id]=100;game=AdamSession.Resume(state,Definition());var gold=state.Profile.Gold;var unlocks=Json(state.Profile.UnlockedWeapons);game.Advance(180);Check.Equal("round_settlement",game.Stage);Check.True(state.Run.PendingLevels>0);Check.Equal(gold,state.Profile.Gold);Check.True(!game.CompleteNormalSettlement());store.Commit(state,0,"settle");state=store.Load()!;game=AdamSession.Resume(state,Definition());RoundTransitionTests.Resolve(game);Check.Equal(gold+180,state.Profile.Gold);Check.Equal(unlocks,Json(state.Profile.UnlockedWeapons));Check.True(!game.ContentComplete);Check.True(!game.CompleteNormalSettlement());state.Validate();
        }));
        test("R10 schema3 future content protects primary backup and profile new challenge restores schema2",()=>Store(store=>{
            var (state,game)=CompletedNine();state=game.PrepareExodus(Definition());store.Commit(state,0,"enter");var original=File.ReadAllText(store.Path);File.WriteAllText(store.Path+".bak",original);
            foreach(var change in new Action<JsonNode>[] {n=>n["SchemaVersion"]=SaveFormat.CurrentVersion+1,n=>n["Run"]!["RoundNumber"]=12,n=>n["Run"]!["Future"]=true,n=>n["Run"]!["CompletedThirdBoss"]!["Future"]=true,n=>n["Run"]!["CompletedThirdBoss"]!["Version"]=4})
            {var node=JsonNode.Parse(original)!;change(node);var future=node.ToJsonString();File.WriteAllText(store.Path,future);Check.Throws<UnsupportedSaveVersionException>(()=>store.Load());Check.Throws<UnsupportedSaveVersionException>(()=>store.Commit(GameSnapshot.New(),1,"overwrite"));Check.Equal(future,File.ReadAllText(store.Path));Check.Equal(original,File.ReadAllText(store.Path+".bak"));}
            state.Run=null;state.Profile.Gold=900;PermanentRules.Purchase(state.Profile,"attack");var unlocks=Json(state.Profile.UnlockedWeapons);var profile=Json(state.Profile);state.Validate();var fresh=new AdamSession(state,CatalogReader.Round(Genesis,1),"bible");Check.Equal(2,state.SchemaVersion);Check.Equal(profile,Json(state.Profile));Check.Equal(unlocks,Json(state.Profile.UnlockedWeapons));Check.Equal(1,fresh.RoundNumber);state.Validate();
        }));
    }
}
