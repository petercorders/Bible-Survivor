# R03 준비: schema2 저장 호환 slice

2026-11-14. R03 보스 구현에 앞선 **순수 Core 저장 형식 준비**다. 보스·지원군·진화·보상·Unity R03는 아직 구현하지 않았다. 현재 실행 앱의 Core DLL과 사용자 저장은 변경하지 않았다. 독립 코드 리뷰·DLL/provenance 동기화·최신 Unity 검증·GC 통합·커밋/push는 coordinator 후속 게이트다.

## 바뀐 동작

새 `GameSnapshot`과 실제 `SaveStore.Commit` writer는 schema2를 쓴다. 기존 schema1 R01/R02 active/between 저장은 기존 유효성 검사를 통과한 뒤 메모리에서만 schema2로 이행한다. `Load` 자체는 primary/backup/원문 보존 파일을 만들거나 바꾸지 않는다. R01/R02 fingerprint와 전투·장비·성장·골드의 의미는 유지한다. 이 slice의 schema2는 아직 R01/R02 필드만 지원하며 보스 필드·단계는 후속 구현에서 검증 계약과 함께 추가한다.

`SaveFormat`은 typed Run 역직렬화 전에 JSON의 정확히 한 개 양의 정수 `SchemaVersion`을 확인한다. 버전3 이상은 Run이 새 형식이거나 정수 범위를 넘더라도 미래 버전으로 거절하며, 옛 정상 backup으로 돌아가 덮어쓰지 않는다. 누락·중복·문자열·bool·소수/지수·0/음수 version은 손상으로 분류해 기존 backup 복구 정책을 적용한다. 알려진 envelope/Run의 중복은 손상으로 거절한다. schema1의 미지원 필드는 손상이며, schema2의 미지원 필드/단계·R03 이후 콘텐츠는 미래 콘텐츠로 보존 거절한다. 이 중간 reader가 후속 보스 저장을 옛 backup으로 덮어쓰지 않도록 하는 경계이며 보스 지원 자체는 아니다.

## 이전 원문 보존과 실패/재시도

최초 legacy source를 새 writer로 commit하기 전에 `save.json.pre-v2/`를 만든다. 실제로 존재한 `primary.json`/`backup.json`만 원문 그대로 복사하며, `manifest.json`에는 파일별 SHA256·검사 당시 schema·valid/invalid/future 분류를 기록한다. 손상 primary에서 유효 legacy backup을 복구하는 경우도 손상 원문을 그대로 보존한다. 파싱하지 못한 schema 또는 미래 schema의 숫자는 manifest에서0(미확정)일 수 있고 `Validity`로 구분한다.

파일들을 새 sibling 임시 디렉터리에 exclusive create·Flush한 뒤 원문 비교를 하고 디렉터리 rename으로 한 번에 게시한다. 보존 실패는 primary 교체 전에 commit을 중단하고 이 시도가 만든 임시 파일/디렉터리만 정리한다. 완성된 보존본은 다시 쓰지 않는다. 재시도는 기존 archive의 원문의 manifest SHA·분류 형식·정확한 파일 집합을 검증한다. 분류는 보존 당시 관측값이므로 후속 앱의 넓어진 reader로 다시 판정하지 않는다. 이후 정상 v2 primary나 뒤에 변경/손상된 primary를 처음 보존한 원문과 비교하지 않는다. 따라서 첫 v2 commit 직후 남은 v1 backup을 나중에 복구할 때도 정상 commit할 수 있다.

정상 v2 commit은 기존 보존본을 재생성하지 않는다. 기존 archive가 불완전하거나 원문/hash가 불일치하거나 분류 형식이 잘못되면 legacy upgrade는 중단한다. 자동 archive 삭제·덮어쓰기·전역 GC는 없다. 기존 OS lock·revision·operation ID payload 일치와 결과 불명 동일 operation 재시도 규칙을 유지한다. archive 게시 후 교체에 실패해도 원본은 남고 같은 operation으로 재시도할 수 있다.

## 검증 범위

최종 명령/종료코드와 source 해시는 ignored `evidence/r03-schema-20261114T023436314332Z/`의 최종 결속에 기록한다.

