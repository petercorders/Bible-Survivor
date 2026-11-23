using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Security.Cryptography;

namespace BibleSurvivor.Core
{
    // Archive all existing bytes before the first schema-2 replacement. Build a complete
    // sibling directory and publish it with one rename, so a failed backup copy cannot leave
    // an apparently complete archive. A retry verifies the original bytes instead of overwriting.
    internal static class SaveMigrationArchive
    {
        internal static void Ensure(string path, Action? beforePublish)
        {
            var files = new Dictionary<string, byte[]>();
            var metadata = new ArchiveMetadata();
            foreach (var pair in new[] { (Source: path, Name: "primary.json"), (Source: path + ".bak", Name: "backup.json") })
            {
                if (!File.Exists(pair.Source)) continue;
                var info = new FileInfo(pair.Source);
                if (info.Length > SaveFormat.MaximumBytes) throw new InvalidDataException("Legacy source too large to archive");
                var bytes = File.ReadAllBytes(pair.Source);
                files.Add(pair.Name, bytes);
                metadata.Files.Add(Describe(pair.Name, bytes, maximumVersion: 2));
            }
            if (files.Count == 0) throw new InvalidDataException("Legacy migration requires original bytes");
            using (var output = new MemoryStream())
            {
                new DataContractJsonSerializer(typeof(ArchiveMetadata)).WriteObject(output, metadata);
                files.Add("manifest.json", output.ToArray());
            }
            var destination = path + ".pre-v2";
            if (Directory.Exists(destination)) { VerifyPublishedArchive(destination); return; }
            if (File.Exists(destination)) throw new IOException("Legacy archive path already exists as a file");
            Publish(destination, files, beforePublish);
        }

        internal static void EnsureSkillTree(string path, int sourceVersion, byte[] source, Action? beforePublish)
        {
            var files = new Dictionary<string, byte[]> { ["source.json"] = source };
            if (File.Exists(path + ".bak"))
            {
                if (new FileInfo(path + ".bak").Length > SaveFormat.MaximumBytes)
                    throw new InvalidDataException("Legacy backup too large to archive");
                files.Add("backup.json", File.ReadAllBytes(path + ".bak"));
            }
            var validatedSource = Describe("source.json", source);
            if (validatedSource.Validity != "valid" || validatedSource.SchemaVersion != sourceVersion)
                throw new InvalidDataException("Permanent skill migration requires a validated original");
            var metadata = new ArchiveMetadata { Files = new List<ArchiveFile> { validatedSource } };
            if (files.TryGetValue("backup.json", out var backup))
                metadata.Files.Add(Describe("backup.json", backup));
            using var output = new MemoryStream();
            new DataContractJsonSerializer(typeof(ArchiveMetadata)).WriteObject(output, metadata);
            var manifest = output.ToArray();
            files.Add("manifest.json", manifest);
            var destination = path + ".pre-skilltree-v" + sourceVersion + "-" + Hash(manifest);
            if (Directory.Exists(destination)) { Verify(destination, files); return; }
            Publish(destination, files, beforePublish);
        }

        private static void Publish(string destination, Dictionary<string, byte[]> files, Action? beforePublish)
        {
            var temporary = destination + ".tmp-" + Guid.NewGuid().ToString("N");
            Directory.CreateDirectory(temporary);
            try
            {
                foreach (var file in files)
                {
                    using var output = new FileStream(System.IO.Path.Combine(temporary, file.Key), FileMode.CreateNew, FileAccess.Write, FileShare.None);
                    output.Write(file.Value, 0, file.Value.Length); output.Flush(true);
                }
                Verify(temporary, files);
                beforePublish?.Invoke();
                Directory.Move(temporary, destination);
            }
            finally { if (Directory.Exists(temporary)) Directory.Delete(temporary, true); }
        }

