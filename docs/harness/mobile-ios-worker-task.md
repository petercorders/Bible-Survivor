# 아담 iOS Simulator 선행 작업

사용자 PC·모바일 앱 개발, 결제 외 지속 진행 승인에 따라 `mobile-next-plan.md`의 M0/M1부터 실행한다. 기준 커밋은4891099이며 아트/폰트·코어 원격 CI도 통과했다. 이 단계는 서명/출시/실물 지원 완료가 아니다.

## 소유와 변경 범위

- 단일 Unity 담당이 무료 iOS Build Support 설치, 모바일 빌드 진입점·필요한 최소 플랫폼 설정·아담 입력/화면 적응과 관련 시험을 소유한다. 먼저 격리 QA 프로젝트에서 타깃을 확인한다. canonical Core/PNG/font/기존 CI·하네스 검사기는 수정하지 않는다. Core 문제가 드러나면 AOT/stripping/호환 근거와 최소 수정안을 coordinator에 인계한다.
- 문서: `docs/progress/mobile-ios.md`, `docs/retrospectives/R01-mobile-ios.md`, 필요 신규 모바일 전용 도구. 기존 mobile-readiness/next-plan은 감사 기준으로 보존하고 실제 결과는 별도 작성한다. commit/push와 독립 리뷰는 root 소유다. 추가 worker를 만들지 않는다.
- 기존 booted Simulator·다른 AVD·기존 SDK/NDK·휴대폰 pairing/계정/프로비저닝/인증서는 건드리지 않는다. 서명 없는 Simulator 경로를 사용한다. 결제는 실행하지 않고 root에게 알린다. 무료 작업은 재확인 없이 진행한다.

## 실행

1. 설치 전 디스크 공간,6000.6.9f1 모듈 위치/현재 metadata를 기록한다. 현재 Mac/WebGL만 있고 Xcode26.2·iOS Simulator26.2가 준비돼 있다. Unity Hub의 현재 공식 CLI/help를 확인해 같은 Editor에 **iOS Build Support만** 추가한다. 기존 설치를 지우거나 Editor를 바꾸지 않는다. 다운로드 크기/해시·목적지·설치 결과를 남긴다. 필요 GUI는 orca-computer-use 지침을 읽고 사용한다. 인증·계약·라이선스/결제 변경을 임의 진행하지 않는다.
2. 실제 iOS 모듈 확인 후 검토된 소스의 격리 작업 사본에서 iOS Simulator arm64용 프로젝트를 생성한다. 새 임시 폴더/DerivedData를 사용하고 원본 GUID/sourcehash를 보존한다. BuildPipeline/Xcode 실패는 비0으로 인계한다. SDK/scheme/destination은 실제 조회한 값으로 고정한다. `CODE_SIGNING_ALLOWED=NO` 등 서명 없는 Simulator 빌드를 먼저 확인한다.
3. 별도 QA Simulator/device set와 앱 ID/저장 컨테이너를 사용한다. 기존 Booted1은 재사용/종료하지 않는다. 항상 정확한 QA UUID/PID를 지정하고 기기 이름/UDID/연결 원문은 ignored evidence에만 둔다. 앱 설치/부팅/렌더·한국어·입력·저장 재개를 실제 빌드로 검사한다. IL2CPP/DataContract/AOT/stripping·File.Replace·backup·글꼴/셰이더 오류를 구분한다.
4. 모바일 화면은 고DPI/가로/세로/safeArea에서 실제 측정한다. PC620px 모달·24px 글자·660 최소값을 그대로 물리 폰트/터치 기준으로 간주하지 않는다. 기존 아트03 모바일 시안/계약을 참고하되 실제 논리 크기·화면비·터치 좌표로 검증한다. 필요 최소 적응은 구현·관련 회귀로 확인하며 PC39/13 기준을 후퇴시키지 않는다. 디자인/에셋 변경이 필요하면 stableID/크기/상태·실제 캡처를 root에 보내 지정 Claude Design 담당과 연결한다. 현재 검수 원본을 조용히 고치지 않는다.
5. 시작 세 무기/선택·이동·정지/재개·사망 공통 입력 가드·백그라운드/복귀·강제 종료/저장 재실행을 가능한 범위에서 실제 실행한다. 합성 터치·시뮬레이터·실물 관측을 구분하고 원래 우회 회귀의 행동 시험을 유지한다. 전원 손실/전화/실물 성능은 실행하지 않았으면 NOT_RUN이다.

## 종료·증거·실패

모든 프로세스는 명시 PID/UUID와 고유 로그로 소유한다. **종료 조작에 restore-window를 붙이지 않는다**: 닫힌 앱을 다시 열어 공유 저장 충돌이 발생했던 실제 회귀가 있다. 상태 확인은 비실행성 조회로 하고 원복은 자기 QA 프로세스가 모두 종료된 뒤 수행한다. 기존 Simulator/PC 저장은 변경하지 않는다.

단계별 실제 argv/exit·새 XML/화면·플랫폼/OS/버전·빌드 SHA·QA 저장/프로세스 종료·원본 보존을 ignored evidence에 남긴다. 같은 환경 실패는 원인 확인 후 재시도1회까지, 반복 네트워크/인증 차단은 그 게이트만 보고한다. Android 설치와 새 콘텐츠는 이번 작업에서 시작하지 않는다. 실제 구현 결과와 미실행/차단을 별도로 보고하고 root가 독립 코드/UI·GC·선별 commit/push/CI를 진행한다.