- 기존89 Core/18원래 구조 변이 baseline을 먼저 실행했다. 새 저장 회귀를 포함한 최종 Core/전체 CLI 및 기존18변이는 아래 결속 결과를 따른다.
- 새 `tools/check_schema_compat.py`는 로컬에 존재하는 pinned `2edd86e` 역사적 Core와 현재 Core를 각각 별도 프로세스로 빌드한다. 3무기×R01/R02 active/완료의30실제 저장은 SchemaVersion만 차이가 나며 Run/Profile/World/fingerprint 값은 같다. 옛30개는 새 reader에서 무쓰기 Load→원문 archive→v2 commit/retry, 새30개는 옛 실제 reader에서 옛 backup이 있어도 미래 버전으로 거절됨을 확인한다. 역사적 ref가 없는 환경은 실패하며 자동 fetch하지 않는다.
- 새 `tools/check_schema_mutations.py`는 격리 사본에서 원래 typed-before-header, schema1 writer, archive 누락/게시 실패 무시/기존 archive 무검증/뒤에 손상된 primary와 잘못 비교/미래 정수 overflow의 구체 회귀를 시험한다. 컴파일 오류·XML 없음은 검출로 세지 않는다.
- 실제 사용자 저장/앱/기기, Unity/IL2CPP의 새 codec·archive 실행과 모바일/성능은 NOT_RUN이다. 이전 Unity DLL은 그대로라 현재 소스와 provenance 재동기화가 필요하며 이 문서의 순수 Core 성공을 앱 지원으로 세지 않는다.

## 최종 순수 Core 결과

- `python3 tools/check_all.py`: exit0, Core106/106(기존89+저장17), 하위14명령 전부0. `evidence/r02-20261114T024634442975Z/checks.json`.
- `python3 tools/check_mutations.py`: exit0, 기존18/18 실제 시험 실패 검출. `evidence/r02-mutations-20261114T024637890713Z/summary.json`.
- `python3 tools/check_schema_mutations.py`: exit0, 새8/8 실제 시험 실패 검출·원복106/106. `evidence/r03-schema-mutations-20261114T024636735975Z/summary.json`.
- `python3 tools/check_schema_compat.py`: exit0, 실제 역사적 binary와30개 의미 동일/30개 migration/30개 옛 reader 미래버전 거절. `evidence/r03-schema-compat-20261114T024635586979Z/summary.json`.
- `python3 -m py_compile tools/check_schema_compat.py tools/check_schema_mutations.py` 및 `git diff --check`: exit0.

새 단위 시험은 기존 Core runner에 연결돼 기존 check_all에서 실행된다. 새 두 별도 driver는 아직 공유 CI/check_all 명령에 추가하지 않았다. 역사적 binary driver는 pinned ref가 로컬에 있어야 하므로 shallow CI에서는 별도 history 준비가 필요하다. 위 PASS는 독립 리뷰·Unity 새 codec/DLL 동기화·실기기 검증을 포함하지 않는다. 최종 소스·증거 경로와 종료코드 결속은 `evidence/r03-schema-20261114T023436314332Z/final-binding.json`에 있다.

## 독립 리뷰 R03S-01 수정·재검토 대기

독립 리뷰는 schema2의 `Profile.BossTokens` 또는 `Run.World.BossPosition`을 typed DTO가 무시해 Load→Commit에서 삭제하는 MEDIUM R03S-01을 확인했다. 앞 절의106개 결과는 이 수정 전 이력이며 최종 통합 승인으로 사용하지 않는다.

`SaveFormat`은 현재 schema2의 지원 DTO를 중첩까지 명시적으로 검사한다. Profile·Wave/Active·World/Tuning·적/드롭 좌표·투사체·무기/OrbitHits·속도 목록과 dictionary의 Key/Value entry에서 미지원 필드는 `UnsupportedSaveVersionException`으로 보존 거절한다. 옛 정상 backup이 있어도 fallback·Commit·archive 생성이 일어나지 않는다. 중첩 DTO의 중복 필드는 손상이며 기존 손상 복구 정책을 유지한다. 사전 안의 실제 무기/강화 ID는 DTO 필드 이름으로 검사하지 않는다. optional absent/null은 typed DTO와 기존 Validate에 맡긴다. schema1의 기존 중첩 extension 읽기 호환은 유지하고, migration 전에 원문을 archive로 보존한다.

이 지원 계약은 reflection 없이 명시했으며 Unity/IL2CPP의 새 실행 성공을 뜻하지 않는다. DTO에 새 필드를 정식 추가할 때 지원 계약도 함께 갱신해야 한다.

