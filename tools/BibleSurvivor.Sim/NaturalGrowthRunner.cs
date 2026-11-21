using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using BibleSurvivor.Core;

namespace BibleSurvivor.Sim;

internal static class NaturalGrowthRunner
{
    private const string GenesisCatalog = "content/genesis/catalog.json";
    private const string ExodusCatalog = "content/exodus/catalog.json";
    // Later bosses can outlast the technical fixture runner’s budget.
    private const int MaxTicksPerRound = 120000;

    internal static int Run(string[] args)
    {
        var policy = args.Length > 0 ? args[0] : "main-weapon";
        if (policy is not ("main-weapon" or "balanced") || args.Length > 4 ||
            (args.Length == 4 && args[3] is not ("C0" or "C1")))
            throw new ArgumentException("Usage: --natural-growth [main-weapon|balanced] [weapon] [seed] [C0|C1]");
        var weapon = args.Length > 1 ? args[1] : "sling";
        var seed = args.Length > 2 ? int.Parse(args[2]) : 7;
        var curve = args.Length > 3 ? args[3] : "C0";
        if (CampaignContent.LastInstalledRound != ExperimentalRounds.CalibrationRound)
            throw new InvalidOperationException("This experiment requires the R60 campaign revision. Run it from the natural-growth preparation commit.");
        var state = GameSnapshot.New();
        var game = new AdamSession(state, Definition(1), weapon);
        // Boss reward offers are seeded with the run ID. Use the CLI seed for both sources of randomness.
        state.Run!.RunId = new Guid(seed, 0, 0, new byte[8]).ToString("N");
        var genesisSpeeds = CatalogReader.MonsterSpeeds(GenesisCatalog);
        var rows = new List<RoundMetrics>();

        for (var round = 1; round <= ExperimentalRounds.LastRound; round++)
        {
            IReadOnlyDictionary<string, double> speeds;
            if (round <= ExperimentalRounds.CalibrationRound)
            {
                if (round > 1)
                    game = EnterRound(ref state, game, Definition(round));
                speeds = round <= 9 ? genesisSpeeds : round <= 15
                    ? ExodusContent.SpeedsFor(round) : CampaignContent.SpeedsFor(round);
            }
            else
            {
                var trial = ExperimentalRounds.Create(round, curve);
                game = game.StartExperimentalRound(trial.Definition, trial.Boss);
                speeds = trial.Speeds;
            }
            var row = Measure(state, game, speeds, policy, weapon, seed);
            rows.Add(row);
            Console.Error.WriteLine($"R{row.Round:D2} {row.Outcome}: {row.Killed}/{row.Spawned}, damage {row.DamageTaken}");
            if (row.Outcome != "complete")
                break;
        }

        var baselineRuns = rows.Where(row => row.Round is >= 31 and <= 60 && row.Round % 3 != 0).ToArray();
        var laterRuns = rows.Where(row => row.Round > 60 && row.Round % 3 != 0).ToArray();
        var baseline = rows.Any(row => row.Round == 60 && row.Outcome == "complete")
            ? baselineRuns.Average(row => row.KillRate) : null;
        var completed = rows[^1].Round == ExperimentalRounds.LastRound && rows[^1].Outcome == "complete";
        Console.WriteLine(JsonSerializer.Serialize(new
        {
            policy, weapon, seed, curve, rows, completed,
            deathRound = rows.FirstOrDefault(row => row.Outcome == "dead")?.Round,
            timedOutShare = rows.Average(row => row.TimedOut ? 1.0 : 0.0),
            baseline, minimumRequired = baseline / 2,
            lowestLaterRate = laterRuns.Length == 0 ? null : laterRuns.Min(row => row.KillRate),
            meetsCriterion = policy == "main-weapon" && baseline.HasValue && completed &&
                laterRuns.All(row => row.KillRate >= baseline / 2),
        }, new JsonSerializerOptions { WriteIndented = true }));
        return rows[^1].Outcome == "tick-limit" ? 1 : 0;
    }

    private static RoundDefinition Definition(int round) => round <= 9
        ? CatalogReader.Round(GenesisCatalog, round) : round <= 15
            ? CatalogReader.Exodus(ExodusCatalog, round) : CampaignContent.Round(round);

