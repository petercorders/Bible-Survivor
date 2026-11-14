using System.Numerics;

namespace BibleSurvivor.Core
{
    // Presentation-only fact at the existing collision decision. No serializer attributes or callbacks.
    public readonly struct BattleImpact
    {
        public long Sequence {get;}
        public long Step {get;}
        public double Time {get;}
        public string Cause {get;}
        // Zero denotes player contact damage; positive IDs are enemies.
        public int TargetId {get;}
        public Vector2 Position {get;}
        public double Damage {get;}
        public bool Killed {get;}
        internal BattleImpact(long sequence,long step,double time,string cause,int targetId,Vector2 position,double damage,bool killed)
        {Sequence=sequence;Step=step;Time=time;Cause=cause;TargetId=targetId;Position=position;Damage=damage;Killed=killed;}
    }
}
