# Android 최소 기술 인계 — 독립 PC 코드/Editor 증거 리뷰

2026-11-14. 기준84ccd9190dc1cfe650b32654f8c8c797ce1e28c7. **PASS — 신규 Editor 빌드 헬퍼와 최소 기술 인계의 코드/기존 실행 증거 범위에서 추가 HIGH/MEDIUM 없음.** 전체 Android 지원·모바일 UI·실물·성능 PASS가 아니다. 최신 사용자 지시대로 독립 리뷰는 PC 코드/Editor 범위로 제한했다.

소유 파일은 이 보고서뿐이다. AGENTS와 전용 하네스, `AdamAndroidBuild.cs`/meta, `mobile-android.md`/회고 및 ignored 실행 증거를 읽었다. Python으로 JSON/XML·파일 해시를 대조했으며 제품 코드·앱·GUI·Editor·기기·저장·SDK·git 상태는 변경하지 않았다. Editor/Android native 재실행·빌드·설치·커밋은 **NOT_RUN**이다.

## 코드 검토

- 진입점은 프로젝트의 `.adam-mobile-qa` marker, 명시적 절대 출력 경로, 새 `.apk` 경로, 격리 사본의 키 파일과 프로세스 암호 변수를 설정 변경 전에 요구한다. 기존 파일/디렉터리를 출력으로 덮어쓰지 않는다. marker는 승인된 QA 사본을 구분하는 실행 가드이며 임의 호출자를 가두는 보안 sandbox가 아니다.
- 아트 계약 검증 뒤 QA 앱ID·개발 APK·ARM64/IL2CPP/Minimal·GLES3·QA API26/36·회전 후보를 사본의 PlayerSettings에 설정한다. 출시 기기/방향/최저 OS를 확정한 코드로 해석하지 않는다. Core/Runtime/iOS 빌드 헬퍼를 바꾸지 않는다.
- 키 파일은 QA 프로젝트 내부 경로이며 암호는 환경 변수에서 읽는다. 코드의 Debug.Log에는 backend/ABI/stripping/API/그래픽 설정만 있고 암호·키 bytes를 출력하지 않는다. 신규 `.cs.meta`의 GUID는 final binding과 같다. 키/원문 로그/QA 설정은 ignored evidence이며 공개 문서에 비밀 값을 싣지 않았다.
- BuildPipeline의 summary가 Succeeded가 아니면 예외를 던진다. 사전 guard와 BuildPipeline 예외도 잡아 성공으로 바꾸는 경로가 없다. 정상 빌드 exit0는 실제 로그로 확인했다. guard/실패 전파의 별도 변이 실행은 없는 만큼 그 행동을 실검증했다고 주장하지 않는다.
- 앱 설치·기기 입력/종료는 이 헬퍼에 포함되지 않는다. 실행 명령 자료는 명시 QA serial의 headless 에뮬레이터·QA 앱을 대상으로 하고 기존 iOS Simulator 제어를 포함하지 않는다. 다른 기기의 미실행/보존을 새 native 관측으로 확대하지 않았다.

## 독립 읽기 전용 확인

요약 bool만 읽지 않고 manifest의 각 파일을 현재 디스크에서 SHA256으로 다시 계산했다. 증거 시점의 파일들이 그대로 존재했으며 대조 결과는 다음과 같다.

| 대상 | 독립 확인 결과 |
| --- | --- |
| source-before.json의 기존 소스·씬·설정·원본 아트 등 | **608/608 일치**, 변경/누락0 |
| existing-android-config-before.json의 기존 SDK/AVD 설정 | **24/24 일치**, 변경/누락0 |
| apk-source-inputs.json의 격리 QA 빌드 입력 | **239/239 일치**, 변경/누락0 |
| final-binding의 현재 헬퍼·meta·link.xml | **3/3 일치**, QA 사본 헬퍼도 canonical과 byte 동일 |
| 빌드 APK와 설치 후 pull한 base APK | 둘 다 final-binding의 `c1bfedd6…1a02d91`와 동일 |
| 격리 빌드 ProjectSettings | final-binding의 `f32a5c93…373f574`와 동일 |
| APK 내부 native libraries | 기록된7개 경로와 일치, 모두 arm64-v8a |
| 신규 PC EditMode XML | **42 passed / 0 failed / 0 skipped / 0 inconclusive** |
| PC 실행 summary | 실제 exit0, wrapper0, timeout false, 해당 격리 프로젝트/새 XML 경로 |

