using UnityEngine;

namespace BibleSurvivor.Unity
{
    internal enum AdamCinematicKind { None, Creation, GenesisEnding, ExodusOpening }

    internal sealed class AdamCinematic
    {
        internal const int Width = 480, Height = 270;
        internal const int CreationSeconds = 20, EndingSeconds = 13, ExodusSeconds = 14;

        [System.Serializable]
        internal sealed class Cut
        {
            public int day;
            public string reference, caption, motif;
            public float end;
        }

        [System.Serializable]
        sealed class Sequence { public Cut[] cuts; }

        Cut[] cuts;

        public static string ResourceName(AdamCinematicKind kind) => kind switch
        {
            AdamCinematicKind.Creation => "GenesisCinematics/creation",
            AdamCinematicKind.GenesisEnding => "GenesisCinematics/ending",
            AdamCinematicKind.ExodusOpening => "GenesisCinematics/exodus-opening",
            _ => throw new System.ArgumentOutOfRangeException(nameof(kind))
        };

        internal static Cut[] Read(AdamCinematicKind kind, string json)
        {
            if (kind == AdamCinematicKind.None || string.IsNullOrEmpty(json)) return null;
            Sequence sequence;
            try { sequence = JsonUtility.FromJson<Sequence>(json); }
            catch (System.ArgumentException) { return null; }
            var (count, duration) = kind switch
            {
                AdamCinematicKind.Creation => (6, CreationSeconds),
                AdamCinematicKind.GenesisEnding => (2, EndingSeconds),
                AdamCinematicKind.ExodusOpening => (3, ExodusSeconds),
                _ => (0, 0)
            };
            var items = sequence?.cuts;
            if (count == 0 || items == null || items.Length != count) return null;
            var previousEnd = 0f;
            for (var i = 0; i < items.Length; i++)
            {
                var cut = items[i];
                if (cut == null || cut.day != (kind == AdamCinematicKind.Creation ? i + 1 : 0) ||
                    !(cut.end > previousEnd) || string.IsNullOrWhiteSpace(cut.reference) ||
                    string.IsNullOrWhiteSpace(cut.caption)) return null;
                previousEnd = cut.end;
            }
            return previousEnd == duration ? items : null;
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
        public float CutElapsed => CurrentIndex == 0 ? Elapsed : Elapsed - cuts[CurrentIndex - 1].end;

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
