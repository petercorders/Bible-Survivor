using System;
using System.Collections.Generic;
using System.Linq;

namespace BibleSurvivor.Core
{
    // R01/R02 engine-independent rules; R03 remains a checkpoint, not an implemented boss round.
    public sealed class AdamSession
    {
        private static readonly string[] Passives = { "scroll","hair","stones","east_wind","seal","cloud","thunder","crown","underwear","whirlwind" };
        private readonly GameSnapshot state;
        private readonly RoundDefinition definition;
        private RunState run;
        public IWaveView? Wave { get; private set; }
        private WaveDirector? Arena => Wave as WaveDirector;
        public GrowthLedger Growth { get; }
        public EquipmentInventory Inventory { get; }
        private bool IsCurrent => state.Run == run && run.RoundNumber == definition.Number + (run.Stage=="between_rounds" ? 1 : 0);
        public bool IsActive => IsCurrent && run.Stage == "normal";
        public int RoundNumber => definition.Number;
        public IReadOnlyDictionary<int,int> DroppedExperience => new System.Collections.ObjectModel.ReadOnlyDictionary<int,int>(run.Drops);
        public double CurrentHealth => state.Run == run ? run.Health : 0;
        public double DamageMultiplier => 1 + 0.03 * PermanentRules.Rank(run.PermanentSnapshot, "attack") + 0.1 * Passive("hair");
        public double MoveSpeedMultiplier => 1 + .05 * PermanentRules.Rank(run.PermanentSnapshot,"move") + .1 * Passive("underwear");
        public double PickupRangeMultiplier => 1 + .1 * PermanentRules.Rank(run.PermanentSnapshot,"pickup");
        public double LastRetaliationDamage {get; private set;}
        public CombatStats AttackProfile(string weaponId)
        {
            if(!Inventory.Weapons.TryGetValue(weaponId,out var level)) throw new InvalidOperationException("Weapon not equipped");
            return CombatStats.For(weaponId,level,new CombatBonuses {
                DamageBonus=DamageMultiplier-1,ExtraAmount=Passive("stones"),DurationBonus=.1*Passive("cloud"),
                CooldownReduction=.02*PermanentRules.Rank(run.PermanentSnapshot,"cooldown")+.08*Passive("thunder"),
                AreaBonus=.1*Passive("whirlwind"),ProjectileSpeedBonus=.1*Passive("east_wind") });
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
            state.Run = run; Capture();
        }

        private AdamSession(GameSnapshot state, RoundDefinition definition)
        {
            state.Validate();
            run = state.Run ?? throw new InvalidOperationException("No run to resume");
            if (definition.Number < 1 || definition.Number > 2 ||
                (run.Stage=="normal" ? run.RoundNumber!=definition.Number || run.Wave==null : run.RoundNumber!=definition.Number+1))
                throw new InvalidOperationException("Round definition does not match checkpoint; R03 is not implemented");
            if (run.RoundFingerprint != null && run.RoundFingerprint != definition.Fingerprint)
                throw new ArgumentException("Round catalog changed; checkpoint preserved");
            if (run.RoundFingerprint == null && definition.Number != 1)
                throw new ArgumentException("Legacy saves support only R01 and its completion checkpoint");
            this.state = state; this.definition = definition;
            Inventory = EquipmentInventory.Restore(run.Weapons, run.Passives);
            Growth = new GrowthLedger(run.Level, run.Experience); Wave = run.Wave == null ? null : WaveDirector.Restore(definition, run.Wave);
            var eligible = Inventory.Choices(state.Profile.UnlockedWeapons, Passives, 3, run.ChoiceSerial);
            if (run.Choices.Any(x => !eligible.Contains(x))) throw new ArgumentException("Invalid saved level-up choices");
        }
        public static AdamSession Resume(GameSnapshot state, RoundDefinition definition) => new AdamSession(state, definition);

        public AdamSession StartNextRound(RoundDefinition next)
        {
            if(!IsCurrent || run.Stage!="between_rounds" || run.PendingLevels!=0 ||
                next.Number!=run.RoundNumber || next.Number!=2 || definition.Number!=1)
                throw new InvalidOperationException("Next round requires completed R01 and resolved choices; R03 is pending");
            state.Validate(); Capture();
            // Construct before publishing the new arena. No reward or healing is performed on entry/retry.
            var arena=new WaveDirector(next);
            run.Stage="normal";run.RoundFingerprint=next.Fingerprint;run.Wave=arena.Snapshot();
            run.World=null;run.Drops.Clear();run.LastHitTime=-1;
            return Resume(state,next);
        }

