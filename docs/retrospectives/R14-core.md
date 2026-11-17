# R14 Core 회고

2026-11-18 KST. 승인된 R14 Core 범위만 수행하고 전역 학습 상태·스킬·검사기를 자동 변경하지 않았다.

- **선행 게이트:** 정확 R13 SHA의 두 run을 실제 조회하고 watch exit0/completed success 확인 뒤 제품을 작성했다. CI 대기에 제품 수정을 섞지 않았고 run ID를 추정하지 않았다.
- **원래 구조 검출:** R07/g3 전용 좌우 조건을 재사용하되 R14/ex13을 명시 추가했다. 원래 R07 전용 조건으로 되돌린 최소 변이는 실제 x경계 assertion으로 실패했고 baseline0/mutant1/restored0이었다. generic random-edge를 테스트 안에서 가짜로 대체하지 않았다.
- **저장/보상:** 새 schema나 receipt 필드를 늘리지 않고 root3/World3와 네 과거 receipt의 적용 범위만 좁게 늘렸다. 성장25/완료260·회복을 분리하고 pending/후보/실제 Choose를 각각 기록했다. disk Load·저장 실패 retry·미래 primary/backup 보호는 기존 fixture/SaveStore 경로를 재사용했다.
- **검증 효율:** R14관련6/R13관련6/R07관련3·최소 변이1건 및 필수 check_all1회 안의 한 무기/seed 좌표 CLI를 사용한다. 예기치 않은 제품 실패가 없으면 전체 조합이나 새 fixture 체계를 늘리지 않는다. 문서로 앱 빌드를 반복하지 않는다.
- **모델/위임:** requested Sol 기본/observed gpt-6.1-sol, 단독 작성이며 Claude/Astra/helper를 새로 실행하지 않았다. effort와 요청별 비용 귀속은 관측하지 않았다. 독립 리뷰와 Unity 통합은 root가 배정하며 자체 검사로 독립 PASS를 주장하지 않는다.
- **추가 개선 제안:** 종별 생성 순번이 실제 좌표 계약에 연결되는 경우 새 콘텐츠의 ID를 명시하고 원래 미연결 경로 변이를 한 번 확인하는 방식은 재사용할 수 있다. 이번에는 새 hook/도구/전역 규칙을 설치하지 않는다. 기존 root Resume 상한 변이는 작성자가 직접 바꾸지 않고 소유자에게 전달했다.

usage는 자기 실제 Codex 세션의 R14-core start/end provider interval만 기록한다. 지연된 event·CI 대기·보고가 포함될 수 있으며 시작 호출의 이전 phase delta는 합산하지 않는다. 실제 청구액·단가·비용/절감률은 미계산이고 누락을0으로 세지 않는다. 최종 실행 결과와 구간 수치는 진행 문서에 남긴다.

최종 check_all은1회 exit0/203개 모두 PASS/15명령 exit0이다. 그 안의 R14 CLI sling/seed7은10971hits/973kills/boss0/180초·90좌표·네receipt·between15를 확인했다. 자기 provider interval은 input4410110/cache4384384/output18342/reasoning3815/total4428452이며 최종 문서/보고 tail은 제외한다. 미검증은 독립 리뷰·Unity/실앱/native/모바일·성능·자연 밸런스로 남기며 자체 검사로 확대하지 않는다.
