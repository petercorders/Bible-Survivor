# R03 노아와 첫 보스 — 순수 Core 실행 계획

2026-11-14. **문서 전용 준비이며 R03 구현/시험/플레이는 NOT_RUN**이다. 현재 R02 Unity 기술 연결은66Edit/26Play·독립 기술 코드/UI 리뷰를 통과한 기하 표식 checkpoint다. 최종 R02 아트·native active 입력·밸런스 수용 PASS를 R03에 이월하지 않는다. 사용자 결제 외 지속 개발·멀티에이전트·라운드별 GC/commit/push 승인과 독립 리뷰를 PC로 제한한 지시를 유지한다.

소유는 이 문서뿐이다. 제품/Core/시험/Editor/앱/저장/아트는 변경하거나 실행하지 않았다. AGENTS·전용 하네스와 `game-design.md`의 진행/보스/보상, `genesis-balance.md`의 전투표/보스/진화, `item-recommendations.md`, catalog와 기존 AdamSession/BattleWorld/Waves/Growth/SaveState/Sim 경계를 읽었다. 기존 도전 설계를 대체하는 새 게임 규칙으로 이 문서의 제안값을 확정하지 않는다.

## 1. 원본 확정값과 미정값

| 확정된 입력 | 값/범위 |
| --- | --- |
| 일반 R03 | genesis_03, 동시60, 예정480, 원시XP1100,180초/생성마감170초 |
| 웨이브 C1/C2/C3 |96/24/0 →112/48/0 →120/60/20 |
| 일반 적 | C1 HP30/접촉6/속도비0.55, C2 50/7/0.50, C3 130/9/0.30 |
| 일반 종료 조건 | 예정 전부 생성·처치 또는180초. 일시적으로 빈 화면/생성 backlog는 완료 아님 |
| 보스 | nephilim_tyrant, HP1800, 접촉12, 고정 처치 골드100 |
| 패턴 | 느린 추적,6초마다1.2초 예고한 곤봉 내려찍기 피해15, 보스 투사체0 |
| 진입/지원 | 보호 intro2초, HP50%에 C1×6 한 번, 보스 지원군 전체 상한12 |
| 완료 | 보스 죽어야 완료. 강제 boss 제한시간 없음. 최종 일반 완료 골드50·moses_staff 해금 |
| 보상 | 최대3택1, 진화 가능하면 최소1칸 보장, 장비1단계/신규1레벨, 부족할 때 골드100 후보1개, 골드만 남으면 자동100 |
| 순서 | 보스 처치→보상→남은 레벨업→현재 최대HP 회복→저장→다음 라운드 대기 |

다음은 원본에 숫자가 없어서 구현 전 root가 고정할 **검증용 제안**이다. catalog/기획 확정값과 별도 boss tuning 계약에 넣고 fingerprint·시험 입력에 포함한다.

- 보스 추적 속도비0.30, 충돌 반경0.60 world unit. 플레이어 기본속도3/반경0.25는 기존 BattleTuning이다. 느린 C3의0.30을 초기 비교 기준으로 삼은 제안이며 원작/성경 수치가 아니다.
- intro 시작에 보스를 플레이어 기준(0,+3) 위치에 고정 배치한다. intro에서 양쪽 피해·플레이어 공격·보스 이동·지원 소환을 막는다. 화면/맵 경계가 없는 현재 Core에서 겹침 없이 시작할 결정적 QA 위치이며 실제 화면 구성은 후속 Unity에서 검수한다.
- 곤봉 바닥 원 반경1.20,1회 판정. 첫 impact는 boss elapsed6초, 예고 시작4.8초, 다음 impact12초/예고10.8초다. 예고 시작의 플레이어 위치를 고정 target으로 저장하고 예고 중 보스 추적은 멈추는 후보를 우선한다. 자동 조준으로 끝까지 따라오거나 같은 원이 지속 피해를 주지 않는다.
- 지원군 소환은 보스 주변 반경1.5의 등간격6위치, C1 기본 stats/속도를 재사용한다. 임의 무한 스폰/추가 강화는 없다. 지원군 XP는 원본에 별도 값이 없으므로 **기술 단계0 제안**이며 일반1100 XP 예산에 섞지 않는다. 이 값이 정식 보스전의 성장 기회를 충분히 제공하는지는 별도 결정/검증이 필요하다.
- 기존 플레이어0.5초 피해 무적을 접촉·slam이 공유한다. 서로 다른 공격이 같은 tick에 들어왔을 때 우선순위는 아래 전투 순서로 고정한다. 피해는 기존 방어·반사 규칙을 적용하며 원본의12/15를 새 실제 최종 피해로 오인하지 않는다.

