using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using BibleSurvivor.Core;

namespace BibleSurvivor.Unity
{
    public static class AdamBossPresentation
    {
        public static string FormName(string form)=>form==EvolutionRules.Bible?"성역의 성경":form==EvolutionRules.Jawbone?"천 명의 턱뼈":form==EvolutionRules.Sling?"다섯 돌의 물맷돌":throw new ArgumentException("Unknown form");
        public static string EvolutionEffect(string form)=>form==EvolutionRules.Bible?"회전 유지 · 2초마다 성역 파동":form==EvolutionRules.Jawbone?"타격 범위 +25% · 적 밀치기":form==EvolutionRules.Sling?"최소 5연발 · 최대 체력 적 우선":throw new ArgumentException("Unknown form");
        public static string Forms(AdamSession session)=>string.Join(" · ",session.Inventory.Forms.Values.Select(FormName));
        public static string Card(BossRewardCandidate candidate,int slot,AdamSession session)
        {
            if(candidate.Kind=="evolution")return $"{slot+1}. {FormName(candidate.ItemId)} | 진화\n{EvolutionEffect(candidate.ItemId)}";
            if(candidate.Kind=="gold")return $"{slot+1}. 골드 보상\n골드 +{candidate.Gold}";
            if(candidate.Kind=="item")return AdamChoiceText.Describe(candidate.ItemId,session.Inventory,session.DamageMultiplier).Card(slot);
            throw new ArgumentException("Unsupported reward candidate");
        }
        public static string Name(int round)=>Name(BossDefinition.ForRound(round));
        public static string Name(BossDefinition rules)=>rules.Id switch
        {
            "nephilim_tyrant"=>"네피림 폭군",
            "sitnah_keeper"=>"싯나의 우물지기",
            "silver_cup_accuser"=>"은잔의 참소자",
            "oppression_chariot"=>"압제의 전차장",
            "defilement_weaver"=>"훼손의 직조군주",
            "ash_veil"=>"잿빛 망령",
            "oath_wraith"=>"서원의 망령",
            "dust_tyrant"=>"먼지의 폭군",
            "ravine_stalker"=>"협곡의 추적자",
            "wilderness_warden"=>"광야의 수문장",
            "oblivion_mist"=>"망각의 안개",
            "crossroads_sentinel"=>"갈림길의 파수꾼",
            "rampart_colossus"=>"성벽의 거상",
            "shifting_boundary"=>"흔들리는 경계석",
            "torrent_serpent"=>"급류의 뱀",
            "ford_gatekeeper"=>"나루터의 문지기",
            "toppling_pillar"=>"무너지는 기둥",
            "chaff_whirl"=>"타작마당의 겨바람",
            "swarm_monarch"=>"무리의 군주",
            "valley_bronze_brute"=>"골짜기의 청동 짐승",
            "palace_roof_eye"=>"왕궁 지붕의 엿보는 눈",
            "lentil_field_beast"=>"녹두밭을 짓밟는 무리 짐승",
            "bethel_altar_calf"=>"벧엘 제단의 금송아지 형상",
            "vineyard_greed_vine"=>"포도원을 삼키는 탐욕 넝쿨",
            "dothan_iron_ring"=>"도단을 조이는 쇠고리",
            "winged_bull"=>"날개 달린 황소",
            "ashen_gate_lion"=>"잿빛 성문 사자",
            "wilderness_raider_chief"=>"광야 약탈 떼의 우두머리 짐승",
            "rending_host"=>"서로 물어뜯는 군세",
            "bronze_hook_chain"=>"청동 갈고리 사슬",
            "roadside_ambush_shadow"=>"길목의 매복 그림자",
            "ono_whispering_trap"=>"오노 평지의 속삭이는 덫",
            "rolling_pur_cart"=>"굴러오는 부르(제비) 수레",
            "king_of_terrors"=>"공포의 왕","roaring_waters"=>"뛰노는 바닷물","terror_by_night"=>"밤의 공포",
            "veiling_darkness"=>"덮으려는 흑암","clamoring_leech"=>"‘다오 다오’ 하는 거머리",
            "clouds_of_trouble"=>"곤고한 날의 먹구름","leopard_of_the_peaks"=>"표범 산의 표범",
            "night_beast_of_ruins"=>"폐허에 깃드는 밤 짐승","leviathan_shadow"=>"날랜 뱀 리워야단의 그림자",
            "viper_eggs_and_web"=>"독사의 알과 거미줄",
            "terror_all_around"=>"사방의 두려움","cistern_mud"=>"웅덩이의 진흙","destroying_mountain"=>"온 세계를 멸하는 멸망의 산",
            "wormwood_and_gall"=>"쑥과 담즙","soul_snaring_band"=>"영혼을 사냥하는 부적 띠",
            "proud_high_cedar"=>"하늘에 닿은 교만한 백향목","gog_host_from_the_north"=>"북쪽 끝에서 오는 곡의 군세",
            "golden_image_of_dura"=>"두라 평지의 금 신상","ten_horned_fourth_beast"=>"열 뿔 가진 넷째 짐승",
            "spiritual_prince_of_persia"=>"바사 왕국의 군주",
            "death_and_sheol"=>"사망과 스올","eagle_of_high_crags"=>"바위 틈 높은 곳의 독수리","lingering_anger"=>"식지 않는 성냄",
            "lightning_torch_chariot"=>"번개 같은 횃불 병거","famine_of_the_empty_fold"=>"빈 외양간의 기근",
            "rebuked_accusing_shadow"=>"고발하는 그림자","malachi_devourer"=>"삼키는 자",
            _=>throw new ArgumentException("Unsupported boss ID")
        };
        public static string Name(AdamSession session)=>Name(session.BossRules);
        public static string RewardDetail(AdamSession session)=>RewardDetail(session.BossRules);
        public static string RewardDetail(BossDefinition rules)=>Name(rules)+" · 게임 각색\n고정 골드 +"+rules.KillGold+" 적용됨 · 선택 즉시 저장";
        public static double HealthFraction(BossEncounterState boss,BossDefinition rules)=>boss.Health/rules.MaxHealth;
        public static double HealthFraction(AdamSession session)=>HealthFraction(session.Boss,session.BossRules);
        public static bool UsesTwoLineHud(BossDefinition rules)=>rules.Number>60?rules.UsesDash||rules.UsesChainProjectiles||rules.UsesSlam||rules.UsesAimedShot||rules.UsesDamageZone:rules.IsChariot||UsesShardHud(rules.Number);
        static bool UsesShardHud(int round)=>round is 15 or 18 or 21 or 24 or 27 or 30 or 33 or 36 or 39 or 42 or 45 or 48 or 51 or 54 or 57 or 60;
        public static string AttackPhase(int round,bool telegraphing,bool support,double period)
        {
            if(UsesShardHud(round))
            {
                var projectile=round==15?"쇳조각":round==18?"재 파편":"파편";
                return (support?"지원군 · ":"")+projectile+" 3방향"+(telegraphing?" 예고":"")+" · "+period.ToString("0.#",CultureInfo.InvariantCulture)+"초 주기";
            }
            if(round==12)return (support?"지원군 · ":"")+(telegraphing?"돌진 예고":"추격")+" · 돌진 "+period.ToString("0.#",CultureInfo.InvariantCulture)+"초 주기";
            if(telegraphing)return round==9?"사슬 3방향 예고":round==6?"흙더미 낙하 예고":"내려치기 예고";
            if(round==9)return (support?"지원군 · ":"")+"사슬 "+period.ToString("0.#",CultureInfo.InvariantCulture)+"초 주기";
            return support?"분노 · 지원군":"추격";
        }
        public static string AttackPhase(BossActions actions,bool telegraphing,bool support,string dashPhase)
        {
            var labels=new List<string>();
            if((actions&BossActions.Dash)!=0)labels.Add(dashPhase=="dash"?"돌진 진행":"돌진");
            if((actions&BossActions.Slam)!=0)labels.Add("내려치기");
            if((actions&BossActions.Chains)!=0)labels.Add("사슬 3방향");
            if ((actions & BossActions.AimedShot) != 0) labels.Add("조준 사격");
            if ((actions & BossActions.DamageZone) != 0) labels.Add("피해 지대");
            var attack=labels.Count==0?"추격":string.Join(" · ",labels)+(telegraphing?" 예고":"");
            return (support?"지원군 · ":"")+attack;
        }

