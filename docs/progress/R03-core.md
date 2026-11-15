# R03 첫 보스 순수 Core — slice 01

2026-11-15. 기준 `d8a7802`, 실행 범위 `docs/harness/r03-implementation-01.md`. **순수 Core의 보스 처치→저장 가능한 정식 보상 대기까지 구현했다. R03 전체 완료나 Unity 지원은 아니다.** 독립 코드 리뷰는 coordinator에게 인계하며 아직 PASS로 합산하지 않는다.

R02 대기의 해결된 레벨 선택 뒤 `StartNextRound`로 R03 일반 전투에 진입한다. 실제 catalog의 480 생성/1100 원시 XP/동시60/170초 생성 마감/180초 일반 종료를 재사용한다. 일반 종료는 남은 적을 보상 없이 정리하고 떨어진 XP만 정산한다. HP 회복·완료50골드·moses_staff 해금·다음 라운드 이동은 하지 않는다. 보스는 ID481, 지원은482부터다.

2초 intro는 공격·피해·추적·지원 소환을 막고, 일시정지/성장 선택/로드 중에는 시뮬레이션 시계가 흐르지 않는다. 보스 HP1800, 접촉12, 느린 추적과 6초 주기·1.2초 고정 위치 예고·피해15 slam을 구현했다. 예고 동안 보스는 추적을 멈춘다. 50% 이하 최초 생존 문턱에서 C1 여섯을 한 번 소환하며, 즉사에는 소환하지 않는다. 접촉→지원 ID 순 접촉→slam→생존 후 반사/무기의 순서이며 플레이어 사망이 보상보다 우선한다. 공통 피해 시계는 일반 종료+intro+boss의 단조 시간이다. boss elapsed에는180초 완료 제한을 적용하지 않는다.

보스 처치100골드는 encounter receipt로 한 번 지급하고 일반/지원5킬 remainder에 보스를 더하지 않는다. 남은 지원 정리는 처치가 아니다. 처치 후 `boss_reward`에서 `NeedsRewardImplementation`을 반환한다. 안정 encounter/reward ID·정식 정책 ID `r03-production-pending-v1`·처치 시 장비/보조·진화 자격을 저장한다. **후속 정식 후보 생성/첫3진화/선택 적용/완료 정산은 미구현**이며 기본 보상이나100골드 fallback으로 대체하지 않는다. 보상 대기에서는 장비 선택·전투·완료 정산을 실행하지 않는다.

실행 문서의 검증용 tuning(속도비0.30·반경0.60·intro offset(0,+3)·slam반경1.20·지원반경1.5·지원XP0)을 고정했다. 보스 계약 fingerprint는 실제 사용 상수·원본 패턴/지원 stats·정식 정책 version으로 만들고 R03에만 결합한다. R1/R2 fingerprint 입력/bytes는 유지한다. 이 수치는 플레이 밸런스 확정값이 아니다.

새 `Run.Boss` 명시 shape와 encounter version1, 보스용 World version2를 추가했다. R1/R2는 기존 World1과 serializer 의미를 유지한다. 단계·HP·지원 ID/상한·문턱 flag·예고/impact 시계·target geometry·보스 골드/보상 receipt·단조 시간·World 대상/ledger/속도를 검사한다. 지원하지 않는 Boss version/중첩 필드/정책·World3·stage/round는 미래 콘텐츠로 원문을 보존하며 옛 backup으로 fallback하지 않는다. schema1에 Boss를 넣어 이행하지 않는다. 기존 header-first/원문 archive/revision/operation payload/실패 및 동일 operation retry 경로는 유지한다.

World는 일반 적/보스/지원에 같은 좌표 무기·투사체 경로를 쓰며 보스 반경을 실제 충돌에 사용한다. `BossKills`를 일반/지원 `Kills`와 분리했다. phase 경계에서 과거 impact를 읽기 범위에서 지우되 World sequence는 단조로 유지하고 실제 마지막 bosskill 신규 사건은 남긴다. Restore는 사건을 재생하지 않는다.

## 검증

모든 아래 명령 exit0. 새 `evidence/`는 기존 ignore 정책을 사용하며 공개 원문 세션/사용자 저장을 포함하지 않는다.

