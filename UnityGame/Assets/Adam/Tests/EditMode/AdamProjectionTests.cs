using BibleSurvivor.Unity;
using NUnit.Framework;
using UnityEngine;

public sealed class AdamProjectionTests
{
    [TestCase(1100,720)] [TestCase(660,440)] [TestCase(1101,721)] [TestCase(883,587)]
    public void ActualArenaAndOddEdgesShareAnIntegerSpriteAndCameraGrid(int width,int height)
    {
        var arena=AdamGame.Arena(width,height);var p=new AdamWorldProjection(arena,new Vector2(101.3f,-37.7f));
        Assert.AreEqual(new Rect(20,116,width-40,height-194),arena);
        Assert.GreaterOrEqual(p.TextureRect.width,arena.width);Assert.Less(p.TextureRect.width-arena.width,p.Scale);
        Assert.GreaterOrEqual(p.TextureRect.height,arena.height);Assert.Less(p.TextureRect.height-arena.height,p.Scale);
        var screen=p.Point(p.Origin.x,p.Origin.y);Assert.AreEqual(Mathf.Round(arena.width/2),screen.x);Assert.AreEqual(Mathf.Round(arena.height/2),screen.y);
        var right=p.Point(p.Origin.x+1,p.Origin.y);Assert.AreEqual(32*p.Scale,right.x-screen.x);
        Assert.Less(Mathf.Abs(((p.Origin.x-p.CameraPosition.x)*32+p.TextureWidth/2f)*p.Scale-screen.x),.001);
        Assert.Less(Mathf.Abs(((p.CameraPosition.y-p.Origin.y)*32+p.TextureHeight/2f)*p.Scale-screen.y),.001);
    }
}
