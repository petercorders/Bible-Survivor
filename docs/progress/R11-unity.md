# R11 Unity 통합

2026-11-18 KST. **통합 작성자 및 독립 최종 code/UI 리뷰 PASS.** [코드 리뷰](../reviews/R11-unity-code.md)·[대표 화면 리뷰](../reviews/R11-unity-ui.md)에 근거와 한계를 기록했다. Core181·원구조 변이·실좌표 R11 CLI 960kills/중간 Load 및 독립 Core·아트 PASS는 재사용했다. 이 작성자는 Core 제품·수치·저장 형식·원본 아트·CI checker를 수정하지 않았다. root의 변이 anchor 갱신과 해당 선택1건 실제 검출은 별도 소유이며 기존18건 전체 재실행으로 세지 않는다.

AdamHost에 AwaitingR11/기존 AwaitingNextRound의 R10 between11→R11 일반 전환을 연결하고 다음 World를 해당 라운드 SpeedsFor로 만든다. AdamCatalog는 설치 끝11, 독립 R10/R11 속도 집합, ForRound11 및 between12 checkpoint를 지원한다. R10 fingerprint `IunkZmlC50vfCA3mEtdpX5iiLjkDjk2gkFVsEXpZF9A=`는 유지한다. R09→R10 CanEnterExodus/TryEnterExodus의 후보·고정 operation/revision·Commit/Load/Resume 뒤 게시 계약은 변경하지 않았다. R11 일반 진입 저장 실패는 한 arena를 정지하고 같은 payload를 재시도하며 기존 R10 완료180을 다시 지급하지 않는다.

R11 완료는 ContentComplete/between12·R12 대기이며 Genesis ChapterComplete와 구분한다. 200/null 해금/최대HP회복을 Core로 한 번 정산하고 세 boss receipt·RunId·장비·진화·프로필을 유지한다. 명시 새 도전 및 사망 재시작은 R01/schema2다. 반환된 UI는 요게벳/출애굽기2 및 ex4/ex5/ex6, wide/small/wide 그림자를 표시한다. 승인된 반환 월드3 PNG/12frames를 Reimport/ValidateBuild했고 이중 import의 stable sprite name/GUID/localID/rect/pivot12개가 같았다. 원본03을 교체하지 않았다.

sync1회·compile/importer·관련 Edit48/48·Play 최초10/10 및 입력 경계 보완2/2·project/portable·diff exit0. Edit는 신규 Host5/UI8/World3과 R09/R10 Host/UI·depth/hitflash를 포함한다. Play는 실제 InputSystem Enter 및 일반 라운드 전환/오류·suspend/같은 frame 중복·설치 끝 새 진입 차단을 포함한다. 최종 근거 대조에서 R10→R11 경계를 Host로만 진행한 빈틈을 발견하여 기존 Enter 시험의 해당 호출을 실제 키 이벤트로 바꾸고 실제 Continue 클릭1건을 추가했다. 이2건만 추가 실행2/2 PASS이며 전체 Play는 고유11건이다. Player에서 제외되는 시험 소스만 바뀌었고 제품/앱 재빌드는 하지 않았다; 원래87입력 기록과 post-build-test-only.json을 함께 보존한다. 미지원 경계만 옮긴 다른 과거 Host시험은 컴파일됐으며 전체 suite 재실행으로 세지 않는다. 근거는 `evidence/r11-unity-integration-20261118/verification-index.json` 및 실제 XML/명령/log다. Core DLL SHA256 `78947c476da41402f11b3f4b9b2edd82fac131b0dc9f19123b557d4ee67dc731`, provenance schema2이며 root save3과 별개다.

최종 정상 Development mac 빌드 **1회**는 `evidence/r11-unity-integration-20261118/build/R11Qa.app`, bundle `com.biblesurvivor.adam.r02artqa.r1199133a6c389b`다. 제품87개 소스/DLL 입력의 고정본과 QA 복사본이 같다. 정상 빌드 앱의 Core DLL도 원본과 같다. QA는 이전 자기 R10 완료 evidence를 seed로 사용했다. HP10000/무기8·진화3/보조6슬롯, timeout/directkill/drop 폐기/weapon cycle1000은 기술 주입이다. QA 시작 전에 예상 저장 실패의 Development Console 팝업만 끄고 원래 예외 로그는 남겼다. 앱 손수 패치·추가 빌드·새 조합은 없었다.

한 trace에서 R10 완료 무쓰기 Load→일반 R11 진입 저장 실패/동일 retry→실제 Core ex4/ex5/ex6 추적3초 및 World3 pause/disk Load→lowFX 토글 후 동일 World→성장/정산→새 process 완료 정확 Load를 확인했다. 일반 고갈 후보1/PendingLevels1/실제 Choose1로 +25, 완료 후보1/PendingLevels1/실제 Choose1로 +25 뒤 200/null 해금/회복을 확인했다. full serial ID를 그대로 선택하며 Unity 별도 골드 지급은 없다. gold2464→2576(기술 준비 처치 포함)→2608(추가 처치·25 포함)→2841(처치·25·완료200 포함)이다. 새 process에서 Run/Profile·세 receipt/forms 정확 동등, 중복200/자동R12 없음, 명시 새 도전 R01/schema2/프로필 보존을 확인했다.

실제 열람한 대표는 capture의 **660 요게벳 normal, 660 같은 World lowFX, 1100 완료 R12대기3장**이다. 실제 추적은3초이며 자연960kill/자연180초 완주·앱 R01~R11 연속 완주·native/device/mobile/performance는 NOT_RUN이다. probe/capture/reload PID23229/23354/23684는 모두 exit0, 정확 자기 저장 realpath와 no-Host probe 뒤 main/bak/lock 부재 및 자기3파일 absent 복구는 restoration.json에 있다. 보호 preview/unknownQA/iOS·해당 저장은 조회·조작하지 않았다.

모든 자기 Editor가 종료된 뒤 자동 Art03 meta79개의 기본 Android/iOS block, 빈 platform icon 확장, Editor welcome flag만 exact HEAD 바이트 동등을 전제로 복구했다. 사전 diff/자동 import 복사 hash·복구 hash를 보존했고 사용자 아이콘/다른 작성자 편집은 바꾸지 않았다. 설정/문서 때문에 재빌드하지 않았다.

binding.json/fixed-product-source.json은 소스·앱·DLL/provenance/catalog·명령/XML·등록·trace/3화면·보존 근거를 연결한다. 자기 usage phase start/end의 관측 모델은 gpt-6.1-sol이다. 공급자 interval input8321274/cache7921664/output35390/reasoning12822/total8356664는 지연된 시작 event 때문에 R11 전용 사용량으로 단정하지 않으며 마지막 tail을 제외한다. Claude/Astra/추가 worker 없고 effort/청구/절감률은 미측정이다. 제품·Editor·Player 소유를 반환하며 root의 독립 최종 리뷰/GC/commitpush/CI가 남는다.
