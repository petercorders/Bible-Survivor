# R03 schema slice 프로젝트 GC

2026-11-14. 전역 entries/index/overview는 수정하지 않았다.

- 이전 구조의 실제 결함: typed Run을 먼저 읽으면 미래 schema의 새 Run 모양이 역직렬화 오류로 처리되어 옛 backup으로 돌아갈 수 있다. header-first probe와 `Future schema header precedes incompatible typed Run` 회귀를 도구화했다. 원래 읽기 순서를 격리 변이로 되돌려 검증한다.
- 보존 원칙: archive를 파일별로 바로 게시하지 않고 임시 디렉터리 완성→원문 검증→rename으로 게시한다. 중간 보존 실패·원본 불변·임시 정리·동일 operation 재시도를 시험한다. 보존과 평상시 `.bak` 복구 수명을 분리한다.
- 자체 검토 중 교정: 기존 archive를 현재 원본과 비교하는 첫 구현은 최초 v2 commit 뒤 남은 v1 backup으로 복구할 때, 뒤에 손상된 primary를 이유로 정상 복구 commit까지 막을 수 있었다. 완성된 archive는 자체 원문의 manifest SHA·분류 형식을 검증하고 최초 원본으로 유지하도록 수정했다. `Schema2 corrupt primary can recover its schema1 backup` 시험과 정확한 첫 구현 변이를 남긴다. 이 수정 전 결과를 최종 결과로 재사용하지 않는다.
- 중간 버전 경계: schema2 writer만 먼저 존재하는 이 slice도 후속 schema2 보스 필드/단계·R03 콘텐츠를 손상으로 fallback하면 안 된다. 미지원 콘텐츠 보존 거절을 추가하고 원래 fallback 구조 변이를 따로 검출한다.
- 시험 드라이버 교정: 첫 새 변이 실행은 부분 문자열 anchor가 두 위치에 일치해 실행 전 중단됐다. 줄 경계 anchor로 좁혔다. 이 중단은 회귀 검출이나 PASS가 아니며 새 출력에서 재실행했다.
- 보존본은 사용자 데이터다. 자동 GC 삭제 대상이 아니며 회고를 이유로 정리하거나 덮어쓰지 않는다. 새 parser/metadata의 Unity IL2CPP 지원은 별도 미검증으로 남긴다.
- 모델/토큰 usage는 이 작업에서 수집되지 않았다. 추정 절감률을 보고하지 않는다.

최종 순수 Core106/106·기존18변이·새8변이·역사적30저장 양방향 검증이 각각 새 출력에서 통과했다. 구체 경로는 `docs/progress/r03-schema.md`와 ignored 최종 결속을 따른다. 독립 리뷰·Unity codec/IL2CPP·공유 CI 연결은 아직 후속 게이트다.

## 독립 리뷰에서 확인한 중첩 계약 누락

- R03S-01은 envelope/Run 직계 필드만 닫고 중첩 DTO는 typed serializer의 무시 동작에 맡긴 판단 누락이다. 미지원 `Profile.BossTokens`·`Run.World.BossPosition`이 같은 schema2에서 읽힌 뒤 삭제되는 두 원래 입력을 회귀로 추가했다. 수정 전 실제3 assertion FAIL→수정112/112 PASS를 확인했다.
- 도구화: schema2의 지원 DTO shape를 reflection 없이 중첩 검사하고 미지원 필드는 보존 거절한다. dictionary Key/Value entry와 실제 게임 ID를 분리하고, optional absent/null 및 schema1 기존 읽기 호환은 유지한다. duplicate field는 손상 정책에 남긴다.
- 원래 얕은 검사 구조를 격리 사본에서 되살린 새 변이는 Profile·World·collection 세 회귀에서 실제 exit1/XML assertion FAIL이며 원복112/112다. 기존18/새9변이·실제 역사적30저장 호환도 새 소스로 재실행했다. 숫자를 이전106/8 결과에 합산하지 않는다.
- 후속: DTO 필드 확장 시 명시 지원 shape를 함께 갱신해야 한다. 같은 독립 리뷰와 Unity/DLL 연결 전에는 저장 준비 완료를 앱 지원으로 승격하지 않는다. 전역 규칙/메모리 자동 승격·사용자 저장 정리는 하지 않았다. 모델·토큰 usage는 미측정이다.

## 중첩 버전도 독립적인 보존 경계

- R03S-02는 중첩 필드 이름을 닫는 것으로 버전 경계까지 보호했다고 본 누락이다. 알려진 `World.SchemaVersion`의 미래 값은 기존 typed Validate에서 손상 예외로 분류돼 정상 옛 backup으로 돌아갔다. Gold19/World2 primary와 Gold0/World1 legacy backup의 원래 입력을 시험했다.
- 도구화: World는 envelope와 별도 version domain으로 선행 검사한다. schema1/2 모두 미래 양의 정수·overflow는 보존 거절하며, malformed/duplicate 값은 손상 복구 정책을 유지한다. 현재 World1 지원이나 nullable/legacy 읽기 의미는 바꾸지 않는다. 다른 명시 version 필드와 archive의 독립 무덮어쓰기 검증도 읽기 대조했다.
- 수정 전3 assertion FAIL→수정115/115 PASS, 정확한 World 버전 검사 호출 제거 변이의 같은3 assertion FAIL→원복115 PASS를 확인했다. 기존18/새10변이와 실제 역사 binary30저장 호환도 새 소스로 실행했다. 112/9 또는106/8의 이전 결과를 새 결과에 합산하지 않는다.
- 같은 리뷰어 재검토와 Unity/DLL 연결은 후속이다. 보존용 archive를 GC 대상으로 삭제하거나 전역 하네스 규칙을 자동 승격하지 않았다. 모델/토큰 usage는 미측정이다.
