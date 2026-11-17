using System;
using UnityEngine;

namespace BibleSurvivor.Unity
{
    public static class AdamRoundPresentation
    {
        public static string Name(int round)=>round==1?"아담":round==2?"가인":round==3?"노아":round==4?"아브라함":round==5?"롯":round==6?"이삭":round==7?"야곱":round==8?"요셉":round==9?"유다":round==10?"십브라·부아":round==11?"요게벳":round==12?"모세":round==13?"미리암":throw new InvalidOperationException("Round is outside installed content");
        public const string ChapterHeading="창세기 완료 · 진행 저장됨";
        public const string ChapterDetail="진행 유지 · 출애굽기는 현재 성장·장비로 계속\n새 도전 선택 시 성장·장비 초기화";
        public const string ContentHeading="출애굽기 4 완료 · 진행 저장됨";
        public const string ContentDetail="완료 골드 +240 · 진행 유지\nR14 대기 · 다음 라운드 준비 중\n새 도전 선택 시 성장·장비 초기화";
        public static string Title(AdamHost host)=>Title(host?.Session?.RoundNumber??1);
        public static string Title(int round)=>(round>=10?"출애굽기 "+(round-9):"창세기")+" / "+Name(round)+" · 3분 시제품";
        public static string ControlHint(int round)=>"WASD / 방향키 이동 · 자동 공격 · Esc 정지 · Enter 재개";
        public static bool R02Enemy(string id)=>id=="b1"||id=="b2"||id=="b3";
        public static bool R03Enemy(string id)=>id=="c1"||id=="c2"||id=="c3"||id=="nephilim_tyrant";
        public static bool R04Enemy(string id)=>id=="d1"||id=="d2"||id=="d3";
        public static bool R05Enemy(string id)=>id=="e1"||id=="e2"||id=="e3";
        public static bool R06Enemy(string id)=>id=="f1"||id=="f2"||id=="f3"||id=="sitnah_keeper";
        public static bool R07Enemy(string id)=>id=="g1"||id=="g2"||id=="g3";
        public static bool R08Enemy(string id)=>id=="h1"||id=="h2"||id=="h3";
        public static bool R09Enemy(string id)=>id=="i1"||id=="i2"||id=="i3"||id=="silver_cup_accuser";
        public static bool R11Enemy(string id)=>id=="ex4"||id=="ex5"||id=="ex6";
        public static bool R12Enemy(string id)=>id=="ex7"||id=="ex8"||id=="ex9"||id=="oppression_chariot";
        public static bool R13Enemy(string id)=>id=="ex10"||id=="ex11"||id=="ex12";
        // Explicit reviewed R02 sidecar mapping; unknown IDs never fall back to Art03.
        public static string R02ClipId(string id)=>id=="b1"?"enm_b1-envy-eye_move_32":id=="b2"?"enm_b2-rage-hound_run_32":id=="b3"?"enm_b3-cracked-mask_move_32":throw new InvalidOperationException("Unknown R02 monster "+id);
        public static string Shadow(string id)=>id=="a1"?"fx_shadow-snake_static_20x4":id=="a2"||id=="b1"||id=="c1"||id=="d2"||id=="e1"||id=="e3"||id=="f1"||id=="g3"||id=="h1"||id=="i1"||id=="i2"||id=="ex2"||id=="ex5"||id=="ex10"?"fx_shadow-small_static_12x4":id=="a3"||id=="b2"||id=="b3"||id=="c2"||id=="c3"||id=="d1"||id=="d3"||id=="e2"||id=="f2"||id=="f3"||id=="g1"||id=="g2"||id=="h2"||id=="h3"||id=="i3"||id=="ex1"||id=="ex3"||id=="ex4"||id=="ex6"||id=="ex7"||id=="ex8"||id=="ex9"||id=="ex11"||id=="ex12"?"fx_shadow-wide_static_24x6":id=="nephilim_tyrant"||id=="sitnah_keeper"||id=="silver_cup_accuser"||id=="oppression_chariot"?"fx_shadow-boss_static_40x8":throw new InvalidOperationException("Unknown monster "+id);
    }
}
