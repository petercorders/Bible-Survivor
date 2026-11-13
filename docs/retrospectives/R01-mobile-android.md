# Android 최소 기술 인계 회고

통합 GC: 원본 소스/자산608개·기존SDK/AVD설정24개와 격리 입력239개·helper/meta/link binding3개의 독립 해시 대조, PC42개 시험 XML과 설치APK 동일성, 실제 저장 revision2→3→재실행→4의 근거를 교차 검토했다. 독립 검토 상세는 `../reviews/mobile-android-code.md`다. 알려진 약38dp 버튼·AssetPackManager CNFE·guard/Android linker 변이 미실행은 해결로 승격하지 않으며 후속 게이트로 보존한다.

후속 타격감 작업은 Android 커밋 경계 뒤 단일 작성자에게 제품 소유를 넘긴다. 먼저 읽기·설계만 진행하고 Core/DLL/provenance 편집은 커밋 후 시작하게 하여 precommit 검사 대상이 섞이지 않도록 한다. 사용자 iOS 플레이/저장은 계속 보존한다. 전역 GC 상태/규칙은 변경하지 않는다.

2026-11-14. 사용자 지시로 독립 리뷰는 PC만, Android는 실제 빌드·최소 저장/Load까지로 축소했다. 전체 모바일 PASS를 주장하지 않는다.

- 교정: Android API enum을 확인 없이 사용해 최초 컴파일1. 실제 Unity API로 수정 후 빌드0/42Edit PASS. 다음 플랫폼 헬퍼 작성 시 설치된 참조의 enum 이름부터 대조하는 후보이며 전역 규칙 추가는 하지 않는다.
- 도구/환경: 초기 adb offline은 명시 QA serial wait-for-device→재설치로 해결. 전역 adb kill이나 기존 기기 재부팅을 사용하지 않았다. fullscreen 안내가 엔진 포커스를 막았으며 접근성 트리 bounds로 QA 창의 안내를 닫았다.
- 검증 판단: 파일 존재만 Load PASS로 세지 않고 실제 재실행 정지 화면→재개/Commit revision4·Run/좌표/프로필을 확인했다. Android linker 제거 변이·native 전체 UI는 미실행으로 남겼다.
- 낭비 방지: canonical Editor를 열지 않고 사본에서 Android 전환하여 원본608파일의 자동 meta/설정 변화를 피했다. 사용 중인 iOS GUI에 포커스를 보내지 않는 headless QA로 진행했다.
- 후속 도구화 후보: 새 marker QA 사본/환경/출력/명시 serial/종료를 한 재현 스크립트로 고정할 수 있다. 별도 승인 범위와 실패 입력 검증을 정한 뒤 적용하며 이번에는 원본 CI·hook을 편집하지 않았다.

단일 worker로 진행했고 추가 위임은 없다. 실제 사용 모델/effort·token/cache/reasoning 사용량·비교 기준 실행 비용은 미측정이며 절감률을 추정하지 않는다. root에 PC 독립 리뷰와 표현 소유를 반환하고 타격감 후속 작업으로 연결한다.
