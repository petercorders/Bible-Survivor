using System;
using System.Collections;
using System.IO;
using System.Linq;
using BibleSurvivor.Core;
using BibleSurvivor.Unity;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.TestTools;

public class AdamPlayTests
{
    GameObject owner;
    AdamGame game;
    Keyboard keyboard;
    Touchscreen touchscreen;
    InputSettings.BackgroundBehavior previousBackground;
    bool previousRunInBackground;
#if UNITY_EDITOR
    InputSettings.EditorInputBehaviorInPlayMode previousEditorInput;
#endif
    string directory;
    [UnitySetUp] public IEnumerator Setup()
    {
        directory = Path.Combine(Path.GetTempPath(), "adam-play-" + Guid.NewGuid().ToString("N"));
        owner = new GameObject("PlayMode test host"); game = owner.AddComponent<AdamGame>(); game.AutoInitialize = false;
        game.Initialize(new AdamHost(AdamCatalog.Load(), new SaveStore(Path.Combine(directory, "save.json"))));
        previousBackground = InputSystem.settings.backgroundBehavior;
        InputSystem.settings.backgroundBehavior = InputSettings.BackgroundBehavior.IgnoreFocus;
        previousRunInBackground = Application.runInBackground; Application.runInBackground = true;
#if UNITY_EDITOR
        previousEditorInput = InputSystem.settings.editorInputBehaviorInPlayMode;
        InputSystem.settings.editorInputBehaviorInPlayMode = InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;
#endif
        keyboard = InputSystem.AddDevice<Keyboard>(); yield return null;
    }
    [UnityTearDown] public IEnumerator Cleanup()
    {
        InputSystem.RemoveDevice(keyboard);
        if (touchscreen != null) InputSystem.RemoveDevice(touchscreen);
        InputSystem.settings.backgroundBehavior = previousBackground;
        Application.runInBackground = previousRunInBackground;
#if UNITY_EDITOR
        InputSystem.settings.editorInputBehaviorInPlayMode = previousEditorInput;
#endif
        UnityEngine.Object.Destroy(owner); yield return null;
        if (Directory.Exists(directory)) Directory.Delete(directory, true);
    }
    void Keys(params Key[] keys) { InputSystem.QueueStateEvent(keyboard, new KeyboardState(keys)); }
    [UnityTest] public IEnumerator KeyboardSelectionMovementPauseAndLifecycle()
    {
        Keys(Key.Digit3); yield return null; Keys(); yield return null;
        Assert.IsNotNull(game.Host.World, $"keyboard={Keyboard.current?.deviceId}, enabled={keyboard.enabled}, focus={Application.isFocused}, suspended={game.Host.Suspended}, error={game.Host.Error}"); Assert.IsTrue(game.Host.Session.Inventory.Weapons.ContainsKey("sling"));
        Keys(Key.D); yield return new WaitForSecondsRealtime(.15f);
        Assert.Greater(game.Host.World.PlayerPosition.X, 0);
        Keys(Key.Escape); yield return null; Keys(); yield return null;
        var time = game.Host.World.SimulationTime;
        for (var i = 0; i < 5; i++) yield return null;
        Assert.AreEqual(time, game.Host.World.SimulationTime);
        game.SetLifecycle(false, false); game.SetLifecycle(true, false);
        Assert.IsTrue(game.Host.Paused); Assert.AreEqual(Vector2.zero, game.Host.Movement);
        Keys(Key.Enter); yield return null; Keys(); yield return null;
        Assert.IsFalse(game.Host.Paused);
    }
    [UnityTest] public IEnumerator HeldMovementDoesNotLeakAcrossPauseAndResume()
    {
        game.Host.Start("bible"); yield return null;
        Keys(Key.D); yield return null; game.Host.SetPaused(true); yield return null;
        var position = game.Host.World.PlayerPosition;
        game.Host.SetPaused(false); for (var i = 0; i < 4; i++) yield return null;
        Assert.AreEqual(position, game.Host.World.PlayerPosition);
        Keys(); yield return null; Keys(Key.D); yield return new WaitForSecondsRealtime(.15f);
        Assert.Greater(game.Host.World.PlayerPosition.X, position.X, $"held={keyboard.dKey.isPressed}, current={Keyboard.current?.deviceId}, time={game.Host.World.SimulationTime}, canPlay={game.Host.CanPlay}, paused={game.Host.Paused}, suspended={game.Host.Suspended}");
    }
    [Test] public void KeyboardAndTouchUseSameNormalizedVector()
    {
        Keys(Key.W, Key.D);
        InputSystem.Update();
        var keyboardVector = AdamGame.KeyboardMovement(keyboard);
        var touchVector = AdamGame.DragMovement(Vector2.zero, new Vector2(80, 80));
        Assert.Less((keyboardVector - touchVector).magnitude, .00001);
        Assert.AreEqual(1, keyboardVector.magnitude, .00001);
    }
    [UnityTest] public IEnumerator Digit1WaitsForDeathClipBeforeStartingBible() => DeathDigitBoundary(Key.Digit1,"bible");
    [UnityTest] public IEnumerator Digit2WaitsForDeathClipBeforeStartingJawbone() => DeathDigitBoundary(Key.Digit2,"jawbone");
    [UnityTest] public IEnumerator Digit3WaitsForDeathClipBeforeStartingSling() => DeathDigitBoundary(Key.Digit3,"sling");
    IEnumerator DeathDigitBoundary(Key digit,string weapon)
    {
        var host=game.Host;host.Start("sling");yield return null;
        for(var i=0;i<100 && host.World.Capture().Enemies.Count==0;i++)host.Tick(.1);
        Assert.Greater(host.World.Capture().Enemies.Count,0);Assert.IsTrue(host.Checkpoint(),host.Error);
        var store=new SaveStore(Path.Combine(directory,"save.json"));var fixture=store.Load();fixture.Run.Health=1;
        fixture.Run.World.Enemies[0].X=fixture.Run.World.PlayerX+.1f;fixture.Run.World.Enemies[0].Y=fixture.Run.World.PlayerY;
        store.Commit(fixture,fixture.Revision,Guid.NewGuid().ToString("N"));Assert.IsTrue(host.Reload(),host.Error);
        game.ArtWorld.Update(host,0);host.SetPaused(false);host.Tick(1d/60);game.ArtWorld.Update(host,0);
        Assert.IsNull(host.Session.Wave);Assert.IsFalse(host.Session.IsActive);Assert.IsTrue(game.DeathPresentationVisible);
        var terminal=host.Session;var gold=host.State.Profile.Gold;var time=host.World.SimulationTime;
        // Freeze only the presentation clock: the component and its actual InputSystem Update remain enabled.
        host.SetPaused(true);game.SetLifecycle(true,false);
        Action unchanged=()=>{
            Assert.AreSame(terminal,host.Session,"Digit input must not bypass the visible death clip");
            Assert.IsNull(host.State.Run);Assert.AreEqual(gold,host.State.Profile.Gold);
            Assert.AreEqual(time,host.World.SimulationTime);Assert.IsTrue(game.DeathPresentationVisible);
        };
        Keys(digit);yield return null;unchanged();
        Keys();yield return null;
        Keys(Key.Digit1,Key.Digit2,Key.Digit3);yield return null;unchanged();
        Keys();yield return null;
        game.SetLifecycle(false,true);Keys(digit);yield return null;unchanged();
        Keys();yield return null;game.SetLifecycle(true,false);
        // Hold across the end boundary: an earlier rejected press must never become a delayed restart.
        Keys(digit);yield return null;unchanged();host.SetPaused(false);
        var deadline=Time.realtimeSinceStartup+5;
        while(game.DeathPresentationVisible && Time.realtimeSinceStartup<deadline)yield return null;
        Assert.IsFalse(game.DeathPresentationVisible);yield return null;
        Assert.AreSame(terminal,host.Session);Assert.IsNull(host.State.Run);Assert.AreEqual(gold,host.State.Profile.Gold);
        Keys();yield return null;Keys(digit);yield return null;
        Assert.AreNotSame(terminal,host.Session);Assert.IsTrue(host.Session.IsActive);Assert.IsNotNull(host.State.Run);
        Assert.IsTrue(host.Session.Inventory.Weapons.ContainsKey(weapon));Assert.AreEqual(gold,host.State.Profile.Gold);
        var run=host.State.Run.RunId;yield return null;Assert.AreEqual(run,host.State.Run.RunId);
        Keys();yield return null;
    }
    [UnityTest] public IEnumerator SameFrameDoubleSelectionCannotStartAnotherChallenge()
    {
        game.SelectSlot(0); var id = game.Host.State.Run.RunId; game.SelectSlot(2);
        Assert.AreEqual(id, game.Host.State.Run.RunId); Assert.IsTrue(game.Host.Session.Inventory.Weapons.ContainsKey("bible"));
        yield return null;
    }
    [UnityTest] public IEnumerator TouchDragMovesAndFocusLossClearsHeldFinger()
    {
        touchscreen = InputSystem.AddDevice<Touchscreen>();
        game.Host.Start("sling"); yield return null;
        var origin = new Vector2(Screen.width * .15f, Screen.height * .2f);
        InputSystem.QueueStateEvent(touchscreen, new TouchState { touchId = 1, phase = UnityEngine.InputSystem.TouchPhase.Began, position = origin });
        yield return null;
        InputSystem.QueueStateEvent(touchscreen, new TouchState { touchId = 1, phase = UnityEngine.InputSystem.TouchPhase.Moved, position = origin + Vector2.right * 100 });
        yield return new WaitForSecondsRealtime(.15f);
        Assert.Greater(game.Host.World.PlayerPosition.X, 0);
        game.SetLifecycle(false, true); var time = game.Host.World.SimulationTime;
        game.SetLifecycle(true, false); game.Host.SetPaused(false);
        yield return new WaitForSecondsRealtime(.1f);
        Assert.AreEqual(Vector2.zero, game.Host.Movement);
        Assert.LessOrEqual(game.Host.World.SimulationTime - time, .15);
    }
    [UnityTest] public IEnumerator OneTwoThreeChoicesAndSameFrameCommandsOnlyApplyOneUpgrade()
    {
        var passives = new[] { "scroll", "hair", "stones", "east_wind", "seal", "cloud", "thunder", "crown", "underwear", "whirlwind" };
        for (int count = 1; count <= 3; count++)
        {
            if (game.Host.State.Run != null) game.Host.State.Run = null; // Disposable boundary fixture, not gameplay evidence.
            game.Host.Start("bible");
            var run = game.Host.State.Run;
            run.PendingLevels = 2;
            run.Choices = game.Host.Session.Inventory.Choices(game.Host.State.Profile.UnlockedWeapons, passives, 3, run.ChoiceSerial).Take(count).ToList();
            Assert.IsTrue(game.Host.Checkpoint(), game.Host.Error); Assert.IsTrue(game.Host.Reload(), game.Host.Error);
            yield return null;
            Assert.AreEqual(count, game.Host.Session.PendingChoices.Count);
            var serial = game.Host.State.Run.ChoiceSerial; var revision = game.Host.State.Revision;
            game.SelectSlot(0); game.SelectSlot(count - 1);
            Assert.AreEqual(serial + 1, game.Host.State.Run.ChoiceSerial);
            Assert.AreEqual(1, game.Host.State.Run.PendingLevels);
            Assert.AreEqual(revision + 1, game.Host.State.Revision);
            Assert.LessOrEqual(game.Host.Session.PendingChoices.Count, 3);
            yield return null;
        }
    }
#if UNITY_EDITOR
    [UnityTest] public IEnumerator SmallPauseModalPointerClickResumesBattle()
    {
        game.Host.Start("sling"); game.Host.SetPaused(true);
        var window = UnityEngine.ScriptableObject.CreateInstance<PauseModalPointerWindow>();
        try
        {
            window.Game = game;
            window.minSize = window.maxSize = new Vector2(660, 440);
            window.position = new Rect(80, 80, 660, 440); window.ShowUtility(); window.Repaint();
            yield return null;
            game.SetLifecycle(true, false);
            var time = game.Host.World.SimulationTime;
            yield return new WaitForSecondsRealtime(.08f);
            Assert.IsTrue(game.Host.Paused); Assert.AreEqual(time, game.Host.World.SimulationTime);
            // A genuine IMGUI mouse click inside the visible modal, far below the old hidden HUD button.
            var point = new Vector2(330, 268);
            window.SendEvent(new Event { type = EventType.MouseDown, mousePosition = point, button = 0, clickCount = 1 });
            window.SendEvent(new Event { type = EventType.MouseUp, mousePosition = point, button = 0, clickCount = 1 });
            Assert.IsFalse(game.Host.Paused, "Clicking the small-window modal's own Resume button must resume");
            yield return new WaitForSecondsRealtime(.12f);
            Assert.Greater(game.Host.World.SimulationTime, time, "Resume click must advance battle, not merely change a label");
        }
        finally { window.Close(); }
    }
#endif
}

#if UNITY_EDITOR
public class PauseModalPointerWindow : UnityEditor.EditorWindow
{
    public AdamGame Game;
    void OnGUI() { if (Game != null) Game.DrawGameView(new Rect(0, 0, position.width, position.height)); }
}
#endif
