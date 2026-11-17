# R10 Core 회고

2026-11-17. 승인 범위의 Core 작업 회고이며 전역 규칙·entries·스킬은 변경하지 않았다.

| 분류 | 근거 | 처리·후보 |
| --- | --- | --- |
| 검증에서 발견 | `CurrentVersion=3`와 창세기기본2 분리 뒤 기존 테스트4개가 미래버전/메모리승격 가정으로 실패했다. | 실제 default schema를 대상으로 미래버전 치환, 미래 fixture4, 기존1→2기대값2로 최소수정했다. 전체176/176 PASS. 같은 회귀는 golden schema1/schema2 fixture로 고정했다. |
| 구현 중 오류 | 편집 스크립트의 넓은 치환이 Decode 승격 줄도 먼저 바꿔 후속 assert가 실패했다. | 멈춘 위치를 확인해 수정한 뒤 재개했다. 테스트에서 승격 범위를 확인했다. 전역 판단규칙 추가 대신 기존 assert 기반 편집을 유지한다. |
| 시험 fixture 오류 | BattleWorld.Advance에 180초를 한 번 전달해 상한30초 검사에 실패했다. | 기존 FixedStep 전투 루프에 맞춰 .1초 반복으로 고쳤다. 제품 규칙 변경 없음. |
| 도구화 후보 | 기존 schema compatibility 도구는 최신 reader를 역사 Core와 같이 컴파일한다. 역사 baseline에는 BossDefinition도 없어 최신reader 계약과 맞지 않는다. | 도구 자체는 미수정. 기존 검증 evidence의 대표 golden2개와 이전55b5833 별도 actual binary 보호검사로 이번 수용조건을 검증했다. 후속 도구 개선 후보는 baseline별 reader 또는 작은 전용 reader 분리. |
| 시험 하네스 재시도 | 임시 historical-reader probe에서 외부 executable project reference의 상대 Core 경로가 잘못 해석됐다. | 임시공간 Core 사본을 직접 참조하고 현재 tests assembly를 reflection으로 불러 사용했다. 결과 PASS; 제품 project 수정 없음. |

모델 적합성: 할당된 Sol로 Core 계약과 검증을 수행했다. 독립 리뷰는 부모 통합자에게 남겼다. 별도 worker는 생성하지 않았다. 모델 상향이나 Claude 호출은 없었다.

전체 토큰: `tools/record_model_usage.py`의 `exodus-r10-core` start/end checkpoint에 관측된 값만 기록한다. 이 작업의 subagent 전용 계량 여부는 도구가 제공하는 session ID 범위에 따르며 부모/다른 worker 중복을 성과로 합산하지 않는다. 실제 비용과 미제공 지표는 null/미측정이다.

위임 목적: Core 저장과 챕터전환을 Unity/아트 소유와 분리했다. 공개 API를 먼저 부모에게 보냈다. Unity/통합 파일은 편집하지 않았다.

추가 개선: 잘못된 fixture 호출과 구버전 project reference 준비로 재시도가 있었다. 다음에는 API 한도 및 역사빌드 최소 참조를 기존 도구에서 먼저 확인한다. 수용조건이 같은 비교실험이 없으므로 효율 향상이나 토큰 절감률을 주장하지 않는다. 최종 라운드 GC·독립 리뷰·commit/push는 통합자에게 인계한다.