        public static string Phase(AdamSession session)=>Phase(session.Stage,session.Boss,session.BossRules);
        public static string Phase(string stage,BossEncounterState boss,BossDefinition rules)
        {
            if(stage=="boss_intro")return "등장 · 피해 보호";
            if(rules.Number>60)return AttackPhase(rules.Actions,boss?.Telegraphing==true,
                boss?.ThresholdUsed==true,boss?.ChariotCharge?.Phase);
            if(UsesShardHud(rules.Number))return AttackPhase(rules.Number,boss?.Telegraphing==true,boss?.ThresholdUsed==true,rules.AttackPeriod)+(boss?.Telegraphing==true?" · "+rules.TelegraphDuration.ToString("0.#",CultureInfo.InvariantCulture)+"초":"");
            if(!rules.IsChariot)return AttackPhase(rules.Number,boss?.Telegraphing==true,boss?.ThresholdUsed==true,rules.AttackPeriod);
            var phase=boss?.ChariotCharge?.Phase;
            var label=phase=="dash"?"돌진 진행 · 고정 방향":phase=="telegraph"?"돌진 예고 "+rules.TelegraphDuration.ToString("0.#",CultureInfo.InvariantCulture)+"초 · 고정 방향":"추격";
            return (boss?.ThresholdUsed==true?"지원군 · ":"")+label+" · "+rules.AttackPeriod.ToString("0.#",CultureInfo.InvariantCulture)+"초 주기";
        }
    }
}
