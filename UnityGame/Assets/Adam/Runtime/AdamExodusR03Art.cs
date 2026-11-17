using System;
using System.Collections.Generic;
using UnityEngine;

namespace BibleSurvivor.Unity
{
    public sealed class AdamExodusR03Art : IDisposable
    {
        public const string Root="ArtExodusR03/";
        public const string ManifestSha="40b92f5efbdf1470a6847b9446a352fc45a3fa05d02da4a40fc1c26a90d27404";
        public const string IndexSha="7d314b2ab14dd41727990a931d70fa6aedf89d7a93030a9871b035b7f588bba3";
        public readonly AdamArtIndex Index;
        readonly Dictionary<string,Sprite[]> sprites=new Dictionary<string,Sprite[]>();
        readonly Dictionary<string,AdamArtClip> clips=new Dictionary<string,AdamArtClip>();
        public static AdamArtIndex ReadIndex(string json)
        {
            if(json==null || AdamBibleArtSidecar.Hash(System.Text.Encoding.UTF8.GetBytes(json))!=IndexSha)throw new InvalidOperationException("Unreviewed Exodus R03 index");
            var index=JsonUtility.FromJson<AdamArtIndex>(json);
            if(index.schema!=1 || index.ppu!=32 || index.manifestSha256!=ManifestSha || index.assets.Length!=9)throw new InvalidOperationException("Unsupported Exodus R03 contract");
            return index;
        }
        public AdamExodusR03Art()
        {
            Index=ReadIndex(Resources.Load<TextAsset>(Root+"index")?.text);
            var manifest=Resources.Load<TextAsset>(Root+"manifest");if(manifest==null || AdamBibleArtSidecar.Hash(manifest.bytes)!=ManifestSha)throw new InvalidOperationException("Exodus R03 manifest mismatch");
            foreach(var clip in Index.assets)
            {
                var frames=Resources.LoadAll<Sprite>(Root+clip.id);Array.Sort(frames,(a,b)=>string.CompareOrdinal(a.name,b.name));
                if(frames.Length!=clip.frames)throw new InvalidOperationException("Missing Exodus R03 frames: "+clip.id);
                for(var i=0;i<frames.Length;i++)
                {
                    var s=frames[i];Texture2D t=s.texture;
                    if(s.name!=clip.id+"_"+i.ToString("D2")||s.rect!=new Rect(i*clip.width,0,clip.width,clip.height)||s.pivot!=new Vector2(clip.pivotX,clip.pivotY)||s.pixelsPerUnit!=32||s.border!=Vector4.zero||t.filterMode!=FilterMode.Point||t.wrapMode!=TextureWrapMode.Clamp||t.mipmapCount!=1||!t.isReadable||t.width!=clip.width*clip.frames||t.height!=clip.height)throw new InvalidOperationException("Invalid Exodus R03 import: "+clip.id);
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
            if(disposed)throw new ObjectDisposedException(nameof(AdamExodusR03Art));
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
            if(disposed)throw new ObjectDisposedException(nameof(AdamExodusR03Art));
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
        public const string Ex9Move="enm_ex9-chariot-afterimage_move_32", Ex9Windup="enm_ex9-chariot-afterimage_windup_32", Ex9Dash="enm_ex9-chariot-afterimage_dash_32";
        public const string BossMove="bos_oppression-chariot_move_64", BossWindup="bos_oppression-chariot_windup_64", BossDash="bos_oppression-chariot_dash_64", BossDeath="bos_oppression-chariot_death_64";
        public static bool Enemy(string id)=>id=="ex7"||id=="ex8"||id=="ex9"||id=="oppression_chariot";
        public static string EnemyClip(string id)=>id=="ex7"?"enm_ex7-labor-chain_move_32":id=="ex8"?"enm_ex8-crown-mask_move_32":id=="ex9"?Ex9Move:id=="oppression_chariot"?BossMove:throw new InvalidOperationException("Unknown Exodus R03 actor "+id);
    }
}
