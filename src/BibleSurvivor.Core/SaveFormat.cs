using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Runtime.Serialization.Json;
using System.Xml;

namespace BibleSurvivor.Core
{
    // Version probing deliberately does not construct RunState. A newer writer may have a
    // completely different Run representation; that must not be mistaken for recoverable corruption.
    public static class SaveFormat
    {
        public const int CurrentVersion = 4;
        public const int CampaignVersion = 3;
        public const int GenesisVersion = 2;
        private const int CurrentWorldVersion = BattleSnapshot.ExpandedVersion;
        internal const int MaximumBytes = 4 * 1024 * 1024;
        private static readonly HashSet<string> EnvelopeFields = new HashSet<string> {
            "SchemaVersion", "Revision", "LastOperationId", "Profile", "Run", "OldTestamentClear" };
        private static readonly HashSet<string> RunFields = new HashSet<string> {
            "RunId", "RoundNumber", "Stage", "Health", "MaxHealth", "Level", "Experience", "KillRemainder",
            "LastHitTime", "PermanentSnapshot", "Weapons", "Passives", "Wave", "Drops", "PendingLevels",
            "ChoiceSerial", "Choices", "World", "RoundFingerprint", "Boss", "Forms", "CompletedBoss", "CompletedSecondBoss", "CompletedThirdBoss", "CompletedFourthBoss", "CompletedFifthBoss", "CompletedSixthBoss", "LaterBossHistory", "NormalEndedAt", "RoundCompletionApplied", "PreviousNormalRound" };
        // Explicit serialized shapes avoid reflection/code generation in the Unity/IL2CPP reader.
        // Dictionary keys are data inside Key/Value entries, not names of DTO members.
        private sealed class ContentShape
        {
            internal Dictionary<string, ContentShape>? Members;
            internal ContentShape? Item;
        }
        private static readonly ContentShape Value = new ContentShape();
        private static readonly ContentShape LegacyContent = BuildSupportedContent(false);
        private static readonly ContentShape SupportedContent = BuildSupportedContent(true);