50% 문턱은1800→900 이하 최초 통과로 저장하고,900에서 반복 히트/로드해도 재소환하지 않는다. 한 방에 보스를 죽이면 지원군을 소환하지 않고 처치를 먼저 확정한다. 이 문턱/치명 우선순위는 첫 보스의 예측 가능한 초기 구현 결정이다.

## 2. 단계와 경계 순서

R1/R2의 현재 normal/완료 경로는 회귀 없이 유지한다. R03에서만 `normal → boss_intro → boss → boss_reward → boss_settlement → between_rounds(number4)`를 사용한다. 실제 값은 문자열 상수/enum 변환 함수를 한 곳에 두고 알려지지 않은 단계는 거절한다.

### normal → intro

1. 기존 WaveDirector의480 생성/원시XP1100/상한60/170마감·180완료를 그대로 사용한다.
2. R03 normal 완료 시 현재 AdamSession.Finish의 일반 라운드 회복/완료 보상/다음 번호 이동을 호출하지 않는다. 남은 일반 적·투사체를 제거하고 **이미 떨어진 경험치만** 한 번 정산한다. timeout 생존 적·미생성 적은 처치/골드/XP로 세지 않는다.
3. intro elapsed0과 normal 종료 시각을 저장한다. normal HP를 그대로 유지하며50골드·100골드·해금·최대HP 회복을 지급하지 않는다. 보석 정산으로 생긴 레벨 선택은 같은 후보를 저장하고, 선택 중 intro clock도 정지한다. 모든 선택 이후2초의 실제 시뮬레이션 보호 시간이 온전히 남는다.
4. intro에서 양쪽 공격을 금지한다.2초 끝 tick 이후 boss 단계로 넘어가며 첫 공격은 정의된 boss clock 기준이다. Pause·후보창·앱 중단/로드의 벽시계는 intro/boss clock에 더하지 않는다.

### boss 전투

현재 BattleWorld.Running은 Wave!=null에 묶여 있어 이를 그대로 boss 상태에 재사용하면 진행할 수 없다. phase별 “시뮬레이션 진행 가능/공격 가능/현재 전투 대상” 조회를 추가하고, Wave는 normal에서만 의미를 갖게 한다. normal Wave를 가짜 보스 웨이브로 만들어180초 제한·생성예산에 보스를 끼워 넣지 않는다.

보스+지원군은 동일한 결정적 충돌/자동무기/투사체 경로의 대상이지만 종류를 구분한다. R03 normal ID1..480, 보스481, 지원482부터의 증가 ID를 제안한다. player contact ID0과 충돌하지 않고 phase를 넘어서 ID를 재사용하지 않는다. boss target는 별도 반경/HP/접촉 피해를 사용하며 support는 일반 C1 값이다.

한 fixed step의 우선순위를 다음과 같이 문서/시험으로 고정한다. R1/R2 normal branch의 기존 실행 순서는 바꾸지 않는다.

1. 입력 정규화·플레이어 이동, 보스/지원 이동·기존 step clock 갱신.
2. 그 tick에 유효한 boss contact→지원군 ID 순 접촉→예고 끝 slam 판정. 기존 공유 무적시간으로 같은 tick 중첩 피해를 막는다. 플레이어 HP0이면 즉시 Run 종료/World clear이며 그 뒤 보스 처치·보상을 적용하지 않는다.
3. 생존한 경우 기존 무기 순서·관통 collision.At/ID 순서로 보스/지원을 타격한다. 반사 피해도 실제 원인으로 기록한다. HP50% 문턱은 살아 있는 보스에서 한 번 소환한다.
4. 보스 HP0이면 boss_reward로 이동하고 전투/남은 지원/적 투사체를 정리한다. 이후 같은 step의 적 공격을 뒤늦게 적용하지 않는다. 아직 보스가 살아 있으면 보석 회수·레벨업 정지를 처리한다.

