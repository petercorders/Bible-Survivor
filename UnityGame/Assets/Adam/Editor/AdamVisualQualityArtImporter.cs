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
 public sealed class AdamVisualQualityArtImporter:AssetPostprocessor,IPreprocessBuildWithReport
 {
  public const string Root="Assets/Adam/Resources/VisualQuality01/",PolishRoot="Assets/Adam/Resources/VisualQuality02/";
  public const string FrontispieceRoot="Assets/Adam/Resources/VisualQuality03Frontispiece/";
  public const string BookRoot="Assets/Adam/Resources/VisualQuality03/";
  public const string Issue47Root = "Assets/Adam/Resources/Issue47Weapons/";
  public int callbackOrder=>0;
  static string Folder(string bundle)=>bundle=="Issue47"?Issue47Root:bundle=="Frontispiece03"?FrontispieceRoot:bundle=="Book03"?BookRoot:bundle=="Polish02"?PolishRoot:Root+bundle+"/";
  static string[] Installed(){var names=AdamVisualQualityArt.Bundles.ToList();if(File.Exists(PolishRoot+"manifest.json"))names.Add("Polish02");if(File.Exists(BookRoot+"manifest.json"))names.Add("Book03");if(File.Exists(FrontispieceRoot+"manifest.json"))names.Add("Frontispiece03");if(File.Exists(Issue47Root+"manifest.json"))names.Add("Issue47");return names.ToArray();}
  static AdamVisualManifest Read(string bundle)=>AdamVisualQualityArt.Parse(File.ReadAllBytes(Folder(bundle)+"manifest.json"),bundle);
  void OnPreprocessTexture()
  {
   var issue47=assetPath.StartsWith(Issue47Root,StringComparison.Ordinal);
   var frontispiece=assetPath.StartsWith(FrontispieceRoot,StringComparison.Ordinal);var book=assetPath.StartsWith(BookRoot,StringComparison.Ordinal);var polish=assetPath.StartsWith(PolishRoot,StringComparison.Ordinal);var sidecar=issue47||frontispiece||book||polish;
   if((!sidecar&&!assetPath.StartsWith(Root,StringComparison.Ordinal))||!assetPath.EndsWith(".png",StringComparison.Ordinal))return;
   var relative=assetPath.Substring(issue47?Issue47Root.Length:frontispiece?FrontispieceRoot.Length:book?BookRoot.Length:polish?PolishRoot.Length:Root.Length).Split('/');if(relative.Length!=(sidecar?1:2))throw new InvalidOperationException("Nested visual asset");var bundle=issue47?"Issue47":frontispiece?"Frontispiece03":book?"Book03":polish?"Polish02":relative[0];var id=Path.GetFileNameWithoutExtension(relative[relative.Length-1]);var asset=Read(bundle).assets.Single(a=>a.id==id);
   if(AdamBibleArtSidecar.Hash(File.ReadAllBytes(assetPath))!=asset.sha256)throw new InvalidOperationException("Unreviewed visual PNG "+id);
   var texture=(TextureImporter)assetImporter;texture.textureType=TextureImporterType.Sprite;texture.textureShape=TextureImporterShape.Texture2D;texture.spriteImportMode=SpriteImportMode.Multiple;texture.spritePixelsPerUnit=32;texture.filterMode=FilterMode.Point;texture.wrapMode=TextureWrapMode.Clamp;texture.mipmapEnabled=false;texture.textureCompression=TextureImporterCompression.Uncompressed;texture.npotScale=TextureImporterNPOTScale.None;texture.alphaSource=TextureImporterAlphaSource.FromInput;texture.alphaIsTransparency=false;texture.isReadable=true;
   var settings=new TextureImporterSettings();texture.ReadTextureSettings(settings);settings.spriteMeshType=SpriteMeshType.FullRect;settings.spriteExtrude=0;texture.SetTextureSettings(settings);
   var factory=new SpriteDataProviderFactories();factory.Init();var provider=factory.GetSpriteEditorDataProviderFromObject(texture);provider.InitSpriteEditorDataProvider();var frames=new SpriteRect[asset.frames];
   for(var i=0;i<frames.Length;i++)frames[i]=new SpriteRect{name=id+"_"+i.ToString("D2"),rect=new Rect(i*asset.cell[0],0,asset.cell[0],asset.cell[1]),pivot=new Vector2(asset.pivot[0]/(float)asset.cell[0],asset.pivot[1]/(float)asset.cell[1]),alignment=SpriteAlignment.Custom,border=new Vector4(asset.borders[0],asset.borders[1],asset.borders[2],asset.borders[3]),spriteID=new GUID(AdamBibleArtSidecar.Hash(Encoding.UTF8.GetBytes((issue47?"issue47:":frontispiece?"visual-quality03-frontispiece:":book?"visual-quality03:":polish?"visual-quality02:":"visual-quality01:")+id+":"+i)).Substring(0,32))};
   provider.SetSpriteRects(frames);provider.GetDataProvider<ISpriteNameFileIdDataProvider>().SetNameFileIdPairs(frames.Select(f=>new SpriteNameFileIdPair(f.name,f.spriteID)));provider.Apply();
  }
  public static void Reimport(){foreach(var bundle in Installed())foreach(var a in Read(bundle).assets)AssetDatabase.ImportAsset(Folder(bundle)+a.file,ImportAssetOptions.ForceUpdate);ValidateBuild();}
  public void OnPreprocessBuild(BuildReport report)=>ValidateBuild();
  public static void ValidateBuild()
  {
   foreach(var bundle in Installed()){var manifest=Read(bundle);if(Directory.GetFiles(Folder(bundle),"*.png").Length!=manifest.assets.Length)throw new InvalidOperationException("Unexpected visual PNG");foreach(var a in manifest.assets)if(AdamBibleArtSidecar.Hash(File.ReadAllBytes(Folder(bundle)+a.file))!=a.sha256)throw new InvalidOperationException("Visual PNG mismatch");}
   AdamVisualQualityArt.Reload();new AdamArt();
  }
 }
}
