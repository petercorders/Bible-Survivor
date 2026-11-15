using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using UnityEngine;

namespace BibleSurvivor.Unity
{
    [Serializable] public sealed class AdamR02Index
    {
        public int schema,ppu;
        public string manifestSha256;
        public AdamR02Entry[] assets;
    }
    [Serializable] public sealed class AdamR02Entry
    {
        public string monster,shadow,pixelSha256;
        public bool ground;
        public AdamArtClip clip;
    }
    // Independent reviewed sidecar. Art03's 79-asset contract is never widened to admit R02.
    public sealed class AdamR02Art : IDisposable
    {
        public const string ManifestSha="e3a69af3c0734e5d37848b5780d020d09bbb054538e4173bf8aacafc0db1be89";
        public const string IndexSha="828f685f970523e769af09e4fd609db556e5d02f09e36bb3fba24f6f37dd55e5";
        static readonly string[] Hashes={"060c78299d9501e44b01130dd079d6c767381c730b283b767f003f3d49bbfd6b","c2fb37d65ead991ab6e0b5adbe139fd3e9556069096c44804c834b2c5eaa5fbd","32a6294b9c4e1c85718eaf60ad2dc28f95719f661b0e82413d560c5c5de359a7"};
        public readonly AdamR02Index Index;
        readonly Dictionary<string,AdamArtClip> clips=new Dictionary<string,AdamArtClip>();
        readonly Dictionary<string,Sprite[]> sprites=new Dictionary<string,Sprite[]>();
        readonly Dictionary<string,Texture2D> hitMasks=new Dictionary<string,Texture2D>();
        bool disposed;
        public static string Hash(byte[] value){using(var sha=SHA256.Create())return BitConverter.ToString(sha.ComputeHash(value)).Replace("-","").ToLowerInvariant();}
        public static AdamR02Index ReadIndex(string json)
        {
            if(json==null || Hash(Encoding.UTF8.GetBytes(json))!=IndexSha)throw new InvalidOperationException("R02 index differs from reviewed contract");
            var index=JsonUtility.FromJson<AdamR02Index>(json);ValidateContract(index);return index;
        }
        public static void ValidateContract(AdamR02Index index)
        {
            if(index==null || index.schema!=1 || index.ppu!=32 || index.manifestSha256!=ManifestSha || index.assets==null || index.assets.Length!=3)throw new InvalidOperationException("R02 requires exactly three reviewed sheets");
            var seen=new HashSet<string>();
            foreach(var entry in index.assets)
            {
                if(entry==null || entry.clip==null || !seen.Add(entry.monster??""))throw new InvalidOperationException("Missing/duplicate R02 registration");
                var i=entry.monster=="b1"?0:entry.monster=="b2"?1:entry.monster=="b3"?2:-1;
                if(i<0)throw new InvalidOperationException("Unknown R02 monster");var clip=entry.clip;
                if(clip.id!=AdamRoundPresentation.R02ClipId(entry.monster) || clip.source!="art/art-genesis-r02/export/png/"+clip.id+".png" || clip.sha256!=Hashes[i] ||
                   clip.width!=32 || clip.height!=32 || clip.frames!=4 || clip.pivotX!=16 || clip.pivotY!=1 || clip.fps!=(i==2?6:8) || !clip.loop ||
                   clip.sorting!="Actors" || clip.quality!="core" || clip.alpha!="binary" || clip.borders==null || clip.borders.Length!=4 || clip.borders.Any(x=>x!=0) ||
                   entry.shadow!=AdamRoundPresentation.Shadow(entry.monster) || entry.ground!=(i!=0) || entry.pixelSha256==null || entry.pixelSha256.Length!=64)
                    throw new InvalidOperationException("Unsupported R02 clip contract: "+clip.id);
            }
        }
        public static void ValidateSprites(AdamR02Entry entry,Sprite[] frames)
        {
            var clip=entry.clip;
            if(frames==null || frames.Length!=4)throw new InvalidOperationException("Missing/extra R02 frames: "+clip.id);
            var texture=frames[0]?.texture;
            if(texture==null || texture.width!=128 || texture.height!=32 || texture.filterMode!=FilterMode.Point || texture.wrapMode!=TextureWrapMode.Clamp || texture.mipmapCount!=1 || !texture.isReadable)
                throw new InvalidOperationException("Unsupported R02 texture: "+clip.id);
            for(var i=0;i<4;i++)
                if(frames[i]==null || frames[i].texture!=texture || frames[i].name!=clip.id+"_"+i.ToString("D2") || frames[i].rect!=new Rect(i*32,0,32,32) ||
                   frames[i].pivot!=new Vector2(16,1) || frames[i].pixelsPerUnit!=32 || frames[i].border!=Vector4.zero)
                    throw new InvalidOperationException("Invalid imported R02 registration: "+clip.id);
            var pixels=texture.GetPixels32();var data=new byte[pixels.Length*4];
            for(var i=0;i<pixels.Length;i++){data[i*4]=pixels[i].r;data[i*4+1]=pixels[i].g;data[i*4+2]=pixels[i].b;data[i*4+3]=pixels[i].a;}
            if(Hash(data)!=entry.pixelSha256)throw new InvalidOperationException("R02 imported pixel SHA mismatch: "+clip.id);
        }
        public AdamR02Art()
        {
            var json=Resources.Load<TextAsset>("ArtR02/index");Index=ReadIndex(json?.text);
            var manifest=Resources.Load<TextAsset>("ArtR02/manifest");
            if(manifest==null || Hash(manifest.bytes)!=ManifestSha)throw new InvalidOperationException("Missing/unreviewed R02 manifest");
            foreach(var entry in Index.assets)
            {
                var clip=entry.clip;var frames=Resources.LoadAll<Sprite>("ArtR02/"+clip.id);Array.Sort(frames,(a,b)=>string.CompareOrdinal(a.name,b.name));ValidateSprites(entry,frames);
                clips.Add(clip.id,clip);sprites.Add(clip.id,frames);
            }
        }
        public AdamArtClip Clip(string id)=>clips.TryGetValue(id,out var clip)?clip:throw new InvalidOperationException("No reviewed R02 clip: "+id);
        public Sprite Sprite(string id,int frame)=>sprites[Clip(id).id][Mathf.Clamp(frame,0,3)];
        // Derived runtime data only: the reviewed source textures and their RGB/alpha bytes stay untouched.
        public void PrepareHitMasks()
        {
            if(disposed)throw new ObjectDisposedException(nameof(AdamR02Art));
            foreach(var entry in Index.assets)
            {
                var id=entry.clip.id;if(hitMasks.ContainsKey(id))continue;
                var source=sprites[id][0].texture;var pixels=source.GetPixels32();
                for(var i=0;i<pixels.Length;i++)pixels[i]=pixels[i].a==0?new Color32(0,0,0,0):new Color32(255,255,255,pixels[i].a);
                var mask=new Texture2D(source.width,source.height,TextureFormat.RGBA32,false){name=id+"_runtime-hit-mask",filterMode=FilterMode.Point,wrapMode=TextureWrapMode.Clamp,hideFlags=HideFlags.HideAndDontSave};
                mask.SetPixels32(pixels);mask.Apply(false,false);hitMasks.Add(id,mask);
            }
        }
        public Texture2D HitMask(string id)
        {
            if(disposed)throw new ObjectDisposedException(nameof(AdamR02Art));
            Clip(id);return hitMasks[id];
        }
        public void DrawHit(string id,Vector2 feet,int scale,double time,bool left,float opacity)
        {
            if(opacity<=0)return;
            var clip=Clip(id);var mask=HitMask(id);var frame=clip.Frame(time);
            var uv=new Rect(frame/4f,0,1/4f,1);if(left){uv.x+=uv.width;uv.width=-uv.width;}
            var before=GUI.color;
            try{GUI.color=new Color(1,1,1,Mathf.Clamp01(opacity));GUI.DrawTextureWithTexCoords(AdamArt.PlacedRect(clip,feet,scale),mask,uv,true);}
            finally{GUI.color=before;}
        }
        public void Dispose()
        {
            if(disposed)return;disposed=true;
            foreach(var mask in hitMasks.Values)
                if(Application.isPlaying)UnityEngine.Object.Destroy(mask);else UnityEngine.Object.DestroyImmediate(mask);
            hitMasks.Clear();
        }
        public void Draw(string id,Vector2 feet,int scale,double time,bool left)
        {
            var clip=Clip(id);AdamArt.Blit(Sprite(id,clip.Frame(time)),AdamArt.PlacedRect(clip,feet,scale),left);
        }
    }
}
