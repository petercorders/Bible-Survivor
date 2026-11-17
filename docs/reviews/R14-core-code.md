# R14 Core 독립 코드 리뷰

- 검토일: 2026-11-18 KST
- 판정: **PASS — 요청된 Core/Sim/catalog/tests 범위에서 막는 HIGH/MEDIUM 발견 없음.**
- 소유 변경: 이 보고서만. 제품 코드, 다른 작성자의 문서, 저장 파일은 수정하지 않았다.

## 검토 범위와 판단

`docs/harness/r14-implementation-01.md`, `docs/progress/R14-core.md`와 현재 Core/Sim/catalog/tests diff를 읽었다. 기존 R13 Core 독립 PASS를 바탕으로 R14 추가분과 영향을 받는 저장·진입·정산 경계를 검토했다. Unity 소스, 앱 결합, 최종 화면은 이 판정의 범위 밖이다.

`BattleWorld`의 R14/ex13 분기는 R07/G3와 같은 종별 생성 순번을 사용해 홀수 왼쪽·짝수 오른쪽을 선택한다. 순번은 살아 있는 배우 수가 아닌 생성 일정에서 계산하므로 사망이나 이전 웨이브 생존자가 좌우 순서를 재설정하지 않는다. 난수 소비는 기존처럼 먼저 수행하고 경계만 덮어써 R07/G3의 기존 경로를 유지한다. ex13의 HP는 각 적 인스턴스에 저장되며 다른 ex13과 공유하거나 연동하는 코드가 없다. ex14/ex15는 기존 일반 배치 경로를 사용한다. R14의 속도·생성 수·XP·fingerprint 계약은 카탈로그와 일치하며 이전 R10/R11/R13 fingerprint 기대값을 유지한다.

R13 완료에서 R14 진입은 같은 RunId와 네 과거 receipt를 보존하고 R13의 240 보상을 재지급하지 않는다. `StartNextRound`는 상태를 변경하기 전에 다음 라운드 정의를 검증하며 R09→10의 후보 생성·원자적 교체 경로는 변경하지 않는다. R14는 root schema 3/normal World 3, previousNormalRound 13을 사용한다. 네 번째 receipt의 유효 범위는 R13과 R14의 실제 normal round에 한정되며 R14에 World 5나 현재 보스 상태를 허용하지 않는다.

R14 완료는 드롭 수거와 성장 선택을 마친 뒤 260을 한 번만 지급하고 between_rounds 15로 이동한다. 성장 선택당 25, 저장 재시도와 정산 재로드의 중복 지급 방지, HP·해금 상태 유지가 기존 흐름에 연결된다. R15 전투는 아직 지원하지 않는다. 미래 round/stage/World/receipt/root 입력은 Unsupported로 처리하고 primary와 backup 원문을 보존한다. 사망 후 명시적 새 도전은 프로필을 보존한 R01/schema 2로 돌아간다.

## 재사용한 검증 근거

새 시험·CLI·변이 실행 없이 아래 기존 결과와 필요한 로그/XML만 읽었다.

| 근거 | 확인한 결과와 한계 |
| --- | --- |
| `evidence/r14-core/initial-related.log` | R14 관련 6/6 PASS. pending 5·성공한 성장 선택 5·성장 골드 125·완료 골드 260을 별도로 기록한다. 실제 좌우 좌표, 독립 HP, 이전 웨이브 생존, pause, 디스크 재개, 정산 재시도, 미래 원문 보호를 포함한다. |
| `evidence/r14-core/r13-related.log`, `r07-related.log` | R13 관련 6/6, R07 관련 3/3 PASS를 재사용한다. |
| `evidence/r14-core-mutation/summary.json` 및 해당 로그 | 원래 R07/G3 전용 조건으로 되돌린 변이는 baseline 0 → mutant 1 → restored 0. 컴파일 오류 없이 R14 실제 좌우 좌표 assertion이 `Expected -11, actual -7.809579372406006`으로 실패했다. 유사 변이를 대신 세지 않았다. |
| `evidence/r02-20261117T172956904445Z/core-tests.xml`, `checks.json` | Core 203/203, 기존 gate 15개 명령 exit 0/PASS. 이번 리뷰에서 실행하지 않았다. |
| 같은 gate의 `14.log` | 네 boss receipt 보존, R14 midpoint 디스크 재개, 좌표 표본 90개, 완료 후 next_round 15/content_complete true/chapter_complete false. R14는 180초에 973킬/예정 생성 1140이다. |

R14 관련 시험은 잘못된 다음 정의가 진입 전 원상태를 유지하는 경우, 정산 저장의 beforeReplace 실패 후 재시도, 재로드 후 재정산 거절, 미래 입력의 primary/backup 보존을 확인한다. 기존 R12/R13 receipt와 과거 보상은 현재 라운드 보상과 분리되어 있다.

Root의 Resume 선택 변이는 별도 작업이다. 현재 검사기의 anchor 13→14 변경은 원래의 non-normal 거절 조건을 제거하는 변이 의미를 유지하지만, 그 선택 실행 결과를 이 보고서의 완료 근거로 합산하지 않는다.

## 실행 범위와 미검증

좌표 CLI는 시작 HP 10000과 강화 장비를 사용한 기술 시험이다. R14의 973/1140킬은 전처치나 자연 완주를 뜻하지 않는다. 이전 라운드 통과와 R12 phase 복원 표본도 기존 기술 근거를 재사용했으며, 새 전체 시험·CLI·변이·해시 행렬을 실행하지 않았다.

실제 Unity 앱·native 저장·GUI 가독성·자연 플레이·모바일·성능은 이 Core 리뷰에서 검증하지 않았다. 사용량은 기존 `record_model_usage.py`의 `r14-core-review` start/end로 기록하며, 세션 구간 자료를 이 작업의 독립 청구액이나 비용 절감률로 계산하지 않는다.
