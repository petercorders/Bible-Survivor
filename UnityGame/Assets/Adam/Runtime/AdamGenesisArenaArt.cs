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
        static readonly Color Canvas = new Color32(153, 132, 102, 255);
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
                case Motif.Field: Field(art, stamp, east); break;
                case Motif.Ark: Ark(stamp, east); break;
                case Motif.Tent: Tent(stamp, east); break;
                case Motif.Gate: Gate(art, stamp, east); break;
                case Motif.Well: Well(stamp, east); break;
                case Motif.Bethel: Bethel(art, stamp, east); break;
                case Motif.Granary: Granary(stamp, east); break;
                case Motif.Hall: Hall(art, stamp, east); break;
            }
        }

        static void Eden(AdamArt art, Stamp stamp, bool east)
        {
            if (east)
            {
                ExistingOrFill(art, stamp, Rock, -20, 7, 14, 9, Ochre);
                if (!stamp.DrawProp(AdamGenesisPropArt.Seal))
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

        static void Field(AdamArt art, Stamp stamp, bool east)
        {
            if (east)
            {
                stamp.Fill(-17, 8, 34, 6, Clay);
                stamp.Fill(-13, 0, 26, 9, Ochre);
                stamp.Fill(-15, -3, 30, 4, Canvas);
                ExistingOrFill(art, stamp, Rock, 17, 4, 14, 9, Ochre);
                return;
            }
            stamp.Fill(-29, -18, 58, 36, Clay);
            for (var row = 0; row < 6; row++)
            {
                var x = row * 9 - 25;
                stamp.Fill(x, -15, 4, 30, Ochre);
                stamp.Fill(x + 1, -10, 1, 19, LightWood);
            }
        }

        static void Ark(Stamp stamp, bool east)
        {
            if (east)
            {
                Planks(stamp, -17, -6);
                return;
            }
            if (stamp.DrawProp(AdamGenesisPropArt.Ark)) return;
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

        static void Tent(Stamp stamp, bool east)
        {
            if (east)
            {
                stamp.Fill(-23, -12, 46, 19, Ink);
                stamp.Fill(-20, 4, 40, 3, Lapis);
                for (var i = 0; i < 6; i++)
                    stamp.Fill(-18 + i * 7, -9 + i * 5 % 11, 2, 2, Silver);
                return;
            }
            stamp.Fill(-34, 11, 68, 5, Clay);
            for (var row = 0; row < 7; row++)
                stamp.Fill(-5 - row * 4, -19 + row * 4, 10 + row * 8, 4,
                    row % 2 == 0 ? Canvas : LightWood);
            stamp.Fill(-10, 1, 20, 12, Ochre);
            stamp.Fill(-6, 3, 12, 10, Ink);
            stamp.Fill(-1, -17, 2, 29, Clay);
        }

        static void Gate(AdamArt art, Stamp stamp, bool east)
        {
            if (east)
            {
                stamp.Fill(-22, 7, 44, 8, Clay);
                stamp.Fill(-8, -13, 13, 21, Silver);
                stamp.Fill(-5, -16, 7, 5, Canvas);
                stamp.Fill(0, -12, 4, 20, Canvas);
                ExistingOrFill(art, stamp, Rock, 8, 4, 14, 9, Ochre);
                return;
            }
            stamp.Fill(-34, 11, 68, 8, Clay);
            stamp.Fill(-31, -12, 62, 9, Canvas);
            for (var x = -30; x < 30; x += 14)
                stamp.Fill(x, -18, 8, 6, Ochre);
            stamp.Fill(-31, -4, 17, 20, Ochre);
            stamp.Fill(14, -4, 17, 20, Ochre);
            stamp.Fill(-14, -4, 28, 8, Ink);
            stamp.Fill(-9, 4, 18, 13, LightWood);
        }

        static void Well(Stamp stamp, bool east)
        {
            stamp.Fill(-21, 1, 42, 15, Ochre);
            stamp.Fill(-24, -8, 48, 11, Canvas);
            stamp.Fill(-18, -5, 36, 7, east ? Clay : Lapis);
            stamp.Fill(-18, 12, 36, 3, Clay);
            stamp.Fill(-2, 4, 2, 8, LightWood);
            if (east) return;
            stamp.Fill(-19, -19, 4, 15, Clay);
            stamp.Fill(15, -19, 4, 15, Clay);
            stamp.Fill(-22, -22, 44, 4, LightWood);
            stamp.Fill(-1, -18, 2, 14, Canvas);
            stamp.Fill(24, 3, 9, 9, LightWood);
            stamp.Fill(26, 0, 5, 3, Ochre);
            stamp.Fill(26, 11, 5, 3, Clay);
        }

        static void Bethel(AdamArt art, Stamp stamp, bool east)
        {
            stamp.Fill(-29, 8, 58, 8, Ink);
            if (east)
            {
                ExistingOrFill(art, stamp, Rock, -13, 1, 14, 9, Ochre);
                stamp.Fill(7, 5, 17, 3, Clay);
                return;
            }
            stamp.Fill(-8, -18, 15, 29, Ochre);
            stamp.Fill(-5, -22, 11, 6, Silver);
            stamp.Fill(-8, -16, 10, 24, Canvas);
            stamp.Fill(-16, 7, 30, 5, Clay);
            ExistingOrFill(art, stamp, Rock, 11, 3, 14, 9, Ochre);
        }

        static void Granary(Stamp stamp, bool east)
        {
            if (east)
            {
                stamp.Fill(-25, -16, 50, 30, Clay);
                stamp.Fill(-22, -13, 44, 19, Ink);
                for (var x = -20; x < 20; x += 8)
                    stamp.Fill(x, -15, 2, 23, Silver);
                stamp.Fill(-22, -7, 44, 2, Canvas);
                return;
            }
            stamp.Fill(-29, -10, 58, 25, Ochre);
            stamp.Fill(-26, -16, 52, 5, Clay);
            stamp.Fill(-31, -12, 62, 4, Canvas);
            stamp.Fill(-10, -5, 20, 21, Ink);
            for (var x = -25; x <= 17; x += 42)
            {
                stamp.Fill(x, 5, 10, 9, LightWood);
                stamp.Fill(x + 2, 3, 6, 2, Canvas);
            }
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
            if (stamp.DrawProp(AdamGenesisPropArt.Cup)) return;
            stamp.Fill(9, -6, 9, 4, Silver);
            stamp.Fill(11, -2, 5, 2, Silver);
            stamp.Fill(13, 0, 1, 4, Silver);
            stamp.Fill(10, 4, 7, 1, Silver);
        }

        internal static Sprite PropSprite(AdamArt art, string id)
        {
            var sprite = art?.FindSprite(id);
            return sprite != null && sprite.texture != null ? sprite : null;
        }

        static void ExistingOrFill(AdamArt art, Stamp stamp, string id, int x, int y, int width, int height, Color fallback)
        {
            var sprite = PropSprite(art, id);
            if (sprite == null)
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
            public bool DrawProp(string id) => AdamGenesisPropArt.TryDraw(id, origin, Scale);
            public Rect Rect(int x, int y, int width, int height) =>
                new Rect(origin.x + x * Scale, origin.y + y * Scale, width * Scale, height * Scale);
            public void Fill(int x, int y, int width, int height, Color color) =>
                AdamHud.Fill(Rect(x, y, width, height), AdamHud.GuiColor(color));
        }
    }
}
