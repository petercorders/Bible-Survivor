using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;

namespace BibleSurvivor.Core
{
    public static class CampaignContent
    {
        public const int LastInstalledRound = 140;

        public static RoundDefinition Round(int number) => number switch
        {
            < NumbersContent.GlobalRoundNumber => LeviticusContent.Round(number),
            < DeuteronomyContent.GlobalRoundNumber => NumbersContent.Round(number),
            < JoshuaContent.GlobalRoundNumber => DeuteronomyContent.Round(number),
            < JudgesContent.GlobalRoundNumber => JoshuaContent.Round(number),
            < RuthContent.GlobalRoundNumber => JudgesContent.Round(number),
            < FirstSamuelContent.GlobalRoundNumber => RuthContent.Round(number),
            < LaterCampaignBooks.FirstRound => FirstSamuelContent.Round(number),
            _ => LaterCampaignBooks.Require(number).Round(number)
        };

        public static IReadOnlyDictionary<string,double> SpeedsFor(int number) => number switch
        {
            < NumbersContent.GlobalRoundNumber => LeviticusContent.SpeedsFor(number),
            < DeuteronomyContent.GlobalRoundNumber => NumbersContent.SpeedsFor(number),
            < JoshuaContent.GlobalRoundNumber => DeuteronomyContent.SpeedsFor(number),
            < JudgesContent.GlobalRoundNumber => JoshuaContent.SpeedsFor(number),
            < RuthContent.GlobalRoundNumber => JudgesContent.SpeedsFor(number),
            < FirstSamuelContent.GlobalRoundNumber => RuthContent.SpeedsFor(number),
            < LaterCampaignBooks.FirstRound => FirstSamuelContent.SpeedsFor(number),
            _ => LaterCampaignBooks.Require(number).SpeedsFor(number)
        };

        // Fingerprint inputs for R22 onward; Numbers keeps its original contract text.
        internal static string SpeedContract(int number) => number switch
        {
            < NumbersContent.GlobalRoundNumber => throw new ArgumentOutOfRangeException(nameof(number)),
            < DeuteronomyContent.GlobalRoundNumber => NumbersContent.SpeedContract,
            < JoshuaContent.GlobalRoundNumber => DeuteronomyContent.SpeedContract,
            < JudgesContent.GlobalRoundNumber => JoshuaContent.SpeedContract,
            < RuthContent.GlobalRoundNumber => JudgesContent.SpeedContract,
            < FirstSamuelContent.GlobalRoundNumber => RuthContent.SpeedContract,
            < LaterCampaignBooks.FirstRound => FirstSamuelContent.SpeedContract,
            _ => LaterCampaignBooks.Require(number).SpeedContract(number)
        };

        internal static string RoundId(int number) => number switch
        {
            < NumbersContent.GlobalRoundNumber => throw new ArgumentOutOfRangeException(nameof(number)),
            < DeuteronomyContent.GlobalRoundNumber => NumbersContent.RoundId(number),
            < JoshuaContent.GlobalRoundNumber => DeuteronomyContent.RoundId(number),
            < JudgesContent.GlobalRoundNumber => JoshuaContent.RoundId(number),
            < RuthContent.GlobalRoundNumber => JudgesContent.RoundId(number),
            < FirstSamuelContent.GlobalRoundNumber => RuthContent.RoundId(number),
            < LaterCampaignBooks.FirstRound => FirstSamuelContent.RoundId(number),
            _ => LaterCampaignBooks.Require(number).RoundId(number)
        };

        internal static void Validate(RoundDefinition definition)
        {
            if(definition.Number<LeviticusContent.GlobalRoundNumber||definition.Number>LastInstalledRound||
                definition.Fingerprint!=Round(definition.Number).Fingerprint)
                throw new ArgumentException("Unsupported campaign round definition",nameof(definition));
        }
    }

    public static class NumbersContent
    {
        public const int GlobalRoundNumber = 22;
        public const int LastRound = 30;
        internal const string SpeedContract = "nm1-speed0.63:nm2-speed0.89:nm3-speed0.37:chase-contact";

        private static readonly IReadOnlyDictionary<string,double> Speeds =
            new ReadOnlyDictionary<string,double>(new Dictionary<string,double>
            {
                {"nm1",.63}, {"nm2",.89}, {"nm3",.37}
            });

