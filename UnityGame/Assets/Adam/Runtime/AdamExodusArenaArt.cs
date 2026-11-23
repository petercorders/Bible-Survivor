using BibleSurvivor.Core;
using UnityEngine;

namespace BibleSurvivor.Unity
{
    internal static class AdamExodusArenaArt
    {
        // Quiet material ramps. The brickwork repeats the clay and ochre of the intro's yard.
        static readonly Color Clay = new Color32(58, 42, 44, 255);
        static readonly Color Brick = new Color32(117, 76, 58, 255);
        static readonly Color Ochre = new Color32(132, 98, 74, 255);
        static readonly Color Sandstone = new Color32(157, 125, 93, 255);
        static readonly Color Water = new Color32(66, 91, 104, 255);
        static readonly Color Shore = new Color32(97, 118, 115, 255);
        static readonly Color Reed = new Color32(102, 109, 67, 255);
        static readonly Color Leaf = new Color32(72, 93, 66, 255);
        static readonly Color Linen = new Color32(169, 149, 118, 255);
        static readonly Color Shade = new Color32(58, 42, 44, 95);

        // The western slots sit below the start HUD at the centered 660×440 camera.
        internal static readonly Vector2 West = new Vector2(-5.25f, 1.5f);
        internal static readonly Vector2 BossWest = new Vector2(-5.25f, 1f);
        internal static readonly Vector2 East = new Vector2(5.25f, -1.5f);

        public static void Draw(AdamWorldProjection projection, int round)
        {
            if (round < ExodusContent.GlobalRoundNumber || round > ExodusContent.LastInstalledRound ||
                Event.current.type != EventType.Repaint) return;
            var west = new Stamp(projection, round is 12 or 15 ? BossWest : West);
            if (round == 10)
            {
                var east = new Stamp(projection, East);
                if (west.Overlaps(projection.TextureRect)) BrickKiln(west);
                if (east.Overlaps(projection.TextureRect)) BrickStack(east);
                return;
            }
            if (!west.Overlaps(projection.TextureRect)) return;
            switch (round)
            {
                case 11: ReedBank(west); break;
                case 12: SeaPassage(west); break;
                case 13: Elim(west); break;
                case 14: Sinai(west); break;
                case 15: Tabernacle(west); break;
            }
        }

        static void BrickKiln(Stamp s)
        {
            s.Fill(-27, 8, 58, 10, Shade);
            s.Fill(-24, 1, 48, 12, Brick);
            s.Fill(-23, -9, 46, 14, Ochre);
            s.Fill(-19, -15, 38, 8, Brick);
            s.Fill(-15, -17, 30, 4, Sandstone);
            s.Fill(-21, -6, 42, 2, Sandstone);
            s.Fill(-20, 1, 40, 2, Brick);
            s.Fill(-9, -1, 18, 12, Clay);
            s.Fill(-6, -4, 12, 4, Clay);
            s.Fill(-17, 6, 2, 5, Ochre);
            s.Fill(16, 6, 2, 5, Ochre);
        }

        static void BrickStack(Stamp s)
        {
            s.Fill(-29, 7, 59, 9, Shade);
            for (var row = 0; row < 3; row++)
            for (var column = 0; column < 4 - row; column++)
            {
                var x = -26 + column * 13 + row * 6;
                var y = 5 - row * 6;
                s.Fill(x, y, 12, 7, Brick);
                s.Fill(x, y, 12, 2, Ochre);
                s.Fill(x + 10, y + 2, 2, 5, Clay);
            }
        }

        static void ReedBank(Stamp s)
        {
            s.Fill(-34, -13, 61, 27, Shore);
            s.Fill(-33, -11, 53, 21, Water);
            s.Fill(-28, -15, 31, 4, Water);
            s.Fill(20, -8, 9, 13, Shore);
            s.Fill(-29, 4, 18, 1, Shore);
            for (var i = 0; i < 7; i++)
            {
                var x = -24 + i * 7;
                var y = -11 - i % 3 * 3;
                s.Fill(x, y, 2, 12, Reed);
                s.Fill(x - 2, y, 3, 3, Ochre);
            }
            s.Fill(1, 3, 17, 9, Clay);
            s.Fill(3, 2, 14, 7, Ochre);
            s.Fill(5, 3, 9, 3, Linen);
            s.Fill(5, 9, 9, 1, Sandstone);
        }

