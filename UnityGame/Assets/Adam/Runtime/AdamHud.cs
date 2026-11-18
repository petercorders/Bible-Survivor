using System;
using System.Globalization;
using BibleSurvivor.Core;
using UnityEngine;

namespace BibleSurvivor.Unity
{
    // Presentation only: all values come from the current aggregate; no local battle clock.
    public static class AdamHud
    {
        public static bool PolishEnabled=>AdamVisualQualityArt.TryGetClip("ico_wpn_bible_emblem_32",out _);
        public static Color GuiColor(Color color)=>QualitySettings.activeColorSpace==ColorSpace.Linear?color.linear:color;
        public static readonly Color Ink0=GuiColor(new Color32(11,14,19,255)), Ink1=GuiColor(new Color32(20,25,34,255)),
            Ink2=GuiColor(new Color32(28,34,45,255)), Ink4=GuiColor(new Color32(58,69,88,255)),
            Primary=new Color32(244,239,225,255), Secondary=new Color32(207,195,160,255),
            Muted=new Color32(154,160,174,255), Gold=new Color32(255,232,160,255),
            Lapis=new Color32(138,166,214,255);
        public static float Fraction(double value,double maximum)=>maximum<=0?0:Mathf.Clamp01((float)(value/maximum));
        public static string Clock(double seconds)
        {
            var whole=(int)Math.Floor(Math.Max(0,seconds));
            return (whole/60).ToString(CultureInfo.InvariantCulture)+":"+(whole%60).ToString("D2",CultureInfo.InvariantCulture);
        }
        public static string Timer(double worldSeconds,double? bossElapsed)=>Clock(bossElapsed??Math.Min(180,worldSeconds));
        public static string Timer(AdamSession session,double worldSeconds)=>Timer(worldSeconds,session?.Boss?.BossElapsed);
        public static string TimerRole(AdamSession session)=>session?.Boss!=null?"보스 경과":"일반 / 3:00";
        public static Rect Health(float width)=>Health(width,PolishEnabled);
        public static Rect Health(float width,bool polish)=>polish?new Rect(20,14,Mathf.Min(260,Mathf.Floor(width*.32f/2)*2),22):new Rect(20,40,Mathf.Min(260,width*.32f),18);
        // At 660 the real 10000/10000 fixture cannot fit between the bar and centred timer.
        public static Rect HealthValue(float width)=>HealthValue(width,PolishEnabled);
        public static Rect HealthValue(float width,bool polish)=>new Rect(26,14,Health(width,polish).width-12,22);
        public static Rect TimerRect(float width)=>new Rect(width/2-66,14,132,38);
        public static Rect Pause(float width,bool mobile)=>mobile?new Rect(width-175,54,150,66):new Rect(width-146,16,126,44);
        public static Rect Experience(float width)=>Experience(width,PolishEnabled);
        public static Rect Experience(float width,bool polish)=>polish?new Rect(94,83,width-214,16):new Rect(94,86,width-114,16);
        public static Rect ExperienceValue(float width)=>new Rect(width-112,80,92,22);
        public static Rect PauseSetting(Rect panel,int slot)=>new Rect(panel.x+30+slot%2*286,panel.y+112+slot/2*44,274,36);
        public static Rect BossBacking(float width)=>BossBacking(width,PolishEnabled);
        public static Rect BossBacking(float width,bool polish)=>new Rect(28,117,width-56,polish?42:64);
        public static Rect PausePanel(float width,float height)=>new Rect(width/2-310,height/2-160,620,340);
        public static Rect PauseResume(float width,float height,bool mobile)=>PauseResume(width,height,mobile,PolishEnabled);
        public static Rect PauseResume(float width,float height,bool mobile,bool polish)=>mobile||!polish?new Rect(width/2-125,height/2+20,250,mobile?66:56):new Rect(width/2-125,PausePanel(width,height).y+270,250,50);
        public static Rect Footer(float width,float height)=>new Rect(0,height-78,width,78);
        public static Rect InventorySlot(float height,int slot,bool weapon)
        {
            if(slot<0||slot>=(weapon?3:10))throw new ArgumentOutOfRangeException(nameof(slot));
            return weapon?new Rect(20+slot*38,height-66,36,36):new Rect(146+slot%5*30,height-72+slot/5*34,26,26);
        }
        public static string Icon(string id)
        {
            if(id=="bible"||id==EvolutionRules.Bible)return "ico_wpn_bible_static_16";
            if(id=="jawbone"||id==EvolutionRules.Jawbone)return "ico_wpn_jawbone_static_16";
            if(id=="sling"||id==EvolutionRules.Sling)return "ico_wpn_sling_static_16";
            if(id=="scroll"||id=="hair"||id=="stones")return "ico_psv_"+id+"_static_16";
            if(id=="gold"||id?.StartsWith(AdamSession.GrowthGoldChoicePrefix,StringComparison.Ordinal)==true)return "ico_res_gold_static_16";
            return null;
        }
        public static int SurfaceState(bool enabled,bool focused,bool hovered,bool pressed)=>!enabled?3:pressed?2:focused||hovered?1:0;
        // Return source margins in GUI top-left coordinates. Manifest order is L,B,R,T.
        public static Vector4 GuiMargins(int[] borders,int scale)=>new Vector4(borders[0]*scale,borders[3]*scale,borders[2]*scale,borders[1]*scale);
        public static string CardName(string card)
        {
            var heading=card.Split('\n')[0];var dot=heading.IndexOf(". ",StringComparison.Ordinal);
            if(dot>=0)heading=heading.Substring(dot+2);
            var tag=heading.IndexOf('|');return (tag<0?heading:heading.Substring(0,tag)).Trim();
        }
        public static string CardTag(string card)
        {var heading=card.Split('\n')[0];var tag=heading.IndexOf('|');return tag<0?"":heading.Substring(tag+1).Trim();}

