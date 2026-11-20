using System;
using System.Collections.Generic;
using BibleSurvivor.Core;
using UnityEngine;

namespace BibleSurvivor.Unity
{
    // Read-only projection of BattleWorld. No collisions, damage callbacks, timers or random calls enter the core.
    public sealed class AdamArtWorld : IDisposable
    {
        public readonly AdamJawboneSwing JawboneSwing=new AdamJawboneSwing();
        public readonly AdamVisualQualityWorld VisualQuality=new AdamVisualQualityWorld();
        public readonly AdamPolishContacts PolishContacts=new AdamPolishContacts();
        readonly AdamPolishFlash polishFlash=new AdamPolishFlash();
        public bool PolishEnabled=>AdamVisualQualityArt.TryGetClip(AdamPolishContacts.Thud,out _);
        AdamR02Art r02;
        AdamR03Art r03;
        AdamR04Art r04;
        AdamR05Art r05;
        AdamR06Art r06;
        AdamR07Art r07;
        AdamR08Art r08;
        AdamR09Art r09;
        AdamExodusR01Art exodusR01;
        AdamExodusR02Art exodusR02;
        AdamExodusR03Art exodusR03;
        AdamExodusR04Art exodusR04;
        AdamExodusR05Art exodusR05;
        AdamExodusR06Art exodusR06;
        ChariotDashView chariotVisual;
        readonly AdamChargeLane chargeLane=new AdamChargeLane();
        readonly AdamChargeLane chariotLane=new AdamChargeLane();
        readonly Dictionary<int,bool> chargeFacing=new Dictionary<int,bool>();
        readonly AdamDangerRing danger=new AdamDangerRing();
        BossEncounterState bossVisual;
        Vector2 bossFeet;
        bool bossLeft;
        double bossImpactAt=-1,bossDeathAt=-1;
        public readonly struct EnemySprite
        {
            public readonly BattleEntityState Enemy;
            public readonly Vector2 Feet;
            public EnemySprite(BattleEntityState enemy,Vector2 feet){Enemy=enemy;Feet=feet;}
        }
        sealed class EnemySpriteComparer : IComparer<EnemySprite>
        {
            public static readonly EnemySpriteComparer Instance=new EnemySpriteComparer();
            public int Compare(EnemySprite a,EnemySprite b)
            {var depth=a.Feet.y.CompareTo(b.Feet.y);return depth!=0?depth:a.Enemy.Id.CompareTo(b.Enemy.Id);}
        }
        readonly List<EnemySprite> enemySprites=new List<EnemySprite>(128);
        // Presentation-only reusable buffer: compare the actual rounded screen feet, then stable Core ID.
        public IReadOnlyList<EnemySprite> PrepareEnemySprites(IReadOnlyList<BattleEntityState> enemies,AdamWorldProjection projection)
        {
            enemySprites.Clear();
            for(var i=0;i<enemies.Count;i++){var enemy=enemies[i];enemySprites.Add(new EnemySprite(enemy,projection.Point(enemy.X,enemy.Y)));}
            // Small bounded actor batches; insertion sort avoids framework/delegate allocations after warmup.
            var comparer=EnemySpriteComparer.Instance;
            for(var i=1;i<enemySprites.Count;i++)
            {var value=enemySprites[i];var j=i-1;while(j>=0 && comparer.Compare(enemySprites[j],value)>0){enemySprites[j+1]=enemySprites[j];j--;}enemySprites[j+1]=value;}
            return enemySprites;
        }
        public const int EffectCapacity = 64;
        public sealed class Effect
        {
            public bool Active;
            public string Clip;
            public Vector2 Position;
            public double Started;
            public int Priority;
        }
        public sealed class EffectPool
        {
            public readonly Effect[] Slots = new Effect[EffectCapacity];
            int next;
            public EffectPool() { for(var i=0;i<Slots.Length;i++) Slots[i]=new Effect(); }
            public void Emit(string clip,Vector2 position,double started,int priority=0)
            {
                // Lower-priority decoration cannot evict an active player warning.
                var slot=-1;
                for(var i=0;i<Slots.Length;i++) {var candidate=(next+i)%Slots.Length;if(!Slots[candidate].Active || Slots[candidate].Priority<=priority){slot=candidate;break;}}
                if(slot<0)return;
                var e=Slots[slot];next=(slot+1)%Slots.Length;e.Priority=priority;
                e.Active=false;e.Clip=clip;e.Position=position;e.Started=started;e.Active=true;
            }
            public void Clear()
            {
                next=0;
                foreach(var e in Slots) {e.Active=false;e.Clip=null;e.Position=Vector2.zero;e.Started=0;e.Priority=0;}
            }
        }
        readonly EffectPool effectPool = new EffectPool();
        public Effect[] Effects => effectPool.Slots;
        public BattleSnapshot Snapshot { get; private set; }
        public string PlayerMotion { get; private set; } = "idle";
        public bool PlayerLeft => Snapshot != null && Snapshot.FacingX < 0;
        public double Clock { get; private set; }
        public int ActiveEffects { get { var n=0; foreach(var e in Effects) if(e.Active)n++; return n; } }
        readonly Dictionary<int, Vector2> previousPositions = new Dictionary<int, Vector2>();
        readonly Dictionary<int,double> reactions=new Dictionary<int,double>();
        readonly List<BattleImpact> frameImpacts=new List<BattleImpact>(BattleWorld.ImpactCapacity);
        readonly HashSet<int> reportedKills=new HashSet<int>();
        public IReadOnlyList<BattleImpact> FrameImpacts => frameImpacts;
        public long ConsumedImpacts {get;private set;}
        public long DroppedImpacts {get;private set;}
        public int Generation {get;private set;}
        public bool FeedbackEnabled=true;
        // Required gameplay marks are independent of optional feedback, including during pause.
        public bool TryChainGeometry(BattleSnapshot snapshot,BossEncounterState boss,string stage,int direction,out AdamChargeGeometry geometry)=>
            AdamChainWarning.TryGeometry(snapshot,boss,stage,direction,out geometry);
        public double PlayerHitUntil {get;private set;}
        long impactCursor;
        public bool EnemyReacting(int id) => reactions.TryGetValue(id,out var until) && Clock<until;
        public const double HitFlashSeconds=.04;
        public int ContactStars
        {get{var count=0;foreach(var effect in Effects)if(effect.Active&&effect.Clip=="fx_hit-spark_hit_16"&&Clock-effect.Started<.2)count++;return count;}}
        public float EnemyFlashOpacity(int id) => (FeedbackEnabled||PolishEnabled) && reactions.TryGetValue(id,out var until) &&
            Clock<until && until-Clock>.09-(PolishEnabled?.05:HitFlashSeconds)+1e-9 ? .65f : 0;
        public static Matrix4x4 ReactionMatrix(Vector2 feet,float strength) => Matrix4x4.Translate(feet)*
            Matrix4x4.Scale(new Vector3(1+.12f*strength,1-.15f*strength,1))*Matrix4x4.Translate(-feet);
        BattleWorld source;
        bool wasActive;
        double deathAt = -1;
        public void Update(AdamHost host, float delta)
        {
            if(host?.World == null) { Clear(); return; }
            if(source != host.World) { Clear(); source=host.World; Generation++; }
            DroppedImpacts+=source.ReadImpacts(impactCursor,frameImpacts);
            impactCursor=source.ImpactSequence;reportedKills.Clear();
            var next=source.Capture();var active=host.Session.IsActive;var monsters=host.Session.Wave?.Active;
            var nextBoss=host.Session.Boss;
            VisualQuality.Observe(next);
            if(host.Session.Inventory.Weapons.ContainsKey("jawbone"))
                JawboneSwing.Observe(next,host.Session.AttackProfile("jawbone").Amount,WeaponRules.Get("jawbone",host.Session.Inventory.Weapons["jawbone"]).BurstInterval);
            if(bossVisual!=null && nextBoss!=null)
            {
                if(nextBoss.NextAttackAt>bossVisual.NextAttackAt)bossImpactAt=next.Time;
                if(bossVisual.Health>0 && nextBoss.Health<=0)bossDeathAt=next.Time;
            }
            // Reload can begin inside the short attack pose; Core birth times are the emission clock.
            if(nextBoss?.Rules.Number==9 && next.HostileProjectiles!=null)
                foreach(var projectile in next.HostileProjectiles)bossImpactAt=Math.Max(bossImpactAt,projectile.BornAt);
            var previousBossLeft=bossLeft;
            foreach(var e in next.Enemies)if(e.Id==nextBoss?.Rules.BossActorId){bossFeet=new Vector2(e.X,e.Y);bossLeft=nextBoss!=null&&nextBoss.Telegraphing?nextBoss.TargetX<e.X:next.PlayerX<e.X;}
            chariotVisual=source.ChariotDash;
            if(chariotVisual!=null && (chariotVisual.Phase=="telegraph"||chariotVisual.Phase=="dash"))
                bossLeft=chariotVisual.Direction.X!=0?chariotVisual.Direction.X<0:previousBossLeft;
            bossVisual=nextBoss;
            // A fatal contact (or retaliation opening a choice) can change the arena without advancing Core time.
            if(Snapshot == null || next.Time != Snapshot.Time || active != wasActive || next.Hits != Snapshot.Hits ||
                next.Kills != Snapshot.Kills || next.Collected != Snapshot.Collected || next.DamageTaken != Snapshot.DamageTaken ||
                next.PlayerX != Snapshot.PlayerX || next.PlayerY != Snapshot.PlayerY || frameImpacts.Count>0)
            {
                PlayerMotion = Snapshot != null && (next.PlayerX != Snapshot.PlayerX || next.PlayerY != Snapshot.PlayerY || frameImpacts.Count>0) ? "walk" : "idle";
                Clock=next.Time;
                foreach(var impact in frameImpacts)
                {
                    ConsumedImpacts++;
                    var position=new Vector2(impact.Position.X,impact.Position.Y);
                    if(impact.TargetId==0)
                    {PlayerHitUntil=Clock+.1;effectPool.Emit("fx_hit-spark_hit_16",position,Clock,2);continue;}
                    var contact=AdamPolishContacts.Id(impact.Cause);var differentiated=contact!=null&&AdamVisualQualityArt.TryGetClip(contact,out _);
                    if(differentiated)PolishContacts.Observe(impact,next,FeedbackEnabled);
                    else if(FeedbackEnabled&&ContactStars<3)Emit("fx_hit-spark_hit_16",position);
                    if(FeedbackEnabled||PolishEnabled)reactions[impact.TargetId]=Clock+.09;
                    if(impact.Killed){reportedKills.Add(impact.TargetId);if(FeedbackEnabled)Emit("fx_enemy-death_burst_32",position);}
                }
                previousPositions.Clear();
                foreach(var enemy in next.Enemies) previousPositions[enemy.Id]=new Vector2(enemy.X,enemy.Y);

                foreach(var id in new List<int>(reactions.Keys))
                    if(!previousPositions.ContainsKey(id) || reactions[id]<=Clock)reactions.Remove(id);
                Snapshot=next;
                wasActive=active;
            }
            if(!host.Session.IsActive && host.Session.CurrentHealth<=0)
            {
                if(deathAt<0) deathAt=Clock;
                PlayerMotion="death";
                // Terminal clip is presentation-only; Core time remains fixed. Lifecycle suspension freezes it too.
                if(!host.Paused && !host.Suspended) Clock+=Math.Max(0,Math.Min(delta,.1f));
            }
            if(bossDeathAt>=0 && !host.Session.IsActive && !host.Paused && !host.Suspended && Clock<bossDeathAt+BossDeathDuration(bossVisual))
                Clock=Math.Min(bossDeathAt+BossDeathDuration(bossVisual),Clock+Math.Max(0,Math.Min(delta,.1f)));
            foreach(var effect in Effects) if(effect.Active && Clock-effect.Started>=1) effect.Active=false;
        }
        void Emit(string clip, Vector2 pos)
        { effectPool.Emit(clip,pos,Clock,1); }
        public double PlayerSeconds => PlayerMotion=="death" ? Math.Max(0,Clock-deathAt) : Clock;
        public void Clear()
        {
            JawboneSwing.Clear();VisualQuality.Clear();PolishContacts.Clear();
            source=null; Snapshot=null; Clock=0; deathAt=-1; wasActive=false; PlayerMotion="idle";
            previousPositions.Clear();reactions.Clear();frameImpacts.Clear();reportedKills.Clear();
            impactCursor=0;ConsumedImpacts=0;DroppedImpacts=0;PlayerHitUntil=0;
            effectPool.Clear();enemySprites.Clear();
            bossVisual=null;chariotVisual=null;bossDeathAt=-1;bossImpactAt=-1;
            chargeFacing.Clear();
        }
        public void Dispose(){Clear();polishFlash.Dispose();r02?.Dispose();r02=null;exodusR01?.Dispose();exodusR01=null;exodusR02?.Dispose();exodusR02=null;exodusR03?.Dispose();exodusR03=null;exodusR04?.Dispose();exodusR04=null;exodusR05?.Dispose();exodusR05=null;exodusR06?.Dispose();exodusR06=null;danger.Dispose();chargeLane.Dispose();chariotLane.Dispose();}

