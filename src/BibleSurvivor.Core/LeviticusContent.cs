using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace BibleSurvivor.Core
{
    public static class LeviticusContent
    {
        public const string ChapterId = "leviticus";
        public const int GlobalRoundNumber = 16;
        public const int LastInstalledRound = 20;
        public const string RoundId = "leviticus_01";
        internal const string SpeedContract = "lv1-speed0.60:lv2-speed0.85:lv3-speed0.35:chase-contact";

        public static IReadOnlyDictionary<string,double> MonsterSpeeds { get; } =
            new ReadOnlyDictionary<string,double>(new Dictionary<string,double>
            {
                {"lv1",.60}, {"lv2",.85}, {"lv3",.35}
            });

        public static RoundDefinition Round() => new RoundDefinition(GlobalRoundNumber,120,6000,300,new[]
        {
            new WaveDefinition(0,60,new[]{new SpawnGroup("lv1",260,300,13),new SpawnGroup("lv2",80,210,12)}),
            new WaveDefinition(60,120,new[]{new SpawnGroup("lv1",280,300,13),new SpawnGroup("lv2",140,210,12)}),
            new WaveDefinition(120,170,new[]{new SpawnGroup("lv1",260,300,13),new SpawnGroup("lv2",144,210,12),new SpawnGroup("lv3",96,900,17)})
        },RoundId);

        public static IReadOnlyDictionary<string,double> SpeedsFor(int number)
        {
            if(number<GlobalRoundNumber||number>LastInstalledRound)
                throw new ArgumentOutOfRangeException(nameof(number));
            return MonsterSpeeds;
        }

        public static RoundDefinition Round(int number) => number switch
        {
            16=>Round(),
            17=>LaterRound(17,"leviticus_02",6200,320,1320,325,225,920),
            18=>LaterRound(18,"leviticus_03",6400,340,1380,350,240,950),
            19=>LaterRound(19,"leviticus_04",6600,360,1440,375,255,980),
            20=>LaterRound(20,"leviticus_05",6800,380,1500,400,270,1010),
            _=>throw new ArgumentOutOfRangeException(nameof(number))
        };

        private static RoundDefinition LaterRound(int number,string id,int xp,int gold,int total,int hp1,int hp2,int hp3)
        {
            var increase=total-1260;
            return new RoundDefinition(number,120,xp,gold,new[]
            {
                new WaveDefinition(0,60,new[]{new SpawnGroup("lv1",260, hp1,14),new SpawnGroup("lv2",80+increase/3,hp2,13)}),
                new WaveDefinition(60,120,new[]{new SpawnGroup("lv1",280,hp1,14),new SpawnGroup("lv2",140+increase/3,hp2,13)}),
                new WaveDefinition(120,170,new[]{new SpawnGroup("lv1",260,hp1,14),new SpawnGroup("lv2",144,hp2,13),new SpawnGroup("lv3",96+increase/3,hp3,18)})
            },id);
        }

        internal static void Validate(RoundDefinition definition)
        {
            if (definition.Number<GlobalRoundNumber || definition.Number>LastInstalledRound ||
                definition.Fingerprint!=Round(definition.Number).Fingerprint)
                throw new ArgumentException("Unsupported Leviticus round definition",nameof(definition));
        }
    }
}
