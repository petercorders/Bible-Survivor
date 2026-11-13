# 모바일 선행 게이트 실행 계획

후속 실행 상태(2026-11-14): iOS 모듈 설치와 지정 Simulator의 빌드·터치·저장 왕복 검증은 구현자 실행으로 완료됐으며, 현재 독립 코드/UI 리뷰 중이다. 상세 범위·실패 후 수정·최종 해시·미검증은 `../progress/mobile-ios.md`를 기준으로 한다. 아래의 미실행 표현은 최초 환경 감사 시점의 계획 기록이다. Android·실물·성능 및 전체 모바일 수용은 완료되지 않았다.

2026-11-14. 근거: `docs/mobile-platform-plan.md`, 전용 하네스 Unity/verification 참조, `docs/progress/mobile-readiness.md` 실제 환경 감사. 사용자 결제 외 자동 진행 승인은 유지한다. 이번 문서는 계획만 작성했고 설치·타깃 전환·게임 실행은 하지 않았다. root의 아트 커밋/CI 및 프로젝트 실행 소유가 끝난 뒤 다음 작업을 순차 실행한다.

## 범위·소유·정지 조건

- 아담 한 라운드/기존3무기를 iOS Simulator와 Android 에뮬레이터로 옮기는 기술 게이트다. 후속 콘텐츠/출시/계정/광고/구매를 추가하지 않는다. 무료 모듈·로컬 시뮬레이터 경로를 먼저 사용하고 비용이 발생하면 사용자에게 확인한다.
- 한 작성자가 모바일 설정·입력/화면 어댑터·빌드 진입점을 소유한다. 기존 Core·PNG/font 원본을 수정하지 않는다. 독립 코드/UI 리뷰는 별도 담당이다. 동일 Unity 프로젝트/Player/저장 경로에 둘 이상의 실행자를 두지 않는다.
- 단계별 실패는 해당 플랫폼의 정확한 오류/exit/log로 남기고 다른 플랫폼·문서·오프라인 검사를 계속한다. 결제·인증·실물 연결이 필요한 게이트만 멈추며 기존 승인된 무료 작업을 중단하지 않는다.

## M0: 모듈과 재현 가능한 빌드 경로

1. 현재 Editor6000.6.9f1을 유지한다. Unity Hub에서 iOS Build Support를 먼저 추가할 작업을 준비하고, Android Build Support와 Unity 관리 SDK/NDK/OpenJDK를 뒤이어 준비한다. 설치 전 공간/모듈 목적지·실제 다운로드 항목을 기록한다. 기존 전역 SDK/다른 앱의 NDK는 교체하지 않는다. 이 감사에서는 설치하지 않았다.
2. 설치 후 버전 상위 PlaybackEngines 실제 파일·모듈 의존을 다시 확인한다. Android는 NDKr27c/JDK17을 고정하며 SDK/Gradle 해석을 실제 빌드에서 확인한다. SDK를 발견했다는 사실만으로 호환 PASS를 부여하지 않는다.
3. Unity 모바일 타깃은 검토된 현재 커밋의 격리 작업 사본에서 먼저 생성한다. 단일 Editor 소유/원본 hash/GUID/폰트 고지/Core DLL provenance를 확인하고 자동 빌드 진입점은 BuildPipeline 결과가 실패면 비0을 반환한다. 생성 프로젝트·Library·SDK/기기 원문은 커밋하지 않는다.

## M1: iOS Simulator 우선 — 계정/실물 서명 분리

