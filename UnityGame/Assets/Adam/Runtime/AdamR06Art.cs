using System;
using System.Collections.Generic;
using UnityEngine;

namespace BibleSurvivor.Unity
{
    public sealed class AdamR06Art
    {
        public const string Root="ArtR06/";
        public const string ManifestSha="2823430a05bbd0e1fbd0fc48ed9431790dc2e701b65012d0ac37888f2178d283";
        public const string IndexSha="9609bec78b3a55da476f4c72704577b50276d010ba58a3bc72a5a393882c407d";
        public const string BossMove="bos_sitnah-keeper_move_64", BossWindup="bos_sitnah-keeper_windup_64", BossImpact="bos_sitnah-keeper_impact_64", BossDeath="bos_sitnah-keeper_death_64";
        public readonly AdamArtIndex Index;
        readonly Dictionary<string,Sprite[]> sprites=new Dictionary<string,Sprite[]>();
        readonly Dictionary<string,AdamArtClip> clips=new Dictionary<string,AdamArtClip>();
        public static AdamArtIndex ReadIndex(string json)
        {
            if(json==null || AdamBibleArtSidecar.Hash(System.Text.Encoding.UTF8.GetBytes(json))!=IndexSha)throw new InvalidOperationException("Unreviewed R06 index");
            var index=JsonUtility.FromJson<AdamArtIndex>(json);
            if(index.schema!=1 || index.ppu!=32 || index.manifestSha256!=ManifestSha || index.assets.Length!=7)throw new InvalidOperationException("Unsupported R06 contract");
            return index;
        }
        public AdamR06Art()
        {
            Index=ReadIndex(Resources.Load<TextAsset>(Root+"index")?.text);
            var manifest=Resources.Load<TextAsset>(Root+"manifest");if(manifest==null || AdamBibleArtSidecar.Hash(manifest.bytes)!=ManifestSha)throw new InvalidOperationException("R06 manifest mismatch");
            foreach(var clip in Index.assets)
            {
                var frames=Resources.LoadAll<Sprite>(Root+clip.id);Array.Sort(frames,(a,b)=>string.CompareOrdinal(a.name,b.name));
                if(frames.Length!=clip.frames)throw new InvalidOperationException("Missing R06 frames: "+clip.id);
                for(var i=0;i<frames.Length;i++)
                {
                    var s=frames[i];var t=s.texture;
                    if(s.name!=clip.id+"_"+i.ToString("D2")||s.rect!=new Rect(i*clip.width,0,clip.width,clip.height)||s.pivot!=new Vector2(clip.pivotX,clip.pivotY)||s.pixelsPerUnit!=32||s.border!=Vector4.zero||t.filterMode!=FilterMode.Point||t.wrapMode!=TextureWrapMode.Clamp||t.mipmapCount!=1||t.width!=clip.width*clip.frames||t.height!=clip.height)throw new InvalidOperationException("Invalid R06 import: "+clip.id);
                }
                clips.Add(clip.id,clip);sprites.Add(clip.id,frames);
            }
        }
        public AdamArtClip Clip(string id)=>clips[id];
        public void Draw(string id,Vector2 anchor,int scale,double time=0,bool left=false,int frame=-1)
        {var c=Clip(id);AdamArt.Blit(sprites[id][frame<0?c.Frame(time):Mathf.Clamp(frame,0,c.frames-1)],AdamArt.PlacedRect(c,anchor,scale),left);}
        public static string EnemyClip(string id)=>id=="f1"?"enm_f1-water-raider_move_32":id=="f2"?"enm_f2-well-filler_move_32":id=="f3"?"enm_f3-strife-armour_move_32":id=="sitnah_keeper"?BossMove:throw new InvalidOperationException("Unknown R06 actor "+id);
    }
}
