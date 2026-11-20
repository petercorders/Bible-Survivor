using System;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text.Json;
using System.Text.Json.Nodes;
using BibleSurvivor.Core;

namespace BibleSurvivor.Core.Tests;

internal static class RoundSixteenTests
{
    public static void Register(Action<string,Action> test)
    {
        test("R16 explicit entry copies all five receipts and preserves completed R15 on failed commit",()=>WithStore(store=>{
            var (state,session)=RoundFifteenTests.CompletedFifteen();
            var original=JsonSerializer.Serialize(state);
            var candidate=session.PrepareLeviticus(LeviticusContent.Round());
            Check.Equal(original,JsonSerializer.Serialize(state));
            Check.True(session.ExodusComplete);
            var before=state.Run!;
            var after=candidate.Run!;
            Check.Equal(before.RunId,after.RunId);
            Check.Equal(Progress(before),Progress(after));
            Check.Equal(JsonSerializer.Serialize(state.Profile),JsonSerializer.Serialize(candidate.Profile));
            Check.Equal(JsonSerializer.Serialize(before.Boss),JsonSerializer.Serialize(after.CompletedFifthBoss));
            Check.Equal(History(before),History(after));
            Check.True(after.Boss==null&&after.World==null&&after.Drops.Count==0);
            Check.Equal(-1d,after.LastHitTime);
            store.Commit(state,0,"complete-exodus");
            var saved=File.ReadAllBytes(store.Path);
            var failing=new SaveStore(store.Path,()=>throw new IOException("before replace"));
            Check.Throws<IOException>(()=>failing.Commit(candidate,1,"enter-leviticus"));
            Check.True(saved.SequenceEqual(File.ReadAllBytes(store.Path)));
            Check.True(AdamSession.Resume(store.Load()!,ExodusContent.Round(15)).ExodusComplete);
            Check.Equal(2L,store.Commit(candidate,1,"enter-leviticus"));
            Check.Equal(2L,store.Commit(candidate,1,"enter-leviticus"));
            var loaded=store.Load()!;
            var resumed=AdamSession.Resume(loaded,LeviticusContent.Round());
            Check.True(!resumed.ExodusComplete&&!resumed.CanEnterLeviticus);
            Check.Equal(History(after),History(loaded.Run!));
            Check.Equal(JsonSerializer.Serialize(before.Boss),JsonSerializer.Serialize(loaded.Run!.CompletedFifthBoss));
            Check.Equal(Progress(before),Progress(loaded.Run));
            Check.Throws<InvalidOperationException>(()=>resumed.PrepareLeviticus(LeviticusContent.Round()));
            Check.Throws<InvalidOperationException>(()=>session.StartNextRound(LeviticusContent.Round()));
        }));

        test("R16 waves, actual movement, World3 disk replay and completion gold once",()=>WithStore(store=>{
            var (_,previous)=RoundFifteenTests.CompletedFifteen();
            var definition=LeviticusContent.Round();
            Check.Equal(1260,definition.Waves.SelectMany(w=>w.Groups).Sum(g=>g.Count));
            var state=previous.PrepareLeviticus(definition);
            var session=AdamSession.Resume(state,definition);
            var world=new BattleWorld(session,LeviticusContent.MonsterSpeeds,7);
            world.Advance(3,Vector2.UnitX);
            Check.True(world.PlayerPosition.X>0 && world.EnemyPositions.Count>0);
            state.Run!.World=world.Capture();
            Check.Equal(3,state.Run.World.SchemaVersion);
            store.Commit(state,0,"combat");
            var loaded=store.Load()!;
            var resumed=AdamSession.Resume(loaded,definition);
            var replay=BattleWorld.Restore(resumed,LeviticusContent.MonsterSpeeds,loaded.Run!.World!);
            world.Advance(1,Vector2.UnitY);
            replay.Advance(1,Vector2.UnitY);
            Check.Equal(JsonSerializer.Serialize(world.Capture()),JsonSerializer.Serialize(replay.Capture()));
            // Isolate pending settlement from the seeded combat's incidental drops.
            resumed.Kill(resumed.Targets.Keys.First());
            foreach(var id in loaded.Run.Drops.Keys.ToArray())loaded.Run.Drops[id]=0;
            loaded.Run.Drops[loaded.Run.Drops.Keys.First()]=100;
            var gold=loaded.Profile.Gold;
            resumed.Advance(180);
            Check.Equal("round_settlement",resumed.Stage);
            Check.True(!resumed.CompleteNormalSettlement());
            store.Commit(loaded,1,"pending");
            loaded=store.Load()!;
            resumed=AdamSession.Resume(loaded,definition);
            var pending=loaded.Run!.PendingLevels;
            Check.True(pending>0);
            while(resumed.PendingChoices.Count>0)
                Check.True(resumed.Choose(resumed.PendingChoices.Single()));
            Check.True(!resumed.ContentComplete&&!resumed.ExodusComplete&&!resumed.CompleteNormalSettlement());
            gold+=pending*25;
            Check.Equal(gold+300,loaded.Profile.Gold);
            Check.Equal(17,loaded.Run.RoundNumber);
            store.Commit(loaded,2,"complete");
            loaded=store.Load()!;
            Check.Equal("between_rounds",AdamSession.Resume(loaded,definition).Stage);
            Check.Equal(gold+300,loaded.Profile.Gold);
        }));

        test("R16 protects future lifecycle and fifth receipt fields on disk",()=>WithStore(store=>{
            var (_,previous)=RoundFifteenTests.CompletedFifteen();
            var state=previous.PrepareLeviticus(LeviticusContent.Round());
            store.Commit(state,0,"r16");
            var valid=File.ReadAllText(store.Path);
            File.WriteAllText(store.Path+".bak",valid);
            foreach(var change in new Action<JsonNode>[] {
                n=>n["Run"]!["RoundNumber"]=21,
                n=>n["Run"]!["RoundFingerprint"]="future-fingerprint",
                n=>n["Run"]!["CompletedFifthBoss"]!["FutureRule"]=true
            })
            {
                var node=JsonNode.Parse(valid)!;
                change(node);
                var future=node.ToJsonString();
                File.WriteAllText(store.Path,future);
                Check.Throws<UnsupportedSaveVersionException>(()=>store.Load());
                Check.Throws<UnsupportedSaveVersionException>(()=>store.Commit(state,1,"overwrite"));
                Check.Equal(future,File.ReadAllText(store.Path));
                Check.Equal(valid,File.ReadAllText(store.Path+".bak"));
            }
        }));
    }

    private static string History(RunState run)=>JsonSerializer.Serialize(new
    {
        run.CompletedBoss, run.CompletedSecondBoss, run.CompletedThirdBoss, run.CompletedFourthBoss
    });

    private static string Progress(RunState run)=>JsonSerializer.Serialize(new
    {
        run.Weapons, run.Passives, run.Forms, run.Level, run.Experience, run.KillRemainder,
        run.ChoiceSerial, run.PermanentSnapshot
    });

    private static void WithStore(Action<SaveStore> test)
    {
        var path=Path.Combine(Path.GetTempPath(),"r16-"+Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(path);
        try { test(new SaveStore(Path.Combine(path,"save.json"))); }
        finally { Directory.Delete(path,true); }
    }
}