        private static ContentShape Object(string values, params (string Name, ContentShape Shape)[] children)
        {
            var members = new Dictionary<string, ContentShape>(StringComparer.Ordinal);
            foreach (var name in values.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries)) members.Add(name, Value);
            foreach (var child in children) members[child.Name] = child.Shape;
            return new ContentShape { Members = members };
        }
        private static ContentShape Array(ContentShape item) => new ContentShape { Item = item };
        private static ContentShape BuildSupportedContent(bool skillTree)
        {
            var integerMap = Array(Object("Key Value"));
            var profile = Object("Gold UnlockedWeapons", ("Upgrades", integerMap));
            if (skillTree)
            {
                profile.Members!.Add("PaidRanks", Array(Object("Key Value")));
                profile.Members.Add("HighestSettledRound", Value);
                profile.Members.Add("EvolvedWeapons", Value);
                profile.Members.Add("LastPermanentSkillReceipt",
                    Object("OperationId ExpectedRevision RequestDigest ResultingRevision GoldDelta"));
            }
            var wave = Object("Elapsed Cursor Paused Expired", ("Active", Array(Object("Id MonsterId Health ContactDamage RawExperience SpawnedAt"))));
            var entity = Object("Id X Y");
            var projectile = Object("Id X Y VelocityX VelocityY Lifetime Damage RemainingPierce Radius HitIds");
            var weapon = Object("WeaponId WeaponForm NextCycle ActiveUntil ActiveSince NextBurst BurstsRemaining PendingBursts NextPulse PulseUntil FocusTargetId PulseHitIds",
                ("OrbitHits", Array(Object("Id At"))),("FormHits",Array(Object("Id At"))));
            if (skillTree)
            {
                weapon.Members!.Add("StrengthenedFirstPulse", Value);
                weapon.Members.Add("FrozenPulseRadius", Value);
                weapon.Members.Add("LastJawboneReach", Value);
                weapon.Members.Add("LastJawboneHalfAngle", Value);
                weapon.Members.Add("LastJawboneDirectionX", Value);
                weapon.Members.Add("LastJawboneDirectionY", Value);
                weapon.Members.Add("JawboneCycleAmount", Value);
                weapon.Members.Add("NextJawboneShockAt", Value);
                weapon.Members.Add("JawboneMarks", Object("LastEliteId ConsecutiveHits", ("Marks", Array(Object("Id At")))));
                weapon.Members.Add("FrozenOrbitCount", Value);
                weapon.Members.Add("FrozenOrbitRadius", Value);
                weapon.Members.Add("FrozenOrbitHitRadius", Value);
                weapon.Members.Add("StarterCycle", Value);
                weapon.Members.Add("OriginalHitLedger", Object("LatestCycle HitCycles LastTargetId ConsecutiveHits",
                    ("Marks", Array(Object("Id At")))));
                projectile.Members!.Add("Cycle", Value);
                projectile.Members.Add("OriginalBaseDamage", Value);
                projectile.Members.Add("SharedShotHitBonuses", Value);
                projectile.Members.Add("TravelledDistance", Value);
                projectile.Members.Add("SlingSlot", Value);
                projectile.Members.Add("SlingEvolved", Value);
                projectile.Members.Add("SlingBoostedVolley", Value);
                projectile.Members.Add("SlingGuaranteedCritical", Value);
                weapon.Members.Add("LaunchedSlingInCycle", Value);
                weapon.Members.Add("SlingCombat", Object("Slot LastTargetId LastOriginalId LastTargetKilled StrengthenedVolley NextRicochetBurstAt NextReturnAt NextRuptureAt NormalHits NextStormAt StormSecondAt StormSecondDamage BossHits NextBossBoostAt BossBoostPending NextShardsAt",
                    ("VolleyHits", Array(Object("Cycle TargetId"))),
                    ("Support", Array(Object("Kind X Y DirectionX DirectionY StartedAt Remaining Damage TargetId ExcludedId")))));
                weapon.Members.Add("FrozenOrbitDamage", Value);
                weapon.Members.Add("FrozenOrbitBaseDamage", Value);
                weapon.Members.Add("BibleMotionCycle", Value);
                weapon.Members.Add("BibleCrowdX", Value);
                weapon.Members.Add("BibleCrowdY", Value);
                weapon.Members.Add("BibleDuration", Object("NativeDuration Extensions NormalKills EliteUsed"));
            }
            var tuning = Object("PlayerSpeed CameraHalfWidth CameraHalfHeight SpawnMargin PlayerRadius EnemyRadius BibleOrbitRadius BookRadius JawboneRange SlingSpeed SlingLifetime PickupRadius MaxProjectiles");
            var world = Object("SchemaVersion Time Remainder NextHostileId PlayerX PlayerY FacingX FacingY RandomState NextProjectileId ShotsFired Hits Kills BossKills Collected DamageTaken",
                ("Tuning", tuning), ("Enemies", Array(entity)), ("Drops", Array(entity)),
                ("Projectiles", Array(projectile)), ("Weapons", Array(weapon)),
                ("MonsterSpeeds", Array(Object("MonsterId Ratio"))),("HostileProjectiles",Array(Object("Version Id BornAt StartX StartY X Y DirectionX DirectionY Lifetime"))),("Charges",Array(Object("Version Id Phase NextAt Until TargetX TargetY DirectionX DirectionY"))));
            if (skillTree)
            {
                world.Members!.Add("OriginalSuppressions", Value);
                world.Members.Add("SupportSuppressions", Value);
                world.Members.Add("NextHostileEraseAt", Value);
                world.Members.Add("ChariotDeflectRandom", Value);
                world.Members.Add("LionDeflectRandom", Value);
                world.Members.Add("ChariotDeflectionAttempts", Array(Object("PathId ProjectileId")));
                world.Members!.Add("Lightning", Object("Evolved WaitingForTarget Cycle NextStrikeId NextCycle NextCurrentAt NextGroundLinkAt NextGroundBurstAt NextEvolutionForkAt NextEliteChainMarkAt EliteChainDamageUntil",
                    ("RecentStrikeHits", Array(Object("Id At"))), ("CurrentMarks", Array(Object("Id At"))),
                    ("GroundCurrents", Array(Object("Id Cycle X Y StartedAt EndsAt NextPulse Radius Damage"))),
                    ("Pending", Array(Object("Id Cycle At X Y Damage Radius AssignedTargetId Aftershock VisitedIds CanRetarget LongerChain UsesFarthestReuse PredecessorTargetId PredecessorX PredecessorY OriginalBaseDamage IsolatedDamage HighHealthDamage RepeatStrikeDamage WiderLastStrike EliteThunderOnHit EliteThunder CurrentRanks GroundRanks EvolutionRanks")))));
                world.Members.Add("WildernessVoice", Object("Evolved Cycle NextCycle ElitePulseSequence FinishHits FinishReady NextFinishAt",
                    ("EliteSeries", Array(Object("TargetId LastPulse Consecutive MarkUntil"))),
                    ("Active", Array(Object("Id StartedAt Duration Damage Reach HalfAngle PrimaryPulses EchoesFired FirstDelay KnockbackBonus CloseKnockbackBonus ShortenedPrimaryInterval EchoRadiusBonus FirstPulseDamage FinalPulseKnockback FourthCycleFinalDamage CenteredDamage EdgeKnockback LockedFacingX LockedFacingY DamageLevel UncappedDamage HasExtraEcho OriginalBaseDamage InitialFacingX InitialFacingY EvolvedCloseKnockback TracksEliteSeries CountsHitsForFinish StrengthensRepeatedTargets",
                        ("OriginalTargetCounts", Array(Object("TargetId Count"))))))));
                world.Members.Add("FirePillar", Object("Evolved WaitingForTarget SuppressedOriginal NextId NextCycle LastStep Cycle NextClosingEmberAt NextClosingAshAt NextCloseRangeAt NextEliteExtensionAt NormalBurstKills NextNormalBurstAt NextEliteReignitionAt NextCyclePulseAt SparkHitCount NextSparkAt NextLinkAt RadiusKillCount RadiusBoostUntil NextRadiusBoostAt NextGroupRushAt NextMovingFireAt",
                    ("Spark", Object("TargetId X Y StartedAt LastStepAt ParentDamage MakesFire RegionShapeBonus RegionDurationBonus")),
                    ("Regions", Array(Object("Id Ember SupportKind X Y StartedAt EndsAt NextHit Damage Radius TargetId NextTargetAt TravelSinceEmber FirstDelay Cycle ShortenedPrimaryInterval BaseDamage IncreasedCoreDamage IncreasedIgnitionDamage OriginalBaseDamage RepeatedHeat NativeDuration EliteExtensionUsed AdaptiveHeat PursuedGroup RushUntil NextRimHitAt TravelSinceMovingFire",
                        ("Contacts", Array(Object("TargetId LastDueAt ConsecutiveHits"))),
                        ("HeatTimers", Array(Object("TargetId EnteredAt LastHitAt"))),
                        ("DurationProgress", Object("NativeDuration Extensions NormalKills EliteUsed")))))));
                world.Members.Add("MosesStaff", Object("Evolved Cycle NextWaveId NextCycle NextReturnAt NextEndPillarAt NextEliteSplashAt NextResidualCrossAt NextResidualEndAt NextWaterScreenAt NextEncirclingWaveAt NextHostileEraseAt HostileEraseRandomState LastCycleStartedAt LastPlayerHurtObservedAt NextHurtAdvanceAt NextEvolvedReturnAt NextEliteKillLaneAt",
                    ("EliteKillLane", Object("SourceWaveId Cycle VictimId StartedAt EndsAt NextPulseAt X Y DirectionX DirectionY Width Damage")),
                    ("ReturnWaves", Array(Object("SourceWaveId Cycle StartedAt LastStepAt X Y DirectionX DirectionY Width Damage Distance HitIds FromEvolvedLane TravelRange TravelSpeed"))),
                    ("Waves", Array(Object("Id Cycle EmittedAt LastStepAt OriginX OriginY DirectionX DirectionY Damage Width Range Speed Distance MakesLane LaneStartedAt LaneTicks HitIds IncreasedKnockback WidenedLane EmpoweredLaneEdge ExtendedLane WidenedSupportShape WiderDamageRegion ResidualDurationBonus OriginalBaseDamage OriginalHitCount TechniqueRanks TurnAtDistance TurnDirectionX TurnDirectionY ResidualLaneRanks ResidualLaneStartedAt ResidualLaneTicks ResidualLaneWidthBonus ResidualLaneDurationBonus ResidualLaneEdgeIds ResidualLaneOverlapIds ErasesHostileProjectiles HostileEraseAttemptIds ReturnsEvolvedLane LeavesEliteKillLane SharedShotHitBonuses")))));
                world.Members.Add("Cross", Object("Evolved Cycle NextShotId NextCycle NextEliteThornAt NextEliteLightAt ExtraPassesEmitted NextExtraPassAt NextRecoveryWaveAt NextIntersectionAt NextTurnSpinAt NextReturnSpinAt",
                    ("RecentTrajectories", Array(Object("ShotId StartX StartY EndX EndY FromAt ToAt StartsAtLaunch"))),
                    ("Shots", Array(Object("Id Cycle EmittedAt SimulatedAt StartX StartY X Y DirectionX DirectionY Damage Radius Range Speed Distance ReturnStartedAt EndedAt OutboundHits ReturnHits LeavesThorns NextThornAt TrainedOutboundSpeed TrainedReturnSpeed IncreasedLegBudget IncreasedSharedLegBudget ExtendedThorns ShortenedRehitInterval WidenedSupportShape FirstOutboundDamage ReturnDamage CurvedReturn PreviouslyHitReturnDamage MultipleHitDamage ExpandedFirstReturnBudget LaunchIndex DamageLevel ResidualDurationBonus OriginalBaseDamage LeavesAfterglow ExtendedAfterglow BoostReturningAfterglow NextAfterglowAt IsExtraPass EmpoweredThornOverlap WiderThornRadius WavesOnRecovery WiderDamageRegion SpinsAtTurn SpinEndsAt SpinsAtReturn SharedShotHitBonuses SharedReturnSpeed IncreasedEvolvedLegBudget LongerReturnLifetime",
                        ("HitTargets", Array(Object("Id Outbound Returned LastHitAt"))),
                        ("Thorns", Array(Object("StartX StartY EndX EndY BornAt Returning"))),
                        ("Afterglow", Array(Object("StartX StartY EndX EndY BornAt Returning"))))))));
                world.Members.Add("ElijahChariot", Object("Evolved Cycle NextPathId NextCycle SuppressedOriginals NextSparkAt NextEndExplosionAt NextWindAt NextGuaranteedWindAt NextDeflectAt IntersectionCheckedAt NextCrossingAt NextEndFlameAt NextProcessionAt ProcessionUntil NextReverseAt NextCrossChildAt",
                    ("SupportChariots", Array(Object("Kind SourcePathId Cycle StartX StartY DirectionX DirectionY MoveAt Range Speed Width Damage Distance HitIds"))),
                    ("Winds", Array(Object("SourcePathId Cycle StartX StartY DirectionX DirectionY MoveAt Range Speed Damage Distance HitIds Guaranteed Wider WiderHitShape TurnDirectionX TurnDirectionY"))),
                    ("Spark", Object("SourcePathId Cycle TargetId X Y Damage SpawnedAt SteppedAt")),
                    ("Paths", Array(Object("Id Cycle StartX StartY DirectionX DirectionY MoveAt Range Speed Width Damage Distance TrailTicksFired BodyHitCount BodyHitIds WhirlwindCandidateIds FirstDelay SuppressedTrail ForwardKnockback WiderWhirlwind ExtendedTrail WidenedSupportShape WiderDamageRegion StaggerIndex ResidualDurationBonus OriginalBaseDamage MakesOriginFire OriginFireTicks MakesBasicTrail WidensEvolvedTrail ExplodesAtEnd RewardsBodyHits FollowsWithWind GuaranteesWind WindChecked WidensWind WidensWindHitShape TracksCrossing MakesEndFlame TurnsMidway MidpointTurnResolved TurnDirectionX TurnDirectionY ReversesAtEnd HasCrossingChild SharedShotHitBonuses")))));
                world.Members.Add("DanielLion", Object("Evolved LastStepAt NextRoarAt GuardianHits KillLeapReadyAt PackCycle NextPackLeapAt NextPatrolAt NextPatrolLeapAt NextProtectionAt NextGuardAt GuardUntil NextRoarDeflectAt",
                    ("Lions", Array(Object("Index X Y TargetId Returning NextClawAt WaitingForTarget ExtendedLeash FirstClawPhaseResolved NextPackRoarAt ClawHasteUntil NextPounceAt PatrolUntil PatrolAngle ReturnHitIds",
                        ("Pounce", Object("StartedAt StartX StartY EndX EndY")),
                        ("PatrolLeap", Object("StartedAt StartX StartY EndX EndY"))))),
                    ("TargetHistory", Array(Object("TargetId MarkUntil", ("RecentClaws", Array(Object("LionIndex At")))))),
                    ("Leap", Object("At OriginX OriginY Damage VisitedIds EmpoweredDamage FollowupOnKill"))));
            }
            var reward = Object("RewardId PolicyId EligibleEvolutions Applied UnlockedWeapons SelectedId PromotedFrom GoldApplied",("Weapons",integerMap),("Passives",integerMap),("Forms",integerMap),("Candidates",Array(Object("Id Kind ItemId Level Gold"))));
            if (skillTree) reward.Members!.Add("RulesVersion", Value);
            var boss = Object("Version EncounterId NormalEndedAt IntroElapsed BossElapsed Health Paused NextAttackAt Telegraphing TargetX TargetY ThresholdUsed Summoned NextActorId BossGoldApplied CompletionApplied RoundNumber SummonMask",
                ("AimedShot",Object("OriginX OriginY DirectionX DirectionY")),("ChariotCharge",Object("Phase DirectionX DirectionY Until")),("ChainDirections",Array(Object("X Y"))),("Supports",Array(Object("Id MonsterId Health ContactDamage RawExperience SpawnedAt"))),("Reward",reward));
            var run = Object(string.Join(" ", RunFields), ("PermanentSnapshot", integerMap),
                ("Weapons", integerMap), ("Passives", integerMap), ("Drops", integerMap), ("Wave", wave), ("World", world), ("Boss",boss),("CompletedBoss",boss),("CompletedSecondBoss",boss),("CompletedThirdBoss",boss),("CompletedFourthBoss",boss),("CompletedFifthBoss",boss),("CompletedSixthBoss",boss),("LaterBossHistory",Array(boss)),("Forms",integerMap));
            if (skillTree)
            {
                run.Members!.Add("CampaignLayout", Value);
                run.Members.Add("CombatRulesVersion", Value);
                run.Members.Add("PermanentCatalogVersion", Value);
                run.Members.Add("GrowthChoiceUnlockedWeapons", Value);
                run.Members.Add("PermanentActions", Object("GemsCollected LastGemAt LastContactAt StationarySince HeadingSince HeadingX HeadingY"));
                run.Members.Add("PermanentRecovery", Object("NormalKills NextEliteAt NextBossSupportAt NextLowHealthAt LastEventAt",
                    ("ImmediateHeals", Array(Object("At Amount")))));
                run.Members.Add("CriticalRandomStates", integerMap);
                run.Members.Add("FirstBibleAttackUsed", Value);
                run.Members.Add("FirstSustainedUsed", Value);
                run.Members.Add("NoHitCarrySeconds", Value);
                run.Members.Add("LastTempoEliteKillAt", Value);
                run.Members.Add("TempoOpeningUsed", Value);
                run.Members.Add("BossEntryVoiceCycle", Value);
                run.Members.Add("BossEntryJawboneCycle", Value);
                run.Members.Add("BossEntrySlingCycle", Value);
                run.Members.Add("BossEntrySlingNormalHits", Value);
                run.Members.Add("BossEntryLionPackCycle", Value);
                run.Members.Add("BossEntryLionNextPackLeapAt", Value);
                run.Members.Add("BossEntryLightningCycle", Value);
                run.Members.Add("BossEntryLightningStrikes", Value);
                run.Members.Add("BossEntryFireCycle", Value);
                run.Members.Add("BossEntryCrossCycle", Value);
                run.Members.Add("BossEntryStaffCycle", Value);
                run.Members.Add("BossEntryStaffWaves", Value);
                run.Members.Add("BossEntryChariot", Object("Cycle NextPathId SuppressedOriginals NextProcessionAt ProcessionUntil"));
            }
            return Object(string.Join(" ", EnvelopeFields), ("Profile", profile), ("Run", run),
                ("OldTestamentClear", Object("RunId CompletedAtUnixMilliseconds")));
        }