두 clock을 억지로 재사용하지 않는다. normal 종료 시각+intro elapsed+boss elapsed의 단조 round combat time을 공통 피해 무적·World.Time에 쓰고, attack schedule에는 boss elapsed를 쓴다. normal 직전 LastHitTime≈180을 boss time0에서 빼서 수 분 무적이 되는 회귀를 막는다. R1/R2는 기존 normal elapsed와 동일하여 serializer 결과를 유지한다. boss clock은 유한·비음수 검사만 하며180초를 boss timeout으로 적용하지 않는다.

### boss 처치 → 보상 → 정산

- 보스 고정100골드는 실제 최초 처치 때만 지급하고 `BossGoldApplied`/안정 encounter ID로 기록한다. boss는 “일반/지원5킬당1골드”의 kill remainder에 더하지 않는다. 지원군 처치만 그 remainder에 더하며 kill gold·XP·boss 고정100·일반 완료50·골드 선택100을 각각 구분한다.
- 보스 지원군의 사후 정리는 처치가 아니다. 제거된 생존 지원에 골드/XP를 지급하지 않는다. 이미 떨어진 지원 XP가 정책상 있으면 보스 처치 때 한 번 정산하고 남은 레벨 수로 보존하되 보상보다 먼저 강제 선택하지 않는다.
- `BossReward`는 일반 레벨업 Choices와 분리한다. stable encounter/reward ID·정책 ID·고정 후보·selected ID·applied flag를 저장한다. 후보는 보스 처치 때 한 번만 결정하고 Pause/Reload/Retry가 새 후보를 뽑지 않는다. 정상 보상 적용은 한 번만, 낡은 후보/두 번째 선택은 거절한다.
- 보상을 적용하면 boss_settlement로 이동한다. 남은 일반 PendingLevels/Choices를 해결한 뒤에만50완료 골드·해금·최대HP 회복·between4를 한 번 적용한다. 정산 전후의 receipt/flags와 stage 조합을 검증하고 회복/지급을 화면에서 재수행하지 않는다.
- between4는 완료한 R03 정의로 Resume하고 R04 미구현 대기로 저장한다. 새 도전은 명시 동작으로만 R1을 시작하고 영구 골드/구매/해금은 유지한다. boss_reward/settlement를 재개할 때 보스를 다시 살리거나 일반 전투를 재실행하지 않는다.

## 3. 데이터·World·사건 설계

기존 AdamSession의 Growth/Inventory/금액/선택을 복제하는 별도 게임을 만들지 않는다. BossDefinition/BossEncounterState/RewardState를 작은 별도 Core 파일로 분리하고 AdamSession이 phase 수명을 소유한다. BattleWorld는 이동·충돌·자동공격과 저장 가능한 기하 상태만 소유한다.

| 저장/정의 | 필수 정보 |
| --- | --- |
| BossDefinition | 원본 ID/HP/피해/공격·소환·골드/intro/cap, 별도 명시 tuning·보상 정책 version |
| BossEncounterState | NormalEndedAt,IntroElapsed,BossElapsed,HP/phase,NextAttackAt/telegraph 시작·impact 시각/locked target,ThresholdUsed,지원 ID발급/총소환·alive 상태,금액/정산receipt |
| BattleSnapshot v2 확장 | 보스 위치/방향·support 위치/HP 대응,normal/지원 drop 출처,공격 hit ledger·무기/투사체/난수·잔여 fixed-step,phase/단조 Time |
| RewardState | reward/encounter ID,policy ID,typed 후보(`item`,`evolution`,`gold`),금액/아이템/진화대상,고른ID/applied 상태 |

R03 첫 보스는 projectile_count0이므로 boss projectile 목록이 있으면 비어 있어야 한다. 향후6/9보스용 투사체 타입을 미리 가짜 동작으로 넣지 않는다. 일반 플레이어 투사체·발사/히트 타이머·반사/관통 ledger는 phase 전환에서 명시적으로 정리하고 boss 중 checkpoint에서는 정확히 복원한다. 재소환·예고 재추첨·공격 clock 리셋을 Restore가 수행하지 않는다.