        private static int Offset(int number)
        {
            if(number<GlobalRoundNumber||number>LastRound)
                throw new ArgumentOutOfRangeException(nameof(number));
            return number-GlobalRoundNumber;
        }

        public static IReadOnlyDictionary<string,double> SpeedsFor(int number)
        {
            _=Offset(number);
            return Speeds;
        }

        internal static string RoundId(int number) => "numbers_"+(Offset(number)+1).ToString("D2",CultureInfo.InvariantCulture);

        public static RoundDefinition Round(int number)
        {
            var offset=Offset(number);
            var cohort=offset%3;
            var steadyHealth=440+20*offset;
            var fastHealth=295+14*offset;
            var heavyHealth=1070+35*offset;
            // Alternate the proportions of the fast and heavy groups through each three-round section.
            return new RoundDefinition(number,120,7200+200*offset,420+20*offset,new[]
            {
                new WaveDefinition(0,60,new[]{new SpawnGroup("nm1",300,steadyHealth,15),new SpawnGroup("nm2",120+18*cohort,fastHealth,14)}),
                new WaveDefinition(60,120,new[]{new SpawnGroup("nm1",320+12*offset,steadyHealth,15),new SpawnGroup("nm2",210-18*cohort,fastHealth,14)}),
                new WaveDefinition(120,170,new[]{new SpawnGroup("nm1",330+15*offset,steadyHealth,15),new SpawnGroup("nm2",160+12*offset,fastHealth,14),
                    new SpawnGroup("nm3",120+21*offset,heavyHealth,19)})
            },RoundId(number));
        }
    }

    // Later books continue the Numbers formula with the campaign-wide offset from R22. Initial values, not measured balance.
    internal static class LaterBookRounds
    {
        internal static RoundDefinition Build(int number,string id,string steady,string fast,string heavy,int steadyContact,int fastContact,int heavyContact,int? tuningOffset=null,double firstFastSpawnPower=1)
        {
            var offset=tuningOffset??number-NumbersContent.GlobalRoundNumber;
            var cohort=offset%3;
            var steadyHealth=440+20*offset;
            var fastHealth=295+14*offset;
            var heavyHealth=1070+35*offset;
            return new RoundDefinition(number,120,7200+200*offset,420+20*offset,new[]
            {
                new WaveDefinition(0,60,new[]{new SpawnGroup(steady,300,steadyHealth,steadyContact),new SpawnGroup(fast,120+18*cohort,fastHealth,fastContact,firstFastSpawnPower)}),
                new WaveDefinition(60,120,new[]{new SpawnGroup(steady,320+12*offset,steadyHealth,steadyContact),new SpawnGroup(fast,210-18*cohort,fastHealth,fastContact)}),
                new WaveDefinition(120,170,new[]{new SpawnGroup(steady,330+15*offset,steadyHealth,steadyContact),new SpawnGroup(fast,160+12*offset,fastHealth,fastContact),
                    new SpawnGroup(heavy,120+21*offset,heavyHealth,heavyContact)})
            },id);
        }

        internal static int Local(int number,int first,int last)
        {
            if(number<first||number>last)throw new ArgumentOutOfRangeException(nameof(number));
            return number-first+1;
        }
    }

    public static class DeuteronomyContent
    {
        public const int GlobalRoundNumber = 31;
        public const int LastRound = 36;
        internal const string SpeedContract = "dt1-speed0.65:dt2-speed0.91:dt3-speed0.38:chase-contact";

        private static readonly IReadOnlyDictionary<string,double> Speeds =
            new ReadOnlyDictionary<string,double>(new Dictionary<string,double>
            {
                {"dt1",.65}, {"dt2",.91}, {"dt3",.38}
            });

        public static IReadOnlyDictionary<string,double> SpeedsFor(int number)
        {
            _=LaterBookRounds.Local(number,GlobalRoundNumber,LastRound);
            return Speeds;
        }

        internal static string RoundId(int number) => "deuteronomy_"+LaterBookRounds.Local(number,GlobalRoundNumber,LastRound).ToString("D2",CultureInfo.InvariantCulture);

