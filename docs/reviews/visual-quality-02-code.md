# Visual quality 02 독립 코드 리뷰

- 판정: **최종 소스·앱 결합 범위 PASS — 막는 HIGH/MEDIUM 발견 없음. Desktop OS pointer는 NOT_VERIFIED이며 별도 시각·청취 판정은 포함하지 않는다.**
- 기준: `0696c98cf78d8248f08769f95d86c9591fd87bb8` 대비 작업 트리. 최초 2026-11-18 **05:31:19 UTC** 소스 checkpoint 이후 U2 최종 반환의 필요한 delta와 결합 근거를 추가 검토했다.
- U2 `ctx_8fed7935cee0`의 고정 소스·소유 반환 handoff 기준이며, 후속 제품 변경에는 이 판정을 자동 전용하지 않는다.
- 소유 변경은 이 보고서뿐이다. 제품·다른 작성자 파일·Editor·앱·사용자 저장을 수정하거나 실행하지 않았다.

## 기준과 소스 버전

외부 `art-visual-quality-02/RESULT.md`, `tokens.json`, `asset-contract.json`, `art-readiness-reviews/visual-quality-02-review.md`를 읽었다. 독립 아트의 Unity 검증용 반입 PASS를 재사용하며, 후보 제작·Design 캔버스 작업·실제 게임 렌더링의 근거를 구분한다. 기존 `jawbone-feedback-code.md`와 `visual-quality-code.md`의 동일한 손 grip·방향 freeze·배우 left 반전·원 GUI group 교정 및 기존 표시 자원 수명 판단을 재사용하고 이번 delta만 검토했다.

검토한 제품 C# 10개는 Editor의 `AdamVisualQualityArtImporter`와 Runtime의 `AdamArtWorld`, `AdamGame`, `AdamHud`, `AdamVisualQualityArt`, `AdamVisualQualityWorld`, 신규 `AdamPolishContacts`, `AdamPolishFlash`, `AdamStartStage`, `AdamUiMotion`이다. 해당 경로를 정렬해 각 `경로 UTF-8 + NUL + 파일 bytes + NUL`을 이어 계산한 최초 checkpoint SHA256은 `a48c378e3d8ae5c42bbdb9f150acc93a703aea41dce1430237f6509263a7b4e6`이다. 이는 최초 소스 식별이며 최종 버전은 아래 binding을 따른다. 관련 기존 시험 3파일과 신규 UI/World 시험 2파일도 읽었다.

## 소스 판단

| 경계 | 확인 내용 |
| --- | --- |
| 모달 우선순위·명령 | `AdamGame.cs:34`의 StartStageAvailable은 오류·suspend·사망 포즈·미선택 성장·보스 보상/정산·일반 정산·챕터/출애굽기/지원 완료·다음 라운드를 제외한다. DrawGameView의 기존 우선순위와 EnterExodus/EnterCommand 완료 가드는 유지한다. Host와 R09 원자 진입 구현에는 diff가 없다. |
| focus·동시 입력·release | 좌우/A D는 focus를 순환하고 동시에 양방향이면 왼쪽이 우선한다. 최종 소스는 MouseMove/Drag/Down에서만 Hover를 호출해 key/repaint의 mousePosition이 focus를 덮지 않는다. GUI.Button 호출 전 isMouse를 보관하여 pointer click만 SelectSlot에 전달하며, Enter는 기존 Update→EnterCommand→SelectSlot 경로가 담당한다. 숫자·Enter·click의 commandFrame 중복 방지와 신규 World의 이동/touch release 가드는 유지한다. 숫자와 Enter가 함께 들어오면 먼저 처리한 숫자 선택이 유지된다. 시각 진입 동작은 명령을 지연하지 않는다. |
| 660/1100 HUD·pause | HP 값은 실제 CurrentHealth/MaxHealth를 bar 내부 lane에, XP/Required·gold·장비는 현재 aggregate에서 읽는다. HP lane 오른쪽은 각각224/274, timer 왼쪽은264/484로 rect가 겹치지 않는다. XP 값도 bar와 분리된다. pause 설정은 실제 F6–F9 상태를 표시하며 Update에서 기존 즉시 토글을 유지한다. PC 재개 버튼은 설정행 아래로 이동하고 mobile/legacy 재개 rect와 mobile 시작 버튼 분기는 유지한다. 이 rect 판단은 실제 글꼴 ink·터치·모바일 가독성 PASS를 뜻하지 않는다. |
| 표시 motion·optional 없음 | stepped unscaled clock은 dim/위치/표시 gold 등에만 쓰며 Host의 보상·저장 시점을 늦추지 않는다. lowFX에서 진입 offset·pulse·count-up·blink 시간이0이다. Polish02 manifest가 없으면 기존 3bundle만 로드하고 legacy HUD/보스/시작·pause 및 기존 Enter 의미를 사용한다. 신규 backdrop은 정적 좌표 타일이며 World/Host/RNG를 만들지 않는다. |
| 9개 strict 계약·원본 | 새 Polish02 whitelist는 emblem3·공격 형태1·bracket1·접촉/whirl4의 정확한9ID다. manifest SHA와 schema/PPU/bundle·집합, PNG SHA·파일 수, sprite rect/pivot/border/frames 등 기존 검증을 재사용한다. thud는6프레임 중 방향별3프레임만 사용한다. 기존 VisualQuality01의3 manifest 상수와 Art03 원본 계약을 바꾸지 않는다. border는 기존 L,B,R,T 변환 경로를 사용한다. |
| 실제 hit·8방향·과거 neutral | AdamPolishContacts는 실제 BattleImpact의 sequence/target/cause/position/time을 소비하며 스윙·miss가 contact를 만들지 않는다. snapshot과 impact 시각이 같을 때만 상대 좌표의8sector를 선택하고 quarter turn만 적용한다. 과거 batch는 현재 player 좌표로 방향을 추정하지 않고 균형 표현을 사용한다. thud .25/crack .15/page .2초 수명과16 슬롯·종류별 cap이 있고 중복 cursor·Clear를 사용한다. |
| 실제 무기·pause·lowFX | sling 잔상은 현재 살아 있는 projectile과 이전 두 snapshot의 같은 ID 위치를 결합한다. 같은 clock의 redraw/pause는 history를 밀지 않으며 기존 trail cap20과 실제 velocity를 유지한다. whirl은 기존 실제 release .12초 window에만 붙는다. Bible 잔상은 실제 orbit/radius·Core clock의 뒤쪽 각도에 그린다. lowFX는 접촉 장식·whirl·잔상을 끄고 실제 무기·흰 피격 flash·필수 rim/core/arrow와 priority2 경고를 유지한다. clock은 기존 snapshot 기준이며 Clear/World 교체 경로에서 이력과 접촉을 비운다. |
| 자원·판정 불변 | 신규 R01 흰 silhouette는 원본 alpha에서 파생한 texture만 cache/Dispose하고 Resources 원본은 파괴하지 않는다. static overlay의 Release/Reload/quit 파생 객체 해제는 기존 경로를 유지한다. 새 접촉/시작/motion은 managed 표시 상태다. 현재 diff에 Core·Host·catalog·피해/충돌·save 제품 변경은 없으며 실제 impact audio/particle의 기존 소비·mute 경로도 변경하지 않는다. |

