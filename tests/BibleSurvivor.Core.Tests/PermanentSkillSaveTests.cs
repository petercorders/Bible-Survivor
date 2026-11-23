using System;
using System.IO;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Runtime.Serialization.Json;

namespace BibleSurvivor.Core.Tests;

internal static class PermanentSkillSaveTests
{
    public static void Register(Action<string, Action> test)
    {
        test("Every catalog rank can be transacted, reopened, frozen into a challenge and refunded", () =>
        {
            WithSave(path =>
            {
                var store = new SaveStore(path);
                var state = SaveStore.PrepareNewChallenge(GameSnapshot.New());
                state.Profile.Gold = 378050;
                state.Profile.HighestSettledRound = 180;
                state.Profile.UnlockedWeapons.AddRange(ExpandedWeaponRules.All.Select(weapon => weapon.WeaponId));
                var nodes = PermanentSkillCatalog.Current.Nodes;
                state.Profile.EvolvedWeapons!.AddRange(nodes.Select(node => node.RequiredEvolution)
                    .Where(id => id != null).Select(id => id!).Distinct());
                var definition = WaveTests.Adam();
                _ = new AdamSession(state, definition, "bible");
                store.Commit(state, 0, "full-ledger-fixture");
                state = store.Load()!;
                var initialRunId = state.Run!.RunId;
                var count = 0;
                while (count < 442)
                {
                    var profile = state.Profile;
                    var node = nodes.First(item => item.Availability(profile.Upgrades, profile.Gold,
                        profile.HighestSettledRound!.Value, profile.UnlockedWeapons, profile.EvolvedWeapons!) ==
                        PermanentSkillAvailability.Available);
                    var rank = PermanentRules.Rank(profile.Upgrades, node.Id);
                    var command = Buy(state, node.Id, rank, node.Prices[rank]);
                    var bought = store.TransactPermanent(command);
                    Check.Equal(state.Revision + 1, bought.Revision);
                    Check.Equal(profile.Gold - node.Prices[rank], bought.Profile.Gold);
                    Check.Equal(rank + 1, bought.Profile.Upgrades[node.Id]);
                    Check.Equal(node.Prices[rank], bought.Profile.PaidRanks![node.Id][rank]);
                    Check.Equal(initialRunId, bought.Run!.RunId);
                    Check.Equal(0, bought.Run.PermanentSnapshot.Count);
                    state = bought;
                    if (++count % 32 == 0)
                    {
                        store = new SaveStore(path);
                        state = store.Load()!;
                    }
                }
                Check.Equal(420, state.Profile.Upgrades.Count);
                Check.Equal(442, state.Profile.PaidRanks!.Values.Sum(prices => prices.Count));
                Check.Equal(0L, state.Profile.Gold);
                state = SaveStore.PrepareNewChallenge(state);
                _ = new AdamSession(state, definition, "sling");
                store.Commit(state, state.Revision, "full-ledger-frozen-run");
                state = store.Load()!;
                Check.True(initialRunId != state.Run!.RunId);
                var frozen = JsonSerializer.Serialize(state.Run);
                var quote = PermanentSkillCommand.QuoteRefund(state.Profile);
                Check.Equal(378050L, quote.Gold);
                var refund = PermanentSkillCommand.RefundAll(PermanentSkillCommand.NewOperationId(state.Revision),
                    state.Revision, quote.Gold, quote.Digest);
                state = store.TransactPermanent(refund);
                Check.Equal(378050L, state.Profile.Gold);
                Check.Equal(0, state.Profile.Upgrades.Count);
                Check.Equal(0, state.Profile.PaidRanks!.Count);
                Check.Equal(frozen, JsonSerializer.Serialize(state.Run));
                store = new SaveStore(path);
                Check.Equal(state.Revision, store.TransactPermanent(refund).Revision);
                Check.Equal(442, store.Load()!.Run!.PermanentSnapshot.Values.Sum());
            });
        });

        test("Permanent transaction archives a v3 source and refunds the original price without touching the run", () =>
        {
            WithSave(path =>
            {
                File.Copy("tests/BibleSurvivor.Core.Tests/Fixtures/r180-build-complete-save.json", path);
                var store = new SaveStore(path);
                var source = store.Load()!;
                PermanentRules.Purchase(source.Profile, "pickup");
                store.Commit(source, source.Revision, "legacy-purchase");
                source = store.Load()!;
                var original = File.ReadAllBytes(path);
                var backup = File.ReadAllBytes(path + ".bak");
                var first = Buy(source, "attack", 0, 100);
                var changed = store.TransactPermanent(first);
                Check.Equal(4, changed.SchemaVersion);
                Check.Equal(source.Profile.Gold - 100, changed.Profile.Gold);
                Check.Equal(180, changed.Profile.HighestSettledRound!.Value);
                Check.True(changed.Profile.EvolvedWeapons!.Contains("sling"));
                Check.Equal("campaign", changed.Run!.CampaignLayout);
                Check.Equal(1, changed.Run.CombatRulesVersion!.Value);
                Check.Equal(source.Run!.RunId, changed.Run.RunId);
                Check.Equal(source.OldTestamentClear!.CompletedAtUnixMilliseconds, changed.OldTestamentClear!.CompletedAtUnixMilliseconds);
                var archive = Directory.GetDirectories(Path.GetDirectoryName(path)!, "*.pre-skilltree-v3-*").Single();
                Check.True(File.ReadAllBytes(Path.Combine(archive, "source.json")).SequenceEqual(original));
                Check.True(File.ReadAllBytes(Path.Combine(archive, "backup.json")).SequenceEqual(backup));
                Check.True(changed.Profile.PaidRanks!["pickup"].SequenceEqual(new[] { 60 }));

                // A historically recorded purchase can differ from the current price table.
                changed.Profile.PaidRanks["pickup"][0] = 55;
                Check.Throws<InvalidOperationException>(() =>
                    store.Commit(changed, changed.Revision, "invalid-ledger-change"));
                using (var fixture = File.Create(path))
                    new DataContractJsonSerializer(typeof(GameSnapshot)).WriteObject(fixture, changed);
                changed = store.Load()!;
                var quote = PermanentSkillCommand.QuoteRefund(changed.Profile);
                Check.Equal(155L, quote.Gold);
                var refund = PermanentSkillCommand.RefundAll(PermanentSkillCommand.NewOperationId(changed.Revision),
                    changed.Revision, quote.Gold, quote.Digest);
                var refunded = store.TransactPermanent(refund);
                Check.Equal(changed.Profile.Gold + 155, refunded.Profile.Gold);
                Check.Equal(0, refunded.Profile.Upgrades.Count);
                Check.Equal(0, refunded.Profile.PaidRanks!.Count);
                Check.Equal(changed.Run!.RunId, refunded.Run!.RunId);
                Check.Equal(changed.Run.RoundNumber, refunded.Run.RoundNumber);
                Check.Equal(changed.Run.PermanentSnapshot.Count, refunded.Run.PermanentSnapshot.Count);
                Check.Equal(180, refunded.Profile.HighestSettledRound!.Value);
                Check.Equal(refunded.Revision, new SaveStore(path).TransactPermanent(refund).Revision);
            });
        });

        test("Permanent transaction refuses stale selections and mismatching repeated operations", () =>
        {
            WithSave(path =>
            {
                var store = new SaveStore(path);
                var profile = Funded(store);
                var oldSelection = Buy(profile, "pickup", 0, 60);
                var first = Buy(profile, "attack", 0, 100);
                var bought = store.TransactPermanent(first);
                var after = File.ReadAllBytes(path);
                Check.Throws<InvalidOperationException>(() => store.TransactPermanent(oldSelection));
                Check.Throws<InvalidOperationException>(() => store.TransactPermanent(
                    PermanentSkillCommand.Purchase(first.OperationId, first.ExpectedRevision, "pickup", 0, 60)));
                Check.Throws<InvalidOperationException>(() => store.TransactPermanent(Buy(bought, "attack", 0, 100)));
                Check.Throws<InvalidOperationException>(() => store.TransactPermanent(Buy(bought, "attack", 1, 201)));
                Check.True(File.ReadAllBytes(path).SequenceEqual(after));
                Check.Equal(bought.Revision, store.TransactPermanent(first).Revision);

                store.Commit(bought, bought.Revision, "automatic-checkpoint");
                var afterAutosave = store.TransactPermanent(first);
                Check.Equal(bought.Revision + 1, afterAutosave.Revision);
                Check.Equal(bought.Profile.Gold, afterAutosave.Profile.Gold);

                var mastery = store.TransactPermanent(Buy(afterAutosave, "weapon.sling.base.01", 0, 150));
                var shot = store.TransactPermanent(Buy(mastery, "weapon.sling.base.03", 0, 350));
                Check.Equal(0L, shot.Profile.Gold);
                Check.Throws<InvalidOperationException>(() => store.TransactPermanent(first));
            });
        });

        test("Permanent transaction retries archive, pre-replace and confirmation failures once", () =>
        {
            foreach (var failure in new[] { "archive", "replace", "confirm" })
            {
                WithSave(path =>
                {
                    var healthy = new SaveStore(path);
                    var profile = Funded(healthy);
                    var before = File.ReadAllBytes(path);
                    var request = Buy(profile, "pickup", 0, 60);
                    Action fail = () => throw new IOException("Injected " + failure);
                    var store = new SaveStore(path, failure == "replace" ? fail : null,
                        failure == "archive" ? fail : null, failure == "confirm" ? fail : null);
                    Check.Throws<IOException>(() => store.TransactPermanent(request));
                    if (failure != "confirm") Check.True(File.ReadAllBytes(path).SequenceEqual(before));
                    var reopened = new SaveStore(path);
                    var confirmed = reopened.TransactPermanent(request);
                    Check.Equal(540L, confirmed.Profile.Gold);
                    Check.Equal(profile.Revision + 1, confirmed.Revision);
                    Check.Equal(request.OperationId, reopened.Load()!.Profile.LastPermanentSkillReceipt!.OperationId);
                    Check.Equal(1, Directory.GetDirectories(Path.GetDirectoryName(path)!, "*.pre-skilltree-v2-*").Length);
                });
            }
        });

        test("Permanent migration preserves active schema 1 and 2 runs and representative campaign saves", () =>
        {
            foreach (var (fixture, version, round) in new[]
            {
                ("genesis-schema1-sling-r2-t120.json", 1, 1),
                ("genesis-schema2-sling-r2-t120.json", 2, 1),
                ("r60-build-complete-save.json", 3, 60),
                ("r170-build-complete-save.json", 3, 170),
            })
            {
                WithSave(path =>
                {
                    File.Copy("tests/BibleSurvivor.Core.Tests/Fixtures/" + fixture, path);
                    var store = new SaveStore(path);
                    var original = File.ReadAllBytes(path);
                    var loaded = store.Load()!;
                    Check.True(File.ReadAllBytes(path).SequenceEqual(original));
                    var projected = SaveStore.Copy(loaded);
                    PermanentProfile.Upgrade(projected);
                    store.Commit(projected, projected.Revision, "skill-migration");
                    var saved = store.Load()!;
                    Check.Equal(4, saved.SchemaVersion);
                    Check.Equal(round, saved.Profile.HighestSettledRound!.Value);
                    Check.Equal(loaded.Profile.Gold, saved.Profile.Gold);
                    Check.Equal(JsonSerializer.Serialize(loaded.Profile.UnlockedWeapons),
                        JsonSerializer.Serialize(saved.Profile.UnlockedWeapons));
                    Check.Equal(JsonSerializer.Serialize(projected.Run), JsonSerializer.Serialize(saved.Run));
                    Check.Equal(loaded.Run!.RunId, saved.Run!.RunId);
                    var archive = Directory.GetDirectories(Path.GetDirectoryName(path)!, "*.pre-skilltree-v" + version + "-*").Single();
                    Check.True(File.ReadAllBytes(Path.Combine(archive, "source.json")).SequenceEqual(original));
                    if (version == 1) Check.True(File.ReadAllBytes(Path.Combine(path + ".pre-v2", "primary.json")).SequenceEqual(original));
                });
            }
        });

        test("Permanent transaction retry refuses to confirm from an older backup", () =>
        {
            WithSave(path =>
            {
                var store = new SaveStore(path);
                var state = Funded(store);
                var cmd = Buy(state, "pickup", 0, 60);
                var confirmed = store.TransactPermanent(cmd);
                var primary = File.ReadAllBytes(path);
                var backup = File.ReadAllBytes(path + ".bak");
                File.Move(path, path + ".unavailable");
                Check.Throws<IOException>(() => new SaveStore(path).TransactPermanent(cmd));
                Check.True(File.ReadAllBytes(path + ".bak").SequenceEqual(backup));
                File.Move(path + ".unavailable", path);
                Check.Equal(confirmed.Revision, new SaveStore(path).TransactPermanent(cmd).Revision);
                Check.True(File.ReadAllBytes(path).SequenceEqual(primary));
            });
        });

        test("Permanent migration reader protects future content while recovering a malformed supported run", () =>
        {
            WithSave(path =>
            {
                File.Copy("tests/BibleSurvivor.Core.Tests/Fixtures/genesis-schema2-sling-r2-t120.json", path);
                var store = new SaveStore(path);
                var current = store.Load()!;
                PermanentProfile.Upgrade(current);
                store.Commit(current, current.Revision, "migrate");
                var supported = File.ReadAllText(path);
                var broken = JsonNode.Parse(supported)!.AsObject();
                broken["Run"]!.AsObject().Remove("CombatRulesVersion");
                File.WriteAllText(path, broken.ToJsonString());
                Check.Equal(2, store.Load()!.SchemaVersion);
                broken["Run"]!["World"]!["SchemaVersion"] = 999;
                File.WriteAllText(path, broken.ToJsonString());
                Check.Throws<UnsupportedSaveVersionException>(() => store.Load());
                broken["Run"]!["World"]!["SchemaVersion"] = 1;
                broken["Run"]!["NewCombatState"] = true;
                File.WriteAllText(path, broken.ToJsonString());
                Check.Throws<UnsupportedSaveVersionException>(() => store.Load());
            });
        });
    }

    private static PermanentSkillCommand Buy(GameSnapshot snapshot, string id, int rank, int price) =>
        PermanentSkillCommand.Purchase(PermanentSkillCommand.NewOperationId(snapshot.Revision), snapshot.Revision, id, rank, price);

    private static GameSnapshot Funded(SaveStore store)
    {
        var state = GameSnapshot.New();
        state.Profile.Gold = 600;
        store.Commit(state, 0, "fund-legacy-profile");
        return store.Load()!;
    }

    private static void WithSave(Action<string> test)
    {
        var directory = Path.Combine(Path.GetTempPath(), "bible-skill-save-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        try { test(Path.Combine(directory, "profile.json")); }
        finally { Directory.Delete(directory, true); }
    }
}
