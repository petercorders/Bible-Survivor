# R13 Unity 독립 코드 리뷰

2026-11-18 KST. **PASS — 검토 범위에서 막는 HIGH/MEDIUM 결함 없음.** 현재 Unity Host/catalog/UI/World diff와 신규 loader/importer·Host/UI/World 시험, `evidence/r13-unity-integration-20261118/handoff.md`·binding/verification-index/XML/trace를 대조했다. Core의 네 번째 receipt·저장 형식·정산 구현은 `docs/reviews/R13-core-code.md`의 독립 PASS를 재사용한다. 코드 리뷰와 독립 화면 가독성 판정은 구분한다.

## 전환·저장·표시 검토

- **R12→R13 일반 진입:** `AdamHost` 변경은 AwaitingR13을 기존 일반 전환 목록에 더한다. 완료 체크포인트 후 R13을 시작하고 실패 시 같은 arena/payload·revision/operation을 고정해 재시도하는 경로를 재사용한다. 신규 Host 정상/실패2개 시험은 원래 완료 R12 디스크 무쓰기 Load, 진입 실패 중 원문/메모리 동결·같은 arena retry, RunId/프로필/세 과거 receipt와 이전 Boss 전체의 CompletedFourthBoss 동등성을 확인한다. R12 보스/선택/완료gold를 Unity가 다시 지급하지 않는다. 기존 R09→R10 candidate→Commit/Load/Resume→게시 코드는 diff상 불변이다.
- **현재 World3/과거 Boss4 분리:** Catalog는 R13 normal/round_settlement·between14를 해당 정의로 복원한다. 설치 목록과 라운드별 속도 검증은 기존 Core canonical 정의에 따라 확장되며 R12 ex9 목록을 재사용하지 않는다. R13 새 World는 clock0/LastHitTime=-1·빈 charge/적탄·ChariotDash null이고, 네 번째 Boss4는 과거 receipt로만 보존된다. 신규 중간 Load 시험은 World/Run/디스크 bytes·pause를 비교한다. R13 boss/R14 combat/ForRound14는 거절한다.
- **25/240 once와 명시 R01:** 일반 성장/마지막 성장의 저장 실패를 나눈2개 Host 시험이 pending/serial/성공Choose 수·고갈25·완료240/null 해금/회복을 확인한다. 오류 중 Tick/재선택/정산은 진행되지 않고 retry/Load 뒤 같은 선택과 재정산을 거절한다. 완료between14에서는 Tick/Enter로 다음 콘텐츠나 새 도전을 자동 시작하지 않으며 명시 Start/사망 재시작만 R01/schema2·프로필 유지로 간다. actual InputSystem Enter 및 IMGUI Continue 클릭 시험도 R12→R13의 무추가지급·네 번째 receipt 이관을 검사한다.
- **UI와 지원 끝:** 미리암/출애굽기4·완료240·R14 대기를 표시하며 전체 출애굽기 완료로 표현하지 않는다. 이전 보스400/고갈100과 성장25 카드는 구분한다. R13 일반 완료에는 R12의 홍해 건넘 문구를 반복하지 않는다. ex10/ex11/ex12 shadow는 계약대로 small/wide/wide이며 미지원 ex13/unknown으로 fallback하지 않는다.
- **아트·Texture2D·해제:** `AdamExodusR04Art`/importer는3PNG12frames, 고정 index/manifest/PNG hash와 rect/pivot/PPU32·point/clamp/no-mip/readable·결정적 sprite ID를 검증한다. loader의 `Texture2D t=s.texture`와 importer의 TextureImporterShape.Texture2D를 유지한다. fps8/8/6·4frame·피트 좌표·right-parent flip·기존 깊이 정렬/lowFX 경로를 사용한다. DrawHit의 UV는 실제 frames를 따르고 파생 흰 mask는 원본 alpha/bytes를 바꾸지 않으며 중복 Prepare에서 재사용된다. Dispose는 파생 mask만 이중 호출 안전하게 해제하고 Resources 원본은 보존한다. `AdamArtWorld.Dispose`도 새 loader를 해제한다. 관련 World3개 시험이 프레임/그림자·원본 불변/mask 해제·lowFX 정렬을 다룬다.

