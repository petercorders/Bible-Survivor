using BibleSurvivor.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace BibleSurvivor.Unity
{
    public sealed partial class AdamGame
    {
        int genesisStoryRound;

        bool CanReadGenesisStory => CanReplayCinematics &&
            (Host.Session == null || Host.Session.PendingChoices.Count == 0 &&
                Host.Session.Stage != "boss_reward" && Host.Session.Stage != "boss_settlement" &&
                Host.Session.Stage != "round_settlement" && !Host.AwaitingNextRound);

        void OpenGenesisStory()
        {
            if (!CanReadGenesisStory || commandFrame == Time.frameCount) return;
            genesisStoryRound = Mathf.Clamp(Host.Session?.RoundNumber ?? 1, 1, ExodusContent.LastInstalledRound);
            commandFrame = Time.frameCount;
        }

        bool HandleGenesisStoryInput(Keyboard keys)
        {
            var toggle = keys?.f4Key.wasPressedThisFrame == true;
            if (genesisStoryRound == 0)
            {
                if (!toggle || !CanReadGenesisStory) return false;
                OpenGenesisStory();
                return genesisStoryRound != 0;
            }
            // Retain the selected page across app suspension. A different underlying screen closes it.
            if (Host.Suspended) return true;
            var pad = Gamepad.current;
            if (!CanReadGenesisStory || toggle || keys?.escapeKey.wasPressedThisFrame == true ||
                pad?.buttonEast.wasPressedThisFrame == true)
            {
                CloseGenesisStory();
                return true;
            }
            if (keys?.leftArrowKey.wasPressedThisFrame == true || pad?.leftShoulder.wasPressedThisFrame == true)
                ChangeGenesisStory(-1);
            else if (keys?.rightArrowKey.wasPressedThisFrame == true || pad?.rightShoulder.wasPressedThisFrame == true)
                ChangeGenesisStory(1);
            return true;
        }

        void CloseGenesisStory()
        {
            genesisStoryRound = 0;
            commandFrame = Time.frameCount;
        }

        void ChangeGenesisStory(int direction) =>
            genesisStoryRound = Mathf.Clamp(genesisStoryRound + direction, 1, ExodusContent.LastInstalledRound);

        void DrawGenesisStoryEntry(float width)
        {
            if (!CanReadGenesisStory) return;
            var style = new GUIStyle(GUI.skin.button) { font = fonts.Label, fontSize = 16 };
            var position = drawingMobile || Host.BookComplete
                ? new Vector2(14, 8)
                : new Vector2(width - 182, StartStageAvailable ? 48 : 8);
            if (GUI.Button(new Rect(position, new Vector2(168, 36)),
                drawingMobile ? "성경 기록" : "성경 기록 (F4)", style))
                OpenGenesisStory();
        }

        void DrawGenesisStory(float width, float height)
        {
            var genesis = AdamGenesisRoundStory.Find(genesisStoryRound);
            var exodus = AdamExodusRoundStory.Find(genesisStoryRound);
            AdamHud.Fill(new Rect(0, 0, width, height), AdamHud.Ink0);
            if (genesis == null && exodus == null) return;
            var book = genesis != null ? "창세기" : "출애굽기";

            var panelWidth = Mathf.Min(width - 24, 600);
            var panelHeight = Mathf.Min(height - 24, 340);
            var panel = new Rect((width - panelWidth) / 2, (height - panelHeight) / 2, panelWidth, panelHeight);
            AdamHud.Panel(art, panel);
            var inset = 16f;
            var contentWidth = panel.width - inset * 2;
            GUI.Label(new Rect(panel.x + inset, panel.y + 10, contentWidth, 26),
                $"{book} 기록 · R{genesisStoryRound:00}", HudText(16, AdamHud.Lapis, false, TextAnchor.MiddleCenter));
            GUI.Label(new Rect(panel.x + inset, panel.y + 38, contentWidth, 38), genesis?.Title ?? exodus.Title,
                HudText(24, AdamHud.Primary, false, TextAnchor.MiddleCenter));
            GUI.Label(new Rect(panel.x + inset, panel.y + 76, contentWidth, 28),
                (genesis?.Reference ?? exodus.Reference) + " · 자체 요약", HudText(16, AdamHud.Secondary, false, TextAnchor.MiddleCenter));

            var summary = HudText(20, AdamHud.Primary);
            summary.wordWrap = true;
            var summaryRect = new Rect(panel.x + inset, panel.y + 111, contentWidth, Mathf.Max(0, panel.height - 202));
            if (summaryRect.height > 0) GUI.Label(summaryRect, genesis?.Summary ?? exodus.Summary, summary);
            GUI.Label(new Rect(panel.x + inset, panel.yMax - 89, contentWidth, 24),
                "전장과 전투는 게임 각색입니다.", HudText(16, AdamHud.Muted, false, TextAnchor.MiddleCenter));

            var style = new GUIStyle(GUI.skin.button) { font = fonts.Label, fontSize = 16 };
            var oldEnabled = GUI.enabled;
            var buttonWidth = (contentWidth - 16) / 3;
            var y = panel.yMax - 57;
            try
            {
                GUI.enabled = oldEnabled && !Host.Suspended && genesisStoryRound > 1;
                if (GUI.Button(new Rect(panel.x + inset, y, buttonWidth, 42), "이전", style)) ChangeGenesisStory(-1);
                GUI.enabled = oldEnabled && !Host.Suspended;
                if (GUI.Button(new Rect(panel.x + inset + buttonWidth + 8, y, buttonWidth, 42),
                    drawingMobile ? "닫기" : "닫기 (Esc)", style)) CloseGenesisStory();
                GUI.enabled = oldEnabled && !Host.Suspended && genesisStoryRound < ExodusContent.LastInstalledRound;
                if (GUI.Button(new Rect(panel.x + inset + 2 * (buttonWidth + 8), y, buttonWidth, 42), "다음", style))
                    ChangeGenesisStory(1);
            }
            finally { GUI.enabled = oldEnabled; }
        }
    }
}
