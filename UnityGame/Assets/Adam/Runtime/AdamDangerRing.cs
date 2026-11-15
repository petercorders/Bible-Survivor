using System;
using BibleSurvivor.Core;
using UnityEngine;

namespace BibleSurvivor.Unity
{
    // Gameplay information, drawn after all actors/effects and never gated by optional FX.
    public sealed class AdamDangerRing:IDisposable
    {
        Texture2D texture;int cachedSize;float cachedBand;
        public static Rect Bounds(AdamWorldProjection projection,Vector2 target,float radius)
        {var center=projection.Point(target.x,target.y);var pixels=radius*32*projection.Scale;return new Rect(center.x-pixels,center.y-pixels,2*pixels,2*pixels);}
        public void Draw(AdamWorldProjection projection,Vector2 target,float radius)
        {
            var rect=Bounds(projection,target,radius);var size=Mathf.CeilToInt(rect.width);var band=2*projection.Scale;
            if(texture==null||cachedSize!=size||cachedBand!=band)
            {
                Dispose();cachedSize=size;cachedBand=band;texture=new Texture2D(size,size,TextureFormat.RGBA32,false){filterMode=FilterMode.Point,wrapMode=TextureWrapMode.Clamp,hideFlags=HideFlags.HideAndDontSave};
                var pixels=new Color32[size*size];var r=rect.width/2;
                for(var y=0;y<size;y++)for(var x=0;x<size;x++)
                {var d=new Vector2((x+.5f)/size*2*r-r,(y+.5f)/size*2*r-r).magnitude;pixels[y*size+x]=d>r||d<r-2*band?new Color32(0,0,0,0):d>=r-band?new Color32(27,20,36,255):new Color32(255,255,255,255);}
                texture.SetPixels32(pixels);texture.Apply(false,false);
            }
            var color=GUI.color;GUI.color=Color.white;GUI.DrawTexture(rect,texture,ScaleMode.StretchToFill,true);GUI.color=color;
        }
        public void Dispose(){if(texture!=null){if(Application.isPlaying)UnityEngine.Object.Destroy(texture);else UnityEngine.Object.DestroyImmediate(texture);texture=null;}}
    }
}
