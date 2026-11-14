# 타격감 P0 — 독립 PC 화면·조작 검토

2026-11-14. 검토자 `unity_ui_review`. **PASS — 아래 실제 PC 시각/조작·산출물 일관성 범위, 미해결 HIGH/MEDIUM 없음.** 소리의 청감 품질·기호 평가와90ms의 실시간 타격감 인증은 **NOT_RUN**이다. 이 제한을 사람 승인이나 전체 타격감 완료로 대체하지 않는다.

## 입력·격리·실행 결속

`AGENTS.md`, 전용 ui-ux/컴퓨터 사용 지침, `docs/progress/combat-feedback.md`와 private handoff를 읽었다. 구현자가 종료한 뒤 현재 `game-review-build/AdamCombatGame.app`만 실행했다. 옛 `game-build`와 별도 Particle 앱은 실행하지 않았다. iOS Simulator/앱/기기/저장은 조회·조작하지 않았다.

- 실행 전 최종 소스302/302와 게임 앱334파일 SHA를 대조했다. 실제 무저장 probe exit0, 반환한 persistent path를 realpath로 정규화해 원래 PC 두 저장 디렉터리 밖임을 확인했다. 회사/제품/bundle 문자열만으로 판정하지 않았다. probe 뒤 QA 저장3파일 모두 부재였다.
- PC 원본6파일의 bytes/존재와 별도 QA3파일을 실행 전에 기록했다. 어떤 fixture도 원본 PC 경로에 쓰지 않았다. 두 번째 작은 창은 본인이 첫 실행에서 자연 획득한 LV2 checkpoint를 QA 경로에만 재사용했다.
-1100×720와660×440 두 실제 게임 실행을 정확한 PID로 제어했다. 종료는 restore 없는 Cmd+Q와 managed exec exit0으로 확인했다. 종료 후 최종 소스302/302와 앱334파일이 여전히 같다. manifest SHA는 `cc31d228e884e575a795245db53a7e5f6d94d86b04e8fe03bfa3744388e2b38c`다.

## 본인이 직접 확인한 실제 게임

| 과업 | 실제 결과 |
| --- | --- |
|1100 시작·피격·사망 | 숫자1로 Bible1 도전 시작. 실제 자동 전투의 HP100→56과 자연 사망/새 도전 메뉴를 확인했다.90ms 피격 단서의 매 프레임은 계측하지 않았다. 정지 상태에서 시작한 배경 입력은 명시적 재개를 요구했고 정상 전면/Enter로 진행했다. |
|새 도전·턱뼈·성장 | 사망 후 숫자2로 jawbone1 새 도전. 실제 자동 전투에서 자연 LV2에 도달했다. 한국어 이름·보조 구분·0→1/5·실효과가 읽히고 숫자2 선택 뒤 underwear1만 적용됐다. 같은 숫자를 다시 눌러도 추가 레벨/선택은 적용되지 않았다. |
|정지·재개 | Esc 모달에서 안쪽 재개 버튼/설명이 읽히며 Enter 뒤 HUD/전투가 돌아온다. 다시 Esc하면 정지한다. 선택 중 화면이나 정지 모달을 타격 효과가 덮는 현상은 관측하지 않았다. |
|660 성장 카드·작은 창 | 첫 실행의 본인 자연 LV2 저장을 재사용해 같은 세 카드/설명을 읽고 숫자1로 crown1만 적용했다. 작은 창의 모달 내부 재개 버튼, 사망/새 도전 메뉴가 가려지지 않았다. |
|660 HUD·새 안내 | 실제 활성 전투에서 체력/경험치/시간/골드와 새 F6효과/F7소리/F8음량 안내가 영역 안에 보이며 글자 잘림은 관측하지 않았다. |
|세 번째 무기 | 작은 창의 자연 사망 후 숫자3으로 sling1 새 도전을 시작했다. 실제 자동 전투22.7333초의 저장에18 shots/18 hits/18 kills가 기록됐고, 장비/성장 초기화와 영구 골드 유지 경로가 동작했다. 처치/피해 주입이 없는 이 실행은 완전한3분 생존·밸런스 검증이 아니다. |

실제 게임 검토에서 새로운 HIGH/MEDIUM이나 기존 UI-01/02의 원시 ID/숨은 재개 버튼 회귀는 발견하지 않았다. F6/F7/F8은 인계된 도구 미지원 사실에 따라 native 재시도를 하지 않았다. 저효과·음소거·음량 API/생명주기 시험과 실제 hotkey 조작을 구분하며 후자는 NOT_RUN이다.

## A/B 여섯 구간: 표본 시각과 상태 대조

