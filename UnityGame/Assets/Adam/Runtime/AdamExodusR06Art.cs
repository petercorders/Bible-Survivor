using System;
using System.Collections.Generic;
using UnityEngine;
using BibleSurvivor.Core;

namespace BibleSurvivor.Unity
{
    public sealed class AdamExodusR06Art : IDisposable
    {
        public const string Root="ArtExodusR06/";
        public const string ManifestSha="e765ce7bdf06e0ad717d88915ecec837e788294203b39ced04d5a78b3c8ff141";
        public const string IndexSha="b79ac6274897649ac085f6716cec5c85cc7c12551969efbb552772d7dfe30b9e";
        public readonly AdamArtIndex Index;
        readonly Dictionary<string,Sprite[]> sprites=new Dictionary<string,Sprite[]>();
        readonly Dictionary<string,AdamArtClip> clips=new Dictionary<string,AdamArtClip>();
        public static AdamArtIndex ReadIndex(string json)
        {
            if(json==null || AdamBibleArtSidecar.Hash(System.Text.Encoding.UTF8.GetBytes(json))!=IndexSha)throw new InvalidOperationException("Unreviewed Exodus R06 index");
            var index=JsonUtility.FromJson<AdamArtIndex>(json);
            if(index.schema!=1 || index.ppu!=32 || index.manifestSha256!=ManifestSha || index.assets.Length!=8)throw new InvalidOperationException("Unsupported Exodus R06 contract");
            return index;
        }
        public AdamExodusR06Art()
        {
            Index=ReadIndex(Resources.Load<TextAsset>(Root+"index")?.text);
            var manifest=Resources.Load<TextAsset>(Root+"manifest");if(manifest==null || AdamBibleArtSidecar.Hash(manifest.bytes)!=ManifestSha)throw new InvalidOperationException("Exodus R06 manifest mismatch");
            foreach(var clip in Index.assets)
            {
                var frames=Resources.LoadAll<Sprite>(Root+clip.id);Array.Sort(frames,(a,b)=>string.CompareOrdinal(a.name,b.name));
                if(frames.Length!=clip.frames)throw new InvalidOperationException("Missing Exodus R06 frames: "+clip.id);
                for(var i=0;i<frames.Length;i++)
                {
                    var s=frames[i];Texture2D t=s.texture;
                    if(s.name!=clip.id+"_"+i.ToString("D2")||s.rect!=new Rect(i*clip.width,0,clip.width,clip.height)||s.pivot!=new Vector2(clip.pivotX,clip.pivotY)||s.pixelsPerUnit!=32||s.border!=Vector4.zero||t.filterMode!=FilterMode.Point||t.wrapMode!=TextureWrapMode.Clamp||t.mipmapCount!=1||!t.isReadable||t.width!=clip.width*clip.frames||t.height!=clip.height)throw new InvalidOperationException("Invalid Exodus R06 import: "+clip.id);
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
            if(disposed)throw new ObjectDisposedException(nameof(AdamExodusR06Art));
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
            if(disposed)throw new ObjectDisposedException(nameof(AdamExodusR06Art));
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
        public const string BossMove="bos_defilement-weaver_move_64", BossWindup="bos_defilement-weaver_windup_64", BossAttack="bos_defilement-weaver_attack_64", BossDeath="bos_defilement-weaver_death_64", Projectile="prj_defilement-shard_spin_16";
        public const double DeathSeconds=4d/8,AttackSeconds=2d/12;
        public static bool Enemy(string id)=>id=="ex16"||id=="ex17"||id=="ex18"||id=="defilement_weaver";
        public static string EnemyClip(string id)=>id=="ex16"?"enm_ex16-greedy-hook_move_32":id=="ex17"?"enm_ex17-twisted-weave_move_32":id=="ex18"?"enm_ex18-ruin-shard_move_32":id=="defilement_weaver"?BossMove:throw new InvalidOperationException("Unknown Exodus R06 actor "+id);
        // Every pose derives from saved boss phase time, including a reload just after emission.
        public static string BossClip(string stage,BossEncounterState boss,double worldTime,out int frame,out double seconds)
        {
            frame=-1;seconds=worldTime;
            if(stage=="boss_intro"){frame=0;return BossMove;}
            if(boss==null || stage!="boss")return BossMove;
            if(boss.Telegraphing)
            {
                seconds=Math.Max(0,boss.BossElapsed-(boss.NextAttackAt-boss.Rules.TelegraphDuration));
                frame=Mathf.Clamp((int)Math.Floor(seconds/boss.Rules.TelegraphDuration*3+1e-8),0,2);return BossWindup;
            }
            var rules=boss.Rules;
            if(!rules.UsesDash&&!rules.UsesSlam&&!rules.UsesChainProjectiles)return BossMove;
            var emission=boss.NextAttackAt-rules.AttackPeriod;
            var elapsed=boss.BossElapsed-emission;
            if(emission>0 && elapsed>=-1e-8 && elapsed<AttackSeconds-1e-9)
            {seconds=Math.Max(0,elapsed);frame=Mathf.Clamp((int)Math.Floor(seconds*12+1e-8),0,1);return BossAttack;}
            return BossMove;
        }
    }
}
