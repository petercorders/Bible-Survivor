using System;
using System.Collections.Generic;
using BibleSurvivor.Core;
using UnityEngine;
namespace BibleSurvivor.Unity
{
 // Presentation history is transient, read-only and reset with the World generation.
 public sealed class AdamVisualQualityWorld
 {
  List<BattleProjectileState> currentShots=new List<BattleProjectileState>(),previousShots=new List<BattleProjectileState>();
  readonly HashSet<long> ids=new HashSet<long>();bool initialized;
  public double SlingReleasedAt {get;private set;}=-1;
  public Vector2 SlingDirection {get;private set;}=Vector2.right;
  public void Clear(){ids.Clear();currentShots.Clear();previousShots.Clear();initialized=false;SlingReleasedAt=-1;SlingDirection=Vector2.right;}
  public void Observe(BattleSnapshot snapshot)
  {
   foreach(var p in snapshot.Projectiles)if(initialized&&!ids.Contains(p.Id))
   {var velocity=new Vector2(p.VelocityX,p.VelocityY);if(velocity.sqrMagnitude>0){SlingReleasedAt=snapshot.Time;SlingDirection=velocity.normalized;}}
   previousShots=currentShots;currentShots=new List<BattleProjectileState>(snapshot.Projectiles);
   ids.Clear();foreach(var p in snapshot.Projectiles)ids.Add(p.Id);initialized=true;
  }
  public Vector2 ImpactDirection(Vector2 position,Vector2 fallback)
  {
   var nearest=float.MaxValue;var axis=fallback;
   foreach(var collection in new[]{previousShots,currentShots})foreach(var p in collection){var delta=(new Vector2(p.X,p.Y)-position).sqrMagnitude;var v=new Vector2(p.VelocityX,p.VelocityY);if(delta<nearest&&v.sqrMagnitude>0){nearest=delta;axis=v.normalized;}}return axis;
  }
  public bool SlingReleasing(double clock)=>SlingReleasedAt>=0&&clock>=SlingReleasedAt&&clock-SlingReleasedAt<.12;
  public static float TrailPixels(BattleProjectileState p,int scale)=>Mathf.Min(20*scale,new Vector2(p.VelocityX,p.VelocityY).magnitude*.035f*32*scale);
  public static string Ground(int x,int y)
  {
   var hash=(uint)(x*73856093^y*19349663);var patch=(uint)((x/4)*83492791^(y/4)*2971215073u)%11;
   return patch==0?"til_eden_meadow-"+(hash%2==0?"a":"b")+"_static_32":"til_eden_grass-"+(char)('a'+hash%4)+"_static_32";
  }
  public static Rect[] CanopyRects(Rect arena,int scale)
  {
   // Corner decals use at most the outer20% on either axis; the centre60% is always clear.
   var size=Mathf.Min(64*scale,Mathf.Min(arena.width,arena.height)*.2f);
   return new[]{new Rect(0,0,size,size),new Rect(arena.width-size,0,size,size),new Rect(0,arena.height-size,size,size),new Rect(arena.width-size,arena.height-size,size,size)};
  }
  public static Rect CanopyUv(int corner)=>new Rect(corner%2==1?1:0,corner>=2?1:0,corner%2==1?-1:1,corner>=2?-1:1);
  public static void Canopy(AdamArt art,Rect arena,int scale,bool feedback)
  {
   if(!feedback)return;var sprite=art.Sprite("dec_eden_canopy-shade_static_64",0);var color=GUI.color;var rects=CanopyRects(arena,scale);
   try{for(var i=0;i<rects.Length;i++)GUI.DrawTextureWithTexCoords(rects[i],sprite.texture,CanopyUv(i),true);}finally{GUI.color=color;}
  }
  public static void Line(Rect arena,Vector2 start,Vector2 end,float width,Color color)
  {
   var matrix=GUI.matrix;var tint=GUI.color;var global=start+arena.position;
   try{GUI.color=color;GUI.matrix=matrix*Matrix4x4.Translate(global)*Matrix4x4.Rotate(Quaternion.Euler(0,0,Mathf.Atan2(end.y-start.y,end.x-start.x)*Mathf.Rad2Deg))*Matrix4x4.Translate(-global);GUI.DrawTexture(new Rect(start.x,start.y-width/2,(end-start).magnitude,width),Texture2D.whiteTexture);}
   finally{GUI.matrix=matrix;GUI.color=tint;}
  }
  public static void WarningBand(AdamWorldProjection projection,AdamChargeGeometry geometry,bool feedback)
  {if(feedback)Line(projection.Arena,projection.Point(geometry.Start.x,geometry.Start.y),projection.Point(geometry.End.x,geometry.End.y),9*projection.Scale,new Color(224/255f,64/255f,80/255f,.4f));}
  public void SlingRelease(Rect arena,Vector2 feet,int scale,bool left,double clock,bool feedback)
  {
   if(!feedback||!SlingReleasing(clock))return;var hand=AdamJawboneSwing.Hand(feet,scale,left);var direction=new Vector2(SlingDirection.x,-SlingDirection.y);var side=new Vector2(-direction.y,direction.x);var t=(float)((clock-SlingReleasedAt)/.12);
   var pouch=hand-direction*(10*(1-t)*scale)+side*Mathf.Sin(t*Mathf.PI)*4*scale;
   Line(arena,hand+side*2*scale,pouch,scale,new Color32(220,204,164,255));Line(arena,pouch,hand-side*2*scale,scale,new Color32(180,124,74,255));
  }
  public static Vector2 SwingAttach(AdamArtClip clip,int frame)=>new Vector2(clip.attachX[frame]-clip.pivotX,clip.attachY[frame]-(clip.height-clip.pivotY));
  public static Matrix4x4 SwingFxMatrix(Rect arena,Vector2 hand,Vector2 direction,double elapsed,AdamArtClip clip,int frame,int scale)
  {
   var attach=SwingAttach(clip,frame);var desired=AdamJawboneSwing.Angle(elapsed);var baseline=Mathf.Atan2(attach.y,attach.x)*Mathf.Rad2Deg;
   var angle=Mathf.Atan2(-direction.y,direction.x)*Mathf.Rad2Deg+desired-baseline;
   var global=hand+arena.position;
   return Matrix4x4.Translate(global)*Matrix4x4.Rotate(Quaternion.Euler(0,0,angle))*Matrix4x4.Translate(-global);
  }
  public static void SwingFx(AdamArt art,AdamJawboneSwing swing,Rect arena,Vector2 feet,int scale,bool left,double clock,bool feedback)
  {
   if(!swing.Active(clock))return;var elapsed=clock-swing.Started;var hand=AdamJawboneSwing.Hand(feet,scale,left);var matrix=GUI.matrix;var tint=GUI.color;
   try
   {
    foreach(var id in new[]{"fx_jawbone-swing-trail_hit_64","fx_jawbone-swing-edge_hit_64"})
    {
     if(!feedback&&id.Contains("trail"))continue;var c=art.Clip(id);var frame=c.Frame(elapsed);var attach=SwingAttach(c,frame);
     // The old attach is now aligned to the physical head, never used as a detached club centre.
     var ratio=22f/attach.magnitude;var size=c.width*scale*ratio;var pivot=new Vector2(c.pivotX,c.height-c.pivotY)*scale*ratio;
     GUI.matrix=matrix*SwingFxMatrix(arena,hand,swing.Direction,elapsed,c,frame,scale);GUI.color=Color.white;
     AdamArt.Blit(art.Sprite(id,frame),new Rect(hand-pivot,new Vector2(size,size)));
    }
   }
   finally{GUI.matrix=matrix;GUI.color=tint;}
  }
 }
}
