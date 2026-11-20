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
            new LaterCampaignBook(10,"first_kings","열왕기상","fk",67,72,22,.77,1.03,.44)
        };

        public static LaterCampaignBook? Find(int round)=>round>CampaignContent.LastInstalledRound?null:
            Books.FirstOrDefault(book=>round>=book.FirstRound&&round<=book.LastRound);

        internal static LaterCampaignBook Require(int round)=>Find(round)??throw new ArgumentOutOfRangeException(nameof(round));
        public static bool StartsBook(int round)=>Find(round)?.FirstRound==round;
    }
}
