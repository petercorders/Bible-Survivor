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
        double sinceSave;
        public GameSnapshot State { get; private set; }
        public AdamSession Session { get; private set; }
        public BattleWorld World { get; private set; }
        public string Error { get; private set; }
        public bool Paused { get; private set; } = true;
        public bool Suspended { get; private set; }
        public Vector2 Movement { get; private set; }
        public bool CanPlay => Error == null && !Paused && !Suspended && Session != null && Session.IsActive && Session.PendingChoices.Count == 0;
        public AdamHost(AdamCatalog catalog, SaveStore store) { this.catalog = catalog; this.store = store; Reload(); }
        public bool Reload()
        {
            try
            {
                var loaded = store.Load() ?? GameSnapshot.New();
                if (loaded.Run != null && (loaded.Run.Stage == "normal" ? loaded.Run.RoundNumber != 1 : loaded.Run.RoundNumber != 2))
                    throw new InvalidOperationException("This checkpoint belongs to later content; preserved on disk");
                var session = loaded.Run == null ? null : AdamSession.Resume(loaded, catalog.Definition);
                var world = session == null || !session.IsActive ? null : loaded.Run.World == null
                    ? new BattleWorld(session, catalog.Speeds, 7) : BattleWorld.Restore(session, catalog.Speeds, loaded.Run.World);
                State = loaded; Session = session; World = world; revision = loaded.Revision;
                operation = null; Error = null; Paused = true; Movement = Vector2.zero; sinceSave = 0;
                return true;
            }
            catch (Exception e) { Error = e.Message; Paused = true; Movement = Vector2.zero; return false; }
        }
        public void Start(string weapon)
        {
            if (Error != null || (State.Run != null && Session != null && Session.IsActive)) return;
            // Explicit new challenge button only; completed checkpoint is otherwise preserved.
            State.Run = null;
            Session = new AdamSession(State, catalog.Definition, weapon);
            World = new BattleWorld(Session, catalog.Speeds, 7);
            Paused = false; Movement = Vector2.zero; Checkpoint();
        }
        public void SetMovement(Vector2 value) { Movement = CanPlay ? Vector2.ClampMagnitude(value, 1) : Vector2.zero; }
        public void Tick(double delta)
        {
            if (!CanPlay) { Movement = Vector2.zero; return; }
            // Never accumulate wall-clock downtime. A long frame advances at most 0.1 simulation seconds.
            var seconds = Math.Min(Math.Max(delta, 0), .1);
            World.Advance(seconds, new NVector(Movement.x, Movement.y)); sinceSave += seconds;
            if (!Session.IsActive || Session.PendingChoices.Count > 0 || sinceSave >= 5) { Movement = Vector2.zero; Checkpoint(); }
        }
        public bool Choose(string id)
        {
            if (Error != null || Suspended || Session == null || !Session.PendingChoices.ContainsId(id)) return false;
            var accepted = Session.Choose(id);
            if (accepted) { Movement = Vector2.zero; Checkpoint(); }
            return accepted;
        }
        public void SetPaused(bool value)
        {
            if (Error != null) return;
            Paused = value; Movement = Vector2.zero;
            // Host gating preserves world clock; core pause flag is deliberately not used as a second pause owner.
            if (Session != null && Session.IsActive) Session.Pause(false);
            if (value) Checkpoint();
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
            Session?.Capture();
            if (State.Run != null) State.Run.World = Session != null && Session.IsActive ? World?.Capture() : null;
            operation = Guid.NewGuid().ToString("N");
            return RetrySave();
        }
        public bool RetrySave()
        {
            if (operation == null) return false;
            try
            {
                revision = store.Commit(State, revision, operation);
                State.Revision = revision; State.LastOperationId = operation;
                operation = null; Error = null; sinceSave = 0; return true;
            }
            catch (Exception e) { Error = e.Message; Paused = true; Movement = Vector2.zero; return false; }
        }
    }
    static class ChoiceLookup
    {
        public static bool ContainsId(this System.Collections.Generic.IReadOnlyList<string> list, string id)
        { for (var i = 0; i < list.Count; i++) if (list[i] == id) return true; return false; }
    }
}
