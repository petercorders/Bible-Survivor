using System;
using System.Linq;
using BibleSurvivor.Core;
using BibleSurvivor.Unity;
using NUnit.Framework;
using UnityEngine;

public class AdamChoiceTextTests
{
    static readonly string[] Passives = { "scroll", "hair", "stones", "east_wind", "seal", "cloud", "thunder", "crown", "underwear", "whirlwind" };
    [TestCase("scroll")] [TestCase("hair")] [TestCase("stones")] [TestCase("east_wind")] [TestCase("seal")]
    [TestCase("cloud")] [TestCase("thunder")] [TestCase("crown")] [TestCase("underwear")] [TestCase("whirlwind")]
    public void EveryPassiveCardMatchesImplementedCoreAtEveryEligibleRank(string id)
    {
        int max = id == "stones" ? 2 : 5;
        for (int rank = 1; rank <= max; rank++)
        {
            var state = GameSnapshot.New(); var session = new AdamSession(state, AdamCatalog.Load().Definition, "bible");
            for (int i = 1; i < rank; i++) Assert.IsTrue(session.Inventory.TryChoose(id, true));
            var card = AdamChoiceText.Describe(id, session.Inventory, session.DamageMultiplier);
            Assert.AreEqual("보조", card.Kind); Assert.AreEqual(rank - 1, card.CurrentLevel); Assert.AreEqual(rank, card.NextLevel); Assert.AreEqual(max, card.Maximum);
            Assert.IsFalse(card.Card(0).Contains(id), "Internal ID must not replace readable content");
            Assert.IsFalse(card.Effect.Contains("진화"));
            Assert.IsTrue(session.Inventory.TryChoose(id, true));
            switch (id)
            {
                case "hair": Assert.AreEqual(card.AfterValue, (session.DamageMultiplier - 1) * 100, 1e-8); break;
                case "underwear": Assert.AreEqual(card.AfterValue, (session.MoveSpeedMultiplier - 1) * 100, 1e-8); break;
                case "stones": Assert.AreEqual(card.AfterValue, session.AttackProfile("bible").Amount - WeaponRules.Get("bible", 1).Amount); break;
                case "cloud": Assert.AreEqual(card.AfterValue, (session.AttackProfile("bible").Duration / WeaponRules.Get("bible", 1).Duration - 1) * 100, 1e-8); break;
                case "whirlwind": Assert.AreEqual(card.AfterValue, (session.AttackProfile("bible").AreaMultiplier - 1) * 100, 1e-8); break;
                case "east_wind":
                    session.Inventory.TryChoose("sling", false);
                    Assert.AreEqual(card.AfterValue, (session.AttackProfile("sling").ProjectileSpeedMultiplier - 1) * 100, 1e-8); break;
                case "thunder":
                    var profile = session.AttackProfile("bible");
                    Assert.AreEqual(card.AfterValue, (1 - (profile.Cycle - profile.Duration) / WeaponRules.Get("bible", 1).Cooldown) * 100, 1e-8); break;
                case "seal": session.Damage(10); Assert.AreEqual(card.AfterValue, session.CurrentHealth - 90); break;
                case "crown": session.Damage(10); Assert.AreEqual(card.AfterValue, session.LastRetaliationDamage, 1e-8); break;
                case "scroll":
                    session.Advance(1); var enemy = session.Wave.Active.Keys.First(); session.Kill(enemy);
                    var raw = session.DroppedExperience[enemy]; session.Collect(enemy);
                    Assert.AreEqual(raw * (1 + card.AfterValue / 100), session.Growth.Experience, 1e-8); break;
            }
        }
    }
    [TestCase("bible", "회전 성경")] [TestCase("jawbone", "삼손의 턱뼈")] [TestCase("sling", "다윗의 물맷돌")]
    public void WeaponCardsFollowRealRuleChangesAtAllSevenUpgrades(string id, string name)
    {
        var inventory = new EquipmentInventory(id);
        for (int rank = 1; rank < 8; rank++)
        {
            var card = AdamChoiceText.Describe(id, inventory);
            Assert.AreEqual(name, card.Name); Assert.AreEqual("무기", card.Kind); Assert.AreEqual(rank, card.CurrentLevel);
            var before = WeaponRules.Get(id, rank); Assert.IsTrue(inventory.TryChoose(id, false));
            var after = WeaponRules.Get(id, inventory.Weapons[id]);
            if (before.Damage != after.Damage) StringAssert.Contains("기본 피해", card.Effect);
            if (before.Amount != after.Amount) StringAssert.Contains("생성·연타 수", card.Effect);
            if (before.Cooldown != after.Cooldown) StringAssert.Contains("대기(초)", card.Effect);
            if (before.Duration != after.Duration) StringAssert.Contains("지속(초)", card.Effect);
            if (before.AreaMultiplier != after.AreaMultiplier) StringAssert.Contains("범위 배율", card.Effect);
            if (before.RotationSpeedMultiplier != after.RotationSpeedMultiplier) StringAssert.Contains("회전속도 배율", card.Effect);
            if (before.Pierce != after.Pierce) StringAssert.Contains("관통 수", card.Effect);
            Assert.IsNotEmpty(card.Effect); Assert.AreEqual(rank + 1, card.NextLevel);
        }
        Assert.Throws<InvalidOperationException>(() => AdamChoiceText.Describe(id, inventory));
    }
    [Test] public void KoreanCardsFit660By440WithoutShrinkingTheir17PixelText()
    {
        var font = AdamChoiceText.CreateKoreanFont();
        try
        {
            var style = new GUIStyle { font = font, fontSize = 17, wordWrap = true, padding = new RectOffset(14, 14, 8, 8) };
            Assert.AreEqual(1, AdamChoiceText.ScreenScale(660, 440));
            var panel = AdamChoiceText.ChoicePanel(660, 440);
            foreach (var id in Passives.Concat(AdamGame.Weapons))
            {
                var inventory = new EquipmentInventory(id == "jawbone" || id == "sling" ? id : "bible");
                var card = AdamChoiceText.Describe(id, inventory);
                var rect = AdamChoiceText.ChoiceCard(panel, 2);
                Assert.LessOrEqual(rect.xMax, 660); Assert.LessOrEqual(rect.yMax, 440); Assert.GreaterOrEqual(rect.xMin, 0);
                Assert.LessOrEqual(style.CalcHeight(new GUIContent(card.Card(2)), rect.width), rect.height, card.Card(2));
                font.RequestCharactersInTexture(card.Card(2), 17);
                foreach (var character in card.Name) Assert.IsTrue(font.HasCharacter(character), "Missing Korean glyph: " + character);
            }
        }
        finally { UnityEngine.Object.DestroyImmediate(font); }
    }
    [Test] public void RetaliationCardIncludesExistingDamageBonus()
    {
        var state = GameSnapshot.New(); var session = new AdamSession(state, AdamCatalog.Load().Definition, "sling");
        session.Inventory.TryChoose("hair", true);
        var card = AdamChoiceText.Describe("crown", session.Inventory, session.DamageMultiplier);
        session.Inventory.TryChoose("crown", true); session.Damage(10);
        Assert.AreEqual(5.5, card.AfterValue, 1e-8); Assert.AreEqual(session.LastRetaliationDamage, card.AfterValue, 1e-8);
    }
}
