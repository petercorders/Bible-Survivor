# 타격감 P0 — 독립 PC 코드 리뷰

2026-11-14. **코드/확인한 실행 증거 범위 PASS, 추가 HIGH/MEDIUM 없음. 최종 재대조로 CFB-01 CLOSED.** ParticleSystem/RenderTexture·공식 팩 실험은 본 P0 제품 통합 판정에 포함하지 않는다.

검토자는 제품/Core/시험/빌드/GUI를 수정·실행하지 않았다. 소유 파일은 이 보고서뿐이며 기존 실행 기록을 읽고 Python으로 JSON/XML·SHA·전후 결과를 대조했다. 실제 Editor/Player·시청각 native 리뷰·빌드·변이 재실행은 NOT_RUN이다. 사용자 iOS 앱·기기·저장에도 접근하지 않았다.

## 코드 검토

- **Core 경계:** BattleImpact는 serializer 특성이 없는 불변 값이다. 이벤트 링·sequence·step은 BattleWorld의 일시 필드이며 BattleSnapshot/저장 DTO에 들어가지 않는다. 기존 충돌 순서·Hit/Session.Damage 호출·Core RNG·시간 진행을 바꾸지 않고 결과가 나온 뒤 사실을 추가한다. 실제 피해는 적의 before−after, lethal은 남은 before HP, 플레이어는0 이하 초과분을 제외한 수신 피해다.
- **사건 정확성:** 턱뼈는 기존 ID 순서의 각 타깃, 성경은 실제 책 접촉점, 물맷돌은 swept collision의 위치/순서로 발행한다. 관통의 타깃별 순서와 같은 적에 대한 복수 사건이 구분된다. contact는 TargetId0, 반사 피해는 retaliation으로 별도 원인을 보존한다. 이미 무적이라 실제 피해가 없으면 contact 사건도 없다.
- **bounded 전달:** 256개 링은 newest 우선이며 손실 수를 반환한다. cursor는0..ImpactSequence만 허용하고 destination은 매번 비운다. Restore 후 이력은 비어 있으며 새 World와 이전 cursor를 섞지 않도록 표현기가 source identity를 확인한다. 이벤트 읽기가 Core 상태/난수를 변경하는 경로는 없다.
- **소비/표현:** ArtWorld.Update에서만 이벤트를 읽고 cursor를 최신 sequence로 옮긴다. OnGUI/Draw는 생성/소비하지 않는다. 같은 업데이트를 다시 호출하면 FrameImpacts가 비워진다. 실제 처치 사건과 previousPositions fallback을 reportedKills로 구분해 같은 적의 사망 효과를 중복 생성하지 않는다. 새 source/Clear는 이전 반응/효과/시간을 비운다. hit tint/squash는 Draw의 행렬·색만 바꾸고 발 pivot·Core 좌표·버튼 영역은 유지한다.
- **풀/경고:** 64슬롯은 제한되며 낮은 priority가 player warning2를 덮지 못한다. 낮음 모드에서도 실제 플레이어 피격 tint와 priority2 단서를 만든다. 물리 넉백·Core hit-stop·전체 화면 흔들림은 추가하지 않았다.
- **음성:** 원본 절차 PCM3종은 자체 xorshift를 사용하고 게임 RNG를 쓰지 않는다. AudioSource8개를 미리 만들고 종류별35ms 간격·8voice 상한·거리20 감쇠로 제한한다. Generation과 sequence cursor로 중복 발음을 막는다. pause/modal/lifecycle/mute/volume0에서는 정지하고 해당 frame 사건 cursor를 넘겨 복귀 후 과거 소리를 재생하지 않는다. Dispose는 보유 source/clip/listener를 해제한다. 원본 PCM 초안이며 외부 샘플/상용 사운드 품질/Claude Design 제작으로 보고하지 않는다.
- **게임 연결:** Game.Update가 Core tick→ArtWorld→Audio 순으로 호출한다. F6/F7/F8은 표현·mute·음량만 바꾼다. OnDisable/lifecycle에서 소리를 정지하며, 기존 사망 SelectSlot 가드와 저장·성장 로직은 유지한다.

