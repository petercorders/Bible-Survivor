using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Security.Cryptography;
using System.Text;

namespace BibleSurvivor.Core
{
    public sealed class PermanentSkillRejectedException : InvalidOperationException
    {
        internal PermanentSkillRejectedException(string message) : base(message) { }
    }

    [DataContract]
    public sealed class PermanentSkillReceipt
    {
        [DataMember(IsRequired = true)] public string OperationId { get; set; } = "";
        [DataMember(IsRequired = true)] public long ExpectedRevision { get; set; }
        [DataMember(IsRequired = true)] public string RequestDigest { get; set; } = "";
        [DataMember(IsRequired = true)] public long ResultingRevision { get; set; }
        [DataMember(IsRequired = true)] public long GoldDelta { get; set; }

        internal void Validate(long saveRevision)
        {
            if (!PermanentSkillCommand.IsOperationId(OperationId, ExpectedRevision) ||
                !PermanentSkillCommand.IsDigest(RequestDigest) || ResultingRevision <= 0 ||
                ExpectedRevision != ResultingRevision - 1 || ResultingRevision > saveRevision || GoldDelta == 0)
                throw new InvalidDataException("Invalid permanent skill transaction receipt");
        }
    }

    public sealed class PermanentSkillCommand
    {
        public string OperationId { get; }
        public long ExpectedRevision { get; }
        public string? NodeId { get; }
        public int DisplayedRank { get; }
        public long DisplayedGold { get; }
        public string? RefundDigest { get; }
        internal string Digest { get; }

        private PermanentSkillCommand(string operationId, long expectedRevision, string? nodeId,
            int displayedRank, long displayedGold, string? refundDigest)
        {
            if (!IsOperationId(operationId, expectedRevision))
                throw new ArgumentException("Permanent skill command ID does not match its expected revision", nameof(operationId));
            OperationId = operationId;
            ExpectedRevision = expectedRevision;
            NodeId = nodeId;
            DisplayedRank = displayedRank;
            DisplayedGold = displayedGold;
            RefundDigest = refundDigest;
            Digest = Hash(writer =>
            {
                writer.Write("permanent-skill-command-v1");
                writer.Write(OperationId);
                writer.Write(ExpectedRevision);
                writer.Write(NodeId != null);
                writer.Write(NodeId ?? "");
                writer.Write(DisplayedRank);
                writer.Write(DisplayedGold);
                writer.Write(RefundDigest ?? "");
            });
        }

        public static string NewOperationId(long expectedRevision) =>
            expectedRevision.ToString(CultureInfo.InvariantCulture) + ":" + Guid.NewGuid().ToString("D");

        public static PermanentSkillCommand Purchase(string operationId, long expectedRevision, string nodeId,
            int displayedRank, int displayedPrice)
        {
            if (string.IsNullOrWhiteSpace(nodeId) || displayedRank < 0 || displayedPrice < 1)
                throw new ArgumentException("Invalid permanent skill purchase selection");
            return new PermanentSkillCommand(operationId, expectedRevision, nodeId, displayedRank, displayedPrice, null);
        }

        public static PermanentSkillCommand RefundAll(string operationId, long expectedRevision,
            long displayedRefund, string refundDigest)
        {
            if (displayedRefund < 1 || !IsDigest(refundDigest))
                throw new ArgumentException("Invalid permanent skill refund selection");
            return new PermanentSkillCommand(operationId, expectedRevision, null, 0, displayedRefund, refundDigest);
        }

        public static (long Gold, string Digest) QuoteRefund(ProfileState profile)
        {
            var receipts = profile.PaidRanks ?? PermanentProfile.LegacyPaidRanks(profile.Upgrades);
            var gold = receipts.Values.SelectMany(prices => prices).Aggregate(0L, (sum, price) => checked(sum + price));
            return (gold, Hash(writer =>
            {
                writer.Write("permanent-skill-refund-v1");
                writer.Write(receipts.Count);
                foreach (var entry in receipts.OrderBy(entry => entry.Key, StringComparer.Ordinal))
                {
                    writer.Write(entry.Key);
                    writer.Write(entry.Value.Count);
                    foreach (var price in entry.Value) writer.Write(price);
                }
            }));
        }

        internal long Apply(ProfileState profile)
        {
            if (NodeId != null)
            {
                if (!PermanentSkillCatalog.Current.TryFind(NodeId, out var node))
                    throw new PermanentSkillRejectedException("Unknown permanent skill");
                var rank = PermanentRules.Rank(profile.Upgrades, NodeId);
                if (rank != DisplayedRank || rank >= node.Prices.Count || node.Prices[rank] != DisplayedGold)
                    throw new PermanentSkillRejectedException("Permanent skill selection changed");
                var status = node.Availability(profile.Upgrades, profile.Gold, profile.HighestSettledRound!.Value,
                    profile.UnlockedWeapons, profile.EvolvedWeapons!);
                if (status != PermanentSkillAvailability.Available)
                    throw new PermanentSkillRejectedException("Permanent skill is unavailable: " + status);
                if (!profile.PaidRanks!.TryGetValue(NodeId, out var receipts))
                    profile.PaidRanks.Add(NodeId, receipts = new List<int>());
                receipts.Add(node.Prices[rank]);
                profile.Upgrades[NodeId] = rank + 1;
                profile.Gold = checked(profile.Gold - DisplayedGold);
                return -DisplayedGold;
            }

            var refund = QuoteRefund(profile);
            if (refund.Gold != DisplayedGold || refund.Digest != RefundDigest)
                throw new PermanentSkillRejectedException("Permanent skill refund selection changed");
            profile.Gold = checked(profile.Gold + refund.Gold);
            profile.Upgrades.Clear();
            profile.PaidRanks!.Clear();
            return refund.Gold;
        }

        internal static bool IsOperationId(string? operationId, long expectedRevision)
        {
            if (expectedRevision < 0 || operationId == null) return false;
            var prefix = expectedRevision.ToString(CultureInfo.InvariantCulture) + ":";
            if (!operationId.StartsWith(prefix, StringComparison.Ordinal)) return false;
            var suffix = operationId.Substring(prefix.Length);
            return Guid.TryParseExact(suffix, "D", out var guid) && guid != Guid.Empty && suffix == guid.ToString("D");
        }

        internal static bool IsDigest(string? digest) =>
            digest?.Length == 64 && digest.All(character =>
                character >= '0' && character <= '9' || character >= 'a' && character <= 'f');

        private static string Hash(Action<BinaryWriter> write)
        {
            using var stream = new MemoryStream();
            using (var writer = new BinaryWriter(stream, new UTF8Encoding(false, true), true)) write(writer);
            using var hash = SHA256.Create();
            return BitConverter.ToString(hash.ComputeHash(stream.ToArray())).Replace("-", "").ToLowerInvariant();
        }
    }
}
