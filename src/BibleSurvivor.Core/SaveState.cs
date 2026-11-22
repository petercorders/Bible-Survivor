using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Text;

namespace BibleSurvivor.Core
{
    [DataContract]
    public sealed class ProfileState
    {
        [DataMember] public long Gold { get; set; }
        [DataMember] public Dictionary<string, int> Upgrades { get; set; } = new Dictionary<string, int>();
        [DataMember] public List<string> UnlockedWeapons { get; set; } = new List<string> { "bible", "jawbone", "sling" };
    }

    [DataContract]
    public sealed class RunState
    {
        [DataMember] public string RunId { get; set; } = "";
        [DataMember] public int RoundNumber { get; set; } = 1;
        [DataMember] public string Stage { get; set; } = "normal";
        [DataMember] public double Health { get; set; } = 100;
        [DataMember] public double MaxHealth { get; set; } = 100;
        [DataMember] public int Level { get; set; } = 1;
        [DataMember] public double Experience { get; set; }
        [DataMember] public int KillRemainder { get; set; }
        [DataMember] public double LastHitTime { get; set; } = -1;
        [DataMember] public Dictionary<string, int> PermanentSnapshot { get; set; } = new Dictionary<string, int>();
        [DataMember] public Dictionary<string, int> Weapons { get; set; } = new Dictionary<string, int>();
        [DataMember] public Dictionary<string, int> Passives { get; set; } = new Dictionary<string, int>();
        [DataMember] public WaveSnapshot? Wave { get; set; }
        [DataMember] public Dictionary<int, int> Drops { get; set; } = new Dictionary<int, int>();
        [DataMember] public int PendingLevels { get; set; }
        [DataMember] public int ChoiceSerial { get; set; } = 1;
        [DataMember] public List<string> Choices { get; set; } = new List<string>();
        [DataMember(EmitDefaultValue=false)] public BattleSnapshot? World {get;set;}
        // Optional additive schema-1 extension. Missing only on legacy R01 checkpoints.
        [DataMember(EmitDefaultValue=false)] public string? RoundFingerprint {get;set;}
        [DataMember(EmitDefaultValue=false)] public BossEncounterState? Boss {get;set;}
        [DataMember(EmitDefaultValue=false)] public Dictionary<string,string>? Forms {get;set;}
        [DataMember(EmitDefaultValue=false)] public BossEncounterState? CompletedBoss {get;set;}
        [DataMember(EmitDefaultValue=false)] public BossEncounterState? CompletedSecondBoss {get;set;}
        [DataMember(EmitDefaultValue=false)] public BossEncounterState? CompletedThirdBoss {get;set;}
        [DataMember(EmitDefaultValue=false)] public BossEncounterState? CompletedFourthBoss {get;set;}
        [DataMember(EmitDefaultValue=false)] public BossEncounterState? CompletedFifthBoss {get;set;}
        [DataMember(EmitDefaultValue=false)] public BossEncounterState? CompletedSixthBoss {get;set;}
        [DataMember(EmitDefaultValue=false)] public List<BossEncounterState>? LaterBossHistory {get;set;}
        [DataMember(EmitDefaultValue=false)] public double? NormalEndedAt {get;set;}
        [DataMember(EmitDefaultValue=false)] public bool RoundCompletionApplied {get;set;}
        [DataMember(EmitDefaultValue=false)] public int PreviousNormalRound {get;set;}
    }

    [DataContract]
    public sealed class OldTestamentClear
    {
        [DataMember(IsRequired=true)] public string RunId { get; set; } = "";
        [DataMember(IsRequired=true)] public long CompletedAtUnixMilliseconds { get; set; }
    }

    [DataContract]
    public sealed class GameSnapshot
    {
        [DataMember] public int SchemaVersion { get; set; } = SaveFormat.GenesisVersion;
        [DataMember] public long Revision { get; set; }
        [DataMember] public string LastOperationId { get; set; } = "";
        [DataMember] public ProfileState Profile { get; set; } = new ProfileState();
        [DataMember] public RunState? Run { get; set; }
        [DataMember(EmitDefaultValue=false)] public OldTestamentClear? OldTestamentClear { get; set; }
        public static GameSnapshot New() => new GameSnapshot();

