using System;
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
        public static string Name(BossDefinition rules)=>rules.Id=="nephilim_tyrant"?"네피림 폭군":rules.Id=="sitnah_keeper"?"싯나의 우물지기":rules.Id=="silver_cup_accuser"?"은잔의 참소자":rules.Id=="oppression_chariot"?"압제의 전차장":rules.Id=="defilement_weaver"?"훼손의 직조군주":throw new ArgumentException("Unsupported boss ID");
        public static string Name(AdamSession session)=>Name(session.BossRules);
        public static string RewardDetail(AdamSession session)=>RewardDetail(session.BossRules);
        public static string RewardDetail(BossDefinition rules)=>Name(rules)+" · 게임 각색\n고정 골드 +"+rules.KillGold+" 적용됨 · 선택 즉시 저장";
        public static double HealthFraction(BossEncounterState boss,BossDefinition rules)=>boss.Health/rules.MaxHealth;
        public static double HealthFraction(AdamSession session)=>HealthFraction(session.Boss,session.BossRules);
        public static bool UsesTwoLineHud(BossDefinition rules)=>rules.IsChariot||rules.Number==15;
        public static string AttackPhase(int round,bool telegraphing,bool support,double period)
        {
            if(round==15)return (support?"지원군 · ":"")+(telegraphing?"쇳조각 3방향 예고":"쇳조각 3방향")+" · "+period.ToString("0.#",CultureInfo.InvariantCulture)+"초 주기";
            if(round==12)return (support?"지원군 · ":"")+(telegraphing?"돌진 예고":"추격")+" · 돌진 "+period.ToString("0.#",CultureInfo.InvariantCulture)+"초 주기";
            if(telegraphing)return round==9?"사슬 3방향 예고":round==6?"흙더미 낙하 예고":"내려치기 예고";
            if(round==9)return (support?"지원군 · ":"")+"사슬 "+period.ToString("0.#",CultureInfo.InvariantCulture)+"초 주기";
            return support?"분노 · 지원군":"추격";
        }
        public static string Phase(AdamSession session)=>Phase(session.Stage,session.Boss,session.BossRules);
        public static string Phase(string stage,BossEncounterState boss,BossDefinition rules)
        {
            if(stage=="boss_intro")return "등장 · 피해 보호";
            if(rules.Number==15)return AttackPhase(rules.Number,boss?.Telegraphing==true,boss?.ThresholdUsed==true,rules.AttackPeriod)+(boss?.Telegraphing==true?" · "+rules.TelegraphDuration.ToString("0.#",CultureInfo.InvariantCulture)+"초":"");
            if(!rules.IsChariot)return AttackPhase(rules.Number,boss?.Telegraphing==true,boss?.ThresholdUsed==true,rules.AttackPeriod);
            var phase=boss?.ChariotCharge?.Phase;
            var label=phase=="dash"?"돌진 진행 · 고정 방향":phase=="telegraph"?"돌진 예고 "+rules.TelegraphDuration.ToString("0.#",CultureInfo.InvariantCulture)+"초 · 고정 방향":"추격";
            return (boss?.ThresholdUsed==true?"지원군 · ":"")+label+" · "+rules.AttackPeriod.ToString("0.#",CultureInfo.InvariantCulture)+"초 주기";
        }
    }
}
