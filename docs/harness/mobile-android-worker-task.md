# 아담 Android 선행 작업

사용자의 결제 외 지속 개발 승인과 `mobile-next-plan.md` M2를 적용한다. 기준은 검토·GC 후 업로드된84ccd9190dc1cfe650b32654f8c8c797ce1e28c7이다. iOS 지정 Simulator 범위는 독립 리뷰 PASS이며 전체 모바일 지원은 미완료다.

## 소유·보존

- 단일 구현자가 무료 Android Build Support 및 Unity 관리 SDK/NDK/OpenJDK 설치와 격리 Android 빌드·최소 플랫폼 적응·관련 시험을 소유한다. 실제 Hub help/공식 의존 버전과 여유 공간을 먼저 확인한다. 기존 Editor6000.6.9f1/iOS 모듈/다른 앱 SDK·NDK·JDK·환경 변수·계정·라이선스를 바꾸거나 삭제하지 않는다.
- 사용자가 현재 iOS QA 앱을 플레이 중이다. **Simulator/그 앱/그 저장은 전혀 조작·종료·원복하지 않는다.** PC 저장과 기존 Android 실물/AVD도 보존한다. 기존 실행 기기 조회와 별도 QA AVD 생성만 허용하고 설치·입력·종료는 명시 QA serial/UUID에 한정한다. adb server 전체 kill은 금지한다.
- Core/원본03/font/기존 CI 검사기·hook은 수정하지 않는다. canonical 씬·설정·PNG meta를 불필요하게 재직렬화하지 않도록 격리 QA 사본에서 빌드한다. 플랫폼 코드 변경이 필요하면 단일 작성자로 소유하고 최종 해시를 남긴다. 디자인 수정이 필요하면 stable ID·크기·상태·실화면을 root에게 보내 지정 Claude Design 협업을 요청한다.
- 소유 문서는 `docs/progress/mobile-android.md`, `docs/retrospectives/R01-mobile-android.md`다. commit/push·독립 코드/UI 리뷰·통합 GC는 root가 진행한다. 추가 worker를 생성하지 않는다. 결제 작업만 중단하고 root에 보고한다.

## 실행·수용

1. 설치된 모듈/SDK·여유 공간·기존 AVD 잠금/실행 상태를 읽기 확인한다. 같은 Editor 무료 Android 모듈과 필요한 관리 의존만 추가한다. 이전 환경 감사는 NDK28.2만 발견했으며 Unity 지원 r27c/JDK17 정합을 실제 확인한다. 다운로드/설치 결과와 실제 경로·버전은 ignored evidence에 기록한다. 공식 기술 문서로 현재 요구를 확인한다.
2. marker가 있는 격리 사본·새 출력에서 QA 앱ID·개발 APK·IL2CPP/arm64를 명시하고 빌드한다. 개발용 debug 키 사용은 공개 배포/스토어 서명이 아니다. BuildPipeline 실패는 비0으로 전파한다. 최저 API/출시 방향은 제품 확정값으로 바꾸지 않는다. ABI/Gradle/SDK/NDK/JDK·stripping·실제 명령/exit를 기록한다.
3. 별도 QA AVD/컨테이너에서 install/launch/render 및 실제 Commit→재Commit→강제 종료→Load를 검사한다. 두 타입 link.xml이 Android에서도 필요한지 단정하지 말고 실제 동작을 관측한다. 저장 실패는 전체 개발 예외로 원인을 확인하며 인메모리 우회/저장 비활성화로 통과시키지 않는다.
4. 세 시작 무기, 세로/가로 safeArea·표시 크기·실제 터치 이동/성장/정지·재개/회전/백그라운드·복귀와 Android Back 동작을 관측한다. 자연 성장과 fixture를 구분한다. 권한/저장 오류 재시도·백업 손상·미래 schema 보호는 QA 저장에서만 실행한다. raw 터치 명령 exit0은 화면/저장 행동 성공과 구분한다. 손가락 멀티터치/실물/성능은 에뮬레이터로 대체하지 않는다.
5. 실제 반복 실패는 원인 확인 뒤 재시도하고, 네트워크/다운로드/ABI 등 외부 차단은 로그와 함께 해당 게이트를 보고한다. 같은 차단에서 무한 반복하거나 다른 앱 SDK를 바꾸지 않는다. 가능한 독립 코드·문서 작업은 계속한다.

## 검증·인계

제품 코드/설정 변경 시 관련 Editor 검사와 기존42Edit/15Play·PC 경계를 유지한다. 원래 실패 구조 변이→행동 실패→복원 PASS를 별도 사본에서 확인한다. 기존 iOS 앱은 새 코드로 교체하지 않는다. 최종 소스/설정/APK/설치 앱 해시와 기존 Core/PNG/TTF/meta 보존을 확인한다. 자동 플랫폼 블록은 본인 변경만 분류·정리한다.

ignored `evidence/mobile-android-20261114/`에 명령·실제 exit·빌드 로그/XML/스크린샷/QA 저장/원문 기기 식별자와 독립 리뷰용 실행 인계를 남긴다. 공개 문서는 개인 경로/기기 ID/인증서를 제외한다. 끝나면 본인 QA 프로세스만 종료하고 기존 기기·저장·iOS 사용자 플레이를 보존한 채 보고한다. PASS/FAIL/BLOCKED/NOT_RUN을 빌드·입력·저장·실물·성능별로 구분하며 모바일 전체 완료로 일반화하지 않는다.
