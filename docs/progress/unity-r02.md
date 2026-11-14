# R02 Unity 기술 연결

2026-11-14. 기준 `4971277bc97c59b189c4be015b7c1701518615e6`. 기술 구현·구현자 검증 checkpoint이며 독립 PC 코드/UI 리뷰·GC·커밋·원격 CI는 coordinator 후속 게이트다. P1의 native active 전투·입자·성장 미검증을 이번 작업의 PASS로 이월하지 않는다.

`AdamCatalog`가 기존 원본의 R01/R02를 조회하고, Host가 normal1/2와 between2/3를 올바른 정의로 복원한다. R01 완료의 남은 선택을 저장한 다음 새 R02 세션/월드를 한 번 만들고 진입 상태를 저장한다. 실패하면 같은 세션·월드·payload·operation을 동결해 재시도하며, 성공 전 전투는 진행하지 않는다. 기존 RunId·장비·도전 성장·영구 스냅샷·골드·XP/remainder를 유지하고 완료 보상/회복은 기존 Core가 담당한다. 새 월드에서는 위치·시간·적·투사체·드롭·피격 시계·표현 이벤트를 초기화하고, 누른 이동 입력은 해제 후 다시 눌러야 적용한다.

완료 대기에서 시작 버튼으로 진행을 덮어쓰지 못한다. 작은 창과 큰 창의 계속 버튼은 R02로 이어지며 제목은 가인으로 바뀐다. R02 사망 후 새 도전은 R01이다. R02 완료는 between3에 저장하고 `노아 라운드 준비 중 (R03 미구현)`을 표시한다. 그때의 명시 새 도전만 성장을 초기화한다. wilderness_voice/lightning 해금 기록을 새 무기의 공격 구현으로 표시하지 않는다.

Core 소스/DLL/provenance·catalog/schema·원본 Art03/폰트와 기존 CI/checker/hook은 변경하지 않는다. 사용자의 iOS/Simulator/저장과 출처 미확인 옛 QA 프로세스·저장은 조회·조작하지 않는다. 일반 PC 저장에는 fixture를 쓰지 않는다.

## 아트 경계

현재 b1/b2/b3는 명시 개발용 마름모/빗금 사각형/십자 표식이다. b몹을 기존 a몹으로 대체하지 않고 모르는 ID는 거절한다. b1에는 기존 작은 그림자, b2/b3에는 넓은 그림자를 쓴다. 배경은 기존 공통 지형이다. 최종 R02 아트의 임포트/시각 수용은 **NOT_RUN**이다.

Design 담당의 sidecar 초안은 독립 QA에서 계약 검사 결함이 발견되어 승격 보류 중이다. 전달된 후속 계약은 32px 4프레임·128×32 시트, pivot(16,1) 하단 원점, PPU32/Point/no mip/무압축, b1/b2/b3 각각8/8/6fps loop, 오른쪽 원본/부모 flipX, b1 ground=false·작은 그림자, b2/b3 ground=true·넓은 그림자다. 후보 pivot(16,4)는 사용하지 않는다. stable ID 경계만 준비했으며 이 시트들은 로드하지 않는다. Design 캔버스 사용과 스크립트 PNG 생성은 서로 다른 제작 단계이며, 후자를 Design 제품 내보내기로 기록하지 않는다.

## 실행 근거

새 ignored 근거 루트는 `evidence/unity-r02-20261114/`다. 실제 명령/exit/XML·실패 시도·앱/소스 SHA·격리 경로와 프로세스 상세는 이 경계에 보존한다. 공개 문서에는 개인 경로·PID·창ID·저장 식별자를 넣지 않는다. 앱 내부 저장은 `Application.persistentDataPath` 아래 전용 QA identity를 사용한다.

