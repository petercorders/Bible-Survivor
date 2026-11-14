using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BibleSurvivor.Core;
using BibleSurvivor.Unity;
using NUnit.Framework;
using UnityEngine;

public sealed class AdamImpactTests
{
    string directory;
    [SetUp] public void Setup(){directory=Path.Combine(Path.GetTempPath(),"adam-impact-"+Guid.NewGuid().ToString("N"));}
    [TearDown] public void Cleanup(){if(Directory.Exists(directory))Directory.Delete(directory,true);}
    public static AdamHost Fixture(string path,bool lethal=false)
    {
        var store=new SaveStore(path);var host=new AdamHost(AdamCatalog.Load(),store);host.Start("jawbone");
        for(var i=0;i<20 && host.World.Capture().Enemies.Count<2;i++)host.Tick(.1);
        host.Checkpoint();var state=store.Load();var snapshot=state.Run.World;

        foreach(var enemy in snapshot.Enemies){enemy.X=1;enemy.Y=0;}
        // Two actual swept projectiles plus the jawbone hit land in the same Core step.
        snapshot.NextProjectileId=3;
        for(var i=1;i<=2;i++)snapshot.Projectiles.Add(new BattleProjectileState{Id=i,X=.8f,VelocityX=100,Lifetime=1,Damage=lethal?1000:.1,RemainingPierce=1,Radius=.1});
        snapshot.Weapons.Clear();snapshot.Weapons.Add(new BattleWeaponState{WeaponId="jawbone",NextCycle=100});
        store.Commit(state,state.Revision,Guid.NewGuid().ToString("N"));Assert.IsTrue(host.Reload(),host.Error);host.SetPaused(false);return host;
    }
    [TestCase(false)] [TestCase(true)] public void ActualWorldHitsEachEmitOneSparkIncludingLethalAndNoRedrawReplay(bool lethal)
    {
        var host=Fixture(Path.Combine(directory,"save.json"),lethal);var view=new AdamArtWorld();view.Update(host,0);
        host.Tick(1d/60);view.Update(host,0);
        var impacts=new List<BattleImpact>();host.World.ReadImpacts(0,impacts);
        Assert.Greater(impacts.Count,1);if(lethal)Assert.IsTrue(impacts.Any(e=>e.Killed));
        Assert.AreEqual(impacts.Count(e=>e.TargetId>0),view.Effects.Count(e=>e.Active&&e.Clip=="fx_hit-spark_hit_16"));
        Assert.AreEqual(impacts.Count(e=>e.Killed),view.Effects.Count(e=>e.Active&&e.Clip=="fx_enemy-death_burst_32"));
        Assert.AreEqual(impacts.Count,view.ConsumedImpacts);
        if(!lethal)Assert.IsTrue(view.EnemyReacting(impacts[0].TargetId),"surviving enemy must show the actual hit reaction");
        var effects=view.ActiveEffects;view.Update(host,0);Assert.AreEqual(effects,view.ActiveEffects);Assert.IsEmpty(view.FrameImpacts);
        host.Checkpoint();host.Reload();view.Update(host,0);Assert.Zero(view.ActiveEffects);Assert.Zero(view.ConsumedImpacts);
    }
    [Test] public void WarningSurvivesPoolPressureAndSquashKeepsFeetPivot()
    {
        var pool=new AdamArtWorld.EffectPool();pool.Emit("warning",Vector2.zero,0,2);
        for(var i=0;i<300;i++)pool.Emit("decoration",Vector2.one,0);
        Assert.AreEqual(1,pool.Slots.Count(e=>e.Active&&e.Clip=="warning"));
        Assert.AreEqual(64,pool.Slots.Length);
        var feet=new Vector2(103,77);Assert.Less(((Vector2)AdamArtWorld.ReactionMatrix(feet,1).MultiplyPoint3x4(feet)-feet).magnitude,.001);
    }
    [Test] public void LowEffectsStillShowsActualPlayerContactWarningAndNoStaleReloadReaction()
    {
        var host=Fixture(Path.Combine(directory,"warning.json"));host.Checkpoint();var store=new SaveStore(Path.Combine(directory,"warning.json"));var state=store.Load();
        state.Run.World.Enemies[0].X=state.Run.World.PlayerX+.1f;state.Run.World.Enemies[0].Y=state.Run.World.PlayerY;
        state.Run.World.Projectiles.Clear();store.Commit(state,state.Revision,Guid.NewGuid().ToString("N"));Assert.IsTrue(host.Reload());host.SetPaused(false);
        var view=new AdamArtWorld{FeedbackEnabled=false};view.Update(host,0);host.Tick(1d/60);view.Update(host,0);
        Assert.Greater(view.PlayerHitUntil,view.Clock);Assert.IsTrue(view.Effects.Any(e=>e.Active&&e.Priority==2));
        host.Checkpoint();host.Reload();view.Update(host,0);Assert.Zero(view.PlayerHitUntil);Assert.Zero(view.ActiveEffects);
    }
    [Test] public void ProceduralWeaponPcmIsDistinctFiniteBoundedAndSilentAtStart()
    {
        var samples=AdamImpactAudio.Weapons.Select(AdamImpactAudio.Samples).ToArray();
        foreach(var data in samples){Assert.Greater(data.Length,1000);Assert.IsTrue(data.All(v=>!float.IsNaN(v)&&Math.Abs(v)<=1));Assert.Zero(data[0]);Assert.Greater(data.Max(v=>Math.Abs(v)),.1);}
        Assert.AreNotEqual(samples[0][100],samples[1][100]);Assert.AreNotEqual(samples[1][100],samples[2][100]);
    }
}
