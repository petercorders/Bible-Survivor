# Visual quality 독립 코드 리뷰

- 검토일: 2026-11-18 KST
- 현재 판정: **최종 소스·앱 결합 범위 PASS — 요청 범위에서 막는 HIGH/MEDIUM 발견 없음.**
- 최종 handoff/binding·기술 trace·기존 native 입력 근거 결합 확인 완료. 별도 시각·청취 판정과 자연 플레이·모바일·성능은 이 코드 PASS에 포함하지 않는다.
- 소유 변경: 이 보고서만. 제품·타인 QA·Editor·앱·사용자 저장을 수정하거나 실행하지 않았다.

## 현재 검토 범위

`docs/harness/visual-quality-01.md`, U2 `preliminary.md`와 `source-files.json`, 현재 overlay/HUD/world/세 무기 효과 diff 및 관련 시험을 읽었다. 기존 `jawbone-feedback-code.md`의 손 grip·방향 freeze·배우 left 반전·원 GUI group 실패/복원 근거는 같은 소스에 한해 재사용한다. 그때의 Bible/sling PCM 불변 결론은 이번 새 사용자 승인으로 변경된 세 무기 음향에 전용하지 않는다.

최초에는 통합 앱 빌드 중의 소스 checkpoint를 검토했고, 최종 소유 반환 후 handoff/source-app-binding/art-binding/files-modified와 필요한 delta만 추가 검토했다. U2의 Editor 단독 소유를 유지하며 리뷰어는 새 suite·CLI·Editor·앱·GUI·전체 해시/픽셀 행렬을 실행하지 않았다. 실제 Fable 제작 출처·디자인 품질 판정은 독립 아트/시각 근거를 따른다.

## 소스 판단

