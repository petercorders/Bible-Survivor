using System;
using System.Collections.Generic;
using UnityEngine;

namespace BibleSurvivor.Unity
{
    public sealed class AdamR04Art
    {
        public const string Root="ArtR04/";
        public const string ManifestSha="b47bd8861ea71534eb78d347bd106336b27fa795f698ede11af6c72bf9276e4e";
        public const string IndexSha="f64972eaac3d6e2d555a5cc9f65c4450812da2446a7624cc7b9ece3dd7e87c95";
        public const string D3Move="enm_d3-raider-steed_move_32",D3Windup="enm_d3-raider-steed_windup_32",D3Dash="enm_d3-raider-steed_dash_32";
        public readonly AdamArtIndex Index;
        readonly Dictionary<string,Sprite[]> sprites=new Dictionary<string,Sprite[]>();
        readonly Dictionary<string,AdamArtClip> clips=new Dictionary<string,AdamArtClip>();
        public static AdamArtIndex ReadIndex(string json)
        {
            if(json==null || AdamBibleArtSidecar.Hash(System.Text.Encoding.UTF8.GetBytes(json))!=IndexSha)throw new InvalidOperationException("Unreviewed R04 index");
            var index=JsonUtility.FromJson<AdamArtIndex>(json);
            if(index.schema!=1 || index.ppu!=32 || index.manifestSha256!=ManifestSha || index.assets.Length!=5)throw new InvalidOperationException("Unsupported R04 contract");
            return index;
        }
        public AdamR04Art()
        {
            Index=ReadIndex(Resources.Load<TextAsset>(Root+"index")?.text);
            var manifest=Resources.Load<TextAsset>(Root+"manifest");if(manifest==null || AdamBibleArtSidecar.Hash(manifest.bytes)!=ManifestSha)throw new InvalidOperationException("R04 manifest mismatch");
            foreach(var clip in Index.assets)
            {
                var frames=Resources.LoadAll<Sprite>(Root+clip.id);Array.Sort(frames,(a,b)=>string.CompareOrdinal(a.name,b.name));
                if(frames.Length!=clip.frames)throw new InvalidOperationException("Missing R04 frames: "+clip.id);
                for(var i=0;i<frames.Length;i++)
                {
                    var s=frames[i];var t=s.texture;
                    if(s.name!=clip.id+"_"+i.ToString("D2")||s.rect!=new Rect(i*clip.width,0,clip.width,clip.height)||s.pivot!=new Vector2(clip.pivotX,clip.pivotY)||s.pixelsPerUnit!=32||s.border!=Vector4.zero||t.filterMode!=FilterMode.Point||t.wrapMode!=TextureWrapMode.Clamp||t.mipmapCount!=1||t.width!=clip.width*clip.frames||t.height!=clip.height)throw new InvalidOperationException("Invalid R04 import: "+clip.id);
                }
                clips.Add(clip.id,clip);sprites.Add(clip.id,frames);
            }
        }
        public AdamArtClip Clip(string id)=>clips[id];
        public void Draw(string id,Vector2 anchor,int scale,double time=0,bool left=false,int frame=-1)
        {var c=Clip(id);AdamArt.Blit(sprites[id][frame<0?c.Frame(time):Mathf.Clamp(frame,0,c.frames-1)],AdamArt.PlacedRect(c,anchor,scale),left);}
        public static string EnemyClip(string id)=>id=="d1"?"enm_d1-wild-stalker_move_32":id=="d2"?"enm_d2-luring-lamp_move_32":id=="d3"?D3Move:throw new InvalidOperationException("Unknown R04 actor "+id);
    }
}