Boss HP·target radius·공격 주기 등은 Encounter fingerprint에 포함한다. R1/R2 RoundDefinition.Fingerprint의 기존 입력/정렬/해시 bytes는 바꾸지 않는다. R03에만 boss/rules/tuning/reward version을 결합한 새 fingerprint를 사용한다. 보스 정의·support speed가 바뀌었는데 정상 옛 snapshot으로 오인하지 않도록 포함 범위를 명시한다.

BattleImpact의 ring256·world별 sequence·overflow 정책은 유지한다. boss/지원 명중도 실제 피해와 killed를 발행하고 intro/무적에서 피해0 사건을 발행하지 않는다. 보스킬은 별도 BossKills 통계를 추가하고 일반/지원 kill 통계와 경제를 분리한다. phase 전환·Restore에서 과거 사건은 비어 있으며 rewind 음향/파티클을 재생하지 않는다. 보스 이동/telegraph는 checkpoint의 사실이며 표현 난수로 전투를 바꾸지 않는다.

P0/P1은 transient event를 읽는 소비자다. 기존 세 무기 Cause는 유지하고 진화 구현 시 추가 WeaponForm 필드로 실제 form을 구분하는 후보를 둔다(예: Cause=bible,WeaponForm=bible_sanctuary). 기존 normal 전투의 이벤트 생성 순서/피해/게임 RNG는 변하지 않아야 한다. Unity는 R03의 새 stage/boss 표현을 후속으로 연결할 때만 업그레이드하며 지금 P0 파티클이 보스를 지원한다고 주장하지 않는다.

## 4. schema2·이전 저장·재시도

boss 상태를 schema1의 선택 필드만으로 추가하지 않는다. 구앱이 이를 기존 normal/between으로 읽거나 필드를 무시한 채 다시 쓰는 것을 막기 위해 **새 작성은 SchemaVersion2**를 사용한다. schema3 이상은 미래 버전으로 거절한다. schema1의 R1/R2/between3는 기존 v1 검증을 먼저 통과한 뒤 memory에서 v2로 이행한다. schema1에 boss field/unknown stage가 있으면 유효 boss 저장으로 승격하지 않는다.

1. Load는 header-only version probe를 typed Run DTO 읽기보다 먼저 한다. valid JSON의 미래 version+새 Run 형식이 있어도 typed deserialization 오류로 backup을 읽는 우회가 없어야 한다. probe는 기존 DataContract/stdlib 범위로 구현하며 외부 JSON 패키지를 Core에 추가하지 않는다. 중복/누락/형식이 잘못된 version 처리도 명시 시험으로 고정한다.
2. 미래 버전 primary는 옛 정상 backup이 있어도 fallback/덮어쓰기를 금지한다. 알려진 schema의 실제 손상만 기존 backup 복구 정책을 따른다. `schema3 + incompatible Run + schema1 backup`을 반드시 실패 입력으로 둔다.
3. v1의 누락 fingerprint는 normalR1/between2만 허용한 기존 제한을 보존한다. R2 normal/between3의 기존 fingerprint를 재계산해 조용히 고치지 않는다. Resume/Capture가 기존 규칙대로 보강한다. R1/R2 normal 실행의 의미/보상은 그대로이며 새 writer version만2다.
4. Load 자체는 원본을 쓰지 않는다. 최초 v2 commit 전에 기존 primary/backup 원문을 유효성/해시와 함께 별도 `.pre-v2` 사본으로 **처음 한 번 exclusive create**해 보존하는 안을 권고한다. 보존 실패 시 upgrade commit도 실패시켜 원본을 유지한다. 기존 `.bak`은 평상시 원자적 회복에 계속 사용하고 migration archive로 덮어쓰지 않는다. 실제 사용자 저장이 아닌 격리 fixture에서 먼저 검증한다.
5. v2 DTO Validate는 phase별 조합을 검사한다. normal은 Wave/시간/대상 정합, intro는 elapsed0..2/보스 무피해·지원0, boss는 양수HP/지원≤12/ID·소환threshold·공격시각·geometry 정합, reward는 bossHP0/전투대상0/동일 후보·applied false, settlement는 reward applied·잔여 선택 정합, between4는최대HP/잔여drop0/완료receipt를 요구한다. NaN/Infinity·음수time·과거/미래 target/ID·누락actor·잘못된 후보/중복지급 flag 조합을 거절한다.
6. SaveStore의 revision/operationID/임시기록 검증→Flush→교체/backup·같은 operation payload 검사는 유지한다. 보상 적용·성장 선택·phase 전환마다 host가 고정 payload와 operation을 보유하고 실패하면 전투/새 선택을 막는다. 결과 불명이면 같은 operation으로 retry하고 Reload를 고르면 마지막 disk state로 돌아간다. memory 보상 적용 자체를 다시 실행하는 retry를 만들지 않는다.

