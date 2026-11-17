# R12 Core 회고와 GC 후보

2026-11-18. 승인된 모세 일반전투·전차장 보스와 between13까지 단독 Sol 구현했다. root가 독립 리뷰·Unity·GC·commit/push를 조율하며 이 문서는 전역 학습 상태를 바꾸지 않는다.

- 판단/교정: 첫 ex9 fixture는 직접61초 전진해 동시120 상한에 막힌 상태에서 ex9 출현을 가정했다. 직접 kill로 출현 기회를 만든 뒤에는 좌표가 없는 기존 드롭 때문에 Restore ledger가 맞지 않았다. fixture의 드롭을 제거하고 실제 인벤토리/방어구 수치를 읽도록 수정했다. 최초·중간 실패 로그를 `evidence/r12-core/`에 보존했다. 후보는 일반 행동을 재사용할 때 시간만 넘기기 전에 동시 상한·출현 cursor·드롭 기하·현재 장비를 한 fixture에서 함께 확인하는 것이다.
- 구현 누락: D3 행동의 cooldown 이동 안에 남아 있던 `speeds["d3"]`를 대표 CLI가 실제 ex9 이동으로 검출했다. 라운드별 명시 키로 수정했고 같은 sling/seed7 CLI가 통과했다. 후보는 행동 재사용 시 ID selector뿐 아니라 함수 내부의 종족별 수치 참조도 확인하는 것이다.
- 경계 기대: 설치 끝을 R12로 옮기며 기존 R11의 `ContentComplete`와 미지원 R12 진입 기대를 R13 경계로 이동했다. 최초 관련9/10 exit1 이후 최종 전체191/191에서 R10/R11 회귀를 통과했다. 기존 R11 회고 후보와 같은 범주이며 별도 규칙을 중복 생성하지 않는다.
- 원회귀 근거: endpoint-only 접촉으로 회귀시키면 서로 움직이는 player/boss의 통과가 누락됐고, D3 돌진끝 cooldown을 보스에 사용하면 다음 공격이12→13.8초로 밀렸다. 격리 사본의 행동 assertion 실패1·1, baseline/restored0을 보존했다. 비슷한 변이를 같은 회귀로 세지 않았다.
- 모델 적합성: 현재 Sol로 저장/상대 swept/시계와 대표 검사를 해결했다. Astra/Claude/새 helper 호출은0이며 상향 필요 미해결 위험은 발견하지 않았다. 독립 리뷰와 Unity 검증은 이 구현 세션의 PASS로 대체하지 않는다.
- 토큰: `record_model_usage.py --phase r12-core --checkpoint start|end`를 실행했다. 도구가 보고한 session `01a1ea1b-b7f1-7f72-830c-0099ac2bb351`의 지연 interval만 관측되며 요청별 독립 귀속/worker 전체 청구량으로 합산하지 않는다. 실제 청구·단가·동일 수용 조건 baseline이 없으므로 비용·절감률은 미계산이다.
- 위임/효율: coordinator가 파일 allowlist를 정했고 worker는 단독 구현했다. R11 원격 CI를 편집 전 기다렸고, 대표10개와 실패 부분만 교정한 뒤 최종 전체 gate는1회 실행했다. 과거 이미지·모바일·전체 UI 조합은 반복하지 않았다. 후속 개선 후보는 기존 mutation 상한 anchor의 수동 인계를 기존 검사 체계 안에서 유지하는 것이며 새 검사 체계를 추가하지 않는다.

증거: `docs/progress/R12-core.md`, `evidence/r12-core-mutation/summary.json`, `evidence/r02-20261117T154050016334Z/checks.json`. 자연 완주·밸런스·Unity·모바일·성능은 미검증이다. 위 내용은 GC 제안이며 자동 승격·전역 파일 변경은 하지 않았다.