        // Leviticus and later books reuse the reviewed Exodus R06 art for equivalent movement roles.
        static string SourceActor(string id)
        {
            switch(id)
            {
                case "lv1":case "nm1":case "dt1":case "js1":case "jd1":case "rt1":case "sm1":case "ss1":case "fk1":case "sk1":case "fc1":case "sc1":
                case "lament_ash":case "tangled_thicket":case "dry_land_ember":case "cave_hunter":case "starving_siege_beast":
                case "scattered_scroll_fragments":case "endless_procession":case "tower_pride_shadow":return "ex17";
                case "lv2":case "nm2":case "dt2":case "js2":case "jd2":case "rt2":case "sm2":case "ss2":case "fk2":case "sk2":case "fc2":case "sc2":return "ex16";
                case "lv3":case "nm3":case "dt3":case "js3":case "jd3":case "rt3":case "sm3":case "ss3":case "fk3":case "sk3":case "fc3":case "sc3":return "ex18";
                case "ash_veil":
                case "oath_wraith":
                case "dust_tyrant":
                case "ravine_stalker":
                case "wilderness_warden":
                case "oblivion_mist":
                case "crossroads_sentinel":
                case "rampart_colossus":
                case "shifting_boundary":
                case "torrent_serpent":
                case "ford_gatekeeper":
                case "toppling_pillar":
                case "chaff_whirl":
                case "swarm_monarch":
                case "valley_bronze_brute":
                case "palace_roof_eye":case "lentil_field_beast":case "bethel_altar_calf":
                case "vineyard_greed_vine":
                case "dothan_iron_ring":
                case "winged_bull":
                case "ashen_gate_lion":case "wilderness_raider_chief":case "rending_host":case "bronze_hook_chain":return "defilement_weaver";
                default:return id;
            }
        }
        public static string EnemyShadow(string id)=>AdamRoundPresentation.Shadow(SourceActor(id));