schema2 파일은 구schema1 앱이 미래 버전으로 거절한다는 실제 양방향 codec 시험을 둔다. 새 앱의 migration이 과거 앱의 완전한 호환 재개를 보장하는 것은 아니다. 기존 사용자 iOS 실행본을 재설치하거나 그 저장을 바꾸지 않는다. Core DLL sync가 필요한 시점에는 새 codec을 포함한 원본 DLL/provenance를 기존 도구로 갱신하되, Unity 어댑터는 R03 checkpoint를 아직 “미지원 콘텐츠”로 거절해야 한다. 그 거절을 R03 Unity 지원으로 보고하지 않는다.

## 5. 보상을 단계로 나누되 정식 규칙을 삭제하지 않기

### A. 작은 Core 기술 보상

기존 실제 구현된 세 기본 무기/10보조의 유효1단계 또는 신규1레벨, 골드100 fallback을 먼저 만든다. 슬롯/최대레벨/중복 후보·동일 reward 재실행을 시험한다. 이때 `r03-technical-basic-v1` 정책을 **명시 CLI/개발 fixture에서만** 선택하고 저장에 정책 ID를 남긴다. 정식 게임에 같은 것으로 노출하지 않는다.

정식 정책에서 진화 자격이 생겼는데 해당 진화가 아직 구현되지 않았다면 자격/보스 처치·보상 대기를 보존하고 `NeedsEvolutionImplementation`을 반환한다. 기본 보상/100골드로 조용히 치환하거나 가능한 진화1칸 보장 규칙을 삭제하지 않는다. 이는 초기 기술 slice의 범위 표시이며 다른 개발을 멈추는 이유가 아니다. 유효 진화가 없는 경우에도 정식 R03 전체 완성 여부와 기술 단계 통과를 구분한다.

후보는 inventory와 정책/RewardSerial의 고정 정렬·별도 결정적 선택기로 한 번 생성한다. 전투 RNG는 소비하지 않는다. 이미 있는 slot·최대렙이 바뀐 낡은 후보는 재조회로 거절하며 선택 후 목록을 새로 뽑지 않는다. 레벨업 ChoiceSerial을 보스 추첨 때문에 증가시키지 않는다. 장비 후보가0개면 gold100 자동 적용,1~2개면 gold100을 한 번만 추가해 최대3개만 보인다.

### B. 정식 첫3무기 진화 지원

정식 R03 완료 조건에는 최초3진화를 이어서 구현하는 하위 작업을 포함한다. 나머지7무기의 공격/진화가 완료됐다고 표시하지 않는다.

- 자격은 해당 기본 무기8레벨·대응 보조≥1·미진화·해당 boss reward다. Weapon8만 있거나 passive만 있는 경우는 제외한다. 가능한 진화가 여러 개면 최소1개를 보장하면서 후보 전체를 최대3개로 유지한다.
- 진화는 기본 form을 같은 슬롯에서 교체하고 보조는 유지한다. 제안 안정 form ID는 bible_sanctuary/jawbone_thousand/sling_fivefold다. 기본 무기가 후보로 재등장하거나 같은 form을 다시 진화시키지 못하게 inventory/Restore/후보 생성을 함께 바꾼다. schema2는 form과 base lineage를 검증하고 진화 후의 임의 추가8단계는 만들지 않는다.
- 원본은 **최대 강화한 기본 수량/피해/범위·보조를 유지한 뒤 추가 효과**를 적용한다. 추가 말씀 파동 등의 기본 피해는 현재1회 타격 피해이며 새 추가 효과의 타격 간격은최소0.5초다. base8 수치/보너스를 다시 더하는 이중 강화는 금지한다.

