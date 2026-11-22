using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using BibleSurvivor.Core;

namespace BibleSurvivor.Sim;

internal static class ExperimentalRounds
{
    internal const int CalibrationRound = 100;
    internal const int LastRound = 180;
    private const double C4FastSpawnTimePower = .85;
    private const double C7FastSpawnTimePower = .65;
    private const int C7EnemyHealthPercent = 80;
    private const int C7FinalBossHealth = 140000;
    private const string Steady = "trial_steady", Fast = "trial_fast", Heavy = "trial_heavy";
    private static readonly int[] BookStarts = { 100, 103, 112, 115, 118, 121, 130, 139, 142, 151, 160 };
    private static readonly string[] Books = {
        "job", "psalms", "proverbs", "ecclesiastes", "song_of_songs", "isaiah",
        "jeremiah", "lamentations", "ezekiel", "daniel", "minor_prophets",
    };

    internal static (RoundDefinition Definition, IReadOnlyDictionary<string, double> Speeds, BossDefinition? Boss)
        Create(int round, string curve)
    {
        if (round <= CalibrationRound || round > LastRound)
            throw new ArgumentOutOfRangeException(nameof(round));
        var book = Array.FindLastIndex(BookStarts, start => start <= round);
        var offset = curve switch
        {
            "C0" => round - NumbersContent.GlobalRoundNumber,
            "C1" => round - NumbersContent.GlobalRoundNumber - (BookStarts[book] == round ? 1 : 0),
            "C3" or "C4" or "C5" or "C6" or "C7" => 78 + (round - CalibrationRound) / 2,
            _ => throw new ArgumentOutOfRangeException(nameof(curve)),
        };

        var steadySpeed = .91 + .02 * book;
        var steadyContact = 29 + book;
        var id = "trial_" + Books[book] + "_" + (round - BookStarts[book] + 1).ToString("D2", CultureInfo.InvariantCulture);
        var contactLimit = (curve is "C5" or "C6" or "C7") && round >= 121 ? 33 : int.MaxValue;
        var definition = LaterBookRounds.Build(round, id, Steady, Fast, Heavy,
            Math.Min(steadyContact, contactLimit), Math.Min(steadyContact - 1, contactLimit),
            Math.Min(steadyContact + 4, contactLimit), offset);
        var spawnPower = curve == "C7" && round >= 160
            ? C7FastSpawnTimePower
            : C4FastSpawnTimePower;
        if ((curve is "C4" or "C5" or "C6" or "C7") && (offset % 3 == 2 || (curve is "C6" or "C7") && round >= 160))
        {
            var first = definition.Waves[0];
            var groups = first.Groups.Select(group => group.MonsterId == Fast
                ? new SpawnGroup(group.MonsterId, group.Count, group.Health, group.ContactDamage, spawnPower)
                : group);
            definition = new RoundDefinition(definition.Number, definition.ConcurrencyCap, definition.RawExperience,
                definition.CompletionGold, new[] { new WaveDefinition(first.Start, first.End, groups) }.Concat(definition.Waves.Skip(1)),
                definition.Id);
        }
        if (curve == "C7" && round >= 160)
        {
            var waves = definition.Waves.Select(wave => new WaveDefinition(wave.Start, wave.End,
                wave.Groups.Select(group => new SpawnGroup(group.MonsterId, group.Count,
                    Math.Max(1, group.Health * C7EnemyHealthPercent / 100), group.ContactDamage, group.SpawnTimePower))));
            definition = new RoundDefinition(definition.Number, definition.ConcurrencyCap, definition.RawExperience,
                definition.CompletionGold, waves, definition.Id);
        }
        var speeds = new Dictionary<string, double>
        {
            [Steady] = steadySpeed, [Fast] = 1.17 + .02 * book, [Heavy] = .51 + .01 * book,
        };
        var actions = ActionsFor(round);
        if (actions == BossActions.None) return (definition, speeds, null);

        var anchor = BossDefinition.ForRound(99);
        var bossStep = (round - anchor.Number) / 3;
        var summons = (actions & BossActions.Summon) != 0;
        var boss = BossDefinition.Experimental(round, id,
            curve == "C7" && round == LastRound
                ? C7FinalBossHealth
                : anchor.MaxHealth + (curve is "C3" or "C4" or "C5" or "C6" or "C7" ? 1750 : 3500) * bossStep,
            anchor.Contact + bossStep, anchor.KillGold + 60 * bossStep, 47 + bossStep,
            anchor.BossActorId + 180 * bossStep,
            summons ? Steady : "", summons ? 440 + 20 * offset : 0, summons ? steadyContact : 0,
            summons ? steadySpeed : 0, actions, .95, .90);
        return (definition, speeds, boss);
    }

    private static BossActions ActionsFor(int round) => round switch
    {
            102 => BossActions.DamageZone | BossActions.Chains,
            105 => BossActions.Slam | BossActions.DamageZone,
            108 => BossActions.Summon | BossActions.AimedShot,
            111 => BossActions.DamageZone | BossActions.Summon,
            114 => BossActions.Dash | BossActions.Summon,
            117 => BossActions.Slam | BossActions.Chains,
            120 => BossActions.Dash | BossActions.AimedShot,
            123 => BossActions.Summon | BossActions.Chains,
            126 => BossActions.Dash | BossActions.Chains | BossActions.DamageZone,
            129 => BossActions.Chains | BossActions.DamageZone,
            132 => BossActions.Summon | BossActions.AimedShot,
            135 => BossActions.Slam | BossActions.DamageZone,
            138 => BossActions.Slam | BossActions.Dash | BossActions.AimedShot,
            141 => BossActions.DamageZone | BossActions.Chains,
            144 => BossActions.Chains | BossActions.AimedShot,
            147 => BossActions.Slam | BossActions.Summon,
            150 => BossActions.Dash | BossActions.Summon | BossActions.AimedShot,
            153 => BossActions.Slam | BossActions.DamageZone,
            156 => BossActions.Dash | BossActions.Chains | BossActions.Slam,
            159 => BossActions.AimedShot | BossActions.Summon | BossActions.DamageZone,
            162 => BossActions.DamageZone | BossActions.Summon,
            165 => BossActions.Dash | BossActions.AimedShot,
            168 => BossActions.Slam | BossActions.Chains,
            171 => BossActions.Dash | BossActions.DamageZone,
            174 => BossActions.Summon | BossActions.DamageZone,
            177 => BossActions.Chains | BossActions.AimedShot | BossActions.Summon,
            180 => BossActions.Dash | BossActions.Slam | BossActions.Chains | BossActions.Summon | BossActions.AimedShot | BossActions.DamageZone,
        _ => BossActions.None,
    };
}
