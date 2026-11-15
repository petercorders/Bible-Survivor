using System;
using System.Collections;
using System.IO;
using System.Linq;
using BibleSurvivor.Core;
using BibleSurvivor.Unity;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
public sealed class AdamR02VisualPlayTests
{
    [UnityTest] public IEnumerator SmallViewDrawsMixed50ActualR02Sprites()=>Render(660,440);
    [UnityTest] public IEnumerator LargeViewDrawsMixed40ActualR02Sprites()=>Render(1100,720);
    IEnumerator Render(int width,int height)
    {
        var dir=Path.Combine(Path.GetTempPath(),"adam-r02-visual-"+Guid.NewGuid().ToString("N"));var path=Path.Combine(dir,"save.json");var owner=new GameObject("R02 visual lifecycle regression");var game=owner.AddComponent<AdamGame>();game.AutoInitialize=false;game.enabled=false;
        var window=ScriptableObject.CreateInstance<PauseModalPointerWindow>();
        try
        {
            var host=Fixture(path,width==660?50:40);game.Initialize(host);game.SetLifecycle(true,false);host.SetPaused(false);game.UpdatePresentation(0);
            CollectionAssert.AreEquivalent(new[]{"b1","b2","b3"},host.Session.Wave.Active.Values.Select(x=>x.MonsterId).Distinct());
            window.Game=game;window.minSize=window.maxSize=new Vector2(width,height);window.position=new Rect(60,60,width,height);window.ShowUtility();window.Repaint();yield return null;
            game.SetLifecycle(true,false);host.SetPaused(false);
            var before=host.World.PlayerPosition;
            for(var i=0;i<10;i++){host.SetMovement(Vector2.right);host.Tick(.1);game.UpdatePresentation(.1f);window.Repaint();yield return null;}
            Assert.Greater(host.World.PlayerPosition.X,before.X);Assert.AreEqual("walk",game.ArtWorld.PlayerMotion);Assert.IsFalse(game.ArtWorld.PlayerLeft);
            host.SetMovement(Vector2.left);host.Tick(.1);game.UpdatePresentation(.1f);Assert.IsTrue(game.ArtWorld.PlayerLeft);window.Repaint();yield return null;
            game.ArtWorld.FeedbackEnabled=false;game.UpdatePresentation(0);Assert.Zero(game.ImpactParticles.ParticleCount);window.Repaint();yield return null;
            game.ArtWorld.FeedbackEnabled=true;host.SetPaused(true);var time=host.World.SimulationTime;var clock=game.ArtWorld.Clock;
            game.UpdatePresentation(.1f);window.Repaint();yield return null;Assert.AreEqual(time,host.World.SimulationTime);Assert.AreEqual(clock,game.ArtWorld.Clock);
            Assert.IsTrue(host.Reload(),host.Error);game.UpdatePresentation(0);Assert.Zero(game.ArtWorld.ActiveEffects);Assert.Zero(game.ImpactParticles.ParticleCount);
        }
        finally{window.Close();UnityEngine.Object.Destroy(owner);}
        yield return null;if(Directory.Exists(dir))Directory.Delete(dir,true);
    }
    static AdamHost Fixture(string path,int count)
    {
        var catalog=AdamCatalog.Load();var store=new SaveStore(path);var host=new AdamHost(catalog,store);host.Start("jawbone");host.Session.Advance(180);host.SetPaused(true);host.SetPaused(false);
        for(var pass=0;pass<8;pass++)
        {
            host.Session.Advance(pass==0?80:0);if(host.Session.Wave.Active.Values.Any(x=>x.MonsterId=="b2"))break;
            var keep=host.Session.Wave.Active.Values.GroupBy(x=>x.MonsterId).Select(g=>g.First().Id).ToArray();foreach(var id in host.Session.Wave.Active.Keys.Where(x=>!keep.Contains(x)).ToArray())host.Session.Kill(id);
        }
        while(host.Session.Wave.Active.Count>count){var id=host.Session.Wave.Active.Values.GroupBy(x=>x.MonsterId).OrderByDescending(g=>g.Count()).First().Last().Id;host.Session.Kill(id);}
        host.Session.Capture();var state=host.State;var world=new BattleWorld(host.Session,catalog.Speeds,7).Capture();var i=0;
        foreach(var enemy in world.Enemies){enemy.X=(i%10-5)*.6f;enemy.Y=(i/10-2)*.65f;i++;}foreach(var id in host.Session.DroppedExperience.Keys)world.Drops.Add(new BattleEntityState{Id=id,X=100,Y=100});state.Run.World=world;store.Commit(state,state.Revision,Guid.NewGuid().ToString("N"));Assert.IsTrue(host.Reload(),host.Error);return host;
    }
}
