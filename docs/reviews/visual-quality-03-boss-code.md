# VQ03 보스 연출 독립 코드 리뷰

**PASS — 병합을 막는 HIGH/MEDIUM 코드 결함을 발견하지 않았다.** 대상은 기준 `34d39e2e456291ae47898d15e531b8cb0686d114` 대비 U2의 17개 변경이다. 외부 `art-visual-quality-03-boss-staging/handoff.json` revision 2, `RESULT.md`, 승인 리뷰를 최종 구현과 대조했다. 승인 리뷰가 인용한 디자인은 Board ⑤ revision 12, 게시 `1794986875-4a06`이다.

## 소스 및 실행 결합

- `/tmp/bible-vq03.hmKtoDHr`의 `REPORT.md`, `changed-files.txt`, `source-sha256.txt`, `binding.json`, `current-run`을 읽었다. 기준 대비 실제 변경 집합은 정확히 17개다. 이 17개 제품 파일과 격리 QA 소스 각 17개 모두 U2의 SHA-256과 직접 대조해 일치했다. `source-sha256.txt` 자체의 SHA-256은 `99df6dec034d5a5fdd795b85499964131f7f1d05baec0f69653fb5b30b498c1f`다. 승인 원본 manifest와 WAV 3개도 byte 일치를 확인했다.
- 최종 `build-final.log`는 동일 격리 프로젝트의 `Adam.Unity.dll` 복사와 macOS arm64 Development 빌드 성공을, `current-run`이 가리키는 `run.vbEo5uM4/player.log`는 해당 QA 번들에서 68개 assertion 및 `VQ03 PLAYER COMPLETE`를 기록한다. Player 종료 0은 U2 인계와 종료 호출을 재사용했다. 소스·빌드·실행 결합은 이 경로 및 로그로 확인했다. 앱 파일을 직접 열거나 바이너리 해시를 계산하지 않았다. 보호 사용자 저장은 조회하지 않았다.
- 변경 경로에 Core 소스/DLL, `AdamHost`, 저장 스키마, 입력 설정, 씬, 빌드 설정은 없다. 기존 `Host.Tick`, `ChooseBossReward`, checkpoint/retry/reload가 같은 정책과 순서로 동작하고 UI는 그 결과를 관찰한다.

## 소스 판정

| 검토 범위 | 확인 내용 |
| --- | --- |
| 실제 Tick과 진입 A | `AdamGame.cs:173–181`이 실제 `Host.Tick` 바로 전후에 Host/Session 참조, 단계, EncounterId, HP, IntroElapsed, receipt와 오류를 관찰한다. `AdamBossStaging.cs:101–122`는 같은 Host와 Session의 로컬 normal→boss_intro에서만 A를 준비하고 이후 동일 encounter의 0.3초 교차에서 소비한다. 0.3초 전후 어느 쪽에서 불러와도 정적·무음이며 전투를 불러온 뒤 일어난 실제 처치는 유효하다. |
| 처치 B와 보상 C | `AdamBossStaging.cs:124–145,172–186`의 동일 세션/encounter, 이전 boss HP>0, 새 HP=0 및 `EncounterId + ":reward"` receipt 조건으로 처치를 증명한다. gold-only가 같은 Tick 안에서 boss_settlement로 가도 B만 발행한다. 실패한 체크포인트는 B 키를 소비하고 C를 준비하지 않는다. 일반 후보 receipt를 reload 또는 retry로 열면 완성된 정적 머리장식, legacy 및 `PromotedFrom` 결과는 장식 없음이다. 플레이어 사망/중복 draw/로드/legacy 명령으로 큐를 만들지 않는다. |
| 키와 소스 소유권 | `AdamStagingAudio.cs:6–45`는 프로세스 수명의 `(cue, id)`를 공유한다. A는 encounter, B/C는 서로 다른 cue와 같은 reward ID를 사용한다. B>A>C 우선순위로 단일 staging AudioSource만 중단·교체한다. 기존 impact 8개 voice와 독립이며, listener는 두 계통이 공유한다. 키 등록이 mute·오류·busy 판정보다 앞서므로 due에서 소비된 cue는 재생되지 않는다. |
| 일반 B→C 경쟁 | B 클립은 0.20초이고 C는 UI tick 6(0.20초)에 한 번만 평가한다. UI 경과 시간과 오디오 소스 종료 시각은 독립이라 B가 아직 `isPlaying`이면 C를 소비하며 이후 빈 소스에서 다시 재생하지 않는다. 시각 머리장식은 그대로 진행한다. 이는 release v2가 명시한 동작이다. 기존 Player의 C 재생 성공은 fixture가 B를 명시적으로 정지시킨 경우다. 별도 경쟁 검사는 실제 소스의 B를 재생하면서 C를 due 처리해 B 유지와 C 미재생 및 이후 재시도 거부를 확인했고, B가 C를 선점하는 경우도 확인했다. 일반 플레이에서 두 음이 연이어 들리는 빈도와 청취 품질은 측정되지 않았다. |
| 수명·정적 표시 | `AdamGame.cs:60–106,136–141,183–188,235,377–383` 및 `AdamBossStaging.cs:48–99`에서 F7·볼륨 0·pause·focus/suspend·error·disable·Initialize·Host/Session 교체 시 정지한다. 교체는 transient만 비운다. 조기 선택·패널 소실·pause/error는 준비한 seal을 취소한다. 이미 시작한 seal은 같은 유효 수명에서 선택 후 남을 수 있다. F6는 표시만 정적으로 만들고 A/B/C 조건을 바꾸지 않는다. |
| 입력과 머리장식 | 숫자 입력은 `AdamGame.cs:142–144`에서 Tick 전에 읽으므로 처치 프레임의 숫자는 생긴 후보를 고르지 않는다. 바로 다음 입력 디스패치는 기존 `ChooseBossReward`와 저장으로 이어진다. IMGUI는 첫 post-tick draw에서 원래 카드 rect를 hit-test한다. 30Hz UI tick의 crest/rule은 draw-only이고 첫 카드·세 텍스트 bounds·crest 리소스를 확인한다. Tick 6의 4px rise, 짝수 폭, 최종 280px와 이미 적용된 골드/카드/Enter/Esc 의미를 유지한다. |
| 전장 및 R12 | `AdamArtWorld.cs:275–291`은 live intro 0–0.3초에만 기존 shadow pass 뒤 실제 보스 ID의 sprite를 건너뛴다. 기존 intro 움직임 프레임과 실제 HP 숫자, 12단 bar, intro chip을 사용한다. `AdamArtWorld.cs:462–494`의 hook·경고 band·의무 rim/core/arrow 및 투사체 순서는 유지된다. R12 첫 예고 4.8초는 Core 전투 시계 기준이다. 방향 잠금, 돌진 6.0–6.6초와 다음 예고 10.8초 정책에는 변경이 없다. |

