using System;
using BibleSurvivor.Core;
using UnityEngine;

namespace BibleSurvivor.Unity
{
    // A swept contact capsule. Starts at the current actor, including any Core knockback.
    public readonly struct AdamChargeGeometry
    {
        public readonly Vector2 Start,End;
        public readonly float HalfWidth;
        public AdamChargeGeometry(Vector2 start,Vector2 end,float halfWidth){Start=start;End=end;HalfWidth=halfWidth;}
    }
    public sealed class AdamChargeLane:IDisposable
    {
        Texture2D caps;int cachedSize;float cachedBand;
        public static bool TryGeometry(BattleSnapshot snapshot,EnemyChargeState charge,out AdamChargeGeometry geometry)
        {
            geometry=default;
            if(charge==null || (charge.Phase!="telegraph"&&charge.Phase!="charge"))return false;
            foreach(var actor in snapshot.Enemies)if(actor.Id==charge.Id)
            {
                var start=new Vector2(actor.X,actor.Y);
                var seconds=charge.Phase=="telegraph"?BattleWorld.ChargeDuration:Math.Max(0,charge.Until-snapshot.Time);
                var end=start+new Vector2(charge.DirectionX,charge.DirectionY)*(float)(snapshot.Tuning.PlayerSpeed*BattleWorld.ChargeSpeed*seconds);
                geometry=new AdamChargeGeometry(start,end,(float)(snapshot.Tuning.PlayerRadius+snapshot.Tuning.EnemyRadius));return true;
            }
            return false;
        }
        public static string Clip(EnemyChargeState charge,double clock,out int frame,out double seconds)
        {
            frame=-1;seconds=clock;
            if(charge?.Phase=="telegraph")
            {frame=Mathf.Clamp((int)Math.Floor((BattleWorld.ChargeTelegraph-(charge.Until-clock))/(BattleWorld.ChargeTelegraph/2)+1e-8),0,1);return AdamR04Art.D3Windup;}
            if(charge?.Phase=="charge"){seconds=Math.Max(0,BattleWorld.ChargeDuration-(charge.Until-clock));return AdamR04Art.D3Dash;}
            return AdamR04Art.D3Move;
        }
        public void Draw(AdamWorldProjection projection,AdamChargeGeometry geometry)
        {
            if(Event.current.type!=EventType.Repaint)return;
            var start=projection.Point(geometry.Start.x,geometry.Start.y);var end=projection.Point(geometry.End.x,geometry.End.y);
            var length=(end-start).magnitude;var radius=geometry.HalfWidth*32*projection.Scale;var band=Mathf.Min(2*projection.Scale,radius/2);
            var size=Mathf.CeilToInt(2*radius);
            if(caps==null || cachedSize!=size || cachedBand!=band)
            {
                Dispose();cachedSize=size;cachedBand=band;caps=new Texture2D(size,size,TextureFormat.RGBA32,false){filterMode=FilterMode.Point,wrapMode=TextureWrapMode.Clamp,hideFlags=HideFlags.HideAndDontSave};
                var pixels=new Color32[size*size];
                for(var y=0;y<size;y++)for(var x=0;x<size;x++)
                {var d=new Vector2((x+.5f)/size*2*radius-radius,(y+.5f)/size*2*radius-radius).magnitude;pixels[y*size+x]=d>radius||d<radius-2*band?new Color32(0,0,0,0):d>=radius-band?new Color32(27,20,36,255):new Color32(255,255,255,255);}
                caps.SetPixels32(pixels);caps.Apply(false,false);
            }
            var matrix=GUI.matrix;var color=GUI.color;var global=start+projection.Arena.position;
            try
            {
                GUI.matrix=matrix*Matrix4x4.Translate(global)*Matrix4x4.Rotate(Quaternion.Euler(0,0,Mathf.Atan2(end.y-start.y,end.x-start.x)*Mathf.Rad2Deg))*Matrix4x4.Translate(-global);
                GUI.color=Color.white;
                GUI.DrawTextureWithTexCoords(new Rect(start.x-radius,start.y-radius,radius,2*radius),caps,new Rect(0,0,.5f,1),true);
                GUI.DrawTextureWithTexCoords(new Rect(start.x+length,start.y-radius,radius,2*radius),caps,new Rect(.5f,0,.5f,1),true);
                GUI.color=new Color32(27,20,36,255);Bar(start.x,start.y-radius,length,band);Bar(start.x,start.y+radius-band,length,band);
                GUI.color=Color.white;Bar(start.x,start.y-radius+band,length,band);Bar(start.x,start.y+radius-2*band,length,band);
                for(var i=1;i<=2;i++)
                {
                    var tip=start+new Vector2(length*i/3,0);var wing=Mathf.Min(5*projection.Scale,radius/2);
                    Line(tip+new Vector2(-wing,-wing),tip,projection.Arena.position,3*projection.Scale,new Color32(27,20,36,255));
                    Line(tip+new Vector2(-wing,wing),tip,projection.Arena.position,3*projection.Scale,new Color32(27,20,36,255));
                    Line(tip+new Vector2(-wing,-wing),tip,projection.Arena.position,projection.Scale,Color.white);
                    Line(tip+new Vector2(-wing,wing),tip,projection.Arena.position,projection.Scale,Color.white);
                }
            }
            finally{GUI.matrix=matrix;GUI.color=color;}
        }
        static void Bar(float x,float y,float width,float height)=>GUI.DrawTexture(new Rect(x,y,width,height),Texture2D.whiteTexture);
        static void Line(Vector2 start,Vector2 end,Vector2 arena,float width,Color color)
        {
            var before=GUI.matrix;var global=start+arena;GUI.color=color;
            GUI.matrix=before*Matrix4x4.Translate(global)*Matrix4x4.Rotate(Quaternion.Euler(0,0,Mathf.Atan2(end.y-start.y,end.x-start.x)*Mathf.Rad2Deg))*Matrix4x4.Translate(-global);
            Bar(start.x,start.y-width/2,(end-start).magnitude,width);GUI.matrix=before;
        }
        public void Dispose(){if(caps!=null){if(Application.isPlaying)UnityEngine.Object.Destroy(caps);else UnityEngine.Object.DestroyImmediate(caps);caps=null;}}
    }
}
