using System;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Json;
using BibleSurvivor.Core;
using BibleSurvivor.Unity;
using NUnit.Framework;
using UnityEngine;
public sealed class AdamJawboneTests
{
    static byte[] Bytes(object value){using(var stream=new MemoryStream()){new DataContractJsonSerializer(value.GetType()).WriteObject(stream,value);return stream.ToArray();}}
    [TestCase(660,440)][TestCase(1100,720)]
    public void GroupRotationKeepsRegisteredHandAtTheActualWindowPivot(int width,int height)
    {
        var arena=AdamGame.Arena(width,height);var hand=new Vector2(arena.width/2+7,arena.height/2-12);var global=hand+arena.position;
        var matrix=AdamJawboneSwing.Matrix(arena,hand,Vector2.up,.03);
        Assert.Less(((Vector2)matrix.MultiplyPoint3x4(global)-global).magnitude,.001,"GUI.matrix uses window coordinates inside BeginGroup");
    }
    [TestCase(660,440)][TestCase(1100,720)]
    public void LeftActorReflectionKeepsActualWindowFeetBesideItsRegisteredHand(int width,int height)
    {
        var arena=AdamGame.Arena(width,height);var feet=new Vector2(arena.width/2,arena.height/2);var actual=feet+arena.position;
        var matrix=AdamArtWorld.FacingMatrix(actual,Vector2.left);
        Assert.Less(((Vector2)matrix.MultiplyPoint3x4(actual)-actual).magnitude,.001);
        var rightHand=AdamJawboneSwing.Hand(feet,2,false)+arena.position;
        Assert.AreEqual(AdamJawboneSwing.Hand(feet,2,true)+arena.position,(Vector2)matrix.MultiplyPoint3x4(rightHand));
    }
    [Test]
    public void BurstDirectionStaysLockedWhileHandFollowsMovementAndPauseWithoutChangingSnapshot()
    {
        var state=new BattleSnapshot{Time=1,FacingX=1,FacingY=0};
        state.Weapons.Add(new BattleWeaponState{WeaponId="jawbone",ActiveSince=1});var original=Bytes(state);
        var swing=new AdamJawboneSwing();swing.Observe(state,1,.12);CollectionAssert.AreEqual(original,Bytes(state));
        state.Time=1.08;state.FacingX=0;state.FacingY=1;state.PlayerX=2;swing.Observe(state,1,.12);
        Assert.AreEqual(Vector2.right,swing.Direction);Assert.AreEqual(1,swing.Started);Assert.IsTrue(swing.Active(state.Time));
        var a=AdamJawboneSwing.Hand(new Vector2(100,100),2,false);var b=AdamJawboneSwing.Hand(new Vector2(120,90),2,false);Assert.AreEqual(new Vector2(20,-10),b-a);
        var angle=AdamJawboneSwing.Angle(state.Time-swing.Started);swing.Observe(state,1,.12);Assert.AreEqual(angle,AdamJawboneSwing.Angle(state.Time-swing.Started));
        state.Time=2;state.Weapons[0].ActiveSince=2;swing.Observe(state,1,.12);Assert.AreEqual(Vector2.up,swing.Direction);
        Assert.IsFalse(swing.Active(2.21));swing.Clear();Assert.IsFalse(swing.Active(2));
    }
    [Test]
    public void FractionalSpriteGripMatchesHandRatherThanCentreForEveryDirection()
    {
        var texture=new Texture2D(32,32);var sprite=Sprite.Create(texture,new Rect(0,0,32,32),new Vector2(7.5f/32,11.5f/32),32);
        try
        {
            var arena=new Rect(20,116,620,246);var hand=new Vector2(300,100);var rect=AdamJawboneSwing.GripRect(sprite,hand,2);
            Assert.AreEqual(hand.x,rect.x+sprite.pivot.x*2);Assert.AreEqual(hand.y,rect.y+(32-sprite.pivot.y)*2);Assert.AreEqual(7.5,sprite.pivot.x);
            foreach(var direction in new[]{Vector2.right,Vector2.up,Vector2.left,Vector2.down})
                Assert.Less(((Vector2)AdamJawboneSwing.Matrix(arena,hand,direction,.08).MultiplyPoint3x4(hand+arena.position)-(hand+arena.position)).magnitude,.001);
        }
        finally{UnityEngine.Object.DestroyImmediate(sprite);UnityEngine.Object.DestroyImmediate(texture);}
    }
    [Test]
    public void ReviewedSidecarRetainsFractionalGripAndOnlyOverridesJawboneIcon()
    {
        var art=new AdamArt();Assert.AreEqual(new Vector2(7.5f,11.5f),art.JawboneClub.pivot);
        Assert.AreEqual(AdamJawboneArt.Held+"_00",art.JawboneClub.name);
        Assert.AreEqual(AdamJawboneArt.Icon+"_00",art.Sprite("ico_wpn_jawbone_static_16",0).name);
        Assert.AreEqual("wpn_jawbone_held_16_00",art.Sprite("wpn_jawbone_held_16",0).name);
        Assert.AreEqual(32,art.JawboneClub.rect.width);Assert.AreEqual(16,art.Sprite("ico_wpn_jawbone_static_16",0).rect.width);
    }
    [Test]
    public void RealHitsOnlyDriveAudioParticlesAndFeedbackDoesNotWriteWorldOrSave()
    {
        var dir=Path.Combine(Path.GetTempPath(),"adam-jaw-feedback-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(dir);var owner=new GameObject("jaw audio test");
        try
        {
            var path=Path.Combine(dir,"save.json");var host=AdamImpactTests.Fixture(path);
            var store=new SaveStore(path);var state=store.Load();state.Run.World.Weapons.Single(w=>w.WeaponId=="jawbone").NextCycle=0;store.Commit(state,state.Revision,"actual-jawbone-schedule");Assert.IsTrue(host.Reload());host.SetPaused(false);
            var ownsListener=UnityEngine.Object.FindFirstObjectByType<AudioListener>()==null;
            using(var view=new AdamArtWorld())using(var audio=new AdamImpactAudio(owner))using(var particles=new AdamImpactParticles())
            {
                view.Update(host,0);var initial=audio.PlayedCount;audio.Update(host,view);Assert.AreEqual(initial,audio.PlayedCount);Assert.Zero(particles.EmittedImpacts);
                host.Tick(1d/60);view.Update(host,0);Assert.IsTrue(view.FrameImpacts.Any(i=>i.Cause=="jawbone"));var world=Bytes(host.World.Capture());var disk=File.ReadAllBytes(Path.Combine(dir,"save.json"));
                audio.Update(host,view);particles.Update(host,view,1f/60);Assert.Greater(audio.PlayedCount,0);Assert.Greater(particles.EmittedImpacts,0);
                var played=audio.PlayedCount;audio.Update(host,view);Assert.AreEqual(played,audio.PlayedCount);
                view.FeedbackEnabled=false;particles.Update(host,view,1f/60);Assert.Zero(particles.ParticleCount);audio.Muted=true;audio.Update(host,view);Assert.Zero(audio.ActiveVoices);
                CollectionAssert.AreEqual(world,Bytes(host.World.Capture()));CollectionAssert.AreEqual(disk,File.ReadAllBytes(Path.Combine(dir,"save.json")));
                // Existing runtime-only Dispose schedules destruction; EditMode reports each owned object.
                for(var i=0;i<13+(ownsListener?1:0);i++)UnityEngine.TestTools.LogAssert.Expect(LogType.Error,new System.Text.RegularExpressions.Regex(".*Destroy may not be called from edit mode!.*"));
            }
        }
        finally{UnityEngine.Object.DestroyImmediate(owner);Directory.Delete(dir,true);}
    }
}
