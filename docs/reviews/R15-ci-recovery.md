# R15 CI fixture 교정 독립 리뷰

- 검토일: 2026-11-18 KST
- 판정: **PASS — 한 줄 기대값 교정 범위에서 막는 HIGH/MEDIUM 없음.**
- 변경 소유: 이 보고서만. 제품·시험·검사기·저장·앱은 리뷰어가 수정하지 않았다.

## 실패와 변경

SHA `d81337f`의 Core CI `44150560639`는 첫 check_all에서 **210/211**, exit1이었다. `.local/r15-final-core-ci.log`의 실행 상태와 `gh run view 44150560639 --log-failed`를 읽었다. 원 실패는 `RoundFifteenTests.cs:58`의 `Inventory.Passives["seal"]` 기대값 평가에서 난 `KeyNotFoundException`이며, 실제 피해 비교 실패나 제품 예외가 아니다. 이번 Linux CI에서 seal 부재가 관측됐다는 사실까지만 확인한다. 플랫폼 자체가 원인이라고 단정하지 않는다. 기존 fixture의 선택·슬롯 경로는 seal 보유를 보장하지 않는다.

현재 diff는 해당 기대식의 dictionary 인덱싱만 `TryGetValue("seal", out var armor) ? armor : 0`으로 바꾼다. seal이 없으면 기대 피해는18, 있으면 실제 레벨을 뺀 `Math.Max(1, 18-level)`이다. 제품 `Damage`의 미보유 passive=0 처리와 일치하며 R12 상대 swept 시험도 같은 조회 방식을 사용한다. 이 fixture의 제품 수치·장비·슬롯 구성·충돌 준비를 바꾸거나 기대값을 관측 HP에서 역산하지 않는다.

raw18과 최소 피해1은 그대로이고 `Check.Near`의 비교 대상·허용 오차를 바꾸지 않았다. 같은 tick의 이동 완료, 적탄 소멸, iframe 차단 시 소멸, 사망 우선·boss kill 무지급·명시 R01 프로필 보존, kill cleanup assertion도 유지한다. 조건부로 시험을 건너뛰거나 예외를 삼키지 않으며 CI 성공/실패 판정도 변경하지 않는다. 따라서 seal 부재를 정상 방어0으로 해석하면서 충돌 회귀 검출 목적을 유지한다. 새 두 armor 조합의 별도 실행 증거가 생겼다고 주장하지 않는다.

## 재사용 근거와 남은 범위

`.local/r15-ci-fixture-related.log`는 교정 후 **관련8/8 PASS**를 기록한다. 리뷰는 이를 읽었으며 시험을 새로 실행하지 않았다. 기존 원 endpoint 변이의0/1/0과 실제 피해 assertion 실패, R15 Core/Unity/UI 독립 PASS는 재사용한다. 제품·DLL·catalog·Unity runtime 변경이 없어 게임 재빌드를 요구하는 변경은 아니다.

교정 후 새 SHA의 전체 CI 성공은 이 보고서 작성 시 확인하지 않았으며 기존 실패 CI를 PASS로 다시 분류하지 않는다. 전체 suite·새 변이·CLI·Editor·앱·GUI·해시 행렬을 반복하지 않았다. 자연 플레이·native/mobile·성능 등 기존 미검증 상태도 유지한다.

기존 사용량 도구에 자기 `r15-ci-recovery-review` start/end를 기록한다. 자료는 지연된 이전 tail을 포함할 수 있는 세션 구간이며 실제 청구액·전용 비용·절감률은 미계산이다.
