# iOS Simulator M0 — 독립 UI 검토

2026-11-14. 검토자 `unity_ui_review`. **PASS — 지정 Simulator phone profile의 아래 실제 조작 범위, 미해결 HIGH/MEDIUM 없음.** 모바일 전체 지원/실물/성능 통과가 아니다. 구현자가 작성한 `docs/progress/mobile-ios.md`와 최종 증거를 먼저 읽고, 인계된 최종 앱을 지정 QA 기기에서 독립 실행했다. 제품 코드·빌드·다른 문서·커밋은 수정하지 않았다.

## 범위와 재현 조건

- iOS26.2 Simulator의 지정 phone profile, portrait1206×2622 / 관측 landscape2622×1206. 동일 outline-font/Art03가 적용된 최종 Simulator arm64 IL2CPP 앱이다. 출시 방향이나 지원 기기를 새로 확정하지 않았다.
- QA 앱/기기만 boot/launch했다. 기존 Simulator GUI와 기존 다른 기기/창에는 입력하지 않았다. 매 native action은 fresh 목록으로 확인한 QA 창ID를 명시했다. 창ID·UUID·컨테이너·PID·개인 경로는 ignored private evidence에만 남겼다.
- 최종 소스/설정/앱277파일 해시가 구현자 최종 manifest와277/277 일치했다(`artifact-match.json`). 빌드·Unity42/15 시험·linker 제거 변이는 구현자의 실행 증거이며 독립 검토자가 다시 빌드/실행한 것으로 세지 않는다.
- 본인 실행 전 QA primary/backup/lock3개와 PC 두 저장 위치의6개 상태를 백업/해시 기록했다. 메뉴 재현에는 구현자의 정상 terminal checkpoint를 QA 앱 종료 상태에서만 사용했다. 처음 세로 진행의 레벨업은 본인이 새 턱뼈 도전에서 자연 도달했고, 뒤의 가로 카드 비교는 구현자의 자연 성장 checkpoint를 재사용했다.

## 본인이 실제 실행한 경로

| 과업 | 실제 결과·관측 |
| --- | --- |
| 세로 시작 | 세 시작 무기 버튼·설명은 notch 아래 안전영역 안에서 읽히고 터치할 수 있다. 터치2로 턱뼈 한 개만 시작, 새 저장에 jawbone1 확인. |
| 세로 전투·성장 | 실제 자동 전투에서 자연 LV2 모달 도달. 가시 면류관/요한의 팬티/동풍의 보조 종류·신규0→1/5·실효과를 비교할 수 있다. 두 줄 카드/하단 안내가 패널 안에 있고 배경이 글자에 비치지 않는다. 터치2 후 underwear1만 적용되고 choices가 비워졌다. |
| 세로 터치 이동 | 모달 종료 뒤 별도 lower-left drag, 저장 PlayerX0→0.30421868/Y0. 해당 입력 뒤 native Pause로 time20.7833s/HP12에 정지했다. 도구 응답의 synthetic/unverified만으로 성공 처리하지 않고 실제 저장 변화와 화면을 대조했다. |
| 정지·재개 | 세로/가로 모달에 읽을 수 있는 재개 버튼과 모바일 안내가 있고 실제 터치로 재개한다. 가로로 돌아온 뒤 Home 전까지 time21.4167s로 진행했다. |
| 회전 | QA Rotate 조작 후 전환 애니메이션이 끝난 가로 화면에서 safe area 내부 모달이 수평으로 안정된다. portrait 정지 checkpoint와 landscape 정지 checkpoint의 전체 Run/Profile이 같다. 순간 회전 중 캡처는 지원 실패로 판단하지 않았다. 다른 가로 방향/upside-down을 검증한 것은 아니다. |
| 백그라운드·복귀 | 가로 재개→QA Home 버튼→같은 앱 launch 복귀. Home 뒤 저장과 복귀 뒤 저장의 전체 Run/Profile 동일, 명시적 재개 모달 표시. 시간 점프 없음. 이동X는0.30421868로 유지돼 이전 drag의 잔류 이동이 관측되지 않았다. 실제 전화/실물 잠금은 시험하지 않았다. |
| 앱 종료·재실행 | QA simctl terminate0→launch0 후 전체 Run/Profile 동일, 장비·성장·위치·HP·골드·시각 복원 및 정지 모달. `restart-check.json`의 두 비교 모두true. 이 독립 실행은 명시 종료/재실행이며 구현자의 SIGKILL 시험을 본인 시험으로 보고하지 않는다. |
| 가로 시작·이동 | 별도 정상 terminal fixture 후 가로 시작 메뉴 터치3으로 sling1만 시작. 실제 lower-left drag 후 PlayerX0→0.196875, native Pause 저장 time16.0833s. 세로와 같은 입력 벡터를 사용하나 물리 멀티터치는 아니다. |
| 가로 성장 카드 | 구현자의 자연 LV2 checkpoint를 QA 종료 중 복사/재실행해 동일 세 카드 비교. 이름·종류·레벨·효과 두 줄/안내 읽힘, 잘림/숨은 HUD 조각 없음. 터치1로 crown1만 적용되고 choices가 비워졌다. |

