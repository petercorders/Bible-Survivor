# 아담 타격감 P0와 분리 실험

2026-11-14. Android 경계 커밋 `a64254c66fe7b2ff0fd3b95627a3cf00922875fe` 이후 P0를 구현했다. 사용자 iOS 앱·기기·저장은 조회·조작·종료하지 않았다. 독립 리뷰/GC/commit/push는 root 소유이며 이 문서는 구현자 검증 인계다.

`BattleWorld`는 기존 충돌 결정 직후 읽은 실제 피해·처치·원인·대상·위치를 immutable `BattleImpact`로 전달한다. 성경은 기존 책 중심 접촉 판정 위치, 턱뼈/반사는 대상 위치, 물맷돌은 기존 swept intersection의 접촉 시점 위치, 플레이어 접촉은 플레이어 발 위치를 쓴다. 위치가 추가 충돌 계산/판정으로 되돌아가지 않는다. World별 sequence와 step ordinal은 저장되지 않으며, 반사가 Core 시각 갱신 전에 발생해도 ordinal로 해당 step을 구별한다. 256개 링은 최신 사건을 유지하고 `ReadImpacts`가 누락 개수를 반환한다. 소비자가 producer나 전투 난수를 호출하지 않으며 Restore는 빈 링으로 시작한다.

Unity는 Update에서 cursor로 한 번 읽고 OnGUI에서는 읽기 표시만 한다. 살아 있는 적 HP 차분의 스파크 생성은 제거했다. 치명 명중의 스파크와 사망 burst는 각각 한 번 생성하고, 직접 Session.Kill을 쓰는 기존 QA 호출에는 사망 표시만 보완한다. 적은 90ms 동안 색과 발 고정 눌림(가로+12%/세로−15%)을 적용한다. 이는 sprite 변형이며 실제 위치/충돌/무기 범위는 그대로다. 플레이어 피격 단서는 저효과에서도 유지하고 64슬롯 포화에서 낮은 우선순위 효과가 경고를 덮지 못한다.

명중음은 기존 Unity AudioSource 8개를 prewarm해 재사용한다. 외부 샘플 없이 `AdamImpactAudio.Samples`의 원본 절차 PCM 초안을 생성했다: 성경의 짧은 740/1180Hz 톤, 턱뼈의 낮은 충격/잡음, 물맷돌의 짧은 고음/잡음이다. 상용 음질·Claude Design 제작을 의미하지 않는다. Core 난수와 분리한 고정 PCM이며, 무기별 35ms 재생 간격/8voice 상한·거리 감쇠·음량·음소거를 적용한다. pause/성장/사망/중단/Reload에서는 소리 재생을 중단하고 과거 사건을 재생하지 않는다. PC F6은 저효과, F7은 음소거, F8은 음량25/50/75/100% 순환이다. 모바일 조작/실물 수용은 검증하지 않았다.

| 검증 | 결과 |
| --- | --- |
| Core 기존84+신규5 | PASS89/89; `core-first/core-tests.xml`, 전체 독립 CLI 게이트도0 |
| 기존 Core 변이 | PASS18/18 검출; `existing-mutations.log`와 해당 도구가 기록한 ignored 결과 |
| 신규 Core 변이 | 치명 사건 삭제·같은 대상 합침·중복 전달 각 assertion FAIL(exit1), 원복89 PASS0; `impact-mutations/results.json` |
| 전후 전투/저장 불변 | 동일 seed/고정 RunId/입력의 세 무기145개 전체 BattleSnapshot+GameSnapshot 직렬화 기록 1,138,900bytes 동일; `replay/before.txt`/`after.txt` |
| 실제 Unity | compile0, EditMode47/47, PlayMode16/16; `compile-safety-final`, `EditMode-safety-final`, `PlayMode-safety-final`의 최종 명령/log/XML/summary (`safety-final-engine-commands.json`) |
| 원래 표시 회귀 | 실제 HP차분 구조에서 복수 Expected2/Actual1, 치명 Expected2/Actual0; cursor 재소비에서 Expected2/Actual4. exit2/실패XML→원복0; `unity-mutations/results.json` |
| 실제 macOS Player | A/B QA 앱 build0 및 실제 Adam 씬 게임 QA 앱 build0. 별도 제품명/명시 QA 저장만 사용하며 기존 PC 저장에 fixture 없음 |
| 실제 A/B | 세 무기 각 before/after20초·200 PNG→MP4, 600 동일 입력 tick. 최종 World bytes 동일. 실제 Core hit/kill 성경4/4, 턱뼈2/2, 물맷돌18/18 |
| 실제 오디오 출력 | Unity AudioListener 필터의 engine-mix WAV: before peak0, after16bit peak4439/4758/3981. 이는 렌더·출력의 수치 근거이며 청감 품질 독립 리뷰는 root 후속 |
| 원본/설정 | 본인 Editor가 자동 추가한 79 PNG meta 플랫폼 기본 블록과 빈 아이콘 설정만 분류·원복. 기존 GUID/rect/pivot/border·PC 설정과 PNG/TTF 원본을 보존 |

