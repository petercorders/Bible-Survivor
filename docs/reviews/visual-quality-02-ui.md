# VQ02 최종 독립 UI / 전투 근거 검토

**읽기 전용 근거 대조 완료. 확인된 차단 HIGH/MEDIUM 없음. 직접 시각·모션·주관 청취는 NOT_RUN이며 전체 시각 승인 PASS로 전용하지 않는다. OS native pointer는 NOT_VERIFIED다.**

2026-11-18 KST, task `task_aeb9cbbc52c4` / dispatch `ctx_b79d793a89a7`. 요청된 실행 배정이며 앞선 active 중 자리표시 메시지와 구분한다. 소유는 이 새 repo 보고서 하나다. 기준은 `evidence/visual-quality-02-20261118/handoff.md`, baseline0696c98, 최종 `build/PolishQa.app` / bundle `com.biblesurvivor.adam.visualqa.polish5cdff3b007c8`이다. 실제 앱을 열거나 사용자 앱·저장을 조회하지 않았다.

## 매체 제약과 독립 확인 방법

실제 게임 이미지/영상이 도구 출력이나 채팅/외부에 게시되지 않도록 **view_image·이미지 출력·영상 프레임 첨부를 전혀 사용하지 않았다**. 기존 로컬 `ocr.swift`(Apple Vision)를 지정 일부 private PNG에 실행하여 텍스트만 받았고, 기존 high-health-probe 및 시험/입력/명중 facts를 대조했다. 로컬 수치 probe도 픽셀을 출력하지 않고 결과 좌표만 기록했다. 새 앱/Editor/GUI/저장 실행·제품편집·테스트 재실행은 없다.

이 환경에서 비공개 게임 이미지를 직접 시각 입력으로 읽으면서 도구 결과에는 게시하지 않는 경로가 없으므로 주관 시각 판독은 **NOT_RUN**이다. OCR의 인식 성공/실패와 픽셀수치를 직접 눈으로 본 가독성으로 바꾸어 보고하지 않는다. VQ02 후보 아트 검수의 직접 시각 PASS는 새 후보 PNG 범위였으며 실제 통합 앱의 시각 승인과 별개다.

## 660 / 1100 높은 HP·타이머

기존 `high-health-probe.json` 및 OCR을 읽고 같은 private PNG에 좁은 흰 glyph열 probe를 로컬에서 대조했다. **660의11 glyph군은 x108–221,1100은 x158–271**이며 기대기술값 `10000 / 10000`의 글자/슬래시11군이 lane 안에 있다. lane끝224/274, 타이머rect시작264/484이므로 실제 glyph끝→timerrect에 각각43/213px 분리가 있다. 앞선 Design markup의 외부HP/타이머 충돌이 동일하게 남아 있다는 근거는 없다.

OCR은 높은HP 전체문자열을 불완전하게 읽지만 `0:02`·일반/3:00·레벨/일시정지/이동안내를 읽는다. 보스lowFX OCR은 보스경과·24920/26000·3방향예고 문구를 읽으며 일반180초 분모와 다른 역할임을 뒷받침한다. OCR의 `1:04` 등 오인식 가능성이 있어 보스초수 자체의 정확 판독으로 쓰지 않는다. glyph군은 글자가 lane밖에서 잘리지 않았다는 제한증거이지 대비/주관가독성 인증이 아니다.

## private UI OCR 표본

기존 OCR 및 이 검토에서 기존 OCR도구로 읽은 파일은 `capture/01-660-start.png`, `01-1100-start.png`, `04-660-growth.png`, `06-660-boss-lowfx.png`, `08-660-boss-reward.png`, `09-660-exodus-complete.png`, `11-1100-save-retry.png`, `13-1100-death-restart.png`와 high-health2장이다. 모든 새 OCR 호출 exit0. 텍스트 결과만 출력했고 PNG 자체는 게시하지 않았다.

- 시작660/1100: 선택제목/자동공격·성장안내/무기명·Enter시작·1~3바로시작을 읽는다.1100 일부 회전성경 문구/숫자 인식누락은 실제 잘림으로 확정하지 않았다. bracket/들림/hover 강조의 주관읽힘은 NOT_RUN.
- 성장660: 선택제목/각 이름/효과행/신규단계/기본무기수치·보조영구강화 footer가 인식된다. OCR오타가 있어 수치/글자 전체의 완전 판독으로 쓰지 않는다.
- 보상660: 구름강화4→5/5, 골드+100과 고정골드적립/선택 footer 일부를 읽는다. 고정500 footer는 OCR이 불완전해 수치가 잘못 표시됐다는 결함도, 완전히 읽힌다는 PASS도 주장하지 않는다. 실제 지급·저장은 생산자근거다.
- 완료660: 출애굽기완료/성막완성/+280진행유지/레위기미구현 및 세 새도전/성장장비초기화 안내를 인식한다. 첫책/완료/새도전 분기의 실제작동은 별도근거다.
- retry1100: 저장/불러오기중단·전투정지·미저장진행폐기·같은저장재시도와 재로드행동이 읽힌다. 재로드라벨에 OCR오타가 있어 실제 버튼글자잘림을 단정하지 않았다.
- death restart1100: 사망·도전종료/성장초기화·영구골드해금유지/새도전행동을 인식한다. 실제 사망포즈/전환모션·키캡/푸터/개발워터마크 겹침의 주관판독은 NOT_RUN.

