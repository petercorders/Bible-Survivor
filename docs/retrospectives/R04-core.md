# R04 Core 회고

2026-11-16. 승인된 R04 checkpoint를 단일 Core 작성자가 구현했다. R03 완료 receipt를 별도 보존하고 전투 시계를 초기화했으며, 첫3진화/장비·성장을 유지해 R04에서 실제 사용하는 연속 CLI를 만들었다. 완료는 성장 선택 뒤 한 번 정산하도록 R04의 작은 settlement 경계를 사용했다.

D3는 기존 chase에 이름만 붙이지 않고 예고/고정 방향/직선 이동/종료부터 재사용을 상태와 geometry에 연결했다. phase 경계가 fixed tick 중간에 있어도 남은 시간만 이동시키고, spawn clock과 World3 named charge snapshot을 저장했다. 저장에서 timer/목표를 버리는 옛 구조를 실제 변이로 재현했다.

검증은 관련7시험·대표 sling1seed CLI·최소2원구조 변이로 끝냈다. 기존138/18/10/30 행렬과 전조합은 반복하지 않았고 새 도구/보조 프로젝트·Unity 재빌드는 만들지 않았다. R04 강한 진화 fixture가600마리를 빨리 처치해 D3 예고를 관측하지 못하는 정상 동작을 CLI의 잘못된 실패 조건으로 확인했다. 조건을 cooldown 저장/재개와 실제 처치/완료로 좁히고, 실제 예고/돌진/해당 저장은 geometry 시험의 독립 근거로 남겼다. 이 기술 fixture를 밸런스 PASS로 보고하지 않았다.

설정 단계의 생성 전 D3 조회·fixed step 불일치·공통 무적에 가로막힌 주입 치명 피해는 fixture만 수정했다. 제품 규칙을 완화하지 않았다. 기존 해금 변이가 새 함수와 중복돼 실패했던 경험을 적용해 기존18개 anchor의 고유 위치만 정적으로 확인했다. 추가 전역 GC/하네스 수정은 없다.

근거는 `evidence/r04-core-01/{summary.json,restored.log,cli-result.json}`이며 진행/미검증은 `docs/progress/R04-core.md`다. 독립 Core 리뷰와 실제 PC 앱 연결이 남아 있다. Core 작성 범위 외 Unity/DLL/provenance·아트·사용자 저장·GUI·CI hooks·commit/push는 실행하지 않았다. Sol 사용 및 phase `r04-core-01` 시작/끝 실제 token counters를 기록하고 청구액/비용 절감은 추정하지 않는다.
