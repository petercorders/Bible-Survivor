# R03 Unity 통합 독립 코드 리뷰

2026-11-16 KST. **PASS — 검토 범위에서 열린 HIGH/MEDIUM 없음.** `docs/harness/r03-unity-integration-01.md`와 `evidence/r03-unity-integration-20261116/handoff.md` 기준의 Unity 연결을 검토했다. Core 실행02의 본인 독립 PASS·Core138/3변이는 재사용하며 제품 Core를 다시 리뷰하거나 실행하지 않았다. 화면 품질 판정은 별도 UI 리뷰 담당자 범위다.

## 검토한 위험 경로

- `AdamCatalog`는 canonical R1/R2/R3와 첫 보스 stats/공격/지원 계약을 읽고 Core 정의를 만든다. normal/intro/boss/reward/settlement와 between4 checkpoint를 해당 R03 정의로 연결하며 R04 전투는 노출하지 않는다. 보상 추첨·효과·골드 계산을 Unity에 복제하지 않았다.
- `AdamHost`의 다음 라운드 진입은 이전 완료 저장과 남은 성장 해결 뒤에 실행된다. reward는 Core의 안정 reward/candidate ID로 선택·명시 승격하고, settlement는 남은 성장 선택이 없을 때 Core receipt를 통해 한 번 처리한다. between4는 로드 가능한 완료 대기이며 새 도전은 명시 동작이다.
- 보상/성장/정산 적용 직후 `Checkpoint`가 같은 payload/revision/operation으로 저장한다. 실패 시 Error·Paused가 새 tick/선택/정산/체크포인트 생성과 덮어쓰기를 차단하고 `RetrySave`는 기존 operation을 재사용한다. suspend/quit도 실패한 payload를 새로 Capture하지 않는다. Reload는 마지막 disk state로 명시 복귀하고 자동 재지급을 수행하지 않는다.
- `AdamGame`의 숫자/카드 입력은 Core 후보 ID를 전달하고 stale/중복 선택은 Core 단계 검사로 거절된다. Error·suspend와 프레임 중복 입력을 차단하며 월드 교체 시 이동 release 경계를 유지한다. 보스 보상과 일반 성장 선택을 구분하고 남은 성장/정산/완료 대기를 표시한다.
- `AdamArtWorld`는 boss/지원도 Core `Targets`로 그린다. intro move0, Core boss clock 기반 windup0/1/2, NextAttackAt 변화의 impact2@10, 실제 보스 사망의 death4@6 매핑을 확인했다. 표현 시계는 Core 전투를 진행하거나 판정을 재생하지 않는다. World 교체/Reload는 이전 사건·표현 상태를 비운다.
- `AdamDangerRing`은 Core locked target·SlamRadius를 기존 `AdamWorldProjection`으로 투영한다. 배우/무기/입자 뒤에 그리고 HUD 이전에 그리며 optional FX 조건을 사용하지 않는다. sidecar PNG 외곽 크기로 판정을 추정하지 않는다.
- R03 loader/importer는 승인 index/manifest와8배우·그림자 sheet를 명시 매핑하고 frame/크기/pivot/PPU/필터/랩/밉맵을 검사한다. importer/build gate는 승인 PNG와 고정 sprite registration을 확인한다. 기존 Art03/R02는 별도로 유지하고 Bible sidecar는 기존 계약 중 book/glow2개만 치환한다. 필수 위험 경계에는 optional PNG를 요구하지 않는다.

## 재사용한 검증 근거

`evidence/r03-unity-integration-20261116/`의 실제 결과를 읽어 handoff와 대조했다.

- `final-EditMode/results.xml`: **97 passed**, failed/inconclusive/skipped0. `final-PlayMode/results.xml`: **31 passed**, failed/inconclusive/skipped0. `final-engine.json`의 두 Editor 실행 및 wrapper exit0과 일치한다. 관련 `related.xml`도17/17이다.
- `AdamR03HostTests`는 intro/예고 중 exact World·Boss 복원·정지, reward/settlement 저장 실패 후 payload 동결·같은 retry·중복 거절, 실제로 남은 성장1건의 정산 차단, Core 반경 투영과 clip frame을 검증한다. 기존 라운드 시험은 R03 진입과 살아 있는 도전 덮어쓰기 거절을 보존한다.
- `build-command.json`은 단일 macOS 빌드 exit0이다. capture helper는 R1/R2 timeout·sling8/stones1/HP10000/seed7의 명시 fixture를 만든 뒤 실제 `Host.Tick`/좌표 이동·Core 무기 경로로 R03을 진행한다. R03 피해/처치를 주입하지 않는다. 실제 UI `SelectSlot`으로 진화를 적용하고 중복 보상/정산을 거절한다.
- `capture/continuous-trace.txt`와 관련 marker는 normal→intro→boss/지원→lowFX 예고의 exact disk restore→실제 투사체 보스 처치→동일 보상 후보 disk restore→진화→정산/between4를 기록한다. 연속 실행의 성장 처리 loop는 관련 시험의 비어 있지 않은 성장 queue와 구분한다. 최종 골드306은 이 fixture의 관측값이다.
- 별도 새 Player 프로세스 Load는 `expected-run.json`/`expected-profile.json`과 Run/Profile 동등·완료 receipt·중복 정산 거절을 확인했다. quit checkpoint가 revision/operation을 바꾸므로 primary 전체bytes 동등을 주장하지 않는다.
- `sync.json`, `checks.json`, `binding.json`, `core-provenance.json`의 기존 기록은 최신 Core 원본/선택 DLL/앱 embedded DLL 일관성을 지원한다. 선택·provenance·embedded의 DLL SHA는 모두 `409909ec918d0e3ca0a8357dddf8b76d74d2386617514ea65fd551af5754f040`이다. local project/portable 검사 exit0과 `CoreSourcesUnchanged:true`를 재사용했다. 새 전체 해시 계산은 하지 않았다.

## 한계와 인계

리뷰 중 Editor/빌드/Player/GUI/추가 probe·전체 검사·픽셀/해시 행렬을 실행하지 않았다. 기존3장 캡처의 화면 품질을 이 코드 리뷰 PASS로 대신하지 않는다. 자연 난이도 완주·native 정밀 타이밍·모바일/IL2CPP/기기·성능은 NOT_RUN이며 R04는 미구현 대기다.

`path-probe.json`·`restoration.json`의 새 QA identity/무저장 probe·정확한 자기 자식 종료·자기 파일만 원복한 근거를 읽었으며, 실제 사용자 저장·금지 identity/프로세스는 조회하거나 조작하지 않았다. 이 리뷰는 제품/시험/아트/DLL/provenance를 편집하지 않았고 본 문서만 소유한다. usage는 `r03-unity-code-review` start/end 세션 구간으로 기록하며 독립 worker 청구 귀속·절감률은 추정하지 않는다.

이 Unity 통합의 독립 코드 리뷰 게이트는 PASS다. 별도 UI 판정과 coordinator의 종료 게이트/선별 커밋·CI는 이 문서가 대신하지 않는다.
