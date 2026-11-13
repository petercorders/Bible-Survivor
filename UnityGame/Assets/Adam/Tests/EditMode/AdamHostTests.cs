using System;
using System.IO;
using System.Linq;
using BibleSurvivor.Core;
using BibleSurvivor.Unity;
using NUnit.Framework;
using UnityEngine;

public class AdamHostTests
{
    string directory;
    string path;
    AdamCatalog catalog;
    [SetUp] public void Setup() { directory = Path.Combine(Path.GetTempPath(), "adam-unity-" + Guid.NewGuid().ToString("N")); path = Path.Combine(directory, "save.json"); catalog = AdamCatalog.Load(); }
    [TearDown] public void Cleanup() { if (Directory.Exists(directory)) Directory.Delete(directory, true); }
    AdamHost NewHost(SaveStore store = null) => new AdamHost(catalog, store ?? new SaveStore(path));
    [TestCase("bible")] [TestCase("jawbone")] [TestCase("sling")]
    public void AllStartingWeaponsMoveAttackAndResumeExactly(string weapon)
    {
        var host = NewHost(); host.Start(weapon);
        for (var i = 0; i < 90; i++) { host.SetMovement(Vector2.right); host.Tick(1.0 / 60); }
        Assert.Greater(host.World.PlayerPosition.X, 2);
        Assert.Greater(host.World.ShotsFired, 0);
        Assert.IsTrue(host.Checkpoint(), host.Error);
        var restored = NewHost();
        Assert.IsNull(restored.Error);
        Assert.AreEqual(host.World.PlayerPosition, restored.World.PlayerPosition);
        Assert.AreEqual(host.World.SimulationTime, restored.World.SimulationTime);
        Assert.AreEqual(host.World.ShotsFired, restored.World.ShotsFired);
        Assert.IsTrue(restored.Paused);
    }
    [Test] public void PauseFocusAndLongFrameCannotAccumulateDowntime()
    {
        var host = NewHost(); host.Start("bible"); host.Tick(.1);
        var before = host.World.SimulationTime;
        host.SetPaused(true); host.SetMovement(Vector2.one); host.Tick(20);
        Assert.AreEqual(before, host.World.SimulationTime); Assert.AreEqual(Vector2.zero, host.Movement);
        host.SetPaused(false); host.SetSuspended(true); host.Tick(20); host.SetSuspended(false); host.Tick(20);
        Assert.AreEqual(before, host.World.SimulationTime);
        host.SetPaused(false); host.Tick(20);
        Assert.LessOrEqual(host.World.SimulationTime - before, .100001);
    }
    [Test] public void SaveFailureFreezesPayloadAndRetriesSameOperationExactlyOnce()
    {
        bool fail = false;
        var store = new SaveStore(path, () => { if (fail) throw new IOException("injected replacement failure"); });
        var host = NewHost(store); host.Start("sling"); host.Tick(.1);
        var revision = store.Load().Revision;
        fail = true; Assert.IsFalse(host.Checkpoint());
        var time = host.World.SimulationTime;
        host.Tick(10); host.Choose("sling"); host.Start("bible");
        Assert.AreEqual(time, host.World.SimulationTime); Assert.AreEqual(revision, store.Load().Revision);
        fail = false; Assert.IsTrue(host.RetrySave(), host.Error);
        Assert.AreEqual(revision + 1, store.Load().Revision);
        Assert.IsFalse(host.RetrySave()); Assert.AreEqual(revision + 1, store.Load().Revision);
        Assert.AreEqual(time, NewHost().World.SimulationTime);
    }
    [Test] public void AutosaveRollsBackLessThanFiveSimulationSeconds()
    {
        var host = NewHost(); host.Start("sling");
        for (int i = 0; i < 370; i++) { host.SetMovement(Vector2.right); host.Tick(1.0 / 60); }
        var restored = NewHost();
        Assert.GreaterOrEqual(restored.World.SimulationTime, 5);
        Assert.Less(host.World.SimulationTime - restored.World.SimulationTime, 5);
    }
    [Test] public void CompletionAndDeathCommitWithoutDuplicateGoldOrResurrection()
    {
        var host = NewHost(); host.Start("bible");
        // Lifecycle boundary injection, not a full survival/balance playthrough.
        host.Session.Advance(180); Assert.IsTrue(host.Checkpoint(), host.Error);
        long gold = host.State.Profile.Gold;
        var restored = NewHost(); Assert.IsNull(restored.Error);
        Assert.AreEqual("between_rounds", restored.State.Run.Stage);
        restored.Session.Advance(180); Assert.IsTrue(restored.Checkpoint());
        Assert.AreEqual(gold, restored.State.Profile.Gold);
        restored.Start("jawbone"); restored.Session.Damage(1000); Assert.IsTrue(restored.Checkpoint(), restored.Error);
        var dead = NewHost(); Assert.IsNull(dead.State.Run); Assert.IsNull(dead.Session);
        Assert.AreEqual(gold, dead.State.Profile.Gold);
    }
    [Test] public void NaturalCombatChoicesFreezeAndPersistBeforeAndAfterSelection()
    {
        var host = NewHost(); host.Start("sling");
        for (int i = 0; i < 10800 && host.Session.IsActive && host.Session.PendingChoices.Count == 0; i++)
        {
            var world = host.World;
            var direction = world.DropPositions.Count == 0 ? new System.Numerics.Vector2((float)Math.Cos(world.SimulationTime * .3), (float)Math.Sin(world.SimulationTime * .3))
                : world.DropPositions.OrderBy(x => System.Numerics.Vector2.DistanceSquared(x.Value, world.PlayerPosition)).First().Value - world.PlayerPosition;
            host.SetMovement(new Vector2(direction.X, direction.Y)); host.Tick(1.0 / 60);
        }
        Assert.Greater(host.Session.PendingChoices.Count, 0, "Natural play must reach a level-up");
        Assert.LessOrEqual(host.Session.PendingChoices.Count, 3);
        var restored = NewHost(); Assert.IsNull(restored.Error);
        CollectionAssert.AreEqual(host.Session.PendingChoices, restored.Session.PendingChoices);
        var time = host.World.SimulationTime; host.Tick(10); Assert.AreEqual(time, host.World.SimulationTime);
        var choice = host.Session.PendingChoices[0]; Assert.IsTrue(host.Choose(choice), host.Error);
        Assert.IsFalse(host.Choose("removed-target"));
        Assert.IsNull(NewHost().Error);
        CollectionAssert.AreEqual(host.Session.PendingChoices, NewHost().Session.PendingChoices);
    }
    [Test] public void FutureSaveIsPreservedAndBlocksStarting()
    {
        Directory.CreateDirectory(directory); File.WriteAllText(path, "{\"SchemaVersion\":999}");
        var host = NewHost(); Assert.IsNotNull(host.Error); host.Start("bible");
        Assert.AreEqual("{\"SchemaVersion\":999}", File.ReadAllText(path));
    }
    [Test] public void CorruptPrimaryRecoversValidBackup()
    {
        var host = NewHost(); host.Start("sling"); host.Tick(.1); Assert.IsTrue(host.Checkpoint());
        File.WriteAllText(path, "corrupt"); var recovered = NewHost();
        Assert.IsNull(recovered.Error); Assert.IsTrue(recovered.Paused); Assert.IsNotNull(recovered.World);
        Assert.IsTrue(recovered.Checkpoint(), recovered.Error);
    }
}