        static void SeaPassage(Stamp s)
        {
            for (var bank = -1; bank <= 1; bank += 2)
            for (var step = 0; step < 4; step++)
            {
                var inner = 7 + step % 3 * 4;
                var x = bank < 0 ? -35 : inner;
                var y = -18 + step * 9;
                s.Fill(x, y, 35 - inner, 9, Water);
                s.Fill(bank < 0 ? -inner - 3 : inner, y, 3, 9, Shore);
                s.Fill(bank < 0 ? x + 5 : x + 8, y + 2, 8, 1, Shore);
            }
            for (var i = 0; i < 3; i++)
                s.Fill(-3 + i % 2 * 4, -11 + i * 9, 3, 2, Ochre);
        }

        static void Elim(Stamp s)
        {
            s.Fill(-27, -2, 53, 18, Shore);
            s.Fill(-22, 0, 44, 15, Water);
            s.Fill(-13, -5, 24, 5, Water);
            s.Fill(-17, 11, 22, 1, Shore);
            for (var i = 0; i < 3; i++)
            {
                var x = -24 + i * 21;
                var y = -12 - i % 2 * 5;
                s.Fill(x - 5, y + 13, 17, 5, Shade);
                s.Fill(x, y, 3, 17, Ochre);
                s.Fill(x - 7, y - 1, 17, 4, Leaf);
                s.Fill(x - 9, y + 3, 7, 3, Reed);
                s.Fill(x + 5, y + 2, 7, 4, Reed);
                s.Fill(x - 2, y - 5, 7, 4, Leaf);
            }
        }

        static void Sinai(Stamp s)
        {
            s.Fill(-34, 9, 69, 9, Shade);
            s.Fill(-33, 0, 64, 13, Brick);
            s.Fill(-26, -6, 50, 12, Ochre);
            s.Fill(-19, -12, 34, 11, Brick);
            s.Fill(-11, -17, 17, 12, Ochre);
            s.Fill(-4, -20, 9, 10, Sandstone);
            s.Fill(-17, -4, 12, 3, Sandstone);
            s.Fill(-25, 4, 15, 3, Sandstone);
            s.Fill(4, -10, 5, 14, Clay);
            s.Fill(10, 3, 12, 3, Clay);
            s.Fill(20, 6, 8, 5, Ochre);
        }

        static void Tabernacle(Stamp s)
        {
            s.Fill(-35, 9, 70, 10, Shade);
            s.Fill(-34, -6, 2, 21, Linen);
            s.Fill(32, -6, 2, 21, Linen);
            s.Fill(-33, 13, 25, 3, Linen);
            s.Fill(8, 13, 25, 3, Linen);
            s.Fill(-23, -7, 46, 15, Ochre);
            s.Fill(-22, -12, 44, 6, Brick);
            s.Fill(-17, -17, 34, 6, Brick);
            s.Fill(-14, -19, 28, 4, Sandstone);
            s.Fill(-20, -10, 40, 2, Sandstone);
            s.Fill(-7, -4, 14, 12, Clay);
            s.Fill(-9, -6, 3, 15, Linen);
            s.Fill(6, -6, 3, 15, Linen);
            s.Fill(-5, 5, 10, 3, Brick);
        }

        readonly struct Stamp
        {
            readonly Vector2 origin;
            readonly int scale;

            public Stamp(AdamWorldProjection projection, Vector2 placement)
            {
                origin = projection.Point(placement.x, placement.y);
                scale = projection.Scale;
            }

            public bool Overlaps(Rect viewport) => Rect(-36, -22, 72, 42).Overlaps(viewport);

            Rect Rect(int x, int y, int width, int height) =>
                new Rect(origin.x + x * scale, origin.y + y * scale, width * scale, height * scale);

            public void Fill(int x, int y, int width, int height, Color color) =>
                AdamHud.Fill(Rect(x, y, width, height), AdamHud.GuiColor(color));
        }
    }
}
