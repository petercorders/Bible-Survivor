using System;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Json;
using BibleSurvivor.Core;
using BibleSurvivor.Unity;
using NUnit.Framework;
using UnityEngine;
public sealed class AdamPolishWorldTests
{
 static byte[] Bytes(object v){using(var m=new MemoryStream()){new DataContractJsonSerializer(v.GetType()).WriteObject(m,v);return m.ToArray();}}
 [Test]public void OptionalNineAssetsKeepExactPlaybackAndPriorMandatoryBundles()
 {
  var m=AdamVisualQualityArt.Parse(Resources.Load<TextAsset>("VisualQuality02/manifest").bytes,"Polish02");Assert.AreEqual(9,m.assets.Length);Assert.AreEqual(3,AdamVisualQualityArt.Bundles.Length);
  foreach(var a in m.assets){var c=AdamVisualQualityArt.GetClip(a.id);Assert.AreEqual(a.frames,c.frames);Assert.AreEqual(a.fps,c.fps);Assert.AreEqual(a.loop,c.loop);var sprite=AdamVisualQualityArt.Sprite(a.id,0);Assert.AreEqual(new Vector2(a.pivot[0],a.pivot[1]),sprite.pivot);Assert.AreEqual(32,sprite.pixelsPerUnit);}
  Assert.AreEqual(3,AdamVisualQualityArt.GetClip("fx_hit-spark_hit_16").frames);Assert.IsFalse(AdamVisualQualityArt.TryGetClip("unapproved-polish-id",out _));
 }
 [Test]public void ThudUsesThreeLogicalFramesAndOnlyQuarterTurnsForAllEightSectors()
 {
  for(var sector=0;sector<8;sector++){var axis=new Vector2(Mathf.Cos(sector*45*Mathf.Deg2Rad),Mathf.Sin(sector*45*Mathf.Deg2Rad));Assert.AreEqual(sector,AdamPolishContacts.Sector(axis));Assert.AreEqual(sector%2*3,AdamPolishContacts.Frame(AdamPolishContacts.Thud,0,sector));Assert.AreEqual(2+sector%2*3,AdamPolishContacts.Frame(AdamPolishContacts.Thud,.249,sector));Assert.AreEqual(sector/2,AdamPolishContacts.QuarterTurns(sector));}
  Assert.AreEqual(.25,AdamPolishContacts.Duration(AdamPolishContacts.Thud));Assert.AreEqual(.15,AdamPolishContacts.Duration(AdamPolishContacts.Crack));Assert.AreEqual(.2,AdamPolishContacts.Duration(AdamPolishContacts.Page));
 }
 [Test]public void RealImpactsOnlyConsumeOnceAndPauseLowAndClearCannotInventOrReplayContacts()
 {
  var dir=Path.Combine(Path.GetTempPath(),"adam-polish-contact-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(dir);
  try
  {
   var host=AdamImpactTests.Fixture(Path.Combine(dir,"save.json"));using(var world=new AdamArtWorld())
   {
    world.Update(host,0);Assert.Zero(world.PolishContacts.Active(world.Clock));host.Tick(1d/60);world.Update(host,0);Assert.Greater(world.FrameImpacts.Count,0);Assert.Greater(world.PolishContacts.Active(world.Clock),0);
    var snapshot=host.World.Capture();var before=Bytes(snapshot);var disk=File.ReadAllBytes(Path.Combine(dir,"save.json"));var count=world.PolishContacts.Active(world.Clock);var contact=world.FrameImpacts.First(x=>x.TargetId>0);
    Assert.IsFalse(world.PolishContacts.Observe(contact,snapshot,true));Assert.AreEqual(count,world.PolishContacts.Active(world.Clock));CollectionAssert.AreEqual(before,Bytes(snapshot));CollectionAssert.AreEqual(disk,File.ReadAllBytes(Path.Combine(dir,"save.json")));
    host.SetPaused(true);world.Update(host,5);Assert.AreEqual(count,world.PolishContacts.Active(world.Clock));world.FeedbackEnabled=false;Assert.Greater(world.EnemyFlashOpacity(contact.TargetId),0,"lowFX keeps real enemy flash");world.Update(host,5);Assert.AreEqual(count,world.PolishContacts.Active(world.Clock));
    var late=host.World.Capture();late.Time+=1;Assert.IsFalse(AdamPolishContacts.TryRadialAxis(contact,late,out _),"later player position cannot supply a past contact direction");
    Assert.IsTrue(host.Reload());world.Update(host,0);Assert.Zero(world.PolishContacts.Active(world.Clock));world.Clear();Assert.Zero(world.PolishContacts.Active(world.Clock));
   }
  }
  finally{Directory.Delete(dir,true);}
 }
 [Test]public void SlingHistoryUsesActualMatchingIdsAndDoesNotAdvanceDuringRedrawOrPause()
 {
  var view=new AdamVisualQualityWorld();var s=new BattleSnapshot{Time=1};s.Projectiles.Add(new BattleProjectileState{Id=7,X=1,VelocityX=8,Lifetime=2});view.Observe(s);
  s=new BattleSnapshot{Time=2};s.Projectiles.Add(new BattleProjectileState{Id=7,X=2,VelocityX=8,Lifetime=1});view.Observe(s);Assert.IsTrue(view.TryAfterStone(7,1,out var old));Assert.AreEqual(new Vector2(1,0),old);Assert.IsFalse(view.TryAfterStone(99,1,out _));
  view.Observe(s);Assert.IsTrue(view.TryAfterStone(7,1,out old));Assert.AreEqual(new Vector2(1,0),old);view.Clear();Assert.IsFalse(view.TryAfterStone(7,1,out _));
 }
}