ignored 근거 루트는 `evidence/combat-feedback-20261114/`다. `video/{bible,jawbone,sling}-{before,after}.mp4`, `capture2/*/engine-mix.wav`, `capture-checks.json`을 사용한다. 비교는 검수 아트의 원래 HP차분 renderer를 QA 사본에 이름만 바꿔 함께 컴파일한 staged 체크포인트이며 자연 밸런스/성능 시험이 아니다. 영상은 200 PNG를 10fps로 변환한 것이어서 90ms 반응 시간/실시간 프레임율을 인증하지 않는다. `video-with-audio/`는 편의 합성본이며 고정 tick 영상과 실제 믹서 WAV의 정확한 재생 지연 동기화를 증명하지 않는다. `capture-save-comparison.json`에서 A/B 전체 저장 파일 bytes는 LastOperationId 차이로 **다르고**, World bytes 및 Run/Profile/Revision은 같다. 별도의 고정 RunId replay145개 전체 직렬화 동등과 구별한다.

최종 결속은 `final-binding.json`, 302개 파일의 `final-source-sha.json`, 파일별 앱 SHA의 `final-app-sha.json`이다. 초기 `p0-freeze.json`/`p0-app-sha.json`은 역사적 체크포인트다. 최종 Core/Runtime 28개는 초기 고정본과 같으며 QA 사본 Runtime/DLL도 현재 제품과 같다. 이후 변경은 검증된 시험 assertion, root 승인 Editor QA 안전 경계와 별도 QA fixture다. 자동 meta/빈 아이콘은 원본으로 복원했다. 기존18변이의 실제 원장은 `evidence/r02-mutations-20261113T223941026531Z/summary.json`, 전체 Core 검사는 `evidence/r02-20261113T223830069095Z/checks.json`에 있고 `validation-ledger.json`에 명령/exit를 연결했다.

실제 게임 검토는 **`game-review-build/AdamCombatGame.app`만** 사용한다. manifest SHA256은 `cc31d228e884e575a795245db53a7e5f6d94d86b04e8fe03bfa3744388e2b38c`다. 옛 `game-build/`는 기존 bundle ID를 발견해 한 번도 실행하지 않았으며 실행 금지 표시를 남겼다. 새 앱은 QA 전용 company/product/bundle과 첫 무저장 probe 씬을 사용한다. `review-path-probes.json`은 게임/드라이버 각각 실제 Application.persistentDataPath와 realpath가 원래 PC 두 저장 디렉터리 밖임, probe exit0 및 저장 미생성을 확인한다. bundle ID만으로 격리를 판정하지 않았다. 실제 게임에서 시작 선택→전투→자연 사망→새도전→Esc 일시정지 화면을 확인하고 정상 exit0 종료했다. 실제 F6/F7/F8 입력은 GUI 도구 미지원으로 NOT_RUN이며 저효과/음소거/음량의 API·생명주기 시험과 구별한다. QA 저장3개는 실행 전 부재 상태로 복원했고 원본 PC6개는 존재/bytes가 모두 같다. 상세 재실행·원본 보존 경로는 ignored `handoff-private.md`에만 둔다.

