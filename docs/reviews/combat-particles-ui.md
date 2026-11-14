# 제품 파티클 PC 독립 UI 검토 — 2026-11-14

판정: **검토 가능한 시작·저장 정지 모달은 이상 없음. 최신 native 전투·파티클 수용은 NOT_RUN.** 새 HIGH/MEDIUM 제품 결함을 재현하지 않았다. 이 결과를 P1 전체 수용 완료로 사용하지 않는다.

## 대상과 증거 경계

`docs/progress/combat-particles.md`와 CPT-01 비공개 인계를 전체 읽고, 전용 하네스 ui-ux 및 Orca 컴퓨터 사용 지침을 적용했다. 제품 소스·빌드·Editor·다른 문서는 변경하지 않았다.

검토 대상은 `evidence/combat-particles-cpt01-20261114/build-clear6bc5e6ab7de7/AdamParticlesQa.app` 하나다. 시작/종료 양쪽에서 소스 320개와 앱 파일 334개를 실제 대조했고 추가 파일도 없었다. 소스 manifest SHA256은 `c47eb8f52861e6f4726bdc873723ed951fd20f1da31374ef0a9c47142d74d338`, 앱 file-map SHA256은 `7bfce240fe329ead4563d412686748bf0ce5d7a5c160288bc505af0efb820ed5`다. 근거: `evidence/combat-particles-ui-review-20261114/preflight-hashes.json`, `final-binding.json`.

`-qaSaveProbe` 실제 exit 0을 확인했다. 회사·제품·bundle 일치, realpath가 PC 두 저장 디렉터리 및 이전 공유 QA 경로와 다름을 검증했다. 이전 미소유 앱/창/저장과 iOS 앱/기기/저장은 조회·조작하지 않았다. 개인 경로와 PID별 명령은 ignored 증거에만 보관한다.

## 이번 리뷰어의 실제 native 실행

| 확인 | 결과와 한계 |
| --- | --- |
| 1100×720 client 시작 화면 | 한국어 제목·세 무기명·선택 버튼 표시, 잘림 없음. `1100-start.png` |
| 키 2 시작 | 시작 메뉴에서 전투 저장 정지 화면으로 전환. 실제 최종 체크포인트는 턱뼈 Lv1, 경과 0.233333초, 명중/킬 0. 전투 성공 증거가 아님 |
| 1100 정지 모달 | 설명 두 줄·재개 버튼 표시, 불투명 하부로 적/제목 조각 비침 없음. `1100-jaw.png`, `1100-afterclick.png` |
| 종료 후 660×440 재실행 | 정상 저장 정지 모달 표시. 글자·버튼·안내는 창 안에 들어옴. `660-restart.png` |
| Enter/마우스 재개 | 도구 action은 성공 응답했으나 foreground를 유지하지 못하고 background-suspended 화면으로 돌아옴. 실제 재개 동작 PASS로 세지 않음 |
| foreground 제한 재시도 | 부모가 허용한 live PID/일치 창을 fresh 목록으로 확인하고 `--restore-window`를 정확히 1회 사용. Enter 후 10초 뒤에도 비활성 모달. `660-live-before-focus.json`, `660-focus-state.json`, `660-enter.json`, `660-live-state.json` |
| 정상 종료 | 두 subprocess 모두 실제 `wait()` exit 0. 종료 후 restore/재실행 명령 없음. `process-1-private.json`, `process-2-private.json` |

처음 단일 키를 hotkey로 전달한 요청은 도구가 modifier 필요 오류로 거절했으며, 안내에 따라 press-key로 바꾸었다. 지원하지 않는 기능키를 반복하지 않았다. AppKit exact-PID 활성화 및 도구의 synthetic/background 입력 성공 응답은 시뮬레이션 진행을 보장하지 않는다. 이 제약을 제품 정지 결함으로 분류하지 않았다.

최신 native의 3무기 명중 파티클, 지속 이동, 성장 선택, 피격/사망/신규 도전, 저효과/입자 toggle, active HUD, inset/창 resize 중 동작은 **NOT_RUN**이다. 저장 재실행은 모달 표시 확인 범위이며 Run/Profile/World 전체 동일성 재증명은 하지 않았다.

## 읽어서 확인한 구현자 증거 — native 성공과 구분

최신 CPT XML을 직접 파싱했다: EditMode 54/54, PlayMode 20/20. CPT-01 두 회귀는 baseline 2 PASS → 원래 Clear 본문 변이 2 FAIL(exit 2) → 복구 2 PASS다. 실제 `AdamGame`의 살아 있는 burst→Clear와 SaveStore no-Run Reload→Prepare 생략 경로를 쓰며, 보유 카메라가 꺼지고 다음 실제 projectile hit에서 같은 카메라가 재활성화되는 assertion이다. 현재 `AdamImpactParticles.Clear()`가 카메라를 즉시 비활성화하는 소스도 확인했다. 이 자료는 구현자 시험을 독립적으로 읽은 근거이며, 이번 리뷰어가 Unity 시험을 다시 실행한 것은 아니다. `reviewed-evidence-summary.json`에 XML 요약을 남겼다.

P1의 역사적 정지 PNG 세 표본(`capture660-handoff/jawbone-on/frame-042.png`, `capture660-handoff/bible-on/frame-042.png`, `capture1100-handoff/sling-on/frame-042.png`)도 직접 열었다. 해당 표본에서 전장과 HUD/하단 안내가 분리되고, 작은 화면의 글자/플레이어와 1100의 명중 섬광이 구별된다. 이 표본은 최신 앱에서 다시 촬영한 것이 아니고, 모든 파티클 시점이나 혼잡한 전투 가독성을 보장하지 않는다. P1의 15 PNG·12 pair·12 영상 및 40/120 적 성능 자료는 역사적 구현자 결과로만 취급하며 이번 native/성능 PASS로 전용하지 않는다. 영상 재생·청감 평가는 하지 않았다.

## 보존과 인계

두 own Player 정상 종료 후 현재 suffix bundle의 실행 writer 0을 확인했다. 이번 QA 저장 3개는 실행 직전의 부재 상태로 복원했고, canonical PC 저장 6개는 쓰거나 복원하지 않고 존재·SHA256 불변을 확인했다(9/9). 실행 중 만들어진 QA 파일은 복원 전에 ignored evidence로 추가 보존했다. 처음 보존 확인 스크립트에서 PC 항목의 `existed` 필드를 `exists`로 읽어 KeyError가 났으나, QA 복원 뒤에 난 읽기 확인 오류였고 필드 수정 후 9/9 검증을 완료했다. 근거: `preservation-results.json`, `own-bundle-after-private.json`.

PC GUI 소유권을 반환한다. 후속 게이트는 실제 foreground가 유지되는 환경에서 최신 앱의 전투/입자/성장/사망/resize를 확인하는 것이다. 3분 전체 수동 생존, 실제 모바일 기기, 작은 기기, 성능/GPU/전체 전투/최대 입자 수용과 청감은 계속 NOT_RUN이다.
