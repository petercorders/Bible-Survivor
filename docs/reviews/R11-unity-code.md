# R11 Unity 독립 코드 리뷰

2026-11-18 KST. **PASS — 검토 범위에서 막는 HIGH/MEDIUM 결함 없음.** `docs/harness/r11-implementation-01.md`, `docs/progress/R11-unity.md`, `evidence/r11-unity-integration-20261118/handoff.md`·`binding.json`·`verification-index.json` 및 현재 Unity diff/신규 파일을 검토했다. Core 독립 PASS181·원구조 정산 변이·대표 좌표 CLI는 `docs/reviews/R11-core-code.md`의 근거를 재사용한다. 이 문서는 코드 리뷰이며 화면 가독성 판정은 별도 UI 리뷰다.

## 전환·저장·정산

- `AdamHost.cs:36`의 AwaitingR11을 일반 라운드 대기에 연결하고 `AdamHost.cs:139`는 해당 라운드의 SpeedsFor로 새 World를 만든다. 완료 체크포인트 저장 후 진입하며, 실패하면 Error/Paused와 고정 operation/revision/payload를 유지한다. Tick·Choose·중복 진입은 오류 중 차단되고 RetrySave는 같은 arena를 저장한다. 명시 Reload는 마지막 디스크 상태로 되돌리며 Paused로 시작한다. 새 R11 entry 시험은 arena 동일성·정지 상태 JSON·R10 180 무재지급·RunId 보존을 확인한다.
- 기존 R09→R10 `TryEnterExodus`/candidate RetrySave의 후보 생성→Commit→Load→Resume/World 복원→게시 경로(`AdamHost.cs:66`, `:181`)는 변경되지 않았다. 일반 R11 진입의 선게시 후 실패 정지 계약과 이 챕터 진입의 원자적 게시 계약을 구분했다. R11 대기에서는 CanEnterExodus/TryEnterExodus를 사용할 수 없다.
- `AdamR11HostTests`는 세 receipt·진화·RunId 유지, 중간 World disk Load, 마지막 성장 선택의 저장 실패/동일 retry, 고갈25의 pending/serial별 실제 선택, 완료200/null 해금/최대HP회복 once, 완료 후 Load 무쓰기·재정산 거절을 검사한다. Host/UI가 별도 골드를 지급하지 않고 Core를 호출한다. 완료between12는 설치 끝으로 남고 Enter/Tick으로 R12나 새 도전을 자동 시작하지 않는다. 명시 새 도전·사망 재시작은 R01/schema2와 프로필 유지다.
- `AdamGame.cs:107`, `:262`의 Enter/Continue는 기존 pause·suspend·오류 가드를 따른다. World 교체 시 `ReleaseOnWorldChange`가 이동/터치 입력을 해제한다. suspend 복귀에는 명시 재개가 필요하며, 오류 화면의 같은 저장 재시도/마지막 저장 Reload 선택을 유지한다. 기존 pause 및 입력 캐시 검사는 관련 Play 근거를 재사용했다.

## 설치 범위·표시·월드 자원

- `AdamCatalog.cs:25` 이후는 R10/R11을 연속 설치하고 각 라운드의 몬스터·속도·fingerprint를 검증한다. 속도 목록은 각각 ex1~3과 ex4~6의3개로 분리되며 R11 entry/Reload/Restore는 R11 목록을 사용한다. normal/round_settlement는 현재 라운드, between12는 R11 정의로 복원한다. ForRound12와 R12 normal checkpoint는 거절한다. R10 fingerprint 고정 시험도 유지한다.
- `AdamRoundPresentation`/`AdamGame`은 요게벳·출애굽기2·R12 대기를 표시하며 창세기 완료와 설치 끝을 구분한다. ex4/ex5/ex6의 그림자는 wide/small/wide이고 미지원 ex7에 fallback하지 않는다. 기존 고갈25 카드와 보스100 카드는 구분한다.
- `AdamExodusR02Art`는 고정 index/manifest와3종×4frames, rect/pivot/PPU/point/clamp/no-mip/readable 계약을 검증한다. importer는 PNG 승인 hash와 결정적 Sprite GUID/name/rect를 설정하고 빌드 전 검증한다. 적당 fps8/8/10과 right-parent flip은 기존 Draw 경로를 사용한다.
- `AdamArtWorld.cs:265`의 새 분기는 기존 hit reaction/피트 깊이 정렬 경로 안에서 R11 loader를 재사용한다. hit mask는 원본을 바꾸지 않은 흰 RGB/동일 alpha 파생 데이터이고 중복 Prepare에서 재생성하지 않는다. World 교체 때 기존 효과·피격·충격 cursor 캐시를 Clear하며, Dispose는 새 loader의 runtime mask도 해제한다. 신규 World3개 시험이 frame/그림자·alpha/source 불변/이중 Dispose·lowFX 깊이 정렬을 다룬다. 소스 texture는 Resources 소유로 남는다.

