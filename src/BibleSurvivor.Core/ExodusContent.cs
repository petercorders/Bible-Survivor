using System;
using System.Collections.Generic;

namespace BibleSurvivor.Core
{
    // Installed content only; global R10 is Exodus local round 1.
    public static class ExodusContent
    {
        public const string ChapterId = "exodus";
        public const int LocalRoundNumber = 1;
        public const int GlobalRoundNumber = 10;
        public const string RoundId = "exodus_01";
        public const string SpeedContract = "ex1-speed0.45:ex2-speed0.70:ex3-speed0.35:chase-contact:no-status-effects";
        public static IReadOnlyDictionary<string,double> MonsterSpeeds => new System.Collections.ObjectModel.ReadOnlyDictionary<string,double>(
            new Dictionary<string,double>{{"ex1",.45},{"ex2",.70},{"ex3",.35}});
        public static RoundDefinition Round() => new RoundDefinition(10,110,3600,180,new[]{
            new WaveDefinition(0,60,new[]{new SpawnGroup("ex1",180,160,10),new SpawnGroup("ex2",60,100,9)}),
            new WaveDefinition(60,120,new[]{new SpawnGroup("ex1",180,160,10),new SpawnGroup("ex2",120,100,9)}),
            new WaveDefinition(120,170,new[]{new SpawnGroup("ex1",180,160,10),new SpawnGroup("ex2",126,100,9),new SpawnGroup("ex3",54,450,13)})
        },RoundId);
        public static void Validate(RoundDefinition definition)
        {
            if(definition==null||definition.Number!=GlobalRoundNumber||definition.Id!=RoundId||definition.Fingerprint!=Round().Fingerprint)
                throw new ArgumentException("Unsupported Exodus round catalog");
        }
    }
}
