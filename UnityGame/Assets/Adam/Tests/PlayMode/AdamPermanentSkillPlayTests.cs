#if UNITY_EDITOR
using System;
using System.Collections;
using System.IO;
using System.Linq;
using BibleSurvivor.Core;
using BibleSurvivor.Unity;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;

public class AdamPermanentSkillPlayTests
{
    const int Width = 660;
    const int Height = 440;
    const long StartingGold = 600;
    const long PickupCost = 60;

    [UnityTest]
    public IEnumerator SmallPauseShopPointerBuysAndRefundsPickupWithoutChangingLegacyChallenge()
    {
        var directory = Path.Combine(Path.GetTempPath(), "adam-permanent-pointer-" + Guid.NewGuid().ToString("N"));
        var owner = new GameObject("Permanent skill pointer fixture");
        var window = ScriptableObject.CreateInstance<PauseModalPointerWindow>();
        var runInBackground = Application.runInBackground;
        try
        {
            Application.runInBackground = true;
            var store = new SaveStore(Path.Combine(directory, "save.json"));
            var catalog = AdamCatalog.Load();
            var initial = GameSnapshot.New();
            initial.Profile.Gold = StartingGold;
            _ = new AdamSession(initial, catalog.Definition, "sling");
            store.Commit(initial, initial.Revision, "legacy-pointer-fixture");

            var host = new AdamHost(catalog, store);
            host.SetPaused(false);
            host.SetMovement(Vector2.right);
            host.Tick(.1);
            host.SetPaused(true);
            var runId = host.State.Run.RunId;
            var battleTime = host.World.SimulationTime;
            var pauseRevision = host.State.Revision;
            Assert.Greater(battleTime, 0);
            Assert.AreEqual(1, host.Session.CombatRulesVersion);

            var game = owner.AddComponent<AdamGame>();
            game.AutoInitialize = false;
            // Tick manually so the pointer window can never advance the fixture between assertions.
            game.enabled = false;
            game.Initialize(host);
            game.UpdatePresentation(0);
            window.Game = game;
            window.minSize = window.maxSize = new Vector2(Width, Height);
            window.position = new Rect(80, 80, Width, Height);
            window.ShowUtility();
            window.Repaint();
            yield return null;

            var resume = AdamHud.PauseResume(Width, Height, false);
            // At 660 px, the shop sits immediately to the right of Resume.
            var shop = new Vector2(resume.xMax + 40, resume.center.y);
            var close = new Vector2(Width - 41, 65);
            yield return Click(shop);
            Assert.IsTrue(host.PermanentSkillsOpen, "The visible pause entry must open the shop.");
            Assert.IsTrue(host.Paused);
            var revision = store.LoadConfirmed().Revision;
            Assert.AreEqual(pauseRevision + 1, revision, "Opening from pause must confirm its checkpoint.");

            // The affordable default tree is first. Follow the browser's Next button to the pickup tree.
            var pickupTreeIndex = PermanentSkillCatalog.Current.Nodes
                .Select(node => node.TreeId).Distinct().TakeWhile(id => id != "common.pickup").Count();
            for (var i = 0; i < pickupTreeIndex; i++)
                yield return Click(new Vector2(339, 65));

            yield return Click(new Vector2(100, 170)); // Second row is locked by the Pickup prerequisite.
            yield return Click(new Vector2(445, 292)); // The prerequisite link jumps back to Pickup.
            yield return Click(new Vector2(445, 292)); // The same detail region now contains Pickup's Buy button.

            var cancel = new Vector2(200, 314);
            var confirm = new Vector2(460, 314);
            var notice = new Vector2(330, 314);
            yield return Click(cancel);
            Assert.AreEqual(revision, store.LoadConfirmed().Revision, "Cancel must leave the primary save intact.");
            Assert.AreEqual(StartingGold, host.PermanentSkillProfile.Gold);

            yield return Click(new Vector2(445, 292));
            Assert.AreEqual(revision, host.State.Revision, "Opening confirmation must not purchase immediately.");
            yield return Click(confirm);
            var purchased = store.LoadConfirmed();
            Assert.AreEqual(revision + 1, purchased.Revision, host.PermanentSkillError);
            Assert.AreEqual(StartingGold - PickupCost, purchased.Profile.Gold);
            Assert.AreEqual(1, PermanentRules.Rank(purchased.Profile.Upgrades, "pickup"));
            Assert.AreEqual(0, PermanentRules.Rank(purchased.Run.PermanentSnapshot, "pickup"),
                "A profile purchase only applies to the next challenge.");
            Assert.AreEqual(runId, purchased.Run.RunId);
            Assert.AreEqual(1, purchased.Run.CombatRulesVersion);
            Assert.AreEqual(battleTime, purchased.Run.World.Time);

            yield return Click(notice);
            var refund = new Vector2(82, Height - 60 + 30);
            yield return Click(refund);
            yield return Click(cancel);
            Assert.AreEqual(purchased.Revision, store.LoadConfirmed().Revision);
            Assert.AreEqual(StartingGold - PickupCost, host.PermanentSkillProfile.Gold);
            yield return Click(close);
            Assert.IsFalse(host.PermanentSkillsOpen, "Cancel must dismiss the refund confirmation and unlock the browser.");
            yield return Click(shop);
            Assert.IsTrue(host.PermanentSkillsOpen);
            var reopenedRevision = store.LoadConfirmed().Revision;
            Assert.AreEqual(purchased.Revision + 1, reopenedRevision);

            yield return Click(refund);
            yield return Click(confirm);
            var refunded = store.LoadConfirmed();
            Assert.AreEqual(reopenedRevision + 1, refunded.Revision, host.PermanentSkillError);
            Assert.AreEqual(StartingGold, refunded.Profile.Gold);
            Assert.IsEmpty(refunded.Profile.Upgrades);
            yield return Click(notice);
            yield return Click(close);

            Assert.IsFalse(host.PermanentSkillsOpen);
            Assert.IsTrue(host.Paused, "Closing the shop returns to the paused challenge.");
            Assert.AreEqual(runId, host.State.Run.RunId);
            Assert.AreEqual(battleTime, host.World.SimulationTime);
            var recovered = new AdamHost(catalog, store);
            Assert.AreEqual(runId, recovered.State.Run.RunId);
            Assert.AreEqual(1, recovered.Session.CombatRulesVersion);
            Assert.AreEqual(battleTime, recovered.World.SimulationTime);

            yield return Click(resume.center);
            Assert.IsFalse(host.Paused, "The original Resume control still responds after the shop closes.");
            host.Tick(.1);
            Assert.Greater(host.World.SimulationTime, battleTime);

            IEnumerator Click(Vector2 point)
            {
                game.SetLifecycle(true, false);
                window.SendEvent(new Event { type = EventType.MouseDown, mousePosition = point, button = 0, clickCount = 1 });
                window.SendEvent(new Event { type = EventType.MouseUp, mousePosition = point, button = 0, clickCount = 1 });
                yield return null;
            }
        }
        finally
        {
            window.Close();
            UnityEngine.Object.DestroyImmediate(owner);
            Application.runInBackground = runInBackground;
            if (Directory.Exists(directory)) Directory.Delete(directory, true);
        }
    }
}
#endif
