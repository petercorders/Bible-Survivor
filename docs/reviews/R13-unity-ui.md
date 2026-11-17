# R13 최종 독립 PC 정적 UI 검토

**PASS — 지정 3 PNG의 정적 관측 범위. 승인 차단 HIGH/MEDIUM 없음.** LOW 1건을 아래에 한정 기록한다. 2026-11-18 KST, task `task_841ccdbcbfb1` / dispatch `ctx_6a0b2d08a4c0`. 구현자와 독립된 Codex worker가 단독 검토했다.

repo AGENTS.md, `evidence/r13-unity-integration-20261118/handoff.md`와 `binding.json`을 읽고 binding이 지정한 원본 3장을 실제 `view_image`로 직접 열었다. 최종 앱 identity는 `com.biblesurvivor.adam.r02artqa.r1304d367eb8f05`, 정상 build 1회라는 생산자 기록을 재사용했다. 리뷰어가 앱·GUI·Editor 소유를 취득하거나 실행한 것은 아니다. 새 캡처/이미지 변형/전체 픽셀 계산/hash 재계산/전체 시험을 하지 않았다.

## 직접 관측

경로는 모두 `/Users/gimgibong/Bible-Survivor/evidence/r13-unity-integration-20261118/capture/` 아래 원본이다. 좌표는 대략값이다.

| 원본 | 관측 | 상태 |
|---|---|---|
| `01-660-miriam-normal.png` (660×440) | `출애굽기 4 / 미리암`, 경험치12.0/레벨23과 체력/시간/골드가 검은 상단 HUD 안에 읽힌다. HUD 약 y55–99와 배우가 있는 경기장 약 y116–362가 분리되어 XP와 배우 겹침이 없다. 좁은 회색 윤곽, 굽은 몸체, 큰 자루/둥근 몸체의 ex10~12 형태가 보이고 길·잔디·중앙 플레이어와 구별된다. | PASS(정적) |
| `02-660-miriam-lowfx.png` (660×440) | 같은 배치와 배우 형태가 유지되고 하단 F6 안내가 효과 낮음으로 읽힌다. XP HUD/배우 분리가 유지된다. 정적 캡처에서 새 위험 효과나 과도한 가림은 관측되지 않는다. | PASS(정적) |
| `03-1100-miriam-complete-r14-wait.png` (1100×720) | `출애굽기 4 완료 · 진행 저장됨`, `완료 골드 +240 · 진행 유지`, `R14 대기 · 다음 라운드 준비 중`, `새 도전 선택 시 성장·장비 초기화`가 구분되고 세 새 도전 버튼이 패널 안에 온전히 표시된다. 완료240은 아래 trace와 일치한다. | PASS(정적) |
| 세 원본 공통 | 가리는 콘솔 창/오류 패널은 보이지 않는다. 오른쪽 아래 Development Build 표시는 있으나 핵심 HUD·완료 문구·선택 버튼을 가리지 않는다. 지정 문구/버튼의 화면 경계 잘림은 관측되지 않는다. | PASS(관측 한정) |

LOW R13-UI-L01: 두 660 화면의 진화 forms 흰 글자 행(약 x160–496/y176–190)이 경기장 위에 있어 중앙 위쪽의 일부 적 스프라이트와 겹친다. 플레이어(약 x330/y225)나 XP HUD를 가리지 않으며 주변 적의 반복 표본으로 형태를 구분할 수 있어 현재 대표 화면의 차단 MEDIUM으로 보지 않는다. 향후 HUD 개선 시 해당 행을 배우 영역 밖으로 옮기는 최소 배치 조정을 검토할 수 있다. 이번 작업에서는 수정하지 않았다. 움직이는 군집에서의 인지/회피 성능을 PASS로 확대하지 않는다.

## 숫자 대조와 근거 경계

원본 완료 문구를 +240으로 판독했고 `capture/continuous-trace.txt` frame41의 `Core240/null unlock/heal once`, 새 프로세스의 `no reset or duplicate240` 기록과 대조했다. R12에서 생긴 숫자 오독을 반복하지 않도록 정확한 원본 경로와 판독 문구를 남겼으며, 현재 숫자 불일치는 없다. 코드 표시값 조사나 제품 수정은 필요하지 않았다.

생산자 근거만 재사용: Edit84 고유 사례=최초83 PASS+stale LastInstalledRound assertion 교정1 PASS(초기 실패 XML/exit2 보존; 단일84/84 XML 주장 아님), Play13/13. normal과 완료는 각각 candidates1/pending1/actualChoose1, 각 growth25 및 완료240 once다. 25 지급 두 경로를 하나로 합치거나 본인 시험 횟수에 합산하지 않는다. 생산자의 정확 paused World3 Load/새 프로세스 완료 Load와 실제 click/InputSystem Enter는 재사용 근거이며 정적 PNG로 조작·저장·정지 동작을 입증한 것은 아니다.

기술 inherited HP10000/gear8/3forms/6slots, timeout/directkill/drop discard/cycle1000 준비를 자연 검증으로 확대하지 않는다. 자연1080kill/180초 밸런스·앱 R01~R13 자연 연속 완주·native/device/mobile/performance는 **NOT_RUN**. 본인은 원본/제품/앱/GUI/Editor/사용자저장/보호identity를 실행·수정·접근하지 않았고 추가 worker/commit/push도 NOT_RUN이다. 지정 세 리뷰 문서만 작성하고 타인 변경을 되돌리지 않았다.

## 사용량·모델

같은 worker 세션 `01a1ea49-7c85-7cb0-854f-074ad240421e`, phase `R13-independent-static-UI`, 관측 gpt-6.1-sol, effort 미측정. Claude/추가 worker 없음. `tools/record_model_usage.py --checkpoint start|end --ledger /dev/null` 양 명령 exit0이며, 허용된 문서 외 ledger 생성 없이 출력 카운터만 기록한다.

- start recorded_at `2026-11-17T17:11:00.507583+00:00`, event `2026-11-17T16:18:26.871Z`: input792714/cached736512/cache_write0/output9134/reasoning521/total801848. start event는 이전 작업 뒤의 오래된 provider 기준점이다.
- end recorded_at `2026-11-17T17:11:14.896934+00:00`, event `2026-11-17T17:11:06.170Z`: input951831/cached880128/cache_write0/output9639/reasoning521/total961470, resultPASS. 문서 작성·최종 인계와 아직 telemetry에 반영되지 않은 후행 구간은 포함하지 않는다.
- 자동 delta_status BASELINE; 누적 수치를 R13 정확한 전체 사용량으로 주장하거나 재계산하지 않는다. reasoning은 output에 포함되므로 별도 합산하지 않는다. 청구·가격·요청 귀속 근거가 없어 비용/API equivalent cost/절감률 **미계산**, 미측정 구간을0으로 세지 않는다.
