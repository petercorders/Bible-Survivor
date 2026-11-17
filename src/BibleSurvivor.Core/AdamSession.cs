using System;
using System.Collections.Generic;
using System.Linq;

namespace BibleSurvivor.Core
{
    // Engine-independent rounds, boss rewards and first three evolution forms.
    public sealed class AdamSession
    {
        private static readonly string[] Passives = { "scroll","hair","stones","east_wind","seal","cloud","thunder","crown","underwear","whirlwind" };
        public const string GrowthGoldChoicePrefix="growth_gold_25:";
        internal static IReadOnlyList<string> GrowthChoices(EquipmentInventory inventory,IEnumerable<string> unlocked,int serial)
        {
            var choices=inventory.Choices(unlocked,Passives,3,serial);
            return choices.Count==0?new[]{GrowthGoldChoicePrefix+serial.ToString(System.Globalization.CultureInfo.InvariantCulture)}:choices;
        }
        internal static bool KnownGrowthChoice(string id)=>Passives.Contains(id)||id=="bible"||id=="jawbone"||id=="sling"||
            id.StartsWith(GrowthGoldChoicePrefix,StringComparison.Ordinal)&&int.TryParse(id.Substring(GrowthGoldChoicePrefix.Length),System.Globalization.NumberStyles.None,System.Globalization.CultureInfo.InvariantCulture,out var serial)&&serial>=0;
        private readonly GameSnapshot state;
        private readonly RoundDefinition definition;
        private RunState run;
        public IWaveView? Wave { get; private set; }
        private WaveDirector? Arena => Wave as WaveDirector;
        public GrowthLedger Growth { get; }
        public EquipmentInventory Inventory { get; }
        private bool IsCurrent => state.Run == run && run.RoundNumber == definition.Number + (run.Stage=="between_rounds" ? 1 : 0);
        public bool IsActive => IsCurrent && (run.Stage == "normal" || run.Stage == "boss_intro" || run.Stage == "boss");
        public string Stage => run.Stage;
        public BossEncounterState? Boss => run.Boss?.Copy();
        internal BossEncounterState? Encounter => run.Boss;
        public BossDefinition? BossRules=>definition.Number==3||definition.Number==6||definition.Number==9||definition.Number==12?BossDefinition.ForRound(definition.Number):null;
        public bool NeedsRewardImplementation => IsCurrent && run.Stage=="boss_reward" && run.Boss?.Reward?.PolicyId==BossDefinition.LegacyPolicy;
        public IReadOnlyList<BossRewardCandidate> BossChoices=>run.Boss?.Reward?.Candidates?.Select(x=>x.Copy()).ToList().AsReadOnly() ?? (IReadOnlyList<BossRewardCandidate>)Array.Empty<BossRewardCandidate>();
        public string WeaponForm(string weapon)=>Inventory.Forms.TryGetValue(weapon,out var form)?form:weapon;
        internal double TargetMaxHealth(int id)
        {
            if(run.Boss!=null)return id==BossRules!.BossActorId?BossRules.MaxHealth:BossRules.SupportHitPoints;
            if(!Targets.TryGetValue(id,out var target))throw new ArgumentException("Target gone");
            return definition.Waves.SelectMany(x=>x.Groups).First(x=>x.MonsterId==target.MonsterId).Health;
        }
        public double CombatTime => run.Boss?.CombatTime ?? Wave?.Elapsed ?? run.NormalEndedAt ?? 0;
        public bool SimulationRunning => IsActive && !(run.Boss?.Paused ?? Wave?.Paused ?? false) && run.PendingLevels==0;
        public bool CanAttack => SimulationRunning && run.Stage!="boss_intro";
        public IReadOnlyDictionary<int,SpawnedEnemy> Targets => run.Boss==null ? Wave?.Active ?? new Dictionary<int,SpawnedEnemy>() :
            new System.Collections.ObjectModel.ReadOnlyDictionary<int,SpawnedEnemy>(BossTargets());
        private Dictionary<int,SpawnedEnemy> BossTargets()
        {
            if(run.Stage=="boss_reward"||run.Stage=="boss_settlement"||run.Stage=="between_rounds")return new Dictionary<int,SpawnedEnemy>();
            var result=run.Boss!.Supports.ToDictionary(x=>x.Id,x=>x.Copy());
            if(run.Boss.Health>0)result.Add(BossRules!.BossActorId,new SpawnedEnemy{Id=BossRules.BossActorId,MonsterId=BossRules.Id,Health=run.Boss.Health,ContactDamage=BossRules.Contact});
            return result;
        }
        public int RoundNumber => definition.Number;
        public bool CanEnterExodus=>ChapterComplete&&run.PendingLevels==0;
        public bool ContentComplete=>IsCurrent&&definition.Number==ExodusContent.LastInstalledRound&&run.Stage=="between_rounds"&&(run.RoundCompletionApplied||run.Boss?.CompletionApplied==true);
        public bool ChapterComplete=>IsCurrent&&definition.Number==9&&run.Stage=="between_rounds"&&run.Boss?.CompletionApplied==true;
        internal int SpeciesOrdinal(int id) => Arena!.SpeciesOrdinal(id);
        public IReadOnlyDictionary<int,int> DroppedExperience => new System.Collections.ObjectModel.ReadOnlyDictionary<int,int>(run.Drops);
        public double CurrentHealth => state.Run == run ? run.Health : 0;
        public double DamageMultiplier => 1 + 0.03 * PermanentRules.Rank(run.PermanentSnapshot, "attack") + 0.1 * Passive("hair");
        public double MoveSpeedMultiplier => 1 + .05 * PermanentRules.Rank(run.PermanentSnapshot,"move") + .1 * Passive("underwear");
        public double PickupRangeMultiplier => 1 + .1 * PermanentRules.Rank(run.PermanentSnapshot,"pickup");
        public double LastRetaliationDamage {get; private set;}
        public CombatStats AttackProfile(string weaponId)
        {
            if(!Inventory.Weapons.TryGetValue(weaponId,out var level)) throw new InvalidOperationException("Weapon not equipped");
            var profile=CombatStats.For(weaponId,level,new CombatBonuses {
                DamageBonus=DamageMultiplier-1,ExtraAmount=Passive("stones"),DurationBonus=.1*Passive("cloud"),
                CooldownReduction=.02*PermanentRules.Rank(run.PermanentSnapshot,"cooldown")+.08*Passive("thunder"),
                AreaBonus=.1*Passive("whirlwind"),ProjectileSpeedBonus=.1*Passive("east_wind") });
            return Inventory.Forms.TryGetValue(weaponId,out var form)?profile.Evolved(form):profile;
        }
        public IReadOnlyList<string> PendingChoices => run.Choices.AsReadOnly();

