using BibleSurvivor.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace BibleSurvivor.Unity
{
    public sealed partial class AdamGame
    {
        readonly AdamCinematic cinematic = new AdamCinematic();
        readonly AdamGenesisCinematicMedia genesisCinematicMedia = new AdamGenesisCinematicMedia();
        bool releaseCinematicControls;
        public bool CinematicPlaying => cinematic.Active;

        bool BeginSavedCinematic()
        {
            var kind = Host.TakeSavedCinematic();
            if (kind != AdamCinematicKind.ExodusOpening) return BeginCinematic(kind);

            // The entry confirmation also owns a retry of that same save operation.
            if (!Host.Suspended && Host.Paused) SetPlayPaused(false);
            if (!BeginCinematic(kind)) FinishCinematic();
            return true;
        }

        bool BeginCinematic(AdamCinematicKind kind)
        {
            if (kind == AdamCinematicKind.None || !AdamCinematicArt.Available) return false;
            var script = Resources.Load<TextAsset>(AdamCinematic.ResourceName(kind));
            if (!cinematic.Begin(kind, script?.text)) return false;
            genesisCinematicMedia.Begin(kind, AdamCinematic.Read(kind, script.text));
            releaseCinematicControls = true;
            inputReleased = false;
            touchStarted = false;
            Host.SetMovement(Vector2.zero);
            ImpactAudio?.Stop();
            return true;
        }

        bool UpdateCinematic(Keyboard keys)
        {
            var pad = Gamepad.current;
            var held = (keys != null && (keys.enterKey.isPressed || keys.escapeKey.isPressed ||
                keys.spaceKey.isPressed || keys.digit1Key.isPressed || keys.digit2Key.isPressed ||
                keys.digit3Key.isPressed || keys.f5Key.isPressed || keys.f10Key.isPressed)) ||
                (pad != null && (pad.buttonEast.isPressed || pad.startButton.isPressed));
            if (releaseCinematicControls && !held) releaseCinematicControls = false;

            if (!cinematic.Active) return releaseCinematicControls;
            if (Host.Suspended) return true;
            if (!releaseCinematicControls)
            {
                if (keys?.escapeKey.wasPressedThisFrame == true || keys?.enterKey.wasPressedThisFrame == true ||
                    pad?.buttonEast.wasPressedThisFrame == true)
                {
                    FinishCinematic();
                    return true;
                }
                if (keys?.spaceKey.wasPressedThisFrame == true || pad?.startButton.wasPressedThisFrame == true)
                    cinematic.Paused = !cinematic.Paused;
            }
            cinematic.Advance(Time.unscaledDeltaTime);
            if (!cinematic.Active) FinishCinematic();
            return true;
        }

        void FinishCinematic()
        {
            cinematic.End();
            genesisCinematicMedia.Clear();
            commandFrame = Time.frameCount;
            releaseCinematicControls = true;
            inputReleased = KeyboardMovement(Keyboard.current) == Vector2.zero &&
                UnityEngine.InputSystem.EnhancedTouch.Touch.activeTouches.Count == 0;
            touchStarted = false;
        }

        bool CanReplayCreation => Host?.Error == null && Host != null && !Host.Suspended &&
            !Host.PermanentSkillsOpen && !NewChallengeOpen && !DeathPresentationVisible &&
            (Host.Session == null || Host.BookComplete || Host.Paused ||
                (!Host.Session.IsActive && Host.Session.CurrentHealth <= 0));

        bool CanReplayEnding => CanReplayCreation && (Host.Session?.ChapterComplete == true ||
            Host.State.Profile.HighestSettledRound >= 9 || Host.State.Run?.CompletedThirdBoss?.CompletionApplied == true);

        bool Replay(AdamCinematicKind kind)
        {
            if (commandFrame == Time.frameCount || releaseCinematicControls ||
                !(kind == AdamCinematicKind.Creation ? CanReplayCreation : CanReplayEnding)) return false;
            if (!BeginCinematic(kind)) return false;
            commandFrame = Time.frameCount;
            return true;
        }

        void DrawCinematic(Rect viewport, bool mobile, Rect? physicalFrame)
        {
            var prior = GUI.matrix;
            GUI.matrix = Matrix4x4.identity;
            var frame = AdamCinematic.Frame(viewport);
            AdamHud.Fill(viewport, AdamHud.Ink0);
            GUI.matrix = Matrix4x4.TRS(frame.position, Quaternion.identity,
                Vector3.one * (frame.width / AdamCinematic.Width));
            var horizon = AdamVisualQualityArt.Sprite(AdamBookTitle.Horizon, 0);
            var actor = art.Sprite("chr_player_layer-base_idle_32", 0);
            var scroll = art.Sprite("ico_psv_scroll_static_16", 0);
            var motion = ArtWorld.FeedbackEnabled;
            var hasMedia = genesisCinematicMedia.TryGetFrame(cinematic, motion, viewport, out var media);
            if (hasMedia)
            {
                if (media.LiveTerrain && Host.World != null)
                    DrawGenesisCinematicTerrain(viewport, frame, mobile, physicalFrame, media.FullTerrain);
                var tint = GUI.color;
                try
                {
                    GUI.color = Color.white;
                    GUI.DrawTextureWithTexCoords(new Rect(0, 0, AdamCinematic.Width, AdamCinematic.Height),
                        media.Texture, media.Uv);
                }
                finally { GUI.color = tint; }
            }
            else if (cinematic.Kind == AdamCinematicKind.ExodusOpening && cinematic.CurrentIndex == 2)
                ArtWorld.DrawBackdrop(art, new Rect(0, 0, AdamCinematic.Width, AdamCinematic.Height),
                    ExodusContent.GlobalRoundNumber);
            else
                AdamCinematicArt.Draw(cinematic, horizon, actor, scroll, ArtWorld.FeedbackEnabled);
            GUI.matrix = Matrix4x4.TRS(viewport.position, Quaternion.identity, Vector3.one);

            var cut = cinematic.Current;
            var subtitleHeight = hasMedia && !mobile
                ? Mathf.Min(100f, 25f * frame.width / AdamCinematic.Width + viewport.yMax - frame.yMax)
                : 100f;
            var subtitleTop = viewport.height - subtitleHeight;
            var subtitleScale = subtitleHeight / 100f;
            AdamHud.Fill(new Rect(0, subtitleTop, viewport.width, subtitleHeight), AdamHud.Ink0);
            if (cut.day > 0)
            {
                AdamHud.Fill(new Rect(12, 10, 116, 32), AdamHud.Ink0);
                GUI.Label(new Rect(20, 10, 100, 32), $"제{cut.day}일",
                    HudText(20, AdamHud.Primary, false));
            }
            var compactCaption = subtitleHeight < 60f;
            if (compactCaption)
            {
                var caption = HudText(14, AdamHud.Primary, false, TextAnchor.MiddleCenter);
                var citation = HudText(12, AdamHud.Muted, false, TextAnchor.MiddleLeft);
                const float gap = 12;
                var citationWidth = Mathf.Ceil(citation.CalcSize(new GUIContent(cut.reference)).x);
                var captionWidth = Mathf.Min(Mathf.Ceil(caption.CalcSize(new GUIContent(cut.caption)).x),
                    viewport.width - 24 - gap - citationWidth);
                var start = Mathf.Floor((viewport.width - captionWidth - gap - citationWidth) / 2);
                GUI.Label(new Rect(start, subtitleTop, captionWidth, 24), cut.caption, caption);
                GUI.Label(new Rect(start + captionWidth + gap, subtitleTop, citationWidth, 24), cut.reference, citation);
            }
            else
            {
                var caption = HudText(Mathf.RoundToInt(20 * subtitleScale), AdamHud.Primary, false, TextAnchor.MiddleCenter);
                caption.wordWrap = true;
                GUI.Label(new Rect(12, subtitleTop, viewport.width - 24, 40 * subtitleScale), cut.caption, caption);
                GUI.Label(new Rect(12, subtitleTop + 38 * subtitleScale, viewport.width - 24, 20 * subtitleScale),
                    cut.reference, HudText(Mathf.RoundToInt(16 * subtitleScale), AdamHud.Muted, false, TextAnchor.MiddleCenter));
            }

            var enabled = GUI.enabled;
            GUI.enabled = enabled && !Host.Suspended;
            var buttonTop = subtitleTop + (compactCaption ? 24 : 61 * subtitleScale);
            var buttonHeight = compactCaption ? 24 : 36 * subtitleScale;
            var controls = new GUIStyle(GUI.skin.button)
            {
                font = fonts.Label,
                fontSize = compactCaption ? 14 : Mathf.RoundToInt(16 * subtitleScale)
            };
            var pointer = Event.current.isMouse;
            if (GUI.Button(new Rect(viewport.width / 2 - 164, buttonTop, 156, buttonHeight),
                cinematic.Paused ? "재개" : "일시정지 (Space)", controls) && pointer)
                cinematic.Paused = !cinematic.Paused;
            if (GUI.Button(new Rect(viewport.width / 2 + 8, buttonTop, 156, buttonHeight),
                "건너뛰기 (Esc)", controls) && pointer)
                FinishCinematic();
            GUI.enabled = enabled;
            GUI.matrix = prior;
        }

        void DrawGenesisCinematicTerrain(Rect viewport, Rect frame, bool mobile, Rect? physicalFrame, bool fullTerrain)
        {
            var prior = GUI.matrix;
            var scale = mobile ? AdamMobileLayout.Scale(viewport) : AdamChoiceText.ScreenScale(viewport.width, viewport.height);
            GUI.matrix = Matrix4x4.TRS(viewport.position, Quaternion.identity, new Vector3(scale, scale, 1));
            var arena = mobile ? AdamMobileLayout.FullFrame(viewport, physicalFrame ?? viewport) :
                Arena(viewport.width / scale, viewport.height / scale);
            var position = Host.World.PlayerPosition;
            if (!fullTerrain)
            {
                // Letterbox gutters stay dark until the authored mask has fully revealed the arena.
                var clip = new Rect((frame.position - viewport.position) / scale, frame.size / scale);
                GUI.BeginGroup(clip);
                arena.position -= clip.position;
            }
            try
            {
                ArtWorld.DrawBackdrop(art, arena, Host.Session.RoundNumber,
                    new Vector2(position.X, position.Y), mobile);
            }
            finally
            {
                if (!fullTerrain) GUI.EndGroup();
                GUI.matrix = prior;
            }
        }

        void DrawCinematicReplay(Vector2 origin, bool endingAllowed = false)
        {
            if (!CanReplayCreation || !AdamCinematicArt.Available) return;
            var style = new GUIStyle(GUI.skin.button) { font = fonts.Label, fontSize = 16 };
            var pointer = Event.current.isMouse;
            if (Resources.Load<TextAsset>(AdamCinematic.ResourceName(AdamCinematicKind.Creation)) != null &&
                GUI.Button(new Rect(origin.x, origin.y, 166, 34), "창조 이야기 (F5)", style) && pointer)
                Replay(AdamCinematicKind.Creation);
            if (endingAllowed && CanReplayEnding &&
                Resources.Load<TextAsset>(AdamCinematic.ResourceName(AdamCinematicKind.GenesisEnding)) != null &&
                GUI.Button(new Rect(origin.x + 174, origin.y, 166, 34), "창세기 마무리 (F10)", style) && pointer)
                Replay(AdamCinematicKind.GenesisEnding);
        }
    }
}
