using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Text.Json.Nodes;
using BibleSurvivor.Core;

namespace BibleSurvivor.Core.Tests;

internal static class BattleSaveTests
{
    public static void Register(Action<string,Action> test)
    {
        test("Coordinate battle checkpoint JSON restores ongoing attacks and deterministic continuation",()=>{
            var state=GameSnapshot.New();var definition=WaveTests.Adam();var speeds=new Dictionary<string,double>{{"a1",.45},{"a2",.6},{"a3",.35}};
            var session=new AdamSession(state,definition,"sling");var original=new BattleWorld(session,speeds,13);
            for(var i=0;i<600 && session.IsActive;i++){Resolve(session);original.Advance(1.0/60,Vector2.UnitX);}
            Resolve(session);Check.True(session.IsActive);session.Capture();state.Run!.World=original.Capture();
            var dir=Path.Combine(Path.GetTempPath(),"bible-battle-save-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(dir);
            try
            {
                var store=new SaveStore(Path.Combine(dir,"save.json"));store.Commit(state,0,"checkpoint");
                var loaded=store.Load()!;var resumedSession=AdamSession.Resume(loaded,definition);
                var resumed=BattleWorld.Restore(resumedSession,speeds,loaded.Run!.World!);
                for(var i=0;i<240 && session.IsActive && resumedSession.IsActive;i++)
                {
                    Resolve(session);Resolve(resumedSession);
                    original.Advance(1.0/60,Vector2.UnitX);resumed.Advance(1.0/60,Vector2.UnitX);
                }
                Check.Near(original.PlayerPosition.X,resumed.PlayerPosition.X);Check.Near(original.PlayerPosition.Y,resumed.PlayerPosition.Y);
                Check.Equal(original.ShotsFired,resumed.ShotsFired);Check.Equal(original.Hits,resumed.Hits);Check.Equal(original.Kills,resumed.Kills);
                Check.Equal(original.Collected,resumed.Collected);Check.Near(session.CurrentHealth,resumedSession.CurrentHealth);
                Check.Equal(state.Profile.Gold,loaded.Profile.Gold);Check.Near(original.SimulationTime,resumed.SimulationTime);
                Check.Equal(Json(original.Capture()),Json(resumed.Capture()));
            }
            finally {Directory.Delete(dir,true);}
        });
        test("Corrupt world hit-list member recovers from valid backup",()=>WithCheckpoint((store,state)=>{
            store.Commit(state,0,"one");store.Commit(state,1,"two");
            var node=JsonNode.Parse(File.ReadAllText(store.Path))!;
            node["Run"]!["World"]!["Weapons"]![0]!["OrbitHits"]=new JsonArray((JsonNode?)null);
            File.WriteAllText(store.Path,node.ToJsonString());
            Check.Equal(1L,store.Load()!.Revision);
        }));
        test("Stale world clock is rejected before overwriting a valid checkpoint",()=>WithCheckpoint((store,state)=>{
            store.Commit(state,0,"one");state.Run!.World!.Time+=1;
            Check.Throws<InvalidDataException>(()=>store.Commit(state,1,"stale-world"));
            Check.Equal(1L,store.Load()!.Revision);
        }));
        test("Null wave member in current and legacy save recovers from valid backup",()=>{
            foreach(var legacy in new[]{false,true}) WithCheckpoint((store,state)=>{
                store.Commit(state,0,"one");store.Commit(state,1,"two");
                var node=JsonNode.Parse(File.ReadAllText(store.Path))!;
                node["Run"]!["Wave"]!["Active"]=new JsonArray((JsonNode?)null);
                if(legacy) node["Run"]!.AsObject().Remove("World");
                File.WriteAllText(store.Path,node.ToJsonString());Check.Equal(1L,store.Load()!.Revision);
            });
            var wave=new WaveDirector(WaveTests.Adam());wave.Advance(2);var snapshot=wave.Snapshot();snapshot.Active[0]=null!;
            Check.Throws<ArgumentException>(()=>WaveDirector.Restore(WaveTests.Adam(),snapshot));
        });
    }
    private static void Resolve(AdamSession game)
    {while(game.PendingChoices.Count>0) Check.True(game.Choose(game.PendingChoices[0]));}
    private static string Json(BattleSnapshot snapshot)
    {
        using var stream=new MemoryStream();new DataContractJsonSerializer(typeof(BattleSnapshot)).WriteObject(stream,snapshot);
        return Encoding.UTF8.GetString(stream.ToArray());
    }
    private static void WithCheckpoint(Action<SaveStore,GameSnapshot> action)
    {
        var state=GameSnapshot.New();var session=new AdamSession(state,WaveTests.Adam(),"bible");
        var world=new BattleWorld(session,new Dictionary<string,double>{{"a1",.45},{"a2",.6},{"a3",.35}},7);
        world.Advance(2,Vector2.Zero);session.Capture();state.Run!.World=world.Capture();
        var dir=Path.Combine(Path.GetTempPath(),"bible-battle-corrupt-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(dir);
        try {action(new SaveStore(Path.Combine(dir,"save.json")),state);}finally{Directory.Delete(dir,true);}
    }
}
