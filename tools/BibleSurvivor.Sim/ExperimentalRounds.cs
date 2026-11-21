using System;
using System.Collections.Generic;
using System.Globalization;
using BibleSurvivor.Core;

namespace BibleSurvivor.Sim;

internal static class ExperimentalRounds
{
    internal const int CalibrationRound = 60;
    internal const int LastRound = 100;
    private static readonly int[] BookStarts = { 61, 67, 73, 82, 85, 91, 94, 97, 100 };
    private const string Steady = "trial_steady", Fast = "trial_fast", Heavy = "trial_heavy";

    internal static (RoundDefinition Definition, IReadOnlyDictionary<string, double> Speeds, BossDefinition? Boss)
        Create(int round, string curve)
    {
        var book = Array.FindLastIndex(BookStarts, start => start <= round) + 1;
        var offset = round - NumbersContent.GlobalRoundNumber;
        if (curve == "C1" && Array.IndexOf(BookStarts, round) >= 0)
            offset--;

        var steadySpeed = .73 + .02 * book;
        var steadyContact = 20 + book;
        var id = "trial_" + round.ToString(CultureInfo.InvariantCulture);

        var definition = LaterBookRounds.Build(round, id, Steady, Fast, Heavy,
            steadyContact, 19 + book, 24 + book, offset);
        var speeds = new Dictionary<string, double>
        {
            [Steady] = steadySpeed, [Fast] = .99 + .02 * book, [Heavy] = .42 + .01 * book,
        };
        var bossStep = (round - CalibrationRound) / 3;
        var boss = round % 3 == 0 ? BossDefinition.Experimental(round, id, 75500 + 3500 * bossStep,
            39 + bossStep, 1420 + 60 * bossStep, 34 + bossStep, 3841 + 180 * bossStep,
            Steady, 440 + 20 * offset, steadyContact, steadySpeed,
            Math.Min(.95, .90 + .05 * (bossStep / 2)), Math.Min(.90, .55 + .05 * ((bossStep + 1) / 2))) : null;
        return (definition, speeds, boss);
    }
}
