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
            Assert.GreaterOrEqual(66*scale/3,44);
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
