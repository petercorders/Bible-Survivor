using System;
using System.IO;
using BibleSurvivor.Core;
using BibleSurvivor.Unity;
using NUnit.Framework;
using UnityEngine;

public class AdamPermanentSkillHostTests
{
    string folder;
    string path;
    AdamCatalog catalog;

    [SetUp]
    public void Setup()
    {
        folder = Path.Combine(Path.GetTempPath(), "adam-permanent-host-" + Guid.NewGuid().ToString("N"));
        path = Path.Combine(folder, "save.json");
        catalog = AdamCatalog.Load();
        var initial = GameSnapshot.New();
        initial.Profile.Gold = 600;
        new SaveStore(path).Commit(initial, 0, "seed");
    }

    [TearDown]
    public void Cleanup()
    {
        if (Directory.Exists(folder)) Directory.Delete(folder, true);
    }

    [Test]
    public void PurchaseInPauseAndRefundPreserveCurrentChallenge()
    {
        var store = new SaveStore(path);
        var legacy = store.Load();
        _ = new AdamSession(legacy, catalog.Definition, "sling");
        store.Commit(legacy, legacy.Revision, "legacy-active-challenge");
        var host = new AdamHost(catalog, store);
        host.SetPaused(false);
        host.SetMovement(Vector2.right);
        host.Tick(.1);
        host.SetPaused(true);
        var run = host.State.Run.RunId;
        var worldTime = host.World.SimulationTime;
        Assert.IsTrue(host.OpenPermanentSkills(), host.Error);
        var selected = BuyPickup(host.State.Revision);
        Assert.IsTrue(host.SubmitPermanentSkill(selected), host.PermanentSkillError);
        Assert.AreEqual(540L, host.PermanentSkillProfile.Gold);
        Assert.AreEqual(run, host.State.Run.RunId);
        Assert.AreEqual(1, host.Session.CombatRulesVersion);
        Assert.AreEqual(worldTime, host.World.SimulationTime);
        host.SetPaused(false);
        host.Tick(.1);
        Assert.AreEqual(worldTime, host.World.SimulationTime);
        Assert.IsTrue(host.Paused);

        var (amount, digest) = PermanentSkillCommand.QuoteRefund(host.PermanentSkillProfile);
        var revision = host.State.Revision;
        Assert.IsTrue(host.SubmitPermanentSkill(PermanentSkillCommand.RefundAll(
            PermanentSkillCommand.NewOperationId(revision), revision, amount, digest)), host.PermanentSkillError);
        Assert.IsTrue(host.ClosePermanentSkills());
        Assert.IsTrue(host.Paused);
        var restarted = new AdamHost(catalog, store);
        Assert.AreEqual(600L, restarted.State.Profile.Gold);
        Assert.AreEqual(run, restarted.State.Run.RunId);
        Assert.AreEqual(worldTime, restarted.World.SimulationTime);
    }

    [Test]
    public void UncertainPurchaseHoldsOldDisplayAndSameRequestUntilPrimaryConfirmation()
    {
        var fault = true;
        var store = new SaveStore(path, beforePermanentConfirmation: () =>
        {
            if (fault) throw new IOException("injected readback interruption");
        });
        var host = new AdamHost(catalog, store);
        Assert.IsTrue(host.OpenPermanentSkills(), host.Error);
        var revision = host.State.Revision;
        Assert.IsFalse(host.SubmitPermanentSkill(BuyPickup(revision)));
        Assert.IsTrue(host.HasPermanentSkillRetry);
        Assert.AreEqual(600L, host.PermanentSkillProfile.Gold);
        Assert.IsFalse(host.ClosePermanentSkills());
        Assert.IsFalse(host.Reload());
        Assert.IsFalse(host.Checkpoint());
        host.Start("sling");
        Assert.IsNull(host.State.Run);
        Assert.AreEqual(540L, store.LoadConfirmed().Profile.Gold);
        fault = false;
        Assert.IsTrue(host.RetryPermanentSkill(), host.PermanentSkillError);
        Assert.IsFalse(host.HasPermanentSkillRetry);
        Assert.AreEqual(revision + 1, host.State.Revision);
        Assert.AreEqual(540L, host.PermanentSkillProfile.Gold);
    }

    [Test]
    public void StaleConfirmationRefreshesTheProfileAndRequiresNewInput()
    {
        var store = new SaveStore(path);
        var host = new AdamHost(catalog, store);
        Assert.IsTrue(host.OpenPermanentSkills(), host.Error);
        var revision = host.State.Revision;
        var selected = BuyPickup(revision);
        store.TransactPermanent(PermanentSkillCommand.Purchase(
            PermanentSkillCommand.NewOperationId(revision), revision, "health", 0, 80));
        Assert.IsFalse(host.SubmitPermanentSkill(selected));
        Assert.IsFalse(host.HasPermanentSkillRetry);
        Assert.IsNotEmpty(host.PermanentSkillError);
        Assert.AreEqual(520L, host.PermanentSkillProfile.Gold);
        Assert.IsFalse(host.PermanentSkillProfile.Upgrades.ContainsKey("pickup"));
        Assert.IsTrue(host.SubmitPermanentSkill(BuyPickup(host.State.Revision)), host.PermanentSkillError);
        Assert.AreEqual(460L, host.PermanentSkillProfile.Gold);
    }

    [Test]
    public void ValidBackupRecoversBeforeTheTitleOpensTheSkillTree()
    {
        File.Move(path, path + ".bak");
        var backup = File.ReadAllBytes(path + ".bak");
        var store = new SaveStore(path);
        var host = new AdamHost(catalog, store);
        Assert.IsTrue(host.OpenPermanentSkills(), host.Error);
        Assert.AreEqual(600L, store.LoadConfirmed().Profile.Gold);
        CollectionAssert.AreEqual(backup, File.ReadAllBytes(path + ".bak"));
        Assert.IsTrue(host.SubmitPermanentSkill(BuyPickup(host.State.Revision)), host.PermanentSkillError);
        Assert.AreEqual(540L, store.LoadConfirmed().Profile.Gold);
    }

    static PermanentSkillCommand BuyPickup(long revision) => PermanentSkillCommand.Purchase(
        PermanentSkillCommand.NewOperationId(revision), revision, "pickup", 0, 60);
}