실패 시도도 보존했다. 최초 Unity compile은 추가 렌더 블록 괄호 누락으로1, 최초 Edit는 QA health가 실제 catalog 최대치를 초과해2였고 원인을 고쳤다. 첫 신규 변이 격리 사본은 content 누락으로 baseline 실패해 결과를 폐기하고 content를 포함한 사본에서 baseline0→변이 실패→원복0을 다시 확인했다. 첫 A/B 앱은 background 미진행으로 본인 QA PID만 종료했고, QA 드라이버에 runInBackground를 명시한 새 앱/새 출력에서 여섯 구간을 완료했다. 이 설정은 제품 Runtime 변경이 아니다.

추가 승인된 기본 ParticleSystem은 `tools/fixtures/unity_particle_*`로 만든 **별도 QA Scene/앱**이다. 현재 제품 Runtime에는 파티클을 연결하지 않았다. Unity 기본 ParticleSystem·URP Particles/Unlit과 Point ARGB32 RT(AA1), 명시 camera aspect, 정수 배율 합성을 사용한다. 16개 시스템×16입자=256 상한이며 수동 Simulate/Pause로 같은 Core tick에 맞춘다. 이 QA 전투 영역은 `(20,70,w−40,h−100)`이고 실제 게임 영역 `(20,116,w−40,h−194)`과 다르다. QA 상단 표시 불변이 실제 제품 HUD/모달 통합 PASS를 뜻하지 않는다.

| 별도 파티클 실험 | 실제 결과/근거 |
| --- | --- |
| 1100×720·660×440, 세 무기 각 기본/PSystem 비교 | 각6구간 완료/Player exit0; `particles1100-review`, `particles660-review`, `particle-review-checks.json` |
| 투영·정수 배율·가장자리 | 1100 RT530×310/2배, 660 RT620×340/1배, Point/AA1. edge 입자 bbox20×8 /10×4, 영역 밖 변경0, 각 Core World bytes 동일 |
| 정지·저효과·풀 | tick90 입자 위치/수명 signature 및 Core 시간 정지 동일; 최대256, 저효과 tick120–139 입자0 |
| 이동 중 resize·합성 inset | `particles-resize-review`: 1100→882×586, 합성 viewport822×466, 배율2→1, edge bbox10×4/영역 밖0, Core 동등, exit0. 실제 OS safeArea는 전체 화면이며 실물 safeArea는 NOT_RUN |
| 원래 bounds0 회귀 | 수동 paused ParticleSystem의 Renderer.bounds0 때문에 살아 있는 입자가 cull됨. 두 bounds 설정만 제거한 실제 앱에서 동일 PNG 검사 exit1/1→복원 앱 exit0/0; `bounds-pixel-mutation-results.json` |