## 재사용한 검증과 한계

- **EditMode 22/22, 종료 0.** `verified-edit.xml/log`에서 R03/06/09/12/15 실제 Tick의 진입과 치명타, 첫 예고 4.8/5.8/4.0/4.8/4.0초, 실패한 저장과 retry, legacy promotion, 타이밍 표, 기존 R12 복원/기하 시험, PCM import를 확인했다. `mutation.xml/log`의 B/C 공용 키 변이는 live 5건에서 seal 재생 assertion을 예상대로 모두 실패시켰다(종료 2). 원본 복원 뒤의 22건과 구분했다.
- **격리 PlayMode IMGUI 2/2, 종료 0.** `pointer-final.xml/log`와 `Vq03PointerTests.cs`는 660×440 및 1100×720에서 실제 `AdamGame.Update`가 후보를 게시한 같은 Editor 프레임에 유틸리티 창으로 MouseDown/Up을 전송하고 저장된 카드 ID를 읽는다. 초기 `pointer.xml/log`는 660에서 유틸리티 창 포커스 때문에 Update 뒤에도 boss 상태여서 1건 실패했다. 최종 fixture가 포커스를 안정화하고 Update를 포인터 직전에 호출했다. 이것은 native standalone 마우스 검증과 별개다.
- **격리 Player의 68개 제어 assertion.** 최종 로그에서 synthetic Unity Input System 입력으로 kill-frame 숫자 보류와 바로 다음 디스패치의 후보 저장, early C 취소, gold-only B, intro/reward reload, lifecycle 제어, 후보 header bounds, R12 lowFX 시계와 잠긴 경고 기하를 확인했다. R12 흰 core/arrow·어두운 rim 표본은 U2의 로컬 렌더 보고를 재사용했다. 보호 파일 복구는 U2 인계에 근거한다.
- 초기 `player.log`의 실제 시작 오류는 MonoBehaviour 초기화 중 `AdamHud`의 색 공간을 읽은 것이었다. 최종 소스는 이를 `EnsureStyles`로 옮겼고 최종 로그에서 초기화와 68개 항목의 완료를 확인했다. 중간 `player-final.log`의 operation ID payload mismatch는 격리 fixture의 시작 저장에서 발생했고, 새 `current-run`의 완료 로그로 해소를 확인했다. 최종 로그의 stripped URP 후처리 메시지는 남아 있다.
- 실제 음감·하드웨어 믹스·일반 플레이 중 B→C 가청 빈도, OS 네이티브 포인터, 모바일/Windows, 성능과 자연 플레이는 **NOT_RUN**이다. 게임 화면이나 오디오를 열거나 출력하지 않았다. 보고서만 작성했고 실행·시험·커밋·푸시는 수행하지 않았다.

## 검토 세션

요청은 기존 실제 `gpt-6.2-sol / medium` 세션을 이어 쓰는 것이었다. Orca `ctx_7dac9692da50`의 이번 Codex turn_context(2026-11-18 07:58:58.673 UTC), 세션 `01a1ed7a-8f6f-7283-968f-31f865e28d98`에서 모델과 effort를 확인했다. U2의 `binding.json`은 구현자 세션의 별도 기록이다.

자기 세션의 07:59:11.365→08:01:20.177 UTC usage 이벤트 사이 차이는 input 1,099,617 / cached input 1,046,400 / output 3,252 / reasoning 670 / total 1,102,869다. 캐시는 input에, reasoning은 output에 포함된다. 첫 이벤트 이전과 마지막 이벤트 뒤의 보고 사용량은 이 구간에서 제외된다. 실제 청구액은 미계산이다.
