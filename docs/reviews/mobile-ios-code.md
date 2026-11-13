# 아담 모바일 M0 독립 코드 리뷰

2026-11-14 KST. 기준 `48910998158eb6e791cf48a5349bd1eac070e4ac`. **PASS — 이번 M0 코드/증거 검토에서 재현 가능한 추가 HIGH/MEDIUM 없음.** 모바일 전체 수용 또는 실물 기기 지원 PASS를 뜻하지 않는다.

저장소 AGENTS와 전용 `harness-run-bible-survivor`의 실행/검증 기준, `mobile-ios.md`, `R01-mobile-ios.md`, 기준 이후 diff 및 신규 MobileLayout/MobileBuild/link.xml/시험을 읽었다. 이 보고서만 작성했다. 제품 코드·도구·증거·저장은 수정하지 않았고 Editor/Player/Simulator/GUI·설치·빌드·커밋을 실행하지 않았다.

## 코드 경계

- **safeArea/좌표:** Touchscreen 좌표는 bottom-left safeArea에서 Contains와 원점 포함 비율로 제한한다. OnGUI는 `Screen.height - safe.yMax`로 top-left viewport를 만들고 정수 scale을 적용한다. 이동 delta도 같은 모바일 scale로 나눈다. 모바일 safeArea 변경은 이동/잡힌 터치를 비우고 명시 재개가 필요한 pause로 바뀐다. 원점 이동을 뺀 뒤 touch 반경을 계산하므로 inset 자체가 이동량으로 섞이지 않는다.
- **터치 상태:** 하나의 touchId와 Began의 원점만 이동을 소유한다. Ended/Canceled를 건너뛰고 기존 다른 손가락의 Moved를 새로운 Began으로 취급하지 않는다. pause/modal/중단 후 held touch는 release가 필요하다. 사망 공통 SelectSlot 가드와 같은 프레임 중복 명령 가드도 유지된다. 새 PC PlayMode는 실제 InputSystem 두 손가락·Canceled 후 양도 금지와 release→새 finger를 검사한다. 이는 실물 멀티터치 지원 결과와 구분한다.
- **화면:** 모바일 경로만 66logicalpx 버튼과 110px 카드·두 줄 효과 상자를 사용한다. PC는 기존 default 인자/상자/폰트 역할을 유지한다. profile의 scale2/3x에서 44pt 계산은 맞지만 scale1·다른 기기의 같은 물리 크기를 보장하지 않는다. 해당 제한은 handoff에 명시되어 있으며 작은 기기의 M3 후속 게이트다.
- **저장 실패/미래 버전:** Host는 기존 SaveStore/원자적 revision/operation 경로를 유지한다. `HasSaveRetry`는 실제 미완료 operation 유무이며 재시도 성공 후 제거한다. `RequiresNewerGame`은 문자열 추측 대신 `UnsupportedSaveVersionException`으로 판정한다. 모바일 UI는 미래 버전 안내와 retry 불가 상태를 제공하고 원문 경로/예외 stack을 화면에 표시하지 않는다. Load 실패로 새 도전을 자동 시작하지 않는다. `ReportDevelopmentFailure`는 DEVELOPMENT_BUILD에만 호출되고 Editor에서는 출력하지 않아, 상세 진단을 배포 일반 경로와 구분한다.
- **AOT:** link.xml은 writer의 내부 generic dictionary enumerator와 reader KeyValue 두 타입만 보존한다. Core 저장 DTO/형식/로직을 대체하지 않는다. Unity/IL2CPP/BCL 내부 이름·stripping 변경에 대한 재검증 제한을 문서가 명시한다.
- **격리 빌드:** BuildIosSimulator는 프로젝트 marker와 절대 새 출력 경로를 설정 변경보다 먼저 요구한다. 검수 아트 검증 후 QA 앱ID·SimulatorSDK/ARM64/IL2CPP·팀 없음/자동 서명 꺼짐을 지정하고 BuildPipeline 실패를 예외로 전파한다. marker가 있는 QA 사본을 대상으로 하는 명시적 개발 진입점이며, 임의 호출자를 격리시키는 보안 sandbox로 해석하지 않았다. canonical에 출시 방향·타깃 설정을 저장하지 않는 실제 보존 결과도 아래에서 확인했다.
- **개인정보:** 개발 예외의 전체 stack·기기/프로세스/원문 저장 경로는 ignored evidence 범위에 남아 있다. `git check-ignore`로 실제 native 실패 로그가 제외됨을 확인했다. 공개 진행/회고 문서는 개인 경로 대신 상대 증거 이름과 실행 범위를 사용한다.

