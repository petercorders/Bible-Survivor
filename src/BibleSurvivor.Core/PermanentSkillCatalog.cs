using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace BibleSurvivor.Core
{
    public enum PermanentSkillAvailability
    {
        Available,
        MaximumRank,
        RequiresSkill,
        RequiresSettledRound,
        RequiresWeapon,
        RequiresEvolution,
        InsufficientGold,
    }

    public sealed class PermanentSkillNode
    {
        public string Id { get; }
        public string TreeId { get; }
        public string TreeName { get; }
        public string BranchName { get; }
        public string Name { get; }
        public IReadOnlyList<int> Prices { get; }
        public IReadOnlyDictionary<string, int> Prerequisites { get; }
        public int RequiredSettledRound { get; }
        public string? RequiredWeapon { get; }
        public string? RequiredEvolution { get; }
        public string EffectDescription { get; }

        internal PermanentSkillNode(string[] columns)
        {
            Id = columns[0];
            TreeId = columns[1];
            TreeName = columns[2];
            BranchName = columns[3];
            Name = columns[4];
            Prices = Array.AsReadOnly(columns[5].Split(',').Select(ParseInteger).ToArray());
            Prerequisites = new ReadOnlyDictionary<string, int>(
                columns[6].Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries)
                    .Select(value => value.Split('@'))
                    .ToDictionary(parts => parts[0], parts => ParseInteger(parts[1]), StringComparer.Ordinal));
            RequiredSettledRound = ParseInteger(columns[7]);
            RequiredWeapon = columns[8].Length > 0 ? columns[8] : null;
            RequiredEvolution = columns[9].Length > 0 ? columns[9] : null;
            EffectDescription = columns[10];
        }

        public PermanentSkillAvailability Availability(
            IReadOnlyDictionary<string, int> ranks, long gold, int settledRound,
            IReadOnlyCollection<string> unlockedWeapons, IReadOnlyCollection<string> evolvedWeapons)
        {
            var rank = PermanentRules.Rank(ranks, Id);
            if (rank >= Prices.Count) return PermanentSkillAvailability.MaximumRank;
            if (!MeetsPrerequisites(ranks)) return PermanentSkillAvailability.RequiresSkill;
            if (settledRound < RequiredSettledRound) return PermanentSkillAvailability.RequiresSettledRound;
            if (RequiredWeapon != null && !unlockedWeapons.Contains(RequiredWeapon))
                return PermanentSkillAvailability.RequiresWeapon;
            if (RequiredEvolution != null && !evolvedWeapons.Contains(RequiredEvolution))
                return PermanentSkillAvailability.RequiresEvolution;
            return gold >= Prices[rank] ? PermanentSkillAvailability.Available : PermanentSkillAvailability.InsufficientGold;
        }

        internal bool MeetsPrerequisites(IReadOnlyDictionary<string, int> ranks) =>
            Prerequisites.All(entry => PermanentRules.Rank(ranks, entry.Key) >= entry.Value);

        private static int ParseInteger(string number) => int.Parse(number, NumberStyles.None, CultureInfo.InvariantCulture);
    }

    public sealed class PermanentSkillCatalog
    {
        public const int CurrentVersion = 1;
        private static readonly PermanentSkillCatalog V1 = new PermanentSkillCatalog(
            "BibleSurvivor.Core.PermanentSkillCatalog.v1.tsv");
        public static PermanentSkillCatalog Current => ForVersion(CurrentVersion);
        private readonly IReadOnlyDictionary<string, PermanentSkillNode> byId;

        public IReadOnlyList<PermanentSkillNode> Nodes { get; }

        private PermanentSkillCatalog(string resource)
        {
            using var stream = typeof(PermanentSkillCatalog).Assembly.GetManifestResourceStream(resource)
                ?? throw new InvalidOperationException("Missing permanent skill catalog resource");
            using var reader = new StreamReader(stream, new UTF8Encoding(false, true));
            var nodes = new List<PermanentSkillNode>();
            while (reader.ReadLine() is { } line)
            {
                if (line.StartsWith("#", StringComparison.Ordinal)) continue;
                var columns = line.Split('\t');
                if (columns.Length != 11) throw new InvalidDataException("Invalid permanent skill catalog row");
                nodes.Add(new PermanentSkillNode(columns));
            }
            Nodes = nodes.AsReadOnly();
            byId = new ReadOnlyDictionary<string, PermanentSkillNode>(
                Nodes.ToDictionary(node => node.Id, StringComparer.Ordinal));
        }

        public PermanentSkillNode Find(string id) => byId[id];
        internal bool TryFind(string id, out PermanentSkillNode node) => byId.TryGetValue(id, out node!);
        internal static bool SupportsVersion(int version) => version == 1;
        internal static PermanentSkillCatalog ForVersion(int version) =>
            SupportsVersion(version) ? V1 : throw new UnsupportedSaveVersionException();

        internal void ValidateRanks(IReadOnlyDictionary<string, int> ranks)
        {
            foreach (var entry in ranks)
            {
                if (!byId.TryGetValue(entry.Key, out var node) || entry.Value < 1 || entry.Value > node.Prices.Count)
                    throw new InvalidDataException("Unknown or invalid permanent skill rank");
                if (!node.MeetsPrerequisites(ranks))
                    throw new InvalidDataException("Missing permanent skill prerequisite");
            }
        }
    }
}