## 독립 읽기 전용 대조

| 증거 | 직접 확인한 결과 |
| --- | --- |
| replay/before.txt ↔ after.txt | **145줄 전체 byte 동일**, 각 줄의 BattleSnapshot·GameSnapshot 직렬화 포함 |
| 고정 입력 replay 코드 | 같은 seed/입력/상태, RunId를 고정하며 출력 차이를 사후 제거하지 않음 |
| capture2의 세 무기 A/B world.json | **3/3 byte 동일** |
| capture2의 세 무기 A/B save.json | Run/Profile 동일. LastOperationId만 서로 달라 전체 bytes는 동일하지 않음 |
| p0-app-sha의 QA/게임 앱 | **각332/332 파일 SHA 일치**, 누락/변경0 |
| Core 첫 실행 |89/89 PASS XML/summary. 검토자 재실행은 아님 |
| Unity final XML |47 EditMode·16 PlayMode, 실패/skipped/inconclusive0 |
| Unity verified EditMode XML |47/47 PASS, final 이후 보강 시험 실행 기록 |
| Core 영향 변이 | baseline/restored0, lethal 누락·복수 합침·중복 전달 각1 및 실제 Impact assertion 실패 |
| 표현 원래 회귀 변이 | 옛 살아 있는 적 HP 차분 exit2/행동 실패3개, cursor0 재생 exit2/실패2개, baseline/restored0 |

전후 replay는 기존 Core와 수정 Core를 별도 사본에서 컴파일하고 같은 Program을 실행하는 구조다.145개 기록은 임의 요약 숫자 비교가 아니라 serializer 결과 전체를 포함한다. 실제 A/B 앱의 저장은 operationID가 새 GUID이므로 byte 동등으로 보고하면 부정확하다. 전후 Core/저장 byte 불변 근거는 고정 RunId의145개 replay이고, 실제 A/B 앱은 World byte 및 Run/Profile 동등으로 구분한다.

변이 스크립트를 읽어 컴파일 실패가 아니라 동일 회귀의 assertion 실패를 요구하는 점을 확인했다. 특히 표현 변이는 실제 옛 HP 차분 코드를 되살리며 여러 사건을 단순히 임의로 지운 변이로 대체하지 않았다. 정상 복원 XML도 있다. 이번 검토자가 변이를 다시 실행하지는 않았다.

## 시청각 근거의 범위

capture-checks는 각 무기 before/after 총6개 영상의 encoder exit0/200frame·WAV peak·실제 QA 완료를 기록한다. WAV6개는 stereo48kHz의 실제 파일이며 before peak0/after nonzero 기록과 대응한다. 캡처 fixture의 QaMixedPcm은 AudioListener 쪽 OnAudioFilterRead에서 엔진 mix를 기록한다. 단순 PCM 생성 함수를 파일로 내보낸 것을 실제 엔진 재생으로 혼동하지 않았다.

다만 이 A/B는 고정 fixture/입력의20초 비교이며 자연 플레이 밸런스·120적 성능·모바일 출력 증거가 아니다. 본인은 소리 청취/영상 재생/native 입력을 실행하지 않았으므로 시청각 품질은 별도 PC 리뷰 결과가 필요하다. ParticleSystem/RenderTexture 비교 및 공식 팩 실험의 빌드 성공을 P0 런타임 통합 PASS로 더하지 않는다.

## LOW CFB-01 — 최종 소스/시험 결속 갱신: CLOSED

검토 시 p0-freeze266개 중 Runtime/Core는 동일하지만79개 Art03 PNG meta와 AdamImpactTests.cs/AdamFeedbackEvidence.cs가 달랐다. p0-source-checkpoint69개에서는 같은 시험/Editor 파일 및 캡처 fixture가 달랐다. 앱2종은 모두 기록 SHA와 동일했다. 따라서 기존 P0 앱과 현재 소스 전체가 완전히 동일하다고 쓸 수 없다.

