using System;
using System.Collections.Generic;
using System.Linq;

namespace BibleSurvivor.Core
{
    public sealed class CombatBonuses
    {
        public double DamageBonus {get;set;}
        public double CooldownReduction {get;set;}
        public double AreaBonus {get;set;}
        public double DurationBonus {get;set;}
        public double ProjectileSpeedBonus {get;set;}
        public int ExtraAmount {get;set;}
    }
    public sealed class CombatStats
    {
        public double Damage {get; private set;}
        public int Amount {get; private set;}
        public int Pierce {get; private set;}
        public double Duration {get; private set;}
        public double Cycle {get; private set;}
        public double BurstInterval {get; private set;}
        public double AreaMultiplier {get; private set;}
        public bool IsOrbit {get; private set;}
        public double ProjectileSpeedMultiplier {get; private set;}
        public static CombatStats For(string id,int level,CombatBonuses bonuses)
        {
            Guard.NonNegative(bonuses.DamageBonus,nameof(bonuses.DamageBonus));
            Guard.NonNegative(bonuses.CooldownReduction,nameof(bonuses.CooldownReduction));
            Guard.NonNegative(bonuses.AreaBonus,nameof(bonuses.AreaBonus));
            Guard.NonNegative(bonuses.DurationBonus,nameof(bonuses.DurationBonus));
            Guard.NonNegative(bonuses.ProjectileSpeedBonus,nameof(bonuses.ProjectileSpeedBonus));
            if(bonuses.ExtraAmount<0 || bonuses.ExtraAmount>1000) throw new ArgumentOutOfRangeException(nameof(bonuses.ExtraAmount));
            var baseStats=WeaponRules.Get(id,level);
            var duration=baseStats.Duration*(1+bonuses.DurationBonus);
            var cooldown=baseStats.Cooldown*Math.Max(.2,1-bonuses.CooldownReduction);
            var result=new CombatStats { Damage=baseStats.Damage*(1+bonuses.DamageBonus),Amount=baseStats.Amount+bonuses.ExtraAmount,
                Pierce=baseStats.Pierce,Duration=duration,Cycle=duration+cooldown,BurstInterval=baseStats.BurstInterval,
                AreaMultiplier=baseStats.AreaMultiplier+bonuses.AreaBonus,IsOrbit=id=="bible",ProjectileSpeedMultiplier=1+bonuses.ProjectileSpeedBonus };
            Guard.NonNegative(result.Damage,nameof(result.Damage)); Guard.NonNegative(result.Cycle,nameof(result.Cycle));
            Guard.NonNegative(result.AreaMultiplier,nameof(result.AreaMultiplier)); return result;
        }
    }
    public sealed class AttackEmission
    {
        public double At {get;}
        public int Ordinal {get;}
        public int Amount {get;}
        internal AttackEmission(double at,int ordinal,int amount) {At=at;Ordinal=ordinal;Amount=amount;}
    }
    public sealed class AttackClock
    {
        private readonly CombatStats stats;
        private double elapsed;
        private long cycle;
        private readonly List<AttackEmission> pending=new List<AttackEmission>();
        public bool Paused {get;set;}
        public AttackClock(CombatStats stats) {this.stats=stats;}
        public IReadOnlyList<AttackEmission> Advance(double seconds)
        {
            Guard.NonNegative(seconds,nameof(seconds));
            if(seconds>3600) throw new ArgumentOutOfRangeException(nameof(seconds),"Advance bounded simulation steps");
            var output=new List<AttackEmission>();
            if(Paused) return output.AsReadOnly();
            elapsed+=seconds;
            while(cycle*stats.Cycle<=elapsed+1e-8)
            {
                var count=stats.IsOrbit?1:stats.Amount;
                for(var i=0;i<count;i++) pending.Add(new AttackEmission(cycle*stats.Cycle+i*stats.BurstInterval,i,stats.IsOrbit?stats.Amount:1));
                cycle++;
            }
            output.AddRange(pending.Where(x=>x.At<=elapsed+1e-8).OrderBy(x=>x.At));
            pending.RemoveAll(x=>x.At<=elapsed+1e-8);
            return output.AsReadOnly();
        }
        public void Reset() {elapsed=0;cycle=0;pending.Clear();Paused=false;}
    }
    public sealed class HitLedger
    {
        private readonly double interval;
        private readonly int maxHits;
        private readonly Dictionary<int,double> last=new Dictionary<int,double>();
        private int hits;
        public HitLedger(double interval,int maxHits)
        {
            if(double.IsNaN(interval)||interval<0||maxHits<1) throw new ArgumentOutOfRangeException(nameof(interval));
            this.interval=interval;this.maxHits=maxHits;
        }
        public bool TryHit(int enemyId,double at)
        {
            Guard.NonNegative(at,nameof(at));
            if(enemyId<1) throw new ArgumentOutOfRangeException(nameof(enemyId));
            if(hits>=maxHits || (last.TryGetValue(enemyId,out var previous)&&at-previous<interval)) return false;
            last[enemyId]=at;hits++;return true;
        }
        public void Reset() {last.Clear();hits=0;}
    }
    public static class HitGeometry
    {
        public static bool InArc(double x,double y,double forwardX,double forwardY,double range,double degrees)
        {
            Guard.NonNegative(range,nameof(range)); Guard.NonNegative(degrees,nameof(degrees));
            if(degrees>360 || !Finite(x)||!Finite(y)||!Finite(forwardX)||!Finite(forwardY)) throw new ArgumentException("Invalid geometry");
            var distance=Math.Sqrt(x*x+y*y);var forward=Math.Sqrt(forwardX*forwardX+forwardY*forwardY);
            if(!Finite(distance)||!Finite(forward)) throw new ArgumentOutOfRangeException(nameof(x));
            if(distance>range || forward==0) return false;
            return distance==0 || (x*forwardX+y*forwardY)/(distance*forward)>=Math.Cos(degrees*.5*Math.PI/180)-1e-10;
        }
        private static bool Finite(double x)=>!double.IsNaN(x)&&!double.IsInfinity(x);
    }
}
