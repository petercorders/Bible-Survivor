using System;
using System.Collections.Generic;
using UnityEngine;

namespace BibleSurvivor.Unity
{
    public sealed class AdamR03Art
    {
        public const string Root="ArtR03/";
        public const string ManifestSha="b7eab4f4454f62bfda140a761dd30f557c64b410107964a5788c3ddd0270668e";
        public const string IndexSha="50ecdf61e301a4fba58dd868e6403a4de4d3246bdec49fa21c52339466474536";
        public const string BossMove="bos_nephilim-tyrant_move_64", BossWindup="bos_nephilim-tyrant_windup_64", BossImpact="bos_nephilim-tyrant_impact_64", BossDeath="bos_nephilim-tyrant_death_64";
        public readonly AdamArtIndex Index;
        readonly Dictionary<string,Sprite[]> sprites=new Dictionary<string,Sprite[]>();
        readonly Dictionary<string,AdamArtClip> clips=new Dictionary<string,AdamArtClip>();
        public static AdamArtIndex ReadIndex(string json)
        {
            if(json==null || AdamBibleArtSidecar.Hash(System.Text.Encoding.UTF8.GetBytes(json))!=IndexSha)throw new InvalidOperationException("Unreviewed R03 index");
            var index=JsonUtility.FromJson<AdamArtIndex>(json);
            if(index.schema!=1 || index.ppu!=32 || index.manifestSha256!=ManifestSha || index.assets.Length!=8)throw new InvalidOperationException("Unsupported R03 contract");
            return index;
        }
        public AdamR03Art()
        {
            Index=ReadIndex(Resources.Load<TextAsset>(Root+"index")?.text);
            var manifest=Resources.Load<TextAsset>(Root+"manifest");if(manifest==null || AdamBibleArtSidecar.Hash(manifest.bytes)!=ManifestSha)throw new InvalidOperationException("R03 manifest mismatch");
            foreach(var clip in Index.assets)
            {
                var frames=Resources.LoadAll<Sprite>(Root+clip.id);Array.Sort(frames,(a,b)=>string.CompareOrdinal(a.name,b.name));
                if(frames.Length!=clip.frames)throw new InvalidOperationException("Missing R03 frames: "+clip.id);
                for(var i=0;i<frames.Length;i++)
                {
                    var s=frames[i];var t=s.texture;
                    if(s.name!=clip.id+"_"+i.ToString("D2")||s.rect!=new Rect(i*clip.width,0,clip.width,clip.height)||s.pivot!=new Vector2(clip.pivotX,clip.pivotY)||s.pixelsPerUnit!=32||s.border!=Vector4.zero||t.filterMode!=FilterMode.Point||t.wrapMode!=TextureWrapMode.Clamp||t.mipmapCount!=1||t.width!=clip.width*clip.frames||t.height!=clip.height)throw new InvalidOperationException("Invalid R03 import: "+clip.id);
                }
                clips.Add(clip.id,clip);sprites.Add(clip.id,frames);
            }
        }
        public AdamArtClip Clip(string id)=>clips[id];
        public void Draw(string id,Vector2 anchor,int scale,double time=0,bool left=false,int frame=-1)
        {var c=Clip(id);AdamArt.Blit(sprites[id][frame<0?c.Frame(time):Mathf.Clamp(frame,0,c.frames-1)],AdamArt.PlacedRect(c,anchor,scale),left);}
        public static string EnemyClip(string id)=>id=="c1"?"enm_c1-violence-shadow_move_32":id=="c2"?"enm_c2-timber-devourer_move_32":id=="c3"?"enm_c3-wreckage-giant_move_32":id=="nephilim_tyrant"?BossMove:throw new InvalidOperationException("Unknown R03 actor "+id);
    }
}
