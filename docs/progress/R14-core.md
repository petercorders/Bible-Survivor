# R14 Core — 아론과 공동체

2026-11-18 KST. 승인된 `docs/harness/r14-implementation-01.md`에 따라 `exodus_05` local5/global14의 Core/콘텐츠/Sim과 관련 검사를 구현했다. 독립 코드 리뷰 및 Unity 통합은 root에게 인계한다. 아트·Unity·DLL/provenance·schema checker/hook/CI·GUI·사용자 저장·commit/push·R15 제품은 수정하거나 실행하지 않았다.

## 선행 게이트와 작성 시작

제품 편집 전에 정확 SHA `399f49324ee76214e9847582c7f3f06b71062aaf`를 `gh run list --commit`으로 조회하여 Core44134873664/art44134873411을 찾았다. `gh run watch 44134873664 --exit-status`, `gh run watch 44134873411 --exit-status` 둘 다 실제 exit0을 확인하고 동일 SHA의 completed/success를 재조회한 후 제품 작성을 시작했다. `evidence/r14-core/r13-ci-{core,art}-watch.log`, `r13-ci-{core,art}-status.json`이 근거다. run ID를 추정하지 않았고 대기 중 제품 편집은 없었다. 자기 usage 시작 근거와 최초 실제 작성 상태를 Orca status로 보냈다.

## 구현과 API/저장 계약

`ExodusContent.Round(14)`/`SpeedsFor(14)`, `RoundFourteenSpeedContract`, `LastInstalledRound=14`를 추가했다. 180초/생성170초/cap120/총1140/원시XP5200/완료260/null 해금·보스다. ex13/14/15의 HP220/700/360·접촉12/15/14·속도.80/.35/.60, waves240/60/0·266/114/0·230/138/92를 사용한다. 기존 HP 우선 정수 XP 분배로 총5200을 검사했다. 모두 기존 chase/contact이며 새 돌진/적탄/피해 packet은 없다.

`BattleWorld.Synchronize`에서 R07/g3의 기존 명시 species 조건에 R14/ex13만 더했다. 기존 `Session.SpeciesOrdinal(id)`로 종별 홀수는 왼쪽·짝수는 오른쪽의 화면 밖 경계에 생성하고 세로 위치는 기존 RNG를 사용한다. ex14/ex15와 다른 라운드는 기존 생성 경로다. 한 actor의 HP/죽음은 다른 actor에 전파하지 않으며 기존 WaveDirector의 생존 개체 유지도 그대로다. R07 fingerprint와 RNG 소비 순서를 바꾸지 않았다. R14 fingerprint만 새 속도/좌우생성 계약을 포함한다. R13 fingerprint literal `IYXFTf1Ay0QB/3B5vLnYziBZsV/32jIp1/5LTFiPdnk=` 및 기존 R10/R11 literal 검사를 둔다. R12와 이전 정의·계약은 변경하지 않았다.

R13 between14에서 `StartNextRound(ExodusContent.Round(14))`로 일반 전환하며 `PrepareExodus`를 사용하지 않는다. R03/R06/R09/R12의 네 receipt·RunId·무기/보조/진화·레벨/XP/serial·영구 snapshot·프로필을 유지하고 PreviousNormalRound=13을 기록한다. R13 완료240은 진입/Load/재호출에서 재지급하지 않는다. R09→R10 별도 원자적 candidate/commit 경로는 그대로다.

root schema3, optional `CompletedFourthBoss`를 포함한 기존 저장 shape와 Boss4/World5의 R12 제한을 유지한다. R14 normal/round_settlement 및 완료 between15만 지원한다. 네 번째 과거 receipt의 검증 범위만 R14까지 이어가며 새 schema 필드는 없다. 새 World는 기존 normal World3·빈 Charges·적탄 없음/NextHostile0·clock0/LastHitTime=-1이며 이전 전투의 적탄/돌진/보스 시계를 넘기지 않는다.

R15 전투/미지원 stage, R14 현재 World5, boss5/receipt unknown member, root4는 UnsupportedSaveVersionException으로 Load와 Commit을 거절하고 primary/backup 원문을 보존한다. 완료 순서는 drop 회수→남은 성장(고갈25)→완료260 once/null 해금/최대HP회복→between15다. `ContentComplete=true`, `ChapterComplete=false`이며 R15 준비중 경계다. 죽음/호스트의 명시 Run 초기화 후 새 도전은 프로필 유지 R01/schema2다.

## 검증과 실제 실패/복구

| 실제 명령 | 결과/exit | 근거 |
| --- | --- | --- |
| `bash tools/dotnet.sh run --project tests/BibleSurvivor.Core.Tests -- --r14-only` | PASS6/6, exit0 | `evidence/r14-core/initial-related.log` |
| 같은 runner `--r13-only` | PASS6/6, exit0 | `evidence/r14-core/r13-related.log` |
| 같은 runner `--r07-only` | PASS3/3, exit0 | `evidence/r14-core/r07-related.log` |
| `python3 evidence/r14-core-mutation/run.py` | PASS, exit0; baseline0/mutant1/restored0 | `evidence/r14-core-mutation/summary.json`, 각 원로그 |
| `python3 tools/check_all.py` 최종1회 | PASS203/203, exit0;15명령 모두 exit0 | `evidence/r14-core/final-gate.log`, `evidence/r02-20261117T172956904445Z/checks.json`, `core-tests.xml` |
| gate의 `bash tools/dotnet.sh run --project tools/BibleSurvivor.Sim -- --r14-series sling 7` | PASS, exit0 | 같은 최종 gate 폴더 `14.log` |
| `git diff --check` | PASS, exit0 | 종료 전 검사 |

