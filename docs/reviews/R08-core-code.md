# R08 Core·일반 성장 고갈 교정 독립 리뷰

2026-11-16 KST. **PASS — 검토 범위에서 열린 HIGH/MEDIUM 없음.** `docs/harness/r08-implementation-01.md`, `docs/genesis-balance.md`의 일반 고갈 정책과 `docs/progress/R08-core.md` 기준 Core 관련 diff·저장/보상 경로를 검토했다. 이전 R07/Core 보스·진화 PASS를 재사용했다.

## 고갈 선택·저장 검토

- `GrowthChoices`는 기존 장비 후보 생성 결과가0개일 때만 `growth_gold_25:<ChoiceSerial>` 단일 후보를 만든다. 장비 후보가 남으면 기존 목록/ID·셔플/선택 의미를 유지한다. 후보 생성과 Resume는 인벤토리/골드를 변경하지 않는다. 기존 최대 장비의 pending 없는 저장도 읽은 뒤 다음 정상 성장에서 fallback을 만들 수 있다.
- `Choose`는 현재 pending/후보 membership을 먼저 확인하고 현재 serial의 정확한 골드 ID이며 장비 후보가 여전히0개인 경우에만25gold를 더한다. 이어 pending/serial을 각1 소비하고 다음 pending 후보를 만든다. 이전 골드 ID는 다음 serial 후보와 달라 재전송이 거절된다. XP/레벨은 선택으로 재계산하거나 버리지 않는다. pending 종료 시 기존 일반 정산 gate를 사용한다.
- 저장 검증은 골드 후보가 현재 장비 고갈 상태·serial로 생성되는 정확한 단일 목록인지 비교한다. 장비 후보가 남는 상태·잘못된 serial·다른 후보와 공존을 거절한다. 새 DTO/스키마/저장 필드는 추가하지 않고 기존 Choices 문자열에 정책 ID를 명시했다. 기존3무기/10보조의 유효 저장 ID를 유지한다.
- header-first 검사는 모르는 일반 후보 ID/미래 `growth_gold_50` 등을 typed fallback 전에 Unsupported로 분류한다. SaveStore는 Unsupported를 backup 복구로 덮지 않으며 Commit도 현재 파일을 먼저 읽어 미래 primary/backup을 보호한다. 알려진25 후보의 불가능한 현재 상태는 invalid로 구분한다.
- SaveStore의 동일 operation/payload 검증과 revision gate는 바뀌지 않았다. 골드 선택 후 state를 같은 operation으로 다시 Commit하면 이미 저장한 지급을 재실행하지 않는다. 새로운 Choose 호출도 이전 serial ID로는 통과하지 못한다. 실패 시 동일 state/revision/operation을 보존하는 Host 연결은 후속 Unity 검증 범위다.
- 보스 reward의 capped/stale 인벤토리 preview 검증은 유지하고, 보스 선택 후 일반 후보가0개라는 이유만으로 선택을 막던 사전 조건을 제거했다. 따라서 마지막 장비 보상 후 pending 일반 성장은 gold25로 정상 전환한다. 보스의 후보/receipt·gold100 지급 정책과 일반25 정책은 별도이며 보스 선택 이후 기존 serial로 일반 후보만 재생성한다.

## R08 전투·정산 검토

- catalog의 총1020/XP3500/cap110·270/340/410, H1→H1/H2→H1/H2/H3 조합·HP/접촉/속도를 소비한다. 세 종은 기존 추적/접촉이며 새로운 속박·성장·피해지형·돌진을 추가하지 않는다. G3 좌우 특화는 R07/g3 조건에만 남고 H는 기존4방향 spawn/RNG를 사용한다. R08 계약만 fingerprint에 추가한다.
- R07완료→R08 진입에서 PreviousNormalRound7로 이전 일반 완료 사실을 보존하며 두 보스 history/진화·성장/remainder/영구 snapshot을 유지한다. World3와 빈 charge를 검증하고 현재 Boss를 일반전에 남기지 않는다. 과거 R07 무쓰기 Load·R03/R06 receipt identity/완료 검증·중간 World replay 경로를 유지한다.
- 종료 후 drop XP→남은 성장→140gold·최대HP 회복/between9의 기존 deferred 정산을 사용한다. 완료 전 조기 지급/회복과 반복 완료를 막는다. unlock null이므로 기존 해금 목록에 새 항목이나 장비를 만들지 않는다. R09 진입과 미래 일반9/between10·모르는 history 필드/정책은 거절/원문 보호한다.

## 재사용 실행 근거

- `evidence/r08-core-01/growth-restored.log` **5/5 exit0**, `related-after.log` **10/10 exit0**(R08 5+R07 3+관련schema2)를 코드와 대조했다. 신규 고갈 시험은 마지막 장비→fallback, 생성/Load 무지급, 두 pending 순차 지급/ID 재전송 거절, 동일 operation Commit retry, XP/레벨 유지, 유효 장비가 남을 때 금지, gold50 primary/backup 원문 보호, 보스gold100/마지막 장비 후 일반 pending 전환을 확인한다.
- `old_finish_without_r08_growth_gate.log`: 정확한 옛 Finish 구조에서 **expected round_settlement/actual between_rounds**, 3/4 exit1→복원4/4 exit0. `old_exhaustion_throw.log`: 정확한 옛 EnsureChoices의 후보0 throw에서 신규 고갈 시험이 실제 실행 예외로 **4/5 exit1**→복원5/5 exit0이다. 최초 CLI/회귀 실패도 `cli-exhaustion-before.log`·`exhaustion-before.log`에 보존되어 있으며 컴파일 실패를 검출로 세지 않았다.
- 같은 `--r08-series sling 7`의 최종 `cli-result.json`은 **exit0**, actual1008kills/7184hits·180초→between9·gold1959·HP10000이다. H3 중간 Load·두 보스 receipt/해금 목록 불변·PreviousNormalRound7/sling_fivefold를 유지한다. 1020킬 완주가 아니라180초 종료다.

리뷰어는 관련 시험/변이/CLI와 기존 전행렬/전체 해시·Unity/GUI/build를 재실행하지 않았다. 새 재현이 필요한 확정 의심이 없어 probe도 추가하지 않았다.

## 한계와 인계

대표 CLI는 R1/R2 timeout·장비8/HP10000의 기술 fixture이며 R03 이후 실제 좌표 피해/처치의 근거다. 자연 난이도·밸런스/앱1020킬 완주를 검증한 것은 아니다. gold25는 승인된 초기 경제값이며 경제 밸런스 수용은 별도다. Unity는 전체 serial ID를 Host/Core Choose에 전달하고 일반 `골드 주머니/영구 골드+25`를 보스gold100/가짜 장비 레벨과 구분해 표시해야 한다. Unity/DLL/provenance·아트·실사용 저장·native/mobile/device/performance는 미검증이다.

제품·시험·도구·Unity·아트는 편집하지 않았고 본 문서만 소유한다. usage `r08-core-review` start/end는 세션 기준점이며 stale 시작 event와 개별 worker 비용/effort 미측정을 구분하고 비용·절감률을 추정하지 않는다. 순수 Core 코드 리뷰 게이트는 PASS다.