        public void Validate()
        {
            if (SchemaVersion > SaveFormat.CurrentVersion) throw new UnsupportedSaveVersionException();
            if ((SchemaVersion != 1 && SchemaVersion != 2 && SchemaVersion != SaveFormat.CurrentVersion) || Revision < 0 || Profile == null || Profile.Gold < 0 || LastOperationId == null ||
                Profile.Upgrades == null || Profile.UnlockedWeapons == null || Profile.UnlockedWeapons.Distinct().Count() != Profile.UnlockedWeapons.Count ||
                Profile.UnlockedWeapons.Any(string.IsNullOrWhiteSpace)) throw new InvalidDataException("Invalid profile snapshot");
            PermanentRules.Validate(Profile.Upgrades);
            if (OldTestamentClear is { } clear &&
                (string.IsNullOrWhiteSpace(clear.RunId) || clear.CompletedAtUnixMilliseconds <= 0 ||
                 clear.CompletedAtUnixMilliseconds > DateTimeOffset.MaxValue.ToUnixTimeMilliseconds()))
                throw new InvalidDataException("Invalid Old Testament clear receipt");
            if (Run == null) return;
            var r10 = SchemaVersion == 3;
            if(r10 && !SaveFormat.SupportedChapterLifecycle(Run.RoundNumber,Run.Stage))
                throw new UnsupportedSaveVersionException();
            var normalRound=Run.Stage=="between_rounds"?Run.RoundNumber-1:Run.RoundNumber;
            var finalBossDefeated=normalRound==CampaignContent.OldTestamentFinalRound && Run.Boss?.Health==0;
            if (finalBossDefeated != (OldTestamentClear != null && OldTestamentClear.RunId == Run.RunId))
                throw new InvalidDataException("Old Testament clear receipt does not match the current run");
            var laterCampaign=r10&&normalRound>=LeviticusContent.GlobalRoundNumber;
            var expansion=r10&&normalRound>=21;
            var expansionBoss=expansion&&BossDefinition.Find(normalRound)!=null;
            var sixthHistory=laterCampaign&&normalRound>18;
            if(r10 && Run.RoundFingerprint!=(laterCampaign?CampaignContent.Round(normalRound):ExodusContent.Round(normalRound)).Fingerprint)
                throw new UnsupportedSaveVersionException();
            if(!laterCampaign&&Run.CompletedFifthBoss!=null||!sixthHistory&&Run.CompletedSixthBoss!=null||(!expansion||normalRound==21)&&Run.LaterBossHistory!=null)throw new UnsupportedSaveVersionException();
            if(!r10 && (Run.CompletedThirdBoss!=null||Run.CompletedFourthBoss!=null))throw new UnsupportedSaveVersionException();
            if (string.IsNullOrWhiteSpace(Run.RunId) || Run.RoundNumber < 1 || Run.RoundNumber > (r10?CampaignContent.LastInstalledRound+1:10) ||
                (Run.Stage=="normal" && Run.RoundNumber>(r10?CampaignContent.LastInstalledRound:SchemaVersion==1?2:9)) || (Run.Stage=="between_rounds" && Run.RoundNumber<2) ||
                (Run.RoundFingerprint!=null && string.IsNullOrWhiteSpace(Run.RoundFingerprint)) ||
                (Run.RoundFingerprint==null && (Run.Stage=="normal" ? Run.RoundNumber!=1 : Run.RoundNumber!=2)) ||
                (Run.Stage != "normal" && Run.Stage != "between_rounds" && Run.Stage!="boss_intro" && Run.Stage!="boss" && Run.Stage!="boss_reward" && Run.Stage!="boss_settlement" && Run.Stage!="round_settlement") || Run.Level < 1 || Run.Level > 10000 ||
                Run.KillRemainder < 0 || Run.KillRemainder > 4 || Run.Health <= 0 || Run.MaxHealth <= 0 || Run.Health > Run.MaxHealth ||
                !Finite(Run.Health) || !Finite(Run.MaxHealth) || !Finite(Run.Experience) || Run.Experience < 0 ||
                !Finite(Run.LastHitTime) || Run.LastHitTime < -1 || Run.Weapons == null || Run.Passives == null || Run.PermanentSnapshot == null)
                throw new InvalidDataException("Invalid run snapshot");
            if (Run.Drops == null || Run.Drops.Any(x=>x.Key <= 0 || x.Value < 0) || Run.PendingLevels < 0 || Run.ChoiceSerial < 1 ||
                Run.Choices == null || Run.Choices.Count > 3 || Run.Choices.Distinct().Count() != Run.Choices.Count ||
                (Run.PendingLevels == 0) != (Run.Choices.Count == 0)) throw new InvalidDataException("Invalid pending progression");
            PermanentRules.Validate(Run.PermanentSnapshot);
            if(Run.Wave!=null && (Run.Wave.Active==null || Run.Wave.Active.Any(x=>x==null)))
                throw new InvalidDataException("Invalid active enemy collection");
            var bossStage=Run.Stage=="boss_intro"||Run.Stage=="boss"||Run.Stage=="boss_reward"||Run.Stage=="boss_settlement"||(Run.Stage=="between_rounds"&&(Run.RoundNumber==4||Run.RoundNumber==7||Run.RoundNumber==10||r10&&(Run.RoundNumber==13||Run.RoundNumber==16||Run.RoundNumber==19||expansionBoss)));
            if(bossStage!=(Run.Boss!=null)||bossStage&&((r10?(Run.Boss!.EncounterRound!=12&&Run.Boss.EncounterRound!=15&&Run.Boss.EncounterRound!=18&&!expansionBoss):SchemaVersion!=2)||Run.RoundNumber!=Run.Boss!.EncounterRound+(Run.Stage=="between_rounds"?1:0)))throw new InvalidDataException("Invalid boss lifecycle");
            if(Run.Forms!=null&&((SchemaVersion!=2&&!r10)||Run.RoundNumber<3))throw new InvalidDataException("Evolution requires R03 schema2");
            Run.Boss?.Validate(Run);
            var r12=r10&&(Run.RoundNumber==12&&Run.Stage!="between_rounds"||Run.RoundNumber==13&&Run.Stage=="between_rounds");
            var r15=r10&&(Run.RoundNumber==15&&Run.Stage!="between_rounds"||Run.RoundNumber==16&&Run.Stage=="between_rounds");
            var r18=laterCampaign&&normalRound==18;
            var r06=Run.RoundNumber==6&&Run.Stage!="between_rounds"||Run.RoundNumber==7&&Run.Stage=="between_rounds";
            var r09=!r10&&(Run.RoundNumber==9&&Run.Stage!="between_rounds"||Run.RoundNumber==10);
            var roundBoss=BossDefinition.Find(normalRound);
            var chainWorld=roundBoss?.UsesChainProjectiles==true;
            var dashWorld=roundBoss?.UsesDash==true;
            var attackPeriod=roundBoss?.AttackPeriod??0;
            var fourthHistory=r10&&(normalRound>=13);
            var lateNormal=normalRound==4||normalRound==5||normalRound==7||normalRound==8||r10&&!r12&&!r15&&!r18&&!expansionBoss;
            if(lateNormal)
            {
                if((SchemaVersion!=2&&!r10)||Run.CompletedBoss==null||Run.PreviousNormalRound!=(r10?(laterCampaign?normalRound-1:normalRound==14?13:normalRound==13?12:normalRound==11?10:8):normalRound==8?7:normalRound==7?5:normalRound==5?4:0)||(Run.Stage!="normal"&&Run.Stage!="round_settlement"&&Run.Stage!="between_rounds")||
                    Run.RoundCompletionApplied!=(Run.Stage=="between_rounds")||(Run.Stage=="normal")!=(Run.NormalEndedAt==null)||
                    (Run.NormalEndedAt.HasValue&&(!Finite(Run.NormalEndedAt.Value)||Run.NormalEndedAt<0||Run.NormalEndedAt>180))||
                    (Run.Stage=="round_settlement"&&(Run.Drops.Count!=0||Run.PendingLevels==0))||(Run.Stage=="between_rounds"&&Run.PendingLevels!=0))
                    throw new InvalidDataException("Invalid later normal lifecycle");

            }
            else if(r06||r09||r12||r15||r18||expansionBoss)
            {
                if((SchemaVersion!=2&&!r12&&!r15&&!r18&&!expansionBoss)||Run.CompletedBoss==null||Run.PreviousNormalRound!=(expansionBoss?normalRound-1:r18?17:r15?14:r12?11:r09?8:5)||Run.RoundCompletionApplied||Run.NormalEndedAt!=null||
                    (Run.Stage!="normal"&&Run.Stage!="boss_intro"&&Run.Stage!="boss"&&Run.Stage!="boss_reward"&&Run.Stage!="boss_settlement"&&Run.Stage!="between_rounds")||
                    (Run.Boss!=null&&Run.Boss.EncounterRound!=(expansionBoss?normalRound:r18?18:r15?15:r12?12:r09?9:6)))throw new InvalidDataException("Invalid R06 history/lifecycle");
            }
            else if(Run.Stage=="round_settlement"||Run.CompletedBoss!=null||Run.NormalEndedAt!=null||Run.RoundCompletionApplied||Run.PreviousNormalRound!=0)throw new InvalidDataException("Normal receipt outside implemented rounds");
            if(lateNormal||r06||r09||r12||r15||r18||expansionBoss)
            {
                if(Run.CompletedBoss!.EncounterRound!=3||!Run.CompletedBoss.CompletionApplied)throw new InvalidDataException("Invalid or reordered completed boss history");
                Run.CompletedBoss.Validate(new RunState{RunId=Run.RunId,Stage="between_rounds",RoundNumber=4,Health=Run.MaxHealth,MaxHealth=Run.MaxHealth,
                    Weapons=Run.Weapons,Passives=Run.Passives,Forms=Run.Forms,LastHitTime=-1});
            }
            if(normalRound==7||normalRound==8||r09||r10)
            {
                if(Run.CompletedSecondBoss==null||Run.CompletedSecondBoss.EncounterRound!=6||!Run.CompletedSecondBoss.CompletionApplied)throw new InvalidDataException("Missing R06 completed history");
                Run.CompletedSecondBoss.Validate(new RunState{RunId=Run.RunId,Stage="between_rounds",RoundNumber=7,Health=Run.MaxHealth,MaxHealth=Run.MaxHealth,Weapons=Run.Weapons,Passives=Run.Passives,Forms=Run.Forms,LastHitTime=-1});
            }
            else if(Run.CompletedSecondBoss!=null)throw new InvalidDataException("Second boss history outside R07/R08");
            if(r10)
            {
                if(Run.CompletedThirdBoss==null||Run.CompletedThirdBoss.EncounterRound!=9||!Run.CompletedThirdBoss.CompletionApplied)throw new InvalidDataException("Missing R09 completed history");
                Run.CompletedThirdBoss.Validate(new RunState{RunId=Run.RunId,Stage="between_rounds",RoundNumber=10,Health=Run.MaxHealth,MaxHealth=Run.MaxHealth,Weapons=Run.Weapons,Passives=Run.Passives,Forms=Run.Forms,LastHitTime=-1});
            }
            if(fourthHistory)
            {
                if(Run.CompletedFourthBoss==null||Run.CompletedFourthBoss.EncounterRound!=12||!Run.CompletedFourthBoss.CompletionApplied)throw new InvalidDataException("Missing R12 completed history");
                Run.CompletedFourthBoss.Validate(new RunState{RunId=Run.RunId,Stage="between_rounds",RoundNumber=13,Health=Run.MaxHealth,MaxHealth=Run.MaxHealth,Weapons=Run.Weapons,Passives=Run.Passives,Forms=Run.Forms,LastHitTime=-1});
            }
            else if(Run.CompletedFourthBoss!=null)throw new UnsupportedSaveVersionException();
            if(laterCampaign)
            {
                if(Run.CompletedFifthBoss==null||Run.CompletedFifthBoss.EncounterRound!=15||!Run.CompletedFifthBoss.CompletionApplied)
                    throw new InvalidDataException("Missing R15 completed history");
                Run.CompletedFifthBoss.Validate(new RunState{RunId=Run.RunId,Stage="between_rounds",RoundNumber=16,
                    Health=Run.MaxHealth,MaxHealth=Run.MaxHealth,Weapons=Run.Weapons,Passives=Run.Passives,Forms=Run.Forms,LastHitTime=-1});
            }
            if(sixthHistory)
            {
                if(Run.CompletedSixthBoss==null||Run.CompletedSixthBoss.EncounterRound!=18||!Run.CompletedSixthBoss.CompletionApplied)
                    throw new InvalidDataException("Missing R18 completed history");
                Run.CompletedSixthBoss.Validate(new RunState{RunId=Run.RunId,Stage="between_rounds",RoundNumber=19,
                    Health=Run.MaxHealth,MaxHealth=Run.MaxHealth,Weapons=Run.Weapons,Passives=Run.Passives,Forms=Run.Forms,LastHitTime=-1});
            }
            if(expansion&&normalRound>21)
            {
                var expected=(normalRound-19)/3;
                var history=Run.LaterBossHistory;
                if(history==null||history.Count!=expected)
                    throw new InvalidDataException("Missing campaign boss history");
                for(var index=0;index<history.Count;index++)
                {
                    var record=history[index];
                    var round=21+index*3;
                    if(record==null||record.EncounterRound!=round||!record.CompletionApplied)
                        throw new InvalidDataException("Invalid campaign boss history order");
                    record.Validate(new RunState{RunId=Run.RunId,Stage="between_rounds",RoundNumber=round+1,
                        Health=Run.MaxHealth,MaxHealth=Run.MaxHealth,Weapons=Run.Weapons,Passives=Run.Passives,Forms=Run.Forms,LastHitTime=-1});
                }
            }
            if(chainWorld&&Run.Stage=="boss"&&Run.Boss!.BossElapsed>=attackPeriod&&Run.World==null)throw new InvalidDataException("R09 attack checkpoint requires hostile geometry");
            if(dashWorld&&Run.Stage=="boss"&&Run.Boss!.BossElapsed>0&&Run.World==null)throw new InvalidDataException("R12 combat requires live geometry");
            if(roundBoss is { } hazardRules && (hazardRules.UsesAimedShot || hazardRules.UsesDamageZone) &&
                Run.Stage=="boss" && Run.Boss!.BossElapsed>0 && Run.World==null)
                throw new InvalidDataException("Boss hazard combat requires live geometry");
            Run.World?.Validate();
            if(Run.World!=null)
            {
                if(chainWorld)
                {
                    if(Run.World.HostileProjectiles==null||Run.World.NextHostileId<1||Run.World.NextHostileId>1+3*Math.Floor(((Run.Boss?.BossElapsed??0)+1e-7)/attackPeriod)||
                        (Run.Stage!="boss"&&Run.World.HostileProjectiles.Count!=0)||Run.World.HostileProjectiles.Any(p=>p.BornAt<(Run.Boss?.NormalEndedAt??180)+2+attackPeriod-1e-7||Math.Abs((p.BornAt-(Run.Boss?.NormalEndedAt??180)-2)/attackPeriod-Math.Round((p.BornAt-(Run.Boss?.NormalEndedAt??180)-2)/attackPeriod))>1e-6))throw new InvalidDataException("Hostile checkpoint outside R09 attack lifecycle");
                }
                else if(Run.World.HostileProjectiles!=null||Run.World.NextHostileId!=0)throw new InvalidDataException("Hostile checkpoint outside R09");
                if(Run.Boss!=null && (Run.World.SchemaVersion!=Run.Boss.Rules.WorldVersion||Math.Abs(Run.World.Time-Run.Boss.CombatTime)>1e-7||
                    !Run.World.Enemies.Select(x=>x.Id).OrderBy(x=>x).SequenceEqual((Run.Boss.Health==0?Array.Empty<int>():
                        Run.Boss.Supports.Select(x=>x.Id).Append(Run.Boss.Rules.BossActorId)).OrderBy(x=>x))||
                    (Run.Boss.Health==0&&(Run.World.Projectiles.Count!=0||Run.World.Weapons.Count!=0))||
                    (Run.Stage=="boss_intro"&&(Run.World.Projectiles.Count!=0||Run.World.Weapons.Count!=0))||
                    Run.World.BossKills!=(Run.Boss.Health==0?1:0)))
                    throw new InvalidDataException("World checkpoint does not match boss lifecycle");
                if(Run.Boss!=null && (Run.World.Projectiles.Any(p=>p.HitIds.Any(id=>id<Run.Boss.Rules.BossActorId||id>=Run.Boss.NextActorId))||
                    Run.World.Weapons.Any(w=>w.OrbitHits.Any(hit=>hit.Id<Run.Boss.Rules.BossActorId||hit.Id>=Run.Boss.NextActorId))||
                    Run.Boss.Rules.SummonsSupport&&!Run.World.MonsterSpeeds.Any(x=>x.MonsterId==Run.Boss.Rules.SupportMonster&&x.Ratio==Run.Boss.Rules.SupportRatio)))
                    throw new InvalidDataException("Invalid boss target ledger or support speed");
                var lastActor=Run.Boss!=null?Run.Boss.NextActorId-1:Run.Wave?.Cursor??int.MaxValue;
                var firstActor=Run.Boss?.Rules.BossActorId??1;
                if(Run.World.Weapons.Any(w=>(w.FocusTargetId!=0&&(w.FocusTargetId<firstActor||w.FocusTargetId>lastActor))||
                    (w.PulseHitIds?.Any(id=>id<firstActor||id>lastActor)??false)||(w.FormHits?.Any(h=>h.Id<firstActor||h.Id>lastActor)??false)))
                    throw new InvalidDataException("Evolution ledger target outside issued actors");
                if(Run.World.Weapons.Any(w=>w.WeaponForm!=(Run.Forms!=null&&Run.Forms.TryGetValue(w.WeaponId,out var form)?form:null)))
                    throw new InvalidDataException("World weapon form does not match inventory");
                if(Run.Boss==null && (Run.World.SchemaVersion!=(normalRound>=4?roundBoss?.WorldVersion??3:Run.Forms==null?1:2)||Run.World.BossKills!=0))throw new InvalidDataException("Invalid normal world version");
                if((r06||chainWorld||dashWorld&&Run.Boss!=null)&&(Run.World.Charges==null||Run.World.Charges.Count!=0))throw new InvalidDataException("R06 cannot inherit D3 charges");
                if((normalRound==5||normalRound==7||normalRound==8||r10&&!r12)&&(Run.World.Charges?.Count!=0||Run.Wave?.Active.Any(x=>x.MonsterId=="d3"||x.SpawnedAt.HasValue)==true))throw new InvalidDataException("R04 charge leaked into R05");
                if((lateNormal||r12&&Run.Stage=="normal") && (Run.World.Charges==null||!Run.World.Charges.Select(x=>x.Id).OrderBy(x=>x).SequenceEqual((Run.Wave?.Active??new List<SpawnedEnemy>()).Where(x=>BattleWorld.UsesNormalCharge(normalRound,x.MonsterId)).Select(x=>x.Id).OrderBy(x=>x))||
                    (Run.NormalEndedAt.HasValue&&Math.Abs(Run.World.Time-Run.NormalEndedAt.Value)>1e-7)))throw new InvalidDataException("R04 charge world does not match round");
                if(Run.Wave!=null && (Run.Wave.Active==null || Math.Abs(Run.World.Time-Run.Wave.Elapsed)>1e-7 ||
                    !Run.World.Enemies.Select(x=>x.Id).OrderBy(x=>x).SequenceEqual(Run.Wave.Active.Select(x=>x.Id).OrderBy(x=>x))))
                    throw new InvalidDataException("World checkpoint does not match round clock or enemies");
                if(!Run.World.Drops.Select(x=>x.Id).OrderBy(x=>x).SequenceEqual(Run.Drops.Keys.OrderBy(x=>x)) ||
                    (Run.Stage=="between_rounds" && (Run.World.Enemies.Count!=0 || Run.World.Projectiles.Count!=0)))
                    throw new InvalidDataException("World checkpoint does not match round lifecycle");
            }
            if((lateNormal||r12||r15||r18||expansionBoss)&&Run.Wave!=null)
            {
                if(Run.Wave.Active.Any(x=>BattleWorld.UsesNormalCharge(normalRound,x.MonsterId)&&(!x.SpawnedAt.HasValue||!Finite(x.SpawnedAt.Value)||x.SpawnedAt<0||x.SpawnedAt>Run.Wave.Elapsed)))throw new InvalidDataException("Invalid D3 spawn time");
                if(Run.Wave.Active.Any(x=>BattleWorld.UsesNormalCharge(normalRound,x.MonsterId))&&Run.World==null)throw new InvalidDataException("D3 checkpoint requires geometry and charge state");
            }
            try
            {
                var inventory=EquipmentInventory.Restore(Run.Weapons, Run.Passives,Run.Forms); _ = new GrowthLedger(Run.Level, Run.Experience);
                if(Run.Choices.Any(x=>x!=null&&x.StartsWith(AdamSession.GrowthGoldChoicePrefix,StringComparison.Ordinal))&&
                    !Run.Choices.SequenceEqual(AdamSession.GrowthChoices(inventory,Profile.UnlockedWeapons,Run.ChoiceSerial)))throw new InvalidDataException("Invalid exhausted growth choice");
            }
            catch (ArgumentException error) { throw new InvalidDataException("Invalid progression", error); }
            if ((Run.Stage == "normal") != (Run.Wave != null)) throw new InvalidDataException("Invalid wave lifecycle");
            if(Run.Stage=="between_rounds" && (Run.Drops.Count!=0 || Run.Health!=Run.MaxHealth))
                throw new InvalidDataException("Invalid completion checkpoint");
            if(Run.Wave!=null && Run.LastHitTime>Run.Wave.Elapsed)
                throw new InvalidDataException("Last hit is ahead of round clock");
            if (Run.Wave != null && (!Finite(Run.Wave.Elapsed) || Run.Wave.Elapsed < 0 || Run.Wave.Elapsed >= 180))
                throw new InvalidDataException("Invalid running clock");
        }
        private static bool Finite(double value) => !double.IsNaN(value) && !double.IsInfinity(value);
    }

