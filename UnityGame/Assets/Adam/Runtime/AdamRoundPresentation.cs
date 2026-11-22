using System;
using BibleSurvivor.Core;

namespace BibleSurvivor.Unity
{
    public static class AdamRoundPresentation
    {
        public static string Name(int round)=>round switch
        {
            1=>"아담",2=>"가인",3=>"노아",4=>"아브라함",5=>"롯",6=>"이삭",7=>"야곱",8=>"요셉",9=>"유다",
            10=>"십브라·부아",11=>"요게벳",12=>"모세",13=>"미리암",14=>"아론",15=>"브살렐",
            16=>"아론",17=>"정결의 길",18=>"속죄일",19=>"이웃 돌봄",20=>"희년",21=>"서원의 끝",
            22=>"인구 조사",23=>"광야 행진",24=>"불평의 진영",25=>"정탐꾼",26=>"긴 광야",27=>"바위의 물",
            28=>"놋뱀",29=>"모압 평야",30=>"요단 앞",
            31=>"모세의 회고",32=>"두 돌판",33=>"기억하라",34=>"도피성",35=>"축복과 저주",36=>"느보산",
            37=>"요단 도하",38=>"길갈의 돌",39=>"여리고 성벽",40=>"아이 성",41=>"기브온의 해",42=>"세겜 언약",
            43=>"옷니엘",44=>"에훗",45=>"드보라",46=>"기드온의 양털",47=>"삼백 용사",48=>"입다",49=>"마노아의 제단",50=>"삼손과 사자",
            51=>"삼손의 마지막",52=>"모압에서 돌아옴",53=>"보아스의 밭",54=>"타작마당",
            55=>"한나의 기도",56=>"실로의 부르심",57=>"빼앗긴 언약궤",58=>"사울의 기름부음",59=>"믹마스의 요나단",60=>"엘라 골짜기",
            61=>"기브온 못가",62=>"오벧에돔의 집",63=>"나단의 책망",64=>"에브라임 수풀",65=>"아벨의 지혜로운 여인",
            66=>"녹두밭과 베들레헴 우물",67=>"솔로몬의 성전",68=>"갈라진 왕국",69=>"벧엘의 제단",70=>"갈멜과 로뎀나무",
            71=>"굴에 숨긴 선지자들",72=>"나봇의 포도원",73=>"엘리사의 겉옷과 기름 그릇",74=>"요단의 일곱 번",
            75=>"도단의 포위",76=>"성문에 전한 소식",77=>"예후와 바알",78=>"사마리아 함락",79=>"랍사게의 위협",80=>"다시 찾은 율법책",
            81=>"예루살렘 함락",82=>"족보 속 야베스의 기도",83=>"레위 찬양대",84=>"요단 동편 지파",
            85=>"아비야의 부르짖음",86=>"아사와 끝없는 행렬",87=>"브라가 골짜기",88=>"웃시야의 망대",
            89=>"오뎃과 돌아온 포로",90=>"므낫세의 회개",
            91=>"귀환과 성전의 기초",92=>"다시 시작한 성전 공사",93=>"아하와 강가의 금식",
            94=>"밤의 성벽 순찰",95=>"성벽을 쌓는 손",96=>"오노 평지의 초대",
            97=>"왕후 에스더",98=>"이 때를 위함",99=>"뒤집힌 제비",100=>"욥의 시련",
            101=>"엘리바스의 밤 환상",102=>"폭풍 속 응답과 회복",
            103=>"시냇가의 나무와 두 길",104=>"사망의 음침한 골짜기",105=>"흔들리는 산과 성의 강",
            106=>"숨어서 쏘는 독한 말",107=>"미끄러질 뻔한 걸음",108=>"지존자의 은밀한 곳",
            109=>"끊어진 올무",110=>"바벨론 강가의 수금",
            111=>"흑암도 숨기지 못함",112=>"개미에게 가라",113=>"지혜와 미련함의 두 초대",114=>"작고 지혜로운 넷",
            115=>"해 아래 새것이 없음",116=>"지혜와 우매",117=>"곤고한 날이 이르기 전에",
            118=>"지키지 못한 포도원",119=>"포도원의 꽃이 필 때",120=>"레바논에서 내려오라",
            121=>"소명과 숯불",122=>"연기 나는 두 부지깽이",123=>"바벨론에 대한 신탁",
            124=>"밤이 어떻게 되었느냐",125=>"산 위의 잔치",126=>"그 날을 앞서 맛보다",
            127=>"거짓을 피난처로 삼은 자들",128=>"우상을 만드는 장인",129=>"일어나 빛을 발하라",
            130=>"소명과 살구나무 가지",
            131=>"썩은 베띠",132=>"바스훌과 차꼬",133=>"하나냐의 나무 멍에",134=>"다시 쓴 두루마리",
            135=>"웅덩이의 예레미야와 에벳멜렉",136=>"애굽으로 내려가지 말라",137=>"갈그미스의 애굽",
            138=>"바벨론에 대한 신탁",139=>"홀로 앉은 성",140=>"성문의 탄식",
            141=>"아침마다 새로운 긍휼",142=>"그발 강가와 두루마리",143=>"성전 은밀한 방의 환상",
            144=>"거짓 선지자와 회칠한 담",145=>"무너진 데를 막아 설 사람",146=>"두로를 위한 애가",
            147=>"레바논의 백향목 비유",148=>"목자 없는 양",149=>"마른 뼈 골짜기",150=>"북쪽 끝의 군세와 성전의 강",
            151=>"왕의 음식을 거절",152=>"큰 신상의 꿈",153=>"풀무불",154=>"벨사살의 잔치와 벽의 글씨",
            155=>"사자 굴",156=>"바다에서 올라온 네 짐승",157=>"숫양과 숫염소",158=>"기도와 일흔 이레",
            159=>"스무하루의 싸움과 미가엘의 도움",160=>"호세아 · 다시 찾은 아내",
            161=>"호세아 · 바람을 심는 백성",162=>"호세아 · 사망아 네 재앙이 어디 있느냐",
            163=>"요엘 · 영을 부어 주심",164=>"아모스 · 정의를 물 같이",165=>"오바댜 · 에돔의 교만",
            166=>"요나 · 물고기 배 속의 기도",167=>"요나 · 니느웨의 회개",168=>"요나 · 박넝쿨 그늘",
            169=>"미가 · 침상에서 꾀하는 악",170=>"미가 · 죄를 깊은 바다에 던지심",
            _=>throw new InvalidOperationException("Round is outside installed content")
        };
        public const string ChapterHeading="창세기 완료 · 진행 저장됨";
        public const string ChapterDetail="진행 유지 · 출애굽기는 현재 성장·장비로 계속\n새 도전 선택 시 성장·장비 초기화";
        public const string ContentHeading="출애굽기 6 완료 · 진행 저장됨";
        public const string ExodusHeading="출애굽기 완료 · 성막 완성";
        public const string ContentDetail="완료 골드 +280 · 진행 유지\n레위기 미구현 · 다음 책 준비 중\n새 도전 선택 시 성장·장비 초기화";
        public const string ExodusDetail="완료 골드 +280 · 레위기는 현재 성장·장비로 계속\n새 도전 선택 시 성장·장비 초기화";
        public const string LeviticusStartCard="레위기 시작 (Enter)";
        public const string NumbersStartCard="민수기 시작 (Enter)";
        public const string DeuteronomyStartCard="신명기 시작 (Enter)";
        public const string JoshuaStartCard="여호수아 시작 (Enter)";
        public const string JudgesStartCard="사사기 시작 (Enter)";
        public const string RuthStartCard="룻기 시작 (Enter)";
        public const string FirstSamuelStartCard="사무엘상 시작 (Enter)";
        public const string LeviticusHeading="레위기 완료 · 진행 저장됨";
        public const string NumbersHeading="민수기 완료 · 진행 저장됨";
        public const string DeuteronomyHeading="신명기 완료 · 진행 저장됨";
        public const string JoshuaHeading="여호수아 완료 · 진행 저장됨";
        public const string JudgesHeading="사사기 완료 · 진행 저장됨";
        public const string RuthHeading="룻기 완료 · 진행 저장됨";
        public static readonly string CampaignEndHeading=EndHeading(CampaignContent.LastInstalledRound);
        static string EndHeading(int round)=>BookLabel(round)+" 완료 · 진행 저장됨";
        const string ResetDetail="\n새 도전 선택 시 성장·장비 초기화";
        static readonly string LeviticusDetail="완료 골드 +"+CampaignContent.Round(LeviticusContent.LastInstalledRound).CompletionGold;
        static readonly string NumbersDetail="완료 골드 +"+CampaignContent.Round(NumbersContent.LastRound).CompletionGold;
        static readonly string DeuteronomyDetail="완료 골드 +"+CampaignContent.Round(DeuteronomyContent.LastRound).CompletionGold;
        static readonly string JoshuaDetail="완료 골드 +"+CampaignContent.Round(JoshuaContent.LastRound).CompletionGold;
        static readonly string JudgesDetail="완료 골드 +"+CampaignContent.Round(JudgesContent.LastRound).CompletionGold;
        static readonly string RuthDetail="완료 골드 +"+CampaignContent.Round(RuthContent.LastRound).CompletionGold;
        static readonly string CampaignEndDetail=EndDetail(CampaignContent.LastInstalledRound);
        static string EndDetail(int round)
        {
            var total=AdamBookTitle.Total(round);
            var completed=total==1?"1":"1–"+total;
            return "완료 골드 +"+CampaignContent.Round(round).CompletionGold+
                " · 진행 유지\n"+AdamBookTitle.Name(round)+" "+completed+" 라운드 클리어"+ResetDetail;
        }
        public static string CompletionHeading(bool genesisComplete,bool exodusComplete,bool leviticusComplete=false,bool contentComplete=false,
            bool numbersComplete=false,bool deuteronomyComplete=false,bool joshuaComplete=false,bool judgesComplete=false,bool ruthComplete=false)
            =>genesisComplete?ChapterHeading:exodusComplete?ExodusHeading:leviticusComplete?LeviticusHeading:numbersComplete?NumbersHeading:
                deuteronomyComplete?DeuteronomyHeading:joshuaComplete?JoshuaHeading:judgesComplete?JudgesHeading:ruthComplete?RuthHeading:
                contentComplete?CampaignEndHeading:ContentHeading;
        public static string CompletionDetail(bool genesisComplete,bool exodusComplete,bool leviticusInstalled=false,bool leviticusComplete=false,bool numbersInstalled=false,bool contentComplete=false,
            bool numbersComplete=false,bool deuteronomyInstalled=false,bool deuteronomyComplete=false,bool joshuaInstalled=false,
            bool joshuaComplete=false,bool judgesInstalled=false,bool judgesComplete=false,bool ruthInstalled=false,bool ruthComplete=false,
            bool firstSamuelInstalled=false)
        {
            if(genesisComplete)return ChapterDetail;
            if(exodusComplete)return leviticusInstalled?ExodusDetail:ContentDetail;
            if(leviticusComplete)return LeviticusDetail+(numbersInstalled?" · 민수기는 현재 성장·장비로 계속":" · 진행 유지\n레위기 1–6 라운드 클리어")+ResetDetail;
            if(numbersComplete)return NumbersDetail+(deuteronomyInstalled?" · 신명기는 현재 성장·장비로 계속":" · 진행 유지\n민수기 1–9 라운드 클리어")+ResetDetail;
            if(deuteronomyComplete)return DeuteronomyDetail+(joshuaInstalled?" · 여호수아는 현재 성장·장비로 계속":" · 진행 유지\n신명기 1–6 라운드 클리어")+ResetDetail;
            if(joshuaComplete)return JoshuaDetail+(judgesInstalled?" · 사사기는 현재 성장·장비로 계속":" · 진행 유지\n여호수아 1–6 라운드 클리어")+ResetDetail;
            if(judgesComplete)return JudgesDetail+(ruthInstalled?" · 룻기는 현재 성장·장비로 계속":" · 진행 유지\n사사기 1–9 라운드 클리어")+ResetDetail;
            if(ruthComplete)return RuthDetail+(firstSamuelInstalled?" · 사무엘상은 현재 성장·장비로 계속":" · 진행 유지\n룻기 1–3 라운드 클리어")+ResetDetail;
            return contentComplete?CampaignEndDetail:ContentDetail;
        }
        // The book-completion panel reads these so tests exercise the same host wiring as the game.
        public static int NextChapter(AdamHost host)=>host.Session?.ChapterComplete==true?ExodusContent.GlobalRoundNumber:
            host.ExodusComplete&&host.LeviticusInstalled?LeviticusContent.GlobalRoundNumber:
            host.LeviticusComplete&&host.NumbersInstalled?NumbersContent.GlobalRoundNumber:
            host.NumbersComplete&&host.DeuteronomyInstalled?DeuteronomyContent.GlobalRoundNumber:
            host.DeuteronomyComplete&&host.JoshuaInstalled?JoshuaContent.GlobalRoundNumber:
            host.JoshuaComplete&&host.JudgesInstalled?JudgesContent.GlobalRoundNumber:
            host.JudgesComplete&&host.RuthInstalled?RuthContent.GlobalRoundNumber:
            host.RuthComplete&&host.FirstSamuelInstalled?FirstSamuelContent.GlobalRoundNumber:host.UpcomingLaterBook?.FirstRound??0;
        public static string CompletionHeading(AdamHost host)=>host.UpcomingLaterBook!=null
            ?AdamBookTitle.Name(host.Session.RoundNumber)+" 완료 · 진행 저장됨"
            :host.ContentComplete&&!host.ExodusComplete?EndHeading(host.Session.RoundNumber):CompletionHeading(host.Session?.ChapterComplete==true,host.ExodusComplete,host.LeviticusComplete,
            host.ContentComplete,host.NumbersComplete,host.DeuteronomyComplete,host.JoshuaComplete,host.JudgesComplete,host.RuthComplete);
        public static string CompletionDetail(AdamHost host)=>host.UpcomingLaterBook!=null
            ?"완료 골드 +"+CampaignContent.Round(host.Session.RoundNumber).CompletionGold+" · "+host.UpcomingLaterBook.Name+"는 현재 성장·장비로 계속"+ResetDetail
            :host.ContentComplete&&!host.ExodusComplete?EndDetail(host.Session.RoundNumber)
            :CompletionDetail(host.Session?.ChapterComplete==true,host.ExodusComplete,host.LeviticusInstalled,
            host.LeviticusComplete,host.NumbersInstalled,host.ContentComplete,host.NumbersComplete,host.DeuteronomyInstalled,host.DeuteronomyComplete,
            host.JoshuaInstalled,host.JoshuaComplete,host.JudgesInstalled,host.JudgesComplete,host.RuthInstalled,host.RuthComplete,
            host.FirstSamuelInstalled);
        // Book-entry save retry: the heading keeps the finished book, the body names the book being entered. Null when no entry retry is pending.
        public static (string Heading,string Body)? EntryRetryModal(AdamHost host)
        {
            static string Body(string book)=>book+" 진입 저장을 확인하지 못했습니다.\n같은 저장을 재시도하거나 저장 기록을 다시 불러오세요.";
            if(host.LaterBookEntryRetry!=null) return (CompletionHeading(host),Body(host.LaterBookEntryRetry.Name));
            if(host.HasFirstSamuelEntryRetry) return (RuthHeading,Body("사무엘상"));
            if(host.HasRuthEntryRetry) return (JudgesHeading,Body("룻기"));
            if(host.HasJudgesEntryRetry) return (JoshuaHeading,Body("사사기"));
            if(host.HasJoshuaEntryRetry) return (DeuteronomyHeading,Body("여호수아"));
            if(host.HasDeuteronomyEntryRetry) return (NumbersHeading,Body("신명기"));
            if(host.HasNumbersEntryRetry) return (LeviticusHeading,Body("민수기"));
            if(host.HasLeviticusEntryRetry) return (ExodusHeading,Body("레위기"));
            return null;
        }
        public static string StartCard(int chapter)=>chapter switch
        {
            ExodusContent.GlobalRoundNumber=>AdamChoiceText.ExodusStartCard,
            LeviticusContent.GlobalRoundNumber=>LeviticusStartCard,
            NumbersContent.GlobalRoundNumber=>NumbersStartCard,
            DeuteronomyContent.GlobalRoundNumber=>DeuteronomyStartCard,
            JoshuaContent.GlobalRoundNumber=>JoshuaStartCard,
            JudgesContent.GlobalRoundNumber=>JudgesStartCard,
            RuthContent.GlobalRoundNumber=>RuthStartCard,
            FirstSamuelContent.GlobalRoundNumber=>FirstSamuelStartCard,
            _ when LaterCampaignBooks.StartsBook(chapter)=>LaterCampaignBooks.Find(chapter).Name+" 시작 (Enter)",
            _=>throw new ArgumentOutOfRangeException(nameof(chapter))
        };
        public static string Title(AdamHost host)=>Title(host?.Session?.RoundNumber??1);
        public static string BookLabel(int round)=>AdamBookTitle.Book(round)==0?AdamBookTitle.Name(round):AdamBookTitle.Name(round)+" "+AdamBookTitle.Local(round);
        public static string Title(int round)=>BookLabel(round)+" / "+Name(round)+" · 3분 시제품";
        public static string ControlHint(int round)=>"WASD / 방향키 이동 · 자동 공격 · Esc 정지 · Enter 재개";
        public static bool R02Enemy(string id)=>id=="b1"||id=="b2"||id=="b3";
        public static bool R03Enemy(string id)=>id=="c1"||id=="c2"||id=="c3"||id=="nephilim_tyrant";
        public static bool R04Enemy(string id)=>id=="d1"||id=="d2"||id=="d3";
        public static bool R05Enemy(string id)=>id=="e1"||id=="e2"||id=="e3";
        public static bool R06Enemy(string id)=>id=="f1"||id=="f2"||id=="f3"||id=="sitnah_keeper";
        public static bool R07Enemy(string id)=>id=="g1"||id=="g2"||id=="g3";
        public static bool R08Enemy(string id)=>id=="h1"||id=="h2"||id=="h3";
        public static bool R09Enemy(string id)=>id=="i1"||id=="i2"||id=="i3"||id=="silver_cup_accuser";
        public static bool R11Enemy(string id)=>id=="ex4"||id=="ex5"||id=="ex6";
        public static bool R12Enemy(string id)=>id=="ex7"||id=="ex8"||id=="ex9"||id=="oppression_chariot";
        public static bool R13Enemy(string id)=>id=="ex10"||id=="ex11"||id=="ex12";
        public static bool R14Enemy(string id)=>id=="ex13"||id=="ex14"||id=="ex15";
        public static bool R15Enemy(string id)=>id=="ex16"||id=="ex17"||id=="ex18"||id=="defilement_weaver";
        public static bool LeviticusEnemy(string id)=>id is "lv1" or "lv2" or "lv3" or "ash_veil" or "oath_wraith";
        public static bool NumbersEnemy(string id)=>id is "nm1" or "nm2" or "nm3" or "dust_tyrant" or "ravine_stalker" or "wilderness_warden";
        // Explicit reviewed R02 sidecar mapping; unknown IDs never fall back to Art03.
        public static string R02ClipId(string id)=>id=="b1"?"enm_b1-envy-eye_move_32":id=="b2"?"enm_b2-rage-hound_run_32":id=="b3"?"enm_b3-cracked-mask_move_32":throw new InvalidOperationException("Unknown R02 monster "+id);
        public static string Shadow(string id)=>id switch
        {
            "a1"=>"fx_shadow-snake_static_20x4",
            "a2" or "b1" or "c1" or "d2" or "e1" or "e3" or "f1" or "g3" or "h1" or "i1" or "i2" or
            "ex2" or "ex5" or "ex10" or "ex13" or "ex16" or "lv2" or "nm2" or "dt2" or "js2" or "jd2" or "rt2" or "sm2"=>"fx_shadow-small_static_12x4",
            "a3" or "b2" or "b3" or "c2" or "c3" or "d1" or "d3" or "e2" or "f2" or "f3" or "g1" or "g2" or
            "h2" or "h3" or "i3" or "ex1" or "ex3" or "ex4" or "ex6" or "ex7" or "ex8" or "ex9" or "ex11" or "ex12" or
            "ex14" or "ex15" or "ex17" or "ex18" or "lv1" or "lv3" or "nm1" or "nm3" or "dt1" or "dt3" or "js1" or "js3" or "jd1" or "jd3" or "rt1" or "rt3" or "sm1" or "sm3"=>"fx_shadow-wide_static_24x6",
            "nephilim_tyrant" or "sitnah_keeper" or "silver_cup_accuser" or "oppression_chariot" or "defilement_weaver" or
            "ash_veil" or "oath_wraith" or "dust_tyrant" or "ravine_stalker" or "wilderness_warden" or
            "oblivion_mist" or "crossroads_sentinel" or "rampart_colossus" or
            "shifting_boundary" or "torrent_serpent" or "ford_gatekeeper" or
            "toppling_pillar" or "chaff_whirl" or "swarm_monarch" or "valley_bronze_brute"=>"fx_shadow-boss_static_40x8",
            _=>throw new InvalidOperationException("Unknown monster "+id)
        };
    }
}
