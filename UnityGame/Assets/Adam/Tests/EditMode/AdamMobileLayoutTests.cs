using System.IO;
using System.Linq;
using BibleSurvivor.Core;
using BibleSurvivor.Unity;
using NUnit.Framework;
using UnityEngine;

public sealed class AdamMobileLayoutTests
{
    [Test] public void PortraitAndLandscapeSafeAreasKeepAllModalTargetsInsideAndAtLeast44PointsOnThreeXPhone()
    {
        foreach(var safe in new[]{new Rect(0,102,1206,2310),new Rect(186,63,2250,1143)})
        {
            var scale=AdamMobileLayout.Scale(safe);Assert.AreEqual(2,scale);
            var w=safe.width/scale;var h=safe.height/scale;
            var panel=AdamMobileLayout.Panel(w,h);var choices=AdamMobileLayout.ChoicePanel(w,h);
            var bounds=new Rect(0,0,w,h);
            foreach(var r in new[]{panel,choices,AdamMobileLayout.StartButton(w,h,0),AdamMobileLayout.StartButton(w,h,2),AdamMobileLayout.ChoiceCard(choices,2)})
            {Assert.IsTrue(bounds.Contains(r.min));Assert.IsTrue(bounds.Contains(r.max));}
            Assert.GreaterOrEqual(AdamMobileLayout.StartButton(w,h,0).height*scale/3,44);
            Assert.GreaterOrEqual(AdamMobileLayout.HudPause(w).height*scale/3,44);
        }
    }

    [TestCase(1206, 2622, 0, 102, 1206, 2310)]
    [TestCase(2622, 1206, 186, 63, 2250, 1143)]
    [TestCase(390, 812, 0, 48, 390, 730)]
    public void WorldFrameIncludesPhysicalEdgesOutsideTheSafeArea(float width, float height, float x, float y, float safeWidth, float safeHeight)
    {
        var physical = new Rect(0, 0, width, height);
        var safe = new Rect(x, y, safeWidth, safeHeight);
        var frame = AdamMobileLayout.FullFrame(safe, physical);
        var scale = AdamMobileLayout.Scale(safe);
        Assert.AreEqual(physical.min, safe.position + frame.min * scale);
        Assert.AreEqual(physical.max, safe.position + frame.max * scale);
        Assert.Less(frame.yMin, 0);
    }

    [TestCase(390)]
    [TestCase(603)]
    [TestCase(1125)]
    public void PauseAndProgressUseSeparateRowsWithoutCoveringAnyHudTarget(float width)
    {
        var rects = new[] { AdamMobileLayout.HudHealth(width), AdamMobileLayout.HudTimer(width),
            AdamMobileLayout.HudPause(width), AdamMobileLayout.HudExperience(width), AdamMobileLayout.HudExperienceValue(width) };
        for (var i = 0; i < rects.Length; i++)
        {
            Assert.GreaterOrEqual(rects[i].xMin, 0);
            Assert.GreaterOrEqual(rects[i].yMin, 0);
            Assert.LessOrEqual(rects[i].xMax, width);
            Assert.LessOrEqual(rects[i].yMax, 116);
            for (var j = i + 1; j < rects.Length; j++)
                Assert.IsFalse(rects[i].Overlaps(rects[j]), $"{rects[i]} overlaps {rects[j]} at {width}");
        }
        Assert.LessOrEqual(rects[2].yMax, 72);
        Assert.GreaterOrEqual(rects[3].yMin, 94);
        Assert.GreaterOrEqual(rects[4].yMin, 92);
        Assert.GreaterOrEqual(rects[4].height, 22);
    }