이 범위에서 기존 PC UI-01의 원시 ID만 표시하는 문제나 UI-02의 재개 버튼 숨김이 재발하지 않았다. 관측 글자의 픽셀 선명도/효과 정보 비교는 양호했다. 약44pt 버튼/폰트 크기의 수치 근거는 구현자의 profile/기하 측정이며, 본 검토는 실물 손가락 가림/가독성 인증이 아니다. 화면의 큰 빈 공간/정수 배율을 작은 기기·모든 화면비 지원으로 일반화하지 않는다.

## 구현자 증거와 미실행 경계

구현자 기록의 실제 빌드/serializer 두 타입 보존·writer/reader 제거 변이, 권한 실패 재시도/backup/미래schema, Unity42 EditMode/15 PlayMode와 native 사망 영상은 읽어 확인했으나 본인 실행으로 합산하지 않았다. 독립 소스 읽기에서 safeArea GUI/터치 공통 좌표, release gate, 회전/중단 명시 재개, 사망 `SelectSlot` 공통 가드를 확인했다. 이는 실제 해당 경계 조작을 전부 수행한 것과 다르다.

다음은 그대로 **NOT_RUN / NOT_TIMED**다.

- native0.75초 안의 정밀 터치 차단: **NOT_TIMED**. 이번 검토는 timing loop를 반복하지 않았으며 구현자의 native 두 번 탭 요청도 정밀 전달 시각 증명으로 전용하지 않는다.
- 실물 멀티터치/손가락 교체·전화/잠금/전원 손실·발열/장기 메모리/성능, 다른 작은 phone/tablet profile·반대 landscape/upside-down, 전체 한글 렌더/접근성 보조 기술, 모바일3분 수동 생존·완료/보상 전체 경계.
- Android 실행과 스토어/계정·개발 서명/실물 pairing 변경. Simulator PASS로 대체하지 않는다.

## 증거·정리·소유 반환

Ignored root: `evidence/mobile-ios-ui-review-20261114/`. Native JSON/PNG22장은 `ios-portrait-*`, `ios-growth-choice`, `ios-drag-after-choice`, `ios-landscape-*`, `ios-home`, `ios-background-return`, `ios-restart-paused`로 보존했다. `observed-summary.json`은 본인 저장 관측, `background-check.json`/`restart-check.json`은 Run/Profile 비교다. 명령/실제 exit·개인 실행 식별자는 `setup-commands-private.json`, `restart-commands-private.json`, `cleanup-commands-private.json` 등에 보존했다. native 도구의 성공 반환과 화면/저장 성공을 구분했다.

마지막 QA 앱 terminate와 QA 기기 shutdown은 각각 실제exit0이다. 종료 뒤 본인 실행 전 QA3파일의 bytes/존재/권한을 복원하고 PC6파일을 변경 없이 대조했다. `restoration.json`9/9true. `device-preservation.json`은 기존 모든 기기 상태 동일/원래 Booted1 유지/QA Shutdown을, `window-preservation.json`은 기존 Simulator PID와 기존 창 보존/QA 창 없음 모두true를 기록한다. 기존 Simulator 앱을 종료하거나 다른 기기를 shutdown하지 않았다.

검토 범위의 수정 요구 HIGH/MEDIUM은 없다. GUI/QA 기기 소유를 coordinator에게 반환하며 남은 실물/작은 기기/성능 게이트는 열린 상태로 유지한다.
