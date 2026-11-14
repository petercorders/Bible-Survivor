using System;
using System.Collections;
using System.IO;
using BibleSurvivor.Core;
using BibleSurvivor.Unity;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.TestTools;

public sealed class AdamRoundPlayTests
{
    string directory;
    GameObject owner;
    AdamGame game;
    Keyboard keys;
    InputSettings.BackgroundBehavior background;
    InputSettings.EditorInputBehaviorInPlayMode editorInput;
    bool run;
    [UnitySetUp] public IEnumerator Setup()
    {
        directory=Path.Combine(Path.GetTempPath(),"adam-round-play-"+Guid.NewGuid().ToString("N"));
        owner=new GameObject("R02 round regression");owner.SetActive(false);game=owner.AddComponent<AdamGame>();game.AutoInitialize=false;
        background=InputSystem.settings.backgroundBehavior;editorInput=InputSystem.settings.editorInputBehaviorInPlayMode;run=Application.runInBackground;
        InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;InputSystem.settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;Application.runInBackground=true;
        keys=InputSystem.AddDevice<Keyboard>();owner.SetActive(true);
        game.Initialize(new AdamHost(AdamCatalog.Load(),new SaveStore(Path.Combine(directory,"save.json"))));yield return null;
    }
    [UnityTearDown] public IEnumerator Cleanup()
    {
        InputSystem.RemoveDevice(keys);InputSystem.settings.backgroundBehavior=background;InputSystem.settings.editorInputBehaviorInPlayMode=editorInput;Application.runInBackground=run;
        UnityEngine.Object.Destroy(owner);yield return null;if(Directory.Exists(directory))Directory.Delete(directory,true);
    }
    void Keys(params Key[] input)=>InputSystem.QueueStateEvent(keys,new KeyboardState(input));
    void CompleteR01()
    {
        var host=game.Host;host.Start("sling");host.Session.Advance(180);host.SetPaused(true);
        while(host.Session.PendingChoices.Count>0)Assert.IsTrue(host.Choose(host.Session.PendingChoices[0]));
        Assert.IsTrue(host.Checkpoint(),host.Error);Assert.IsTrue(host.AwaitingR02);
    }
    [UnityTest] public IEnumerator SmallWindowContinueClickEntersSavedR02()=>ContinueClick(660,440);
    [UnityTest] public IEnumerator LargeWindowContinueClickEntersSavedR02()=>ContinueClick(1100,720);
    IEnumerator ContinueClick(int width,int height)
    {
        CompleteR01();game.enabled=false;game.Initialize(game.Host); // Deterministic presentation, real IMGUI pointer dispatch below.
        var window=ScriptableObject.CreateInstance<PauseModalPointerWindow>();
        try
        {
            window.Game=game;window.minSize=window.maxSize=new Vector2(width,height);window.position=new Rect(40,40,width,height);window.ShowUtility();window.Repaint();yield return null;
            game.SetLifecycle(true,false);var host=game.Host;var id=host.State.Run.RunId;var gold=host.State.Profile.Gold;var old=host.World;
            var point=new Vector2(width/2,height/2+48);
            window.SendEvent(new Event{type=EventType.MouseDown,mousePosition=point,button=0,clickCount=1});
            window.SendEvent(new Event{type=EventType.MouseUp,mousePosition=point,button=0,clickCount=1});
            Assert.AreEqual(2,host.Session.RoundNumber,"visible Continue must enter R02, not offer a new R01 challenge");
            Assert.AreNotSame(old,host.World);Assert.Zero(host.World.SimulationTime);Assert.AreEqual(id,host.State.Run.RunId);Assert.AreEqual(gold,host.State.Profile.Gold);
            var restored=new AdamHost(AdamCatalog.Load(),new SaveStore(Path.Combine(directory,"save.json")));Assert.AreEqual(2,restored.Session.RoundNumber);Assert.Zero(restored.World.SimulationTime);
            game.UpdatePresentation(0);Assert.AreEqual(0,game.ArtWorld.ActiveEffects);Assert.Zero(game.ImpactAudio.ActiveVoices);Assert.Zero(game.ImpactParticles.ParticleCount);
            // Exercise actual b-monster geometry in the product view, with no a-monster fallback.
            for(var i=0;i<10;i++)host.Tick(.1);game.UpdatePresentation(.1f);window.Repaint();yield return null;
            Assert.AreEqual("창세기 / 가인 · 3분 시제품",AdamRoundPresentation.Title(host));
        }
        finally{window.Close();}
    }
    [UnityTest] public IEnumerator HeldMovementAcrossEntryRequiresReleaseAndFreshPress()
    {
        game.Host.Start("sling");Keys();yield return null;yield return null;Keys(Key.D);yield return new WaitForSecondsRealtime(.1f);Assert.Greater(game.Host.World.PlayerPosition.X,0);
        CompleteExisting();var old=game.Host.World;game.Host.SetPaused(false);Assert.AreNotSame(old,game.Host.World);
        for(var i=0;i<4;i++)yield return null;
        Assert.AreEqual(System.Numerics.Vector2.Zero,game.Host.World.PlayerPosition,"old round held movement must be released");
        Keys();yield return null;Keys(Key.D);yield return new WaitForSecondsRealtime(.1f);Assert.Greater(game.Host.World.PlayerPosition.X,0);
        game.SetLifecycle(false,true);var time=game.Host.World.SimulationTime;yield return null;game.SetLifecycle(true,false);yield return null;
        Assert.IsTrue(game.Host.Paused);Assert.AreEqual(time,game.Host.World.SimulationTime);Assert.AreEqual(Vector2.zero,game.Host.Movement);Keys();yield return null;
    }
    void CompleteExisting()
    {
        var host=game.Host;host.Session.Advance(180);host.SetPaused(true);
        while(host.Session.PendingChoices.Count>0)Assert.IsTrue(host.Choose(host.Session.PendingChoices[0]));Assert.IsTrue(host.Checkpoint());
    }
    [UnityTest] public IEnumerator R02ContactDeathBlocksDigitsUntilClipThenStartsOnlyR01()
    {
        CompleteR01();var host=game.Host;host.SetPaused(false);
        for(var i=0;i<30 && host.World.Capture().Enemies.Count==0;i++)host.Tick(.1);
        Assert.IsTrue(host.Checkpoint());var store=new SaveStore(Path.Combine(directory,"save.json"));var state=store.Load();
        state.Run.Health=1;state.Run.World.Enemies[0].X=state.Run.World.PlayerX+.1f;state.Run.World.Enemies[0].Y=state.Run.World.PlayerY;
        store.Commit(state,state.Revision,Guid.NewGuid().ToString("N"));Assert.IsTrue(host.Reload());game.ArtWorld.Update(host,0);host.SetPaused(false);host.Tick(1d/60);game.UpdatePresentation(0);
        Assert.IsTrue(game.DeathPresentationVisible);var dead=host.Session;var gold=host.State.Profile.Gold;host.SetPaused(true);game.SetLifecycle(true,false);
        Keys(Key.Digit2);yield return null;Assert.AreSame(dead,host.Session);Assert.IsNull(host.State.Run);
        Keys();yield return null;host.SetPaused(false);yield return new WaitForSecondsRealtime(.85f);Assert.IsFalse(game.DeathPresentationVisible);
        Keys(Key.Digit2);yield return null;Assert.AreEqual(1,host.Session.RoundNumber);Assert.IsTrue(host.Session.Inventory.Weapons.ContainsKey("jawbone"));Assert.AreEqual(gold,host.State.Profile.Gold);Keys();yield return null;
    }
    [UnityTest] public IEnumerator EntryClearsLiveR01ImpactParticlesAudioAndReactionCaches()
    {
        var host=game.Host;host.Start("jawbone");
        for(var i=0;i<20 && host.World.Capture().Enemies.Count<2;i++)host.Tick(.1);
        host.Checkpoint();var store=new SaveStore(Path.Combine(directory,"save.json"));var state=store.Load();
        foreach(var e in state.Run.World.Enemies){e.X=1;e.Y=0;}state.Run.World.Weapons.Clear();
        store.Commit(state,state.Revision,Guid.NewGuid().ToString("N"));Assert.IsTrue(host.Reload());host.SetPaused(false);
        game.UpdatePresentation(0);host.Tick(1d/60);game.UpdatePresentation(0);
        Assert.Greater(game.ArtWorld.ActiveEffects,0);Assert.Greater(game.ImpactParticles.ParticleCount,0);Assert.Greater(game.ImpactAudio.PlayedCount,0);
        var generation=game.ArtWorld.Generation;CompleteExisting();host.SetPaused(false);game.UpdatePresentation(0);
        Assert.AreEqual(2,host.Session.RoundNumber);Assert.Greater(game.ArtWorld.Generation,generation);Assert.Zero(game.ArtWorld.ActiveEffects);Assert.Zero(game.ImpactParticles.ParticleCount);Assert.Zero(game.ImpactAudio.ActiveVoices);
        Assert.IsFalse(game.DeathPresentationVisible);Assert.AreEqual("idle",game.ArtWorld.PlayerMotion);yield return null;
    }
    [UnityTest] public IEnumerator R02FeedbackOnAndOffPreserveExactRunProfileAndWorld()
    {
        CompleteR01();var host=game.Host;host.SetPaused(false);
        for(var i=0;i<20 && host.World.Capture().Enemies.Count<2;i++)host.Tick(.1);
        host.Checkpoint();var path=Path.Combine(directory,"save.json");var store=new SaveStore(path);var state=store.Load();
        foreach(var e in state.Run.World.Enemies){e.X=1.4f;e.Y=0;}state.Run.World.Weapons.Clear();store.Commit(state,state.Revision,Guid.NewGuid().ToString("N"));
        game.enabled=false;string expected=null;
        foreach(var enabled in new[]{false,true})
        {
            var copy=Path.Combine(directory,enabled?"on.json":"off.json");File.Copy(path,copy);
            var replay=new AdamHost(AdamCatalog.Load(),new SaveStore(copy));game.Initialize(replay);game.ArtWorld.FeedbackEnabled=enabled;game.ImpactParticles.Enabled=enabled;game.ImpactAudio.Muted=!enabled;replay.SetPaused(false);
            for(var i=0;i<240 && replay.Session.IsActive && replay.Session.PendingChoices.Count==0;i++){replay.Tick(1d/60);game.UpdatePresentation(1f/60);}
            Assert.Greater(replay.World.Hits,0,"comparison must include actual R02 combat events");Assert.IsTrue(replay.Checkpoint(),replay.Error);
            var actual=Json(replay.State.Run)+Json(replay.State.Profile)+Json(replay.World.Capture());
            if(expected==null)expected=actual;else Assert.AreEqual(expected,actual,"presentation must not change battle or saved progression");
        }
        yield return null;
    }
    static string Json(object value)
    {using(var stream=new System.IO.MemoryStream()){new System.Runtime.Serialization.Json.DataContractJsonSerializer(value.GetType()).WriteObject(stream,value);return System.Text.Encoding.UTF8.GetString(stream.ToArray());}}
}
