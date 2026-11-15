# R03 Core slice 01 독립 코드 리뷰

2026-11-15. 기준 `d8a7802` 이후 미커밋 변경. **PASS — 첫 Core slice의 MEDIUM 1건 수정·재리뷰 완료, 열린 HIGH/MEDIUM 없음.** 정식 보상·진화·완료 정산은 승인된 다음 slice이므로 미구현 자체를 결함으로 세지 않았다.

## 발견 사항

### CLOSED MEDIUM — 보스 전투에서 구매한 초당 체력 회복이 사라짐

- 위치: `src/BibleSurvivor.Core/BattleWorld.cs:335`의 `StepBoss` 시간 진행, `src/BibleSurvivor.Core/AdamSession.cs:114`의 normal 전용 regen 처리.
- 원인: normal은 `AdamSession.Advance`에서 `regen rank × 0.1 × 진행시간`을 적용하지만, boss 분기는 `BossElapsed`만 직접 증가시키며 이 처리를 거치지 않는다. `docs/gold-shop.md:20` 및 `:24`는 이 업그레이드를 전투 중 초당 회복으로 정의한다. 보스 진입 때 금지한 완료 전량 회복과는 다른 기존 영구 효과다.
- 실제 재현: 유효한 영구 업그레이드 `health=2, armor=1, regen=1`로 R1/R2 timeout 후 R03에 진입했다. normal에서 피해 후 1초 진행하면 HP가 `0.09999999999999432` 증가했다. intro 2초 완료 후 피해를 받고 접촉·slam이 발생하지 않는 첫 boss 1초를 이동하면 boss elapsed는 `1.0000000000000013` 증가하지만 HP 증가는 `0`이었다. 최종 snapshot의 `Validate`도 통과했다.
- 실행 근거: 격리 probe `/var/folders/ly/4sm0t3qs4pgg3mpqcrrh7c580000gn/T/r03-review-regen-ustqpsgd/Probe.csproj`, `bash tools/dotnet.sh run --project <probe>` exit 0. 제품 파일은 읽기만 했고 출력·중간 빌드는 temp에만 생성했다. assertion 실패가 필요한 기존 변이 검증과 달리, 이 probe는 두 구간의 실제 HP 차이를 출력하는 재현이다.
- 최소 수정 조건: 실제로 진행한 boss tick에도 기존 영구 regen을 적용하고 최대 HP로 제한한다. pause·레벨 선택·reward 대기는 회복 시간을 만들지 않아야 한다. 완료 전량 회복이나 처치 이전 완료 보상은 추가하지 않는다. regen 보유 fixture의 normal/boss 진행 및 pause 경계를 확인하는 좁은 회귀 시험을 추가하고 해당 시험·재리뷰로 닫는다.

### 동일 리뷰어 재검토 — PASS

`AdamSession.cs:119`의 공유 `Regenerate`는 기존 식과 최대 HP cap을 유지하고, normal 호출은 원래 `Advance` 위치에서 실제 진행시간을 전달한다. `BattleWorld.cs:364`의 boss 호출은 intro return 이후, contact/slam 사망 return 이후에 tick당 한 번만 실행되며 반사·무기 공격 전에 적용된다. 따라서 죽은 플레이어를 재생으로 살리지 않는다. pause·레벨 선택·reward는 `Running`에서 tick을 차단하고 intro는 별도 return하여 재생하지 않는다. 완료 전량 회복이나 조기 골드/해금은 추가되지 않았다.

새 `BossTests.cs:36`의 시험은 유효 업그레이드로 normal/boss 1초 재생량, intro·pause·choice·reward 정지, 최대 HP cap 및 완료 골드/해금 보류를 검사한다. `evidence/r03-boss-regen-review/probe-before-after.json`의 수정 전 **11/12 FAIL(예상111.1/실제111)**과 수정 후 **12/12 PASS**, `summary.json` 및 `regen-call-removed.log`의 정확한 옛 구조(새 boss 호출만 삭제) **exit1 assertion 실패**, `restored.log`의 **12/12 PASS**를 대조했다. 근거에 결합된 수정 대상3개 파일 해시만 현재 소스와 한 번 비교해 모두 MATCH를 확인했다. 별도 전체 행렬·CLI·추가 변이는 재실행하지 않았다. 이 수정 범위에서 신규 HIGH/MEDIUM은 발견하지 못했으며 최초 발견 건을 닫는다.