- `python3 tools/check_all.py`: `evidence/r02-20261115T112059889137Z/checks.json`. 기존115+보스10=125/125 및 기존 CLI와 새 대표 `--boss-combat sling 7`까지15명령 PASS.
- 보강 후 최종 `bash tools/dotnet.sh run --project tests/BibleSurvivor.Core.Tests -- --results evidence/r03-boss-core-01-final-tests`: 126/126. 상수 기반 fingerprint와 phase 사건 경계 변경 때문에 관련 검증을 재실행했으며 전체 CLI/기존 변이 행렬은 반복하지 않았다.
- `python3 tools/check_boss_mutations.py`: 기존 Finish로 조기 완료, 지원 문턱 flag 유실, 보스 일반킬 remainder 합산, Restore World clock0의4가지 실제 assertion 실패를 검출하고 원복 PASS. 최종 경로는 `evidence/r03-boss-core-01/final-binding.json`에 기록한다.
- `python3 tools/check_mutations.py`: `evidence/r02-mutations-20261115T112129897693Z/summary.json`, 기존18/18.
- `python3 tools/check_schema_mutations.py`: `evidence/r03-schema-mutations-20261115T112129898228Z/summary.json`, 기존10/10.
- `python3 tools/check_schema_compat.py`: `evidence/r03-schema-compat-20261115T112129899516Z/summary.json`, 실제 역사적 binary30개 Run/Profile/World/fingerprint 동등(헤더만1→2),30이행·30옛 reader 미래버전 거절.
- 최종 boss-only11/11, 대표 sling 좌표 CLI는 R03 일반856hits→보스 포함954hits·bosskill1·지원6·보상 대기·예고 중 disk restore PASS. R1/R2 timeout, 무기8, HP10000을 준비한 **격리 기술 fixture**다. R03 피해/처치를 주입하지 않았으며 밸런스 수용으로 쓰지 않는다. 최종 체력9537/골드257/보스12.0167초는 해당 fixture 결과다.
- `python3 -m py_compile`(변경 Python driver)와 `git diff --check`: exit0.

최초 전체 검사에서는 malformed World version을 unknown field보다 먼저 분류하던 기존 우선순위가 바뀐 회귀1개(124/125)를 발견해 원래 순서로 복구했다. 수정 중 `Array` helper 이름 충돌 컴파일 오류도 수정했다. 최초 Finish 변이의 상수false는 경고를 오류로 처리해 빌드 실패했으므로 검출로 세지 않았고 비상수 조건으로 바꾸어 실제 assertion 실패를 확인했다. 실패 이력도 ignored evidence에 보존했다.

UnityGame/DLL/provenance·기존 아트·실제 사용자 저장·Editor/Player GUI·CI hooks·전역GC는 변경하지 않았다. DLL이 이전 Core이므로 portable/provenance 동기화 검사는 이 slice에서 NOT_RUN이며 순수 Core 증거로 Unity 동기화 성공을 주장하지 않는다. Unity/모바일/실플레이·성능·최종 아트·정식 보상/진화·정산/between4는 NOT_RUN 또는 미구현이다. commit/push 및 독립 리뷰는 coordinator 소유다.

## 독립 리뷰 MEDIUM 수정 — 전투 중 영구 재생

`docs/reviews/R03-core-01-code.md`의 보스 regen 누락을 수정했다. normal에서 쓰던 `regen rank ×0.1×실제 진행시간` 계산을 `AdamSession.Regenerate`로 옮기고, 보스도 실제 진행한 생존 tick에서 한 번 호출한다. 양쪽 모두 해당 tick의 적 피해·사망 판정 후, 플레이어 공격 전에 재생하므로 치명타 우선순위를 유지한다. normal 호출은 원래 위치와 진행시간을 유지하며 최대 HP cap을 공유한다. 보호 intro는 전투 재생을 하지 않고, pause·성장 선택·reward 대기는 tick을 만들지 않는다. 보스 처치 전 완료 전량 회복·완료50골드·해금은 추가하지 않았다.

회귀 시험은 유효 `health2/armor1/regen1` 도전에서 normal 1초+0.1HP와 접촉/slam 없는 boss 1초+0.1HP를 비교하고, intro·pause·선택·reward 정지와 cap을 확인한다. 수정 전 실제 시험은 **exit1(11/12), 예상111.1/실제111**로 실패했고 수정 후 `bash tools/dotnet.sh run --project tests/BibleSurvivor.Core.Tests -- --boss-only`는 **exit0(12/12)**다. 격리 사본에서 새 boss regen 호출만 삭제한 정확한 원래 구조 변이1개를 같은 assertion 실패(exit1)로 검출한 뒤 복원12/12(exit0)를 확인했다. 근거는 `evidence/r03-boss-regen-review/probe-before-after.json`, `summary.json`, `regen-call-removed.log`, `restored.log`다.

수정 범위는 AdamSession/BattleWorld·BossTests 및 이 진행/회고 문서뿐이다. 기존126전체/18변이/10schema변이/30호환·CLI 행렬은 다시 실행하지 않았다. 이전 첫 slice 증거는 해당 source 시점의 기록으로 유지한다. 이 수정의 독립 재리뷰는 같은 리뷰어에게 인계하며 아직 PASS로 표시하지 않는다. Unity/DLL/아트/사용자 저장 및 후속 보상·진화 작업은 실행하지 않았다.

## 실행 02 — 정식 보상·첫 3진화·완료 정산

2026-11-16 KST. `r03-implementation-02.md`의 순수 Core checkpoint를 구현했다. **독립 코드 리뷰 대기**이며 Unity/DLL 연결 완료는 아니다.

