using UnityEngine;

namespace BibleSurvivor.Unity
{
    // Technical compositions use the frontispiece palette and existing sprite artwork.
    internal static class AdamCinematicArt
    {
        static readonly Color Ink = new Color32(20, 25, 34, 255);
        static readonly Color Night = new Color32(42, 58, 106, 255);
        static readonly Color Lapis = new Color32(64, 88, 150, 255);
        static readonly Color Water = new Color32(92, 122, 188, 255);
        static readonly Color Pale = new Color32(138, 166, 214, 255);
        static readonly Color Ochre = new Color32(132, 98, 74, 255);
        static readonly Color Clay = new Color32(58, 42, 44, 255);
        static readonly Color Gold = new Color32(255, 232, 160, 255);
        static readonly Color Moss = new Color32(56, 84, 44, 255);
        static readonly Color Leaf = new Color32(98, 128, 58, 255);

        public static bool Available => AdamVisualQualityArt.TryGetClip(AdamBookTitle.Horizon, out _);

        public static void Draw(AdamCinematic scene, Sprite horizon, Sprite figure, Sprite scroll, bool motion)
        {
            int beat = motion ? Mathf.FloorToInt(scene.CutElapsed * 8) : 0;
            if (scene.Kind == AdamCinematicKind.ExodusOpening)
            {
                if (scene.CurrentIndex == 0)
                {
                    DrawEnding(true, figure, scroll);
                    var shade = Ink;
                    shade.a = Mathf.Clamp01((beat - 12) / 20f) * .45f;
                    if (shade.a > 0) Fill(0, 0, 480, 270, shade);
                }
                else DrawBrickworks(figure, beat);
                return;
            }
            switch (scene.Current.day)
            {
                case 1: FirstLight(beat, motion); break;
                case 2: SkyAndWater(beat); break;
                case 3: LandAndPlants(beat, motion); break;
                case 4: LightsInTheSky(horizon, beat); break;
                case 5: FishAndBirds(beat); break;
                case 6: LandAnimals(beat); break;
                default: DrawEnding(scene.CurrentIndex == 1, figure, scroll); break;
            }
        }

