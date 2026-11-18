using System;
using System.IO;
using System.Linq;
using BibleSurvivor.Core;
using BibleSurvivor.Unity;
using NUnit.Framework;
using UnityEngine;
public sealed class AdamHitFlashTests
{
    [Test] public void DerivedMasksKeepAllTwelveFrameAlphasAndOriginalBytesAndReuseWithoutAllocation()
    {
        using(var art=new AdamR02Art())
        {
            art.PrepareHitMasks();
            foreach(var entry in art.Index.assets)
            {
                var source=art.Sprite(entry.clip.id,0).texture;var before=source.GetPixels32();var mask=art.HitMask(entry.clip.id);var first=mask.GetPixels32();
                Assert.AreNotSame(source,mask);Assert.AreEqual(FilterMode.Point,mask.filterMode);Assert.AreEqual(TextureWrapMode.Clamp,mask.wrapMode);Assert.AreEqual(1,mask.mipmapCount);Assert.AreEqual(source.width,mask.width);Assert.AreEqual(source.height,mask.height);
                for(var i=0;i<before.Length;i++)Assert.AreEqual(before[i].a==0?new Color32(0,0,0,0):new Color32(255,255,255,before[i].a),first[i],"derived alpha pixel "+i);
                art.PrepareHitMasks();Assert.AreSame(mask,art.HitMask(entry.clip.id));CollectionAssert.AreEqual(before,source.GetPixels32());
                var allocated=GC.GetAllocatedBytesForCurrentThread();for(var i=0;i<1000;i++)art.HitMask(entry.clip.id);Assert.Zero(GC.GetAllocatedBytesForCurrentThread()-allocated);
            }
        }
    }
    [Test] public void MasksAreDeterministicAndDisposeOnlyDerivedTextures()
    {
        var a=new AdamR02Art();var b=new AdamR02Art();
        try
        {
            a.PrepareHitMasks();b.PrepareHitMasks();var entry=a.Index.assets[0];var original=a.Sprite(entry.clip.id,0).texture;var mask=a.HitMask(entry.clip.id);
            CollectionAssert.AreEqual(mask.GetPixels32(),b.HitMask(entry.clip.id).GetPixels32());a.Dispose();a.Dispose();Assert.IsTrue(mask==null);Assert.IsFalse(original==null);
            Assert.Throws<ObjectDisposedException>(()=>a.PrepareHitMasks());Assert.Throws<ObjectDisposedException>(()=>a.HitMask(entry.clip.id));
        }
        finally{a.Dispose();b.Dispose();}
    }
    [Test] public void ActualHitFlashIsTargetOnlyBriefPausedLowSafeAndClearedOnReload()
    {
        var directory=Path.Combine(Path.GetTempPath(),"adam-flash-"+Guid.NewGuid().ToString("N"));
        try
        {
            var host=AdamImpactTests.Fixture(Path.Combine(directory,"save.json"));using(var view=new AdamArtWorld())
            {
                view.Update(host,0);host.Tick(1d/60);view.Update(host,0);var target=view.FrameImpacts.First(x=>x.TargetId>0&&!x.Killed).TargetId;
                Assert.Greater(view.EnemyFlashOpacity(target),0);Assert.Zero(view.EnemyFlashOpacity(int.MaxValue));
                view.FeedbackEnabled=false;if(view.PolishEnabled)Assert.Greater(view.EnemyFlashOpacity(target),0);else Assert.Zero(view.EnemyFlashOpacity(target));view.FeedbackEnabled=true;
                host.SetPaused(true);var clock=view.Clock;view.Update(host,10);Assert.AreEqual(clock,view.Clock);Assert.Greater(view.EnemyFlashOpacity(target),0);
                host.SetPaused(false);for(var i=0;i<3;i++){host.Tick(1d/60);view.Update(host,0);}Assert.Zero(view.EnemyFlashOpacity(target));Assert.IsTrue(view.EnemyReacting(target),"brief flash ends before original .09s reaction");
                host.Checkpoint();Assert.IsTrue(host.Reload());view.Update(host,0);Assert.Zero(view.EnemyFlashOpacity(target));Assert.IsFalse(view.EnemyReacting(target));view.Clear();Assert.Zero(view.EnemyFlashOpacity(target));
            }
        }
        finally{if(Directory.Exists(directory))Directory.Delete(directory,true);}
    }
}