        private static void CheckSupportedContent(XmlElement element, ContentShape shape)
        {
            // Leave null/primitive type validation and required-member semantics to the typed DTO
            // and Validate; this gate prevents silent loss of unknown members on supported DTOs.
            if (shape.Members != null && element.GetAttribute("type") == "object")
            {
                var names = new HashSet<string>(StringComparer.Ordinal);
                foreach (var member in element.ChildNodes.OfType<XmlElement>())
                {
                    if (!names.Add(member.Name)) throw new InvalidDataException("Duplicate nested save field");
                    if (!shape.Members.TryGetValue(member.Name, out var child)) throw new UnsupportedSaveVersionException();
                    CheckSupportedContent(member, child);
                }
            }
            else if (shape.Item != null && element.GetAttribute("type") == "array")
                foreach (var item in element.ChildNodes.OfType<XmlElement>()) CheckSupportedContent(item, shape.Item);
        }

        private static void CheckWorldVersion(XmlElement? world)
        {
            // World has an independent version domain. Protect its future payload in either
            // supported envelope version before typed DTO validation can recover an old backup.
            if (world == null || world.GetAttribute("type") != "object") return;
            var versions = world.ChildNodes.OfType<XmlElement>().Where(x => x.Name == "SchemaVersion").ToArray();
            if (versions.Length != 1 || versions[0].GetAttribute("type") != "number")
                throw new InvalidDataException("World requires exactly one positive integer SchemaVersion");
            var number = versions[0].InnerText;
            if (number.Length == 0 || number.Any(c => c < '0' || c > '9') || number.All(c => c == '0'))
                throw new InvalidDataException("World requires exactly one positive integer SchemaVersion");
            if (!int.TryParse(number, NumberStyles.None, CultureInfo.InvariantCulture, out var worldVersion) || worldVersion > CurrentWorldVersion)
                throw new UnsupportedSaveVersionException();
            var charges=world.ChildNodes.OfType<XmlElement>().FirstOrDefault(x=>x.Name=="Charges");
            if(charges?.GetAttribute("type")=="array")foreach(var charge in charges.ChildNodes.OfType<XmlElement>())
            {
                var version=charge.ChildNodes.OfType<XmlElement>().FirstOrDefault(x=>x.Name=="Version");
                if(version?.GetAttribute("type")=="number"&&(!int.TryParse(version.InnerText,out var cv)||cv>1))throw new UnsupportedSaveVersionException();
                var phase=charge.ChildNodes.OfType<XmlElement>().FirstOrDefault(x=>x.Name=="Phase");
                if(phase?.GetAttribute("type")=="string"&&phase.InnerText!="cooldown"&&phase.InnerText!="telegraph"&&phase.InnerText!="charge")throw new UnsupportedSaveVersionException();
            }
        }