        // Temporary compositions for timing, input and subtitle verification.
        public static void DrawCuratedPrototype(AdamCinematic scene, Sprite figure, Sprite scroll, bool motion)
        {
            var cut = scene.CurrentIndex;
            var beat = motion ? Mathf.FloorToInt(scene.CutElapsed * 8) : 0;
            switch (scene.Kind)
            {
                case AdamCinematicKind.Judges:
                    Fill(0, 0, 480, 124, Pale);
                    Fill(0, 124, 480, 146, Ochre);
                    for (var i = 0; i < 3; i++)
                    {
                        var x = 50 + i * 135;
                        Fill(x - 8, 100 + 12 * i, 86, 14, Clay);
                        Fill(x, 114 + 12 * i, 70, 53, Ochre);
                        Fill(x + 26, 135 + 12 * i, 18, 32, Clay);
                    }
                    if (cut == 1)
                    {
                        Fill(208, 169, 100, 18, Clay);
                        Fill(227, 154, 61, 15, Pale);
                    }
                    else
                    {
                        Figure(figure, 196 - beat / 4, 151);
                        Figure(figure, 245 + beat / 3, 172, true);
                    }
                    break;
                case AdamCinematicKind.Samuel:
                    DrawEnding(false, figure, scroll);
                    Fill(158, 96, 164, 111, Clay);
                    Pillar(145, 72);
                    Pillar(319, 72);
                    Fill(324, 132, 8, 7, Gold);
                    Figure(figure, 228, cut == 1 ? 159 : 141);
                    Figure(figure, 281, 119, true);
                    break;
                case AdamCinematicKind.Kings:
                case AdamCinematicKind.Isaiah:
                case AdamCinematicKind.Daniel:
                    DrawEnding(false, figure, scroll);
                    if (scene.Kind == AdamCinematicKind.Daniel)
                    {
                        Fill(148, 90, 184, 23, Lapis);
                        Fill(167, 177, 146, 12, Lapis);
                    }
                    if (scene.Kind == AdamCinematicKind.Isaiah && cut == 1)
                    {
                        Fill(223, 155, 34, 14, Ochre);
                        Fill(235, 150, 9, 5, Gold);
                    }
                    if (scene.Kind == AdamCinematicKind.Kings && cut == 1)
                    {
                        Fill(176 - beat / 2, 57, 128 + beat, 41, Pale);
                        Fill(222, 163, 43, 24, Gold);
                    }
                    break;
                case AdamCinematicKind.Nehemiah:
                case AdamCinematicKind.Habakkuk:
                    Fill(0, 0, 480, 128, Night);
                    Fill(0, 128, 480, 142, Clay);
                    for (var row = 0; row < 4; row++)
                    {
                        var width = scene.Kind == AdamCinematicKind.Nehemiah ? 230 + row * 23 : 89;
                        var start = scene.Kind == AdamCinematicKind.Nehemiah ? 36 : 210;
                        for (var x = start; x < start + width; x += 30)
                            Fill(x + row % 2 * 8, 81 + row * 17, 26, 14, Ochre);
                    }
                    Figure(figure, scene.Kind == AdamCinematicKind.Nehemiah ? 308 : 231,
                        scene.Kind == AdamCinematicKind.Nehemiah ? 139 : 48);
                    break;
                case AdamCinematicKind.Jonah:
                    Fill(0, 0, 480, 270, Night);
                    Fill(0, 92, 480, 178, Lapis);
                    Waves(cut == 0 ? 92 : 162, beat, Pale);
                    if (cut == 0)
                    {
                        Fill(176, 98, 119, 13, Ochre);
                        Fill(194, 111, 82, 10, Clay);
                        Fill(225, 42, 4, 57, Ochre);
                        Fill(229, 44, 33, 35, Pale);
                    }
                    else if (cut == 2)
                    {
                        Fill(132, 113, 201, 40, Ink);
                        Fill(171, 98, 152, 71, Ink);
                        Fill(113, 111, 39, 45, Ink);
                        Fill(295, 123, 4, 4, Pale);
                    }
                    else Figure(figure, 228, 127 + beat);
                    break;
                case AdamCinematicKind.Malachi:
                    if (cut == 0)
                    {
                        Fill(0, 0, 480, 270, Ink);
                        Fill(0, 127, 480, 143, Clay);
                        for (var i = 0; i < 9; i++)
                            Fill(27 + i * 51, 127 - (i * 7 + beat) % 14, 9, 20, Ochre);
                    }
                    else
                    {
                        FirstLight(Mathf.Min(27, beat), motion, showSeal: false);
                        if (cut == 1)
                        {
                            Fill(334, 99, 31, 36, Gold);
                            Fill(327, 106, 45, 22, Gold);
                        }
                        else
                        {
                            Animal(151 + beat, 178, 2, Ochre, beat);
                            Animal(278 + beat, 198, 1, Clay, beat);
                        }
                    }
                    break;
            }
        }

        static void FirstLight(int beat, bool motion, bool showSeal = true)
        {
            Fill(0, 0, 480, 156, Ink);
            var opening = motion ? 124 + beat * 8 : 220;
            Fill(480 - opening, 0, opening, 156, Night);
            Fill(480 - opening + 26, 0, opening - 26, 156, Lapis);
            Fill(480 - opening + 60, 0, opening - 60, 156, Pale);
            Fill(480 - opening + 92, 0, opening - 92, 156, Gold);
            Fill(0, 156, 480, 114, Night);
            Fill(480 - opening + 16, 156, opening - 16, 4, Gold);
            Waves(170, beat, Pale);
            if (showSeal) DrawEdenSeal(new Vector2(24, 213), 1);
        }

