# R15 Core 독립 코드 리뷰

- 검토일: 2026-11-18 KST
- 판정: **PASS — 지정 Core/Sim/catalog/tests 범위에서 막는 HIGH/MEDIUM 발견 없음.**
- 소유 변경: 이 보고서만. 제품·타인 문서·사용자 저장은 수정하지 않았다.

## 범위와 판단

`docs/progress/R15-core.md`, `docs/harness/r15-implementation-01.md`, 현재 Core/Sim/catalog/tests diff와 직접 연결된 기존 전투·보상·저장 검증 경로를 읽었다. R14 독립 PASS를 재사용하고 새 R15 단위와 R09/R12 공통 경로의 영향을 검토했다. Unity/DLL/provenance·실앱·GUI는 이 판정 밖이다. 새 전체 시험·CLI·변이·Editor 실행은 하지 않았다.

| 경계 | 판단 |
| --- | --- |
| 콘텐츠·Rules·기존 계약 | exodus_06/global15, 일반180초·생성170초·cap120·총1200·XP5600·완료280/null 해금이 계획과 일치한다. ex16/17/18은 추적·접촉만 사용한다. 보스는 HP26000/contact22/kill500/5초 주기/피해18이며 R09 피해15는 유지한다. `UsesChainProjectiles`는 R09·R15에만 참이고 공개 projectile speed/radius/lifetime/cap/spread는 실제 공통 상수1.35/.16/6/12/20도를 반환한다. 기존 R09 fingerprint의 분기 결과와 수치·정책은 같고, R10~R14 계약은 바꾸지 않고 R15 계약만 추가한다. |
| 예고·발사·live origin | 4초에 현재 boss/player 차이로 center/±20도 방향을 고정하고 겹침은(1,0)을 사용한다. 예고 중 boss 이동을 멈추며 5초에 실제 EnemyPositions의 boss 위치에서3발을 생성한다. 발사는 무기/반격 처리 뒤 tick 끝에서 이뤄져 새탄은 다음 tick부터 이동한다. 공개 Boss/HostileProjectiles는 복사본이며 Unity가 별도 시계나 PNG 판정을 만들 필요가 없다. |
| 상대 swept·수명·사망 순서 | 기존 상대 선분 `from-previousPlayer → to-endPlayer`와 실제 player 반경을 사용한다. 절대 BornAt으로 위치를 계산하고 남은 TTL로 마지막 tick의 player 이동도 제한한다. 접촉한 탄은 armor/무적으로 피해가 막혀도 제거된다. 적탄·접촉의 player 사망을 무기/반격보다 먼저 처리하며 boss kill 후 적탄·배우를 정리한다. pause는 전투 시계와 TTL을 함께 멈춘다. 새 homing/slam/dash/반사 규칙을 추가하지 않는다. |
| 지원병 | 60%·30%에서 ex17 각6 once, 총12와 발급 actor ID/mask를 기존 경로로 관리한다. 치명타는 BossDefeated를 먼저 처리해 추가 소환하지 않는다. 신규 시험은 두 threshold와 재피격 무추가·13개 world actor를 확인한다. |
| R14 진입·네 history | StartNextRound는 다음 정의를 상태 변경 전에 검증한다. R14 완료260 재지급 없이 RunId·장비/forms·성장/serial·프로필과 네 과거 receipt를 유지하고 PreviousNormalRound14로 시작한다. normal World4는 clock0/빈 적탄/NextHostile1/빈 charge, LastHitTime-1이며 보호 intro 전 조기 회복이 없다. R09→R10의 PrepareExodus 후보·저장 원자성 경로는 변경하지 않았다. |
| 저장 shape·미래 원문 | root3, R15 current Boss3/World4/적탄Version1과 기존 ChainDirections를 재사용하며 새 DataMember나 다섯 번째 과거 receipt를 만들지 않는다. current R15 Boss와 R03/R06/R09/R12 네 history를 각각 검증하며 R12 history는 Boss4를 유지한다. 시계·방향·ID/BornAt/TTL·5초 발사 ledger·지원 mask·동결 보상 ledger를 기존 검증에 연결한다. 지원 범위는 R15 normal/intro/boss/reward/settlement/between16이며 R15 round_settlement나 R16 전투를 열지 않는다. World5/Boss4·미래 적탄/중첩 필드·policy·receipt 미래 입력은 Unsupported로 Load/Commit을 거절하고 primary/backup 원문을 보존한다. |
| 보상·완료 | kill500, 고갈 자동100, 성장 선택당25, 완료280은 별도 once 경로다. 최대3 후보·가능진화 우선·이미진화 제외·동결후보 무지급 Load와 full serial 중복 거절을 유지한다. 미해결 성장 중에는 완료/회복을 막고 CompleteBossSettlement에서만280·회복·between16을 적용한다. `ExodusComplete`는 명시 R15 완료 receipt 상태이며 설치 끝 `ContentComplete`, 창세기 R09 `ChapterComplete`/CanEnterExodus와 구분된다. 재로드 후 자동 다음 책/강제 초기화가 없고 명시 새 도전만 프로필을 보존한 R01/schema2로 시작한다. |