| form | 원본에서 정한 변화 | 미정 수치와 초기 검증 제안 |
| --- | --- | --- |
| 말씀의 성역 | 회전 공백 제거+말씀 파동, 기존 최대 성경 능력 유지 | 대기0/연속 orbit, 별도 파동2초 주기·반경기본1.4→2.8/수명0.4초·각 대상1회 후보. 파동 피해는 현재성경1회값, 최소0.5 hit interval 지킴 |
| 천인의 힘 | 넓은 연속 휘두르기+강한 밀쳐내기 | 기존2연타/0.15초 burst 유지, base area에0.25 가산 후보, 적1world unit 밀침 후보. boss만 별도 무적/면역을 임의 추가하지 않고 실제 push/경계·저장 정합 검증 |
| 다섯 돌의 연격 | 연속 돌+강한 적 집중, 기존 능력 유지 | burst=max(5,기존8렙수량+보조수량) 후보로 수량을 줄이지 않음.0.1초 기존간격/관통 유지, 한 volley의 강한 목표 우선·사망시 재탐색 규칙/ID tie-break를 별도 고정 |

이 표 오른쪽은 확정 밸런스가 아니라 root가 다음 구현 전에 선택·고정할 후보다. 특히 수량 보조로6발이 되는 경우 “다섯”이라는 이름 때문에 기존 수량을5로 낮추지 않는다. 지속 공백0·새 파동 hit ledger·실제 넉백·집중 target 수명/Restore·무기 교체 중발사·lowFX on/off 전투 동등을 각각 검사한다. 원본 수치가 없는 진화 반경/추가주기·밀침량을 추정 완성값으로 발표하지 않는다.

정식3진화의 후보/적용/공격·저장 검증이 끝나야 production reward policy를 활성화하고 R03 Core 정식 완료로 분류한다. 기술 보상 정책으로 저장된 미완료 세션을 정식 정책으로 자동 재추첨하지 않고 versioned migration/명시 재개 경계를 마련한다. 미구현7무기 해금 ID는 기록할 수 있지만 공격/시작/진화 후보에서는 제외한다.

## 6. 파일 소유와 실행 순서

| 작업 | 소유/변경 후보 |
| --- | --- |
| schema/encounter 정의 | Core 단일 작성자: SaveState, 신규 BossDefinition/BossState/RewardState·codec/migration 파일 |
| 상태/전투 | 같은 작성자: AdamSession/BattleWorld/Waves의 R03 분기·Growth/Combat의 별도 진화 하위 작업 |
| 데이터 | content/genesis/catalog.json 원본 수치 유지. 미정 tuning/version은 별도 명시 계약으로 추가 후 content 검사 |
| 순수 실행 | CatalogReader·신규 BossRunner/SeriesRunner·의미 있는 Core tests, check_all/check_mutations의 실제 새 명령 |
| Unity/provenance | 순수 Core 단계 후 기존 sync 도구로 DLL/기록 갱신. 어댑터 R03/아트/씬은 후속 별도 계획·단일 작성자 |
| 독립 리뷰/통합 | 다른 PC 코드 리뷰어가 소스/변이/저장 검토, coordinator가 진행·R03 GC·선별commit/push/해당 SHA CI |

순서: v1 baseline/고정 replay 확보 → schema2 codec/이전저장/거절 → R03 normal→intro와무회복 → boss/지원/공격/clock→처치 → 기술보상/정산저장 → 첫3진화 정식 하위 작업 → production reward/연속CLI → 독립 리뷰/수정/같은 담당 재검토/GC/CI. 독립 데이터/아트 브리프는 병렬 준비할 수 있지만 Core·동일 시험·DLL sync의 두 작성자를 만들지 않는다.

## 7. 수용 시험·원래 구조 변이

