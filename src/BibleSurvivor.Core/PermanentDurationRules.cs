using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace BibleSurvivor.Core
{
    [Flags]
    public enum PermanentDurationExtensions : byte
    {
        None = 0,
        NormalKills = 1,
        EliteKill = 2,
    }

    public readonly struct PermanentDurationRules
    {
        public const double MaximumScale = 1.75;
        public const double LongerFlightBonus = .08;
        private readonly bool denseOriginalRegion;
        private readonly bool longerEvolvedOriginal;

        public bool HasFirstSustainedBonus { get; }
        public bool HasLongerFlight { get; }
        public PermanentDurationExtensions Extensions { get; }

        private PermanentDurationRules(IReadOnlyDictionary<string, int> ranks)
        {
            bool Has(string number) => ranks.TryGetValue("common.duration." + number, out var rank) && rank > 0;
            HasFirstSustainedBonus = Has("05");
            Extensions = (Has("06") ? PermanentDurationExtensions.NormalKills : PermanentDurationExtensions.None) |
                (Has("07") ? PermanentDurationExtensions.EliteKill : PermanentDurationExtensions.None);
            denseOriginalRegion = Has("08");
            HasLongerFlight = Has("09");
            longerEvolvedOriginal = Has("10");
        }

        public static PermanentDurationRules FromRanks(IReadOnlyDictionary<string, int> ranks) =>
            new PermanentDurationRules(ranks ?? throw new ArgumentNullException(nameof(ranks)));

        // These launch bonuses share the native-duration basis with previously
        // applied bonuses. Nearby actors are supplied only for an original region.
        public double LaunchDuration(double nativeDuration, double existingDuration,
            bool firstSustained = false, int? nearbyAtOriginalRegion = null, bool evolved = false)
        {
            var bonus = (HasFirstSustainedBonus && firstSustained ? .08 : 0) +
                (denseOriginalRegion && nearbyAtOriginalRegion >= 6 ? .06 : 0) +
                (longerEvolvedOriginal && evolved ? .08 : 0);
            return bonus > 0 ? Extend(nativeDuration, existingDuration, nativeDuration * bonus) : existingDuration;
        }

        public PermanentDurationProgress? BeginKillTracking(double nativeDuration) =>
            Extensions == PermanentDurationExtensions.None ? null :
                new PermanentDurationProgress { NativeDuration = nativeDuration, Extensions = Extensions };

        public double MaximumFlightLifetime(double nativeLifetime, double existingLifetime, double? fixedRangeLimit = null)
        {
            var maximumLifetime = HasLongerFlight
                ? Extend(nativeLifetime, existingLifetime, nativeLifetime * LongerFlightBonus) : existingLifetime;
            return fixedRangeLimit.HasValue ? Math.Min(maximumLifetime, fixedRangeLimit.Value) : maximumLifetime;
        }

        internal static double Extend(double nativeDuration, double currentDuration, double extra) =>
            Math.Min(nativeDuration * MaximumScale, currentDuration + extra);

        public static bool ValidExtensions(PermanentDurationExtensions extensions) =>
            (extensions & ~(PermanentDurationExtensions.NormalKills | PermanentDurationExtensions.EliteKill)) == 0;
    }

    [DataContract]
    public sealed class PermanentDurationProgress
    {
        private const int RequiredNormalKills = 8;

        [DataMember(IsRequired = true)] public double NativeDuration { get; set; }
        [DataMember(IsRequired = true)] public PermanentDurationExtensions Extensions { get; set; }
        [DataMember(EmitDefaultValue = false)] public int NormalKills { get; set; }
        [DataMember(EmitDefaultValue = false)] public bool EliteUsed { get; set; }

        // Report only an accepted original kill while its originating attack is active.
        // A qualifying kill uses its one extension even when the lifetime is already capped.
        public double RecordOriginalKill(double currentDuration, bool elite)
        {
            if (elite)
            {
                if ((Extensions & PermanentDurationExtensions.EliteKill) == 0 || EliteUsed) return currentDuration;
                EliteUsed = true;
                return PermanentDurationRules.Extend(NativeDuration, currentDuration, .30);
            }

            if ((Extensions & PermanentDurationExtensions.NormalKills) == 0 ||
                NormalKills >= RequiredNormalKills || ++NormalKills != RequiredNormalKills) return currentDuration;
            return PermanentDurationRules.Extend(NativeDuration, currentDuration, .25);
        }

        public PermanentDurationProgress Copy() => (PermanentDurationProgress)MemberwiseClone();

        public void Validate(double currentDuration)
        {
            if (!BattleSnapshot.Finite(NativeDuration) || NativeDuration <= 0 ||
                !BattleSnapshot.Finite(currentDuration) || currentDuration <= 0 ||
                currentDuration > NativeDuration * PermanentDurationRules.MaximumScale + 1e-8 ||
                Extensions == PermanentDurationExtensions.None || !PermanentDurationRules.ValidExtensions(Extensions) ||
                NormalKills < 0 || NormalKills > RequiredNormalKills ||
                NormalKills != 0 && (Extensions & PermanentDurationExtensions.NormalKills) == 0 ||
                EliteUsed && (Extensions & PermanentDurationExtensions.EliteKill) == 0)
                throw new ArgumentException("Invalid permanent duration progress");
        }
    }
}