- 시작 baseline: 새 실행 Core89/기존18변이, EditMode54/PlayMode20 PASS.
- Host 시험: 정의/예산·원본 수치·pending 선택·carry/reset·완료/진입 저장 실패·같은 operation 재시도·결과 불명·normal/between/legacy/World 누락/fingerprint·R03 대기·R02 사망/새 도전. 결과 불명은 시험용 beforeReplace callback에서 staged candidate를 primary로 복사하고 예외를 발생시킨 것으로, 실제 OS 교체 후 callback이 아니다.
- 세 무기 명중/처치/경험 회수 fixture는 적을 부상 HP1·접촉 위치로 배치한다. 전체 체력 밸런스·자연 수동 레벨업의 증거로 사용하지 않는다. canonical 수치 비교는 별도 시험이다.
- 실제 PlayMode InputSystem/IMGUI: 660×440·1100×720 계속 클릭, 이동키 held→전환→release/repress, 중단/복귀, R02 접촉 사망 중 digit 차단→clip 후 R01, 살아 있는 R01 FX/오디오/입자/cursor가 R02에서 초기화됨.
- 별도 macOS Development 앱의 무저장 첫 씬 probe가 실제 identity/저장 realpath를 확인한 뒤에만 Host를 실행한다. 자동 고정 스텝 봇, 강제 완료 fixture, staged 좌표/화면, 실제 새 프로세스 Load는 각각 구분한다.

첫 앱 시도는 화면 fixture가 세션만 앞당기고 옛 월드 시계를 checkpoint해 Core 검증에 거절되어 exit2였다. 두 번째 앱은 재사용 QA 씬에 자체 캡처 driver가2개 누적돼 같은 시험 저장에서 stale revision으로 exit2였다. 두 실패를 보존하고 동일 시계의 세션/월드/드롭 fixture와 새 QA 씬을 사용했다. 빌드 헬퍼는 기존 driver가 있으면 거절한다. 제품 저장 검증을 우회하거나 Core 규칙을 낮추지 않았다.

모바일/실물·성능·자연 수동 3분 플레이·최종 R02 아트는 NOT_RUN이다. 자동 봇의 생존 완료는 타격감/밸런스 수용과 구분한다. 후속 R03 보스·진화·새 무기 구현은 이번 범위 밖이다.


## 최종 구현자 결과

| 검사 | 관측 결과 |
| --- | --- |
| 원본 baseline | Core89·기존18변이 exit0, 시작54Edit/20Play exit0 |
| 최종 엔진 | compile exit0, EditMode66 exit0, PlayMode26 exit0·실패/skip0 |
| PlayMode 결속 | canonical 최종25개 뒤 R02 표현 on/off 정확 동등 시험1개 추가, 동일 Runtime의 격리 QA에서 전체26개 재실행 PASS. 시험 어셈블리는 앱에 포함되지 않음 |
| 실제 macOS 앱 | 새 출력 Development build exit0, 첫 무저장 probe0, 자동 캡처0, 별도 새 프로세스 Load0 |
| 저장 | 실제 Load의 Run/Profile/World 정확 일치·반복 Commit, 중단 시간 정지/복귀 명시 재개·R03 대기 |
| 변이 | R01 전용 복원·선택 조건 제거·old World·새 retry operation·옛 완료 UI 각각 실제 assertion FAIL(exit2) → 원복 PASS(exit0). 최종 Game source에서 UI 변이 재확인 |
| 표현 동등 | 실제 R02 명중을 포함한 on/off 실행의 Run/Profile/World 직렬화 동일. 저장 operation ID가 다른 전체 파일 bytes 동일을 주장하지 않음 |
| 보존 | 보호 원본222개 해시 동일, PNG meta79개 overridden0 자동 항목·빈 아이콘만 분류 후 원본 byte 복원, PC 저장6개 존재/해시 보존, 자체 새 QA3 원복 |
| 정적/동기화 | local/portable checker·diff whitespace 모두 exit0. Core/DLL/catalog/schema/원본 아트·폰트·기존 GUID/설정 불변 |
| 네이티브 입력 | **NOT_RUN**: 정확 자체 PID/창에 background synthetic 키·클릭을 전달했으나 world time 변화0. 실제 active 입력 수용으로 합산하지 않음 |