        static void SkyAndWater(int beat)
        {
            Fill(0, 0, 480, 142, Pale);
            for (var i = 0; i < 4; i++)
            {
                var x = 20 + i * 109 + beat % 12;
                var y = 54 + i % 2 * 22;
                Fill(x, y, 76, 11, Gold);
                Fill(x + 12, y - 12, 45, 15, Gold);
                Fill(x + 22, y - 18, 20, 8, Gold);
            }
            Fill(0, 140, 480, 6, Gold);
            Fill(0, 146, 480, 124, Lapis);
            Waves(171, beat, Pale);
        }

        static void LandAndPlants(int beat, bool motion)
        {
            Fill(0, 0, 480, 106, Pale);
            Fill(0, 106, 480, 164, Moss);
            Fill(0, 130, 480, 24, Leaf);
            DrawRiver();
            var growth = motion ? Mathf.Min(14, beat) : 14;
            Tree(104, 185, growth);
            Tree(176, 162, growth);
            Tree(332, 179, growth);
            Tree(407, 205, growth);
        }

        static void LightsInTheSky(Sprite horizon, int beat)
        {
            Fill(0, 0, 480, 270, Night);
            Fill(248, 0, 232, 150, Lapis);
            for (var i = 0; i < 13; i++)
                Fill(27 + i * 15, 37 + i * 17 % 82, 2 + i % 3, 2 + i % 3, Pale);
            var moonY = 49 + beat / 3;
            Fill(77, moonY, 23, 32, Pale);
            Fill(70, moonY + 6, 38, 20, Pale);
            Fill(85, moonY - 3, 25, 24, Night);
            AdamArt.Blit(horizon, new Rect(40, 126, 400, 55));
            Fill(0, 181, 480, 89, Moss);
        }

        static void FishAndBirds(int beat)
        {
            Fill(0, 0, 480, 100, Pale);
            Fill(0, 100, 480, 4, Gold);
            Fill(0, 104, 480, 166, Lapis);
            Fill(0, 116, 480, 36, Water);
            Waves(121, beat, Pale);
            for (var i = 0; i < 5; i++)
            {
                var x = 40 + i * 77 + beat;
                var y = 168 + i % 2 * 37;
                Fill(x, y, 24, 11, Pale);
                Fill(x + 3, y - 3, 15, 17, Pale);
                Fill(x - 6, y - 1, 7, 13, Water);
                Fill(x + 18, y + 3, 2, 2, Night);
                var birdX = 71 + i * 67 + beat;
                var birdY = 47 + i % 2 * 20;
                Fill(birdX, birdY - 6, 9, 4, Ink);
                Fill(birdX + 9, birdY - 2, 8, 4, Ink);
                Fill(birdX + 17, birdY - 6, 9, 4, Ink);
            }
        }

        static void LandAnimals(int beat)
        {
            Fill(0, 0, 480, 113, Gold);
            Fill(0, 113, 480, 157, Leaf);
            Fill(0, 142, 480, 128, Moss);
            Tree(72, 150, 14);
            Tree(416, 153, 14);
            DrawRiver();
            Animal(99 + beat, 170, 2, Clay, beat);
            Animal(267 + beat, 179, 2, Ochre, beat);
            Animal(352 + beat, 201, 1, Gold, beat);
        }

        static void DrawEnding(bool last, Sprite figure, Sprite scroll)
        {
            Fill(0, 0, 480, 270, Ink);
            Fill(0, 84, 480, 186, Clay);
            for (var i = 0; i < 6; i++)
                Fill(28 + i * 14, 125 + i * 18, 424 - i * 28, 2, Ochre);
            for (var i = 0; i < 4; i++)
            {
                var x = 35 + i * 44;
                Pillar(x, 74 + i * 4);
                Pillar(425 - i * 44, 74 + i * 4);
            }
            if (!last)
            {
                Figure(figure, 210, 117);
                Figure(figure, 241, 132, true);
                Figure(figure, 272, 128, true);
            }
            else
            {
                Fill(396, 176, 37, 6, Ochre);
                Fill(401, 182, 26, 8, Ink);
                AdamArt.Blit(scroll, new Rect(408, 162, 16, 16));
            }
        }

