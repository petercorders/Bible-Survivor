using System;
using System.IO;
using System.Linq;
using BibleSurvivor.Core;
using BibleSurvivor.Unity;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

public sealed class AdamFontTests
{
    [Serializable] public sealed class Metrics {public Card[] cards;}
    [Serializable] public sealed class Card {public string item,level,header,effect;}
    [Test] public void ActualRuntimeStringsMatchAll71ReviewedCardsAndFitTheirRealRoleBoxes()
    {
        var path=Path.GetFullPath(Path.Combine(Application.dataPath,"../../art/art-unity-font-01/export/text_metrics_v1.json"));
        var metrics=JsonUtility.FromJson<Metrics>(File.ReadAllText(path));Assert.AreEqual(71,metrics.cards.Length);
        using(var fonts=new AdamFonts())
        {
            var heading=new GUIStyle{font=fonts.Label,fontSize=24,padding=new RectOffset()};
            var body=new GUIStyle{font=fonts.Body,fontSize=20,padding=new RectOffset()};
            foreach(var expected in metrics.cards)
            {
                var current=int.Parse(expected.level.Split('-')[0]);
                var inventory=new EquipmentInventory(expected.item=="bible"&&current==0?"jawbone":"bible");
                for(var i=inventory.Weapons.ContainsKey(expected.item)?1:0;i<current;i++)Assert.IsTrue(inventory.TryChoose(expected.item,!AdamGame.Weapons.Contains(expected.item)));
                var actual=AdamChoiceText.Describe(expected.item,inventory).Card(0).Split('\n');
                Assert.AreEqual(expected.header,actual[0]);Assert.AreEqual(expected.effect,actual[1]);
                foreach(var width in new[]{660,1100})
                {
                    var card=AdamChoiceText.ChoiceCard(AdamChoiceText.ChoicePanel(width,width==660?440:720),0);
                    Assert.LessOrEqual(heading.CalcSize(new GUIContent(actual[0])).x,card.width-52,actual[0]);
                    Assert.LessOrEqual(body.CalcSize(new GUIContent(actual[1])).x,card.width-52,actual[1]);
                    Assert.LessOrEqual(heading.CalcSize(new GUIContent(actual[0])).y,32);
                    Assert.LessOrEqual(body.CalcSize(new GUIContent(actual[1])).y,26);
                    Assert.LessOrEqual(14+32+26+16,card.height);
                }
            }
        }
    }
    [Test] public void AllRolesUseOriginalOutlineRasterWithoutFallbackReferencesAndWithCompleteHangul()
    {
        using(var fonts=new AdamFonts())
        {
            var names=new[]{"Galmuri11-Bold","Galmuri11","Galmuri9","Galmuri7"};var sizes=new[]{24,24,20,16};
            var values=new[]{fonts.Title,fonts.Label,fonts.Body,fonts.Small};
            for(var i=0;i<values.Length;i++)
            {
                var name=names[i];var path="Assets/Adam/Resources/ArtFonts/"+name+".ttf";
                var source=Path.GetFullPath(Path.Combine(Application.dataPath,"../../art/art-unity-font-01/fonts/galmuri-outline/"+name+".ttf"));
                CollectionAssert.AreEqual(File.ReadAllBytes(source),File.ReadAllBytes(path));
                var importer=(TrueTypeFontImporter)AssetImporter.GetAtPath(path);
                Assert.AreEqual(FontTextureCase.Dynamic,importer.fontTextureCase);Assert.AreEqual(FontRenderingMode.HintedRaster,importer.fontRenderingMode);
                CollectionAssert.AreEqual(new[]{name=="Galmuri11-Bold"?"Galmuri11":name},importer.fontNames);
                Assert.IsEmpty(importer.fontReferences);
                Assert.IsTrue(importer.includeFontData);
                var font=values[i];Assert.IsTrue(font.dynamic);
                Assert.AreEqual(UnityEngine.TextCore.LowLevel.FontEngineError.Success,UnityEngine.TextCore.LowLevel.FontEngine.LoadFontFace(font,sizes[i]));
                for(var code=0xAC00;code<=0xD7A3;code++)
                {Assert.IsTrue(UnityEngine.TextCore.LowLevel.FontEngine.TryGetGlyphIndex((uint)code,out var glyph));Assert.Greater(glyph,0);}
                UnityEngine.TextCore.LowLevel.FontEngine.UnloadFontFace();
                for(var code=0xAC00;code<=0xD7A3;code++)Assert.IsTrue(font.HasCharacter((char)code),name+":"+code);
                foreach(var c in " !0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz°·–…→")Assert.IsTrue(font.HasCharacter(c));
                font.RequestCharactersInTexture("창세기 아담 레벨 상승 하나를 선택하세요 0 → 1/5",sizes[i],FontStyle.Normal);
                Assert.AreEqual(FilterMode.Point,font.material.mainTexture.filterMode);
                AssertBinaryRaster(font.material.mainTexture);
            }
        }
        Assert.IsTrue(File.Exists("Assets/Adam/Resources/ArtFonts/LICENSE.txt"));
    }
    [Test] public void FontReimportKeepsGuidAndLocalFileIdentity()
    {
        foreach(var name in new[]{"Galmuri11-Bold","Galmuri11","Galmuri9","Galmuri7"})
        {
            var path="Assets/Adam/Resources/ArtFonts/"+name+".ttf";
            AssetDatabase.TryGetGUIDAndLocalFileIdentifier(AssetDatabase.LoadAssetAtPath<Font>(path),out string beforeGuid,out long beforeId);
            AssetDatabase.ImportAsset(path,ImportAssetOptions.ForceUpdate);
            AssetDatabase.TryGetGUIDAndLocalFileIdentifier(AssetDatabase.LoadAssetAtPath<Font>(path),out string afterGuid,out long afterId);
            Assert.AreEqual(beforeGuid,afterGuid);Assert.AreEqual(beforeId,afterId);
        }
    }
    static void AssertBinaryRaster(Texture texture)
    {
        var saved=RenderTexture.active;var target=RenderTexture.GetTemporary(texture.width,texture.height,0,RenderTextureFormat.ARGB32);
        var cpu=new Texture2D(texture.width,texture.height,TextureFormat.RGBA32,false);
        try
        {
            Graphics.Blit(texture,target);RenderTexture.active=target;cpu.ReadPixels(new Rect(0,0,target.width,target.height),0,0);cpu.Apply();
            var pixels=cpu.GetPixels32();Assert.IsTrue(pixels.Any(p=>p.a==255),"Raster contains ink");
            Assert.IsFalse(pixels.Any(p=>p.a!=0&&p.a!=255),"Hinted Raster must not introduce AA alpha");
        }
        finally {RenderTexture.active=saved;RenderTexture.ReleaseTemporary(target);UnityEngine.Object.DestroyImmediate(cpu);}
    }
}
