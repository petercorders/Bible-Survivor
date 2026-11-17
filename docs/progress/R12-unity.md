# R12 Unity 통합

2026-11-18 KST. **통합 작성자 기능·대표 화면 확인 PASS, 최종 HUD delta와 binding 독립 코드 리뷰 PASS. 지정3화면 독립 UI 리뷰 PASS.** Core191/관련10·원구조 swept/end-cooldown 변이·독립 Core/art PASS는 재사용했다. 실제 좌표 CLI147샘플에는 telegraph가 없으므로 그 샘플 자체를 예고 검증으로 세지 않는다. 별도 Core phase 시험과 이번 Unity 실제 phase Load를 구분한다. Core 제품·수치·저장 형식·CI checker·원본 아트는 수정하지 않았다.

Host는 AwaitingR12와 R11 between12→R12 일반 전환을 추가한다. Catalog는 R12 normal/intro/boss/reward/settlement 및 완료 between13을 해석하고 ex7/8/9 속도·ex9 명시 돌진 behavior·전차장 정의를 Core와 대조한다. R10/R11 fingerprint와 R09→R10 후보/고정 operation/revision·Commit/Load/Resume 뒤 게시 원자성은 유지한다. R12 Boss4/World5와 현재 보스·R03/R06/R09 세 과거 receipt, RunId·장비·진화를 재개하며 설치 콘텐츠 끝은 R13 미리암 대기다. 명시 새 도전/사망은 R01/schema2이고 영구 프로필을 보존한다.

반환 UI/World를 통합했고 승인9PNG/29frames를 실제 Reimport/ValidateBuild했다. 이중 import의 sprite name/GUID/localID/rect/pivot29개가 같다. Core live ChariotDash의 원점/방향/남은 거리·실제 tuning/radii로 lowFX 방향 lane을 그리며 Unity에 판정/고정거리/새 공격 시계를 추가하지 않았다. 일반 ex9 WorldTime과 보스 BossElapsed를 분리한다. 실제660에서 보스 HUD 한줄 잘림을 발견하여 R12만 name/HP와 phase를16px 두줄로 나눴다. 이어 폼 행이 배우 머리와 겹쳐 R12 보스 때만 기존 하단 이동 힌트 행을 폼 정보로 재사용했다. 입력 동작/F6 안내는 유지하며 다른 라운드/normal/보스 표현은 변경하지 않았다. 정확한 최종 한 파일 delta는 `post-review-ui-layout.diff`다.

sync1회/compile/importer/project·portable는 exit0, DLL SHA256 `759b580f2300fddd9f1ad37803c2edbae1320d44d570e84840892ffe1d15db1b`, provenance schema2다(root save3과 별개). 관련 Edit는 고유70건으로 최초69 PASS/1 FAIL와 교정 후 해당1 PASS를 합친 결과이며 단일70/70 XML이 아니다. 신규 Host6/UI14/World5와 관련 R09~R11 Host/UI·depth/hitflash를 포함한다. 최초 phase fixture는 가까운 live 보스에서 view null이었다; 보스를30으로 격리한 후 지원 생성 뒤.01초가 fixed tick보다 짧아 World 복원 비교가 달라지는 두 번째 fixture 실패를 확인했다. 준비를.1초로 바꿔 해당1건만 최종PASS했고 두 exit2/XML을 보존했다. Play12/12는 실제 R11→R12 Continue 클릭/Enter와 기존 chapter entry/suspend/같은 frame 중복/설치 끝 경계를 포함한다. 나머지 과거 Host시험의 미지원 경계만13으로 옮겼으며 컴파일과 별도로 전체 실행을 주장하지 않는다. HUD 표시만 바뀐 뒤 전투/저장 suite는 반복하지 않고 최종 정상 Player 빌드로 컴파일했다.

최종 앱은 `evidence/r12-unity-integration-20261118/delivery/build/R12Qa.app`이며 identity/실명령/exit는 delivery/run.json·build-command.json·player-commands.json에 있다. 최종93개 입력/QA 복사본과 앱 Core DLL이 일치한다. 최초 시험 당시 입력과 최종 입력의 차이는 AdamGame 표시1파일뿐이며 두 입력 hash와 exact delta를 보존한다. 기술 seed는 이전 자기 R11 완료 evidence에서 가져와 HP10000/무기8/진화3/6슬롯을 사용하되 **cloud5→4를 처음 저장하기 전에 기술 조정**했다. 원래 만석·전고갈 seed의 자동100 정책과 실제 카드 선택 경로를 구분하기 위함이며 자연 장비 유지 검증으로 확대하지 않는다. 세 receipt/forms/RunId와 조정된 seed의 진입 전후 프로필은 유지한다.

최종 한 trace에서 R11 완료 무쓰기 Load→R12 명시 Resume(200 재지급 없음)→일반 timeout·injury20→실제 보호 intro2초/조기 회복 없음→boss position9/cycle1000 기술 준비→실 BossElapsed5.1 telegraph disk Load→직접 Hit threshold 지원6/12→BossElapsed6.1 dash live 이동/고정방향/남은 거리 disk Load를 확인했다. 예고·돌진 모두 pause 동안 World5와 Boss4를 정확히 재개했다. lethal Hit 뒤 경고 즉시 제거, 다음 Core advance에서 배우/돌진 cleanup과 고정400을 확인했다. dash 표본6.5→6.1은 QA 시점 교정이며 보스 clock/판정 변경이 아니다.