최종 static QA 앱 `particle-review-build/AdamCombatQa.app` manifest SHA는 `d8fba7026fdae5a40756e32cdc5e07893430675a5dc88e14fc7d15d0e71f5d9f`, resize/복원 앱 `particle-resize-review-build/AdamCombatQa.app`는 `a1a3da395ebfd6b8d2525637e367fb54ee0c38a14e21ccf9d0fb83382fa59cdc`다. 투영 좌표·실제 camera bounds·PNG를 같은 tick에 보존했다. 초기 bounds 미설정 실패와 종료 중 파괴된 시스템 정리 오류도 보존했으며 최종 QA Clear는 유효 객체만 정리한다. 이는 [Renderer.bounds 공식 API](https://docs.unity.com/en-us/engine/6000.0/script-reference/unityengine/renderer/bounds)와 [Camera.aspect 공식 API](https://docs.unity3d.com/ja/current/ScriptReference/Camera-aspect.html)에 맞춰 QA 경계를 고친 것이다.

후속 제품 연결의 최소 범위는 실제 게임 arena/투영 공유, optional Runtime 파티클 풀과 명중 cursor·reload/terminal/pause clear 연결, overlay layer의 main camera 중복 렌더 방지 및 플레이어/HUD/모달 합성 순서 검증이다. 실제 게임의 작은 창/홀수 해상도/크기 변경/기존 UI-01·02·사망 입력/저효과 회귀와 40/120 적 성능 측정이 추가로 필요하다. 이 단계에서 P0 고정 Runtime을 바꾸거나 제품 통합 성공으로 확대하지 않았다.

공식 무료 Particle Pack 3.3은 MyAssets Download 후 Import 버튼까지 확인했으며 **임포트하지 않았다**. ignored 원본은 167,961,562 bytes, SHA256 `a023e358cf8cfc53de10be7b8880c611e5e15a571fb6f18201cbef6c3ba52300`이며 `official-particle-pack/`에 출처/Readme/449 asset payload·70폴더/원본 GUID 의존 목록과 현재 [Unity Companion License](https://unity.com/legal/licenses/unity-companion-license)를 보존했다. archive에 별도 LICENSE/NOTICE 파일은 없어 UI/Readme 고지와 공식 약관 출처를 구별했다. SparksEffect/DustMotesEffect만 후속 선별 후보이며 StoneImpacts의 3D·decal·다수 의존을 제품에 넣지 않았다. 팩 샘플 렌더/제품 적용은 NOT_RUN, 원본 전체의 공개 재배포도 하지 않는다.

`final-checkers.json`의 local/portable/diff 검사 exit0, `final-protected-assets.json`의201개 원본/설정 변경0, `final-pc-preservation.json`의 PC6개 보존과 `game-native-qa-restoration.json`의 QA3개 원복을 확인했다. 본인 Editor/Player는 종료했고 GUI·제품 편집 소유를 root에 반환한다. 사용자 iOS는 조회/조작하지 않았다.

남은 게이트: root의 최종 결속/LOW CFB-01 재대조·독립 PC 청감/가독성 검토·GC/commit/push/해당 SHA CI. 적40/120 frame time/할당 예산, 실물/모바일/햅틱·hit-stop·화면 흔들림 및 제품 ParticleSystem 통합은 NOT_RUN이다. 디지털 WAV peak/단위 시험은 사람의 청감이나 성능 검토를 대신하지 않는다.

통합 결과: [독립 PC 코드 리뷰](../reviews/combat-feedback-code.md)는 최종302개 소스와 최신4앱 각334파일 SHA·QA path gate·47Edit/16Play·기존18변이를 확인해 CFB-01 CLOSED, 추가 HIGH/MEDIUM 없음으로 마쳤다. [독립 PC 화면·조작 리뷰](../reviews/combat-feedback-ui.md)는 실제1100/660에서 세 무기·자연 피격/사망/새 도전·성장 선택/중복 입력·정지/재개·HUD를 확인해 해당 범위 PASS다. 두 Player 정상exit0, PC6/QA3 보존·복원9/9, 최종소스/앱SHA 불변이다.

청감 선호/실제 청취·90ms 정밀 체감·F6–F8 실제 hotkey는 NOT_RUN으로 유지한다. 이 제한을 사람 승인·전체 타격감 수용으로 바꾸지 않는다. 프로젝트 GC는 `../retrospectives/R01-combat-feedback.md`에 기록했다. 이번 커밋은 P0 구현·해당 PC 검증과 별도 파티클 실험 근거를 보존하는 범위이며, 제품 ParticleSystem 연결은 다음 단계다.

업로드 결과: `96bab9217cd98a884b5cab1fef06eaeb696bddb8`을 `feat/adam-core`에 push했다. 같은 SHA의 [코어·portable Unity CI](https://github.com/petercorders/Bible-Survivor/actions/runs/43534121023)와 [아트·폰트 CI](https://github.com/petercorders/Bible-Survivor/actions/runs/43534121030)는 모두 success다. 후속 제품 파티클 작업은 `../harness/combat-particles-product-task.md`로 별도 인계했으며 기존 iOS 앱은 교체하지 않았다.
