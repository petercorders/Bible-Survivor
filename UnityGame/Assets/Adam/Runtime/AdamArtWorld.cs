using System;
using System.Collections.Generic;
using BibleSurvivor.Core;
using UnityEngine;

namespace BibleSurvivor.Unity
{
    // Read-only projection of BattleWorld. No collisions, damage callbacks, timers or random calls enter the core.
    public sealed class AdamArtWorld
    {
        public const int EffectCapacity = 64;
        public sealed class Effect
        {
            public bool Active;
            public string Clip;
            public Vector2 Position;
            public double Started;
        }
        public sealed class EffectPool
        {
            public readonly Effect[] Slots = new Effect[EffectCapacity];
            int next;
            public EffectPool() { for(var i=0;i<Slots.Length;i++) Slots[i]=new Effect(); }
            public void Emit(string clip,Vector2 position,double started)
            {
                var e=Slots[next];next=(next+1)%Slots.Length;
                e.Active=false;e.Clip=clip;e.Position=position;e.Started=started;e.Active=true;
            }
            public void Clear()
            {
                next=0;
                foreach(var e in Slots) {e.Active=false;e.Clip=null;e.Position=Vector2.zero;e.Started=0;}
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
        readonly Dictionary<int, double> previousHealth = new Dictionary<int, double>();
        BattleWorld source;
        bool wasActive;
        double deathAt = -1;
        public void Update(AdamHost host, float delta)
        {
            if(host?.World == null) { Clear(); return; }
            if(source != host.World) { Clear(); source=host.World; }
            var next=source.Capture();var active=host.Session.IsActive;var monsters=host.Session.Wave?.Active;
            // A fatal contact (or retaliation opening a choice) can change the arena without advancing Core time.
            if(Snapshot == null || next.Time != Snapshot.Time || active != wasActive || next.Hits != Snapshot.Hits ||
                next.Kills != Snapshot.Kills || next.Collected != Snapshot.Collected || next.DamageTaken != Snapshot.DamageTaken ||
                next.PlayerX != Snapshot.PlayerX || next.PlayerY != Snapshot.PlayerY)
            {
                PlayerMotion = Snapshot != null && (next.PlayerX != Snapshot.PlayerX || next.PlayerY != Snapshot.PlayerY) ? "walk" : "idle";
                Clock=next.Time;
                if(active && monsters!=null)
                {
                    foreach(var pair in previousPositions)
                        if(!monsters.ContainsKey(pair.Key)) Emit("fx_enemy-death_burst_32",pair.Value);
                    foreach(var enemy in monsters)
                        if(previousHealth.TryGetValue(enemy.Key,out var health) && enemy.Value.Health<health && previousPositions.TryGetValue(enemy.Key,out var pos))
                            Emit("fx_hit-spark_hit_16",pos+new Vector2(0,.4f));
                }
                previousPositions.Clear(); previousHealth.Clear();
                foreach(var enemy in next.Enemies) previousPositions[enemy.Id]=new Vector2(enemy.X,enemy.Y);
                if(active && monsters!=null)foreach(var enemy in monsters) previousHealth[enemy.Key]=enemy.Value.Health;
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
            foreach(var effect in Effects) if(effect.Active && Clock-effect.Started>=1) effect.Active=false;
        }
        void Emit(string clip, Vector2 pos)
        { effectPool.Emit(clip,pos,Clock); }
        public double PlayerSeconds => PlayerMotion=="death" ? Math.Max(0,Clock-deathAt) : Clock;
        public void Clear()
        {
            source=null; Snapshot=null; Clock=0; deathAt=-1; wasActive=false; PlayerMotion="idle";
            previousHealth.Clear(); previousPositions.Clear();
            effectPool.Clear();
        }
        public static string EnemyClip(string id)
        {
            switch(id) { case "a1":return "enm_a1-small-snake_move_32"; case "a2":return "enm_a2-fruit-moth_fly_32"; case "a3":return "enm_a3-thorn-beast_walk_32";
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
        public void Draw(AdamArt art, AdamHost host, Rect arena)
        {
            if(Snapshot==null || source!=host.World) return;
            var snap=Snapshot; var scale=PixelScale(arena); var pixels=32*scale;
            var origin=new Vector2(snap.PlayerX,snap.PlayerY);
            Vector2 Point(float x,float y) => new Vector2(Mathf.Round(arena.width/2+(x-origin.x)*pixels), Mathf.Round(arena.height/2-(y-origin.y)*pixels));
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
                var monsters=host.Session.Wave?.Active;var experience=host.Session.DroppedExperience;
                foreach(var enemy in snap.Enemies)
                    if(monsters!=null && monsters.TryGetValue(enemy.Id,out var monster))
                    {
                        var id=monster.MonsterId;
                        art.Draw(id=="a1"?"fx_shadow-snake_static_20x4":id=="a2"?"fx_shadow-small_static_12x4":"fx_shadow-wide_static_24x6",Point(enemy.X,enemy.Y),scale);
                    }
                foreach(var drop in snap.Drops)
                    if(experience.TryGetValue(drop.Id,out var xp))
                        art.Draw(xp>1?"pkp_xp-gem-bonus_idle_8":"pkp_xp-gem-base_idle_8",Point(drop.X,drop.Y),scale,Clock);
                foreach(var enemy in snap.Enemies)
                    if(monsters!=null && monsters.TryGetValue(enemy.Id,out var monster))
                        art.Draw(EnemyClip(monster.MonsterId),Point(enemy.X,enemy.Y),scale,Clock,enemy.X>snap.PlayerX);
            }
            // All six layers share ONE parent reflection about the registered feet pivot.
            var feet=Point(snap.PlayerX,snap.PlayerY); var matrix=GUI.matrix;
            if(PlayerLeft) GUI.matrix=matrix*Matrix4x4.Translate(new Vector3(feet.x,feet.y,0))*Matrix4x4.Scale(new Vector3(-1,1,1))*Matrix4x4.Translate(new Vector3(-feet.x,-feet.y,0));
            foreach(var layer in art.Index.layers)
                art.Draw("chr_player_layer-"+layer.Replace('_','-')+"_"+PlayerMotion+"_32",feet,scale,PlayerSeconds);
            GUI.matrix=matrix;
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
            GUI.EndGroup();
        }
    }
}
