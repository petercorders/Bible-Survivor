using System;
using System.IO;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace BibleSurvivor.Unity.Editor
{
    public sealed class AdamGenesisPropImporter : AssetPostprocessor, IPreprocessBuildWithReport
    {
        const string Root = "Assets/Adam/Resources/" + AdamGenesisPropArt.Root;
        const string ShadowPng = AdamGenesisPropArt.ShadowSuffix + ".png";

        public int callbackOrder => 0;

        static AdamGenesisPropArt.Manifest ReviewedManifest()
        {
            var bytes = File.ReadAllBytes(Root + "manifest.json");
            return AdamGenesisPropArt.ReadManifest(bytes) ??
                throw new InvalidOperationException("Unreviewed Genesis arena prop manifest");
        }

        void OnPreprocessTexture()
        {
            if (!assetPath.StartsWith(Root, StringComparison.Ordinal) ||
                !assetPath.EndsWith(".png", StringComparison.Ordinal)) return;

            foreach (var slot in ReviewedManifest().assets)
            {
                var body = assetPath == Root + slot.id + ".png";
                var shadow = assetPath == Root + slot.id + ShadowPng;
                if (!body && !shadow) continue;
                VerifyPng(assetPath, body ? slot.propSha256 : slot.shadowSha256);
                Configure((TextureImporter)assetImporter, slot);
                return;
            }
            throw new InvalidOperationException("Unexpected Genesis arena prop texture: " + assetPath);
        }

        static void Configure(TextureImporter importer, AdamGenesisPropArt.Slot slot)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 32;
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
            settings.spritePivot = slot.NormalizedPivot;
            settings.spriteMeshType = SpriteMeshType.FullRect;
            settings.spriteExtrude = 0;
            settings.spriteGenerateFallbackPhysicsShape = false;
            importer.SetTextureSettings(settings);
        }

        static void VerifyPng(string path, string sha)
        {
            if (AdamGenesisPropArt.Sha256(File.ReadAllBytes(path)) != sha)
                throw new InvalidOperationException("Unreviewed Genesis arena prop PNG: " + path);
        }

        public void OnPreprocessBuild(BuildReport report)
        {
            var assets = ReviewedManifest().assets;
            if (Directory.GetFiles(Root, "*.png").Length != assets.Length * 2)
                throw new BuildFailedException("Genesis arena prop PNG inventory differs from the reviewed manifest");
            foreach (var slot in assets)
            {
                VerifyImport(slot, ".png", slot.propSha256);
                VerifyImport(slot, ShadowPng, slot.shadowSha256);
            }
        }

        static void VerifyImport(AdamGenesisPropArt.Slot slot, string suffix, string sha)
        {
            var path = Root + slot.id + suffix;
            VerifyPng(path, sha);
            if (!AdamGenesisPropArt.MatchesSlot(AssetDatabase.LoadAssetAtPath<Sprite>(path), slot))
                throw new BuildFailedException("Invalid Genesis arena prop import: " + path);
        }
    }
}