신규6개는 정확 콘텐츠/XP/cap, R13 완료 및 R14 진입 disk Load/네 receipt/성장·프로필 동등성/무추가지급/새 World, 실좌우 좌표·독립HP/이전 웨이브 생존·중간 Load/일시정지/돌진유입 거절, 정산·두 저장 실패 retry/성장25·260 once/회복/null 해금, 미래 원문보호, receipt 필수·사망/명시 새 도전을 묶었다. 기존 R13의 설치끝 기대를 false로 바꾸고 지원되는14의 잘못된 catalog는 ArgumentException을 기대한다. R11/R13의 미지원 전투 경계만15로 이동했다.

좌우 생성 시험은 기술 timeout/장비 fixture에서3초의 실제 ex13 생성 좌표를 읽고 종별 홀짝의 화면 밖 x경계를 검사했다. 첫 actor에 Hit7을 주면 HP213, 이웃은220이며 첫 actor 처치/수집 뒤에도 이웃은220이다. wave 경계를 넘긴62초에도 생존 actor를 유지하고 disk Restore 뒤1초 같은 입력의 World JSON이 일치한다. 이 fixture의 직접 Hit/kill과 시간 전진을 자연 전투로 세지 않는다.

정산의 실제 pending5·후보1·성공 Choose5를 구분했으며 성장125와 완료260을 따로 검사했다. 완료 전에는 부상 HP를 유지하고 마지막 선택 후 최대HP로 회복한다. 선택 저장과 완료 저장 각각의 injected IOException 뒤 동일 operation retry/retry 및 Load 후 이전 선택·정산 거절을 확인했다. 실패한 저장 전 disk 상태가 그대로인 것도 검사했다.

최소 원회귀 변이는 격리 사본의 좌우 조건을 원래 `RoundNumber==7 && MonsterId==g3` 전용 구조로 되돌려 ex13이 기존 generic random-edge 생성으로 빠지게 했다. baseline6/6→mutant5/6→restored6/6이며 mutant는 실제 좌표 `Expected -11, actual -7.809579372406006` assertion으로 실패했다. compiler error나 비슷한 다른 변이를 검출로 세지 않았다. 원본 작업 소스는 변이하지 않았다.

최종 gate 안의 한 무기 인자 sling/seed7 CLI는 R01→R14 실제 좌표 전투와 disk Save/Load를 통과했다. R14 hits10971/kills973/boss0/180초·90좌표 샘플이며 진입과60초 midpoint 및 완료 disk Load, 네 receipt·RunId 유지, between15/ContentComplete=true/ChapterComplete=false를 확인한다.1140 전체 처치나 자연 완주라고 세지 않는다. R10 hits4517/kills894/180초, R11 hits5394/kills960/176.4166667초, R12 hits8683/kills1028/boss1/292.1333333초, R13 hits7594/kills1059/180초와 R12 예고·돌진 disk restore는 이전 결과를 유지한다.

CLI의 기존 초기 fixture는 HP10000/장비8/보조1이며 R14 진입 장비는 bible/jawbone/sling8·sling진화·stones2/crown5/seal5/cloud5/underwear5/whirlwind5다. 실제 hit/kill 주입은 없지만 강화 장비/HP가 있는 기술 검증이며 자연 장비/밸런스 실측은 아니다. 대표 CLI는 최종 gate 안에서 한 번 실행했고 별도 같은 시리즈를 반복하지 않았다. R14 제품 검사에서 예기치 않은 실패는 없었으며 의도된 저장 IOException/미래 거절과 최소 변이의 exit1을 실제 부정 사례로 보존한다.

## 소유 반환/후속

worker 변경18파일: Core `AdamSession.cs`, `BattleWorld.cs`, `ExodusContent.cs`, `SaveFormat.cs`, `SaveState.cs`, `Waves.cs`; `content/exodus/catalog.json`; tests `RoundFourteenTests.cs`, `RoundThirteenTests.cs`, `RoundElevenTests.cs`, `Program.cs`; Sim `CatalogReader.cs`, `ExodusRunner.cs`, `BossRunner.cs`, `Program.cs`; 기존 대표 명령 연결만 바꾼 `tools/check_all.py`; 이 문서와 `docs/retrospectives/R14-core.md`다. shared working tree의 `tools/check_mutations.py` 상한13→14 old/new 변경은 root 소유이며 worker가 편집하지 않았다. root에 구체 anchor 후속을 전달했다.

독립 code review 및 root 선택 Resume 변이, Unity catalog/Host/UI/World와 DLL/provenance/sync, Editor/정상 앱/새process Load·native/mobile/performance·자연 밸런스는 후속 담당의 NOT_RUN/UNVALIDATED다. Core 제품 소유를 root에게 반환하며 R15 제품을 임의로 시작하지 않는다.

자기 실제 Codex session `01a1ea4f-e630-7e10-b3f7-34dbb4899d6d`의 R14-core start/end provider event `16:53:57.641Z`→`17:31:31.281Z` 구간은 input4410110/cache4384384/cache-write0/output18342/reasoning3815/total4428452다. reasoning을 output에 다시 더하지 않으며 시작 호출의 이전 phase delta는 R14로 합산하지 않았다. 근거는 `evidence/model-usage/checkpoints.jsonl`, end 복사본 `evidence/r14-core/usage-end.json`이다. 지연 telemetry/CI 대기·보고를 포함하는 세션 구간이고 end 뒤 최종 문서/보고 tail은 제외한다. 실제 청구액·단가·비용/절감률은 미계산이다.