검토자는 세 무기 before/after 영상의 원본 PNG/CSV 접촉 프레임을 샘플링했다(Bible0001, jawbone0190, sling0002). 적/공격 위치는 같은 staged Core 상황이며 after에서 접촉·처치 부근의 추가 밝은 단서를 볼 수 있다. 가독성의 큰 악화나 arena 밖 큰 효과는 해당 표본에서 보이지 않았다. 작은 국소 단서/포즈 변형의 **실시간 체감·90ms 판독성**은 이10fps 표본으로 인증하지 않는다. 여섯 영상을 사람처럼 연속 시청했다는 주장도 하지 않는다.

독립 파일 읽기로 각 영상 MP4 duration20초, 원본 PNG200장씩을 확인했다. 세 쌍의 final World bytes는 각각 동일하고 Run/Profile/Revision도 같다. 전체 save bytes는 서로 다르며, 다른 top-level key는 LastOperationId다(`ab-state-comparison.json`). 이것은 해당 staged 비교의 끝 상태 대조이며, 구현자의145개 전체 replay 직렬화 동등 시험을 본인이 재실행한 것이 아니다. A/B QA의 status strip/arena는 제품의 실제 HUD/모달을 대신하지 않는다.

## 소리: 출력 수치와 청감의 분리

검토자는 실제 Unity AudioListener mixer가 기록한 WAV6개를 독립 파싱했다. 모두48kHz/stereo/16bit다.

| 무기 | before peak | after peak | after RMS |
| --- | ---: | ---: | ---: |
|성경|0|4439|109.65|
|턱뼈|0|4758|74.49|
|물맷돌|0|3981|151.07|

after 파일에는 비0 믹서 출력이 있고 관측 peak는16bit 상한32767 아래다. 이 수치는 출력 파일의 신호/디지털 포화 여부만 뒷받침한다. 스피커/헤드폰 실제 청취, 장시간 피로·타격음 선호·음색 분별·게임 내 음량 적절성·기기 지연은 **NOT_RUN**이다. 도구가 소리를 들은 것처럼 해석하거나 사용자 승인을 받은 것으로 기록하지 않았다. 영상과 WAV의 길이가 조금 다르고 편의 mux도 지연 보정 증거가 아니므로 A/V latency 동기화 PASS가 아니다.

## 별도 ParticleSystem 실험 경계

별도 앱을 재실행하지 않고 제공된660/1100 PNG와6개 clipping 결과를 읽었다. 기록은 outsideChangedPixels0, World exact, 최대256, low-effects 구간 입자0을 보여준다. 이들은 구현자 실험의 산출물 읽기이며 독립 실행 재검증으로 세지 않는다.

QA arena는 `(20,70,w−40,h−100)`, 실제 게임은 `(20,116,w−40,h−194)`로 다르다. 따라서 이 기록의 상단 strip/경계 안전성이 제품 HUD·모달/실제 arena의 Particle 통합 PASS가 될 수 없다. 이번 제품 Runtime에 ParticleSystem을 연결하거나 공식 팩을 임포트하지 않았다. 원래 paused bounds 회귀의 mutant/복원 기록과 제품 연결에 필요한 실제 arena/레이어/저효과/resize 검증은 후속 범위다.

## 증거·정리·남은 것

Ignored root: `evidence/combat-feedback-ui-review-20261114/`. 실제 native JSON/PNG는 `combat1100*`, `combat660*`, 실제 저장은 `jawbone-later-save.json`, `jawbone-after-choice-paused.json`, `660-after-choice.json`, `660-after-natural.json`, `sling-final-paused-save.json`에 있다. `preflight-hashes.json`, `probe-command-private.json`, `probe-result-private.json`, `postflight-binding.json`이 빌드/realpath 결속을 기록한다. `wav-observations.json`, `ab-video-metadata.json`, `ab-state-comparison.json`, `particle-observation-summary.json`은 본인 파일 관측이다.

두 Player는 실제exit0 종료됐고 정확한 게임 실행 경로의 process가 없음을 확인했다. Cmd+Q 뒤 AX/window 조회가 오류를 반환한 경우에도 다시 restore/실행하지 않았고, 종료 자체는 managed exec로 판정했다. 생성된 QA3파일은 ignored evidence로 옮겨 원래 부재 상태를 복원했다. PC6파일은 변경 없이 원본 bytes/존재를 대조했다. `restoration.json`9/9true, `player-release.json`은 두 exit0/추가 게임 process 없음이다. 로그에서 관측 NullReference/MissingMethod/KeyNotFound/IndexOutOfRange 예외는 없었다.

제품/기존 문서/커밋을 수정하지 않았고 GUI 소유를 root에 반환했다. 실시간90ms 체감/청감·실제 설정 hotkey·적40/120 성능/장기 할당·모바일/실물·제품 Particle 통합은 미실행으로 남긴다. 이 제한을 전체 시청·청취 또는 전체 타격감 완료로 보고하지 않는다.
