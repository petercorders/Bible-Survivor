using System;
using UnityEngine;

namespace BibleSurvivor.Unity
{
    public static class AdamRoundPresentation
    {
        public static string Name(int round)=>round==1?"아담":round==2?"가인":throw new InvalidOperationException("R03 is pending");
        public static string Title(AdamHost host)=>"창세기 / "+Name(host?.Session?.RoundNumber??1)+" · 3분 시제품";
        public static string ControlHint(int round)=>round==2?"WASD 이동 · Esc 정지 · 임시 B1 마름모 / B2 빗금 / B3 십자":"WASD / 방향키 이동 · 자동 공격 · Esc 정지 · Enter 재개";
        public static bool DevelopmentEnemy(string id)=>id=="b1"||id=="b2"||id=="b3";
        // Future reviewed R02 sidecar boundary. These IDs are NOT loaded or mapped to Art03 a-monsters.
        public static string R02ClipId(string id)=>id=="b1"?"enm_b1-envy-eye_move_32":id=="b2"?"enm_b2-rage-hound_run_32":id=="b3"?"enm_b3-cracked-mask_move_32":throw new InvalidOperationException("Unknown R02 monster "+id);
        public static string Shadow(string id)=>id=="a1"?"fx_shadow-snake_static_20x4":id=="a2"||id=="b1"?"fx_shadow-small_static_12x4":id=="a3"||id=="b2"||id=="b3"?"fx_shadow-wide_static_24x6":throw new InvalidOperationException("Unknown monster "+id);
        public static void DrawDevelopmentEnemy(string id,Vector2 feet,int scale)
        {
            if(!DevelopmentEnemy(id))throw new InvalidOperationException("No development marker for "+id);
            var tint=GUI.color;GUI.color=id=="b1"?new Color(1,.85f,.35f):id=="b2"?new Color(1,.4f,.25f):new Color(.55f,.8f,1);
            // Explicit geometric technical markers: diamond / striped low rectangle / wide cross; no generated PNG or Design claim.
            void Box(float x,float y,float w,float h)=>GUI.DrawTexture(new Rect(feet.x+x*scale,feet.y+y*scale,w*scale,h*scale),Texture2D.whiteTexture);
            if(id=="b1")for(var y=0;y<12;y++){var half=6-Math.Abs(y-6);Box(-half,-22+y,Math.Max(1,half*2),1);}
            else if(id=="b2"){Box(-11,-11,22,10);GUI.color=new Color(.12f,.06f,.08f);for(var x=-8;x<10;x+=6)Box(x,-10,2,8);}
            else{Box(-12,-13,24,4);Box(-2,-23,4,23);Box(-9,-21,18,4);}
            GUI.color=tint;
        }
    }
}
