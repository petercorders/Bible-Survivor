# R11 최종 Unity 대표 UI 독립 리뷰

2026-11-18 · **PASS — 지정 PC 3화면의 정적 관측 범위**. 막는 HIGH/MEDIUM 및 필수 수정 ID 없음. 생산자와 독립된 화면 리뷰이며 자연 완주·실제 조작 판정이 아니다.

## 직접 확인한 근거

`evidence/r11-unity-integration-20261118/handoff.md`와 `binding.json`을 읽고 binding 지정 `capture/`의 아래 3PNG를 각각 `view_image`로 직접 확인했다. 기준 앱은 생산자 binding의 `build/R11Qa.app`, bundle `com.biblesurvivor.adam.r02artqa.r1199133a6c389b`다. 앱 실행·추가 해시 행렬·재빌드는 하지 않았다.

| 정확 표본 | 관측과 판정 |
| --- | --- |
| `01-660-jochebed-normal.png` | PASS. 660×440에서 ‘출애굽기 2 / 요게벳 · 3분 시제품’, 체력·경험치·레벨·시간·골드, 일시정지, 키 안내와 ‘F6 효과 기본’이 읽힌다. 밝은 중앙 플레이어, 높은 갈대 묶음 사족 ex4, 세로 케이지 등불 ex5, 낮고 긴 꼬리의 ex6를 형태로 구별할 수 있다. 중앙 무기명 텍스트와 일부 배우가 겹치지만 라운드·HUD·플레이어 식별을 막지 않는다. |
| `02-660-jochebed-lowfx.png` | PASS. 같은 작은 화면에서 ‘F6 효과 낮음’이 명확하며 세 종의 본체 실루엣과 플레이어가 유지된다. 낮은 접지 음영은 그림자로 읽히고 독립 위험 원·속박 범위·히트박스로 오인할 근거가 없다. 등불은 손잡이와 케이지를 가진 배우로 읽히며 이 표본에서 독립 XP 알갱이나 공격 효과로 오인할 근거는 없다. 실제 XP/공격 혼전 오인율을 검증한 것은 아니다. |
| `03-1100-jochebed-complete-r12-wait.png` | PASS. 1100×720에서 ‘출애굽기 2 완료 · 진행 저장됨’, ‘R12 대기 · 다음 라운드 준비 중’, 진행 유지 및 새 도전 선택 시 성장·장비 초기화가 읽힌다. 숫자 2와 다음 라운드 대기가 출애굽기 책 전체 완료 또는 R12 즉시 플레이 가능이라는 오인을 막는다. 세 버튼은 각각 새 도전과 시작 무기명을 명시한다. |

세 화면에 developer console 가림이 없다. 우하단 Development Build 표시는 남지만 핵심 문구·완료 버튼을 가리지 않는다. 작은 화면의 하단 키 안내도 이 표본에서는 읽힌다. 모든 해상도·UI scale·접근성 인증으로 확대하지 않는다.

## 재사용한 근거와 자기 실행 구분

외부 `art-readiness-reviews/exodus-r02-review.md`의 적3종·4프레임·그림자 계약·×1/혼합/흑백 QA는 선행 정적 아트 근거로 재사용했다. 이번에 원본 시트·흑백 보드·전 프레임을 다시 검사하지 않았다. 이번 직접 확인은 위 실화면 normal/lowFX/완료 3장뿐이다.

생산자의 `capture/continuous-trace.txt`를 읽었다. 명시 진입 실패 후 동일 진입 재시도, 일시정지 World3의 정확 Load, ex4/5/6 실제 chase3s, 완료 Run/Profile·3receipt·forms의 새 프로세스 Load와 중복 보상 없음, 명시 새 도전만 R01/schema2/Profile 유지는 **생산자 실행 근거 재사용**이다.

- 일반 성장 고갈: `candidates=1 / pending=1 / actualChoose=1 / growthGold=25`.
- 완료 정산 성장: 별도로 `candidates=1 / pending=1 / actualChoose=1 / growthGold=25`.
- Core 완료 보상은 위 두 성장25와 별개의 `200 / null unlock / heal once`다. 후보 수·pending 수·Choose 호출 수를 대체하지 않으며, 보상25/200이 완료 PNG에 표시된다고 주장하지 않는다.

생산자 Edit48/48, Play 고유11(최초10+보완2 중 중복1), 실제 InputSystem Enter/IMGUI Continue click은 본인 실행·독립 시험 수에 합산하지 않았다. `post-build-test-only.json`은 `AdamRoundPlayTests.cs`만의 보완이며 Player 제외, Runtime/Editor/Core/catalog 불변으로 기록한다. 정상 앱 빌드는 생산자 1회이고 이 시험 소스 보완 때문에 앱을 재빌드한 것으로 세지 않는다.

## 한계·사용량·소유 반환

**NOT_RUN:** 자연960kill·앱 전체11라운드 완주·밸런스, native 실제 입력/focus/입력 누수, 모바일/기기/터치/안전영역, 성능, 애니메이션·flip·실제 이동/충돌/피격/사망과 전군집 XP/공격 오인율. HP10000/gear8/three forms/six slots, timeout/directkill/drop discard/cycle1000 기술 fixture는 자연 플레이 증거가 아니다.

Unity/Editor/Player/GUI/저장 조회·변경/새 시험/캡처/빌드/전체 hash 행렬/제품·아트 원본·타 문서 변경/commit/push/추가 worker/Claude 호출 없음. 소유 문서는 이 파일과 외부 `unity-exodus-r02-review.md`, `unity-exodus-r02-gc.md`뿐이며 승인된 기존 usage 도구가 자기 checkpoint를 추가했다. 다른 작성자의 변경은 보존했다.

자기 `r11-final-ui-review` start/end는 `evidence/model-usage/checkpoints.jsonl`에 기록한다. start 이전 초기 지침·handoff/binding 열람은 구간에 포함되지 않고 공급자 이벤트 지연과 최종 보고 이후 토큰 누락 가능성이 있어 전체 작업량이 아니다. inherited Sol, 관측 모델은 도구의 `gpt-6.1-sol`; effort·요청별 귀속·실제 청구액 미측정, 비용·절감률 미계산이며 생산자 usage를 합산하지 않는다. 최종 코드/통합/CI/커밋 및 다음 R12 판정과 문서 소유를 coordinator에게 반환한다.
