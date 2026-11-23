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
}
