# R12 Core — 모세와 압제의 전차장

2026-11-18. 승인 계약 `docs/harness/r12-implementation-01.md`에 따라 출애굽기 local3/global12 `exodus_03`와 완료 between13까지 구현했다. 계획 독립 PASS 및 최신 외부 contract `72afdf87e6e29a9c35ab307da5c13fe4277fcb755b29622beac3ffc4de55b999`, manifest `40b92f5efbdf1470a6847b9446a352fc45a3fa05d02da4a40fc1c26a90d27404`를 읽었으며 아트는 수정하지 않았다. 독립 리뷰·Unity 통합·업로드는 coordinator에게 인계한다.

제품 편집 전에 R11 SHA `880ff69583a93f14e75b5c8cb41306986b16fe50`의 `gh run watch 44113757898 --exit-status`와 `gh run watch 44113757645 --exit-status`가 실제 exit0으로 끝났으며 completed/success와 SHA도 확인했다. 로그는 `evidence/r12-core/r11-ci-core.log`, `r11-ci-art.log`다. 대기 중 제품 편집은 하지 않았다.

## 구현과 공개 API

R11 between12에서 `StartNextRound(ExodusContent.Round(12))`로 진입한다. RunId·장비·진화·성장·영구 스냅샷과 R03/R06/R09의 세 receipt를 유지하고 이전 정산을 재지급하지 않는다. R12 총1020/XP4500/cap120/완료220·해금없음, ex7/8/9 수치와 270/340/410 구성은 승인 계획 그대로다. 일반 종료 후 드롭과 남은 성장, 보호 intro2초, 보스, 보스 보상, 남은 성장, 완료 정산 순서를 유지한다. intro에 조기 HP회복은 없다.

일반 ex9는 기존 D3의 정지 예고1.2초·고정 방향·돌진.6초·기본 playerSpeed×1.3·돌진 끝 cooldown6초를 사용한다. `BattleWorld.UsesNormalCharge(round, monster)`가 R04/d3와 R12/ex9만 명시적으로 선택한다. 기존 D3 ID/수치/`ChargeContract`는 유지했다. 일반 돌진 snapshot은 기존 방어 복사 `ChargeStates`의 `NextAt`/`Until`로 읽는다.

보스 `oppression_chariot`는 HP20000/contact20/고정400/period6, ex8 지원60%·30%에서 각6·총12 once다. 보스는 별도 절대 `BossElapsed` 시계로4.8초 정지 예고→6초 돌진→6.6초 추적→10.8초 다음 예고를 실행한다. 예고 시작에 고정한 방향과 겹침 방향(1,0)을 사용한다. 실제 몸체가 이동하며 player와 boss 양쪽 이동의 상대 선분 swept 원 충돌을 기존 contact/armor/공통 무적 경로로 전달한다. 예고선·slam·chain·별도 피해 packet은 없다. 같은 tick player 사망을 먼저 처리하고, 치명 보스 타격은 지원 생성과 돌진/예고를 즉시 정리한다.

추가 API는 `ExodusContent.RoundTwelveContract`, R12를 받는 `Round(int)`/`SpeedsFor(int)`, `BossDefinition.ChariotPolicy`/`ChariotDashDuration`/`ChariotDashSpeed`/`IsChariot`/`DashDuration`/`DashSpeedRatio`, `BossEncounterState.ChariotCharge`, `ChariotChargeState`, `BattleWorld.ChariotDash`/`UsesNormalCharge`, 불변 `ChariotDashView`다. `AdamSession.Boss`는 새 charge까지 방어 복사한다. `ChariotDash`는 live `Origin`, 고정 `Direction`, `Phase`, `BossElapsed`, 절대 `Deadline`, `RemainingDistance`, 실제 `BodyRadius`/`PlayerRadius`/`DangerHalfWidth`를 새 불변 view로 반환한다. 기본 예고 반폭은.85wu이며 player 반경과 속도는 실제 저장 tuning을 따른다. Unity가 별도 공격 시계를 만들 필요가 없다.

root schema3을 유지하고 R12에만 named boss4와 World5를 사용한다. `ChariotCharge`에 phase·단위 방향·절대 deadline, World5에 live 좌표를 저장하고 clock/phase/좌표 ledger를 검증한다. R12 normal/intro/boss/reward/settlement와 between13까지만 허용한다. 기존 root2/World1~4/boss2·3은 유지하고 미래 R13 전투·boss5/World6·알 수 없는 phase/member는 원문과 backup을 보존한다. R10 fingerprint `IunkZmlC50vfCA3mEtdpX5iiLjkDjk2gkFVsEXpZF9A=`, R11 `GLT9uoWhUnPu8fFrWqeQKDYBrDWEiNHqazqM+/7eHFg=`를 고정 검사한다. R09→R10 원자적 candidate 경로는 변경하지 않았다.

고정400, 선택 고갈100, 일반 성장 고갈25, 완료220은 각각 once이며 후보와 남은 pending은 Load로 추가 지급되지 않는다. 완료 시 최대HP회복과 between13/`ContentComplete`를 기록한다. 현재 R12 receipt는 `Boss`, 이전 세 receipt는 기존 필드를 사용하며 네 번째 과거 필드는 추가하지 않았다. 사망 후 새 도전은 영구 프로필을 보존하는 R01/schema2다.

## 검증과 실패 기록

