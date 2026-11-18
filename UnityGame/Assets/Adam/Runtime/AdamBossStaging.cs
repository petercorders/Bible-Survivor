using System;
using System.Runtime.CompilerServices;
using BibleSurvivor.Core;
using UnityEngine;

[assembly: InternalsVisibleTo("Adam.EditMode")]

namespace BibleSurvivor.Unity
{
    internal sealed class AdamBossStaging
    {
        const double RevealAt = .3 - 1e-9;
        const int SealTick = 6;
        AdamHost host;
        AdamSession session;
        string liveIntro, liveReward;
        bool sealPending;
        int rewardTick;
        public AdamStagingAudio Audio { get; private set; }

        internal readonly struct State
        {
            public readonly AdamHost Host;
            public readonly AdamSession Session;
            public readonly string Stage, Encounter, Reward;
            public readonly double Health, IntroElapsed;
            public readonly bool Error, CandidatePanel;

            public State(AdamHost host)
            {
                Host = host;
                Session = host?.Session;
                Stage = Session?.Stage;
                var boss = Session?.Boss;
                var receipt = boss?.Reward;
                Encounter = boss?.EncounterId;
                Reward = receipt?.RewardId;
                Health = boss?.Health ?? 0;
                IntroElapsed = boss?.IntroElapsed ?? 0;
                Error = host?.Error != null;
                CandidatePanel = IsCandidatePanel(Session, boss, Error);
            }

            public bool MatchingReward => !string.IsNullOrWhiteSpace(Encounter) &&
                Reward == Encounter + ":reward";
        }

        public void Initialize(GameObject owner)
        {
            Reset();
            Audio ??= new AdamStagingAudio(owner);
        }

        public void Reset()
        {
            host = null;
            session = null;
            liveIntro = null;
            Interrupt();
        }

        public void Interrupt()
        {
            Audio?.Stop();
            ClearReward();
        }

        void ClearReward()
        {
            liveReward = null;
            sealPending = false;
            rewardTick = 0;
        }

        // Lifecycle changes can occur during a GUI command or an application callback.
        public void Sync(AdamHost current, AdamImpactAudio settings, bool enabled) =>
            Sync(new State(current), settings, enabled);

        void Sync(State state, AdamImpactAudio settings, bool enabled)
        {
            var current = state.Host;
            if (!ReferenceEquals(host, current) || !ReferenceEquals(session, current?.Session))
            {
                Reset();
                host = current;
                session = current?.Session;
            }

            if (!enabled || current == null || current.Error != null || current.Paused || current.Suspended)
            {
                Interrupt();
                if (current?.Error != null) liveIntro = null;
                return;
            }

            if (!state.CandidatePanel || state.Reward != liveReward)
                ClearReward();
            Audio?.SetVolume(settings?.Muted == false ? settings.Volume : 0);
        }

        public State BeforeTick(AdamHost current, AdamImpactAudio settings, bool enabled)
        {
            var state = new State(current);
            Sync(state, settings, enabled);
            return state;
        }

        public void AfterTick(State before, AdamHost current, AdamImpactAudio settings, bool enabled)
        {
            var after = new State(current);
            Sync(after, settings, enabled);
            if (!ReferenceEquals(before.Host, after.Host) || !ReferenceEquals(before.Session, after.Session)) return;

            var sameIntro = after.Stage == "boss_intro" && !after.Error &&
                !string.IsNullOrWhiteSpace(after.Encounter) && after.Reward == null;
            if (!sameIntro || liveIntro != after.Encounter) liveIntro = null;

            if (sameIntro && !before.Error && before.Stage == "normal")
                liveIntro = after.Encounter;
            else if (liveIntro != null && before.Stage == "boss_intro" && before.Encounter == liveIntro &&
                before.IntroElapsed < RevealAt && after.IntroElapsed >= RevealAt)
                Audio?.Due(AdamStagingCue.Arrive, liveIntro, CanPlay(current, settings, enabled));

            // A receipt produced by this tick also proves kills that settle gold inline.
            if (before.Error || before.Stage != "boss" || before.Health <= 0 || after.Health != 0 ||
                before.Encounter != after.Encounter || before.Reward != null || !after.MatchingReward ||
                (after.Stage != "boss_reward" && after.Stage != "boss_settlement")) return;

            var firstDefeat = Audio?.Due(AdamStagingCue.Fall, after.Reward, CanPlay(current, settings, enabled)) == true;
            if (firstDefeat && enabled && !current.Paused && !current.Suspended && after.CandidatePanel)
            {
                liveReward = after.Reward;
                sealPending = true;
                rewardTick = 0;
            }
        }

        public void AdvanceReward(AdamHost current, double elapsed, AdamImpactAudio settings, bool enabled)
        {
            if (liveReward == null) return;
            rewardTick = (int)Math.Floor(AdamUiMotion.Quantized(elapsed) * 30 + 1e-8);
            if (!sealPending || rewardTick < SealTick) return;
            sealPending = false;
            Audio?.Due(AdamStagingCue.Seal, liveReward, CanPlay(current, settings, enabled));
        }

        static bool CanPlay(AdamHost host, AdamImpactAudio settings, bool enabled) =>
            enabled && host.Error == null && !host.Paused && !host.Suspended &&
            settings?.Muted == false && settings.Volume > 0;

        bool IsLiveIntro(AdamHost current, bool effects) =>
            effects && ReferenceEquals(host, current) && ReferenceEquals(session, current?.Session) &&
            current?.Error == null && session?.Stage == "boss_intro" && session.Boss.Reward == null &&
            liveIntro != null && liveIntro == session.Boss.EncounterId;

        public bool ShadowOnly(AdamHost current, bool effects) =>
            IsLiveIntro(current, effects) && session.Boss.IntroElapsed < RevealAt;

        public double HealthFraction(AdamHost current, bool effects)
        {
            if (IsLiveIntro(current, effects)) return IntroFill(current.Session.Boss.IntroElapsed);
            return AdamBossPresentation.HealthFraction(current.Session);
        }

        internal static float IntroFill(double elapsed)
        {
            if (elapsed < .6 - 1e-9) return 0;
            if (elapsed >= 1.6 - 1e-9) return 1;
            return (1 + (int)Math.Floor(11 * Math.Max(0, elapsed - .6) + 1e-9)) / 12f;
        }

        public static bool ShowsCandidateHeader(AdamHost host)
        {
            var current = host?.Session;
            return current?.Stage == "boss_reward" && IsCandidatePanel(current, current.Boss, host.Error != null);
        }

        static bool IsCandidatePanel(AdamSession current, BossEncounterState boss, bool error)
        {
            var reward = boss?.Reward;
            return !error && current?.Stage == "boss_reward" && reward != null && !reward.Applied && reward.PromotedFrom == null &&
                reward.PolicyId == current.BossRules?.RewardPolicy && reward.Candidates?.Count > 0 &&
                !string.IsNullOrWhiteSpace(boss.EncounterId) && reward.RewardId == boss.EncounterId + ":reward";
        }

        public int HeaderTick(AdamHost current, bool effects) =>
            effects && ReferenceEquals(host, current) && ReferenceEquals(session, current?.Session) &&
            !current.Paused && !current.Suspended && liveReward != null &&
            liveReward == current.Session?.Boss?.Reward?.RewardId ? rewardTick : 14;

        internal static int RuleEighths(int tick) => tick < SealTick ? 0 : Math.Min(8, 1 + (tick - SealTick) * 7 / 8);
    }
}
