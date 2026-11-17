# R13 Core 독립 코드 리뷰

2026-11-18 KST. **PASS — 검토 범위에서 막는 HIGH/MEDIUM 결함 없음.** `docs/harness/r13-implementation-01.md`, `docs/progress/R13-core.md`, 현재 Core/Sim/catalog/tests diff 및 신규 `RoundThirteenTests.cs`를 독립 검토했다. 기존 실행 근거를 재사용했으며 Unity/실앱 완료 판정은 포함하지 않는다.

## 주요 경계

- **네 번째 receipt의 목적지:** `AdamSession.StartNextRound`는 완료 R12/between13·pending0·순차 번호를 요구하며 R13 정의를 상태 변경 전에 검증한다. 현재 Boss를 `CompletedFourthBoss`로 방어 복사하고 PreviousNormalRound=12로 기록한 뒤 Boss를 비운다. 기존 R03/R06/R09 receipt는 덮어쓰지 않는다. 진입/Load/재호출 경로에는 R12의400·선택100·완료220 지급이 없으며 RunId/프로필/장비/보조/진화/성장을 유지한다. 신규 disk entry 시험이 기존3개 identity와 이전 Boss 전체 JSON의 네 번째 필드 동등성, gold 무변경, 반복 진입 거절을 확인한다. 잘못된 R13 gold999 정의는 전체 snapshot을 바꾸지 않고 거절한다.
- **root3/World3와 과거 Boss4:** optional CompletedFourthBoss만 명시 shape/재귀 검증에 추가하며 root 버전은 올리지 않는다. R13 normal/round_settlement·between14에서만 네 번째 receipt를 허용한다. `SaveState`는 완료 R12·같은 RunId·고정 policy/후보/선택 ledger를 기존 Boss 검증으로 검사하고 누락·불완료·순서 위반을 거절한다. 현재 R13 Boss는 없고 World3·빈 Charges·적탄 없음/NextHostile0·새 clock·LastHitTime=-1로 시작한다. 이전 receipt의 Boss4/ChariotCharge는 역사 기록이며 현재 World5를 허용하는 근거가 아니다. World5는 R12 전투/완료 체크포인트에만 허용되고 R13은 inherited dash/SpawnedAt을 거절한다.
- **미래 원문 보호:** SaveFormat의 lifecycle은 R13의 일반 단계와 between14만 추가한다. R14 combat/미지원 stage·R13 World5·boss5·과거 receipt의 미래 phase/member·root4는 UnsupportedSaveVersionException으로 Load/Commit을 거절하며 primary/backup을 보존한다. 네 번째 receipt가 다른 라운드에 등장하는 경우도 미래형식으로 처리한다. 구 R12 reader도 R13 normal/between14 또는 새 필드를 조용히 읽고 버리는 경로를 갖지 않는다.
- **성장25/완료240 once:** R13을 기존 later-normal Finish/CompleteNormalSettlement에 추가해 drop 회수→pending 해결→240/null 해금/최대HP회복/between14 순서를 따른다. 마지막 성장 선택 전에는 정산이 차단되고 serial/단계/완료 flag가 중복 지급을 막는다. 신규 정산 시험의 실제 pending5·후보1·성공Choose5·성장125·완료240을 구분했다. 선택 및 완료 저장 각각 injected IOException 뒤 동일 operation retry와 Load 후 이전 선택/재정산 거절을 검사한다. 사망/명시 새 도전은 프로필 유지 R01/schema2다.
- **콘텐츠/기존 경로:** exodus_04 local4/global13, cap110/1080/XP4800/완료240·보스/해금 없음, 세 구간280/360/440와 ex10/11/12 HP150/280/600·접촉10/12/15·속도.60/.60/.35가 승인 계획과 일치한다. 전부 기존 추적·접촉이며 R12/일반 D3 시계나 신규 공격은 활성화하지 않는다. R13에만 새 speed fingerprint를 붙였고 R10~12 정의 계약은 diff상 불변이다. R10/R11 literal 고정·R12 catalog/Core fingerprint 동등 시험을 유지한다. R09→R10 PrepareExodus candidate 경로도 불변이다. Sim은 기존 명령을 유지하고 R13 entry/중간/완료 disk Load와 네 receipt를 검사한다.

## 재사용한 증거

| 근거 | 결과 |
| --- | --- |
| `evidence/r13-core/initial-related.log`, `r12-related.log`, `r10-r11-related.log` | R13 **6/6**, R12 **10/10**, R10~11 **10/10**, 각exit0. 시험 본문과 대조했다. |
| `evidence/r13-core-mutation/summary.json` 및 원로그 | **baseline0/mutant1/restored0**, 6/6→1/6→6/6. R12 receipt 목적지만 원래 공통 `CompletedBoss`로 돌리고 PreviousNormalRound=12는 유지한 변이다. mutant5개가 실제 `Invalid or reordered completed boss history` 검증 예외로 실패했다. compile 실패가 아니며 marker 회귀 검출과 혼동하지 않는다. |
| `evidence/r02-20261117T165105556169Z/core-tests.xml`, `checks.json` | **197/197·failures0**, R13 testcase6개. 최종 gate15명령 모두exit0. 리뷰어 재실행은 없다. |
| 같은 gate `14.log` | `--r13-series sling 7` **PASS/exit0**. R13 **7594hits/1059kills/boss0/180초·좌표90샘플**, entry/60초 midpoint/완료 disk Load, 네 receipt·RunId와 between14/ContentComplete=true/ChapterComplete=false를 확인한다. 전체1080 처치를 달성했다고 세지 않는다. |
| 같은 CLI의 이전 구간 | R10 4517hits/894kills/180초, R11 5394hits/960kills/176.4166667초, R12 8683hits/1028kills/boss1/292.1333333초로 이전 결과를 유지한다. R12 예고/dash 별도 disk Restore도 유지한다. |
| 리뷰어 `git diff --check` | exit0. 새 suite/CLI/Editor/GUI/저장/변이를 실행하지 않았다. |

선행 R12 CI 실패와 root의 미래 World 입력5→6 복구·정확한 복구 SHA `3bc19dc7d75cece0768bc4506843d1a1f29cb9d3`의 두 CI 실제exit0 이력은 progress에서 구분한다. 해당 복구와 R13 기능 검증을 합쳐 무실패 이력으로 바꾸지 않는다.

## 후속·한계

root 소유 Resume 변이 anchor12→13은 old/new 상한을 함께 옮기고 원래 non-normal Resume 거절 주입 의미를 유지한다. 선택 Resume 변이 실행은 root 별도 작업이며 이번 리뷰의 receipt 변이 근거로 대신하지 않는다.

단위시험은 timeout/직접 Boss Hit·최대 장비/진화·dropXP100 기술 fixture다. CLI는 시작HP10000/장비8/보조1이며 R13 진입 장비는 bible/jawbone/sling8·sling진화·stones2/crown5/seal5/cloud5/underwear5/whirlwind5다. 실제 좌표 hit/kill 주입은 없지만 자연 장비/완주·밸런스 실측이 아니다. Unity catalog/Host/UI/World·DLL/sync/Editor/실앱·native/모바일·성능은 NOT_RUN/UNVALIDATED다.

작성물은 이 문서뿐이며 제품/시험/타문서/공유 변경을 편집·원복하지 않았다. 사용자 저장 조회·새 worker/Claude/Astra·commit/push도 없다. 자기 usage `r13-core-review` start/end는 기존 도구로 기록하며 지연된 공급자 event를 포함할 수 있는 세션 구간이다. 실제 청구액·전용 비용·절감률은 미계산이다.
