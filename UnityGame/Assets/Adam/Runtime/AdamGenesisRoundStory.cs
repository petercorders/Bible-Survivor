using System;

namespace BibleSurvivor.Unity
{
    // Presentation data only. A saved round number is enough to reconstruct the same scene.
    internal sealed class AdamGenesisRoundStory
    {
        internal enum Motif { Eden, Field, Ark, Tent, Gate, Well, Bethel, Granary, Hall }

        public readonly struct Placement
        {
            public readonly float X, Y;
            public Placement(float x, float y) { X = x; Y = y; }
        }

        public readonly int Round;
        public readonly Motif Environment;
        public readonly string Title, Reference, Summary;
        public readonly Placement West, East;

        AdamGenesisRoundStory(int round, Motif environment, string title, string reference, string summary,
            Placement west, Placement east)
        {
            Round = round;
            Environment = environment;
            Title = title;
            Reference = reference;
            Summary = summary;
            West = west;
            East = east;
        }

        static readonly AdamGenesisRoundStory[] Stories =
        {
            new AdamGenesisRoundStory(1, Motif.Eden, "동산과 물길", "창 2:8–10",
                "하나님은 에덴에 동산을 마련하고 사람을 그곳에 두셨다. 에덴에서 흘러나온 강이 동산을 적셨다.",
                new Placement(-5.5f, 2.5f), new Placement(5.5f, -1.5f)),
            null,
            new AdamGenesisRoundStory(3, Motif.Ark, "노아의 방주", "창 6:14–16",
                "하나님은 노아에게 나무로 방주를 만들고, 내부를 나누고, 방주의 크기와 구조를 갖추라고 하셨다.",
                new Placement(-5.5f, 2.5f), new Placement(4.5f, -2f)),
            null, null, null, null, null,
            new AdamGenesisRoundStory(9, Motif.Hall, "은잔과 베냐민", "창 44:12, 18–34",
                "베냐민의 자루에서 은잔이 나왔다. 유다는 아버지의 슬픔을 염려해 베냐민 대신 자신이 남겠다고 청했다.",
                new Placement(-5.5f, 2.5f), new Placement(5f, -1.75f))
        };

        public static AdamGenesisRoundStory Find(int round) =>
            round >= 1 && round <= Stories.Length ? Stories[round - 1] : null;
    }
}
