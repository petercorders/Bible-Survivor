# R08 요셉 순수 Core checkpoint

2026-11-16 KST. 기준d988ad6, 실행 계약 `docs/harness/r08-implementation-01.md`와 경험치절의 고갈 교정 결정. **관련 Core 검증 PASS·독립 코드 리뷰 대기**. R07완료→R08일반→성장/140gold·회복→between9 연결이며 Unity/앱 완료는 아니다.

catalog의1020/XP3500/cap110, wave270/340/410·180초/170초 생성마감, H1 160/10/.65·H2 340/12/.40·H3 400/13/.55를 사용한다. 구간은 H1→H1/H2→H1/H2/H3이며 기존 추적·접촉 경로다. R07 G3 좌우특화는 round7/g3 조건에 머무르고 H는 기존4방향/RNG spawn을 쓴다. 새 효과·패턴·저장필드·버전은 없다.

기존 일반 정산에서 성장해결 이후140gold/최대HP회복/between9을 한 번 적용한다. 해금null로 새 장비/해금이 없다. PreviousNormalRound7로 이전 일반 완료연쇄를 유지하고 CompletedBoss(R03)/CompletedSecondBoss(R06) receipt·첫3진화·성장·remainder/영구snapshot을 보존한다. schema2/World3와 미래원문보호를 재사용하고 R09진입은 거절한다.

최초 연속 실제좌표 CLI는 장비가 모두 최대가 된 뒤 일반 선택 후보0에서 기존 EnsureChoices 예외로 중단됐다. 원출력 `cli-exhaustion-before.log`를 보존했다. coordinator가 원본 경험치절과 실행 계약에 일반 성장 고갈 시 골드25 정책을 확정한 뒤 최소 교정했다. 기존 Choices 문자열에 `growth_gold_25:<ChoiceSerial>`을 사용하며 후보0일 때만 단일 선택을 만든다. 후보 생성/Load는 무지급, 선택 시25gold와 pending/serial 각1을 소비한다. serial별 ID로 이전 선택 재전송을 거절하며 여러 pending은 하나씩 처리한다. 기존 장비후보가1개라도 남으면 노출하지 않는다. 보스 마지막 장비 보상 후 일반 pending이 고갈되어도 정상 전환하며 보스gold100 정책은 유지한다. 미래 선택 ID는 header-first 보호, 알려진25 후보의 잘못된 serial/유효장비와 공존은 저장검증으로 거절한다. 기존 장비 선택 ID/저장 의미와 schema는 그대로다.

근거는 `evidence/r08-core-01/summary.json`과 개별 로그에 있다.

- `--r08-only` 최초4/4 exit0; 고갈 회귀 추가 직후 원래 throw로4/5 FAIL(exit1), 교정 후5/5 PASS. 신규1검사 안에서 마지막장비→고갈, 생성/Load무지급, 순차선택/중복ID·SaveStore retry, XP/레벨 유지, 유효장비 남을 때 금지, 미래gold50 원문보호, 보스gold100 별도/마지막장비 전환을 확인했다.
- `--r08-related` 최종10/10 exit0(신규R08 5+기존R07 3+관련schema2). 세구간/1020·3500/cap110, R07무쓰기Load·이력, 기존spawn/추적·접촉/중간replay, 성장·완료once/null해금/회복·미래보호를 확인했다.
- 격리사본의 정확한 옛 Finish 성장gate(round8 미포함)는 R08 growth assert FAIL(exit1)→원복4/4 exit0. 정확한 옛 EnsureChoices 후보0 throw는 고갈 assert FAIL(exit1)→원복5/5 exit0. 기존18+4 anchor 고유성 정적확인·diffcheck exit0, driver/전행렬/새도구 미실행.
- 같은 `--r08-series sling 7` 재실행 exit0. R03~R08 실제좌표전투, R08 180초/1008킬/7184hits, H3중간Load·두보스receipt/해금목록 불변·between9·sling_fivefold 유지. R1/R2 timeout·장비8·HP10000 준비 fixture이며 R03 이후 피해/처치를 주입하지 않았다. HP10000·골드1959는 기술 결과이고 자연밸런스 수용이 아니다.

Unity 인계: H1~3를 `EnemyPositions`의 독립 actor/실제좌표로 표시한다. R08 normal/round_settlement→between9, 완료금140·unlock null을 사용한다. `PendingChoices`에 `AdamSession.GrowthGoldChoicePrefix`로 시작하는 ID는 `골드25`로 표시하고 원래 전체 ID를 Choose에 전달한다. 일반 fallback과 BossChoices gold100을 혼동하지 않는다. 두보스 history slot/기존 첫3진화 표현은 유지한다. Unity/DLL/provenance/art/CIhooks/GUI/실사용저장/commitpush 미실행. 독립리뷰·Unity/자연플레이·모바일/실기기성능 NOT_RUN. 사용모델Sol, 자기usage r08-core-01 및 교정구간 r08-growth-fix-01 시작/끝을 기록하며 청구비용/절감률은 미측정이다.
