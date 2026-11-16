# R07 Core 독립 코드 리뷰

2026-11-16 KST. **PASS — 검토 범위에서 열린 HIGH/MEDIUM 없음.** `docs/harness/r07-implementation-01.md` 기준 R07 관련 Core/저장 diff와 기존 실행 근거를 검토했다. 이전 R06 독립 PASS와 기존 전투/보상·진화 검증은 재사용했다.

## 확인 사항

- R06 완료/between7에서 pending 성장이 없어야 R07에 진입한다. R03 receipt는 `CompletedBoss`에 유지하고 현재 R06 `Boss`는 optional `CompletedSecondBoss`로 복사한 뒤 비운다. 새 일반전의 시계/World/현재 완료 flag를 초기화하되 PreviousNormalRound5·장비/첫3진화·성장/킬 remainder/영구 snapshot을 유지한다. 다음 라운드 상한은7이며 R08은 거절한다.
- G3의 actor ID는 기존 생성표의1부터 시작하는 schedule index다. 동일 species의 누적 순번을 그 표에서 계산하므로 사망한 actor도 순서에 포함되고 살아 있는 수·dictionary 순서에 따라 좌우가 바뀌지 않는다. 홀수 왼쪽/짝수 오른쪽으로 기존 가장자리 좌표식을 선택하며 기존 `Next(4)`와 `NextUnit()` 두 draw를 먼저 소비한다. 새 RNG/counter를 만들지 않고 기존 가로거리/세로분포와 다른 종·과거 라운드의 난수 소비를 유지한다. Restore는 같은 정의의 생성표와 저장된 cursor/좌표/RNG를 재사용한다.
- G1/G2/G3는 catalog HP/접촉/속도를 사용하는 기존 추적 전투다. G3 각각의 HP·피해·처치/drop은 actor별이며 공유HP·무적 위장·새 상태효과를 추가하지 않는다. R07 속도와 좌우 순서/독립HP 계약만 fingerprint에 추가하여 R06까지의 fingerprint 입력을 바꾸지 않는다. R07 World3에 D3 charge/SpawnedAt과 현재 boss 상태가 유출되지 않는다.
- R07도 기존 deferred 일반 정산을 사용한다. 종료 시 drop XP만 회수하고 pending 성장을 해결한 뒤120gold·elijah_chariot 영구해금·현재 최대HP 회복·완료 flag/between8을 한 번 적용한다. 성장 전 조기 지급·회복과 반복 정산을 거절한다. 같은 저장 operation retry는 이미 적용한 보상을 다시 실행하지 않는다. 해금만 추가하며 불수레 장착/공격은 미구현 거절을 유지한다.
- R07 normal/settlement/between8은 두 역사 슬롯의 존재·라운드 identity·정식 선택/완료 receipt를 각각 검증한다. R03/R06 슬롯 교체나 누락을 거절하고 R06까지는 새 두 번째 역사 슬롯을 허용하지 않는다. `CompletedSecondBoss`는 optional 미출력이어서 과거 R06 저장의 shape를 덮어쓰지 않으며 Load만으로 저장을 재작성하지 않는다.
- 명시 shape와 nested 미래 policy/encounter/필드 검사가 새 역사 슬롯까지 재귀 적용된다. normal8/between9 등 다음 미지원 경계와 모르는 역사 필드·정책은 primary/backup 원문을 보호한다. 알려진 형식의 잘못된 역사 순서는 invalid로 거절한다. 기존 resume 변이 anchor와 미래 fixture는 지원 상한만7/다음 경계로 이동했고 기존 변이 driver를 새로 실행하지 않았다.

## 재사용 근거

- `evidence/r07-core-01/related.log`와 `summary.json`: 관련 **27/27 exit0**(R07 신규3·R06 기존7·schema/round 관련17). `RoundSevenTests`의 과거 R06 무쓰기 Load·두 receipt/진화 carry, G3 좌우와 독립HP·pause·disk 후속 World 동등, 성장 전 정산 차단/120gold·해금·회복 once·동일 operation retry·미래 원문 보호를 코드와 대조했다.
- `old_generic_random_g3_edges.log`는 정확히 기존 임의 가장자리 배치로 되돌렸을 때 G3 좌우 assertion 실패 **2/3 exit1**이다. `old_single_completed_boss_overwrite.log`는 기존 단일 `CompletedBoss` 덮어쓰기로 되돌렸을 때 R07 진입의 역사 순서 검증에서 **0/3 exit1**이다. 둘 다 실제 실행 실패이며 컴파일 실패를 검출로 세지 않았다. `restored.log`는 **3/3 exit0**이다.
- `cli-result.json`: 대표 `--r07-series sling 7` **exit0**, R07 **899 actual kills/5220hits·180초 timeout**→between8·gold1468·HP10000이다. G3 중간 disk Load와 두 보스 receipt 불변·PreviousNormalRound5·sling_fivefold 보존을 확인한다. R03~R06 실좌표 전투/저장 연속성은 같은 runner의 선행 근거다. **900킬 완주 근거로 세지 않는다.**

리뷰어는 관련 시험·변이·CLI와 기존 전행렬/18·10·30/전체 해시·Unity·GUI·build를 재실행하지 않았다. 좁은 재현이 필요한 실제 의심을 발견하지 않아 새 probe도 추가하지 않았다.

## 한계와 인계

대표 CLI는 R1/R2 timeout·장비8/HP10000의 기술 fixture다. R03 이후 피해/처치는 실제 좌표 전투지만 자연 난이도·밸런스나 앱900킬 완주를 검증한 것은 아니다. Unity는 G3를 각각 독립 actor로 실제 `EnemyPositions`/`World.Enemies`에 투영하고 R07 normal/round_settlement/between8을 연결해야 한다. Unity/DLL/provenance·아트·실사용 저장·native/mobile/device/performance는 이번 리뷰 범위 밖이다.

제품·시험·도구·Unity·아트는 편집하지 않았고 본 문서만 소유한다. usage `r07-core-review` start/end는 세션 기준점이며 stale 시작 event와 개별 worker 비용/effort 미측정을 구분하고 비용·절감률을 추정하지 않는다. 순수 Core 코드 리뷰 게이트는 PASS다.
