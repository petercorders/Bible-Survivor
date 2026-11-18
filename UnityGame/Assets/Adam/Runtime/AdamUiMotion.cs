using System;

namespace BibleSurvivor.Unity
{
    // Unscaled presentation time only. Never owns input gates, saves or a Core clock.
    public sealed class AdamUiMotion
    {
        public string Sequence {get;private set;}="";
        public double Elapsed {get;private set;}
        public void Advance(string sequence,double seconds)
        {
            if(Sequence!=sequence){Sequence=sequence;Elapsed=0;return;}
            Elapsed+=Math.Max(0,seconds);
        }
        public static double Quantized(double seconds)=>Math.Floor(Math.Max(0,seconds)*30+1e-8)/30;
        public static int EnterOffset(double seconds,bool effects,int slot=0)
        {
            if(!effects)return 0;
            var step=(int)Math.Floor(Math.Max(0,Quantized(seconds)-slot/30d)*30+1e-8);
            return step==0?8:step==1?4:step==2?2:0;
        }
        public static float Dim(double seconds,bool effects,bool death=false)=>!effects ? .55f : .55f*Math.Min(1,(int)(Quantized(seconds)/(death ? .1 : 1d/30)+1e-8)/4f);
        public static int BracketFrame(double seconds,bool effects)=>effects?(int)(Quantized(seconds)/.25)%2:0;
        public static int Gold(int applied,double seconds,bool effects)
        {
            if(!effects)return applied;
            if(Quantized(seconds)<4d/30)return 0;
            var steps=Math.Max(0,Math.Min(8,(int)Math.Floor((Quantized(seconds)-4d/30)/.05)+1));
            return (int)Math.Floor(applied*steps/8d);
        }
        public static float CompletionRule(double seconds,bool effects)=>!effects?1:Math.Min(1,(int)(Quantized(seconds)*30+1e-8)/8f);
        public static bool CautionBright(double seconds,bool effects)=>!effects||Quantized(seconds)<.6||Quantized(seconds)>=.8;
    }
}
