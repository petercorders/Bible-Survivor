using System;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Json;

namespace BibleSurvivor.Core.Tests;

internal static class PermanentDurationRulesTests
{
    public static void Register(Action<string, Action> test)
    {
        test("Common duration launch stacks first, dense region and evolved original on one native basis", () =>
        {
            var rules = Ranks("05", "08", "10");
            Check.True(rules.HasFirstSustainedBonus);
            Check.Near(2.64, rules.LaunchDuration(2, 2.2, firstSustained: true,
                nearbyAtOriginalRegion: 6, evolved: true));
            Check.Near(2.52, rules.LaunchDuration(2, 2.2, firstSustained: true,
                nearbyAtOriginalRegion: 5, evolved: true));
            Check.Near(2.36, rules.LaunchDuration(2, 2.2, evolved: true));
            Check.Near(2.32, rules.LaunchDuration(2, 2.2, nearbyAtOriginalRegion: 6));
            Check.Near(3.5, rules.LaunchDuration(2, 3.4, firstSustained: true,
                nearbyAtOriginalRegion: 7, evolved: true));
            Check.Near(2.2, default(PermanentDurationRules).LaunchDuration(2, 2.2, true, 6, true));
        });

        test("Common duration records one eighth normal kill and one first elite kill per restored source", () =>
        {
            var rules = Ranks("06", "07");
            var source = rules.BeginKillTracking(2)!;
            var other = rules.BeginKillTracking(2)!;
            var active = 2.2;
            for (var kill = 0; kill < 4; kill++) active = source.RecordOriginalKill(active, elite: false);
            Check.Near(2.2, active);
            source = RoundTrip(source);
            source.Validate(active);
            Check.Equal(4, source.NormalKills);
            active = source.RecordOriginalKill(active, elite: true);
            Check.Near(2.5, active);
            Check.True(source.EliteUsed);
            Check.Near(2.5, source.RecordOriginalKill(active, elite: true));
            for (var kill = 4; kill < 7; kill++) active = source.RecordOriginalKill(active, elite: false);
            Check.Near(2.5, active);
            active = source.RecordOriginalKill(active, elite: false);
            Check.Near(2.75, active);
            Check.Equal(8, source.NormalKills);
            Check.Near(2.75, source.RecordOriginalKill(active, elite: false));
            Check.Equal(0, other.NormalKills);
            Check.True(!other.EliteUsed);

            var saturated = RoundTrip(other);
            Check.Near(3.5, saturated.RecordOriginalKill(3.49, elite: true));
            Check.True(saturated.EliteUsed);
            Check.Near(2, saturated.RecordOriginalKill(2, elite: true));
            Check.True(default(PermanentDurationRules).BeginKillTracking(2) == null);
            var corrupt = source.Copy();
            corrupt.NormalKills = 9;
            Check.Throws<ArgumentException>(() => corrupt.Validate(active));
        });

        test("Common duration flight expands the independent maximum while fixed travel still wins", () =>
        {
            var rules = Ranks("09");
            Check.True(rules.HasLongerFlight);
            Check.Near(3.24, rules.MaximumFlightLifetime(3, 3));
            Check.Near(2.5, rules.MaximumFlightLifetime(3, 3, 2.5));
            Check.Near(3.24, rules.MaximumFlightLifetime(3, 3, 4));
            Check.Near(5.25, rules.MaximumFlightLifetime(3, 5.2));
            Check.Near(3, default(PermanentDurationRules).MaximumFlightLifetime(3, 3));
        });
    }

    private static PermanentDurationRules Ranks(params string[] ids) =>
        PermanentDurationRules.FromRanks(ids.ToDictionary(id => "common.duration." + id, _ => 1));

    private static PermanentDurationProgress RoundTrip(PermanentDurationProgress value)
    {
        using var stream = new MemoryStream();
        var serializer = new DataContractJsonSerializer(typeof(PermanentDurationProgress));
        serializer.WriteObject(stream, value);
        stream.Position = 0;
        return (PermanentDurationProgress)serializer.ReadObject(stream)!;
    }
}
