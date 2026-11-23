using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace BibleSurvivor.Core.Tests;

internal static class PermanentSkillCatalogTests
{
    public static void Register(Action<string, Action> test)
    {
        test("Permanent catalog matches the launch counts, prices and reachable prerequisite graph", () =>
        {
            var nodes = PermanentSkillCatalog.Current.Nodes;
            var common = nodes.Where(node => node.TreeId.StartsWith("common.", StringComparison.Ordinal)).ToArray();
            var weapons = nodes.Except(common).GroupBy(node => node.TreeId).ToArray();
            Check.Equal(420, nodes.Count);
            Check.Equal(442, nodes.Sum(node => node.Prices.Count));
            Check.Equal(120, common.Length);
            Check.Equal(12, common.Select(node => node.TreeId).Distinct().Count());
            Check.True(common.GroupBy(node => node.TreeId).All(tree => tree.Count() == 10));
            Check.Equal(108050, common.Sum(node => node.Prices.Sum()));
            Check.Equal(10, weapons.Length);
            foreach (var tree in weapons)
            {
                Check.Equal(30, tree.Count());
                Check.Equal(6, tree.Select(node => node.BranchName).Distinct().Count());
                Check.True(tree.GroupBy(node => node.BranchName).All(branch => branch.Count() == 5));
                Check.Equal(27000, tree.Sum(node => node.Prices.Sum()));
            }
            foreach (var node in nodes)
            {
                Check.True(node.Id.Length > 0 && node.Name.Length > 0 && node.EffectDescription.Length > 0);
                Check.True(node.Prices.Count > 0 && node.Prices.All(price => price > 0));
                Check.True(node.RequiredSettledRound >= 0 && node.RequiredSettledRound <= 180);
            }

            var weaponIds = weapons.Select(tree => tree.Key).ToArray();
            var complete = Invest(180, 378050, weaponIds, weaponIds);
            Check.Equal(420, complete.Ranks.Count);
            Check.Equal(442, complete.Ranks.Values.Sum());
            Check.Equal(0L, complete.Gold);
            PermanentSkillCatalog.Current.ValidateRanks(complete.Ranks);

            var r60 = Invest(60, 65115, weaponIds, new[] { "sling" });
            Check.Equal(153, r60.Ranks.Count);
            Check.Equal(169, r60.Ranks.Values.Sum());
            Check.Equal(135L, r60.Gold);
            var r180 = Invest(180, 429115, weaponIds, new[] { "sling" });
            Check.Equal(375, r180.Ranks.Count);
            Check.Equal(397, r180.Ranks.Values.Sum());
            Check.Equal(109115L, r180.Gold);
        });

        test("Permanent catalog preserves every legacy purchase price and gate", () =>
        {
            var legacyIds = new[] { "attack", "health", "armor", "pickup", "move", "growth", "cooldown", "regen" };
            var catalog = PermanentSkillCatalog.Current;
            var profile = new ProfileState { Gold = 14450 };
            foreach (var id in legacyIds)
            {
                var node = catalog.Find(id);
                foreach (var price in node.Prices)
                {
                    Check.Equal(price, PermanentRules.NextPrice(profile, id));
                    PermanentRules.Purchase(profile, id);
                }
                Check.Throws<InvalidOperationException>(() => PermanentRules.NextPrice(profile, id));
            }
            Check.Equal(0L, profile.Gold);
            catalog.ValidateRanks(profile.Upgrades);
            Check.Throws<InvalidDataException>(() =>
                catalog.ValidateRanks(new Dictionary<string, int> { ["move"] = 1, ["pickup"] = 1 }));
            Check.Throws<InvalidDataException>(() =>
                catalog.ValidateRanks(new Dictionary<string, int> { ["pickup"] = 6 }));
            Check.Throws<InvalidDataException>(() =>
                catalog.ValidateRanks(new Dictionary<string, int> { ["unknown"] = 1 }));
        });

        test("Permanent catalog distinguishes weapon, evolution, round, rank and gold locks", () =>
        {
            var catalog = PermanentSkillCatalog.Current;
            var ranks = new Dictionary<string, int>();
            var noWeapons = Array.Empty<string>();
            var sling = new[] { "sling" };
            var mastery = catalog.Find("weapon.sling.base.01");
            var extraShot = catalog.Find("weapon.sling.base.03");
            Check.Equal(PermanentSkillAvailability.RequiresSkill, mastery.Availability(ranks, 600, 0, sling, noWeapons));
            ranks.Add("attack", 1);
            Check.Equal(PermanentSkillAvailability.RequiresWeapon, mastery.Availability(ranks, 500, 0, noWeapons, noWeapons));
            Check.Equal(PermanentSkillAvailability.Available, mastery.Availability(ranks, 500, 0, sling, noWeapons));
            ranks.Add(mastery.Id, 1);
            Check.Equal(350, extraShot.Prices[0]);
            Check.Equal(PermanentSkillAvailability.InsufficientGold, extraShot.Availability(ranks, 349, 0, sling, noWeapons));
            Check.Equal(PermanentSkillAvailability.Available, extraShot.Availability(ranks, 350, 0, sling, noWeapons));
            ranks.Add(extraShot.Id, 1);
            Check.Equal(PermanentSkillAvailability.MaximumRank, extraShot.Availability(ranks, 350, 0, sling, noWeapons));
            var evolution = catalog.Find("weapon.sling.e.05");
            var completed = Invest(180, 378050, catalog.Nodes.Select(node => node.TreeId).ToArray(), sling).Ranks;
            completed.Remove(evolution.Id);
            Check.Equal(PermanentSkillAvailability.RequiresSettledRound, evolution.Availability(completed, 1500, 149, sling, sling));
            Check.Equal(PermanentSkillAvailability.RequiresEvolution, evolution.Availability(completed, 1500, 150, sling, noWeapons));
            Check.Equal(PermanentSkillAvailability.Available, evolution.Availability(completed, evolution.Prices[0], 150, sling, sling));
        });
    }

    private static (Dictionary<string, int> Ranks, long Gold) Invest(
        int settledRound, long gold, string[] weapons, string[] evolved)
    {
        var ranks = new Dictionary<string, int>(StringComparer.Ordinal);
        while (true)
        {
            var next = PermanentSkillCatalog.Current.Nodes
                .Where(node => node.Availability(ranks, gold, settledRound, weapons, evolved) == PermanentSkillAvailability.Available)
                .OrderBy(node => node.Prices[PermanentRules.Rank(ranks, node.Id)])
                .ThenBy(node => node.Id, StringComparer.Ordinal)
                .FirstOrDefault();
            if (next == null) return (ranks, gold);
            var rank = PermanentRules.Rank(ranks, next.Id);
            gold -= next.Prices[rank];
            ranks[next.Id] = rank + 1;
        }
    }
}