| 경계 | 확인 내용 |
| --- | --- |
| 31ID·3manifest·Resources | UI15/World12/Effects4 ID whitelist와 manifest SHA를 고정하며 schema/version/bundle/PPU32/pivotSpace/borderOrder를 검사한다. Resources manifest의 세 SHA가 loader 상수와 일치한다. importer는 해당 PNG의 manifest SHA와 파일 집합·sprite 이름/rect/pivot/border/PPU·Point/Clamp·무 mipmap을 검증하고 stable spriteID/name-fileID를 등록한다. 런타임은 검증한 클립과 Sprite를 static API로 제공하고 Art03→Bible→Jawbone 뒤 overlay를 적용한다. |
| 원본·alpha·수명 | 기존79 Art03 index와 원본 PNG를 덮어쓰지 않는다. 기존 ID의 dimensions/frames/fps/loop/pivot/border/attach 계약을 보존하고 overlay별 alphaValues를 분리한다. World shadow0/51/77·canopy0/51, trail0/46/76은 승인된 새 manifest의 계약이며 원본 alpha 검사를 느슨하게 만든 결과가 아니다. 원본 시험은 AssetDatabase의 원본 Sprite를 직접 검사한다. 최종 Linear 표시 경로는 별도 소유 float texture/sprite를 만들고 Release/Reload/quit에서 파생 객체만 해제한다. 상세 수량·색 한계는 최종 delta 절에 기록한다. |
| static API·9slice | GetClip/TryGetClip/Sprite가 같은 overlay clip/sprite를 제공한다. manifest L,B,R,T를 importer Vector4에 그대로 넣고 GUI source row는 top-left 순서로 변환한다. keycap3,4,3,3은 scale2에서 GUI margin6,6,6,8이다. tiling/cropped edge는 각 sprite strip의 rect를 기준으로 UV를 계산한다. |
| 실제 HUD | HP/maxHP·XP/Required(level)·레벨·gold·장비/진화는 현재 Session/State에서 읽는다. 일반 timer는3:00까지, boss는 BossElapsed와 별도 역할을 표시해3:06/3:00 모순을 피한다. 실제 inventory의 슬롯/레벨을 표시하며 예시 디자인 금액을 게임 상태로 쓰지 않는다. 좁은 화면의 문자열·실제 아이콘 가독성은 최종 화면 검증이 남는다. |
| 모달·focus·pointer·가드 | ArtButton은 기존 hit rect와 GUI.Button·control name을 유지하며 표면만 바꾼다. disabled/pressed/focus/hover를 구분하고 pressed에서 content만2px 이동한다. 카드 수·선택 ID·reward ID와 Host 호출은 실제 후보를 사용한다. SelectSlot/EnterCommand·commandFrame·Error/suspend·ExodusComplete/ChapterComplete 가드는 변경하지 않았고 Host의 R09→R10 원자 후보/commit 경로도 변경하지 않았다. 실제 최종 앱 pointer/focus·저장 retry를 소스만으로 실행 PASS 처리하지 않는다. |
| 월드·경고 계층 | Ground는 표시용 정수 hash로 grass/meadow를 고르며 Core RNG를 사용하지 않는다. canopy는 네 모서리 X/Y flip이며 중앙60%를 침범하지 않고 lowFX에서는 숨긴다. 그림자와 optional smoke/chips는 XP·배우·무기보다 아래다. 실제 Core 기하의 기존 dark rim/white/arrow·실탄은 마지막 world 계층/HUD 아래에 유지하고 red9px band만 선택 feedback이다. |
| 실제 사건·표시 밀도 | death smoke는 배우가 사라졌다는 추론을 제거하고 실제 killed FrameImpact에서만 생성한다. optional contact star는 최대3을 확인한 뒤 추가하고, 실제 impact 소비/반응 기록을 생략하지 않는다. player damage 표시와 필수 경고는 별도 경로다. |
| 턱뼈 delta | 반 픽셀 grip·현재 손 follow·burst 방향 freeze·Core clock·여섯 배우 레이어 left 반전을 유지한다. 새 trail/edge는 기존4frame attach를 읽고 현재 손에서22px head 반경으로 맞춘다. attach를 held sprite 중심으로 사용하거나 새 hit를 만들지 않는다. trail은 lowFX에서 숨기며 held club/edge는 유지한다. |
| 물맷돌·성경 delta | 새 observed projectile ID와 실제 velocity만 post-release rope를 시작한다. 첫 관찰의 기존 탄은 발사로 오인하지 않고 NextCycle로 가짜 발사를 만들지 않는다. 실탄의 현재 좌표와 arena 창 pivot을 사용하며 trail은 속도에 비례하고 기본20px×scale로 제한한다. 실제 sling impact 조각 방향은 최근/현재 탄 velocity에서 읽고, 성경은 접촉 축의 tangent를 사용한다. 표현 history는 snapshot을 변경하지 않으며 World generation Clear에서 초기화한다. |
| audio·particle·cleanup | 세 무기 PCM은 이번 승인으로 의도적으로 새로 조정됐다. 실제 FrameImpacts/cursor·voice8cap/35ms throttle·mute/volume/pause·비활성 stop을 재사용하며 빗나감에는 명중음/조각을 만들지 않는다. jawbone6/10·sling4·bible4의 방향 조각은 batch18cap와 기존 emitter16×16/256pool을 사용한다. lowFX clear/paused simulation·generation 초기화·RT/material/root/event/camera mask Dispose 경로는 유지한다. 음향 선호·latency는 실제 청취 전 미검증이다. |

Core 판정·피해·라운드 데이터·저장·DLL·Host·원본 Art03 경로에는 이번 표시 작업의 diff가 없다. 표시 helper는 snapshot/history를 읽고 일시적인 표현 상태만 저장한다. 최종 app DLL/source 결합은 아래 추가 검토에서 확인했다.

## 재사용한 실행 근거

