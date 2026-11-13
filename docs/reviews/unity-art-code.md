# 아담 아트·outline 폰트 Unity 연결 독립 코드 리뷰

2026-11-14 KST. baseline `d12311b` 이후 runtime/editor/test/preparation-tool 변경 및 `docs/progress/unity-art.md`를 읽었다. 이 보고서만 작성했으며 Core/원본 아트/Unity/tool 코드는 수정하지 않았다. Editor/Player/GUI는 독립 UI 담당자의 소유로 실행하지 않았다.

## 발견 사항

### MEDIUM UA01 — 사망 연출 중 숫자키가 숨겨진 새 도전을 시작할 수 있음

위치: `AdamGame.cs:111`의 `SelectSlot`, `Update`의 digit1–3 전달과 `DeathPresentationVisible` 분기.

렌더러는 6프레임@8fps의 사망 연출이 끝나기 전 새 도전 버튼을 그리지 않는다. 그러나 `SelectSlot`은 사망 연출 여부를 확인하지 않고, 비활성 세션이면 즉시 `Host.Start`를 호출한다. 따라서 사망 후 `DeathPresentationVisible == true`에서 숫자1–3을 누르면 보이지 않는 시작 무기를 선택하고 사망 연출을 중단할 수 있다. 기존 same-frame 중복 가드는 이전 프레임에서 시작한 세션의 사망 이후 첫 숫자 입력을 막지 않는다.

구체 경계: 사망 fixture에서 `ArtWorld.Update(host,0)` 후 `DeathPresentationVisible`을 확인 → 다음 프레임 digit1 입력 또는 `game.SelectSlot(0)` → 기존 terminal 세션과 `Host.Session`이 달라지고 새 활성 세션이 시작되는 경로다. 현재 `DeathClipCannotBeCoveredByClickableRestartModal` 시험은 마우스 클릭만 확인하므로 이 키보드 경계를 검증하지 않는다.

상태: **해결 — 같은 실제 숫자 입력의 실패/수정/원회귀 증거와 최종 코드 경계를 독립 재검토했다**. 최초 발견 당시에는 코드 경로 확인 후 실행 담당자에게 재현을 요청했다. 이후 독립 UI 담당자도 실제 Player에서 같은 우회를 확인했다. 아래 후속 결과는 이번 리뷰어의 Editor 재실행과 구분한다.

## 나머지 읽기 검토

- `AdamArtWorld`는 snapshot/세션을 읽고 표현 상태만 갱신한다. 시각 외 활성상태/hits/kills/collected/damage/위치도 cache 무효화 조건으로 사용하여 접촉 사망이 Core 시간을 늘리지 않고 Wave를 비우는 경로를 처리한다. 종료 시 남은 전투 객체/무기 효과를 그리지 않고, 플레이어 최종 방향과 사망 clip을 표시한다. 새 World/Reload는 cache와 재사용 효과64슬롯을 초기화한다.
- 사망 clip의 추가 시각은 presentation에만 속하며 pause/suspension 동안 멈춘다. 일반 애니메이션은 Core 시각을 따르고, 표현에서 피해·충돌·스폰/RNG를 호출하지 않는다. 세 무기500step 전체 snapshot 동등성 및 same-time 사망/원래 clock-only 변이는 구현자의 별도 실행 증거이며 여기서 중복 실행하지 않았다.
- Art importer는 pinned index와 원본 PNG hash를 확인하고 FullRect/PPU32/Point/비압축/mipmap없음/pivot/border를 지정한다. spriteID는 ID+프레임의 결정적 hash이고 name/fileID 대응도 provider로 지정한다. BuildMac 경로는 PNG/TTF/license와 실제 Sprite 등록 검증 후 배포 고지/manifest를 복사한다.
- `AdamFonts`는 imported Font를 직접 사용한다. textureRebuilt에서 소유 폰트의 Point/Clamp를 다시 설정하고, `AdamGame.OnDestroy`에서 구독을 해제한다. 원본 Font asset을 임의 Destroy하지 않는다. 글리프·raster·동적 텍스처·재임포트 시험은 실제 엔진 경계를 대상으로 하지만 이번 리뷰어가 직접 실행하지 않았다.
- 모달의 error/choice/terminal 우선순위, HUD 차단, pause 내부 재개 및 기존 입력 release/reset 경로를 읽었다. ArtButton은 GUI.Button의 실제 hit rect를 유지하고 pressed 내용만 움직인다. 폰트/9slice 화면의 직접 가독성·입력 판정은 별도 UI 리뷰 범위다.

## 실제 독립 오프라인 실행

`python3 -B tools/unity_art_tool_tests.py` → exit0, **7/7 PASS**. 준비 도구는 자체 임시 사본에만 파일을 썼다. 기존 meta 유지, 원본 바이트/반복 실행, pinned manifest/연관 계약/PNG/TTF/license 손상 거절을 확인했다.

