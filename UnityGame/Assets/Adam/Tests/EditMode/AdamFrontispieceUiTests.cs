using System;
using System.IO;
using BibleSurvivor.Core;
using BibleSurvivor.Unity;
using NUnit.Framework;
using UnityEngine;

public sealed class AdamFrontispieceUiTests
{
    [Test] public void NoSessionHasGenesisEmptyPipsAndNoCount()
    {
        Assert.AreEqual("창세기",AdamBookTitle.Name(null));Assert.Zero(AdamBookTitle.Local(null));
        Assert.AreEqual(9,AdamBookTitle.Total(null));
        for(var i=0;i<9;i++)Assert.Zero(AdamBookTitle.Pip(null,i));
    }
    [TestCase(3,0,3,9)][TestCase(9,0,9,9)][TestCase(10,1,1,6)][TestCase(12,1,3,6)][TestCase(15,1,6,6)]
    [TestCase(16,2,1,5)][TestCase(18,2,3,5)][TestCase(20,2,5,5)]
    public void RealRoundDeterminesBookAndCurrentRoundIsNeverMarkedCleared(int round,int book,int local,int total)
    {
        Assert.AreEqual(book,AdamBookTitle.Book(round));Assert.AreEqual(local,AdamBookTitle.Local(round));Assert.AreEqual(total,AdamBookTitle.Total(round));
        for(var i=0;i<total;i++)Assert.AreEqual(i<local-1?1:i==local-1?2:0,AdamBookTitle.Pip(round,i));
        for(var i=0;i<total;i++)Assert.AreEqual(i==2||i==5&&book<2||i==8&&book==0,AdamBookTitle.Boss(book,i));
        Assert.AreEqual(Math.Min(book,1),AdamBookTitle.ArtFrame(round));
    }
    [TestCase(660,-140,520,180,136)][TestCase(1100,300,960,400,150)][TestCase(1400,600,1260,550,150)]
    public void HorizonUsesRightAnchoredTwoTimesScaleAndRibbonIsCentered(float width,float origin,float focal,float ribbonX,float ribbonY)
    {
        Assert.AreEqual(origin,AdamBookTitle.HorizonX(width));Assert.AreEqual(focal,AdamBookTitle.FocalX(width));
        Assert.AreEqual(new Rect(ribbonX,ribbonY,300,68),AdamBookTitle.Ribbon(width));
        var local=AdamBookTitle.Ribbon(width);local.position-=AdamGame.Arena(width,440).position;
        Assert.AreEqual(new Vector2(ribbonX-20,ribbonY-116),local.position);
        Assert.LessOrEqual(96+(AdamBookTitle.Total(null)-1)*18+12,300);
    }
    [Test] public void LoadedIdentityWithoutLocalTransitionNeverCreatesASortie()
    {
        var ribbon=new AdamSortieRibbon();var key=AdamSortieRibbon.Identity("loaded",10);
        ribbon.Advance(key,true,false,false,.1);Assert.IsFalse(ribbon.Visible(key,true));
        ribbon.Queue(key);ribbon.Reset(); // Initialize/reload cancels an unplayed local transition.
        ribbon.Advance(key,true,false,false,.1);Assert.IsFalse(ribbon.Visible(key,true));
    }
    [Test] public void FailedEntryWaitsForActualPlayAndPauseOrBossCannotReplayIt()
    {
        var ribbon=new AdamSortieRibbon();var key=AdamSortieRibbon.Identity("run",10);
        ribbon.Queue(key);ribbon.Advance(key,false,true,false,5);Assert.IsFalse(ribbon.Visible(key,true));
        ribbon.Advance(key,true,false,false,0);Assert.IsTrue(ribbon.Visible(key,true));
        ribbon.Advance(key,false,true,false,.1);ribbon.Advance(key,true,false,false,.1);Assert.IsFalse(ribbon.Visible(key,true));
        ribbon.Queue(key);ribbon.Advance(key,true,false,false,.1);Assert.IsFalse(ribbon.Visible(key,true));
        var next=AdamSortieRibbon.Identity("run",11);ribbon.Queue(next);ribbon.Advance(next,true,false,false,0);Assert.IsTrue(ribbon.Visible(next,true));
        ribbon.Advance(next,true,false,true,.1);Assert.IsFalse(ribbon.Visible(next,true));
    }
    [Test] public void LowEffectsRibbonIsStaticAndExpiresAtPointEightWithoutReappearing()
    {
        var ribbon=new AdamSortieRibbon();var key=AdamSortieRibbon.Identity("run",1);
        ribbon.Queue(key);ribbon.Advance(key,true,false,false,0,false);
        Assert.AreEqual(1,AdamSortieRibbon.Extent(0,false));Assert.AreEqual(1,AdamSortieRibbon.Extent(.79,false));
        ribbon.Advance(key,true,false,false,.8,false);Assert.IsFalse(ribbon.Visible(key,false));Assert.IsFalse(ribbon.Visible(key,true));
    }
}
