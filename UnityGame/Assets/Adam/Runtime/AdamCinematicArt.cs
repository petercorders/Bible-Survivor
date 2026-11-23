using UnityEngine;

namespace BibleSurvivor.Unity
{
    // Temporary composition using the existing Genesis frontispiece, clay/ink tones and actor sprite.
    internal static class AdamCinematicArt
    {
        static readonly Color Ink = new Color32(20, 25, 34, 255);
        static readonly Color Night = new Color32(42, 58, 106, 255);
        static readonly Color Lapis = new Color32(64, 88, 150, 255);
        static readonly Color Water = new Color32(42, 58, 106, 255);
        static readonly Color Rim = new Color32(138, 166, 214, 255);
        static readonly Color Ochre = new Color32(132, 98, 74, 255);
        static readonly Color Gold = new Color32(255, 232, 160, 255);
        static readonly Color Moss = new Color32(56, 84, 44, 255);
        static readonly Color Leaf = new Color32(98, 128, 58, 255);

        public static bool Available => AdamVisualQualityArt.TryGetClip(AdamBookTitle.Horizon, out _) &&
            AdamVisualQualityArt.TryGetClip(AdamBookTitle.Crest, out _);

        public static void Draw(AdamCinematic scene, Sprite horizon, Sprite figure, bool motion)
        {
            int day = scene.Current.day;
            int beat = motion && day != 7 ? Mathf.FloorToInt(scene.Elapsed * 8) : 0;
            if (day == 0)
            {
                DrawEnding(scene.CurrentIndex == 1, horizon, figure, beat);
                return;
            }

            Fill(0, 0, 480, 270, Ink);
            Fill(0, 15, 480, 136, day >= 2 ? Lapis : Night);
            // The first cut starts with a visible, slowly opening break in the darkness.
            if (day == 1)
            {
                var glow = 28 + (motion ? Mathf.Min(80, beat * 4) : 50);
                Fill(240 - glow, 138, glow * 2, 3, Gold);
                Fill(240 - glow / 2, 135, glow, 3, Ochre);
            }
            if (day >= 2)
            {
                Fill(0, 126, 480, 25, Night);
                Fill(0, 145, 480, 2, Rim);
            }
            if (day >= 3)
            {
                if (day >= 4) AdamArt.Blit(horizon, new Rect(40, 101, 400, 55));
                else
                {
                    Fill(0, 144, 480, 11, Moss);
                    Tree(128, 151, beat);
                    Tree(307, 152, beat);
                }
                Fill(0, 155, 480, 23, Moss);
                for (var i = 0; i < 16; i++)
                {
                    var x = 14 + i * 29;
                    Fill(x, 158 + i % 4 * 3, 2, 7, Night);
                    Fill(x - 2, 157 + i % 4 * 3, 6, 2, Leaf);
                }
            }
            if (day >= 4)
            {
                Fill(0, 17, 186, 84, Night);
                for (var i = 0; i < 12; i++)
                    Fill(14 + i * 13, 26 + i * 17 % 65, 2, 2, Rim);
                Fill(75, 52 + beat % 4, 13, 13, Rim);
                Fill(80, 50 + beat % 4, 11, 11, Night);
            }

            Fill(0, day >= 3 ? 178 : 151, 480, 119, Water);
            for (var row = 0; row < 3; row++)
            for (var i = 0; i < 8; i++)
            {
                var x = 8 + (i * 59 + row * 19 + beat * 2) % 452;
                Fill(x, 184 + row * 19, 10 + i % 4 * 4, 2, row == 0 ? Rim : Lapis);
            }
            if (day >= 5)
            {
                for (var i = 0; i < 5; i++)
                {
                    var fish = 74 + i * 22 + beat * 2;
                    Fill(fish, 208 + i % 2 * 7, 9, 3, Rim);
                    Fill(fish - 3, 207 + i % 2 * 7, 3, 5, Lapis);
                    var bird = 208 + i * 20 + beat;
                    Fill(bird, 64 + i % 2 * 7, 5, 2, Ink);
                    Fill(bird + 5, 66 + i % 2 * 7, 4, 2, Ink);
                    Fill(bird + 9, 64 + i % 2 * 7, 5, 2, Ink);
                }
            }
            if (day >= 6)
            {
                Animal(124, 157);
                Animal(156, 160);
                Figure(figure, 262, 142);
                Figure(figure, 297, 143, true);
            }
        }

        static void DrawEnding(bool last, Sprite horizon, Sprite figure, int beat)
        {
            Fill(0, 0, 480, 270, last ? Ink : Night);
            AdamArt.Blit(horizon, new Rect(40, 92, 400, 55));
            Fill(0, 149, 480, 121, last ? Ink : Water);
            if (last)
            {
                AdamArt.Blit(AdamVisualQualityArt.Sprite(AdamBookTitle.Crest, 0), new Rect(224, 103, 32, 32));
                Fill(186, 158, 108, 2, Ochre);
                return;
            }
            Figure(figure, 218, 128);
            Figure(figure, 261, 132, true);
            Figure(figure, 290, 132, true);
            for (var i = 0; i < 6; i++)
                Fill(62 + i * 35 + beat % 16, 164 + i % 3 * 6, 7, 1, Ochre);
        }

        static void Animal(int x, int y)
        {
            Fill(x, y, 18, 7, Ink);
            Fill(x + 16, y - 3, 7, 5, Ink);
            Fill(x + 2, y + 7, 3, 6, Night);
            Fill(x + 13, y + 7, 3, 6, Night);
        }

        static void Tree(int x, int y, int beat)
        {
            var height = 13 + Mathf.Min(14, beat % 65);
            Fill(x, y - height, 4, height, Ochre);
            Fill(x - 7, y - height - 5, 18, 8, Moss);
            Fill(x - 5, y - height - 9, 14, 7, Leaf);
        }

        static void Figure(Sprite figure, int x, int y, bool flip = false)
        {
            var tint = GUI.color;
            GUI.color *= new Color(.43f, .40f, .52f, 1);
            AdamArt.Blit(figure, new Rect(x, y, 32, 32), flip);
            GUI.color = tint;
        }

        static void Fill(int x, int y, int width, int height, Color color) =>
            AdamHud.Fill(new Rect(x, y, width, height), AdamHud.GuiColor(color));
    }
}
