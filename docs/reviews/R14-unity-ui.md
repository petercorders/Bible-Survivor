# R14 최종 독립 PC 정적 UI 리뷰

**PASS — 지정 원본 세 PNG의 정적 관측 범위. 차단 HIGH/MEDIUM 없음.** LOW 1건은 아래에 한정 기록한다. 2026-11-18 KST, task `task_eba75d6fd535` / dispatch `ctx_7cf411e36188`, 구현자와 별도 Codex worker 단독 검토.

AGENTS.md와 `evidence/r14-unity-integration-20261118/handoff.md`, `binding.json`의 지정 근거를 읽었다. 최종 앱 identity `com.biblesurvivor.adam.r02artqa.r1446fdab19d0bf`, 정상 mac build1회는 생산자 기록 재사용이다. 원본 PNG는 모두 `view_image`로 실제 직접 확인했다. 앱/Editor/GUI 실행 소유는 취득하지 않았다.

## 직접 관측

경로 접두는 `/Users/gimgibong/Bible-Survivor/evidence/r14-unity-integration-20261118/capture/`이며 좌표는 대략값이다.

| 원본 | 관측 | 상태 |
|---|---|---|
| `01-660-aaron-initial-left-right.png` (660×440) | 출애굽기5/아론, 시간3.0/180초가 표시된다. 중앙 플레이어 양옆에 낮은 네발 회색 실루엣의 ex13 배우가 분리되어 보이며 길·잔디와 구분된다. 좌우 생성량/독립HP는 픽셀을 새로 세어 입증한 것이 아니라 아래 생산자 Core 근거 재사용이다. | PASS(정적) |
| `02-660-aaron-lowfx.png` (660×440) | 회색 네발, 황색 들린 팔/귀 형태, 밝은 둥근 몸체의 혼합 배우가 구분된다. XP/레벨 등 HUD(약 y55–99)는 경기장(약 y116–362)과 분리되어 배우와 겹치지 않는다. 중앙 플레이어도 읽힌다. F6 효과 낮음 안내와 나머지 하단 키 안내가 보인다. | PASS(정적) |
| `03-1100-aaron-complete-r15-wait.png` (1100×720) | `출애굽기 5 완료 · 진행 저장됨`, `완료 골드 +260 · 진행 유지`, `R15 브살렐 대기 · 다음 라운드 준비 중`, `새 도전 선택 시 성장·장비 초기화`와 세 새 도전 버튼이 온전히 읽힌다. 아론 문맥은 위 전투 화면 제목과 연결된다. | PASS(정적) |
| 세 원본 공통 | 화면을 가리는 콘솔/오류 패널은 보이지 않으며 핵심 HUD·완료 문구·버튼의 잘림은 관측되지 않는다. Development Build 워터마크는 우하단에 있으나 핵심 정보와 버튼을 가리지 않는다. | PASS(관측 한정) |

LOW R14-UI-L01: 660 혼합 lowFX 원본의 진화 forms 흰 글자 행(약 x160–496/y176–190)은 경기장 위에 있어 약 x190 및 x450 부근 적의 일부와 겹친다. XP HUD 및 중앙 플레이어는 가리지 않고 다른 배우 표본에서 형태가 읽혀 이번 정적 관측의 차단 MEDIUM으로 보지 않는다. 향후 배치 개선 시 forms 행을 배우 영역 밖으로 옮기는 최소 조정을 검토할 수 있다. 현재 수정·재빌드 권고나 새 캡처 요구는 없다. 움직이는 군집 인지 성능으로 확대하지 않는다.

## 숫자 대조·재사용·미실행

원본 완료 금액은 **+260**으로 직접 판독하고 `capture/continuous-trace.txt` frame72의 `Core260/null unlock/heal once`, 새 프로세스 `no reset or duplicate260`과 대조했다. R15 브살렐 대기/초기화 문구도 원본에서 읽었다. 숫자 불일치나 재빌드 사유는 관측되지 않아 추가 코드 조사/전체 픽셀 계산을 하지 않았다.

생산자 근거만 재사용: 실제 Core3초 좌우4/4, 독립HP Hit7→213/neighbor220 및 kill isolation, World3 disk Load/네 receipt/forms/profile/RunId 유지, normal·완료 각각 candidates1/pending1/actualChoose1과 growth25 각1회 및 완료260 once/새 프로세스 정확 Load. Edit101 고유사례=최초100 PASS+직접 Session.Kill missing-drop-geometry fixture 교정1 PASS(초기 XML/exit2 보존; 단일101/101 XML 주장 아님), Play14/14의 실제 click/InputSystem Enter도 생산자 기록이며 본인 실행에 더하지 않는다. 정적 PNG로 이동·독립HP·입력·시간 정지·저장 원자성/중복방지를 입증하지 않는다.

기술 inherited HP10000/gear8/3forms/6slots, timeout/directkill/drop discard/cycle1000 준비를 자연완주로 확대하지 않는다. 자연1140kill/180초 밸런스·앱 R01~R14 자연 연속 완주·native/device/mobile/performance는 **NOT_RUN**. 본인은 원본/제품/앱/Editor/GUI/사용자저장/보호identity 수정·실행·접근, 추가worker, commit/push를 하지 않았다. 새 캡처/전조합/이미지행렬/hash/전체시험 반복 없이 허용 세 문서만 작성하고 타인 편집을 보존했다.

## 사용량

세션 `01a1ea49-7c85-7cb0-854f-074ad240421e`, phase `R14-independent-static-UI`, 관측 gpt-6.1-sol, effort 미측정. Claude/추가 worker 없음. `tools/record_model_usage.py --checkpoint start|end --ledger /dev/null` 양 명령 exit0, 출력 카운터만 보존하여 소유 외 ledger 파일을 생성하지 않았다.

- start recorded_at `2026-11-17T17:49:14.209462+00:00`, event `2026-11-17T17:12:36.900Z`: input1311898/cached1236992/cache_write0/output13649/reasoning734/total1325547. 오래된 provider baseline event이며 정확한 R14 시작 사용량으로 확대하지 않는다.
- end recorded_at `2026-11-17T17:49:26.379022+00:00`, event `2026-11-17T17:49:20.016Z`: input1501921/cached1422848/cache_write0/output14136/reasoning734/total1516057, resultPASS. 문서 작성/최종 인계 및 telemetry 지연 구간은 미포함이다.
- 자동 delta_status BASELINE, 총량 재계산 없음. reasoning은 output에 포함되어 별도 합산하지 않았다. 실제 청구·가격·요청별 귀속 근거가 없어 비용/API equivalent cost/절감률 **미계산**. 미측정을0으로 합산하지 않는다.
