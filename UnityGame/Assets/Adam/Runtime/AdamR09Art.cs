using System;
using System.Collections.Generic;
using UnityEngine;

namespace BibleSurvivor.Unity
{
    public sealed class AdamR09Art
    {
        public const string Root="ArtR09/";
        public const string ManifestSha="0a9d44d1f79dcba097d723e29b4a5377a3dc008eb9da610d9ddd50cbbde9160a";
        public const string IndexSha="aed55ca5836216dbf571b35d2f116053c624b78c150d7f479124b5108c5fac2e";
        public const string BossMove="bos_silver-cup-accuser_move_64", BossWindup="bos_silver-cup-accuser_windup_64", BossImpact="bos_silver-cup-accuser_attack_64", BossDeath="bos_silver-cup-accuser_death_64";
        public readonly AdamArtIndex Index;
        readonly Dictionary<string,Sprite[]> sprites=new Dictionary<string,Sprite[]>();
        readonly Dictionary<string,AdamArtClip> clips=new Dictionary<string,AdamArtClip>();
        public static AdamArtIndex ReadIndex(string json)
        {
            if(json==null || AdamBibleArtSidecar.Hash(System.Text.Encoding.UTF8.GetBytes(json))!=IndexSha)throw new InvalidOperationException("Unreviewed R09 index");
            var index=JsonUtility.FromJson<AdamArtIndex>(json);
            if(index.schema!=1 || index.ppu!=32 || index.manifestSha256!=ManifestSha || index.assets.Length!=8)throw new InvalidOperationException("Unsupported R09 contract");
            return index;
        }
        public AdamR09Art()
        {
            Index=ReadIndex(Resources.Load<TextAsset>(Root+"index")?.text);
            var manifest=Resources.Load<TextAsset>(Root+"manifest");if(manifest==null || AdamBibleArtSidecar.Hash(manifest.bytes)!=ManifestSha)throw new InvalidOperationException("R09 manifest mismatch");
            foreach(var clip in Index.assets)
            {
                var frames=Resources.LoadAll<Sprite>(Root+clip.id);Array.Sort(frames,(a,b)=>string.CompareOrdinal(a.name,b.name));
                if(frames.Length!=clip.frames)throw new InvalidOperationException("Missing R09 frames: "+clip.id);
                for(var i=0;i<frames.Length;i++)
                {
                    var s=frames[i];var t=s.texture;
                    if(s.name!=clip.id+"_"+i.ToString("D2")||s.rect!=new Rect(i*clip.width,0,clip.width,clip.height)||s.pivot!=new Vector2(clip.pivotX,clip.pivotY)||s.pixelsPerUnit!=32||s.border!=Vector4.zero||t.filterMode!=FilterMode.Point||t.wrapMode!=TextureWrapMode.Clamp||t.mipmapCount!=1||t.width!=clip.width*clip.frames||t.height!=clip.height)throw new InvalidOperationException("Invalid R09 import: "+clip.id);
                }
                clips.Add(clip.id,clip);sprites.Add(clip.id,frames);
            }
        }
        public AdamArtClip Clip(string id)=>clips[id];
        public void Draw(string id,Vector2 anchor,int scale,double time=0,bool left=false,int frame=-1)
        {var c=Clip(id);AdamArt.Blit(sprites[id][frame<0?c.Frame(time):Mathf.Clamp(frame,0,c.frames-1)],AdamArt.PlacedRect(c,anchor,scale),left);}
        public const string Projectile="prj_accuser-chain_fly_16";
        public static string EnemyClip(string id)=>id=="i1"?"enm_i1-silver-devourer_move_32":id=="i2"?"enm_i2-concealing-mask_move_32":id=="i3"?"enm_i3-brother-binder_move_32":id=="silver_cup_accuser"?BossMove:throw new InvalidOperationException("Unknown R09 actor "+id);
    }
}
