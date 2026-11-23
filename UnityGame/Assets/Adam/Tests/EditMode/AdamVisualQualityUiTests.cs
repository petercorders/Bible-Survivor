using System;
using System.Linq;
using System.IO;
using BibleSurvivor.Core;
using BibleSurvivor.Unity;
using NUnit.Framework;
using UnityEngine;

public sealed class AdamVisualQualityUiTests
{
    [TestCase(660,440)][TestCase(1100,720)]
    public void HealthTimerPauseAndAllInventorySlotsFitWithoutCoveringTheArena(float width,float height)
    {
        var hp=AdamHud.Health(width);var timer=AdamHud.TimerRect(width);var pause=AdamHud.Pause(width,false);
        Assert.IsFalse(hp.Overlaps(timer));Assert.IsFalse(timer.Overlaps(pause));
        foreach(var rect in new[]{hp,timer,pause,AdamHud.Experience(width)})
        {Assert.GreaterOrEqual(rect.xMin,0);Assert.LessOrEqual(rect.xMax,width);Assert.LessOrEqual(rect.yMax,AdamGame.Arena(width,height).yMin);}
        var footer=AdamHud.Footer(width,height);Assert.AreEqual(AdamGame.Arena(width,height).yMax,footer.yMin);
        for(var i=0;i<13;i++)
        {
            var rect=AdamHud.InventorySlot(height,i<3?i:i-3,i<3);
            Assert.GreaterOrEqual(rect.yMin,footer.yMin);Assert.LessOrEqual(rect.yMax+5,height);
            Assert.GreaterOrEqual(rect.xMin,0);Assert.LessOrEqual(rect.xMax,350);
            for(var j=0;j<i;j++)Assert.IsFalse(rect.Overlaps(AdamHud.InventorySlot(height,j<3?j:j-3,j<3)));
        }
    }

    [TestCase(660,440)][TestCase(1100,720)]
    public void ExpandedInventoryFitsSixWeaponsAndSixPassivesBeforeTheLegacyHints(float width,float height)
    {
        var footer=AdamHud.Footer(width,height);
        var slots=Enumerable.Range(0,6).Select(slot=>AdamHud.InventorySlot(height,slot,true,2))
            .Concat(Enumerable.Range(0,6).Select(slot=>AdamHud.InventorySlot(height,slot,false,2))).ToArray();
        for(var i=0;i<slots.Length;i++)
        {
            var rect=slots[i];
            Assert.GreaterOrEqual(rect.yMin-2,footer.yMin);
            Assert.LessOrEqual(rect.yMax+6,height);
            Assert.GreaterOrEqual(rect.xMin,0);
            Assert.LessOrEqual(rect.xMax,Mathf.Max(350,width*.5f));
            for(var j=0;j<i;j++)
            {
                var withPips=slots[j];
                withPips.yMax+=6;
                Assert.IsFalse(rect.Overlaps(withPips));
            }
        }
        Assert.Throws<ArgumentOutOfRangeException>(()=>AdamHud.InventorySlot(height,3,true));
    }

    [Test] public void BossClockCannotBecomeAnElapsedTimeOverNormalThreeMinuteDenominator()
    {
        Assert.AreEqual("0:06",AdamHud.Timer(186.2,6.2));
        Assert.AreEqual("3:00",AdamHud.Timer(186.2,null));
        Assert.AreEqual("0:47",AdamHud.Timer(47.9,null));
        Assert.AreEqual("0:00",AdamHud.Timer(0,null));
        Assert.AreEqual("5:00",AdamHud.Timer(480,300));
        StringAssert.DoesNotContain("/",AdamHud.Timer(186.2,6.2));
        Assert.AreEqual(.5f,AdamHud.Fraction(50,100));Assert.AreEqual(0,AdamHud.Fraction(-10,100));Assert.AreEqual(1,AdamHud.Fraction(150,100));
    }

    [Test] public void AsymmetricKeycapKeepsBottomFourAndTopThreeSourcePixels()
    {
        // Approved normalized manifest is left,bottom,right,top = 3,4,3,3.
        var gui=AdamHud.GuiMargins(new[]{3,4,3,3},2);
        Assert.AreEqual(new Vector4(6,6,6,8),gui);
    }

    [TestCase(false,true,true,true,3)]
    [TestCase(true,false,false,true,2)]
    [TestCase(true,true,false,false,1)]
    [TestCase(true,false,false,false,0)]
    public void ReviewedSurfaceStatesKeepDisabledPressedFocusAndNormalDistinct(bool enabled,bool focused,bool hovered,bool pressed,int expected)
    {Assert.AreEqual(expected,AdamHud.SurfaceState(enabled,focused,hovered,pressed));}

