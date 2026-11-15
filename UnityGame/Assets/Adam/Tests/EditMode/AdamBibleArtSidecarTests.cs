using System;
using System.Linq;
using BibleSurvivor.Unity;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public sealed class AdamBibleArtSidecarTests
{
    [Test] public void LoaderOverridesOnlyTheTwoReviewedIdsAndKeepsOriginalPlaybackAndQuality()
    {
        var art=new AdamArt();
        Assert.AreEqual(79,art.Index.assets.Length);
        foreach(var clip in art.Index.assets)
        {
            var root=AdamBibleArtSidecar.Overrides(clip.id)?AdamBibleArtSidecar.Root:"Art03/";
            var imported=Resources.LoadAll<Sprite>(root+clip.id).OrderBy(s=>s.name,StringComparer.Ordinal).ToArray();
            Assert.AreSame(imported[0],art.Sprite(clip.id,0),clip.id);
        }
        var book=art.Clip(AdamBibleArtSidecar.Book);var glow=art.Clip(AdamBibleArtSidecar.Glow);
        Assert.AreEqual(2,book.frames);Assert.AreEqual(6,book.fps);Assert.IsTrue(book.loop);Assert.AreEqual("core",book.quality);
        Assert.AreEqual(1,glow.frames);Assert.AreEqual(0,glow.fps);Assert.IsFalse(glow.loop);Assert.AreEqual("optional-fx",glow.quality);
        Assert.AreEqual(new Vector2(8,8),art.Sprite(book.id,1).pivot);Assert.AreEqual(new Vector2(10,10),art.Sprite(glow.id,0).pivot);
        Assert.IsFalse(AdamBibleArtSidecar.Overrides("wpn_sling-stone_spin_8"));
        Assert.Throws<InvalidOperationException>(()=>AdamBibleArtSidecar.PngSha("not-reviewed"));
    }
    [Test] public void ImportedSidecarUsesReviewedBytesAndFullRectPointAlphaSettings()
    {
        var art=new AdamArt();
        foreach(var id in new[]{AdamBibleArtSidecar.Book,AdamBibleArtSidecar.Glow})
        {
            var sprite=art.Sprite(id,0);var path=AssetDatabase.GetAssetPath(sprite);
            Assert.AreEqual("Assets/Adam/Resources/"+AdamBibleArtSidecar.Root+id+".png",path);
            Assert.AreEqual(AdamBibleArtSidecar.PngSha(id),AdamBibleArtSidecar.Hash(System.IO.File.ReadAllBytes(path)));
            var importer=(TextureImporter)AssetImporter.GetAtPath(path);
            Assert.AreEqual(FilterMode.Point,importer.filterMode);Assert.AreEqual(TextureWrapMode.Clamp,importer.wrapMode);
            Assert.IsFalse(importer.mipmapEnabled);Assert.IsFalse(importer.alphaIsTransparency);
            Assert.AreEqual(TextureImporterCompression.Uncompressed,importer.textureCompression);
            var settings=new TextureImporterSettings();importer.ReadTextureSettings(settings);Assert.AreEqual(SpriteMeshType.FullRect,settings.spriteMeshType);
        }
    }
}
