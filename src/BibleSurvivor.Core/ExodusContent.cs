using System;
using System.Collections.Generic;

namespace BibleSurvivor.Core
{
    // Installed Exodus rounds; the parameterless APIs preserve the R10 contract.
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
        public const int LastInstalledRound = 11;
        public const string RoundElevenSpeedContract = "ex4-speed0.60:ex5-speed0.75:ex6-speed0.90:chase-contact:no-status-effects";
        public static IReadOnlyDictionary<string,double> SpeedsFor(int number)
        {
            if(number==10)return MonsterSpeeds;
            if(number!=11)throw new ArgumentOutOfRangeException(nameof(number));
            return new System.Collections.ObjectModel.ReadOnlyDictionary<string,double>(new Dictionary<string,double>{{"ex4",.60},{"ex5",.75},{"ex6",.90}});
        }
        public static RoundDefinition Round(int number)
        {
            if(number==10)return Round();
            if(number!=11)throw new ArgumentOutOfRangeException(nameof(number));
            return new RoundDefinition(11,110,4000,200,new[]{
                new WaveDefinition(0,60,new[]{new SpawnGroup("ex4",192,180,10),new SpawnGroup("ex5",48,140,10)}),
                new WaveDefinition(60,120,new[]{new SpawnGroup("ex4",192,180,10),new SpawnGroup("ex5",128,140,10)}),
                new WaveDefinition(120,170,new[]{new SpawnGroup("ex4",200,180,10),new SpawnGroup("ex5",120,140,10),new SpawnGroup("ex6",80,240,12)})
            },"exodus_02");
        }
        public static void Validate(RoundDefinition definition)
        {
            if(definition==null||(definition.Number!=10&&definition.Number!=11)||definition.Fingerprint!=Round(definition.Number).Fingerprint)
                throw new ArgumentException("Unsupported Exodus round catalog");
        }
    }
}