        static bool UsesWeaverArt(BossEncounterState boss)=>SourceActor(boss?.Rules.Id)=="defilement_weaver";
        AdamExodusR06Art ExodusR06Art()
        {
            if(exodusR06!=null)return exodusR06;
            exodusR06=new AdamExodusR06Art();
            exodusR06.PrepareHitMasks();
            return exodusR06;
        }
        public static string EnemyClip(string id)
        {
            id=SourceActor(id);
            switch(id) { case "a1":return "enm_a1-small-snake_move_32"; case "a2":return "enm_a2-fruit-moth_fly_32"; case "a3":return "enm_a3-thorn-beast_walk_32";
                case "b1":case "b2":case "b3":return AdamRoundPresentation.R02ClipId(id);
                case "c1":case "c2":case "c3":case "nephilim_tyrant":return AdamR03Art.EnemyClip(id);
                case "d1":case "d2":case "d3":return AdamR04Art.EnemyClip(id);
                case "e1":case "e2":case "e3":return AdamR05Art.EnemyClip(id);
                case "f1":case "f2":case "f3":case "sitnah_keeper":return AdamR06Art.EnemyClip(id);
                case "g1":case "g2":case "g3":return AdamR07Art.EnemyClip(id);
                case "h1":case "h2":case "h3":return AdamR08Art.EnemyClip(id);
                case "i1":case "i2":case "i3":case "silver_cup_accuser":return AdamR09Art.EnemyClip(id);
                case "ex1":case "ex2":case "ex3":return AdamExodusR01Art.EnemyClip(id);
                case "ex4":case "ex5":case "ex6":return AdamExodusR02Art.EnemyClip(id);
                case "ex7":case "ex8":case "ex9":case "oppression_chariot":return AdamExodusR03Art.EnemyClip(id);
                case "ex10":case "ex11":case "ex12":return AdamExodusR04Art.EnemyClip(id);
                case "ex13":case "ex14":case "ex15":return AdamExodusR05Art.EnemyClip(id);
                case "ex16":case "ex17":case "ex18":case "defilement_weaver":return AdamExodusR06Art.EnemyClip(id);
                default:throw new InvalidOperationException("No reviewed enemy art: "+id); }
        }
        public static bool PathCell(int x,int y) => x==0 || y==0; // One-cell-wide cross, never a filled 2x2.
        public static int PathMask(int x,int y) => (PathCell(x,y+1)?1:0)|(PathCell(x+1,y)?2:0)|(PathCell(x,y-1)?4:0)|(PathCell(x-1,y)?8:0);
        public static int PixelScale(Rect arena) => Math.Max(1, Mathf.FloorToInt(Mathf.Min(arena.width/512f,arena.height/256f)));
        public static Matrix4x4 FacingMatrix(Vector2 anchor,Vector2 direction)
        {
            var left=direction.x<0;
            var angle=Mathf.Atan2(left?direction.y:-direction.y,Mathf.Abs(direction.x))*Mathf.Rad2Deg;
            return Matrix4x4.Translate(anchor)*Matrix4x4.Rotate(Quaternion.Euler(0,0,angle))*
                Matrix4x4.Scale(new Vector3(left?-1:1,1,1))*Matrix4x4.Translate(-anchor);
        }
        public static double LatestJawboneBurst(BattleWeaponState weapon,double now,int amount,double interval)
        {
            var latest=-1d;
            if(weapon.ActiveSince<0)return latest;
            for(var i=0;i<amount;i++)
            {
                var at=weapon.ActiveSince+i*interval;
                if(at>now+1e-9)continue;
                var pending=false;foreach(var future in weapon.PendingBursts)if(Math.Abs(future-at)<1e-8)pending=true;
                if(!pending)latest=at;
            }
            return latest;
        }
        public void Draw(AdamArt art, AdamHost host, Rect arena, AdamImpactParticles particles = null, System.Action beforeTelegraphs = null, bool bossShadowOnly = false)
        {
            if(Snapshot==null || source!=host.World) return;
            var round=host.Session.RoundNumber;
            var leviticus=round>=LeviticusContent.GlobalRoundNumber && round<=LeviticusContent.LastInstalledRound;
            var numbers=round>=NumbersContent.GlobalRoundNumber && round<=NumbersContent.LastRound;
            var deuteronomy=round>=DeuteronomyContent.GlobalRoundNumber && round<=DeuteronomyContent.LastRound;
            var joshua=round>=JoshuaContent.GlobalRoundNumber && round<=JoshuaContent.LastRound;
            var judges=round>=JudgesContent.GlobalRoundNumber && round<=JudgesContent.LastRound;
            var ruth=round>=RuthContent.GlobalRoundNumber && round<=RuthContent.LastRound;
            var firstSamuel=round>=FirstSamuelContent.GlobalRoundNumber && round<=FirstSamuelContent.LastRound;
            var laterBook=LaterCampaignBooks.Find(round);
            var desert=AdamBookGround.Exodus(round) || ((leviticus || numbers || deuteronomy || joshua || judges || ruth || firstSamuel || laterBook!=null) && AdamVisualQualityArt.TryGetClip(AdamBookGround.Sand,out _));
            var snap=Snapshot; var scale=PixelScale(arena); var pixels=32*scale;
            var origin=new Vector2(snap.PlayerX,snap.PlayerY);
            var projection=new AdamWorldProjection(arena,origin);
            Vector2 Point(float x,float y) => projection.Point(x,y);
            GUI.BeginGroup(arena);
            var rx=Mathf.Min(10,arena.width/(float)pixels/2+1); var ry=Mathf.Min(5.625f,arena.height/(float)pixels/2+1);
            // Terrain is deterministic by integer coordinate, not gameplay RNG. Tiles are never rotated/flipped.
            var groundColor=GUI.color;
            try
            {
                if(leviticus && desert)GUI.color=groundColor*new Color(.80f,.90f,.98f,1);
                if(numbers && desert)GUI.color=groundColor*new Color(.94f,.83f,.72f,1);
                if(deuteronomy && desert)GUI.color=groundColor*new Color(.88f,.84f,.94f,1);
                if(joshua && desert)GUI.color=groundColor*new Color(.84f,.94f,.76f,1);
                if(judges && desert)GUI.color=groundColor*new Color(.98f,.80f,.76f,1);
                if(ruth && desert)GUI.color=groundColor*new Color(1f,.94f,.70f,1);
                if(firstSamuel && desert)GUI.color=groundColor*new Color(.78f,.86f,.96f,1);
                if(laterBook!=null && desert)GUI.color=groundColor*(laterBook.Id=="second_samuel"?new Color(.90f,.76f,.72f,1):new Color(.91f,.87f,.72f,1));
                for(var y=Mathf.FloorToInt(origin.y-ry);y<=Mathf.CeilToInt(origin.y+ry);y++)
                for(var x=Mathf.FloorToInt(origin.x-rx);x<=Mathf.CeilToInt(origin.x+rx);x++)
                {
                    art.Draw(AdamBookGround.Ground(x,y,desert),Point(x+.5f,y+.5f),scale);
                    if(PathCell(x,y)) art.Draw(AdamBookGround.Path(desert),Point(x+.5f,y+.5f),scale,frame:PathMask(x,y));
                }
                if(desert)AdamBookGround.Hollows(art,projection,origin,rx,ry);
                for(var y=Mathf.FloorToInt(origin.y-ry);y<=Mathf.CeilToInt(origin.y+ry);y++)
                for(var x=Mathf.FloorToInt(origin.x-rx);x<=Mathf.CeilToInt(origin.x+rx);x++)
                    if(!PathCell(x,y) && (uint)(x*73856093^y*19349663)%5==0)
                        art.Draw(AdamBookGround.Decor(x,y,desert),Point(x+.5f,y+.2f),scale);
            }
            finally{GUI.color=groundColor;}
            if(desert)AdamBookGround.Edges(arena,FeedbackEnabled);
            else AdamVisualQualityWorld.Canopy(art,arena,scale,FeedbackEnabled);
            art.Draw("fx_shadow_static_16x6",Point(snap.PlayerX,snap.PlayerY),scale);
            // Optional hit smoke/chips stay below XP, actors, weapons and required telegraphs.
            particles?.Draw(projection);
            PolishContacts.Draw(projection,Clock,FeedbackEnabled);
            foreach(var effect in Effects)
                if((FeedbackEnabled||effect.Priority==2) && effect.Active && Clock-effect.Started<art.Clip(effect.Clip).frames/(double)art.Clip(effect.Clip).fps)
                    art.Draw(effect.Clip,Point(effect.Position.x,effect.Position.y),scale,Clock-effect.Started);
            if(host.Session.IsActive)
            {
                var monsters=host.Session.Targets;var experience=host.Session.DroppedExperience;
                foreach(var enemy in snap.Enemies)
                    if(monsters!=null && monsters.TryGetValue(enemy.Id,out var monster))
                    {
                        var shadow=EnemyShadow(monster.MonsterId);
                        if(shadow=="fx_shadow-boss_static_40x8"){if(r03==null)r03=new AdamR03Art();r03.Draw(shadow,Point(enemy.X,enemy.Y),scale);}
                        else art.Draw(shadow,Point(enemy.X,enemy.Y),scale);
                    }
                foreach(var drop in snap.Drops)
                    if(experience.TryGetValue(drop.Id,out var xp))
                        art.Draw(xp>1?"pkp_xp-gem-bonus_idle_8":"pkp_xp-gem-base_idle_8",Point(drop.X,drop.Y),scale,Clock);
                var orderedSprites=PrepareEnemySprites(snap.Enemies,projection);
                for(var spriteIndex=0;spriteIndex<orderedSprites.Count;spriteIndex++)
                {
                    var sprite=orderedSprites[spriteIndex];var enemy=sprite.Enemy;
                    if (bossShadowOnly && enemy.Id == host.Session.BossRules?.BossActorId) continue;
                    if(monsters!=null && monsters.TryGetValue(enemy.Id,out var monster))
                    {
                        var anchor=sprite.Feet;var before=GUI.matrix;var tint=GUI.color;
                        // GUI.matrix transforms window coordinates even inside the arena group.
                        if(FeedbackEnabled && EnemyReacting(enemy.Id)){GUI.matrix=before*ReactionMatrix(anchor+arena.position,1);GUI.color=new Color(1,.55f,.35f,1);}
                        var sourceActor=SourceActor(monster.MonsterId);
                        if(AdamExodusR06Art.Enemy(sourceActor))
                        {
                            var rig=ExodusR06Art();
                            var clip=AdamExodusR06Art.EnemyClip(sourceActor);var left=enemy.X>snap.PlayerX;var frame=-1;var seconds=Clock;
                            if(sourceActor=="defilement_weaver")
                            {clip=AdamExodusR06Art.BossClip(host.Session.Stage,bossVisual,snap.Time,out frame,out seconds);left=bossLeft;}
                            rig.Draw(clip,anchor,scale,seconds,left,frame);
                            rig.DrawHit(clip,anchor,scale,seconds,left,EnemyFlashOpacity(enemy.Id),frame);
                        }
                        else if(AdamExodusR05Art.Enemy(monster.MonsterId))
                        {
                            if(exodusR05==null){exodusR05=new AdamExodusR05Art();exodusR05.PrepareHitMasks();}
                            var clip=EnemyClip(monster.MonsterId);var left=enemy.X>snap.PlayerX;
                            exodusR05.Draw(clip,anchor,scale,Clock,left);
                            exodusR05.DrawHit(clip,anchor,scale,Clock,left,EnemyFlashOpacity(enemy.Id));
                        }
                        else if(AdamExodusR04Art.Enemy(monster.MonsterId))
                        {
                            if(exodusR04==null){exodusR04=new AdamExodusR04Art();exodusR04.PrepareHitMasks();}
                            var clip=EnemyClip(monster.MonsterId);var left=enemy.X>snap.PlayerX;
                            exodusR04.Draw(clip,anchor,scale,Clock,left);
                            exodusR04.DrawHit(clip,anchor,scale,Clock,left,EnemyFlashOpacity(enemy.Id));
                        }
                        else if(AdamExodusR03Art.Enemy(monster.MonsterId))
                        {
                            if(exodusR03==null){exodusR03=new AdamExodusR03Art();exodusR03.PrepareHitMasks();}
                            var id=monster.MonsterId;var clip=EnemyClip(id);var left=enemy.X>snap.PlayerX;var frame=-1;var seconds=Clock;
                            if(id=="oppression_chariot")
                            {clip=AdamChariotWarning.BossClip(host.Session.Stage,chariotVisual,snap.Time,out frame,out seconds);left=bossLeft;}
                            else if(id=="ex9")
                            {
                                EnemyChargeState charge=null;if(snap.Charges!=null)foreach(var c in snap.Charges)if(c.Id==enemy.Id){charge=c;break;}
                                clip=AdamChariotWarning.NormalClip(charge,snap.Time,out frame,out seconds);
                                if(charge!=null && charge.Phase!="cooldown")
                                {if(charge.DirectionX!=0)left=charge.DirectionX<0;else if(chargeFacing.TryGetValue(enemy.Id,out var previous))left=previous;}
                                chargeFacing[enemy.Id]=left;
                            }
                            exodusR03.Draw(clip,anchor,scale,seconds,left,frame);
                            exodusR03.DrawHit(clip,anchor,scale,seconds,left,EnemyFlashOpacity(enemy.Id),frame);
                        }
                        else if(AdamExodusR02Art.Enemy(monster.MonsterId))
                        {
                            if(exodusR02==null){exodusR02=new AdamExodusR02Art();exodusR02.PrepareHitMasks();}
                            var clip=EnemyClip(monster.MonsterId);var left=enemy.X>snap.PlayerX;
                            exodusR02.Draw(clip,anchor,scale,Clock,left);
                            exodusR02.DrawHit(clip,anchor,scale,Clock,left,EnemyFlashOpacity(enemy.Id));
                        }
                        else if(AdamExodusR01Art.Enemy(monster.MonsterId))
                        {
                            if(exodusR01==null){exodusR01=new AdamExodusR01Art();exodusR01.PrepareHitMasks();}
                            var clip=EnemyClip(monster.MonsterId);var left=enemy.X>snap.PlayerX;
                            exodusR01.Draw(clip,anchor,scale,Clock,left);
                            exodusR01.DrawHit(clip,anchor,scale,Clock,left,EnemyFlashOpacity(enemy.Id));
                        }
                        else if(AdamRoundPresentation.R09Enemy(monster.MonsterId) || monster.MonsterId=="silver_cup_accuser")
                        {
                            if(r09==null)r09=new AdamR09Art();var id=monster.MonsterId;
                            if(id=="silver_cup_accuser"){var clip=BossClip(host.Session.Stage,bossVisual,bossImpactAt,Clock,out var frame);r09.Draw(clip,anchor,scale,Clock,bossLeft,frame);}
                            else r09.Draw(EnemyClip(id),anchor,scale,Clock,enemy.X>snap.PlayerX);
                        }
                        else if(AdamRoundPresentation.R08Enemy(monster.MonsterId))
                        {
                            if(r08==null)r08=new AdamR08Art();r08.Draw(EnemyClip(monster.MonsterId),anchor,scale,Clock,enemy.X>snap.PlayerX);
                        }
                        else if(AdamRoundPresentation.R07Enemy(monster.MonsterId))
                        {
                            if(r07==null)r07=new AdamR07Art();r07.Draw(EnemyClip(monster.MonsterId),anchor,scale,Clock,enemy.X>snap.PlayerX);
                        }
                        else if(AdamRoundPresentation.R06Enemy(monster.MonsterId))
                        {
                            if(r06==null)r06=new AdamR06Art();var id=monster.MonsterId;
                            if(id=="sitnah_keeper"){var clip=BossClip(host.Session.Stage,bossVisual,bossImpactAt,Clock,out var frame);r06.Draw(clip,anchor,scale,Clock,bossLeft,frame);}
                            else r06.Draw(EnemyClip(id),anchor,scale,Clock,enemy.X>snap.PlayerX);
                        }
                        else if(AdamRoundPresentation.R05Enemy(monster.MonsterId))
                        {
                            if(r05==null)r05=new AdamR05Art();r05.Draw(EnemyClip(monster.MonsterId),anchor,scale,Clock,enemy.X>snap.PlayerX);
                        }
                        else if(AdamRoundPresentation.R04Enemy(monster.MonsterId))
                        {
                            if(r04==null)r04=new AdamR04Art();var id=monster.MonsterId;var clip=EnemyClip(id);var frame=-1;var seconds=Clock;var left=enemy.X>snap.PlayerX;
                            if(id=="d3")
                            {
                                EnemyChargeState charge=null;if(snap.Charges!=null)foreach(var c in snap.Charges)if(c.Id==enemy.Id){charge=c;break;}
                                clip=AdamChargeLane.Clip(charge,snap.Time,out frame,out seconds);
                                if(charge!=null&&charge.Phase!="cooldown")
                                {if(charge.DirectionX!=0)left=charge.DirectionX<0;else if(chargeFacing.TryGetValue(enemy.Id,out var previous))left=previous;}
                                chargeFacing[enemy.Id]=left;
                            }
                            r04.Draw(clip,anchor,scale,seconds,left,frame);
                        }
                        else if(AdamRoundPresentation.R03Enemy(monster.MonsterId))
                        {
                            if(r03==null)r03=new AdamR03Art();var id=monster.MonsterId;
                            if(id=="nephilim_tyrant")
                            {
                                var clip=BossClip(host.Session.Stage,bossVisual,bossImpactAt,Clock,out var frame);
                                r03.Draw(clip,anchor,scale,Clock,bossLeft,frame);
                            }
                            else r03.Draw(EnemyClip(id),anchor,scale,Clock,enemy.X>snap.PlayerX);
                        }
                        else if(AdamRoundPresentation.R02Enemy(monster.MonsterId))
                        {
                            if(r02==null){r02=new AdamR02Art();r02.PrepareHitMasks();}
                            var clip=EnemyClip(monster.MonsterId);var left=enemy.X>snap.PlayerX;
                            r02.Draw(clip,anchor,scale,Clock,left);
                            r02.DrawHit(clip,anchor,scale,Clock,left,EnemyFlashOpacity(enemy.Id));
                        }
                        else{var clip=EnemyClip(monster.MonsterId);var left=enemy.X>snap.PlayerX;art.Draw(clip,anchor,scale,Clock,left);if(PolishEnabled)polishFlash.Draw(art,clip,anchor,scale,Clock,left,EnemyFlashOpacity(enemy.Id));}
                        GUI.matrix=before;GUI.color=tint;
                    }
                }
            }
            if(BossDeathVisible(bossDeathAt,Clock,bossVisual))
            {
                if(UsesWeaverArt(bossVisual))ExodusR06Art().Draw(AdamExodusR06Art.BossDeath,Point(bossFeet.x,bossFeet.y),scale,Math.Max(0,Clock-bossDeathAt),bossLeft);
                else if(bossVisual.Rules.IsChariot){if(exodusR03==null)exodusR03=new AdamExodusR03Art();exodusR03.Draw(AdamExodusR03Art.BossDeath,Point(bossFeet.x,bossFeet.y),scale,Math.Max(0,Clock-bossDeathAt),bossLeft);}
                else if(bossVisual.Rules.Number==9){if(r09==null)r09=new AdamR09Art();r09.Draw(AdamR09Art.BossDeath,Point(bossFeet.x,bossFeet.y),scale,Math.Max(0,Clock-bossDeathAt),bossLeft);}
                else if(bossVisual.Rules.Number==6){if(r06==null)r06=new AdamR06Art();r06.Draw(AdamR06Art.BossDeath,Point(bossFeet.x,bossFeet.y),scale,Math.Max(0,Clock-bossDeathAt),bossLeft);}
                else{if(r03==null)r03=new AdamR03Art();r03.Draw(AdamR03Art.BossDeath,Point(bossFeet.x,bossFeet.y),scale,Math.Max(0,Clock-bossDeathAt),bossLeft);}
            }

            // All six layers share ONE parent reflection about the registered feet pivot.
            var feet=Point(snap.PlayerX,snap.PlayerY); var matrix=GUI.matrix;
            if(PlayerLeft) GUI.matrix=matrix*FacingMatrix(feet+arena.position,Vector2.left);
            var playerTint=GUI.color;
            if(Clock<PlayerHitUntil) GUI.color=new Color(1,.4f,.4f,1);
            foreach(var layer in art.Index.layers)
                art.Draw("chr_player_layer-"+layer.Replace('_','-')+"_"+PlayerMotion+"_32",feet,scale,PlayerSeconds);
            GUI.color=playerTint;GUI.matrix=matrix;
            if(!host.Session.IsActive) {GUI.EndGroup();return;}
            foreach(var p in snap.Projectiles)
            {
                VisualQuality.AfterStones(art,projection,p,Clock,FeedbackEnabled);
                var anchor=Point(p.X,p.Y);var before=GUI.matrix;
                GUI.matrix=before*FacingMatrix(anchor+arena.position,new Vector2(p.VelocityX,p.VelocityY));
                if(FeedbackEnabled){var length=AdamVisualQualityWorld.TrailPixels(p,scale);AdamArt.Blit(art.Sprite("fx_sling-trail_static_20x8",0),new Rect(anchor.x-length,anchor.y-4*scale,length,8*scale));}
                art.Draw("wpn_sling-stone_spin_8",anchor,scale,Clock);
                GUI.matrix=before;
            }
            VisualQuality.SlingRelease(arena,feet,scale,PlayerLeft,snap.Time,FeedbackEnabled);
            foreach(var weapon in snap.Weapons)
            {
                if(weapon.WeaponId=="bible" && weapon.ActiveUntil>snap.Time)
                {
                    var profile=host.Session.AttackProfile("bible");
                    for(var i=0;i<profile.Amount;i++)
                    {
                        var angle=snap.Time*2.5*WeaponRules.Get("bible",host.Session.Inventory.Weapons["bible"]).RotationSpeedMultiplier+i*Math.PI*2/profile.Amount;
                        var radius=snap.Tuning.BibleOrbitRadius*profile.AreaMultiplier;
                        var pos=Point(snap.PlayerX+(float)(Math.Cos(angle)*radius),snap.PlayerY+(float)(Math.Sin(angle)*radius));
                        if(FeedbackEnabled&&AdamVisualQualityArt.TryGetClip(AdamPolishContacts.Page,out _))
                        {
                            var tint=GUI.color;try{for(var age=2;age>=1;age--){var behind=angle-age*12*Mathf.Deg2Rad;var ghost=Point(snap.PlayerX+(float)(Math.Cos(behind)*radius),snap.PlayerY+(float)(Math.Sin(behind)*radius));GUI.color=new Color(1,1,1,age==1?.45f:.2f);art.Draw("wpn_bible-book_spin_16",ghost,scale,Clock);}}finally{GUI.color=tint;}
                        }
                        art.Draw("fx_bible-glow_static_20",pos,scale); art.Draw("wpn_bible-book_spin_16",pos,scale,Clock);
                    }
                }
                if(weapon.WeaponId=="jawbone")
                {
                    AdamVisualQualityWorld.SwingFx(art,JawboneSwing,arena,feet,scale,PlayerLeft,snap.Time,FeedbackEnabled);
                    JawboneSwing.Draw(art,arena,feet,scale,PlayerLeft,snap.Time,
                        (float)(snap.Tuning.JawboneRange*host.Session.AttackProfile("jawbone").AreaMultiplier),FeedbackEnabled);
                }
            }
            beforeTelegraphs?.Invoke();
            if(FeedbackEnabled)
            {
                if(snap.Charges!=null)foreach(var charge in snap.Charges){AdamChargeGeometry band;var visible=host.Session.RoundNumber==12?AdamChariotWarning.TryNormalGeometry(snap,charge,out band):AdamChargeLane.TryGeometry(snap,charge,out band);if(visible)AdamVisualQualityWorld.WarningBand(projection,band,true);}
                for(var direction=0;direction<3;direction++)if(TryChainGeometry(snap,bossVisual,host.Session.Stage,direction,out var band))AdamVisualQualityWorld.WarningBand(projection,band,true);
                if(AdamChariotWarning.TryBossGeometry(chariotVisual,out var bossBand))AdamVisualQualityWorld.WarningBand(projection,bossBand,true);
            }
            if(bossVisual?.Telegraphing==true && bossVisual.Rules.UsesSlam && host.Session.Stage=="boss")
                danger.Draw(projection,new Vector2(bossVisual.TargetX,bossVisual.TargetY),(float)BossDefinition.SlamRadius);
            if(snap.Charges!=null)foreach(var charge in snap.Charges)
            {
                AdamChargeGeometry geometry;var visible=host.Session.RoundNumber==12?AdamChariotWarning.TryNormalGeometry(snap,charge,out geometry):AdamChargeLane.TryGeometry(snap,charge,out geometry);
                if(visible)chargeLane.Draw(projection,geometry);
            }
            // Mandatory chain-boss information is above actors, weapons and optional particles, below HUD.
            // Neither feedback nor low-FX options suppress warnings or actual hostile projectiles.
            if(snap.HostileProjectiles!=null)foreach(var projectile in snap.HostileProjectiles)
                if(AdamChainWarning.ProjectileVisible(projectile,host.Session.Stage))
                {
                    var anchor=AdamChainWarning.ProjectilePoint(projection,projectile);var before=GUI.matrix;
                    try
                    {
                        GUI.matrix=before*AdamChainWarning.ProjectileMatrix(anchor+arena.position,projectile);
                        var seconds=AdamChainWarning.ProjectileSeconds(snap,projectile);
                        if(UsesWeaverArt(bossVisual))ExodusR06Art().Draw(AdamExodusR06Art.Projectile,anchor,scale,seconds);
                        else{if(r09==null)r09=new AdamR09Art();r09.Draw(AdamR09Art.Projectile,anchor,scale,seconds);}
                    }
                    finally{GUI.matrix=before;}
                }
            for(var direction=0;direction<3;direction++)
                if(TryChainGeometry(snap,bossVisual,host.Session.Stage,direction,out var chain))AdamChainWarning.Draw(projection,chain);
            // Mandatory chariot lane shares the existing outlined arrows, above all optional FX and below HUD.
            if(AdamChariotWarning.TryBossGeometry(chariotVisual,out var chariotGeometry))chariotLane.Draw(projection,chariotGeometry);
            GUI.EndGroup();
        }
        public static double BossDeathDuration(BossEncounterState boss)=>UsesWeaverArt(boss)?AdamExodusR06Art.DeathSeconds:4d/6;
        public static bool BossDeathVisible(double deathAt,double clock,BossEncounterState boss=null)=>deathAt>=0 && clock<deathAt+BossDeathDuration(boss);
        public static string BossClip(string stage,BossEncounterState boss,double impactAt,double clock,out int frame)
        {
            frame=-1;
            var move=boss?.Rules.Number==9?AdamR09Art.BossMove:boss?.Rules.Number==6?AdamR06Art.BossMove:AdamR03Art.BossMove;
            var windup=boss?.Rules.Number==9?AdamR09Art.BossWindup:boss?.Rules.Number==6?AdamR06Art.BossWindup:AdamR03Art.BossWindup;
            var impact=boss?.Rules.Number==9?AdamR09Art.BossImpact:boss?.Rules.Number==6?AdamR06Art.BossImpact:AdamR03Art.BossImpact;
            if(stage=="boss_intro"){frame=0;return move;}
            if(boss?.Telegraphing==true)
            {frame=Mathf.Clamp((int)Math.Floor((boss.BossElapsed-(boss.NextAttackAt-boss.Rules.TelegraphDuration))/(boss.Rules.TelegraphDuration/3)+1e-8),0,2);return windup;}
            if(impactAt>=0 && clock>=impactAt && clock-impactAt<.2){frame=Mathf.Clamp((int)Math.Floor((clock-impactAt)*10+1e-8),0,1);return impact;}
            return move;
        }
    }
}
