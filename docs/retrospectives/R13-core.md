# R13 Core 회고

2026-11-18 KST. 승인된 R13 Core 범위만 수행했다. 전역 학습 상태나 스킬을 자동 수정하지 않았다.

- **선행 실패/복구:** 첫 R12 CI의 미래 World 변이 미검출은 상한5가 지원 버전이 된 시험 입력 문제였고 R13으로 덮지 않고 실패로 반환했다. root의 입력6 수정·선택 변이·독립 리뷰와 복구 SHA의 두 CI 실제 성공 뒤에만 제품 작성을 시작했다. 이전 실패 로그는 `evidence/r13-core/r12-ci-core-failed.log`에 보존한다.
- **원래 구조 검출:** 보스 완료 후 공통 전환이 R03 receipt에 새 보스를 저장하던 경로를 R12에 적용하면 네 history가 깨진다. 명시 `CompletedFourthBoss`와 격리 원목적지 변이 baseline0/mutant1/restored0으로 이 경계를 확인했다. 새 lifecycle marker는 유지해 history 덮어쓰기 자체를 검출했다.
- **검증 효율:** R13 관련6, R12 관련10, R10~11 관련10과 최소 변이1건을 사용하고 필수 최종 check_all1회 안의 한 무기/seed 좌표 CLI를 대표로 사용한다. 새 실패나 제품 교정이 없으면 전체 조합/추가 fixture/Unity 빌드를 늘리지 않는다. 시간·총토큰 절감률을 추정하지 않는다.
- **도구/판단 구분:** 첫 usage 호출에 Orca 세션 ID를 넣어 해당 Codex 로그를 찾지 못했으나 실제 CODEX_THREAD_ID로 바로 기록했다. 재개 status 전송에서 미지원 type=message는 exit1이었고 지원 type=status로 교정했다. 잘못된 추정 glob은 zsh no matches로 중단되어 `rg --files`로 교정했다. 제품 편집 실패나 사용자 저장 변경은 없었다.
- **모델 적합성/위임:** observed `gpt-6.1-sol`, 단독 작성이며 Claude/Astra/helper를 새로 실행하지 않았다. effort는 별도 관측하지 않았다. 독립 리뷰와 Unity 통합은 root가 배정하며 자기 결과를 독립 PASS로 세지 않는다.
- **추가 개선 후보:** 지원 버전이 늘어날 때 미래 버전 시험 입력과 회귀 변이의 다른 방어 분기가 원래 검출을 가리지 않는지 확인하는 방식은 재사용할 수 있다. 이번에는 root가 두 입력만 고쳤으며 새 hook/검사기/전역 규칙을 설치하지 않는다. 미래 checkpoint 설계에도 receipt 목적지와 현재 World/과거 receipt 버전을 별도로 대조한다.

usage는 `evidence/model-usage/checkpoints.jsonl`의 자기 세션 구간만 기록하며 지연된 공급자 event와 보고 tail이 있을 수 있다. 실제 청구액/단가/요청별 귀속과 절감률은 미계산이며 누락을0으로 대체하지 않는다. 최종 결과와 실제 구간 수치는 진행 문서 및 인계에 남긴다.

최종 `check_all`은1회 exit0/197개 모두 PASS,15명령 모두 exit0이다. 그 안의 R13 CLI는 sling/seed7·hits7594/kills1059/boss0/180초,90좌표 샘플과 네receipt·between14를 확인했다. 자기 재개 phase provider interval은 input4214103/cache4182272/output19996/reasoning3674/total4234099이며 비용·절감률은 미계산이다. 원래 실패 phase의 별도 구간을 재개 구간에 합산하지 않았고 최종 문서/보고 tail을 제외한다. 독립 리뷰/Unity/실앱/기기/성능/자연 밸런스는 후속 담당의 미검증 범위다.
