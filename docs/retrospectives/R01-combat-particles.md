# 기본 파티클 제품 연결 회고

2026-11-14. 전역 학습 상태/CI/hook은 바꾸지 않았으며 root가 독립 리뷰와 GC/commit/push를 맡는다.

- QA 실험의 arena를 제품에 복사하지 않고 실제 게임의 arena와 스프라이트 Point/PixelScale을 공통 구조로 뽑았다. 홀수 크기 RT는 늘려 맞추는 대신 정수 배율과 마지막 픽셀 클리핑을 사용했다.
- bounds0, 화면 aspect, 동일 batch 재소비의 원래 구조를 실제 제품 클래스에 각각 재현했다. 단순 alive 수가 아니라 실제 렌더 alpha·투영 오차·2→4 방출 assertion 실패를 보존했다.
- 첫 PlayMode에서 batchmode가 WaitForEndOfFrame을 지원하지 않는 것을 실제 실패로 확인했다. 일반 Player 캡처와 UnityTest의 실행 조건을 구별하고 player-loop 후 RT를 읽었다.
- 실제 키보드 시험도 Editor focus routing 설정이 없으면 누름이 전달되지 않는다. 기존 시험과 같은 입력 설정을 적용하고 finally에서 원복했다. 입력 실패를 제품 toggle 성공으로 기록하지 않았다.
- 캡처의 초기 리소스 이름 오류와 resize A/B 초기 화면 크기 불일치는 QA fixture 문제로 분리했다. 각 케이스 시작 해상도를 복구하고, pause/resize는 살아 있는 입자로 검증하도록 고쳤다.
- 성능 조건/임시 가드를 측정 전에 기록했다. 정확한40/120적의 고정 장면 표현 비용과 실제 진행 중 전체 게임 성능을 구별하고 측정 범위를 한정한다.

후보 제안은 화면 크기를 바꾸는 paired 캡처의 초기 화면·viewport·fixture hash를 함께 기록하는 검증 도구다. 이번에는 제안만 남기고 자동 전역 규칙 승격은 하지 않는다. 모델/토큰·캐시·대안 비용은 미측정이며 절감률을 주장하지 않는다.

측정 교정: 첫40적 장면은 명중 입자가 없고 Mono thread allocation 값은 전부0이었다. 이를 성능 PASS에서 제외하고 실제 hit target/켜기64입자와 유효한 Unity frame GC counter로 재측정했다. 기존 GUI 할당 약78/101KB와 추가36B를 분리하고 무할당/성능 개선으로 포장하지 않았다. GUI 전경 클릭 거절·Apple Events 권한 거절 후 QA PID만 AppKit으로 활성화했으며 다른 앱/권한을 바꾸지 않았다. 정상 종료 뒤 app_not_found를 복구 명분으로 새 실행하지 않았다.

저장 경계 교정: 동일 QA ID의 원장 밖 실행 때문에 stale revision이 발생했고, 도구 성공 JSON이나 종료 시점의 파일 원복만으로 독점 실행을 증명할 수 없었다. 과거 원복 판정은 INVALID로 남겼고, root 승인대로 새 suffix ID/제품/경로와 무저장 gate·검증된 입력 형식을 추가했다. 새 PID만 AppKit으로 활성화하고 restore-window를 사용하지 않았으며 두 실행의 실제 Host Load/Enter 시간 증가와 새 ID의 다른 writer 부재를 확인한 뒤 새 QA3만 원복했다. 과거 원장 밖 앱은 종료/추가 원복하지 않았다. native 스크린샷이 도구의 포커스 복귀로 다시 suspended가 되는 경우도 있어 실제 저장 시간/로드 상태와 함께 판정했다. 프로세스 생성 주체를 근거 없이 단정하지 않았으며 `native-action-chronology.json`에 가설과 관측을 분리했다.

CPT-01 후속: count0/Draw 생략은 RT camera 비활성화를 보장하지 않는다. Run 없는 Reload는 후속 Prepare도 건너뛰므로 Clear 자체의 수명 계약을 검증해야 했다. 실제 live burst→Prepare→Clear/World 없는 Reload 두 경로를 원래 Clear로 실패시키고, 최소 camera disable 후 같은 camera의 새 burst 재활성화까지 통과했다. QA JSON 파일 존재도 쓰기 완료를 보장하지 않아 첫 증거 수집은 중단됐으며 해당 Player exit를 수집하지 못한 시도는 성공으로 세지 않았다. 완료 JSON 파싱 대기와 새 기록으로 두 actual exit0·로드/원복을 다시 확인했다. 도구화 후보는 비동기 증거의 완료 표식/원자 쓰기와 리더 대기이며 전역 규칙은 자동 변경하지 않았다.

통합 GC: 같은 독립 코드 리뷰어가CPT-01의 최소 수정과 두 실제 회귀·정확한 원래 Clear 변이 실패→복원, 최종320소스/334앱 SHA·54Edit/20Play·새 QA 무저장 probe·Host Load·두 actual exit0를 대조해 CLOSED로 마무리했다. 최초 P1 PNG/성능과 CPT 수정 뒤 검사를 각각의 source/app 시점에 결속했고, 불확정 원복과 exit 미수집은 새 성공에 합산하지 않았다. 세션별 QA suffix/실경로·정확 PID/완료 JSON 대기는 실제 도구화·검사로 남기고 경고문·전역 규칙은 추가하지 않았다. PC 시각/조작 리뷰는 별도 단독 GUI 소유자가 수행한다.

독립 native 한계: 도구 호출의 포커스 복귀로 게임이 다시 suspended가 됐다. 입력 JSON 성공을 게임 시각 진행으로 세지 않았고, 이미 종료한 PID에는 restore를 사용하지 않았다. 살아 있는 정확 PID/창에서 승인된 전경 유지 한 번도 해결하지 못해 반복·권한 변경을 중단했다. 코드/엔진·저장 복구/정적 화면 범위와 native 연속 전투·입자/성장 미검증을 분리해 checkpoint로 남긴다. UI 제한을 숨기거나 사람 수용으로 바꾸지 않고 다음 독립 개발에서도 이월한다. 측정/검사에 기반한 모델·위임 기여만 기록하며 청감/시각 체감·모델별 총 token/cache/reasoning 비용은 미측정이다.

독립 UI 인계 종료: 최신 앱320소스/334파일을 전후 대조하고1100 시작/키2 선택·정지 모달과660 재실행/모달 가독성까지만 확인했다. active 전투·입자·성장·사망·resize는 NOT_RUN이다. 두 own Player actualexit0, 현재 suffix writer0, QA3 부재 원복/PC6 불변9/9를 확인했고 기존 unknown 프로세스/oldQA/iOS는 조회·조작하지 않았다. 보존 스크립트의 existed/exists 필드 오독은 읽기 오류로 정정 후 재검증했으며 앱 재실행이나 사용자 저장 원복을 하지 않았다. 통합은 한정된 구현 checkpoint로 남기며 전체 P1 native 수용 완료로 선언하지 않는다.