        private static void VerifyPublishedArchive(string directory)
        {
            var manifest = System.IO.Path.Combine(directory, "manifest.json");
            if (!File.Exists(manifest) || new FileInfo(manifest).Length > 65536)
                throw new InvalidDataException("Missing or invalid legacy archive manifest");
            var manifestBytes = File.ReadAllBytes(manifest);
            ArchiveMetadata? metadata;
            try
            {
                using var stream = new MemoryStream(manifestBytes);
                metadata = new DataContractJsonSerializer(typeof(ArchiveMetadata)).ReadObject(stream) as ArchiveMetadata;
            }
            catch (SerializationException error) { throw new InvalidDataException("Invalid legacy archive manifest", error); }
            if (metadata == null || metadata.ArchiveVersion != 1 || metadata.Files == null || metadata.Files.Count < 1 || metadata.Files.Count > 2 ||
                metadata.Files.Any(x => x == null || (x.Name != "primary.json" && x.Name != "backup.json")) ||
                metadata.Files.Select(x => x.Name).Distinct().Count() != metadata.Files.Count)
                throw new InvalidDataException("Invalid legacy archive entries");
            var expected = new Dictionary<string, byte[]> { ["manifest.json"] = manifestBytes };
            foreach (var entry in metadata.Files)
            {
                var file = System.IO.Path.Combine(directory, entry.Name);
                if (!File.Exists(file) || new FileInfo(file).Length > SaveFormat.MaximumBytes)
                    throw new InvalidDataException("Missing legacy archive original");
                var bytes = File.ReadAllBytes(file);
                // Validity is a historical observation, not a classification to recompute with
                // a future game's broader reader. Format-1 archives were created by schema2.
                if (entry.Sha256 != Hash(bytes) || entry.SchemaVersion < 0 || entry.SchemaVersion > 2 ||
                    (entry.Validity != "valid" && entry.Validity != "invalid" && entry.Validity != "future") ||
                    (entry.Validity == "valid" && entry.SchemaVersion == 0))
                    throw new InvalidDataException("Legacy archive original or provenance does not match manifest");
                expected.Add(entry.Name, bytes);
            }
            Verify(directory, expected);
        }

        private static void Verify(string directory, Dictionary<string, byte[]> expected)
        {
            if (Directory.GetDirectories(directory).Length != 0 || Directory.GetFiles(directory).Length != expected.Count)
                throw new InvalidDataException("Legacy archive is incomplete or has unexpected entries");
            foreach (var file in expected)
            {
                var path = System.IO.Path.Combine(directory, file.Key);
                if (!File.Exists(path) || new FileInfo(path).Length != file.Value.Length || !File.ReadAllBytes(path).SequenceEqual(file.Value))
                    throw new InvalidDataException("Legacy archive differs from expected original; refusing overwrite");
            }
        }

        private static ArchiveFile Describe(string name, byte[] bytes, int maximumVersion = SaveFormat.CurrentVersion)
        {
            var result = new ArchiveFile { Name = name, Sha256 = Hash(bytes), Validity = "invalid" };
            try
            {
                result.SchemaVersion = SaveFormat.Probe(bytes);
                if (result.SchemaVersion > maximumVersion)
                {
                    result.SchemaVersion = 0;
                    result.Validity = "future";
                    return result;
                }
                using var input = new MemoryStream(bytes);
                var state = new DataContractJsonSerializer(typeof(GameSnapshot)).ReadObject(input) as GameSnapshot;
                if (state == null) return result;
                state.Validate(); result.Validity = "valid";
            }
            catch (UnsupportedSaveVersionException) { result.Validity = "future"; }
            catch (Exception error) when (error is SerializationException || error is InvalidDataException || error is ArgumentException) { }
            return result;
        }

        private static string Hash(byte[] bytes)
        {
            using var hash = SHA256.Create();
            return BitConverter.ToString(hash.ComputeHash(bytes)).Replace("-", "").ToLowerInvariant();
        }

        [DataContract]
        private sealed class ArchiveMetadata
        {
            [DataMember] public int ArchiveVersion { get; set; } = 1;
            [DataMember] public List<ArchiveFile> Files { get; set; } = new List<ArchiveFile>();
        }
        [DataContract]
        private sealed class ArchiveFile
        {
            [DataMember] public string Name { get; set; } = "";
            [DataMember] public string Sha256 { get; set; } = "";
            [DataMember] public int SchemaVersion { get; set; }
            [DataMember] public string Validity { get; set; } = "";
        }
    }
}