    [Test] public void CardRolesKeepActualCoreNamesAmountsAndLevelDescriptions()
    {
        var inventory=new EquipmentInventory("sling");
        var weapon=AdamChoiceText.Describe("sling",inventory);var text=weapon.Card(0);
        Assert.AreEqual(weapon.Name,AdamHud.CardName(text));
        StringAssert.Contains("1 → 2/8",AdamHud.CardTag(text));
        StringAssert.Contains(weapon.Effect,text);
        var currency=AdamChoiceText.Describe(AdamSession.GrowthGoldChoicePrefix+"1",inventory);
        Assert.AreEqual(currency.Name,AdamHud.CardName(currency.Card(1)));
        Assert.AreEqual("재화",AdamHud.CardTag(currency.Card(1)));
        StringAssert.Contains("+25",currency.Card(1));
        var boss=AdamBossPresentation.Card(new BossRewardCandidate{Kind="gold",Gold=100},2,null);
        Assert.AreEqual("골드 보상",AdamHud.CardName(boss));StringAssert.Contains("+100",boss);
    }

    [Test] public void ExpandedCardsUseAllSevenCoreNamesAndTheirLevelAndFormDescriptions()
    {
        foreach(var profile in ExpandedWeaponRules.All)
        {
            var inventory=new EquipmentInventory("bible",2);
            for(var level=0;level<8;level++)
            {
                var choice=AdamChoiceText.Describe(profile.WeaponId,inventory);
                Assert.AreEqual(profile.Name,choice.Name);
                Assert.AreEqual("무기",choice.Kind);
                Assert.AreEqual(level,choice.CurrentLevel);
                Assert.IsNotEmpty(choice.Effect);
                if(level==0)
                {
                    StringAssert.Contains("기본 피해 "+profile.AtLevel(1).Damage,choice.Effect);
                    StringAssert.Contains("수량 "+profile.AtLevel(1).Amount,choice.Effect);
                }
                Assert.IsTrue(inventory.TryChoose(profile.WeaponId,false));
            }
            var card=AdamBossPresentation.Card(new BossRewardCandidate{Kind="evolution",ItemId=profile.EvolutionId},0,null);
            Assert.AreEqual(profile.EvolutionName,AdamHud.CardName(card));
            Assert.AreEqual(profile.EvolutionName,AdamChoiceText.ItemName(profile.EvolutionId));
            Assert.AreEqual("진화",AdamHud.CardTag(card));
            StringAssert.Contains(AdamBossPresentation.EvolutionEffect(profile.EvolutionId),card);
        }
    }

    [Test] public void PermanentShopOffersOnlyTheLinkedCommonAndWeaponEffects()
    {
        Assert.AreEqual(92,PermanentSkillCatalog.Current.Nodes.Count(node=>AdamGame.OffersPermanentSkillPurchase(node.Id)));
        foreach(var profile in ExpandedWeaponRules.All)
        {
            for(var rank=1;rank<=3;rank++)
                Assert.IsTrue(AdamGame.OffersPermanentSkillPurchase($"weapon.{profile.WeaponId}.base.0{rank}"));
            Assert.IsFalse(AdamGame.OffersPermanentSkillPurchase($"weapon.{profile.WeaponId}.base.04"));
        }
        Assert.IsTrue(AdamGame.OffersPermanentSkillPurchase("common.health.10"));
        Assert.IsTrue(AdamGame.OffersPermanentSkillPurchase("common.armor.10"));
        Assert.IsTrue(AdamGame.OffersPermanentSkillPurchase("common.regen.02"));
        Assert.IsTrue(AdamGame.OffersPermanentSkillPurchase("common.move.08"));
        Assert.IsTrue(AdamGame.OffersPermanentSkillPurchase("common.xp.07"));
        Assert.IsTrue(AdamGame.OffersPermanentSkillPurchase("common.pickup.10"));
        Assert.IsFalse(AdamGame.OffersPermanentSkillPurchase("common.crit.01"));
    }

    [TestCase(false)][TestCase(true)]
    public void ReskinnedSelectionStillRejectsSuspendedDuplicateAndFailedSaveCommands(bool failSave)
    {
        var folder=Path.Combine(Path.GetTempPath(),"adam-visual-ui-"+Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(folder);var owner=new GameObject("visual UI command guard");owner.SetActive(false);
        try
        {
            var failActive=false;
            var host=new AdamHost(AdamCatalog.Load(),new SaveStore(Path.Combine(folder,"save.json"),()=>{if(failSave&&failActive)throw new IOException("visual UI fixture");}));
            var game=owner.AddComponent<AdamGame>();game.AutoInitialize=false;game.enabled=false;game.Initialize(host);
            game.SetLifecycle(false,true);game.SelectSlot(0);game.EnterCommand();Assert.IsNull(host.State.Run);
            game.SetLifecycle(true,false);failActive=true;game.SelectSlot(0);var run=host.State.Run.RunId;var revision=host.State.Revision;
            game.SelectSlot(2);game.EnterCommand();game.EnterExodus();
            Assert.AreEqual(run,host.State.Run.RunId);Assert.AreEqual(revision,host.State.Revision);
            Assert.IsTrue(host.Session.Inventory.Weapons.ContainsKey("bible"));Assert.AreEqual(1,host.Session.RoundNumber);
            if(failSave){Assert.IsNotNull(host.Error);Assert.IsTrue(host.HasSaveRetry);}else Assert.IsNull(host.Error);
        }
        finally{UnityEngine.Object.DestroyImmediate(owner);Directory.Delete(folder,true);}
    }
}
