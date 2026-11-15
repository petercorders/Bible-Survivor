# R03 Core 실행 02 독립 코드 리뷰

2026-11-16 KST. **PASS — 검토 범위에서 열린 HIGH/MEDIUM 없음.** `docs/harness/r03-implementation-02.md`의 정식 보상·첫3진화·완료 정산 순수 Core slice를 검토했다. 앞선 boss/regen slice의 독립 PASS를 재사용한다. 이 판정은 R04 또는 Unity 지원 완료를 뜻하지 않는다.

## 검토 결과

- `BossRewards.Generate`는 처치 때 동결한 장비·보조·form·해금과 안정 reward ID로 최대3고유 후보를 만든다. 자격 있는 첫3진화를 먼저 포함하고 남은 칸에 유효 장비1단계/신규1레벨을 채운다. 부족할 때만 골드100을 한 번 추가한다. 전투 RNG와 일반 ChoiceSerial을 소비하지 않는다. 읽기 API는 후보를 복사하고 저장 검증은 동결 입력에서 계산한 후보와 비교한다.
- `PromoteBossReward`는 이전 `pending-v1` receipt의 장비/자격과 기존 fingerprint를 보존한 명시 경계다. Load/Resume/Capture 자체로 새 후보를 만들지 않는다. 승격은 같은 reward ID로 한 번만 진행되며 이전 정책을 `PromotedFrom`에 기록한다. 예전 R03 전투 fingerprint를 일반적으로 무시하는 우회는 추가하지 않았다.
- `EquipmentInventory`의 Forms는 base ID·8레벨·대응 보조를 유지한 같은 슬롯의 lineage다. 적용/Restore가 lineage를 확인하고 기본 무기의 재획득·동일 진화 반복을 막는다. `CombatStats.Evolved`는 계산된 최대 기본 능력에 추가 효과만 적용한다. 미구현7무기를 신규 공격/진화 후보에 넣지 않는다.
- `BattleWorld`의 성경 연속 orbit/별도 파동·대상 ledger와 최소0.5초 간격, 턱뼈 기존 burst/추가 범위·실제1unit 넉백, 물맷돌 수량/간격/관통과 최대HP·ID 기준 volley focus/사망 재탐색을 확인했다. World copy/Restore는 form·pulse ledger·focus·잔여 shot/타이머를 보존한다. 실제 진화 form을 transient impact에 기록하고 기존 Cause는 유지한다.
- 보상 선택은 안정 reward/candidate ID와 현재 inventory를 다시 확인하고 preview로 적용 가능성을 검사한 뒤 receipt를 기록한다. 적용 후 기존 serial로 남은 성장 후보를 갱신한다. `CompleteBossSettlement`는 적용 receipt·성장 완료·미정산을 요구하고 완료50골드/해금/현재 최대HP 회복/between4를 한 번 처리한다. 보스100·선택100·일반킬 remainder·완료50의 경로를 분리하며 R04 시작은 거절한다.
- Boss version2·후보/선택/완료 receipt·Forms·World 진화 clock의 명시 shape와 phase/target 정합을 확인했다. 미래 form/정책/후보 kind/중첩 필드는 typed 읽기/옛 backup 복구 이전에 원문 보존 거절한다. SaveStore의 원자 교체·동일 operation 재시도 구현은 변경하지 않았다. R03 fingerprint만 새 정책/진화 tuning 계약을 결합하며 R1/R2 입력은 유지한다.

## 재사용한 실행 근거

`evidence/r03-reward-evolution-02/final-binding.json`과 진행 문서, 실제 XML/로그·변이 driver를 대조했다.

- 최종 `bash tools/dotnet.sh run --project tests/BibleSurvivor.Core.Tests -- --results evidence/r03-reward-evolution-02-final-tests`: exit0. XML은 **tests138/failures0**, failure node0이다. 새11개 시험은 후보·승격·유효 장비·fallback·중복/정산/실패한 저장 재시도·3진화 실제 명중 및 disk replay·미래 콘텐츠 보호를 다룬다.
- `evidence/r03-reward-mutations-20261115T151257009782Z/summary.json`: baseline/복원 exit0. 진화 보장 삭제, 완료 stage/receipt 무시, Restore focus 유실의 **3개 모두 실제 시험 실패 exit1**이다. 각 로그의 실패명과 driver의 원래 구조 변경을 확인했으며 컴파일 실패를 검출로 세지 않았다.
- 대표 `--boss-series sling 7`: exit0, `boss-series.log`는 보스킬1/지원6/예고 중 disk restore/동일 보상 재개/`sling_fivefold`/완료 receipt/between4/골드307을 기록한다. R1/R2 timeout·장비8/보조1·HP10000을 준비한 기술 fixture이며 R03 피해/처치는 주입하지 않는다.

이 리뷰에서는 기존138/18/10/30·CLI 또는 변이 행렬을 다시 실행하지 않았다. 새로 재현할 확정 의심이 없어 probe·전체 해시·캡처를 추가하지 않았다. 소스/시험/원래 구조 변이와 저장 재시도 근거를 중심으로 판단했으며 독립 재실행 통과로 표시하지 않는다.

## 한계와 인계

진화 실제 공격은 R04 경계를 넘지 않는 명시 R03 geometry fixture의 증거다. 정식 보상으로 획득한 뒤 between4에 도달하는 연속 CLI와 구분한다. 이 결과는 실제 플레이 밸런스나 R04 전투 수용이 아니다.

Unity 어댑터·DLL/provenance 동기화·GUI·실제 사용자 저장·모바일·실플레이/성능·아트 품질은 NOT_RUN이다. 병행 Bible sidecar 변경은 검토/수정하지 않았다. 제품·시험·도구·아트는 편집하지 않았고 이 리뷰 문서만 소유한다. 모델 사용량은 `r03-core-review-02` start/end 세션 기준점으로 기록하며 독립 worker 청구·비용/절감률은 계산하지 않는다.

순수 Core 실행02의 독립 코드 리뷰 게이트는 PASS다. 이후 Unity 연결·R04 확대는 각 단계 계약에 따른 별도 구현·검증·리뷰가 필요하다.
