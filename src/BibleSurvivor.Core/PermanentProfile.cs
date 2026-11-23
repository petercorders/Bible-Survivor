using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace BibleSurvivor.Core
{
    internal static class PermanentProfile
    {
        private static readonly string[] UnlockOrder =
        {
            "wilderness_voice", "lightning", "moses_staff", "fire_pillar",
            "daniel_lion", "cross", "elijah_chariot"
        };
        private static readonly HashSet<string> WeaponIds = new HashSet<string>(
            UnlockOrder.Concat(new[] { "bible", "jawbone", "sling" }), StringComparer.Ordinal);

        internal static Dictionary<string, List<int>> LegacyPaidRanks(IReadOnlyDictionary<string, int> upgrades) =>
            upgrades.ToDictionary(entry => entry.Key,
                entry => PermanentRules.LegacyRankPrices(entry.Key, entry.Value), StringComparer.Ordinal);

        internal static void Upgrade(GameSnapshot snapshot)
        {
            if (snapshot.SchemaVersion == SaveFormat.CurrentVersion) return;

            var profile = snapshot.Profile;
            profile.PaidRanks = LegacyPaidRanks(profile.Upgrades);
            var highest = Array.FindLastIndex(UnlockOrder, profile.UnlockedWeapons.Contains) + 1;
            var evolved = new HashSet<string>(StringComparer.Ordinal);
            if (snapshot.Run is { } run)
            {
                run.CampaignLayout = snapshot.SchemaVersion == SaveFormat.CampaignVersion ? "campaign" : "genesis";
                run.CombatRulesVersion = 1;
                highest = Math.Max(highest, run.RoundNumber - 1);
                if (run.Forms != null) evolved.UnionWith(run.Forms.Keys);
                foreach (var boss in EncounterHistory(run))
                {
                    if (boss.CompletionApplied) highest = Math.Max(highest, boss.EncounterRound);
                    if (boss.Reward is { Applied: true } reward)
                    {
                        var choice = reward.Candidates?.SingleOrDefault(candidate => candidate.Id == reward.SelectedId);
                        if (choice?.Kind == "evolution") evolved.Add(EvolutionRules.Base(choice.ItemId));
                    }
                }
            }
            profile.HighestSettledRound = highest;
            profile.EvolvedWeapons = evolved.OrderBy(id => id, StringComparer.Ordinal).ToList();
            snapshot.SchemaVersion = SaveFormat.CurrentVersion;
        }

        private static IEnumerable<BossEncounterState> EncounterHistory(RunState run) =>
            new[] { run.Boss, run.CompletedBoss, run.CompletedSecondBoss, run.CompletedThirdBoss,
                run.CompletedFourthBoss, run.CompletedFifthBoss, run.CompletedSixthBoss }
                .Concat(run.LaterBossHistory ?? Enumerable.Empty<BossEncounterState>()).OfType<BossEncounterState>();

        internal static void RequireUnchangedInvestment(ProfileState current, ProfileState candidate)
        {
            var receipts = current.PaidRanks ?? LegacyPaidRanks(current.Upgrades);
            if (!BossRewards.Same(current.Upgrades, candidate.Upgrades) ||
                receipts.Any(entry => !candidate.PaidRanks!.TryGetValue(entry.Key, out var prices) ||
                    !entry.Value.SequenceEqual(prices)))
                throw new InvalidOperationException("Permanent skill purchases and refunds require a profile transaction");
        }

        internal static void Validate(ProfileState profile, long revision)
        {
            var receipts = profile.PaidRanks;
            var evolved = profile.EvolvedWeapons;
            if (profile.HighestSettledRound is not int settled || settled < 0 ||
                settled > CampaignContent.LastInstalledRound || evolved == null || evolved.Distinct().Count() != evolved.Count ||
                evolved.Any(id => !WeaponIds.Contains(id)) || receipts == null || receipts.Count != profile.Upgrades.Count)
                throw new InvalidDataException("Invalid permanent skill profile");

            PermanentSkillCatalog.Current.ValidateRanks(profile.Upgrades);
            foreach (var entry in profile.Upgrades)
            {
                if (!receipts.TryGetValue(entry.Key, out var prices) || prices == null ||
                    prices.Count != entry.Value || prices.Any(price => price < 1))
                    throw new InvalidDataException("Invalid permanent skill price receipt");
                var node = PermanentSkillCatalog.Current.Find(entry.Key);
                if (node.RequiredSettledRound > settled ||
                    node.RequiredWeapon != null && !profile.UnlockedWeapons.Contains(node.RequiredWeapon) ||
                    node.RequiredEvolution != null && !evolved.Contains(node.RequiredEvolution))
                    throw new InvalidDataException("Invalid permanent skill progression");
            }
            profile.LastPermanentSkillReceipt?.Validate(revision);
        }
    }
}
