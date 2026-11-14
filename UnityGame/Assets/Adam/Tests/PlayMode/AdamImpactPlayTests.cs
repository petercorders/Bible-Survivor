using System;
using System.Collections;
using System.IO;
using System.Linq;
using BibleSurvivor.Core;
using BibleSurvivor.Unity;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public sealed class AdamImpactPlayTests
{
    [UnityTest] public IEnumerator RealImpactsPlayBoundedOnceAndMutePauseLifecycleReloadStopVoices()
    {
        var dir=Path.Combine(Path.GetTempPath(),"adam-audio-"+Guid.NewGuid().ToString("N"));var owner=new GameObject("impact audio test");
        var store=new SaveStore(Path.Combine(dir,"save.json"));var host=new AdamHost(AdamCatalog.Load(),store);host.Start("jawbone");
        for(var i=0;i<20&&host.World.Capture().Enemies.Count<2;i++)host.Tick(.1);
        host.Checkpoint();var state=store.Load();foreach(var e in state.Run.World.Enemies){e.X=1;e.Y=0;}state.Run.World.Weapons.Clear();store.Commit(state,state.Revision,Guid.NewGuid().ToString("N"));Assert.IsTrue(host.Reload());host.SetPaused(false);
        var view=new AdamArtWorld();var audio=new AdamImpactAudio(owner);
        try
        {
            view.Update(host,0);audio.Update(host,view);Assert.Zero(audio.PlayedCount);
            host.Tick(1d/60);view.Update(host,0);audio.Update(host,view);
            Assert.AreEqual(1,audio.PlayedCount,"same-step jawbone group is audibly rate limited");
            Assert.AreEqual(AdamImpactAudio.VoiceCapacity,owner.GetComponentsInChildren<AudioSource>().Length);Assert.LessOrEqual(audio.ActiveVoices,AdamImpactAudio.VoiceCapacity);
            var played=audio.PlayedCount;audio.Update(host,view);Assert.AreEqual(played,audio.PlayedCount,"same event cannot replay");
            Assert.IsTrue(owner.GetComponentsInChildren<AudioSource>().Any(s=>s.isPlaying),"real source must start playing");
            audio.Muted=true;audio.Update(host,view);Assert.Zero(audio.ActiveVoices);Assert.IsFalse(owner.GetComponentsInChildren<AudioSource>().Any(s=>s.isPlaying));
            audio.Muted=false;audio.Update(host,view);Assert.AreEqual(played,audio.PlayedCount);
            host.SetPaused(true);audio.Update(host,view);Assert.Zero(audio.ActiveVoices);
            host.SetSuspended(true);audio.Update(host,view);Assert.Zero(audio.ActiveVoices);
            host.SetSuspended(false);host.Reload();view.Update(host,0);audio.Update(host,view);Assert.Zero(audio.ActiveVoices);Assert.AreEqual(played,audio.PlayedCount);
            yield return null;
        }
        finally{audio.Dispose();UnityEngine.Object.Destroy(owner);}
        yield return null;if(Directory.Exists(dir))Directory.Delete(dir,true);
    }
}
