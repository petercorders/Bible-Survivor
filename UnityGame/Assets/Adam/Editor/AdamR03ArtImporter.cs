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
    public sealed class AdamR03ArtImporter : AssetPostprocessor, IPreprocessBuildWithReport
    {
        public const string Root="Assets/Adam/Resources/ArtR03/";
        public int callbackOrder => 0;
        void OnPreprocessTexture()
        {
            if(!assetPath.StartsWith(Root,StringComparison.Ordinal) || !assetPath.EndsWith(".png",StringComparison.Ordinal))return;
            var id=Path.GetFileNameWithoutExtension(assetPath);
            if(AdamBibleArtSidecar.Hash(File.ReadAllBytes(assetPath))!=Clip(id).sha256)throw new InvalidOperationException("Bible sidecar PNG mismatch: "+id);
            var clip=Clip(id);
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
                spriteID=new GUID(AdamBibleArtSidecar.Hash(Encoding.UTF8.GetBytes("R03:"+id+":"+i)).Substring(0,32))};
            provider.SetSpriteRects(rects);provider.GetDataProvider<ISpriteNameFileIdDataProvider>().SetNameFileIdPairs(rects.Select(s=>new SpriteNameFileIdPair(s.name,s.spriteID)));provider.Apply();
        }
        public void OnPreprocessBuild(BuildReport report) => ValidateBuild();
        static AdamArtClip Clip(string id)=>AdamR03Art.ReadIndex(File.ReadAllText(Root+"index.json")).assets.Single(x=>x.id==id);
        public static void ValidateBuild()
        {
            var index=AdamR03Art.ReadIndex(File.ReadAllText(Root+"index.json"));
            if(Directory.GetFiles(Root,"*.png").Length!=8)throw new InvalidOperationException("R03 requires eight actor/shadow sheets");
            foreach(var c in index.assets)if(AdamBibleArtSidecar.Hash(File.ReadAllBytes(Root+c.id+".png"))!=c.sha256)throw new InvalidOperationException("R03 PNG mismatch: "+c.id);
            new AdamR03Art();
        }
    }
}
