using System;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Json;
using BibleSurvivor.Core;
using BibleSurvivor.Unity;
using NUnit.Framework;
using UnityEngine;
public sealed class AdamVisualQualityWorldTests
{
 static byte[] Bytes(object v){using(var s=new MemoryStream()){new DataContractJsonSerializer(v.GetType()).WriteObject(s,v);return s.ToArray();}}
 [Test]public void ReviewedOverlayKeepsRegistrationAndUsesAsymmetricBottomKeycapBorder()
 {
  var art=new AdamArt();var original=JsonUtility.FromJson<AdamArtIndex>(Resources.Load<TextAsset>("Art03/index").text);var count=0;
  foreach(var bundle in AdamVisualQualityArt.Bundles)foreach(var id in AdamVisualQualityArt.Ids(bundle))
  {
   count++;Assert.IsTrue(AdamVisualQualityArt.TryGetClip(id,out var c));Assert.AreSame(c,art.Clip(id));Assert.AreSame(AdamVisualQualityArt.Sprite(id,0),art.Sprite(id,0));
   foreach(var pixel in art.Sprite(id,0).texture.GetPixels32())Assert.Contains((int)pixel.a,c.alphaValues,id);
   var old=original.assets.SingleOrDefault(x=>x.id==id);if(old!=null){Assert.AreEqual(old.frames,c.frames);Assert.AreEqual(old.fps,c.fps);Assert.AreEqual(old.loop,c.loop);CollectionAssert.AreEqual(old.borders,c.borders);CollectionAssert.AreEqual(old.attachX,c.attachX);CollectionAssert.AreEqual(old.attachY,c.attachY);}
  }
  Assert.AreEqual(31,count);CollectionAssert.AreEqual(new[]{3,4,3,3},AdamVisualQualityArt.GetClip("ui_keycap_9slice_12").borders);
  Assert.AreEqual(new Vector2(7.5f,11.5f),art.JawboneClub.pivot);Assert.AreEqual(79,art.Index.assets.Length);
 }
 [Test]public void LinearGuiPresentationCancelsBothObservedGammaEncodesAndPreservesAlpha()
 {
  var source=(Color)new Color32(28,34,45,77);var display=AdamVisualQualityArt.PresentationPixel(source,true);
  Assert.AreEqual(source.r,Mathf.LinearToGammaSpace(Mathf.LinearToGammaSpace(display.r)),.0001);Assert.AreEqual(source.g,Mathf.LinearToGammaSpace(Mathf.LinearToGammaSpace(display.g)),.0001);Assert.AreEqual(source.a,display.a);
  Assert.AreEqual(source,AdamVisualQualityArt.PresentationPixel(source,false));
  Assert.AreEqual(source.r,Mathf.LinearToGammaSpace(source.linear.r),.0001);
 }
 [Test]public void ActualProjectileBirthOnlyDrivesReleaseAndPauseClearDoNotCreateShots()
 {
  var snapshot=new BattleSnapshot{Time=10};snapshot.Projectiles.Add(new BattleProjectileState{Id=1,VelocityX=8,Lifetime=1});var view=new AdamVisualQualityWorld();var bytes=Bytes(snapshot);view.Observe(snapshot);Assert.IsFalse(view.SlingReleasing(10));CollectionAssert.AreEqual(bytes,Bytes(snapshot));
  snapshot.Time=10.1;snapshot.Projectiles.Add(new BattleProjectileState{Id=2,VelocityY=8,Lifetime=1});view.Observe(snapshot);Assert.IsTrue(view.SlingReleasing(10.1));Assert.AreEqual(Vector2.up,view.SlingDirection);var at=view.SlingReleasedAt;
  view.Observe(snapshot);Assert.AreEqual(at,view.SlingReleasedAt);Assert.IsTrue(view.SlingReleasing(10.1));Assert.IsFalse(view.SlingReleasing(10.23));view.Clear();Assert.IsFalse(view.SlingReleasing(10.1));
  Assert.LessOrEqual(AdamVisualQualityWorld.TrailPixels(new BattleProjectileState{VelocityX=10000},2),40);Assert.Zero(AdamVisualQualityWorld.TrailPixels(new BattleProjectileState(),2));
 }
 [TestCase(660,440)][TestCase(1100,720)]public void SwingEffectsConsumeActualAttachAndKeepWindowGripAndCanopyOutsideCentre(int width,int height)
 {
  var art=new AdamArt();var arena=AdamGame.Arena(width,height);var hand=new Vector2(100,80);var global=hand+arena.position;
  foreach(var id in new[]{"fx_jawbone-swing-trail_hit_64","fx_jawbone-swing-edge_hit_64"})
  {var c=art.Clip(id);for(var frame=0;frame<c.frames;frame++){var m=AdamVisualQualityWorld.SwingFxMatrix(arena,hand,Vector2.left,.08,c,frame,2);Assert.Less(((Vector2)m.MultiplyPoint3x4(global)-global).magnitude,.001);Assert.Greater(AdamVisualQualityWorld.SwingAttach(c,frame).magnitude,0);}}
  var centre=new Rect(arena.width*.2f,arena.height*.2f,arena.width*.6f,arena.height*.6f);foreach(var rect in AdamVisualQualityWorld.CanopyRects(arena,2))Assert.IsFalse(rect.Overlaps(centre));
 }
 [Test]public void DirectionalBoneChipsAreBoundedAndAllThreeOriginalPcmsAreDistinct()
 {
  Assert.AreEqual(6,AdamImpactParticles.ChipCount(false));Assert.AreEqual(10,AdamImpactParticles.ChipCount(true));
  for(var i=0;i<6;i++){var v=AdamImpactParticles.ChipVelocity(Vector2.up,i,6,.75f);Assert.LessOrEqual(Vector2.Angle(Vector2.up,v),50.001);Assert.AreEqual(.75,v.magnitude,.0001);}
  foreach(var weapon in AdamImpactAudio.Weapons){var data=AdamImpactAudio.Samples(weapon);Assert.Zero(data[0]);Assert.Greater(data.Max(v=>Math.Abs(v)),.1);Assert.IsTrue(data.All(v=>!float.IsNaN(v)&&Math.Abs(v)<1));}
  Assert.AreNotEqual(AdamImpactAudio.Samples("bible")[100],AdamImpactAudio.Samples("sling")[100]);
 }
}
