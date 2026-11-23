using UnityEngine;
using Motif = BibleSurvivor.Unity.AdamGenesisRoundStory.Motif;

namespace BibleSurvivor.Unity
{
    internal static class AdamGenesisArenaArt
    {
        internal const string Rock = "dec_eden_rock-m_14x9";
        internal const string Scroll = "ico_psv_scroll_static_16";
        static readonly Color Ink = new Color32(32, 34, 38, 255);
        static readonly Color Clay = new Color32(62, 48, 46, 255);
        static readonly Color Ochre = new Color32(116, 89, 67, 255);
        static readonly Color LightWood = new Color32(141, 112, 83, 255);
        static readonly Color Silver = new Color32(146, 151, 154, 255);
        static readonly Color Lapis = new Color32(64, 88, 150, 255);

        public static void Draw(AdamArt art, AdamWorldProjection projection, int round)
        {
            var story = AdamGenesisRoundStory.Find(round);
            if (story == null || Event.current.type != EventType.Repaint) return;
            DrawPlacement(art, projection, story.Environment, story.West, east: false);
            DrawPlacement(art, projection, story.Environment, story.East, east: true);
        }

        // Broad-phase clipping keeps these static compositions out of the draw loop when offscreen.
        internal static Rect Bounds(AdamWorldProjection projection, AdamGenesisRoundStory.Placement placement)
        {
            var at = projection.Point(placement.X, placement.Y);
            return new Rect(at.x - 40 * projection.Scale, at.y - 24 * projection.Scale,
                80 * projection.Scale, 48 * projection.Scale);
        }

        static void DrawPlacement(AdamArt art, AdamWorldProjection projection, Motif motif,
            AdamGenesisRoundStory.Placement placement, bool east)
        {
            var rect = Bounds(projection, placement);
            if (!rect.Overlaps(projection.TextureRect)) return;
            var stamp = new Stamp(rect.center, projection.Scale);
            switch (motif)
            {
                case Motif.Eden: Eden(art, stamp, east); break;
                case Motif.Ark: Ark(stamp, east); break;
                case Motif.Hall: Hall(art, stamp, east); break;
            }
        }

        static void Eden(AdamArt art, Stamp stamp, bool east)
        {
            if (east)
            {
                ExistingOrFill(art, stamp, Rock, -20, 7, 14, 9, Ochre);
                AdamCinematicArt.DrawEdenSeal(stamp.Rect(0, 0, 16, 16).position, stamp.Scale);
                return;
            }
            // Same blue bend as the creation scene, confined to the western bank.
            for (var row = 0; row < 6; row++)
            {
                var bend = row < 3 ? row : 5 - row;
                stamp.Fill(-27 + bend * 5, -21 + row * 7, 21, 7, Ink);
                stamp.Fill(-25 + bend * 5, -21 + row * 7, 16, 7, Lapis);
            }
            stamp.Fill(-17, -9, 10, 1, Silver * .65f);
            ExistingOrFill(art, stamp, Rock, 1, 4, 14, 9, Ochre);
        }

        static void Ark(Stamp stamp, bool east)
        {
            if (east)
            {
                Planks(stamp, -17, -6);
                return;
            }
            stamp.Fill(-34, 9, 68, 5, Clay);
            stamp.Fill(-29, 14, 58, 4, Ochre);
            for (var rib = 0; rib < 5; rib++)
            {
                var x = rib * 12 - 29;
                stamp.Fill(x, -17, 3, 28, Ochre);
                stamp.Fill(x + 3, -20, 4, 4, LightWood);
                stamp.Fill(x + 7, -19, 2, 27, Clay);
            }
            stamp.Fill(-33, -13, 65, 3, LightWood);
            stamp.Fill(-30, 3, 60, 3, Ochre);
        }

        static void Planks(Stamp stamp, int x, int y)
        {
            for (var i = 0; i < 3; i++)
            {
                stamp.Fill(x + i * 3, y + i * 5, 30, 4, Clay);
                stamp.Fill(x + i * 3 + 1, y + i * 5, 28, 2, Ochre);
            }
            stamp.Fill(x + 12, y, 2, 14, LightWood);
            stamp.Fill(x + 23, y + 1, 2, 13, LightWood);
        }

        static void Hall(AdamArt art, Stamp stamp, bool east)
        {
            stamp.Fill(-35, -19, 70, 36, Clay);
            stamp.Fill(-32, -16, 64, 2, Lapis);
            stamp.Fill(-32, 13, 64, 2, Lapis);
            for (var x = -30; x <= 20; x += 25)
            {
                stamp.Fill(x, -10, 10, 23, Ink);
                stamp.Fill(x + 2, -10, 7, 20, Ochre);
                stamp.Fill(x, -12, 11, 4, LightWood);
                stamp.Fill(x, 8, 11, 4, Clay);
            }
            if (!east) return;
            // A small shelf with the ending's scroll on the east and the story's silver cup next to it.
            stamp.Fill(4, 3, 30, 8, Ink);
            ExistingOrFill(art, stamp, Scroll, 19, -9, 16, 16, Ochre);
            stamp.Fill(9, -6, 9, 4, Silver);
            stamp.Fill(11, -2, 5, 2, Silver);
            stamp.Fill(13, 0, 1, 4, Silver);
            stamp.Fill(10, 4, 7, 1, Silver);
        }

        internal static bool CanReuse(Sprite sprite) => sprite != null && sprite.texture != null;

        static void ExistingOrFill(AdamArt art, Stamp stamp, string id, int x, int y, int width, int height, Color fallback)
        {
            var sprite = art?.FindSprite(id);
            if (!CanReuse(sprite))
            {
                stamp.Fill(x + 2, y + 2, width - 4, height - 3, fallback);
                return;
            }
            var prior = GUI.color;
            try
            {
                GUI.color = prior * new Color(.72f, .72f, .72f, 1);
                AdamArt.Blit(sprite, stamp.Rect(x, y, width, height));
            }
            finally { GUI.color = prior; }
        }

        readonly struct Stamp
        {
            readonly Vector2 origin;
            public readonly int Scale;
            public Stamp(Vector2 origin, int scale) { this.origin = origin; Scale = scale; }
            public Rect Rect(int x, int y, int width, int height) =>
                new Rect(origin.x + x * Scale, origin.y + y * Scale, width * Scale, height * Scale);
            public void Fill(int x, int y, int width, int height, Color color) =>
                AdamHud.Fill(Rect(x, y, width, height), AdamHud.GuiColor(color));
        }
    }
}
