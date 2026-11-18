using System.Linq;
using BibleSurvivor.Unity;
using NUnit.Framework;
using UnityEngine;
public sealed class AdamBookGroundTests
{
 [Test]public void BookSelectionUsesCurrentRoundOnlyAndGenesisRemainsExact()
 {
  foreach(var round in new[]{1,9,16,0})Assert.IsFalse(AdamBookGround.Exodus(round));foreach(var round in Enumerable.Range(10,6))Assert.IsTrue(AdamBookGround.Exodus(round));
  for(var y=-5;y<=5;y++)for(var x=-8;x<=8;x++)
  {Assert.AreEqual(AdamVisualQualityWorld.Ground(x,y),AdamBookGround.Ground(x,y,false));var ground=AdamBookGround.Ground(x,y,true);Assert.IsTrue(ground.StartsWith("til_exodus_"));Assert.IsTrue(AdamVisualQualityArt.TryGetClip(ground,out _));}
  Assert.AreEqual("til_eden_path-16mask_static_32",AdamBookGround.Path(false));Assert.AreEqual("til_exodus_path-16mask_static_32",AdamBookGround.Path(true));
  Assert.AreEqual("dec_exodus_pebbles-1_11x8",AdamBookGround.Decor(0,0,true));Assert.AreEqual("dec_exodus_scrub-1_16x10",AdamBookGround.Decor(1,0,true));
 }
 [Test]public void ElevenReviewedClipsPreserveCellsPivotsAlphaAndMaskFrameOrder()
 {
  var manifest=AdamVisualQualityArt.Parse(Resources.Load<TextAsset>("VisualQuality03/manifest").bytes,"Book03");Assert.AreEqual(11,manifest.assets.Length);
  foreach(var a in manifest.assets)
  {var c=AdamVisualQualityArt.GetClip(a.id);Assert.AreEqual(a.frames,c.frames);Assert.Zero(c.fps);Assert.IsFalse(c.loop);var sprite=AdamVisualQualityArt.Sprite(c.id,0);Assert.AreEqual(new Vector2(a.pivot[0],a.pivot[1]),sprite.pivot);Assert.AreEqual(32,sprite.pixelsPerUnit);var alpha=sprite.texture.GetPixels32().Select(p=>(int)p.a).Distinct();if(a.alphaValues!=null)CollectionAssert.AreEquivalent(a.alphaValues,alpha);else CollectionAssert.IsSubsetOf(alpha,new[]{0,255});}
  var path=AdamVisualQualityArt.GetClip(AdamBookGround.Path(true));Assert.AreEqual(16,path.frames);for(var mask=0;mask<16;mask++)Assert.AreEqual(mask.ToString("D2"),AdamVisualQualityArt.Sprite(path.id,mask).name.Substring(path.id.Length+1));
  Assert.AreEqual(2,AdamVisualQualityArt.GetClip("ui_book-crest_static_32").frames);
 }
 [TestCase(620,246)][TestCase(1060,526)]public void EdgeBandsAreWindowPixelsDisjointAndNeverCoverTheCentre(float width,float height)
 {
  var arena=new Rect(20,116,width,height);var centre=new Rect(width*.28f,height*.22f,width*.44f,height*.56f);var bands=Enumerable.Range(0,3).SelectMany(i=>Enumerable.Range(0,4).Select(side=>AdamBookGround.Edge(arena,i,side))).ToArray();
  for(var i=0;i<bands.Length;i++){Assert.IsFalse(bands[i].Overlaps(centre));Assert.GreaterOrEqual(bands[i].xMin,0);Assert.LessOrEqual(bands[i].xMax,width);for(var j=0;j<i;j++)Assert.IsFalse(bands[i].Overlaps(bands[j]));}
  Assert.AreEqual(4,bands[0].height);Assert.AreEqual(6,bands[4].height);Assert.AreEqual(8,bands[8].height);
 }
 [Test]public void StaticHollowsHaveNoClockOrGameRandomAndUseTwoTileLattice()
 {
  for(var x=-3;x<4;x++)for(var y=-3;y<4;y++){var p=AdamBookGround.HollowPosition(x,y);Assert.AreEqual(p,AdamBookGround.HollowPosition(x,y));Assert.GreaterOrEqual(p.x,x*2+.5f);Assert.Less(p.x,x*2+1.5f);Assert.GreaterOrEqual(p.y,y*2+.5f);Assert.Less(p.y,y*2+1.5f);}
 }
}
