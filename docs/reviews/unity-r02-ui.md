# R02 Unity 독립 PC UI 기술 검토

2026-11-14. 판정은 **제공된 실제 DrawGameView 캡처와 자동 입력/저장 증거에 한정한 기술 checkpoint 수용**이다. 새 HIGH/MEDIUM 결함은 발견하지 않았다. 최종 R02 아트와 최신 native active 조작 수용은 **NOT_RUN**이다.

AGENTS·전용 ui-ux 지침, `docs/progress/unity-r02.md`, ignored `private-handoff.md`·`binding-final.json`을 읽었다. 소스 331개와 지정된 `final2/qa-run.json` 앱의 334개 파일을 직접 SHA256 대조했고 추가 파일도 없었다. 소스 manifest SHA256 `34d208d9fe6c598fd540d10c38758accc8f30a580fc735eb77a307d2f5789202`, 앱 file-map SHA256 `82c2c38ed7fd697199ea591f6cb58349027e305804d4643827c1247b5bf45a95`와 일치했다. 증거는 `evidence/unity-r02-ui-review-20261114/`의 `source-verification.json`, `verified-technical-evidence.json`, `mutation-xml-review.json`이다.

## 화면을 직접 열어 확인한 결과

아래는 **구현자가 실제 게임 전체 DrawGameView를 자동 캡처한 PNG를 이번 리뷰어가 직접 연 결과**다. 이번 리뷰어의 native 조작 결과가 아니다. 캡처 driver를 읽어 강제 180초 완료·자동 선택·동일 시계의 배치 fixture 및 정지/명시 재개 경로를 구분했다.

| 자료 (`evidence/unity-r02-20261114/final2/capture/`) | 관측 |
| --- | --- |
| `r01-complete-1100.png` | 아담 완료·가인으로 계속, 성장/장비/영구 골드 유지와 다음 진입 저장, 개발용 기하 표식임을 명시. 계속 버튼/Enter 안내가 패널 안에 들어옴 |
| `r02-entry-1100.png` | 가인 제목, 체력/XP/레벨/시간0/골드, 정지 버튼이 읽힘. 하단은 B1 마름모/B2 빗금/B3 십자를 임시 표식으로 설명 |
| `r02-geometric-660-active.png` | 가인 HUD와 표식 안내가 잘리지 않음. 마름모·빗금 사각형·십자를 색 이외의 형태로 구별할 수 있음. 배치된 다수 표식이 플레이어 가까이 겹치므로 최종 적 실루엣/혼잡 전투 가독성 수용으로 세지 않음 |
| `r02-geometric-660-paused.png`, `r02-geometric-1100.png` | 두 크기의 불투명 정지 패널과 명시 재개 버튼/Enter 설명이 읽힘. 후자의 파일명과 달리 관측 화면은 active 전투가 아니라 정지 모달임 |
| `r03-wait-660.png` | 가인 완료·진행 저장, 노아 준비 중/R03 미구현, 새 도전은 성장 초기화·영구 골드와 해금 유지라는 안내와 세 시작 무기 버튼이 모두 들어옴 |
| `bot-bible.png`, `bot-jawbone.png`, `bot-sling.png` | 세 봇의 마지막 화면은 모두 R03 대기 모달임. 세 무기의 전투/명중 장면이나 자연 성장 선택 화면으로 세지 않음 |

R01의 남은 성장 선택을 해결하기 **전**의 실제 화면은 이 캡처셋에 없다. 해당 선택 화면/중복 입력의 시각 검증은 NOT_RUN이다. 자동 fixture가 `Resolve()`로 선택을 완료한 뒤 완료 화면을 촬영한다는 점을 소스에서 확인했다.

## 입력·저장·회귀 근거 확인

최종 XML을 직접 파싱해 EditMode 66/66, PlayMode 26/26, failed/skip 0을 확인했다. `AdamRoundPlayTests`를 읽어 다음을 구분했다.

- 660/1100 계속 버튼: 실제 전체 IMGUI 경로에 MouseDown/MouseUp 이벤트를 보내 R02 진입·새 World·시간0·RunId/골드 유지·저장 복원과 표현 초기화를 assertion한다. 외부 native 마우스 수용 시험은 아니다.
- 실제 InputSystem 이벤트: 이동키를 누른 상태로 전환한 뒤 release/repress 전 이동 차단, 중단/복귀 시 시간 정지, R02 접촉 사망 연출 중 digit 차단과 연출 후 R01 재도전, 살아 있는 R01 표현 캐시의 전환 초기화를 시험한다. Editor 시험용 background/focus 설정이 있으므로 OS foreground 입력 수용으로 확대하지 않는다.
- 표현 on/off: 같은 R02 저장 사본을 고정 스텝 실행해 실제 명중 >0과 Run/Profile/World 직렬화 동일성을 assertion한다. 저장 operation ID까지 포함한 전체 파일 bytes 동일이라는 주장은 아니다.

변이 XML과 명령 결과를 함께 읽었다. R01 전용 복원/선택 조건 제거/옛 World 유지/새 retry operation/옛 완료 UI가 각각 실제 시험 실패를 냈고 복구 Edit11·Play5가 통과했다. 현재 Game 소스의 옛 완료 UI 재확인도 두 크기의 계속 클릭 assertion 2 FAIL → 원복5 PASS다. 이번 리뷰어가 엔진 시험을 재실행한 것은 아니다.

`capture/persisted-r02.json`과 별도 프로세스의 `reload/reload-state.json`, `reload/reload-world.json`을 직접 파싱해 Run/Profile/World 정확 일치를 확인했다. 구현자 명령의 capture/reload exit0 및 후속 Commit 파일을 읽었다. 이 경로는 제품 Load 기술 증거이며 이번 리뷰어의 GUI 재개/수동 전투가 아니다.

세 무기 봇 결과는 원본 수치의 고정 1/60 tick·자동 이동/선택이다. 성경/턱뼈 각 R02 명중2/킬0/180초 완료, 물맷돌 명중521/킬360/회수341/약174.883초 완료라는 관측을 읽었다. 부상 HP1/접촉 배치의 별도 공격 시험, 고정 봇 생존, 강제 완료 캡처를 자연 플레이나 밸런스 수용으로 사용하지 않는다.

## 실행 범위와 소유권 반환

이번 검토는 자료·이미지·소스 읽기만 수행했다. 이전 작업의 foreground 도구 제약을 반복하지 않기 위해 선택 사항인 native 재실행을 생략했다. Player/Editor/GUI를 실행하거나 저장을 읽고 쓰지 않았고, 따라서 이번 리뷰어 자식 PID나 새 복원 작업은 없다. 구현자의 QA3 복원/PC6 보존 기록은 읽은 인계 자료이며 이번 리뷰어가 직접 복원했다고 주장하지 않는다. 기존 미소유 프로세스·옛 QA 경로·iOS 기기/앱/저장에 접근하지 않았다. 제품·다른 문서·빌드·commit은 변경하지 않았다. PC GUI 소유권을 반환한다.

최신 native의 계속/재개/지속 이동·성장 선택·사망 입력, 자연 수동 3분 플레이, 최종 R02 적 아트/애니메이션·혼잡 가독성, 실물 모바일·작은 기기·성능/GPU·청감은 NOT_RUN이다. 현재 표식은 개발용이며 pending 아트 QA의 FAIL을 해결하거나 최종 시각 PASS로 대체하지 않는다.
