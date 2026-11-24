using System;
using UnityEngine;

namespace BibleSurvivor.Unity
{
    internal sealed class AdamCuratedCinematicArt
    {
        internal const int FramesPerSecond = 12;
        const int Columns = 4, RowsPerPage = 7, FramesPerPage = Columns * RowsPerPage;

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
            public readonly Texture2D Texture;
            public readonly Rect Uv;
            public readonly bool LiveTerrain;

            public Frame(Texture2D texture, Rect uv, bool liveTerrain)
            {
                Texture = texture;
                Uv = uv;
                LiveTerrain = liveTerrain;
            }
        }

        string root, currentAsset;
        MediaIndex index;
        Texture2D currentTexture;
        internal bool Preview => index?.status == "review";

        internal void Begin(AdamCinematicSpec spec, AdamCinematic timeline)
        {
            Clear();
            if (!spec.IsCurated) return;
            root = spec.Resource + "-media";
            var manifest = Resources.Load<TextAsset>(root + "/index");
            if (manifest == null) return;
            MediaIndex candidate;
            try { candidate = JsonUtility.FromJson<MediaIndex>(manifest.text); }
            catch (ArgumentException) { return; }
            if (candidate?.shots == null || candidate.shots.Length != spec.CutCount ||
                candidate.designVersion <= 0 || (candidate.status != "review" && candidate.status != "approved")) return;

            for (var i = 0; i < candidate.shots.Length; i++)
            {
                var shot = candidate.shots[i];
                if (shot == null || shot.frames != Mathf.CeilToInt(timeline.CutDuration(i) * FramesPerSecond) ||
                    (shot.liveTerrain && (spec.Boundary != AdamCinematicBoundary.Entry || i != candidate.shots.Length - 1)))
                    return;
            }
            index = candidate;
        }

        internal bool TryGetFrame(AdamCinematic timeline, bool motion, out Frame frame)
        {
            frame = default;
            if (index == null) return false;

            var shot = index.shots[timeline.CurrentIndex];
            var prefix = root + "/cut-" + (timeline.CurrentIndex + 1);
            if (!motion)
            {
                var still = Load(prefix + "-still");
                if (still == null || still.width != AdamCinematic.Width || still.height != AdamCinematic.Height)
                    return AbandonInvalidMedia();
                frame = new Frame(still, new Rect(0, 0, 1, 1), false);
                return true;
            }

            var number = Mathf.Clamp(Mathf.FloorToInt(timeline.CutElapsed * FramesPerSecond), 0, shot.frames - 1);
            var page = number / FramesPerPage;
            var texture = Load(prefix + "-page-" + page);
            var pageFrames = Math.Min(FramesPerPage, shot.frames - page * FramesPerPage);
            var rows = (pageFrames + Columns - 1) / Columns;
            if (texture == null || texture.width != Columns * AdamCinematic.Width ||
                texture.height != rows * AdamCinematic.Height) return AbandonInvalidMedia();

            var cell = number % FramesPerPage;
            var uv = new Rect(cell % Columns / (float)Columns, (rows - 1 - cell / Columns) / (float)rows,
                1f / Columns, 1f / rows);
            frame = new Frame(texture, uv, shot.liveTerrain);
            return true;
        }

        bool AbandonInvalidMedia()
        {
            Clear();
            return false;
        }

        Texture2D Load(string asset)
        {
            if (asset == currentAsset) return currentTexture;
            ReleaseTexture();
            currentAsset = asset;
            currentTexture = Resources.Load<Texture2D>(asset);
            return currentTexture;
        }

        internal void Clear()
        {
            index = null;
            root = currentAsset = null;
            ReleaseTexture();
        }

        void ReleaseTexture()
        {
            if (currentTexture != null) Resources.UnloadAsset(currentTexture);
            currentTexture = null;
        }
    }
}
