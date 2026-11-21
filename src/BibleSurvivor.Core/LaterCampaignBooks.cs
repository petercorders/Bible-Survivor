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
        internal RoundDefinition Round(int number)=>LaterBookRounds.Build(number,RoundId(number),
            SteadyMonster(number),MonsterPrefix+"2",MonsterPrefix+"3",contact,contact-1,contact+4);
    }

    public static class LaterCampaignBooks
    {
        public const int FirstRound = 61;

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
            new LaterCampaignBook(17,"job","욥기","jb",100,100,29,.91,1.17,.51)
        };

        public static LaterCampaignBook? Find(int round)=>round>CampaignContent.LastInstalledRound?null:
            Books.FirstOrDefault(book=>round>=book.FirstRound&&round<=book.LastRound);

        internal static LaterCampaignBook Require(int round)=>Find(round)??throw new ArgumentOutOfRangeException(nameof(round));
        public static bool StartsBook(int round)=>Find(round)?.FirstRound==round;
    }
}
