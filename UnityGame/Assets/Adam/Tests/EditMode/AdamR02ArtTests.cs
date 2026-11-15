using System;
using System.IO;
using System.Linq;
using BibleSurvivor.Unity;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
public sealed class AdamR02ArtTests
{
    AdamR02Index Fresh()=>JsonUtility.FromJson<AdamR02Index>(Resources.Load<TextAsset>("ArtR02/index").text);
    [Test] public void TwelveFramesHaveExactReviewedPixelsPlaybackAndExplicitMappings()
    {
        var art=new AdamR02Art();Assert.AreEqual(3,art.Index.assets.Length);Assert.AreEqual(79,new AdamArt().Index.assets.Length);
        foreach(var entry in art.Index.assets)
        {
            var c=entry.clip;Assert.AreEqual(c.id,AdamArtWorld.EnemyClip(entry.monster));
            var source=Path.GetFullPath(Path.Combine(Application.dataPath,"../../",c.source));var imported="Assets/Adam/Resources/ArtR02/"+c.id+".png";
            CollectionAssert.AreEqual(File.ReadAllBytes(source),File.ReadAllBytes(imported));
            var settings=(TextureImporter)AssetImporter.GetAtPath(imported);Assert.AreEqual(TextureImporterCompression.Uncompressed,settings.textureCompression);Assert.IsFalse(settings.mipmapEnabled);Assert.IsFalse(settings.alphaIsTransparency);
            for(var i=0;i<4;i++){Assert.AreEqual(i,c.Frame(i/(double)c.fps));Assert.AreEqual(new Vector2(16,1),art.Sprite(c.id,i).pivot);Assert.AreEqual(new Rect(i*32,0,32,32),art.Sprite(c.id,i).rect);}
            Assert.Zero(c.Frame(4d/c.fps));Assert.AreEqual(3,c.Frame(4d/c.fps-1e-5));
        }
        Assert.Throws<InvalidOperationException>(()=>AdamArtWorld.EnemyClip("unknown"));Assert.Throws<InvalidOperationException>(()=>art.Clip("enm_a1-small-snake_move_32"));
    }
    [TestCase("missing")] [TestCase("duplicate")] [TestCase("id")] [TestCase("hash")] [TestCase("pivot")] [TestCase("fps")] [TestCase("frames")] [TestCase("ppu")] [TestCase("shadow")] [TestCase("ground")] [TestCase("border")] [TestCase("sorting")]
    public void UnsupportedContractFailsEvenWithoutTheOuterHashGate(string problem)
    {
        var index=Fresh();var e=index.assets[0];var c=e.clip;
        switch(problem){case "missing":index.assets=index.assets.Take(2).ToArray();break;case "duplicate":index.assets[2]=e;break;case "id":c.id="enm_a1-small-snake_move_32";break;case "hash":c.sha256=new string('0',64);break;case "pivot":c.pivotY=4;break;case "fps":c.fps=9;break;case "frames":c.frames=3;break;case "ppu":index.ppu=16;break;case "shadow":e.shadow="fx_shadow-wide_static_24x6";break;case "ground":e.ground=true;break;case "border":c.borders[0]=1;break;case "sorting":c.sorting="UI";break;}
        Assert.Throws<InvalidOperationException>(()=>AdamR02Art.ValidateContract(index));
    }
    [Test] public void UnreviewedIndexBytesAndMissingFramesAreRejected()
    {
        var json=Resources.Load<TextAsset>("ArtR02/index").text;Assert.Throws<InvalidOperationException>(()=>AdamR02Art.ReadIndex(json+" "));Assert.Throws<InvalidOperationException>(()=>AdamR02Art.ReadIndex("{"));
        Assert.Throws<InvalidOperationException>(()=>AdamR02Art.ValidateSprites(Fresh().assets[0],new Sprite[3]));
    }
    [TestCase("pivot")] [TestCase("rect")] [TestCase("pixels")] public void BadImportedRegistrationOrPixelsAreRejected(string problem)
    {
        var art=new AdamR02Art();var entry=art.Index.assets[0];var c=entry.clip;var texture=art.Sprite(c.id,0).texture;Texture2D copy=null;var frames=new Sprite[4];
        try
        {
            if(problem=="pixels"){copy=new Texture2D(128,32,TextureFormat.RGBA32,false);copy.filterMode=FilterMode.Point;copy.wrapMode=TextureWrapMode.Clamp;var pixels=texture.GetPixels32();pixels[0]=new Color32(255,0,0,255);copy.SetPixels32(pixels);copy.Apply();texture=copy;}
            for(var i=0;i<4;i++){frames[i]=Sprite.Create(texture,new Rect(i*32,0,problem=="rect"?31:32,32),new Vector2(.5f,problem=="pivot"?.125f:.03125f),32);frames[i].name=c.id+"_"+i.ToString("D2");}
            Assert.Throws<InvalidOperationException>(()=>AdamR02Art.ValidateSprites(entry,frames));
        }
        finally{foreach(var frame in frames)if(frame!=null)UnityEngine.Object.DestroyImmediate(frame);if(copy!=null)UnityEngine.Object.DestroyImmediate(copy);}
    }
    [Test] public void ReimportRetainsTwelveSpriteIdentitiesAndAllRegistration()
    {
        var art=new AdamR02Art();
        foreach(var entry in art.Index.assets)
        {
            var path="Assets/Adam/Resources/ArtR02/"+entry.clip.id+".png";var guid=AssetDatabase.AssetPathToGUID(path);var before=Identities(path);
            AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceUpdate);Assert.AreEqual(guid,AssetDatabase.AssetPathToGUID(path));CollectionAssert.AreEqual(before,Identities(path));
        }
        new AdamR02Art();
    }
    static string[] Identities(string path)=>AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().Select(sprite=>{AssetDatabase.TryGetGUIDAndLocalFileIdentifier(sprite,out string guid,out long local);return sprite.name+":"+guid+":"+local;}).OrderBy(x=>x,StringComparer.Ordinal).ToArray();
}