        public static RoundDefinition Round(int number) => LaterBookRounds.Build(number,RoundId(number),"dt1","dt2","dt3",16,15,20);
    }

    public static class JoshuaContent
    {
        public const int GlobalRoundNumber = 37;
        public const int LastRound = 42;
        internal const string SpeedContract = "js1-speed0.67:js2-speed0.93:js3-speed0.39:chase-contact";

        private static readonly IReadOnlyDictionary<string,double> Speeds =
            new ReadOnlyDictionary<string,double>(new Dictionary<string,double>
            {
                {"js1",.67}, {"js2",.93}, {"js3",.39}
            });

        public static IReadOnlyDictionary<string,double> SpeedsFor(int number)
        {
            _=LaterBookRounds.Local(number,GlobalRoundNumber,LastRound);
            return Speeds;
        }

        internal static string RoundId(int number) => "joshua_"+LaterBookRounds.Local(number,GlobalRoundNumber,LastRound).ToString("D2",CultureInfo.InvariantCulture);

        public static RoundDefinition Round(int number) => LaterBookRounds.Build(number,RoundId(number),"js1","js2","js3",17,16,21);
    }

    public static class JudgesContent
    {
        public const int GlobalRoundNumber = 43;
        public const int LastRound = 51;
        internal const string SpeedContract = "jd1-speed0.69:jd2-speed0.95:jd3-speed0.40:chase-contact";

        private static readonly IReadOnlyDictionary<string,double> Speeds =
            new ReadOnlyDictionary<string,double>(new Dictionary<string,double>
            {
                {"jd1",.69}, {"jd2",.95}, {"jd3",.40}
            });

        public static IReadOnlyDictionary<string,double> SpeedsFor(int number)
        {
            _=LaterBookRounds.Local(number,GlobalRoundNumber,LastRound);
            return Speeds;
        }

        internal static string RoundId(int number) => "judges_"+LaterBookRounds.Local(number,GlobalRoundNumber,LastRound).ToString("D2",CultureInfo.InvariantCulture);

        public static RoundDefinition Round(int number) => LaterBookRounds.Build(number,RoundId(number),"jd1","jd2","jd3",18,17,22);
    }

    public static class RuthContent
    {
        public const int GlobalRoundNumber = 52;
        public const int LastRound = 54;
        internal const string SpeedContract = "rt1-speed0.71:rt2-speed0.97:rt3-speed0.41:chase-contact";

        private static readonly IReadOnlyDictionary<string,double> Speeds =
            new ReadOnlyDictionary<string,double>(new Dictionary<string,double>
            {
                {"rt1",.71}, {"rt2",.97}, {"rt3",.41}
            });

        public static IReadOnlyDictionary<string,double> SpeedsFor(int number)
        {
            _=LaterBookRounds.Local(number,GlobalRoundNumber,LastRound);
            return Speeds;
        }

        internal static string RoundId(int number) => "ruth_"+LaterBookRounds.Local(number,GlobalRoundNumber,LastRound).ToString("D2",CultureInfo.InvariantCulture);

        public static RoundDefinition Round(int number) => LaterBookRounds.Build(number,RoundId(number),"rt1","rt2","rt3",19,18,23);
    }

    // First Samuel ends at R60, which is also the current campaign endpoint.
    public static class FirstSamuelContent
    {
        public const int GlobalRoundNumber = 55;
        public const int LastRound = 60;
        internal const string SpeedContract = "sm1-speed0.73:sm2-speed0.99:sm3-speed0.42:chase-contact";

        private static readonly IReadOnlyDictionary<string,double> Speeds =
            new ReadOnlyDictionary<string,double>(new Dictionary<string,double>
            {
                {"sm1",.73}, {"sm2",.99}, {"sm3",.42}
            });

        public static IReadOnlyDictionary<string,double> SpeedsFor(int number)
        {
            _=LaterBookRounds.Local(number,GlobalRoundNumber,LastRound);
            return Speeds;
        }

        internal static string RoundId(int number) => "first_samuel_"+LaterBookRounds.Local(number,GlobalRoundNumber,LastRound).ToString("D2",CultureInfo.InvariantCulture);

        public static RoundDefinition Round(int number) => LaterBookRounds.Build(number,RoundId(number),"sm1","sm2","sm3",20,19,24);
    }
}
