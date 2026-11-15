using System;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEditor.U2D.Sprites;
using UnityEngine;
namespace BibleSurvivor.Unity.Editor
{
    public sealed class AdamR02ArtImporter:AssetPostprocessor
    {
        public const string Root="Assets/Adam/Resources/ArtR02/";
        void OnPreprocessTexture()
        {
            if(!assetPath.StartsWith(Root,StringComparison.Ordinal) || !assetPath.EndsWith(".png",StringComparison.OrdinalIgnoreCase))return;
            var index=AdamR02Art.ReadIndex(File.ReadAllText(Root+"index.json"));
            var clip=index.assets.Single(x=>x.clip.id==Path.GetFileNameWithoutExtension(assetPath)).clip;
            if(AdamR02Art.Hash(File.ReadAllBytes(assetPath))!=clip.sha256)throw new InvalidOperationException("R02 PNG SHA mismatch: "+assetPath);
            var texture=(TextureImporter)assetImporter;
            texture.textureType=TextureImporterType.Sprite;texture.spriteImportMode=SpriteImportMode.Multiple;texture.spritePixelsPerUnit=32;
            texture.sRGBTexture=true;texture.filterMode=FilterMode.Point;texture.textureCompression=TextureImporterCompression.Uncompressed;texture.mipmapEnabled=false;
            texture.npotScale=TextureImporterNPOTScale.None;texture.wrapMode=TextureWrapMode.Clamp;texture.alphaSource=TextureImporterAlphaSource.FromInput;
            texture.alphaIsTransparency=false;texture.isReadable=true;texture.maxTextureSize=2048;
            var settings=new TextureImporterSettings();texture.ReadTextureSettings(settings);settings.spriteMeshType=SpriteMeshType.FullRect;settings.spriteExtrude=0;texture.SetTextureSettings(settings);
            var factory=new SpriteDataProviderFactories();factory.Init();var provider=factory.GetSpriteEditorDataProviderFromObject(texture);provider.InitSpriteEditorDataProvider();
            var rects=new SpriteRect[4];
            for(var i=0;i<4;i++)rects[i]=new SpriteRect{name=clip.id+"_"+i.ToString("D2"),rect=new Rect(i*32,0,32,32),pivot=new Vector2(.5f,.03125f),alignment=SpriteAlignment.Custom,border=Vector4.zero,
                spriteID=new GUID(AdamR02Art.Hash(Encoding.UTF8.GetBytes("R02:"+clip.id+":"+i)).Substring(0,32))};
            provider.SetSpriteRects(rects);provider.GetDataProvider<ISpriteNameFileIdDataProvider>().SetNameFileIdPairs(rects.Select(x=>new SpriteNameFileIdPair(x.name,x.spriteID)));provider.Apply();
        }
        public static void ValidateBuild()
        {
            var art=new AdamR02Art();
            if(Directory.GetFiles(Root).Count(path=>Path.GetExtension(path).Equals(".png",StringComparison.OrdinalIgnoreCase))!=3)throw new InvalidOperationException("R02 requires exactly three PNG files");
            foreach(var entry in art.Index.assets)
            {
                var path=Root+entry.clip.id+".png";
                if(AdamR02Art.Hash(File.ReadAllBytes(path))!=entry.clip.sha256)throw new InvalidOperationException("R02 build PNG SHA mismatch");
                var importer=(TextureImporter)AssetImporter.GetAtPath(path);
                if(importer==null || !importer.sRGBTexture || importer.textureCompression!=TextureImporterCompression.Uncompressed || importer.mipmapEnabled || importer.alphaIsTransparency ||
                   importer.spriteImportMode!=SpriteImportMode.Multiple || importer.spritePixelsPerUnit!=32 || importer.filterMode!=FilterMode.Point)
                    throw new InvalidOperationException("Invalid R02 importer settings");
            }
        }
    }
    public sealed class AdamR02BuildValidation:IPreprocessBuildWithReport
    {
        public int callbackOrder=>0;
        public void OnPreprocessBuild(BuildReport report){AdamR02ArtImporter.ValidateBuild();}
    }
}
