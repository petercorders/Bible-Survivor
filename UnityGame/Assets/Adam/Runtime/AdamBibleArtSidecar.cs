using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using UnityEngine;

namespace BibleSurvivor.Unity
{
    // Two reviewed pixel overrides only. Art03 remains the authoritative playback/placement contract.
    public static class AdamBibleArtSidecar
    {
        public const string Root = "BibleReadability01/";
        public const string Book = "wpn_bible-book_spin_16", Glow = "fx_bible-glow_static_20";
        public const string ManifestSha = "ddc5a01d16a8b609ed4569452529072f48a6b02120123be25f9a26e62054b17b";
        public static bool Overrides(string id) => id == Book || id == Glow;
        public static string PngSha(string id)
        {
            switch(id)
            {
                case Book: return "6a985c8dec83532dcc041f63d9a1273abd790722814ef5e47c06c36ea63c632e";
                case Glow: return "d92d0e9d698a9e744c9d56a74a22b21bc2b5ac6e434f19872258b83138f764d5";
                default: throw new InvalidOperationException("Unreviewed Bible sidecar ID: " + id);
            }
        }
        public static string Hash(byte[] bytes)
        { using(var sha=SHA256.Create()) return BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-", "").ToLowerInvariant(); }
        internal static void Apply(Dictionary<string,AdamArtClip> clips, Dictionary<string,Sprite[]> sprites)
        {
            var manifest=Resources.Load<TextAsset>(Root+"manifest");
            if(manifest==null || Hash(manifest.bytes)!=ManifestSha) throw new InvalidOperationException("Missing/unreviewed Bible sidecar manifest");
            foreach(var id in new[]{Book,Glow})
            {
                var clip=clips[id];
                var frames=Resources.LoadAll<Sprite>(Root+id);
                Array.Sort(frames,(a,b)=>string.CompareOrdinal(a.name,b.name));
                if(frames.Length!=clip.frames)throw new InvalidOperationException("Missing Bible sidecar frames: "+id);
                for(var i=0;i<frames.Length;i++)
                {
                    var s=frames[i];var t=s.texture;
                    if(s.name!=id+"_"+i.ToString("D2") || s.rect!=new Rect(i*clip.width,0,clip.width,clip.height) ||
                        s.pivot!=new Vector2(clip.pivotX,clip.pivotY) || s.pixelsPerUnit!=32 || s.border!=Vector4.zero ||
                        t.width!=clip.width*clip.frames || t.height!=clip.height || t.filterMode!=FilterMode.Point ||
                        t.wrapMode!=TextureWrapMode.Clamp || t.mipmapCount!=1)
                        throw new InvalidOperationException("Invalid Bible sidecar import: "+id);
                }
                sprites[id]=frames;
            }
        }
    }
}
