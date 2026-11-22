using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;

namespace BibleSurvivor.Core
{
    public sealed class LaterCampaignBook
    {
        public int Index { get; }
        public string Id { get; }
        public string Name { get; }
        public string MonsterPrefix { get; }
        public int FirstRound { get; }
        public int LastRound { get; }
        private readonly double steadySpeed, fastSpeed, heavySpeed;
        private readonly IReadOnlyDictionary<string,double> speeds;
        private readonly int contact;

        internal LaterCampaignBook(int index,string id,string name,string prefix,int first,int last,int contact,double steady,double fast,double heavy)
        {
            Index=index;Id=id;Name=name;MonsterPrefix=prefix;FirstRound=first;LastRound=last;this.contact=contact;
            steadySpeed=steady;fastSpeed=fast;heavySpeed=heavy;
            speeds=MakeSpeeds(prefix+"1");
        }

        private string SteadyMonster(int round)=>round switch
        {
            61=>"lament_ash",
            64=>"tangled_thicket",
            70=>"dry_land_ember",
            71=>"cave_hunter",
            76=>"starving_siege_beast",
            82=>"scattered_scroll_fragments",
            86=>"endless_procession",
            88=>"tower_pride_shadow",
            92=>"accusation_cloud",
            100=>"accusing_shadow",
            101=>"night_vision_whisper",
            103=>"windblown_chaff",
            104=>"valley_shadow",
            106=>"poisoned_word_arrows",
            107=>"prosperity_mirage",
            108=>"hidden_place_shadow",
            109=>"fowlers_snare",
            110=>"song_demand_mockery",
            112=>"poverty_like_a_robber",
            113=>"clamorous_folly",
            115=>"elusive_wind",
            116=>"ointment_flies",
            118=>"scorching_sunlight",
            119=>"little_vineyard_foxes",
            _=>MonsterPrefix+"1"
        };

        private IReadOnlyDictionary<string,double> MakeSpeeds(string steady)=>new ReadOnlyDictionary<string,double>(
            new Dictionary<string,double> {{steady,steadySpeed},{MonsterPrefix+"2",fastSpeed},{MonsterPrefix+"3",heavySpeed}});

        internal IReadOnlyDictionary<string,double> SpeedsFor(int round)
        {
            var steady=SteadyMonster(round);
            return steady==MonsterPrefix+"1"?speeds:MakeSpeeds(steady);
        }

        internal string SpeedContract(int round)=>string.Join(":",SpeedsFor(round)
            .Select(pair=>pair.Key+"-speed"+pair.Value.ToString("R",CultureInfo.InvariantCulture)))+":chase-contact";

        internal string RoundId(int number)=>Id+"_"+(number-FirstRound+1).ToString("D2",CultureInfo.InvariantCulture);
        internal RoundDefinition Round(int number)
        {
            var offset=number>100?78+(number-100)/2:(int?)null;
            var firstFastSpawnPower=offset%3==2?.85:1;
            return LaterBookRounds.Build(number,RoundId(number),SteadyMonster(number),MonsterPrefix+"2",MonsterPrefix+"3",
                contact,contact-1,contact+4,offset,firstFastSpawnPower);
        }
    }

    public static class LaterCampaignBooks
    {
        public const int FirstRound = 61;
        internal const double ProverbsSteadySpeed = .91 + .02 * 2;

        private static readonly LaterCampaignBook[] Books =
        {
            new LaterCampaignBook(9,"second_samuel","사무엘하","ss",61,66,21,.75,1.01,.43),
            new LaterCampaignBook(10,"first_kings","열왕기상","fk",67,72,22,.77,1.03,.44),
            new LaterCampaignBook(11,"second_kings","열왕기하","sk",73,81,23,.79,1.05,.45),
            new LaterCampaignBook(12,"first_chronicles","역대상","fc",82,84,24,.81,1.07,.46),
            new LaterCampaignBook(13,"second_chronicles","역대하","sc",85,90,25,.83,1.09,.47),
            new LaterCampaignBook(14,"ezra","에스라","ez",91,93,26,.85,1.11,.48),
            new LaterCampaignBook(15,"nehemiah","느헤미야","nh",94,96,27,.87,1.13,.49),
            new LaterCampaignBook(16,"esther","에스더","es",97,99,28,.89,1.15,.50),
            new LaterCampaignBook(17,"job","욥기","jb",100,102,29,.91,1.17,.51),
            new LaterCampaignBook(18,"psalms","시편","ps",103,111,30,.93,1.19,.52),
            new LaterCampaignBook(19,"proverbs","잠언","pv",112,114,31,ProverbsSteadySpeed,1.21,.53),
            new LaterCampaignBook(20,"ecclesiastes","전도서","ec",115,117,32,.97,1.23,.54),
            new LaterCampaignBook(21,"song_of_songs","아가","sg",118,120,33,.99,1.25,.55)
        };

        public static LaterCampaignBook? Find(int round)=>round>CampaignContent.LastInstalledRound?null:
            Books.FirstOrDefault(book=>round>=book.FirstRound&&round<=book.LastRound);

        internal static LaterCampaignBook Require(int round)=>Find(round)??throw new ArgumentOutOfRangeException(nameof(round));
        public static bool StartsBook(int round)=>Find(round)?.FirstRound==round;
    }
}
