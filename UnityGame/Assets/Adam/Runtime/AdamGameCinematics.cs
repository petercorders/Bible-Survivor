using BibleSurvivor.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace BibleSurvivor.Unity
{
    public sealed partial class AdamGame
    {
        readonly AdamCinematic cinematic = new AdamCinematic();
        readonly AdamCuratedCinematicArt curatedArt = new AdamCuratedCinematicArt();
        readonly AdamGenesisCinematicMedia genesisCinematicMedia = new AdamGenesisCinematicMedia();
        bool releaseCinematicControls;
        int cinematicExitFrame = -1;
        public bool CinematicPlaying => cinematic.Active;

        int replaySelection = -1;

        bool BeginSavedCinematic()
        {
            var kind = Host.TakeSavedCinematic();
            if (kind == AdamCinematicKind.None) return false;
            // A retry freezes play. An entry event resumes the same committed battle.
            if (AdamCinematicSpec.Find(kind).Boundary == AdamCinematicBoundary.Entry &&
                !Host.Suspended && Host.Paused) SetPlayPaused(false);
            if (!BeginCinematic(kind)) FinishCinematic();
            return true;
        }

        bool BeginCinematic(AdamCinematicKind kind)
        {
            var spec = AdamCinematicSpec.Find(kind);
            if (spec == null || (!spec.IsCurated && !AdamCinematicArt.Available)) return false;
            var script = Resources.Load<TextAsset>(spec.Resource);
            if (!cinematic.Begin(kind, script?.text)) return false;
            curatedArt.Begin(spec, cinematic);
            if (!spec.IsCurated) genesisCinematicMedia.Begin(kind, AdamCinematic.Read(kind, script.text));
            commandFrame = Time.frameCount;
            inputReleased = false;
            touchStarted = false;
            Host.SetMovement(Vector2.zero);
            ImpactAudio?.Stop();
            return true;
        }

        static bool CinematicControlsHeld(Keyboard keys, Gamepad pad)
        {
            return (keys != null && (keys.enterKey.isPressed || keys.escapeKey.isPressed ||
                keys.spaceKey.isPressed || keys.digit1Key.isPressed || keys.digit2Key.isPressed ||
                keys.digit3Key.isPressed || keys.f3Key.isPressed || keys.f4Key.isPressed || keys.f5Key.isPressed || keys.f10Key.isPressed ||
                keys.upArrowKey.isPressed || keys.downArrowKey.isPressed ||
                keys.leftArrowKey.isPressed || keys.rightArrowKey.isPressed ||
                keys.wKey.isPressed || keys.aKey.isPressed || keys.sKey.isPressed || keys.dKey.isPressed)) ||
                (pad != null && (pad.buttonEast.isPressed || pad.buttonSouth.isPressed ||
                    pad.startButton.isPressed || pad.selectButton.isPressed || pad.dpad.IsActuated())) ||
                Mouse.current?.leftButton.isPressed == true ||
                UnityEngine.InputSystem.EnhancedTouch.Touch.activeTouches.Count > 0;
        }

        bool UpdateCinematic(Keyboard keys)
        {
            var pad = Gamepad.current;
            if (!cinematic.Active)
            {
                if (releaseCinematicControls && commandFrame != Time.frameCount && !CinematicControlsHeld(keys, pad))
                    releaseCinematicControls = false;
                return releaseCinematicControls;
            }

            if (Host.Suspended) return true;
            // Enter can also select a weapon or enter a chapter. Escape and pad East
            // are available immediately, including on the frame that opened the scene.
            if (keys?.escapeKey.wasPressedThisFrame == true ||
                pad?.buttonEast.wasPressedThisFrame == true ||
                (commandFrame != Time.frameCount && keys?.enterKey.wasPressedThisFrame == true))
            {
                FinishCinematic();
                return true;
            }
            if (keys?.spaceKey.wasPressedThisFrame == true || pad?.startButton.wasPressedThisFrame == true)
                cinematic.Paused = !cinematic.Paused;

            cinematic.Advance(Time.unscaledDeltaTime);
            if (!cinematic.Active) FinishCinematic();
            return true;
        }

        void FinishCinematic(bool pointer = false)
        {
            cinematic.End();
            genesisCinematicMedia.Clear();
            curatedArt.Clear();
            cinematicExitFrame = commandFrame = Time.frameCount;
            var keys = Keyboard.current;
            releaseCinematicControls = pointer || CinematicControlsHeld(keys, Gamepad.current);
            inputReleased = !releaseCinematicControls;
            touchStarted = false;
        }

        bool CanReplayCinematics => Host?.Error == null && Host != null && !Host.Suspended &&
            !Host.PermanentSkillsOpen && !NewChallengeOpen && !DeathPresentationVisible &&
            (Host.Session == null || Host.BookComplete || Host.Paused ||
                (!Host.Session.IsActive && Host.Session.CurrentHealth <= 0));

        bool CanReplay(AdamCinematicSpec spec)
        {
            if (!CanReplayCinematics) return false;
            if (spec.Kind == AdamCinematicKind.Creation) return true;
            var settled = Host.State.Profile.HighestSettledRound.GetValueOrDefault();
            if (spec.Kind == AdamCinematicKind.Malachi && Host.State.OldTestamentClear is { } clear &&
                clear.RunId != Host.State.Run?.RunId) return true;
            if (spec.Kind == AdamCinematicKind.GenesisEnding &&
                (Host.Session?.ChapterComplete == true || Host.State.Run?.CompletedThirdBoss?.CompletionApplied == true))
                return true;
            return spec.Boundary == AdamCinematicBoundary.Settlement
                ? settled >= spec.Round || (Host.ContentComplete && Host.Session?.RoundNumber == spec.Round)
                : settled >= spec.Round || (Host.Session?.RoundNumber ?? 0) >= spec.Round;
        }

        internal bool Replay(AdamCinematicKind kind)
        {
            var spec = AdamCinematicSpec.Find(kind);
            if (commandFrame == Time.frameCount || releaseCinematicControls || spec == null || !CanReplay(spec))
                return false;
            if (!BeginCinematic(kind)) return false;
            replaySelection = -1;
            return true;
        }

        bool UpdateCinematicReplay(Keyboard keys)
        {
            var pad = Gamepad.current;
            if (replaySelection < 0)
            {
                if (keys?.f3Key.wasPressedThisFrame != true && pad?.selectButton.wasPressedThisFrame != true)
                    return false;
                return OpenCinematicReplay();
            }
            if (!CanReplayCinematics || keys?.escapeKey.wasPressedThisFrame == true ||
                pad?.buttonEast.wasPressedThisFrame == true)
            {
                replaySelection = -1;
                FinishCinematic();
                return true;
            }
            var count = AdamCinematicSpec.Segments.Count;
            if (keys?.rightArrowKey.wasPressedThisFrame == true || pad?.dpad.right.wasPressedThisFrame == true)
                replaySelection = (replaySelection + 1) % count;
            if (keys?.leftArrowKey.wasPressedThisFrame == true || pad?.dpad.left.wasPressedThisFrame == true)
                replaySelection = (replaySelection + count - 1) % count;
            if (keys?.downArrowKey.wasPressedThisFrame == true || pad?.dpad.down.wasPressedThisFrame == true)
                replaySelection = (replaySelection + 2) % count;
            if (keys?.upArrowKey.wasPressedThisFrame == true || pad?.dpad.up.wasPressedThisFrame == true)
                replaySelection = (replaySelection + count - 2) % count;
            if (keys?.enterKey.wasPressedThisFrame == true || pad?.buttonSouth.wasPressedThisFrame == true)
                Replay(AdamCinematicSpec.Segments[replaySelection].Kind);
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
            var spec = AdamCinematicSpec.Find(cinematic.Kind);
            var sourceRect = new Rect(0, 0, AdamCinematic.Width, AdamCinematic.Height);
            var motion = ArtWorld.FeedbackEnabled;
            var artLabel = "";
            if (spec.IsCurated)
            {
                if (curatedArt.TryGetFrame(cinematic, motion, out var media))
                {
                    if (media.LiveTerrain) DrawCinematicTerrain(viewport, frame, spec.Round, mobile, physicalFrame);
                    GUI.DrawTextureWithTexCoords(sourceRect, media.Texture, media.Uv);
                    if (curatedArt.Preview) artLabel = "컷신 검수용";
                }
                else
                {
                    AdamCinematicArt.DrawCuratedPrototype(cinematic, actor, scroll, motion);
                    artLabel = "컷신 시제품";
                }
            }
            else if (genesisCinematicMedia.TryGetFrame(cinematic, motion, viewport, out var genesisMedia))
            {
                if (genesisMedia.LiveTerrain)
                    DrawGenesisCinematicTerrain(viewport, frame, mobile, physicalFrame, genesisMedia.FullTerrain);
                var tint = GUI.color;
                try
                {
                    GUI.color = Color.white;
                    GUI.DrawTextureWithTexCoords(sourceRect, genesisMedia.Texture, genesisMedia.Uv);
                }
                finally { GUI.color = tint; }
            }
            else if (cinematic.Kind == AdamCinematicKind.ExodusOpening && cinematic.CurrentIndex == 2)
                ArtWorld.DrawBackdrop(art, sourceRect, ExodusContent.GlobalRoundNumber);
            else
                AdamCinematicArt.Draw(cinematic, horizon, actor, scroll, motion);
            GUI.matrix = Matrix4x4.TRS(viewport.position, Quaternion.identity, Vector3.one);

            var cut = cinematic.Current;
            if (artLabel.Length > 0)
            {
                AdamHud.Fill(new Rect(10, 8, 116, 30), AdamHud.Ink0);
                GUI.Label(new Rect(16, 10, 110, 26), artLabel, HudText(16, AdamHud.Primary, false));
            }
            var subtitleHeight = SubtitleHeight(viewport, frame);
            var subtitleTop = viewport.height - subtitleHeight;
            var compact = subtitleHeight < 90;
            AdamHud.Fill(new Rect(0, subtitleTop, viewport.width, subtitleHeight), AdamHud.Ink0);
            if (cut.day > 0)
            {
                AdamHud.Fill(new Rect(12, 10, 116, 32), AdamHud.Ink0);
                GUI.Label(new Rect(20, 10, 100, 32), $"제{cut.day}일",
                    HudText(20, AdamHud.Primary, false));
            }
            var captionFraction = compact ? (viewport.width >= 900 ? .68f : .55f) : 1;
            var captionSize = compact ? 16 : viewport.width < 760 ? 18 : 20;
            var caption = HudText(captionSize, AdamHud.Primary, false, TextAnchor.MiddleCenter);
            caption.wordWrap = true;
            GUI.Label(new Rect(10, subtitleTop, viewport.width * captionFraction - 20,
                compact ? subtitleHeight - 18 : 44), cut.caption, caption);
            GUI.Label(new Rect(10, subtitleTop + (compact ? subtitleHeight - 18 : 44),
                viewport.width * captionFraction - 20, 18), cut.reference,
                HudText(compact ? 13 : 16, AdamHud.Muted, false, TextAnchor.MiddleCenter));

            var enabled = GUI.enabled;
            GUI.enabled = enabled && !Host.Suspended;
            var controls = new GUIStyle(GUI.skin.button) { font = fonts.Label, fontSize = compact ? 14 : 16 };
            var pointer = Event.current.isMouse;
            var controlWidth = viewport.width * (1 - captionFraction) - 20;
            var pause = compact
                ? new Rect(viewport.width * captionFraction, subtitleTop + (subtitleHeight - 30) / 2 - 4, controlWidth * .42f, 30)
                : new Rect(viewport.width / 2 - 174, subtitleTop + 61, 166, 36);
            var skip = new Rect(pause.xMax + (compact ? 8 : 16), pause.y, compact ? controlWidth - pause.width - 8 : 166, pause.height);
            if (GUI.Button(pause, cinematic.Paused ? "재개" : compact ? "일시정지" : "일시정지 (Space)", controls) && pointer)
                cinematic.Paused = !cinematic.Paused;
            var skipLabel = skip.width >= 145 ? "건너뛰기 (Esc / B)" : "건너뛰기";
            if (GUI.Button(skip, skipLabel, controls) && pointer)
                FinishCinematic(pointer: true);
            GUI.enabled = enabled;
            GUI.matrix = prior;
        }

        void DrawCinematicTerrain(Rect viewport, Rect frame, int round, bool mobile, Rect? physicalFrame)
        {
            var prior = GUI.matrix;
            var scale = mobile ? AdamMobileLayout.Scale(viewport) : AdamChoiceText.ScreenScale(viewport.width, viewport.height);
            var arena = mobile ? AdamMobileLayout.FullFrame(viewport, physicalFrame ?? viewport)
                : Arena(viewport.width / scale, viewport.height / scale);
            var clip = new Rect((frame.position - viewport.position) / scale, frame.size / scale);
            GUI.matrix = Matrix4x4.TRS(viewport.position, Quaternion.identity, Vector3.one * scale);
            GUI.BeginGroup(clip);
            arena.position -= clip.position;
            ArtWorld.DrawBackdrop(art, arena, round);
            GUI.EndGroup();
            GUI.matrix = prior;
        }

        void DrawGenesisCinematicTerrain(Rect viewport, Rect frame, bool mobile, Rect? physicalFrame, bool fullTerrain)
        {
            var prior = GUI.matrix;
            var scale = mobile ? AdamMobileLayout.Scale(viewport) : AdamChoiceText.ScreenScale(viewport.width, viewport.height);
            GUI.matrix = Matrix4x4.TRS(viewport.position, Quaternion.identity, new Vector3(scale, scale, 1));
            var arena = mobile ? AdamMobileLayout.FullFrame(viewport, physicalFrame ?? viewport) :
                Arena(viewport.width / scale, viewport.height / scale);
            if (!fullTerrain)
            {
                // Letterbox gutters stay dark until the authored mask has fully revealed the arena.
                var clip = new Rect((frame.position - viewport.position) / scale, frame.size / scale);
                GUI.BeginGroup(clip);
                arena.position -= clip.position;
            }
            try
            {
                // A replay can start from a later save; use the opening camera and round.
                ArtWorld.DrawBackdrop(art, arena, ExodusContent.GlobalRoundNumber, Vector2.zero, mobile);
            }
            finally
            {
                if (!fullTerrain) GUI.EndGroup();
                GUI.matrix = prior;
            }
        }

        internal static float SubtitleHeight(Rect viewport, Rect frame) =>
            Mathf.Min(100, frame.width / AdamCinematic.Width * 25 + viewport.yMax - frame.yMax);

        bool OpenCinematicReplay()
        {
            if (!CanReplayCinematics || releaseCinematicControls || commandFrame == Time.frameCount) return false;
            replaySelection = 0;
            commandFrame = Time.frameCount;
            return true;
        }

        void DrawCinematicReplay(Vector2 origin)
        {
            if (!CanReplayCinematics) return;
            var style = new GUIStyle(GUI.skin.button) { font = fonts.Label, fontSize = 16 };
            var pointer = Event.current.isMouse;
            if (GUI.Button(new Rect(origin.x, origin.y, 166, 34), "컷신 다시 보기 (F3)", style) && pointer)
                OpenCinematicReplay();
        }

        void DrawCinematicReplayMenu(float width, float height)
        {
            AdamHud.Fill(new Rect(0, 0, width, height), AdamHud.Ink0);
            var panelWidth = Mathf.Min(490, width - 24);
            var row = Mathf.Min(42, (height - 100) / 7);
            var left = (width - panelWidth) / 2;
            var top = (height - (7 * row + 44)) / 2;
            GUI.Label(new Rect(left, top, panelWidth, 36), "컷신 다시 보기",
                HudText(22, AdamHud.Primary, false, TextAnchor.MiddleCenter));
            var prior = GUI.enabled;
            var pointer = Event.current.isMouse;
            var style = new GUIStyle(GUI.skin.button) { font = fonts.Label, fontSize = 16 };
            for (var i = 0; i < AdamCinematicSpec.Segments.Count; i++)
            {
                var spec = AdamCinematicSpec.Segments[i];
                GUI.enabled = prior && CanReplay(spec) && Resources.Load<TextAsset>(spec.Resource) != null;
                var rect = new Rect(left + i % 2 * (panelWidth / 2), top + 44 + i / 2 * row,
                    panelWidth / 2 - 4, row - 4);
                if (i == replaySelection)
                    AdamHud.Fill(new Rect(rect.x - 2, rect.y - 2, rect.width + 4, rect.height + 4), AdamHud.Primary);
                if (GUI.Button(rect, spec.Title, style) && pointer && Replay(spec.Kind)) break;
            }
            GUI.enabled = prior;
            if (GUI.Button(new Rect(left, top + 44 + 6 * row, panelWidth, row - 4), "돌아가기 (Esc)", style) &&
                pointer && commandFrame != Time.frameCount)
            {
                replaySelection = -1;
                FinishCinematic(pointer: true);
            }
        }
    }
}
