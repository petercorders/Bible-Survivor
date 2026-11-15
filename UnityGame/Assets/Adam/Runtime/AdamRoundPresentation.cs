using System;
using UnityEngine;

namespace BibleSurvivor.Unity
{
    public static class AdamRoundPresentation
    {
        public static string Name(int round)=>round==1?"아담":round==2?"가인":throw new InvalidOperationException("R03 is pending");
        public static string Title(AdamHost host)=>"창세기 / "+Name(host?.Session?.RoundNumber??1)+" · 3분 시제품";
        public static string ControlHint(int round)=>"WASD / 방향키 이동 · 자동 공격 · Esc 정지 · Enter 재개";
        public static bool R02Enemy(string id)=>id=="b1"||id=="b2"||id=="b3";
        // Explicit reviewed R02 sidecar mapping; unknown IDs never fall back to Art03.
        public static string R02ClipId(string id)=>id=="b1"?"enm_b1-envy-eye_move_32":id=="b2"?"enm_b2-rage-hound_run_32":id=="b3"?"enm_b3-cracked-mask_move_32":throw new InvalidOperationException("Unknown R02 monster "+id);
        public static string Shadow(string id)=>id=="a1"?"fx_shadow-snake_static_20x4":id=="a2"||id=="b1"?"fx_shadow-small_static_12x4":id=="a3"||id=="b2"||id=="b3"?"fx_shadow-wide_static_24x6":throw new InvalidOperationException("Unknown monster "+id);
    }
}