최종 고정 스텝 봇의 **R02만** 집계하면 다음과 같다. 세 봇 모두 R01부터 연속 실행했으며 canonical 수치·1/60 tick·최근접 보석/결정적 원형 이동과 자동 선택을 사용했다. 처치/체력/시간을 주입하지 않았다. 이는 자연 수동 플레이나 밸런스 수용이 아니다.

| 시작 무기 | R02 명중/처치/회수 | R02 시간·종료 |
| --- | --- | --- |
| 회전 성경 | 2 / 0 / 0 | 180초 시간 완료 |
| 삼손의 턱뼈 | 2 / 0 / 0 | 180초 시간 완료 |
| 다윗의 물맷돌 | 521 / 360 / 341 | 약174.883초 전부 처치 완료 |

최종 소스 manifest(331파일) SHA256은 `34d208d9fe6c598fd540d10c38758accc8f30a580fc735eb77a307d2f5789202`, 앱 file-manifest SHA256은 `82c2c38ed7fd697199ea591f6cb58349027e305804d4643827c1247b5bf45a95`다. 최종 index는 ignored 근거의 `binding-final.json`, 상세 소스는 `final-source-v2-sha.json`, 앱은 `final-app-sha.json`이다. 기존 `final-source-sha.json`은 마지막 시험1개 추가 전 경계로 보존한다. 마지막 변경은 시험뿐이고 앱의 Runtime/Editor/QA fixture는 현재 소스와 일치하며 앱에 시험 DLL이 없음을 확인했다.

660×440/1100×720 기술 화면과 작은 창의 R03 대기·표식/안내를 내부 캡처로 확인했다. 실제 네이티브 재개 입력 수용은 미확인이다. native quit의 screenshot:null을 처리하지 못한 QA parser 예외 후 자기 자식에만 종료 fallback을 요청했고 실제 exit0·같은 새 identity writer 부재를 확인했다. 이를 정상 네이티브 quit 수용 시험으로 합산하지 않는다. 재실행/restore-window 복구 루프는 하지 않았다.

자동 캡처/Load/native의 자체 자식은 모두 종료됐고 Editor 명령도 종료됐다. 제품·Editor·Player GUI 소유를 coordinator에 반환한다. 정확한 새 앱 경로/identity·PID/창·저장·재실행 절차는 ignored `private-handoff.md`와 `final2/`에만 있다. root의 동일 코드/PC UI 독립 리뷰·최종 아트 검수/승격·GC·commit/push·해당 SHA CI가 남는다. R03는 저장된 대기 상태이며 구현하지 않았다.

## 독립 기술 리뷰·checkpoint

[독립 코드 리뷰](../reviews/unity-r02-code.md)는331소스/334앱·보호222/QA27파일과66Edit/26Play·원래구조5변이·새 프로세스 Run/Profile/World 복구를 직접 대조해 해당 범위 PASS, 추가 HIGH/MEDIUM 없음으로 마쳤다. [독립 PC UI 검토](../reviews/unity-r02-ui.md)는 실제 전체 DrawGameView 캡처9장과자동 입력/저장 근거를 직접 읽어 완료/계속·가인HUD·개발표식/모달·R03대기 안내의 기술checkpoint를 수용했다.

UI 검토자는 native 앱/저장을 다시 실행·조작하지 않았다. 캡처셋에 없는 R01 완료 전 미처리 성장 카드의 시각 관측, 최종 R02 아트·혼잡 시각·최신 native active 조작·자연 수동/밸런스·모바일/성능은 계속 NOT_RUN이다. 봇의 마지막 R03 대기 PNG를 무기 전투 장면으로 바꾸어 부르지 않는다. 이번 기록은 기술 연결 checkpoint이며 R02 최종 시각/전체 플레이 수용 완료가 아니다. 프로젝트 GC는 `../retrospectives/R02-unity.md`에 기록했다.
