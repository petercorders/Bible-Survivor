using System;

namespace BibleSurvivor.Unity
{
    // Only explicit local transitions queue a ribbon. Loading a session never queues one.
    public sealed class AdamSortieRibbon
    {
        string pending,delivered,active;
        public double Elapsed {get;private set;}
        public void Reset(){pending=delivered=active=null;Elapsed=0;}
        public static string Identity(string runId,int round)=>runId+":"+round;
        public void Queue(string identity){if(identity!=delivered)pending=identity;}
        public void Advance(string identity,bool canPlay,bool blocked,bool boss,double seconds,bool effects=true)
        {
            if(pending!=null&&pending!=identity)pending=null;
            if(active!=null&&(active!=identity||blocked||boss))active=null;
            if(pending==identity&&canPlay&&!blocked&&!boss)
            {active=identity;delivered=identity;pending=null;Elapsed=0;return;}
            if(active!=null)Elapsed+=Math.Max(0,seconds);
            if(Elapsed>=(effects?1.6:.8))active=null;
        }
        public bool Visible(string identity,bool effects)=>active!=null&&active==identity&&Elapsed<(effects?1.6:.8);
        public static float Extent(double seconds,bool effects)
        {
            if(!effects)return seconds<.8?1:0;
            var t=AdamUiMotion.Quantized(seconds);
            return t<.2?(float)(t/.2):t<1.4?1:t<1.6?(float)((1.6-t)/.2):0;
        }
    }
}
