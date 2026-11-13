# 아담 iOS Simulator 선행 검증

2026-11-14, 기준48910998158eb6e791cf48a5349bd1eac070e4ac. **무료 iOS 모듈·Simulator arm64 빌드/실행과 지정 phone profile의 주요 조작·저장 검증 PASS, 모바일 전체 수용은 미완료**다. native 연출 중0.75초 터치 타이밍과 실물·성능 등의 남은 게이트는 아래에서 구분한다. 독립 코드/UI 재검토·GC·커밋/push/해당 SHA CI는 coordinator 소유다.

증거 루트는 ignored `evidence/mobile-ios-20261114/`이며 실제 명령·exit·새 XML·PNG/MOV·원문 기기/프로세스·QA 저장은 여기에만 있다. 기존 mobile-readiness/next-plan과 coordinator의 기존 결과 문서는 보존했다.

## 모듈과 빌드

설치 전 여유 약132GiB와 동일 Unity6000.6.9f1의 Mac/WebGL metadata를 기록했다. 실제 Hub help 및 [공식 CLI 설명](https://docs.unity.com/en-us/hub/cli-overview)을 확인하고 `install-modules --version 6000.6.9f1 --module ios`만 실행했다. exit0, installed successfully/All Tasks Completed Successfully, iOSSupport 약2.7GiB, modules selected=true다. Android·기존 SDK·계정·개발 서명/pairing은 변경하지 않았다.

공식 URL 다운로드874745588bytes, Hub 선언 MD5와 별도 재다운로드 MD5 일치, SHA256 `0d11e375afc049d7840f4da74e58a203ab2edcfc92b8d8b224b9c230d6c487d8`를 남겼다(`module-download-check.json`). 기존 Editor/모듈을 삭제하거나 버전을 바꾸지 않았다.

`AdamMobileBuild.BuildIosSimulator`는 `.adam-mobile-qa` marker가 있는 격리 Assets/Packages/ProjectSettings 사본만 허용한다. 새 절대 출력 경로를 요구하고 기존 출력은 거절한다. QA 전용 앱ID·IL2CPP·SimulatorSDK·ARM64·자동 서명 꺼짐·개발 팀 없음·회전 후보를 해당 사본에서 설정한다. 실제 BuildPipeline 실패면 예외로 비0 종료한다. 원본 프로젝트의 타깃/출시 방향을 확정하지 않는다. [Simulator SDK 설정](https://docs.unity.com/en-us/engine/6000.6/script-reference/unityeditor/playersettings/ios/sdkversion).

최종 `export-final-command.json`/`export-final.log`는 exit0, Exiting batchmode successfully다. Xcode26.2/build17C52에서 실제 조회한 Unity-iPhone scheme·iphonesimulator26.2 SDK·generic iOS Simulator destination·새 DerivedData·ARCHS=arm64·CODE_SIGNING_ALLOWED=NO/CODE_SIGNING_REQUIRED=NO로 `xcode-build-final-command.json` exit0, BUILD SUCCEEDED다. Info.plist는 iPhoneSimulator/최저15.0을 표시한다.15.0 실물 호환을 검증한 것은 아니다.

서명 비활성화는 **개발 인증서/프로비저닝/팀을 사용하지 않았다는 범위**다. 실제 arm64 Mach-O에는 Apple 링커가 만든 `adhoc,linker-signed` 서명이 있으며 TeamIdentifier는 not set이다(`app-platform-inspection.json`). 서명 바이트가 전혀 없다고 보고하지 않는다. 계정/스토어/실물 서명 완료가 아니다.

## 실제 저장 실패와 좁은 linker 수정

초기 export/build/install/launch/render가 exit0이어도 native 시작 선택의 첫 Commit은 실패했다. 개발빌드에서만 전체 예외/stack을 기록해 다음을 확인했다. Core/serializer/저장 형식이나 실패 시 정지 정책은 바꾸지 않았다.

| 빌드 | 실제 동작 |
| --- | --- |
| 보존 없음 | `adapted-app-stdout.log`: WriteObject→Encode에서 내부 GenericDictionaryEnumerator의 생성자 MissingMethodException, Commit 파일 없음 |
| writer 타입만 보존 | `linker-app-stdout.log`: 쓰기는 지나고 Decode→StoreCollectionValue에서 Key/Value DataMember 목록 index 예외, Commit 파일 없음 |
| 두 타입 보존 | 최초 Commit·반복 Commit·File.Replace/backup·강제 종료 후 Load/실제 재개 PASS |
| writer만 제거 변이 | `drop-writer` export/Xcode exit0 뒤 실제 native Commit이 생성자 MissingMethodException으로 FAIL |
| reader만 제거 변이 | `drop-reader` export/Xcode exit0 뒤 실제 native Commit이 StoreCollectionValue index 예외로 FAIL |
| 정상 앱 재설치 | `restored-sling-save.json`: 실제 물맷돌 시작·정지로 최초/재Commit revision1→2 PASS |

`Assets/Adam/link.xml`은 System.Runtime.Serialization 어셈블리의 다음 **두 내부 타입만** 보존한다. 어셈블리 전체 보존이나 저장 우회/인메모리 대체는 없다.

- ``System.Runtime.Serialization.CollectionDataContract/GenericDictionaryEnumerator`2``: reflection 생성자와 열거 멤버.
- ``System.Runtime.Serialization.KeyValue`2``: dictionary reader가 반사하는 Key/Value 멤버.

[Unity linker의 nested/generic 형식](https://docs.unity3d.com/cn/6000.0/Manual/managed-code-stripping-xml-formatting.html)을 따른다. 두 제거 변이의 실제 예외/앱 PID/파일 부재는 `linker-mutation-results.json`, 각 빌드 SHA는 `ios-variant-build-sha.json`이다. 컴파일 오류를 행동 변이 실패로 세지 않았다.

최종 Unity 로그는6000.6.9f1 / IL2CPP / Minimal stripping / SimulatorSDK / ARM64다. 내부 BCL 이름에 의존하므로 Unity/Mono/IL2CPP/stripping 변경 때 실제 재검증한다. 더 높은 stripping·Android/WebGL·다른 Unity 버전은 이 PASS에 포함하지 않는다. 최종 QA/원본 link.xml은 두 타입 보존 상태와 byte 동일하다.

## 모바일 표현과 입력

원본 PNG/font 변경 없이 `AdamMobileLayout`과 `AdamGame`의 mobile 렌더 분기를 추가했다. 정수 UI 배율, safeArea 내부 반응형 모달/시작·재개/오류 버튼, 두 줄 효과 카드와 모바일 안내를 사용한다. 터치 시작은 같은 safeArea의 왼쪽 아래에서만 받고 이동 거리는 UI 배율을 반영한다. 회전/safeArea 변경 때 이동을 지우고 명시적 재개를 요구한다. 기존 `SelectSlot` 사망 연출 공통 가드는 유지한다.

관측 profile은 iOS Simulator26.2의 iPhone17,1206×2622px/3x다. portrait safeArea=(0,102,1206,2334), LandscapeRight는2622×1206px/safeArea=(186,60,2250,1146)이다. 실제 `ADAM_DISPLAY` 로그와 설치된 device type의 mainScreenScale=3을 대조했다. UI 정수2배에서 logical viewport는603×1167 /1125×573이다.

66logicalpx 버튼 높이=132px=44pt, 카드 높이110=약73.3pt다. 제목/버튼24px×2/3=16pt, 본문/모바일 안내20px×2/3=약13.3pt다. [Apple 게임 UI 지침](https://developer.apple.com/design/human-interface-guidelines/designing-for-games)의 기본17pt/최소11pt와44pt 버튼 기준에 대조했다. 최소 크기 대조이며 실물 가독성/접근성 인증이나 Dynamic Type/VoiceOver 지원을 뜻하지 않는다. 더 좁은 profile은 배율1로 내려갈 수 있어44pt를 보장하지 않으며 별도 M3 게이트다.

실제71개 C# 성장 문자열을 원본과 대조하고 portrait 카드의 이름 한 줄·효과 두 줄 상자에 들어감을 EditMode로 확인했다. native 자연 LV2의 crown/underwear/east_wind 카드도 portrait/landscape에서 한국어 종류·효과·0→1/5를 읽을 수 있었다. 새 그림/폰트 제작은 필요하지 않아 원본03/outline을 그대로 재사용했다. 전체 한글 glyph/AA의 상세 검사는 기존 Editor 범위이며 iOS에서는 관측 UI 문자열의 렌더 범위만 주장한다.

## 실제 조작·저장 증거

custom device set에서 build/install/launch/PNG는 성공했으나 새 GUI 프로세스는 즉시exit0이며 창이 생기지 않았다. 기존 Simulator를 종료하지 않고 coordinator 승인으로 default set에 **새 QA 기기**를 만들어 별도 창을 얻었다. 이후 simctl은 QA UUID만, native action은 fresh 목록의 QA 창ID만 지정했다. 기존 기기/창/앱/저장에 action을 보내지 않았다.

| 관측 | 근거와 범위 |
| --- | --- |
| 세 시작 무기 | native 성경·턱뼈·물맷돌 선택 및 저장/표현 확인; 관련 PNG/각 단계 앱 SHA·로그 보존 |
| 이동·정지/재개 | `native-drag.json` 뒤 QA World.PlayerX 0→약0.25; 실제 모달 재개/정지로 시각 진행/정지. 선택/복귀 뒤 위치가 잔류 이동하지 않음 |
| 자연 성장 | 턱뼈 플레이18.016…초의 자연 LV2 모달을 저장. portrait/landscape 카드 확인 후 터치2로 underwear1만 적용, ChoiceSerial 한 번 증가 |
| SIGKILL→Load | 장비·성장·좌표·건강·골드·RunId·시각의 저장 전체 동등. 실제 새 프로세스에서 명시 재개 뒤 전투가 진행됨 (`growth-reload-check.json`) |
| 백그라운드 | QA Home→기존 앱 동일PID 복귀. Run/Profile exact, 전투 시각 불변, 재개 모달 표시. 재개 후 시각만 진행하고 held 이동 없음 (`background-check.json`) |
| 회전 | portrait↔관측 LandscapeRight, safeArea 갱신/명시 재개/Run 불변. 중간 뒤집힌 자세는 UI가 이전 가로 방향을 유지했으며 정상 portrait로 돌아온 뒤 검사; upside-down/다른 방향 지원 PASS로 세지 않음 |
| 실제 쓰기 실패 재시도 | QA lock 파일 권한 오류에서 기존 Commit 파일 불변·전투 정지. 권한 원복 뒤 native 재시도 한 번만 revision+1, 두 번째 클릭 추가 저장 없음 (`transient-retry-check.json`) |
| 손상 원본/백업 | 손상 primary→유효 backup Load→명시 재개/재Commit. 정상 backup이 손상 primary로 덮이지 않음 (`backup-recovery-check.json`) |
| 미래 schema | schema2 primary+유효 backup도 거절, Retry/Reload 후 두 파일 불변. 최종 빌드의 한국어 업데이트 안내·재시도 불가 상태 확인 (`final-future-check.json`) |

최종 SHA의 앱에서도 QA 자연 LV2 저장을 다시 불러와 터치2→SIGKILL→Load→실제 재개를 실행했고, 장비/성장 보존과 이동 전파 없음을 `final-load-check.json`으로 확인했다. 최종 권한 실패→native 재시도는 `final-transient-retry-check.json`이다. 이러한 재현용 fixture는 **본인이 만든 QA 컨테이너에만**, 앱 종료 후 썼다. PC 저장에는 쓰지 않았다.

실제 Core 접촉 사망 fixture의 최종 native 영상은 `final-native-death.mov`다. 추출한 `death-active-frame-04/07/10.png`에서 사망 포즈 진행→쓰러짐→종료 모달이 보이고, clip 뒤 클릭은 새 성경 Run을 정상 시작했다. 이 fixture는 기존 Core로 생성된 전투에 체력1/접촉 위치를 배치한 경계 시험이며 자연150초 생존/밸런스 플레이가 아니다.

**native0.75초 안의 터치 차단은 NOT_TIMED**다. no-screenshot 호출도 약1.8초 왕복이어서 원래 hit rect의 다음 클릭이 이미 clip 뒤였다. clip 중 도착한다고 가정했던 probe의 Run-null assert는 실패했으며 이를 게임 결함/차단 PASS로 세지 않았다(`death-native-action-times.json`, `death-timing-assessment.json`). 정확한 공통 경계는 유지된 실제 InputSystem digit1/2/3·동시/hold/앱 중단·clip 종료 후 선택 및 실제 전체 renderer 마우스 회귀로 검증했다. native 단일 손가락을 실물 멀티터치로 전용하지 않는다. 후속으로 단일 `click-count 2` 요청을 Resume/새 도전 두 번째 버튼이 겹치는 위치에 보내자 종료 Run null·골드39가 유지됐고, 이후 새 클릭은 턱뼈 Run을 시작했다(`double-death-check.json`, `native-double-death.mov`). 이는 native 연속 탭 요청의 상태 관측이며 개별 touch 전달 시각을 계측한0.75초 타이밍 증명으로 확대하지 않는다.

## 최종 소스·회귀·보존

`release-pc-compile` exit0, `release-pc-EditMode/results.xml`42/42, `release-pc-PlayMode/results.xml`15/15, skipped0·각 Unity exit0, `release-pc-build-command.json` macOS BuildMac exit0다. 기존39/13에 mobile geometry/71문자열, 실제2배 UI 시작/정지/재개 클릭, 실제 InputSystem 두 손가락/취소 회귀를 더했다. 미래 schema/재시도 타입 상태도 기존 Host 시험에 추가했다. PC UI-01/02·사망 digit·중복/hold 회귀는 유지한다.

별도 복사본에서 mobile 렌더를 원래 PC 구조로 되돌리면 실제 scaled 시작 클릭 시험이 실패(exit2)하고 원복하면 통과(exit0)했다(`mobile-layout-mutant/restored-results.xml`). 로컬·portable checker도 최종exit0다. 이 검사는 Simulator native 시험을 대신하지 않는다.

새 iOS 모듈을 읽은 재임포트가 canonical PNG79개에 default iOS/overridden:0 블록만13줄씩 추가했고 ProjectSettings에는 빈 iPhone 아이콘 슬롯만 생겼다. 원문 diff를 보존하고 GUID/rect/pivot/border 불변을 확인했다. **본인 자동 추가분만** 정리해79개 meta와 canonical ProjectSettings를 HEAD byte와 동일하게 복원했다. 의미 있는 모바일 설정은 새 export 진입점과 격리 QA ProjectSettings에 보존했다. 새 canonical Editor 실행 시 이 기본 직렬화가 재발할 수 있다.

`final-source-settings-app-sha.json`은 마지막 Adam 소스/시험/meta·PC DLL·iOS 앱36파일·격리 설정을 포함한277파일 SHA다. 최종 실제 설치 앱36파일도 빌드와 모두 동일하다(`installed-final-build-sha.json`). 수정하지 않은 OFL 원문 payload도 앱의 Data/resources.assets에서 확인했다(`font-notice-in-app.json`). PNG/TTF83개의 원본 bytes/GUID/meta는 기존 등록과 같고 원본03/font/Core는 최초 해시와 같다. Core/기존 CI 검사기·hook은 수정하지 않았다. 이 해시 뒤 코드/설정/빌드를 바꾸지 않았다.

QA 앱 terminate와 default/custom QA 기기 shutdown을 실제exit0으로 끝냈다. 기존11기기의 상태와 원래 Booted1·Simulator GUI 프로세스/기존 창은 유지했다. PC 저장6개는 이전 원복 원본 SHA/존재와 모두 같다. `final-cleanup.json`, `final-existing-simulators-preserved.json`, `final-process-release.json`, `pc-saves-preserved.json`이 근거다. 개인 원문/경로는 ignored evidence에만 있다.

독립 리뷰 재실행은 `private-review-handoff.json`의 정확한 QA UUID/앱·컨테이너·최종 빌드/로그·boot/launch 명령을 사용한다. 재부팅 뒤 QA 창ID는 fresh list-windows로 다시 찾아 지정하며 기존 창에는 action하지 않는다. 개발 빌드의 의도적 저장 실패 로그/URP 경고도 보존했다. Simulator에서 일부 URP 후처리/셰이더 지원 경고가 있으나 관측 IMGUI/PNG/한글을 막지는 않았다. 이를 모든 모바일 셰이더·성능 지원으로 세지 않는다.

독립 코드 리뷰(`../reviews/mobile-ios-code.md`)와 실제 Simulator UI 리뷰(`../reviews/mobile-ios-ui.md`)는 PASS이며 추가 HIGH/MEDIUM은 없다. 코드 리뷰는277/277 소스·설정·빌드 및 설치 앱36/36 SHA를 확인했고, UI 리뷰는 지정 profile의 세로/가로 터치·자연 성장·중단/재실행을 직접 검증했다. UI 검수 종료 시 QA3/PC6 저장9개를 보존·복원했다. 프로젝트 GC는 `../retrospectives/R01-mobile-ios.md`에 기록했다. 선별 commit/push/해당 SHA CI 결과는 후속 기록으로 남긴다.

사용자 직접 보기 요청 후 QA Simulator와 정상 앱을 다시 실행했다. 실제 표시 화면은 원본 정상 저장의 레벨업 선택이며 재개 모달이 아니다. 사용자가 플레이 중이므로 자동 GUI 입력·종료·과거 QA 저장 원복을 중단했다. 전시 인계는 ignored `evidence/mobile-ios-user-display-20261114/`에 보존한다. 검수 시점의 종료·복원 증거와 이후 사용자 플레이 상태를 구분한다.

native clip 중 정밀 터치·다른 작은폰/방향·실물 멀티터치/전화/잠금/전원 손실·모바일3분 완료/보상 경계 실플레이·전체 glyph on-device·장기 성능/발열/메모리·Android는 NOT_RUN/별도 게이트다. M0/iOS Simulator 일부 PASS를 전체 모바일 지원 완료로 올리지 않는다.