## 최종 결합·재사용한 실행 근거

`evidence/visual-quality-02-20261118/related.xml`은 2026-11-18 05:28:39 UTC 실행의 **31/31 Passed, failed/inconclusive/skipped 각0**이다. UI11·Polish World4와 기존 턱뼈/피격 관련16을 포함한다. focus 확인·같은 프레임 중복 방지, 두 크기 rect, optional baseline geometry, lowFX/표시 motion, 실제 impact·과거 방향 거절·pause/clear·같은 projectile ID 이력을 관련 범위에서 뒷받침한다. 이 리뷰에서 시험을 다시 실행하지 않았고 새 전체 suite/원래 변이를 검출했다고 주장하지 않는다.

초기 `compile-invalid-meta.log`의 신규 타입 미인식 exit1과 `build-fixture-assembly-failure.log`의 fixture assembly 탐색 exit1은 보존된 실패다. `meta-correction.json`은 관련 meta GUID 교정 기록이며 현재 XML PASS와 구분한다. 이 실패 로그를 최종 제품 실패나 최종 빌드 성공으로 바꾸어 세지 않는다.

최종 `handoff.md`·`source-app-binding.json`·`files-modified.json`·`art-binding.json`과 필요한 XML/log/fixture 경계를 대조해 최초 앱 결합 PENDING을 해소했다. 인계 목록은42파일·C#15입력이며, 후속 제품 delta는 `AdamGame.cs:337`의 pointer-only Hover와 `:345`의 pointer-only GUI.Button 활성화다. 신규 PlayMode 두 시험과 GUID3의32자리 교정을 추가 확인했다. 해당 AdamGame/AdamPlayTests의 현재 bytes와 QA 사본이 최종 binding SHA에 일치한다. 나머지 소스는 기존 검토와 U2의 qaSourceSame 근거를 재사용했다.

최종 AdamGame SHA는 `25d5045e15697779eb7284e32691174660b161ae6324769ffee89695a2635922`다. QA 앱 `PolishQa.app`의 Adam.Unity.dll은 binding의 `04ed2ce2675b11f1ef98abb833199aa2097959fc2811fd4fe37a8af37155974e`, Core DLL은 `e40985359fe9d367c14f8e19e990e20613e80e6e21b6815ed1a4743ca791eeaf`와 직접 일치했다. Core의 baseline/product 동등·Host/content diff 없음은 반환 근거를 재사용했다. 아트 binding은 승인 manifest와9PNG의 계약·기존 아트 변경 없음에 연결된다. 전체 앱 파일/hash 행렬은 다시 계산하지 않았다.

