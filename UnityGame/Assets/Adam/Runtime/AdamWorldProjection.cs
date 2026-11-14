using UnityEngine;

namespace BibleSurvivor.Unity
{
    // One integer raster grid for sprites and the transparent particle camera. Odd arena edges clip the last pixel.
    public readonly struct AdamWorldProjection
    {
        public readonly Rect Arena;
        public readonly Vector2 Origin;
        public readonly int Scale, TextureWidth, TextureHeight;
        public AdamWorldProjection(Rect arena,Vector2 origin)
        {
            Arena=arena;Origin=origin;Scale=AdamArtWorld.PixelScale(arena);
            TextureWidth=Mathf.Max(1,Mathf.CeilToInt(arena.width/Scale));
            TextureHeight=Mathf.Max(1,Mathf.CeilToInt(arena.height/Scale));
        }
        public Vector2 Point(float x,float y) => new Vector2(Mathf.Round(Arena.width/2+(x-Origin.x)*32*Scale),Mathf.Round(Arena.height/2-(y-Origin.y)*32*Scale));
        public Vector3 CameraPosition => new Vector3(Origin.x+(TextureWidth*Scale/2f-Mathf.Round(Arena.width/2))/(32*Scale),
            Origin.y+(Mathf.Round(Arena.height/2)-TextureHeight*Scale/2f)/(32*Scale),-10);
        public Rect TextureRect => new Rect(0,0,TextureWidth*Scale,TextureHeight*Scale);
    }
}
