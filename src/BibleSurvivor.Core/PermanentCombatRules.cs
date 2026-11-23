using System;
using System.Collections.Generic;

namespace BibleSurvivor.Core
{
    internal enum IncomingDamageKind { Contact, Projectile, Other }

    internal static class PermanentCombatRules
    {
        private static readonly (string Id, int Percent)[] HealthBonuses =
        {
            ("common.health.02", 4), ("common.health.03", 4), ("common.health.04", 5),
            ("common.health.05", 5), ("common.health.06", 5), ("common.health.07", 6),
            ("common.health.08", 6), ("common.health.09", 7), ("common.health.10", 8),
        };

        internal static double StartingHealth(IReadOnlyDictionary<string, int> ranks, bool expanded)
        {
            var legacy = PermanentRules.Rank(ranks, "health");
            if (!expanded) return 100 * (1 + 0.1 * legacy);
            var percent = 10 * legacy;
            foreach (var (id, gain) in HealthBonuses)
                percent += gain * PermanentRules.Rank(ranks, id);
            return 100 + Math.Min(100, percent);
        }

        internal static int Armor(IReadOnlyDictionary<string, int> ranks, int seal, IncomingDamageKind kind,
            double healthRatio, double secondsSinceLastHit, bool eliteOrBoss, bool surrounded, bool bossActive)
        {
            var result = PermanentRules.Rank(ranks, "armor") + seal;
            if (kind == IncomingDamageKind.Contact) result += PermanentRules.Rank(ranks, "common.armor.02");
            if (kind == IncomingDamageKind.Projectile) result += PermanentRules.Rank(ranks, "common.armor.03");
            if (healthRatio <= .30) result += PermanentRules.Rank(ranks, "common.armor.04");
            if (secondsSinceLastHit >= 8) result += 2 * PermanentRules.Rank(ranks, "common.armor.05");
            if (eliteOrBoss) result += PermanentRules.Rank(ranks, "common.armor.06");
            if (surrounded) result += PermanentRules.Rank(ranks, "common.armor.07");
            result += PermanentRules.Rank(ranks, "common.armor.08");
            if (healthRatio >= .90) result += PermanentRules.Rank(ranks, "common.armor.09");
            if (bossActive) result += 2 * PermanentRules.Rank(ranks, "common.armor.10");
            return Math.Min(12, result);
        }
    }
}