| 명령/검사 | 결과·실제 exit | 증거 |
| --- | --- | --- |
| `bash tools/dotnet.sh run --project tests/BibleSurvivor.Core.Tests -- --r12-only` 최종 관련 검사 | PASS10/10, exit0 | `evidence/r12-core/r12-final-related.log` |
| R12 최초 관련 검사 | FAIL9/10, exit1 | `r12-initial.log` |
| ex9 fixture 준비 수정 과정 | FAIL8/10, 이후9/10, 각exit1 | `r12-related-fixed.log`, `r12-fixtures-fixed.log` |
| ex9 속도 키와 fixture 교정 후 | PASS10/10, exit0 | `r12-corrected.log` |
| 기존 R10/R11 관련 첫 실행 | FAIL9/10, exit1 | `related-initial.log`, `r10-r11-related.log`; R11 설치끝/미지원 다음 라운드 기대를 R13으로 이동 |
| 대표 `--r12-series sling 7` 첫 실행 | FAIL, exit1 | `series-initial.log`; 실제 ex9 이동에서 기존 함수의 d3 속도 키 참조 검출 |
| 같은 CLI 수정 후 | PASS, exit0 | `series-speed-fixed.log` |
| 격리 원회귀 변이2개 | baseline0 / mutant1·1 / restored0 | `evidence/r12-core-mutation/summary.json`와 각 원로그 |
| `python3 tools/check_all.py` 최종1회 | PASS, exit0; 전체191/191 | `evidence/r02-20261117T154050016334Z/checks.json`, `core-tests.xml`; wrapper `evidence/r12-core/final-gate.log` |
| 최종 gate의 대표 CLI | PASS, exit0 | 같은 폴더 `14.log` |
| `git diff --check` | PASS, exit0 | 종료 전 재확인 |

원회귀 변이1은 이전 endpoint-only contact 조건으로 돌아가 player와 boss가 서로 통과할 때 누락되는 원래 구조를 재현했다. `Expected 80, actual 100`으로 실패했다. 변이2는 일반 D3의 돌진 끝 cooldown 시계를 보스에 사용해 다음 공격이12초에서13.8초로 밀리는 구조를 재현했고 `Expected 12, actual 13.799999999999999` assertion과 저장 clock 검증으로 실패했다. 컴파일 실패를 검출 성공으로 세지 않았으며 별도 임시 사본에서만 변이했다.

새10개는 정확 콘텐츠/XP, intro·세 history, ex9 고정방향/종료 cooldown·Load, 보스 절대 시계/정지/거리·pause/선택 blocker/방어 복사, 예고·돌진·추적 disk replay, 양쪽 이동 swept/armor/무적·겹침 방향, 지원6/12·치명 skip·killcleanup·player death 우선, 고정 후보/Load/정산 once, 선택 고갈100·남은 성장25의 중간Load, 미래 원문보호·R01/schema2를 묶는다. 단위검사는 직접 타격·시간 전진·좌표/속도·pending 주입을 쓰는 기술 검증이다. ex9 시험에서 직접 kill한 드롭은 기하가 없는 fixture이므로 제거하고 월드를 만들었다.

대표 CLI는 한 무기 인자 sling/seed7의 R01→R12 실제 좌표 전투와 disk Save/Load다. R10 hits4517/kills894/180초, R11 hits5394/kills960/176.4166667초를 유지했다. R12 hits8683/일반·지원 kills1028/boss kill1/월드292.1333333초, between13과 세 과거 receipt를 확인했다. 일반60초, 보스 예고와 돌진에서 좌표·phase·방향·deadline을 저장/복원하고 R12 좌표 시리즈를 기록했다. 기존 fixture는 시작HP10000·강화 장비이며 R12 진입 시 bible/jawbone/sling8, sling진화, stones2와 crown/seal/cloud/underwear/whirlwind5다. 전투 hit/kill 주입은 없지만 자연 완주·밸런스 실측으로 분류하지 않는다.

## 소유 반환과 후속

변경한19파일: Core `AdamSession.cs`, `BattleWorld.cs`, `BossEncounter.cs`, `BossRewards.cs`, `ExodusContent.cs`, `SaveFormat.cs`, `SaveState.cs`, `Waves.cs`; `content/exodus/catalog.json`; tests `RoundTwelveTests.cs`, `RoundElevenTests.cs`, `Program.cs`; Sim `CatalogReader.cs`, `ExodusRunner.cs`, `BossRunner.cs`, `Program.cs`; 대표 CLI만 바꾼 `tools/check_all.py`; 이 문서와 `docs/retrospectives/R12-core.md`다.

소유 밖 `tools/check_mutations.py:19`의 `completion_resume_refused` old/new literal은 Resume 상한 `>11`에서 `>12`로 coordinator가 동기화해야 한다고 전달했다. 기존 mutation/schema-mutation literal을 읽기 전용 대조했을 때 이것만 달랐다. 해당 도구와 다른 작성자의 `docs/harness/r13-implementation-01.md`는 이 worker가 편집하지 않았다.

Core 제품 소유권을 coordinator에게 반환한다. 독립 code review, Unity UI/월드/Host·DLL/provenance/sync·아트 연동, Editor/앱·모바일·성능·자연 밸런스는 NOT_RUN이며 coordinator 담당이다. 사용자 저장, Unity/DLL/아트/CI/기획/AGENTS/GUI/commit/push는 수정·실행하지 않았다. 후속 R13은 현재 R12 receipt의 네 번째 과거 보관을 다음 단위에서 설계해야 한다.
