# R12 Unity 독립 코드 리뷰

2026-11-18 KST. **PASS — 소스 및 최종 앱 결합 검토에서 막는 HIGH/MEDIUM 결함 없음.** 최초에는 `AGENTS.md`, R12 계획·Core 진행/독립 리뷰, `evidence/r12-unity-world-20261118/handoff.md`와 고정 Host/UI/World 소스·신규 importer/loader·관련 시험을 검토했다. 최종 인계 후에는 `docs/progress/R12-unity.md`, `delivery/handoff.md`, parent `binding.json`/`verification-index.json`과 `post-review-ui-layout.diff`만 좁게 대조해 당시 미도착한 앱 결합 검토를 마쳤다. 기존 소스/시험 검토는 반복하지 않았다. 화면 가독성의 독립 판정은 별도 UI 리뷰다.

## 소스 검토

- **동적 catalog/단계 복원:** `AdamCatalog`는 Core R12 규칙에서 boss ID/체력/접촉/고정gold·period/telegraph/dash·intro/offset·지원 threshold/종류/수치를 검증한다. 일반 ex9만 telegraphed_fixed_dash를 허용하고 R10/R11의 속도 집합·fingerprint 검증을 유지한다. ForCheckpoint는 R12 normal/intro/boss/reward/settlement와 완료between13을 R12로 복원하며 R12 round_settlement/R13 전투는 거절한다. Host/World 복원은 해당 라운드 SpeedsFor를 사용한다.
- **진입·receipt·저장 재시도:** `AdamHost`의 변경은 AwaitingR12를 기존 일반 전환에 더하는 범위다. 완료 R11→R12는 이전200을 재지급하지 않고 같은 RunId/세 과거 receipt/진화를 유지한다. 실패한 진입은 같은 arena/payload·operation/revision으로 정지·retry한다. 기존 R09→R10의 후보→Commit→Load→Resume/World 복원→게시 경로는 불변이다. 신규 Host 시험은 정상/실패 entry2개와 단계별 disk replay, 보상/성장 retry, 완료 후 Load 무쓰기/명시 새 도전R01/schema2·프로필 유지, R13 거절을 포함한다.
- **일반/보스 시계 분리:** `AdamChariotWarning.NormalClip`은 World.Time/일반 charge.Until로1.2초의2pose(0/.6)와 dash 시간을 읽는다. `BossClip`은 Core ChariotDashView.BossElapsed/Deadline으로3pose(0/.4/.8)·dash 시간을 읽고 World.Time은 chase 애니메이션에만 사용한다. intro는 frame0이다. 공격 시계·충돌·피해를 Unity에 복제하지 않는다.
- **고정 방향/live origin/실 tuning:** 일반 lane은 기존 AdamChargeLane의 현재 actor 위치·저장 방향·Until·snapshot tuning을 재사용한다. 보스 lane은 Core view의 Origin/Direction/RemainingDistance/BodyRadius+PlayerRadius를 그대로 투영한다. 기본2.34/.85를 그림에 상수로 새기지 않는다. `AdamArtWorld.cs:132`는 매 Update 새 view를 읽고 telegraph/dash에서 고정 방향으로 뒤집으며 수직 방향은 이전 facing을 유지한다. actor는 기존 feet 정렬/실좌표 경로에 그린다. 다른 tuning·pause·Restore·kill의 view 시험이 있다.
- **경고 레이어/lowFX/정리:** 일반 lane은 `AdamArtWorld.cs:423`, 보스 lane은`:444`에서 actor/무기/입자 이후, World 그룹 종료와 HUD 이전에 그린다. FeedbackEnabled/lowFX 조건으로 숨기지 않으며 기존 비점멸 이중선/방향 renderer를 사용한다. R12는 구 slam ring에서 제외된다. source 교체/null/Clear는 chariot view와 관련 반응 캐시를 초기화하고 kill 뒤 Core view null로 경고를 제거한다. 보스 death는4/6초 유한 clip으로 끝나며 영구 잔해를 만들지 않는다. Dispose는 R12 파생 mask와 일반/보스 lane texture cache를 모두 해제한다.
- **9sheet/29frames·짧은 pose:** `AdamExodusR03Art`/importer는 승인 index/manifest 및 PNG별 bytes 계약, PPU32·rect/pivot/name·point/clamp/no-mip/readable·결정적 Sprite ID를 검증한다. Draw/Sprite의 명시 frame clamp는 clip.frames-1이고, mask UV도 실제 frames로 나누므로2/3frame pose가4frame 규칙에 끼워지지 않는다. DrawHit는 선택된 windup frame을 받아 몸체와 맞춘다.9개 mask는 원본 alpha를 보존하는 파생 texture로1회 준비/재사용/Dispose하며 Resources 원본은 지우지 않는다. 아트 독립 PASS는 재사용하고 원본 재생성/픽셀 행렬은 하지 않았다.
- **UI/보상/기존 입력:** 모세/출애굽기3·보스 이름/HP/단계는 선택된 Rules와 저장 Phase를 사용한다.400 고정 보스gold, 후보 고갈100, 성장25, 완료220을 각각 표시하고 지급은 Core에 맡긴다. 기존 freeze/retry·serial 선택·CompletionApplied once 경로를 Host 시험으로 대조했다. 완료는 홍해 건넘/R13 미리암 준비 안내이며 전체 출애굽기 완료나 플레이어 처치가 기적 원인이라는 표현은 없다. Enter/Continue의 R12 일반 진입, suspend·중복입력 가드와 설치 끝 Enter 무동작, 명시 새 도전R01을 유지한다. 660 화면 줄바꿈/버튼·lane 가독성은 최종 UI 리뷰 대상이다.

