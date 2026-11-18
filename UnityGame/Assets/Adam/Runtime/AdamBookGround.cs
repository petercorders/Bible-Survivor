using UnityEngine;
namespace BibleSurvivor.Unity
{
 // Book skin only. The caller supplies the current Core round; no terrain/gameplay state is stored.
 public static class AdamBookGround
 {
  public const string Sand="til_exodus_sand-a_static_32",Hollow="dec_ground-hollow_static_64";
  public static bool Exodus(int round)=>round>=10&&round<=15&&AdamVisualQualityArt.TryGetClip(Sand,out _);
  public static string Ground(int x,int y,bool exodus)
  {
   var eden=AdamVisualQualityWorld.Ground(x,y);if(!exodus)return eden;
   return eden.Replace("til_eden_grass-","til_exodus_sand-").Replace("til_eden_meadow-","til_exodus_flag-");
  }
  public static string Path(bool exodus)=>exodus?"til_exodus_path-16mask_static_32":"til_eden_path-16mask_static_32";
  public static string Decor(int x,int y,bool exodus)=>(x+y)%2==0?(exodus?"dec_exodus_pebbles-1_11x8":"dec_eden_tuft-1_11x8"):(exodus?"dec_exodus_scrub-1_16x10":"dec_eden_flowers-1_16x10");
  public static uint Hash(int x,int y)=>unchecked((uint)(x*73856093^y*19349663));
  public static Vector2 HollowPosition(int x,int y)=>new Vector2(x*2+.5f+(Hash(x,y)&31)/32f,y*2+.5f+((Hash(x,y)>>5)&31)/32f);
  public static void Hollows(AdamArt art,AdamWorldProjection projection,Vector2 origin,float rx,float ry)
  {
   for(var y=Mathf.FloorToInt((origin.y-ry)/2)-1;y<=Mathf.CeilToInt((origin.y+ry)/2)+1;y++)
   for(var x=Mathf.FloorToInt((origin.x-rx)/2)-1;x<=Mathf.CeilToInt((origin.x+rx)/2)+1;x++)
   {
    var hash=Hash(x,y);if(hash%3!=0)continue;var p=HollowPosition(x,y);var anchor=projection.Point(p.x,p.y);var sprite=art.Sprite(Hollow,0);var uv=new Rect((hash&1024)!=0?1:0,(hash&2048)!=0?1:0,(hash&1024)!=0?-1:1,(hash&2048)!=0?-1:1);
    GUI.DrawTextureWithTexCoords(AdamArt.PlacedRect(art.Clip(Hollow),anchor,projection.Scale),sprite.texture,uv,true);
   }
  }
  public static Rect Edge(Rect arena,int step,int side)
  {
   var inset=step==0?0:step==1?4:10;var thickness=step==0?4:step==1?6:8;var w=arena.width-2*inset;var h=arena.height-2*inset;
   return side==0?new Rect(inset,inset,w,thickness):side==1?new Rect(inset,inset+h-thickness,w,thickness):side==2?new Rect(inset,inset+thickness,thickness,h-2*thickness):new Rect(inset+w-thickness,inset+thickness,thickness,h-2*thickness);
  }
  public static void Edges(Rect arena,bool feedback)
  {
   if(!feedback)return;var previous=GUI.color;try{for(var i=0;i<3;i++){var color=AdamHud.Ink0;color.a=i==0?.3f:i==1?.2f:.1f;GUI.color=color;for(var side=0;side<4;side++)GUI.DrawTexture(Edge(arena,i,side),Texture2D.whiteTexture);}}finally{GUI.color=previous;}
  }
 }
}