        public void Advance(double seconds)
        {
            Guard.NonNegative(seconds,nameof(seconds));
            if (!IsActive || Wave == null || Wave.Paused || run.PendingLevels > 0) return;
            var before = Wave.Elapsed; Arena!.Advance(seconds);
            var regen = 0.1 * PermanentRules.Rank(run.PermanentSnapshot,"regen");
            run.Health = Math.Min(run.MaxHealth, run.Health + (Wave.Elapsed - before) * regen);
            if (Wave.IsNormalComplete) Finish();
            Capture();
        }
        public void Pause(bool paused) { if (IsActive && Arena != null) { Arena.Paused = paused; Capture(); } }

        public void Hit(int id,double damage)
        {
            Guard.NonNegative(damage,nameof(damage));
            if(!IsActive || Wave==null || Wave.Paused || run.PendingLevels>0 || !Wave.Active.TryGetValue(id,out var enemy)) return;
            if(damage>=enemy.Health) Kill(id); else Arena!.Hit(id,damage);
            Capture();
        }

        public void Kill(int id)
        {
            if (!IsActive || Wave == null || Wave.Paused || run.PendingLevels > 0 || !Wave.Active.ContainsKey(id)) return;
            var xp = Arena!.Kill(id); run.Drops.Add(id,xp);
            if (++run.KillRemainder == 5) { state.Profile.Gold = checked(state.Profile.Gold + 1); run.KillRemainder = 0; }
            if (Wave.IsNormalComplete) Finish();
            Capture();
        }
        public void Collect(int id)
        {
            if (!IsActive || Wave == null || Wave.Paused || run.PendingLevels > 0 || !run.Drops.TryGetValue(id, out var xp)) return;
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
            run.Choices = Inventory.Choices(state.Profile.UnlockedWeapons,Passives,3,run.ChoiceSerial).ToList();
            if (run.Choices.Count == 0) throw new InvalidOperationException("No valid level-up choice: fallback is not implemented in R01");
        }
        public bool Choose(string id)
        {
            if (!IsCurrent || run.PendingLevels <= 0 || !run.Choices.Contains(id)) return false;
            if (!Inventory.TryChoose(id, Passives.Contains(id))) return false;
            run.PendingLevels--; run.ChoiceSerial++; run.Choices.Clear(); EnsureChoices(); Capture(); return true;
        }
        public void Damage(int incoming)
        {
            LastRetaliationDamage=0;
            if (incoming < 0) throw new ArgumentOutOfRangeException(nameof(incoming));
            if (incoming == 0 || !IsActive || Wave == null || Wave.Paused || run.PendingLevels > 0 || Wave.Elapsed - run.LastHitTime < 0.5) return;
            var armor = PermanentRules.Rank(run.PermanentSnapshot,"armor") + Passive("seal");
            run.Health -= Math.Max(1, incoming - armor); run.LastHitTime = Wave.Elapsed;
            LastRetaliationDamage=5*Passive("crown")*DamageMultiplier;
            if (run.Health <= 0) { state.Run = null; Wave = null; return; }
            Capture();
        }
        private void Finish()
        {
            if (!IsActive) return;
            // Only dropped XP is collected. Unspawned/live enemies grant nothing at timeout.
            foreach (var xp in run.Drops.OrderBy(x=>x.Key).Select(x=>x.Value)) AwardExperience(xp);
            run.Drops.Clear(); state.Profile.Gold = checked(state.Profile.Gold + definition.CompletionGold);
            run.World=null; // An earlier host checkpoint cannot describe the completed arena.
            if (definition.UnlockWeaponId != null && !state.Profile.UnlockedWeapons.Contains(definition.UnlockWeaponId))
                state.Profile.UnlockedWeapons.Add(definition.UnlockWeaponId);
            run.Health = run.MaxHealth; run.Stage = "between_rounds"; run.RoundNumber = definition.Number + 1;
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
            run.Passives = Inventory.Passives.ToDictionary(x=>x.Key,x=>x.Value);
            run.Wave = run.Stage == "normal" ? Arena?.Snapshot() : null;
            run.RoundFingerprint=definition.Fingerprint;
        }
        private int Passive(string id) => Inventory.Passives.TryGetValue(id,out var level) ? level : 0;
    }
}