설치된 Xcode26.2/Simulator26.2를 이용한다. Unity iOS Simulator 타깃과 arm64 구조를 명시한 뒤 생성 Xcode 프로젝트를 출력한다. `xcodebuild -list`로 실제 scheme을 확인하고 Simulator SDK/지정된 QA destination/격리 DerivedData에서 signing-disabled 빌드를 시도한다. 이것은 실행할 구체 경로이며 아직 성공한 명령이 아니다. Unity는 Xcode 프로젝트를 만들고 Xcode가 앱을 빌드한다. [Unity iOS 빌드 과정](https://docs.unity.com/en-us/engine/6000.6/manual/platform-specific/iphone/ios-building-and-delivering/build-process).

이미 부팅된 Simulator는 다른 작업으로 간주한다. 별도의 QA simulator 인스턴스/앱 ID/저장 컨테이너를 기록하고 실행 소유를 확보한 뒤 사용한다. 기기 이름/UDID는 로컬 evidence에만 두며 공유 문서에는 타입·runtime만 쓴다. 최초 설치·시작/세 시작 무기·선택·정지·사망·완료와 저장 왕복을 확인한다. IL2CPP/DataContract/AOT/stripping, TTF atlas/한국어 글자 누락·정수 픽셀, PNG/셰이더를 실제 Player 로그로 검사한다. 계정·배포 서명은 이 시뮬레이터 게이트와 분리한다.

GUI 인계 보완: custom device set의 QA 기기는 headless 설치/렌더까지 가능했지만 기존 Simulator GUI가 다른 set을 사용해 새 GUI 프로세스가 종료되는 경우가 있었다. 이 경우 기본 set에 **새 QA 기기 UUID**를 추가하고 기존 Simulator 앱의 별도 QA 창에서 검증해도 된다. 격리 경계는 전용 기기·앱 ID·저장 컨테이너·명시 UUID/창 선택이며 GUI 프로세스를 반드시 분리하는 조건은 아니다. 기존 booted 기기/창/설치 앱/저장은 종료·전환·삭제하지 않는다. custom QA 기기를 정리할 때도 그 작업이 만든 UUID만 대상으로 한다.

실행 담당자의 후속 관측: 기본 set 새 QA 기기 생성/boot/install/launch가 각각 exit0이었고 전용 QA 창이 실제 열렸다. 기존 Simulator 프로세스·기기 창은 유지됐다. 따라서 GUI 선택 차단은 해결됐지만 native touch·레이아웃·중단/저장은 각 실제 검증 전까지 NOT_RUN이며 창 생성 성공에 합산하지 않는다. 식별자·창 ID 원문은 ignored evidence에만 보존한다.

## M2: Android 에뮬레이터 — SDK 존재와 빌드 지원 분리

Android Build Support 및 의존 확인 후 개발용 arm64 APK를 격리 출력한다. 실제 SDK/NDK/JDK 경로·버전, 사용 backend/ABI·API를 증거에 기록한다. 기존 API36 arm64 이미지/AVD1개를 활용할 수 있는지 먼저 잠금/사용 여부를 확인하며 다른 작업의 AVD를 부팅/종료하지 않는다. 필요하면 별도 QA AVD를 생성하는 후속 작업으로 분리한다. unsigned release/store 배포는 하지 않고 로컬 debug 설치만 검증한다.

설치 시점에만 명시적으로 adb server/QA emulator를 실행한다. 현재 읽기 감사는 adb server를 시작하지 않았으므로 연결 Android 실물의 유무는 미확인이다. 시작·입력·모달·중단·저장을 iOS와 같은 계약으로 검증하되 Android back/멀티윈도우·권한/오버레이 전환은 별도로 기록한다. 백그라운드 강제종료는 사망으로 처리하지 않는다.

## M3: 공통 화면·터치·중단 수용 조건

| 영역 | 실제 검증과 완료 조건 |
| --- | --- |
| 방향/크기 | 가로·세로 각각 notch/home inset 포함 QA profile로 실행, viewport/안전영역/정수 배율/물리 표시 크기를 기록. 고정620px 모달이 safeArea 밖으로 나가지 않고 카드3개/설명/재개 버튼이 보이고 눌려야 한다. 두 방향 비교 전 출시 방향을 확정하지 않는다. |
| 읽기/터치 | 실제 화면에서 이름·종류·현재→다음/최대·실효과를 비교할 수 있어야 한다. 고DPI의24px가 PC24px처럼 보인다고 가정하지 않는다. 터치 영역과 UI 크기를 기기 논리 크기/물리 화면으로 측정하고 플랫폼 공식 기준에 대조한다. |
| 입력 | 단일 손가락 이동·대각 정규화·손가락 교체·두 손가락·터치 cancel, 노치/홈 영역, 선택을 누른 손가락의 이동 전파/다음 선택 중복을 시험. 숫자키 없이 시작/성장/정지/재개/재시도가 가능해야 한다. |
| 중단 | 앱 전환/잠금/알림·권한창/화면회전/OS background 후 전투·연출 시간과 입력 상태를 확인. 복귀는 명시적 재개, 시간 점프/즉시 피격/자동 선택/held 입력 잔류가 없어야 한다. 실제 전화는 실기기에서만 판정. |
| 사망 | 실제 Core 접촉 사망에서6frame/0.75초 연출과 공통 숫자/터치 경계, 앱 중단 후 clip/terminal state를 검사. 원래 숫자/마우스 우회 변이를 격리 사본에서 재현해 행동 실패를 확인. |
| 저장 | 선택/완료/사망/중단 즉시 checkpoint와5.1초 공칭 롤백, 일시 실패 retry 동일 operation ID/reload 명시, 원본/backup 손상·미래schema 거절을 대상 저장소에서 확인. OS kill 직전/직후 재실행에서 골드·보상 중복/사망 오인 없이 복원되어야 한다. |

시뮬레이터의 합성 터치는 실물 멀티터치·손가락 가림·전화·발열·스토리지 내구성을 대체하지 않는다. 단계별 실제 명령/exit/새 XML/화면/저장/빌드 SHA를 보존하고 compile/build/input/UI/lifecycle/save를 플랫폼별로 나눈다. 미실행은 NOT_RUN, 모듈/기기 부재는 해당 게이트 BLOCKED다.

## M4: 실기기·성능 게이트

현재 알려진 iOS 휴대폰은 paired지만 연결 tunnel이 내려간 상태다. 다시 연결 가능한지·현재 개발 서명으로 설치 가능한지는 별도 실행 때 확인하며 계정/프로비저닝을 임의 변경하지 않는다. Android 실물은 미확인이다. 실물 확보 전에는 모바일 완료로 보고하지 않는다.

지원 기기 후보와 OS·해상도·품질·적 수·시드·시간을 먼저 명시하고 Development Player에서 CPU/GPU p95/p99, GC allocation, peak memory와 지속 발열을 측정한다. 목표 FPS/메모리 예산은 측정 후 결정 기록으로 고정한다. 시뮬레이터나 Editor 수치로 실물 성능을 합격 처리하지 않는다. 원본 전투 규칙을 품질 옵션으로 바꾸지 않는다.

각 플랫폼의 독립 코드/UI 리뷰→결함 수정→동일 담당 재검토→모바일 선행 GC→선별 커밋/push/해당 SHA CI 순서로 마무리한다. 전체 모바일/실물 게이트가 닫히기 전 다음 창세기 콘텐츠 확대는 승인된 독립 코어 준비와 플레이 완료를 명확히 구분한다. 이번 감사에서 설치·실행·검증 완료로 올린 단계는 없다.
