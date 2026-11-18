using System;
using System.IO;
using BibleSurvivor.Core;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using EnhancedTouch = UnityEngine.InputSystem.EnhancedTouch.Touch;

namespace BibleSurvivor.Unity
{
    public sealed class AdamGame : MonoBehaviour
    {
        public bool AutoInitialize = true;
        public AdamHost Host { get; private set; }
        bool inputReleased;
        BattleWorld inputWorld;
        bool focused = true;
        bool appPaused;
        bool touchStarted;
        int touchId;
        Vector2 touchOrigin;
        int commandFrame = -1;
        string startupError;
        bool drawingMobile;
        Rect previousTouchSafe;
        GUIStyle label, button, title, choiceButton, clearButton, clearChoice;
        AdamFonts fonts;
        AdamArt art;
        public AdamImpactAudio ImpactAudio {get;private set;}
        public AdamImpactParticles ImpactParticles {get;private set;}
        public static Rect Arena(float width,float height) => new Rect(20,116,width-40,height-194);
        public static Rect ScreenViewport() {var safe=Screen.safeArea;return new Rect(safe.x,Screen.height-safe.yMax,safe.width,safe.height);}
        public void PrepareParticles(Rect viewport,bool mobile=false)
        {
            if(ArtWorld.Snapshot==null || ImpactParticles==null)return;
            var scale=mobile?AdamMobileLayout.Scale(viewport):AdamChoiceText.ScreenScale(viewport.width,viewport.height);
            ImpactParticles.Prepare(new AdamWorldProjection(Arena(viewport.width/scale,viewport.height/scale),new Vector2(ArtWorld.Snapshot.PlayerX,ArtWorld.Snapshot.PlayerY)));
        }
        public AdamArtWorld ArtWorld { get; } = new AdamArtWorld();
        // Reviewed death strip: six frames at 8 fps. Keep the actor visible before covering it with restart controls.
        public bool DeathPresentationVisible => Host?.Session != null && !Host.Session.IsActive && Host.Session.CurrentHealth <= 0 &&
            ArtWorld.PlayerMotion == "death" && ArtWorld.PlayerSeconds < 6d / 8;
        static readonly Color TextColor = AdamHud.Primary; // art03 text.0
        public static readonly string[] Weapons = { "bible", "jawbone", "sling" };
        void OnEnable() {EnhancedTouchSupport.Enable();if(Host!=null && ImpactParticles==null)ImpactParticles=new AdamImpactParticles();}
        void OnDisable() {EnhancedTouchSupport.Disable();ImpactAudio?.Stop();ImpactParticles?.Dispose();ImpactParticles=null;}
        void OnDestroy() { ArtWorld.Dispose(); ImpactParticles?.Dispose(); ImpactAudio?.Dispose(); fonts?.Dispose(); }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        static void Bootstrap()
        {
            if (UnityEngine.SceneManagement.SceneManager.GetActiveScene().name == "Adam" && FindFirstObjectByType<AdamGame>() == null)
                new GameObject("Adam Prototype").AddComponent<AdamGame>();
        }
        void Start()
        {
            if (!AutoInitialize || Host != null) return;
            try { Initialize(new AdamHost(AdamCatalog.Load(), new SaveStore(Path.Combine(Application.persistentDataPath, "adam-prototype.json")))); }
            catch (Exception e) { startupError = e.Message; }
        }
        public void Initialize(AdamHost host) { Host = host; inputReleased = false; ImpactAudio?.Dispose(); ImpactAudio=new AdamImpactAudio(gameObject);ImpactParticles?.Dispose();ImpactParticles=new AdamImpactParticles(); }
        public static Vector2 KeyboardMovement(Keyboard keys)
        {
            if (keys == null) return Vector2.zero;
            return Vector2.ClampMagnitude(new Vector2(
                (keys.dKey.isPressed || keys.rightArrowKey.isPressed ? 1 : 0) - (keys.aKey.isPressed || keys.leftArrowKey.isPressed ? 1 : 0),
                (keys.wKey.isPressed || keys.upArrowKey.isPressed ? 1 : 0) - (keys.sKey.isPressed || keys.downArrowKey.isPressed ? 1 : 0)), 1);
        }
        public static Vector2 DragMovement(Vector2 origin, Vector2 position) => Vector2.ClampMagnitude((position - origin) / 80f, 1);
        public void SetLifecycle(bool hasFocus, bool paused)
        {
            focused = hasFocus; appPaused = paused; inputReleased = false; touchStarted = false;
            Host?.SetSuspended(!focused || appPaused);
            if(!focused || appPaused)ImpactAudio?.Stop();
        }
        void OnApplicationFocus(bool value) => SetLifecycle(value, appPaused);
        void OnApplicationPause(bool value) => SetLifecycle(focused, value);
        void OnApplicationQuit() => Host?.Checkpoint();
        void Update()
        {
#if UNITY_STANDALONE && !UNITY_EDITOR
            // The supported PC client area starts at 660x440. Avoid resampling raster fonts at fractional scale.
            if (!Screen.fullScreen && (Screen.width < 660 || Screen.height < 440))
                Screen.SetResolution(Mathf.Max(660, Screen.width), Mathf.Max(440, Screen.height), FullScreenMode.Windowed);
#endif
            if (Host == null) return;
            ReleaseOnWorldChange();
            var safe = Screen.safeArea;
            if (Application.isMobilePlatform && previousTouchSafe != default && safe != previousTouchSafe)
            {
                Host.SetPaused(true); inputReleased = false; touchStarted = false;
            }
#if DEVELOPMENT_BUILD && !UNITY_EDITOR
            if (safe != previousTouchSafe)
                Debug.Log($"ADAM_DISPLAY pixels={Screen.width}x{Screen.height} safe={safe} dpi={Screen.dpi} scale={AdamMobileLayout.Scale(safe)} orientation={Screen.orientation}");
#endif
            previousTouchSafe = safe;
            var keys = Keyboard.current;
            if(keys!=null && keys.f6Key.wasPressedThisFrame) ArtWorld.FeedbackEnabled=!ArtWorld.FeedbackEnabled;
            if(keys!=null && keys.f9Key.wasPressedThisFrame) ImpactParticles.Enabled=!ImpactParticles.Enabled;
            if(keys!=null && keys.f7Key.wasPressedThisFrame) ImpactAudio.Muted=!ImpactAudio.Muted;
            if(keys!=null && keys.f8Key.wasPressedThisFrame) ImpactAudio.Volume=ImpactAudio.Volume>=.99f?.25f:Mathf.Min(1,ImpactAudio.Volume+.25f);
            if (keys != null && keys.escapeKey.wasPressedThisFrame && Host.Session != null && Host.Session.IsActive)
            { Host.SetPaused(!Host.Paused); inputReleased = false; touchStarted = false; }
            var slot = keys == null ? -1 : keys.digit1Key.wasPressedThisFrame ? 0 : keys.digit2Key.wasPressedThisFrame ? 1 : keys.digit3Key.wasPressedThisFrame ? 2 : -1;
            if (slot >= 0) SelectSlot(slot);
            if (keys != null && keys.enterKey.wasPressedThisFrame) EnterCommand();
            var movement = KeyboardMovement(keys);
            var pressed = false;
            foreach (var touch in EnhancedTouch.activeTouches)
                if (touch.phase != UnityEngine.InputSystem.TouchPhase.Ended && touch.phase != UnityEngine.InputSystem.TouchPhase.Canceled) pressed = true;
            if (!Host.CanPlay) { inputReleased = false; touchStarted = false; }
            if (!inputReleased)
            {
                if (movement == Vector2.zero && !pressed) inputReleased = true;
                Host.SetMovement(Vector2.zero);
            }
            else
            {
                if (pressed)
                {
                    var found = false;
                    foreach (var touch in EnhancedTouch.activeTouches)
                    {
                        if (touch.phase == UnityEngine.InputSystem.TouchPhase.Ended || touch.phase == UnityEngine.InputSystem.TouchPhase.Canceled) continue;
                        var position = touch.screenPosition;
                        if (!touchStarted && touch.phase == UnityEngine.InputSystem.TouchPhase.Began && AdamMobileLayout.MovementTouchAllowed(position, safe))
                        { touchStarted = true; touchOrigin = position; touchId = touch.touchId; }
                        if (touchStarted && touch.touchId == touchId) { movement = DragMovement(touchOrigin, touchOrigin + (position - touchOrigin) / (Application.isMobilePlatform ? AdamMobileLayout.Scale(safe) : 1)); found = true; }
                    }
                    if (!found) touchStarted = false;
                }
                else touchStarted = false;
                Host.SetMovement(movement);
            }
            Host.Tick(Time.unscaledDeltaTime);
            ReleaseOnWorldChange();
            UpdatePresentation(Time.unscaledDeltaTime);
        }
        void ReleaseOnWorldChange()
        {
            if(inputWorld==Host.World)return;inputWorld=Host.World;inputReleased=false;touchStarted=false;Host.SetMovement(Vector2.zero);
        }
        public void UpdatePresentation(float delta)
        {
            ArtWorld.Update(Host,delta);
            ImpactAudio?.Update(Host,ArtWorld);
            ImpactParticles?.Update(Host,ArtWorld,delta);
            PrepareParticles(ScreenViewport(),Application.isMobilePlatform);
        }
        public void EnterCommand()
        {
            if(Host==null||Host.Error!=null||Host.Suspended||commandFrame==Time.frameCount)return;
            if(Host.CanEnterExodus)EnterExodus();
            else if(Host.Paused && Host.Session?.ChapterComplete!=true && !Host.ExodusComplete && !Host.ContentComplete)
            {commandFrame=Time.frameCount;Host.SetPaused(false);inputReleased=false;}
        }
        public void EnterExodus()
        {
            if(Host==null||Host.Error!=null||Host.Suspended||DeathPresentationVisible||commandFrame==Time.frameCount||!Host.CanEnterExodus)return;
            commandFrame=Time.frameCount;
            Host.TryEnterExodus();
            inputReleased=false;touchStarted=false;
        }
        public void SelectSlot(int slot)
        {
            if (Host == null || Host.Error != null || Host.Suspended || DeathPresentationVisible || commandFrame == Time.frameCount || slot < 0 || slot > 2) return;
            commandFrame = Time.frameCount;
            if(Host.Session?.Stage=="boss_reward")
            {var choices=Host.Session.BossChoices;if(slot<choices.Count)Host.ChooseBossReward(Host.Session.Boss.Reward.RewardId,choices[slot].Id);}
            else if (Host.Session == null || Host.CanStartNew) Host.Start(Weapons[slot]);
            else if (slot < Host.Session.PendingChoices.Count) Host.Choose(Host.Session.PendingChoices[slot]);
            inputReleased = false; touchStarted = false;
        }
        void OnGUI()
        {
            var safe = Screen.safeArea;
            DrawGameView(new Rect(safe.x, Screen.height - safe.yMax, safe.width, safe.height), Application.isMobilePlatform);
        }
        // The real game and the pointer regression render the same complete HUD/modal stack.
        public void DrawGameView(Rect viewport, bool mobile = false)
        {
            drawingMobile = mobile;
            var scale = mobile ? AdamMobileLayout.Scale(viewport) : AdamChoiceText.ScreenScale(viewport.width, viewport.height);
            GUI.matrix = Matrix4x4.TRS(new Vector3(viewport.x, viewport.y, 0), Quaternion.identity, new Vector3(scale, scale, 1));
            var width = viewport.width / scale; var height = viewport.height / scale;
            EnsureStyles();
            AdamHud.Fill(new Rect(0,0,width,height),AdamHud.Ink0);
            var modalOpen = Host != null && (Host.Error != null || Host.Session == null || !Host.Session.IsActive || Host.Session.PendingChoices.Count > 0 || Host.Paused || Host.Suspended);
            var instruction = new GUIStyle(label) { font = mobile?fonts.Body:fonts.Small, fontSize = mobile ? 20 : 16, wordWrap = true, normal = { textColor = AdamHud.Secondary } };
            if (Host == null)
            { GUI.Label(new Rect(30,120,width-60,160),startupError ?? "저장 기록을 불러오는 중…",label);return; }
            var session = Host.Session;
            if(session!=null && !modalOpen) DrawHud(width,height,session,mobile);
            var arena = Arena(width,height);
            art.OptionalEffects=ArtWorld.FeedbackEnabled;
            if (Host.World != null) ArtWorld.Draw(art, Host, arena, ImpactParticles);
            if(session?.Boss!=null && session.IsActive && !modalOpen) DrawBossHud(width,session,instruction);
            if(session?.RoundNumber==4 && ArtWorld.Snapshot?.Charges!=null && !modalOpen)
            {
                var phase="";foreach(var charge in ArtWorld.Snapshot.Charges)
                {if(charge.Phase=="charge"){phase="돌진 진행 · 고정 방향";break;}if(charge.Phase=="telegraph")phase="돌진 예고 · 고정 방향";}
                if(phase!="")GUI.Label(new Rect(30,122,width-60,28),"약탈 기병마 · "+phase,instruction);
            }
            if (Host.Error != null)
            {
                Modal(width, height, "저장 / 불러오기 중단", mobile ? Host.RequiresNewerGame ? "더 새 게임 버전이 필요한 저장입니다.\n원본과 백업은 보존했습니다. 게임을 업데이트하세요." : "저장할 수 없어 진행을 멈췄습니다.\n재시도하거나 마지막 저장을 불러오세요." : Host.Error + "\n전투가 정지됐습니다. 다시 불러오면 미저장 진행을 버립니다.");
                var previousEnabled = GUI.enabled;
                GUI.enabled = previousEnabled && (!mobile || Host.HasSaveRetry);
                if (ArtButton(mobile ? new Rect(24, height / 2 + 40, (width - 60) / 2, 66) : new Rect(width / 2 - 220, height / 2 + 40, 210, 50), mobile && !Host.HasSaveRetry ? "재시도 불가" : "같은 저장 재시도", button)) Host.RetrySave();
                GUI.enabled = previousEnabled;
                if (ArtButton(mobile ? new Rect(width / 2 + 6, height / 2 + 40, (width - 60) / 2, 66) : new Rect(width / 2 + 10, height / 2 + 40, 210, 50), "저장 다시 불러오기", button)) Host.Reload();
            }
            else if(session?.Stage=="boss_reward")
            {
                var panel=AdamChoiceText.ChoicePanel(width,height);FillPanel(panel);AdamHud.Panel(art,panel);
                ChoiceHeader(panel,AdamRoundPresentation.Name(session.RoundNumber)+" · 보스 보상","하나를 선택하세요",AdamBossPresentation.Name(session)+" · 게임 각색");
                PanelFooter(panel,"고정 골드 +"+session.BossRules.KillGold+" 적용됨","선택 즉시 저장",true);
                if(session.NeedsRewardImplementation)
                {if(ArtButton(new Rect(panel.x+24,panel.y+130,panel.width-48,56),"이전 보상 후보 준비",button))Host.PromoteBossReward();}
                else
                {
                    var candidates=session.BossChoices;var rewardId=session.Boss.Reward.RewardId;
                    for(var i=0;i<candidates.Count;i++)
                    {var candidate=candidates[i];if(ArtButton(AdamChoiceText.ChoiceCard(panel,i),AdamBossPresentation.Card(candidate,i,session),choiceButton,candidate.Kind=="gold"?"gold":candidate.ItemId))Host.ChooseBossReward(rewardId,candidate.Id);}
                }
            }
            else if (session != null && session.PendingChoices.Count > 0)
            {
                var panel = mobile ? AdamMobileLayout.ChoicePanel(width, height) : AdamChoiceText.ChoicePanel(width, height);
                FillPanel(panel);
                AdamHud.Panel(art,panel);
                ChoiceHeader(panel,session.Stage=="boss_settlement"?"보상 적용됨 · 남은 성장 선택":session.Stage=="round_settlement"?"라운드 완료 · 남은 성장 선택":"레벨 상승 · Lv "+session.Growth.Level,"하나를 선택하세요","전투 정지 중 · 진화는 보스 보상");
                for (var i = 0; i < session.PendingChoices.Count; i++)
                {
                    var choice = AdamChoiceText.Describe(session.PendingChoices[i], session.Inventory, session.DamageMultiplier);
                    if (ArtButton(mobile ? AdamMobileLayout.ChoiceCard(panel, i) : AdamChoiceText.ChoiceCard(panel, i), choice.Card(i), choiceButton,choice.Id,choice.CurrentLevel,choice.Maximum)) SelectSlot(i);
                }
                PanelFooter(panel,"기본 무기 수치 · 보조/영구 강화 별도","선택 즉시 저장");
            }
            else if (DeathPresentationVisible)
            {
                if (Host.Paused || Host.Suspended) DrawPauseOverlay(width, height);
                else GUI.Label(new Rect(20, height - 60, width - 40, 36), "사망 · 이번 도전이 끝났습니다", label);
            }
            else if(Host.AwaitingNextRound)
            {
                var next=Host.AwaitingR02?"가인":Host.AwaitingR03?"노아":Host.AwaitingR04?"아브라함":Host.AwaitingR05?"롯":Host.AwaitingR06?"이삭":Host.AwaitingR07?"야곱":Host.AwaitingR08?"요셉":Host.AwaitingR09?"유다":Host.AwaitingR11?"요게벳":Host.AwaitingR12?"모세":Host.AwaitingR13?"미리암":Host.AwaitingR14?"아론":Host.AwaitingR15?"브살렐":throw new InvalidOperationException("Unsupported next round");
                Modal(width,height,$"{AdamRoundPresentation.Name(session.RoundNumber)} 완료 · {next} 라운드로 계속","성장·장비·영구 골드 유지 · 다음 라운드 진입 저장");
                var enabled=GUI.enabled;GUI.enabled=enabled && !Host.Suspended;
                if(ArtButton(new Rect(width/2-180,height/2+20,360,drawingMobile?66:56),next+" 라운드 계속 (Enter)",button)){Host.SetPaused(false);inputReleased=false;touchStarted=false;}
                GUI.enabled=enabled;
            }
            else if(session?.Stage=="boss_settlement"||session?.Stage=="round_settlement")
            {
                Modal(width,height,"보상 적용됨 · 완료 정산",AdamBossPresentation.Forms(session));
                if(ArtButton(new Rect(width/2-150,height/2+30,300,56),"완료 정산",button)){if(session.Stage=="boss_settlement")Host.CompleteBossSettlement();else Host.CompleteNormalSettlement();}
            }
            else if(session?.ChapterComplete==true || Host.ExodusComplete || Host.ContentComplete)
            {
                var genesisComplete=session?.ChapterComplete==true;
                var panel=AdamChoiceText.ChoicePanel(width,height);FillPanel(panel);AdamHud.Panel(art,panel);
                CompletionHeader(panel,AdamRoundPresentation.CompletionHeading(genesisComplete,Host.ExodusComplete),AdamRoundPresentation.CompletionDetail(genesisComplete,Host.ExodusComplete));
                var enabled=GUI.enabled;
                if(genesisComplete)
                {
                    GUI.enabled=enabled && Host.CanEnterExodus && !Host.Suspended;
                    if(ArtButton(AdamChoiceText.ChapterAction(panel,0),AdamChoiceText.ExodusStartCard,button))EnterExodus();
                }
                GUI.enabled=enabled && !Host.Suspended;
                for(var i=0;i<3;i++)
                    if(ArtButton(AdamChoiceText.ChapterAction(panel,i+(genesisComplete?1:0)),AdamChoiceText.StartCard(Weapons[i],i,true),button,Weapons[i]))SelectSlot(i);
                GUI.enabled=enabled;
            }
            else if (session == null || !session.IsActive)
            {
                var heading=session==null?"시작 무기를 선택하세요":"사망 · 이번 도전 종료";
                Modal(width,height,heading,session==null?"이동하면 자동 공격 · 빛나는 보석을 모아 성장하세요":"새 도전은 성장 초기화 · 영구 골드와 해금 유지");
                for (var i = 0; i < 3; i++)
                    if (ArtButton(mobile ? AdamMobileLayout.StartButton(width, height, i) : new Rect(width / 2 - 240, height / 2 - 15 + i * 58, 480, 50), AdamChoiceText.StartCard(Weapons[i],i), button,Weapons[i])) SelectSlot(i);
            }
            else if (Host.Paused || Host.Suspended)
            {
                DrawPauseOverlay(width, height);
            }
        }
        GUIStyle HudText(int size,Color color,bool bold=false,TextAnchor align=TextAnchor.MiddleLeft)
        {return new GUIStyle(label){font=bold&&size>=24?fonts.Title:size<=16?fonts.Small:fonts.Body,fontSize=size,alignment=align,wordWrap=false,normal={textColor=color}};}
        void DrawHud(float width,float height,AdamSession session,bool mobile)
        {
            AdamHud.Fill(new Rect(0,0,width,110),AdamHud.Ink1);
            AdamHud.Rule(art,new Rect(0,109,width,2),true);
            var body=HudText(16,AdamHud.Secondary);
            GUI.Label(new Rect(20,8,width*.36f,24),(session.RoundNumber>=10?"출애굽기 "+(session.RoundNumber-9):"창세기")+" · "+AdamRoundPresentation.Name(session.RoundNumber),body);
            var hp=AdamHud.Health(width);AdamHud.Bar(art,hp,"ui_bar-hp_tile_1x2",AdamHud.Fraction(session.CurrentHealth,Host.State.Run.MaxHealth));
            GUI.Label(new Rect(hp.x,hp.yMax+1,hp.width,24),$"체력 {session.CurrentHealth:0} / {Host.State.Run.MaxHealth:0}",body);
            GUI.Label(AdamHud.TimerRect(width),AdamHud.Timer(session,Host.World?.SimulationTime??0),HudText(36,AdamHud.Primary,true,TextAnchor.MiddleCenter));
            GUI.Label(new Rect(width/2-66,52,132,24),AdamHud.TimerRole(session),HudText(16,AdamHud.Muted,false,TextAnchor.MiddleCenter));
            AdamHud.Image(art,"ico_res_gold_static_16",new Rect(width-246,23,24,24));
            GUI.Label(new Rect(width-218,20,70,30),Host.State.Profile.Gold.ToString(),HudText(20,AdamHud.Gold));
            if(ArtButton(AdamHud.Pause(width,mobile),Host.Paused?"재개":"일시정지",button)){Host.SetPaused(!Host.Paused);inputReleased=false;}
            var level=new Rect(20,82,66,26);AdamHud.Panel(art,level,"ui_chip_9slice_8");
            GUI.Label(level,"Lv "+session.Growth.Level,HudText(16,AdamHud.Lapis,true,TextAnchor.MiddleCenter));
            var xp=AdamHud.Experience(width);var required=ExperienceRules.Required(session.Growth.Level);
            AdamHud.Bar(art,xp,"ui_bar-xp_tile_1x4",AdamHud.Fraction(session.Growth.Experience,required));
            GUI.Label(new Rect(xp.x,63,xp.width,22),$"경험치 {session.Growth.Experience:0.#} / {required}",HudText(16,AdamHud.Muted,false,TextAnchor.MiddleRight));
            var footer=AdamHud.Footer(width,height);AdamHud.Fill(footer,AdamHud.Ink1);AdamHud.Rule(art,new Rect(0,footer.y,width,2),true);
            var slot=0;foreach(var pair in session.Inventory.Weapons)
            {AdamHud.Item(art,AdamHud.InventorySlot(height,slot++,true),pair.Key,pair.Value,8,session.Inventory.Forms.ContainsKey(pair.Key),body);}
            slot=0;foreach(var pair in session.Inventory.Passives)
            {AdamHud.Item(art,AdamHud.InventorySlot(height,slot++,false),pair.Key,pair.Value,pair.Key=="stones"?2:5,false,HudText(16,AdamHud.Secondary));}
            var hintX=Mathf.Max(350,width*.5f);var y=height-74;
            Hint(hintX,y,mobile?"드래그":"WASD",mobile?"이동":"이동",mobile?50:42);Hint(hintX+84,y,"Esc","정지",30);Hint(hintX+158,y,"Enter","재개",44);
            Hint(hintX,y+24,"1–3","선택",34);Hint(hintX+80,y+24,"F6",ArtWorld.FeedbackEnabled?"효과 기본":"효과 낮음",26);Hint(hintX+192,y+24,"F7",ImpactAudio?.Muted==true?"소리 끔":"소리 켬",26);
            Hint(hintX,y+48,"F8",$"음량 {ImpactAudio?.Volume*100:0}%",26);Hint(hintX+144,y+48,"F9",ImpactParticles?.Enabled==true?"입자 켬":"입자 끔",26);
        }
        void Hint(float x,float y,string key,string description,float keyWidth)
        {var text=HudText(16,AdamHud.Muted);AdamHud.Key(art,new Rect(x,y,keyWidth,21),key,text);GUI.Label(new Rect(x+keyWidth+4,y,90,21),description,text);}
        void DrawBossHud(float width,AdamSession session,GUIStyle instruction)
        {
            var boss=session.Boss;
            AdamHud.Fill(new Rect(28,117,width-56,64),AdamHud.Ink1);
            AdamHud.Bar(art,new Rect(32,120,width-64,16),"ui_bar-boss_tile_1x4",AdamBossPresentation.HealthFraction(session));
            GUI.Label(new Rect(34,136,width-68,22),$"{AdamBossPresentation.Name(session)}  {boss.Health:0} / {session.BossRules.MaxHealth:0}",HudText(16,AdamHud.Primary));
            AdamHud.Panel(art,new Rect(32,158,width-64,22),"ui_chip_9slice_8",boss.Telegraphing?1:0,1);
            GUI.Label(new Rect(40,158,width-80,22),AdamBossPresentation.Phase(session),instruction);
        }
        void DrawPauseOverlay(float width, float height)
        {
            EnsureStyles();
            Modal(width, height, "일시정지 · 저장됨", drawingMobile ? "손가락을 놓은 뒤 아래 재개를 누르세요.\n앱 중단 중에는 전투 시간이 흐르지 않습니다." : "이동 입력을 놓은 뒤 Enter 또는 아래 재개를 누르세요.\n앱 중단 중에는 전투 시간이 흐르지 않습니다.");
            var enabled = GUI.enabled;
            GUI.enabled = enabled && Host != null && Host.Error == null && !Host.Suspended;
            if (ArtButton(new Rect(width / 2 - 125, height / 2 + 20, 250, drawingMobile ? 66 : 56), drawingMobile ? "재개" : "재개 (Enter)", button))
            {
                Host.SetPaused(false); inputReleased = false; touchStarted = false;
            }
            GUI.enabled = enabled;
        }
        void EnsureStyles()
        {
            if (label != null) return;
            art = new AdamArt();
            fonts = new AdamFonts();
            label = new GUIStyle(GUI.skin.label) { font = fonts.Body, fontSize = 20, alignment = TextAnchor.MiddleCenter, normal = { textColor = TextColor } };
            title = new GUIStyle(label) { font = fonts.Title, fontSize = 24, fontStyle = FontStyle.Normal };
            button = new GUIStyle(GUI.skin.button) { font = fonts.Label, fontSize = 24, fixedHeight = 50 };
            choiceButton = new GUIStyle(button) { fontSize = 24, fixedHeight = 0, wordWrap = true, alignment = TextAnchor.MiddleLeft, padding = new RectOffset(16,16,8,8) };
            choiceButton.focused.textColor = AdamHud.Gold;
            clearButton = new GUIStyle(button); clearChoice = new GUIStyle(choiceButton) { padding = new RectOffset(36,16,8,8) };
            foreach(var clear in new[] { clearButton, clearChoice })
                foreach(var state in new[] { clear.normal, clear.hover, clear.active, clear.focused }) state.background=null;
        }
        bool ArtButton(Rect rect, string text, GUIStyle style,string itemId=null,int currentLevel=0,int maximum=0)
        {
            var card=ReferenceEquals(style,choiceButton);
            var name="adam:"+text.Split('\n')[0];GUI.SetNextControlName(name);
            var focused=GUI.GetNameOfFocusedControl()==name;var hover=rect.Contains(Event.current.mousePosition);
            var pressed=hover&&GUIUtility.hotControl!=0;
            var state=AdamHud.SurfaceState(GUI.enabled,focused,hover,pressed);
            AdamHud.Panel(art,rect,card?"ui_card_9slice_24":"ui_button_9slice_16",state);
            var clear=card?clearChoice:clearButton;
            // Keep IMGUI focus, capture, disabled behaviour and hit target at the reviewed position.
            var clicked=GUI.Button(rect,GUIContent.none,clear);
            var content=rect;if(pressed)content.y+=2;
            var color=GUI.enabled?AdamHud.Primary:AdamHud.Muted;
            if(card)
            {
                var rows=text.Split('\n');var slot=text.IndexOf(". ",StringComparison.Ordinal);
                var key=slot>0?text.Substring(0,slot):"";
                AdamHud.Key(art,new Rect(content.x+14,content.center.y-14,28,28),key,HudText(20,color),state==1);
                var icon=AdamHud.Icon(itemId);
                AdamHud.Image(art,"ui_slot_18",new Rect(content.x+52,content.y+20,36,36),1);
                if(icon!=null)AdamHud.Image(art,icon,new Rect(content.x+54,content.y+22,32,32));
                var heading=AdamHud.CardName(text);var tag=AdamHud.CardTag(text);
                if(icon==null&&heading.Length>0)GUI.Label(new Rect(content.x+52,content.y+20,38,38),heading.Substring(0,1),HudText(20,AdamHud.Secondary,false,TextAnchor.MiddleCenter));
                var tagStyle=HudText(16,AdamHud.Secondary);var tagWidth=Mathf.Min(220,tagStyle.CalcSize(new GUIContent(tag)).x+12);
                var tagRect=new Rect(content.xMax-tagWidth-14,content.y+10,tagWidth,24);
                if(tag!=""){AdamHud.Panel(art,tagRect,"ui_chip_9slice_8",0,1);GUI.Label(new Rect(tagRect.x+6,tagRect.y,tagRect.width-12,24),tag,tagStyle);}
                var nameRect=new Rect(content.x+102,content.y+8,Mathf.Max(20,content.width-116-(tag==""?0:tagWidth+8)),30);
                var nameStyle=HudText(20,color);if(nameStyle.CalcSize(new GUIContent(heading)).x>nameRect.width)nameStyle=HudText(16,color);
                GUI.Label(nameRect,heading,nameStyle);
                var effect=HudText(16,GUI.enabled?AdamHud.Secondary:AdamHud.Muted);effect.wordWrap=true;
                GUI.Label(new Rect(content.x+102,content.y+40,content.width-116,content.height-48),rows.Length>1?rows[1]:"",effect);
                for(var i=0;i<maximum;i++)AdamHud.Image(art,"ui_pip_2",new Rect(content.xMax-14-maximum*5+i*5,content.yMax-8,4,4),i<=currentLevel?1:0);
            }
            else
            {
                var icon=AdamHud.Icon(itemId);
                if(icon!=null)
                {
                    var prefix=text.IndexOf(". ",StringComparison.Ordinal);
                    if(prefix>0)AdamHud.Key(art,new Rect(content.x+14,content.center.y-14,28,28),text.Substring(0,prefix),HudText(20,color),state==1);
                    AdamHud.Image(art,icon,new Rect(content.x+54,content.center.y-16,32,32));
                    GUI.Label(new Rect(content.x+96,content.y,content.width-108,content.height),prefix>0?text.Substring(prefix+2):text,HudText(20,color));
                }
                else GUI.Label(content,text,new GUIStyle(clear){normal={textColor=color}});
            }
            return clicked;
        }
        void ChoiceHeader(Rect panel,string eyebrow,string heading,string detail)
        {
            GUI.Label(new Rect(panel.x+16,panel.y+8,panel.width-32,22),eyebrow,HudText(16,AdamHud.Lapis,false,TextAnchor.MiddleCenter));
            GUI.Label(new Rect(panel.x+16,panel.y+30,panel.width-32,32),heading,title);
            GUI.Label(new Rect(panel.x+16,panel.y+(drawingMobile?56:64),panel.width-32,24),detail,HudText(16,AdamHud.Muted,false,TextAnchor.MiddleCenter));
        }
        void PanelFooter(Rect panel,string result,string guidance,bool gold=false)
        {
            AdamHud.Rule(art,new Rect(panel.x+24,panel.yMax-34,panel.width-48,1));
            GUI.Label(new Rect(panel.x+24,panel.yMax-30,panel.width-48,24),result,HudText(16,gold?AdamHud.Gold:AdamHud.Secondary));
            GUI.Label(new Rect(panel.x+24,panel.yMax-30,panel.width-48,24),guidance,HudText(16,AdamHud.Muted,false,TextAnchor.MiddleRight));
        }
        void CompletionHeader(Rect panel,string heading,string detail)
        {
            var split=heading.IndexOf(" · ",StringComparison.Ordinal);
            var eyebrow=split<0?"진행 저장됨":heading.Substring(0,split);
            var headline=split<0?heading:heading.Substring(split+3);
            ChoiceHeader(panel,eyebrow,headline,"");
            var lines=detail.Split('\n');var caution=lines[lines.Length-1];
            var summary=lines.Length>1?string.Join("\n",lines,0,lines.Length-1):detail;
            var style=HudText(20,AdamHud.Secondary,false,TextAnchor.MiddleCenter);style.wordWrap=true;
            GUI.Label(new Rect(panel.x+24,panel.y+72,panel.width-48,66),summary,style);
            PanelFooter(panel,"◆ "+caution,"",true);
        }
        void Modal(float width, float height, string heading, string detail)
        {
            var panel = drawingMobile ? AdamMobileLayout.Panel(width, height) : new Rect(width / 2 - 310, height / 2 - 160, 620, 340);
            FillPanel(panel);
            AdamHud.Panel(art,panel);
            GUI.Label(new Rect(panel.x+16,panel.y+10,panel.width-32,20),Host?.Error!=null?"진행 보호":"도전 기록",HudText(16,AdamHud.Lapis,false,TextAnchor.MiddleCenter));
            GUI.Label(new Rect(panel.x+16,panel.y+32,panel.width-32,40),heading,title);
            var detailStyle = new GUIStyle(label) { wordWrap = true, fontSize = 20 };
            GUI.Label(new Rect(panel.x + 24, panel.y + 78, panel.width - 48, 58), detail, detailStyle);
        }
        static void FillPanel(Rect panel)
        {
            var previous = GUI.color;
            GUI.color = AdamHud.Ink2;
            GUI.DrawTexture(panel, Texture2D.whiteTexture);
            GUI.color = previous;
        }
    }
}
