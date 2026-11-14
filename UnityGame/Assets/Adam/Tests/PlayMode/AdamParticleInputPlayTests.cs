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

public sealed class AdamParticleInputPlayTests
{
    [UnityTest] public IEnumerator RealF9PressAndHoldToggleOnlyOnceAndDisableReenableReleasesOwnedPool()
    {
        var path=Path.Combine(Path.GetTempPath(),"adam-particle-input-"+Guid.NewGuid().ToString("N"));var obj=new GameObject("product particle keyboard regression");obj.SetActive(false);
        var game=obj.AddComponent<AdamGame>();game.AutoInitialize=false;
        var background=InputSystem.settings.backgroundBehavior;var editorInput=InputSystem.settings.editorInputBehaviorInPlayMode;var run=Application.runInBackground;
        InputSystem.settings.backgroundBehavior=InputSettings.BackgroundBehavior.IgnoreFocus;InputSystem.settings.editorInputBehaviorInPlayMode=InputSettings.EditorInputBehaviorInPlayMode.AllDeviceInputAlwaysGoesToGameView;Application.runInBackground=true;
        var keys=InputSystem.AddDevice<Keyboard>();
        try
        {
            obj.SetActive(true);var host=new AdamHost(AdamCatalog.Load(),new SaveStore(Path.Combine(path,"save.json")));host.Start("bible");game.Initialize(host);yield return null;
            Assert.IsTrue(game.ImpactParticles.Enabled);InputSystem.QueueStateEvent(keys,new KeyboardState(Key.F9));yield return null;Assert.IsFalse(game.ImpactParticles.Enabled);
            yield return null;Assert.IsFalse(game.ImpactParticles.Enabled,"held key cannot toggle repeatedly");
            InputSystem.QueueStateEvent(keys,new KeyboardState());yield return null;InputSystem.QueueStateEvent(keys,new KeyboardState(Key.F9));yield return null;Assert.IsTrue(game.ImpactParticles.Enabled);
            InputSystem.QueueStateEvent(keys,new KeyboardState());yield return null;
            game.enabled=false;yield return null;Assert.IsNull(game.ImpactParticles,"disable releases owned resources");
            game.enabled=true;yield return null;Assert.NotNull(game.ImpactParticles);Assert.AreEqual(16,game.ImpactParticles.OverlayCamera.transform.parent.GetComponentsInChildren<ParticleSystem>().Length);
        }
        finally{InputSystem.RemoveDevice(keys);InputSystem.settings.backgroundBehavior=background;InputSystem.settings.editorInputBehaviorInPlayMode=editorInput;Application.runInBackground=run;UnityEngine.Object.Destroy(obj);}
        yield return null;if(Directory.Exists(path))Directory.Delete(path,true);
    }
}