        public static void Fill(Rect rect,Color color)
        {var old=GUI.color;GUI.color=color;GUI.DrawTexture(rect,Texture2D.whiteTexture);GUI.color=old;}
        public static void Panel(AdamArt fallback,Rect rect,string id="ui_panel_9slice_16",int state=0,int scale=2)
        {
            if(!AdamVisualQualityArt.TryGetClip(id,out var clip))
            {
                // Only used while the approved sidecar has not been imported; never invent clip metadata.
                if(id=="ui_keycap_9slice_12"||id=="ui_chip_9slice_8")
                {Fill(rect,Ink4);Fill(new Rect(rect.x+1,rect.y+1,rect.width-2,rect.height-3),Ink1);return;}
                fallback.Panel(rect,id,state,scale);return;
            }
            var sprite=AdamVisualQualityArt.Sprite(id,Mathf.Clamp(state,0,clip.frames-1));var b=clip.borders;
            var xs=new[]{0,b[0],clip.width-b[2],clip.width};
            var ys=new[]{0,b[3],clip.height-b[1],clip.height};
            var margins=GuiMargins(b,scale);
            var dx=new[]{rect.x,rect.x+margins.x,rect.xMax-margins.z,rect.xMax};
            var dy=new[]{rect.y,rect.y+margins.y,rect.yMax-margins.w,rect.yMax};
            for(var row=0;row<3;row++)for(var col=0;col<3;col++)
            {
                var sw=xs[col+1]-xs[col];var sh=ys[row+1]-ys[row];if(sw<=0||sh<=0)continue;
                for(var y=dy[row];y<dy[row+1]-.01f;y+=sh*scale)
                for(var x=dx[col];x<dx[col+1]-.01f;x+=sw*scale)
                {
                    var w=Mathf.Min(sw*scale,dx[col+1]-x);var h=Mathf.Min(sh*scale,dy[row+1]-y);
                    var uv=new Rect((sprite.rect.x+xs[col])/sprite.texture.width,
                        (sprite.rect.y+clip.height-ys[row]-h/scale)/sprite.texture.height,
                        w/scale/sprite.texture.width,h/scale/sprite.texture.height);
                    GUI.DrawTextureWithTexCoords(new Rect(x,y,w,h),sprite.texture,uv,true);
                }
            }
        }
        public static void Image(AdamArt fallback,string id,Rect rect,int state=0)
        {
            if(AdamVisualQualityArt.TryGetClip(id,out var clip))AdamArt.Blit(AdamVisualQualityArt.Sprite(id,Mathf.Clamp(state,0,clip.frames-1)),rect);
            else if(id=="ui_rule_tile_2x1")Fill(rect,state==0?new Color32(90,56,32,255):Ink4);
            else AdamArt.Blit(fallback.Sprite(id=="ui_bar-boss_tile_1x4"?"ui_bar-hp_tile_1x2":id,state),rect);
        }
        public static void Rule(AdamArt art,Rect rect,bool bronze=false)=>Image(art,"ui_rule_tile_2x1",rect,bronze?0:1);
        public static void Bar(AdamArt art,Rect rect,string tile,double fraction)
        {
            Panel(art,rect,"ui_bar-frame_9slice_8x6",0,1);
            var inside=new Rect(rect.x+2,rect.y+2,rect.width-4,rect.height-4);
            Image(art,tile,inside,0);
            if(fraction>0)Image(art,tile,new Rect(inside.x,inside.y,inside.width*Mathf.Clamp01((float)fraction),inside.height),1);
        }
        public static void Key(AdamArt art,Rect rect,string key,GUIStyle text,bool focus=false)
        {
            Panel(art,rect,"ui_keycap_9slice_12",focus?1:0,2);
            var old=text.alignment;text.alignment=TextAnchor.MiddleCenter;GUI.Label(rect,key,text);text.alignment=old;
        }
        public static void Item(AdamArt art,Rect rect,string id,int level,int maximum,bool evolved,GUIStyle text)
        {
            var wellSize=rect.width>=32?36:18;
            Image(art,"ui_slot_18",new Rect(rect.center.x-wellSize/2,rect.center.y-wellSize/2,wellSize,wellSize),evolved||level>=maximum?3:1);
            var icon=Icon(id);var size=rect.width>=32?32:16;
            var inset=new Rect(rect.center.x-size/2,rect.center.y-size/2,size,size);
            if(icon!=null)Image(art,icon,inset);
            else
            {
                var name=AdamChoiceText.ItemName(id);var old=text.alignment;text.alignment=TextAnchor.MiddleCenter;
                GUI.Label(inset,new GUIContent(name.Substring(0,1),name+" "+level),text);text.alignment=old;
            }
            if(evolved)Image(art,"ui_badge-evolve_8",new Rect(rect.xMax-8,rect.y-2,8,8));
            for(var i=0;i<maximum;i++)Image(art,"ui_pip_2",new Rect(rect.x+i*4,rect.yMax+2,4,4),i<level?1:0);
        }
    }
}