이는 진행 중 Editor 재직렬화/증거·시험 보강과 겹친 관측이었으며 제품 런타임 결함으로 단정하지 않았다. 이후 구현자가 자동 meta를 정리하고 최종 source/test/tool SHA 및 새 검토 앱을 인계했다. 아래 최종 직접 대조로 이 항목을 닫는다. 초기 p0-freeze/p0-app-sha는 역사적 근거이며 최신 소스 전체 결속으로 쓰지 않는다.

| 최종 직접 대조 | 결과 |
| --- | --- |
| final-source-sha | **302/302 현재 파일 SHA 일치**, manifest SHA도 final-binding과 일치 |
| 초기 고정 Core/Runtime | **28/28 일치**, 제품 P0 로직 드리프트 없음 |
| 자동 직렬화 원복 | **Art03 PNG meta79개+ProjectSettings 총80/80 HEAD bytes 동일** |
| 최종 앱4종 | **각334/334 파일 SHA 일치**, game-review/particle-review/particle-resize-review/particle-bounds-mutant를 서로 구분 |
| 최종 안전 경계 시험 | compile0, **47 Edit/16 Play PASS**, 실패/skipped/inconclusive0 |
| 기존 Core 변이 | 정확한 `evidence/r02-mutations-20261113T223941026531Z/summary.json`에서 baseline PASS·18/18 행동 실패 검출, 실행 원장 exit0/console과 대조 |

최신 실제 게임 검토 대상은 **game-review-build/AdamCombatGame.app만**이다. manifest SHA `cc31d228e884e575a795245db53a7e5f6d94d86b04e8fe03bfa3744388e2b38c`와 파일 해시가 일치했다. 예전 game-build 앱은 검토용 실행 금지이며 실행하지 않았다. 나머지3앱의 해시 확인은 별도 ParticleSystem QA 실험/변이 결속 확인이고 제품 파티클 통합 PASS가 아니다.

신규 Editor QA 진입점은 marker·절대 새 .app 출력·QA bundle/company/product를 요구한다. 첫 FeedbackPathGate 씬에서 식별자/저장 경로를 검사하고 probe에서는 Host/SaveStore 씬을 로드하기 전에 종료한다. destination도 Adam/FeedbackQa로 제한한다. 일반 게임 씬 부트스트랩은 gate 뒤에 실행된다. 실제 probe2건의 exit0/log facts와 원문 저장 경로/realpath를 기록한 `review-path-probes.json`을 대조하여, 각 QA 저장 경로가 두 기존 canonical 디렉터리 밖이며 probe 후 저장 미생성인 기록을 확인했다. 이는 관측 QA 경로의 격리 증거이며 임의 symlink/악의적 호출자에 대한 보안 sandbox 보장은 아니다. 본인은 앱을 재실행하거나 원본 PC/iOS 저장을 읽지 않았다.

final-handoff-validation의 source drift0/privacy0/diff0, 기존 보호 자산/설정201개 변화0 및 QA3/PC6 보존 결과는 구현자 기록으로 확인했다. 본인이 직접 대조한 것은 현재 소스·에셋·앱/로그/시험 파일이며 구현자의 native 실행을 본인 실행으로 합산하지 않는다.

재검토 중 `impact-mutations-corrected/results.json`을 가리킨 진행 문서/validation-ledger의 잘못된 참조도 발견했다. 실제 `impact-mutations/results.json`의 baseline0/restored0·3변이 exit1 및 FAIL Impact assertion을 대조했고 coordinator가 두 참조를 실제 경로로 정정한 것을 다시 확인했다. 실행 결과를 바꾸지 않은 참조 정정이며 열린 결함으로 남기지 않는다.

## 남은 경계

정밀 native 입력·새 모바일 빌드/실물 소리·발열/메모리/프레임·공식 팩 통합/입자 성능은 이 판정에 포함하지 않는다. 성능 예산은 미확정이며 Audio8/Effect64 제한만으로 최적화 완료를 주장하지 않는다. 추가 HIGH/MEDIUM은 발견하지 않았으며 별도 PC 시청각 리뷰·GC/commit/push/해당 SHA CI는 coordinator 인계 사항이다.
