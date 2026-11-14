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
        bool focused = true;
        bool appPaused;
        bool touchStarted;
        int touchId;
        Vector2 touchOrigin;
        int commandFrame = -1;
        string startupError;
        bool drawingMobile;
        Rect previousTouchSafe;
        GUIStyle label, button, title, choiceButton, clearButton, clearChoice, cardHeading, cardEffect;
        AdamFonts fonts;
        AdamArt art;
        public AdamImpactAudio ImpactAudio {get;private set;}
        public AdamArtWorld ArtWorld { get; } = new AdamArtWorld();
        // Reviewed death strip: six frames at 8 fps. Keep the actor visible before covering it with restart controls.
        public bool DeathPresentationVisible => Host?.Session != null && !Host.Session.IsActive && Host.Session.CurrentHealth <= 0 &&
            ArtWorld.PlayerMotion == "death" && ArtWorld.PlayerSeconds < 6d / 8;
        static readonly Color TextColor = new Color32(244,239,225,255); // art03 text.0
        public static readonly string[] Weapons = { "bible", "jawbone", "sling" };
        void OnEnable() => EnhancedTouchSupport.Enable();
        void OnDisable() {EnhancedTouchSupport.Disable();ImpactAudio?.Stop();}
        void OnDestroy() { ArtWorld.Clear(); ImpactAudio?.Dispose(); fonts?.Dispose(); }

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
        public void Initialize(AdamHost host) { Host = host; inputReleased = false; ImpactAudio?.Dispose(); ImpactAudio=new AdamImpactAudio(gameObject); }
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
            if(keys!=null && keys.f7Key.wasPressedThisFrame) ImpactAudio.Muted=!ImpactAudio.Muted;
            if(keys!=null && keys.f8Key.wasPressedThisFrame) ImpactAudio.Volume=ImpactAudio.Volume>=.99f?.25f:Mathf.Min(1,ImpactAudio.Volume+.25f);
            if (keys != null && keys.escapeKey.wasPressedThisFrame && Host.Session != null && Host.Session.IsActive)
            { Host.SetPaused(!Host.Paused); inputReleased = false; touchStarted = false; }
            var slot = keys == null ? -1 : keys.digit1Key.wasPressedThisFrame ? 0 : keys.digit2Key.wasPressedThisFrame ? 1 : keys.digit3Key.wasPressedThisFrame ? 2 : -1;
            if (slot >= 0) SelectSlot(slot);
            if (keys != null && keys.enterKey.wasPressedThisFrame && Host.Paused && Host.Error == null)
            { Host.SetPaused(false); inputReleased = false; }
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
            ArtWorld.Update(Host, Time.unscaledDeltaTime);
            ImpactAudio?.Update(Host,ArtWorld);
        }
        public void SelectSlot(int slot)
        {
            if (Host == null || Host.Error != null || Host.Suspended || DeathPresentationVisible || commandFrame == Time.frameCount || slot < 0 || slot > 2) return;
            commandFrame = Time.frameCount;
            if (Host.Session == null || (!Host.Session.IsActive && Host.Session.PendingChoices.Count == 0)) Host.Start(Weapons[slot]);
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
            GUI.Box(new Rect(0, 0, width, height), "");
            var modalOpen = Host != null && (Host.Error != null || Host.Session == null || !Host.Session.IsActive || Host.Session.PendingChoices.Count > 0 || Host.Paused || Host.Suspended);
            // A modal overlaps HUD rows at small sizes; keep all underlying HUD text/controls out of the modal layer.
            if (!modalOpen) GUI.Label(new Rect(20, 10, width - 40, 36), "창세기 / 아담 · 3분 시제품", title);
            var instruction = new GUIStyle(label) { font = mobile ? fonts.Body : fonts.Small, fontSize = mobile ? 20 : 16, wordWrap = true };
            if (!modalOpen)
            {
                GUI.Label(new Rect(20, height - 64, width - 40, 28), mobile ? "왼쪽 아래 드래그 이동 · 자동 공격" : "WASD / 방향키 이동 · 자동 공격 · Esc 정지 · Enter 재개", instruction);
                GUI.Label(new Rect(20, height - 36, width - 40, 28), mobile ? "빛나는 보석 수집 · 5초 자동 저장" :  $"1–3 선택 · F6 효과 {(ArtWorld.FeedbackEnabled ? "기본" : "낮음")} · F7 소리 {(ImpactAudio?.Muted == true ? "끔" : "켬")} · F8 음량 {ImpactAudio?.Volume*100:0}%", instruction);
            }
            if (Host == null)
            { GUI.Label(new Rect(30, 120, width - 60, 160), startupError ?? "저장 기록을 불러오는 중…", label); return; }
            var session = Host.Session;
            if (session != null && !modalOpen)
            {
                GUI.Label(new Rect(20, 50, width - 195, 56), $"체력 {session.CurrentHealth:0}/{Host.State.Run?.MaxHealth ?? 100:0}  경험치 {session.Growth.Experience:0.0}  레벨 {session.Growth.Level}\n시간 {Host.World?.SimulationTime ?? 180:0.0}/180초  골드 {Host.State.Profile.Gold}", label);
                if (ArtButton(new Rect(width - 175, 54, 150, mobile ? 66 : 44), Host.Paused ? "재개" : "일시정지", button)) { Host.SetPaused(!Host.Paused); inputReleased = false; }
            }
            var arena = new Rect(20, 116, width - 40, height - 194);
            art.OptionalEffects=ArtWorld.FeedbackEnabled;
            if (Host.World != null) ArtWorld.Draw(art, Host, arena);
            if (Host.Error != null)
            {
                Modal(width, height, "저장 / 불러오기 중단", mobile ? Host.RequiresNewerGame ? "더 새 게임 버전이 필요한 저장입니다.\n원본과 백업은 보존했습니다. 게임을 업데이트하세요." : "저장할 수 없어 진행을 멈췄습니다.\n재시도하거나 마지막 저장을 불러오세요." : Host.Error + "\n전투가 정지됐습니다. 다시 불러오면 미저장 진행을 버립니다.");
                var previousEnabled = GUI.enabled;
                GUI.enabled = previousEnabled && (!mobile || Host.HasSaveRetry);
                if (ArtButton(mobile ? new Rect(24, height / 2 + 40, (width - 60) / 2, 66) : new Rect(width / 2 - 220, height / 2 + 40, 210, 50), mobile && !Host.HasSaveRetry ? "재시도 불가" : "같은 저장 재시도", button)) Host.RetrySave();
                GUI.enabled = previousEnabled;
                if (ArtButton(mobile ? new Rect(width / 2 + 6, height / 2 + 40, (width - 60) / 2, 66) : new Rect(width / 2 + 10, height / 2 + 40, 210, 50), "저장 다시 불러오기", button)) Host.Reload();
            }
            else if (session != null && session.PendingChoices.Count > 0)
            {
                var panel = mobile ? AdamMobileLayout.ChoicePanel(width, height) : AdamChoiceText.ChoicePanel(width, height);
                FillPanel(panel);
                art.Panel(panel);
                GUI.Label(new Rect(panel.x + 12, panel.y + 10, panel.width - 24, 36), "레벨 상승 · 하나를 선택하세요", title);
                GUI.Label(new Rect(panel.x + 12, panel.y + 48, panel.width - 24, 36), mobile ? "전투 정지 중 · 선택 즉시 저장" : "전투 정지 중 · 숫자 1–3 / 클릭 · 선택 즉시 저장", instruction);
                for (var i = 0; i < session.PendingChoices.Count; i++)
                {
                    var choice = AdamChoiceText.Describe(session.PendingChoices[i], session.Inventory, session.DamageMultiplier);
                    if (ArtButton(mobile ? AdamMobileLayout.ChoiceCard(panel, i) : AdamChoiceText.ChoiceCard(panel, i), choice.Card(i), choiceButton)) SelectSlot(i);
                }
                GUI.Label(new Rect(panel.x + 12, panel.yMax - (mobile ? 48 : 30), panel.width - 24, mobile ? 44 : 24), mobile ? "기본 무기 수치 · 보조/영구 강화 별도 · 진화 미구현" : "무기 수치는 기본값 · 보조/영구 강화는 별도 합산 · 진화 미구현", instruction);
            }
            else if (DeathPresentationVisible)
            {
                if (Host.Paused || Host.Suspended) DrawPauseOverlay(width, height);
                else GUI.Label(new Rect(20, height - 60, width - 40, 36), "사망 · 이번 도전이 끝났습니다", label);
            }
            else if (session == null || !session.IsActive)
            {
                var heading = session == null ? "시작 무기를 선택하세요" : Host.State.Run == null ? "사망 · 이번 도전 종료" : "아담 완료 · 저장됨";
                Modal(width, height, heading, session == null ? "이동하면 자동 공격 · 빛나는 보석을 모아 성장하세요" : "새 도전은 성장 초기화 · 영구 골드와 해금 유지");
                for (var i = 0; i < 3; i++)
                    if (ArtButton(mobile ? AdamMobileLayout.StartButton(width, height, i) : new Rect(width / 2 - 240, height / 2 - 15 + i * 58, 480, 50), $"{i + 1}. {AdamChoiceText.ItemName(Weapons[i])}", button)) SelectSlot(i);
            }
            else if (Host.Paused || Host.Suspended)
            {
                DrawPauseOverlay(width, height);
            }
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
            cardHeading = new GUIStyle(label) { font = fonts.Label, fontSize = 24, alignment = TextAnchor.UpperLeft, padding = new RectOffset() };
            cardEffect = new GUIStyle(cardHeading) { font = fonts.Body, fontSize = 20 };
            choiceButton.focused.textColor = Color.yellow;
            clearButton = new GUIStyle(button); clearChoice = new GUIStyle(choiceButton) { padding = new RectOffset(36,16,8,8) };
            foreach(var clear in new[] { clearButton, clearChoice })
                foreach(var state in new[] { clear.normal, clear.hover, clear.active, clear.focused }) state.background=null;
        }
        bool ArtButton(Rect rect, string text, GUIStyle style)
        {
            var card = ReferenceEquals(style, choiceButton);
            var name = "adam:" + text.Split('\n')[0];
            GUI.SetNextControlName(name);
            // GUI.Button keeps IMGUI keyboard, pointer capture and disabled semantics; reviewed art supplies its surface.
            var focused = GUI.GetNameOfFocusedControl() == name;
            var hover = rect.Contains(Event.current.mousePosition);
            var pressed = hover && GUIUtility.hotControl != 0;
            art.Panel(rect, card ? "ui_card_9slice_24" : "ui_button_9slice_16", !GUI.enabled ? 3 : pressed ? 2 : focused || hover ? 1 : 0);
            var clear = card ? clearChoice : clearButton;
            clear.normal.textColor = GUI.enabled ? TextColor : new Color(.66f,.61f,.71f);
            var content = rect; if (pressed) content.y += 2;
            // The button's hit rect never moves when pressed; only its content does.
            var clicked = GUI.Button(rect, GUIContent.none, clear);
            if(card)
            {
                var rows=text.Split('\n');
                cardHeading.normal.textColor=clear.normal.textColor;cardEffect.normal.textColor=clear.normal.textColor;
                cardEffect.wordWrap = drawingMobile;
                GUI.Label(new Rect(content.x+36,content.y+14,content.width-52,32),rows[0],cardHeading);
                GUI.Label(new Rect(content.x+36,content.y+46,content.width-52,drawingMobile ? 52 : 26),rows[1],cardEffect);
            }
            else GUI.Label(content,text,clear);
            if(card && (focused || hover)) art.Draw("ui_arrow_6x9",new Vector2(rect.x+20,rect.center.y+8),2);
            return clicked;
        }
        void Modal(float width, float height, string heading, string detail)
        {
            var panel = drawingMobile ? AdamMobileLayout.Panel(width, height) : new Rect(width / 2 - 310, height / 2 - 160, 620, 340);
            FillPanel(panel);
            art.Panel(panel);
            GUI.Label(new Rect(panel.x + 10, panel.y + 15, panel.width - 20, 50), heading, title);
            var detailStyle = new GUIStyle(label) { wordWrap = true, fontSize = 20 };
            GUI.Label(new Rect(panel.x + 30, panel.y + 65, panel.width - 60, 70), detail, detailStyle);
        }
        static void FillPanel(Rect panel)
        {
            var previous = GUI.color;
            GUI.color = new Color(.035f, .055f, .065f, 1);
            GUI.DrawTexture(panel, Texture2D.whiteTexture);
            GUI.color = previous;
        }
    }
}
