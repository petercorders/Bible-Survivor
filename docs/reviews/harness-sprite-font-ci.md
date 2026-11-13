# Sprite Editor 허용 범위·폰트 CI 교정 독립 리뷰

2026-11-14 KST. **PASS — 이번 좁은 변경에서 추가 HIGH/MEDIUM 없음.** Core/아트 원본/Unity/tool 코드는 수정하지 않았고 이 보고서만 작성했다. Editor/Player/GUI와 전체 Core·폰트 검사는 실행하지 않았다.

## 선언된 어셈블리 경계

`unity_assemblies.py`는 네 어셈블리의 참조 목록을 정확히 비교한다. Sprite Editor 의존성은 `Adam.Editor`에만 추가되었고 Runtime 허용 목록은 `Unity.InputSystem`으로 유지된다. Editor/EditMode는 `includePlatforms == [Editor]`를 요구한다. 중복·누락된 어셈블리도 거절한다. strict/portable 두 실제 checker가 같은 함수를 호출하는 것을 읽기 확인했다. 설치된 `com.unity.2d.sprite`의 `Unity.2D.Sprite.Editor.asmdef`도 Editor 플랫폼 제한을 선언한다.

실제 독립 명령 `python3 -B tools/unity_test_assemblies.py`는 정상 입력과 금지 입력5종을 통과했다. 이전 독립 portable 격리 사본의 asmdef를 최종 선언으로 갱신한 뒤 실제 CLI로 다음을 실행했다. MSBuild·provenance는 그대로 사용했다.

| 입력 | `unity_check_portable.py --root <fixture>` 실제 결과 |
|---|---|
| 최종 정상 어셈블리 선언 | exit0 / PASS |
| Runtime 참조에 `Unity.2D.Sprite.Editor` 추가 | exit1; `Unexpected declared assembly references: Adam.Unity` |
| Editor의 `includePlatforms` 삭제 | exit1; `Editor assembly platform mismatch: Adam.Editor` |
| 두 입력 원상복구 | exit0 / PASS |

근거는 `<temporary>/bible-portable-review-8dob9cfm/sprite-boundary-review.json`이다. 이번에 strict checker의 전체 환경 검증을 재실행했다고 주장하지 않는다. 이 검사는 선언된 asmdef 경계이며 C#의 모든 완전수식 Editor API 사용을 분석하는 의미론적 검사는 아니다.

## 폰트 CI와 실패 진단

manifest 생성기는 `sys.version.split()[0]`를 `build.python`에 쓰며 저장된 값은 `3.11.6`이다. 재생성은 JSON 전체의 바이트를 비교하므로 Python 버전 문자열만 달라도 실패한다. coordinator의 `evidence/font-python-runtime-repro.json`은 실행 interpreter3.11.6에서 문자열만3.12.12로 바꾼 **시뮬레이션**이고, 그 외 manifest 필드가 같았다는 증거다. 실제 다른 interpreter 실행 증거나 기존 CI 실패 원인의 최종 확정과 구분한다.

최종 `art.yml`은 ubuntu24.04와 고정된 setup-python v6 SHA로 Python3.11.6을 요청하고 버전을 출력한 뒤 기본 아트/폰트 검사를 실행한다. manifest를 느슨하게 허용하거나 다시 써 차이를 숨기지 않는다. 공식 배포판 archive를 CI에서 다운로드하지 않으며 이전 ZIP SKIP/Unity NOT_RUN 범위는 유지된다. 원격 재실행 결과와 실제 런타임 확보는 coordinator의 후속 확인 사항이다.

`check_fonts.py`의 변경은 실패한 자식의 stdout/stderr를 로그 보존과 함께 부모의 stdout/stderr에도 출력한다. 기존 폰트 격리 사본에 새 wrapper를 복사하고 verify_all만 진단 문자열 출력+exit9로 대체했다. 실제 wrapper는 **exit1**, stdout/stderr 각각의 원문이 캡처된 출력에 모두 존재했다. 임시 스크립트는 원상복구했다. 근거는 `<temporary>/bible-font-review-d24bp6k3/failure-diagnostics-review.json`이다. 실제 CI 오류 문구를 합성한 시험이 아니라 실패 출력 전달 자체의 시험이다.

## 검증 경계

새 전체 suite·폰트 3분 변이·네트워크/설치·Unity 실행은 **NOT_RUN**이다. 원격 폰트 CI의 해결 여부는 재실행 전까지 미확정이다. Runtime Sprite Editor 유출과 Editor 플랫폼 제한 누락의 실제 CLI 거절, 자식 오류 출력/실패 코드 전파만 이번에 독립 실행했다. Unity importer/아트/폰트 화면 품질은 현재 실행 담당자의 별도 게이트다.