## 재사용한 검증 근거

| 기존 근거 | 확인한 결과 |
| --- | --- |
| `evidence/r15-core/initial-related.log`, `related-nullability-fixed.log` | 최초는 기술 시험 Forms.Remove의 nullable 경고로 compile exit1이었다. 표기 교정 후 관련8/8 PASS이며 이 compile 실패를 회귀 검출로 세지 않는다. |
| `r09-related.log`, `r12-related.log`, `r14-related.log` | 관련6/6·10/10·6/6 PASS를 재사용한다. 기존 미래 경계 시험은16으로 옮기고 R14 잘못된 지원15 정의를 사전 catalog 검증의 ArgumentException으로 확인하며 금액 assertion을 유지한다. |
| `evidence/r15-core-mutation/summary.json` 및 원로그 | 원래 endpoint-only 충돌 조건을 복원한 변이: baseline0/8PASS → mutant1/7PASS·1FAIL → restored0/8PASS. 컴파일 오류 없이 상대 swept 시험이 `Expected 87, actual 100`으로 실패했다. player가 한 fixed tick에-1→+1, 탄은0→.0225로 이동하여 양 endpoint가 합산반경.41 밖이어도 교차하는 회귀를 직접 검출한다. |
| `evidence/r02-20261117T181425053122Z/core-tests.xml`, `checks.json` | Core211/211, gate15명령 PASS. 리뷰에서 재실행하지 않았다. |
| 같은 gate `14.log` | sling/seed7 대표 좌표 CLI PASS, normal midpoint 및 telegraph/projectiles disk Restore, 네 history 보존·현재R15Boss·between16·ExodusComplete/ContentComplete true·ChapterComplete false를 확인한다. R15 hits12891/kills1097/boss1/World335.6833333초/좌표168샘플이다. |

신규8개 시험을 읽어 실제 고정방향/겹침/다음 tick 이동·pause/TTL·defensive copy·상대 swept18와 seal 적용·무적 접촉 소멸·same-tick death 우선·killclear·지원6/12 once·디스크 예고/실탄 재개·미래 primary/backup 보호·선택/완료 실패 retry·재정산 거절을 확인했다. 정상 full-serial 성장 normalPending5/후보1/Choose5와 기술 bossPending2/후보1/Choose2를 분리한다. 자동 고갈100은 사용자가 선택한 카드 횟수로 세지 않는다.

Root의 Resume 선택 변이는 별도 작업이다. 현재 anchor14→15 변경은 기존 non-normal 거절 조건을 추가하는 변이 의미를 유지하며, 그 별도 실행 결과를 여기서 새로 완료했다고 주장하지 않는다.

## 한계와 사용량

신규 단위 시험은 최대 장비·timeout·직접 Hit, boss 좌표30/무기 clock1000, 겹침 HP10000 등의 기술 준비를 사용한다. 진화 후보 시험은 과거 선택 reward ledger가 요구하지 않는 진화 한 개만 제거한 뒤 기존 Validate/Resume을 사용해 진화 가능성을 만든다. 이는 자연 획득·자연 완주 근거가 아니다. bossPending2 역시 정산 격리를 위한 주입이다.

CLI는 초기 HP10000/강화 장비를 사용하며 R15 진입 시 무기3개8·sling진화·stones2/나머지 보조5다. CLI에 직접 hit/kill 주입은 없지만 **1097킬(일반·지원 포함)/일반 예정1200을 전처치·자연 밸런스로 세지 않는다.** 좌표 샘플의 적탄 관측 최대는6이며 **cap12를 실제로 채웠다는 실측이 아니다.** 상한12는 Rules/생성 guard/저장 검증 계약과 구분한다. R10~R14의 기존 hits/kills/시간 및 R12 예고·돌진 Restore 결과는 유지된다.

실제 Unity 앱·native 저장/입력·GUI 가독성·자연 플레이·모바일·성능은 **NOT_RUN/UNVALIDATED**다. 자기 usage는 기존 도구의 `r15-core-review` start/end로 기록한다. 지연된 이전 tail을 포함할 수 있는 자기 세션 구간이며 독립 작업 청구액·단가·비용 절감률은 계산하지 않는다.
