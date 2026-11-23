using System;
using BibleSurvivor.Core;
using UnityEngine;

namespace BibleSurvivor.Unity
{
    public static class AdamBookTitle
    {
        public const string Horizon="bg_book-horizon_static_400x55",Fade="ui_band-fade_static_16x55",Crest="ui_book-crest_static_32";
        public static bool Enabled=>AdamVisualQualityArt.TryGetClip(Horizon,out _)&&AdamVisualQualityArt.TryGetClip(Fade,out _)&&AdamVisualQualityArt.TryGetClip(Crest,out _);
        public static int Book(int? round)=>LaterCampaignBooks.Find(round.GetValueOrDefault()) is LaterCampaignBook book?book.Index:round>=FirstSamuelContent.GlobalRoundNumber?8:round>=RuthContent.GlobalRoundNumber?7:round>=JudgesContent.GlobalRoundNumber?6:round>=JoshuaContent.GlobalRoundNumber?5:round>=DeuteronomyContent.GlobalRoundNumber?4:
            round>=NumbersContent.GlobalRoundNumber?3:round>=LeviticusContent.GlobalRoundNumber?2:round>=ExodusContent.GlobalRoundNumber?1:0;
        public static string Name(int? round)=>LaterCampaignBooks.Find(round.GetValueOrDefault())?.Name??(Book(round) switch {8=>"사무엘상",7=>"룻기",6=>"사사기",5=>"여호수아",4=>"신명기",3=>"민수기",2=>"레위기",1=>"출애굽기",_=>"창세기"});
        // Installed rounds per book.
        public static int Total(int? round)=>LaterCampaignBooks.Find(round.GetValueOrDefault()) is LaterCampaignBook book?Math.Min(book.LastRound,CampaignContent.LastInstalledRound)-book.FirstRound+1:Book(round) switch
        {
            8=>FirstSamuelContent.LastRound-FirstSamuelContent.GlobalRoundNumber+1,
            7=>RuthContent.LastRound-RuthContent.GlobalRoundNumber+1,
            6=>JudgesContent.LastRound-JudgesContent.GlobalRoundNumber+1,
            5=>JoshuaContent.LastRound-JoshuaContent.GlobalRoundNumber+1,
            4=>DeuteronomyContent.LastRound-DeuteronomyContent.GlobalRoundNumber+1,
            3=>NumbersContent.LastRound-NumbersContent.GlobalRoundNumber+1,
            2=>LeviticusContent.LastInstalledRound-LeviticusContent.GlobalRoundNumber+1,
            1=>6,
            _=>9
        };
        public static int Local(int? round)=>LaterCampaignBooks.Find(round.GetValueOrDefault()) is LaterCampaignBook book?round.Value-book.FirstRound+1:round.GetValueOrDefault()-(Book(round) switch
        {
            8=>FirstSamuelContent.GlobalRoundNumber-1,
            7=>RuthContent.GlobalRoundNumber-1,
            6=>JudgesContent.GlobalRoundNumber-1,
            5=>JoshuaContent.GlobalRoundNumber-1,
            4=>DeuteronomyContent.GlobalRoundNumber-1,
            3=>NumbersContent.GlobalRoundNumber-1,
            2=>LeviticusContent.GlobalRoundNumber-1,
            1=>9,
            _=>0
        });
        // The two-frame art strips share their wilderness artwork across the later books.
        public static int ArtFrame(int? round)=>Math.Min(Book(round),1);
        // 0 ahead, 1 cleared, 2 current. No run history is inferred for a null session.
        public static int Pip(int? round,int index)=>!round.HasValue||index+1>Local(round)?0:index+1==Local(round)?2:1;
        public static bool Boss(int book,int index)=>index>=0 && (book switch
        {
            0 or 3 or 6=>index is 2 or 5 or 8,
            1 or 2 or 4 or 5 or 8=>index is 2 or 5,
            7=>index is 2,
            >=9=>index%3==2,
            _=>false
        });
        public static float HorizonX(float width)=>width-800;
        public static float FocalX(float width)=>HorizonX(width)+660;
        public static Rect Ribbon(float width)=>new Rect(Mathf.Round((width-300)/4)*2,width<880?136:150,300,68);
        public static void DrawBand(float width,int? round,bool death,GUIStyle eyebrow,GUIStyle name,GUIStyle count)
        {
            AdamHud.Fill(new Rect(0,0,width,110),AdamHud.Ink1);
            var sprite=AdamVisualQualityArt.Sprite(Horizon,ArtFrame(round));var origin=HorizonX(width);
            GUI.BeginGroup(new Rect(300,0,Mathf.Max(0,width-300),110));
            AdamArt.Blit(sprite,new Rect(origin-300,0,800,110));
            // The reviewed [0,8) source columns repeat with a four-column period.
            for(var x=origin-16;x>284;x-=16)
            {
                var uv=new Rect(sprite.rect.x/sprite.texture.width,sprite.rect.y/sprite.texture.height,8f/sprite.texture.width,55f/sprite.texture.height);
                GUI.DrawTextureWithTexCoords(new Rect(x-300,0,16,110),sprite.texture,uv,true);
            }
            GUI.EndGroup();
            AdamArt.Blit(AdamVisualQualityArt.Sprite(Fade,0),new Rect(300,0,32,110));
            var genesis = Book(round) == 0;
            if (genesis) AdamCinematicArt.DrawEdenSeal(new Vector2(28, 35), 2);
            else AdamArt.Blit(AdamVisualQualityArt.Sprite(Crest,ArtFrame(round)),new Rect(20,22,64,64));
            GUI.Label(new Rect(96,14,204,22),death?"사망 · 이번 도전 종료":"성경 서바이버",eyebrow);
            if (genesis)
                GUI.Label(new Rect(96,38,204,36), death ? "도전 기록" : "시작 무기를 선택하세요", count);
            else
                GUI.Label(new Rect(96,34,204,44),Name(round),name);
            if(round.HasValue && !genesis)
            {
                var end=96+name.CalcSize(new GUIContent(Name(round))).x+12;
                GUI.Label(new Rect(end,50,Mathf.Max(0,300-end),22),Local(round)+" / "+Total(round),count);
            }
            DrawPips(new Vector2(96,82),round);
            AdamHud.Fill(new Rect(0,108,width,2),AdamHud.GuiColor(new Color32(138,90,50,255)));
        }
        public static void DrawPips(Vector2 position,int? round)
        {
            for(var i=0;i<Total(round);i++)
            {
                var state=Pip(round,i);var x=position.x+i*18;
                var border=state==2?AdamHud.GuiColor(AdamHud.Lapis):AdamHud.Ink0;
                var fill=state==1?AdamHud.GuiColor(AdamHud.Gold):state==2?AdamHud.Ink1:AdamHud.Ink4;
                if(Boss(Book(round),i))
                {
                    for(var row=0;row<6;row++)
                    {
                        var half=row<3?row+1:6-row;
                        AdamHud.Fill(new Rect(x+6-half*2,position.y+row*2,half*4,2),border);
                        if(half>1)AdamHud.Fill(new Rect(x+8-half*2,position.y+row*2,half*4-4,2),fill);
                    }
                }
                else{AdamHud.Fill(new Rect(x,position.y,12,12),border);AdamHud.Fill(new Rect(x+2,position.y+2,8,8),fill);}
            }
        }
    }
}
