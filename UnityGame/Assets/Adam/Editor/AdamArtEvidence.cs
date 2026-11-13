using System;
using System.IO;
using System.Linq;
using BibleSurvivor.Core;
using UnityEngine;

namespace BibleSurvivor.Unity.Editor
{
    // Explicit QA-only snapshots, never called by the game. Clocks/IDs are produced by the real wave/session APIs.
    // Spatial placement is staged to compare all three approved enemies in one visible shot; this is not a balance run.
    public static class AdamArtEvidence
    {
        public static void CreateGallerySaves()
        {
            var directory=Path.GetFullPath(Path.Combine(Application.dataPath,"../../evidence/unity-art-20261114/gallery"));
            Directory.CreateDirectory(directory);
            var catalog=AdamCatalog.Load();
            foreach(var weapon in AdamGame.Weapons)
            {
                var state=GameSnapshot.New();var session=new AdamSession(state,catalog.Definition,weapon);
                for(var i=0;i<150;i++)
                {
                    session.Advance(1);
                    var kept=new System.Collections.Generic.HashSet<string>();
                    foreach(var enemy in session.Wave.Active.Values.ToArray()) if(!kept.Add(enemy.MonsterId)) session.Kill(enemy.Id);
                }
                var world=new BattleWorld(session,catalog.Speeds,7);var snapshot=world.Capture();snapshot.Time=session.Wave.Elapsed;
                snapshot.Enemies.Clear();var index=0;
                foreach(var enemy in session.Wave.Active.Values.OrderBy(e=>e.MonsterId))
                {
                    var positions=new[]{new Vector2(-3,0),new Vector2(3,1),new Vector2(1,-2)};var position=positions[index++];
                    snapshot.Enemies.Add(new BattleEntityState {Id=enemy.Id,X=position.x,Y=position.y});
                }
                snapshot.Drops.Clear();index=0;
                foreach(var id in session.DroppedExperience.Keys)
                {
                    // A few visible gems; remaining logical drops are placed well outside the view, still present in the valid snapshot.
                    snapshot.Drops.Add(new BattleEntityState {Id=id,X=index<4?-2+index:1000,Y=index<4?2:1000});index++;
                }
                snapshot.FacingX=-1;snapshot.FacingY=0;
                var restored=BattleWorld.Restore(session,catalog.Speeds,snapshot);
                state.Run.World=restored.Capture();session.Capture();state.Validate();
                var path=Path.Combine(directory,weapon+".json");
                if(File.Exists(path))throw new IOException("Use a fresh evidence path: "+path);
                var store=new SaveStore(path);store.Commit(state,0,Guid.NewGuid().ToString("N"));
                // The actual runtime must accept every gallery through the normal load path.
                var host=new AdamHost(catalog,new SaveStore(path));if(host.Error!=null)throw new InvalidOperationException(host.Error);
                if(host.Session.Wave.Active.Values.Select(e=>e.MonsterId).Distinct().Count()!=3)throw new InvalidOperationException("Gallery lacks a monster type");
            }
        }
    }
}
