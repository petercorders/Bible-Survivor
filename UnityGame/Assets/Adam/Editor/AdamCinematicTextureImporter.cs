using UnityEditor;
using UnityEngine;

namespace BibleSurvivor.Unity.Editor
{
    sealed class AdamCinematicTextureImporter : AssetPostprocessor
    {
        void OnPreprocessTexture()
        {
            const string root = "Assets/Adam/Resources/CuratedCinematics/";
            if (!assetPath.StartsWith(root, System.StringComparison.Ordinal) ||
                assetPath.IndexOf("-media/", root.Length, System.StringComparison.Ordinal) < 0) return;

            var importer = (TextureImporter)assetImporter;
            importer.textureType = TextureImporterType.Default;
            importer.alphaSource = TextureImporterAlphaSource.FromInput;
            importer.alphaIsTransparency = false;
            importer.sRGBTexture = true;
            importer.mipmapEnabled = false;
            importer.isReadable = false;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.maxTextureSize = 2048;
            importer.filterMode = FilterMode.Point;
            importer.wrapMode = TextureWrapMode.Clamp;
        }
    }
}
