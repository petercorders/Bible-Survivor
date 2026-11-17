# R11 Core — 요게벳 구현 및 인계

2026-11-17. 승인 계약은 `docs/harness/r11-implementation-01.md`다. 출애굽기 local2/global11 `exodus_02`까지만 설치했다. 독립 리뷰·Unity 통합은 통합자에게 인계한다.

제품 편집 전에 R10 SHA `409327ecf9c6b8dafabb40fb145cabfd0198bb90`의 `gh run watch 44046348912 --exit-status` 및 `44046348829`가 각각 실제 exit0임을 확인했다. 두 run의 headSha·completed/success도 재확인했다.

## 구현 및 공개 계약

R10 between11에서 `StartNextRound(Round(11))`로 일반 라운드를 시작한다. `PrepareExodus`는 계속 R09→R10 전용이며 R09 자동 진입은 거절한다. R11 definition을 상태 변경 전에 검증한다. RunId·세 보스 receipt·장비·세 진화·성장·영구 스냅샷·프로필을 보존하며 R10 완료180을 재지급하지 않는다. 월드·피격 시계·drop을 초기화하고 PreviousNormalRound를10으로 기록한다.

schema3은 R10/R11 normal·round_settlement와 between11/12만 허용한다. 저장 단계에 맞는 현재/직전 라운드 fingerprint를 검증한다. R12 전투·미지원 필드·다른 fingerprint는 원문보호 예외로 거절한다. root3 버전을 올리지 않았으며 기존 R10 fingerprint `IunkZmlC50vfCA3mEtdpX5iiLjkDjk2gkFVsEXpZF9A=`를 고정 검사한다. 새 도전·사망 후 시작은 프로필을 보존하는 R01/schema2다.

R11 총960/원시XP4000/cap110/완료200·해금없음, ex4/5/6 수치와 세 구간 composition은 승인 계획 그대로다. 완료 시 기존 성장 선택과 고갈25gold를 순서대로 처리하고 200gold once·최대HP회복·between12가 된다. `ContentComplete`는 설치된 R11 끝으로 이동했고 `ChapterComplete`는 창세기 의미를 유지한다.

추가 공개 API는 `ExodusContent.LastInstalledRound`, `RoundElevenSpeedContract`, `Round(int)`, `SpeedsFor(int)`다. 기존 R10 `Round()`·`MonsterSpeeds`·`SpeedContract`는 유지했다. Sim에는 `CatalogReader.Exodus(file, number=10)`과 `--r11-series sling 7`을 추가했으며 기존 R10 명령도 제공한다.

## 검증

| 검사 | 결과·실제 exit | 근거 |
| --- | --- | --- |
| 최초 R10/R11 관련 | 9/10, exit1 | `evidence/r11-core/related-initial.log`; 새 R11 5개는 PASS, R10의 옛 미지원R11 기대1건 실패 |
| 해당 R10 경계 기대를 R12로 이동 | 5/5, exit0 | `evidence/r11-core/r10-boundary-fixed.log` |
| 원구조 회귀 변이 | PASS, baseline0/mutant1/restored0 | `evidence/r11-core-mutation/summary.json` 및 각 원본 log |
| 전체 standalone Core | PASS, 181/181, exit0 | `evidence/r02-20261117T083824205984Z/core-tests.xml` |
| 최종 전체 gate, 1회 | PASS, exit0 | 같은 폴더 `checks.json`; wrapper `evidence/r11-core/final-gate.log` |
| 대표 R01→R11 좌표 전투/중간 disk Load | PASS, exit0 | 같은 폴더 `14.log`; gate에 포함한 한 무기·한 seed |
| diff whitespace | PASS, exit0 | `git diff --check` |

변이는 R11을 새 later-normal 정산 분기에서 제외해 이전 generic Finish 구조로 돌렸다. pending 성장 전에 즉시 완료되는 원회귀를 재현했고 `Expected round_settlement, actual between_rounds`라는 assertion 실패로 검출했다. 컴파일 실패가 아니며 별도 임시 사본에서만 실행했다. 기존 R10 독립 증거 조합을 새로 반복하지 않았다.

대표 CLI는 sling/seed7, R10 hits4517/kills894/180초, R11 hits5394/kills960/176.4166667초다. R11은 마지막 살아 있는 적까지 모두 처치해 180초 전에 정상 종료했다. 두 라운드60초 중간 disk Save/Load, 세 receipt·RunId 보존과 재정산 거절, 최종 between12/ContentComplete를 확인했다. 시작 전 HP10000·equipment8/passive1을 넣는 기존 기술 fixture를 유지하며 hit/kill 주입은 없다. 실제 출애굽기 진입 장비는 bible/jawbone/sling8, sling진화, stones2와 crown/seal/cloud/underwear/whirlwind5, HP10000이며 로그에 공개했다. 단위검사에는 짧은 이전 보스 hit 및 성장 drop·pending fixture를 사용한다. 자연 완주·밸런스·Unity·native·모바일·성능은 NOT_RUN/UNVALIDATED다.

## 변경파일과 소유 반환

Core: `AdamSession.cs`, `BattleWorld.cs`, `ExodusContent.cs`, `SaveFormat.cs`, `SaveState.cs`, `Waves.cs`. 콘텐츠: `content/exodus/catalog.json`.

tests: 새 `RoundElevenTests.cs`, 등록/관련 필터의 `Program.cs`, R10 완료/미지원 경계 기대만 바꾼 `RoundTenTests.cs`. Sim: `CatalogReader.cs`, `ExodusRunner.cs`, `BossRunner.cs`, `Program.cs`. 최종 대표 CLI만 교체한 `tools/check_all.py`. 문서는 이 파일과 `docs/retrospectives/R11-core.md`다.

`tools/check_mutations.py`의 `completion_resume_refused`는 Resume 상한 변경으로 old/new literal의 `>10`을 `>11`로 동기화해야 한다고 통합자에게 전달했다. 해당 도구는 소유 밖이라 수정하지 않았다. 다른 mutation anchor는 유지했다.

Unity/DLL/provenance/sync/CI/아트/GUI/실사용 저장/기획/AGENTS/commit/push는 수정·실행하지 않았다. root의 독립 리뷰와 후속 통합에 소유를 반환한다.
