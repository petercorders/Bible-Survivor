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
            122=>"smoldering_firebrand_stump",
            124=>"unending_night",
            125=>"veil_over_nations",
            127=>"false_refuge",
            128=>"wooden_idol",
            130=>"fear_of_faces",
            133=>"yoke_of_false_peace",
            134=>"scroll_burning_brazier",
            136=>"egypt_road_mirage",
            137=>"surging_nile_host",
            139=>"tears_without_comfort",
            140=>"gnashing_mockery",
            142=>"briers_and_scorpions",
            143=>"carved_creeping_things",
            145=>"broken_breach",
            146=>"sinking_proud_ship",
            148=>"beast_of_scattered_sheep",
            149=>"dry_wind_without_hope",
            151=>"royal_delicacies",
            152=>"iron_and_clay_feet",
            154=>"shadow_of_the_vessel_feast",
            155=>"hungry_lion_pride",
            157=>"little_horn",
            158=>"shadow_of_desolation",
            160=>"baal_adornments",
            161=>"reaped_whirlwind",
            163=>"dust_of_devastation",
            164=>"ivory_bed_complacency",
            166=>"entangling_seaweed",
            167=>"violence_of_hands",
            169=>"evil_plotted_at_night",
            170=>"weight_of_sin",
            172=>"evening_wolves",
            173=>"crying_stones",
            175=>"settled_indifference",
            176=>"punctured_purse",
            178=>"evil_in_the_ephah",
            179=>"departing_unclean_spirit",
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
            var minorProphets=number>=160;
            var firstFastSpawnPower=minorProphets?.65:offset%3==2?.85:1;
            var contactLimit=number>=121?33:int.MaxValue;
            return LaterBookRounds.Build(number,RoundId(number),SteadyMonster(number),MonsterPrefix+"2",MonsterPrefix+"3",
                Math.Min(contact,contactLimit),Math.Min(contact-1,contactLimit),Math.Min(contact+4,contactLimit),
                offset,firstFastSpawnPower,minorProphets?80:100);
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
            new LaterCampaignBook(21,"song_of_songs","아가","sg",118,120,33,.99,1.25,.55),
            new LaterCampaignBook(22,"isaiah","이사야","isa",121,129,34,1.01,1.27,.56),
            new LaterCampaignBook(23,"jeremiah","예레미야","jer",130,138,35,1.03,1.29,.51+.01*6),
            new LaterCampaignBook(24,"lamentations","예레미야애가","lam",139,141,36,1.05,1.31,.51+.01*7),
            new LaterCampaignBook(25,"ezekiel","에스겔","eze",142,150,37,1.07,1.17+.02*8,.59),
            new LaterCampaignBook(26,"daniel","다니엘","dan",151,159,38,1.09,1.17+.02*9,.60),
            new LaterCampaignBook(27,"minor_prophets","열두 예언서","mp",160,180,39,1.11,1.17+.02*10,.61)
        };

        public static LaterCampaignBook? Find(int round)=>round>CampaignContent.LastInstalledRound?null:
            Books.FirstOrDefault(book=>round>=book.FirstRound&&round<=book.LastRound);

        internal static LaterCampaignBook Require(int round)=>Find(round)??throw new ArgumentOutOfRangeException(nameof(round));
        public static bool StartsBook(int round)=>Find(round)?.FirstRound==round;
    }
}
