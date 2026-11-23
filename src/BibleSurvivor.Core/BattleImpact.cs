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
        public string? WeaponForm {get;}
        // Zero denotes player contact damage; positive IDs are enemies.
        public int TargetId {get;}
        public Vector2 Position {get;}
        public double Damage {get;}
        public bool Killed {get;}
        internal BattleImpact(long sequence,long step,double time,string cause,int targetId,Vector2 position,double damage,bool killed,string? weaponForm=null)
        {Sequence=sequence;Step=step;Time=time;Cause=cause;TargetId=targetId;Position=position;Damage=damage;Killed=killed;WeaponForm=weaponForm;}
    }

    public enum BattleAreaEffectKind { LightningStrike, LightningAftershock, LightningEliteThunder, CrossRecovery, CrossIntersection, CrossReturnSpin, ChariotEndExplosion, ChariotCrossingWhirlwind, ChariotEndFlame, LightningChain, StaffHit, CrossHit, LionClaw }

    // Holds presentation coordinates from the actual effect emission, kept only in live world memory.
    public readonly struct BattleAreaEffect
    {
        public BattleAreaEffectKind Kind { get; }
        public Vector2 Center { get; }
        public double Radius { get; }
        public double At { get; }

        internal BattleAreaEffect(BattleAreaEffectKind kind, Vector2 center, double radius, double at)
        {
            Kind = kind;
            Center = center;
            Radius = radius;
            At = at;
        }
    }
}
