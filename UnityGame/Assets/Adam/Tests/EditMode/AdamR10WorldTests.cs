using System;
using System.Linq;
using BibleSurvivor.Core;
using BibleSurvivor.Unity;
using NUnit.Framework;
using UnityEngine;

public sealed class AdamR10WorldTests
{
    [Test]
    public void ExodusActorsUseThreeReviewedFourFrameClipsAndSharedShadows()
    {
        using(var art=new AdamExodusR01Art())
        {
            var ids=new[]{"ex1","ex2","ex3"};var fps=new[]{8,8,6};
            Assert.AreEqual(3,art.Index.assets.Length);Assert.AreEqual(12,art.Index.assets.Sum(c=>c.frames));
            for(var i=0;i<ids.Length;i++)
            {
                Assert.IsTrue(AdamExodusR01Art.Enemy(ids[i]));var clip=art.Clip(AdamArtWorld.EnemyClip(ids[i]));
                Assert.AreEqual(AdamExodusR01Art.EnemyClip(ids[i]),clip.id);
                Assert.AreEqual(32,clip.width);Assert.AreEqual(32,clip.height);Assert.AreEqual(4,clip.frames);
                Assert.AreEqual(16,clip.pivotX);Assert.AreEqual(1,clip.pivotY);Assert.AreEqual(fps[i],clip.fps);Assert.IsTrue(clip.loop);
                Assert.AreEqual(1,clip.Frame(1d/fps[i]));Assert.AreEqual(0,clip.Frame(4d/fps[i]));
                Assert.AreEqual(i==1?"fx_shadow-small_static_12x4":"fx_shadow-wide_static_24x6",AdamRoundPresentation.Shadow(ids[i]));
                var rect=AdamArt.PlacedRect(clip,new Vector2(100,200),2);
                Assert.AreEqual(new Rect(68,138,64,64),rect);
            }
            Assert.IsFalse(AdamExodusR01Art.Enemy("i1"));Assert.Throws<InvalidOperationException>(()=>AdamExodusR01Art.EnemyClip("ex4"));
            Assert.Throws<InvalidOperationException>(()=>AdamExodusR01Art.ReadIndex("{}"));
        }
    }
    [Test]
    public void DerivedHitMasksReuseAlphaAndDisposeWithoutChangingSourceTextures()
    {
        var art=new AdamExodusR01Art();Texture2D mask=null,source=null;
        try
        {
            art.PrepareHitMasks();
            foreach(var clip in art.Index.assets)
            {
                source=art.Sprite(clip.id,0).texture;var original=source.GetPixels32();mask=art.HitMask(clip.id);
                Assert.AreNotSame(source,mask);Assert.AreEqual(FilterMode.Point,mask.filterMode);Assert.AreEqual(TextureWrapMode.Clamp,mask.wrapMode);
                var derived=mask.GetPixels32();
                // Sample an opaque and transparent pixel in every frame, covering mask registration and alpha.
                for(var frame=0;frame<4;frame++)
                {
                    var opaque=-1;var transparent=-1;
                    for(var y=0;y<32;y++)for(var x=frame*32;x<(frame+1)*32;x++)
                    {var n=y*source.width+x;if(original[n].a!=0)opaque=n;else transparent=n;}
                    Assert.GreaterOrEqual(opaque,0);Assert.GreaterOrEqual(transparent,0);
                    Assert.AreEqual(new Color32(255,255,255,original[opaque].a),derived[opaque]);Assert.AreEqual(new Color32(0,0,0,0),derived[transparent]);
                }
                art.PrepareHitMasks();Assert.AreSame(mask,art.HitMask(clip.id));CollectionAssert.AreEqual(original,source.GetPixels32());
            }
            art.Dispose();art.Dispose();Assert.IsTrue(mask==null);Assert.IsFalse(source==null);
            Assert.Throws<ObjectDisposedException>(()=>art.PrepareHitMasks());
        }
        finally{art.Dispose();}
    }
    [Test]
    public void ExodusActorsShareFootDepthOrderWhenFeedbackIsLowWithoutChangingCoreCoordinates()
    {
        var actors=new[]{new BattleEntityState{Id=3,X=1,Y=-1},new BattleEntityState{Id=1,X=0,Y=1},new BattleEntityState{Id=2,X=2,Y=0}};
        using(var view=new AdamArtWorld{FeedbackEnabled=false})
        {
            var projection=new AdamWorldProjection(new Rect(20,116,620,246),Vector2.zero);var sprites=view.PrepareEnemySprites(actors,projection);
            CollectionAssert.AreEqual(new[]{1,2,3},sprites.Select(s=>s.Enemy.Id));
            foreach(var actor in sprites)Assert.AreEqual(projection.Point(actor.Enemy.X,actor.Enemy.Y),actor.Feet);
            CollectionAssert.AreEqual(new[]{3,1,2},actors.Select(a=>a.Id));Assert.Zero(view.EnemyFlashOpacity(1));
        }
    }
}
