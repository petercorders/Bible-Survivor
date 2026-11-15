using System;
using System.Linq;
using BibleSurvivor.Core;
using BibleSurvivor.Unity;
using NUnit.Framework;
using UnityEngine;
public sealed class AdamEnemyDepthTests
{
    [TestCase(660,440)] [TestCase(1100,720)] public void DrawOrderUsesAscendingProjectedFeetThenIdWithoutChangingCapture(int width,int height)
    {
        var input=new[]{new BattleEntityState{Id=1,X=0,Y=-1},new BattleEntityState{Id=2,X=0,Y=1},new BattleEntityState{Id=3,X=0,Y=0}};var view=new AdamArtWorld();
        var projection=new AdamWorldProjection(AdamGame.Arena(width,height),Vector2.zero);var sorted=view.PrepareEnemySprites(input,projection);
        CollectionAssert.AreEqual(new[]{2,3,1},sorted.Select(x=>x.Enemy.Id));CollectionAssert.AreEqual(new[]{1,2,3},input.Select(x=>x.Id));
        for(var i=0;i<sorted.Count;i++)Assert.AreEqual(projection.Point(sorted[i].Enemy.X,sorted[i].Enemy.Y),sorted[i].Feet);
        var tied=new[]{new BattleEntityState{Id=2,Y=.001f},new BattleEntityState{Id=1,Y=-.001f}};
        sorted=view.PrepareEnemySprites(tied,projection);Assert.AreEqual(sorted[0].Feet.y,sorted[1].Feet.y);CollectionAssert.AreEqual(new[]{1,2},sorted.Select(x=>x.Enemy.Id),"rounded screen ties use ID, not unrounded world Y");
    }
    [Test] public void SortingReusesBufferAndDoesNotAllocateAfterWarmupOrChangeInput()
    {
        var input=Enumerable.Range(1,120).Select(id=>new BattleEntityState{Id=id,X=id%7,Y=id*.01f}).ToArray();var view=new AdamArtWorld();var projection=new AdamWorldProjection(AdamGame.Arena(1100,720),Vector2.zero);
        var buffer=view.PrepareEnemySprites(input,projection);for(var i=0;i<10;i++)Assert.AreSame(buffer,view.PrepareEnemySprites(input,projection));
        var before=GC.GetAllocatedBytesForCurrentThread();for(var i=0;i<1000;i++)view.PrepareEnemySprites(input,projection);var allocated=GC.GetAllocatedBytesForCurrentThread()-before;
        Assert.Zero(allocated,"sorting/projection alone must not add per-frame managed allocations");CollectionAssert.AreEqual(Enumerable.Range(1,120),input.Select(x=>x.Id));
        view.Clear();Assert.Zero(buffer.Count);Assert.AreSame(buffer,view.PrepareEnemySprites(input,projection));
    }
}
