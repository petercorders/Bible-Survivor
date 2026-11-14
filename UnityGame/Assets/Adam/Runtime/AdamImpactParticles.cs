using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace BibleSurvivor.Unity
{
    // Built-in ParticleSystem, original tiny presets. Consumes only the view's already-drained transient impact batch.
    public sealed class AdamImpactParticles : IDisposable
    {
        public const int EmitterCapacity=16, ParticlesPerEmitter=16, Capacity=256, OverlayLayer=30;
        readonly GameObject root;
        readonly Camera camera;
        readonly Material material;
        readonly ParticleSystem[] slots=new ParticleSystem[EmitterCapacity];
        readonly ParticleSystemRenderer[] renderers=new ParticleSystemRenderer[EmitterCapacity];
        readonly int[] priorities=new int[EmitterCapacity];
        readonly Dictionary<Camera,bool> masked=new Dictionary<Camera,bool>();
        RenderTexture texture;
        int next,generation=-1;
        long cursor;
        bool disposed;
        public bool Enabled=true;
        public int ParticleCount {get;private set;}
        public long EmittedImpacts {get;private set;}
        public int Dropped {get;private set;}
        public Camera OverlayCamera => camera;
        public RenderTexture Texture => texture;
        public AdamWorldProjection Projection {get;private set;}
        public AdamImpactParticles()
        {
            var shader=Resources.Load<Shader>("AdamImpactParticles");
            if(shader==null)throw new InvalidOperationException("Missing original particle shader");
            material=new Material(shader){name="Adam owned impact material"};
            root=new GameObject("Adam owned impact particles");
            var eye=new GameObject("Adam transparent impact camera");eye.transform.SetParent(root.transform,false);camera=eye.AddComponent<Camera>();
            camera.enabled=false;camera.clearFlags=CameraClearFlags.SolidColor;camera.backgroundColor=Color.clear;
            camera.cullingMask=1<<OverlayLayer;camera.orthographic=true;camera.allowHDR=false;camera.allowMSAA=false;camera.depth=-20;
            for(var i=0;i<slots.Length;i++)
            {
                var owner=new GameObject("pooled burst "+i);owner.layer=OverlayLayer;owner.transform.SetParent(root.transform,false);
                var ps=owner.AddComponent<ParticleSystem>();slots[i]=ps;ps.Stop(false,ParticleSystemStopBehavior.StopEmittingAndClear);
                var main=ps.main;main.loop=false;main.playOnAwake=false;main.duration=.3f;main.maxParticles=ParticlesPerEmitter;
                main.simulationSpace=ParticleSystemSimulationSpace.World;main.startSpeed=0;main.startLifetime=.15f;main.startSize=.05f;main.gravityModifier=0;
                var emission=ps.emission;emission.enabled=false;var shape=ps.shape;shape.enabled=false;
                var color=ps.colorOverLifetime;color.enabled=true;var gradient=new Gradient();
                gradient.SetKeys(new[]{new GradientColorKey(Color.white,0),new GradientColorKey(Color.white,1)},new[]{new GradientAlphaKey(1,0),new GradientAlphaKey(0,1)});color.color=gradient;
                ps.useAutoRandomSeed=false;ps.randomSeed=(uint)(i+1);
                var renderer=owner.GetComponent<ParticleSystemRenderer>();renderers[i]=renderer;renderer.sharedMaterial=material;renderer.renderMode=ParticleSystemRenderMode.Billboard;
            }
            foreach(var other in UnityEngine.Object.FindObjectsByType<Camera>(FindObjectsSortMode.None))Mask(other);
            RenderPipelineManager.beginCameraRendering+=BeforeCamera;
        }
        void Mask(Camera other)
        {
            if(other==null || other==camera)return;
            if(!masked.ContainsKey(other))masked.Add(other,(other.cullingMask&(1<<OverlayLayer))!=0);
            other.cullingMask&=~(1<<OverlayLayer);
        }
        void BeforeCamera(ScriptableRenderContext context,Camera other)=>Mask(other);
        void Consume(AdamArtWorld view){foreach(var impact in view.FrameImpacts)cursor=Math.Max(cursor,impact.Sequence);}
        public void Update(AdamHost host,AdamArtWorld view,float delta)
        {
            if(disposed)return;
            if(generation!=view.Generation){Clear();cursor=0;generation=view.Generation;}
            if(!Enabled || !view.FeedbackEnabled || host?.Session==null || !host.Session.IsActive){Clear();Consume(view);return;}
            if(!host.CanPlay){Consume(view);return;} // Freeze existing particles during pause/choice/background, discard late batches.
            ParticleCount=0;
            foreach(var slot in slots){slot.Simulate(Mathf.Clamp(delta,0,.1f),false,false,false);slot.Pause(false);ParticleCount+=slot.particleCount;}
            foreach(var impact in view.FrameImpacts)
            {
                if(impact.Sequence<=cursor)continue;cursor=impact.Sequence;
                if(impact.TargetId==0 || (impact.Cause!="bible" && impact.Cause!="jawbone" && impact.Cause!="sling"))continue;
                var priority=impact.Killed?2:1;var found=-1;
                for(var i=0;i<slots.Length;i++){var candidate=(next+i)%slots.Length;if(slots[candidate].particleCount==0){found=candidate;break;}}
                if(found<0 && priority==2)for(var i=0;i<slots.Length;i++){var candidate=(next+i)%slots.Length;if(priorities[candidate]<priority){found=candidate;break;}}
                if(found<0){Dropped++;continue;}next=(found+1)%slots.Length;var ps=slots[found];
                ParticleCount-=ps.particleCount;ps.Stop(false,ParticleSystemStopBehavior.StopEmittingAndClear);priorities[found]=priority;
                var count=impact.Killed?12:impact.Cause=="jawbone"?8:impact.Cause=="bible"?6:4;
                var color=impact.Killed?new Color32(247,220,133,255):impact.Cause=="bible"?new Color32(247,220,133,255):impact.Cause=="jawbone"?new Color32(182,138,101,255):new Color32(230,224,211,255);
                for(var i=0;i<count;i++)
                {
                    var angle=(impact.Sequence*.618+i/(double)count)*Math.PI*2;var speed=impact.Cause=="jawbone"?.9f:1.6f;
                    ps.Emit(new ParticleSystem.EmitParams{position=new Vector3(impact.Position.X,impact.Position.Y,0),velocity=new Vector3((float)Math.Cos(angle),(float)Math.Sin(angle),0)*speed,
                        startSize=impact.Cause=="jawbone"?.075f:.045f,startLifetime=impact.Killed?.22f:impact.Cause=="jawbone"?.2f:.13f,startColor=color},1);
                }
                // Manual paused simulation does not reliably refresh Unity's automatic world culling bound.
                renderers[found].bounds=new Bounds(new Vector3(impact.Position.X,impact.Position.Y,0),new Vector3(2,2,2));
                ps.Pause(false);ParticleCount+=count;EmittedImpacts++;
            }
        }
        // Called in Update using the same viewport as OnGUI, so the automatic URP camera renders before GUI composition.
        public void Prepare(AdamWorldProjection projection)
        {
            if(disposed)return;Projection=projection;
            if(texture==null || texture.width!=projection.TextureWidth || texture.height!=projection.TextureHeight)
            {
                camera.enabled=false;camera.targetTexture=null;
                if(texture!=null){texture.Release();UnityEngine.Object.Destroy(texture);}
                texture=new RenderTexture(projection.TextureWidth,projection.TextureHeight,16,RenderTextureFormat.ARGB32){name="Adam Point impact RT",filterMode=FilterMode.Point,antiAliasing=1,wrapMode=TextureWrapMode.Clamp};
                texture.Create();camera.targetTexture=texture;
            }
            camera.transform.position=projection.CameraPosition;camera.orthographicSize=projection.TextureHeight/64f;
            camera.aspect=projection.TextureWidth/(float)projection.TextureHeight;camera.enabled=ParticleCount>0 && Enabled;
        }
        public void Draw(AdamWorldProjection projection)
        {
            if(disposed || texture==null || ParticleCount==0 || !Enabled)return;
            // A resize/inset can arrive after Update. Skip one mismatched frame instead of stretching/stale-position overlay.
            if(projection.Arena!=Projection.Arena || projection.Origin!=Projection.Origin)return;
            GUI.DrawTexture(projection.TextureRect,texture,ScaleMode.StretchToFill,true);
        }
        public void Clear()
        {
            if(disposed)return;
            if(camera!=null)camera.enabled=false;
            foreach(var slot in slots)if(slot!=null)slot.Stop(false,ParticleSystemStopBehavior.StopEmittingAndClear);
            ParticleCount=0;
        }
        public void Dispose()
        {
            if(disposed)return;Clear();disposed=true;RenderPipelineManager.beginCameraRendering-=BeforeCamera;
            foreach(var entry in masked)if(entry.Key!=null && entry.Value)entry.Key.cullingMask|=1<<OverlayLayer;
            if(camera!=null){camera.enabled=false;camera.targetTexture=null;}
            if(texture!=null){texture.Release();UnityEngine.Object.Destroy(texture);}
            UnityEngine.Object.Destroy(material);UnityEngine.Object.Destroy(root);
        }
    }
}