        public AdamSession(GameSnapshot state, RoundDefinition definition, string startingWeapon)
        {
            state.Validate();
            if (state.Run != null) throw new InvalidOperationException("Existing run must be resumed or abandoned explicitly");
            if (definition.Number != 1 || !state.Profile.UnlockedWeapons.Contains(startingWeapon)) throw new ArgumentException("Invalid R01 start");
            this.state = state; this.definition = definition;
            Inventory = new EquipmentInventory(startingWeapon); Growth = new GrowthLedger(); Wave = new WaveDirector(definition);
            var hp = 100 * (1 + 0.1 * PermanentRules.Rank(state.Profile.Upgrades,"health"));
            run = new RunState { RunId = Guid.NewGuid().ToString("N"), Health = hp, MaxHealth = hp,
                PermanentSnapshot = new Dictionary<string,int>(state.Profile.Upgrades), RoundFingerprint=definition.Fingerprint };
            state.SchemaVersion = SaveFormat.GenesisVersion; state.Run = run; Capture();
        }

        private AdamSession(GameSnapshot state, RoundDefinition definition)
        {
            state.Validate();
            run = state.Run ?? throw new InvalidOperationException("No run to resume");
            if (definition.Number < 1 || definition.Number > 13 ||
                (run.Stage=="between_rounds" ? run.RoundNumber!=definition.Number+1 : run.RoundNumber!=definition.Number || (run.Stage=="normal" && run.Wave==null)))
                throw new InvalidOperationException("Round definition does not match checkpoint");
            if (run.RoundFingerprint != null && run.RoundFingerprint != definition.Fingerprint &&
                !(run.Stage=="boss_reward"&&run.Boss?.Reward?.PolicyId==BossDefinition.LegacyPolicy&&run.RoundFingerprint==definition.PreviousBossFingerprint))
                throw new ArgumentException("Round catalog changed; checkpoint preserved");
            if (run.RoundFingerprint == null && definition.Number != 1)
                throw new ArgumentException("Legacy saves support only R01 and its completion checkpoint");
            if(definition.Number>=10)ExodusContent.Validate(definition);
            this.state = state; this.definition = definition;
            Inventory = EquipmentInventory.Restore(run.Weapons, run.Passives,run.Forms);
            Growth = new GrowthLedger(run.Level, run.Experience); Wave = run.Wave == null ? null : WaveDirector.Restore(definition, run.Wave);
            var eligible = GrowthChoices(Inventory,state.Profile.UnlockedWeapons,run.ChoiceSerial);
            if (run.Choices.Any(x => !eligible.Contains(x))) throw new ArgumentException("Invalid saved level-up choices");
        }
        public static AdamSession Resume(GameSnapshot state, RoundDefinition definition) => new AdamSession(state, definition);

