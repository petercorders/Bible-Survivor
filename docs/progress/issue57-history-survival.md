# 히스토리 서바이벌 PC 표시명

[#57](https://github.com/petercorders/Bible-Survivor/issues/57)의 한국어 표기는 **히스토리 서바이벌**, 영어 표기는 **History Survival**이다. 한국어 게임 시작과 책 완료 화면에 새 이름을 표시한다. PC 창과 배포 파일은 영어 이름을 사용한다. 일반 macOS 빌드는 `Builds/History Survival.app`을 만든다. Windows에서 수동으로 빌드할 때는 출력 파일명을 `History Survival.exe`로 지정한다.

Unity 회사명 `BibleSurvivor`, 제품 식별자 `Adam Prototype`, macOS 번들 ID `com.DefaultCompany.2D-URP`를 유지한다. 창 제목과 macOS `CFBundleDisplayName`을 표시 계층에서 설정한다. 세이브 파일명 `adam-prototype.json`과 Unity의 `Application.persistentDataPath` 선택에 의존해 업데이트 전과 같은 위치를 연다. #55의 해상도와 #56의 컷신 자막은 각각의 브랜치에서 관리한다.

## 검증

Apple M4 Pro, macOS 26.5.1, Unity 6000.6.9f1 Standalone Development Player. 구 버전은 #53의 `9251b3de9969ab18c5c76bdc863dc30882064a53`에서 빌드했다. 로컬 증거는 `evidence/issue57/`에 있다.

| 확인 | 관찰 결과 | 로컬 증거 |
|---|---|---|
| 일반 macOS 빌드와 패키지 | 구·신 BuildMac 종료 0. 새 앱의 plist 검사와 엄격한 서명 검사를 통과했다. OS 앱 이름과 창 제목은 History Survival이었다. | `old-build.log`, `new-build.log`, `package-check.log`, `new-dual-title.json` |
| 신규 프로필의 타이틀과 기존 프로필의 책 완료 화면 | 실측 클라이언트 1920×1080과 660×440에서 한국어 이름의 글리프, 잘림 여부와 영어 창 제목을 확인했다. macOS 외부 창 크기는 위쪽 제목 막대 32px를 포함한다. | `new-fresh-1080-*`, `new-fresh-660-*`, `new-existing-1080-*`, `new-existing-660-*` |
| 기존 진행·영구 스킬 | 동일한 격리 프로필에서 구·신 Player 모두 창세기 완료와 출애굽기 이어 가기를 표시했다. 원본에 R09 완료·R10 대기, RunId, 공격력 1랭크, 잔액 1,332와 구매 영수증이 있었다. 원본과 백업 각각의 업데이트 전후 해시가 일치했다. | `old-existing-*`, `new-existing-*`, `profile-fixture.log`, `upgrade-before.sha`, `upgrade-after.sha` |
| macOS 저장 후보 두 곳 | 앱 지원 디렉터리의 번들 ID 경로와 회사명/제품명 경로에 서로 다른 유효 세이브를 넣었다. 구·신 Player 모두 회사명/제품명 경로의 R09 정산을 표시했고 두 파일의 해시가 그대로였다. | `old-dual-*`, `new-dual-*`, `dual-before.sha`, `dual-after.sha` |
| 이전 화면 설정 | 별도 환경설정 도메인을 같은 값으로 지정한 구·신 앱 복제본을 해상도 CLI 없이 연속 실행했다. 두 Player 모두 1280×800 창 모드를 사용했다. 기본값은 1100×720으로 기록돼 있었다. | `old-prefs-*`, `new-prefs-*`, `prefs-test/before.plist`, `prefs-test/after.plist` |
| 영구 스킬 PlayMode와 휴대성 검사 | 기존 소형 창 영구 스킬 구매·환불 회귀 1/1, 휴대성 검사 종료 0. | `skill-playmode.xml`, `portable.log` |

기존 진행 시험은 #53의 R09 완료 기술 저장에 Core `SaveStore`의 실제 구매 명령을 실행해 준비했다. 두 일반 Player에서 해당 저장을 열어 완료 상태를 확인하고 저장 바이트를 대조했다. 새 Player에서 스킬 구매나 다음 챕터 저장을 만드는 입력은 이번 업데이트 시험에서 관찰하지 못했다. 화면 설정은 같은 격리 도메인으로 확인했으며 배포본 번들 ID는 구·신 앱의 메타데이터에서 별도 대조했다.

Windows에서 직접 띄운 Player, Steam 설치·실행과 공개 이름 검수는 미실행이다. Steam의 검수 표기는 배포 설정에서 확인한다.
