# R10 Core — 구현 및 인계

2026-11-17. 승인 계약: `docs/harness/r10-implementation-01.md`. 제품 범위는 출애굽기 local1/global10 `exodus_01`뿐이다. 6라운드는 초안이며 R11 전투는 미지원이다.

## 구현

창세기 기본 root schema2와 최대 지원 root schema3을 분리했다. Decode는 기존 schema1→2만 수행한다. 새 도전과 기존 창세기 재개는 schema2이며 schema3 프로필에서 명시 새 도전을 해도 프로필 구매·해금·잔액을 보존하고 R01/schema2가 된다. 창세기 카탈로그는 수정하지 않았다.

`AdamSession.PrepareExodus(next)`는 완료된 R09 session에서만 독립 복사본을 만든다. 원본 snapshot/session/파일은 건드리지 않는다. 복사본에 root3과 R10 wave를 구성하고 검증한 뒤 반환한다. R03·R06 receipt를 그대로 두고 유다 receipt를 `CompletedThirdBoss`에 보존한다. RunId·장비·보조·진화·성장·영구스냅샷·gold·kill 나머지·history는 보존한다. World·drop·피격시계는 초기화하며 진입 추가 회복/지급은 없다.

Host는 기존 revision과 고정 operation ID로 후보를 Commit하고 성공한 Load/Resume만 게시해야 한다. 실패/결과 불명 상태에서 기존 완료 session을 버리거나 정상 전투를 진행하지 않는다. 재시도에는 같은 후보/operation ID를 쓰며 commit 성공 후 R09로 자동 롤백하지 않는다. Core의 `StartNextRound`는 R09→R10 자동진입을 계속 거절한다.

root3은 R10 normal/round_settlement와 between11만 지원하고 R11 전투·미지원 root/중첩필드는 원문보호 예외로 거절한다. R10 World는 내부 schema3, 빈 Charges, 적탄 없음이며 root schema와 독립이다. R10 완료는 기존 성장 선택/만석25gold 경로를 거쳐 180gold once·해금없음·최대HP회복·between11이 된다. `ChapterComplete`는 기존 창세기 의미를 유지하고 `ContentComplete`는 설치된 R10 끝을 나타낸다.

## 공개 API

- `ExodusContent`: `ChapterId`, `LocalRoundNumber`, `GlobalRoundNumber`, `RoundId`, `SpeedContract`, `MonsterSpeeds`, `Round()`, `Validate(definition)`.
- `AdamSession`: `CanEnterExodus`, `ContentComplete`, `PrepareExodus(RoundDefinition)`.
- `RunState.CompletedThirdBoss`.
- `SaveFormat.GenesisVersion == 2`, `CurrentVersion == 3`.
- `SaveStore.Copy(GameSnapshot)`.
- Sim: `CatalogReader.Exodus(file)` 및 `Round(exodusCatalog,10)`, `--r10-series sling 7`.

## 검증

| 검사 | 결과·실제 exit | 근거 |
| --- | --- | --- |
| 기존 R09 관련 core | PASS, 0; 6/6 | 초기 `--r09-only` |
| R10 전용 | PASS, 0; 5/5 | `--r10-only`, golden schema1/schema2 각 대표1 포함 |
| 전체 standalone Core | PASS, 0; 176/176 | `evidence/r02-20261117T062135733099Z/core-tests.xml` |
| standalone 전체 gate | PASS, 0 | `evidence/r02-20261117T062135733099Z/checks.json` |
| 원회귀 변이 | PASS, baseline0/mutant1/restored0 | `evidence/r10-core-mutation/summary.json` |
| 이전55b5833 실제 reader | PASS, 0 | `evidence/r10-old-reader/summary.json`; schema3 Load/Commit 거절, primary/backup 보존 |
| R01→R09완료→R10→between11 좌표전투·중간SaveLoad | PASS, 0 | `evidence/r10-core/coordinate-series.json`; seed7, hits4517/kills894, 180초, 3receipt 보존 |
| diff whitespace | PASS, 0 | `git diff --check` |

원회귀 변이는 기존 generic 보스전환 구조대로 R09 receipt를 `CompletedBoss`에 넣어 첫 기록을 덮어쓰게 했다. 컴파일 오류가 아닌 R10 진입 assertion 실패로 검출했다. 변이는 임시 복사본에서만 실행했다.

전체 gate 실행 뒤 Sim의 마지막 R09 command를 상위 범위 R10 command로 최소교체했으며 그 교체된 command를 별도 실행해 PASS를 확인했다. 기존 R09 command도 그대로 제공된다.

좌표 시리즈는 기존 장비8/보조1/HP10000 기술 fixture를 사용하며 추가 hit/kill 주입은 없다. 단위검사의 짧은 R09 fixture는 boss hit를 주입하며 R10 timer path 검사는 큰 HP를 주입한다. 어느 것도 자연 플레이 완주·게임밸런스 검증이 아니다.

## 실제 변경파일과 소유 반환

Core: `AdamSession.cs`, `BattleWorld.cs`, `SaveFormat.cs`, `SaveState.cs`, `Waves.cs`, 새 `ExodusContent.cs`.

콘텐츠: 새 `content/exodus/catalog.json`.

tests: 새 `RoundTenTests.cs`, 새 `Fixtures/genesis-schema1-sling-r2-t120.json` 및 schema2 짝, 등록 `Program.cs`, 버전가정 최소수정 `SaveTests.cs`, `SaveSchemaTests.cs`, `RoundTransitionTests.cs`.

Sim: `CatalogReader.cs`, `Program.cs`, `BossRunner.cs`, 새 `ExodusRunner.cs`. gate 최소연결: `tools/check_all.py`의 마지막 CLI 한 줄.

문서: 이 파일 및 `docs/retrospectives/R10-core.md`만 작성. 계획서는 읽기만 했다.

Unity/DLL/provenance/catalog 복사/아트/Editor/GUI/사용자 저장/commit/push는 수행하지 않았다. 독립 코드 리뷰와 Host/UI 연결, Unity sync/build 및 실제 화면·입력 검증은 통합자에게 인계한다. 제품 소유를 반환한다.
