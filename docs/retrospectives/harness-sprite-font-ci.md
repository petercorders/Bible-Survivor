# Sprite Editor 경계·폰트 CI 교정 GC

2026-11-14. 실제 아트 임포터가 설치된 Sprite 패키지의 `ISpriteEditorDataProvider`를 사용하므로 `Adam.Editor`에 `Unity.2D.Sprite.Editor` 참조가 필요해졌다. 패키지 asmdef와 소비자 코드, Editor 전용 플랫폼을 확인했다. 기존 고정 허용 목록은 정상 추가를 거절했으며 이 거절 자체는 검사가 느슨해진 결함이 아니다.

`tools/unity_assemblies.py`로 선언 경계를 공유하고 두 검사기에서 같은 좁은 예외를 사용한다. Runtime은 이 참조를 허용하지 않는다. 정상 스냅샷의 strict/portable checker0과 Editor 전용 참조의 정상 기준선, Runtime 유출·Editor의 Player 노출·다른 미승인 의존성·Runtime editor import·중복 어셈블리5오류 거절을 확인했다. 이 선언 검사는 C# 의미론/전체 Unity 컴파일을 대체하지 않는다. 게임 아트 worker의 실제 compile/Edit39/Play9/build0은 별도 작업자 증거다.

폰트 커밋 `f0d4368`의 [core CI](https://github.com/petercorders/Bible-Survivor/actions/runs/43467777194)는 성공했지만 [아트/폰트 CI](https://github.com/petercorders/Bible-Survivor/actions/runs/43467777195)는 폰트 재생성 단계에서 실패했다. 기존 wrapper는 내부 로그를 ignored evidence에만 보존해 원격 실패 원인이 출력되지 않았다. 이제 실패 때 자식 출력도 CI 로그에 표시한다.

폰트 manifest의 `build.python`은 생성기의 실제 Python 버전이다. 격리 실험에서 이 런타임 메타데이터 문자열을3.11.6→3.12.12로 바꾸면 다른 모든 manifest 필드가 같아도 byte 비교가 실패하는 것을 확인했다(`evidence/font-python-runtime-repro.json`). 이 실험은3.11.6 interpreter 안에서 버전 문자열만 바꾼 것이며 다른 interpreter/Linux 실행을 했다는 주장은 아니다. 기존 실패가 그 원인이었는지는 교정 후 실제 원격 CI 결과로 확인한다.

아트 CI는 원본 생성 환경의 Python3.11.6과 지원되는 Ubuntu24.04로 고정했다. [setup-python 공식 안내](https://github.com/actions/setup-python#basic-usage)와 공식 python-versions manifest에서 해당 Linux24.04/x64 배포를 확인하고 setup-python v6를 검증한 SHA로 고정했다. 검수된 export/폰트 바이트를 CI 통과 목적으로 고치지 않았다. 나중에 생성기 버전을 바꾸면 생성 메타데이터/출처·재생성 근거를 함께 갱신하고 검토해야 한다.

도구 개선으로 처리하고 전역 GC 규칙은 추가하지 않는다. 실패 원문 누락과 실행 환경 미고정은 이번 실제 원격 실패의 근거로 남긴다. 격리 실험의 첫 argv 구성 오류는 인자를 별도 변수에 보존한 뒤 수정했고 성공한 재현만 결과로 기록했다. 실제 토큰/비용·절감률은 미측정이다. 후속 독립 검토와 원격 CI 확인 후 완료를 판정한다.

[독립 교정 리뷰](../reviews/harness-sprite-font-ci.md)는 추가 HIGH/MEDIUM 없이 통과했다. 실제 격리 portable CLI는 정상0→Runtime Sprite Editor 유출1→Editor 플랫폼 제한 삭제1→복원0을 확인했다. 별도 실패 자식의 stdout/stderr와 exit9가 wrapper 원문 출력/exit1로 전파되는 것도 확인했다. 전체 Editor/원격 CI를 이 독립 리뷰가 실행한 것으로 합산하지 않는다. 교정 커밋 후 실제 CI 결과는 별도로 확인한다.

교정 커밋 `d12311b85a50ed6168bc8a029c155bd45edde576`을 원격 브랜치에서 확인했다. [코어·동기화·어셈블리 경계 CI](https://github.com/petercorders/Bible-Survivor/actions/runs/43470940690)와 [아트·폰트 CI](https://github.com/petercorders/Bible-Survivor/actions/runs/43470940666)가 모두 성공했다. 원격에서 고정 Python 설치와 폰트 재생성/오류 입력을 실제 통과했으며 검수 폰트/manifest 바이트는 그대로다. 실패 이력은 유지하고 교정 후 해당 SHA의 성공으로 닫는다. 현재 진행 중인 실제 아트/폰트 Unity 지원은 이 CI 결과와 별개다.
