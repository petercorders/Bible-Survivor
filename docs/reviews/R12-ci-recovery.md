# R12 CI 복구 독립 리뷰

2026-11-18 KST. **PASS — 검토 범위에서 막는 HIGH/MEDIUM 결함 없음.** R12 SHA `9f711bf`의 Core CI `44124668801` 실패와 art CI `44124668750` 성공은 coordinator 인계를 기준으로 구분했다. 이번 검토는 `SaveSchemaTests.cs` 두 literal 변경과 선택 schema 변이 결과만 대상으로 하며 기존 R12 Core/Unity 코드 리뷰 PASS를 재사용한다.

## 원인과 최소 변경

R12에서 지원 World 상한이5가 되고, non-R12의 named World5 payload를 원문보호로 거절하는 별도 guard가 생겼다. 이전 시험 입력5는 이제 지원 상한을 넘는 미래 버전이 아니므로 `CheckWorldVersion` 호출을 제거한 mutant도 별도 World5 guard에서 거절됐다. 이 때문에 원래 상한 검사 제거 회귀가 가려졌다.

현재 diff는 `tests/BibleSurvivor.Core.Tests/SaveSchemaTests.cs:83`의 World5→6과`:89`의 버전 배열 첫 항목 문자열5→6뿐이다. 제품·검사기·required 목록·판정 조건은 바꾸지 않았다.6은 현재 지원 상한5보다 크며, World5 named guard를 통과하므로 `CheckWorldVersion`의 미래 상한 거절을 직접 요구한다.

- 첫 시험은 schema1/2 각각 Gold19 primary와 Gold0 legacy backup을 준비한다. 정상 reader는 Load/Commit을 UnsupportedSaveVersionException으로 거절하고 두 원문·archive/tmp 부재를 보존해야 한다. mutant는 typed corruption 복구로 빠져 옛 Gold0 backup을 읽으므로 같은 보존 계약을 충족하지 못한다.
- 두 번째 시험은 미래 World6 및 기존 거대 정수 overflow 입력, 비호환 Tuning/Wave 본문과 미지원 필드를 유지한다. 미래 World 거절이 typed 본문 해석보다 먼저 일어나야 하며 primary/backup뿐 아니라 backup 단독·corrupt primary 뒤 미래 backup도 보호한다. overflow 문자열과 이 assertion들은 변경되지 않았다.
- `tools/check_schema_mutations.py:42`의 선택 변이는 여전히 `CheckWorldVersion(...)` 호출만 제거한다. detected 조건은 actual exit1·XML 존재·기존 **두 필수 시험명 모두 실패**를 요구한다. required2를1로 줄이거나 다른 R12 시험 실패로 대신하지 않았다.

## 실행 근거 재사용

`evidence/r03-schema-mutations-20261117T163200538663Z/summary.json`의 실제 결과는 **PASS, baseline exit0 / mutant exit1 / restored exit0**, 세 단계 모두 XML 존재다. mutant의 failed_tests에 다음 두 필수 시험이 모두 들어 있으며 `detected=true`다.

- `Future World version preserves Gold19 primary instead of replacing it with Gold0 legacy backup`
- `Future World version and overflow precede incompatible typed bodies and protect future backups`

원로그의 두 실패는 모두 `Expected UnsupportedSaveVersionException` assertion 실패이며 compile 실패가 아니다. malformed World 및 R12 future 시험 실패2개는 부수 검출이며 필수2개의 대체 근거로 세지 않는다. 두 번째 mutant 시험은 배열 첫 World6에서 실패하므로 이번 선택 변이가 **overflow 하위 경우까지 별도로 검출했다**고 주장하지 않는다. overflow 경우는 기존 코드/입력을 유지한 baseline/restored PASS 범위이며, 미래 World6 회귀의 필수 검출은 이번 로그로 확인된다.

실행은 root가 격리 사본에서 수행한 선택 **1건**이다. 리뷰어는 전체 suite·새 변이·Unity/앱/GUI/사용자 저장을 실행하지 않았다. 복구 커밋과 원격 CI 결과는 후속 확인 범위이며 이 로컬 선택PASS를 원격 CI 재통과로 세지 않는다. 자연 완주·모바일·성능 미검증도 기존과 같다.

작성물은 이 문서뿐이며 제품/시험/타문서 편집·타인 변경 원복·새 worker/Claude/Astra·commit/push는 없다. 자기 usage `r12-ci-recovery-review` start/end는 세션 구간 기록이며 지연된 공급자 event를 포함할 수 있다. 실제 청구액·전용 작업 비용·절감률은 미계산이다.
