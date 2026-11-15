# R05 Core 독립 코드 리뷰

2026-11-16 KST. **PASS — 검토 범위에서 열린 HIGH/MEDIUM 없음.** `docs/harness/r05-implementation-01.md`의 R04 완료→R05 추적/접촉 전투→성장 후 정산/between6를 검토했다. 이전 R04 독립 PASS와 기존 전투/저장 근거는 재사용하며 Unity 통합 완료로 확대하지 않는다.

## 확인 사항

- R04 between5의 완료 flag가 유효할 때만 R05에 진입한다. 새 진입 시 현재 전투 시계·World·완료 flag를 초기화하고 `PreviousNormalRound=4`로 이전 R04 완료 사실을 남긴다. R03 `CompletedBoss` 전체·장비/첫3진화·레벨/XP·영구 snapshot·킬 remainder는 같은 run에서 유지한다. 선택 int 필드는 default0 미출력이므로 과거 R01~R04 작성 shape를 바꾸지 않는다. R04 완료/between5는 normalRound4/previous0, R05 active/settlement/between6는 normalRound5/previous4로 구분해 검증한다.
- canonical690/XP1900/동시80/180초·170초 마감과 E1/E2/E3의 HP/접촉/속도 입력을 사용한다. 세 종은 기존 일반 추적·접촉 경로를 재사용하며 E2 강제이동이나 새 돌진/탄막/디버프는 없다. 속도/행동 계약은 R05 fingerprint에만 추가한다.
- 새 World는 R04 charge dictionary를 가져오지 않는다. R05도 schema2/World3를 재사용하되 charge 목록이 비어야 하며 D3 actor/SpawnedAt/charge 누출을 거절한다. 무기/진화 ledger와 RNG·고정 step 잔여시간은 기존 Capture/Restore 경로를 유지한다.
- R04의 deferred settlement를 R05로 확장하여 떨어진 XP만 회수하고 pending 성장 후에만80gold·daniel_lion 해금·최대HP 회복/완료 receipt/between6를 적용한다. 완료 번호를 현재 정의+1로 계산하며 반복 정산을 거절한다. 저장 교체 실패 뒤 동일 payload/operation retry는 이미 적용된 memory 보상을 다시 지급하지 않는다. 미구현 다니엘 사자 장착/공격·R06 시작은 거절한다.
- 새 선택 필드/지원 라운드 상한의 명시 shape와 기존 미래 round/World/필드 원문 보호를 확인했다. 옛 future fixture는 다음 미지원 경계로 이동했고 기존 resume 변이는 지원 상한 anchor만 갱신한다. R04까지의 fingerprint/전투·정산 분기를 다른 규칙으로 바꾸지 않았다.

## 재사용 실행 근거

- `evidence/r05-core-01/summary.json`, `restored.log`는 관련 **5/5 exit0**이다. `RoundFiveTests`의 진입/carry·receipt, 세 종 실제 추적거리/접촉과 플레이어 강제이동 없음, 진화 좌표 명중 및 중간 disk 후속 World 동등, 성장 전 지급 보류/완료 once/교체 실패·동일 retry, charge 누출/잘못된 이전 완료/미래 원문 보호를 대조했다.
- 정확히 이전 **R04만 deferred settlement를 사용하던 구조**로 되돌린 변이는 `old_r04_only_settlement.log`에서 **exit1(4/5), 예상 round_settlement/실제 between_rounds**로 실패한다. 복원은5/5 exit0이다. 컴파일 실패를 검출로 세지 않았으며 불필요한 추가 변이를 만들지 않았다.
- `cli-result.json`: 대표 `--r05-series sling 7` **exit0**, 실제 좌표690킬/2095hits·176.5167초·중간 Load→between6·골드710·HP10000·PreviousNormalRound4/R03 완료 receipt/sling_fivefold를 유지한다. R04 실제600킬과 그 charge state restore도 이어진 결과다. R05 전투 동안 charge 목록0을 확인한다.

리뷰어는 위 시험/변이/CLI를 재실행하지 않았고 기존 전체 시험·18/10/30/전행렬·빌드·전체 해시·GUI도 반복하지 않았다. 실제 의심 재현이 필요한 새 결함을 발견하지 않아 probe를 추가하지 않았다.

## 한계와 인계

대표 CLI는 R1/R2 timeout·장비8/HP10000의 기술 fixture다. R03~R05 피해/처치는 실제 좌표 전투지만 자연 난이도/밸런스·실제 PC 앱 수용은 아니다. Unity/DLL/provenance·아트·실사용 저장·모바일/성능은 NOT_RUN/후속 범위이며 R06은 미구현 대기다.

제품·시험·도구·Unity·아트는 편집하지 않았고 본 문서만 소유한다. usage는 `r05-core-review` start/end 세션 기준점이며 독립 worker 비용/절감률을 추정하지 않는다. 순수 Core 코드 리뷰 게이트는 PASS이고 다음 단일 Unity 통합·관련 검증·독립 코드/UI 리뷰는 별도다.
