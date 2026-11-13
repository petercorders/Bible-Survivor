# Unity 아담 연결 담당 작업

## 승인과 경로

사용자가 unity-adam-plan.md에 “너가 진행해줘”, 이어 “결제만 아니면 나한테 물어보지 말고 쭉쭉 진행해줘”라고 승인했다. 유료 작업 없이 구현·검증·수정까지 계속한다. Bible Survivor 전용 skill의 Unity·verification·ui-ux 참조를 읽고 적용한다. 기존 순수 코어와 그래픽은 독립 담당이 병행한다.

저장소 <repo>, 브랜치 feat/adam-core. UnityGame/ 프로젝트는 coordinator가 6000.6.9f1 2D 템플릿으로 생성했다. Editor 첫 import가 실행 중일 수 있으므로 같은 프로젝트를 동시에 열지 않는다. coordinator가 생성 프로세스 종료 후 작업을 맡긴다. Editor는 <Unity 6000.6.9f1 executable>. 생성 로그는 evidence/unity-setup-20261114/create.log. 라이선스 entitlement 인식은 확인됐으나 성공 종료를 별도 확인한다.

## 파일 소유

- 이 작업은 UnityGame/ 전체 씬·프리팹·runtime·editor·tests, Unity 전용 tools/unity_* 와 docs/progress/unity-adam.md, docs/retrospectives/R01-unity.md를 단독 소유한다. .gitignore의 Unity 관련 좁은 추가는 가능하다. 다른 변경은 보존한다. commit/push는 coordinator가 한다.
- src/BibleSurvivor.Core, 기존 tests와 tools의 R02 변경을 수정하지 않는다. R02 worker dispatch ctx_47c034b5d36a가 작업 중이다. 현재 API를 읽어 어댑터를 작성하되 API 변경이 필요하면 coordinator에게 알린다. 로컬 UPM package 신규 manifest/asmdef가 필요하면 coordinator에 인계하여 소유 충돌 없이 통합한다. 대안으로 검증된 controlled DLL 빌드/동기화 도구를 작성할 수 있다. 코어 원본을 수동 복사 유지하지 않는다. 라이선스/계정 값은 출력하지 않는다.
- 그래픽03은 별도 worker 소유이며 QA 전 임포트하지 않는다. 임시 도형과 글자로 게임을 동작시킨다. 최종 아트나 모바일 완료로 주장하지 않는다.

## 구현과 검증

unity-adam-plan.md대로 아담 한 라운드, 세 시작무기 선택, 공통 키보드/터치 이동 벡터, 자동 전투 표현, XP·최대3선택·HUD·정지·사망·완료·저장 재개를 연결한다. 기존 BattleWorld를 원본으로 두고 Unity 물리로 다른 전투 규칙을 만들지 않는다. content/genesis/catalog.json을 단일 데이터 원본으로 읽는다. 기본 템플릿의 불필요한 학습/협업 패키지는 근거를 기록하고 줄일 수 있으나 버전을 임의 최신화하지 않는다.

저장 정책은 기존 문서 결정과 대조한다. 미결정 주기는 무료·로컬 시제품 기본으로 5초 자동 체크포인트 및 선택/완료/사망/정지/포커스 상실 즉시 저장을 채택하고 문서에 결정 이유·최대 롤백 범위를 명시한다. session+world 동일 시점, 실패 시 전투 정지/재시도/재로드, operation ID 중복 방지를 지킨다. 앱 비활성에서 전투시간·입력을 멈추고 복귀 시간 누적을 버린다.

실제 Editor 컴파일과 EditMode/PlayMode를 실행한다. 새 증거 폴더/로그/XML/종료코드를 기록한다. 핵심 입력/정지/선택/사망/완료/저장 복구를 행동 시험으로 확인하고 테스트를 미러링하지 않는다. 변이는 별도 사본에서 기능 실패를 확인한다. 코드나 설정만으로 플레이 PASS를 기록하지 않는다. 가능하면 실제 PC 실행 화면과 입력 재현 증거를 확보하고 독립 UI/UX 리뷰용 경로를 남긴다. 불가능한 GUI/실기기 증거는 정확히 NOT_RUN으로 보고한다. 필요한 공식 API는 현재 설치 패키지 버전/공식 문서로 확인한다.

## 완료 인계

구현 파일, 실제 명령·종료코드·시험수·증거, 남은 실패, 독립 리뷰에 필요한 재현 절차를 보고한다. 구현자 자신의 리뷰를 독립 리뷰로 세지 않는다. coordinator가 별도 리뷰·수정 루프·최종 GC·커밋·push를 진행한다. 실제 측정되지 않은 성능과 모바일 지원은 NOT_RUN. 비용 발생 작업만 사용자 확인이 필요하며 일상 선택으로 작업을 멈추지 않는다.
