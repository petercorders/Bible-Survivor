using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Json;
using System.Text;
using System.Text.Json.Nodes;
using BibleSurvivor.Core;

namespace BibleSurvivor.Core.Tests;

internal static class SaveSchemaTests
{
    public static void Register(Action<string, Action> test)
    {
        test("Schema2 writer and memory migration keep schema1 original bytes untouched on Load", () => WithStore(store => {
            var state = GameSnapshot.New(); Check.Equal(2, state.SchemaVersion);
            var legacy = Legacy(state); File.WriteAllText(store.Path, legacy);
            var loaded = store.Load()!; Check.Equal(2, loaded.SchemaVersion); Check.Equal(legacy, File.ReadAllText(store.Path));
            Check.True(!Directory.Exists(store.Path + ".pre-v2"));
            store.Commit(loaded, 0, "upgrade"); Check.Equal(2, Version(File.ReadAllText(store.Path)));
            Check.Equal(legacy, File.ReadAllText(store.Path + ".pre-v2/primary.json"));
            Check.True(!File.Exists(store.Path + ".pre-v2/backup.json"));
            Check.True(File.ReadAllText(store.Path + ".pre-v2/manifest.json").Contains("\"Validity\":\"valid\""));
        }));
        test("Schema1 R01 and R02 active and between checkpoints resume without fingerprint or economy changes", () => {
            foreach (var round in new[] { 1, 2 }) foreach (var completed in new[] { false, true }) WithStore(store => {
                var state = GameSnapshot.New();
                var session = new AdamSession(state, WaveTests.Adam(), "sling");
                if (round == 2) { session.Advance(180); session = session.StartNextRound(BibleSurvivor.Sim.CatalogReader.Round(Path.Combine(Directory.GetCurrentDirectory(), "content/genesis/catalog.json"), 2)); }
                if (completed) session.Advance(180); else session.Advance(3);
                session.Capture();
                var before = JsonNode.Parse(Encode(state))!; before["SchemaVersion"] = 2;
                var legacy = Legacy(state); File.WriteAllText(store.Path, legacy);
                var loaded = store.Load()!;
                Check.Equal(before.ToJsonString(), JsonNode.Parse(Encode(loaded))!.ToJsonString());
                var resumed = AdamSession.Resume(loaded, round == 1 ? WaveTests.Adam() : BibleSurvivor.Sim.CatalogReader.Round(Path.Combine(Directory.GetCurrentDirectory(), "content/genesis/catalog.json"), 2));
                Check.Equal(state.Run!.RoundFingerprint, loaded.Run!.RoundFingerprint);
                Check.Equal(state.Profile.Gold, loaded.Profile.Gold); Check.Equal(state.Run.Stage, loaded.Run.Stage);
                Check.Equal(legacy, File.ReadAllText(store.Path));
                store.Commit(loaded, 0, "upgrade"); Check.Equal(2, store.Load()!.SchemaVersion);
                Check.True(resumed != null);
            });
        });
        test("Future schema header precedes incompatible typed Run and never falls back to schema1 backup", () => WithStore(store => {
            var backup = Legacy(GameSnapshot.New()); File.WriteAllText(store.Path + ".bak", backup);
            foreach (var run in new[] { "42", "[]", "{\"Wave\":\"new-shape\"}" }) {
                var future = "{\"Run\":" + run + ",\"SchemaVersion\":4,\"Profile\":false}";
                File.WriteAllText(store.Path, future);
                Check.Throws<UnsupportedSaveVersionException>(() => store.Load());
                Check.Throws<UnsupportedSaveVersionException>(() => store.Commit(GameSnapshot.New(), 0, "overwrite"));
                Check.Equal(future, File.ReadAllText(store.Path)); Check.Equal(backup, File.ReadAllText(store.Path + ".bak"));
            }
            Check.True(!Directory.Exists(store.Path + ".pre-v2"));
        }));
        test("Schema2 reader preserves future boss content instead of recovering older normal backup", () => WithStore(store => {
            File.WriteAllText(store.Path + ".bak", Legacy(GameSnapshot.New()));
            var state = GameSnapshot.New(); _ = new AdamSession(state, WaveTests.Adam(), "bible");
            foreach (var change in new Action<JsonNode>[] {
                n => n["Run"]!["Boss"] = new JsonObject { ["FutureBoss"] = true },
                n => n["Run"]!["Stage"] = "future_boss_stage",
                n => n["Run"]!["RoundNumber"] = 10,
                n => { n["Run"]!["RoundNumber"] = 11; n["Run"]!["Stage"] = "between_rounds"; },
                n => n["NewBossEnvelope"] = new JsonObject()
            }) {
                var node = JsonNode.Parse(Encode(state))!; change(node); var original = node.ToJsonString(); File.WriteAllText(store.Path, original);
                Check.Throws<UnsupportedSaveVersionException>(() => store.Load());
                Check.Throws<UnsupportedSaveVersionException>(() => store.Commit(GameSnapshot.New(), 0, "overwrite"));
                Check.Equal(original, File.ReadAllText(store.Path));
            }
        }));
        test("Schema2 nested Profile unknown content is preserved instead of silently discarded on commit", () => {
            AssertUnsupportedNested(n => n["Profile"]!["BossTokens"] = 7);
        });
        test("Schema2 nested World unknown content is preserved instead of silently discarded on commit", () => {
            AssertUnsupportedNested(n => n["Run"]!["World"]!["BossPosition"] = new JsonObject { ["X"] = 3, ["Y"] = 2 });
        });
        test("Future World version preserves Gold19 primary instead of replacing it with Gold0 legacy backup", () => {
            foreach (var schema in new[] { 1, 2 }) WithStore(store => {
                var old = CombatState(); old.Profile.Gold = 0;
                var backup = Legacy(old); File.WriteAllText(store.Path + ".bak", backup);
                var future = JsonNode.Parse(Encode(old))!; future["SchemaVersion"] = schema;
                future["Profile"]!["Gold"] = 19; future["Run"]!["World"]!["SchemaVersion"] = 5;
                var original = future.ToJsonString(); File.WriteAllText(store.Path, original);
                AssertFutureOriginals(store, original, backup);
            });
        });
        test("Future World version and overflow precede incompatible typed bodies and protect future backups", () => {
            foreach (var schema in new[] { 1, 2 }) foreach (var version in new[] { "5", "999999999999999999999999999999" }) WithStore(store => {
                var future = JsonNode.Parse(Encode(CombatState()))!; future["SchemaVersion"] = schema;
                future["Run"]!["World"]!["Tuning"] = "future tuning shape";
                future["Run"]!["Wave"] = "future wave shape";
                future["Run"]!["UnknownFutureRun"] = true;
                future["UnknownFutureEnvelope"] = true;
                var original = future.ToJsonString();
                // Change only the nested version token; keep the envelope version in its own domain.
                var worldVersion = future["Run"]!["World"]!["SchemaVersion"]!.ToJsonString();
                var worldJson = future["Run"]!["World"]!.ToJsonString();
                var futureWorld = worldJson.Replace("\"SchemaVersion\":" + worldVersion, "\"SchemaVersion\":" + version);
                original = original.Replace(worldJson, futureWorld);
                Check.True(original.Contains("\"SchemaVersion\":" + version));
                File.WriteAllText(store.Path + ".bak", Legacy(GameSnapshot.New())); File.WriteAllText(store.Path, original);
                AssertFutureOriginals(store, original, File.ReadAllText(store.Path + ".bak"));
                File.Delete(store.Path); File.WriteAllText(store.Path + ".bak", original);
                Check.Throws<UnsupportedSaveVersionException>(() => store.Load());
                Check.Throws<UnsupportedSaveVersionException>(() => store.Commit(GameSnapshot.New(), 0, "overwrite-backup"));
                Check.True(!File.Exists(store.Path)); Check.Equal(original, File.ReadAllText(store.Path + ".bak"));
                File.WriteAllText(store.Path, "corrupt primary");
                AssertFutureOriginals(store, "corrupt primary", original);
            });
        });
        test("Malformed World versions remain corruption while current World1 and legacy migration stay supported", () => {
            foreach (var schema in new[] { 1, 2 }) foreach (var invalid in new[] {
                "\"SchemaVersion\":0", "\"SchemaVersion\":-1", "\"SchemaVersion\":\"2\"", "\"SchemaVersion\":true",
                "\"SchemaVersion\":null", "\"SchemaVersion\":1.0", "\"SchemaVersion\":1e0",
                "\"SchemaVersion\":2,\"SchemaVersion\":1", "\"MissingWorldVersion\":1"
            }) WithStore(store => {
                var state = CombatState(); state.Profile.Gold = 19;
                var node = JsonNode.Parse(Encode(state))!; node["SchemaVersion"] = schema;
                var world = node["Run"]!["World"]!.ToJsonString();
                var malformed = node.ToJsonString().Replace(world, world.Replace("\"SchemaVersion\":1", invalid));
                Check.True(malformed != node.ToJsonString()); File.WriteAllText(store.Path, malformed);
                Check.Throws<InvalidDataException>(() => store.Load()); Check.Equal(malformed, File.ReadAllText(store.Path));
                var old = CombatState(); old.Profile.Gold = 0; var backup = Legacy(old); File.WriteAllText(store.Path + ".bak", backup);
                Check.Equal(0L, store.Load()!.Profile.Gold); Check.Equal(malformed, File.ReadAllText(store.Path));
                Check.Equal(backup, File.ReadAllText(store.Path + ".bak")); Check.True(!Directory.Exists(store.Path + ".pre-v2"));
            });
        });
        test("Schema2 nested collection DTOs and dictionary entries preserve unknown content", () => {
            foreach (var change in new Action<JsonNode>[] {
                n => n["Run"]!["World"]!["Tuning"]!["BossRadius"] = .8,
                n => n["Run"]!["World"]!["Weapons"]![0]!["WeaponForm"] = "future-form",
                n => n["Run"]!["World"]!["Enemies"]![0]!["BossActor"] = true,
                n => n["Run"]!["World"]!["MonsterSpeeds"]![0]!["BossRatio"] = .3,
                n => n["Run"]!["Wave"]!["Active"]![0]!["BossPhase"] = 1,
                n => n["Profile"]!["Upgrades"]![0]!["Bonus"] = 1,
                n => n["Run"]!["Weapons"]![0]!["Evolution"] = "future-form"
            }) AssertUnsupportedNested(change);
        });
        test("Schema2 nested duplicate DTO members are corrupt without losing either original file", () => WithStore(store => {
            var json = Encode(CombatState());
            foreach (var duplicate in new[] {
                json.Replace("\"Profile\":{", "\"Profile\":{\"Gold\":0,"),
                json.Replace("\"Tuning\":{", "\"Tuning\":{\"PlayerSpeed\":3,")
            }) {
                Check.True(duplicate != json); File.WriteAllText(store.Path, duplicate);
                Check.Throws<InvalidDataException>(() => store.Load());
                Check.Equal(duplicate, File.ReadAllText(store.Path));
                Check.True(!File.Exists(store.Path + ".bak")); Check.True(!Directory.Exists(store.Path + ".pre-v2"));
            }
        }));
        test("Nested contract gate accepts dictionary game keys and optional absent or null world", () => WithStore(store => {
            var state = CombatState(); store.Commit(state, 0, "world");
            var loaded = store.Load()!;
            Check.Equal(1, loaded.Profile.Upgrades["attack"]);
            Check.Equal(1, loaded.Run!.Weapons["sling"]);
            Check.Equal(1, loaded.Run.PermanentSnapshot["attack"]);
            Check.Equal(Encode(state).Replace("\"Revision\":0", "\"Revision\":1").Replace("\"LastOperationId\":\"\"", "\"LastOperationId\":\"world\""), Encode(loaded));
            foreach (var removeWorld in new[] { false, true }) {
                var node = JsonNode.Parse(Encode(state))!;
                if (removeWorld) node["Run"]!.AsObject().Remove("World"); else node["Run"]!["World"] = null;
                File.WriteAllText(store.Path, node.ToJsonString()); Check.True(store.Load()!.Run!.World == null);
            }
        }));
        test("Schema1 keeps its previous nested extension compatibility during migration", () => WithStore(store => {
            var node = JsonNode.Parse(Legacy(CombatState()))!;
            node["Profile"]!["LegacyExtra"] = 7;
            node["Run"]!["World"]!["LegacyWorldExtra"] = new JsonObject { ["Value"] = 2 };
            var original = node.ToJsonString(); File.WriteAllText(store.Path, original);
            var loaded = store.Load()!; Check.Equal(2, loaded.SchemaVersion);
            Check.Equal(original, File.ReadAllText(store.Path));
            store.Commit(loaded, 0, "legacy-upgrade");
            Check.Equal(original, File.ReadAllText(store.Path + ".pre-v2/primary.json"));
        }));
        test("Future backup is refused when primary is corrupt or absent", () => WithStore(store => {
            File.WriteAllText(store.Path + ".bak", "{\"Run\":42,\"SchemaVersion\":4}");
            Check.Throws<UnsupportedSaveVersionException>(() => store.Load());
            File.WriteAllText(store.Path, "broken"); Check.Throws<UnsupportedSaveVersionException>(() => store.Load());
        }));
        test("Malformed version fields and schema1 boss or duplicate members are corruption, never implicit migration", () => WithStore(store => {
            var valid = Legacy(GameSnapshot.New());
            foreach (var replacement in new[] { "\"SchemaVersion\":0", "\"SchemaVersion\":-1", "\"SchemaVersion\":true", "\"SchemaVersion\":\"1\"", "\"SchemaVersion\":1.0", "\"SchemaVersion\":1e0", "\"SchemaVersion\":null", "\"SchemaVersion\":1,\"SchemaVersion\":1", "\"Missing\":1" }) {
                var malformed = valid.Replace("\"SchemaVersion\":1", replacement); File.WriteAllText(store.Path, malformed);
                Check.Throws<InvalidDataException>(() => store.Load()); Check.Equal(malformed, File.ReadAllText(store.Path));
            }
            var state = GameSnapshot.New(); _ = new AdamSession(state, WaveTests.Adam(), "bible");
            foreach (var field in new[] { "\"Boss\":{}", "\"BossReward\":{}", "\"Stage\":\"normal\"" }) {
                var node = JsonNode.Parse(Legacy(state))!; var json = node.ToJsonString();
                var marker = "\"Run\":{"; var bad = json.Replace(marker, marker + field + ",");
                File.WriteAllText(store.Path, bad); Check.Throws<InvalidDataException>(() => store.Load());
            }
        }));
        test("Known malformed version can recover backup without rewriting primary", () => WithStore(store => {
            File.WriteAllText(store.Path + ".bak", Legacy(GameSnapshot.New()));
            var bad = "{\"SchemaVersion\":true}"; File.WriteAllText(store.Path, bad);
            Check.Equal(2, store.Load()!.SchemaVersion); Check.Equal(bad, File.ReadAllText(store.Path));
        }));
        test("Schema upgrade archives primary and backup bytes hashes validity once and does not replace archive later", () => WithStore(store => {
            var older = GameSnapshot.New(); older.Revision = 4; older.Profile.Gold = 20;
            var latest = GameSnapshot.New(); latest.Revision = 5; latest.Profile.Gold = 30;
            var primary = Legacy(latest); var backup = Legacy(older);
            File.WriteAllText(store.Path, primary); File.WriteAllText(store.Path + ".bak", backup);
            var loaded = store.Load()!; loaded.Profile.Gold++;
            Check.Equal(6L, store.Commit(loaded, 5, "upgrade"));
            Check.Equal(primary, File.ReadAllText(store.Path + ".pre-v2/primary.json"));
            Check.Equal(backup, File.ReadAllText(store.Path + ".pre-v2/backup.json"));
            var archive = Directory.GetFiles(store.Path + ".pre-v2").ToDictionary(x => x, File.ReadAllText);
            Check.Equal(6L, store.Commit(loaded, 5, "upgrade"));
            loaded = store.Load()!; loaded.Profile.Gold++; store.Commit(loaded, 6, "later");
            foreach (var item in archive) Check.Equal(item.Value, File.ReadAllText(item.Key));
            var manifest = JsonNode.Parse(archive[store.Path + ".pre-v2/manifest.json"])!;
            foreach (var item in manifest["Files"]!.AsArray()) {
                var name = item!["Name"]!.GetValue<string>();
                var hash = Convert.ToHexString(System.Security.Cryptography.SHA256.HashData(File.ReadAllBytes(store.Path + ".pre-v2/" + name))).ToLowerInvariant();
                Check.Equal(hash, item["Sha256"]!.GetValue<string>()); Check.Equal("valid", item["Validity"]!.GetValue<string>());
            }
        }));
        test("Schema upgrade preserves corrupt primary and valid legacy backup provenance", () => WithStore(store => {
            File.WriteAllText(store.Path, "corrupt original"); var backup = Legacy(GameSnapshot.New()); File.WriteAllText(store.Path + ".bak", backup);
            store.Commit(store.Load()!, 0, "recover-upgrade");
            Check.Equal("corrupt original", File.ReadAllText(store.Path + ".pre-v2/primary.json"));
            Check.Equal(backup, File.ReadAllText(store.Path + ".pre-v2/backup.json"));
            Check.Equal(backup, File.ReadAllText(store.Path + ".bak"));
            Check.True(File.ReadAllText(store.Path + ".pre-v2/manifest.json").Contains("\"Validity\":\"invalid\""));
        }));
        test("Schema archive path failure and incomplete or altered archive block upgrade before replacing originals", () => {
            foreach (var mode in new[] { "file", "incomplete", "altered" }) WithStore(store => {
                var original = Legacy(GameSnapshot.New()); File.WriteAllText(store.Path, original);
                var archive = store.Path + ".pre-v2";
                if (mode == "file") File.WriteAllText(archive, "occupied");
                else { Directory.CreateDirectory(archive); if (mode == "altered") File.WriteAllText(archive + "/primary.json", "wrong original"); }
                if (mode == "file") Check.Throws<IOException>(() => store.Commit(store.Load()!, 0, "upgrade"));
                else Check.Throws<InvalidDataException>(() => store.Commit(store.Load()!, 0, "upgrade"));
                Check.Equal(original, File.ReadAllText(store.Path)); Check.True(!File.Exists(store.Path + ".bak"));
                Check.Equal(0, Directory.GetFiles(Path.GetDirectoryName(store.Path)!, "*.tmp-*").Length);
                Check.Equal(0, Directory.GetDirectories(Path.GetDirectoryName(store.Path)!, "*.pre-v2.tmp-*").Length);
            });
        });
        test("Schema completed archive remains the initial original when a later legacy writer changes the source", () => WithStore(store => {
            var original = Legacy(GameSnapshot.New()); File.WriteAllText(store.Path, original);
            var broken = new SaveStore(store.Path, () => throw new IOException("replace failed"));
            Check.Throws<IOException>(() => broken.Commit(store.Load()!, 0, "first-upgrade"));
            var savedArchive = File.ReadAllBytes(store.Path + ".pre-v2/primary.json");
            var different = GameSnapshot.New(); different.Profile.Gold = 11; var changed = Legacy(different); File.WriteAllText(store.Path, changed);
            Check.Equal(1L, store.Commit(store.Load()!, 0, "different-upgrade"));
            Check.Equal(changed, File.ReadAllText(store.Path + ".bak")); Check.Equal(11L, store.Load()!.Profile.Gold);
            Check.True(savedArchive.SequenceEqual(File.ReadAllBytes(store.Path + ".pre-v2/primary.json")));
        }));
        test("Schema backup-only migration archives the actual backup without inventing a primary original", () => WithStore(store => {
            var backup = Legacy(GameSnapshot.New()); File.WriteAllText(store.Path + ".bak", backup);
            Check.Equal(2, store.Load()!.SchemaVersion); Check.True(!File.Exists(store.Path));
            Check.Equal(1L, store.Commit(store.Load()!, 0, "backup-only"));
            Check.Equal(backup, File.ReadAllText(store.Path + ".pre-v2/backup.json"));
            Check.True(!File.Exists(store.Path + ".pre-v2/primary.json")); Check.Equal(2, Version(File.ReadAllText(store.Path)));
        }));
        test("Schema2 corrupt primary can recover its schema1 backup while verifying the initial archive against itself", () => WithStore(store => {
            var legacy = Legacy(GameSnapshot.New()); File.WriteAllText(store.Path, legacy);
            var pending = store.Load()!; pending.Profile.Gold = 5; store.Commit(pending, 0, "upgrade");
            var archive = Directory.GetFiles(store.Path + ".pre-v2").ToDictionary(x => x, File.ReadAllBytes);
            File.WriteAllText(store.Path, "later corrupt v2 primary"); var recovered = store.Load()!;
            Check.Equal(0L, recovered.Profile.Gold); Check.Equal(1L, store.Commit(recovered, 0, "recover"));
            Check.Equal(2, Version(File.ReadAllText(store.Path))); Check.Equal(legacy, File.ReadAllText(store.Path + ".bak"));
            foreach (var file in archive) Check.True(file.Value.SequenceEqual(File.ReadAllBytes(file.Key)));
        }));
        test("Schema completed archive with changed original or manifest provenance rejects migration", () => {
            foreach (var mode in new[] { "bytes", "hash", "validity", "schema", "extra" }) WithStore(store => {
                var legacy = Legacy(GameSnapshot.New()); File.WriteAllText(store.Path, legacy);
                var broken = new SaveStore(store.Path, () => throw new IOException("replace failed"));
                Check.Throws<IOException>(() => broken.Commit(store.Load()!, 0, "upgrade"));
                var folder = store.Path + ".pre-v2";
                if (mode == "bytes") File.WriteAllText(folder + "/primary.json", "tampered");
                else if (mode == "extra") File.WriteAllText(folder + "/extra.json", "extra");
                else {
                    var json = JsonNode.Parse(File.ReadAllText(folder + "/manifest.json"))!;
                    if (mode == "schema") json["Files"]![0]!["SchemaVersion"] = 99;
                    else json["Files"]![0]![mode == "hash" ? "Sha256" : "Validity"] = "wrong";
                    File.WriteAllText(folder + "/manifest.json", json.ToJsonString());
                }
                Check.Throws<InvalidDataException>(() => store.Commit(store.Load()!, 0, "upgrade"));
                Check.Equal(legacy, File.ReadAllText(store.Path)); Check.True(!File.Exists(store.Path + ".bak"));
            });
        });
        test("Schema archive publication failure cleans partial files and preserves primary and backup for retry", () => WithStore(store => {
            var original = Legacy(GameSnapshot.New()); File.WriteAllText(store.Path, original); File.WriteAllText(store.Path + ".bak", original);
            var pending = store.Load()!; pending.Profile.Gold = 9;
            var broken = new SaveStore(store.Path, beforeMigrationArchivePublish: () => throw new IOException("archive disk failure"));
            Check.Throws<IOException>(() => broken.Commit(pending, 0, "upgrade"));
            Check.Equal(original, File.ReadAllText(store.Path)); Check.Equal(original, File.ReadAllText(store.Path + ".bak"));
            Check.True(!Directory.Exists(store.Path + ".pre-v2"));
            Check.Equal(0, Directory.GetDirectories(Path.GetDirectoryName(store.Path)!, "*.pre-v2.tmp-*").Length);
            Check.Equal(1L, store.Commit(pending, 0, "upgrade")); Check.Equal(9L, store.Load()!.Profile.Gold);
        }));
        test("Schema archive complete but replace failure supports identical retry and rejects stale payload", () => WithStore(store => {
            var original = Legacy(GameSnapshot.New()); File.WriteAllText(store.Path, original);
            var pending = store.Load()!; pending.Profile.Gold = 7;
            var broken = new SaveStore(store.Path, () => throw new IOException("staged replace failure"));
            Check.Throws<IOException>(() => broken.Commit(pending, 0, "upgrade")); Check.Equal(original, File.ReadAllText(store.Path));
            var archived = File.ReadAllText(store.Path + ".pre-v2/manifest.json");
            Check.Equal(1L, store.Commit(pending, 0, "upgrade")); Check.Equal(1L, store.Commit(pending, 0, "upgrade"));
            Check.Equal(7L, store.Load()!.Profile.Gold); Check.Equal(archived, File.ReadAllText(store.Path + ".pre-v2/manifest.json"));
            pending.Profile.Gold = 8; Check.Throws<InvalidOperationException>(() => store.Commit(pending, 0, "upgrade"));
        }));
        test("Future schema integer overflow remains protected with incompatible Run and older backup", () => WithStore(store => {
            File.WriteAllText(store.Path + ".bak", Legacy(GameSnapshot.New()));
            var future = "{\"SchemaVersion\":999999999999999999999999999999,\"Run\":42}";
            File.WriteAllText(store.Path, future); Check.Throws<UnsupportedSaveVersionException>(() => store.Load());
            Check.Equal(future, File.ReadAllText(store.Path));
        }));

    }
    private static GameSnapshot CombatState()
    {
        var state = GameSnapshot.New(); state.Profile.Upgrades["attack"] = 1;
        var session = new AdamSession(state, WaveTests.Adam(), "sling");
        var world = new BattleWorld(session, new Dictionary<string, double> { ["a1"] = .45, ["a2"] = .6, ["a3"] = .35 }, 13);
        world.Advance(3, System.Numerics.Vector2.UnitX); session.Capture(); state.Run!.World = world.Capture();
        state.Validate(); Check.True(state.Run.Wave!.Active.Count > 0); Check.True(state.Run.World.Enemies.Count > 0);
        return state;
    }
    private static void AssertUnsupportedNested(Action<JsonNode> change)
    {
        WithStore(store => {
            var backup = Legacy(GameSnapshot.New()); File.WriteAllText(store.Path + ".bak", backup);
            var node = JsonNode.Parse(Encode(CombatState()))!; change(node); var original = node.ToJsonString();
            File.WriteAllText(store.Path, original);
            Check.Throws<UnsupportedSaveVersionException>(() => store.Load());
            Check.Throws<UnsupportedSaveVersionException>(() => store.Commit(GameSnapshot.New(), 0, "overwrite"));
            Check.Equal(original, File.ReadAllText(store.Path)); Check.Equal(backup, File.ReadAllText(store.Path + ".bak"));
            Check.True(!Directory.Exists(store.Path + ".pre-v2"));
            Check.Equal(0, Directory.GetFiles(Path.GetDirectoryName(store.Path)!, "*.tmp-*").Length);
        });
    }
    private static void AssertFutureOriginals(SaveStore store, string primary, string backup)
    {
        Check.Throws<UnsupportedSaveVersionException>(() => store.Load());
        Check.Throws<UnsupportedSaveVersionException>(() => store.Commit(GameSnapshot.New(), 0, "overwrite"));
        Check.Equal(primary, File.ReadAllText(store.Path)); Check.Equal(backup, File.ReadAllText(store.Path + ".bak"));
        Check.True(!Directory.Exists(store.Path + ".pre-v2"));
        Check.Equal(0, Directory.GetFiles(Path.GetDirectoryName(store.Path)!, "*.tmp-*").Length);
    }
    private static string Encode(GameSnapshot state)
    {
        using var stream = new MemoryStream(); new DataContractJsonSerializer(typeof(GameSnapshot)).WriteObject(stream, state);
        return Encoding.UTF8.GetString(stream.ToArray());
    }
    private static string Legacy(GameSnapshot state)
    { var node = JsonNode.Parse(Encode(state))!; node["SchemaVersion"] = 1; return node.ToJsonString(); }
    private static int Version(string json) => JsonNode.Parse(json)!["SchemaVersion"]!.GetValue<int>();
    private static void WithStore(Action<SaveStore> test)
    {
        var directory = Path.Combine(Path.GetTempPath(), "bible-schema-tests-" + Guid.NewGuid().ToString("N")); Directory.CreateDirectory(directory);
        try { test(new SaveStore(Path.Combine(directory, "save.json"))); } finally { Directory.Delete(directory, true); }
    }
}
