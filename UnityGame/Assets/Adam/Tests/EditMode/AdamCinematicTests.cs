using BibleSurvivor.Unity;
using NUnit.Framework;
using UnityEngine;

public sealed class AdamCinematicTests
{
    [TestCase(1920,1080,0,0,1920,1080)]
    [TestCase(660,440,90,85,480,270)]
    public void PixelFrameCentersAtAnIntegerScale(int width,int height,int x,int y,int frameWidth,int frameHeight)
    {
        Assert.AreEqual(new Rect(x,y,frameWidth,frameHeight),AdamCinematic.Frame(new Rect(0,0,width,height)));
    }

    [TestCase(true,6)]
    [TestCase(false,2)]
    public void ScriptPlaysEveryCutAndCompletes(bool creation,int expectedCuts)
    {
        var kind = creation ? AdamCinematicKind.Creation : AdamCinematicKind.GenesisEnding;
        var replay = new AdamCinematic();
        Assert.IsFalse(replay.Begin(kind,null));
        Assert.IsFalse(replay.Begin(kind,"{broken"));
        Assert.IsFalse(replay.Begin(kind,"{\"cuts\":[]}"));
        var script = Resources.Load<TextAsset>(AdamCinematic.ResourceName(kind));
        Assert.IsTrue(replay.Begin(kind,script.text));
        Assert.IsFalse(replay.Begin(kind,null));
        replay.Paused = true;
        replay.Advance(30);
        Assert.Zero(replay.Elapsed);
        replay.Paused = false;
        var cuts = new System.Collections.Generic.HashSet<AdamCinematic.Cut>();
        for(var frame=0;frame<220 && replay.Active;frame++)
        {
            cuts.Add(replay.Current);
            replay.Advance(.1f);
        }
        Assert.IsFalse(replay.Active);
        Assert.AreEqual(expectedCuts,cuts.Count);
        if (creation)
            Assert.AreEqual("창 1:24–25", replay.Current.reference);
    }

    [Test]
    public void ExodusOpeningSharesEndingMotifAndEndsAtTheWorksite()
    {
        AdamCinematic.Cut[] Read(AdamCinematicKind kind) =>
            AdamCinematic.Read(kind, Resources.Load<TextAsset>(AdamCinematic.ResourceName(kind)).text);
        var ending = Read(AdamCinematicKind.GenesisEnding);
        var opening = Read(AdamCinematicKind.ExodusOpening);
        Assert.AreEqual("창 50:24–26", ending[1].reference);
        Assert.AreEqual("출 1:6–7", opening[0].reference);
        StringAssert.Contains("여러 세대", opening[0].caption);
        Assert.AreEqual("egypt-scroll-promise", opening[0].motif);
        Assert.AreEqual(ending[1].motif, opening[0].motif);
        Assert.AreEqual("출 1:8–14", opening[1].reference);
        Assert.AreEqual(14, opening[2].end);
        Assert.IsFalse(new AdamCinematic().Begin(AdamCinematicKind.ExodusOpening, null));
    }
    [TestCase(1, 6)]
    [TestCase(2, 2)]
    [TestCase(3, 3)]
    public void ApprovedResourcesLoadAtEveryCutAndPageBoundary(int kindValue, int count)
    {
        var kind = (AdamCinematicKind)kindValue;
        var script = Resources.Load<TextAsset>(AdamCinematic.ResourceName(kind));
        var cuts = AdamCinematic.Read(kind, script.text);
        var timeline = new AdamCinematic();
        Assert.IsTrue(timeline.Begin(kind, script.text));
        var art = new AdamGenesisCinematicMedia();
        art.Begin(kind, cuts);
        var viewport = new Rect(0, 0, 660, 440);
        try
        {
            for (var cut = 0; cut < count; cut++)
            {
                var start = cut == 0 ? 0 : cuts[cut - 1].end;
                AdvanceTo(timeline, start + .003f);
                Assert.AreEqual(cut, timeline.CurrentIndex);
                Assert.IsTrue(art.TryGetFrame(timeline, false, viewport, out var still));
                Assert.IsFalse(still.LiveTerrain);
                Assert.AreEqual(FilterMode.Point, still.Texture.filterMode);
                Assert.AreEqual(1, still.Texture.mipmapCount);
                Assert.AreEqual(new Rect(0, 0, 1, 1), still.Uv);

                var yard = kind == AdamCinematicKind.ExodusOpening && cut == count - 1;
                var firstPage = art.TryGetFrame(timeline, true, viewport, out var initial);
                Assert.IsTrue(firstPage);
                Assert.IsFalse(initial.LiveTerrain);
                Assert.AreEqual(1920, initial.Texture.width);
                Assert.AreEqual(1, initial.Texture.mipmapCount);
                Assert.AreEqual(FilterMode.Point, initial.Texture.filterMode);

                AdvanceTo(timeline, start + 28 / 12f + .003f);
                Assert.IsTrue(art.TryGetFrame(timeline, true, viewport, out var secondPage));
                Assert.IsFalse(secondPage.LiveTerrain);
                if (yard)
                {
                    AdvanceTo(timeline, start + 3.6f);
                    Assert.IsTrue(art.TryGetFrame(timeline, true, viewport, out var revealing));
                    Assert.IsTrue(revealing.LiveTerrain);
                    Assert.IsFalse(revealing.FullTerrain);
                }
                AdvanceTo(timeline, cuts[cut].end - .016f);
                Assert.IsTrue(art.TryGetFrame(timeline, true, viewport, out var final));
                Assert.AreEqual(yard, final.LiveTerrain);
                Assert.AreEqual(yard, final.FullTerrain);
            }
        }
        finally { art.Clear(); }
    }

    [TestCase(null, 1920, 1080, null)]
    [TestCase("different-layout", 660, 440, null)]
    [TestCase(AdamGenesisCinematicMedia.YardLayoutV25 + "\n", 660, 440, null)]
    [TestCase(AdamGenesisCinematicMedia.YardLayoutV25, 1920, 1080, "1920x1080")]
    [TestCase(AdamGenesisCinematicMedia.YardLayoutV25, 660, 440, "660x440")]
    [TestCase(AdamGenesisCinematicMedia.YardLayoutV25, 960, 540, null)]
    public void YardVariantRequiresExactLivePropLayoutAndWindow(string layout, int width, int height, string suffix)
    {
        Assert.AreEqual(suffix, AdamGenesisCinematicMedia.YardVariant(layout, new Rect(0, 0, width, height)));
        if (suffix != null)
        {
            var page = Resources.Load<Texture2D>("GenesisCinematics/exodus-opening-media/cut-3-r10-" + suffix + "-page-1");
            Assert.NotNull(page);
            Assert.AreEqual(new Vector2(1920, 1350), new Vector2(page.width, page.height));
            Resources.UnloadAsset(page);
        }
    }

    static void AdvanceTo(AdamCinematic timeline, float elapsed)
    {
        while (timeline.Elapsed + .1f < elapsed) timeline.Advance(.1f);
        timeline.Advance(elapsed - timeline.Elapsed);
    }

}