    [TestCase(390, 812)]
    [TestCase(603, 1167)]
    [TestCase(1125, 569)]
    public void FooterControlsDoNotCoverLegacyOrExpandedEquipment(float width, float height)
    {
        var footer = AdamMobileLayout.HudFooter(width, height);
        var controls = Enumerable.Range(0, 3).Select(slot => AdamMobileLayout.HudFooterControl(width, height, slot)).ToArray();
        for (var i = 0; i < controls.Length; i++)
        {
            Assert.IsTrue(footer.Contains(controls[i].min));
            Assert.IsTrue(footer.Contains(controls[i].max));
            if (i > 0) Assert.Greater(controls[i].xMin, controls[i - 1].xMax);
            foreach (var version in new[] { 1, 2 })
            {
                var weapons = version == 2 ? 6 : 3;
                var passives = version == 2 ? 6 : 10;
                for (var slot = 0; slot < weapons; slot++)
                    Assert.IsFalse(controls[i].Overlaps(AdamHud.InventorySlot(height, slot, true, version)));
                for (var slot = 0; slot < passives; slot++)
                    Assert.IsFalse(controls[i].Overlaps(AdamHud.InventorySlot(height, slot, false, version)));
            }
        }
    }

    [TestCase(390, 812)]
    [TestCase(603, 1167)]
    [TestCase(1125, 569)]
    [TestCase(800, 390)]
    public void ChoiceCardsAndFourActionRowsStayInsideTheirCenteredPanel(float width, float height)
    {
        var panel = AdamMobileLayout.ChoicePanel(width, height);
        Assert.LessOrEqual(Mathf.Abs(width / 2 - panel.center.x), 1);
        Assert.LessOrEqual(Mathf.Abs(height / 2 - panel.center.y), 1);
        var cards = Enumerable.Range(0, 3).Select(slot => AdamMobileLayout.ChoiceCard(panel, slot));
        var actions = Enumerable.Range(0, 4).Select(slot => AdamMobileLayout.Action(panel, slot, 4));
        foreach (var group in new[] { cards, actions })
        {
            var previousBottom = panel.yMin;
            foreach (var rect in group)
            {
                Assert.IsTrue(panel.Contains(rect.min));
                Assert.IsTrue(panel.Contains(rect.max));
                Assert.Greater(rect.height, 0);
                Assert.Greater(rect.yMin, previousBottom);
                previousBottom = rect.yMax;
            }
            Assert.GreaterOrEqual(panel.yMax - previousBottom, 32);
        }
    }

    [Test] public void NotchHomeAndOutsideSafeAreaNeverStartMovement()
    {
        var safe=new Rect(180,90,2200,1020);
        foreach(var p in new[]{new Vector2(10,200),new Vector2(200,10),new Vector2(200,1200),new Vector2(2000,500),new Vector2(200,1000)})
            Assert.IsFalse(AdamMobileLayout.MovementTouchAllowed(p,safe),p.ToString());
        Assert.IsTrue(AdamMobileLayout.MovementTouchAllowed(new Vector2(250,200),safe));
    }
    [Test] public void All71ActualChoiceStringsFitPortraitCardHeadingAndTwoEffectLines()
    {
        var path=Path.GetFullPath(Path.Combine(Application.dataPath,"../../art/art-unity-font-01/export/text_metrics_v1.json"));
        var metrics=JsonUtility.FromJson<AdamFontTests.Metrics>(File.ReadAllText(path));Assert.AreEqual(71,metrics.cards.Length);
        using(var fonts=new AdamFonts())
        {
            var heading=new GUIStyle{font=fonts.Label,fontSize=24,padding=new RectOffset()};
            var body=new GUIStyle{font=fonts.Body,fontSize=20,padding=new RectOffset(),wordWrap=true};
            var card=AdamMobileLayout.ChoiceCard(AdamMobileLayout.ChoicePanel(603,1155),0);
            foreach(var expected in metrics.cards)
            {
                var current=int.Parse(expected.level.Split('-')[0]);
                var inventory=new EquipmentInventory(expected.item=="bible"&&current==0?"jawbone":"bible");
                for(var i=inventory.Weapons.ContainsKey(expected.item)?1:0;i<current;i++)Assert.IsTrue(inventory.TryChoose(expected.item,!AdamGame.Weapons.Contains(expected.item)));
                var actual=AdamChoiceText.Describe(expected.item,inventory).Card(0).Split('\n');
                Assert.LessOrEqual(heading.CalcSize(new GUIContent(actual[0])).x,card.width-52,actual[0]);
                Assert.LessOrEqual(body.CalcHeight(new GUIContent(actual[1]),card.width-52),52,actual[1]);
            }
        }
    }
}
