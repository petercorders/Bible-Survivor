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

    [Test]
    public void ElevenFilmsHaveTwelveIndependentRoundBoundaries()
    {
        var entries = new[] { 1, 10, 43, 55, 67, 94, 121, 151, 166, 172 };
        var settlements = new[] { 9, 180 };
        var resources = new System.Collections.Generic.HashSet<string>();
        var films = new System.Collections.Generic.HashSet<AdamCinematicKind>();
        var kinds = new System.Collections.Generic.HashSet<AdamCinematicKind>();
        for (var round = 1; round <= 180; round++)
        foreach (var boundary in new[] { AdamCinematicBoundary.Entry, AdamCinematicBoundary.Settlement })
        {
            var spec = AdamCinematicSpec.Find(round, boundary);
            var expected = System.Array.IndexOf(boundary == AdamCinematicBoundary.Entry ? entries : settlements, round) >= 0;
            Assert.AreEqual(expected, spec != null, $"R{round} {boundary}");
            if (spec == null) continue;
            Assert.IsTrue(resources.Add(spec.Resource), spec.Resource);
            Assert.IsTrue(kinds.Add(spec.Kind), spec.Kind.ToString());
            films.Add(spec.Film);
            Assert.IsNull(AdamCinematic.Read(spec.Kind, null));
            var script = Resources.Load<TextAsset>(spec.Resource);
            Assert.IsNotNull(script, spec.Resource);
            var playback = new AdamCinematic();
            Assert.IsTrue(playback.Begin(spec.Kind, script.text), spec.Resource);
            var cuts = new System.Collections.Generic.HashSet<AdamCinematic.Cut>();
            for (var frame = 0; frame < 240 && playback.Active; frame++)
            {
                cuts.Add(playback.Current);
                playback.Advance(.1f);
            }
            Assert.IsFalse(playback.Active, spec.Resource);
            Assert.AreEqual(spec.CutCount, cuts.Count);
        }
        Assert.AreEqual(11, films.Count);
        Assert.AreEqual(12, resources.Count);
        Assert.AreEqual(12, AdamCinematicSpec.Segments.Count);
        Assert.AreEqual(AdamCinematicSpec.Find(9, AdamCinematicBoundary.Settlement).Film,
            AdamCinematicSpec.Find(10, AdamCinematicBoundary.Entry).Film);
    }

    [TestCase(1920, 1080, 245)]
    [TestCase(1440, 810, 245)]
    [TestCase(960, 540, 245)]
    [TestCase(660, 440, 255)]
    public void SubtitlesLeaveDesignSubjectsAboveTheOverlay(int width, int height, int firstCoveredRow)
    {
        var viewport = new Rect(0, 0, width, height);
        var frame = AdamCinematic.Frame(viewport);
        var scale = frame.width / AdamCinematic.Width;
        var subtitleTop = viewport.yMax - AdamGame.SubtitleHeight(viewport, frame);
        Assert.AreEqual(firstCoveredRow, (subtitleTop - frame.yMin) / scale);
    }

    [TestCase(false)]
    [TestCase(true)]
    public void CreationConfirmsSavedBattleAndNeverReplaysOnReload(bool preexistingClear)
    {
        var directory = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "adam-creation-" + System.Guid.NewGuid().ToString("N"));
        var path = System.IO.Path.Combine(directory, "save.json");
        try
        {
            var store = new BibleSurvivor.Core.SaveStore(path);
            if (preexistingClear)
            {
                var previous = BibleSurvivor.Core.GameSnapshot.New();
                previous.OldTestamentClear = new BibleSurvivor.Core.OldTestamentClear
                    { RunId = "legacy-180", CompletedAtUnixMilliseconds = 1 };
                previous.Profile.Gold = 123;
                store.Commit(previous, 0, "legacy-profile");
            }

            var host = new AdamHost(AdamCatalog.Load(), store);
            host.Start("bible", showCreation: true);
            Assert.IsNull(host.Error);
            Assert.AreEqual(preexistingClear ? AdamCinematicKind.None : AdamCinematicKind.Creation, host.TakeSavedCinematic());
            Assert.AreEqual(AdamCinematicKind.None, host.TakeSavedCinematic());
            var runId = host.State.Run.RunId;
            var bytes = System.IO.File.ReadAllBytes(path);
            host = new AdamHost(AdamCatalog.Load(), store);
            Assert.AreEqual(AdamCinematicKind.None, host.TakeSavedCinematic());
            Assert.AreEqual(runId, host.State.Run.RunId);
            Assert.AreEqual(preexistingClear ? 123 : 0, host.State.Profile.Gold);
            Assert.AreEqual(preexistingClear ? "legacy-180" : null, host.State.OldTestamentClear?.RunId);
            Assert.Zero(host.World.SimulationTime);
            CollectionAssert.AreEqual(bytes, System.IO.File.ReadAllBytes(path));
        }
        finally
        {
            if (System.IO.Directory.Exists(directory)) System.IO.Directory.Delete(directory, true);
        }
    }

    [TestCase(43)]
    [TestCase(55)]
    [TestCase(67)]
    [TestCase(94)]
    [TestCase(121)]
    [TestCase(151)]
    [TestCase(166)]
    [TestCase(172)]
    [TestCase(180)]
    public void ApprovedCuratedArtLoadsEveryPageAndReducedMotionStill(int round)
    {
        var spec = AdamCinematicSpec.Find(round, round == 180 ? AdamCinematicBoundary.Settlement : AdamCinematicBoundary.Entry);
        var timeline = new AdamCinematic();
        Assert.IsTrue(timeline.Begin(spec.Kind, Resources.Load<TextAsset>(spec.Resource).text));
        var renderer = new AdamCuratedCinematicArt();
        try
        {
            renderer.Begin(spec, timeline);
            Assert.IsFalse(renderer.Preview);
            var seen = new System.Collections.Generic.HashSet<string>();
            for (var i = 0; i < spec.CutCount; i++)
            {
                for (var frame = 0; timeline.Active && timeline.CurrentIndex == i; frame++)
                {
                    Assert.IsTrue(renderer.TryGetFrame(timeline, true, out var art), $"R{round} cut {i} frame {frame}");
                    if (seen.Add(art.Texture.name))
                    {
                        Assert.AreEqual(FilterMode.Point, art.Texture.filterMode);
                        Assert.AreEqual(1, art.Texture.mipmapCount);
                        Assert.IsFalse(art.Texture.isReadable);
                    }
                    if (frame == 0)
                    {
                        Assert.IsTrue(renderer.TryGetFrame(timeline, false, out var still));
                        Assert.IsFalse(still.LiveTerrain);
                        Assert.AreEqual(new Rect(0, 0, 1, 1), still.Uv);
                    }
                    timeline.Advance(.1f);
                }
            }
            Assert.GreaterOrEqual(seen.Count, 6);
        }
        finally { renderer.Clear(); }
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