    public sealed class UnsupportedSaveVersionException : IOException
    { public UnsupportedSaveVersionException() : base("Save requires a newer game version; original preserved") { } }

    public sealed class SaveStore
    {
        private readonly object gate = new object();
        private readonly Action? beforeReplace;
        private readonly Action? beforeMigrationArchivePublish;
        public string Path { get; }
        public SaveStore(string path, Action? beforeReplace = null, Action? beforeMigrationArchivePublish = null)
        { Path = System.IO.Path.GetFullPath(path); this.beforeReplace = beforeReplace; this.beforeMigrationArchivePublish = beforeMigrationArchivePublish; }

        public GameSnapshot? Load()
        {
            lock (gate)
            {
                return LoadCore(out _);
            }
        }
        public GameSnapshot? LoadBackup() { lock (gate) return File.Exists(Path + ".bak") ? Read(Path + ".bak") : null; }

        private GameSnapshot? LoadCore(out int sourceVersion)
        {
            sourceVersion = SaveFormat.GenesisVersion;
            if (!File.Exists(Path)) return File.Exists(Path + ".bak") ? Read(Path + ".bak", out sourceVersion) : null;
            try { return Read(Path, out sourceVersion); }
            catch (UnsupportedSaveVersionException) { throw; }
            catch (Exception error) when (error is SerializationException || error is InvalidDataException || error is ArgumentException)
            {
                if (File.Exists(Path + ".bak")) return Read(Path + ".bak", out sourceVersion);
                throw new InvalidDataException("Save corrupt and no valid backup available", error);
            }
        }