## 재사용한 실행 근거와 실패 이력

근거 루트 `evidence/r12-unity-integration-20261118/`의 실제 XML/명령 receipt를 읽었다. 리뷰어는 Unity/Editor/시험/CLI/GUI/저장을 실행하지 않았다.

| 근거 | 소스 체크포인트 결과 |
| --- | --- |
| `sync-command.json`, `compile-command.json`, `static-checks.json` | sync1회, compile/importer 및 project/portable 각exit0. 실제 앱 실행 증거로 전용하지 않는다. |
| `related.xml`, `related-command.json` | 최초 **70개 중69 PASS/1 FAIL**, command exit2. 실패는 R12 phase/threshold disk replay fixture의 NullReference이며 원XML을 보존했다. |
| `phase-corrected.xml`/command, `phase-final.xml`/command | 같은1개 fixture의 첫 교정은 World byte 비교 FAIL·exit2, 최종은 **1/1 PASS·exit0**. distant boss/무기 억제로 live view를 격리하고 지원 spawn 후.01 대신.1 tick으로 World를 준비한 시험 교정이다. Runtime/Core 제품 수정으로 우회하지 않았다. 최초69개와 최종 같은1개를 합친 **고유70개 PASS**이며, 재실행까지72개 고유 시험이라고 세지 않는다. |
| `play.xml`, `play-command.json` | 배정 시 진행 중이었으나 검토 중 도착한 결과는 **12/12 PASS·exit0**다. 실제 Enter/Continue 및 기존 입력 경계가 포함되며 R12 완료 준비는 timeout/직접 boss hit 기술 fixture다. |
| `docs/reviews/R12-core-code.md` | 기존 Core191/191·endpoint-only 및 보스 종료cooldown 두 원회귀 변이·한 무기/seed 실제 좌표 CLI와147샘플/별도 phase Load 근거를 재사용한다. |
| root 선택 Resume 변이 | coordinator가 상한11→12 동기화 후 선택1건의 실제exit0 완료를 전달했다. non-normal Resume 거절 주입 의미는 유지된다. 이를18개 전체 mutation 실행 PASS로 확대하지 않는다. |
| 리뷰어 `git diff --check` | exit0. 전체 suite/CLI/변이/hash 행렬은 재실행하지 않았다. |

## 최종 인계 후 한정 보완

