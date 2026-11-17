# R15 Core — 브살렐과 출애굽기 완료

2026-11-18 KST. 승인된 `docs/harness/r15-implementation-01.md`의 마지막 Core 단위를 구현했다. Core/콘텐츠/Sim/검사만 담당하며 독립 코드 리뷰와 Unity 최종 통합은 root에게 인계한다. 이 Core 결과를 R10~R15 전체 제품 완료로 세지 않는다. 다음 책·새 무기·아트·Unity/DLL/provenance·GUI·사용자 저장·commit/push는 수정하거나 실행하지 않았다.

## 선행 CI와 실제 작성

제품 편집 전에 정확 SHA `f194bdddcb163c755e2682617dabc5d2fcbed865`의 `gh run watch 44141939976 --exit-status`와 `gh run watch 44141939967 --exit-status`를 실행해 둘 다 실제 exit0을 확인했다. 같은 SHA의 completed/success도 조회한 후 제품 작성을 시작했다. `evidence/r15-core/r14-ci-{core,art}-watch.log`, `r14-ci-{core,art}-status.json`이 근거다. 대기 중 제품 편집은 없었고 자기 usage 시작 및 최초 실제 작성 상태를 Orca status로 보냈다.

## 콘텐츠와 공개 API

`ExodusContent.Round(15)`/`SpeedsFor(15)`, `RoundFifteenContract`, `LastInstalledRound=15`를 추가했다. `exodus_06` local6/global15, 180초/생성170초/cap120/총1200/원시XP5600/완료280/null 해금이다. ex16/17/18의 HP280/400/850·접촉13/14/16·속도.85/.65/.35, waves256/64/0·280/120/0·240/144/96을 사용한다. 일반 적은 기존 추적/접촉뿐이며 기존 HP 우선 정수 XP 분배 총합5600을 확인했다.

`BossDefinition.ForRound(15)`는 `defilement_weaver`, HP26000/contact22/kill500/period5/AttackDamage18/actor1201/ex17 지원을 반환한다. 기존 공통 move ratio.30/radius.60/보호intro2초/offset(0,+3)을 사용한다. 새 공개 `WeaverPolicy`, `UsesChainProjectiles`, `ProjectileSpeed`, `ProjectileRadius`, `ProjectileLifetime`, `ProjectileCap`, `ProjectileSpreadDegrees`를 제공한다. R15 값은1.35/.16/6/12/20도, `TelegraphDuration=1`이며 R09도 같은 기하 규칙을 읽는다. `AttackDamage`는 R15에서18, R09에서15다.

Unity는 `AdamSession.BossRules`, 방어 복사 `Boss`의 BossElapsed/NextAttackAt/Telegraphing/ChainDirections, `BattleWorld.EnemyPositions[BossActorId]`의 실제 live origin, 방어 복사 `HostileProjectiles`의 ID/BornAt/현재 좌표/고정 방향/남은 수명, `Capture().Tuning`의 실제 player 반경을 읽을 수 있다. 별도 공격 시계/PNG 기반 판정을 추가할 필요가 없다. 예고 길이는 Rules.ProjectileSpeed×ProjectileLifetime이다.

R09의 기존 세 방향 경로를 명시 `UsesChainProjectiles`로 재사용한다. 첫4초에 멈춰 center/±20도를 고정하고5초에 live boss origin에서3발을 tick끝에 생성한다. 겹침 중심은(1,0), 다음 tick부터 이동하며 절대 BornAt 기반 직선 이동/수명과 상대 swept 원 충돌을 유지한다. 공통 armor/무적 경로를 사용하고 막힌 적탄도 접촉 시 소멸한다. player 사망을 무기/반격보다 먼저 처리하고 boss kill 뒤 적탄/배우를 정리한다. 새 homing/반사탄/slam/dash는 없으며 기존 crown 반격 정책을 변경하지 않는다. HP60%·30%에 ex17 각6개체/총12를 once 생성하고 치명타에는 추가 소환이 없다.

`AdamSession.ExodusComplete`는 현재 R15의 완료 receipt/between16을 뜻하며 LastInstalledRound에 의존하지 않는 명시 상태다. 기존 `ChapterComplete`/`CanEnterExodus`의 R09→R10 의미와 `PrepareExodus` 원자적 candidate/commit 경로는 변경하지 않았다. `ContentComplete`는 설치끝 의미를 그대로 유지한다. R15 완료 Load는 장비/진행을 보존하며 자동 다음 책 진입이나 강제 새 도전이 없다.

## 저장·보상 경계

root schema3과 기존 DataMember shape를 유지한다. R09와 동형이므로 R15 current Boss3/World4 및 HostileProjectile Version1/ChainDirections를 재사용했다. 새 schema 필드/버전/다섯 번째 과거 receipt 필드는 없다. R12 Boss4/World5와 `CompletedFourthBoss`는 과거 기록 그대로다. R10~R14 및 창세기 fingerprint 계약을 바꾸지 않고 R15의 새 계약만 추가했다.

