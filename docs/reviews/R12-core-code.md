# R12 Core 독립 코드 리뷰

2026-11-18 KST. **PASS — 지정된 Core/Sim/catalog/tests 변경에서 막는 HIGH/MEDIUM 결함 없음.** `docs/harness/r12-implementation-01.md`, `docs/progress/R12-core.md`와 현재 diff/신규 `RoundTwelveTests.cs`를 독립 검토했다. 판정은 순수 Core와 반환된 실행 근거에 한정하며 Unity/실앱 통합 완료를 뜻하지 않는다.

## 주요 경계 검토

- **일반 ex9와 보스 시계 분리:** `BattleWorld.UsesNormalCharge`는 R04/d3와 R12/ex9만 선택한다. `Waves`는 해당 actor의 실제 생성시각을 기록하고 기존 MoveCharge의 생성+6 예고/1.2 정지/.6 돌진/종료+6 cooldown을 재사용한다. cooldown 추적 속도 키는 R12에서 ex9를 사용하도록 교정돼 있다. 기존 R04 ID/ChargeContract는 유지된다. 반면 `BattleWorld.cs:551`의 MoveChariot는 별도 BossElapsed/NextAttackAt을 사용하고, 돌진 시작에서 NextAttackAt을 period6만큼 증가시킨다. 따라서 4.8 예고→6 dash→6.6 chase→10.8 예고→12 dash이며 돌진 종료+6으로 밀리지 않는다. phase 경계에서는 실제 남은 구간만 적분한다.
- **접촉20/swept/우선순위:** `BattleWorld.cs:471`에서 player 이전 좌표를 보존하고 `:507`에서 boss와 player 양쪽 이동을 뺀 상대 선분을 합산 반경의 원과 교차 검사한다. 기존 ApplyPlayerDamage(contact)·armor·공통 무적 경로를 통과하며 R12는 slam/chain 분기를 실행하지 않는다. 같은 tick의 player 사망을 공격·반사보다 먼저 처리한다. 치명 Boss Hit는 지원 생성 분기보다 먼저 BossDefeated를 호출하므로 뒤늦은 지원이 없다. `AdamSession.cs:337`은 경고/돌진을 종료하고 World의 ClearEnded도 actor/charge/projectile을 정리한다. threshold mask는60%/30% 각6·총12를 한 번만 허용한다.
- **방어적 표시 API:** `AdamSession.Boss` 복사는 ChariotCharge까지 복사한다. `ChariotDashView`는 get-only 값으로 live boss Origin·저장된 고정 Direction·Phase·BossElapsed·절대 Deadline과 실제 tuning 기반 남은 거리/반경을 반환한다. 예고는 전 거리2.34, 돌진 중에는 남은 시간×속도이며 기본 위험 반폭은.85다. stage가 boss를 벗어나면 view는 null이다. Unity가 별도 공격 시계를 만들 필요가 없으며 실제 화면 투영은 후속 통합 범위다.
- **저장 복원:** `ChariotChargeState.Validate`(`BossEncounter.cs:78`)는 단위 방향/유한 deadline과 BossElapsed/NextAttackAt에서 계산한 phase·Until·Telegraphing의 일치를 검사한다. `SaveState.cs:133`은 진행 중 R12 보스의 live World를 요구하고, World time·actor/drop ID·현재 Boss/지원 ledger·weapon form·빈 normal charge를 검증한다. `BattleWorld.Restore`는 clock/대상/속도/형식 검증 뒤 저장 좌표·RNG·잔여 fixed step·무기 및 charge 상태를 복원한다. 예고/돌진 중 Load가 공격 시계를 새로 시작하지 않는다.
- **좁은 버전/미래 원문 보호:** root3은 유지하고 World5/boss4를 R12에 한정한다. `SaveFormat.SupportedExodusLifecycle`은 기존 R10/R11 normal/round_settlement·between11/12에 R12 normal/intro/boss/reward/settlement·between13만 더한다. R12에 round_settlement를 열거나 R13 전투를 허용하지 않는다. boss4의 R12 encounter/policy, 기존 boss2/3 및 World1~4 제한을 유지한다. 미지원 R13 combat/boss5/World6/phase/member는 UnsupportedSaveVersionException으로 primary와 backup을 보존하며 Commit도 이를 덮어쓰지 않는다. 중첩 미래 필드 보호와 typed lifecycle 검증을 함께 확인했다.
- **receipt·정산·호환:** R11→R12는 정의 검증을 상태 변경보다 먼저 수행하는 일반 전환이며 PreviousNormalRound=11과 RunId/성장/장비/진화/영구 snapshot을 유지한다. 세 과거 receipt는3/6/9 순서·완료·RunId 결합 검증을 계속 받으며 현재 R12 receipt는 Boss로 between13까지 유지한다. R13의 네 번째 과거 보관은 다음 단위다. 일반 종료는 drop/남은 성장 후 intro2초이며 조기 회복이 없다. BossGoldApplied의400, 고정 후보/Reward.Applied의 고갈100, serial별 성장25, CompletionApplied/pending0의220/회복/해금없음을 기존 once 경로로 처리한다. Load·이전 선택·재정산 거절 시험을 대조했다. 사망/새 도전은 프로필 유지 R01/schema2다. R09→R10 candidate 경로는 diff상 불변이고 R10/R11 fingerprint는 literal 고정 검사로 보호한다.
- **콘텐츠/Sim:** exodus_03의1020/XP4500/cap120/완료220, 구간270/340/410, ex7/8/9 수치 및 보스20000/contact20/400/6초·지원 계약이 계획과 일치한다. CatalogReader는 ex9 행동과 boss attack/summon 수치까지 검사한다. Sim의 기존 R10/R11 명령을 유지하며 대표 gate 명령을 한 무기/seed의 R12 시리즈로 확장했다. 신규10개 시험이 runner에 등록됐다.

