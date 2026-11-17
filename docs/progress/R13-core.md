# R13 Core — 미리암과 홍해 이후 공동체

2026-11-18 KST. 승인된 `docs/harness/r13-implementation-01.md`의 `exodus_04` local4/global13을 구현했다. 순수 Core/콘텐츠/Sim/검사만 담당하며 독립 리뷰와 Unity 통합은 coordinator에게 인계한다. 원본 아트·Unity·DLL/provenance·GUI·사용자 저장·commit/push는 수정하거나 실행하지 않았다.

## 선행 CI 게이트와 실제 작성

첫 배정에서 R12 SHA `9f711bfa4d63b8e16898511c42c016b959ca43d1`의 `gh run watch 44124668750 --exit-status`는 exit0이나 `gh run watch 44124668801 --exit-status`는 exit1이었다. `Save schema concrete regression mutations`의 `future_world_version_recovers_legacy_backup` 미검출을 보고하고 제품 편집 없이 소유를 반환했다. `evidence/r13-core/r12-ci-core-failed.log`, `r12-ci-core-status.json`, `r12-ci-art-status.json`이 이 실패를 보존한다.

root의 복구는 `SaveSchemaTests.cs`의 미래 World 입력 두 곳을5→6으로 바꿨고 독립 `docs/reviews/R12-ci-recovery.md` PASS를 받았다. 이번 worker가 해당 파일이나 schema 검사기를 바꾸지 않았다. 재배정 뒤 `gh run list --commit 3bc19dc7d75cece0768bc4506843d1a1f29cb9d3`에서 정확한 두 run을 찾았다. `gh run watch 44127497272 --exit-status`, `gh run watch 44127497458 --exit-status` 모두 실제 exit0을 확인하고 같은 SHA의 completed/success도 조회한 뒤 제품 작성을 시작했다. 근거는 `evidence/r13-core/recovery-ci-{core,art}-watch.log`, `recovery-ci-{core,art}-status.json`이다. 최초 실제 작성 상태와 root 소유 mutation anchor 후속을 Orca status로 전달했다.

## 구현과 공개 API/저장

`ExodusContent.Round(13)`/`SpeedsFor(13)`, `RoundThirteenSpeedContract`, `LastInstalledRound=13`을 추가했다. ID `exodus_04`, 180초/생성170초/cap110/총1080/원시XP4800/완료240/null 해금·보스다. ex10/11/12는 HP150/280/600, 접촉10/12/15, 속도.60/.60/.35의 기존 추적/접촉만 사용한다. 구성은224/56/0,216/144/0,220/132/88이며 기존 정수 XP 분배 총합4800을 검사했다. R10~12 정의와 fingerprint 계약은 바꾸지 않고 R13 속도 계약만 새 fingerprint에 포함했다.

`RunState.CompletedFourthBoss`는 optional `BossEncounterState` DataMember이며 R12의 완료 Boss4 receipt를 그대로 복사한다. R12 between13의 `StartNextRound(ExodusContent.Round(13))`는 이 필드와 `PreviousNormalRound=12`를 설정하고 현재 Boss를 비운다. R03/R06/R09의 `CompletedBoss`/`CompletedSecondBoss`/`CompletedThirdBoss`와 RunId·장비·보조·진화·레벨·XP·serial·영구 snapshot·프로필을 이어간다. R12의400+고갈100+완료220은 진입·Load·재호출로 재지급하지 않는다. R09→R10 별도 candidate/commit 진입은 변경하지 않았다.

root schema3을 유지한다. R13은 normal/round_settlement와 완료 between14만 지원하며 현재 Boss가 없다. 새 World는 기존 일반 라운드와 같은 World3, 빈 Charges, 적탄 없음, clock0/LastHitTime=-1로 시작한다. World5는 여전히 R12에만 허용되며 과거 Boss4 receipt와 현재 World3을 구분한다. R13 receipt는 완료된 R12/같은 RunId/고정 policy와 ledger를 기존 보스 검증으로 검사하며 누락·덮어쓰기·불완료 receipt를 거절한다. `SaveFormat`의 명시 shape/재귀 검증에 네 번째 필드를 추가했고, 다른 라운드에 나타난 네 번째 필드는 미래형식으로 거절한다.

R14 전투/미지원 stage, World5를 R13 현재 World로 사용, boss5, 과거 receipt의 미래 phase/member, root4는 Load/Commit 모두 원문·backup 보존으로 거절한다. R13 완료는 drop 회수→남은 성장(고갈25)→완료240 once·회복→between14다. `ContentComplete=true`, `ChapterComplete=false`, R14 준비중 경계이며 출애굽기 전체 완료를 뜻하지 않는다. 사망과 호스트의 명시 Run 초기화 뒤 새 도전은 프로필 유지 R01/schema2다.

## 검증

| 실제 명령 | 결과/exit | 근거 |
| --- | --- | --- |
| `bash tools/dotnet.sh run --project tests/BibleSurvivor.Core.Tests -- --r13-only` | PASS6/6, exit0 | `evidence/r13-core/initial-related.log` |
| 같은 runner `--r12-only` | PASS10/10, exit0 | `evidence/r13-core/r12-related.log` |
| 같은 runner `--r11-related` | PASS10/10, exit0 | `evidence/r13-core/r10-r11-related.log` |
| `python3 evidence/r13-core-mutation/run.py` | PASS, exit0; baseline0/mutant1/restored0 | `evidence/r13-core-mutation/summary.json` 및 원로그 |
| `python3 tools/check_all.py` 최종1회 | PASS197/197, exit0;15명령 모두 exit0 | `evidence/r13-core/final-gate.log`, `evidence/r02-20261117T165105556169Z/checks.json`, `core-tests.xml` |
| gate의 `bash tools/dotnet.sh run --project tools/BibleSurvivor.Sim -- --r13-series sling 7` | PASS, exit0 | 같은 최종 gate 폴더 `14.log` |
| `git diff --check` | PASS, exit0 | 종료 전 검사 |

