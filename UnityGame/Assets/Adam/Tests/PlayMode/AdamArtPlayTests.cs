using System;
using System.Collections;
using System.IO;
using System.Linq;
using BibleSurvivor.Core;
using BibleSurvivor.Unity;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public sealed class AdamArtPlayTests
{
    [UnityTest] public IEnumerator RealGameProjectionFreezesRestoresAndFinishesDeathWithoutCoreTime()
    {
        var directory=Path.Combine(Path.GetTempPath(),"adam-art-play-"+Guid.NewGuid().ToString("N"));
        var owner=new GameObject("art lifecycle test"); var game=owner.AddComponent<AdamGame>();game.AutoInitialize=false;
        var host=new AdamHost(AdamCatalog.Load(),new SaveStore(Path.Combine(directory,"save.json")));
        game.Initialize(host);host.Start("bible");
        try
        {
            yield return null;
            // Use actual host movement and component Update; the component deliberately clears unconfirmed held input.
            host.SetMovement(Vector2.left);host.Tick(.1);game.ArtWorld.Update(host,0);
            Assert.IsTrue(game.ArtWorld.PlayerLeft);Assert.AreEqual("walk",game.ArtWorld.PlayerMotion);
            game.SetLifecycle(false,false);yield return null;
            var clock=game.ArtWorld.Clock;
            for(var i=0;i<4;i++)yield return null;
            Assert.AreEqual(clock,game.ArtWorld.Clock);Assert.IsTrue(game.ArtWorld.PlayerLeft);
            game.SetLifecycle(true,false);Assert.IsTrue(host.Paused);
            host.Reload();yield return null;Assert.IsTrue(game.ArtWorld.PlayerLeft);Assert.AreEqual("idle",game.ArtWorld.PlayerMotion);
            host.SetPaused(false);host.Session.Damage(100000);yield return null;
            Assert.AreEqual("death",game.ArtWorld.PlayerMotion);Assert.IsTrue(game.DeathPresentationVisible);var coreTime=host.World.SimulationTime;
            yield return new WaitForSecondsRealtime(.85f);
            Assert.AreEqual(coreTime,host.World.SimulationTime);Assert.IsFalse(game.DeathPresentationVisible);
            var art=new AdamArt();
            foreach(var layer in art.Index.layers)
                Assert.AreEqual(5,art.Clip("chr_player_layer-"+layer.Replace('_','-')+"_death_32").Frame(game.ArtWorld.PlayerSeconds));
            host.Start("sling");yield return null;
            Assert.AreEqual("idle",game.ArtWorld.PlayerMotion);Assert.IsFalse(game.ArtWorld.PlayerLeft);Assert.AreEqual(0,game.ArtWorld.ActiveEffects);
        }
        finally {UnityEngine.Object.Destroy(owner);}
        yield return null;Directory.Delete(directory,true);
    }
    [Test] public void ActualKillAndHitEventsUseBoundedReusableSlotsAndResetOnReload()
    {
        var dir=Path.Combine(Path.GetTempPath(),"adam-art-events-"+Guid.NewGuid().ToString("N"));
        try
        {
            var host=new AdamHost(AdamCatalog.Load(),new SaveStore(Path.Combine(dir,"save.json")));host.Start("sling");
            var view=new AdamArtWorld();view.Update(host,0);var slots=view.Effects.ToArray();var killed=0;var sawDeath=false;
            for(var i=0;i<1800 && host.Session.IsActive;i++)
            {
                if(host.Session.PendingChoices.Count>0) host.Choose(host.Session.PendingChoices[0]);
                host.SetMovement(new Vector2(1,1));host.Tick(.1);view.Update(host,.1f);
                var id=host.Session.Wave.Active.Keys.FirstOrDefault();
                if(id!=0)
                {
                    host.Session.Hit(id,1);host.Tick(1d/60);view.Update(host,0);
                    host.Session.Kill(id);host.Tick(1d/60);view.Update(host,0);killed++;
                    sawDeath |= view.Effects.Any(e=>e.Active&&e.Clip=="fx_enemy-death_burst_32");
                    Assert.LessOrEqual(view.ActiveEffects,AdamArtWorld.EffectCapacity);
                }
                if(killed>AdamArtWorld.EffectCapacity) break;
            }
            Assert.IsTrue(sawDeath);Assert.Greater(killed,0);
            CollectionAssert.AreEqual(slots,view.Effects); // Slots are reused rather than instantiating scene objects.
            host.Reload();view.Update(host,0);Assert.AreEqual(0,view.ActiveEffects);
        }
        finally {if(Directory.Exists(dir))Directory.Delete(dir,true);}
    }
#if UNITY_EDITOR
    public sealed class DeathPointerWindow : UnityEditor.EditorWindow
    {
        public AdamGame Game;
        void OnGUI() {if(Game!=null)Game.DrawGameView(new Rect(0,0,position.width,position.height));}
    }
    [UnityTest] public IEnumerator DeathClipCannotBeCoveredByClickableRestartModal()
    {
        var directory=Path.Combine(Path.GetTempPath(),"adam-death-pointer-"+Guid.NewGuid().ToString("N"));
        var owner=new GameObject("death pointer test");var game=owner.AddComponent<AdamGame>();game.AutoInitialize=false;
        var host=new AdamHost(AdamCatalog.Load(),new SaveStore(Path.Combine(directory,"save.json")));game.Initialize(host);host.Start("jawbone");
        yield return null;
        for(var i=0;i<100 && host.World.Capture().Enemies.Count==0;i++) host.Tick(.1);
        Assert.Greater(host.World.Capture().Enemies.Count,0);host.Checkpoint();
        var store=new SaveStore(Path.Combine(directory,"save.json"));var fixture=store.Load();fixture.Run.Health=1;
        fixture.Run.World.Enemies[0].X=fixture.Run.World.PlayerX+.1f;fixture.Run.World.Enemies[0].Y=fixture.Run.World.PlayerY;
        store.Commit(fixture,fixture.Revision,Guid.NewGuid().ToString("N"));Assert.IsTrue(host.Reload(),host.Error);
        game.ArtWorld.Update(host,0);var time=host.World.SimulationTime;Assert.Greater(game.ArtWorld.Snapshot.Enemies.Count,0);
        host.SetPaused(false);host.SetMovement(Vector2.left);host.Tick(1d/60);Assert.IsNull(host.Session.Wave);
        Assert.AreEqual(time,host.World.SimulationTime,"Fatal contact clears the arena before advancing the clock");
        game.ArtWorld.Update(host,0);Assert.IsEmpty(game.ArtWorld.Snapshot.Enemies,"Terminal state must refresh even at the same time");
        Assert.IsTrue(game.ArtWorld.PlayerLeft,"Fatal movement facing must come from the final Core snapshot");
        game.enabled=false;game.SetLifecycle(true,false);host.SetPaused(false);
        // Freeze presentation at its first death frame so this tests the real complete renderer, independent of editor timing.
        var window=ScriptableObject.CreateInstance<DeathPointerWindow>();window.Game=game;
        window.minSize=window.maxSize=new Vector2(660,440);window.position=new Rect(20,20,660,440);window.ShowUtility();
        try
        {
            window.Repaint();yield return null;window.Repaint();yield return null;
            var id=host.Session;Assert.IsTrue(game.DeathPresentationVisible);
            // This is the first restart weapon's actual hit rectangle in the old immediate terminal modal.
            window.SendEvent(new Event {type=EventType.MouseDown,button=0,mousePosition=new Vector2(330,230)});
            yield return null;
            window.SendEvent(new Event {type=EventType.MouseUp,button=0,mousePosition=new Vector2(330,230)});
            yield return null;
            Assert.AreSame(id,host.Session,"Restart controls must not cover or consume clicks during the death clip");
            Assert.IsFalse(host.Session.IsActive);
        }
        finally {window.Close();UnityEngine.Object.Destroy(owner);}
        yield return null;Directory.Delete(directory,true);
    }
#endif

}
