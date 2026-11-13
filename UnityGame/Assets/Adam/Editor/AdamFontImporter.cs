using System;
using System.IO;
using UnityEditor;

namespace BibleSurvivor.Unity.Editor
{
    public sealed class AdamFontImporter : AssetPostprocessor
    {
        void OnPreprocessAsset()
        {
            if(!assetPath.StartsWith("Assets/Adam/Resources/ArtFonts/",StringComparison.Ordinal) || !assetPath.EndsWith(".ttf",StringComparison.Ordinal))return;
            var importer=(TrueTypeFontImporter)assetImporter;
            importer.fontTextureCase=FontTextureCase.Dynamic;
            importer.fontRenderingMode=FontRenderingMode.HintedRaster;
            importer.fontNames=Array.Empty<string>();
            importer.fontReferences=Array.Empty<UnityEngine.Font>();
            importer.includeFontData=true;
            var expected=AdamFonts.HashFor(Path.GetFileNameWithoutExtension(assetPath));
            if(AdamArtImporter.Hash(File.ReadAllBytes(assetPath))!=expected)throw new InvalidOperationException("Unreviewed outline font");
        }
    }
}
