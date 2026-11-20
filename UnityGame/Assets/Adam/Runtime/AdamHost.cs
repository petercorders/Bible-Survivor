using System;
using BibleSurvivor.Core;
using UnityEngine;
using NVector = System.Numerics.Vector2;

namespace BibleSurvivor.Unity
{
    // Single-thread owner. Failed commits freeze the exact payload/revision/operation until retry or explicit reload.
    public sealed class AdamHost
    {
        readonly AdamCatalog catalog;
        readonly SaveStore store;
        long revision;
        string operation;
        GameSnapshot chapterCandidate;
        double sinceSave;
        bool completedCheckpoint;
        public GameSnapshot State { get; private set; }
        public AdamSession Session { get; private set; }
        public BattleWorld World { get; private set; }
        public string Error { get; private set; }
        public bool RequiresNewerGame { get; private set; }
        public bool HasSaveRetry => operation != null;
        public bool Paused { get; private set; } = true;
        public bool Suspended { get; private set; }
        public Vector2 Movement { get; private set; }
        public bool CanPlay => Error == null && !Paused && !Suspended && Session != null && Session.IsActive && Session.PendingChoices.Count == 0;
        public bool AwaitingR02 => State?.Run?.Stage=="between_rounds" && State.Run.RoundNumber==2 && Session?.RoundNumber==1;
        public bool AwaitingR03 => State?.Run?.Stage=="between_rounds" && State.Run.RoundNumber==3 && Session?.RoundNumber==2;
        public bool AwaitingR04 => State?.Run?.Stage=="between_rounds" && State.Run.RoundNumber==4 && Session?.RoundNumber==3;
        public bool AwaitingR05 => State?.Run?.Stage=="between_rounds" && State.Run.RoundNumber==5 && Session?.RoundNumber==4;
        public bool AwaitingR06 => State?.Run?.Stage=="between_rounds" && State.Run.RoundNumber==6 && Session?.RoundNumber==5;
        public bool AwaitingR07 => State?.Run?.Stage=="between_rounds" && State.Run.RoundNumber==7 && Session?.RoundNumber==6;
        public bool AwaitingR08 => State?.Run?.Stage=="between_rounds" && State.Run.RoundNumber==8 && Session?.RoundNumber==7;
        public bool AwaitingR09 => State?.Run?.Stage=="between_rounds" && State.Run.RoundNumber==9 && Session?.RoundNumber==8;
        public bool AwaitingR11 => State?.Run?.Stage=="between_rounds" && State.Run.RoundNumber==11 && Session?.RoundNumber==10;
        public bool AwaitingR12 => State?.Run?.Stage=="between_rounds" && State.Run.RoundNumber==12 && Session?.RoundNumber==11;
        public bool AwaitingR13 => State?.Run?.Stage=="between_rounds" && State.Run.RoundNumber==13 && Session?.RoundNumber==12;
        public bool AwaitingR14 => State?.Run?.Stage=="between_rounds" && State.Run.RoundNumber==14 && Session?.RoundNumber==13;
        public bool AwaitingR15 => State?.Run?.Stage=="between_rounds" && State.Run.RoundNumber==15 && Session?.RoundNumber==14;
        public bool AwaitingNextRound => State?.Run?.Stage=="between_rounds" && Session?.RoundNumber==State.Run.RoundNumber-1 &&
            State.Run.RoundNumber!=ExodusContent.GlobalRoundNumber && State.Run.RoundNumber!=LeviticusContent.GlobalRoundNumber &&
            State.Run.RoundNumber!=NumbersContent.GlobalRoundNumber && State.Run.RoundNumber!=DeuteronomyContent.GlobalRoundNumber &&
            State.Run.RoundNumber!=JoshuaContent.GlobalRoundNumber && State.Run.RoundNumber!=JudgesContent.GlobalRoundNumber &&
            State.Run.RoundNumber!=RuthContent.GlobalRoundNumber && State.Run.RoundNumber!=FirstSamuelContent.GlobalRoundNumber &&
            State.Run.RoundNumber<=catalog.LastInstalledRound;
        bool CanEnterChapter => Error==null && !Suspended && operation==null && chapterCandidate==null;
        public bool CanEnterExodus => CanEnterChapter && catalog.ExodusInstalled && Session?.CanEnterExodus==true;
        public bool LeviticusInstalled => catalog.LeviticusInstalled;
        public bool CanEnterLeviticus => CanEnterChapter && LeviticusInstalled && Session?.CanEnterLeviticus==true;
        public bool HasLeviticusEntryRetry => chapterCandidate?.Run?.RoundNumber==LeviticusContent.GlobalRoundNumber;
        public bool NumbersInstalled => catalog.NumbersInstalled;
        public bool CanEnterNumbers => CanEnterChapter && NumbersInstalled && Session?.CanEnterNumbers==true;
        public bool HasNumbersEntryRetry => chapterCandidate?.Run?.RoundNumber==NumbersContent.GlobalRoundNumber;
        public bool DeuteronomyInstalled => catalog.DeuteronomyInstalled;
        public bool CanEnterDeuteronomy => CanEnterChapter && DeuteronomyInstalled && Session?.CanEnterDeuteronomy==true;
        public bool HasDeuteronomyEntryRetry => chapterCandidate?.Run?.RoundNumber==DeuteronomyContent.GlobalRoundNumber;
        public bool JoshuaInstalled => catalog.JoshuaInstalled;
        public bool CanEnterJoshua => CanEnterChapter && JoshuaInstalled && Session?.CanEnterJoshua==true;
        public bool HasJoshuaEntryRetry => chapterCandidate?.Run?.RoundNumber==JoshuaContent.GlobalRoundNumber;
        public bool JudgesInstalled => catalog.JudgesInstalled;
        public bool CanEnterJudges => CanEnterChapter && JudgesInstalled && Session?.CanEnterJudges==true;
        public bool HasJudgesEntryRetry => chapterCandidate?.Run?.RoundNumber==JudgesContent.GlobalRoundNumber;
        public bool RuthInstalled => catalog.RuthInstalled;
        public bool CanEnterRuth => CanEnterChapter && RuthInstalled && Session?.CanEnterRuth==true;
        public bool HasRuthEntryRetry => chapterCandidate?.Run?.RoundNumber==RuthContent.GlobalRoundNumber;
        public bool FirstSamuelInstalled => catalog.FirstSamuelInstalled;
        public bool CanEnterFirstSamuel => CanEnterChapter && FirstSamuelInstalled && Session?.CanEnterFirstSamuel==true;
        public bool HasFirstSamuelEntryRetry => chapterCandidate?.Run?.RoundNumber==FirstSamuelContent.GlobalRoundNumber;
        public bool ContentComplete => Session?.ContentComplete==true || (ExodusComplete && !LeviticusInstalled);
        public bool ExodusComplete => Session?.ExodusComplete == true;
        public bool LeviticusComplete => Session?.LeviticusComplete == true;
        public bool NumbersComplete => Session?.NumbersComplete == true;
        public bool DeuteronomyComplete => Session?.DeuteronomyComplete == true;
        public bool JoshuaComplete => Session?.JoshuaComplete == true;
        public bool JudgesComplete => Session?.JudgesComplete == true;
        public bool RuthComplete => Session?.RuthComplete == true;
        // Any completed book or the installed endpoint: the run is kept and a new challenge is a separate choice.
        public bool BookComplete => Session?.ChapterComplete==true || ExodusComplete || LeviticusComplete || NumbersComplete || DeuteronomyComplete || JoshuaComplete || JudgesComplete || RuthComplete || ContentComplete;
        public bool CanStartNew => Error==null && chapterCandidate==null && State!=null &&
            (State.Run==null || (BookComplete && Session.PendingChoices.Count==0));
        bool RoundEntryReady => AwaitingNextRound && Session.PendingChoices.Count==0 && completedCheckpoint;
        public AdamHost(AdamCatalog catalog, SaveStore store) { this.catalog = catalog; this.store = store; Reload(); }
        public bool Reload()
        {
            try
            {
                var loaded = store.Load() ?? GameSnapshot.New();
                var session = loaded.Run == null ? null : AdamSession.Resume(loaded, catalog.ForCheckpoint(loaded.Run));
                var world = session == null || !session.IsActive ? null : loaded.Run.World == null
                    ? new BattleWorld(session, catalog.SpeedsFor(session.RoundNumber), 7) : BattleWorld.Restore(session, catalog.SpeedsFor(session.RoundNumber), loaded.Run.World);
                State = loaded; Session = session; World = world; revision = loaded.Revision;
                operation = null; chapterCandidate=null; Error = null; RequiresNewerGame = false; Paused = true; Movement = Vector2.zero; sinceSave = 0;completedCheckpoint=AwaitingNextRound;
                return true;
            }
            catch (Exception e) { ReportDevelopmentFailure(e); Error = e.Message; RequiresNewerGame = e is UnsupportedSaveVersionException; Paused = true; Movement = Vector2.zero; return false; }
        }
        public void Start(string weapon)
        {
            if (Error != null || !CanStartNew || (weapon!="bible" && weapon!="jawbone" && weapon!="sling")) return;
            // Explicit new challenge button only; completed checkpoint is otherwise preserved.
            State.Run = null;completedCheckpoint=false;
            Session = new AdamSession(State, catalog.Definition, weapon);
            World = new BattleWorld(Session, catalog.Speeds, 7);
            Paused = false; Movement = Vector2.zero; Checkpoint();
        }
        public bool TryEnterExodus() => CanEnterExodus && TryEnterChapter(ExodusContent.GlobalRoundNumber);
        public bool TryEnterLeviticus() => CanEnterLeviticus && TryEnterChapter(LeviticusContent.GlobalRoundNumber);
        public bool TryEnterNumbers() => CanEnterNumbers && TryEnterChapter(NumbersContent.GlobalRoundNumber);
        public bool TryEnterDeuteronomy() => CanEnterDeuteronomy && TryEnterChapter(DeuteronomyContent.GlobalRoundNumber);
        public bool TryEnterJoshua() => CanEnterJoshua && TryEnterChapter(JoshuaContent.GlobalRoundNumber);
        public bool TryEnterJudges() => CanEnterJudges && TryEnterChapter(JudgesContent.GlobalRoundNumber);
        public bool TryEnterRuth() => CanEnterRuth && TryEnterChapter(RuthContent.GlobalRoundNumber);
        public bool TryEnterFirstSamuel() => CanEnterFirstSamuel && TryEnterChapter(FirstSamuelContent.GlobalRoundNumber);
        bool TryEnterChapter(int round)
        {
            try
            {
                // Keep the completed book published while preparing and committing a separate candidate.
                var definition=catalog.ForRound(round);
                var candidate=round switch
                {
                    ExodusContent.GlobalRoundNumber=>Session.PrepareExodus(definition),
                    LeviticusContent.GlobalRoundNumber=>Session.PrepareLeviticus(definition),
                    NumbersContent.GlobalRoundNumber=>Session.PrepareNumbers(definition),
                    DeuteronomyContent.GlobalRoundNumber=>Session.PrepareDeuteronomy(definition),
                    JoshuaContent.GlobalRoundNumber=>Session.PrepareJoshua(definition),
                    JudgesContent.GlobalRoundNumber=>Session.PrepareJudges(definition),
                    RuthContent.GlobalRoundNumber=>Session.PrepareRuth(definition),
                    FirstSamuelContent.GlobalRoundNumber=>Session.PrepareFirstSamuel(definition),
                    _=>throw new ArgumentOutOfRangeException(nameof(round))
                };
                var preview=AdamSession.Resume(candidate,definition);
                candidate.Run.World=new BattleWorld(preview,catalog.SpeedsFor(definition.Number),7).Capture();
                candidate.Validate();
                chapterCandidate=candidate;operation=Guid.NewGuid().ToString("N");
                Paused=true;Movement=Vector2.zero;
                var accepted=RetrySave();
                if(accepted && !Suspended)Paused=false;
                return accepted;
            }
            catch(Exception error){ReportDevelopmentFailure(error);Error=error.Message;RequiresNewerGame=error is UnsupportedSaveVersionException;Paused=true;Movement=Vector2.zero;return false;}
        }
        public void SetMovement(Vector2 value) { Movement = CanPlay ? Vector2.ClampMagnitude(value, 1) : Vector2.zero; }
        public void Tick(double delta)
        {
            if(Error==null && !Paused && !Suspended)
            {
                if(RoundEntryReady){EnterNextRound();return;}
                if(Session?.Stage=="boss_settlement" && Session.PendingChoices.Count==0){CompleteBossSettlement();return;}
                if(Session?.Stage=="round_settlement" && Session.PendingChoices.Count==0){CompleteNormalSettlement();return;}
            }
            if (!CanPlay) { Movement = Vector2.zero; return; }
            // Never accumulate wall-clock downtime. A long frame advances at most 0.1 simulation seconds.
            var seconds = Math.Min(Math.Max(delta, 0), .1);
            World.Advance(seconds, new NVector(Movement.x, Movement.y)); sinceSave += seconds;
            if (!Session.IsActive || Session.PendingChoices.Count > 0 || sinceSave >= 5)
            { Movement=Vector2.zero;if(Checkpoint() && !Paused && !Suspended && RoundEntryReady)EnterNextRound(); }
        }
        public bool Choose(string id)
        {
            if (Error != null || Suspended || Session == null || !Session.PendingChoices.ContainsId(id)) return false;
            var accepted = Session.Choose(id);
            if (accepted) { completedCheckpoint=false;Movement=Vector2.zero;if(Checkpoint() && !Paused && !Suspended && RoundEntryReady)EnterNextRound(); }
            return accepted;
        }
        public bool ChooseBossReward(string rewardId,string candidateId)
        {
            if(Error!=null || Suspended || Session==null)return false;
            var accepted=Session.ChooseBossReward(rewardId,candidateId);
            if(accepted){Movement=Vector2.zero;Checkpoint();}
            return accepted;
        }
        public bool PromoteBossReward()
        {
            if(Error!=null || Suspended || Session==null || !Session.PromoteBossReward())return false;
            return Checkpoint();
        }
        public bool CompleteBossSettlement()
        {
            if(Error!=null || Suspended || Session==null || !Session.CompleteBossSettlement())return false;
            Movement=Vector2.zero;return Checkpoint();
        }
        public bool CompleteNormalSettlement()
        {
            if(Error!=null || Suspended || Session==null || !Session.CompleteNormalSettlement())return false;
            Movement=Vector2.zero;return Checkpoint();
        }
        public void SetPaused(bool value)
        {
            if (Error != null) return;
            Paused = value; Movement = Vector2.zero;
            // Host gating preserves world clock; core pause flag is deliberately not used as a second pause owner.
            if (Session != null && Session.IsActive) Session.Pause(false);
            if (value) Checkpoint();
            else if(!Suspended && RoundEntryReady)EnterNextRound();
        }
        void EnterNextRound()
        {
            if(Error!=null || Paused || Suspended || !RoundEntryReady)return;
            completedCheckpoint=false;Movement=Vector2.zero;
            try
            {
                var next=Session.StartNextRound(catalog.ForRound(State.Run.RoundNumber));
                var arena=new BattleWorld(next,catalog.SpeedsFor(next.RoundNumber),7);
                Session=next;World=arena;sinceSave=0;
                // Publish exactly once, then freeze this same new arena/payload if its entry commit fails.
                Checkpoint();
            }
            catch(Exception error){ReportDevelopmentFailure(error);Error=error.Message;Paused=true;Movement=Vector2.zero;}
        }
        public void SetSuspended(bool value)
        {
            Suspended = value; Movement = Vector2.zero;
            if (value) { Paused = true; Checkpoint(); }
            // Resume always requires explicit player confirmation.
        }
        public bool Checkpoint()
        {
            if (Error != null) return false;
            if (State == null) return true;
            completedCheckpoint=false;
            Session?.Capture();
            if (State.Run != null) State.Run.World = Session != null && Session.IsActive ? World?.Capture() : null;
            operation = Guid.NewGuid().ToString("N");
            return RetrySave();
        }
        [System.Diagnostics.Conditional("DEVELOPMENT_BUILD")]
        static void ReportDevelopmentFailure(Exception error)
        {
#if !UNITY_EDITOR
            Debug.LogException(error);
#endif
        }
        public bool RetrySave()
        {
            if (operation == null) return false;
            try
            {
                if(chapterCandidate!=null)
                {
                    // A failed or uncertain attempt retains this exact candidate, old revision and operation for retry.
                    var committed=store.Commit(chapterCandidate,revision,operation);
                    var loaded=store.Load() ?? throw new InvalidOperationException("Chapter entry readback is missing");
                    if(loaded.Revision!=committed || loaded.LastOperationId!=operation)
                        throw new InvalidOperationException("Chapter entry readback changed; reload before new input");
                    var session=AdamSession.Resume(loaded,catalog.ForRound(chapterCandidate.Run.RoundNumber));
                    var world=BattleWorld.Restore(session,catalog.SpeedsFor(session.RoundNumber),loaded.Run.World);
                    // Publish only after Commit, Load, validation, Resume and World reconstruction all succeed.
                    State=loaded;Session=session;World=world;revision=committed;
                    chapterCandidate=null;operation=null;Error=null;RequiresNewerGame=false;
                    Paused=true;Movement=Vector2.zero;sinceSave=0;completedCheckpoint=false;return true;
                }
                revision = store.Commit(State, revision, operation);
                State.Revision = revision; State.LastOperationId = operation;
                operation = null; Error = null; RequiresNewerGame = false; sinceSave = 0;completedCheckpoint=AwaitingNextRound;return true;
            }
            catch (Exception e) { ReportDevelopmentFailure(e); Error = e.Message; RequiresNewerGame = e is UnsupportedSaveVersionException; Paused = true; Movement = Vector2.zero; return false; }
        }
    }
    static class ChoiceLookup
    {
        public static bool ContainsId(this System.Collections.Generic.IReadOnlyList<string> list, string id)
        { for (var i = 0; i < list.Count; i++) if (list[i] == id) return true; return false; }
    }
}
