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
}