후보2개를 Load해 ID/내용·무추가지급 동등을 확인하고 고정 gold100을 선택했다. 기술 PendingLevels2에 실제 Choose2(구름4→5 한 번, full serial growth25 한 번)를 기록했고 후보 수와 pending/실제 호출 수를 섞지 않았다. 선택 저장 실패→동일 retry 및 완료 저장 실패→동일 retry 중 메모리는 정지하고 각각 중복 지급을 막았다. gold2841→3241(+400)→3341(+100)→3366(+25)→3586(+220), null 해금·최대HP회복·between13을 확인했다. 새 process의 완료 Run/Profile/currentBoss4/세 history 정확 Load와 무중복220·자동R13/초기화 없음, 명시 새 도전 R01/schema2까지 확인했다. gold-only 자동100은 별도 Host 시험에서도 PASS다.

정상 mac 빌드는 총5회였다. 첫 앱은 seed key LastOperationId를 LastOperation으로 잘못 써 초기 Commit에서 exit2였다. 두 번째는 이미 완전히 고갈된 원 seed에 카드 화면을 기대한 fixture 오류로 exit2였고 Core의400+자동100은 정상 정책이다. 세 번째는 기능/Load exit0이나660 HUD 잘림으로 표시 FAIL, 네 번째는 기능 exit0이나 내려놓은 폼 줄이 배우와 겹쳐 표시 FAIL이었다. 다섯 번째 delivery가 최종이며 앞선 앱/로그/trace/이미지/각 자기 저장 복구를 모두 보존한다. 첫 seed 오류 뒤 build 전 Core.Validate를 추가했다. 모든 앱은 source의 정상 빌드이며 번들 손수 패치는 없다. 마지막 표시 교정 외 Core/Host/World/DLL/provenance는 고정했고 전 suite/추가 조합은 실행하지 않았다.

최종 실제 열람 대표는 delivery/capture의 **660 lowFX 보스 dash, 1100 고정 보상, 1100 완료 R13 대기3장**이다. HUD 잘림/폼-배우 겹침이 사라지고 실제 남은 방향 lane·지원군이 보인다. 자연1020kill/180초 밸런스·앱 R01~R12 연속 완주·native/device/mobile/performance는 NOT_RUN이다. 기술 HP/장비/좌표/Hit/timeout/pending 주입을 자연 검증으로 세지 않는다.

매 앱은 새 독점 suffix/no-Host pathprobe/정확 realpath·자기 child PID를 기록했고 마지막 probe/capture/reload 모두 exit0이다. 각 runner의 restoration.json은 자기 main/bak/lock을 원래 absent로 복구하고 자기 writer 부재를 확인한다. 보호 pivot9534f7594886/unknownparticlesqa/iOS 및 해당 저장은 조회·조작하지 않았다. 자기 Editor가 종료된 뒤 원본 Art03 meta79개의 자동 기본 Android/iOS block과 빈 icon 설정만 exact HEAD 바이트 확인 후 복구했으며 사전 diff/hash를 보존했다. 다른 작성자 편집은 되돌리지 않았다.

`binding.json`, `verification-index.json`, `fixed-product-source*.json`, `usage.json`이 제품·앱·DLL/provenance/catalog·XML/실명령/29 stable 등록·최종 trace/3화면·모든 실패/보존을 연결한다. requested Sol 기본/observed gpt-6.1-sol, Claude/Astra/helpers 없음. 자기 start/end provider interval은 input12350132/cache12267776/output45625/reasoning18890/total12395757이며 마지막 보고 tail을 제외한다. 시작의 이전 phase delta는 R12로 합산하지 않는다. effort/실청구·단가/절감률은 미측정이다. 제품·Editor·Player 소유를 반환하며 root 독립 최종 code/UI·GC·commitpush/CI 및 R13 인계가 남는다.

Root 마감: 독립 코드·UI 리뷰 모두 PASS이며 막는 HIGH/MEDIUM은 없다. UI R12-UI-01은 원본의 +220/홍해 건넘을 +200으로 오독한 것으로 같은 이미지 재열람 뒤 철회했다. 제품/이미지 수정이나 여섯 번째 빌드는 없었다. root Resume 상한11→12 선택 변이1건은 원거절 의미를 유지하며 실제 검출 exit0으로 확인했다. 필수 커밋 훅과 해당 SHA 원격 CI 결과는 업로드 뒤 기록한다.

첫 업로드9f711bf 원격CI: art44124668750 성공, Core44124668801은 미래World 회복 변이 미검출로 실패했다. 테스트의 미래World값5가 현재지원5와 겹쳐 두 입력만6으로 갱신했고 선택변이1건은 실제 검출 exit0이었다. 게임/Core제품/Unity앱 변경은 없으며 최종복구SHA CI를 별도로 확인한다.
