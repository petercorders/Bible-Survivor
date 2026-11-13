using UnityEngine;

namespace BibleSurvivor.Unity
{
    public static class AdamMobileLayout
    {
        // Raster assets/fonts remain on an integer pixel grid. This is a phone QA profile, not a physical-size guarantee.
        public static int Scale(Rect safe) => Mathf.Clamp(Mathf.FloorToInt(Mathf.Min(safe.width / 560, safe.height / 480)), 1, 2);
        public static bool MovementTouchAllowed(Vector2 position, Rect safe) => safe.Contains(position) &&
            position.x < safe.x + safe.width * .5f && position.y < safe.y + safe.height * .65f;
        public static Rect Panel(float width, float height) => new Rect(12, (height - 390) / 2, width - 24, 390);
        public static Rect StartButton(float width, float height, int slot) => new Rect(24, height / 2 - 35 + slot * 74, width - 48, 66);
        public static Rect ChoicePanel(float width, float height) => new Rect(12, (height - 480) / 2, width - 24, 480);
        public static Rect ChoiceCard(Rect panel, int slot) => new Rect(panel.x + 12, panel.y + 84 + slot * 116, panel.width - 24, 110);
    }
}
