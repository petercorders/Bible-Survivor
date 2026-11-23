using System;
using System.IO;
using System.Runtime.Serialization;
using BibleSurvivor.Core;
using UnityEngine;

namespace BibleSurvivor.Unity
{
    public sealed partial class AdamHost
    {
        PermanentSkillCommand permanentSkillCommand;
        public bool PermanentSkillsOpen { get; private set; }
        public ProfileState PermanentSkillProfile { get; private set; }
        public string PermanentSkillError { get; private set; }
        public bool HasPermanentSkillRetry => permanentSkillCommand != null;
        public bool CanOpenPermanentSkills => !PermanentSkillsOpen && Error == null && !Suspended &&
            operation == null && chapterCandidate == null && State != null &&
            (State.Run == null || BookComplete || Paused && Session?.IsActive == true) &&
            (Session == null || Session.PendingChoices.Count == 0);

        public bool OpenPermanentSkills()
        {
            if (!CanOpenPermanentSkills) return false;
            if (Session?.IsActive == true && !Checkpoint()) return false;
            try
            {
                GameSnapshot confirmed;
                try
                {
                    confirmed = store.LoadConfirmed() ?? GameSnapshot.New();
                }
                catch (Exception error) when (Session?.IsActive != true && !(error is UnsupportedSaveVersionException) &&
                    (error is IOException || error is InvalidDataException || error is SerializationException || error is ArgumentException))
                {
                    // Recovery happens before a permanent request exists. Never use a backup
                    // as confirmation for an uncertain debit or refund.
                    var recovery = store.Load();
                    if (recovery == null || recovery.Revision != revision) throw;
                    if (!Checkpoint()) return false;
                    confirmed = store.LoadConfirmed() ?? throw new IOException("Recovered primary could not be verified");
                }
                if (confirmed.Revision != revision) UseConfirmedSkillState(confirmed);
                else PermanentSkillProfile = SaveStore.PreviewPermanentProfile(confirmed);
                PermanentSkillError = null;
                PermanentSkillsOpen = true;
                Movement = Vector2.zero;
                return true;
            }
            catch (Exception error)
            {
                ReportDevelopmentFailure(error);
                Error = error.Message;
                RequiresNewerGame = error is UnsupportedSaveVersionException;
                Paused = true;
                Movement = Vector2.zero;
                return false;
            }
        }

        public bool ClosePermanentSkills()
        {
            if (!PermanentSkillsOpen || HasPermanentSkillRetry) return false;
            PermanentSkillsOpen = false;
            PermanentSkillProfile = null;
            PermanentSkillError = null;
            return true;
        }

        public bool SubmitPermanentSkill(PermanentSkillCommand command)
        {
            if (!PermanentSkillsOpen || HasPermanentSkillRetry || command == null || Suspended) return false;
            permanentSkillCommand = command;
            return RetryPermanentSkill();
        }

        public bool RetryPermanentSkill()
        {
            if (!PermanentSkillsOpen || permanentSkillCommand == null || Suspended) return false;
            try
            {
                try
                {
                    UseConfirmedSkillState(store.TransactPermanent(permanentSkillCommand));
                    permanentSkillCommand = null;
                    PermanentSkillError = null;
                    return true;
                }
                catch (PermanentSkillRejectedException)
                {
                    UseConfirmedSkillState(store.LoadConfirmed() ?? GameSnapshot.New());
                    permanentSkillCommand = null;
                    PermanentSkillError = "저장 상태 또는 구매 조건이 달라졌습니다. 최신 정보에서 다시 선택하세요.";
                    return false;
                }
            }
            catch (Exception error)
            {
                ReportDevelopmentFailure(error);
                PermanentSkillError = "저장 결과를 확인할 수 없습니다. 같은 요청을 다시 확인하세요.";
                return false;
            }
        }

        void UseConfirmedSkillState(GameSnapshot snapshot)
        {
            var keepEndedSession = snapshot.Run == null && State?.Run == null;
            var session = snapshot.Run == null ? keepEndedSession ? Session : null :
                AdamSession.Resume(snapshot, catalog.ForCheckpoint(snapshot.Run));
            var world = session == null || !session.IsActive ? null : snapshot.Run.World == null
                ? new BattleWorld(session, catalog.SpeedsFor(session.RoundNumber), 7)
                : BattleWorld.Restore(session, catalog.SpeedsFor(session.RoundNumber), snapshot.Run.World);
            var profile = SaveStore.PreviewPermanentProfile(snapshot);
            State = snapshot;
            Session = session;
            World = world;
            revision = snapshot.Revision;
            PermanentSkillProfile = profile;
            Paused = true;
            Movement = Vector2.zero;
            sinceSave = 0;
            completedCheckpoint = AwaitingNextRound;
        }
    }
}