## 기존 실행 근거 대조

근거 루트는 `evidence/r11-unity-integration-20261118/`다. 리뷰어는 게임·Editor·시험·빌드·GUI·사용자 저장을 실행하거나 조회하지 않았으며 아래 결과는 작성자의 명령 receipt/XML/trace를 읽어 확인했다.

| 근거 | 결과와 범위 |
| --- | --- |
| `verification-index.json`, `related.xml` | sync1회·compile/importer exit0, 관련 Edit **48/48 PASS**. R11 Host5/UI8/World3 및 기존 R09/R10·depth/hitflash를 포함한다. 경계 기대만 바꾼 나머지 과거 Host 시험은 컴파일 범위이며 실행 PASS로 늘리지 않는다. |
| `play.xml`, `input-boundary.xml` | 최초 Play **10/10**, 보완 **2/2**, 명령 exit0. XML fullname 집합을 대조한 결과 중복1·합집합 **11개**다. 총12회 실행을12개 고유 시험으로 세지 않는다. |
| QA 사본과 현재 `AdamRoundPlayTests.cs` diff, `post-build-test-only.json` | 기존 Enter 시험의 R10→R11 Host 직접 호출을 실제 InputSystem Enter로 바꾸고, 실제 IMGUI Continue 클릭 시험1개를 추가했다. 이 두 케이스만 보완 실행했다. 변경은 TestAssemblies 소속 PlayMode 시험 파일뿐이며 제품 변경/Player 재빌드를 요구하지 않는다. 원래87입력 기록은 보존됐다. |
| `binding.json`, `fixed-product-source.json`, `build-command.json` | 정상 Development mac 앱 `build/R11Qa.app` **1회 빌드/exit0**. 작성자가 기록한 고정87입력·QA 사본·앱 DLL 연결을 재사용했다. Core DLL SHA256 `78947c476da41402f11b3f4b9b2edd82fac131b0dc9f19123b557d4ee67dc731`; provenance schema2와 게임 저장 root3은 별개다. 새 hash 행렬은 돌리지 않았다. |
| `capture/continuous-trace.txt`, `restoration.json` | R10 완료 무쓰기 Load→일반 entry 실패/같은 retry→ex4/5/6 실제 추적3초·World3 pause/disk Load→동일 World lowFX→일반/정산 각각 pending1/Choose1의25→Core200/회복→새 process Run/Profile·세 receipt/forms 정확 Load→명시 R01. probe/capture/reload child PID23229/23354/23684 각각 exit0, 자기3파일 absent 복구 기록을 확인했다. |
| `build/registration.json`, static 검사 receipt | 재import12개 sprite의 등록 동등, project/portable exit0 근거를 재사용한다. 리뷰어 `git diff --check`도 exit0이다. |

대표 capture3장(660 normal/같은 World lowFX/1100 R12대기)의 존재와 trace 연결은 확인했다. 이번 코드 리뷰에서 이미지를 열람하거나 가독성을 PASS로 판정하지 않았다.

## 변이 도구 후속·한계

root 소유 `tools/check_mutations.py:19` 변경은 `completion_resume_refused`의 old/new literal 상한을 함께10→11로 옮겼다. 현재 Resume anchor(`AdamSession.cs:92`)와 일치하며 mutant는 여전히 `run.Stage != "normal"`을 주입해 완료 체크포인트 Resume를 거절한다. 원래 회귀 의미를 유지하는 최소 동기화다. 선택 변이1회 실행은 root 후속 검증이며 이 리뷰에서는 실행 PASS를 주장하지 않는다.

앱 QA는 이전 자기 R10 evidence seed·HP10000/장비8/진화3/보조6슬롯·timeout/directkill/drop 폐기/weapon cycle1000 기술 fixture다. 실제 앱 추적3초와 Core CLI960kills를 구분한다. 자연960kill/180초 완주·앱 R01~R11 연속 완주·native·모바일/실기기·성능은 **NOT_RUN/UNVALIDATED**다. 최종 UI 가독성 리뷰·root 선택 변이 검증·GC/commitpush/CI는 별도 후속 범위다.

작성 산출물은 이 문서뿐이며 제품/Core/Unity/저장/외부 아트/R12 기획은 편집하지 않았다. 새 worker/Claude/Astra·전체 suite/변이·앱 재실행·commit/push는 없다. 자기 세션 usage `r11-unity-code-review` start/end를 기존 도구로 기록하며, 공급자 event 지연 가능성이 있어 전용 단계 사용량/실비/절감률을 추정하지 않는다.
