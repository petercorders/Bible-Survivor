using System;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using UnityEngine;

namespace BibleSurvivor.Unity.Editor
{
    public sealed class AdamArtImporter : AssetPostprocessor
    {
        const string Root = "Assets/Adam/Resources/Art03/";
        void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith(Root, StringComparison.Ordinal) || !assetPath.EndsWith(".png", StringComparison.Ordinal)) return;
            var json=File.ReadAllText(Root+"index.json");
            if(!AdamArt.ReviewedIndex(json)) throw new InvalidOperationException("Modified art index");
            var index = JsonUtility.FromJson<AdamArtIndex>(json);
            if (index.manifestSha256 != AdamArt.ManifestSha || index.ppu != 32) throw new InvalidOperationException("Unreviewed art index");
            var clip = index.assets.Single(c => c.id == Path.GetFileNameWithoutExtension(assetPath));
            if (Hash(File.ReadAllBytes(assetPath)) != clip.sha256) throw new InvalidOperationException("Art PNG SHA mismatch: " + assetPath);
            var texture = (TextureImporter)assetImporter;
            texture.textureType = TextureImporterType.Sprite; texture.spriteImportMode = SpriteImportMode.Multiple;
            texture.spritePixelsPerUnit = 32; texture.filterMode = FilterMode.Point;
            texture.textureCompression = TextureImporterCompression.Uncompressed;
            texture.mipmapEnabled = false; texture.npotScale = TextureImporterNPOTScale.None;
            texture.wrapMode = TextureWrapMode.Clamp; texture.alphaSource = TextureImporterAlphaSource.FromInput;
            texture.alphaIsTransparency = false; texture.isReadable = true; texture.maxTextureSize = 2048;
            var settings = new TextureImporterSettings(); texture.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect; settings.spriteExtrude = 0; texture.SetTextureSettings(settings);
            var factory = new SpriteDataProviderFactories(); factory.Init();
            var provider = factory.GetSpriteEditorDataProviderFromObject(texture); provider.InitSpriteEditorDataProvider();
            var rects = new SpriteRect[clip.frames];
            for (var i=0; i<rects.Length; i++) rects[i] = new SpriteRect {
                name = clip.id + "_" + i.ToString("D2"), rect = new Rect(i*clip.width,0,clip.width,clip.height),
                pivot = new Vector2((float)clip.pivotX/clip.width, (float)clip.pivotY/clip.height), alignment = SpriteAlignment.Custom,
                border = new Vector4(clip.borders[0],clip.borders[1],clip.borders[2],clip.borders[3]),
                spriteID = new GUID(Hash(Encoding.UTF8.GetBytes(clip.id+":"+i)).Substring(0,32)) };
            provider.SetSpriteRects(rects);
            provider.GetDataProvider<ISpriteNameFileIdDataProvider>().SetNameFileIdPairs(rects.Select(r=>new SpriteNameFileIdPair(r.name,r.spriteID)));
            provider.Apply();
        }
        public static void ValidateBuild()
        {
            var art=new AdamArt(); // Includes actual imported frame/pivot/PPU/filter/border registration.
            foreach(var clip in art.Index.assets)
                if(Hash(File.ReadAllBytes(Root+clip.id+".png"))!=clip.sha256)throw new InvalidOperationException("Art build SHA mismatch: "+clip.id);
            foreach(var name in new[]{"Galmuri11-Bold","Galmuri11","Galmuri9","Galmuri7"})
                if(Hash(File.ReadAllBytes("Assets/Adam/Resources/ArtFonts/"+name+".ttf"))!=AdamFonts.HashFor(name))
                    throw new InvalidOperationException("Font build SHA mismatch: "+name);
            if(Hash(File.ReadAllBytes("Assets/Adam/Resources/ArtFonts/LICENSE.txt"))!="86a3ee9495f942f0243f18c103da9faca27adb88142613edb8bb852e56c892c1")
                throw new InvalidOperationException("Font build license mismatch");
        }
        public static string Hash(byte[] bytes)
        { using (var sha=SHA256.Create()) return BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-", "").ToLowerInvariant(); }
    }
}
