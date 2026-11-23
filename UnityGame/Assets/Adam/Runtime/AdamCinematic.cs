using UnityEngine;

namespace BibleSurvivor.Unity
{
    internal enum AdamCinematicKind { None, Creation, GenesisEnding }

    internal sealed class AdamCinematic
    {
        internal const int Width = 480, Height = 270;
        internal const int CreationSeconds = 20, EndingSeconds = 13;

        [System.Serializable]
        internal sealed class Cut
        {
            public int day;
            public string reference, caption;
            public float end;
        }

        [System.Serializable]
        sealed class Sequence { public Cut[] cuts; }

        Cut[] cuts;

        public static string ResourceName(AdamCinematicKind kind) =>
            kind == AdamCinematicKind.Creation ? "GenesisCinematics/creation" : "GenesisCinematics/ending";

        internal static Cut[] Read(AdamCinematicKind kind, string json)
        {
            if (kind == AdamCinematicKind.None || string.IsNullOrEmpty(json)) return null;
            Sequence sequence;
            try { sequence = JsonUtility.FromJson<Sequence>(json); }
            catch (System.ArgumentException) { return null; }
            bool creation = kind == AdamCinematicKind.Creation;
            var items = sequence?.cuts;
            if (items == null || items.Length != (creation ? 7 : 2)) return null;
            var previousEnd = 0f;
            for (var i = 0; i < items.Length; i++)
            {
                var cut = items[i];
                if (cut == null || cut.day != (creation ? i + 1 : 0) ||
                    !(cut.end > previousEnd) || string.IsNullOrWhiteSpace(cut.reference) ||
                    string.IsNullOrWhiteSpace(cut.caption)) return null;
                previousEnd = cut.end;
            }
            return previousEnd == (creation ? CreationSeconds : EndingSeconds) ? items : null;
        }

        public AdamCinematicKind Kind { get; private set; }
        public float Elapsed { get; private set; }
        public bool Paused { get; set; }
        public bool Active => Kind != AdamCinematicKind.None;
        public int CurrentIndex
        {
            get
            {
                for (var i = 0; i < cuts.Length; i++)
                    if (Elapsed < cuts[i].end) return i;
                return cuts.Length - 1;
            }
        }
        public Cut Current => cuts[CurrentIndex];

        public bool Begin(AdamCinematicKind kind, string json)
        {
            if (Active) return false;
            cuts = Read(kind, json);
            if (cuts == null) return false;
            Kind = kind;
            Elapsed = 0;
            Paused = false;
            return true;
        }

        public void Advance(float seconds)
        {
            if (!Active || Paused) return;
            Elapsed += Mathf.Clamp(seconds, 0, .1f);
            if (Elapsed >= cuts[cuts.Length - 1].end) End();
        }

        public void End() => Kind = AdamCinematicKind.None;

        public static Rect Frame(Rect available)
        {
            var fit = Mathf.Min(available.width / Width, available.height / Height);
            var scale = fit >= 1 ? Mathf.Floor(fit) : fit;
            var width = Width * scale;
            var height = Height * scale;
            return new Rect(available.x + Mathf.Floor((available.width - width) / 2),
                available.y + Mathf.Floor((available.height - height) / 2), width, height);
        }
    }
}
