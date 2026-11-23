using UnityEngine;

namespace BibleSurvivor.Unity
{
    internal enum AdamCinematicKind
    {
        None, Creation, GenesisEnding, ExodusOpening, Judges, Samuel, Kings,
        Nehemiah, Isaiah, Daniel, Jonah, Habakkuk, Malachi
    }

    internal enum AdamCinematicBoundary { Entry, Settlement }

    internal sealed class AdamCinematicSpec
    {
        static readonly AdamCinematicSpec[] catalog =
        {
            new(AdamCinematicKind.Creation, 1, AdamCinematicBoundary.Entry,
                "GenesisCinematics/creation", "창조 이야기", 6, AdamCinematic.CreationSeconds),
            new(AdamCinematicKind.GenesisEnding, 9, AdamCinematicBoundary.Settlement,
                "GenesisCinematics/ending", "연결편 · 창세기 마무리", 2, AdamCinematic.EndingSeconds),
            new(AdamCinematicKind.ExodusOpening, 10, AdamCinematicBoundary.Entry,
                "GenesisCinematics/exodus-opening", "연결편 · 출애굽기 시작", 3, AdamCinematic.ExodusSeconds),
            new(AdamCinematicKind.Judges, 43, AdamCinematicBoundary.Entry,
                "CuratedCinematics/r43-judges", "흩어진 지파의 마을", 3, 13),
            new(AdamCinematicKind.Samuel, 55, AdamCinematicBoundary.Entry,
                "CuratedCinematics/r55-samuel", "한나의 기도", 3, 13.5f),
            new(AdamCinematicKind.Kings, 67, AdamCinematicBoundary.Entry,
                "CuratedCinematics/r67-kings", "솔로몬과 성전", 3, 12),
            new(AdamCinematicKind.Nehemiah, 94, AdamCinematicBoundary.Entry,
                "CuratedCinematics/r94-nehemiah", "무너진 성벽", 3, 12),
            new(AdamCinematicKind.Isaiah, 121, AdamCinematicBoundary.Entry,
                "CuratedCinematics/r121-isaiah", "성전과 숯불", 3, 12),
            new(AdamCinematicKind.Daniel, 151, AdamCinematicBoundary.Entry,
                "CuratedCinematics/r151-daniel", "바벨론 왕궁", 3, 12),
            new(AdamCinematicKind.Jonah, 166, AdamCinematicBoundary.Entry,
                "CuratedCinematics/r166-jonah", "폭풍과 큰 물고기", 3, 12),
            new(AdamCinematicKind.Habakkuk, 172, AdamCinematicBoundary.Entry,
                "CuratedCinematics/r172-habakkuk", "파수대", 3, 12),
            new(AdamCinematicKind.Malachi, 180, AdamCinematicBoundary.Settlement,
                "CuratedCinematics/r180-malachi", "공의로운 해", 3, 14)
        };

        internal static System.Collections.Generic.IReadOnlyList<AdamCinematicSpec> Segments { get; } =
            System.Array.AsReadOnly(catalog);
        internal static AdamCinematicSpec Find(AdamCinematicKind kind) =>
            System.Array.Find(catalog, item => item.Kind == kind);
        internal static AdamCinematicSpec Find(int round, AdamCinematicBoundary boundary) =>
            System.Array.Find(catalog, item => item.Round == round && item.Boundary == boundary);

        internal readonly AdamCinematicKind Kind;
        internal readonly int Round, CutCount;
        internal readonly float Seconds;
        internal readonly AdamCinematicBoundary Boundary;
        internal readonly string Resource, Title;
        internal bool IsCurated => Round > 10;
        // The Genesis–Exodus film has two independent playback and save boundaries.
        internal AdamCinematicKind Film => Kind == AdamCinematicKind.ExodusOpening ? AdamCinematicKind.GenesisEnding : Kind;

        AdamCinematicSpec(AdamCinematicKind kind, int round, AdamCinematicBoundary boundary,
            string resource, string title, int cutCount, float seconds)
        {
            Kind = kind;
            Round = round;
            Boundary = boundary;
            Resource = resource;
            Title = title;
            CutCount = cutCount;
            Seconds = seconds;
        }
    }

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

        public static string ResourceName(AdamCinematicKind kind) =>
            AdamCinematicSpec.Find(kind)?.Resource ?? throw new System.ArgumentOutOfRangeException(nameof(kind));

        internal static Cut[] Read(AdamCinematicKind kind, string json)
        {
            var spec = AdamCinematicSpec.Find(kind);
            if (spec == null || string.IsNullOrEmpty(json)) return null;
            Sequence sequence;
            try { sequence = JsonUtility.FromJson<Sequence>(json); }
            catch (System.ArgumentException) { return null; }
            var items = sequence?.cuts;
            if (items == null || items.Length != spec.CutCount) return null;
            var previousEnd = 0f;
            for (var i = 0; i < items.Length; i++)
            {
                var cut = items[i];
                if (cut == null || cut.day != (kind == AdamCinematicKind.Creation ? i + 1 : 0) ||
                    !(cut.end > previousEnd) || string.IsNullOrWhiteSpace(cut.reference) ||
                    string.IsNullOrWhiteSpace(cut.caption)) return null;
                previousEnd = cut.end;
            }
            return previousEnd == spec.Seconds ? items : null;
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
        public float CutElapsed => Elapsed - CutStart(CurrentIndex);
        internal float CutDuration(int index) => cuts[index].end - CutStart(index);
        float CutStart(int index) => index == 0 ? 0 : cuts[index - 1].end;

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
