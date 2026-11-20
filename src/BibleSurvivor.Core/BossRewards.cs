using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Security.Cryptography;
using System.Text;

namespace BibleSurvivor.Core
{
    [DataContract]
    public sealed class BossRewardCandidate
    {
        [DataMember] public string Id {get;set;}="";
        [DataMember] public string Kind {get;set;}="";
        [DataMember] public string ItemId {get;set;}="";
        [DataMember] public int Level {get;set;}
        [DataMember] public int Gold {get;set;}
        internal BossRewardCandidate Copy()=>(BossRewardCandidate)MemberwiseClone();
        internal bool Same(BossRewardCandidate x)=>Id==x.Id&&Kind==x.Kind&&ItemId==x.ItemId&&Level==x.Level&&Gold==x.Gold;
    }
    internal static class BossRewards
    {
        internal static readonly string[] Passives={"scroll","hair","stones","east_wind","seal","cloud","thunder","crown","underwear","whirlwind"};
        internal static List<BossRewardCandidate> Generate(BossRewardState reward)
        {
            var inventory=EquipmentInventory.Restore(reward.Weapons,reward.Passives,reward.Forms);
            using var hash=SHA256.Create();var bytes=hash.ComputeHash(Encoding.UTF8.GetBytes(reward.RewardId));
            var seed=unchecked((int)((uint)bytes[0]|(uint)bytes[1]<<8|(uint)bytes[2]<<16|(uint)bytes[3]<<24));
            var result=new List<BossRewardCandidate>();
            var evolutions=EvolutionRules.Eligibility(reward.Weapons,reward.Passives,reward.Forms);
            if(evolutions.Count>0)
            {
                var offset=(int)((uint)seed%(uint)evolutions.Count);
                for(var i=0;i<evolutions.Count&&result.Count<3;i++)
                {
                    var form=evolutions[(offset+i)%evolutions.Count];
                    result.Add(new BossRewardCandidate{Id=reward.RewardId+":evolution:"+form,Kind="evolution",ItemId=form,Level=8});
                }
            }
            foreach(var id in inventory.Choices(reward.UnlockedWeapons!,Passives,3,seed).Take(3-result.Count))
            {
                var passive=Passives.Contains(id);var items=passive?reward.Passives:reward.Weapons;
                result.Add(new BossRewardCandidate{Id=reward.RewardId+":item:"+id,Kind="item",ItemId=id,Level=items.TryGetValue(id,out var level)?level+1:1});
            }
            if(result.Count<3)result.Add(new BossRewardCandidate{Id=reward.RewardId+":gold",Kind="gold",Gold=100});
            return result;
        }
        internal static bool Same<T>(IReadOnlyDictionary<string,T>? a,IReadOnlyDictionary<string,T>? b)
            =>(a??new Dictionary<string,T>()).OrderBy(x=>x.Key,StringComparer.Ordinal).SequenceEqual((b??new Dictionary<string,T>()).OrderBy(x=>x.Key,StringComparer.Ordinal));
        internal static void Validate(BossRewardState reward,RunState run,bool completed)
        {
            if(reward.PolicyId==BossDefinition.LegacyPolicy)
            {
                if(reward.Applied||reward.Candidates!=null||reward.SelectedId!=null||reward.PromotedFrom!=null||reward.GoldApplied||completed||
                    !Same(reward.Weapons,run.Weapons)||!Same(reward.Passives,run.Passives)||!Same(reward.Forms,run.Forms))throw new InvalidDataException("Invalid legacy reward boundary");
                return;
            }
            if(!BossDefinition.IsProductionPolicy(reward.PolicyId))throw new UnsupportedSaveVersionException();
            if(reward.UnlockedWeapons==null||reward.UnlockedWeapons.Any(string.IsNullOrWhiteSpace)||reward.UnlockedWeapons.Distinct().Count()!=reward.UnlockedWeapons.Count||
                reward.Candidates==null||reward.Candidates.Count<1||reward.Candidates.Count>3||reward.Candidates.Any(x=>x==null)||
                reward.Candidates.Select(x=>x.Id).Distinct().Count()!=reward.Candidates.Count||
                (reward.PromotedFrom!=null&&reward.PromotedFrom!=BossDefinition.LegacyPolicy))throw new InvalidDataException("Invalid reward candidates");
            List<BossRewardCandidate> expected;
            try{expected=Generate(reward);}catch(ArgumentException error){throw new InvalidDataException("Invalid reward input",error);}
            if(expected.Count!=reward.Candidates.Count||!expected.Zip(reward.Candidates,(a,b)=>a.Same(b)).All(x=>x))throw new InvalidDataException("Reward candidates changed");
            if(!reward.Applied)
            {
                if(reward.SelectedId!=null||reward.GoldApplied||completed||!Same(reward.Weapons,run.Weapons)||!Same(reward.Passives,run.Passives)||!Same(reward.Forms,run.Forms))
                    throw new InvalidDataException("Invalid unapplied reward receipt");
            }
            else
            {
                var selected=reward.Candidates.SingleOrDefault(x=>x.Id==reward.SelectedId);
                if(selected==null||reward.GoldApplied!=(selected.Kind=="gold"))throw new InvalidDataException("Invalid applied reward receipt");
                if(selected.Kind=="evolution" && (!(run.Forms?.TryGetValue(EvolutionRules.Base(selected.ItemId),out var form)??false)||form!=selected.ItemId))
                    throw new InvalidDataException("Missing granted evolution");
                if(selected.Kind=="item" && !(BossRewards.Passives.Contains(selected.ItemId)?run.Passives:run.Weapons).TryGetValue(selected.ItemId,out _))throw new InvalidDataException("Missing granted item");
                if(selected.Kind=="item" && (BossRewards.Passives.Contains(selected.ItemId)?run.Passives:run.Weapons)[selected.ItemId]<selected.Level)throw new InvalidDataException("Missing granted item level");
            }
        }
    }
}
