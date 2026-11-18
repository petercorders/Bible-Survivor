using System;
using UnityEngine;

namespace BibleSurvivor.Unity
{
    public sealed class AdamStartStage
    {
        public int Focus {get;private set;}
        Vector2 pointer;
        bool pointerKnown;
        public void Reset(){Focus=0;pointerKnown=false;}
        public void Move(int direction)=>Focus=(Focus+(direction<0?2:direction>0?1:0))%3;
        public void Hover(Vector2 position,float width,float height)
        {
            if(pointerKnown&&pointer==position)return;
            pointerKnown=true;pointer=position;
            for(var i=0;i<3;i++)if(Card(width,height,i).Contains(position)){Focus=i;return;}
        }
        public static Rect Card(float width,float height,int slot)
        {
            if(slot<0||slot>2)throw new ArgumentOutOfRangeException(nameof(slot));
            var wide=width>=880;var w=wide?240:192;var h=wide?250:190;var gap=wide?20:12;
            var arena=AdamGame.Arena(width,height);
            var y=Mathf.Round((arena.y+(arena.height-h)/2)/2)*2;
            return new Rect((width-3*w-2*gap)/2+slot*(w+gap),y,w,h);
        }
        public static string Emblem(int slot)=>slot==0?"ico_wpn_bible_emblem_32":slot==1?"ico_wpn_jawbone_emblem_32":slot==2?"ico_wpn_sling_emblem_32":throw new ArgumentOutOfRangeException(nameof(slot));
        public static Rect Visual(Rect hit,bool focused,double elapsed,bool effects,int slot)
        {hit.y+=AdamUiMotion.EnterOffset(elapsed,effects,slot)-(focused?4:0);return hit;}
    }
}
