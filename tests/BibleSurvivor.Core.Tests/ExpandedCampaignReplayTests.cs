using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text.Json;
using BibleSurvivor.Sim;

namespace BibleSurvivor.Core.Tests;

internal static class ExpandedCampaignReplayTests
{
    private const string GenesisCatalog = "content/genesis/catalog.json";
    private static readonly int[] LiveBossCheckpoints = { 3, 9, 12, 15, 60, 180 };
    private static readonly DateTimeOffset FinalClearTime = new(2026, 1, 1, 0, 0, 0, TimeSpan.Zero);

    public static void Register(Action<string, Action> test) =>
        test("Expanded installed campaign reopens book and boss checkpoints through R180", Replay);

    private static void Replay()
    {
        var directory = Directory.CreateTempSubdirectory("adam-expanded-campaign-");
        try
        {
            var path = Path.Combine(directory.FullName, "save.json");
            var store = new SaveStore(path);
            var first = Definition(1);
            var state = SaveStore.PrepareNewChallenge(GameSnapshot.New());
            var session = new AdamSession(state, first, "sling");
            var runId = state.Run!.RunId;
            var permanentSnapshot = JsonSerializer.Serialize(state.Run.PermanentSnapshot);
            var bossCount = 0;
            var lastRound = CampaignContent.LastInstalledRound;
            Check.Equal(180, lastRound);

            for (var round = 1; round <= lastRound; round++)
            {
                try
                {
                    var definition = Definition(round);
                    if (round > 1)
                    {
                        var entry = PrepareBookEntry(session, definition);
                        if (entry is null) session = session.StartNextRound(definition);
                        else
                        {
                            Check.Throws<InvalidOperationException>(() => session.StartNextRound(definition));
                            var gold = state.Profile.Gold;
                            (state, session) = Reopen(path, store, entry, definition, "book-entry-" + round);
                            Check.Equal(gold, state.Profile.Gold);
                        }
                    }

                    Check.Equal(SaveFormat.CurrentVersion, state.SchemaVersion);
                    Check.Equal(2, session.CombatRulesVersion);
                    Check.Equal(round, state.Run!.RoundNumber);
                    Check.Equal("normal", session.Stage);
                    Check.Equal(definition.Fingerprint, state.Run.RoundFingerprint);
                    Check.Equal(round < ExodusContent.GlobalRoundNumber ? "genesis" : "campaign", state.Run.CampaignLayout);
                    Check.Equal(runId, state.Run.RunId);
                    Check.Equal(PermanentSkillCatalog.CurrentVersion, state.Run.PermanentCatalogVersion!.Value);
                    Check.Equal(permanentSnapshot, JsonSerializer.Serialize(state.Run.PermanentSnapshot));
                    Check.Equal(round - 1, state.Profile.HighestSettledRound!.Value);
                    if (round == 2)
                    {
                        session.Advance(25);
                        foreach (var target in session.Targets.Keys.ToArray()) session.Kill(target);
                    }

                    // Advance the actual wave director and deliver controlled boss damage.
                    session.Advance(180);
                    if (session.BossRules is BossDefinition boss)
                    {
                        bossCount++;
                        Check.Equal("boss_intro", session.Stage);
                        var saveBoss = LiveBossCheckpoints.Contains(round);
                        if (saveBoss)
                        {
                            var speeds = Speeds(round);
                            var world = new BattleWorld(session, speeds, 7);
                            world.Advance(2.25, Vector2.UnitX);
                            Check.Equal("boss", session.Stage);
                            if (round == lastRound)
                            {
                                session.Hit(boss.BossActorId, 1);
                                Check.True(state.OldTestamentClear is null);
                            }

                            session.Capture();
                            var captured = world.Capture();
                            state.Run.World = captured;
                            Check.Equal(6, captured.SchemaVersion);
                            Check.True(captured.PlayerX > 0);
                            Check.True(captured.Tuning.CameraHalfWidth > captured.Tuning.CameraHalfHeight);
                            Check.True(captured.Enemies.Any(actor => actor.Id == boss.BossActorId));
                            (state, session) = Reopen(path, store, state, definition, "live-boss-" + round);
                            var restored = BattleWorld.Restore(session, speeds, state.Run!.World!);
                            Check.Equal(JsonSerializer.Serialize(captured), JsonSerializer.Serialize(restored.Capture()));
                            Check.Equal(round - 1, state.Profile.HighestSettledRound!.Value);
                            Check.True(state.OldTestamentClear is null);
                        }
                        else session.Advance(2);

                        session.Hit(boss.BossActorId, boss.MaxHealth, FinalClearTime);
                        var reward = session.Boss!.Reward!;
                        Check.Equal(boss.RewardPolicy, reward.PolicyId);
                        Check.Equal(2, reward.RulesVersion!.Value);
                        Check.Equal(round - 1, state.Profile.HighestSettledRound!.Value);
                        if (saveBoss)
                        {
                            var frozenReward = JsonSerializer.Serialize(reward);
                            var gold = state.Profile.Gold;
                            (state, session) = Reopen(path, store, state, definition, "boss-reward-" + round);
                            Check.Equal(frozenReward, JsonSerializer.Serialize(session.Boss!.Reward));
                            Check.Equal(gold, state.Profile.Gold);
                        }

                        if (session.Stage == "boss_reward")
                            Check.True(session.ChooseBossReward(reward.RewardId, session.BossChoices[0].Id));
                        ResolveGrowth(session);
                        Check.True(session.CompleteBossSettlement());
                        Check.True(!session.CompleteBossSettlement());
                    }
                    else
                    {
                        if (round == 2)
                        {
                            var choices = session.PendingChoices.ToArray();
                            Check.True(choices.Length > 0);
                            Check.True(state.Profile.UnlockedWeapons.Contains("lightning"));
                            Check.True(!state.Run.GrowthChoiceUnlockedWeapons!.Contains("lightning"));
                            (state, session) = Reopen(path, store, state, definition, "normal-growth-2");
                            Check.True(choices.SequenceEqual(session.PendingChoices));
                        }
                        ResolveGrowth(session);
                    }
                    Check.Equal("between_rounds", session.Stage);
                    Check.Equal(round, state.Profile.HighestSettledRound!.Value);
                }
                catch (Exception error)
                {
                    throw new InvalidOperationException($"Expanded campaign replay failed at R{round:00}", error);
                }
            }

            Check.Equal(60, bossCount);
            (state, session) = Reopen(path, store, state, Definition(lastRound), "campaign-complete");
            Check.True(session.ContentComplete);
            Check.Equal(lastRound + 1, state.Run!.RoundNumber);
            Check.Equal(lastRound, state.Profile.HighestSettledRound!.Value);
            Check.Equal(runId, state.OldTestamentClear!.RunId);
            Check.Equal(FinalClearTime.ToUnixTimeMilliseconds(), state.OldTestamentClear.CompletedAtUnixMilliseconds);
            var completedClear = JsonSerializer.Serialize(state.OldTestamentClear);
            var profileGold = state.Profile.Gold;

            var next = SaveStore.PrepareNewChallenge(state);
            Check.True(next.Run is null);
            session = new AdamSession(next, first, "sling");
            (state, session) = Reopen(path, store, next, first, "challenge-after-clear");
            Check.Equal(2, session.CombatRulesVersion);
            Check.True(state.Run!.RunId != runId);
            Check.Equal(1, state.Run.RoundNumber);
            Check.Equal(lastRound, state.Profile.HighestSettledRound!.Value);
            Check.Equal(profileGold, state.Profile.Gold);
            Check.Equal(completedClear, JsonSerializer.Serialize(state.OldTestamentClear));
        }
        finally { directory.Delete(true); }
    }

