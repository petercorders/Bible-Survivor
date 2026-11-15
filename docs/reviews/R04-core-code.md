# R04 Core 독립 코드 리뷰

2026-11-16 KST. **PASS — 검토 범위에서 열린 HIGH/MEDIUM 없음.** `docs/harness/r04-implementation-01.md`의 R03 완료→R04/D3→성장 후 정산/between5 계약을 현재 고정 소스와 관련 근거로 검토했다. R03 Core 독립 PASS는 재사용한다. 이 판정은 Unity/PC 앱 통합 완료가 아니다.

## 확인한 동작

- R03 between4에서 R04로 진입할 때 완료한 보스/보상 receipt를 `CompletedBoss`로 보존하고 장비·첫3진화·경험치·영구 snapshot·킬 remainder를 유지한다. 새 Wave/World·피격 시계로 시작하며 살아 있는 이전 세션은 새 라운드를 조작하지 못한다. canonical600/XP1600/동시70/생성170초·종료180초와 D1/D2/D3를 사용한다.
- D3는 실제 생성시각+6초에 플레이어 target/방향을 고정한다. 예고1.2초 동안 추적을 멈추고 속도비1.3으로0.6초 직선 이동한 뒤 종료+6초 재사용한다. `MoveCharge`가 fixed step 안의 단계 경계를 남은 구간만 적분하므로 기본속도3에서 돌진 거리는2.34다. 겹침 방향은(1,0)이며 기존 접촉 피해10/공통 무적을 사용하고 추가 돌진 피해는 만들지 않는다.
- pause·성장 선택은 World 진행을 차단한다. 실제 처치와 동기화/종료 경로에서 해당 ID의 charge 상태를 지우고 새 actor ID는 자기 SpawnedAt으로 새 clock을 만든다. 사망 후 보유 World를 다시 호출해도 돌진 시간이 진행하지 않는다.
- World3의 명시 charge DTO는 version/ID/phase/NextAt/Until/고정 target/단위 direction을 저장한다. 현재 actor 목록과의 일치·timing/geometry·미래 World/charge version·phase/중첩 필드·과거 보스 receipt의 미래 버전 보호를 확인했다. 예고/돌진 Restore는 생성자 초기 상태를 저장된 state로 교체하고 World RNG/잔여 fixed step/무기·진화 ledger도 기존 경로로 복원한다.
- R04 완료는 떨어진 XP만 정산하고 `round_settlement`에서 남은 성장을 해결한다. `NormalEndedAt`으로 끝난 전투 시간을 유지하며 마지막 선택 후에만65골드/fire_pillar 해금/현재 최대HP 회복/완료 flag/between5를 한 번 적용한다. 반복 완료와 동일 operation commit은 재지급하지 않는다. 미구현 불기둥을 공격·획득 후보에 넣거나 R05 전투를 시작하지 않는다.
- 새 계약은 R04 fingerprint에만 추가한다. 이전 round/World serializer 선택 필드의 기본값 미출력과 R1~R3 경로를 유지한다. 미래 입력 시험은 새 지원 상한 다음 값으로 옮겼고 기존 resume 변이는 지원 상한 anchor만 갱신한다. 이전 완료 해금/골드의 고유 anchor를 중복시키는 변경은 없다.

## Unity에 넘기는 실제 geometry와 한계

표현에 필요한 phase/고정 target/고정 direction/Until·NextAt은 `Capture().Charges`에, 현재 actor 위치와 실제 기본속도는 같은 snapshot의 Enemies/Tuning에 있다. 예고 시 실제 예정 endpoint는 **현재 actor 위치 + direction × PlayerSpeed ×1.3×0.6**, 돌진 중 남은 endpoint는 **현재 actor 위치 + direction × PlayerSpeed ×1.3×max(0,Until−Time)**로 산출할 수 있다. 피해 경계의 반폭은 기존 PlayerRadius+EnemyRadius다. direction/target을 재조준하거나 PNG 치수를 판정으로 쓰지 않아야 한다.

별도의 불변 windup-start/endpoint 필드는 없다. 기존 턱뼈 넉백이 예고/돌진 중 actor 위치를 바꿀 수 있으므로, 최초 시작좌표에 고정한 아트선은 이후 실제 경로와 달라질 수 있다. 현 Core 계약은 target/direction 고정이며 불변 시작좌표를 요구하지 않으므로 이를 Core 결함으로 세지 않았다. Unity 통합자는 같은 snapshot의 live 위치·남은시간을 사용하고 예고 중 넉백·중간 Load의 판정/표현 정합을 연결 범위에서 확인해야 한다. 변경된 경로를 불변 아트선으로 대신하는 것은 이 PASS에 포함되지 않는다.

## 재사용한 실행 근거

- 관련 `--r04-only`는 **7/7 exit0**이며 `RoundFourTests`의 진입/receipt·진화 유지, D3 고정 target/실제 거리·재사용, 정지/죽음/새 ID, 예고·돌진 disk replay, 성장 후 정산 once/between5, 접촉 무적/겹침, 미래 원문 보호를 확인했다.
- `evidence/r04-core-01/summary.json`과 실제 로그는 일반 chase 분기로 돌아간 원래 구조 및 Restore charge state 무시의 **2개 변이를 실제 시험 실패 exit1**로 검출하고 `restored.log`의7/7 exit0을 기록한다. 컴파일 실패를 기능 검출로 세지 않았다.
- `evidence/r04-core-01/cli-result.json`: 대표 `--r04-series sling 7` exit0. 실제 좌표 전투600킬/1653hits·177.1667초·골드492/최대HP10000·sling_fivefold·R03 완료 receipt·R04 완료 receipt/between5를 보존한다. R1/R2 timeout·장비8/보조1·HP10000의 기술 fixture다.
- 이 CLI의 D3 restore는 **cooldown**이며 `observed_telegraph_or_charge=false`다. 강한 진화 무기가 첫 예고 전에 처치했기 때문이다. 실제 예고/돌진·해당 단계 disk replay는 별도의 고정좌표 시험 근거로 한정한다. CLI가 돌진을 관측했다고 확대하지 않는다.

검토자는 시험·변이·CLI를 추가 실행하지 않았으며 기존138/18/10/30·전행렬·빌드·Unity/GUI·전체 해시·캡처도 반복하지 않았다. 최종 test wiring은 boss-only/reward-only가 R04 시험을 추가 실행하지 않게 제한하며 r04-only의7개 범위는 유지한다.

## 인계

Unity/DLL/provenance 동기화·아트 품질·실사용 저장·PC 실제 앱·모바일/성능·자연 밸런스는 NOT_RUN/후속 범위다. R05는 미구현 대기다. 제품·시험·도구·Unity·아트는 편집하지 않았고 본 리뷰 문서만 소유한다. usage는 `r04-core-review` start/end 세션 구간으로 기록하며 worker별 독립 비용/절감률은 추정하지 않는다.

이 순수 Core slice의 독립 코드 리뷰 게이트는 PASS다. 다음 단일 Unity 통합은 위 live geometry 연결 조건과 별도 관련 검증·독립 코드/UI 리뷰를 따른다.
