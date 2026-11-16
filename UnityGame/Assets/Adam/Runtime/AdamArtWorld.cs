using System;
using System.Collections.Generic;
using BibleSurvivor.Core;
using UnityEngine;

namespace BibleSurvivor.Unity
{
    // Read-only projection of BattleWorld. No collisions, damage callbacks, timers or random calls enter the core.
    public sealed class AdamArtWorld : IDisposable
    {
        AdamR02Art r02;
        AdamR03Art r03;
        AdamR04Art r04;
        AdamR05Art r05;
        AdamR06Art r06;
        readonly AdamChargeLane chargeLane=new AdamChargeLane();
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
        public double PlayerHitUntil {get;private set;}
        long impactCursor;
        public bool EnemyReacting(int id) => reactions.TryGetValue(id,out var until) && Clock<until;
        public const double HitFlashSeconds=.04;
        public float EnemyFlashOpacity(int id) => FeedbackEnabled && reactions.TryGetValue(id,out var until) &&
            Clock<until && until-Clock>.09-HitFlashSeconds+1e-9 ? .65f : 0;
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
            if(bossVisual!=null && nextBoss!=null)
            {
                if(nextBoss.NextAttackAt>bossVisual.NextAttackAt)bossImpactAt=next.Time;
                if(bossVisual.Health>0 && nextBoss.Health<=0)bossDeathAt=next.Time;
            }
            foreach(var e in next.Enemies)if(e.Id==nextBoss?.Rules.BossActorId){bossFeet=new Vector2(e.X,e.Y);bossLeft=nextBoss!=null&&nextBoss.Telegraphing?nextBoss.TargetX<e.X:next.PlayerX<e.X;}
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
                    if(FeedbackEnabled) {Emit("fx_hit-spark_hit_16",position);reactions[impact.TargetId]=Clock+.09;}
                    if(impact.Killed){reportedKills.Add(impact.TargetId);Emit("fx_enemy-death_burst_32",position);}
                }
                if(active && monsters!=null)
                {
                    foreach(var pair in previousPositions)
                        if(!monsters.ContainsKey(pair.Key) && !reportedKills.Contains(pair.Key)) Emit("fx_enemy-death_burst_32",pair.Value);
                    // Direct Session.Kill callers can still remove an enemy without a World impact. Never infer hit sounds/sparks from HP deltas.
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
            if(bossDeathAt>=0 && !host.Session.IsActive && !host.Paused && !host.Suspended && Clock<bossDeathAt+4d/6)
                Clock=Math.Min(bossDeathAt+4d/6,Clock+Math.Max(0,Math.Min(delta,.1f)));
            foreach(var effect in Effects) if(effect.Active && Clock-effect.Started>=1) effect.Active=false;
        }
        void Emit(string clip, Vector2 pos)
        { effectPool.Emit(clip,pos,Clock,1); }
        public double PlayerSeconds => PlayerMotion=="death" ? Math.Max(0,Clock-deathAt) : Clock;
        public void Clear()
        {
            source=null; Snapshot=null; Clock=0; deathAt=-1; wasActive=false; PlayerMotion="idle";
            previousPositions.Clear();reactions.Clear();frameImpacts.Clear();reportedKills.Clear();
            impactCursor=0;ConsumedImpacts=0;DroppedImpacts=0;PlayerHitUntil=0;
            effectPool.Clear();enemySprites.Clear();
            bossVisual=null;bossDeathAt=-1;bossImpactAt=-1;
            chargeFacing.Clear();
        }
        public void Dispose(){Clear();r02?.Dispose();r02=null;danger.Dispose();chargeLane.Dispose();}
        public static string EnemyClip(string id)
        {
            switch(id) { case "a1":return "enm_a1-small-snake_move_32"; case "a2":return "enm_a2-fruit-moth_fly_32"; case "a3":return "enm_a3-thorn-beast_walk_32";
                case "b1":case "b2":case "b3":return AdamRoundPresentation.R02ClipId(id);
                case "c1":case "c2":case "c3":case "nephilim_tyrant":return AdamR03Art.EnemyClip(id);
                case "d1":case "d2":case "d3":return AdamR04Art.EnemyClip(id);
                case "e1":case "e2":case "e3":return AdamR05Art.EnemyClip(id);
                case "f1":case "f2":case "f3":case "sitnah_keeper":return AdamR06Art.EnemyClip(id);
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
        public void Draw(AdamArt art, AdamHost host, Rect arena, AdamImpactParticles particles = null)
        {
            if(Snapshot==null || source!=host.World) return;
            var snap=Snapshot; var scale=PixelScale(arena); var pixels=32*scale;
            var origin=new Vector2(snap.PlayerX,snap.PlayerY);
            var projection=new AdamWorldProjection(arena,origin);
            Vector2 Point(float x,float y) => projection.Point(x,y);
            GUI.BeginGroup(arena);
            var rx=Mathf.Min(10,arena.width/(float)pixels/2+1); var ry=Mathf.Min(5.625f,arena.height/(float)pixels/2+1);
            // Terrain is deterministic by integer coordinate, not gameplay RNG. Tiles are never rotated/flipped.
            for(var y=Mathf.FloorToInt(origin.y-ry);y<=Mathf.CeilToInt(origin.y+ry);y++)
            for(var x=Mathf.FloorToInt(origin.x-rx);x<=Mathf.CeilToInt(origin.x+rx);x++)
            {
                var variation=(int)((uint)(x*73856093^y*19349663)%4);
                art.Draw("til_eden_grass-"+(char)('a'+variation)+"_static_32",Point(x+.5f,y+.5f),scale);
                if(PathCell(x,y)) art.Draw("til_eden_path-16mask_static_32",Point(x+.5f,y+.5f),scale,frame:PathMask(x,y));
            }
            for(var y=Mathf.FloorToInt(origin.y-ry);y<=Mathf.CeilToInt(origin.y+ry);y++)
            for(var x=Mathf.FloorToInt(origin.x-rx);x<=Mathf.CeilToInt(origin.x+rx);x++)
                if(!PathCell(x,y) && (uint)(x*73856093^y*19349663)%5==0)
                    art.Draw((x+y)%2==0?"dec_eden_tuft-1_11x8":"dec_eden_flowers-1_16x10",Point(x+.5f,y+.2f),scale);
            art.Draw("fx_shadow_static_16x6",Point(snap.PlayerX,snap.PlayerY),scale);
            if(host.Session.IsActive)
            {
                var monsters=host.Session.Targets;var experience=host.Session.DroppedExperience;
                foreach(var enemy in snap.Enemies)
                    if(monsters!=null && monsters.TryGetValue(enemy.Id,out var monster))
                    {
                        var id=monster.MonsterId;
                        if(id=="nephilim_tyrant"||id=="sitnah_keeper"){if(r03==null)r03=new AdamR03Art();r03.Draw(AdamRoundPresentation.Shadow(id),Point(enemy.X,enemy.Y),scale);}
                        else art.Draw(AdamRoundPresentation.Shadow(id),Point(enemy.X,enemy.Y),scale);
                    }
                foreach(var drop in snap.Drops)
                    if(experience.TryGetValue(drop.Id,out var xp))
                        art.Draw(xp>1?"pkp_xp-gem-bonus_idle_8":"pkp_xp-gem-base_idle_8",Point(drop.X,drop.Y),scale,Clock);
                var orderedSprites=PrepareEnemySprites(snap.Enemies,projection);
                for(var spriteIndex=0;spriteIndex<orderedSprites.Count;spriteIndex++)
                {
                    var sprite=orderedSprites[spriteIndex];var enemy=sprite.Enemy;
                    if(monsters!=null && monsters.TryGetValue(enemy.Id,out var monster))
                    {
                        var anchor=sprite.Feet;var before=GUI.matrix;var tint=GUI.color;
                        // GUI.matrix transforms window coordinates even inside the arena group.
                        if(FeedbackEnabled && EnemyReacting(enemy.Id)){GUI.matrix=before*ReactionMatrix(anchor+arena.position,1);GUI.color=new Color(1,.55f,.35f,1);}
                        if(AdamRoundPresentation.R06Enemy(monster.MonsterId))
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
                        else art.Draw(EnemyClip(monster.MonsterId),anchor,scale,Clock,enemy.X>snap.PlayerX);
                        GUI.matrix=before;GUI.color=tint;
                    }
                }
            }
            if(BossDeathVisible(bossDeathAt,Clock))
            {
                if(bossVisual.Rules.Number==6){if(r06==null)r06=new AdamR06Art();r06.Draw(AdamR06Art.BossDeath,Point(bossFeet.x,bossFeet.y),scale,Math.Max(0,Clock-bossDeathAt),bossLeft);}
                else{if(r03==null)r03=new AdamR03Art();r03.Draw(AdamR03Art.BossDeath,Point(bossFeet.x,bossFeet.y),scale,Math.Max(0,Clock-bossDeathAt),bossLeft);}
            }
            // Transparent particles sit above terrain/enemies but below actor/weapons and every HUD/modal control.
            particles?.Draw(projection);
            // All six layers share ONE parent reflection about the registered feet pivot.
            var feet=Point(snap.PlayerX,snap.PlayerY); var matrix=GUI.matrix;
            if(PlayerLeft) GUI.matrix=matrix*Matrix4x4.Translate(new Vector3(feet.x,feet.y,0))*Matrix4x4.Scale(new Vector3(-1,1,1))*Matrix4x4.Translate(new Vector3(-feet.x,-feet.y,0));
            var playerTint=GUI.color;
            if(Clock<PlayerHitUntil) GUI.color=new Color(1,.4f,.4f,1);
            foreach(var layer in art.Index.layers)
                art.Draw("chr_player_layer-"+layer.Replace('_','-')+"_"+PlayerMotion+"_32",feet,scale,PlayerSeconds);
            GUI.color=playerTint;GUI.matrix=matrix;
            if(!host.Session.IsActive) {GUI.EndGroup();return;}
            foreach(var p in snap.Projectiles)
            {
                var anchor=Point(p.X,p.Y);var before=GUI.matrix;
                GUI.matrix=before*FacingMatrix(anchor,new Vector2(p.VelocityX,p.VelocityY));
                art.Draw("fx_sling-trail_static_20x8",anchor,scale);
                art.Draw("wpn_sling-stone_spin_8",anchor,scale,Clock);
                GUI.matrix=before;
            }
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
                        art.Draw("fx_bible-glow_static_20",pos,scale); art.Draw("wpn_bible-book_spin_16",pos,scale,Clock);
                    }
                }
                if(weapon.WeaponId=="jawbone")
                {
                    var at=LatestJawboneBurst(weapon,snap.Time,host.Session.AttackProfile("jawbone").Amount,
                        WeaponRules.Get("jawbone",host.Session.Inventory.Weapons["jawbone"]).BurstInterval);
                    var elapsed=snap.Time-at;
                    if(at<0 || elapsed>=.2)continue;
                    var pos=Point(snap.PlayerX,snap.PlayerY);var before=GUI.matrix;
                    GUI.matrix=before*FacingMatrix(pos,new Vector2(snap.FacingX,snap.FacingY));
                    art.Draw("fx_jawbone-swing-trail_hit_64",pos,scale,elapsed);
                    art.Draw("fx_jawbone-swing-edge_hit_64",pos,scale,elapsed);
                    var clip=art.Clip("fx_jawbone-swing-edge_hit_64");var frame=clip.Frame(elapsed);
                    var attached=pos+new Vector2(clip.attachX[frame]-clip.pivotX,clip.attachY[frame]-(clip.height-clip.pivotY))*scale;
                    art.Draw("wpn_jawbone_held_16",attached,scale);
                    GUI.matrix=before;
                }
            }
            foreach(var effect in Effects)
                if(effect.Active && Clock-effect.Started<art.Clip(effect.Clip).frames/(double)art.Clip(effect.Clip).fps)
                    art.Draw(effect.Clip,Point(effect.Position.x,effect.Position.y),scale,Clock-effect.Started);
            if(bossVisual?.Telegraphing==true && host.Session.Stage=="boss")
                danger.Draw(projection,new Vector2(bossVisual.TargetX,bossVisual.TargetY),(float)BossDefinition.SlamRadius);
            if(snap.Charges!=null)foreach(var charge in snap.Charges)
                if(AdamChargeLane.TryGeometry(snap,charge,out var geometry))chargeLane.Draw(projection,geometry);
            GUI.EndGroup();
        }
        public static bool BossDeathVisible(double deathAt,double clock)=>deathAt>=0 && clock<deathAt+4d/6;
        public static string BossClip(string stage,BossEncounterState boss,double impactAt,double clock,out int frame)
        {
            frame=-1;
            var move=boss?.Rules.Number==6?AdamR06Art.BossMove:AdamR03Art.BossMove;
            var windup=boss?.Rules.Number==6?AdamR06Art.BossWindup:AdamR03Art.BossWindup;
            var impact=boss?.Rules.Number==6?AdamR06Art.BossImpact:AdamR03Art.BossImpact;
            if(stage=="boss_intro"){frame=0;return move;}
            if(boss?.Telegraphing==true)
            {frame=Mathf.Clamp((int)Math.Floor((boss.BossElapsed-(boss.NextAttackAt-BossDefinition.TelegraphSeconds))/(BossDefinition.TelegraphSeconds/3)+1e-8),0,2);return windup;}
            if(impactAt>=0 && clock-impactAt<.2){frame=Mathf.Clamp((int)((clock-impactAt)*10),0,1);return impact;}
            return move;
        }
    }
}
