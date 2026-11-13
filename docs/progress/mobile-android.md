# Android 기술 선행 작업

2026-11-14, 기준84ccd9190dc1cfe650b32654f8c8c797ce1e28c7. 최소 기술 인계 완료이며 전체 Android 지원·실물/성능 완료가 아니다. 사용자 최신 지시 「일단 리뷰는 PC만 하고 다음거로 넘어가줘」와 coordinator 인계에 따라 독립 리뷰는 PC 코드/Editor/PC UI로 제한하고, Android는 무료 모듈·격리 빌드/실행·최초 저장/재Commit/재실행 Load 등 최소 기술 근거를 우선한다. 장시간 모바일 UI/경계 시험 및 native 독립 UI 리뷰는 이번 범위에서 확대하지 않는다. 미실행을 모바일 PASS로 올리지 않는다.

현재 사용 중인 iOS Simulator/앱/저장은 조회·조작·종료·원복하지 않는다. Android QA는 별도 AVD·명시 serial·앱ID/컨테이너만 사용하며 기존 Android SDK/AVD·PC 저장을 보존한다. raw 명령/기기/경로/로그/키는 ignored `evidence/mobile-android-20261114/`에만 둔다.

설치 전 여유 약103GiB와 모듈 metadata/help를 기록했다. 동일 Unity6000.6.9f1에 Android Build Support와 필수 관리 의존만 추가했다. Hub CLI exit0 및 All Tasks Completed Successfully, 관리 NDK r27c/27.2.12479018·OpenJDK17.0.18+8·build-tools36.0.0을 실제 확인했다. [Unity 공식 의존 버전](https://docs.unity.com/en-us/engine/6000.6/manual/platform-specific/android/getting-started/sdksetup/supported-dependency-versions)에 대조했다. 기존 다른 앱의 NDK28.2/SDK/JDK·계정·환경 설정은 교체하지 않았다.

`AdamAndroidBuild.BuildQaApk`는 marker가 있는 격리 사본과 새 APK 경로, QA 사본의 별도 debug keystore/프로세스별 암호 변수를 요구한다. arm64/IL2CPP/Minimal stripping/GLES3·QA minAPI26/target36·QA 앱ID·개발 APK를 설정하며 빌드 실패는 비0으로 전파한다. minAPI/방향은 QA 설정이며 제품 지원 하한/출시 방향 확정값이 아니다. 기존 iOS 빌드 코드는 수정하지 않는다. canonical 씬/플랫폼 설정/PNG meta를 열지 않고 사본에서 빌드한다.

별도 QA AVD는 기존 API36 Google APIs arm64 시스템 이미지를 읽기 재사용하고 쓰기 데이터/환경 경로를 격리한다. headless·RAM2GiB/2core로 실행해 사용 중인 iOS GUI에 입력/포커스를 보내지 않는다. [Android Emulator CLI](https://developer.android.com/studio/run/emulator-commandline)에 따른 명시 QA serial만 install/launch/input/종료 대상으로 삼는다. 초기 adb devices 조회는 연결0이며 기존 AVD는 재사용/종료하지 않았다.

최초 빌드는 API enum 이름 오류로 compile exit1을 남겼고 `AndroidSdkVersions`로 교정한 다음 실제 APK 빌드 exit0/Exiting batchmode successfully를 확인했다. 초기 adb install은 device offline으로 실패했으며 명시 QA 기기 wait-for-device 후 재설치 exit0/launch exit0으로 진행했다. 두 linker 타입은 기존 검토본 그대로 포함하며 Android에서의 보존 제거 변이는 NOT_RUN이다.

| 게이트 | 결과와 근거 |
| --- | --- |
| 무료 모듈/관리 의존 | PASS: Hub exit0, Android/필수 자식만 설치; iOS 모듈 metadata 유지. Gradle9.3.1/JDK17/NDKr27c. 별도 다운로드 아카이브 자체 해시 재계산은 NOT_RUN. |
| 컴파일/실 APK | PASS: IL2CPP arm64, Minimal, GLES3, Development, 실제 APK 빌드0. ARM64 라이브러리만 포함; apksigner verify0, 로컬 QA debug 서명이며 배포 서명 아님. |
| install/launch/render | PASS: API36 arm64 QA 에뮬레이터, 한글/픽셀 아트·시작/정지 모달 실제 화면 확인. 시스템 fullscreen 안내는 QA 기기에서만 닫음. |
| 최초 Commit→재Commit→종료/Load | PASS: 첫 무기 회전 성경, 터치 드래그로 X좌표0→1.41535735, 정지 저장 revision2→3. 강제 종료/재실행 후 정지 모달·저장 bytes 동일; 재개→정지 저장 revision4, Run/프로필/좌표 유지·시간 증가로 실제 읽기 이후 쓰기까지 대조. fixture 없음. |
| 관련 PC Editor | PASS: 격리 사본 실제 EditMode42/42, wrapper0/XML 신규 생성. 제품 Runtime/시험/PC 설정 변경 없음; 이전 검토본 Play15는 이번에 재실행하지 않음. PC GUI/새 macOS 빌드도 NOT_RUN. |
| 경계/보존 | PASS: local/portable checker 각0. 기존 소스·씬·설정·메타·원본 자산608파일과 기존 Android SDK/AVD 설정24파일 해시 변화0. 사용자 iOS 및 PC 저장에는 접근/fixture 없음. |
| 종료 | PASS: QA 앱 force-stop0, QA 에뮬레이터 kill0 및 프로세스 exit0. 기존 기기/Simulator/전역 adb server 종료 없음. |

APK SHA256: `c1bfedd63f797ab9d91db45a7c2c296f29c7180f464d868469bc9d9a61a02d91` — 설치된 base APK를 pull해 동일 확인. 빌드 헬퍼 SHA256: `3cd6c2596f2989c19c4d9e38c6d8e88d81146617e0c2e79bf512817509484c2e`. 격리 빌드 설정 SHA256: `f32a5c933acdf9e43e6e24f143fda786a0e8a598cb8c5fc00b474f5ed373f574`. 최종 `link.xml`은 기존 두 타입 보존본이며 해시는 `final-binding.json`에 있다.

실행 근거는 ignored `evidence/mobile-android-20261114/`의 `apk-command.json`/`apk-build.log`, `pc-EditMode/results.xml`·`summary.json`, `save-checks.json`·`save-final-commands.json`, `save-first.json`/`save-repeat.json`/`save-loaded.json`/`save-after-load-commit.json`, `reloaded-ready.png`/`after-load-paused.png`, `final-binding.json`/`qa-shutdown.json`에 있다. QA 명령/컨테이너 재현은 같은 폴더 `handoff-private.md`를 따른다.

남은 항목: 세 무기 모두·성장 카드·회전/다른 화면·Back·중단복귀 세부경계·백업/미래schema·권한오류·사망 입력·동시 터치·실물·성능은 이번 Android에서 NOT_RUN이다. 1440×3120/dpi560/scale2에서 66게임px 버튼은 약38dp이므로 Android48dp 목표는 아직 미충족 가능성이 있으며 모바일 UI 수용 완료로 세지 않는다. 시작 로그에 `AssetPackManager` ClassNotFoundException이 두 번 있었지만 이 APK에서는 뒤이어 렌더/저장/Load가 성공했다; 원인 확정·AAB/Play Asset Delivery 지원은 NOT_RUN이며 무시/해결됐다고 기록하지 않는다.

사용자의 PC만 리뷰 지시에 따라 추가 native 독립 UI 리뷰를 요청하지 않는다. root가 PC 코드/Editor 범위 리뷰·GC·commit/push를 맡는다. **Android/Unity Editor 및 Runtime·표현 파일 작성 소유권을 root에 반환한다**. 후속 타격감/이펙트 작업은 `docs/combat-feel-plan.md`와 `docs/art/combat-fx-brief.md`에서 이어갈 수 있으며 이 문서나 root의 다른 작업을 수정하지 않았다. 사용자 iOS 플레이는 그대로 유지한다.

새 빌드 헬퍼 guard/실패구조의 별도 변이 시험은 NOT_RUN이며 실제 컴파일/정상 빌드와 구분한다.

PC 코드/Editor 범위 독립 검토에서 원본608개·기존SDK/AVD설정24개·격리입력239개·binding3개의 해시, PC42개 XML과 설치APK·저장 왕복 근거를 읽기 전용 대조했다. 검토 보고서는 `../reviews/mobile-android-code.md`, 통합 GC는 `../retrospectives/R01-mobile-android.md`에 남긴다. Android native 독립 재실행은 추가하지 않았다. 원격 커밋/CI 결과는 후속 기록으로 구분한다.