## 독립 읽기 전용 검증

새 Python subprocess에서 JSON/XML 파싱, SHA256 및 `git show 4891099:<path>` 비교만 수행했다. 증거를 새로 생성한 실행 결과로 표시하지 않는다.

| 독립 확인 | 실제 결과 |
|---|---|
| `final-source-settings-app-sha.json`의 소스/설정/빌드277파일 → 현재 디스크 | **277/277 일치**, 누락/변경0 |
| `installed-final-build-sha.json`의 allFiles → 실제 설치 bundle | **36/36 일치**, 누락/변경0 |
| canonical PNG79개의 meta + ProjectSettings → 기준4891099 | **80/80 byte 동일** |
| `release-pc-EditMode/results.xml` | 42 passed / 0 failed / 0 skipped |
| `release-pc-PlayMode/results.xml` | 15 passed / 0 failed / 0 skipped |
| 실제 scaled-start 원래 PC 구조 변이 XML | 1 failed, `Expected not null / actual null` 시작 행동 assertion |
| 같은 시험 복원 XML | 1 passed / 0 failed |

277파일 기록에는 신규 모바일 소스/시험/link.xml과 앱·격리 설정이 포함되며 현재 파일과 일치한다. 설치 기록의 요약 bool만 믿지 않고 실제 설치 bundle36개를 다시 hash했다. Simulator를 부팅하거나 앱을 실행하지 않았다.

## 개발자 실행 증거의 적합성

- writer/reader 제거 스크립트는 QA 사본의 해당 `<type>` 한 줄씩을 제거하고 복원한다. 실제 export/Xcode 명령의 성공과 변형별 앱 SHA가 따로 남아 있다. `drop-writer-app-stdout.log`는 Encode의 MissingMethodException, `drop-reader-app-stdout.log`는 Decode/StoreCollectionValue의 ArgumentOutOfRangeException을 포함한다. `linker-mutation-results.json`은 각각 빌드0·예외 발생·Commit 파일 부재를 기록한다. 컴파일 실패를 기능 변이 검출로 세지 않는다.
- 정상 최종 앱의 강제 종료 후 Load/장비·성장·위치/선택 터치 잔류 없음, 실제 권한 실패 후 한 번만 revision 증가, 손상 primary→backup 복구, 미래 schema에서 primary/backup 불변은 각 JSON뿐 아니라 관련 실행 스크립트/명령·로그·QA 저장과 결합된 근거다. Host 시험의 새 상태 assertions도 실제 동작 경계와 대응한다.
- 개발자 compile/export/Xcode/macOS build exit0와 42Edit/15Play를 이번 리뷰어의 재실행으로 합산하지 않았다. IL2CPP 두 변이도 로그·스크립트의 독립 검토이며 네이티브 앱 재설치/재현은 하지 않았다.
- native 사망 포즈→종료 모달 및 연속 탭 요청 결과는 정밀0.75초 touch 도착 증명이 아니다. handoff는 실패한 타이밍 가정과 NOT_TIMED를 보존한다. 정확한 공통 입력 경계는 기존 digit/hold/중단·실제 renderer 마우스 시험의 범위로 한정되어 있어 주장과 증거가 일치한다.

## 한계와 후속 게이트

독립 모바일 실제 화면·조작 리뷰는 별도 담당자의 결과다. 이 리뷰어의 Editor/Player/Simulator·실물·네트워크·빌드 재실행은 **NOT_RUN**이다. native 정밀 사망 touch, 작은 profile/다른 방향, 실물 멀티터치/전화/잠금/전원 손실, 모바일3분 완료·보상 경계, 모든 글리프의 on-device 렌더, 성능/발열/장기 메모리와 Android는 M0 완료로 합산하지 않는다. 재현 가능한 새 HIGH/MEDIUM 결함이나 현재 최종 SHA 불일치는 발견하지 않았다.