    private static AdamSession EnterRound(ref GameSnapshot state, AdamSession game, RoundDefinition next)
    {
        switch (next.Number)
        {
            case 10: state = game.PrepareExodus(next); break;
            case LeviticusContent.GlobalRoundNumber: state = game.PrepareLeviticus(next); break;
            case NumbersContent.GlobalRoundNumber: state = game.PrepareNumbers(next); break;
            case DeuteronomyContent.GlobalRoundNumber: state = game.PrepareDeuteronomy(next); break;
            case JoshuaContent.GlobalRoundNumber: state = game.PrepareJoshua(next); break;
            case JudgesContent.GlobalRoundNumber: state = game.PrepareJudges(next); break;
            case RuthContent.GlobalRoundNumber: state = game.PrepareRuth(next); break;
            case FirstSamuelContent.GlobalRoundNumber: state = game.PrepareFirstSamuel(next); break;
            default: return game.StartNextRound(next);
        }
        return AdamSession.Resume(state, next);
    }

    private static RoundMetrics Measure(GameSnapshot state, AdamSession game,
        IReadOnlyDictionary<string, double> speeds, string policy, string weapon, int seed)
    {
        var wave = game.Wave ?? throw new InvalidOperationException("Missing normal wave");
        var world = new BattleWorld(game, speeds, seed);
        var movement = new NaturalGrowthMovement(world, speeds);
        var normalKills = 0;
        for (var tick = 0; tick < MaxTicksPerRound && game.IsActive; tick++)
        {
            ResolveChoices(game, policy, weapon);
            var inNormalWave = game.Stage == "normal";
            world.Advance(BattleWorld.FixedStep, movement.Next(tick));
            if (inNormalWave)
                normalKills = world.Kills;
        }

        if (state.Run != null)
        {
            if (game.Stage == "boss_reward")
            {
                var reward = game.Boss!.Reward!;
                var choice = game.BossChoices
                    .OrderBy(candidate => candidate.Kind switch { "evolution" => 0, "item" => 1, _ => 2 })
                    .ThenBy(candidate => ChoiceRank(game, policy, weapon, candidate.ItemId))
                    .First();
                if (!game.ChooseBossReward(reward.RewardId, choice.Id))
                    throw new InvalidOperationException("Boss reward refused");
            }

            ResolveChoices(game, policy, weapon);
            if (game.Stage == "boss_settlement" && !game.CompleteBossSettlement() ||
                game.Stage == "round_settlement" && !game.CompleteNormalSettlement())
                throw new InvalidOperationException("Round settlement refused");
        }

        var outcome = state.Run == null ? "dead" : game.Stage == "between_rounds" ? "complete" : "tick-limit";
        return new RoundMetrics(game.RoundNumber, normalKills, wave.SpawnedCount,
            wave.SpawnedCount == 0 ? null : (double)normalKills / wave.SpawnedCount,
            wave.Elapsed >= 180, world.DamageTaken,
            world.BossKills == 1 ? game.Boss!.BossElapsed : null, outcome);
    }

    private static void ResolveChoices(AdamSession game, string policy, string weapon)
    {
        while (game.PendingChoices.Count > 0)
        {
            var choice = game.PendingChoices.OrderBy(id => ChoiceRank(game, policy, weapon, id)).First();
            if (!game.Choose(choice))
                throw new InvalidOperationException("Growth choice refused");
        }
    }

    private static int ChoiceRank(AdamSession game, string policy, string weapon, string choice)
    {
        if (policy == "main-weapon")
        {
            var matchingPassive = weapon switch { "sling" => "stones", "jawbone" => "hair", _ => "scroll" };
            return choice == weapon ? 0 : choice == matchingPassive ? 1 : 2;
        }
        return game.Inventory.Weapons.GetValueOrDefault(choice) + game.Inventory.Passives.GetValueOrDefault(choice);
    }

    private sealed record RoundMetrics(int Round, int Killed, int Spawned, double? KillRate,
        bool TimedOut, int DamageTaken, double? BossKillSeconds, string Outcome);
}
