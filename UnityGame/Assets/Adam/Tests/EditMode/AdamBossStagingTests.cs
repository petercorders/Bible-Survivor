using System;
using System.IO;
using System.Linq;
using BibleSurvivor.Core;
using BibleSurvivor.Unity;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using CoreVector = System.Numerics.Vector2;

internal static class AdamBossStagingFixture
{
    internal const double Step = 1d / 60;

    internal static AdamHost BeforeIntro(string path, int round, bool exhausted = false)
    {
        var catalog = AdamCatalog.Load();
        var state = GameSnapshot.New();
        var session = new AdamSession(state, catalog.ForRound(1), "jawbone");
        for (var current = 1; current < round; current++)
        {
            session.Advance(180);
            if (session.Stage == "boss_intro")
            {
                new BattleWorld(session, catalog.SpeedsFor(current), 7).Advance(2, CoreVector.Zero);
                session.Hit(session.BossRules.BossActorId, session.BossRules.MaxHealth);
                if (session.Stage == "boss_reward")
                    session.ChooseBossReward(session.Boss.Reward.RewardId, session.BossChoices[0].Id);
                session.CompleteBossSettlement();
            }
            if (current == 9)
            {
                state = session.PrepareExodus(catalog.ForRound(10));
                session = AdamSession.Resume(state, catalog.ForRound(10));
            }
            else session = session.StartNextRound(catalog.ForRound(current + 1));
        }

        if (exhausted)
        {
            foreach (var weapon in new[] { "bible", "jawbone", "sling" })
                while (session.Inventory.TryChoose(weapon, false)) { }
            foreach (var item in new[] { "scroll", "hair", "stones", "east_wind", "seal", "cloud", "thunder", "crown", "underwear", "whirlwind" })
                while (session.Inventory.TryChoose(item, true)) { }
            foreach (var weapon in new[] { "bible", "jawbone", "sling" })
                session.Inventory.TryEvolve(EvolutionRules.Form(weapon));
        }

        session.Advance(180 - Step);
        session.Capture();
        var store = new SaveStore(path);
        store.Commit(state, 0, "technical-vq03-before-intro");
        return new AdamHost(catalog, store);
    }

    internal static void ArrangeCombat(AdamHost host, string path, bool lethal)
    {
        var world = host.World.Capture();
        var actorId = host.Session.BossRules.BossActorId;
        if (lethal)
        {
            // Let Core create the threshold supports and mask. The next real jawbone tick awards the kill.
            host.Session.Hit(actorId, host.Session.Boss.Health - 1);
            world = new BattleWorld(host.Session, AdamCatalog.Load().SpeedsFor(host.Session.RoundNumber), 7).Capture();
        }
        var boss = world.Enemies.Single(e => e.Id == actorId);
        foreach (var enemy in world.Enemies)
        {
            enemy.X = 30;
            enemy.Y = 0;
        }
        if (lethal) boss.X = 1.3f;
        world.Weapons.Clear();
        foreach (var id in host.Session.Inventory.Weapons.Keys)
            if (!lethal || id != "jawbone")
                world.Weapons.Add(new BattleWeaponState
                {
                    WeaponId = id,
                    WeaponForm = host.Session.Inventory.Forms.TryGetValue(id, out var form) ? form : null,
                    NextCycle = 1000
                });
        host.Session.Capture();
        host.State.Run.World = world;
        new SaveStore(path).Commit(host.State, host.State.Revision, "technical-vq03-combat-" + Guid.NewGuid().ToString("N"));
        host.Reload();
    }
}

public sealed class AdamBossStagingTests
{
    string directory, path;
    GameObject owner;
    AdamBossStaging staging;
    AudioSource voice;
    AdamImpactAudio settings;

    [SetUp]
    public void SetUp()
    {
        directory = Path.Combine(Path.GetTempPath(), "adam-vq03-" + Guid.NewGuid().ToString("N"));
        path = Path.Combine(directory, "save.json");
        owner = new GameObject("Boss staging test");
        staging = new AdamBossStaging();
        staging.Initialize(owner);
        settings = new AdamImpactAudio(owner);
        voice = owner.transform.Find("boss staging voice").GetComponent<AudioSource>();
    }

    [TearDown]
    public void TearDown()
    {
        UnityEngine.Object.DestroyImmediate(owner);
        if (Directory.Exists(directory)) Directory.Delete(directory, true);
    }

