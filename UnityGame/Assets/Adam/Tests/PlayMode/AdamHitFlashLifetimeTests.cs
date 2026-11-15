using System.Collections;
using System.Linq;
using BibleSurvivor.Unity;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
public sealed class AdamHitFlashLifetimeTests
{
    [UnityTest] public IEnumerator RepeatedLoadAndDisposeLeavesNoNativeDerivedMasksAndPreservesResources()
    {
        var count=Resources.FindObjectsOfTypeAll<Texture2D>().Count(t=>t.name.EndsWith("_runtime-hit-mask"));Texture2D source=null;
        for(var i=0;i<10;i++)
        {
            var art=new AdamR02Art();art.PrepareHitMasks();var id=art.Index.assets[0].clip.id;var mask=art.HitMask(id);source=art.Sprite(id,0).texture;
            art.Dispose();art.Dispose();yield return null;Assert.IsTrue(mask==null);Assert.IsFalse(source==null);
        }
        Assert.AreEqual(count,Resources.FindObjectsOfTypeAll<Texture2D>().Count(t=>t.name.EndsWith("_runtime-hit-mask")));Assert.IsFalse(source==null);
    }
}