        // A candidate only: the host publishes a resumed session after a successful SaveStore commit.
        public GameSnapshot PrepareExodus(RoundDefinition next)
        {
            if(!CanEnterExodus)throw new InvalidOperationException("Explicit Exodus entry requires completed R09");
            ExodusContent.Validate(next);
            if(next.Number!=10)throw new ArgumentException("Exodus entry requires R10");
            state.Validate();
            var candidate=SaveStore.Copy(state);
            var entering=candidate.Run!;
            entering.CompletedThirdBoss=entering.Boss!.Copy();entering.Boss=null;
            candidate.SchemaVersion=3;entering.Stage="normal";entering.RoundFingerprint=next.Fingerprint;
            entering.NormalEndedAt=null;entering.RoundCompletionApplied=false;
            entering.Wave=new WaveDirector(next).Snapshot();entering.World=null;entering.Drops.Clear();entering.LastHitTime=-1;
            candidate.Validate();_ = Resume(candidate,next);return candidate;
        }

        public AdamSession StartNextRound(RoundDefinition next)
        {
            if(!IsCurrent || run.Stage!="between_rounds" || run.PendingLevels!=0 ||
                next.Number!=run.RoundNumber || next.Number==10 || next.Number>13 || definition.Number!=next.Number-1)
                throw new InvalidOperationException("Next round requires the previous completed round and resolved choices");
            if(next.Number>=11)ExodusContent.Validate(next);
            state.Validate(); Capture();
            // Construct before publishing the new arena. No reward or healing is performed on entry/retry.
            var arena=new WaveDirector(next);
            if(run.Boss!=null)
            {
                if(definition.Number==12){run.CompletedFourthBoss=run.Boss.Copy();run.PreviousNormalRound=12;}
                else if(definition.Number==6)run.CompletedSecondBoss=run.Boss.Copy();
                else run.CompletedBoss=run.Boss.Copy();
                run.Boss=null;
            }
            if(run.RoundCompletionApplied)run.PreviousNormalRound=definition.Number;
            run.NormalEndedAt=null;run.RoundCompletionApplied=false;
            run.Stage="normal";run.RoundFingerprint=next.Fingerprint;run.Wave=arena.Snapshot();
            run.World=null;run.Drops.Clear();run.LastHitTime=-1;
            return Resume(state,next);
        }

