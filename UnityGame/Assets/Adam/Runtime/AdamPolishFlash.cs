using System;
using System.Collections.Generic;
using UnityEngine;
namespace BibleSurvivor.Unity
{
 // Bounded original R01 silhouettes; Resources textures are never mutated or destroyed.
 public sealed class AdamPolishFlash:IDisposable
 {
  readonly Dictionary<Texture2D,Texture2D> masks=new Dictionary<Texture2D,Texture2D>();
  public void Draw(AdamArt art,string id,Vector2 feet,int scale,double clock,bool left,float opacity)
  {
   if(opacity<=0)return;var c=art.Clip(id);var sprite=art.Sprite(id,c.Frame(clock));var source=sprite.texture;
   if(!masks.TryGetValue(source,out var mask))
   {
    var pixels=source.GetPixels32();for(var i=0;i<pixels.Length;i++)pixels[i]=pixels[i].a==0?new Color32(0,0,0,0):new Color32(255,255,255,pixels[i].a);
    mask=new Texture2D(source.width,source.height,TextureFormat.RGBA32,false){name=id+" polish white silhouette",filterMode=FilterMode.Point,wrapMode=TextureWrapMode.Clamp,hideFlags=HideFlags.HideAndDontSave};mask.SetPixels32(pixels);mask.Apply(false,false);masks.Add(source,mask);
   }
   var uv=new Rect(sprite.rect.x/mask.width,sprite.rect.y/mask.height,sprite.rect.width/mask.width,sprite.rect.height/mask.height);if(left){uv.x+=uv.width;uv.width=-uv.width;}
   var tint=GUI.color;try{GUI.color=new Color(1,1,1,opacity);GUI.DrawTextureWithTexCoords(AdamArt.PlacedRect(c,feet,scale),mask,uv,true);}finally{GUI.color=tint;}
  }
  public void Dispose(){foreach(var mask in masks.Values)if(mask!=null){if(Application.isPlaying)UnityEngine.Object.Destroy(mask);else UnityEngine.Object.DestroyImmediate(mask);}masks.Clear();}
 }
}
