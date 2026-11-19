using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;

namespace BibleSurvivor.Core
{
    public static class CampaignContent
    {
        public const int LastInstalledRound = 30;

        public static RoundDefinition Round(int number) => number < NumbersContent.GlobalRoundNumber
            ? LeviticusContent.Round(number) : NumbersContent.Round(number);

        public static IReadOnlyDictionary<string,double> SpeedsFor(int number) => number < NumbersContent.GlobalRoundNumber
            ? LeviticusContent.SpeedsFor(number) : NumbersContent.SpeedsFor(number);

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
        internal const string SpeedContract = "nm1-speed0.63:nm2-speed0.89:nm3-speed0.37:chase-contact";

        private static readonly IReadOnlyDictionary<string,double> Speeds =
            new ReadOnlyDictionary<string,double>(new Dictionary<string,double>
            {
                {"nm1",.63}, {"nm2",.89}, {"nm3",.37}
            });

        private static int Offset(int number)
        {
            if(number<GlobalRoundNumber||number>CampaignContent.LastInstalledRound)
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
}
