using System;
using BibleSurvivor.Core;
using UnityEngine;

namespace BibleSurvivor.Unity
{
    // Transient presentation only. No gameplay timer, RNG, damage or saved state.
    public sealed class AdamJawboneSwing
    {
        public const double Duration=.2;
        public double Started {get;private set;}=-1;
        public Vector2 Direction {get;private set;}=Vector2.right;
        private float sampledArc;
        public void Clear(){Started=-1;Direction=Vector2.right;sampledArc=0;}
        public void Observe(BattleSnapshot snapshot,int amount,double interval)
        {
            foreach(var weapon in snapshot.Weapons)if(weapon.WeaponId=="jawbone")
            {
                var at=AdamArtWorld.LatestJawboneBurst(weapon,snapshot.Time,
                    weapon.JawboneCycleAmount > 0 ? weapon.JawboneCycleAmount : amount,interval);
                if(at>=0 && (Started<0 || at>Started+1e-9))
                {
                    Started=at;
                    sampledArc=(float)weapon.LastJawboneHalfAngle*2;
                    Direction=sampledArc>0 ? new Vector2(weapon.LastJawboneDirectionX,weapon.LastJawboneDirectionY) :
                        new Vector2(snapshot.FacingX,snapshot.FacingY);
                }
                break;
            }
        }
        public bool Active(double clock)=>Started>=0 && clock>=Started && clock-Started<Duration;
        public static Vector2 Hand(Vector2 feet,int scale,bool playerLeft)=>feet+new Vector2(playerLeft?-7:7,-12)*scale;
        public static float Angle(double seconds)
        {
            var t=Mathf.Clamp01((float)(seconds/Duration));
            if(t<.15f)return Mathf.Lerp(-55,-70,t/.15f);
            if(t<.48f)return Mathf.Lerp(-70,55,(t-.15f)/.33f);
            return Mathf.Lerp(55,-10,(t-.48f)/.52f);
        }
        public static Matrix4x4 Matrix(Rect arena,Vector2 hand,Vector2 direction,double seconds)
        {
            var global=hand+arena.position;
            var angle=Mathf.Atan2(-direction.y,direction.x)*Mathf.Rad2Deg+Angle(seconds);
            return Matrix4x4.Translate(global)*Matrix4x4.Rotate(Quaternion.Euler(0,0,angle))*Matrix4x4.Translate(-global);
        }
        public static Rect GripRect(Sprite sprite,Vector2 hand,int scale)=>new Rect(
            hand.x-sprite.pivot.x*scale,hand.y-(sprite.rect.height-sprite.pivot.y)*scale,sprite.rect.width*scale,sprite.rect.height*scale);
        public void Draw(AdamArt art,Rect arena,Vector2 feet,int scale,bool playerLeft,double clock,float range,bool feedback,
            float arcDegrees = 120)
        {
            if(!Active(clock))return;var elapsed=clock-Started;var hand=Hand(feet,scale,playerLeft);
            if (sampledArc>0) arcDegrees=sampledArc;
            var matrix=GUI.matrix;var color=GUI.color;
            try
            {
                // The arc projects the live sector without changing hit detection.
                if((feedback || arcDegrees != 120) && elapsed>=.03 && elapsed<.11)
                {
                    var aim=Mathf.Atan2(-Direction.y,Direction.x);var radius=range*32*scale;
                    var segment = arcDegrees / 8;
                    for(var i=0;i<8;i++)
                    {
                        var a=aim+(-arcDegrees/2+i*segment)*Mathf.Deg2Rad;var b=aim+(-arcDegrees/2+(i+1)*segment)*Mathf.Deg2Rad;
                        var from=feet+new Vector2(Mathf.Cos(a),Mathf.Sin(a))*radius;
                        var to=feet+new Vector2(Mathf.Cos(b),Mathf.Sin(b))*radius;
                        Line(arena,from,to,scale,new Color(1,.92f,.72f,.65f));
                    }
                }
                GUI.matrix=matrix*Matrix(arena,hand,Direction,elapsed);
                GUI.color=Color.white;var sprite=art.JawboneClub;
                AdamArt.Blit(sprite,GripRect(sprite,hand,scale));
                // The registered grip stays at the visible hand even as movement turns the actor.
                GUI.color=new Color32(214,164,117,255);GUI.DrawTexture(new Rect(hand.x-scale,hand.y-scale,2*scale,2*scale),Texture2D.whiteTexture);
            }
            finally{GUI.matrix=matrix;GUI.color=color;}
        }
        static void Line(Rect arena,Vector2 from,Vector2 to,float width,Color color)
        {
            var before=GUI.matrix;var global=from+arena.position;GUI.color=color;
            GUI.matrix=before*Matrix4x4.Translate(global)*Matrix4x4.Rotate(Quaternion.Euler(0,0,Mathf.Atan2(to.y-from.y,to.x-from.x)*Mathf.Rad2Deg))*Matrix4x4.Translate(-global);
            GUI.DrawTexture(new Rect(from.x,from.y-width/2,(to-from).magnitude,width),Texture2D.whiteTexture);GUI.matrix=before;
        }
    }
}