- 이전 R1/R2 Core89와18변이를 새 실제 실행으로 유지한다. 같은 seed/input의 R1/R2 World/Run/Profile 및 canonical fingerprint를 baseline과 비교한다. 새 schema2 header 차이는 명시하고 JSON 전체bytes 동등이라고 거짓 보고하지 않는다.
- R2완료 pending 선택→R3진입/carry/새World, R3 normal480/XP1100/60상한·timeout와전멸, normal 종료 무회복/무완료지급·보석만정산을 검사한다.
- intro120 fixed ticks의 피해0·공격0, 선택/정지/중단/로드 전후 남은시간 동일. boss에서180초를 넘겨도 자동완료/회복하지 않고 HP0/문턱/C1×6/한번/상한12를 검사한다.
- 예고 시작·locked target·impact1회·6초 일정·접촉/area 공통 iframe, 실제 플레이어 사망 우선과 마지막 bosskill의정확event/골드구분·지원 사후 제거 무보상을 검사한다.
- reward 최대3고유·자격보장·slot/max레벨·골드fallback·같은 후보 재개·중복선택거절, 선택/정산/회복/50골드/해금/다음대기 한 번. 기술basic와production진화필수경계를 분리한다.
- 각 stage/timer/예고 중/50%직전후/보상선택 전후/남은level/최종commit 전후 저장으로 같은 후속 입력의 World/Run/Profile이 이어지는지 검사한다. 부분교체·권한/실패·결과불명 retry·backup/미래schema·migration archive 실패·null/NaN/중복ID/불일치flags를 포함한다.
- 원래 구조 변이는 기존 Finish로돌려intro전회복/완료지급, boss도180timeout, 지원thresholdflag누락, bosskill을5킬gold에포함, reward재추첨/중복적용, retry새operation, 구schema1그대로boss쓰기/미래Runtyped오류에서backupfallback, timer/target복원누락, 기본form재획득·evo보장삭제를 별도사본에서 재현한다. 실제 assertion FAIL→복원PASS가 필요하며 컴파일 실패는 검출로 세지 않는다.
- CLI는 injected kill 규칙 시험과 좌표 실제명중·boss/pattern/지원 전투를 별도 모드로 표시한다. 예시 `--series-boss-combat <weapon> <seed>`/`--boss-rules`는 구현 후 확정하며 아직 존재하는 명령으로 보고하지 않는다. 기본 bot 생존/처치 숫자를 밸런스 수용으로 쓰지 않는다.

실제 명령/exit/새 XML·serializer replay·단계별 저장·최종 source/DLL/content hash와 변이 근거를 ignored 새 R03 evidence에 남긴다. source/검증이 통과한 단계만 독립 코드 리뷰→수정→R03 프로젝트 GC→commit/push/해당 SHA CI로 마무리한다. 전역 GC entries/index/overview는 수정하지 않는다. 순수 Core PASS는 Unity/모바일/최종 보스 아트·실플레이 PASS가 아니다.

## 8. root에 남기는 결정과 다음 연결

### 실행 배정: schema slice

root는 기존 지속 승인 안에서 §4의 schema2/이전 저장 보존을 첫 구현 slice로 배정했다. 새 writer2·v1 읽기·header-first 미래 버전 거절·미래 primary의 backup fallback 금지·최초 exclusive `.pre-v2` 보존 및 보존 실패 시 commit 중단을 구현한다. boss/전투/진화 수치 후보는 이 slice에서 제품에 넣지 않는다. Core와 순수 시험만 단일 작성자가 변경하고 Unity DLL/provenance sync·앱 연결은 독립 리뷰와 작성자 인계 후 진행한다. 현재 앱은 기존 DLL을 유지하며 사용자 iOS와 실제 저장을 건드리지 않는다. 이 배정은 구현/검증 완료 판정이 아니다.

현재 사용자에게 새 질문을 해야 할 정보는 없다. root가 기존 권한 안에서 **미정 tuning 후보·지원 XP0 기술 정책·schema2 archive 방식·정식3진화 수치 후보**를 구현 packet에 고정하면 된다. 충돌하는 원본값은 바꾸지 않았고 미정값은 제안으로 표시했다. 첫basic slice에서 진화가 막혀도 schema/상태/공격/저장·3진화 후속 개발을 계속한다. 보류를 전체 개발 중단이나 완료로 바꾸지 않는다.

R03 Unity 연결은 normal/intro/boss/reward/settlement UI와 새 entity/telegraph·보스/지원 아트·입자/오디오 family/form·정밀 boss hit radius 표시를 별도 계획으로 받아야 한다. Noah/네피림/홍수는 성경 인용과 창작 보스 표현을 구분하고 홍수 자체를 사탄 공격/환경 피해로 만들지 않는다. 실제 디자인 요청은 지정 Claude Design 제품 연결 여부를 확인하고 stable ID/몸체·예고·공격·사망/지원 clip 계약으로 인계한다. R02 외부 아트 검사기 수정 loop와 원본03/font를 이 Core 준비가 수정하지 않는다.