        // Execute against a fresh deserialized aggregate; a failed write never mutates the last committed state.
        // Callers retain expectedRevision/operationId on uncertain results, and reload before new input.
        public GameSnapshot Transact(long expectedRevision,string operationId,Action<GameSnapshot> change)
        {
            lock(gate)
            {
                var snapshot=Load()??GameSnapshot.New();
                if(snapshot.LastOperationId==operationId) return snapshot;
                if(snapshot.Revision!=expectedRevision) throw new InvalidOperationException("Stale command revision");
                change(snapshot); Commit(snapshot,expectedRevision,operationId); return Load()!;
            }
        }

        public long Commit(GameSnapshot state, long expectedRevision, string operationId)
        {
            if (string.IsNullOrWhiteSpace(operationId)) throw new ArgumentException("Operation ID required");
            state.Validate();
            lock (gate)
            {
                Directory.CreateDirectory(System.IO.Path.GetDirectoryName(Path)!);
                // Single process host + OS lock across store instances. No silent last-writer-wins.
                using var fileLock = new FileStream(Path + ".lock", FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
                var current = LoadCore(out var sourceVersion);
                var revision = current?.Revision ?? 0;
                if (current != null && current.LastOperationId == operationId)
                {
                    if (Content(current) != Content(state)) throw new InvalidOperationException("Operation ID payload mismatch");
                    return current.Revision;
                }
                if (revision != expectedRevision) throw new InvalidOperationException("Stale save revision; reload before retry");
                var candidate = Decode(Encode(state));
                candidate.Revision = checked(revision + 1); candidate.LastOperationId = operationId;
                var staged = Path + ".tmp-" + Guid.NewGuid().ToString("N");
                try
                {
                    using (var output = new FileStream(staged, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                    { var bytes = Encode(candidate); output.Write(bytes, 0, bytes.Length); output.Flush(true); }
                    Read(staged);
                    if (sourceVersion == 1) SaveMigrationArchive.Ensure(Path, beforeMigrationArchivePublish);
                    beforeReplace?.Invoke();
                    if (File.Exists(Path))
                    {
                        // If primary is corrupt, keep the last valid backup instead of backing up corruption.
                        var primaryValid = true;
                        try { Read(Path); }
                        catch (Exception error) when (error is SerializationException || error is InvalidDataException || error is ArgumentException) { primaryValid = false; }
                        File.Replace(staged, Path, primaryValid ? Path + ".bak" : null);
                    }
                    else File.Move(staged, Path);
                    return candidate.Revision;
                }
                finally { if (File.Exists(staged)) File.Delete(staged); }
            }
        }

        public static GameSnapshot Copy(GameSnapshot state) => Decode(Encode(state));

        private static string Content(GameSnapshot state)
        {
            var copy = Decode(Encode(state)); copy.Revision = 0; copy.LastOperationId = "";
            return Encoding.UTF8.GetString(Encode(copy));
        }
        private static GameSnapshot Read(string path) => Read(path, out _);
        private static GameSnapshot Read(string path, out int sourceVersion)
        {
            var info = new FileInfo(path); if (info.Length > SaveFormat.MaximumBytes) throw new InvalidDataException("Save too large");
            var bytes = File.ReadAllBytes(path);
            sourceVersion = SaveFormat.Probe(bytes);
            return Decode(bytes);
        }
        private static byte[] Encode(GameSnapshot state)
        {
            using var stream = new MemoryStream();
            new DataContractJsonSerializer(typeof(GameSnapshot)).WriteObject(stream, state); return stream.ToArray();
        }
        private static GameSnapshot Decode(byte[] bytes)
        {
            var sourceVersion = SaveFormat.Probe(bytes); // Header before typed Run: future bodies must never trigger backup recovery.
            using var stream = new MemoryStream(bytes);
            var state = new DataContractJsonSerializer(typeof(GameSnapshot)).ReadObject(stream) as GameSnapshot;
            if (state == null || state.SchemaVersion != sourceVersion) throw new InvalidDataException("Missing or inconsistent save state");
            state.Validate();
            if (sourceVersion == 1) state.SchemaVersion = SaveFormat.GenesisVersion; // Memory migration only; Load never writes the original.
            return state;
        }
    }

    public static class PermanentRules
    {
        private static readonly Dictionary<string, int[]> Prices = new Dictionary<string, int[]> {
            ["attack"] = new[] {100,200,350,550,800}, ["health"] = new[] {80,160,280,440,640},
            ["armor"] = new[] {250,600,1100}, ["move"] = new[] {100,250,450}, ["pickup"] = new[] {60,120,220,360,540},
            ["growth"] = new[] {300,650,1100}, ["cooldown"] = new[] {500,1000,1800}, ["regen"] = new[] {200,450,800} };
        public static int Rank(IReadOnlyDictionary<string,int> values, string key) => values.TryGetValue(key, out var rank) ? rank : 0;
        internal static void Validate(Dictionary<string,int> values)
        {
            if (values.Any(x => !Prices.ContainsKey(x.Key) || x.Value < 1 || x.Value > Prices[x.Key].Length))
                throw new InvalidDataException("Unknown or invalid permanent upgrade");
            // Prerequisites are checked at purchase; a restored profile must satisfy them too.
            foreach (var key in values.Keys) if (!Unlocked(values, key)) throw new InvalidDataException("Missing upgrade prerequisite");
        }
        private static bool Unlocked(IReadOnlyDictionary<string,int> values, string key) =>
            key == "cooldown" ? Rank(values,"attack") >= 3 : key == "armor" ? Rank(values,"health") >= 2 :
            key == "regen" ? Rank(values,"armor") >= 1 : key == "move" ? Rank(values,"pickup") >= 2 :
            key == "growth" ? Rank(values,"move") >= 1 : true;
        public static int NextPrice(ProfileState profile, string key)
        {
            if (!Prices.TryGetValue(key,out var prices) || !Unlocked(profile.Upgrades,key)) throw new InvalidOperationException("Upgrade locked");
            var rank=Rank(profile.Upgrades,key);
            if (rank >= prices.Length) throw new InvalidOperationException("Upgrade maxed"); return prices[rank];
        }
        public static void Purchase(ProfileState profile, string key)
        {
            if (!Prices.TryGetValue(key, out var prices) || !Unlocked(profile.Upgrades, key)) throw new InvalidOperationException("Upgrade locked");
            var rank = Rank(profile.Upgrades,key);
            if (rank == prices.Length || profile.Gold < prices[rank]) throw new InvalidOperationException("Upgrade unavailable");
            profile.Gold -= prices[rank]; profile.Upgrades[key] = rank + 1;
        }
        public static long RefundAll(ProfileState profile)
        {
            Validate(profile.Upgrades);
            long refund=0;
            foreach(var node in profile.Upgrades) for(var rank=0;rank<node.Value;rank++) refund=checked(refund+Prices[node.Key][rank]);
            var balance=checked(profile.Gold+refund);profile.Upgrades.Clear();profile.Gold=balance;return refund;
        }
    }
}
