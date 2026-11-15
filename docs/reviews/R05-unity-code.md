# R05 Unity 독립 코드 리뷰

2026-11-16 KST. **PASS — 검토 범위에서 열린 HIGH/MEDIUM 없음.** `docs/harness/r05-implementation-01.md`와 `evidence/r05-unity-integration-20261116/handoff.md` 기준 Unity 변경을 검토했다. 본인의 R05 Core PASS·관련5/정확한 옛 R04-only 정산 변이1/대표 실제690킬 CLI는 재사용하며 제품 Core를 다시 리뷰하거나 실행하지 않았다. 실제 화면 품질은 별도 UI 판정 범위다.

## 확인한 연결

- Host/catalog는 R04 between5를 R05 진입 대기로 연결하고 R05 normal/round_settlement·완료 between6를 해당 정의로 재개한다. `PreviousNormalRound=4`, R03 완료/보상 receipt, 장비·첫3진화를 유지한다. R04 최종 상태를 새 도전으로 덮는 대신 명시 다음 라운드로 연결하며 최종 새 도전 경계는 between6다. R06 및 미구현 daniel_lion 장착/공격은 노출하지 않는다.
- 성장·정산/80gold·해금·현재 최대HP 회복은 독립 검토된 Core 함수와 기존 Host Checkpoint 경로를 사용한다. pending 성장을 해결하기 전에 지급하지 않는다. 실패한 commit의 Error gate가 새 tick/선택/정산을 막고 같은 State/revision/operation으로 retry하므로 memory에 이미 적용한 완료를 다시 지급하지 않는다. 새 추첨/지급 알고리즘은 없다.
- 새 R05 World와 재개된 snapshot에서 D3 charge가 비어 있는 경로를 확인했다. E1/E2/E3는 각각 승인 move clip과 기존 shadow로 매핑하고 같은 Core target/좌표/추적·접촉 경로를 그린다. E2 팔/연기 표현에 새 강제이동·돌진·상태효과를 붙이지 않았다.
- ArtR05 loader/importer는 pinned index/manifest와3PNG/각4프레임을 검사한다. E1/E2/E3는8/6/12fps, PPU32/pivot16,1·point/clamp/밉맵 없음과 안정 sprite registration을 사용한다. 알 수 없는 actor는 임의 기존 아트로 fallback하지 않는다. 기존 Art03/R02/R03/R04/Bible/font 연결은 유지한다.

## 재사용 검증 근거

`evidence/r05-unity-integration-20261116/`의 실제 XML/실행 결과·QA helper·연속 trace·binding을 대조했다.

- related.xml **21 passed**, 최종 EditMode **106 passed**, PlayMode **31 passed**, 각각 failed/inconclusive/skipped0이다. 관련/최종 engine 명령 exit0, 실제 compile·단일 mac Development build exit0, 기존 local project/portable/diff 검사 exit0을 재사용한다.
- `AdamR05HostTests`는 실제 R04 D3 fixture를 끝낸 뒤 R05 진입·previous4/보스 receipt/진화 유지·charge0과 exact World 재개, **비어 있지 않은 pending 성장의 지급·회복 보류/선택 후 완료**, between6 Run/Profile 동등·미지원 콘텐츠 거절, 완료 저장 실패 후 payload 동결/동일 retry·중복 거절,3종 clip/frame/fps/shadow를 확인한다. R04 관련 시험은 완료 상태를 AwaitingR05로 바꾸면서 기존65gold/receipt 검증을 유지한다.
- 앱 trace는 이전 R04 앱 완료 JSON을 새 QA identity에 seed한 뒤 R05 진입·3종 실제1초 추적/접촉·charge0·pause/midWorld exact disk restore·timeout 완료/80gold·해금·회복/between6를 기록한다. 새 Player 프로세스는 expected Run/Profile 정확 비교와 중복 완료 거절을 통과한다. ordinary quit의 revision/operation 변화는 primary 전체bytes 동등 주장에 포함하지 않는다.
- **앱 완료 시 pending 성장은0**이다. 앱에 실제 성장 선택 증거가 있다고 확대하지 않으며 그 경로는 위 Host 시험으로 한정한다. 앱은 HP10000·기존 진화·backlog 직접 kill·3종 각4명 위치배치·무기 clock 억제·timeout의 기술 fixture다. trace 골드475→556은 정산 전 직접5킬의 remainder1과 완료80이며 helper는 그1 이후의 기준에서 정확+80을 assert한다. 앱의 자연690킬/완주 주장이 아니며 실제690킬/2095hits는 별도 Core CLI 근거다.
- sync/provenance/app embedded DLL의 기록 SHA는 모두 `2d5db5b58e6bbb12722753681ecd19f931d23696a3938d9c9bd567fa6c036bf4`다. binding의 고정 runtime 입력·`coreSourceUnchangedFromHandoff:true`·771보호 파일 보존 및 기존 project/portable PASS를 재사용한다. 전체 해시를 다시 계산하지 않았다.

## 한계와 인계

리뷰 중 제품·시험·도구·Core·Unity·아트·DLL/provenance를 편집하지 않았고 본 문서만 소유한다. 새 시험·전행렬·CLI·hash·Editor·GUI·빌드를 실행하지 않았다. QA 새 identity의 no-Host/no-save probe·자기 child exit0/자기 저장 absent 복구 근거만 읽었으며 보호 사용자 프로세스·저장은 조회/조작하지 않았다.

E3가 빠르게 플레이어 주변에 겹치는 대표 still만으로 가독성을 보장하지 않는다. 정확3화면의 독립 UI 판정은 별도다. 자연 밸런스·native 입력/타이밍·모바일/IL2CPP/기기·성능은 NOT_RUN이며 R06은 미구현 대기다. usage는 `r05-unity-code-review` start/end 세션 기준점이고 worker 비용/절감률은 추정하지 않는다.

이 Unity 통합의 독립 코드 리뷰 게이트는 PASS이며 UI 판정과 coordinator의 GC/커밋·CI 종료 게이트는 후속이다.
