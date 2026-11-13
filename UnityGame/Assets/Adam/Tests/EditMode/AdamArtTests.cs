using System;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Json;
using BibleSurvivor.Core;
using BibleSurvivor.Unity;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public sealed class AdamArtTests
{
    AdamArt art;
    [SetUp] public void Setup() { art=new AdamArt(); }
    [Test] public void EveryReviewedPngImportsExactFramesPivotsAlphaAndPointPixels()
    {
        var root=Path.GetFullPath(Path.Combine(Application.dataPath,"../../art/art-concept-03"));
        foreach(var c in art.Index.assets)
        {
            var path="Assets/Adam/Resources/Art03/"+c.id+".png";
            CollectionAssert.AreEqual(File.ReadAllBytes(Path.Combine(root,c.source)),File.ReadAllBytes(path),c.id);
            var importer=(TextureImporter)AssetImporter.GetAtPath(path);
            Assert.AreEqual(FilterMode.Point,importer.filterMode,c.id); Assert.IsFalse(importer.mipmapEnabled);
            Assert.AreEqual(TextureImporterCompression.Uncompressed,importer.textureCompression);
            for(var i=0;i<c.frames;i++)
            {
                var sprite=art.Sprite(c.id,i);
                Assert.AreEqual(new Rect(i*c.width,0,c.width,c.height),sprite.rect,c.id);
                Assert.AreEqual(new Vector2(c.pivotX,c.pivotY),sprite.pivot,c.id);
                Assert.AreEqual(32,sprite.pixelsPerUnit,c.id);
                Assert.AreEqual(new Vector4(c.borders[0],c.borders[1],c.borders[2],c.borders[3]),sprite.border,c.id);
            }
            foreach(var pixel in art.Sprite(c.id,0).texture.GetPixels32()) Assert.Contains((int)pixel.a,c.alphaValues,c.id);
        }
    }
    [Test] public void AllSixLayersShareRegisteredFramesAndDeclaredPlayback()
    {
        CollectionAssert.AreEqual(new[]{"cloak_back","base","hair_short","beard","hood","cloak_front"},art.Index.layers);
        foreach(var motion in new[]{"idle","walk","death"})
        {
            var expectedFrames=motion=="death"?6:4; var expectedFps=motion=="idle"?6:8;
            foreach(var layer in art.Index.layers)
            {
                var c=art.Clip("chr_player_layer-"+layer.Replace('_','-')+"_"+motion+"_32");
                Assert.AreEqual(expectedFrames,c.frames); Assert.AreEqual(expectedFps,c.fps); Assert.AreEqual(motion!="death",c.loop);
                Assert.AreEqual(new Vector2(16,1),art.Sprite(c.id,0).pivot);
                Assert.AreEqual(motion=="death"?5:0,c.Frame(4));
                Assert.AreEqual(1,c.Frame(1d/expectedFps));
            }
        }
        Assert.AreEqual(0,art.Clip("enm_a1-small-snake_move_32").Frame(.5));
        Assert.AreEqual(2,art.Clip("enm_a2-fruit-moth_fly_32").Frame(.5));
        Assert.AreEqual(3,art.Clip("enm_a3-thorn-beast_walk_32").Frame(.5));
        Assert.AreEqual(3,art.Clip("fx_jawbone-swing-edge_hit_64").Frame(10));
    }
    [Test] public void ForcedReimportRetainsAssetAndEverySpriteIdentity()
    {
        foreach(var c in art.Index.assets)
        {
            var path="Assets/Adam/Resources/Art03/"+c.id+".png"; var before=Identities(path);
            AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceUpdate);
            CollectionAssert.AreEqual(before,Identities(path),c.id);
        }
    }
    static string[] Identities(string path) => AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().Select(s=>{
        AssetDatabase.TryGetGUIDAndLocalFileIdentifier(s,out string guid,out long local); return s.name+":"+guid+":"+local;
    }).OrderBy(x=>x,StringComparer.Ordinal).ToArray();
    [Test] public void SupportedPathIsOneCellWideAndAllMasksUseNeswBitsWithoutRotation()
    {
        Assert.AreEqual(15,AdamArtWorld.PathMask(0,0)); Assert.AreEqual(5,AdamArtWorld.PathMask(0,3)); Assert.AreEqual(10,AdamArtWorld.PathMask(3,0));
        for(var x=-10;x<10;x++) for(var y=-10;y<10;y++)
            Assert.IsFalse(AdamArtWorld.PathCell(x,y)&&AdamArtWorld.PathCell(x+1,y)&&AdamArtWorld.PathCell(x,y+1)&&AdamArtWorld.PathCell(x+1,y+1));
        Assert.AreEqual(16,art.Clip("til_eden_path-16mask_static_32").frames);
    }
    [Test] public void PixelRegistrationAndSmallWindowKeepIntegerUndistortedScale()
    {
        Assert.AreEqual(2,AdamArtWorld.PixelScale(new Rect(20,116,1060,526)));
        Assert.AreEqual(1,AdamArtWorld.PixelScale(new Rect(20,116,620,246)));
        var rect=AdamArt.PlacedRect(art.Clip("chr_player_layer-base_idle_32"),new Vector2(99.7f,80.1f),2);
        Assert.AreEqual(new Rect(68,18,64,64),rect);
        Assert.AreEqual(3,art.Clip("ui_card_9slice_24").states.ToList().IndexOf("disabled"));
    }
    [Test] public void EffectRingReusesAndFullyInitializesTheWrappedSlot()
    {
        var pool=new AdamArtWorld.EffectPool();var first=pool.Slots[0];
        for(var i=0;i<=AdamArtWorld.EffectCapacity;i++) pool.Emit(i==0?"old":"new",new Vector2(i,-i),i);
        Assert.AreSame(first,pool.Slots[0]);Assert.AreEqual("new",first.Clip);
        Assert.AreEqual(new Vector2(64,-64),first.Position);Assert.AreEqual(64,first.Started);Assert.IsTrue(first.Active);
        Assert.AreEqual(64,pool.Slots.Length);pool.Clear();
        Assert.IsTrue(pool.Slots.All(e=>!e.Active&&e.Clip==null&&e.Position==Vector2.zero&&e.Started==0));
        pool.Emit("restart",Vector2.one,0);Assert.AreEqual("restart",first.Clip);
    }
    [Test] public void ModifiedContractIsRejectedBeforeAnyTextureLoads()
    {
        var json=Resources.Load<TextAsset>("Art03/index").text;Assert.IsTrue(AdamArt.ReviewedIndex(json));
        Assert.IsFalse(AdamArt.ReviewedIndex(json.Replace("\"ppu\": 32","\"ppu\": 16")));
    }
    [Test] public void DirectionTransformKeepsAnchorAndMapsShotRightAxisWithoutDoubleFlip()
    {
        var anchor=new Vector2(100,200);
        foreach(var dir in new[]{Vector2.left,Vector2.right,Vector2.up,Vector2.down,new Vector2(-1,1).normalized})
        {
            var matrix=AdamArtWorld.FacingMatrix(anchor,dir);
            Assert.Less(((Vector2)matrix.MultiplyPoint3x4(anchor)-anchor).magnitude,.001);
            var end=(Vector2)matrix.MultiplyPoint3x4(anchor+Vector2.right)-anchor;
            Assert.Less((end-new Vector2(dir.x,-dir.y)).magnitude,.001);
        }
        var c=art.Clip("fx_jawbone-swing-edge_hit_64");
        CollectionAssert.AreEqual(new[]{59,60,49,47},c.attachX);CollectionAssert.AreEqual(new[]{44,24,8,6},c.attachY);
        var weapon=new BattleWeaponState {ActiveSince=1,PendingBursts=new System.Collections.Generic.List<double>{1.2}};
        Assert.AreEqual(1,AdamArtWorld.LatestJawboneBurst(weapon,1.25,2,.2));
        weapon.PendingBursts.Clear();Assert.AreEqual(1.2,AdamArtWorld.LatestJawboneBurst(weapon,1.25,2,.2));
    }
    static string Json(object value) { using(var stream=new MemoryStream()) { new DataContractJsonSerializer(value.GetType()).WriteObject(stream,value); return Convert.ToBase64String(stream.ToArray()); } }
    [TestCase("bible")] [TestCase("jawbone")] [TestCase("sling")]
    public void ProjectionCannotAlterCombatAndRebuildsFacingAfterSaveReload(string weapon)
    {
        var dir=Path.Combine(Path.GetTempPath(),"adam-art-"+Guid.NewGuid().ToString("N")); Directory.CreateDirectory(dir);
        try
        {
            var a=new AdamHost(AdamCatalog.Load(),new SaveStore(Path.Combine(dir,"a.json")));
            var b=new AdamHost(AdamCatalog.Load(),new SaveStore(Path.Combine(dir,"b.json")));
            a.Start(weapon); b.Start(weapon); var view=new AdamArtWorld();
            for(var i=0;i<500;i++)
            {
                var move=new Vector2(i<200?-1:1,0); a.SetMovement(move);b.SetMovement(move);a.Tick(1d/60);b.Tick(1d/60);
                view.Update(a,1f/60); view.Update(a,1f/60); // Repeated presentation sampling cannot advance or hit anything.
                Assert.AreEqual(Json(b.World.Capture()),Json(a.World.Capture()),"step "+i);
                if(i==100) { Assert.AreEqual("walk",view.PlayerMotion); Assert.IsTrue(view.PlayerLeft); }
            }
            a.SetMovement(Vector2.left);a.Tick(.1);view.Update(a,.1f);a.SetPaused(true);
            var time=view.Clock; var frame=art.Clip("chr_player_layer-base_walk_32").Frame(view.PlayerSeconds);
            for(var i=0;i<20;i++) {a.Tick(.1);view.Update(a,.1f);}
            Assert.AreEqual(time,view.Clock);Assert.AreEqual(frame,art.Clip("chr_player_layer-base_walk_32").Frame(view.PlayerSeconds));
            var before=Json(a.World.Capture());a.Reload();view.Update(a,0);
            Assert.AreEqual(before,Json(a.World.Capture()));Assert.IsTrue(view.PlayerLeft);Assert.AreEqual("idle",view.PlayerMotion);
            view.Clear(); Assert.IsNull(view.Snapshot);Assert.AreEqual(0,view.ActiveEffects);
            Assert.IsTrue(view.Effects.All(e=>e.Clip==null && e.Position==Vector2.zero && e.Started==0));
        }
        finally { Directory.Delete(dir,true); }
    }
}
