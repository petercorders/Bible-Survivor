using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;

namespace BibleSurvivor.Core
{
    [DataContract]
    public sealed class ProfileState
    {
        [DataMember] public long Gold { get; set; }
        [DataMember] public Dictionary<string, int> Upgrades { get; set; } = new Dictionary<string, int>();
        [DataMember] public List<string> UnlockedWeapons { get; set; } = new List<string> { "bible", "jawbone", "sling" };
    }

    [DataContract]
    public sealed class RunState
    {
        [DataMember] public string RunId { get; set; } = "";
        [DataMember] public int RoundNumber { get; set; } = 1;
        [DataMember] public string Stage { get; set; } = "normal";
        [DataMember] public double Health { get; set; } = 100;
        [DataMember] public double MaxHealth { get; set; } = 100;
        [DataMember] public int Level { get; set; } = 1;
        [DataMember] public double Experience { get; set; }
        [DataMember] public int KillRemainder { get; set; }
        [DataMember] public double LastHitTime { get; set; } = -1;
        [DataMember] public Dictionary<string, int> PermanentSnapshot { get; set; } = new Dictionary<string, int>();
        [DataMember] public Dictionary<string, int> Weapons { get; set; } = new Dictionary<string, int>();
        [DataMember] public Dictionary<string, int> Passives { get; set; } = new Dictionary<string, int>();
        [DataMember] public WaveSnapshot? Wave { get; set; }
        [DataMember] public Dictionary<int, int> Drops { get; set; } = new Dictionary<int, int>();
        [DataMember] public int PendingLevels { get; set; }
        [DataMember] public int ChoiceSerial { get; set; } = 1;
        [DataMember] public List<string> Choices { get; set; } = new List<string>();
        [DataMember(EmitDefaultValue=false)] public BattleSnapshot? World {get;set;}
    }

    [DataContract]
    public sealed class GameSnapshot
    {
        [DataMember] public int SchemaVersion { get; set; } = 1;
        [DataMember] public long Revision { get; set; }
        [DataMember] public string LastOperationId { get; set; } = "";
        [DataMember] public ProfileState Profile { get; set; } = new ProfileState();
        [DataMember] public RunState? Run { get; set; }
        public static GameSnapshot New() => new GameSnapshot();

