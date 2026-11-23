using System;
using System.Collections.Generic;
using System.Numerics;

namespace BibleSurvivor.Core
{
    [Flags]
    public enum PermanentShotHitBonuses : byte
    {
        None = 0,
        LastPierce = 1,
        LongFlight = 2,
    }

    public readonly struct PermanentShotRules
    {
        private readonly bool fasterFlight;
        private readonly bool widerCollision;
        private readonly bool deeperPierce;
        private readonly bool reduceAimError;
        private readonly bool fasterFirstFlight;
        private readonly bool widerFollowups;
        private readonly bool deeperEvolvedPierce;
        private readonly bool fasterReturn;

        public double FlightSpeedBonus => fasterFlight ? .05 : 0;
        public double CollisionBonus => widerCollision ? .04 : 0;
        public int PierceBonus => deeperPierce ? 1 : 0;
        public PermanentShotHitBonuses HitBonuses { get; }

        private PermanentShotRules(IReadOnlyDictionary<string, int> ranks)
        {
            bool Has(string number) => ranks.TryGetValue("common.shot." + number, out var rank) && rank > 0;
            fasterFlight = Has("01");
            widerCollision = Has("02");
            deeperPierce = Has("03");
            reduceAimError = Has("04");
            fasterFirstFlight = Has("05");
            widerFollowups = Has("06");
            HitBonuses = (Has("07") ? PermanentShotHitBonuses.LastPierce : PermanentShotHitBonuses.None) |
                (Has("08") ? PermanentShotHitBonuses.LongFlight : PermanentShotHitBonuses.None);
            deeperEvolvedPierce = Has("09");
            fasterReturn = Has("10");
        }

        public static PermanentShotRules FromRanks(IReadOnlyDictionary<string, int> ranks) =>
            new PermanentShotRules(ranks ?? throw new ArgumentNullException(nameof(ranks)));

        // Travel speed uses the active level's base, while collision sizes use the Lv1 size.
        // The first three ranks are already included in the supplied effective stats.
        public double LaunchSpeed(double levelBaseSpeed, double effectiveSpeed, bool first) =>
            fasterFirstFlight && first ? AddSpeed(levelBaseSpeed, effectiveSpeed, .08) : effectiveSpeed;

        public double LaunchCollisionDimension(double levelOneDimension, double effectiveDimension, bool first) =>
            widerFollowups && !first
                ? Math.Min(levelOneDimension * 2.5, effectiveDimension + levelOneDimension * .06)
                : effectiveDimension;

        public int EvolutionPierceBudget(int budget, bool evolved) =>
            deeperEvolvedPierce && evolved ? Math.Min(8, budget + 1) : budget;

        public double ReturnSpeed(double levelBaseSpeed, double effectiveSpeed) =>
            fasterReturn ? AddSpeed(levelBaseSpeed, effectiveSpeed, .10) : effectiveSpeed;

        public Vector2 CorrectAim(Vector2 aimedDirection, Vector2 targetDirection)
        {
            if (!reduceAimError || aimedDirection.LengthSquared() <= 1e-12f || targetDirection.LengthSquared() <= 1e-12f)
                return aimedDirection;
            var target = Vector2.Normalize(targetDirection);
            var side = (double)target.X * aimedDirection.Y - (double)target.Y * aimedDirection.X;
            var along = (double)target.X * aimedDirection.X + (double)target.Y * aimedDirection.Y;
            if (along > 0 && Math.Abs(side) <= along * 1e-7) return aimedDirection;
            var angle = Math.Atan2(side, along) * .90;
            var cosine = Math.Cos(angle);
            var sine = Math.Sin(angle);
            return new Vector2((float)(target.X * cosine - target.Y * sine),
                (float)(target.X * sine + target.Y * cosine));
        }

        public static bool ValidHitBonuses(PermanentShotHitBonuses bonuses) =>
            (bonuses & ~(PermanentShotHitBonuses.LastPierce | PermanentShotHitBonuses.LongFlight)) == 0;

        // remainingPierce is the original budget before accepting this hit. Null means infinite.
        // Distance is the distance the projectile itself has travelled at contact.
        public static double HitDamage(PermanentShotHitBonuses bonuses, double originalBaseDamage, double damage,
            int? remainingPierce, double travelledDistance)
        {
            var bonus = (bonuses & PermanentShotHitBonuses.LastPierce) != 0 && remainingPierce == 1 ? .07 : 0;
            if ((bonuses & PermanentShotHitBonuses.LongFlight) != 0 && travelledDistance >= 4) bonus += .06;
            return bonus > 0 ? Math.Min(originalBaseDamage * 2.25, damage + originalBaseDamage * bonus) : damage;
        }

        private static double AddSpeed(double levelBaseSpeed, double effectiveSpeed, double bonus) =>
            Math.Min(levelBaseSpeed * 1.75, effectiveSpeed + levelBaseSpeed * bonus);
    }
}