        public void Advance(double seconds)
        {
            Guard.NonNegative(seconds,nameof(seconds));
            if(run.Boss!=null)
            {
                if(!SimulationRunning)return;
                if(run.Stage=="boss_intro")
                {
                    run.Boss.IntroElapsed=Math.Min(2,run.Boss.IntroElapsed+seconds);
                    if(run.Boss.IntroElapsed>=2-1e-9){run.Boss.IntroElapsed=2;run.Stage="boss";}
                }
                else throw new InvalidOperationException("Advance boss combat through BattleWorld");
                Capture();return;
            }
            if (!IsActive || Wave == null || Wave.Paused || run.PendingLevels > 0) return;
            var before = Wave.Elapsed; Arena!.Advance(seconds);
            Regenerate(Wave.Elapsed - before);
            if (Wave.IsNormalComplete) Finish();
            Capture();
        }
        // Called once for the elapsed time of a surviving combat tick, after incoming damage.
        internal void Regenerate(double elapsed)
        {
            var regen = 0.1 * PermanentRules.Rank(run.PermanentSnapshot,"regen");
            run.Health = Math.Min(run.MaxHealth, run.Health + elapsed * regen);
        }
        public void Pause(bool paused) { if(!IsActive)return; if(run.Boss!=null)run.Boss.Paused=paused; else if(Arena!=null)Arena.Paused=paused; Capture(); }

        public void Hit(int id,double damage)
        {
            Guard.NonNegative(damage,nameof(damage));
            if(run.Boss!=null)
            {
                if(!CanAttack)return;
                if(id==BossRules!.BossActorId)
                {
                    run.Boss.Health=Math.Max(0,run.Boss.Health-damage);
                    if(run.Boss.Health==0) BossDefeated();
                    else
                    {
                        var rules=BossRules!;
                        for(var threshold=0;threshold<rules.Thresholds.Count;threshold++)
                        {
                            var used=rules.Number==3?run.Boss.ThresholdUsed:(run.Boss.SummonMask&(1<<threshold))!=0;
                            if(used||run.Boss.Health>rules.MaxHealth*rules.Thresholds[threshold])continue;
                            run.Boss.ThresholdUsed=true;
                            if(rules.Number!=3)run.Boss.SummonMask|=1<<threshold;
                            run.Boss.Summoned+=BossDefinition.SupportCount;
                            for(var i=0;i<BossDefinition.SupportCount;i++)run.Boss.Supports.Add(new SpawnedEnemy{Id=run.Boss.NextActorId++,MonsterId=rules.SupportMonster,Health=rules.SupportHitPoints,ContactDamage=rules.SupportContact,RawExperience=0});
                        }
                    }
                }
                else
                {
                    var support=run.Boss.Supports.FirstOrDefault(x=>x.Id==id);
                    if(support==null)return;
                    support.Health-=damage;
                    if(support.Health<=0){run.Boss.Supports.Remove(support);run.Drops.Add(id,0);CreditKill();}
                }
                Capture();return;
            }
            if(!IsActive || Wave==null || Wave.Paused || run.PendingLevels>0 || !Wave.Active.TryGetValue(id,out var enemy)) return;
            if(damage>=enemy.Health) Kill(id); else Arena!.Hit(id,damage);
            Capture();
        }