    void Tick(AdamHost host)
    {
        var before = staging.BeforeTick(host, settings, true);
        host.Tick(AdamBossStagingFixture.Step);
        staging.AfterTick(before, host, settings, true);
    }

    [TestCase(3, 4.8)]
    [TestCase(6, 5.8)]
    [TestCase(9, 4.0)]
    [TestCase(12, 4.8)]
    [TestCase(15, 4.0)]
    public void LiveIntroWarningAndDefeatUseTheRealHostTick(int round, double warning)
    {
        var host = AdamBossStagingFixture.BeforeIntro(path, round);
        Assert.IsNull(host.Error);
        host.SetPaused(false);
        Tick(host);
        Assert.AreEqual("boss_intro", host.Session.Stage);
        Assert.IsTrue(staging.ShadowOnly(host, true));
        Assert.IsFalse(staging.ShadowOnly(host, false));
        Assert.AreEqual(1, staging.HealthFraction(host, false));

        for (var step = 1; step <= 120; step++)
        {
            Tick(host);
            Assert.AreEqual(Math.Min(2, step / 60d), host.Session.Boss.IntroElapsed, 1e-8);
            Assert.AreEqual(step < 18, staging.ShadowOnly(host, true));
            Assert.IsFalse(host.Session.Boss.Telegraphing);
            if (step < 18) Assert.IsNull(voice.clip);
            if (step == 18) Assert.AreEqual("sfx_boss-arrive", voice.clip?.name);
            if (step == 60) Assert.AreEqual(5 / 12d, staging.HealthFraction(host, true), 1e-6);
        }
        Assert.AreEqual("boss", host.Session.Stage);
        Assert.AreEqual(host.State.Run.MaxHealth, host.Session.CurrentHealth);
        Assert.Zero(host.Session.Boss.BossElapsed);
        AdamBossStagingFixture.ArrangeCombat(host, path, false);
        Assert.IsNull(host.Error);
        host.SetPaused(false);
        for (var step = 1; step <= (int)Math.Round(warning * 60); step++)
        {
            Tick(host);
            Assert.AreEqual(step / 60d, host.Session.Boss.BossElapsed, 1e-8);
            Assert.AreEqual(step == (int)Math.Round(warning * 60), host.Session.Boss.Telegraphing);
        }
        if (round == 12) Assert.IsTrue(AdamChariotWarning.TryBossGeometry(host.World.ChariotDash, out _));

        AdamBossStagingFixture.ArrangeCombat(host, path, true);
        Assert.IsNull(host.Error);
        host.SetPaused(false);
        Tick(host);
        Assert.IsNull(host.Error);
        Assert.AreEqual("boss_reward", host.Session.Stage);
        Assert.AreEqual(host.Session.Boss.EncounterId + ":reward", host.Session.Boss.Reward.RewardId);
        Assert.AreEqual("sfx_boss-fall", voice.clip?.name);
        Assert.AreEqual(0, staging.HeaderTick(host, true));
        Assert.AreEqual(14, staging.HeaderTick(host, false));
        // Deliberately publish the seal after stopping the clip in the test; real contention is tested in Player.
        staging.Audio.Stop();
        staging.AdvanceReward(host, .2, settings, true);
        Assert.AreEqual("sfx_boss-seal", voice.clip?.name);
        Assert.AreEqual(6, staging.HeaderTick(host, true));
        Assert.IsTrue(host.Reload());
        Tick(host);
        Assert.IsNull(voice.clip);
        Assert.AreEqual(14, staging.HeaderTick(host, true));
    }

    [Test]
    public void FailedDefeatCheckpointConsumesFallAndRetryShowsOnlyStaticHeader()
    {
        var host = AdamBossStagingFixture.BeforeIntro(path, 3);
        host.SetPaused(false);
        for (var step = 0; step <= 120; step++) Tick(host);
        AdamBossStagingFixture.ArrangeCombat(host, path, true);
        var fail = true;
        host = new AdamHost(AdamCatalog.Load(), new SaveStore(path, () =>
        {
            if (fail) throw new IOException("technical reward checkpoint failure");
        }));
        host.SetPaused(false);
        Tick(host);
        Assert.NotNull(host.Error);
        Assert.AreEqual("boss_reward", host.Session.Stage);
        Assert.IsNull(voice.clip);
        Assert.IsFalse(AdamBossStaging.ShowsCandidateHeader(host));
        fail = false;
        Assert.IsTrue(host.RetrySave());
        Tick(host);
        Assert.IsTrue(AdamBossStaging.ShowsCandidateHeader(host));
        staging.AdvanceReward(host, 1, settings, true);
        Assert.AreEqual(14, staging.HeaderTick(host, true));
        Assert.IsNull(voice.clip);

        var reward = host.Session.Boss.Reward.RewardId;
        Assert.IsFalse(staging.Audio.Due(AdamStagingCue.Fall, reward, true));
    }