        public void Validate()
        {
            if (SchemaVersion > 1) throw new UnsupportedSaveVersionException();
            if (SchemaVersion != 1 || Revision < 0 || Profile == null || Profile.Gold < 0 || LastOperationId == null ||
                Profile.Upgrades == null || Profile.UnlockedWeapons == null || Profile.UnlockedWeapons.Distinct().Count() != Profile.UnlockedWeapons.Count ||
                Profile.UnlockedWeapons.Any(string.IsNullOrWhiteSpace)) throw new InvalidDataException("Invalid profile snapshot");
            PermanentRules.Validate(Profile.Upgrades);
            if (Run == null) return;
            if (string.IsNullOrWhiteSpace(Run.RunId) || Run.RoundNumber < 1 || Run.RoundNumber > 10 ||
                (Run.Stage != "normal" && Run.Stage != "between_rounds") || Run.Level < 1 || Run.Level > 10000 ||
                Run.KillRemainder < 0 || Run.KillRemainder > 4 || Run.Health <= 0 || Run.MaxHealth <= 0 || Run.Health > Run.MaxHealth ||
                !Finite(Run.Health) || !Finite(Run.MaxHealth) || !Finite(Run.Experience) || Run.Experience < 0 ||
                !Finite(Run.LastHitTime) || Run.LastHitTime < -1 || Run.Weapons == null || Run.Passives == null || Run.PermanentSnapshot == null)
                throw new InvalidDataException("Invalid run snapshot");
            if (Run.Drops == null || Run.Drops.Any(x=>x.Key <= 0 || x.Value < 0) || Run.PendingLevels < 0 || Run.ChoiceSerial < 1 ||
                Run.Choices == null || Run.Choices.Count > 3 || Run.Choices.Distinct().Count() != Run.Choices.Count ||
                (Run.PendingLevels == 0) != (Run.Choices.Count == 0)) throw new InvalidDataException("Invalid pending progression");
            PermanentRules.Validate(Run.PermanentSnapshot);
            if(Run.Wave!=null && (Run.Wave.Active==null || Run.Wave.Active.Any(x=>x==null)))
                throw new InvalidDataException("Invalid active enemy collection");
            Run.World?.Validate();
            if(Run.World!=null)
            {
                if(Run.Wave!=null && (Run.Wave.Active==null || Math.Abs(Run.World.Time-Run.Wave.Elapsed)>1e-7 ||
                    !Run.World.Enemies.Select(x=>x.Id).OrderBy(x=>x).SequenceEqual(Run.Wave.Active.Select(x=>x.Id).OrderBy(x=>x))))
                    throw new InvalidDataException("World checkpoint does not match round clock or enemies");
                if(!Run.World.Drops.Select(x=>x.Id).OrderBy(x=>x).SequenceEqual(Run.Drops.Keys.OrderBy(x=>x)) ||
                    (Run.Stage=="between_rounds" && (Run.World.Enemies.Count!=0 || Run.World.Projectiles.Count!=0)))
                    throw new InvalidDataException("World checkpoint does not match round lifecycle");
            }
            try { EquipmentInventory.Restore(Run.Weapons, Run.Passives); _ = new GrowthLedger(Run.Level, Run.Experience); }
            catch (ArgumentException error) { throw new InvalidDataException("Invalid progression", error); }
            if ((Run.Stage == "normal") != (Run.Wave != null)) throw new InvalidDataException("Invalid wave lifecycle");
            if (Run.Wave != null && (!Finite(Run.Wave.Elapsed) || Run.Wave.Elapsed < 0 || Run.Wave.Elapsed >= 180))
                throw new InvalidDataException("Invalid running clock");
        }
        private static bool Finite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);
    }

    public sealed class UnsupportedSaveVersionException : IOException
    { public UnsupportedSaveVersionException() : base("Save requires a newer game version; original preserved") { } }

    public sealed class SaveStore
    {
        private readonly object gate = new object();
        private readonly Action? beforeReplace;
        public string Path { get; }
        public SaveStore(string path, Action? beforeReplace = null)
        { Path = System.IO.Path.GetFullPath(path); this.beforeReplace = beforeReplace; }

        public GameSnapshot? Load()
        {
            lock (gate)
            {
                if (!File.Exists(Path)) return File.Exists(Path + ".bak") ? Read(Path + ".bak") : null;
                try { return Read(Path); }
                catch (UnsupportedSaveVersionException) { throw; }
                catch (Exception error) when (error is SerializationException || error is InvalidDataException || error is ArgumentException)
                {
                    if (File.Exists(Path + ".bak")) return Read(Path + ".bak");
                    throw new InvalidDataException("Save corrupt and no valid backup available", error);
                }
            }
        }
        public GameSnapshot? LoadBackup() { lock (gate) return File.Exists(Path + ".bak") ? Read(Path + ".bak") : null; }

        // Execute against a fresh deserialized aggregate; a failed write never mutates the last committed state.
        // Callers retain expectedRevision/operationId on uncertain results, and reload before new input.
        public GameSnapshot Transact(long expectedRevision,string operationId,Action<GameSnapshot> change)
        {
            lock(gate)
            {
                var snapshot=Load()??GameSnapshot.New();
                if(snapshot.LastOperationId==operationId) return snapshot;
                if(snapshot.Revision!=expectedRevision) throw new InvalidOperationException("Stale command revision");
                change(snapshot); Commit(snapshot,expectedRevision,operationId); return Load()!;
            }
        }

        public long Commit(GameSnapshot state, long expectedRevision, string operationId)
        {
            if (string.IsNullOrWhiteSpace(operationId)) throw new ArgumentException("Operation ID required");
            state.Validate();
            lock (gate)
            {
                Directory.CreateDirectory(System.IO.Path.GetDirectoryName(Path)!);
                // Single process host + OS lock across store instances. No silent last-writer-wins.
                using var fileLock = new FileStream(Path + ".lock", FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
                var current = Load();
                var revision = current?.Revision ?? 0;
                if (current != null && current.LastOperationId == operationId)
                {
                    if (Content(current) != Content(state)) throw new InvalidOperationException("Operation ID payload mismatch");
                    return current.Revision;
                }
                if (revision != expectedRevision) throw new InvalidOperationException("Stale save revision; reload before retry");
                var candidate = Decode(Encode(state));
                candidate.Revision = checked(revision + 1); candidate.LastOperationId = operationId;
                var staged = Path + ".tmp-" + Guid.NewGuid().ToString("N");
                try
                {
                    using (var output = new FileStream(staged, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                    { var bytes = Encode(candidate); output.Write(bytes, 0, bytes.Length); output.Flush(true); }
                    Read(staged); beforeReplace?.Invoke();
                    if (File.Exists(Path))
                    {
                        // If primary is corrupt, keep the last valid backup instead of backing up corruption.
                        var primaryValid = true;
                        try { Read(Path); }
                        catch (Exception error) when (error is SerializationException || error is InvalidDataException || error is ArgumentException) { primaryValid = false; }
                        File.Replace(staged, Path, primaryValid ? Path + ".bak" : null);
                    }
                    else File.Move(staged, Path);
                    return candidate.Revision;
                }
                finally { if (File.Exists(staged)) File.Delete(staged); }
            }
        }

        private static string Content(GameSnapshot state)
        {
            var copy = Decode(Encode(state)); copy.Revision = 0; copy.LastOperationId = "";
            return Encoding.UTF8.GetString(Encode(copy));
        }
        private static GameSnapshot Read(string path)
        {
            var info = new FileInfo(path); if (info.Length > 4 * 1024 * 1024) throw new InvalidDataException("Save too large");
            return Decode(File.ReadAllBytes(path));
        }
        private static byte[] Encode(GameSnapshot state)
        {
            using var stream = new MemoryStream();
            new DataContractJsonSerializer(typeof(GameSnapshot)).WriteObject(stream, state); return stream.ToArray();
        }
        private static GameSnapshot Decode(byte[] bytes)
        {
            using var stream = new MemoryStream(bytes);
            var state = new DataContractJsonSerializer(typeof(GameSnapshot)).ReadObject(stream) as GameSnapshot;
            if (state == null) throw new InvalidDataException("Missing save state"); state.Validate(); return state;
        }
    }

    public static class PermanentRules
    {
        private static readonly Dictionary<string, int[]> Prices = new Dictionary<string, int[]> {
            ["attack"] = new[] {100,200,350,550,800}, ["health"] = new[] {80,160,280,440,640},
            ["armor"] = new[] {250,600,1100}, ["move"] = new[] {100,250,450}, ["pickup"] = new[] {60,120,220,360,540},
            ["growth"] = new[] {300,650,1100}, ["cooldown"] = new[] {500,1000,1800}, ["regen"] = new[] {200,450,800} };
        public static int Rank(IReadOnlyDictionary<string,int> values, string key) => values.TryGetValue(key, out var rank) ? rank : 0;
        internal static void Validate(Dictionary<string,int> values)
        {
            if (values.Any(x => !Prices.ContainsKey(x.Key) || x.Value < 1 || x.Value > Prices[x.Key].Length))
                throw new InvalidDataException("Unknown or invalid permanent upgrade");
            // Prerequisites are checked at purchase; a restored profile must satisfy them too.
            foreach (var key in values.Keys) if (!Unlocked(values, key)) throw new InvalidDataException("Missing upgrade prerequisite");
        }
        private static bool Unlocked(IReadOnlyDictionary<string,int> values, string key) =>
            key == "cooldown" ? Rank(values,"attack") >= 3 : key == "armor" ? Rank(values,"health") >= 2 :
            key == "regen" ? Rank(values,"armor") >= 1 : key == "move" ? Rank(values,"pickup") >= 2 :
            key == "growth" ? Rank(values,"move") >= 1 : true;
        public static int NextPrice(ProfileState profile, string key)
        {
            if (!Prices.TryGetValue(key,out var prices) || !Unlocked(profile.Upgrades,key)) throw new InvalidOperationException("Upgrade locked");
            var rank=Rank(profile.Upgrades,key);
            if (rank >= prices.Length) throw new InvalidOperationException("Upgrade maxed"); return prices[rank];
        }
        public static void Purchase(ProfileState profile, string key)
        {
            if (!Prices.TryGetValue(key, out var prices) || !Unlocked(profile.Upgrades, key)) throw new InvalidOperationException("Upgrade locked");
            var rank = Rank(profile.Upgrades,key);
            if (rank == prices.Length || profile.Gold < prices[rank]) throw new InvalidOperationException("Upgrade unavailable");
            profile.Gold -= prices[rank]; profile.Upgrades[key] = rank + 1;
        }
        public static long RefundAll(ProfileState profile)
        {
            Validate(profile.Upgrades);
            long refund=0;
            foreach(var node in profile.Upgrades) for(var rank=0;rank<node.Value;rank++) refund=checked(refund+Prices[node.Key][rank]);
            var balance=checked(profile.Gold+refund);profile.Upgrades.Clear();profile.Gold=balance;return refund;
        }
    }
}
