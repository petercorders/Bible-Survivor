using System;
using UnityEngine;

namespace BibleSurvivor.Unity
{
    internal sealed class AdamGenesisCinematicMedia
    {
        const int Columns = 4, RowsPerPage = 7, FramesPerPage = Columns * RowsPerPage;
        const int FramesPerSecond = 12;
        const int YardRevealFrame = 39, YardFullRevealFrame = 46;
        const string YardLayoutResource = "ExodusArenaProps/r10-yard-v25";
        internal const string YardLayoutV25 = "samer10-v25-72x42-west-5.25-1.5-east5.25--1.5";

        [Serializable]
        sealed class MediaIndex
        {
            public int designVersion;
            public string status;
            public Shot[] shots;
        }

        [Serializable]
        sealed class Shot
        {
            public int frames;
            public bool liveTerrain;
        }

        internal readonly struct Frame
        {
            internal readonly Texture2D Texture;
            internal readonly Rect Uv;
            internal readonly bool LiveTerrain, FullTerrain;

            internal Frame(Texture2D texture, Rect uv, bool liveTerrain = false, bool fullTerrain = false)
            {
                Texture = texture;
                Uv = uv;
                LiveTerrain = liveTerrain;
                FullTerrain = fullTerrain;
            }
        }

        string root, currentAsset, yardLayout;
        MediaIndex index;
        Texture2D currentTexture;
        bool ownsPresentationTexture;
        static ushort[] linearChannels;

        internal void Begin(AdamCinematicKind kind, AdamCinematic.Cut[] cuts)
        {
            Clear();
            root = AdamCinematic.ResourceName(kind) + "-media";
            var asset = Resources.Load<TextAsset>(root + "/index");
            if (asset == null) return;

            MediaIndex candidate;
            try { candidate = JsonUtility.FromJson<MediaIndex>(asset.text); }
            catch (ArgumentException) { return; }

            if (candidate?.shots == null || candidate.shots.Length != cuts.Length ||
                candidate.designVersion != 23 || candidate.status != "approved") return;

            var previousEnd = 0f;
            for (var i = 0; i < cuts.Length; i++)
            {
                var shot = candidate.shots[i];
                // Timeline JSON is float-valued, so avoid a one-frame ceil at exact boundaries.
                var expectedFrames = Mathf.CeilToInt((cuts[i].end - previousEnd) * FramesPerSecond - .0001f);
                var revealsTerrain = kind == AdamCinematicKind.ExodusOpening && i == cuts.Length - 1;
                if (shot == null || shot.frames != expectedFrames || shot.liveTerrain != revealsTerrain)
                    return;
                previousEnd = cuts[i].end;
            }

            index = candidate;
            if (kind == AdamCinematicKind.ExodusOpening)
                yardLayout = Resources.Load<TextAsset>(YardLayoutResource)?.text;
        }

        internal bool TryGetFrame(AdamCinematic timeline, bool motion, Rect viewport, out Frame frame)
        {
            frame = default;
            if (index == null) return false;

            var cut = timeline.CurrentIndex;
            var shot = index.shots[cut];
            var prefix = root + "/cut-" + (cut + 1);
            if (!motion)
            {
                var still = Load(prefix + "-still");
                if (!HasSize(still, AdamCinematic.Width, AdamCinematic.Height)) return AbandonInvalidMedia();
                frame = new Frame(still, new Rect(0, 0, 1, 1));
                return true;
            }

            var number = Mathf.Clamp(Mathf.FloorToInt(timeline.CutElapsed * FramesPerSecond), 0, shot.frames - 1);
            var page = number / FramesPerPage;
            var standard = prefix + "-page-" + page;
            var variant = shot.liveTerrain && page == 1 ? YardVariant(yardLayout, viewport) : null;
            var texture = variant == null ? Load(standard) : Load(prefix + "-r10-" + variant + "-page-1", standard);
            var pageFrames = Math.Min(FramesPerPage, shot.frames - page * FramesPerPage);
            var rows = (pageFrames + Columns - 1) / Columns;
            if (!HasSize(texture, Columns * AdamCinematic.Width, rows * AdamCinematic.Height))
                return AbandonInvalidMedia();

            var cell = number % FramesPerPage;
            var uv = new Rect(cell % Columns / (float)Columns, (rows - 1 - cell / Columns) / (float)rows,
                1f / Columns, 1f / rows);
            frame = new Frame(texture, uv, shot.liveTerrain && number >= YardRevealFrame,
                shot.liveTerrain && number >= YardFullRevealFrame);
            return true;
        }

