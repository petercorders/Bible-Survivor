using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Numerics;

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
        internal static bool KnownGrowthChoice(string id, int version=1)=>Passives.Contains(id)||EquipmentInventory.SupportsWeapon(id,version)||
            id.StartsWith(GrowthGoldChoicePrefix,StringComparison.Ordinal)&&int.TryParse(id.Substring(GrowthGoldChoicePrefix.Length),System.Globalization.NumberStyles.None,System.Globalization.CultureInfo.InvariantCulture,out var serial)&&serial>=0;
        private readonly GameSnapshot state;
        private readonly RoundDefinition definition;
        private readonly BossDefinition? experimentalBoss;
        internal bool IsExperimental {get;}
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
        public BossDefinition? BossRules=>IsExperimental?experimentalBoss:BossDefinition.Find(definition.Number);
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
        public bool CanEnterLeviticus=>ExodusComplete&&run.PendingLevels==0;
        public bool LeviticusComplete=>IsCurrent&&definition.Number==LeviticusContent.LastInstalledRound&&run.Stage=="between_rounds"&&run.Boss?.CompletionApplied==true;
        public bool CanEnterNumbers=>LeviticusComplete&&run.PendingLevels==0;
        public bool NumbersComplete=>IsCurrent&&definition.Number==NumbersContent.LastRound&&run.Stage=="between_rounds"&&run.Boss?.CompletionApplied==true;
        public bool CanEnterDeuteronomy=>NumbersComplete&&run.PendingLevels==0;
        public bool DeuteronomyComplete=>IsCurrent&&definition.Number==DeuteronomyContent.LastRound&&run.Stage=="between_rounds"&&run.Boss?.CompletionApplied==true;
        public bool CanEnterJoshua=>DeuteronomyComplete&&run.PendingLevels==0;
        public bool JoshuaComplete=>IsCurrent&&definition.Number==JoshuaContent.LastRound&&run.Stage=="between_rounds"&&run.Boss?.CompletionApplied==true;
        public bool CanEnterJudges=>JoshuaComplete&&run.PendingLevels==0;
        public bool JudgesComplete=>IsCurrent&&definition.Number==JudgesContent.LastRound&&run.Stage=="between_rounds"&&run.Boss?.CompletionApplied==true;
        public bool CanEnterRuth=>JudgesComplete&&run.PendingLevels==0;
        public bool RuthComplete=>IsCurrent&&definition.Number==RuthContent.LastRound&&run.Stage=="between_rounds"&&run.Boss?.CompletionApplied==true;
        public bool CanEnterFirstSamuel=>RuthComplete&&run.PendingLevels==0;
        public bool CanEnterLaterBook=>IsCurrent&&run.Stage=="between_rounds"&&run.PendingLevels==0&&LaterCampaignBooks.StartsBook(run.RoundNumber);
        public bool ContentComplete=>IsCurrent&&definition.Number==CampaignContent.LastInstalledRound&&run.Stage=="between_rounds"&&(run.RoundCompletionApplied||run.Boss?.CompletionApplied==true);
        public bool ExodusComplete=>IsCurrent&&definition.Number==15&&run.Stage=="between_rounds"&&run.Boss?.CompletionApplied==true;
        public bool ChapterComplete=>IsCurrent&&definition.Number==9&&run.Stage=="between_rounds"&&run.Boss?.CompletionApplied==true;
        internal int SpeciesOrdinal(int id) => Arena!.SpeciesOrdinal(id);
        public IReadOnlyDictionary<int,int> DroppedExperience => new System.Collections.ObjectModel.ReadOnlyDictionary<int,int>(run.Drops);
        public double CurrentHealth => state.Run == run ? run.Health : 0;
        public double DamageMultiplier => 1 + 0.03 * PermanentRules.Rank(run.PermanentSnapshot, "attack") + 0.1 * Passive("hair");
        public double BibleRotationMultiplier => WeaponRules.Get("bible", Inventory.Weapons["bible"]).RotationSpeedMultiplier +
            (HasExpandedSkill("weapon.bible.a.01") ? .12 : 0);
        public double JawboneArcDegrees =>
            (HasExpandedSkill("weapon.jawbone.a.01") ? 140 : 120) + 2 * AreaProfile().HalfAngleBonus;
        public double MoveSpeedMultiplier => MoveSpeedAt(false);
        public double PickupRangeMultiplier => PickupRangeAt(false);
        private PermanentActionState Actions => run.PermanentActions!;
        private PermanentRecoveryState Recovery => run.PermanentRecovery!;
        private double HealthRatio => run.Health / run.MaxHealth;
        internal double MoveSpeedAt(bool surrounded) => CombatRulesVersion == 2
            ? 1 + PermanentActionRules.Move(run.PermanentSnapshot, Actions, CombatTime, run.LastHitTime,
                HealthRatio, surrounded, run.Stage == "boss", Passive("underwear"))
            : 1 + .05 * PermanentRules.Rank(run.PermanentSnapshot, "move") + .1 * Passive("underwear");
        internal double PickupRangeAt(bool surrounded) => CombatRulesVersion == 2
            ? 1 + PermanentActionRules.Pickup(run.PermanentSnapshot, Actions, CombatTime, HealthRatio,
                surrounded, BossRules != null && run.Stage == "normal")
            : 1 + .1 * PermanentRules.Rank(run.PermanentSnapshot, "pickup");
        internal void ObserveMotion(Vector2 movement)
        {
            if (CombatRulesVersion == 2) Actions.ObserveMotion(CombatTime, movement);
        }
        public int CombatRulesVersion => run.CombatRulesVersion ?? 1;
        internal bool HasExpandedSkill(string id) => CombatRulesVersion == 2 && PermanentRules.Rank(run.PermanentSnapshot, id) > 0;
        internal double RollOriginalDamage(string weaponId, double damage)
        {
            if (CombatRulesVersion != 2 || damage <= 0 || run.CriticalRandomStates == null) return damage;
            var profile = PermanentOriginalHitRules.BaseCritical(run.PermanentSnapshot, weaponId, damage);
            if (!profile.RequiresCriticalRoll) return damage;
            var streams = run.CriticalRandomStates!;
            streams.TryGetValue(weaponId, out var previous);
            var current = PermanentOriginalHitRules.NextCriticalState(run.RunId, definition.Number, weaponId, previous);
            streams[weaponId] = current;
            return profile.Resolve(current / 4294967296d).Damage;
        }
        internal bool IsEliteActor(int id) =>
            Encounter != null ? IsBossActor(id) :
            TargetMaxHealth(id) >= 3L * definition.Waves.SelectMany(wave => wave.Groups).Min(group => group.Health);
        internal bool IsBossActor(int id) => Encounter != null && id == BossRules!.BossActorId;
        internal ExpandedLightningSettings LightningSettings() =>
            ExpandedLightningSettings.FromRanks(run.PermanentSnapshot, IsBossActor, IsEliteActor);
        internal ExpandedFirePillarSettings FirePillarSettings() => new ExpandedFirePillarSettings(
            HasExpandedSkill("weapon.fire_pillar.a.01"),
            HasExpandedSkill("weapon.fire_pillar.a.02") ? IsEliteActor : (Func<int, bool>?)null,
            HasExpandedSkill("weapon.fire_pillar.e.01"), emberRadiusBonus: AreaProfile().HitShapeBonus,
            predictPlacement: HasExpandedSkill("weapon.fire_pillar.a.03"),
            preferSeparatedCenters: HasExpandedSkill("weapon.fire_pillar.a.04"),
            slowInCrowds: HasExpandedSkill("weapon.fire_pillar.e.02"),
            increaseCoreDamage: HasExpandedSkill("weapon.fire_pillar.b.02"),
            increaseIgnitionDamage: HasExpandedSkill("weapon.fire_pillar.d.01"));
        internal ExpandedWildernessVoiceRankOptions WildernessVoiceRankOptions() =>
            ExpandedWildernessVoiceRankOptions.FromRanks(run.PermanentSnapshot);
        internal ExpandedMosesStaffRankOptions MosesStaffRankOptions() =>
            ExpandedMosesStaffRankOptions.FromRanks(run.PermanentSnapshot);
        internal ExpandedCrossRankOptions CrossRankOptions() => ExpandedCrossRankOptions.FromRanks(run.PermanentSnapshot);
        internal ExpandedDanielLionRankOptions DanielLionRankOptions() =>
            ExpandedDanielLionRankOptions.FromRanks(run.PermanentSnapshot);
        public double LionRoarRadius(bool guardian) => DanielLionRankOptions().RoarRadius(guardian);
        internal ExpandedElijahChariotRankOptions ElijahChariotRankOptions() =>
            ExpandedElijahChariotRankOptions.FromRanks(run.PermanentSnapshot);
        internal PermanentAreaProfile AreaProfile() => HasExpandedSkill("common.area.01")
            ? PermanentAreaRules.Evaluate(run.PermanentSnapshot) : default;
        internal long BossEntryVoiceCycle => run.Stage == "boss" ? run.BossEntryVoiceCycle : 0;
        internal void RecordVoiceCyclesAtBossEntry(long cycle)
        {
            if (HasExpandedSkill("weapon.wilderness_voice.a.05") || HasExpandedSkill("weapon.wilderness_voice.e.03"))
                run.BossEntryVoiceCycle = cycle;
        }
        public double LastRetaliationDamage {get; private set;}
        internal double LastDamageReceived { get; private set; }
        public CombatStats AttackProfile(string weaponId) => AttackProfile(weaponId, false);

        internal CombatStats AttackProfile(string weaponId, bool firstBibleWait, bool firstTempoWait = false)
        {
            if(!Inventory.Weapons.TryGetValue(weaponId,out var level)) throw new InvalidOperationException("Weapon not equipped");
            var bonuses = new CombatBonuses {
                DamageBonus=DamageMultiplier-1,ExtraAmount=Passive("stones"),DurationBonus=.1*Passive("cloud"),
                CooldownReduction=.02*PermanentRules.Rank(run.PermanentSnapshot,"cooldown")+.08*Passive("thunder"),
                AreaBonus=.1*Passive("whirlwind"),ProjectileSpeedBonus=.1*Passive("east_wind") };
            Inventory.Forms.TryGetValue(weaponId, out var form);
            if (CombatRulesVersion == 2)
            {
                var skillPrefix = "weapon." + weaponId + ".base.";
                var area = AreaProfile();
                bonuses.DamageBonus += .10 * PermanentRules.Rank(run.PermanentSnapshot, skillPrefix + "01");
                bonuses.AreaBonus += area.HitShapeBonus;
                bonuses.TravelDistanceBonus = area.FlightDistanceBonus;
                bonuses.PlacedRadiusBonus = area.PlacedCircleBonus;
                bonuses.OrbitDistanceBonus = area.OrbitDistanceBonus;
                var weaponWaitReduction = .05 * PermanentRules.Rank(run.PermanentSnapshot, skillPrefix + "02");
                if (firstBibleWait) weaponWaitReduction += .25;
                if (HasExpandedSkill("common.tempo.02"))
                {
                    var tempo = PermanentTempoRules.Evaluate(run.PermanentSnapshot, TempoContext(firstTempoWait),
                        Passive("thunder"), otherCycleReduction: weaponWaitReduction);
                    bonuses.CooldownReduction = tempo.CycleReduction;
                    bonuses.BurstReduction = tempo.BurstReduction;
                }
                else bonuses.CooldownReduction += weaponWaitReduction;
                if (HasExpandedSkill("common.duration.01")) bonuses.DurationBonus += .05;
                if ((weaponId == "sling" || weaponId == "cross" || weaponId == "moses_staff" || weaponId == "elijah_chariot") &&
                    HasExpandedSkill("common.shot.01")) bonuses.ProjectileSpeedBonus += .05;
                var specialization = PermanentRules.Rank(run.PermanentSnapshot, skillPrefix + "03");
                var collisionBonus = weaponId == "sling" || weaponId == "cross"
                    ? area.ProjectileRadiusBonus + (HasExpandedSkill("common.shot.02") ? .04 : 0) : 0;
                if (ExpandedWeaponRules.TryGet(weaponId, out var expanded))
                {
                    bonuses.ExtraAmount = weaponId == "wilderness_voice" ? 0 : specialization;
                    if (weaponId == "wilderness_voice" && HasExpandedSkill("weapon.wilderness_voice.a.01") ||
                        weaponId == "fire_pillar" && HasExpandedSkill("weapon.fire_pillar.b.01")) bonuses.DurationBonus += .12;
                    return CombatStats.ForScheduled(expanded, level, bonuses, specialization,
                        HasExpandedSkill(skillPrefix + "04"), HasExpandedSkill(skillPrefix + "05"), collisionBonus);
                }
                bonuses.ExtraAmount += specialization;
                switch (weaponId)
                {
                    case "bible":
                        if (HasExpandedSkill(skillPrefix + "05")) collisionBonus = .12;
                        break;
                    case "jawbone":
                        if (HasExpandedSkill(skillPrefix + "04")) bonuses.AreaBonus += .10;
                        break;
                    case "sling":
                        if (HasExpandedSkill(skillPrefix + "04")) collisionBonus += .10;
                        if (HasExpandedSkill(skillPrefix + "05")) bonuses.ProjectileSpeedBonus += .12;
                        if (HasExpandedSkill("common.shot.03")) bonuses.ExtraPierce++;
                        if (HasExpandedSkill("weapon.sling.d.01")) bonuses.ExtraPierce++;
                        if (HasExpandedSkill("weapon.sling.b.01"))
                            bonuses.BurstReduction = Math.Min(.25, bonuses.BurstReduction + .12);
                        break;
                }
                return CombatStats.ForExpanded(weaponId, level, bonuses, form, collisionBonus);
            }
            var profile = CombatStats.For(weaponId, level, bonuses);
            return form != null ? profile.Evolved(form) : profile;
        }

        internal bool TempoOpeningAvailable => HasExpandedSkill("common.tempo.02") && !run.TempoOpeningUsed;

        internal void ObserveTempoOpening()
        {
            if (TempoOpeningAvailable) run.TempoOpeningUsed = true;
        }

        private double ActiveRoundTime(double combatTime) => run.Boss is { } boss
            ? combatTime - Math.Min(boss.IntroElapsed, Math.Max(0, combatTime - boss.NormalEndedAt))
            : combatTime;

        private PermanentTempoContext TempoContext(bool firstTempoWait)
        {
            if (!IsActive) return default;
            double ContextTime(double time) => Math.Round(ActiveRoundTime(time), 9);
            var now = ContextTime(CombatTime);
            double? sinceHurt = run.LastHitTime >= 0 ? Math.Round(now - ContextTime(run.LastHitTime), 9) : (double?)null;
            double? sinceEliteKill = run.LastTempoEliteKillAt is double killAt
                ? Math.Round(now - ContextTime(killAt), 9) : (double?)null;
            return new PermanentTempoContext(firstTempoWait, now, sinceHurt,
                sinceHurt ?? Math.Round(run.NoHitCarrySeconds + now, 9),
                sinceEliteKill, run.Stage == "boss");
        }
        public IReadOnlyList<string> PendingChoices => run.Choices.AsReadOnly();

        internal bool ConsumeFirstBibleAttack()
        {
            if (!HasExpandedSkill("weapon.bible.base.04") || run.FirstBibleAttackUsed) return false;
            run.FirstBibleAttackUsed = true;
            return true;
        }

        internal double JawboneDamage(double damage, double distance, double reach)
        {
            if (!HasExpandedSkill("weapon.jawbone.base.05") || distance + 1e-8 < .8 * reach) return damage;
            var baseDamage = WeaponRules.Get("jawbone", Inventory.Weapons["jawbone"]).Damage;
            return Math.Min(baseDamage * 2.25, damage + baseDamage * .08);
        }

        public AdamSession(GameSnapshot state, RoundDefinition definition, string startingWeapon)
        {
            state.Validate();
            if (state.Run != null) throw new InvalidOperationException("Existing run must be resumed or abandoned explicitly");
            if (definition.Number != 1 || !state.Profile.UnlockedWeapons.Contains(startingWeapon)) throw new ArgumentException("Invalid R01 start");
            this.state = state; this.definition = definition;
            Inventory = new EquipmentInventory(startingWeapon, state.SchemaVersion == SaveFormat.CurrentVersion ? 2 : 1);
            Growth = new GrowthLedger(); Wave = new WaveDirector(definition);
            var hp = PermanentCombatRules.StartingHealth(state.Profile.Upgrades, state.SchemaVersion == SaveFormat.CurrentVersion);
            run = new RunState { RunId = Guid.NewGuid().ToString("N"), Health = hp, MaxHealth = hp,
                PermanentSnapshot = new Dictionary<string,int>(state.Profile.Upgrades), RoundFingerprint=definition.Fingerprint };
            if (state.SchemaVersion == SaveFormat.CurrentVersion)
            {
                run.CampaignLayout = "genesis";
                run.CombatRulesVersion = 2;
                run.PermanentCatalogVersion = PermanentSkillCatalog.CurrentVersion;
                ResetPermanentRound(run);
            }
            else state.SchemaVersion = SaveFormat.GenesisVersion;
            state.Run = run; Capture();
        }

        private AdamSession(GameSnapshot state, RoundDefinition definition)
        {
            state.Validate();
            run = state.Run ?? throw new InvalidOperationException("No run to resume");
            if (definition.Number < 1 || definition.Number > CampaignContent.LastInstalledRound ||
                (run.Stage=="between_rounds" ? run.RoundNumber!=definition.Number+1 : run.RoundNumber!=definition.Number || (run.Stage=="normal" && run.Wave==null)))
                throw new InvalidOperationException("Round definition does not match checkpoint");
            if (run.RoundFingerprint != null && run.RoundFingerprint != definition.Fingerprint &&
                !(run.Stage=="boss_reward"&&run.Boss?.Reward?.PolicyId==BossDefinition.LegacyPolicy&&run.RoundFingerprint==definition.PreviousBossFingerprint))
                throw new ArgumentException("Round catalog changed; checkpoint preserved");
            if (run.RoundFingerprint == null && definition.Number != 1)
                throw new ArgumentException("Legacy saves support only R01 and its completion checkpoint");
            if(definition.Number>=LeviticusContent.GlobalRoundNumber)CampaignContent.Validate(definition);
            else if(definition.Number>=10)ExodusContent.Validate(definition);
            this.state = state; this.definition = definition;
            Inventory = EquipmentInventory.Restore(run.Weapons, run.Passives, run.Forms, CombatRulesVersion);
            Growth = new GrowthLedger(run.Level, run.Experience); Wave = run.Wave == null ? null : WaveDirector.Restore(definition, run.Wave);
            var eligible = GrowthChoices(Inventory, run.GrowthChoiceUnlockedWeapons ?? state.Profile.UnlockedWeapons, run.ChoiceSerial);
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
            if (candidate.SchemaVersion == SaveFormat.CurrentVersion) entering.CampaignLayout = "campaign";
            else candidate.SchemaVersion = SaveFormat.CampaignVersion;
            entering.Stage="normal";entering.RoundFingerprint=next.Fingerprint;
            entering.NormalEndedAt=null;entering.RoundCompletionApplied=false;
            entering.Wave=new WaveDirector(next).Snapshot();entering.World=null;entering.Drops.Clear();entering.LastHitTime=-1;
            ResetPermanentRound(entering);
            candidate.Validate();_ = Resume(candidate,next);return candidate;
        }

        // Publish only after the caller commits and resumes the candidate.
        public GameSnapshot PrepareLeviticus(RoundDefinition next)
        {
            if(!CanEnterLeviticus)throw new InvalidOperationException("Explicit Leviticus entry requires completed R15");
            if(next.Number!=LeviticusContent.GlobalRoundNumber)throw new ArgumentException("Leviticus entry requires R16");
            LeviticusContent.Validate(next);
            state.Validate();
            var candidate=SaveStore.Copy(state);
            var entering=candidate.Run!;
            entering.CompletedFifthBoss=entering.Boss!.Copy();
            entering.Boss=null;
            entering.PreviousNormalRound=15;
            entering.Stage="normal";
            entering.RoundFingerprint=next.Fingerprint;
            entering.NormalEndedAt=null;
            entering.RoundCompletionApplied=false;
            entering.Wave=new WaveDirector(next).Snapshot();
            entering.World=null;
            entering.Drops.Clear();
            entering.LastHitTime=-1;
            ResetPermanentRound(entering);
            _ = Resume(candidate,next);
            return candidate;
        }

        public GameSnapshot PrepareNumbers(RoundDefinition next)=>PrepareLaterBook(CanEnterNumbers,NumbersContent.GlobalRoundNumber,next,"Numbers");
        public GameSnapshot PrepareDeuteronomy(RoundDefinition next)=>PrepareLaterBook(CanEnterDeuteronomy,DeuteronomyContent.GlobalRoundNumber,next,"Deuteronomy");
        public GameSnapshot PrepareJoshua(RoundDefinition next)=>PrepareLaterBook(CanEnterJoshua,JoshuaContent.GlobalRoundNumber,next,"Joshua");
        public GameSnapshot PrepareJudges(RoundDefinition next)=>PrepareLaterBook(CanEnterJudges,JudgesContent.GlobalRoundNumber,next,"Judges");
        public GameSnapshot PrepareRuth(RoundDefinition next)=>PrepareLaterBook(CanEnterRuth,RuthContent.GlobalRoundNumber,next,"Ruth");
        public GameSnapshot PrepareFirstSamuel(RoundDefinition next)=>PrepareLaterBook(CanEnterFirstSamuel,FirstSamuelContent.GlobalRoundNumber,next,"First Samuel");

        public GameSnapshot PrepareLaterBook(RoundDefinition next)
        {
            var book=LaterCampaignBooks.Require(run.RoundNumber);
            return PrepareLaterBook(CanEnterLaterBook,book.FirstRound,next,book.Name);
        }

        private GameSnapshot PrepareLaterBook(bool allowed,int first,RoundDefinition next,string book)
        {
            if(!allowed)throw new InvalidOperationException("Explicit "+book+" entry requires completed R"+(first-1).ToString(CultureInfo.InvariantCulture));
            if(next.Number!=first)throw new ArgumentException(book+" entry requires R"+first.ToString(CultureInfo.InvariantCulture));
            CampaignContent.Validate(next);
            state.Validate();
            var candidate=SaveStore.Copy(state);
            BeginNextRound(candidate.Run!,next);
            _=Resume(candidate,next);
            return candidate;
        }

        public AdamSession StartNextRound(RoundDefinition next)
        {
            if(!IsCurrent || run.Stage!="between_rounds" || run.PendingLevels!=0 ||
                next.Number!=run.RoundNumber || next.Number==10 || next.Number==LeviticusContent.GlobalRoundNumber || next.Number==NumbersContent.GlobalRoundNumber || next.Number==DeuteronomyContent.GlobalRoundNumber ||
                next.Number==JoshuaContent.GlobalRoundNumber || next.Number==JudgesContent.GlobalRoundNumber ||
                next.Number==RuthContent.GlobalRoundNumber || next.Number==FirstSamuelContent.GlobalRoundNumber || LaterCampaignBooks.StartsBook(next.Number) || next.Number>CampaignContent.LastInstalledRound || definition.Number!=next.Number-1)
                throw new InvalidOperationException("Next round requires the previous completed round and resolved choices");
            if(next.Number>=LeviticusContent.GlobalRoundNumber)CampaignContent.Validate(next);
            else if(next.Number>=11)ExodusContent.Validate(next);
            state.Validate(); Capture();
            BeginNextRound(run,next);
            return Resume(state,next);
        }

        // The simulator extends an existing run in memory. Public entry and
        // save validation continue to enforce the installed campaign boundary.
        internal AdamSession StartExperimentalRound(RoundDefinition next,BossDefinition? boss)
        {
            if(next.Number<LaterCampaignBooks.FirstRound||!IsCurrent||run.Stage!="between_rounds"||
                run.PendingLevels!=0||next.Number!=run.RoundNumber||next.Number!=definition.Number+1||
                (boss!=null&&boss.Number!=next.Number))
                throw new InvalidOperationException("Experimental round requires a completed preceding round");
            BeginNextRound(run,next);
            return new AdamSession(this,next,boss);
        }

        private AdamSession(AdamSession previous,RoundDefinition next,BossDefinition? boss)
        {
            state=previous.state;run=previous.run;definition=next;experimentalBoss=boss;IsExperimental=true;
            Inventory=EquipmentInventory.Restore(run.Weapons, run.Passives,run.Forms, CombatRulesVersion);
            Growth=new GrowthLedger(run.Level,run.Experience);
            Wave=WaveDirector.Restore(definition,run.Wave!);
        }

        private static void BeginNextRound(RunState run,RoundDefinition next)
        {
            var previous=next.Number-1;
            // Construct before publishing the new arena. No reward or healing is performed on entry/retry.
            var arena=new WaveDirector(next);
            if(run.Boss!=null)
            {
                if(previous>=21)
                {
                    (run.LaterBossHistory??=new List<BossEncounterState>()).Add(run.Boss.Copy());
                    run.PreviousNormalRound=previous;
                }
                else if(previous==18){run.CompletedSixthBoss=run.Boss.Copy();run.PreviousNormalRound=18;}
                else if(previous==12){run.CompletedFourthBoss=run.Boss.Copy();run.PreviousNormalRound=12;}
                else if(previous==6)run.CompletedSecondBoss=run.Boss.Copy();
                else run.CompletedBoss=run.Boss.Copy();
                run.Boss=null;
            }
            if(run.RoundCompletionApplied)run.PreviousNormalRound=previous;
            run.NormalEndedAt=null;run.RoundCompletionApplied=false;
            run.Stage="normal";run.RoundFingerprint=next.Fingerprint;run.Wave=arena.Snapshot();
            run.World=null;run.Drops.Clear();run.LastHitTime=-1;
            ResetPermanentRound(run);
        }

        private static void ResetPermanentRound(RunState target)
        {
            if (target.CombatRulesVersion != 2) return;
            target.PermanentActions = new PermanentActionState();
            target.PermanentRecovery = new PermanentRecoveryState();
            target.CriticalRandomStates = PermanentOriginalHitRules.HasBaseCriticalChance(target.PermanentSnapshot)
                ? new Dictionary<string, uint>(StringComparer.Ordinal) : null;
            target.FirstBibleAttackUsed = false;
            target.LastTempoEliteKillAt = null;
            target.TempoOpeningUsed = false;
            target.BossEntryVoiceCycle = 0;
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
            var regen = CombatRulesVersion == 2
                ? PermanentRecoveryRules.Rate(run.PermanentSnapshot, CombatTime - Math.Max(0, run.LastHitTime),
                    HealthRatio, Actions.Moving ? 0 : CombatTime - Actions.StationarySince)
                : .1 * PermanentRules.Rank(run.PermanentSnapshot, "regen");
            run.Health = Math.Min(run.MaxHealth, run.Health + elapsed * regen);
        }
        public void Pause(bool paused) { if(!IsActive)return; if(run.Boss!=null)run.Boss.Paused=paused; else if(Arena!=null)Arena.Paused=paused; Capture(); }

        public void Hit(int id,double damage) => Hit(id,damage,null);

        internal void Hit(int id,double damage,DateTimeOffset? clearTime,bool original = true)
        {
            Guard.NonNegative(damage,nameof(damage));
            if(run.Boss!=null)
            {
                if(!CanAttack)return;
                if(id==BossRules!.BossActorId)
                {
                    run.Boss.Health=Math.Max(0,run.Boss.Health-damage);
                    if(run.Boss.Health==0) BossDefeated(clearTime);
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
                    if(support.Health<=0)
                    {
                        run.Boss.Supports.Remove(support);run.Drops.Add(id,0);CreditKill();
                        RecoverOnKill(PermanentRecoveryKillKind.BossSupport, original);
                    }
                }
                Capture();return;
            }
            if(!IsActive || Wave==null || Wave.Paused || run.PendingLevels>0 || !Wave.Active.TryGetValue(id,out var enemy)) return;
            if(damage>=enemy.Health) Kill(id, original); else Arena!.Hit(id,damage);
            Capture();
        }

        public void Kill(int id) => Kill(id, true);

        private void Kill(int id, bool original)
        {
            if(run.Boss!=null){if(Targets.TryGetValue(id,out var target))Hit(id,target.Health,null,original);return;}
            if (!IsActive || Wave == null || Wave.Paused || run.PendingLevels > 0 || !Wave.Active.ContainsKey(id)) return;
            if (original && (HasExpandedSkill("common.regen.05") || HasExpandedSkill("common.regen.06") ||
                HasExpandedSkill("common.tempo.08")))
            {
                var elite = IsEliteActor(id);
                if (elite && HasExpandedSkill("common.tempo.08")) run.LastTempoEliteKillAt = CombatTime;
                RecoverOnKill(elite ? PermanentRecoveryKillKind.Elite : PermanentRecoveryKillKind.Normal, true);
            }
            var xp = Arena!.Kill(id); run.Drops.Add(id,xp);
            CreditKill();
            if (Wave.IsNormalComplete) Finish();
            Capture();
        }

        private void RecoverOnKill(PermanentRecoveryKillKind kind, bool original)
        {
            if (!original || CombatRulesVersion != 2 ||
                kind == PermanentRecoveryKillKind.BossSupport && !HasExpandedSkill("common.regen.09")) return;
            run.Health += PermanentRecoveryRules.OnKill(Recovery, run.PermanentSnapshot, CombatTime, kind, true,
                run.Health, run.MaxHealth);
        }
        public void Collect(int id) => Collect(id, false);

        internal void Collect(int id, bool surrounded)
        {
            if (!CanAttack || !run.Drops.TryGetValue(id, out var xp)) return;
            run.Drops.Remove(id); AwardExperience(xp, surrounded); Capture();
        }
        private void AwardExperience(double raw, bool surrounded = false, bool settling = false)
        {
            if (CombatRulesVersion == 2 && raw > 0)
            {
                var bonus = PermanentActionRules.Experience(run.PermanentSnapshot, Actions, CombatTime, Growth.Level,
                    HealthRatio, surrounded, BossRules != null && (run.Stage == "normal" || settling), Passive("scroll"));
                run.PendingLevels += Growth.Add(raw, bonus);
                Actions.Collect(CombatTime);
            }
            else run.PendingLevels += Growth.Add(raw, .03 * PermanentRules.Rank(run.PermanentSnapshot, "growth"), .08 * Passive("scroll"));
            EnsureChoices();
        }
        private void EnsureChoices()
        {
            if (run.PendingLevels == 0 || run.Choices.Count > 0) return;
            if (CombatRulesVersion == 2)
                run.GrowthChoiceUnlockedWeapons = state.Profile.UnlockedWeapons
                    .Where(id => EquipmentInventory.SupportsWeapon(id, 2)).OrderBy(id => id,StringComparer.Ordinal).ToList();
            run.Choices = GrowthChoices(Inventory, run.GrowthChoiceUnlockedWeapons ?? state.Profile.UnlockedWeapons, run.ChoiceSerial).ToList();
        }
        public bool Choose(string id)
        {
            if (!IsCurrent || run.Stage=="boss_reward" || run.PendingLevels <= 0 || !run.Choices.Contains(id)) return false;
            if(id==GrowthGoldChoicePrefix+run.ChoiceSerial.ToString(System.Globalization.CultureInfo.InvariantCulture))
            {
                if(Inventory.Choices(run.GrowthChoiceUnlockedWeapons ?? state.Profile.UnlockedWeapons,Passives,3,run.ChoiceSerial).Count!=0)return false;
                state.Profile.Gold=checked(state.Profile.Gold+25);
            }
            else if (!Inventory.TryChoose(id, Passives.Contains(id))) return false;
            run.PendingLevels--; run.ChoiceSerial++; run.Choices.Clear(); run.GrowthChoiceUnlockedWeapons=null; EnsureChoices();
            if(run.Stage=="round_settlement"&&run.PendingLevels==0)CompleteNormalSettlement();
            Capture(); return true;
        }
        internal int EffectiveContactDamage(int incoming) =>
            EffectiveIncomingDamage(incoming, IncomingDamageKind.Contact, false, false);

        private int EffectiveIncomingDamage(int incoming, IncomingDamageKind kind, bool eliteOrBoss, bool surrounded)
        {
            var armor = CombatRulesVersion == 2
                ? PermanentCombatRules.Armor(run.PermanentSnapshot, Passive("seal"), kind, run.Health / run.MaxHealth,
                    CombatTime - Math.Max(0, run.LastHitTime), eliteOrBoss, surrounded, run.Stage == "boss")
                : PermanentRules.Rank(run.PermanentSnapshot, "armor") + Passive("seal");
            return Math.Max(1, incoming - armor);
        }

        public void Damage(int incoming) => Damage(incoming, IncomingDamageKind.Contact, false, false);

        internal void Damage(int incoming, IncomingDamageKind kind, bool eliteOrBoss, bool surrounded)
        {
            LastRetaliationDamage=0;
            LastDamageReceived=0;
            if (incoming < 0) throw new ArgumentOutOfRangeException(nameof(incoming));
            if (incoming == 0 || !CanAttack || CombatTime - run.LastHitTime < (run.Boss==null?0.5:0.5-1e-9)) return;
            var healthBefore = run.Health;
            run.Health -= EffectiveIncomingDamage(incoming, kind, eliteOrBoss, surrounded); run.LastHitTime = CombatTime;
            LastDamageReceived = Math.Min(healthBefore, healthBefore - run.Health);
            if (CombatRulesVersion == 2 && kind == IncomingDamageKind.Contact) Actions.LastContactAt = CombatTime;
            LastRetaliationDamage=5*Passive("crown")*DamageMultiplier;
            if (run.Health <= 0) { state.Run = null; Wave = null; return; }
            if (HasExpandedSkill("common.regen.08"))
                run.Health += PermanentRecoveryRules.OnActualDamage(Recovery, run.PermanentSnapshot, CombatTime,
                    healthBefore, run.Health, run.MaxHealth);
            Capture();
        }
        private void Finish()
        {
            if (!IsActive) return;
            if(BossRules!=null)
            {
                var ended=Wave!.Elapsed;
                foreach(var xp in run.Drops.OrderBy(x=>x.Key).Select(x=>x.Value))AwardExperience(xp);
                run.Drops.Clear();run.World=null;Wave=null;run.Stage="boss_intro";
                run.Boss=new BossEncounterState{Version=BossRules!.EncounterVersion,ChariotCharge=BossRules.UsesDash?new ChariotChargeState():null,ChainDirections=BossRules!.UsesChainProjectiles?new List<ChainDirectionState>():null,EncounterId=BossRules!.EncounterKey(run.RunId),RoundNumber=definition.Number==3?0:definition.Number,Health=BossRules.MaxHealth,NextAttackAt=BossRules.AttackPeriod,NextActorId=BossRules.BossActorId+1,NormalEndedAt=ended};
                return;
            }
            if(UsesNormalSettlement)
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
            RecordSettlement();
            run.Health = run.MaxHealth; run.Stage = "between_rounds"; run.RoundNumber = definition.Number + 1;
        }
        private bool UsesNormalSettlement=>definition.Number==4||definition.Number==5||
            definition.Number==7||definition.Number==8||definition.Number==10||definition.Number==11||
            definition.Number==13||definition.Number==14||definition.Number>=LeviticusContent.GlobalRoundNumber&&BossRules==null;

        public bool CompleteNormalSettlement()
        {
            if(!IsCurrent||!UsesNormalSettlement||run.Stage!="round_settlement"||run.PendingLevels!=0||run.RoundCompletionApplied)return false;
            state.Profile.Gold=checked(state.Profile.Gold+definition.CompletionGold);
            if(definition.UnlockWeaponId!=null&&!state.Profile.UnlockedWeapons.Contains(definition.UnlockWeaponId))state.Profile.UnlockedWeapons.Add(definition.UnlockWeaponId);
            RecordSettlement();
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
            var preview=EquipmentInventory.Restore(Inventory.Weapons, Inventory.Passives, Inventory.Forms, CombatRulesVersion);
            if(candidate.Kind=="evolution"&&!preview.TryEvolve(candidate.ItemId)||candidate.Kind=="item"&&!preview.TryChoose(candidate.ItemId,Passives.Contains(candidate.ItemId)))return false;
            if(candidate.Kind=="evolution")
            {
                if(!Inventory.TryEvolve(candidate.ItemId))return false;
                var evolved = state.Profile.EvolvedWeapons;
                var weapon = EvolutionRules.Base(candidate.ItemId);
                if (evolved != null && !evolved.Contains(weapon)) evolved.Add(weapon);
            }
            else if(candidate.Kind=="item") { if(!Inventory.TryChoose(candidate.ItemId,Passives.Contains(candidate.ItemId)))return false; }
            else if(candidate.Kind=="gold") { state.Profile.Gold=checked(state.Profile.Gold+candidate.Gold);reward.GoldApplied=true; }
            else return false;
            reward.Applied=true;reward.SelectedId=candidate.Id;run.Stage="boss_settlement";run.World=null;
            // Only an equipment change can invalidate the v2 candidate already shown to the player.
            if (CombatRulesVersion == 1 || candidate.Kind != "gold")
            {
                run.Choices.Clear();
                run.GrowthChoiceUnlockedWeapons = null;
                EnsureChoices();
            }
            Capture();return true;
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
            RecordSettlement();
            run.Health=run.MaxHealth;run.Boss.CompletionApplied=true;run.Stage="between_rounds";run.RoundNumber=definition.Number+1;run.World=null;Capture();return true;
        }
        private void CreditKill()
        { if (++run.KillRemainder == 5) { state.Profile.Gold = checked(state.Profile.Gold + 1); run.KillRemainder = 0; } }
        private void RecordSettlement()
        {
            if (HasExpandedSkill("common.tempo.07"))
                run.NoHitCarrySeconds = Math.Min(10, ActiveRoundTime(CombatTime) +
                    (run.LastHitTime >= 0 ? -ActiveRoundTime(run.LastHitTime) : run.NoHitCarrySeconds));
            if (state.Profile.HighestSettledRound is int settled)
                state.Profile.HighestSettledRound = Math.Max(settled, definition.Number);
        }
        private void BossDefeated(DateTimeOffset? clearTime)
        {
            var boss=run.Boss!;
            if(!boss.BossGoldApplied){state.Profile.Gold=checked(state.Profile.Gold+BossRules!.KillGold);boss.BossGoldApplied=true;}
            foreach(var xp in run.Drops.OrderBy(x=>x.Key).Select(x=>x.Value))AwardExperience(xp, settling: true);
            run.Drops.Clear();boss.Supports.Clear();boss.Telegraphing=false;boss.AimedShot=null;if(boss.ChariotCharge!=null){boss.ChariotCharge.Phase="chase";boss.ChariotCharge.Until=0;}run.World=null;run.Stage="boss_reward";
            boss.Reward=new BossRewardState{PolicyId=BossRules!.RewardPolicy,RewardId=boss.EncounterId+":reward",Weapons=Inventory.Weapons.ToDictionary(x=>x.Key,x=>x.Value),
                Passives=Inventory.Passives.ToDictionary(x=>x.Key,x=>x.Value),Forms=Inventory.Forms.Count==0?null:Inventory.Forms.ToDictionary(x=>x.Key,x=>x.Value),
                UnlockedWeapons=state.Profile.UnlockedWeapons.OrderBy(x=>x,StringComparer.Ordinal).ToList(),
                RulesVersion=CombatRulesVersion==2?2:(int?)null};
            boss.Reward.EligibleEvolutions=EvolutionRules.Eligibility(Inventory.Weapons,Inventory.Passives,Inventory.Forms,
                CombatRulesVersion,boss.Reward.UnlockedWeapons);
            boss.Reward.Candidates=BossRewards.Generate(boss.Reward);
            if (!IsExperimental && definition.Number==CampaignContent.OldTestamentFinalRound)
                state.OldTestamentClear = new OldTestamentClear
                {
                    RunId=run.RunId,
                    CompletedAtUnixMilliseconds=(clearTime ?? DateTimeOffset.UtcNow).ToUnixTimeMilliseconds()
                };
            AutoApplyGoldOnly();
        }
        public void Purchase(string key, int displayedPrice)
        {
            if (state.SchemaVersion == SaveFormat.CurrentVersion)
                throw new InvalidOperationException("Permanent skill purchases require a profile transaction");
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
