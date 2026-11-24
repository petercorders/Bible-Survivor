# #67 Mac 표시명과 Core 빌드 재현성

비교 기준은 #54의 e51f117bba69438062d79bd7f4cc6a345becd81f다. 실행 세션의 turn_context에서 gpt-6.2-astra, high를 확인했다. 로컬 증거 디렉터리는 이 작업공간의 `evidence/issue67/`이다.

## 표시명과 저장

Unity 6000.6.9f1, macOS 26.5.1 (25F80) arm64, Development Player, 격리한 QA 번들 도메인과 홈, AudioManager 비활성 및 -noaudio. 기존 QA 프로젝트를 APFS 복제해 단일 Editor에서 빌드했다. 테스트 전용 관측 코드가 실제 Screen.SetResolution을 호출하고 Orca가 PID별 OS 창 목록을 읽었다. 제품 번들 ID는 com.DefaultCompany.2D-URP이고 테스트 복제본만 별도 도메인을 사용한다.

- 기준 앱의 저장 모드 1에서 OS 제목 Adam Prototype, OS 창 1920×1080. 관측 후 SIGTERM(-15).
- 수정 앱의 같은 전체화면에서 History Survival, 창 전환 뒤 History Survival 1280×832(클라이언트 1280×800). 정상 종료 0. 제목 폴링이 macOS 전체화면에서도 AppKit의 기존 제품명 창을 찾는다.
- 실제 저장 위치는 격리 홈의 Library/Application Support/BibleSurvivor/Adam Prototype/adam-prototype.json이다. 정상 종료 자동저장은 Revision과 LastOperationId를 갱신한다. 프로필, RunId와 나머지 게임 상태를 필드별로 대조한다. 사용자 설정 표식은 719다.

## 생성 입력

기준에서 `bash tools/dotnet.sh build src/BibleSurvivor.Core -c Release`는 0, `python3 tools/unity_check_project.py`는 1. NuGet props에 패키지 절대 경로, editorconfig에 ProjectDir, CoreCompileInputs.cache에 SDK가 사용한 순서 있는 레퍼런스 경로가 반영된다. AssemblyInfo에는 Git revision이 들어간다. 이전 감사의 기록 revision 대입 시 3개 실패, 두 텍스트의 경로 치환과 SDK Hash의 186개 입력 경로 대입 시 기존 해시 일치 증거를 재사용했다.

schema 3은 두 생성 텍스트의 루트 접두사만 정규화하여 해시한다. 캐시는 Rebuild에서 실제 순서와 IgnoreCase를 캡처하고 현 경로로 확장한 뒤 같은 SDK Hash 태스크로 실제 파일을 검증한다. Release에서는 소스 경로를 매핑하고 Git revision 및 추적 여부에 따른 PDB 차이를 제거했다. 실제 로컬 Release DLL과 커밋한 Unity 플러그인을 모두 대조한다.

재현은 각 깨끗한 경로에서 위 Release build와 엄격 검사를 차례로 실행한다. 검사 전에 sync를 실행하면 기준이 바뀐다. 이번 두 루트는 공백을 포함하고 깊이가 다르며 원장과 플러그인은 같은 후보에서 복사했다. 원래 소스 및 생성 파일 변이와 복원은 `python3 tools/unity_test_provenance.py --out <새 디렉터리>`로 수행한다.

Release DLL SHA-256은 `969b51a0cfa3ec1cf9e832e5930ba5614f6ae5d55df7eca91ea8e6d49269eec8`이다. `final-mutations/summary.json`에서 45개 통과, 실제 Combat.cs와 생성 파일 4개를 각각 변경하면 종료 1, 복원하면 0이다. `check_all.py` 종료 0, Core 1,263/1,263 및 R100 기술 픽스처 통과. 최종 checkout SHA별 두 빌드와 Player 재실행의 관측 결과는 Draft PR 검증 표에 기록한다.

## macOS 배포 검수

AdamProject.BuildMac은 BuildOptions.Development를 사용한다. 이름·리소스 후처리와 네이티브 실행 파일 설치가 끝난 로컬 QA 번들은 ad hoc 서명이다. 첫 전환·저장 관측에 사용한 빌드의 CDHash e4b0a58edd3b2913d32f0e95e0655356ba68e508, TeamIdentifier 미설정, codesign --verify --deep --strict 종료 0, spctl --assess --type execute --verbose=4 종료 3(rejected).

배포 담당자는 Development와 QA probe가 빠진 제품 앱을 준비하고, 최종 실행 파일·리소스 설치 후 승인된 팀의 Developer ID Application으로 안쪽 코드부터 앱까지 서명한다. Hardened Runtime, Unity 백엔드에 맞는 최소 entitlements와 secure timestamp를 확인한다. 승인된 배포 환경에서 공증 Accepted를 확인한 뒤 티켓을 staple한다. 최종 전달 파일 SHA-256과 앱 CDHash를 기록하고 codesign --verify --deep --strict, xcrun stapler validate, spctl --assess --type execute --verbose=4를 같은 아티팩트에서 실행한다. 새 Mac의 설치·실행은 별도 검수다. [Apple 서명 절차](https://developer.apple.com/documentation/xcode/creating-distribution-signed-code-for-the-mac/) · [Apple 공증 절차](https://developer.apple.com/documentation/security/notarizing-macos-software-before-distribution/)

Developer ID Application identity 한 건이 읽기 전용 조회에서 발견됐다. 이 제품에 승인된 서명 팀·배포 환경과 공식 패키지가 제공되지 않아 최종 패키지 해시·서명·공증·Gatekeeper 인수는 BLOCKED다. Windows/Steam 실기기 설치·업데이트, 물리 입력·오디오, 442단계 효과 전수와 장기 밸런스는 이번 변경에서 NOT_RUN이다.