별도 `<temporary>/bible-artcode-review-6ha9x1f0`에서 Art/Font 준비를 각각 완료한 후 다음4건을 직접 실행했다:

| 입력 | 실제 결과 |
|---|---|
| Art: 기존 목적지 존재, source PNG 손상 | ValueError; 기존 목적지 전체 SHA 불변 |
| Font: 기존 목적지 존재, source TTF 손상 | ValueError; 기존 목적지 전체 SHA 불변 |
| Art: 예상 밖 목적지 PNG 존재 | ValueError; 기존 목적지 전체 SHA 불변 |
| Font: 예상 밖 목적지 TTF 존재 | ValueError; 기존 목적지 전체 SHA 불변 |

근거 `prepare-review.json`을 coordinator에게 인계했다. 입력 검증 실패의 쓰기 전 보존을 확인한 것이며, 임의 디스크 I/O 실패 도중 여러 파일 쓰기의 원자적 rollback을 입증한 것은 아니다.

## 검증 경계

이 리뷰어의 Unity compile/EditMode/PlayMode/Player/GUI·모바일·성능은 **NOT_RUN**이다. 구현자의 최초39Edit/10Play/build와3변이, 원본83SHA/GUID 재임포트는 해당 handoff의 별도 근거다. 최종 숫자키 수정의 후속 실행 근거는 아래와 같다. 원본 그림/폰트 QA나 실제 화면 PASS를 코드 읽기로 대체하지 않는다.

## UA01 동일 회귀 수정 후 재검토

최종 판정: **UA01 해결 / 좁은 코드 재검토 PASS; 추가 HIGH/MEDIUM 없음**.

`SelectSlot`의 공통 진입 조건은 이제 `DeathPresentationVisible`이면 `commandFrame` 갱신·Host.Start/Choose 전에 반환한다. Update의 숫자 입력과 포인터 선택이 동일 경계를 통과하므로 사망 연출 중 숨겨진 새 도전 경로가 차단된다. 이전 입력을 예약하는 큐가 없으며 숫자키는 wasPressedThisFrame을 사용한다. 추가 시험은 실제 접촉 사망 fixture에서 component Update를 유지하고 presentation만 pause한다. digit1/2/3 각각, 세 키 동시 입력, 중단/복귀, clip 종료까지 hold 중 세션·Run null·골드·Core 시각이 유지되고, clip 종료 후 release→새 press에서만 해당 무기가 시작되는지 확인한다.

리뷰어가 `evidence/unity-death-digit-20261114/`의 XML·명령·summary·변이 스크립트를 직접 읽고 파싱한 결과:

| 같은 실제 InputSystem 경계 | 확인된 결과 |
|---|---|
| `baseline-no-guard/results.xml` | 전체13 중 기존10 PASS / 새 digit3 FAIL; 모두 Session 참조 변경 assert |
| `fixed-PlayMode/results.xml` | 13/13 PASS, skipped0, Unity/wrapper0 |
| `mutant-results.xml` | 공통 가드만 제거한 원래 우회: 같은 digit3 모두 Session assert FAIL, Unity2 |
| `restored-results.xml` | 같은 격리 복사본 가드 복원: 3/3 PASS, Unity0 |
| `final-EditMode/results.xml`, `final-compile/summary.json` | 39/39 PASS/skipped0, compile Unity/wrapper0 |

변이 스크립트가 최종 소스에서 `DeathPresentationVisible` 가드만 제거하고 같은 세 시험을 실행/복원하는 것을 확인했다. 실패 XML은 실제 행동 assertion이며 컴파일 오류를 검출로 세지 않았다. 이전 마우스 전용 시험과 다른 회귀로 대체한 것이 아니라 원래 숫자키 우회를 직접 재현한다.

리뷰어의 별도 읽기 전용 Python 명령으로 새 `final-source-build-sha.json`의 **231개 파일 전부**를 현재 디스크 SHA256과 비교했다. 누락/변경0건으로, 추가 시험·가드·빌드가 새 인계 snapshot에 포함됨을 확인했다. 실제 새 macOS 빌드 exit0와 원본83개 등록 불변은 해당 실행 담당자의 최종 근거이며 별도 재빌드하지 않았다.

이 후속에서 Editor/Player/GUI를 실행하지 않았다. native GUI의 정밀0.75초 숫자 차단 성공을 실행 담당자가 주장하지 않는 제한도 유지한다. 그 입력 경계는 위 실제 InputSystem 시험/원회귀 변이로 확인했고, 최종 실제 Player의 가독성/조작 재검토는 독립 UI 담당자의 별도 판정이다.
