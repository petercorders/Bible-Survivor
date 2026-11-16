# R06 Core 독립 코드 리뷰

2026-11-16 KST. **PASS — 검토 범위에서 열린 HIGH/MEDIUM 없음.** `docs/harness/r06-implementation-01.md`를 기준으로 R06 관련 Core diff, 저장/보상·R03 호환 경로와 구현자의 최종 근거를 검토했다. 기존 R03~R05 독립 PASS와 전투/저장 근거는 재사용했다.

## 확인 사항

- R05 완료 후 `PreviousNormalRound=5`와 R03 `CompletedBoss`를 유지한 채 R06에 진입한다. 일반전 종료 후 별도 intro/보스 시계로 전환하며 조기 회복·완료 보상을 지급하지 않는다. R06 정의는 HP6500/contact16/주기7초/고정 target 예고1.2초/피해18/반경1.2, 보스 actor781과 F1 지원 HP80/contact8/속도비0.70을 사용한다. pause·intro·선택 단계 정지, 공유 피격 무적·사망 우선·생존 tick 회복 경로를 유지한다.
- 70%/35% threshold를 각각 mask bit로 기록하고 각6, 누적12 지원을 한 번씩 소환한다. 한 타격에서 두 threshold를 넘으면12, 치명타면 소환 없이 처치를 우선한다. 단조 actor ID와 mask/누적 수/다음 ID/HP의 관계를 저장 검증한다. 지원 처치는 보스 고정200gold를 지급하지 않으며 보스 처치는 일반 kill remainder와 별도로 한 번만 처리한다.
- 실제 무기 공격이 threshold를 넘은 같은 tick 끝에서 World를 다시 동기화한다. 신규 지원의 session actor와 좌표가 저장 전에 일치하므로 첫 threshold 직후 Capture/Commit에서 actor 누락이 남지 않는다. 지원 제거·보스 종료 시 World 정리와 공격 중단도 함께 검토했다.
- R06 보상은 고유 encounter/reward ID와 `r06-production-v1`의 동결 후보/인벤토리를 사용한다. 기존 최대3후보·진화 보장·첫3진화 실효과 경로를 재사용한다. 선택 적용과 pending 성장 완료 후에만 완료100gold·cross 해금·최대HP 회복·between7을 적용한다. 완료 flag와 저장 operation retry가 중복 지급을 막고 R07 시작은 거절한다.
- R03 완료 receipt는 `CompletedBoss`, 현재 R06 receipt는 `Boss`에 병존한다. R06 선택/정산이 과거 R03 receipt를 덮지 않으며 잘못된 역사 슬롯·이전 일반 라운드·단계/라운드 조합을 거절한다. 기본값을 쓰지 않는 R03의 새 선택 필드는 DataContract 출력에서 생략된다. R03 정의/정책/기존 fingerprint 문자열은 유지하고 R06만 동적 규칙 계약을 추가한다. R06 World3에는 D3 charge가 허용되지 않는다.
- 새 round/mask 필드를 명시 shape에 포함하고, 지원 범위를 벗어난 encounter round/ID·정책·중첩 필드/버전은 미래 형식으로 보호한다. 알려진 형식의 불가능한 mask나 receipt 조합은 invalid로 구분한다. 기존 미래 원문 보호를 완화하지 않았다.
- Unity는 `AdamSession.BossRules`와 `BossEncounterState.Rules`에서 라운드별 HP/접촉/주기/피해/actor ID/지원/threshold/정책을 읽을 수 있다. 현재 보스의 target·telegraph·elapsed/next attack 및 World 실제 좌표도 노출된다. 공통 예고/반경 등은 공유 상수다. 후속 Unity 통합은 R03 고정 HP1800/actor481/6초를 사용하던 표현 경로를 이 동적 값으로 연결해야 하며, 이 리뷰는 Unity 연결 완료 판정이 아니다.

## 재사용 실행 근거

- `evidence/r06-core-01/summary.json`과 `restored.log`: R06 관련 **7/7 exit0**. 진입/intro, 7초 고정 target 단발 피해, 두 threshold/치명 우선/같은 tick actor 좌표, threshold·예고 disk 복원 후 World 동등, 보상/성장/정산 once·R03 역사 보존·동일 operation retry, 회복/정지/사망 우선, 미래 원문 보호 시험의 코드와 결과를 대조했다.
- 정확한 이전 구조 3변이는 모두 실제 실행 중 실패했다. `old_threshold_shape_without_mask.log`는 mask 저장 유실 후 복원 검증 실패(4/7), `old_single_completed_receipt.log`는 과거 R03 receipt가 R06으로 바뀐 비교 실패(6/7), `old_next_tick_support_sync.log`는 같은 tick actor 좌표 **expected7/actual1** 실패(6/7)다. 복원은7/7이며 컴파일 실패를 검출로 세지 않았다.
- summary의 관련 R03 보스 **12/12**, 보상 **11/11** PASS를 재사용했다. R03 통계/정책/fingerprint와 공용 경로 변화에 관한 보조 근거이며 기존 전체 행렬을 새로 실행했다는 의미는 아니다.
- `cli-result.json`·`boss-series.log`: `--r06-series sling 7` **exit0**, 일반780+지원11 actual kills/보스1 actual kill/4080hits, 보스21.4초, mask3·12소환, threshold 및 후보 disk Load 후 R03 receipt/진화 유지→between7·gold1168을 확인했다. 지원12소환 중 실제11처치이며 보스 처치가 남은 지원을 정리한다.

리뷰어는 위 시험/변이/CLI를 재실행하지 않았고 기존 전체 시험·18/10/30/전행렬·전체 해시·빌드·Unity/GUI도 반복하지 않았다. 새 재현이 필요한 확정 의심이 없어 probe를 추가하지 않았다.

## 한계와 인계

대표 CLI는 장비8/HP10000 등 기술 fixture를 사용한다. 실제 좌표 공격·처치/디스크 연속성의 근거이며 자연 난이도·밸런스 수용 근거는 아니다. Unity Host/표현·DLL/provenance·아트·실사용자 저장·모바일/성능은 이번 리뷰 범위 밖이다. 기존 R03 receipt 한 개와 현재 R06 receipt를 보존하는 구조이며 R07 이후의 다중 보스 역사 확장까지 구현된 것으로 보지 않는다.

제품·시험·도구·Unity·아트는 편집하지 않았고 본 문서만 소유한다. usage는 `r06-core-review` start/end 세션 기준점이며 독립 worker 비용/절감률을 추정하지 않는다. 순수 Core 코드 리뷰 게이트는 PASS다.
