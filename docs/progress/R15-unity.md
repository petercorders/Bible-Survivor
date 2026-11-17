# R15 Unity 최종 통합

2026-11-18 KST. **작성자 검증과 최종 독립 코드·UI 리뷰 PASS. 프로젝트 GC를 마쳤으며 최종 commitpush/해당 SHA CI는 후속이다.** Core211/관련8·원 endpoint swept 변이·root 선택 Resume15 변이·독립 Core/art PASS와 실제 좌표 R09→R15 CLI를 재사용했다. CLI의 R15 기술1097/1200 kills·관측 최대 적탄6을 전처치·자연 밸런스·cap12 실측으로 확대하지 않는다. Core 제품/수치/저장 형식·원본 아트·CI/checker는 이 통합자가 수정하지 않았다.

Host는 AwaitingR15/R14 between15→R15 일반 전환을 연결하고 `ExodusComplete => Session?.ExodusComplete == true`를 공개한다. ContentComplete는 설치 끝, ChapterComplete와 CanEnterExodus/TryEnterExodus는 기존 R09→R10 의미를 유지한다. 별도 후보와 고정 operation/revision→Commit/Load/Resume 후 게시하는 R09 진입 원자성은 불변이다. Catalog는 ForRound15/SpeedsFor15와 normal/intro/boss/reward/settlement/between16을 지원하고 R16 전투·R15 round_settlement는 거절한다. R12 보스와 R15 보스를 각각 canonical 동적 규칙으로 검증하며 R10~R14 fingerprint를 보존했다. R15 현재 Boss3/World4와 과거 R12 Boss4/World5를 혼동하지 않는다.

UI/World 별도 작성자 반환과 Core/art 독립 PASS 뒤 단독 sync1회·실제 importer/compile·관련 검사·정상 mac Development 앱을 실행했다. 신규 Host6/UI18/World5를 포함한 관련 Edit131은 최초130 PASS/1 기술fixture FAIL이고, 교정된 보상 메서드2케이스 PASS로 결속했다. 단일131/131 XML로 보고하지 않는다. Play14 PASS이며 최종 R15 Enter 후 완료·Enter 무진입을 포함한다. project/portable 검사 exit0이다. 첫 compile의 신규 시험 `HostileProjectileState.Position` 오참조는 실제 X/Y 필드로 교정했다. 비고갈 Host seed는 Session.Capture 전 cloud4를 넣어 덮어쓴 준비오류였고 Capture 뒤 commit 전으로 옮겼다. Runtime/Core 교정은 없었다. 원 compile/log/XML/exit1·2와 교정 근거를 보존했다.

최종 앱은 `evidence/r15-unity-integration-20261118/review-build/R15Qa.app`, fresh suffix `r1580111c7154f5`이다. 최종 Core DLL SHA256은 `e40985359fe9d367c14f8e19e990e20613e80e6e21b6815ed1a4743ca791eeaf`이며 source108·QA 복사·앱 DLL/provenance/catalog와 27 stable sprite 등록을 binding.json에 결속했다. 승인8PNG27frames/20원본은 art-binding.json으로 기록했다. 최종 정상 build exit0, no-Host probe PID75823/기록 PID75825/새 process Load PID75834 모두 exit0이다.

정상 mac build는 총2회다. 첫 앱은 실제 예고/실탄 Load와 지원6/12 뒤 direct Session.Hit 직후 World 적탄 목록까지 즉시 empty여야 한다는 QA 기대에서 exit2가 났다. 실제 World lifecycle은 다음 Advance에서 ClearEnded를 적용하고 stage가 이미 warning/탄 표시를 숨기므로, 기존 World 회귀처럼 즉시 Telegraphing false→World.Advance(0)→적탄/actor empty로 driver만 교정했다. 원 앱/driver/FAIL 화면/rawlog/자기 저장 복구를 initial-failure/에 보존했다. 재준비 중 이전 QA 씬에 캡처 컴포넌트가 남아 prebuild gate exit1도 있었으며 BuildPipeline 실행 전이다. 원 제품 씬을 격리 QA에 다시 복사해 해소했으며 앱을 산출하지 않은 이 호출을 정상 build로 세지 않는다. 총 Editor build 호출3, 정상 앱2를 구분한다. 교정 앱은 새 identity/새 절대 출력에 정상 빌드했으며 번들을 손으로 패치하지 않았다. 추가 suite/조합/성공 화면 확대는 없다.

한 기술 trace는 자기 R14 완료 seed를 재사용했다. HP10000/무기8/3진화/6슬롯은 이전 기술 seed에서 상속하며 실제 보상 카드를 위해 **cloud5→4만 사전 조정**했다. 이는 자연 장비 유지 검증이 아니다. 네 과거 receipt의 필수 진화는 제거하지 않았다. build 전 정확 DataMember/Core.Validate와 같은 seed의 실제 처치→후보2/gold100→PendingLevels2·Choose2(cloud5 이후 full-serial25)→500/280/ExodusComplete 분기를 사전 확인했다. 가능 진화 우선은 기존 Core 근거를 재사용하며 이 seed에서 새 진화를 관측했다고 주장하지 않는다.

