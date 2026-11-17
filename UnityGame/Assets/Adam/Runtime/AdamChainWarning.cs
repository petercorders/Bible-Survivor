using BibleSurvivor.Core;
using UnityEngine;

namespace BibleSurvivor.Unity
{
    // Read-only gameplay information. Direction, distance and projectile lifetime belong to Core.
    public static class AdamChainWarning
    {
        public static bool TryGeometry(BattleSnapshot snapshot,BossEncounterState boss,string stage,int direction,out AdamChargeGeometry geometry)
        {
            geometry=default;
            if(snapshot==null || boss==null || boss.Rules.Number!=9 || stage!="boss" || !boss.Telegraphing ||
                boss.ChainDirections==null || boss.ChainDirections.Count!=3 || direction<0 || direction>=3)return false;
            foreach(var actor in snapshot.Enemies)if(actor.Id==boss.Rules.BossActorId)
            {
                var start=new Vector2(actor.X,actor.Y);var saved=boss.ChainDirections[direction];
                geometry=new AdamChargeGeometry(start,start+new Vector2(saved.X,saved.Y)*(float)(BossDefinition.ChainSpeed*BossDefinition.ChainLifetime),(float)BossDefinition.ChainRadius);
                return true;
            }
            return false;
        }
        public static bool ProjectileVisible(HostileProjectileState projectile,string stage)=>
            stage=="boss" && projectile!=null && projectile.Lifetime>0;
        public static Vector2 ProjectilePoint(AdamWorldProjection projection,HostileProjectileState projectile)=>projection.Point(projectile.X,projectile.Y);
        public static double ProjectileSeconds(BattleSnapshot snapshot,HostileProjectileState projectile)=>System.Math.Max(0,snapshot.Time-projectile.BornAt);
        public static Matrix4x4 ProjectileMatrix(Vector2 globalAnchor,HostileProjectileState projectile)=>
            Matrix4x4.Translate(globalAnchor)*Matrix4x4.Rotate(Quaternion.Euler(0,0,Mathf.Atan2(-projectile.DirectionY,projectile.DirectionX)*Mathf.Rad2Deg))*Matrix4x4.Translate(-globalAnchor);
        public static void Draw(AdamWorldProjection projection,AdamChargeGeometry geometry)
        {
            if(Event.current.type!=EventType.Repaint)return;
            var start=projection.Point(geometry.Start.x,geometry.Start.y);var end=projection.Point(geometry.End.x,geometry.End.y);
            var matrix=GUI.matrix;var color=GUI.color;var global=start+projection.Arena.position;
            try
            {
                GUI.matrix=matrix*Matrix4x4.Translate(global)*Matrix4x4.Rotate(Quaternion.Euler(0,0,Mathf.Atan2(end.y-start.y,end.x-start.x)*Mathf.Rad2Deg))*Matrix4x4.Translate(-global);
                var length=(end-start).magnitude;var unit=projection.Scale;
                GUI.color=new Color32(27,20,36,255);Bar(start.x,start.y-2*unit,length,4*unit);
                GUI.color=Color.white;Bar(start.x,start.y-unit,length,2*unit);
                // Repeated outlined arrowheads stay legible on both pale path and dark actors, without blinking.
                for(var i=1;i<=3;i++)
                {
                    var tip=start+Vector2.right*(length*i/4);var wing=5*unit;
                    Line(tip+new Vector2(-wing,-wing),tip,projection.Arena.position,3*unit,new Color32(27,20,36,255));
                    Line(tip+new Vector2(-wing,wing),tip,projection.Arena.position,3*unit,new Color32(27,20,36,255));
                    Line(tip+new Vector2(-wing,-wing),tip,projection.Arena.position,unit,Color.white);
                    Line(tip+new Vector2(-wing,wing),tip,projection.Arena.position,unit,Color.white);
                }
            }
            finally{GUI.matrix=matrix;GUI.color=color;}
        }
        static void Bar(float x,float y,float width,float height)=>GUI.DrawTexture(new Rect(x,y,width,height),Texture2D.whiteTexture);
        static void Line(Vector2 start,Vector2 end,Vector2 arena,float width,Color color)
        {
            var matrix=GUI.matrix;var global=start+arena;GUI.color=color;
            GUI.matrix=matrix*Matrix4x4.Translate(global)*Matrix4x4.Rotate(Quaternion.Euler(0,0,Mathf.Atan2(end.y-start.y,end.x-start.x)*Mathf.Rad2Deg))*Matrix4x4.Translate(-global);
            Bar(start.x,start.y-width/2,(end-start).magnitude,width);GUI.matrix=matrix;
        }
    }
}