R14 between15→`StartNextRound(ExodusContent.Round(15))`는 네 과거 receipt·RunId·장비/보조/진화·레벨/XP/serial·영구 snapshot·프로필을 유지한다. PreviousNormalRound=14이며 과거 정산을 재지급하지 않는다. 새 World4의 적탄은 빈 배열/NextHostile1, 돌진은 빈 배열, clock0/LastHitTime=-1로 시작한다. 일반 종료 drop 회수/남은 성장 뒤 보호 intro를 진행하며 조기 회복은 없다.

R15 normal/intro/boss/reward/settlement/between16만 지원하고 일반 round_settlement는 열지 않는다. 현재 R15 Boss와 R03/R06/R09/R12의 네 history를 함께 검증한다. World4의 시계/대상/drop/방향/ID/BornAt/수명·5초 발사 ledger/지원mask, 동결 후보/policy/선택 ledger를 기존 검증으로 재개한다. R16 전투·R15 World5/Boss4·미지원 policy/적탄 version/중첩 필드와 네 번째 receipt 미래 필드는 Load/Commit을 UnsupportedSaveVersionException으로 거절하고 primary/backup을 보존한다.

고정500, 선택 고갈100, 성장 고갈25, 완료280은 별도 once 경로다. 후보최대3/가능진화우선/이미진화제외/동결후보의 무지급 Load를 유지한다. 남은 성장 해결 후 완료280/null 해금/최대HP회복/between16이다. 죽으면 Run이 사라지고 명시 새 도전 constructor는 영구 프로필 유지 R01/schema2다. 완료 상태에서 새 도전은 호스트의 명시 Run 초기화 후에만 시작하며 Core가 자동 reset하지 않는다.

## 실행 근거와 실패/복구

| 실제 명령 | 결과/exit | 근거 |
| --- | --- | --- |
| `bash tools/dotnet.sh run --project tests/BibleSurvivor.Core.Tests -- --r15-only` 최초 | FAIL compile, exit1 | `evidence/r15-core/initial-related.log`; 시험 준비의 nullable Forms.Remove 경고 CS8602 |
| 같은 R15 관련 명령, null 표기 교정 뒤 | PASS8/8, exit0 | `evidence/r15-core/related-nullability-fixed.log` |
| 같은 runner `--r09-only` | PASS6/6, exit0 | `evidence/r15-core/r09-related.log` |
| 같은 runner `--r12-only` | PASS10/10, exit0 | `evidence/r15-core/r12-related.log` |
| 같은 runner `--r14-only` | PASS6/6, exit0 | `evidence/r15-core/r14-related.log` |
| `python3 evidence/r15-core-mutation/run.py` | PASS, exit0; baseline0/mutant1/restored0 | `evidence/r15-core-mutation/summary.json` 및 원로그 |
| `python3 tools/check_all.py` 최종1회 | PASS211/211, exit0;15명령 모두 exit0 | `evidence/r15-core/final-gate.log`, `evidence/r02-20261117T181425053122Z/checks.json`, `core-tests.xml` |
| gate의 `bash tools/dotnet.sh run --project tools/BibleSurvivor.Sim -- --r15-series sling 7` | PASS, exit0 | 같은 최종 gate 폴더 `14.log` |
| `git diff --check` | PASS, exit0 | 종료 전 검사 |

신규8개는 콘텐츠/XP/Rules/R09 피해15, R14→R15 disk entry/네 history/일반 World4/보호 intro, 실제3방향/정지/고정방향/live origin/다음 tick 이동/겹침/TTL/무적·pause, 상대 swept 피해18와 armor/무적 적탄 소멸/사망 우선/killclear, 지원6/12 once, 예고/실탄 disk replay와 미래 원문보호, 동결 진화우선 후보/500/선택 retry/280·ExodusComplete Load, 고갈100/성장25/280·회복/완료 retry/명시 새 도전을 묶었다. 사망 뒤 새 R01/schema2 및 프로필 보존도 검사했다.

기술 fixture는 기존 R14 완료 준비(최대 장비/진화/timeout/직접 보스 Hit)를 재사용한다. 보스 좌표30과 무기 clock1000으로 패턴을 격리하고 겹침 시험은 HP10000을 사용한다. 진화 선택 시험은 과거 reward ledger가 요구하지 않는 진화 하나만 기술 준비에서 제거해 진화 가능성을 만든 뒤 기존 Validate/Resume을 사용한다. 실제 지원/보상은 직접 Hit로 진행하며 자연 완주가 아니다.

상대 swept 시험은 기술 PlayerSpeed=120/MoveSpeedMultiplier로 player가 한 fixed tick에-1→+1을 이동하고 쇳조각은0→.0225를 이동하게 한다. 양 endpoint가 합산 반경.41 밖이어도 교차 충돌하며 seal5를 적용한 실제 피해13(원시18)과 적탄 소멸을 확인했다. 원구조 변이는 이 상대 선분 검사를 endpoint 거리 조건으로 되돌렸고 `Expected 87, actual 100`으로 실패했다. baseline8/8→mutant7/8→restored8/8이며 compile 실패는 검출로 세지 않았다. 작업 소스에는 변이를 적용하지 않았다.

