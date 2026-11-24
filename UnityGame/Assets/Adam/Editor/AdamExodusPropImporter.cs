using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace BibleSurvivor.Unity.Editor
{
    public sealed class AdamExodusPropImporter : AssetPostprocessor, IPreprocessBuildWithReport
    {
        const string Root = "Assets/Adam/Resources/" + AdamExodusPropArt.Root;

        public int callbackOrder => 0;

        void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith(Root, StringComparison.Ordinal) ||
                !assetPath.EndsWith(".png", StringComparison.Ordinal)) return;

            VerifyPng(assetPath);
            var importer = (TextureImporter)assetImporter;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 32;
            importer.sRGBTexture = true;
            importer.isReadable = true;
            importer.filterMode = FilterMode.Point;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false;
            importer.npotScale = TextureImporterNPOTScale.None;
            importer.alphaSource = TextureImporterAlphaSource.FromInput;
            importer.alphaIsTransparency = false;

            var settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteAlignment = (int)SpriteAlignment.Custom;
            settings.spritePivot = AdamExodusPropArt.NormalizedPivot;
            settings.spriteMeshType = SpriteMeshType.FullRect;
            settings.spriteExtrude = 0;
            settings.spriteGenerateFallbackPhysicsShape = false;
            importer.SetTextureSettings(settings);
        }

        public void OnPreprocessBuild(BuildReport report)
        {
            foreach (var path in Directory.GetFiles(Root, "*.png", SearchOption.AllDirectories)) VerifyPng(path.Replace('\\', '/'));
            foreach (var id in AdamExodusPropArt.AssetIds)
            {
                var path = Root + id + ".png";
                VerifyPng(path);
                if (!AdamExodusPropArt.Matches(AssetDatabase.LoadAssetAtPath<Sprite>(path)))
                    throw new BuildFailedException("Invalid Exodus arena prop import: " + path);
            }
        }

        static void VerifyPng(string path)
        {
            var id = Path.GetFileNameWithoutExtension(path);
            if (path != Root + id + ".png" || !File.Exists(path) ||
                AdamGenesisPropArt.Sha256(File.ReadAllBytes(path)) != AdamExodusPropArt.ExpectedHash(id))
                throw new BuildFailedException("Missing or unreviewed Exodus arena prop: " + path);
        }
    }
}