키 생성 기록·빌드/프로세스 로그·빌드 설정을 읽어 공개 코드의 암호 출력 경로가 없는 점과 대조했다. 읽은 build/process/signature/settings 자료에서 keystorePass/keyaliasPass/환경 변수의 비어 있지 않은 대입 출력은 발견하지 않았다. `git check-ignore`는 원문 키 생성 기록·QA keystore·빌드 로그3개 모두 제외됨을 확인했다. 비밀 값 자체를 이 보고서에 복사하지 않았다.

## 구현자 실행 증거와 주장 일치

APK command의 실제 Unity exit0와 `Exiting batchmode successfully`, apksigner verify의 v2 서명 유효 결과가 있다. 로컬 QA debug 인증서이며 스토어 배포 서명으로 보고하지 않았다. 최초 enum 컴파일 실패와 초기 adb offline 실패는 별도로 남아 있고 이후 성공과 혼합하지 않았다.

저장 fixture 파일을 본인이 파싱한 결과 첫 Commit은 revision2/성경1/HP100, 반복 Commit은 revision3/동일 RunId/X=1.41535735였다. 재실행 뒤 save-loaded는 revision3의 저장 bytes와 정확히 같았고, 재개 뒤 저장은 revision4/같은 좌표·장비와 증가한 전투 시각을 담는다. 따라서 파일 생성만으로 Load 성공을 주장하는 자료가 아니다. 실제 native 화면/입력과 강제 종료·재실행은 **구현자 실행**이며 독립 리뷰어의 실행으로 합산하지 않았다.

종료 자료는 QA 앱 force-stop0·QA 에뮬레이터 kill0·에뮬레이터 프로세스 exit0을 기록한다. 기존 원본/SDK 설정 해시 보존과 일치한다. 사용자가 플레이 중인 iOS 앱/저장에 이 리뷰어는 접근하거나 복원하지 않았다. local/portable checker0도 구현자의 실행 기록으로만 인용한다. 이전 PlayMode15/macOS 빌드를 이번 Android 작업의 신규 실행으로 합산하지 않는다.

## 열린 제한

- 관측 Android profile에서 버튼이 약38dp여서48dp 목표는 아직 충족하지 않을 수 있다. 이번 변경은 Editor 전용 헬퍼이므로 기존 모바일 UI 결함을 닫거나 Android UI 수용 PASS로 올리지 않는다.
- 실제 로그의 `AssetPackManager` ClassNotFoundException은 남아 있다. 이후 렌더/저장/Load 성공이 해당 예외의 원인 해결이나 AAB/Play Asset Delivery 지원을 뜻하지 않는다.
- 헬퍼 guard/실패 전파 변이·Android linker 타입 제거 변이·세 무기 전체·성장/회전/Back/중단 세부 경계·손상/미래 schema·사망 입력·실물/성능은 NOT_RUN이다. 추가 Android native 독립 UI 리뷰도 사용자 범위 밖이므로 요청하지 않는다.
- 이 결과는 최소 기술 인계를 다음 PC 게임 개발로 연결할 수 있다는 범위다. 모바일 전체 완료 주장을 허용하지 않는다. 후속 변경 시 최종 SHA와 해당 실행 증거를 다시 결속해야 한다.

근거 위치: ignored `evidence/mobile-android-20261114/`의 final-binding/source-before/existing-android-config-before/apk-source-inputs, pc-EditMode XML/summary, APK/build/signature, save4종/save-checks, shutdown/exit와 checker 기록. 원문 개인 경로·기기 식별자·키는 공개 문서에 포함하지 않았다.
