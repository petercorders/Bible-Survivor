using System;
using BibleSurvivor.Core;
using UnityEngine;
namespace BibleSurvivor.Unity
{
 // Real impact facts only. Clock and contact position are Core-owned; no queued gameplay callbacks.
 public sealed class AdamPolishContacts
 {
  public const int Capacity=16;
  public const string Thud="fx_jawbone-thud_hit_24",Crack="fx_sling-crack_hit_16",Page="fx_bible-page-flare_hit_16";
  public sealed class Beat{public string Id;public Vector2 Position;public double At;public int Sector;public bool Balanced;}
  readonly Beat[] beats=new Beat[Capacity];long cursor;
  public AdamPolishContacts(){for(var i=0;i<beats.Length;i++)beats[i]=new Beat();}
  public void Clear(){cursor=0;foreach(var b in beats)b.Id=null;}
  public static string Id(string cause)=>cause=="jawbone"?Thud:cause=="sling"?Crack:cause=="bible"?Page:null;
  public static double Duration(string id)=>id==Thud?.25:id==Crack?.15:.2;
  public int Active(double clock,string id=null){var n=0;foreach(var b in beats)if(b.Id!=null&&(id==null||id==b.Id)&&clock>=b.At&&clock-b.At<Duration(b.Id))n++;return n;}
  public static bool TryRadialAxis(BattleImpact impact,BattleSnapshot snapshot,out Vector2 axis)
  {
   axis=Vector2.zero;if(snapshot==null||Math.Abs(snapshot.Time-impact.Time)>1e-8)return false;
   var v=new Vector2(impact.Position.X-snapshot.PlayerX,impact.Position.Y-snapshot.PlayerY);if(v.sqrMagnitude<1e-8f)return false;axis=v.normalized;return true;
  }
  public static int Sector(Vector2 axis)=>((int)Mathf.Floor((Mathf.Atan2(axis.y,axis.x)*Mathf.Rad2Deg+22.5f)/45f)%8+8)%8;
  public static int Frame(string id,double elapsed,int sector=0)=>Mathf.Min(2,Mathf.FloorToInt((float)Math.Max(0,elapsed)*(id==Thud?12:id==Crack?20:15)+1e-6f))+(id==Thud?sector%2*3:0);
  public static int QuarterTurns(int sector)=>sector/2;
  public bool Observe(BattleImpact impact,BattleSnapshot snapshot,bool feedback)
  {
   if(impact.Sequence<=cursor)return false;cursor=impact.Sequence;var id=Id(impact.Cause);if(!feedback||impact.TargetId<=0||id==null)return false;
   var clock=snapshot.Time;var cap=id==Page?2:3;if(Active(clock,id)>=cap)return false;
   foreach(var b in beats)if(b.Id==null||clock-b.At>=Duration(b.Id))
   {b.Id=id;b.At=impact.Time;b.Position=new Vector2(impact.Position.X,impact.Position.Y);b.Balanced=!TryRadialAxis(impact,snapshot,out var axis);b.Sector=b.Balanced?0:Sector(axis);return true;}return false;
  }
  public void Draw(AdamWorldProjection projection,double clock,bool feedback)
  {
   if(!feedback)return;foreach(var b in beats)if(b.Id!=null&&clock>=b.At&&clock-b.At<Duration(b.Id))
   {
    if(!AdamVisualQualityArt.TryGetClip(b.Id,out var clip))continue;var anchor=projection.Point(b.Position.x,b.Position.y);var frame=Frame(b.Id,clock-b.At,b.Sector);
    if(b.Id==Thud&&b.Balanced){DrawOne(projection,anchor,b.Id,frame,0,.5f);DrawOne(projection,anchor,b.Id,frame,2,.5f);}
    else DrawOne(projection,anchor,b.Id,frame,b.Id==Thud?QuarterTurns(b.Sector):0,1);
   }
  }
  static void DrawOne(AdamWorldProjection projection,Vector2 anchor,string id,int frame,int quarters,float alpha)
  {
   var matrix=GUI.matrix;var tint=GUI.color;var global=anchor+projection.Arena.position;
   try{GUI.matrix=matrix*Matrix4x4.Translate(global)*Matrix4x4.Rotate(Quaternion.Euler(0,0,-quarters*90))*Matrix4x4.Translate(-global);GUI.color=new Color(1,1,1,alpha);var c=AdamVisualQualityArt.GetClip(id);AdamArt.Blit(AdamVisualQualityArt.Sprite(id,frame),AdamArt.PlacedRect(c,anchor,projection.Scale));}
   finally{GUI.matrix=matrix;GUI.color=tint;}
  }
 }
}