        internal static string YardVariant(string layout, Rect viewport)
        {
            if (layout != YardLayoutV25) return null;
            if (viewport.width == 1920 && viewport.height == 1080) return "1920x1080";
            if (viewport.width == 660 && viewport.height == 440) return "660x440";
            return null;
        }

        static bool HasSize(Texture2D texture, int width, int height) =>
            texture != null && texture.width == width && texture.height == height;

        bool AbandonInvalidMedia()
        {
            Clear();
            return false;
        }

        Texture2D Load(string asset, string fallback = null)
        {
            if (asset == currentAsset) return currentTexture;
            ReleaseTexture();
            currentAsset = asset;
            var source = Resources.Load<Texture2D>(asset);
            if (source == null && fallback != null) source = Resources.Load<Texture2D>(fallback);
            if (source == null) return null;
            if (QualitySettings.activeColorSpace != ColorSpace.Linear)
                return currentTexture = source;

            try
            {
                currentTexture = CreatePresentationTexture(source);
                ownsPresentationTexture = currentTexture != null;
                return currentTexture;
            }
            finally { Resources.UnloadAsset(source); }
        }

        static Texture2D CreatePresentationTexture(Texture2D source)
        {
            if (!source.isReadable || source.format != TextureFormat.RGBA32 ||
                !SystemInfo.SupportsTextureFormat(TextureFormat.RGBAHalf)) return null;

            // Match the project's IMGUI palette on the Linear framebuffer, without touching source PNGs.
            if (linearChannels == null)
            {
                linearChannels = new ushort[256];
                for (var i = 0; i < linearChannels.Length; i++)
                {
                    if (i == 0) continue;
                    var linearOutput = Mathf.GammaToLinearSpace(i / 255f);
                    // IMGUI encodes with the sRGB power branch, then the framebuffer
                    // encodes with full sRGB. The normal toe formula loses near-black levels here.
                    var guiInput = Mathf.Pow((linearOutput + .055f) / 1.055f, 2.4f);
                    linearChannels[i] = Mathf.FloatToHalf(guiInput);
                }
            }

            var raw = source.GetPixelData<Color32>(0);
            var rgba = new ushort[raw.Length * 4];
            var opaque = Mathf.FloatToHalf(1);
            for (var i = 0; i < raw.Length; i++)
            {
                var pixel = raw[i];
                var target = i * 4;
                rgba[target] = linearChannels[pixel.r];
                rgba[target + 1] = linearChannels[pixel.g];
                rgba[target + 2] = linearChannels[pixel.b];
                rgba[target + 3] = pixel.a == 0 ? (ushort)0 : opaque;
            }

            var texture = new Texture2D(source.width, source.height, TextureFormat.RGBAHalf, false, true)
            {
                filterMode = FilterMode.Point,
                wrapMode = TextureWrapMode.Clamp,
                hideFlags = HideFlags.HideAndDontSave
            };
            texture.SetPixelData(rgba, 0);
            texture.Apply(false, true);
            return texture;
        }

        internal void Clear()
        {
            index = null;
            root = currentAsset = yardLayout = null;
            ReleaseTexture();
        }

        void ReleaseTexture()
        {
            if (currentTexture != null)
            {
                if (!ownsPresentationTexture) Resources.UnloadAsset(currentTexture);
                else if (Application.isPlaying) UnityEngine.Object.Destroy(currentTexture);
                else UnityEngine.Object.DestroyImmediate(currentTexture);
            }
            currentTexture = null;
            ownsPresentationTexture = false;
        }
    }
}