실앱에서 R14 완료 무쓰기 Load→명시 R15 진입/RunId·프로필·네 receipt·3진화 유지/260 무재지급을 확인했다. 정상180초 timeout·Damage20 요청(방어 적용 후 관측 HP9985)·보스 위치9·무기 cycle1000은 기술 준비다. 실제 intro2초 뒤 BossElapsed4.2의 저장 3방향/live 투영·lowFX를 관찰하고 World4 disk Load/pause exact를 확인했다. 5.2초 실제 적탄3발의 disk Load, BornAt 유지와 다음 tick 실제 이동, 직접 Hit 임계값 뒤 지원6/12·mask1/3 Load, lethal Hit 뒤 World.Advance(0)의 탄/actor 정리를 확인했다. cap12 실측이나 자연 보스 처치가 아니다.

실제 보상은 고정500으로 gold4488→4988, 동결 후보2개(구름기둥4→5·골드100)의 무지급 Load, 실제 골드 후보 선택 once로5088, 기술 PendingLevels2의 실제 Choose2 중 item cloud5 한 번·full-serial25 한 번으로5113, 별도 완료280으로5393이다. 후보 개수와 pending·실제 Choose 횟수를 구분한다. 보상과 완료 beforeReplace 실패 뒤 메모리/금액/전투 정지·동일 retry와 중복 지급 거절도 실제 앱에서 확인했다. Host 시험은 만석 자동100 분기를 별도 확인했다. 완료는 회복10000/null 해금/현재 Boss3+네 과거 receipt와 between16/ExodusComplete true·ChapterComplete false이다. 새 process가 정확 Run/Profile를 무쓰기 Load하고 Enter/Resume으로 R16·리셋·재지급하지 않으며 명시 새 도전만 R01/schema2·프로필5393 보존으로 전환했다.

성공 대표는 **660 lowFX 3방향 예고, 1100 실제 동결 보상, 1100 출애굽기 완료/성막 완성·명시 새 도전3버튼**의3장이다. 모두 실제 열람했고 660 보스명·HP/예고 두 줄과 forms footer·arena 사이 겹침/잘림이 없었다. 첫 실패 화면은 성공3장과 별도다. 실탄은 trace/저장·좌표 검사 근거이며 별도 네 번째 성공 화면을 만들지 않았다. 직접 API/timeout/입력 fixture 관찰과 실제 좌표 CLI를 구분하며 앱 R01~R15 자연 연속 완주·자연1200kills·native OS 입력/device/mobile/performance는 NOT_RUN이다.

각 identity의 no-Host probe와 실제 realpath를 먼저 확인했다. 두 앱 모두 정확 자기 child 종료와 자기 SaveStore main/bak/lock 원래 absent 복구를 restoration.json에 기록했다. 보호 preview pivot9534f7594886/unknownparticlesqa/iOS 및 해당 저장은 조회·조작하지 않았다. 자기 Editor 종료 후 Art03 meta79개의 자동 기본 플랫폼 block과 빈 아이콘 설정1건만 exact HEAD 확인으로 복구하고 원 diff를 보존했다. 다른 작성자의 변경은 유지했다.

requested Sol 기본/observed gpt-6.1-sol, Claude/Astra/helper 호출은 없다. 자기 provider interval input6859259/cache6387200/output26728/reasoning5666/total6885987은 지연된 start event로 이전 phase tail을 포함할 수 있고 마지막 보고 tail을 제외한다. start의 이전 phase delta는 합산하지 않으며 reasoning을 output에 더하지 않는다. 실청구액/단가/effort/절감률은 미측정이다. fixed-product-source.json·verification-index.json·binding.json과 handoff.md가 실명령/XML/exit/앱/trace/3화면/실패 복구/usage를 연결한다. 제품·Editor·Player 소유를 반환하며 후속 책·무기·모바일은 시작하지 않는다.

Root 최종 결합: 독립 Core/Unity코드/지정3화면 UI 모두 막는 HIGH/MEDIUM 없이 PASS. root Resume15 선택변이는 실제 검출 PASS(.local/r15-resume-mutation.log). 대표 연속 R09→R15는 기존 Core 좌표CLI/중간diskLoad로 확인했고 PC앱은 기술준비 R14→R15/출애굽기완료/새processLoad를 확인했다. 이를 앱15라운드 자연완주/native/모바일/성능 PASS로 합산하지 않는다. 필수커밋훅 및 원격 Core/art CI를 마지막SHA로 확인한 뒤 승인범위를 종료하며 레위기/새무기/추가모바일 작업은 시작하지 않는다.

첫 업로드 d81337f의 art CI44150560621 success, Core44150560639는 신규시험의 seal미존재 KeyNotFound로210/211 실패했다. 테스트 기대피해 한줄만장비없는경우방어0/있으면실level로고쳤고관련8/8 exit0이다. 제품/CoreDLL/Unity최종앱/대표화면은불변이며복구SHA원격CI를별도확인한다.
