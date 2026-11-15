using System;
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
        public static string Phase(AdamSession session)=>session.Stage=="boss_intro"?"등장 · 피해 보호":session.Boss?.Telegraphing==true?"내려치기 예고":session.Boss?.ThresholdUsed==true?"분노 · 지원군":"추격";
    }
}