현재 텍스트/수치증거에서 새 확정 HIGH/MEDIUM은 없다. **white/rim/arrow 세방향의 실제 시각식별, 적탄/XP구분, 모든 카드·footer잘림과 선택강조는 직접 시각 NOT_RUN**이다. lowFX유지 문구 및 생산자의 동작 assertion은 해당미검증을 대신하지 않는다.

## 세 무기: 숫자근거와 직접관측 분리

private `capture/{jawbone,sling,bible}/frame-{000,002,015,045}.png` 표본은 파일/크기1100×720 RGBA만 로컬로 확인했다. 프레임의 실제 그림을 출력·시각판독하지 않았다. 따라서 손anchor·활성좌우반전·돌release/실궤적·성경orbit/잔상·각무기의 contact shape를 눈으로 식별한 PASS는 **NOT_RUN**이다. 영상전프레임/픽셀행렬을 돌리지 않았다.

생산자 `facts.txt`는 jaw1hit/contact8sample/played1, sling5hit/contact25sample/projectile140observation/played5, Bible3hit/contact12sample/played1을 기록한다. 실제표시sample수는 고유hit수와 다르며 sample로hit를 추정하지 않는다. `sling-shot-facts.txt`의 누적shots7/hits6/live0은 초기warmup을 포함한 checkpoint이므로 최소1제거무명중의 하한 근거이지 per-ID명중귀속이 아니다. sling clip의5hit와 누적6hit를 모순이나6차명중주입으로 단정하지 않는다.

실제Core명중 소비·whirl .12초창·동일ID previous observations·thud방향3프레임group/quarter-turn·Bibleglobal2cap 및 모호축의 두 반투명pose는 생산자구현/근거 설명이다. 본인 실발사/실명중/노미스 시각관찰로 합산하지 않는다. lowFX suppression/적flash/필수선유지/cleanup은 생산자assertion 재사용이다.

## 실행증거 판정

| 항목 | 판정 / 귀속 |
|---|---|
| Edit related.xml31/31 | 생산자PASS 재사용; 신규UI11+World4+기존16. 본인재실행없음 |
| 실제 IMGUI input.xml3/3 | 생산자PASS 재사용: 키/repaint좌표의focus침범방지·heldEnter/release, moving start-card 즉시pointer hit, pause pointer재개 |
| 원래 hover/GUI.Button변이 | QA한정1expectedFAIL, 복구근거focus-restored 재사용. main제품변이없음 |
| OS native keyboard | 실제 arrows→Enter/F6–F9/Esc/Enter 및 앱상태proof 생산자재사용 |
| OS desktop native pointer | **NOT_VERIFIED**: foreground/delivery 실패와권한거절. IMGUI pointer3PASS와 합치지 않음 |
| optional9 absent | `fallback.txt`의 기존31조회·oldHUD/World복귀 및최종복구 생산자PASS재사용 |
| 최종build/capture/cleanup | handoff의최종exit0/complete/restoration재사용. 앞선GUID compile·fixture/driver실패는실패역사로보존 |
| Draw/World/save불변·pausefreeze·mute0voices | 생산자assertion재사용; 본인저장조회없음 |

## 청취·남은 한계

`audio-facts.json`의3engine stereo WAV는nonzeroPCM과각hash를 기록하고 원본PCM제품변경false다. 이는 소리가 존재한다는 생산자수치근거이지 음색선호/명료성청취PASS가 아니다. 현재 도구에 게시없이 로컬오디오를 실제 주관청취하는 입력경로가 없어 **thud/crack/page 주관청취 NOT_RUN**. 편의video-with-audio mux는 sample정확동기/지연인증이 아니며 검사하지 않았다.

자연밸런스·전체native연속·성능/실시간지연·mobile은 NOT_RUN. 확정된차단결함은없지만 직접시각/청취 및 OS pointer검증은 남아있다. 릴리스나 전체시각PASS로 과장하지 않고 권한있는로컬검토자/통합자가 남은게이트를 확인해야 한다. 추가빌드/재검사·보호앱조작을 요구하거나 수행하지 않았다.

소유보고서하나만 작성, VQ03/외부보고서/제품/VQ02진행중작성분을 수정하지 않았다. 다른작성자변경보존, 보호라이브앱/저장불조회불조작, 새worker·commitpush없음. 이미지/영상/기존스프라이트는 도구결과/채팅/외부에 전송하지 않았다.

## 자기 사용량

phase `visual-quality-02-final-readonly-review`, 세션 `01a1ea49-7c85-7cb0-854f-074ad240421e`, 관측gpt-6.1-sol/effort미측정. 기존usage도구start/end `--ledger /dev/null`, 양exit0.

- start recorded_at `2026-11-18T05:52:32.264587+00:00`, event05:51:38.310Z: input15838552/cached15134592/cache_write0/output62606/reasoning6350/total15901158. 이전provider baseline으로정확task시작량전용금지.
- end recorded_at `2026-11-18T05:53:26.310450+00:00`, event05:53:04.057Z: input16819777/cached16104704/cache_write0/output63834/reasoning6701/total16883611, resultPASS(요청된근거검토수행결과이며전체시각PASS아님). 후행문서/인계·telemetry지연미포함.
- 자동deltaBASELINE, 총량재계산·reasoning중복합산없음. 청구·가격·요청별귀속근거가없어 비용/APIequivalent/절감률 **미계산**. 미측정을0으로합산하지않는다.
