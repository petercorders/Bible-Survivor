# R06 Unity 독립 코드 리뷰

2026-11-16 KST. **PASS — 검토 범위에서 열린 HIGH/MEDIUM 없음.** `docs/harness/r06-implementation-01.md`와 `evidence/r06-unity-integration-20261116/handoff.md` 기준 Unity Runtime/Editor/관련 시험·sidecar 연결 diff를 검토했다. 본인의 `R06-core-code.md` PASS, Core7/7·정확3변이·관련 R03 보스12/보상11·실좌표 R06 CLI 근거는 재사용했으며 Core 제품을 다시 리뷰하거나 실행하지 않았다.

## 연결 확인

- catalog는 R06 일반전과 두 번째 보스를 명시 지원한다. R03/R06 각각의 `BossDefinition.ForRound`로 HP/접촉/고정골드/공격주기·피해/지원 threshold·종류·수·수치를 대조하고 R07은 거절한다. checkpoint는 R06 normal/intro/boss/reward/settlement 및 between7을 현재 정의6으로 복원한다.
- Host는 R05 완료/between6을 `AwaitingR06`으로 넘기고, R06 완료/between7만 `ContentComplete`로 처리한다. 기존 완료 checkpoint·성장 대기 gate와 진입 저장을 재사용한다. 선택/정산/진입 commit 실패 시 Error로 입력·진행·다른 지급을 차단하고 같은 state/revision/operation을 RetrySave에 유지한다. Unity가 골드·해금·회복·receipt를 별도로 재계산하지 않는다.
- HP bar/수치/보스 이름/phase와 actor 발 위치 탐색은 동적 `BossRules`/`Boss.Rules`를 사용하여 R06 HP6500/actor781/7초에 대응한다. 예고는 저장된 `BossElapsed`, `NextAttackAt`과 공통1.2초에서3프레임을 계산하며 실제 Core target/반경1.2를 World projection으로 그린다. 경고 고리는 모든 actor/effect 뒤, HUD 앞에 그리며 선택 FX/피드백 끄기에 종속되지 않는다. 장식이 새 피해·충돌·투사체를 만들지 않는다.
- F1/F2/F3와 싯나 보스의 move/windup/impact/death를 R06 loader로 명시 매핑한다. 기존 R03 clip은 유지한다. 사망은4프레임/6fps의 짧은 presentation 이후 draw를 종료하므로 마지막 우물이 영구 지형으로 남지 않는다. 보스 종료 후 Core 시간과 별도로 제한된 presentation 시계만 진행하고 pause/suspend는 이를 멈춘다.
- R06 loader는 고정 index/manifest 계약과7개 clip·프레임 수/이름·정확한 rect/pivot/PPU32·Point/Clamp/비mipmap을 검증한다. importer는 PNG whitelist/hash,32/64 격자·발 pivot16,1/32,1·무압축·FullRect·고정 sprite ID를 적용하며 build 전7장과 loader를 다시 검증한다. index는 총25프레임이며 미등록 actor가 다른 아트로 조용히 대체되지 않는다.
- reward modal의 최종 두 label은 현재 라운드명과 보스명으로 이삭/싯나를 표시한다. R03도 같은 expression으로 노아/네피림을 유지한다. 후보 선택은 기존 SelectSlot→Host→Core 경로이고, 기존 진화 표시와 pending 성장 UI 뒤 정산을 유지한다. 완료 화면은 이삭 완료·R07 미구현 대기다.
- `sync.json`의 성공 결과와 `core-provenance.json`/Core handoff에 기록된 소스 hash 항목을 대조해 일치를 확인했다. DLL 기록은 provenance·sync·binding의 repo/final app에서 `3719a258…442d50`으로 일치한다. 기록된 provenance 비교이며 새 전체 해시/재빌드 검사를 실행한 것은 아니다.

## 재사용 검증과 최종 앱 구분

- `related.xml` **26/26**, `final-EditMode/results.xml` **111/111**, `final-PlayMode/results.xml` **31/31**, `checks.json`의 project/portable PASS를 확인했다. R06 Host 시험은 동적 intro/70·35 mask와6·12지원 및 disk 재개/과거 R03 receipt, 고정 후보·이미 진화한 sling 제외·bible 진화, 보상/정산 각각의 실패 payload 고정과 retry, pending 성장 뒤100gold·cross·회복/between7 once,7클립/시계/반경/짧은 death를 다룬다.
- 위 시험은 마지막 **reward-only 두 label expression 수정 전** 근거다. `reward-copy-fix.json`·`representative-reuse.json`에 영향 범위가 명시되어 있으며, 최종 `build-review/R06Review.app`(run `r06f88a2de38dda`)은 보정 소스의 정상 compile/macbuild **exit0** 및 reward checkpoint Load 화면으로 확인했다. 두 문구 수정 뒤 전체 시험/전투를 재실행한 것으로 세지 않는다.
- `combat-capture/continuous-trace.txt`에는 실제 Core 예고와7초 impact, HP4520에서 mask1/6지원 및 HP2270에서 mask3/12지원 disk replay,231.283초 좌표 보스 처치→고정200gold, 짧은 death 제거, 고정 후보 Load·SelectSlot 성경 진화→pending0→완료100/cross/HP회복·between7→새 process Run/Profile 동등이 남아 있다. 과거 R03/현재 R06 receipt·PreviousNormalRound5·sling/bible form을 보존한다.
- 최종 대표01/03과 전투 기록은 두 reward 문구의 영향을 받지 않는 앞 정상 combat app 근거 재사용이다. 최종02는 보정된 최종 앱의 새 reward-only 화면이다. 앞 `combat-capture/02`의 노아/네피림 오표기는 FAIL 보존이며 최종 대표에 포함하지 않는다. 화면 독립 UI 판정은 별도 담당 범위다.
- 첫 두 QA 실행 **exit2**는 보조 슬롯6칸 만석 및 intro weapons0 fixture 검증 실패로 보존되어 있다. 임시 TextAsset seed 보정도 `qa-seed-adjustment.json`에 따로 남아 있다. 초기·승인 QA 교정·승인 UI 교정의 정상 build 명령은 총 **3개 exit0**이며 최종 앱은 수동 TextAsset 패치 결과물이 아니다. 초기 실패를 PASS 실행으로 세거나 단일 build로 축약하지 않는다.

## 범위와 한계

앱은 이전 R05 기술 seed/HP10000·장비와 bible8/scroll 준비, normal timeout·초기 보스 위치(3,0)·첫 무기 cycle190을 사용한다. 이후 보스 피해/처치는 실제 좌표 전투지만 자연 일반780킬/밸런스 완주 근거는 아니다. 지원12는 소환 수다. 앱 pending 성장은0이고 남은 성장 차단/선택은 Host 시험 근거다. F2/F3 모든 실애니메이션·전프레임·native input·mobile/device/performance·실사용 저장은 미검증이다.

최종 path probe/자기 child exit0·자기3저장 absent 복구·보호 대상 불조회는 인계 기록을 재사용했다. 리뷰어는 제품/시험/아트/DLL/저장을 편집하거나 새 suite·해시행렬·CLI·Editor·GUI·build를 실행하지 않았다. 실제 의심 재현이 필요한 새 결함이 없어 probe도 추가하지 않았다. 소유는 본 문서뿐이다. usage `r06-unity-review` start/end는 세션 기준점이며 stale 시작 event와 개별 worker 비용/effort 미측정을 구분하고 비용·절감률을 추정하지 않는다.