        internal static int Probe(byte[] bytes)
        {
            if (bytes.Length > MaximumBytes) throw new InvalidDataException("Save too large");
            var document = new XmlDocument { XmlResolver = null };
            try
            {
                using var reader = JsonReaderWriterFactory.CreateJsonReader(bytes, XmlDictionaryReaderQuotas.Max);
                document.Load(reader);
            }
            catch (Exception error) when (error is XmlException || error is SerializationException || error is ArgumentException)
            { throw new InvalidDataException("Invalid save JSON header", error); }
            var root = document.DocumentElement;
            if (root == null || root.GetAttribute("type") != "object") throw new InvalidDataException("Save envelope must be an object");
            var fields = root.ChildNodes.OfType<XmlElement>().ToArray();
            var versions = fields.Where(x => x.Name == "SchemaVersion").ToArray();
            if (versions.Length != 1 || versions[0].GetAttribute("type") != "number")
                throw new InvalidDataException("Save requires exactly one positive integer SchemaVersion");
            var number = versions[0].InnerText;
            if (number.Length == 0 || number.Any(c => c < '0' || c > '9') || number.All(c => c == '0'))
                throw new InvalidDataException("Save requires exactly one positive integer SchemaVersion");
            // A positive integer larger than Int32 is still a future version, not corrupt data.
            // Never turn overflow into permission to recover and overwrite an older backup.
            if (!int.TryParse(number, NumberStyles.None, CultureInfo.InvariantCulture, out var version) || version > CurrentVersion)
                throw new UnsupportedSaveVersionException();
            if (fields.Select(x => x.Name).Distinct().Count() != fields.Length)
                throw new InvalidDataException("Duplicate save envelope field");
            var run = fields.SingleOrDefault(x => x.Name == "Run");
            var skillTree = version == CurrentVersion;
            if (skillTree) CheckSupportedContent(root, SupportedContent);
            var layoutVersion = version;
            var expandedCombat = false;
            if (run != null && run.GetAttribute("type") == "object")
            {
                var members = run.ChildNodes.OfType<XmlElement>().ToArray();
                if (members.Select(x => x.Name).Distinct().Count() != members.Length)
                    throw new InvalidDataException("Duplicate run field for this save format");
                var world = members.SingleOrDefault(x => x.Name == "World");
                CheckWorldVersion(world);
                if (skillTree)
                {
                    var layout = members.SingleOrDefault(member => member.Name == "CampaignLayout");
                    var rules = members.SingleOrDefault(member => member.Name == "CombatRulesVersion");
                    if (rules?.GetAttribute("type") != "number" || rules.InnerText.Length == 0 ||
                        rules.InnerText.Any(character => character < '0' || character > '9') ||
                        rules.InnerText.All(character => character == '0') ||
                        layout?.GetAttribute("type") != "string" || string.IsNullOrWhiteSpace(layout.InnerText))
                        throw new InvalidDataException("Invalid permanent skill run contract fields");
                    if (rules.InnerText != "1" && rules.InnerText != "2")
                        throw new UnsupportedSaveVersionException();
                    expandedCombat = rules.InnerText == "2";
                    if (expandedCombat)
                    {
                        var catalog = members.SingleOrDefault(member => member.Name == "PermanentCatalogVersion");
                        if (catalog?.GetAttribute("type") != "number")
                            throw new InvalidDataException("Expanded combat requires a permanent skill catalog version");
                        if (!int.TryParse(catalog.InnerText, NumberStyles.None, CultureInfo.InvariantCulture, out var catalogVersion) ||
                            !PermanentSkillCatalog.SupportsVersion(catalogVersion))
                            throw new UnsupportedSaveVersionException();
                    }
                    layoutVersion = layout?.InnerText switch
                    {
                        "genesis" => GenesisVersion,
                        "campaign" => CampaignVersion,
                        _ => throw new UnsupportedSaveVersionException(),
                    };
                }
                var worldVersion=world?.ChildNodes.OfType<XmlElement>().SingleOrDefault(x=>x.Name=="SchemaVersion")?.InnerText;
                if (!expandedCombat && worldVersion == BattleSnapshot.ExpandedVersion.ToString(CultureInfo.InvariantCulture))
                    throw new UnsupportedSaveVersionException();
                var r12=layoutVersion==CampaignVersion&&(members.Any(x=>x.Name=="RoundNumber"&&x.InnerText=="12")||
                    members.Any(x=>x.Name=="RoundNumber"&&x.InnerText=="13")&&members.Any(x=>x.Name=="Stage"&&x.InnerText=="between_rounds"));
                if(worldVersion=="5"&&!r12)
                {
                    var roundText=members.SingleOrDefault(x=>x.Name=="RoundNumber")?.InnerText;
                    var betweenRounds=members.Any(x=>x.Name=="Stage"&&x.InnerText=="between_rounds");
                    if(layoutVersion!=CampaignVersion||!int.TryParse(roundText,out var worldRound)||
                        BossDefinition.Find(worldRound-(betweenRounds?1:0))?.WorldVersion!=5)
                        throw new UnsupportedSaveVersionException();
                }
                if (!skillTree && members.Any(x => !RunFields.Contains(x.Name)))
                    RejectUnsupportedContent(version, "Unknown run field for this save format");
                var boss=members.SingleOrDefault(x=>x.Name=="Boss");
                if(!r12&&boss?.ChildNodes.OfType<XmlElement>().Any(x=>x.Name=="RoundNumber"&&x.InnerText=="12")==true)throw new UnsupportedSaveVersionException();
                var r15=layoutVersion==CampaignVersion&&(members.Any(x=>x.Name=="RoundNumber"&&x.InnerText=="15")&&members.Any(x=>x.Name=="Stage"&&x.InnerText!="between_rounds")||members.Any(x=>x.Name=="RoundNumber"&&x.InnerText=="16")&&members.Any(x=>x.Name=="Stage"&&x.InnerText=="between_rounds"));
                if(!r15&&boss?.ChildNodes.OfType<XmlElement>().Any(x=>x.Name=="RoundNumber"&&x.InnerText=="15")==true)throw new UnsupportedSaveVersionException();
                var r18=layoutVersion==CampaignVersion&&(members.Any(x=>x.Name=="RoundNumber"&&x.InnerText=="18")&&members.Any(x=>x.Name=="Stage"&&x.InnerText!="between_rounds")||members.Any(x=>x.Name=="RoundNumber"&&x.InnerText=="19")&&members.Any(x=>x.Name=="Stage"&&x.InnerText=="between_rounds"));
                if(!r18&&boss?.ChildNodes.OfType<XmlElement>().Any(x=>x.Name=="RoundNumber"&&x.InnerText=="18")==true)throw new UnsupportedSaveVersionException();
                var savedBossRound=boss?.ChildNodes.OfType<XmlElement>().FirstOrDefault(x=>x.Name=="RoundNumber")?.InnerText;
                var hasExpansionBoss=int.TryParse(savedBossRound,out var laterBoss)&&laterBoss>=21;
                if(layoutVersion<CampaignVersion&&hasExpansionBoss)throw new UnsupportedSaveVersionException();
                if(version==1 && (boss!=null||members.Any(x=>x.Name=="Forms"||x.Name=="CompletedBoss"||x.Name=="CompletedSecondBoss"||x.Name=="NormalEndedAt"||x.Name=="RoundCompletionApplied"||x.Name=="PreviousNormalRound")))throw new InvalidDataException("Schema1 cannot contain Boss");
                if(boss!=null && boss.GetAttribute("type")=="object")
                {
                    var bossFields=boss.ChildNodes.OfType<XmlElement>().ToArray();
                    if(bossFields.Select(x=>x.Name).Distinct().Count()!=bossFields.Length)throw new InvalidDataException("Duplicate boss field");
                    var bossVersion=bossFields.SingleOrDefault(x=>x.Name=="Version");
                    if(bossVersion!=null && bossVersion.GetAttribute("type")=="number" &&
                        (!int.TryParse(bossVersion.InnerText,out var bv)||bv>BossDefinition.LatestEncounterVersion))throw new UnsupportedSaveVersionException();
                    var reward=bossFields.SingleOrDefault(x=>x.Name=="Reward");
                    var rewardFields=reward?.ChildNodes.OfType<XmlElement>().ToArray()??System.Array.Empty<XmlElement>();
                    if(rewardFields.Select(x=>x.Name).Distinct().Count()!=rewardFields.Length)throw new InvalidDataException("Duplicate reward field");
                    var policy=rewardFields.SingleOrDefault(x=>x.Name=="PolicyId");
                    if(policy!=null && policy.GetAttribute("type")=="string" && policy.InnerText!=BossDefinition.LegacyPolicy&&!BossDefinition.IsProductionPolicy(policy.InnerText))throw new UnsupportedSaveVersionException();
                }
                if(layoutVersion<CampaignVersion && members.Any(x=>x.Name=="CompletedThirdBoss"||x.Name=="CompletedFourthBoss"||x.Name=="CompletedFifthBoss"||x.Name=="CompletedSixthBoss"||x.Name=="LaterBossHistory"))throw new UnsupportedSaveVersionException();
                if(layoutVersion==CampaignVersion)
                {
                    var stage3=members.SingleOrDefault(x=>x.Name=="Stage");
                    var round3=members.SingleOrDefault(x=>x.Name=="RoundNumber");
                    if(stage3==null||round3==null||!int.TryParse(round3.InnerText,out var rn)||
                        !SupportedChapterLifecycle(rn,stage3.InnerText))throw new UnsupportedSaveVersionException();
                    var normalRound=rn-(stage3.InnerText=="between_rounds"?1:0);
                    if(normalRound<13&&members.Any(x=>x.Name=="CompletedFourthBoss"))throw new UnsupportedSaveVersionException();
                    if(normalRound<LeviticusContent.GlobalRoundNumber&&members.Any(x=>x.Name=="CompletedFifthBoss"))throw new UnsupportedSaveVersionException();
                    if(normalRound<=18&&members.Any(x=>x.Name=="CompletedSixthBoss"))throw new UnsupportedSaveVersionException();
                    if(normalRound<=21&&members.Any(x=>x.Name=="LaterBossHistory"))throw new UnsupportedSaveVersionException();
                    if(hasExpansionBoss&&laterBoss!=normalRound)
                        throw new UnsupportedSaveVersionException();
                }
                if (layoutVersion == GenesisVersion)
                {
                    var stage = members.SingleOrDefault(x => x.Name == "Stage");
                    if (stage != null && stage.GetAttribute("type") == "string" && stage.InnerText != "normal" && stage.InnerText != "between_rounds" && stage.InnerText!="boss_intro" && stage.InnerText!="boss" && stage.InnerText!="boss_reward" && stage.InnerText!="boss_settlement" && stage.InnerText!="round_settlement")
                        throw new UnsupportedSaveVersionException();
                    var round = members.SingleOrDefault(x => x.Name == "RoundNumber");
                    if (stage != null && round != null && round.GetAttribute("type") == "number" &&
                        round.InnerText.Length > 0 && round.InnerText.All(c => c >= '0' && c <= '9') &&
                        (!int.TryParse(round.InnerText, NumberStyles.None, CultureInfo.InvariantCulture, out var numberValue) ||
                         (stage.InnerText == "normal" && numberValue > 9) || (stage.InnerText == "between_rounds" && numberValue > 10) || ((stage.InnerText=="boss"||stage.InnerText=="boss_intro"||stage.InnerText=="boss_reward"||stage.InnerText=="boss_settlement") && numberValue>9) || (stage.InnerText=="round_settlement"&&numberValue>8)))
                        throw new UnsupportedSaveVersionException();
                }
            }
            if (fields.Any(x => !EnvelopeFields.Contains(x.Name))) RejectUnsupportedContent(version, "Unknown save envelope field");
            if (version >= GenesisVersion && !skillTree) CheckSupportedContent(root, LegacyContent);
            if (version >= GenesisVersion) CheckForms(root, expandedCombat);
            return version;
        }

