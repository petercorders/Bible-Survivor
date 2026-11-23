using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace BibleSurvivor.Core
{
    public static class EvolutionRules
    {
        public const string Bible="bible_sanctuary", Jawbone="jawbone_thousand", Sling="sling_fivefold";
        public const double PulsePeriod=2, PulseRadius=2.8, PulseLifetime=.4, MinimumHitInterval=.5, JawboneArea=.25, Knockback=1;
        public const int SlingMinimum=5;
        public static string Contract=>"evolution-v1:"+string.Join(":",new[]{PulsePeriod,PulseRadius,PulseLifetime,MinimumHitInterval,JawboneArea,Knockback,(double)SlingMinimum}.Select(x=>x.ToString("R",CultureInfo.InvariantCulture)))+":strongest-max-hp-id:burst-base";
        public static string Base(string form)=>form==Bible?"bible":form==Jawbone?"jawbone":form==Sling?"sling":
            ExpandedWeaponRules.TryGetByEvolution(form,out var rules)?rules.WeaponId:throw new ArgumentException("Unknown weapon form");
        public static string Form(string weapon)=>weapon=="bible"?Bible:weapon=="jawbone"?Jawbone:weapon=="sling"?Sling:
            ExpandedWeaponRules.TryGet(weapon,out var rules)?rules.EvolutionId:throw new ArgumentException("Unknown base weapon");
        internal static string Passive(string weapon)=>weapon=="bible"?"scroll":weapon=="jawbone"?"hair":weapon=="sling"?"stones":
            ExpandedWeaponRules.Get(weapon).PassiveId;
        internal static bool Eligible(string weapon,IReadOnlyDictionary<string,int> weapons,IReadOnlyDictionary<string,int> passives,IReadOnlyDictionary<string,string>? forms)
            =>weapons.TryGetValue(weapon,out var level)&&level==8&&passives.TryGetValue(Passive(weapon),out var rank)&&rank>=1&&!(forms?.ContainsKey(weapon)??false);
        internal static List<string> Eligibility(IReadOnlyDictionary<string,int> weapons,IReadOnlyDictionary<string,int> passives,
            IReadOnlyDictionary<string,string>? forms, int combatRulesVersion=1, IReadOnlyCollection<string>? unlocked=null)
        {
            var ordering = combatRulesVersion == 2
                ? weapons.Keys.Where(id => EquipmentInventory.SupportsWeapon(id, combatRulesVersion) &&
                    unlocked?.Contains(id) == true).OrderBy(id => id, StringComparer.Ordinal)
                : (IEnumerable<string>)new[] { "bible", "jawbone", "sling" };
            return ordering.Where(id => Eligible(id,weapons,passives,forms)).Select(Form).ToList();
        }
    }
}