## 재사용한 실행 근거

| 근거 | 확인한 결과 |
| --- | --- |
| `evidence/r02-20261117T154050016334Z/core-tests.xml`, `checks.json` | **191/191, failures0**, R12 testcase10개. standalone gate15개 명령 모두 exit0. 리뷰어가 새 suite를 실행한 결과가 아니다. |
| `evidence/r12-core/r12-final-related.log`, `summary.json` | 최종 관련 **10/10·exit0**. 초기9/10 및 fixture 교정8/10→9/10, ex9 속도 키 수정 전 CLI exit1, R11 설치끝/미지원 기대의 R13 이동은 progress의 실패 이력과 현재 diff를 대조했다. 최종 PASS로 실패 이력을 지우지 않았다. |
| `evidence/r12-core-mutation/summary.json` 및 baseline/restored·두 mutant 로그 | **baseline0 / mutant1·1 / restored0**, baseline/restored10/10. endpoint-only 원구조는 `Expected 80, actual 100`, 보스에 일반 D3 종료 cooldown을 쓰는 원구조는 `Expected 12, actual 13.799999999999999`와 저장 clock 검증으로 실패했다. 각각9/10·8/10이며 compiler error를 검출로 세지 않는다. |
| 최종 gate `14.log`와 CLI 소스 | `--r12-series sling 7` **PASS/exit0**. R10 4517hits/894kills/180초, R11 5394hits/960kills/176.4166667초, R12 **8683hits/일반·지원1028kills/boss1/월드292.1333333초**, between13·세 receipt 보존. 일반 midpoint 및 boss telegraph/dash에서 직렬화된 encounter/World 동등 disk Restore를 검사한다. |
| 같은 CLI의 좌표 배열 | R12 **147개 샘플**. 주기적 샘플에는 normal(null)/chase/dash가 있으며 telegraph 표본은 없다. 예고 저장/복원 근거는 별도 `boss_phase_restores=[telegraph,dash]`와 관련 phase replay 시험이다.147개를 모든 phase가 관측된 표본이라고 확대 해석하지 않는다. |
| 리뷰어 `git diff --check` | exit0. 제품·시험·CLI·저장 실행은 재수행하지 않았다. |

## 별도 후속과 미검증

root 소유 `tools/check_mutations.py:19`는 completion_resume_refused의 old/new anchor 상한을 함께11→12로 옮겼다. 현재 Resume guard와 일치하며 mutant가 주입하는 `run.Stage != "normal"`은 원래 완료 checkpoint Resume 거절 의미를 유지한다. root 예정 선택 변이1회는 별도 근거이며 이번 리뷰가 실행 PASS로 대신하지 않는다. 전체 suite/hash/30개 호환·변이 행렬을 새로 돌리지 않았다.

관련 단위시험은 직접 타격·timeout·좌표/속도·pending 주입을 쓰는 기술 검증이다. CLI는 시작HP10000/강화 장비와 R12 진입 bible/jawbone/sling8·sling진화·stones2/crown/seal/cloud/underwear/whirlwind5 fixture이며 실제 좌표 공격의 hit/kill 주입은 없다. 자연 완주·밸런스·Unity DLL/Host/Editor/실앱/GUI·native·모바일/기기·성능은 **NOT_RUN/UNVALIDATED**다. 이 Core PASS를 실앱 통합 PASS로 전용하지 않는다.

작성 산출물은 이 문서뿐이며 Core/Sim/Unity/제품/타 문서를 편집하지 않았다. 사용자 저장 조회·새 worker/Claude/Astra·commit/push도 없다. 자기 세션 usage `r12-core-review` start/end를 기존 도구로 기록한다. 지연된 공급자 event를 포함할 수 있는 세션 구간이며 전용 작업 비용이나 절감률은 미계산이다.
