# R09 Core 독립 코드 리뷰

2026-11-17 KST. **PASS — 검토 범위에서 열린 HIGH/MEDIUM 없음.** `docs/harness/r09-implementation-01.md`와 `docs/progress/R09-core.md` 기준 Core 관련 diff·저장/보상·실행 근거를 검토했다. 앞선 R03~R08 Core PASS·성장 고갈25/첫3진화/저장 근거를 재사용했다. 병행 중인 Unity 제품 파일은 검토·편집 범위 밖이다.

## 사슬 전투와 공개 읽기 계약

- R09는 catalog1140/XP4200/cap120·기존 I1/I2/I3 추적/접촉을 소비한다. 일반 종료 후 drop XP만 회수하고 무조기회복·보호 intro2초를 거쳐 HP16000/contact20·actor1141 보스로 전환한다. 60/30% mask·각 I2지원6/누적12·단조 ID·치명 우선/같은 tick 지원 좌표 동기화를 기존 공용 경로로 유지한다.
- 보스 elapsed4초부터1초 예고하며 당시 플레이어 방향과±20도를 고정한다. 겹치면(1,0)이다.5초 주기 만료 시 point-slam 피해 대신 발사 flag를 만들고, 플레이어가 생존하고 보스가 여전히 전투 중일 때 tick 끝의 현재 보스 위치에서3발을 생성한다. 새 탄은 그 tick에 이동하지 않으며 넉백으로 바뀐 현재 보스 위치도 발사 원점에 반영된다.
- 기존 적탄은 이전 플레이어 위치부터 현재 위치까지의 상대 선분으로 swept circle 충돌을 계산한다. 속도1.35/반경.16/TTL6을 사용하고 탄의 남은 수명보다 긴 마지막 step에는 플레이어 이동도 같은 비율로 제한한다. 접촉한 탄은 방어/공유무적으로 피해가 막혀도 제거된다. 실제 피해는 기존 Session.Damage와 `chain` cause를 사용하며 별도 속박/arena clamp는 없다.
- 적탄→접촉 등 incoming damage→생존 확인/회복→반격·플레이어 공격 순서다. 같은 tick 플레이어 사망은 무기/반격 보스 처치·고정골드보다 먼저 종료한다. 보스 처치 후 잔여 적탄은 보상 없이 정리하고 발사 flag도 실행하지 않는다. pause·선택·intro·reward는 적탄 시계를 진행하지 않는다. 정상5초/TTL6 경로에서는 최대6이며 목록 상한12를 검증한다.
- `Session.Boss`의 ChainDirections와 `BattleWorld.HostileProjectiles`는 원본 객체까지 복사하므로 Unity가 방향/위치/수명을 바꾸어 Core를 수정할 수 없다. 예고는 현재 `EnemyPositions[1141]`과 저장된 -20/기본/+20도 방향, ChainSpeed×ChainLifetime=8.1 구간을 읽고 발사 후에는 실제 탄 좌표를 읽으면 된다. BossElapsed/NextAttackAt은 보스 상대시계, BornAt/World.Time은 NormalEndedAt+IntroElapsed+BossElapsed의 절대 전투시계다. 해당 정보와 공통 projection으로 Unity 연결에 필요한 값이 제공된다.

## 저장·호환·보상

- envelope schema2를 유지하고 R09만 Boss3/optional ChainDirections·World4/optional HostileProjectiles/NextHostileId·HostileVersion1을 작성한다. R03/R06 ChainDirections는 null, R01~R08 새 World 필드는 null/0 미출력이다. 기존 R06 point-impact 계약은 같은1.2초 값을 유지하고 R09에만 사슬 상수/방향·예고/지원·정책 계약을 fingerprint에 추가한다.
- 적탄 고유 ID/상한·단위 방향·시작점/현재점·BornAt/남은 수명·일정 속도 이동을 검증한다. R09 발사 lifecycle/5초 주기와 World 시간·actor 목록을 대조하고, 공격 시작 뒤 active 저장에는 World geometry를 요구한다. 다른 라운드의 적탄 필드·R09 D3 charge를 거절한다. Restore는 적탄 목록/다음 ID·RNG·step remainder를 복원하고 예고 방향/시계를 유지한다.
- 명시 nested shape에 새 탄/방향 필드를 포함하고 미래 World5/Boss4/Hostile2·unknown 필드/encounter/정책은 typed body 복구 전에 Unsupported로 보호한다. 알려진 형식의 불가능한 clock/lifecycle은 invalid로 구분한다. 기존 유효 저장 shape/ID 의미와 무쓰기 Load 경로를 유지하며 미래 fixture 경계만 다음 미지원 값으로 이동했다.
- R03 CompletedBoss·R06 CompletedSecondBoss를 보존한 채 현재 R09 Boss receipt를 유지한다. 세 identity/순서/정식 선택·완료를 검증하며 과거 두 receipt를 덮거나 다시 활성화하지 않는다. PreviousNormalRound8·장비/레벨/XP/진화/remainder/구매 snapshot을 유지한다.
- 고정350·보스 선택100·일반 성장25·완료160은 기존 각 receipt/ChoiceSerial/완료 flag 경로로 분리한다. 고정 후보/진화 보장·이미 진화 제외·gold-only100 자동 적용·성장 후 정산을 재사용한다. 완료160/null 해금/현재 최대HP 회복→between10에서 ChapterComplete가 true이며 Resume는 같은 완료 상태다. StartNextRound10은 거절하고 강제 초기화·자동 다음 책 진입을 추가하지 않는다. 명시 새 도전 UI는 후속 Unity 범위다.