        static void Pillar(int x, int y)
        {
            Fill(x - 6, y, 23, 7, Clay);
            Fill(x, y + 7, 12, 91, Ochre);
            Fill(x + 2, y + 7, 2, 91, Clay);
            Fill(x - 6, y + 98, 23, 7, Ink);
        }

        static void DrawBrickworks(Sprite figure, int beat)
        {
            Fill(0, 0, 480, 107, Ochre);
            Fill(0, 107, 480, 163, new Color32(178, 135, 90, 255));
            Fill(56, 104, 89, 52, Clay);
            Fill(61, 94, 79, 16, Clay);
            Fill(77, 126, 41, 30, Ink);
            Fill(87, 142, 21, 10, Ochre);
            for (var i = 0; i < 3; i++)
                Fill(83 + (beat / 4 + i * 2) % 13, 63 + i * 7, 11 - 2 * i, 5, Clay);
            for (var row = 0; row < 4; row++)
            for (var column = 0; column < 5 - row; column++)
            {
                var x = 329 + column * 22 + row * 7;
                var y = 171 - row * 9;
                Fill(x, y, 19, 7, Clay);
                Fill(x, y, 19, 2, Ochre);
            }
            Figure(figure, 172 + beat / 3, 141);
            Fill(177 + beat / 3, 148, 18, 6, Clay);
            Figure(figure, 232 - beat / 4, 159, true);
            Fill(237 - beat / 4, 166, 18, 6, Clay);
        }

        public static void DrawEdenSeal(Vector2 origin, int scale)
        {
            void Stamp(int x, int y, int w, int h, Color color) =>
                AdamHud.Fill(new Rect(origin.x + x * scale, origin.y + y * scale, w * scale, h * scale),
                    AdamHud.GuiColor(color));
            Stamp(0, 4, 16, 11, Clay);
            Stamp(2, 1, 12, 13, Clay);
            Stamp(2, 5, 12, 8, Ochre);
            Stamp(7, 5, 2, 7, Ink);
            Stamp(5, 4, 6, 4, Moss);
            Stamp(6, 3, 4, 2, Leaf);
        }

        static void Waves(int y, int beat, Color edge)
        {
            for (var row = 0; row < 3; row++)
            for (var i = 0; i < 8; i++)
            {
                var x = 8 + (i * 59 + row * 19 + beat * 2) % 452;
                Fill(x, y + row * 21, 10 + i % 4 * 4, 2, row == 0 ? edge : Water);
            }
        }

        static void DrawRiver()
        {
            Fill(215, 141, 26, 44, Lapis);
            Fill(210, 177, 37, 41, Lapis);
            Fill(191, 211, 48, 31, Lapis);
            Fill(176, 236, 50, 34, Lapis);
        }

        static void Animal(int x, int y, int scale, Color coat, int beat)
        {
            var step = beat % 3;
            Fill(x, y, 20 * scale, 10 * scale, coat);
            Fill(x + 18 * scale, y - 4 * scale, 9 * scale, 7 * scale, coat);
            Fill(x + (3 + step) * scale, y + 10 * scale, 3 * scale, 8 * scale, Clay);
            Fill(x + (15 - step) * scale, y + 10 * scale, 3 * scale, 8 * scale, Clay);
        }

        static void Tree(int x, int y, int growth)
        {
            var height = 15 + growth;
            Fill(x, y - height, 6, height, Ochre);
            Fill(x - 13, y - height - 8, 32, 13, Moss);
            Fill(x - 9, y - height - 14, 25, 12, Leaf);
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
