using System;
using System.Collections.Generic;
using UnityEngine;

namespace BibleSurvivor.Unity
{
    // Reviewed standalone club and icon; Art03's immutable registration is preserved.
    public static class AdamJawboneArt
    {
        public const string Root="JawboneFeedback01/",Held="wpn_jawbone-club_held_32",Icon="ico_wpn_jawbone-club_static_16";
        public const string ManifestSha="87333c69aa50fcb13aa1bddfdf0fd679be8ad70d10c29566015fed6a5b329ce0";
        public static string PngSha(string id)=>id==Held?"51f4364ebe5c9b25fc6c8aba192acfa55fb4554958c01b5318851494e1304a6e":id==Icon?"847297b86a862e4279dd77434a2ef806342fc87cb54ffd3196d926a454720245":throw new InvalidOperationException("Unknown jawbone sidecar");
        public static Vector2 Pivot(string id)=>id==Held?new Vector2(7.5f,11.5f):id==Icon?new Vector2(8,8):throw new InvalidOperationException("Unknown jawbone sidecar");
        public static int Size(string id)=>id==Held?32:id==Icon?16:throw new InvalidOperationException("Unknown jawbone sidecar");
        public static Sprite Load(Dictionary<string,Sprite[]> sprites)
        {
            var manifest=Resources.Load<TextAsset>(Root+"manifest_jawbone_feedback_01");
            if(manifest==null || AdamBibleArtSidecar.Hash(manifest.bytes)!=ManifestSha)throw new InvalidOperationException("Unreviewed jawbone manifest");
            var held=Read(Held);sprites["ico_wpn_jawbone_static_16"]=new[]{Read(Icon)};return held;
        }
        static Sprite Read(string id)
        {
            var frames=Resources.LoadAll<Sprite>(Root+id);if(frames.Length!=1)throw new InvalidOperationException("Missing jawbone sidecar "+id);
            var sprite=frames[0];Texture2D texture=sprite.texture;var size=Size(id);
            if(sprite.name!=id+"_00" || sprite.rect!=new Rect(0,0,size,size) || sprite.pivot!=Pivot(id) || sprite.pixelsPerUnit!=32 ||
                texture.width!=size || texture.height!=size || texture.filterMode!=FilterMode.Point || texture.wrapMode!=TextureWrapMode.Clamp || texture.mipmapCount!=1)
                throw new InvalidOperationException("Invalid jawbone grip/import "+id);
            return sprite;
        }
    }
}