## 재사용 실행 근거

- `evidence/r09-core-01/related.log` **20/20 exit0**(R09 6+R06 7+R08 5+schema2), `final-r09.log`·`restored.log` **6/6 exit0**을 확인했다. 신규 시험의 intro/이력, 고정3방향·정지·tick말 발사/속도·TTL/pause, 실제 적탄 피해·무적 소멸·동 tick 치명/kill 정리,60/30 지원/저장, 예고/live 탄 disk 후속 World 동등·상한·미래 원문, 후보/고정골드·완료once/세 receipt·완료 Load를 코드와 대조했다.
- `old_boss_point_slam.log`는 정확한 옛 point-slam 분기에서 **expected3/actual0** assertion을 포함해3/6 exit1이다. `old_restore_without_hostile_ledger.log`는 정확한 옛 적탄 없는 Restore 구조에서 **expected HP85/actual100** 및 live World 동등 비교 실패로4/6 exit1이다. 두 변이는 실제 실행 실패이며 컴파일 실패를 검출로 세지 않았다. 복원6/6이다.
- 마지막 R09 피해 시험과 관련 R08 접촉 기대값은 실제 인벤토리 seal armor를 반영하도록 교정됐다. 방어를 무시하던 시험 기대를 고친 것이며 제품 피해 수치를 약화하지 않았다. 다른 기존 시험/변이 anchor는 지원 상한·미래 경계만 이동했고18+4+10+3 driver 실행은 재사용 주장에 포함하지 않는다.
- **단1회** `--r09-series sling 7` 최종 CLI는 R01~R09 실제 좌표 공격/처치를 연속 진행하고 exit0이다. R01 240킬/R02 360킬, R09 일반+지원1142킬/8607hits·보스kill1/40.8167초·사슬24발·mask3/12소환, 예고/live 적탄/보상 Load·두 history 유지·ChapterComplete/between10·gold3119를 기록한다. gold-only100 자동 선택 receipt도 완료 저장에 있다. kill/피해 주입은 없으며 R09 지원12는 소환 수다.

리뷰어는 관련 시험/변이/CLI·기존 전행렬/전체 해시·Unity/Editor/GUI/build를 재실행하지 않았다. 새 재현이 필요한 실제 의심이 없어 probe도 추가하지 않았다.

## 한계와 인계

대표 CLI는 R01 이전 장비8/대응보조1·HP10000 기술 준비를 사용한다. 실제 좌표 연속성/피해·처치·선택·중간 Load 근거이며 자연 난이도·경제 밸런스·실기기 완주 가능성 PASS가 아니다. R04의 observed_charge=false는 기술 장비로 예고 전 처치한 결과이므로 돌진 전조 검증으로 쓰지 않는다. Unity의 세 방향 비점멸 예고/실제 탄·lowFX·끝 대기/명시 새 도전과 DLL/provenance·아트·실사용 저장·native/mobile/performance는 이번 리뷰 범위 밖이다.

제품·시험·도구·Unity·아트는 편집하지 않았고 본 문서만 소유한다. Claude/추가 에이전트 호출은 없다. usage `r09-core-review` start/end는 세션 기준점이며 stale 시작 event와 개별 worker 비용/effort 미측정을 구분하고 청구비용·절감률을 추정하지 않는다. 순수 Core 코드 리뷰 게이트는 PASS다.