신규6개는 정확 콘텐츠/XP, R12 완료와 R13 진입 disk Load/네 receipt/장비·진화·성장 무변경/무추가지급, 새 World3·추적·pause·deterministic replay/돌진 유입 거절, 정산·저장 실패 retry·성장/240 once·회복·null unlock, 미래형식 primary/backup 보존, receipt 필수/사망·명시 새 도전을 묶었다. 기술 fixture는 timeout/직접 Boss Hit/최대 장비·진화/드롭XP100을 사용한다. 정산의 실제 pending5/후보1/성공 Choose5를 구분했고 성장125와 완료240을 따로 확인했다. 선택 저장과 완료 저장의 injected IOException 뒤 같은 operation retry/retry를 검사했다.

최소 원회귀 변이는 격리 소스 사본에서 R12의 완료 receipt 목적지를 `CompletedFourthBoss` 대신 기존 공통 전환의 `CompletedBoss`로 되돌렸다. 새 PreviousNormalRound=12는 유지해 clock/lifecycle marker 회귀와 분리했다. baseline6/6→mutant1/6→restored6/6이며 mutant의5개 실패는 `Invalid or reordered completed boss history`다. 컴파일 실패를 검출로 세지 않았고 작업 소스에는 변이를 적용하지 않았다.

최종 gate 안의 한 무기 인자 sling/seed7 CLI는 R01→R13 실제 좌표 전투와 disk Save/Load를 통과했다. R13 hits7594/kills1059/boss0/180초/좌표90샘플, 진입과60초 midpoint disk Restore, 완료 disk Load 뒤 네 receipt/RunId 유지와 between14·ContentComplete=true·ChapterComplete=false를 확인했다. 기존 R10 hits4517/kills894/180초, R11 hits5394/kills960/176.4166667초, R12 hits8683/kills1028/boss1/292.1333333초는 이전 결과와 같았다. R12 예고/돌진 disk restore도 유지한다.

CLI 시작 fixture는 HP10000/장비8/보조1이며 기존 선택을 따라 R13 진입에는 bible/jawbone/sling8·sling진화·stones2/crown5/seal5/cloud5/underwear5/whirlwind5다. 실제 hit/kill 주입은 없지만 자연 장비·완주·밸런스 실측으로 세지 않는다. CLI는 최종 gate에서 한 번 실행했으며 별도 같은 시리즈를 반복하지 않았다. R13 작성 후 예기치 않은 제품 검사 실패는 없었고 의도적 저장 IOException/거절·최소 변이의 exit1은 부정 수용 사례다.

## 파일 소유와 후속

worker 변경18파일: Core `AdamSession.cs`, `BattleWorld.cs`, `ExodusContent.cs`, `SaveFormat.cs`, `SaveState.cs`, `Waves.cs`; `content/exodus/catalog.json`; tests `RoundThirteenTests.cs`, `RoundElevenTests.cs`, `RoundTwelveTests.cs`, `Program.cs`; Sim `CatalogReader.cs`, `ExodusRunner.cs`, `BossRunner.cs`, `Program.cs`; 기존 실행 연결만 바꾼 `tools/check_all.py`; 이 문서와 `docs/retrospectives/R13-core.md`다. R12 기존 검사의 설치끝 기대만 false로, 지원되는13의 잘못된 catalog 거절 기대를 ArgumentException으로 바꿨고 R11 미래 전투 경계를14로 옮겼다.

공유 working tree의 `tools/check_mutations.py`는 root가 completion_resume_refused의 old/new 상한12→13 두 곳을 동기화한 변경이며 worker 작성물이 아니다. root는 반환 후 해당 선택 변이1건을 검증할 예정이다. schema checker/hook/CI/Unity/아트/기획/QA 문서는 이 worker가 수정하지 않았다.

usage start/end는 실제 Codex session `01a1ea4f-e630-7e10-b3f7-34dbb4899d6d`의 `R13-core-resume`으로 기록했다. provider event `16:31:31.560Z`→`16:52:39.577Z` 구간 input4214103/cache4182272/cache-write0/output19996/reasoning3674/total4234099이며 reasoning을 output에 다시 더하지 않았다. start 호출이 출력한 이전 실패 phase 이후 delta는 이번 구간으로 합산하지 않는다. end는 `evidence/r13-core/usage-end.json`, 원장에는 start/end가 있다. 지연 telemetry와 CI 대기·보고가 섞인 세션 구간이며 end 뒤 마무리 문서/보고 tail은 제외한다. 실제 청구액/단가/비용/절감률은 미계산이다.

Core 소유를 coordinator에게 반환한다. 독립 code review, Unity catalog/Host/UI/World·DLL/provenance/sync와 Editor/앱·native/mobile/performance·자연 밸런스는 NOT_RUN/UNVALIDATED다. R14 제품 편집은 하지 않았으며 root 통합 사이클에서 진행한다.
