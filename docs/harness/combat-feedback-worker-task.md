# 아담 타격감 P0 작업

2026-11-14. 사용자 실제 플레이 피드백과 `../combat-feel-plan.md`의 P0를 적용한다. 기존 지속 개발·멀티에이전트·PC 리뷰·GC/commit/push 승인을 유지한다. 제품 구현은 이 작업의 단일 작성자가 소유하며 root는 독립 리뷰/통합을 맡는다. 사용자가 플레이 중인 iOS 앱·기기·저장은 조회·조작·종료·교체·원복하지 않는다.

## 범위

- 세 시작 무기의 명중 전달 정확성, 피격 강조·짧은 표현 변형, 무기별 명중음 및 선택적 음량/효과 강도까지만 우선한다. 전체 화면 흔들림/실제 hit-stop/전투 넉백/치명타/밸런스/새 라운드는 추가하지 않는다.
- 한 방 처치의 스파크 누락, 여러 명중의 합침, 살아 있는 적에만 차분 효과를 만드는 현재 구조를 먼저 재현한다. 필요 시 Core에 저장되지 않는 bounded 일시 명중 사건 전달을 추가한다. 사건은 실제 Hit 지점/무기ID/적ID/충돌 위치/실제 피해/처치를 포함하고 snapshot/저장 schema/게임 난수/HP·시간·XP·골드 결과를 바꾸지 않는다. retaliation도 별도 원인으로 구분한다. 순번/소비자는 World별이며 로드 시 과거 명중을 재생하지 않는다. 버퍼 포화 때의 전달 정책도 명시한다.
- 기존 검수 Art03 PNG·font·원본 manifest/GUID는 그대로 쓴다. 피격 tint·소폭 sprite 눌림/복원은 표현에만 적용하고 발 pivot/충돌/HUD/hit rect는 고정한다. 같은 Core tick의 명중→처치를 정확히 표시하고 일반 명중과 치명 명중의 중복 재생을 막는다. 새 그림이 필요하면 `../art/combat-fx-brief.md`로 요청하되 Design 제품 미연결을 다른 worker/절차 그림 생성으로 숨기지 않는다.
- 오디오는 외부 유료/출처 불명 샘플을 쓰지 않는다. 기존 프로젝트에 사용권 확인된 소리가 없으면 짧은 원본 절차적 PCM 초안을 별도 소스/생성 규칙과 함께 만들 수 있으나 상용 음질/Claude Design 제작이라고 보고하지 않는다. Unity AudioSource 재사용/최대 동시 발음·짧은 재생 제한·별도 음량/음소거 경로, pause/background 때 재생 중단을 고려한다. pitch 등 표현 난수는 Core와 분리한다. Core 이벤트가 확인된 경우만 명중음을 낸다. 시청각 실제 PC 검토가 안 된 부분은 NOT_RUN이다.
- 효과64풀 포화에서 장식부터 생략하고 플레이어 피격 단서는 보존한다. OnGUI Layout/Repaint에서 사건을 생성/소비하지 않는다. 정지/성장/사망/새도전/Reload에 따른 중복·오래된 효과·사운드의 잔류를 검증한다.

## 소유·검증

Core·관련 tests/Sim·Unity Runtime/관련 시험·필요 Editor evidence 진입점만 수정 가능하다. 기존 CI/checker/hook/전역규칙과 원본 아트·폰트는 수정하지 않는다. Core DLL은 원본 sync 도구로만 갱신하고 schema2 provenance/local/portable checker를 통과해야 한다. Android/iOS 기존 앱·격리 증거는 덮어쓰지 않는다. PC 실행은 단일 소유로 격리 QA 저장/앱을 사용하며 사용자 저장 보존을 증명한다.

전후 동일seed/입력의 전체 Core snapshot·저장 결과 동등, 실제 단발 치명/복수명중/관통/반사 피해 사건 정합, 로드 직후 중복 없음, 풀/음성 제한/음소거·저효과/중단을 관련 시험으로 확인한다. 원래 누락/합침/중복 구조 변이를 별도 사본에서 재현해 assertion 실패→원복PASS를 기록한다. 기존 Core84/변이18·Unity42Edit/15Play를 후퇴시키지 말고 관련 검사와 실제 macOS 빌드/3무기 A/B 영상/실제소리 WAV 근거를 남긴다. 전투 적40/120 표현 성능은 측정 조건/Frame time·할당을 실제 측정한 경우에만 기록한다. 수용 예산은 미확정이며 실물 성능 PASS는 주장하지 않는다.

`docs/progress/combat-feedback.md`, `docs/retrospectives/R01-combat-feedback.md`와 ignored `evidence/combat-feedback-20261114/`에 실제 명령/exit/XML/전후A/B/오디오·소스/앱SHA/기존 저장·아트 보존/미실행을 기록한다. private 경로/식별자는 공개 문서에서 제거한다. 독립 PC 코드/시청각 리뷰·수정/재리뷰·통합 GC·commit/push/해당 SHA CI는 root 소유이며 worker는 commit/push하지 않는다. 추가 worker 생성하지 않는다. 외부 도구/기기 부재는 해당 게이트만 보고하고 독립 코드 작업은 계속한다.