- 수정 전 새 회귀는112개 중3개 실제 assertion FAIL이었다. Profile/World 원래 두 입력과 대표 중첩 collection 입력이 예외 없이 읽히는 문제를 재현했다. 수정 후 `python3 tools/check_all.py`는 exit0, Core112/112·하위14명령 전부0이다. `evidence/r02-20261114T033248683554Z/checks.json`.
- 기존 `python3 tools/check_mutations.py`는 exit0·18/18 검출이다. `evidence/r02-mutations-20261114T033248684469Z/summary.json`.
- `python3 tools/check_schema_mutations.py`는 exit0·9/9 검출·원복112/112다. 새 `old_shallow_schema2_content_guard`는 원래 envelope/Run 직계 검사만 남기며 Profile·World·collection 세 회귀가 실제 assertion FAIL(exit1)로 바뀌었다. `evidence/r03-schema-mutations-20261114T033248685399Z/summary.json`.
- `python3 tools/check_schema_compat.py`는 exit0, pinned 실제 역사 binary와30개 의미 동등/30 migration/30 옛 reader 거절을 새 소스로 다시 확인했다. `evidence/r03-schema-compat-20261114T033248687227Z/summary.json`.

수정 소스/시험·명령/exit/XML·protected DLL 해시는 `evidence/r03-schema-nested-20261114/final-binding.json`에 결속한다. 같은 독립 리뷰어 재검토·최종 DLL/provenance sync/Unity 빌드·GC 통합·커밋/push·해당 SHA CI는 아직 후속 게이트다. coordinator는 schema driver 두 개와 역사 ref checkout을 CI에 연결했으나 원격 실행 PASS는 아직 없다. 실제 사용자 저장·앱·기기·Editor는 수정자가 실행하지 않았다.

## 독립 재검토 R03S-02 수정·재검토 대기

같은 리뷰어는 R03S-01을 닫고, `Run.World.SchemaVersion=2`를 기존 typed validation이 손상으로 취급해 Gold19의 primary를 Gold0의 legacy backup으로 대체할 수 있는 MEDIUM R03S-02를 확인했다. 현재 지원 World 버전은1이며 새 버전을 구현한 것이 아니다.

`SaveFormat`은 envelope와 별개인 World 버전을 typed 읽기·미지원 Run/Envelope 필드 검사보다 먼저 확인한다. envelope schema1/2 모두 World의 정확한 단일 양의 정수 버전이1보다 크거나 정수 범위를 넘으면 `UnsupportedSaveVersionException`으로 원본 보존 거절한다. 미래 World가 backup에만 있거나 손상 primary의 fallback 대상일 때도 같은 거절이며 Commit/archive를 시작하지 않는다. World 버전의 누락·중복·문자열·bool·null·0/음수·소수/지수 표현은 손상으로 분류하고 기존 backup 복구 정책을 따른다. typed int가 일부 값을 자동 변환하던 동작에 기대지 않고 버전 header를 명시적으로 검사한다. World absent/null 및 정상 World1·schema1 migration은 유지한다.

실제 저장 DTO의 명시 버전 필드는 envelope와 World 두 곳임을 읽기 확인했다. 별도 원문 archive의 `ArchiveVersion`은 기존 manifest 검증에서1만 허용하며 실패하면 원본 교체 전에 중단한다. 미래 archive 포맷을 읽거나 덮어쓰도록 넓히지 않았다.

- 새 회귀3개는 수정 전 실제 assertion FAIL(112/115)이었다. 수정 후 `python3 tools/check_all.py`는 exit0, Core115/115·14명령 전부0이다. `evidence/r02-20261114T040532634538Z/checks.json`.
- `python3 tools/check_mutations.py`는 exit0·기존18/18 검출이다. `evidence/r02-mutations-20261114T040532634708Z/summary.json`.
- `python3 tools/check_schema_mutations.py`는 exit0·10/10 검출·원복115/115다. 새 `future_world_version_recovers_legacy_backup`은 World 버전 선행 검사 호출만 제거한 정확한 옛 분기로, Gold19/0 원래 입력·overflow/새 typed 모양·잘못된 버전 세 회귀가 실제 assertion FAIL(exit1)로 바뀌었다. `evidence/r03-schema-mutations-20261114T040532634795Z/summary.json`.
- `python3 tools/check_schema_compat.py`는 exit0, 실제 역사 binary30개 의미 동등/30 migration/30 옛 reader 거절을 새 소스로 확인했다. `evidence/r03-schema-compat-20261114T040532636864Z/summary.json`.

최신 소스·명령/exit/XML·원본 DLL 불변은 `evidence/r03-schema-world-version-20261114/final-binding.json`에 결속한다. 같은 리뷰어 재검토 전 저장 slice 최종 PASS는 보류한다. Unity/DLL sync·실제 앱/사용자 저장·기기 검증과 commit/push는 이 수정 작업에서 실행하지 않았다.
