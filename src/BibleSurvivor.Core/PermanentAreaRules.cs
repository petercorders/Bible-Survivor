using System;
using System.Collections.Generic;

namespace BibleSurvivor.Core
{
    internal readonly struct PermanentAreaProfile
    {
        internal double HitShapeBonus { get; }
        internal double ConditionalHitShapeBonus { get; }
        internal double HalfAngleBonus { get; }
        internal double FlightDistanceBonus { get; }
        internal double PlacedCircleBonus { get; }
        internal double OrbitDistanceBonus { get; }
        internal double ProjectileRadiusBonus { get; }

        internal PermanentAreaProfile(double hitShapeBonus, double halfAngleBonus, double flightDistanceBonus,
            double placedCircleBonus, double orbitDistanceBonus, double projectileRadiusBonus, double conditionalHitShapeBonus)
        {
            HitShapeBonus = hitShapeBonus + conditionalHitShapeBonus;
            ConditionalHitShapeBonus = conditionalHitShapeBonus;
            HalfAngleBonus = halfAngleBonus;
            FlightDistanceBonus = flightDistanceBonus;
            PlacedCircleBonus = placedCircleBonus;
            OrbitDistanceBonus = orbitDistanceBonus;
            ProjectileRadiusBonus = projectileRadiusBonus;
        }
    }

    internal static class PermanentAreaRules
    {
        internal static PermanentAreaProfile Evaluate(IReadOnlyDictionary<string, int> ranks,
            bool lowHealth = false, bool surrounded = false, bool evolved = false)
        {
            if (ranks == null) throw new ArgumentNullException(nameof(ranks));
            bool Has(string id) => ranks.TryGetValue(id, out var rank) && rank > 0;
            var conditionalHitShapeBonus = (lowHealth && Has("common.area.07") ? .06 : 0)
                + (surrounded && Has("common.area.08") ? .06 : 0)
                + (evolved && Has("common.area.10") ? .07 : 0);
            return new PermanentAreaProfile(
                Has("common.area.01") ? .04 : 0,
                Has("common.area.02") ? 4 : 0,
                Has("common.area.03") ? .05 : 0,
                Has("common.area.04") ? .05 : 0,
                Has("common.area.05") ? .05 : 0,
                Has("common.area.06") ? .05 : 0,
                conditionalHitShapeBonus);
        }
    }
}
