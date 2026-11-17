# R10 Core 독립 코드 리뷰

2026-11-17 KST. **PASS — 검토 범위에서 열린 HIGH/MEDIUM 없음.** `AGENTS.md`, `docs/harness/r10-implementation-01.md`, `docs/progress/R10-core.md`와 실제 Core/content/tests/Sim diff를 대조했다. R10 `exodus_01`과 R09 완료에서의 명시 진입만 검토했으며 R11~R15 구현을 통과로 세지 않는다.

## 저장·챕터 경계

- `SaveState.cs:55`, `:69`, `:304`에서 새 저장은 root2, 기존 schema1은 메모리에서만2로 승격하며 root2를3으로 자동 변경하지 않는다. 프로필만 있는 root3은 허용하고, 명시 새 도전 생성은 프로필 구매·해금·잔액을 그대로 둔 채 R01/root2로 전환한다(`AdamSession.cs:75`).
- root3은 R10 normal/round_settlement와 between11에 한정하며 정확한 설치 fingerprint를 확인한다(`SaveFormat.cs:156`, `SaveState.cs:70`). R11 normal과 미래 root/중첩필드/보스 버전·정책은 Unsupported 경로로 보호한다. root2의 기존 stage/round 지원 경계와 schema1 경로는 별도로 남아 있다. root 번호와 World 내부 버전은 분리된다.
- `CompletedBoss`/`CompletedSecondBoss`/`CompletedThirdBoss`는 각각 R03/R06/R09 identity·RunId·완료 receipt를 검증한다(`SaveState.cs:114`). 새 필드는 기본값 미출력이라 기존 창세기 저장에 추가되지 않으며 root1/2의 세 번째 receipt는 거절한다. 유다 receipt는 기존 첫 기록을 덮지 않는다.
- `SaveStore.LoadCore`는 Unsupported를 backup 복구로 취급하지 않는다. Commit도 현재 파일을 읽은 뒤 동일 operation의 payload를 비교하고 revision을 확인한다(`SaveState.cs:215`, `:241`). 미래 저장을 이전 backup으로 조용히 덮어쓰는 새 경로는 없다.

## 진입·전투·정산

- `PrepareExodus`는 완료 R09와 남은 성장0을 요구한다. catalog 검증 후 직렬화 복사본만 바꾸고, 새 root3/Wave를 검증·Resume할 수 있는지 확인해 반환한다(`AdamSession.cs:109`). 원본 session/snapshot의 receipt·재화·장비·진화·레벨/XP·ChoiceSerial·영구 snapshot·kill 나머지·RunId를 유지한다. 진입 지급/추가 회복은 없으며 World/drop/피격시계와 새 Wave의 시계·pause를 초기화한다. R09의 적탄·적좌표·무기 ledger는 새 World로 전달되지 않는다.
- 이 API는 후보 생성 계약으로 Host의 저장 후 게시를 지원한다. 기존 `StartNextRound`는 여전히 R10 자동진입을 거절한다. 후속 Host는 후보/expected revision/operation ID를 고정하고 Commit 및 Load/Resume 성공 후 새 State/Session/World를 게시해야 한다. 기존 `AdamHost.EnterNextRound`의 선게시 후 Checkpoint 구조를 R10에 그대로 재사용하면 승인 계약이 충족되지 않는다. 아직 구현되지 않은 Host 흐름은 이번 Core PASS의 대상이 아니다.
- catalog와 `ExodusContent.Validate`는 global10/local1/id·900/cap110/XP3600/gold180·세 종의 HP/접촉/속도·세 구간 조합을 고정한다. 창세기 fingerprint는 변경하지 않고 R10에만 새 속도 계약을 붙인다. R10은 기존 추적·접촉 경로를 사용하며 D3 돌진/G3 좌우 생성/R09 사슬 분기는 활성화하지 않는다.
- R10 World는 내부3, 빈 Charges, 적탄 없음으로 Capture/Restore된다(`BattleWorld.cs:713`, `SaveState.cs:159`). 중간 저장은 clock·actor/drop ID·진화 ledger와 일치해야 한다. 종료는 기존 drop XP→남은 성장/만석25gold→완료180gold once·최대HP 회복·between11 경로다. 완료 flag와 stage가 반복 지급을 막으며 해금을 추가하지 않는다. `ChapterComplete`와 `ContentComplete`를 구분하고 between11에서 다음 전투는 열지 않는다.

## 재사용한 실행 근거

- `evidence/r02-20261117T062135733099Z/core-tests.xml`: **176/176, failures0**, `checks.json`: standalone 전체 gate의 명령별 **exit0**. XML의 신규 R10 5개와 실제 시험 코드를 대조했다. 기존 schema1/2 대표 geometry 저장의 Load 무쓰기·내용 보존, R09 완료 Load 무쓰기, 잘못된 catalog와 staging 실패의 원본 보존, 동일 operation 진입 재시도, 세 receipt/성장 유지, 중간 World 후속 동등, pending 정산/완료 once, future primary/backup 보호 및 root3 프로필 새 도전을 포함한다.
- `evidence/r10-core-mutation/summary.json` 및 로그: **baseline0 / mutant1 / restored0**, 복원 **5/5**. 원래 generic boss 전환처럼 R09를 `CompletedBoss`에 넣는 변이에서 실제 실행 중 `Invalid or reordered completed boss history`가 발생하여4개 시험이 실패했다. 컴파일 실패를 검출로 세지 않았다. 기존 임시 사본의 변이 근거를 재사용했다.
- `evidence/r10-old-reader/summary.json`, `0.log`, `1.log`: 이전 **55b5833** reader가 새 root3의 Load/Commit을 거절하고 primary/backup을 보존한 **exit0** 근거다.
- `evidence/r10-core/coordinate-series.json`: **PASS/exit0**, seed7·R01→R09 완료→R10→between11, 중간 disk Restore·세 receipt 보존·진입 무지급/무회복, R10 **4517hits/894kills/180초**. 전체 gate 이후 교체한 `--r10-series` 명령의 별도 실행 근거이며, gate JSON의 마지막 명령은 기존 `--r09-series`임을 구분한다.
- 리뷰어의 `git diff --check`: **exit0**. 새로운 의심이나 의미 있는 검증 공백이 없어 전체 suite/CLI/변이/Unity 검사를 재실행하지 않았다.

## 한계와 인계

좌표 CLI는 기존 장비8/보조1/HP10000 기술 fixture다. 단위검사의 R09 완료 준비에는 직접 boss hit, R10 전체 timer 검사에는 큰 HP 주입이 있다. 자연 플레이·난이도/경제 밸런스·Unity/Editor/DLL·화면/입력·모바일/실기기·성능·사용자 저장은 미검증이다. R10 Host의 명시 진입·실패/결과 불명·중복 클릭/재시도·저장 성공 후 게시와 화면 상태는 후속 통합 리뷰에서 확인해야 한다.

제품·시험·도구·Unity·아트는 편집하지 않았고 이 문서만 작성했다. 추가 worker/Astra 호출·commit/push는 없다. usage `r10-core-review` start/end는 자기 세션의 기록이며 실제 청구액·절감률은 미계산이다. 기존 graphify 산출물이 없어 읽기 전용 소스/diff와 검증 증거를 사용했다.
