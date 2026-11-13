# 모바일 환경 준비 상태 — 읽기 전용 감사

2026-11-14. 범위: 설치 환경·기존 모바일 설계·현재 소스 읽기. Unity/게임/Simulator/에뮬레이터 실행, 설치·계정·서명·pairing 변경, 소스 수정은 하지 않았다. 사용자 PC+Android/iOS 앱 요구와 결제 외 자동 진행 승인은 유지한다. **환경 조회 PASS, 모바일 게임 빌드·실행은 NOT_RUN**이다.

## 실제 환경

| 대상 | 확인 사실 | 다음 실행 가능성 / 제한 |
| --- | --- | --- |
| Unity6000.6.9f1 | Editor 버전 폴더 바로 아래 PlaybackEngines에 MacStandaloneSupport/WebGLSupport만 존재. modules.json도 Android/iOS selected=false. | 현재 Editor에는 두 모바일 모듈이 없다. `.app/Contents`만 확인한 결론이 아니다. Unity 모바일 빌드는 모듈 추가 전 BLOCKED. |
| Xcode | Xcode26.2/build17C52, 선택된 개발 도구 정상. iOS26.2 및 iOS Simulator26.2 SDK가 조회된다. | iOS Build Support 추가 후 로컬 Xcode 생성/시뮬레이터 빌드 경로를 시도할 준비가 있다. Unity와의 실제 호환 컴파일은 NOT_RUN. |
| iOS Simulator | 사용 가능 iOS26.2 runtime1개, 사용 가능 시뮬레이터11개, 이미 Booted1개 조회. | 이번 감사에서 부팅/종료/게임 설치하지 않았다. 기존 Simulator는 다른 작업일 수 있어 재사용·종료하지 않는다. |
| Apple 실기기 | devicectl에 iOS 휴대폰1개가 알려져 있고 기존 paired/developer-mode-enabled 상태. 조회 시 tunnel disconnected. | 설치 가능한 현재 연결·서명·프로비저닝은 미확인. 새 연결/pair/trust/계정 변경을 하지 않았다. 실기기 게이트는 NOT_RUN. |
| Android 도구 | PATH에 adb/emulator가 없지만, 기존 SDK에서 adb1.0.41/platform-tools37.0.1 바이너리와 emulator가 실제 실행된다. adb version/emulator list exit0. AVD1개, API36 arm64-v8a 이미지1종 발견. | 도구 부재로 단정할 수 없다. emulator 부팅·adb server 시작·연결 기기 조회는 하지 않아 Android 기기/AVD 실행은 NOT_RUN. |
| Android SDK/NDK | SDK platforms34/35/36, build-tools35/36, CMake3.22.1. 기존 NDK28.2.13676358만 발견. Unity 설치 metadata의 NDK 의존은r27c. | Unity6000.6 공식 요구 NDKr27c/JDK17과 대조해 Unity Hub 관리 도구를 우선 준비한다. 기존 다른 앱의 SDK/NDK를 교체하지 않는다. JDK17의 실제 Unity 경로·Gradle 빌드는 미확인. |

Unity는 Android Build Support와 SDK/NDK/JDK, iOS는 iOS Build Support와 Xcode를 요구한다. [Android 의존 설치](https://docs.unity.com/en-us/engine/6000.6/manual/platform-specific/android/getting-started/sdksetup/install-dependencies), [iOS 환경](https://docs.unity.com/en-us/engine/6000.6/manual/platform-specific/iphone/getting-started/ios-environment-setup). Android6000.6의 지원 버전 표는 NDKr27c와 JDK17을 명시하므로 기존 NDK28.2를 호환 확인 없이 연결하지 않는다. [지원 의존 버전](https://docs.unity.com/en-us/engine/6000.6/manual/platform-specific/android/getting-started/sdksetup/supported-dependency-versions).

## 기존 코드의 준비 범위와 모바일에서 남은 것

- 공통 이동 벡터·EnhancedTouch 단일 손가락 추적·modal 후 release gate, safeArea 기반 GUI viewport, 앱 pause/focus 시 입력 초기화·전투 정지, 명시적 재개는 이미 있다. 현재 PC/Editor 증거를 실제 모바일 통과로 세지 않는다.
- PC 최소660×440 강제는 standalone 전용이며 모바일에는 적용되지 않는다. GUI.matrix1과620px 고정 모달·24/24/20/16px 글꼴은 모바일 safeArea가 좁거나 고DPI일 때 잘림/물리 가독성을 보장하지 않는다. 세로·가로/회전 전후 정수 배율과 실제 터치 크기 비교가 필요하다.
- 터치 시작 영역은 전체 Screen의 절반/높이65%를 쓰지만 GUI는 safeArea를 쓴다. 노치·홈영역과 두 좌표 기준의 차이, UI를 누른 손가락의 이동 전파, 손가락 교체/멀티터치/취소를 기기에서 확인해야 한다. 현재 시제품의 터치 영역은 모바일 완성 조이스틱이 아니다.
- 저장은5초 시뮬레이션 간격+최대0.1초 host tick, 선택/종료/정지/중단 즉시 체크포인트다. 공칭 최대 롤백5.1 시뮬레이션 초, 실패 시 freeze/retry/reload 정책을 모바일 파일 시스템·앱 강제 종료에서 별도로 검증한다. 정지 콜백이 반드시 오거나 전원 손실까지 내구성을 보장한다고 가정하지 않는다.
- iOS/Android IL2CPP의 Core/DataContract 직렬화·stripping/AOT, 원자적 파일 교체/backup, 동적 outline atlas와 Point·원본 폰트 고지, 스프라이트/URP/셰이더 및 메모리 동작은 실제 대상 빌드가 필요하다. 설정 파일에 모바일 키가 있다는 것은 타깃 빌드 성공이 아니다.
- 프로젝트에 auto-rotation 설정과 플랫폼별 SDK 값이 있으나 템플릿/초기값이다. 출시 방향·최저 OS·지원 기기·FPS·발열/메모리 예산은 아직 제품 확정값이 아니다. 모바일 전용 application identifier와 격리 저장을 생성 빌드 전에 명시해 기존 PC 저장과 섞이지 않게 한다.

## 증거와 상태

개인 기기 이름/UDID/연결 원문은 ignored `evidence/mobile-env-20261114/`에만 저장했다. 공개 보고에는 종류·개수·버전·연결 가능 상태만 남겼다. `commands.json`의 Xcode/SDK/simctl/xctrace/help 조회7개는 모두 exit0, `device-list.json` 생성 명령도 exit0이다. `filesystem.json`, `android-redacted-summary.json`, `android-packages-summary.json`, `device-redacted-summary.json`에 파일/집계 근거가 있다. adb version, emulator list, SDK 설치 목록 읽기도 exit0이며 도구를 설치/갱신하거나 emulator를 부팅하지 않았다.

PASS는 조회·발견 범위만 의미한다. Android/iOS compile/build/install/touch/lifecycle/device/performance는 전부 NOT_RUN이며, Unity 타깃 생성은 해당 Build Support 부재로 현재 BLOCKED다. 다음 무료 실행 순서는 [모바일 다음 계획](../harness/mobile-next-plan.md)에 명시했다.