| 근거 | 판정과 한계 |
| --- | --- |
| `input.xml` | 고정 소스의 실제 IMGUI **3/3 PASS**, skipped/failed0. key/repaint가 focus를 덮지 않음·Enter hold/release 후 RunId 유지, 이동 중인 시작 카드의 즉시 mouse hit, 작은 pause modal의 실제 World 재개를 확인한다. Edit31과 별도 범위다. |
| `focus-original.xml`, `focus-restored.txt` | QA 사본에 원 hover/GUI.Button 구조를 한 번 복원하자 **expected1/actual0 assertion으로1 expected FAIL**했다. byte 원복과 최종 QA/product 일치를 확인했으며, 주 제품 소스를 변이하지 않았다. 원복 뒤 새 시험 실행을 했다는 뜻으로 확대하지 않는다. |
| `fallback.txt` | optional 폴더를 물리적으로 제거했을 때9ID 미노출·원31ID·기존 HUD/World gate 복구를 관측하고 finally 복원했다는 기존 실행 근거다. |
| `native-keyboard-proof.json`, `direct-pointer-driver-failure/capture.log` | OS 키 입력 후 앱 assertion으로 Right→Right→Enter의 sling 선택, F6–F9 상태, Esc pause의 World bytes 동결, Enter 재개를 확인한 뒤 mouse-pause 단계까지 도달했다. 명령 도구 ack만을 성공으로 세지 않는다. A/D·숫자 전부의 native 실행으로 확대하지 않는다. |
| Desktop OS pointer | **NOT_VERIFIED.** foreground/delivery 실패·window_not_focused·Apple Events 거절의 실패 driver 기록을 보존한다. IMGUI pointer PASS와 구분하며 권한 변경·다른 앱 조작으로 우회하지 않았다. |
| 최종 build/capture | `build.log`는 실제 exit0, `restoration.json`의 probe41796/capture41797은 각각 exit0다. 첫 fixture assembly 실패와 focus/driver 교정용 재빌드·실패 capture는 보존되어 있으며 모든 시도가 성공했거나 단일 빌드였다고 세지 않는다. 최종 controlled capture의 skip-native는 별도 partial keyboard proof를 요구하며 pointer PASS를 만들지 않는다. |
| 실제 impact·불변 경계 | fixture는 소유 snapshot의 적 위치·공격 일정·기술HP10000을 준비하고 실제 Core Tick/이동으로 세 무기 contact를 얻는다. 해당 contact 검사에 damage/projectile/impact를 직접 주입하지 않는다. 보상·완료 화면용 보스 직접 Hit/timeout, 사망 화면용 Damage는 별도 기술 준비다. 실제 hit는 jawbone1/sling5/Bible3, contact 표본 합8/25/12이며 고유 impact 수와 다르다. sling 누적 shots7/hits6/live0은 warm-up 포함 적어도 한 발이 hit 없이 제거된 근거로, 특정 ID의 명중/실패 귀속을 주장하지 않는다. Bible cap2는 book ID가 없는 Core 사실에 따른 전역 cap이며 책별 identity를 만들지 않는다. |
| pause/mute·표시와 저장 | fixture의 Core pause 및 save bytes 불변·mute active voice0·OnGUI 전후 World/disk 불변 assertion을 재사용한다. 의도한 `owned visual retry` 예외는 retry 화면 준비이며 최종 capture 완료와 구분한다. 자기 QA 저장 absent→absent·자기 child 모두 종료 복구 기록만 읽었으며 리뷰어는 앱/저장을 직접 조회·조작하지 않았다. |

두 크기 high-health probe는11 glyph group이 lane 내부 x221/x271에서 끝나고 timer x264/x484와 분리됨을 기록한다. OCR은 부분 인식이므로 이 수치와 자동 assertion을 주관적인 글꼴/움직임 가독성 PASS로 전용하지 않는다. **별도 시각·청취 판정은 남으며, 모바일·자연 플레이·성능·음향 취향/latency는 NOT_RUN이다.** 편의 영상/WAV mux를 정밀 동기화 근거로 세지 않는다. 게임 이미지·영상을 조회하거나 어떤 채널에 게시하지 않았으며 텍스트 자료만 검토했다. 새 시험·CLI·Editor·앱·GUI·전체 hash 행렬은 실행하지 않았다.

## 사용량

기존 도구의 자기 `visual-quality-02-code-review` start/end checkpoint로 세션 구간을 기록했다. 기록 시각은05:27:55→05:32:25 UTC이고 provider event 구간은00:33:46→05:32:18 UTC다. 도구의 세션 구간 delta는 input1,812,984/cache1,389,952/output6,731/total1,819,715이며 reasoning1,802는 output에 포함된다. start의 지연된 이전 tail을 포함하므로 이번 리뷰에 정확히 귀속한 요청별 사용량으로 보지 않는다. 실제 청구액·단가·독립 작업 비용과 절감률은 미계산이다.

최종 결합 재검토는 자기 `visual-quality-02-code-final-review` start/end를 별도로 기록했다. 기록 시각05:49:59→05:51:43 UTC, provider event05:32:50→05:51:37 UTC의 세션 구간 delta는 input424,079/cache402,304/output4,818/total428,897이며 reasoning758은 output에 포함된다. 이 구간에도 지연 tail이 포함되며 task별 실청구 귀속·비용·절감률은 계산하지 않았다.