## 검토 범위와 재사용 근거

AGENTS, 전용 Bible Survivor 하네스의 리뷰·검증 계약, `docs/harness/r03-implementation-01.md`, `r03-core-plan.md`의 phase/clock/보상 receipt/저장 경계, `docs/progress/R03-core.md`, `evidence/r03-boss-core-01/final-binding.json`과 연결된 결과를 읽었다. graphify 기존 그래프는 없으며 읽기 전용 리뷰 범위에서 새 그래프를 생성하지 않았다.

위 1건 외 다음 변경 경로에서 추가 HIGH/MEDIUM 결함은 발견하지 못했다.

- R2 대기→R3 normal, normal 완료 때 HP·완료 골드·해금 보류, 떨어진 XP만 정산, 선택/일시정지 동안 보호 intro 정지.
- 단조 공통 피해 시계와 별도 boss 공격 시계, 고정 target 예고/6초 slam 1회, 접촉과 slam의 공통 iframe, 피해·사망 우선 처리.
- 생존 50% 문턱 지원 6개 1회, 즉사 시 지원 없음, 지원 킬의 일반 remainder와 보스 고정100 분리, 남은 지원 정리의 무보상 처리.
- 보스 처치 receipt·안정 ID·장비/보조/진화 자격 보존과 `NeedsRewardImplementation` 대기. 정식 후보 생성·진화·정산/between4는 아직 수용 범위 밖이다.
- Boss 명시 shape 및 미래 버전/정책 거절, World v2와 clock/target ledger 정합, 중간 저장·복원, 같은 World의 phase 사건 이력 비우기와 실제 마지막 보스 처치 사건 유지.
- R03에만 추가한 boss 계약 fingerprint와 R1/R2 기존 입력 유지. 기존 호환성 결과와 대조했으며 전수 해시를 다시 계산하지 않았다.

기존 증거는 Core 최종 `126/126`, boss 원래 구조 변이 `4/4`(조기 Finish/threshold flag 유실/일반 remainder 합산/Restore clock0의 실제 assertion 실패), 기존 변이 `18/18`, schema 변이 `10/10`, 역사적 실제 binary fixture `30`개 동등·이행·구 reader 거절 PASS다. 최초 전체125 뒤 fingerprint 상수 결합/phase 사건 경계를 보강하고 최종126 및 boss 변이를 실행한 순서도 binding/진행 문서와 일치한다. 이 리뷰에서는 해당 행렬과 대표 CLI를 재실행하지 않았다.

## 미검증과 인계

Unity/DLL/provenance 동기화·GUI·실제 사용자 저장·모바일·플레이 밸런스·성능·아트 품질은 NOT_RUN이다. 대표 sling CLI의 장비8/HP10000 fixture는 기술 증거이며 실제 밸런스 통과가 아니다. 정식 보상/첫3진화/완료 정산은 NOT_IMPLEMENTED다. 이 리뷰는 실제 저장이나 R03 Unity 지원 완료를 뜻하지 않는다.

리뷰 산출물 외 제품·시험·도구·아트는 수정하지 않았다. `record_model_usage.py`의 최초 `r03-core-review`와 재리뷰 `r03-core-review-regen` start/end는 세션 구간 기록이며 worker별 독립 청구·요청 귀속과 비용/절감률은 계산하지 않았다. 첫 Core slice 독립 코드 리뷰는 PASS이며 후속 정식 보상·진화·정산은 별도 구현·검증·리뷰가 필요하다.