고갈 정산 대표는 normalPending5/후보1/성공 Choose5, bossPending2/후보1/성공 Choose2를 구분했다. 성장25×7=175, kill500, 자동 고갈100, 완료280을 따로 검증했다. 마지막 성장 뒤에도 HP는 부상 상태이며 `CompleteBossSettlement`에서만 회복한다. 동결 진화 후보에서 실제 성공 보상 선택1회와 중복 거절·저장 실패 retry, 완료 저장의 실패 retry와 Load 후 무추가지급을 확인했다. 자동100을 사용자가 카드를 고른 횟수로 세지 않는다.

최종 gate 안의 한 무기 인자 sling/seed7 CLI는 기존 R01 prefix를 포함해 R09→R15 실제 좌표 전투와 disk Save/Load를 통과했다. R15 hits12891/일반·지원kills1097/boss1/World335.6833333초/좌표168샘플이며 normal60초 midpoint와 boss telegraph/projectiles에서 World4/Boss3 전체 JSON 동등 disk Restore를 확인했다. 좌표 샘플에 chase/telegraph/projectiles가 있고 관측 최대 적탄수는6이다. 완료 disk Load는 현재 R15 Boss+네 history·RunId를 유지하고 between16/ExodusComplete=true/ContentComplete=true/ChapterComplete=false, 재정산 무지급을 확인한다. 전체 일반1200 처치나 자연 완주라고 세지 않는다.

기존 R10 hits4517/kills894/180초, R11 hits5394/kills960/176.4166667초, R12 hits8683/kills1028/boss1/292.1333333초와 telegraph/dash Restore, R13 hits7594/kills1059/180초, R14 hits10971/kills973/180초는 이전 결과를 유지했다. CLI의 기존 초기 HP10000/장비8/보조1 fixture에서 R15 진입 장비는 bible/jawbone/sling8·sling진화·stones2/crown5/seal5/cloud5/underwear5/whirlwind5다. 실제 hit/kill 주입은 없지만 기술 HP/강화 장비를 사용하며 자연 밸런스가 아니다. 대표 CLI는 최종 gate 안에서 한 번 실행했고 별도 같은 시리즈를 반복하지 않았다.

## 파일과 소유 반환

worker 변경21파일: Core `AdamSession.cs`, `BattleWorld.cs`, `BossEncounter.cs`, `BossRewards.cs`, `ExodusContent.cs`, `SaveFormat.cs`, `SaveState.cs`, `Waves.cs`; `content/exodus/catalog.json`; tests `RoundFifteenTests.cs`, `RoundFourteenTests.cs`, `RoundThirteenTests.cs`, `RoundElevenTests.cs`, `Program.cs`; Sim `CatalogReader.cs`, `ExodusRunner.cs`, `BossRunner.cs`, `Program.cs`; 대표 명령 연결만 바꾼 `tools/check_all.py`; 이 문서와 `docs/retrospectives/R15-core.md`다. 기존 검사 수정은 R14 설치끝 기대/잘못된 지원15 catalog 거절 형식과 R11/R13/R14 미래 경계16 이동뿐이다.

공유 working tree의 `tools/check_mutations.py` Resume old/new 상한14→15 동기화는 root 작성이며 worker가 편집하지 않았다. 구체 anchor 후속을 전달했고 반환 후 root 선택 변이 실행 대상으로 남긴다. schema checker/hook/CI/Unity/DLL/provenance/아트/기획/QA 문서는 수정하지 않았다.

Core 제품 소유를 root에게 반환한다. 독립 code review, Unity catalog/Host/UI/World·DLL/provenance/sync 및 Editor/정상 앱/새processLoad/최종 UI·native/mobile/performance·자연 밸런스는 후속 담당의 NOT_RUN/UNVALIDATED다. 승인 R10~R15 전체 종료는 root의 Unity 최종 게이트 후이며 다음 책 제품은 시작하지 않는다.

usage는 실제 Codex session `01a1ea4f-e630-7e10-b3f7-34dbb4899d6d`의 R15-core start/end provider event `17:32:42.722Z`→`18:16:14.759Z` 구간이다. input8726492/cache8673664/cache-write0/output32567/reasoning11869/total8759059이며 reasoning을 output에 다시 더하지 않았다. 시작 호출의 이전 phase delta는 합산하지 않는다. `evidence/model-usage/checkpoints.jsonl`, end 복사본 `evidence/r15-core/usage-end.json`이 근거다. 지연 telemetry/CI 대기·보고를 포함하는 세션 구간이며 end 뒤 최종 문서/보고 tail은 제외한다. 실제 청구액·단가/전용 비용·절감률은 미계산이다.
