using System;
using System.Collections.Generic;
using System.Globalization;
using BibleSurvivor.Core;
using UnityEngine;

namespace BibleSurvivor.Unity
{
    public sealed class AdamChoiceText
    {
        public string Id { get; private set; }
        public string Name { get; private set; }
        public string Kind { get; private set; }
        public int CurrentLevel { get; private set; }
        public int NextLevel => CurrentLevel + 1;
        public int Maximum { get; private set; }
        public string Effect { get; private set; }
        // Passive contribution values allow tests to compare the card with actual Core behavior.
        public double BeforeValue { get; private set; }
        public double AfterValue { get; private set; }
        public string Card(int slot) => $"{slot + 1}. {Name}  |  {Kind} · {(CurrentLevel == 0 ? "신규" : "강화")} {CurrentLevel} → {NextLevel}/{Maximum}\n{Effect}";
        public static string ItemName(string id)
        {
            switch (id)
            {
                case "bible": return "회전 성경";
                case "jawbone": return "삼손의 턱뼈";
                case "sling": return "다윗의 물맷돌";
                case "scroll": return "말씀의 두루마리";
                case "hair": return "삼손의 머리카락";
                case "stones": return "매끄러운 다섯 돌";
                case "east_wind": return "동풍";
                case "seal": return "사자굴의 봉인석";
                case "cloud": return "구름기둥";
                case "thunder": return "우레";
                case "crown": return "가시 면류관";
                case "underwear": return "요한의 팬티";
                case "whirlwind": return "회오리바람";
                default: throw new ArgumentException("No implemented Adam choice description", nameof(id));
            }
        }
        static string N(double value) => value.ToString("0.##", CultureInfo.InvariantCulture);
        public static AdamChoiceText Describe(string id, EquipmentInventory inventory, double damageMultiplier = 1)
        {
            var weapon = id == "bible" || id == "jawbone" || id == "sling";
            var levels = weapon ? inventory.Weapons : inventory.Passives;
            var current = levels.TryGetValue(id, out var level) ? level : 0;
            var result = new AdamChoiceText { Id = id, Name = ItemName(id), Kind = weapon ? "무기" : "보조", CurrentLevel = current, Maximum = weapon ? 8 : id == "stones" ? 2 : 5 };
            if (current >= result.Maximum) throw new InvalidOperationException("Item is already at maximum level");
            if (weapon)
            {
                var next = WeaponRules.Get(id, current + 1);
                if (current == 0)
                {
                    var attack = id == "bible" ? "주변 회전" : id == "jawbone" ? "전방 120° 타격" : "가까운 적 자동 조준";
                    result.Effect = $"{attack} · 기본 피해 {next.Damage} · 수 {next.Amount} · 대기 {N(next.Cooldown)}초";
                }
                else
                {
                    var before = WeaponRules.Get(id, current); var changes = new List<string>();
                    Add(changes, "기본 피해", before.Damage, next.Damage);
                    Add(changes, "생성·연타 수", before.Amount, next.Amount);
                    Add(changes, "대기(초)", before.Cooldown, next.Cooldown);
                    Add(changes, "지속(초)", before.Duration, next.Duration);
                    Add(changes, "범위 배율", before.AreaMultiplier, next.AreaMultiplier);
                    Add(changes, "회전속도 배율", before.RotationSpeedMultiplier, next.RotationSpeedMultiplier);
                    Add(changes, "관통 수", before.Pierce, next.Pierce);
                    result.Effect = string.Join(" · ", changes);
                }
                return result;
            }
            string effect; string unit;
            double perLevel;
            switch (id)
            {
                case "scroll": effect = "경험치 획득 보너스"; perLevel = 8; unit = "%"; break;
                case "hair": effect = "무기 피해 보너스"; perLevel = 10; unit = "%"; break;
                case "stones": effect = "무기 생성·연타 추가 수"; perLevel = 1; unit = "개"; break;
                case "east_wind": effect = "돌 투사체 속도 보너스"; perLevel = 10; unit = "%"; break;
                case "seal": effect = "보조 방어(피해 감소)"; perLevel = 1; unit = " (최소 피해 1)"; break;
                case "cloud": effect = "회전 성경 지속시간 보너스"; perLevel = 10; unit = "%"; break;
                case "thunder": effect = "대기시간 감소 보너스"; perLevel = 8; unit = "% (합산 최대 80%)"; break;
                case "crown": effect = "피격 시 근처 적 반격 피해"; perLevel = 5 * damageMultiplier; unit = " (피해 보너스 반영)"; break;
                case "underwear": effect = "이동속도 보너스"; perLevel = 10; unit = "%"; break;
                case "whirlwind": effect = "무기 범위 보너스"; perLevel = 10; unit = "%"; break;
                default: throw new ArgumentException(nameof(id));
            }
            result.BeforeValue = current * perLevel; result.AfterValue = (current + 1) * perLevel;
            result.Effect = $"{effect} {N(result.BeforeValue)} → {N(result.AfterValue)}{unit}";
            return result;
        }
        static void Add(List<string> changes, string label, double before, double after)
        { if (before != after) changes.Add($"{label} {N(before)} → {N(after)}"); }
        public static Font CreateKoreanFont() => Font.CreateDynamicFontFromOSFont(new[] { "Apple SD Gothic Neo", "Malgun Gothic", "Noto Sans CJK KR", "Arial Unicode MS" }, 18);
        public static float ScreenScale(float width, float height) => Math.Min(1, Math.Min(width / 660f, height / 440f));
        public static Rect ChoicePanel(float width, float height) => new Rect((width - Math.Min(760, width - 24)) / 2, (height - 410) / 2, Math.Min(760, width - 24), 410);
        public static Rect ChoiceCard(Rect panel, int slot) => new Rect(panel.x + 12, panel.y + 94 + slot * 94, panel.width - 24, 88);
    }
}