`evidence/visual-quality-20261118/related.xml`은 **36/36 PASS**다. XML 기준 분류는 World5/UI11/Jawbone8/Impact5/HitFlash3/Depth3/Art03원본1이며 실패·skip은 없다. 관련 케이스에서31 overlay ID/API/등록/alpha·keycap 비대칭 border, 실제 projectile birth·기존 탄 재개 무가짜발사·pause/clear·trail cap, window grip·canopy 중앙 범위, 방향 조각·세 PCM bounded/distinct, HUD 배치/clock/fraction·명령 중복/suspend/save-failure 가드를 확인한다. 원본 Art03 검사는 원본 imported assets와 새 overlay를 분리한다.

이 XML의 surface-state 계산 시험은 실제 focus/pointer 입력 시험을 대신하지 않는다. 최종 인계는 아래의 기존 native 입력 관측을 별도로 결합한다. 기존 턱뼈 A/B World/Profile 동등·원 group 실패/복원은 같은 동작의 선행 근거로만 재사용하며 새 HUD/월드/성경/물맷돌 최종 A/B 완료로 세지 않는다. 기존 턱뼈 음향의 다른 무기 PCM 불변 시험은 현재 변경된 Bible/sling에 대한 기대가 아니다.

## 최종 반환 delta·앱 결합 — 확인 완료

추가 소스 범위는 정확히 Runtime `AdamVisualQualityArt`/`AdamArtWorld`/`AdamHud`/`AdamGame`과 World 색 시험이다. 이전 소스 검토를 다시 반복하지 않았다.

- **색·소유 수명:** Linear 모드에서 원본 `GetPixels32`의 raw RGBA를 읽어 RGB에만 `source.linear.linear`를 적용한다. 새 Texture2D는 RGBAFloat·mipmap 없음·명시 linear=true·Point/Clamp이고 alpha는 유지한다. 원 sprite의 rect/pivot/PPU/border/name을 복사하며 원본을 바꾸지 않는다. 고정31 texture/74 sprite의 raw float pixel 수량은 **1,126,496bytes**다. 원본·관리 배열·Sprite/엔진·GPU overhead를 포함한 총 메모리나 성능 실측으로 표현하지 않는다. Release는 소유 목록을 역순 파괴하고 cache를 비우며 Reload는 Release 후 재생성, Application.quitting은 Release를 호출한다. 매 프레임/매 draw에 새 texture를 만들지 않는다.
- **GUI ink와 glyph:** solid Ink0/1/2/4에만 GuiColor의 단일 linear 변환을 적용하고 Fill/FillPanel은 이를 다시 변환하지 않는다. Primary/Secondary/Muted/Gold/Lapis glyph token과 HudText textColor 경로는 별도로 유지해 같은 ink에 변환을 중복 적용하지 않는다. PC 관측의 source93,102,116→13,26,41 및 solid30,34,44 대 목표28,34,45는 근사 보정 근거다. 새 시험은 변환의 수학적 역관계와 alpha/등록을 확인할 뿐 **정확한 색도·모든 색공간·모바일 PASS가 아니다.** RGBAFloat도 엔진/디스플레이 간 색 일치를 보장한다는 뜻으로 세지 않는다.
- **lowFX·경고 순서:** 모든 optional red band를 한 묶음으로 먼저 그린 뒤 필수 danger/rim/core/arrow·실탄을 그린다. 교차하는 선택 band가 다른 필수 경고 위에 덮이는 순서를 피한다. player-contact effect의 priority2는 FeedbackEnabled=false에서도 표시하고 기존 actor tint/필수 경고를 보존한다. 기하/입력/판정 값은 바꾸지 않는다.

`source-app-binding.json`에는 제품 **115 C# 입력 전부 qaSourceEqual=true**가 기록되어 있다. 리뷰는 그 기존 ledger를 재사용하고 위 네 Runtime만 현재/QA/기록 SHA 일치를 직접 확인했다. 최종 앱 `build/VisualQa.app`, bundle `com.biblesurvivor.adam.visualqa.visualf37f399abf3d`의 Assembly-CSharp fingerprint도 기록과 같다. app Core DLL과 원 제품 Core DLL은 실제 byte 동일하고 SHA `e40985359fe9d367c14f8e19e990e20613e80e6e21b6815ed1a4743ca791eeaf`를 유지한다. art-binding과 source-app의 artManifestBinding은 동일한3manifest/31PNG/74frame을 결속한다. 기존 keycap border·원본 아트 불변·정확 public whitelist 결론을 유지한다. 전체115입력이나 PNG 픽셀 행렬을 새로 실행하지 않았다.