- **표시 delta:** 최초/최종 `fixed-product-source*.json`의 기록93개를 비교하면 변경은 `AdamGame.cs`1개이고 다른92개는 같다. 최종 source 입력 기록은 binding의93개와 일치한다. `post-review-ui-layout.diff`와 해당 현재 OnGUI 부분을 대조했다. R12 보스 이름/HP와 phase를16px의두 행으로 나누고, active R12 boss의 forms를 기존 footer controlHint 행에 표시하며 이전 actor 근처 forms 줄을 제외한다. 기존 보스 표시는 유지하고 Core/Host/World·전투/저장/입력 분기는 바꾸지 않는다. 이 표시 변경은 최종 정상 Player 빌드에서 컴파일됐으며 관련 Edit고유70/Play12 근거를 재사용한다.
- **최종 산출물 결합:** `binding.json`은 최종 `delivery/build/R12Qa.app`, bundle `com.biblesurvivor.adam.r02artqa.r12e800f00a5a93`, 고정93개 입력/QA 사본·Assembly-CSharp·Core DLL·provenance/catalog·29개 sprite 등록·trace/3화면을 연결한다. 기록된 source Core DLL과 최종 앱 DLL은 모두 `759b580f2300fddd9f1ad37803c2edbae1320d44d570e84840892ffe1d15db1b`다. canonical catalog 동등성은 인계/기존 binding 검증 근거를 재사용했으며 새 앱 실행·전체 hash 재계산은 하지 않았다.
- **다섯 빌드와 실패 구분:** 정상 mac 빌드는 총5회이며 build 명령은 모두exit0이다. initial/두 번째 final의 **Player exit2**는 각각 잘못된 seed envelope key와 이미 완전 고갈된 seed에서 자동100 대신 카드 화면을 기대한 QA fixture 오류다. 세 번째 final-clean은 기능/Load exit0이나660 HUD 잘림으로 표시FAIL, 네 번째 review는 기능exit0이나 forms/배우 겹침으로 표시FAIL이었다. 다섯 번째 delivery가 최종 작성자PASS다. 앞선 앱/원로그/실패 화면·각 저장 복구 기록을 보존했으며 빌드exit0을 해당 앱 전체PASS와 혼동하지 않는다.
- **최종 trace/저장:** `delivery/capture/continuous-trace.txt`는 R11 완료 무쓰기 Load→R12 명시 재개/200 무재지급→실제 intro2→BossElapsed5.1 예고 및6.1 돌진의 정확 World5/Boss4 disk Load·live origin/고정 방향→지원6/12→치명타 cleanup·400 once를 기록한다. 고정 후보2개 중gold100 선택, 기술pending2의 실제 Choose2(구름4→5 한 번·serial gold25 한 번), 동일 저장 retry 후220/회복·between13을 확인한다. gold2841→3241→3341→3366→3586이며 현재Boss4+세 과거 history를 유지한다. 새 process 완료 Run/Profile 정확 Load와 무중복220·자동R13 없음·명시R01/schema2까지 연결된다.
- **기술 조건/보존:** 이전 자기 R11 seed의HP10000/장비8/진화3/6슬롯에 **cloud5→4를 최초 저장 전에 기술 조정**했고 normal timeout/injury20/boss position9/cycle1000/threshold·lethal Hit/pending 주입을 사용했다. 조정 전 자연 장비를 그대로 유지한 완주 증거로 세지 않는다. `delivery/restoration.json`의 probe/capture/reload PID51307/51324/51333은 각각exit0이며 자기 main/bak/lock의 원래 absent 복구와 writer 부재를 기록한다. 보호 사용자 저장을 리뷰어가 조회하지 않았다.

최종660 lowFX dash·1100 고정 보상·1100 완료/R13대기3장의 결합과 작성자 열람 결과는 확인했으나 리뷰어가 새 화면을 열거나 독립 가독성PASS를 대신하지 않았다. 남은 것은 별도 최종 UI 리뷰 및 root GC/commitpush/CI다. 자연 완주·밸런스·앱 R01~R12 연속 완주·native/모바일/실기기·성능은 NOT_RUN/UNVALIDATED다.

제품·Core·Unity·외부아트·타문서·저장 편집/실행, 타인 변경 원복, 새 worker/Claude/Astra, commit/push는 없으며 작성물은 이 문서뿐이다. 전체 소스/시험/CLI/변이/hash 행렬·앱 실행을 반복하지 않았다. 자기 usage `r12-unity-code-review` 및 최종 보완 `r12-unity-code-review-final` start/end를 기존 도구로 기록하며 지연 event를 포함할 수 있는 세션 구간이다. 실제 청구액/작업 전용 비용/절감률은 미계산이다.