    [Test]
    public void LegacyPromotionDoesNotArmAudioOrDecorateThePromotedPanel()
    {
        var host = AdamBossStagingFixture.BeforeIntro(path, 3);
        host.SetPaused(false);
        for (var step = 0; step <= 120; step++) Tick(host);
        AdamBossStagingFixture.ArrangeCombat(host, path, true);
        // Create a genuine legacy receipt for the existing promotion command without a live defeat observation.
        host.SetPaused(false);
        host.Tick(AdamBossStagingFixture.Step);
        Assert.AreEqual("boss_reward", host.Session.Stage);
        host.State.Run.Boss.Version = 1;
        var reward = host.State.Run.Boss.Reward;
        reward.PolicyId = BossDefinition.LegacyPolicy;
        reward.Candidates = null;
        reward.UnlockedWeapons = null;
        new SaveStore(path).Commit(host.State, host.State.Revision, "technical-vq03-legacy-receipt");
        Assert.IsTrue(host.Reload());
        Tick(host);
        Assert.IsTrue(host.Session.NeedsRewardImplementation);
        Assert.IsFalse(AdamBossStaging.ShowsCandidateHeader(host));
        Assert.IsTrue(host.PromoteBossReward());
        Tick(host);
        Assert.IsFalse(host.Session.NeedsRewardImplementation);
        Assert.Greater(host.Session.BossChoices.Count, 0);
        Assert.IsFalse(AdamBossStaging.ShowsCandidateHeader(host));
        staging.AdvanceReward(host, 1, settings, true);
        Assert.IsNull(voice.clip);
    }

    [TestCase(.59, 0)]
    [TestCase(.6, 1)]
    [TestCase(1.0, 5)]
    [TestCase(1.59, 11)]
    [TestCase(1.6, 12)]
    public void IntroFillHasTwelveSteps(double elapsed, int twelfths) =>
        Assert.AreEqual(twelfths / 12f, AdamBossStaging.IntroFill(elapsed));

    [TestCase(5, 0)]
    [TestCase(6, 1)]
    [TestCase(7, 1)]
    [TestCase(10, 4)]
    [TestCase(14, 8)]
    [TestCase(15, 8)]
    public void RuleUsesUiTicks(int tick, int eighths) => Assert.AreEqual(eighths, AdamBossStaging.RuleEighths(tick));

    [TestCase("sfx_boss-arrive", 9922)]
    [TestCase("sfx_boss-fall", 4410)]
    [TestCase("sfx_boss-seal", 26460)]
    public void ReviewedWavsImportAsOriginalMonoPcm(string name, int frames)
    {
        var clip = Resources.Load<AudioClip>(AdamStagingAudio.ResourceRoot + name);
        Assert.NotNull(clip);
        Assert.AreEqual(22050, clip.frequency);
        Assert.AreEqual(1, clip.channels);
        Assert.AreEqual(frames, clip.samples);
        var importer = (AudioImporter)AssetImporter.GetAtPath(AssetDatabase.GetAssetPath(clip));
        Assert.IsFalse(importer.forceToMono);
        Assert.AreEqual(AudioCompressionFormat.PCM, importer.defaultSampleSettings.compressionFormat);
        Assert.AreEqual(AudioSampleRateSetting.PreserveSampleRate, importer.defaultSampleSettings.sampleRateSetting);
        Assert.AreEqual(AudioClipLoadType.DecompressOnLoad, clip.loadType);
        var samples = new float[clip.samples];
        Assert.IsTrue(clip.GetData(samples, 0));
        Assert.AreEqual(0, samples[0]);
        Assert.AreEqual(0, samples[frames - 1]);
        Assert.LessOrEqual(samples.Max(x => Math.Abs(x)), .6f);
    }
}
