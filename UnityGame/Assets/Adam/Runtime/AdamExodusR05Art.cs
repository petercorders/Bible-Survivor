using System;
using System.Collections.Generic;
using UnityEngine;

namespace BibleSurvivor.Unity
{
    public sealed class AdamExodusR05Art : IDisposable
    {
        public const string Root="ArtExodusR05/";
        public const string ManifestSha="f9b64d0e96bedc4b0ade476fc3cb93dde571714e2335cd59c4e0ebd07186c8fc";
        public const string IndexSha="a8e996c8264d3c5ea92fcaf5e71821d8fdd8840bdc3d761647c0d4584d1b489e";
        public readonly AdamArtIndex Index;
        readonly Dictionary<string,Sprite[]> sprites=new Dictionary<string,Sprite[]>();
        readonly Dictionary<string,AdamArtClip> clips=new Dictionary<string,AdamArtClip>();
        public static AdamArtIndex ReadIndex(string json)
        {
            if(json==null || AdamBibleArtSidecar.Hash(System.Text.Encoding.UTF8.GetBytes(json))!=IndexSha)throw new InvalidOperationException("Unreviewed Exodus R05 index");
            var index=JsonUtility.FromJson<AdamArtIndex>(json);
            if(index.schema!=1 || index.ppu!=32 || index.manifestSha256!=ManifestSha || index.assets.Length!=3)throw new InvalidOperationException("Unsupported Exodus R05 contract");
            return index;
        }
        public AdamExodusR05Art()
        {
            Index=ReadIndex(Resources.Load<TextAsset>(Root+"index")?.text);
            var manifest=Resources.Load<TextAsset>(Root+"manifest");if(manifest==null || AdamBibleArtSidecar.Hash(manifest.bytes)!=ManifestSha)throw new InvalidOperationException("Exodus R05 manifest mismatch");
            foreach(var clip in Index.assets)
            {
                var frames=Resources.LoadAll<Sprite>(Root+clip.id);Array.Sort(frames,(a,b)=>string.CompareOrdinal(a.name,b.name));
                if(frames.Length!=clip.frames)throw new InvalidOperationException("Missing Exodus R05 frames: "+clip.id);
                for(var i=0;i<frames.Length;i++)
                {
                    var s=frames[i];Texture2D t=s.texture;
                    if(s.name!=clip.id+"_"+i.ToString("D2")||s.rect!=new Rect(i*clip.width,0,clip.width,clip.height)||s.pivot!=new Vector2(clip.pivotX,clip.pivotY)||s.pixelsPerUnit!=32||s.border!=Vector4.zero||t.filterMode!=FilterMode.Point||t.wrapMode!=TextureWrapMode.Clamp||t.mipmapCount!=1||!t.isReadable||t.width!=clip.width*clip.frames||t.height!=clip.height)throw new InvalidOperationException("Invalid Exodus R05 import: "+clip.id);
                }
                clips.Add(clip.id,clip);sprites.Add(clip.id,frames);
            }
        }
        public AdamArtClip Clip(string id)=>clips[id];
        public void Draw(string id,Vector2 anchor,int scale,double time=0,bool left=false,int frame=-1)
        {var c=Clip(id);AdamArt.Blit(sprites[id][frame<0?c.Frame(time):Mathf.Clamp(frame,0,c.frames-1)],AdamArt.PlacedRect(c,anchor,scale),left);}
        readonly Dictionary<string,Texture2D> hitMasks=new Dictionary<string,Texture2D>();
        bool disposed;
        public Sprite Sprite(string id,int frame)=>sprites[Clip(id).id][Mathf.Clamp(frame,0,Clip(id).frames-1)];
        // Derived runtime data only: the reviewed source textures and their RGB/alpha bytes stay untouched.
        public void PrepareHitMasks()
        {
            if(disposed)throw new ObjectDisposedException(nameof(AdamExodusR05Art));
            foreach(var clip in Index.assets)
            {
                var id=clip.id;if(hitMasks.ContainsKey(id))continue;
                var source=sprites[id][0].texture;var pixels=source.GetPixels32();
                for(var i=0;i<pixels.Length;i++)pixels[i]=pixels[i].a==0?new Color32(0,0,0,0):new Color32(255,255,255,pixels[i].a);
                var mask=new Texture2D(source.width,source.height,TextureFormat.RGBA32,false){name=id+"_runtime-hit-mask",filterMode=FilterMode.Point,wrapMode=TextureWrapMode.Clamp,hideFlags=HideFlags.HideAndDontSave};
                mask.SetPixels32(pixels);mask.Apply(false,false);hitMasks.Add(id,mask);
            }
        }
        public Texture2D HitMask(string id)
        {
            if(disposed)throw new ObjectDisposedException(nameof(AdamExodusR05Art));
            Clip(id);return hitMasks[id];
        }
        public void DrawHit(string id,Vector2 feet,int scale,double time,bool left,float opacity,int frame=-1)
        {
            if(opacity<=0)return;
            var clip=Clip(id);var mask=HitMask(id);frame=frame<0?clip.Frame(time):Mathf.Clamp(frame,0,clip.frames-1);
            var uv=new Rect(frame/(float)clip.frames,0,1f/clip.frames,1);if(left){uv.x+=uv.width;uv.width=-uv.width;}
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
        public static bool Enemy(string id)=>id=="ex13"||id=="ex14"||id=="ex15";
        public static string EnemyClip(string id)=>id=="ex13"?"enm_ex13-ambush-shadow_move_32":id=="ex14"?"enm_ex14-discord-stone-armor_move_32":id=="ex15"?"enm_ex15-golden-mask_move_32":throw new InvalidOperationException("Unknown Exodus R05 actor "+id);
    }
}