## 재사용한 실제 근거

근거 루트는 `evidence/r13-unity-integration-20261118/`다. 리뷰어는 시험/Editor/앱/CLI/GUI/저장을 실행하지 않았다.

| 근거 | 결과와 실제 범위 |
| --- | --- |
| `verification-index.json`, `related.xml`, `boundary-corrected.xml` 및 명령 receipt | sync/compile/importer exit0. 최초 Edit **83/84 PASS·exit2**는 기존 R11 시험의 LastInstalledRound 기대12에 실제13이 나온 실패다. 해당 기대만 교정한 동일1개 **1/1 PASS·exit0**를 합쳐 **고유84개 PASS**이며 단일84/84 XML이나85개 고유 시험으로 세지 않는다. 신규 Host8/UI8/World3을 포함한다. |
| `play.xml`, `play-command.json` | **13/13 PASS·exit0**. R12→R13 Enter/Continue 및 기존 chapter/suspend/중복입력/설치 끝 동작을 포함한다. 이전 보스 완료 준비는 기술 timeout/직접Hit다. |
| `binding.json`, `fixed-product-source.json` | 최종98개 입력 기록이 서로 일치하고 current/QA 동등을 인계했다. 정상 mac 앱은 `build/R13Qa.app`, bundle `com.biblesurvivor.adam.r02artqa.r1304d367eb8f05`, **1회 build·exit0**다. source/app Core DLL의 기록SHA는 모두 `568a72ec3a7fa4de4da78d7f77034eb70b0efa25d7e5775f15f7ff7e1ffc4e74`이며 canonical catalog와 sprite12개 등록 결합 근거를 재사용한다. 전체 hash 행렬을 재계산하지 않았다. |
| `capture/continuous-trace.txt` | 원래 자기 R12 완료 seed의 장비/serial을 바꾸지 않고 무쓰기 Load→일반 entry 실패/같은 retry→ex10/11/12 실제 추적3초·World3 pause/disk Load→동일 World lowFX를 확인한다. 일반/완료 각각 후보1·pending1·실제Choose1로25를 지급하고 Core240/null 해금/회복/between14를 확인한다. gold3586→3714→3746→4020은 기술 처치gold도 포함하므로 단계 차액 전부를 성장gold로 세지 않는다. 새 process Run/Profile/네 receipt/forms 정확 Load·중복240/자동R14 없음·명시R01/schema2를 기록한다. |
| `restoration.json` | probe/capture/reload PID61055/61075/61090 각exit0, 정확 자기 realpath/no-Host probe·writer 부재·자기 main/bak/lock 원래 absent 복구 성공을 기록한다. 보호 사용자 저장은 불조회다. |
| 기존 Core 독립 리뷰 | Core197/관련6·R12관련10·R10~11관련10·원래 receipt 목적지 변이와 CLI1059/1080kills 근거를 재사용한다. 앱에서1080 전처치나 전체13라운드 연속 완주를 확인했다고 세지 않는다. |
| 리뷰어 `git diff --check` | exit0. 새 suite/CLI/변이/빌드/GUI 실행은 없다. |

대표 normal/lowFX/완료3장의 trace/binding 연결과 작성자 실제 열람 보고는 확인했다. 리뷰어가 화면을 새로 열거나 독립 가독성PASS를 대신하지 않았으며 최종 UI 리뷰·root GC/commitpush/CI는 별도다.

seed 자체의 장비/serial이 불변이어도 상속된 HP10000/장비8/진화3/6슬롯과 timeout/directkill/drop 폐기/cycle1000은 기술 조건이다. 실제 추적3초를 자연1080kill/180초 완주·밸런스 실측으로 확대하지 않는다. native/모바일/기기·성능 및 앱 R01~R13 연속 완주는 NOT_RUN/UNVALIDATED다.

작성물은 이 문서뿐이며 제품/타문서/다른 작성자 파일·저장을 편집·원복하지 않았다. 새 worker/Claude/Astra·commit/push도 없다. 자기 usage `r13-unity-code-review` start/end를 기존 도구로 기록한다. 지연된 공급자 event를 포함할 수 있는 세션 구간이며 실제 청구액·전용 비용·절감률은 미계산이다.