    private static void ResolveGrowth(AdamSession session)
    {
        while (session.PendingChoices.Count > 0)
            Check.True(session.Choose(session.PendingChoices[0]));
    }

    private static (GameSnapshot State, AdamSession Session) Reopen(string path, SaveStore store,
        GameSnapshot state, RoundDefinition definition, string operation)
    {
        var revision = store.Commit(state, state.Revision, operation);
        var reopened = new SaveStore(path).Load()!;
        Check.Equal(revision, reopened.Revision);
        return (reopened, AdamSession.Resume(reopened, definition));
    }

    private static RoundDefinition Definition(int round) => round switch
    {
        < ExodusContent.GlobalRoundNumber => CatalogReader.Round(GenesisCatalog, round),
        < LeviticusContent.GlobalRoundNumber => ExodusContent.Round(round),
        _ => CampaignContent.Round(round)
    };

    private static IReadOnlyDictionary<string, double> Speeds(int round) => round switch
    {
        < ExodusContent.GlobalRoundNumber => CatalogReader.MonsterSpeeds(GenesisCatalog),
        < LeviticusContent.GlobalRoundNumber => ExodusContent.SpeedsFor(round),
        _ => CampaignContent.SpeedsFor(round)
    };

    private static GameSnapshot? PrepareBookEntry(AdamSession session, RoundDefinition definition) => definition.Number switch
    {
        ExodusContent.GlobalRoundNumber => session.PrepareExodus(definition),
        LeviticusContent.GlobalRoundNumber => session.PrepareLeviticus(definition),
        NumbersContent.GlobalRoundNumber => session.PrepareNumbers(definition),
        DeuteronomyContent.GlobalRoundNumber => session.PrepareDeuteronomy(definition),
        JoshuaContent.GlobalRoundNumber => session.PrepareJoshua(definition),
        JudgesContent.GlobalRoundNumber => session.PrepareJudges(definition),
        RuthContent.GlobalRoundNumber => session.PrepareRuth(definition),
        FirstSamuelContent.GlobalRoundNumber => session.PrepareFirstSamuel(definition),
        _ when LaterCampaignBooks.StartsBook(definition.Number) => session.PrepareLaterBook(definition),
        _ => null
    };
}
