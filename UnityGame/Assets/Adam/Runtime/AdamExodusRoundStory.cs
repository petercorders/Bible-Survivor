using BibleSurvivor.Core;

namespace BibleSurvivor.Unity
{
    internal sealed class AdamExodusRoundStory
    {
        public readonly string Title, Reference, Summary;

        AdamExodusRoundStory(string title, string reference, string summary)
        {
            Title = title;
            Reference = reference;
            Summary = summary;
        }

        static readonly AdamExodusRoundStory[] Stories =
        {
            new AdamExodusRoundStory("벽돌 일터", "출 1:8–21",
                "새 왕은 이스라엘 백성에게 벽돌 노역을 시켰다. 산파 십브라와 부아는 왕의 명령을 따르지 않고 아이들을 살렸다."),
            new AdamExodusRoundStory("갈대 사이의 바구니", "출 2:1–10, 6:20",
                "요게벳은 아기를 바구니에 담아 나일강 갈대 사이에 두었다. 왕의 딸이 아기를 발견했고, 누이의 도움으로 어머니가 젖을 먹였다."),
            new AdamExodusRoundStory("바다 사이의 길", "출 14:21–29",
                "모세가 바다 위로 손을 내밀었다. 하나님이 바람으로 바다를 물러나게 하셨고, 백성은 마른 땅으로 건넜다."),
            new AdamExodusRoundStory("엘림의 샘", "출 15:20–27",
                "미리암은 바다를 건넌 뒤 사람들과 노래했다. 백성은 광야를 지나 샘과 종려나무가 있는 엘림의 물가에 진을 쳤다."),
            new AdamExodusRoundStory("시내산 기슭", "출 19:1–6, 28:1",
                "백성은 시내산 앞에 진을 쳤고 하나님은 언약을 맺으셨다. 하나님은 아론과 그 아들들을 제사장으로 부르셨다."),
            new AdamExodusRoundStory("브살렐의 성막", "출 31:1–11, 35:30–35, 40:1–8",
                "브살렐과 오홀리압은 성막을 만드는 일을 맡았다. 모세는 성막을 세우고 그 둘레에 뜰을 마련하라는 명령을 받았다.")
        };

        public static AdamExodusRoundStory Find(int round) =>
            round >= ExodusContent.GlobalRoundNumber && round <= ExodusContent.LastInstalledRound
                ? Stories[round - ExodusContent.GlobalRoundNumber] : null;
    }
}
