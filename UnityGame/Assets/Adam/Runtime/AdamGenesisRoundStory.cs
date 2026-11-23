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
            new AdamGenesisRoundStory(2, Motif.Field, "제물과 형제", "창 4:3–8",
                "가인은 땅의 소산을, 아벨은 양 떼의 첫 새끼를 드렸다. 가인은 들에서 동생 아벨을 쳐 죽였다.",
                new Placement(-5.25f, 2.25f), new Placement(5f, -1.75f)),
            new AdamGenesisRoundStory(3, Motif.Ark, "노아의 방주", "창 6:14–16",
                "하나님은 노아에게 나무로 방주를 만들고, 내부를 나누고, 방주의 크기와 구조를 갖추라고 하셨다.",
                new Placement(-5.5f, 2.5f), new Placement(4.5f, -2f)),
            new AdamGenesisRoundStory(4, Motif.Tent, "별과 약속", "창 15:5, 18:1",
                "하나님은 아브라함에게 하늘의 별을 보여 주며 많은 후손을 약속하셨다. 훗날 아브라함은 마므레에서 천막 입구에 앉아 있었다.",
                new Placement(-5f, 2.5f), new Placement(5.25f, -1.5f)),
            new AdamGenesisRoundStory(5, Motif.Gate, "성을 떠나는 길", "창 19:15–26",
                "롯과 가족은 소돔에서 벗어나라는 재촉을 받았다. 길을 떠난 롯의 아내는 뒤를 돌아보다 소금 기둥이 되었다.",
                new Placement(-4.75f, 2.25f), new Placement(5.5f, -1.75f)),
            new AdamGenesisRoundStory(6, Motif.Well, "르호봇 우물", "창 26:19–22",
                "이삭의 사람들은 우물을 팔 때마다 다툼을 겪었다. 이삭은 다툼 없이 판 우물을 르호봇이라 불렀다.",
                new Placement(-4.75f, 2.25f), new Placement(5.25f, -1.75f)),
            new AdamGenesisRoundStory(7, Motif.Bethel, "꿈과 돌기둥", "창 28:10–19",
                "야곱은 돌을 베고 잠들어 하늘에 닿는 사닥다리를 꿈꾸었다. 아침에 그 돌을 기둥으로 세우고 그곳을 벧엘이라 불렀다.",
                new Placement(-5.25f, 2.25f), new Placement(5.25f, -1.5f)),
            new AdamGenesisRoundStory(8, Motif.Granary, "옷과 감옥과 곡식", "창 37:3, 39:20, 41:48–49",
                "요셉은 아버지에게 특별한 옷을 받았다. 이집트에서 감옥에 갇혔고, 훗날 풍년 동안 곡식을 모아 성읍마다 저장했다.",
                new Placement(-5.5f, 2.25f), new Placement(5f, -1.75f)),
            new AdamGenesisRoundStory(9, Motif.Hall, "은잔과 베냐민", "창 44:12, 18–34",
                "베냐민의 자루에서 은잔이 나왔다. 유다는 아버지의 슬픔을 염려해 베냐민 대신 자신이 남겠다고 청했다.",
                new Placement(-5.5f, 2.5f), new Placement(5f, -1.75f))
        };

        public static AdamGenesisRoundStory Find(int round) =>
            round >= 1 && round <= Stories.Length ? Stories[round - 1] : null;
    }
}
