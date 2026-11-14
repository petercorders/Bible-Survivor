using System;
using System.Collections;
using System.IO;
using System.Linq;
using BibleSurvivor.Core;
using BibleSurvivor.Unity;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public sealed class AdamParticlePlayTests
{
    static AdamHost Fixture(string path,int projectileCount=2,bool lethalLast=false)
    {
        var store=new SaveStore(path);var host=new AdamHost(AdamCatalog.Load(),store);host.Start("sling");
        for(var i=0;i<20 && host.World.Capture().Enemies.Count<2;i++)host.Tick(.1);
        host.Checkpoint();var state=store.Load();var snapshot=state.Run.World;snapshot.PlayerX=100;snapshot.PlayerY=70;
        foreach(var enemy in snapshot.Enemies){enemy.X=101;enemy.Y=70;}
        snapshot.Projectiles.Clear();snapshot.NextProjectileId=projectileCount+1;
        for(var i=1;i<=projectileCount;i++)snapshot.Projectiles.Add(new BattleProjectileState{Id=i,X=100.8f,Y=70,VelocityX=100,Lifetime=1,Damage=lethalLast && i==projectileCount?1000:.001,RemainingPierce=1,Radius=.1});
        snapshot.Weapons.Clear();snapshot.Weapons.Add(new BattleWeaponState{WeaponId="sling",NextCycle=100});
        store.Commit(state,state.Revision,Guid.NewGuid().ToString("N"));Assert.IsTrue(host.Reload(),host.Error);host.SetPaused(false);return host;
    }
    [UnityTest] public IEnumerator ActualHitsRenderAtWorldPositionOnceAndPoolPauseLowReloadDisposeStayBounded()
    {
        var directory=Path.Combine(Path.GetTempPath(),"adam-particles-"+Guid.NewGuid().ToString("N"));var main=new GameObject("particle mask test").AddComponent<Camera>();
        var oldMask=main.cullingMask;var host=Fixture(Path.Combine(directory,"save.json"));var view=new AdamArtWorld();var particles=new AdamImpactParticles();
        try
        {
            view.Update(host,0);particles.Update(host,view,0);host.Tick(1d/60);view.Update(host,0);particles.Update(host,view,0);
            Assert.Greater(particles.EmittedImpacts,0);Assert.LessOrEqual(particles.ParticleCount,AdamImpactParticles.Capacity);
            var emitted=particles.EmittedImpacts;var count=particles.ParticleCount;particles.Update(host,view,0);Assert.AreEqual(emitted,particles.EmittedImpacts,"same batch cannot replay");Assert.AreEqual(count,particles.ParticleCount);
            Assert.Zero(main.cullingMask&(1<<AdamImpactParticles.OverlayLayer));
            Assert.AreEqual(256,particles.OverlayCamera.transform.parent.GetComponentsInChildren<ParticleSystem>().Sum(p=>p.main.maxParticles),"real engine pool has exactly256 capacity");
            var projection=new AdamWorldProjection(AdamGame.Arena(1100,720),new Vector2(view.Snapshot.PlayerX,view.Snapshot.PlayerY));particles.Prepare(projection);
            Assert.AreEqual(FilterMode.Point,particles.Texture.filterMode);Assert.AreEqual(1,particles.Texture.antiAliasing);
            var v=particles.OverlayCamera.WorldToViewportPoint(new Vector3(101,70,0));var expected=projection.Point(101,70);
            Assert.Less(Mathf.Abs(v.x*projection.TextureRect.width-expected.x),1,"actual particle camera must share sprite projection");
            yield return null;yield return null;
            var previous=RenderTexture.active;RenderTexture.active=particles.Texture;var image=new Texture2D(particles.Texture.width,particles.Texture.height,TextureFormat.RGBA32,false);
            image.ReadPixels(new Rect(0,0,image.width,image.height),0,0);image.Apply();RenderTexture.active=previous;
            Assert.IsTrue(image.GetPixels32().Any(p=>p.a>10),"real particles at world100/70 must render: zero automatic bounds used to cull them");UnityEngine.Object.Destroy(image);
            host.SetPaused(true);particles.Update(host,view,.1f);Assert.AreEqual(count,particles.ParticleCount);host.SetPaused(false);
            host.SetSuspended(true);particles.Update(host,view,.1f);Assert.AreEqual(count,particles.ParticleCount);host.SetSuspended(false);
            view.FeedbackEnabled=false;particles.Update(host,view,0);Assert.Zero(particles.ParticleCount);view.FeedbackEnabled=true;particles.Update(host,view,0);Assert.Zero(particles.ParticleCount,"low effects cannot backlog replay");
            host.Checkpoint();host.Reload();view.Update(host,0);particles.Update(host,view,0);Assert.Zero(particles.ParticleCount);
            var crowded=Fixture(Path.Combine(directory,"crowded.json"),41,true);view.Update(crowded,0);particles.Update(crowded,view,0);crowded.Tick(1d/60);view.Update(crowded,0);particles.Update(crowded,view,0);
            Assert.Greater(particles.Dropped,0);Assert.AreEqual(72,particles.ParticleCount,"lethal burst must replace a lower priority burst in a saturated pool");Assert.LessOrEqual(particles.ParticleCount,AdamImpactParticles.Capacity);
            crowded.Session.Damage(100000);view.Update(crowded,0);particles.Update(crowded,view,0);Assert.Zero(particles.ParticleCount,"terminal clears decoration before death clip/modal");
        }
        finally{particles.Dispose();Assert.AreEqual(oldMask,main.cullingMask);UnityEngine.Object.Destroy(main.gameObject);}
        yield return null;Assert.IsEmpty(UnityEngine.Object.FindObjectsByType<ParticleSystem>(FindObjectsSortMode.None).Where(p=>p.name.StartsWith("pooled burst ")));
        if(Directory.Exists(directory))Directory.Delete(directory,true);
    }
    [UnityTest] public IEnumerator LivePrepareThenClearDisablesCameraAndNextRealBurstReenablesSameCamera() => ClearCameraRegression(false);
    [UnityTest] public IEnumerator NoRunReloadSkipsPrepareButStillDisablesCameraAndNextWorldReenablesIt() => ClearCameraRegression(true);
    static IEnumerator ClearCameraRegression(bool noRunReload)
    {
        var directory=Path.Combine(Path.GetTempPath(),"adam-particle-clear-"+Guid.NewGuid().ToString("N"));
        var owner=new GameObject("actual game Clear camera regression");var game=owner.AddComponent<AdamGame>();game.AutoInitialize=false;game.enabled=false;
        var path=Path.Combine(directory,"save.json");var host=Fixture(path);game.Initialize(host);game.ImpactAudio.Muted=true;
        try
        {
            game.UpdatePresentation(0);host.Tick(1d/60);game.UpdatePresentation(0);game.PrepareParticles(new Rect(0,0,1100,720));
            var camera=game.ImpactParticles.OverlayCamera;Assert.Greater(game.ImpactParticles.ParticleCount,0);Assert.IsTrue(camera.enabled,"live prepared particles need their RT camera");
            if(noRunReload)
            {
                var store=new SaveStore(path);var state=store.Load();state.Run=null;store.Commit(state,state.Revision,Guid.NewGuid().ToString("N"));
                Assert.IsTrue(host.Reload(),host.Error);Assert.IsNull(host.World);game.UpdatePresentation(0);
                Assert.IsNull(game.ArtWorld.Snapshot,"real PrepareParticles returns early for an absent World");
            }
            else game.ImpactParticles.Clear();
            Assert.Zero(game.ImpactParticles.ParticleCount);Assert.IsFalse(camera.enabled,"CPT-01: Clear must disable RT camera even when no later valid Prepare runs");
            // Stage a real saved next World with swept hits, without replacing the product particle owner/camera.
            if(host.Session==null)host.Start("sling");host.SetSuspended(false);host.SetPaused(false);
            for(var n=0;n<20 && host.World.Capture().Enemies.Count<2;n++)host.Tick(.1);
            host.Checkpoint();var nextStore=new SaveStore(path);var next=nextStore.Load();var world=next.Run.World;
            foreach(var enemy in world.Enemies){enemy.X=world.PlayerX+1;enemy.Y=world.PlayerY;}
            world.Projectiles.Clear();var id=world.NextProjectileId++;world.Projectiles.Add(new BattleProjectileState{Id=id,X=world.PlayerX+.8f,Y=world.PlayerY,VelocityX=100,Lifetime=1,Damage=.001,RemainingPierce=1,Radius=.1});
            world.Weapons.Clear();world.Weapons.Add(new BattleWeaponState{WeaponId="sling",NextCycle=100});
            nextStore.Commit(next,next.Revision,Guid.NewGuid().ToString("N"));Assert.IsTrue(host.Reload(),host.Error);host.SetPaused(false);
            game.UpdatePresentation(0);Assert.IsFalse(camera.enabled);host.Tick(1d/60);game.UpdatePresentation(0);game.PrepareParticles(new Rect(0,0,1100,720));
            Assert.AreSame(camera,game.ImpactParticles.OverlayCamera);Assert.Greater(game.ImpactParticles.ParticleCount,0);Assert.IsTrue(camera.enabled,"a new valid real burst must reactivate the retained camera");
        }
        finally{UnityEngine.Object.Destroy(owner);}
        yield return null;if(Directory.Exists(directory))Directory.Delete(directory,true);
    }

}