검사는 기존 related36/36, color-ui-delta13/13, final-color2/2 PASS를 재사용한다. 후속 두 실행은 이전 UI/색·등록 케이스를 포함하므로 **51개 고유 검사로 합산하지 않는다.** final2는 실제 `LinearGuiPresentationCancelsBothObservedGammaEncodesAndPreservesAlpha`와 overlay 등록/alpha 시험이다.

## 최종 실행 근거의 범위

최종 build-capture-complete exit0와 final probe/Player 두 child exit0, `capture/complete.txt` PASS를 인계 원장으로 확인했다. 기존 첫 동일 입력 경로의 native 숫자3/F6/F7/Escape/Return·성장 카드 pointer·pause/resume pointer와 앱 상태 수신 증거를 재사용한다. 최종은 visSkipNative/prior-pass 확인으로 구동되므로 최종 앱에서 native 입력을 전부 다시 실행했다고 세지 않는다. palette delta에 hit target/입력 predicate 변경이 없고 EnterCommand·완료/R09 원자 진입 경로도 불변이다.

세 무기의 최종 facts는 hit/played 각각 jawbone1/1·sling5/5·bible3/1이다. Bible의 같은 step audio 제한을 hit 수만큼 재생됐다고 부풀리지 않는다. 물맷돌은 실제7shots/5hits/1live로 no-hit 제거 최소1을 기록하며 fixture가 탄이나 damage를 주입·삭제하지 않는다. 적 위치/초기 NextCycle은 대표 접촉을 위한 기술 준비다. R15 timeout/직접 lethal Hit·유효 과거 seed의 실제 보상/정산·retry 오류 주입도 자연 완주가 아니다. fixture의 DrawGameView World/전체 저장 bytes 동일 assertion(프로필 포함), pause freeze·mute0voices를 재사용한다. 이후 R15 준비로 Bible 기술 store를 덮어쓴 사실과 clip facts를 구분한다.

첫 native 성공 뒤 OnDisable 리소스 예외, unavailable probe API, CanStartNew의 정상 거절, Bible 접촉 없는 배치의 실패는 원 fixture/log/복구 기록에 보존됐다. 최종 검증에서 이를 숨긴 성공 실행으로 세지 않으며 Core 판정이나 가짜 hit로 우회하지 않았다. `restoration.json`은 success=true·allStartedChildrenExited=true·자기 canonical SaveStore3파일의 원 absent 복원을 기록한다. 리뷰어는 저장 파일을 열거나 현재 live 앱을 조회하지 않았다.

6 WAV의 비무음/bounded 수치와 실제48kHz stereo engine mix/22.05kHz mono PCM 구분을 확인했다. 고정tick60frame/10fps/6초 무음 영상과 convenience mux는 정확 동기화·latency·realtime 성능 인증이 아니다. **시각 가독성·음향 취향/실제 청취는 별도 판정으로 남는다.** 자연15라운드·정확 색·native/device/mobile·성능 전체 PASS로 확대하지 않는다. 위에 명시된 제한적 native 입력 관측만 재사용했다.

사용자 live PID98774/window244879/bundle 및 해당 저장·보호 QA identity는 조회하거나 조작하지 않았다. 자기 사용량은 기존 도구의 최초 `visual-quality-code-review`와 최종 `visual-quality-code-final-review` start/end로 기록하며 지연된 이전 tail을 포함할 수 있는 세션 구간이다. 실제 청구액·전용 비용·절감률은 미계산이다.
