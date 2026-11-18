using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace BibleSurvivor.Core
{
    public static class LeviticusContent
    {
        public const string ChapterId = "leviticus";
        public const int GlobalRoundNumber = 16;
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

        internal static void Validate(RoundDefinition definition)
        {
            if (definition.Number!=GlobalRoundNumber || definition.Fingerprint!=Round().Fingerprint)
                throw new ArgumentException("Unsupported Leviticus round definition",nameof(definition));
        }
    }
}
