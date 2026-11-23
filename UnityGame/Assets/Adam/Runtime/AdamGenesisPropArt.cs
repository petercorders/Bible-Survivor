using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace BibleSurvivor.Unity
{
    public static class AdamGenesisPropArt
    {
        public const string Root = "GenesisArenaProps/";
        public const string ShadowSuffix = "-shadow";
        public const string Seal = "genesis-env-eden-seal";
        public const string Ark = "genesis-env-ark-frame";
        public const string Cup = "genesis-env-hall-cup";
        const string ManifestSha = "b4f0490f1202b9dcfeb41175a80a96909ebb27568811a169f1e832950c7f9363";

        [Serializable]
        public sealed class Manifest
        {
            public int version;
            public string versionId, sourceSha256;
            public float shadowAlpha;
            public Slot[] assets;
        }

        [Serializable]
        public sealed class Slot
        {
            public string id, propSha256, shadowSha256;
            public int width, height, x, y;
            public float pivotX, pivotY;
            public Vector2 NormalizedPivot => new Vector2(pivotX / width, pivotY / height);
            public Rect Destination(Vector2 origin, int scale) =>
                new Rect(origin.x + x * scale, origin.y + y * scale, width * scale, height * scale);
        }

        internal static bool HasReviewedManifest(byte[] bytes) => bytes != null && Sha256(bytes) == ManifestSha;

        public static Manifest ReadManifest(byte[] bytes) => HasReviewedManifest(bytes)
            ? JsonUtility.FromJson<Manifest>(Encoding.UTF8.GetString(bytes)) : null;

        public static string Sha256(byte[] bytes)
        {
            using (var sha = SHA256.Create())
                return BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-", "").ToLowerInvariant();
        }

        public static bool MatchesSlot(Sprite sprite, Slot slot)
        {
            if (sprite == null || sprite.texture == null) return false;
            var texture = sprite.texture;
            return sprite.rect == new Rect(0, 0, slot.width, slot.height) &&
                (sprite.pivot - new Vector2(slot.pivotX, slot.pivotY)).sqrMagnitude < .0001f &&
                sprite.pixelsPerUnit == 32 && sprite.border == Vector4.zero &&
                texture.width == slot.width && texture.height == slot.height &&
                texture.filterMode == FilterMode.Point && texture.wrapMode == TextureWrapMode.Clamp &&
                texture.mipmapCount == 1;
        }

        internal static bool PairMatches(Slot slot, Sprite body, Sprite shadow) =>
            MatchesSlot(body, slot) && MatchesSlot(shadow, slot);

        readonly struct Prop
        {
            public readonly Slot Slot;
            public readonly Sprite Body, Shadow;
            public Prop(Slot slot, Sprite body, Sprite shadow)
            { Slot = slot; Body = body; Shadow = shadow; }
        }

        static Dictionary<string, Prop> props;
        static float shadowAlpha;

        static void Load()
        {
            props = new Dictionary<string, Prop>();
            var manifest = ReadManifest(Resources.Load<TextAsset>(Root + "manifest")?.bytes);
            if (manifest == null) return;
            shadowAlpha = manifest.shadowAlpha;
            foreach (var slot in manifest.assets)
            {
                var body = Resources.Load<Sprite>(Root + slot.id);
                var shadow = Resources.Load<Sprite>(Root + slot.id + ShadowSuffix);
                if (PairMatches(slot, body, shadow))
                    props.Add(slot.id, new Prop(slot, body, shadow));
            }
        }

        internal static bool TryDraw(string id, Vector2 origin, int scale)
        {
            if (props == null) Load();
            if (!props.TryGetValue(id, out var prop)) return false;
            var rect = prop.Slot.Destination(origin, scale);
            var prior = GUI.color;
            try
            {
                GUI.color = new Color(prior.r, prior.g, prior.b, prior.a * shadowAlpha);
                AdamArt.Blit(prop.Shadow, rect);
            }
            finally { GUI.color = prior; }
            AdamArt.Blit(prop.Body, rect);
            return true;
        }
    }
}