        internal static bool SupportedChapterLifecycle(int round,string stage)=>
            ((round==10||round==11||round==13||round==14||round==16||round==17||round==19||round==20)&&(stage=="normal"||stage=="round_settlement"))||
            ((round==12||round==15||round==18)&&(stage=="normal"||stage=="boss_intro"||stage=="boss"||stage=="boss_reward"||stage=="boss_settlement"))||
            (round>=11&&round<=CampaignContent.LastInstalledRound+1&&stage=="between_rounds")||
            (round>=21&&round<=CampaignContent.LastInstalledRound&&
                (stage=="normal"||(BossDefinition.Find(round)!=null
                    ?stage=="boss_intro"||stage=="boss"||stage=="boss_reward"||stage=="boss_settlement"
                    :stage=="round_settlement")));

        private static void CheckForms(XmlElement element, bool expanded)
        {
            if((element.Name=="Boss"||element.Name=="CompletedBoss"||element.Name=="CompletedSecondBoss"||element.Name=="CompletedThirdBoss"||element.Name=="CompletedFourthBoss"||element.Name=="CompletedFifthBoss"||element.Name=="CompletedSixthBoss"||element.ParentNode?.Name=="LaterBossHistory")&&element.GetAttribute("type")=="object")
            {
                var round=element.ChildNodes.OfType<XmlElement>().FirstOrDefault(x=>x.Name=="RoundNumber");
                BossDefinition? rules=null;
                if(round?.GetAttribute("type")=="number")
                {
                    if(!int.TryParse(round.InnerText,out var br)||(rules=BossDefinition.Find(br==0?3:br))==null)
                        throw new UnsupportedSaveVersionException();
                }
                var encounter=element.ChildNodes.OfType<XmlElement>().FirstOrDefault(x=>x.Name=="EncounterId");
                if(rules!=null&&rules.Number>=21&&encounter?.InnerText.EndsWith(rules.EncounterSuffix,StringComparison.Ordinal)!=true)
                    throw new UnsupportedSaveVersionException();
                if(round?.InnerText=="18"&&encounter?.InnerText.EndsWith(":leviticus_03:ash_veil",StringComparison.Ordinal)!=true)throw new UnsupportedSaveVersionException();
                if(round?.InnerText=="15"&&encounter?.InnerText.EndsWith(":exodus_06:defilement_weaver",StringComparison.Ordinal)!=true)throw new UnsupportedSaveVersionException();
                if(round?.InnerText=="12"&&encounter?.InnerText.EndsWith(":exodus_03:oppression_chariot",StringComparison.Ordinal)!=true)throw new UnsupportedSaveVersionException();
                if(encounter?.GetAttribute("type")=="string"&&!encounter.InnerText.EndsWith(":genesis_03:nephilim_tyrant",StringComparison.Ordinal)&&!encounter.InnerText.EndsWith(":genesis_06:sitnah_keeper",StringComparison.Ordinal)&&!encounter.InnerText.EndsWith(":genesis_09:silver_cup_accuser",StringComparison.Ordinal)&&!encounter.InnerText.EndsWith(":exodus_03:oppression_chariot",StringComparison.Ordinal)&&!encounter.InnerText.EndsWith(":exodus_06:defilement_weaver",StringComparison.Ordinal)&&!encounter.InnerText.EndsWith(":leviticus_03:ash_veil",StringComparison.Ordinal)&&!(rules!=null&&rules.Number>=21&&encounter.InnerText.EndsWith(rules.EncounterSuffix,StringComparison.Ordinal)))throw new UnsupportedSaveVersionException();
                var version=element.ChildNodes.OfType<XmlElement>().FirstOrDefault(x=>x.Name=="Version");
                var maxVersion=rules?.EncounterVersion??2;
                if(version?.GetAttribute("type")=="number"&&(!int.TryParse(version.InnerText,out var bv)||bv>maxVersion))throw new UnsupportedSaveVersionException();
                if(rules?.UsesDash==true&&version?.InnerText!=maxVersion.ToString(CultureInfo.InvariantCulture))throw new UnsupportedSaveVersionException();
                var reward=element.ChildNodes.OfType<XmlElement>().FirstOrDefault(x=>x.Name=="Reward");
                var rewardVersion=reward?.ChildNodes.OfType<XmlElement>().FirstOrDefault(x=>x.Name=="RulesVersion");
                if (rewardVersion?.GetAttribute("type")=="number" &&
                    (!int.TryParse(rewardVersion.InnerText,out var rv)||rv>2))
                    throw new UnsupportedSaveVersionException();
                var policy=reward?.ChildNodes.OfType<XmlElement>().FirstOrDefault(x=>x.Name=="PolicyId");
                if(policy?.GetAttribute("type")=="string")
                {
                    var id=policy.InnerText;
                    if(id!=BossDefinition.LegacyPolicy&&!BossDefinition.IsProductionPolicy(id))
                        throw new UnsupportedSaveVersionException();
                    if(BossDefinition.IsExpansionPolicy(id)&&rules?.RewardPolicy!=id)
                        throw new UnsupportedSaveVersionException();
                }
            }
            if(element.Name=="HostileProjectiles"&&element.GetAttribute("type")=="array")
                foreach(var shot in element.ChildNodes.OfType<XmlElement>())
                {
                    var version=shot.ChildNodes.OfType<XmlElement>().FirstOrDefault(x=>x.Name=="Version");
                    if(version?.GetAttribute("type")=="number"&&(!int.TryParse(version.InnerText,out var hv)||hv>1))throw new UnsupportedSaveVersionException();
                }
            if(element.Name=="Choices"&&element.GetAttribute("type")=="array")
                foreach(var choice in element.ChildNodes.OfType<XmlElement>())
                    if(choice.GetAttribute("type")=="string"&&!AdamSession.KnownGrowthChoice(choice.InnerText,expanded?2:1))throw new UnsupportedSaveVersionException();
            if(element.Name=="Forms" && element.GetAttribute("type")=="array")
                foreach(var entry in element.ChildNodes.OfType<XmlElement>())
                {
                    var value=entry.ChildNodes.OfType<XmlElement>().FirstOrDefault(x=>x.Name=="Value");
                    if(value?.GetAttribute("type")=="string"&&!KnownForm(value.InnerText,expanded))throw new UnsupportedSaveVersionException();
                }
            if(element.Name=="WeaponForm" && element.GetAttribute("type")=="string"&&!KnownForm(element.InnerText,expanded))throw new UnsupportedSaveVersionException();
            if(element.Name=="item" && element.GetAttribute("type")=="object")
            {
                var fields=element.ChildNodes.OfType<XmlElement>().ToArray();
                var kind=fields.FirstOrDefault(x=>x.Name=="Kind");
                if(kind?.GetAttribute("type")=="string"&&kind.InnerText!="item"&&kind.InnerText!="evolution"&&kind.InnerText!="gold")throw new UnsupportedSaveVersionException();
                if(fields.Any(x=>x.Name=="Kind"&&x.InnerText=="evolution"))
                {
                    var item=fields.FirstOrDefault(x=>x.Name=="ItemId");
                    if(item?.GetAttribute("type")=="string"&&!KnownForm(item.InnerText,expanded))throw new UnsupportedSaveVersionException();
                }
            }
            foreach(var child in element.ChildNodes.OfType<XmlElement>())CheckForms(child,expanded);
        }
        private static bool KnownForm(string form,bool expanded)=>form==EvolutionRules.Bible||form==EvolutionRules.Jawbone||
            form==EvolutionRules.Sling||expanded&&ExpandedWeaponRules.TryGetByEvolution(form,out var rules)&&
            EquipmentInventory.SupportsWeapon(rules.WeaponId,2);

        private static void RejectUnsupportedContent(int version, string message)
        {
            if (version >= GenesisVersion) throw new UnsupportedSaveVersionException();
            throw new InvalidDataException(message);
        }
    }
}
