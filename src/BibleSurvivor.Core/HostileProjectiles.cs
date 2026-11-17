using System;
using System.IO;
using System.Runtime.Serialization;
namespace BibleSurvivor.Core
{
    [DataContract]
    public sealed class ChainDirectionState
    {
        [DataMember] public float X {get;set;}
        [DataMember] public float Y {get;set;}
        internal ChainDirectionState Copy()=>(ChainDirectionState)MemberwiseClone();
        internal void Validate()
        {if(!BattleSnapshot.Position(X,Y)||Math.Abs((double)X*X+(double)Y*Y-1)>.0001)throw new InvalidDataException("Invalid chain direction");}
    }
    [DataContract]
    public sealed class HostileProjectileState
    {
        [DataMember] public int Version {get;set;}=1;
        [DataMember] public long Id {get;set;}
        [DataMember] public double BornAt {get;set;}
        [DataMember] public float StartX {get;set;}
        [DataMember] public float StartY {get;set;}
        [DataMember] public float X {get;set;}
        [DataMember] public float Y {get;set;}
        [DataMember] public float DirectionX {get;set;}
        [DataMember] public float DirectionY {get;set;}
        [DataMember] public double Lifetime {get;set;}
        internal HostileProjectileState Copy()=>(HostileProjectileState)MemberwiseClone();
        internal void Validate(double time,long next)
        {
            if(Version>1)throw new UnsupportedSaveVersionException();
            var age=time-BornAt;
            if(Version!=1||Id<1||Id>=next||!BattleSnapshot.Finite(BornAt)||BornAt<0||age< -1e-7||!BattleSnapshot.Finite(Lifetime)||Lifetime<=0||Lifetime>BossDefinition.ChainLifetime+1e-7||
                Math.Abs(Lifetime+age-BossDefinition.ChainLifetime)>1e-6||!BattleSnapshot.Position(StartX,StartY)||!BattleSnapshot.Position(X,Y)||!BattleSnapshot.Position(DirectionX,DirectionY)||
                Math.Abs((double)DirectionX*DirectionX+(double)DirectionY*DirectionY-1)>.0001||
                Math.Abs(X-(float)(StartX+DirectionX*BossDefinition.ChainSpeed*age))>.0001||Math.Abs(Y-(float)(StartY+DirectionY*BossDefinition.ChainSpeed*age))>.0001)
                throw new InvalidDataException("Invalid hostile projectile");
        }
    }
}
