using UnityEngine;

namespace BibleSurvivor.Unity
{
    public static class AdamMobileLayout
    {
        const float Edge = 12;
        const float ActionGap = 8;
        const float ActionHeight = 66;
        const float ActionTop = 150;
        const float ChoiceTop = 92;
        const float ChoiceGap = 6;

        // Raster assets/fonts remain on an integer pixel grid. This is a phone QA profile, not a physical-size guarantee.
        public static int Scale(Rect safe) => Mathf.Clamp(Mathf.FloorToInt(Mathf.Min(safe.width / 560, safe.height / 480)), 1, 2);

        // Both rectangles use top-left physical coordinates, matching the IMGUI matrix.
        public static Rect FullFrame(Rect safe, Rect frame)
        {
            var scale = Scale(safe);
            return new Rect((frame.position - safe.position) / scale, frame.size / scale);
        }

        public static bool MovementTouchAllowed(Vector2 position, Rect safe) => safe.Contains(position) &&
            position.x < safe.x + safe.width * .5f && position.y < safe.y + safe.height * .65f;

        // The pause target keeps its full height, while book and progress rows start below it.
        static float HudColumn(float width) => Mathf.Floor((width - 2 * Edge - 2 * ActionGap) / 3);
        public static Rect HudHealth(float width) => new Rect(Edge, 12, Mathf.Min(260, HudColumn(width)), 22);
        public static Rect HudTimer(float width)
        {
            var timerWidth = Mathf.Min(132, HudColumn(width));
            return new Rect(Mathf.Floor((width - timerWidth) / 2), 8, timerWidth, 40);
        }
        public static Rect HudPause(float width)
        {
            var buttonWidth = Mathf.Min(150, HudColumn(width));
            return new Rect(width - Edge - buttonWidth, 6, buttonWidth, ActionHeight);
        }
        public static Rect HudExperience(float width)
        {
            var value = HudExperienceValue(width);
            return new Rect(94, 98, value.x - 102, 12);
        }
        public static Rect HudExperienceValue(float width)
        {
            var valueWidth = Mathf.Clamp(Mathf.Floor(width * .26f), 112, 180);
            return new Rect(width - Edge - valueWidth, 92, valueWidth, 22);
        }
        public static Rect HudFooter(float width, float height) => new Rect(0, height - 110, width, 110);
        public static Rect HudFooterControl(float width, float height, int slot)
        {
            var column = HudColumn(width);
            return new Rect(Edge + slot * (column + ActionGap), height - 103, Mathf.Min(112, column), 24);
        }

        public static Rect Panel(float width, float height) => CenteredPanel(width, height, 620, 390);
        public static Rect ChoicePanel(float width, float height) => CenteredPanel(width, height, 760, 480);

        static Rect CenteredPanel(float width, float height, float maxWidth, float maxHeight)
        {
            var panelWidth = Mathf.Min(maxWidth, width - 2 * Edge);
            var panelHeight = Mathf.Min(maxHeight, height - 2 * Edge);
            return new Rect(Mathf.Floor((width - panelWidth) / 2), Mathf.Floor((height - panelHeight) / 2), panelWidth, panelHeight);
        }

        public static Rect StartButton(float width, float height, int slot) => ChoiceCard(ChoicePanel(width, height), slot);

        public static Rect Action(Rect panel, int slot, int count)
        {
            var rowHeight = Mathf.Min(ActionHeight, Mathf.Floor((panel.height - ActionTop - 32 - (count - 1) * ActionGap) / count));
            return new Rect(panel.x + 2 * Edge, panel.y + ActionTop + slot * (rowHeight + ActionGap), panel.width - 4 * Edge, rowHeight);
        }

        public static Rect ChoiceCard(Rect panel, int slot)
        {
            var footer = panel.width < 550 ? 60 : 40;
            var cardHeight = Mathf.Min(110, Mathf.Floor((panel.height - ChoiceTop - footer - 2 * ChoiceGap) / 3));
            return new Rect(panel.x + Edge, panel.y + ChoiceTop + slot * (cardHeight + ChoiceGap), panel.width - 2 * Edge, cardHeight);
        }
    }
}
