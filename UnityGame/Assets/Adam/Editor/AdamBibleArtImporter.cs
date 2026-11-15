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
    public sealed class AdamBibleArtImporter : AssetPostprocessor, IPreprocessBuildWithReport
    {
        public const string Root="Assets/Adam/Resources/BibleReadability01/";
        public int callbackOrder => 0;
        void OnPreprocessTexture()
        {
            if(!assetPath.StartsWith(Root,StringComparison.Ordinal) || !assetPath.EndsWith(".png",StringComparison.Ordinal))return;
            var id=Path.GetFileNameWithoutExtension(assetPath);
            if(AdamBibleArtSidecar.Hash(File.ReadAllBytes(assetPath))!=AdamBibleArtSidecar.PngSha(id))throw new InvalidOperationException("Bible sidecar PNG mismatch: "+id);
            var original=File.ReadAllText("Assets/Adam/Resources/Art03/index.json");
            if(!AdamArt.ReviewedIndex(original))throw new InvalidOperationException("Modified Art03 contract");
            var clip=JsonUtility.FromJson<AdamArtIndex>(original).assets.Single(c=>c.id==id);
            var texture=(TextureImporter)assetImporter;
            texture.textureType=TextureImporterType.Sprite;texture.spriteImportMode=SpriteImportMode.Multiple;
            texture.spritePixelsPerUnit=32;texture.filterMode=FilterMode.Point;texture.wrapMode=TextureWrapMode.Clamp;
            texture.textureCompression=TextureImporterCompression.Uncompressed;texture.mipmapEnabled=false;
            texture.npotScale=TextureImporterNPOTScale.None;texture.alphaSource=TextureImporterAlphaSource.FromInput;
            texture.alphaIsTransparency=false;texture.isReadable=true;texture.maxTextureSize=2048;
            var settings=new TextureImporterSettings();texture.ReadTextureSettings(settings);settings.spriteMeshType=SpriteMeshType.FullRect;settings.spriteExtrude=0;texture.SetTextureSettings(settings);
            var factory=new SpriteDataProviderFactories();factory.Init();var provider=factory.GetSpriteEditorDataProviderFromObject(texture);provider.InitSpriteEditorDataProvider();
            var rects=new SpriteRect[clip.frames];
            for(var i=0;i<rects.Length;i++)rects[i]=new SpriteRect{
                name=id+"_"+i.ToString("D2"),rect=new Rect(i*clip.width,0,clip.width,clip.height),
                pivot=new Vector2((float)clip.pivotX/clip.width,(float)clip.pivotY/clip.height),alignment=SpriteAlignment.Custom,border=Vector4.zero,
                spriteID=new GUID(AdamBibleArtSidecar.Hash(Encoding.UTF8.GetBytes("bible-readability-01:"+id+":"+i)).Substring(0,32))};
            provider.SetSpriteRects(rects);provider.GetDataProvider<ISpriteNameFileIdDataProvider>().SetNameFileIdPairs(rects.Select(s=>new SpriteNameFileIdPair(s.name,s.spriteID)));provider.Apply();
        }
        public void OnPreprocessBuild(BuildReport report) => ValidateBuild();
        public static void ValidateBuild()
        {
            var files=Directory.GetFiles(Root,"*.png");
            if(files.Length!=2)throw new InvalidOperationException("Bible sidecar must contain exactly two reviewed PNGs");
            foreach(var file in files)
                if(AdamBibleArtSidecar.Hash(File.ReadAllBytes(file))!=AdamBibleArtSidecar.PngSha(Path.GetFileNameWithoutExtension(file)))throw new InvalidOperationException("Bible sidecar build PNG mismatch");
            new AdamArt();
        }
    }
}
