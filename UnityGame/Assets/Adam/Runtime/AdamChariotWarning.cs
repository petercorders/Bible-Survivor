using System;
using BibleSurvivor.Core;
using UnityEngine;

namespace BibleSurvivor.Unity
{
    // Presentation only. Normal Until is World.Time; boss Deadline is BossElapsed.
    // Geometry is passed to the existing outlined, nonblinking capsule/arrow renderer.
    public static class AdamChariotWarning
    {
        public static bool TryNormalGeometry(BattleSnapshot snapshot,EnemyChargeState charge,out AdamChargeGeometry geometry)=>
            AdamChargeLane.TryGeometry(snapshot,charge,out geometry);
        public static bool TryBossGeometry(ChariotDashView view,out AdamChargeGeometry geometry)
        {
            geometry=default;
            if(view==null || (view.Phase!="telegraph" && view.Phase!="dash"))return false;
            var start=new Vector2(view.Origin.X,view.Origin.Y);
            geometry=new AdamChargeGeometry(start,start+new Vector2(view.Direction.X,view.Direction.Y)*(float)view.RemainingDistance,(float)(view.BodyRadius+view.PlayerRadius));
            return true;
        }
        public static int WindupFrame(double elapsed,double duration,int frames)=>
            Mathf.Clamp((int)Math.Floor(Math.Max(0,elapsed)/duration*frames+1e-8),0,frames-1);
        public static string NormalClip(EnemyChargeState charge,double worldTime,out int frame,out double seconds)
        {
            frame=-1;seconds=worldTime;
            if(charge?.Phase=="telegraph")
            {seconds=Math.Max(0,BattleWorld.ChargeTelegraph-(charge.Until-worldTime));frame=WindupFrame(seconds,BattleWorld.ChargeTelegraph,2);return AdamExodusR03Art.Ex9Windup;}
            if(charge?.Phase=="charge")
            {seconds=Math.Max(0,BattleWorld.ChargeDuration-(charge.Until-worldTime));return AdamExodusR03Art.Ex9Dash;}
            return AdamExodusR03Art.Ex9Move;
        }
        public static string BossClip(string stage,ChariotDashView view,double worldTime,out int frame,out double seconds)
        {
            frame=-1;seconds=worldTime;
            if(stage=="boss_intro"){frame=0;return AdamExodusR03Art.BossMove;}
            if(view?.Phase=="telegraph")
            {var duration=BossDefinition.ForRound(12).TelegraphDuration;seconds=Math.Max(0,duration-(view.Deadline-view.BossElapsed));frame=WindupFrame(seconds,duration,3);return AdamExodusR03Art.BossWindup;}
            if(view?.Phase=="dash")
            {seconds=Math.Max(0,BossDefinition.ChariotDashDuration-(view.Deadline-view.BossElapsed));return AdamExodusR03Art.BossDash;}
            return AdamExodusR03Art.BossMove;
        }
    }
}
