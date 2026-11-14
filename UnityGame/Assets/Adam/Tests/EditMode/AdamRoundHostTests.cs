using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Collections.Generic;
using System.Runtime.Serialization.Json;
using BibleSurvivor.Core;
using BibleSurvivor.Unity;
using NUnit.Framework;
using UnityEngine;

public sealed class AdamRoundHostTests
{
    string directory,path;AdamCatalog catalog;
    [SetUp] public void Setup(){directory=Path.Combine(Path.GetTempPath(),"adam-r02-"+Guid.NewGuid().ToString("N"));path=Path.Combine(directory,"save.json");catalog=AdamCatalog.Load();}
    [TearDown] public void Cleanup(){if(Directory.Exists(directory))Directory.Delete(directory,true);}
    public static string Json(GameSnapshot value){using(var stream=new MemoryStream()){new DataContractJsonSerializer(typeof(GameSnapshot)).WriteObject(stream,value);return Encoding.UTF8.GetString(stream.ToArray());}}
    public static GameSnapshot Read(string file){using(var stream=File.OpenRead(file))return (GameSnapshot)new DataContractJsonSerializer(typeof(GameSnapshot)).ReadObject(stream);}
    public static AdamHost Completed(string path,SaveStore store=null,bool pending=false)
    {
        var host=new AdamHost(AdamCatalog.Load(),store??new SaveStore(path));host.State.Profile.Upgrades["attack"]=1;host.Start("sling");
        for(var i=0;i<12;i++){host.SetMovement(Vector2.right);host.Tick(.1);}
        host.Session.Inventory.TryChoose("hair",true);host.Session.Advance(20);
        foreach(var id in host.Session.Wave.Active.Keys.Take(7).ToArray())host.Session.Kill(id);
        host.Session.Advance(180);host.SetPaused(true);
        if(!pending)while(host.Session.PendingChoices.Count>0)Assert.IsTrue(host.Choose(host.Session.PendingChoices[0]),host.Error);
        Assert.IsTrue(host.Checkpoint(),host.Error);return host;
    }
    [Test] public void CanonicalR01R02CatalogAndDevelopmentMarkersAreExplicitAndR03IsNotExposed()
    {
        Assert.AreEqual(40,catalog.Definition.ConcurrencyCap);var r=catalog.ForRound(2);Assert.AreEqual("genesis_02",r.Id);Assert.AreEqual(50,r.ConcurrencyCap);Assert.AreEqual(600,r.RawExperience);Assert.AreEqual(35,r.CompletionGold);Assert.AreEqual(360,r.Waves.Sum(w=>w.Groups.Sum(g=>g.Count)));Assert.AreEqual("lightning",r.UnlockWeaponId);Assert.AreNotEqual(catalog.Definition.Fingerprint,r.Fingerprint);
        Assert.AreEqual(.55,catalog.Speeds["b1"],1e-9);Assert.AreEqual(.90,catalog.Speeds["b2"],1e-9);Assert.AreEqual(.45,catalog.Speeds["b3"],1e-9);Assert.Throws<InvalidDataException>(()=>catalog.ForRound(3));
        Assert.AreEqual("enm_b1-envy-eye_move_32",AdamRoundPresentation.R02ClipId("b1"));Assert.AreEqual("fx_shadow-small_static_12x4",AdamRoundPresentation.Shadow("b1"));Assert.Throws<InvalidOperationException>(()=>AdamArtWorld.EnemyClip("b1"),"b1 must never silently reuse a-monster art");
        var root=JsonUtility.FromJson<AdamCatalog.Root>(Resources.Load<TextAsset>("genesis-catalog").text);root.rounds.Single(x=>x.number==2).spawn_budget++;Assert.Throws<InvalidDataException>(()=>new AdamCatalog(JsonUtility.ToJson(root)));
    }
    [Test] public void R02ControlHintFitsSmallWindowWithImportedPixelFont()
    {
        using(var fonts=new AdamFonts())
        {var style=new GUIStyle{font=fonts.Small,fontSize=16};Assert.LessOrEqual(style.CalcSize(new GUIContent(AdamRoundPresentation.ControlHint(2))).x,620,"single footer row at 660px must fit without clipping the next row");}
    }
    [Test] public void PendingCompletionChoicesCannotEnterOrBeOverwrittenAndLastSavedChoiceEntersOnce()
    {
        var host=Completed(path,pending:true);Assert.Greater(host.Session.PendingChoices.Count,0);var id=host.State.Run.RunId;var old=host.World;host.SetPaused(false);host.Tick(1);
        Assert.IsNull(host.Error);Assert.AreEqual(1,host.Session.RoundNumber);Assert.IsTrue(host.AwaitingR02);host.Start("bible");Assert.AreEqual(id,host.State.Run.RunId);
        while(host.Session.RoundNumber==1 && host.Session.PendingChoices.Count>0)Assert.IsTrue(host.Choose(host.Session.PendingChoices[0]),host.Error);
        Assert.AreEqual(2,host.Session.RoundNumber);Assert.IsNull(host.Error);Assert.AreNotSame(old,host.World);Assert.Zero(host.World.SimulationTime);var next=host.World;var gold=host.State.Profile.Gold;
        host.Tick(0);host.SetPaused(false);Assert.AreSame(next,host.World);Assert.AreEqual(gold,host.State.Profile.Gold);Assert.AreEqual(id,host.State.Run.RunId);
    }
    [Test] public void EntryPreservesProgressionAndSnapshotButResetsWorldAndDoesNotRehealOrReward()
    {
        var host=Completed(path);var old=host.World;Assert.Greater(old.SimulationTime,0);Assert.Greater(old.PlayerPosition.X,0);
        host.State.Profile.Upgrades["attack"]=2;Assert.IsTrue(host.Checkpoint());var before=Read(path);var weapons=before.Run.Weapons.ToArray();var passives=before.Run.Passives.ToArray();
        host.SetPaused(false);Assert.AreEqual(2,host.Session.RoundNumber);Assert.AreEqual(before.Run.RunId,host.State.Run.RunId);Assert.AreEqual(before.Run.Level,host.State.Run.Level);Assert.AreEqual(before.Run.Experience,host.State.Run.Experience);Assert.AreEqual(before.Run.KillRemainder,host.State.Run.KillRemainder);CollectionAssert.AreEquivalent(weapons,host.State.Run.Weapons.ToArray());CollectionAssert.AreEquivalent(passives,host.State.Run.Passives.ToArray());Assert.AreEqual(before.Profile.Gold,host.State.Profile.Gold);Assert.AreEqual(1,host.State.Run.PermanentSnapshot["attack"]);Assert.AreEqual(before.Run.Health,host.Session.CurrentHealth);
        Assert.AreNotSame(old,host.World);var world=host.World.Capture();Assert.Zero(world.Time);Assert.Zero(world.PlayerX);Assert.Zero(world.PlayerY);Assert.IsEmpty(world.Enemies);Assert.IsEmpty(world.Projectiles);Assert.IsEmpty(world.Weapons);Assert.IsEmpty(world.Drops);Assert.AreEqual(-1,host.State.Run.LastHitTime);
        host.Session.Damage(1);Assert.AreEqual(before.Run.Health-1,host.Session.CurrentHealth,"old-clock invulnerability/healing must not survive entry");
        var restored=new AdamHost(catalog,new SaveStore(path));Assert.IsNull(restored.Error);Assert.AreEqual(2,restored.Session.RoundNumber);Assert.IsTrue(restored.Paused);Assert.Zero(restored.World.SimulationTime);Assert.AreEqual(before.Run.Health,restored.Session.CurrentHealth);
    }
    [TestCase(false)] [TestCase(true)] public void R02EntryWriteFailureOrUnknownOutcomeFreezesSameArenaAndOperation(bool resultUnknown)
    {
        var fail=false;var operations=new List<string>();var store=new SaveStore(path,()=>
        {
            var staged=Directory.GetFiles(directory,"save.json.tmp-*").Single();var candidate=Read(staged);
            if(candidate.Run?.Stage=="normal" && candidate.Run.RoundNumber==2)
            {
                operations.Add(candidate.LastOperationId);
                if(fail)
                {
                    if(resultUnknown){if(File.Exists(path))File.Copy(path,path+".bak",true);File.Copy(staged,path,true);}
                    throw new IOException(resultUnknown?"simulated committed outcome unknown":"entry write failed");
                }
            }
        });
        var host=Completed(path,store);var gold=host.State.Profile.Gold;var previous=store.Load().Revision;fail=true;host.SetPaused(false);Assert.IsNotNull(host.Error);Assert.IsTrue(host.HasSaveRetry);Assert.AreEqual(2,host.Session.RoundNumber);var world=host.World;var frozen=Json(host.State);
        host.Tick(20);host.SetPaused(false);host.Start("bible");host.Choose("sling");Assert.AreSame(world,host.World);Assert.Zero(world.SimulationTime);Assert.AreEqual(frozen,Json(host.State));Assert.AreEqual(gold,host.State.Profile.Gold);
        fail=false;Assert.IsTrue(host.RetrySave(),host.Error);Assert.AreSame(world,host.World);Assert.Zero(world.SimulationTime);Assert.IsTrue(host.Paused);Assert.AreEqual(previous+1,store.Load().Revision);Assert.AreEqual(gold,store.Load().Profile.Gold);
        if(!resultUnknown){Assert.AreEqual(2,operations.Count);Assert.AreEqual(operations[0],operations[1]);}else Assert.AreEqual(1,operations.Count,"idempotent retry must bypass a second write");
        Assert.IsFalse(host.RetrySave());var restored=new AdamHost(catalog,new SaveStore(path));Assert.IsNull(restored.Error);Assert.AreEqual(2,restored.Session.RoundNumber);Assert.Zero(restored.World.SimulationTime);
    }
    [Test] public void LastChoiceWriteFailureStaysCompletedUntilRetryAndExplicitResume()
    {
        var fail=false;var store=new SaveStore(path,()=>{if(fail)throw new IOException("completed choice failed");});var host=Completed(path,store,pending:true);host.SetPaused(false);var gold=host.State.Profile.Gold;fail=true;
        Assert.IsTrue(host.Choose(host.Session.PendingChoices[0]));Assert.IsNotNull(host.Error);Assert.AreEqual(1,host.Session.RoundNumber);Assert.IsTrue(host.AwaitingR02);Assert.IsFalse(host.CanPlay);fail=false;Assert.IsTrue(host.RetrySave());
        host.SetPaused(false);while(host.Session.RoundNumber==1 && host.Session.PendingChoices.Count>0)host.Choose(host.Session.PendingChoices[0]);Assert.AreEqual(2,host.Session.RoundNumber);Assert.AreEqual(gold,host.State.Profile.Gold);
    }
    [Test] public void NormalAndBetweenDefinitionsLegacyMissingWorldAndFingerprintsRestoreWithoutOverwrites()
    {
        var host=Completed(path);var boundary=new AdamHost(catalog,new SaveStore(path));Assert.IsNull(boundary.Error);Assert.AreEqual(1,boundary.Session.RoundNumber);Assert.IsTrue(boundary.AwaitingR02);Assert.IsNull(boundary.World);
        boundary.SetPaused(false);boundary.Tick(.1);boundary.Checkpoint();var store=new SaveStore(path);var state=store.Load();state.Run.World=null;store.Commit(state,state.Revision,Guid.NewGuid().ToString("N"));var compatible=new AdamHost(catalog,store);Assert.IsNull(compatible.Error);Assert.AreEqual(2,compatible.Session.RoundNumber);Assert.NotNull(compatible.World);
        state=store.Load();state.Run.RoundFingerprint=new string('a',64);store.Commit(state,state.Revision,Guid.NewGuid().ToString("N"));var bytes=File.ReadAllBytes(path);var rejected=new AdamHost(catalog,store);Assert.NotNull(rejected.Error);rejected.Start("bible");CollectionAssert.AreEqual(bytes,File.ReadAllBytes(path));
        state=store.Load();state.Run.RoundFingerprint=null;Assert.Throws<InvalidDataException>(()=>store.Commit(state,state.Revision,Guid.NewGuid().ToString("N")));var invalidLegacy=Path.Combine(directory,"invalid-legacy-r02.json");File.WriteAllText(invalidLegacy,Json(state));Assert.IsNotNull(new AdamHost(catalog,new SaveStore(invalidLegacy)).Error,"legacy R02 must not bypass Core fingerprint protection");
        var legacyPath=Path.Combine(directory,"legacy.json");var old=Completed(legacyPath);var legacyStore=new SaveStore(legacyPath);state=legacyStore.Load();state.Run.RoundFingerprint=null;legacyStore.Commit(state,state.Revision,Guid.NewGuid().ToString("N"));var legacy=new AdamHost(catalog,legacyStore);Assert.IsNull(legacy.Error);Assert.IsTrue(legacy.AwaitingR02);legacy.SetPaused(false);Assert.AreEqual(2,legacy.Session.RoundNumber);
    }
    [Test] public void R02CompletionPersistsR03WaitingAndDeathOrExplicitNewChallengeStartsOnlyR01()
    {
        var host=Completed(path);host.SetPaused(false);Assert.AreEqual(2,host.Session.RoundNumber);host.Session.Advance(180);Assert.IsTrue(host.Checkpoint());Assert.IsTrue(host.ContentComplete);Assert.AreEqual(3,host.State.Run.RoundNumber);var gold=host.State.Profile.Gold;Assert.IsTrue(host.State.Profile.UnlockedWeapons.Contains("lightning"));
        var restored=new AdamHost(catalog,new SaveStore(path));Assert.IsNull(restored.Error);Assert.IsTrue(restored.ContentComplete);Assert.AreEqual(2,restored.Session.RoundNumber);restored.SetPaused(false);restored.Tick(180);Assert.IsNull(restored.World);Assert.AreEqual(gold,restored.State.Profile.Gold);restored.Start("lightning");Assert.IsTrue(restored.ContentComplete);restored.Start("bible");Assert.AreEqual(1,restored.Session.RoundNumber);Assert.AreEqual(1,restored.Session.Growth.Level);Assert.AreEqual(gold,restored.State.Profile.Gold);
        var second=Completed(Path.Combine(directory,"death.json"));second.SetPaused(false);var balance=second.State.Profile.Gold;second.Session.Damage(100000);Assert.IsTrue(second.Checkpoint());second.Start("jawbone");Assert.AreEqual(1,second.Session.RoundNumber);Assert.AreEqual(balance,second.State.Profile.Gold);
    }
    [TestCase("bible")] [TestCase("jawbone")] [TestCase("sling")] public void ThreeWeaponsHitInjuredR02EnemiesAndCollectedExperienceCanBeChosen(string weapon)
    {
        var host=Completed(path);host.SetPaused(false);var store=new SaveStore(path);host.Checkpoint();var state=store.Load();state.Run.Weapons.Clear();state.Run.Weapons.Add(weapon,1);state.Run.Passives.Clear();state.Run.Level=1;state.Run.Experience=0;state.Run.PendingLevels=0;state.Run.Choices.Clear();state.Run.World=null;store.Commit(state,state.Revision,Guid.NewGuid().ToString("N"));Assert.IsTrue(host.Reload(),host.Error);host.SetPaused(false);
        for(var i=0;i<80 && host.Session.Wave.Active.Count<12;i++)host.Tick(.1);Assert.IsTrue(host.Session.Wave.Active.Values.All(e=>e.MonsterId.StartsWith("b")));host.Checkpoint();state=store.Load();foreach(var enemy in state.Run.World.Enemies){var angle=weapon=="bible"?(state.Run.World.Time+1d/60)*2.5:0;enemy.X=state.Run.World.PlayerX+(float)Math.Cos(angle)*1.4f;enemy.Y=state.Run.World.PlayerY+(float)Math.Sin(angle)*1.4f;}foreach(var enemy in state.Run.Wave.Active)enemy.Health=1; // Explicit injured-enemy fixture; not a balance or full-health kill measurement.
        store.Commit(state,state.Revision,Guid.NewGuid().ToString("N"));Assert.IsTrue(host.Reload(),host.Error);host.SetPaused(false);
        for(var i=0;i<600 && host.Session.IsActive && host.Session.PendingChoices.Count==0;i++)
        {
            var drops=host.World.DropPositions;
            var target=drops.Count==0?host.World.PlayerPosition:drops.OrderBy(x=>System.Numerics.Vector2.DistanceSquared(x.Value,host.World.PlayerPosition)).First().Value;
            host.SetMovement(new Vector2(target.X-host.World.PlayerPosition.X,target.Y-host.World.PlayerPosition.Y));host.Tick(1d/60);
        }
        Assert.Greater(host.World.Hits,0,"actual R02 hits");Assert.Greater(host.World.Kills,0,"actual R02 kills");Assert.Greater(host.Session.PendingChoices.Count,0,"staged real R02 combat must naturally award collectible growth");var choice=host.Session.PendingChoices[0];Assert.IsTrue(host.Choose(choice),host.Error);
    }
}
