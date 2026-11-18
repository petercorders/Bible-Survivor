using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.U2D.Sprites;
using UnityEngine;

namespace BibleSurvivor.Unity.Editor
{
    public sealed class AdamJawboneArtImporter:AssetPostprocessor,IPreprocessBuildWithReport
    {
        public const string Root="Assets/Adam/Resources/JawboneFeedback01/";
        public int callbackOrder=>0;
        void OnPreprocessTexture()
        {
            if(!assetPath.StartsWith(Root,StringComparison.Ordinal)||!assetPath.EndsWith(".png",StringComparison.Ordinal))return;
            var id=Path.GetFileNameWithoutExtension(assetPath);if(AdamBibleArtSidecar.Hash(File.ReadAllBytes(assetPath))!=AdamJawboneArt.PngSha(id))throw new InvalidOperationException("Jawbone PNG mismatch");
            var texture=(TextureImporter)assetImporter;texture.textureType=TextureImporterType.Sprite;texture.textureShape=TextureImporterShape.Texture2D;texture.spriteImportMode=SpriteImportMode.Multiple;
            texture.spritePixelsPerUnit=32;texture.filterMode=FilterMode.Point;texture.wrapMode=TextureWrapMode.Clamp;texture.textureCompression=TextureImporterCompression.Uncompressed;
            texture.mipmapEnabled=false;texture.npotScale=TextureImporterNPOTScale.None;texture.alphaSource=TextureImporterAlphaSource.FromInput;texture.alphaIsTransparency=false;texture.isReadable=true;
            var settings=new TextureImporterSettings();texture.ReadTextureSettings(settings);settings.spriteMeshType=SpriteMeshType.FullRect;settings.spriteExtrude=0;texture.SetTextureSettings(settings);
            var factory=new SpriteDataProviderFactories();factory.Init();var provider=factory.GetSpriteEditorDataProviderFromObject(texture);provider.InitSpriteEditorDataProvider();var size=AdamJawboneArt.Size(id);
            var rect=new SpriteRect{name=id+"_00",rect=new Rect(0,0,size,size),pivot=AdamJawboneArt.Pivot(id)/size,alignment=SpriteAlignment.Custom,border=Vector4.zero,spriteID=new GUID(AdamBibleArtSidecar.Hash(System.Text.Encoding.UTF8.GetBytes("jawbone-feedback01:"+id)).Substring(0,32))};
            provider.SetSpriteRects(new[]{rect});provider.GetDataProvider<ISpriteNameFileIdDataProvider>().SetNameFileIdPairs(new[]{new SpriteNameFileIdPair(rect.name,rect.spriteID)});provider.Apply();
        }
        public static void Reimport(){foreach(var id in new[]{AdamJawboneArt.Held,AdamJawboneArt.Icon})AssetDatabase.ImportAsset(Root+id+".png",ImportAssetOptions.ForceUpdate);ValidateBuild();}
        public void OnPreprocessBuild(BuildReport report)=>ValidateBuild();
        public static void ValidateBuild()
        {
            if(Directory.GetFiles(Root,"*.png").Length!=2)throw new InvalidOperationException("Jawbone sidecar requires two sheets");
            foreach(var id in new[]{AdamJawboneArt.Held,AdamJawboneArt.Icon})if(AdamBibleArtSidecar.Hash(File.ReadAllBytes(Root+id+".png"))!=AdamJawboneArt.PngSha(id))throw new InvalidOperationException("Jawbone sidecar mismatch");
            new AdamArt();
        }
    }
}
