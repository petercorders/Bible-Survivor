# R10 최종 Unity 대표 화면 독립 UI 리뷰

2026-11-17 · **PASS — final-clean 지정 PC 3화면의 정적 관측 범위**. 막는 HIGH/MEDIUM과 필수 수정 ID 없음. 구현·생산자가 아닌 독립 리뷰이며 실제 조작이나 자연 완주 PASS가 아니다.

## 정확 근거와 직접 관측

`evidence/r10-unity-integration-20261117/handoff.md`, `final-clean/binding.json`, 그 binding에 지정된 아래 **정확히 3PNG**를 `view_image`로 직접 확인했다. 최종 기준은 `final-clean/build/R10Qa.app`, bundle `com.biblesurvivor.adam.r02artqa.r1072f41b4f4136`의 binding이다. 최초 handoff의 옛 identity는 root가 실제 Info.plist/binding 대조 후 교정했다고 인계했으며 본인은 앱을 열거나 재대조하지 않았다.

| final-clean/capture 원본 | 직접 관측·판정 |
| --- | --- |
| `01-660-genesis-complete-exodus-entry.png` | PASS. 660×440에서 ‘창세기 완료 · 진행 저장됨’, 진행 유지/현재 성장·장비로 계속, ‘출애굽기 시작 (Enter)’가 읽힌다. 아래 3개 버튼은 각각 ‘새 도전’과 시작 무기명을 표시하며 성장·장비 초기화 안내와 연결된다. 다음 책 진입과 새 도전의 목적이 분리된다. |
| `02-660-exodus-01-mixed-lowfx.png` | PASS. 660×440에서 ‘출애굽기 1 / 십브라·부아 · 3분 시제품’, HP/경험치/레벨/시간/골드, 일시정지, ‘F6 효과 낮음’ 안내가 읽힌다. 밝은 플레이어와 낮은 벽돌짐 짐승 ex1, 가로 단안 ex2, 세로 빈 갑주 ex3가 형태로 구분된다. 일부 배우 아래의 낮은 접지 음영은 작은 그림자로 읽히며 분리된 위험 원·속박 반경·히트박스 표시로 오인할 근거가 없다. 낮은 효과에서도 배우와 실루엣이 남아 있다. 중앙 무기명은 적과 일부 겹치지만 제목/HUD/플레이어/세 종 식별을 막지 않는다. |
| `03-1100-exodus-01-complete-r11-wait.png` | PASS. 1100×720에서 ‘출애굽기 1 완료 · 진행 저장됨’, ‘R11 대기 · 다음 라운드 준비 중’, 진행 유지/새 도전 선택 시 성장·장비 초기화가 읽힌다. 출애굽기 전체 책 완료나 다음 라운드 즉시 플레이 가능이라는 표현이 아니다. 세 버튼 모두 새 도전으로 명시된다. |

세 화면 모두 developer console이 본문·버튼·전투를 가리지 않는다. 우하단 Development Build 표시는 남지만 핵심 과업을 막지 않는다. `build/`와 `final-review/`의 이전 앱/콘솔 가림 화면은 **시각 FAIL 이력**이며 이번 최종 PASS 표본에 섞지 않았다. 아트 시트 전체/모든 프레임을 이번에 다시 검사하지 않았다. 기존 `art-readiness-reviews/exodus-r01-review.md`의 정적 아트 판정은 선행 근거 재사용이다.

## 화면에 없는 동작: 생산자 근거 재사용

`final-clean/capture/continuous-trace.txt`를 읽었다. 다음은 본인 실행이나 정지 이미지의 증명이 아니다.

- R09 Load가 자동 진입 없이 바이트를 유지하고 명시 진입에서만 Commit/readback/Resume 후 R10 World를 발행한다. 실패 시 옛 메모리 유지·같은 프레임 명령 거절·동일 후보 재시도, 진입 시 Run/Profile/3receipt/폼 유지·추가 골드/회복 없음은 생산자 trace/handoff 근거다.
- 일반 성장 고갈은 `candidateCount=1, pending=1, actualChoose=1, growthGold=25`다. 완료 정산은 `candidateCount=0, pending=0, actualChoose=0, growthGold=0`이며 별도 Core 완료 보상180/null unlock/회복1회가 기록된다. 후보 개수, 실제 pending 수, 실제 Choose 호출 수는 서로 다른 값이다. 완료 화면에 보상180/25가 직접 표시되었다고 주장하지 않는다.
- between11/R11 대기, 새 프로세스의 정확 Run/Profile 보존·중복 보상 없음·Enter 무시, 명시 새 도전만 R01/schema2/Profile 유지라는 동작은 생산자 증거다. 세 시작 무기 버튼을 보존 중인 현재 장비 목록으로 해석하지 않았다.
- 생산자의 Edit47은 관련46+교정1의 distinct PASS이며 실패 XML은 보존된다. Play22와 실제 InputSystem 키 조작은 생산자 실행이다. 본인이 Edit/Play/Input을 실행하거나 이 숫자를 독립 시험 수에 더하지 않았다. 첫/둘째 콘솔 가림 빌드와 최종 셋째 정상 빌드도 구분한다.

## 범위·미검증·사용량

**본인 NOT_RUN:** Unity/Editor/Player/GUI, 실제/native 입력·focus·입력 누수, 자연900kills/9round 앱 완주·밸런스, 애니메이션 전 프레임/좌우 flip·실제 이동/충돌·피격·사망, XP/공격과 겹친 전군집 오인율, 모바일/기기/터치/안전영역, 성능. 생산자의 HP10000/장비8/six passives·direct kills/drop discard/timeout/cycle1000 기술 fixture를 자연 플레이로 세지 않는다. 모바일 지원이나 국제 접근성 인증을 부여하지 않는다.

새 시험/변이/캡처/재빌드/전파일 해시 행렬/저장 조회·변경/제품·아트 원본 수정/commit/push는 하지 않았다. 작성 소유는 이 파일과 외부 `unity-exodus-r01-review.md`, `unity-exodus-r01-gc.md`뿐이며 승인된 usage 도구가 기존 ledger에 자기 checkpoint를 추가했다. 다른 작업자의 변경은 유지했다.

자기 phase `r10-final-ui-review` start/end는 `evidence/model-usage/checkpoints.jsonl`에 기록한다. start는 초기 근거 열람과 3PNG 확인 직후라 전체 리뷰 사용량을 포함하지 않으며 공급자 이벤트 지연도 가능하다. requested/inherited Sol, 관측 모델은 checkpoint의 `gpt-6.1-sol`; 생산자 usage를 합산하지 않는다. 실제 청구액·비용·절감률은 미계산이다. 최종 코드/통합/CI/커밋 판정은 root 소유다.
