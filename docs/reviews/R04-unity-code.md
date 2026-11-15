# R04 Unity 독립 코드 리뷰

2026-11-16 KST. **PASS — 검토 범위에서 열린 HIGH/MEDIUM 없음.** `docs/harness/r04-implementation-01.md`와 `evidence/r04-unity-integration-20261116/handoff.md` 기준의 Unity 변경만 검토했다. 본인의 R04 Core 독립 PASS·관련7/최소2변이·대표 실제600킬 CLI는 재사용하며 제품 Core를 다시 리뷰하거나 실행하지 않았다. 화면 품질은 별도 독립 UI 판정 범위다.

## 검토 결과

- Host/catalog는 R03 between4→R04 normal/round_settlement→between5를 올바른 정의로 연결한다. R03 장비·진화·완료 receipt를 유지하고, pending 성장이 있을 때 완료 지급·회복을 보류한다. 마지막 성장 후 Core의 완료65골드/fire_pillar 해금/최대HP 회복/receipt를 저장한다. 불기둥 장착·공격 및 R05 전투는 노출하지 않는다.
- 새 완료 adapter는 기존 Checkpoint/RetrySave를 사용한다. 저장 실패의 Error gate가 tick·선택·새 정산을 차단하고 같은 State/revision/operation을 retry한다. 이미 memory에 적용된 완료를 다시 호출해 지급하지 않는다. R03 완료는 새 도전 가능한 최종 상태에서 R04 진입 대기로 바뀌며 최종 새 도전 경계는 between5다.
- `AdamChargeLane.TryGeometry`는 같은 World snapshot의 live actor 위치·고정 direction·실제 PlayerSpeed/ChargeSpeed를 사용한다. telegraph는 전체0.6초 거리, charge는 max(0,Until−Time)의 남은 거리, 반폭은 PlayerRadius+EnemyRadius다. 고정 PNG/보드 치수를 사용하거나 target을 재조준하지 않는다. 실제 넉백으로 배우가 이동하면 시작·끝이 함께 이동하므로 앞 Core 리뷰의 불변 windup-start 부재를 잘못된 정적 선으로 보완하지 않는다.
- 위험 capsule과 방향 표시는 공유 WorldProjection으로 배우·무기·입자 위/HUD 아래에 그린다. optional FX 조건이 없어 lowFX에도 유지되고, 실제 비활성/완료 World에는 남기지 않는다. 표현용 clamp나 추가 피해/판정을 만들지 않는다.
- D1/D2/D3 move와 D3 windup/dash 매핑을 확인했다. windup은 Core 남은 예고시간으로2프레임, dash는 실제 진행시간으로 재생한다. loader/importer는 승인 index/manifest·5PNG/16프레임의 hash/등록/피벗/PPU·필터/랩/밉맵을 검증하고 기존 Art03/R02/R03/Bible/font를 별도로 유지한다.

## 재사용 근거와 정확한 한계

`evidence/r04-unity-integration-20261116/`의 실제 명령 결과·XML·QA helper·trace·binding을 대조했다.

- related.xml은 **21 passed**, 최종 EditMode는 **102 passed**, PlayMode는 **31 passed**이며 각 failed/inconclusive/skipped0이다. 최종 engine/관련 명령 exit0, 실제 compile·단일 mac Development build exit0, project/portable/diff 검사 exit0을 재사용한다. 관련21 이후 보강한 실제 Core 넉백 시험은 최종 Edit102 범위다.
- `AdamR04HostTests`는 R03 receipt/진화 유지·성장 전 정산 차단/완료 once, 예고/돌진 pause·정확 disk replay, custom Tuning/live geometry와 남은시간, **실제 진화 턱뼈 명중의1unit 넉백 후 고정 target/direction·이동 endpoint**, 완료 저장 실패 후 frozen payload/retry를 다룬다.
- 연속 Player trace는 이전 R03 앱의 완료 JSON을 seed로 R04에 진입하고 D3 예고/돌진/cooldown·두 단계 World/geometry disk Load·완료65/해금/회복/between5를 확인했다. 새 프로세스 Load는 expected Run/Profile과 정확히 비교하고 중복 완료를 거절했다. quit의 revision/operation 변경은 전체 primary bytes 동등 주장에 포함하지 않는다.
- 이 앱 QA는 **HP10000·진화 sling·backlog 직접 kill·무기 clock 억제로 D3 생존·마지막 timeout**을 사용한다. 실제 D3 Core 이동/예고/돌진·저장 연결의 기술 증거이며 자연 완주나 앱 실제600킬 검증이 아니다. Core의 별도 실제600킬/1653hits CLI와 구분한다. 정확3화면의 UI 품질은 별도 담당자 판정이다.
- sync/provenance/app binding의 Core DLL SHA는 모두 `0ac82dfcf83a6a4dda8557ad70b5a76ee6b8e11449cda2b5d56942aa19e6d6e6`으로 기록된다. `coreSourceUnchangedFromHandoff:true`, 고정 runtime build 입력·734보호 파일 보존 및 기존 project/portable PASS를 재사용하며 전체 해시를 다시 계산하지 않았다.

## 인계

리뷰 중 제품/Core/시험/도구/Unity/아트/DLL/provenance를 편집하지 않았고 본 문서만 소유한다. 시험·전행렬·CLI·hash·Editor·빌드·GUI를 추가 실행하지 않았다. QA 새 identity의 no-Host/no-save probe·realpath·자기 child 종료/저장 absent 복구 근거만 읽었으며 보호 사용자 프로세스·저장은 조회/조작하지 않았다.

자연 밸런스·native 정밀 입력/타이밍·모바일/IL2CPP/기기·성능은 NOT_RUN, R05는 미구현 대기다. usage는 `r04-unity-code-review` start/end 세션 기준점이며 독립 worker 비용/절감률을 추정하지 않는다. 이 Unity 통합의 코드 리뷰 게이트는 PASS이며 별도 UI 판정과 coordinator의 GC/커밋·CI 종료 게이트는 후속이다.