        public void Kill(int id)
        {
            if(run.Boss!=null){if(Targets.TryGetValue(id,out var target))Hit(id,target.Health);return;}
            if (!IsActive || Wave == null || Wave.Paused || run.PendingLevels > 0 || !Wave.Active.ContainsKey(id)) return;
            var xp = Arena!.Kill(id); run.Drops.Add(id,xp);
            CreditKill();
            if (Wave.IsNormalComplete) Finish();
            Capture();
        }
        public void Collect(int id)
        {
            if (!CanAttack || !run.Drops.TryGetValue(id, out var xp)) return;
            run.Drops.Remove(id); AwardExperience(xp); Capture();
        }
        private void AwardExperience(double raw)
        {
            run.PendingLevels += Growth.Add(raw, 0.03 * PermanentRules.Rank(run.PermanentSnapshot,"growth"), 0.08 * Passive("scroll"));
            EnsureChoices();
        }
        private void EnsureChoices()
        {
            if (run.PendingLevels == 0 || run.Choices.Count > 0) return;
            run.Choices = GrowthChoices(Inventory,state.Profile.UnlockedWeapons,run.ChoiceSerial).ToList();
        }
        public bool Choose(string id)
        {
            if (!IsCurrent || run.Stage=="boss_reward" || run.PendingLevels <= 0 || !run.Choices.Contains(id)) return false;
            if(id==GrowthGoldChoicePrefix+run.ChoiceSerial.ToString(System.Globalization.CultureInfo.InvariantCulture))
            {
                if(Inventory.Choices(state.Profile.UnlockedWeapons,Passives,3,run.ChoiceSerial).Count!=0)return false;
                state.Profile.Gold=checked(state.Profile.Gold+25);
            }
            else if (!Inventory.TryChoose(id, Passives.Contains(id))) return false;
            run.PendingLevels--; run.ChoiceSerial++; run.Choices.Clear(); EnsureChoices();
            if(run.Stage=="round_settlement"&&run.PendingLevels==0)CompleteNormalSettlement();
            Capture(); return true;
        }
        public void Damage(int incoming)
        {
            LastRetaliationDamage=0;
            if (incoming < 0) throw new ArgumentOutOfRangeException(nameof(incoming));
            if (incoming == 0 || !CanAttack || CombatTime - run.LastHitTime < (run.Boss==null?0.5:0.5-1e-9)) return;
            var armor = PermanentRules.Rank(run.PermanentSnapshot,"armor") + Passive("seal");
            run.Health -= Math.Max(1, incoming - armor); run.LastHitTime = CombatTime;
            LastRetaliationDamage=5*Passive("crown")*DamageMultiplier;
            if (run.Health <= 0) { state.Run = null; Wave = null; return; }
            Capture();
        }
        private void Finish()
        {
            if (!IsActive) return;
            if(definition.Number==3||definition.Number==6||definition.Number==9||definition.Number==12)
            {
                var ended=Wave!.Elapsed;
                foreach(var xp in run.Drops.OrderBy(x=>x.Key).Select(x=>x.Value))AwardExperience(xp);
                run.Drops.Clear();run.World=null;Wave=null;run.Stage="boss_intro";
                run.Boss=new BossEncounterState{Version=definition.Number==12?4:definition.Number==9?3:2,ChariotCharge=definition.Number==12?new ChariotChargeState():null,ChainDirections=definition.Number==9?new List<ChainDirectionState>():null,EncounterId=BossRules!.EncounterKey(run.RunId),RoundNumber=definition.Number==3?0:definition.Number,Health=BossRules.MaxHealth,NextAttackAt=BossRules.AttackPeriod,NextActorId=BossRules.BossActorId+1,NormalEndedAt=ended};
                return;
            }
            if(definition.Number==4||definition.Number==5||definition.Number==7||definition.Number==8||definition.Number==10||definition.Number==11||definition.Number==13)
            {
                run.NormalEndedAt=Wave!.Elapsed;
                foreach(var xp in run.Drops.OrderBy(x=>x.Key).Select(x=>x.Value))AwardExperience(xp);
                run.Drops.Clear();run.World=null;Wave=null;run.Stage="round_settlement";
                if(run.PendingLevels==0)CompleteNormalSettlement();
                return;
            }
            // Only dropped XP is collected. Unspawned/live enemies grant nothing at timeout.
            foreach (var xp in run.Drops.OrderBy(x=>x.Key).Select(x=>x.Value)) AwardExperience(xp);
            run.Drops.Clear(); state.Profile.Gold = checked(state.Profile.Gold + definition.CompletionGold);
            run.World=null; // An earlier host checkpoint cannot describe the completed arena.
            if (definition.UnlockWeaponId != null && !state.Profile.UnlockedWeapons.Contains(definition.UnlockWeaponId))
                state.Profile.UnlockedWeapons.Add(definition.UnlockWeaponId);
            run.Health = run.MaxHealth; run.Stage = "between_rounds"; run.RoundNumber = definition.Number + 1;
        }
        public bool CompleteNormalSettlement()
        {
            if(!IsCurrent||(definition.Number!=4&&definition.Number!=5&&definition.Number!=7&&definition.Number!=8&&definition.Number!=10&&definition.Number!=11&&definition.Number!=13)||run.Stage!="round_settlement"||run.PendingLevels!=0||run.RoundCompletionApplied)return false;
            state.Profile.Gold=checked(state.Profile.Gold+definition.CompletionGold);
            if(definition.UnlockWeaponId!=null&&!state.Profile.UnlockedWeapons.Contains(definition.UnlockWeaponId))state.Profile.UnlockedWeapons.Add(definition.UnlockWeaponId);
            run.Health=run.MaxHealth;run.RoundCompletionApplied=true;run.Stage="between_rounds";run.RoundNumber=definition.Number+1;run.World=null;Capture();return true;
        }
        public bool PromoteBossReward()
        {
            if(!IsCurrent||run.Stage!="boss_reward"||run.Boss?.Reward?.PolicyId!=BossDefinition.LegacyPolicy)return false;
            var reward=run.Boss.Reward;
            if(!BossRewards.Same(reward.Weapons,Inventory.Weapons)||!BossRewards.Same(reward.Passives,Inventory.Passives)||!BossRewards.Same(reward.Forms,Inventory.Forms))return false;
            reward.PolicyId=BossDefinition.Policy;reward.PromotedFrom=BossDefinition.LegacyPolicy;
            reward.UnlockedWeapons=state.Profile.UnlockedWeapons.OrderBy(x=>x,StringComparer.Ordinal).ToList();
            reward.Candidates=BossRewards.Generate(reward);run.Boss.Version=2;run.RoundFingerprint=definition.Fingerprint;Capture();
            AutoApplyGoldOnly();return true;
        }
        public bool ChooseBossReward(string rewardId,string candidateId)
        {
            if(!IsCurrent||run.Stage!="boss_reward"||run.Boss?.Reward==null)return false;
            var reward=run.Boss.Reward;
            if(reward.PolicyId!=BossRules!.RewardPolicy||reward.RewardId!=rewardId||reward.Applied||
                !BossRewards.Same(reward.Weapons,Inventory.Weapons)||!BossRewards.Same(reward.Passives,Inventory.Passives)||!BossRewards.Same(reward.Forms,Inventory.Forms))return false;
            var candidate=reward.Candidates?.SingleOrDefault(x=>x.Id==candidateId);if(candidate==null)return false;
            // Refuse a stale/capped grant before changing inventory, money or the receipt.
            var preview=EquipmentInventory.Restore(Inventory.Weapons,Inventory.Passives,Inventory.Forms);
            if(candidate.Kind=="evolution"&&!preview.TryEvolve(candidate.ItemId)||candidate.Kind=="item"&&!preview.TryChoose(candidate.ItemId,Passives.Contains(candidate.ItemId)))return false;
            if(candidate.Kind=="evolution") { if(!Inventory.TryEvolve(candidate.ItemId))return false; }
            else if(candidate.Kind=="item") { if(!Inventory.TryChoose(candidate.ItemId,Passives.Contains(candidate.ItemId)))return false; }
            else if(candidate.Kind=="gold") { state.Profile.Gold=checked(state.Profile.Gold+candidate.Gold);reward.GoldApplied=true; }
            else return false;
            reward.Applied=true;reward.SelectedId=candidate.Id;run.Stage="boss_settlement";run.World=null;
            // Reward changes can invalidate a pending level candidate. Rebuild only that level's list using its existing serial.
            run.Choices.Clear();EnsureChoices();Capture();return true;
        }
        private void AutoApplyGoldOnly()
        {
            var reward=run.Boss?.Reward;
            if(reward?.Candidates?.Count==1&&reward.Candidates[0].Kind=="gold")ChooseBossReward(reward.RewardId,reward.Candidates[0].Id);
        }
        public bool CompleteBossSettlement()
        {
            if(!IsCurrent||run.Stage!="boss_settlement"||run.PendingLevels!=0||run.Boss?.Reward?.Applied!=true||run.Boss.CompletionApplied)return false;
            state.Profile.Gold=checked(state.Profile.Gold+definition.CompletionGold);
            if(definition.UnlockWeaponId!=null&&!state.Profile.UnlockedWeapons.Contains(definition.UnlockWeaponId))state.Profile.UnlockedWeapons.Add(definition.UnlockWeaponId);
            run.Health=run.MaxHealth;run.Boss.CompletionApplied=true;run.Stage="between_rounds";run.RoundNumber=definition.Number+1;run.World=null;Capture();return true;
        }
        private void CreditKill()
        { if (++run.KillRemainder == 5) { state.Profile.Gold = checked(state.Profile.Gold + 1); run.KillRemainder = 0; } }
        private void BossDefeated()
        {
            var boss=run.Boss!;
            if(!boss.BossGoldApplied){state.Profile.Gold=checked(state.Profile.Gold+BossRules!.KillGold);boss.BossGoldApplied=true;}
            foreach(var xp in run.Drops.OrderBy(x=>x.Key).Select(x=>x.Value))AwardExperience(xp);
            run.Drops.Clear();boss.Supports.Clear();boss.Telegraphing=false;if(boss.ChariotCharge!=null){boss.ChariotCharge.Phase="chase";boss.ChariotCharge.Until=0;}run.World=null;run.Stage="boss_reward";
            boss.Reward=new BossRewardState{PolicyId=BossRules!.RewardPolicy,RewardId=boss.EncounterId+":reward",Weapons=Inventory.Weapons.ToDictionary(x=>x.Key,x=>x.Value),
                Passives=Inventory.Passives.ToDictionary(x=>x.Key,x=>x.Value),Forms=Inventory.Forms.Count==0?null:Inventory.Forms.ToDictionary(x=>x.Key,x=>x.Value),
                UnlockedWeapons=state.Profile.UnlockedWeapons.OrderBy(x=>x,StringComparer.Ordinal).ToList(),EligibleEvolutions=EvolutionRules.Eligibility(Inventory.Weapons,Inventory.Passives,Inventory.Forms)};
            boss.Reward.Candidates=BossRewards.Generate(boss.Reward);
            AutoApplyGoldOnly();
        }
        public void Purchase(string key, int displayedPrice)
        {
            if (PermanentRules.NextPrice(state.Profile,key) != displayedPrice) throw new InvalidOperationException("Price changed; refresh selection");
            PermanentRules.Purchase(state.Profile,key);
        }
        public void Capture()
        {
            if (!IsCurrent) return;
            run.Level = Growth.Level; run.Experience = Growth.Experience;
            run.Weapons = Inventory.Weapons.ToDictionary(x=>x.Key,x=>x.Value);
            run.Forms=Inventory.Forms.Count==0?null:Inventory.Forms.ToDictionary(x=>x.Key,x=>x.Value);
            run.Passives = Inventory.Passives.ToDictionary(x=>x.Key,x=>x.Value);
            run.Wave = run.Stage == "normal" ? Arena?.Snapshot() : null;
            if(run.Boss?.Reward?.PolicyId!=BossDefinition.LegacyPolicy)run.RoundFingerprint=definition.Fingerprint;
        }
        private int Passive(string id) => Inventory.Passives.TryGetValue(id,out var level) ? level : 0;
    }
}
