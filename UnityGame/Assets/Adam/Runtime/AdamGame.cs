using System;
using System.IO;
using BibleSurvivor.Core;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using EnhancedTouch = UnityEngine.InputSystem.EnhancedTouch.Touch;

namespace BibleSurvivor.Unity
{
    public sealed partial class AdamGame : MonoBehaviour
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
        AdamSession newChallengeSource;
        int newChallengeFocus;
        bool focusNewChallenge;
        public bool NewChallengeOpen => newChallengeSource != null && ReferenceEquals(newChallengeSource,Host?.Session) && Host.BookComplete;
        string startupError;
        bool drawingMobile;
        Rect previousTouchSafe;
        GUIStyle label, button, title, choiceButton, clearButton, clearChoice;
        AdamFonts fonts;
        AdamArt art;
        readonly AdamStartStage startStage=new AdamStartStage();
        readonly AdamUiMotion uiMotion=new AdamUiMotion();
        readonly AdamBossStaging bossStaging = new AdamBossStaging();
        readonly AdamSortieRibbon sortie=new AdamSortieRibbon();
        AdamSession sortieSession;
        public bool SortieVisible=>AdamBookTitle.Enabled&&Host?.CanPlay==true&&Host.Session.Boss==null&&
            sortie.Visible(AdamSortieRibbon.Identity(Host.State?.Run?.RunId,Host.Session.RoundNumber),ArtWorld.FeedbackEnabled);
        string startIdentity;
        bool UiMotionEnabled=>ArtWorld.FeedbackEnabled&&AdamHud.PolishEnabled;
        public int StartFocus=>startStage.Focus;
        public bool StartStageAvailable=>AdamHud.PolishEnabled&&Host!=null&&!Host.PermanentSkillsOpen&&Host.Error==null&&!Host.Suspended&&!DeathPresentationVisible&&
            (Host.Session==null||(!Host.Session.IsActive&&Host.Session.CurrentHealth<=0&&Host.Session.PendingChoices.Count==0&&
            Host.Session.Stage!="boss_reward"&&Host.Session.Stage!="boss_settlement"&&Host.Session.Stage!="round_settlement"&&
            !Host.BookComplete&&!Host.AwaitingNextRound));
        public void MoveStartFocus(int direction){if(StartStageAvailable)startStage.Move(direction);}
        public AdamImpactAudio ImpactAudio {get;private set;}
        public AdamImpactParticles ImpactParticles {get;private set;}
        public static Rect Arena(float width,float height) => new Rect(20,116,width-40,height-194);
        public static Rect ScreenViewport() {var safe=Screen.safeArea;return new Rect(safe.x,Screen.height-safe.yMax,safe.width,safe.height);}
        public void PrepareParticles(Rect viewport,bool mobile=false,Rect? physicalFrame=null)
        {
            if(ArtWorld.Snapshot==null || ImpactParticles==null)return;
            var scale=mobile?AdamMobileLayout.Scale(viewport):AdamChoiceText.ScreenScale(viewport.width,viewport.height);
            var arena = mobile ? AdamMobileLayout.FullFrame(viewport,physicalFrame??viewport) : Arena(viewport.width/scale,viewport.height/scale);
            ImpactParticles.Prepare(new AdamWorldProjection(arena,new Vector2(ArtWorld.Snapshot.PlayerX,ArtWorld.Snapshot.PlayerY)));
        }
        public AdamArtWorld ArtWorld { get; } = new AdamArtWorld();
        // Reviewed death strip: six frames at 8 fps. Keep the actor visible before covering it with restart controls.
        public bool DeathPresentationVisible => Host?.Session != null && !Host.Session.IsActive && Host.Session.CurrentHealth <= 0 &&
            ArtWorld.PlayerMotion == "death" && ArtWorld.PlayerSeconds < 6d / 8;
        public static readonly string[] Weapons = { "bible", "jawbone", "sling" };
        void OnEnable() {EnhancedTouchSupport.Enable();if(Host!=null && ImpactParticles==null)ImpactParticles=new AdamImpactParticles();}
        void OnDisable() {EnhancedTouchSupport.Disable();bossStaging.Reset();ImpactAudio?.Stop();ImpactParticles?.Dispose();ImpactParticles=null;}
        void OnDestroy() { ArtWorld.Dispose(); ImpactParticles?.Dispose(); ImpactAudio?.Dispose(); bossStaging.Audio?.Dispose(); fonts?.Dispose(); }

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
        public void Initialize(AdamHost host)
        {
            bossStaging.Initialize(gameObject);
            Host = host;
            newChallengeSource=null;
            focusNewChallenge=false;
            sortie.Reset();sortieSession=host.Session;inputReleased=false;
            // The listener serves both audio systems and survives a host replacement.
            if (GetComponent<AudioListener>() == null && FindAnyObjectByType<AudioListener>() == null)
                gameObject.AddComponent<AudioListener>();
            ImpactAudio?.Dispose();ImpactAudio=new AdamImpactAudio(gameObject);
            ImpactParticles?.Dispose();ImpactParticles=new AdamImpactParticles();
        }
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
            if (!focused || appPaused)
            {
                bossStaging.Interrupt();
                ImpactAudio?.Stop();
            }
            Host?.SetSuspended(!focused || appPaused);
            SyncBossStaging();
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
            SyncSortieSession();
            ReleaseOnWorldChange();
            var safe = Screen.safeArea;
            if (Application.isMobilePlatform && previousTouchSafe != default && safe != previousTouchSafe)
            {
                SetPlayPaused(true); inputReleased = false; touchStarted = false;
            }
#if DEVELOPMENT_BUILD && !UNITY_EDITOR
            if (safe != previousTouchSafe)
                Debug.Log($"ADAM_DISPLAY pixels={Screen.width}x{Screen.height} safe={safe} dpi={Screen.dpi} scale={AdamMobileLayout.Scale(safe)} orientation={Screen.orientation}");
#endif
            previousTouchSafe = safe;
            var keys = Keyboard.current;
            if (HandlePermanentSkillInput(keys))
            {
                Host.SetMovement(Vector2.zero);
                UpdatePresentation(Time.unscaledDeltaTime);
                return;
            }
            var identity=StartStageAvailable?(Host.Session==null?"start":"death"):"";
            if(identity!=startIdentity){startIdentity=identity;startStage.Reset();}
            if(StartStageAvailable&&keys!=null)
            {
                if(keys.leftArrowKey.wasPressedThisFrame||keys.aKey.wasPressedThisFrame)MoveStartFocus(-1);
                else if(keys.rightArrowKey.wasPressedThisFrame||keys.dKey.wasPressedThisFrame)MoveStartFocus(1);
            }
            if(keys!=null && keys.f6Key.wasPressedThisFrame) ArtWorld.FeedbackEnabled=!ArtWorld.FeedbackEnabled;
            if(keys!=null && keys.f9Key.wasPressedThisFrame) ImpactParticles.Enabled=!ImpactParticles.Enabled;
            if(keys!=null && keys.f7Key.wasPressedThisFrame) ImpactAudio.Muted=!ImpactAudio.Muted;
            if(keys!=null && keys.f8Key.wasPressedThisFrame) ImpactAudio.Volume=ImpactAudio.Volume>=.99f?.25f:Mathf.Min(1,ImpactAudio.Volume+.25f);
            if (keys != null && keys.escapeKey.wasPressedThisFrame)
            {
                if(NewChallengeOpen) CancelNewChallenge();
                else if(Host.Session?.IsActive==true){SetPlayPaused(!Host.Paused);inputReleased=false;touchStarted=false;}
            }
            if(NewChallengeOpen && !Host.Suspended && keys!=null)
            {
                if(keys.upArrowKey.wasPressedThisFrame) newChallengeFocus=(newChallengeFocus+3)%4;
                else if(keys.downArrowKey.wasPressedThisFrame) newChallengeFocus=(newChallengeFocus+1)%4;
            }
            else if(Host.BookComplete && !Host.Suspended && keys!=null)
            {
                if(keys.upArrowKey.wasPressedThisFrame) focusNewChallenge=false;
                else if(keys.downArrowKey.wasPressedThisFrame) focusNewChallenge=true;
            }
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
            var tickSession=Host.Session;var tickRun=Host.State?.Run?.RunId;
            var stagingBefore = bossStaging.BeforeTick(Host, ImpactAudio, isActiveAndEnabled);
            Host.Tick(Time.unscaledDeltaTime);
            bossStaging.AfterTick(stagingBefore, Host, ImpactAudio, isActiveAndEnabled);
            RecordSortieTransition(tickSession,tickRun);
            ReleaseOnWorldChange();
            UpdatePresentation(Time.unscaledDeltaTime);
            uiMotion.Advance(UiSequence(),Time.unscaledDeltaTime);
            bossStaging.AdvanceReward(Host, uiMotion.Elapsed, ImpactAudio, isActiveAndEnabled);
        }
        void SyncBossStaging() => bossStaging.Sync(Host, ImpactAudio, isActiveAndEnabled);
        void SetPlayPaused(bool paused)
        {
            if (paused) bossStaging.Interrupt();
            Host.SetPaused(paused);
            SyncBossStaging();
        }
        void ReleaseOnWorldChange()
        {
            if(inputWorld==Host.World)return;inputWorld=Host.World;inputReleased=false;touchStarted=false;Host.SetMovement(Vector2.zero);
        }
        public void UpdatePresentation(float delta)
        {
            SyncSortieSession();
            ArtWorld.Update(Host,delta);
            ImpactAudio?.Update(Host,ArtWorld);
            ImpactParticles?.Update(Host,ArtWorld,delta);
            PrepareParticles(ScreenViewport(),Application.isMobilePlatform,new Rect(0,0,Screen.width,Screen.height));
            if(Host!=null)AdvanceSortie(delta);
        }
        public void EnterCommand()
        {
            if(Host==null||Host.PermanentSkillsOpen||Host.Error!=null||Host.Suspended||commandFrame==Time.frameCount)return;
            if(NewChallengeOpen)
            {
                if(newChallengeFocus==0) CancelNewChallenge();
                else ConfirmNewChallenge(newChallengeFocus-1);
                return;
            }
            if(PermanentSkillEntryHasFocus){OpenPermanentSkillMenu(SkillEntryScreen);return;}
            if(StartStageAvailable){SelectSlot(startStage.Focus);return;}
            if(Host.BookComplete && (focusNewChallenge || AdamRoundPresentation.NextChapter(Host)==0))
            {
                OpenNewChallenge();
                return;
            }
            if(Host.CanEnterExodus)EnterExodus();
            else if(Host.CanEnterLeviticus)EnterLeviticus();
            else if(Host.CanEnterNumbers)EnterNumbers();
            else if(Host.CanEnterDeuteronomy)EnterDeuteronomy();
            else if(Host.CanEnterJoshua)EnterJoshua();
            else if(Host.CanEnterJudges)EnterJudges();
            else if(Host.CanEnterRuth)EnterRuth();
            else if(Host.CanEnterFirstSamuel)EnterFirstSamuel();
            else if(Host.CanEnterLaterBook)EnterChapter(Host.UpcomingLaterBook.FirstRound);
            else if(Host.Paused && !Host.BookComplete)
            {commandFrame=Time.frameCount;ResumePlay();inputReleased=false;}
        }
        public void EnterExodus() => EnterChapter(ExodusContent.GlobalRoundNumber);
        public void EnterLeviticus() => EnterChapter(LeviticusContent.GlobalRoundNumber);
        public void EnterNumbers() => EnterChapter(NumbersContent.GlobalRoundNumber);
        public void EnterDeuteronomy() => EnterChapter(DeuteronomyContent.GlobalRoundNumber);
        public void EnterJoshua() => EnterChapter(JoshuaContent.GlobalRoundNumber);
        public void EnterJudges() => EnterChapter(JudgesContent.GlobalRoundNumber);
        public void EnterRuth() => EnterChapter(RuthContent.GlobalRoundNumber);
        public void EnterFirstSamuel() => EnterChapter(FirstSamuelContent.GlobalRoundNumber);
        bool CanEnterChapter(int round)=>round switch
        {
            ExodusContent.GlobalRoundNumber=>Host.CanEnterExodus,
            LeviticusContent.GlobalRoundNumber=>Host.CanEnterLeviticus,
            NumbersContent.GlobalRoundNumber=>Host.CanEnterNumbers,
            DeuteronomyContent.GlobalRoundNumber=>Host.CanEnterDeuteronomy,
            JoshuaContent.GlobalRoundNumber=>Host.CanEnterJoshua,
            JudgesContent.GlobalRoundNumber=>Host.CanEnterJudges,
            RuthContent.GlobalRoundNumber=>Host.CanEnterRuth,
            FirstSamuelContent.GlobalRoundNumber=>Host.CanEnterFirstSamuel,
            _=>Host.CanEnterLaterBook && Host.UpcomingLaterBook.FirstRound==round
        };
        void EnterChapter(int round)
        {
            if(Host==null||Host.PermanentSkillsOpen||NewChallengeOpen||DeathPresentationVisible||commandFrame==Time.frameCount||!CanEnterChapter(round))return;
            commandFrame=Time.frameCount;
            focusNewChallenge=false;
            var previous=Host.Session;var run=Host.State?.Run?.RunId;
            switch(round)
            {
                case ExodusContent.GlobalRoundNumber: Host.TryEnterExodus(); break;
                case LeviticusContent.GlobalRoundNumber: Host.TryEnterLeviticus(); break;
                case NumbersContent.GlobalRoundNumber: Host.TryEnterNumbers(); break;
                case DeuteronomyContent.GlobalRoundNumber: Host.TryEnterDeuteronomy(); break;
                case JoshuaContent.GlobalRoundNumber: Host.TryEnterJoshua(); break;
                case JudgesContent.GlobalRoundNumber: Host.TryEnterJudges(); break;
                case RuthContent.GlobalRoundNumber: Host.TryEnterRuth(); break;
                case FirstSamuelContent.GlobalRoundNumber: Host.TryEnterFirstSamuel(); break;
                default: Host.TryEnterLaterBook(); break;
            }
            RecordSortieTransition(previous,run);
            inputReleased=false;touchStarted=false;
        }
        public void OpenNewChallenge()
        {
            if(NewChallengeOpen || Host?.BookComplete!=true || Host.PermanentSkillsOpen || !Host.CanStartNew || Host.Suspended || commandFrame==Time.frameCount) return;
            commandFrame=Time.frameCount;
            newChallengeSource=Host.Session;
            newChallengeFocus=0;
        }

        public void CancelNewChallenge()
        {
            if(!NewChallengeOpen || Host.Suspended || commandFrame==Time.frameCount) return;
            commandFrame=Time.frameCount;
            newChallengeSource=null;
            focusNewChallenge=false;
        }

        public void ConfirmNewChallenge(int slot)
        {
            if(!NewChallengeOpen || !Host.CanStartNew || Host.Suspended || commandFrame==Time.frameCount || slot<0 || slot>=Weapons.Length) return;
            commandFrame=Time.frameCount;
            var previous=Host.Session;var run=Host.State.Run.RunId;
            newChallengeSource=null;
            focusNewChallenge=false;
            Host.Start(Weapons[slot]);
            RecordSortieTransition(previous,run);
            inputReleased=false;touchStarted=false;
        }

        public void SelectSlot(int slot)
        {
            if (Host == null || Host.PermanentSkillsOpen || Host.Error != null || Host.Suspended || DeathPresentationVisible || commandFrame == Time.frameCount || slot < 0 || slot > 2) return;
            if (Host.BookComplete && Host.CanStartNew) return;
            commandFrame = Time.frameCount;
            var previous=Host.Session;var run=Host.State?.Run?.RunId;
            if(Host.Session?.Stage=="boss_reward")
            {var choices=Host.Session.BossChoices;if(slot<choices.Count)ChooseBossReward(Host.Session.Boss.Reward.RewardId,choices[slot].Id);}
            else if (Host.Session == null || Host.CanStartNew) Host.Start(Weapons[slot]);
            else if (slot < Host.Session.PendingChoices.Count) Host.Choose(Host.Session.PendingChoices[slot]);
            RecordSortieTransition(previous,run);
            inputReleased = false; touchStarted = false;
        }
        void ChooseBossReward(string rewardId, string candidateId)
        {
            Host.ChooseBossReward(rewardId, candidateId);
            SyncBossStaging();
        }
        void OnGUI()
        {
            var safe = Screen.safeArea;
            DrawGameView(new Rect(safe.x, Screen.height - safe.yMax, safe.width, safe.height), Application.isMobilePlatform,new Rect(0,0,Screen.width,Screen.height));
        }
        // The real game and the pointer regression render the same complete HUD/modal stack.
        public void DrawGameView(Rect viewport, bool mobile = false, Rect? physicalFrame = null)
        {
            drawingMobile = mobile;
            var scale = mobile ? AdamMobileLayout.Scale(viewport) : AdamChoiceText.ScreenScale(viewport.width, viewport.height);
            GUI.matrix = Matrix4x4.TRS(new Vector3(viewport.x, viewport.y, 0), Quaternion.identity, new Vector3(scale, scale, 1));
            var width = viewport.width / scale; var height = viewport.height / scale;
            EnsureStyles();
            SyncSortieSession();
            uiMotion.Advance(UiSequence(),0);
            var arena = mobile ? AdamMobileLayout.FullFrame(viewport,physicalFrame??viewport) : Arena(width,height);
            AdamHud.Fill(mobile?arena:new Rect(0,0,width,height),AdamHud.Ink0);
            if(mobile && Host==null)ArtWorld.DrawBackdrop(art,arena,1);
            var modalOpen = Host != null && (Host.Error != null || Host.Session == null || !Host.Session.IsActive || Host.Session.PendingChoices.Count > 0 || Host.Paused || Host.Suspended);
            var instruction = new GUIStyle(label) { font = mobile?fonts.Body:fonts.Small, fontSize = mobile ? 20 : 16, wordWrap = true, normal = { textColor = AdamHud.Secondary } };
            if (Host == null)
            { GUI.Label(new Rect(30,120,width-60,160),startupError ?? "저장 기록을 불러오는 중…",label);return; }
            if (Host.PermanentSkillsOpen)
            {
                DrawPermanentSkills(width, height);
                return;
            }
            var session = Host.Session;
            if(!mobile && session!=null && !modalOpen){if(AdamHud.PolishEnabled)DrawHud(width,height,session,false);else DrawLegacyHud(width,height,session,false);}
            art.OptionalEffects=ArtWorld.FeedbackEnabled;
            System.Action beforeTelegraphs=null;
            if(!modalOpen&&session?.Boss==null&&SortieVisible)beforeTelegraphs=()=>DrawSortie(width,session,arena.position);
            var drewWorld=Host.World!=null && ArtWorld.Draw(art,Host,arena,ImpactParticles,beforeTelegraphs,
                bossStaging.ShadowOnly(Host,UiMotionEnabled),mobile);
            if(mobile && !drewWorld)ArtWorld.DrawBackdrop(art,arena,session?.RoundNumber??1);
            if(mobile && session!=null && !modalOpen)DrawHud(width,height,session,true);
            if(AdamHud.PolishEnabled&&modalOpen&&!DeathPresentationVisible&&session!=null&&(session.IsActive||session.CurrentHealth>0))
            {var dim=AdamHud.Ink0;dim.a=AdamUiMotion.Dim(uiMotion.Elapsed,UiMotionEnabled);AdamHud.Fill(arena,dim);}
            if(session?.Boss!=null && session.IsActive && !modalOpen){if(AdamHud.PolishEnabled)DrawBossHud(width,session,instruction);else DrawLegacyBossHud(width,session,instruction);}
            if(session?.RoundNumber==4 && ArtWorld.Snapshot?.Charges!=null && !modalOpen)
            {
                var phase="";foreach(var charge in ArtWorld.Snapshot.Charges)
                {if(charge.Phase=="charge"){phase="돌진 진행 · 고정 방향";break;}if(charge.Phase=="telegraph")phase="돌진 예고 · 고정 방향";}
                if(phase!="")GUI.Label(new Rect(30,122,width-60,28),"약탈 기병마 · "+phase,instruction);
            }
            if (Host.Error != null)
            {
                var entryRetry=AdamRoundPresentation.EntryRetryModal(Host);
                if(entryRetry!=null)
                    Modal(width,height,entryRetry.Value.Heading,entryRetry.Value.Body);
                else
                    Modal(width, height, "저장 / 불러오기 중단", mobile ? Host.RequiresNewerGame ? "더 새 게임 버전이 필요한 저장입니다.\n원본과 백업은 보존했습니다. 게임을 업데이트하세요." : "저장할 수 없어 진행을 멈췄습니다.\n재시도하거나 마지막 저장을 불러오세요." : Host.Error + "\n전투가 정지됐습니다. 다시 불러오면 미저장 진행을 버립니다.");
                var previousEnabled = GUI.enabled;
                GUI.enabled = previousEnabled && (!mobile || Host.HasSaveRetry);
                if (ArtButton(mobile ? AdamMobileLayout.Action(AdamMobileLayout.Panel(width,height),0,2) : new Rect(width / 2 - 220, height / 2 + 40, 210, 50), mobile && !Host.HasSaveRetry ? "재시도 불가" : "같은 저장 재시도", button)) RetrySaveWithDeparture();
                GUI.enabled = previousEnabled;
                if (ArtButton(mobile ? AdamMobileLayout.Action(AdamMobileLayout.Panel(width,height),1,2) : new Rect(width / 2 + 10, height / 2 + 40, 210, 50), "저장 다시 불러오기", button)) { newChallengeSource=null; focusNewChallenge=false; Host.Reload(); SyncBossStaging(); }
            }
            else if(session?.Stage=="boss_reward")
            {
                var panel=mobile?AdamMobileLayout.ChoicePanel(width,height):AdamChoiceText.ChoicePanel(width,height);
                FillPanel(panel);AdamHud.Panel(art,MotionRect(panel));
                ChoiceHeader(panel,AdamRoundPresentation.Name(session.RoundNumber)+" · 보스 보상","하나를 선택하세요",AdamBossPresentation.Name(session)+" · 게임 각색",
                    AdamHud.PolishEnabled && AdamBossStaging.ShowsCandidateHeader(Host));
                PanelFooter(panel,"고정 골드 +"+AdamUiMotion.Gold(session.BossRules.KillGold,uiMotion.Elapsed,UiMotionEnabled)+" 적용됨","선택 즉시 저장",true);
                if(session.NeedsRewardImplementation)
                {if(ArtButton(new Rect(panel.x+24,panel.y+130,panel.width-48,56),"이전 보상 후보 준비",button))Host.PromoteBossReward();}
                else
                {
                    var candidates=session.BossChoices;var rewardId=session.Boss.Reward.RewardId;
                    for(var i=0;i<candidates.Count;i++)
                    {var candidate=candidates[i];if(ArtButton(mobile?AdamMobileLayout.ChoiceCard(panel,i):AdamChoiceText.ChoiceCard(panel,i),AdamBossPresentation.Card(candidate,i,session),choiceButton,candidate.Kind=="gold"?"gold":candidate.ItemId))ChooseBossReward(rewardId,candidate.Id);}
                }
            }
            else if (session != null && session.PendingChoices.Count > 0)
            {
                var panel = mobile ? AdamMobileLayout.ChoicePanel(width, height) : AdamChoiceText.ChoicePanel(width, height);
                FillPanel(panel);
                AdamHud.Panel(art,MotionRect(panel));
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
                var next=AdamRoundPresentation.Name(Host.State.Run.RoundNumber);
                Modal(width,height,$"{AdamRoundPresentation.Name(session.RoundNumber)} 완료 · {next} 라운드로 계속","성장·장비·영구 골드 유지 · 다음 라운드 진입 저장");
                var enabled=GUI.enabled;GUI.enabled=enabled && !Host.Suspended;
                if(ArtButton(mobile?AdamMobileLayout.Action(AdamMobileLayout.Panel(width,height),0,1):new Rect(width/2-180,height/2+20,360,56),mobile?next+" 라운드 계속":next+" 라운드 계속 (Enter)",button)){ResumePlay();inputReleased=false;touchStarted=false;}
                GUI.enabled=enabled;
            }
            else if(session?.Stage=="boss_settlement"||session?.Stage=="round_settlement")
            {
                Modal(width,height,"보상 적용됨 · 완료 정산",AdamBossPresentation.Forms(session));
                if(ArtButton(mobile?AdamMobileLayout.Action(AdamMobileLayout.Panel(width,height),0,1):new Rect(width/2-150,height/2+30,300,56),"완료 정산",button)){if(session.Stage=="boss_settlement")Host.CompleteBossSettlement();else Host.CompleteNormalSettlement();}
            }
            else if(Host.BookComplete)
            {
                DrawBookActions(width,height,mobile);
                DrawPermanentSkillEntry(new Rect(width-184, 8, 168, 36), "책 완료");
            }
            else if (session == null || !session.IsActive)
            {
                var heading=session==null?"시작 무기를 선택하세요":"사망 · 이번 도전 종료";
                if(AdamHud.PolishEnabled&&!mobile)DrawStartStage(width,height,session!=null);
                else
                {
                    if(mobile)
                    {
                        var panel=AdamMobileLayout.ChoicePanel(width,height);
                        FillPanel(panel);AdamHud.Panel(art,MotionRect(panel));
                        ChoiceHeader(panel,session==null?"창세기 · 새 도전":"사망 · 이번 도전 종료","시작 무기를 선택하세요",session==null?"이동 · 자동 공격 · 보석 수집":"도전 성장 초기화 · 영구 골드와 해금 유지");
                        for(var i=0;i<Weapons.Length;i++)
                            if(ArtButton(AdamMobileLayout.StartButton(width,height,i),AdamChoiceText.StartCard(Weapons[i],i)+"\n"+AdamChoiceText.StartWeaponEffect(Weapons[i]),choiceButton,Weapons[i]))
                                SelectSlot(i);
                    }
                    else
                    {
                        Modal(width,height,heading,session==null?"이동하면 자동 공격 · 빛나는 보석을 모아 성장하세요":"새 도전은 성장 초기화 · 영구 골드와 해금 유지",
                            session==null?AdamGameTitle.Korean:null);
                        for(var i=0;i<Weapons.Length;i++)
                            if(ArtButton(new Rect(width/2-240,height/2-15+i*58,480,50),AdamChoiceText.StartCard(Weapons[i],i),button,Weapons[i])) SelectSlot(i);
                    }
                }
                var entryY = AdamHud.PolishEnabled && !mobile ? height - 47 : 8;
                DrawPermanentSkillEntry(new Rect(width-184, entryY, 168, 38), session == null ? "시작 화면" : "다시 도전");
            }
            else if (Host.Paused || Host.Suspended)
            {
                DrawPauseOverlay(width, height);
            }
        }
        void DrawBookActions(float width,float height,bool mobile)
        {
            var panel=mobile?AdamMobileLayout.ChoicePanel(width,height):AdamChoiceText.ChoicePanel(width,height);
            FillPanel(panel);AdamHud.Panel(art,MotionRect(panel));
            var enabled=GUI.enabled;
            GUI.enabled=enabled && !Host.Suspended;
            try
            {
                if(NewChallengeOpen)
                {
                    ChoiceHeader(panel,"새 도전","시작 무기를 선택하세요","성장·장비 초기화 · 영구 골드와 해금 유지");
                    if(!mobile)PanelFooter(panel,"Esc 취소","↑ ↓ 선택 · Enter 확인");
                    if(ArtButton(BookAction(panel,0,4,mobile),"취소",button,focusOverride:newChallengeFocus==0,pointerOnly:true)) CancelNewChallenge();
                    for(var i=0;i<Weapons.Length;i++)
                        if(ArtButton(BookAction(panel,i+1,4,mobile),AdamChoiceText.ItemName(Weapons[i]),button,Weapons[i],focusOverride:newChallengeFocus==i+1,pointerOnly:true)) ConfirmNewChallenge(i);
                    return;
                }
                CompletionHeader(panel,AdamRoundPresentation.CompletionHeading(Host),AdamRoundPresentation.CompletionDetail(Host));
                var next=AdamRoundPresentation.NextChapter(Host);
                if(next!=0)
                {
                    GUI.enabled=enabled && !Host.Suspended && CanEnterChapter(next);
                    var nextName=AdamBookTitle.Name(next)+" 이어 가기";
                    var continueFocused=!focusNewChallenge&&!PermanentSkillEntryHasFocus;
                    if(ArtButton(BookAction(panel,0,2,mobile),mobile||!continueFocused?nextName:nextName+" (Enter)",button,focusOverride:continueFocused,pointerOnly:true)) EnterChapter(next);
                }
                GUI.enabled=enabled && !Host.Suspended && Host.CanStartNew;
                if(ArtButton(BookAction(panel,next==0?0:1,next==0?1:2,mobile),"새 도전…",button,focusOverride:!PermanentSkillEntryHasFocus&&(focusNewChallenge||next==0),pointerOnly:true)) OpenNewChallenge();
            }
            finally {GUI.enabled=enabled;}
        }

        static Rect BookAction(Rect panel,int slot,int count,bool mobile)
        {
            if(mobile)return AdamMobileLayout.Action(panel,slot,count);
            return new Rect(panel.x+24,panel.y+144+slot*54,panel.width-48,46);
        }

        void SyncSortieSession()
        {
            if(ReferenceEquals(sortieSession,Host?.Session))return;
            // A session swapped outside a tracked local action is a load/reload, not a departure.
            newChallengeSource=null;focusNewChallenge=false;
            sortie.Reset();sortieSession=Host?.Session;
        }
        void RecordSortieTransition(AdamSession previous,string previousRun)
        {
            var current=Host.Session;sortieSession=current;
            if(!AdamBookTitle.Enabled||current==null||!current.IsActive||current.Boss!=null||ReferenceEquals(previous,current))return;
            var run=Host.State?.Run?.RunId;
            if(run!=null&&(run!=previousRun||previous?.RoundNumber!=current.RoundNumber))
                sortie.Queue(AdamSortieRibbon.Identity(run,current.RoundNumber));
        }
        void ResumePlay()
        {
            var previous=Host.Session;var run=Host.State?.Run?.RunId;
            SetPlayPaused(false);RecordSortieTransition(previous,run);
        }
        void RetrySaveWithDeparture()
        {
            var previous=Host.Session;var run=Host.State?.Run?.RunId;
            Host.RetrySave();SyncBossStaging();RecordSortieTransition(previous,run);
        }
        void AdvanceSortie(double seconds)
        {
            var session=Host.Session;
            if(!AdamBookTitle.Enabled||session==null){sortie.Reset();return;}
            var identity=AdamSortieRibbon.Identity(Host.State?.Run?.RunId,session.RoundNumber);
            var blocked=Host.Error!=null||Host.Paused||Host.Suspended||session.PendingChoices.Count>0||!session.IsActive;
            sortie.Advance(identity,Host.CanPlay,blocked,session.Boss!=null,seconds,ArtWorld.FeedbackEnabled);
        }
        void DrawSortie(float width,AdamSession session,Vector2 arenaPosition)
        {
            if(!SortieVisible||session==null)return;
            var identity=AdamSortieRibbon.Identity(Host.State?.Run?.RunId,session.RoundNumber);
            if(!sortie.Visible(identity,ArtWorld.FeedbackEnabled))return;
            var rect=AdamBookTitle.Ribbon(width);rect.position-=arenaPosition;
            var extent=AdamSortieRibbon.Extent(sortie.Elapsed,ArtWorld.FeedbackEnabled);
            if(extent<=0)return;
            var surface=new Rect(rect.center.x-rect.width*extent/2,rect.y,rect.width*extent,rect.height);
            AdamHud.Fill(surface,AdamHud.Ink1);
            AdamHud.Fill(new Rect(surface.x,surface.y-2,surface.width,2),AdamHud.Ink0);
            AdamHud.Fill(new Rect(surface.x,surface.yMax,surface.width,2),AdamHud.Ink0);
            AdamHud.Fill(new Rect(surface.x,surface.y,surface.width,2),AdamHud.GuiColor(new Color32(180,124,74,255)));
            AdamHud.Fill(new Rect(surface.x,surface.yMax-2,surface.width,2),AdamHud.GuiColor(new Color32(90,56,32,255)));
            if(ArtWorld.FeedbackEnabled&&sortie.Elapsed<.2)return;
            AdamArt.Blit(AdamVisualQualityArt.Sprite(AdamBookTitle.Crest,AdamBookTitle.ArtFrame(session.RoundNumber)),new Rect(rect.x-16,rect.y+2,64,64));
            GUI.Label(new Rect(rect.x+60,rect.y+6,rect.width-72,22),AdamBookTitle.Name(session.RoundNumber)+" "+AdamBookTitle.Local(session.RoundNumber),HudText(16,AdamHud.Lapis));
            GUI.Label(new Rect(rect.x+60,rect.y+28,rect.width-72,24),AdamRoundPresentation.Name(session.RoundNumber),title);
            var pipWidth=(AdamBookTitle.Total(session.RoundNumber)-1)*18+12;
            AdamBookTitle.DrawPips(new Vector2(rect.xMax-pipWidth-12,rect.y+52),session.RoundNumber);
        }
        string UiSequence()
        {
            if(Host?.Error!=null)return "error";
            var session=Host?.Session;if(session==null)return "start";
            if(session.Stage=="boss_reward")return "reward:"+session.Boss.Reward.RewardId;
            if(session.PendingChoices.Count>0)return "growth:"+Host.State.Run.ChoiceSerial;
            if(DeathPresentationVisible)return "death_pose";
            if(NewChallengeOpen)return "new_challenge:"+session.RoundNumber;
            if(Host.BookComplete)return "complete:"+session.RoundNumber;
            if(!session.IsActive&&session.CurrentHealth<=0)return "death";
            if(Host.Paused||Host.Suspended)return "pause";
            return session.Stage;
        }
        Rect MotionRect(Rect rect,int slot=0)
        {if(AdamHud.PolishEnabled&&uiMotion.Sequence!="normal"&&uiMotion.Sequence!="boss"&&uiMotion.Sequence!="boss_intro")rect.y+=AdamUiMotion.EnterOffset(uiMotion.Elapsed,UiMotionEnabled,slot);return rect;}
        void StateChip(Rect rect,string text)
        {AdamHud.Panel(art,rect,"ui_chip_9slice_8",0,1);GUI.Label(rect,text,HudText(16,AdamHud.Muted,false,TextAnchor.MiddleCenter));}
        void DrawStartStage(float width,float height,bool death)
        {
            var arena=Arena(width,height);
            if(Host.World==null)AdamVisualQualityWorld.DrawBackdrop(art,arena,ArtWorld.FeedbackEnabled);
            var dim=AdamHud.Ink0;dim.a=AdamUiMotion.Dim(uiMotion.Elapsed,UiMotionEnabled,death);AdamHud.Fill(arena,dim);
            AdamHud.Fill(new Rect(0,0,width,110),AdamHud.Ink1);AdamHud.Fill(AdamHud.Footer(width,height),AdamHud.Ink1);
            if(AdamBookTitle.Enabled)
                AdamBookTitle.DrawBand(width,Host.Session?.RoundNumber,death,HudText(16,death?AdamHud.Secondary:AdamHud.Lapis),HudText(36,AdamHud.Primary,true),HudText(16,AdamHud.Muted));
            else
            {
            GUI.Label(new Rect(20,14,width-40,22),death?"도전 기록":AdamGameTitle.Korean,HudText(16,AdamHud.Lapis,false,TextAnchor.MiddleCenter));
            GUI.Label(new Rect(20,38,width-40,34),death?"사망 · 이번 도전 종료":"시작 무기를 선택하세요",title);
            GUI.Label(new Rect(20,78,width-40,22),death?"새 도전은 성장 초기화 · 영구 골드와 해금 유지":"이동하면 자동 공격 · 빛나는 보석을 모아 성장하세요",HudText(16,AdamHud.Secondary,false,TextAnchor.MiddleCenter));
            }
            if(Event.current.type==EventType.MouseMove||Event.current.type==EventType.MouseDrag||Event.current.type==EventType.MouseDown)
                startStage.Hover(Event.current.mousePosition,width,height);
            var previous=GUI.enabled;GUI.enabled=previous&&StartStageAvailable;
            for(var i=0;i<3;i++)
            {
                var hit=AdamStartStage.Card(width,height,i);var focused=startStage.Focus==i&&!PermanentSkillEntryHasFocus;
                var display=AdamStartStage.Visual(hit,false,uiMotion.Elapsed,ArtWorld.FeedbackEnabled,i);
                GUI.SetNextControlName("adam:start:"+Weapons[i]);
                var pointerConfirm=Event.current.isMouse;var clicked=GUI.Button(display,GUIContent.none,clearButton)&&pointerConfirm;
                AdamHud.Panel(art,display,"ui_card_9slice_24",!GUI.enabled?3:focused?1:0);
                if(focused)DrawFocusBracket(new Rect(display.x-8,display.y-8,display.width+16,display.height+16));
                AdamHud.Key(art,new Rect(display.x+10,display.y+10,28,28),(i+1).ToString(),HudText(20,AdamHud.Primary),focused);
                var wide=width>=880;var well=new Rect(display.center.x-(wide?44:38),display.y+(wide?24:12),wide?88:76,wide?88:76);
                AdamHud.Panel(art,well,"ui_chip_9slice_8",0,1);
                var emblem=new Rect(well.center.x-32,well.center.y-32,64,64);
                if(AdamVisualQualityArt.TryGetClip(AdamStartStage.Emblem(i),out var clip))AdamArt.Blit(AdamVisualQualityArt.Sprite(clip.id,0),emblem);
                else AdamHud.Image(art,AdamHud.Icon(Weapons[i]),emblem);
                GUI.Label(new Rect(display.x+6,display.y+(wide?128:96),display.width-12,30),AdamChoiceText.ItemName(Weapons[i]),HudText(20,AdamHud.Primary,false,TextAnchor.MiddleCenter));
                GUI.Label(new Rect(display.x+6,display.y+(wide?166:128),display.width-12,26),AdamChoiceText.StartWeaponEffect(Weapons[i]),HudText(16,AdamHud.Secondary,false,TextAnchor.MiddleCenter));
                if(focused)
                {
                    var y=display.y+(wide?210:158);AdamHud.Key(art,new Rect(display.center.x-62,y,58,24),"Enter",HudText(16,AdamHud.Primary),true);
                    GUI.Label(new Rect(display.center.x+4,y,62,24),death?"새 도전":"시작",HudText(16,AdamHud.Secondary));
                }
                if(clicked)SelectSlot(i);
            }
            GUI.enabled=previous;
            var action=death?"새 도전":"시작";
            var legend=PermanentSkillEntryHasFocus?"← → 무기 고르기    1 2 3 바로 "+action:
                "← → 고르기    Enter "+action+"    1 2 3 바로 "+action;
            if(AdamBookTitle.Enabled)
                GUI.Label(new Rect(16,height-68,width-32,22),death?"새 도전은 성장 초기화 · 영구 골드와 해금 유지":"이동하면 자동 공격 · 빛나는 보석을 모아 성장하세요",HudText(16,AdamHud.Secondary,false,TextAnchor.MiddleCenter));
            var legendWidth = CanShowPermanentSkillEntry ? width - 210 : width - 32;
            GUI.Label(new Rect(16,height-(AdamBookTitle.Enabled?38:58),legendWidth,26),legend,HudText(16,AdamHud.Muted,false,TextAnchor.MiddleCenter));
        }
        void DrawFocusBracket(Rect rect)
        {
            if(AdamVisualQualityArt.TryGetClip("ui_focus-bracket_9slice_12",out var clip))AdamHud.Panel(art,rect,clip.id,AdamUiMotion.BracketFrame(uiMotion.Elapsed,UiMotionEnabled));
            else
            {
                foreach(var x in new[]{rect.x,rect.xMax-12})foreach(var y in new[]{rect.y,rect.yMax-12})
                {AdamHud.Fill(new Rect(x,y,12,2),AdamHud.Lapis);AdamHud.Fill(new Rect(x,y,2,12),AdamHud.Lapis);}
            }
        }
        GUIStyle HudText(int size,Color color,bool bold=false,TextAnchor align=TextAnchor.MiddleLeft)
        {return new GUIStyle(label){font=bold&&size>=24?fonts.Title:size<=16?fonts.Small:fonts.Body,fontSize=size,alignment=align,wordWrap=false,normal={textColor=color}};}
        void DrawLegacyHud(float width,float height,AdamSession session,bool mobile)
        {
            AdamHud.Fill(new Rect(0,0,width,110),AdamHud.Ink1);
            AdamHud.Rule(art,new Rect(0,109,width,2),true);
            var body=HudText(16,AdamHud.Secondary);
            GUI.Label(new Rect(20,8,width*.36f,24),AdamRoundPresentation.BookLabel(session.RoundNumber)+" · "+AdamRoundPresentation.Name(session.RoundNumber),body);
            var hp=AdamHud.Health(width);AdamHud.Bar(art,hp,"ui_bar-hp_tile_1x2",AdamHud.Fraction(session.CurrentHealth,Host.State.Run.MaxHealth));
            GUI.Label(new Rect(hp.x,hp.yMax+1,hp.width,24),$"체력 {session.CurrentHealth:0} / {Host.State.Run.MaxHealth:0}",body);
            GUI.Label(AdamHud.TimerRect(width),AdamHud.Timer(session,Host.World?.SimulationTime??0),HudText(36,AdamHud.Primary,true,TextAnchor.MiddleCenter));
            GUI.Label(new Rect(width/2-66,52,132,24),AdamHud.TimerRole(session),HudText(16,AdamHud.Muted,false,TextAnchor.MiddleCenter));
            AdamHud.Image(art,"ico_res_gold_static_16",new Rect(width-246,23,24,24));
            GUI.Label(new Rect(width-218,20,70,30),Host.State.Profile.Gold.ToString(),HudText(20,AdamHud.Gold));
            if(ArtButton(AdamHud.Pause(width,mobile),Host.Paused?"재개":"일시정지",button)){SetPlayPaused(!Host.Paused);inputReleased=false;}
            var level=new Rect(20,82,66,26);AdamHud.Panel(art,level,"ui_chip_9slice_8");
            GUI.Label(level,"Lv "+session.Growth.Level,HudText(16,AdamHud.Lapis,true,TextAnchor.MiddleCenter));
            var xp=AdamHud.Experience(width);var required=ExperienceRules.Required(session.Growth.Level);
            AdamHud.Bar(art,xp,"ui_bar-xp_tile_1x4",AdamHud.Fraction(session.Growth.Experience,required));
            GUI.Label(new Rect(xp.x,63,xp.width,22),$"경험치 {session.Growth.Experience:0.#} / {required}",HudText(16,AdamHud.Muted,false,TextAnchor.MiddleRight));
            var footer=AdamHud.Footer(width,height);AdamHud.Fill(footer,AdamHud.Ink1);AdamHud.Rule(art,new Rect(0,footer.y,width,2),true);
            DrawInventory(height,session,body);
            var hintX=Mathf.Max(350,width*.5f);var y=height-74;
            Hint(hintX,y,mobile?"드래그":"WASD",mobile?"이동":"이동",mobile?50:42);Hint(hintX+84,y,"Esc","정지",30);Hint(hintX+158,y,"Enter","재개",44);
            Hint(hintX,y+24,"1–3","선택",34);Hint(hintX+80,y+24,"F6",ArtWorld.FeedbackEnabled?"효과 기본":"효과 낮음",26);Hint(hintX+192,y+24,"F7",ImpactAudio?.Muted==true?"소리 끔":"소리 켬",26);
            Hint(hintX,y+48,"F8",$"음량 {ImpactAudio?.Volume*100:0}%",26);Hint(hintX+144,y+48,"F9",ImpactParticles?.Enabled==true?"입자 켬":"입자 끔",26);
        }
        void DrawLegacyBossHud(float width,AdamSession session,GUIStyle instruction)
        {
            var boss=session.Boss;
            AdamHud.Fill(new Rect(28,117,width-56,64),AdamHud.Ink1);
            AdamHud.Bar(art,new Rect(32,120,width-64,16),"ui_bar-boss_tile_1x4",AdamBossPresentation.HealthFraction(session));
            GUI.Label(new Rect(34,136,width-68,22),$"{AdamBossPresentation.Name(session)}  {boss.Health:0} / {session.BossRules.MaxHealth:0}",HudText(16,AdamHud.Primary));
            AdamHud.Panel(art,new Rect(32,158,width-64,22),"ui_chip_9slice_8",boss.Telegraphing?1:0,1);
            GUI.Label(new Rect(40,158,width-80,22),AdamBossPresentation.Phase(session),instruction);
        }
        void DrawHud(float width,float height,AdamSession session,bool mobile)
        {
            var backing=AdamHud.Ink1;
            if(mobile)backing.a=.72f;
            var headerHeight=mobile?116:110;
            AdamHud.Fill(new Rect(0,0,width,headerHeight),backing);
            AdamHud.Rule(art,new Rect(0,headerHeight-1,width,2),true);
            var body=HudText(16,AdamHud.Secondary);
            var bookRect = mobile ? new Rect(12,72,width-24,20) : new Rect(20,42,width*.36f,24);
            GUI.Label(bookRect,AdamRoundPresentation.BookLabel(session.RoundNumber)+" · "+AdamRoundPresentation.Name(session.RoundNumber),HudText(16,AdamHud.Muted));
            var hp=mobile?AdamMobileLayout.HudHealth(width):AdamHud.Health(width);
            AdamHud.Bar(art,hp,"ui_bar-hp_tile_1x2",AdamHud.Fraction(session.CurrentHealth,Host.State.Run.MaxHealth));
            var hpValue=mobile?new Rect(hp.x+4,hp.y,hp.width-8,hp.height):AdamHud.HealthValue(width);
            var hpText=$"{session.CurrentHealth:0} / {Host.State.Run.MaxHealth:0}";
            var shadow=hpValue;shadow.y+=1;GUI.Label(shadow,hpText,HudText(16,AdamHud.Ink0,false,TextAnchor.MiddleRight));
            GUI.Label(hpValue,hpText,HudText(16,AdamHud.Primary,false,TextAnchor.MiddleRight));
            var timer=mobile?AdamMobileLayout.HudTimer(width):AdamHud.TimerRect(width);
            GUI.Label(timer,AdamHud.Timer(session,Host.World?.SimulationTime??0),HudText(36,AdamHud.Primary,true,TextAnchor.MiddleCenter));
            GUI.Label(new Rect(timer.x,timer.yMax,timer.width,mobile?20:24),AdamHud.TimerRole(session),HudText(16,AdamHud.Muted,false,TextAnchor.MiddleCenter));
            if(mobile)
                GUI.Label(new Rect(hp.x,40,hp.width,28),"골드 "+Host.State.Profile.Gold,HudText(16,AdamHud.Gold));
            else
            {
                AdamHud.Image(art,"ico_res_gold_static_16",new Rect(width-246,23,24,24));
                GUI.Label(new Rect(width-218,20,70,30),Host.State.Profile.Gold.ToString(),HudText(20,AdamHud.Gold));
            }
            if(ArtButton(mobile?AdamMobileLayout.HudPause(width):AdamHud.Pause(width,false),Host.Paused?"재개":"일시정지",button)){SetPlayPaused(!Host.Paused);inputReleased=false;}
            var level=new Rect(mobile?12:20,mobile?93:80,66,mobile?20:22);AdamHud.Panel(art,level,"ui_chip_9slice_8");
            GUI.Label(level,"Lv "+session.Growth.Level,HudText(16,AdamHud.Lapis,true,TextAnchor.MiddleCenter));
            var xp=mobile?AdamMobileLayout.HudExperience(width):AdamHud.Experience(width);
            var required=ExperienceRules.Required(session.Growth.Level);
            AdamHud.Bar(art,xp,"ui_bar-xp_tile_1x4",AdamHud.Fraction(session.Growth.Experience,required));
            GUI.Label(mobile?AdamMobileLayout.HudExperienceValue(width):AdamHud.ExperienceValue(width),$"{session.Growth.Experience:0.#} / {required}",HudText(16,AdamHud.Muted,false,TextAnchor.MiddleRight));
            var footer=mobile?AdamMobileLayout.HudFooter(width,height):AdamHud.Footer(width,height);
            AdamHud.Fill(footer,backing);AdamHud.Rule(art,new Rect(0,footer.y,width,2),true);
            DrawInventory(height,session,body);
            if(mobile)
            {
                GUI.Label(AdamMobileLayout.HudFooterControl(width,height,0),"드래그 이동",body);
                if(!ArtWorld.FeedbackEnabled)StateChip(AdamMobileLayout.HudFooterControl(width,height,1),"효과 낮음");
                if(ImpactAudio?.Muted==true)StateChip(AdamMobileLayout.HudFooterControl(width,height,2),"소리 끔");
                return;
            }
            Hint(width-136,height-68,"WASD","이동",42);
            var chipX=width-136;
            if(!ArtWorld.FeedbackEnabled){StateChip(new Rect(chipX,height-44,104,24),"효과 낮음");chipX-=112;}
            if(ImpactAudio?.Muted==true)StateChip(new Rect(chipX,height-44,104,24),"소리 끔");
        }
        void DrawInventory(float height, AdamSession session, GUIStyle text)
        {
            var rules = session.CombatRulesVersion;
            var slot = 0;
            foreach (var pair in session.Inventory.Weapons)
            {
                var evolved = session.Inventory.Forms.TryGetValue(pair.Key, out var form);
                var iconId = evolved && rules == 2 && ExpandedWeaponRules.TryGetByEvolution(form, out _) ? form : pair.Key;
                AdamHud.Item(art, AdamHud.InventorySlot(height, slot++, true, rules), iconId, pair.Value, 8, evolved, text);
            }

            slot = 0;
            foreach (var pair in session.Inventory.Passives)
                AdamHud.Item(art, AdamHud.InventorySlot(height, slot++, false, rules), pair.Key, pair.Value,
                    pair.Key == "stones" ? 2 : 5, false, text);
        }
        void Hint(float x,float y,string key,string description,float keyWidth)
        {var text=HudText(16,AdamHud.Muted);AdamHud.Key(art,new Rect(x,y,keyWidth,21),key,text);GUI.Label(new Rect(x+keyWidth+4,y,90,21),description,text);}
        void DrawBossHud(float width,AdamSession session,GUIStyle instruction)
        {
            var boss=session.Boss;
            var backing=AdamHud.Ink1;
            if(drawingMobile)backing.a=.72f;
            AdamHud.Fill(AdamHud.BossBacking(width),backing);
            AdamHud.Bar(art,new Rect(32,121,width-64,12),"ui_bar-boss_tile_1x4",bossStaging.HealthFraction(Host, UiMotionEnabled));
            var name=AdamBossPresentation.Name(session);var nameStyle=HudText(16,AdamHud.Primary);
            GUI.Label(new Rect(34,135,width-220,22),name,nameStyle);
            GUI.Label(new Rect(width-190,135,156,22),$"{boss.Health:0} / {session.BossRules.MaxHealth:0}",HudText(16,AdamHud.Secondary,false,TextAnchor.MiddleRight));
            var phase=AdamBossPresentation.Phase(session);
            if (session.Stage == "boss_intro")
            {
                var w = Mathf.Min(width - 64, instruction.CalcSize(new GUIContent(phase)).x + 24);
                StateChip(new Rect((width - w) / 2, 164, w, 24), phase);
            }
            else if(boss.Telegraphing)
            {
                var w=Mathf.Min(width-64,instruction.CalcSize(new GUIContent(phase)).x+24);
                var chip=new Rect((width-w)/2,164,w,24);AdamHud.Panel(art,chip,"ui_chip_9slice_8",1,1);
                GUI.Label(chip,phase,HudText(16,AdamHud.Primary,false,TextAnchor.MiddleCenter));
            }
            else
            {
                var x=40+nameStyle.CalcSize(new GUIContent(name)).x;var available=width-202-x;
                if(available>0)GUI.Label(new Rect(x,135,available,22),phase,HudText(16,AdamHud.Muted));
            }
        }
        void DrawPauseOverlay(float width, float height)
        {
            EnsureStyles();
            var resumeInput = PermanentSkillEntryHasFocus ? "아래 재개" : "Enter 또는 아래 재개";
            var resumeHelp = $"이동 입력을 놓은 뒤 {resumeInput}를 누르세요.\n앱 중단 중에는 전투 시간이 흐르지 않습니다.";
            if(drawingMobile||!AdamHud.PolishEnabled)Modal(width,height,"일시정지 · 저장됨",drawingMobile?"손가락을 놓은 뒤 아래 재개를 누르세요.\n앱 중단 중에는 전투 시간이 흐르지 않습니다.":resumeHelp);
            else
            {
                var panel=AdamHud.PausePanel(width,height);FillPanel(panel);AdamHud.Panel(art,MotionRect(panel));
                ChoiceHeader(panel,"진행 저장됨","일시정지","");
                PauseSetting(AdamHud.PauseSetting(panel,0),"F6","효과",ArtWorld.FeedbackEnabled?"기본":"낮음");
                PauseSetting(AdamHud.PauseSetting(panel,1),"F7","소리",ImpactAudio?.Muted==true?"끔":"켬");
                PauseSetting(AdamHud.PauseSetting(panel,2),"F8","음량",$"{ImpactAudio?.Volume*100:0}%");
                PauseSetting(AdamHud.PauseSetting(panel,3),"F9","입자",ImpactParticles?.Enabled==true?"켬":"끔");
                GUI.Label(MotionRect(new Rect(panel.x+24,panel.y+218,panel.width-48,40)),resumeHelp,HudText(16,AdamHud.Muted,false,TextAnchor.MiddleCenter));
            }
            var enabled = GUI.enabled;
            GUI.enabled = enabled && Host != null && Host.Error == null && !Host.Suspended;
            if (ArtButton(AdamHud.PauseResume(width,height,drawingMobile), drawingMobile ? "재개" : CompetingEnterHint("재개 (Enter)"), button))
            {
                ResumePlay(); inputReleased = false; touchStarted = false;
            }
            GUI.enabled = enabled;
            var resume = AdamHud.PauseResume(width, height, drawingMobile);
            var entry = width >= 640
                ? new Rect(resume.xMax + 12, resume.y, width - resume.xMax - 26, resume.height)
                : new Rect(resume.center.x - 84, resume.yMax + 8, 168, 44);
            DrawPermanentSkillEntry(entry, "일시정지");
        }
        void PauseSetting(Rect rect,string key,string name,string value)
        {
            rect=MotionRect(rect);
            AdamHud.Key(art,new Rect(rect.x,rect.y+4,32,26),key,HudText(16,AdamHud.Muted));
            GUI.Label(new Rect(rect.x+42,rect.y,140,36),name,HudText(16,AdamHud.Secondary));
            GUI.Label(new Rect(rect.x+180,rect.y,rect.width-180,36),value,HudText(16,AdamHud.Primary,false,TextAnchor.MiddleRight));
        }
        void EnsureStyles()
        {
            if (label != null) return;
            art = new AdamArt();
            fonts = new AdamFonts();
            label = new GUIStyle(GUI.skin.label) { font = fonts.Body, fontSize = 20, alignment = TextAnchor.MiddleCenter, normal = { textColor = AdamHud.Primary } };
            title = new GUIStyle(label) { font = fonts.Title, fontSize = 24, fontStyle = FontStyle.Normal };
            button = new GUIStyle(GUI.skin.button) { font = fonts.Label, fontSize = 24, fixedHeight = 50 };
            choiceButton = new GUIStyle(button) { fontSize = 24, fixedHeight = 0, wordWrap = true, alignment = TextAnchor.MiddleLeft, padding = new RectOffset(16,16,8,8) };
            choiceButton.focused.textColor = AdamHud.Gold;
            clearButton = new GUIStyle(button); clearChoice = new GUIStyle(choiceButton) { padding = new RectOffset(36,16,8,8) };
            foreach(var clear in new[] { clearButton, clearChoice })
                foreach(var state in new[] { clear.normal, clear.hover, clear.active, clear.focused }) state.background=null;
        }
        bool ArtButton(Rect rect, string text, GUIStyle style,string itemId=null,int currentLevel=0,int maximum=0,bool? focusOverride=null,bool pointerOnly=false)
        {
            var card=ReferenceEquals(style,choiceButton);
            var name="adam:"+text.Split('\n')[0];GUI.SetNextControlName(name);
            var display=MotionRect(rect);
            var hitTarget=pointerOnly?display:rect;
            var focused=focusOverride??(GUI.GetNameOfFocusedControl()==name);var hover=hitTarget.Contains(Event.current.mousePosition);
            var pressed=hover&&GUIUtility.hotControl!=0;
            var state=AdamHud.SurfaceState(GUI.enabled,focused,hover,pressed);
            AdamHud.Panel(art,display,card?"ui_card_9slice_24":"ui_button_9slice_16",state);
            var clear=card?clearChoice:clearButton;
            // Book actions keep their hit target on the animated button.
            var canClick=!pointerOnly||Event.current.isMouse;
            var clicked=GUI.Button(hitTarget,GUIContent.none,clear)&&canClick;
            var content=display;if(pressed)content.y+=2;
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
        void ChoiceHeader(Rect panel, string eyebrow, string heading, string detail, bool bossHeader = false)
        {
            var firstCard = bossHeader ? MotionRect(AdamChoiceText.ChoiceCard(panel, 0)) : default;
            panel = MotionRect(panel);
            var eyebrowArea = new Rect(panel.x + 16, panel.y + 8, panel.width - 32, 22);
            var headingArea = new Rect(panel.x + 16, panel.y + 30, panel.width - 32, 32);
            var detailArea = new Rect(panel.x + 16, panel.y + 64, panel.width - 32, 24);
            var eyebrowStyle = HudText(16, AdamHud.Lapis, false, TextAnchor.MiddleCenter);
            var detailStyle = HudText(16, AdamHud.Muted, false, TextAnchor.MiddleCenter);
            if (bossHeader)
            {
                var tick = bossStaging.HeaderTick(Host, UiMotionEnabled);
                var eighths = AdamBossStaging.RuleEighths(tick);
                if (eighths > 0)
                {
                    var lineWidth = 2 * Mathf.RoundToInt(140f * eighths / 8);
                    var rule = new Rect(panel.center.x - lineWidth / 2f, panel.y + 88, lineWidth, 2);
                    if (!rule.Overlaps(eyebrowArea) && !rule.Overlaps(headingArea) && !rule.Overlaps(detailArea) &&
                        !rule.Overlaps(firstCard))
                        AdamHud.Fill(rule, AdamHud.GuiColor(new Color32(180, 124, 74, 255)));

                    var crest = new Rect(panel.x + 16, panel.y + (tick == 6 ? 10 : 14), 64, 64);
                    var frame = AdamBookTitle.ArtFrame(Host.Session.RoundNumber);
                    if (ClearsCrest(crest, eyebrowArea, eyebrowStyle, eyebrow) &&
                        ClearsCrest(crest, headingArea, title, heading) &&
                        ClearsCrest(crest, detailArea, detailStyle, detail) &&
                        !crest.Overlaps(firstCard) &&
                        AdamVisualQualityArt.TryGetClip(AdamBookTitle.Crest, out var clip) && frame < clip.frames)
                        AdamArt.Blit(AdamVisualQualityArt.Sprite(AdamBookTitle.Crest, frame), crest);
                }
            }
            GUI.Label(eyebrowArea, eyebrow, eyebrowStyle);
            GUI.Label(headingArea, heading, title);
            GUI.Label(detailArea, detail, detailStyle);
        }
        static bool ClearsCrest(Rect crest, Rect labelArea, GUIStyle style, string text)
        {
            var size = style.CalcSize(new GUIContent(text));
            var bounds = new Rect(labelArea.center - size / 2, size);
            return crest.xMax + 8 <= bounds.xMin || crest.yMax + 8 <= bounds.yMin ||
                bounds.yMax + 8 <= crest.yMin;
        }
        void PanelFooter(Rect panel,string result,string guidance,bool gold=false)
        {
            panel=MotionRect(panel);
            var complete=AdamHud.PolishEnabled&&uiMotion.Sequence.StartsWith("complete:",StringComparison.Ordinal);
            var grow=complete?AdamUiMotion.CompletionRule(uiMotion.Elapsed,UiMotionEnabled):1;
            var stacked=drawingMobile && panel.width<550 && !string.IsNullOrEmpty(guidance);
            if(grow>0)AdamHud.Rule(art,new Rect(panel.center.x-(panel.width-48)*grow/2,panel.yMax-(stacked?54:34),(panel.width-48)*grow,1),complete);
            var color=gold?AdamHud.Gold:AdamHud.Secondary;
            if(result.StartsWith("◆",StringComparison.Ordinal)&&!AdamUiMotion.CautionBright(uiMotion.Elapsed,UiMotionEnabled))color=AdamHud.Muted;
            var resultArea=new Rect(panel.x+24,panel.yMax-30,panel.width-48,24);
            var guidanceArea=resultArea;
            if(stacked) resultArea.y-=20;
            GUI.Label(resultArea,result,HudText(16,color));
            GUI.Label(guidanceArea,guidance,HudText(16,AdamHud.Muted,false,TextAnchor.MiddleRight));
        }
        void CompletionHeader(Rect panel,string heading,string detail)
        {
            var split=heading.IndexOf(" · ",StringComparison.Ordinal);
            var status=split<0?"진행 저장됨":heading.Substring(0,split);
            var eyebrow=drawingMobile?status:AdamGameTitle.Korean+" · "+status;
            var headline=split<0?heading:heading.Substring(split+3);
            ChoiceHeader(panel,eyebrow,headline,"");
            var lines=detail.Split('\n');var caution=lines[lines.Length-1];
            var summary=lines.Length>1?string.Join("\n",lines,0,lines.Length-1):detail;
            var style=HudText(20,AdamHud.Secondary,false,TextAnchor.MiddleCenter);style.wordWrap=true;
            GUI.Label(MotionRect(new Rect(panel.x+24,panel.y+72,panel.width-48,66)),summary,style);
            PanelFooter(panel,"◆ "+caution,drawingMobile?"":"↑ ↓ 선택 · Enter 확인",true);
        }
        void Modal(float width, float height, string heading, string detail, string eyebrow = null)
        {
            var panel = drawingMobile ? AdamMobileLayout.Panel(width, height) : new Rect(width / 2 - 310, height / 2 - 160, 620, 340);
            FillPanel(panel);
            AdamHud.Panel(art,MotionRect(panel));
            GUI.Label(MotionRect(new Rect(panel.x+16,panel.y+10,panel.width-32,20)),eyebrow ?? (Host?.Error!=null?"진행 보호":"도전 기록"),HudText(16,AdamHud.Lapis,false,TextAnchor.MiddleCenter));
            GUI.Label(MotionRect(new Rect(panel.x+16,panel.y+32,panel.width-32,40)),heading,title);
            var detailStyle = new GUIStyle(label) { wordWrap = true, fontSize = 20 };
            GUI.Label(MotionRect(new Rect(panel.x + 24, panel.y + 78, panel.width - 48, 58)), detail, detailStyle);
        }
        void FillPanel(Rect panel)
        {
            panel=MotionRect(panel);
            var previous = GUI.color;
            GUI.color = AdamHud.Ink2;
            GUI.DrawTexture(panel, Texture2D.whiteTexture);
            GUI.color = previous;
        }
    }
}