새 보스 처치는 정식 정책 `r03-production-v2`로 최대3개의 고유 후보를 한 번 생성한다. 자격 있는 첫3진화를 우선 포함하고, 남은 칸에는 실제 구현된 장비의 신규1레벨/유효1단계를 제시한다. 성장 후보가 부족하면 골드100을 한 번만 추가하고 골드만 남으면 자동 적용한다. 전투 RNG·일반 ChoiceSerial은 사용하지 않으며 encounter/reward ID 기반 별도 결정적 선택을 사용한다. 처치 시 장비·보조·form·해금 입력과 후보/선택/금액 receipt를 저장하고 같은 후보를 재개한다. 낡은 inventory/ID·반복 적용은 거절한다.

기존 `pending-v1` 처치 대기는 기존 fingerprint·정책·자격을 그대로 읽고 보존한다. 명시적 `PromoteBossReward()`만 version2/정식 정책으로 한 번 승격한다. 자동 Load/Capture는 보상을 재추첨하거나 정책을 조용히 바꾸지 않는다. 예전 R03 전투 checkpoint의 변경된 fingerprint를 무시하는 일반 우회는 추가하지 않았다.

진화는 기존 무기 ID·8레벨을 보존하고 `Forms`에 base→form lineage를 같은 슬롯으로 기록하며 보조를 유지한다. 성경은 기존 최대 성능의 회전 공백을 없애고2초/반경2.8/수명0.4 파동을 추가한다. 파동당 대상1회 ledger와 orbit/pulse 간 최소0.5초 hit 간격을 저장한다. 턱뼈는 기존2연타/0.15초 burst를 유지하고 area+0.25·실제 명중 넉백1을 적용한다. 보스에도 같은 넉백이 적용된다. 물맷돌은 max(5,최대 기본수량+보조)발,0.1초 간격/관통을 유지하고 volley 시작에 최대HP 내림차순·ID 순으로 목표를 고정한다. 목표 사망 시 같은 기준으로 재탐색하며 focus/잔여 shot/타이머를 복원한다. 기존 Cause는 유지하고 실제 진화 `WeaponForm`을 transient impact에 추가했다. 모든 진화 수치는 기술 초기값이다.

보상 적용 후 `boss_settlement`에서 남은 성장 선택을 처리한다. `CompleteBossSettlement()`는 그 뒤에만 완료50골드·moses_staff 해금·현재 최대HP 회복을 한 번 적용해 between4로 저장한다. 보스100·선택100·일반킬 remainder·완료50 receipt를 구분한다. R04 전투 시작은 계속 거절한다. 새 Boss version2/정식 후보·Forms·진화 clock의 명시 shape와 R03 fingerprint를 연결했고 미래 form/정책/후보 kind/필드는 옛 backup으로 덮어쓰지 않고 원문 보존 거절한다. R1/R2 serializer 확장은 default값을 쓰지 않도록 유지했다.

검증은 source 고정 후 기존 regen 포함127+신규11인 **138/138 Core**를 한 번 실행했다: `bash tools/dotnet.sh run --project tests/BibleSurvivor.Core.Tests -- --results evidence/r03-reward-evolution-02-final-tests`, exit0. 신규 시험은 보상 진화 보장/최대3·fallback·일반 장비1단계·명시 승격/재개·중복 거절·pending 선택/정산·교체 실패/같은 operation 재시도·세 진화 실제 명중/추가 효과와 save replay·미래 콘텐츠 보호다. 첫3진화의 실제 공격은 R04 미구현 경계를 넘지 않는 **명시 R03 geometry fixture**로 시험하며, 정식 보상→진화→between4 흐름과 구분한다.

`python3 tools/check_reward_mutations.py`는 진화 보장 삭제·완료 receipt 무시 중복·Restore volley focus 유실의3개 원래 구조 회귀를 실제 assertion 실패(exit1)로 검출하고 원복PASS(exit0)했다. 근거: `evidence/r03-reward-mutations-20261115T151257009782Z/summary.json`. 전체 기존18변이/10schema변이/30호환·전무기 CLI 행렬은 반복하지 않았다.

대표 `--boss-series sling 7`은 R1/R2 timeout·장비8/보조1·HP10000을 준비한 격리 기술 fixture에서 R03 실제 좌표 전투→예고 중 disk Load→같은 보상 후보 재개→물맷돌 진화→완료 정산→between4를 확인한다. R03 피해/처치는 주입하지 않는다. 최종 골드307·최대HP10000·보스킬1·지원6·completion receipt가 저장되며 밸런스 수용이 아니다. 최종 CLI 명령/exit·source hashes와 결과 결속은 `evidence/r03-reward-evolution-02/final-binding.json`, 원문 로그는 같은 폴더의 `boss-series.log`다. `check_all`의 대표 보스 CLI는 이 연속 경로로 교체했으며 여기서 전체 driver를 다시 실행하지 않았다. `git diff --check`/변경 Python driver compile은 exit0다.

병행 Bible sidecar/Unity/아트 편집을 보존했고 이 작성자는 해당 파일·DLL/provenance·실사용 저장·GUI·CI/hooks·commit/push를 변경하지 않았다. 다음 핵심은 이 checkpoint의 독립 코드 리뷰→필요한 결함 수정→별도 Unity 통합이다. Unity/모바일/실플레이/성능은 NOT_RUN, R04는 미구현 대기다.
